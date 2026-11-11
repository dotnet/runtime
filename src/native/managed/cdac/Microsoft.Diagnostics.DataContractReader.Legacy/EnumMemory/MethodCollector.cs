// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Contracts.Extensions;
using Microsoft.Diagnostics.DataContractReader.Legacy;
using ContractModuleHandle = Microsoft.Diagnostics.DataContractReader.Contracts.ModuleHandle;

namespace Microsoft.Diagnostics.DataContractReader.Legacy.EnumMemory;

internal sealed class MethodCollector(CollectorTarget target)
{
    private readonly CollectorTarget _collectorTarget = target;
    private readonly HashSet<TargetPointer> _captured = [];
    private readonly HashSet<TargetPointer> _capturedCodeBlocks = [];

    public void CaptureMethod(TargetPointer methodDesc, TargetCodePointer instructionPointer)
    {
        if (instructionPointer != TargetCodePointer.Null)
        {
            EnumerateInstructionPointer(instructionPointer);
            EnumerateCodeDependencies(instructionPointer);
        }

        if (methodDesc == TargetPointer.Null || !_captured.Add(methodDesc))
            return;

        EnumerateMethodDependencies(methodDesc);
        EnumerateMethodDescDataDependencies(methodDesc);
        CacheMethodName(methodDesc);
    }

    private void EnumerateInstructionPointer(TargetCodePointer instructionPointer)
    {
        ulong address = instructionPointer.ToAddress(_collectorTarget).Value;
        uint size = (uint)_collectorTarget.Target.PointerSize;
        ulong maxAddress = _collectorTarget.Target.PointerSize == sizeof(uint) ? uint.MaxValue : ulong.MaxValue;
        if (address > maxAddress - size)
            return;

        _collectorTarget.Emitter.Add(address, size);
    }

    private void EnumerateCodeDependencies(TargetCodePointer instructionPointer)
    {
        IExecutionManager executionManager = _collectorTarget.Target.Contracts.ExecutionManager;
        if (executionManager.GetCodeBlockHandle(instructionPointer) is not CodeBlockHandle codeBlock
            || !_capturedCodeBlocks.Add(codeBlock.Address))
            return;

        executionManager.GetGCInfo(codeBlock, out _, out _);

        IDebugInfo debugInfo = _collectorTarget.Target.Contracts.DebugInfo;
        if (!debugInfo.HasDebugInfo(instructionPointer))
            return;

        foreach (OffsetMapping _ in debugInfo.GetMethodNativeMap(instructionPointer, preferUninstrumented: true, out _))
        {
        }

        foreach (DebugVarInfo _ in debugInfo.GetMethodVarInfo(instructionPointer, out _))
        {
        }
    }

    private void EnumerateMethodDependencies(TargetPointer methodDesc)
    {
        IRuntimeTypeSystem types = _collectorTarget.Target.Contracts.RuntimeTypeSystem;
        MethodDescHandle method = types.GetMethodDescHandle(methodDesc);
        if (types.IsNoMetadataMethod(method, out _))
            return;

        uint token = types.GetMethodToken(method);
        TargetPointer methodTable = types.GetMethodTable(method);
        TargetPointer module = types.GetModule(types.GetTypeHandle(methodTable));
        ILoader loader = _collectorTarget.Target.Contracts.Loader;
        ContractModuleHandle moduleHandle = loader.GetModuleHandleFromModulePtr(module);
        loader.GetAssembly(moduleHandle);
        loader.GetFlags(moduleHandle);
        if (_collectorTarget.DumpType != DumpType.Triage)
            loader.GetPath(moduleHandle);
        loader.IsProbeExtensionResultValid(moduleHandle);
        loader.GetModuleLookupMapElement(moduleHandle, ModuleLookupMapKind.MethodDefToDesc, token, out _);
        loader.GetILHeader(moduleHandle, token);
    }

    private void EnumerateMethodDescDataDependencies(TargetPointer methodDesc)
    {
        IRuntimeTypeSystem types = _collectorTarget.Target.Contracts.RuntimeTypeSystem;
        MethodDescHandle method = types.GetMethodDescHandle(methodDesc);
        ICodeVersions codeVersions = _collectorTarget.Target.Contracts.CodeVersions;
        NativeCodeVersionHandle codeVersion = codeVersions.GetActiveNativeCodeVersion(methodDesc);

        types.IsDynamicMethod(method);
        types.GetSlotNumber(method);
        if (codeVersion.Valid)
        {
            TargetCodePointer nativeCode = codeVersions.GetNativeCode(codeVersion);
            if (nativeCode != TargetCodePointer.Null)
                _collectorTarget.Target.Contracts.PrecodeStubs.GetInterpreterCodeFromInterpreterPrecodeIfPresent(nativeCode);

            codeVersions.GetGCStressCodeCopy(codeVersion);
        }
        if (types.HasNativeCodeSlot(method))
            types.GetAddressOfNativeCodeSlot(method);
    }

    private void CacheMethodName(TargetPointer methodDesc)
    {
        if (_collectorTarget.MiniMetadata.Contains(methodDesc))
            return;

        IRuntimeTypeSystem types = _collectorTarget.Target.Contracts.RuntimeTypeSystem;
        MethodDescHandle method = types.GetMethodDescHandle(methodDesc);
        if (types.IsNoMetadataMethod(method, out _) && !types.IsILStub(method))
            return;

        try
        {
            StringBuilder name = new();
            TypeNameBuilder.AppendMethodInternal(
                _collectorTarget,
                name,
                method,
                TypeNameFormat.FormatSignature |
                TypeNameFormat.FormatNamespace |
                TypeNameFormat.FormatFullInst);
            if (_collectorTarget.DumpType is DumpType.Mini or DumpType.Triage)
                Sanitizer.StripArgumentsFromMethodName(name);
            if (name.Length != 0)
                _collectorTarget.MiniMetadata.Add(methodDesc, name.ToString());
        }
        catch (System.Exception ex) when (ex.HResult != HResults.COR_E_OPERATIONCANCELED)
        {
        }
    }
}
