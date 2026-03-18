using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureAIInference;
using Microsoft.SemanticKernel.Connectors.Onnx;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System;
using System.Collections.Generic;
using System.Text;

namespace SemanticKernel_DEV
{
    internal class Onnx
    {
        public async Task OnnxGen()
        {
            // Build and get configuration from appsettings.json, environment variables, and user secrets
            IConfiguration config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .AddUserSecrets<Program>()
                .Build();

            /* 
 * git clone --no-checkout https://huggingface.co/microsoft/Phi-3-mini-4k-instruct-onnx
 * cd .\Phi-3-mini-4k-instruct-onnx\
 * git sparse-checkout init
 * git sparse-checkout set cpu_and_mobile\cpu-int4-rtn-block-32-acc-level-4
 * 
 */

            // Create a kernel builder and add Azure OpenAI chat completion service
            //var builder = Kernel.CreateBuilder();

            //Azure OpenAI
            //builder.AddAzureOpenAIChatCompletion(config["modelid"], config["endpoint"], config["apikey"]);

            //OpenAI
            //builder.AddOpenAIChatCompletion(config["OpenAI:modelid"], config["OpenAI:apikey"]);

            // yerel model için onnx runtime api'si
            //builder.AddOnnxRuntimeGenAIChatCompletion(config["onnx:modelid"], config["onnx:modelpath"]);

            //// Build the kernel
            //Kernel kernel = builder.Build();

            using OnnxRuntimeGenAIChatCompletionService chatCompletionService = new(config["onnx:modelid"], config["onnx:modelpath"]);

            // Create chat history
            // azure ai inference api'si, system prompt'u desteklemediği için, system prompt'u chat history'e ekleyelim.
            var history = new ChatHistory(systemMessage: "You are a friendly AI Assistant that answers in a friendly manner");

            // Get reference to chat completion service
            //var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

            // Define settings for OpenAI prompt execution
            OnnxRuntimeGenAIPromptExecutionSettings settings = new()
            {
                Temperature = 0.9f,
                MaxTokens = 1500,
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
                OpenAI.Chat.ChatTokenUsage usage = null;

                history.AddUserMessage(prompt);
                // Get streaming response from chat completion service
                await foreach (StreamingChatMessageContent responseChunk in chatCompletionService.GetStreamingChatMessageContentsAsync(history, settings))
                {
                    // Print response to console
                    Console.Write(responseChunk.Content);
                    fullMessage += responseChunk.Content;
                    usage = ((OpenAI.Chat.StreamingChatCompletionUpdate)responseChunk.InnerContent).Usage;
                }
                // Add response to chat history
                history.AddAssistantMessage(fullMessage);


                //get non-streaming result from chat completion setvice
                //var response = await chatCompletionService.GetChatMessageContentAsync(history, settings);
                //add response to chat history
                //history.Add(response);

                // Display number of tokens used (model specific)
                Console.WriteLine($"\n\tInput Tokens: \t{usage?.InputTokenCount}");
                Console.WriteLine($"\tOutput Tokens: \t{usage?.OutputTokenCount}");
                Console.WriteLine($"\tTotal Tokens: \t{usage?.TotalTokenCount}");

                // Reduce chat history if necessary
                var reduceMessages = await reducer.ReduceAsync(history);
                if (reduceMessages is not null)
                    history = new(reduceMessages);
            }
            // bunun nedeni onnx runtime gen ai chat completion servisinin IDisposable implement etmesi. Kernel tarafından yönetilen diğer servisler gibi, kernelin Dispose edilmesiyle birlikte onnx runtime gen ai chat completion servisi de dispose edilir. Ancak, burada kernelin dispose edilmediği için, onnx runtime gen ai chat completion servisinin dispose edilmesi gerekiyor. Eğer kernel dispose edilirse, onnx runtime gen ai chat completion servisi de dispose edilir ve bu durumda tekrar dispose etmeye gerek kalmaz.
            //((OnnxRuntimeGenAIChatCompletionService)chatCompletionService).Dispose();
        }
    }
}
