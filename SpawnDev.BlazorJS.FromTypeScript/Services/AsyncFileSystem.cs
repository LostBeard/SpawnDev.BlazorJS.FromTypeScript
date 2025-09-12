using SpawnDev.BlazorJS.JSObjects;
using SpawnDev.BlazorJS.Toolbox;
using System.Text.Json;

namespace SpawnDev.BlazorJS.FromTypeScript.Services
{

    /// <summary>
    /// An asynchronous file system for the browser that uses the FileSystemDirectory returned by navigator.getDirectory()
    /// </summary>
    public class AsyncFileSystem : IAsyncBackgroundService, IAsyncFileSystem
    {
        public Task Ready => _Ready ??= InitAsync();
        Task? _Ready;
        BlazorJSRuntime JS;
        public StorageManager Storage { get; private set; }
        public event EventHandler<FileSystemChangeEventArgs> FileSystemChanged = default!;
        FileSystemDirectoryHandle? Root;
        /// <summary>
        /// Creates new instance
        /// </summary>
        /// <param name="js"></param>
        public AsyncFileSystem(BlazorJSRuntime js)
        {
            JS = js;
            using var navigator = JS.Get<Navigator>("navigator");
            Storage = navigator.Storage;
        }
        async Task InitAsync()
        {
            Root = await Storage.GetDirectory();
        }
        #region  Directory
        /// <summary>
        /// Returns the directory names in the given relative path
        /// </summary>
        /// <param name="_this"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public async Task<List<string>> GetDirectories(string path)
        {
            using var dir = await GetDirectoryHandle(path, false);
            var values = await dir!.ValuesList();
            var dirs = values.Where(o => o is FileSystemDirectoryHandle).Select(o => (FileSystemDirectoryHandle)o);
            var ret = dirs.Select(o => o.Name).ToList();
            values.ToArray().DisposeAll();
            return ret;
        }
        /// <summary>
        /// Returns the directory names in the specified path
        /// </summary>
        /// <returns></returns>
        public async Task<List<string>> GetEntries(string path)
        {
            using var dir = await GetDirectoryHandle(path, false);
            var values = await dir!.ValuesList();
            var ret = values.Where(o => o is FileSystemDirectoryHandle).Select(o => o.Name + "/").ToList();
            var files = values.Where(o => o is FileSystemFileHandle).Select(o => o.Name);
            ret.AddRange(files);
            values.ToArray().DisposeAll();
            return ret;
        }
        /// <summary>
        /// Returns the directory names in the specified path
        /// </summary>
        /// <returns></returns>
        public async IAsyncEnumerable<ASyncFSEntryInfo> EnumerateInfos(string path, bool recursive = false)
        {
            var toScan = new Queue<string>();
            toScan.Enqueue(path);
            while (toScan.Any())
            {
                path = toScan.Dequeue();
                using var dir = await GetDirectoryHandle(path, false);
                var values = await dir!.ValuesList();
                foreach (var entry in values)
                {
                    if (entry is FileSystemDirectoryHandle fileSystemDirectoryHandle)
                    {
                        var info = new ASyncFSEntryInfo(true, entry.Name, path);
                        yield return info;
                        if (recursive)
                        {
                            toScan.Enqueue(IOPath.Combine(path, entry.Name));
                        }
                    }
                    else if (entry is FileSystemFileHandle fileSystemFileHandle)
                    {
                        using var file = await fileSystemFileHandle.GetFile();
                        var info = new ASyncFSEntryInfo(false, entry.Name, path, file.LastModified, file.Size);
                        yield return info;
                    }
                }
            }
        }
        /// <summary>
        /// Returns the directory names in the specified path
        /// </summary>
        /// <returns></returns>
        public async Task<List<ASyncFSEntryInfo>> GetInfos(string path, bool recursive = false)
        {
            var ret = new List<ASyncFSEntryInfo>();
            var infosAsyncEnum = EnumerateInfos(path, recursive);
            await foreach(var info in infosAsyncEnum)
            {
                ret.Add(info);
            }
            return ret;
        }
        /// <summary>
        /// Returns the info of the specified path or null
        /// </summary>
        /// <returns></returns>
        public async Task<ASyncFSEntryInfo?> GetInfo(string path)
        {
            var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var dir = parts.Length <= 1 ? "" : string.Join("/", parts.Take(parts.Length - 1));
            using var entry = await GetHandle(path);
            if (entry is FileSystemDirectoryHandle fileSystemDirectoryHandle)
            {
                return new ASyncFSEntryInfo(true, entry.Name, dir);
            }
            else if (entry is FileSystemFileHandle fileSystemFileHandle)
            {
                using var file = await fileSystemFileHandle.GetFile();
                return new ASyncFSEntryInfo(false, entry.Name, dir, file.LastModified, file.Size);
            }
            return null;
        }
        /// <summary>
        /// Returns the FileSystemFileHandle names in the given relative path
        /// </summary>
        /// <param name="_this"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public async Task<List<string>> GetFiles(string path)
        {
            using var dir = await GetDirectoryHandle(path, false);
            var values = await dir!.ValuesList();
            var files = values.Where(o => o is FileSystemFileHandle).Select(o => (FileSystemFileHandle)o);
            var ret = files.Select(o => o.Name).ToList();
            values.ToArray().DisposeAll();
            return ret;
        }
        /// <summary>
        /// Returns true if the path exists
        /// </summary>
        /// <param name="_this"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public async Task<bool> Exists(string path)
        {
            using var dir = await GetHandle(path);
            return dir != null;
        }
        /// <summary>
        /// Returns true if a directory exists at the given path
        /// </summary>
        /// <param name="_this"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public async Task<bool> DirectoryExists(string path)
        {
            using var dir = await GetDirectoryHandle(path, false);
            return dir != null;
        }
        /// <summary>
        /// Returns true if a file exists at the given path
        /// </summary>
        /// <param name="_this"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public async Task<bool> FileExists(string path)
        {
            using var file = await GetFileHandle(path, false);
            return file != null;
        }
        /// <summary>
        /// Creates the given path or throws an exception
        /// </summary>
        /// <param name="_this"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task CreateDirectory(string path)
        {
            var pathParts = path.Split("/", StringSplitOptions.RemoveEmptyEntries).ToList();
            if (pathParts.Count == 0) throw new Exception("CreateDirectory failed: invalid path");
            var curDir = Root!;
            FileSystemDirectoryHandle? nextDir;
            var rootDir = "";
            for (var i = 0; i < pathParts.Count - 1; i++)
            {
                var pathPart = pathParts[i];
                nextDir = await curDir.GetDirectoryHandle(pathPart, true);
                if (Root != curDir) curDir.Dispose();
                if (nextDir == null) throw new Exception("CreateDirectory failed: failed to create path");
                rootDir += rootDir == "" ? pathPart : $"/{pathPart}";
                curDir = nextDir;
            }
            var fileName = pathParts.Last();
            var ret = await curDir.GetDirectoryHandle(fileName, true);
            if (Root != curDir) curDir.Dispose();
            FileSystemChanged?.Invoke(this, new FileSystemChangeEventArgs(FileSystemChangeType.Created, path));
        }
        public async Task Reset()
        {
            var dirs = await GetDirectories("");
            foreach(var d in dirs)
            {
                try
                {
                    await Remove(d, true);
                }
                catch { }
            }
            var files = await GetFiles("");
            foreach (var d in files)
            {
                try
                {
                    await Remove(d);
                }
                catch { }
            }
        }
        /// <summary>
        /// Removes the entry at the given relative path
        /// </summary>
        /// <param name="_this"></param>
        /// <param name="path"></param>
        /// <param name="recursive"></param>
        /// <returns></returns>
        public async Task Remove(string path, bool recursive = false)
        {
            var pathParts = path.Split("/", StringSplitOptions.RemoveEmptyEntries).ToList();
            if (pathParts.Count == 0) return;
            var curDir = Root!;
            FileSystemDirectoryHandle? nextDir;
            for (var i = 0; i < pathParts.Count - 1; i++)
            {
                var pathPart = pathParts[i];
                try
                {
                    nextDir = await curDir.GetDirectoryHandle(pathPart, false);
                }
                catch
                {
                    return;
                }
                if (Root != curDir) curDir.Dispose();
                if (nextDir == null) return;
                curDir = nextDir;
            }
            var fileName = pathParts.Last();
            try
            {
                await curDir.RemoveEntry(fileName, recursive);
            }
            catch
            {
                // can throw if the entry does not exist, in which case we got what we wanted
            }
            FileSystemChanged?.Invoke(this, new FileSystemChangeEventArgs(FileSystemChangeType.Deleted, path));
        }
        #endregion
        #region Write
        /// <summary>
        /// Write the data to the file, the file will be created if it does not exist
        /// </summary>
        /// <param name="_this"></param>
        /// <param name="path"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task Write(string path, ArrayBuffer data)
        {
            using var fileHandle = await Root!.GetPathFileHandle(path, true);
            using var stream = await fileHandle!.CreateWritable();
            await stream.Write(data);
            stream.Close();
            FileSystemChanged?.Invoke(this, new FileSystemChangeEventArgs(FileSystemChangeType.Changed, path));
        }
        /// <summary>
        /// Write the data to the file, the file will be created if it does not exist
        /// </summary>
        /// <param name="_this"></param>
        /// <param name="path"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task Write(string path, Blob data)
        {
            using var fileHandle = await Root!.GetPathFileHandle(path, true);
            using var stream = await fileHandle!.CreateWritable();
            await stream.Write(data);
            stream.Close();
            FileSystemChanged?.Invoke(this, new FileSystemChangeEventArgs(FileSystemChangeType.Changed, path));
        }
        /// <summary>
        /// Write the data to the file, the file will be created if it does not exist
        /// </summary>
        /// <param name="_this"></param>
        /// <param name="path"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task Write(string path, Stream data)
        {
            using var fileHandle = await Root!.GetPathFileHandle(path, true);
            await fileHandle!.Write(data);
            FileSystemChanged?.Invoke(this, new FileSystemChangeEventArgs(FileSystemChangeType.Changed, path));
        }
        /// <summary>
        /// Write the data to the file, the file will be created if it does not exist
        /// </summary>
        /// <param name="_this"></param>
        /// <param name="path"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task Write(string path, TypedArray data)
        {
            using var fileHandle = await Root!.GetPathFileHandle(path, true);
            using var stream = await fileHandle!.CreateWritable();
            await stream.Write(data);
            stream.Close();
            FileSystemChanged?.Invoke(this, new FileSystemChangeEventArgs(FileSystemChangeType.Changed, path));
        }
        /// <summary>
        /// Write the data to the file, the file will be created if it does not exist
        /// </summary>
        /// <param name="_this"></param>
        /// <param name="path"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task Write(string path, byte[] data)
        {
            using var fileHandle = await Root!.GetPathFileHandle(path, true);
            using var stream = await fileHandle!.CreateWritable();
            await stream.Write(data);
            stream.Close();
            FileSystemChanged?.Invoke(this, new FileSystemChangeEventArgs(FileSystemChangeType.Changed, path));
        }
        /// <summary>
        /// Write the data to the file, the file will be created if it does not exist
        /// </summary>
        /// <param name="_this"></param>
        /// <param name="path"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task Write(string path, DataView data)
        {
            using var fileHandle = await Root!.GetPathFileHandle(path, true);
            using var stream = await fileHandle!.CreateWritable();
            await stream.Write(data);
            stream.Close();
            FileSystemChanged?.Invoke(this, new FileSystemChangeEventArgs(FileSystemChangeType.Changed, path));
        }
        /// <summary>
        /// Write the data to the file, the file will be created if it does not exist
        /// </summary>
        /// <param name="_this"></param>
        /// <param name="path"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task Write(string path, string data)
        {
            using var fileHandle = await Root!.GetPathFileHandle(path, true);
            using var stream = await fileHandle!.CreateWritable();
            await stream.Write(data);
            stream.Close();
            FileSystemChanged?.Invoke(this, new FileSystemChangeEventArgs(FileSystemChangeType.Changed, path));
        }
        /// <summary>
        /// Write the data to the file, the file will be created if it does not exist
        /// </summary>
        /// <param name="_this"></param>
        /// <param name="path"></param>
        /// <param name="data"></param>
        /// <param name="jsonSerializerOptions"></param>
        /// <returns></returns>
        public async Task WriteJSON(string path, object data, JsonSerializerOptions? jsonSerializerOptions = null)
        {
            var json = JsonSerializer.Serialize(data, jsonSerializerOptions);
            using var fileHandle = await Root!.GetPathFileHandle(path, true);
            using var stream = await fileHandle!.CreateWritable();
            await stream.Write(json);
            stream.Close();
            FileSystemChanged?.Invoke(this, new FileSystemChangeEventArgs(FileSystemChangeType.Changed, path));
        }
        /// <summary>
        /// Write the data to the file, the file will be created if it does not exist
        /// </summary>
        /// <param name="_this"></param>
        /// <param name="path"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task Write(string path, FileSystemWriteOptions data)
        {
            using var fileHandle = await Root!.GetPathFileHandle(path, true);
            using var stream = await fileHandle!.CreateWritable();
            await stream.Write(data);
            stream.Close();
            FileSystemChanged?.Invoke(this, new FileSystemChangeEventArgs(FileSystemChangeType.Changed, path));
        }
        #endregion
        #region Append
        /// <summary>
        /// Append data to the file, the file will be created if it does not exist
        /// </summary>
        /// <param name="_this"></param>
        /// <param name="path"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task Append(string path, ArrayBuffer data)
        {
            using var fileHandle = await Root!.GetPathFileHandle(path, true);
            using var stream = await fileHandle!.CreateWritable(new FileSystemCreateWritableOptions { KeepExistingData = true });
            await stream.Write(data);
            stream.Close();
            FileSystemChanged?.Invoke(this, new FileSystemChangeEventArgs(FileSystemChangeType.Changed, path));
        }
        /// <summary>
        /// Append data to the file, the file will be created if it does not exist
        /// </summary>
        /// <param name="_this"></param>
        /// <param name="path"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task Append(string path, Blob data)
        {
            using var fileHandle = await Root!.GetPathFileHandle(path, true);
            using var stream = await fileHandle!.CreateWritable(new FileSystemCreateWritableOptions { KeepExistingData = true }); ;
            await stream.Write(data);
            stream.Close();
            FileSystemChanged?.Invoke(this, new FileSystemChangeEventArgs(FileSystemChangeType.Changed, path));
        }
        /// <summary>
        /// Append data to the file, the file will be created if it does not exist
        /// </summary>
        /// <param name="_this"></param>
        /// <param name="path"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task Append(string path, TypedArray data)
        {
            using var fileHandle = await Root!.GetPathFileHandle(path, true);
            using var stream = await fileHandle!.CreateWritable(new FileSystemCreateWritableOptions { KeepExistingData = true });
            await stream.Write(data);
            stream.Close();
            FileSystemChanged?.Invoke(this, new FileSystemChangeEventArgs(FileSystemChangeType.Changed, path));
        }
        /// <summary>
        /// Append data to the file, the file will be created if it does not exist
        /// </summary>
        /// <param name="_this"></param>
        /// <param name="path"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task Append(string path, byte[] data)
        {
            using var fileHandle = await Root!.GetPathFileHandle(path, true);
            using var stream = await fileHandle!.CreateWritable(new FileSystemCreateWritableOptions { KeepExistingData = true });
            await stream.Write(data);
            stream.Close();
            FileSystemChanged?.Invoke(this, new FileSystemChangeEventArgs(FileSystemChangeType.Changed, path));
        }
        public async Task Append(string path, Stream data)
        {
            using var fileHandle = await Root!.GetPathFileHandle(path, true);
            await fileHandle!.Append(data);
            FileSystemChanged?.Invoke(this, new FileSystemChangeEventArgs(FileSystemChangeType.Changed, path));
        }
        /// <summary>
        /// Append data to the file, the file will be created if it does not exist
        /// </summary>
        /// <param name="_this"></param>
        /// <param name="path"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task Append(string path, DataView data)
        {
            using var fileHandle = await Root!.GetPathFileHandle(path, true);
            using var stream = await fileHandle!.CreateWritable(new FileSystemCreateWritableOptions { KeepExistingData = true });
            await stream.Write(data);
            stream.Close();
            FileSystemChanged?.Invoke(this, new FileSystemChangeEventArgs(FileSystemChangeType.Changed, path));
        }
        /// <summary>
        /// Append data to the file, the file will be created if it does not exist
        /// </summary>
        /// <param name="_this"></param>
        /// <param name="path"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task Append(string path, string data)
        {
            using var fileHandle = await Root!.GetPathFileHandle(path, true);
            using var stream = await fileHandle!.CreateWritable(new FileSystemCreateWritableOptions { KeepExistingData = true });
            await stream.Write(data);
            stream.Close();
            FileSystemChanged?.Invoke(this, new FileSystemChangeEventArgs(FileSystemChangeType.Changed, path));
        }
        /// <summary>
        /// Append data to the file, the file will be created if it does not exist
        /// </summary>
        /// <param name="_this"></param>
        /// <param name="path"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task Append(string path, FileSystemWriteOptions data)
        {
            using var fileHandle = await Root!.GetPathFileHandle(path, true);
            using var stream = await fileHandle!.CreateWritable(new FileSystemCreateWritableOptions { KeepExistingData = true });
            await stream.Write(data);
            stream.Close();
            FileSystemChanged?.Invoke(this, new FileSystemChangeEventArgs(FileSystemChangeType.Changed, path));
        }
        #endregion
        #region Read
        /// <summary>
        /// Read the data from the file as T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="_this"></param>
        /// <param name="path"></param>
        /// <param name="jsonSerializerOptions"></param>
        /// <returns></returns>
        public async Task<T> ReadJSON<T>(string path, JsonSerializerOptions? jsonSerializerOptions = null)
        {
            using var fileHandle = await Root!.GetPathFileHandle(path, false);
            if (fileHandle == null) throw new FileNotFoundException();
            using var file = await fileHandle.GetFile();
            var ret = await file.Text();
            var obj = JsonSerializer.Deserialize<T>(ret, jsonSerializerOptions);
            return obj!;
        }
        /// <summary>
        /// Read the data from the file as a string
        /// </summary>
        /// <param name="_this"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public async Task<string> ReadText(string path)
        {
            using var fileHandle = await Root!.GetPathFileHandle(path, false);
            if (fileHandle == null) throw new FileNotFoundException();
            using var file = await fileHandle.GetFile();
            var ret = await file.Text();
            return ret;
        }
        /// <summary>
        /// Read the data from the file as a Stream
        /// </summary>
        /// <param name="_this"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public async Task<ArrayBufferStream> ReadStream(string path)
        {
            using var fileHandle = await Root!.GetPathFileHandle(path, false);
            if (fileHandle == null) throw new FileNotFoundException();
            var arrayBuffer = await fileHandle.ReadArrayBuffer();
            var stream = new ArrayBufferStream(arrayBuffer);
            return stream;
        }
        /// <summary>
        /// Read the data from the file as a byte array
        /// </summary>
        /// <param name="_this"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public async Task<byte[]> ReadBytes(string path)
        {
            using var fileHandle = await Root!.GetPathFileHandle(path, false);
            if (fileHandle == null) throw new FileNotFoundException();
            using var file = await fileHandle.GetFile();
            using var ret = await file.ArrayBuffer();
            return ret.ReadBytes();
        }
        /// <summary>
        /// Read the data from the file as an ArrayBuffer
        /// </summary>
        /// <param name="_this"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public async Task<ArrayBuffer> ReadArrayBuffer(string path)
        {
            using var fileHandle = await Root!.GetPathFileHandle(path, false);
            if (fileHandle == null) throw new FileNotFoundException();
            using var file = await fileHandle.GetFile();
            var ret = await file.ArrayBuffer();
            return ret;
        }
        /// <summary>
        /// Returns the file as a Uint8Array
        /// </summary>
        /// <param name="_this"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        /// <exception cref="FileNotFoundException"></exception>
        public async Task<Uint8Array> ReadUint8Array(string path)
        {
            using var fileHandle = await Root!.GetPathFileHandle(path, false);
            if (fileHandle == null) throw new FileNotFoundException();
            using var file = await fileHandle.GetFile();
            using var arrayBuffer = await file.ArrayBuffer();
            return new Uint8Array(arrayBuffer);
        }
        /// <summary>
        /// Returns the file as a TypedArray
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="_this"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        /// <exception cref="FileNotFoundException"></exception>
        public async Task<T> ReadTypedArray<T>(string path) where T : TypedArray
        {
            using var fileHandle = await Root!.GetPathFileHandle(path, false);
            if (fileHandle == null) throw new FileNotFoundException();
            using var file = await fileHandle.GetFile();
            using var arrayBuffer = await file.ArrayBuffer();
            var ret = Activator.CreateInstance(typeof(T), arrayBuffer)!;
            return (T)ret;
        }
        /// <summary>
        /// Read the data from the file as File
        /// </summary>
        /// <param name="_this"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        /// <exception cref="FileNotFoundException"></exception>
        public async Task<JSObjects.File> ReadFile(string path)
        {
            using var fileHandle = await Root!.GetPathFileHandle(path, false);
            if (fileHandle == null) throw new FileNotFoundException();
            var file = await fileHandle.GetFile();
            return file;
        }
        #endregion
        #region FileSystemHandle specific
        /// <summary>
        /// Returns the path handle or null if it does not exist
        /// </summary>
        /// <param name="_this"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public async Task<FileSystemHandle?> GetHandle(string path)
        {
            return string.IsNullOrEmpty(path) ? Root!.JSRefCopy<FileSystemDirectoryHandle>() : await Root!.GetPathHandle(path);
        }
        public Task<FileSystemFileHandle?> GetFileHandle(string path) => GetFileHandle(path, false);
        public Task<FileSystemDirectoryHandle?> GetDirectoryHandle(string path) => GetDirectoryHandle(path, false);
        async Task<FileSystemDirectoryHandle?> GetDirectoryHandle(string path, bool create)
        {
            return string.IsNullOrEmpty(path) ? Root!.JSRefCopy<FileSystemDirectoryHandle>() : await Root!.GetPathDirectoryHandle(path, create);
        }
        async Task<FileSystemFileHandle?> GetFileHandle(string path, bool create)
        {
            return string.IsNullOrEmpty(path) ? null : await Root!.GetPathFileHandle(path, create);
        }
        #endregion
    }
}
