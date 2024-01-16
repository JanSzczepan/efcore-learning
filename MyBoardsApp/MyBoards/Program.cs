using Microsoft.EntityFrameworkCore;
using MyBoards.Entities;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder
    .Services
    .AddDbContext<MyBoardsContext>(
        option =>
            option.UseSqlServer(
                builder.Configuration.GetConnectionString("MyBoardsConnectionString")
            )
    );

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

using var scope = app.Services.CreateScope();
var dbContext = scope.ServiceProvider.GetService<MyBoardsContext>();

var pendingMigrations = dbContext.Database.GetPendingMigrations();
if (pendingMigrations.Any())
{
    dbContext.Database.Migrate();
}

var users = dbContext.Users.ToList();
if (!users.Any())
{
    var user1 = new User()
    {
        Email = "user1@test.com",
        FullName = "User One",
        Address = new Address() { City = "Warszwa", Street = "Szeroka" }
    };

    var user2 = new User()
    {
        Email = "user2@test.com",
        FullName = "User Two",
        Address = new Address() { City = "Krakow", Street = "D³uga" }
    };

    dbContext.Users.AddRange(user1, user2);

    dbContext.SaveChanges();
}

app.MapGet(
    "data1",
    async (MyBoardsContext db) =>
    {
        var statesCount = await db.WorkItems
            .GroupBy(x => x.StateId)
            .Select(g => new { stateId = g.Key, count = g.Count() })
            .ToListAsync();

        return statesCount;
    }
);

app.MapGet(
    "data2",
    async (MyBoardsContext db) =>
    {
        var selectedEpics = await db.Epics
            .Where(e => e.StateId == 1)
            .OrderBy(e => e.Priority)
            .ToListAsync();
        return selectedEpics;
    }
);

app.MapGet(
    "data3",
    async (MyBoardsContext db) =>
    {
        var authorsCommentCounts = await db.Comments
            .GroupBy(c => c.AuthorId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToListAsync();
        var topAuthor = authorsCommentCounts.First(
            a => a.Count == authorsCommentCounts.Max(acc => acc.Count)
        );
        var userDetails = db.Users.First(u => u.Id == topAuthor.Key);
        return new { userDetails, commentCount = topAuthor.Count };
    }
);

app.Run();
