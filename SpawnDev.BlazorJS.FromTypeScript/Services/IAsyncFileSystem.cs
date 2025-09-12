using SpawnDev.BlazorJS.JSObjects;
using SpawnDev.BlazorJS.Toolbox;
using System.Text.Json;

namespace SpawnDev.BlazorJS.FromTypeScript.Services
{
    public interface IAsyncFileSystem
    {
        event EventHandler<FileSystemChangeEventArgs> FileSystemChanged;
        Task Append(string path, ArrayBuffer data);
        Task Append(string path, Blob data);
        Task Append(string path, byte[] data);
        Task Append(string path, DataView data);
        Task Append(string path, FileSystemWriteOptions data);
        Task Append(string path, Stream data);
        Task Append(string path, string data);
        Task Append(string path, TypedArray data);
        Task CreateDirectory(string path);
        Task<bool> DirectoryExists(string path);
        Task<bool> Exists(string path);
        Task<bool> FileExists(string path);
        Task<List<string>> GetDirectories(string path);
        Task<FileSystemDirectoryHandle?> GetDirectoryHandle(string path);
        Task<List<string>> GetEntries(string path);
        Task<FileSystemFileHandle?> GetFileHandle(string path);
        Task<List<string>> GetFiles(string path);
        Task<FileSystemHandle?> GetHandle(string path);
        Task<ASyncFSEntryInfo?> GetInfo(string path);
        Task<List<ASyncFSEntryInfo>> GetInfos(string path, bool recursive = false);
        IAsyncEnumerable<ASyncFSEntryInfo> EnumerateInfos(string path, bool recursive = false);
        Task<ArrayBuffer> ReadArrayBuffer(string path);
        Task<byte[]> ReadBytes(string path);
        Task<JSObjects.File> ReadFile(string path);
        Task<T> ReadJSON<T>(string path, JsonSerializerOptions? jsonSerializerOptions = null);
        Task<ArrayBufferStream> ReadStream(string path);
        Task<string> ReadText(string path);
        Task<T> ReadTypedArray<T>(string path) where T : TypedArray;
        Task<Uint8Array> ReadUint8Array(string path);
        Task Remove(string path, bool recursive = false);
        Task Write(string path, ArrayBuffer data);
        Task Write(string path, Blob data);
        Task Write(string path, byte[] data);
        Task Write(string path, DataView data);
        Task Write(string path, FileSystemWriteOptions data);
        Task Write(string path, Stream data);
        Task Write(string path, string data);
        Task Write(string path, TypedArray data);
        Task WriteJSON(string path, object data, JsonSerializerOptions? jsonSerializerOptions = null);
    }
}
