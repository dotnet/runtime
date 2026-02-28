// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

public interface IBuiltInCOM : IContract
{
    static string IContract.Name { get; } = nameof(BuiltInCOM);
    ulong GetRefCount(TargetPointer address) => throw new NotImplementedException();
    bool IsHandleWeak(TargetPointer address) => throw new NotImplementedException();
    IEnumerable<(TargetPointer MethodTable, TargetPointer Unknown)> GetRCWInterfaces(TargetPointer rcw) => throw new NotImplementedException();
}

public readonly struct BuiltInCOM : IBuiltInCOM
{
    // Everything throws NotImplementedException
}
