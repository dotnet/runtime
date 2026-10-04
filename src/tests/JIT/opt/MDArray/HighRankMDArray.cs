// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

// Exercise intrinsic expansion beyond rank 3, including the maximum supported rank.
// Keep the accessors out of line so each one is compiled with an unknown array shape.
public class HighRankMDArray
{
    private const MethodImplOptions TestOptions = MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization;
    private static string s_trace;

    public struct Pair
    {
        public long A;
        public double B;
    }

    [InlineArray(300)]
    public struct Payload
    {
        private byte _element0;
    }

    public struct StructWithReferences
    {
        public Payload Data;
        public string Reference;
        public int Value;
    }

    [MethodImpl(TestOptions)]
    private static int Get4(int[,,,] a, int[] i) => a[i[0], i[1], i[2], i[3]];

    [MethodImpl(TestOptions)]
    private static void Set4(int[,,,] a, int[] i, int value) => a[i[0], i[1], i[2], i[3]] = value;

    [MethodImpl(TestOptions)]
    private static ref int Address4(int[,,,] a, int[] i) => ref a[i[0], i[1], i[2], i[3]];

    [MethodImpl(TestOptions)]
    private static int Get5(int[,,,,] a, int[] i) => a[i[0], i[1], i[2], i[3], i[4]];

    [MethodImpl(TestOptions)]
    private static void Set5(int[,,,,] a, int[] i, int value) => a[i[0], i[1], i[2], i[3], i[4]] = value;

    [MethodImpl(TestOptions)]
    private static ref int Address5(int[,,,,] a, int[] i) => ref a[i[0], i[1], i[2], i[3], i[4]];

    [MethodImpl(TestOptions)]
    private static int Get32(int[,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,] a, int[] i) => a[
            i[0], i[1], i[2], i[3], i[4], i[5], i[6], i[7],
            i[8], i[9], i[10], i[11], i[12], i[13], i[14], i[15],
            i[16], i[17], i[18], i[19], i[20], i[21], i[22], i[23],
            i[24], i[25], i[26], i[27], i[28], i[29], i[30], i[31]];

    [MethodImpl(TestOptions)]
    private static void Set32(int[,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,] a, int[] i, int value) => a[
            i[0], i[1], i[2], i[3], i[4], i[5], i[6], i[7],
            i[8], i[9], i[10], i[11], i[12], i[13], i[14], i[15],
            i[16], i[17], i[18], i[19], i[20], i[21], i[22], i[23],
            i[24], i[25], i[26], i[27], i[28], i[29], i[30], i[31]] = value;

    [MethodImpl(TestOptions)]
    private static ref int Address32(int[,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,] a, int[] i) => ref a[
            i[0], i[1], i[2], i[3], i[4], i[5], i[6], i[7],
            i[8], i[9], i[10], i[11], i[12], i[13], i[14], i[15],
            i[16], i[17], i[18], i[19], i[20], i[21], i[22], i[23],
            i[24], i[25], i[26], i[27], i[28], i[29], i[30], i[31]];

    [Fact]
    public static void ZeroBasedArrays()
    {
        Verify4(new int[2, 3, 4, 5]);
        Verify5(new int[2, 3, 2, 3, 2]);
        Verify32(new int[2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 3,
                         1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 4]);
    }

    [ConditionalFact(typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.IsNonZeroLowerBoundArraySupported))]
    public static void NonZeroLowerBounds()
    {
        Verify4((int[,,,])Array.CreateInstance(typeof(int), new[] { 2, 3, 4, 5 }, new[] { -3, 5, -7, 11 }));
        Verify5((int[,,,,])Array.CreateInstance(typeof(int), new[] { 2, 3, 2, 3, 2 }, new[] { 5, -4, 3, -2, 1 }));

        int[] lengths = new int[32];
        int[] lowerBounds = new int[32];
        for (int dimension = 0; dimension < lengths.Length; dimension++)
        {
            lengths[dimension] = 1;
            lowerBounds[dimension] = dimension - 16;
        }
        lengths[0] = 2;
        lengths[15] = 3;
        lengths[31] = 4;
        Verify32((int[,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,])Array.CreateInstance(typeof(int), lengths, lowerBounds));
    }

    [Fact]
    public static void EmptyDimensions()
    {
        int[] indices = new int[32];
        int[,,,] a = new int[0, 2, 3, 4];
        int[,,,,] b = new int[2, 3, 2, 3, 0];
        int[,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,] c = new int[
            1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
            1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0];
        CheckOutOfRange4(a, indices);
        Assert.Throws<IndexOutOfRangeException>(() => Get5(b, indices));
        Assert.Throws<IndexOutOfRangeException>(() => Set5(b, indices, 1));
        Assert.Throws<IndexOutOfRangeException>(() => { Address5(b, indices) = 1; });
        Assert.Throws<IndexOutOfRangeException>(() => Get32(c, indices));
        Assert.Throws<IndexOutOfRangeException>(() => Set32(c, indices, 1));
        Assert.Throws<IndexOutOfRangeException>(() => { Address32(c, indices) = 1; });
    }

    [ConditionalFact(typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.IsNonZeroLowerBoundArraySupported))]
    public static void ExtremeLowerBounds()
    {
        int[] lowerBounds = { int.MinValue, int.MaxValue - 1, int.MinValue + 1, int.MaxValue - 2 };
        int[,,,] a = (int[,,,])Array.CreateInstance(typeof(int), new[] { 2, 2, 2, 2 }, lowerBounds);
        int[] indices = new int[4];
        // Enumerate without incrementing past int.MaxValue in the second dimension.
        for (int element = 0; element < 16; element++)
        {
            for (int dimension = 0; dimension < 4; dimension++)
            {
                indices[dimension] = lowerBounds[dimension] + ((element >> dimension) & 1);
            }
            Set4(a, indices, element + 1);
            Assert.Equal(element + 1, Get4(a, indices));
            Assert.Equal(element + 1, (int)a.GetValue(indices));
            Address4(a, indices) += 1000;
            Assert.Equal(element + 1001, Get4(a, indices));
            Assert.Equal(element + 1001, (int)a.GetValue(indices));
        }

        indices = (int[])lowerBounds.Clone();
        for (int dimension = 0; dimension < 4; dimension++)
        {
            // These wrap to int.MaxValue / int.MinValue at the extreme bounds.
            // Subtracting the lower bound must still reject the resulting index.
            indices[dimension] = unchecked(lowerBounds[dimension] - 1);
            CheckOutOfRange4(a, indices);
            indices[dimension] = unchecked(lowerBounds[dimension] + 2);
            CheckOutOfRange4(a, indices);
            indices[dimension] = lowerBounds[dimension];
        }
    }

    private static void CheckOutOfRange4(int[,,,] a, int[] indices)
    {
        Assert.Throws<IndexOutOfRangeException>(() => Get4(a, indices));
        Assert.Throws<IndexOutOfRangeException>(() => Set4(a, indices, 1));
        Assert.Throws<IndexOutOfRangeException>(() => { Address4(a, indices) = 1; });
    }

    private static void Verify4(int[,,,] a) =>
        VerifyArray(a, i => Get4(a, i), (i, value) => Set4(a, i, value), (i, value) => Address4(a, i) = value);

    private static void Verify5(int[,,,,] a) =>
        VerifyArray(a, i => Get5(a, i), (i, value) => Set5(a, i, value), (i, value) => Address5(a, i) = value);

    private static void Verify32(int[,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,] a) =>
        VerifyArray(a, i => Get32(a, i), (i, value) => Set32(a, i, value), (i, value) => Address32(a, i) = value);

    private static void VerifyArray(Array array, Func<int[], int> get, Action<int[], int> set, Action<int[], int> setByAddress)
    {
        int[] indices = new int[array.Rank];
        for (int dimension = 0; dimension < array.Rank; dimension++)
        {
            indices[dimension] = array.GetLowerBound(dimension);
        }

        int value = 1;
        do
        {
            set(indices, value++);
        }
        while (NextIndex(array, indices));

        value = 1;
        do
        {
            // GetValue supplies an independent check of the row-major offsets. Matching
            // mistakes in Get, Set and Address must not allow this test to pass.
            Assert.Equal(value, (int)array.GetValue(indices));
            Assert.Equal(value, get(indices));
            setByAddress(indices, value + 1000);
            Assert.Equal(value + 1000, get(indices));
            Assert.Equal(value + 1000, (int)array.GetValue(indices));
            value++;
        }
        while (NextIndex(array, indices));

        // Test both boundaries of every dimension, especially dimensions beyond 3.
        for (int dimension = 0; dimension < array.Rank; dimension++)
        {
            int lowerBound = array.GetLowerBound(dimension);
            indices[dimension] = lowerBound - 1;
            CheckBounds();
            indices[dimension] = array.GetUpperBound(dimension) + 1;
            CheckBounds();
            indices[dimension] = lowerBound;
        }

        void CheckBounds()
        {
            Assert.Throws<IndexOutOfRangeException>(() => get(indices));
            Assert.Throws<IndexOutOfRangeException>(() => set(indices, 0));
            Assert.Throws<IndexOutOfRangeException>(() => setByAddress(indices, 0));
        }
    }

    // Reset to the lower bounds when enumeration finishes, so the same indices can
    // be reused for the next pass and for independent per-dimension bounds checks.
    private static bool NextIndex(Array array, int[] indices)
    {
        for (int dimension = array.Rank - 1; dimension >= 0; dimension--)
        {
            if (++indices[dimension] <= array.GetUpperBound(dimension))
            {
                return true;
            }
            indices[dimension] = array.GetLowerBound(dimension);
        }
        return false;
    }

    [Fact]
    public static void NullArrays()
    {
        int[] indices = new int[32];
        Assert.Throws<NullReferenceException>(() => Get4(null, indices));
        Assert.Throws<NullReferenceException>(() => Set4(null, indices, 1));
        Assert.Throws<NullReferenceException>(() => { Address4(null, indices) = 1; });
        Assert.Throws<NullReferenceException>(() => Get5(null, indices));
        Assert.Throws<NullReferenceException>(() => Set5(null, indices, 1));
        Assert.Throws<NullReferenceException>(() => { Address5(null, indices) = 1; });
        Assert.Throws<NullReferenceException>(() => Get32(null, indices));
        Assert.Throws<NullReferenceException>(() => Set32(null, indices, 1));
        Assert.Throws<NullReferenceException>(() => { Address32(null, indices) = 1; });
    }

    [MethodImpl(TestOptions)]
    private static Pair GetPair(Pair[,,,] a, int i) => a[1, 2, 1, i];

    [MethodImpl(TestOptions)]
    private static void SetPair(Pair[,,,] a, int i, Pair value) => a[1, 2, 1, i] = value;

    [MethodImpl(TestOptions)]
    private static ref Pair AddressPair(Pair[,,,] a, int i) => ref a[1, 2, 1, i];

    [MethodImpl(TestOptions)]
    private static StructWithReferences GetStruct(StructWithReferences[,,,,] a, int i) => a[1, 0, 1, 0, i];

    [MethodImpl(TestOptions)]
    private static void SetStruct(StructWithReferences[,,,,] a, int i, StructWithReferences value) => a[1, 0, 1, 0, i] = value;

    [MethodImpl(TestOptions)]
    private static ref StructWithReferences AddressStruct(StructWithReferences[,,,,] a, int i) => ref a[1, 0, 1, 0, i];

    [MethodImpl(TestOptions)]
    private static StructWithReferences MakeStruct(int value)
    {
        StructWithReferences result = default;
        result.Data[299] = (byte)value;
        result.Reference = new string((char)('a' + value), 10);
        result.Value = value;
        return result;
    }

    [Fact]
    public static void StructElements()
    {
        Pair[,,,] pairs = new Pair[2, 3, 2, 2];
        SetPair(pairs, 1, new Pair { A = 1234567890123, B = 1.25 });
        Assert.Equal(1234567890123, GetPair(pairs, 1).A);
        Assert.Equal(1.25, GetPair(pairs, 1).B);
        AddressPair(pairs, 1).A = -1;
        Assert.Equal(-1, GetPair(pairs, 1).A);

        StructWithReferences[,,,,] structs = new StructWithReferences[2, 1, 2, 1, 3];
        // Promote the array before storing new references to exercise write barriers.
        GC.Collect();
        for (int i = 0; i < 3; i++)
        {
            SetStruct(structs, i, MakeStruct(i));
        }
        GC.Collect(0);
        for (int i = 0; i < 3; i++)
        {
            StructWithReferences value = GetStruct(structs, i);
            Assert.Equal(i, value.Value);
            Assert.Equal((byte)i, value.Data[299]);
            Assert.Equal(new string((char)('a' + i), 10), value.Reference);
        }

        GC.Collect();
        AddressStruct(structs, 2) = MakeStruct(7);
        GC.Collect(0);
        StructWithReferences updated = GetStruct(structs, 2);
        Assert.Equal(7, updated.Value);
        Assert.Equal((byte)7, updated.Data[299]);
        Assert.Equal(new string('h', 10), updated.Reference);
    }

    [MethodImpl(TestOptions)]
    private static int[,,,] EvaluateArray4(int[,,,] a)
    {
        s_trace += "A";
        return a;
    }

    [MethodImpl(TestOptions)]
    private static int[,,,,] EvaluateArray5(int[,,,,] a)
    {
        s_trace += "A";
        return a;
    }

    [MethodImpl(TestOptions)]
    private static int EvaluateIndex(int index, string marker)
    {
        s_trace += marker;
        return index;
    }

    [MethodImpl(TestOptions)]
    private static int EvaluateValue(int divisor)
    {
        s_trace += "V";
        return 100 / divisor;
    }

    [MethodImpl(TestOptions)]
    private static StructWithReferences EvaluateStructValue()
    {
        s_trace += "V";
        return MakeStruct(7);
    }

    [MethodImpl(TestOptions)]
    private static int GetWithSideEffects(int[,,,] a, int firstIndex, int divisor) =>
        EvaluateArray4(a)[EvaluateIndex(firstIndex, "1"), EvaluateIndex(0, "2"),
                          EvaluateIndex(0, "3"), EvaluateIndex(0, "4") / divisor];

    [MethodImpl(TestOptions)]
    private static void SetWithSideEffects(int[,,,,] a, int firstIndex, int divisor) =>
        EvaluateArray5(a)[EvaluateIndex(firstIndex, "1"), EvaluateIndex(0, "2"),
                          EvaluateIndex(0, "3"), EvaluateIndex(0, "4"), EvaluateIndex(0, "5")] = EvaluateValue(divisor);

    [MethodImpl(TestOptions)]
    private static void SetStructWithSideEffects(StructWithReferences[,,,,] a, int firstIndex) =>
        a[EvaluateIndex(firstIndex, "1"), EvaluateIndex(0, "2"), EvaluateIndex(0, "3"),
          EvaluateIndex(0, "4"), EvaluateIndex(0, "5")] = EvaluateStructValue();

    [MethodImpl(TestOptions)]
    private static ref int AddressWithSideEffects(int[,,,,] a, int firstIndex) =>
        ref EvaluateArray5(a)[EvaluateIndex(firstIndex, "1"), EvaluateIndex(0, "2"),
                              EvaluateIndex(0, "3"), EvaluateIndex(0, "4"), EvaluateIndex(0, "5")];

    [Fact]
    public static void EvaluationOrder()
    {
        int[,,,] a = new int[1, 1, 1, 1];
        int[,,,,] b = new int[1, 1, 1, 1, 1];
        s_trace = "";
        Assert.Equal(0, GetWithSideEffects(a, 0, 1));
        Assert.Equal("A1234", s_trace);
        s_trace = "";
        SetWithSideEffects(b, 0, 1);
        Assert.Equal("A12345V", s_trace);
        Assert.Equal(100, b[0, 0, 0, 0, 0]);
        s_trace = "";
        AddressWithSideEffects(b, 0) = 42;
        Assert.Equal("A12345", s_trace);
        Assert.Equal(42, b[0, 0, 0, 0, 0]);

        // Later arguments, including the stored value, precede null and bounds checks.
        s_trace = "";
        Assert.Throws<IndexOutOfRangeException>(() => GetWithSideEffects(a, 1, 1));
        Assert.Equal("A1234", s_trace);
        s_trace = "";
        Assert.Throws<NullReferenceException>(() => GetWithSideEffects(null, 0, 1));
        Assert.Equal("A1234", s_trace);
        s_trace = "";
        Assert.Throws<DivideByZeroException>(() => GetWithSideEffects(a, 1, 0));
        Assert.Equal("A1234", s_trace);
        s_trace = "";
        Assert.Throws<DivideByZeroException>(() => GetWithSideEffects(null, 0, 0));
        Assert.Equal("A1234", s_trace);
        s_trace = "";
        Assert.Throws<IndexOutOfRangeException>(() => SetWithSideEffects(b, 1, 1));
        Assert.Equal("A12345V", s_trace);
        s_trace = "";
        Assert.Throws<NullReferenceException>(() => SetWithSideEffects(null, 0, 1));
        Assert.Equal("A12345V", s_trace);
        s_trace = "";
        Assert.Throws<DivideByZeroException>(() => SetWithSideEffects(b, 1, 0));
        Assert.Equal("A12345V", s_trace);
        s_trace = "";
        Assert.Throws<DivideByZeroException>(() => SetWithSideEffects(null, 0, 0));
        Assert.Equal("A12345V", s_trace);
        s_trace = "";
        Assert.Throws<IndexOutOfRangeException>(() => { AddressWithSideEffects(b, 1) = 0; });
        Assert.Equal("A12345", s_trace);
        s_trace = "";
        Assert.Throws<NullReferenceException>(() => { AddressWithSideEffects(null, 0) = 0; });
        Assert.Equal("A12345", s_trace);

        StructWithReferences[,,,,] structs = new StructWithReferences[1, 1, 1, 1, 1];
        s_trace = "";
        Assert.Throws<IndexOutOfRangeException>(() => SetStructWithSideEffects(structs, 1));
        Assert.Equal("12345V", s_trace);
        s_trace = "";
        Assert.Throws<NullReferenceException>(() => SetStructWithSideEffects(null, 0));
        Assert.Equal("12345V", s_trace);
    }

    [MethodImpl(TestOptions)]
    private static object GetObject4(object[,,,] a, int i) => a[0, 0, 0, i];

    [MethodImpl(TestOptions)]
    private static void SetObject4(object[,,,] a, int i, object value) => a[0, 0, 0, i] = value;

    [MethodImpl(TestOptions)]
    private static ref object AddressObject4(object[,,,] a, int i) => ref a[0, 0, 0, i];

    [MethodImpl(TestOptions)]
    private static object GetObject5(object[,,,,] a, int i) => a[0, 0, 0, 0, i];

    [MethodImpl(TestOptions)]
    private static void SetObject5(object[,,,,] a, int i, object value) => a[0, 0, 0, 0, i] = value;

    [MethodImpl(TestOptions)]
    private static ref object AddressObject5(object[,,,,] a, int i) => ref a[0, 0, 0, 0, i];

    [MethodImpl(TestOptions)]
    private static string GetString4(string[,,,] a, int i) => a[1, 0, 0, i];

    [MethodImpl(TestOptions)]
    private static void SetString4(string[,,,] a, int i, string value) => a[1, 0, 0, i] = value;

    [MethodImpl(TestOptions)]
    private static ref string AddressString4(string[,,,] a, int i) => ref a[1, 0, 0, i];

    [MethodImpl(TestOptions)]
    private static void StoreNewString4(string[,,,] a, bool byAddress)
    {
        // Keep the newly allocated reference out of the caller's locals at GC time.
        if (byAddress)
        {
            AddressString4(a, 1) = new string('b', 10);
        }
        else
        {
            SetString4(a, 1, new string('a', 10));
        }
    }

    [Fact]
    public static void ExactReferenceElements()
    {
        string[,,,] a = new string[2, 1, 1, 2];
        GC.Collect();
        StoreNewString4(a, false);
        GC.Collect(0);
        Assert.Equal("aaaaaaaaaa", GetString4(a, 1));
        // Clear the remembered card before checking the independent Address store.
        GC.Collect();
        StoreNewString4(a, true);
        GC.Collect(0);
        Assert.Equal("bbbbbbbbbb", GetString4(a, 1));

        SetString4(a, 1, null);
        Assert.Null(GetString4(a, 1));
        StoreNewString4(a, false);
        AddressString4(a, 1) = null;
        Assert.Null(GetString4(a, 1));
    }

    [Fact]
    public static void ReferenceArrayCovariance()
    {
        object[,,,] a = new string[1, 1, 1, 2];
        object[,,,,] b = new string[1, 1, 1, 1, 2];
        SetObject4(a, 1, "four");
        SetObject5(b, 1, "five");
        Assert.Equal("four", GetObject4(a, 1));
        Assert.Equal("five", GetObject5(b, 1));

        // Set and Address must still perform the runtime's covariance checks.
        Assert.Throws<ArrayTypeMismatchException>(() => SetObject4(a, 1, new object()));
        Assert.Throws<ArrayTypeMismatchException>(() => SetObject5(b, 1, new object()));
        Assert.Throws<ArrayTypeMismatchException>(() => { AddressObject4(a, 1) = "replacement"; });
        Assert.Throws<ArrayTypeMismatchException>(() => { AddressObject5(b, 1) = "replacement"; });
        Assert.Equal("four", GetObject4(a, 1));
        Assert.Equal("five", GetObject5(b, 1));

        object value = new object();
        object[,,,] exact4 = new object[1, 1, 1, 2];
        object[,,,,] exact5 = new object[1, 1, 1, 1, 2];
        AddressObject4(exact4, 1) = value;
        AddressObject5(exact5, 1) = value;
        Assert.Same(value, GetObject4(exact4, 1));
        Assert.Same(value, GetObject5(exact5, 1));
    }

    private static int s_mutatedIndex;
    private static volatile int s_volatileIndex;

    [MethodImpl(TestOptions)]
    private static int MutateLastIndex(int divisor)
    {
        s_mutatedIndex = 1;
        s_volatileIndex = 1;
        s_trace += "L";
        return 0 / divisor;
    }

    [MethodImpl(TestOptions)]
    private static int Get32WithMutation(int[,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,] a, int divisor) =>
        a[s_mutatedIndex, 0, 0, 0, 0, 0, 0, 0,
          0, 0, 0, 0, 0, 0, 0, 0,
          0, 0, 0, 0, 0, 0, 0, 0,
          0, 0, 0, 0, 0, 0, 0, MutateLastIndex(divisor)];

    [MethodImpl(TestOptions)]
    private static ref int Address32WithVolatile(int[,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,] a, int divisor) =>
        ref a[s_volatileIndex, 0, 0, 0, 0, 0, 0, 0,
              0, 0, 0, 0, 0, 0, 0, 0,
              0, 0, 0, 0, 0, 0, 0, 0,
              0, 0, 0, 0, 0, 0, 0, MutateLastIndex(divisor)];

    [MethodImpl(TestOptions)]
    private static void Set32WithLateValue(int[,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,] a, int divisor) =>
        a[s_mutatedIndex, 0, 0, 0, 0, 0, 0, 0,
          0, 0, 0, 0, 0, 0, 0, 0,
          0, 0, 0, 0, 0, 0, 0, 0,
          0, 0, 0, 0, 0, 0, 0, 0] = EvaluateValue(divisor);

    [Fact]
    public static void MaximumRankEvaluationOrder()
    {
        int[,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,] a = new int[2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
            1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2];
        int[] indices = new int[32];
        a.SetValue(17, indices);
        indices[0] = 1;
        a.SetValue(29, indices);
        s_mutatedIndex = 0;
        s_trace = "";
        Assert.Equal(17, Get32WithMutation(a, 1));
        Assert.Equal("L", s_trace);
        Assert.Equal(1, s_mutatedIndex);
        s_volatileIndex = 0;
        s_trace = "";
        Address32WithVolatile(a, 1) = 43;
        Assert.Equal("L", s_trace);
        indices[0] = 0;
        Assert.Equal(43, (int)a.GetValue(indices));
        indices[0] = 1;
        Assert.Equal(29, (int)a.GetValue(indices));

        s_mutatedIndex = 2;
        s_trace = "";
        Assert.Throws<DivideByZeroException>(() => Get32WithMutation(a, 0));
        Assert.Equal("L", s_trace);
        s_trace = "";
        Assert.Throws<DivideByZeroException>(() => Get32WithMutation(null, 0));
        Assert.Equal("L", s_trace);
        s_mutatedIndex = 2;
        s_trace = "";
        Assert.Throws<DivideByZeroException>(() => Set32WithLateValue(a, 0));
        Assert.Equal("V", s_trace);
        s_trace = "";
        Assert.Throws<DivideByZeroException>(() => Set32WithLateValue(null, 0));
        Assert.Equal("V", s_trace);
    }

    [MethodImpl(TestOptions)]
    private static int NestedAccess(int[,,,] outer, int[,,,,] inner, int index) =>
        outer[inner[0, 0, 0, 0, index], inner[0, 0, 0, 0, index + 1], 0, 0];

    [Fact]
    public static void NestedArrayExpressions()
    {
        int[,,,] outer = new int[2, 2, 1, 1];
        int[,,,,] inner = new int[1, 1, 1, 1, 2];
        outer[0, 1, 0, 0] = 37;
        inner[0, 0, 0, 0, 1] = 1;
        Assert.Equal(37, NestedAccess(outer, inner, 0));
        Assert.Throws<IndexOutOfRangeException>(() => NestedAccess(null, inner, 1));
        Assert.Throws<NullReferenceException>(() => NestedAccess(outer, null, 0));
    }
}
