using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Radzen;
using Radzen.Blazor;
using SpawnDev.BlazorJS.FromTypeScript.Components;
using SpawnDev.BlazorJS.FromTypeScript.Layout.AppTray;
using SpawnDev.BlazorJS.FromTypeScript.Parsing;
using SpawnDev.BlazorJS.FromTypeScript.Services;
using SpawnDev.MatrixLEDDisplay.Demo.Services;
using System.IO.Compression;
using File = SpawnDev.BlazorJS.JSObjects.File;
using Type = System.Type;

namespace SpawnDev.BlazorJS.FromTypeScript.Layout
{
    public partial class MainLayout
    {


        [Inject]
        ProgressModalService ProgressModalService { get; set; } = default!;
        [Inject]
        FileIconService FileIconService { get; set; } = default!;
        [Inject]
        AssetManifestService AssetManifestService { get; set; } = default!;
        [Inject]
        ContextMenuService ContextMenuService { get; set; } = default!;
        [Inject]
        FileService FileService { get; set; } = default!;
        [Inject]
        AsyncFileSystem FS { get; set; } = default!;
        [Inject]
        BlazorJSRuntime JS { get; set; } = default!;
        [Inject]
        NotificationService NotificationService { get; set; } = default!;
        [Inject]
        DialogService DialogService { get; set; } = default!;
        [Inject]
        AppTrayService TrayIconService { get; set; } = default!;
        [Inject]
        protected NavigationManager NavigationManager { get; set; } = default!;
        [Inject]
        MainLayoutService MainLayoutService { get; set; } = default!;
        [Inject]
        ThemeService ThemeService { get; set; } = default!;

        bool Busy => ProgressModalService.Visible;
        string Title => MainLayoutService.Title;
        bool leftSidebarExpanded = false;
        bool rightSidebarExpanded = false;
        bool rightSidebarEnabled = false;
        public Type? PageType { get; private set; }
        public string PageTypeName => PageType?.Name ?? "";
        public string Location { get; private set; } = "";
        public string? HistoryEntryState { get; private set; }
        public DateTime LocationUpdated { get; private set; } = DateTime.MinValue;
        protected override void OnInitialized()
        {
            NavigationManager.LocationChanged += NavigationManager_LocationChanged;
            MainLayoutService.OnTitleChanged += MainLayoutService_OnTitleChanged;
            FS.FileSystemChanged += FS_FileSystemChanged;
            if (_delayedReload == null)
            {
                _delayedReload = new System.Timers.Timer(500);
                _delayedReload.Elapsed += _delayedReload_Elapsed;
                _delayedReload.AutoReset = false;
            }
        }

        private async void _delayedReload_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            JS.Log("_delayedReload_Elapsed");
            if (ProgressModalService.Visible)
            {
                _delayedReload?.Start();
                return;
            }
            if (tree != null) await tree.Reload();
            StateHasChanged();
        }

        System.Timers.Timer? _delayedReload = null;
        private void FS_FileSystemChanged(object? sender, FileSystemChangeEventArgs e)
        {
            _delayedReload?.Stop();
            _delayedReload?.Start();
        }

        private void MainLayoutService_OnTitleChanged()
        {
            StateHasChanged();
        }
        private void NavigationManager_LocationChanged(object? sender, Microsoft.AspNetCore.Components.Routing.LocationChangedEventArgs e)
        {
            AfterLocationChanged(e.HistoryEntryState);
        }
        protected override void OnAfterRender(bool firstRender)
        {
            _awaitAfterRender?.SetResult();
            _awaitAfterRender = null;
            MainLayoutService.TriggerOnAfterRender(this, firstRender);
            if (firstRender)
            {
                AfterLocationChanged();
            }
        }
        void AfterLocationChanged(string? historyEntryState = null)
        {
            var pageType = Body != null && Body.Target != null && Body.Target is RouteView routeView ? routeView.RouteData.PageType : null;
            var location = NavigationManager.Uri;
            if (PageType == pageType && Location == location)
            {
#if DEBUG
                Console.WriteLine($"SendLocationChanged: false");
#endif
                return;
            }
            LocationUpdated = DateTime.Now;
            PageType = pageType;
            Location = location;
            HistoryEntryState = historyEntryState;
#if DEBUG
            Console.WriteLine($"LocationChanged: {PageTypeName} [{HistoryEntryState ?? ""}] {Location}");
#endif
        }
        ASyncFSEntryInfo? SelectedFile = null;

        string? SelectedTypeScriptLibraryRootPath => GetLibraryRoot(SelectedFile?.FullPath);
        string? SelectedTypeScriptLibraryName => GetLibraryName(SelectedFile?.FullPath);

        async Task<bool> CheckUnsavedChanges(EditorInstance editor)
        {
            // TODO - compare editor to file version or use flag to determine  unsaved changes
            if (editor.UnsavedChanges)
            {
                // TODO - change this to question asking if they want to save before closing with options - Yes, No, Cancel
                var resp = await DialogService.Confirm($"{IOPath.GetFilename(editor.FullPath)} has unsaved changes. Are you sure you want to close it?", "Unsaved changes",
                    new ConfirmOptions
                    {
                        CloseDialogOnEsc = true,
                        OkButtonText = "Close without saving",
                        CancelButtonText = "Cancel"
                    });
                return resp == true;
            }
            return true;
        }
        async Task CloseEntry(string fileName)
        {
            var editor = OpenEditors.LastOrDefault(o => o.FullPath == fileName);
            if (editor == null || editor.Closed) return;
            var confirmed = await CheckUnsavedChanges(editor);
            if (!confirmed) return;
            var i = Editors.IndexOf(editor);
            var openIndexes = OpenEditors.Select(o => Editors.IndexOf(o)).ToList();
            editor.Closed = true;
            if (selectedIndex == i)
            {
                if (openIndexes.Count == 1)
                {
                    // nothing to do. no other open editors to set
                    selectedIndex = 0;
                }
                else
                {
                    var n = openIndexes.IndexOf(i);
                    if (n == openIndexes.Count - 1)
                    {
                        // non-closed before
                        selectedIndex = openIndexes[n - 1];
                    }
                    else if (n < openIndexes.Count - 1)
                    {
                        // non-closed after
                        selectedIndex = openIndexes[n + 1];
                    }
                }
            }
            StateHasChanged();
        }
        public string? GetLibraryRoot(string path)
        {
            return !string.IsNullOrEmpty(path) && path.Split('/', StringSplitOptions.TrimEntries | StringSplitOptions.TrimEntries).Length >= 2 ? string.Join("/", path.Split('/', StringSplitOptions.TrimEntries | StringSplitOptions.TrimEntries).Take(2)) : null;
        }
        public string? GetLibraryName(string path)
        {
            var libraryRoot = GetLibraryRoot(path);
            return !string.IsNullOrEmpty(libraryRoot) ? libraryRoot.Split("/").Last() : null;
        }
        public bool IsLibraryRoot(string path) => GetLibraryRoot(path) == path;
        public bool IsInLibrary(string path) => !string.IsNullOrEmpty(GetLibraryRoot(path));
        async Task OnEntryClick(MouseEventArgs args, ASyncFSEntryInfo f)
        {
            var oldSelected = SelectedFile;
            SelectedFile = f;
            var changed = oldSelected?.FullPath != SelectedFile?.FullPath;
            JS.Log("Click", SelectedFile?.FullPath);
            JS.Log("SelectedTypeScriptLibraryName", SelectedTypeScriptLibraryName);
            if (!changed) return;
            if (f == null) return;

            if (!f.IsDirectory)
            {
                OpenPath(f.FullPath);
            }
            ////  ....
            //if (!f.IsDirectory && !OpenFiles.Contains(f.FullPath))
            //{
            //    OpenFiles.Add(f.FullPath);
            //}
            //var index = OpenFiles.IndexOf(f.FullPath);
            //if (index > -1)
            //{
            //    await RenderTick();
            //    selectedIndex = index;
            //}
            StateHasChanged();
        }
        async Task Tabs_OnChange(int index)
        {

        }
        RadzenTabs? tabs;
        int selectedIndex = 0;
        //List<string> OpenFiles = new List<string>();
        List<EditorInstance> Editors = new List<EditorInstance>();
        List<EditorInstance> OpenEditors => Editors.Where(o => !o.Closed).ToList();

        void OpenPath(string fullPath)
        {
            var openEditor = OpenEditors.FirstOrDefault(o => o.FullPath == fullPath);
            if (openEditor == null)
            {
                openEditor = new EditorInstance(fullPath);
                Editors.Add(openEditor);
            }
            selectedIndex = Editors.IndexOf(openEditor);
            StateHasChanged();
        }
        async Task ContextMenu(MouseEventArgs args, ASyncFSEntryInfo f)
        {
            if (f == null) return;
            JS.Log("ContextMenu", f.FullPath);
            var options = new List<ContextMenuItem>();
            //
            string? libraryRoot = f.FullPath.StartsWith($"{FileIconService.typeScriptPath}/") && f.FullPath.Split('/').Length >= 2 ? string.Join("/", f.FullPath.Split('/').Take(2)) : null;
            string? libraryName = !string.IsNullOrEmpty(libraryRoot) ? libraryRoot.Split('/').Last() : null;
            if (!string.IsNullOrEmpty(libraryName))
            {
                options.Add(new ContextMenuItem
                {
                    Text = "Extract JSObjects",
                    Icon = "file_export",
                    Value = async () =>
                    {
                        await NewProject(f.FullPath, libraryName);
                    }
                });
            }
            options.Add(new ContextMenuItem
            {
                Text = "Remove",
                Icon = "delete",
                Value = async () =>
                {
                    var confirm = await DialogService.Confirm($"Delete {f.Name}?");
                    if (confirm != true) return;
                    await FS.Remove(f.FullPath, true);
                }
            });
            //
            ContextMenuService.Open(args, options, async (res) =>
            {
                ContextMenuService.Close();
                try
                {
                    if (res.Value is Action action)
                    {
                        action();
                    }
                    else if (res.Value is Func<Task> asyncAction)
                    {
                        await asyncAction();
                    }
                }
                catch (Exception ex)
                {
                    JS.Log("Context menu error:", ex.ToString());
                }
            });
        }
        async Task Tree_OnChange(TreeEventArgs args)
        {
            //if (args.Value is ASyncFSEntryInfo fsEntry)
            //{
            //    await OpenEntry(fsEntry);
            //}
        }
        bool ShouldSelectThisNode(object node)
        {
            //if (node is ASyncFSEntryInfo fsEntry)
            //{
            //    JS.Log("shouldselect", fsEntry.FullPath);
            //    if (fsEntry.FullPath.Split('/').Length <= 4)
            //    {
            //        return true;
            //    }
            //}
            //else
            //{
            //    return true;
            //}
            return false;
        }
        bool ShouldExpandThisNode(object node)
        {
            if (node is ASyncFSEntryInfo fsEntry)
            {
                if (fsEntry.FullPath.Split('/').Length <= 2)
                {
                    return true;
                }
            }
            return false;
        }
        async Task TreeItem_OnContextMenu(TreeItemContextMenuEventArgs args)
        {
            if (Busy) return;
            if (args.Value is ASyncFSEntryInfo fsEntry)
            {
                await ContextMenu(args, fsEntry);
            }
        }
        async Task TreeviewItemClick(MouseEventArgs args, RadzenTreeItem radzenTreeItem)
        {
            if (radzenTreeItem.Value is ASyncFSEntryInfo fsEntry)
            {
                await OnEntryClick(args, fsEntry);
            }
        }
        async Task OnExpand(TreeExpandEventArgs args)
        {
            if (args.Value is ASyncFSEntryInfo fsEntry)
            {
                if (fsEntry!.IsDirectory)
                {
                    try
                    {
                        var data = await FS.GetInfos(fsEntry.FullPath);
                        args.Children.Data = data?.OrderByDescending(o => o.IsDirectory).ThenBy(o => o.Name).ToList();
                    }
                    catch { }
                }
                args.Children.TextProperty = nameof(ASyncFSEntryInfo.Name);
                args.Children.HasChildren = (childEntry) =>
                {
                    var ret = (childEntry as ASyncFSEntryInfo)?.IsDirectory == true;
                    return ret;
                };
                args.Children.Selected = ShouldSelectThisNode;
                args.Children.Expanded = ShouldExpandThisNode;
                args.Children.Template = RenderTreeItem;
            }
            else
            {
                var nmt = true;
            }
        }
        async Task Reset()
        {
            try
            {
                var confirm = await DialogService.Confirm("This will reset everything. Are you sure?");
                if (confirm != true) return;
                await FS.Reset();
                NotificationService.Notify(NotificationSeverity.Success, "Reset");
            }
            catch (Exception ex)
            {
                JS.Log(ex.ToString());
                var nmt = true;
            }
        }
        async Task ImportTypeScriptDeclarations()
        {
            try
            {
                await _ImportTypeScriptDeclarations();
            }
            catch (Exception ex)
            {
                JS.Log(ex.ToString());
                var nmt = true;
            }
        }
        async Task _ImportTypeScriptDeclarations()
        {
            File[]? files = null;
            using var pm = await ProgressModalService.NewSession("Importing...");
            try
            {
                files = await FileService.ShowOpenFilePickerCompat(".zip,.rar,.7zip", false);
            }
            catch { }
            if (files != null && files.Any())
            {
                pm.Text = "Looking for TypeScript declaration (*.d.ts) files...";
                await pm.DelayForUIUpdate();
                var file = files[0];
                using var zipArrayBuffer = await file.ArrayBuffer();
                var zipBytes = zipArrayBuffer.ReadBytes();
                using var archive = new ZipArchive(new MemoryStream(zipBytes));

                // find first *.d.ts file
                var typeFiles = archive.Entries.Where(o => o.Name.EndsWith(".d.ts")).ToList();
                typeFiles = typeFiles.OrderBy(o => o.FullName.Split('/').Length).ToList();
                var firstEntry = typeFiles.FirstOrDefault(o => o.Name == "index.d.ts") ?? typeFiles.FirstOrDefault();
                pm.Max = typeFiles.Count;
                var tsDone = 0;
                pm.Value = tsDone;
                if (firstEntry == null)
                {
                    NotificationService.Notify(NotificationSeverity.Error, "No *.d.ts files found");
                    return;
                }

                pm.Text = "";
                var importAs = await DialogService.ShowInputBox("Import as?");
                if (string.IsNullOrWhiteSpace(importAs))
                {
                    NotificationService.Notify(NotificationSeverity.Error, "Cancelled");
                    return;
                }
                importAs = importAs.Trim();

                var dirs = new List<string>();
                //
                var extractBasePath = $"{FileIconService.typeScriptPath}/{importAs}";

                await FS.Remove(extractBasePath, true);
                await FS.CreateDirectory(extractBasePath);

                // extract files starting from the first folder to have a *.d.ts file

                var pathMin = IOPath.GetDirectoryName(firstEntry.FullName);
                if (!string.IsNullOrEmpty(pathMin)) pathMin = pathMin.TrimEnd('/') + "/";

                foreach (ZipArchiveEntry entry in typeFiles)
                {
                    var isDir = entry.FullName.EndsWith("/");
                    var fullName = isDir ? entry.FullName.TrimEnd('/') : entry.FullName;
                    if (!string.IsNullOrEmpty(pathMin))
                    {
                        if (!fullName.StartsWith(pathMin))
                        {
                            continue;
                        }
                        fullName = fullName.Substring(pathMin.Length);
                        var gg = "";
                    }
                    var entryDestinationPath = IOPath.Combine(extractBasePath, fullName);
                    if (isDir)
                    {
                        dirs.Add(entryDestinationPath);
                        Console.WriteLine($"Creating folder: {entryDestinationPath}");
                        await FS.CreateDirectory(entryDestinationPath);
                    }
                    else
                    {
                        if (entry.Name.EndsWith(".d.ts"))
                        {
                            tsDone += 1;
                            pm.Text = entry.Name;
                            pm.Value = tsDone;
                        }
                        var parentPath = IOPath.GetDirectoryName(entryDestinationPath);
                        if (!string.IsNullOrEmpty(parentPath))
                        {
                            if (!dirs.Contains(parentPath))
                            {
                                dirs.Add(parentPath);
                                var parentExists = await FS.DirectoryExists(parentPath);
                                if (!parentExists)
                                {
                                    await FS.CreateDirectory(parentPath);
                                }
                            }
                        }
                        using var entryStream = entry.Open();
                        try
                        {
                            await FS.Write(entryDestinationPath, entryStream);
                        }
                        catch (Exception ex)
                        {
                            // some addon zips contain restricted files (like desktop.ini) that the browser is (apparently) not allowed to write using the File System API
                            // we must ignore them
                            Console.WriteLine($"Writing file failed: {entryDestinationPath}");
                        }
                    }
                }
                NotificationService.Notify(NotificationSeverity.Success, "Import successful");
                StateHasChanged();
            }
        }
        async Task NewProject(string sourcePath, string libraryName)
        {
            var info = new ProjectInfo();
            info.Source = sourcePath;
            info.JSModuleNamespace = libraryName;
            info = await NewProjectDialog.Show(DialogService, info);
            JS.Log("_info", info);
            if (info != null && !string.IsNullOrEmpty(info.Source) && !string.IsNullOrEmpty(info.ProjectName))
            {
                var projectFolder = $"{FileIconService.blazorProjectPath}/{info.ProjectName}";
                await FS.CreateDirectory(projectFolder);

                using var pm = await ProgressModalService.NewSession("Creating project...");
                var parser = new BlazorJSProject(FS, sourcePath, info.ProjectName, info.JSModuleNamespace, info.NameSpaceFromPath);
                var writing = false;
                void Parser_OnProgress()
                {
                    if (parser.TypeScriptDeclarationFileCount > 0)
                    {
                        pm.Text = $"Reading typescript... Classes: {parser.Interfaces.Count} Enums: {parser.Enums.Count} Constants: {parser.Constants.Count}";
                        pm.Max = parser.TypeScriptDeclarationFileCount;
                        pm.Value = parser.Modules.Count;
                    }
                }
                parser.OnProgress += Parser_OnProgress;
                try
                {
                    pm.Text = "Reading typescript...";
                    await parser.ProcessDir();
                    writing = true;
                    pm.Text = "Building bindings...";
                    await parser.WriteProject(projectFolder, (total, done) =>
                    {
                        pm.Text = $"Building bindings... Classes: {parser.Interfaces.Count} Enums: {parser.Enums.Count} Constants: {parser.Constants.Count}";
                        pm.Max = total;
                        pm.Value = done;
                    });
                }
                catch (Exception ex)
                {
                    JS.Log(ex.ToString());
                    var nmt = true;
                }
                parser.OnProgress -= Parser_OnProgress;
            }
            StateHasChanged();
        }
        /// <summary>
        /// Triggers a render (via a StateHasChanged in AppDesktop.razor) and waits for it to complete<br/>
        /// </summary>
        /// <returns></returns>
        async Task RenderTick(int sleep = 0)
        {
            // must start waiting before calling StateHasChanged
            var task = AwaitAfterRender;
            StateHasChanged();
            if (sleep > 0)
            {
                await Task.Delay(sleep);
            }
            else
            {
                await Task.Yield();
            }
            await task;
        }
        TaskCompletionSource? _awaitAfterRender = null;
        /// <summary>
        /// This task will complete the next time OnAfterRender is called on AppDesktop.razor<br/>
        /// </summary>
        Task AwaitAfterRender => (_awaitAfterRender ??= new TaskCompletionSource()).Task;
    }
}
