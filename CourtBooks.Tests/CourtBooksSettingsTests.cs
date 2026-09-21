using System.Text.Json;
using System.Text.Json.Serialization;
using CourtBooks.Core.Configuration;
using Xunit;

namespace CourtBooks.Tests;

public sealed class CourtBooksSettingsTests
{
    [Fact]
    public void Load_ReturnsDefaultsWhenFileDoesNotExist()
    {
        var path = Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}.json");

        var settings = CourtBooksSettings.Load(path);

        Assert.Equal("Data Source=courtbooks.db;Foreign Keys=True", settings.DefaultConnection);
    }

    [Fact]
    public void Load_ReadsConfiguredConnectionString()
    {
        var path = CreateFile("""
        {
          "ConnectionStrings": {
            "DefaultConnection": "Data Source=test.db;Foreign Keys=True"
          }
        }
        """);

        try
        {
            var settings = CourtBooksSettings.Load(path);
            Assert.Equal("Data Source=test.db;Foreign Keys=True", settings.DefaultConnection);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Load_UsesDefaultWhenConnectionStringIsBlank()
    {
        var path = CreateFile("""
        {
          "ConnectionStrings": {
            "DefaultConnection": "   "
          }
        }
        """);

        try
        {
            var settings = CourtBooksSettings.Load(path);
            Assert.Equal("Data Source=courtbooks.db;Foreign Keys=True", settings.DefaultConnection);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Load_RejectsMalformedJson()
    {
        var path = CreateFile("{ invalid json");

        try
        {
            Assert.Throws<JsonReaderException>(() => CourtBooksSettings.Load(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static string CreateFile(string content)
    {
        var path = Path.Combine(Path.GetTempPath(), $"courtbooks-settings-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, content);
        return path;
    }
}
