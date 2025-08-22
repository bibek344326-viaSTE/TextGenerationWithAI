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

        /// <summary>
        /// Generates text using the Mistral API, with caching and DB storage.
        /// </summary>
        public async Task<PromptResponse> GenerateTextAsync(string prompt, string model)
        {
            try
            {
                // Determine the model to use
                string selectedModel = GetSelectedModel(model);
                _logger.LogInformation("Using model '{Model}' for prompt: {Prompt}", selectedModel, prompt);

                // Check cache first
                string cacheKey = $"{selectedModel}:{prompt}";
                if (_cache.TryGetValue(cacheKey, out var cachedObj) && cachedObj is PromptResponse cachedResponse)
                    return cachedResponse;

                // Check database
                var dbResponse = await GetResponseFromDatabaseAsync(prompt, selectedModel);
                if (dbResponse != null)
                {
                    _cache.Set(cacheKey, dbResponse, TimeSpan.FromMinutes(10));
                    return dbResponse;
                }

                // Call Mistral API
                var apiResponse = await CallMistralApiAsync(prompt, selectedModel);

                // Save response to DB and cache
                await SaveResponseAsync(prompt, selectedModel, apiResponse.Text);
                _cache.Set(cacheKey, apiResponse, TimeSpan.FromMinutes(10));

                return apiResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating text for model {Model}, prompt: {Prompt}", model, prompt);
                throw;
            }
        }

        /// <summary>
        /// Retrieves all generated texts from the database.
        /// </summary>
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

        /// <summary>
        /// Returns the list of available models from configuration.
        /// </summary>
        public List<string> GetAvailableModels()
            => _configuration.GetSection("Mistral:AvailableModels").Get<List<string>>() ?? new List<string>();

        /// <summary>
        /// Clears the memory cache.
        /// </summary>
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

        #region Private Helpers

        private string GetSelectedModel(string model)
        {
            var availableModels = GetAvailableModels();
            return !string.IsNullOrWhiteSpace(model) && availableModels.Contains(model)
                ? model
                : _configuration["Mistral:DefaultModel"] ?? "mistral-large";
        }

        private async Task<PromptResponse?> GetResponseFromDatabaseAsync(string prompt, string model)
        {
            var dbEntry = await _db.GeneratedText.FirstOrDefaultAsync(g => g.Prompt == prompt && g.Model == model);
            return dbEntry == null ? null : new PromptResponse { Text = dbEntry.Response };
        }

        private async Task<PromptResponse> CallMistralApiAsync(string prompt, string model)
        {
            string apiKey = _configuration["Mistral:ApiKey"]
                ?? throw new InvalidOperationException("Mistral API key is missing.");

            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

            var body = new
            {
                model = model, // Important: API expects 'model' field
                messages = new[] { new { role = "user", content = prompt } },
                max_tokens = 600
            };

            using var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(
                _configuration["Mistral:BaseUrl"] ?? "https://api.mistral.ai/v1/chat/completions",
                content
            );

            if (!response.IsSuccessStatusCode)
            {
                string errorBody = await response.Content.ReadAsStringAsync();
                _logger.LogError("API error {StatusCode}: {ErrorBody}", response.StatusCode, errorBody);
                throw new HttpRequestException($"API error: {response.StatusCode}");
            }

            using var stream = await response.Content.ReadAsStreamAsync();
            using var doc = await JsonDocument.ParseAsync(stream);
            string reply = doc.RootElement
                              .GetProperty("choices")[0]
                              .GetProperty("message")
                              .GetProperty("content")
                              .GetString() ?? string.Empty;

            return new PromptResponse { Text = reply };
        }

        private async Task SaveResponseAsync(string prompt, string model, string responseText)
        {
            try
            {
                var existing = await _db.GeneratedText.FirstOrDefaultAsync(g => g.Prompt == prompt && g.Model == model);

                if (existing != null)
                {
                    existing.Response = responseText;
                    existing.CreatedAt = DateTime.UtcNow;
                    _db.GeneratedText.Update(existing);
                }
                else
                {
                    var generated = new GeneratedText
                    {
                        Prompt = prompt,
                        Response = responseText,
                        Model = model,
                        CreatedAt = DateTime.UtcNow
                    };
                    _db.GeneratedText.Add(generated);
                }

                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save generated response for model {Model}", model);
            }
        }

        #endregion
    }
}
