﻿using System;
using System.Linq;
using System.Windows.Forms;
using System.Globalization;
using System.Runtime;

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
            TextBox tTB = sender as TextBox;
            foreach (Char c in tTB.Text) if (Char.IsDigit(c)) return;
            tTB.Text = "0";
        }
        /// <summary>
        /// это событие позволяет возможность вводить десятичные положительные и отрицательные числа
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void TbKeyDoubleMinus(object sender, KeyPressEventArgs e)
        {         
            TextBox tTB = sender as TextBox;

            int position = tTB.SelectionStart;
            string withoutSelected = tTB.WithoutSelected();

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
                if (tTB.Text.Contains('-'))
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
                if (withoutSelected.Contains('.') | (position == 0 & withoutSelected.Contains('-'))) e.Handled = true;
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
                if (withoutSelected.Contains('-') & position == 0) e.Handled = true;
                else
                {
                    tTB.Text = withoutSelected;
                    tTB.SelectionStart = position;
                }
                return;
            }
            e.Handled = true;
        }
        /// <summary>
        /// это событие позволяет вводить десятичные положительные числа
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void TbKeyDouble(object sender, KeyPressEventArgs e)
        {
            TextBox tTB = sender as TextBox;

            int position = tTB.SelectionStart;
            string withoutSelected = tTB.WithoutSelected();

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
          
            //если введена точка то проверяем что бы точки еще не было, и что бы точку не пытались вводить перед минусом
            if (e.KeyChar == '.')
            {
                if (withoutSelected.Contains('.') | (position == 0 & withoutSelected.Contains('-'))) e.Handled = true;
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
                if (withoutSelected.Contains('-') & position == 0) e.Handled = true;
                else
                {
                    tTB.Text = withoutSelected;
                    tTB.SelectionStart = position;
                }
                return;
            }

            e.Handled = true;
        }
        /// <summary>
        /// это событие позволяет вводить целые положительные и отрицательные числа
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void TbKeyIntegerMinus(object sender, KeyPressEventArgs e)
        {
            TextBox tTB = sender as TextBox;

            int position = tTB.SelectionStart;
            string withoutSelected = tTB.WithoutSelected();

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
                if (tTB.Text.Contains('-'))
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
                if (withoutSelected.Contains('-') & position == 0) e.Handled = true;
                else
                {
                    tTB.Text = withoutSelected;
                    tTB.SelectionStart = position;
                }
                return;
            }

            e.Handled = true;
        }

        /// <summary>
        /// это событие позволяет вводить целые положительные числа
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void TbKeyInteger(object sender, KeyPressEventArgs e)
        {
            TextBox tTB = sender as TextBox;

            int position = tTB.SelectionStart;
            string withoutSelected = tTB.WithoutSelected();

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
                if (withoutSelected.Contains('-') & position == 0) e.Handled = true;
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
    }
}
