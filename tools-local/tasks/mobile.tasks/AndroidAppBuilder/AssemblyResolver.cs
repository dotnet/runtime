// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

internal class AssemblyResolver : MetadataAssemblyResolver
{
    private string[] _searchPaths;

    public AssemblyResolver(string[] searchPaths) => _searchPaths = searchPaths;

    public static string[] ResolveDependencies(
        string[] assembliesToResolve, string[] searchPaths, bool ignoreErrors = true)
    {
        var assemblies = new Dictionary<string, Assembly>();
        var mlc = new MetadataLoadContext(new AssemblyResolver(searchPaths), "System.Private.CoreLib");
        foreach (string assemblyPath in assembliesToResolve)
        {
            try
            {
                AddAssembly(mlc, mlc.LoadFromAssemblyPath(assemblyPath), assemblies, ignoreErrors);
            }
            catch (Exception)
            {
                if (!ignoreErrors)
                {
                    throw;
                }
            }
        }
        return assemblies.Values.Select(i => i.Location).Distinct().ToArray();
    }

    private static void AddAssembly(MetadataLoadContext mlc, Assembly assembly, Dictionary<string, Assembly> assemblies, bool ignoreErrors)
    {
        if (assemblies.ContainsKey(assembly.GetName().Name!))
            return;
        assemblies[assembly.GetName().Name!] = assembly;
        foreach (AssemblyName name in assembly.GetReferencedAssemblies())
        {
            try
            {
                Assembly refAssembly = mlc.LoadFromAssemblyName(name);
                AddAssembly(mlc, refAssembly, assemblies, ignoreErrors);
            }
            catch (Exception)
            {
                if (!ignoreErrors)
                {
                    throw;
                }
            }
        }
    }

    public override Assembly? Resolve(MetadataLoadContext context, AssemblyName assemblyName)
    {
        string name = assemblyName.Name!;
        foreach (string dir in _searchPaths)
        {
            string path = Path.Combine(dir, name + ".dll");
            if (File.Exists(path))
                return context.LoadFromAssemblyPath(path);
        }
        return null;
    }
}
