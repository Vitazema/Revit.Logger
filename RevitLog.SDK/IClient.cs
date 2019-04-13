namespace RevitLog.SDK
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using JetBrains.Annotations;

    /// <summary>
    /// Интерфейс базового клиента
    /// </summary>
    /// <typeparam name="TDto">Тип dto</typeparam>
    [PublicAPI]
    public interface IClient<TDto>
    {
        /// <summary>
        /// Получает заданное количество объектов
        /// </summary>
        /// <param name="skip">Количество которое необходмо пропустить</param>
        /// <param name="limit">Количество которое необходмо взять</param>
        /// <returns></returns>
        Task<IList<TDto>> GetRange(int skip, int limit);

        /// <summary>
        /// Получение по ID
        /// </summary>
        /// <param name="id">ID объекта</param>
        /// <returns></returns>
        Task<TDto> Get(long id);

        /// <summary>
        /// Получение всех объектов
        /// </summary>
        /// <returns></returns>
        Task<IList<TDto>> GetAll();

        /// <summary>
        /// Поиск объекта
        /// </summary>
        /// <param name="findString">Строка поиска</param>
        /// <returns></returns>
        Task<IList<TDto>> Search(string findString);

        /// <summary>
        /// Добавление объекта
        /// </summary>
        /// <param name="dto">Dto</param>
        /// <returns></returns>
        Task<TDto> Add(TDto dto);

        /// <summary>
        /// Удаление объекта
        /// </summary>
        /// <param name="id">ID объекта</param>
        Task Delete(long id);

        /// <summary>
        /// Обновление объекта
        /// </summary>
        /// <param name="updatedDto">Обновленное dto</param>
        /// <returns></returns>
        Task<TDto> Update(TDto updatedDto);
    }
}