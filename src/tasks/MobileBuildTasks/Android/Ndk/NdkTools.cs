// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.Android.Build.Ndk
{
    public sealed class NdkTools
    {
        private string toolRootPath;
        private string toolPrefixPath;
        private string asPrefixPath;

        private string clangPath;
        private string ldName;
        private string ldPath;

        private string netArch;
        private string hostOS;
        private string apiLevel;

        private static readonly Dictionary<string, string> validHosts = new Dictionary<string, string>()
        {
            { "osx", "darwin-x86_64" },
            { "linux", "linux-x86_64" },
            { "windows", "windows-x86_64" }
        };

        private static readonly Dictionary<string, AndroidArch> validArches = new Dictionary<string, AndroidArch>()
        {
            { "arm", new AndroidArch("arm", "armeabi-v7a", "arm-linux-androideabi") },
            { "arm64", new AndroidArch("aarch64", "aarch64-v8a", "aarch64-linux-android") },
            { "x86", new AndroidArch("x86", "x86", "i686-linux-android") },
            { "x64", new AndroidArch("x86_64", "x86_64", "x86_64-linux-android") }
        };

        private string armClangPrefix = "armv7a";

        public NdkTools(string netArch, string hostOS, string apiLevel)
        {
            string cmdExt = Utils.IsWindows() ? ".cmd" : "";

            this.netArch = netArch;
            this.apiLevel = apiLevel;

            ValidateRequiredProps(hostOS);

            this.hostOS = validHosts[hostOS];

            toolRootPath = Path.Combine(Ndk.NdkPath, "toolchains", "llvm", "prebuilt", this.hostOS);

            asPrefixPath = Path.Combine(toolRootPath, Triple, "bin");
            toolPrefixPath = Path.Combine(toolRootPath, "bin");

            // arm clang prefix is not the triple, but armv7a instead
            if (netArch == "arm")
            {
                clangPath = Path.Combine(ToolPrefixPath, $"{armClangPrefix}{apiLevel}-clang{cmdExt}");
            }
            else
            {
                clangPath = Path.Combine(ToolPrefixPath, $"{Triple}{apiLevel}-clang{cmdExt}");
            }

            ldPath = toolPrefixPath;
            ldName = "ld";
        }

        public string ToolPrefixPath
        {
            get => toolPrefixPath;
        }

        public string AsPrefixPath
        {
            get => asPrefixPath;
        }

        public string Triple
        {
            get => validArches[netArch].Triple;
        }

        public string LdName
        {
            get => ldName;
        }

        public string LdPath
        {
            get => ldPath;
        }

        public string ClangPath
        {
            get => clangPath;
        }

        private void ValidateRequiredProps(string hostOS)
        {
            if (Ndk.NdkVersion.Main.Major != 27)
            {
                throw new Exception($"NDK 27 is required. An unsupported NDK version was found ({Ndk.NdkVersion.Main.Major}).");
            }

            try
            {
                string triple = Triple;
            }
            catch (KeyNotFoundException)
            {
                throw new Exception("An invalid target architecture was supplied. Only arm64, x64, arm, and x86 are supported.");
            }

            try
            {
                string host = validHosts[hostOS];
            }
            catch (KeyNotFoundException)
            {
                throw new Exception("An invalid HostOS value was supplied. Only windows, osx, and linux are supported.");
            }
        }
    }
}
