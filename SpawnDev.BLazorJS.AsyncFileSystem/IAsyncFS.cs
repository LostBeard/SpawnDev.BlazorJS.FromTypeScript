using System.Text.Json;

namespace SpawnDev.BlazorJS.AsyncFileSystem
{
    public interface IAsyncFS
    {
        event EventHandler<FileSystemChangeEventArgs> FileSystemChanged;
        Task Append(string path, Stream data);
        Task Append(string path, string data);
        Task Append(string path, byte[] data);
        Task CreateDirectory(string path);
        Task<bool> DirectoryExists(string path);
        Task<bool> Exists(string path);
        Task<bool> FileExists(string path);
        Task<List<string>> GetDirectories(string path);
        Task<List<string>> GetEntries(string path);
        Task<List<string>> GetFiles(string path);
        Task<ASyncFSEntryInfo?> GetInfo(string path);
        Task<List<ASyncFSEntryInfo>> GetInfos(string path, bool recursive = false);
        IAsyncEnumerable<ASyncFSEntryInfo> EnumerateInfos(string path, bool recursive = false);
        Task<byte[]> ReadBytes(string path);
        Task<T> ReadJSON<T>(string path, JsonSerializerOptions? jsonSerializerOptions = null);
        Task<Stream> ReadStream(string path);
        Task<string> ReadText(string path);
        Task Remove(string path, bool recursive = false);
        Task Write(string path, Stream data);
        Task Write(string path, string data);
        Task Write(string path, byte[] data);
        Task WriteJSON(string path, object data, JsonSerializerOptions? jsonSerializerOptions = null);
    }
}
