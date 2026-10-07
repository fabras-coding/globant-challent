using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GlobantChallenge.Application.Models
{
    public record Story
    {
        public int Id { get; init; }
        public string? Title { get; init; }
        public string? Url { get; init; }
        public string? By { get; init; }
        public long Time { get; init; } // unix seconds, as returned by the HN API
        public int Score { get; init; }
        public int Descendants { get; init; }
        public string? Type { get; init; }
    }
}