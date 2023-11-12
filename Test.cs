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
        [CommandMethod("test01")]
        public void Test01()
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
                            if (mtext.IsNewObject)
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
