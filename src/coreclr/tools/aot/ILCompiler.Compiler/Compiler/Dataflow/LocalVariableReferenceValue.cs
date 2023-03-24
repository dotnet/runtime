// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using ILLink.Shared.DataFlow;
using Internal.TypeSystem;

#nullable enable

namespace ILLink.Shared.TrimAnalysis
{
    public partial record LocalVariableReferenceValue : ReferenceValue
    {
        public int LocalIndex { get; }

        public LocalVariableReferenceValue(int localIndex, TypeDesc localType)
            : base(localType)
        {
            LocalIndex = localIndex;
        }

        public override SingleValue DeepCopy()
        {
            return this;
        }
    }
}
