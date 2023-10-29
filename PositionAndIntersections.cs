using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BaseFunction
{
    public static class PositionAndIntersections
    {
        public static bool TryGetIntersections(this Curve curve, Curve curve2, out List<Point3d> intersections)
        { 
            if (curve.Equals(curve2)) return curve.TryGetSelfIntersect(out intersections);

            intersections = new List<Point3d>();
            using (Curve curvePr = curve.GetProjectedCurve(new Plane(), Vector3d.ZAxis))
            using (Curve curve2Pr = curve2.GetProjectedCurve(new Plane(), Vector3d.ZAxis))
            {
                if (curvePr.GetLength() == 0 || curve2Pr.GetLength() == 0) return false;
                try
                {
                    using (Curve3d icurve3d = curve.GetGeCurve())
                    using (Curve3d curve3d = curvePr.GetGeCurve())
                    using (Curve3d curve23d = curve2Pr.GetGeCurve())
                    using (CurveCurveIntersector3d cci = new CurveCurveIntersector3d(curve3d, curve23d, Vector3d.ZAxis))
                    {
                        for (int i = 0; i < cci.NumberOfIntersectionPoints; i++)
                        {
                            using (Line3d l3d = new Line3d(cci.GetIntersectionPoint(i), Vector3d.ZAxis))
                            using (CurveCurveIntersector3d cci2 = new CurveCurveIntersector3d(l3d, icurve3d, Vector3d.ZAxis))
                            {
                                for (int j = 0; j < cci2.NumberOfIntersectionPoints; j++)
                                { 
                                    intersections.Add(cci2.GetIntersectionPoint(j));
                                }                            
                            }
                        } 
                    }
                    intersections.SortOnCurve(curve);
                }
                catch { return false; }
            }
            if (intersections.Count > 0) return true; return false;            
        }
        public static bool TryGetSelfIntersect(this Curve curve, out List<Point3d> intersections)
        {
            intersections = new List<Point3d>();
            if (curve is Line || curve is Circle || curve is Arc) return false;
            if (curve is Spline)
            {
                intersections.AddRange(Intersectionts(curve, curve));
            }
            else
            {
                using (DBObjectCollection coll = new DBObjectCollection())
                {
                    if (coll.Count > 1)
                    {
                        foreach (Curve c in coll)
                        {
                            if (c == null) continue;
                            foreach (Curve c2 in coll)
                            {
                                if (c2.Equals(c)) continue;
                                intersections.AddRange(Intersectionts(c, c2));
                            }
                        }
                        foreach (Curve c in coll)
                        {
                            c?.Dispose();
                        }
                    }
                    else if (coll.Count == 1 && coll[0] is Curve c)
                    {                      
                        intersections.AddRange(Intersectionts(c, c));
                    }
                }
            }
            if (intersections.Count > 0) return true; return false;
        }

        public static List<Point3d> Intersectionts(this Curve c, Curve c2)
        {
            List<Point3d> intersections = new List<Point3d>();
            using (Point3dCollection coll2 = new Point3dCollection())
            {
                c.IntersectWith(c2, Intersect.OnBothOperands, coll2, IntPtr.Zero, IntPtr.Zero);
                if (coll2.Count > 0)
                {
                    foreach (Point3d p in coll2)
                    {
                        if ((p.IsEqualTo(c.StartPoint) || p.IsEqualTo(c.EndPoint)) &&
                            (p.IsEqualTo(c2.StartPoint) || p.IsEqualTo(c2.EndPoint))) continue;
                        if (!intersections.Contains(p)) intersections.Add(p);
                    }
                }
            }
            return intersections;
        }
        public static PositionType GetPositionType(this Point3d point, ObjectId c)
        {
            return point.GetPositionType(new List<object> { c });
        }
        public static PositionType GetPositionType(this Point3d point, Curve c)
        {      
            return point.GetPositionType(new List<object> { c }, null);
        }
        public static PositionType GetPositionType(this Point3d point, List<Object> objects)
        {
            PositionType position = PositionType.fault;
            using (Transaction tr = HostApplicationServices.WorkingDatabase.TransactionManager.StartTransaction())
            {
                position = point.GetPositionType(objects, tr);
                tr.Commit();
            }
            return position;
        }
        /// <summary>
        /// определяет положение точки относительно кривых в плоскости XY Autocad
        /// </summary>
        /// <param name="point"></param>
        /// <param name="objects">список кривых в виде собственно самих кривых или их ObjectId</param>       
        /// <returns></returns>
        public static PositionType GetPositionType(this Point3d point, List<Object> objects, Transaction tr)
        {
            if (objects.Count == 0) return PositionType.fault;
            //получаем проекцию точки на проскость XY
            point = point.Z0();
            //создаем список для кривых
            List<Curve> curves = new List<Curve>();
            foreach (Object obj in objects)
            {
                Curve curve = null;
                if (obj is ObjectId id)
                {
                    //если кривая в виде ObjectId то получаем ее из базы данных
                    if (tr != null) curve = tr.GetObject(id, OpenMode.ForRead, false, true).Clone() as Curve;       
                }
                //если это кривая то получаем ее как кривую
                else if (obj is Curve) curve = obj as Curve;
                if (curve != null && curve.IsAcadCurve())
                {
                    //проецируем кривую на плоскость XY
                    curve = curve.GetProjectedCurve(new Plane(), Vector3d.ZAxis);
                    //если длина спроецированной кривой равна нулю то пропускаем ее 
                    if (curve.GetLength() == 0) continue;
                    //определяем находится ли точка на линии   
                    if (curve.GetClosestPointTo(point, false).IsEqualTo(point)) return PositionType.onBound;
                    curves.Add(curve);
                }
            }
            if (curves.Count == 0) return PositionType.fault;
            //если контур один и он замкнут пробуем определить положение точки используя mpolygon
            if (curves.Count == 1 && curves[0] is Polyline poly && poly.StartPoint.IsEqualTo(poly.EndPoint))
            {
                if (poly.Area == 0) return PositionType.onBound;
                Curve curve = curves[0];
                if (curve.Closed || curve.StartPoint.IsEqualTo(curve.EndPoint))
                {
                    try
                    {
                        using (MPolygon mp = new MPolygon())
                        {
                            using (Polyline polyline = curve.Clone() as Polyline)
                            {
                               
                                if (polyline != null)
                                {
                                    polyline.Closed = true;
                                    if (!poly.TryGetSelfIntersect(out _))
                                    {
                                        mp.AppendLoopFromBoundary(polyline, true, Tolerance.Global.EqualPoint);
                                        if (mp.IsPointOnLoopBoundary(point, 0, Tolerance.Global.EqualPoint)) return PositionType.onBound;
                                        if (mp.IsPointInsideMPolygon(point, Tolerance.Global.EqualPoint).Count > 0) return PositionType.inner;
                                        else return PositionType.outer;
                                    }
                                }
                            }
                        }
                    }
                    catch
                    {
                    }
                }
            }
            //если через полигон не удалось то проверяем через лучи

            //число пересечений
            List<int> per = new List<int>();
            //3 раза определяем количество пересечений что бы свести к минимуму возможность попадания луча на сегмент контура
            List<Point3d> points = IntersectPointsOnVector(point, Vector3d.XAxis.TransformBy(Matrix3d.Rotation(Math.PI / 200, Vector3d.ZAxis, point)), curves);
            //если произошла ошибка возвращаем ее
            if (points == null) return PositionType.fault;
            //если ни одного пересечения нет точка точно снаружи
            if (points.Count == 0) return PositionType.outer;
            //если пересечени есть записываем остаток от деления на 2 в число пересечений
            per.Add(points.Count % 2);
            //повторяем
            points = IntersectPointsOnVector(point, Vector3d.XAxis.TransformBy(Matrix3d.Rotation(Math.PI / 100, Vector3d.ZAxis, point)), curves);
            if (points == null) return PositionType.fault;
            if (points.Count == 0) return PositionType.outer;
            per.Add(points.Count % 2);
            //повторяем
            points = IntersectPointsOnVector(point, Vector3d.XAxis.TransformBy(Matrix3d.Rotation(-Math.PI / 200, Vector3d.ZAxis, point)), curves);
            if (points == null) return PositionType.fault;
            if (points.Count == 0) return PositionType.outer;
            //ищем остаток от деления общего числа пересечений на 2
            per.Add(points.Count % 2);
            //если результат больше 0,5 то почти 100% что она внутри
            //так как шанс что 3 луча из точки пройдут ровно по ребрам фигуры крайне мал
            if (per.Average() > 0.5) return PositionType.inner;
            return PositionType.outer;
        }
        public enum PositionType : int
        {
            //внутри
            inner = 1,
            //снаружи
            outer = 2,
            //на границе
            onBound = 3,
            //ошибка определения
            fault = 0
        }
        private static List<Point3d> IntersectPointsOnVector(Point3d point, Vector3d direct, List<Curve> curves)
        {
            //список точек пересечения
            List<Point3d> points = new List<Point3d>();
            //создаем луч из точки по выбранному вектору и геометрическую кривую из луча
            using (Ray ray = new Ray() { BasePoint = point, UnitDir = direct })
            using (Curve3d ray3d = ray.GetGeCurve())
            {
                try
                {
                    //проходим по всем кривым и ищем пересечения    
                    foreach (Curve curve in curves)
                    {
                        //список точек пересечения с конкретной кривой
                        List<Point3d> vpoints = new List<Point3d>();
                        //получаем геометрическую кривую и пересечения этой кривой и луча
                        using (Curve3d curve3d = curve.GetGeCurve())
                        using (CurveCurveIntersector3d cci = new CurveCurveIntersector3d(ray3d, curve3d, Vector3d.ZAxis))
                        {
                            //если пересечения есть
                            if (cci.NumberOfIntersectionPoints > 0)
                            {
                                for (int i = 0; i < cci.NumberOfIntersectionPoints; i++)
                                {
                                    //если пересечение проходящее то добавляем его в список
                                    if (cci.IsTransversal(i))
                                    {
                                        vpoints.Add(cci.GetIntersectionPoint(i));
                                    }
                                }
                                //если кривых несколько то не добавляем дублирующиеся точки
                                //(что бы обойти задваивание пересечений в точке начала одной кривой и конца другой)
                                if (curves.Count > 1)
                                {
                                    //проходим по точкам и удаляем существующие в общем списке
                                    for (int i = vpoints.Count - 1; i > -1; i--)
                                    {
                                        if (points.Contains(vpoints[i])) vpoints.RemoveAt(i);
                                    }
                                }
                                //добавляем точки пересечения с этой кривой в общий список
                                points.AddRange(vpoints);
                            }
                        }
                    }
                }
                catch
                {
                    return null;
                }
            }
            return points;
        }
    }
}
