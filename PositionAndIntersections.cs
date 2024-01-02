using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BaseFunction
{
    public static class PositionAndIntersections
    {
        /// <summary>
        /// соединяет фрагменты кривой и возвращает список с результатом соединения. Возвращает false если произошла ошибка.
        /// </summary>   
        public static bool ConnectCurve(this List<Curve> fragments, out List<Curve> result)
        {
            result = new List<Curve>();
               
            while (fragments.Count > 0)
            {
                Curve contour = null;

                foreach (Curve c in fragments)
                {
                    if (c is Spline)
                    {
                        contour = c;
                        fragments.Remove(c);
                        break;
                    }                
                }

                if (contour == null)
                {
                    contour = fragments[0];
                    fragments.RemoveAt(0);
                }               
                
                if (contour is Arc arc)
                {
                    Polyline polyline = new Polyline();
                    polyline.AddVertexAt(0, new Point2d(arc.StartPoint.X, arc.StartPoint.Y), arc.GetArcBulge(), 0, 0);
                    polyline.AddVertexAt(1, new Point2d(arc.EndPoint.X, arc.EndPoint.Y), 0, 0, 0);
                    polyline.Normal = arc.Normal;
                    polyline.EntityCopySettings(arc);                    
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
                            //if (!fragment.StartPoint.IsEqualTo(contour.EndPoint) && fragment.EndPoint.IsEqualTo(contour.EndPoint)) fragment.ReverseCurve();

                            if (fragment.StartPoint.IsEqualTo(contour.EndPoint) || fragment.EndPoint.IsEqualTo(contour.EndPoint))
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
        public static PositionType CurveOfCurve(this Curve c1, Curve c2)
        {
            if (c1.GetCentrPoint(out Point3d center))
            {
                return center.GetPositionType(c2);
            }
            return PositionType.fault;
        }
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
        /// <summary>
        /// разделяет замкнутую кривую другой замкнутой кривой и возвращает получившиеся фрагменты
        /// </summary>
        /// <param name="poly1">разрезаемая кривая</param>
        /// <param name="poly2">разрезающая кривая</param>
        /// <param name="inner">фрагменты внутри разрезающей кривой</param>
        /// <param name="outer">фрагменты снаружи разрезающей кривой</param>
        /// <param name="result">все получившиеся фрагменты</param>
        /// <returns></returns>
        public static bool SplitCurve(this Curve poly1, Curve poly2, bool inPlane, out List<Curve> inner, out List<Curve> outer, out List<Curve> result)
        {
            inner = new List<Curve>();
            outer = new List<Curve>();
            result = new List<Curve>();

            List<Curve> poly1fragments = new List<Curve>();
            List<Curve> poly2fragments = new List<Curve>();

            Curve intersectCurve = poly2 as Curve;

            //проверяем плоская ли кривая и получаем ее плоскость
            bool planar = poly1.IsPlanar;
            Plane plane = null;
            if (planar) plane = poly1.GetPlane();

            if (inPlane && planar)
            {
                intersectCurve = poly2.GetProjectedCurve(plane, Vector3d.ZAxis);
                if (intersectCurve is Polyline3d p3d && intersectCurve.ObjectId == ObjectId.Null)
                { 
                    if (!p3d.AddEntityInCurrentBTR()) return false;
                }
            }

            //список для фагментов 3д полилиний,
            //их требуется добавить в базу данных иначе с ними нельзя будет полноценно работать дальше
            List<Entity> curveToAppend = new List<Entity>();
            //список для лишних фрагментов 3д полилиний, которые требуется удалить
            List<Entity> curveToDelete = new List<Entity>();

            using (Point3dCollection coll = new Point3dCollection())
            {
                poly1.IntersectWith(intersectCurve, Intersect.OnBothOperands, coll, IntPtr.Zero, IntPtr.Zero);

                if (coll.Count == 0 || !poly1.IsIntersect(intersectCurve, coll.ToList())) return false;        

                List<double> parametrs = new List<double>();

                foreach (Point3d p in coll) parametrs.Add(poly1.GetParameterAtPoint(poly1.GetClosestPointTo(p, false)));
                parametrs.Sort();

                using (DBObjectCollection pColl = poly1.GetSplitCurves(new DoubleCollection(parametrs.ToArray())))
                {
                    foreach (DBObject dBObject in pColl)
                    {
                        if (dBObject is Curve curve)
                        {
                            poly1fragments.Add(curve);
                            if (curve is Polyline3d) curveToAppend.Add(curve);                       
                        }
                        else dBObject?.Dispose();
                    }
                }

                parametrs.Clear();
                foreach (Point3d p in coll) parametrs.Add(intersectCurve.GetParameterAtPoint(intersectCurve.GetClosestPointTo(p, false)));
                parametrs.Sort();

                using (DBObjectCollection pColl = intersectCurve.GetSplitCurves(new DoubleCollection(parametrs.ToArray())))
                {
                    foreach (DBObject dBObject in pColl)
                    {
                        if (dBObject is Curve curve)
                        {
                            poly2fragments.Add(curve);
                            if (curve is Polyline3d) curveToAppend.Add(curve);
                        }
                        else dBObject?.Dispose();
                    }
                }
            }

            //добавляем в базу данных 3д полилинии
            if (curveToAppend.Count > 0)
            {
                curveToAppend.AddEntityInCurrentBTR();
                curveToAppend.Clear();
            }

            //распределяем
            foreach (Curve curve in poly1fragments)
            {
                PositionType position = curve.CurveOfCurve(intersectCurve);
                if (position == PositionType.inner) inner.Add(curve);
                else if (position == PositionType.outer) outer.Add(curve);
                else
                {
                    if (curve.ObjectId != ObjectId.Null) curveToDelete.Add(curve);
                    continue;
                }

                curve.EntityCopySettings(poly1);     
            }          

            foreach (Curve curve in poly2fragments)
            {
                bool notInPlaneOrIncorrect = false;
                //проверяем лежит ли фрагмент в плоскости если кривая плоская
                if (planar &&
                    (!curve.GetCentrPoint(out Point3d center) || !plane.DistanceTo(center).IsEqualTo(0))
                    ) notInPlaneOrIncorrect = true;

                if (!notInPlaneOrIncorrect)
                {
                    PositionType position = curve.CurveOfCurve(poly1);
                    if (position == PositionType.inner || position == PositionType.onBound)
                    {
                        inner.Add(curve);
                        Curve clone = curve.Clone() as Curve;
                        outer.Add(clone);
                       
                        curve.EntityCopySettings(poly1);
                        clone.EntityCopySettings(poly1);
                    }  
                    else notInPlaneOrIncorrect = true;
                }
            
                if (notInPlaneOrIncorrect && curve.ObjectId != ObjectId.Null) curveToDelete.Add(curve);
            }

            //добавляем в базу данных клоны 3д полилинии
            if (curveToAppend.Count > 0) curveToAppend.AddEntityInCurrentBTR();
            if (curveToDelete.Count > 0) curveToDelete.DeleteEntity();

            bool boolResult;

            boolResult = inner.ConnectCurve(out inner);

            if (boolResult) boolResult = outer.ConnectCurve(out outer);

            if (boolResult)
            {
                result.AddRange(inner);
                result.AddRange(outer);
            }
            else
            {
                curveToAppend.DeleteEntity();
            }
            
            return boolResult;
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
