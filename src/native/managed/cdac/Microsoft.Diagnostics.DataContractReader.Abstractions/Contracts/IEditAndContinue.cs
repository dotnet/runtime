// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

public interface IEditAndContinue : IContract
{
    static string IContract.Name { get; } = nameof(EditAndContinue);

    IEnumerable<TargetPointer> EnumerateAddedFieldDescs(TypeHandle typeHandle, bool staticFields)
        => throw new NotImplementedException();
}

public readonly struct EditAndContinue : IEditAndContinue
{
    // Everything throws NotImplementedException
}
