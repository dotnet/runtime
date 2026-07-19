// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.ManagedObjectWrapperLayout))]
internal sealed partial class ManagedObjectWrapperLayout : IData<ManagedObjectWrapperLayout>
{
    [Field] public long RefCount { get; }
    [Field] public int Flags { get; }
    [Field] public int UserDefinedCount { get; }
    [Field] public TargetPointer UserDefined { get; }
    [Field] public TargetPointer Dispatches { get; }
}
