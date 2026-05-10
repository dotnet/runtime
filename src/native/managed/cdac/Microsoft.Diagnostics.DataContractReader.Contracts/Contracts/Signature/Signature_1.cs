// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal readonly struct Signature_1 : ISignature
{
    private readonly Target _target;

    internal Signature_1(Target target)
    {
        _target = target;
    }

    private TargetPointer ReadVASigCookiePointer(TargetPointer vaSigCookieAddr)
    {
        // The argument is the address of a VASigCookie* slot on the stack, so dereference once
        // to obtain the pointer to the actual VASigCookie instance.
        if (vaSigCookieAddr == TargetPointer.Null)
            throw new ArgumentException("VASigCookie address must be non-null.", nameof(vaSigCookieAddr));

        TargetPointer vaSigCookie = _target.ReadPointer(vaSigCookieAddr);
        if (vaSigCookie == TargetPointer.Null)
            throw new InvalidOperationException("VASigCookie pointer is null.");

        return vaSigCookie;
    }

    TargetPointer ISignature.GetVarArgArgsBase(TargetPointer vaSigCookieAddr)
    {
        TargetPointer vaSigCookie = ReadVASigCookiePointer(vaSigCookieAddr);
        Data.VASigCookie cookie = _target.ProcessedData.GetOrAdd<Data.VASigCookie>(vaSigCookie);

        // Compute the address of the first argument. On x86 the args are pushed below the cookie
        // pointer (stack grows down on the args walk), so the first argument lies at
        //   vaSigCookieAddr + sizeOfArgs.
        // On all other platforms the first argument follows the cookie pointer in memory
        // (stack grows up on the args walk), so its address is
        //   vaSigCookieAddr + sizeof(VASigCookie*).
        RuntimeInfoArchitecture arch = _target.Contracts.RuntimeInfo.GetTargetArchitecture();
        if (arch == RuntimeInfoArchitecture.X86)
        {
            return new TargetPointer(vaSigCookieAddr.Value + cookie.SizeOfArgs);
        }

        return new TargetPointer(vaSigCookieAddr.Value + (ulong)_target.PointerSize);
    }

    void ISignature.GetVarArgSignature(TargetPointer vaSigCookieAddr, out TargetPointer signatureAddress, out uint signatureLength)
    {
        TargetPointer vaSigCookie = ReadVASigCookiePointer(vaSigCookieAddr);
        Data.VASigCookie cookie = _target.ProcessedData.GetOrAdd<Data.VASigCookie>(vaSigCookie);

        signatureAddress = cookie.SignaturePointer;
        signatureLength = cookie.SignatureLength;
        Debug.Assert(signatureAddress != TargetPointer.Null || signatureLength == 0,
            "VASigCookie has a non-zero signature length but a null signature pointer.");
    }
}
