using SpawnDev.BlazorJS.JSObjects;

namespace SpawnDev.BlazorJS.FromTypeScript.Services
{
    public class FileService : IAsyncBackgroundService
    {
        public Task Ready => _Ready ??= InitAsync();
        Task? _Ready;
        HttpClient HttpClient;
        BlazorJSRuntime JS;
        public StorageManager Storage { get; private set; }
        public FileSystemDirectoryHandle FS { get; private set; }
        IDBDatabase? idb = null;
        string FileSystemHandleDBName = "FileServiceHandles";
        string FileSystemHandleStoreName = "default";
        long dbVersion = 1;
        public FileService(HttpClient httpClient, BlazorJSRuntime js)
        {
            HttpClient = httpClient;
            JS = js;
        }
        async Task InitAsync()
        {
            using var navigator = JS.Get<Navigator>("navigator");
            Storage = navigator.Storage;
            FS = await Storage.GetDirectory();
            // init IndexedDB storage for storing FileSystemDirectoryHandles that point to external files and folders.
            if (IDBDatabase.IsSupported)
            {
                try
                {
                    idb = await IDBDatabase.OpenAsync(FileSystemHandleDBName, dbVersion, Db_OnUpgradeNeeded);
                }
                catch (Exception ex)
                {
                    JS.Log("FileService.InitAsync error: idb init failed.", ex.ToString());
                }
            }
        }
        /// <summary>
        /// Most compatible file picker
        /// </summary>
        /// <param name="accept"></param>
        /// <param name="multiple"></param>
        /// <returns></returns>
        public Task<JSObjects.File[]?> ShowOpenFilePickerCompat(string? accept = null, bool? multiple = null)
        {
            return Toolbox.FilePicker.ShowOpenFilePicker(accept, multiple);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        public async Task<FileSystemDirectoryHandle> ShowOpenFolderDialog(ShowDirectoryPickerOptions? options = null)
        {
            using var window = JS.Get<Window>("window");
            return await window.ShowDirectoryPicker(options);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        public async Task<Array<FileSystemFileHandle>> ShowOpenFilePicker(ShowOpenFilePickerOptions? options = null)
        {
            using var window = JS.Get<Window>("window");
            return await window.ShowOpenFilePicker(options);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        public async Task<FileSystemFileHandle> ShowSaveFilePicker(ShowSaveFilePickerOptions? options = null)
        {
            using var window = JS.Get<Window>("window");
            return await window.ShowSaveFilePicker(options);
        }
        private void Db_OnUpgradeNeeded(IDBVersionChangeEvent evt)
        {
            try
            {
                var oldVersion = evt.OldVersion;
                var newVersion = evt.NewVersion;
                using var request = evt.Target;
                using var db = request.Result;
                var names = db.ObjectStoreNames.ToArray();
                var stores = db.ObjectStoreNames;
                if (!stores.Contains(FileSystemHandleStoreName))
                {
                    db.CreateObjectStore<string, FileSystemHandle>(FileSystemHandleStoreName);
                }
            }
            catch (Exception ex)
            {
                var nmt = true;
            }
        }
        public async Task<FileSystemHandle?> GetHandle(string key)
        {
            if (idb == null) return null;
            using var tx = idb.Transaction(FileSystemHandleStoreName, false);
            using var store = tx.ObjectStore<string, FileSystemHandle>(FileSystemHandleStoreName);
            return await store.GetAsync(key);
        }
        public async Task<FileSystemFileHandle?> GetFileSystemFileHandle(string key)
        {
            if (idb == null) return null;
            using var tx = idb.Transaction(FileSystemHandleStoreName, false);
            using var store = tx.ObjectStore<string, FileSystemHandle>(FileSystemHandleStoreName);
            var handle = await store.GetAsync(key);
            return handle?.ToFileSystemFileHandle(true);
        }
        public async Task<FileSystemDirectoryHandle?> GetFileSystemDirectoryHandle(string key)
        {
            if (idb == null) return null;
            using var tx = idb.Transaction(FileSystemHandleStoreName, false);
            using var store = tx.ObjectStore<string, FileSystemHandle>(FileSystemHandleStoreName);
            var handle = await store.GetAsync(key);
            return handle?.ToFileSystemDirectoryHandle(true);
        }
        public async Task SetHandle(string key, FileSystemHandle fsHandle)
        {
            if (idb == null) return;
            using var tx = idb.Transaction(FileSystemHandleStoreName, true);
            using var store = tx.ObjectStore<string, FileSystemHandle>(FileSystemHandleStoreName);
            await store.PutAsync(fsHandle, key);
        }
        public async Task RemoveHandle(string key)
        {
            if (idb == null) return;
            using var tx = idb.Transaction(FileSystemHandleStoreName, true);
            using var store = tx.ObjectStore<string, FileSystemHandle>(FileSystemHandleStoreName);
            await store.DeleteAsync(key);
        }
    }
}
