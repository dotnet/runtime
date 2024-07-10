// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

using static System.Buffers.Binary.BinaryPrimitives;
using System.Text;
using System.Reflection;

namespace System
{
    public static class TestHelpers
    {
        public static void Validate<T>(this Span<T> span, params T[] expected) where T : struct, IEquatable<T>
        {
            Assert.True(span.SequenceEqual(expected));
        }

        public static void ValidateReferenceType<T>(this Span<T> span, params T[] expected) where T : class
        {
            Assert.Equal(span.Length, expected.Length);
            for (int i = 0; i < expected.Length; i++)
            {
                T actual = span[i];
                Assert.Same(expected[i], actual);
            }

            T ignore;
            AssertThrows<IndexOutOfRangeException, T>(span, (_span) => ignore = _span[expected.Length]);
        }

        public static unsafe void ValidateNonNullEmpty<T>(this Span<T> span)
        {
            Assert.True(span.IsEmpty);

            // Validate that empty Span is not normalized to null
            Assert.False(Unsafe.IsNullRef(ref MemoryMarshal.GetReference(span)));
        }

        public delegate void AssertThrowsAction<T>(Span<T> span);

        // Cannot use standard Assert.Throws() when testing Span - Span and closures don't get along.
        public static void AssertThrows<E, T>(Span<T> span, AssertThrowsAction<T> action) where E : Exception
        {
            try
            {
                action(span);
                Assert.Fail($"Expected exception: {typeof(E)}");
            }
            catch (Exception ex)
            {
                Assert.True(ex is E, $"Wrong exception thrown. Expected: {typeof(E)} Actual: {ex.GetType()}");
            }
        }

        //
        // The innocent looking construct:
        //
        //    Assert.Throws<E>( () => new Span() );
        //
        // generates a hidden box of the Span as the return value of the lambda. This makes the IL illegal and unloadable on
        // runtimes that enforce the actual Span rules (never mind that we expect never to reach the box instruction...)
        //
        // The workaround is to code it like this:
        //
        //    Assert.Throws<E>( () => new Span().DontBox() );
        //
        // which turns the lambda return type back to "void" and eliminates the troublesome box instruction.
        //
        public static void DontBox<T>(this Span<T> span)
        {
            // This space intentionally left blank.
        }

        public static void Validate<T>(this ReadOnlySpan<T> span, params T[] expected) where T : struct, IEquatable<T>
        {
            Assert.True(span.SequenceEqual(expected));
        }

        public static void ValidateReferenceType<T>(this ReadOnlySpan<T> span, params T[] expected) where T : class
        {
            Assert.Equal(span.Length, expected.Length);
            for (int i = 0; i < expected.Length; i++)
            {
                T actual = span[i];
                Assert.Same(expected[i], actual);
            }

            T ignore;
            AssertThrows<IndexOutOfRangeException, T>(span, (_span) => ignore = _span[expected.Length]);
        }

        public static unsafe void ValidateNonNullEmpty<T>(this ReadOnlySpan<T> span)
        {
            Assert.True(span.IsEmpty);

            // Validate that empty Span is not normalized to null
            Assert.False(Unsafe.IsNullRef(ref MemoryMarshal.GetReference(span)));
        }

        public delegate void AssertThrowsActionReadOnly<T>(ReadOnlySpan<T> span);

        // Cannot use standard Assert.Throws() when testing Span - Span and closures don't get along.
        public static void AssertThrows<E, T>(ReadOnlySpan<T> span, AssertThrowsActionReadOnly<T> action) where E : Exception
        {
            try
            {
                action(span);
                Assert.Fail($"Expected exception: {typeof(E)}");
            }
            catch (Exception ex)
            {
                Assert.True(ex is E, $"Wrong exception thrown. Expected: {typeof(E)} Actual: {ex.GetType()}");
            }
        }

        //
        // The innocent looking construct:
        //
        //    Assert.Throws<E>( () => new Span() );
        //
        // generates a hidden box of the Span as the return value of the lambda. This makes the IL illegal and unloadable on
        // runtimes that enforce the actual Span rules (never mind that we expect never to reach the box instruction...)
        //
        // The workaround is to code it like this:
        //
        //    Assert.Throws<E>( () => new Span().DontBox() );
        //
        // which turns the lambda return type back to "void" and eliminates the troublesome box instruction.
        //
        public static void DontBox<T>(this ReadOnlySpan<T> span)
        {
            // This space intentionally left blank.
        }

        public static void Validate<T>(this Memory<T> memory, params T[] expected) where T : IEquatable<T>
        {
            Assert.True(memory.Span.SequenceEqual(expected));
        }

        public static void ValidateReferenceType<T>(this Memory<T> memory, params T[] expected) where T : class
        {
            T[] bufferArray = memory.ToArray();
            Assert.Equal(memory.Length, expected.Length);
            for (int i = 0; i < expected.Length; i++)
            {
                T actual = bufferArray[i];
                Assert.Same(expected[i], actual);
            }
        }

        public static void Validate<T>(this ReadOnlyMemory<T> memory, params T[] expected) where T : IEquatable<T>
        {
            Assert.True(memory.Span.SequenceEqual(expected));
        }

        public static void ValidateReferenceType<T>(this ReadOnlyMemory<T> memory, params T[] expected) where T : class
        {
            T[] bufferArray = memory.ToArray();
            Assert.Equal(memory.Length, expected.Length);
            for (int i = 0; i < expected.Length; i++)
            {
                T actual = bufferArray[i];
                Assert.Same(expected[i], actual);
            }
        }

        public static void Validate<T>(Span<byte> span, T value) where T : struct
        {
            T read = MemoryMarshal.Read<T>(span);
            Assert.Equal(value, read);
            span.Clear();
        }

        public static TestStructExplicit s_testExplicitStruct = new TestStructExplicit
        {
            S0 = short.MaxValue,
            I0 = int.MaxValue,
            L0 = long.MaxValue,
            US0 = ushort.MaxValue,
            UI0 = uint.MaxValue,
            UL0 = ulong.MaxValue,
            S1 = short.MinValue,
            I1 = int.MinValue,
            L1 = long.MinValue,
            US1 = ushort.MinValue,
            UI1 = uint.MinValue,
            UL1 = ulong.MinValue
        };

        public static Span<byte> GetSpanBE()
        {
            Span<byte> spanBE = new byte[Unsafe.SizeOf<TestStructExplicit>()];

            WriteInt16BigEndian(spanBE, s_testExplicitStruct.S0);
            WriteInt32BigEndian(spanBE.Slice(2), s_testExplicitStruct.I0);
            WriteInt64BigEndian(spanBE.Slice(6), s_testExplicitStruct.L0);
            WriteUInt16BigEndian(spanBE.Slice(14), s_testExplicitStruct.US0);
            WriteUInt32BigEndian(spanBE.Slice(16), s_testExplicitStruct.UI0);
            WriteUInt64BigEndian(spanBE.Slice(20), s_testExplicitStruct.UL0);
            WriteInt16BigEndian(spanBE.Slice(28), s_testExplicitStruct.S1);
            WriteInt32BigEndian(spanBE.Slice(30), s_testExplicitStruct.I1);
            WriteInt64BigEndian(spanBE.Slice(34), s_testExplicitStruct.L1);
            WriteUInt16BigEndian(spanBE.Slice(42), s_testExplicitStruct.US1);
            WriteUInt32BigEndian(spanBE.Slice(44), s_testExplicitStruct.UI1);
            WriteUInt64BigEndian(spanBE.Slice(48), s_testExplicitStruct.UL1);

            Assert.Equal(56, spanBE.Length);
            return spanBE;
        }

        public static Span<byte> GetSpanLE()
        {
            Span<byte> spanLE = new byte[Unsafe.SizeOf<TestStructExplicit>()];

            WriteInt16LittleEndian(spanLE, s_testExplicitStruct.S0);
            WriteInt32LittleEndian(spanLE.Slice(2), s_testExplicitStruct.I0);
            WriteInt64LittleEndian(spanLE.Slice(6), s_testExplicitStruct.L0);
            WriteUInt16LittleEndian(spanLE.Slice(14), s_testExplicitStruct.US0);
            WriteUInt32LittleEndian(spanLE.Slice(16), s_testExplicitStruct.UI0);
            WriteUInt64LittleEndian(spanLE.Slice(20), s_testExplicitStruct.UL0);
            WriteInt16LittleEndian(spanLE.Slice(28), s_testExplicitStruct.S1);
            WriteInt32LittleEndian(spanLE.Slice(30), s_testExplicitStruct.I1);
            WriteInt64LittleEndian(spanLE.Slice(34), s_testExplicitStruct.L1);
            WriteUInt16LittleEndian(spanLE.Slice(42), s_testExplicitStruct.US1);
            WriteUInt32LittleEndian(spanLE.Slice(44), s_testExplicitStruct.UI1);
            WriteUInt64LittleEndian(spanLE.Slice(48), s_testExplicitStruct.UL1);

            Assert.Equal(56, spanLE.Length);
            return spanLE;
        }

        public static string BuildString(int length, int seed)
        {
            Random rnd = new Random(seed);
            var builder = new StringBuilder();
            for (int i = 0; i < length; i++)
            {
                builder.Append((char)rnd.Next(65, 91));
            }
            return builder.ToString();
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct TestStructExplicit
        {
            [FieldOffset(0)]
            public short S0;
            [FieldOffset(2)]
            public int I0;
            [FieldOffset(6)]
            public long L0;
            [FieldOffset(14)]
            public ushort US0;
            [FieldOffset(16)]
            public uint UI0;
            [FieldOffset(20)]
            public ulong UL0;
            [FieldOffset(28)]
            public short S1;
            [FieldOffset(30)]
            public int I1;
            [FieldOffset(34)]
            public long L1;
            [FieldOffset(42)]
            public ushort US1;
            [FieldOffset(44)]
            public uint UI1;
            [FieldOffset(48)]
            public ulong UL1;
        }

        [StructLayout(LayoutKind.Sequential)]
        public sealed class TestClass
        {
            private double _d;
            public char C0;
            public char C1;
            public char C2;
            public char C3;
            public char C4;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct TestValueTypeWithReference
        {
            public int I;
            public string S;
        }

#pragma warning disable 0649 //Field 'SpanTests.InnerStruct.J' is never assigned to, and will always have its default value 0
        internal struct StructWithReferences
        {
            public int I;
            public InnerStruct Inner;
        }

        internal struct InnerStruct
        {
            public int J;
            public object O;
        }
#pragma warning restore 0649 //Field 'SpanTests.InnerStruct.J' is never assigned to, and will always have its default value 0

        public enum TestEnum
        {
            E0,
            E1,
            E2,
            E3,
            E4,
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void DoNotIgnore<T>(T value, int consumed)
        {
        }

        //
        // { text, start, length } triplets. A "-1" in start or length means "test the overload that doesn't have that parameter."
        //
        public static IEnumerable<object[]> StringSliceTestData
        {
            get
            {
                foreach (string text in new string[] { string.Empty, "012" })
                {
                    yield return new object[] { text, -1, -1 };
                    for (int start = 0; start <= text.Length; start++)
                    {
                        yield return new object[] { text, start, -1 };

                        for (int length = 0; length <= text.Length - start; length++)
                        {
                            yield return new object[] { text, start, length };
                        }
                    }
                }
            }
        }

        public static IEnumerable<object[]> StringSlice2ArgTestOutOfRangeData
        {
            get
            {
                foreach (string text in new string[] { string.Empty, "012" })
                {
                    yield return new object[] { text, -1 };
                    yield return new object[] { text, int.MinValue };

                    yield return new object[] { text, text.Length + 1 };
                    yield return new object[] { text, int.MaxValue };
                }
            }
        }

        public static IEnumerable<object[]> StringSlice3ArgTestOutOfRangeData
        {
            get
            {
                foreach (string text in new string[] { string.Empty, "012" })
                {
                    yield return new object[] { text, -1, 0 };
                    yield return new object[] { text, int.MinValue, 0 };

                    yield return new object[] { text, text.Length + 1, 0 };
                    yield return new object[] { text, int.MaxValue, 0 };

                    yield return new object[] { text, 0, -1 };
                    yield return new object[] { text, 0, int.MinValue };

                    yield return new object[] { text, 0, text.Length + 1 };
                    yield return new object[] { text, 0, int.MaxValue };

                    yield return new object[] { text, 1, text.Length };
                    yield return new object[] { text, 1, int.MaxValue };

                    yield return new object[] { text, text.Length - 1, 2 };
                    yield return new object[] { text, text.Length - 1, int.MaxValue };

                    yield return new object[] { text, text.Length, 1 };
                    yield return new object[] { text, text.Length, int.MaxValue };
                }
            }
        }

        /// <summary>Creates a <see cref="Memory{T}"/> with the specified values in its backing field.</summary>
        public static Memory<T> DangerousCreateMemory<T>(object obj, int offset, int length)
        {
            Memory<T> mem = default;
            object boxedMemory = mem;

            typeof(Memory<T>).GetField("_object", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(boxedMemory, obj);
            typeof(Memory<T>).GetField("_index", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(boxedMemory, offset);
            typeof(Memory<T>).GetField("_length", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(boxedMemory, length);

            return (Memory<T>)boxedMemory;
        }

        /// <summary>Creates a <see cref="ReadOnlyMemory{T}"/> with the specified values in its backing field.</summary>
        public static ReadOnlyMemory<T> DangerousCreateReadOnlyMemory<T>(object obj, int offset, int length) =>
            DangerousCreateMemory<T>(obj, offset, length);
        
        public static TheoryData<string[], bool> ContainsNullData => new TheoryData<string[], bool>()
        {
            { new string[] { "1", null, "2" }, true},
            { new string[] { "1", "3", "2" }, false},
            { null, false},
            { new string[] { "1", null, null }, true},
            { new string[] { null, null, null }, true},
        };
        
        public static TheoryData<string[], int> CountNullData => new TheoryData<string[], int>()
        {
            { new string[] { "1", null, "2" }, 1},
            { new string[] { "1", "3", "2" }, 0},
            { null, 0},
            { new string[] { "1", null, null }, 2},
            { new string[] { null, null, null }, 3},
        };

        public static TheoryData<string[], int> CountNullRosData => new TheoryData<string[], int>()
        {
            { new string[] { "1", null, "9", "2" }, 1},
            { new string[] { "1", "3", "9", "2" }, 0},
            { null, 0},
            { new string[] { "1", null, "9", null, "9"}, 2},
            { new string[] { null, null, "9", null, "9", "9", null, "9"}, 3},
        };

        public static TheoryData<string[], string[],  bool> SequenceEqualsNullData => new TheoryData<string[], string[], bool>()
        {
            { new string[] { "1", null, "2" }, new string[] { "1", null, "2" } , true},
            { new string[] { "1", null, "2" }, new string[] { "1", "3", "2" } , false},
            { new string[] { "1", null, "2" }, new string[] { null, "3", "2" } , false},
            { new string[] { "1", null, "2" }, new string[] { null } , false},
            { new string[] { "1", null, "2" }, null , false},

            { new string[] { null, "2", "1" }, new string[] { null, "2" } , false},

            { null, new string[] { null }, false},
            { null, null , true},
            { null, new string[] { "1", "3", "2" } , false},
            { null, new string[] { "1", null, "2" } , false},

            { new string[] { "1", null, null }, new string[] { "1", null, null }, true},
            { new string[] { null, null, null }, new string[] { null, null, null }, true},
        };

        public static TheoryData<string[], int> IndexOfNullData => new TheoryData<string[], int>()
        {
            { new string[] { "1", null, "2" }, 1},
            { new string[] { "1", "3", "2" }, -1},
            { null, -1},
            { new string[] { "1", null, null }, 1},
            { new string[] { null, null, null }, 0},
        };

        public static TheoryData<string[], string[], int> IndexOfNullSequenceData => new TheoryData<string[], string[], int>()
        {
            { new string[] { "1", null, "2" }, new string[] { "1", null, "2" }, 0},
            { new string[] { "1", null, "2" }, new string[] { null }, 1},
            { new string[] { "1", null, "2" }, (string[])null, 0},

            { new string[] { "1", "3", "2" }, new string[] { "1", null, "2" }, -1},
            { new string[] { "1", "3", "2" }, new string[] { null }, -1},
            { new string[] { "1", "3", "2" }, (string[])null, 0},

            { null, new string[] { "1", null, "2" }, -1},

            { new string[] { "1", null, null }, new string[] { null, null, "2" }, -1},
            { new string[] { null, null, null }, new string[] { null, null }, 0},
        };

        public static TheoryData<string[], string[], int> IndexOfAnyNullSequenceData => new TheoryData<string[], string[], int>()
        {
            { new string[] { "1", null, "2" }, new string[] { "1", null, "2" }, 0},
            { new string[] { "1", null, "2" }, new string[] { null, null }, 1},

            { new string[] { "1", null, "2" }, new string[] { "3", null }, 1},
            { new string[] { "1", null, "2" }, new string[] { "1", "2" }, 0},
            { new string[] { "1", null, "2" }, new string[] { "3", "4" }, -1},

            { new string[] { null, null, "2" }, new string[] { "3", null }, 0},
            { new string[] { null, null, "2" }, new string[] { null, "1" }, 0},
            { new string[] { null, null, "2" }, new string[] { null, "1" }, 0},

            { new string[] { "1", "3", "2" }, new string[] { "1", null, "2" }, 0},
            { new string[] { "1", "3", "2" }, new string[] { null, null }, -1},

            { new string[] { "1", "3", "2" }, new string[] { null, "1" }, 0},

            { null, new string[] { "1", null, "2" }, -1},

            { new string[] { "1", null, null }, new string[] { null, null, "2" }, 1},
            { new string[] { null, null, null }, new string[] { null, null }, 0},

            { new string[] { "1", "3", "2" }, null, -1},
            { new string[] { "1", null, "2" }, null, -1},
        };

        public static TheoryData<string[], int> LastIndexOfNullData => new TheoryData<string[], int>()
        {
            { new string[] { "1", null, "2" }, 1},
            { new string[] { "1", "3", "2" }, -1},
            { null, -1},
            { new string[] { "1", null, null }, 2},
            { new string[] { null, null, null }, 2},
            { new string[] { null, null, "3" }, 1},
        };

        public static TheoryData<string[], string[], int> LastIndexOfNullSequenceData => new TheoryData<string[], string[], int>()
        {
            { new string[] { "1", null, "2" }, new string[] { "1", null, "2" }, 0},
            { new string[] { "1", null, "2" }, new string[] { null }, 1},
            { new string[] { "1", null, "2" }, (string[])null, 3},

            { new string[] { "1", "3", "1" }, new string[] { "1", null, "2" }, -1},
            { new string[] { "1", "3", "1" }, new string[] { "1" }, 2},
            { new string[] { "1", "3", "1" }, new string[] { null }, -1},
            { new string[] { "1", "3", "1" }, (string[])null, 3},

            { null, new string[] { "1", null, "2" }, -1},

            { new string[] { "1", null, null }, new string[] { null, null, "2" }, -1},
            { new string[] { null, null, null }, new string[] { null, null }, 1},
        };

        public static TheoryData<string[], string[], int> LastIndexOfAnyNullSequenceData => new TheoryData<string[], string[], int>()
        {
            { new string[] { "1", null, "2" }, new string[] { "1", null, "3" }, 1},
            { new string[] { "1", null, "2" }, new string[] { null, null }, 1},
            { new string[] { "1", null, "2" }, new string[] { "3", "4" }, -1},
            { new string[] { "1", null, "2" }, new string[] { "3", null }, 1},
            { new string[] { "1", null, "2" }, new string[] { "1", null }, 1},
            { new string[] { "1", null, "2" }, new string[] { null, null }, 1},
            { new string[] { "1", null, "2" }, new string[] { "1", "2" }, 2},
            { null, new string[] { "1", null, "2" }, -1},

            { new string[] { null, null, "2" }, new string[] { "3", null }, 1},
            { new string[] { null, null, "2" }, new string[] { null, "1" }, 1},
            { new string[] { null, null, "2" }, new string[] { null, "1" }, 1},

            { new string[] { "1", "3", "2" }, new string[] { null, "1" }, 0},
            { new string[] { "1", "3", "2" }, new string[] { "1", "2", null }, 2},
            { new string[] { "1", "3", "2" }, new string[] { "4", "5", null }, -1},
            { new string[] { "1", "3", "2" }, new string[] { null, null }, -1},
            { new string[] { "1", "3", "2" }, new string[] { null, null, null }, -1},
            { new string[] { "1", "3", "2" }, new string[] { null, null, null, null }, -1},
            { new string[] { "1", "3", "2" }, new string[] { null, null, null, null, null }, -1},

            { null, new string[] { null, "1" }, -1},

            { new string[] { "1", null, null }, new string[] { null, null, "2" }, 2},
            { new string[] { null, null, null }, new string[] { null, null }, 2},

            { new string[] { "1", null, "2" }, null, -1},
            { new string[] { "1", "3", "2" }, null, -1},
        };
    }
}
