// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.CMiniMdSchema))]
internal sealed partial class CMiniMdSchema : IData<CMiniMdSchema>
{
    [Field] public partial byte Heaps { get; }
    [Field] public partial ulong Sorted { get; }
    [FieldAddress] public partial TargetPointer RecordCounts { get; }
}
