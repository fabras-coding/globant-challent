using GlobantChallenge.Application.Abstractions;
using GlobantChallenge.Application.Models;
using GlobantChallenge.Application.Services;
using Moq;

namespace GlobantChallenge.Application.Tests
{
    public class StoryServiceTests
    {
        private static Story MakeStory(int id, int score) => new()
        {
            Id = id,
            Title = $"Story {id}",
            Url = $"https://example.com/{id}",
            By = "author",
            Score = score,
            Descendants = 0,
            Time = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };

        [Fact]
        public async Task GetTopStoriesAsync_ReturnsStories_OrderedByScoreDescending()
        {
            var client = new Mock<IHackerNewsClient>();
            client.Setup(c => c.GetBestStoryIdsAsync(It.IsAny<CancellationToken>()))
                  .ReturnsAsync(new List<int> { 1, 2, 3 });
            client.Setup(c => c.GetStoryAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(MakeStory(1, 10));
            client.Setup(c => c.GetStoryAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(MakeStory(2, 50));
            client.Setup(c => c.GetStoryAsync(3, It.IsAny<CancellationToken>())).ReturnsAsync(MakeStory(3, 30));

            var sut = new StoryService(client.Object);

            var result = await sut.GetTopStoriesAsync(3, CancellationToken.None);

            Assert.Equal(new[] { 50, 30, 10 }, result.Select(s => s.Score));
        }

        [Fact]
        public async Task GetTopStoriesAsync_RequestsOnlyFirstNIds()
        {
            var client = new Mock<IHackerNewsClient>();
            client.Setup(c => c.GetBestStoryIdsAsync(It.IsAny<CancellationToken>()))
                  .ReturnsAsync(new List<int> { 1, 2, 3, 4, 5 });
            client.Setup(c => c.GetStoryAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync((int id, CancellationToken _) => MakeStory(id, id));

            var sut = new StoryService(client.Object);

            var result = await sut.GetTopStoriesAsync(2, CancellationToken.None);

            Assert.Equal(2, result.Count);
            client.Verify(c => c.GetStoryAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Fact]
        public async Task GetTopStoriesAsync_SkipsIdsThatReturnNull()
        {
            var client = new Mock<IHackerNewsClient>();
            client.Setup(c => c.GetBestStoryIdsAsync(It.IsAny<CancellationToken>()))
                  .ReturnsAsync(new List<int> { 1, 2 });
            client.Setup(c => c.GetStoryAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(MakeStory(1, 10));
            client.Setup(c => c.GetStoryAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync((Story?)null);

            var sut = new StoryService(client.Object);

            var result = await sut.GetTopStoriesAsync(2, CancellationToken.None);

            Assert.Single(result);
            Assert.Equal(10, result[0].Score);
        }

        [Fact]
        public async Task GetTopStoriesAsync_MapsFieldsToDto()
        {
            var client = new Mock<IHackerNewsClient>();
            var time = DateTimeOffset.Parse("2019-10-12T13:43:01+00:00");
            client.Setup(c => c.GetBestStoryIdsAsync(It.IsAny<CancellationToken>()))
                  .ReturnsAsync(new List<int> { 1 });
            client.Setup(c => c.GetStoryAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(new Story
            {
                Id = 1,
                Title = "Title",
                Url = "https://example.com",
                By = "author",
                Score = 100,
                Descendants = 42,
                Time = time.ToUnixTimeSeconds()
            });

            var sut = new StoryService(client.Object);

            var result = await sut.GetTopStoriesAsync(1, CancellationToken.None);

            var dto = Assert.Single(result);
            Assert.Equal("Title", dto.Title);
            Assert.Equal("https://example.com", dto.Uri);
            Assert.Equal("author", dto.PostedBy);
            Assert.Equal(time, dto.Time);
            Assert.Equal(100, dto.Score);
            Assert.Equal(42, dto.CommentsCount);
        }
    }
}
