// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices.Marshalling;

namespace Microsoft.Diagnostics.DataContractReader.Legacy;

[GeneratedComClass]
public sealed unsafe partial class ClrDataTypeDefinition : IXCLRDataTypeDefinition
{
    private readonly Target _target;

    public ClrDataTypeDefinition(Target target)
    {
        _target = target;
    }

    int IXCLRDataTypeDefinition.GetModule(DacComNullableByRef<IXCLRDataModule> mod)
        => HResults.E_NOTIMPL;

    int IXCLRDataTypeDefinition.StartEnumMethodDefinitions(ulong* handle)
        => HResults.E_NOTIMPL;

    int IXCLRDataTypeDefinition.EnumMethodDefinition(ulong* handle, DacComNullableByRef<IXCLRDataMethodDefinition> methodDefinition)
        => HResults.E_NOTIMPL;

    int IXCLRDataTypeDefinition.EndEnumMethodDefinitions(ulong handle)
        => HResults.E_NOTIMPL;

    int IXCLRDataTypeDefinition.StartEnumMethodDefinitionsByName(char* name, uint flags, ulong* handle)
        => HResults.E_NOTIMPL;

    int IXCLRDataTypeDefinition.EnumMethodDefinitionByName(ulong* handle, DacComNullableByRef<IXCLRDataMethodDefinition> method)
        => HResults.E_NOTIMPL;

    int IXCLRDataTypeDefinition.EndEnumMethodDefinitionsByName(ulong handle)
        => HResults.E_NOTIMPL;

    int IXCLRDataTypeDefinition.GetMethodDefinitionByToken(uint token, DacComNullableByRef<IXCLRDataMethodDefinition> methodDefinition)
        => HResults.E_NOTIMPL;

    int IXCLRDataTypeDefinition.StartEnumInstances(IXCLRDataAppDomain? appDomain, ulong* handle)
        => HResults.E_NOTIMPL;

    int IXCLRDataTypeDefinition.EnumInstance(ulong* handle, DacComNullableByRef<IXCLRDataTypeInstance> instance)
        => HResults.E_NOTIMPL;

    int IXCLRDataTypeDefinition.EndEnumInstances(ulong handle)
        => HResults.E_NOTIMPL;

    int IXCLRDataTypeDefinition.GetName(uint flags, uint bufLen, uint* nameLen, char* nameBuf)
        => HResults.E_NOTIMPL;

    int IXCLRDataTypeDefinition.GetTokenAndScope(uint* token, DacComNullableByRef<IXCLRDataModule> mod)
        => HResults.E_NOTIMPL;

    int IXCLRDataTypeDefinition.GetCorElementType(uint* type)
        => HResults.E_NOTIMPL;

    int IXCLRDataTypeDefinition.GetFlags(uint* flags)
        => HResults.E_NOTIMPL;

    int IXCLRDataTypeDefinition.IsSameObject(IXCLRDataTypeDefinition? type)
        => HResults.E_NOTIMPL;

    int IXCLRDataTypeDefinition.Request(uint reqCode, uint inBufferSize, byte* inBuffer, uint outBufferSize, byte* outBuffer)
        => HResults.E_NOTIMPL;

    int IXCLRDataTypeDefinition.GetArrayRank(uint* rank)
        => HResults.E_NOTIMPL;

    int IXCLRDataTypeDefinition.GetBase(DacComNullableByRef<IXCLRDataTypeDefinition> @base)
        => HResults.E_NOTIMPL;

    int IXCLRDataTypeDefinition.GetNumFields(uint flags, uint* numFields)
        => HResults.E_NOTIMPL;

    int IXCLRDataTypeDefinition.StartEnumFields(uint flags, ulong* handle)
        => HResults.E_NOTIMPL;

    int IXCLRDataTypeDefinition.EnumField(ulong* handle, uint nameBufLen, uint* nameLen, char* nameBuf, DacComNullableByRef<IXCLRDataTypeDefinition> type, uint* flags, uint* token)
        => HResults.E_NOTIMPL;

    int IXCLRDataTypeDefinition.EndEnumFields(ulong handle)
        => HResults.E_NOTIMPL;

    int IXCLRDataTypeDefinition.StartEnumFieldsByName(char* name, uint nameFlags, uint fieldFlags, ulong* handle)
        => HResults.E_NOTIMPL;

    int IXCLRDataTypeDefinition.EnumFieldByName(ulong* handle, DacComNullableByRef<IXCLRDataTypeDefinition> type, uint* flags, uint* token)
        => HResults.E_NOTIMPL;

    int IXCLRDataTypeDefinition.EndEnumFieldsByName(ulong handle)
        => HResults.E_NOTIMPL;

    int IXCLRDataTypeDefinition.GetFieldByToken(uint token, uint nameBufLen, uint* nameLen, char* nameBuf, DacComNullableByRef<IXCLRDataTypeDefinition> type, uint* flags)
        => HResults.E_NOTIMPL;

    int IXCLRDataTypeDefinition.GetTypeNotification(uint* flags)
        => HResults.E_NOTIMPL;

    int IXCLRDataTypeDefinition.SetTypeNotification(uint flags)
        => HResults.E_NOTIMPL;

    int IXCLRDataTypeDefinition.EnumField2(ulong* handle, uint nameBufLen, uint* nameLen, char* nameBuf, DacComNullableByRef<IXCLRDataTypeDefinition> type, uint* flags, DacComNullableByRef<IXCLRDataModule> tokenScope, uint* token)
        => HResults.E_NOTIMPL;

    int IXCLRDataTypeDefinition.EnumFieldByName2(ulong* handle, DacComNullableByRef<IXCLRDataTypeDefinition> type, uint* flags, DacComNullableByRef<IXCLRDataModule> tokenScope, uint* token)
        => HResults.E_NOTIMPL;

    int IXCLRDataTypeDefinition.GetFieldByToken2(IXCLRDataModule? tokenScope, uint token, uint nameBufLen, uint* nameLen, char* nameBuf, DacComNullableByRef<IXCLRDataTypeDefinition> type, uint* flags)
        => HResults.E_NOTIMPL;
}
