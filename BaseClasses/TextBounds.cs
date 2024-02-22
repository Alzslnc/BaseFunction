using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;

namespace BaseFunction
{
    public static class TextBounds
    {
        public static Polyline CreatePolyline(MText mTexta)
        {
            using (MText mText = mTexta.Clone() as MText)
            {
                //доворот пск
                double csr = 0;
                if (System.Convert.ToInt32(Autodesk.AutoCAD.ApplicationServices.Application.GetSystemVariable("WORLDUCS")) == 0)
                {     
                    Vector3d xAxT = Autodesk.AutoCAD.ApplicationServices.Application.
                        DocumentManager.MdiActiveDocument.Editor.CurrentUserCoordinateSystem.
                        CoordinateSystem3d.Xaxis.ProjectTo(new Plane().Normal, Vector3d.ZAxis);
                    Vector3d xAx = Vector3d.XAxis;
                    csr = Math.Acos((xAx.X * xAxT.X + xAx.Y * xAxT.Y) / ( Math.Sqrt(xAx.X * xAx.X + xAx.Y * xAx.Y) * Math.Sqrt(xAxT.X * xAxT.X + xAxT.Y * xAxT.Y)));
                    if (csr == double.NaN) csr = 0;
                }
                double rotation = mText.Rotation + csr;

                Point3d point = mText.Location;
                Plane plane = new Plane(point, mText.Normal);
                Vector3d vx = plane.Normal.GetPerpendicularVector().TransformBy(Matrix3d.Rotation(rotation, plane.Normal, point));
                Vector3d vy = vx.TransformBy(Matrix3d.Rotation(Math.PI / 2, plane.Normal, point));
                double h = mText.ActualHeight;
                double w = mText.ActualWidth;
                //получаем нижний левый угол текста
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
                return CreatePolyline(new List<Point3d>
                { 
                    point,
                    (point + vx * w),
                    (point + vx * w + vy * h),
                    (point + vy * h),
                });              
            }
        }
        public static Polyline CreatePolyline(DBText texta)
        {
            using (DBText text = texta.Clone() as DBText)
            {
                if (text.Bounds.HasValue)
                {
                    if (!text.GetPlane().Normal.IsEqualTo(Vector3d.ZAxis) && !text.GetPlane().Normal.IsEqualTo(-Vector3d.ZAxis))
                    {
                        return CreatePolyline(text.Bounds.Value);
                    }
                    double rot = text.Rotation;
                    if (rot == 0) return CreatePolyline(text.Bounds.Value);
                    else
                    {
                        text.TransformBy(Matrix3d.Rotation(-rot, Vector3d.ZAxis, text.Position));
                        Polyline poly = CreatePolyline(text.Bounds.Value);
                        poly.TransformBy(Matrix3d.Rotation(rot, Vector3d.ZAxis, text.Position));
                        return poly;
                    }
                }
                else
                {
                    Extents3d ex = new Extents3d();
                    if (
                        text.VerticalMode == TextVerticalMode.TextBase ||
                        text.HorizontalMode == TextHorizontalMode.TextLeft ||
                        text.HorizontalMode == TextHorizontalMode.TextAlign ||
                        text.HorizontalMode == TextHorizontalMode.TextFit
                        ) ex.AddPoint(text.Position);
                    else ex.AddPoint(text.AlignmentPoint);
                    return CreatePolyline(ex);
                }
            }
        }
        public static Polyline CreatePolyline(Extents3d ex)
        {
            return CreatePolyline(new List<Point3d>
            {
                ex.MinPoint,
                new Point3d(ex.MinPoint.X, ex.MaxPoint.Y, 0),
                ex.MaxPoint,
                new Point3d(ex.MaxPoint.X, ex.MinPoint.Y, 0)
            });
        }
        private static Polyline CreatePolyline(List<Point3d> points)
        {
            Polyline poly = new Polyline();
            int i = 0;
            foreach (Point3d point in points)
            {
                poly.AddVertexAt(i++, new Point2d(point.X, point.Y), 0, 0, 0);
            }
            poly.Closed = true;
            return poly;
        }
    }
}
