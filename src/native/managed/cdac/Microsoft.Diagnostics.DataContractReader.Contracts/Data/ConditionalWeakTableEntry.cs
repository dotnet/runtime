// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(ManagedFullName = "System.Runtime.CompilerServices.ConditionalWeakTable`2+Entry", IsValueType = true)]
internal sealed partial class ConditionalWeakTableEntry : IData<ConditionalWeakTableEntry>
{
    [Field("HashCode")]
    public int HashCode { get; }

    [Field("Next")]
    public int Next { get; }

    [FieldAddress("depHnd")]
    public TargetPointer DepHndAddress { get; }
}
