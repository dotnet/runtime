// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
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
            return (PlatformDetection.IsNotBrowser) ?
                a.Location
                : "/" + a.GetName().Name + ".dll";
        }
    }
}