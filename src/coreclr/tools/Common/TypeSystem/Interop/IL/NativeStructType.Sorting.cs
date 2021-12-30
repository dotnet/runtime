// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Internal.TypeSystem.Interop
{
    // Functionality related to determinstic ordering of types
    partial class NativeStructType
    {
        protected internal override int ClassCode => -377751537;

        protected internal override int CompareToImpl(TypeDesc other, TypeSystemComparer comparer)
        {
            return comparer.Compare(ManagedStructType, ((NativeStructType)other).ManagedStructType);
        }

        partial class NativeStructField
        {
            protected internal override int ClassCode => 1580219745;

            protected internal override int CompareToImpl(FieldDesc other, TypeSystemComparer comparer)
            {
                return comparer.Compare(_managedField, ((NativeStructField)other)._managedField);
            }
        }
    }
}
