namespace RevitLog.SDK
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading.Tasks;
    using BimLab.Api.Sdk.Lib.Utils;
    using JetBrains.Annotations;
    using RevitLogSdk.Dto;

    /// <summary>
    /// Основной клиент
    /// </summary>
    [PublicAPI]
    public class RevitClient
    {
        private readonly HttpClient _client;

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="baseUrl">Базовый url</param>
        public RevitClient(
            [NotNull] string baseUrl)
            : this(new HttpClient { BaseAddress = new Uri(baseUrl) })
        {
        }

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="httpClient">Http клиент</param>
        public RevitClient(HttpClient httpClient)
        {
            _client = httpClient;
            LocationLog  = new LocationLogClient(_client);
        }

        /// <summary>
        /// Площадка
        /// </summary>
        public LocationLogClient LocationLog { get; set; }

        /// <summary>
        /// Проверка ответа сервера
        /// </summary>
        public async Task<bool> Ping()
        {
            try
            {
                var response = await _client.GetAsync(string.Empty);
                return response.StatusCode != 0;
            }
            catch
            {
                return false;
            }
        }
    }
}