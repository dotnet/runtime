// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

public interface IEditAndContinue : IContract
{
    static string IContract.Name { get; } = nameof(EditAndContinue);

    /// <summary>
    /// Enumerate FieldDesc pointers for fields added to <paramref name="typeHandle"/> via
    /// Edit-and-Continue. The enumeration is empty when the owning module is not EnC-enabled
    /// or no EnC fields have been added for the type.
    /// </summary>
    /// <param name="typeHandle">A MethodTable type handle.</param>
    /// <param name="staticFields">If <c>true</c>, enumerate added static fields; otherwise added instance fields.</param>
    IEnumerable<TargetPointer> EnumerateAddedFieldDescs(TypeHandle typeHandle, bool staticFields)
        => throw new NotImplementedException();
}

public readonly struct EditAndContinue : IEditAndContinue
{
    // Everything throws NotImplementedException
}
