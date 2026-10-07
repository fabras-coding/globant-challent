# Globant Challenge — Hacker News Best Stories API

A RESTful ASP.NET Core API that returns the details of the best *n* stories
from Hacker News, ordered by score descending.

## Project structure

- `GlobantChallenge.Api` — ASP.NET Core Web API, controllers and DI wiring.
- `GlobantChallenge.Application` — core/business layer: abstractions
  (`IHackerNewsClient`, `IStoryService`), domain model (`Story`), DTOs
  (`StoryDto`) and `StoryService` (ordering/mapping logic).
- `GlobantChallenge.Infrastructure` — `HackerNewsClient`, the concrete
  implementation of `IHackerNewsClient` that talks to the Hacker News API.
- `GlobantChallenge.Application.Tests` — xUnit + Moq unit tests for
  `StoryService`.

## How to run

```bash
dotnet run --project GlobantChallenge.Api
```

Then call:

```
GET https://localhost:<port>/api/stories?n=10
```

Returns an array of up to 10 stories, ordered by score descending:

```json
[
  {
    "title": "A uBlock Origin update was rejected from the Chrome Web Store",
    "uri": "https://github.com/uBlockOrigin/uBlock-issues/issues/745",
    "postedBy": "ismaildonmez",
    "time": "2019-10-12T13:43:01+00:00",
    "score": 1716,
    "commentCount": 572
  }
]
```

## How to run the tests

```bash
dotnet test GlobantChallenge.Application.Tests
```

## Assumptions

- `n` must be a positive integer; `n <= 0` returns `400 Bad Request`. There is
  no enforced upper bound on `n` — if `n` exceeds the number of best story IDs
  returned by Hacker News, all available stories are returned.
- The Hacker News base URL is read from configuration
  (`HackerNews:BaseUrl`), not hardcoded, to keep it environment-configurable.
- Stories that fail to be fetched (e.g. deleted/null items) are silently
  skipped rather than failing the whole request.
- Score is assumed to be the correct ranking signal (as stated in the spec);
  ties are left in whatever order `OrderByDescending` produces.
- The list of best story IDs and each individual story are both cached
  in-memory (`IMemoryCache`) to avoid refetching on every request:
  - Best story IDs: 60s absolute expiration (list changes frequently but
    doesn't need to be real-time).
  - Individual stories: 5 min absolute expiration (score/comment count
    drift slowly).
- A per-cache-key `SemaphoreSlim(1,1)` lock prevents a cache "stampede" —
  concurrent requests for the same id/key collapse into a single upstream
  call instead of each hitting the Hacker News API.
- A single shared `SemaphoreSlim` (registered as a singleton, capacity 10)
  caps the total number of concurrent outbound calls to the Hacker News API
  across all requests, regardless of how many stories are requested
  concurrently, so the app can't overload the upstream API.
- `HackerNewsClient` logs cache misses and upstream fetches (`ILogger`) to
  make cache behavior and API call volume observable.

## Future improvements (given more time)

- Replace `IMemoryCache` with a shared/distributed cache (e.g. Redis via
  `IDistributedCache`) so caching and the stampede protection work correctly
  across multiple API instances — right now each instance has its own
  in-memory cache and semaphores, so a horizontally scaled deployment would
  still issue one burst of upstream calls per instance.
- Replace the hand-rolled per-key `SemaphoreSlim` dictionary with a proper
  distributed/async cache helper (e.g. `Lazy<Task<T>>` cache pattern) or a
  library like `LazyCache`/`FusionCache`, which handle stampede protection,
  eviction and refresh-ahead out of the box.
- Add integration tests for `HackerNewsClient` using a fake `HttpMessageHandler`
  to verify caching, concurrency throttling and error handling without
  hitting the real API.
- Add resilience (retry/circuit breaker) around the outbound HTTP calls via
  `Microsoft.Extensions.Http.Resilience` / Polly, since the exercise
  explicitly asks to avoid overloading the upstream API — currently there is
  no retry/backoff on transient failures.
- Validate/cap `n` (e.g. max 100) to avoid a single caller requesting an
  unbounded number of stories.
- Add response caching / ETag support at the API level so repeated identical
  requests from clients don't even reach `StoryService`.
- Add structured logging around cache hit/miss and throttling to make
  production behavior observable.
- Consider making the cache TTLs configurable instead of hardcoded
  constants.
