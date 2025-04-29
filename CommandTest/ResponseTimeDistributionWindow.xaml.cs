using System;
using System.Linq;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using CommandTest.Models;
using ScottPlot;
using Timer = System.Timers.Timer;

namespace CommandTest
{
    public partial class ResponseTimeDistributionWindow : Window
    {
        private readonly CommunicationStatistics statistics;
        private readonly Timer updateTimer;
        private int binCount = 10; // デフォルトのバー数

        public ResponseTimeDistributionWindow(CommunicationStatistics statistics)
        {
            InitializeComponent();
            this.statistics = statistics;

            // 自動更新タイマーの設定
            updateTimer = new Timer(1000); // 1秒間隔
            updateTimer.Elapsed += (s, e) => Dispatcher.Invoke(UpdatePlot);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // イベントハンドラを設定
                BinCountComboBox.SelectionChanged += BinCountComboBox_SelectionChanged;

                // デフォルト値を設定
                BinCountComboBox.SelectedIndex = 1;

                // 初期表示とタイマー開始
                UpdatePlot();
                updateTimer.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"初期化エラー: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdatePlot()
        {
            try
            {
                if (PlotControl?.Plot == null) return;

                var data = statistics.GetResponseTimeHistory();
                if (data.Count == 0)
                {
                    PlotControl.Plot.Clear();
                    PlotControl.Plot.Title("応答時間の分布 (データなし)");
                    PlotControl.Refresh();
                    return;
                }

                // データの範囲を計算
                double min = data.Min();
                double max = data.Max();
                double range = max - min;

                if (range <= 0)
                {
                    // すべての値が同じ場合
                    min -= 0.5;
                    max += 0.5;
                    range = 1.0;
                }

                // データの準備
                double[] values = data.ToArray();
                Array.Sort(values);

                // 平均と標準偏差を計算
                double mean = values.Average();
                double stdDev = Math.Sqrt(values.Select(x => Math.Pow(x - mean, 2)).Average());

                // 基本の区間数を設定
                int targetBinCount = binCount;

                // 選択された区間数を使用
                int calculatedBinCount = Math.Min(binCount, data.Count);
                calculatedBinCount = Math.Max(5, calculatedBinCount); // 最小5区間を保証

                // 区間の幅と境界を計算
                double binWidth = range / calculatedBinCount;
                double[] binEdges = new double[calculatedBinCount + 1];
                for (int i = 0; i <= calculatedBinCount; i++)
                {
                    binEdges[i] = min + binWidth * i;
                }

                // 各区間のカウントを計算
                double[] counts = new double[calculatedBinCount];
                double[] positions = new double[calculatedBinCount];

                // 各データを適切な区間にカウント
                foreach (var value in values)
                {
                    int binIndex = (int)((value - min) / binWidth);
                    // 最大値は最後の区間に含める
                    if (binIndex == calculatedBinCount) binIndex--;
                    // 範囲チェック
                    if (binIndex >= 0 && binIndex < calculatedBinCount)
                    {
                        counts[binIndex]++;
                    }
                }

                // 区間の中心位置を計算
                for (int i = 0; i < calculatedBinCount; i++)
                {
                    positions[i] = binEdges[i] + binWidth / 2;
                }

                // プロットの作成
                PlotControl.Plot.Clear();
                
                // バーの描画
                var bar = PlotControl.Plot.AddBar(counts, positions);
                bar.FillColor = System.Drawing.Color.FromArgb(255, 135, 206, 235); // SkyBlue
                bar.BorderColor = System.Drawing.Color.Blue;
                bar.BarWidth = (max - min) / calculatedBinCount * 0.8; // バー幅を少し狭めに設定

                // グラフの設定
                PlotControl.Plot.Title("応答時間の分布");
                PlotControl.Plot.XLabel("応答時間 (ms)");
                PlotControl.Plot.YLabel("頻度");

                // X軸の範囲を調整（余白を追加）
                double margin = (max - min) * 0.1;
                PlotControl.Plot.SetAxisLimits(
                    xMin: min - margin,
                    xMax: max + margin,
                    yMin: 0,
                    yMax: counts.Max() * 1.2);

                // 統計情報の表示（未加工のデータから計算）
                var minStr = statistics.MinResponseTime.HasValue ? $"{statistics.MinResponseTime.Value:F1}" : "---";
                var maxStr = statistics.MaxResponseTime.HasValue ? $"{statistics.MaxResponseTime.Value:F1}" : "---";
                var avgStr = statistics.AverageResponseTime.HasValue ? $"{statistics.AverageResponseTime.Value:F1}" : "---";

                var stats = $"データ数: {data.Count}\n" +
                           $"最小値: {minStr} ms\n" +
                           $"最大値: {maxStr} ms\n" +
                           $"平均値: {avgStr} ms";
                
                PlotControl.Plot.AddAnnotation(stats, 0.98, 0.98);
                PlotControl.Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"グラフ更新エラー: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BinCountComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (BinCountComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                if (int.TryParse(selectedItem.Content.ToString(), out int newBinCount))
                {
                    binCount = newBinCount;
                    UpdatePlot();
                }
            }
        }

        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            UpdatePlot();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            updateTimer.Stop();
            base.OnClosed(e);
        }
    }
}
