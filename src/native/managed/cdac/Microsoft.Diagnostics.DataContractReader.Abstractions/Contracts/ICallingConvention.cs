// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

public readonly record struct ArgSlot(
    int Offset,
    CorElementType ElementType);

public readonly record struct ArgLayout(
    bool IsPassedByRef,
    IReadOnlyList<ArgSlot> Slots,
    TypeHandle? ValueTypeHandle = null);

public readonly record struct CallSiteLayout(
    int? ThisOffset,
    bool IsValueTypeThis,
    int? AsyncContinuationOffset,
    int? VarArgCookieOffset,
    IReadOnlyList<ArgLayout> Arguments);

public interface ICallingConvention : IContract
{
    static string IContract.Name { get; } = nameof(CallingConvention);

    CallSiteLayout ComputeCallSiteLayout(MethodDescHandle method)
        => throw new NotImplementedException();
}

public readonly struct CallingConvention : ICallingConvention
{
    // Everything throws NotImplementedException
}
