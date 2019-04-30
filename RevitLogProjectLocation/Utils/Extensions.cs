namespace RevitLogProjectLocation
{
    using System;
    using Autodesk.Revit.DB;
    using Logger;

    /// <summary>
    /// Расширения для работы с Revit элементами
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Получение координаты XYZ в строке
        /// </summary>
        /// <param name="bPoint"></param>
        /// <returns></returns>
        public static string ToXyzStr(this BasePoint bPoint)
        {
            if (bPoint == null)
                return string.Empty;

            var xyz = new Position(Math.Round(bPoint.get_Parameter(BuiltInParameter.BASEPOINT_EASTWEST_PARAM).AsDouble().ToMillimeters(), 4),
                Math.Round(bPoint.get_Parameter(BuiltInParameter.BASEPOINT_NORTHSOUTH_PARAM).AsDouble().ToMillimeters(), 4),
                Math.Round(bPoint.get_Parameter(BuiltInParameter.BASEPOINT_ELEVATION_PARAM).AsDouble().ToMillimeters(), 4),
                Math.Round(bPoint.get_Parameter(BuiltInParameter.BASEPOINT_ANGLETON_PARAM).AsDouble().ToDegrees(), 2));

            return xyz.GetRoundedValuesAsString();
        }
        
        /// <summary>
        /// Взятие базовой точки проекта из документа
        /// </summary>
        /// <param name="doc"></param>
        /// <returns></returns>
        public static BasePoint GetProjectBasePoint(this Document doc)
        {
            var locationPoints = new FilteredElementCollector(doc)
                .OfClass(typeof(BasePoint))
                .ToElements();
            foreach (var locationPoint in locationPoints) {
                if (locationPoint is BasePoint bp && !bp.IsShared) {
                    return bp;
                }
            }

            return null;
        }

        /// <summary>
        /// Отсечение замыкающих нулей
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static double RemoveTrailingZeros(this double value)
        {
            return value / 1.0000;
        }

        public static double ToMillimeters(this double a)
        {
            return UnitUtils.ConvertFromInternalUnits(a, DisplayUnitType.DUT_MILLIMETERS);
        }

        public static double ToDegrees(this double a)
        {
            return UnitUtils.ConvertFromInternalUnits(a, DisplayUnitType.DUT_DECIMAL_DEGREES);
        }
        
        /// <summary>
        /// Убирает маркер имени пользователя в имени файла, если файл Ревита открыт как локальный
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="username"></param>
        /// <returns></returns>
        public static string RemoveUserLocalFileMark(this string fileName, string username)
        {
            if (fileName.ToUpper().EndsWith(username.ToUpper()))
            {
                return fileName.Remove(fileName.Length - username.Length - 1);
            }

            return fileName;
        }

        /// <summary>
        /// Убирает четыре знака в расширение файла, если таковое имеется. Работает с .dwg или .rvt
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static string RemoveFileExtension(this string fileName)
        {
            return fileName.EndsWith(".rvt")
                   || fileName.EndsWith(".dwg")
                ? fileName.Remove(fileName.Length - 4) : fileName;
        }

        /// <summary>
        /// Находит полный путь к файлу, вне зависимости от сценария использования типа открытия модели в Ревит
        /// </summary>
        /// <param name="doc">Revit Document</param>
        /// <returns></returns>
        public static string GetDocFullPath(this Document doc)
        {
            if (doc == null)
                return "Document is null";

            if (doc.IsDetached)
            {
                return "Отсоединено";
            }

            if (doc.IsWorkshared)
            {
                if (doc.GetWorksharingCentralModelPath() is ModelPath modelPath)
                {
                    return ModelPathUtils.ConvertModelPathToUserVisiblePath(modelPath);
                }
            }
            else
            {
                return doc.PathName != string.Empty ? doc.PathName : "Не сохранено";
            }

            return "Не определено";
        }
    }
}