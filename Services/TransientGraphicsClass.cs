using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using agi = Autodesk.AutoCAD.GraphicsInterface;

namespace BaseFunction
{
    public class SpEntity : IDisposable
    {
        private agi.TransientManager _transientManager;
        private Entity _entity = null;
        private bool _tDraw = false;
        private bool _visible = true;
        private bool _highlight = false;

        public SpEntity()
        {
            Initialize();
        }

        public SpEntity(Entity e)
        {
            Initialize();
            Entity = e;
        }

        private void Initialize()
        {
            _transientManager = agi.TransientManager.CurrentTransientManager;
        }

        public event EventHandler Changed;

        protected virtual void OnChange()
        {
            Changed?.Invoke(this, EventArgs.Empty);
            if (AutoRedraw) Redraw();
        }

        public void Redraw()
        {
            if (_entity == null) return;

            if (_tDraw)
            {
                _transientManager.EraseTransient(_entity, new IntegerCollection());
                _tDraw = false;
            }
            if (Visible)
            {
                _transientManager.AddTransient(_entity, DrawingMode, 128, new IntegerCollection());
                _tDraw = true;
            }
        }

        public agi.TransientDrawingMode DrawingMode { get; set; } = agi.TransientDrawingMode.DirectTopmost;
        public bool AutoRedraw { get; set; } = true;
        public bool Disposed { get; private set; } = false;

        // Сеттеры сделаны приватными для защиты от случайного изменения извне
        public Circle Circle { get; private set; } = null;
        public Curve Curve { get; private set; } = null;

        public Entity Entity
        {
            get => _entity;
            set
            {
                if (_entity == value) return;

                // Очищаем старый объект и графику
                if (_entity != null)
                {
                    if (_tDraw) _transientManager.EraseTransient(_entity, new IntegerCollection());
                    _entity.Dispose();
                }

                _entity = value;

                if (_entity == null)
                {
                    Circle = null;
                    Curve = null;
                }
                else
                {
                    // Безопасная валидация типа с учетом нашего IsAcadCurve метода расширения
                    // (Замените на вашу прямую проверку типов, если метод расширения в другом месте)
                    Circle = _entity as Circle;

                    // Проверяем, что это строго стандартная кривая ванильного AutoCAD (защита от Civil 3D)
                    Curve = _entity.IsAcadCurve() ? (_entity as Curve) : null;
                }

                OnChange();
            }
        }

        public Point3d? Center
        {
            get
            {
                if (Circle != null) return Circle.Center;
                if (_entity is Solid3d s) return s.MassProperties.Centroid;
                return null;
            }
            set
            {
                if (Circle != null && value.HasValue && !Circle.Center.IsEqualTo(value.Value))
                {
                    Circle.Center = value.Value;
                    OnChange();
                }
            }
        }

        public Point3d? StartPoint
        {
            get => Curve?.StartPoint; // Возвращает null, если кривой нет (вместо обманчивого Origin)
            set
            {
                if (Curve != null && value.HasValue && !Curve.StartPoint.IsEqualTo(value.Value))
                {
                    Curve.StartPoint = value.Value;
                    OnChange();
                }
            }
        }

        public Point3d? EndPoint
        {
            get => Curve?.EndPoint;
            set
            {
                if (Curve != null && value.HasValue && !Curve.EndPoint.IsEqualTo(value.Value))
                {
                    Curve.EndPoint = value.Value;
                    OnChange();
                }
            }
        }

        public int ColorIndex
        {
            get => _entity != null ? _entity.ColorIndex : 0;
            set
            {
                // Проверяем диапазон индексов ACI (0 - ByBlock, 256 - ByLayer)
                if (_entity != null && value >= 0 && value <= 256 && _entity.ColorIndex != value)
                {
                    try
                    {
                        _entity.ColorIndex = value;
                        OnChange();
                    }
                    catch
                    {
                        // Игнорируем ошибку, если AutoCAD не смог применить ByLayer/ByBlock к временному объекту
                    }
                }
            }
        }

        public double Radius
        {
            get => Circle != null ? Circle.Radius : double.NaN;
            set
            {
                if (Circle != null && value > 0 && !Circle.Radius.IsEqualTo(value))
                {
                    try
                    {
                        Circle.Radius = value;
                        OnChange();
                    }
                    catch { /* Игнорируем некорректную геометрию */ }
                }
            }
        }

        public bool Visible
        {
            get => _visible;
            set
            {
                if (_visible != value)
                {
                    _visible = value;
                    OnChange();
                }
            }
        }

        public bool HightLight
        {
            get => _entity != null && _highlight;
            set
            {
                if (_entity != null && _highlight != value)
                {
                    _highlight = value;
                    if (_highlight) _entity.Highlight();
                    else _entity.Unhighlight();
                    OnChange();
                }
            }
        }

        // --- Правильная реализация паттерна IDisposable ---

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this); // Запрещаем сборщику мусора вызывать деструктор, так как память уже очищена
        }

        protected virtual void Dispose(bool disposing)
        {
            if (Disposed) return;

            if (disposing)
            {
                // Очистка управляемых ресурсов (если они есть)
            }

            // Очистка неуправляемых ресурсов AutoCAD графики
            if (_entity != null)
            {
                if (_tDraw && _transientManager != null)
                    _transientManager.EraseTransient(_entity, new IntegerCollection());

                _entity.Dispose();
                _entity = null;
            }

            Circle = null;
            Curve = null;
            Disposed = true;
        }

        // Деструктор на случай, если разработчик забыл вызвать Dispose() вручную
        ~SpEntity()
        {
            Dispose(false);
        }
    }
}
