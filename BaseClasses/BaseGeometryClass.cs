using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace BaseFunction
{
    public static class BaseGeometryClass
    {
        /// <summary>
        /// добавляет в список новую точку, если она в нем отсутствовала ранее
        /// </summary>
        public static void AddNewPoint(this List<Point3d> points, Point3d point)
        {
            if (!points.ContainPoint(point)) points.Add(point);
        }
        public enum ExPosition
        {
            none,
            Inner,
            Outer,
            Intersect,
            Surround
        }
        /// <summary>
        /// определяет положение первой области относительно второй
        /// </summary>
        /// <param name="ex1"></param>
        /// <param name="ex2"></param>
        /// <returns></returns>
        public static ExPosition GetExPosition(this Extents3d ex1, Extents3d ex2)
        {
            ExPosition result;
            //снаружи второго
            if (
                ex1.MaxPoint.X < ex2.MinPoint.X ||
                ex1.MaxPoint.Y < ex2.MinPoint.Y ||
                ex1.MinPoint.X > ex2.MaxPoint.X ||
                ex1.MinPoint.Y > ex2.MaxPoint.Y
                ) result = ExPosition.Outer;
            //внутри второго
            else if (
                ex1.MaxPoint.X < ex2.MaxPoint.X &&
                ex1.MinPoint.X > ex2.MinPoint.X &&
                ex1.MaxPoint.Y < ex2.MaxPoint.Y &&
                ex1.MinPoint.Y > ex2.MinPoint.Y
                ) result = ExPosition.Inner;
            //окружает второй
            else if (
                ex1.MinPoint.X < ex2.MinPoint.X &&
                ex1.MaxPoint.X > ex2.MaxPoint.X &&
                ex1.MinPoint.Y < ex2.MinPoint.Y &&
                ex1.MaxPoint.Y > ex2.MaxPoint.Y
                ) result = ExPosition.Surround;
            //иначе пересекаются
            else result = ExPosition.Intersect;
            return result;
        }
        public static int GetAfterPointNumber(this string s)
        {
            s = s.Replace(",", ".");
            if (!s.Contains(".")) return 0;
            return s.Length - 1 - s.IndexOf(".");
        }
        public static Polyline GetClearPolyline(this Polyline poly, Tolerance? tolerance = null, bool reverse = true)
        {
            if (!tolerance.HasValue) tolerance = new Tolerance(Tolerance.Global.EqualPoint, Tolerance.Global.EqualVector);
            for (int i = poly.NumberOfVertices - 2; i > 0; i--)
            {
                Point3d nextPt = poly.GetPoint3dAt(i + 1);
                Point3d previousPt = poly.GetPoint3dAt(i);

                if (nextPt.IsEqualTo(previousPt))
                {
                    poly.RemoveVertexAt(i);
                    continue;
                }

                Vector3d next = (nextPt - previousPt).GetNormal();
                Vector3d previous = (poly.GetPoint3dAt(i - 1) - poly.GetPoint3dAt(i)).GetNormal();
                if ((next.IsEqualTo(previous, tolerance.Value) && reverse) || next.IsEqualTo(-previous, tolerance.Value)) poly.RemoveVertexAt(i);
            }
            return poly;
        }
        public static Polyline3d GetClearPolyline(this Polyline3d poly, Transaction tr, Tolerance? tolerance = null, bool reverse = true)
        {
            if (!tolerance.HasValue) tolerance = new Tolerance(Tolerance.Global.EqualPoint, Tolerance.Global.EqualVector);

            List<PolylineVertex3d> vertices = new List<PolylineVertex3d>();

            Transaction transaction;

            if (tr == null)
            {
                transaction = HostApplicationServices.WorkingDatabase.TransactionManager.StartTransaction();
            }
            else transaction = tr;

            foreach (object o in poly)
            {
                if (o is PolylineVertex3d v) vertices.Add(v);
                else if (o is ObjectId id && transaction.GetObject(id, OpenMode.ForWrite, false, true) is PolylineVertex3d v2) vertices.Add(v2);
            }

            for (int i = vertices.Count - 2; i > 0; i--)
            {
                Point3d nextPt = vertices[i + 1].Position;
                Point3d previousPt = vertices[i].Position;

                if (nextPt.IsEqualTo(previousPt))
                {
                    vertices[i].Erase();
                    vertices.RemoveAt(i);
                    continue;
                }

                Vector3d next = (nextPt - previousPt).GetNormal();
                Vector3d previous = (vertices[i - 1].Position - vertices[i].Position).GetNormal();
                if ((next.IsEqualTo(previous, tolerance.Value) && reverse) || next.IsEqualTo(-previous, tolerance.Value))
                {
                    vertices[i].Erase();
                    vertices.RemoveAt(i);
                }
            }

            if (tr == null)
            {
                transaction.Commit();
                transaction.Dispose();
            }

            return poly;
        }

        /// <summary>
        /// очищает список от дублирующихся точек
        /// </summary>
        public static List<Point3d> ClearFromDoubles(this List<Point3d> points)
        {
            List<Point3d> result = new List<Point3d>();
            foreach (Point3d point in points)
            {
                bool none = true;
                foreach (Point3d newPoint in result)
                {
                    if (newPoint.IsEqualTo(point))
                    {
                        none = false;
                        break;
                    }
                }
                if (none) result.Add(point);
            }
            return result;
        }
        /// <summary>
        /// очищает список от дублирующихся точек
        /// </summary>
        public static List<double> ClearFromDoubles(this List<double> points)
        {
            List<double> result = new List<double>();
            foreach (double point in points)
            {
                bool none = true;
                foreach (double newPoint in result)
                {
                    if (newPoint.IsEqualTo(point))
                    {
                        none = false;
                        break;
                    }
                }
                if (none) result.Add(point);
            }
            return result;
        }

        /// <summary>
        /// соединяет фрагменты кривой и возвращает список с результатом соединения. Возвращает false если произошла ошибка.
        /// </summary>   
        public static bool ConnectCurve(this List<Curve> fragments, out List<Curve> result, double tolerance = 1e-6)
        {
            Tolerance tl = new Tolerance(tolerance / 10, tolerance);

            result = new List<Curve>();

            for (int i = fragments.Count - 1; i >= 0; i--)
            {
                if (fragments[i] is Circle || fragments[i].Closed || fragments[i].StartPoint.IsEqualTo(fragments[i].EndPoint))
                {
                    result.Add(fragments[i]);
                    fragments.RemoveAt(i);
                }
            }

            while (fragments.Count > 0)
            {
                Curve contour = null;

                foreach (Curve c in fragments)
                {
                    if (c is Spline)
                    {
                        contour = c;
                        break;
                    }
                }

                if (contour == null)
                {
                    contour = fragments[0];
                }

                fragments.Remove(contour);

                if (contour is Arc arc)
                {
                    Polyline polyline = new Polyline();
                    polyline.AddVertexAt(0, new Point2d(arc.StartPoint.X, arc.StartPoint.Y), arc.GetArcBulge(), 0, 0);
                    polyline.AddVertexAt(1, new Point2d(arc.EndPoint.X, arc.EndPoint.Y), 0, 0, 0);
                    polyline.Normal = arc.Normal;
                    polyline.EntityCopySettings(arc);
                    contour = polyline;
                }

                if (contour is Line line)
                {
                    Polyline polyline = new Polyline();
                    polyline.AddVertexAt(0, new Point2d(line.StartPoint.X, line.StartPoint.Y), 0, 0, 0);
                    polyline.AddVertexAt(1, new Point2d(line.EndPoint.X, line.EndPoint.Y), 0, 0, 0);
                    polyline.Normal = line.Normal;
                    polyline.EntityCopySettings(line);
                    contour = polyline;
                }

                while (!contour.StartPoint.IsEqualTo(contour.EndPoint))
                {
                    bool stop = true;
                    for (int i = fragments.Count - 1; i >= 0; i--)
                    {
                        Curve fragment = fragments[i];

                        try
                        {
                            if (fragment.StartPoint.IsEqualTo(contour.EndPoint) || fragment.EndPoint.IsEqualTo(contour.EndPoint) ||
                                fragment.StartPoint.IsEqualTo(contour.StartPoint) || fragment.EndPoint.IsEqualTo(contour.StartPoint))
                            {
                                contour.JoinEntity(fragment);
                                fragments.RemoveAt(i);
                                stop = false;
                            }
                        }
                        catch
                        {
                            return false;
                        }
                    }
                    if (stop) break;
                }
                result.Add(contour);
            }
            return true;
        }
        /// <summary>
        /// Проверяет, содержит ли список точку
        /// </summary>
        /// <param name="points"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        public static bool ContainPoint(this List<Point3d> points, Point3d point, Tolerance? tolerance = null)
        {
            foreach (Point3d p in points)
            {
                if (tolerance.HasValue ? p.IsEqualTo(point, tolerance.Value) : p.IsEqualTo(point)) return true;
            }
            return false;
        }
        public static Polyline Convert3dPolylineToPolyline(this Polyline3d pline, Transaction tr, bool erase)
        {
            if (pline == null) return null;

            Polyline polyline = new Polyline();
            //счетчик вершин полилинии
            int i = 0;
            //считываем вершины из 3д полилинии в список

            foreach (object o in pline)
            {
                PolylineVertex3d vertex3D;
                if (o is ObjectId id)
                {

                    if (tr != null) vertex3D = tr.GetObject(id, OpenMode.ForRead) as PolylineVertex3d;
                    else vertex3D = id.Open(OpenMode.ForRead) as PolylineVertex3d;
                }
                else if (o is PolylineVertex3d) vertex3D = o as PolylineVertex3d;
                else continue;

                if (vertex3D != null)
                {
                    //добавляем вершину полилинии
                    polyline.AddVertexAt(i++, vertex3D.Position.GetPoint2d(), 0, 0, 0);
                }
            }
            //если вершин меньше 2 то возвращаем null
            if (polyline.NumberOfVertices < 2) return null;
            //пеоеносим параметры
            polyline.Closed = pline.Closed;
            polyline.Layer = pline.Layer;
            polyline.Linetype = pline.Linetype;
            polyline.LineWeight = pline.LineWeight;
            polyline.Color = pline.Color;
            //удаляем 3д полилинию если надо
            if (erase)
            {
                if (pline.IsReadEnabled) pline.UpgradeOpen();
                pline.Erase();
            }

            return polyline;
        }
        public static Polyline Convert3dPolylineToPolyline(ObjectId id, bool erase)
        {
            Polyline polyline;
            using (Transaction tr = HostApplicationServices.WorkingDatabase.TransactionManager.StartTransaction())
            {
                using (Polyline3d pline = tr.GetObject(id, OpenMode.ForRead) as Polyline3d)
                {
                    polyline = Convert3dPolylineToPolyline(pline, tr, erase);
                }
                tr.Commit();
            }
            return polyline;
        }
        /// <summary>
        /// Возвращает матрицу преобразования объетов модели в выбранный видовой экран
        /// </summary>
        /// <returns></returns>
        public static Matrix3d ConvertToViewport(this Viewport viewport)
        {
            Matrix3d matrix =
            Matrix3d.Scaling(1 / viewport.CustomScale, viewport.CenterPoint).PreMultiplyBy
            (Matrix3d.Displacement(viewport.ViewCenter.GetPoint3d(0) - viewport.CenterPoint)).PreMultiplyBy
            (Matrix3d.PlaneToWorld(viewport.ViewDirection)).PreMultiplyBy
            (Matrix3d.Displacement(viewport.ViewTarget - Point3d.Origin)).PreMultiplyBy
            (Matrix3d.Rotation(-viewport.TwistAngle, viewport.ViewDirection, viewport.ViewTarget));
            return matrix.Inverse();
        }

        /// <summary>
        /// создает замкнутую полилинию по границе
        /// </summary>     
        public static Polyline CreatePolylineFromExtents(this Extents3d ex)
        {
            return CreatePolylineFromPointList(new List<Point3d>
            {
                ex.MinPoint,
                new Point3d(ex.MinPoint.X, ex.MaxPoint.Y, 0),
                ex.MaxPoint,
                new Point3d(ex.MaxPoint.X, ex.MinPoint.Y, 0)
            });
        }
        /// <summary>
        /// Создает замкнутую полилинию по списку точек
        /// </summary>   
        public static Polyline CreatePolylineFromPointList(this List<Point3d> points, bool closed = true)
        {
            Polyline poly = new Polyline();
            int i = 0;
            foreach (Point3d point in points)
            {
                poly.AddVertexAt(i++, new Point2d(point.X, point.Y), 0, 0, 0);
            }
            poly.Closed = closed;
            return poly;
        }
        /// <summary>
        /// Получает точку положения текста в зависимости от выравнивания
        /// </summary>
        public static Point3d DBTextPositionGet(this DBText dBText)
        {
            Point3d result;
            if (
                (dBText.HorizontalMode == TextHorizontalMode.TextLeft ||
                dBText.HorizontalMode == TextHorizontalMode.TextAlign ||
                dBText.HorizontalMode == TextHorizontalMode.TextFit) &&
                dBText.VerticalMode == TextVerticalMode.TextBase
               ) result = dBText.Position;
            else result = dBText.AlignmentPoint;
            return result;
        }
        /// <summary>
        /// Выставляет положение текста в зависимости от выравнивания
        /// </summary>
        public static bool DBTextPositionSet(this DBText dBText, Point3d newPosition)
        {
            if (!dBText.IsNewObject && !dBText.IsWriteEnabled) return false;
            try
            {
                if (dBText.VerticalMode == TextVerticalMode.TextBase ||
                dBText.HorizontalMode == TextHorizontalMode.TextLeft ||
                dBText.HorizontalMode == TextHorizontalMode.TextAlign ||
                dBText.HorizontalMode == TextHorizontalMode.TextFit
                ) dBText.Position = newPosition;
                else dBText.AlignmentPoint = newPosition;
                return true;
            }
            catch { return false; }
        }
        /// <summary>
        /// Удаляет из списка точки эквивалентные данной
        /// </summary>  
        public static void DeletePoint(this List<Point3d> points, Point3d point)
        {
            for (int i = points.Count - 1; i >= 0; i--)
            {
                if (points[i].IsEqualTo(point)) points.RemoveAt(i);
            }
        }
        /// <summary>
        /// возвращает число, округленное до выбранного значения
        /// </summary>
        /// <param name="d">исходное число</param>
        /// <param name="round">значение, до которого округляем в виде числа или текста</param>
        /// <returns>округленное число если получилось округлить или исходное</returns>
        public static double DRound(this double d, object round)
        {
            //если округление задано строкой то пробуем парсить строку и округлять
            if (round is string @string)
            {
                if (double.TryParse(@string, out double rou)) return rou *= Math.Round(d /= rou);
            }
            //округляем если округление задано числом
            else if (round is double @double)
            {
                return @double *= Math.Round(d /= @double);
            }
            return d;
        }
        /// <summary>
        /// копирует свойства одного объекта другому
        /// </summary>
        /// <param name="e"></param>
        /// <param name="ie"></param>
        public static void EntityCopySettings(this Entity e, Entity ie)
        {
            try
            {
                e.Color = ie.Color;
            }
            catch { }
            try
            {
                e.Linetype = ie.Linetype;
            }
            catch { }
            try
            {
                e.LineWeight = ie.LineWeight;
            }
            catch { }
            try
            {
                e.Layer = ie.Layer;
            }
            catch { }
            try
            {
                e.LinetypeScale = ie.LinetypeScale;
            }
            catch { }
            try
            {
                e.Transparency = ie.Transparency;
            }
            catch { }
        }
        /// <summary>
        /// возвращает угол от оси X Autocad из точки pt1 на точку pt2 (аналог polar из лиспа)
        /// </summary>
        /// <param name="pt1"></param>
        /// <param name="pt2"></param>
        /// <returns>угол в радианах</returns>
        public static double GetAcadAngle(this Point3d pt1, Point3d pt2)
        {
            double angle = Math.Atan2(pt2.Y - pt1.Y, pt2.X - pt1.X);
            if (angle < 0) angle += Math.PI * 2;
            return angle;
        }
        /// <summary>
        /// возвращает угол от оси X Autocad из точки pt1 на точку pt2 (аналог polar из лиспа)
        /// </summary>
        /// <param name="pt1"></param>
        /// <param name="pt2"></param>
        /// <returns>угол в радианах</returns>
        public static double GetAcadAngle(this Point2d pt1, Point2d pt2)
        {
            double angle = Math.Atan2(pt2.Y - pt1.Y, pt2.X - pt1.X);
            if (angle < 0) angle += Math.PI * 2;
            return angle;
        }
        public static double GetArcBulge(this Arc arc)
        {
            double deltaAng = arc.EndAngle - arc.StartAngle;
            if (deltaAng < 0)
                deltaAng += 2 * Math.PI;
            double bulge = Math.Tan(deltaAng * 0.25);
            if (arc.Normal.Z < 0) bulge = -bulge;
            return bulge;
        }
        /// <summary>
        /// Возвращает центральную точку кривой если кривая корректна и не нулевой длины
        /// </summary>
        /// <param name="curve">кривая</param>
        /// <param name="result">центральная точка</param>
        /// <returns>true если кривая корректна и не нулевой длины</returns>
        public static bool GetCentrPoint(this Curve curve, out Point3d result)
        {
            result = Point3d.Origin;
            try
            {
                if (curve == null || curve.IsDisposed || curve.IsErased || curve.GetLength() == 0) return false;
                result = curve.GetPointAtDist((curve.GetDistanceAtParameter(curve.StartParam) + curve.GetDistanceAtParameter(curve.EndParam)) / 2);
                return true;
            }
            catch { return false; }
        }
        /// <summary>
        /// возвращает точку между двумя точками
        /// </summary>
        /// <param name="curve"></param>
        /// <returns></returns>
        public static Point3d GetCentrPoint(this Point3d p1, Point3d p2)
        {
            return p1 + (p2 - p1) * 0.5;
        }
        public static Curve GetCurveFromGe(this Curve2d cv, Plane plane)
        {
            Curve result = null;

            LineSegment2d line2d = cv as LineSegment2d;
            CircularArc2d arc2d = cv as CircularArc2d;
            EllipticalArc2d ellipse2d = cv as EllipticalArc2d;
            NurbCurve2d spline2d = cv as NurbCurve2d;
            if (line2d != null)
            {
                result = new Line(new Point3d(plane, line2d.StartPoint), new Point3d(plane, line2d.EndPoint));
            }
            else if (arc2d != null)
            {
                if (arc2d.IsClosed() || Math.Abs(arc2d.EndAngle - arc2d.StartAngle) < 1e-5)
                {
                    result = new Circle(new Point3d(plane, arc2d.Center), plane.Normal, arc2d.Radius);
                }
                else
                {
                    if (arc2d.IsClockWise)
                    {
                        arc2d = arc2d.GetReverseParameterCurve() as CircularArc2d;
                    }
                    double angle = new Vector3d(plane, arc2d.ReferenceVector).AngleOnPlane(plane);
                    double startAngle = arc2d.StartAngle + angle;
                    double endAngle = arc2d.EndAngle + angle;
                    result = new Arc(new Point3d(plane, arc2d.Center), plane.Normal, arc2d.Radius, startAngle, endAngle);
                }
            }
            else if (ellipse2d != null)
            {
                //-------------------------------------------------------------------------------------------
                // Bug: Can not assign StartParam and EndParam of Ellipse:
                // Ellipse ent = new Ellipse(new Point3d(plane, e2d.Center), plane.Normal, 
                //      new Vector3d(plane,e2d.MajorAxis) * e2d.MajorRadius,
                //      e2d.MinorRadius / e2d.MajorRadius, e2d.StartAngle, e2d.EndAngle);
                // ent.StartParam = e2d.StartAngle; 
                // ent.EndParam = e2d.EndAngle;
                // error CS0200: Property or indexer 'Autodesk.AutoCAD.DatabaseServices.Curve.StartParam' cannot be assigned to -- it is read only
                // error CS0200: Property or indexer 'Autodesk.AutoCAD.DatabaseServices.Curve.EndParam' cannot be assigned to -- it is read only
                //---------------------------------------------------------------------------------------------
                // Workaround is using Reflection
                // 
                Ellipse ent = new Ellipse(new Point3d(plane, ellipse2d.Center), plane.Normal,
                     new Vector3d(plane, ellipse2d.MajorAxis) * ellipse2d.MajorRadius,
                     ellipse2d.MinorRadius / ellipse2d.MajorRadius, ellipse2d.StartAngle, ellipse2d.EndAngle);
                ent.GetType().InvokeMember("StartParam", BindingFlags.SetProperty, null,
                  ent, new object[] { ellipse2d.StartAngle });
                ent.GetType().InvokeMember("EndParam", BindingFlags.SetProperty, null,
                  ent, new object[] { ellipse2d.EndAngle });
                result = ent;
            }
            else if (spline2d != null)
            {
                if (spline2d.HasFitData)
                {
                    NurbCurve2dFitData n2fd = spline2d.FitData;
                    using (Point3dCollection p3ds = new Point3dCollection())
                    {
                        foreach (Point2d p in n2fd.FitPoints) p3ds.Add(new Point3d(plane, p));
                        result = new Spline(p3ds, new Vector3d(plane, n2fd.StartTangent), new Vector3d(plane, n2fd.EndTangent),
                        /* n2fd.KnotParam, */  n2fd.Degree, n2fd.FitTolerance.EqualPoint);

                    }
                }
                else
                {
                    NurbCurve2dData n2fd = spline2d.DefinitionData;
                    using (Point3dCollection p3ds = new Point3dCollection())
                    {
                        DoubleCollection knots = new DoubleCollection(n2fd.Knots.Count);
                        foreach (Point2d p in n2fd.ControlPoints) p3ds.Add(new Point3d(plane, p));
                        foreach (double k in n2fd.Knots) knots.Add(k);
                        double period = 0;
                        result = new Spline(n2fd.Degree, n2fd.Rational,
                                 spline2d.IsClosed(), spline2d.IsPeriodic(out period),
                                 p3ds, knots, n2fd.Weights, n2fd.Knots.Tolerance, n2fd.Knots.Tolerance);
                    }
                }
            }
            return result;
        }
        public static Curve GetCurveFromGe(this Curve3d cv, Plane plane)
        {
            Curve result = null;
            LineSegment3d line2d;
            CircularArc3d arc2d;
            EllipticalArc3d ellipse2d;
            NurbCurve3d spline2d;
            if (cv is ExternalCurve3d ex)
            {
                line2d = ex.NativeCurve as LineSegment3d;
                arc2d = ex.NativeCurve as CircularArc3d;
                ellipse2d = ex.NativeCurve as EllipticalArc3d;
                spline2d = ex.NativeCurve as NurbCurve3d;
            }
            else
            {
                line2d = cv as LineSegment3d;
                arc2d = cv as CircularArc3d;
                ellipse2d = cv as EllipticalArc3d;
                spline2d = cv as NurbCurve3d;
            }
            if (line2d != null)
            {
                result = new Line(line2d.StartPoint, line2d.EndPoint);
            }
            else if (arc2d != null)
            {
                if (arc2d.IsClosed() || Math.Abs(arc2d.EndAngle - arc2d.StartAngle) < 1e-5)
                {
                    result = new Circle(arc2d.Center, plane.Normal, arc2d.Radius);
                }
                else
                {
                    double angle = arc2d.ReferenceVector.AngleOnPlane(plane);
                    double startAngle = arc2d.StartAngle + angle;
                    double endAngle = arc2d.EndAngle + angle;
                    result = new Arc(arc2d.Center, plane.Normal, arc2d.Radius, startAngle, endAngle);
                }
            }
            else if (ellipse2d != null)
            {
                //-------------------------------------------------------------------------------------------
                // Bug: Can not assign StartParam and EndParam of Ellipse:
                // Ellipse ent = new Ellipse(new Point3d(plane, e2d.Center), plane.Normal, 
                //      new Vector3d(plane,e2d.MajorAxis) * e2d.MajorRadius,
                //      e2d.MinorRadius / e2d.MajorRadius, e2d.StartAngle, e2d.EndAngle);
                // ent.StartParam = e2d.StartAngle; 
                // ent.EndParam = e2d.EndAngle;
                // error CS0200: Property or indexer 'Autodesk.AutoCAD.DatabaseServices.Curve.StartParam' cannot be assigned to -- it is read only
                // error CS0200: Property or indexer 'Autodesk.AutoCAD.DatabaseServices.Curve.EndParam' cannot be assigned to -- it is read only
                //---------------------------------------------------------------------------------------------
                // Workaround is using Reflection
                // 
                Ellipse ent = new Ellipse(ellipse2d.Center, plane.Normal,
                     ellipse2d.MajorAxis * ellipse2d.MajorRadius,
                     ellipse2d.MinorRadius / ellipse2d.MajorRadius, ellipse2d.StartAngle, ellipse2d.EndAngle);
                ent.GetType().InvokeMember("StartParam", BindingFlags.SetProperty, null,
                  ent, new object[] { ellipse2d.StartAngle });
                ent.GetType().InvokeMember("EndParam", BindingFlags.SetProperty, null,
                  ent, new object[] { ellipse2d.EndAngle });
                result = ent;
            }
            else if (spline2d != null)
            {
                if (spline2d.HasFitData)
                {
                    NurbCurve3dFitData n2fd = spline2d.FitData;
                    using (Point3dCollection p3ds = new Point3dCollection())
                    {
                        foreach (Point2d p in n2fd.FitPoints) p3ds.Add(new Point3d(plane, p));
                        result = new Spline(p3ds, n2fd.StartTangent, n2fd.EndTangent,
                        /* n2fd.KnotParam, */  n2fd.Degree, n2fd.FitTolerance.EqualPoint);

                    }
                }
                else
                {
                    NurbCurve3dData n2fd = spline2d.DefinitionData;
                    using (Point3dCollection p3ds = new Point3dCollection())
                    {
                        DoubleCollection knots = new DoubleCollection(n2fd.Knots.Count);
                        foreach (Point3d p in n2fd.ControlPoints) p3ds.Add(p);
                        foreach (double k in n2fd.Knots) knots.Add(k);
                        double period = 0;
                        result = new Spline(n2fd.Degree, n2fd.Rational,
                                 spline2d.IsClosed(), spline2d.IsPeriodic(out period),
                                 p3ds, knots, n2fd.Weights, n2fd.Knots.Tolerance, n2fd.Knots.Tolerance);
                    }
                }
            }
            return result;
        }
        /// <summary>
        /// возвращает длину кривой
        /// </summary>
        /// <param name="curve"></param>
        /// <returns></returns>
        public static double GetLength(this Curve curve)
        {
            if (curve == null || curve.IsDisposed || curve.IsErased) return 0;
            try
            {
                return curve.GetDistanceAtParameter(curve.EndParam) - curve.GetDistanceAtParameter(curve.StartParam);
            }
            catch { return 0; }
        }
        /// <summary>
        /// находит ближайшую точку из списка, если списко пустой возвращает точку
        /// </summary>    
        public static Point3d GetClosestPoint(this List<Point3d> points, Point3d point)
        {
            return GetClosestPoint(points, point, double.MaxValue);
        }
        /// <summary>
        /// находит ближайшую точку из списка, если списко пустой возвращает точку
        /// </summary>   
        public static Point3d GetClosestPoint(this List<Point3d> points, Point3d point, double mindist)
        {
            if (points == null || points.Count == 0) return point;
            Point3d closest = point;
            double dist = mindist;
            foreach (Point3d p in points)
            {
                if (p.Z0().DistanceTo(point.Z0()).IsEqualTo(0)) return p;
                else if (p.Z0().DistanceTo(point.Z0()) < dist)
                {
                    closest = p;
                    dist = closest.Z0().DistanceTo(point.Z0());
                }
            }
            return closest;
        }
        /// <summary>
        /// возвращает 2д точку полученную из 3д точки путем откидывания Z
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public static Point2d GetPoint2d(this Point3d point)
        {
            return new Point2d(point.X, point.Y);
        }
        /// <summary>
        /// возвращает 3д точку полученную из 2д точки путем добавления отметки
        /// <param name="point"></param>
        /// <returns></returns>
        public static Point3d GetPoint3d(this Point2d point, double z)
        {
            return new Point3d(point.X, point.Y, z);
        }
        /// <summary>
        /// возвращает вектор по направлению поворота автокада
        /// </summary>
        /// <param name="rotation"></param>
        /// <returns></returns>
        public static Vector3d GetVectorFromRotation(double rotation)
        {
            return Vector3d.XAxis.TransformBy(Matrix3d.Rotation(rotation, Vector3d.ZAxis, Point3d.Origin));
        }
        public static Vector3d GetViewVector()
        {
            Point3d vpoint = (Point3d)Autodesk.AutoCAD.ApplicationServices.Application.GetSystemVariable("VIEWDIR");
            return vpoint.GetAsVector().GetNormal();
        }
        /// <summary>
        /// проверяет объект на пренадлежность типу Acad Curve
        /// </summary>
        /// <param name="ent"></param>
        /// <returns></returns>
        public static bool IsAcadCurve(this Entity ent, bool includeLeader = false)
        {
            if (ent is Arc || ent is Circle || ent is Ellipse || ent is Line ||
                ent is Polyline || ent is Polyline2d || ent is Polyline3d ||
                ent is Ray || ent is Spline || ent is Xline || (includeLeader && ent is Leader)) return true;
            else return false;
        }

        public static bool IsEqualTo(this double d1, double d2)
        {
            return d1.IsEqualTo(d2, 0);
        }
        /// <summary>
        /// сраванивает 2 числа
        /// </summary>
        /// <param name="d1"></param>
        /// <param name="d2"></param>
        /// <returns></returns>
        public static bool IsEqualTo(this double d1, double d2, double delta = 0)
        {
            if (delta == 0) delta = Tolerance.Global.EqualPoint;
            if (Math.Abs(d2 - d1) > delta) return false; return true;
        }
        public static bool IsIntersect(this Curve p1, Curve p2)
        {
            using (Point3dCollection collection = new Point3dCollection())
            {
                p1.IntersectWith(p2, Intersect.OnBothOperands, collection, IntPtr.Zero, IntPtr.Zero);
                if (collection.Count > 0)
                {
                    foreach (Point3d p in collection)
                    {
                        if (p.IsEqualTo(p1.GetClosestPointTo(p, false)) &&
                            p.IsEqualTo(p2.GetClosestPointTo(p, false)))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }
        /// <summary>
        /// проверяет пересекаются ли кривые или нет
        /// </summary>   
        public static bool IsIntersect(this Curve curve, Curve curve2, bool apparentIntersections)
        {
            List<Point3d> intersections = new List<Point3d>();

            Curve c1;
            Curve c2;

            if (apparentIntersections)
            {
                c1 = curve.GetProjectedCurve(new Plane(), Vector3d.ZAxis);
                c2 = curve2.GetProjectedCurve(new Plane(), Vector3d.ZAxis);
            }
            else
            {
                c1 = curve;
                c2 = curve2;
            }

            using (Point3dCollection coll = new Point3dCollection())
            {
                c1.IntersectWith(c2, Intersect.OnBothOperands, coll, IntPtr.Zero, IntPtr.Zero);
                intersections.AddRange(coll.ToList());
            }

            return IsIntersect(c1, c2, intersections);
        }
        /// <summary>
        /// проверяет пересекаются ли кривые или нет
        /// </summary>   
        public static bool IsIntersect(this Curve curve, Curve curve2, List<Point3d> intersections)
        {
            if (intersections.Count == 0 || curve.GetLength().IsEqualTo(0) || curve2.GetLength().IsEqualTo(0)) return false;

            using (Plane plane = new Plane())
            {
                Tolerance tolerance = new Tolerance(0.00001, 0.00001);

                foreach (Point3d point in intersections)
                {
                    Vector2d c1direct = new Vector2d();
                    Vector2d c1invers = new Vector2d();

                    Vector2d c2direct = new Vector2d();
                    Vector2d c2invers = new Vector2d();

                    try { c1direct = curve.GetFirstDerivative(curve.GetIncrementParametr(point, 0.000001)).Convert2d(plane).GetNormal(); } catch { }
                    try { c1invers = curve.GetFirstDerivative(curve.GetIncrementParametr(point, -0.000001)).Convert2d(plane).GetNormal(); } catch { }

                    try { c2direct = curve2.GetFirstDerivative(curve2.GetIncrementParametr(point, 0.000001)).Convert2d(plane).GetNormal(); } catch { }
                    try { c2invers = curve2.GetFirstDerivative(curve2.GetIncrementParametr(point, -0.000001)).Convert2d(plane).GetNormal(); } catch { }

                    if (c1direct.IsEqualTo(c2direct, tolerance) ||
                        c1direct.IsEqualTo(-c2direct, tolerance) ||
                        c1direct.IsEqualTo(c2invers, tolerance) ||
                        c1direct.IsEqualTo(-c2invers, tolerance) ||
                        c1invers.IsEqualTo(c2direct, tolerance) ||
                        c1invers.IsEqualTo(-c2direct, tolerance) ||
                        c1invers.IsEqualTo(c2invers, tolerance) ||
                        c1invers.IsEqualTo(-c2invers, tolerance)
                        ) continue;

                    return true;
                }
            }
            return false;
        }
        public static bool? IsSelfIntersect(this Curve curve)
        {
            try
            {
                using (Curve3d c3d = curve.GetGeCurve())
                using (CurveCurveIntersector3d cci = new CurveCurveIntersector3d(c3d, c3d, Vector3d.ZAxis))
                {
                    if (cci.NumberOfIntersectionPoints > 0) return true;
                    return false;
                }
            }
            catch
            {
                return null;
            }
        }
        private static double GetIncrementParametr(this Curve curve, Point3d point, double increment)
        {
            double parametr = curve.GetParameterAtPoint(curve.GetClosestPointTo(point, false));

            if (parametr.IsEqualTo(curve.EndParam) && increment > 0) parametr = curve.StartParam + increment;
            else if (parametr.IsEqualTo(curve.StartParam) && increment < 0) parametr = curve.EndParam + increment;
            else parametr += increment;

            return parametr;
        }
        /// <summary>
        /// создает зеркальные относительно оси Y точки в списке
        /// </summary>
        /// <param name="points"></param>
        public static void MirrorList(this List<Point3d> points)
        {
            for (int i = points.Count - 1; i >= 0; i--)
            {
                points.Add(new Point3d(-points[i].X, points[i].Y, 0));
            }
        }

        /// <summary>
        /// сортирует точки по близости к началу кривой
        /// </summary>
        public static void SortOnCurve(this List<Point3d> points, Curve curve)
        {
            if (points.Count == 0 || curve.GetLength() == 0) return;
            for (int i = 0; i < points.Count; i++)
            {
                points[i] = curve.GetClosestPointTo(points[i], false);
            }
            List<Point3d> result = new List<Point3d>();
            while (points.Count > 0)
            {
                double dist = curve.GetDistAtPoint(points[0]);
                Point3d closest = points[0];
                foreach (Point3d point in points)
                {
                    double newDist = curve.GetDistAtPoint(point);
                    if (newDist < dist)
                    {
                        dist = newDist;
                        closest = point;
                    }
                }
                points.Remove(closest);
                result.Add(closest);
            }
            points.AddRange(result);
        }
        /// <summary>
        /// сортирует точки по близости к началу кривой
        /// </summary>
        public static void SortOnCurve(this Point3dCollection points, Curve curve)
        {
            Point3dCollection result = new Point3dCollection();
            while (points.Count > 0)
            {
                double dist = curve.GetDistAtPoint(points[0]);
                Point3d closest = points[0];
                foreach (Point3d point in points)
                {
                    double newDist = curve.GetDistAtPoint(point);
                    if (newDist < dist)
                    {
                        dist = newDist;
                        closest = point;
                    }
                }
                points.Remove(closest);
                result.Add(closest);
            }
            foreach (Point3d p in result) points.Add(p);
        }
        /// <summary>
        /// преобразует список в коллекцию точек
        /// </summary>
        public static Point3dCollection ToPoint3dCollection(this List<Point3d> points)
        {
            Point3dCollection result = new Point3dCollection();
            foreach (Point3d point in points) result.Add(point);
            return result;
        }
        /// <summary>
        /// преобразует коллекцию точек в список
        /// </summary>
        public static List<Point3d> ToList(this Point3dCollection points)
        {
            List<Point3d> result = new List<Point3d>();
            foreach (Point3d point in points) result.Add(point);
            return result;
        }
        /// <summary>
        /// возвращает точку с обнуленной высотой
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public static Point3d Z0(this Point3d point)
        {
            return new Point3d(point.X, point.Y, 0);
        }
        public static int? GetFirstPointIndex(this List<Point3d> points, Point3d point)
        {
            for (int i = 0; i < points.Count; i++)
            {
                if (points[i].IsEqualTo(point)) return i;
            }
            return null;
        }
        public static int? GetLastPointIndex(this List<Point3d> points, Point3d point)
        {
            for (int i = points.Count - 1; i >= 0; i--)
            {
                if (points[i].IsEqualTo(point)) return i;
            }
            return null;
        }
        public static bool Under(this double number, double number2) => !number.IsEqualTo(number2) && number < number2;
        public static bool Over(this double number, double number2) => !number.IsEqualTo(number2) && number > number2;
        public static Curve2d ConvertToCurve2d(this Curve3d curve3D)
        {
            Curve2d result = null;

            LineSegment3d line2d = curve3D as LineSegment3d;
            CircularArc3d arc2d = curve3D as CircularArc3d;
            EllipticalArc3d ellipse2d = curve3D as EllipticalArc3d;
            NurbCurve3d spline2d = curve3D as NurbCurve3d;
            if (line2d != null)
            {
                result = new Line2d(new Point2d(line2d.StartPoint.X, line2d.StartPoint.Y), new Point2d(line2d.EndPoint.X, line2d.EndPoint.Y));
            }
            else if (arc2d != null)
            {

                if (arc2d.IsClosed() || Math.Abs(arc2d.EndAngle - arc2d.StartAngle) < 1e-5)
                {
                    result = new CircularArc2d(arc2d.Center.GetPoint2d(), arc2d.Radius);
                }
                else
                {
                    arc2d.GetCurveFromGe(new Plane()).GetCentrPoint(out Point3d center);
                    result = new CircularArc2d(arc2d.StartPoint.GetPoint2d(), center.GetPoint2d(), arc2d.EndPoint.GetPoint2d());
                }
            }
            else if (ellipse2d != null)
            {
                result = new EllipticalArc2d(ellipse2d.Center.GetPoint2d(),
                    ellipse2d.MajorAxis.Convert2d(new Plane()),
                    ellipse2d.MinorAxis.Convert2d(new Plane()),
                    ellipse2d.MajorRadius, ellipse2d.MinorRadius);
            }
            else if (spline2d != null)
            {
                if (spline2d.HasFitData)
                {
                    NurbCurve3dFitData n2fd = spline2d.FitData;
                    Point2dCollection p3ds = new Point2dCollection();
                    foreach (Point3d p in n2fd.FitPoints) p3ds.Add(p.GetPoint2d());
                    result = new NurbCurve2d(p3ds,
                        n2fd.StartTangent.Convert2d(new Plane()),
                        n2fd.EndTangent.Convert2d(new Plane()), true, true, n2fd.KnotParam
                        );
                }
                else
                {
                    NurbCurve3dData n2fd = spline2d.DefinitionData;
                    Point2dCollection p2dcoll = new Point2dCollection();
                    foreach (Point3d p in n2fd.ControlPoints) p2dcoll.Add(p.GetPoint2d());
                    result = new NurbCurve2d(n2fd.Degree, n2fd.Knots, p2dcoll, n2fd.Periodic);
                }
            }
            return result;
        }


        #region connectCurve
        /// <summary>
        /// Соединяет фрагменты кривой и возвращает список с результатом соединения. Возвращает false и пусотой список если произошла ошибка.
        /// Исходный список не трогается.
        /// </summary>   
        public static bool ConnectCurve(this List<Curve> fragments, out List<Curve> result, bool project, bool disposeFragments = false)
        {
            bool boolResult = true;

            result = new List<Curve>();
            List<Curve> curveToUnite = new List<Curve>();
            List<Curve> uniteResult = new List<Curve>();

            List<CurveData> curveDatas = new List<CurveData>();
            Dictionary<int, List<CurveData>> hashes = new Dictionary<int, List<CurveData>>();

            Extents3d extents = new Extents3d();

            using (Plane plane = new Plane())
            {
                foreach (Curve curve in fragments)
                {
                    //Если кривая замкнута то сразу закидываем ее в рузультат
                    if (curve.Closed || curve.StartPoint.IsEqualTo(curve.EndPoint))
                    {
                        result.Add(curve.Clone() as Curve);
                        continue;
                    }

                    if (AllowedCurveTypes.Contains(curve.GetType()))
                    {
                        Curve toUnite = project
                            ? curve.GetOrthoProjectedCurve(plane)
                            : curve.Clone() as Curve;

                        curveToUnite.Add(toUnite);
                        extents.AddExtents(toUnite.GeometricExtents);
                    }
                }
            }

            if (curveToUnite.Count == 0) return true;

            //Вектор смещения к началу координат в плане, высоту не трогаем,
            //обычно большинство элементов лежит в районе 0 и добавлять координату Z из за затесавшегося объекта с высотой не стоит
            Point3d center = extents.MaxPoint + (extents.MaxPoint - extents.MinPoint) / 2;
            Vector3d vector = Point3d.Origin - center;
            vector = new Vector3d(vector.X, vector.Y, 0);

            foreach (Curve curve in curveToUnite)
            {
                //Переносим к началу координат
                curve.TransformBy(Matrix3d.Displacement(vector));

                CurveData curveData = new CurveData(curve);
                curveDatas.Add(curveData);
                foreach (int i in curveData.Hashes)
                {
                    if (hashes.TryGetValue(i, out List<CurveData> datas)) datas.Add(curveData);
                    else hashes.Add(i, new List<CurveData> { curveData });
                }
            }

            //Основная работа метода
            bool splineExist = true;
            while (boolResult && curveDatas.Count > 0)
            {
                CurveData first = null;

                //Если возможно есть объекты, которые можно привести к сплайну
                if (splineExist)
                {
                    //Пробуем найти сплайн
                    first = curveDatas.FirstOrDefault(x => x.CurveType == CurveType.IsSpline || x.CurveType == CurveType.IsEllipse || x.CurveType == CurveType.IsPolyline3d);
                    //Если не нашли обозначаем что сплайнов в нашем списке нет
                    if (first == null) splineExist = false;
                    else
                    {
                        //если нашли то приводим к сплайну
                        if (first.CurveType != CurveType.IsSpline)
                        {
                            Spline spline = first.Curve.Spline;
                            first.Curve?.Dispose();
                            first.Curve = spline;
                        }
                    }

                }

                //Если сплайнов нет то пробуем найти полилинию
                if (!splineExist) first = curveDatas.FirstOrDefault(x => x.CurveType == CurveType.IsPolyline);

                //Если не нашли то в списке остались только отрезки и дуги. Приводим к полилинии
                if (first == null)
                {
                    first = curveDatas[0];
                    Polyline polyline = new Polyline(2);
                    if (first.Curve is Arc arc)
                    {
                        polyline.AddVertexAt(0, new Point2d(arc.StartPoint.X, arc.StartPoint.Y), arc.GetArcBulge(), 0, 0);
                        polyline.AddVertexAt(1, new Point2d(arc.EndPoint.X, arc.EndPoint.Y), 0, 0, 0);
                        polyline.Normal = arc.Normal;

                    }
                    else if (first.Curve is Line line)
                    {
                        polyline.AddVertexAt(0, new Point2d(line.StartPoint.X, line.StartPoint.Y), 0, 0, 0);
                        polyline.AddVertexAt(1, new Point2d(line.EndPoint.X, line.EndPoint.Y), 0, 0, 0);
                        polyline.Normal = line.Normal;
                    }
                    else
                    {
                        boolResult = false;
                        break;
                    }
                    first.Curve.Dispose();
                    first.Curve = polyline;
                }

                //На всякий случай
                if (first == null)
                {
                    boolResult = false;
                    break;
                }

                //Удаляем из списка выбранный первым элемент
                curveDatas.Remove(first);

                //Удаляем его из хэша
                RemoveData(hashes, first);

                //пересчитываем хэш если вдруг что-то изменилось при преобразовании
                first = new CurveData(first.Curve);

                //ищем присоединенные элементы
                while (boolResult && curveDatas.Count > 0)
                {
                    //ищем совпадения
                    List<int> interscts = first.Hashes.Intersect(hashes.Keys).ToList();
                    if (interscts.Count == 0) break;

                    //ищем совпадающий элемент
                    if (hashes.TryGetValue(interscts[0], out List<CurveData> datas) && datas.Count > 0)
                    {
                        CurveData curveData = null;


                        if (datas.Count == 1)
                        {
                            // Развилки нет — забираем единственный элемент без расчетов
                            curveData = datas[0];
                        }
                        else
                        {
                            // НАЙДЕНА РАЗВИЛКА! Выбираем магистральное направление
                            try
                            {
                                // 1. Находим точную координату точки стыка
                                Point3d junctionPoint = GetJunctionPoint(first.Curve, datas[0].Curve);

                                // 2. Получаем вектор направления нашей растущей кривой (смотрит НАРУЖУ)
                                Vector3d currentDir = GetCurveTangent(first.Curve, junctionPoint, lookOut: true);

                                double minAngle = double.MaxValue;

                                // 3. Сканируем кандидатов
                                foreach (CurveData candidate in datas)
                                {
                                    // Вектор кандидата (тоже смотрит НАРУЖУ из точки стыка в свое тело)
                                    Vector3d candidateDir = GetCurveTangent(candidate.Curve, junctionPoint, lookOut: true);

                                    // ИСПРАВЛЕНО: Считаем честный плоский угол от 0 до 2*PI против часовой стрелки
                                    double angle = currentDir.GetAngleTo(candidateDir, Vector3d.ZAxis);

                                    // Ищем кандидата с минимальным плоским углом.
                                    // Это гарантирует, что на развилке мы всегда будем выбирать 
                                    // самый "крайний" поворот в одну сторону (например, строго направо),
                                    // аккуратно "отслаивая" и закрывая текущую петлю контура.
                                    if (angle < minAngle)
                                    {
                                        minAngle = angle;
                                        curveData = candidate;
                                    }
                                }
                            }
                            catch
                            {
                                // Если на каком-то сложном сплайне расчет геометрии упал —
                                // берем первый попавшийся элемент как фолбэк (как и было раньше)
                                curveData = datas[0];
                            }
                        }

                        try
                        {
                            //присоединияем
                            first.Curve.JoinEntity(curveData.Curve);
                            //очищаем присоединенный элемент
                            curveData.Curve?.Dispose();
                            //удаляем присоединенный элемент из списка
                            curveDatas.Remove(curveData);
                            //удаляем присоединенный элемент из хэша
                            RemoveData(hashes, curveData);
                            //пересоздаем первый элемент что бы пересчитать кэш с учетом новых границ
                            first = new CurveData(first.Curve);
                            continue;
                        }
                        catch
                        {
                        }
                    }


                    boolResult = false;
                    break;
                }


                //Добавляем в список результа объединения
                uniteResult.Add(first.Curve);
            }

            if (disposeFragments)
            {
                foreach (Curve curve in fragments) { curve?.Dispose(); }
            }

            //если все прошло нормально то возвращаем результат на место и переносим его в результирующий список
            if (boolResult)
            {
                foreach (Curve curve in uniteResult)
                {
                    curve.TransformBy(Matrix3d.Displacement(-vector));
                    result.Add(curve);
                }
            }
            else
            {

                // Очищаем то, что успели собрать в финальный пул
                foreach (Curve curve in uniteResult) { curve?.Dispose(); }
                foreach (Curve curve in result) { curve?.Dispose(); }

                // Очищаем то, что осталось в обработке и НЕ было уничтожено/конвертировано
                foreach (CurveData data in curveDatas)
                {
                    data.Curve?.Dispose();
                }
            }

            return boolResult;
        }
        private static void RemoveData(Dictionary<int, List<CurveData>> hashes, CurveData data)
        {
            foreach (int hash in data.Hashes)
            {
                if (hashes.TryGetValue(hash, out List<CurveData> datas))
                {
                    datas.Remove(data);
                    if (datas.Count == 0)
                    {
                        hashes.Remove(hash);
                    }
                }
            }
        }
        /// <summary>
        /// Возвращает вектор касательной кривой в заданной точке, направленный внутрь или наружу объекта.
        /// </summary>
        private static Vector3d GetCurveTangent(Curve curve, Point3d point, bool lookOut)
        {
            // Получаем внутренний параметр AutoCAD для этой точки
            double param = curve.GetParameterAtPoint(point);

            // Берем первую производную (вектор касательной в направлении роста параметра кривой)
            Vector3d tangent = curve.GetFirstDerivative(param).GetNormal();

            // AutoCAD всегда считает производную от StartParam к EndParam. 
            // Нам нужно сопоставить её с направлением движения.
            bool isStartPoint = point.IsEqualTo(curve.StartPoint);

            if (lookOut)
            {
                // Если смотрим НАРУЖУ из кривой:
                // Из стартовой точки вектор должен смотреть против хода параметра, из конечной — по ходу.
                return isStartPoint ? -tangent : tangent;
            }
            else
            {
                // Если смотрим ВНУТРЬ кривой-кандидата:
                // Входя в стартовую точку, движемся по ходу параметра. Входя в конечную — против.
                return isStartPoint ? tangent : -tangent;
            }
        }

        /// <summary>
        /// Находит точку соприкосновения двух кривых (сравнивает их концы).
        /// </summary>
        private static Point3d GetJunctionPoint(Curve c1, Curve c2)
        {
            if (c1.StartPoint.IsEqualTo(c2.StartPoint) || c1.StartPoint.IsEqualTo(c2.EndPoint))
                return c1.StartPoint;

            return c1.EndPoint;
        }

        private static readonly HashSet<Type> AllowedCurveTypes = new HashSet<Type>
        {
            typeof(Spline),
            typeof(Polyline),
            typeof(Polyline2d),
            typeof(Polyline3d),
            typeof(Ellipse),
            typeof(Line),
            typeof(Arc)
        };
        #endregion
    }

    internal enum CurveType
    {
        IsSpline,
        IsPolyline,
        IsEllipse,
        IsPolyline3d,
        none,
    }
    internal class CurveData
    {
        public CurveData(Curve curve)
        {
            Curve = curve;

            if (Curve is Spline) CurveType = CurveType.IsSpline;
            else if (Curve is Polyline2d polyline2D)
            {
                Polyline converted = new Polyline();
                converted.ConvertFrom(polyline2D, false);
                polyline2D?.Dispose();
                Curve = converted;
                CurveType = CurveType.IsPolyline;
            }
            else if (Curve is Polyline) CurveType = CurveType.IsPolyline;
            else if (Curve is Ellipse) CurveType = CurveType.IsEllipse;
            else if (Curve is Polyline3d) CurveType = CurveType.IsPolyline3d;
            else CurveType = CurveType.none;

            // Собираем множественные хэши для старта и конца
            var startHashes = Point3dComparer.Precise.GetHashes(curve.StartPoint);
            var endHashes = Point3dComparer.Precise.GetHashes(curve.EndPoint);

            // Объединяем их в один плоский список уникальных хэшей
            foreach (var h in startHashes) Hashes.Add(h);
            foreach (var h in endHashes) Hashes.Add(h);

            // Убираем дубликаты, если они возникли на стыках
            Hashes = Hashes.Distinct().ToList();
        }
        public Curve Curve;
        public List<int> Hashes = new List<int>();
        public CurveType CurveType;
    }
    public class Point3dComparer : IEqualityComparer<Point3d>
    {
        // Сирлтоны (готовые статические экземпляры) для частых допусков
        public static readonly Point3dComparer Global = new Point3dComparer(Tolerance.Global.EqualPoint);
        public static readonly Point3dComparer Precise = new Point3dComparer(1e-7);

        private readonly double _toleranceValue;
        private readonly int _decimals;
        private readonly Tolerance _tolerance; // Кэшируем объект AutoCAD один раз!

        public Point3dComparer(double tolerance)
        {
            _toleranceValue = tolerance;
            _decimals = Math.Max(0, (int)Math.Ceiling(-Math.Log10(tolerance)));
            // Создаем Tolerance один раз при инициализации компаратора
            _tolerance = new Tolerance(tolerance, tolerance);
        }

        public bool Equals(Point3d p1, Point3d p2)
        {
            // Используем закэшированный Tolerance, ничего нового в памяти не создается!
            return p1.IsEqualTo(p2, _tolerance);
        }

        public int GetHashCode(Point3d obj)
        {
            double x = Math.Round(obj.X, _decimals);
            double y = Math.Round(obj.Y, _decimals);
            double z = Math.Round(obj.Z, _decimals);

            unchecked
            {
                int hash = 17;
                hash = hash * 23 + x.GetHashCode();
                hash = hash * 23 + y.GetHashCode();
                hash = hash * 23 + z.GetHashCode();
                return hash;
            }
        }
        // Изменяем сигнатуру, чтобы возвращать все подходящие хэши для точки
        public IEnumerable<int> GetHashes(Point3d obj)
        {
            // 1. Переводим координаты в пространство сетки (масштабируем)
            double valX = obj.X / _toleranceValue;
            double valY = obj.Y / _toleranceValue;
            double valZ = obj.Z / _toleranceValue;

            // 2. Базовые индексы текущей ячейки
            long baseX = (long)Math.Floor(valX);
            long baseY = (long)Math.Floor(valY);
            long baseZ = (long)Math.Floor(valZ);

            // 3. Списки индексов по каждой оси, которые затронуты (базовая + возможные соседние)
            // В большинстве случаев тут будет только 1 элемент, на границах — 2.
            var gridX = GetTargetIndices(valX, baseX);
            var gridY = GetTargetIndices(valY, baseY);
            var gridZ = GetTargetIndices(valZ, baseZ);

            // 4. Комбинируем все найденные индексы (декартово произведение осей)
            // Если точка в центре ячейки: цикл выполнится 1 раз (1*1*1)
            // Если на стыке плоскостей/ребер/углов: от 2 до 8 раз
            foreach (long x in gridX)
            {
                foreach (long y in gridY)
                {
                    foreach (long z in gridZ)
                    {
                        yield return CombineHash(x, y, z);
                    }
                }
            }
        }

        // Вспомогательный метод определения: нужно ли захватить соседнюю ячейку
        private IEnumerable<long> GetTargetIndices(double scaledVal, long baseIdx)
        {
            yield return baseIdx; // Базовая ячейка нужна всегда

            double fraction = scaledVal - baseIdx; // Остаток от деления (положение внутри ячейки от 0.0 до 1.0)

            // Если точка прижалась к ЛЕВОЙ границе текущей ячейки (ближе чем на 1 допуск)
            if (fraction < 0.1)
            {
                yield return baseIdx - 1;
            }
            // Если точка прижалась к ПРАВОЙ границе текущей ячейки
            else if (fraction > 0.9)
            {
                yield return baseIdx + 1;
            }
        }

        // Выносим расчет хэша на основе целочисленных координат сетки
        private int CombineHash(long x, long y, long z)
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + x.GetHashCode();
                hash = hash * 23 + y.GetHashCode();
                hash = hash * 23 + z.GetHashCode();
                return hash;
            }
        }

    }
}
