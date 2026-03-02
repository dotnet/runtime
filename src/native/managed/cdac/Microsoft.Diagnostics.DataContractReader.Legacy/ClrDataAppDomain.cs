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
    private const ulong DefaultADID = 1;

    private readonly Target _target;
    private readonly TargetPointer _appDomain;
    private readonly IXCLRDataAppDomain? _legacyImpl;

    public TargetPointer Address => _appDomain;

    public ClrDataAppDomain(Target target, TargetPointer appDomain, IXCLRDataAppDomain? legacyImpl)
    {
        _target = target;
        _appDomain = appDomain;
        _legacyImpl = legacyImpl;
    }

    int IXCLRDataAppDomain.GetProcess(void** process)
        => _legacyImpl is not null ? _legacyImpl.GetProcess(process) : HResults.E_NOTIMPL;

    int IXCLRDataAppDomain.GetName(uint bufLen, uint* nameLen, char* name)
    {
        int hr = HResults.S_OK;
        try
        {
            ILoader loader = _target.Contracts.Loader;
            string friendlyName = loader.GetAppDomainFriendlyName();

            if (nameLen is not null)
                *nameLen = (uint)(friendlyName.Length + 1);

            if (name is not null && bufLen > 0)
            {
                int charsToCopy = Math.Min(friendlyName.Length, (int)bufLen - 1);
                friendlyName.AsSpan(0, charsToCopy).CopyTo(new Span<char>(name, (int)bufLen));
                name[charsToCopy] = '\0';

                if ((uint)friendlyName.Length >= bufLen)
                    hr = HResults.S_FALSE;
            }
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacyImpl is not null)
        {
            uint nameLenLocal;
            int hrLocal = _legacyImpl.GetName(bufLen, &nameLenLocal, null);
            Debug.Assert(hrLocal == hr || hr == HResults.S_FALSE, $"cDAC: {hr:x}, DAC: {hrLocal:x}");
        }
#endif

        return hr;
    }

    int IXCLRDataAppDomain.GetUniqueID(ulong* id)
    {
        *id = DefaultADID;

#if DEBUG
        if (_legacyImpl is not null)
        {
            ulong idLocal;
            int hrLocal = _legacyImpl.GetUniqueID(&idLocal);
            Debug.Assert(hrLocal == HResults.S_OK, $"cDAC: {HResults.S_OK:x}, DAC: {hrLocal:x}");
            Debug.Assert(*id == idLocal, $"cDAC: {*id}, DAC: {idLocal}");
        }
#endif

        return HResults.S_OK;
    }

    int IXCLRDataAppDomain.GetFlags(uint* flags)
    {
        // CLRDATA_DOMAIN_DEFAULT = 0
        *flags = 0;

#if DEBUG
        if (_legacyImpl is not null)
        {
            uint flagsLocal;
            int hrLocal = _legacyImpl.GetFlags(&flagsLocal);
            Debug.Assert(hrLocal == HResults.S_OK, $"cDAC: {HResults.S_OK:x}, DAC: {hrLocal:x}");
            Debug.Assert(*flags == flagsLocal, $"cDAC: {*flags}, DAC: {flagsLocal}");
        }
#endif

        return HResults.S_OK;
    }

    int IXCLRDataAppDomain.IsSameObject(IXCLRDataAppDomain* appDomain)
    {
        int hr = HResults.S_FALSE;
        try
        {
            StrategyBasedComWrappers cw = new();
            object obj = cw.GetOrCreateObjectForComInstance((nint)appDomain, CreateObjectFlags.None);
            if (obj is ClrDataAppDomain other)
            {
                hr = _appDomain == other._appDomain ? HResults.S_OK : HResults.S_FALSE;
            }
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacyImpl is not null)
        {
            int hrLocal = _legacyImpl.IsSameObject(appDomain);
            Debug.Assert(hrLocal == hr, $"cDAC: {hr}, DAC: {hrLocal}");
        }
#endif

        return hr;
    }

    int IXCLRDataAppDomain.GetManagedObject(DacComNullableByRef<IXCLRDataValue> value)
        => _legacyImpl is not null ? _legacyImpl.GetManagedObject(value) : HResults.E_NOTIMPL;

    int IXCLRDataAppDomain.Request(uint reqCode, uint inBufferSize, byte* inBuffer, uint outBufferSize, byte* outBuffer)
        => _legacyImpl is not null ? _legacyImpl.Request(reqCode, inBufferSize, inBuffer, outBufferSize, outBuffer) : HResults.E_NOTIMPL;
}