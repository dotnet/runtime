// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;
using NiceIO;

namespace BuildDriver;

public class BuildNullGC : BuildDriver
{
    public static void Run(GlobalConfig gConfig)
    {
        Console.WriteLine("***********************");
        Console.WriteLine("Unity: Building Null GC");
        Console.WriteLine("***********************");

        NPath workingDir = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ?
            Paths.UnityGC : Paths.UnityGC.CreateDirectory(gConfig.Configuration);

        string extraArchDefine = string.Empty;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) && gConfig.Architecture.Equals("arm64"))
            extraArchDefine = "-DCMAKE_OSX_ARCHITECTURES=arm64 ";
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            extraArchDefine = gConfig.Architecture.Equals("x86") ? "Win32" : "x64";

        ProcessStartInfo sInfo = new();
        sInfo.FileName = "cmake";
        sInfo.WorkingDirectory = workingDir;
        sInfo.Arguments = $". -A {extraArchDefine}";
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            sInfo.Arguments = $"{extraArchDefine}-DCMAKE_BUILD_TYPE={gConfig.Configuration} ..";

        RunProcess(sInfo, gConfig);

        sInfo.Arguments = "--build .";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            sInfo.Arguments = $"--build . --config {gConfig.Configuration}";

        RunProcess(sInfo, gConfig);
    }
}
