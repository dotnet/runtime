// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using Xunit;

public class AppPaths
{
    private const string AssemblyToLoad = "AssemblyToLoad";

    [Fact]
    public static void DefaultALC()
    {
        LoadFromStream(AssemblyLoadContext.Default);
    }

    [Fact]
    public static void CustomALC()
    {
        AssemblyLoadContext alc = new("alc");
        LoadFromStream(alc);
    }

    private static void LoadFromStream(AssemblyLoadContext alc)
    {
        // corerun should add the app directory as the APP_PATHS property
        Assert.Equal(AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar), ((string)AppContext.GetData("APP_PATHS")).TrimEnd(Path.DirectorySeparatorChar), StringComparer.OrdinalIgnoreCase);
        string assemblyPath = Path.Combine(AppContext.BaseDirectory, $"{AssemblyToLoad}.dll");
        byte[] assemblyBytes = File.ReadAllBytes(assemblyPath);
        MemoryStream stream = new(assemblyBytes);

        Assembly assembly = alc.LoadFromStream(stream);
        Assert.NotNull(assembly);
        Assert.Equal(AssemblyToLoad, assembly.GetName().Name);
        Assert.True(string.IsNullOrEmpty(assembly.Location),
            $"Assembly should be loaded from stream and have an empty Location. Actual: '{assembly.Location}'");
    }
}
