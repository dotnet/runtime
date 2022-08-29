// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using ILLink.Shared.DataFlow;

using Internal.TypeSystem;

#nullable enable

namespace ILLink.Shared.TrimAnalysis
{
    public partial record FieldReferenceValue(FieldDesc FieldDefinition) : ReferenceValue
    {
        public override SingleValue DeepCopy() => this;
    }
}
