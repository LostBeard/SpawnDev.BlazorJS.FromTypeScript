namespace SpawnDev.BlazorJS.FromTypeScript.Services
{
    public class ASyncFSEntryInfo
    {
        public string Directory { get; init; }
        public string Name { get; init; }
        public string FullPath { get; init; }
        public long LastModified { get; init; }
        public DateTimeOffset LastModifiedDate { get; init; }
        public long Size { get; init; }
        public bool IsDirectory { get; init; }
        public ASyncFSEntryInfo() { }
        public ASyncFSEntryInfo(bool isDirectory, string name, string directory, long lastModified = 0, long size = 0) 
            => (IsDirectory, Name, Directory, Size, LastModified, LastModifiedDate, FullPath)
            = (isDirectory, name, directory, size, lastModified, DateTimeOffset.FromUnixTimeMilliseconds(lastModified), $"{directory}/{name}".Trim('/'));
    }
}
