// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.Mobile.Build.Clang;

namespace Microsoft.Apple.Build
{
    public sealed class AppleProject
    {
        private string defaultMinOSVersion;

        private TaskLoggingHelper logger;

        private string projectName;
        private string sdkRoot;
        private string targetAbi;
        private string targetArchitecture;
        private string targetOS;

        public AppleProject(string projectName, string runtimeIdentifier, TaskLoggingHelper logger)
        {
            GetTargets(runtimeIdentifier, out targetOS, out targetArchitecture);

            defaultMinOSVersion = (targetOS == "maccatalyst") ? "15.0" : "12.2";
            targetAbi = DetermineAbi(targetArchitecture);

            AppleSdk sdk = new AppleSdk(targetOS, logger);
            sdkRoot = sdk.SdkRoot;

            this.logger = logger;
            this.projectName = projectName;
        }

        public string SdkRoot
        {
            get => sdkRoot;
            set
            {
                sdkRoot = value;
            }
        }

        public void Build(string workingDir, ClangBuildOptions buildOptions, bool stripDebugSymbols = false)
        {
            Build(workingDir, buildOptions, defaultMinOSVersion, stripDebugSymbols);
        }

        public void Build(string workingDir, ClangBuildOptions buildOptions, string minOSVersion, bool stripDebugSymbols = false)
        {
            string clangArgs = BuildClangArgs(buildOptions, minOSVersion);
            Utils.RunProcess(logger, "xcrun", workingDir: workingDir, args: clangArgs);
        }

        private string BuildClangArgs(ClangBuildOptions buildOptions, string minOSVersion)
        {
            StringBuilder ret = new StringBuilder();

            ret.Append("clang ");

            if (targetOS == "maccatalyst")
            {
                string frameworkPath = Path.Combine(SdkRoot, "System", "iOSSupport", "System", "Library", "Frameworks");
                string iosLibPath = Path.Combine(SdkRoot, "System", "iOSSupport", "usr", "lib");

                buildOptions.CompilerArguments.Add($"-target {targetAbi}-apple-ios{minOSVersion}-macabi");
                buildOptions.CompilerArguments.Add($"-isysroot {SdkRoot}");
                buildOptions.CompilerArguments.Add($"-iframework {frameworkPath}");

                ret.Append($"-L {iosLibPath}");
                ret.Append(' ');
            }
            else
            {
                buildOptions.CompilerArguments.Add($"-m{targetOS}-version-min={minOSVersion}");
                buildOptions.CompilerArguments.Add($"-isysroot {sdkRoot}");
                buildOptions.CompilerArguments.Add($"-arch {targetAbi}");
            }

            foreach(string compilerArg in buildOptions.CompilerArguments)
            {
                ret.Append(compilerArg);
                ret.Append(' ');
            }

            foreach(string includeDir in buildOptions.IncludePaths)
            {
                ret.Append($"-I {includeDir} ");
            }

            foreach(string linkerArg in buildOptions.LinkerArguments)
            {
                ret.Append($"{linkerArg} ");
            }

            foreach(string source in buildOptions.Sources)
            {
                string ext = Path.GetExtension(source);

                if (ext == ".a")
                {
                    ret.Append($"-force_load {source}");
                }
                else
                {
                    ret.Append(source);
                }

                ret.Append(' ');
            }

            HashSet<string> libDirs = new HashSet<string>();
            foreach(string lib in buildOptions.NativeLibraryPaths)
            {
                string rootPath = Path.GetDirectoryName(lib)!;
                string libName = Path.GetFileName(lib);
                string ext = Path.GetExtension(lib);

                if (ext == ".a")
                {
                    ret.Append($"-force_load {lib}");
                    ret.Append(' ');
                }
                else
                {
                    if (libDirs.Add(rootPath))
                    {
                        ret.Append($"-L {rootPath} ");
                    }
                    ret.Append($"-l{libName} ");
                }
            }

            return ret.ToString();
        }

        private static void GetTargets(string runtimeIdentifier, out string os, out string arch)
        {
            int pos = runtimeIdentifier.IndexOf('-');
            os = (pos > -1) ? DetermineTargetOS(runtimeIdentifier.Substring(0, pos)) : "";
            arch = (pos > -1) ? runtimeIdentifier.Substring(pos + 1) : "";
        }

        private static string DetermineAbi(string arch) =>
            arch switch
            {
                "arm64" => "arm64",
                "x64" => "x86_64",
                _ => throw new ArgumentException($"{arch} is not supported"),
            };

        private static string DetermineTargetOS(string os) =>
            os switch
            {
                "ios" => "ios",
                "iossimulator" => "iphonesimulator",
                "tvos" => "tvos",
                "tvossimulator" => "tvos-simulator",
                "maccatalyst" => "maccatalyst",
                _ => throw new ArgumentException($"{os} is not supported")
            };
    }
}
