using SpawnDev.BlazorJS.JSObjects;
using SpawnDev.BlazorJS.Toolbox;

namespace SpawnDev.BlazorJS.FromTypeScript.Services
{
    public class ProjectManager : IAsyncBackgroundService
    {
        public Task Ready => _Ready ??= InitAsync();
        Task? _Ready;
        HttpClient HttpClient;
        BlazorJSRuntime JS;
        FileService FSService;
        FileSystemDirectoryHandle FS => FSService.FS;
        public ProjectManager(FileService fsService, HttpClient httpClient, BlazorJSRuntime js)
        {
            HttpClient = httpClient;
            JS = js;
            FSService = fsService;
        }
        async Task InitAsync()
        {
            await FSService.Ready;
        }
        public async Task<List<string>> GetProjects()
        {
            var ret = new List<string>();
            var files = await FS.GetPathFiles();
            foreach (var file in files)
            {
                JS.Log($"file:",  file);
                var ext = Path.GetExtension(file);
                JS.Log($"ext:", ext);
            }
            return ret;
        }
    }
}
