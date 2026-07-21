// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

public interface IStackDataFrameHandle
{
    StackWalkState State { get; }
}

public enum StackWalkState
{
    Complete,
    Error,

    // The current Context represents a managed method.
    Frameless,

    // The current Context is the seed native context from init (the
    // thread's saved CONTEXT). FrameIter may or may not be on a Frame.
    InitialNativeContext,

    // The current Context is native, produced by unwinding a managed
    // frame down to an M2U boundary. FrameIter is on the explicit Frame.
    NativeMarker,

    // FrameAddress is valid and identifies the explicit Frame at FrameIter.
    // The current Context has not yet been bridged through that Frame; the
    // next step uses it to update the Context, after which Frameless is yielded.
    Frame,
    SkippedFrame,
}

// Classifies the origin of a reported stack reference.
//   InstructionPointer - a managed (frameless) JIT frame; Source is the native PC.
//   Frame              - an explicit capital-F Frame; Source is the Frame address.
//   Other              - a root which is not any of the other enums
public enum StackSourceType
{
    InstructionPointer = 0,
    Frame = 1,
    Other = 2,
}

public class StackReferenceData
{
    public bool HasRegisterInformation { get; init; }
    public bool IsInteriorPointer { get; init; }
    public int Register { get; init; }
    public int Offset { get; init; }
    public TargetPointer Address { get; init; }
    public TargetPointer Object { get; init; }
    public uint Flags { get; init; }
    public StackSourceType SourceType { get; init; }
    public TargetPointer Source { get; init; }
    public TargetPointer StackPointer { get; init; }
}

public enum InternalFrameType
{
    None,
    M2U,
    U2M,
    FuncEval,
    InternalCall,
    ClassInit,
    Exception,
    JitCompilation,
}

public record struct StackFrameData(
    TargetPointer FrameAddress,
    TargetPointer FrameIdentifier,
    InternalFrameType InternalFrameType);

public record struct DebuggerEvalData(
    uint MethodToken,
    TargetPointer AssemblyPtr);

[Flags]
public enum StackwalkFlag
{
    Default = 0,
}

public interface IStackWalk : IContract
{
    static string IContract.Name => nameof(StackWalk);
    IEnumerable<IStackDataFrameHandle> CreateStackWalk(ThreadData threadData) => throw new NotImplementedException();
    IEnumerable<IStackDataFrameHandle> CreateStackWalk(ThreadData threadData, byte[] contextBuffer, bool isFirst = true) => throw new NotImplementedException();
    IReadOnlyList<StackReferenceData> WalkStackReferences(ThreadData threadData, bool resolveInteriorPointers) => throw new NotImplementedException();
    byte[] GetRawContext(IStackDataFrameHandle stackDataFrameHandle, StackwalkFlag flags = StackwalkFlag.Default) => throw new NotImplementedException();
    TargetPointer GetFrameAddress(IStackDataFrameHandle stackDataFrameHandle) => throw new NotImplementedException();
    string GetFrameName(TargetPointer frameIdentifier) => throw new NotImplementedException();
    TargetPointer GetMethodDescPtr(TargetPointer framePtr) => throw new NotImplementedException();
    TargetPointer GetMethodDescPtr(IStackDataFrameHandle stackDataFrameHandle) => throw new NotImplementedException();
    TargetCodePointer GetInstructionPointer(IStackDataFrameHandle stackDataFrameHandle) => throw new NotImplementedException();
    IEnumerable<StackFrameData> GetFrames(TargetPointer threadPointer) => throw new NotImplementedException();
    bool IsExceptionHandlingHelperInlinedCallFrame(TargetPointer frameAddress) => throw new NotImplementedException();
    DebuggerEvalData GetDebuggerEvalData(TargetPointer funcEvalFrameAddress) => throw new NotImplementedException();
    TargetPointer GetRedirectedContextPointer(ThreadData threadData) => throw new NotImplementedException();
    byte[] GetContext(ThreadData threadData, ThreadContextSource contextSource, uint contextFlags) => throw new NotImplementedException();
}

public struct StackWalk : IStackWalk
{
    // Everything throws NotImplementedException
}
