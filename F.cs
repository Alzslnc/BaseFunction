﻿using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;

namespace BaseFunction
{
    public static class F
    {
        public static void ExtensionDictionaryErase(List<ObjectId> ids)
        {              
            using (Transaction tr = HostApplicationServices.WorkingDatabase.TransactionManager.StartTransaction())
            {
                foreach (ObjectId id in ids)
                {
                    using (Entity d = tr.GetObject(id, OpenMode.ForWrite) as Entity)
                    {
                        if (d == null || d.ExtensionDictionary == null || d.ExtensionDictionary == ObjectId.Null || d.ExtensionDictionary.IsErased) continue;
                        using (DBDictionary dd = tr.GetObject(d.ExtensionDictionary, OpenMode.ForWrite, true, true) as DBDictionary)
                        {
                            dd?.Erase();
                        }
                    }
                }
                tr.Commit();
            }
        }

        /// <summary>
        /// Добавляет в чертеж типы линий из списка
        /// </summary>
        /// <param name="ltNames">Список с названиями нужных типов линий (null или пустая строка поиск в файле acad.lin )</param>
        /// <param name="fileName">Файл из которого добавляются типы линий</param>
        /// <returns>true - все типы линий есть в чертеже, false - не удалось добавить какие либо типы из списка</returns>
        public static bool AddLineTypeTableRecord(List<string> ltNames, string fileName)
        {
            using (Transaction tr = HostApplicationServices.WorkingDatabase.TransactionManager.StartTransaction())
            {
                //открываем тaблицу типов линий
                using (LinetypeTable linetypeTable = tr.GetObject(HostApplicationServices.WorkingDatabase.LinetypeTableId, OpenMode.ForRead, false, true) as LinetypeTable)
                {
                    foreach (string ltName in ltNames)
                    {
                        //проверяем наличие линии
                        if (linetypeTable.Has(ltName)) continue;
                        //если тип линий отсутствует то пытаемся добавить 
                        try
                        {
                            if (string.IsNullOrEmpty(fileName)) HostApplicationServices.WorkingDatabase.LoadLineTypeFile(ltName, "acad.lin");
                            else HostApplicationServices.WorkingDatabase.LoadLineTypeFile(ltName, fileName);
                        }
                        catch
                        {
                            tr.Commit();
                            return false;
                        }
                    }
                }
                tr.Commit();
                return true;
            }
        }


        
               
        /// <summary>
        /// Выключает привязки на объектах из списка (утянул у киану вроде, точно не помню)
        /// </summary>
        /// <param name="ids">Список ObjectId объектов</param>
        public static void SnappingDisable(List<ObjectId> ids)
        {
            if (ids.Count == 0) return;
            ToggleOverruling(true);
            // Start a transaction to modify the entities' XData
            using (Transaction tr = HostApplicationServices.WorkingDatabase.TransactionManager.StartTransaction())
            {


                // Make sure our RegAppID is in the table
                var rat =
                  (RegAppTable)tr.GetObject(
                    HostApplicationServices.WorkingDatabase.RegAppTableId,
                    OpenMode.ForRead
                  );
                if (!rat.Has(regAppName))
                {
                    rat.UpgradeOpen();
                    var ratr = new RegAppTableRecord
                    {
                        Name = regAppName
                    };
                    rat.Add(ratr);
                    tr.AddNewlyCreatedDBObject(ratr, true);
                }
                // Create the XData and set it on the object
                using (
                  var rb =
                    new ResultBuffer(
                      new TypedValue(
                        (int)DxfCode.ExtendedDataRegAppName, regAppName
                      ),
                      new TypedValue(
                        (int)DxfCode.ExtendedDataInteger16, 1
                      )
                    )
                )
                {
                    foreach (ObjectId id in ids)
                    {
                        var ent = tr.GetObject(id, OpenMode.ForWrite, false, true) as Entity;
                        if (ent != null)
                        {
                            ent.XData = rb;
                        }
                    }
                };
                tr.Commit();
            }
        }
        /// <summary>
        /// Включает привязки на объектах из списка (утянул у киану вроде, точно не помню)
        /// </summary>
        /// <param name="ids">Список ObjectId объектов</param>
        public static void SnappingEnable(List<ObjectId> ids)
        {
            if (ids.Count == 0) return;
            using (Transaction tr = HostApplicationServices.WorkingDatabase.TransactionManager.StartTransaction())
            {
                using (RegAppTable rat = tr.GetObject(HostApplicationServices.WorkingDatabase.RegAppTableId, OpenMode.ForRead) as RegAppTable)
                {
                    if (!rat.Has(regAppName))
                    {
                        tr.Abort(); return;
                    }
                }
                // Create a ResultBuffer and use it to remove the XData
                // from the object
                using (var rb = new ResultBuffer(new TypedValue((int)DxfCode.ExtendedDataRegAppName, regAppName)))
                {
                    foreach (ObjectId id in ids)
                    {
                        using (var ent = tr.GetObject(id, OpenMode.ForWrite, false, true) as Entity)
                        {
                            if (ent != null)
                            {
                                ent.XData = rb;
                            }
                        }
                    }
                };
                tr.Commit();
            }
        }
        /// <summary>
        /// Очищает Xdata объекта
        /// </summary>
        /// <param name="id">ObjectId объекта</param>
        /// <param name="app">приложение чьи данные очищаем, если пусто или null чистим все</param>
        /// <param name="values">список удаляемых значений в приложении</param>
        /// <returns>true - если удалось очистить</returns>
        public static bool XDataClear(this ObjectId id, string app, List<TypedValue> values)
        {
            //если объект удален прекращаем
            if (id.IsErased) return false;
            using (Transaction tr = HostApplicationServices.WorkingDatabase.TransactionManager.StartTransaction())
            {
                //открываем объект на запись
                using (Entity ent = tr.GetObject(id, OpenMode.ForWrite, false, true) as Entity)
                {
                    if (ent != null)
                    {
                        //если приложение не выбрано
                        if (string.IsNullOrEmpty(app))
                        {
                            //получаем xdata объекта
                            using (ResultBuffer rb = ent.XData)
                            {
                                //если она присутствует
                                if (rb != null)
                                {
                                    //проходим по всей xdata
                                    foreach (TypedValue tv in rb)
                                    {
                                        //если находим приложение то перезаписываем ResultBuffer только с названием приложения, без данных, xdata удаляется только так
                                        if (tv.TypeCode.Equals(Convert.ToInt32(DxfCode.ExtendedDataRegAppName))) ent.XData = new ResultBuffer(new TypedValue(Convert.ToInt32(DxfCode.ExtendedDataRegAppName), tv.Value));
                                    }
                                }
                            }
                        }
                        //если приложение выбрано
                        else
                        {
                            try
                            {
                                //если нет списка данных для удаления то удаляем все данные от этого приложения
                                if (values == null || values.Count == 0)
                                {
                                    ent.XData = new ResultBuffer(new TypedValue(Convert.ToInt32(DxfCode.ExtendedDataRegAppName), app));
                                }
                                //если нужно удалить только часть данных
                                else
                                {
                                    //создаем новый ResultBuffer
                                    using (ResultBuffer rb = new ResultBuffer())
                                    {
                                        //считываем старый ResultBuffer
                                        using (ResultBuffer crb = ent.XData)
                                        {
                                            //проходим по парам, если она в списке на удаление пропускаем иначе записываем в новый ResultBuffer
                                            foreach (TypedValue tv in crb)
                                            {
                                                if (!values.Contains(tv)) rb.Add(tv);
                                            }
                                            //перезаписываем ResultBuffer на новый, без элементов на удаление
                                            ent.XData = rb;
                                        }
                                    }
                                }
                            }
                            catch
                            {
                                tr.Commit();
                                return false;
                            }
                        }
                    }
                }
                tr.Commit();
            }
            return true;
        }
        /// <summary>
        /// возвращает данные Xdata (в целом не особо полезно, единственный плюс можно не отрывать объект, работать через ObjectId)
        /// </summary>
        /// <param name="id">ObjectId объекта</param>
        /// <param name="app">приложение - чьи данные получаем или получаем все если null или пустое</param>
        /// <returns>ResultBuffer - список TypeValue</returns>
        public static ResultBuffer XDataGet(this ObjectId id, string app)
        {
            if (id.IsErased) return null;
            ResultBuffer resultBuffer = null;
            using (Transaction tr = HostApplicationServices.WorkingDatabase.TransactionManager.StartTransaction())
            {
                using (DBObject ent = tr.GetObject(id, OpenMode.ForRead, false, true) as DBObject)
                {
                    if (ent != null)
                    {
                        try
                        {
                            if (string.IsNullOrEmpty(app)) resultBuffer = ent.XData;
                            else resultBuffer = ent.GetXDataForApplication(app);
                        }
                        catch { }
                    }
                }
                tr.Commit();
            }
            return resultBuffer;
        }
        /// <summary>
        /// записывает данные в Xdata объекта
        /// </summary>
        /// <param name="id">ObjectId объекта</param>
        /// <param name="app">приложение данны которого записывам</param>
        /// <param name="datas">список TypedValue с данными</param>
        /// <param name="reSet">перезаписывать данные?</param>
        public static void XDataSet(this ObjectId id, string app, List<TypedValue> datas, bool reSet)
        {
            if (id.IsErased) return;
            try
            {
                //если нет названия приложения или данных прекращаем
                if (string.IsNullOrEmpty(app) || datas == null || datas.Count == 0) return;
                using (Transaction tr = HostApplicationServices.WorkingDatabase.TransactionManager.StartTransaction())
                {
                    //открываем объект на запись
                    using (DBObject ent = tr.GetObject(id, OpenMode.ForWrite, false, true) as DBObject)
                    {
                        if (ent != null)
                        {
                            //регистрируем приложение если еще не зарегистрировано
                            using (RegAppTable rat = tr.GetObject(HostApplicationServices.WorkingDatabase.RegAppTableId, OpenMode.ForWrite, false, true) as RegAppTable)
                            {
                                if (!rat.Has(app))
                                {
                                    using (RegAppTableRecord ratr = new RegAppTableRecord())
                                    {
                                        ratr.Name = app;
                                        rat.Add(ratr);
                                        tr.AddNewlyCreatedDBObject(ratr, true);
                                    }
                                }
                            }
                            //если перезаписываем то создаем новый 
                            if (reSet)
                            {
                                using (ResultBuffer rb = new ResultBuffer(new TypedValue(1001, app)))
                                {
                                    foreach (TypedValue v in datas) rb.Add(v);
                                    ent.XData = rb;
                                }
                            }
                            //если добавляем
                            else
                            {
                                //получаем данные этого приложения
                                using (ResultBuffer rb = ent.GetXDataForApplication(app))
                                {
                                    //если данных нет просто записываем новые
                                    if (rb == null)
                                    {
                                        using (ResultBuffer rb2 = new ResultBuffer(new TypedValue(1001, app)))
                                        {
                                            foreach (TypedValue v in datas) rb2.Add(v);
                                            ent.XData = rb2;
                                        }
                                    }
                                    //если данные есть то бавляем новые к уже существующим
                                    //(возможно требуется проверка на то, есть ли уже добавляемые в списке, не реализовано)
                                    else
                                    {
                                        foreach (TypedValue v in datas) rb.Add(v);
                                        ent.XData = rb;
                                    }
                                }
                            }
                        }
                    }
                    tr.Commit();
                }
            }
            catch { }
        }


        ////////////////////////////////////////////////////////
        ///////////////////вспомогательные функции//////////////
        ////////////////////////////////////////////////////////



        /// <summary>
        /// доволнительные функции для отключения/включения привязок от кеану
        /// </summary>
        public class OSOverrule : OsnapOverrule
        {
            public OSOverrule()
            {
                // Tell AutoCAD to filter on our application name
                // (this should mean our overrule only gets called
                // on objects possessing XData with this name)
                SetXDataFilter(regAppName);
            }
            public override void GetObjectSnapPoints(
              Entity ent,
              ObjectSnapModes mode,
              IntPtr gsm,
              Point3d pick,
              Point3d last,
              Matrix3d view,
              Point3dCollection snap,
              IntegerCollection geomIds
            )
            {
            }
            public override void GetObjectSnapPoints(
              Entity ent,
              ObjectSnapModes mode,
              IntPtr gsm,
              Point3d pick,
              Point3d last,
              Matrix3d view,
              Point3dCollection snaps,
              IntegerCollection geomIds,
              Matrix3d insertion
            )
            {
            }
            public override bool IsContentSnappable(Entity entity)
            {
                return false;
            }
        }
        public class IntOverrule : GeometryOverrule
        {
            public IntOverrule()
            {
                // Tell AutoCAD to filter on our application name
                // (this should mean our overrule only gets called
                // on objects possessing XData with this name)
                SetXDataFilter(regAppName);
            }
            public override void IntersectWith(
              Entity ent1,
              Entity ent2,
              Intersect intType,
              Plane proj,
              Point3dCollection points,
              IntPtr thisGsm,
              IntPtr otherGsm
            )
            {
            }
            public override void IntersectWith(
              Entity ent1,
              Entity ent2,
              Intersect intType,
              Point3dCollection points,
              IntPtr thisGsm,
              IntPtr otherGsm
            )
            {
            }
        }
        private static void ToggleOverruling(bool on)
        {
            if (on)
            {
                if (_osOverrule == null)
                {
                    _osOverrule = new OSOverrule();
                    ObjectOverrule.AddOverrule(
                      RXObject.GetClass(typeof(Entity)),
                      _osOverrule,
                      false
                    );
                }
                if (_geoOverrule == null)
                {
                    _geoOverrule = new IntOverrule();
                    ObjectOverrule.AddOverrule(
                      RXObject.GetClass(typeof(Entity)),
                      _geoOverrule,
                      false
                    );
                }
                ObjectOverrule.Overruling = true;
            }
            else
            {
                if (_osOverrule != null)
                {
                    ObjectOverrule.RemoveOverrule(
                      RXObject.GetClass(typeof(Entity)),
                      _osOverrule
                    );
                    _osOverrule.Dispose();
                    _osOverrule = null;
                }
                if (_geoOverrule != null)
                {
                    ObjectOverrule.RemoveOverrule(
                      RXObject.GetClass(typeof(Entity)),
                      _geoOverrule
                    );
                    _geoOverrule.Dispose();
                    _geoOverrule = null;
                }
                // I don't like doing this and so have commented it out:
                // there's too much risk of stomping on other overrules...
                // ObjectOverrule.Overruling = false;
            }
        }

        ////////////////////////////////////////////////////////
        ///////////////////переменные///////////////////////////
        ////////////////////////////////////////////////////////

        const string regAppName = "TTIF_SNAP";
        private static OSOverrule _osOverrule = null;
        private static IntOverrule _geoOverrule = null;
    }
}
