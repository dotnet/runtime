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
        /// <safety>Holds only a pointer value addressing a caller-provided character buffer; reading or writing the field never dereferences it, so field access alone cannot read or write that buffer (any dereference requires an unsafe context).</safety>
        public char* AssemblyPathBuffer;
        /// <safety>Holds only a pointer value addressing a caller-provided character buffer; reading or writing the field never dereferences it, so field access alone cannot read or write that buffer (any dereference requires an unsafe context).</safety>
        public char* AssemblyNameBuffer;
        /// <safety>Holds only a pointer value addressing a caller-provided character buffer; reading or writing the field never dereferences it, so field access alone cannot read or write that buffer (any dereference requires an unsafe context).</safety>
        public char* TypeNameBuffer;
        public IntPtr ClassFactoryDest;
    }
}
