// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Microsoft.Diagnostics.DataContractReader.Legacy;

/// <summary>
/// Implementation of IXCLRDataProcess* interfaces intended to be passed out to consumers
/// interacting with the DAC via those COM interfaces.
/// </summary>
internal sealed unsafe partial class SOSDacImpl : IXCLRDataProcess, IXCLRDataProcess2
{
    int IXCLRDataProcess.Flush()
    {
        _target.ProcessedData.Clear();

        // As long as any part of cDAC falls back to the legacy DAC, we need to propagate the Flush call
        if (_legacyProcess is not null)
            return _legacyProcess.Flush();

        return HResults.S_OK;
    }

    int IXCLRDataProcess.StartEnumTasks(ulong* handle)
        => _legacyProcess is not null ? _legacyProcess.StartEnumTasks(handle) : HResults.E_NOTIMPL;

    int IXCLRDataProcess.EnumTask(ulong* handle, /*IXCLRDataTask*/ void** task)
        => _legacyProcess is not null ? _legacyProcess.EnumTask(handle, task) : HResults.E_NOTIMPL;

    int IXCLRDataProcess.EndEnumTasks(ulong handle)
        => _legacyProcess is not null ? _legacyProcess.EndEnumTasks(handle) : HResults.E_NOTIMPL;

    int IXCLRDataProcess.GetTaskByOSThreadID(uint osThreadID, out IXCLRDataTask? task)
    {
        task = default;

        // Find the thread correspending to the OS thread ID
        Contracts.IThread contract = _target.Contracts.Thread;
        TargetPointer thread = contract.GetThreadStoreData().FirstThread;
        TargetPointer matchingThread = TargetPointer.Null;
        while (thread != TargetPointer.Null)
        {
            Contracts.ThreadData threadData = contract.GetThreadData(thread);
            if (threadData.OSId.Value == osThreadID)
            {
                matchingThread = thread;
                break;
            }

            thread = threadData.NextThread;
        }

        if (matchingThread == TargetPointer.Null)
            return HResults.E_INVALIDARG;

        IXCLRDataTask? legacyTask = null;
        if (_legacyProcess is not null)
        {
            int hr = _legacyProcess.GetTaskByOSThreadID(osThreadID, out legacyTask);
            if (hr < 0)
                return hr;
        }

        task = new ClrDataTask(matchingThread, _target, legacyTask);
        return HResults.S_OK;
    }

    int IXCLRDataProcess.GetTaskByUniqueID(ulong taskID, /*IXCLRDataTask*/ void** task)
        => _legacyProcess is not null ? _legacyProcess.GetTaskByUniqueID(taskID, task) : HResults.E_NOTIMPL;

    int IXCLRDataProcess.GetFlags(uint* flags)
        => _legacyProcess is not null ? _legacyProcess.GetFlags(flags) : HResults.E_NOTIMPL;

    int IXCLRDataProcess.IsSameObject(IXCLRDataProcess* process)
        => _legacyProcess is not null ? _legacyProcess.IsSameObject(process) : HResults.E_NOTIMPL;

    int IXCLRDataProcess.GetManagedObject(/*IXCLRDataValue*/ void** value)
        => _legacyProcess is not null ? _legacyProcess.GetManagedObject(value) : HResults.E_NOTIMPL;

    int IXCLRDataProcess.GetDesiredExecutionState(uint* state)
        => _legacyProcess is not null ? _legacyProcess.GetDesiredExecutionState(state) : HResults.E_NOTIMPL;

    int IXCLRDataProcess.SetDesiredExecutionState(uint state)
        => _legacyProcess is not null ? _legacyProcess.SetDesiredExecutionState(state) : HResults.E_NOTIMPL;

    int IXCLRDataProcess.GetAddressType(ClrDataAddress address, /*CLRDataAddressType*/ uint* type)
        => _legacyProcess is not null ? _legacyProcess.GetAddressType(address, type) : HResults.E_NOTIMPL;

    int IXCLRDataProcess.GetRuntimeNameByAddress(
        ClrDataAddress address,
        uint flags,
        uint bufLen,
        uint* nameLen,
        char* nameBuf,
        ClrDataAddress* displacement)
        => _legacyProcess is not null ? _legacyProcess.GetRuntimeNameByAddress(address, flags, bufLen, nameLen, nameBuf, displacement) : HResults.E_NOTIMPL;

    int IXCLRDataProcess.StartEnumAppDomains(ulong* handle)
        => _legacyProcess is not null ? _legacyProcess.StartEnumAppDomains(handle) : HResults.E_NOTIMPL;

    int IXCLRDataProcess.EnumAppDomain(ulong* handle, /*IXCLRDataAppDomain*/ void** appDomain)
        => _legacyProcess is not null ? _legacyProcess.EnumAppDomain(handle, appDomain) : HResults.E_NOTIMPL;

    int IXCLRDataProcess.EndEnumAppDomains(ulong handle)
        => _legacyProcess is not null ? _legacyProcess.EndEnumAppDomains(handle) : HResults.E_NOTIMPL;

    int IXCLRDataProcess.GetAppDomainByUniqueID(ulong id, /*IXCLRDataAppDomain*/ void** appDomain)
        => _legacyProcess is not null ? _legacyProcess.GetAppDomainByUniqueID(id, appDomain) : HResults.E_NOTIMPL;

    int IXCLRDataProcess.StartEnumAssemblies(ulong* handle)
        => _legacyProcess is not null ? _legacyProcess.StartEnumAssemblies(handle) : HResults.E_NOTIMPL;

    int IXCLRDataProcess.EnumAssembly(ulong* handle, /*IXCLRDataAssembly*/ void** assembly)
        => _legacyProcess is not null ? _legacyProcess.EnumAssembly(handle, assembly) : HResults.E_NOTIMPL;

    int IXCLRDataProcess.EndEnumAssemblies(ulong handle)
        => _legacyProcess is not null ? _legacyProcess.EndEnumAssemblies(handle) : HResults.E_NOTIMPL;

    int IXCLRDataProcess.StartEnumModules(ulong* handle)
        => _legacyProcess is not null ? _legacyProcess.StartEnumModules(handle) : HResults.E_NOTIMPL;

    int IXCLRDataProcess.EnumModule(ulong* handle, /*IXCLRDataModule*/ void** mod)
        => _legacyProcess is not null ? _legacyProcess.EnumModule(handle, mod) : HResults.E_NOTIMPL;

    int IXCLRDataProcess.EndEnumModules(ulong handle)
        => _legacyProcess is not null ? _legacyProcess.EndEnumModules(handle) : HResults.E_NOTIMPL;

    int IXCLRDataProcess.GetModuleByAddress(ClrDataAddress address, /*IXCLRDataModule*/ void** mod)
        => _legacyProcess is not null ? _legacyProcess.GetModuleByAddress(address, mod) : HResults.E_NOTIMPL;

    int IXCLRDataProcess.StartEnumMethodInstancesByAddress(ulong address, /*IXCLRDataAppDomain*/ void* appDomain, ulong* handle)
        => _legacyProcess is not null ? _legacyProcess.StartEnumMethodInstancesByAddress(address, appDomain, handle) : HResults.E_NOTIMPL;

    int IXCLRDataProcess.EnumMethodInstanceByAddress(ulong* handle, /*IXCLRDataMethodInstance*/ void** method)
        => _legacyProcess is not null ? _legacyProcess.EnumMethodInstanceByAddress(handle, method) : HResults.E_NOTIMPL;

    int IXCLRDataProcess.EndEnumMethodInstancesByAddress(ulong handle)
        => _legacyProcess is not null ? _legacyProcess.EndEnumMethodInstancesByAddress(handle) : HResults.E_NOTIMPL;

    int IXCLRDataProcess.GetDataByAddress(
        ClrDataAddress address,
        uint flags,
        /*IXCLRDataAppDomain*/ void* appDomain,
        /*IXCLRDataTask*/ void* tlsTask,
        uint bufLen,
        uint* nameLen,
        char* nameBuf,
        /*IXCLRDataValue*/ void** value,
        ClrDataAddress* displacement)
        => _legacyProcess is not null ? _legacyProcess.GetDataByAddress(address, flags, appDomain, tlsTask, bufLen, nameLen, nameBuf, value, displacement) : HResults.E_NOTIMPL;

    int IXCLRDataProcess.GetExceptionStateByExceptionRecord(/*struct EXCEPTION_RECORD64*/ void* record, /*IXCLRDataExceptionState*/ void** exState)
        => _legacyProcess is not null ? _legacyProcess.GetExceptionStateByExceptionRecord(record, exState) : HResults.E_NOTIMPL;

    int IXCLRDataProcess.TranslateExceptionRecordToNotification(/*struct EXCEPTION_RECORD64*/ void* record, /*IXCLRDataExceptionNotification*/ void* notify)
        => _legacyProcess is not null ? _legacyProcess.TranslateExceptionRecordToNotification(record, notify) : HResults.E_NOTIMPL;

    int IXCLRDataProcess.Request(uint reqCode, uint inBufferSize, byte* inBuffer, uint outBufferSize, byte* outBuffer)
        => _legacyProcess is not null ? _legacyProcess.Request(reqCode, inBufferSize, inBuffer, outBufferSize, outBuffer) : HResults.E_NOTIMPL;

    int IXCLRDataProcess.CreateMemoryValue(
        /*IXCLRDataAppDomain*/ void* appDomain,
        /*IXCLRDataTask*/ void* tlsTask,
        /*IXCLRDataTypeInstance*/ void* type,
        ClrDataAddress addr,
        /*IXCLRDataValue*/ void** value)
        => _legacyProcess is not null ? _legacyProcess.CreateMemoryValue(appDomain, tlsTask, type, addr, value) : HResults.E_NOTIMPL;

    int IXCLRDataProcess.SetAllTypeNotifications(/*IXCLRDataModule*/ void* mod, uint flags)
        => _legacyProcess is not null ? _legacyProcess.SetAllTypeNotifications(mod, flags) : HResults.E_NOTIMPL;

    int IXCLRDataProcess.SetAllCodeNotifications(/*IXCLRDataModule*/ void* mod, uint flags)
        => _legacyProcess is not null ? _legacyProcess.SetAllCodeNotifications(mod, flags) : HResults.E_NOTIMPL;

    int IXCLRDataProcess.GetTypeNotifications(
        uint numTokens,
        /*IXCLRDataModule*/ void** mods,
        /*IXCLRDataModule*/ void* singleMod,
        [In, MarshalUsing(CountElementName = nameof(numTokens))] /*mdTypeDef*/ uint[] tokens,
        [In, Out, MarshalUsing(CountElementName = nameof(numTokens))] uint[] flags)
        => _legacyProcess is not null ? _legacyProcess.GetTypeNotifications(numTokens, mods, singleMod, tokens, flags) : HResults.E_NOTIMPL;

    int IXCLRDataProcess.SetTypeNotifications(
        uint numTokens,
        /*IXCLRDataModule*/ void** mods,
        /*IXCLRDataModule*/ void* singleMod,
        [In, MarshalUsing(CountElementName = nameof(numTokens))] /*mdTypeDef*/ uint[] tokens,
        [In, MarshalUsing(CountElementName = nameof(numTokens))] uint[] flags,
        uint singleFlags)
        => _legacyProcess is not null ? _legacyProcess.SetTypeNotifications(numTokens, mods, singleMod, tokens, flags, singleFlags) : HResults.E_NOTIMPL;

    int IXCLRDataProcess.GetCodeNotifications(
        uint numTokens,
        /*IXCLRDataModule*/ void** mods,
        /*IXCLRDataModule*/ void* singleMod,
        [In, MarshalUsing(CountElementName = nameof(numTokens))] /*mdMethodDef*/ uint[] tokens,
        [In, Out, MarshalUsing(CountElementName = nameof(numTokens))] uint[] flags)
        => _legacyProcess is not null ? _legacyProcess.GetCodeNotifications(numTokens, mods, singleMod, tokens, flags) : HResults.E_NOTIMPL;

    int IXCLRDataProcess.SetCodeNotifications(
        uint numTokens,
        /*IXCLRDataModule*/ void** mods,
        /*IXCLRDataModule*/ void* singleMod,
        [In, MarshalUsing(CountElementName = nameof(numTokens))] /*mdMethodDef */ uint[] tokens,
        [In, MarshalUsing(CountElementName = nameof(numTokens))] uint[] flags,
        uint singleFlags)
        => _legacyProcess is not null ? _legacyProcess.SetCodeNotifications(numTokens, mods, singleMod, tokens, flags, singleFlags) : HResults.E_NOTIMPL;

    int IXCLRDataProcess.GetOtherNotificationFlags(uint* flags)
        => _legacyProcess is not null ? _legacyProcess.GetOtherNotificationFlags(flags) : HResults.E_NOTIMPL;

    int IXCLRDataProcess.SetOtherNotificationFlags(uint flags)
        => _legacyProcess is not null ? _legacyProcess.SetOtherNotificationFlags(flags) : HResults.E_NOTIMPL;

    int IXCLRDataProcess.StartEnumMethodDefinitionsByAddress(ClrDataAddress address, ulong* handle)
        => _legacyProcess is not null ? _legacyProcess.StartEnumMethodDefinitionsByAddress(address, handle) : HResults.E_NOTIMPL;

    int IXCLRDataProcess.EnumMethodDefinitionByAddress(ulong* handle, /*IXCLRDataMethodDefinition*/ void** method)
        => _legacyProcess is not null ? _legacyProcess.EnumMethodDefinitionByAddress(handle, method) : HResults.E_NOTIMPL;

    int IXCLRDataProcess.EndEnumMethodDefinitionsByAddress(ulong handle)
        => _legacyProcess is not null ? _legacyProcess.EndEnumMethodDefinitionsByAddress(handle) : HResults.E_NOTIMPL;

    int IXCLRDataProcess.FollowStub(
        uint inFlags,
        ClrDataAddress inAddr,
        /*struct CLRDATA_FOLLOW_STUB_BUFFER*/ void* inBuffer,
        ClrDataAddress* outAddr,
        /*struct CLRDATA_FOLLOW_STUB_BUFFER*/ void* outBuffer,
        uint* outFlags)
        => _legacyProcess is not null ? _legacyProcess.FollowStub(inFlags, inAddr, inBuffer, outAddr, outBuffer, outFlags) : HResults.E_NOTIMPL;

    int IXCLRDataProcess.FollowStub2(
        /*IXCLRDataTask*/ void* task,
        uint inFlags,
        ClrDataAddress inAddr,
        /*struct CLRDATA_FOLLOW_STUB_BUFFER*/ void* inBuffer,
        ClrDataAddress* outAddr,
        /*struct CLRDATA_FOLLOW_STUB_BUFFER*/ void* outBuffer,
        uint* outFlags)
        => _legacyProcess is not null ? _legacyProcess.FollowStub2(task, inFlags, inAddr, inBuffer, outAddr, outBuffer, outFlags) : HResults.E_NOTIMPL;

    int IXCLRDataProcess.DumpNativeImage(
        ClrDataAddress loadedBase,
        char* name,
        /*IXCLRDataDisplay*/ void* display,
        /*IXCLRLibrarySupport*/ void* libSupport,
        /*IXCLRDisassemblySupport*/ void* dis)
        => _legacyProcess is not null ? _legacyProcess.DumpNativeImage(loadedBase, name, display, libSupport, dis) : HResults.E_NOTIMPL;

    int IXCLRDataProcess2.GetGcNotification(GcEvtArgs* gcEvtArgs)
        => _legacyProcess2 is not null ? _legacyProcess2.GetGcNotification(gcEvtArgs) : HResults.E_NOTIMPL;

    int IXCLRDataProcess2.SetGcNotification(GcEvtArgs gcEvtArgs)
        => _legacyProcess2 is not null ? _legacyProcess2.SetGcNotification(gcEvtArgs) : HResults.E_NOTIMPL;
}
