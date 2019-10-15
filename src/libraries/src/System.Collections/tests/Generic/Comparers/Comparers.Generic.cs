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
    public class SByteComparersTests : ComparersGenericTests<sbyte> { }
    public class ByteComparersTests : ComparersGenericTests<byte> { }
    public class Int16ComparersTests : ComparersGenericTests<short> { }
    public class UInt16ComparersTests : ComparersGenericTests<ushort> { }
    public class Int32ComparersTests : ComparersGenericTests<int> { }
    public class UInt32ComparersTests : ComparersGenericTests<uint> { }
    public class Int64ComparersTests : ComparersGenericTests<long> { }
    public class UInt64ComparersTests : ComparersGenericTests<ulong> { }
    public class IntPtrComparersTests : ComparersGenericTests<IntPtr> { }
    public class UIntPtrComparersTests : ComparersGenericTests<UIntPtr> { }
    public class SingleComparersTests : ComparersGenericTests<float> { }
    public class DoubleComparersTests : ComparersGenericTests<double> { }
    public class DecimalComparersTests : ComparersGenericTests<decimal> { }
    public class StringComparersTests : ComparersGenericTests<string> { }

    // Nullables are handled specially
    public class NullableInt32ComparersTests : ComparersGenericTests<int?> { }
    public class NullableUInt32ComparersTests : ComparersGenericTests<uint?> { }
    public class NullableIntPtrComparersTests : ComparersGenericTests<IntPtr?> { }
    public class NullableUIntPtrComparersTests : ComparersGenericTests<UIntPtr?> { }

    // Currently the Default properties are special-cased for enums depending
    // on their underlying type (byte, short, ulong, etc.)
    // We should cover all of those.
    public class SByteEnumComparersTests : ComparersGenericTests<SByteEnum> { }
    public class ByteEnumComparersTests : ComparersGenericTests<ByteEnum> { }
    public class Int16EnumComparersTests : ComparersGenericTests<Int16Enum> { }
    public class UInt16EnumComparersTests : ComparersGenericTests<UInt16Enum> { }
    public class Int32EnumComparersTests : ComparersGenericTests<Int32Enum> { }
    public class UInt32EnumComparersTests : ComparersGenericTests<UInt32Enum> { }
    public class Int64EnumComparersTests : ComparersGenericTests<Int64Enum> { }
    public class UInt64EnumComparersTests : ComparersGenericTests<UInt64Enum> { }

    // Default properties currently will be special-cased for T : enum and
    // T : U? where U : {IComparable,IEquatable}<U>, but not if T : U? where U : enum
    // So let's cover those cases as well
    public class NullableSByteEnumComparersTests : ComparersGenericTests<SByteEnum?> { }
    public class NullableByteEnumComparersTests : ComparersGenericTests<ByteEnum?> { }
    public class NullableInt16EnumComparersTests : ComparersGenericTests<Int16Enum?> { }
    public class NullableUInt16EnumComparersTests : ComparersGenericTests<UInt16Enum?> { }
    public class NullableInt32EnumComparersTests : ComparersGenericTests<Int32Enum?> { }
    public class NullableUInt32EnumComparersTests : ComparersGenericTests<UInt32Enum?> { }
    public class NullableInt64EnumComparersTests : ComparersGenericTests<Int64Enum?> { }
    public class NullableUInt64EnumComparersTests : ComparersGenericTests<UInt64Enum?> { }

    // Comparer<T>.Default should still work OK with non-IComparables
    public class ObjectComparersTests : ComparersGenericTests<object> { }

    // Other cases: IComparable<T>, IComparable, and both
    public class GenericComparableComparersTests : ComparersGenericTests<GenericComparable> { }
    public class NonGenericComparableComparersTests : ComparersGenericTests<NonGenericComparable> { }
    public class BadlyBehavingComparableComparersTests : ComparersGenericTests<BadlyBehavingComparable> { }

    // IEquatable<T>
    public class EquatableComparersTests : ComparersGenericTests<Equatable> { }
}
