// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.DataContractReader.ExecutionManagerHelpers;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

public sealed class ExecutionManager_2 : IExecutionManager
{
    private IExecutionManager _executionManagerCore;

    internal ExecutionManager_2(Target target, Data.RangeSectionMap topRangeSectionMap)
    {
        _executionManagerCore = new ExecutionManagerCore<NibbleMapConstantLookup>(target, topRangeSectionMap);
    }

    public CodeBlockHandle? GetCodeBlockHandle(TargetCodePointer ip) => _executionManagerCore.GetCodeBlockHandle(ip);
    public TargetPointer GetMethodDesc(CodeBlockHandle codeInfoHandle) => _executionManagerCore.GetMethodDesc(codeInfoHandle);
    public TargetCodePointer GetStartAddress(CodeBlockHandle codeInfoHandle) => _executionManagerCore.GetStartAddress(codeInfoHandle);
    public TargetPointer GetUnwindInfo(CodeBlockHandle codeInfoHandle, TargetCodePointer ip) => _executionManagerCore.GetUnwindInfo(codeInfoHandle, ip);
    public TargetPointer GetUnwindInfoBaseAddress(CodeBlockHandle codeInfoHandle) => _executionManagerCore.GetUnwindInfoBaseAddress(codeInfoHandle);
}
