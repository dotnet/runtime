// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Internal.TypeSystem;

namespace Internal.IL.Stubs
{
    /// <summary>
    /// contains functionality related to name mangling
    /// </summary>
    public partial class ForwardDelegateCreationThunk : IPrefixMangledType
    {
        TypeDesc IPrefixMangledType.BaseType
        {
            get
            {
                return _delegateType;
            }
        }

        ReadOnlySpan<byte> IPrefixMangledType.Prefix
        {
            get
            {
                return "ForwardDelegateCreationStub"u8;
            }
        }
    }
}
