// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Loop cloning for multi-dimensional (rectangular) array bounds checks.
//
// Each test method is shaped so the fast clone (with bounds checks stripped)
// is correct, and the slow clone (with bounds checks intact) is also
// exercised by inputs that would OOB the fast path. The JIT picks between
// them via runtime cloning conditions, so we need both paths to produce the
// right answer (or the expected throw).

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class MdArrayCloning
{
    // ---- Helpers ----------------------------------------------------------

    static int[,] Make2D(int rows, int cols)
    {
        var a = new int[rows, cols];
        for (int i = 0; i < rows; i++)
            for (int j = 0; j < cols; j++)
                a[i, j] = i * cols + j;
        return a;
    }

    static int[,,] Make3D(int d0, int d1, int d2)
    {
        var a = new int[d0, d1, d2];
        for (int i = 0; i < d0; i++)
            for (int j = 0; j < d1; j++)
                for (int k = 0; k < d2; k++)
                    a[i, j, k] = ((i * d1) + j) * d2 + k;
        return a;
    }

    // ---- Cases where bounds-check elimination must be safe ----------------

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Sum2DDimLimits(int[,] a)
    {
        // Loop bounds match a.GetLength(d), accessed at a[i,j] — both bounds
        // checks should be removable.
        int sum = 0;
        for (int i = 0; i < a.GetLength(0); i++)
            for (int j = 0; j < a.GetLength(1); j++)
                sum += a[i, j];
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Sum2DCachedLengths(int[,] a)
    {
        // Lengths cached in locals; the cloning condition becomes
        // (len0 <= a.MDLen0) which is true iff the local matches.
        int len0 = a.GetLength(0);
        int len1 = a.GetLength(1);
        int sum  = 0;
        for (int i = 0; i < len0; i++)
            for (int j = 0; j < len1; j++)
                sum += a[i, j];
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Sum3D(int[,,] a)
    {
        int sum = 0;
        for (int i = 0; i < a.GetLength(0); i++)
            for (int j = 0; j < a.GetLength(1); j++)
                for (int k = 0; k < a.GetLength(2); k++)
                    sum += a[i, j, k];
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Sum2DLengthMinus1(int[,] a)
    {
        // Limit with constant offset: `< len - 1`.
        int sum = 0;
        for (int i = 0; i < a.GetLength(0) - 1; i++)
            for (int j = 0; j < a.GetLength(1) - 1; j++)
                sum += a[i, j];
        return sum;
    }

    // ---- Cases where the runtime guard must NOT pick the fast path --------

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Sum2DUserLimit(int[,] a, int limit0, int limit1)
    {
        // User-provided limits can exceed the actual dim lengths. The cloning
        // condition checks at runtime that limit0 <= MDLen0 and limit1 <=
        // MDLen1 before taking the fast clone. If a limit is too large the
        // slow clone (with bounds checks) must run.
        int sum = 0;
        for (int i = 0; i < limit0; i++)
            for (int j = 0; j < limit1; j++)
                sum += a[i, j];
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Sum2DSwappedIndex(int[,] a, int limit)
    {
        // Iter var bound by `limit` is used as the column index, which is
        // bounded by a.GetLength(1) NOT a.GetLength(0). For a square matrix
        // this is fine; for non-square it can OOB when limit > GetLength(1).
        int sum = 0;
        for (int i = 0; i < limit; i++)
            sum += a[0, i];
        return sum;
    }

    // ---- "Trivially true" condition path ----------------------------------

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Sum2DDirect(int[,] a, int dim0Hint)
    {
        // Outer loop limit == access dim length, so the cloning condition
        // MDLen0 <= MDLen0 is trivially true → static optimization removes
        // the dim-0 bounds check without duplicating the loop.
        int sum = 0;
        for (int i = 0; i < a.GetLength(0); i++)
            sum += a[i, dim0Hint];
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Sum2DStride2(int[,] a)
    {
        // Non-unit (constant) stride.
        int sum = 0;
        for (int i = 0; i < a.GetLength(0); i += 2)
            for (int j = 0; j < a.GetLength(1); j += 2)
                sum += a[i, j];
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Sum2DDecreasing(int[,] a)
    {
        // Decreasing IV (i--).
        int sum = 0;
        for (int i = a.GetLength(0) - 1; i >= 0; i--)
            for (int j = a.GetLength(1) - 1; j >= 0; j--)
                sum += a[i, j];
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Sum2DDecStride2(int[,] a)
    {
        // Decreasing non-unit stride.
        int sum = 0;
        for (int i = a.GetLength(0) - 1; i >= 0; i -= 2)
            for (int j = a.GetLength(1) - 1; j >= 0; j -= 2)
                sum += a[i, j];
        return sum;
    }

    // ---- foreach / GetUpperBound (issue #103321) pattern ------------------

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Sum2DGetUpperBound(int[,] a)
    {
        // The literal #103321 example: inline GetLowerBound/GetUpperBound on
        // each iteration. The limit shape is LOWER + LEN - 1; the iter init
        // is LOWER. Both must match for the bounded-range optimization to
        // fire (and strip the per-dim bounds check on a[i,j]).
        int sum = 0;
        for (int i = a.GetLowerBound(0); i < a.GetUpperBound(0); i++)
            for (int j = a.GetLowerBound(1); j < a.GetUpperBound(1); j++)
                sum += a[i, j];
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Sum2DGetUpperBoundLE(int[,] a)
    {
        // LE variant: iterates the full valid index range.
        int sum = 0;
        for (int i = a.GetLowerBound(0); i <= a.GetUpperBound(0); i++)
            for (int j = a.GetLowerBound(1); j <= a.GetUpperBound(1); j++)
                sum += a[i, j];
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Sum1DOfDim0_GetUpperBound(int[,] a)
    {
        // Bounded-range loop on dim 0, body accesses a fixed column. The
        // dim-0 bounds check on the access is removable.
        int sum = 0;
        for (int i = a.GetLowerBound(0); i < a.GetUpperBound(0); i++)
            sum += a[i, 0];
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Sum2DInitMismatch(int[,] a)
    {
        // Init is a const 0 but the limit is GetUpperBound(0) — these don't
        // bracket the iter range to the valid index range, so the bounded-
        // range optimization must not apply. Cloning bails entirely (limit
        // not analyzable) so the original loop with bounds checks runs.
        int sum = 0;
        for (int i = 0; i < a.GetUpperBound(0); i++)
            sum += a[i, 0];
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Sum2DForeach(int[,] a)
    {
        int sum = 0;
        foreach (int value in a)
            sum += value;
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Sum3DForeach(int[,,] a)
    {
        int sum = 0;
        foreach (int value in a)
            sum += value;
        return sum;
    }

    // ---- Sum reference (slow but obviously correct) -----------------------

    static int Sum2DSlow(int[,] a, int limit0, int limit1)
    {
        int sum = 0;
        for (int i = 0; i < limit0; i++)
            for (int j = 0; j < limit1; j++)
                sum += a[i, j];
        return sum;
    }

    // ======================================================================
    // Tests where the fast path is provably safe.
    // ======================================================================

    [Theory]
    [InlineData(1, 1)]
    [InlineData(3, 5)]
    [InlineData(10, 20)]
    [InlineData(100, 1)]
    [InlineData(1, 100)]
    public static void DimLimits_AlwaysSafe(int rows, int cols)
    {
        var a   = Make2D(rows, cols);
        int got = Sum2DDimLimits(a);
        int want = Sum2DSlow(a, rows, cols);
        Assert.Equal(want, got);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(3, 5)]
    [InlineData(10, 20)]
    public static void CachedLengths_AlwaysSafe(int rows, int cols)
    {
        var a   = Make2D(rows, cols);
        int got = Sum2DCachedLengths(a);
        int want = Sum2DSlow(a, rows, cols);
        Assert.Equal(want, got);
    }

    [Theory]
    [InlineData(1, 1, 1)]
    [InlineData(2, 3, 4)]
    [InlineData(10, 5, 7)]
    public static void Sum3D_AlwaysSafe(int d0, int d1, int d2)
    {
        var a   = Make3D(d0, d1, d2);
        int got = Sum3D(a);
        int want = 0;
        for (int i = 0; i < d0; i++)
            for (int j = 0; j < d1; j++)
                for (int k = 0; k < d2; k++)
                    want += ((i * d1) + j) * d2 + k;
        Assert.Equal(want, got);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(3, 5)]
    [InlineData(10, 20)]
    public static void LengthMinus1_AlwaysSafe(int rows, int cols)
    {
        var a   = Make2D(rows, cols);
        int got = Sum2DLengthMinus1(a);
        int want = 0;
        for (int i = 0; i < rows - 1; i++)
            for (int j = 0; j < cols - 1; j++)
                want += a[i, j];
        Assert.Equal(want, got);
    }

    [Fact]
    public static void Direct_TriviallyTrueCondition()
    {
        // Trivially-true cloning condition (MDLen0 <= MDLen0).
        var a   = Make2D(10, 5);
        int got = Sum2DDirect(a, 2);
        int want = 0;
        for (int i = 0; i < 10; i++)
            want += a[i, 2];
        Assert.Equal(want, got);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(3, 5)]
    [InlineData(10, 20)]
    public static void Stride2_AlwaysSafe(int rows, int cols)
    {
        var a   = Make2D(rows, cols);
        int got = Sum2DStride2(a);
        int want = 0;
        for (int i = 0; i < rows; i += 2)
            for (int j = 0; j < cols; j += 2)
                want += a[i, j];
        Assert.Equal(want, got);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(3, 5)]
    [InlineData(10, 20)]
    public static void Decreasing_AlwaysSafe(int rows, int cols)
    {
        var a   = Make2D(rows, cols);
        int got = Sum2DDecreasing(a);
        int want = Sum2DSlow(a, rows, cols);
        Assert.Equal(want, got);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(3, 5)]
    [InlineData(10, 20)]
    public static void DecStride2_AlwaysSafe(int rows, int cols)
    {
        var a   = Make2D(rows, cols);
        int got = Sum2DDecStride2(a);
        int want = 0;
        for (int i = rows - 1; i >= 0; i -= 2)
            for (int j = cols - 1; j >= 0; j -= 2)
                want += a[i, j];
        Assert.Equal(want, got);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 3)]
    [InlineData(10, 20)]
    public static void GetUpperBound_AlwaysSafe(int rows, int cols)
    {
        // Issue #103321: `for (i = arr.GetLowerBound(d); i < arr.GetUpperBound(d); ...)`.
        var a   = Make2D(rows, cols);
        int got = Sum2DGetUpperBound(a);
        int want = 0;
        for (int i = 0; i < rows - 1; i++)
            for (int j = 0; j < cols - 1; j++)
                want += a[i, j];
        Assert.Equal(want, got);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 3)]
    [InlineData(10, 20)]
    public static void GetUpperBoundLE_AlwaysSafe(int rows, int cols)
    {
        var a   = Make2D(rows, cols);
        int got = Sum2DGetUpperBoundLE(a);
        int want = Sum2DSlow(a, rows, cols);
        Assert.Equal(want, got);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(5, 7)]
    public static void Sum1DOfDim0_GetUpperBound_AlwaysSafe(int rows, int cols)
    {
        var a   = Make2D(rows, cols);
        int got = Sum1DOfDim0_GetUpperBound(a);
        int want = 0;
        for (int i = 0; i < rows - 1; i++)
            want += a[i, 0];
        Assert.Equal(want, got);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(5, 7)]
    public static void InitMismatch_StillCorrect(int rows, int cols)
    {
        // Init constant 0 doesn't match LOWER (= 0 for int[,], but the JIT
        // can't lexically prove that). The bounded-range optimization must
        // not apply; the test asserts only that the answer is still correct.
        var a   = Make2D(rows, cols);
        int got = Sum2DInitMismatch(a);
        int want = 0;
        for (int i = 0; i < rows - 1; i++)
            want += a[i, 0];
        Assert.Equal(want, got);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(3, 5)]
    [InlineData(10, 20)]
    public static void Foreach2D_AlwaysSafe(int rows, int cols)
    {
        var a    = Make2D(rows, cols);
        int got  = Sum2DForeach(a);
        int want = Sum2DSlow(a, rows, cols);
        Assert.Equal(want, got);
    }

    [Theory]
    [InlineData(1, 1, 1)]
    [InlineData(2, 3, 4)]
    [InlineData(10, 5, 7)]
    public static void Foreach3D_AlwaysSafe(int d0, int d1, int d2)
    {
        var a   = Make3D(d0, d1, d2);
        int got = Sum3DForeach(a);
        int want = 0;
        for (int i = 0; i < d0; i++)
            for (int j = 0; j < d1; j++)
                for (int k = 0; k < d2; k++)
                    want += ((i * d1) + j) * d2 + k;
        Assert.Equal(want, got);
    }

    // ======================================================================
    // Tests where the user-provided limits stay within bounds.
    // The fast path is taken and the answer must match the slow path.
    // ======================================================================

    [Theory]
    [InlineData(10, 20, 10, 20)]   // limits == lengths
    [InlineData(10, 20,  5,  7)]   // limits well inside
    [InlineData(10, 20,  0,  5)]   // zero outer iterations
    [InlineData(10, 20,  5,  0)]   // zero inner iterations
    public static void UserLimit_InBounds_FastPath(int rows, int cols, int l0, int l1)
    {
        var a    = Make2D(rows, cols);
        int got  = Sum2DUserLimit(a, l0, l1);
        int want = Sum2DSlow(a, l0, l1);
        Assert.Equal(want, got);
    }

    // ======================================================================
    // Tests where the limit exceeds a dim length → the cloning condition
    // must steer execution into the slow clone, which throws IOORE.
    // ======================================================================

    [Theory]
    [InlineData(10, 20, 11, 20)]   // outer limit > dim 0
    [InlineData(10, 20, 10, 21)]   // inner limit > dim 1
    [InlineData(10, 20, 100, 5)]   // outer way past dim 0
    public static void UserLimit_OutOfBounds_Throws(int rows, int cols, int l0, int l1)
    {
        var a = Make2D(rows, cols);
        Assert.Throws<IndexOutOfRangeException>(() => Sum2DUserLimit(a, l0, l1));
    }

    // ======================================================================
    // Cross-dim cases: iter var bound by `limit` is used as a different dim.
    // Square matrix: fast path is correct. Non-square with limit > other-dim:
    // must throw.
    // ======================================================================

    [Theory]
    [InlineData(5, 5, 5)]    // square, in-bounds for dim 1 (cols=5)
    [InlineData(5, 5, 3)]    // partial
    [InlineData(5, 5, 0)]    // empty
    [InlineData(3, 7, 7)]    // non-square, in-bounds for dim 1 (cols=7)
    [InlineData(7, 3, 3)]    // non-square, in-bounds for dim 1 (cols=3)
    public static void Swapped_InBounds(int rows, int cols, int limit)
    {
        var a    = Make2D(rows, cols);
        int got  = Sum2DSwappedIndex(a, limit);
        int want = 0;
        for (int i = 0; i < limit; i++)
            want += a[0, i];
        Assert.Equal(want, got);
    }

    [Theory]
    [InlineData(3, 7, 8)]   // limit > cols (dim 1)
    [InlineData(7, 3, 4)]   // limit > cols (dim 1)
    [InlineData(5, 5, 6)]   // limit > both
    public static void Swapped_OutOfBounds_Throws(int rows, int cols, int limit)
    {
        var a = Make2D(rows, cols);
        Assert.Throws<IndexOutOfRangeException>(() => Sum2DSwappedIndex(a, limit));
    }

    // ======================================================================
    // Bounded MD array (non-zero lower bounds): Array.CreateInstance with
    // explicit lower bounds. The morph form subtracts MDARR_LOWER_BOUND
    // (read at runtime), so cloning must remain sound even when the lower
    // bound isn't zero.
    // ======================================================================

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Sum2DBounded(Array a)
    {
        int[,] md = (int[,])a;

        // Iterate dim 0 from 0 to GetLength(0)-1, accessing a[i, 0]. Note:
        // for a bounded MD array, valid indices are
        // [GetLowerBound(d), GetUpperBound(d)], NOT [0, GetLength(d)).
        // The bounds checks (after morph) subtract the lower bound, so
        // accessing at `i` with i < GetLength(d) (when lower bound != 0)
        // throws IOORE for the first iteration if lower bound > 0.
        int sum = 0;
        for (int i = 0; i < md.GetLength(0); i++)
            sum += md[i, 0];
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int SumCrossArrayBoundedRange(Array bounds, int[,] data)
    {
        int[,] source = (int[,])bounds;

        int sum = 0;
        for (int i = source.GetLowerBound(0); i < source.GetUpperBound(0); i++)
            sum += data[i, 0];
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int SumSzArrayBoundedRange(Array bounds, int[] data)
    {
        int[,] source = (int[,])bounds;

        int sum = 0;
        for (int i = source.GetLowerBound(0); i < source.GetUpperBound(0); i++)
            sum += data[i];
        return sum;
    }

    [Fact]
    public static void Bounded_NonZeroLowerBound_Throws()
    {
        var a = Array.CreateInstance(typeof(int), new int[] { 4, 4 }, new int[] { 10, 10 });
        // Indexing at 0 below the [10, 10] lower bound must throw, whether or
        // not cloning fires.
        Assert.Throws<IndexOutOfRangeException>(() => Sum2DBounded(a));
    }

    [Fact]
    public static void CrossArrayBoundedRange_NonZeroLowerBound_Throws()
    {
        var sourceBounds = Array.CreateInstance(typeof(int), new int[] { 4, 4 }, new int[] { 10, 10 });
        var data         = Make2D(4, 1);
        Assert.Throws<IndexOutOfRangeException>(() => SumCrossArrayBoundedRange(sourceBounds, data));
    }

    [Fact]
    public static void SzArrayBoundedRange_NonZeroLowerBound_Throws()
    {
        var sourceBounds = Array.CreateInstance(typeof(int), new int[] { 4, 4 }, new int[] { 10, 10 });
        var data         = new int[4];
        Assert.Throws<IndexOutOfRangeException>(() => SumSzArrayBoundedRange(sourceBounds, data));
    }

    [Fact]
    public static void NullArray_ZeroTrip_DoesNotThrow()
    {
        Assert.Equal(0, Sum2DUserLimit(null!, 0, 0));
    }
}
