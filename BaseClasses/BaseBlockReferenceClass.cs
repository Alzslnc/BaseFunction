using Autodesk.AutoCAD.ApplicationServices;
using Aap = Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BaseFunction
{
    public static class BaseBlockReferenceClass
    {
        #region получаем название блока
        public static string GetName(this BlockReference reference)
        {
            if (reference.IsDynamicBlock)
            {
                using (BlockTableRecord btr = reference.DynamicBlockTableRecord.Open(OpenMode.ForRead) as BlockTableRecord)
                { 
                    return btr.Name;
                }
            }
            else return reference.Name;
        }
        #endregion

        #region перенос блоков из других чертежей
        /// <summary>
        /// переносит отсутствующие в чертеже блоки из выбранного файла в используемый файл
        /// </summary>
        /// <param name="blNames">список с названиями нужных блоков</param>
        /// <param name="fileName">файл из которого переносятся блоки</param>
        /// <param name="folders">список местоположений где может находиться файл</param>
        /// <returns>true если блоки перенесены успешно</returns>
        public static bool BlockMigrate(List<string> blNames, string fileName, List<string> folders, bool replace = false)
        {

            if (blNames.Count == 0 || (GetMissBlocks(HostApplicationServices.WorkingDatabase, blNames).Count == 0 && !replace)) return true;       
            foreach (string folder in folders)
            {
                if (System.IO.Directory.Exists(folder))
                {
                    string[] allFoundFiles = Directory.GetFiles(folder, fileName, SearchOption.AllDirectories);
                    if (allFoundFiles.Count() > 0) return BlockMigrate(blNames, allFoundFiles[0], replace);
                }
            }
            System.Windows.Forms.MessageBox.Show("Файл ресурсов не найден");
            return false;
        }
        /// <summary>
        /// переносит отсутствующие в чертеже блоки из выбранного файла
        /// </summary>
        /// <param name="blNames">список с названиями нужных блоков</param>
        /// <param name="fullFileName">файл из которого переносятся блоки (полное название с путем)</param>
        /// <returns>true если блоки перенесены успешно</returns>
        public static bool BlockMigrate(List<string> blNames, string fullFileName, bool replace = false)
        {
            if (blNames.Count == 0 || (GetMissBlocks(HostApplicationServices.WorkingDatabase, blNames).Count == 0 && !replace)) return true;
            if (string.IsNullOrEmpty(fullFileName)) return false;
            return BlockMigrate(HostApplicationServices.WorkingDatabase, blNames, fullFileName, replace);
        }
        public static bool BlockMigrate(Database targetDb, List<string> blNames, string fullFileName, bool replace = false)
        {
            if (blNames.Count == 0) return true;
            if (string.IsNullOrEmpty(fullFileName)) return false;
            using (Database db = new Database(false, true))
            {
                try
                {
                    db.ReadDwgFile(fullFileName, FileShare.Read, true, String.Empty);
                    return (BlockMigrate(targetDb, blNames, db, replace));
                }
                catch
                {
                    System.Windows.Forms.MessageBox.Show("Не удалось считать файл ресурсов");
                    return false;
                }
            }
        }
        public static bool BlockMigrate(Database targetDb, List<string> blockNames, Database storageDb, bool replace = false)
        {
            if (blockNames.Count == 0) return true;
            if (targetDb == null || storageDb == null) return false;
            //если в списке отсутствующих блоков ничего нет то блоки в чертеже присутствуют           
            using (ObjectIdCollection missBlocksId = new ObjectIdCollection())
            {
                using (Transaction tr = storageDb.TransactionManager.StartTransaction())
                {
                    //получаем таблицу блоков
                    BlockTable bt = tr.GetObject(storageDb.BlockTableId, OpenMode.ForRead, false, true) as BlockTable;
                    //проходим по списку нужных блоков, если находим записываем его Id, если нет то возвращаем false                                
                    foreach (string blockName in blockNames)
                    {
                        if (bt.Has(blockName))
                        {
                            missBlocksId.Add(bt[blockName]);
                        }
                        else
                        {
                            tr.Commit();
                            System.Windows.Forms.MessageBox.Show("В файле ресурсов блоки не найдены");
                            return false;
                        }
                    }                   
                }
                //записываем блоки
                using (IdMapping idMapping = new IdMapping())
                {
                    try
                    {
                        DuplicateRecordCloning duplicateRecordCloning;
                        if (replace) duplicateRecordCloning = DuplicateRecordCloning.Replace;
                        else duplicateRecordCloning= DuplicateRecordCloning.Ignore;
                        targetDb.WblockCloneObjects(missBlocksId, targetDb.BlockTableId, idMapping, duplicateRecordCloning, false);                       
                        if (GetMissBlocks(targetDb, blockNames).Count > 0) return false;
                    }
                    catch
                    {
                        System.Windows.Forms.MessageBox.Show("Не удалось перенести блоки из файла ресурсов");
                        return false;
                    }
                }
            }
            return true;
        }
       
        /// <summary>
        /// проверяет список блоков на наличие в базе данных и возвращает список отсутствующих
        /// </summary>
        /// <param name="db"></param>
        /// <param name="blNames"></param>
        /// <returns></returns>
        public static List<string> GetMissBlocks(Database db, List<string> blNames)
        {
            //объявляем переменную для хранения недостающих блоков
            List<string> missBlocks = new List<string>();
            //проверяем наличие блоков в чертеже
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                //получаем таблицу блоков
                using (BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead, false, true) as BlockTable)
                {
                    //проверяем наличие блоков, если нет записываем в список
                    foreach (string block in blNames)
                    {
                        if (!bt.Has(block)) missBlocks.Add(block);
                    }
                }
                tr.Commit();
            }
            return missBlocks;
        }
        #endregion

        #region изменение атрибутов в блоке
        /// <summary>
        /// изменяет атрибуты блока
        /// </summary>
        /// <param name="brId">ObjectId блока</param>
        /// <param name="attributes">список (таг, значение) изменяемых атрибутов</param>
        public static bool BlockReferenceChangeAttribute(this ObjectId brId, List<(string tag, object value)> attributes)
        {
            if (brId == null || brId == ObjectId.Null) return false;
            Dictionary<string, string> dictionary = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
            foreach ((string tag, object value) in attributes)
            {
                if (!dictionary.ContainsKey(tag)) dictionary.Add(tag, value.ToString());
            }
            return BlockReferenceChangeAttribute(brId, dictionary);
        }
        /// <summary>
        /// изменяет один выбранный атрибут в блоке
        /// </summary>
        /// <param name="brId"></param>
        /// <param name="tag"></param>
        /// <param name="value"></param>
        /// <returns>true если атрибут найден и изменен</returns>
        public static bool BlockReferenceChangeAttribute(this ObjectId brId, string tag, string value)
        {
            if (brId == null || brId == ObjectId.Null) return false;
            Dictionary<string, string> dictionary = new Dictionary<string, string>() { { tag, value } };
            return BlockReferenceChangeAttribute(brId, dictionary);
        }
        /// <summary>
        /// изменяет атрибуты блока
        /// </summary>
        /// <param name="brId"></param>
        /// <param name="attributes">словарь с атрибутами(таг/значение)</param>
        /// <returns>true если все атрибуты из словаря поменяны</returns>
        public static bool BlockReferenceChangeAttribute(this ObjectId brId, Dictionary<string, string> attributes)
        {
            if (brId == null || brId == ObjectId.Null) return false;
            return BlockReferenceChangeAttribute(brId, attributes, false);
        }
        /// <summary>
        /// изменяет атрибуты блока
        /// </summary>
        /// <param name="brId"></param>
        /// <param name="attributes">словарь с атрибутами(таг/значение)</param>
        /// <param name="allReplace">изменяет атрибуты только полным набором, если хоть один атрибут из списка не найден то ничего не будет изменено</param>
        /// <returns>true если все атрибуты из словаря поменяны</returns>
        public static bool BlockReferenceChangeAttribute(this ObjectId brId, Dictionary<string, string> attributes, bool allReplace)
        {
            if (brId == null || brId == ObjectId.Null) return false;

            bool result = false;
            //запускаем транзакцию
            using (Transaction tr = HostApplicationServices.WorkingDatabase.TransactionManager.StartTransaction())
            {
                //открываем блок
                using (BlockReference br = tr.GetObject(brId, OpenMode.ForRead, false, true) as BlockReference)
                {
                    result = BlockReferenceChangeAttribute(br, tr, attributes);
                }
                if (result || !allReplace) tr.Commit();
                else tr.Abort();
            }
            return result;
        }
        public static bool BlockReferenceChangeAttribute(this BlockReference br, Transaction tr, Dictionary<string, string> attributes)
        {

            DocumentLock documentLock = null;
            if (Aap.Application.DocumentManager != null && Aap.Application.DocumentManager.MdiActiveDocument != null)
            {
                documentLock = Aap.Application.DocumentManager.MdiActiveDocument.LockDocument();
            }
            attributes = new Dictionary<string, string>(attributes, StringComparer.InvariantCultureIgnoreCase);
            List<string> usingTag = new List<string>();
            bool result = false;

            foreach (object o in br.AttributeCollection)
            {
                if (o is ObjectId id)
                {
                    using (AttributeReference attRef = tr.GetObject(id, OpenMode.ForWrite, false, true) as AttributeReference)
                    {
                        if (attRef != null && attributes.ContainsKey(attRef.Tag))
                        {                           
                            attRef.TextString = attributes[attRef.Tag];
                            if (!usingTag.Contains(attRef.Tag)) usingTag.Add(attRef.Tag);
                        }
                    }
                }
                else if (o is AttributeReference attRef)
                {
                    if (attRef != null && attributes.ContainsKey(attRef.Tag))
                    {                        
                        attRef.TextString = attributes[attRef.Tag];
                        if (!usingTag.Contains(attRef.Tag)) usingTag.Add(attRef.Tag);
                        attRef.AdjustAlignment(HostApplicationServices.WorkingDatabase);
                    }
                }          
            }
            if (usingTag.Count.Equals(attributes.Count)) result = true;
            documentLock?.Dispose();
            return result;
        }
        #endregion

        #region добавление атрибутов новому блоку
        /// <summary>
        /// добавляет атрибуты блоку
        /// </summary>
        /// <param name="brId">ObjectId блока</param>
        public static void BlockReferenceSetAttribute(ObjectId brId)
        {
            if (brId == null || brId == ObjectId.Null) return;
            using (Transaction tr = HostApplicationServices.WorkingDatabase.TransactionManager.StartTransaction())
            {
                using (BlockReference br = tr.GetObject(brId, OpenMode.ForWrite, false, true) as BlockReference)
                {
                    if (br != null)
                    {
                        BlockReferenceSetAttribute(br, tr);
                    }
                }
                tr.Commit();
            }
        }
        public static void BlockReferenceSetAttribute(BlockReference br, Transaction tr)
        {
            DocumentLock documentLock = null;
            if (Aap.Application.DocumentManager != null && Aap.Application.DocumentManager.MdiActiveDocument != null)
            {
                documentLock = Aap.Application.DocumentManager.MdiActiveDocument.LockDocument();
            }
            //запускаем транзакцию
            //получаем запись о блоке в таблице блоков
            using (BlockTableRecord btr = tr.GetObject(br.BlockTableRecord, OpenMode.ForRead, false, true) as BlockTableRecord)
            {
                //если блок имеет атрибуты
                if (btr.HasAttributeDefinitions)
                {
                    //проходим по всем ID в записи блока
                    foreach (ObjectId id in btr)
                    {
                        //пытаемся открыть объект как запись о атрибуте
                        using (AttributeDefinition attr = tr.GetObject(id, OpenMode.ForRead, false, true) as AttributeDefinition)
                        {
                            //если это атрибут и он не константа
                            if (attr == null || attr.Constant) continue;
                            //создаем вхождение атрибута
                            using (AttributeReference attrRef = new AttributeReference())
                            {
                                //добавляем вхождение атрибута в вхождение блока
                                attrRef.SetAttributeFromBlock(attr, br.BlockTransform);
                                br.AttributeCollection.AppendAttribute(attrRef);
                                tr.AddNewlyCreatedDBObject(attrRef, true);
                                //дублируем после добавления что бы нормально отрабатывали вставки полей 
                                attrRef.SetAttributeFromBlock(attr, br.BlockTransform);
                            }
                        }
                    }
                }
            }

            documentLock?.Dispose();
        }
        #endregion

        #region получение атрибутов из блока
        /// <summary>
        /// Получает словарь со всеми парами таг/значение атрибутов блока
        /// </summary>
        /// <param name="br"></param>
        /// <param name="result"></param>
        /// <param name="tr"></param>
        /// <returns>true если получено хотя бы одно значение</returns>
        public static bool BlockReferenceGetAttribute(this BlockReference br, out Dictionary<string, string> result, Transaction tr, List<string> names = null, bool fullContents = false)
        {
            result = new Dictionary<string, string>();
            if (br == null) return false;
            //проходим по всем объектам в коллекции атрибутов
            foreach (ObjectId id in br.AttributeCollection)
            {
                //открываем объект как атрибут
                using (AttributeReference attRef = tr.GetObject(id, OpenMode.ForRead, false, true) as AttributeReference)
                {
                    if (names != null && !names.Contains(attRef.Tag)) continue;
                    if (attRef != null && !result.ContainsKey(attRef.Tag))
                    {
                        if (attRef.IsMTextAttribute)
                        {
                            if (fullContents) result.Add(attRef.Tag, attRef.MTextAttribute.Contents);
                            else result.Add(attRef.Tag, attRef.MTextAttribute.Text);
                        }
                        else
                        {
                            result.Add(attRef.Tag, attRef.TextString);
                        }
                    }
                   
                }
            }
            if (result.Count > 0) return true; return false;
        }
        /// <summary>
        /// Получает словарь со всеми парами таг/значение атрибутов блока
        /// </summary>
        /// <param name="br"></param>    
        /// <param name="result"></param>
        /// <returns>true если получено хотя бы одно значение</returns>        
        public static bool BlockReferenceGetAttribute(this BlockReference br, out Dictionary<string, string> result, List<string> names = null)
        {
            result = new Dictionary<string, string>();
            if (br == null) return false;
            DocumentLock documentLock = null;
            if (Aap.Application.DocumentManager != null && Aap.Application.DocumentManager.MdiActiveDocument != null)
            {
                documentLock = Aap.Application.DocumentManager.MdiActiveDocument.LockDocument();
            }
            //запускаем транзакцию
            using (Transaction tr = HostApplicationServices.WorkingDatabase.TransactionManager.StartTransaction())
            {
                //проходим по всем объектам в коллекции атрибутов
                foreach (ObjectId id in br.AttributeCollection)
                {
                    //открываем объект как атрибут
                    using (AttributeReference attRef = tr.GetObject(id, OpenMode.ForRead, false, true) as AttributeReference)
                    {
                        if (names != null && !names.Contains(attRef.Tag)) continue;
                        if (attRef != null && !result.ContainsKey(attRef.Tag)) result.Add(attRef.Tag, attRef.TextString);
                    }
                }
                tr.Commit();
            }
            documentLock?.Dispose();
            if (result.Count > 0) return true; return false;
        }
        /// <summary>
        /// Получает словарь со всеми парами таг/значение атрибутов блока
        /// </summary>
        /// <param name="brId"></param>
        /// <param name="tag"></param>
        /// <param name="result"></param>
        /// <returns>true если получено хотя бы одно значение</returns>        
        public static bool BlockReferenceGetAttribute(this ObjectId brId, out Dictionary<string, string> result)
        {
            result = new Dictionary<string, string>();
            if (brId == null || brId == ObjectId.Null) return false;
            DocumentLock documentLock = null;
            if (Aap.Application.DocumentManager != null && Aap.Application.DocumentManager.MdiActiveDocument != null)
            {
                documentLock = Aap.Application.DocumentManager.MdiActiveDocument.LockDocument();
            }
            //запускаем транзакцию
            using (Transaction tr = HostApplicationServices.WorkingDatabase.TransactionManager.StartTransaction())
            {
                //открываем блок
                using (BlockReference br = tr.GetObject(brId, OpenMode.ForRead, false, true) as BlockReference)
                {
                    if (br != null)
                    {
                        //проходим по всем объектам в коллекции атрибутов
                        foreach (ObjectId id in br.AttributeCollection)
                        {
                            //открываем объект как атрибут
                            using (AttributeReference attRef = tr.GetObject(id, OpenMode.ForRead, false, true) as AttributeReference)
                            {
                                if (attRef != null && !result.ContainsKey(attRef.Tag)) result.Add(attRef.Tag, attRef.TextString);
                            }
                        }
                    }
                }
                tr.Commit();
            }
            documentLock?.Dispose();
            if (result.Count > 0) return true; return false;
        }
        /// <summary>
        /// Получает значение атрибута по тагу
        /// </summary>
        /// <param name="brId"></param>
        /// <param name="tag"></param>
        /// <param name="result"></param>
        /// <returns>true если получено хотя бы одно значение</returns>
        public static bool BlockReferenceGetAttribute(this BlockReference br, string tag, out string result)
        {
            result = string.Empty;
            if (br == null) return false;
            DocumentLock documentLock = null;
            if (Aap.Application.DocumentManager != null && Aap.Application.DocumentManager.MdiActiveDocument != null)
            {
                documentLock = Aap.Application.DocumentManager.MdiActiveDocument.LockDocument();
            }
            //запускаем транзакцию
            using (Transaction tr = HostApplicationServices.WorkingDatabase.TransactionManager.StartTransaction())
            {
                //проходим по всем объектам в коллекции атрибутов
                foreach (ObjectId id in br.AttributeCollection)
                {
                    //открываем объект как атрибут
                    using (AttributeReference attRef = tr.GetObject(id, OpenMode.ForRead, false, true) as AttributeReference)
                    {
                        if (attRef != null && attRef.Tag.Equals(tag))
                        {
                            result = attRef.TextString;
                            break;
                        }
                    }
                }
                tr.Commit();
            }
            documentLock?.Dispose();
            if (!string.IsNullOrEmpty(result)) return true; return false;
        }
        /// <summary>
        /// Получает значение атрибута по тагу
        /// </summary>
        /// <param name="brId"></param>
        /// <param name="tag"></param>
        /// <param name="result"></param>
        /// <returns>true если получено хотя бы одно значение</returns>
        public static bool BlockReferenceGetAttribute(this ObjectId brId, string tag, out string result)
        {
            result = string.Empty;
            if (brId == null || brId == ObjectId.Null) return false;
            DocumentLock documentLock = null;
            if (Aap.Application.DocumentManager != null && Aap.Application.DocumentManager.MdiActiveDocument != null)
            {
                documentLock = Aap.Application.DocumentManager.MdiActiveDocument.LockDocument();
            }
            //запускаем транзакцию
            using (Transaction tr = HostApplicationServices.WorkingDatabase.TransactionManager.StartTransaction())
            {
                //открываем блок
                using (BlockReference br = tr.GetObject(brId, OpenMode.ForRead, false, true) as BlockReference)
                {
                    if (br != null)
                    {
                        //проходим по всем объектам в коллекции атрибутов
                        foreach (ObjectId id in br.AttributeCollection)
                        {
                            //открываем объект как атрибут
                            using (AttributeReference attRef = tr.GetObject(id, OpenMode.ForRead, false, true) as AttributeReference)
                            {
                                if (attRef != null && attRef.Tag.Equals(tag)) result = attRef.TextString;
                                break;
                            }
                        }
                    }
                }
                tr.Commit();
            }
            documentLock?.Dispose();
            if (!string.IsNullOrEmpty(result)) return true; return false;
        }
        #endregion

        #region замещение одного блока другим
        public static void BlockReplace(this BlockReference oldReference, ObjectId newBtr)
        {
            using (Transaction tr = HostApplicationServices.WorkingDatabase.TransactionManager.StartTransaction())
            {
                oldReference.BlockReplace(newBtr);
                tr.Commit();
            }
        }
        public static void BlockReplace(this BlockReference oldReference, ObjectId newBtr, Transaction tr)
        {
            using (BlockTableRecord ms = tr.GetObject(oldReference.OwnerId, OpenMode.ForWrite) as BlockTableRecord)
            {
                using (BlockReference newReference = new BlockReference(oldReference.Position, newBtr))
                {
                    newReference.ScaleFactors = oldReference.ScaleFactors;
                    newReference.Normal = oldReference.Normal;
                    newReference.Rotation = oldReference.Rotation;
                    //newReference.TransformBy(Matrix3d.Rotation(oldReference.Rotation, newReference.Normal, newReference.Position));               
                    newReference.XData = oldReference.XData;

                    newReference.Color = oldReference.Color;
                    newReference.Linetype = oldReference.Linetype;
                    newReference.LineWeight = oldReference.LineWeight;
                    newReference.Layer = oldReference.Layer;
                    newReference.LinetypeScale = oldReference.LinetypeScale;
                    newReference.Transparency = oldReference.Transparency;

                    ObjectId newReferenceId = ms.AppendEntity(newReference);
                    tr.AddNewlyCreatedDBObject(newReference, true);

                    BlockReferenceSetAttribute(newReference, tr);

                    if (BlockReferenceGetAttribute(oldReference, out Dictionary<string, string> attributes, tr))
                    {
                        BlockReferenceChangeAttribute(newReference, tr, attributes);
                    }

                    if (newReference.IsDynamicBlock)
                    {                
                        newReference.SetBlockreferenceProperties(oldReference.GetBlockReferenceProperties());
                    }                    
                }
            }
        }
        #endregion

        #region получение и установка параметров блока
        public static Dictionary<string, dynamic> GetBlockReferenceProperties(this BlockReference reference)
        { 
            Dictionary<string, dynamic> result = new Dictionary<string, dynamic>();
            DynamicBlockReferencePropertyCollection collection = reference.DynamicBlockReferencePropertyCollection;
            foreach (DynamicBlockReferenceProperty property in collection)
            {
                if (result.ContainsKey(property.PropertyName))
                { 
                    int i = 0;
                    while (result.ContainsKey(property.PropertyName + "..." + ++i)) continue;
                    result.Add(property.PropertyName + "..." + i, property.Value);
                }
                else result.Add(property.PropertyName, property.Value);              
            }
            return result;
        }
        public static Dictionary<string, (dynamic, dynamic)> GetBlockReferencePropertiesAndUnits(this BlockReference reference)
        {
            Dictionary<string, (dynamic, dynamic)> result = new Dictionary<string, (dynamic, dynamic)>();
            DynamicBlockReferencePropertyCollection collection = reference.DynamicBlockReferencePropertyCollection;
                       
            foreach (DynamicBlockReferenceProperty property in collection)
            {
                if (result.ContainsKey(property.PropertyName))
                {
                    int i = 0;
                    while (result.ContainsKey(property.PropertyName + "..." + ++i)) continue;
                    result.Add(property.PropertyName + "..." + i, (property.Value, property.UnitsType));
                }
                else result.Add(property.PropertyName, (property.Value, property.UnitsType));         
            }
            return result;
        }
        public static void SetBlockreferenceProperties(this BlockReference reference, Dictionary<string, object> properties)
        {          
            DynamicBlockReferencePropertyCollection collection = reference.DynamicBlockReferencePropertyCollection;
            foreach (DynamicBlockReferenceProperty property in collection)
            {
                try
                {
                    if (properties.ContainsKey(property.PropertyName) && !property.PropertyName.Contains("Origin")) property.Value = properties[property.PropertyName];

                } catch { }
            }
        }
        #endregion
    }

    public sealed class WorkingDatabaseSwitcher : IDisposable
    {
        private readonly Database PrevDb = null;
        private readonly Database NextDb = null;
        private readonly bool Save = false;
        /// <summary>
        /// База данных, в контексте которой должна производиться работа. Эта база данных на время становится текущей.
        /// По завершению работы текущей станет та база, которая была ею до этого.
        /// </summary>
        /// <param name="db">База данных, которая должна быть установлена текущей</param>
        public WorkingDatabaseSwitcher(Database db, bool save = false)
        {
            PrevDb = HostApplicationServices.WorkingDatabase;
            HostApplicationServices.WorkingDatabase = db;
            NextDb = db;
            Save = save;
        }

        /// <summary>
        /// Возвращаем свойству <c>HostApplicationServices.WorkingDatabase</c> прежнее значение
        /// </summary>
        public void Dispose()
        {
            HostApplicationServices.WorkingDatabase = PrevDb;
            if (Save) NextDb.SaveAs(NextDb.Filename, DwgVersion.Current);
        }
    }
    /// <summary>
    /// Методы расширений для объектов класса Autodesk.AutoCAD.DatabaseServices.BlockTableRecord
    /// </summary>
    public static class BlockTableRecordExtensionMethods
    {
        /// <summary>
        /// Синхронизация вхождений блоков с их определением
        /// </summary>
        /// <param name="btr">Запись таблицы блоков, принятая за определение блока</param>
        /// <param name="directOnly">Следует ли искать только на верхнем уровне, или же нужно 
        /// анализировать и вложенные вхождения, т.е. следует ли рекурсивно обрабатывать блок в блоке
        /// true - только верхний; false - рекурсивно проверять вложенные блоки.</param>
        /// <param name="removeSuperfluous">
        /// Следует ли во вхождениях блока удалять лишние атрибуты (те, которых нет в определении блока).</param>
        /// <param name="setAttDefValues">
        /// Следует ли всем атрибутам, во вхождениях блока, назначить текущим значением значение по умолчанию.</param>
        public static void AttSync(this BlockTableRecord btr, bool directOnly = true, bool removeSuperfluous = false, bool setAttDefValues = false)
        {
            Database db = btr.Database;
            using (WorkingDatabaseSwitcher wdb = new WorkingDatabaseSwitcher(db))
            {
                using (Transaction t = db.TransactionManager.StartTransaction())
                {
                    BlockTable bt = (BlockTable)t.GetObject(db.BlockTableId, OpenMode.ForRead);

                    //Получаем все определения атрибутов из определения блока
                    IEnumerable<AttributeDefinition> attdefs = btr.Cast<ObjectId>()
                        .Where(n => n.ObjectClass.Name == "AcDbAttributeDefinition")
                        .Select(n => (AttributeDefinition)t.GetObject(n, OpenMode.ForRead))
                        .Where(n => !n.Constant);//Исключаем константные атрибуты, т.к. для них AttributeReference не создаются.

                    //В цикле перебираем все вхождения искомого определения блока
                    foreach (ObjectId brId in btr.GetBlockReferenceIds(directOnly, false))
                    {
                        BlockReference br = (BlockReference)t.GetObject(brId, OpenMode.ForWrite);

                        //Проверяем имена на соответствие. В том случае, если вхождение блока "A" вложено в определение блока "B", 
                        //то вхождения блока "B" тоже попадут в выборку. Нам нужно их исключить из набора обрабатываемых объектов 
                        //- именно поэтому проверяем имена.
                        if (br.Name != btr.Name)
                            continue;

                        //Получаем все атрибуты вхождения блока
                        IEnumerable<AttributeReference> attrefs = br.AttributeCollection.Cast<ObjectId>()
                            .Select(n => (AttributeReference)t.GetObject(n, OpenMode.ForWrite));

                        //Тэги существующих определений атрибутов
                        IEnumerable<string> dtags = attdefs.Select(n => n.Tag);
                        //Тэги существующих атрибутов во вхождении
                        IEnumerable<string> rtags = attrefs.Select(n => n.Tag);

                        //Если требуется - удаляем те атрибуты, для которых нет определения 
                        //в составе определения блока
                        if (removeSuperfluous)
                            foreach (AttributeReference attref in attrefs.Where(n => rtags
                                .Except(dtags).Contains(n.Tag)))
                                attref.Erase(true);

                        //Свойства существующих атрибутов синхронизируем со свойствами их определений
                        foreach (AttributeReference attref in attrefs.Where(n => dtags
                            .Join(rtags, a => a, b => b, (a, b) => a).Contains(n.Tag)))
                        {
                            AttributeDefinition ad = attdefs.First(n => n.Tag == attref.Tag);

                            //Метод SetAttributeFromBlock, используемый нами далее в коде, сбрасывает
                            //текущее значение многострочного атрибута. Поэтому запоминаем это значение,
                            //чтобы восстановить его сразу после вызова SetAttributeFromBlock.
                            string value = attref.TextString;
                            attref.SetAttributeFromBlock(ad, br.BlockTransform);
                            //Восстанавливаем значение атрибута
                            attref.TextString = value;

                            if (attref.IsMTextAttribute)
                            {

                            }

                            //Если требуется - устанавливаем для атрибута значение по умолчанию
                            if (setAttDefValues)
                                attref.TextString = ad.TextString;

                            attref.AdjustAlignment(db);
                        }

                        //Если во вхождении блока отсутствуют нужные атрибуты - создаём их
                        IEnumerable<AttributeDefinition> attdefsNew = attdefs.Where(n => dtags
                            .Except(rtags).Contains(n.Tag));

                        foreach (AttributeDefinition ad in attdefsNew)
                        {
                            AttributeReference attref = new AttributeReference();
                            attref.SetAttributeFromBlock(ad, br.BlockTransform);
                            attref.AdjustAlignment(db);
                            br.AttributeCollection.AppendAttribute(attref);
                            t.AddNewlyCreatedDBObject(attref, true);
                        }
                    }
                    btr.UpdateAnonymousBlocks();
                    t.Commit();
                }
                //Если это динамический блок
                if (btr.IsDynamicBlock)
                {
                    using (Transaction t = db.TransactionManager.StartTransaction())
                    {
                        foreach (ObjectId id in btr.GetAnonymousBlockIds())
                        {
                            BlockTableRecord _btr = (BlockTableRecord)t.GetObject(id, OpenMode.ForWrite);

                            //Получаем все определения атрибутов из оригинального определения блока
                            IEnumerable<AttributeDefinition> attdefs = btr.Cast<ObjectId>()
                                .Where(n => n.ObjectClass.Name == "AcDbAttributeDefinition")
                                .Select(n => (AttributeDefinition)t.GetObject(n, OpenMode.ForRead));

                            //Получаем все определения атрибутов из определения анонимного блока
                            IEnumerable<AttributeDefinition> attdefs2 = _btr.Cast<ObjectId>()
                                .Where(n => n.ObjectClass.Name == "AcDbAttributeDefinition")
                                .Select(n => (AttributeDefinition)t.GetObject(n, OpenMode.ForWrite));

                            //Определения атрибутов анонимных блоков следует синхронизировать 
                            //с определениями атрибутов основного блока

                            //Тэги существующих определений атрибутов
                            IEnumerable<string> dtags = attdefs.Select(n => n.Tag);
                            IEnumerable<string> dtags2 = attdefs2.Select(n => n.Tag);

                            //1. Удаляем лишние
                            foreach (AttributeDefinition attdef in attdefs2.Where(n => !dtags.Contains(n.Tag)))
                            {
                                attdef.Erase(true);
                            }

                            //2. Синхронизируем существующие
                            foreach (AttributeDefinition attdef in attdefs.Where(n => dtags
                                .Join(dtags2, a => a, b => b, (a, b) => a).Contains(n.Tag)))
                            {
                                AttributeDefinition ad = attdefs2.First(n => n.Tag == attdef.Tag);
                                ad.Position = attdef.Position;
                                ad.TextStyleId = attdef.TextStyleId;
                                //Если требуется - устанавливаем для атрибута значение по умолчанию
                                if (setAttDefValues)
                                    ad.TextString = attdef.TextString;

                                ad.Tag = attdef.Tag;
                                ad.Prompt = attdef.Prompt;

                                ad.LayerId = attdef.LayerId;
                                ad.Rotation = attdef.Rotation;
                                ad.LinetypeId = attdef.LinetypeId;
                                ad.LineWeight = attdef.LineWeight;
                                ad.LinetypeScale = attdef.LinetypeScale;
                                ad.Annotative = attdef.Annotative;
                                ad.Color = attdef.Color;
                                ad.Height = attdef.Height;
                                ad.HorizontalMode = attdef.HorizontalMode;
                                ad.Invisible = attdef.Invisible;
                                ad.IsMirroredInX = attdef.IsMirroredInX;
                                ad.IsMirroredInY = attdef.IsMirroredInY;
                                ad.Justify = attdef.Justify;
                                ad.LockPositionInBlock = attdef.LockPositionInBlock;
                                ad.MaterialId = attdef.MaterialId;
                                ad.Oblique = attdef.Oblique;
                                ad.Thickness = attdef.Thickness;
                                ad.Transparency = attdef.Transparency;
                                ad.VerticalMode = attdef.VerticalMode;
                                ad.Visible = attdef.Visible;
                                ad.WidthFactor = attdef.WidthFactor;

                                ad.CastShadows = attdef.CastShadows;
                                ad.Constant = attdef.Constant;
                                ad.FieldLength = attdef.FieldLength;
                                ad.ForceAnnoAllVisible = attdef.ForceAnnoAllVisible;
                                ad.Preset = attdef.Preset;
                                ad.Prompt = attdef.Prompt;
                                ad.Verifiable = attdef.Verifiable;

                                ad.AdjustAlignment(db);
                            }

                            //3. Добавляем недостающие
                            foreach (AttributeDefinition attdef in attdefs.Where(n => !dtags2.Contains(n.Tag)))
                            {
                                AttributeDefinition ad = new AttributeDefinition();
                                ad.SetDatabaseDefaults();
                                ad.Position = attdef.Position;
                                ad.TextStyleId = attdef.TextStyleId;
                                ad.TextString = attdef.TextString;
                                ad.Tag = attdef.Tag;
                                ad.Prompt = attdef.Prompt;

                                ad.LayerId = attdef.LayerId;
                                ad.Rotation = attdef.Rotation;
                                ad.LinetypeId = attdef.LinetypeId;
                                ad.LineWeight = attdef.LineWeight;
                                ad.LinetypeScale = attdef.LinetypeScale;
                                ad.Annotative = attdef.Annotative;
                                ad.Color = attdef.Color;
                                ad.Height = attdef.Height;
                                ad.HorizontalMode = attdef.HorizontalMode;
                                ad.Invisible = attdef.Invisible;
                                ad.IsMirroredInX = attdef.IsMirroredInX;
                                ad.IsMirroredInY = attdef.IsMirroredInY;
                                ad.Justify = attdef.Justify;
                                ad.LockPositionInBlock = attdef.LockPositionInBlock;
                                ad.MaterialId = attdef.MaterialId;
                                ad.Oblique = attdef.Oblique;
                                ad.Thickness = attdef.Thickness;
                                ad.Transparency = attdef.Transparency;
                                ad.VerticalMode = attdef.VerticalMode;
                                ad.Visible = attdef.Visible;
                                ad.WidthFactor = attdef.WidthFactor;

                                ad.CastShadows = attdef.CastShadows;
                                ad.Constant = attdef.Constant;
                                ad.FieldLength = attdef.FieldLength;
                                ad.ForceAnnoAllVisible = attdef.ForceAnnoAllVisible;
                                ad.Preset = attdef.Preset;
                                ad.Prompt = attdef.Prompt;
                                ad.Verifiable = attdef.Verifiable;

                                _btr.AppendEntity(ad);
                                t.AddNewlyCreatedDBObject(ad, true);
                                ad.AdjustAlignment(db);
                            }
                            //Синхронизируем все вхождения данного анонимного определения блока
                            _btr.AttSync(directOnly, removeSuperfluous, setAttDefValues);
                        }
                        //Обновляем геометрию определений анонимных блоков, полученных на основе 
                        //этого динамического блока
                        btr.UpdateAnonymousBlocks();
                        t.Commit();
                    }
                }
            }
        }
    }

    
}
