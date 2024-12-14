using System.Runtime.Serialization;

using Newtonsoft.Json;

namespace SDK.Payments.Models;

[DataContract]
public class Pix
{
    [JsonProperty("txid")]
    public string Txid { get; set; }

    [JsonProperty("pixCopiaECola")]
    public string PixCopiaECola { get; set; }
}

