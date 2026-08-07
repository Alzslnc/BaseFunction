using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace BaseFunction
{
    public class BaseCreateClass
    {
        public static MLeader CreateMLeader(MText mText, Point3d point, Vector3d? direction = null, double xShift = 1, double yShift = 1)
        {
            // Исправляем логику null: теперь значение гарантированно запишется в переменную, если пришел null
            Vector3d actualDirection = direction ?? Vector3d.XAxis;

            MLeader mLeader = new MLeader()
            {
                TextHeight = mText.TextHeight,
                Layer = mText.Layer,
                Color = mText.Color,
                ArrowSize = 0,
                LandingGap = 0,
                ContentType = ContentType.MTextContent
            };

            mLeader.MText = mText;

            // Инициализируем кластер выноски
            int leaderIndex = mLeader.AddLeader();

            // ВАЖНО: сохраняем индекс созданной ЛИНИИ выноски
            int lineIndex = mLeader.AddLeaderLine(leaderIndex);

            // Вершины добавляем именно к ЛИНИИ (lineIndex), а не к кластеру (leaderIndex)
            mLeader.AddFirstVertex(lineIndex, point);
            mLeader.AddLastVertex(lineIndex, Point3d.Origin);

            mLeader.TextLocation = (point + Vector3d.XAxis * xShift + Vector3d.YAxis * yShift);

            mLeader.EnableDogleg = true;

            // Для SetDogleg нужен индекс кластера выноски (leaderIndex) и очищенный от null вектор
            mLeader.SetDogleg(leaderIndex, actualDirection);
            mLeader.DoglegLength = 0;

            // привязка текста
            mLeader.SetTextAttachmentType(TextAttachmentType.AttachmentBottomOfTopLine, LeaderDirectionType.BottomLeader);
            mLeader.SetTextAttachmentType(TextAttachmentType.AttachmentBottomOfTopLine, LeaderDirectionType.RightLeader);
            mLeader.SetTextAttachmentType(TextAttachmentType.AttachmentBottomOfTopLine, LeaderDirectionType.LeftLeader);
            mLeader.SetTextAttachmentType(TextAttachmentType.AttachmentBottomOfTopLine, LeaderDirectionType.TopLeader);

            return mLeader;
        }

    }
}
