using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;

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
        public static bool TryGetPointFromUser(out Point3d result)
        {
            return TryGetPointFromUser(out result, true, "Выберите точку");
        }
        public static bool TryGetPointFromUser(out Point3d result, string message)
        {
            return TryGetPointFromUser(out result, true, message);
        }
        public static bool TryGetPointFromUser(out Point3d result, bool inWCS, string message)
        {
            Editor ed = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Editor;
            while (true)
            {
                PromptPointResult res = ed.GetPoint("\n" + message);
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
            RXClass rXClass = RXClass.GetClass(type);
            if (rXClass != null) return TryGetObjectsIds(out result, new List<string> { rXClass.DxfName }, message);
            else
            {
                result = new List<ObjectId>();
                return false;
            }
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
        public static bool TryGetKeywords(out string result, List<string> variants, string message)
        {
            result = string.Empty;
            if (variants.Count == 0) return false;

            bool allEmpty = true;

            message += " [";

            foreach (string variant in variants)
            {
                if (string.IsNullOrEmpty(variant)) continue;              
                allEmpty = false;
                message += variant.ToString().ToUpper() +"/";
            }

            message = message.Substring(0, message.Length - 1) + "]";

            if (allEmpty) return false;

            PromptKeywordOptions pso = new PromptKeywordOptions(message);

            foreach (string variant in variants)
            {
                if (string.IsNullOrEmpty(variant)) continue;
                pso.Keywords.Add(variant.ToUpper());
            }

            PromptResult pr = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Editor.GetKeywords(pso);

            if (pr.Status == PromptStatus.OK)
            {
                result = pr.StringResult;
                return true;
            }

            return false;
        }
        #endregion
    }
}
