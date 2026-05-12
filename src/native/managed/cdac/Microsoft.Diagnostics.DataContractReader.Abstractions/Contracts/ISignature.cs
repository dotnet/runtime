// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection.Metadata;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

public interface ISignature : IContract
{
    static string IContract.Name { get; } = nameof(Signature);
    TypeHandle DecodeFieldSignature(BlobHandle blobHandle, ModuleHandle moduleHandle, TypeHandle ctx) => throw new NotImplementedException();

    /// <summary>
    /// Given the address of a <c>VASigCookie*</c> slot pushed by a vararg call site, return
    /// the address of the first argument of that call relative to the cookie pointer location,
    /// taking the platform calling convention into account.
    /// </summary>
    TargetPointer GetVarArgArgsBase(TargetPointer vaSigCookieAddr) => throw new NotImplementedException();

    /// <summary>
    /// Given the address of a <c>VASigCookie*</c> slot pushed by a vararg call site, return the
    /// target address and length (in bytes) of the raw vararg signature blob held by the cookie.
    /// </summary>
    void GetVarArgSignature(TargetPointer vaSigCookieAddr, out TargetPointer signatureAddress, out uint signatureLength)
        => throw new NotImplementedException();
}

public readonly struct Signature : ISignature
{
    // Everything throws NotImplementedException
}
