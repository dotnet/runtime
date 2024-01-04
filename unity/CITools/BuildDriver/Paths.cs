// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using NiceIO;

namespace BuildDriver;

public class Paths
{
    public static NPath RepoRoot => typeof(Paths).Assembly.Location.ToNPath().RecursiveParents
        .First(dir => dir.Combine(".yamato").DirectoryExists());

    public static NPath UnityRoot => RepoRoot.Combine("unity");
    public static NPath UnityGC => UnityRoot.Combine("unitygc");

    public static NPath UnityEmbedApiTests => UnityRoot.Combine("embed_api_tests");

    public static NPath UnityEmbedHost => UnityRoot.Combine("unity-embed-host");

    public static NPath Artifacts => RepoRoot.Combine("artifacts");

    public static string DotNetExecutableName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "dotnet.exe" : "dotnet";

    public static string BootstrapDotNetExecutableName =>
        RuntimeInformation.IsOSPlatform(OSPlatform.OSX) || RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
            ? "dotnet.sh"
            : "dotnet.cmd";

    public static string FullPlatformNameInPaths
    {
        get
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return "windows";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return "linux";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return "OSX";

            throw new ArgumentException("Unhandled platform");
        }
    }

    public static string ShortPlatformNameInPaths
    {
        get
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return "win";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return "linux";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return "osx";

            throw new ArgumentException("Unhandled platform");
        }
    }

    public static string UnityGCFileName
    {
        get
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return "libunitygc.dylib";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return "libunitygc.so";

            return "unitygc.dll";
        }
    }
}
