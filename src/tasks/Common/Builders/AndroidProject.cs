// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

internal sealed class AndroidProject
{
    private const string DefaultMinApiLevel = "21";
    private const string Cmake = "cmake";

    private TaskLoggingHelper logger;

    private string abi;
    private string androidToolchainPath;
    private string projectName;

    public string Abi => abi;

    // set the project name to something generic.
    // return the output path
    // let the builder figure out the name + extension

    public AndroidProject(string projectName, string runtimeIdentifier, TaskLoggingHelper logger) :
        this(projectName, runtimeIdentifier, Environment.GetEnvironmentVariable("ANDROID_NDK_ROOT")!, logger)
    {
    }

    public AndroidProject(string projectName, string runtimeIdentifier, string androidNdkPath, TaskLoggingHelper logger)
    {
        androidToolchainPath = Path.Combine(androidNdkPath, "build", "cmake", "android.toolchain.cmake");
        abi = DetermineAbi(runtimeIdentifier);

        this.logger = logger;
        this.projectName = projectName;
    }

    public void GenerateCMake(string workingDir, bool stripDebugSymbols)
    {
        GenerateCMake(workingDir, DefaultMinApiLevel, stripDebugSymbols);
    }

    public void GenerateCMake(string workingDir, string apiLevel = DefaultMinApiLevel, bool stripDebugSymbols = false)
    {
        string cmakeGenArgs = $"-DCMAKE_TOOLCHAIN_FILE={androidToolchainPath} -DANDROID_ABI=\"{Abi}\" -DANDROID_STL=none -DTARGETS_ANDROID=1 " +
            $"-DANDROID_PLATFORM=android-{apiLevel} -B {projectName}";

        if (stripDebugSymbols)
        {
            // Use "-s" to strip debug symbols, it complains it's unused but it works
            cmakeGenArgs += " -DCMAKE_BUILD_TYPE=MinSizeRel -DCMAKE_C_FLAGS=\"-s -Wno-unused-command-line-argument\"";
        }
        else
        {
            cmakeGenArgs += " -DCMAKE_BUILD_TYPE=Debug";
        }

        Utils.RunProcess(logger, Cmake, workingDir: workingDir, args: cmakeGenArgs);
    }

    public string BuildCMake(string workingDir, bool stripDebugSymbols = false)
    {
        string cmakeBuildArgs = $"--build {projectName}";

        if (stripDebugSymbols)
        {
            cmakeBuildArgs += " --config MinSizeRel";
        }
        else
        {
            cmakeBuildArgs += " --config Debug";
        }

        Utils.RunProcess(logger, Cmake, workingDir: workingDir, args: cmakeBuildArgs);

        return Path.Combine(workingDir, projectName);
    }

    private static string DetermineAbi(string runtimeIdentifier) =>
        runtimeIdentifier switch
        {
            "android-x86" => "x86",
            "android-x64" => "x86_64",
            "android-arm" => "armeabi-v7a",
            "android-arm64" => "arm64-v8a",
            _ => throw new ArgumentException($"{runtimeIdentifier} is not supported for Android"),
        };
}
