using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Blazorise;
using CsvHelper;
using Microsoft.Extensions.Logging;
using Microsoft.IO;

namespace TheOmenDen.CawingAgainstTheMoon;

public interface IJsonExtractionService
{
    Task ConvertToCsvAsync(IFileEntry file, CancellationToken cancellationToken = default);
}

internal sealed class JsonExtractionService(ILogger<JsonExtractionService> logger, RecyclableMemoryStreamManager recyclableMemoryStreamManager) : IJsonExtractionService
{
    public async Task ConvertToCsvAsync(IFileEntry file, CancellationToken cancellationToken = default)
    {
        await using var stream = file.OpenReadStream(long.MaxValue, cancellationToken);
        await using var memoryStream = recyclableMemoryStreamManager.GetStream();
        await stream.CopyToAsync(memoryStream, cancellationToken);

        await using var streamWriter = new StreamWriter(memoryStream);
        var csv = new CsvWriter(streamWriter, CultureInfo.InvariantCulture);

        var writableItems =
            await JsonSerializer.DeserializeAsync<IEnumerable<JsonNode>>(memoryStream, cancellationToken: cancellationToken);
        await csv.WriteRecordsAsync(
            writableItems,
            cancellationToken);
    }
}