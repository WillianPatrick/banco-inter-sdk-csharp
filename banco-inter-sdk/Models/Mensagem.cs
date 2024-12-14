using System.Runtime.Serialization;

namespace SDK.Payments.Models;

[DataContract]
public class Mensagem
{
    [DataMember(Name = "linha1", EmitDefaultValue = false)]
    public string Linha1;
    [DataMember(Name = "linha2", EmitDefaultValue = false)]
    public string Linha2;
    [DataMember(Name = "linha3", EmitDefaultValue = false)]
    public string Linha3;
    [DataMember(Name = "linha4", EmitDefaultValue = false)]
    public string Linha4;
    [DataMember(Name = "linha5", EmitDefaultValue = false)]
    public string Linha5;
}

