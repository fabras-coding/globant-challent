using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GlobantChallenge.Application.DTO
{
    public record StoryDto
    {
        public string Title { get; init; }
        public string Uri { get; init; }
        public string PostedBy { get; init; }
        public DateTimeOffset Time { get; init; } 
        public int Score { get; init; }
        public int CommentsCount { get; init; }
    }
}