\# Partilhaê — API .NET



\## Integrantes



| Nome | RM | Turma |

|---|---|---|

| Agatha Yie Won Yun | 561507 | 2TDSA |

| Ana Claudia Fernandes Martins | 561190 | 2TDSR |

| André Rosa Colombo | 563112 | 2TDSA |

| Samantha Faruolo Galdi | 554794 | 2TDSA |

| Vitor Fria Dalmagro | 566052 | 2TDSA |



\## Sobre o projeto



O Partilhaê é uma solução CitizenTech desenvolvida para conectar

doadores e pessoas que necessitam de ajuda, facilitando o acesso

a doações e promovendo a solidariedade.



A API .NET é responsável pelo gerenciamento de solicitações,

conversas, mensagens e doações, disponibilizando funcionalidades

para integração com o aplicativo Mobile.



\## Links do projeto



| Recurso | Link |

|---|---|

| Aplicação online | \[API Partilhaê](https://partilhae-net.onrender.com) |

| Documentação | \[Swagger](https://partilhae-net.onrender.com/swagger/index.html) |

| Health Check | \[Health](https://partilhae-net.onrender.com/health) |



\## Tecnologias utilizadas



\- C#

\- .NET 10

\- ASP.NET Core Minimal APIs

\- Entity Framework Core

\- Oracle Database

\- SQLite

\- Swagger / OpenAPI

\- JWT

\- xUnit

\- Coverlet

\- Docker

\- Render



\## Funcionalidades



\- Criação e consulta de solicitações de ajuda.

\- Atualização do status das solicitações.

\- Criação e consulta de conversas.

\- Envio e consulta de mensagens.

\- Registro e acompanhamento de doações.

\- Confirmação de entrega e recebimento.

\- Autenticação e controle de acesso.

\- Proteção contra requisições duplicadas.

\- Limitação de requisições.

\- Monitoramento da saúde da aplicação.



\## Arquitetura



A aplicação utiliza uma arquitetura em camadas, separando

os endpoints, regras de negócio, contratos de comunicação

e acesso ao banco de dados.



Fluxo simplificado:



```mermaid

flowchart TD

&#x20;   Mobile\[Aplicativo Mobile]

&#x20;   Java\[API Java - Autenticação]

&#x20;   Net\[API .NET]

&#x20;   Endpoints\[Endpoints]

&#x20;   Services\[Services]

&#x20;   EF\[Entity Framework Core]

&#x20;   Oracle\[(Oracle Database)]



&#x20;   Mobile -->|Login| Java

&#x20;   Java -->|JWT| Mobile

&#x20;   Mobile -->|Requisições com JWT| Net

&#x20;   Net --> Endpoints

&#x20;   Endpoints --> Services

&#x20;   Services --> EF

&#x20;   EF --> Oracle

&#x20;   Java --> Oracle

```



\## Estrutura do projeto



```text

Partilhae\_Net/

├── Partilhae\_Net/

│   ├── Data/

│   ├── DTOs/

│   ├── Endpoints/

│   ├── HealthChecks/

│   ├── Middlewares/

│   ├── Models/

│   ├── Security/

│   ├── Services/

│   ├── Validators/

│   ├── Program.cs

│   └── appsettings.json

├── Partilhae\_Net.Tests/

│   ├── Endpoints/

│   ├── Helpers/

│   ├── Integration/

│   ├── Services/

│   └── Validators/

├── Dockerfile

├── .dockerignore

├── .gitignore

├── coverlet.runsettings

└── README.md

```



\## Endpoints



\### Solicitações



| Método | Rota | Descrição |

|---|---|---|

| POST | `/solicitacoes` | Criar solicitação |

| GET | `/solicitacoes` | Listar solicitações |

| GET | `/solicitacoes/{id}` | Buscar solicitação |

| GET | `/solicitacoes/receptor/{idReceptor}` | Listar solicitações do receptor |

| PATCH | `/solicitacoes/{id}/status` | Atualizar status |



\### Conversas



| Método | Rota | Descrição |

|---|---|---|

| POST | `/conversas` | Criar ou buscar conversa |

| GET | `/conversas/usuario/{idUsuario}` | Listar conversas do usuário |

| GET | `/conversas/{idConversa}/mensagens` | Listar mensagens |

| POST | `/conversas/{idConversa}/mensagens` | Enviar mensagem |



\### Doações



| Método | Rota | Descrição |

|---|---|---|

| POST | `/doacoes` | Registrar doação |

| GET | `/doacoes/doador/{idDoador}` | Listar doações do doador |

| GET | `/doacoes/solicitacao/{idSolicitacao}` | Consultar doação pela solicitação |

| PATCH | `/doacoes/{id}/status` | Atualizar status da doação |



Os parâmetros, contratos de entrada e saída e códigos HTTP

estão documentados no Swagger.



\## Autenticação e autorização



A API utiliza JWT integrado à API Java do Partilhaê.



O token é gerado durante o login na API Java e reutilizado

nas requisições para a API .NET.



```http

Authorization: Bearer TOKEN

```



O controle de acesso considera os perfis:



\- ROLE\_DOADOR

\- ROLE\_RECEPTOR



Além do perfil, a aplicação verifica a identidade do usuário

e sua relação com os recursos acessados.



Requisições sem autenticação válida retornam HTTP 401.

Acessos sem permissão retornam HTTP 403.



\## Idempotência



Os endpoints POST aceitam o header opcional:



```http

Idempotency-Key: chave-unica-da-operacao

```



Ao repetir uma requisição com a mesma chave e os mesmos dados,

a API retorna a resposta bem-sucedida anteriormente armazenada,

evitando a duplicação da operação.



\- Chaves com até 100 caracteres.

\- Respostas armazenadas por 30 minutos.

\- Controle por usuário, rota e chave.

\- Conflitos de reutilização retornam HTTP 409.

\- Chaves inválidas retornam HTTP 400.



O armazenamento utiliza cache em memória, portanto não persiste

após a reinicialização da aplicação.



\## Rate Limiting



A API utiliza o mecanismo de Rate Limiting do ASP.NET Core.



\- Limite de 100 requisições por minuto.

\- Controle por usuário autenticado ou endereço IP.

\- Retorno HTTP 429 quando o limite é excedido.



\## Health Checks



| Endpoint | Descrição |

|---|---|

| `/health` | Verifica a saúde geral da aplicação |

| `/health/api` | Verifica a saúde da API |

| `/health/db` | Verifica a conexão com o Oracle |



A API .NET realiza a validação do JWT localmente, sem depender

de chamadas diretas à API Java durante as requisições.



\## Testes automatizados



O projeto utiliza xUnit para testes unitários e de integração,

abrangendo:



\- Validações.

\- Regras de negócio.

\- Endpoints REST.

\- Autenticação JWT.

\- Controle de acesso.

\- Idempotência.

\- Rate Limiting.

\- Health Checks.



Os testes utilizam SQLite em memória e implementações

simuladas para funcionalidades específicas do Oracle,

preservando os dados do banco compartilhado.



\### Resultados



| Indicador | Resultado |

|---|---|

| Total de testes | 249 |

| Testes aprovados | 249 |

| Testes com falha | 0 |

| Taxa de aprovação | 100% |

| Cobertura de linhas | 89,76% |

| Cobertura de branches | 75,41% |



\### Executar os testes



```bash

dotnet test

```



\### Gerar relatório de cobertura



```bash

dotnet test --settings coverlet.runsettings --collect:"XPlat Code Coverage"

```



\## Como executar localmente



\### Pré-requisitos



\- .NET SDK 10

\- Git

\- Acesso ao banco Oracle do projeto

\- Chave JWT compartilhada com a API Java



\### Clonar o repositório



```bash

git clone https://github.com/Partilhae/Partilhae\_Net.git

cd Partilhae\_Net

```



\### Restaurar as dependências



```bash

dotnet restore

```



\### Configurar as credenciais



As credenciais devem ser configuradas utilizando User Secrets.



```bash

dotnet user-secrets set "ConnectionStrings:Oracle" "SUA\_STRING\_DE\_CONEXAO" --project Partilhae\_Net

```



```bash

dotnet user-secrets set "JWT\_SECRET" "SUA\_CHAVE\_JWT" --project Partilhae\_Net

```



A chave JWT deve ser a mesma utilizada pela API Java.



\### Executar a aplicação



```bash

dotnet run --project Partilhae\_Net

```



Acesse o endereço informado no terminal, seguido de `/swagger`,

para visualizar a documentação interativa.



\## Deploy



A aplicação está hospedada no Render utilizando Docker.



As credenciais são configuradas por meio de variáveis de

ambiente, sem exposição no repositório:



\- `ConnectionStrings\_\_Oracle`

\- `JWT\_SECRET`



A hospedagem utiliza o plano gratuito do Render, podendo

apresentar demora na primeira requisição após um período

de inatividade.



\## Projeto acadêmico



Desenvolvido para a Global Solution 2026 da FIAP,

no curso de Análise e Desenvolvimento de Sistemas.



Tema: CitizenTech.

