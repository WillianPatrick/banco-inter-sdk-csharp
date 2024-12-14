using System.Runtime.Serialization;

using Newtonsoft.Json;

namespace SDK.Payments.Models;

[DataContract]
public class Cobranca
{
    [JsonProperty("seuNumero")]
    public string SeuNumero { get; set; }

    [JsonProperty("dataEmissao")]
    public DateTime DataEmissao { get; set; }

    [JsonProperty("dataVencimento")]
    public DateTime DataVencimento { get; set; }

    [JsonProperty("valorNominal")]
    public decimal ValorNominal { get; set; }

    [JsonProperty("tipoCobranca")]
    public string TipoCobranca { get; set; }

    [JsonProperty("situacao")]
    public string Situacao { get; set; }

    [JsonProperty("dataSituacao")]
    public DateTime DataSituacao { get; set; }

    [JsonProperty("arquivada")]
    public bool Arquivada { get; set; }

    [JsonProperty("descontos")]
    public List<Desconto> Descontos { get; set; }

    [JsonProperty("multa")]
    public Multa Multa { get; set; }

    [JsonProperty("mora")]
    public Mora Mora { get; set; }

    [JsonProperty("pagador")]
    public Pagador Pagador { get; set; }

    [JsonProperty("valorTotalRecebido")]
    public double ValorTotalRecebido { get; set; }
}

