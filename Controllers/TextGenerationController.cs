using Microsoft.AspNetCore.Mvc;
using TextGenerationWithAI.DTOs;

namespace TextGenerationWithAI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TextGenerationController(
        Services.TextGenerationService textGenerationService,
        ILogger<TextGenerationController> logger,
        IConfiguration configuration) : ControllerBase
    {
        private readonly Services.TextGenerationService _textGenerationService = textGenerationService;
        private readonly ILogger<TextGenerationController> _logger = logger;
        private readonly IConfiguration _configuration = configuration;

        /// <summary>
        /// Generates text based on the provided prompt and optional model.
        /// </summary>
        [HttpPost("generate")]
        public async Task<IActionResult> GenerateTextAsync([FromBody] PromptRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Prompt))
                return BadRequest("Prompt cannot be empty.");

            try
            {
                var response = await _textGenerationService.GenerateTextAsync(request.Prompt, request.Model);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating text for prompt: {@Request}", request);
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        /// <summary>
        /// Retrieves the history of all generated texts.
        /// </summary>
        [HttpGet("history")]
        public async Task<IActionResult> GetHistoryAsync()
        {
            try
            {
                var history = await _textGenerationService.GetAllGeneratedTextsAsync();
                return Ok(history);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving generated texts history.");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        /// <summary>
        /// Retrieves the list of available models.
        /// </summary>
        [HttpGet("models")]
        public IActionResult GetModels()
        {
            try
            {
                var models = _textGenerationService.GetAvailableModels();
                return Ok(models);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving available models.");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}
