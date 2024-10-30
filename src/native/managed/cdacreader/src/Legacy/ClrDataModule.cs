// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Microsoft.Diagnostics.DataContractReader.Legacy;

[GeneratedComClass]
internal sealed unsafe partial class ClrDataModule : ICustomQueryInterface, IXCLRDataModule, IXCLRDataModule2
{
    private readonly TargetPointer _address;
    private readonly Target _target;
    private readonly nint _legacyModulePointer;
    private readonly IXCLRDataModule? _legacyModule;
    private readonly IXCLRDataModule2? _legacyModule2;

    public ClrDataModule(TargetPointer address, Target target, nint legacyModulePointer, object? legacyImpl)
    {
        _address = address;
        _target = target;
        _legacyModulePointer = legacyModulePointer;
        _legacyModule = legacyImpl as IXCLRDataModule;
        _legacyModule2 = legacyImpl as IXCLRDataModule2;
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
        => _legacyModule is not null ? _legacyModule.GetFlags(flags) : HResults.E_NOTIMPL;

    int IXCLRDataModule.IsSameObject(IXCLRDataModule* mod)
        => _legacyModule is not null ? _legacyModule.IsSameObject(mod) : HResults.E_NOTIMPL;

    int IXCLRDataModule.StartEnumExtents(ulong* handle)
        => _legacyModule is not null ? _legacyModule.StartEnumExtents(handle) : HResults.E_NOTIMPL;
    int IXCLRDataModule.EnumExtent(ulong* handle, /*CLRDATA_MODULE_EXTENT*/ void* extent)
        => _legacyModule is not null ? _legacyModule.EnumExtent(handle, extent) : HResults.E_NOTIMPL;
    int IXCLRDataModule.EndEnumExtents(ulong handle)
        => _legacyModule is not null ? _legacyModule.EndEnumExtents(handle) : HResults.E_NOTIMPL;

    int IXCLRDataModule.Request(uint reqCode, uint inBufferSize, byte* inBuffer, uint outBufferSize, byte* outBuffer)
        => _legacyModule is not null ? _legacyModule.Request(reqCode, inBufferSize, inBuffer, outBufferSize, outBuffer) : HResults.E_NOTIMPL;

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
