using SpawnDev.BlazorJS.FromTypeScript.Services;

namespace SpawnDev.BlazorJS.FromTypeScript.Components
{
    public class EditorInstance
    {
        static long Id = 0;
        public long InstanceId { get; } = ++Id;
        public string FullPath { get; private set; }
        public bool Closed { get; set; }
        public bool UnsavedChanges { get; set; }
        public string TabText => IOPath.GetFileName(FullPath) + (UnsavedChanges ? " *" : "");
        public EditorInstance(string fullPath)
        {
            FullPath  = fullPath;
        }
    }
}
