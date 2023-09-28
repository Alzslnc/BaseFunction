using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using static BaseFunction.BaseGeometryClass;
using static BaseFunction.BaseGetObjectClass;
using static BaseFunction.PositionAndIntersections;
using static BaseFunction.F;

namespace BaseFunction
{
    public class Test
    {
        //[CommandMethod("test01")]
        //public void Test01()
        //{
        //    if (!TryGetobjectId(out ObjectId id, typeof(Polyline))) return;
        //    if (!TryGetobjectId(out ObjectId id2, typeof(Circle))) return;
        //    Polyline p1 = id.Open(OpenMode.ForRead) as Polyline;
        //    Circle p2 = id2.Open(OpenMode.ForRead) as Circle;
        //    if (p1 == null || p2 == null) return;
        //    p1.TryGetIntersections(p2, out List<Point3d> result);
        //    foreach (Point3d p in result)
        //    {
        //        using (Circle c = new Circle(p, Vector3d.ZAxis, 0.5))
        //        {
        //            c.AddEntityInCurrentBTR();
        //        }
        //    }
        //    p1.Close();
        //    p2.Close();
        //}

    }
}
