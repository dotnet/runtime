// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ReferencedAssembly1
{
    public class Configuration
    {
        static int assemblyVersion;

        [UnmanagedCallersOnly(EntryPoint = "InitializeReferencedAssembly1", CallConvs = new Type[] { typeof(CallConvCdecl) })]
        public static void InitializeReferencedAssembly(int version)
        {
            assemblyVersion = version;
        }

        public int GetAssemblyVersion() => Configuration.assemblyVersion;
    }
}