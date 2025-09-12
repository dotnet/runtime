// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

public struct CodeBlockHandle
{
    // TODO-Layering: These members should be accessible only to contract implementations.
    public readonly TargetPointer Address;
    public CodeBlockHandle(TargetPointer address) => Address = address;
}

public interface IExecutionManager : IContract
{
    static string IContract.Name { get; } = nameof(ExecutionManager);
    CodeBlockHandle? GetCodeBlockHandle(TargetCodePointer ip) => throw new NotImplementedException();
    TargetPointer GetMethodDesc(CodeBlockHandle codeInfoHandle) => throw new NotImplementedException();
    TargetCodePointer GetStartAddress(CodeBlockHandle codeInfoHandle) => throw new NotImplementedException();
    TargetCodePointer GetFuncletStartAddress(CodeBlockHandle codeInfoHandle) => throw new NotImplementedException();
    void GetMethodRegionInfo(CodeBlockHandle codeInfoHandle, out uint hotSize, out TargetPointer coldStart, out uint coldSize) => throw new NotImplementedException();
    uint GetJITType(CodeBlockHandle codeInfoHandle) => throw new NotImplementedException();
    TargetPointer NonVirtualEntry2MethodDesc(TargetCodePointer entrypoint) => throw new NotImplementedException();
    TargetPointer GetUnwindInfo(CodeBlockHandle codeInfoHandle) => throw new NotImplementedException();
    TargetPointer GetUnwindInfoBaseAddress(CodeBlockHandle codeInfoHandle) => throw new NotImplementedException();
    TargetPointer GetDebugInfo(CodeBlockHandle codeInfoHandle, out bool hasFlagByte) => throw new NotImplementedException();
    void GetGCInfo(CodeBlockHandle codeInfoHandle, out TargetPointer gcInfo, out uint gcVersion) => throw new NotImplementedException();
    TargetNUInt GetRelativeOffset(CodeBlockHandle codeInfoHandle) => throw new NotImplementedException();
}

public readonly struct ExecutionManager : IExecutionManager
{

}
