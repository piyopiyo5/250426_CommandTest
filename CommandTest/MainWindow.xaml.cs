using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Linq;
using CommandTest.Communications;
using CommandTest.Models;

static class VisualTreeHelperExtensions
{
    public static IEnumerable<T> FindChildren<T>(this DependencyObject parent) where T : DependencyObject
    {
        var count = VisualTreeHelper.GetChildrenCount(parent);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T childType)
            {
                yield return childType;
            }

            foreach (var descendant in FindChildren<T>(child))
            {
                yield return descendant;
            }
        }
    }
}

namespace CommandTest
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private readonly ICommunicator communicator;
        private Command singleCommand;
        private readonly ObservableCollection<LogEntry> communicationLogs;
        private readonly ObservableCollection<Command> sequenceCommands;
        private readonly CommunicationSettings settings;
        private readonly CommunicationStatistics statistics;
        private readonly CommandExecutionManager executionManager;
        private bool isConnected;
        private bool isSequenceRunning;
        private string connectionState = "未接続";

        public string ConnectionState
        {
            get => connectionState;
            set
            {
                connectionState = value;
                OnPropertyChanged(nameof(ConnectionState));
            }
        }

        private readonly System.Windows.Threading.DispatcherTimer connectionTimer;
        private CancellationTokenSource? sequenceCts;

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
        public ObservableCollection<Command> SequenceCommands => sequenceCommands;
        public CommunicationSettings Settings => settings;
        public CommunicationStatistics Statistics => statistics;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            settings = new CommunicationSettings();
            statistics = new CommunicationStatistics();
            communicator = new TcpCommunicator(settings);
            executionManager = new CommandExecutionManager(communicator);
            executionManager.CommandExecutionCompleted += OnCommandExecutionCompleted;
            communicationLogs = new ObservableCollection<LogEntry>();
            sequenceCommands = new ObservableCollection<Command>();
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
                    ConnectionState = "接続中";
                }
                else
                {
                    await communicator.Disconnect();
                    isConnected = false;
                    ConnectButton.Content = "接続";
                    statistics.EndConnection();
                    connectionTimer.Stop();

                    LogMessage("Info", "切断しました", "Success");
                    ConnectionState = "未接続";
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

        private void ExecuteButton_Click(object sender, RoutedEventArgs e)
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
            executionManager.EnqueueCommand(SingleCommand, false);
        }

        private async void OnCommandExecutionCompleted(object? sender, CommandExecutionEventArgs e)
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var request = e.Request;
                var command = request.Command;
                var elapsed = request.ExecutionEndTime!.Value.Subtract(request.ExecutionStartTime).TotalMilliseconds;

                if (command.Mode == "Normal")
                {
                    LogMessage("Send", command.CommandText, "Success");
                    LogMessage("Receive", request.ReceivedData ?? string.Empty, "Success", elapsed);
                    statistics.IncrementSendSuccess();
                    statistics.IncrementReceiveSuccess();
                    statistics.UpdateResponseTime(elapsed);
                }
                else if (command.Mode == "NoCommand")
                {
                    LogMessage("Receive", request.ReceivedData ?? string.Empty, "Success");
                    statistics.IncrementReceiveSuccess();
                }
                else if (command.Mode == "NoResponse")
                {
                    LogMessage("Send", command.CommandText, "Success");
                    statistics.IncrementSendSuccess();
                }

                // 単発コマンド実行完了時
                if (!request.IsSequenceCommand)
                {
                    ExecuteButton.IsEnabled = true;
                }
                // シーケンスコマンド実行完了時で、シーケンス実行中なら次のコマンドを登録
                else if (isSequenceRunning)
                {
                    var index = SequenceCommands.IndexOf(command);
                    var nextIndex = (index + 1) % SequenceCommands.Count;
                    executionManager.EnqueueCommand(SequenceCommands[nextIndex], true);
                }
            });
        }

        private void ConnectionTimer_Tick(object sender, EventArgs e)
        {
            statistics.UpdateConnectionTime();
            OnPropertyChanged(nameof(Statistics));
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

        private void AddCommandButton_Click(object sender, RoutedEventArgs e)
        {
            SequenceCommands.Add(new Command
            {
                CommandText = "",
                Mode = "Normal",
                Timeout = 1000,
                Interval = 0
            });
        }

        private void RemoveCommandButton_Click(object sender, RoutedEventArgs e)
        {
            if (SequenceCommandsDataGrid.SelectedItem is Command command)
            {
                SequenceCommands.Remove(command);
            }
        }

        private void MoveUpButton_Click(object sender, RoutedEventArgs e)
        {
            if (SequenceCommandsDataGrid.SelectedItem is Command command)
            {
                var index = SequenceCommands.IndexOf(command);
                if (index > 0)
                {
                    SequenceCommands.Move(index, index - 1);
                }
            }
        }

        private void MoveDownButton_Click(object sender, RoutedEventArgs e)
        {
            if (SequenceCommandsDataGrid.SelectedItem is Command command)
            {
                var index = SequenceCommands.IndexOf(command);
                if (index < SequenceCommands.Count - 1)
                {
                    SequenceCommands.Move(index, index + 1);
                }
            }
        }

        private void StartSequenceButton_Click(object sender, RoutedEventArgs e)
        {
            if (!isConnected)
            {
                MessageBox.Show("先に接続してください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (SequenceCommands.Count == 0)
            {
                MessageBox.Show("コマンドが登録されていません。", "エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (isSequenceRunning)
            {
                MessageBox.Show("すでにシーケンスを実行中です。", "エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            StartSequenceButton.IsEnabled = false;
            AddCommandButton.IsEnabled = false;
            RemoveCommandButton.IsEnabled = false;
            MoveUpButton.IsEnabled = false;
            MoveDownButton.IsEnabled = false;
            SequenceCommandsDataGrid.IsEnabled = false;
            ConnectButton.IsEnabled = false;
            isSequenceRunning = true;

            // 履歴・統計操作ボタンを無効化
            DisableOperationButtons();

            try
            {
                sequenceCts = new CancellationTokenSource();
                if (SequenceCommands.Count > 0)
                {
                    executionManager.EnqueueCommand(SequenceCommands[0], true);
                }
            }
            catch (Exception ex)
            {
                LogError("Sequence", ex.Message);
                MessageBox.Show($"シーケンス実行エラー: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DisableOperationButtons()
        {
            var logPane = (DependencyObject)CommunicationLogDataGrid.Parent;
            foreach (var button in logPane.FindChildren<Button>())
            {
                button.IsEnabled = false;
            }

            var statsPane = (Panel)((GroupBox)this.FindName("StatisticsGroupBox")).Content;
            foreach (var button in statsPane.FindChildren<Button>())
            {
                button.IsEnabled = false;
            }
        }

        private void EnableOperationButtons()
        {
            var logPane = (DependencyObject)CommunicationLogDataGrid.Parent;
            foreach (var button in logPane.FindChildren<Button>())
            {
                button.IsEnabled = true;
            }

            var statsPane = (Panel)((GroupBox)this.FindName("StatisticsGroupBox")).Content;
            foreach (var button in statsPane.FindChildren<Button>())
            {
                button.IsEnabled = true;
            }
        }

        private void StopSequenceButton_Click(object sender, RoutedEventArgs e)
        {
            sequenceCts?.Cancel();
            isSequenceRunning = false;
            executionManager.StopExecution();

            StartSequenceButton.IsEnabled = true;
            AddCommandButton.IsEnabled = true;
            RemoveCommandButton.IsEnabled = true;
            MoveUpButton.IsEnabled = true;
            MoveDownButton.IsEnabled = true;
            SequenceCommandsDataGrid.IsEnabled = true;
            ConnectButton.IsEnabled = true;

            EnableOperationButtons();
        }

        private void ClearCommunicationLog_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("送受信履歴をクリアしますか？", "確認", 
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                communicationLogs.Clear();
            }
        }

        private void ClearStatistics_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("統計情報をクリアしますか？", "確認", 
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                statistics.Reset();
                OnPropertyChanged(nameof(Statistics));
            }
        }

        private void ExportStatistics_Click(object sender, RoutedEventArgs e)
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
                    sb.AppendLine(CommunicationStatistics.CsvHeader);
                    sb.AppendLine(statistics.ToCsv());

                    File.WriteAllText(dialog.FileName, sb.ToString(), Encoding.UTF8);
                    MessageBox.Show("統計情報を保存しました。", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"統計情報の保存に失敗しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
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
                    foreach (var log in communicationLogs.OrderBy(log => log.Timestamp))
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

        private void ShowDistributionGraph_Click(object sender, RoutedEventArgs e)
        {
            var window = new ResponseTimeDistributionWindow(statistics);
            window.Owner = this;
            window.Show();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
