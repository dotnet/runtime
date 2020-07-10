// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Internal.TypeSystem
{
    // Functionality related to determinstic ordering of types
    partial class PointerType
    {
        protected internal override int ClassCode => -2124247792;

        protected internal override int CompareToImpl(TypeDesc other, TypeSystemComparer comparer)
        {
            return comparer.Compare(ParameterType, ((PointerType)other).ParameterType);
        }
    }
}
