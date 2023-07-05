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
        return GetLibraryFileName(Name);
    }

    public static string GetLibraryFileName(string name)
    {
        if (OperatingSystem.IsWindows())
            return $"{name}.dll";

        if (OperatingSystem.IsLinux())
            return $"lib{name}.so";

        if (OperatingSystem.IsMacOS())
            return $"lib{name}.dylib";

        throw new PlatformNotSupportedException();
    }

    public static string GetFullPath()
    {
        return Path.Combine(GetDirectory(), GetFileName());
    }

    public static string GetDirectory()
    {
        string directory;
        if (TestLibrary.Utilities.IsNativeAot)
        {
            // NativeAOT test is put in a native/ subdirectory, so we want the parent
            // directory that contains the native library to load
            directory = new DirectoryInfo(AppContext.BaseDirectory).Parent.FullName;
        }
        else
        {
            directory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        }

        return directory;
    }
}
