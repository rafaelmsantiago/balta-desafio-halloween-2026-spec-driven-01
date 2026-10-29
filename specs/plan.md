# Plano técnico

## Contexto
Este plano descreve **como** implementar o que está definido no `spec.md` (Gerador de Senhas Fortes): uma API com dois endpoints, `POST /senhas` e `GET /senhas/{id}`, que gera senhas fortes, persiste e permite consulta por GUID.

Restrições herdadas do spec:
- Projeto lúdico, para aprender SDD. Não deve ir para produção.
- Senha em texto puro, sem autenticação, sem rate limiting (fora de escopo).
- Senha com 16 caracteres, ao menos um especial, uma maiúscula, uma minúscula e um dígito, sem espaços (RN01–RN04).

Restrições herdadas da `constitution.md`: stack (.NET 10, Minimal API, SQLite via EF Core, xUnit), arquitetura em camadas (endpoint → serviço de aplicação → banco), qualidade (teste unitário para toda regra de negócio, teste de integração de caminho feliz para todo endpoint, erro de validação como 400 com ProblemDetails), convenções (código em inglês, mensagens de erro em português) e governança (se o plano contradiz a constituição, o plano muda).

Princípio do plano: **a solução mais simples que satisfaça os critérios de aceite CA01–CA10 e respeite a constituição**. Cada decisão abaixo tem sua justificativa ao lado.

### Conformidade com a constituição

| Regra da constituição | Como o plano atende |
|---|---|
| Stack: .NET 10, C#, Minimal API | D1, D2 |
| Persistência em SQLite via EF Core | D3, D4 |
| Testes com xUnit | D17 |
| Endpoint não acessa banco diretamente, sempre via serviço de aplicação | D19. O endpoint depende só de `IPasswordService`; apenas o serviço usa o `AppDbContext`. |
| Regra de negócio não vive no endpoint | D19. Geração e persistência ficam em `PasswordGenerator` e `PasswordService`; o endpoint só traduz HTTP. |
| Nenhuma dependência nova sem justificativa escrita no plano | D21 lista cada pacote e sua justificativa. |
| Toda regra de negócio tem teste unitário | D18. Gerador e serviço têm testes unitários próprios. |
| Todo endpoint tem teste de integração de caminho feliz | D18. POST e GET têm testes de integração do caminho feliz. |
| Erro de validação retorna 400 com ProblemDetails | D13. |
| Nomes de classe e arquivo em inglês, mensagens de erro em português | D20. |
| Commit no formato `tipo(escopo): descrição` com o identificador do requisito | D22. |

### Ajustes feitos em relação à versão anterior do plano
- Antes: endpoints usavam o `AppDbContext` direto. Agora: passam por `IPasswordService` (D19).
- Antes: erros no formato `{ "erro": "..." }`. Agora: ProblemDetails para 400, 404 e 500 (D13). O exemplo de RF04 no `spec.md` foi atualizado para refletir isso; o requisito (corpo com mensagem legível) não mudou.
- Antes: classe `Senha`, tabela `Senhas`. Agora: `Password`, tabela `Passwords` (D20). A rota `/senhas` e o campo JSON `senha` seguem como no spec, pois fazem parte do contrato público.
- Antes: dependências sem justificativa individual. Agora: D21.

## Arquitetura
Aplicação única (um projeto web), com três camadas:

```
Cliente HTTP
    │
    ▼
┌──────────────────────────────┐
│ Endpoints (Minimal API)      │  valida o GUID e traduz o resultado em HTTP
├──────────────────────────────┤
│ IPasswordService             │  serviço de aplicação: orquestra geração + persistência
│   ├─ IPasswordGenerator      │  regra de negócio: gera a senha (RN01–RN05)
│   └─ AppDbContext (EF Core)  │  persistência (só o serviço acessa)
└──────────────────────────────┘
    │
    ▼
 SQLite (arquivo senhas.db)
```

Estrutura de pastas:

```
src/PasswordApi/
  Program.cs                      # configuração, DI e mapeamento dos endpoints
  Endpoints/PasswordsEndpoints.cs
  Services/IPasswordGenerator.cs
  Services/PasswordGenerator.cs
  Services/IPasswordService.cs
  Services/PasswordService.cs
  Data/AppDbContext.cs
  Data/Password.cs
  Migrations/
tests/PasswordApi.Tests/
  PasswordGeneratorTests.cs       # unitários das regras de geração
  PasswordServiceTests.cs         # unitários do serviço (SQLite em memória)
  PasswordsEndpointsTests.cs      # integração: critérios de aceite
  TestApiFactory.cs               # WebApplicationFactory customizada
```

## Decisões

| # | Decisão | Justificativa |
|---|---------|---------------|
| D1 | **.NET 10 (LTS) com C#** | Definido na constituição. Plataforma com suporte de longo prazo e que traz tudo que precisamos (HTTP, DI, aleatoriedade criptográfica, ORM). |
| D2 | **Minimal API** em vez de Controllers | Definido na constituição. São apenas dois endpoints; reduz o código ao essencial e deixa o mapeamento spec → código fácil de ler. |
| D3 | **SQLite** como banco | Definido na constituição. Zero instalação: um arquivo local. Atende CA09 (persistência após reinício). |
| D4 | **EF Core** como acesso a dados | Definido na constituição. Abstrai o SQL, cria o schema por migration e facilita testes. |
| D5 | **`RandomNumberGenerator`** (`System.Security.Cryptography`) como fonte de aleatoriedade | Atende RN05. `System.Random` é previsível e não deve ser usado para senhas, mesmo num exercício. |
| D6 | **Algoritmo de geração:** (1) sorteia 1 caractere de cada categoria (maiúscula, minúscula, dígito, especial); (2) completa até 16 com caracteres do conjunto total; (3) embaralha com Fisher-Yates usando o mesmo gerador criptográfico | Garante RN02 e RN04 **por construção**, em vez de gerar e tentar de novo (não determinístico). O embaralhamento evita que os 4 primeiros caracteres sigam sempre a mesma ordem de categorias. |
| D7 | **`RandomNumberGenerator.GetInt32(max)`** para sorteio de índices | Evita viés de módulo (`byte % n`), que favoreceria certos caracteres. |
| D8 | **Conjunto de especiais:** `!@#$%^&*()-_=+[]{}` | Apenas ASCII imprimível, sem espaço (RN03) e sem caracteres que exigem escape em JSON (`"`, `\`). Resolve o caso de borda do spec. |
| D9 | **`IPasswordGenerator` como interface** registrada na injeção de dependência | Permite testar a geração isoladamente e substituir o gerador por um fixo nos testes de serviço e de endpoint. |
| D10 | **GUID v4 gerado com `Guid.NewGuid()`** | O spec (RN06) pede GUID v4. A aleatoriedade torna o identificador não adivinhável, a única proteção de acesso que temos. |
| D11 | **GUID como chave primária**, gravado como texto no SQLite | É o identificador público; um ID sequencial paralelo só acrescentaria complexidade. |
| D12 | **O endpoint `GET` recebe `{id}` como `string`** e valida com `Guid.TryParse` | Com a restrição de rota `{id:guid}`, um valor inválido resultaria em `404` pelo roteamento. O spec exige `400` para formato inválido (CA07) e `404` só para GUID inexistente (CA06). A conversão fica no endpoint por ser validação de entrada HTTP; o serviço recebe um `Guid` já válido. |
| D13 | **Erros em ProblemDetails (RFC 9457), com texto em português:** `400` via `Results.ValidationProblem` (erro de validação, campo `errors.id`); `404` via `Results.Problem`; `500` via `AddProblemDetails()` + `UseExceptionHandler()` | A constituição exige 400 com ProblemDetails para validação. Usar o mesmo formato nos demais erros mantém a API consistente e não exige nenhum pacote novo (é recurso nativo do ASP.NET Core). `ValidationProblem` já produz um tipo derivado de ProblemDetails. |
| D14 | **A senha só é retornada após o `SaveChanges` concluir** (garantido dentro do `PasswordService`) | Atende o caso de borda: falha de banco → `500` sem devolver senha/GUID que não existam no banco. |
| D15 | **Não registrar a senha em logs** | Mesmo sendo exercício, evita vazamento em logs e não custa nada. |
| D16 | **OpenAPI nativo** (`Microsoft.AspNetCore.OpenApi`) | Documenta e permite experimentar os endpoints sem ferramentas externas, útil para quem está aprendendo. |
| D17 | **xUnit + `WebApplicationFactory`** para testes | xUnit definido na constituição. A factory sobe a API de verdade com SQLite em memória e cobre os critérios de aceite de ponta a ponta. |
| D18 | **Estratégia de testes:** unitário do gerador (todas as regras RN01–RN05), unitário do serviço (persistência, consulta, falha de banco) e integração por endpoint (um teste por critério de aceite CA01–CA10, com o ID do critério no nome do teste) | A constituição exige teste unitário para toda regra de negócio e integração de caminho feliz para todo endpoint. Nomear testes por critério faz a ponte do SDD: o spec vira teste e nenhum critério fica sem cobertura. |
| D19 | **Serviço de aplicação `IPasswordService`** com `CreateAsync` e `GetByIdAsync`; é o único que usa `AppDbContext` e `IPasswordGenerator` | Exigido pela constituição: endpoint não acessa banco e regra de negócio não vive no endpoint. Também deixa o endpoint trivial e o serviço testável sem HTTP. Sem repositório separado: o `DbContext` já é a abstração de persistência e uma camada a mais seria excesso para uma tabela. |
| D20 | **Nomes em inglês no código, arquivos, tabela e colunas** (`Password`, `PasswordService`, tabela `Passwords`, colunas `Id`, `Value`, `CreatedAtUtc`); **mensagens de erro em português**. O contrato público (rota `/senhas` e campo JSON `senha`) permanece em português como definido no spec | Convenção da constituição. A rota e o campo JSON são contrato com o consumidor, definido no spec, e não código interno; mudá-los exigiria alterar o spec. A conversão entre `Value` e `senha` acontece no endpoint, num DTO de resposta. |
| D21 | **Dependências permitidas**, detalhadas na tabela abaixo | A constituição proíbe dependência nova sem justificativa escrita no plano. Qualquer pacote fora da lista exige atualizar este plano antes. |
| D22 | **Um commit por tarefa**, no formato `tipo(escopo): descrição [ID do requisito]` (ex.: `feat(generator): sorteia um caractere por categoria [RN02]`) | Convenção da constituição. Um commit por tarefa mantém o histórico rastreável até o requisito e facilita reverter uma tarefa isolada. Mudanças na constituição vão em commit separado. |
| D23 | **Interface Scalar** em `/scalar`, lendo o documento de `/openapi/v1.json` | O documento OpenAPI sozinho é só um JSON, e o browser não consegue chamar o `POST`. A interface permite testar os dois endpoints no browser, sem curl nem ferramentas externas, o que ajuda quem está aprendendo (mesma motivação da D16). |

### D21 – Dependências e justificativas

| Pacote / ferramenta | Projeto | Justificativa |
|---|---|---|
| `Microsoft.EntityFrameworkCore.Sqlite` | API | Provedor SQLite do EF Core (constituição, D3 e D4). |
| `Microsoft.EntityFrameworkCore.Design` (com `PrivateAssets="all"`) | API | Necessário apenas para criar migrations (`dotnet ef migrations add`). Não vai para a saída de build. |
| `dotnet-ef` (ferramenta local, via manifesto de tools) | Repositório | Comando que cria as migrations. Local ao repositório para não depender da máquina de quem clona. |
| `Microsoft.AspNetCore.OpenApi` | API | Documento OpenAPI nativo (D16). Pacote oficial da Microsoft; evita adicionar bibliotecas de terceiros como Swashbuckle. |
| `Scalar.AspNetCore` | API | Interface no browser para experimentar os endpoints (D23). Só lê o documento OpenAPI já gerado pelo pacote acima, sem gerar outro; mais leve e atual que o Swashbuckle. |
| `xunit`, `xunit.runner.visualstudio`, `Microsoft.NET.Test.Sdk` | Testes | Framework de testes definido na constituição e pacotes mínimos para o `dotnet test` descobrir e rodar os testes. |
| `Microsoft.AspNetCore.Mvc.Testing` | Testes | Fornece o `WebApplicationFactory` para testes de integração (D17). Alternativa seria subir o servidor manualmente, o que é mais trabalhoso e frágil. |

Não são usados: bibliotecas de mock (os testes usam dublês simples escritos à mão), FluentAssertions, AutoMapper, MediatR ou qualquer biblioteca de validação.

## Modelo de dados

Uma única tabela, `Passwords`:

| Coluna | Tipo (SQLite) | Restrições | Observação |
|--------|---------------|------------|------------|
| `Id` | TEXT (GUID) | PK, NOT NULL | Identificador público retornado pelo `POST`. |
| `Value` | TEXT | NOT NULL, máx. 128 | Senha em texto puro (RN07). Exposta na API como `senha`. |
| `CreatedAtUtc` | TEXT (ISO 8601) | NOT NULL | Apenas para diagnóstico; **não é exposta** pela API. |

Entidade C#:

```csharp
public class Password
{
    public Guid Id { get; set; }
    public string Value { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; }
}
```

- Schema criado por **migration** do EF Core, aplicada na inicialização da aplicação (simplifica rodar o projeto).
- Sem índices extras: a busca é sempre pela PK.
- Connection string: `Data Source=senhas.db` (configurável por `appsettings.json`).

## Contratos

### `POST /senhas`
Gera e armazena uma senha. Não recebe corpo.

| | |
|---|---|
| **Sucesso** | `201 Created`, cabeçalho `Location: /senhas/{id}` |
| **Erro** | `500 Internal Server Error` (ProblemDetails) se não conseguir persistir |

Resposta `201`:
```json
{
  "id": "3f2b8c1e-5d4a-4e7b-9a10-2c6f8d9e1a34",
  "senha": "k#9Vp!2xQ@m7Zt&L"
}
```

### `GET /senhas/{id}`
Consulta a senha associada ao GUID.

| Situação | Status | Corpo |
|----------|--------|-------|
| Encontrada | `200 OK` | `{ "id": "...", "senha": "..." }` |
| `id` não é um GUID | `400 Bad Request` | ProblemDetails de validação |
| GUID não existe | `404 Not Found` | ProblemDetails |
| Falha inesperada | `500 Internal Server Error` | ProblemDetails |

Exemplo `400` (`Content-Type: application/problem+json`):
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Erro de validação.",
  "status": 400,
  "errors": { "id": ["Identificador inválido."] }
}
```

Exemplo `404`:
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
  "title": "Senha não encontrada.",
  "status": 404
}
```

Exemplo `500`:
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.6.1",
  "title": "Erro interno.",
  "status": 500
}
```

Nenhuma resposta de erro contém a senha nem o GUID gerado (R8).

### Contratos internos

```csharp
public interface IPasswordGenerator
{
    string Generate(); // 16 caracteres, atende RN01–RN04
}

public interface IPasswordService
{
    Task<Password> CreateAsync(CancellationToken cancellationToken); // gera, persiste e só então retorna
    Task<Password?> GetByIdAsync(Guid id, CancellationToken cancellationToken); // null se não existir
}
```

### Rastreabilidade spec → plano

| Spec | Onde é atendido |
|------|-----------------|
| RF01, RF03 | `POST /senhas` + `PasswordService.CreateAsync` + tabela `Passwords` (D3, D4, D14, D19) |
| RF02, RF04 | `GET /senhas/{id}` + `PasswordService.GetByIdAsync` (D12, D13, D19) |
| RN01–RN04 | `PasswordGenerator` (D6, D8) |
| RN05 | D5, D7 |
| RN06 | D10, D11 |
| RN07 | Modelo de dados + aviso no README |
| CA01–CA10 | Testes (D17, D18) |

## Riscos

| # | Risco | Impacto | Mitigação |
|---|-------|---------|-----------|
| R1 | **Senhas em texto puro** no banco | Qualquer pessoa com acesso ao arquivo lê todas as senhas | Aceito pelo spec (RN07). Documentar no README e **não usar em produção**. |
| R2 | **Sem autenticação:** quem tem o GUID lê a senha | Vazamento do GUID expõe a senha | Aceito pelo spec. O GUID v4 não é adivinhável (D10), mas deve ser tratado como segredo. |
| R3 | Uso indevido do projeto como serviço real | Exposição de dados sensíveis | Aviso explícito no README e na descrição do OpenAPI. |
| R4 | Gerador produzir senha fora das regras (bug) | Quebra de RN01–RN04 | Geração por construção (D6) + testes unitários repetidos muitas vezes (ex.: 1.000 senhas) validando todas as regras. |
| R5 | Viés estatístico na escolha dos caracteres | Senhas menos aleatórias | `GetInt32` do gerador criptográfico (D7). |
| R6 | Colisão de GUID | Praticamente nula | A PK impede duplicidade; a colisão causaria `500`, comportamento aceitável neste escopo. |
| R7 | Concorrência de escrita no SQLite | Erros de "database is locked" em carga alta | Irrelevante para o volume do exercício; trocar de banco seria a evolução natural. |
| R8 | Senha vazar por logs ou mensagens de erro | Exposição indevida | D15: não logar a senha; ProblemDetails de erro nunca incluem o valor. |
| R9 | Dependência de arquivo local `senhas.db` | Dados perdidos se o arquivo for apagado ou o container for recriado | Aceitável para aprendizado; documentar no README. |
| R10 | Endpoint voltar a acessar o banco diretamente por conveniência (regressão arquitetural) | Viola a constituição | Tarefa final de conformidade verifica que nenhum arquivo em `Endpoints/` referencia `AppDbContext`. |
| R11 | Exceções não tratadas expondo detalhes internos em ProblemDetails | Vazamento de informação | `UseExceptionHandler` + `AddProblemDetails` com título genérico "Erro interno."; sem stack trace no corpo. |
