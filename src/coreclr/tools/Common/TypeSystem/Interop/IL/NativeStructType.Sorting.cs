// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Internal.TypeSystem.Interop
{
    // Functionality related to deterministic ordering of types
    partial class NativeStructType
    {
        protected override int ClassCode => -377751537;

        protected override int CompareToImpl(TypeDesc other, TypeSystemComparer comparer)
        {
            return comparer.Compare(ManagedStructType, ((NativeStructType)other).ManagedStructType);
        }

        partial class NativeStructField
        {
            protected override int ClassCode => 1580219745;

            protected override int CompareToImpl(FieldDesc other, TypeSystemComparer comparer)
            {
                return comparer.Compare(_managedField, ((NativeStructField)other)._managedField);
            }
        }
    }
}
