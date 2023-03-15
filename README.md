# Dapr AI Bindings

A sample of using Dapr as an abstraction over several AI services.

## Prerequisites

- [Dapr 1.10](https://dapr.io/) or later
- [.NET 7](https://dotnet.microsoft.com/) or later
- Linux, MacOS, or Windows using WSL

To use the Chat GPT binding, you must have an Open AI account and an [API key](https://platform.openai.com/account/api-keys).

To use the Azure AI binding, you must have an [Azure Cognitive Services](https://azure.microsoft.com/en-us/products/cognitive-services/) instance and its access key.

To use the Azure Open AI binding, you must have an [Azure Open AI](https://azure.microsoft.com/en-us/products/cognitive-services/openai-service) instance and its access key.

To use the `.http` files to send requests, install the [REST Client](https://marketplace.visualstudio.com/items?itemName=humao.rest-client) VS Code extension.

## Setup

1. Configure the Dapr components.

   Create a JSON file called `secrets.json` in the repo root folder to hold the access keys. It should look something like:

   ```json
   {
      "azure-ai-endpoint": "https://<name>.cognitiveservices.azure.com",
      "azure-ai-key": "<Azure Cognitive Services key>",

      "azure-open-ai-deployment": "<deployed language model name>"
      "azure-open-ai-endpoint": "https://<name>.openai.azure.com"
      "azure-open-ai-key": "<Azure Open AI API key>"

      "open-api-endpoint": "https://api.openai.com"
      "open-api-key": "<Open AI API key>"
   }
   ```

1. Build and run the pluggable components.

   ```bash
   cd src/DaprAI.PluggableComponents
   dotnet run
   ```

1. Build and run the application.

   ```bash
   dapr run -f ./dapr.yaml
   ```

1. Send a prompt request:

   ```http
   POST http://localhost:5111/prompt HTTP/1.1
   content-type: application/json

   {
       "prompt": "How are you?"
   }
   ```

   See the response from Chat GPT:

   ```http
   HTTP/1.1 200 OK
   Connection: close
   Content-Type: application/json; charset=utf-8
   Date: Mon, 06 Mar 2023 22:01:23 GMT
   Server: Kestrel
   Transfer-Encoding: chunked

   {
     "response": "\n\nI'm doing well, thanks for asking. How are you?"
   }
   ```

   > By default, requests will be made to Chat GPT. You can use the query parameter to specify which binding to use, Chat GPT (e.g. `?component=chat-gpt`) or Azure AI (e.g. `?component=azure-ai`) Not all operations (i.e. prompt or summarize) are supported by all bindings.