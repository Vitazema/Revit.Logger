namespace RevitLog.SDK
{
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading.Tasks;
    using BimLab.Api.Sdk.Lib.Utils;
    using RevitLogSdk.Dto;

    /// <summary>
    /// Базовый клиент
    /// </summary>
    /// <typeparam name="TDto">Тип dto</typeparam>
    public abstract class BaseClient<TDto> : IClient<TDto>
        where TDto : DtoBase, new()
    {
        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="client">RestClient</param>
        protected BaseClient(HttpClient client)
        {
            Client = client;
        }

        /// <summary>
        /// Url для работы с заданным типом объектов
        /// </summary>
        protected abstract string Url { get; set; }

        /// <summary>
        /// RestClient
        /// </summary>
        protected HttpClient Client { get; set; }

        /// <inheritdoc />
        public async Task<IList<TDto>> GetRange(int skip, int limit)
        {
            var query = new { skip, limit }.ToQueryString();
            var response = await Client.GetAsync($"{Url}{query}");
            await response.HandleErrorAsync();
            return await response.GetDataAsync<List<TDto>>();
        }

        /// <inheritdoc />
        public async Task<TDto> Get(long id)
        {
            return await GetAsync<TDto>($"{Url}/{id}");
        }

        /// <inheritdoc />
        public async Task<IList<TDto>> GetAll()
        {
            return await GetRange(-1, -1);
        }

        /// <inheritdoc />
        public async Task<IList<TDto>> Search(string findString)
        {
            var query = new { query = findString }.ToQueryString();
            var response = await Client.GetAsync($"{Url}/search{query}");
            await response.HandleErrorAsync();
            return await response.GetDataAsync<List<TDto>>();
        }

        /// <inheritdoc />
        public async Task<TDto> Add(TDto dto)
        {
            var response = await Client.PostAsync(Url, dto.ToStringContent());
            await response.HandleErrorAsync();
            return await response.GetDataAsync<TDto>();
        }

        /// <inheritdoc />
        public async Task Delete(long id)
        {
            var response = await Client.DeleteAsync($"{Url}/{id}");
            await response.HandleErrorAsync();
        }

        /// <inheritdoc />
        public async Task<TDto> Update(TDto dto)
        {
            var response = await Client.PutAsync($"{Url}/{dto.Id}", dto.ToStringContent());
            await response.HandleErrorAsync();
            return await response.GetDataAsync<TDto>();
        }

        /// <summary>
        /// Sends a GET request to the specified uri.
        /// </summary>
        /// <typeparam name="T">Тип объекта в ответе</typeparam>
        /// <param name="uri">URI</param>
        protected async Task<T> GetAsync<T>(string uri)
            where T : new()
        {
            var response = await Client.GetAsync(uri);
            await response.HandleErrorAsync();
            return await response.GetDataAsync<T>();
        }
    }
}