namespace RevitLogProjectLocation.Model
{
    using System;

    /// <summary>
    /// Лог изменения координат
    /// </summary>
    public class LocationLogDto
    {
        /// <summary>
        /// Имя площадки
        /// </summary>
        public string SiteName { get; set; }

        /// <summary>
        /// Файл площадки имя (главный файл активный)
        /// </summary>
        public string ParentFileName { get; set; }

        /// <summary>
        /// Файл площадки полный путь (главный файл активный)
        /// </summary>
        public string ParentFileFullName { get; set; }

        /// <summary>
        /// Файл из которого берем координаты (связь) имя
        /// </summary>
        public string ProviderFileName { get; set; }

        /// <summary>
        /// Координаты базовой точки
        /// </summary>
        public string LocationBase { get; set; }

        /// <summary>
        /// Время последнего изменения
        /// </summary>
        public DateTime ChangedDate { get; set; }

        /// <summary>
        /// Автор изменения
        /// </summary>
        public string Author { get; set; }

        /// <summary>
        /// Автор изменения
        /// </summary>
        public bool Success { get; set; }
    }
}