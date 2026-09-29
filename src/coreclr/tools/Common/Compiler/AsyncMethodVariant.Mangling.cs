// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Internal.TypeSystem;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler
{
    public partial class AsyncMethodVariant : MethodDelegator, IPrefixMangledMethod
    {
        MethodDesc IPrefixMangledMethod.BaseMethod => _wrappedMethod;

        ReadOnlySpan<byte> IPrefixMangledMethod.Prefix => "AsyncCallable"u8;
    }

    public partial class ReturnDroppingAsyncThunk : MethodDelegator, IPrefixMangledMethod
    {
        MethodDesc IPrefixMangledMethod.BaseMethod => _asyncVariant.Target;

        ReadOnlySpan<byte> IPrefixMangledMethod.Prefix => "ReturnDroppingAsync"u8;
    }
}
