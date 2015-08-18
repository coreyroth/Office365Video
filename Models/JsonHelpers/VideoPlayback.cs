using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Office365Video.Models.JsonHelpers
{
    public class VideoPlaybackData
    {
        [JsonProperty(PropertyName = "d")]
        public VideoPlayback Data { get; set; }
    }

    public class VideoPlayback
    {
        [JsonProperty(PropertyName = "odata.netadata")]
        public string Metadata { get; set; }

        [JsonProperty(PropertyName = "GetPlaybackUrl")]
        public string Value {get; set;}
    }
}
