using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Batch_Barcode_Scanner
{
    public partial class Configuration
    {
        [JsonProperty("UpdateConfiguration")]
        public List<UpdateConfiguration> UpdateConfiguration { get; set; }
    }

    public partial class UpdateConfiguration
    {
        [JsonProperty("UploadEndPoint")]
        public Uri UploadEndPoint { get; set; }

        [JsonProperty("RegexEndPoint")]
        public Uri RegexEndPoint { get; set; }

        [JsonProperty("ApplicationKey")]
        public string ApplicationKey { get; set; }

        [JsonProperty("RetentionPeriod")]
        public long RetentionPeriod { get; set; }

    }
}