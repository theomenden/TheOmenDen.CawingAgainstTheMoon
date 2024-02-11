using System.Collections.Immutable;
using System.Text;
using Blazorise;
using Blazorise.LoadingIndicator;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.IO;
using TheOmenDen.CawingAgainstTheMoon.Models;

namespace TheOmenDen.CawingAgainstTheMoon.Pages;

public partial class UploadCsv : ComponentBase
{
    private FilePicker filePickerCustom;
    private const int FileEditMaxChunkSize = 24576;
    private bool disableProgressReport;
    private bool directory;
    [Inject] private ILogger<UploadCsv> Logger { get; init; }
    [Inject] private RecyclableMemoryStreamManager MemoryStreamManager { get; init; }
    [Inject] private ICsvExtractionService DecompressionService { get; init; }
    [Inject] INotificationService NotificationService { get; init; }
    [Inject] IPageProgressService PageProgressService { get; init; }
    private readonly List<FileHeader> _fileHeaders = new(100);
    private List<FileHeader> _fileHeadersToExtract = new(100);
    private List<FileHeader> _fileHeadersToJson = new(100);
    private void OnFileUploadEnded(FileEndedEventArgs e)
    {
        Logger.LogInformation("File {Name} upload {Success}", e.File.Name, e.Success ? "succeeded" : "failed");
        PageProgressService.Go(-1);
    }
    private void OnFilePartReceived(FileWrittenEventArgs e)
    {
        Logger.LogInformation("File part received. Position: {Position} Received data size: {Length}", e.Position, e.Data.Length);
    }

    private void OnFileUploadProgressChanged(FileProgressedEventArgs e)
    {
        Logger.LogInformation("File upload progress: {Percentage} %", e.Percentage);
        PageProgressService.Go(null, options => options.Color = Blazorise.Color.Warning);
    }
    private async Task OnFileUploadBuffered(FileUploadEventArgs e)
    {
        try
        {
            var headers = (await DecompressionService.ReadHeaderAsync(e.File)).ToImmutableList();
            _fileHeaders.AddRange(headers);
            _fileHeadersToExtract.AddRange(headers);
        }
        catch (Exception exc)
        {
            Logger.LogError(exc, "Error uploading file");
            await NotificationService.Error("Error uploading file");
        }
        finally
        {
            await InvokeAsync(StateHasChanged);
            await PageProgressService.Go(-1);
        }
    }

    private async Task ProcessSelectedColumns()
    {
        await PageProgressService.Go(null, options => options.Color = Blazorise.Color.Warning);

        if (_fileHeadersToJson.Count is 0)
        {
            await NotificationService.Error("No columns selected");
            await PageProgressService.Go(0, options => options.Color = Blazorise.Color.Warning);
            return;
        }


        var columnsAsList = new List<string>();
        foreach (var file in filePickerCustom.FileEdit.Files)
        {
            columnsAsList.Add(await DecompressionService.ConvertToJsonAsync(file, _fileHeadersToJson));
        }

        var jsonTotal = String.Join(',', columnsAsList);

        await PageProgressService.Go(0, options => options.Color = Blazorise.Color.Warning);
    }
}