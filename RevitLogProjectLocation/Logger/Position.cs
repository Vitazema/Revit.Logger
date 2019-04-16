namespace RevitLogProjectLocation.Logger
{
    using System.Globalization;
    using System.Linq;
    using System.Windows.Documents;

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

        public string GetRoundedValuesAsString()
        {
            var v = new double[] { X, Y, Z, Angle }.Select(x => x.Normalize().ToString(CultureInfo.InvariantCulture)).ToArray();
            var res = string.Join(";", v);
            return res;
        }
    }
}