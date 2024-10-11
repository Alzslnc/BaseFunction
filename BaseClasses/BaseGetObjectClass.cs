using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using agi = Autodesk.AutoCAD.GraphicsInterface;

namespace BaseFunction
{
    public static class BaseGetObjectClass
    {
        #region получение дробного числа
        /// <summary>
        /// Запрашивает у пользователя дробное число и если пользователь его ввел возвращает
        /// </summary>
        public static bool TryGetDoubleFromUser(out double result)
        {
            return TryGetDoubleFromUser(out result, 0, null, null, "Введите число");
        }
        /// <summary>
        /// Запрашивает у пользователя дробное число и если пользователь его ввел возвращает
        /// </summary>
        public static bool TryGetDoubleFromUser(out double result, string message)
        {
            return TryGetDoubleFromUser(out result, 0, null, null, message);
        }
        /// <summary>
        /// Запрашивает у пользователя дробное число и если пользователь его ввел возвращает
        /// </summary>
        public static bool TryGetDoubleFromUser(out double result, double baseValue)
        {
            return TryGetDoubleFromUser(out result, baseValue, null, null, "Введите число");
        }
        /// <summary>
        /// Запрашивает у пользователя дробное число и если пользователь его ввел возвращает
        /// </summary>
        public static bool TryGetDoubleFromUser(out double result, double? minValue, double? maxValue)
        {
            return TryGetDoubleFromUser(out result, 0, minValue, maxValue, "Введите число");
        }
        /// <summary>
        /// Запрашивает у пользователя дробное число и если пользователь его ввел возвращает
        /// </summary>
        /// <param name="result">Вывод полученного числа (0) если пользователь отменил выбор</param>
        /// <param name="baseValue">Число по умолчанию</param>
        /// <param name="minValue">Минимальный принимаемый результат</param>
        /// <param name="maxValue">Максимальный принимаемый результат</param>
        /// <param name="message">Сообщение для пользователя при выборе числа</param>
        /// <returns>true если пользователь ввел число, false если произвел отмену</returns>
        public static bool TryGetDoubleFromUser(out double result, double baseValue, double? minValue, double? maxValue, string message)
        {
            PromptStringOptions pso = new PromptStringOptions("\n" + message + "(" + minValue + " - " + maxValue + ")")
            {
                //текст в строке автокада
                DefaultValue = baseValue.ToString(),
                //могут ли быть пробелы в тексте от пользователя
                AllowSpaces = false,
                //добавление дефолтного значения в строку
                UseDefaultValue = true
            };
            //ждем от пользователя результат пока он его не выдаст или не отменит
            while (true)
            {
                PromptResult res = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Editor.GetString(pso);
                //если отмена возвращаем null
                if (res.Status == PromptStatus.Cancel)
                {
                    result = 0;
                    return false;
                }
                //если результат принят проверяем на допуски
                else if (res.Status == PromptStatus.OK)
                {
                    //если результат пустой то лпять запрашиваем
                    if (string.IsNullOrEmpty(res.StringResult)) continue;
                    //если результат не парсится то опять запрашиваем
                    if (!double.TryParse(res.StringResult.Replace(",", "."), out double dRes)) continue;
                    //если результат не в допуске то опять запрашиваем
                    if (minValue.HasValue && dRes < minValue)
                    {
                        Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage("\nВведено число ниже допустимого значения - " + minValue);
                        continue;
                    }
                    if (maxValue.HasValue && dRes > maxValue)
                    {
                        Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage("\nВведено число выше допустимого значения - " + maxValue);
                        continue;
                    }
                    //если все нормально возвращаем результат
                    result = dRes;
                    return true;
                }
            }
        }
        #endregion

        #region получение целого числа
        /// <summary>
        /// Запрашивает у пользователя дробное число и если пользователь его ввел возвращает
        /// </summary>
        public static bool TryGetIntFromUser(out int result)
        {
            return TryGetIntFromUser(out result, 0, null, null, "Введите целое число");
        }
        /// <summary>
        /// Запрашивает у пользователя дробное число и если пользователь его ввел возвращает
        /// </summary>
        public static bool TryGetIntFromUser(out int result, string message)
        {
            return TryGetIntFromUser(out result, 0, null, null, message);
        }
        /// <summary>
        /// Запрашивает у пользователя дробное число и если пользователь его ввел возвращает
        /// </summary>
        public static bool TryGetIntFromUser(out int result, int baseValue)
        {
            return TryGetIntFromUser(out result, baseValue, null, null, "Введите целое число");
        }
        /// <summary>
        /// Запрашивает у пользователя дробное число и если пользователь его ввел возвращает
        /// </summary>
        public static bool TryGetIntFromUser(out int result, int? minValue, int? maxValue)
        {
            return TryGetIntFromUser(out result, 0, minValue, maxValue, "Введите целое число");
        }
        /// <summary>
        /// Запрашивает у пользователя целое число и если пользователь его ввел возвращает
        /// </summary>
        /// <param name="result">Вывод полученного числа (0) если пользователь отменил выбор</param>
        /// <param name="baseValue">Число по умолчанию</param>
        /// <param name="minValue">Минимальный принимаемый результат</param>
        /// <param name="maxValue">Максимальный принимаемый результат</param>
        /// <param name="message">Сообщение для пользователя при выборе числа</param>
        /// <returns>true если пользователь ввел число, false если произвел отмену</returns>
        public static bool TryGetIntFromUser(out int result, int baseValue, int? minValue, int? maxValue, string message)
        {
            PromptStringOptions pso = new PromptStringOptions("\n" + message + "(" + minValue + " - " + maxValue + ")")
            {
                DefaultValue = baseValue.ToString(),
                AllowSpaces = false,
                UseDefaultValue = true
            };
            while (true)
            {
                PromptResult res = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Editor.GetString(pso);
                if (res.Status == PromptStatus.Cancel)
                {
                    result = 0;
                    return false;
                }
                else if (res.Status == PromptStatus.OK)
                {

                    if (string.IsNullOrEmpty(res.StringResult)) continue;
                    if (!int.TryParse(res.StringResult, out int iRes)) continue;
                    if (minValue.HasValue && iRes < minValue)
                    {
                        Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage("\nВведено число ниже допустимого значения - " + minValue);
                        continue;
                    }
                    if (iRes < minValue || iRes > maxValue)
                    {
                        Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage("\nВведено число выше допустимого значения - " + maxValue);
                        continue;
                    }
                    result = iRes;
                    return true;
                }
            }
        }
        #endregion

        #region точки
        public static bool TryGetRegion(out Extents3d result)
        { 
            result = new Extents3d();
            if (!TryGetPointFromUser(out Point3d firstCorner, "Выберите первый угол:")) return false;
            
            PromptPointResult promptPointResult = 
                Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Editor.GetCorner("\nВыберите второй угол:", firstCorner);

            if (promptPointResult.Status != PromptStatus.OK) return false;      

            result.AddPoint(firstCorner.Z0());
            result.AddPoint(promptPointResult.Value.Z0());

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
        public static bool TryGetPointFromUser(out Point3d result, bool inWCS, string message, Point3d? point)
        {
            Editor ed = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Editor;
            PromptPointOptions ppo = new PromptPointOptions("\n" + message);
            if (point.HasValue)
            {
                ppo.BasePoint = point.Value;
                ppo.UseBasePoint = true;
            }
            while (true)
            {
                PromptPointResult res = ed.GetPoint(ppo);
                if (res.Status == PromptStatus.Cancel)
                {
                    result = Point3d.Origin;
                    return false;
                }
                else if (res.Status == PromptStatus.OK)
                {
                    result = res.Value;
                    if (inWCS) result = result.TransformBy(ed.CurrentUserCoordinateSystem);
                    return true;
                }
            }
        }
        #endregion

        #region объекты в точке

        public static bool GetObjectInPoint(out List<ObjectId> result, Type type, string message, List<ObjectId> excludes, Point3d? clickPoint = null, double? precision = null)
        {
            return GetObjectInPoint(out result, new List<Type> { type },  message, excludes, clickPoint, precision);
        }

        public static bool GetObjectInPoint(out List<ObjectId> result, List<Type> types, string message, List<ObjectId> excludes, Point3d? point = null, double? precision = null)
        {
            result = new List<ObjectId>();

            Point3d clickPoint;

            if (point.HasValue) clickPoint = point.Value;
            else if (!TryGetPointFromUser(out clickPoint, message)) return false;          

            if (precision == null) precision = Tolerance.Global.EqualPoint;

            string typeString = "";
            foreach (Type type in types)
            { 
                typeString += RXClass.GetClass(type).DxfName + ",";
            }
            if (typeString.Length > 1) typeString = typeString.Substring(0, typeString.Length - 1);
            //выбираем типы для множественного выбора
            TypedValue[] values = new TypedValue[]
            {
                  new TypedValue((int)DxfCode.Start,typeString)
            };

            //объявляем фильтр
            SelectionFilter filter = new SelectionFilter(values);
            //создаем точки для выбора объектов в области
            Point3d pt1 = new Point3d(clickPoint.X - precision.Value, clickPoint.Y - precision.Value, 0);
            Point3d pt2 = new Point3d(clickPoint.X + precision.Value, clickPoint.Y + precision.Value, 0);
            //выбираем объекты в области вокруг выбранной точки
            PromptSelectionResult psr = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager
                //.MdiActiveDocument.Editor.SelectCrossingWindow(pt1, pt2, filter);
                .MdiActiveDocument.Editor.SelectCrossingPolygon(new Point3dCollection 
                { pt1, pt1 + Vector3d.XAxis * precision.Value * 2, pt2, pt1 + Vector3d.YAxis * precision.Value * 2}, filter);
            if (psr.Status != PromptStatus.OK || psr.Value.Count == 0) return false;

            result.AddRange(psr.Value.GetObjectIds().Except(excludes));

            return true;
        }

        #endregion

        #region получение ObjectId
        /// <summary>
        /// возвращает ObjectId выбранного элемента
        /// </summary>    
        public static bool TryGetobjectId(out ObjectId id)
        {
            return TryGetobjectId(out id, new List<string>(), "Выберите объект");
        }
        /// <summary>
        /// возвращает ObjectId выбранного элемента
        /// </summary>    
        public static bool TryGetobjectId(out ObjectId id, string message)
        {
            return TryGetobjectId(out id, new List<string>(), message);
        }
        /// <summary>
        /// возвращает ObjectId выбранного элемента
        /// </summary>    
        public static bool TryGetobjectId(out ObjectId id, Type type)
        {
            return TryGetobjectId(out id, type, "Выберите объект");
        }
        /// <summary>
        /// возвращает ObjectId выбранного элемента
        /// </summary>    
        public static bool TryGetobjectId(out ObjectId id, Type type, string message)
        {
            return TryGetobjectId(out id, new List<Type> { type }, message);
        }
        /// <summary>
        /// возвращает ObjectId выбранного элемента
        /// </summary>    
        public static bool TryGetobjectId(out ObjectId id, List<Type> objTypes)
        {
            return TryGetobjectId(out id, objTypes, "Выберите объект");
        }
        /// <summary>
        /// возвращает ObjectId выбранного элемента
        /// </summary>
        /// <param name="objTypes">допустимые типы объектов RXObject.GetClass(typeof(Circle)).Name, null или пустой список для выбора любых элементов</param>
        /// <param name="message">сообщение пользователю при выборе</param>
        /// <returns>ObjectId объекта или ObjectId.Null если произошла отмена выбора</returns>
        public static bool TryGetobjectId(out ObjectId id, List<Type> objTypes, string message)
        {
            List<string> typeString = new List<string>();
            foreach (Type type in objTypes)
            {
                RXClass rXClass = RXClass.GetClass(type);
                if (rXClass != null) typeString.Add(rXClass.Name);
            }
            if (objTypes.Count > 0 && typeString.Count == 0)
            {
                id = ObjectId.Null;
                return false;
            }
            return TryGetobjectId(out id, typeString, message);
        }
        /// <summary>
        /// возвращает ObjectId выбранного элемента
        /// </summary>
        /// <param name="objTypes">допустимые типы объектов RXObject.GetClass(typeof(Circle)).Name, null или пустой список для выбора любых элементов</param>
        /// <param name="message">сообщение пользователю при выборе</param>
        /// <returns>ObjectId объекта или ObjectId.Null если произошла отмена выбора</returns>
        public static bool TryGetobjectId(out ObjectId id, List<string> objTypes, string message)
        {
            //повторяем пока не выбран нужный объект
            while (true)
            {
                //Выбираем объект на чертеже
                PromptEntityResult entRes = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Editor.GetEntity("\n" + message);
                //Если объект не выбран то прекращаем программу
                if (entRes.Status == PromptStatus.Cancel)
                {
                    id = ObjectId.Null;
                    return false;
                }
                //проверка на тип объекта если выбранный объект корректен то выходим из цикла выбора
                if (objTypes == null || objTypes.Count == 0)
                {
                    id = entRes.ObjectId;
                    return true;
                }
                else
                {
                    foreach (string objType in objTypes)
                    {
                        if (entRes.ObjectId.ObjectClass.Name.ToLower().Equals(objType.ToLower()))
                        {
                            id = entRes.ObjectId;
                            return true;
                        }
                    }
                }
            }
        }


        /// <summary>
        /// Запрашивает у пользователя выбор объектов и возвращает их ObjectId
        /// </summary>    
        public static bool TryGetObjectsIds(out List<ObjectId> result)
        {
            return TryGetObjectsIds(out result, new List<string>(), "Выберите объекты");
        }
        /// <summary>
        /// Запрашивает у пользователя выбор объектов и возвращает их ObjectId
        /// </summary>    
        public static bool TryGetObjectsIds(out List<ObjectId> result, string message)
        {
            return TryGetObjectsIds(out result, new List<string>(), message);
        }
        /// <summary>
        /// Запрашивает у пользователя выбор объектов и возвращает их ObjectId
        /// </summary>      
        public static bool TryGetObjectsIds(out List<ObjectId> result, Type type)
        {
            return TryGetObjectsIds(out result, type, "Выберите объекты");
        }        
        /// <summary>
        /// Запрашивает у пользователя выбор объектов и возвращает их ObjectId
        /// </summary>    
        public static bool TryGetObjectsIds(out List<ObjectId> result, Type type, string message)
        {
            return TryGetObjectsIds(out result, new List<Type> { type }, message);
        }
        /// <summary>
        /// Запрашивает у пользователя выбор объектов и возвращает их ObjectId
        /// </summary>
        /// <param name="objectTypes">список типов для возможного выбора пользователя, null или пустой списко для выбора любых объектов</param>
        /// <returns>список ObjectId объекта или ObjectId.Null если произошла отмена выбора</returns>         
        public static bool TryGetObjectsIds(out List<ObjectId> result, List<Type> objTypes)
        {
            return TryGetObjectsIds(out result, objTypes, "Выберите объекты");
        }
        /// <summary>
        /// Запрашивает у пользователя выбор объектов и возвращает их ObjectId
        /// </summary>
        /// <param name="objectTypes">список типов для возможного выбора пользователя, null или пустой списко для выбора любых объектов</param>
        /// <returns>список ObjectId объекта или ObjectId.Null если произошла отмена выбора</returns>         
        public static bool TryGetObjectsIds(out List<ObjectId> result, List<Type> objTypes, string message)
        {
            List<string> typeString = new List<string>();
            foreach (Type type in objTypes)
            {
                if (type.Equals(typeof(ProxyEntity)))
                {
                    typeString.Add("ACAD_PROXY_ENTITY");
                    continue;
                }                    

                RXClass rXClass = RXClass.GetClass(type);
                if (rXClass != null) typeString.Add(rXClass.DxfName);
            }
            if (objTypes.Count > 0 && typeString.Count == 0)
            {
                result = new List<ObjectId>();
                return false;
            }
            return TryGetObjectsIds(out result, typeString, message);
        }
        /// <summary>
        /// Запрашивает у пользователя выбор объектов и возвращает их ObjectId
        /// </summary>
        /// <param name="objectTypes">DXF названия объектов ( RXObject.GetClass(typeof(Circle)).DxfName ) для возможного выбора пользователя, null или пустой списко для выбора любых объектов</param>
        /// <returns>список ObjectId объекта или ObjectId.Null если произошла отмена выбора</returns>   
        public static bool TryGetObjectsIds(out List<ObjectId> result, List<string> objectTypes, string message)
        {
            //создаем результат выбора
            PromptSelectionResult pResult;
            PromptSelectionOptions pOptions = new PromptSelectionOptions();
            if (!string.IsNullOrEmpty(message)) pOptions.MessageForAdding = message;
            //создаем список Id
            result = new List<ObjectId>();
            if (objectTypes != null && objectTypes.Count > 0)
            {
                //создаем строку с типами объектов для фильтра
                string objectTypesAll = string.Empty;
                foreach (string objectType in objectTypes) objectTypesAll = objectTypesAll + objectType + ",";
                objectTypesAll = objectTypesAll.Substring(0, objectTypesAll.Length - 1);
                pResult = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Editor.GetSelection(pOptions,
                    new SelectionFilter(new TypedValue[] { new TypedValue((int)DxfCode.Start, objectTypesAll) }));
            }
            else
            {
                pResult = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Editor.GetSelection(pOptions);
            }
            //записываем Id выбранных объектов в список и возвращаем его
            if (pResult.Status == PromptStatus.OK)
            {
                result.AddRange(pResult.Value.GetObjectIds());
                return true;
            }
            return false;
        }
        #endregion

        #region получение ключевых слов
        /// <summary>
        /// Возвращает true если пользователь выбрал ключевое слово из списка (само слово возвращается в верхнем регистре)
        /// </summary>
        public static bool TryGetKeywords(out string result, List<string> variants, string message)
        {
            result = string.Empty;
            if (variants.Count == 0) return false;

            //bool allEmpty = true;

            //message += " [";

            //foreach (string variant in variants)
            //{
            //    if (string.IsNullOrEmpty(variant)) continue;
            //    allEmpty = false;
            //    message += variant.ToString().ToUpper() + "/";
            //}

            //message = message.Substring(0, message.Length - 1) + "]";

            //if (allEmpty) return false;

            PromptKeywordOptions pso = new PromptKeywordOptions(message)
            {
                AllowNone = false
            };

            //PromptStringOptions pso = new PromptStringOptions(message);

            foreach (string variant in variants)
            {
                if (string.IsNullOrEmpty(variant)) continue;
                pso.Keywords.Add(variant);               
            }

            pso.Keywords.Default = variants[0];
            //pso.AppendKeywordsToMessage = true;

            PromptResult pr = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Editor.GetKeywords(pso);

            if (pr.Status == PromptStatus.OK)
            {
                result = pr.StringResult;
                return true;
            }

            return false;
        }
        #endregion

        #region добавление и удаление объектов
        public static bool AddEntityInCurrentBTR(this Entity entity)
        {
            return entity.AddEntityInCurrentBTR(out _);
        }
        public static bool AddEntityInCurrentBTR(this Entity entity, out ObjectId id)
        {
            id = ObjectId.Null;
            if (!entity.IsNewObject) return false;
            try
            {
                using (Transaction tr = HostApplicationServices.WorkingDatabase.TransactionManager.StartTransaction())
                {
                    using (BlockTableRecord ms = tr.GetObject(HostApplicationServices.WorkingDatabase.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord)
                    {
                        id = ms.AppendEntity(entity);
                        tr.AddNewlyCreatedDBObject(entity, true);
                    }
                    tr.Commit();
                }
                return true;
            }
            catch { return false; }
        }
        public static bool AddEntityInCurrentBTR(this List<Entity> entities)
        {
            return entities.AddEntityInCurrentBTR(out _);
        }
        public static bool AddEntityInCurrentBTR(this List<Entity> entities, out List<ObjectId> ids)
        {
            ids = new List<ObjectId>();
            try
            {
                using (Transaction tr = HostApplicationServices.WorkingDatabase.TransactionManager.StartTransaction())
                {
                    using (BlockTableRecord ms = tr.GetObject(HostApplicationServices.WorkingDatabase.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord)
                    {
                        foreach (Entity e in entities)
                        {
                            if (!e.IsNewObject) continue;
                            ids.Add(ms.AppendEntity(e));
                            tr.AddNewlyCreatedDBObject(e, true);
                        }
                    }
                    tr.Commit();
                }
                return true;
            }
            catch { return false; }
        }

        public static bool DeleteEntity(this ObjectId id)
        {
            return DeleteEntity(new List<ObjectId> { id });
        }
        public static bool DeleteEntity(this List<ObjectId> ids)
        {
            try
            {
                using (Transaction tr = HostApplicationServices.WorkingDatabase.TransactionManager.StartTransaction())
                {
                    foreach (ObjectId id in ids)
                    {
                        if (id == null || id == ObjectId.Null || !id.IsValid || id.IsErased) continue;
                        using (Entity entity = tr.GetObject(id, OpenMode.ForWrite, false, true) as Entity)
                        {
                            entity?.Erase();
                        }
                    }
                    tr.Commit();
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
