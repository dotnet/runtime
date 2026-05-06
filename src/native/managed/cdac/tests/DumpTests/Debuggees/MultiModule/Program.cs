// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Runtime.Loader;

/// <summary>
/// Debuggee for cDAC dump tests — exercises the Loader contract.
/// Loads assemblies from multiple AssemblyLoadContexts then crashes.
/// Also includes metadata features for MetaDataImport dump tests:
/// - Non-const fields (for ELEMENT_TYPE_VOID default value testing)
/// - String literals (user strings in #US heap)
/// </summary>
internal static class Program
{
    public const int CustomAlcCount = 3;

    // Non-const field — used by MetaDataImport dump tests to verify
    // ELEMENT_TYPE_VOID is returned for fields without default constants.
    private static int s_nonConstField;

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

        // Use the non-const field so it doesn't get optimized away
        s_nonConstField = contexts.Length;

        // Use a string literal to ensure the #US heap has a user string entry
        // (MetaDataImport dump tests verify GetUserString char count semantics)
        string userString = "cDAC dump test marker string";

        // Keep references alive
        GC.KeepAlive(contexts);
        GC.KeepAlive(loadedAssemblies);
        GC.KeepAlive(xmlAssembly);
        GC.KeepAlive(userString);
        GC.KeepAlive(s_nonConstField);

        Environment.FailFast("cDAC dump test: MultiModule debuggee intentional crash");
    }
}
