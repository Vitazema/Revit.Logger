namespace RevitLogProjectLocation.Logger
{
    using System.Globalization;
    using System.Linq;

    /// <summary>
    /// Класс обработки координат в читабельный вид для передачи в БД
    /// </summary>
    public class Position
    {
        public Position(double x, double y, double z, double angle)
        {
            X = x;
            Y = y;
            Z = z;
            Angle = angle;
        }

        /// <summary>
        /// Координата X
        /// </summary>
        public double X { get; set; }

        /// <summary>
        /// Координата Y
        /// </summary>
        public double Y { get; set; }

        /// <summary>
        /// Координата Z
        /// </summary>
        public double Z { get; set; }

        /// <summary>
        /// Угол поворота координатных осей
        /// </summary>
        public double Angle { get; set; }
        
        /// <summary>
        /// Причёсывание отображения координат в строчном виде для передачи в БД
        /// </summary>
        /// <returns></returns>
        public string GetRoundedValuesAsString()
        {
            var v = new double[] { X, Y, Z, Angle }.Select(x => x.RemoveTrailingZeros().ToString(CultureInfo.InvariantCulture)).ToArray();
            var res = string.Join(";", v);
            return res;
        }
    }
}