// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Microsoft.Diagnostics.DataContractReader.Legacy;

[GeneratedComClass]
internal sealed unsafe partial class ClrDataModule : IXCLRDataModule
{
    private readonly Target _target;
    private readonly IXCLRDataModule? _legacyModule;

    public ClrDataModule(Target target, IXCLRDataModule? legacyModule)
    {
        _target = target;
        _legacyModule = legacyModule;
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
        => _legacyModule is not null ? _legacyModule.GetFileName(bufLen, nameLen, name) : HResults.E_NOTIMPL;

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
}
