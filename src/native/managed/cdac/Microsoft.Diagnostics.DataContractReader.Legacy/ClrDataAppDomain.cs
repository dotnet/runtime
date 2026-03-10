// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Microsoft.Diagnostics.DataContractReader.Legacy;

[GeneratedComClass]
public sealed unsafe partial class ClrDataAppDomain : IXCLRDataAppDomain
{
    private readonly TargetPointer _appDomain;
    private readonly IXCLRDataAppDomain? _legacyImpl;

    public TargetPointer Address => _appDomain;

    public ClrDataAppDomain(TargetPointer appDomain, IXCLRDataAppDomain? legacyImpl)
    {
        _appDomain = appDomain;
        _legacyImpl = legacyImpl;
    }

    int IXCLRDataAppDomain.GetProcess(/*IXCLRDataProcess*/ void** process)
        => _legacyImpl is not null ? _legacyImpl.GetProcess(process) : HResults.E_NOTIMPL;

    int IXCLRDataAppDomain.GetName(uint bufLen, uint* nameLen, char* name)
        => _legacyImpl is not null ? _legacyImpl.GetName(bufLen, nameLen, name) : HResults.E_NOTIMPL;

    int IXCLRDataAppDomain.GetUniqueID(ulong* id)
        => _legacyImpl is not null ? _legacyImpl.GetUniqueID(id) : HResults.E_NOTIMPL;

    int IXCLRDataAppDomain.GetFlags(uint* flags)
        => _legacyImpl is not null ? _legacyImpl.GetFlags(flags) : HResults.E_NOTIMPL;

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
        catch (Exception ex)
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
