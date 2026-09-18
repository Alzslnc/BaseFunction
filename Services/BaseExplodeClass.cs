using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace BaseFunction
{
    public static class BaseExplodeClass
    {
        
        /// <summary>
        /// получает составные элементы объекта и добавляет их в чертеж, возвращает их ObjectId
        /// </summary>
        public static List<ObjectId> ExplodeObject(Entity e, bool erase)
        {
            using (Transaction tr = HostApplicationServices.WorkingDatabase.TransactionManager.StartTransaction())
            {
                using (BlockTableRecord ms = tr.GetObject(HostApplicationServices.WorkingDatabase.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord)
                {
                    List<ObjectId> result = ExplodeObject(e, tr, ms, erase);
                    tr.Commit();
                    return result;
                }
            }
        }
        /// <summary>
        /// получает составные элементы объекта и добавляет их в чертеж, возвращает их ObjectId
        /// </summary>
        public static List<ObjectId> ExplodeObject(Entity e, Transaction tr, BlockTableRecord ms, bool erase)
        {
            List<ObjectId> result = new List<ObjectId>();
            try
            {
                if (e is MText) result.Add(e.ObjectId);
                else if (e is BlockReference reference)
                {
                    result.AddRange(ExplodeBlock(tr, HostApplicationServices.WorkingDatabase, ObjectId.Null, true, false, true, true, reference, ms));                   
                }
                else if (e is ProxyEntity proxyEntity)
                {
                    ProcessProxyEntity(proxyEntity, tr, ms, HostApplicationServices.WorkingDatabase, false, result, null);
                    if (erase && !proxyEntity.IsErased)
                    {
                        proxyEntity.UpgradeOpen();
                        proxyEntity.Erase();
                    }
                }
                else 
                {
                    using (DBObjectCollection coll = new DBObjectCollection())
                    {
                        e.Explode(coll);
                        foreach (DBObject obj in coll)
                        {
                            if (obj is Entity newE)
                            {
                                // ПРАВИЛЬНО: добавляем в БД, обертку НЕ уничтожаем через Dispose вручную!
                                result.Add(ms.AppendEntity(newE));
                                tr.AddNewlyCreatedDBObject(newE, true);
                            }
                            else
                            {
                                // Объекты, не являющиеся Entity (параметризация), безопасно удаляем из ОЗУ
                                obj?.Dispose();
                            }
                        }

                        if (erase && !e.IsErased)
                        {
                            e.UpgradeOpen();
                            e.Erase();
                        }
                    }
                }
            }
            catch { }
            return result;
        }

        /// <summary>
        /// Расчленяет вставку блока и возвращает ObjectId всех полученных элементов.
        /// </summary>
        public static List<ObjectId> ExplodeBlock(Transaction tr, Database db, ObjectId id, bool erase, bool inLayer, bool recursive, bool explodeProxy, BlockReference br = null, BlockTableRecord ms = null)
        {
            List<ObjectId> result = new List<ObjectId>();
            List<ObjectId> toExplode = new List<ObjectId>();
            List<ObjectId> attrList = new List<ObjectId>();
            List<ObjectId> dimList = new List<ObjectId>();
            List<ObjectId> toDelete = new List<ObjectId>();

            try
            {
                if (ms == null) ms = tr.GetObject(HostApplicationServices.WorkingDatabase.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord;

                if (br == null) br = tr.GetObject(id, OpenMode.ForWrite, false, true) as BlockReference;
                if (br == null) return result;
                               
                // Расчет масштаба для размеров
                Scale3d scale3D = br.ScaleFactors;
                double scale = Math.Abs(scale3D.X).IsEqualTo(Math.Abs(scale3D.Y)) ? Math.Abs(scale3D.X) : 1.0;

                // 1. Выносим обработку атрибутов в отдельный метод
                if (br.AttributeCollection.Count > 0)
                {
                    ProcessAttributes(tr, ms, br, inLayer, result);
                }
                        
                // Локальный обработчик для перехвата объектов, созданных методом ExplodeToOwnerSpace
                void ObjectAppendedHandler(object s, ObjectEventArgs e)
                {
                    DBObject obj = e.DBObject;

                    if (obj is Entity ent)
                    {
                        // 1. Проверяем, нужно ли сразу отправить объект в список на удаление
                        if (FilterAndCheckIfShouldDelete(ent))
                        {
                            toDelete.Add(obj.ObjectId);
                            return;
                        }

                        // 2. Стандартная сортировка оставшихся валидных объектов
                        if (obj is BlockReference && recursive) toExplode.Add(obj.ObjectId);
                        else if (obj is ProxyEntity proxy) toDelete.Add(obj.ObjectId);
                        else if (obj is AttributeDefinition) attrList.Add(obj.ObjectId);
                        else if (obj is Dimension) dimList.Add(obj.ObjectId);
                        else result.Add(obj.ObjectId);
                    }

                    else toDelete.Add(obj.ObjectId);
                }

                // Подписываемся на событие, взрываем в пространство владельца и отписываемся
                db.ObjectAppended += ObjectAppendedHandler;
                // 2. Выносим ручную обработку прокси-объектов в отдельный метод
                try
                {
                    if (explodeProxy)
                    {
                        ProcessProxyEntities(tr, db, ms, br, inLayer, result);
                    }
                }
                catch { }
                try
                {
                    br.ExplodeToOwnerSpace();
                    // Удаляем исходный блок
                    if (erase && !br.IsErased)
                    {
                        br.Erase();
                    }
                }
                catch (Autodesk.AutoCAD.Runtime.Exception ex)
                {
                    try
                    {
                        SecondExplodeType(br, tr, ms);
                        // Удаляем исходный блок
                        if (erase && !br.IsErased)
                        {
                            br.Erase();
                        }
                    }
                    catch { }
                }
                finally
                {
                    
                    db.ObjectAppended -= ObjectAppendedHandler;
                }

                // Рекурсивный спуск по вложенным блокам
                foreach (ObjectId bid in toExplode)
                {
                    result.AddRange(ExplodeBlock(tr, db, bid, erase, inLayer, recursive, explodeProxy, ms: ms));
                }

                // Удаляем служебные определения атрибутов (AttributeDefinition), оставшиеся от взрыва
                foreach (ObjectId attrId in attrList)
                {
                    Entity e = tr.GetObject(attrId, OpenMode.ForWrite, false, true) as Entity;
                    if (e != null && !e.IsErased) e.Erase();
                }

                // Корректируем масштаб размеров (Dimension)
                if (scale != 1.0)
                {
                    foreach (ObjectId dimId in dimList)
                    {
                        // Метод сам разберется: кого клонировать, кого просто перекрасить, 
                        // и применит Dimscale/Dimlfac ко всем без исключения за один вызов tr.GetObject
                        ObjectId actualId = ConvertAndScaleDimension(tr, ms, dimId, scale);

                        if (actualId != ObjectId.Null)
                        {
                            result.Add(actualId); // Добавляем итоговый размер в финальный результат
                        }
                    }
                }

                // Переносим элементы на слой родительского блока, если требуется
                foreach (ObjectId objectId in result)
                {
                    Entity e = tr.GetObject(objectId, OpenMode.ForWrite, false, true) as Entity;
                    if (e != null) ResolveEntityProperties(e, br, inLayer);
                }

                // Физически удаляем объекты, попавшие под фильтр мусора
                foreach (ObjectId deleteId in toDelete)
                {
                    DBObject e = tr.GetObject(deleteId, OpenMode.ForWrite, false, true) as DBObject;
                    if (e != null && !e.IsErased)
                        e.Erase();                    
                }                                
            }
            catch { }
            return result;
        }

        private static void SecondExplodeType(BlockReference br, Transaction tr, BlockTableRecord ms)
        {
            using (DBObjectCollection collection = new DBObjectCollection())
            {
                br.Explode(collection);

                List<Entity> entities = new List<Entity>();

                foreach (DBObject dBObject in collection)
                {
                    if (dBObject is Entity e) entities.Add(e);
                    else dBObject?.Dispose();                
                }

                entities.AddEntityInCurrentBTR(out _, ms, tr);                
            }
        }

        private static bool FilterAndCheckIfShouldDelete(Entity obj)
        {
            if (obj == null) return false;           

            if (!obj.Visible) return true;
                   
            string typeName = obj.GetType().Name;
                        
            if (obj is DBText dbText && string.IsNullOrWhiteSpace(dbText.TextString))
            {
                return true;
            }

            if (obj is MText mText && string.IsNullOrWhiteSpace(mText.Contents))
            {
                return true;
            }

            if (obj is Curve line && line.GetLength().IsEqualTo(0.0))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Извлекает атрибуты блока и преобразует их в текстовые элементы на чертеже.
        /// </summary>
        private static void ProcessAttributes(Transaction tr, BlockTableRecord ms, BlockReference br, bool forceLayer, List<ObjectId> resultList)
        {
            foreach (ObjectId attRefId in br.AttributeCollection)
            {
                using (AttributeReference attr = tr.GetObject(attRefId, OpenMode.ForRead, false, true) as AttributeReference)
                {
                    if (attr == null || (!attr.Visible && attr.Invisible)) continue;

                    if (attr.IsMTextAttribute)
                    {
                        using (MText nText = attr.MTextAttribute)
                        {
                            ResolveEntityProperties(nText, br, forceLayer);
                            resultList.Add(ms.AppendEntity(nText));
                            tr.AddNewlyCreatedDBObject(nText, true);
                        }
                    }
                    else
                    {
                        using (DBText nText = new DBText())
                        {
                            nText.SetPropertiesFrom(attr);
                            nText.Height = attr.Height;
                            nText.Color = attr.Color;
                            nText.Layer = attr.Layer;
                            nText.TextStyleId = attr.TextStyleId;
                            nText.Linetype = attr.Linetype;
                            nText.LineWeight = attr.LineWeight;
                            nText.Position = attr.Position;
                            nText.TextString = attr.TextString;
                            nText.Justify = attr.Justify;
                            nText.WidthFactor = attr.WidthFactor;
                            nText.Rotation = attr.Rotation;

                            if (attr.Justify != AttachmentPoint.BaseLeft)
                                nText.AlignmentPoint = attr.AlignmentPoint;

                            ResolveEntityProperties(nText, br, forceLayer);

                            resultList.Add(ms.AppendEntity(nText));
                            tr.AddNewlyCreatedDBObject(nText, true);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Извлекает геометрию из Proxy-объектов, находящихся внутри определения блока, с исправлением выравнивания текстов.
        /// </summary>
        private static void ProcessProxyEntities(Transaction tr, Database db, BlockTableRecord ms, BlockReference br, bool forceLayer, List<ObjectId> resultList)
        {
            // Важно: всегда читаем br.BlockTableRecord, чтобы захватить состояние измененного динамического блока (*X...)
            ObjectId btrId = br.BlockTableRecord;
            if (btrId == ObjectId.Null) return;

            BlockTableRecord btr = tr.GetObject(btrId, OpenMode.ForRead, false, true) as BlockTableRecord;
            foreach (ObjectId entId in btr)
            {
                DBObject obj = tr.GetObject(entId, OpenMode.ForRead, false, true);
                if (obj == null) continue;

                if (obj is ProxyEntity proxyEntity)
                {
                    ProcessProxyEntity(proxyEntity, tr, ms, db, forceLayer, resultList, br);
                }
            }
        }

        private static void ProcessProxyEntity(ProxyEntity proxyEntity, Transaction tr, BlockTableRecord ms, Database db, bool forceLayer, List<ObjectId> resultList, BlockReference reference = null)
        {
            if (proxyEntity.GraphicsMetafileType != GraphicsMetafileType.FullGraphics) return;

            using (DBObjectCollection collection = new DBObjectCollection())
            {
                proxyEntity.Explode(collection);
                foreach (DBObject subObj in collection)
                {
                    if (subObj is Entity entity)
                    {
                        // Если из прокси вывалился вложенный прокси — уходим в рекурсию.
                        // НЕ трансформируем его здесь, чтобы избежать паразитного сдвига координат!
                        if (entity is ProxyEntity proxy)
                        {
                            ProcessProxyEntity(proxy, tr, ms, db, forceLayer, resultList, reference);
                            proxy?.Dispose();
                            continue;
                        }

                        // Трансформируем ТОЛЬКО конечные графические примитивы (линии, тексты)
                        if (reference != null)
                            entity.TransformBy(reference.BlockTransform);

                        // Фикс для улетающих однострочных текстов
                        if (entity is DBText dbText && dbText.Justify != AttachmentPoint.BaseLeft)
                        {
                            dbText.AdjustAlignment(db);
                        }

                        // Наследуем свойства ByBlock и слои для конечных примитивов
                        if (reference != null)
                            ResolveEntityProperties(entity, reference, forceLayer);

                        resultList.Add(ms.AppendEntity(entity));
                        tr.AddNewlyCreatedDBObject(entity, true);
                    }
                    else
                    {
                        subObj?.Dispose();
                    }
                }
            }
        }

        /// <summary>
        /// Проверяет размер: если это аннотационная зависимость — клонирует её в чистый размер.
        /// Параллельно применяет масштабирование (Dimscale/Dimlfac) к ЛЮБОМУ типу размера за один проход.
        /// </summary>
        private static ObjectId ConvertAndScaleDimension(Transaction tr, BlockTableRecord ms, ObjectId dimId, double insertScale)
        {
            // Если масштаб равен 1.0, нам не нужно модифицировать обычные размеры, 
            // но зависимости всё равно нужно проверить и клонировать. Поэтому открываем ForRead.
            bool needScale = (insertScale != 1.0 && insertScale != 0.0);

            using (Dimension srcDim = tr.GetObject(dimId, OpenMode.ForRead, false, true) as Dimension)
            {
                if (srcDim == null) return ObjectId.Null;

                // 1. Точная проверка на аннотационную зависимость (Constraints не имеют AcadObject)
                bool isConstraint = false;
                try
                {
                    object obj = srcDim.AcadObject;
                    if (obj == null) isConstraint = true;
                }
                catch (System.Runtime.InteropServices.COMException)
                {
                    isConstraint = true;
                }

                // --- СЦЕНАРИЙ А: Это ОБЫЧНЫЙ РАЗМЕР чертежа ---
                if (!isConstraint)
                {
                    if (needScale)
                    {
                        // Переоткрываем на запись только если реально нужно применить масштаб
                        srcDim.UpgradeOpen();
                        try
                        {
                            srcDim.Dimscale *= insertScale; // Масштаб стрелок/текста
                            srcDim.Dimlfac /= insertScale;  // Компенсация линейных измерений
                        }
                        catch { }
                    }
                    return dimId; // Возвращаем исходный ID
                }

                // --- СЦЕНАРИЙ Б: Это АННОТАЦИОННАЯ ЗАВИСИМОСТЬ (требуется клон) ---
                Dimension newDim = srcDim.Clone() as Dimension;
                if (newDim != null)
                {
                    newDim.DimensionText = "";

                    // Масштабируем клон прямо в памяти (ОЗУ) до добавления в базу данных
                    if (needScale)
                    {
                        try
                        {
                            newDim.Dimscale *= insertScale;
                            newDim.Dimlfac /= insertScale;
                        }
                        catch { }
                    }

                    // Добавляем чистый и уже смасштабированный клон на чертеж
                    ObjectId newId = ms.AppendEntity(newDim);
                    tr.AddNewlyCreatedDBObject(newDim, true);

                    // Стираем старый объект-зависимость
                    srcDim.UpgradeOpen();
                    srcDim.Erase();

                    return newId; // Возвращаем ID нового созданного размера
                }

                return dimId;
            }
        }



        /// <summary>
        /// Наследует свойства от родительского блока для элементов со слоя "0" или со свойствами ByBlock
        /// </summary>
        private static void ResolveEntityProperties(Entity target, BlockReference parentBlock, bool forceLayer)
        {
            // 1. Обработка слоя "0". Если элемент на слое "0" или включен forceLayer, переносим на слой блока
            if (target.Layer == "0" || forceLayer)
            {
                target.Layer = parentBlock.Layer;
            }

            // 2. Обработка цвета ByBlock
            if (target.Color.IsByBlock)
            {
                target.Color = parentBlock.Color;
            }

            // 3. Обработка типа линий ByBlock
            if (string.Equals(target.Linetype, "ByBlock", StringComparison.OrdinalIgnoreCase))
            {
                target.Linetype = parentBlock.Linetype;
            }

            // 4. Обработка веса линий ByBlock
            if (target.LineWeight == LineWeight.ByBlock)
            {
                target.LineWeight = parentBlock.LineWeight;
            }
        }


        #region old

        /// <summary>
        /// расчленияет блок и возвращает ObjectId полученных элементов
        /// </summary>
        //public static List<ObjectId> ExplodeBlock22(Transaction tr, Database db, ObjectId id, bool erase, bool inLayer, bool recursive, bool explodeProxy, Matrix3d matrix, BlockReference br = null)
        //{

        //    List<ObjectId> result = new List<ObjectId>();
        //    List<ObjectId> attrList = new List<ObjectId>();
        //    List<ObjectId> dimList = new List<ObjectId>();
        //    List<ObjectId> toExplode = new List<ObjectId>();
        //    // Открываем вставку блока – для расчленения достаточно возможности
        //    // открыть «для чтения»т.к. эта операция не меняет исходный примитив          
        //    if (br == null) br = tr.GetObject(id, OpenMode.ForRead, false, true) as BlockReference;
        //    if (br == null) { return result; }

        //    matrix = br.BlockTransform;

        //    Scale3d scale3D = br.ScaleFactors;
        //    double scale = 1;
        //    if (Math.Abs(scale3D.X).IsEqualTo(Math.Abs(scale3D.Y))) scale = Math.Abs(scale3D.X);
        //    // Отдельно обрабатываем атрибуты блока
        //    if (br.AttributeCollection.Count > 0)
        //    {
        //        using (BlockTableRecord ms = tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite, false, true) as BlockTableRecord)
        //        {
        //            foreach (ObjectId attRefId in br.AttributeCollection)
        //            {
        //                using (AttributeReference attr = tr.GetObject(attRefId, OpenMode.ForRead, false, true) as AttributeReference)
        //                {
        //                    if (attr == null || (!attr.Visible && attr.Invisible)) continue;
        //                    if (attr.IsMTextAttribute)
        //                    {
        //                        using (MText nText = attr.MTextAttribute)
        //                        {
        //                            result.Add(ms.AppendEntity(nText));
        //                            tr.AddNewlyCreatedDBObject(nText, true);
        //                        }
        //                    }
        //                    else
        //                    {
        //                        using (DBText nText = new DBText())
        //                        {
        //                            nText.SetPropertiesFrom(attr);
        //                            nText.Height = attr.Height;
        //                            nText.Color = attr.Color;
        //                            nText.Layer = attr.Layer;
        //                            nText.TextStyleId = attr.TextStyleId;
        //                            nText.Linetype = attr.Linetype;
        //                            nText.LineWeight = attr.LineWeight;
        //                            nText.Position = attr.Position;
        //                            nText.TextString = attr.TextString;
        //                            nText.Justify = attr.Justify;
        //                            nText.WidthFactor = attr.WidthFactor;
        //                            nText.Rotation = attr.Rotation;
        //                            if (attr.Justify != AttachmentPoint.BaseLeft) nText.AlignmentPoint = attr.AlignmentPoint;
        //                            result.Add(ms.AppendEntity(nText));
        //                            tr.AddNewlyCreatedDBObject(nText, true);
        //                        }
        //                    }
        //                }
        //            }
        //        }
        //    }
        //    if (explodeProxy)
        //    {


        //        ObjectId btrId = ObjectId.Null;

        //        if (br.IsDynamicBlock && br.DynamicBlockTableRecord != ObjectId.Null) btrId = br.DynamicBlockTableRecord;
        //        else btrId = br.BlockTableRecord;

        //        if (btrId != ObjectId.Null)
        //        {
        //            using (BlockTableRecord ms = tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite, false, true) as BlockTableRecord)
        //            {
        //                BlockTableRecord btr = tr.GetObject(btrId, OpenMode.ForRead, false, true) as BlockTableRecord;
        //                foreach (ObjectId prId in btr)
        //                {
        //                    ProxyEntity proxyEntity = tr.GetObject(prId, OpenMode.ForRead, false, true) as ProxyEntity;
        //                    if (proxyEntity != null && proxyEntity.GraphicsMetafileType == GraphicsMetafileType.FullGraphics)
        //                    {
        //                        using (DBObjectCollection collection = new DBObjectCollection())
        //                        {
        //                            proxyEntity.Explode(collection);
        //                            foreach (DBObject obj1 in collection)
        //                            {
        //                                if (obj1 is Entity entity)
        //                                {
        //                                    entity.TransformBy(matrix);
        //                                    ms.AppendEntity(entity);
        //                                    tr.AddNewlyCreatedDBObject(entity, true);
        //                                }
        //                                else obj1?.Dispose();
        //                            }
        //                        }
        //                    }

        //                }
        //            }
        //        }
        //    }
        //    // Создаем обработчик для получения вложенных вставок блока
        //    void handler(object s, ObjectEventArgs e)
        //    {
        //        if (e.DBObject is BlockReference && recursive) toExplode.Add(e.DBObject.ObjectId);
        //        else if (e.DBObject is AttributeDefinition) attrList.Add(e.DBObject.ObjectId);
        //        else if (e.DBObject is Dimension) dimList.Add(e.DBObject.ObjectId);
        //        else result.Add(e.DBObject.ObjectId);
        //    }
        //    // Добавляем обработчик перед вызовом расчленения
        //    //  удаляем сразу после этого
        //    db.ObjectAppended += handler;
        //    br.ExplodeToOwnerSpace();
        //    db.ObjectAppended -= handler;
        //    // Проходимся по всем полученным вставкам блока и рекурсивно
        //    // расчленяем их если надо
        //    foreach (ObjectId bid in toExplode)
        //    {
        //        result.AddRange(ExplodeBlock(tr, db, bid, erase, inLayer, recursive, explodeProxy, matrix));
        //    }
        //    //удаляем атрибуты, они уже преобразованы в тексты
        //    foreach (ObjectId objectId in attrList)
        //    {
        //        using (Entity e = tr.GetObject(objectId, OpenMode.ForWrite, false, true) as Entity)
        //        {
        //            if (e != null && !e.IsErased) e.Erase();
        //        }
        //    }
        //    //изменяем масштаб размеров
        //    if (scale != 1)
        //    {
        //        foreach (ObjectId objectId in dimList)
        //        {
        //            using (Dimension dstr = tr.GetObject(objectId, OpenMode.ForWrite, false, true) as Dimension)
        //            {
        //                if (dstr != null)
        //                {
        //                    try
        //                    {
        //                        dstr.Dimscale *= scale;
        //                    }
        //                    catch { }
        //                    result.Add(objectId);
        //                }
        //            }
        //        }
        //    }
        //    //меняем слой если надо
        //    if (inLayer)
        //    {
        //        foreach (ObjectId objectId in result)
        //        {
        //            using (Entity e = tr.GetObject(objectId, OpenMode.ForWrite, false, true) as Entity)
        //            {
        //                if (e != null) e.Layer = br.Layer;
        //            }
        //        }
        //    }
        //    // Чтобы повторить поведение команды РАСЧЛЕНИ
        //    // необходимо удалить исходный примитив
        //    if (erase && !br.IsErased)
        //    {
        //        br.UpgradeOpen();
        //        br.Erase();
        //        br.DowngradeOpen();
        //    }
        //    return result;
        //}

        #endregion
    }
}
