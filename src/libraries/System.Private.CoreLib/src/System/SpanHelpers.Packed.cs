// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

#pragma warning disable IDE0060 // https://github.com/dotnet/roslyn-analyzers/issues/6228

#pragma warning disable 8500 // sizeof of managed types

namespace System
{
    // This is a separate class instead of 'partial SpanHelpers' to hide the private helpers
    // included in this file which are specific to the packed implementation.
    internal static partial class PackedSpanHelpers
    {
        // We only do this optimization if we have support for X86 intrinsics (Sse2) as the packing is noticeably cheaper compared to ARM (AdvSimd).
        // While the impact on the worst-case (match at the start) is minimal on X86, it's prohibitively large on ARM.
        public static bool PackedIndexOfIsSupported => Sse2.IsSupported;

        // Not all values can benefit from packing the searchSpace. See comments in PackSources below.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe bool CanUsePackedIndexOf<T>(T value)
        {
            Debug.Assert(PackedIndexOfIsSupported);
            Debug.Assert(RuntimeHelpers.IsBitwiseEquatable<T>());
            Debug.Assert(sizeof(T) == sizeof(ushort));

            return *(ushort*)&value - 1u < 254u;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CompExactlyDependsOn(typeof(Sse2))]
        public static int IndexOf(ref char searchSpace, char value, int length) =>
            IndexOf<SpanHelpers.DontNegate<short>, NopTransform>(ref Unsafe.As<char, short>(ref searchSpace), (short)value, length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CompExactlyDependsOn(typeof(Sse2))]
        public static int IndexOfAnyExcept(ref char searchSpace, char value, int length) =>
            IndexOf<SpanHelpers.Negate<short>, NopTransform>(ref Unsafe.As<char, short>(ref searchSpace), (short)value, length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CompExactlyDependsOn(typeof(Sse2))]
        public static int IndexOfAny(ref char searchSpace, char value0, char value1, int length) =>
            IndexOfAny<SpanHelpers.DontNegate<short>, NopTransform>(ref Unsafe.As<char, short>(ref searchSpace), (short)value0, (short)value1, length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CompExactlyDependsOn(typeof(Sse2))]
        public static int IndexOfAnyExcept(ref char searchSpace, char value0, char value1, int length) =>
            IndexOfAny<SpanHelpers.Negate<short>, NopTransform>(ref Unsafe.As<char, short>(ref searchSpace), (short)value0, (short)value1, length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CompExactlyDependsOn(typeof(Sse2))]
        public static int IndexOfAny(ref char searchSpace, char value0, char value1, char value2, int length) =>
            IndexOfAny<SpanHelpers.DontNegate<short>, NopTransform>(ref Unsafe.As<char, short>(ref searchSpace), (short)value0, (short)value1, (short)value2, length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CompExactlyDependsOn(typeof(Sse2))]
        public static int IndexOfAnyExcept(ref char searchSpace, char value0, char value1, char value2, int length) =>
            IndexOfAny<SpanHelpers.Negate<short>, NopTransform>(ref Unsafe.As<char, short>(ref searchSpace), (short)value0, (short)value1, (short)value2, length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CompExactlyDependsOn(typeof(Sse2))]
        public static int IndexOfAnyIgnoreCase(ref char searchSpace, char value, int length)
        {
            Debug.Assert((value | 0x20) == value);

            return IndexOf<SpanHelpers.DontNegate<short>, Or20Transform>(ref Unsafe.As<char, short>(ref searchSpace), (short)value, length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CompExactlyDependsOn(typeof(Sse2))]
        public static int IndexOfAnyExceptIgnoreCase(ref char searchSpace, char value, int length)
        {
            Debug.Assert((value | 0x20) == value);

            return IndexOf<SpanHelpers.Negate<short>, Or20Transform>(ref Unsafe.As<char, short>(ref searchSpace), (short)value, length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CompExactlyDependsOn(typeof(Sse2))]
        public static int IndexOfAnyIgnoreCase(ref char searchSpace, char value0, char value1, int length)
        {
            Debug.Assert((value0 | 0x20) == value0);
            Debug.Assert((value1 | 0x20) == value1);

            return IndexOfAny<SpanHelpers.DontNegate<short>, Or20Transform>(ref Unsafe.As<char, short>(ref searchSpace), (short)value0, (short)value1, length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CompExactlyDependsOn(typeof(Sse2))]
        public static int IndexOfAnyExceptIgnoreCase(ref char searchSpace, char value0, char value1, int length)
        {
            Debug.Assert((value0 | 0x20) == value0);
            Debug.Assert((value1 | 0x20) == value1);

            return IndexOfAny<SpanHelpers.Negate<short>, Or20Transform>(ref Unsafe.As<char, short>(ref searchSpace), (short)value0, (short)value1, length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CompExactlyDependsOn(typeof(Sse2))]
        public static int IndexOfAnyIgnoreCase(ref char searchSpace, char value0, char value1, char value2, int length)
        {
            Debug.Assert((value0 | 0x20) == value0);
            Debug.Assert((value1 | 0x20) == value1);
            Debug.Assert((value2 | 0x20) == value2);

            return IndexOfAny<SpanHelpers.DontNegate<short>, Or20Transform>(ref Unsafe.As<char, short>(ref searchSpace), (short)value0, (short)value1, (short)value2, length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CompExactlyDependsOn(typeof(Sse2))]
        public static int IndexOfAnyExceptIgnoreCase(ref char searchSpace, char value0, char value1, char value2, int length)
        {
            Debug.Assert((value0 | 0x20) == value0);
            Debug.Assert((value1 | 0x20) == value1);
            Debug.Assert((value2 | 0x20) == value2);

            return IndexOfAny<SpanHelpers.Negate<short>, Or20Transform>(ref Unsafe.As<char, short>(ref searchSpace), (short)value0, (short)value1, (short)value2, length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CompExactlyDependsOn(typeof(Sse2))]
        public static int IndexOfAnyInRange(ref char searchSpace, char lowInclusive, char rangeInclusive, int length) =>
            IndexOfAnyInRange<SpanHelpers.DontNegate<short>>(ref Unsafe.As<char, short>(ref searchSpace), (short)lowInclusive, (short)rangeInclusive, length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CompExactlyDependsOn(typeof(Sse2))]
        public static int IndexOfAnyExceptInRange(ref char searchSpace, char lowInclusive, char rangeInclusive, int length) =>
            IndexOfAnyInRange<SpanHelpers.Negate<short>>(ref Unsafe.As<char, short>(ref searchSpace), (short)lowInclusive, (short)rangeInclusive, length);

        [CompExactlyDependsOn(typeof(Sse2))]
        public static bool Contains(ref short searchSpace, short value, int length)
        {
            Debug.Assert(CanUsePackedIndexOf(value));

            if (length < Vector128<short>.Count)
            {
                nuint offset = 0;

                if (length >= 4)
                {
                    length -= 4;

                    if (searchSpace == value ||
                        Unsafe.Add(ref searchSpace, 1) == value ||
                        Unsafe.Add(ref searchSpace, 2) == value ||
                        Unsafe.Add(ref searchSpace, 3) == value)
                    {
                        return true;
                    }

                    offset = 4;
                }

                while (length > 0)
                {
                    length -= 1;

                    if (Unsafe.Add(ref searchSpace, offset) == value)
                    {
                        return true;
                    }

                    offset += 1;
                }
            }
            else
            {
                ref short currentSearchSpace = ref searchSpace;
#pragma warning disable IntrinsicsInSystemPrivateCoreLibAttributeNotSpecificEnough // The else condition for this if statement is identical in semantics to Avx2 specific code
                if (Avx512BW.IsSupported && Vector512.IsHardwareAccelerated && length > Vector512<short>.Count)
#pragma warning restore IntrinsicsInSystemPrivateCoreLibAttributeNotSpecificEnough
                {
                    Vector512<byte> packedValue = Vector512.Create((byte)value);

                    if (length > 2 * Vector512<short>.Count)
                    {
                        // Process the input in chunks of 64 characters (2 * Vector512<short>).
                        // If the input length is a multiple of 64, don't consume the last 16 characters in this loop.
                        // Let the fallback below handle it instead. This is why the condition is
                        // ">" instead of ">=" above, and why "IsAddressLessThan" is used instead of "!IsAddressGreaterThan".
                        ref short twoVectorsAwayFromEnd = ref Unsafe.Add(ref searchSpace, length - (2 * Vector512<short>.Count));

                        do
                        {
                            Vector512<short> source0 = Vector512.LoadUnsafe(ref currentSearchSpace);
                            Vector512<short> source1 = Vector512.LoadUnsafe(ref currentSearchSpace, (nuint)Vector512<short>.Count);
                            Vector512<byte> packedSource = PackSources(source0, source1);

                            if (Vector512.EqualsAny(packedValue, packedSource))
                            {
                                return true;
                            }

                            currentSearchSpace = ref Unsafe.Add(ref currentSearchSpace, 2 * Vector512<short>.Count);
                        }
                        while (Unsafe.IsAddressLessThan(ref currentSearchSpace, ref twoVectorsAwayFromEnd));
                    }

                    // We have 1-32 characters remaining. Process the first and last vector in the search space.
                    // They may overlap, but we're only interested in whether any value matched.
                    {
                        ref short oneVectorAwayFromEnd = ref Unsafe.Add(ref searchSpace, length - Vector512<short>.Count);

                        ref short firstVector = ref Unsafe.IsAddressGreaterThan(ref currentSearchSpace, ref oneVectorAwayFromEnd)
                            ? ref oneVectorAwayFromEnd
                            : ref currentSearchSpace;

                        Vector512<short> source0 = Vector512.LoadUnsafe(ref firstVector);
                        Vector512<short> source1 = Vector512.LoadUnsafe(ref oneVectorAwayFromEnd);
                        Vector512<byte> packedSource = PackSources(source0, source1);

                        if (Vector512.EqualsAny(packedValue, packedSource))
                        {
                            return true;
                        }
                    }
                }
#pragma warning disable IntrinsicsInSystemPrivateCoreLibAttributeNotSpecificEnough // The else condition for this if statement is identical in semantics to Avx2 specific code
                else if (Avx2.IsSupported && length > Vector256<short>.Count)
#pragma warning restore IntrinsicsInSystemPrivateCoreLibAttributeNotSpecificEnough
                {
                    Vector256<byte> packedValue = Vector256.Create((byte)value);

                    if (length > 2 * Vector256<short>.Count)
                    {
                        // Process the input in chunks of 32 characters (2 * Vector256<short>).
                        // If the input length is a multiple of 32, don't consume the last 16 characters in this loop.
                        // Let the fallback below handle it instead. This is why the condition is
                        // ">" instead of ">=" above, and why "IsAddressLessThan" is used instead of "!IsAddressGreaterThan".
                        ref short twoVectorsAwayFromEnd = ref Unsafe.Add(ref searchSpace, length - (2 * Vector256<short>.Count));

                        do
                        {
                            Vector256<short> source0 = Vector256.LoadUnsafe(ref currentSearchSpace);
                            Vector256<short> source1 = Vector256.LoadUnsafe(ref currentSearchSpace, (nuint)Vector256<short>.Count);
                            Vector256<byte> packedSource = PackSources(source0, source1);
                            Vector256<byte> result = Vector256.Equals(packedValue, packedSource);

                            if (result != Vector256<byte>.Zero)
                            {
                                return true;
                            }

                            currentSearchSpace = ref Unsafe.Add(ref currentSearchSpace, 2 * Vector256<short>.Count);
                        }
                        while (Unsafe.IsAddressLessThan(ref currentSearchSpace, ref twoVectorsAwayFromEnd));
                    }

                    // We have 1-32 characters remaining. Process the first and last vector in the search space.
                    // They may overlap, but we're only interested in whether any value matched.
                    {
                        ref short oneVectorAwayFromEnd = ref Unsafe.Add(ref searchSpace, length - Vector256<short>.Count);

                        ref short firstVector = ref Unsafe.IsAddressGreaterThan(ref currentSearchSpace, ref oneVectorAwayFromEnd)
                            ? ref oneVectorAwayFromEnd
                            : ref currentSearchSpace;

                        Vector256<short> source0 = Vector256.LoadUnsafe(ref firstVector);
                        Vector256<short> source1 = Vector256.LoadUnsafe(ref oneVectorAwayFromEnd);
                        Vector256<byte> packedSource = PackSources(source0, source1);
                        Vector256<byte> result = Vector256.Equals(packedValue, packedSource);

                        if (result != Vector256<byte>.Zero)
                        {
                            return true;
                        }
                    }
                }
                else
                {
                    Vector128<byte> packedValue = Vector128.Create((byte)value);

#pragma warning disable IntrinsicsInSystemPrivateCoreLibConditionParsing // A negated IsSupported condition isn't parseable by the intrinsics analyzer, but in this case, it is only used in combination
                                                                         // with the check above of Avx2.IsSupported && length > Vector256<short>.Count which makes the logic
                                                                         // in this if statement dead code when Avx2.IsSupported. Presumably this negated IsSupported check is to assist the JIT in
                                                                         // not generating dead code.
#pragma warning disable IntrinsicsInSystemPrivateCoreLibAttributeNotSpecificEnough // This is paired with the check above, and since these if statements are contained in 1 function, the code
                                                                                   // may take a dependence on the JIT compiler producing a consistent value for the result of a call to IsSupported
                                                                                   // This logic MUST NOT be extracted to a helper function
                    if (!Avx2.IsSupported && length > 2 * Vector128<short>.Count)
#pragma warning restore IntrinsicsInSystemPrivateCoreLibAttributeNotSpecificEnough
#pragma warning restore IntrinsicsInSystemPrivateCoreLibConditionParsing
                    {
                        // Process the input in chunks of 16 characters (2 * Vector128<short>).
                        // If the input length is a multiple of 16, don't consume the last 16 characters in this loop.
                        // Let the fallback below handle it instead. This is why the condition is
                        // ">" instead of ">=" above, and why "IsAddressLessThan" is used instead of "!IsAddressGreaterThan".
                        ref short twoVectorsAwayFromEnd = ref Unsafe.Add(ref searchSpace, length - (2 * Vector128<short>.Count));

                        do
                        {
                            Vector128<short> source0 = Vector128.LoadUnsafe(ref currentSearchSpace);
                            Vector128<short> source1 = Vector128.LoadUnsafe(ref currentSearchSpace, (nuint)Vector128<short>.Count);
                            Vector128<byte> packedSource = PackSources(source0, source1);
                            Vector128<byte> result = Vector128.Equals(packedValue, packedSource);

                            if (result != Vector128<byte>.Zero)
                            {
                                return true;
                            }

                            currentSearchSpace = ref Unsafe.Add(ref currentSearchSpace, 2 * Vector128<short>.Count);
                        }
                        while (Unsafe.IsAddressLessThan(ref currentSearchSpace, ref twoVectorsAwayFromEnd));
                    }

                    // We have 1-16 characters remaining. Process the first and last vector in the search space.
                    // They may overlap, but we're only interested in whether any value matched.
                    {
                        ref short oneVectorAwayFromEnd = ref Unsafe.Add(ref searchSpace, length - Vector128<short>.Count);

                        ref short firstVector = ref Unsafe.IsAddressGreaterThan(ref currentSearchSpace, ref oneVectorAwayFromEnd)
                            ? ref oneVectorAwayFromEnd
                            : ref currentSearchSpace;

                        Vector128<short> source0 = Vector128.LoadUnsafe(ref firstVector);
                        Vector128<short> source1 = Vector128.LoadUnsafe(ref oneVectorAwayFromEnd);
                        Vector128<byte> packedSource = PackSources(source0, source1);
                        Vector128<byte> result = Vector128.Equals(packedValue, packedSource);

                        if (result != Vector128<byte>.Zero)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        [CompExactlyDependsOn(typeof(Sse2))]
        private static int IndexOf<TNegator, TTransform>(ref short searchSpace, short value, int length)
            where TNegator : struct, SpanHelpers.INegator<short>
            where TTransform : struct, ITransform
        {
            Debug.Assert(CanUsePackedIndexOf(value));

            if (length < Vector128<short>.Count)
            {
                nuint offset = 0;

                if (length >= 4)
                {
                    length -= 4;

                    if (TNegator.NegateIfNeeded(TTransform.TransformInput(searchSpace) == value)) return 0;
                    if (TNegator.NegateIfNeeded(TTransform.TransformInput(Unsafe.Add(ref searchSpace, 1)) == value)) return 1;
                    if (TNegator.NegateIfNeeded(TTransform.TransformInput(Unsafe.Add(ref searchSpace, 2)) == value)) return 2;
                    if (TNegator.NegateIfNeeded(TTransform.TransformInput(Unsafe.Add(ref searchSpace, 3)) == value)) return 3;

                    offset = 4;
                }

                while (length > 0)
                {
                    length -= 1;

                    if (TNegator.NegateIfNeeded(TTransform.TransformInput(Unsafe.Add(ref searchSpace, offset)) == value)) return (int)offset;

                    offset += 1;
                }
            }
            else
            {
                ref short currentSearchSpace = ref searchSpace;

#pragma warning disable IntrinsicsInSystemPrivateCoreLibAttributeNotSpecificEnough // The else condition for this if statement is identical in semantics to Avx2 specific code
                if (Avx512BW.IsSupported && Vector512.IsHardwareAccelerated && length > Vector512<short>.Count)
#pragma warning restore IntrinsicsInSystemPrivateCoreLibAttributeNotSpecificEnough
                {
                    Vector512<byte> packedValue = Vector512.Create((byte)value);

                    if (length > 2 * Vector512<short>.Count)
                    {
                        // Process the input in chunks of 64 characters (2 * Vector512<short>).
                        // If the input length is a multiple of 64, don't consume the last 16 characters in this loop.
                        // Let the fallback below handle it instead. This is why the condition is
                        // ">" instead of ">=" above, and why "IsAddressLessThan" is used instead of "!IsAddressGreaterThan".
                        ref short twoVectorsAwayFromEnd = ref Unsafe.Add(ref searchSpace, length - (2 * Vector512<short>.Count));

                        do
                        {
                            Vector512<short> source0 = Vector512.LoadUnsafe(ref currentSearchSpace);
                            Vector512<short> source1 = Vector512.LoadUnsafe(ref currentSearchSpace, (nuint)Vector512<short>.Count);
                            Vector512<byte> packedSource = TTransform.TransformInput(PackSources(source0, source1));

                            if (HasMatch<TNegator>(packedValue, packedSource))
                            {
                                return ComputeFirstIndex(ref searchSpace, ref currentSearchSpace, GetMatchMask<TNegator>(packedValue, packedSource));
                            }

                            currentSearchSpace = ref Unsafe.Add(ref currentSearchSpace, 2 * Vector512<short>.Count);
                        }
                        while (Unsafe.IsAddressLessThan(ref currentSearchSpace, ref twoVectorsAwayFromEnd));
                    }

                    // We have 1-32 characters remaining. Process the first and last vector in the search space.
                    // They may overlap, but we'll handle that in the index calculation if we do get a match.
                    {
                        ref short oneVectorAwayFromEnd = ref Unsafe.Add(ref searchSpace, length - Vector512<short>.Count);

                        ref short firstVector = ref Unsafe.IsAddressGreaterThan(ref currentSearchSpace, ref oneVectorAwayFromEnd)
                            ? ref oneVectorAwayFromEnd
                            : ref currentSearchSpace;

                        Vector512<short> source0 = Vector512.LoadUnsafe(ref firstVector);
                        Vector512<short> source1 = Vector512.LoadUnsafe(ref oneVectorAwayFromEnd);
                        Vector512<byte> packedSource = TTransform.TransformInput(PackSources(source0, source1));

                        if (HasMatch<TNegator>(packedValue, packedSource))
                        {
                            return ComputeFirstIndexOverlapped(ref searchSpace, ref firstVector, ref oneVectorAwayFromEnd, GetMatchMask<TNegator>(packedValue, packedSource));
                        }
                    }
                }
#pragma warning disable IntrinsicsInSystemPrivateCoreLibAttributeNotSpecificEnough // The else condition for this if statement is identical in semantics to Avx2 specific code
                else if (Avx2.IsSupported && length > Vector256<short>.Count)
#pragma warning restore IntrinsicsInSystemPrivateCoreLibAttributeNotSpecificEnough
                {
                    Vector256<byte> packedValue = Vector256.Create((byte)value);

                    if (length > 2 * Vector256<short>.Count)
                    {
                        // Process the input in chunks of 32 characters (2 * Vector256<short>).
                        // If the input length is a multiple of 32, don't consume the last 16 characters in this loop.
                        // Let the fallback below handle it instead. This is why the condition is
                        // ">" instead of ">=" above, and why "IsAddressLessThan" is used instead of "!IsAddressGreaterThan".
                        ref short twoVectorsAwayFromEnd = ref Unsafe.Add(ref searchSpace, length - (2 * Vector256<short>.Count));

                        do
                        {
                            Vector256<short> source0 = Vector256.LoadUnsafe(ref currentSearchSpace);
                            Vector256<short> source1 = Vector256.LoadUnsafe(ref currentSearchSpace, (nuint)Vector256<short>.Count);
                            Vector256<byte> packedSource = TTransform.TransformInput(PackSources(source0, source1));
                            Vector256<byte> result = Vector256.Equals(packedValue, packedSource);
                            result = NegateIfNeeded<TNegator>(result);

                            if (result != Vector256<byte>.Zero)
                            {
                                return ComputeFirstIndex(ref searchSpace, ref currentSearchSpace, result);
                            }

                            currentSearchSpace = ref Unsafe.Add(ref currentSearchSpace, 2 * Vector256<short>.Count);
                        }
                        while (Unsafe.IsAddressLessThan(ref currentSearchSpace, ref twoVectorsAwayFromEnd));
                    }

                    // We have 1-32 characters remaining. Process the first and last vector in the search space.
                    // They may overlap, but we'll handle that in the index calculation if we do get a match.
                    {
                        ref short oneVectorAwayFromEnd = ref Unsafe.Add(ref searchSpace, length - Vector256<short>.Count);

                        ref short firstVector = ref Unsafe.IsAddressGreaterThan(ref currentSearchSpace, ref oneVectorAwayFromEnd)
                            ? ref oneVectorAwayFromEnd
                            : ref currentSearchSpace;

                        Vector256<short> source0 = Vector256.LoadUnsafe(ref firstVector);
                        Vector256<short> source1 = Vector256.LoadUnsafe(ref oneVectorAwayFromEnd);
                        Vector256<byte> packedSource = TTransform.TransformInput(PackSources(source0, source1));
                        Vector256<byte> result = Vector256.Equals(packedValue, packedSource);
                        result = NegateIfNeeded<TNegator>(result);

                        if (result != Vector256<byte>.Zero)
                        {
                            return ComputeFirstIndexOverlapped(ref searchSpace, ref firstVector, ref oneVectorAwayFromEnd, result);
                        }
                    }
                }
                else
                {
                    Vector128<byte> packedValue = Vector128.Create((byte)value);

#pragma warning disable IntrinsicsInSystemPrivateCoreLibConditionParsing // A negated IsSupported condition isn't parseable by the intrinsics analyzer, but in this case, it is only used in combination
                                                                         // with the check above of Avx2.IsSupported && length > Vector256<short>.Count which makes the logic
                                                                         // in this if statement dead code when Avx2.IsSupported. Presumably this negated IsSupported check is to assist the JIT in
                                                                         // not generating dead code.
#pragma warning disable IntrinsicsInSystemPrivateCoreLibAttributeNotSpecificEnough // This is paired with the check above, and since these if statements are contained in 1 function, the code
                                                                                   // may take a dependence on the JIT compiler producing a consistent value for the result of a call to IsSupported
                                                                                   // This logic MUST NOT be extracted to a helper function
                    if (!Avx2.IsSupported && length > 2 * Vector128<short>.Count)
#pragma warning restore IntrinsicsInSystemPrivateCoreLibAttributeNotSpecificEnough
#pragma warning restore IntrinsicsInSystemPrivateCoreLibConditionParsing
                    {
                        // Process the input in chunks of 16 characters (2 * Vector128<short>).
                        // If the input length is a multiple of 16, don't consume the last 16 characters in this loop.
                        // Let the fallback below handle it instead. This is why the condition is
                        // ">" instead of ">=" above, and why "IsAddressLessThan" is used instead of "!IsAddressGreaterThan".
                        ref short twoVectorsAwayFromEnd = ref Unsafe.Add(ref searchSpace, length - (2 * Vector128<short>.Count));

                        do
                        {
                            Vector128<short> source0 = Vector128.LoadUnsafe(ref currentSearchSpace);
                            Vector128<short> source1 = Vector128.LoadUnsafe(ref currentSearchSpace, (nuint)Vector128<short>.Count);
                            Vector128<byte> packedSource = TTransform.TransformInput(PackSources(source0, source1));
                            Vector128<byte> result = Vector128.Equals(packedValue, packedSource);
                            result = NegateIfNeeded<TNegator>(result);

                            if (result != Vector128<byte>.Zero)
                            {
                                return ComputeFirstIndex(ref searchSpace, ref currentSearchSpace, result);
                            }

                            currentSearchSpace = ref Unsafe.Add(ref currentSearchSpace, 2 * Vector128<short>.Count);
                        }
                        while (Unsafe.IsAddressLessThan(ref currentSearchSpace, ref twoVectorsAwayFromEnd));
                    }

                    // We have 1-16 characters remaining. Process the first and last vector in the search space.
                    // They may overlap, but we'll handle that in the index calculation if we do get a match.
                    {
                        ref short oneVectorAwayFromEnd = ref Unsafe.Add(ref searchSpace, length - Vector128<short>.Count);

                        ref short firstVector = ref Unsafe.IsAddressGreaterThan(ref currentSearchSpace, ref oneVectorAwayFromEnd)
                            ? ref oneVectorAwayFromEnd
                            : ref currentSearchSpace;

                        Vector128<short> source0 = Vector128.LoadUnsafe(ref firstVector);
                        Vector128<short> source1 = Vector128.LoadUnsafe(ref oneVectorAwayFromEnd);
                        Vector128<byte> packedSource = TTransform.TransformInput(PackSources(source0, source1));
                        Vector128<byte> result = Vector128.Equals(packedValue, packedSource);
                        result = NegateIfNeeded<TNegator>(result);

                        if (result != Vector128<byte>.Zero)
                        {
                            return ComputeFirstIndexOverlapped(ref searchSpace, ref firstVector, ref oneVectorAwayFromEnd, result);
                        }
                    }
                }
            }

            return -1;
        }

        [CompExactlyDependsOn(typeof(Sse2))]
        private static int IndexOfAny<TNegator, TTransform>(ref short searchSpace, short value0, short value1, int length)
            where TNegator : struct, SpanHelpers.INegator<short>
            where TTransform : struct, ITransform
        {
            Debug.Assert(CanUsePackedIndexOf(value0));
            Debug.Assert(CanUsePackedIndexOf(value1));

            if (length < Vector128<short>.Count)
            {
                nuint offset = 0;
                short lookUp;

                if (length >= 4)
                {
                    length -= 4;

                    lookUp = TTransform.TransformInput(searchSpace);
                    if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1)) return 0;
                    lookUp = TTransform.TransformInput(Unsafe.Add(ref searchSpace, 1));
                    if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1)) return 1;
                    lookUp = TTransform.TransformInput(Unsafe.Add(ref searchSpace, 2));
                    if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1)) return 2;
                    lookUp = TTransform.TransformInput(Unsafe.Add(ref searchSpace, 3));
                    if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1)) return 3;

                    offset = 4;
                }

                while (length > 0)
                {
                    length -= 1;

                    lookUp = TTransform.TransformInput(Unsafe.Add(ref searchSpace, offset));
                    if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1)) return (int)offset;

                    offset += 1;
                }
            }
            else
            {
                ref short currentSearchSpace = ref searchSpace;
#pragma warning disable IntrinsicsInSystemPrivateCoreLibAttributeNotSpecificEnough // The else condition for this if statement is identical in semantics to Avx2 specific code
                if (Avx512BW.IsSupported && Vector512.IsHardwareAccelerated && length > Vector512<short>.Count)
#pragma warning restore IntrinsicsInSystemPrivateCoreLibAttributeNotSpecificEnough
                {
                    Vector512<byte> packedValue0 = Vector512.Create((byte)value0);
                    Vector512<byte> packedValue1 = Vector512.Create((byte)value1);

                    if (length > 2 * Vector512<short>.Count)
                    {
                        // Process the input in chunks of 64 characters (2 * Vector512<short>).
                        // If the input length is a multiple of 64, don't consume the last 16 characters in this loop.
                        // Let the fallback below handle it instead. This is why the condition is
                        // ">" instead of ">=" above, and why "IsAddressLessThan" is used instead of "!IsAddressGreaterThan".
                        ref short twoVectorsAwayFromEnd = ref Unsafe.Add(ref searchSpace, length - (2 * Vector512<short>.Count));

                        do
                        {
                            Vector512<short> source0 = Vector512.LoadUnsafe(ref currentSearchSpace);
                            Vector512<short> source1 = Vector512.LoadUnsafe(ref currentSearchSpace, (nuint)Vector512<short>.Count);
                            Vector512<byte> packedSource = TTransform.TransformInput(PackSources(source0, source1));
                            Vector512<byte> result = NegateIfNeeded<TNegator>(Vector512.Equals(packedValue0, packedSource) | Vector512.Equals(packedValue1, packedSource));

                            if (result != Vector512<byte>.Zero)
                            {
                                return ComputeFirstIndex(ref searchSpace, ref currentSearchSpace, result);
                            }

                            currentSearchSpace = ref Unsafe.Add(ref currentSearchSpace, 2 * Vector512<short>.Count);
                        }
                        while (Unsafe.IsAddressLessThan(ref currentSearchSpace, ref twoVectorsAwayFromEnd));
                    }

                    // We have 1-32 characters remaining. Process the first and last vector in the search space.
                    // They may overlap, but we'll handle that in the index calculation if we do get a match.
                    {
                        ref short oneVectorAwayFromEnd = ref Unsafe.Add(ref searchSpace, length - Vector512<short>.Count);

                        ref short firstVector = ref Unsafe.IsAddressGreaterThan(ref currentSearchSpace, ref oneVectorAwayFromEnd)
                            ? ref oneVectorAwayFromEnd
                            : ref currentSearchSpace;

                        Vector512<short> source0 = Vector512.LoadUnsafe(ref firstVector);
                        Vector512<short> source1 = Vector512.LoadUnsafe(ref oneVectorAwayFromEnd);
                        Vector512<byte> packedSource = TTransform.TransformInput(PackSources(source0, source1));
                        Vector512<byte> result = NegateIfNeeded<TNegator>(Vector512.Equals(packedValue0, packedSource) | Vector512.Equals(packedValue1, packedSource));

                        if (result != Vector512<byte>.Zero)
                        {
                            return ComputeFirstIndexOverlapped(ref searchSpace, ref firstVector, ref oneVectorAwayFromEnd, result);
                        }
                    }
                }
#pragma warning disable IntrinsicsInSystemPrivateCoreLibAttributeNotSpecificEnough // The else condition for this if statement is identical in semantics to Avx2 specific code
                else if (Avx2.IsSupported && length > Vector256<short>.Count)
#pragma warning restore IntrinsicsInSystemPrivateCoreLibAttributeNotSpecificEnough
                {
                    Vector256<byte> packedValue0 = Vector256.Create((byte)value0);
                    Vector256<byte> packedValue1 = Vector256.Create((byte)value1);

                    if (length > 2 * Vector256<short>.Count)
                    {
                        // Process the input in chunks of 32 characters (2 * Vector256<short>).
                        // If the input length is a multiple of 32, don't consume the last 16 characters in this loop.
                        // Let the fallback below handle it instead. This is why the condition is
                        // ">" instead of ">=" above, and why "IsAddressLessThan" is used instead of "!IsAddressGreaterThan".
                        ref short twoVectorsAwayFromEnd = ref Unsafe.Add(ref searchSpace, length - (2 * Vector256<short>.Count));

                        do
                        {
                            Vector256<short> source0 = Vector256.LoadUnsafe(ref currentSearchSpace);
                            Vector256<short> source1 = Vector256.LoadUnsafe(ref currentSearchSpace, (nuint)Vector256<short>.Count);
                            Vector256<byte> packedSource = TTransform.TransformInput(PackSources(source0, source1));
                            Vector256<byte> result = Vector256.Equals(packedValue0, packedSource) | Vector256.Equals(packedValue1, packedSource);
                            result = NegateIfNeeded<TNegator>(result);

                            if (result != Vector256<byte>.Zero)
                            {
                                return ComputeFirstIndex(ref searchSpace, ref currentSearchSpace, result);
                            }

                            currentSearchSpace = ref Unsafe.Add(ref currentSearchSpace, 2 * Vector256<short>.Count);
                        }
                        while (Unsafe.IsAddressLessThan(ref currentSearchSpace, ref twoVectorsAwayFromEnd));
                    }

                    // We have 1-32 characters remaining. Process the first and last vector in the search space.
                    // They may overlap, but we'll handle that in the index calculation if we do get a match.
                    {
                        ref short oneVectorAwayFromEnd = ref Unsafe.Add(ref searchSpace, length - Vector256<short>.Count);

                        ref short firstVector = ref Unsafe.IsAddressGreaterThan(ref currentSearchSpace, ref oneVectorAwayFromEnd)
                            ? ref oneVectorAwayFromEnd
                            : ref currentSearchSpace;

                        Vector256<short> source0 = Vector256.LoadUnsafe(ref firstVector);
                        Vector256<short> source1 = Vector256.LoadUnsafe(ref oneVectorAwayFromEnd);
                        Vector256<byte> packedSource = TTransform.TransformInput(PackSources(source0, source1));
                        Vector256<byte> result = Vector256.Equals(packedValue0, packedSource) | Vector256.Equals(packedValue1, packedSource);
                        result = NegateIfNeeded<TNegator>(result);

                        if (result != Vector256<byte>.Zero)
                        {
                            return ComputeFirstIndexOverlapped(ref searchSpace, ref firstVector, ref oneVectorAwayFromEnd, result);
                        }
                    }
                }
                else
                {
                    Vector128<byte> packedValue0 = Vector128.Create((byte)value0);
                    Vector128<byte> packedValue1 = Vector128.Create((byte)value1);

#pragma warning disable IntrinsicsInSystemPrivateCoreLibConditionParsing // A negated IsSupported condition isn't parseable by the intrinsics analyzer, but in this case, it is only used in combination
                                                                         // with the check above of Avx2.IsSupported && length > Vector256<short>.Count which makes the logic
                                                                         // in this if statement dead code when Avx2.IsSupported. Presumably this negated IsSupported check is to assist the JIT in
                                                                         // not generating dead code.
#pragma warning disable IntrinsicsInSystemPrivateCoreLibAttributeNotSpecificEnough // This is paired with the check above, and since these if statements are contained in 1 function, the code
                                                                                   // may take a dependence on the JIT compiler producing a consistent value for the result of a call to IsSupported
                                                                                   // This logic MUST NOT be extracted to a helper function
                    if (!Avx2.IsSupported && length > 2 * Vector128<short>.Count)
#pragma warning restore IntrinsicsInSystemPrivateCoreLibAttributeNotSpecificEnough
#pragma warning restore IntrinsicsInSystemPrivateCoreLibConditionParsing
                    {
                        // Process the input in chunks of 16 characters (2 * Vector128<short>).
                        // If the input length is a multiple of 16, don't consume the last 16 characters in this loop.
                        // Let the fallback below handle it instead. This is why the condition is
                        // ">" instead of ">=" above, and why "IsAddressLessThan" is used instead of "!IsAddressGreaterThan".
                        ref short twoVectorsAwayFromEnd = ref Unsafe.Add(ref searchSpace, length - (2 * Vector128<short>.Count));

                        do
                        {
                            Vector128<short> source0 = Vector128.LoadUnsafe(ref currentSearchSpace);
                            Vector128<short> source1 = Vector128.LoadUnsafe(ref currentSearchSpace, (nuint)Vector128<short>.Count);
                            Vector128<byte> packedSource = TTransform.TransformInput(PackSources(source0, source1));
                            Vector128<byte> result = Vector128.Equals(packedValue0, packedSource) | Vector128.Equals(packedValue1, packedSource);
                            result = NegateIfNeeded<TNegator>(result);

                            if (result != Vector128<byte>.Zero)
                            {
                                return ComputeFirstIndex(ref searchSpace, ref currentSearchSpace, result);
                            }

                            currentSearchSpace = ref Unsafe.Add(ref currentSearchSpace, 2 * Vector128<short>.Count);
                        }
                        while (Unsafe.IsAddressLessThan(ref currentSearchSpace, ref twoVectorsAwayFromEnd));
                    }

                    // We have 1-16 characters remaining. Process the first and last vector in the search space.
                    // They may overlap, but we'll handle that in the index calculation if we do get a match.
                    {
                        ref short oneVectorAwayFromEnd = ref Unsafe.Add(ref searchSpace, length - Vector128<short>.Count);

                        ref short firstVector = ref Unsafe.IsAddressGreaterThan(ref currentSearchSpace, ref oneVectorAwayFromEnd)
                            ? ref oneVectorAwayFromEnd
                            : ref currentSearchSpace;

                        Vector128<short> source0 = Vector128.LoadUnsafe(ref firstVector);
                        Vector128<short> source1 = Vector128.LoadUnsafe(ref oneVectorAwayFromEnd);
                        Vector128<byte> packedSource = TTransform.TransformInput(PackSources(source0, source1));
                        Vector128<byte> result = Vector128.Equals(packedValue0, packedSource) | Vector128.Equals(packedValue1, packedSource);
                        result = NegateIfNeeded<TNegator>(result);

                        if (result != Vector128<byte>.Zero)
                        {
                            return ComputeFirstIndexOverlapped(ref searchSpace, ref firstVector, ref oneVectorAwayFromEnd, result);
                        }
                    }
                }
            }

            return -1;
        }

        [CompExactlyDependsOn(typeof(Sse2))]
        private static int IndexOfAny<TNegator, TTransform>(ref short searchSpace, short value0, short value1, short value2, int length)
            where TNegator : struct, SpanHelpers.INegator<short>
            where TTransform : struct, ITransform
        {
            Debug.Assert(CanUsePackedIndexOf(value0));
            Debug.Assert(CanUsePackedIndexOf(value1));
            Debug.Assert(CanUsePackedIndexOf(value2));

            if (length < Vector128<short>.Count)
            {
                nuint offset = 0;
                short lookUp;

                if (length >= 4)
                {
                    length -= 4;

                    lookUp = TTransform.TransformInput(searchSpace);
                    if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1 || lookUp == value2)) return 0;
                    lookUp = TTransform.TransformInput(Unsafe.Add(ref searchSpace, 1));
                    if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1 || lookUp == value2)) return 1;
                    lookUp = TTransform.TransformInput(Unsafe.Add(ref searchSpace, 2));
                    if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1 || lookUp == value2)) return 2;
                    lookUp = TTransform.TransformInput(Unsafe.Add(ref searchSpace, 3));
                    if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1 || lookUp == value2)) return 3;

                    offset = 4;
                }

                while (length > 0)
                {
                    length -= 1;

                    lookUp = TTransform.TransformInput(Unsafe.Add(ref searchSpace, offset));
                    if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1 || lookUp == value2)) return (int)offset;

                    offset += 1;
                }
            }
            else
            {
                ref short currentSearchSpace = ref searchSpace;

#pragma warning disable IntrinsicsInSystemPrivateCoreLibAttributeNotSpecificEnough // The else condition for this if statement is identical in semantics to Avx2 specific code
                if (Avx512BW.IsSupported && Vector512.IsHardwareAccelerated && length > Vector512<short>.Count)
#pragma warning restore IntrinsicsInSystemPrivateCoreLibAttributeNotSpecificEnough
                {
                    Vector512<byte> packedValue0 = Vector512.Create((byte)value0);
                    Vector512<byte> packedValue1 = Vector512.Create((byte)value1);
                    Vector512<byte> packedValue2 = Vector512.Create((byte)value2);

                    if (length > 2 * Vector512<short>.Count)
                    {
                        // Process the input in chunks of 64 characters (2 * Vector512<short>).
                        // If the input length is a multiple of 64, don't consume the last 16 characters in this loop.
                        // Let the fallback below handle it instead. This is why the condition is
                        // ">" instead of ">=" above, and why "IsAddressLessThan" is used instead of "!IsAddressGreaterThan".
                        ref short twoVectorsAwayFromEnd = ref Unsafe.Add(ref searchSpace, length - (2 * Vector512<short>.Count));

                        do
                        {
                            Vector512<short> source0 = Vector512.LoadUnsafe(ref currentSearchSpace);
                            Vector512<short> source1 = Vector512.LoadUnsafe(ref currentSearchSpace, (nuint)Vector512<short>.Count);
                            Vector512<byte> packedSource = TTransform.TransformInput(PackSources(source0, source1));
                            Vector512<byte> result = NegateIfNeeded<TNegator>(Vector512.Equals(packedValue0, packedSource) | Vector512.Equals(packedValue1, packedSource) | Vector512.Equals(packedValue2, packedSource));

                            if (result != Vector512<byte>.Zero)
                            {
                                return ComputeFirstIndex(ref searchSpace, ref currentSearchSpace, result);
                            }

                            currentSearchSpace = ref Unsafe.Add(ref currentSearchSpace, 2 * Vector512<short>.Count);
                        }
                        while (Unsafe.IsAddressLessThan(ref currentSearchSpace, ref twoVectorsAwayFromEnd));
                    }

                    // We have 1-32 characters remaining. Process the first and last vector in the search space.
                    // They may overlap, but we'll handle that in the index calculation if we do get a match.
                    {
                        ref short oneVectorAwayFromEnd = ref Unsafe.Add(ref searchSpace, length - Vector512<short>.Count);

                        ref short firstVector = ref Unsafe.IsAddressGreaterThan(ref currentSearchSpace, ref oneVectorAwayFromEnd)
                            ? ref oneVectorAwayFromEnd
                            : ref currentSearchSpace;

                        Vector512<short> source0 = Vector512.LoadUnsafe(ref firstVector);
                        Vector512<short> source1 = Vector512.LoadUnsafe(ref oneVectorAwayFromEnd);
                        Vector512<byte> packedSource = TTransform.TransformInput(PackSources(source0, source1));
                        Vector512<byte> result = NegateIfNeeded<TNegator>(Vector512.Equals(packedValue0, packedSource) | Vector512.Equals(packedValue1, packedSource) | Vector512.Equals(packedValue2, packedSource));

                        if (result != Vector512<byte>.Zero)
                        {
                            return ComputeFirstIndexOverlapped(ref searchSpace, ref firstVector, ref oneVectorAwayFromEnd, result);
                        }
                    }
                }
#pragma warning disable IntrinsicsInSystemPrivateCoreLibAttributeNotSpecificEnough // The else condition for this if statement is identical in semantics to Avx2 specific code
                else if (Avx2.IsSupported && length > Vector256<short>.Count)
#pragma warning restore IntrinsicsInSystemPrivateCoreLibAttributeNotSpecificEnough
                {
                    Vector256<byte> packedValue0 = Vector256.Create((byte)value0);
                    Vector256<byte> packedValue1 = Vector256.Create((byte)value1);
                    Vector256<byte> packedValue2 = Vector256.Create((byte)value2);

                    if (length > 2 * Vector256<short>.Count)
                    {
                        // Process the input in chunks of 32 characters (2 * Vector256<short>).
                        // If the input length is a multiple of 32, don't consume the last 16 characters in this loop.
                        // Let the fallback below handle it instead. This is why the condition is
                        // ">" instead of ">=" above, and why "IsAddressLessThan" is used instead of "!IsAddressGreaterThan".
                        ref short twoVectorsAwayFromEnd = ref Unsafe.Add(ref searchSpace, length - (2 * Vector256<short>.Count));

                        do
                        {
                            Vector256<short> source0 = Vector256.LoadUnsafe(ref currentSearchSpace);
                            Vector256<short> source1 = Vector256.LoadUnsafe(ref currentSearchSpace, (nuint)Vector256<short>.Count);
                            Vector256<byte> packedSource = TTransform.TransformInput(PackSources(source0, source1));
                            Vector256<byte> result = Vector256.Equals(packedValue0, packedSource) | Vector256.Equals(packedValue1, packedSource) | Vector256.Equals(packedValue2, packedSource);
                            result = NegateIfNeeded<TNegator>(result);

                            if (result != Vector256<byte>.Zero)
                            {
                                return ComputeFirstIndex(ref searchSpace, ref currentSearchSpace, result);
                            }

                            currentSearchSpace = ref Unsafe.Add(ref currentSearchSpace, 2 * Vector256<short>.Count);
                        }
                        while (Unsafe.IsAddressLessThan(ref currentSearchSpace, ref twoVectorsAwayFromEnd));
                    }

                    // We have 1-32 characters remaining. Process the first and last vector in the search space.
                    // They may overlap, but we'll handle that in the index calculation if we do get a match.
                    {
                        ref short oneVectorAwayFromEnd = ref Unsafe.Add(ref searchSpace, length - Vector256<short>.Count);

                        ref short firstVector = ref Unsafe.IsAddressGreaterThan(ref currentSearchSpace, ref oneVectorAwayFromEnd)
                            ? ref oneVectorAwayFromEnd
                            : ref currentSearchSpace;

                        Vector256<short> source0 = Vector256.LoadUnsafe(ref firstVector);
                        Vector256<short> source1 = Vector256.LoadUnsafe(ref oneVectorAwayFromEnd);
                        Vector256<byte> packedSource = TTransform.TransformInput(PackSources(source0, source1));
                        Vector256<byte> result = Vector256.Equals(packedValue0, packedSource) | Vector256.Equals(packedValue1, packedSource) | Vector256.Equals(packedValue2, packedSource);
                        result = NegateIfNeeded<TNegator>(result);

                        if (result != Vector256<byte>.Zero)
                        {
                            return ComputeFirstIndexOverlapped(ref searchSpace, ref firstVector, ref oneVectorAwayFromEnd, result);
                        }
                    }
                }
                else
                {
                    Vector128<byte> packedValue0 = Vector128.Create((byte)value0);
                    Vector128<byte> packedValue1 = Vector128.Create((byte)value1);
                    Vector128<byte> packedValue2 = Vector128.Create((byte)value2);

#pragma warning disable IntrinsicsInSystemPrivateCoreLibConditionParsing // A negated IsSupported condition isn't parseable by the intrinsics analyzer, but in this case, it is only used in combination
                                                                         // with the check above of Avx2.IsSupported && length > Vector256<short>.Count which makes the logic
                                                                         // in this if statement dead code when Avx2.IsSupported. Presumably this negated IsSupported check is to assist the JIT in
                                                                         // not generating dead code.
#pragma warning disable IntrinsicsInSystemPrivateCoreLibAttributeNotSpecificEnough // This is paired with the check above, and since these if statements are contained in 1 function, the code
                                                                                   // may take a dependence on the JIT compiler producing a consistent value for the result of a call to IsSupported
                                                                                   // This logic MUST NOT be extracted to a helper function
                    if (!Avx2.IsSupported && length > 2 * Vector128<short>.Count)
#pragma warning restore IntrinsicsInSystemPrivateCoreLibAttributeNotSpecificEnough
#pragma warning restore IntrinsicsInSystemPrivateCoreLibConditionParsing
                    {
                        // Process the input in chunks of 16 characters (2 * Vector128<short>).
                        // If the input length is a multiple of 16, don't consume the last 16 characters in this loop.
                        // Let the fallback below handle it instead. This is why the condition is
                        // ">" instead of ">=" above, and why "IsAddressLessThan" is used instead of "!IsAddressGreaterThan".
                        ref short twoVectorsAwayFromEnd = ref Unsafe.Add(ref searchSpace, length - (2 * Vector128<short>.Count));

                        do
                        {
                            Vector128<short> source0 = Vector128.LoadUnsafe(ref currentSearchSpace);
                            Vector128<short> source1 = Vector128.LoadUnsafe(ref currentSearchSpace, (nuint)Vector128<short>.Count);
                            Vector128<byte> packedSource = TTransform.TransformInput(PackSources(source0, source1));
                            Vector128<byte> result = Vector128.Equals(packedValue0, packedSource) | Vector128.Equals(packedValue1, packedSource) | Vector128.Equals(packedValue2, packedSource);
                            result = NegateIfNeeded<TNegator>(result);

                            if (result != Vector128<byte>.Zero)
                            {
                                return ComputeFirstIndex(ref searchSpace, ref currentSearchSpace, result);
                            }

                            currentSearchSpace = ref Unsafe.Add(ref currentSearchSpace, 2 * Vector128<short>.Count);
                        }
                        while (Unsafe.IsAddressLessThan(ref currentSearchSpace, ref twoVectorsAwayFromEnd));
                    }

                    // We have 1-16 characters remaining. Process the first and last vector in the search space.
                    // They may overlap, but we'll handle that in the index calculation if we do get a match.
                    {
                        ref short oneVectorAwayFromEnd = ref Unsafe.Add(ref searchSpace, length - Vector128<short>.Count);

                        ref short firstVector = ref Unsafe.IsAddressGreaterThan(ref currentSearchSpace, ref oneVectorAwayFromEnd)
                            ? ref oneVectorAwayFromEnd
                            : ref currentSearchSpace;

                        Vector128<short> source0 = Vector128.LoadUnsafe(ref firstVector);
                        Vector128<short> source1 = Vector128.LoadUnsafe(ref oneVectorAwayFromEnd);
                        Vector128<byte> packedSource = TTransform.TransformInput(PackSources(source0, source1));
                        Vector128<byte> result = Vector128.Equals(packedValue0, packedSource) | Vector128.Equals(packedValue1, packedSource) | Vector128.Equals(packedValue2, packedSource);
                        result = NegateIfNeeded<TNegator>(result);

                        if (result != Vector128<byte>.Zero)
                        {
                            return ComputeFirstIndexOverlapped(ref searchSpace, ref firstVector, ref oneVectorAwayFromEnd, result);
                        }
                    }
                }
            }

            return -1;
        }

        [CompExactlyDependsOn(typeof(Sse2))]
        private static int IndexOfAnyInRange<TNegator>(ref short searchSpace, short lowInclusive, short rangeInclusive, int length)
            where TNegator : struct, SpanHelpers.INegator<short>
        {
            Debug.Assert(CanUsePackedIndexOf(lowInclusive));
            Debug.Assert(CanUsePackedIndexOf((short)(lowInclusive + rangeInclusive)));
            Debug.Assert(rangeInclusive >= 0);

            if (length < Vector128<short>.Count)
            {
                uint lowInclusiveUint = (uint)lowInclusive;
                uint rangeInclusiveUint = (uint)rangeInclusive;
                for (int i = 0; i < length; i++)
                {
                    uint current = (uint)Unsafe.Add(ref searchSpace, i);
                    if (TNegator.NegateIfNeeded((current - lowInclusiveUint) <= rangeInclusiveUint))
                    {
                        return i;
                    }
                }
            }
            else
            {
                ref short currentSearchSpace = ref searchSpace;

#pragma warning disable IntrinsicsInSystemPrivateCoreLibAttributeNotSpecificEnough // The else condition for this if statement is identical in semantics to Avx2 specific code
                if (Avx512BW.IsSupported && Vector512.IsHardwareAccelerated && length > Vector512<short>.Count)
#pragma warning restore IntrinsicsInSystemPrivateCoreLibAttributeNotSpecificEnough
                {
                    Vector512<byte> lowVector = Vector512.Create((byte)lowInclusive);
                    Vector512<byte> rangeVector = Vector512.Create((byte)rangeInclusive);

                    if (length > 2 * Vector512<short>.Count)
                    {
                        // Process the input in chunks of 64 characters (2 * Vector512<short>).
                        // If the input length is a multiple of 64, don't consume the last 16 characters in this loop.
                        // Let the fallback below handle it instead. This is why the condition is
                        // ">" instead of ">=" above, and why "IsAddressLessThan" is used instead of "!IsAddressGreaterThan".
                        ref short twoVectorsAwayFromEnd = ref Unsafe.Add(ref searchSpace, length - (2 * Vector512<short>.Count));

                        do
                        {
                            Vector512<short> source0 = Vector512.LoadUnsafe(ref currentSearchSpace);
                            Vector512<short> source1 = Vector512.LoadUnsafe(ref currentSearchSpace, (nuint)Vector512<short>.Count);
                            Vector512<byte> packedSource = PackSources(source0, source1) - lowVector;

                            if (HasMatchInRange<TNegator>(packedSource, rangeVector))
                            {
                                return ComputeFirstIndex(ref searchSpace, ref currentSearchSpace, GetMatchInRangeMask<TNegator>(packedSource, rangeVector));
                            }

                            currentSearchSpace = ref Unsafe.Add(ref currentSearchSpace, 2 * Vector512<short>.Count);
                        }
                        while (Unsafe.IsAddressLessThan(ref currentSearchSpace, ref twoVectorsAwayFromEnd));
                    }

                    // We have 1-32 characters remaining. Process the first and last vector in the search space.
                    // They may overlap, but we'll handle that in the index calculation if we do get a match.
                    {
                        ref short oneVectorAwayFromEnd = ref Unsafe.Add(ref searchSpace, length - Vector512<short>.Count);

                        ref short firstVector = ref Unsafe.IsAddressGreaterThan(ref currentSearchSpace, ref oneVectorAwayFromEnd)
                            ? ref oneVectorAwayFromEnd
                            : ref currentSearchSpace;

                        Vector512<short> source0 = Vector512.LoadUnsafe(ref firstVector);
                        Vector512<short> source1 = Vector512.LoadUnsafe(ref oneVectorAwayFromEnd);
                        Vector512<byte> packedSource = PackSources(source0, source1) - lowVector;

                        if (HasMatchInRange<TNegator>(packedSource, rangeVector))
                        {
                            return ComputeFirstIndexOverlapped(ref searchSpace, ref firstVector, ref oneVectorAwayFromEnd, GetMatchInRangeMask<TNegator>(packedSource, rangeVector));
                        }
                    }
                }
#pragma warning disable IntrinsicsInSystemPrivateCoreLibAttributeNotSpecificEnough // The else condition for this if statement is identical in semantics to Avx2 specific code
                else if (Avx2.IsSupported && length > Vector256<short>.Count)
#pragma warning restore IntrinsicsInSystemPrivateCoreLibAttributeNotSpecificEnough
                {
                    Vector256<byte> lowVector = Vector256.Create((byte)lowInclusive);
                    Vector256<byte> rangeVector = Vector256.Create((byte)rangeInclusive);

                    if (length > 2 * Vector256<short>.Count)
                    {
                        // Process the input in chunks of 32 characters (2 * Vector256<short>).
                        // If the input length is a multiple of 32, don't consume the last 16 characters in this loop.
                        // Let the fallback below handle it instead. This is why the condition is
                        // ">" instead of ">=" above, and why "IsAddressLessThan" is used instead of "!IsAddressGreaterThan".
                        ref short twoVectorsAwayFromEnd = ref Unsafe.Add(ref searchSpace, length - (2 * Vector256<short>.Count));

                        do
                        {
                            Vector256<short> source0 = Vector256.LoadUnsafe(ref currentSearchSpace);
                            Vector256<short> source1 = Vector256.LoadUnsafe(ref currentSearchSpace, (nuint)Vector256<short>.Count);
                            Vector256<byte> packedSource = PackSources(source0, source1);
                            Vector256<byte> result = Vector256.LessThanOrEqual(packedSource - lowVector, rangeVector);
                            result = NegateIfNeeded<TNegator>(result);

                            if (result != Vector256<byte>.Zero)
                            {
                                return ComputeFirstIndex(ref searchSpace, ref currentSearchSpace, result);
                            }

                            currentSearchSpace = ref Unsafe.Add(ref currentSearchSpace, 2 * Vector256<short>.Count);
                        }
                        while (Unsafe.IsAddressLessThan(ref currentSearchSpace, ref twoVectorsAwayFromEnd));
                    }

                    // We have 1-32 characters remaining. Process the first and last vector in the search space.
                    // They may overlap, but we'll handle that in the index calculation if we do get a match.
                    {
                        ref short oneVectorAwayFromEnd = ref Unsafe.Add(ref searchSpace, length - Vector256<short>.Count);

                        ref short firstVector = ref Unsafe.IsAddressGreaterThan(ref currentSearchSpace, ref oneVectorAwayFromEnd)
                            ? ref oneVectorAwayFromEnd
                            : ref currentSearchSpace;

                        Vector256<short> source0 = Vector256.LoadUnsafe(ref firstVector);
                        Vector256<short> source1 = Vector256.LoadUnsafe(ref oneVectorAwayFromEnd);
                        Vector256<byte> packedSource = PackSources(source0, source1);
                        Vector256<byte> result = Vector256.LessThanOrEqual(packedSource - lowVector, rangeVector);
                        result = NegateIfNeeded<TNegator>(result);

                        if (result != Vector256<byte>.Zero)
                        {
                            return ComputeFirstIndexOverlapped(ref searchSpace, ref firstVector, ref oneVectorAwayFromEnd, result);
                        }
                    }
                }
                else
                {
                    Vector128<byte> lowVector = Vector128.Create((byte)lowInclusive);
                    Vector128<byte> rangeVector = Vector128.Create((byte)rangeInclusive);

#pragma warning disable IntrinsicsInSystemPrivateCoreLibConditionParsing // A negated IsSupported condition isn't parseable by the intrinsics analyzer, but in this case, it is only used in combination
                                                                         // with the check above of Avx2.IsSupported && length > Vector256<short>.Count which makes the logic
                                                                         // in this if statement dead code when Avx2.IsSupported. Presumably this negated IsSupported check is to assist the JIT in
                                                                         // not generating dead code.
#pragma warning disable IntrinsicsInSystemPrivateCoreLibAttributeNotSpecificEnough // This is paired with the check above, and since these if statements are contained in 1 function, the code
                                                                                   // may take a dependence on the JIT compiler producing a consistent value for the result of a call to IsSupported
                                                                                   // This logic MUST NOT be extracted to a helper function
                    if (!Avx2.IsSupported && length > 2 * Vector128<short>.Count)
#pragma warning restore IntrinsicsInSystemPrivateCoreLibAttributeNotSpecificEnough
#pragma warning restore IntrinsicsInSystemPrivateCoreLibConditionParsing
                    {
                        // Process the input in chunks of 16 characters (2 * Vector128<short>).
                        // If the input length is a multiple of 16, don't consume the last 16 characters in this loop.
                        // Let the fallback below handle it instead. This is why the condition is
                        // ">" instead of ">=" above, and why "IsAddressLessThan" is used instead of "!IsAddressGreaterThan".
                        ref short twoVectorsAwayFromEnd = ref Unsafe.Add(ref searchSpace, length - (2 * Vector128<short>.Count));

                        do
                        {
                            Vector128<short> source0 = Vector128.LoadUnsafe(ref currentSearchSpace);
                            Vector128<short> source1 = Vector128.LoadUnsafe(ref currentSearchSpace, (nuint)Vector128<short>.Count);
                            Vector128<byte> packedSource = PackSources(source0, source1);
                            Vector128<byte> result = Vector128.LessThanOrEqual(packedSource - lowVector, rangeVector);
                            result = NegateIfNeeded<TNegator>(result);

                            if (result != Vector128<byte>.Zero)
                            {
                                return ComputeFirstIndex(ref searchSpace, ref currentSearchSpace, result);
                            }

                            currentSearchSpace = ref Unsafe.Add(ref currentSearchSpace, 2 * Vector128<short>.Count);
                        }
                        while (Unsafe.IsAddressLessThan(ref currentSearchSpace, ref twoVectorsAwayFromEnd));
                    }

                    // We have 1-16 characters remaining. Process the first and last vector in the search space.
                    // They may overlap, but we'll handle that in the index calculation if we do get a match.
                    {
                        ref short oneVectorAwayFromEnd = ref Unsafe.Add(ref searchSpace, length - Vector128<short>.Count);

                        ref short firstVector = ref Unsafe.IsAddressGreaterThan(ref currentSearchSpace, ref oneVectorAwayFromEnd)
                            ? ref oneVectorAwayFromEnd
                            : ref currentSearchSpace;

                        Vector128<short> source0 = Vector128.LoadUnsafe(ref firstVector);
                        Vector128<short> source1 = Vector128.LoadUnsafe(ref oneVectorAwayFromEnd);
                        Vector128<byte> packedSource = PackSources(source0, source1);
                        Vector128<byte> result = Vector128.LessThanOrEqual(packedSource - lowVector, rangeVector);
                        result = NegateIfNeeded<TNegator>(result);

                        if (result != Vector128<byte>.Zero)
                        {
                            return ComputeFirstIndexOverlapped(ref searchSpace, ref firstVector, ref oneVectorAwayFromEnd, result);
                        }
                    }
                }
            }

            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CompExactlyDependsOn(typeof(Avx512BW))]
        private static Vector512<byte> PackSources(Vector512<short> source0, Vector512<short> source1)
        {
            Debug.Assert(Avx512BW.IsSupported);
            // Pack two vectors of characters into bytes. While the type is Vector256<short>, these are really UInt16 characters.
            // X86: Downcast every character using saturation.
            // - Values <= 32767 result in min(value, 255).
            // - Values  > 32767 result in 0. Because of this we can't accept needles that contain 0.
            return Avx512BW.PackUnsignedSaturate(source0, source1).AsByte();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CompExactlyDependsOn(typeof(Avx2))]
        private static Vector256<byte> PackSources(Vector256<short> source0, Vector256<short> source1)
        {
            Debug.Assert(Avx2.IsSupported);
            // Pack two vectors of characters into bytes. While the type is Vector256<short>, these are really UInt16 characters.
            // X86: Downcast every character using saturation.
            // - Values <= 32767 result in min(value, 255).
            // - Values  > 32767 result in 0. Because of this we can't accept needles that contain 0.
            return Avx2.PackUnsignedSaturate(source0, source1).AsByte();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CompExactlyDependsOn(typeof(Sse2))]
        private static Vector128<byte> PackSources(Vector128<short> source0, Vector128<short> source1)
        {
            Debug.Assert(Sse2.IsSupported);
            // Pack two vectors of characters into bytes. While the type is Vector128<short>, these are really UInt16 characters.
            // X86: Downcast every character using saturation.
            // - Values <= 32767 result in min(value, 255).
            // - Values  > 32767 result in 0. Because of this we can't accept needles that contain 0.
            return Sse2.PackUnsignedSaturate(source0, source1).AsByte();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool NegateIfNeeded<TNegator>(bool result)
            where TNegator : struct, SpanHelpers.INegator<short> =>
            typeof(TNegator) == typeof(SpanHelpers.DontNegate<short>) ? result : !result;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector128<byte> NegateIfNeeded<TNegator>(Vector128<byte> result)
            where TNegator : struct, SpanHelpers.INegator<short> =>
            typeof(TNegator) == typeof(SpanHelpers.DontNegate<short>) ? result : ~result;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector256<byte> NegateIfNeeded<TNegator>(Vector256<byte> result)
            where TNegator : struct, SpanHelpers.INegator<short> =>
            typeof(TNegator) == typeof(SpanHelpers.DontNegate<short>) ? result : ~result;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector512<byte> NegateIfNeeded<TNegator>(Vector512<byte> result)
            where TNegator : struct, SpanHelpers.INegator<short> =>
            typeof(TNegator) == typeof(SpanHelpers.DontNegate<short>) ? result : ~result;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool HasMatch<TNegator>(Vector512<byte> left, Vector512<byte> right)
            where TNegator : struct, SpanHelpers.INegator<short>
        {
            return (typeof(TNegator) == typeof(SpanHelpers.DontNegate<short>))
                 ? Vector512.EqualsAny(left, right) : !Vector512.EqualsAll(left, right);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector512<byte> GetMatchMask<TNegator>(Vector512<byte> left, Vector512<byte> right)
             where TNegator : struct, SpanHelpers.INegator<short>
        {
            return (typeof(TNegator) == typeof(SpanHelpers.DontNegate<short>))
                 ? Vector512.Equals(left, right) : ~Vector512.Equals(left, right);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool HasMatchInRange<TNegator>(Vector512<byte> left, Vector512<byte> right)
        where TNegator : struct, SpanHelpers.INegator<short>
        {
            return (typeof(TNegator) == typeof(SpanHelpers.DontNegate<short>))
                 ? Vector512.LessThanOrEqualAny(left, right) : !Vector512.LessThanOrEqualAll(left, right);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector512<byte> GetMatchInRangeMask<TNegator>(Vector512<byte> left, Vector512<byte> right)
            where TNegator : struct, SpanHelpers.INegator<short>
        {
            return (typeof(TNegator) == typeof(SpanHelpers.DontNegate<short>))
                 ? Vector512.LessThanOrEqual(left, right) : ~Vector512.LessThanOrEqual(left, right);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int ComputeFirstIndex(ref short searchSpace, ref short current, Vector128<byte> equals)
        {
            uint notEqualsElements = equals.ExtractMostSignificantBits();
            int index = BitOperations.TrailingZeroCount(notEqualsElements);
            return index + (int)((nuint)Unsafe.ByteOffset(ref searchSpace, ref current) / sizeof(short));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CompExactlyDependsOn(typeof(Avx2))]
        private static int ComputeFirstIndex(ref short searchSpace, ref short current, Vector256<byte> equals)
        {
            uint notEqualsElements = FixUpPackedVector256Result(equals).ExtractMostSignificantBits();
            int index = BitOperations.TrailingZeroCount(notEqualsElements);
            return index + (int)((nuint)Unsafe.ByteOffset(ref searchSpace, ref current) / sizeof(short));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CompExactlyDependsOn(typeof(Avx512F))]
        private static int ComputeFirstIndex(ref short searchSpace, ref short current, Vector512<byte> equals)
        {
            ulong notEqualsElements = FixUpPackedVector512Result(equals).ExtractMostSignificantBits();
            int index = BitOperations.TrailingZeroCount(notEqualsElements);
            return index + (int)((nuint)Unsafe.ByteOffset(ref searchSpace, ref current) / sizeof(short));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int ComputeFirstIndexOverlapped(ref short searchSpace, ref short current0, ref short current1, Vector128<byte> equals)
        {
            uint notEqualsElements = equals.ExtractMostSignificantBits();
            int offsetInVector = BitOperations.TrailingZeroCount(notEqualsElements);
            if (offsetInVector >= Vector128<short>.Count)
            {
                // We matched within the second vector
                current0 = ref current1;
                offsetInVector -= Vector128<short>.Count;
            }
            return offsetInVector + (int)((nuint)Unsafe.ByteOffset(ref searchSpace, ref current0) / sizeof(short));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CompExactlyDependsOn(typeof(Avx2))]
        private static int ComputeFirstIndexOverlapped(ref short searchSpace, ref short current0, ref short current1, Vector256<byte> equals)
        {
            uint notEqualsElements = FixUpPackedVector256Result(equals).ExtractMostSignificantBits();
            int offsetInVector = BitOperations.TrailingZeroCount(notEqualsElements);
            if (offsetInVector >= Vector256<short>.Count)
            {
                // We matched within the second vector
                current0 = ref current1;
                offsetInVector -= Vector256<short>.Count;
            }
            return offsetInVector + (int)((nuint)Unsafe.ByteOffset(ref searchSpace, ref current0) / sizeof(short));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CompExactlyDependsOn(typeof(Avx512F))]
        private static int ComputeFirstIndexOverlapped(ref short searchSpace, ref short current0, ref short current1, Vector512<byte> equals)
        {
            ulong notEqualsElements = FixUpPackedVector512Result(equals).ExtractMostSignificantBits();
            int offsetInVector = BitOperations.TrailingZeroCount(notEqualsElements);
            if (offsetInVector >= Vector512<short>.Count)
            {
                // We matched within the second vector
                current0 = ref current1;
                offsetInVector -= Vector512<short>.Count;
            }
            return offsetInVector + (int)((nuint)Unsafe.ByteOffset(ref searchSpace, ref current0) / sizeof(short));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CompExactlyDependsOn(typeof(Avx2))]
        internal static Vector256<byte> FixUpPackedVector256Result(Vector256<byte> result)
        {
            Debug.Assert(Avx2.IsSupported);
            // Avx2.PackUnsignedSaturate(Vector256.Create((short)1), Vector256.Create((short)2)) will result in
            // 1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 2, 2, 2, 2, 1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 2, 2, 2, 2
            // We want to swap the X and Y bits
            // 1, 1, 1, 1, 1, 1, 1, 1, X, X, X, X, X, X, X, X, Y, Y, Y, Y, Y, Y, Y, Y, 2, 2, 2, 2, 2, 2, 2, 2
            return Avx2.Permute4x64(result.AsInt64(), 0b_11_01_10_00).AsByte();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CompExactlyDependsOn(typeof(Avx512F))]
        internal static Vector512<byte> FixUpPackedVector512Result(Vector512<byte> result)
        {
            Debug.Assert(Avx512F.IsSupported);
            // Avx512BW.PackUnsignedSaturate will interleave the inputs in 8-byte blocks.
            // We want to preserve the order of the two input vectors, so we deinterleave the packed value.
            return Avx512F.PermuteVar8x64(result.AsInt64(), Vector512.Create(0, 2, 4, 6, 1, 3, 5, 7)).AsByte();
        }

        private interface ITransform
        {
            static abstract short TransformInput(short input);
            static abstract Vector128<byte> TransformInput(Vector128<byte> input);
            static abstract Vector256<byte> TransformInput(Vector256<byte> input);
            static abstract Vector512<byte> TransformInput(Vector512<byte> input);
        }

        private readonly struct NopTransform : ITransform
        {
            public static short TransformInput(short input) => input;
            public static Vector128<byte> TransformInput(Vector128<byte> input) => input;
            public static Vector256<byte> TransformInput(Vector256<byte> input) => input;
            public static Vector512<byte> TransformInput(Vector512<byte> input) => input;
        }

        private readonly struct Or20Transform : ITransform
        {
            public static short TransformInput(short input) => (short)(input | 0x20);
            public static Vector128<byte> TransformInput(Vector128<byte> input) => input | Vector128.Create((byte)0x20);
            public static Vector256<byte> TransformInput(Vector256<byte> input) => input | Vector256.Create((byte)0x20);
            public static Vector512<byte> TransformInput(Vector512<byte> input) => input | Vector512.Create((byte)0x20);
        }
    }
}
