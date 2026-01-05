using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using agi = Autodesk.AutoCAD.GraphicsInterface;

namespace BaseFunction
{
    public class SpEntity : IDisposable
    {
        public SpEntity()
        {
            Ini();
        }
        public SpEntity(Entity e)
        {
            Ini();
            Entity = e;
        }

        private void Ini()
        {
            TransientManager = agi.TransientManager.CurrentTransientManager;
        }

        public event EventHandler Changed;
        public void OnChange(object sender, EventArgs e)
        {
            Changed?.Invoke(this, EventArgs.Empty);
            if (AutoRedraw) Redraw();            
        }
        public void Redraw()
        {
            if (Entity != null)
            {
                if (TDraw)
                {
                    TransientManager.EraseTransient(Entity, new IntegerCollection());
                    TDraw = false;
                }
                if (Visible)
                {
                    TransientManager.AddTransient(Entity, DrawingMode, 128, new IntegerCollection());
                    TDraw = true;
                }
            }
        }


        public agi.TransientDrawingMode DrawingMode { get; set; } = agi.TransientDrawingMode.DirectTopmost;
        public Point3d? Center
        {
            get
            {
                if (Circle != null) return Circle.Center;
                else if (Entity is Solid3d s) return s.MassProperties.Centroid;
                else return null;
            }
            set
            {
                if (Circle != null && value.HasValue && !Circle.Center.IsEqualTo(value.Value))
                {
                    Circle.Center = value.Value;
                    OnChange(this, EventArgs.Empty);
                }
            }
        }
        public Point3d? StartPoint
        {
            get
            {
                if (Curve != null) return Curve.StartPoint;
                else return Point3d.Origin;
            }
            set
            {
                if (Curve != null && value.HasValue && !Curve.StartPoint.IsEqualTo(value.Value))
                {
                    Curve.StartPoint = value.Value;
                    OnChange(this, EventArgs.Empty);
                }
            }
        }
        public Point3d? EndPoint
        {
            get
            {
                if (Curve != null) return Curve.EndPoint;
                else return Point3d.Origin;
            }
            set
            {
                if (Curve != null && value.HasValue && !Curve.EndPoint.IsEqualTo(value.Value))
                {
                    Curve.EndPoint = value.Value;
                    OnChange(this, EventArgs.Empty);
                }
            }
        }
        public int ColorIndex
        {
            get
            {
                if (Entity != null) return Entity.ColorIndex;
                else return 0;
            }
            set
            {
                if (Entity != null && Entity.ColorIndex != value)
                {
                    Entity.ColorIndex = value;
                    OnChange(this, EventArgs.Empty);
                }
            }
        }
        public double Radius
        {
            get
            {
                if (Circle != null) return Circle.Radius;
                else return double.NaN;
            }
            set
            {
                if (Circle != null && value > 0  && !Circle.Radius.IsEqualTo(value))
                {
                    try
                    {
                        Circle.Radius = value;
                        OnChange(this, EventArgs.Empty);
                    }
                    catch { }
                }
            }
        }
        public bool Visible
        {
            get
            {
                return _Visible;
            }
            set
            {
                if (_Visible != value)
                {
                    _Visible = value;
                    OnChange(this, EventArgs.Empty);
                }
            }
        }
        private bool _Visible = true;
        public Entity Entity
        {
            get { return _Entity; }
            set
            {
                if (_Entity != value)
                {
                    
                    if (_Entity != null)
                    {
                        if (TDraw) TransientManager.EraseTransient(Entity, new IntegerCollection());
                        _Entity.Dispose();
                    }
                    
                    _Entity = value;
                    if (_Entity == null)
                    {
                        Entity?.Dispose();
                        Circle = null;
                        Curve = null;
                    }
                    else
                    {
                        if (_Entity is Circle c) Circle = c;
                        if (_Entity is Curve cur) Curve = cur;
                    }
                    OnChange(this, EventArgs.Empty);
                }
            }

        }
        private Entity _Entity = null;
        public bool AutoRedraw { get; set; } = true;
        public Circle Circle { get; set; } = null;
        public Curve Curve { get; set; } = null;
        public bool HightLight
        {
            get
            {
                if (Entity != null) return _HightLight;
                else return false;
            }
            set
            {
                if (Entity != null && _HightLight != value)
                {
                    _HightLight = value;
                    if (_HightLight) Entity.Highlight();
                    else Entity.Unhighlight();
                    OnChange(this, EventArgs.Empty);
                }
            }
        }
        private bool _HightLight = false;
        public bool Disposed { get; private set; } = false;



        private agi.TransientManager TransientManager { get; set; } = null;
        private bool TDraw { get; set; } = false;
        public void Dispose()
        {
            if (Disposed) return;
            Disposed = true;

            if (Entity != null)
            {
                if (TDraw) TransientManager.EraseTransient(Entity, new IntegerCollection());
                Entity?.Dispose();
                Entity = null;
            }
        }
    }
}
