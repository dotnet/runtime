// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.TypedByRef))]
internal sealed partial class TypedByRef : IData<TypedByRef>
{
    [Field]
    public partial TargetPointer Data { get; }

    [Field]
    public partial TargetPointer Type { get; }
}
