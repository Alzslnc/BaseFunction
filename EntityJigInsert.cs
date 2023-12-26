using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using static BaseFunction.BaseBlockReferenceClass;

namespace BaseFunction
{
    public static class EntityJigInsert
    {       
        /// <summary>
        /// вставляет объект в выбранное пользователем место в чертеже
        /// </summary>
        /// <param name="ent"></param>
        /// <returns></returns>
        public static bool EntityInsert(this Entity ent)
        {
            if (!ent.IsNewObject) return false;
            //трай на всякий случай вдруг кто-то применит к уже добавленному в базу данных объекту
            //хотя по идее можно сделать проверку на наличие ObjectId, мысля на будущее
            try
            {
                //блоки вставляем не так как другие объекты, так как предврительно надо добавить его в базу данных и вставить атрибуты
                if (ent is BlockReference)
                {
                    //открываем транзакцию
                    using (Transaction tr = HostApplicationServices.WorkingDatabase.TransactionManager.StartTransaction())
                    {
                        //открываем текущее пространство на запись
                        using (BlockTableRecord ms = tr.GetObject(HostApplicationServices.WorkingDatabase.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord)
                        {
                            //вставляем блок
                            ms.AppendEntity(ent);
                            tr.AddNewlyCreatedDBObject(ent, true);
                            //вставляем атрибуты
                            BlockReferenceSetAttribute(ent.Id);
                            //запускаем джиг
                            DrawJigClassEntityInsertAndReplace ijc = new DrawJigClassEntityInsertAndReplace();
                            //если джиг вернул ок то коммитим транзакцию если нет то откатываем
                            PromptResult pr = ijc.Drow(ref ent, null, true);
                            if (pr != null && pr.Status == PromptStatus.OK)
                            {
                                tr.Commit();
                                return true;
                            }
                            else
                            {
                                tr.Abort();
                                return false;
                            }
                        }
                    }
                }
                else
                {
                    //запускаем джиг
                    DrawJigClassEntityInsertAndReplace ijc = new DrawJigClassEntityInsertAndReplace();
                    //если ожиг вернул ок то добавляем объект в базу данных
                    PromptResult pr = ijc.Drow(ref ent, null, true);
                    if (pr != null && pr.Status == PromptStatus.OK)
                    {
                        //открываем транзакцию
                        using (Transaction tr = HostApplicationServices.WorkingDatabase.TransactionManager.StartTransaction())
                        {
                            //открываем текущее пространство на запись
                            using (BlockTableRecord ms = tr.GetObject(HostApplicationServices.WorkingDatabase.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord)
                            {
                                //добавляем объект в базу данных 
                                ms.AppendEntity(ent);
                                tr.AddNewlyCreatedDBObject(ent, true);
                            }
                            tr.Commit();
                        }
                        return true;
                    }
                }
            }
            catch { }
            return false;
        }
        /// <summary>
        /// переносит выбранный объект в чертеже
        /// </summary>
        /// <param name="ent"></param>
        /// <returns></returns>
        public static bool EntytyReplace(this Entity ent)
        {
            try
            {
                //копируем объект, двигаться будет копия
                Entity e = ent.Clone() as Entity;
                //точка объекта, для реплейса
                Point3d point1;
                //вообще не помню зачем тут фолс, может быть был еще один какой-то тип, ни на что не влияет
                if (false) { }
                else if (e is MText mt) { point1 = mt.Location; }
                else if (e is DBText dt)
                {
                    if (dt.Justify == AttachmentPoint.BaseLeft) point1 = dt.Position;
                    else point1 = dt.AlignmentPoint;
                }
                else if (e is Table tb) { point1 = tb.Position; }
                else if (e is Circle cr) { point1 = cr.Center; }
                else if (e is Curve cu) { point1 = cu.StartPoint; }
                else if (e is MLeader ml) { point1 = ml.GetFirstVertex(0); }
                else if (e is BlockReference br) { point1 = br.Position; }
                //если это не один из вышевыбранных типор то удаляем клон и возвращаем фолс
                else { e.Dispose(); return false; }
                //запускаем перенос
                DrawJigClassEntityInsertAndReplace ijc = new DrawJigClassEntityInsertAndReplace();
                //если возврат ОК то переносим базовый объект на место клона
                PromptResult pr = ijc.Drow(ref e, point1, true);
                if (pr != null && pr.Status == PromptStatus.OK)
                {
                    //получаем координаты точки клона соответствующие точке базового объекта
                    Point3d point2 = new Point3d();
                    if (e is MText mt) { point2 = mt.Location; }
                    else if (e is DBText dt)
                    {
                        if (dt.Justify == AttachmentPoint.BaseLeft) point2 = dt.Position;
                        else point2 = dt.AlignmentPoint;
                    }
                    else if (e is Table tb) { point2 = tb.Position; }
                    else if (e is Circle cr) { point2 = cr.Center; }
                    else if (e is Curve cu) { point2 = cu.StartPoint; }
                    else if (e is MLeader ml) { point2 = ml.GetFirstVertex(0); }
                    else if (e is BlockReference br) { point2 = br.Position; }
                    //удаляем клон
                    e.Dispose();
                    //переносим объект 
                    //переменная для определения открыт ли объект на чтение
                    bool read = false;
                    if (ent.IsReadEnabled)
                    {
                        //открываем на запись
                        read = true;
                        ent.UpgradeOpen();
                    }
                    //переносим объект на выбранное место
                    ent.TransformBy(Matrix3d.Displacement(point2 - point1));
                    if (read)
                    {
                        //закрываем на запись
                        ent.DowngradeOpen();
                    }
                    return true;
                }
                e.Dispose();
            }
            catch
            { }
            return false;
        }
        private class DrawJigClassEntityInsertAndReplace : DrawJig
        {
            readonly Editor ed = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Editor;
            private Point3d _visiblePoint = new Point3d(); //точка получаемая от поинт монитора или самплера, не используется в чистом виде так как может слетать если включены привязки и курсор стоит на месте
            private Point3d _computedPoint = new Point3d(); //точка передаваемая поинт монитором
            private Point3d _currentPoint = new Point3d(); //окончательный вариант передаваемой точки
            private Point3d _savePoint = new Point3d(); //сохраненное значение точки для получения статуса noChange, для обхода отваливающейся привязки
            //тип выбранного объекта
            private enum EntType : int
            {
                None = 0,
                mText = 1,
                dbText = 2,
                table = 3,
                curve = 4,
                mLeader = 5,
                blockReference = 6,
                circle = 7,
            }
            //точка изначального объекта при перемещении, от нее будет рисоваться нитка 
            Point3d? _point1 = null;
            //тип объекта
            EntType eType = EntType.None;
            //объекты
            MText _mt = null;
            DBText _dbtext = null;
            Table _table = null;
            Curve _curve = null;
            MLeader _mleader = null;
            BlockReference _blockReference = null;
            Circle _circle = null;
            //переменная для использования PointMonitor, нужна если требуелся привязка к объектам во время вставки
            private bool _pm = false;
            //основная функция, вызываемая для вставки/перемещение объекта
            public PromptResult Drow(
                //объект
                ref Entity e,
                //точка базового объекта для отрисовки нитки
                Point3d? point1,
                //использовать ли поинт монитор
                bool pm
                )
            {
                //получаем точку для нитки если она задана
                if (point1 != null) _point1 = point1;
                //получаем ти п объекта
                if (e is MText) { _mt = e as MText; eType = EntType.mText; }
                else if (e is DBText) { _dbtext = e as DBText; eType = EntType.dbText; }
                else if (e is Table) { _table = e as Table; eType = EntType.table; }
                else if (e is Circle) { _circle = e as Circle; eType = EntType.circle; }
                else if (e is Curve) { _curve = e as Curve; eType = EntType.curve; }
                else if (e is MLeader) { _mleader = e as MLeader; eType = EntType.mLeader; }
                else if (e is BlockReference) { _blockReference = e as BlockReference; eType = EntType.blockReference; }
                //если используем поинт монитор то включаем его
                _pm = pm;
                if (_pm) ed.PointMonitor += new PointMonitorEventHandler(ed_PointMonitor);
                //переменная с результатом
                PromptResult jigRes = null;
                //если выбран неподдерживаемый тип то возвращаем null
                if (eType == EntType.None) return jigRes;
                //конкретно тут цикл не нужен, но если трубуется сделать несколько действий то будет использоваться
                while (true)
                {
                    //получаем результат от самплера
                    jigRes = ed.Drag(this);
                    return jigRes;
                }
            }
            //поинт монитор, если требуется привязка во время перемещения объекта, иначе объект будет на курсоре
            //а не в точке привязки пока не будет выбрано место вставки, объект будет вставлен в точку привязки
            //независимо от того, включен поинт монитор или нет
            void ed_PointMonitor(object sender, PointMonitorEventArgs e)
            {
                if (e.Context.PointComputed && (e.Context.History & PointHistoryBits.ObjectSnapped) > 0)
                    _computedPoint = e.Context.ObjectSnappedPoint;
                else
                    _computedPoint = e.Context.ComputedPoint;
            }
            //хз как работает, но походу отвечает за получение координат во время движения курсора, и возвращает результат клика или отмены
            protected override SamplerStatus Sampler(JigPrompts prompts)
            {
                //опции работают так же как и обычный Prompt, можно настроить на получение keywords тут не применяется
                JigPromptPointOptions jigOpts = new JigPromptPointOptions();
                jigOpts.Message = "\nВыберите место вставки: ";
                jigOpts.UserInputControls =
                        UserInputControls.Accept3dCoordinates |
                        UserInputControls.NoZeroResponseAccepted;
                //получаем результат от джига
                PromptPointResult jigRes = prompts.AcquirePoint(jigOpts);
                //получаем положение точки от джига
                Point3d pt = jigRes.Value;
                //если работаем через поинт монитор то результат видимой точки берем из поинт понитора
                //если нет то из результатов джига
                if (_pm) _visiblePoint = _computedPoint;
                else _visiblePoint = pt;
                //если сохраненная точка равна текущей, полученной от джига то возвращаем отсутствие изменений
                if (_savePoint.IsEqualTo(pt)) return SamplerStatus.NoChange;
                //перезаписываем сохраненную точку
                _savePoint = pt;
                //устанавливаем актуальную точку
                _currentPoint = _visiblePoint;
                //возвращаем статус
                if (jigRes.Status == PromptStatus.OK) return SamplerStatus.OK;
                return SamplerStatus.Cancel;
            }
            //тут содержится перерисовка объектов
            protected override bool WorldDraw(Autodesk.AutoCAD.GraphicsInterface.WorldDraw draw)
            {
                //если точка была задана рисуем нитку от точки, до курсора или точки привязки
                if (_point1.HasValue) draw.Geometry.WorldLine(_point1.Value, _currentPoint);
                //перемещаем объект соответствующим методом в зависимости от типа
                switch (eType)
                {
                    case EntType.mText:
                        {
                            //перемещаем объект
                            _mt.TransformBy(Matrix3d.Displacement(_currentPoint - _mt.Location));
                            //отрисовываем объект
                            draw.Geometry.Draw(_mt);
                            break;
                        }
                    case EntType.dbText:
                        {
                            if (_dbtext.Justify == AttachmentPoint.BaseLeft) _dbtext.TransformBy(Matrix3d.Displacement(_currentPoint - _dbtext.Position));
                            else _dbtext.TransformBy(Matrix3d.Displacement(_currentPoint - _dbtext.AlignmentPoint));
                            draw.Geometry.Draw(_dbtext);
                            break;
                        }
                    case EntType.table:
                        {
                            _table.TransformBy(Matrix3d.Displacement(_currentPoint - _table.Position));
                            draw.Geometry.Draw(_table);
                            break;
                        }
                    case EntType.curve:
                        {
                            _curve.TransformBy(Matrix3d.Displacement(_currentPoint - _curve.StartPoint));
                            draw.Geometry.Draw(_curve);
                            break;
                        }
                    case EntType.mLeader:
                        {
                            if (_mleader.GetFirstVertex(0) != null) _mleader.TransformBy(Matrix3d.Displacement(_currentPoint - _mleader.GetFirstVertex(0)));
                            draw.Geometry.Draw(_mleader);
                            break;
                        }
                    case EntType.blockReference:
                        {
                            _blockReference.TransformBy(Matrix3d.Displacement(_currentPoint - _blockReference.Position));
                            draw.Geometry.Draw(_blockReference);
                            break;
                        }
                    case EntType.circle:
                        {
                            _circle.TransformBy(Matrix3d.Displacement(_currentPoint - _circle.Center));
                            draw.Geometry.Draw(_circle);
                            break;
                        }
                }
                return true;
            }
        }
    }
}
