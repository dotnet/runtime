// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Android.Build.Ndk;
using Microsoft.Mobile.Build.Clang;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.Android.Build
{
    public sealed class AndroidProject
    {
        private const string DefaultMinApiLevel = "21";
        private const string Cmake = "cmake";

        private TaskLoggingHelper logger;

        private string abi;
        private string androidToolchainPath;
        private string projectName;
        private string targetArchitecture;

        public string Abi => abi;

        public AndroidProject(string projectName, string runtimeIdentifier, TaskLoggingHelper logger) :
            this(projectName, runtimeIdentifier, Environment.GetEnvironmentVariable("ANDROID_NDK_ROOT")!, logger)
        {
        }

        public AndroidProject(string projectName, string runtimeIdentifier, string androidNdkPath, TaskLoggingHelper logger)
        {
            androidToolchainPath = Path.Combine(androidNdkPath, "build", "cmake", "android.toolchain.cmake").Replace('\\', '/');
            abi = DetermineAbi(runtimeIdentifier);
            targetArchitecture = GetTargetArchitecture(runtimeIdentifier);

            this.logger = logger;
            this.projectName = projectName;
        }

        // builds using NDK toolchain
        public void Build(string workingDir, ClangBuildOptions buildOptions, bool stripDebugSymbols = false, string apiLevel = DefaultMinApiLevel)
        {
            NdkTools tools = new NdkTools(targetArchitecture, GetHostOS(), apiLevel);

            string clangArgs = BuildClangArgs(buildOptions);
            Utils.RunProcess(logger, tools.ClangPath, workingDir: workingDir, args: clangArgs);
        }

        public void GenerateCMake(string workingDir, bool stripDebugSymbols)
        {
            GenerateCMake(workingDir, DefaultMinApiLevel, stripDebugSymbols);
        }

        public void GenerateCMake(string workingDir, string apiLevel = DefaultMinApiLevel, bool stripDebugSymbols = false)
        {
            // force ninja generator on Windows, the VS generator causes issues with the built-in Android support in VS
            var generator = Utils.IsWindows() ? "-G Ninja" : "";
            string cmakeGenArgs = $"{generator} -DCMAKE_TOOLCHAIN_FILE={androidToolchainPath} -DANDROID_ABI=\"{Abi}\" -DANDROID_STL=none -DTARGETS_ANDROID=1 " +
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

        private static string BuildClangArgs(ClangBuildOptions buildOptions)
        {
            StringBuilder ret = new StringBuilder();

            foreach (string compilerArg in buildOptions.CompilerArguments)
            {
                ret.Append(compilerArg);
                ret.Append(' ');
            }

            foreach (string includeDir in buildOptions.IncludePaths)
            {
                ret.Append($"-I {includeDir} ");
            }

            foreach (string linkerArg in buildOptions.LinkerArguments)
            {
                ret.Append($"-Xlinker {linkerArg} ");
            }

            foreach (string source in buildOptions.Sources)
            {
                ret.Append(source);
                ret.Append(' ');
            }

            HashSet<string> libDirs = new HashSet<string>();
            foreach (string lib in buildOptions.NativeLibraryPaths)
            {
                string rootPath = Path.GetDirectoryName(lib)!;
                string libName = Path.GetFileName(lib);

                if (libDirs.Add(rootPath))
                {
                    ret.Append($"-L {rootPath} ");
                }
                ret.Append($"-l:{libName} ");
            }

            return ret.ToString();
        }

        private static string GetTargetArchitecture(string runtimeIdentifier)
        {
            int pos = runtimeIdentifier.IndexOf('-');
            return (pos > -1) ? runtimeIdentifier.Substring(pos + 1) : "";
        }

        private static string GetHostOS()
        {
            if (Utils.IsWindows())
                return "windows";
            else if (Utils.IsMacOS())
                return "osx";
            else
                return "linux";
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
}
