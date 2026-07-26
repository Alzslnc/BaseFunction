using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaseFunction
{
    internal class Store
    {
        public void ToUnite1()
        {
            StartEvents startEvents = new StartEvents();

            #region фасад + проекции
            startEvents.Buttons.Add(new Button("mCommand", "Проекции",
                new List<ButtonCommand> { new ButtonCommand("Fasad_Create", "Фасад", "Разворачивает фасад вдоль выбранного котнутра"), }));
            startEvents.Buttons.Add(new Button("mCommand", "Проекции",
                new List<ButtonCommand> { new ButtonCommand("CilinderOnPlane", "По Прямой", "Проецирует точки и тексты на плоскость относительно выбранной оси"), }));
            startEvents.Buttons.Add(new Button("mCommand", "Проекции",
                new List<ButtonCommand> { new ButtonCommand("RadialElementOnPlane", "Вертикально", "Проецирует объекты (точки, линии, полилинии) на плоскость относительно вертикальной оси"), }));
            startEvents.Buttons.Add(new Button("mCommand", "Проекции",
                new List<ButtonCommand> { new ButtonCommand("ProjectAlongCurve", "По кривой", "Проецирует объекты (точки, линии, полилинии) на плоскость вдоль выбранной кривой"), }));


            #endregion

            #region Числа

            startEvents.Buttons.Add(new Button("mCommand", "Числа и текст",
                new List<ButtonCommand> { new ButtonCommand("Fasad_Dev_Invert", "Инвертировать", "Меняет знак выбранных чисел"), }));
            startEvents.Buttons.Add(new Button("mCommand", "Числа и текст",
                new List<ButtonCommand> { new ButtonCommand("Fasad_Tolerance_View", "Сверхдопуск", "Перекрашивает числа больше заданных(по модулю) в выбранный цвет"), }));
            startEvents.Buttons.Add(new Button("mCommand", "Числа и текст",
                new List<ButtonCommand> { new ButtonCommand("NumInt", "Нумерация", "Записывает в выбранные тексты(мтексты) нумерацию с выбранного значения"), }));
            startEvents.Buttons.Add(new Button("mCommand", "Числа и текст",
                new List<ButtonCommand> { new ButtonCommand("TextClearFormat", "Очистить", "Удаляет форматирование из выбранных текстов и мультивыносок"), }));

            #endregion

            #region размеры
            startEvents.Buttons.Add(new Button("mCommand", "Размеры",
                new List<ButtonCommand> { new ButtonCommand("DimensionOnCurve", "На кривые", "Проставляет размеры на линии/полилинии/3д полилинии и диаметры на круги/дуги"), }));
            startEvents.Buttons.Add(new Button("mCommand", "Размеры",
                new List<ButtonCommand> { new ButtonCommand("LineToLinesDimension", "До контуров", "Проставляет размеры между выбранными объектами"), }));
            startEvents.Buttons.Add(new Button("mCommand", "Размеры",
                new List<ButtonCommand> { new ButtonCommand("DimensionIntoCurves", "Внутри кривых", "Проставляет размеры, ограниченные ближайшими кривыми."), }));
            startEvents.Buttons.Add(new Button("mCommand", "Размеры",
                new List<ButtonCommand> { new ButtonCommand("AddRandomDimension", "Рандомно", "Добавляет случайный параметр к выбранным размерам"), }));
            startEvents.Buttons.Add(new Button("mCommand", "Размеры",
                new List<ButtonCommand> { new ButtonCommand("AddDimensionPrefixSuffix", "Дополнить", "Добавляет префикс и суффикс выбранной строке"), }));
            startEvents.Buttons.Add(new Button("mCommand", "Размеры",
                new List<ButtonCommand> { new ButtonCommand("DeviationDimension", "Отклонения", "Проставляет размеры до осей и прописывает проектные/фактические данные"), }));
            startEvents.Buttons.Add(new Button("mCommand", "Размеры",
                new List<ButtonCommand> { new ButtonCommand("ExtremumDimension", "Экстремумы", "Показывает минимальное и максимальное расстояние между кривыми"), }));
            startEvents.Buttons.Add(new Button("mCommand", "Размеры",
                new List<ButtonCommand> { new ButtonCommand("DimensionProgramSettings", "Параметры", "Настройки команд раздела."), }));
            #endregion

            #region мультивыноски
            //startEvents.Buttons.Add(new Button("mCommand", "Мультивыноски",
            //    new List<ButtonCommand> { new ButtonCommand("MLeaderCoordinateCreate", "Координаты", "Создает мультивыноски в выбранных точках или в выбранном месте"), }));
            startEvents.Buttons.Add(new Button("mCommand", "Мультивыноски",
                new List<ButtonCommand> { new ButtonCommand("MLeaderRaplace", "Расставить", "Расставляет текст мультивыносок вдоль выбранного направления"), }));
            startEvents.Buttons.Add(new Button("mCommand", "Мультивыноски",
                new List<ButtonCommand> { new ButtonCommand("MleaderZOtk", "Отклонение", "Проставляет высотное отклонение в виде мультивыноски в вертикальной плоскости автокада"), }));
            startEvents.Buttons.Add(new Button("mCommand", "Мультивыноски",
                new List<ButtonCommand> { new ButtonCommand("TextToMleader", "Вставить текст", "Создает мультивыноску в месте выбранного текста и вставлеет текст в нее"), }));
            startEvents.Buttons.Add(new Button("mCommand", "Мультивыноски",
                new List<ButtonCommand> { new ButtonCommand("BlockReferenceToMLeader", "Вставить блок", "Создает мультивыноску в месте выбранного блока и вставлеет блок в нее"), }));
            startEvents.Buttons.Add(new Button("mCommand", "Мультивыноски",
                new List<ButtonCommand> { new ButtonCommand("MleaderTextColorFromLeader", "Изменить цвет", "Изменяет цвет объекта мультивыноски на цвет мультивыноски"), }));

            #endregion

            #region объекты
            startEvents.Buttons.Add(new Button("mCommand", "Объекты",
              new List<ButtonCommand> { new ButtonCommand("CopyObject", "Скопировать", "Копирует выбранные объекты опционально в выбранный слой или с выбранным цветом"), }));
            startEvents.Buttons.Add(new Button("mCommand", "Объекты",
              new List<ButtonCommand> { new ButtonCommand("ChangeObjectColor", "Перекрасить", "Перекрашивает выбранные объекты в выбранный цвет"), }));
            startEvents.Buttons.Add(new Button("mCommand", "Объекты",
                new List<ButtonCommand> { new ButtonCommand("ObjetcToView", "По виду", "Разворачивает объекты по текущему виду (тексты, точки, блоки)"), }));
            startEvents.Buttons.Add(new Button("mCommand", "Объекты",
                new List<ButtonCommand> { new ButtonCommand("ReplaceEntity", "Заменить", "Заменяет различные объекты (блоки, точки, круги, тексты, мтексты) на другие"), }));
            startEvents.Buttons.Add(new Button("mCommand", "Объекты",
                new List<ButtonCommand> { new ButtonCommand("ExDictionaryRemove", "Удалить словари", "Удаляет словари из выбранных объектов чертежа"), }));
            startEvents.Buttons.Add(new Button("mCommand", "Объекты",
                new List<ButtonCommand> { new ButtonCommand("PointOnText", "Точки на текст", "Создает точки в месте положения текста с отметкой прописанной в тексте."), }));
            startEvents.Buttons.Add(new Button("mCommand", "Объекты",
                new List<ButtonCommand> { new ButtonCommand("DistributeObjects", "Распределить", "Перекрашивает тексты и точки рядом с ними в разные цвета в зависимости от текста в месте расположения точек"), }));
            startEvents.Buttons.Add(new Button("mCommand", "Объекты",
                new List<ButtonCommand> { new ButtonCommand("PointExportToTxt", "Экспорт с названием", "Экспортирует коордианты точек с названием взятом из ближайшего в 10 метрах текста в текстовый файл на рабочем столе"), }));


            #endregion

            #region штриховки
            startEvents.Buttons.Add(new Button("mCommand", "Штриховки",
              new List<ButtonCommand> { new ButtonCommand("CreateHatchContours", "Создать контур", "Восстанавливает контура выбранныъ штриховок"), }));
            #endregion

            #region 
            startEvents.Buttons.Add(new Button("mCommand", "Рандом",
              new List<ButtonCommand> { new ButtonCommand("Random001", "Рандом", "Устанавливает случайные значения для выбранных текстов, мультивыносок. Устанавливает случайные значения для координат выбранных объектов"), }));
            #endregion

            #region разное
            startEvents.Buttons.Add(new Button("mCommand", "Разное",
                new List<ButtonCommand> { new ButtonCommand("UscFollowED", "UscFollow", "Изменяет UscFollow во всех видовых экранах"), }));
            startEvents.Buttons.Add(new Button("mCommand", "Разное",
                new List<ButtonCommand> { new ButtonCommand("ObjectToLayout", "В лист", "Копирует в лист выбранные обыъекты, отображаемые на выбранных видовых экранах"), }));
            startEvents.Buttons.Add(new Button("mCommand", "Разное",
                new List<ButtonCommand> { new ButtonCommand("LayerPlot", "Включить печать", "Включает печатаемость всех слои в чертеже кроме defpoints, может рабоать как с текущим чертежом так и с любыми выбранными"), }));
            startEvents.Buttons.Add(new Button("mCommand", "Разное",
             new List<ButtonCommand> { new ButtonCommand("CenterFromCircles", "Центр по кругам", "Показывает среднюю точку из выбранной группы кругов, вес круга определяется радиусом"), }));
            startEvents.Buttons.Add(new Button("mCommand", "Разное",
             new List<ButtonCommand> { new ButtonCommand("CenterFromPoints", "Центр по точкам", "Показывает среднюю точки из выбранной группы точек"), }));
            startEvents.Buttons.Add(new Button("mCommand", "Разное",
             new List<ButtonCommand> { new ButtonCommand("Solid3dConnect", "Соединить тела", "Соединяет тела отрезая меньшие части"), }));
            #endregion

            #region
            startEvents.Buttons.Add(new Button("mCommand", "Блоки",
                new List<ButtonCommand> { new ButtonCommand("ColorChange", "Формат", "Изменяет цвет, толщину линий или слой объектов в блоках"), }));
            startEvents.Buttons.Add(new Button("mCommand", "Блоки",
                new List<ButtonCommand>
                {
                    new ButtonCommand("BlockRefShow", "Пометить блоки", "Обводит кругом блоки с выбранным значением аттрибута или очищает чертеж от этих пометок"),
                }));
            startEvents.Buttons.Add(new Button("mCommand", "Блоки",
                new List<ButtonCommand>
                {
                    new ButtonCommand("TextMarkShow", "Пометить тексты", "Помечает найденные тексты обводкой зеленым кругом"),
                    new ButtonCommand("TextMarkRemove", "Удалить метки ", "Удаляет метки текстов"),
                }));
            startEvents.Buttons.Add(new Button("mCommand", "Блоки",
                new List<ButtonCommand> { new ButtonCommand("BlockClone", "Копировать", "Копирует описание выбранного блока, в отличии от встроенной функции корректно копирует динамические блоки"), }));
            startEvents.Buttons.Add(new Button("mCommand", "Блоки",
                new List<ButtonCommand> { new ButtonCommand("BlockRename", "Переименовать", "Пеоеименовывает описание выбранного блока"), }));
            startEvents.Buttons.Add(new Button("mCommand", "Блоки",
                new List<ButtonCommand> { new ButtonCommand("AlignOnCurves", "Развернуть", "Разворачивает выбранные блоки(тексты, мтексты) вдоль выбранных кривых на выбранном от кривых расстоянии (если не выбирать то разворачивает все блоки(тесты, мтексты) в чертеже, если они удовлетворяют условиям расположения)"), }));
            startEvents.Buttons.Add(new Button("mCommand", "Блоки",
                new List<ButtonCommand>
                {
                    new ButtonCommand("WeedObjects", "Проредить", "Прореживает выбранные объекты (блоки, точки, текст, круги)"),
                    new ButtonCommand("WeedObjectsSettings", "Настройки", "Настройки прореживания объектов"),
                }));
            startEvents.Buttons.Add(new Button("mCommand", "Блоки",
                new List<ButtonCommand> { new ButtonCommand("TextToAttribute", "Текст в атрибут", "Заменяет выбранный атрибут значением текста, найденного в выбранной области"), }));
            startEvents.Buttons.Add(new Button("mCommand", "Блоки", new List<ButtonCommand>
                {
                    //кнопка
                    new ButtonCommand("ExplodeBlock", "Взорвать блоки",
                    "Взрывает блоки сохраняя текстовые атрибуты, опционально помещает результат в исходный слой блока. Так же команда находится в контекстном меню по ПКМ если выбраны блоки."),
                    new ButtonCommand("ExplodeObjects", "Взорвать объекты",
                    "Взрывает объекты сохраняя текстовые атрибуты, опционально помещает результат в исходный слой блока. Так же команда находится в контекстном меню по ПКМ если выбраны объекты для взрывания."),
                    new ButtonCommand("ExplodeSettings", "Параметры",
                    "Параметры расчленения объектов"),
                }
               ));
            #endregion

            #region
            startEvents.Buttons.Add(new Button("mCommand", "Таблицы",
              new List<ButtonCommand> { new ButtonCommand("TableColorChange", "Изменить", "Меняет цвет всех текстов на выбранный, опционально меняет высоту шрифта всех текстов, может обрабатывать как выбранные в чертеже таблицы так и все таблицы в выбранных чертежах"), }));
            startEvents.Buttons.Add(new Button("mCommand", "Таблицы",
                new List<ButtonCommand> { new ButtonCommand("ClearNumbers", "Очистить", "Если в ячейке несколько числовых значений то удаляет все кроме первого"), }));
            startEvents.Buttons.Add(new Button("mCommand", "Таблицы",
                new List<ButtonCommand> { new ButtonCommand("DeleteRowColumn", "Удалить", "Удаляет выбранные строки или столбцы в выбранных таблицах"), }));
            startEvents.Buttons.Add(new Button("mCommand", "Таблицы",
                new List<ButtonCommand> { new ButtonCommand("TableClearFormat", "Очистить формат", "Удаляет все форматирование всех текстов в выбранных таблицах"), }));
            startEvents.Buttons.Add(new Button("mCommand", "Таблицы",
                new List<ButtonCommand>
                {
                    new ButtonCommand("MultiplyTable", "Умножение", "Умножает значения выбранных ячеек на выбранное число"),
                    new ButtonCommand("DivideTable", "Деление", "Делит значения выбранных ячеек на выбранное число"),
                    new ButtonCommand("PlusTable", "Сложение", "Складывае значения выбранных ячеек с выбранным числом"),
                    new ButtonCommand("MinusTable", "Вычитание", "Вычитает из значения выбранных ячеек выбранное число"),
                }));
            #endregion

            #region  
            startEvents.Buttons.Add(new Button("mCommand", "Кривые",
               new List<ButtonCommand>
               {
                   new ButtonCommand("PlineExtendCreate", "Полилиния", "Создает полилинию с возможностью включения других объектов или их частей."),
                   new ButtonCommand("PlineExtendSettings", "Параметры", "Содержит параметры создания полилинии."),
               }));
            startEvents.Buttons.Add(new Button("mCommand", "Кривые",
               new List<ButtonCommand> { new ButtonCommand("WeedPolyline", "Проредить", "Упрощает полилинию или 3д полилинию, удаляет точки, не являющиеся точками поворота."), }));
            startEvents.Buttons.Add(new Button("mCommand", "Кривые",
              new List<ButtonCommand>
              {
                   new ButtonCommand("ExtendPLine", "Продолжить", "Дает возможность продолжить полилинию в ее текущем направлении или под определенным углом."),
                   new ButtonCommand("ExtendPLineSettings", "Параметры", "Содержит параметры продолжения полилинии."),
              }));
            startEvents.Buttons.Add(new Button("mCommand", "Кривые",
                new List<ButtonCommand>
                {
                    new ButtonCommand("CurveBreak", "Разрыв", "Добавляет разрыв в выбранном месте и опционально заполняет его элементами"),
                    new ButtonCommand("CurveBreakSettings", "Параметры", "Настройки команды"),
                }));
            startEvents.Buttons.Add(new Button("mCommand", "Кривые",
              new List<ButtonCommand> { new ButtonCommand("LinesToVertices", "Соединить", "Создает линии между точками выбранных кривых (линий, полилиний, 3д полилиний) и выбранным местом"), }));
            startEvents.Buttons.Add(new Button("mCommand", "Кривые",
              new List<ButtonCommand> { new ButtonCommand("MassOffset", "Смещение", "Смещает выбранные кривые"), }));
            startEvents.Buttons.Add(new Button("mCommand", "Кривые",
                new List<ButtonCommand> { new ButtonCommand("PointOnIntersections", "Пересечения(точки)", "Создает точки в местах пересечения выбранных кривых"), }));
            startEvents.Buttons.Add(new Button("mCommand", "Кривые",
               new List<ButtonCommand> { new ButtonCommand("ContourIntersect", "Пересечения(регион)", "Создает объект (region) в местах пересечения выбранных замкнутых полилиний"), }));
            startEvents.Buttons.Add(new Button("mCommand", "Кривые",
               new List<ButtonCommand> { new ButtonCommand("Slope_Create", "Штризовка откоса", "Штриховку откоса между выбранными кривыми"), }));
            startEvents.Buttons.Add(new Button("mCommand", "Кривые",
             new List<ButtonCommand> { new ButtonCommand("CreateLineSlope", "Уклон линии", "Показывает уклон линии в %"), }));
            startEvents.Buttons.Add(new Button("mCommand", "Кривые",
             new List<ButtonCommand> { new ButtonCommand("PlineInsertPoint", "Вставить точку", "Вставляет точку в выбранном месте или на пересечении с выбранными кривыми"), }));

            #endregion 
        }

        #region MiniProgram
        [CommandMethod("PlineInsertPoint", CommandFlags.Modal | CommandFlags.UsePickSet | CommandFlags.Redraw)]
        public static void PlineInsertPoint()
        {
            new CurveClass().PlineInsertPoint();
        }
        [CommandMethod("CreateLineSlope", CommandFlags.Modal | CommandFlags.UsePickSet | CommandFlags.Redraw)]
        public static void CreateLineSlope()
        {
            new LineDataClass().LineSlope();
        }
        [CommandMethod("Fasad_Create")]
        public static void FasadStart()
        {
            new FasadClass().CreateStart();
        }
        [CommandMethod("Fasad_Dev_Invert", CommandFlags.Modal | CommandFlags.UsePickSet | CommandFlags.Redraw)]
        public static void InvertStart()
        {
            new FasadClass().InvertStart();
        }
        [CommandMethod("Fasad_Tolerance_View", CommandFlags.Modal | CommandFlags.UsePickSet | CommandFlags.Redraw)]
        public static void ToleranceStart()
        {
            new ToleranceClass().ToleranceStart();
        }
        [CommandMethod("CilinderOnPlane")]
        public static void CilinderOnPlane()
        {
            new CylinderOnPlaneClass().Start();
        }
        [CommandMethod("MLeaderRaplace")]
        public static void MLeaderRaplace()
        {
            new MLeaderReplaceClass().Start();
        }
        [CommandMethod("RadialElementOnPlane")]
        public static void RadialElementOnPlane()
        {
            new RadialElementOnPlaneClass().Start();
        }
        [CommandMethod("ProjectAlongCurve")]
        public static void ProjectAlongCurve()
        {
            new ProjectAlongCurveClass().Start();
        }
        [CommandMethod("ColorChange")]
        public static void ColorChange()
        {
            new ColorChangeClass().Start();
        }
        [CommandMethod("UscFollowED")]
        public static void UscFollowED()
        {
            new ColorChangeClass().Start2();
        }
        [CommandMethod("MleaderZOtk")]
        public static void MleaderZOtk()
        {
            new MLeaderInPlaneClass().Start();
        }
        [CommandMethod("BlockRefShow")]
        public static void BlockRefShow()
        {
            new BlockRefShowClass().Start();
        }
        [CommandMethod("TextMarkShow")]
        public static void TextMarkShow()
        {
            new BlockRefShowClass().TextStart();
        }
        [CommandMethod("TextMarkRemove")]
        public static void TextMarkRemove()
        {
            new BlockRefShowClass().DeleteTextMark();
        }
        [CommandMethod("ObjectToLayout")]
        public static void ObjectToLayout()
        {
            new ObjectToLayoutClass().Start();
        }
        [CommandMethod("TableColorChange")]
        public static void TableColorChange()
        {
            new TableColorChange().Start();
        }
        [CommandMethod("BlockClone")]
        public static void BlockClone()
        {
            new BlockCloneClass().BlockClone();
        }
        [CommandMethod("BlockRename")]
        public static void BlockRename()
        {
            new BlockCloneClass().BlockRename();
        }
        [CommandMethod("ClearNumbers")]
        public static void ClearNumbers()
        {
            new ClearNumbersClass().ClearNumbers();
        }
        [CommandMethod("DeleteRowColumn")]
        public static void DeleteRowColumn()
        {
            new DeleteRowColumnClass().Start();
        }
        [CommandMethod("NumInt", CommandFlags.Modal | CommandFlags.UsePickSet | CommandFlags.Redraw)]
        public static void NumInt()
        {
            new NumClass().NumIntStart();
        }
        [CommandMethod("LayerPlot")]
        public static void LayerPlot()
        {
            new LayerPlotClass().Start();
        }
        [CommandMethod("PointOnIntersections", CommandFlags.UsePickSet | CommandFlags.Modal | CommandFlags.Redraw)]
        public static void PointOnIntersections()
        {
            new PointOnIntersections().Start();
        }
        [CommandMethod("TableClearFormat")]
        public static void TableClearFormat()
        {
            new ClearFormatClass().ClearTable();
        }
        [CommandMethod("TextClearFormat", CommandFlags.Modal | CommandFlags.UsePickSet | CommandFlags.Redraw)]
        public static void TextClearFormat()
        {
            new ClearFormatClass().ClearText();
        }
        [CommandMethod("ContourIntersect", CommandFlags.UsePickSet | CommandFlags.Modal | CommandFlags.Redraw)]
        public static void ContourIntersect()
        {
            new ContourIntersectClass().Start();
        }
        [CommandMethod("AlignOnCurves", CommandFlags.UsePickSet | CommandFlags.Modal | CommandFlags.Redraw)]
        public static void AlignOnCurves()
        {
            new AlignOnCurvesClass().Start();
        }
        [CommandMethod("WeedObjects", CommandFlags.UsePickSet | CommandFlags.Modal | CommandFlags.Redraw)]
        public static void WeedObjects()
        {
            new WeedObjectsClass().Start();
        }
        [CommandMethod("WeedObjectsSettings")]
        public static void WeedObjectsSettings()
        {
            new WeedObjectsClass().FormStart();
        }
        [CommandMethod("ReplaceEntity", CommandFlags.UsePickSet | CommandFlags.Modal | CommandFlags.Redraw)]
        public static void ReplaceEntity()
        {
            new ReplaceEntityClass().Start();
        }
        [CommandMethod("CurveBreak")]
        public static void CurveBreak()
        {
            new CurveBreakClass().Start();
        }
        [CommandMethod("CurveBreakSettings")]
        public static void CurveBreakSettings()
        {
            new CurveBreakClass().StartSettings();
        }
        [CommandMethod("ExDictionaryRemove", CommandFlags.UsePickSet | CommandFlags.Modal | CommandFlags.Redraw)]
        public static void ExDictionaryRemove()
        {
            new ExDictionaryRemoveClass().EraseDictionary();
        }
        [CommandMethod("PointOnText", CommandFlags.UsePickSet | CommandFlags.Modal | CommandFlags.Redraw)]
        public static void PointOnText()
        {
            PointOnTextClass.Start();
        }
        [CommandMethod("DistributeObjects", CommandFlags.UsePickSet | CommandFlags.Modal | CommandFlags.Redraw)]
        public static void DistributeObjects()
        {
            new DistributeObjectsClass().Start();
        }
        [CommandMethod("PlineExtendCreate")]
        public static void CreatePolyObject()
        {
            new CreateCurveObjectClass().CCO();
        }
        [CommandMethod("PlineExtendSettings")]
        public static void PlineExtendSettings()
        {
            new CreateCurveObjectClass().Settings();
        }
        [CommandMethod("WeedPolyline", CommandFlags.UsePickSet | CommandFlags.Modal | CommandFlags.Redraw)]
        public static void WeedPolyline()
        {
            new WeedPolylineClass().Start();
        }
        [CommandMethod("MultiplyTable")]
        public static void MultiplyTable()
        {
            new TableCalculatorClass().Multiply();
        }
        [CommandMethod("DivideTable")]
        public static void DivideTable()
        {
            new TableCalculatorClass().Divide();
        }
        [CommandMethod("PlusTable")]
        public static void PlusTable()
        {
            new TableCalculatorClass().Plus();
        }
        [CommandMethod("MinusTable")]
        public static void MinusTable()
        {
            new TableCalculatorClass().Minus();
        }

        [CommandMethod("ExtendPLine", CommandFlags.Modal | CommandFlags.UsePickSet | CommandFlags.Redraw)]
        public static void ExtendPLine()
        {
            new ExtendPLineClass().Start();
        }
        [CommandMethod("ExtendPLineSettings", CommandFlags.Modal | CommandFlags.UsePickSet | CommandFlags.Redraw)]
        public static void ExtendPLineSettings()
        {
            new FormsFolder.ExtendPlineSettings().ShowDialog();
        }
        [CommandMethod("LinesToVertices", CommandFlags.Modal | CommandFlags.UsePickSet | CommandFlags.Redraw)]
        public static void LinesToVertices()
        {
            new LinesToVerticesClass().Start();
        }
        [CommandMethod("TextToAttribute", CommandFlags.Modal | CommandFlags.UsePickSet | CommandFlags.Redraw)]
        public static void TextToAttribute()
        {
            new TextToAttributeClass().Start();
        }
        [CommandMethod("ObjetcToView", CommandFlags.Modal | CommandFlags.UsePickSet | CommandFlags.Redraw)]
        public static void ObjetcToView()
        {
            new ObjetcToViewClass().Start();
        }
        [CommandMethod("MergeHatch", CommandFlags.Modal | CommandFlags.UsePickSet | CommandFlags.Redraw)]
        public static void MergeHatch()
        {
            new MergeHatchClass().Start();
        }
        [CommandMethod("MassOffset", CommandFlags.Modal | CommandFlags.UsePickSet | CommandFlags.Redraw)]
        public static void MassOffset()
        {
            new MassOffsetClass().Start();
        }
        [CommandMethod("CreateHatchContours", CommandFlags.Modal | CommandFlags.UsePickSet | CommandFlags.Redraw)]
        public static void CreateHatchContours()
        {
            new CreateHatchContoursClass().Start();
        }
        [CommandMethod("CopyObject")]
        public static void CopyObject()
        {
            new CopyObjectClass().Start();
        }
        [CommandMethod("ChangeObjectColor")]
        public static void ChangeObjectColor()
        {
            new ChangeObjectColorClass().Start();
        }

        [CommandMethod("TextToMleader", CommandFlags.Modal | CommandFlags.UsePickSet | CommandFlags.Redraw)]
        public static void TextToMleader()
        {
            new TextToMleaderClass().Start();
        }
        [CommandMethod("CenterFromCircles", CommandFlags.Modal | CommandFlags.UsePickSet | CommandFlags.Redraw)]
        public static void CenterFromCircles()
        {
            new CenterPointProgramClass().CenterFromCircles();
        }
        [CommandMethod("CenterFromPoints", CommandFlags.Modal | CommandFlags.UsePickSet | CommandFlags.Redraw)]
        public static void CenterFromPoints()
        {
            new CenterPointProgramClass().CenterFromPoints();
        }
        [CommandMethod("bufferInsertMtext")]
        public static void InsertMtext()
        {
            BufferInsertClass.BufferInsertMtext();
        }
        [CommandMethod("bufferInsertTable")]
        public static void InsertTable()
        {
            BufferInsertClass.BufferInsertTable();
        }
        [CommandMethod("BlockReferenceToMLeader")]
        public static void BlockReferenceToMLeader()
        {
            new BlockReferenceToMLeaderClass().Start();
        }
        [CommandMethod("Solid3dConnect", CommandFlags.Modal | CommandFlags.UsePickSet | CommandFlags.Redraw)]
        public static void Solid3dConnect()
        {
            new Solid3dClass().Solid3dConnect();
        }
        [CommandMethod("MleaderTextColorFromLeader", CommandFlags.Modal | CommandFlags.UsePickSet | CommandFlags.Redraw)]
        public static void MleaderTextColorFromLeader()
        {
            new MleaderCreateClass().ColorUpdate();
        }
        [CommandMethod("PointExportToTxt", CommandFlags.Modal | CommandFlags.UsePickSet | CommandFlags.Redraw)]
        public static void PointExportToTxt()
        {
            new PointExportClass().Export();
        }
        [CommandMethod("Slope_Create")]
        public static void Slope_Create()
        {
            new MiniProgram.Program.SlopeCreateClass().Create();
        }
        #endregion

        #region Random001
        [CommandMethod("Random001", CommandFlags.Modal | CommandFlags.UsePickSet | CommandFlags.Redraw)]
        public static void Random001()
        {
            Autodesk.AutoCAD.ApplicationServices.Application.ShowModalWindow(new Program.Random001.View.Random001View());
        }
        #endregion

        #region DimensionProgram
        [CommandMethod("DimensionOnCurve", CommandFlags.UsePickSet | CommandFlags.Modal | CommandFlags.Redraw)]
        public void DimensionOnCurveCommand()
        {
            new DimensionOnCurveClass().Start();
        }
        [CommandMethod("LineToLinesDimension")]
        public void LineToLinesDimensionCommand()
        {
            new LineToLinesDimensionClass().Start();
        }
        [CommandMethod("DimensionIntoCurves")]
        public void DimensionIntoCurvesCommand()
        {
            new DimensionIntoCurvesClass().Start();
        }
        [CommandMethod("AddRandomDimension", CommandFlags.UsePickSet | CommandFlags.Modal | CommandFlags.Redraw)]
        public void AddRandomDimensionCommand()
        {
            new AddRandomDimensionClass().Start();
        }
        [CommandMethod("AddDimensionPrefixSuffix", CommandFlags.UsePickSet | CommandFlags.Modal | CommandFlags.Redraw)]
        public void AddDimensionPrefixSuffix()
        {
            new AddRandomDimensionClass().AddPrefixSuffix();
        }
        [CommandMethod("DeviationDimension")]
        public void DeviationDimension()
        {
            new DeviationDimensionClass().Start();
        }
        [CommandMethod("ExtremumDimension")]
        public void ExtremumDimension()
        {
            new DimBetweenCurvesClass().ExtremumDimension();
        }
        [CommandMethod("DimensionProgramSettings")]
        public void DimensionProgramSettings()
        {
            Autodesk.AutoCAD.ApplicationServices.Application.ShowModalWindow(new MiniProgram.Program.DimensionProgram.View.DimensionProgramSettingsView());
        }
        #endregion
    }
}
