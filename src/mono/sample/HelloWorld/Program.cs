// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Reflection;

if (args.Length != 1)
{
    Console.WriteLine("Usage: generic-runner <directory>");
    return 1;
}

string directory = args[0];

if (!Directory.Exists(directory))
{
    Console.WriteLine("Directory does not exist: {0}", directory);
    return 1;
}


foreach (var subdir in Directory.GetDirectories(directory))
{
    // FIXME Mono: updates don't apply in custom ALCs
    //var alc = new TestALC(subdir);
    var alc = System.Runtime.Loader.AssemblyLoadContext.Default;

    var assemblies = Directory.GetFiles(subdir, "*.dll");
    foreach (var assembly in assemblies)
    {
        var asm = alc.LoadFromAssemblyPath(assembly);
        var type = asm.GetType("HotReloadTest", throwOnError: false);
        if (type == null)
            continue;
        var mi = type.GetMethod("Run", BindingFlags.Static | BindingFlags.Public);
        if (mi == null)
            continue;
        Console.WriteLine($"Running {asm.GetName().Name}");
        var passed = false;
        try
        {
            mi.Invoke(null, null);
            passed = true;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
        if (passed)
            Console.WriteLine($"Passed {asm.GetName().Name}");
    }
}

return 0;

class TestALC : System.Runtime.Loader.AssemblyLoadContext
{
    System.Runtime.Loader.AssemblyDependencyResolver _resolver;
    public TestALC(string basedir) : base(false)
    {
        _resolver = new System.Runtime.Loader.AssemblyDependencyResolver(basedir);

    }
    protected override Assembly? Load(AssemblyName assemblyName)
    {
        string? assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
        if (assemblyPath != null)
        {
            return LoadFromAssemblyPath(assemblyPath);
        }

        return null;
    }
}
