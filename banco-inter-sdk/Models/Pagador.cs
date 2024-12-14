using System.Runtime.Serialization;

using Newtonsoft.Json;

namespace SDK.Payments.Models;

[DataContract]
public class Pagador
{
    [JsonProperty("cpfCnpj")]
    public string CpfCnpj { get; set; }

    [JsonProperty("tipoPessoa")]
    public string TipoPessoa { get; set; }

    [JsonProperty("nome")]
    public string Nome { get; set; }

    [JsonProperty("endereco")]
    public string Endereco { get; set; }

    [JsonProperty("bairro")]
    public string Bairro { get; set; }

    [JsonProperty("cidade")]
    public string Cidade { get; set; }

    [JsonProperty("uf")]
    public string Uf { get; set; }

    [JsonProperty("cep")]
    public string Cep { get; set; }

    [JsonProperty("email")]
    public string Email { get; set; }

    [JsonProperty("numero")]
    public string Numero { get; set; }

    [JsonProperty("complemento")]
    public string Complemento { get; set; }
}

