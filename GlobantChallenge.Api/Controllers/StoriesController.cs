using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GlobantChallenge.Application.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace GlobantChallenge.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StoriesController : ControllerBase
    {
        public StoriesController(IStoryService storyService)
        {
            _storyService = storyService;
        }

        private readonly IStoryService _storyService;
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetStories([FromQuery] int n, CancellationToken cancellationToken)
        {
            
            if(n <= 0)
                return BadRequest("The number of stories requested must be greater than zero.");

            var stories = await _storyService.GetTopStoriesAsync(n, cancellationToken);
            // Implementation goes here
            return Ok(stories);
        }

    }
}