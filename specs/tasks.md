# Tarefas

Como usar:
- Execute **na ordem dos IDs**. Uma tarefa só começa quando todas as listadas em "Depende de" estiverem concluídas.
- Cada tarefa é pequena e verificável sozinha: a coluna "Concluída quando" diz como confirmar sem depender das tarefas seguintes.
- "Origem" aponta para o requisito que gerou a tarefa: `RF`, `RN`, `CA` e "Casos de borda" vêm do `spec.md`; `D`, `R` e "Modelo de dados" vêm do `plan.md`; "Constituição" vem da `constitution.md`.
- **Um commit por tarefa**, no formato `tipo(escopo): descrição [requisito]`. Exemplos: `feat(generator): sorteia um caractere por categoria [RN02]`, `test(api): cobre GUID inválido no GET [CA07]`. Tipos usados: `feat`, `test`, `chore`, `docs`, `refactor`.
- Mudanças na constituição vão em **commit separado** e nunca dentro de uma tarefa.
- Convenção de testes: o nome de cada teste de aceite começa com o ID do critério (ex.: `CA05_...`).
- Nomes de classe e arquivo em inglês; mensagens de erro em português.

## Fase 1: Fundação

| ID | Tarefa | Origem | Depende de | Concluída quando |
|---|---|---|---|---|
| T-01 | Criar a solution, o projeto web `src/PasswordApi` (Minimal API, .NET 10) e o projeto de testes `tests/PasswordApi.Tests` (xUnit), com a estrutura de pastas do plano | Constituição (Stack), D1, D2, D17 | | `dotnet build` na raiz termina sem erros |
| T-02 | Adicionar **somente** os pacotes e ferramentas listados em D21 (EF Core Sqlite, EF Core Design com `PrivateAssets="all"`, OpenApi na API; Mvc.Testing nos testes; `dotnet-ef` como ferramenta local em manifesto) | Constituição (Dependências), D21 | T-01 | `dotnet restore` e `dotnet build` passam, `dotnet ef --version` funciona e `dotnet list package` não mostra nenhum pacote fora de D21 |
| T-03 | Criar a entidade `Password` com `Id` (Guid), `Value` (string) e `CreatedAtUtc` (DateTime) | Modelo de dados, RN06, RN07, D20 | T-01 | Classe existe em `Data/Password.cs` e o projeto compila |

## Fase 2: Persistência

| ID | Tarefa | Origem | Depende de | Concluída quando |
|---|---|---|---|---|
| T-04 | Criar `AppDbContext` com `DbSet<Password>` e mapeamento: `Id` como PK, `Value` obrigatório com máximo de 128, `CreatedAtUtc` obrigatório | Modelo de dados, D4, D11 | T-02, T-03 | Projeto compila e o mapeamento está em `OnModelCreating` |
| T-05 | Adicionar a connection string `Data Source=senhas.db` em `appsettings.json` e registrar o `AppDbContext` com SQLite no `Program.cs` | D3 | T-04 | `dotnet run` sobe a API sem erros |
| T-06 | Criar a migration inicial e aplicá-la automaticamente na inicialização da aplicação | RF03, D4 | T-05 | Ao subir a API, o arquivo `senhas.db` existe e contém a tabela `Passwords` com as 3 colunas |

## Fase 3: Gerador de senhas

| ID | Tarefa | Origem | Depende de | Concluída quando |
|---|---|---|---|---|
| T-07 | Definir a interface `IPasswordGenerator` com o método `string Generate()` | D9 | T-01 | Interface existe em `Services/IPasswordGenerator.cs` e o projeto compila |
| T-08 | Implementar `PasswordGenerator` (parte 1): definir os conjuntos de caracteres (maiúsculas, minúsculas, dígitos e especiais `!@#$%^&*()-_=+[]{}`) e sortear 1 caractere de cada categoria com `RandomNumberGenerator.GetInt32` | RN02, RN04, RN05, D5, D7, D8 | T-07 | `Generate()` retorna uma string que contém ao menos 1 caractere de cada categoria (confirmado por um teste rápido ou no console) |
| T-09 | Implementar `PasswordGenerator` (parte 2): completar até 16 caracteres sorteando do conjunto total | RN01, RN03, D6 | T-08 | `Generate()` retorna sempre uma string de exatamente 16 caracteres |
| T-10 | Implementar `PasswordGenerator` (parte 3): embaralhar o resultado com Fisher-Yates usando `RandomNumberGenerator` | D6 | T-09 | Em 100 gerações, os 4 primeiros caracteres não seguem sempre a mesma ordem de categorias |
| T-11 | Escrever o teste unitário que gera 1.000 senhas e verifica, para todas: tamanho 16, pelo menos 1 maiúscula, 1 minúscula, 1 dígito e 1 especial, e nenhum caractere de espaço em branco | Constituição (Qualidade), RN01 a RN04, R4 | T-10 | Teste passa com `dotnet test --filter PasswordGeneratorTests` |
| T-12 | Escrever o teste unitário que gera 1.000 senhas e verifica que não há duplicadas | Constituição (Qualidade), RN05, CA08 | T-10 | Teste passa com `dotnet test --filter PasswordGeneratorTests` |

## Fase 4: Serviço de aplicação

| ID | Tarefa | Origem | Depende de | Concluída quando |
|---|---|---|---|---|
| T-13 | Definir a interface `IPasswordService` com `CreateAsync(CancellationToken)` retornando `Password` e `GetByIdAsync(Guid, CancellationToken)` retornando `Password?` | Constituição (Arquitetura), D19 | T-03, T-07 | Interface existe em `Services/IPasswordService.cs` e o projeto compila |
| T-14 | Implementar `PasswordService.CreateAsync`: chamar o gerador, criar `Password` com `Guid.NewGuid()` e `CreatedAtUtc`, gravar com `SaveChangesAsync` e **só então** retornar | RF01, RF03, RN06, D10, D14, D19 | T-06, T-10, T-13 | Método implementado e o projeto compila; o comportamento é verificado na T-15 |
| T-15 | Escrever os testes unitários de `CreateAsync` com SQLite em memória e gerador falso: a senha retornada é a do gerador, o `Id` é um GUID não vazio, e a linha existe no banco com os mesmos valores | Constituição (Qualidade), RF01, RF03, RN06 | T-14 | Testes passam com `dotnet test --filter PasswordServiceTests` |
| T-16 | Escrever o teste unitário de falha: com um `AppDbContext` de teste cujo `SaveChangesAsync` lança exceção, `CreateAsync` propaga a exceção e nenhuma linha é gravada | Constituição (Qualidade), D14, Casos de borda | T-14 | Teste passa com `dotnet test --filter PasswordServiceTests` |
| T-17 | Implementar `PasswordService.GetByIdAsync`: buscar pela PK e retornar `Password?` (null se não existir) | RF02, D19 | T-14 | Método implementado e o projeto compila; o comportamento é verificado na T-18 |
| T-18 | Escrever os testes unitários de `GetByIdAsync`: GUID existente retorna a senha gravada; GUID inexistente retorna `null` | Constituição (Qualidade), RF02, RF04 | T-17 | Testes passam com `dotnet test --filter PasswordServiceTests` |

## Fase 5: Infraestrutura da API e de testes

| ID | Tarefa | Origem | Depende de | Concluída quando |
|---|---|---|---|---|
| T-19 | No `Program.cs`, registrar `IPasswordGenerator` e `IPasswordService` na injeção de dependência, habilitar o OpenAPI e configurar `AddProblemDetails()` + `UseExceptionHandler()` | D9, D13, D16, D19 | T-05, T-10, T-17 | `dotnet run` sobe e o documento OpenAPI é acessível |
| T-20 | Criar o `TestApiFactory` (`WebApplicationFactory`) com SQLite em memória e a possibilidade de substituir serviços | D17 | T-06, T-19 | Um teste de fumaça sobe a API pela factory e a aplicação inicia sem erro |

## Fase 6: Endpoint de geração (POST)

| ID | Tarefa | Origem | Depende de | Concluída quando |
|---|---|---|---|---|
| T-21 | Criar `POST /senhas` em `Endpoints/PasswordsEndpoints.cs`: chamar apenas `IPasswordService.CreateAsync` e retornar `201` com `{ id, senha }` e cabeçalho `Location: /senhas/{id}` (sem acessar `AppDbContext`) | RF01, Constituição (Arquitetura), D19, D20 | T-19 | Uma chamada `POST /senhas` retorna `201` com corpo no formato do contrato e a linha aparece na tabela `Passwords` |
| T-22 | Escrever o teste de integração `CA01`: `POST /senhas` retorna `201` com `id` que é um GUID válido e `senha` preenchida (caminho feliz do endpoint) | Constituição (Qualidade), CA01 | T-20, T-21 | Teste passa |
| T-23 | Escrever os testes de integração `CA02`, `CA03` e `CA04` sobre a senha retornada pelo `POST`: tamanho mínimo 16, maiúscula, minúscula, dígito, especial e nenhum espaço | CA02, CA03, CA04 | T-22 | Três testes passam |
| T-24 | Escrever o teste de integração `CA08`: duas chamadas ao `POST` retornam `id` e `senha` diferentes | CA08 | T-22 | Teste passa |

## Fase 7: Endpoint de consulta (GET)

| ID | Tarefa | Origem | Depende de | Concluída quando |
|---|---|---|---|---|
| T-25 | Criar `GET /senhas/{id}` (caminho feliz): receber `id` como string, converter com `Guid.TryParse`, chamar `IPasswordService.GetByIdAsync` e retornar `200` com `{ id, senha }` | RF02, D12, Constituição (Arquitetura) | T-21 | Usando o `id` retornado por um `POST`, o `GET` devolve `200` com os dados corretos |
| T-26 | Escrever o teste de integração `CA05`: `POST` seguido de `GET` com o `id` retorna `200` e **exatamente a mesma senha** (caminho feliz do endpoint) | Constituição (Qualidade), CA05 | T-20, T-25 | Teste passa |
| T-27 | No `GET`, retornar `400` com `Results.ValidationProblem` (erro `id`: "Identificador inválido.") quando `id` não for um GUID válido | RF04, D12, D13, Constituição (Qualidade) | T-25 | `GET /senhas/abc` retorna `400` com `Content-Type: application/problem+json` e `errors.id` preenchido |
| T-28 | Escrever o teste de integração `CA07`: `GET /senhas/abc` retorna `400`, tipo `application/problem+json` e a mensagem "Identificador inválido." | CA07, Constituição (Qualidade) | T-20, T-27 | Teste passa |
| T-29 | No `GET`, retornar `404` com `Results.Problem` (título "Senha não encontrada.") quando o GUID for válido mas o serviço retornar `null` | RF04, D13 | T-25 | `GET /senhas/{guid aleatório}` retorna `404` com `Content-Type: application/problem+json` e o título esperado |
| T-30 | Escrever o teste de integração `CA06`: GUID válido inexistente retorna `404`, tipo `application/problem+json` e o título esperado | CA06 | T-20, T-29 | Teste passa |
| T-31 | Escrever o teste de borda: o mesmo GUID em letras maiúsculas e em minúsculas retorna a mesma senha | Casos de borda | T-20, T-25 | Teste passa |

## Fase 8: Robustez e persistência

| ID | Tarefa | Origem | Depende de | Concluída quando |
|---|---|---|---|---|
| T-32 | Escrever o teste de borda de integração: substituir o `IPasswordService` por um que lança exceção e verificar que `POST /senhas` retorna `500` com `application/problem+json`, título "Erro interno.", e corpo **sem** `id` nem `senha` nem stack trace | Casos de borda, D13, D14, R8, R11 | T-20, T-21 | Teste passa |
| T-33 | Escrever o teste `CA09`: usar um arquivo SQLite temporário, gerar uma senha, recriar a aplicação (nova factory apontando para o mesmo arquivo) e consultar o `id` | CA09, RF03 | T-20, T-25 | Teste passa e o arquivo temporário é removido ao final |
| T-34 | Revisar o código e garantir que nenhum log, exceção ou resposta de erro inclua o valor da senha | D15, R8 | T-32 | Busca no código por usos de `ILogger` e mensagens de exceção não encontra a senha sendo escrita |

## Fase 9: Documentação e fechamento

| ID | Tarefa | Origem | Depende de | Concluída quando |
|---|---|---|---|---|
| T-35 | Adicionar resumo e descrição aos dois endpoints no OpenAPI, incluindo o aviso de que é um projeto de aprendizado e os status possíveis (200, 201, 400, 404, 500) | D16, R3 | T-29 | O documento OpenAPI mostra descrição e respostas dos dois endpoints |
| T-36 | Escrever o `README.md` com: propósito, aviso **"não usar em produção"**, aviso de senhas em texto puro e sem autenticação, como rodar a API, como rodar os testes, exemplos de `curl` e referência à `constitution.md` | CA10, RN07, R1, R3, R9 | T-25 | Um colega consegue clonar o repositório e rodar a API apenas seguindo o README |
| T-37 | Conferir a conformidade com a constituição: (a) nenhum arquivo em `Endpoints/` referencia `AppDbContext` ou `IPasswordGenerator`; (b) `dotnet list package` mostra só o que está em D21; (c) todo erro 400 retorna `application/problem+json`; (d) nomes de classe e arquivo em inglês; (e) histórico de commits segue o formato definido | Constituição (todas as seções), R10 | T-12, T-18, T-31, T-33, T-36 | Os cinco itens verificados e sem pendências |
| T-38 | Rodar a suíte completa e conferir a matriz de cobertura abaixo: todo critério de aceite tem teste correspondente | CA01 a CA10 | T-24, T-26, T-28, T-30, T-32, T-33, T-34, T-37 | `dotnet test` passa 100% e a matriz abaixo está toda preenchida |

## Fase 10: Experiência no browser

| ID | Tarefa | Origem | Depende de | Concluída quando |
|---|---|---|---|---|
| T-39 | Adicionar o pacote `Scalar.AspNetCore`, mapear a interface Scalar em `/scalar` e citar o endereço no README | D21, D23 | T-35, T-36 | `dotnet run` sobe, `/scalar` abre no browser listando os dois endpoints e `dotnet list package` só mostra pacotes da D21 |

## Matriz de cobertura: critérios de aceite → tarefas

| Critério | Verificado por |
|---|---|
| CA01 | T-22 |
| CA02 | T-23 (e T-11 no nível do gerador) |
| CA03 | T-23 (e T-11) |
| CA04 | T-23 (e T-11) |
| CA05 | T-26 |
| CA06 | T-30 (e T-18 no nível do serviço) |
| CA07 | T-28 |
| CA08 | T-24 (e T-12) |
| CA09 | T-33 |
| CA10 | T-36 (revisão do README) |

## Matriz de cobertura: constituição → tarefas

| Regra | Verificado por |
|---|---|
| Stack (.NET 10, Minimal API, SQLite/EF Core, xUnit) | T-01, T-02, T-04 |
| Endpoint não acessa banco, sempre via serviço | T-21, T-25, T-37 (a) |
| Regra de negócio fora do endpoint | T-14, T-17, T-37 (a) |
| Nenhuma dependência sem justificativa | T-02, T-37 (b) |
| Toda regra de negócio com teste unitário | T-11, T-12, T-15, T-16, T-18 |
| Todo endpoint com teste de integração de caminho feliz | T-22 (POST), T-26 (GET) |
| Erro de validação 400 com ProblemDetails | T-27, T-28, T-37 (c) |
| Nomes em inglês, mensagens em português | T-03, T-37 (d) |
| Formato de commit com identificador do requisito | Convenção no topo, T-37 (e) |
| Mudança de constituição em commit separado | Convenção no topo |
