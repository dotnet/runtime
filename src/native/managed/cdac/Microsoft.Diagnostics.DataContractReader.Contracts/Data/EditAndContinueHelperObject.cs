// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType("System.Diagnostics.EditAndContinueHelper")]
internal sealed partial class EditAndContinueHelperObject : IData<EditAndContinueHelperObject>
{
    /// <summary>
    /// Address of the <c>_objectReference</c> OBJECTREF slot on the helper instance.
    /// For EnC-added instance fields of reference type, this slot itself is the field
    /// value location; for value-typed and primitive fields, the slot holds the boxed
    /// value or its array-wrapper container.
    /// </summary>
    [FieldAddress("_objectReference")]
    public TargetPointer ObjectReferenceAddress { get; }
}
