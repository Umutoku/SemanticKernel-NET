using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using OpenAI.Chat;
using System;
using System.Collections.Generic;
using System.Text;

namespace SemanticKernel_DEV
{
    internal class AzureOpenAI
    {
        public async Task GetAzureOpenAI()
        {
            var modelid = "gpt-4o";
            var endpoint = "https://umut-mm3nguh3-swedencentral.openai.azure.com/";
            var apiKey = "BASEKEY";

            var builder = Kernel.CreateBuilder();
            builder.AddAzureOpenAIChatCompletion(modelid, endpoint, apiKey);

            Kernel kernel = builder.Build();

            var history = new ChatHistory();

            var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

            OpenAIPromptExecutionSettings settings = new OpenAIPromptExecutionSettings
            {
                MaxTokens = 100, // Maksimum token sayısı
                Temperature = 0.7, // Yaratıcılık düzeyi (0.0 - 1.0)
                TopP = 0.9, // Nucleus sampling için kullanılan olasılık eşiği (0.0 - 1.0)
                FrequencyPenalty = 0.5, // Aynı tokenlerin tekrarını azaltmak için kullanılan ceza (0.0 - 1.0)
                PresencePenalty = 0.5 // Yeni konulara geçişi teşvik etmek için kullanılan ceza (0.0 - 1.0)
            };

            var trancationReducer = new ChatHistoryTruncationReducer(targetCount: 10); // Bu sınıf, sohbet geçmişini belirli bir mesaj sayısına kadar azaltmak için kullanılabilir. Örneğin, targetCount: 10, geçmişi son 10 mesaja kadar azaltır.
                                                                                       // SummarizationReducer, sohbet geçmişini özetleyerek belirli bir mesaj sayısına kadar azaltmak için kullanılabilir.
                                                                                       // Örneğin, targetCount: 10, geçmişi son 10 mesaja kadar özetler. thresholdCount ise özetleme işleminin ne zaman tetikleneceğini belirler.
                                                                                       // Örneğin, thresholdCount: 5, geçmişteki mesaj sayısı 5'i aştığında özetleme işlemi başlar.
            var reducer = new ChatHistorySummarizationReducer(chatCompletionService, targetCount: 10, thresholdCount: 5);
            while (true)
            {
                Console.Write("User: ");
                var userInput = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(userInput))
                {
                    Console.WriteLine("Exiting...");
                    break;
                }
                ChatTokenUsage usage = null;
                string fullMessage = "";
                history.AddUserMessage(userInput); // Kullanıcı mesajını sohbet geçmişine ekleyelim
                                                   //var response = await chatCompletionService.GetChatMessageContentAsync(history, settings);

                await foreach (StreamingChatMessageContent responseChunk in chatCompletionService.GetStreamingChatMessageContentsAsync(history, settings))
                {
                    Console.WriteLine(responseChunk.Content);
                    fullMessage += responseChunk.Content;
                    usage = ((OpenAIGen.Chat.StreamingChatCompletionUpdate)responseChunk.InnerContent).Usage;
                }


                // SSohbet geçmişine kullanıcı mesajını ekleyelim
                //history.Add(response);
                history.AddAssistantMessage(fullMessage);

                //ChatTokenUsage usage = ((ChatCompletion)response.InnerContent).Usage;
                //var assistantMessage = response.Content;
                Console.WriteLine($"Tokens Used: {usage.InputTokenCount}, Prompt Tokens: {usage.OutputTokenDetails}, Completion Tokens: {usage.TotalTokenCount}");

                var reduceMessage = trancationReducer.ReduceAsync(history); // Sohbet geçmişini belirli bir mesaj sayısına kadar azaltmak için trancationReducer'ı kullanabiliriz. Örneğin, targetCount: 10, geçmişi son 10 mesaja kadar azaltır.
                if (reduceMessage != null)
                {
                    history = [.. history]; // Sohbet geçmişini güncelleyelim
                }

            }

        }
    }
}
