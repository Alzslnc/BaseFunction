using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaseFunction
{   

    /// <summary>
    /// Атомарное математическое ядро для нарезки треугольников.
    /// Не имеет зависимостей от Civil 3D. Оперирует чистой геометрией.
    /// </summary>
    public class TriangleSlicerCore
    {
        /// <summary>
        /// Динамический допуск точности для сшивания и проверки вырождения геометрии.
        /// </summary>
        public double Epsilon { get; set; }

        /// <summary>
        /// Инициализирует ядро разрезания со стандартным строительным допуском 5 знаков (0.01 мм).
        /// </summary>
        public TriangleSlicerCore()
        {
            Epsilon = 1e-5;
        }

        /// <summary>
        /// Инициализирует ядро разрезания с кастомным допуском точности.
        /// </summary>
        public TriangleSlicerCore(double epsilon)
        {
            Epsilon = epsilon;
        }

        /// <summary>
        /// Перегрузка метода для сценариев, где достаточно передать обычную double-отметку.
        /// </summary>
        public List<SlicedPiece> SliceTriangle(Point3d[] triangle, double elevation)
        {
            return SliceTriangle(triangle, new SliceLevel(elevation));
        }

        /// <summary>
        /// Основной метод ядра. Разрезает исходный треугольник плоскостью уровня.
        /// Если в процессе резки образуется трапеция, она автоматически делится по диагонали на два треугольника.
        /// </summary>
        public List<SlicedPiece> SliceTriangle(Point3d[] triangle, SliceLevel level)
        {
            List<SlicedPiece> result = new List<SlicedPiece>();

            // Защита от некорректных данных и вырождения исходного треугольника на входе
            if (triangle == null || triangle.Length != 3 || IsPointsDegenerate(triangle))
                return result;

            double elev = level.Value;

            Point3d p1 = triangle[0];
            Point3d p2 = triangle[1];
            Point3d p3 = triangle[2];

            // Расчет знаков вершин относительно секущей плоскости с учетом динамического эпсилона
            int s1 = Math.Abs(p1.Z - elev) < Epsilon ? 0 : (p1.Z > elev ? 1 : -1);
            int s2 = Math.Abs(p2.Z - elev) < Epsilon ? 0 : (p2.Z > elev ? 1 : -1);
            int s3 = Math.Abs(p3.Z - elev) < Epsilon ? 0 : (p3.Z > elev ? 1 : -1);

            Point3d[] pts = { p1, p2, p3 };
            int[] signs = { s1, s2, s3 };

            int countPos = 0, countNeg = 0, countZero = 0;
            for (int i = 0; i < 3; i++)
            {
                if (signs[i] > 0) countPos++;
                else if (signs[i] < 0) countNeg++;
                else countZero++;
            }

            // Сценарий 1: Плоское плато (все 3 точки лежат на плоскости)
            if (countZero == 3)
            {
                result.Add(new SlicedPiece(p1, p2, p3, level, 0));
                return result;
            }

            // Сценарий 2: Треугольник целиком по одну сторону (нет пересечений)
            if (countNeg == 0)
            {
                result.Add(new SlicedPiece(p1, p2, p3, level, 1));
                return result;
            }
            if (countPos == 0)
            {
                result.Add(new SlicedPiece(p1, p2, p3, level, -1));
                return result;
            }

            // Сценарий 3: Разрез выходит точно из одной вершины (Знаки: 0, 1, -1)
            if (countZero == 1)
            {
                int zeroIndex = Array.IndexOf(signs, 0);
                Point3d vZero = pts[zeroIndex];
                Point3d vPlus = pts[(zeroIndex + 1) % 3].Z > elev ? pts[(zeroIndex + 1) % 3] : pts[(zeroIndex + 2) % 3];
                Point3d vMinus = pts[(zeroIndex + 1) % 3].Z < elev ? pts[(zeroIndex + 1) % 3] : pts[(zeroIndex + 2) % 3];

                Point3d intersect = GetIntersectPoint(vPlus, vMinus, elev);

                Point3d[] piecePos = { vZero, vPlus, intersect };
                Point3d[] pieceNeg = { vZero, vMinus, intersect };
                               
                // Если верхний осколок выродился — отдаем весь исходный треугольник целиком вниз (-)
                if (IsPointsDegenerate(piecePos))
                {
                    result.Add(new SlicedPiece(p1, p2, p3, level, -1));
                    return result;
                }
                // Если нижний осколок выродился — отдаем весь исходный треугольник целиком вверх (+)
                if (IsPointsDegenerate(pieceNeg))
                {
                    result.Add(new SlicedPiece(p1, p2, p3, level, 1));
                    return result;
                }

                // Если никто не выродился — добавляем обе части штатно
                result.Add(new SlicedPiece(piecePos[0], piecePos[1], piecePos[2], level, 1));
                result.Add(new SlicedPiece(pieceNeg[0], pieceNeg[1], pieceNeg[2], level, -1));
                return result;
            }

            // Сценарий 4: Плоскость полностью разрезает две стороны (Знаки: 1, 1, -1 или -1, -1, 1)  
            if (countPos == 1 || countNeg == 1)
            {
                int loneIndex = -1;
                for (int i = 0; i < 3; i++)
                {
                    if (countPos == 1 && signs[i] > 0) { loneIndex = i; break; }
                    if (countNeg == 1 && signs[i] < 0) { loneIndex = i; break; }
                }

                Point3d v0 = pts[loneIndex];
                Point3d v1 = pts[(loneIndex + 1) % 3];
                Point3d v2 = pts[(loneIndex + 2) % 3];
                int signLone = signs[loneIndex];

                Point3d intersect1 = GetIntersectPoint(v0, v1, elev);
                Point3d intersect2 = GetIntersectPoint(v0, v2, elev);

                // Определяем знаки для корзин на основе одинокой вершины
                int loneSign = signLone > 0 ? 1 : -1;
                int restSign = -loneSign;

                // Анализируем совпадение точек из-за округления double (используем динамический Epsilon ядра)
                bool int1AtV0 = intersect1.DistanceTo(v0) < Epsilon;
                bool int1AtV1 = intersect1.DistanceTo(v1) < Epsilon;

                bool int2AtV0 = intersect2.DistanceTo(v0) < Epsilon;
                bool int2AtV2 = intersect2.DistanceTo(v2) < Epsilon;

                // Вариант А: Линия разреза легла на вершину v0 (макушки нет, весь треугольник остался внизу/вверху)
                if (int1AtV0 || int2AtV0)
                {
                    result.Add(new SlicedPiece(p1, p2, p3, level, restSign));
                    return result;
                }

                // Вариант Б: Линия разреза легла на противоположное ребро (вся масса осталась у одинокой вершины v0)
                if (int1AtV1 && int2AtV2)
                {
                    result.Add(new SlicedPiece(p1, p2, p3, level, loneSign));
                    return result;
                }

                // Вариант В: Разрез из-за шума вышел точно из вершины v1 (трапеция превратилась в треугольник)
                if (int1AtV1)
                {
                    result.Add(new SlicedPiece(v0, v1, intersect2, level, loneSign));
                    result.Add(new SlicedPiece(v1, v2, intersect2, level, restSign));
                    return result;
                }

                // Вариант Г: Разрез из-за шума вышел точно из вершины v2 (трапеция превратилась в треугольник)
                if (int2AtV2)
                {
                    result.Add(new SlicedPiece(v0, intersect1, v2, level, loneSign));
                    result.Add(new SlicedPiece(intersect1, v1, v2, level, restSign));
                    return result;
                }

                // =========================================================================
                // СТАНДАРТНЫЙ ВАРИАНТ: Округление не схлопнуло точки в нанометрах.
                // Формируем геометрию трех потенциальных треугольных осколков
                // =========================================================================
                Point3d[] topPiece = { v0, intersect1, intersect2 };
                Point3d[] bottomPiece1 = { intersect1, v1, v2 };
                Point3d[] bottomPiece2 = { intersect1, v2, intersect2 };

                // ЖЕЛЕЗНЫЙ ФИЛЬТР ГАУССА:
                // Если из-за узости треугольника Civil 3D или пограничного шума double 
                // ХОТЬ ОДИН из трех осколков выродился по площади (меньше Epsilon) —
                // мы отменяем весь распил трапеции и возвращаем исходный треугольник целиком!
                // Знак определяется большинством вершин (у v1 и v2 знак restSign).
                if (IsPointsDegenerate(topPiece) ||
                    IsPointsDegenerate(bottomPiece1) ||
                    IsPointsDegenerate(bottomPiece2))
                {
                    result.Add(new SlicedPiece(p1, p2, p3, level, restSign));
                    return result;
                }

                // Если контроль качества по Гауссу пройден — штатно добавляем все 3 треугольника        
                result.Add(new SlicedPiece(topPiece[0], topPiece[1], topPiece[2], level, loneSign));
                result.Add(new SlicedPiece(bottomPiece1[0], bottomPiece1[1], bottomPiece1[2], level, restSign));
                result.Add(new SlicedPiece(bottomPiece2[0], bottomPiece2[1], bottomPiece2[2], level, restSign));
            }

            return result;
        }
        /// <summary>
        /// Универсальная проверка треугольника на вырождение в пространстве XY.
        /// Защищает от слипшихся вершин и от длинных "иголок" нулевой площади по формуле Гаусса.
        /// </summary>
        private bool IsPointsDegenerate(Point3d[] vertices)
        {
            if (vertices == null || vertices.Length != 3) return true;

            Point3d p0 = vertices[0];
            Point3d p1 = vertices[1];
            Point3d p2 = vertices[2];

            // РУБЕЖ 1: Быстрая проверка на слипание соседних вершин (допуск Epsilon)
            if (p0.DistanceTo(p1) < Epsilon ||
                p1.DistanceTo(p2) < Epsilon ||
                p2.DistanceTo(p0) < Epsilon)
            {
                return true;
            }

            // РУБЕЖ 2: Формула Гаусса (Surveyor's Formula) для вычисления площади на чистых регистрах ЦП.
            // Нам плевать на 3D-высоту Z, так как штриховка и регионы строятся в проекции XY.
            double gaussArea = 0.5 * Math.Abs(
                p0.X * (p1.Y - p2.Y) +
                p1.X * (p2.Y - p0.Y) +
                p2.X * (p0.Y - p1.Y)
            );

            // Если площадь треугольника меньше нашего допуска — это вырожденная "иголка"
            if (gaussArea < Epsilon)
            {
                return true;
            }

            return false;
        }


        /// <summary>
        /// Вычисляет точные плоские координаты точки пересечения ребра с плоскостью.
        /// </summary>
        private Point3d GetIntersectPoint(Point3d p1, Point3d p2, double elevation)
        {
            double deltaZ = p2.Z - p1.Z;
            if (Math.Abs(deltaZ) < Epsilon)
                return new Point3d(p1.X, p1.Y, elevation);

            double t = (elevation - p1.Z) / deltaZ;
            if (t < 0.0) t = 0.0;
            if (t > 1.0) t = 1.0;

            double x = p1.X + t * (p2.X - p1.X);
            double y = p1.Y + t * (p2.Y - p1.Y);

            return new Point3d(x, y, elevation);
        }
    }

    /// <summary>
    /// Сервис для работы с топологией регионов AutoCAD (ядро ACIS).
    /// Полностью изолирован от Civil 3D и адаптирован для работы с внешними прогресс-барами.
    /// </summary>
    public static class RegionTopologyService
    {      
        /// <summary>
        /// ШАГ 1: Поштучное контролируемое создание элементарных регионов из сырых полигонов точек.
        /// Изолирует каждый треугольник, гарантируя, что пограничные коллизии ACIS не сломают весь расчет.
        /// </summary>
        /// <param name="polygons">Коллекция контуров (каждый контур — массив строго из 3-х или более точек).</param>
        /// <param name="epsilon">Допуск точности для отсечения вырожденных ребер.</param>
        /// <returns>Список чистых элементарных регионов, готовых к склейке.</returns>
        public static List<Region> BuildElementaryRegions(IEnumerable<Point3d[]> polygons, double epsilon)
        {
            List<Region> elementaryRegions = new List<Region>();
            if (polygons == null) return elementaryRegions;

            foreach (Point3d[] pts in polygons)
            {
                if (pts == null || pts.Length < 3) continue;

                // 1. Защита от вырождения ребер контура перед созданием полилинии
                bool isDegenerate = false;
                int numPts = pts.Length;
                for (int i = 0; i < numPts; i++)
                {
                    if (pts[i].DistanceTo(pts[(i + 1) % numPts]) < epsilon)
                    {
                        isDegenerate = true;
                        break;
                    }
                }
                if (isDegenerate) continue;

                // 2. Создаем легкий in-memory каркас полилинии для ОДНОГО треугольника
                using (Polyline pl = new Polyline(numPts))
                {
                    for (int i = 0; i < numPts; i++)
                    {
                        pl.AddVertexAt(i, new Point2d(pts[i].X, pts[i].Y), 0, 0, 0);
                    }
                    pl.Closed = true;

                    // 3. Упаковываем ОДНУ полилинию в контейнер
                    using (DBObjectCollection singleCurveCollection = new DBObjectCollection { pl })
                    {
                        try
                        {
                            // Изолированный вызов ядра ACIS для одного элемента
                            using (DBObjectCollection createdRegions = Region.CreateFromCurves(singleCurveCollection))
                            {
                                if (createdRegions != null && createdRegions.Count > 0)
                                {
                                    // Извлекаем созданный регион
                                    if (createdRegions[0] is Region reg)
                                    {
                                        // Отсоединяем регион от коллекции чертежа, чтобы его не уничтожил dispose контейнера
                                        createdRegions.RemoveAt(0);
                                        elementaryRegions.Add(reg);
                                    }
                                }
                            }
                        }
                        catch
                        {
                            // Если конкретный треугольник на изломе откоса взрывает ядро ACIS,
                            // мы просто изолированно пропускаем его. Остальной расчет спасен!
                        }
                    }
                }
            }

            return elementaryRegions;
        }
        public static Region MergeRegions(List<Region> regions) => MergeRegionsWithProgress(regions, 1e-6, null, null);
        public static Region MergeRegions(List<Region> regions, double epsilon) => MergeRegionsWithProgress(regions, epsilon, null, null);
        /// <summary>
        /// ШАГ 2: Интегрированная склейка регионов с поддержкой шагов прогресс-бара и отмены операции.
        /// Заменяет старый CombineRegionsParallel, вызывая под капотом ваш готовый тяжелый библиотечный метод.
        /// </summary>
        /// <param name="regions">Список элементарных регионов (будет опустошен/модифицирован в процессе).</param>
        /// <param name="incrementProgress">Делегат для продвижения подшага прогресс-бара (например, () => data.Dialog.IncrementSubStep()).</param>
        /// <param name="isCancellationRequested">Делегат для проверки нажатия кнопки "Отмена" (например, () => data.Dialog.CancellationToken.IsCancellationRequested).</param>
        /// <returns>Единый монолитный мегарегион или null в случае отмены/ошибки.</returns>
        public static Region MergeRegionsWithProgress(
            List<Region> regions,
            double epsilon,
            Action incrementProgress,
            Func<bool> isCancellationRequested)
        {
            if (regions == null || regions.Count == 0) return null;

            // Если регион всего один — клеить нечего, отдаем его сразу
            if (regions.Count == 1) return regions[0];

            Queue<Region> queue = new Queue<Region>(regions);
            int errorLimit = 30;

            while (errorLimit > 0 && queue.Count > 1)
            {
                // Проверяем, не нажал ли пользователь "Отмена" на чертеже
                if (isCancellationRequested != null && isCancellationRequested())
                {
                    // Безопасно зачищаем всё, что осталось в очереди, предотвращая утечки памяти ACIS
                    foreach (Region r in queue) { if (r != null && !r.IsDisposed) r.Dispose(); }
                    return null;
                }

                // ЖЕСТКОЕ И ПОСЛЕДОВАТЕЛЬНОЕ ИЗВЛЕЧЕНИЕ ПАРЫ ИЗ ОЧЕРЕДИ
                Region reg1 = queue.Dequeue();
                Region reg2 = queue.Dequeue();

                // РУБЕЖ ЗАЩИТЫ №1: Проверка первого региона на null и вырождение площади
                if (reg1 == null || reg1.IsDisposed || reg1.IsNull || double.IsNaN(reg1.Area) || reg1.Area < epsilon)
                {
                    reg1?.Dispose();     // Уничтожаем брак
                    queue.Enqueue(reg2); // Живой второй регион возвращаем обратно в начало очереди!
                    incrementProgress?.Invoke(); // Пул уменьшился на 1, двигаем бар
                    continue;
                }

                // РУБЕЖ ЗАЩИТЫ №2: Проверка второго региона на null и вырождение площади
                if (reg2 == null || reg2.IsDisposed || reg2.IsNull || double.IsNaN(reg2.Area) || reg2.Area < epsilon)
                {
                    reg2?.Dispose();     // Уничтожаем брак
                    queue.Enqueue(reg1); // Живой первый регион возвращаем обратно в начало очереди!
                    incrementProgress?.Invoke(); // Пул уменьшился на 1, двигаем бар
                    continue;
                }

                // РУБЕЖ ЗАЩИТЫ №3: Обе фигуры идеальны. Запускаем нативное булево слияние
                try
                {
                    // Пытаемся объединить на уровне C++ ядра ACIS
                    reg1.BooleanOperation(BooleanOperationType.BoolUnite, reg2);

                    // РЕГИОН УСПЕШНО ПОГЛОЩЕН И УДАЛЕН ИЗ ПАМЯТИ
                    reg2.Dispose();
                    queue.Enqueue(reg1);

                    // Общее число регионов уменьшилось на 1. Продвигаем прогресс-бар!
                    incrementProgress?.Invoke();
                }
                catch
                {
                    errorLimit--;
                    // В случае редкого геометрического сбоя возвращаем оба объекта в очередь.
                    // Публичный пул не уменьшился, прогресс здесь не вызываем.
                    queue.Enqueue(reg2);
                    queue.Enqueue(reg1);
                }
            }

            if (queue.Count == 0) return null;

            Region finalMegaRegion = queue.Dequeue();

            // Железная финальная проверка на зомби-объекты поврежденной булевой геометрии перед выдачей
            if (finalMegaRegion == null || finalMegaRegion.IsDisposed || finalMegaRegion.IsNull || double.IsNaN(finalMegaRegion.Area) || finalMegaRegion.Area < epsilon)
            {
                if (finalMegaRegion != null && !finalMegaRegion.IsDisposed) finalMegaRegion.Dispose();
                return null;
            }

            return finalMegaRegion;
        }

        /// <summary>
        /// Извлекает топологически чистые замкнутые контуры графики (полилинии) из мегарегиона.
        /// Использует готовый библиотечный метод сшивания тяжелых Curve.
        /// </summary>
        /// <param name="megaRegion">Монолитный мегарегион, полученный после склейки.</param>
        /// <param name="coordinateShift">Вектор смещения, рассчитанный при нарезке треугольников.</param>
        /// <returns>Список полностью готовых, замкнутых полилиний для штриховки.</returns>
        public static List<Curve> ExtractClosedContours(Region megaRegion, Vector3d coordinateShift)
        {
            List<Curve> result = new List<Curve>();

            // Жесткая проверка на валидность ссылки и нативного C++ тела региона
            if (megaRegion == null || megaRegion.IsDisposed || megaRegion.IsNull)
                return result;

            using (DBObjectCollection explodedObjects = new DBObjectCollection())
            {
                // Выполняем одноуровневый контролируемый взрыв мегарегиона в памяти
                megaRegion.Explode(explodedObjects);
                if (explodedObjects.Count == 0) return result;

                // Проверяем, является ли регион составным (Composite) — содержит ли он независимые острова
                bool isComposite = false;
                foreach (DBObject obj in explodedObjects)
                {
                    if (obj is Region)
                    {
                        isComposite = true;
                        break;
                    }
                }

                if (isComposite)
                {
                    // СЦЕНАРИЙ А: Регион составной (содержит несколько независимых островков откосов).
                    // Обрабатываем каждый дочерний кусок СТРОГО ИЗОЛИРОВАННО. Это изолирует 
                    // точки касания разных откосов друг от друга и защищает алгоритм сшивания.
                    foreach (DBObject obj in explodedObjects)
                    {
                        if (obj is Region subRegion)
                        {
                            List<Curve> localLines = new List<Curve>();

                            // Взрываем конкретный изолированный остров на линии
                            using (DBObjectCollection subExploded = new DBObjectCollection())
                            {
                                subRegion.Explode(subExploded);
                                foreach (DBObject subObj in subExploded)
                                {
                                    if (subObj is Curve cv) localLines.Add(cv);
                                    else subObj.Dispose();
                                }
                            }

                            if (localLines.Count >= 3)
                            {
                                // ВЫЗОВ ВАШЕГО МЕТОДА ИЗ БИБЛИОТЕКИ:
                                // Передаем тяжелые Curve. Метод сам сошьет их и вызовет Dispose для исходных фрагментов!
                                localLines.ConnectCurve(out List<Curve> connected, false, true);

                                if (connected != null && connected.Count > 0)
                                    result.AddRange(connected);
                            }
                            subRegion.Dispose();
                        }
                        else
                        {
                            if (obj != null && !obj.IsDisposed) obj.Dispose();
                        }
                    }
                }
                else
                {
                    // СЦЕНАРИЙ Б: Регион простой (одна сплошная монолитная область)
                    List<Curve> mainLines = new List<Curve>();
                    foreach (DBObject obj in explodedObjects)
                    {
                        if (obj is Curve cv) mainLines.Add(cv);
                        else if (obj != null && !obj.IsDisposed) obj.Dispose();
                    }

                    if (mainLines.Count >= 3)
                    {
                        // ВЫЗОВ ВАШЕГО МЕТОДА ИЗ БИБЛИОТЕКИ:
                        mainLines.ConnectCurve(out List<Curve> connected, false, true);

                        if (connected != null && connected.Count > 0)
                            result.AddRange(connected);
                    }
                }
            }

            // ИНКАПСУЛИРОВАННЫЙ ВОЗВРАТ КООРДИНАТ И ВЫРАВНИВАНИЕ ПЛОСКОСТИ ДЛЯ ИНТЕРФЕЙСА AUTOCAD
            if (result.Count > 0)
            {
                Matrix3d returnMatrix = coordinateShift.Length > 0.1
                    ? Matrix3d.Displacement(coordinateShift)
                    : Matrix3d.Identity;

                foreach (Curve curve in result)
                {
                    if (curve != null && !curve.IsDisposed)
                    {
                        if (curve is Polyline pl)
                        {
                            // СБРОС СДВИГОВ: Намертво зануляем микро-дрейф плоскости и нормали.
                            // Это возвращает стабильность окну свойств AutoCAD, и площадь полилинии 
                            // всегда корректно отображается в стандартном инспекторе на экране.
                            pl.Elevation = 0.0;
                            pl.Normal = new Vector3d(0, 0, 1);
                        }

                        // Возвращаем полилинию из локального пространства (0,0) на миллионные координаты Civil 3D
                        if (coordinateShift.Length > 0.1)
                            curve.TransformBy(returnMatrix);
                    }
                }
            }

            return result;
        }
    }

    #region классы и структуры
    /// <summary>
    /// Ссылочный идентификатор плоскости разреза поверхности.
    /// Использует паттерн Identity Object: сравнение в коллекциях (Dictionary, GroupBy)
    /// идет строго по уникальному адресу объекта в памяти, что на 100% защищает от double-шума.
    /// </summary>
    public class SliceLevel
    {
        /// <summary>
        /// Реальное точное значение высотной отметки уровня.
        /// Используется исключительно для геометрических расчетов резки и сортировки.
        /// </summary>
        public double Value { get; }

        /// <summary>
        /// Инициализирует новый экземпляр плоскости разреза.
        /// </summary>
        /// <param name="value">Высотная отметка уровня.</param>
        public SliceLevel(double value)
        {
            Value = value;
        }

        /// <summary>
        /// Удобное форматирование для отладки, инспектора свойств и вывода в логи.
        /// </summary>
        public override string ToString()
        {
            return Value.ToString("F5");
        }
    }
    /// <summary>
    /// Контейнер для результирующего осколка разрезания. 
    /// Всегда содержит ровно 3 вершины (треугольник) и жестко связан со своим уровнем и знаком.
    /// </summary>
    public readonly struct SlicedPiece
    {
        public Point3d[] Vertices { get; }
        public SliceLevel Level { get; }

        // 1 = Выше уровня, -1 = Ниже уровня, 0 = Лежит строго на уровне (плато)
        public int Sign { get; }

        public SlicedPiece(Point3d p1, Point3d p2, Point3d p3, SliceLevel level, int sign)
        {
            Vertices = new Point3d[] { p1, p2, p3 };
            Level = level;
            Sign = sign;
        }
    }
    #endregion
}
