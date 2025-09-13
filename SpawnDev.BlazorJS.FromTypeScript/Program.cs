using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Radzen;
using Sdcb.TypeScript;
using Sdcb.TypeScript.TsTypes;
using SpawnDev.BlazorJS;
using SpawnDev.BlazorJS.FromTypeScript;
using SpawnDev.BlazorJS.FromTypeScript.Layout;
using SpawnDev.BlazorJS.FromTypeScript.Layout.AppTray;
using SpawnDev.BlazorJS.FromTypeScript.Parsing;
using SpawnDev.BlazorJS.FromTypeScript.Services;
using SpawnDev.BlazorJS.JSObjects;
using SpawnDev.BlazorJS.Toolbox;
using SpawnDev.MatrixLEDDisplay.Demo.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.Services.AddBlazorJSRuntime(out var JS);

if (JS.IsWindow)
{
    builder.RootComponents.Add<App>("#app");
    builder.RootComponents.Add<HeadOutlet>("head::after");
}

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddScoped<DialogService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<TooltipService>();
builder.Services.AddScoped<ContextMenuService>();
builder.Services.AddScoped<ThemeService>();
builder.Services.AddScoped<AppTrayService>();
builder.Services.AddScoped<MainLayoutService>();
builder.Services.AddScoped<ThemeTrayIconService>();
builder.Services.AddScoped<FileService>();
builder.Services.AddScoped<GitHubService>();
builder.Services.AddScoped<ProjectManager>();
builder.Services.AddScoped<AsyncFileSystem>();
builder.Services.AddScoped<AssetManifestService>();
builder.Services.AddScoped<FileIconService>();
builder.Services.AddScoped<ProgressModalService>(); 

var host = await builder.Build().StartBackgroundServices();

#if DEBUG
//var fileService = host.Services.GetRequiredService<AsyncFileSystem>();

//var text = @"
//declare module ""../../core/Object3D.js"" {
//    interface Object3D {
//        // See https://github.com/mrdoob/three.js/pull/28683
//        count?: number | undefined;
//        // See https://github.com/mrdoob/three.js/pull/26335
//        occlusionTest?: boolean | undefined;
//        // https://github.com/mrdoob/three.js/pull/29386
//        static?: boolean | undefined;
//    }
//}
//";

//var x = new TypeScriptAST(text);
//JS.Log(x.RootNode.GetView());

//var nmtt = true;
//var fs = fileService;

//var dirs = await fs.GetDirectories("");
//var files = await fs.GetFiles("");

//await fs.CreateDirectory("Projects");

//await fs.Write("Projects/settings.json", "Woohoo 2!");

//await fs.Write("settings.json", "Woohoo 1!");

//var rb = await fs.ReadText("settings.json");

//var w = JS.Get<Window>("");
//w.JSRef!.CallVoidAsync()

//var rbinfo1 = await fs.GetInfo("settings.json");
//var rbinfo2 = await fs.GetInfo("settingb.json");
//var rbinfo3 = await fs.GetInfo("");
//var rbinfo4 = await fs.GetInfos("");
//var rbinfo5 = await fs.GetInfos("Projects");
//var iii = await fs.GetInfo("Projects/settings.json");


//var dirs1 = await fs.GetDirectories("");
//var files1 = await fs.GetFiles("");

////await fs.Remove("settings.json");

//var dirs2 = await fs.GetDirectories("");
//var files2 = await fs.GetFiles("");

//var dirs4 = await fs.GetEntries("");

//var dirs3 = await fs.GetDirectories("");
//var files3 = await fs.GetFiles("");
//var nmt = true;

//var gitHubService = host.Services.GetRequiredService<GitHubService>();

//var downloadLink = await gitHubService.GetRepoDefaultBranchDownloadLink("three-types/three-ts-types");

//var typess = await gitHubService.GetEntries("DefinitelyTyped/DefinitelyTyped/types");
//var fc = typess.Count;
//var f1 = typess.First();
//var types2 = await gitHubService.GetEntries("three-types/three-ts-types", 2);
//var fb = types2.Count;
//var f2 = types2.First();
//var files1 = await gitHubService.GetEntries("LostBeard/SpawnDev.BlazorJS/SpawnDev.BlazorJS.Demo");
//var filei1 = await gitHubService.GetEntry("LostBeard/SpawnDev.BlazorJS/README.md1");
//var fileBy = await gitHubService.GetBytes("LostBeard/SpawnDev.BlazorJS/README.md");
//var fileSs = await gitHubService.GetString("LostBeard/SpawnDev.BlazorJS/README.md");
//var fileSt = await gitHubService.GetArrayBuffer("LostBeard/SpawnDev.BlazorJS/README.md");
#endif
await host.BlazorJSRunAsync();

