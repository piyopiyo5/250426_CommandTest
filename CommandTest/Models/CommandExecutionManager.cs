using System;
using System.Threading;
using System.Threading.Tasks;
using CommandTest.Communications;

namespace CommandTest.Models
{
    /// <summary>
    /// コマンド実行完了イベントの引数
    /// </summary>
    public class CommandExecutionEventArgs : EventArgs
    {
        public CommandRequest Request { get; }

        public CommandExecutionEventArgs(CommandRequest request)
        {
            Request = request;
        }
    }

    /// <summary>
    /// コマンド実行を管理するクラス
    /// </summary>
    public class CommandExecutionManager
    {
        private readonly CommandQueue queue = new CommandQueue();
        private CommandRequest? currentRequest;
        private CancellationTokenSource? executorCts;
        private Task? executionTask;
        private readonly ICommunicator communicator;

        /// <summary>
        /// コマンド実行完了時に発生します
        /// </summary>
        public event EventHandler<CommandExecutionEventArgs>? CommandExecutionCompleted;

        public CommandExecutionManager(ICommunicator communicator)
        {
            this.communicator = communicator ?? throw new ArgumentNullException(nameof(communicator));
        }

        /// <summary>
        /// コマンドの実行要求をキューに追加します
        /// </summary>
        /// <param name="command">実行するコマンド</param>
        /// <param name="isSequenceCommand">シーケンスコマンドかどうか</param>
        public void EnqueueCommand(Command command, bool isSequenceCommand)
        {
            var request = new CommandRequest(command, isSequenceCommand);
            queue.Enqueue(request);

            // 実行ループが開始されていない場合は開始
            if (executionTask == null || executionTask.IsCompleted)
            {
                StartExecution();
            }
        }

        /// <summary>
        /// コマンド実行ループを開始します
        /// </summary>
        public void StartExecution()
        {
            if (executionTask != null && !executionTask.IsCompleted)
            {
                return;
            }

            executorCts = new CancellationTokenSource();
            executionTask = Task.Run(ExecutionLoop);
        }

        /// <summary>
        /// コマンド実行ループを停止します
        /// </summary>
        public void StopExecution()
        {
            executorCts?.Cancel();
            queue.Clear();
        }

        private async Task ExecutionLoop()
        {
            while (!executorCts!.Token.IsCancellationRequested)
            {
                try
                {
                    // 次の実行要求を取得
                    currentRequest = queue.TryDequeue();
                    if (currentRequest == null)
                    {
                        await Task.Delay(100, executorCts.Token); // キューが空の場合は少し待機
                        continue;
                    }

                    // コマンド実行
                    await ExecuteCommand(currentRequest);

                    // 実行完了を通知
                    OnCommandExecutionCompleted(currentRequest);

                    // 待ち時間
                    if (currentRequest.WaitTimeAfterExecution > 0)
                    {
                        await Task.Delay(currentRequest.WaitTimeAfterExecution, executorCts.Token);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception)
                {
                    // エラー発生時は次のコマンドへ
                    continue;
                }
                finally
                {
                    currentRequest = null;
                }
            }
        }

        private async Task ExecuteCommand(CommandRequest request)
        {
            request.StartExecution();

            try
            {
                switch (request.Command.Mode)
                {
                    case "Normal":
                        await ExecuteNormalMode(request.Command);
                        break;

                    case "NoCommand":
                        await ExecuteNoCommandMode(request.Command);
                        break;

                    case "NoResponse":
                        await ExecuteNoResponseMode(request.Command);
                        break;
                }
            }
            finally
            {
                request.CompleteExecution();
            }
        }

        private async Task ExecuteNormalMode(Command command)
        {
            await communicator.Send(command.CommandText);
            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(command.Timeout));
            var receivedData = await Task.Run(() => communicator.Receive(), cts.Token);
            currentRequest!.ReceivedData = receivedData;
        }

        private async Task ExecuteNoCommandMode(Command command)
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(command.Timeout));
            var receivedData = await Task.Run(() => communicator.Receive(), cts.Token);
            currentRequest!.ReceivedData = receivedData;
        }

        private async Task ExecuteNoResponseMode(Command command)
        {
            await communicator.Send(command.CommandText);
        }

        private void OnCommandExecutionCompleted(CommandRequest request)
        {
            CommandExecutionCompleted?.Invoke(this, new CommandExecutionEventArgs(request));
        }
    }
}
