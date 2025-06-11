// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Microsoft.Diagnostics.DataContractReader.Contracts;

namespace Microsoft.Diagnostics.DataContractReader.Legacy;

[GeneratedComClass]
internal sealed unsafe partial class ClrDataModule : ICustomQueryInterface, IXCLRDataModule, IXCLRDataModule2
{
    private readonly TargetPointer _address;
    private readonly Target _target;

    private bool _extentsSet;
    private CLRDataModuleExtent[] _extents = new CLRDataModuleExtent[2];

#if DEBUG
    private ulong legacyExtentHandle;
#endif


    private readonly IXCLRDataModule? _legacyModule;
    private readonly IXCLRDataModule2? _legacyModule2;

    // This is an IUnknown pointer for the legacy implementation
    private readonly nint _legacyModulePointer;

    public ClrDataModule(TargetPointer address, Target target, IXCLRDataModule? legacyImpl)
    {
        _address = address;
        _target = target;
        _legacyModule = legacyImpl;
        _legacyModule2 = legacyImpl as IXCLRDataModule2;
        if (legacyImpl is not null && ComWrappers.TryGetComInstance(legacyImpl, out _legacyModulePointer))
        {
            // Release the AddRef from TryGetComInstance. We rely on the ref-count from holding on to IXCLRDataModule.
            Marshal.Release(_legacyModulePointer);
        }
    }

    private static readonly Guid IID_IMetaDataImport = Guid.Parse("7DAC8207-D3AE-4c75-9B67-92801A497D44");

    CustomQueryInterfaceResult ICustomQueryInterface.GetInterface(ref Guid iid, out nint ppv)
    {
        ppv = default;
        if (_legacyModulePointer == 0)
            return CustomQueryInterfaceResult.NotHandled;

        // Legacy DAC implementation of IXCLRDataModule handles QIs for IMetaDataImport by creating and
        // passing out an implementation of IMetaDataImport. Note that it does not do COM aggregation.
        // It simply returns a completely separate object. See ClrDataModule::QueryInterface in task.cpp
        if (iid == IID_IMetaDataImport && Marshal.QueryInterface(_legacyModulePointer, iid, out ppv) >= 0)
            return CustomQueryInterfaceResult.Handled;

        return CustomQueryInterfaceResult.NotHandled;
    }

    int IXCLRDataModule.StartEnumAssemblies(ulong* handle)
        => _legacyModule is not null ? _legacyModule.StartEnumAssemblies(handle) : HResults.E_NOTIMPL;
    int IXCLRDataModule.EnumAssembly(ulong* handle, /*IXCLRDataAssembly*/ void** assembly)
        => _legacyModule is not null ? _legacyModule.EnumAssembly(handle, assembly) : HResults.E_NOTIMPL;
    int IXCLRDataModule.EndEnumAssemblies(ulong handle)
        => _legacyModule is not null ? _legacyModule.EndEnumAssemblies(handle) : HResults.E_NOTIMPL;

    int IXCLRDataModule.StartEnumTypeDefinitions(ulong* handle)
        => _legacyModule is not null ? _legacyModule.StartEnumTypeDefinitions(handle) : HResults.E_NOTIMPL;
    int IXCLRDataModule.EnumTypeDefinition(ulong* handle, /*IXCLRDataTypeDefinition*/ void** typeDefinition)
        => _legacyModule is not null ? _legacyModule.EnumTypeDefinition(handle, typeDefinition) : HResults.E_NOTIMPL;
    int IXCLRDataModule.EndEnumTypeDefinitions(ulong handle)
        => _legacyModule is not null ? _legacyModule.EndEnumTypeDefinitions(handle) : HResults.E_NOTIMPL;

    int IXCLRDataModule.StartEnumTypeInstances(/*IXCLRDataAppDomain*/ void* appDomain, ulong* handle)
        => _legacyModule is not null ? _legacyModule.StartEnumTypeInstances(appDomain, handle) : HResults.E_NOTIMPL;
    int IXCLRDataModule.EnumTypeInstance(ulong* handle, /*IXCLRDataTypeInstance*/ void** typeInstance)
        => _legacyModule is not null ? _legacyModule.EnumTypeInstance(handle, typeInstance) : HResults.E_NOTIMPL;
    int IXCLRDataModule.EndEnumTypeInstances(ulong handle)
        => _legacyModule is not null ? _legacyModule.EndEnumTypeInstances(handle) : HResults.E_NOTIMPL;

    int IXCLRDataModule.StartEnumTypeDefinitionsByName(char* name, uint flags, ulong* handle)
        => _legacyModule is not null ? _legacyModule.StartEnumTypeDefinitionsByName(name, flags, handle) : HResults.E_NOTIMPL;
    int IXCLRDataModule.EnumTypeDefinitionByName(ulong* handle, /*IXCLRDataTypeDefinition*/ void** type)
        => _legacyModule is not null ? _legacyModule.EnumTypeDefinitionByName(handle, type) : HResults.E_NOTIMPL;
    int IXCLRDataModule.EndEnumTypeDefinitionsByName(ulong handle)
        => _legacyModule is not null ? _legacyModule.EndEnumTypeDefinitionsByName(handle) : HResults.E_NOTIMPL;

    int IXCLRDataModule.StartEnumTypeInstancesByName(char* name, uint flags, /*IXCLRDataAppDomain*/ void* appDomain, ulong* handle)
        => _legacyModule is not null ? _legacyModule.StartEnumTypeInstancesByName(name, flags, appDomain, handle) : HResults.E_NOTIMPL;
    int IXCLRDataModule.EnumTypeInstanceByName(ulong* handle, /*IXCLRDataTypeInstance*/ void** type)
        => _legacyModule is not null ? _legacyModule.EnumTypeInstanceByName(handle, type) : HResults.E_NOTIMPL;
    int IXCLRDataModule.EndEnumTypeInstancesByName(ulong handle)
        => _legacyModule is not null ? _legacyModule.EndEnumTypeInstancesByName(handle) : HResults.E_NOTIMPL;

    int IXCLRDataModule.GetTypeDefinitionByToken(/*mdTypeDef*/ uint token, /*IXCLRDataTypeDefinition*/ void** typeDefinition)
        => _legacyModule is not null ? _legacyModule.GetTypeDefinitionByToken(token, typeDefinition) : HResults.E_NOTIMPL;

    int IXCLRDataModule.StartEnumMethodDefinitionsByName(char* name, uint flags, ulong* handle)
        => _legacyModule is not null ? _legacyModule.StartEnumMethodDefinitionsByName(name, flags, handle) : HResults.E_NOTIMPL;
    int IXCLRDataModule.EnumMethodDefinitionByName(ulong* handle, /*IXCLRDataMethodDefinition*/ void** method)
        => _legacyModule is not null ? _legacyModule.EnumMethodDefinitionByName(handle, method) : HResults.E_NOTIMPL;
    int IXCLRDataModule.EndEnumMethodDefinitionsByName(ulong handle)
        => _legacyModule is not null ? _legacyModule.EndEnumMethodDefinitionsByName(handle) : HResults.E_NOTIMPL;

    int IXCLRDataModule.StartEnumMethodInstancesByName(char* name, uint flags, /*IXCLRDataAppDomain*/ void* appDomain, ulong* handle)
        => _legacyModule is not null ? _legacyModule.StartEnumMethodInstancesByName(name, flags, appDomain, handle) : HResults.E_NOTIMPL;
    int IXCLRDataModule.EnumMethodInstanceByName(ulong* handle, /*IXCLRDataMethodInstance*/ void** method)
        => _legacyModule is not null ? _legacyModule.EnumMethodInstanceByName(handle, method) : HResults.E_NOTIMPL;
    int IXCLRDataModule.EndEnumMethodInstancesByName(ulong handle)
        => _legacyModule is not null ? _legacyModule.EndEnumMethodInstancesByName(handle) : HResults.E_NOTIMPL;

    int IXCLRDataModule.GetMethodDefinitionByToken(/*mdMethodDef*/ uint token, /*IXCLRDataMethodDefinition*/ void** methodDefinition)
        => _legacyModule is not null ? _legacyModule.GetMethodDefinitionByToken(token, methodDefinition) : HResults.E_NOTIMPL;

    int IXCLRDataModule.StartEnumDataByName(char* name, uint flags, /*IXCLRDataAppDomain*/ void* appDomain, /*IXCLRDataTask*/ void* tlsTask, ulong* handle)
        => _legacyModule is not null ? _legacyModule.StartEnumDataByName(name, flags, appDomain, tlsTask, handle) : HResults.E_NOTIMPL;
    int IXCLRDataModule.EnumDataByName(ulong* handle, /*IXCLRDataValue*/ void** value)
        => _legacyModule is not null ? _legacyModule.EnumDataByName(handle, value) : HResults.E_NOTIMPL;
    int IXCLRDataModule.EndEnumDataByName(ulong handle)
        => _legacyModule is not null ? _legacyModule.EndEnumDataByName(handle) : HResults.E_NOTIMPL;

    int IXCLRDataModule.GetName(uint bufLen, uint* nameLen, char* name)
        => _legacyModule is not null ? _legacyModule.GetName(bufLen, nameLen, name) : HResults.E_NOTIMPL;
    int IXCLRDataModule.GetFileName(uint bufLen, uint* nameLen, char* name)
    {
        try
        {
            Contracts.ILoader contract = _target.Contracts.Loader;
            Contracts.ModuleHandle handle = contract.GetModuleHandle(_address);
            string result = string.Empty;
            try
            {
                result = contract.GetPath(handle);
            }
            catch (InvalidOperationException)
            {
                // The memory for the path may not be enumerated - for example, in triage dumps
                // In this case, GetPath will throw InvalidOperationException
            }

            if (string.IsNullOrEmpty(result))
            {
                result = contract.GetFileName(handle);
            }

            if (string.IsNullOrEmpty(result))
                return HResults.E_FAIL;

            OutputBufferHelpers.CopyStringToBuffer(name, bufLen, nameLen, result);
        }
        catch (System.Exception ex)
        {
            return ex.HResult;
        }

#if DEBUG
        if (_legacyModule is not null)
        {
            char[] nameLocal = new char[bufLen];
            uint nameLenLocal;
            int hrLocal;
            fixed (char* ptr = nameLocal)
            {
                hrLocal = _legacyModule.GetFileName(bufLen, &nameLenLocal, ptr);
            }
            Debug.Assert(hrLocal == HResults.S_OK);
            Debug.Assert(nameLen == null || *nameLen == nameLenLocal);
            Debug.Assert(name == null || new ReadOnlySpan<char>(nameLocal, 0, (int)nameLenLocal - 1).SequenceEqual(new string(name)));
        }
#endif
        return HResults.S_OK;
    }

    int IXCLRDataModule.GetFlags(uint* flags)
    {
        *flags = 0;
        try
        {
            Contracts.ILoader contract = _target.Contracts.Loader;
            Contracts.ModuleHandle handle = contract.GetModuleHandle(_address);

            ModuleFlags moduleFlags = contract.GetFlags(handle);
            if ((moduleFlags & ModuleFlags.ReflectionEmit) != 0)
            {
                *flags |= 0x1; // CLRDATA_MODULE_IS_DYNAMIC
            }

            if (contract.GetAssembly(handle) == contract.GetRootAssembly())
            {
                *flags |= 0x4; // CLRDATA_MODULE_FLAGS_ROOT_ASSEMBLY
            }
        }
        catch (System.Exception ex)
        {
            return ex.HResult;
        }

#if DEBUG
        if (_legacyModule is not null)
        {
            uint flagsLocal;
            int hrLocal = _legacyModule.GetFlags(&flagsLocal);
            Debug.Assert(hrLocal == HResults.S_OK, $"cDAC: {HResults.S_OK}, DAC: {hrLocal}");
            Debug.Assert(flagsLocal == *flags, $"cDAC: {*flags}, DAC: {flagsLocal}");
        }
#endif

        return HResults.S_OK;
    }

    int IXCLRDataModule.IsSameObject(IXCLRDataModule* mod)
        => _legacyModule is not null ? _legacyModule.IsSameObject(mod) : HResults.E_NOTIMPL;

    int IXCLRDataModule.StartEnumExtents(ulong* handle)
    {
        int hr = HResults.S_OK;
        try
        {
            if (!_extentsSet)
            {
                Contracts.ILoader contract = _target.Contracts.Loader;
                Contracts.ModuleHandle moduleHandle = contract.GetModuleHandle(_address);

                TargetPointer peAssembly = contract.GetPEAssembly(moduleHandle);
                if (peAssembly == 0)
                {
                    *handle = 0;
                    hr = HResults.E_INVALIDARG;
                }
                else
                {
                    if (contract.TryGetLoadedImageContents(moduleHandle, out TargetPointer baseAddress, out uint size, out _))
                    {
                        _extents[0].baseAddress = baseAddress.ToClrDataAddress(_target);
                        _extents[0].length = size;
                        _extents[0].type = 0x0; // CLRDATA_MODULE_PE_FILE
                    }
                    _extentsSet = true;
                }

                *handle = 1;
                hr = _extents[0].baseAddress != 0 ? HResults.S_OK : HResults.S_FALSE;
            }
        }
        catch (System.Exception ex)
        {
            return ex.HResult;
        }

#if DEBUG
        if (_legacyModule is not null)
        {
            ulong handleLocal = 0;
            int hrLocal = _legacyModule.StartEnumExtents(&handleLocal);
            legacyExtentHandle = handleLocal;
            Debug.Assert(hrLocal == HResults.S_OK, $"cDAC: {HResults.S_OK}, DAC: {hrLocal}");
        }
#endif

        return hr;
    }
    int IXCLRDataModule.EnumExtent(ulong* handle, /*CLRDATA_MODULE_EXTENT*/ void* extent)
    {
        int hr = HResults.S_OK;
        try
        {
            Contracts.ILoader contract = _target.Contracts.Loader;
            Contracts.ModuleHandle moduleHandle = contract.GetModuleHandle(_address);

            if (!_extentsSet)
            {
                hr = HResults.E_INVALIDARG;
            }
            else if (*handle == 1)
            {
                *handle += 1;
                CLRDataModuleExtent* dataModuleExtent = (CLRDataModuleExtent*)extent;
                dataModuleExtent->baseAddress = _extents[0].baseAddress;
                dataModuleExtent->length = _extents[0].length;
                dataModuleExtent->type = _extents[0].type;
            }
            else
            {
                hr = HResults.S_FALSE;
            }
        }
        catch (System.Exception ex)
        {
            return ex.HResult;
        }

#if DEBUG
        if (_legacyModule is not null)
        {
            ulong handleLocal = legacyExtentHandle;
            CLRDataModuleExtent dataModuleExtentLocal = default;
            int hrLocal = _legacyModule.EnumExtent(&handleLocal, &dataModuleExtentLocal);
            legacyExtentHandle = handleLocal;
            Debug.Assert(hr == hrLocal, $"cDAC: {hr}, DAC: {hrLocal}");
            CLRDataModuleExtent* dataModuleExtent = (CLRDataModuleExtent*)extent;
            Debug.Assert(dataModuleExtent->baseAddress == dataModuleExtentLocal.baseAddress, $"cDAC: {dataModuleExtent->baseAddress}, DAC: {dataModuleExtentLocal.baseAddress}");
            Debug.Assert(dataModuleExtent->length == dataModuleExtentLocal.length, $"cDAC: {dataModuleExtent->length}, DAC: {dataModuleExtentLocal.length}");
            Debug.Assert(dataModuleExtent->type == dataModuleExtentLocal.type, $"cDAC: {dataModuleExtent->type}, DAC: {dataModuleExtentLocal.type}");
        }
#endif

        return hr;
    }
    int IXCLRDataModule.EndEnumExtents(ulong handle)
    {
#if DEBUG
        if (_legacyModule is not null)
        {
            int hrLocal = _legacyModule.EndEnumExtents(handle);
            Debug.Assert(hrLocal == HResults.S_OK, $"cDAC: {HResults.S_OK}, DAC: {hrLocal}");
        }
#endif

        return HResults.S_OK;
    }

    int IXCLRDataModule.Request(uint reqCode, uint inBufferSize, byte* inBuffer, uint outBufferSize, byte* outBuffer)
    {
        return reqCode switch
        {
            0xf0000001 /*DACDATAMODULEPRIV_REQUEST_GET_MODULEDATA*/ => DacPrivateRequestGetModuleData(inBufferSize, inBuffer, outBufferSize, outBuffer),
            _ => _legacyModule is not null ? _legacyModule.Request(reqCode, inBufferSize, inBuffer, outBufferSize, outBuffer) : HResults.E_NOTIMPL
        };
    }

    private int DacPrivateRequestGetModuleData(uint inBufferSize, byte* inBuffer, uint outBufferSize, byte* outBuffer)
    {
        // Validate params.
        // Input: Nothing.
        // Output: a DacpGetModuleData structure.
        if (inBufferSize != 0 || inBuffer != null || outBufferSize != sizeof(DacpGetModuleData) || outBuffer == null)
            return HResults.E_INVALIDARG;

        // Cast outbuffer to DacpGetModuleData and zero it out
        DacpGetModuleData* getModuleData = (DacpGetModuleData*)outBuffer;
        Unsafe.InitBlock(getModuleData, 0, (uint)sizeof(DacpGetModuleData));

        Contracts.ILoader contract = _target.Contracts.Loader;
        Contracts.ModuleHandle moduleHandle = contract.GetModuleHandle(_address);
        TargetPointer peAssembly = contract.GetPEAssembly(moduleHandle);

        bool isReflectionEmit = (contract.GetFlags(moduleHandle) & ModuleFlags.ReflectionEmit) != 0;

        getModuleData->PEAssembly = _address.ToClrDataAddress(_target);
        getModuleData->IsDynamic = isReflectionEmit ? 1u : 0u;

        if (peAssembly != TargetPointer.Null)
        {
            // If isReflectionEmit or ProbeExtension is valid, the path is not valid.
            if (isReflectionEmit || contract.IsProbeExtensionResultValid(moduleHandle))
            {
                getModuleData->IsInMemory = 1u;
            }
            else
            {
                getModuleData->IsInMemory = contract.GetPath(moduleHandle).Length == 0 ? 1u : 0u;
            }

            contract.TryGetLoadedImageContents(moduleHandle, out TargetPointer baseAddress, out uint size, out uint flags);
            getModuleData->LoadedPEAddress = baseAddress.ToClrDataAddress(_target);
            getModuleData->LoadedPESize = size;

            // Can not get the assembly layout for a dynamic module
            if (getModuleData->IsDynamic == 0u)
            {
                getModuleData->IsFileLayout = ((flags & /*FLAG_CONTENTS*/0x2) != 0) && !((flags & /*FLAG_MAPPED*/0x1) != 0) ? 1u : 0u; // HasContents && !IsMapped
            }
        }

        if (contract.TryGetSymbolStream(moduleHandle, out TargetPointer symbolBuffer, out uint symbolBufferSize))
        {
            getModuleData->InMemoryPdbAddress = symbolBuffer.ToClrDataAddress(_target);
            getModuleData->InMemoryPdbSize = symbolBufferSize;
        }

#if DEBUG
        if (_legacyModule is not null)
        {
            DacpGetModuleData getModuleDataLocal = default;
            int hrLocal = _legacyModule.Request(
                0xf0000001 /*DACDATAMODULEPRIV_REQUEST_GET_MODULEDATA*/,
                0,
                null,
                (uint)sizeof(DacpGetModuleData),
                (byte*)&getModuleDataLocal);
            Debug.Assert(HResults.S_OK == hrLocal, $"cDAC: {HResults.S_OK}, DAC: {hrLocal}");

            Debug.Assert(getModuleDataLocal.IsDynamic == getModuleData->IsDynamic, $"cDAC: {getModuleData->IsDynamic}, DAC: {getModuleDataLocal.IsDynamic}");
            Debug.Assert(getModuleDataLocal.IsInMemory == getModuleData->IsInMemory, $"cDAC: {getModuleData->IsInMemory}, DAC: {getModuleDataLocal.IsInMemory}");
            Debug.Assert(getModuleDataLocal.IsFileLayout == getModuleData->IsFileLayout, $"cDAC: {getModuleData->IsFileLayout}, DAC: {getModuleDataLocal.IsFileLayout}");
            Debug.Assert(getModuleDataLocal.PEAssembly == getModuleData->PEAssembly, $"cDAC: {getModuleData->PEAssembly}, DAC: {getModuleDataLocal.PEAssembly}");
            Debug.Assert(getModuleDataLocal.LoadedPEAddress == getModuleData->LoadedPEAddress, $"cDAC: {getModuleData->LoadedPEAddress}, DAC: {getModuleDataLocal.LoadedPEAddress}");
            Debug.Assert(getModuleDataLocal.LoadedPESize == getModuleData->LoadedPESize, $"cDAC: {getModuleData->LoadedPESize}, DAC: {getModuleDataLocal.LoadedPESize}");
            Debug.Assert(getModuleDataLocal.InMemoryPdbAddress == getModuleData->InMemoryPdbAddress, $"cDAC: {getModuleData->InMemoryPdbAddress}, DAC: {getModuleDataLocal.InMemoryPdbAddress}");
            Debug.Assert(getModuleDataLocal.InMemoryPdbSize == getModuleData->InMemoryPdbSize, $"cDAC: {getModuleData->InMemoryPdbSize}, DAC: {getModuleDataLocal.InMemoryPdbSize}");
        }
#endif

        return HResults.S_OK;
    }

    int IXCLRDataModule.StartEnumAppDomains(ulong* handle)
        => _legacyModule is not null ? _legacyModule.StartEnumAppDomains(handle) : HResults.E_NOTIMPL;
    int IXCLRDataModule.EnumAppDomain(ulong* handle, /*IXCLRDataAppDomain*/ void** appDomain)
        => _legacyModule is not null ? _legacyModule.EnumAppDomain(handle, appDomain) : HResults.E_NOTIMPL;
    int IXCLRDataModule.EndEnumAppDomains(ulong handle)
        => _legacyModule is not null ? _legacyModule.EndEnumAppDomains(handle) : HResults.E_NOTIMPL;

    int IXCLRDataModule.GetVersionId(Guid* vid)
        => _legacyModule is not null ? _legacyModule.GetVersionId(vid) : HResults.E_NOTIMPL;

    int IXCLRDataModule2.SetJITCompilerFlags(uint flags)
        => _legacyModule2 is not null ? _legacyModule2.SetJITCompilerFlags(flags) : HResults.E_NOTIMPL;
}
