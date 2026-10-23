// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Reflection;

namespace System
{
    public static class AssemblyPathHelper
    {
        public static string GetAssemblyLocation(Assembly a)
        {
            // Note, in Browser, assemblies are loaded from memory and in that case, Assembly.Location will return an empty
            // string.  For these tests, the assemblies will also be available in the VFS, so just specify the assembly name
            // plus extension.
            const string browserVirtualAppBase = "/"; // keep in sync other places that define browserVirtualAppBase

            if (PlatformDetection.IsBrowser)
            {
                return browserVirtualAppBase + a.GetName().Name + ".dll";
            }

            string location = a.Location;
            if (string.IsNullOrEmpty(location) && PlatformDetection.IsWasi)
            {
                // WASI ReadyToRun serves assembly images from memory, but the IL files are still on the TPA list.
                location = FindTrustedPlatformAssembly(a.GetName().Name) ?? location;
            }

            return location;
        }

        private static string FindTrustedPlatformAssembly(string name)
        {
            if (AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") is string tpa)
            {
                foreach (string path in tpa.Split(Path.PathSeparator))
                {
                    if (string.Equals(Path.GetFileNameWithoutExtension(path), name, StringComparison.OrdinalIgnoreCase))
                    {
                        return path;
                    }
                }
            }

            return null;
        }
    }
}
