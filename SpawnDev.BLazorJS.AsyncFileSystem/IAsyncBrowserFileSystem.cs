using SpawnDev.BlazorJS.JSObjects;

namespace SpawnDev.BlazorJS.AsyncFileSystem
{
    public interface IAsyncBrowserFileSystem : IAsyncFS
    {
        Task Append(string path, ArrayBuffer data);
        Task Append(string path, Blob data);
        Task Append(string path, DataView data);
        Task Append(string path, FileSystemWriteOptions data);
        Task Append(string path, TypedArray data);
        Task<T> ReadTypedArray<T>(string path) where T : TypedArray;
        Task<JSObjects.File> ReadFile(string path);
        Task<ArrayBuffer> ReadArrayBuffer(string path);
        Task<Uint8Array> ReadUint8Array(string path);
        Task Write(string path, ArrayBuffer data);
        Task Write(string path, Blob data);
        Task Write(string path, DataView data);
        Task Write(string path, FileSystemWriteOptions data);
        Task Write(string path, TypedArray data);
        Task<FileSystemDirectoryHandle?> GetDirectoryHandle(string path);
        Task<FileSystemFileHandle?> GetFileHandle(string path);
        Task<FileSystemHandle?> GetHandle(string path);
    }
}
