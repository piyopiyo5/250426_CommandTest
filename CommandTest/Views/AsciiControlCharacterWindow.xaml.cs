using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using CommandTest.Models;

namespace CommandTest.Views
{
    /// <summary>
    /// ASCII制御文字選択ウィンドウ
    /// </summary>
    public partial class AsciiControlCharacterWindow : Window
    {
        /// <summary>
        /// 選択された制御文字
        /// </summary>
        public string? SelectedControlCharacter { get; private set; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public AsciiControlCharacterWindow()
        {
            InitializeComponent();

            // 制御文字の表示名リストを生成
            var displayNames = new List<string>();
            for (int i = 0; i <= 0x1F; i++)
            {
                displayNames.Add($"[{AsciiControlCharacter.GetControlCharacterName(i)}]");
            }
            displayNames.Add("[DEL]");

            // ListBoxにセット
            ControlCharacterList.ItemsSource = displayNames;
        }

        /// <summary>
        /// リストボックスダブルクリック時の処理
        /// </summary>
        private void ControlCharacterList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ControlCharacterList.SelectedItem is string selectedItem)
            {
                SelectedControlCharacter = selectedItem;
                DialogResult = true;
                Close();
            }
        }
    }
}
