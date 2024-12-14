using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Polly;

namespace SDK.Payments;

/// <summary>
/// Classe responsável pela integração com o Banco Inter para operações de boletos, cobranças e webhooks.
/// </summary>
public class BancoInter
{
    private readonly HttpClient _httpClient;
    private readonly Config _config;
    private readonly ILogger _logger;
    private string _authToken;

    /// <summary>
    /// Construtor para inicializar a instância do SDK do Banco Inter.
    /// </summary>
    public BancoInter(Config config, ILogger logger)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _logger.LogInformation("Inicializando BancoInter SDK com URL base: {BaseUrl}", _config.BaseUrl);

        _config.BaseUrl = _config.BaseUrl ?? throw new ArgumentException("BaseUrl inválido. Deve ser a URL completa de 'sandbox' ou 'produção'.");

        var cert = new X509Certificate2(_config.Certificado, _config.Chave);
        var handler = new HttpClientHandler();
        handler.ClientCertificates.Add(cert);

        _httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(_config.BaseUrl)
        };

        _logger.LogDebug("Configurando HttpClient com base URL: {BaseUrl}", _httpClient.BaseAddress);

        _authToken = ObterTokenAsync().Result;
    }

    /// <summary>
    /// Obtém o token de autenticação.
    /// </summary>
    public async Task<string> ObterTokenAsync()
    {
        _logger.LogInformation("Iniciando a obtenção do token de autenticação.");

        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("client_id", _config.ClientId),
            new KeyValuePair<string, string>("client_secret", _config.ClientSecret),
            new KeyValuePair<string, string>("grant_type", "client_credentials"),
            new KeyValuePair<string, string>("scope", "boleto-cobranca.write boleto-cobranca.read")
        });

        _logger.LogDebug("Payload para obtenção do token: {Payload}", await content.ReadAsStringAsync());

        var response = await Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(5, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                (exception, timeSpan, retryCount, context) =>
                    _logger.LogWarning("Retry {RetryCount} para ObterTokenAsync devido a: {Message}", retryCount, exception.Message))
            .ExecuteAsync(() => _httpClient.PostAsync("/oauth/v2/token", content));

        response.EnsureSuccessStatusCode();
        var jsonResponse = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<dynamic>(jsonResponse);

        if (result == null || string.IsNullOrWhiteSpace(result.access_token))
            throw new Exception("Erro ao obter token de autenticação.");

        _authToken = result.access_token;

        _logger.LogInformation("Token de autenticação obtido com sucesso.");
        return _authToken;
    }

    /// <summary>
    /// Emite um boleto bancário.
    /// </summary>
    public async Task<string> EmitirBoletoAsync(Payments.DTOs.RequisicaoEmitirCobranca cobranca)
    {
        _logger.LogInformation("Iniciando a emissão do boleto.");

        if (string.IsNullOrWhiteSpace(cobranca.SeuNumero) || cobranca.ValorNominal <= 0 || string.IsNullOrWhiteSpace(cobranca.DataVencimento))
            throw new ArgumentException("Campos obrigatórios ausentes: 'SeuNumero', 'ValorNominal' e 'DataVencimento'.");

        var jsonBody = JsonConvert.SerializeObject(cobranca);
        _logger.LogDebug("Payload de emissão de boleto: {Payload}", jsonBody);

        var response = await _httpClient.PostAsync("/cobranca/v3/cobrancas", new StringContent(jsonBody, Encoding.UTF8, "application/json"));

        if (!response.IsSuccessStatusCode)
        {
            var erro = await response.Content.ReadAsStringAsync();
            _logger.LogError("Erro ao emitir boleto. Código: {StatusCode}, Detalhe: {Erro}", response.StatusCode, erro);
            throw new Exception($"Erro ao emitir boleto. Código: {response.StatusCode}, Detalhe: {erro}");
        }

        var jsonResponse = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<dynamic>(jsonResponse);

        if (result == null || string.IsNullOrWhiteSpace(result.codigoSolicitacao))
            throw new Exception("Erro ao emitir boleto. Código de solicitação não retornado.");

        _logger.LogInformation($"Boleto emitido com sucesso. Código de solicitação: {result.codigoSolicitacao}");

        return result.codigoSolicitacao;
    }

    /// <summary>
    /// Consulta os dados de uma cobrança.
    /// </summary>
    public async Task<Payments.DTOs.ConsultaCobrancaResponse> ConsultarCobrancaAsync(string codigoSolicitacao)
    {
        _logger.LogInformation("Consultando cobrança para o código {CodigoSolicitacao}.", codigoSolicitacao);

        var response = await _httpClient.GetAsync($"/cobranca/v3/cobrancas/{codigoSolicitacao}");

        if (!response.IsSuccessStatusCode)
        {
            var erro = await response.Content.ReadAsStringAsync();
            _logger.LogError("Erro ao consultar cobrança. Código: {StatusCode}, Detalhe: {Erro}", response.StatusCode, erro);
            throw new Exception($"Erro ao consultar cobrança. Código: {response.StatusCode}, Detalhe: {erro}");
        }

        var jsonResponse = await response.Content.ReadAsStringAsync();
        var consultaResponse = JsonConvert.DeserializeObject<Payments.DTOs.ConsultaCobrancaResponse>(jsonResponse);

        if (consultaResponse == null)
            throw new Exception("Erro ao deserializar a resposta da consulta de cobrança.");

        _logger.LogInformation("Consulta de cobrança bem-sucedida para o código {CodigoSolicitacao}.", codigoSolicitacao);

        return consultaResponse;
    }

    /// <summary>
    /// Obtém o PDF de uma cobrança.
    /// </summary>
    public async Task<string> ObterPdfAsync(string codigoSolicitacao)
    {
        _logger.LogInformation("Obtendo PDF para o código de cobrança {CodigoSolicitacao}.", codigoSolicitacao);

        var response = await _httpClient.GetAsync($"/cobranca/v3/cobrancas/{codigoSolicitacao}/pdf");

        if (!response.IsSuccessStatusCode)
        {
            var erro = await response.Content.ReadAsStringAsync();
            _logger.LogError("Erro ao obter PDF. Código: {StatusCode}, Detalhe: {Erro}", response.StatusCode, erro);
            throw new Exception($"Erro ao obter PDF. Código: {response.StatusCode}, Detalhe: {erro}");
        }

        _logger.LogInformation("PDF obtido com sucesso para o código de cobrança {CodigoSolicitacao}.", codigoSolicitacao);

        return await response.Content.ReadAsStringAsync();
    }

    /// <summary>
    /// Cancela uma cobrança.
    /// </summary>
    public async Task CancelarCobrancaAsync(string codigoSolicitacao, string motivo)
    {
        _logger.LogInformation("Cancelando a cobrança {CodigoSolicitacao} com motivo: {Motivo}.", codigoSolicitacao, motivo);

        var payload = new { motivoCancelamento = motivo };
        var jsonBody = JsonConvert.SerializeObject(payload);

        var response = await _httpClient.PatchAsync($"/cobranca/v3/cobrancas/{codigoSolicitacao}/cancelar", new StringContent(jsonBody, Encoding.UTF8, "application/json"));

        if (!response.IsSuccessStatusCode)
        {
            var erro = await response.Content.ReadAsStringAsync();
            _logger.LogError("Erro ao cancelar cobrança. Código: {StatusCode}, Detalhe: {Erro}", response.StatusCode, erro);
            throw new Exception($"Erro ao cancelar cobrança. Código: {response.StatusCode}, Detalhe: {erro}");
        }

        _logger.LogInformation("Cobrança {CodigoSolicitacao} cancelada com sucesso.", codigoSolicitacao);
    }

    /// <summary>
    /// Cria um webhook para notificações de cobrança.
    /// </summary>
    public async Task CriarWebhookAsync(string urlWebhook)
    {
        _logger.LogInformation("Criando webhook para URL: {UrlWebhook}.", urlWebhook);

        var payload = new { webhookUrl = urlWebhook };

        var response = await _httpClient.PutAsync("/cobranca/v3/cobrancas/webhook", new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json"));

        if (!response.IsSuccessStatusCode)
        {
            var erro = await response.Content.ReadAsStringAsync();
            _logger.LogError("Erro ao criar webhook. Código: {StatusCode}, Detalhe: {Erro}", response.StatusCode, erro);
            throw new Exception($"Erro ao criar webhook. Código: {response.StatusCode}, Detalhe: {erro}");
        }

        _logger.LogInformation("Webhook criado com sucesso.");
    }

    /// <summary>
    /// Exclui o webhook de notificações.
    /// </summary>
    public async Task ExcluirWebhookAsync()
    {
        _logger.LogInformation("Excluindo webhook.");

        var response = await _httpClient.DeleteAsync("/cobranca/v2/webhook");

        if (!response.IsSuccessStatusCode)
        {
            var erro = await response.Content.ReadAsStringAsync();
            _logger.LogError("Erro ao excluir webhook. Código: {StatusCode}, Detalhe: {Erro}", response.StatusCode, erro);
            throw new Exception($"Erro ao excluir webhook. Código: {response.StatusCode}, Detalhe: {erro}");
        }

        _logger.LogInformation("Webhook excluído com sucesso.");
    }
}
