using HtmlAgilityPack;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApiDocument(config => {
    config.DocumentName = "ConsultaServicosPublicosAOApi";
    config.Title = "ConsultaServicosPublicosAOApi v1";
    config.Version = "v1";
});

var app = builder.Build();

if (app.Environment.IsDevelopment()) {
    app.UseOpenApi();
    app.UseSwaggerUi(config => {
        config.DocumentTitle = "ConsultaServicosPublicosAOApi";
        config.Path = "/swagger";
        config.DocumentPath = "/swagger/{documentName}/swagger.json";
        config.DocExpansion = "list";
    });
}


app.MapGet("public/passaporte",  async (string numBI) => {
    HttpClient httpClient = new();
    var requestContent = new MultipartFormDataContent
    {
        { new StringContent(numBI), "bi" }
    };
    var response = await httpClient.PostAsync("https://www.sme.gov.ao/bilhetemethod/", requestContent);
    
    if (response.IsSuccessStatusCode) {
        var htmlContent = await response.Content.ReadAsStringAsync();

        var doc = new HtmlDocument();
        doc.LoadHtml(htmlContent);

        var nomeNode = doc.DocumentNode.SelectSingleNode("//ol[@class='breadcrumb']/strong[text()='Nome: ']/following-sibling::text()[1]");
        var numeroPassaporteNode = doc.DocumentNode.SelectSingleNode("//ol[@class='breadcrumb']/strong[text()='| Número Passaporte: ']/following-sibling::text()[1]");
        var dataEmissaoNode = doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'col-4')]/p[contains(@class, 'text-success') and contains(text(), 'Passaporte emitido:')]");

        var Nome = nomeNode?.InnerText.Trim();
        var Numero = numeroPassaporteNode?.InnerText.Trim();
        var DataEmissao = dataEmissaoNode?.InnerText.Split("Passaporte emitido:")[1].Trim();

        return  (string.IsNullOrEmpty(Nome) || 
                 string.IsNullOrEmpty(Numero)) 
                 ? Results.Json(new {},statusCode: StatusCodes.Status404NotFound)
                 : Results.Json(new { Nome,
                                      Numero,
                                      DataEmissao
                                    }, 
                                    statusCode: StatusCodes.Status200OK); 


    } else {
        return Results.StatusCode((int)response.StatusCode);
    }
});


app.MapGet("public/nif", async (string numBI) => {
    HttpClient httpClient = new();
    var requestContent = new MultipartFormDataContent
    {
        { new StringContent(numBI), "nif" }
    };
    var response = await httpClient.GetAsync($"https://portaldocontribuinte.minfin.gov.ao/consultar-nif-do-contribuinte?nif={numBI}");

    if (response.IsSuccessStatusCode) {
        var htmlContent = await response.Content.ReadAsStringAsync();
        var doc = new HtmlDocument();
        doc.LoadHtml(htmlContent);

        var nomeNode = doc.DocumentNode.SelectSingleNode("//div[label[contains(text(), 'Nome:')]]/div/label");
        var nifNode = doc.DocumentNode.SelectSingleNode("//div[label[contains(text(), 'NIF:')]]/div/label");
        var tipoNode = doc.DocumentNode.SelectSingleNode("//div[label[contains(text(), 'Tipo:')]]/div/label");
        var estadoNode = doc.DocumentNode.SelectSingleNode("//div[label[contains(text(), 'Estado:')]]/div/label");

        var nomeExtracted = nomeNode?.InnerText.Trim();
        var nifExtracted = nifNode?.InnerText.Trim();
        var tipoExtracted = tipoNode?.InnerText.Trim();
        var estadoExtracted = estadoNode?.InnerText.Trim();

        return (string.IsNullOrEmpty(nomeExtracted) || string.IsNullOrEmpty(nifExtracted)) 
            ? Results.Json(new { }, statusCode: StatusCodes.Status404NotFound)
            : Results.Json(new {
                Nome = nomeExtracted,
                NIF = nifExtracted,
                Tipo = tipoExtracted,
                Estado = estadoExtracted
              }, statusCode: StatusCodes.Status200OK);
    } else {
        return Results.StatusCode((int)response.StatusCode);
    }
});

app.MapGet("public/bilhete", async(string numBI) => {
    HttpClient httpClient = new();
    if (string.IsNullOrEmpty(numBI)) {
        Results.Json(new { }, statusCode: StatusCodes.Status400BadRequest);
    }
    var response = await httpClient.GetAsync($"https://bi.minjusdh.gov.ao/api/identityLostService/identitycardlost/queryRegisterInfo/{numBI}");
    if (response.IsSuccessStatusCode) {
        var resultado = await response.Content.ReadAsStringAsync();
        var jsonObject = System.Text.Json.JsonSerializer.Deserialize<object>(resultado);

        return Results.Json(jsonObject);
    }
    return Results.StatusCode((int)response.StatusCode);
});


app.Run();
