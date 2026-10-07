using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GlobantChallenge.Application.Models;

namespace GlobantChallenge.Application.Abstractions
{
    public interface IHackerNewsClient
    {
        Task <IReadOnlyList<int>> GetBestStoryIdsAsync(CancellationToken cancellationToken);

        Task<Story?> GetStoryAsync(int id, CancellationToken cancellationToken);
    }
}