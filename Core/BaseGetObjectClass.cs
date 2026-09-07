using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BaseFunction
{
    public static class BaseGetObjectClass
    {
        #region получение дробного числа
        /// <summary>
        /// Запрашивает у пользователя дробное число со значением по умолчанию 0.
        /// </summary>
        public static bool TryGetDoubleFromUser(out double result)
        {
            return TryGetDoubleFromUser(out result, 0, null, null, "Введите число");
        }

        /// <summary>
        /// Запрашивает у пользователя дробное число со значением по умолчанию 0 и кастомным сообщением.
        /// </summary>
        public static bool TryGetDoubleFromUser(out double result, string message)
        {
            return TryGetDoubleFromUser(out result, 0, null, null, message);
        }

        /// <summary>
        /// Запрашивает у пользователя дробное число со значением по умолчанию.
        /// </summary>
        public static bool TryGetDoubleFromUser(out double result, double baseValue)
        {
            return TryGetDoubleFromUser(out result, baseValue, null, null, "Введите число");
        }

        /// <summary>
        /// Запрашивает у пользователя дробное число со значением по умолчанию и кастомным сообщением.
        /// </summary>
        public static bool TryGetDoubleFromUser(out double result, double baseValue, string message)
        {
            return TryGetDoubleFromUser(out result, baseValue, null, null, message);
        }

        /// <summary>
        /// Запрашивает у пользователя дробное число со значением по умолчанию 0 в заданных диапазонах.
        /// </summary>
        public static bool TryGetDoubleFromUser(out double result, double? minValue, double? maxValue)
        {
            return TryGetDoubleFromUser(out result, 0, minValue, maxValue, "Введите число");
        }

        /// <summary>
        /// Базовый метод: запрашивает у пользователя дробное число с округлением до 6 знаков и выравниванием по границам.
        /// </summary>
        public static bool TryGetDoubleFromUser(out double result, double baseValue, double? minValue, double? maxValue, string message)
        {
            // Округляем входящие границы (если они заданы), чтобы избежать шума при сравнении
            double? roundedMin = minValue.HasValue ? (double?)Math.Round(minValue.Value, 6) : null;
            double? roundedMax = maxValue.HasValue ? (double?)Math.Round(maxValue.Value, 6) : null;

            // Сначала округляем дефолтное значение
            baseValue = Math.Round(baseValue, 6);

            // Выравниваем baseValue по округленным границам
            if (roundedMin.HasValue && baseValue < roundedMin.Value)
                baseValue = roundedMin.Value;
            if (roundedMax.HasValue && baseValue > roundedMax.Value)
                baseValue = roundedMax.Value;

            // Формируем красивую подсказку с точкой
            string rangeInfo = "";
            if (roundedMin.HasValue || roundedMax.HasValue)
            {
                string minStr = roundedMin.HasValue ? roundedMin.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) : null;
                string maxStr = roundedMax.HasValue ? roundedMax.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) : null;

                if (roundedMin.HasValue && roundedMax.HasValue)
                    rangeInfo = $" [{minStr} - {maxStr}]";
                else if (roundedMin.HasValue)
                    rangeInfo = $" [>= {minStr}]";
                else if (roundedMax.HasValue)
                    rangeInfo = $" [<= {maxStr}]";
            }

            string promptText = $"\n{message}{rangeInfo}";

            PromptStringOptions pso = new PromptStringOptions(promptText)
            {
                // Переводим в строку строго через InvariantCulture, чтобы в скобках < > была точка
                DefaultValue = baseValue.ToString(System.Globalization.CultureInfo.InvariantCulture),
                AllowSpaces = false,
                UseDefaultValue = true
            };

            var ed = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Editor;

            while (true)
            {
                PromptResult res = ed.GetString(pso);

                if (res.Status == PromptStatus.Cancel)
                {
                    result = 0;
                    return false;
                }
                else if (res.Status == PromptStatus.OK)
                {
                    if (string.IsNullOrEmpty(res.StringResult)) continue;

                    string cleanInput = res.StringResult.Replace(",", ".");
                    if (!double.TryParse(cleanInput, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double dRes))
                        continue;

                    dRes = Math.Round(dRes, 6);

                    if (roundedMin.HasValue && dRes < roundedMin.Value)
                    {
                        string minStr = roundedMin.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
                        ed.WriteMessage($"\nЧисло ниже допустимого ({minStr}).");
                        continue;
                    }
                    if (roundedMax.HasValue && dRes > roundedMax.Value)
                    {
                        string maxStr = roundedMax.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
                        ed.WriteMessage($"\nЧисло выше допустимого ({maxStr}).");
                        continue;
                    }

                    result = dRes;
                    return true;
                }
            }
        }
        #endregion

        #region получение целого числа
        /// <summary>
        /// Запрашивает у пользователя целое число со значением по умолчанию 0.
        /// </summary>
        public static bool TryGetIntFromUser(out int result)
        {
            return TryGetIntFromUser(out result, 0, null, null, "Введите целое число");
        }

        /// <summary>
        /// Запрашивает у пользователя целое число со значением по умолчанию 0 и кастомным сообщением.
        /// </summary>
        public static bool TryGetIntFromUser(out int result, string message)
        {
            return TryGetIntFromUser(out result, 0, null, null, message);
        }

        /// <summary>
        /// Запрашивает у пользователя целое число со значением по умолчанию.
        /// </summary>
        public static bool TryGetIntFromUser(out int result, int baseValue)
        {
            return TryGetIntFromUser(out result, baseValue, null, null, "Введите целое число");
        }

        /// <summary>
        /// Запрашивает у пользователя целое число со значением по умолчанию и кастомным сообщением.
        /// </summary>
        public static bool TryGetIntFromUser(out int result, int baseValue, string message)
        {
            return TryGetIntFromUser(out result, baseValue, null, null, message);
        }

        /// <summary>
        /// Запрашивает у пользователя целое число со значением по умолчанию 0 в заданных диапазонах.
        /// </summary>
        public static bool TryGetIntFromUser(out int result, int? minValue, int? maxValue)
        {
            return TryGetIntFromUser(out result, 0, minValue, maxValue, "Введите целое число");
        }

        /// <summary>
        /// Базовый метод: запрашивает у пользователя целое число. Автоматически корректирует baseValue под границы.
        /// </summary>
        public static bool TryGetIntFromUser(out int result, int baseValue, int? minValue, int? maxValue, string message)
        {
            // Совместимое с .NET 4.7.2 выравнивание по границам
            if (minValue.HasValue && baseValue < minValue.Value)
                baseValue = minValue.Value;
            if (maxValue.HasValue && baseValue > maxValue.Value)
                baseValue = maxValue.Value;

            string rangeInfo = "";
            if (minValue.HasValue && maxValue.HasValue)
                rangeInfo = $" [{minValue} - {maxValue}]";
            else if (minValue.HasValue)
                rangeInfo = $" [>= {minValue}]";
            else if (maxValue.HasValue)
                rangeInfo = $" [<= {maxValue}]";

            string promptText = $"\n{message}{rangeInfo}";

            PromptStringOptions pso = new PromptStringOptions(promptText)
            {
                DefaultValue = baseValue.ToString(),
                AllowSpaces = false,
                UseDefaultValue = true
            };

            var ed = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Editor;

            while (true)
            {
                PromptResult res = ed.GetString(pso);

                if (res.Status == PromptStatus.Cancel)
                {
                    result = 0;
                    return false;
                }
                else if (res.Status == PromptStatus.OK)
                {
                    if (string.IsNullOrEmpty(res.StringResult)) continue;

                    string cleanInput = res.StringResult.Trim();
                    if (!int.TryParse(cleanInput, out int iRes))
                        continue;

                    if (minValue.HasValue && iRes < minValue.Value)
                    {
                        ed.WriteMessage($"\nВведено число ниже допустимого значения - {minValue.Value}");
                        continue;
                    }
                    if (maxValue.HasValue && iRes > maxValue.Value)
                    {
                        ed.WriteMessage($"\nВведено число выше допустимого значения - {maxValue.Value}");
                        continue;
                    }

                    result = iRes;
                    return true;
                }
            }
        }
        #endregion

        #region точки
        /// <summary>
        /// Запрашивает у пользователя прямоугольную область на плоскости через две точки.
        /// Возвращает границы в системе координат WCS со сброшенной координатой Z.
        /// </summary>
        public static bool TryGetRegion(out Extents3d result)
        {
            result = new Extents3d();
            var ed = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Editor;

            // 1. Запрашиваем первую точку в UCS (передаем false в inWCS!), чтобы GetCorner отработал корректно
            if (!TryGetPointFromUser(out Point3d firstCornerUcs, false, "Выберите первый угол:", null))
                return false;

            // 2. Запрашиваем второй угол через GetCorner (он ожидает и возвращает точку в UCS)
            PromptPointOptions ppo = new PromptPointOptions("\nВыберите второй угол:")
            {
                BasePoint = firstCornerUcs,
                UseBasePoint = true,
                UseDashedLine = true // Добавляет пунктирную рамку при выборе, как в стандартных командах
            };

            PromptPointResult resCorner = ed.GetCorner(ppo);
            if (resCorner.Status != PromptStatus.OK) return false;

            Point3d secondCornerUcs = resCorner.Value;

            // 3. Переводим обе точки в WCS только СЕЙЧАС, перед формированием итогового результата
            Matrix3d ucsToWcs = ed.CurrentUserCoordinateSystem;
            Point3d firstCornerWcs = firstCornerUcs.TransformBy(ucsToWcs);
            Point3d secondCornerWcs = secondCornerUcs.TransformBy(ucsToWcs);

            // 4. Сбрасываем Z и формируем Extents3d
            // (Используем ваш метод .Z0(). Если это ваш кастомный метод расширения, он применится)
            Point3d p1 = new Point3d(firstCornerWcs.X, firstCornerWcs.Y, 0);
            Point3d p2 = new Point3d(secondCornerWcs.X, secondCornerWcs.Y, 0);

            result.AddPoint(p1);
            result.AddPoint(p2);

            return true;
        }

        public static bool TryGetPointFromUser(out Point3d result)
        {
            return TryGetPointFromUser(out result, true, "Выберите точку", null);
        }

        public static bool TryGetPointFromUser(out Point3d result, string message)
        {
            return TryGetPointFromUser(out result, true, message, null);
        }

        public static bool TryGetPointFromUser(out Point3d result, string message, Point3d? point)
        {
            return TryGetPointFromUser(out result, true, message, point);
        }

        /// <summary>
        /// Запрашивает у пользователя точку на чертеже.
        /// </summary>
        public static bool TryGetPointFromUser(out Point3d result, bool inWCS, string message, Point3d? point)
        {
            var ed = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Editor;
            PromptPointOptions ppo = new PromptPointOptions("\n" + message);

            if (point.HasValue)
            {
                ppo.BasePoint = point.Value;
                ppo.UseBasePoint = true;
            }

            // В GetPoint бесконечный цикл while(true) обычно избыточен, 
            // так как GetPoint не падает при неверном вводе (пользователь либо тыкает в экран, либо жмет Esc).
            PromptPointResult res = ed.GetPoint(ppo);

            if (res.Status == PromptStatus.OK)
            {
                result = res.Value;
                if (inWCS)
                    result = result.TransformBy(ed.CurrentUserCoordinateSystem);
                return true;
            }

            result = Point3d.Origin;
            return false;
        }

        #endregion

        #region объекты в точке

        public static bool GetObjectInPoint(out List<ObjectId> result, Type type, string message, List<ObjectId> excludes, Point3d? clickPoint = null, double? precision = null)
        {
            return GetObjectInPoint(out result, new List<Type> { type }, message, excludes, clickPoint, precision);
        }

        public static bool GetObjectInPoint(out List<ObjectId> result, List<Type> types, string message, List<ObjectId> excludes, Point3d? point = null, double? precision = null)
        {
            // Создаем локальный список-буфер для обхода ограничения CS1628
            List<ObjectId> localResult = new List<ObjectId>();
            double p = precision ?? Tolerance.Global.EqualPoint;

            var ed = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Editor;

            RXClass proxyClass = RXObject.GetClass(typeof(Autodesk.AutoCAD.DatabaseServices.ProxyEntity));

            var dxfNames = new List<string>();
            foreach (Type type in types)
            {
                RXClass currentClass = RXObject.GetClass(type);
                if (currentClass.IsDerivedFrom(proxyClass))
                {
                    dxfNames.Add("ACAD_PROXY_ENTITY");
                }
                else
                {
                    dxfNames.Add(currentClass.DxfName);
                }
            }
            string typeString = string.Join(",", dxfNames);

            SelectionFilter filter = new SelectionFilter(new[]
            {
                new TypedValue((int)DxfCode.Start, typeString)
            });

            HashSet<ObjectId> excludeSet = excludes != null && excludes.Count > 0
                ? new HashSet<ObjectId>(excludes)
                : null;

            // Теперь локальная функция безопасно наполняет localResult вместо out result
            bool ProcessSelection(PromptSelectionResult psr)
            {
                if (psr.Status == PromptStatus.OK)
                {
                    ObjectId[] ids = psr.Value.GetObjectIds();
                    if (excludeSet != null)
                    {
                        foreach (ObjectId id in ids)
                        {
                            if (!excludeSet.Contains(id)) localResult.Add(id);
                        }
                    }
                    else
                    {
                        localResult.AddRange(ids);
                    }
                }
                return localResult.Count > 0;
            }

            // Сценарий 1: Точка передана программно
            if (point.HasValue)
            {
                Point3d clickPoint = point.Value;
                Point3d pt1 = new Point3d(clickPoint.X - p, clickPoint.Y - p, 0);
                Point3d pt2 = new Point3d(clickPoint.X + p, clickPoint.Y + p, 0);

                PromptSelectionResult psr = ed.SelectCrossingWindow(pt1, pt2, filter);

                bool hasObjects = ProcessSelection(psr);
                result = localResult; // Присваиваем out параметр перед выходом
                return hasObjects;
            }

            // Сценарий 2: Интерактивный выбор пользователя
            while (true)
            {
                if (!TryGetPointFromUser(out Point3d clickPoint, false, message, null))
                {
                    result = localResult; // Присваиваем пустой или частичный список при отмене
                    return false;
                }

                Point3d pt1 = new Point3d(clickPoint.X - p, clickPoint.Y - p, 0);
                Point3d pt2 = new Point3d(clickPoint.X + p, clickPoint.Y + p, 0);

                PromptSelectionResult psr = ed.SelectCrossingWindow(pt1, pt2, filter);

                if (ProcessSelection(psr))
                {
                    result = localResult; // Присваиваем заполненный список перед успешным выходом
                    return true;
                }
            }
        }



        #endregion

        #region получение ObjectId

        /// <summary>
        /// Возвращает ObjectId выбранного элемента. Значение по умолчанию — любой объект чертежа.
        /// </summary>    
        public static bool TryGetobjectId(out ObjectId id)
        {
            return TryGetobjectId(out id, new List<Type>(), "Выберите объект", false);
        }

        /// <summary>
        /// Возвращает ObjectId выбранного элемента с кастомным сообщением.
        /// </summary>    
        public static bool TryGetobjectId(out ObjectId id, string message)
        {
            return TryGetobjectId(out id, new List<Type>(), message, false);
        }

        /// <summary>
        /// Возвращает ObjectId выбранного элемента с фильтрацией по одному типу.
        /// </summary>    
        public static bool TryGetobjectId(out ObjectId id, Type type, bool subclassInclude = false)
        {
            return TryGetobjectId(out id, new List<Type> { type }, "Выберите объект", subclassInclude);
        }

        /// <summary>
        /// Возвращает ObjectId выбранного элемента с фильтрацией по одному типу и кастомным сообщением.
        /// </summary>    
        public static bool TryGetobjectId(out ObjectId id, Type type, string message, bool subclassInclude = false)
        {
            return TryGetobjectId(out id, new List<Type> { type }, message, subclassInclude);
        }

        /// <summary>
        /// Возвращает ObjectId выбранного элемента с фильтрацией по списку типов.
        /// </summary>    
        public static bool TryGetobjectId(out ObjectId id, List<Type> objTypes, bool subclassInclude = false)
        {
            return TryGetobjectId(out id, objTypes, "Выберите object", subclassInclude);
        }

        // <summary>
        /// Единственный базовый метод: запрашивает у пользователя объект с нативной фильтрацией типов AutoCAD.
        /// </summary>
        /// <param name="id">Выходной ObjectId (ObjectId.Null в случае отмены или ошибки).</param>
        /// <param name="objTypes">Список разрешенных .NET типов. Если пустой или null — разрешен выбор любых объектов чертежа.</param>
        /// <param name="message">Сообщение для пользователя в командной строке.</param>
        /// <param name="subclassInclude">true — разрешить выбор классов-наследников (например, Polyline2d при типе Curve).</param>
        public static bool TryGetobjectId(out ObjectId id, List<Type> objTypes, string message, bool subclassInclude = false)
        {
            id = ObjectId.Null;
            var ed = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Editor;

            PromptEntityOptions peo = new PromptEntityOptions($"\n{message}");

            // Настраиваем нативный фильтр AutoCAD
            if (objTypes != null && objTypes.Count > 0)
            {
                // ВАЖНО: Сначала устанавливаем системное сообщение об ошибке при неверном выборе
                peo.SetRejectMessage("\nВыбран недопустимый тип объекта!");

                foreach (Type type in objTypes)
                {
                    if (type == null) continue;

                    // Теперь вызов AddAllowedClass отработает стабильно и без исключений
                    peo.AddAllowedClass(type, !subclassInclude);
                }
            }

            while (true)
            {
                PromptEntityResult entRes = ed.GetEntity(peo);

                if (entRes.Status == PromptStatus.Cancel)
                {
                    return false;
                }

                if (entRes.Status == PromptStatus.OK)
                {                   
                    id = entRes.ObjectId;
                    return true;
                }
            }
        }



        /// <summary>
        /// Запрашивает у пользователя выбор любых объектов чертежа.
        /// </summary>    
        public static bool TryGetObjectsIds(out List<ObjectId> result)
        {
            return TryGetObjectsIds(out result, new List<Type>(), "Выберите объекты", false);
        }

        /// <summary>
        /// Запрашивает у пользователя выбор любых объектов с кастомным сообщением.
        /// </summary>    
        public static bool TryGetObjectsIds(out List<ObjectId> result, string message)
        {
            return TryGetObjectsIds(out result, new List<Type>(), message, false);
        }

        /// <summary>
        /// Запрашивает у пользователя выбор объектов с фильтрацией по одному .NET-типу.
        /// </summary>      
        public static bool TryGetObjectsIds(out List<ObjectId> result, Type type, bool subclassInclude = false)
        {
            return TryGetObjectsIds(out result, new List<Type> { type }, "Выберите объекты", subclassInclude);
        }

        /// <summary>
        /// Запрашивает у пользователя выбор объектов с фильтрацией по одному .NET-типу и кастомным сообщением.
        /// </summary>    
        public static bool TryGetObjectsIds(out List<ObjectId> result, Type type, string message, bool subclassInclude = false)
        {
            return TryGetObjectsIds(out result, new List<Type> { type }, message, subclassInclude);
        }

        /// <summary>
        /// Запрашивает у пользователя выбор объектов с фильтрацией по списку .NET-типов.
        /// </summary>         
        public static bool TryGetObjectsIds(out List<ObjectId> result, List<Type> objTypes, bool subclassInclude = false)
        {
            return TryGetObjectsIds(out result, objTypes, "Выберите объекты", subclassInclude);
        }

        /// <summary>
        /// Запрашивает у пользователя выбор объектов с фильтрацией по списку родных классов AutoCAD (RXClass).
        /// </summary>
        public static bool TryGetObjectsIds(out List<ObjectId> result, List<RXClass> rxClasses, string message, bool subclassInclude = false)
        {
            // Просто переводим RXClass в системные типы Type и вызываем базовый метод, чтобы не дублировать код
            var types = new List<Type>();
            if (rxClasses != null)
            {
                foreach (var rxClass in rxClasses)
                {
                    if (rxClass != null && rxClass.GetRuntimeType() != null)
                        types.Add(rxClass.GetRuntimeType());
                }
            }
            return TryGetObjectsIds(out result, types, message, subclassInclude);
        }

        /// <summary>
        /// Единый базовый метод: запрашивает множественный выбор объектов с нативным DXF-фильтром и постобработкой наследников.
        /// </summary>
        /// <param name="result">Выходной список ObjectId (всегда инициализирован, пустой при отмене).</param>
        /// <param name="objTypes">Список разрешенных .NET типов (например, typeof(Line)). Если пуст — разрешены все типы.</param>
        /// <param name="message">Сообщение при добавлении объектов в набор.</param>
        /// <param name="subclassInclude">true — автоматически выбирать классы-наследники.</param>
        public static bool TryGetObjectsIds(out List<ObjectId> result, List<Type> objTypes, string message, bool subclassInclude = false)
        {
            result = new List<ObjectId>();
            var ed = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Editor;

            PromptSelectionOptions pOptions = new PromptSelectionOptions();
            if (!string.IsNullOrEmpty(message))
                pOptions.MessageForAdding = message;

            PromptSelectionResult pResult;

            // 1. Формируем нативный DXF-фильтр, если заданы типы ограничений
            if (objTypes != null && objTypes.Count > 0)
            {
                var dxfNames = new List<string>();
                RXClass proxyClass = RXObject.GetClass(typeof(ProxyEntity));

                foreach (Type type in objTypes)
                {
                    if (type == null) continue;
                    RXClass rxClass = RXObject.GetClass(type);
                    if (rxClass == null) continue;

                    if (rxClass.IsDerivedFrom(proxyClass))
                        dxfNames.Add("ACAD_PROXY_ENTITY");
                    else
                        dxfNames.Add(rxClass.DxfName);
                }

                // Если типы передали, но ни один корректный DXF-класс не распознан
                if (dxfNames.Count == 0) return false;

                // Склеиваем типы через запятую (AutoCAD нативно понимает логику "ИЛИ" для разделителя-запятой в DXF 0)
                string objectTypesAll = string.Join(",", dxfNames);
                SelectionFilter filter = new SelectionFilter(new[] { new TypedValue((int)DxfCode.Start, objectTypesAll) });

                pResult = ed.GetSelection(pOptions, filter);
            }
            else
            {
                // Без фильтра — выбираем вообще всё
                pResult = ed.GetSelection(pOptions);
            }

            // 2. Обрабатываем успешный выбор
            if (pResult.Status == PromptStatus.OK && pResult.Value != null)
            {
                ObjectId[] selectedIds = pResult.Value.GetObjectIds();

                // Если наследники НЕ нужны (subclassInclude = false), то нативный DXF-фильтр уже сделал всю работу идеально
                if (!subclassInclude || objTypes == null || objTypes.Count == 0)
                {
                    result.AddRange(selectedIds);
                    return result.Count > 0;
                }

                // Если subclassInclude = true, нам нужно отсеять лишнее, оставив только базовые типы и их наследников
                // (Так как нативный DXF-фильтр по строке "LINE,ARC" выберет строго Line и Arc, но пропустит кастомные типы-наследники, если они есть)
                var allowedClasses = new List<RXClass>();
                foreach (Type type in objTypes)
                {
                    if (type != null) allowedClasses.Add(RXObject.GetClass(type));
                }

                // Создаем буфер-список во избежание ограничений на out параметры
                List<ObjectId> filteredIds = new List<ObjectId>();

                foreach (ObjectId id in selectedIds)
                {
                    RXClass currentClass = id.ObjectClass;
                    foreach (RXClass allowedClass in allowedClasses)
                    {
                        if (currentClass.IsDerivedFrom(allowedClass))
                        {
                            filteredIds.Add(id);
                            break;
                        }
                    }
                }

                result = filteredIds;
                return result.Count > 0;
            }

            return false;
        }

        /// <summary>
        /// Возвращает список ObjectId из текущего предварительного выбора (Pickfirst), отфильтрованный по одному .NET типу.
        /// </summary>
        public static List<ObjectId> GetSelectImplied(this Type type)
        {
            if (type == null) return new List<ObjectId>();
            return GetSelectImplied(new List<Type> { type });
        }

        /// <summary>
        /// Возвращает список ObjectId из текущего предварительного выбора (Pickfirst), отфильтрованный по одному RXClass.
        /// </summary>
        public static List<ObjectId> GetSelectImplied(this RXClass rxClass)
        {
            if (rxClass == null) return new List<ObjectId>();

            var runtimeType = rxClass.GetRuntimeType();
            if (runtimeType == null) return new List<ObjectId>();

            return GetSelectImplied(new List<Type> { runtimeType });
        }

        /// <summary>
        /// Возвращает список ObjectId из текущего предварительного выбора (Pickfirst), отфильтрованный по списку RXClass.
        /// </summary>
        public static List<ObjectId> GetSelectImplied(List<RXClass> rxClasses)
        {
            var types = new List<Type>();
            if (rxClasses != null)
            {
                foreach (var rxClass in rxClasses)
                {
                    if (rxClass != null && rxClass.GetRuntimeType() != null)
                        types.Add(rxClass.GetRuntimeType());
                }
            }
            return GetSelectImplied(types);
        }

        /// <summary>
        /// Обычный статический метод: возвращает весь текущий предварительный выбор (Pickfirst) без фильтрации.
        /// </summary>
        public static List<ObjectId> GetSelectImplied()
        {
            // Вызываем базовый метод, передавая null вместо списка типов
            return GetSelectImpliedInternal(null);
        }

        /// <summary>
        /// Метод расширения: возвращает список ObjectId из текущего предварительного выбора (Pickfirst), отфильтрованный по списку .NET типов.
        /// </summary>
        public static List<ObjectId> GetSelectImplied(this List<Type> types)
        {
            return GetSelectImpliedInternal(types);
        }

        /// <summary>
        /// Внутренний базовый метод для объединения логики фильтрации.
        /// </summary>
        private static List<ObjectId> GetSelectImpliedInternal(List<Type> types)
        {
            var ids = new List<ObjectId>();
            var ed = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Editor;

            PromptSelectionResult result = ed.SelectImplied();

            if (result.Status != PromptStatus.OK || result.Value == null)
                return ids;

            ObjectId[] selectedIds = result.Value.GetObjectIds();

            // Если типы не заданы — возвращаем весь выбор
            if (types == null || types.Count == 0)
            {
                ids.AddRange(selectedIds);
                return ids;
            }

            var allowedClasses = new HashSet<RXClass>();
            foreach (Type type in types)
            {
                if (type == null) continue;
                RXClass rxClass = RXObject.GetClass(type);
                if (rxClass != null) allowedClasses.Add(rxClass);
            }

            if (allowedClasses.Count == 0) return ids;

            foreach (ObjectId id in selectedIds)
            {
                if (allowedClasses.Contains(id.ObjectClass))
                {
                    ids.Add(id);
                }
            }

            return ids;
        }
        #endregion

        #region получение ключевых слов
        /// <summary>
        /// Возвращает true, если пользователь выбрал ключевое слово из списка. Введенное слово возвращается в исходном регистре.
        /// </summary>
        public static bool TryGetKeywords(out string result, List<string> variants, string message)
        {
            result = string.Empty;

            // Предварительно очищаем список от пустых элементов и пробелов
            var validVariants = new List<string>();
            if (variants != null)
            {
                foreach (string v in variants)
                {
                    if (!string.IsNullOrWhiteSpace(v)) validVariants.Add(v);
                }
            }

            if (validVariants.Count == 0) return false;

            // Передаем чистое сообщение. AutoCAD сам нативно добавит скобки [ ] и < >
            PromptKeywordOptions pso = new PromptKeywordOptions("\n" + message)
            {
                AllowNone = false
            };

            foreach (string variant in validVariants)
            {
                pso.Keywords.Add(variant);
            }

            // Устанавливаем дефолтное значение из первого валидного элемента
            pso.Keywords.Default = validVariants[0];

            var ed = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Editor;
            PromptResult pr = ed.GetKeywords(pso);

            if (pr.Status == PromptStatus.OK)
            {
                // Возвращаем строку в том виде, в котором её отдал AutoCAD
                result = pr.StringResult;
                return true;
            }

            return false;
        }

        #endregion

        #region добавление и удаление объектов
        public static bool AddInSpace(Transaction tr, BlockTable bt, List<Entity> entitiesToInsert)
        {
            // 3. ⚡ ФИНАЛЬНЫЙ АККОРД: Выгружаем все в текущее активное пространство
            if (entitiesToInsert.Count > 0)
            {
                BlockTableRecord btrToJig = new BlockTableRecord { Name = "*U" };
                ObjectId btrToJigId = bt.Add(btrToJig);
                tr.AddNewlyCreatedDBObject(btrToJig, true);
                entitiesToInsert.AddEntityInCurrentBTR(btrToJigId, tr);
                BlockReference refToJig = new BlockReference(Point3d.Origin, btrToJigId);
                if (refToJig.EntityInsert(out ObjectId refToJigId))
                {
                    // 1. Взрываем (AutoCAD неявно переводит refToJig в режим ForRead)
                    refToJig.ExplodeToOwnerSpace();

                    // 2. Возвращаем режим записи и удаляем ссылку
                    refToJig.UpgradeOpen();
                    refToJig.Erase();

                    // 3. Возвращаем режим записи и удаляем описание
                    btrToJig.UpgradeOpen();
                    btrToJig.Erase();
                    return true;
                }
            }
            return false;
        }
        public static bool AddEntityInCurrentBTR(this Entity entity, Transaction transaction = null)
        {
            return entity.AddEntityInCurrentBTR(out _, ObjectId.Null, transaction);
        }
        public static bool AddEntityInCurrentBTR(this Entity entity, ObjectId targetSpaceId, Transaction transaction = null)
        {
            return entity.AddEntityInCurrentBTR(out _, targetSpaceId, transaction);
        }

        public static bool AddEntityInCurrentBTR(this Entity entity, out ObjectId id, ObjectId targetSpaceId = default, Transaction transaction = null)
        {
            bool result = AddEntityInCurrentBTR(new List<Entity> { entity }, out List<ObjectId> ids, targetSpaceId, transaction);
            id = ids.Count == 0 ? ObjectId.Null : ids[0];
            return result;
        }

        public static bool AddEntityInCurrentBTR(this List<Entity> entities, Transaction transaction = null)
        {
            return entities.AddEntityInCurrentBTR(ObjectId.Null, transaction);
        }

        public static bool AddEntityInCurrentBTR(this List<Entity> entities, ObjectId targetSpaceId, Transaction transaction = null)
        {
            return entities.AddEntityInCurrentBTR(out _, targetSpaceId, transaction);
        }

        public static bool AddEntityInCurrentBTR(this List<Entity> entities, out List<ObjectId> ids, ObjectId targetSpaceId = default, Transaction transaction = null)
        {
            ids = new List<ObjectId>();
            if (entities == null || entities.Count == 0) return false;

            Database db = HostApplicationServices.WorkingDatabase;
            bool newTransaction = transaction == null;

            try
            {
                if (newTransaction) transaction = db.TransactionManager.StartTransaction();

                // Если пространство не передано или передано как Null/default, пишем в текущее пространство
                ObjectId actualSpaceId = targetSpaceId == ObjectId.Null ? db.CurrentSpaceId : targetSpaceId;

                BlockTableRecord btr = (BlockTableRecord)transaction.GetObject(actualSpaceId, OpenMode.ForWrite);

                foreach (Entity e in entities)
                {
                    if (e == null || e.IsDisposed || !e.IsNewObject) continue;
                    ids.Add(btr.AppendEntity(e));
                    transaction.AddNewlyCreatedDBObject(e, true);
                }

                return true;
            }
            catch
            {
                return false;
            }
            finally
            {
                if (newTransaction)
                {
                    transaction?.Commit();
                    transaction?.Dispose();
                }
            }
        }

        public static bool DeleteEntity(this ObjectId id, Transaction tr = null)
        {
            return DeleteEntity(new List<ObjectId> { id }, tr);
        }
        public static bool DeleteEntity(this List<ObjectId> ids, Transaction tr = null)
        {
            try
            {
                if (tr == null)
                {
                    using (tr = HostApplicationServices.WorkingDatabase.TransactionManager.StartTransaction())
                    {
                        foreach (ObjectId id in ids)
                        {
                            if (id == null || id == ObjectId.Null || !id.IsValid || id.IsErased) continue;
                            (tr.GetObject(id, OpenMode.ForWrite, false, true) as Entity)?.Erase();
                        }
                        tr.Commit();
                    }
                }
                else
                {
                    foreach (ObjectId id in ids)
                    {
                        if (id == null || id == ObjectId.Null || !id.IsValid || id.IsErased) continue;
                        (tr.GetObject(id, OpenMode.ForWrite, false, true) as Entity)?.Erase();
                    }
                }
                return true;
            }
            catch { return false; }
        }
        public static bool DeleteEntity(this Entity entity)
        {
            return DeleteEntity(new List<Entity> { entity });
        }
        public static bool DeleteEntity(this List<Entity> entities)
        {
            try
            {
                foreach (Entity entity in entities)
                {
                    if (entity == null || entity.ObjectId == ObjectId.Null || entity.IsDisposed || entity.IsErased) continue;
                    if (!entity.IsWriteEnabled) entity.UpgradeOpen();
                    entity.Erase();
                }
            }
            catch { return false; }

            return true;
        }
        #endregion
    }



}
