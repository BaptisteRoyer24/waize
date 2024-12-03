namespace WaizeRoutingServer.Services;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

public class ProxyClient
{
    private readonly HttpClient _httpClient;
    private readonly string _serviceUrl;

    public ProxyClient(string serviceUrl)
    {
        _httpClient = new HttpClient();
        _serviceUrl = serviceUrl;
    }

    public async Task<string> GetResponseAsync(string url)
    {
        // Créez une requête SOAP au format XML
        var soapEnvelope = $@"<?xml version=""1.0"" encoding=""utf-8""?>
            <soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:ser=""http://tempuri.org/"">
              <soap:Body>
                <ser:GetResponse>
                  <ser:url>{System.Security.SecurityElement.Escape(url)}</ser:url>
                </ser:GetResponse>
              </soap:Body>
            </soap:Envelope>";

        // Envoi de la requête
        var content = new StringContent(soapEnvelope, Encoding.UTF8, "text/xml");

        // Ajout des en-têtes nécessaires
        content.Headers.Clear();
        content.Headers.Add("Content-Type", "text/xml");
        content.Headers.Add("SOAPAction", "\"http://tempuri.org/IProxyCacheService/GetResponse\"");

        var response = await _httpClient.PostAsync(_serviceUrl, content);

        // Vérifiez le statut HTTP
        response.EnsureSuccessStatusCode();

        // Lire et retourner la réponse SOAP
        var responseContent = await response.Content.ReadAsStringAsync();
        var jsonResult = ExtractJsonFromSoap(responseContent);

        return jsonResult;
    }

    public async Task<string> SetCacheDurationAsync(int seconds)
    {
        // Créez une requête SOAP au format XML
        var soapEnvelope = $@"
            <?xml version=""1.0"" encoding=""utf-8""?>
            <soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:ser=""http://tempuri.org/"">
                <soap:Body>
                    <ser:SetCacheDuration>
                        <ser:seconds>{seconds}</ser:seconds>
                    </ser:SetCacheDuration>
                </soap:Body>
            </soap:Envelope>";

        // Envoi de la requête
        var content = new StringContent(soapEnvelope, Encoding.UTF8, "text/xml");
        var response = await _httpClient.PostAsync(_serviceUrl, content);

        // Vérifiez le statut HTTP
        response.EnsureSuccessStatusCode();
        
        var responseContent = await response.Content.ReadAsStringAsync();
        var jsonResult = ExtractJsonFromSoap(responseContent);

        return jsonResult;
    }
    
    // Méthode utilitaire pour extraire la partie JSON d'une réponse SOAP
    private string ExtractJsonFromSoap(string soapResponse)
    {
        var xmlDoc = new XmlDocument();
        xmlDoc.LoadXml(soapResponse);

        // Recherche la balise <GetResponseResult>
        var namespaceManager = new XmlNamespaceManager(xmlDoc.NameTable);
        namespaceManager.AddNamespace("s", "http://schemas.xmlsoap.org/soap/envelope/");
        namespaceManager.AddNamespace("temp", "http://tempuri.org/");

        var jsonNode = xmlDoc.SelectSingleNode("//temp:GetResponseResult", namespaceManager);

        return jsonNode?.InnerText ?? string.Empty;
    }
}