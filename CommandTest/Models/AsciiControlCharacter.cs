using System.Collections.Generic;
using System.Linq;

namespace CommandTest.Models
{
    /// <summary>
    /// ASCII制御文字の変換を行うクラス
    /// </summary>
    public static class AsciiControlCharacter
    {
        // 制御文字と表示名の対応
        private static readonly Dictionary<string, char> controlCharacters = new()
        {
            ["NUL"] = (char)0x00,
            ["SOH"] = (char)0x01,
            ["STX"] = (char)0x02,
            ["ETX"] = (char)0x03,
            ["EOT"] = (char)0x04,
            ["ENQ"] = (char)0x05,
            ["ACK"] = (char)0x06,
            ["BEL"] = (char)0x07,
            ["BS"] = (char)0x08,
            ["HT"] = (char)0x09,
            ["LF"] = (char)0x0A,
            ["VT"] = (char)0x0B,
            ["FF"] = (char)0x0C,
            ["CR"] = (char)0x0D,
            ["SO"] = (char)0x0E,
            ["SI"] = (char)0x0F,
            ["DLE"] = (char)0x10,
            ["DC1"] = (char)0x11,
            ["DC2"] = (char)0x12,
            ["DC3"] = (char)0x13,
            ["DC4"] = (char)0x14,
            ["NAK"] = (char)0x15,
            ["SYN"] = (char)0x16,
            ["ETB"] = (char)0x17,
            ["CAN"] = (char)0x18,
            ["EM"] = (char)0x19,
            ["SUB"] = (char)0x1A,
            ["ESC"] = (char)0x1B,
            ["FS"] = (char)0x1C,
            ["GS"] = (char)0x1D,
            ["RS"] = (char)0x1E,
            ["US"] = (char)0x1F,
            ["DEL"] = (char)0x7F
        };

        /// <summary>
        /// 制御文字のコードから表示名を取得
        /// </summary>
        public static string GetControlCharacterName(int code)
        {
            if (code == 0x7F)
                return "DEL";

            if (code >= 0 && code <= 0x1F)
            {
                return controlCharacters.First(x => x.Value == (char)code).Key;
            }

            return string.Empty;
        }

        /// <summary>
        /// 制御文字の表示名を実際の制御文字に変換
        /// </summary>
        public static string ConvertDisplayNameToControlCharacters(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            var result = text;
            foreach (var pair in controlCharacters)
            {
                result = result.Replace($"[{pair.Key}]", pair.Value.ToString());
            }
            return result;
        }
    }
}
