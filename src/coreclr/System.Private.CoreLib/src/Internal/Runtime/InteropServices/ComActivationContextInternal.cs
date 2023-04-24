// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Internal.Runtime.InteropServices
{
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct ComActivationContextInternal
    {
        public Guid ClassId;
        public Guid InterfaceId;
        public char* AssemblyPathBuffer;
        public char* AssemblyNameBuffer;
        public char* TypeNameBuffer;
        public IntPtr ClassFactoryDest;
    }
}
