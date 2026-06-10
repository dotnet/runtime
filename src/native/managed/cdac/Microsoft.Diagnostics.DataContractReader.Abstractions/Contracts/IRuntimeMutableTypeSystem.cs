// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

public interface IRuntimeMutableTypeSystem : IContract
{
    static string IContract.Name { get; } = nameof(RuntimeMutableTypeSystem);

    IEnumerable<TargetPointer> EnumerateAddedFieldDescs(TypeHandle typeHandle, bool staticFields) => throw new NotImplementedException();
    bool IsFieldDescEnCNew(TargetPointer fieldDescPointer) => throw new NotImplementedException();
    bool DoesEnCFieldDescNeedFixup(TargetPointer encFieldDescPointer) => throw new NotImplementedException();
    TargetPointer GetEnCStaticFieldDataAddress(TargetPointer encFieldDescPointer) => throw new NotImplementedException();
    TargetPointer GetEnCInstanceFieldAddress(TargetPointer objectAddress, TargetPointer encFieldDescPointer) => throw new NotImplementedException();
}

public readonly struct RuntimeMutableTypeSystem : IRuntimeMutableTypeSystem
{
    // Everything throws NotImplementedException
}
