// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

public class NativeLibraryToLoad
{
    public const string Name = "NativeLibrary";
    public const string InvalidName = "DoesNotExist";

    public static string GetFileName()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return $"{Name}.dll";

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return $"lib{Name}.so";

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return $"lib{Name}.dylib";

        throw new PlatformNotSupportedException();
    }

    public static string GetFullPath()
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        string directory = Path.GetDirectoryName(assembly.Location);
        return Path.Combine(directory, GetFileName());
    }
}
