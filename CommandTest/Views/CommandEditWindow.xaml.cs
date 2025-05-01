using System.Windows;
using CommandTest.Models;

namespace CommandTest.Views
{
    /// <summary>
    /// コマンド編集ウィンドウ
    /// </summary>
    public partial class CommandEditWindow : Window
    {
        /// <summary>
        /// 編集結果のコマンド
        /// </summary>
        public Command? ResultCommand { get; private set; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="command">編集対象のコマンド（nullの場合は新規作成）</param>
        public CommandEditWindow(Command? command = null)
        {
            InitializeComponent();

            // 既存のコマンドがある場合は値を設定
            if (command != null)
            {
                CommandTextBox.Text = command.CommandText;
                ModeComboBox.SelectedValue = command.Mode;
                TimeoutTextBox.Text = command.Timeout.ToString();
                IntervalTextBox.Text = command.Interval.ToString();
            }
            else
            {
                // デフォルト値を設定
                ModeComboBox.SelectedValue = "Normal";
                TimeoutTextBox.Text = "1000";
                IntervalTextBox.Text = "0";
            }
        }

        /// <summary>
        /// OKボタンクリック時の処理
        /// </summary>
        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            // 入力値のバリデーション
            if (!ValidateInput())
            {
                return;
            }

            // コマンドオブジェクトを作成
            ResultCommand = new Command
            {
                CommandText = CommandTextBox.Text,
                Mode = ModeComboBox.SelectedValue.ToString()!,
                Timeout = int.Parse(TimeoutTextBox.Text),
                Interval = int.Parse(IntervalTextBox.Text)
            };

            DialogResult = true;
            Close();
        }

        /// <summary>
        /// キャンセルボタンクリック時の処理
        /// </summary>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        /// <summary>
        /// 制御記号挿入ボタンクリック時の処理
        /// </summary>
        private void InsertControlCharacter_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AsciiControlCharacterWindow
            {
                Owner = this
            };

            if (dialog.ShowDialog() == true && !string.IsNullOrEmpty(dialog.SelectedControlCharacter))
            {
                int caretIndex = CommandTextBox.CaretIndex;
                string currentText = CommandTextBox.Text ?? string.Empty;
                string newText = currentText.Insert(caretIndex, dialog.SelectedControlCharacter);
                
                CommandTextBox.Text = newText;
                CommandTextBox.CaretIndex = caretIndex + dialog.SelectedControlCharacter.Length;
                CommandTextBox.Focus();
            }
        }

        /// <summary>
        /// 入力値のバリデーション
        /// </summary>
        private bool ValidateInput()
        {
            if (ModeComboBox.SelectedValue == null)
            {
                MessageBox.Show("送受信モードを選択してください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                ModeComboBox.Focus();
                return false;
            }

            // 受信のみモード以外でコマンドが空の場合はエラー
            if (ModeComboBox.SelectedValue.ToString() != "NoCommand" && string.IsNullOrEmpty(CommandTextBox.Text))
            {
                MessageBox.Show("コマンドを入力してください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                CommandTextBox.Focus();
                return false;
            }

            if (!int.TryParse(TimeoutTextBox.Text, out var timeout) || timeout <= 0)
            {
                MessageBox.Show("タイムアウトには正の整数を入力してください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                TimeoutTextBox.Focus();
                return false;
            }

            if (!int.TryParse(IntervalTextBox.Text, out var interval) || interval < 0)
            {
                MessageBox.Show("待ち時間には0以上の整数を入力してください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                IntervalTextBox.Focus();
                return false;
            }

            return true;
        }
    }
}
