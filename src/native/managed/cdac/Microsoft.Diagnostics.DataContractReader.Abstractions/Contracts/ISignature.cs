// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

public interface ISignature : IContract
{
    static string IContract.Name { get; } = nameof(Signature);

    /// <summary>
    /// Given the address of a <c>VASigCookie</c> on the stack (the cookie pointer location
    /// pushed for a vararg call), return the address of the first argument relative to that
    /// cookie pointer, taking the platform calling convention into account.
    /// </summary>
    TargetPointer GetVarArgArgsBase(TargetPointer vaSigCookieAddr) => throw new NotImplementedException();

    /// <summary>
    /// Given the address of a <c>VASigCookie</c> on the stack, return the target address and
    /// length (in bytes) of the raw vararg signature blob held by the cookie.
    /// </summary>
    void GetVarArgSignature(TargetPointer vaSigCookieAddr, out TargetPointer signatureAddress, out uint signatureLength)
        => throw new NotImplementedException();
}

public readonly struct Signature : ISignature
{
    // Everything throws NotImplementedException
}
