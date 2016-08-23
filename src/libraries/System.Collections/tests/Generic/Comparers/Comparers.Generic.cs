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
    public class SByteComparerTests : ComparerGenericTests<sbyte> { }
    public class ByteComparerTests : ComparerGenericTests<byte> { }
    public class ShortComparerTests : ComparerGenericTests<short> { }
    public class UShortComparerTests : ComparerGenericTests<ushort> { }
    public class IntComparerTests : ComparerGenericTests<int> { }
    public class UIntComparerTests : ComparerGenericTests<uint> { }
    public class LongComparerTests : ComparerGenericTests<long> { }
    public class ULongComparerTests : ComparerGenericTests<ulong> { }
    public class IntPtrComparerTests : ComparerGenericTests<IntPtr> { }
    public class UIntPtrComparerTests : ComparerGenericTests<UIntPtr> { }
    public class FloatComparerTests : ComparerGenericTests<float> { }
    public class DoubleComparerTests : ComparerGenericTests<double> { }
    public class DecimalComparerTests : ComparerGenericTests<decimal> { }
    public class StringComparerTests : ComparerGenericTests<string> { }

    // Nullables are handled specially
    public class NullableIntComparerTests : ComparerGenericTests<int?> { }
    public class NullableUIntComparerTests : ComparerGenericTests<uint?> { }

    // Currently the Default properties are special-cased for enums depending
    // on their underlying type (byte, short, ulong, etc.)
    // We should cover all of those.
    public class SByteEnumComparerTests : ComparerGenericTests<SByteEnum> { }
    public class ByteEnumComparerTests : ComparerGenericTests<ByteEnum> { }
    public class ShortEnumComparerTests : ComparerGenericTests<ShortEnum> { }
    public class UShortEnumComparerTests : ComparerGenericTests<UShortEnum> { }
    public class IntEnumComparerTests : ComparerGenericTests<IntEnum> { }
    public class UIntEnumComparerTests : ComparerGenericTests<UIntEnum> { }
    public class LongEnumComparerTests : ComparerGenericTests<LongEnum> { }
    public class ULongEnumComparerTests : ComparerGenericTests<ULongEnum> { }

    // Default properties currently will be special-cased for T : enum and
    // T : U? where U : {IComparable,IEquatable}<U>, but not if T : U? where U : enum
    // So let's cover those cases as well
    public class NullableSByteEnumComparerTests : ComparerGenericTests<SByteEnum?> { }
    public class NullableByteEnumComparerTests : ComparerGenericTests<ByteEnum?> { }
    public class NullableShortEnumComparerTests : ComparerGenericTests<ShortEnum?> { }
    public class NullableUShortEnumComparerTests : ComparerGenericTests<UShortEnum?> { }
    public class NullableIntEnumComparerTests : ComparerGenericTests<IntEnum?> { }
    public class NullableUIntEnumComparerTests : ComparerGenericTests<UIntEnum?> { }
    public class NullableLongEnumComparerTests : ComparerGenericTests<LongEnum?> { }
    public class NullableULongEnumComparerTests : ComparerGenericTests<ULongEnum?> { }

    // Comparer<T>.Default should still work OK with non-IComparables
    public class ObjectComparerTests : ComparerGenericTests<object> { }

    // Other cases: IComparable<T>, IComparable, and both
    public class GenericComparableComparerTests : ComparerGenericTests<GenericComparable> { }
    public class NonGenericComparableComparerTests : ComparerGenericTests<NonGenericComparable> { }
    public class BadlyBehavingComparableComparerTests : ComparerGenericTests<BadlyBehavingComparable> { }
}
