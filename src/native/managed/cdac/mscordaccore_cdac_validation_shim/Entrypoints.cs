// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Microsoft.Diagnostics.DataContractReader.Legacy;

/// <summary>
/// Per-<c>cdac_reader_init</c> state: the production cDAC's reader handle plus the callbacks the
/// caller supplied, kept alive so the interposed callbacks and the synthesized data targets stay
/// valid for as long as the caller holds the handle.
/// </summary>
internal sealed unsafe class ReaderHandle
{
    internal IntPtr ProductionHandle;
    internal ulong ContractDescriptor;
    internal ReaderCallbacks* Callbacks;
    internal ValidationSession Session = new();
}

/// <summary>
/// The shim's exported entry points. They mirror the production cDAC's exports exactly so a consumer
/// can load the shim wherever it would load the cDAC (via <c>DOTNET_CDAC_PATH</c>, for example) and
/// see the same ABI.
/// </summary>
internal static unsafe class Entrypoints
{
    private const string CDAC = "cdac_reader_";

    private static Guid s_processIid = typeof(IXCLRDataProcess).GUID;

    [UnmanagedCallersOnly(EntryPoint = $"{CDAC}init")]
    private static int Init(
        ulong descriptor,
        delegate* unmanaged<ulong, byte*, uint, void*, int> readFromTarget,
        delegate* unmanaged<ulong, byte*, uint, void*, int> writeToTarget,
        delegate* unmanaged<uint, uint, uint, byte*, void*, int> readThreadContext,
        delegate* unmanaged<uint, uint, byte*, void*, int> writeThreadContext,
        delegate* unmanaged<uint, ulong*, void*, int> allocVirtual,
        void* delegateContext,
        IntPtr* handle)
    {
        try
        {
            if (handle == null)
                return HResults.E_INVALIDARG;
            *handle = IntPtr.Zero;

            delegate* unmanaged<ulong, delegate* unmanaged<ulong, byte*, uint, void*, int>,
                delegate* unmanaged<ulong, byte*, uint, void*, int>,
                delegate* unmanaged<uint, uint, uint, byte*, void*, int>,
                delegate* unmanaged<uint, uint, byte*, void*, int>,
                delegate* unmanaged<uint, ulong*, void*, int>, void*, IntPtr*, int> productionInit =
                (delegate* unmanaged<ulong, delegate* unmanaged<ulong, byte*, uint, void*, int>,
                    delegate* unmanaged<ulong, byte*, uint, void*, int>,
                    delegate* unmanaged<uint, uint, uint, byte*, void*, int>,
                    delegate* unmanaged<uint, uint, byte*, void*, int>,
                    delegate* unmanaged<uint, ulong*, void*, int>, void*, IntPtr*, int>)
                NativeModules.GetExport(NativeModules.ProductionCDac, $"{CDAC}init");

            if (productionInit == null)
            {
                ShimLog.Error($"The production cDAC does not export {CDAC}init.");
                return HResults.E_FAIL;
            }

            // The caller's callbacks are interposed so that target mutations made by the production
            // cDAC are recorded and can be replayed to the legacy DAC instead of executed twice.
            ReaderCallbacks* callbacks = (ReaderCallbacks*)NativeMemory.AllocZeroed((nuint)sizeof(ReaderCallbacks));
            callbacks->ReadFromTarget = readFromTarget;
            callbacks->WriteToTarget = writeToTarget;
            callbacks->ReadThreadContext = readThreadContext;
            callbacks->WriteThreadContext = writeThreadContext;
            callbacks->AllocVirtual = allocVirtual;
            callbacks->Context = delegateContext;

            IntPtr productionHandle;
            int hr = productionInit(
                descriptor,
                readFromTarget is null ? null : &InterposedRead,
                writeToTarget is null ? null : &InterposedWrite,
                readThreadContext is null ? null : &InterposedReadThreadContext,
                writeThreadContext is null ? null : &InterposedWriteThreadContext,
                allocVirtual is null ? null : &InterposedAllocVirtual,
                callbacks,
                &productionHandle);

            if (hr < 0)
            {
                NativeMemory.Free(callbacks);
                return hr;
            }

            ReaderHandle readerHandle = new()
            {
                ProductionHandle = productionHandle,
                ContractDescriptor = descriptor,
                Callbacks = callbacks,
            };

            *handle = GCHandle.ToIntPtr(GCHandle.Alloc(readerHandle));
            return 0;
        }
        catch (Exception ex)
        {
            int hr = ex.HResult;
            return hr < 0 ? hr : HResults.E_FAIL;
        }
    }

    [UnmanagedCallersOnly]
    private static int InterposedRead(ulong address, byte* buffer, uint count, void* context)
    {
        ReaderCallbacks* callbacks = (ReaderCallbacks*)context;
        return callbacks->ReadFromTarget(address, buffer, count, callbacks->Context);
    }

    [UnmanagedCallersOnly]
    private static int InterposedWrite(ulong address, byte* buffer, uint count, void* context)
    {
        ReaderCallbacks* callbacks = (ReaderCallbacks*)context;
        int hr = callbacks->WriteToTarget(address, buffer, count, callbacks->Context);

        ShimCall.Current?.Record(new Mutation
        {
            Kind = MutationKind.WriteVirtual,
            Address = address,
            Data = buffer is null ? null : new ReadOnlySpan<byte>(buffer, checked((int)count)).ToArray(),
            Count = hr >= 0 ? count : 0,
            HResult = hr,
        });

        return hr;
    }

    [UnmanagedCallersOnly]
    private static int InterposedReadThreadContext(uint threadId, uint contextFlags, uint bufferSize, byte* buffer, void* context)
    {
        ReaderCallbacks* callbacks = (ReaderCallbacks*)context;
        return callbacks->ReadThreadContext(threadId, contextFlags, bufferSize, buffer, callbacks->Context);
    }

    [UnmanagedCallersOnly]
    private static int InterposedWriteThreadContext(uint threadId, uint contextSize, byte* buffer, void* context)
    {
        ReaderCallbacks* callbacks = (ReaderCallbacks*)context;
        int hr = callbacks->WriteThreadContext(threadId, contextSize, buffer, callbacks->Context);

        ShimCall.Current?.Record(new Mutation
        {
            Kind = MutationKind.SetThreadContext,
            ThreadId = threadId,
            Data = buffer is null ? null : new ReadOnlySpan<byte>(buffer, checked((int)contextSize)).ToArray(),
            HResult = hr,
        });

        return hr;
    }

    [UnmanagedCallersOnly]
    private static int InterposedAllocVirtual(uint size, ulong* address, void* context)
    {
        ReaderCallbacks* callbacks = (ReaderCallbacks*)context;
        int hr = callbacks->AllocVirtual(size, address, callbacks->Context);

        ShimCall.Current?.Record(new Mutation
        {
            Kind = MutationKind.AllocVirtual,
            Count = size,
            Value = address is null ? 0 : *address,
            HResult = hr,
        });

        return hr;
    }

    [UnmanagedCallersOnly(EntryPoint = $"{CDAC}free")]
    private static int Free(IntPtr handle)
    {
        try
        {
            GCHandle h = GCHandle.FromIntPtr(handle);
            if (h.Target is ReaderHandle readerHandle)
            {
                delegate* unmanaged<IntPtr, int> productionFree =
                    (delegate* unmanaged<IntPtr, int>)NativeModules.GetExport(NativeModules.ProductionCDac, $"{CDAC}free");
                if (productionFree != null && readerHandle.ProductionHandle != IntPtr.Zero)
                    productionFree(readerHandle.ProductionHandle);

                if (readerHandle.Callbacks != null)
                {
                    NativeMemory.Free(readerHandle.Callbacks);
                    readerHandle.Callbacks = null;
                }
            }

            h.Free();
            return 0;
        }
        catch (Exception ex)
        {
            int hr = ex.HResult;
            return hr < 0 ? hr : HResults.E_FAIL;
        }
    }

    /// <summary>
    /// Create the SOS-DAC interface implementation.
    /// </summary>
    [UnmanagedCallersOnly(EntryPoint = $"{CDAC}create_sos_interface")]
    private static int CreateSosInterface(IntPtr handle, nint* obj)
    {
        try
        {
            if (obj == null)
                return HResults.E_INVALIDARG;
            *obj = 0;

            if (GCHandle.FromIntPtr(handle).Target is not ReaderHandle readerHandle)
                return HResults.E_INVALIDARG;

            delegate* unmanaged<IntPtr, nint*, int> productionCreate =
                (delegate* unmanaged<IntPtr, nint*, int>)NativeModules.GetExport(NativeModules.ProductionCDac, $"{CDAC}create_sos_interface");
            if (productionCreate == null)
                return HResults.E_FAIL;

            nint cdacUnknown;
            int hr = productionCreate(readerHandle.ProductionHandle, &cdacUnknown);
            if (hr < 0)
                return hr;

            object cdacObject = ComInterfaceMarshaller<IXCLRDataProcess>.ConvertToManaged((void*)cdacUnknown)!;
            // The RCW took its own reference; release the one the create call transferred to us.
            ComInterfaceMarshaller<IXCLRDataProcess>.Free((void*)cdacUnknown);
            object? dacObject = CreateLegacySosFromReaderCallbacks(readerHandle);

            SOSDacImplProxy proxy = new(readerHandle.Session, cdacObject, dacObject);
            *obj = (nint)ComInterfaceMarshaller<IXCLRDataProcess>.ConvertToUnmanaged(proxy);
            return 0;
        }
        catch (Exception ex)
        {
            if (obj != null)
                *obj = 0;
            int hr = ex.HResult;
            return hr < 0 ? hr : HResults.E_FAIL;
        }
    }

    /// <summary>
    /// Create the DacDbi interface implementation.
    /// </summary>
    /// <remarks>
    /// The legacy DBI cannot be created from the reader callback ABI: <c>DacDbiInterfaceInstance</c>
    /// needs the runtime module base and an <c>ICorDebugDataTarget</c>, neither of which the callback
    /// ABI carries. This entry point therefore proxies the production cDAC without a comparison side.
    /// </remarks>
    [UnmanagedCallersOnly(EntryPoint = $"{CDAC}create_dacdbi_interface")]
    private static int CreateDacDbiInterface(IntPtr handle, nint* obj)
    {
        try
        {
            if (obj == null)
                return HResults.E_INVALIDARG;
            *obj = 0;

            if (GCHandle.FromIntPtr(handle).Target is not ReaderHandle readerHandle)
                return HResults.E_INVALIDARG;

            delegate* unmanaged<IntPtr, nint*, int> productionCreate =
                (delegate* unmanaged<IntPtr, nint*, int>)NativeModules.GetExport(NativeModules.ProductionCDac, $"{CDAC}create_dacdbi_interface");
            if (productionCreate == null)
                return HResults.E_FAIL;

            nint cdacUnknown;
            int hr = productionCreate(readerHandle.ProductionHandle, &cdacUnknown);
            if (hr < 0)
                return hr;

            object cdacObject = ComInterfaceMarshaller<IDacDbiInterface>.ConvertToManaged((void*)cdacUnknown)!;
            ComInterfaceMarshaller<IDacDbiInterface>.Free((void*)cdacUnknown);
            DacDbiProxy proxy = new(readerHandle.Session, cdacObject, dacObject: null);
            *obj = (nint)ComInterfaceMarshaller<IDacDbiInterface>.ConvertToUnmanaged(proxy);
            return HResults.S_OK;
        }
        catch (Exception ex)
        {
            if (obj != null)
                *obj = 0;
            int hr = ex.HResult;
            return hr < 0 ? hr : HResults.E_FAIL;
        }
    }

    // Same export name and signature as the DAC's CLRDataCreateInstance in daccess.cpp.
    [UnmanagedCallersOnly(EntryPoint = "CLRDataCreateInstance")]
    private static int CLRDataCreateInstance(Guid* pIID, IntPtr /*ICLRDataTarget*/ pLegacyTarget, void** iface)
    {
        if (pLegacyTarget == IntPtr.Zero || iface == null)
            return HResults.E_INVALIDARG;
        *iface = null;

        try
        {
            return CreateInstance(pIID, pLegacyTarget, contractDescriptor: 0, iface);
        }
        catch (Exception ex)
        {
            int hr = ex.HResult;
            return hr < 0 ? hr : HResults.E_FAIL;
        }
    }

    // Creates a data-access instance from an explicit contract descriptor address, so the data
    // target does not need to implement ICLRContractLocator.
    [UnmanagedCallersOnly(EntryPoint = "DbgShimCreateInstanceFromContractDescriptor")]
    private static int DbgShimCreateInstanceFromContractDescriptor(
        Guid* pIID,
        IntPtr /*ICLRDataTarget*/ pLegacyTarget,
        ulong contractDescriptorAddr,
        void** iface)
    {
        if (pLegacyTarget == IntPtr.Zero || contractDescriptorAddr == 0 || iface == null)
            return HResults.E_INVALIDARG;
        *iface = null;

        try
        {
            return CreateInstance(pIID, pLegacyTarget, contractDescriptorAddr, iface);
        }
        catch (Exception ex)
        {
            int hr = ex.HResult;
            return hr < 0 ? hr : HResults.E_FAIL;
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "DacDbiInterfaceInstance")]
    private static int DacDbiInterfaceInstance(
        IntPtr /*ICorDebugDataTarget*/ pTarget,
        ulong runtimeBase,
        ulong contractDescriptorAddress,
        IntPtr /*IDacDbiInterface::IAllocator*/ pAllocator,
        IntPtr /*IDacDbiInterface::IMetaDataLookup*/ pMetaDataLookup,
        void** iface)
    {
        if (pTarget == IntPtr.Zero
            || runtimeBase == 0
            || contractDescriptorAddress == 0
            || iface == null)
        {
            return HResults.E_INVALIDARG;
        }

        *iface = null;

        try
        {
            delegate* unmanaged<IntPtr, ulong, ulong, IntPtr, IntPtr, void**, int> productionCreate =
                NativeModules.ProductionDacDbiInterfaceInstance;
            if (productionCreate == null)
            {
                ShimLog.Error("The production cDAC does not export DacDbiInterfaceInstance.");
                return HResults.E_FAIL;
            }

            object callerTarget = ComInterfaceMarshaller<ICorDebugDataTarget>.ConvertToManaged((void*)pTarget)!;

            RecordingCorDebugDataTarget recording = new(pTarget, callerTarget);
            nint recordingPtr = (nint)ComInterfaceMarshaller<ICorDebugDataTarget>.ConvertToUnmanaged(recording);

            void* cdacUnknown;
            int hr;
            try
            {
                hr = productionCreate(recordingPtr, runtimeBase, contractDescriptorAddress, pAllocator, pMetaDataLookup, &cdacUnknown);
            }
            finally
            {
                // The callee AddRefs the target it keeps; release the reference this call created.
                ComInterfaceMarshaller<ICorDebugDataTarget>.Free((void*)recordingPtr);
            }

            if (hr < 0)
                return hr;

            object cdacObject = ComInterfaceMarshaller<IDacDbiInterface>.ConvertToManaged(cdacUnknown)!;
            ComInterfaceMarshaller<IDacDbiInterface>.Free(cdacUnknown);

            object? dacObject = null;
            delegate* unmanaged<IntPtr, ulong, ulong, IntPtr, IntPtr, void**, int> legacyCreate =
                NativeModules.LegacyDacDbiInterfaceInstance;
            if (legacyCreate != null)
            {
                ReplayCorDebugDataTarget replay = new(pTarget, callerTarget);
                nint replayPtr = (nint)ComInterfaceMarshaller<ICorDebugDataTarget>.ConvertToUnmanaged(replay);

                void* dacUnknown;
                int hrLegacy;
                try
                {
                    hrLegacy = legacyCreate(replayPtr, runtimeBase, contractDescriptorAddress, pAllocator, pMetaDataLookup, &dacUnknown);
                }
                finally
                {
                    ComInterfaceMarshaller<ICorDebugDataTarget>.Free((void*)replayPtr);
                }

                if (hrLegacy >= 0)
                {
                    dacObject = ComInterfaceMarshaller<IDacDbiInterface>.ConvertToManaged(dacUnknown);
                    ComInterfaceMarshaller<IDacDbiInterface>.Free(dacUnknown);
                }
                else
                {
                    ShimLog.Error($"Legacy DacDbiInterfaceInstance failed with 0x{unchecked((uint)hrLegacy):X8}; running without DBI validation.");
                }
            }

            ValidationSession session = new();
            DacDbiProxy proxy = new(session, cdacObject, dacObject);
            *iface = ComInterfaceMarshaller<IDacDbiInterface>.ConvertToUnmanaged(proxy);
            return HResults.S_OK;
        }
        catch (Exception ex)
        {
            if (iface != null)
                *iface = null;
            int hr = ex.HResult;
            return hr < 0 ? hr : HResults.E_FAIL;
        }
    }

    /// <summary>
    /// Shared implementation of the two <c>ICLRDataTarget</c>-based creation exports.
    /// </summary>
    /// <remarks>
    /// The production cDAC object is always created first and defines the result. The legacy DAC is
    /// created over a replaying data target, so the two sides read the same memory but only the cDAC's
    /// writes and allocations actually reach the target.
    /// </remarks>
    private static int CreateInstance(Guid* pIID, IntPtr pLegacyTarget, ulong contractDescriptor, void** iface)
    {
        object callerTarget = ComInterfaceMarshaller<ICLRDataTarget>.ConvertToManaged((void*)pLegacyTarget)!;

        RecordingDataTarget recording = new(pLegacyTarget, callerTarget, contractDescriptor);
        nint recordingPtr = (nint)ComInterfaceMarshaller<ICLRDataTarget>.ConvertToUnmanaged(recording);

        void* cdacUnknown = null;
        int hr;
        try
        {
            if (contractDescriptor != 0)
            {
                delegate* unmanaged<Guid*, IntPtr, ulong, void**, int> create = NativeModules.ProductionDbgShimCreateInstanceFromContractDescriptor;
                if (create == null)
                {
                    ShimLog.Error("The production cDAC does not export DbgShimCreateInstanceFromContractDescriptor.");
                    return HResults.E_FAIL;
                }

                fixed (Guid* processIid = &s_processIid)
                {
                    hr = create(processIid, recordingPtr, contractDescriptor, &cdacUnknown);
                }
            }
            else
            {
                delegate* unmanaged<Guid*, IntPtr, void**, int> create = NativeModules.ProductionCLRDataCreateInstance;
                if (create == null)
                {
                    ShimLog.Error("The production cDAC does not export CLRDataCreateInstance.");
                    return HResults.E_FAIL;
                }

                fixed (Guid* processIid = &s_processIid)
                {
                    hr = create(processIid, recordingPtr, &cdacUnknown);
                }
            }
        }
        finally
        {
            // The created instance AddRefs the data target it keeps; release the reference this
            // call created so the recording target is not leaked.
            ComInterfaceMarshaller<ICLRDataTarget>.Free((void*)recordingPtr);
        }

        if (hr < 0)
            return hr;

        object cdacObject = ComInterfaceMarshaller<IXCLRDataProcess>.ConvertToManaged(cdacUnknown)!;
        ComInterfaceMarshaller<IXCLRDataProcess>.Free(cdacUnknown);
        object? dacObject = CreateLegacyProcess(pLegacyTarget, callerTarget, contractDescriptor);

        ValidationSession session = new();
        SOSDacImplProxy proxy = new(session, cdacObject, dacObject);

        void* ccw = ComInterfaceMarshaller<IXCLRDataProcess>.ConvertToUnmanaged(proxy);
        int hrQI = Marshal.QueryInterface((nint)ccw, *pIID, out nint ptrToIface);

        // QueryInterface added a reference; drop the one ConvertToUnmanaged handed us.
        ComInterfaceMarshaller<IXCLRDataProcess>.Free(ccw);

        if (hrQI < 0)
            return hrQI;

        *iface = (void*)ptrToIface;
        return 0;
    }

    private static object? CreateLegacyProcess(IntPtr callerTargetPointer, object callerTarget, ulong contractDescriptor)
    {
        delegate* unmanaged<Guid*, IntPtr, void**, int> legacyCreate = NativeModules.LegacyCLRDataCreateInstance;
        if (legacyCreate == null)
            return null;

        ReplayDataTarget replay = new(callerTargetPointer, callerTarget, contractDescriptor);
        nint replayPtr = (nint)ComInterfaceMarshaller<ICLRDataTarget>.ConvertToUnmanaged(replay);

        void* dacUnknown;
        int hr;
        try
        {
            fixed (Guid* processIid = &s_processIid)
            {
                hr = legacyCreate(processIid, replayPtr, &dacUnknown);
            }
        }
        finally
        {
            ComInterfaceMarshaller<ICLRDataTarget>.Free((void*)replayPtr);
        }

        if (hr < 0)
        {
            ShimLog.Error($"Legacy CLRDataCreateInstance failed with 0x{unchecked((uint)hr):X8}; running without validation.");
            return null;
        }

        object? dac = ComInterfaceMarshaller<IXCLRDataProcess>.ConvertToManaged(dacUnknown);
        ComInterfaceMarshaller<IXCLRDataProcess>.Free(dacUnknown);
        return dac;
    }

    /// <summary>
    /// Creates the legacy DAC's SOS interface over the reader callback ABI.
    /// </summary>
    /// <remarks>
    /// The synthesized target cannot answer <c>GetMachineType</c> and the other queries the callback
    /// ABI does not carry, so the legacy DAC usually refuses to initialize here. That is reported once
    /// and the entry point degrades to a pass-through of the production cDAC.
    /// </remarks>
    private static object? CreateLegacySosFromReaderCallbacks(ReaderHandle readerHandle)
    {
        delegate* unmanaged<Guid*, IntPtr, void**, int> legacyCreate = NativeModules.LegacyCLRDataCreateInstance;
        if (legacyCreate == null)
            return null;

        SynthesizedReaderDataTarget target = new(*readerHandle.Callbacks, readerHandle.ContractDescriptor, isRecording: false);
        nint targetPtr = (nint)ComInterfaceMarshaller<ICLRDataTarget>.ConvertToUnmanaged(target);

        void* dacUnknown;
        int hr;
        try
        {
            fixed (Guid* processIid = &s_processIid)
            {
                hr = legacyCreate(processIid, targetPtr, &dacUnknown);
            }
        }
        finally
        {
            ComInterfaceMarshaller<ICLRDataTarget>.Free((void*)targetPtr);
        }

        if (hr < 0)
        {
            ShimLog.Error(
                $"Legacy CLRDataCreateInstance over the reader callback ABI failed with 0x{unchecked((uint)hr):X8}. "
                + "The callback ABI cannot express every ICLRDataTarget operation, so cdac_reader_* runs without validation.");
            return null;
        }

        object? dac = ComInterfaceMarshaller<IXCLRDataProcess>.ConvertToManaged(dacUnknown);
        ComInterfaceMarshaller<IXCLRDataProcess>.Free(dacUnknown);
        return dac;
    }
}
