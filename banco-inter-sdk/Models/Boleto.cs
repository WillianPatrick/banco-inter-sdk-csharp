using System.Runtime.Serialization;

using Newtonsoft.Json;

namespace SDK.Payments.Models;

[DataContract]
public class Boleto
{
    [JsonProperty("nossoNumero")]
    public string NossoNumero { get; set; }

    [JsonProperty("codigoBarras")]
    public string CodigoBarras { get; set; }

    [JsonProperty("linhaDigitavel")]
    public string LinhaDigitavel { get; set; }
}

