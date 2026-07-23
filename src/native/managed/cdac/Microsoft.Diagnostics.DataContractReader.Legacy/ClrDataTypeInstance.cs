// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using Microsoft.Diagnostics.DataContractReader.Contracts;

namespace Microsoft.Diagnostics.DataContractReader.Legacy;

[GeneratedComClass]
public sealed unsafe partial class ClrDataTypeInstance : IXCLRDataTypeInstance
{
    private readonly record struct StaticFieldData(TargetPointer FieldDesc, ITypeHandle EnclosingType, string Name, uint Token);

    private readonly Target _target;
    private readonly ITypeHandle _typeHandle;
    private readonly IXCLRDataTypeInstance? _legacyImpl;

    public ClrDataTypeInstance(Target target, ITypeHandle typeHandle, IXCLRDataTypeInstance? legacyImpl)
    {
        _target = target;
        _typeHandle = typeHandle;
        _legacyImpl = legacyImpl;
    }

    private static bool IsMethodTable(ITypeHandle typeHandle)
        => typeHandle.Address != TargetPointer.Null && (((ulong)typeHandle.Address & 2UL) == 0);

    int IXCLRDataTypeInstance.GetName(uint flags, uint bufLen, uint* nameLen, char* nameBuf)
    {
        int hr = HResults.S_OK;
        try
        {
            if (flags != 0)
                throw new ArgumentException();

            StringBuilder builder = new();
            TypeNameBuilder.AppendType(_target, builder, _typeHandle, TypeNameFormat.FormatNamespace | TypeNameFormat.FormatFullInst);
            string result = builder.ToString();
            OutputBufferHelpers.CopyStringToBuffer(nameBuf, bufLen, nameLen, result);
            if (nameBuf is not null && bufLen < result.Length + 1)
                hr = HResults.S_FALSE;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacyImpl is not null)
        {
            uint nameLenLocal;
            char[] nameLocal = new char[bufLen > 0 ? bufLen : 1];
            int hrLocal;
            fixed (char* pNameLocal = nameLocal)
                hrLocal = _legacyImpl.GetName(flags, bufLen, &nameLenLocal, nameBuf is null ? null : pNameLocal);
            Debug.ValidateHResult(hr, hrLocal);
        }
#endif
        return hr;
    }

    int IXCLRDataTypeInstance.GetModule(DacComNullableByRef<IXCLRDataModule> mod)
    {
        IXCLRDataModule? legacyModule = null;
        int hrLocal = HResults.S_OK;
        if (_legacyImpl is not null)
        {
            DacComNullableByRef<IXCLRDataModule> legacyOut = new(isNullRef: false);
            hrLocal = _legacyImpl.GetModule(legacyOut);
            legacyModule = legacyOut.Interface;
        }

        int hr = HResults.S_OK;
        try
        {
            TargetPointer module = _target.Contracts.RuntimeTypeSystem.GetModule(_typeHandle);
            mod.Interface = new ClrDataModule(module, _target, legacyModule);
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacyImpl is not null)
            Debug.ValidateHResult(hr, hrLocal);
#endif
        return hr;
    }

    int IXCLRDataTypeInstance.GetDefinition(DacComNullableByRef<IXCLRDataTypeDefinition> typeDefinition)
    {
        IXCLRDataTypeDefinition? legacyDefinition = null;
        int hrLocal = HResults.S_OK;
        if (_legacyImpl is not null)
        {
            DacComNullableByRef<IXCLRDataTypeDefinition> legacyOut = new(isNullRef: false);
            hrLocal = _legacyImpl.GetDefinition(legacyOut);
            legacyDefinition = legacyOut.Interface;
        }

        int hr = HResults.S_OK;
        try
        {
            IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;
            typeDefinition.Interface = new ClrDataTypeDefinition(_target, rts.GetModule(_typeHandle), rts.GetTypeDefToken(_typeHandle), legacyDefinition);
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacyImpl is not null)
            Debug.ValidateHResult(hr, hrLocal);
#endif
        return hr;
    }

    private MetadataReader GetMetadataReader(ITypeHandle typeHandle)
    {
        ILoader loader = _target.Contracts.Loader;
        Contracts.ModuleHandle module = loader.GetModuleHandleFromModulePtr(_target.Contracts.RuntimeTypeSystem.GetModule(typeHandle));
        return _target.Contracts.EcmaMetadata.GetMetadata(module)
            ?? throw new InvalidOperationException("Failed to get metadata reader");
    }

    private IEnumerable<MethodDescHandle> EnumerateMethodInstances()
    {
        IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;
        ILoader loader = _target.Contracts.Loader;
        MetadataReader reader = GetMetadataReader(_typeHandle);
        Contracts.ModuleHandle moduleHandle = loader.GetModuleHandleFromModulePtr(rts.GetModule(_typeHandle));
        TargetPointer methodDefToDesc = loader.GetLookupTables(moduleHandle).MethodDefToDesc;
        TypeDefinitionHandle typeDefinitionHandle = MetadataTokens.TypeDefinitionHandle((int)(rts.GetTypeDefToken(_typeHandle) & 0x00FFFFFF));

        foreach (MethodDefinitionHandle handle in reader.GetTypeDefinition(typeDefinitionHandle).GetMethods())
        {
            uint token = (uint)MetadataTokens.GetToken(handle);
            TargetPointer methodDescAddress = loader.GetModuleLookupMapElement(methodDefToDesc, token, out _);
            if (methodDescAddress == TargetPointer.Null)
                continue;

            MethodDescHandle methodDesc = rts.GetMethodDescHandle(methodDescAddress);
            if (rts.GetNativeCode(methodDesc) == TargetCodePointer.Null)
                continue;

            yield return methodDesc;
        }
    }

    private List<ITypeHandle> EnumerateTypeHierarchyBaseFirst()
    {
        IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;
        List<ITypeHandle> hierarchy = new();

        if (!IsMethodTable(_typeHandle))
            return hierarchy;

        for (ITypeHandle current = _typeHandle; ; )
        {
            hierarchy.Add(current);

            TargetPointer parent = rts.GetParentMethodTable(current);
            if (parent == TargetPointer.Null)
                break;

            current = rts.GetTypeHandle(parent);
        }

        hierarchy.Reverse();
        return hierarchy;
    }

    private IEnumerable<StaticFieldData> EnumerateStaticFields()
    {
        IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;
        ILoader loader = _target.Contracts.Loader;

        foreach (ITypeHandle current in EnumerateTypeHierarchyBaseFirst())
        {
            TargetPointer module = rts.GetModule(current);
            Contracts.ModuleHandle moduleHandle = loader.GetModuleHandleFromModulePtr(module);
            MetadataReader reader = _target.Contracts.EcmaMetadata.GetMetadata(moduleHandle)
                ?? throw new InvalidOperationException("Failed to get metadata reader");
            TargetPointer fieldDefToDesc = loader.GetLookupTables(moduleHandle).FieldDefToDesc;
            TypeDefinitionHandle typeDefinitionHandle = MetadataTokens.TypeDefinitionHandle((int)(rts.GetTypeDefToken(current) & 0x00FFFFFF));

            foreach (FieldDefinitionHandle handle in reader.GetTypeDefinition(typeDefinitionHandle).GetFields())
            {
                FieldDefinition definition = reader.GetFieldDefinition(handle);
                if ((definition.Attributes & FieldAttributes.Static) == 0)
                    continue;

                uint token = (uint)MetadataTokens.GetToken(handle);
                TargetPointer fieldDesc = loader.GetModuleLookupMapElement(fieldDefToDesc, token, out _);
                if (fieldDesc == TargetPointer.Null)
                    continue;

                yield return new(fieldDesc, current, reader.GetString(definition.Name), token);
            }
        }
    }

    private (uint Flags, ulong Size) GetStaticFieldValueInfo(TargetPointer fieldDesc)
    {
        IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;
        CorElementType elementType = rts.GetFieldDescType(fieldDesc);

        return elementType switch
        {
            CorElementType.Boolean or CorElementType.I1 or CorElementType.U1 => ((uint)ClrDataValueFlag.IS_PRIMITIVE, 1),
            CorElementType.Char or CorElementType.I2 or CorElementType.U2 => ((uint)ClrDataValueFlag.IS_PRIMITIVE, 2),
            CorElementType.I4 or CorElementType.U4 or CorElementType.R4 => ((uint)ClrDataValueFlag.IS_PRIMITIVE, 4),
            CorElementType.I8 or CorElementType.U8 or CorElementType.R8 => ((uint)ClrDataValueFlag.IS_PRIMITIVE, 8),
            CorElementType.I or CorElementType.U => ((uint)ClrDataValueFlag.IS_PRIMITIVE, (ulong)_target.PointerSize),
            CorElementType.Ptr or CorElementType.FnPtr => ((uint)ClrDataValueFlag.IS_POINTER, (ulong)_target.PointerSize),
            CorElementType.String => ((uint)(ClrDataValueFlag.IS_REFERENCE | ClrDataValueFlag.IS_STRING), (ulong)_target.PointerSize),
            CorElementType.Array or CorElementType.SzArray => ((uint)(ClrDataValueFlag.IS_REFERENCE | ClrDataValueFlag.IS_ARRAY), (ulong)_target.PointerSize),
            CorElementType.Class or CorElementType.Object => ((uint)ClrDataValueFlag.IS_REFERENCE, (ulong)_target.PointerSize),
            CorElementType.ValueType => GetValueTypeValueInfo(fieldDesc),
            _ => throw new InvalidOperationException($"Unsupported field element type {elementType}"),
        };
    }

    private (uint Flags, ulong Size) GetValueTypeValueInfo(TargetPointer fieldDesc)
    {
        IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;
        ITypeHandle type = rts.GetFieldDescApproxTypeHandle(fieldDesc)
            ?? throw new InvalidOperationException("Failed to get field type");

        uint flags = (uint)ClrDataValueFlag.IS_VALUE_TYPE;
        if (rts.IsEnum(type))
            flags |= (uint)ClrDataValueFlag.IS_ENUM;

        return (flags, rts.GetNumInstanceFieldBytes(type));
    }

    int IXCLRDataTypeInstance.StartEnumMethodInstances(ulong* handle)
    {
        int hr = HResults.S_OK;
        ulong legacyHandle = 0;
        int hrLocal = HResults.S_OK;

        if (_legacyImpl is not null)
            hrLocal = _legacyImpl.StartEnumMethodInstances(&legacyHandle);

        try
        {
            if (handle is null)
                throw new NullReferenceException();

            *handle = 0;
            if (!IsMethodTable(_typeHandle))
            {
                hr = HResults.S_FALSE;
            }
            else
            {
                ClrDataModule.ModuleEnumeration<MethodDescHandle> enumeration = new(EnumerateMethodInstances(), (nuint)legacyHandle);
                *handle = (ulong)((IEnum<MethodDescHandle>)enumeration).GetHandle();
                legacyHandle = 0;
            }
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
        finally
        {
            if (_legacyImpl is not null && legacyHandle != 0)
                _legacyImpl.EndEnumMethodInstances(legacyHandle);
        }

#if DEBUG
        if (_legacyImpl is not null)
            Debug.ValidateHResult(hr, hrLocal);
#endif
        return hr;
    }

    int IXCLRDataTypeInstance.EnumMethodInstance(ulong* handle, DacComNullableByRef<IXCLRDataMethodInstance> methodInstance)
    {
        int hr = HResults.S_OK;
        ClrDataModule.ModuleEnumeration<MethodDescHandle> enumeration;

        try
        {
            if (handle is null || methodInstance.IsNullRef)
                throw new NullReferenceException();
            if (*handle == 0)
                return HResults.S_FALSE;

            GCHandle gcHandle = GCHandle.FromIntPtr((IntPtr)(*handle));
            if (gcHandle.Target is not ClrDataModule.ModuleEnumeration<MethodDescHandle> currentEnumeration)
                throw new ArgumentException();

            enumeration = currentEnumeration;
        }
        catch (System.Exception ex)
        {
            return ex.HResult;
        }

        IXCLRDataMethodInstance? legacyMethod = null;
        int hrLocal = HResults.S_OK;
        if (_legacyImpl is not null)
        {
            ulong legacyHandle = enumeration.LegacyHandle;
            DacComNullableByRef<IXCLRDataMethodInstance> legacyOut = new(isNullRef: false);
            hrLocal = _legacyImpl.EnumMethodInstance(&legacyHandle, legacyOut);
            legacyMethod = legacyOut.Interface;
            enumeration.LegacyHandle = (nuint)legacyHandle;
        }

        try
        {
            if (enumeration.Enumerator.MoveNext())
                methodInstance.Interface = new ClrDataMethodInstance(_target, enumeration.Enumerator.Current, TargetPointer.Null, legacyMethod);
            else
                hr = HResults.S_FALSE;
        }
        catch (System.Exception ex)
        {
            if (_legacyImpl is not null)
            {
                hr = hrLocal;
                methodInstance.Interface = legacyMethod;
            }
            else
            {
                hr = ex.HResult;
            }
        }

#if DEBUG
        if (_legacyImpl is not null)
            Debug.ValidateHResult(hr, hrLocal);
#endif
        return hr;
    }

    int IXCLRDataTypeInstance.EndEnumMethodInstances(ulong handle)
    {
        int hr = HResults.S_OK;

        try
        {
            if (handle == 0)
                throw new ArgumentException();

            GCHandle gcHandle = GCHandle.FromIntPtr((IntPtr)handle);
            if (gcHandle.Target is not ClrDataModule.ModuleEnumeration<MethodDescHandle> enumeration)
                throw new ArgumentException();

            ((IEnum<MethodDescHandle>)enumeration).Dispose();
            gcHandle.Free();

            if (_legacyImpl is not null && enumeration.LegacyHandle != 0)
            {
                int hrLocal = _legacyImpl.EndEnumMethodInstances((ulong)enumeration.LegacyHandle);
                if (hrLocal < 0)
                    hr = hrLocal;
            }
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

        return hr;
    }
    int IXCLRDataTypeInstance.StartEnumMethodInstancesByName(char* name, uint flags, ulong* handle) => _legacyImpl?.StartEnumMethodInstancesByName(name, flags, handle) ?? HResults.E_NOTIMPL;
    int IXCLRDataTypeInstance.EnumMethodInstanceByName(ulong* handle, DacComNullableByRef<IXCLRDataMethodInstance> method) => _legacyImpl?.EnumMethodInstanceByName(handle, method) ?? HResults.E_NOTIMPL;
    int IXCLRDataTypeInstance.EndEnumMethodInstancesByName(ulong handle) => _legacyImpl?.EndEnumMethodInstancesByName(handle) ?? HResults.E_NOTIMPL;
    int IXCLRDataTypeInstance.GetNumStaticFields(uint* numFields)
    {
        int hr = HResults.S_OK;
        try
        {
            if (numFields is null)
                throw new NullReferenceException();
            if (!IsMethodTable(_typeHandle))
                throw new ArgumentException();

            uint count = 0;
            foreach (ITypeHandle current in EnumerateTypeHierarchyBaseFirst())
                count += _target.Contracts.RuntimeTypeSystem.GetNumStaticFields(current);

            *numFields = count;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacyImpl is not null)
        {
            uint numFieldsLocal = 0;
            int hrLocal = _legacyImpl.GetNumStaticFields(&numFieldsLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr >= 0 && numFields is not null)
                Debug.Assert(*numFields == numFieldsLocal, $"cDAC: {*numFields}, DAC: {numFieldsLocal}");
        }
#endif
        return hr;
    }

    int IXCLRDataTypeInstance.GetStaticFieldByIndex(uint index, IXCLRDataTask? tlsTask, DacComNullableByRef<IXCLRDataValue> field, uint bufLen, uint* nameLen, char* nameBuf, uint* token)
    {
        int hr = HResults.S_OK;
        IXCLRDataValue? legacyValue = null;
        int hrLocal = HResults.S_OK;
        if (_legacyImpl is not null)
        {
            DacComNullableByRef<IXCLRDataValue> legacyOut = new(isNullRef: false);
            hrLocal = _legacyImpl.GetStaticFieldByIndex(index, tlsTask, legacyOut, 0, null, null, null);
            legacyValue = legacyOut.Interface;
        }

        try
        {
            if (field.IsNullRef)
                throw new NullReferenceException();
            if (!IsMethodTable(_typeHandle))
                throw new ArgumentException();

            uint currentIndex = 0;
            foreach (StaticFieldData staticField in EnumerateStaticFields())
            {
                if (currentIndex++ != index)
                    continue;

                OutputBufferHelpers.CopyStringToBuffer(nameBuf, bufLen, nameLen, staticField.Name, out bool truncated);
                if (token is not null)
                    *token = staticField.Token;
                if (truncated)
                    return HResults.S_FALSE;

                IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;
                (uint valueFlags, ulong size) = GetStaticFieldValueInfo(staticField.FieldDesc);
                if (rts.ContainsGenericVariables(staticField.EnclosingType))
                {
                    field.Interface = new ClrDataValue(_target, valueFlags, Array.Empty<NativeVarLocation>(), legacyValue);
                    return HResults.S_OK;
                }

                TargetPointer thread = tlsTask is ClrDataTask task ? task.Address : TargetPointer.Null;
                TargetPointer address;
                if (rts.IsFieldDescThreadStatic(staticField.FieldDesc))
                {
                    if (thread == TargetPointer.Null)
                        throw new ArgumentException();

                    address = rts.GetFieldDescThreadStaticAddress(staticField.FieldDesc, thread);
                }
                else
                {
                    address = rts.GetFieldDescStaticAddress(staticField.FieldDesc);
                }

                NativeVarLocation location = new()
                {
                    AddressOrValue = address.ToClrDataAddress(_target),
                    Size = size,
                    IsRegisterValue = false,
                };
                field.Interface = new ClrDataValue(_target, valueFlags, [location], legacyValue);
                return HResults.S_OK;
            }

            hr = HResults.E_INVALIDARG;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacyImpl is not null)
            Debug.ValidateHResult(hr, hrLocal);
#endif
        return hr;
    }
    int IXCLRDataTypeInstance.StartEnumStaticFieldsByName(char* name, uint flags, IXCLRDataTask? tlsTask, ulong* handle) => _legacyImpl?.StartEnumStaticFieldsByName(name, flags, tlsTask, handle) ?? HResults.E_NOTIMPL;
    int IXCLRDataTypeInstance.EnumStaticFieldByName(ulong* handle, DacComNullableByRef<IXCLRDataValue> value) => _legacyImpl?.EnumStaticFieldByName(handle, value) ?? HResults.E_NOTIMPL;
    int IXCLRDataTypeInstance.EndEnumStaticFieldsByName(ulong handle) => _legacyImpl?.EndEnumStaticFieldsByName(handle) ?? HResults.E_NOTIMPL;
    int IXCLRDataTypeInstance.GetNumTypeArguments(uint* numTypeArgs) => _legacyImpl?.GetNumTypeArguments(numTypeArgs) ?? HResults.E_NOTIMPL;
    int IXCLRDataTypeInstance.GetTypeArgumentByIndex(uint index, DacComNullableByRef<IXCLRDataTypeInstance> typeArg) => _legacyImpl?.GetTypeArgumentByIndex(index, typeArg) ?? HResults.E_NOTIMPL;
    int IXCLRDataTypeInstance.GetFlags(uint* flags) => _legacyImpl?.GetFlags(flags) ?? HResults.E_NOTIMPL;
    int IXCLRDataTypeInstance.IsSameObject(IXCLRDataTypeInstance? type)
        => type is ClrDataTypeInstance other && other._typeHandle.Address == _typeHandle.Address
            ? HResults.S_OK
            : HResults.S_FALSE;
    int IXCLRDataTypeInstance.Request(uint reqCode, uint inBufferSize, byte* inBuffer, uint outBufferSize, byte* outBuffer) => _legacyImpl?.Request(reqCode, inBufferSize, inBuffer, outBufferSize, outBuffer) ?? HResults.E_NOTIMPL;
    int IXCLRDataTypeInstance.GetNumStaticFields2(uint flags, uint* numFields) => _legacyImpl?.GetNumStaticFields2(flags, numFields) ?? HResults.E_NOTIMPL;
    int IXCLRDataTypeInstance.StartEnumStaticFields(uint flags, IXCLRDataTask? tlsTask, ulong* handle) => _legacyImpl?.StartEnumStaticFields(flags, tlsTask, handle) ?? HResults.E_NOTIMPL;
    int IXCLRDataTypeInstance.EnumStaticField(ulong* handle, DacComNullableByRef<IXCLRDataValue> value) => _legacyImpl?.EnumStaticField(handle, value) ?? HResults.E_NOTIMPL;
    int IXCLRDataTypeInstance.EndEnumStaticFields(ulong handle) => _legacyImpl?.EndEnumStaticFields(handle) ?? HResults.E_NOTIMPL;
    int IXCLRDataTypeInstance.StartEnumStaticFieldsByName2(char* name, uint nameFlags, uint fieldFlags, IXCLRDataTask? tlsTask, ulong* handle) => _legacyImpl?.StartEnumStaticFieldsByName2(name, nameFlags, fieldFlags, tlsTask, handle) ?? HResults.E_NOTIMPL;
    int IXCLRDataTypeInstance.EnumStaticFieldByName2(ulong* handle, DacComNullableByRef<IXCLRDataValue> value) => _legacyImpl?.EnumStaticFieldByName2(handle, value) ?? HResults.E_NOTIMPL;
    int IXCLRDataTypeInstance.EndEnumStaticFieldsByName2(ulong handle) => _legacyImpl?.EndEnumStaticFieldsByName2(handle) ?? HResults.E_NOTIMPL;
    int IXCLRDataTypeInstance.GetStaticFieldByToken(uint token, IXCLRDataTask? tlsTask, DacComNullableByRef<IXCLRDataValue> field, uint bufLen, uint* nameLen, char* nameBuf) => _legacyImpl?.GetStaticFieldByToken(token, tlsTask, field, bufLen, nameLen, nameBuf) ?? HResults.E_NOTIMPL;
    int IXCLRDataTypeInstance.GetBase(DacComNullableByRef<IXCLRDataTypeInstance> @base) => _legacyImpl?.GetBase(@base) ?? HResults.E_NOTIMPL;
    int IXCLRDataTypeInstance.EnumStaticField2(ulong* handle, DacComNullableByRef<IXCLRDataValue> value, uint bufLen, uint* nameLen, char* nameBuf, DacComNullableByRef<IXCLRDataModule> tokenScope, uint* token) => _legacyImpl?.EnumStaticField2(handle, value, bufLen, nameLen, nameBuf, tokenScope, token) ?? HResults.E_NOTIMPL;
    int IXCLRDataTypeInstance.EnumStaticFieldByName3(ulong* handle, DacComNullableByRef<IXCLRDataValue> value, DacComNullableByRef<IXCLRDataModule> tokenScope, uint* token) => _legacyImpl?.EnumStaticFieldByName3(handle, value, tokenScope, token) ?? HResults.E_NOTIMPL;
    int IXCLRDataTypeInstance.GetStaticFieldByToken2(IXCLRDataModule? tokenScope, uint token, IXCLRDataTask? tlsTask, DacComNullableByRef<IXCLRDataValue> field, uint bufLen, uint* nameLen, char* nameBuf) => _legacyImpl?.GetStaticFieldByToken2(tokenScope, token, tlsTask, field, bufLen, nameLen, nameBuf) ?? HResults.E_NOTIMPL;
}
