// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Internal.TypeSystem
{
    // Functionality related to deterministic ordering of types
    partial class ArrayType
    {
        protected internal override int ClassCode => -1274559616;

        protected internal override int CompareToImpl(TypeDesc other, TypeSystemComparer comparer)
        {
            var otherType = (ArrayType)other;
            int result = _rank - otherType._rank;
            if (result != 0)
                return result;

            return comparer.Compare(ElementType, otherType.ElementType);
        }
    }

    partial class ArrayMethod
    {
        protected internal override int ClassCode => 487354154;

        protected internal override int CompareToImpl(MethodDesc other, TypeSystemComparer comparer)
        {
            var otherMethod = (ArrayMethod)other;
            int result = _kind - otherMethod._kind;
            if (result != 0)
                return result;

            return comparer.CompareWithinClass(OwningArray, otherMethod.OwningArray);
        }
    }
}
