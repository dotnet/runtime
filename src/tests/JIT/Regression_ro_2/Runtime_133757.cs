// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace Runtime_133757;

public class Runtime_133757
{
    private const int RowLength = 100;
    private const int ShortRowLength = 1;

    [Theory]
    [InlineData(nameof(DirectStore))]
    [InlineData(nameof(ReplaceViaCall))]
    [InlineData(nameof(StoreIntoRow))]
    [InlineData(nameof(StoreViaAlias))]
    [InlineData(nameof(StoreViaRef))]
    public static void ReplacedRowIsBoundsChecked(string method)
    {
        int[][] rows = MakeRows(RowLength);
        Assert.Throws<IndexOutOfRangeException>(() =>
        {
            switch (method)
            {
                case nameof(DirectStore):
                    DirectStore(rows, 0, RowLength);
                    break;
                case nameof(ReplaceViaCall):
                    ReplaceViaCall(rows, 0, RowLength);
                    break;
                case nameof(StoreIntoRow):
                    StoreIntoRow(rows, 0, RowLength);
                    break;
                case nameof(StoreViaAlias):
                    StoreViaAlias(rows, 0, RowLength, rows);
                    break;
                case nameof(StoreViaRef):
                    StoreViaRef(rows, 0, RowLength, ref rows[0]);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(method));
            }
        });
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(32)]
    public static void UnchangedRowsRemainUsable(int count)
    {
        int[][] rows = MakeRows(count);
        string?[] tags = new string?[count];
        int expected = count * (count - 1) / 2;

        FillRow(rows, 0, count);
        Assert.Equal(expected, SumRow(rows, 0, count));
        Assert.Equal(expected, SumAndTag(rows, 0, count, tags, "tag"));
        Assert.All(tags, tag => Assert.Equal("tag", tag));
        Assert.Equal(expected, SumAndTag(rows, 0, count, tags, null));
        Assert.All(tags, tag => Assert.Null(tag));
    }

    [Fact]
    public static void CapturedRowRemainsUsable()
    {
        int[][] rows = MakeRows(RowLength);
        rows[0][7] = 42;
        Assert.Equal(42, SumCapturedRow(rows, 0, RowLength));
    }

    [Fact]
    public static void ReferenceElementWritesRemainUsable()
    {
        string?[][] rows = new[] { new string?[RowLength] };
        FillStringRow(rows, 0, RowLength, "tag");
        Assert.All(rows[0], tag => Assert.Equal("tag", tag));
        FillStringRow(rows, 0, RowLength, null);
        Assert.All(rows[0], tag => Assert.Null(tag));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int[][] MakeRows(int count)
    {
        return new[] { new int[count] };
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int DirectStore(int[][] rows, int k, int n)
    {
        int sum = 0;
        for (int i = 0; i < n; i++)
        {
            sum += rows[k][i];
            rows[k] = new int[ShortRowLength];
        }
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int ReplaceViaCall(int[][] rows, int k, int n)
    {
        int sum = 0;
        for (int i = 0; i < n; i++)
        {
            sum += rows[k][i];
            Replace(rows, k);
        }
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static void StoreIntoRow(int[][] rows, int k, int n)
    {
        for (int i = 0; i < n; i++)
        {
            rows[k][i] = i;
            rows[k] = new int[ShortRowLength];
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int StoreViaAlias(int[][] rows, int k, int n, object[] alias)
    {
        int sum = 0;
        for (int i = 0; i < n; i++)
        {
            sum += rows[k][i];
            alias[k] = new int[ShortRowLength];
        }
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int StoreViaRef(int[][] rows, int k, int n, ref int[] row)
    {
        int sum = 0;
        for (int i = 0; i < n; i++)
        {
            sum += rows[k][i];
            row = new int[ShortRowLength];
        }
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Replace(int[][] rows, int k)
    {
        rows[k] = new int[ShortRowLength];
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int SumRow(int[][] rows, int k, int n)
    {
        int sum = 0;
        for (int i = 0; i < n; i++)
        {
            sum += rows[k][i];
        }
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static void FillRow(int[][] rows, int k, int n)
    {
        for (int i = 0; i < n; i++)
        {
            rows[k][i] = i;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int SumAndTag(int[][] rows, int k, int n, string?[] tags, string? tag)
    {
        int sum = 0;
        for (int i = 0; i < n; i++)
        {
            sum += rows[k][i];
            tags[i] = tag;
        }
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int SumCapturedRow(int[][] rows, int k, int n)
    {
        int[] row = rows[k];
        int sum = 0;
        for (int i = 0; i < n; i++)
        {
            sum += row[i];
            rows[k] = new int[ShortRowLength];
        }
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static void FillStringRow(string?[][] rows, int k, int n, string? tag)
    {
        for (int i = 0; i < n; i++)
        {
            rows[k][i] = tag;
        }
    }
}
