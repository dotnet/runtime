// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection.Metadata;

namespace System.Reflection.Emit
{
    /// <summary>
    /// Extensions to the public <cref>System.Reflection.Metadata.SignatureCallingConvention</cref> enum.
    /// </summary>
    [Flags]
    internal enum SignatureCallingConventionEx : byte
    {
        /// <summary>
        /// Indicates the presence of a "this" parameter.
        /// </summary>
        HasThis = 0x20,

        // Other values based on ECMA-335:
        // 0x0A: GenericInst (generic method instantiation)
        // 0x0B: NativeVarArg
        // 0x0C: Max (first invalid calling convention)
        // 0x0F: Mask for SignatureCallingConvention values
        // 0x10: Generic (generic method signature with explicit number of type arguments)
        // 0x40: ExplicitThis ("this" parameter is explicitly in the signature)
    }
}
