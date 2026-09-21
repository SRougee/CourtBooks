using System.Text.Json;

namespace CourtBooks.Core.Configuration;

public sealed class CourtBooksSettings
{
    public string DefaultConnection { get; init; } = "Data Source=courtbooks.db;Foreign Keys=True";

    public static CourtBooksSettings Load(string path)
    {
        if (!File.Exists(path)) return new CourtBooksSettings();

        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var connection = document.RootElement
            .GetProperty("ConnectionStrings")
            .GetProperty("DefaultConnection")
            .GetString();

        return new CourtBooksSettings { DefaultConnection = string.IsNullOrWhiteSpace(connection)
            ? "Data Source=courtbooks.db;Foreign Keys=True" : connection };
    }
}