// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal struct EECodeInfoHandle
{
    public readonly TargetPointer Address;
    internal EECodeInfoHandle(TargetPointer address) => Address = address;
}

internal interface IExecutionManager : IContract
{
    static string IContract.Name { get; } = nameof(ExecutionManager);
    EECodeInfoHandle? GetEECodeInfoHandle(TargetCodePointer ip) => throw new NotImplementedException();
    TargetPointer GetMethodDesc(EECodeInfoHandle codeInfoHandle) => throw new NotImplementedException();
    TargetCodePointer GetStartAddress(EECodeInfoHandle codeInfoHandle) => throw new NotImplementedException();

}

internal readonly struct ExecutionManager : IExecutionManager
{

}
