# Projeto de Exemplo: Consumidor SQS em .NET com Kubernetes

Este projeto demonstra uma arquitetura de microsserviços desacoplada, utilizando uma API em C# (.NET) para consumir mensagens de uma fila SQS, com orquestração e escalabilidade gerenciadas pelo Kubernetes.

O objetivo é ilustrar o fluxo completo de uma aplicação de processamento de background, desde o desenvolvimento local até o deploy em um ambiente de contêineres.

## Tecnologias Utilizadas

- **.NET 8**
- **Amazon SQS** (simulado por LocalStack)
- **C#**
- **Docker**
- **Kubernetes**
- **AWS CLI** (para interagir com SQS)
- **LocalStack** (para simular serviços AWS localmente)

## Arquitetura 🗺️

A arquitetura do projeto é baseada em um padrão de produtor/consumidor, onde um serviço (a AWS CLI, em nosso caso) atua como produtor de mensagens, e nossa API .NET atua como consumidor. A orquestração e a escalabilidade são gerenciadas pelo Kubernetes.

```
+------------------------------------+      +---------------------------+
| [Sua Máquina com aws-cli]          |      |                           |
|   (Produtor de Mensagem)           |      |  [Pod da API .NET]        |
|                                    |      |    (Consumidor SQS)       |
|   -- (Via port-forward) -->        |      |                           |
+------------------------------------+      +---------------------------+
         |                                                 ^
         |                                                 |
         v (envia mensagem)                (puxa mensagens)
  +-------------------------------------------------------------+
  |              [ Service (localstack-service) ]               |
  |                           |                                 |
  |                           v                                 |
  |             [ Pod do LocalStack (SQS) ]                     |
  +-------------------------------------------------------------+
               (Cluster Kubernetes no Docker Desktop)
```

## Pré-requisitos 🚀

Certifique-se de que os seguintes softwares estão instalados na sua máquina:

1.  **.NET 8 SDK**
2.  **Docker Desktop** (com o Kubernetes habilitado nas configurações)
3.  **kubectl** (normalmente já instalado com o Docker Desktop)
4.  **AWS CLI**

## Como Executar o Projeto

Siga os passos abaixo em um terminal (PowerShell, Bash, etc.).

### Passo 1: Preparar o Ambiente Local

Clone o repositório e navegue até a pasta do projeto .NET.

```bash
git clone [https://github.com/](https://github.com/)[SEU-USUARIO]/SqsConsumerApi.git
cd SqsConsumerApi/SqsConsumerApi
```

### Passo 2: Construir e Enviar a Imagem Docker

Crie a imagem do seu contêiner e envie-a para o Docker Hub, para que o Kubernetes possa acessá-la.

```bash
# 1. Construa a imagem
docker build -t sqs-consumer-api .

# 2. Etiquete a imagem para o Docker Hub
#    Substitua '[SEU-USUARIO]' pelo seu nome de usuário do Docker Hub
docker tag sqs-consumer-api [SEU-USUARIO]/sqs-consumer-api:v1

# 3. Envie a imagem para o Docker Hub
docker push [SEU-USUARIO]/sqs-consumer-api:v1
```

### Passo 3: Fazer o Deploy no Kubernetes

Certifique-se de que seu `kubectl` está apontando para o cluster do Docker Desktop e, em seguida, aplique os manifestos.

```bash
# Mude para a pasta raiz do projeto
cd ..

# Garanta que o kubectl está usando o contexto do Docker Desktop
kubectl config use-context docker-desktop

# Aplique todos os manifests (Deployments, Services, ConfigMap, HPA)
kubectl apply -f k8s/deployment.yaml
```

### Passo 4: Verificação e Teste 🧪

1.  **Monitore a criação dos Pods:**
    ```bash
    kubectl get pods -w
    ```
    Aguarde até que os pods `localstack-deployment` e `sqs-consumer-deployment` estejam com o status `Running`.

2.  **Crie a Fila SQS:**
    Em um **novo terminal**, crie uma ponte de comunicação para o LocalStack e, em seguida, crie a fila.
    ```bash
    # Deixe este comando rodando em um terminal
    kubectl port-forward svc/localstack-service 6060:4566

    # Em outro terminal, crie a fila
    aws sqs create-queue --queue-name minha-fila-processamento --endpoint-url http://localhost:6060 --region us-east-1
    ```

3.  **Envie Mensagens e Observe a Escalabilidade:**
    Em um terceiro terminal, envie mensagens em massa e observe os logs do seu consumidor e o HPA em ação.
    ```bash
    # Envie 50 mensagens para a fila (Exemplo para PowerShell)
    foreach ($i in 1..50) { aws sqs send-message --queue-url http://localhost:6060/000000000000/minha-fila-processamento --message-body "Mensagem numero $i" --endpoint-url http://localhost:6060 --region us-east-1 }

    # Em outro terminal, monitore o HPA
    kubectl get hpa -w
    ```
    Você verá a coluna `REPLICAS` aumentar para lidar com a carga.

4.  **Veja os Logs do Consumidor:**
    ```bash
    # Pegue o nome do seu pod (kubectl get pods) e rode
    kubectl logs -f <nome-do-pod-consumidor>
    ```

## Conceitos Chave Aprendidos

-   **Arquitetura Desacoplada:** A separação do "produtor" (que envia a mensagem) e do "consumidor" (que processa a mensagem) via uma fila central.
-   **Containerização e Imutabilidade:** Empacotar a aplicação e suas dependências em uma imagem Docker, garantindo que ela rode da mesma forma em qualquer ambiente.
-   **Orquestração de Contêineres:** Uso do Kubernetes para automatizar o deploy, escalabilidade e gerenciamento dos contêineres da aplicação.
-   **Service, Deployment e ConfigMap:** Compreensão dos recursos fundamentais do Kubernetes para gerenciar a aplicação, seu endereço de rede e suas configurações.
-   **Escalabilidade Automática (HPA):** Configuração do Horizontal Pod Autoscaler para adicionar ou remover réplicas do consumidor automaticamente com base na carga de trabalho.
-   **Desenvolvimento Cloud-Native:** Utilização de ferramentas como LocalStack para simular serviços de nuvem de forma local, agilizando o desenvolvimento.

## Próximos Passos (Evoluindo o Projeto)

Este projeto serve como uma base sólida. Para levá-lo ao próximo nível, você pode:
-   Configurar uma pipeline de **CI/CD** (ex: GitHub Actions) para automatizar o `docker build`, `push` e `kubectl apply` a cada `git push`.
-   Migrar a aplicação para um ambiente de produção real na nuvem (ex: **AWS EKS** com **Amazon SQS** real).
-   Adicionar **Dead Letter Queue** para lidar com mensagens que falham no processamento.
-   Implementar um sistema de **monitoramento e alertas** (ex: Prometheus e Grafana).

