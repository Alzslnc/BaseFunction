using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Markup;

namespace BaseFunction
{
    public abstract class BaseClass : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public virtual bool SetData<T>(ref T data, T value, [CallerMemberName] string name = "")
        {
            if (EqualityComparer<T>.Default.Equals(data, value)) return false;
            data = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            return true;
        }
        public virtual bool Call([CallerMemberName] string name = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            return true;
        }
    }

    public class RelayCommand : ICommand
    {
        public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
        {
            this.execute = execute;
            this.canExecute = canExecute;
        }

        private Action<object> execute;
        private Func<object, bool> canExecute;

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object parameter)
        {
            return this.canExecute == null || this.canExecute(parameter);
        }

        public void Execute(object parameter)
        {
            this.execute(parameter);
        }
    }

    public abstract class ConverterBase : MarkupExtension, IValueConverter
    {
        public virtual object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public virtual object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }
    }

    internal class EnumConverter : ConverterBase
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value?.Equals(parameter);
        }
        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value?.Equals(true) == true ? parameter : Binding.DoNothing;
        }
    }

    public class BoolToVisibleConverter : ConverterBase
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return Visibility.Visible;
            if (value is bool bValue)
            {
                if (bValue) return Visibility.Visible;
                else return Visibility.Collapsed;
            }
            return Binding.DoNothing;
        }
    }
    public class NumberToVisibleConverter : ConverterBase
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return Visibility.Visible;
            if (value is int bValue)
            {
                if (bValue > 0) return Visibility.Visible;
                else return Visibility.Collapsed;
            }
            return Binding.DoNothing;
        }
    }
    public class BoolToInvisibleConverter : ConverterBase
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return Visibility.Collapsed;
            if (value is bool bValue)
            {
                if (bValue) return Visibility.Collapsed;
                else return Visibility.Visible;
            }
            return Binding.DoNothing;
        }
    }

    public class BoolInvertedConverter : ConverterBase
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return false;
            if (value is bool bValue) return !bValue;
            return false;
        }
        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return false;
            if (value is bool bValue) return !bValue;
            return false;
        }
    }

    public class StringToDoubleConverter : ConverterBase
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return Binding.DoNothing;

            if (double.TryParse(value.ToString().Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out double result)) return result;

            return Binding.DoNothing;
        }
    }
    public class StringToIntConverter : ConverterBase
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return Binding.DoNothing;

            if (int.TryParse(value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out int result)) return result;

            return Binding.DoNothing;
        }
    }
    public class BaseDateConverter : ConverterBase
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime date)
            {
                return date.ToString("dd.MM.yyyy HH:mm");
            }
            return Binding.DoNothing;
        }
    }
    /// <summary>
    /// созвращает true если значение больше нуля
    /// </summary>
    public class NumberToBoolConverter : ConverterBase
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return false;
            if (value is int i && i > 0) return true;
            return false;
        }
    }
    public class StringToBoolConverter : ConverterBase
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return false;
            return !string.IsNullOrEmpty(value.ToString());
        }
    }

    /// <summary>
    /// Безопасный конвертер строк в GridLength для .NET Framework (WPF).
    /// Защищает InitializeComponent от падений при некорректных данных в кэше.
    /// </summary>
    public class StringToGridLengthConverter : ConverterBase
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 1. Безопасная проверка на null и пустые строки в стиле .NET Framework
            if (value == null || string.IsNullOrEmpty(value.ToString().Trim()))
            {
                return new GridLength(150);
            }

            string strValue = value.ToString().Trim();

            // 2. Обработка пропорционального размера со звездочкой (например, "1.5*")
            if (strValue.EndsWith("*"))
            {
                string numPart = strValue.Replace("*", "");

                // Если перед звездой ничего нет (просто "*"), значит это 1*
                if (string.IsNullOrEmpty(numPart))
                {
                    return new GridLength(1, GridUnitType.Star);
                }

                // Заменяем разделители для универсального парсинга в САПР
                numPart = numPart.Replace(",", ".");

                if (double.TryParse(numPart, NumberStyles.Any, CultureInfo.InvariantCulture, out double starValue))
                {
                    return new GridLength(starValue, GridUnitType.Star);
                }

                return new GridLength(1, GridUnitType.Star);
            }

            // 3. Обработка фиксированного размера в пикселях (например, "150" или "150.5")
            strValue = strValue.Replace(",", ".");
            if (double.TryParse(strValue, NumberStyles.Any, CultureInfo.InvariantCulture, out double pixels))
            {
                return new GridLength(pixels);
            }

            // Железобетонный дефолт на случай сбоя данных в файле настроек
            return new GridLength(150);
        }

        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is GridLength)
            {
                GridLength gl = (GridLength)value;

                if (gl.IsStar)
                {
                    return gl.Value.ToString(CultureInfo.InvariantCulture) + "*";
                }

                return gl.Value.ToString(CultureInfo.InvariantCulture);
            }

            return "150";
        }
    }
}
