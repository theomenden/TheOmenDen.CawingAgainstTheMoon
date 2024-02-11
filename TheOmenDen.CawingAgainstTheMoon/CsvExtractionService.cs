using Blazorise;
using CsvHelper;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.IO;
using System.Buffers;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;
using TheOmenDen.CawingAgainstTheMoon.Models;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace TheOmenDen.CawingAgainstTheMoon;

public interface ICsvExtractionService
{
    Task<IEnumerable<FileHeader>> ReadHeaderAsync(IFileEntry e, CancellationToken cancellationToken = default);
    Task ExtractFilesAsync(Stream stream, FileHeader header);
    IAsyncEnumerable<string> EnumerateAsJsonAsync(IEnumerable<IFileEntry> files, IEnumerable<FileHeader> headersToConvert, CancellationToken cancellationToken = default);
    ValueTask<string> ConvertToJsonAsync(IFileEntry e, IEnumerable<FileHeader> headersToConvert, CancellationToken cancellationToken = default);
}

internal sealed class CsvExtractionService(ILogger<CsvExtractionService> logger, RecyclableMemoryStreamManager recyclableMemoryStreamManager) : ICsvExtractionService
{
    public async Task<IEnumerable<FileHeader>> ReadHeaderAsync(IFileEntry e, CancellationToken cancellationToken = default)
    {
        await using var stream = e.OpenReadStream(long.MaxValue, cancellationToken);
        using var streamReader = new StreamReader(stream);
        using var csv = new CsvReader(streamReader, CultureInfo.InvariantCulture);

        if (!await csv.ReadAsync().ConfigureAwait(false))
        {
            return Enumerable.Empty<FileHeader>();
        }

        csv.ReadHeader();

        return csv.HeaderRecord
            .Select((t, i) => new FileHeader
            {
                Name = t,
                ColumnIndex = i
            });
    }

    public async Task ExtractFilesAsync(Stream stream, FileHeader header)
    {
        throw new NotImplementedException();
    }

    public async ValueTask<string> ConvertToJsonAsync(IFileEntry e, IEnumerable<FileHeader> headersToConvert, CancellationToken cancellationToken = default)
    {
        await using var stream = e.OpenReadStream(long.MaxValue, cancellationToken);
        using var streamReader = new StreamReader(stream);
        using var csv = new CsvReader(streamReader, CultureInfo.InvariantCulture);

        if (!await csv.ReadAsync().ConfigureAwait(false))
        {
            return "{}";
        }

        csv.ReadHeader();

        var headers = headersToConvert.ToArray();

        var sb = new StringBuilder();

        while (await csv.ReadAsync().ConfigureAwait(false))
        {
            sb.Append('{');
            foreach (var fileHeader in headers.Select(x => x.Name))
            {
                if (!csv.HeaderRecord.Contains(fileHeader, StringComparer.OrdinalIgnoreCase))
                {
                    continue;
                }

                var fieldValue = csv.GetField<string>(fileHeader);
                sb.AppendFormat("""
                                "{0}":"{1}"
                                """, fileHeader, fieldValue);
                sb.Append(',');
            }
            sb.Remove(sb.Length - 1, 1);
            sb.Append('}');
        }

        return sb.ToString();
    }

    public async IAsyncEnumerable<string> EnumerateAsJsonAsync(IEnumerable<IFileEntry> files, IEnumerable<FileHeader> headersToConvert, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var file in files)
        {
            yield return await ConvertToJsonAsync(file, headersToConvert.ToList(), cancellationToken);
        }
    }
}