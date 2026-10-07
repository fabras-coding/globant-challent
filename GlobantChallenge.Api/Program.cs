using GlobantChallenge.Application.Abstractions;
using GlobantChallenge.Application.Services;
using GlobantChallenge.Infrastructure.ExternalServices;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddMemoryCache();
builder.Services.AddSingleton(new SemaphoreSlim(10,10));
builder.Services.AddHttpClient<IHackerNewsClient, HackerNewsClient>(client =>
{
    // trailing slash is required so relative request URIs resolve correctly
    var baseUrl = builder.Configuration["HackerNews:BaseUrl"]!.TrimEnd('/') + "/";
    client.BaseAddress = new Uri(baseUrl);
});
builder.Services.AddScoped<IStoryService, StoryService>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
