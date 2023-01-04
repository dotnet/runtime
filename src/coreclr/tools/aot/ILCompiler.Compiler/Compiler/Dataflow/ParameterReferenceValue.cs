// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using ILLink.Shared.DataFlow;
using ILLink.Shared.TypeSystemProxy;

#nullable enable

namespace ILLink.Shared.TrimAnalysis
{
    internal sealed record ParameterReferenceValue(ParameterProxy Parameter)
        : ReferenceValue(Parameter.ParameterType)
    {
        public override SingleValue DeepCopy()
        {
            return this;
        }
    }
}
