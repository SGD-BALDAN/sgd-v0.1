using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using SgdBaldan.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// MVC com proteção antiforgery automática em todo POST/PUT/DELETE.
builder.Services.AddControllersWithViews(opcoes =>
    opcoes.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));

builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();
builder.Services.AddSgdDatabase(builder.Configuration);                                   // EF Core + MySQL (Pomelo)
builder.Services.AddSgdOptions(builder.Configuration);                                    // opções tipadas
builder.Services.AddSgdAutenticacao(builder.Configuration, builder.Environment);          // cookie + política padrão
builder.Services.AddSgdServices();                                                        // services da aplicação

var app = builder.Build();

// Migrations (opcional) e dados iniciais: categorias, produtos e primeiro Programador.
await SgdBaldan.Data.DbInicializador.InicializarAsync(app.Services, app.Configuration, app.Logger);

// Cultura pt-BR: vírgula decimal nas medições, datas dd/MM/yyyy.
var ptBr = new CultureInfo("pt-BR");
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(ptBr),
    SupportedCultures = new List<CultureInfo> { ptBr },
    SupportedUICultures = new List<CultureInfo> { ptBr }
});

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/erro/500");   // loga e mostra a página amigável
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/erro/{0}");   // 404, 403 etc.
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();   // rotas por atributo ([Route]) nos controllers

app.Run();
