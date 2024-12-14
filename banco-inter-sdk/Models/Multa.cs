using System.Runtime.Serialization;

using Newtonsoft.Json;

namespace SDK.Payments.Models;

[DataContract]
public class Multa
{
    [JsonProperty("codigo")]
    public string Codigo { get; set; }

    [JsonProperty("taxa")]
    public decimal Taxa { get; set; }
}

