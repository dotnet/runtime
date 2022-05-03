// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace System.Reflection
{
    [System.Runtime.CompilerServices.ReflectionBlocked]
    public static class AssemblyRuntimeNameHelpers
    {
        //
        // Converts an AssemblyName to a RuntimeAssemblyName that is free from any future mutations on the AssemblyName.
        //
        public static RuntimeAssemblyName ToRuntimeAssemblyName(this AssemblyName assemblyName)
        {
            if (assemblyName.Name == null)
                throw new ArgumentException(SR.InvalidAssemblyName);

            AssemblyNameFlags flags = assemblyName.Flags;
            AssemblyContentType contentType = assemblyName.ContentType;
#pragma warning disable SYSLIB0037 // AssemblyName.ProcessorArchitecture is obsolete
            ProcessorArchitecture processorArchitecture = assemblyName.ProcessorArchitecture;
#pragma warning restore SYSLIB0037
            AssemblyNameFlags combinedFlags = CombineAssemblyNameFlags(flags, contentType, processorArchitecture);
            byte[]? pkOriginal;
            if (0 != (flags & AssemblyNameFlags.PublicKey))
                pkOriginal = assemblyName.GetPublicKey();
            else
                pkOriginal = assemblyName.GetPublicKeyToken();

            // AssemblyName's PKT property getters do NOT copy the array before giving it out. Make our own copy
            // as the original is wide open to tampering by anyone.
            byte[]? pkCopy = null;
            if (pkOriginal != null)
            {
                pkCopy = new byte[pkOriginal.Length];
                Array.Copy(pkOriginal, pkCopy, pkOriginal.Length);
            }

            return new RuntimeAssemblyName(assemblyName.Name, assemblyName.Version, assemblyName.CultureName, combinedFlags, pkCopy);
        }

        internal static AssemblyNameFlags CombineAssemblyNameFlags(AssemblyNameFlags flags, AssemblyContentType contentType, ProcessorArchitecture processorArchitecture)
        {
            return (AssemblyNameFlags)(((int)flags) | (((int)contentType) << 9) | ((int)processorArchitecture << 4));
        }
    }
}
