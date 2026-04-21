// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Diagnostics.DataContractReader.ExecutionManagerHelpers;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

public sealed class ExecutionManager_1 : IExecutionManager
{
    private IExecutionManager _executionManagerCore;

    internal ExecutionManager_1(Target target)
    {
        TargetPointer addr = target.ReadGlobalPointer(Constants.Globals.ExecutionManagerCodeRangeMapAddress);
        Data.RangeSectionMap map = target.ProcessedData.GetOrAdd<Data.RangeSectionMap>(addr);
        _executionManagerCore = new ExecutionManagerCore<NibbleMapLinearLookup>(target, map);
    }

    public CodeBlockHandle? GetCodeBlockHandle(TargetCodePointer ip) => _executionManagerCore.GetCodeBlockHandle(ip);
    public TargetPointer GetMethodDesc(CodeBlockHandle codeInfoHandle) => _executionManagerCore.GetMethodDesc(codeInfoHandle);
    public TargetCodePointer GetStartAddress(CodeBlockHandle codeInfoHandle) => _executionManagerCore.GetStartAddress(codeInfoHandle);
    public TargetCodePointer GetFuncletStartAddress(CodeBlockHandle codeInfoHandle) => _executionManagerCore.GetFuncletStartAddress(codeInfoHandle);
    public void GetMethodRegionInfo(CodeBlockHandle codeInfoHandle, out uint hotSize, out TargetPointer coldStart, out uint coldSize) => _executionManagerCore.GetMethodRegionInfo(codeInfoHandle, out hotSize, out coldStart, out coldSize);
    public JitType GetJITType(CodeBlockHandle codeInfoHandle) => _executionManagerCore.GetJITType(codeInfoHandle);
    public TargetPointer NonVirtualEntry2MethodDesc(TargetCodePointer entrypoint) => _executionManagerCore.NonVirtualEntry2MethodDesc(entrypoint);
    public bool IsFunclet(CodeBlockHandle codeInfoHandle) => _executionManagerCore.IsFunclet(codeInfoHandle);
    public bool IsFilterFunclet(CodeBlockHandle codeInfoHandle) => _executionManagerCore.IsFilterFunclet(codeInfoHandle);
    public TargetPointer GetUnwindInfo(CodeBlockHandle codeInfoHandle) => _executionManagerCore.GetUnwindInfo(codeInfoHandle);
    public TargetPointer GetUnwindInfoBaseAddress(CodeBlockHandle codeInfoHandle) => _executionManagerCore.GetUnwindInfoBaseAddress(codeInfoHandle);
    public TargetPointer GetDebugInfo(CodeBlockHandle codeInfoHandle, out bool hasFlagByte) => _executionManagerCore.GetDebugInfo(codeInfoHandle, out hasFlagByte);
    public void GetGCInfo(CodeBlockHandle codeInfoHandle, out TargetPointer gcInfo, out uint gcVersion) => _executionManagerCore.GetGCInfo(codeInfoHandle, out gcInfo, out gcVersion);
    public TargetNUInt GetRelativeOffset(CodeBlockHandle codeInfoHandle) => _executionManagerCore.GetRelativeOffset(codeInfoHandle);
    public List<ExceptionClauseInfo> GetExceptionClauses(CodeBlockHandle codeInfoHandle) => _executionManagerCore.GetExceptionClauses(codeInfoHandle);
    public JitManagerInfo GetEEJitManagerInfo() => _executionManagerCore.GetEEJitManagerInfo();
    public IEnumerable<ICodeHeapInfo> GetCodeHeapInfos() => _executionManagerCore.GetCodeHeapInfos();
    public void Flush() => _executionManagerCore.Flush();
}
