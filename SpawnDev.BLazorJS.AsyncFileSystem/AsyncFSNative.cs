using System.Text.Json;
using File = System.IO.File;

namespace SpawnDev.BlazorJS.AsyncFileSystem
{
    public class AsyncFSNative : IAsyncFS
    {
        public event EventHandler<FileSystemChangeEventArgs> FileSystemChanged;

        public string BasePath { get; private set; }

        public AsyncFSNative(string basePath)
        {
            if (!Directory.Exists(basePath))
            {
                throw new DirectoryNotFoundException(nameof(basePath));
            }
            BasePath = basePath;
        }

        string GetFullPath(string path, bool ensurePathExists = false)
        {
            string ret;
            path = path.Trim('\\').Trim('/');
            if (string.IsNullOrEmpty(path))
            {
                ret = BasePath;
            }
            else
            {
                ret = Path.Combine(BasePath, path);
                ret = Path.GetFullPath(ret)!;
            }
            if (ensurePathExists)
            {
                var dir = Path.GetDirectoryName(ret);
                if (!string.IsNullOrEmpty(dir))
                {
                    if (!Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }
                }
            }
            return ret;
        }

        public async Task Append(string path, Stream data)
        {
            var fPath = GetFullPath(path, true);
            using var fileStream = new FileStream(fPath, FileMode.Append, FileAccess.Write);
            // using FileMode.Append auto sets the stream position to the end
            await data.CopyToAsync(fileStream);
        }

        public Task Append(string path, string data)
        {
            var fPath = GetFullPath(path, true);
            File.AppendAllText(fPath, data);
            return Task.CompletedTask;
        }

        public async Task Append(string path, byte[] data)
        {
            using var source = new MemoryStream(data);
            await Append(path, source);
        }

        public Task CreateDirectory(string path)
        {
            var fPath = GetFullPath(path, true);
            Directory.CreateDirectory(fPath);
            return Task.CompletedTask;
        }

        public Task<bool> DirectoryExists(string path)
        {
            var fPath = GetFullPath(path);
            var ret = Directory.Exists(fPath);
            return Task.FromResult(ret);
        }

        public async IAsyncEnumerable<ASyncFSEntryInfo> EnumerateInfos(string path, bool recursive = false)
        {
            var targets = new Queue<string>();
            targets.Enqueue(path);
            while (targets.Any())
            {
                var t = targets.Dequeue();
                var fPath = GetFullPath(t);
                var dInfo = new DirectoryInfo(fPath);
                foreach (var o in dInfo.EnumerateDirectories())
                {
                    var e = new ASyncFSEntryInfo(true, o.Name, t, o.LastAccessTimeUtc);
                    yield return e;
                    if (recursive) targets.Enqueue(e.FullPath);
                }
                foreach (var o in dInfo.EnumerateFiles())
                {
                    yield return new ASyncFSEntryInfo(false, o.Name, t, o.LastAccessTimeUtc, o.Length);
                }
            }
        }

        public Task<ASyncFSEntryInfo?> GetInfo(string path)
        {
            var fPath = GetFullPath(path);
            ASyncFSEntryInfo? ret = null;
            if (Directory.Exists(fPath))
            {
                var o = new DirectoryInfo(fPath);
                ret = new ASyncFSEntryInfo(true, o.Name, path, o.LastAccessTimeUtc);
            }
            else if (File.Exists(fPath))
            {
                var o = new FileInfo(fPath);
                ret = new ASyncFSEntryInfo(false, o.Name, path, o.LastAccessTimeUtc, o.Length);
            }
            return Task.FromResult(ret);
        }

        public Task<List<ASyncFSEntryInfo>> GetInfos(string path, bool recursive = false)
        {
            var ret = new List<ASyncFSEntryInfo>();
            var targets = new Queue<string>();
            targets.Enqueue(path);
            while (targets.Any())
            {
                var t = targets.Dequeue();
                var fPath = GetFullPath(t);
                var dInfo = new DirectoryInfo(fPath);
                foreach (var o in dInfo.EnumerateDirectories())
                {
                    var e = new ASyncFSEntryInfo(true, o.Name, t, o.LastAccessTimeUtc);
                    ret.Add(e);
                    if (recursive) targets.Enqueue(e.FullPath);
                }
                foreach (var o in dInfo.EnumerateFiles())
                {
                    var e = new ASyncFSEntryInfo(false, o.Name, t, o.LastAccessTimeUtc, o.Length);
                    ret.Add(e);
                }
            }
            return Task.FromResult(ret);
        }

        public Task<List<string>> GetDirectories(string path)
        {
            var fPath = GetFullPath(path);
            var ret = Directory.GetDirectories(fPath).Select(o => Path.GetFileName(o)).ToList();
            return Task.FromResult(ret);
        }

        public Task<List<string>> GetEntries(string path)
        {
            var fPath = GetFullPath(path);
            var dirs = Directory.GetDirectories(fPath).Select(o => Path.GetFileName(o) + "/").ToList();
            var files = Directory.GetFiles(fPath).Select(o => Path.GetFileName(o)).ToList();
            return Task.FromResult(dirs.Concat(files).ToList());
        }

        public Task<List<string>> GetFiles(string path)
        {
            var fPath = GetFullPath(path);
            var ret = Directory.GetFiles(fPath).Select(o => Path.GetFileName(o)).ToList();
            return Task.FromResult(ret);
        }

        public Task<bool> Exists(string path)
        {
            var fPath = GetFullPath(path);
            var ret = Directory.Exists(fPath) || File.Exists(fPath);
            return Task.FromResult(ret);
        }

        public Task<bool> FileExists(string path)
        {
            var fPath = GetFullPath(path);
            var ret = File.Exists(fPath);
            return Task.FromResult(ret);
        }

        public async Task<byte[]> ReadBytes(string path)
        {
            var fPath = GetFullPath(path);
            var ret = await File.ReadAllBytesAsync(fPath);
            return ret;
        }

        public async Task<T> ReadJSON<T>(string path, JsonSerializerOptions? jsonSerializerOptions = null)
        {
            var txt = await ReadText(path);
            var ret = JsonSerializer.Deserialize<T>(txt, jsonSerializerOptions)!;
            return ret;
        }

        public Task<Stream> ReadStream(string path)
        {
            var fPath = GetFullPath(path);
            var ret = new FileStream(fPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            return Task.FromResult((Stream)ret);
        }

        public async Task<string> ReadText(string path)
        {
            var fPath = GetFullPath(path);
            var ret = await File.ReadAllTextAsync(fPath);
            return ret;
        }

        public Task Remove(string path, bool recursive = false)
        {
            var fPath = GetFullPath(path);
            if (File.Exists(fPath))
            {
                File.Delete(fPath);
            }
            else if (Directory.Exists(fPath))
            {
                Directory.Delete(fPath, true);
            }
            return Task.CompletedTask;
        }

        public async Task Write(string path, Stream data)
        {
            var fPath = GetFullPath(path, true);
            using var fileStream = new FileStream(fPath, FileMode.Create, FileAccess.Write);
            await data.CopyToAsync(fileStream);
        }

        public async Task Write(string path, string data)
        {
            var fPath = GetFullPath(path, true);
            await File.WriteAllTextAsync(fPath, data);
        }

        public async Task Write(string path, byte[] data)
        {
            var fPath = GetFullPath(path, true);
            await File.WriteAllBytesAsync(fPath, data);
        }

        public async Task WriteJSON(string path, object data, JsonSerializerOptions? jsonSerializerOptions = null)
        {
            var fPath = GetFullPath(path, true);
            var json = JsonSerializer.Serialize(data, jsonSerializerOptions);
            await File.WriteAllTextAsync(fPath, json);
        }
    }
}
