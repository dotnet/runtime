// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices.Marshalling;

namespace Microsoft.Diagnostics.DataContractReader.Legacy;

[GeneratedComClass]
public sealed unsafe partial class ClrDataTypeInstance : IXCLRDataTypeInstance
{
    private readonly Target _target;

    public ClrDataTypeInstance(Target target)
    {
        _target = target;
    }

    int IXCLRDataTypeInstance.StartEnumMethodInstances(ulong* handle)
        => HResults.E_NOTIMPL;

    int IXCLRDataTypeInstance.EnumMethodInstance(ulong* handle, DacComNullableByRef<IXCLRDataMethodInstance> methodInstance)
        => HResults.E_NOTIMPL;

    int IXCLRDataTypeInstance.EndEnumMethodInstances(ulong handle)
        => HResults.E_NOTIMPL;

    int IXCLRDataTypeInstance.StartEnumMethodInstancesByName(char* name, uint flags, ulong* handle)
        => HResults.E_NOTIMPL;

    int IXCLRDataTypeInstance.EnumMethodInstanceByName(ulong* handle, DacComNullableByRef<IXCLRDataMethodInstance> method)
        => HResults.E_NOTIMPL;

    int IXCLRDataTypeInstance.EndEnumMethodInstancesByName(ulong handle)
        => HResults.E_NOTIMPL;

    int IXCLRDataTypeInstance.GetNumStaticFields(uint* numFields)
        => HResults.E_NOTIMPL;

    int IXCLRDataTypeInstance.GetStaticFieldByIndex(uint index, IXCLRDataTask? tlsTask, DacComNullableByRef<IXCLRDataValue> field, uint bufLen, uint* nameLen, char* nameBuf, uint* token)
        => HResults.E_NOTIMPL;

    int IXCLRDataTypeInstance.StartEnumStaticFieldsByName(char* name, uint flags, IXCLRDataTask? tlsTask, ulong* handle)
        => HResults.E_NOTIMPL;

    int IXCLRDataTypeInstance.EnumStaticFieldByName(ulong* handle, DacComNullableByRef<IXCLRDataValue> value)
        => HResults.E_NOTIMPL;

    int IXCLRDataTypeInstance.EndEnumStaticFieldsByName(ulong handle)
        => HResults.E_NOTIMPL;

    int IXCLRDataTypeInstance.GetNumTypeArguments(uint* numTypeArgs)
        => HResults.E_NOTIMPL;

    int IXCLRDataTypeInstance.GetTypeArgumentByIndex(uint index, DacComNullableByRef<IXCLRDataTypeInstance> typeArg)
        => HResults.E_NOTIMPL;

    int IXCLRDataTypeInstance.GetName(uint flags, uint bufLen, uint* nameLen, char* nameBuf)
        => HResults.E_NOTIMPL;

    int IXCLRDataTypeInstance.GetModule(DacComNullableByRef<IXCLRDataModule> mod)
        => HResults.E_NOTIMPL;

    int IXCLRDataTypeInstance.GetDefinition(DacComNullableByRef<IXCLRDataTypeDefinition> typeDefinition)
        => HResults.E_NOTIMPL;

    int IXCLRDataTypeInstance.GetFlags(uint* flags)
        => HResults.E_NOTIMPL;

    int IXCLRDataTypeInstance.IsSameObject(IXCLRDataTypeInstance? type)
        => HResults.E_NOTIMPL;

    int IXCLRDataTypeInstance.Request(uint reqCode, uint inBufferSize, byte* inBuffer, uint outBufferSize, byte* outBuffer)
        => HResults.E_NOTIMPL;

    int IXCLRDataTypeInstance.GetNumStaticFields2(uint flags, uint* numFields)
        => HResults.E_NOTIMPL;

    int IXCLRDataTypeInstance.StartEnumStaticFields(uint flags, IXCLRDataTask? tlsTask, ulong* handle)
        => HResults.E_NOTIMPL;

    int IXCLRDataTypeInstance.EnumStaticField(ulong* handle, DacComNullableByRef<IXCLRDataValue> value)
        => HResults.E_NOTIMPL;

    int IXCLRDataTypeInstance.EndEnumStaticFields(ulong handle)
        => HResults.E_NOTIMPL;

    int IXCLRDataTypeInstance.StartEnumStaticFieldsByName2(char* name, uint nameFlags, uint fieldFlags, IXCLRDataTask? tlsTask, ulong* handle)
        => HResults.E_NOTIMPL;

    int IXCLRDataTypeInstance.EnumStaticFieldByName2(ulong* handle, DacComNullableByRef<IXCLRDataValue> value)
        => HResults.E_NOTIMPL;

    int IXCLRDataTypeInstance.EndEnumStaticFieldsByName2(ulong handle)
        => HResults.E_NOTIMPL;

    int IXCLRDataTypeInstance.GetStaticFieldByToken(uint token, IXCLRDataTask? tlsTask, DacComNullableByRef<IXCLRDataValue> field, uint bufLen, uint* nameLen, char* nameBuf)
        => HResults.E_NOTIMPL;

    int IXCLRDataTypeInstance.GetBase(DacComNullableByRef<IXCLRDataTypeInstance> @base)
        => HResults.E_NOTIMPL;

    int IXCLRDataTypeInstance.EnumStaticField2(ulong* handle, DacComNullableByRef<IXCLRDataValue> value, uint bufLen, uint* nameLen, char* nameBuf, DacComNullableByRef<IXCLRDataModule> tokenScope, uint* token)
        => HResults.E_NOTIMPL;

    int IXCLRDataTypeInstance.EnumStaticFieldByName3(ulong* handle, DacComNullableByRef<IXCLRDataValue> value, DacComNullableByRef<IXCLRDataModule> tokenScope, uint* token)
        => HResults.E_NOTIMPL;

    int IXCLRDataTypeInstance.GetStaticFieldByToken2(IXCLRDataModule? tokenScope, uint token, IXCLRDataTask? tlsTask, DacComNullableByRef<IXCLRDataValue> field, uint bufLen, uint* nameLen, char* nameBuf)
        => HResults.E_NOTIMPL;
}
