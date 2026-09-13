using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;

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
                else
                {
                    using (DBObjectCollection coll = new DBObjectCollection())
                    {
                        e.Explode(coll);
                        foreach (DBObject obj in coll)
                        {
                            using (Entity newE = obj as Entity)
                            {
                                if (newE != null)
                                {
                                    result.Add(ms.AppendEntity(newE));
                                    tr.AddNewlyCreatedDBObject(newE, true);
                                }
                            }
                            obj.Dispose();
                        }
                        if (erase)
                        {
                            if (e.IsReadEnabled) e.UpgradeOpen();
                            e.Erase();
                        }
                    }
                }
            }
            catch { }
            return result;
        }

        /// <summary>
        /// Центральный диспетчер расчленения вставки блока. 
        /// Собирает все примитивы в памяти и добавляет их на чертеж одной пачкой через фреймворк.
        /// </summary>
        public static List<ObjectId> ExplodeBlock(Transaction tr, Database db, ObjectId id, bool erase, bool inLayer, bool recursive, bool explodeProxy, Matrix3d currentTransform, BlockReference br = null)
        {
            List<ObjectId> result = new List<ObjectId>();

            if (br == null)
            {
                br = tr.GetObject(id, OpenMode.ForRead, false, true) as BlockReference;
            }
            if (br == null) return result;

            // Накопление матриц трансформации для вложенных структур
            Matrix3d localMatrix = currentTransform * br.BlockTransform;
            double scale = Math.Abs(br.ScaleFactors.X).IsEqualTo(Math.Abs(br.ScaleFactors.Y)) ? Math.Abs(br.ScaleFactors.X) : 1.0;

            // Локальный буфер для сбора всех созданных в ОЗУ примитивов
            var entitiesToAppend = new List<Entity>();

            // Наполняем буфер примитивами из атрибутов, геометрии и прокси
            ProcessBlockAttributes(tr, br, inLayer, entitiesToAppend);
            ProcessBlockGeometry(tr, db, br, localMatrix, scale, inLayer, recursive, explodeProxy, entitiesToAppend);

            if (explodeProxy)
            {
                ProcessBlockProxyEntities(tr, br, localMatrix, inLayer, entitiesToAppend);
            }

            // Добавляем всю пачку объектов на чертеж за один раз через метод фреймворка
            if (entitiesToAppend.Count > 0)
            {
                if (entitiesToAppend.AddEntityInCurrentBTR(out List<ObjectId> appendedIds, db.CurrentSpaceId, tr))
                {
                    result.AddRange(appendedIds);
                }
            }

            // Удаление исходной вставки блока (только на самом верхнем уровне)
            if (erase && id != ObjectId.Null && !br.IsErased)
            {
                br.UpgradeOpen();
                br.Erase();
                br.DowngradeOpen();
            }

            return result;
        }

        /// <summary>
        /// Вспомогательный метод для правильного наследования слоев и цветов по стандартам AutoCAD (Слой "0" и "ByBlock")
        /// </summary>
        private static void ApplyEntityProperties(Entity target, Entity source, BlockReference parentBlock, bool forceLayer)
        {
            target.Layer = (source.Layer == "0" || forceLayer) ? parentBlock.Layer : source.Layer;

            if (source.Color.IsByBlock)
            {
                target.Color = parentBlock.Color;
            }
        }

        /// <summary>
        /// Извлекает атрибуты из вставки блока и подготавливает их текстовые копии в памяти.
        /// </summary>
        private static void ProcessBlockAttributes(Transaction tr, BlockReference br, bool inLayer, List<Entity> outputList)
        {
            if (br.AttributeCollection.Count == 0) return;

            foreach (ObjectId attRefId in br.AttributeCollection)
            {
                using (var attr = tr.GetObject(attRefId, OpenMode.ForRead, false, true) as AttributeReference)
                {
                    if (attr == null || (!attr.Visible && attr.Invisible)) continue;

                    if (attr.IsMTextAttribute)
                    {
                        using (MText sourceMText = attr.MTextAttribute)
                        {
                            MText newMText = sourceMText.Clone() as MText;
                            if (newMText != null)
                            {
                                ApplyEntityProperties(newMText, attr, br, inLayer);
                                outputList.Add(newMText);
                            }
                        }
                    }
                    else
                    {
                        var newText = new DBText();
                        newText.SetPropertiesFrom(attr);
                        newText.Height = attr.Height;
                        newText.TextStyleId = attr.TextStyleId;
                        newText.Position = attr.Position;
                        newText.TextString = attr.TextString;
                        newText.Justify = attr.Justify;
                        newText.WidthFactor = attr.WidthFactor;
                        newText.Rotation = attr.Rotation;

                        if (attr.Justify != AttachmentPoint.BaseLeft)
                        {
                            newText.AlignmentPoint = attr.AlignmentPoint;
                        }

                        ApplyEntityProperties(newText, attr, br, inLayer);
                        outputList.Add(newText);
                    }
                }
            }
        }

        /// <summary>
        /// Локально взрывает тело блока и накапливает готовую геометрию (включая рекурсивные вложения) в ОЗУ.
        /// </summary>
        private static void ProcessBlockGeometry(Transaction tr, Database db, BlockReference br, Matrix3d localMatrix,
            double scale, bool inLayer, bool recursive, bool explodeProxy, List<Entity> outputList)
        {
            using (var explodedCollection = new DBObjectCollection())
            {
                br.Explode(explodedCollection);

                var innersToExplode = new List<BlockReference>();

                foreach (DBObject obj in explodedCollection)
                {
                    if (obj is Entity ent)
                    {
                        if (!ent.Visible)
                        {
                            ent.Dispose();
                            continue;
                        }

                        if (recursive && ent is BlockReference innerRef)
                        {
                            innerRef.TransformBy(localMatrix);
                            innersToExplode.Add(innerRef);
                            continue;
                        }

                        if (ent is AttributeDefinition)
                        {
                            ent.Dispose();
                            continue;
                        }

                        if (scale != 1.0 && ent is Dimension dim)
                        {
                            try { dim.Dimscale *= scale; } catch { }
                        }

                        ent.TransformBy(localMatrix);
                        ApplyEntityProperties(ent, ent, br, inLayer);
                        outputList.Add(ent);
                    }
                    else
                    {
                        obj?.Dispose();
                    }
                }

                // Рекурсивный спуск: наполняем этот же общий список на всю глубину иерархии
                foreach (BlockReference innerRef in innersToExplode)
                {
                    var innerEntities = new List<Entity>();

                    ProcessBlockAttributes(tr, innerRef, inLayer, innerEntities);
                    ProcessBlockGeometry(tr, db, innerRef, Matrix3d.Identity, 1.0, inLayer, recursive, explodeProxy, innerEntities);

                    if (explodeProxy)
                    {
                        ProcessBlockProxyEntities(tr, innerRef, Matrix3d.Identity, inLayer, innerEntities);
                    }

                    outputList.AddRange(innerEntities);
                    innerRef.Dispose();
                }
            }
        }
        /// <summary>
        /// Безопасно извлекает графику из Proxy-объектов внутри блока и сохраняет её в ОЗУ-список.
        /// </summary>
        private static void ProcessBlockProxyEntities(Transaction tr, BlockReference br, Matrix3d localMatrix, bool inLayer, List<Entity> outputList)
        {
            ObjectId btrId = br.IsDynamicBlock && br.DynamicBlockTableRecord != ObjectId.Null
                ? br.DynamicBlockTableRecord
                : br.BlockTableRecord;

            if (btrId == ObjectId.Null) return;

            using (var btr = tr.GetObject(btrId, OpenMode.ForRead, false, true) as BlockTableRecord)
            {
                foreach (ObjectId prId in btr)
                {
                    using (var obj = tr.GetObject(prId, OpenMode.ForRead, false, true))
                    {
                        if (obj is ProxyEntity proxy && proxy.GraphicsMetafileType == GraphicsMetafileType.FullGraphics)
                        {
                            using (var proxyCollection = new DBObjectCollection())
                            {
                                proxy.Explode(proxyCollection);
                                foreach (DBObject pObj in proxyCollection)
                                {
                                    if (pObj is Entity pEnt)
                                    {
                                        pEnt.TransformBy(localMatrix);
                                        ApplyEntityProperties(pEnt, pEnt, br, inLayer);
                                        outputList.Add(pEnt);
                                    }
                                    else pObj?.Dispose();
                                }
                            }
                        }
                    }
                }
            }
        }



        /// <summary>
        /// расчленияет блок и возвращает ObjectId полученных элементов
        /// </summary>
        public static List<ObjectId> ExplodeBlockOld(Transaction tr, Database db, ObjectId id, bool erase, bool inLayer, bool recursive, bool explodeProxy, Matrix3d matrix, BlockReference br = null)
        {

            List<ObjectId> result = new List<ObjectId>();
            List<ObjectId> attrList = new List<ObjectId>();
            List<ObjectId> dimList = new List<ObjectId>();
            List<ObjectId> toExplode = new List<ObjectId>();
            // Открываем вставку блока – для расчленения достаточно возможности
            // открыть «для чтения»т.к. эта операция не меняет исходный примитив          
            if (br == null) br = tr.GetObject(id, OpenMode.ForRead, false, true) as BlockReference;
            if (br == null) { return result; }

            matrix = br.BlockTransform;

            Scale3d scale3D = br.ScaleFactors;
            double scale = 1;
            if (Math.Abs(scale3D.X).IsEqualTo(Math.Abs(scale3D.Y))) scale = Math.Abs(scale3D.X);
            // Отдельно обрабатываем атрибуты блока
            if (br.AttributeCollection.Count > 0)
            {
                using (BlockTableRecord ms = tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite, false, true) as BlockTableRecord)
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
                                    result.Add(ms.AppendEntity(nText));
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
                                    if (attr.Justify != AttachmentPoint.BaseLeft) nText.AlignmentPoint = attr.AlignmentPoint;
                                    result.Add(ms.AppendEntity(nText));
                                    tr.AddNewlyCreatedDBObject(nText, true);
                                }
                            }
                        }
                    }
                }
            }
            if (explodeProxy)
            {


                ObjectId btrId = ObjectId.Null;

                if (br.IsDynamicBlock && br.DynamicBlockTableRecord != ObjectId.Null) btrId = br.DynamicBlockTableRecord;
                else btrId = br.BlockTableRecord;

                if (btrId != ObjectId.Null)
                {
                    using (BlockTableRecord ms = tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite, false, true) as BlockTableRecord)
                    {
                        BlockTableRecord btr = tr.GetObject(btrId, OpenMode.ForRead, false, true) as BlockTableRecord;
                        foreach (ObjectId prId in btr)
                        {
                            ProxyEntity proxyEntity = tr.GetObject(prId, OpenMode.ForRead, false, true) as ProxyEntity;
                            if (proxyEntity != null && proxyEntity.GraphicsMetafileType == GraphicsMetafileType.FullGraphics)
                            {
                                using (DBObjectCollection collection = new DBObjectCollection())
                                {
                                    proxyEntity.Explode(collection);
                                    foreach (DBObject obj1 in collection)
                                    {
                                        if (obj1 is Entity entity)
                                        {
                                            entity.TransformBy(matrix);
                                            ms.AppendEntity(entity);
                                            tr.AddNewlyCreatedDBObject(entity, true);
                                        }
                                        else obj1?.Dispose();
                                    }
                                }
                            }

                        }
                    }
                }
            }
            // Создаем обработчик для получения вложенных вставок блока
            void handler(object s, ObjectEventArgs e)
            {
                if (e.DBObject is BlockReference && recursive) toExplode.Add(e.DBObject.ObjectId);
                else if (e.DBObject is AttributeDefinition) attrList.Add(e.DBObject.ObjectId);
                else if (e.DBObject is Dimension) dimList.Add(e.DBObject.ObjectId);
                else result.Add(e.DBObject.ObjectId);
            }
            // Добавляем обработчик перед вызовом расчленения
            //  удаляем сразу после этого
            db.ObjectAppended += handler;
            br.ExplodeToOwnerSpace();
            db.ObjectAppended -= handler;
            // Проходимся по всем полученным вставкам блока и рекурсивно
            // расчленяем их если надо
            foreach (ObjectId bid in toExplode)
            {
                result.AddRange(ExplodeBlock(tr, db, bid, erase, inLayer, recursive, explodeProxy, matrix));
            }
            //удаляем атрибуты, они уже преобразованы в тексты
            foreach (ObjectId objectId in attrList)
            {
                using (Entity e = tr.GetObject(objectId, OpenMode.ForWrite, false, true) as Entity)
                {
                    if (e != null && !e.IsErased) e.Erase();
                }
            }
            //изменяем масштаб размеров
            if (scale != 1)
            {
                foreach (ObjectId objectId in dimList)
                {
                    using (Dimension dstr = tr.GetObject(objectId, OpenMode.ForWrite, false, true) as Dimension)
                    {
                        if (dstr != null)
                        {
                            try
                            {
                                dstr.Dimscale *= scale;
                            }
                            catch { }
                            result.Add(objectId);
                        }
                    }
                }
            }
            //меняем слой если надо
            if (inLayer)
            {
                foreach (ObjectId objectId in result)
                {
                    using (Entity e = tr.GetObject(objectId, OpenMode.ForWrite, false, true) as Entity)
                    {
                        if (e != null) e.Layer = br.Layer;
                    }
                }
            }
            // Чтобы повторить поведение команды РАСЧЛЕНИ
            // необходимо удалить исходный примитив
            if (erase && !br.IsErased)
            {
                br.UpgradeOpen();
                br.Erase();
                br.DowngradeOpen();
            }
            return result;
        }


    }
}
