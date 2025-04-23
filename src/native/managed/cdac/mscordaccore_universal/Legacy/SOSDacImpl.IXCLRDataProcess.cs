// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Contracts.Extensions;
using Microsoft.Diagnostics.DataContractReader.RuntimeTypeSystemHelpers;


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

    int IXCLRDataProcess.GetAddressType(ulong address, /*CLRDataAddressType*/ uint* type)
        => _legacyProcess is not null ? _legacyProcess.GetAddressType(address, type) : HResults.E_NOTIMPL;

    int IXCLRDataProcess.GetRuntimeNameByAddress(
        ulong address,
        uint flags,
        uint bufLen,
        uint* nameLen,
        char* nameBuf,
        ulong* displacement)
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

    int IXCLRDataProcess.GetModuleByAddress(ulong address, /*IXCLRDataModule*/ void** mod)
        => _legacyProcess is not null ? _legacyProcess.GetModuleByAddress(address, mod) : HResults.E_NOTIMPL;

    internal class EnumMethodInstances
    {
        private readonly Target _target;
        private readonly TargetPointer _mainMethodDesc;
        private readonly TargetPointer _appDomain;
        private readonly ILoader _loader;
        private readonly IRuntimeTypeSystem _rts;
        public EnumMethodInstances(Target target, TargetPointer methodDesc, TargetPointer appDomain)
        {
            _target = target;
            _mainMethodDesc = methodDesc;
            if (appDomain == TargetPointer.Null)
            {
                TargetPointer appDomainPointer = _target.ReadGlobalPointer(Constants.Globals.AppDomain);
                _appDomain = _target.ReadPointer(appDomainPointer);
            }
            else
            {
                _appDomain = appDomain;
            }

            _loader = _target.Contracts.Loader;
            _rts = _target.Contracts.RuntimeTypeSystem;
        }

        public int Start(TargetPointer methodDesc, TargetPointer appDomain)
        {
            if (appDomain == TargetPointer.Null)
            {
                TargetPointer appDomainPointer = _target.ReadGlobalPointer(Constants.Globals.AppDomain);
                appDomain = _target.ReadPointer(appDomainPointer);
            }

            if (!HasClassOrMethodInstantiation(methodDesc) && !HasNativeCodeAnyVersion(methodDesc))
            {
                return HResults.S_FALSE;
            }

            List<Contracts.ModuleHandle> modules = _loader.GetAssemblies(
                appDomain,
                AssemblyIterationFlags.IncludeLoaded | AssemblyIterationFlags.IncludeExecution);

            foreach (Contracts.ModuleHandle moduleHandle in modules)
            {
                List<TargetPointer> typeParams = _loader.GetAvailableTypeParams(moduleHandle);
                foreach (TargetPointer type in typeParams)
                {
                    Console.WriteLine(type);
                }
            }

            // for each module find available type params
            // for each module get inst method hash table

            return HResults.S_OK;
        }

        public IEnumerable<MethodDescHandle> IterateMethodInstantiations(Contracts.ModuleHandle moduleHandle)
        {
            List<TargetPointer> methodInstantiations = _loader.GetInstantiatedMethods(moduleHandle);

            foreach (TargetPointer methodPtr in methodInstantiations)
            {
                yield return _rts.GetMethodDescHandle(methodPtr);
            }
        }

        public IEnumerable<Contracts.TypeHandle> IterateTypeParams(Contracts.ModuleHandle moduleHandle)
        {
            List<TargetPointer> typeParams = _loader.GetAvailableTypeParams(moduleHandle);

            foreach (TargetPointer type in typeParams)
            {
                yield return _rts.GetTypeHandle(type);
            }
        }

        public IEnumerable<Contracts.ModuleHandle> IterateModules()
        {
            ILoader loader = _target.Contracts.Loader;
            List<Contracts.ModuleHandle> modules = loader.GetAssemblies(
                _appDomain,
                AssemblyIterationFlags.IncludeLoaded | AssemblyIterationFlags.IncludeExecution);

            foreach (Contracts.ModuleHandle moduleHandle in modules)
            {
                yield return moduleHandle;
            }
        }

        public IEnumerable<MethodDescHandle> IterateMethodInstances()
        {
            /*
            There are 4 cases for method instances:
            1. Non-generic method on non-generic type  (There is 1 MethodDesc for the method (excluding unboxing stubs, and such)
            2. Generic method on non-generic type (There is a generic defining method + a instantiated method for each particular instantiation)
            3. Non-generic method on generic type (There is 1 method for each generic instance created + 1 for the method on the uninstantiated generic type)
            4. Generic method on Generic type (There are N generic defining methods where N is the number of generic instantiations of the generic type + 1 on the uninstantiated generic types + M different generic instances of the method)
            */

            MethodDescHandle mainMD = _rts.GetMethodDescHandle(_mainMethodDesc);

            if (!HasClassOrMethodInstantiation(_mainMethodDesc))
            {
                // case 1
                // no method or class instantiation, then it's not generic.
                yield return mainMD;
                yield break;
            }

            TargetPointer mtAddr = _rts.GetMethodTable(mainMD);
            TypeHandle mainMT = _rts.GetTypeHandle(mtAddr);
            TargetPointer mainModule = _rts.GetModule(mainMT);
            uint mainMTToken = _rts.GetTypeDefToken(mainMT);
            uint mainMDToken = _rts.GetMethodToken(mainMD);
            ushort slotNum = _rts.GetSlotNumber(mainMD);

            if (HasMethodInstantiation(_mainMethodDesc))
            {
                // case 2/4
                // 2 is trivial, 4 is covered because the defining method on a generic type is not instantiated
                foreach (Contracts.ModuleHandle moduleHandle in IterateModules())
                {
                    foreach (MethodDescHandle methodDesc in IterateMethodInstantiations(moduleHandle))
                    {
                        TypeHandle methodTypeHandle = _rts.GetTypeHandle(_rts.GetMethodTable(methodDesc));

                        if (mainModule != _rts.GetModule(methodTypeHandle)) continue;
                        if (mainMDToken != _rts.GetMethodToken(methodDesc)) continue;

                        yield return methodDesc;
                    }
                }
            }

            if (HasClassInstantiation(_mainMethodDesc))
            {
                // case 3
                // class instantiations are only interesting if the method is not generic
                foreach (Contracts.ModuleHandle moduleHandle in IterateModules())
                {
                    if (HasClassInstantiation(_mainMethodDesc))
                    {
                        foreach (Contracts.TypeHandle typeParam in IterateTypeParams(moduleHandle))
                        {
                            uint typeParamToken = _rts.GetTypeDefToken(typeParam);

                            // not a MethodTable
                            if (typeParamToken == 0) continue;

                            // Check the class token
                            if (mainMTToken != typeParamToken) continue;

                            // Check the module is correct
                            if (mainModule != _rts.GetModule(typeParam)) continue;

                            TargetPointer cmt = _rts.GetCanonicalMethodTable(typeParam);
                            TypeHandle cmtHandle = _rts.GetTypeHandle(cmt);



                        }
                    }
                }
            }

        }

        private bool HasNativeCodeAnyVersion(TargetPointer methodDesc)
        {
            IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;
            ICodeVersions cv = _target.Contracts.CodeVersions;

            MethodDescHandle mdHandle = rts.GetMethodDescHandle(methodDesc);
            TargetCodePointer pcode = rts.GetNativeCode(mdHandle);

            if (pcode == TargetCodePointer.Null)
            {
                // I think this is equivalent to get any native code version
                NativeCodeVersionHandle nativeCodeVersion = cv.GetActiveNativeCodeVersion(methodDesc);
                if (nativeCodeVersion.Valid)
                {
                    pcode = cv.GetNativeCode(nativeCodeVersion);
                }
            }

            return pcode != TargetCodePointer.Null;
        }

        private bool HasClassOrMethodInstantiation(TargetPointer mdAddr)
        {
            return HasClassInstantiation(mdAddr) || HasMethodInstantiation(mdAddr);
        }

        private bool HasClassInstantiation(TargetPointer mdAddr)
        {
            IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;

            MethodDescHandle md = rts.GetMethodDescHandle(mdAddr);
            TargetPointer mtAddr = rts.GetMethodTable(md);
            TypeHandle mt = rts.GetTypeHandle(mtAddr);
            return !rts.GetInstantiation(mt).IsEmpty;
        }

        private bool HasMethodInstantiation(TargetPointer mdAddr)
        {
            IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;

            MethodDescHandle md = rts.GetMethodDescHandle(mdAddr);

            if (rts.IsGenericMethodDefinition(md)) return true;
            return !rts.GetGenericMethodInstantiation(md).IsEmpty;
        }
    }

    int IXCLRDataProcess.StartEnumMethodInstancesByAddress(ulong address, /*IXCLRDataAppDomain*/ void* appDomain, ulong* handle)
    {
        int hr = HResults.S_OK;
        try
        {
            *handle = 0;
            hr = HResults.S_FALSE;

            IExecutionManager eman = _target.Contracts.ExecutionManager;
            if (eman.GetCodeBlockHandle(address) is CodeBlockHandle cbh && eman.GetMethodDesc(cbh) is TargetPointer methodDesc)
            {

                EnumMethodInstances emi = new(_target);
                emi.Start(methodDesc, (ulong)appDomain);
                // GCHandle gcHandle = GCHandle.Alloc(emi);
                // *handle = (ulong)GCHandle.ToIntPtr(gcHandle).ToInt64();

            }
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
        return _legacyProcess is not null ? _legacyProcess.StartEnumMethodInstancesByAddress(address, appDomain, handle) : HResults.E_NOTIMPL;

        //return hr;
    }

    int IXCLRDataProcess.EnumMethodInstanceByAddress(ulong* handle, /*IXCLRDataMethodInstance*/ void** method)
        => _legacyProcess is not null ? _legacyProcess.EnumMethodInstanceByAddress(handle, method) : HResults.E_NOTIMPL;

    int IXCLRDataProcess.EndEnumMethodInstancesByAddress(ulong handle)
        => _legacyProcess is not null ? _legacyProcess.EndEnumMethodInstancesByAddress(handle) : HResults.E_NOTIMPL;

    int IXCLRDataProcess.GetDataByAddress(
        ulong address,
        uint flags,
        /*IXCLRDataAppDomain*/ void* appDomain,
        /*IXCLRDataTask*/ void* tlsTask,
        uint bufLen,
        uint* nameLen,
        char* nameBuf,
        /*IXCLRDataValue*/ void** value,
        ulong* displacement)
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
        ulong addr,
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

    int IXCLRDataProcess.StartEnumMethodDefinitionsByAddress(ulong address, ulong* handle)
        => _legacyProcess is not null ? _legacyProcess.StartEnumMethodDefinitionsByAddress(address, handle) : HResults.E_NOTIMPL;

    int IXCLRDataProcess.EnumMethodDefinitionByAddress(ulong* handle, /*IXCLRDataMethodDefinition*/ void** method)
        => _legacyProcess is not null ? _legacyProcess.EnumMethodDefinitionByAddress(handle, method) : HResults.E_NOTIMPL;

    int IXCLRDataProcess.EndEnumMethodDefinitionsByAddress(ulong handle)
        => _legacyProcess is not null ? _legacyProcess.EndEnumMethodDefinitionsByAddress(handle) : HResults.E_NOTIMPL;

    int IXCLRDataProcess.FollowStub(
        uint inFlags,
        ulong inAddr,
        /*struct CLRDATA_FOLLOW_STUB_BUFFER*/ void* inBuffer,
        ulong* outAddr,
        /*struct CLRDATA_FOLLOW_STUB_BUFFER*/ void* outBuffer,
        uint* outFlags)
        => _legacyProcess is not null ? _legacyProcess.FollowStub(inFlags, inAddr, inBuffer, outAddr, outBuffer, outFlags) : HResults.E_NOTIMPL;

    int IXCLRDataProcess.FollowStub2(
        /*IXCLRDataTask*/ void* task,
        uint inFlags,
        ulong inAddr,
        /*struct CLRDATA_FOLLOW_STUB_BUFFER*/ void* inBuffer,
        ulong* outAddr,
        /*struct CLRDATA_FOLLOW_STUB_BUFFER*/ void* outBuffer,
        uint* outFlags)
        => _legacyProcess is not null ? _legacyProcess.FollowStub2(task, inFlags, inAddr, inBuffer, outAddr, outBuffer, outFlags) : HResults.E_NOTIMPL;

    int IXCLRDataProcess.DumpNativeImage(
        ulong loadedBase,
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
