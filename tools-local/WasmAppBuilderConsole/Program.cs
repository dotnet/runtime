// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Reflection;

public class WasmAppBuilder
{
    // FIXME: Document
    private  string AppDir;
    public string MicrosoftNetCoreAppRuntimePackDir;
    private string MainAssembly;
    private string MainJS;
    private string[] AssemblySearchPaths;
    private int DebugLevel;
    private bool InvariantGlobalization;

    public WasmAppBuilder(string appDir, string microsoftNetCoreAppRuntimePackDir, string mainAssembly, string mainJS, string[] assemblySearchPaths)
    {
       this.AppDir = appDir;
       this.MicrosoftNetCoreAppRuntimePackDir=microsoftNetCoreAppRuntimePackDir;
       this.MainAssembly = mainAssembly;
       this.MainJS = mainJS;
       this.AssemblySearchPaths = assemblySearchPaths;
    }

    SortedDictionary<string, Assembly>? _assemblies;
    Resolver? _resolver;

    private class WasmAppConfig
    {
        [JsonPropertyName("assembly_root")]
        public string AssemblyRoot { get; set; } = "managed";
        [JsonPropertyName("debug_level")]
        public int DebugLevel { get; set; } = 0;
        [JsonPropertyName("assets")]
        public List<object> Assets { get; } = new List<object>();
        [JsonPropertyName("remote_sources")]
        public List<string> RemoteSources { get; set; } = new List<string>();
    }

    private class AssetEntry {
        protected AssetEntry (string name, string behavior)
        {
            Name = name;
            Behavior = behavior;
        }
        [JsonPropertyName("behavior")]
        public string Behavior { get; init; }
        [JsonPropertyName("name")]
        public string Name { get; init; }
    }

    private class AssemblyEntry : AssetEntry
    {
        public AssemblyEntry(string name) : base(name, "assembly") {}
    }

    private class VfsEntry : AssetEntry {
        public VfsEntry(string name) : base(name, "vfs") {}
        [JsonPropertyName("virtual_path")]
        public string? VirtualPath { get; set; }
    }

    private class IcuData : AssetEntry {
        public IcuData(string name = "icudt.dat") : base(name, "icu") {}
        [JsonPropertyName("load_remote")]
        public bool LoadRemote { get; set; }
    }

    public static void Main(string[] argv)
    {
        Dictionary <string, string> parsedArgs = parseArguments(argv);

        Console.WriteLine("---Arguments---");
        foreach (KeyValuePair<string, string> argValue in parsedArgs)
        {
            Console.WriteLine($"{argValue.Key}: {argValue.Value} ");
        }


        string[] assemblySearchPaths = parsedArgs["assemblysearchpaths"].Split(";");


        new WasmAppBuilder(parsedArgs["appdir"],
                           parsedArgs["microsoftnetcoreappruntimepackdir"],
                           parsedArgs["mainassembly"],
                           parsedArgs["mainjs"],
                           assemblySearchPaths).Execute();  
    }

    private static Dictionary<string, string> parseArguments(string[] arguments)
    {
        Dictionary<string, string> argumentMap = new Dictionary<string, string>(); 

        foreach (string arg in arguments)
        {
            if (arg.Contains("="))
            {
                string[] keyValue = arg.Split("=");
                argumentMap.Add(keyValue[0], keyValue[1]);
            }
        }
        
        return argumentMap;
    }

    public bool Execute ()
    {
        if (!File.Exists(MainAssembly))
            throw new ArgumentException($"File MainAssembly='{MainAssembly}' doesn't exist.");
        if (!File.Exists(MainJS))
            throw new ArgumentException($"File MainJS='{MainJS}' doesn't exist.");

        var paths = new List<string>();
        _assemblies = new SortedDictionary<string, Assembly>();

        // Collect and load assemblies used by the app
        foreach (string dir in AssemblySearchPaths)
        {
            if (!Directory.Exists(dir))
                throw new ArgumentException($"Directory '{dir}' doesn't exist or not a directory.");
            paths.Add(dir);
        }
        _resolver = new Resolver(paths);
        var mlc = new MetadataLoadContext(_resolver, "System.Private.CoreLib");

        var mainAssembly = mlc.LoadFromAssemblyPath(MainAssembly);
        Add(mlc, mainAssembly);

        var config = new WasmAppConfig ();

        // Create app
        Directory.CreateDirectory(AppDir!);
        Directory.CreateDirectory(Path.Join(AppDir, config.AssemblyRoot));
        foreach (var assembly in _assemblies!.Values) {
            File.Copy(assembly.Location, Path.Join(AppDir, config.AssemblyRoot, Path.GetFileName(assembly.Location)), true);
            if (DebugLevel > 0) {
                var pdb = assembly.Location;
                pdb = Path.ChangeExtension(pdb, ".pdb");
                if (File.Exists(pdb))
                    File.Copy(pdb, Path.Join(AppDir, config.AssemblyRoot, Path.GetFileName(pdb)), true);
            }
        }

        List<string> nativeAssets = new List<string>() { "dotnet.wasm", "dotnet.js", "dotnet.timezones.blat" };

        if (!InvariantGlobalization)
        {
            nativeAssets.Add("icudt.dat");
        }

        foreach (var f in nativeAssets)
            File.Copy(Path.Join (MicrosoftNetCoreAppRuntimePackDir, "native", f), Path.Join(AppDir, f), true);
        File.Copy(MainJS!, Path.Join(AppDir, "runtime.js"),  true);

        foreach (var assembly in _assemblies.Values) {
            config.Assets.Add(new AssemblyEntry (Path.GetFileName(assembly.Location)));
            if (DebugLevel > 0) {
                var pdb = assembly.Location;
                pdb = Path.ChangeExtension(pdb, ".pdb");
                if (File.Exists(pdb))
                    config.Assets.Add(new AssemblyEntry (Path.GetFileName(pdb)));
            }
        }

        config.DebugLevel = DebugLevel;

        using (var sw = File.CreateText(Path.Join(AppDir, "mono-config.js")))
        {
            var json = JsonSerializer.Serialize (config, new JsonSerializerOptions { WriteIndented = true });
            sw.Write($"config = {json};");
        }

        using (var sw = File.CreateText(Path.Join(AppDir, "run-v8.sh")))
        {
            sw.WriteLine("v8 --expose_wasm runtime.js -- --run " + Path.GetFileName(MainAssembly) + " $*");
        }

        return true;
    }

    private void Add(MetadataLoadContext mlc, Assembly assembly)
    {
        if (_assemblies!.ContainsKey(assembly.GetName().Name!))
            return;
        _assemblies![assembly.GetName().Name!] = assembly;
        foreach (var aname in assembly.GetReferencedAssemblies())
        {
            try
            {
                Assembly refAssembly = mlc.LoadFromAssemblyName(aname);
                Add(mlc, refAssembly);
            }
            catch (FileNotFoundException)
            {
            }
        }
    }
}

class Resolver : MetadataAssemblyResolver
{
    List<String> _searchPaths;

    public Resolver(List<string> searchPaths)
    {
        _searchPaths = searchPaths;
    }

    public override Assembly? Resolve(MetadataLoadContext context, AssemblyName assemblyName)
    {
        var name = assemblyName.Name;
        foreach (var dir in _searchPaths)
        {
            var path = Path.Combine(dir, name + ".dll");
            if (File.Exists(path))
            {
                return context.LoadFromAssemblyPath(path);
            }
        }
        return null;
    }
}
