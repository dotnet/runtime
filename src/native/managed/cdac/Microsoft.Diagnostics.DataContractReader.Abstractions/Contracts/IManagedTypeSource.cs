// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Diagnostics.DataContractReader.Data;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

/// <summary>
/// Resolves layout information for managed CLR types by fully-qualified name.
/// </summary>
public interface IManagedTypeSource : IContract
{
    static string IContract.Name { get; } = nameof(ManagedTypeSource);

    bool TryGetTypeInfo(string fullyQualifiedName, out Target.TypeInfo info) => throw new NotImplementedException();
    Target.TypeInfo GetTypeInfo(string fullyQualifiedName) => throw new NotImplementedException();

    bool TryGetTypeHandle(string fullyQualifiedName, out ITypeHandle typeHandle) => throw new NotImplementedException();
    ITypeHandle GetTypeHandle(string fullyQualifiedName) => throw new NotImplementedException();

    bool TryGetStaticFieldAddress(string fullyQualifiedName, string fieldName, out TargetPointer address) => throw new NotImplementedException();
    TargetPointer GetStaticFieldAddress(string fullyQualifiedName, string fieldName) => throw new NotImplementedException();

    bool TryGetThreadStaticFieldAddress(string fullyQualifiedName, string fieldName, TargetPointer thread, out TargetPointer address) => throw new NotImplementedException();
    TargetPointer GetThreadStaticFieldAddress(string fullyQualifiedName, string fieldName, TargetPointer thread) => throw new NotImplementedException();
}

public readonly struct ManagedTypeSource : IManagedTypeSource
{
    // Everything throws NotImplementedException
}
