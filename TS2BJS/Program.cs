// See https://aka.ms/new-console-template for more information
using SpawnDev.BlazorJS.FromTypeScript.Parsing;
using SpawnDev.BlazorJS.FromTypeScript.Services;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.RegularExpressions;

var srcPath = @"C:\Users\TJ\Downloads\three-ts-types-master\three-ts-types-master\types\three";

var FS1 = new AsyncFSNative(srcPath);

//var FS2 = new AsyncFSNative(destPath);
//var dirs1 = await FS1.GetDirectories("");
//var files = await FS1.GetFiles("");
//var entries = await FS1.GetInfos("", true);
//var adirs1 = await FS1.GetDirectories("animation");
//var afiles = await FS1.GetFiles("animation");
//var aentries = await FS1.GetInfos("animation");
////await FS2.Remove("fruit.txt");
////await FS2.Append("fruit.txt", Encoding.UTF8.GetBytes("apples\r\n"));
////await FS2.Append("fruit.txt", Encoding.UTF8.GetBytes("grapes\r\n"));
//var bb = await FS2.ReadText("fruit.txt");
//var nmtt = true;

var proj = new BlazorJSProject(FS1, "src", "THREE", "THREE", false);
await proj.ProcessDir();

var replacements = new Dictionary<string, string> {
    {
        "public default Default { get => JSRef!.Get<default>(\"default\"); set => JSRef!.Set(\"default\", value); }",
        "public TValue Default { get => JSRef!.Get<TValue>(\"default\"); set => JSRef!.Set(\"default\", value); }"
    },
    {
        "public  RenderBundle { get => JSRef!.Get<>(\"RenderBundle\"); set => JSRef!.Set(\"RenderBundle\", value); }",
        "public JSObject RenderBundle { get => JSRef!.Get<JSObject>(\"RenderBundle\"); set => JSRef!.Set(\"RenderBundle\", value); }"
    },
    {
        "public  ChainMaps { get => JSRef!.Get<>(\"chainMaps\"); set => JSRef!.Set(\"chainMaps\", value); }",
        "public JSObject ChainMaps { get => JSRef!.Get<JSObject>(\"chainMaps\"); set => JSRef!.Set(\"chainMaps\", value); }"
    },
    {
        "public  TypedArray { get => JSRef!.Get<>(\"TypedArray\"); set => JSRef!.Set(\"TypedArray\", value); }",
        "public TypedArray TypedArray { get => JSRef!.Get<TypedArray>(\"TypedArray\"); set => JSRef!.Set(\"TypedArray\", value); }"
    },
    {
        "public  RenderList { get => JSRef!.Get<>(\"RenderList\"); set => JSRef!.Set(\"RenderList\", value); }",
        "public JSObject RenderList { get => JSRef!.Get<JSObject>(\"RenderList\"); set => JSRef!.Set(\"RenderList\", value); }"
    }
};

proj.PostProcessBeforeWrite = (string fileName, ref string code) =>
{
    code = code.Replace("EventListener<object, T, EventDispatcher>", "Callback");
    code = code.Replace("EventDispatcher<TEventMap>", "EventDispatcher");
    code = code.Replace("EventDispatcher<object>", "EventDispatcher");
    code = code.Replace(", TEventMap>", ">");
    code = code.Replace("<TEventMap>", "");
    code = code.Replace("const object ", "const string ");
    code = Regex.Replace(code, @"EventDispatcher<.+?>", "EventDispatcher");
    code = Regex.Replace(code, @" : Partial<.+?$", "EventDispatcher", RegexOptions.Multiline);
    foreach(var kvp in replacements)
    {
        code = code.Replace(kvp.Key, kvp.Value);
    }
};

// : Partial<MapColorPropertiesToColorRepresentations<MeshDistanceMaterialProperties>>

await proj.WriteProject("_three_alt", (total, done) => Console.WriteLine($"{done} / {total}"));
var gg = TypeScriptASTExtensions.GetTypeStringNodeTypes.OrderBy(o => o).ToList();
//await proj.WriteViewsCondensed("_views", (total, done) => Console.WriteLine($"{done} / {total}"));

await FS1.CreateDirectory("_views2");
await proj.WriteSignatures("_views2", (total, done) => Console.WriteLine($"{done} / {total}"));

Console.WriteLine("Done");

















