using System.Runtime.Serialization;

using Newtonsoft.Json;

namespace SDK.Payments.Models;

[DataContract]
public class Desconto
{
    [JsonProperty("codigo")]
    public string Codigo { get; set; }

    [JsonProperty("quantidadeDias")]
    public int QuantidadeDias { get; set; }

    [JsonProperty("taxa")]
    public decimal Taxa { get; set; }
}

