// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using ILLink.Shared.DataFlow;
using Mono.Cecil;

namespace ILLink.Shared.TrimAnalysis
{
    public partial record ThisParameterReferenceValue(MethodDefinition MethodDefinition)
    : ReferenceValue(MethodDefinition.DeclaringType)
    {
        public override SingleValue DeepCopy()
        {
            return this;
        }
    }
}
