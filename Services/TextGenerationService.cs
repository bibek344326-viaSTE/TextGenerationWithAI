using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Text;
using System.Text.Json;
using TextGenerationWithAI.Data;
using TextGenerationWithAI.DTOs;
using TextGenerationWithAI.Models;

namespace TextGenerationWithAI.Services
{
    public class TextGenerationService(
        HttpClient httpClient,
        IConfiguration configuration,
        IMemoryCache cache,
        AppDbContext db,
        ILogger<TextGenerationService> logger)
    {
        private readonly HttpClient _httpClient = httpClient;
        private readonly IConfiguration _configuration = configuration;
        private readonly IMemoryCache _cache = cache;
        private readonly AppDbContext _db = db;
        private readonly ILogger<TextGenerationService> _logger = logger;

        public async Task<PromptResponse> GenerateTextAsync(string prompt)
        {
            try
            {
                // Check cache
                if (_cache.TryGetValue(prompt, out var cachedObj) && cachedObj is PromptResponse cachedResponse)
                {
                    _logger.LogInformation("Returning cached response for prompt: {Prompt}", prompt);
                    return cachedResponse;
                }

                // Check DB
                var dbEntry = await _db.GeneratedText.FirstOrDefaultAsync(g => g.Prompt == prompt);
                if (dbEntry != null)
                {
                    var response = new PromptResponse { Text = dbEntry.Response };
                    _cache.Set(prompt, response, TimeSpan.FromMinutes(10));
                    _logger.LogInformation("Returning response from database for prompt: {Prompt}", prompt);
                    return response;
                }

                // Get API key
                string apiKey = _configuration["Mistral:ApiKey"]
                    ?? Environment.GetEnvironmentVariable("MISTRAL_API_KEY")
                    ?? throw new InvalidOperationException("Mistral API key is missing.");

                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

                var body = new
                {
                    model = _configuration["Mistral:Model"] ?? "mistral-large-latest",
                    messages = new object[] { new { role = "user", content = prompt } },
                    max_tokens = 500
                };

                string json = JsonSerializer.Serialize(body);
                using var content = new StringContent(json, Encoding.UTF8, "application/json");

                //get response from Mistral API
                var resp = await _httpClient.PostAsync(
                    _configuration["Mistral:BaseUrl"] ?? "https://api.mistral.ai/v1/chat/completions",
                    content
                );

                // Check for success
                if (!resp.IsSuccessStatusCode)
                {
                    string errorBody = await resp.Content.ReadAsStringAsync();
                    _logger.LogError("Mistral API returned error {StatusCode}: {ErrorBody}", resp.StatusCode, errorBody);
                    throw new HttpRequestException($"Mistral API error: {resp.StatusCode}");
                }

                //Converting JSON to string
                using var stream = await resp.Content.ReadAsStreamAsync();
                using var doc = await JsonDocument.ParseAsync(stream);            
                string reply = doc.RootElement
                                  .GetProperty("choices")[0]
                                  .GetProperty("message")
                                  .GetProperty("content")
                                  .GetString() ?? string.Empty;

                var responseObj = new PromptResponse { Text = reply };

                // Save or update DB
                try
                {
                    var existing = await _db.GeneratedText.FirstOrDefaultAsync(g => g.Prompt == prompt);
                    if (existing != null)
                    {
                        existing.Response = reply;
                        existing.CreatedAt = DateTime.UtcNow;
                        _db.GeneratedText.Update(existing);
                    }
                    else
                    {
                        var generated = new GeneratedText
                        {
                            Prompt = prompt,
                            Response = reply,
                            CreatedAt = DateTime.UtcNow
                        };
                        _db.GeneratedText.Add(generated);
                    }
                    await _db.SaveChangesAsync();
                }
                catch (Exception dbEx)
                {
                    _logger.LogError(dbEx, "Failed to save generated response for prompt: {Prompt}", prompt);
                }

                // Cache the response
                _cache.Set(prompt, responseObj, TimeSpan.FromMinutes(10));
                _logger.LogInformation("Generated new response for prompt: {Prompt}", prompt);

                return responseObj;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while generating text for prompt: {Prompt}", prompt);
                throw; // rethrow so controller can handle response
            }
        }

        // Retrieve all generated texts from the database
        public async Task<List<GeneratedText>> GetAllGeneratedTextsAsync()
        {
            try
            {
                return await _db.GeneratedText
                    .OrderByDescending(g => g.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve generated texts history.");
                throw;
            }
        }

        // Clear the cache if needed
        public void ClearCache()
        {
            try
            {
                _cache.Dispose();
                _logger.LogInformation("Cache cleared successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while clearing cache.");
            }
        }
    }
}
