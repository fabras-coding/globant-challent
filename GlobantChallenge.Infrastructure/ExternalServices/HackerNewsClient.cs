using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using GlobantChallenge.Application.Abstractions;
using GlobantChallenge.Application.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace GlobantChallenge.Infrastructure.ExternalServices
{
    public class HackerNewsClient : IHackerNewsClient
    {

        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _memoryCache;
        private readonly SemaphoreSlim _semaphore;
        private readonly ILogger<HackerNewsClient> _logger;
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> _keyLocks = new();
        private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

        public HackerNewsClient(HttpClient httpClient, IMemoryCache memoryCache, SemaphoreSlim semaphore, ILogger<HackerNewsClient> logger)
        {
            _httpClient = httpClient;
            _memoryCache = memoryCache;
            _semaphore = semaphore;
            _logger = logger;
        }


        private static SemaphoreSlim KeyLock(string key)
        {
            return _keyLocks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        }
        public async Task<IReadOnlyList<int>> GetBestStoryIdsAsync(CancellationToken ct)
        {
            const string key = "best-story-ids";
            if (_memoryCache.TryGetValue(key, out IReadOnlyList<int>? cached))
                return cached!;

            _logger.LogInformation("Cache miss for key: {Key}", key);

            var keyLock = KeyLock(key);
            await keyLock.WaitAsync(ct);
            try
            {
                if (_memoryCache.TryGetValue(key, out cached))
                    return cached!; // double-checked after acquiring lock

                _logger.LogInformation("Fetching best story IDs from API");

                var ids = await FetchBestStoryIdsFromApiAsync(ct);
                _memoryCache.Set(key, ids, TimeSpan.FromSeconds(60));
                return ids;
            }
            finally
            {
                keyLock.Release();
            }
        }



        public async Task<Story?> GetStoryAsync(int id, CancellationToken ct)
        {
            var key = $"story:{id}";
            if (_memoryCache.TryGetValue(key, out Story? cached))
                return cached;

            _logger.LogInformation("Cache miss for key: {Key}", key);
            _logger.LogInformation("Fetching story from API for key: {Key}", key);
            var keyLock = KeyLock(key);
            await keyLock.WaitAsync(ct);
            try
            {
                if (_memoryCache.TryGetValue(key, out cached))
                    return cached;

                await _semaphore.WaitAsync(ct); // throttle concurrent HN calls
                Story? story;
                try
                {
                    story = await FetchStoryFromApiAsync(id, ct);
                }
                finally
                {
                    _semaphore.Release();
                }

                _logger.LogInformation("Fetched story from API for key: {Key}", key);

                if (story is not null)
                    _memoryCache.Set(key, story, TimeSpan.FromMinutes(5));
                return story;
            }
            finally
            {
                keyLock.Release();
            }
        }


        private async Task<IReadOnlyList<int>> FetchBestStoryIdsFromApiAsync(CancellationToken ct)
        {
            // relative to HttpClient.BaseAddress configured in Program.cs
            var response = await _httpClient.GetAsync("beststories.json", ct);
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync(ct);
            var ids = JsonSerializer.Deserialize<IReadOnlyList<int>>(content, _jsonOptions);
            return ids ?? Array.Empty<int>();
        }

        private async Task<Story?> FetchStoryFromApiAsync(int id, CancellationToken ct)
        {
            var response = await _httpClient.GetAsync($"item/{id}.json", ct);
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync(ct);
            var story = JsonSerializer.Deserialize<Story>(content, _jsonOptions);
            return story;
        }
    }
}