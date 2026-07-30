// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Microsoft.Diagnostics.DataContractReader.Contracts;

namespace Microsoft.Diagnostics.DataContractReader.Legacy;

[GeneratedComClass]
public sealed unsafe partial class ClrDataAppDomain : IXCLRDataAppDomain
{
    internal const uint DefaultAppDomainId = 1;

    private readonly Target _target;
    private readonly TargetPointer _appDomain;

    public TargetPointer Address => _appDomain;

    public ClrDataAppDomain(Target target, TargetPointer appDomain)
    {
        _target = target;
        _appDomain = appDomain;
    }

    int IXCLRDataAppDomain.GetProcess(DacComNullableByRef<IXCLRDataProcess> process)
        => HResults.E_NOTIMPL;

    int IXCLRDataAppDomain.GetName(uint bufLen, uint* nameLen, char* name)
    {
        int hr = HResults.S_OK;
        string friendlyName;
        try
        {
            ILoader loader = _target.Contracts.Loader;
            friendlyName = loader.GetAppDomainFriendlyName();
        }
        catch (VirtualReadException)
        {
            // Match native DAC / SOSDacImpl behavior: fall back to empty string
            // when the FriendlyName pointer targets unreadable memory.
            friendlyName = string.Empty;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
            friendlyName = string.Empty;
        }

        if (hr >= 0)
        {
            OutputBufferHelpers.CopyStringToBuffer(name, bufLen, nameLen, friendlyName);

            // Match native DAC behavior: return S_FALSE when output is truncated.
            uint requiredLen = (uint)friendlyName.Length + 1;
            if (name is not null && bufLen > 0 && bufLen < requiredLen)
                hr = HResults.S_FALSE;
        }


        return hr;
    }

    int IXCLRDataAppDomain.GetUniqueID(ulong* id)
    {
        int hr = HResults.S_OK;
        try
        {
            if (id is null)
                throw new ArgumentNullException(nameof(id));

            *id = DefaultAppDomainId;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }


        return hr;
    }

    int IXCLRDataAppDomain.GetFlags(uint* flags)
    {
        int hr = HResults.S_OK;
        try
        {
            if (flags is null)
                throw new ArgumentNullException(nameof(flags));

            // CLRDATA_DOMAIN_DEFAULT = 0
            *flags = 0;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }


        return hr;
    }

    int IXCLRDataAppDomain.IsSameObject(IXCLRDataAppDomain* appDomain)
    {
        int hr = HResults.S_FALSE;
        try
        {
            if (System.Runtime.InteropServices.ComWrappers.TryGetObject((nint)appDomain, out object? obj)
                && obj is ClrDataAppDomain other)
            {
                hr = _appDomain == other._appDomain ? HResults.S_OK : HResults.S_FALSE;
            }
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }


        return hr;
    }

    int IXCLRDataAppDomain.GetManagedObject(DacComNullableByRef<IXCLRDataValue> value)
        => HResults.E_NOTIMPL;

    int IXCLRDataAppDomain.Request(uint reqCode, uint inBufferSize, byte* inBuffer, uint outBufferSize, byte* outBuffer)
        => HResults.E_NOTIMPL;
}
