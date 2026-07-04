// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Diagnostics.DataContractReader.ExecutionManagerHelpers;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

public sealed class ExecutionManager_2 : IExecutionManager
{
    private IExecutionManager _executionManagerCore;

    internal ExecutionManager_2(Target target)
    {
        TargetPointer addr = target.ReadGlobalPointer(Constants.Globals.ExecutionManagerCodeRangeMapAddress);
        _executionManagerCore = new ExecutionManagerCore<NibbleMapConstantLookup>(target, addr);
    }

    public CodeBlockHandle? GetCodeBlockHandle(TargetCodePointer ip) => _executionManagerCore.GetCodeBlockHandle(ip);
    public TargetPointer GetMethodDesc(CodeBlockHandle codeInfoHandle) => _executionManagerCore.GetMethodDesc(codeInfoHandle);
    public TargetPointer GetStartAddress(CodeBlockHandle codeInfoHandle) => _executionManagerCore.GetStartAddress(codeInfoHandle);
    public TargetPointer GetFuncletStartAddress(CodeBlockHandle codeInfoHandle) => _executionManagerCore.GetFuncletStartAddress(codeInfoHandle);
    public void GetMethodRegionInfo(CodeBlockHandle codeInfoHandle, out uint hotSize, out TargetPointer coldStart, out uint coldSize) => _executionManagerCore.GetMethodRegionInfo(codeInfoHandle, out hotSize, out coldStart, out coldSize);
    public TargetPointer NonVirtualEntry2MethodDesc(TargetCodePointer entrypoint) => _executionManagerCore.NonVirtualEntry2MethodDesc(entrypoint);
    public bool IsFunclet(CodeBlockHandle codeInfoHandle) => _executionManagerCore.IsFunclet(codeInfoHandle);
    public bool IsFilterFunclet(CodeBlockHandle codeInfoHandle) => _executionManagerCore.IsFilterFunclet(codeInfoHandle);
    public TargetPointer GetUnwindInfo(CodeBlockHandle codeInfoHandle) => _executionManagerCore.GetUnwindInfo(codeInfoHandle);
    public TargetPointer GetUnwindInfoBaseAddress(CodeBlockHandle codeInfoHandle) => _executionManagerCore.GetUnwindInfoBaseAddress(codeInfoHandle);
    public TargetPointer GetDebugInfo(CodeBlockHandle codeInfoHandle, out bool hasFlagByte) => _executionManagerCore.GetDebugInfo(codeInfoHandle, out hasFlagByte);
    public void GetGCInfo(CodeBlockHandle codeInfoHandle, out TargetPointer gcInfo, out uint gcVersion) => _executionManagerCore.GetGCInfo(codeInfoHandle, out gcInfo, out gcVersion);
    public TargetNUInt GetRelativeOffset(CodeBlockHandle codeInfoHandle) => _executionManagerCore.GetRelativeOffset(codeInfoHandle);
    public bool IsGcSafe(TargetCodePointer instructionPointer) => _executionManagerCore.IsGcSafe(instructionPointer);
    public uint GetStackParameterSize(CodeBlockHandle codeInfoHandle) => _executionManagerCore.GetStackParameterSize(codeInfoHandle);
    public List<ExceptionClauseInfo> GetExceptionClauses(CodeBlockHandle codeInfoHandle) => _executionManagerCore.GetExceptionClauses(codeInfoHandle);
    public JitManagerInfo GetEEJitManagerInfo() => _executionManagerCore.GetEEJitManagerInfo();
    public IEnumerable<ICodeHeapInfo> GetCodeHeapInfos() => _executionManagerCore.GetCodeHeapInfos();
    public CodeKind GetCodeKind(TargetCodePointer codeAddress) => _executionManagerCore.GetCodeKind(codeAddress);
    public TargetPointer FindReadyToRunModule(TargetPointer address) => _executionManagerCore.FindReadyToRunModule(address);
    public void Flush(FlushScope scope) => _executionManagerCore.Flush(scope);
}
