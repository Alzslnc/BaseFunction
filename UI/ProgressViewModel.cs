using BaseFunction;
using System;
using System.Windows.Input;
using System.Windows.Threading;

namespace Progress
{
    public class ProgressViewModel : BaseClass
    {
        // Сырые переменные для фоновых потоков
        public string RawMainStatus = "Загрузка...";
        public double RawMainProgress;
        public string RawSubStatus = "Подготовка...";
        public double RawSubProgress;

        // Базовые тексты, задаваемые при старте/рестарте
        public string BaseMainStatus = "";
        public string BaseSubStatus = "";

        // Счетчики шагов
        public int MainCurrentSteps { get; set; }
        public int MainTotalSteps { get; set; }
        public int SubCurrentSteps { get; set; }
        public int SubTotalSteps { get; set; }

        private bool _isMainCounterVisible = true;
        public bool IsMainCounterVisible { get => _isMainCounterVisible; set => SetData(ref _isMainCounterVisible, value); }

        private bool _isSubCounterVisible = true;
        public bool IsSubCounterVisible { get => _isSubCounterVisible; set => SetData(ref _isSubCounterVisible, value); }

        // Публичные свойства для привязки в XAML
        private string _mainStatus;
        public string MainStatus { get => _mainStatus; set => SetData(ref _mainStatus, value); }

        private double _mainProgress;
        public double MainProgress { get => _mainProgress; set => SetData(ref _mainProgress, value); }

        private string _subStatus;
        public string SubStatus { get => _subStatus; set => SetData(ref _subStatus, value); }

        private double _subProgress;
        public double SubProgress { get => _subProgress; set => SetData(ref _subProgress, value); }

        private bool _isSubProgressVisible = false;
        public bool IsSubProgressVisible { get => _isSubProgressVisible; set => SetData(ref _isSubProgressVisible, value); }

        private bool _isCancelButtonVisible = false;
        public bool IsCancelButtonVisible { get => _isCancelButtonVisible; set => SetData(ref _isCancelButtonVisible, value); }

        private bool _isSubProgressLinkedToMain = true;
        public bool IsSubProgressLinkedToMain { get => _isSubProgressLinkedToMain; set => SetData(ref _isSubProgressLinkedToMain, value); }

        public System.Threading.CancellationTokenSource Cts { get; } = new System.Threading.CancellationTokenSource();
        public ICommand CancelCommand { get; }

        public ProgressViewModel()
        {
            // Использует вашу реализацию RelayCommand из BaseFunction
            CancelCommand = new RelayCommand(
                execute: (param) => Cts.Cancel(),
                canExecute: (param) => !Cts.IsCancellationRequested
            );
        }

        // Объявляем переменную таймера
        private DispatcherTimer _uiRefreshTimer;

        public void StartLiveUpdate()
        {
            // Создаем таймер СТРОГО в потоке окна, где вызван этот метод (Loaded окна)
            _uiRefreshTimer = new DispatcherTimer(DispatcherPriority.Render)
            {
                Interval = TimeSpan.FromMilliseconds(33)
            };
            _uiRefreshTimer.Tick += UiRefreshTimer_Tick;
            _uiRefreshTimer.Start();
        }

        public void StopLiveUpdate()
        {
            if (_uiRefreshTimer != null)
            {
                _uiRefreshTimer.Stop();
                _uiRefreshTimer.Tick -= UiRefreshTimer_Tick;
            }
            FlushFinalValues();
        }

        // Множитель плавности (по умолчанию 1.0)
        // Значения > 1.0 (например, 2.0) ускорят движение ползунка (меньше сглаживания)
        // Значения < 1.0 (например, 0.5) сделают движение еще более тягучим и мягким
        private double _smoothnessMultiplier = 1.0;
        public double SmoothnessMultiplier
        {
            get => _smoothnessMultiplier;
            set { _smoothnessMultiplier = value; RecalculateMainLerp(); RecalculateSubLerp(); }
        }

        // Свойства для хранения динамических коэффициентов сглаживания
        private double _mainLerpFactor = 0.2;
        private double _subLerpFactor = 0.2;

        /// <summary>
        /// Автоматически пересчитывает скорость сглаживания для основного бара.
        /// Вызывается из ProgressScope при изменении TotalSteps.
        /// </summary>
        public void RecalculateMainLerp()
        {
            if (MainTotalSteps <= 0) { _mainLerpFactor = 1.0; return; }

            double baseLerp = 0.15 + (0.85 * (1.0 - Math.Exp(-0.015 * MainTotalSteps)));

            // Умножаем базовый коэффициент на внешний множитель
            _mainLerpFactor = baseLerp * SmoothnessMultiplier;

            // Жесткое ограничение, чтобы коэффициент не улетел выше 1.0 (мгновенно) или ниже 0.05 (зависание)
            _mainLerpFactor = Math.Max(0.05, Math.Min(1.0, _mainLerpFactor));
        }

        /// <summary>
        /// Автоматически пересчитывает скорость сглаживания для дочернего бара.
        /// </summary>
        public void RecalculateSubLerp()
        {
            if (SubTotalSteps <= 0) { _subLerpFactor = 1.0; return; }

            double baseLerp = 0.15 + (0.85 * (1.0 - Math.Exp(-0.015 * SubTotalSteps)));

            // Умножаем на внешний множитель
            _subLerpFactor = baseLerp * SmoothnessMultiplier;
            _subLerpFactor = Math.Max(0.05, Math.Min(1.0, _subLerpFactor));
        }

        private void UiRefreshTimer_Tick(object sender, EventArgs e)
        {
            // Тексты обновляем мгновенно
            MainStatus = RawMainStatus;
            SubStatus = RawSubStatus;

            // --- Динамический LERP для основного бара ---
            double mainDiff = RawMainProgress - MainProgress;
            if (Math.Abs(mainDiff) > 0.01)
                MainProgress += mainDiff * _mainLerpFactor; // Используем динамический коэффициент
            else
                MainProgress = RawMainProgress;

            // --- Динамический LERP для дочернего бара ---
            double subDiff = RawSubProgress - SubProgress;
            if (Math.Abs(subDiff) > 0.01)
                SubProgress += subDiff * _subLerpFactor;    // Используем динамический коэффициент
            else
                SubProgress = RawSubProgress;
        }

        public void FlushFinalValues()
        {
            MainStatus = RawMainStatus;
            SubStatus = RawSubStatus;

            // На финише жестко выставляем целевые значения, чтобы полоса заполнилась до конца
            MainProgress = RawMainProgress;
            SubProgress = RawSubProgress;
        }
    }
}
