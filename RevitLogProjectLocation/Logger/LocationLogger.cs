namespace RevitLogProjectLocation
{
    using System;
    using System.Net.Http;
    using System.Threading.Tasks;
    using RevitLog.SDK;
    using RevitLogSdk.Dto;

    public class LocationLogger
    {
        private const string _url = "http://vpp-sql04.main.picompany.ru/liramonitor/";
        private bool _isActive;

        public LocationLogger()
        {
            try
            {
                RevitSdkClient = new RevitClient(new HttpClient()
                {
                    // http://localhost:5000
                    // http://vpp-sql04/liramonitor/
                    BaseAddress = new Uri("http://vpp-sql04/liramonitor/"),
                    Timeout = TimeSpan.FromMinutes(1)
                });
                PingServer();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        /// <summary>
        /// Сервер активен
        /// </summary>
        public bool IsActive => _isActive;

        private static RevitClient RevitSdkClient { get; set; }

        /// <summary>
        /// Проверяет подключение к серверу
        /// </summary>
        public async void PingServer()
        {
            try
            {
                _isActive = await RevitSdkClient.Ping();
            }
            catch (Exception e)
            {
            }
        }

        /// <summary>
        /// Добавляет запись в БД. Если вернуло false, запись не произошла
        /// </summary>
        /// <param name="log">Лог</param>
        /// <returns></returns>
        public async Task<bool> WriteLog(LocationLogDto log)
        {
            try
            {
                await RevitSdkClient.LocationLog.Add(log);
                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }
    }
}