// -*- indent-tabs-mode: nil -*-
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Reflection;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

public class WasmAppBuilder : Task
{
    // FIXME: Document

    [Required]
    public string? AppDir { get; set; }
    [Required]
    public string? MicrosoftNetCoreAppRuntimePackDir { get; set; }
    [Required]
    public string? MainAssembly { get; set; }
    [Required]
    public string? MainJS { get; set; }
    [Required]
    public ITaskItem[]? AssemblySearchPaths { get; set; }
    public ITaskItem[]? ExtraAssemblies { get; set; }
    public ITaskItem[]? FilesToIncludeInFileSystem { get; set; }
    public ITaskItem[]? RemoteSources { get; set; }

    Dictionary<string, Assembly>? _assemblies;
    Resolver? _resolver;

    public override bool Execute ()
    {
        if (!File.Exists(MainAssembly))
            throw new ArgumentException($"File MainAssembly='{MainAssembly}' doesn't exist.");
        if (!File.Exists(MainJS))
            throw new ArgumentException($"File MainJS='{MainJS}' doesn't exist.");

        var paths = new List<string>();
        _assemblies = new Dictionary<string, Assembly>();

        // Collect and load assemblies used by the app
        foreach (var v in AssemblySearchPaths!)
        {
            var dir = v.ItemSpec;
            if (!Directory.Exists(dir))
                throw new ArgumentException($"Directory '{dir}' doesn't exist or not a directory.");
            paths.Add(dir);
        }
        _resolver = new Resolver(paths);
        var mlc = new MetadataLoadContext(_resolver, "System.Private.CoreLib");

        var mainAssembly = mlc.LoadFromAssemblyPath (MainAssembly);
        Add(mlc, mainAssembly);

        if (ExtraAssemblies != null)
        {
            foreach (var item in ExtraAssemblies)
            {
                var refAssembly = mlc.LoadFromAssemblyPath(item.ItemSpec);
                Add(mlc, refAssembly);
            }
        }

        // Create app
        Directory.CreateDirectory(AppDir!);
        Directory.CreateDirectory(Path.Join(AppDir, "managed"));
        foreach (var assembly in _assemblies!.Values)
            File.Copy(assembly.Location, Path.Join(AppDir, "managed", Path.GetFileName(assembly.Location)), true);
        foreach (var f in new string[] { "dotnet.wasm", "dotnet.js", "dotnet.timezones.blat", "icudt.dat" })
            File.Copy(Path.Join (MicrosoftNetCoreAppRuntimePackDir, "native", f), Path.Join(AppDir, f), true);
        File.Copy(MainJS!, Path.Join(AppDir, "runtime.js"),  true);

        using (var sw = File.CreateText(Path.Join(AppDir, "mono-config.js")))
        {
            sw.WriteLine("config = {");
            sw.WriteLine("\tassembly_root: \"managed\",");
            sw.WriteLine("\tenable_debugging: 0,");
            sw.WriteLine("\tassets: [");

            foreach (var assembly in _assemblies.Values)
                sw.WriteLine($"\t\t{{ behavior: \"assembly\", name: \"{Path.GetFileName(assembly.Location)}\" }},");

            if (FilesToIncludeInFileSystem != null)
            {
                string supportFilesDir = Path.Join(AppDir, "supportFiles");
                Directory.CreateDirectory(supportFilesDir);

                var i = 0;
                foreach (var item in FilesToIncludeInFileSystem)
                {
                    string? targetPath = item.GetMetadata("TargetPath");
                    if (string.IsNullOrEmpty(targetPath))
                    {
                        targetPath = Path.GetFileName(item.ItemSpec);
                    }

                    // We normalize paths from `\` to `/` as MSBuild items could use `\`.
                    targetPath = targetPath.Replace('\\', '/');

                    var generatedFileName = $"{i++}_{Path.GetFileName(item.ItemSpec)}";

                    File.Copy(item.ItemSpec, Path.Join(supportFilesDir, generatedFileName), true);

                    var actualItemName = "supportFiles/" + generatedFileName;

                    sw.WriteLine("\t\t{");
                    sw.WriteLine("\t\t\tbehavior: \"vfs\",");
                    sw.WriteLine($"\t\t\tname: \"{actualItemName}\",");
                    sw.WriteLine($"\t\t\tvirtual_path: \"{targetPath}\",");
                    sw.WriteLine("\t\t},");
                }
            }

            var enableRemote = (RemoteSources != null) && (RemoteSources!.Length > 0);
            var sEnableRemote = enableRemote ? "true" : "false";

            sw.WriteLine($"\t\t{{ behavior: \"icu\", name: \"icudt.dat\", load_remote: {sEnableRemote} }},");
            sw.WriteLine($"\t\t{{ behavior: \"vfs\", name: \"dotnet.timezones.blat\", virtual_path: \"/usr/share/zoneinfo/\" }}");
            sw.WriteLine ("\t],");

            if (enableRemote) {
                sw.WriteLine("\tremote_sources: [");
                foreach (var source in RemoteSources!)
                    sw.WriteLine("\t\t\"" + source.ItemSpec + "\", ");
                sw.WriteLine ("\t],");
            }

            sw.WriteLine ("};");
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
