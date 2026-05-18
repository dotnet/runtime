// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

public interface IEnC : IContract
{
    static string IContract.Name { get; } = nameof(EnC);
    TargetNUInt GetLatestEnCVersion(TargetPointer module, uint methodDef) => throw new NotImplementedException();
    TargetNUInt GetEnCVersion(TargetPointer module, uint methodDef, TargetCodePointer nativeCodeAddress) => throw new NotImplementedException();
}

public readonly struct EnC : IEnC
{
}
