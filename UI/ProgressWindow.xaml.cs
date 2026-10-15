using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace Progress
{
    /// <summary>
    /// Логика взаимодействия для ProgressWindow.xaml
    /// </summary>
    public partial class ProgressWindow : Window
    {
        private readonly ProgressViewModel _viewModel;
        internal bool AllowClose { get; set; } = false;
        // --- Win32 константы для управления фокусом и перемещением ---
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_NOACTIVATE = 0x08000000;

        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HT_CAPTION = 0x2;

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        private static extern bool SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();
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

            // Внедряем стиль "Не активировать / без фокуса" при инициализации окна
            this.SourceInitialized += ProgressWindow_SourceInitialized;
        }
        private void MainStatusContainer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // Чтобы избежать бесконечного цикла разметки, проверяем разницу
            if (e.NewSize.Height > MainStatusContainer.MinHeight && Math.Abs(e.NewSize.Height - MainStatusContainer.MinHeight) > 1)
            {
                // Асинхронно обновляем размер, чтобы дать WPF завершить текущую отрисовку
                Dispatcher.BeginInvoke(new Action(() => {
                    MainStatusContainer.MinHeight = e.NewSize.Height;
                }), System.Windows.Threading.DispatcherPriority.Loaded);
            }
        }

        private void SubStatusContainer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.NewSize.Height > SubStatusContainer.MinHeight && Math.Abs(e.NewSize.Height - SubStatusContainer.MinHeight) > 1)
            {
                Dispatcher.BeginInvoke(new Action(() => {
                    SubStatusContainer.MinHeight = e.NewSize.Height;
                }), System.Windows.Threading.DispatcherPriority.Loaded);
            }
        }

        // --- Метод сброса памяти высоты (понадобится для "матрешки") ---
        internal void ResetMinHeight()
        {
            this.MinHeight = 0;
            MainStatusContainer.MinHeight = 0;
            SubStatusContainer.MinHeight = 0;
        }
        private void ProgressWindow_SourceInitialized(object sender, EventArgs e)
        {
            // Говорим Windows: это окно-призрак, никогда не отдавай ему фокус ввода
            var helper = new WindowInteropHelper(this);
            int exStyle = GetWindowLong(helper.Handle, GWL_EXSTYLE);
            SetWindowLong(helper.Handle, GWL_EXSTYLE, exStyle | WS_EX_NOACTIVATE);
        }
        private void ProgressWindow_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                // Заменяем стандартный DragMove() на безопасный Win32 API вызов.
                // Он перемещает окно на уровне ОС, не блокируя WPF-диспетчер фонового потока.
                var helper = new WindowInteropHelper(this);
                if (helper.Handle != IntPtr.Zero)
                {
                    ReleaseCapture();
                    SendMessage(helper.Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
                }
            }
        }
        private void ProgressWindow_Closing(object sender, CancelEventArgs e)
        {
            if (!AllowClose)
            {
                e.Cancel = true;
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
