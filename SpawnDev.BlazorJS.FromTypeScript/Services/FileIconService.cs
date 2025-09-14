using SpawnDev.MatrixLEDDisplay.Demo.Services;

namespace SpawnDev.BlazorJS.FromTypeScript.Services
{
    public class FileIconService : IAsyncBackgroundService
    {
        public string typeScriptPath { get; } = "TypeScript";
        public string blazorProjectPath { get; } = "Blazor";
        Task? _Ready;
        public Task Ready => _Ready ??= InitAsync();
        AssetManifestService AssetManifestService;
        public FileIconService(AssetManifestService assetManifestService)
        {
            AssetManifestService = assetManifestService;
        }
        async Task InitAsync()
        {
            await AssetManifestService.Ready;

        }
        public string? GetExtensionLanguage(string ext)
        {
            var r = LanguageToExtMaps.FirstOrDefault(o => o.Value.Contains(ext, StringComparer.OrdinalIgnoreCase)).Key;
            return r;
        }
        public string? GetPathImage(ASyncFSEntryInfo f)
        {
            string? image = null;
            if (f == null) return image;
            if (!f.IsDirectory)
            {
                image = GetFileImage(f.FullPath);
            }
            else
            {
                image = GetDirectoryImage(f.FullPath);
            }
            return image;
        }
        public string? GetFileImage(string path)
        {
            string? image = "ext/_blank.png";
            if (path == null) return image;
            var assets = AssetManifestService.Assets.Where(o => o.StartsWith($"ext/")).ToList();
            var ext = IOPath.GetExtension(path);
            var lang = GetExtensionLanguage(ext);
            var extImage = !string.IsNullOrEmpty(lang) ? assets.FirstOrDefault(o => o.StartsWith($"ext/{lang}_")) : null;
            if (extImage == null && ext.StartsWith(".") && ext.Length > 1)
            {
                var extWithoutDot = ext.Substring(1);
                extImage = assets.FirstOrDefault(o => o.StartsWith($"ext/{extWithoutDot}.") || o.StartsWith($"ext/{extWithoutDot}_"));
            }
            return !string.IsNullOrEmpty(extImage) ? extImage : image;
        }
        public string? GetDirectoryImage(string path)
        {
            string? image = "ext/directory.png";
            if (path == null) return image;
            if (path.Split('/').Length == 2)
            {
                var baseFolder = path.Split('/').First();
                if (baseFolder == blazorProjectPath)
                {
                    image = "blazor.png";
                }
                else if (baseFolder == typeScriptPath)
                {
                    image = "ts-icon.png";
                }
            }   
            return image;
        }
        public string? GetFileLanguage(string path)
        {
            var ext = IOPath.GetExtension(path);
            return GetExtensionLanguage(ext);
        }
        Dictionary<string, string[]> LanguageToExtMaps = new Dictionary<string, string[]>
    {
        { "csharp", [".cs"] },
        { "typescript", [".ts"] },
    };
    }
}
