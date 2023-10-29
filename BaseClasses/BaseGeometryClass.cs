using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;

namespace BaseFunction
{
    public static class BaseGeometryClass
    {
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
        /// Проверяет, содержит ли список точку
        /// </summary>
        /// <param name="points"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        public static bool ContainPoint(this List<Point3d> points, Point3d point)
        {
            foreach (Point3d p in points) { if (p.IsEqualTo(point)) return true; }
            return false;
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
        /// <summary>
        /// Возвращает центральную точку кривой если кривая корректна и не нулевой длины
        /// </summary>
        /// <param name="curve">кривая</param>
        /// <param name="result">центральная точка</param>
        /// <returns>true если кривая корректна и не нулевой длины</returns>
        public static bool GetCentrPoint(this Curve curve, out Point3d result)
        {
            result = new Point3d();
            if (curve == null || curve.IsDisposed || curve.IsErased || curve.GetLength() == 0) return false;
            result = curve.GetPointAtDist((curve.GetDistanceAtParameter(curve.StartParam) + curve.GetDistanceAtParameter(curve.EndParam)) / 2);
            return true;
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
        /// <summary>
        /// возвращает длину кривой
        /// </summary>
        /// <param name="curve"></param>
        /// <returns></returns>
        public static double GetLength(this Curve curve)
        {
            if (curve == null || curve.IsDisposed || curve.IsErased) return 0;
            return curve.GetDistanceAtParameter(curve.EndParam) - curve.GetDistanceAtParameter(curve.StartParam);
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
        /// <summary>
        /// проверяет объект на пренадлежность типу Acad Curve
        /// </summary>
        /// <param name="ent"></param>
        /// <returns></returns>
        public static bool IsAcadCurve(this Entity ent)
        {
            if (ent is Arc || ent is Circle || ent is Ellipse || ent is Line ||
                ent is Polyline || ent is Polyline2d || ent is Polyline3d ||
                ent is Ray || ent is Spline || ent is Xline) return true;
            else return false;
        }
        /// <summary>
        /// сраванивает 2 числа
        /// </summary>
        /// <param name="d1"></param>
        /// <param name="d2"></param>
        /// <returns></returns>
        public static bool IsEqualTo(this double d1, double d2)
        { 
            if (Math.Abs(d2 - d1) > Tolerance.Global.EqualPoint) return false; return true;        
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
    }
}
