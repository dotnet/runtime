// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

using Internal.TypeSystem;

namespace Internal.IL.Stubs
{
    public partial class CalliMarshallingMethodThunk : IPrefixMangledSignature
    {
        MethodSignature IPrefixMangledSignature.BaseSignature
        {
            get
            {
                return _targetSignature;
            }
        }

        ReadOnlySpan<byte> IPrefixMangledSignature.Prefix
        {
            get
            {
                ReadOnlySpan<byte> prefix = RuntimeMarshallingEnabled ? "CalliWithRuntimeMarshalling"u8 : "Calli"u8;

                // The target signature is expected to be normalized as MethodSignatureFlags.UnmanagedCallingConvention
                Debug.Assert((_targetSignature.Flags & MethodSignatureFlags.UnmanagedCallingConventionMask) == MethodSignatureFlags.UnmanagedCallingConvention);

                // Append calling convention details to the prefix
                if (_targetSignature.HasEmbeddedSignatureData)
                    prefix = prefix.Append(System.Text.Encoding.ASCII.GetBytes(_targetSignature.GetStandaloneMethodSignatureCallingConventions().ToString("x")));

                return prefix;
            }
        }
    }
}
