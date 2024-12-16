using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Polly;

using SDK.Payments.DTOs;
using SDK.Payments.Models;

using static System.Formats.Asn1.AsnWriter;

namespace SDK.Payments;

/// <summary>
/// Classe responsável pela integração com o Banco Inter para operações de boletos, cobranças e webhooks.
/// </summary>
public class BancoInter
{
    private readonly HttpClient _httpClient;
    private readonly Config _config;
    private readonly ILogger _logger;
    private List<Jwt> _authTokens;

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
    }

    /// <summary>
    /// Obtém o token de autenticação.
    /// </summary>
    public async Task<string> ObterTokenAsync(string[] scopes)
    {
        _logger.LogInformation("Iniciando a obtenção do token de autenticação.");

        if (_authTokens != null)
        {
            var existingToken = _authTokens.FirstOrDefault(t => t.ExpiresOn > DateTime.UtcNow && t.Scopes.Intersect(scopes).Any());
            if (existingToken != null)
            {
                _logger.LogInformation("Token de autenticação obtido com sucesso.");
                return existingToken.Token;
            }
        }

        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("client_id", _config.ClientId),
            new KeyValuePair<string, string>("client_secret", _config.ClientSecret),
            new KeyValuePair<string, string>("grant_type", "client_credentials"),
            new KeyValuePair<string, string>("scope", scopes != null ? string.Join(" ", scopes) : "boleto-cobranca.write boleto-cobranca.read"),
        });

        if (!string.IsNullOrWhiteSpace((string)_config.ContaCorrente))
            content.Headers.Add("x-inter-conta-corrente", _config.ContaCorrente);

        _logger.LogDebug("Payload para obtenção do token: {Payload}", await content.ReadAsStringAsync());

        var response = await Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(5, retryAttempt => TimeSpan.FromSeconds(Math.Pow(1, retryAttempt)),
                (exception, timeSpan, retryCount, context) =>
                    _logger.LogWarning("Retry {RetryCount} para ObterTokenAsync devido a: {Message}", retryCount, exception.Message))
            .ExecuteAsync(async () => await _httpClient.PostAsync("/oauth/v2/token", content));

        if (!response.IsSuccessStatusCode)
            throw new Exception($"Erro ao obter token de autenticação. Código: {response.StatusCode}");

        response.EnsureSuccessStatusCode();
        var jsonResponse = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<dynamic>(jsonResponse);

        if (result == null || string.IsNullOrWhiteSpace((string)result.access_token))
            throw new Exception("Erro ao obter token de autenticação.");


        _authTokens = _authTokens ?? new List<Jwt>();

        _authTokens.Add(new Jwt
        {
            Token = result.access_token,
            ExpiresOn = DateTime.UtcNow.AddSeconds((int)result.expires_in),
            Scopes = scopes
        });

        _logger.LogInformation("Token de autenticação obtido com sucesso.");
        return result.access_token;
    }

    /// <summary>
    /// Emite um boleto bancário.
    /// </summary>
    public async Task<string> EmitirBoletoAsync(Payments.DTOs.RequisicaoEmitirCobranca cobranca)
    {
        if (string.IsNullOrWhiteSpace(cobranca.SeuNumero) || cobranca.ValorNominal <= 0 || string.IsNullOrWhiteSpace(cobranca.DataVencimento))
            throw new ArgumentException("Campos obrigatórios ausentes: 'SeuNumero', 'ValorNominal' e 'DataVencimento'.");

        cobranca.FormasRecebimento = new List<string> { "PIX", "BOLETO" };

        var requestMessage = new HttpRequestMessage(HttpMethod.Post, "/cobranca/v3/cobrancas");
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await ObterTokenAsync(new[] { "boleto-cobranca.write" }));

        if (!string.IsNullOrWhiteSpace((string)_config.ContaCorrente))
            requestMessage.Headers.Add("x-conta-corrente", _config.ContaCorrente);

        var jsonBody = JsonConvert.SerializeObject(cobranca);
        requestMessage.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

        var response = await Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                retryCount: 5,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(1, retryAttempt)),
                onRetry: (exception, timeSpan, retryCount, context) =>
                {
                    _logger.LogWarning($"Retry {retryCount} for EmitirBoletoAsync due to: {exception.Message}");
                })
            .ExecuteAsync(async () => await _httpClient.SendAsync(requestMessage));

        if (!response.IsSuccessStatusCode)
        {
            var erro = await response.Content.ReadAsStringAsync();
            throw new Exception($"Erro ao emitir boleto. Código: {response.StatusCode}, Detalhe: {erro}");
        }

        var jsonResponse = await response.Content.ReadAsStringAsync();

        if(string.IsNullOrWhiteSpace(jsonResponse))
            throw new Exception("Erro ao emitir boleto. Resposta vazia.");

        var result = JsonConvert.DeserializeObject<dynamic>(jsonResponse);

        return result.codigoSolicitacao;
    }

    /// <summary>
    /// Consulta os dados de uma cobrança.
    /// </summary>
    public async Task<Payments.DTOs.ConsultaCobrancaResponse> ConsultarCobrancaAsync(string codigoSolicitacao)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"/cobranca/v3/cobrancas/{codigoSolicitacao}");
        if (!string.IsNullOrWhiteSpace((string)_config.ContaCorrente))
            request.Headers.Add("x-conta-corrente", _config.ContaCorrente);

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await ObterTokenAsync(new[] { "boleto-cobranca.read" }));

        var response = await Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                retryCount: 5,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(1, retryAttempt)),
                onRetry: (exception, timeSpan, retryCount, context) =>
                {
                    _logger.LogWarning($"Retry {retryCount} for ConsultarCobrancaAsync due to: {exception.Message}");
                })
            .ExecuteAsync(async () => await _httpClient.SendAsync(request));

        if (!response.IsSuccessStatusCode)
        {
            var erro = await response.Content.ReadAsStringAsync();
            throw new Exception($"Erro ao consultar cobrança. Código: {response.StatusCode}, Detalhe: {erro}");
        }

        var jsonResponse = await response.Content.ReadAsStringAsync();

        if(string.IsNullOrWhiteSpace(jsonResponse))
            throw new Exception("Erro ao consultar cobrança. Resposta vazia.");

        var consultaResponse = JsonConvert.DeserializeObject<ConsultaCobrancaResponse>(jsonResponse);

        return consultaResponse;
    }

    /// <summary>
    /// Obtém o PDF de uma cobrança.
    /// </summary>
    public async Task<string> ObterPdfAsync(string codigoSolicitacao)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"/cobranca/v3/cobrancas/{codigoSolicitacao}/pdf");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await ObterTokenAsync(new[] { "boleto-cobranca.read" }));
        if (!string.IsNullOrWhiteSpace((string)_config.ContaCorrente))
            request.Headers.Add("x-conta-corrente", _config.ContaCorrente);

        var response = await Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                retryCount: 5,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(1, retryAttempt)),
                onRetry: (exception, timeSpan, retryCount, context) =>
                {
                    Console.WriteLine($"Retry {retryCount} for ObterPdfAsync due to: {exception.Message}");
                })
            .ExecuteAsync(async () => await _httpClient.SendAsync(request));

        if (!response.IsSuccessStatusCode)
        {
            var erro = await response.Content.ReadAsStringAsync();
            throw new Exception($"Erro ao obter PDF. Código: {response.StatusCode}, Detalhe: {erro}");
        }
        return await response.Content.ReadAsStringAsync();
    }

    /// <summary>
    /// Cancela uma cobrança.
    /// </summary>
    public async Task CancelarCobrancaAsync(string codigoSolicitacao, string motivo)
    {
        var requestMessage = new HttpRequestMessage(HttpMethod.Patch, $"/cobranca/v3/cobrancas/{codigoSolicitacao}/cancelar");
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await ObterTokenAsync(new[] { "boleto-cobranca.write" }));
        if (!string.IsNullOrWhiteSpace((string)_config.ContaCorrente))
            requestMessage.Headers.Add("x-conta-corrente", _config.ContaCorrente);

        var payload = new { motivoCancelamento = motivo };
        var jsonBody = JsonConvert.SerializeObject(payload);
        requestMessage.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

        var response = await Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(5, retryAttempt => TimeSpan.FromSeconds(Math.Pow(1, retryAttempt)), onRetry: (exception, timeSpan, retryCount, context) =>
            {
                _logger.LogWarning($"Retry {retryCount} for CancelarCobrancaAsync due to: {exception.Message}");
            })
            .ExecuteAsync(async () => await _httpClient.SendAsync(requestMessage));

        if (!response.IsSuccessStatusCode)
        {
            var erro = await response.Content.ReadAsStringAsync();
            throw new Exception($"Erro ao cancelar cobrança. Código: {response.StatusCode}, Detalhe: {erro}");
        }
    }

    /// <summary>
    /// Cria um webhook para notificações de cobrança.
    /// </summary>
    public async Task CriarWebhookAsync(string urlWebhook)
    {
        var requestMessage = new HttpRequestMessage(HttpMethod.Put, "/cobranca/v3/cobrancas/webhook");

        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await ObterTokenAsync(new[] { "boleto-cobranca.write" }));
        if (!string.IsNullOrWhiteSpace((string)_config.ContaCorrente))
            requestMessage.Headers.Add("x-conta-corrente", _config.ContaCorrente);

        requestMessage.Content = new StringContent(JsonConvert.SerializeObject(new
        {
            webhookUrl = urlWebhook,
        }), Encoding.UTF8, "application/json");

        var response = await Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(5, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), onRetry: (exception, timeSpan, retryCount, context) =>
            {
                _logger.LogWarning($"Retry {retryCount} for CriarWebhookAsync due to: {exception.Message}");
            })
            .ExecuteAsync(async () => await _httpClient.SendAsync(requestMessage));

        if (!response.IsSuccessStatusCode)
        {
            var erro = await response.Content.ReadAsStringAsync();
            throw new Exception($"Erro ao criar webhook. Código: {response.StatusCode}, Detalhe: {erro}");
        }

        _logger.LogInformation("Webhook created successfully with URL: {Webhook_URL}", _config.WebhookUrl);
    }

    /// <summary>
    /// Exclui o webhook de notificações.
    /// </summary>
    public async Task ExcluirWebhookAsync()
    {
        var requestMessage = new HttpRequestMessage(HttpMethod.Delete, "/cobranca/v3/cobrancas/webhook");
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await ObterTokenAsync(new[] { "boleto-cobranca.write" }));
        if (!string.IsNullOrWhiteSpace((string)_config.ContaCorrente))
            requestMessage.Headers.Add("x-conta-corrente", _config.ContaCorrente);
        var response = await Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(5, retryAttempt => TimeSpan.FromSeconds(Math.Pow(1, retryAttempt)), onRetry: (exception, timeSpan, retryCount, context) =>
            {
                _logger.LogWarning($"Retry {retryCount} for ExcluirWebhookAsync due to: {exception.Message}");
            })
            .ExecuteAsync(async () => await _httpClient.SendAsync(requestMessage));
        if (!response.IsSuccessStatusCode) {
            var erro = await response.Content.ReadAsStringAsync();
            throw new Exception($"Erro ao excluir webhook. Código: {response.StatusCode}, Detalhe: {erro}");
        }
    }
}
