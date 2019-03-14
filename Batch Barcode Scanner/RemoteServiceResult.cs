namespace Batch_Barcode_Scanner
{
    using Newtonsoft.Json;
    using System;

    public partial class RemoteServiceResult
    {
        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("attempt")]
        public Guid Attempt { get; set; }

        [JsonProperty("id")]
        public Guid Id { get; set; }

        [JsonProperty("request_id")]
        public Guid RequestId { get; set; }
    }
}
