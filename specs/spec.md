# Gerador de Senhas Fortes

## Problema
Criar senhas fortes manualmente é trabalhoso e propenso a padrões previsíveis. Além disso, depois de geradas, é fácil perder a senha ou esquecer onde ela foi usada. Este projeto é um desafio inicial, de caráter lúdico, para praticar os conceitos de SDD (Spec-Driven Development) construindo uma API pequena e bem definida.

> **Aviso:** este é um app de aprendizado. Não deve ser utilizado em produção.

## Objetivo
Disponibilizar uma API simples com dois endpoints:

1. Um que gera uma senha forte seguindo um padrão definido, armazena-a no banco e devolve um GUID.
2. Outro que, dado um GUID, retorna a senha associada.

## Usuários
- **Pessoa desenvolvedora / estudante:** consome a API (via curl, Postman, Swagger etc.) para gerar e consultar senhas, e usa o projeto para aprender SDD.
- **Aplicação cliente:** qualquer sistema que precise de uma senha forte e queira recuperá-la depois usando o GUID.

## Histórias
- **H1:** Como usuário, quero solicitar a geração de uma senha forte para não ter que inventá-la.
- **H2:** Como usuário, quero receber um GUID junto com a senha gerada para poder consultá-la depois.
- **H3:** Como usuário, quero informar um GUID e obter a senha associada para recuperá-la quando precisar.
- **H4:** Como usuário, quero receber uma mensagem de erro clara quando o GUID for inválido ou não existir.

## Requisitos funcionais
- **RF01 – Gerar senha:** `POST /senhas` gera uma nova senha forte, persiste no banco e retorna `201 Created` com o GUID e a senha.
  - Resposta de exemplo:
    ```json
    { "id": "3f2b8c1e-5d4a-4e7b-9a10-2c6f8d9e1a34", "senha": "k#9Vp!2xQ@m7Zt&L" }
    ```
- **RF02 – Consultar senha:** `GET /senhas/{id}` recebe um GUID e retorna `200 OK` com a senha associada.
  - Resposta de exemplo:
    ```json
    { "id": "3f2b8c1e-5d4a-4e7b-9a10-2c6f8d9e1a34", "senha": "k#9Vp!2xQ@m7Zt&L" }
    ```
- **RF03 – Persistência:** toda senha gerada é armazenada no banco de dados, associada ao seu GUID.
- **RF04 – Erros:** o `GET` retorna `400 Bad Request` para GUID com formato inválido e `404 Not Found` quando o GUID não existe. O corpo do erro segue o padrão ProblemDetails (RFC 9457) e deve conter uma mensagem legível em português.
  - Exemplo: `{ "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5", "title": "Senha não encontrada.", "status": 404 }`

## Regras de negócio
- **RN01:** a senha deve ter **no mínimo 16 caracteres** (a API gera senhas com exatamente 16).
- **RN02:** a senha deve conter **pelo menos um caractere especial**.
- **RN03:** a senha **não pode conter espaços em branco** (espaço, tabulação, quebra de linha).
- **RN04:** a senha deve conter ao menos uma letra maiúscula, uma minúscula e um dígito, além do caractere especial.
- **RN05:** a geração deve usar uma fonte de aleatoriedade adequada (por exemplo, o gerador criptográfico da plataforma), não uma sequência previsível.
- **RN06:** cada senha gerada recebe um GUID (v4) único, que é o identificador público para consulta.
- **RN07:** a senha é armazenada em **texto puro**. Isso é uma decisão consciente para este exercício e deve estar documentada no README.
- **RN08:** uma senha, uma vez gerada, não é alterada nem removida pela API.

## Casos de borda
- GUID em formato inválido (ex.: `abc`, string vazia) → `400`.
- GUID válido, mas inexistente no banco → `404`.
- GUID com letras maiúsculas ou minúsculas → deve ser tratado de forma equivalente.
- Duas chamadas consecutivas a `POST /senhas` → devem retornar senhas e GUIDs diferentes.
- Falha ao gravar no banco → `500` sem devolver senha nem GUID (a senha só é retornada se foi persistida).
- Conjunto de caracteres especiais: usar apenas símbolos imprimíveis ASCII (ex.: `!@#$%^&*()-_=+[]{}`), evitando caracteres que causem problemas em JSON ou URLs.
- `POST /senhas` sem corpo → deve funcionar normalmente, pois não há parâmetros de entrada.

## Fora de escopo
- Criptografia ou hash das senhas armazenadas.
- Autenticação, autorização e controle de acesso.
- Rate limiting, auditoria e logs de segurança.
- Expiração, atualização ou exclusão de senhas.
- Parâmetros de personalização (tamanho, conjunto de caracteres).
- Interface gráfica (front-end).
- Deploy em produção, alta disponibilidade e escalabilidade.

## Critérios de aceite
- [ ] **CA01:** `POST /senhas` retorna `201` com um `id` (GUID válido) e uma `senha`.
- [ ] **CA02:** A senha retornada tem 16 ou mais caracteres.
- [ ] **CA03:** A senha retornada contém ao menos um caractere especial, uma maiúscula, uma minúscula e um dígito.
- [ ] **CA04:** A senha retornada não contém nenhum espaço em branco.
- [ ] **CA05:** Após gerar uma senha, `GET /senhas/{id}` com o GUID retornado devolve `200` e exatamente a mesma senha.
- [ ] **CA06:** `GET /senhas/{id}` com um GUID inexistente retorna `404`.
- [ ] **CA07:** `GET /senhas/{id}` com um valor que não é GUID retorna `400`.
- [ ] **CA08:** Duas chamadas a `POST /senhas` geram GUIDs e senhas distintos.
- [ ] **CA09:** Os dados persistem no banco (a consulta continua funcionando após reiniciar a aplicação, se o banco não for em memória).
- [ ] **CA10:** O README informa que o projeto é apenas para aprendizado e que as senhas não são criptografadas.
