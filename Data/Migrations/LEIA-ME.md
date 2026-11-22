# Migrations do SGD

O banco é MySQL 8.0 (Pomelo). As tabelas são criadas e alteradas **somente** por migrations.

## Como a migration é aplicada

| Ambiente | `Database:AplicarMigrations` | Como o banco é atualizado |
|---|---|---|
| Desenvolvimento | `true` (em `appsettings.Development.json`) | Automático ao iniciar a aplicação (`dotnet run`). |
| Produção | `false` (padrão em `appsettings.json`) | Manual, antes de subir a nova versão (veja abaixo). |

Com `AplicarMigrations` desligado, a aplicação **não sobe** se houver migration pendente e informa o que fazer.

## Comandos

O `dotnet ef` não lê os user-secrets. Antes de usá-lo, defina a conexão na janela do terminal:

```powershell
$env:ConnectionStrings__Sgd = "Server=localhost;Port=33047;Database=sgd_dev;User=sgd_app;Password=SUA_SENHA;CharSet=utf8mb4;"
```

Criar uma nova migration (depois de alterar um model ou uma configuration):

```
dotnet ef migrations add NomeDaMudanca --output-dir Data/Migrations
```

Aplicar no banco indicado pela variável acima:

```
dotnet ef database update
```

Gerar o script SQL para o servidor (produção), que pode ser executado mais de uma vez com segurança:

```
dotnet ef migrations script --idempotent -o migracao.sql
```

Desfazer a última migration **ainda não aplicada**:

```
dotnet ef migrations remove
```

## Regras

- Não edite uma migration que já foi aplicada em algum banco; crie uma nova.
- Faça backup do banco antes de aplicar migrations em produção.
- Sempre versione os três arquivos de cada migration (`..._Nome.cs`, `..._Nome.Designer.cs`) e o `SgdDbContextModelSnapshot.cs`.
