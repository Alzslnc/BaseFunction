using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace Progress
{
    /// <summary>
    /// Логика взаимодействия для InputBox.xaml
    /// </summary>
    public partial class InputBox : Window
    {
        // Свойство-флаг для контроля валидации имени файла
        private bool ValidateFileNameChars { get; set; }

        private List<string> ExceptValues { get; set; } = new List<string>();

        public string Value => TextBox.Text.Trim();

        /// <summary>
        /// Конструктор окна ввода.
        /// </summary>
        /// <param name="exceptValues">Список зарезервированных/существующих имен для проверки уникальности.</param>
        /// <param name="message">Текст сообщения над полем ввода.</param>
        /// <param name="validateFileNameChars">Если true, запрещает ввод символов, недопустимых для имен файлов Windows.</param>
        public InputBox(List<string> exceptValues = null, string message = null, bool validateFileNameChars = false)
        {
            InitializeComponent();

            ValidateFileNameChars = validateFileNameChars;

            if (exceptValues != null) ExceptValues = exceptValues;
            if (!string.IsNullOrEmpty(message)) TextBlock.Text = message;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string inputText = TextBox.Text.Trim();

            // 1. Проверка на пустоту
            if (string.IsNullOrEmpty(inputText))
            {
                MessageBox.Show("Значение должно иметь хотя бы один символ", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 2. Проверка на недопустимые символы файловой системы Windows
            if (ValidateFileNameChars)
            {
                // Получаем массив запрещенных символов из самой Windows (\, /, :, *, ?, ", <, >, | и др.)
                char[] invalidChars = Path.GetInvalidFileNameChars();

                // Классический способ проверки наличия любого символа из массива в строке
                if (inputText.IndexOfAny(invalidChars) >= 0)
                {
                    string forbiddenCharsDisplay = string.Join(" ", invalidChars.Where(c => !char.IsControl(c)));
                    MessageBox.Show(
                        $"Имя содержит недопустимые символы!\nЗапрещено использовать: {forbiddenCharsDisplay}",
                        "Внимание",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }
            }

            // 3. Проверка уникальности
            bool isDuplicate = ExceptValues.Any(val =>
                val.Trim().Equals(inputText, StringComparison.OrdinalIgnoreCase));

            if (isDuplicate)
            {
                MessageBox.Show("Данное значение уже имеется (регистр не имеет значения)", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DialogResult = true;
            Close();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            TextBox.Text = string.Empty;
            DialogResult = false;
            Close();
        }

        private void Move(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }
    }
}
