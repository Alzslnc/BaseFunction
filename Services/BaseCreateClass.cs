using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;

namespace BaseFunction
{
    public static class BaseCreateClass
    {
        /// <summary>
        /// Базовый метод-помощник для ультра-быстрого создания объекта Polyline по списку плоских координат.
        /// </summary>
        /// <param name="points">Список плоских координат Point2d (в метрах)</param>
        /// <param name="isClosed">Флаг замкнутости контура (true для слоев засыпки, false для разомкнутой траншеи)</param>
        /// <param name="colorIndex">Индекс цвета AutoCAD (например, 7 - белый/черный, 256 - ByLayer)</param>
        /// <param name="weight">Вес линии (LineWeight)</param>
        /// <returns>Готовый, скомпилированный объект Polyline в оперативной памяти</returns>
        public static Polyline CreatePolyline(List<Point2d> points, bool isClosed, short colorIndex = 256, LineWeight weight = LineWeight.ByLayer)
        {
            if (points == null || points.Count < 2) return null;

            Polyline pl = new Polyline();
            for (int i = 0; i < points.Count; i++)
            {
                // Добавляем вершины: индекс, точка, прогиб(bulge)=0, старт_ширина=0, финиш_ширина=0
                pl.AddVertexAt(i, points[i], 0, 0, 0);
            }

            pl.Closed = isClosed;
            pl.ColorIndex = colorIndex;
            pl.LineWeight = weight;

            return pl;
        }

        public static MLeader CreateMLeader(MText mText, Point3d point, Vector3d? direction = null, double xShift = 1, double yShift = 1)
        {
            // Исправляем логику null: теперь значение гарантированно запишется в переменную, если пришел null
            Vector3d actualDirection = direction ?? Vector3d.XAxis;

            MLeader mLeader = new MLeader()
            {
                ArrowSize = 0,
                LandingGap = 0,
                ContentType = ContentType.MTextContent
            };

            mLeader.MText = mText;

            // Инициализируем кластер выноски
            int leaderIndex = mLeader.AddLeader();

            // ВАЖНО: сохраняем индекс созданной ЛИНИИ выноски
            int lineIndex = mLeader.AddLeaderLine(leaderIndex);

            // Вершины добавляем именно к ЛИНИИ (lineIndex), а не к кластеру (leaderIndex)
            mLeader.AddFirstVertex(lineIndex, point);
            mLeader.AddLastVertex(lineIndex, Point3d.Origin);

            mLeader.TextLocation = (point + Vector3d.XAxis * xShift + Vector3d.YAxis * yShift);

            mLeader.EnableDogleg = true;

            // Для SetDogleg нужен индекс кластера выноски (leaderIndex) и очищенный от null вектор
            mLeader.SetDogleg(leaderIndex, actualDirection);
            mLeader.DoglegLength = 0;

            // привязка текста
            mLeader.SetTextAttachmentType(TextAttachmentType.AttachmentBottomOfTopLine, LeaderDirectionType.BottomLeader);
            mLeader.SetTextAttachmentType(TextAttachmentType.AttachmentBottomOfTopLine, LeaderDirectionType.RightLeader);
            mLeader.SetTextAttachmentType(TextAttachmentType.AttachmentBottomOfTopLine, LeaderDirectionType.LeftLeader);
            mLeader.SetTextAttachmentType(TextAttachmentType.AttachmentBottomOfTopLine, LeaderDirectionType.TopLeader);

            return mLeader;
        }

        /// <summary>
        /// Универсальный метод создания штриховки с поддержкой многоуровневого контроля вложенности островков.
        /// </summary>
        /// <param name="tr">Текущая открытая транзакция.</param>
        /// <param name="targetBtrId">ID контейнера (Блока или Пространства), куда добавляется штриховка.</param>
        /// <param name="externalIds">Список ID верхних внешних границ (External).</param>
        /// <param name="outermostInternalIds">Список ID островков первого уровня вложенности (Outermost вычеты).</param>
        /// <param name="defaultInternalIds">Список ID всех остальных глубоких островков (Default).</param>
        /// <param name="patternName">Имя паттерна ("SOLID", "ANSI31" и т.д.).</param>
        /// <param name="scale">Масштаб узора штриховки.</param>
        /// <param name="layerName">Имя целевого слоя.</param>
        /// <returns>Созданный и вычисленный примитив Hatch.</returns>
        public static Hatch CreateHatch(
            Transaction tr,
            ObjectId targetBtrId,
            List<ObjectId> externalIds,
            List<ObjectId> outermostInternalIds,
            List<ObjectId> defaultInternalIds,
            string patternName = "SOLID",
            double scale = 1.0,
            string layerName = "0")
        {
            if (externalIds == null || externalIds.Count == 0) return null;

            BlockTableRecord btr = (BlockTableRecord)tr.GetObject(targetBtrId, OpenMode.ForWrite);

            Hatch hatch = new Hatch();
            hatch.Layer = layerName;
            hatch.Associative = false;

            // Настройка стиля штриховки: 
            // Если у нас есть только первый уровень вложенности, используем Outer (Outermost).
            // Если есть глубокие вложенные примитивы (через один), переключаем на Normal.
            hatch.HatchStyle = (defaultInternalIds != null && defaultInternalIds.Count > 0)
                ? HatchStyle.Normal
                : HatchStyle.Outer;

            // Настройка паттерна
            string upperPattern = string.IsNullOrEmpty(patternName) ? "SOLID" : patternName.ToUpper();
            if (upperPattern == "SOLID")
            {
                hatch.SetHatchPattern(HatchPatternType.PreDefined, "SOLID");
            }
            else
            {
                hatch.PatternScale = scale <= 0 ? 1.0 : scale;
                hatch.SetHatchPattern(HatchPatternType.PreDefined, upperPattern);
                hatch.PatternScale = scale <= 0 ? 1.0 : scale;
            }

            // ⚡ Сначала регистрируем штриховку в базе данных, чтобы AppendLoop не падал
            btr.AppendEntity(hatch);
            tr.AddNewlyCreatedDBObject(hatch, true);

            // 1. Добавляем внешние границы (External / Outermost)
            foreach (ObjectId extId in externalIds)
            {
                if (extId == ObjectId.Null) continue;
                hatch.AppendLoop(HatchLoopTypes.External, new ObjectIdCollection(new[] { extId }));
            }

            // 2. Добавляем вычитаемые островки первого уровня (Outermost)
            if (outermostInternalIds != null)
            {
                foreach (ObjectId outerInternalId in outermostInternalIds)
                {
                    if (outerInternalId == ObjectId.Null) continue;
                    // Маркируем как внешний остров (в комбинации с HatchStyle.Outer это даст чистый вырез)
                    hatch.AppendLoop(HatchLoopTypes.Outermost, new ObjectIdCollection(new[] { outerInternalId }));
                }
            }

            // 3. Добавляем глубокие внутренние островки (Default)
            if (defaultInternalIds != null)
            {
                foreach (ObjectId defInternalId in defaultInternalIds)
                {
                    if (defInternalId == ObjectId.Null) continue;
                    hatch.AppendLoop(HatchLoopTypes.Default, new ObjectIdCollection(new[] { defInternalId }));
                }
            }

            // Вычисляем геометрию пересечений
            hatch.EvaluateHatch(true);

            return hatch;
        }
        public static Dimension CreateDimension(Point3d startPoint, Point3d endPoint, double offset)
        {
            // Создаем временную линию. Поскольку мы не добавляем её в базу данных (ms), 
            // блок using корректно уничтожит её в памяти после завершения работы.
            using (Line temporaryLine = new Line(startPoint, endPoint))
            {
                // Передаем временную линию в основной метод. 
                // Флаги returnArcLength и returnDiameter здесь не имеют значения, так как это Line.
                return CreateDimension(temporaryLine, offset);
            }
        }
        public static Dimension CreateDimension(Curve curve, double offset, bool returnArcLength = false, bool returnDiameter = true)
        {
            CoordinateSystem3d coordinate = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Editor.CurrentUserCoordinateSystem.CoordinateSystem3d;

            if (curve is Line)
            {
                if (!curve.GetCentrPoint(out Point3d point)) return null;

                AlignedDimension dim = new AlignedDimension();
                dim.Normal = coordinate.Zaxis;
                dim.XLine1Point = curve.StartPoint;
                dim.XLine2Point = curve.EndPoint;
                dim.DimLinePoint = point + (curve.EndPoint - curve.StartPoint).TransformBy(Matrix3d.Rotation(Math.PI / 2, Vector3d.ZAxis, point)).GetNormal() * offset;
                dim.Layer = "!_dimension";
                return dim;
            }

            if (curve is Circle circle)
            {
                if (returnDiameter)
                {
                    DiametricDimension dim = new DiametricDimension();
                    dim.Normal = coordinate.Zaxis;
                    dim.FarChordPoint = circle.StartPoint + (circle.Center - circle.StartPoint) * 2;
                    dim.ChordPoint = circle.StartPoint;
                    dim.Layer = "!_dimension";
                    return dim;
                }
                else
                {
                    RadialDimension dim = new RadialDimension();
                    dim.Normal = coordinate.Zaxis;
                    dim.Center = circle.Center;
                    dim.ChordPoint = circle.StartPoint;
                    dim.Layer = "!_dimension";
                    return dim;
                }
            }

            if (curve is Arc arc)
            {
                if (!curve.GetCentrPoint(out Point3d point)) return null;

                // 1. Наивысший приоритет для дуги — длина, если флаг включен
                if (returnArcLength)
                {
                    ArcDimension dim = new ArcDimension(arc.Center, arc.StartPoint, arc.EndPoint, point + (point - arc.Center).GetNormal() * offset,
                        "<>", HostApplicationServices.WorkingDatabase.Dimstyle);
                    dim.Normal = coordinate.Zaxis;
                    dim.Layer = "!_dimension";
                    return dim;
                }

                // 2. Вторичный приоритет — диаметр или радиус по первому флагу
                if (returnDiameter)
                {
                    DiametricDimension dim = new DiametricDimension();
                    dim.Normal = coordinate.Zaxis;
                    dim.FarChordPoint = point + (arc.Center - point) * 2;
                    dim.ChordPoint = point;
                    dim.Layer = "!_dimension";
                    return dim;
                }
                else
                {
                    RadialDimension dim = new RadialDimension();
                    dim.Normal = coordinate.Zaxis;
                    dim.Center = arc.Center;
                    dim.ChordPoint = point;
                    dim.Layer = "!_dimension";
                    return dim;
                }
            }

            return null;
        }

    }
}

