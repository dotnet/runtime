// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Runtime.Loader;

/// <summary>
/// Debuggee for cDAC dump tests — exercises the Loader contract.
/// Loads assemblies from multiple AssemblyLoadContexts then crashes.
/// </summary>
internal static class Program
{
    public const int CustomAlcCount = 3;

    private static void Main()
    {
        // Create multiple AssemblyLoadContexts and load the runtime assembly in each
        // to exercise the Loader contract's module enumeration.
        AssemblyLoadContext[] contexts = new AssemblyLoadContext[CustomAlcCount];
        Assembly[] loadedAssemblies = new Assembly[CustomAlcCount];

        for (int i = 0; i < CustomAlcCount; i++)
        {
            contexts[i] = new AssemblyLoadContext($"cdac-test-alc-{i}", isCollectible: true);

            // Each ALC will have the core assembly visible.
            // We can also load the current assembly's location in a new context,
            // but for simplicity, just exercise the ALC infrastructure.
            loadedAssemblies[i] = contexts[i].LoadFromAssemblyName(typeof(object).Assembly.GetName());
        }

        // Also load System.Xml to have another module present
        var xmlAssembly = Assembly.Load("System.Private.Xml");

        // Keep references alive
        GC.KeepAlive(contexts);
        GC.KeepAlive(loadedAssemblies);
        GC.KeepAlive(xmlAssembly);

        Environment.FailFast("cDAC dump test: MultiModule debuggee intentional crash");
    }
}
