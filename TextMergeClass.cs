using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using static BaseFunction.BaseLayerClass;
using static BaseFunction.TextBounds;

namespace BaseFunction
{
    public static class TextMergeClass
    {
        public static List<MText> TextMergeMass(List<Text> texts, double dopusk)
        {
            List<MText> result = new List<MText>();
            LayerNew("_NewMTexts");
            foreach (Text t in texts)
            {
                if (t.TextType == TextType.SimpleMText) result.Add(t.MText);
                else if (t.TextType == TextType.Fragmented) result.Add(TextMerge(t, dopusk));
            }
            return result;
        }
        #region overloads
        public static List<MText> TextMergeMass(List<object> texts, double dopusk, double textDopusk, string layer, bool layerCreated)
        {
            return TextMergeMass(CreateTexts(texts, textDopusk, layer, layerCreated), dopusk);
        }
        public static List<MText> TextMergeMass(List<object> texts, double dopusk)
        {
            return TextMergeMass(CreateTexts(texts, 1), dopusk);
        }
        public static List<MText> TextMergeMass(List<object> texts)
        {
            return TextMergeMass(texts, 0.5);
        }
        #endregion

        public static MText TextMerge(Text text, double dopusk)
        {

            if (text.TextType == TextType.none) return null;
            if (text.TextType == TextType.SimpleMText) return text.MText;
            if (text.TextType != TextType.Fragmented || text.DBTextsAndBounds.Count <= 2) return null;

            DBText dBText = text.DBTextsAndBounds[0].Text as DBText;
            if (dBText == null) return null;

            double csr = 0;
            if (System.Convert.ToInt32(Autodesk.AutoCAD.ApplicationServices.Application.GetSystemVariable("WORLDUCS")) == 0)
            {
                Vector3d xAxT = Autodesk.AutoCAD.ApplicationServices.Application.
                    DocumentManager.MdiActiveDocument.Editor.CurrentUserCoordinateSystem.
                    CoordinateSystem3d.Xaxis.ProjectTo(new Plane().Normal, Vector3d.ZAxis);
                Vector3d xAx = Vector3d.XAxis;
                csr = Math.Acos((xAx.X * xAxT.X + xAx.Y * xAxT.Y) / (Math.Sqrt(xAx.X * xAx.X + xAx.Y * xAx.Y) * Math.Sqrt(xAxT.X * xAxT.X + xAxT.Y * xAxT.Y)));
                if (csr == double.NaN) csr = 0;
            }

            MText m = new MText
            {
                TextHeight = dBText.Height,
                TextStyleId = dBText.TextStyleId,
                LineWeight = dBText.LineWeight,
                Layer = text.Layer,
                Rotation = dBText.Rotation - csr,             
            };

            string contents = "";

            List<Point3d> xLeftP = new List<Point3d>();
            List<Point3d> xRightP = new List<Point3d>();

            foreach (ObjectAndBound objectAndBound in text.DBTextsAndBounds)
            {
                xLeftP.Add(objectAndBound.Position);
                xRightP.Add(objectAndBound.PositionRight);
            }

            double shirina = 0;
            
            List<double> xLeft = new List<double>();
            List<double> xRight = new List<double>();

            List<ObjectAndBound> yList = new List<ObjectAndBound>();
            yList.AddRange(text.DBTextsAndBounds);
            yList.SortCoord(false);

            Point3d location;
            Point3d locationRight;

            while (true)
            {
                List<ObjectAndBound> xList = new List<ObjectAndBound>();
                double xStart = yList[0].Position.Y;
                double height = yList[0].DBText.Height;
                xList.Add(yList[0]);
                yList.RemoveAt(0);

                double leftX = xList[0].Position.X;
                double rightX = xList[0].PositionRight.X;

                while (true)
                {
                    if (yList.Count == 0 || (xStart - yList[0].Position.Y) > (dopusk * height)) break;

                    if (yList[0].Position.X < leftX) leftX = yList[0].Position.X;
                    if (yList[0].PositionRight.X > rightX) rightX = yList[0].PositionRight.X;

                    xList.Add(yList[0]);
                    yList.RemoveAt(0);
                }

                xList.SortCoord(true);

                string line = "";

                foreach (ObjectAndBound t in xList)
                {
                    if (t.DBText != null)
                    { 
                        if (!string.IsNullOrEmpty(line)) line += " ";
                        line += t.DBText.TextString;
                    }   
                }

                if (!string.IsNullOrEmpty(line))
                {
                    if (!string.IsNullOrEmpty(contents)) contents += "\n";
                    contents += line;
                    xLeft.Add(leftX);
                    xRight.Add(rightX);
                    if (shirina < rightX - leftX) shirina = rightX - leftX;
                }

                if (yList.Count == 0)
                {
                    location = xList[0].Position;
                    locationRight = xList[0].PositionRight;
                    break;
                }
            }

            xLeft.Sort();
            xRight.Sort();

            Vector3d v = (locationRight - location).GetNormal();

            location = new Point3d(xLeft[0], location.Y, 0);

            locationRight = location + v * shirina;         

            AttachmentPoint attachmentPoint = AttachmentPoint.BottomCenter;
            if (((xLeft[xLeft.Count - 1] - xLeft[0]) / shirina) < 0.1) attachmentPoint = AttachmentPoint.BottomLeft;
            else if (((xRight[xRight.Count - 1] - xRight[0]) / shirina) < 0.1) attachmentPoint = AttachmentPoint.BottomRight;

            m.Attachment = attachmentPoint;

            if (attachmentPoint == AttachmentPoint.BottomLeft) m.Location = location;
            else if (attachmentPoint == AttachmentPoint.BottomRight) m.Location = locationRight;
            else m.Location = location + (locationRight - location) * 0.5;

            m.Contents = contents;

            return m;
        }
        #region overloads
        public static MText TextMerge(List<object> texts, double dopusk, string layer, bool layerCreated)
        {
            Text t = new Text(texts, layer, layerCreated);
            return TextMerge(t, dopusk);
        }
        public static MText TextMerge(List<object> texts, double dopusk)
        {
            Text t = new Text(texts);
            return TextMerge(t, dopusk);
        }
        public static MText TextMerge(List<object> texts)
        {
            return TextMerge(texts, 0.5);
        }
        #endregion

        #region privates
        private static List<Text> CreateTexts(List<object> objects, double textsDopusk, string layer, bool layerCreated)
        {
            List<Text> result = new List<Text>();
            List<ObjectAndBound> objectAndBounds = new List<ObjectAndBound>();
            foreach (object obj in objects)
            {
                objectAndBounds.Add(new ObjectAndBound(obj));
            }
            while (objectAndBounds.Count > 0)
            {
                List<ObjectAndBound> current = new List<ObjectAndBound>();
                current.Add(objectAndBounds[0]);
                objectAndBounds.RemoveAt(0);

                if (objectAndBounds.Count > 0 && current[0].Bound.GetLength() != 0)
                {
                    List<Curve3d> curCurves = new List<Curve3d>() { current[0].Bound.GetGeCurve() };
                    bool Added = true;
                    
                    while (Added)
                    {          
                        Added = false;
                        for (int i = objectAndBounds.Count - 1; i >= 0; i--)
                        {
                            Polyline p = objectAndBounds[i].Bound;
                            if (p.GetLength().IsEqualTo(0)) continue;

                            Curve3d nCur = p.GetGeCurve();

                            bool closest = false;

                            foreach (Curve3d cur in curCurves)
                            {
                                double dist = cur.GetDistanceTo(nCur);
                                if (cur.GetDistanceTo(nCur) < textsDopusk)
                                {
                                    closest = true;
                                    break;
                                }
                            }
                            if (closest)
                            {
                                current.Add(objectAndBounds[i]);
                                objectAndBounds.RemoveAt(i);
                                curCurves.Add(nCur);
                                Added = true;
                            }
                            else nCur?.Dispose();
                        }
                    }
                    foreach (Curve3d cur in curCurves) cur.Dispose();
                }
                List<object> currObj = new List<object>();
                foreach (ObjectAndBound o in current) currObj.Add(o.Text);
                Text t = new Text(currObj, layer, layerCreated);
                if (t.TextType != TextType.none) result.Add(t);
            }
            return result;
        }
        private static List<Text> CreateTexts(List<object> objects, double textsDopusk)
        {
            return CreateTexts(objects, textsDopusk, "_NewMTexts", false);
        }
        private static void SortCoord(this List<ObjectAndBound> objectAndBounds, bool X)
        {
            List<ObjectAndBound> newList = new List<ObjectAndBound>();
            while (objectAndBounds.Count > 0)
            {
                double peremennaya;
                ObjectAndBound current = objectAndBounds[0];
                if (X) peremennaya = current.Position.X;
                else peremennaya = current.Position.Y;
                foreach (ObjectAndBound o in objectAndBounds)
                {
                    double pcurr;
                    if (X) pcurr = o.Position.X;
                    else pcurr = o.Position.Y;

                    if ((X && pcurr < peremennaya) || (!X && pcurr > peremennaya))
                    {
                        peremennaya = pcurr;
                        current = o;
                    }
                }
                newList.Add(current);
                objectAndBounds.Remove(current);
            }
            objectAndBounds.AddRange(newList);
        }
        #endregion
    }
    public class ObjectAndBound
    {
        public ObjectAndBound(object o)
        {
            if (o is MText m)
            {
                Text = m;
                MText = m;
                Bound = CreatePolyline(m);
            }
            else if (o is DBText t)
            {
                Text = t;
                DBText = t;
                Bound = CreatePolyline(t);
            }
            else return;
            if (!Bound.Bounds.HasValue) return;

            Position = Bound.Bounds.Value.MinPoint;
            PositionRight = new Point3d(Bound.Bounds.Value.MaxPoint.X, Bound.Bounds.Value.MinPoint.Y, 0);

            Exits = true;
        }
        public bool Exits { get; private set; } = false;
        public object Text { get; private set; } = null;
        public DBText DBText { get; private set; } = null;
        public MText MText { get; private set; } = null;
        public Polyline Bound { get; private set; } = null;
        public Point3d Position { get; private set; }
        public Point3d PositionRight { get; private set; }
    }
    public class Text
    {
        public Text(List<object> objects, string layer, bool layerCreated)
        {
            List<ObjectAndBound> objectAndBounds = new List<ObjectAndBound>();
            foreach (object o in objects)
            {
                ObjectAndBound t = new ObjectAndBound(o);
                if (!t.Exits) continue;
                objectAndBounds.Add(t);
            }
            Create(objectAndBounds, layer, layerCreated);
        }
        public Text(List<object> objects)
        {
            List<ObjectAndBound> objectAndBounds = new List<ObjectAndBound>();
            foreach (object o in objects)
            {
                ObjectAndBound t = new ObjectAndBound(o);
                if (!t.Exits) continue;
                objectAndBounds.Add(t);
            }
            Create(objectAndBounds, "_NewMTexts", false);
        }
        public Text(List<ObjectAndBound> objects, string layer, bool layerCreated)
        {
            Create(objects, layer, layerCreated);
        }
        public Text(List<ObjectAndBound> objects)
        {
            Create(objects, "_NewMTexts", false);
        }

        public MText MText { get; private set; } = null;
        public List<ObjectAndBound> DBTextsAndBounds { get; private set; } = new List<ObjectAndBound>();
        public TextType TextType { get; private set; } = TextType.none;
        public string Layer { get; private set; }

        #region privates
        private void Create(List<ObjectAndBound> objects, string layer, bool layerCreated)
        {
            Layer = layer;
            if (!layerCreated) LayerNew(layer);
            if (objects.Count == 0) return;
            if (objects.Count == 1)
            {
                if (objects[0].Text is MText m) MText = GetMtextFromMtext(m, layer);
                else if (objects[0].Text is DBText t) MText = GetMtextFromDBText(t, layer);
                else return;
                TextType = TextType.SimpleMText;
            }
            else
            {
                DBTextsAndBounds = GetDBTextFromObjects(objects, layer);
                if (DBTextsAndBounds.Count == 0) return;
                if (DBTextsAndBounds.Count == 1)
                {
                    MText = GetMtextFromDBText(DBTextsAndBounds[0].Text as DBText, layer);
                    TextType = TextType.SimpleMText;
                    return;
                }
                TextType = TextType.Fragmented;
            }
        }
        private List<ObjectAndBound> GetDBTextFromObjects(List<ObjectAndBound> objects, string layer)
        {
            List<ObjectAndBound> result = new List<ObjectAndBound>();
            foreach (ObjectAndBound o in objects)
            {
                if (o.Text is DBText t && !result.Contains(o))
                {
                    t.Layer = layer;
                    result.Add(o);
                }
                else if (o.Text is MText m)
                {
                    using (DBObjectCollection coll = new DBObjectCollection())
                    {
                        m.Explode(coll);
                        foreach (DBObject ob in coll)
                        {
                            if (ob is DBText dt)
                            {
                                dt.Layer = layer;
                                ObjectAndBound newOAB = new ObjectAndBound(dt);
                                if (newOAB.Exits)
                                {
                                    result.Add(newOAB);
                                }
                                else ob?.Dispose();
                            }
                            else ob?.Dispose();
                        }
                    }
                }
            }
            return result;
        }
        private MText GetMtextFromDBText(DBText t, string layer)
        {
            double csr = 0;
            if (System.Convert.ToInt32(Autodesk.AutoCAD.ApplicationServices.Application.GetSystemVariable("WORLDUCS")) == 0)
            {
                Vector3d xAxT = Autodesk.AutoCAD.ApplicationServices.Application.
                    DocumentManager.MdiActiveDocument.Editor.CurrentUserCoordinateSystem.
                    CoordinateSystem3d.Xaxis.ProjectTo(new Plane().Normal, Vector3d.ZAxis);
                Vector3d xAx = Vector3d.XAxis;
                csr = Math.Acos((xAx.X * xAxT.X + xAx.Y * xAxT.Y) / (Math.Sqrt(xAx.X * xAx.X + xAx.Y * xAx.Y) * Math.Sqrt(xAxT.X * xAxT.X + xAxT.Y * xAxT.Y)));
                if (csr == double.NaN) csr = 0;
            }
            MText m = new MText();
            if (
                t.VerticalMode == TextVerticalMode.TextBase ||
                t.HorizontalMode == TextHorizontalMode.TextLeft ||
                t.HorizontalMode == TextHorizontalMode.TextAlign ||
                t.HorizontalMode == TextHorizontalMode.TextFit
                ) m.Location = t.Position;
            else m.Location = t.AlignmentPoint;
            m.Attachment = t.Justify;
            m.TextHeight = t.Height;
            m.TextStyleId = t.TextStyleId;
            m.Contents = t.TextString;
            m.Color = t.Color;
            m.Layer = layer;
            m.LineWeight = t.LineWeight;
            m.Rotation = t.Rotation - csr;
            return m;
        }
        private MText GetMtextFromMtext(MText t, string layer)
        {
            MText m = t.Clone() as MText;
            m.Layer = layer;
            return m;
        }
        #endregion
    }
    public enum TextType
    {
        none,
        SimpleMText,
        Fragmented,
    }
}
