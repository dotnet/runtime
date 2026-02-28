// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Microsoft.Diagnostics.DataContractReader.Legacy;

[GeneratedComClass]
public sealed unsafe partial class ClrDataAppDomain: IXCLRDataAppDomain
{
    private readonly Target _target;
    private readonly TargetPointer _appDomain;
    private readonly IXCLRDataAppDomain? _legacyImpl;

    public ClrDataAppDomain(Target target, TargetPointer appDomain, IXCLRDataAppDomain? legacyImpl)
    {
        _target = target;
        _appDomain = appDomain;
        _legacyImpl = legacyImpl;
    }

    int IXCLRDataAppDomain.GetProcess(IXCLRDataProcess** process)
        => _legacyImpl is not null ? _legacyImpl.GetProcess(process) : HResults.E_NOTIMPL;

    int IXCLRDataAppDomain.GetName(uint bufLen, uint* nameLen, char* name)
        => _legacyImpl is not null ? _legacyImpl.GetName(bufLen, nameLen, name) : HResults.E_NOTIMPL;

    int IXCLRDataAppDomain.GetUniqueID(ulong* id)
        => _legacyImpl is not null ? _legacyImpl.GetUniqueID(id) : HResults.E_NOTIMPL;

    int IXCLRDataAppDomain.GetFlags(uint* flags)
        => _legacyImpl is not null ? _legacyImpl.GetFlags(flags) : HResults.E_NOTIMPL;

    int IXCLRDataAppDomain.IsSameObject(IXCLRDataAppDomain* appDomain)
        => _legacyImpl is not null ? _legacyImpl.IsSameObject(appDomain) : HResults.E_NOTIMPL;

    int IXCLRDataAppDomain.GetManagedObject(/*IXCLRDataValue*/ void** value)
        => _legacyImpl is not null ? _legacyImpl.GetManagedObject(value) : HResults.E_NOTIMPL;

    int IXCLRDataAppDomain.Request(uint reqCode,
                uint inBufferSize,
                [In, MarshalUsing(CountElementName = nameof(inBufferSize))] byte[] inBuffer,
                uint outBufferSize,
                [Out, MarshalUsing(CountElementName = nameof(outBufferSize))] byte[] outBuffer)
        => _legacyImpl is not null ? _legacyImpl.Request(reqCode, inBufferSize, inBuffer, outBufferSize, outBuffer) : HResults.E_NOTIMPL;
}
