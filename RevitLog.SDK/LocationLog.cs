namespace RevitLog.SDK
{
    using System.Net.Http;
    using RevitLogSdk.Dto;

    public class LocationLogClient : BaseClient<LocationLogDto>
    {
        /// <inheritdoc />
        public LocationLogClient(HttpClient client)
            : base(client)
        {
        }

        /// <inheritdoc />
        protected override string Url { get; set; } = "Revit/v1/LocationLog";

    }
}