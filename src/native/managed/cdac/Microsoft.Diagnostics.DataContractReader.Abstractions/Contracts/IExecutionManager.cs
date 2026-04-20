// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

public struct CodeBlockHandle
{
    // TODO-Layering: These members should be accessible only to contract implementations.
    public readonly TargetPointer Address;
    public CodeBlockHandle(TargetPointer address) => Address = address;
}

public struct ExceptionClauseInfo
{
    public enum ExceptionClauseFlags : uint
    {
        Unknown = 0,
        Fault = 0x1,
        Finally = 0x2,
        Filter = 0x3,
        Typed = 0x4
    }
    public ExceptionClauseFlags ClauseType;
    public bool? IsCatchAllHandler;
    public uint TryStartPC;
    public uint TryEndPC;
    public uint HandlerStartPC;
    public uint HandlerEndPC;
    public uint? FilterOffset;
    public uint? ClassToken;
    public TargetNUInt? TypeHandle;
    public TargetPointer? ModuleAddr;
}

public struct JitManagerInfo
{
    public TargetPointer ManagerAddress;
    public uint CodeType;
    public TargetPointer HeapListAddress;
}

public interface ICodeHeapInfo
{
}

public sealed class LoaderCodeHeapInfo : ICodeHeapInfo
{
    public TargetPointer HeapAddress { get; }
    public TargetPointer LoaderHeapAddress { get; }

    public LoaderCodeHeapInfo(TargetPointer heapAddress, TargetPointer loaderHeapAddress)
    {
        HeapAddress = heapAddress;
        LoaderHeapAddress = loaderHeapAddress;
    }
}

public sealed class HostCodeHeapInfo : ICodeHeapInfo
{
    public TargetPointer HeapAddress { get; }
    public TargetPointer BaseAddress { get; }
    public TargetPointer CurrentAddress { get; }

    public HostCodeHeapInfo(TargetPointer heapAddress, TargetPointer baseAddress, TargetPointer currentAddress)
    {
        HeapAddress = heapAddress;
        BaseAddress = baseAddress;
        CurrentAddress = currentAddress;
    }
}

public sealed class UnknownCodeHeapInfo : ICodeHeapInfo {}

public enum JitType : uint
{
    Unknown = 0,
    Jit = 1,
    R2R = 2
}

public interface IExecutionManager : IContract
{
    static string IContract.Name { get; } = nameof(ExecutionManager);
    CodeBlockHandle? GetCodeBlockHandle(TargetCodePointer ip) => throw new NotImplementedException();
    TargetPointer GetMethodDesc(CodeBlockHandle codeInfoHandle) => throw new NotImplementedException();
    TargetCodePointer GetStartAddress(CodeBlockHandle codeInfoHandle) => throw new NotImplementedException();
    TargetCodePointer GetFuncletStartAddress(CodeBlockHandle codeInfoHandle) => throw new NotImplementedException();
    void GetMethodRegionInfo(CodeBlockHandle codeInfoHandle, out uint hotSize, out TargetPointer coldStart, out uint coldSize) => throw new NotImplementedException();
    JitType GetJITType(CodeBlockHandle codeInfoHandle) => throw new NotImplementedException();
    TargetPointer NonVirtualEntry2MethodDesc(TargetCodePointer entrypoint) => throw new NotImplementedException();
    bool IsFunclet(CodeBlockHandle codeInfoHandle) => throw new NotImplementedException();
    bool IsFilterFunclet(CodeBlockHandle codeInfoHandle) => throw new NotImplementedException();
    TargetPointer GetUnwindInfo(CodeBlockHandle codeInfoHandle) => throw new NotImplementedException();
    TargetPointer GetUnwindInfoBaseAddress(CodeBlockHandle codeInfoHandle) => throw new NotImplementedException();
    TargetPointer GetDebugInfo(CodeBlockHandle codeInfoHandle, out bool hasFlagByte) => throw new NotImplementedException();
    void GetGCInfo(CodeBlockHandle codeInfoHandle, out TargetPointer gcInfo, out uint gcVersion) => throw new NotImplementedException();
    TargetNUInt GetRelativeOffset(CodeBlockHandle codeInfoHandle) => throw new NotImplementedException();
    List<ExceptionClauseInfo> GetExceptionClauses(CodeBlockHandle codeInfoHandle) => throw new NotImplementedException();
    JitManagerInfo GetEEJitManagerInfo() => throw new NotImplementedException();
    IEnumerable<ICodeHeapInfo> GetCodeHeapInfos() => throw new NotImplementedException();
}

public readonly struct ExecutionManager : IExecutionManager
{

}
