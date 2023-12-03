using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BaseFunction
{
    public static class BaseBlockReferenceClass
    {
        #region перенос блоков из других чертежей
        /// <summary>
        /// переносит отсутствующие в чертеже блоки из выбранного файла в используемый файл
        /// </summary>
        /// <param name="blNames">список с названиями нужных блоков</param>
        /// <param name="fileName">файл из которого переносятся блоки</param>
        /// <param name="folders">список местоположений где может находиться файл</param>
        /// <returns>true если блоки перенесены успешно</returns>
        public static bool BlockMigrate(List<string> blNames, string fileName, List<string> folders)
        {
            if (blNames.Count == 0) return true;           
            foreach (string folder in folders)
            {
                if (System.IO.Directory.Exists(folder))
                {
                    string[] allFoundFiles = Directory.GetFiles(folder, fileName, SearchOption.AllDirectories);
                    if (allFoundFiles.Count() > 0) return BlockMigrate(blNames, allFoundFiles[0]);
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
        public static bool BlockMigrate(List<string> blNames, string fullFileName)
        {
            if (blNames.Count == 0) return true;
            if (string.IsNullOrEmpty(fullFileName)) return false;
            return BlockMigrate(HostApplicationServices.WorkingDatabase, blNames, fullFileName);
        }
        public static bool BlockMigrate(Database targetDb, List<string> blNames, string fullFileName)
        {
            if (blNames.Count == 0) return true;
            if (string.IsNullOrEmpty(fullFileName)) return false;
            using (Database db = new Database(false, true))
            {
                try
                {
                    db.ReadDwgFile(fullFileName, FileShare.Read, true, String.Empty);
                    return (BlockMigrate(targetDb, blNames, db));
                }
                catch
                {
                    System.Windows.Forms.MessageBox.Show("Не удалось считать файл ресурсов");
                    return false;
                }               
            }
        }
        public static bool BlockMigrate(Database targetDb, List<string> blNames, Database storageDb)
        {
            bool result = true;
            DocumentLock documentLock = null;
            if (Application.DocumentManager != null && Application.DocumentManager.MdiActiveDocument != null)
            {
                documentLock = Application.DocumentManager.MdiActiveDocument.LockDocument();
            }
            //проверяем наличие блоков в чертеже           
            if (CheckBlocks(targetDb, blNames, out List<string> missBlocks)) return true;
            //если в списке отсутствующих блоков ничего нет то блоки в чертеже присутствуют           
            using (ObjectIdCollection missBlocksId = new ObjectIdCollection())
            {
                using (Transaction tr = storageDb.TransactionManager.StartTransaction())
                {
                    //получаем таблицу блоков
                    using (BlockTable bt = tr.GetObject(storageDb.BlockTableId, OpenMode.ForRead, false, true) as BlockTable)
                    {
                        //проходим по списку нужных блоков, если находим записываем его Id, если нет то возвращаем false                                
                        foreach (string block in missBlocks)
                        {
                            if (bt.Has(block))
                            {
                                missBlocksId.Add(bt[block]);
                            }
                            else
                            {
                                tr.Commit();
                                System.Windows.Forms.MessageBox.Show("В файле ресурсов блоки не найдены");
                                result = false;
                            }
                        }
                    }
                }
                if (result)
                {
                    //записываем блоки
                    using (IdMapping idMapping = new IdMapping())
                    {
                        try
                        {
                            targetDb.WblockCloneObjects(missBlocksId, targetDb.BlockTableId, idMapping, DuplicateRecordCloning.Ignore, false);
                        }
                        catch
                        {                         
                            result = false;
                        }
                    }
                    if (result) result = CheckBlocks(targetDb, blNames);
                }
            }
            documentLock?.Dispose();
            return result;
        }
        /// <summary>
        /// проверяет список блоков на наличие в базе данных
        /// </summary>
        /// <param name="db"></param>
        /// <param name="blNames"></param>
        /// <returns></returns>
        public static bool CheckBlocks(Database db, List<string> blNames)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                //получаем таблицу блоков
                using (BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead, false, true) as BlockTable)
                {
                    //проверяем наличие блоков
                    foreach (string block in blNames)
                    {
                        //если не найден то печалька
                        if (!bt.Has(block))
                        {
                            tr.Commit();                            
                            return false;
                        }
                    }
                }
                tr.Commit();
            }
            return true;
        }
        /// <summary>
        /// проверяет список блоков на наличие в базе данных и возвращает список отсутствующих
        /// </summary>
        /// <param name="db"></param>
        /// <param name="blNames"></param>
        /// <param name="missBlocks"></param>
        /// <returns></returns>
        public static bool CheckBlocks(Database db, List<string> blNames, out List<string> missBlocks)
        {            
            //объявляем переменную для хранения недостающих блоков
            missBlocks = new List<string>();
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
            if (missBlocks.Count == 0) return true; return false;
        }
        #endregion

        #region изменение атрибутов в блоке
        /// <summary>
        /// изменяет атрибуты блока
        /// </summary>
        /// <param name="brId">ObjectId блока</param>
        /// <param name="attributes">список (таг, значение) изменяемых атрибутов</param>
        public static bool BlockReferenceChangeAttribute(ObjectId brId, List<(string tag, object value)> attributes)
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
        public static bool BlockReferenceChangeAttribute(ObjectId brId, string tag, string value)
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
        public static bool BlockReferenceChangeAttribute(ObjectId brId, Dictionary<string, string> attributes)
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
        public static bool BlockReferenceChangeAttribute(ObjectId brId, Dictionary<string, string> attributes, bool allReplace)
        {
            if (brId == null || brId == ObjectId.Null) return false;
            DocumentLock documentLock = null;
            if (Application.DocumentManager != null && Application.DocumentManager.MdiActiveDocument != null)
            {
                documentLock = Application.DocumentManager.MdiActiveDocument.LockDocument();
            }
            attributes = new Dictionary<string, string>(attributes, StringComparer.InvariantCultureIgnoreCase);
            List<string> usingTag = new List<string>();
            bool result = false;
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
                            using (AttributeReference attRef = tr.GetObject(id, OpenMode.ForWrite, false, true) as AttributeReference)
                            {
                                if (attRef != null && attributes.ContainsKey(attRef.Tag))
                                {
                                    attRef.TextString = attributes[attRef.Tag];
                                    if (!usingTag.Contains(attRef.Tag)) usingTag.Add(attRef.Tag);
                                }
                            }
                        }
                    }
                }
                if (usingTag.Count.Equals(attributes.Count)) result = true;
                if (result || !allReplace) tr.Commit();
                else tr.Abort();
            }
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
            DocumentLock documentLock = null;
            if (Application.DocumentManager != null && Application.DocumentManager.MdiActiveDocument != null)
            {
                documentLock = Application.DocumentManager.MdiActiveDocument.LockDocument();
            }
            //запускаем транзакцию
            using (Transaction tr = HostApplicationServices.WorkingDatabase.TransactionManager.StartTransaction())
            {
                //получаем вхождение блока
                using (BlockReference br = tr.GetObject(brId, OpenMode.ForWrite, false, true) as BlockReference)
                {
                    if (br != null)
                    {
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
                    }
                }
                tr.Commit();
            }
            documentLock?.Dispose();
        }
        #endregion

        #region получение атрибутов из блока
        /// <summary>
        /// Получает словарь со всеми парами таг/значение атрибутов блока
        /// </summary>
        /// <param name="br"></param>
        /// <param name="tag"></param>
        /// <param name="result"></param>
        /// <returns>true если получено хотя бы одно значение</returns>        
        public static bool BlockReferenceGetAttribute(BlockReference br, out Dictionary<string, string> result)
        {
            result = new Dictionary<string, string>();
            if (br == null) return false;
            DocumentLock documentLock = null;
            if (Application.DocumentManager != null && Application.DocumentManager.MdiActiveDocument != null)
            {
                documentLock = Application.DocumentManager.MdiActiveDocument.LockDocument();
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
        public static bool BlockReferenceGetAttribute(ObjectId brId, out Dictionary<string, string> result)
        {
            result = new Dictionary<string, string>();
            if (brId == null || brId == ObjectId.Null) return false;
            DocumentLock documentLock = null;
            if (Application.DocumentManager != null && Application.DocumentManager.MdiActiveDocument != null)
            {
                documentLock = Application.DocumentManager.MdiActiveDocument.LockDocument();
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
        public static bool BlockReferenceGetAttribute(BlockReference br, string tag, out string result)
        {
            result = string.Empty;
            if (br == null) return false;
            DocumentLock documentLock = null;
            if (Application.DocumentManager != null && Application.DocumentManager.MdiActiveDocument != null)
            {
                documentLock = Application.DocumentManager.MdiActiveDocument.LockDocument();
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
        public static bool BlockReferenceGetAttribute(ObjectId brId, string tag, out string result)
        {
            result = string.Empty;
            if (brId == null || brId == ObjectId.Null) return false;
            DocumentLock documentLock = null;
            if (Application.DocumentManager != null && Application.DocumentManager.MdiActiveDocument != null)
            {
                documentLock = Application.DocumentManager.MdiActiveDocument.LockDocument();
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
    }
}
