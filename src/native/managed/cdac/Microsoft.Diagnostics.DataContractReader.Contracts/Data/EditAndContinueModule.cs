// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.EditAndContinueModule))]
internal sealed partial class EditAndContinueModule : IData<EditAndContinueModule>
{
    [Field] public int ApplyChangesCount { get; private set; }
}
