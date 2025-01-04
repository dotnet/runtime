// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal struct CodeBlockHandle
{
    public readonly TargetPointer Address;
    internal CodeBlockHandle(TargetPointer address) => Address = address;
}

internal interface IExecutionManager : IContract
{
    static string IContract.Name { get; } = nameof(ExecutionManager);
    CodeBlockHandle? GetCodeBlockHandle(TargetCodePointer ip) => throw new NotImplementedException();
    TargetPointer GetMethodDesc(CodeBlockHandle codeInfoHandle) => throw new NotImplementedException();
    TargetCodePointer GetStartAddress(CodeBlockHandle codeInfoHandle) => throw new NotImplementedException();
}

internal readonly struct ExecutionManager : IExecutionManager
{

}
