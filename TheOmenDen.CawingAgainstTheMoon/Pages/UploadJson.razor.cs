using System.Collections.Immutable;
using Blazorise;
using Blazorise.DataGrid;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.IO;
using TheOmenDen.CawingAgainstTheMoon.Models;

namespace TheOmenDen.CawingAgainstTheMoon.Pages;
public partial class UploadJson : ComponentBase
{
    private FilePicker filePickerCustom;
    private const int FileEditMaxChunkSize = 24576;
    protected DataGrid<string> datagridRef;
    protected Progress progressRef;
    protected int progress;
    [Inject] private ILogger<UploadJson> Logger { get; init; }
    [Inject] private IJsonExtractionService DecompressionService { get; init; }
    [Inject] INotificationService NotificationService { get; init; }
    [Inject] IPageProgressService PageProgressService { get; init; }
    private readonly List<FileHeader> _fileHeaders = new(100);

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
            await DecompressionService.ConvertToCsvAsync(e.File);
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

    private Task ProcessSelectedColumns() =>
        PageProgressService.Go(null, options => options.Color = Blazorise.Color.Warning);
}
