// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Reflection;

#pragma warning disable 618

namespace DPStressHarness
{
    public class VersionUtil
    {
        public static string GetFileVersion(string moduleName)
        {
            FileVersionInfo info = GetFileVersionInfo(moduleName);
            return info.FileVersion;
        }

        public static string GetPrivateBuild(string moduleName)
        {
            FileVersionInfo info = GetFileVersionInfo(moduleName);
            return info.PrivateBuild;
        }

        private static FileVersionInfo GetFileVersionInfo(string moduleName)
        {
            if (File.Exists(moduleName))
            {
                return FileVersionInfo.GetVersionInfo(Path.GetFullPath(moduleName));
            }
            else
            {
                string moduleInRuntimeDir = AppContext.BaseDirectory + moduleName;
                return FileVersionInfo.GetVersionInfo(moduleInRuntimeDir);
            }
        }
    }
}
