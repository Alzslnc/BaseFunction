using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;

namespace BaseFunction
{
    /// <summary>
    /// Сохраняем параметры полученного элемента и сравнивает их с выбранными
    /// </summary>
    public class CheckEntityClass : IDisposable
    {
        #region конструкторы
        /// <summary>
        /// Сохраняет параметры объекта для последующего сравнения (по умолчанию цвет и слой)
        /// </summary>
        /// <param name="ids">список ObjectId исходных объектов</param>
        public CheckEntityClass(List<ObjectId> ids)
        {
            UseList = true;
            GetParametr(ids);
        }
        /// <summary>
        /// Сохраняет параметры объекта для последующего сравнения
        /// </summary>
        /// <param name="ids">список ObjectId исходных объектов</param>
        /// <param name="_UseColor">Сравнивать цвет?</param>
        /// <param name="_UseLayer">Сравнивать слой?</param>
        /// <param name="_UseLineWeight">Сравнивать вес линий?</param>
        /// <param name="_UseLineType">Сравнивать тип линий?</param>
        /// <param name="_UseClassType">Сравнивать класс объекта?</param>
        public CheckEntityClass(List<ObjectId> ids, bool _UseColor, bool _UseLayer, bool _UseLineWeight, bool _UseLineType, bool _UseClassType)
        {
            UseList = true;
            UseColor = _UseColor;
            UseLayer = _UseLayer;
            UseLineWeight = _UseLineWeight;
            UseClassType = _UseClassType;
            UseLineType = _UseLineType;
            GetParametr(ids);
        }
        /// <summary>
        /// Сохраняет параметры объекта для последующего сравнения (по умолчанию цвет и слой)
        /// </summary>
        /// <param name="id">ObjectId исходного объекта</param>
        public CheckEntityClass(ObjectId id)
        {
            GetParametr(id);
        }
        /// <summary>
        /// Сохраняет параметры объекта для последующего сравнения
        /// </summary>
        /// <param name="id">ObjectId исходного объекта</param>
        /// <param name="_UseColor">Сравнивать цвет?</param>
        /// <param name="_UseLayer">Сравнивать слой?</param>
        /// <param name="_UseLineWeight">Сравнивать вес линий?</param>
        /// <param name="_UseLineType">Сравнивать тип линий?</param>
        /// <param name="_UseClassType">Сравнивать класс объекта?</param>
        public CheckEntityClass(ObjectId id, bool _UseColor, bool _UseLayer, bool _UseLineWeight, bool _UseLineType, bool _UseClassType)
        {
            UseColor = _UseColor;
            UseLayer = _UseLayer;
            UseLineWeight = _UseLineWeight;
            UseClassType = _UseClassType;
            UseLineType = _UseLineType;
            GetParametr(id);
        }
        #endregion
        #region открытые методы
        /// <summary>
        /// Проверяет эквивалентность объекта через его ObjectId в соответствии с выбранными параметрами
        /// </summary>
        /// <param name="id">ObjectId объекта</param>
        /// <returns></returns>
        public bool IsEqualBase(ObjectId id)
        {
            ClassCheck();
            ObjectIdCheck(id);
            if (UseClassType == true && UseColor == false && UseLayer == false && UseLineType == false && UseLineWeight == false)
            {
                if (UseList)
                {
                    foreach ((_, _, _, _, Type tp) in ParametrList)
                    {
                        if (id.GetType().Equals(tp)) return true;
                    }
                    return false;
                }
                else if (id.GetType().Equals(Parametr.tp)) return true;
                return false;
            }
            using (Transaction tr = HostApplicationServices.WorkingDatabase.TransactionManager.StartTransaction())
            {
                using (Entity e = tr.GetObject(id, OpenMode.ForRead, false, true) as Entity)
                {
                    EntityCheck(e);
                    bool result = IsEqual(e);
                    tr.Commit();
                    return result;
                }
            }
        }
        /// <summary>
        /// Проверяет эквивалентность объекта в соответствии с выбранными параметрами
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public bool IsEqualBase(Entity e)
        {
            ClassCheck();
            EntityCheck(e);
            return IsEqual(e);
        }
        /// <summary>
        /// Проверяет эквивалентность объектов из списка и возвращает список прошедших проверку объектов
        /// </summary>
        /// <param name="result">Список объектов прошедших проверку</param>
        /// <param name="objects">Список исходных объектов</param>
        /// <returns>true если хотя бы один объект прошел проверку иначе false</returns>
        public bool GetEqualBaseIds(out List<ObjectId> result, List<ObjectId> objects)
        {
            ClassCheck();
            foreach (ObjectId id in objects)
            {
                ObjectIdCheck(id);
            }
            result = new List<ObjectId>();
            if (UseClassType == true && UseColor == false && UseLayer == false && UseLineType == false && UseLineWeight == false)
            {
                foreach (ObjectId id in objects)
                {
                    if (UseList)
                    {
                        foreach ((_, _, _, _, Type tp) in ParametrList)
                        {
                            if (id.GetType().Equals(tp))
                            {
                                result.Add(id);
                                break;
                            }
                        }
                    }
                    else if (id.GetType().Equals(Parametr.tp)) result.Add(id);
                }
            }
            else
            {
                using (Transaction tr = HostApplicationServices.WorkingDatabase.TransactionManager.StartTransaction())
                {
                    foreach (ObjectId id in objects)
                    {
                        using (Entity e = tr.GetObject(id, OpenMode.ForRead, false, true) as Entity)
                        {
                            EntityCheck(e);
                            if (IsEqual(e)) result.Add(id);
                        }
                    }
                }
            }
            if (result.Count == 0) return false;
            return true;
        }
        #endregion
        #region закрытые методы
        private void GetParametr(ObjectId id)
        {
            ObjectIdCheck(id);
            using (Transaction tr = HostApplicationServices.WorkingDatabase.TransactionManager.StartTransaction())
            {
                using (Entity e = tr.GetObject(id, OpenMode.ForRead, false, true) as Entity)
                {
                    if (e != null)
                    {
                        Parametr = (e.Color, e.Layer, e.LineWeight, e.Linetype, e.GetType());
                        Exist = true;
                    }
                }
                tr.Commit();
            }
        }
        private void GetParametr(List<ObjectId> ids)
        {
            using (Transaction tr = HostApplicationServices.WorkingDatabase.TransactionManager.StartTransaction())
            {
                foreach (ObjectId id in ids)
                {
                    ObjectIdCheck(id);
                    using (Entity e = tr.GetObject(id, OpenMode.ForRead, false, true) as Entity)
                    {
                        if (e != null)
                        {
                            (Color col, string lay, LineWeight lw, string lt, Type tp) parametr = (e.Color, e.Layer, e.LineWeight, e.Linetype, e.GetType());
                            if (!ParametrList.Contains(parametr)) ParametrList.Add(parametr);
                            Exist = true;
                        }
                        else
                        {
                            throw new Exception("ObjectIsNotEntity");
                        }
                    }
                }
                tr.Commit();
            }
        }
        private bool IsEqual(Entity e)
        {
            if (UseList)
            {
                foreach ((Color col, string lay, LineWeight lw, string lt, Type tp) parametr in ParametrList)
                {
                    if (IsEqual(e, parametr)) return true;
                }
                return false;
            }
            else
            {
                return IsEqual(e, Parametr);
            }
        }
        private bool IsEqual(Entity e, (Color col, string lay, LineWeight lw, string lt, Type tp) parametr)
        {
            if (UseColor && !e.Color.Equals(parametr.col)) return false;
            if (UseLayer && !e.Layer.Equals(parametr.lay)) return false;
            if (UseLineWeight && !e.LineWeight.Equals(parametr.lw)) return false;
            if (UseLineType && !e.Linetype.Equals(parametr.lt)) return false;
            if (UseClassType && !e.GetType().Equals(parametr.tp)) return false;
            return true;
        }
        private void ObjectIdCheck(ObjectId id)
        {
            if (id == null)
            {
                throw new Exception("ObjectIdIsnull");
            }
            if (id == ObjectId.Null)
            {
                throw new Exception("ObjectIdIsObjectId.Null");
            }
            if (!id.IsValid)
            {
                throw new Exception("ObjectIdIsNotValid");
            }
            if (id.IsErased)
            {
                throw new Exception("ObjectIsErased");
            }
        }
        private void EntityCheck(Entity e)
        {
            if (e == null)
            {
                throw new Exception("ObjectIsNull");
            }
            if (e.IsDisposed)
            {
                throw new Exception("ObjectDisposed");
            }
            if (e.IsErased)
            {
                throw new Exception("ObjectIsErased");
            }
        }
        private void ClassCheck()
        {
            if (IsDisposed)
            {
                throw new Exception("ClassDisposed");
            }
            if (!Exist)
            {
                throw new Exception("BaseObjectNotExist");
            }
        }
        #endregion
        #region открытые переменные
        public bool UseColor { get; set; } = true;
        public bool UseLayer { get; set; } = true;
        public bool UseLineWeight { get; set; } = false;
        public bool UseLineType { get; set; } = false;
        public bool UseClassType { get; set; } = false;
        public bool Exist { get; set; } = false;
        #endregion
        #region закрытые переменные
        private bool UseList { get; set; } = false;
        private (Color col, string lay, LineWeight lw, string lt, Type tp) Parametr { get; set; } = (null, string.Empty, LineWeight.ByLineWeightDefault, string.Empty, null);
        private List<(Color col, string lay, LineWeight lw, string lt, Type tp)> ParametrList { get; set; } = new List<(Color col, string lay, LineWeight lw, string lt, Type tp)>();
        private bool IsDisposed { get; set; } = false;
        #endregion
        public void Dispose()
        {
            if (IsDisposed) return;
            IsDisposed = true;
            ParametrList.Clear();
            Exist = false;
        }
    }
}
