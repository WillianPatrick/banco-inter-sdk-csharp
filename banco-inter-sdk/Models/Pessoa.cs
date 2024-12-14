using System.Runtime.Serialization;

namespace SDK.Payments.Models;

[DataContract]
public class Pessoa
{
    [DataMember(Name = "cpfCnpj", EmitDefaultValue = false)]
    public string CpfCnpj;

    [DataMember(Name = "tipoPessoa", EmitDefaultValue = false)]
    public string TipoPessoa;

    [DataMember(Name = "nome", EmitDefaultValue = false)]
    public string Nome;

    [DataMember(Name = "endereco", EmitDefaultValue = false)]
    public string Endereco;
    [DataMember(Name = "numero", EmitDefaultValue = false)]
    public string Numero;
    [DataMember(Name = "complemento", EmitDefaultValue = false)]
    public string Complemento;
    [DataMember(Name = "bairro", EmitDefaultValue = false)]
    public string Bairro;

    [DataMember(Name = "cidade", EmitDefaultValue = false)]
    public string Cidade;

    [DataMember(Name = "uf", EmitDefaultValue = false)]
    public string Uf;

    [DataMember(Name = "cep", EmitDefaultValue = false)]
    public string Cep;
    [DataMember(Name = "email", EmitDefaultValue = false)]
    public string Email;
    [DataMember(Name = "ddd", EmitDefaultValue = false)]
    public string Ddd;
    [DataMember(Name = "telefone", EmitDefaultValue = false)]
    public string Telefone;
}

