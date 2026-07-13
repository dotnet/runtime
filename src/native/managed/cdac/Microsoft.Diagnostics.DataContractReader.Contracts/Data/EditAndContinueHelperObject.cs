// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType("System.Diagnostics.EditAndContinueHelper")]
internal sealed partial class EditAndContinueHelperObject : IData<EditAndContinueHelperObject>
{
    [FieldAddress("_objectReference")]
    public TargetPointer ObjectReferenceAddress { get; }
}
