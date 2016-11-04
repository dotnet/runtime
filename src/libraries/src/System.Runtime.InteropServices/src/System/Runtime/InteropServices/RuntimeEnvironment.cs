// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Reflection;

namespace System.Runtime.InteropServices
{
    public static class RuntimeEnvironment
    {
        public static string SystemConfigurationFile
        {
            get
            {
                throw new PlatformNotSupportedException();
            }
        }
        public static bool FromGlobalAccessCache(System.Reflection.Assembly a)
        {
            return false;
        }
        public static string GetRuntimeDirectory()
        {
            return Path.GetDirectoryName(typeof(object).Assembly.Location) + Path.DirectorySeparatorChar;
        }
        public static System.IntPtr GetRuntimeInterfaceAsIntPtr(Guid clsid, Guid riid)
        {
            throw new PlatformNotSupportedException();
        }
        public static object GetRuntimeInterfaceAsObject(Guid clsid, Guid riid)
        {
            throw new PlatformNotSupportedException();
        }
        public static string GetSystemVersion()
        {
            return typeof(object).Assembly.ImageRuntimeVersion;
        }
    }
}
