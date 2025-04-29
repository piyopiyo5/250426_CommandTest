using System;

namespace CommandTest.Models
{
    /// <summary>
    /// コマンド実行要求を表すクラス
    /// </summary>
    public class CommandRequest
    {
        /// <summary>
        /// 実行するコマンド
        /// </summary>
        public Command Command { get; }

        /// <summary>
        /// 実行開始時刻
        /// </summary>
        public DateTime ExecutionStartTime { get; private set; }

        /// <summary>
        /// 実行完了時刻
        /// </summary>
        public DateTime? ExecutionEndTime { get; private set; }

        /// <summary>
        /// 実行後の待ち時間（ミリ秒）
        /// </summary>
        public int WaitTimeAfterExecution { get; }

        /// <summary>
        /// シーケンスコマンドかどうか
        /// </summary>
        public bool IsSequenceCommand { get; }

        /// <summary>
        /// 実行完了しているかどうか
        /// </summary>
        public bool IsComplete => ExecutionEndTime.HasValue;

        /// <summary>
        /// 受信したデータ
        /// </summary>
        public string? ReceivedData { get; set; }

        /// <summary>
        /// コマンド実行要求を作成します
        /// </summary>
        /// <param name="command">実行するコマンド</param>
        /// <param name="isSequenceCommand">シーケンスコマンドかどうか</param>
        public CommandRequest(Command command, bool isSequenceCommand)
        {
            Command = command ?? throw new ArgumentNullException(nameof(command));
            WaitTimeAfterExecution = command.Interval;
            IsSequenceCommand = isSequenceCommand;
        }

        /// <summary>
        /// コマンドの実行開始を記録します
        /// </summary>
        public void StartExecution()
        {
            ExecutionStartTime = DateTime.Now;
            Command.StartExecution();
        }

        /// <summary>
        /// コマンドの実行完了を記録します
        /// </summary>
        public void CompleteExecution()
        {
            ExecutionEndTime = DateTime.Now;
            Command.CompleteExecution();
        }
    }
}
