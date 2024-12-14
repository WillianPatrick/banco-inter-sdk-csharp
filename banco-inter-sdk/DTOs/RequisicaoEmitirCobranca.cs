using SDK.Payments.Models;

using System.Runtime.Serialization;

namespace SDK.Payments.DTOs;

[DataContract]
public class RequisicaoEmitirCobranca
{
    [DataMember(Name = "seuNumero", EmitDefaultValue = false)]
    public string SeuNumero;
    [DataMember(Name = "valorNominal", EmitDefaultValue = false)]
    public double ValorNominal;
    [DataMember(Name = "dataVencimento", EmitDefaultValue = false)]
    public string DataVencimento;
    [DataMember(Name = "numDiasAgenda", EmitDefaultValue = true)]
    public int NumDiasAgenda;
    [DataMember(Name = "pagador", EmitDefaultValue = false)]
    public Models.Pessoa Pagador;
    [DataMember(Name = "desconto", EmitDefaultValue = false)]
    public Models.Desconto Desconto;
    [DataMember(Name = "multa", EmitDefaultValue = false)]
    public Models.Multa Multa;
    [DataMember(Name = "mora", EmitDefaultValue = false)]
    public Models.Mora Mora;
    [DataMember(Name = "mensagem", EmitDefaultValue = false)]
    public Models.Mensagem Mensagem;
    [DataMember(Name = "beneficiarioFinal", EmitDefaultValue = false)]
    public Models.Pessoa BeneficiarioFinal;
    [DataMember(Name = "formasRecebimento", EmitDefaultValue = false)]
    public List<string> FormasRecebimento;

}

