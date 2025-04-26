using System;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Linq;
using System.Diagnostics;

namespace CommandTest
{
    public class ReceiveHistory
    {
        public DateTime Time { get; set; }
        public string Direction { get; set; }
        public string Content { get; set; }

        public ReceiveHistory(DateTime time, string direction, string content)
        {
            Time = time;
            Direction = direction;
            Content = content;
        }
    }

    public class CommandResponsePair
    {
        public string Command { get; set; }
        public string Response { get; set; }
        public int DelayMs { get; set; }

        public CommandResponsePair(string command, string response, int delayMs)
        {
            Command = command;
            Response = response;
            DelayMs = delayMs;
        }
    }

    public partial class MainWindow : Window
    {
        private TcpListener? _listener;
        private CancellationTokenSource? _cancellationTokenSource;
        private readonly ObservableCollection<ReceiveHistory> _receiveHistories;
        private readonly ObservableCollection<CommandResponsePair> _commandResponses;
        private const int MAX_COMMAND_PAIRS = 10;
        private CommandResponsePair? _selectedCommand;
        private readonly Stopwatch _stopwatch;

        public MainWindow()
        {
            InitializeComponent();
            _receiveHistories = new ObservableCollection<ReceiveHistory>();
            _commandResponses = new ObservableCollection<CommandResponsePair>();
            _stopwatch = new Stopwatch();
            
            ReceiveHistoryListView.ItemsSource = _receiveHistories;
            CommandResponseListView.ItemsSource = _commandResponses;

            // デフォルトコマンドの追加
            _commandResponses.Add(new CommandResponsePair("TEST", "RESPONSE", 1));
        }

        private async void OpenButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var ipAddress = IPAddress.Parse(IpAddressTextBox.Text);
                var port = int.Parse(PortTextBox.Text);

                _listener = new TcpListener(ipAddress, port);
                _cancellationTokenSource = new CancellationTokenSource();
                
                _listener.Start();
                
                OpenButton.IsEnabled = false;
                CloseButton.IsEnabled = true;
                IpAddressTextBox.IsEnabled = false;
                PortTextBox.IsEnabled = false;

                await AcceptClientsAsync(_cancellationTokenSource.Token);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"エラーが発生しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                CloseServer();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            CloseServer();
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(CommandTextBox.Text) || string.IsNullOrWhiteSpace(ResponseTextBox.Text))
            {
                MessageBox.Show("コマンドとレスポンスを入力してください。", "入力エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(DelayTextBox.Text, out int delayMs) || delayMs < 0)
            {
                MessageBox.Show("応答時間は0以上の整数を入力してください。", "入力エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_commandResponses.Count >= MAX_COMMAND_PAIRS)
            {
                MessageBox.Show("コマンドとレスポンスのペアは最大10個までです。", "登録制限", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _commandResponses.Add(new CommandResponsePair(CommandTextBox.Text, ResponseTextBox.Text, delayMs));
            ClearInputs();
        }

        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedCommand == null) return;

            if (string.IsNullOrWhiteSpace(CommandTextBox.Text) || string.IsNullOrWhiteSpace(ResponseTextBox.Text))
            {
                MessageBox.Show("コマンドとレスポンスを入力してください。", "入力エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(DelayTextBox.Text, out int delayMs) || delayMs < 0)
            {
                MessageBox.Show("応答時間は0以上の整数を入力してください。", "入力エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int index = _commandResponses.IndexOf(_selectedCommand);
            if (index != -1)
            {
                _commandResponses[index] = new CommandResponsePair(CommandTextBox.Text, ResponseTextBox.Text, delayMs);
            }

            ClearInputs();
            CommandResponseListView.SelectedItem = null;
        }

        private void CommandResponseListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedCommand = CommandResponseListView.SelectedItem as CommandResponsePair;
            if (_selectedCommand != null)
            {
                CommandTextBox.Text = _selectedCommand.Command;
                ResponseTextBox.Text = _selectedCommand.Response;
                DelayTextBox.Text = _selectedCommand.DelayMs.ToString();
                UpdateButton.IsEnabled = true;
                AddButton.IsEnabled = false;
            }
            else
            {
                ClearInputs();
                UpdateButton.IsEnabled = false;
                AddButton.IsEnabled = true;
            }
        }

        private void ClearInputs()
        {
            CommandTextBox.Clear();
            ResponseTextBox.Clear();
            DelayTextBox.Text = "1";
            _selectedCommand = null;
            UpdateButton.IsEnabled = false;
            AddButton.IsEnabled = true;
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = CommandResponseListView.SelectedItem as CommandResponsePair;
            if (selectedItem != null)
            {
                _commandResponses.Remove(selectedItem);
                ClearInputs();
            }
        }

        private void CloseServer()
        {
            _cancellationTokenSource?.Cancel();
            _listener?.Stop();
            
            OpenButton.IsEnabled = true;
            CloseButton.IsEnabled = false;
            IpAddressTextBox.IsEnabled = true;
            PortTextBox.IsEnabled = true;
        }

        private async Task AcceptClientsAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var client = await _listener!.AcceptTcpClientAsync();
                    _ = HandleClientAsync(client, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                // キャンセル時は正常終了
            }
            catch (Exception ex)
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show($"接続エラーが発生しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                });
                CloseServer();
            }
        }

        private string GetDelimiter()
        {
            return DelimiterComboBox.SelectedIndex switch
            {
                0 => "\r",
                1 => "\n",
                2 => "\r\n",
                _ => "\n"
            };
        }

        private async Task PreciseDelayAsync(int milliseconds, CancellationToken cancellationToken)
        {
            if (milliseconds <= 0) return;

            _stopwatch.Restart();
            var spinWait = new SpinWait();

            while (_stopwatch.ElapsedMilliseconds < milliseconds)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    throw new OperationCanceledException(cancellationToken);
                }

                if (milliseconds - _stopwatch.ElapsedMilliseconds > 1)
                {
                    await Task.Delay(1, cancellationToken);
                }
                else
                {
                    spinWait.SpinOnce();
                }
            }
        }

        private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
        {
            try
            {
                using (client)
                using (var stream = client.GetStream())
                {
                    var buffer = new byte[1024];
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                        if (bytesRead == 0) break; // クライアントが切断

                        var receivedData = Encoding.UTF8.GetString(buffer, 0, bytesRead).TrimEnd();
                        
                        await Dispatcher.InvokeAsync(() =>
                        {
                            // 受信データを記録
                            _receiveHistories.Add(new ReceiveHistory(DateTime.Now, "受信", receivedData));
                            if (ReceiveHistoryListView.Items.Count > 0)
                            {
                                var lastItem = ReceiveHistoryListView.Items[^1];
                                ReceiveHistoryListView.ScrollIntoView(lastItem);
                            }
                        });

                        // 登録されたコマンドに一致するか確認し、レスポンスを送信
                        var matchingPair = _commandResponses.FirstOrDefault(pair => pair.Command == receivedData);
                        if (matchingPair != null)
                        {
                            // 高精度な待機処理
                            await PreciseDelayAsync(matchingPair.DelayMs, cancellationToken);

                            var response = matchingPair.Response + GetDelimiter();
                            var responseBytes = Encoding.UTF8.GetBytes(response);
                            await stream.WriteAsync(responseBytes, 0, responseBytes.Length, cancellationToken);

                            // 送信データを記録
                            await Dispatcher.InvokeAsync(() =>
                            {
                                _receiveHistories.Add(new ReceiveHistory(DateTime.Now, "送信", matchingPair.Response));
                                if (ReceiveHistoryListView.Items.Count > 0)
                                {
                                    var lastItem = ReceiveHistoryListView.Items[^1];
                                    ReceiveHistoryListView.ScrollIntoView(lastItem);
                                }
                            });
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // キャンセル時は正常終了
            }
            catch (Exception ex)
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show($"クライアント処理でエラーが発生しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            CloseServer();
            base.OnClosed(e);
        }
    }
}
