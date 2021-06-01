// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Reflection;

namespace System.Runtime.InteropServices
{
    public static class RuntimeEnvironment
    {
        [Obsolete(Obsoletions.RuntimeEnvironmentMessage, DiagnosticId = Obsoletions.RuntimeEnvironmentDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        public static string SystemConfigurationFile => throw new PlatformNotSupportedException();

        public static bool FromGlobalAccessCache(Assembly a) => false;

        public static string GetRuntimeDirectory()
        {
            string? runtimeDirectory = typeof(object).Assembly.Location;
            if (!Path.IsPathRooted(runtimeDirectory))
            {
                runtimeDirectory = AppDomain.CurrentDomain.BaseDirectory;
            }
            return Path.GetDirectoryName(runtimeDirectory) + Path.DirectorySeparatorChar;
        }

        [Obsolete(Obsoletions.RuntimeEnvironmentMessage, DiagnosticId = Obsoletions.RuntimeEnvironmentDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        public static IntPtr GetRuntimeInterfaceAsIntPtr(Guid clsid, Guid riid) => throw new PlatformNotSupportedException();

        [Obsolete(Obsoletions.RuntimeEnvironmentMessage, DiagnosticId = Obsoletions.RuntimeEnvironmentDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        public static object GetRuntimeInterfaceAsObject(Guid clsid, Guid riid) => throw new PlatformNotSupportedException();

        public static string GetSystemVersion() => typeof(object).Assembly.ImageRuntimeVersion;
    }
}
