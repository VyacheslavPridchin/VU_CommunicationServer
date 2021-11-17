using System;

namespace CustomClasses
{
    [Serializable]
    public class Package
    {
        public enum Types
        {
            WebCam,
            Screen,
            Sketch,
            Audio,
            GreetingDownload,
            GreetingUpload,
            Parting,
            MiniWebcam,
            MiniScreen,
            MiniSketch,
            HoldOn,
            IP,
            unknown,
            SetTypePack
        }


        public Package(bool isGreeting, bool isUpload, string OwnerID, string LessonID)
        {
            ownerID = OwnerID;
            lessonID = LessonID;
            if (isGreeting)
            {
                if (isUpload) 
                    type = Types.GreetingUpload;
                else
                    type = Types.GreetingDownload;
            }
            else type = Types.Parting;
            data = null;
        }

        public Package(Types Type, string OwnerID, string LessonID)
        {
            ownerID = OwnerID;
            lessonID = LessonID;
            type = Type;
            data = null;
        }

        public Package(string OwnerID, string LessonID, Types Type, byte[] Data)
        {
            ownerID = OwnerID;
            lessonID = LessonID;
            type = Type;
            data = Data;
        }

        public string ownerID;

        public string lessonID; //Иногда содержит UniqueKey, иногда ID в SQL таблицы, иногда ID в массиве на сервере. В зависимости от Package.Type

        public Types type = Types.unknown;

        public byte[] data; //Содержит либо partImage, либо partAudio
    }

    [Serializable]
    public class setType
    {
        public Package.Types type;
        public int ownerID;

        public setType(Package.Types TYPE, int OWNERID){
            type = TYPE;
            ownerID = OWNERID;
        }
    }

        [Serializable]
    public class partImage
    {
        public byte[] bitmap;

        public int width, height, left, top, max_x, max_y;

        public partImage(byte[] BMP, int Width, int Height, int Left, int Top, int Max_x, int Max_y)
        {
            bitmap = BMP;
            width = Width;
            height = Height;
            left = Left;
            top = Top;
            max_x = Max_x;
            max_y = Max_y;
        }
    }

    [Serializable]
    public class partAudio
    {
        public bool isTeacher; 
        public byte[] data;
        public partAudio(bool IsTeacher, byte[] Data)
        {
            isTeacher = IsTeacher;
            data = Data;
        }
    }

}
