using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.LayerManager;
using System.Collections.Generic;

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
        /// Изменяет параметры существующего слоя
        /// </summary>
        public static LayerTypeChange LayerChangeParametrs(string layer_name, short colorIndex)
        {
            return LayerChangeParametrs(layer_name, colorIndex, null, null);
        }
        /// <summary>
        /// Изменяет параметры существующего слоя
        /// </summary>
        public static LayerTypeChange LayerChangeParametrs(string layer_name, LineWeight lineWeight)
        {
            return LayerChangeParametrs(layer_name, null, lineWeight, null);
        }
        /// <summary>
        /// Изменяет параметры существующего слоя
        /// </summary>
        public static LayerTypeChange LayerChangeParametrs(string layer_name, string lineType)
        {
            return LayerChangeParametrs(layer_name, null, null, lineType);
        }
        /// <summary>
        /// Изменяет параметры существующего слоя
        /// </summary>
        /// <param name="layer_name">название изменяемого слоя</param>
        /// <param name="colorIndex">новый индекс цвета :0 - 256 (если вне диапазона или null то не изменяется)</param>
        /// <param name="lineWeight">новый вес линий (null не изменянятся)</param>
        /// <param name="lineType">название нового типа линий (пустая строка или null то не меняется)</param>
        public static LayerTypeChange LayerChangeParametrs(string layer_name, short? colorIndex, LineWeight? lineWeight, string lineType)
        {            
            List<LayerTypeChange> variant = new List<LayerTypeChange>();
            //если имя не задано преращаем
            if (string.IsNullOrEmpty(layer_name)) return LayerTypeChange.none;
            //если не задано ни одного параметра для изменения прекращаем
            if (colorIndex == null & lineWeight == null & string.IsNullOrEmpty(lineType)) return LayerTypeChange.none;
            DocumentLock documentLock = null;
            if (Application.DocumentManager != null && Application.DocumentManager.MdiActiveDocument != null)
            {
                documentLock = Application.DocumentManager.MdiActiveDocument.LockDocument();
            }
            //используем транзакцию что бы получить данные о слоях 
            using (Transaction tr = HostApplicationServices.WorkingDatabase.TransactionManager.StartTransaction())
            {
                //получаем таблицу слоев
                using (LayerTable lt = tr.GetObject(HostApplicationServices.WorkingDatabase.LayerTableId, OpenMode.ForRead) as LayerTable)
                {
                    //если в таблице слоев есть нужный слой
                    if (lt.Has(layer_name))
                    {
                        //открываем слой на запись 
                        using (LayerTableRecord layer = tr.GetObject(lt[layer_name], OpenMode.ForWrite, false, true) as LayerTableRecord)
                        {
                            //изменяем цвет слоя
                            if (colorIndex.HasValue)
                            {
                                if (colorIndex.Value >= 0 & colorIndex.Value <= 256)
                                {
                                    layer.Color = Color.FromColorIndex(ColorMethod.ByAci, colorIndex.Value);
                                    variant.Add(LayerTypeChange.full);
                                }
                                else variant.Add(LayerTypeChange.none);
                            }
                            //изменяем тип линии слоя
                            if (!string.IsNullOrEmpty(lineType))
                            {
                                using (LinetypeTable typeTable = tr.GetObject(HostApplicationServices.WorkingDatabase.LinetypeTableId, OpenMode.ForRead, false, true) as LinetypeTable)
                                {
                                    if (typeTable.Has(lineType))
                                    {
                                        layer.LinetypeObjectId = typeTable[lineType];
                                        variant.Add(LayerTypeChange.full);
                                    }
                                    else variant.Add(LayerTypeChange.none);

                                }
                            }
                            //изменяем вес линии слоя
                            if (lineWeight.HasValue)
                            {
                                layer.LineWeight = lineWeight.Value;
                                variant.Add(LayerTypeChange.full);
                            }
                        }
                    }
                }
                tr.Commit();
            }
            documentLock?.Dispose();
            if (variant.Contains(LayerTypeChange.full) && variant.Contains(LayerTypeChange.none)) return LayerTypeChange.fragmentary;
            else if (variant.Contains(LayerTypeChange.full)) return LayerTypeChange.full;
            else return LayerTypeChange.none;
        }
        public static void LayerDelete(string layerName)
        {
            using (Transaction tr = HostApplicationServices.WorkingDatabase.TransactionManager.StartTransaction())
            {
                LayerTable lt = tr.GetObject(HostApplicationServices.WorkingDatabase.LayerTableId, OpenMode.ForRead, false, true) as LayerTable;

                if (lt != null)
                {
                    if (lt.Has(layerName))
                    {
                        BlockTable blockTable = tr.GetObject(HostApplicationServices.WorkingDatabase.BlockTableId, OpenMode.ForRead) as BlockTable;

                        foreach (ObjectId btrId in blockTable)
                        {
                            BlockTableRecord btr = tr.GetObject(btrId, OpenMode.ForRead, false, true) as BlockTableRecord;

                            foreach (ObjectId id in btr)
                            {
                                Entity e = tr.GetObject(id, OpenMode.ForRead, false, true) as Entity;
                                if (e == null || e.Layer != layerName) continue;
                                e.UpgradeOpen();
                                e.Erase();
                            }
                        }
                        LayerTableRecord ltr = tr.GetObject(lt[layerName], OpenMode.ForWrite, false, true) as LayerTableRecord;
                        ltr?.Erase();
                    }
                }

                tr.Commit();
            }

        }
        public static bool LayerGroupNew(string groupName)
        {
            using (Transaction tr = HostApplicationServices.WorkingDatabase.TransactionManager.StartTransaction())
            {              
                //LayerFilterTree lf = ;
                //LayerFilter clg = lf.Current;
                //if (clg != null)
                //{
                //    if (clg.IsIdFilter)
                //    {
                //        ((LayerGroup)clg).LayerIds.Add(newLayerId);
                //        clayerId.Database.LayerFilters = lf;
                //    }
                //}
                tr.Commit();
            }
            return true;
        }
        /// <summary>
        /// Создает новый слой в Autocad
        /// </summary>       
        public static bool LayerNew(string layer_name)
        {
            return LayerNew(layer_name, false, false);
        }
        /// <summary>
        /// Создает новый слой в Autocad
        /// </summary>
        /// <param name="layer_name">Название слоя</param>
        /// <param name="inGroup">Поместить в текущую группу?</param>
        /// <returns>false - если не удалось создать</returns>       
        public static bool LayerNew(string layer_name, bool inGroup, bool message)
        {
            //если не задано название то прекращаем
            if (string.IsNullOrEmpty(layer_name)) return false;
            DocumentLock documentLock = null;
            if (Application.DocumentManager != null && Application.DocumentManager.MdiActiveDocument != null)
            {
                documentLock = Application.DocumentManager.MdiActiveDocument.LockDocument();
            }
            bool result = true;
            //используем транзакцию что бы получить данные о слоях и если нужного слоя нет то создать его
            using (Transaction tr = HostApplicationServices.WorkingDatabase.TransactionManager.StartTransaction())
            {
                //получаем таблицу слоев
                using (LayerTable lt = tr.GetObject(HostApplicationServices.WorkingDatabase.LayerTableId, OpenMode.ForWrite) as LayerTable)
                {
                    //получаем Id текущего слоя
                    ObjectId clayerId = HostApplicationServices.WorkingDatabase.Clayer;
                    //если слоя в таблице нет то создаем, если есть то включаем/размораживаем/разблокируем
                    if (!lt.Has(layer_name))
                    {
                        try
                        {
                            //если нет создаем слой
                            using (LayerTableRecord layer = new LayerTableRecord
                            {
                                Name = layer_name,
                                IsOff = false,
                                IsFrozen = false,
                                IsLocked = false,
                                Color = Color.FromColorIndex(ColorMethod.ByAci, 7),
                                LineWeight = LineWeight.ByLineWeightDefault,
                                LinetypeObjectId = HostApplicationServices.WorkingDatabase.ContinuousLinetype
                            })
                            {
                                //добавляем слой с таблицу слоев и получаем его ObjectId
                                ObjectId newLayerId = lt.Add(layer);
                                tr.AddNewlyCreatedDBObject(layer, true);
                                //если требуется помещаем в текущую группу
                                if (inGroup)
                                {
                                    //тут магия)
                                    LayerFilterTree lf = clayerId.Database.LayerFilters;
                                    LayerFilter clg = lf.Current;
                                    if (clg != null)
                                    {
                                        if (clg.IsIdFilter)
                                        {
                                            ((LayerGroup)clg).LayerIds.Add(newLayerId);
                                            clayerId.Database.LayerFilters = lf;
                                        }
                                    }
                                }
                            }
                        }
                        catch
                        {
                            if (message) System.Windows.Forms.MessageBox.Show("Введено некорректное название слоя");                           
                            result = false;
                        }
                    }
                    else
                    {
                        //если слой есть то вытаскиваем его из таблицы слоев
                        LayerTableRecord layer = tr.GetObject(lt[layer_name], OpenMode.ForWrite, false, true) as LayerTableRecord;
                        // включаем разблокируем и размораживаем слой
                        layer.IsOff = false;
                        //если слой текущий то морозить/размораживать нельзя, будет фатал
                        if (layer.Id != clayerId) { layer.IsFrozen = false; }
                        layer.IsLocked = false;
                    }
                }
                tr.Commit();              
            }
            documentLock.Dispose();
            return result;
        }

        
    }
    public enum LayerTypeChange
    {
        none,
        fragmentary,
        full
    }
}
