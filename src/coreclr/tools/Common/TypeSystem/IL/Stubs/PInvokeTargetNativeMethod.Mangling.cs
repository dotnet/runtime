// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Internal.TypeSystem;

namespace Internal.IL.Stubs
{
    public partial class PInvokeTargetNativeMethod : IPrefixMangledMethod
    {
        MethodDesc IPrefixMangledMethod.BaseMethod
        {
            get
            {
                return _declMethod;
            }
        }

        ReadOnlySpan<byte> IPrefixMangledMethod.Prefix
        {
            get
            {
                return "rawpinvoke"u8;
            }
        }
    }
}
