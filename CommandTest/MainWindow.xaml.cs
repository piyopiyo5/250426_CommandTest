using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CommandTest.Communications;
using CommandTest.Models;

namespace CommandTest
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private readonly ICommunicator communicator;
        private Command singleCommand;
        private readonly ObservableCollection<LogEntry> communicationLogs;
        private readonly CommunicationSettings settings;
        private readonly CommunicationStatistics statistics;
        private bool isConnected;
        private readonly System.Windows.Threading.DispatcherTimer connectionTimer;

        public Command SingleCommand
        {
            get => singleCommand;
            set
            {
                singleCommand = value;
                OnPropertyChanged(nameof(SingleCommand));
            }
        }

        public ObservableCollection<LogEntry> CommunicationLogs => communicationLogs;
        public CommunicationSettings Settings => settings;
        public CommunicationStatistics Statistics => statistics;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            settings = new CommunicationSettings();
            statistics = new CommunicationStatistics();
            communicator = new TcpCommunicator(settings);
            communicationLogs = new ObservableCollection<LogEntry>();
            singleCommand = new Command();

            connectionTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            connectionTimer.Tick += ConnectionTimer_Tick;
            connectionTimer.Start();  // タイマーを起動
        }

        private async void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!isConnected)
                {
                    ConnectButton.Content = "接続中...";
                    ConnectButton.IsEnabled = false;

                    await communicator.Connect();
                    isConnected = true;
                    ConnectButton.Content = "切断";
                    statistics.StartConnection();
                    connectionTimer.Start();

                    LogMessage("Info", "接続しました", "Success");
                }
                else
                {
                    await communicator.Disconnect();
                    isConnected = false;
                    ConnectButton.Content = "接続";
                    statistics.EndConnection();
                    connectionTimer.Stop();

                    LogMessage("Info", "切断しました", "Success");
                }
            }
            catch (Exception ex)
            {
                LogError("Connection", ex.Message);
                MessageBox.Show($"接続エラー: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                ConnectButton.IsEnabled = true;
            }
        }

        private async void ExecuteButton_Click(object sender, RoutedEventArgs e)
        {
            if (!isConnected)
            {
                MessageBox.Show("先に接続してください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrEmpty(SingleCommand.CommandText) && SingleCommand.Mode != "NoCommand")
            {
                MessageBox.Show("コマンドを入力してください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ExecuteButton.IsEnabled = false;

            try
            {
                await ExecuteCommand(SingleCommand);
            }
            catch (Exception ex)
            {
                LogError("Execution", ex.Message);
                MessageBox.Show($"実行エラー: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                ExecuteButton.IsEnabled = true;
            }
        }

        private async Task ExecuteCommand(Command command)
        {
            command.StartExecution();

            try
            {
                switch (command.Mode)
                {
                    case "Normal":
                        await ExecuteNormalMode(command);
                        break;

                    case "NoCommand":
                        await ExecuteNoCommandMode(command);
                        break;

                    case "NoResponse":
                        await ExecuteNoResponseMode(command);
                        break;
                }
            }
            finally
            {
                command.CompleteExecution();
            }

            if (command.Interval > 0)
            {
                await Task.Delay(command.Interval);
            }
        }

        private async Task ExecuteNormalMode(Command command)
        {
            var sendTime = DateTime.Now;
            await communicator.Send(command.CommandText);
            LogMessage("Send", command.CommandText, "Success");
            statistics.IncrementSendSuccess();

            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(command.Timeout));
            try
            {
                var response = await Task.Run(() => communicator.Receive(), cts.Token);
                var responseTime = (DateTime.Now - sendTime).TotalMilliseconds;
                LogMessage("Receive", response, "Success", responseTime);
                statistics.IncrementReceiveSuccess();
                statistics.UpdateResponseTime(responseTime);
            }
            catch (OperationCanceledException)
            {
                statistics.IncrementTimeout();
                throw new TimeoutException($"応答待ちがタイムアウトしました（{command.Timeout}ms）");
            }
        }

        private async Task ExecuteNoCommandMode(Command command)
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(command.Timeout));
            try
            {
                var response = await Task.Run(() => communicator.Receive(), cts.Token);
                LogMessage("Receive", response, "Success");
                statistics.IncrementReceiveSuccess();
            }
            catch (OperationCanceledException)
            {
                statistics.IncrementTimeout();
                throw new TimeoutException($"受信待ちがタイムアウトしました（{command.Timeout}ms）");
            }
        }

        private async Task ExecuteNoResponseMode(Command command)
        {
            await communicator.Send(command.CommandText);
            LogMessage("Send", command.CommandText, "Success");
            statistics.IncrementSendSuccess();
        }

        private void ConnectionTimer_Tick(object sender, EventArgs e)
        {
            statistics.UpdateConnectionTime();
            OnPropertyChanged(nameof(Statistics));  // 統計情報の更新を通知
        }

        private void LogMessage(string type, string data, string result, double responseTime = 0)
        {
            var log = new LogEntry
            {
                Type = type,
                Data = data,
                Result = result,
                ResponseTime = responseTime
            };

            Application.Current.Dispatcher.Invoke(() =>
            {
                communicationLogs.Insert(0, log);
            });
        }

        private void LogError(string location, string message)
        {
            var log = new LogEntry
            {
                Type = "Error",
                ErrorType = "Error",
                Location = location,
                Data = message
            };

            Application.Current.Dispatcher.Invoke(() =>
            {
                communicationLogs.Insert(0, log);
            });

            statistics.IncrementError();
        }

        private void ExportCommunicationLog_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                DefaultExt = ".csv",
                Filter = "CSVファイル (*.csv)|*.csv"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var sb = new StringBuilder();
                    sb.AppendLine(LogEntry.CsvHeader);
                    foreach (var log in communicationLogs)
                    {
                        sb.AppendLine(log.ToCsv());
                    }

                    File.WriteAllText(dialog.FileName, sb.ToString(), Encoding.UTF8);
                    MessageBox.Show("ログを保存しました。", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"ログの保存に失敗しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
