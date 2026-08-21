using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.GraphicsInterface;

using Autodesk.AutoCAD.LayerManager;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BaseFunction
{
    public static class BaseLayerClass
    {
        /// <summary>
        /// возвращает список слоев активной базы данных
        /// </summary>      
        public static List<string> GetLayerNames(bool dependent = true)
        {
            List<string> result = new List<string>();
            using (Transaction tr = HostApplicationServices.WorkingDatabase.TransactionManager.StartTransaction())
            {
                using (LayerTable lt = tr.GetObject(HostApplicationServices.WorkingDatabase.LayerTableId, OpenMode.ForRead) as LayerTable)
                {
                    foreach (ObjectId id in lt)
                    {
                        using (LayerTableRecord layer = tr.GetObject(id, OpenMode.ForRead, false, true) as LayerTableRecord)
                        {
                            if (!dependent && layer.IsDependent) continue;
                            result.Add(layer.Name);
                        }
                    }
                }
                tr.Commit();
            }
            return result;
        }
        /// <summary>
        /// возвращает список слоев активной базы данных
        /// </summary>      
        public static Dictionary<string, Color> GetLayerNamesAndColors(bool dependent = true)
        {
            Dictionary<string, Color> result = new Dictionary<string, Color>();
            using (Transaction tr = HostApplicationServices.WorkingDatabase.TransactionManager.StartTransaction())
            {
                using (LayerTable lt = tr.GetObject(HostApplicationServices.WorkingDatabase.LayerTableId, OpenMode.ForRead) as LayerTable)
                {
                    foreach (ObjectId id in lt)
                    {
                        using (LayerTableRecord layer = tr.GetObject(id, OpenMode.ForRead, false, true) as LayerTableRecord)
                        {
                            if (!dependent && layer.IsDependent) continue;
                            result.Add(layer.Name, layer.Color);
                        }
                    }
                }
                tr.Commit();
            }
            return result.OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value);
        }
        /// <summary>
        /// возвращает список слоев активной базы данных c полным перечнем их параметров
        /// </summary>      
        public static Dictionary<string, LayerData> GetLayerNamesAndData(bool dependent = true)
        {
            Dictionary<string, LayerData> result = new Dictionary<string, LayerData>();
            using (Transaction tr = HostApplicationServices.WorkingDatabase.TransactionManager.StartTransaction())
            {
                using (LayerTable lt = tr.GetObject(HostApplicationServices.WorkingDatabase.LayerTableId, OpenMode.ForRead) as LayerTable)
                {
                    foreach (ObjectId id in lt)
                    {
                        using (LayerTableRecord layer = tr.GetObject(id, OpenMode.ForRead, false, true) as LayerTableRecord)
                        {
                            if (!dependent && layer.IsDependent) continue;
                            result.Add(layer.Name, new LayerData(layer, tr));
                        }
                    }
                }
                tr.Commit();
            }
            return result.OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value);
        }
        // ==========================================
        // ИЗОЛИРОВАННЫЕ МЕТОДЫ (Создают свою транзакцию)
        // ==========================================

        public static void LayerChangeColor(string layerName, Color color)
        {
            if (string.IsNullOrEmpty(layerName) || color == null) return;
            Document doc = Application.DocumentManager?.MdiActiveDocument;
            if (doc == null) return;

            using (DocumentLock docLock = doc.LockDocument())
            {
                using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
                {
                    LayerChangeColor(tr, doc.Database, layerName, color);
                    tr.Commit();
                }
            }
        }

        public static LayerTypeChange LayerChangeParametrs(string layerName, short colorIndex)
        {
            return LayerChangeParametrs(layerName, (short?)colorIndex, null, null);
        }

        public static LayerTypeChange LayerChangeParametrs(string layerName, LineWeight lineWeight)
        {
            return LayerChangeParametrs(layerName, null, lineWeight, null);
        }

        public static LayerTypeChange LayerChangeParametrs(string layerName, string lineType)
        {
            return LayerChangeParametrs(layerName, null, null, lineType);
        }

        public static LayerTypeChange LayerChangeParametrs(string layerName, short? colorIndex, LineWeight? lineWeight, string lineType)
        {
            if (string.IsNullOrEmpty(layerName)) return LayerTypeChange.none;
            if (colorIndex == null && lineWeight == null && string.IsNullOrEmpty(lineType)) return LayerTypeChange.none;

            Document doc = Application.DocumentManager?.MdiActiveDocument;
            if (doc == null) return LayerTypeChange.none;

            using (DocumentLock docLock = doc.LockDocument())
            {
                using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
                {
                    var result = LayerChangeParametrsCore(tr, doc.Database, layerName, colorIndex, lineWeight, lineType);
                    tr.Commit();
                    return result;
                }
            }
        }

        // ==========================================
        // БОЕВЫЕ МЕТОДЫ (Принимают открытую транзакцию)
        // ==========================================

        public static void LayerChangeColor(Transaction tr, Database db, string layerName, Color color)
        {
            LayerTable lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
            if (lt.Has(layerName))
            {
                LayerTableRecord layer = (LayerTableRecord)tr.GetObject(lt[layerName], OpenMode.ForWrite);
                layer.Color = color;
            }
        }

        public static LayerTypeChange LayerChangeParametrs(Transaction tr, string layerName, short? colorIndex, LineWeight? lineWeight, string lineType)
        {
            return LayerChangeParametrsCore(tr, HostApplicationServices.WorkingDatabase, layerName, colorIndex, lineWeight, lineType);
        }

        // ==========================================
        // ЯДРО СМЕНЫ ПАРАМЕТРОВ
        // ==========================================
        private static LayerTypeChange LayerChangeParametrsCore(Transaction tr, Database db, string layerName, short? colorIndex, LineWeight? lineWeight, string lineType)
        {
            List<LayerTypeChange> variant = new List<LayerTypeChange>();

            LayerTable lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
            if (!lt.Has(layerName)) return LayerTypeChange.none;

            LayerTableRecord layer = (LayerTableRecord)tr.GetObject(lt[layerName], OpenMode.ForWrite);

            // 1. Изменение цвета
            if (colorIndex.HasValue)
            {
                if (colorIndex.Value >= 0 && colorIndex.Value <= 256)
                {
                    layer.Color = Color.FromColorIndex(ColorMethod.ByAci, colorIndex.Value);
                    variant.Add(LayerTypeChange.full);
                }
                else variant.Add(LayerTypeChange.none);
            }

            // 2. Изменение типа линии
            if (!string.IsNullOrEmpty(lineType))
            {
                LinetypeTable typeTable = (LinetypeTable)tr.GetObject(db.LinetypeTableId, OpenMode.ForRead);
                if (typeTable.Has(lineType))
                {
                    layer.LinetypeObjectId = typeTable[lineType];
                    variant.Add(LayerTypeChange.full);
                }
                else variant.Add(LayerTypeChange.none);
            }

            // 3. Изменение веса линии
            if (lineWeight.HasValue)
            {
                layer.LineWeight = lineWeight.Value;
                variant.Add(LayerTypeChange.full);
            }

            // Расчет возвращаемого статуса модификации
            if (variant.Contains(LayerTypeChange.full) && variant.Contains(LayerTypeChange.none)) return LayerTypeChange.fragmentary;
            if (variant.Contains(LayerTypeChange.full)) return LayerTypeChange.full;

            return LayerTypeChange.none;
        }

        // ==========================================
        // МЕТОД УДАЛЕНИЯ СЛОЯ (Безопасный и быстрый)
        // ==========================================
        /// <summary>
        /// Безопасное удаление слоя. Очищает геометрию и удаляет слой через Purge, исключая появление битых ссылок.
        /// </summary>
        public static void LayerDelete(Transaction tr, Database db, string layerName)
        {
            LayerTable lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
            if (!lt.Has(layerName)) return;

            ObjectId layerId = lt[layerName];

            // Защита от удаления системных слоев
            if (layerId == db.Clayer ||
                layerName.Equals("0", StringComparison.OrdinalIgnoreCase) ||
                layerName.Equals("Defpoints", StringComparison.OrdinalIgnoreCase))
            {
                System.Windows.MessageBox.Show($"Критическая ошибка: Нельзя удалить системный или текущий слой '{layerName}'.");
                return;
            }

            // 1. Сначала честно стираем все графические примитивы на этом слое по всей базе данных
            BlockTable blockTable = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
            foreach (ObjectId btrId in blockTable)
            {
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(btrId, OpenMode.ForRead);
                foreach (ObjectId entId in btr)
                {
                    Entity e = tr.GetObject(entId, OpenMode.ForRead) as Entity;
                    if (e == null || e.Layer != layerName) continue;

                    e.UpgradeOpen();
                    e.Erase();
                }
            }

            // 2. ИНЖЕНЕРНЫЙ FIX: Проверяем слой на наличие скрытых неграфических ссылок через Purge
            ObjectIdCollection idCollection = new ObjectIdCollection(new ObjectId[] { layerId });
            db.Purge(idCollection); // Метод удалит из коллекции ID те слои, которые СЕЙЧАС ИСПОЛЬЗУЮТСЯ (нельзя очистить)

            // 3. Если layerId остался в коллекции, значит графики нет И неграфических ссылок тоже нет — слой чист!
            if (idCollection.Contains(layerId))
            {
                LayerTableRecord ltr = (LayerTableRecord)tr.GetObject(layerId, OpenMode.ForWrite);
                ltr.Erase(); // Теперь это абсолютно безопасно, чертеж будет чистым без ошибок AUDIT
            }
            else
            {
                // Если AutoCAD не разрешил Purge, мы не вызываем Erase(), чтобы избежать "зомби-ссылок"
                System.Windows.MessageBox.Show($"Графика со слоя '{layerName}' удалена. Сам слой оставлен в базе, так как на него ссылаются системные объекты чертежа (размерные стили, блоки или xData).");
            }
        }


        // ==========================================
        // ПЕРЕГРУЗКИ ДЛЯ ИЗОЛИРОВАННОГО ВЫЗОВА (Создают свою транзакцию)
        // ==========================================

        public static bool LayerNew(string layerName)
        {
            return LayerNew(layerName, false, false);
        }

        public static bool LayerNew(string layerName, bool inGroup, bool showMessage)
        {
            if (string.IsNullOrEmpty(layerName)) return false;

            Document doc = Application.DocumentManager?.MdiActiveDocument;
            if (doc == null) return false;

            // Блокируем документ безопасно через using
            using (DocumentLock docLock = doc.LockDocument())
            {
                using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
                {
                    bool result = LayerNewCore(tr, doc.Database, layerName, inGroup, showMessage);
                    if (result) tr.Commit();
                    return result;
                }
            }
        }

        // ==========================================
        // ПЕРЕГРУЗКИ ДЛЯ БОЕВОГО ИСПОЛЬЗОВАНИЯ (Принимают открытую транзакцию)
        // ==========================================

        public static bool LayerNew(Transaction tr, string layerName)
        {
            return LayerNewCore(tr, HostApplicationServices.WorkingDatabase, layerName, false, false);
        }

        public static bool LayerNew(Transaction tr, string layerName, bool inGroup, bool showMessage)
        {
            return LayerNewCore(tr, HostApplicationServices.WorkingDatabase, layerName, inGroup, showMessage);
        }

        // ==========================================
        // КОРНЕВАЯ ЛОГИКА (Внутреннее ядро метода)
        // ==========================================
        private static bool LayerNewCore(Transaction tr, Database db, string layerName, bool inGroup, bool showMessage)
        {
            // 1. Проверяем имя слоя на запрещенные символы AutoCAD
            try
            {
                SymbolUtilityServices.ValidateSymbolName(layerName, false);
            }
            catch
            {
                if (showMessage) System.Windows.MessageBox.Show($"Некорректное название слоя: '{layerName}'. Слой содержит запрещенные символы.");
                return false;
            }

            try
            {
                // Открываем таблицу строго на чтение (оптимизация)
                LayerTable lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
                ObjectId clayerId = db.Clayer;

                if (!lt.Has(layerName))
                {
                    // Повышаем права таблицы до записи только при необходимости
                    lt.UpgradeOpen();

                    // ИНЖЕНЕРНЫЙ FIX: Никаких 'using' для LayerTableRecord, если добавляем его в базу!
                    LayerTableRecord layer = new LayerTableRecord
                    {
                        Name = layerName,
                        IsOff = false,
                        IsFrozen = false,
                        IsLocked = false,
                        Color = Color.FromColorIndex(ColorMethod.ByAci, 7),
                        LineWeight = LineWeight.ByLineWeightDefault,
                        LinetypeObjectId = db.ContinuousLinetype
                    };

                    ObjectId newLayerId = lt.Add(layer);
                    tr.AddNewlyCreatedDBObject(layer, true);

                    // Магия добавления в текущую группу фильтров слоев
                    if (inGroup)
                    {
                        LayerFilterTree lf = db.LayerFilters;
                        LayerFilter clg = lf.Current;
                        if (clg != null && clg.IsIdFilter)
                        {
                            ((LayerGroup)clg).LayerIds.Add(newLayerId);
                            db.LayerFilters = lf;
                        }
                    }
                }
                else
                {
                    // Если слой есть, открываем на запись только запись слоя
                    LayerTableRecord layer = (LayerTableRecord)tr.GetObject(lt[layerName], OpenMode.ForWrite);

                    layer.IsOff = false;
                    layer.IsLocked = false;

                    // Безопасная проверка заморозки: текущий слой морозить нельзя
                    if (layer.Id != clayerId)
                    {
                        layer.IsFrozen = false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                if (showMessage) System.Windows.MessageBox.Show($"Ошибка при работе со слоем: {ex.Message}");
                return false;
            }
        }


    }
    public class LayerData
    {
        public LayerData(LayerTableRecord layer, Transaction tr)
        {
            Name = layer.Name;
            Color = layer.Color;
            LineWeight = layer.LineWeight;
            LineTypeObjectId = layer.LinetypeObjectId;
            IsFrozen = layer.IsFrozen;
            IsLocked = layer.IsLocked;
            IsOff = layer.IsOff;

            LinetypeTableRecord linetypeTableRecord = tr.GetObject(LineTypeObjectId, OpenMode.ForRead) as LinetypeTableRecord;

            LineTypeName = linetypeTableRecord.Name;
        }
        public string Name { get; set; }
        public Color Color { get; set; }
        public ObjectId LineTypeObjectId { get; set; }
        public string LineTypeName { get; set; }
        public LineWeight LineWeight { get; set; }
        public bool IsFrozen { get; set; }
        public bool IsLocked { get; set; }
        public bool IsOff { get; set; }
    }
    public enum LayerTypeChange
    {
        none,
        fragmentary,
        full
    }
}
