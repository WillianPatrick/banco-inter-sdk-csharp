
# Banco Inter SDK

Este **Banco Inter SDK** permite a integração com os serviços de pagamento do Banco Inter, incluindo funcionalidades de emissão de boletos, consulta de cobranças, exclusão de webhooks, e mais. O SDK foi projetado para garantir estabilidade, resiliência a falhas e logs abrangentes para monitoramento.

## ✨ **Recursos Principais**
- 💳 **Emissão de Boletos**
- 📈 **Consulta de Cobranças**
- 🔐 **Obtenção de Token de Autenticação**
- ✉️ **Criação e Exclusão de Webhooks**
- 🔖 **Download de PDF do Boleto**
- 🔄 **Cancelamento de Cobranças**

---

## 📘 **Instalação**

Para instalar o SDK, basta importar o namespace no seu projeto .NET.

```csharp
using SDK.Payments;
```

---

## 📚 **Configuração Inicial**
Para utilizar o SDK, é necessário configurar as credenciais e o certificado de acesso ao Banco Inter.

### **Configuração da Classe `Config`**

```csharp
var config = new Config
{
    BaseUrl = "https://sandbox.bancointer.com.br", // URL da API do Banco Inter
    ClientId = "seu-client-id",
    ClientSecret = "seu-client-secret",
    Certificado = "caminho/do/seu/certificado.pfx", // Caminho do certificado PFX
    Chave = "sua-senha-do-certificado",
    ContaCorrente = "000123456" // Conta corrente vinculada
};
```

---

## 💪 **Exemplos de Uso**

### **Instanciação do BancoInter SDK**

```csharp
var logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<BancoInter>();
var bancoInter = new BancoInter(config, logger);
```

---

## 📈 **Métodos Disponíveis**

### **1. Obter Token de Autenticação**

```csharp
var token = await bancoInter.ObterTokenAsync();
Console.WriteLine("Token obtido: " + token);
```

### **2. Emissão de Boleto**

```csharp
var cobranca = new Payments.DTOs.RequisicaoEmitirCobranca
{
    SeuNumero = "123456789",
    ValorNominal = 150.00,
    DataVencimento = "2024-12-31",
    Pagador = new Payments.DTOs.Pagador
    {
        Nome = "João da Silva",
        CpfCnpj = "12345678900",
        Endereco = "Rua Principal, 123",
        Cidade = "São Paulo",
        Uf = "SP",
        Cep = "01010100"
    }
};

var codigoSolicitacao = await bancoInter.EmitirBoletoAsync(cobranca);
Console.WriteLine("Boleto emitido. Código de Solicitação: " + codigoSolicitacao);
```

### **3. Consultar Cobrança**

```csharp
var cobranca = await bancoInter.ConsultarCobrancaAsync("1234567890");
Console.WriteLine("Detalhes da Cobrança: " + JsonConvert.SerializeObject(cobranca));
```

### **4. Obter PDF do Boleto**

```csharp
var pdfBase64 = await bancoInter.ObterPdfAsync("1234567890");
Console.WriteLine("PDF do boleto em Base64: " + pdfBase64);
```

### **5. Criar Webhook**

```csharp
await bancoInter.CriarWebhookAsync("https://minhaurl.com/webhook");
Console.WriteLine("Webhook criado com sucesso.");
```

### **6. Excluir Webhook**

```csharp
await bancoInter.ExcluirWebhookAsync();
Console.WriteLine("Webhook excluído com sucesso.");
```

### **7. Cancelar Cobrança**

```csharp
await bancoInter.CancelarCobrancaAsync("1234567890", "Cancelamento solicitado pelo cliente");
Console.WriteLine("Cobrança cancelada com sucesso.");
```

---

## ⚙️ **Boas Práticas**
- **Logs Detalhados**: O SDK gera logs de Info, Debug, Warning e Error para rastreamento de falhas e execução de métodos.
- **Retry Policy**: Usa a biblioteca Polly para tentar novamente em caso de falha, com até 5 tentativas de reconexão.
- **Timeouts e Confiabilidade**: Configure um tempo de espera adequado para a resposta da API.
- **Segurança do Certificado**: Mantenha o certificado .pfx protegido e evite armazená-lo em repositórios públicos.

---

## ⚠️ **Erros Comuns e Soluções**

### **Erro: Token Inválido**
**Causa**: Token JWT expirado ou não solicitado corretamente.
**Solução**: Certifique-se de que o método `ObterTokenAsync` foi executado antes de chamar outras operações.

### **Erro: Falha de Certificado**
**Causa**: O caminho ou a senha do certificado está incorreta.
**Solução**: Verifique o caminho e a senha do certificado PFX.

---

## 🔗 **Links Úteis**
- [Documentação Oficial Banco Inter](https://developers.bancointer.com.br/)
- [Polly - Resiliência para .NET](https://github.com/App-vNext/Polly)

---

## 💪 **Contribuição**
Se desejar contribuir com o SDK, envie um pull request para melhorias no código e documentação.

---

## ⚠️ **Licença**
O SDK está licenciado sob a **MIT License**. Veja o arquivo LICENSE para mais detalhes.

---

## 🛠️ **Suporte**
Caso precise de suporte, envie um e-mail para **suporte@seudominio.com** com o assunto **[SUPORTE SDK Banco Inter]**.

---

📈 **Versão Atual**: 1.0.0

Willian Patrick dos Santos - superhitec@gmail.com

## **Doações**:
```
PIX: superhitec@gmail.com
Ethereum / Polygon: 0x7e7f8511f4Bbb44fF811A4526b0bF949949c6F5e
```
