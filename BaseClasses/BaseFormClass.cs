using System;
using System.Linq;
using System.Windows.Forms;
using System.Globalization;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Input;

namespace BaseFunction
{
    public static class BaseFormClass
    {    
        /// <summary>
        /// устанавалиет 0 в текстбокс если пользователь не ввел число
        /// используется при событии потери фокуса что бы в текстбоксе точно было число которое можно парсить      
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void TbLostFocus(object sender, EventArgs e)
        {
            dynamic tTB = sender;           
            if (tTB != null)
            {
                foreach (char c in tTB.Text) if (char.IsDigit(c)) return;
                tTB.Text = "0";
            }
        }
        public static void TbKeyDoubleMinus(object sender, System.Windows.Input.KeyEventArgs e)
        {
            KeyPressEventArgs e2 = new KeyPressEventArgs(GetCharFromKey(e.Key));
            TbKeyDoubleMinus(sender, e2);
            e.Handled = e2.Handled;
        }
        /// <summary>
        /// это событие позволяет возможность вводить десятичные положительные и отрицательные числа
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void TbKeyDoubleMinus(object sender, KeyPressEventArgs e)
        {         
            dynamic tTB = sender as TextBox;

            int position = tTB.SelectionStart;
            string withoutSelected = WithoutSelected(tTB);        

            //заменяем запятую на точку
            if (e.KeyChar == ',') e.KeyChar = '.';

            //обработка бекспейса, возможность удаления выделенного фрагмента и установку курсора в месте удаления
            if (e.KeyChar == 8)
            {
                if (tTB.SelectionLength > 0)
                {
                    tTB.Text = withoutSelected;
                    tTB.SelectionStart = position;
                    e.Handled = true;
                }   
                return;
            }
            //инвертируем записанное число при минусе
            if (e.KeyChar == '-')
            {
                tTB.Text = withoutSelected;
                tTB.SelectionStart = position;
                if (tTB.Text.Contains("-"))
                {
                    tTB.Text = tTB.Text.Substring(1);
                    tTB.SelectionStart = position - 1;
                }
                else
                {
                    tTB.Text = '-' + tTB.Text;
                    tTB.SelectionStart = position + 1;
                }
                e.Handled = true;
                return;
            }
            //если введена точка то проверяем что бы точки еще не было, и что бы точку не пытались вводить перед минусом
            if (e.KeyChar == '.')
            {
                if (withoutSelected.Contains('.') | (position == 0 & withoutSelected.Contains("-"))) e.Handled = true;
                else
                {
                    tTB.Text = withoutSelected;
                    tTB.SelectionStart = position;
                }
                return;
            }
           
            //проверка что бы вводимое число не ставилось перед минусом
            if (Char.IsDigit(e.KeyChar))
            {
                if (withoutSelected.Contains("-") & position == 0) e.Handled = true;
                else
                {
                    tTB.Text = withoutSelected;
                    tTB.SelectionStart = position;
                }
                return;
            }
            e.Handled = true;
        }
        public static void TbKeyDouble(object sender, System.Windows.Input.KeyEventArgs e)
        {
            KeyPressEventArgs e2 = new KeyPressEventArgs(GetCharFromKey(e.Key));
            TbKeyDouble(sender, e2);
            e.Handled = e2.Handled;                 
        }
        /// <summary>
        /// это событие позволяет вводить десятичные положительные числа
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void TbKeyDouble(object sender, KeyPressEventArgs e)
        {
            dynamic tTB = sender;

            int position = tTB.SelectionStart;
            string withoutSelected = WithoutSelected(tTB);
            
            //обработка бекспейса, возможность удаления выделенного фрагмента и установку курсора в месте удаления
            if (e.KeyChar == 8)
            {
                if (tTB.SelectionLength > 0)
                {
                    tTB.Text = withoutSelected;
                    tTB.SelectionStart = position;
                    e.Handled = true;
                }
                return;
            }
          
            //если введена точка то проверяем что бы точки еще не было, и что бы точку не пытались вводить перед минусом
            if (e.KeyChar == '.')
            {
                if (withoutSelected.Contains('.') | (position == 0 & withoutSelected.Contains("-"))) e.Handled = true;
                else
                {
                    tTB.Text = withoutSelected;
                    tTB.SelectionStart = position;
                }
                return;
            }

            //проверка что бы вводимое число не ставилось перед минусом
            if (char.IsDigit(e.KeyChar))
            {
                if (withoutSelected.Contains("-") & position == 0) e.Handled = true;
                else
                {
                    tTB.Text = withoutSelected;
                    tTB.SelectionStart = position;
                }
                return;
            }

            e.Handled = true;
        }
        public static void TbKeyIntegerMinus(object sender, System.Windows.Input.KeyEventArgs e)
        {
            KeyPressEventArgs e2 = new KeyPressEventArgs(GetCharFromKey(e.Key));
            TbKeyIntegerMinus(sender, e2);
            e.Handled = e2.Handled;
        }
        /// <summary>
        /// это событие позволяет вводить целые положительные и отрицательные числа
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void TbKeyIntegerMinus(object sender, KeyPressEventArgs e)
        {
            dynamic tTB = sender as TextBox;

            int position = tTB.SelectionStart;
            string withoutSelected = WithoutSelected(tTB);

            //обработка бекспейса, возможность удаления выделенного фрагмента и установку курсора в месте удаления
            if (e.KeyChar == 8)
            {
                if (tTB.SelectionLength > 0)
                {
                    tTB.Text = withoutSelected;
                    tTB.SelectionStart = position;
                    e.Handled = true;
                }
                return;
            }
            //инвертируем записанное число при минусе
            if (e.KeyChar == '-')
            {
                tTB.Text = withoutSelected;
                tTB.SelectionStart = position;
                if (tTB.Text.Contains("-"))
                {
                    tTB.Text = tTB.Text.Substring(1);
                    tTB.SelectionStart = position - 1;
                }
                else
                {
                    tTB.Text = '-' + tTB.Text;
                    tTB.SelectionStart = position + 1;
                }
                e.Handled = true;
                return;
            }       

            //проверка что бы вводимое число не ставилось перед минусом
            if (Char.IsDigit(e.KeyChar))
            {
                if (withoutSelected.Contains("-") & position == 0) e.Handled = true;
                else
                {
                    tTB.Text = withoutSelected;
                    tTB.SelectionStart = position;
                }
                return;
            }

            e.Handled = true;
        }
        public static void TbKeyInteger(object sender, System.Windows.Input.KeyEventArgs e)
        {
            KeyPressEventArgs e2 = new KeyPressEventArgs(GetCharFromKey(e.Key));
            TbKeyInteger(sender, e2);
            e.Handled = e2.Handled;
        }
        /// <summary>
        /// это событие позволяет вводить целые положительные числа
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void TbKeyInteger(object sender, KeyPressEventArgs e)
        {
            dynamic tTB = sender as TextBox;

            int position = tTB.SelectionStart;
            string withoutSelected = WithoutSelected(tTB);

            //обработка бекспейса, возможность удаления выделенного фрагмента и установку курсора в месте удаления
            if (e.KeyChar == 8)
            {
                if (tTB.SelectionLength > 0)
                {
                    tTB.Text = withoutSelected;
                    tTB.SelectionStart = position;
                    e.Handled = true;
                }
                return;
            }
        
            //проверка что бы вводимое число не ставилось перед минусом
            if (Char.IsDigit(e.KeyChar))
            {
                if (withoutSelected.Contains("-") & position == 0) e.Handled = true;
                else
                {
                    tTB.Text = withoutSelected;
                    tTB.SelectionStart = position;
                }
                return;
            }

            e.Handled = true;
        }
        public static bool TryGetIntFromTextBox(TextBox textBox, out int result, int min, int max, string message)
        {
            ParseResult parseResult = TryGetIntFromTextBox(textBox, out result, min, max);
            if (parseResult == ParseResult.Ok) return true;
            else
            {
                if (parseResult == ParseResult.none) MessageBox.Show(message + " введен не корректно");
                else if (parseResult == ParseResult.ToSmall) MessageBox.Show(message + " не может быть меньше " + min);
                else if (parseResult == ParseResult.ToBig) MessageBox.Show(message + " не может быть больше " + max);
                return false;
            }
        }
        public static ParseResult TryGetIntFromTextBox(TextBox textBox, out int result, int min, int max)
        {         
            if (!int.TryParse(textBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out result)) return ParseResult.none;

            if (result > max) return ParseResult.ToBig;
            if (result < min) return ParseResult.ToSmall;

            return ParseResult.Ok;
        }

        public static bool TryGetDoubleFromTextBox(TextBox textBox, out double result, double min, double max, string message)
        {
            ParseResult parseResult = TryGetDoubleFromTextBox(textBox, out result, min, max);
            if (parseResult == ParseResult.Ok) return true;
            else
            {
                if (parseResult == ParseResult.none) MessageBox.Show(message + " введен не корректно");
                else if (parseResult == ParseResult.ToSmall) MessageBox.Show(message + " не может быть меньше " + min);
                else if (parseResult == ParseResult.ToBig) MessageBox.Show(message + " не может быть больше " + max);
                return false;
            }
        }

        public static ParseResult TryGetDoubleFromTextBox(TextBox textBox, out double result, double min, double max)
        {
            if (!double.TryParse(textBox.Text.Trim().Replace(",", "."), NumberStyles.Number, CultureInfo.InvariantCulture, out result)) return ParseResult.none;

            if (result > max) return ParseResult.ToBig;
            if (result < min) return ParseResult.ToSmall;

            return ParseResult.Ok;
        }

        private static string WithoutSelected(this TextBox tTB)
        {
            string text = tTB.Text;
            if (tTB.SelectionLength > 0)
            {               
                text = tTB.Text.Substring(0, tTB.SelectionStart) + tTB.Text.Substring(tTB.SelectionStart + tTB.SelectionLength);              
            }
            return text;
        }
        private static string WithoutSelected(this System.Windows.Controls.TextBox tTB)
        {
            string text = tTB.Text;
            if (tTB.SelectionLength > 0)
            {
                text = tTB.Text.Substring(0, tTB.SelectionStart) + tTB.Text.Substring(tTB.SelectionStart + tTB.SelectionLength);
            }
            return text;
        }
        public static bool CheckString(this string _string,  bool text, bool number, bool space, bool message)
        {
            if (!text && !number && !space) throw new ArgumentException("Параметры не заданы");
            string messageString = "В тексте могут быть только ";
            if (text) messageString += "буквы ";
            if (number) messageString += "цифры ";
            if (space) messageString += "пробелы ";

            foreach (char c in _string)
            {
                if (space && c == ' ') continue;
                if (text)
                {
                    if (c >= 'a' && c <= 'z') continue;
                    if (c >= 'A' && c <= 'Z') continue;
                    if (c >= 'а' && c <= 'я') continue;
                    if (c >= 'А' && c <= 'Я') continue;
                }
                if(space && (c >= '0' && c <= '9')) continue;
                if (message) MessageBox.Show(messageString);
                return false;
            }
            return true;
        }
        public enum ParseResult
        { 
            Ok,
            none,
            ToBig,
            ToSmall,
        }

        public enum MapType : uint
        {
            MAPVK_VK_TO_VSC = 0x0,
            MAPVK_VSC_TO_VK = 0x1,
            MAPVK_VK_TO_CHAR = 0x2,
            MAPVK_VSC_TO_VK_EX = 0x3,
        }

        [DllImport("user32.dll")]
        public static extern int ToUnicode(
            uint wVirtKey,
            uint wScanCode,
            byte[] lpKeyState,
            [Out, MarshalAs(UnmanagedType.LPWStr, SizeParamIndex = 4)]
            StringBuilder pwszBuff,
            int cchBuff,
            uint wFlags);

        [DllImport("user32.dll")]
        public static extern bool GetKeyboardState(byte[] lpKeyState);

        [DllImport("user32.dll")]
        public static extern uint MapVirtualKey(uint uCode, MapType uMapType);

        public static char GetCharFromKey(Key key)
        {
            char ch = ' ';

            int virtualKey = KeyInterop.VirtualKeyFromKey(key);
            byte[] keyboardState = new byte[256];
            GetKeyboardState(keyboardState);

            uint scanCode = MapVirtualKey((uint)virtualKey, MapType.MAPVK_VK_TO_VSC);
            StringBuilder stringBuilder = new StringBuilder(2);

            int result = ToUnicode((uint)virtualKey, scanCode, keyboardState, stringBuilder, stringBuilder.Capacity, 0);
            switch (result)
            {
                case -1:
                    break;
                case 0:
                    break;
                case 1:
                    {
                        ch = stringBuilder[0];
                        break;
                    }
                default:
                    {
                        ch = stringBuilder[0];
                        break;
                    }
            }
            return ch;
        }
    }
}
