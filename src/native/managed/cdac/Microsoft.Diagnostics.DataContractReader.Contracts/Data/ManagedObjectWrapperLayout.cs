// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.ManagedObjectWrapperLayout))]
internal sealed partial class ManagedObjectWrapperLayout : IData<ManagedObjectWrapperLayout>
{
    [Field] public partial long RefCount { get; }
    [Field] public partial int Flags { get; }
    [Field] public partial int UserDefinedCount { get; }
    [Field] public partial TargetPointer UserDefined { get; }
    [Field] public partial TargetPointer Dispatches { get; }
}
