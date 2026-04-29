// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Contracts.Extensions;

namespace Microsoft.Diagnostics.DataContractReader.Legacy;

/// <summary>
/// Implementation of IXCLRDataProcess* interfaces intended to be passed out to consumers
/// interacting with the DAC via those COM interfaces.
/// </summary>
public sealed unsafe partial class SOSDacImpl : IXCLRDataProcess, IXCLRDataProcess2
{
    int IXCLRDataProcess.Flush()
    {
        _target.Flush();

        // Flush is always propagated — it's cache management, not data retrieval.
        if (_legacyProcess is not null)
            return _legacyProcess.Flush();

        return HResults.S_OK;
    }

    int IXCLRDataProcess.StartEnumTasks(ulong* handle)
        => LegacyFallbackHelper.CanFallback() && _legacyProcess is not null ? _legacyProcess.StartEnumTasks(handle) : HResults.E_NOTIMPL;

    int IXCLRDataProcess.EnumTask(ulong* handle, DacComNullableByRef<IXCLRDataTask> task)
        => LegacyFallbackHelper.CanFallback() && _legacyProcess is not null ? _legacyProcess.EnumTask(handle, task) : HResults.E_NOTIMPL;

    int IXCLRDataProcess.EndEnumTasks(ulong handle)
        => LegacyFallbackHelper.CanFallback() && _legacyProcess is not null ? _legacyProcess.EndEnumTasks(handle) : HResults.E_NOTIMPL;

    int IXCLRDataProcess.GetTaskByOSThreadID(uint osThreadID, DacComNullableByRef<IXCLRDataTask> task)
    {
        // Find the thread corresponding to the OS thread ID
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
            DacComNullableByRef<IXCLRDataTask> legacyTaskOut = new(isNullRef: false);
            int hr = _legacyProcess.GetTaskByOSThreadID(osThreadID, legacyTaskOut);
            if (hr < 0)
                return hr;
            legacyTask = legacyTaskOut.Interface;
        }

        task.Interface = new ClrDataTask(matchingThread, _target, legacyTask);
        return HResults.S_OK;
    }

    int IXCLRDataProcess.GetTaskByUniqueID(ulong taskID, DacComNullableByRef<IXCLRDataTask> task)
        => LegacyFallbackHelper.CanFallback() && _legacyProcess is not null ? _legacyProcess.GetTaskByUniqueID(taskID, task) : HResults.E_NOTIMPL;

    int IXCLRDataProcess.GetFlags(uint* flags)
        => LegacyFallbackHelper.CanFallback() && _legacyProcess is not null ? _legacyProcess.GetFlags(flags) : HResults.E_NOTIMPL;

    int IXCLRDataProcess.IsSameObject(IXCLRDataProcess* process)
        => LegacyFallbackHelper.CanFallback() && _legacyProcess is not null ? _legacyProcess.IsSameObject(process) : HResults.E_NOTIMPL;

    int IXCLRDataProcess.GetManagedObject(DacComNullableByRef<IXCLRDataValue> value)
        => LegacyFallbackHelper.CanFallback() && _legacyProcess is not null ? _legacyProcess.GetManagedObject(value) : HResults.E_NOTIMPL;

    int IXCLRDataProcess.GetDesiredExecutionState(uint* state)
        => LegacyFallbackHelper.CanFallback() && _legacyProcess is not null ? _legacyProcess.GetDesiredExecutionState(state) : HResults.E_NOTIMPL;

    int IXCLRDataProcess.SetDesiredExecutionState(uint state)
        => LegacyFallbackHelper.CanFallback() && _legacyProcess is not null ? _legacyProcess.SetDesiredExecutionState(state) : HResults.E_NOTIMPL;

    int IXCLRDataProcess.GetAddressType(ClrDataAddress address, /*CLRDataAddressType*/ uint* type)
        => LegacyFallbackHelper.CanFallback() && _legacyProcess is not null ? _legacyProcess.GetAddressType(address, type) : HResults.E_NOTIMPL;

    int IXCLRDataProcess.GetRuntimeNameByAddress(
        ClrDataAddress address,
        uint flags,
        uint bufLen,
        uint* nameLen,
        char* nameBuf,
        ClrDataAddress* displacement)
        => LegacyFallbackHelper.CanFallback() && _legacyProcess is not null ? _legacyProcess.GetRuntimeNameByAddress(address, flags, bufLen, nameLen, nameBuf, displacement) : HResults.E_NOTIMPL;

    int IXCLRDataProcess.StartEnumAppDomains(ulong* handle)
        => LegacyFallbackHelper.CanFallback() && _legacyProcess is not null ? _legacyProcess.StartEnumAppDomains(handle) : HResults.E_NOTIMPL;

    int IXCLRDataProcess.EnumAppDomain(ulong* handle, /*IXCLRDataAppDomain*/ void** appDomain)
        => LegacyFallbackHelper.CanFallback() && _legacyProcess is not null ? _legacyProcess.EnumAppDomain(handle, appDomain) : HResults.E_NOTIMPL;

    int IXCLRDataProcess.EndEnumAppDomains(ulong handle)
        => LegacyFallbackHelper.CanFallback() && _legacyProcess is not null ? _legacyProcess.EndEnumAppDomains(handle) : HResults.E_NOTIMPL;

    int IXCLRDataProcess.GetAppDomainByUniqueID(ulong id, /*IXCLRDataAppDomain*/ void** appDomain)
        => LegacyFallbackHelper.CanFallback() && _legacyProcess is not null ? _legacyProcess.GetAppDomainByUniqueID(id, appDomain) : HResults.E_NOTIMPL;

    int IXCLRDataProcess.StartEnumAssemblies(ulong* handle)
        => LegacyFallbackHelper.CanFallback() && _legacyProcess is not null ? _legacyProcess.StartEnumAssemblies(handle) : HResults.E_NOTIMPL;

    int IXCLRDataProcess.EnumAssembly(ulong* handle, DacComNullableByRef<IXCLRDataAssembly> assembly)
        => LegacyFallbackHelper.CanFallback() && _legacyProcess is not null ? _legacyProcess.EnumAssembly(handle, assembly) : HResults.E_NOTIMPL;

    int IXCLRDataProcess.EndEnumAssemblies(ulong handle)
        => LegacyFallbackHelper.CanFallback() && _legacyProcess is not null ? _legacyProcess.EndEnumAssemblies(handle) : HResults.E_NOTIMPL;

    int IXCLRDataProcess.StartEnumModules(ulong* handle)
        => LegacyFallbackHelper.CanFallback() && _legacyProcess is not null ? _legacyProcess.StartEnumModules(handle) : HResults.E_NOTIMPL;

    int IXCLRDataProcess.EnumModule(ulong* handle, DacComNullableByRef<IXCLRDataModule> mod)
        => LegacyFallbackHelper.CanFallback() && _legacyProcess is not null ? _legacyProcess.EnumModule(handle, mod) : HResults.E_NOTIMPL;

    int IXCLRDataProcess.EndEnumModules(ulong handle)
        => LegacyFallbackHelper.CanFallback() && _legacyProcess is not null ? _legacyProcess.EndEnumModules(handle) : HResults.E_NOTIMPL;

    int IXCLRDataProcess.GetModuleByAddress(ClrDataAddress address, DacComNullableByRef<IXCLRDataModule> mod)
        => LegacyFallbackHelper.CanFallback() && _legacyProcess is not null ? _legacyProcess.GetModuleByAddress(address, mod) : HResults.E_NOTIMPL;

    internal sealed class EnumMethodInstances : IEnum<MethodDescHandle>
    {
        private readonly Target _target;
        private readonly TargetPointer _mainMethodDesc;
        public readonly TargetPointer _appDomain;
        private readonly ILoader _loader;
        private readonly IRuntimeTypeSystem _rts;
        private readonly ICodeVersions _cv;
        public IEnumerator<MethodDescHandle> Enumerator { get; set; } = Enumerable.Empty<MethodDescHandle>().GetEnumerator();
        public TargetPointer LegacyHandle { get; set; } = TargetPointer.Null;

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
            _cv = _target.Contracts.CodeVersions;
        }

        public int Start()
        {
            MethodDescHandle mainMD = _rts.GetMethodDescHandle(_mainMethodDesc);
            if (!HasClassOrMethodInstantiation(mainMD) && !_cv.HasNativeCodeAnyVersion(_mainMethodDesc))
            {
                return HResults.S_FALSE;
            }

            Enumerator = IterateMethodInstances().GetEnumerator();

            return HResults.S_OK;
        }

        private IEnumerable<MethodDescHandle> IterateMethodInstantiations(Contracts.ModuleHandle moduleHandle)
        {
            IEnumerable<TargetPointer> methodInstantiations = _loader.GetInstantiatedMethods(moduleHandle);

            foreach (TargetPointer methodPtr in methodInstantiations)
            {
                yield return _rts.GetMethodDescHandle(methodPtr);
            }
        }

        private IEnumerable<Contracts.TypeHandle> IterateTypeParams(Contracts.ModuleHandle moduleHandle)
        {
            IEnumerable<TargetPointer> typeParams = _loader.GetAvailableTypeParams(moduleHandle);

            foreach (TargetPointer type in typeParams)
            {
                yield return _rts.GetTypeHandle(type);
            }
        }

        private IEnumerable<Contracts.ModuleHandle> IterateModules()
        {
            ILoader loader = _target.Contracts.Loader;
            IEnumerable<Contracts.ModuleHandle> modules = loader.GetModuleHandles(
                _appDomain,
                AssemblyIterationFlags.IncludeLoaded | AssemblyIterationFlags.IncludeExecution);

            foreach (Contracts.ModuleHandle moduleHandle in modules)
            {
                yield return moduleHandle;
            }
        }

        private IEnumerable<MethodDescHandle> IterateMethodInstances()
        {
            /*
            There are 4 cases for method instances:
            1. Non-generic method on non-generic type  (There is 1 MethodDesc for the method (excluding unboxing stubs, and such)
            2. Generic method on non-generic type (There is a generic defining method + a instantiated method for each particular instantiation)
            3. Non-generic method on generic type (There is 1 method for each generic instance created + 1 for the method on the uninstantiated generic type)
            4. Generic method on Generic type (There are N generic defining methods where N is the number of generic instantiations of the generic type + 1 on the uninstantiated generic types + M different generic instances of the method)
            */

            MethodDescHandle mainMD = _rts.GetMethodDescHandle(_mainMethodDesc);

            if (!HasClassOrMethodInstantiation(mainMD))
            {
                // case 1
                // no method or class instantiation, then it's not generic.
                if (_cv.HasNativeCodeAnyVersion(_mainMethodDesc))
                {
                    yield return mainMD;
                }
                yield break;
            }

            TargetPointer mtAddr = _rts.GetMethodTable(mainMD);
            TypeHandle mainMT = _rts.GetTypeHandle(mtAddr);
            TargetPointer mainModule = _rts.GetModule(mainMT);
            uint mainMTToken = _rts.GetTypeDefToken(mainMT);
            uint mainMDToken = _rts.GetMethodToken(mainMD);
            ushort slotNum = _rts.GetSlotNumber(mainMD);

            if (HasMethodInstantiation(mainMD))
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

                        if (_cv.HasNativeCodeAnyVersion(methodDesc.Address))
                        {
                            yield return methodDesc;
                        }
                    }
                }

                yield break;
            }

            if (HasClassInstantiation(mainMD))
            {
                // case 3
                // class instantiations are only interesting if the method is not generic
                foreach (Contracts.ModuleHandle moduleHandle in IterateModules())
                {
                    if (HasClassInstantiation(mainMD))
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

                            TargetPointer methodDescAddr = _rts.GetMethodDescForSlot(cmtHandle, slotNum);
                            if (methodDescAddr == TargetPointer.Null) continue;
                            MethodDescHandle methodDesc = _rts.GetMethodDescHandle(methodDescAddr);

                            if (_cv.HasNativeCodeAnyVersion(methodDescAddr))
                            {
                                yield return methodDesc;
                            }
                        }
                    }
                }

                yield break;
            }

        }

        private bool HasClassOrMethodInstantiation(MethodDescHandle md)
        {
            return HasClassInstantiation(md) || HasMethodInstantiation(md);
        }

        private bool HasClassInstantiation(MethodDescHandle md)
        {
            IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;

            TargetPointer mtAddr = rts.GetMethodTable(md);
            TypeHandle mt = rts.GetTypeHandle(mtAddr);
            return !rts.GetInstantiation(mt).IsEmpty;
        }

        private bool HasMethodInstantiation(MethodDescHandle md)
        {
            IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;

            if (rts.IsGenericMethodDefinition(md)) return true;
            return !rts.GetGenericMethodInstantiation(md).IsEmpty;
        }
    }

    int IXCLRDataProcess.StartEnumMethodInstancesByAddress(ClrDataAddress address, IXCLRDataAppDomain? appDomain, ulong* handle)
    {
        int hr = HResults.S_FALSE;
        *handle = 0;

        // Start the legacy enumeration to keep it in sync with the cDAC enumeration.
        // EnumMethodInstanceByAddress passes the legacy method instance to ClrDataMethodInstance,
        // which delegates some operations to it.
        ulong handleLocal = default;
        int hrLocal = default;
        if (_legacyProcess is not null)
        {
            hrLocal = _legacyProcess.StartEnumMethodInstancesByAddress(address, appDomain, &handleLocal);
        }

        try
        {
            TargetCodePointer methodAddr = address.ToTargetCodePointer(_target);

            // ClrDataAccess::IsPossibleCodeAddress
            // Does a trivial check on the readability of the address
            bool isTriviallyReadable = _target.TryRead(methodAddr, out byte _);
            if (!isTriviallyReadable)
                throw new ArgumentException();

            IExecutionManager eman = _target.Contracts.ExecutionManager;
            if (eman.GetCodeBlockHandle(methodAddr) is CodeBlockHandle cbh &&
                eman.GetMethodDesc(cbh) is TargetPointer methodDesc)
            {
                EnumMethodInstances emi = new(_target, methodDesc, TargetPointer.Null);
                emi.LegacyHandle = handleLocal;

                hr = emi.Start();
                if (hr == HResults.S_OK)
                {
                    *handle = (ulong)((IEnum<MethodDescHandle>)emi).GetHandle();
                    // Legacy handle ownership transferred to emi — don't clean up below.
                    handleLocal = default;
                }
            }
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
        finally
        {
            // The legacy enumeration is started eagerly (before the cDAC try block) so
            // that EnumMethodInstanceByAddress can advance both enumerations in lockstep.
            // If the cDAC side fails to produce an enum (exception, no code block found,
            // or emi.Start() returns S_FALSE), the legacy handle would be orphaned because
            // the caller receives *handle == 0 and has no way to call End. Clean it up here.
            if (_legacyProcess is not null && handleLocal != default)
            {
                _legacyProcess.EndEnumMethodInstancesByAddress(handleLocal);
            }
        }

#if DEBUG
        if (_legacyProcess is not null)
        {
            Debug.ValidateHResult(hr, hrLocal);
        }
#endif
        return hr;
    }

    int IXCLRDataProcess.EnumMethodInstanceByAddress(ulong* handle, DacComNullableByRef<IXCLRDataMethodInstance> method)
    {
        int hr = HResults.S_OK;

        GCHandle gcHandle = GCHandle.FromIntPtr((IntPtr)(*handle));
        if (gcHandle.Target is not EnumMethodInstances emi) return HResults.E_INVALIDARG;

        // Advance the legacy enumeration to keep it in sync with the cDAC enumeration.
        // The legacy method instance is passed to ClrDataMethodInstance for delegation.
        IXCLRDataMethodInstance? legacyMethod = null;
        int hrLocal = default;
        if (_legacyProcess is not null)
        {
            ulong legacyHandle = emi.LegacyHandle;
            DacComNullableByRef<IXCLRDataMethodInstance> legacyMethodOut = new(isNullRef: false);
            hrLocal = _legacyProcess.EnumMethodInstanceByAddress(&legacyHandle, legacyMethodOut);
            legacyMethod = legacyMethodOut.Interface;
            emi.LegacyHandle = legacyHandle;
        }

        try
        {
            if (emi.Enumerator.MoveNext())
            {
                MethodDescHandle methodDesc = emi.Enumerator.Current;
                method.Interface = new ClrDataMethodInstance(_target, methodDesc, emi._appDomain, legacyMethod);
            }
            else
            {
                hr = HResults.S_FALSE;
            }
        }
        catch (System.Exception ex)
        {
            // The cDAC's IterateMethodInstances() implementation is incomplete compared
            // to the native DAC's EnumMethodInstances::Next(). The native DAC uses a
            // MethodIterator backed by AppDomain assembly iteration with EX_TRY/EX_CATCH
            // error handling around each step. The cDAC re-implements this with
            // IterateModules()/IterateMethodInstantiations()/IterateTypeParams() which
            // call into IRuntimeTypeSystem and ILoader contracts. These contract calls
            // (e.g. GetMethodTable, GetTypeHandle, GetMethodDescForSlot, GetModule,
            // GetTypeDefToken) can throw when encountering method descs or type handles
            // from assemblies/modules that the cDAC cannot fully process. This has been
            // observed for generic method instantiations (cases 2-4 in
            // IterateMethodInstances) in the SOS.WebApp3 integration test.
            //
            // Fall back to the legacy DAC result when available, otherwise propagate the error.
            if (_legacyProcess is not null)
            {
                hr = hrLocal;
                method.Interface = legacyMethod;
            }
            else
            {
                hr = ex.HResult;
            }
        }

#if DEBUG
        if (_legacyProcess is not null)
        {
            Debug.ValidateHResult(hr, hrLocal);
        }
#endif

        return hr;
    }

    int IXCLRDataProcess.EndEnumMethodInstancesByAddress(ulong handle)
    {
        int hr = HResults.S_OK;

        GCHandle gcHandle = GCHandle.FromIntPtr((IntPtr)handle);
        if (gcHandle.Target is not EnumMethodInstances emi) return HResults.E_INVALIDARG;
        gcHandle.Free();

        if (_legacyProcess != null && emi.LegacyHandle != TargetPointer.Null)
        {
            int hrLocal = _legacyProcess.EndEnumMethodInstancesByAddress(emi.LegacyHandle);
            if (hrLocal < 0)
                return hrLocal;
        }

        return hr;
    }

    int IXCLRDataProcess.GetDataByAddress(
        ClrDataAddress address,
        uint flags,
        IXCLRDataAppDomain? appDomain,
        IXCLRDataTask? tlsTask,
        uint bufLen,
        uint* nameLen,
        char* nameBuf,
        DacComNullableByRef<IXCLRDataValue> value,
        ClrDataAddress* displacement)
        => LegacyFallbackHelper.CanFallback() && _legacyProcess is not null ? _legacyProcess.GetDataByAddress(address, flags, appDomain, tlsTask, bufLen, nameLen, nameBuf, value, displacement) : HResults.E_NOTIMPL;

    int IXCLRDataProcess.GetExceptionStateByExceptionRecord(EXCEPTION_RECORD64* record, DacComNullableByRef<IXCLRDataExceptionState> exState)
        => LegacyFallbackHelper.CanFallback() && _legacyProcess is not null ? _legacyProcess.GetExceptionStateByExceptionRecord(record, exState) : HResults.E_NOTIMPL;

    int IXCLRDataProcess.TranslateExceptionRecordToNotification(EXCEPTION_RECORD64* record, [MarshalUsing(typeof(UniqueComInterfaceMarshaller<IXCLRDataExceptionNotification>))] IXCLRDataExceptionNotification notify)
    {
        // notify must be unique so that we can cast it to ComObject, call FinalRelease on it, and deterministically release.
        // This is required because notify is a stack allocated object created by the caller.
        // If the object goes out of scope before we dispose and finalize it, we can crash during GC.
        ComObject comObj = (ComObject)(object)notify;

        int hr = HResults.S_OK;
        try
        {
            Span<TargetPointer> exInfo = stackalloc TargetPointer[EXCEPTION_RECORD64.ExceptionMaximumParameters];
            for (int i = 0; i < EXCEPTION_RECORD64.ExceptionMaximumParameters; i++)
                exInfo[i] = new TargetPointer(record->ExceptionInformation[i]);

            INotifications notifications = _target.Contracts.Notifications;
            if (!notifications.TryParseNotification(exInfo, out NotificationData? notification))
                return HResults.E_INVALIDARG;

            switch (notification)
            {
                case ModuleLoadNotificationData moduleLoad:
                {
                    IXCLRDataModule? legacyModule = null;
                    if (_legacyImpl is not null)
                    {
                        DacComNullableByRef<IXCLRDataModule> legacyModuleOut = new(isNullRef: false);
                        _legacyImpl.GetModule(moduleLoad.ModuleAddress.ToClrDataAddress(_target), legacyModuleOut);
                        legacyModule = legacyModuleOut.Interface;
                    }

                    notify.OnModuleLoaded(new ClrDataModule(moduleLoad.ModuleAddress, _target, legacyModule));
                    break;
                }

                case ModuleUnloadNotificationData moduleUnload:
                {
                    IXCLRDataModule? legacyModule = null;
                    if (_legacyImpl is not null)
                    {
                        DacComNullableByRef<IXCLRDataModule> legacyModuleOut = new(isNullRef: false);
                        _legacyImpl.GetModule(moduleUnload.ModuleAddress.ToClrDataAddress(_target), legacyModuleOut);
                        legacyModule = legacyModuleOut.Interface;
                    }

                    notify.OnModuleUnloaded(new ClrDataModule(moduleUnload.ModuleAddress, _target, legacyModule));
                    break;
                }

                case JitNotificationData jit:
                {
                    TargetPointer appDomainPointer = _target.ReadGlobalPointer(Constants.Globals.AppDomain);
                    TargetPointer appDomain = _target.ReadPointer(appDomainPointer);

                    IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;
                    MethodDescHandle methodDesc = rts.GetMethodDescHandle(jit.MethodDescAddress);

                    ClrDataMethodInstance methodInst = new(_target, methodDesc, appDomain, null);
                    notify.OnCodeGenerated(methodInst);
                    if (notify is IXCLRDataExceptionNotification5 notify5)
                    {
                        notify5.OnCodeGenerated2(methodInst, jit.NativeCodeAddress.ToClrDataAddress(_target));
                    }
                    break;
                }

                case ExceptionNotificationData exception:
                {
                    if (notify is IXCLRDataExceptionNotification2 notify2)
                    {
                        IThread thread = _target.Contracts.Thread;
                        Contracts.ThreadData threadData = thread.GetThreadData(exception.ThreadAddress);
                        TargetPointer thrownObjectHandle = thread.GetCurrentExceptionHandle(exception.ThreadAddress);
                        notify2.OnException(new ClrDataExceptionState(
                            _target,
                            exception.ThreadAddress,
                            (uint)CLRDataExceptionStateFlag.CLRDATA_EXCEPTION_DEFAULT,
                            thrownObjectHandle,
                            threadData.FirstNestedException,
                            null));
                    }
                    else
                        return HResults.E_INVALIDARG;
                    break;
                }

                case GcNotificationData gc:
                {
                    if (gc.IsSupportedEvent)
                    {
                        if (notify is IXCLRDataExceptionNotification3 notify3)
                        {
                            notify3.OnGcEvent(new GcEvtArgs
                            {
                                type = gc.EventData.EventType switch
                                {
                                    GcEventType.MarkEnd => GcEvtArgs.GcEvt_t.GC_MARK_END,
                                    _ => GcEvtArgs.GcEvt_t.GC_EVENT_TYPE_MAX,
                                },
                                condemnedGeneration = gc.EventData.CondemnedGeneration,
                            });
                        }
                        hr = HResults.S_OK;
                    }
                    else
                        hr = HResults.E_FAIL;
                    break;
                }

                case ExceptionCatcherEnterNotificationData exceptionCatcherEnter:
                {
                    if (notify is IXCLRDataExceptionNotification4 notify4)
                    {
                        TargetPointer appDomainPointer = _target.ReadGlobalPointer(Constants.Globals.AppDomain);
                        TargetPointer appDomain = _target.ReadPointer(appDomainPointer);
                        IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;
                        MethodDescHandle methodDesc = rts.GetMethodDescHandle(exceptionCatcherEnter.MethodDescAddress);
                        notify4.ExceptionCatcherEnter(new ClrDataMethodInstance(_target, methodDesc, appDomain, null), exceptionCatcherEnter.NativeOffset);
                    }
                    break;
                }
            }
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
        finally
        {
            comObj.FinalRelease();
        }
        return hr;
    }

    int IXCLRDataProcess.Request(uint reqCode, uint inBufferSize, byte* inBuffer, uint outBufferSize, byte* outBuffer)
    {
        int hr = HResults.E_INVALIDARG;

        if (reqCode == (uint)CLRDataGeneralRequest.CLRDATA_REQUEST_REVISION)
        {
            if (inBufferSize == 0 && inBuffer is null && outBufferSize == sizeof(uint) && outBuffer is not null)
            {
                // Revision 10: Fixed DefaultCOMImpl::Release() to use pre-decrement (--mRef).
                // Consumers that previously compensated for the broken ref counting (e.g., ClrMD)
                // should check this revision to avoid double-freeing.
                *(uint*)outBuffer = 10;
                hr = HResults.S_OK;
            }
        }
        else
        {
            return LegacyFallbackHelper.CanFallback() && _legacyProcess is not null ? _legacyProcess.Request(reqCode, inBufferSize, inBuffer, outBufferSize, outBuffer) : HResults.E_NOTIMPL;
        }
#if DEBUG
        if (_legacyProcess is not null)
        {
            byte[] localBuffer = new byte[(int)outBufferSize];
            fixed (byte* localOutBuffer = localBuffer)
            {
                int hrLocal = _legacyProcess.Request(reqCode, inBufferSize, inBuffer, outBufferSize, localOutBuffer);
                Debug.ValidateHResult(hr, hrLocal);
                if (hr == HResults.S_OK && reqCode == (uint)CLRDataGeneralRequest.CLRDATA_REQUEST_REVISION)
                {
                    Debug.Assert(outBufferSize == sizeof(uint) && outBuffer is not null);
                    uint legacyRevision = *(uint*)localOutBuffer;
                    uint revision = *(uint*)outBuffer;
                    Debug.Assert(revision == legacyRevision);
                }
            }
        }
#endif
        return hr;
    }

    int IXCLRDataProcess.CreateMemoryValue(
        IXCLRDataAppDomain? appDomain,
        IXCLRDataTask? tlsTask,
        IXCLRDataTypeInstance? type,
        ClrDataAddress addr,
        DacComNullableByRef<IXCLRDataValue> value)
        => LegacyFallbackHelper.CanFallback() && _legacyProcess is not null ? _legacyProcess.CreateMemoryValue(appDomain, tlsTask, type, addr, value) : HResults.E_NOTIMPL;

    int IXCLRDataProcess.SetAllTypeNotifications(IXCLRDataModule? mod, uint flags)
        => LegacyFallbackHelper.CanFallback() && _legacyProcess is not null ? _legacyProcess.SetAllTypeNotifications(mod, flags) : HResults.E_NOTIMPL;

    int IXCLRDataProcess.SetAllCodeNotifications(IXCLRDataModule? mod, uint flags)
    {
        int hr = HResults.S_OK;
        try
        {
            if (!CodeNotificationFlagsConverter.IsValid(flags))
                throw new ArgumentException("Invalid code notification flags");

            TargetPointer moduleAddr = TargetPointer.Null;
            if (mod is not null)
            {
                if (mod is not ClrDataModule cdm)
                    throw new ArgumentException();
                moduleAddr = cdm.Address;
            }

            _target.Contracts.CodeNotifications.SetAllCodeNotifications(moduleAddr, CodeNotificationFlagsConverter.FromCom(flags));
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

        // No #if DEBUG validation: SetAllCodeNotifications is a write operation.
        // Both the cDAC and legacy DAC independently write to g_pNotificationTable.

        return hr;
    }

    int IXCLRDataProcess.GetTypeNotifications(
        uint numTokens,
        /*IXCLRDataModule*/ void** mods,
        IXCLRDataModule? singleMod,
        [In, MarshalUsing(CountElementName = nameof(numTokens))] /*mdTypeDef*/ uint[]? tokens,
        [In, Out, MarshalUsing(CountElementName = nameof(numTokens))] uint[]? flags)
        => LegacyFallbackHelper.CanFallback() && _legacyProcess is not null ? _legacyProcess.GetTypeNotifications(numTokens, mods, singleMod, tokens, flags) : HResults.E_NOTIMPL;

    int IXCLRDataProcess.SetTypeNotifications(
        uint numTokens,
        /*IXCLRDataModule*/ void** mods,
        IXCLRDataModule? singleMod,
        [In, MarshalUsing(CountElementName = nameof(numTokens))] /*mdTypeDef*/ uint[]? tokens,
        [In, MarshalUsing(CountElementName = nameof(numTokens))] uint[]? flags,
        uint singleFlags)
        => LegacyFallbackHelper.CanFallback() && _legacyProcess is not null ? _legacyProcess.SetTypeNotifications(numTokens, mods, singleMod, tokens, flags, singleFlags) : HResults.E_NOTIMPL;

    int IXCLRDataProcess.GetCodeNotifications(
        uint numTokens,
        /*IXCLRDataModule*/ void** mods,
        IXCLRDataModule? singleMod,
        [In, MarshalUsing(CountElementName = nameof(numTokens))] /*mdMethodDef*/ uint[]? tokens,
        [In, Out, MarshalUsing(CountElementName = nameof(numTokens))] uint[]? flags)
    {
        int hr = HResults.S_OK;
        ICodeNotifications codeNotif = _target.Contracts.CodeNotifications;

        try
        {
            // Match legacy DAC (daccess.cpp ClrDataAccess::GetCodeNotifications):
            // tokens and flags are both required; exactly one of mods/singleMod must be non-null.
            if (tokens is null || flags is null ||
                (mods is null && singleMod is null) ||
                (mods is not null && singleMod is not null))
                throw new ArgumentException();

            TargetPointer moduleAddr = TargetPointer.Null;
            if (singleMod is not null)
            {
                if (singleMod is not ClrDataModule singleCdm)
                    throw new ArgumentException();
                moduleAddr = singleCdm.Address;
            }

            for (uint i = 0; i < numTokens; i++)
            {
                if (singleMod is null)
                    moduleAddr = GetModuleAddress(mods[i]);

                flags[i] = CodeNotificationFlagsConverter.ToCom(codeNotif.GetCodeNotification(moduleAddr, tokens[i]));
            }
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

        // No #if DEBUG validation: GetCodeNotifications is a read, but both cDAC and
        // legacy DAC allocate the table on-demand when called, which would cause
        // dual-allocation. Validation is safe at a higher layer when a dump is used.

        return hr;
    }

    int IXCLRDataProcess.SetCodeNotifications(
        uint numTokens,
        /*IXCLRDataModule*/ void** mods,
        IXCLRDataModule? singleMod,
        [In, MarshalUsing(CountElementName = nameof(numTokens))] /*mdMethodDef */ uint[]? tokens,
        [In, MarshalUsing(CountElementName = nameof(numTokens))] uint[]? flags,
        uint singleFlags)
    {
        // Behavior difference from the legacy DAC: the legacy DAC performs an upfront
        // capacity check (numTokens > table size returns E_OUTOFMEMORY with no writes
        // performed). The cDAC's CodeNotifications contract does not expose a capacity
        // check, so this batch wrapper writes entries one at a time. If the in-target
        // table fills up part-way through, entries written before the overflow remain
        // set and the first failing per-entry SetCodeNotification surfaces a COMException
        // with HResult == E_FAIL, which is mapped to the returned hr below. Callers that
        // depend on atomic batch semantics should size their batches conservatively.
        int hr = HResults.S_OK;

        try
        {
            if (tokens is null ||
                (mods is null && singleMod is null) ||
                (mods is not null && singleMod is not null))
                throw new ArgumentException();

            // Validate flags.
            if (flags is not null)
            {
                for (uint check = 0; check < numTokens; check++)
                {
                    if (!CodeNotificationFlagsConverter.IsValid(flags[check]))
                        throw new ArgumentException("Invalid code notification flags");
                }
            }
            else if (!CodeNotificationFlagsConverter.IsValid(singleFlags))
            {
                throw new ArgumentException("Invalid code notification flags");
            }

            TargetPointer moduleAddr = TargetPointer.Null;
            if (singleMod is not null)
            {
                if (singleMod is not ClrDataModule singleCdm)
                    throw new ArgumentException();
                moduleAddr = singleCdm.Address;
            }

            for (uint i = 0; i < numTokens; i++)
            {
                if (singleMod is null)
                    moduleAddr = GetModuleAddress(mods[i]);

                uint f = flags is not null ? flags[i] : singleFlags;
                _target.Contracts.CodeNotifications.SetCodeNotification(moduleAddr, tokens[i], CodeNotificationFlagsConverter.FromCom(f));
            }
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

        // No #if DEBUG validation: SetCodeNotifications is a write operation.
        // Both the cDAC and legacy DAC independently write to g_pNotificationTable.

        return hr;
    }

    int IXCLRDataProcess.GetOtherNotificationFlags(uint* flags)
    {
        int hr = HResults.S_OK;
        try
        {
            *flags = _target.Read<uint>(_target.ReadGlobalPointer(Constants.Globals.DacNotificationFlags));
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
#if DEBUG
        if (_legacyProcess is not null)
        {
            uint flagsLocal;
            int hrLocal = _legacyProcess.GetOtherNotificationFlags(&flagsLocal);
            Debug.ValidateHResult(hr, hrLocal);
            Debug.Assert(*flags == flagsLocal);
        }
#endif
        return hr;
    }
    int IXCLRDataProcess.SetOtherNotificationFlags(uint flags)
    {
        int hr = HResults.S_OK;
        try
        {
            if ((flags & ~((uint)CLRDataOtherNotifyFlag.CLRDATA_NOTIFY_ON_MODULE_LOAD |
                           (uint)CLRDataOtherNotifyFlag.CLRDATA_NOTIFY_ON_MODULE_UNLOAD |
                           (uint)CLRDataOtherNotifyFlag.CLRDATA_NOTIFY_ON_EXCEPTION |
                           (uint)CLRDataOtherNotifyFlag.CLRDATA_NOTIFY_ON_EXCEPTION_CATCH_ENTER)) != 0)
            {
                hr = HResults.E_INVALIDARG;
            }
            else
            {
                TargetPointer dacNotificationFlags = _target.ReadGlobalPointer(Constants.Globals.DacNotificationFlags);
                _target.Write<uint>(dacNotificationFlags, flags);
            }
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
#if DEBUG
        if (_legacyProcess is not null)
        {
            int hrLocal = default;
            uint flagsLocal = default;
            // have to read the flags like this and not with GetOtherNotificationFlags
            // because the legacy DAC cache will not be updated when we set the flags in cDAC
            // so we need to verify without using the legacy DAC
            hrLocal = HResults.S_OK;
            try
            {
                flagsLocal = _target.Read<uint>(_target.ReadGlobalPointer(Constants.Globals.DacNotificationFlags));
            }
            catch (System.Exception ex)
            {
                hrLocal = ex.HResult;
            }
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
            {
                Debug.Assert(flags == flagsLocal);
            }
            // update the DAC cache
            _legacyProcess.SetOtherNotificationFlags(flags);
        }
#endif
        return hr;
    }

    int IXCLRDataProcess.StartEnumMethodDefinitionsByAddress(ClrDataAddress address, ulong* handle)
        => LegacyFallbackHelper.CanFallback() && _legacyProcess is not null ? _legacyProcess.StartEnumMethodDefinitionsByAddress(address, handle) : HResults.E_NOTIMPL;

    int IXCLRDataProcess.EnumMethodDefinitionByAddress(ulong* handle, DacComNullableByRef<IXCLRDataMethodDefinition> method)
        => LegacyFallbackHelper.CanFallback() && _legacyProcess is not null ? _legacyProcess.EnumMethodDefinitionByAddress(handle, method) : HResults.E_NOTIMPL;

    int IXCLRDataProcess.EndEnumMethodDefinitionsByAddress(ulong handle)
        => LegacyFallbackHelper.CanFallback() && _legacyProcess is not null ? _legacyProcess.EndEnumMethodDefinitionsByAddress(handle) : HResults.E_NOTIMPL;

    int IXCLRDataProcess.FollowStub(
        uint inFlags,
        ClrDataAddress inAddr,
        /*struct CLRDATA_FOLLOW_STUB_BUFFER*/ void* inBuffer,
        ClrDataAddress* outAddr,
        /*struct CLRDATA_FOLLOW_STUB_BUFFER*/ void* outBuffer,
        uint* outFlags)
        => LegacyFallbackHelper.CanFallback() && _legacyProcess is not null ? _legacyProcess.FollowStub(inFlags, inAddr, inBuffer, outAddr, outBuffer, outFlags) : HResults.E_NOTIMPL;

    int IXCLRDataProcess.FollowStub2(
        IXCLRDataTask? task,
        uint inFlags,
        ClrDataAddress inAddr,
        /*struct CLRDATA_FOLLOW_STUB_BUFFER*/ void* inBuffer,
        ClrDataAddress* outAddr,
        /*struct CLRDATA_FOLLOW_STUB_BUFFER*/ void* outBuffer,
        uint* outFlags)
        => LegacyFallbackHelper.CanFallback() && _legacyProcess is not null ? _legacyProcess.FollowStub2(task, inFlags, inAddr, inBuffer, outAddr, outBuffer, outFlags) : HResults.E_NOTIMPL;

    int IXCLRDataProcess.DumpNativeImage(
        ClrDataAddress loadedBase,
        char* name,
        /*IXCLRDataDisplay*/ void* display,
        /*IXCLRLibrarySupport*/ void* libSupport,
        /*IXCLRDisassemblySupport*/ void* dis)
        => LegacyFallbackHelper.CanFallback() && _legacyProcess is not null ? _legacyProcess.DumpNativeImage(loadedBase, name, display, libSupport, dis) : HResults.E_NOTIMPL;

    int IXCLRDataProcess2.GetGcNotification(GcEvtArgs* gcEvtArgs)
    {
        int hr = HResults.E_NOTIMPL;
#if DEBUG
        if (_legacyProcess2 is not null)
        {
            int hrLocal = _legacyProcess2.GetGcNotification(gcEvtArgs);
            Debug.ValidateHResult(hr, hrLocal);
        }
#endif
        return hr;
    }

    int IXCLRDataProcess2.SetGcNotification(GcEvtArgs gcEvtArgs)
    {
        int hr = HResults.S_OK;
        try
        {
            if (gcEvtArgs.type >= GcEvtArgs.GcEvt_t.GC_EVENT_TYPE_MAX)
                throw new ArgumentException();
            _target.Contracts.Notifications.SetGcNotification(gcEvtArgs.condemnedGeneration);
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
#if DEBUG
        if (_legacyProcess2 is not null)
        {
            // update the DAC cache
            int hrLocal = _legacyProcess2.SetGcNotification(gcEvtArgs);
            Debug.ValidateHResult(hr, hrLocal);
        }
#endif
        return hr;
    }

    private static TargetPointer GetModuleAddress(void* comModulePtr)
    {
        if (System.Runtime.InteropServices.ComWrappers.TryGetObject((nint)comModulePtr, out object? obj))
        {
            if (obj is ClrDataModule cdm)
                return cdm.Address;
        }
        throw new ArgumentException("Could not resolve module address from COM pointer");
    }
}
