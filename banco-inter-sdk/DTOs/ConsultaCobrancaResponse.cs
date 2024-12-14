using System.Runtime.Serialization;

using Newtonsoft.Json;

using SDK.Payments.Models;

namespace SDK.Payments.DTOs;

[DataContract]
public class ConsultaCobrancaResponse
{
    [JsonProperty("cobranca")]
    public Models.Cobranca Cobranca { get; set; }

    [JsonProperty("boleto")]
    public Models.Boleto Boleto { get; set; }

    [JsonProperty("pix")]
    public Models.Pix Pix { get; set; }

}

