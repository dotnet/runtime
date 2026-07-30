// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using Microsoft.Diagnostics.DataContractReader.Contracts;

namespace Microsoft.Diagnostics.DataContractReader.Legacy;

[GeneratedComClass]
public sealed unsafe partial class ClrDataMethodDefinition : IXCLRDataMethodDefinition
{
    private readonly Target _target;
    private readonly TargetPointer _module;
    private readonly uint _token;
    public ClrDataMethodDefinition(
        Target target,
        TargetPointer module,
        uint token)
    {
        _target = target;
        _module = module;
        _token = token;
    }

    private TargetPointer TryResolveMethodDesc()
    {
        ILoader loader = _target.Contracts.Loader;
        Contracts.ModuleHandle moduleHandle = loader.GetModuleHandleFromModulePtr(_module);
        ModuleLookupTables tables = loader.GetLookupTables(moduleHandle);
        TargetPointer methodDescAddr = loader.GetModuleLookupMapElement(tables.MethodDefToDesc, _token, out _);

        return methodDescAddr;
    }

    private static bool HasClassInstantiation(Target target, MethodDescHandle md)
    {
        IRuntimeTypeSystem rts = target.Contracts.RuntimeTypeSystem;
        TargetPointer mtAddr = rts.GetMethodTable(md);
        ITypeHandle mt = rts.GetTypeHandle(mtAddr);

        return rts.GetInstantiation(mt).Length > 0;
    }

    private static bool HasMethodInstantiation(Target target, MethodDescHandle md)
    {
        IRuntimeTypeSystem rts = target.Contracts.RuntimeTypeSystem;
        if (rts.IsGenericMethodDefinition(md))
            return true;

        return rts.GetGenericMethodInstantiation(md).Length > 0;
    }

    private static bool HasClassOrMethodInstantiation(Target target, MethodDescHandle md)
    {
        return HasClassInstantiation(target, md) || HasMethodInstantiation(target, md);
    }

    private string GetFullMethodNameFromMetadata()
    {
        ILoader loader = _target.Contracts.Loader;
        Contracts.ModuleHandle moduleHandle = loader.GetModuleHandleFromModulePtr(_module);
        IEcmaMetadata ecmaMetadata = _target.Contracts.EcmaMetadata;
        MetadataReader reader = ecmaMetadata.GetMetadata(moduleHandle)
            ?? throw new InvalidOperationException("Failed to get metadata reader");

        int rowId = (int)(_token & 0x00FFFFFF);
        MethodDefinitionHandle methodDefHandle = MetadataTokens.MethodDefinitionHandle(rowId);
        MethodDefinition methodDef = reader.GetMethodDefinition(methodDefHandle);
        string methodName = reader.GetString(methodDef.Name);

        TypeDefinitionHandle typeDefHandle = methodDef.GetDeclaringType();
        if (typeDefHandle.IsNil)
            return methodName;

        TypeDefinition typeDef = reader.GetTypeDefinition(typeDefHandle);
        string typeName = reader.GetString(typeDef.Name);
        string namespaceName = reader.GetString(typeDef.Namespace);

        StringBuilder sb = new();
        if (!string.IsNullOrEmpty(namespaceName))
        {
            sb.Append(namespaceName);
            sb.Append('.');
        }
        sb.Append(typeName);
        sb.Append('.');
        sb.Append(methodName);

        return sb.ToString();
    }

    int IXCLRDataMethodDefinition.GetTypeDefinition(DacComNullableByRef<IXCLRDataTypeDefinition> typeDefinition)
        => HResults.E_NOTIMPL;

    int IXCLRDataMethodDefinition.StartEnumInstances(IXCLRDataAppDomain? appDomain, ulong* handle)
    {
        int hr = HResults.S_FALSE;
        *handle = 0;
        // which delegates some operations to it.
        ulong legacyHandle = default;

        try
        {
            TargetPointer methodDescAddr = TryResolveMethodDesc();
            if (methodDescAddr != TargetPointer.Null)
            {
                SOSDacImpl.EnumMethodInstances emi = new(_target, methodDescAddr, TargetPointer.Null);
                emi.LegacyHandle = (nuint)legacyHandle;

                hr = emi.Start();
                if (hr == HResults.S_OK)
                {
                    *handle = (ulong)((IEnum<MethodDescHandle>)emi).GetHandle();
                    // Legacy handle ownership transferred to emi — don't clean up below.
                    legacyHandle = default;
                }
            }
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
        finally
        {
            // that EnumInstance can advance both enumerations in lockstep. If the cDAC
            // side fails to produce an enum (no MethodDesc, exception, or emi.Start()
            // returns S_FALSE), the legacy handle would be orphaned because the caller
            // receives *handle == 0 and has no way to call End. Clean it up here.
        }


        return hr;
    }

    int IXCLRDataMethodDefinition.EnumInstance(ulong* handle, DacComNullableByRef<IXCLRDataMethodInstance> instance)
    {
        int hr = HResults.S_OK;

        if (*handle == 0)
            return HResults.S_FALSE;

        GCHandle gcHandle = GCHandle.FromIntPtr((IntPtr)(*handle));
        if (gcHandle.Target is not SOSDacImpl.EnumMethodInstances emi)
            return HResults.E_INVALIDARG;

        try
        {
            if (emi.Enumerator.MoveNext())
            {
                MethodDescHandle methodDesc = emi.Enumerator.Current;
                instance.Interface = new ClrDataMethodInstance(_target, methodDesc, emi._appDomain);
            }
            else
            {
                hr = HResults.S_FALSE;
            }
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }


        return hr;
    }

    int IXCLRDataMethodDefinition.EndEnumInstances(ulong handle)
    {
        int hr = HResults.S_OK;

        try
        {
            if (handle == 0)
                throw new ArgumentException();

            GCHandle gcHandle = GCHandle.FromIntPtr((IntPtr)handle);
            if (gcHandle.Target is not SOSDacImpl.EnumMethodInstances emi)
                throw new ArgumentException();

            ((IEnum<MethodDescHandle>)emi).Dispose();
            gcHandle.Free();

        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

        return hr;
    }

    int IXCLRDataMethodDefinition.GetName(uint flags, uint bufLen, uint* nameLen, char* name)
    {
        int hr = HResults.S_OK;

        try
        {
            if (flags != 0)
                throw new ArgumentException();

            StringBuilder sb = new();

            TargetPointer methodDescAddr = TryResolveMethodDesc();

            if (methodDescAddr != TargetPointer.Null)
            {
                IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;
                MethodDescHandle methodDescHandle = rts.GetMethodDescHandle(methodDescAddr);
                TypeNameBuilder.AppendMethodInternal(
                    _target,
                    sb,
                    methodDescHandle,
                    TypeNameFormat.FormatSignature |
                    TypeNameFormat.FormatNamespace |
                    TypeNameFormat.FormatFullInst);
            }
            else
            {
                sb.Append(GetFullMethodNameFromMetadata());
            }

            OutputBufferHelpers.CopyStringToBuffer(name, bufLen, nameLen, sb.ToString());

            if (name is not null && bufLen < (uint)(sb.Length + 1))
            {
                hr = HResults.S_FALSE;
            }
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }


        return hr;
    }

    int IXCLRDataMethodDefinition.GetTokenAndScope(uint* token, DacComNullableByRef<IXCLRDataModule> mod)
    {
        int hr = HResults.S_OK;
        try
        {
            if (token is not null)
            {
                *token = _token;
            }
            if (!mod.IsNullRef)
            {

                mod.Interface = new ClrDataModule(_module, _target);
            }
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }


        return hr;
    }

    int IXCLRDataMethodDefinition.GetFlags(uint* flags)
        => HResults.E_NOTIMPL;

    int IXCLRDataMethodDefinition.IsSameObject(IXCLRDataMethodDefinition? method)
        => HResults.E_NOTIMPL;

    int IXCLRDataMethodDefinition.GetLatestEnCVersion(uint* version)
        => HResults.E_NOTIMPL;

    int IXCLRDataMethodDefinition.StartEnumExtents(ulong* handle)
        => HResults.E_NOTIMPL;

    int IXCLRDataMethodDefinition.EnumExtent(ulong* handle, ClrDataMethodDefinitionExtent* extent)
        => HResults.E_NOTIMPL;

    int IXCLRDataMethodDefinition.EndEnumExtents(ulong handle)
        => HResults.E_NOTIMPL;

    int IXCLRDataMethodDefinition.GetCodeNotification(uint* flags)
    {
        int hr = HResults.S_OK;
        ICodeNotifications codeNotif = _target.Contracts.CodeNotifications;

        try
        {
            if (flags is null)
                throw new ArgumentNullException(nameof(flags));

            *flags = CodeNotificationFlagsConverter.ToCom(codeNotif.GetCodeNotification(_module, _token));
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }


        return hr;
    }

    int IXCLRDataMethodDefinition.SetCodeNotification(uint flags)
    {
        int hr = HResults.S_OK;
        ICodeNotifications codeNotif = _target.Contracts.CodeNotifications;

        try
        {
            if (!CodeNotificationFlagsConverter.IsValid(flags))
                throw new ArgumentException("Invalid code notification flags", nameof(flags));

            codeNotif.SetCodeNotification(_module, _token, CodeNotificationFlagsConverter.FromCom(flags));
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }


        return hr;
    }

    int IXCLRDataMethodDefinition.Request(uint reqCode, uint inBufferSize, byte* inBuffer, uint outBufferSize, byte* outBuffer)
    {
        int hr = HResults.S_OK;

        try
        {
            if (reqCode != (uint)CLRDataGeneralRequest.CLRDATA_REQUEST_REVISION
                || inBufferSize != 0
                || inBuffer is not null
                || outBufferSize != sizeof(uint))
            {
                throw new ArgumentException("Invalid request parameters.");
            }

            if (outBuffer is null)
                throw new NullReferenceException("The output buffer is null.");

            *(uint*)outBuffer = 1;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }


        return hr;
    }

    int IXCLRDataMethodDefinition.GetRepresentativeEntryAddress(ClrDataAddress* addr)
        => HResults.E_NOTIMPL;

    int IXCLRDataMethodDefinition.HasClassOrMethodInstantiation(int* bGeneric)
    {
        int hr = HResults.S_OK;

        try
        {
            if (bGeneric is null)
                throw new NullReferenceException();

            TargetPointer methodDescAddr = TryResolveMethodDesc();
            if (methodDescAddr == TargetPointer.Null)
                throw new System.Runtime.InteropServices.COMException(null, unchecked((int)0x8000FFFF)); // E_UNEXPECTED

            IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;
            MethodDescHandle methodDescHandle = rts.GetMethodDescHandle(methodDescAddr);
            *bGeneric = HasClassOrMethodInstantiation(_target, methodDescHandle) ? (int)Interop.BOOL.TRUE : (int)Interop.BOOL.FALSE;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }


        return hr;
    }
}
