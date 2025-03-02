using System.Text.Json;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

var app = builder.Build();
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("Squares API")
               .WithTheme(ScalarTheme.Mars) // Choose a preferred theme
               .WithDarkModeToggle(true);   // Enable dark mode toggle
    });
}

// Load saved squares from file on startup
List<Square> savedSquares = await SquareHelper.LoadSquaresFromFile();

app.MapPost("/CreateSquare", async () =>
{
    var (x, y) = SquareHelper.GetNextPosition(savedSquares.LastOrDefault());
    var square = new Square(Guid.NewGuid(),
                SquareHelper.GenerateRandomTailwindColor(),
                x,
                y);

    savedSquares.Add(square);

    // Save new square to file
    await SquareHelper.SaveSquaresToFile(savedSquares);

    return Results.Created($"/squares/{square.Id}",square);
})
.Produces<Square>(201)
.Produces(500)
.WithName("CreateSquare")
.WithTags("Squares")
.WithSummary("Creates a new square and returns it")
.WithDescription("""
    Returns a new square with a unique ID, a random Tailwind color, 
    and calculated coordinates based on previous squares.
    The square is persisted in the backend.
""");

app.MapGet("/GetSavedSquares", () =>
{
    return Results.Ok(savedSquares);
})
.Produces<List<Square>>(200)
.Produces(500)
.WithName("GetSavedSquares")
.WithTags("Squares")
.WithSummary("Fetch all saved squares")
.WithDescription("""
    Returns a list of all squares that have been generated and saved in the backend.
    Each square includes an ID, a Tailwind color class, and X/Y coordinates.
""");

app.MapDelete("/DeleteSquares", async () =>
{
    try
    {
        savedSquares.Clear();
        await File.WriteAllTextAsync("squares.json", "[]");
        return Results.NoContent();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error clearing squares: {ex.Message}");
        return Results.Problem("Failed to clear squares.");
    }
})
.Produces(204)
.Produces(500)
.WithName("DeleteSquares")
.WithTags("Squares")
.WithSummary("Delete all saved squares")
.WithDescription("""
    Deletes all squares from the backend storage.
    This action resets the grid and removes all stored square data.
""");

app.MapDefaultEndpoints();
app.Run();

static class SquareHelper
{
    //since im using tailwind, why not... Just a shame that safelisting doesnt work the same way in v4.0...
    private static readonly string[] Colors =
    {
        "red", "orange", "amber", "yellow", "lime", "green", "emerald", "teal",
        "cyan", "sky", "blue", "indigo", "violet", "purple", "fuchsia", "pink",
        "rose", "slate", "gray", "zinc", "neutral", "stone"
    };

    private static readonly string[] Shades = { "200", "300", "400", "500", "600", "700", "800" };

    private static readonly Random Random = new();

    // (0,0)
    // (1,0)
    // (1,1)
    // (0,1)
    // (2,0)
    // (2,1)
    // (2,2)
    // (1,2)
    // (0,2)
    // (3,0)
    public static (int x, int y) GetNextPosition(Square? previousSquare)
    {
        if (previousSquare is null) return (0, 0);
        if (previousSquare.X == 0) return (previousSquare.Y + 1, 0);
        if (previousSquare.Y == 0) return (previousSquare.X, previousSquare.Y + 1);
        if (previousSquare.X <= previousSquare.Y) return (previousSquare.X - 1, previousSquare.Y);

        return (previousSquare.X, previousSquare.Y + 1);
    }
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true //Makes JSON more readable
    };
    private const string FilePath = "squares.json";

    //Locks the file for writing so that we don't have multiple threads writing to it at the same time, which wont happen but this is just for fun
    private static readonly SemaphoreSlim FileLock = new(1, 1);

    public static string GenerateRandomTailwindColor()
    {
        string color = Colors[Random.Next(Colors.Length)];
        string shade = Shades[Random.Next(Shades.Length)];

        return $"bg-{color}-{shade}"; // "bg-blue-600", "bg-teal-400"
    }

    //might be a bit overkill with filestream but hey, maybe we want an A lot of Squares!
    public static async Task<List<Square>> LoadSquaresFromFile()
    {
        if (!File.Exists(FilePath))
        {
            return [];
        }

        try
        {
            using FileStream fs = new(FilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var squares = await JsonSerializer.DeserializeAsync<List<Square>>(fs, JsonOptions);
            return squares ?? [];
        }
        catch(Exception ex)
        {
            Console.WriteLine($"Error loading squares: {ex.Message}");
            return [];
        }
    }

    public static async Task SaveSquaresToFile(List<Square> squares)
    {
        //closed for business, no one else allowed in
        await FileLock.WaitAsync(); 
        try
        {
            using FileStream fs = new(FilePath, FileMode.Create, FileAccess.Write, FileShare.None);
            await JsonSerializer.SerializeAsync(fs, squares, JsonOptions);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving squares: {ex.Message}");
        }
        finally
        {
            //open for business
            FileLock.Release();
        }
    }
}
record Square(Guid Id, string Color, int X, int Y);
