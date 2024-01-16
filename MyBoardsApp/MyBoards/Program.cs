using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.EntityFrameworkCore;
using MyBoards.Entities;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder
    .Services
    .Configure<JsonOptions>(options =>
    {
        options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });

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

app.MapGet(
    "data4",
    async (MyBoardsContext db) =>
    {
        var user = await db.Users
            .Include(u => u.Comments)
            .ThenInclude(c => c.WorkItem)
            .Include(u => u.Address)
            .FirstAsync(u => u.Id == Guid.Parse("68366DBE-0809-490F-CC1D-08DA10AB0E61"));

        return user;
    }
);

app.MapPost(
    "update1",
    async (MyBoardsContext db) =>
    {
        var epic = await db.Epics.FirstAsync(e => e.Id == 2);
        var doingState = await db.WorkItemStates.FirstAsync(s => s.Value == "Doing");
        epic.State = doingState;
        await db.SaveChangesAsync();
        return epic;
    }
);

app.MapPost(
    "create1",
    async (MyBoardsContext db) =>
    {
        var address = new Address()
        {
            City = "Kraków",
            Country = "Poland",
            Street = "D³uga"
        };
        var user = new User()
        {
            Email = "user@test.com",
            FullName = "Test User",
            Address = address,
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }
);

app.MapDelete(
    "delete1",
    async (MyBoardsContext db) =>
    {
        var user = await db.Users
            .Include(u => u.Comments)
            .FirstAsync(u => u.Id == Guid.Parse("4EBB526D-2196-41E1-CBDA-08DA10AB0E61"));
        db.Users.Remove(user);
        await db.SaveChangesAsync();
    }
);

app.Run();
