// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace SuperPmi.Shim;

// The settings that the shims read from the environment, and the real JIT that they load.
internal static partial class ShimEnvironment
{
    private const int MAX_PATH = 260;

    private static readonly string s_defaultRealJitName =
        OperatingSystem.IsWindows() ? "clrjit2.dll" : OperatingSystem.IsMacOS() ? "libclrjit2.dylib" : "libclrjit2.so";

    private static string s_homeDirectory;
    private static string s_defaultRealJitPath;

    public static string RealJitPath { get; private set; }

    public static string LogPath { get; private set; }

    public static IntPtr RealJit { get; private set; }

    private static string GetEnvWithDefault(string name, string defaultValue) => Environment.GetEnvironmentVariable(name) ?? defaultValue;

    public static void SetDefaultPaths()
    {
        s_homeDirectory ??= GetEnvWithDefault("HOME", ".");
        s_defaultRealJitPath ??= s_homeDirectory + Path.DirectorySeparatorChar + s_defaultRealJitName;
    }

    public static void SetLibName() => RealJitPath ??= GetEnvWithDefault("SuperPMIShimPath", s_defaultRealJitPath);

    public static void SetLogPath() => LogPath ??= GetEnvWithDefault("SuperPMIShimLogPath", s_homeDirectory);

    public static bool LoadRealJitLib()
    {
        if (RealJit != 0)
            return true;

        if (string.IsNullOrEmpty(RealJitPath))
        {
            Logger.LogError("LoadRealJitLib - No real jit path");
            return false;
        }

        try
        {
            RealJit = NativeLibrary.Load(RealJitPath);
        }
        catch (Exception e) when (e is DllNotFoundException or BadImageFormatException)
        {
            Logger.LogError($"LoadRealJitLib - {(OperatingSystem.IsWindows() ? "LoadLibrary" : "dlopen")} failed to load '{RealJitPath}' ({e.Message})");
            return false;
        }
        return true;
    }

    public static IntPtr GetRealJitExport(string name) => NativeLibrary.TryGetExport(RealJit, name, out IntPtr address) ? address : 0;

    // A file name in 'folderPath' from 'fileName' (made a legal file name) and a random suffix, for uniqueness.
    public static string GetResultFileName(string folderPath, string fileName, string extension)
    {
        string suffix = RandomNumberGenerator.GetHexString(8, lowercase: true) + extension;

        // Limit the total file name length to MAX_PATH - 50.
        int usableLength = MAX_PATH - 50 - Encoding.UTF8.GetByteCount(folderPath) - suffix.Length;
        if (usableLength < 0)
        {
            Logger.LogError($"GetResultFileName - folder path '{folderPath}' length + minimal file name exceeds limit {MAX_PATH - 50}");
            return "";
        }

        byte[] copy = Encoding.UTF8.GetBytes(fileName);
        if (copy.Length > usableLength)
            Array.Resize(ref copy, usableLength);

        ReplaceIllegalCharacters(copy);
        return folderPath + Path.DirectorySeparatorChar + Encoding.ASCII.GetString(copy) + suffix;
    }

    // Makes the bytes textual ASCII characters, without the illegal or annoying ones for file names.
    private static void ReplaceIllegalCharacters(byte[] fileName)
    {
        for (int i = 0; i < fileName.Length; i++)
        {
            byte ch = fileName[i];
            if (ch <= 32 || ch >= 127 || "()=<>:/\\|?!*.,".Contains((char)ch))
                fileName[i] = (byte)'_';
            else if (ch == '"')
                fileName[i] = (byte)' ';
        }
    }

    // The command line as the process got it: Environment.CommandLine is rebuilt from the arguments.
    [LibraryImport("kernel32.dll")]
    private static partial IntPtr GetCommandLineW();

    public static string GetProcessCommandLine()
    {
        if (OperatingSystem.IsWindows())
            return Marshal.PtrToStringUni(GetCommandLineW());

        try
        {
            // The arguments are separated by NULs, which are converted to spaces.
            return File.ReadAllText("/proc/self/cmdline").TrimEnd('\0').Replace('\0', ' ');
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return "";
        }
    }
}
