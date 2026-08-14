using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.Civil.DatabaseServices;
using BaseFunction;
using Progress;
using System;
using System.Collections.Generic;
using System.Linq;
using Entity = Autodesk.AutoCAD.DatabaseServices.Entity;
using Region = Autodesk.AutoCAD.DatabaseServices.Region;

namespace BaseFunction
{
    internal class SurfaceContourExtraction
    {
        // Хранилище вектора смещения для текущего сеанса расчета
        private static Vector3d _coordinateShift = new Vector3d(0, 0, 0);
        private static bool _isShiftCalculated = false;

        public static bool GetElevationZoneContours(
            out List<Polyline> positive,
            out List<Polyline> negative,
            out List<Polyline> zero,
            TinSurface VolumeSurfaceAsTinSurface,
            ProgressScope dialog,
            bool main,
            out double positiveArea,
            out double negativeArea,
            out double zeroArea,
            double elevation = 0)
        {

            positiveArea = 0;
            negativeArea = 0;
            zeroArea = 0;

            positive = new List<Polyline>();
            negative = new List<Polyline>();
            zero = new List<Polyline>();

            bool result = GetElevationZoneContours(out List<Curve> pos, out List<Curve> neg, out List<Curve> zer, VolumeSurfaceAsTinSurface, dialog, main, out positiveArea, out negativeArea, out zeroArea, elevation);

            foreach (Curve c in pos)
            {
                if (c is Polyline poly) positive.Add(poly);
                else c?.Dispose();
            }
            foreach (Curve c in neg)
            {
                if (c is Polyline poly) negative.Add(poly);
                else c?.Dispose();
            }
            foreach (Curve c in zer)
            {
                if (c is Polyline poly) zero.Add(poly);
                else c?.Dispose();
            }

            return result;
        }

        public static bool GetElevationZoneContours(
            out List<Curve> positive,
            out List<Curve> negative,
            out List<Curve> zero,
            TinSurface VolumeSurfaceAsTinSurface,
            ProgressScope dialog,
            bool main,
            out double positiveArea,
            out double negativeArea,
            out double zeroArea,
            double elevation = 0)
        {
            // Обязательно сбрасываем статическое смещение перед стартом команды
            ResetCoordinateShift();

            double epsilon = 1e-5;

            positiveArea = 0;
            negativeArea = 0;
            zeroArea = 0;

            positive = new List<Curve>();
            negative = new List<Curve>();
            zero = new List<Curve>();

            // Жестко и раздельно кэшируем делегаты для 100% совместимости с C# 7.3
            Action action = null;
            Action<string, int> actionRestart = null;

            if (dialog != null)
            {
                if (main)
                {
                    action = () => dialog.IncrementMainStep();
                    actionRestart = (string s, int count) => dialog.RestartMain(s, count);
                }
                else
                {
                    action = () => dialog.IncrementSubStep();
                    actionRestart = (string s, int count) => dialog.RestartSub(s, count);
                }
            }

            // Локальная функция для быстрой проверки отмены           
            bool isCancelled() => dialog?.CancellationToken.IsCancellationRequested == true;

            TinSurfaceTriangleCollection visibleTriangles = VolumeSurfaceAsTinSurface.GetTriangles(false);

            List<Point3d[]> posRegsArr = new List<Point3d[]>();
            List<Point3d[]> negRegsArr = new List<Point3d[]>();
            List<Point3d[]> zeroRegsArr = new List<Point3d[]>();

            // ШАГ 0: Геометрический анализ треугольников
            actionRestart?.Invoke("Геометрический анализ треугольников", visibleTriangles.Count);

            int step = Math.Max(1, visibleTriangles.Count / 100);
            int i = 0;
            int counter = step;

            Dictionary<TinSurfaceVertex, Point3d> vertexCache = new Dictionary<TinSurfaceVertex, Point3d>();
            TriangleSlicerCore slicerCore = new TriangleSlicerCore(epsilon);

            foreach (TinSurfaceTriangle triangle in visibleTriangles)
            {
                i++;
                SplitAndDistributeTriangle(slicerCore, triangle, elevation, vertexCache, posRegsArr, negRegsArr, zeroRegsArr);

                if (--counter == 0)
                {
                    if (main) dialog?.Update(mainCurrent: i);
                    else dialog?.Update(subCurrent: i);

                    if (isCancelled()) return false;
                    counter = step;
                }
            }

            // Проверка отмены перед ШАГОМ 1
            if (isCancelled()) return false;

            // ШАГ 1: Сборка элементарных регионов (пакетный запуск ACIS)
            actionRestart?.Invoke("Обработка шаг 1", 3);

            List<Region> posRegs = RegionTopologyService.BuildElementaryRegions(posRegsArr, epsilon);
            action?.Invoke();
            if (isCancelled()) { ClearRegionLists(posRegs, null, null); return false; }

            List<Region> negRegs = RegionTopologyService.BuildElementaryRegions(negRegsArr, epsilon);
            action?.Invoke();
            if (isCancelled()) { ClearRegionLists(posRegs, negRegs, null); return false; }

            List<Region> zeroRegs = RegionTopologyService.BuildElementaryRegions(zeroRegsArr, epsilon);
            action?.Invoke();
            if (isCancelled()) { ClearRegionLists(posRegs, negRegs, zeroRegs); return false; }

            // ШАГ 2: Булево объединение (Склейка регионов)
            int totalMergeSteps = posRegs.Count + negRegs.Count + zeroRegs.Count;
            actionRestart?.Invoke("Обработка шаг 2", totalMergeSteps);

            // Склеиваем выемку
            Region finalPositive = RegionTopologyService.MergeRegionsWithProgress(posRegs, epsilon, action, isCancelled);
            if (finalPositive != null) action?.Invoke();
            if (isCancelled()) { ClearRegionLists(null, negRegs, zeroRegs); finalPositive?.Dispose(); return false; }

            // Склеиваем насыпь
            Region finalNegative = RegionTopologyService.MergeRegionsWithProgress(negRegs, epsilon, action, isCancelled);
            if (finalNegative != null) action?.Invoke();
            if (isCancelled()) { ClearRegionLists(null, null, zeroRegs); finalPositive?.Dispose(); finalNegative?.Dispose(); return false; }

            // Склеиваем нулевые зоны
            Region finalZero = RegionTopologyService.MergeRegionsWithProgress(zeroRegs, epsilon, action, isCancelled);
            if (finalZero != null) action?.Invoke();
            if (isCancelled()) { finalPositive?.Dispose(); finalNegative?.Dispose(); finalZero?.Dispose(); return false; }

            // Фиксация инженерных площадей напрямую из ACIS
            positiveArea = finalPositive?.Area ?? 0;
            negativeArea = finalNegative?.Area ?? 0;
            zeroArea = finalZero?.Area ?? 0;

            // ШАГ 3: Извлечение графики полилиний чертежа
            int step3Count = (finalPositive == null ? 0 : 1) + (finalNegative == null ? 0 : 1) + (finalZero == null ? 0 : 1);
            actionRestart?.Invoke("Обработка шаг 3", step3Count);

            if (finalPositive != null)
            {
                positive = RegionTopologyService.ExtractClosedContours(finalPositive, _coordinateShift);
                action?.Invoke();
                finalPositive.Dispose(); // Чистим нативную память
            }
            if (isCancelled()) { finalNegative?.Dispose(); finalZero?.Dispose(); return false; }

            if (finalNegative != null)
            {
                negative = RegionTopologyService.ExtractClosedContours(finalNegative, _coordinateShift);
                action?.Invoke();
                finalNegative.Dispose();
            }
            if (isCancelled()) { finalZero?.Dispose(); return false; }

            if (finalZero != null)
            {
                zero = RegionTopologyService.ExtractClosedContours(finalZero, _coordinateShift);
                action?.Invoke();
                finalZero.Dispose();
            }

            return true;
        }
        /// <summary>
        /// Выполняет сквозную каскадную нарезку поверхности Civil 3D по группе уровней.
        /// Возвращает плоский список всех полученных треугольных осколков, жестко связанных со своими SliceLevel.
        /// </summary>
        /// <param name="surface">Исходная поверхность Civil 3D.</param>
        /// <param name="rawElevations">Сырой список отметок из интерфейса программы.</param>
        /// <param name="incrementProgress">Кэшированный делегат шага прогресс-бара для исходных треугольников.</param>
        /// <param name="isCancelled">Локальная функция или делегат опроса отмены операции.</param>
        /// <param name="epsilon">Допуск точности для формулы Гаусса и расстояний.</param>
        /// <returns>Полный список SlicedPiece, распределенный по уровням и знакам согласно топологии каскада.</returns>
        public static List<SlicedPiece> CalculateLayeredPieces(
            TinSurface surface,
            List<double> rawElevations,
            Action incrementProgress,
            Func<bool> isCancelled,
            double epsilon = 1e-5,
            bool replaceBack = false)
        {
            List<SlicedPiece> finalResults = new List<SlicedPiece>();

            if (surface == null || rawElevations == null || rawElevations.Count == 0)
                return finalResults;

            // 1. Упаковываем сырые double-отметки в наши безопасные ссылочные SliceLevel
            // Сортируем их строго по возрастанию для корректной работы каскадного сита
            List<SliceLevel> activeLevels = rawElevations
                .Distinct()
                .OrderBy(e => e)
                .Select(e => new SliceLevel(e))
                .ToList();

            // 2. Извлекаем треугольники поверхности и прогоняем их через внешний контур кэша смещения к (0,0)
            TinSurfaceTriangleCollection triangles = surface.GetTriangles(false);
            List<Point3d[]> cleanedTriangles = new List<Point3d[]>();
            Dictionary<TinSurfaceVertex, Point3d> vertexCache = new Dictionary<TinSurfaceVertex, Point3d>();

            foreach (TinSurfaceTriangle tri in triangles)
            {
                // Используем ваш стандартный метод кэширования из первого модуля
                Point3d p1 = GetOrCreateCleanedVertex(tri.Vertex1, vertexCache);
                Point3d p2 = GetOrCreateCleanedVertex(tri.Vertex2, vertexCache);
                Point3d p3 = GetOrCreateCleanedVertex(tri.Vertex3, vertexCache);

                // Быстрая предварительная фильтрация Гаусса на чистых регистрах ЦП (Рубеж №1)
                // Отсекаем исходные вырожденные треугольники Civil 3D до выделения памяти под массивы конвейера
                double gaussArea = 0.5 * Math.Abs(p1.X * (p2.Y - p3.Y) + p2.X * (p3.Y - p1.Y) + p3.X * (p1.Y - p2.Y));
                if (gaussArea < epsilon)
                {
                    incrementProgress.Invoke();
                    continue;
                }

                cleanedTriangles.Add(new Point3d[] { p1, p2, p3 });
            }

            if (isCancelled != null && isCancelled()) return finalResults;

            // 3. Запускаем каскадный конвейер нарезки. 
            // Он за один проход поштучно нашинкует все треугольники по всем уровням сразу,
            // используя наше обновлённое ядро с формулой Гаусса внутри IsPointsDegenerate.
            finalResults = SliceSurfaceMultipleLevels(
                cleanedTriangles,
                activeLevels,
                incrementProgress, // Прогресс-бар плавно ползёт по числу исходных треугольников
                isCancelled,
                epsilon
            );

            return finalResults;
        }
        public static void ReplaceBack(IEnumerable<Entity> entities)
        {
            Matrix3d back = Matrix3d.Displacement(_coordinateShift);

            foreach (Entity e in entities) e.TransformBy(back);
        }

        #region private
        // Метод для сброса смещения перед началом обработки новой поверхности
        public static void ResetCoordinateShift()
        {
            _coordinateShift = new Vector3d(0, 0, 0);
            _isShiftCalculated = false;
        }
        private static List<SlicedPiece> SliceSurfaceMultipleLevels(List<Point3d[]> cleanedTriangles,
            List<SliceLevel> activeLevels, Action incrementProgress, Func<bool> isCancelled, double epsilon)
        {
            List<SlicedPiece> globalResults = new List<SlicedPiece>();

            if (cleanedTriangles == null || cleanedTriangles.Count == 0 || activeLevels == null || activeLevels.Count == 0)
                return globalResults;

            // 1. Инициализируем атомарное математическое ядро с заданной точностью
            TriangleSlicerCore slicerCore = new TriangleSlicerCore(epsilon);

            // 2. Сортируем уровни строго снизу вверх по их реальной высоте (Value).
            // Это гарантирует, что каскадный сито-фильтр отработает корректно.
            List<SliceLevel> sortedLevels = activeLevels.OrderBy(l => l.Value).ToList();

            // 3. Запускаем главный цикл по всем исходным треугольникам поверхности
            foreach (Point3d[] triangle in cleanedTriangles)
            {
                // Опрос стоп-крана на каждой итерации внешнего цикла.
                // Если пользователь нажал отмену, мгновенно прекращаем расчет и возвращаем то, что успели собрать.
                if (isCancelled != null && isCancelled())
                    return globalResults;

                if (triangle == null || triangle.Length != 3)
                    continue;

                // Запуск рекурсивного каскада для текущего треугольника с самого нижнего уровня (индекс 0)
                CascadeSlice(triangle, sortedLevels, 0, slicerCore, globalResults);

                // Продвигаем прогресс-бар для каждого обработанного исходного треугольника
                incrementProgress?.Invoke();
            }

            return globalResults;
        }
        /// <summary>
        /// Внутренний приватный рекурсивный метод каскадного сита.
        /// Режет треугольник текущим уровнем, нижнюю часть фиксирует, а верхнюю передает на следующий уровень.
        /// </summary>
        private static void CascadeSlice(
            Point3d[] triangle,
            List<SliceLevel> sortedLevels,
            int currentLevelIdx,
            TriangleSlicerCore slicerCore,
            List<SlicedPiece> globalResults)
        {
            // БАЗОВЫЙ СЛУЧАЙ РЕКУРСИИ:
            // Если мы дошли до конца списка уровней, а геометрия всё еще осталась — 
            // значит этот остаток находится выше самого верхнего проектного уровня.
            // Фиксируем его за самым верхним уровнем со знаком 1 (Выше).
            if (currentLevelIdx >= sortedLevels.Count)
            {
                SliceLevel topLevel = sortedLevels[sortedLevels.Count - 1];
                globalResults.Add(new SlicedPiece(triangle[0], triangle[1], triangle[2], topLevel, 1));
                return;
            }

            SliceLevel currentLevel = sortedLevels[currentLevelIdx];

            // Вызываем наше доработанное атомарное ядро (с формулой Гаусса внутри IsPointsDegenerate)
            List<SlicedPiece> atomicPieces = slicerCore.SliceTriangle(triangle, currentLevel);

            foreach (SlicedPiece piece in atomicPieces)
            {
                // Если осколок находится НИЖЕ плоскости (Sign = -1) или СТРОГО НА НЕЙ (Sign = 0),
                // он полностью готов для текущего слоя и гарантированно не пересечет верхние уровни.
                // Фиксируем его в глобальный результат.
                if (piece.Sign <= 0)
                {
                    globalResults.Add(piece);
                }
                else
                {
                    // А вот если осколок ушел ВЫШЕ текущей плоскости (Sign = 1) — 
                    // это новый чистый треугольник, который потенциально может пересечь следующий уровень!
                    // Передаем его РЕКУРСИВНО на следующую ступень каскада (индекс уровня + 1).
                    CascadeSlice(piece.Vertices, sortedLevels, currentLevelIdx + 1, slicerCore, globalResults);
                }
            }
        }
        /// <summary>
        /// Служебный метод для гарантированной зачистки списков регионов при преждевременной отмене операции.
        /// Предотвращает утечки неуправляемой памяти и Fatal Error в AutoCAD.
        /// </summary>
        private static void ClearRegionLists(List<Region> r1, List<Region> r2, List<Region> r3)
        {
            if (r1 != null) { foreach (var r in r1) r?.Dispose(); r1.Clear(); }
            if (r2 != null) { foreach (var r in r2) r?.Dispose(); r2.Clear(); }
            if (r3 != null) { foreach (var r in r3) r?.Dispose(); r3.Clear(); }
        }
        /// <summary>
        /// Разрезает треугольник плоскостью заданной отметки (elevation) 
        /// и распределяет полученные геометрические фигуры (регионы) по корзинам:
        /// выше (posRegs), ниже (negRegs) или строго на плоскости (zeroRegs).
        /// </summary>
        private static void SplitAndDistributeTriangle(
            TriangleSlicerCore slicerCore,
            TinSurfaceTriangle triangle,
            double elevation,
            Dictionary<TinSurfaceVertex, Point3d> vertexCache,
            List<Point3d[]> posRegsArr,
            List<Point3d[]> negRegsArr,
            List<Point3d[]> zeroRegsArr)
        {
            // 1. Получаем округленные и смещенные вершины из кэша
            Point3d p1 = GetOrCreateCleanedVertex(triangle.Vertex1, vertexCache);
            Point3d p2 = GetOrCreateCleanedVertex(triangle.Vertex2, vertexCache);
            Point3d p3 = GetOrCreateCleanedVertex(triangle.Vertex3, vertexCache);

            // 2. Первичная быстрая защита от вырожденных треугольников самой поверхности
            // (Экономит память и такты процессора на больших поверхностях)
            if (p1.DistanceTo(p2) < slicerCore.Epsilon ||
                p2.DistanceTo(p3) < slicerCore.Epsilon ||
                p3.DistanceTo(p1) < slicerCore.Epsilon)
                return;

            // 3. Вызываем математическое ядро, передавая ИСТИННУЮ отметку разрезания (elevation)
            List<SlicedPiece> pices = slicerCore.SliceTriangle(new Point3d[] { p1, p2, p3 }, elevation);

            // 4. Распределяем полученные чистые треугольники по спискам регионов
            foreach (SlicedPiece piece in pices)
            {
                if (piece.Sign == -1) negRegsArr.Add(piece.Vertices);
                else if (piece.Sign == 0) zeroRegsArr.Add(piece.Vertices);
                else posRegsArr.Add(piece.Vertices);
            }
        }
        private static Point3d GetOrCreateCleanedVertex(TinSurfaceVertex vertex, Dictionary<TinSurfaceVertex, Point3d> cache)
        {
            if (cache.TryGetValue(vertex, out Point3d cleanedPoint))
            {
                return cleanedPoint;
            }

            Point3d loc = vertex.Location;

            // Инициализируем вектор смещения по первой попавшейся точке
            if (!_isShiftCalculated)
            {
                // Оставляем круглую часть (вычитаем миллионы, оставляя остаток в пределах тысяч)
                // Например: 2345678.123 -> Math.Floor(2345678.123 / 1000) * 1000 = 2345000
                double shiftX = Math.Floor(loc.X / 1000.0) * 1000.0;
                double shiftY = Math.Floor(loc.Y / 1000.0) * 1000.0;

                _coordinateShift = new Vector3d(shiftX, shiftY, 0.0);
                _isShiftCalculated = true;
            }

            // Вычитаем вектор смещения, перенося точку ближе к (0,0) чертежа, и округляем до 5 знаков
            Point3d shiftedPoint = new Point3d(
                Math.Round(loc.X - _coordinateShift.X, 5),
                Math.Round(loc.Y - _coordinateShift.Y, 5),
                Math.Round(loc.Z, 5)
            );

            cache.Add(vertex, shiftedPoint);
            return shiftedPoint;
        }
        #endregion
    }

    public static class CurveConnector
    {
        public static bool ConnectCurve(List<LightLine> lines, out List<Curve> polylines)
        {
            polylines = new List<Curve>();
            if (lines.Count == 0) return true;

            // Строительный допуск для сшивания линий
            Tolerance customTolerance = new Tolerance(1e-5, 1e-5);

            // Работаем напрямую со списком, без опасных хэш-таблиц
            List<LightLine> activeLines = new List<LightLine>(lines);

            // Запускаем сборку, пока в списке есть нераспределенные отрезки
            while (activeLines.Count > 0)
            {
                // Берем первый доступный отрезок за основу контура
                LightLine startLine = activeLines[0];
                activeLines.RemoveAt(0);

                // Проверяем на вырождение в точку
                if (startLine.StartPoint.IsEqualTo(startLine.EndPoint, customTolerance)) continue;

                Point2dCollection pts = new Point2dCollection();
                pts.Add(new Point2d(startLine.StartPoint.X, startLine.StartPoint.Y));
                pts.Add(new Point2d(startLine.EndPoint.X, startLine.EndPoint.Y));

                Point3d currentEnd = startLine.EndPoint;
                Point3d currentStart = startLine.StartPoint;

                // Фиксируем стартовую точку всего контура для проверки замыкания
                Point3d contourStartPoint = startLine.StartPoint;

                bool matchFound;

                // Движение вперед
                do
                {
                    matchFound = false;

                    // 1. Сначала сканируем список и находим всех подходящих кандидатов в текущей точке currentEnd
                    List<int> candidateIndices = new List<int>();
                    for (int i = 0; i < activeLines.Count; i++)
                    {
                        LightLine checkLine = activeLines[i];
                        if (checkLine.StartPoint.IsEqualTo(currentEnd, customTolerance) ||
                            checkLine.EndPoint.IsEqualTo(currentEnd, customTolerance))
                        {
                            candidateIndices.Add(i);
                        }
                    }

                    if (candidateIndices.Count == 0) break; // Тупик, продолжений нет

                    int selectedIndex = -1;

                    if (candidateIndices.Count == 1)
                    {
                        // Развилки нет, кандидат всего один — берем его без геометрических расчетов
                        selectedIndex = candidateIndices[0];
                    }
                    else
                    {
                        // НАЙДЕНА РАЗВИЛКА / ПЕРЕКРЕСТОК!
                        // Базовый вектор направлен НАЗАД: от последней точки (currentEnd) к предпоследней (currentStart)
                        // То есть вектор 3 -> 2 на вашей схеме
                        Vector3d baseDir = (currentStart - currentEnd).GetNormal();

                        double minAngle = double.MaxValue;

                        foreach (int idx in candidateIndices)
                        {
                            LightLine candidate = activeLines[idx];

                            // Вектор кандидата: ВСЕГДА из точки стыка (currentEnd) наружу в его свободную вершину
                            Vector3d candidateDir = candidate.StartPoint.IsEqualTo(currentEnd, customTolerance)
                                ? (candidate.EndPoint - currentEnd).GetNormal()
                                : (candidate.StartPoint - currentEnd).GetNormal();

                            // Считаем плоский угол от 0 до 2*PI строго ПРОТИВ ЧАСОВОЙ СТРЕЛКИ относительно вектора назад
                            double angle = baseDir.GetAngleTo(candidateDir, Vector3d.ZAxis);

                            // Выбираем кандидата с минимальным углом (самый первый при обходе против часовой стрелки)
                            // На вашей схеме это кандидат 3, что заставит контур чисто замкнуть левый ромб
                            if (angle < minAngle)
                            {
                                minAngle = angle;
                                selectedIndex = idx;
                            }
                        }
                    }

                    // 2. Шагаем по выбранной линии и извлекаем её из дальнейших расчетов
                    if (selectedIndex != -1)
                    {
                        LightLine selectedLine = activeLines[selectedIndex];
                        activeLines.RemoveAt(selectedIndex);

                        // Запоминаем текущую точку 3 как предпоследнюю (currentStart) для следующего шага
                        currentStart = currentEnd;

                        // Перемещаем маркер конца вперед на свободный конец выбранного отрезка
                        currentEnd = selectedLine.StartPoint.IsEqualTo(currentEnd, customTolerance)
                            ? selectedLine.EndPoint
                            : selectedLine.StartPoint;

                        pts.Add(new Point2d(currentEnd.X, currentEnd.Y));
                        matchFound = true;
                    }

                    // Проверяем замыкание на самую первую точку, с которой начался этот контур
                    if (currentEnd.IsEqualTo(contourStartPoint, customTolerance) && pts.Count > 2)
                    {
                        break;
                    }

                } while (matchFound);

                // Создание полилинии AutoCAD
                if (pts.Count >= 2)
                {
                    Polyline pl = new Polyline();

                    if (pts[0].IsEqualTo(pts[pts.Count - 1], customTolerance) && pts.Count > 2)
                    {
                        // Для замкнутых контуров последнюю дублирующую точку не добавляем, а выставляем Closed
                        for (int i = 0; i < pts.Count - 1; i++)
                        {
                            pl.AddVertexAt(i, pts[i], 0, 0, 0);
                        }
                        pl.Closed = true;
                    }
                    else
                    {
                        for (int i = 0; i < pts.Count; i++)
                        {
                            pl.AddVertexAt(i, pts[i], 0, 0, 0);
                        }
                    }

                    polylines.Add(pl);
                }
            }

            return true;
        }

    }
    // Легковесная структура отрезка для быстрой работы в памяти (стеке)
    public struct LightLine
    {
        public Point3d StartPoint;
        public Point3d EndPoint;

        public LightLine(Point3d start, Point3d end)
        {
            // Обнуляем Z, перенося вычисления в чистую 2D-плоскость
            StartPoint = new Point3d(start.X, start.Y, 0);
            EndPoint = new Point3d(end.X, end.Y, 0);
        }
    }
}
