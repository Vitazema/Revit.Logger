namespace RevitLogProjectLocation
{
    using System;
    using System.Security.RightsManagement;
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

        public static double Normalize(this double value)
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
        /// Находит полный путь к файлу, вне зависимости от сценария использования типа открытия модели в Ревит
        /// </summary>
        /// <param name="doc">Revit Document</param>
        /// <returns></returns>
        public static string GetDocFullPath(this Document doc)
        {
            string parentFileFullName;

            var modelPath = doc.GetWorksharingCentralModelPath();

            if (doc.IsDetached)
            {
                parentFileFullName = "Отсоединено:" + ModelPathUtils.ConvertModelPathToUserVisiblePath(modelPath);
            }
            else if (modelPath != null)
            {
                parentFileFullName = ModelPathUtils.ConvertModelPathToUserVisiblePath(modelPath);
            }
            else
            {
                parentFileFullName = doc.PathName != string.Empty ? doc.PathName : "Не сохранено";
            }

            return parentFileFullName;
        }
    }
}