using System.Text.Json;
using Microsoft.OpenApi.Models;
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
List<Square> savedSquares = SquareHelper.LoadSquaresFromFile();

app.MapGet("/GetSquare", async () =>
{
    var (x, y) = SquareHelper.GetNextPosition(savedSquares.LastOrDefault());
    var square = new Square(Guid.NewGuid(),
                SquareHelper.GenerateRandomTailwindColor(),
                x,
                y);

    savedSquares.Add(square);

    // Save new square to file
    await SquareHelper.SaveSquaresToFile(savedSquares);

    return Results.Ok(square);
})
.Produces<Square>(200)
.Produces(500)
.WithName("GetSquare")
.WithTags("Squares")
.WithSummary("Fetch a new square")
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

app.MapPost("/ClearSquares", async () =>
{
    try
    {
        savedSquares.Clear();
        await File.WriteAllTextAsync("squares.json", "[]");
        return Results.Ok();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error clearing squares: {ex.Message}");
        return Results.Problem("Failed to clear squares.");
    }
})
.Produces(200)
.Produces(500)
.WithName("ClearSquares")
.WithTags("Squares")
.WithSummary("Clear all saved squares")
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

    public static string GenerateRandomTailwindColor()
    {
        string color = Colors[Random.Next(Colors.Length)];
        string shade = Shades[Random.Next(Shades.Length)];

        return $"bg-{color}-{shade}"; // "bg-blue-600", "bg-teal-400"
    }

    public static List<Square> LoadSquaresFromFile()
    {
        if (!File.Exists("squares.json"))
        {
            return [];
        }

        try
        {
            var squares = JsonSerializer.Deserialize<List<Square>>(File.ReadAllText("squares.json"));
            return squares ?? [];
        }
        catch
        {
            return [];
        }
    }
    public static async Task SaveSquaresToFile(List<Square> squares)
    {
        try
        {
            string json = System.Text.Json.JsonSerializer.Serialize(squares, JsonOptions);
            await File.WriteAllTextAsync("squares.json", json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving squares: {ex.Message}");
        }
    }
}
record Square(Guid Id, string Color, int X, int Y);
