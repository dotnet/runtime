// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;

internal class StartupHook
{
    public static void Initialize()
    {
        Console.WriteLine($"Hello from startup hook in {(typeof(StartupHook).Assembly.GetName().Name)}!");

        bool addResolver = Environment.GetEnvironmentVariable("TEST_STARTUPHOOK_ADD_RESOLVER") == true.ToString();
        if (addResolver)
        {
            AssemblyLoadContext.Default.Resolving += OnResolving;
        }

        bool useDependency = Environment.GetEnvironmentVariable("TEST_STARTUPHOOK_USE_DEPENDENCY") == true.ToString();
        if (useDependency)
        {
            UseDependency();
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void UseDependency()
    {
        Console.WriteLine($"SharedLibrary.Value: {SharedLibrary.SharedType.Value}");
    }

    private static Assembly OnResolving(AssemblyLoadContext context, AssemblyName assemblyName)
    {
        if (assemblyName.Name != "SharedLibrary")
            return null;

        Console.WriteLine($"Resolving {assemblyName.Name} in startup hook");
        string startupHookDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        string sharedLibrary = Path.GetFullPath(Path.Combine(startupHookDirectory, "SharedLibrary.dll"));
        return AssemblyLoadContext.Default.LoadFromAssemblyPath(sharedLibrary);
    }
}
