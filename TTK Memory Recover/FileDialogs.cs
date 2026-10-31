using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace TtkMemoryRecover;

public static class FileDialogs
{
    public static readonly FilePickerFileType ImgFiles = new("IMG files") { Patterns = ["*.img"] };
    public static readonly FilePickerFileType TextFiles = new("Text files") { Patterns = ["*.txt"] };

    public static async Task<string?> PickOpenAsync(
        Visual anchor, string title, params FilePickerFileType[] types)
    {
        TopLevel? top = TopLevel.GetTopLevel(anchor);
        if (top is null)
        {
            return null;
        }

        var files = await top.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
            FileTypeFilter = [.. types, FilePickerFileTypes.All],
        });

        return files.Count == 0 ? null : files[0].TryGetLocalPath();
    }

    public static async Task<string?> PickSaveAsync(Visual anchor, string title, string suggestedName)
    {
        TopLevel? top = TopLevel.GetTopLevel(anchor);
        if (top is null)
        {
            return null;
        }

        var file = await top.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = title,
            SuggestedFileName = suggestedName,
            DefaultExtension = "img",
            FileTypeChoices = [ImgFiles, FilePickerFileTypes.All],
        });

        return file?.TryGetLocalPath();
    }

    public static string SuggestSaveName(string imgPath) =>
        $"{Path.GetFileNameWithoutExtension(imgPath)}_recovered{Path.GetExtension(imgPath)}";
}
