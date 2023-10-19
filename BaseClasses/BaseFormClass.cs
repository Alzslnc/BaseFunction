using System;
using System.Linq;
using System.Windows.Forms;

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
            //заменяем запятую на точку
            if (e.KeyChar == ',') e.KeyChar = '.';
            //инвертируем записанное число при минусе
            if (e.KeyChar == '-')
            {
                int pos = tTB.SelectionStart;
                if (tTB.Text.Contains('-'))
                {
                    tTB.Text = tTB.Text.Substring(1);
                    tTB.SelectionStart = pos - 1;
                }
                else
                {
                    tTB.Text = '-' + tTB.Text;
                    tTB.SelectionStart = pos + 1;
                }
                e.Handled = true;
                return;
            }
            //если введена точка то проверяем что бы точки еще не было, и что бы точку не пытались вводить перед минусом
            if (e.KeyChar == '.')
            {
                if (tTB.Text.Contains('.') | (tTB.SelectionStart == 0 & tTB.Text.Contains('-'))) e.Handled = true;
                return;
            }
            //обработка бекспейса, возможность удаления выделенного фрагмента и установку курсора в месте удаления
            if (e.KeyChar == 8)
            {
                if (tTB.SelectionLength > 0)
                {
                    int pos = tTB.SelectionStart;
                    tTB.Text = tTB.Text.Substring(0, tTB.SelectionStart) + tTB.Text.Substring(tTB.SelectionStart + tTB.SelectionLength);
                    tTB.SelectionStart = pos;
                    e.Handled = true;
                }
                return;
            }
            //проверка что бы вводимое число не ставилось перед минусом
            if (Char.IsDigit(e.KeyChar))
            {
                if (tTB.Text.Contains('-') & tTB.SelectionStart == 0) e.Handled = true;
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
            if (e.KeyChar == ',') e.KeyChar = '.';
            if (e.KeyChar == '.')
            {
                if (tTB.Text.Contains('.') | (tTB.SelectionStart == 0 & tTB.Text.Contains('-'))) e.Handled = true;
                return;
            }
            if (e.KeyChar == 8)
            {
                if (tTB.SelectionLength > 0)
                {
                    int pos = tTB.SelectionStart;
                    tTB.Text = tTB.Text.Substring(0, tTB.SelectionStart) + tTB.Text.Substring(tTB.SelectionStart + tTB.SelectionLength);
                    tTB.SelectionStart = pos;
                    e.Handled = true;
                }
                return;
            }
            if (Char.IsDigit(e.KeyChar))
            {
                if (tTB.Text.Contains('-') & tTB.SelectionStart == 0) e.Handled = true;
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
            if (e.KeyChar == '-')
            {
                int pos = tTB.SelectionStart;
                if (tTB.Text.Contains('-'))
                {
                    tTB.Text = tTB.Text.Substring(1);
                    tTB.SelectionStart = pos - 1;
                }
                else
                {
                    tTB.Text = '-' + tTB.Text;
                    tTB.SelectionStart = pos + 1;
                }
                e.Handled = true;
                return;
            }
            if (e.KeyChar == 8)
            {
                if (tTB.SelectionLength > 0)
                {
                    int pos = tTB.SelectionStart;
                    tTB.Text = tTB.Text.Substring(0, tTB.SelectionStart) + tTB.Text.Substring(tTB.SelectionStart + tTB.SelectionLength);
                    tTB.SelectionStart = pos;
                    e.Handled = true;
                }
                return;
            }
            if (Char.IsDigit(e.KeyChar))
            {
                if (tTB.Text.Contains('-') & tTB.SelectionStart == 0) e.Handled = true;
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
            if (e.KeyChar == 8)
            {
                if (tTB.SelectionLength > 0)
                {
                    int pos = tTB.SelectionStart;
                    tTB.Text = tTB.Text.Substring(0, tTB.SelectionStart) + tTB.Text.Substring(tTB.SelectionStart + tTB.SelectionLength);
                    tTB.SelectionStart = pos;
                    e.Handled = true;
                }
                return;
            }
            if (Char.IsDigit(e.KeyChar))
            {
                if (tTB.Text.Contains('-') & tTB.SelectionStart == 0) e.Handled = true;
                return;
            }
            e.Handled = true;
        }
    }
}
