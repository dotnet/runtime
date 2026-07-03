// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.ManagedObjectWrapperHolderObject))]
internal sealed partial class ManagedObjectWrapperHolderObject : IData<ManagedObjectWrapperHolderObject>
{
    [Field] public TargetPointer WrappedObject { get; }
    [Field] public TargetPointer Wrapper { get; }
}
