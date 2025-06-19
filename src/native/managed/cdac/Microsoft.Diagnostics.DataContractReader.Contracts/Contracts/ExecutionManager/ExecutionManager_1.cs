// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.DataContractReader.ExecutionManagerHelpers;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

public sealed class ExecutionManager_1 : IExecutionManager
{
    private IExecutionManager _executionManagerCore;

    internal ExecutionManager_1(Target target, Data.RangeSectionMap topRangeSectionMap)
    {
        _executionManagerCore = new ExecutionManagerCore<NibbleMapLinearLookup>(target, topRangeSectionMap);
    }

    public CodeBlockHandle? GetCodeBlockHandle(TargetCodePointer ip) => _executionManagerCore.GetCodeBlockHandle(ip);
    public TargetPointer GetMethodDesc(CodeBlockHandle codeInfoHandle) => _executionManagerCore.GetMethodDesc(codeInfoHandle);
    public TargetCodePointer GetStartAddress(CodeBlockHandle codeInfoHandle) => _executionManagerCore.GetStartAddress(codeInfoHandle);
    public TargetCodePointer GetFuncletStartAddress(CodeBlockHandle codeInfoHandle) => _executionManagerCore.GetFuncletStartAddress(codeInfoHandle);
    public TargetPointer GetUnwindInfo(CodeBlockHandle codeInfoHandle) => _executionManagerCore.GetUnwindInfo(codeInfoHandle);
    public TargetPointer GetUnwindInfoBaseAddress(CodeBlockHandle codeInfoHandle) => _executionManagerCore.GetUnwindInfoBaseAddress(codeInfoHandle);
    public void GetGCInfo(CodeBlockHandle codeInfoHandle, out TargetPointer gcInfo, out uint gcVersion) => _executionManagerCore.GetGCInfo(codeInfoHandle, out gcInfo, out gcVersion);
    public TargetNUInt GetRelativeOffset(CodeBlockHandle codeInfoHandle) => _executionManagerCore.GetRelativeOffset(codeInfoHandle);
}
