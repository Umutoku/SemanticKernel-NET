using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureAIInference;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using Microsoft.SemanticKernel.Connectors.Ollama;
using Microsoft.SemanticKernel.Connectors.Onnx;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using OllamaSharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace SemanticKernel_DEV
{
    internal class LMStudio
    {
        public async Task LMStudioGen()
        {
            // Build and get configuration from appsettings.json, environment variables, and user secrets
            IConfiguration config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .AddUserSecrets<Program>()
                .Build();

            // Validate required configuration values to avoid null-reference diagnostics
            var modelId = config["LMStudio:modelid"] ?? throw new InvalidOperationException("Missing configuration: 'LMStudio:modelid'.");
            var endpointStr = config["LMStudio:endpoint"] ?? throw new InvalidOperationException("Missing configuration: 'LMStudio:endpoint'.");

            if (!Uri.TryCreate(endpointStr, UriKind.Absolute, out var endpointUri))
            {
                throw new InvalidOperationException($"Configuration value for 'LMStudio:endpoint' is not a valid absolute URI: '{endpointStr}'.");
            }

            // The OpenAIChatCompletionService constructor used here is marked as evaluation-only by the SDK analyzer.
            // Suppress the analyzer diagnostic locally while keeping the runtime behavior intact.
#pragma warning disable SKEXP0010
            OpenAIChatCompletionService chatCompletionService = new(modelId, endpointUri);
#pragma warning restore SKEXP0010

            // Create chat history
            var history = new ChatHistory(systemMessage: "You are a friendly AI Assistant that answers in a friendly manner");

            // Define settings for OpenAI prompt execution
            AzureOpenAIPromptExecutionSettings settings = new()
            {
                Temperature = 0.9f
            };

            // Create a chat history truncation reducer
            var reducer = new ChatHistoryTruncationReducer(targetCount: 10);
            // var reducer = new ChatHistorySummarizationReducer(chatCompletionService, 2, 2);

            foreach (var attr in chatCompletionService.Attributes)
                Console.WriteLine($"{attr.Key} \t\t{attr.Value}");

            // Control loop for user interaction
            while (true)
            {
                // Get input from user
                Console.Write("\nEnter your prompt: ");
                var prompt = Console.ReadLine();

                // Exit if prompt is null or empty
                if (string.IsNullOrEmpty(prompt))
                    break;

                string fullMessage = "";

                history.AddUserMessage(prompt);
                // Get streaming response from chat completion service
                await foreach (StreamingChatMessageContent responseChunk in chatCompletionService.GetStreamingChatMessageContentsAsync(history, settings))
                {
                    // Print response to console
                    Console.Write(responseChunk.Content);
                    fullMessage += responseChunk.Content;
                }
                // Add response to chat history
                history.AddAssistantMessage(fullMessage);

                // Reduce chat history if necessary
                var reduceMessages = await reducer.ReduceAsync(history);
                if (reduceMessages is not null)
                    history = new(reduceMessages);
            }

        }
    }
}
