// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

public enum StubTraceKind
{
    Unknown,
    Failed,
    Managed,
    Unmanaged,
    UnjittedMethod,
    FramePush,
}

public enum StubContinuationKind : ulong
{
    None,
    MethodJitted,
    FramePush,
}

public readonly record struct StubContinuation(
    StubContinuationKind Kind,
    TargetPointer MethodDesc,
    TargetCodePointer Address);

public readonly record struct StubTraceStep(
    StubTraceKind Kind,
    TargetCodePointer Address,
    StubContinuation Continuation);

public interface IStubTracing : IContract
{
    static string IContract.Name { get; } = nameof(StubTracing);

    StubTraceStep TraceStubStep(
        TargetCodePointer address,
        StubContinuation continuation,
        TargetPointer thread) => throw new NotImplementedException();
}

public readonly struct StubTracing : IStubTracing
{
}
