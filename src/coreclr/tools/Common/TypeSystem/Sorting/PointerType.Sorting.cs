// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Internal.TypeSystem
{
    // Functionality related to deterministic ordering of types
    public partial class PointerType
    {
        protected internal override int ClassCode => -2124247792;

        protected internal override int CompareToImpl(TypeDesc other, TypeSystemComparer comparer)
        {
            return comparer.Compare(ParameterType, ((PointerType)other).ParameterType);
        }
    }
}
