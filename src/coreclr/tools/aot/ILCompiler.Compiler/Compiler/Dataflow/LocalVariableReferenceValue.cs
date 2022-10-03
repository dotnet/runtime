// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using ILLink.Shared.DataFlow;

#nullable enable

namespace ILLink.Shared.TrimAnalysis
{
    public partial record LocalVariableReferenceValue(int LocalIndex) : ReferenceValue
    {
        public override SingleValue DeepCopy()
        {
            return this;
        }
    }
}
