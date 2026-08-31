// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Internal.TypeSystem.Interop
{
    // Functionality related to deterministic ordering of types
    internal partial class InlineArrayType
    {
        protected override int ClassCode => 226817075;

        protected override int CompareToImpl(TypeDesc other, TypeSystemComparer comparer)
        {
            var otherType = (InlineArrayType)other;
            int result = (int)Length - (int)otherType.Length;
            if (result != 0)
                return result;

            return comparer.Compare(ElementType, otherType.ElementType);
        }

        private partial class InlineArrayMethod
        {
            protected override int ClassCode => -1303220581;

            protected override int CompareToImpl(MethodDesc other, TypeSystemComparer comparer)
            {
                var otherMethod = (InlineArrayMethod)other;

                int result = _kind - otherMethod._kind;
                if (result != 0)
                    return result;

                return comparer.Compare(OwningType, otherMethod.OwningType);
            }
        }

        private partial class InlineArrayField
        {
            protected override int ClassCode => 1542668652;

            protected override int CompareToImpl(FieldDesc other, TypeSystemComparer comparer)
            {
                var otherField = (InlineArrayField)other;

                return comparer.Compare(OwningType, otherField.OwningType);
            }
        }
    }
}
