using System;
using System.Collections.Concurrent;
using System.Net.Http;
using MemGPT;
using MemGPT.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Qdrant.Client;
using Qdrant.Client.Grpc;

var builder = WebApplication.CreateBuilder(args);


// Configuration

var vectorSize = 1536ul;
var collectionName = "chat-messages";
var azureOpenAiEmbeddingDeploymentName = "text-embedding-ada-002";
var azureOpenAiChatDeploymentName = "gpt-4.1-mini";
var azureOpenAiApiKey = "<AZURE_OPEN_AI_API_KEY>";
var azureOpenAiEndpoint = "<AZURE_OPEN_AI_ENDPOINT>";
var qdrantDbHost = "localhost";
var qdrantDbPort = 6334;
var stmCapacity = 10;
var ltmCapacity = 10;

// Configuration


#pragma warning disable SKEXP0010
builder.Services
    .AddKernel()
    .AddAzureOpenAIChatCompletion(
        deploymentName: azureOpenAiChatDeploymentName,
        endpoint: azureOpenAiEndpoint,
        apiKey: azureOpenAiApiKey,
        httpClient: new HttpClient(new HttpClientHandler
        {
            SslProtocols = System.Security.Authentication.SslProtocols.Tls12 |
                           System.Security.Authentication.SslProtocols.Tls13,
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        })
    )
    .AddAzureOpenAIEmbeddingGenerator
    (
        deploymentName: azureOpenAiEmbeddingDeploymentName,
        endpoint: azureOpenAiEndpoint,
        apiKey: azureOpenAiApiKey,
        httpClient: new HttpClient(new HttpClientHandler
        {
            SslProtocols = System.Security.Authentication.SslProtocols.Tls12 |
                           System.Security.Authentication.SslProtocols.Tls13,
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        })
    );
#pragma warning restore SKEXP0010

builder.Services.AddSingleton(_ => new QdrantClient(qdrantDbHost, qdrantDbPort));
builder.Services.AddTransient<IEmbeddingService, EmbeddingService>();
builder.Services.AddSingleton<Func<string, IShortTermMemory>>(sp =>
{
    var cache = new ConcurrentDictionary<string, IShortTermMemory>();

    return userId =>
    {
        if (string.IsNullOrEmpty(userId))
        {
            return null;
        }

        if (cache.TryGetValue(userId, out var shortTermMemory))
        {
            return shortTermMemory;
        }

        shortTermMemory = ActivatorUtilities.CreateInstance<ShortTermMemory>(sp, stmCapacity);

        cache[userId] = shortTermMemory;

        return shortTermMemory;
    };
});
builder.Services.AddSingleton<Func<string, ILongTermMemory>>(sp =>
{
    var cache = new ConcurrentDictionary<string, ILongTermMemory>();

    return userId =>
    {
        if (string.IsNullOrEmpty(userId))
        {
            return null;
        }

        if (cache.TryGetValue(userId, out var longTermMemory))
        {
            return longTermMemory;
        }

        longTermMemory = ActivatorUtilities.CreateInstance<LongTermMemory>(sp, userId, collectionName, ltmCapacity);

        cache[userId] = longTermMemory;

        return longTermMemory;
    };
});
builder.Services.AddSingleton<Func<string, Kernel>>(sp =>
{
    var cache = new ConcurrentDictionary<string, Kernel>();

    return userId =>
    {
        if (string.IsNullOrEmpty(userId))
        {
            return null;
        }

        if (cache.TryGetValue(userId, out var kernel))
        {
            return kernel;
        }

        kernel = sp.GetRequiredService<Kernel>();

        cache[userId] = kernel;

        return kernel;
    };
});
builder.Services.AddTransient<IMemoryManager, MemoryManager>();
builder.Services.AddTransient<IPromptBuilder, PromptBuilder>();
builder.Services.AddSingleton<IMemoryDeletionWorker, MemoryDeletionWorker>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<IMemoryDeletionWorker>() as MemoryDeletionWorker);

builder.Services.AddControllers();
var app = builder.Build();
app.MapControllers();

var qdrantClient = app.Services.GetRequiredService<QdrantClient>();
if (!await qdrantClient.CollectionExistsAsync(collectionName))
{
    var vectorParams = new VectorParams
    {
        Size = vectorSize,
        Distance = Distance.Cosine
    };

    await qdrantClient.CreateCollectionAsync(collectionName, vectorParams);
}

app.Run();