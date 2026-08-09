using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace Progress
{
    /// <summary>
    /// Логика взаимодействия для ProgressWindow.xaml
    /// </summary>
    public partial class ProgressWindow : Window
    {
        private readonly ProgressViewModel _viewModel;
        internal bool AllowClose { get; set; } = false;

        public ProgressWindow(ProgressViewModel viewModel, Window owner)
        {
            InitializeComponent();
            this.Owner = owner;
            this.DataContext = viewModel;
            _viewModel = viewModel;

            this.Loaded += ProgressWindow_Loaded;
            this.Closed += ProgressWindow_Closed;
            this.Closing += ProgressWindow_Closing;

            // Подписываемся на нажатие мыши для перетаскивания окна
            this.MouseLeftButtonDown += ProgressWindow_MouseLeftButtonDown;
            // Подписываемся на изменение размеров текстовых контейнеров
            MainStatusContainer.SizeChanged += MainStatusContainer_SizeChanged;
            SubStatusContainer.SizeChanged += SubStatusContainer_SizeChanged;
        }
        private void MainStatusContainer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // Фиксируем максимальную высоту для основного текста
            if (e.NewSize.Height > MainStatusContainer.MinHeight)
            {
                MainStatusContainer.MinHeight = e.NewSize.Height;
            }
        }

        private void SubStatusContainer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // Фиксируем максимальную высоту для дополнительного текста
            if (e.NewSize.Height > SubStatusContainer.MinHeight)
            {
                SubStatusContainer.MinHeight = e.NewSize.Height;
            }
        }

        // --- Метод сброса памяти высоты (понадобится для "матрешки") ---
        internal void ResetMinHeight()
        {
            this.MinHeight = 0; // Временно разрешаем окну пересчитать высоту с нуля
        }

        private void ProgressWindow_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Защита: перетаскиваем только если левая кнопка мыши реально зажата
            if (e.ChangedButton == MouseButton.Left)
            {
                try
                {
                    // Встроенный метод WPF для инициализации перемещения окна мышью
                    this.DragMove();
                }
                catch { /* Защита на случай некорректного состояния окна */ }
            }
        }
        private void ProgressWindow_Closing(object sender, CancelEventArgs e)
        {
            if (!AllowClose)
            {
                e.Cancel = true; // Блокируем крестик и Alt+F4

                // Перенаправляем на команду Отмены во ViewModel
                if (_viewModel.CancelCommand.CanExecute(null))
                {
                    _viewModel.CancelCommand.Execute(null);
                }
            }
        }

        private void ProgressWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _viewModel.StartLiveUpdate();
        }

        private void ProgressWindow_Closed(object sender, EventArgs e)
        {
            _viewModel.StopLiveUpdate();
        }
    }
}
