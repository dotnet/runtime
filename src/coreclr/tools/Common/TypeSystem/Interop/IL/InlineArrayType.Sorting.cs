// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Internal.TypeSystem.Interop
{
    // Functionality related to determinstic ordering of types
    partial class InlineArrayType
    {
        protected internal override int ClassCode => 226817075;

        protected internal override int CompareToImpl(TypeDesc other, TypeSystemComparer comparer)
        {
            var otherType = (InlineArrayType)other;
            int result = (int)Length - (int)otherType.Length;
            if (result != 0)
                return result;

            return comparer.Compare(ElementType, otherType.ElementType);
        }

        partial class InlineArrayMethod
        {
            protected internal override int ClassCode => -1303220581;

            protected internal override int CompareToImpl(MethodDesc other, TypeSystemComparer comparer)
            {
                var otherMethod = (InlineArrayMethod)other;

                int result = _kind - otherMethod._kind;
                if (result != 0)
                    return result;

                return comparer.CompareWithinClass(OwningType, otherMethod.OwningType);
            }
        }

        partial class InlineArrayField
        {
            protected internal override int ClassCode => 1542668652;

            protected internal override int CompareToImpl(FieldDesc other, TypeSystemComparer comparer)
            {
                var otherField = (InlineArrayField)other;

                return comparer.CompareWithinClass(OwningType, otherField.OwningType);
            }
        }
    }
}
