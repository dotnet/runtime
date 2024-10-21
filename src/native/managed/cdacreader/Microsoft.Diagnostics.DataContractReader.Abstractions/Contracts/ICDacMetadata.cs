// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal interface ICDacMetadata : IContract
{
    static string IContract.Name { get; } = nameof(CDacMetadata);
    TargetPointer GetPrecodeMachineDescriptor() => throw new NotImplementedException();
}

internal readonly struct CDacMetadata : ICDacMetadata
{

}
