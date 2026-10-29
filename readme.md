<img width="100%" alt="Halloween 2026" src="https://baltaio.blob.core.windows.net/static/images/v4/challenges/halloween-2026/banner.jpg" />

## 🎃 Halloween - Desafio 1

Oi, eu sou o Rafael Santiago e este é o espaço onde compartilho minha jornada de aprendizado durante o desafio **Halloween 2026**, realizado pelo [balta.io](https://balta.io). 👻

Aqui você vai encontrar projetos, exercícios e códigos que estou desenvolvendo durante o desafio.

> ⚠️ **Não usar em produção.** Este é um projeto de aprendizado. As senhas são armazenadas em **texto puro** (sem criptografia nem hash) e a API **não tem autenticação**: qualquer pessoa que tenha o GUID consegue ler a senha.

### Sobre este desafio
Neste desafio o objetivo é consolidar os fundamentos do Spec Driven Development, criando uma constituição, especificação, planejamento e tarefas manualmente para serem implementadas pela IA posteriormente.

As especificações estão em [`specs/`](specs/):

- [`constitution.md`](specs/constitution.md): regras do projeto (stack, arquitetura, qualidade, convenções e governança).
- [`spec.md`](specs/spec.md): o quê e por quê (requisitos, regras de negócio e critérios de aceite).
- [`plan.md`](specs/plan.md): como (decisões técnicas com justificativa, modelo de dados e contratos).
- [`tasks.md`](specs/tasks.md): tarefas pequenas e verificáveis, cada uma ligada ao requisito de origem.

#### O que foi implementado

- API em .NET 10 (Minimal API) com dois endpoints:
  - `POST /senhas`: gera uma senha forte, grava no banco e retorna `201` com `id` (GUID) e `senha`.
  - `GET /senhas/{id}`: retorna a senha do GUID (`200`), `400` se o id não for um GUID e `404` se não existir.
- Senhas com 16 caracteres, com ao menos uma maiúscula, uma minúscula, um dígito e um caractere especial (`!@#$%^&*()-_=+[]{}`), sem espaços, geradas com `RandomNumberGenerator`.
- Persistência em SQLite via EF Core, com migration aplicada automaticamente ao subir a API.
- Erros no formato ProblemDetails (`application/problem+json`) com mensagens em português.
- Documento OpenAPI em `/openapi/v1.json` e interface Scalar em `/scalar` para testar no browser.
- Testes com xUnit: unitários do gerador e do serviço, e de integração para cada critério de aceite (`CA01` a `CA09`).

Neste processo eu aprendi:
* ✅

## Bagde
<img src="https://baltaio.blob.core.windows.net/static/images/v4/challenges/halloween-2026/01.png" width="200" />

## Problema
Criar senhas fortes manualmente é trabalhoso e propenso a padrões previsíveis, e depois de geradas é fácil perdê-las. Esta API gera uma senha forte, guarda e permite recuperá-la depois pelo GUID. É um projeto lúdico, feito para praticar SDD.

## Como rodar

Pré-requisito: [SDK do .NET 10](https://dotnet.microsoft.com/download).

```bash
git clone https://github.com/rafaelmsantiago/balta-desafio-halloween-2026-spec-driven-01.git
cd balta-desafio-halloween-2026-spec-driven-01
dotnet run --project src/PasswordApi
```

A API sobe em `http://localhost:5187`. Na primeira execução é criado o arquivo `src/PasswordApi/senhas.db` com a tabela `Passwords`. Se o arquivo for apagado, as senhas gravadas são perdidas.

### Exemplos com `curl`

Gerar uma senha:

```bash
curl -i -X POST http://localhost:5187/senhas
```

```json
{ "id": "3f2b8c1e-5d4a-4e7b-9a10-2c6f8d9e1a34", "senha": "k#9Vp!2xQ@m7Zt&L" }
```

Consultar pelo GUID retornado:

```bash
curl -i http://localhost:5187/senhas/3f2b8c1e-5d4a-4e7b-9a10-2c6f8d9e1a34
```

Erros:

```bash
curl -i http://localhost:5187/senhas/abc                                   # 400: Identificador inválido.
curl -i http://localhost:5187/senhas/00000000-0000-0000-0000-000000000001  # 404: Senha não encontrada.
```

### Testando no browser

Com a API rodando, abra **`http://localhost:5187/scalar`**. A interface [Scalar](https://github.com/scalar/scalar) lista os dois endpoints e permite chamar o `POST` e o `GET` direto do browser (botão **Test Request**).

O documento OpenAPI bruto fica em `http://localhost:5187/openapi/v1.json`.

## Como rodar os testes

```bash
dotnet test
```

Os testes usam SQLite em memória (e um arquivo temporário no teste de persistência `CA09`), então não mexem no `senhas.db`.

Para criar novas migrations, restaure a ferramenta local `dotnet-ef` com `dotnet tool restore`.

## Sobre o Halloween 2026
O desafio **Halloween 2026** consiste em implementar implementar o modelo Spec Driven Development de ponta a ponta, criando apps completas com IA.

### Veja meu progresso no desafio
https://github.com/balta-io/balta-desafio-halloween-2026-spec-driven-01
