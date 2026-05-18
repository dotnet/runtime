// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Collections.Generic;
using System.Runtime.InteropServices;
namespace Microsoft.Diagnostics.DataContractReader.Legacy;

internal interface IEnum<T>
{
    IEnumerator<T> Enumerator { get; }
    TargetPointer LegacyHandle { get; }
    void Dispose() => Enumerator.Dispose();
    long GetHandle()
    {
        GCHandle gcHandle = GCHandle.Alloc(this);
        return GCHandle.ToIntPtr(gcHandle).ToInt64();
    }
}
