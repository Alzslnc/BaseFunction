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
using static BaseFunction.BaseBlockReferenceClass;
using static BaseFunction.F;
using static BaseFunction.TextMergeClass;
using static BaseFunction.BaseLayerClass;

namespace BaseFunction
{
    public class Test
    {
        [CommandMethod("Test01")]
        public void Test01()
        {
            if (!TryGetobjectId(out ObjectId contourid, new List<Type>
            { typeof(Arc), typeof(Polyline), typeof(Line), typeof(Circle) }, "Выберите контур в плане")) return;
            if (!TryGetPointFromUser(out Point3d point, true, "Выберите точку на фасаде соответствующую началу контура на нулевой отметке", null)) return;
            if (!TryGetObjectsIds(out List<ObjectId> ids, typeof(BlockReference), "Выберите закладные")) return;

            using (Transaction tr = HostApplicationServices.WorkingDatabase.TransactionManager.StartTransaction())
            {
                using (Curve c = tr.GetObject(contourid, OpenMode.ForRead, false, true) as Curve)
                {
                    double length = c.GetLength();

                    double x = point.X;
                    double y = point.Y;

                    if (c != null)
                    {
                        List<Entity> entities = new List<Entity>();

                        foreach (ObjectId id in ids)
                        {
                            using (BlockReference br = tr.GetObject(id, OpenMode.ForRead) as BlockReference)
                            {
                                if (br == null) continue;

                                double offset = br.Position.X - x;
                                if (offset <= 0 || offset > length) continue;

                                double height = br.Position.Y - y;

                                Point3d position = c.GetPointAtDist(offset);

                                entities.Add(new DBText
                                {
                                    Position = position,
                                    TextString = height.ToString("F2"),
                                    Justify = AttachmentPoint.BaseLeft
                                });

                                entities.Add(new DBPoint(position));
                            }
                        }

                        entities.AddEntityInCurrentBTR();
                    }
                }
                tr.Commit();
            }

        }

        [CommandMethod("test02")]
        public void Test02()
        {
            if (!TryGetObjectsIds(out List<ObjectId> ids, new List<Type> { typeof(MText), typeof(DBText) })) return;
            using (Transaction tr = HostApplicationServices.WorkingDatabase.TransactionManager.StartTransaction())
            {      
                List<object> texts = new List<object>();
                foreach (ObjectId id in ids)
                {
                    using (Entity e = tr.GetObject(id, OpenMode.ForRead, false, true) as Entity)
                    { 
                        if (e is MText || e is DBText) texts.Add(e.Clone());   
                    }
                }

                LayerNew("_mtexts");
                List<MText> mtexts = TextMergeMass(texts, 0.5, 1, "_mtexts", true);

                if (mtexts.Count > 0)
                {
                    using (BlockTableRecord ms = tr.GetObject(HostApplicationServices.WorkingDatabase.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord)
                    {
                        foreach (MText mtext in mtexts)
                        {
                            if (mtext != null &&  mtext.IsNewObject)
                            { 
                                ms.AppendEntity(mtext);
                                tr.AddNewlyCreatedDBObject(mtext, true);
                            }
                        }
                    }
                }

                tr.Commit();
            }
        }

    }
}
