namespace RevitLogProjectLocation
{
    using Autodesk.Revit.DB;

    /// <summary>
    /// Расширения для работы с Revit элементами
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Получение координаты XYZ в строке
        /// </summary>
        /// <param name="point">Location Revit</param>
        /// <returns></returns>
        public static string ToXyzStr(this Location point)
        {
            if (!(point is LocationPoint locPoint))
                return string.Empty;
            var point3D = locPoint.Point;
            return point3D.X + ";" + point3D.Y + ";" + point3D.Z;
        }
    }
}