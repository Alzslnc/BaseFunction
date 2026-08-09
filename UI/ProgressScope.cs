using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Windows.Threading;

namespace Progress
{
    public class ProgressScope : IDisposable
    {
        // Внутренний контейнер для снимка состояния ("матрешки")
        private class ProgressState
        {
            public string BaseMainStatus { get; set; }
            public string BaseSubStatus { get; set; }
            public int MainCurrentSteps { get; set; }
            public int MainTotalSteps { get; set; }
            public int SubCurrentSteps { get; set; }
            public int SubTotalSteps { get; set; }
            public double RawMainProgress { get; set; }
            public double RawSubProgress { get; set; }
            public bool IsMainCounterVisible { get; set; }
            public bool IsSubCounterVisible { get; set; }
            public bool IsSubProgressVisible { get; set; }
            public bool IsSubProgressLinkedToMain { get; set; }
        }

        private readonly Stack<ProgressState> _statesStack = new Stack<ProgressState>();
        private Thread _progressThread;
        private ProgressWindow _window;
        private bool _isDisposed;

        public ProgressViewModel ViewModel { get; }
        public CancellationToken CancellationToken => ViewModel.Cts.Token;

        /// <summary>
        /// Конструктор области прогресса.
        /// </summary>
        /// <param name="smoothnessMultiplier">Множитель скорости сглаживания. 
        /// 1.0 — стандартно. 
        /// Больше 1.0 (например, 2.5) — ускоряет ползунок для медленных задач. 
        /// Меньше 1.0 (например, 0.5) — делает анимацию еще плавнее для сверхбыстрых циклов.</param>
        public ProgressScope(bool showSubProgress = true, bool showCancelButton = true, double smoothnessMultiplier = 1.0)
        {
            ViewModel = new ProgressViewModel
            {
                IsSubProgressVisible = showSubProgress,
                IsCancelButtonVisible = showCancelButton,
                SmoothnessMultiplier = smoothnessMultiplier // Применяем коэффициент
            };
        }

        public void Start(string initialMainStatus = "Инициализация...", IntPtr? ownerWindowHandle = null)
        {
            if (_progressThread != null) throw new InvalidOperationException("Прогресс-бар уже запущен.");
            ViewModel.RawMainStatus = initialMainStatus;

            using (var windowCreatedEvent = new ManualResetEvent(false))
            {
                _progressThread = new Thread(() =>
                {
                    _window = new ProgressWindow(ViewModel, null);

                    if (ownerWindowHandle.HasValue && ownerWindowHandle.Value != IntPtr.Zero)
                    {
                        try
                        {
                            var interopHelper = new System.Windows.Interop.WindowInteropHelper(_window);
                            interopHelper.Owner = ownerWindowHandle.Value;
                        }
                        catch { /* Игнорируем ошибки interop в окружениях без окон */ }
                    }

                    _window.Show();
                    windowCreatedEvent.Set();
                    Dispatcher.Run();
                });

                _progressThread.SetApartmentState(ApartmentState.STA);
                _progressThread.IsBackground = true;
                _progressThread.Start();
                windowCreatedEvent.WaitOne();
            }
        }

        public void PushState()
        {
            var state = new ProgressState
            {
                BaseMainStatus = ViewModel.BaseMainStatus,
                BaseSubStatus = ViewModel.BaseSubStatus,
                MainCurrentSteps = ViewModel.MainCurrentSteps,
                MainTotalSteps = ViewModel.MainTotalSteps,
                SubCurrentSteps = ViewModel.SubCurrentSteps,
                SubTotalSteps = ViewModel.SubTotalSteps,
                RawMainProgress = ViewModel.RawMainProgress,
                RawSubProgress = ViewModel.RawSubProgress,
                IsMainCounterVisible = ViewModel.IsMainCounterVisible,
                IsSubCounterVisible = ViewModel.IsSubCounterVisible,
                IsSubProgressVisible = ViewModel.IsSubProgressVisible,
                IsSubProgressLinkedToMain = ViewModel.IsSubProgressLinkedToMain
            };
            _statesStack.Push(state);
        }

        public void PopState()
        {
            if (_statesStack.Count == 0) return;
            var state = _statesStack.Pop();

            // Сбрасываем память высоты окна перед новой задачей, чтобы оно могло адаптироваться
            if (_window != null) _window.Dispatcher.Invoke(() => _window.ResetMinHeight());

            ViewModel.BaseMainStatus = state.BaseMainStatus;
            ViewModel.BaseSubStatus = state.BaseSubStatus;
            ViewModel.MainCurrentSteps = state.MainCurrentSteps;
            ViewModel.MainTotalSteps = state.MainTotalSteps;
            ViewModel.SubCurrentSteps = state.SubCurrentSteps;
            ViewModel.SubTotalSteps = state.SubTotalSteps;
            ViewModel.RawMainProgress = state.RawMainProgress;
            ViewModel.RawSubProgress = state.RawSubProgress;
            ViewModel.IsMainCounterVisible = state.IsMainCounterVisible;
            ViewModel.IsSubCounterVisible = state.IsSubCounterVisible;
            ViewModel.IsSubProgressVisible = state.IsSubProgressVisible;
            ViewModel.IsSubProgressLinkedToMain = state.IsSubProgressLinkedToMain;

            // ВАЖНО: Восстанавливаем коэффициенты LERP для возвращенного состояния
            ViewModel.RecalculateMainLerp();
            ViewModel.RecalculateSubLerp();

            RefreshProgressAndTexts();
        }

        public void RestartMain(string message, int totalSteps, bool showCounter = true)
        {           
            ViewModel.BaseMainStatus = message;
            ViewModel.MainTotalSteps = totalSteps;
            ViewModel.MainCurrentSteps = 0;
            ViewModel.RawMainProgress = 0;
            ViewModel.IsMainCounterVisible = showCounter;

            // ВАЖНО: Пересчитываем скорость сглаживания под новое число шагов!
            ViewModel.RecalculateMainLerp();

            RefreshProgressAndTexts();
        }

        public void RestartSub(string message, int totalSteps, bool showCounter = true, bool linkToMain = false)
        {
            ViewModel.BaseSubStatus = message;
            ViewModel.SubTotalSteps = totalSteps;
            ViewModel.SubCurrentSteps = 0;
            ViewModel.RawSubProgress = 0;
            ViewModel.IsSubCounterVisible = showCounter;
            ViewModel.IsSubProgressLinkedToMain = linkToMain;

            // ВАЖНО: Пересчитываем скорость сглаживания под новое число шагов!
            ViewModel.RecalculateSubLerp();

            RefreshProgressAndTexts();
        }

        /// <summary>
        /// Прибавляет 1 к текущему шагу основного бара.
        /// </summary>
        public void IncrementMainStep()
        {
            ViewModel.MainCurrentSteps++;
            RefreshProgressAndTexts();
        }

        /// <summary>
        /// Прибавляет 1 к текущему шагу дополнительного бара.
        /// </summary>
        public void IncrementSubStep()
        {
            ViewModel.SubCurrentSteps++;
            RefreshProgressAndTexts();
        }
              

        /// <summary>
        /// Единый метод для одновременного пересчета процентов и обновления текстовых статусов
        /// </summary>
        public void RefreshProgressAndTexts()
        {
            // 1. Расчет и обновление под-бара (проценты + текст)
            double subPercent = ViewModel.SubTotalSteps > 0
                ? (double)ViewModel.SubCurrentSteps / ViewModel.SubTotalSteps * 100
                : 0;
            ViewModel.RawSubProgress = Math.Min(subPercent, 100);

            if (ViewModel.IsSubCounterVisible && ViewModel.SubTotalSteps > 0)
                ViewModel.RawSubStatus = $"{ViewModel.BaseSubStatus}{Environment.NewLine}({ViewModel.SubCurrentSteps} из {ViewModel.SubTotalSteps})";
            else
                ViewModel.RawSubStatus = ViewModel.BaseSubStatus;

            // 2. Расчет основного бара с учетом привязки к под-бару
            double mainBasePercent = ViewModel.MainTotalSteps > 0
                ? (double)ViewModel.MainCurrentSteps / ViewModel.MainTotalSteps * 100
                : 0;

            if (ViewModel.IsSubProgressLinkedToMain && ViewModel.MainTotalSteps > 0 && ViewModel.SubTotalSteps > 0)
            {
                double oneMainStepWeight = 100.0 / ViewModel.MainTotalSteps;
                double linkedBonus = (ViewModel.RawSubProgress / 100.0) * oneMainStepWeight;
                ViewModel.RawMainProgress = Math.Min(mainBasePercent + linkedBonus, 100);
            }
            else
            {
                ViewModel.RawMainProgress = Math.Min(mainBasePercent, 100);
            }

            // 3. Обновление текста основного бара
            if (ViewModel.IsMainCounterVisible && ViewModel.MainTotalSteps > 0)
                ViewModel.RawMainStatus = $"{ViewModel.BaseMainStatus}{Environment.NewLine}({ViewModel.MainCurrentSteps} из {ViewModel.MainTotalSteps})";
            else
                ViewModel.RawMainStatus = ViewModel.BaseMainStatus;
        }

        /// <summary>
        /// Вручную устанавливает любые параметры шагов и статусов, после чего мгновенно пересчитывает прогресс.
        /// Передавайте только те аргументы, которые нужно изменить.
        /// </summary>
        public void Update(
            int? mainCurrent = null,
            int? mainTotal = null,
            string mainMessage = null,
            int? subCurrent = null,
            int? subTotal = null,
            string subMessage = null)
        {
            // Обновление основного прогресс-бара
            if (mainTotal.HasValue)
            {
                ViewModel.MainTotalSteps = mainTotal.Value;
                ViewModel.RecalculateMainLerp(); // Пересчитываем если изменился лимит
            }
            if (mainCurrent.HasValue) ViewModel.MainCurrentSteps = mainCurrent.Value;
            if (mainMessage != null) ViewModel.BaseMainStatus = mainMessage;

            // Обновление дополнительного прогресс-бара
            if (subTotal.HasValue) 
            {
                ViewModel.SubTotalSteps = subTotal.Value;
                ViewModel.RecalculateSubLerp();  // Пересчитываем если изменился лимит
            }
            if (subCurrent.HasValue) ViewModel.SubCurrentSteps = subCurrent.Value;
            if (subMessage != null) ViewModel.BaseSubStatus = subMessage;

            // Мгновенный пересчет и обновление строковых переменных
            RefreshProgressAndTexts();
        }

        public void Stop() => Dispose();

        public void Dispose()
        {
            if (_isDisposed) return;
            if (_window != null && _window.Dispatcher.HasShutdownStarted == false)
            {
                ViewModel.FlushFinalValues();
                _window.Dispatcher.Invoke(() =>
                {
                    _window.AllowClose = true;
                    _window.Close();
                    Dispatcher.CurrentDispatcher.BeginInvokeShutdown(DispatcherPriority.Background);
                });
            }
            _isDisposed = true;
            GC.SuppressFinalize(this);
        }
    }
}
