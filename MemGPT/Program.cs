using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using MemGPT;
using MemGPT.Config;
using MemGPT.Contracts;
using MemGPT.Contracts.Services;
using MemGPT.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;

var builder = WebApplication.CreateBuilder(args);


// Configuration
var configuration = builder.Configuration;

var azureOpenAISection = configuration.GetSection("MemGpt:Azure:OpenAI");
var azureOpenAIOptions = azureOpenAISection.Get<AzureOpenAIOptions>();

var azureOpenAiEndpoint = azureOpenAIOptions.Endpoint;
var azureOpenAiApiKey = azureOpenAIOptions.ApiKey;
var azureOpenAiEmbeddingDeploymentName = azureOpenAIOptions.EmbeddingDeploymentName;
var azureOpenAiChatDeploymentName = azureOpenAIOptions.ChatDeploymentName;

var azureSearchSection = configuration.GetSection("MemGpt:Azure:Search");
var azureSearchOptions = azureSearchSection.Get<AzureSearchOptions>();

var azureSearchEndpoint = azureSearchOptions.Endpoint;
var azureSearchApiKey = azureSearchOptions.ApiKey;
var collectionName = azureSearchOptions.CollectionName;

var memorySettingsSection = configuration.GetSection("MemGpt:MemorySettings");
builder.Services.Configure<MemorySettingsOptions>(memorySettingsSection);

// Configuration


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
    .AddAzureOpenAIEmbeddingGenerator(
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

builder.Services.AddSingleton(_ => new SearchIndexClient(
    new Uri(azureSearchEndpoint),
    new AzureKeyCredential(azureSearchApiKey),
    new SearchClientOptions { Retry = { MaxRetries = 10, MaxDelay = TimeSpan.FromSeconds(5) } }
));

builder.Services.AddSingleton(sp => 
{
    var searchIndexClient = sp.GetRequiredService<SearchIndexClient>();
    return searchIndexClient.GetSearchClient(collectionName);
});

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

        shortTermMemory = ActivatorUtilities.CreateInstance<ShortTermMemory>(sp);

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
        longTermMemory = ActivatorUtilities.CreateInstance<LongTermMemory>(sp, userId);

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

builder.Services.AddTransient<IMigrator>(sp => new ChatMessageIndexMigrator(
    sp.GetRequiredService<SearchIndexClient>(),
    collectionName,
    azureSearchEndpoint,
    azureSearchApiKey
));

builder.Services.AddControllers();
var app = builder.Build();
app.MapControllers();

var migrators = app.Services.GetRequiredService<IEnumerable<IMigrator>>();
foreach (var migrator in migrators)
{
    await migrator.MigrateAsync();
}

await app.RunAsync();