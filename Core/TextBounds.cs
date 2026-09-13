using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;

namespace BaseFunction
{
    public static class TextBounds
    {
        public static Polyline CreatePolyline(this MText mTexta)
        {
            using (MText mText = mTexta.Clone() as MText)
            {
                // Доворот ПСК
                double csr = 0;
                if (Convert.ToInt32(Autodesk.AutoCAD.ApplicationServices.Application.GetSystemVariable("WORLDUCS")) == 0)
                {
                    using (Plane wPlane = new Plane())
                    {
                        Vector3d xAxT = Autodesk.AutoCAD.ApplicationServices.Application.
                            DocumentManager.MdiActiveDocument.Editor.CurrentUserCoordinateSystem.
                            CoordinateSystem3d.Xaxis.ProjectTo(wPlane.Normal, Vector3d.ZAxis);
                        Vector3d xAx = Vector3d.XAxis;
                        csr = Math.Acos((xAx.X * xAxT.X + xAx.Y * xAxT.Y) / (Math.Sqrt(xAx.X * xAx.X + xAx.Y * xAx.Y) * Math.Sqrt(xAxT.X * xAxT.X + xAxT.Y * xAxT.Y)));
                        if (double.IsNaN(csr)) csr = 0;
                    }
                }
                double rotation = mText.Rotation + csr;

                Point3d point = mText.Location;

                // Оборачиваем Plane в using для гарантированной очистки памяти
                using (Plane plane = new Plane(point, mText.Normal))
                {
                    Vector3d vx = plane.Normal.GetPerpendicularVector().TransformBy(Matrix3d.Rotation(rotation, plane.Normal, point));
                    Vector3d vy = vx.TransformBy(Matrix3d.Rotation(Math.PI / 2, plane.Normal, point));
                    double h = mText.ActualHeight;
                    double w = mText.ActualWidth;

                    // Получаем нижний левый угол текста
                    switch (mText.Attachment)
                    {
                        case AttachmentPoint.TopLeft:
                            point -= vy * h;
                            break;
                        case AttachmentPoint.MiddleCenter:
                            point -= (vy * h / 2 + vx * w / 2);
                            break;
                        case AttachmentPoint.TopCenter:
                            point -= (vy * h + vx * w / 2);
                            break;
                        case AttachmentPoint.TopRight:
                            point -= (vy * h + vx * w);
                            break;
                        case AttachmentPoint.MiddleLeft:
                            point -= vy * h / 2;
                            break;
                        case AttachmentPoint.MiddleRight:
                            point -= (vy * h / 2 + vx * w);
                            break;
                        case AttachmentPoint.BottomLeft:
                            break;
                        case AttachmentPoint.BottomCenter:
                            point -= vx * w / 2;
                            break;
                        case AttachmentPoint.BottomRight:
                            point -= vx * w;
                            break;
                    }

                    var points = new List<Point3d>
                    {
                        point,
                        (point + vx * w),
                        (point + vx * w + vy * h),
                        (point + vy * h),
                    };

                    // Используем наш новый метод из BaseCreateClass
                    return BaseCreateClass.CreatePolyline(points, true);
                }
            }
        }

        public static Polyline CreatePolyline(this DBText texta)
        {
            using (DBText text = texta.Clone() as DBText)
            {
                if (text.Bounds.HasValue)
                {
                    using (Plane textPlane = text.GetPlane())
                    {
                        if (!textPlane.Normal.IsEqualTo(Vector3d.ZAxis) && !textPlane.Normal.IsEqualTo(-Vector3d.ZAxis))
                        {
                            return BaseCreateClass.CreatePolyline(text.Bounds.Value);
                        }
                    }

                    double rot = text.Rotation;
                    if (rot == 0)
                    {
                        return BaseCreateClass.CreatePolyline(text.Bounds.Value);
                    }
                    else
                    {
                        text.TransformBy(Matrix3d.Rotation(-rot, Vector3d.ZAxis, text.Position));
                        Polyline poly = BaseCreateClass.CreatePolyline(text.Bounds.Value);
                        poly.TransformBy(Matrix3d.Rotation(rot, Vector3d.ZAxis, text.Position));
                        return poly;
                    }
                }
                else
                {
                    Extents3d ex = new Extents3d();
                    if (text.VerticalMode == TextVerticalMode.TextBase ||
                        text.HorizontalMode == TextHorizontalMode.TextLeft ||
                        text.HorizontalMode == TextHorizontalMode.TextAlign ||
                        text.HorizontalMode == TextHorizontalMode.TextFit)
                    {
                        ex.AddPoint(text.Position);
                    }
                    else
                    {
                        ex.AddPoint(text.AlignmentPoint);
                    }

                    return BaseCreateClass.CreatePolyline(ex);
                }
            }
        }
    }
}
