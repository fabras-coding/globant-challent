using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GlobantChallenge.Application.Abstractions;
using GlobantChallenge.Application.DTO;
using GlobantChallenge.Application.Models;

namespace GlobantChallenge.Application.Services
{
    public class StoryService : IStoryService
    {
        public StoryService(IHackerNewsClient hackerNewsClient)
        {
            _hackerNewsClient = hackerNewsClient;
        }

        private readonly IHackerNewsClient _hackerNewsClient;


        public async Task<IReadOnlyList<StoryDto>> GetTopStoriesAsync(int n, CancellationToken cancellationToken)
        {
            var ids = await _hackerNewsClient.GetBestStoryIdsAsync(cancellationToken);

            var selectedIds = ids.Take(n);

            var stories = await GetStoriesAsync(selectedIds, cancellationToken);

            return stories.OrderByDescending(s => s.Score).ToList();

        }

        private async Task<IReadOnlyList<StoryDto>> GetStoriesAsync(IEnumerable<int> selectedIds, CancellationToken cancellationToken)
        {
            var tasks = selectedIds.Select(id => _hackerNewsClient.GetStoryAsync(id, cancellationToken));
            var stories = await Task.WhenAll(tasks);

            var result = stories
                .Where(story => story is not null)
                .Select(story => new Story
                {
                    Id = story!.Id,
                    Title = story.Title,
                    Url = story.Url,
                    Score = story.Score,
                    By = story.By,
                    Time = story.Time,
                    Descendants = story.Descendants
                })
                .ToList();
            
            var resultDto = result
                .Select(story => new StoryDto
                {
                    Title = story.Title!,
                    Uri = story.Url!,
                    PostedBy = story.By!,
                    Time = DateTimeOffset.FromUnixTimeSeconds(story.Time),
                    Score = story.Score,            
                    CommentsCount = story.Descendants
                })
                .ToList();

            return resultDto;

        }

    }
}