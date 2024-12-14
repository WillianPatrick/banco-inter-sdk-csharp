namespace SDK.Payments;

public class Config
{
    public required string ClientId { get; set; }
    public required string ClientSecret { get; set; }
    public required string Certificado { get; set; }
    public required string Chave { get; set; }
    public required string BaseUrl { get; set; }
    public string? ContaCorrente { get; set; }
    public string? WebhookUrl { get; set; }
}

