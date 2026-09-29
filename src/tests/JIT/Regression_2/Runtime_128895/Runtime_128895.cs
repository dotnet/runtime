// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// optOptimizeBoolsCondBlock folded a pair of BBJ_COND blocks of the form
// "(x == 0) || (x relop 0)" into a single directional comparison (e.g. LT/LE/GE)
// via SetOper, which preserved gtFlags. When the EQ side was imported from
// "beq.un"/"bne.un" (the typical C# pattern for "long == 0") it carried a
// meaningless GTF_UNSIGNED that became semantically active on the rewritten
// directional relop, producing "x <_U 0" (always false) or "x >=_U 0"
// (always true) instead of the intended signed comparison.

namespace Runtime_128895;

using System.Runtime.CompilerServices;
using Xunit;

public abstract class Base
{
    public long fValue;
    public abstract int Compare(long other);
}

public sealed class Cell : Base
{
    public override int Compare(long other)
    {
        if (fValue == other) return 0;
        if (fValue > other) return 1;
        return -1;
    }
}

public static class CellPool
{
    private static readonly Cell[] s_pool = CreatePool();
    private static int s_slot;

    private static Cell[] CreatePool()
    {
        Cell[] pool = new Cell[16];
        for (int i = 0; i < pool.Length; i++)
        {
            pool[i] = new Cell();
        }

        return pool;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Cell Make(int idx)
    {
        Cell c = s_pool[s_slot++ & 15];
        c.fValue = idx >= 0 ? idx + 1 : -1;
        return c;
    }
}

public class Runtime_128895
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int FindCharLocal(string s, string needle) =>
        s.IndexOfAny(needle.ToCharArray(), 0, s.Length);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool ValidateMarks(string s)
    {
        bool r = false;
        Cell mark1 = CellPool.Make(FindCharLocal(s, "\u00ab"));
        Cell mark2 = CellPool.Make(FindCharLocal(s, "\u00bb"));
        Cell mark3 = CellPool.Make(FindCharLocal(s, "\u201c"));
        if ((mark1.Compare(0) == 0 || mark1.Compare(0) > 0)
         || (mark2.Compare(0) == 0 || mark2.Compare(0) > 0)
         || (mark3.Compare(0) == 0 || mark3.Compare(0) > 0))
        {
            r = true;
        }

        return r;
    }

    private static readonly string[] s_inputs = new[]
    {
        "abc", "def", "ghi", "jkl", "mno", "pqr", "abc\u00abdef", "xyz"
    };

    [Fact]
    public static int TestEntryPoint()
    {
        // Drive ValidateMarks through Tier0 -> Instrumented Tier0 -> Tier1+PGO.
        for (int i = 0; i < 1000; i++)
        {
            ValidateMarks(s_inputs[i & 7]);
            System.Threading.Thread.Sleep(1);
        }

        System.Threading.Thread.Sleep(100);

        int failures = 0;
        for (int k = 0; k < s_inputs.Length; k++)
        {
            string s = s_inputs[k];
            bool expected = s.Contains('\u00ab') || s.Contains('\u00bb') || s.Contains('\u201c');
            bool actual = ValidateMarks(s);
            if (expected != actual)
            {
                failures++;
            }
        }

        return failures == 0 ? 100 : 1;
    }
}
