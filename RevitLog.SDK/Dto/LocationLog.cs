namespace RevitLogSdk.Dto
{
    using System;

    /// <summary>
    /// Лог изменения координат
    /// </summary>
    public class LocationLogDto : DtoBase
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
        /// Файл из которого берем координаты (связь) имя
        /// </summary>
        public string ProviderFileFullName { get; set; }

        public string ProviderFileFullName { get; set; }

        /// <summary>
        /// Координаты базовой точки
        /// </summary>
        public string LocationBase { get; set; }

        /// <summary>
        /// Время последнего изменения
        /// </summary>
        public DateTime ChangedDate { get; set; }

        /// <summary>
        /// Имя пользователя в Ревите
        /// </summary>
        public string Author { get; set; }

        /// <summary>
        /// Имя пользователя компьютера
        /// </summary>
        public string AccountUser { get; set; }

        /// <summary>
        /// Удачно ли прошло измнение координат в файле
        /// </summary>
        public bool Success { get; set; }
    }
}