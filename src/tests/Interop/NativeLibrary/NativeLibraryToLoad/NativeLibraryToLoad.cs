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
        if (OperatingSystem.IsWindows())
            return $"{Name}.dll";

        if (OperatingSystem.IsLinux())
            return $"lib{Name}.so";

        if (OperatingSystem.IsMacOS())
            return $"lib{Name}.dylib";

        throw new PlatformNotSupportedException();
    }

    public static string GetFullPath()
    {
        return Path.Combine(GetDirectory(), GetFileName());
    }

    public static string GetDirectory()
    {
        return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
    }
}
