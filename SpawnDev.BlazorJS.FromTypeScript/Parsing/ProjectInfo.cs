using File = SpawnDev.BlazorJS.JSObjects.File;

namespace SpawnDev.BlazorJS.FromTypeScript.Parsing
{
    public class ProjectInfo
    {
        /// <summary>
        /// Blazor project name. Used as the base namespace for created objects.
        /// </summary>
        public string ProjectName { get; set; }
        /// <summary>
        /// The global namespace the Javascript module will be loaded to.<br/>
        /// Example: Three.js can be loaded to 'THREE'; therefore when a THREE.Scene is created in Blazor 'JS.New("THREE.Scene")' will be used.
        /// </summary>
        public string JSModuleNamespace { get; set; } = "";
        /// <summary>
        /// If true, the generated namespace each class/enum will take the path into account. [ProjectName].[PATH].[CLASS_NAME]<br/>
        /// Otherwise, just ProjectName is used. [ProjectName].[CLASS_NAME]
        /// </summary>
        public bool NameSpaceFromPath { get; set; }
        /// <summary>
        /// If true, methods and property names will have their first letter changed to uppercase.<br/>
        /// If false, the names will not be changed. 
        /// </summary>
        public bool UseCSNaming { get; set; } = true;
        /// <summary>
        /// The path to the source in the async file system
        /// </summary>
        public string Source { get; set; }
    }
}
