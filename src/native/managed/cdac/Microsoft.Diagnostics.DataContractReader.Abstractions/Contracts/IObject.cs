// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

public interface IObject : IContract
{
    static string IContract.Name { get; } = nameof(Object);
    TargetPointer GetMethodTableAddress(TargetPointer address) => throw new NotImplementedException();
    string GetStringValue(TargetPointer address) => throw new NotImplementedException();
    TargetPointer GetArrayData(TargetPointer address, out uint count, out TargetPointer boundsStart, out TargetPointer lowerBounds) => throw new NotImplementedException();
    bool GetBuiltInComData(TargetPointer address, out TargetPointer rcw, out TargetPointer ccw) => throw new NotImplementedException();
}

public readonly struct Object : IObject
{
    // Everything throws NotImplementedException
}
