
namespace SpawnDev.BlazorJS.AsyncFileSystem
{
    public class FileSystemChangeEventArgs
    {
        public FileSystemChangeType ChangeType { get; private set; }
        public string Path { get; private set; }
        public FileSystemChangeEventArgs(FileSystemChangeType changeType, string path)
        {
            ChangeType = changeType;
            Path = path;
        }
    }
}
