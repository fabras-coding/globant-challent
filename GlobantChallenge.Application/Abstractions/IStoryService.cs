using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GlobantChallenge.Application.DTO;

namespace GlobantChallenge.Application.Abstractions
{
    public interface IStoryService
    {
        
        Task<IReadOnlyList<StoryDto>> GetTopStoriesAsync(int n, CancellationToken cancellationToken);

    }
}