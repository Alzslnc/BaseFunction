using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaseFunction
{
    internal static class BaseCreateStyleClass
    {
        #region textStyle
        public static ObjectId CreateTextStyle(Transaction tr, Database db, string styleName, string primaryFont = "isocpeur.ttf", string backupFont = "arial.ttf")
        {
            return CreateTextStyle(tr, db, styleName, out _, false, primaryFont, backupFont);
        }
        public static ObjectId CreateTextStyle(Transaction tr, Database db, string styleName, out TextStyleTableRecord textStyleObject, bool returnStyleObject = true, string primaryFont = "isocpeur.ttf", string backupFont = "arial.ttf")
        {
            textStyleObject = null;

            if (tr == null || db == null || string.IsNullOrWhiteSpace(styleName))
                return ObjectId.Null;

            // 1. Открываем таблицу стилей текста на чтение (Для проверки этого достаточно)
            TextStyleTable tst = tr.GetObject(db.TextStyleTableId, OpenMode.ForRead) as TextStyleTable;
            if (tst == null)
                return ObjectId.Null;

            // Если стиль уже существует
            if (tst.Has(styleName))
            {
                ObjectId existingId = tst[styleName];
                if (returnStyleObject)
                {
                    // Открываем существующий стиль на запись для изменений "на лету"
                    textStyleObject = tr.GetObject(existingId, OpenMode.ForWrite) as TextStyleTableRecord;
                }
                return existingId;
            }

            // 2. Создаем новый текстовый стиль
            TextStyleTableRecord tstr = new TextStyleTableRecord
            {
                Name = styleName
            };

            // Проверяем физическое наличие основного шрифта в системе (в папке Fonts или Fonts внутри AutoCAD)
            // Так как присвоение FileName не генерирует Exception, проверяем существование файла.
            // Если путь не абсолютный, AutoCAD ищет в своих путях поддержки. Для надежности просто пишем основной.
            tstr.FileName = primaryFont;

            // Дополнительная настройка стиля (опционально, базовые дефолты)
            tstr.TextSize = 0.0; // Высота 0 делает стиль динамическим по высоте (удобно для MText/Размеров)

            // 3. Добавляем новый стиль в базу данных
            // Переводим таблицу стилей на запись только в момент добавления
            tst.UpgradeOpen();
            ObjectId textStyleId = tst.Add(tstr);
            tr.AddNewlyCreatedDBObject(tstr, true);

            if (returnStyleObject)
            {
                // Гарантируем, что только что созданный объект открыт на запись для вызывающего кода
                if (!tstr.IsWriteEnabled)
                {
                    tstr.UpgradeOpen();
                }
                textStyleObject = tstr;
            }

            return textStyleId;
        }
        #endregion

        #region Dimension
        public static ObjectId CreateDimensionStyle(Transaction tr, Database db, string styleName, ObjectId textStyleId)
        {
            return CreateDimensionStyle(tr, db, styleName, textStyleId, false, out _);
        }
        public static ObjectId CreateDimensionStyle(Transaction tr, Database db, string styleName, ObjectId textStyleId, bool returnStyleObject, out DimStyleTableRecord dimStyleRecord)
        {
            dimStyleRecord = null;

            if (tr == null || db == null || string.IsNullOrWhiteSpace(styleName))
                return ObjectId.Null;

            DimStyleTable dst = tr.GetObject(db.DimStyleTableId, OpenMode.ForWrite) as DimStyleTable;
            if (dst == null)
                return ObjectId.Null;

            // Если стиль уже существует
            if (dst.Has(styleName))
            {
                ObjectId existingId = dst[styleName];
                if (returnStyleObject)
                {
                    dimStyleRecord = tr.GetObject(existingId, OpenMode.ForWrite) as DimStyleTableRecord;
                }
                return existingId;
            }

            // Создаем новый стиль
            DimStyleTableRecord dstr = new DimStyleTableRecord
            {
                Name = styleName,
                Dimadec = 0,
                Dimalt = false,
                Dimaltd = 2,
                Dimaltf = 25.4,
                Dimaltrnd = 0,
                Dimalttd = 2,
                Dimalttz = 0,
                Dimaltu = 2,
                Dimaltz = 0,
                Dimarcsym = 0,
                Dimasz = 0.01,
                Dimatfit = 3,
                Dimaunit = 0,
                Dimazin = 0,
                Dimcen = 0.09,
                Dimdec = 0,
                Dimdle = 0,
                Dimdli = 0.01,
                Dimexe = 0.01,
                Dimexo = 0.001,
                Dimfrac = 0,
                Dimfxlen = 1,
                DimfxlenOn = false,
                Dimgap = 0.001,
                Dimjust = 0,
                Dimlfac = 1000,
                Dimlunit = 2,
                Dimrnd = 0,
                Dimscale = 1,
                Dimtad = 0,
                Dimtdec = 0,
                Dimtfac = 1,
                Dimtfill = 0,
                Dimtxt = 0.08
            };

            if (textStyleId != ObjectId.Null)
            {
                dstr.Dimtxsty = textStyleId;
            }

            ObjectId dimStyleId = dst.Add(dstr);
            tr.AddNewlyCreatedDBObject(dstr, true);

            if (returnStyleObject)
            {
                dimStyleRecord = dstr;
            }

            return dimStyleId;
        }
        #endregion

        #region MLeader
        public static ObjectId CreateMLeaderStyle(Transaction tr, Database db, string styleName, ObjectId textStyleId)
        {
            return CreateMLeaderStyle(tr, db, styleName, textStyleId, false, out _);
        }
        public static ObjectId CreateMLeaderStyle(Transaction tr, Database db, string styleName, ObjectId textStyleId, bool returnStyleObject, out MLeaderStyle mLeaderStyleObject)
        {
            mLeaderStyleObject = null;

            if (tr == null || db == null || string.IsNullOrWhiteSpace(styleName))
                return ObjectId.Null;

            DBDictionary mLeaders = tr.GetObject(db.MLeaderStyleDictionaryId, OpenMode.ForRead) as DBDictionary;
            if (mLeaders == null)
                return ObjectId.Null;

            // Если стиль уже существует
            if (mLeaders.Contains(styleName))
            {
                ObjectId existingId = mLeaders.GetAt(styleName);
                if (returnStyleObject)
                {
                    mLeaderStyleObject = tr.GetObject(existingId, OpenMode.ForWrite) as MLeaderStyle;
                }
                return existingId;
            }

            // Создаем новый стиль мультивыноски
            MLeaderStyle mls = new MLeaderStyle
            {
                ContentType = ContentType.MTextContent,
                TextHeight = 0.08,
                ArrowSize = 0.0,
                EnableDogleg = true,
                EnableLanding = true,
                DoglegLength = 0.05,
                LandingGap = 0.0,
                TextAlignmentType = TextAlignmentType.RightAlignment
            };

            if (textStyleId != ObjectId.Null)
            {
                mls.TextStyleId = textStyleId;
            }

            mls.SetTextAttachmentType(TextAttachmentType.AttachmentBottomOfTopLine, LeaderDirectionType.BottomLeader);
            mls.SetTextAttachmentType(TextAttachmentType.AttachmentBottomOfTopLine, LeaderDirectionType.RightLeader);
            mls.SetTextAttachmentType(TextAttachmentType.AttachmentBottomOfTopLine, LeaderDirectionType.LeftLeader);
            mls.SetTextAttachmentType(TextAttachmentType.AttachmentBottomOfTopLine, LeaderDirectionType.UnknownLeader);
            mls.SetTextAttachmentType(TextAttachmentType.AttachmentBottomOfTopLine, LeaderDirectionType.TopLeader);

            // Добавляем стиль в базу данных AutoCAD
            ObjectId mLeaderStyleId = mls.PostMLeaderStyleToDb(db, styleName);
            tr.AddNewlyCreatedDBObject(mls, true);

            if (returnStyleObject)
            {
                mLeaderStyleObject = mls;
            }

            return mLeaderStyleId;
        }
        #endregion

        #region Table
        // 3. Основной метод создания стиля таблицы с перегрузками
        public static ObjectId CreateTableStyle(
            Transaction tr,
            Database db,
            string styleName,
            ObjectId textStyleId)
        {
            return CreateTableStyle(tr, db, styleName, textStyleId, false, out _);
        }

        public static ObjectId CreateTableStyle(
            Transaction tr,
            Database db,
            string styleName,
            ObjectId textStyleId,
            bool returnStyleObject,
            out TableStyle tableStyleObject)
        {
            tableStyleObject = null;

            if (tr == null || db == null || string.IsNullOrWhiteSpace(styleName))
                return ObjectId.Null;

            DBDictionary tableStyles = tr.GetObject(db.TableStyleDictionaryId, OpenMode.ForWrite) as DBDictionary;
            if (tableStyles == null)
                return ObjectId.Null;

            // Если стиль уже существует
            if (tableStyles.Contains(styleName))
            {
                ObjectId existingId = tableStyles.GetAt(styleName);
                if (returnStyleObject)
                {
                    tableStyleObject = tr.GetObject(existingId, OpenMode.ForWrite) as TableStyle;
                }
                return existingId;
            }

            // Создаем новый чистый стиль таблицы
            TableStyle ts = new TableStyle();

            // Настраиваем только базовые стандартные группы
            SetDefaultSettings(ts, "Title", textStyleId);
            SetDefaultSettings(ts, "Header", textStyleId);
            SetDefaultSettings(ts, "Data", textStyleId);

            // Специфические высоты шрифтов для иерархии заголовков
            ts.SetTextHeight(3.5, "Title");
            ts.SetTextHeight(3.0, "Header");

            // Сохраняем стиль в словарь базы данных
            ObjectId tableStyleId = tableStyles.SetAt(styleName, ts);
            tr.AddNewlyCreatedDBObject(ts, true);

            if (returnStyleObject)
            {
                tableStyleObject = ts;
            }

            return tableStyleId;
        }
        // 1. Метод для установки дефолтных геометрических параметров стиля ячейки
        private static void SetDefaultSettings(TableStyle ts, string styleName, ObjectId textStyleId)
        {
            if (textStyleId != ObjectId.Null)
            {
                ts.SetTextStyle(textStyleId, styleName);
            }

            ts.SetTextHeight(2.5, styleName);

            // Установка отступов (Margins) 0.5 со всех сторон
            ts.SetMargin(CellMargins.Left | CellMargins.Right, 0.5, styleName);
            ts.SetMargin(CellMargins.Top | CellMargins.Bottom, 0.5, styleName);
        }

        // 2. Метод форматирования ячейки существующей таблицы
        public static void FormatCell(this Cell cell, TableCellDataType cellDataType)
        {
            if (cell == null)
                return;

            switch (cellDataType)
            {
                case TableCellDataType.Text:
                    cell.DataType = new DataTypeParameter(DataType.String, UnitType.Unitless);
                    break;

                case TableCellDataType.Integer:
                    cell.DataType = new DataTypeParameter(DataType.Long, UnitType.Unitless);
                    cell.DataFormat = "%lu2%pr0"; // Форматирование целого числа
                    break;

                case TableCellDataType.Double0:
                    cell.DataType = new DataTypeParameter(DataType.Double, UnitType.Unitless);
                    cell.DataFormat = "%lu2%pr0";
                    break;

                case TableCellDataType.Double1:
                    cell.DataType = new DataTypeParameter(DataType.Double, UnitType.Unitless);
                    cell.DataFormat = "%lu2%pr1";
                    break;

                case TableCellDataType.Double2:
                    cell.DataType = new DataTypeParameter(DataType.Double, UnitType.Unitless);
                    cell.DataFormat = "%lu2%pr2";
                    break;

                case TableCellDataType.Double3:
                    cell.DataType = new DataTypeParameter(DataType.Double, UnitType.Unitless);
                    cell.DataFormat = "%lu2%pr3";
                    break;
            }
        }

        // 1. Перечисление для типов данных в таблице плагина
        public enum TableCellDataType
        {
            Text,      // Обычный текст
            Integer,   // Целое число (без знаков после запятой)
            Double0,   // Вещественное, 0 знаков после запятой (округление до целого)
            Double1,   // Вещественное, 1 знак после запятой
            Double2,   // Вещественное, 2 знака после запятой
            Double3    // Вещественное, 3 знака после запятой
        }
        #endregion
    }
}
