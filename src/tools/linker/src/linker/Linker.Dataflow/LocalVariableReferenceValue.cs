// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using ILLink.Shared.DataFlow;
using Mono.Cecil.Cil;

namespace ILLink.Shared.TrimAnalysis
{
    public partial record LocalVariableReferenceValue(VariableDefinition LocalDefinition)
        : ReferenceValue(LocalDefinition.VariableType)
    {
        public override SingleValue DeepCopy()
        {
            return this;
        }
    }
}
