namespace SpawnDev.BlazorJS.FromTypeScript.Services
{
    //
    // Summary:
    //     Changes that might occur to a file or directory.
    [Flags]
    public enum FileSystemChangeType
    {
        //
        // Summary:
        //     The creation of a file or folder.
        //     Will fire for most folder creation events, but not for files. Files will fire changed events when modified or created.
        Created = 1,
        //
        // Summary:
        //     The deletion of a file or folder.
        Deleted = 2,
        //
        // Summary:
        //     The change of a file or folder. The types of changes include: changes to size,
        //     attributes, security settings, last write, and last access time.
        //     This may be called when a file is created instead of the Created.
        Changed = 4,
    }
}
