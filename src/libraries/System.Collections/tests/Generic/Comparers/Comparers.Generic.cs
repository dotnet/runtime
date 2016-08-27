// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Tests;

namespace System.Collections.Generic.Tests
{
    // On .NET Native EqualityComparer is specialized for all of the primitive
    // types and a few more
    // Cover all of those cases
    public class SByteComparerTests : ComparersGenericTests<sbyte> { }
    public class ByteComparerTests : ComparersGenericTests<byte> { }
    public class ShortComparerTests : ComparersGenericTests<short> { }
    public class UShortComparerTests : ComparersGenericTests<ushort> { }
    public class IntComparerTests : ComparersGenericTests<int> { }
    public class UIntComparerTests : ComparersGenericTests<uint> { }
    public class LongComparerTests : ComparersGenericTests<long> { }
    public class ULongComparerTests : ComparersGenericTests<ulong> { }
    public class IntPtrComparerTests : ComparersGenericTests<IntPtr> { }
    public class UIntPtrComparerTests : ComparersGenericTests<UIntPtr> { }
    public class FloatComparerTests : ComparersGenericTests<float> { }
    public class DoubleComparerTests : ComparersGenericTests<double> { }
    public class DecimalComparerTests : ComparersGenericTests<decimal> { }
    public class StringComparerTests : ComparersGenericTests<string> { }

    // Nullables are handled specially
    public class NullableIntComparerTests : ComparersGenericTests<int?> { }
    public class NullableUIntComparerTests : ComparersGenericTests<uint?> { }

    // Currently the Default properties are special-cased for enums depending
    // on their underlying type (byte, short, ulong, etc.)
    // We should cover all of those.
    public class SByteEnumComparerTests : ComparersGenericTests<SByteEnum> { }
    public class ByteEnumComparerTests : ComparersGenericTests<ByteEnum> { }
    public class ShortEnumComparerTests : ComparersGenericTests<ShortEnum> { }
    public class UShortEnumComparerTests : ComparersGenericTests<UShortEnum> { }
    public class IntEnumComparerTests : ComparersGenericTests<IntEnum> { }
    public class UIntEnumComparerTests : ComparersGenericTests<UIntEnum> { }
    public class LongEnumComparerTests : ComparersGenericTests<LongEnum> { }
    public class ULongEnumComparerTests : ComparersGenericTests<ULongEnum> { }

    // Default properties currently will be special-cased for T : enum and
    // T : U? where U : {IComparable,IEquatable}<U>, but not if T : U? where U : enum
    // So let's cover those cases as well
    public class NullableSByteEnumComparerTests : ComparersGenericTests<SByteEnum?> { }
    public class NullableByteEnumComparerTests : ComparersGenericTests<ByteEnum?> { }
    public class NullableShortEnumComparerTests : ComparersGenericTests<ShortEnum?> { }
    public class NullableUShortEnumComparerTests : ComparersGenericTests<UShortEnum?> { }
    public class NullableIntEnumComparerTests : ComparersGenericTests<IntEnum?> { }
    public class NullableUIntEnumComparerTests : ComparersGenericTests<UIntEnum?> { }
    public class NullableLongEnumComparerTests : ComparersGenericTests<LongEnum?> { }
    public class NullableULongEnumComparerTests : ComparersGenericTests<ULongEnum?> { }

    // Comparer<T>.Default should still work OK with non-IComparables
    public class ObjectComparerTests : ComparersGenericTests<object> { }

    // Other cases: IComparable<T>, IComparable, and both
    public class GenericComparableComparerTests : ComparersGenericTests<GenericComparable> { }
    public class NonGenericComparableComparerTests : ComparersGenericTests<NonGenericComparable> { }
    public class BadlyBehavingComparableComparerTests : ComparersGenericTests<BadlyBehavingComparable> { }
}
