using Microsoft.AspNetCore.Mvc;
using TextGenerationWithAI.DTOs;

namespace TextGenerationWithAI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TextGenerationController : ControllerBase
    {
        private readonly Services.TextGenerationService _textGenerationService;
        private readonly ILogger<TextGenerationController> _logger;
        public TextGenerationController(Services.TextGenerationService textGenerationService, ILogger<TextGenerationController> _logger)
        {
            _textGenerationService = textGenerationService;
            this._logger = _logger;
        }


        //Post endpoint to generate text based on a prompt
        [HttpPost("generate")]
        public async Task<IActionResult> GenerateTextAsync([FromBody] PromptRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Prompt))
            {

                // Log the error
                _logger.LogError("Empty prompt received from the client.");
                return BadRequest("Prompt cannot be empty.");
            }
            try
            {
                var response = await _textGenerationService.GenerateTextAsync(request.Prompt);
                return Ok(response);
            }
            catch (Exception ex)
            {
                // Log the exception
                _logger.LogError(ex, "An error occurred while generating text for the prompt: {@Request}", request);
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        //Get endpoint to retrieve the history of generated texts
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
                // Log the exception
                _logger.LogError(ex, "An error occurred while retrieving the history of generated texts.");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

    }
}
