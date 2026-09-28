// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Numerics.Tensors;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DotnetFuzzing.Fuzzers;

/// <summary>
/// Fuzzes the <see cref="Tensor"/> operations over strided, sliced, reshaped and broadcast tensor spans.
/// Operands are views built from guard-paged storage through the public view APIs (explicit/permuted/padded/zero strides,
/// Slice, Reshape, Squeeze, SqueezeDimension, Unsqueeze), and every result is compared with a reference computed through the
/// tensor indexer with right-aligned broadcasting. Storage outside the destination view is filled with canaries that must
/// survive, and operand storage must be left untouched.
/// </summary>
internal sealed class TensorOperationsFuzzer : IFuzzer
{
    private const int MaxRank = 4;
    private const int Canary = unchecked((int)0xCA11AB1E);

    // Known issues on main, avoided unless TENSOR_FUZZ_STRICT=1:
    // * The pairwise comparisons (EqualsAll/Any, GreaterThan*, LessThan*...) iterate over the shape of whichever operand has
    //   the larger FlattenedLength instead of the broadcast shape: bidirectional broadcasts give wrong answers, and a
    //   lower-rank "larger" operand trips Debug.Assert(indexes.Length >= Rank) in TensorShape.AdjustToNextIndex.
    // * The *Any comparisons return true for empty tensors.
    // * Squeeze() of a tensor whose lengths are all 1 produces a rank-0 view with FlattenedLength 0, losing the element.
    // * ResizeTo does not zero the part of a larger destination beyond the source, although that is documented.
    private static readonly bool s_strict = Environment.GetEnvironmentVariable("TENSOR_FUZZ_STRICT") == "1";

    public string[] TargetAssemblies { get; } = ["System.Numerics.Tensors"];
    public string[] TargetCoreLibPrefixes => [];

    public void FuzzTarget(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < 4)
        {
            return;
        }

        var reader = new Reader(bytes);
        int op = reader.Byte() % 44;
        PoisonPagePlacement placement = (reader.Byte() & 1) != 0 ? PoisonPagePlacement.Before : PoisonPagePlacement.After;

        // The logical result shape; operands are right-aligned and may have fewer dimensions or length-1 (broadcast) dimensions.
        // (Rank 0 isn't expressible: empty lengths mean "one dimension spanning the storage".)
        int rank = 1 + reader.Byte() % MaxRank;
        nint[] resultLengths = new nint[rank];
        for (int i = 0; i < rank; i++)
        {
            resultLengths[i] = reader.Byte() % 8 == 0 ? 0 : 1 + reader.Byte() % 4;
        }

        using Operand x = Operand.Create(ref reader, BroadcastLengths(ref reader, resultLengths), placement, seed: 1, allowZeroStrides: true);
        using Operand y = Operand.Create(ref reader, BroadcastLengths(ref reader, resultLengths), placement, seed: 1000, allowZeroStrides: true);
        using Operand d = Operand.Create(ref reader, resultLengths, placement, seed: 0, allowZeroStrides: (reader.Byte() & 7) == 0);
        if (x.Failed || y.Failed || d.Failed)
        {
            return;
        }

        int scalar = (sbyte)reader.Byte();
        string context = $"op {op}: x {x}, y {y}, destination {d}, scalar {scalar}";

        x.CheckView(context + " [x]");
        y.CheckView(context + " [y]");
        d.CheckView(context + " [destination]");

        d.FillCanaries();
        int[] xBefore = x.Storage.ToArray(), yBefore = y.Storage.ToArray();

        try
        {
            RunOperation(op, x, y, d, scalar, context);
        }
        catch (ArgumentException)
        {
            // Rejected shapes are fine, but nothing may have been written outside the destination view.
        }

        Check(x.Storage.SequenceEqual(xBefore), context, "x storage was modified");
        Check(y.Storage.SequenceEqual(yBefore), context, "y storage was modified");
        d.CheckCanaries(context);
    }

    private static nint[] BroadcastLengths(ref Reader reader, nint[] resultLengths)
    {
        // Drop some leading dimensions and turn some others into length-1 broadcast dimensions.
        int drop = reader.Byte() % resultLengths.Length / 2;
        nint[] lengths = resultLengths[drop..];
        byte ones = reader.Byte();
        for (int i = 0; i < lengths.Length; i++)
        {
            if ((ones & (1 << i)) != 0 && lengths[i] != 0 && (ones & 0x80) != 0)
            {
                lengths[i] = 1;
            }
        }

        return lengths;
    }

    private static void RunOperation(int op, Operand x, Operand y, Operand d, int scalar, string context)
    {
        ReadOnlyTensorSpan<int> xs = x.View, ys = y.View;
        TensorSpan<int> ds = d.View;

        switch (op)
        {
            case 0: Tensor.Add(xs, ys, ds); CheckBinary(x, y, d, static (a, b) => unchecked(a + b), context); break;
            case 1: Tensor.Subtract(xs, ys, ds); CheckBinary(x, y, d, static (a, b) => unchecked(a - b), context); break;
            case 2: Tensor.Multiply(xs, ys, ds); CheckBinary(x, y, d, static (a, b) => unchecked(a * b), context); break;
            case 3: Tensor.Min(xs, ys, ds); CheckBinary(x, y, d, Math.Min, context); break;
            case 4: Tensor.Max(xs, ys, ds); CheckBinary(x, y, d, Math.Max, context); break;
            case 5: Tensor.BitwiseAnd(xs, ys, ds); CheckBinary(x, y, d, static (a, b) => a & b, context); break;
            case 6: Tensor.Xor(xs, ys, ds); CheckBinary(x, y, d, static (a, b) => a ^ b, context); break;
            case 7: Tensor.Negate(xs, ds); CheckBinary(x, x, d, static (a, _) => unchecked(-a), context); break;
            case 8: Tensor.OnesComplement(xs, ds); CheckBinary(x, x, d, static (a, _) => ~a, context); break;
            case 9: Tensor.Add(xs, scalar, ds); CheckBinary(x, x, d, (a, _) => unchecked(a + scalar), context); break;
            case 10: Tensor.Subtract(scalar, xs, ds); CheckBinary(x, x, d, (a, _) => unchecked(scalar - a), context); break;
            case 11: Tensor.ShiftLeft(xs, scalar & 31, ds); CheckBinary(x, x, d, (a, _) => a << (scalar & 31), context); break;
            case 12: Tensor.BroadcastTo(xs, ds); CheckBinary(x, x, d, static (a, _) => a, context); break;
            case 13: xs.CopyTo(ds); CheckBinary(x, x, d, static (a, _) => a, context); break;
            case 14:
                if (xs.TryCopyTo(ds))
                {
                    CheckBinary(x, x, d, static (a, _) => a, context);
                }

                break;
            case 15:
                ds.Fill(scalar);
                CheckBinary(d, d, d, (_, _) => scalar, context);
                break;
            case 16:
                ds.Clear();
                CheckBinary(d, d, d, static (_, _) => 0, context);
                break;
            case 17: Check(Tensor.Sum(xs) == Fold(x, 0, static (a, b) => unchecked(a + b)), context, $"Sum {Tensor.Sum(xs)}"); break;
            case 18: Check(Tensor.Product(xs) == Fold(x, 1, static (a, b) => unchecked(a * b)), context, $"Product {Tensor.Product(xs)}"); break;
            case 19:
                if (!xs.IsEmpty)
                {
                    Check(Tensor.Max(xs) == Fold(x, int.MinValue, Math.Max), context, $"Max {Tensor.Max(xs)}");
                    Check(Tensor.Min(xs) == Fold(x, int.MaxValue, Math.Min), context, $"Min {Tensor.Min(xs)}");
                }

                break;
            case 20:
                if (!xs.IsEmpty)
                {
                    int[] flat = x.Flatten();
                    nint expectedMax = Array.IndexOf(flat, flat.Max()), expectedMin = Array.IndexOf(flat, flat.Min());
                    Check(Tensor.IndexOfMax(xs) == expectedMax, context, $"IndexOfMax {Tensor.IndexOfMax(xs)}, expected {expectedMax}");
                    Check(Tensor.IndexOfMin(xs) == expectedMin, context, $"IndexOfMin {Tensor.IndexOfMin(xs)}, expected {expectedMin}");
                }

                break;
            case 21 or 22 or 23 when !s_strict && !PairwiseIsReliable(xs, ys):
                break;
            case 21: CheckPairwise(x, y, Tensor.EqualsAll(xs, ys), Tensor.EqualsAny(xs, ys), static (a, b) => a == b, "Equals", context); break;
            case 22: CheckPairwise(x, y, Tensor.GreaterThanAll(xs, ys), Tensor.GreaterThanAny(xs, ys), static (a, b) => a > b, "GreaterThan", context); break;
            case 23: CheckPairwise(x, y, Tensor.LessThanOrEqualAll(xs, ys), Tensor.LessThanOrEqualAny(xs, ys), static (a, b) => a <= b, "LessThanOrEqual", context); break;
            case 24:
                {
                    // Dot over two same-shaped operands.
                    int expected = 0;
                    int[] xf = x.Flatten(), yf = y.Flatten();
                    if (xf.Length == yf.Length && xs.Lengths.SequenceEqual(ys.Lengths))
                    {
                        for (int i = 0; i < xf.Length; i++)
                        {
                            expected = unchecked(expected + xf[i] * yf[i]);
                        }

                        Check(Tensor.Dot(xs, ys) == expected, context, $"Dot {Tensor.Dot(xs, ys)}, expected {expected}");
                    }

                    break;
                }
            case 25:
                {
                    // Reverse writes the flattened elements of x in reverse order.
                    Tensor.Reverse(xs, ds);
                    int[] xf = x.Flatten();
                    Array.Reverse(xf);
                    if (!d.HasAliasing && d.View.Lengths.SequenceEqual(xs.Lengths))
                    {
                        Check(d.Flatten().AsSpan().SequenceEqual(xf), context, $"Reverse wrote [{string.Join(", ", d.Flatten())}], expected [{string.Join(", ", xf)}]");
                    }

                    break;
                }
            case 26: CheckReshape(x, xs.Reshape(ReshapeTarget(xs, scalar)), context); break;
            case 27:
                if (s_strict || xs.Lengths.ContainsAnyExcept(1))
                {
                    CheckReshape(x, xs.Squeeze(), context);
                }

                break;
            case 28: CheckReshape(x, xs.Unsqueeze((scalar & 0x7F) % (xs.Rank + 1)), context); break;
            default: RunStructuralOperation(op, x, y, d, scalar, context); break;
        }
    }

    private static void RunStructuralOperation(int op, Operand x, Operand y, Operand d, int scalar, string context)
    {
        ReadOnlyTensorSpan<int> xs = x.View, ys = y.View;
        TensorSpan<int> ds = d.View;
        int dim = xs.Rank == 0 ? 0 : (scalar & 0x7F) % xs.Rank;

        switch (op)
        {
            case 29:
                {
                    Tensor.ReverseDimension(xs, ds, dim);
                    nint[] xLengths = xs.Lengths.ToArray();
                    if (!d.HasAliasing && ds.Lengths.SequenceEqual(xLengths))
                    {
                        ForEachIndex(xLengths, index =>
                        {
                            nint[] source = (nint[])index.Clone();
                            source[dim] = xLengths[dim] - 1 - index[dim];
                            Check(d.View[index] == x.View[source], context, $"ReverseDimension({dim}) element [{string.Join(", ", index)}] is {d.View[index]}, expected {x.View[source]}");
                        });
                    }

                    break;
                }
            case 30:
                {
                    // SetSlice writes x into a sub-range of the destination and must leave the rest of the destination alone.
                    if (xs.Rank != ds.Rank)
                    {
                        break;
                    }

                    NRange[] ranges = new NRange[ds.Rank];
                    nint[] starts = new nint[ds.Rank];
                    nint[] xLengths = xs.Lengths.ToArray();
                    for (int i = 0; i < ds.Rank; i++)
                    {
                        starts[i] = Math.Max(0, Math.Min((scalar >> i) & 1, ds.Lengths[i] - xs.Lengths[i]));
                        ranges[i] = new NRange(starts[i], starts[i] + xs.Lengths[i]);
                    }

                    int[] before = d.Flatten();
                    Tensor.SetSlice(ds, xs, ranges);
                    if (!d.HasAliasing)
                    {
                        int k = 0;
                        ForEachIndex(ds.Lengths, index =>
                        {
                            bool inside = true;
                            nint[] source = new nint[index.Length];
                            for (int i = 0; i < index.Length; i++)
                            {
                                source[i] = index[i] - starts[i];
                                inside &= source[i] >= 0 && source[i] < xLengths[i];
                            }

                            int expected = inside ? x.View[source] : before[k];
                            Check(d.View[index] == expected, context, $"SetSlice element [{string.Join(", ", index)}] is {d.View[index]}, expected {expected}");
                            k++;
                        });
                    }

                    break;
                }
            case 31:
            case 32:
                {
                    // FilteredUpdate with a scalar or with (broadcast) values from x, under a dense bool filter shaped like the destination.
                    bool[] filterData = new bool[ds.FlattenedLength];
                    for (int i = 0; i < filterData.Length; i++)
                    {
                        filterData[i] = ((i * 7 + scalar) & 3) == 0;
                    }

                    ReadOnlyTensorSpan<bool> filter = new ReadOnlyTensorSpan<bool>(filterData, ds.Lengths, []);
                    int[] before = d.Flatten();
                    if (op == 31)
                    {
                        ds.FilteredUpdate(filter, scalar);
                    }
                    else
                    {
                        ds.FilteredUpdate(filter, xs);
                    }

                    if (!d.HasAliasing)
                    {
                        int k = 0;
                        ForEachIndex(ds.Lengths, index =>
                        {
                            int expected = filterData[k] ? (op == 31 ? scalar : x.At(index)) : before[k];
                            Check(d.View[index] == expected, context, $"FilteredUpdate element [{string.Join(", ", index)}] is {d.View[index]}, expected {expected}");
                            k++;
                        });
                    }

                    break;
                }
            case 33:
                {
                    // GetDimensionSpan(dim) enumerates the sub-tensors indexed by the first dim + 1 dimensions.
                    ReadOnlyTensorDimensionSpan<int> dimensionSpan = xs.GetDimensionSpan(dim);
                    nint count = 1;
                    for (int i = 0; i <= dim; i++)
                    {
                        count *= xs.Lengths[i];
                    }

                    Check(dimensionSpan.Length == count, context, $"GetDimensionSpan({dim}).Length is {dimensionSpan.Length}, expected {count}");
                    for (nint k = 0; k < Math.Min(count, 64); k++)
                    {
                        nint[] prefix = new nint[dim + 1];
                        nint rest = k;
                        for (int i = dim; i >= 0; i--)
                        {
                            prefix[i] = rest % xs.Lengths[i];
                            rest /= xs.Lengths[i];
                        }

                        ReadOnlyTensorSpan<int> slice = dimensionSpan[k];
                        ReadOnlySpan<nint> sliceLengths = slice.Lengths;
                        nint[] expectedLengths = dim + 1 < xs.Rank ? xs.Lengths[(dim + 1)..].ToArray() : [1];
                        Check(sliceLengths.SequenceEqual(expectedLengths), context, $"GetDimensionSpan({dim})[{k}] has lengths [{string.Join(", ", sliceLengths.ToArray())}] strides [{string.Join(", ", slice.Strides.ToArray())}], expected lengths [{string.Join(", ", expectedLengths)}]");
                        if (slice.IsEmpty)
                        {
                            continue;
                        }

                        try
                        {
                            nint[] probe = new nint[slice.Rank];
                            do
                            {
                                _ = slice[probe];
                            }
                            while (Increment(probe, sliceLengths));
                        }
                        catch (IndexOutOfRangeException)
                        {
                            Check(false, context, $"GetDimensionSpan({dim})[{k}] (lengths [{string.Join(", ", sliceLengths.ToArray())}] strides [{string.Join(", ", slice.Strides.ToArray())}] rank {slice.Rank}) throws IndexOutOfRangeException from its own indexer");
                        }

                        nint[] local = new nint[slice.Rank];
                        do
                        {
                            nint[] full = new nint[xs.Rank];
                            prefix.CopyTo(full, 0);
                            if (dim + 1 < xs.Rank)
                            {
                                local.CopyTo(full, dim + 1);
                            }

                            Check(slice[local] == x.View[full], context, $"GetDimensionSpan({dim})[{k}][{string.Join(", ", local)}] is {slice[local]}, expected x[{string.Join(", ", full)}] = {x.View[full]}");
                        }
                        while (Increment(local, sliceLengths));
                    }

                    break;
                }
            case 34:
                {
                    int splitCount = 1 + (scalar & 3);
                    Tensor<int>[] parts = Tensor.Split(xs, splitCount, dim);
                    Check(parts.Length == splitCount, context, $"Split returned {parts.Length} parts");
                    nint partLength = xs.Lengths[dim] / splitCount;
                    for (int p = 0; p < parts.Length; p++)
                    {
                        Tensor<int> part = parts[p];
                        Check(part.Lengths[dim] == partLength, context, $"Split part {p} has length {part.Lengths[dim]} in dimension {dim}, expected {partLength}");
                        int partIndex = p;
                        ForEachIndex(part.Lengths, index =>
                        {
                            nint[] source = (nint[])index.Clone();
                            source[dim] += partIndex * partLength;
                            Check(part[index] == x.View[source], context, $"Split({splitCount}, {dim}) part {partIndex}[{string.Join(", ", index)}] is {part[index]}, expected {x.View[source]}");
                        });
                    }

                    break;
                }
            case 35:
                {
                    Tensor<int> tx = x.ToTensor(), ty = y.ToTensor();
                    Tensor<int> result = Tensor.ConcatenateOnDimension(dim, [tx, ty]);
                    ForEachIndex(result.Lengths, index =>
                    {
                        nint[] source = (nint[])index.Clone();
                        bool first = source[dim] < tx.Lengths[dim];
                        if (!first)
                        {
                            source[dim] -= tx.Lengths[dim];
                        }

                        int expected = first ? tx[source] : ty[source];
                        Check(result[index] == expected, context, $"ConcatenateOnDimension({dim}) element [{string.Join(", ", index)}] is {result[index]}, expected {expected}");
                    });
                    break;
                }
            case 36:
                {
                    int stackDim = (scalar & 0x7F) % (xs.Rank + 1);
                    Tensor<int> tx = x.ToTensor();
                    Tensor<int> result = Tensor.StackAlongDimension(stackDim, [tx, tx]);
                    Check(result.Rank == tx.Rank + 1 && result.Lengths[stackDim] == 2, context, $"StackAlongDimension({stackDim}) has lengths [{string.Join(", ", result.Lengths.ToArray())}]");
                    ForEachIndex(result.Lengths, index =>
                    {
                        nint[] source = [.. index[..stackDim], .. index[(stackDim + 1)..]];
                        Check(result[index] == tx[source], context, $"StackAlongDimension({stackDim}) element [{string.Join(", ", index)}] is {result[index]}, expected {tx[source]}");
                    });
                    break;
                }
            case 37:
                {
                    Tensor<int> tx = x.ToTensor();
                    int[] permutation = Enumerable.Range(0, tx.Rank).ToArray();
                    for (int i = permutation.Length - 1; i > 0; i--)
                    {
                        int j = ((scalar >> i) & 0xFF) % (i + 1);
                        (permutation[i], permutation[j]) = (permutation[j], permutation[i]);
                    }

                    Tensor<int> permuted = tx.PermuteDimensions(permutation);
                    CheckPermuted(tx, permuted, permutation, "PermuteDimensions", context);
                    if (tx.Rank >= 2)
                    {
                        int[] swap = Enumerable.Range(0, tx.Rank).ToArray();
                        (swap[^1], swap[^2]) = (swap[^2], swap[^1]);
                        CheckPermuted(tx, Tensor.Transpose(tx), swap, "Transpose", context);
                    }

                    break;
                }
            case 38:
                {
                    // Documented: copies the flattened data; if the destination is bigger the rest is filled with zeros.
                    Tensor.ResizeTo(xs, ds);
                    if (!d.HasAliasing)
                    {
                        int[] source = x.Flatten();
                        int[] actual = d.Flatten();
                        for (int k = 0; k < actual.Length; k++)
                        {
                            int expected = k < source.Length ? source[k] : 0;
                            if (k >= source.Length && !s_strict)
                            {
                                break;
                            }

                            Check(actual[k] == expected, context, $"ResizeTo element {k} is {actual[k]}, expected {expected}");
                        }
                    }

                    break;
                }
            case 39:
                if (xs.TryBroadcastTo(ds))
                {
                    CheckBinary(x, x, d, static (a, _) => a, context);
                }

                break;
            case 40:
                {
                    Tensor<int> dense = Tensor.Create(x.Flatten(), xs.Lengths);
                    Check(xs.SequenceEqual(dense), context, "SequenceEqual(x, dense copy of x) is false");
                    bool expected = xs.Lengths.SequenceEqual(ys.Lengths) && x.Flatten().AsSpan().SequenceEqual(y.Flatten());
                    Check(xs.SequenceEqual(ys) == expected, context, $"SequenceEqual(x, y) is {!expected}");
                    break;
                }
            case 41:
                {
                    Tensor<bool> result = Tensor.GreaterThan(xs, ys);
                    ForEachIndex(result.Lengths, index =>
                    {
                        bool expected = x.At(index) > y.At(index);
                        Check(result[index] == expected, context, $"GreaterThan element [{string.Join(", ", index)}] is {result[index]}, expected {expected}");
                    });
                    break;
                }
            case 42:
                {
                    Tensor<int> reversed = Tensor.Reverse(xs);
                    int[] expected = x.Flatten();
                    Array.Reverse(expected);
                    int[] actual = new int[reversed.FlattenedLength];
                    reversed.FlattenTo(actual);
                    Check(reversed.Lengths.SequenceEqual(xs.Lengths) && actual.AsSpan().SequenceEqual(expected), context, $"Reverse(x) is [{string.Join(", ", actual)}], expected [{string.Join(", ", expected)}]");
                    break;
                }
            default:
                {
                    // Slicing a strided Tensor<T> object must view the same elements.
                    Tensor<int> tx = x.ToTensor();
                    NRange[] ranges = new NRange[tx.Rank];
                    nint[] starts = new nint[tx.Rank];
                    for (int i = 0; i < tx.Rank; i++)
                    {
                        starts[i] = Math.Min((scalar >> i) & 1, tx.Lengths[i]);
                        ranges[i] = new NRange(starts[i], tx.Lengths[i]);
                    }

                    Tensor<int> sliced = tx.Slice(ranges);
                    ForEachIndex(sliced.Lengths, index =>
                    {
                        nint[] source = new nint[index.Length];
                        for (int i = 0; i < index.Length; i++)
                        {
                            source[i] = index[i] + starts[i];
                        }

                        Check(sliced[index] == tx[source], context, $"Tensor.Slice element [{string.Join(", ", index)}] is {sliced[index]}, expected {tx[source]}");
                    });
                    break;
                }
        }
    }

    private static void CheckPermuted(Tensor<int> source, Tensor<int> permuted, int[] permutation, string name, string context)
    {
        for (int i = 0; i < permutation.Length; i++)
        {
            Check(permuted.Lengths[i] == source.Lengths[permutation[i]], context, $"{name}([{string.Join(", ", permutation)}]) has lengths [{string.Join(", ", permuted.Lengths.ToArray())}]");
        }

        ForEachIndex(permuted.Lengths, index =>
        {
            nint[] original = new nint[index.Length];
            for (int i = 0; i < index.Length; i++)
            {
                original[permutation[i]] = index[i];
            }

            Check(permuted[index] == source[original], context, $"{name}([{string.Join(", ", permutation)}]) element [{string.Join(", ", index)}] is {permuted[index]}, expected {source[original]}");
        });
    }

    private static void ForEachIndex(ReadOnlySpan<nint> lengths, Action<nint[]> action)
    {
        foreach (nint length in lengths)
        {
            if (length == 0)
            {
                return;
            }
        }

        nint[] index = new nint[lengths.Length];
        do
        {
            action(index);
        }
        while (Increment(index, lengths));
    }

    private static bool PairwiseIsReliable(ReadOnlyTensorSpan<int> x, ReadOnlyTensorSpan<int> y)
    {
        // Reliable on main only when the operand with the larger FlattenedLength already has the broadcast shape (and at least
        // the other operand's rank), and the result is not empty.
        ReadOnlyTensorSpan<int> larger = x.FlattenedLength > y.FlattenedLength ? x : y;
        ReadOnlyTensorSpan<int> other = x.FlattenedLength > y.FlattenedLength ? y : x;
        if (larger.IsEmpty || other.IsEmpty || larger.Rank < other.Rank)
        {
            return false;
        }

        for (int i = 0; i < other.Rank; i++)
        {
            nint l = larger.Lengths[larger.Rank - other.Rank + i], o = other.Lengths[i];
            if (l != o && !(o == 1))
            {
                return false;
            }
        }

        return true;
    }

    private static nint[] ReshapeTarget(ReadOnlyTensorSpan<int> tensor, int selector)
    {
        // A different factorization of the same flattened length, possibly with a -1 placeholder.
        nint n = tensor.FlattenedLength;
        List<nint> dims = [];
        nint rest = n;
        for (nint f = 2; f <= rest && dims.Count < 3; f++)
        {
            while (rest % f == 0 && dims.Count < 3 && ((selector >> dims.Count) & 1) != 0)
            {
                dims.Add(f);
                rest /= f;
            }
        }

        dims.Add(rest);
        if ((selector & 0x40) != 0 && dims.Count > 1)
        {
            dims[0] = -1;
        }

        return [.. dims];
    }

    private static void CheckReshape(Operand source, ReadOnlyTensorSpan<int> reshaped, string context)
    {
        // A reshape/squeeze/unsqueeze view must keep the flattened element sequence and stay inside the storage.
        Check(reshaped.FlattenedLength == source.View.FlattenedLength, context, $"view has FlattenedLength {reshaped.FlattenedLength}");
        int[] expected = source.Flatten();
        int[] actual = new int[reshaped.FlattenedLength];
        reshaped.FlattenTo(actual);
        Check(actual.AsSpan().SequenceEqual(expected), context, $"view flattens to [{string.Join(", ", actual)}], expected [{string.Join(", ", expected)}] (lengths [{string.Join(", ", reshaped.Lengths.ToArray())}], strides [{string.Join(", ", reshaped.Strides.ToArray())}])");
        Operand.CheckViewOver(reshaped, source.Storage, context + " [reshaped]");
    }

    private static int Fold(Operand x, int seed, Func<int, int, int> op)
    {
        int result = seed;
        foreach (int value in x.Flatten())
        {
            result = op(result, value);
        }

        return result;
    }

    private static void CheckBinary(Operand x, Operand y, Operand d, Func<int, int, int> op, string context)
    {
        if (d.HasAliasing)
        {
            return; // Several destination elements share storage, so the stored value depends on iteration order.
        }

        ReadOnlyTensorSpan<int> ds = d.View;
        nint[] index = new nint[ds.Rank];
        for (nint k = 0; k < ds.FlattenedLength; k++)
        {
            int expected = op(x.At(index), y.At(index));
            int actual = ds[index];
            Check(expected == actual, context, $"destination[{string.Join(", ", index)}] is {actual}, expected {expected}");
            Increment(index, ds.Lengths);
        }
    }

    private static void CheckPairwise(Operand x, Operand y, bool all, bool any, Func<int, int, bool> predicate, string name, string context)
    {
        // Broadcast x and y against each other (bidirectionally).
        int rank = Math.Max(x.View.Rank, y.View.Rank);
        nint[] lengths = new nint[rank];
        for (int i = 0; i < rank; i++)
        {
            nint a = Operand.LengthAt(x.View.Lengths, i - rank + x.View.Rank), b = Operand.LengthAt(y.View.Lengths, i - rank + y.View.Rank);
            lengths[i] = a == 1 ? b : a;
        }

        nint count = 1;
        foreach (nint length in lengths)
        {
            count *= length;
        }

        bool expectedAll = count > 0, expectedAny = false;
        nint[] index = new nint[rank];
        for (nint k = 0; k < count; k++)
        {
            bool match = predicate(x.At(index), y.At(index));
            expectedAll &= match;
            expectedAny |= match;
            Increment(index, lengths);
        }

        Check(all == expectedAll, context, $"{name}All is {all}, expected {expectedAll}");
        Check(any == expectedAny, context, $"{name}Any is {any}, expected {expectedAny}");
    }

    internal static bool Increment(Span<nint> index, ReadOnlySpan<nint> lengths)
    {
        for (int i = index.Length - 1; i >= 0; i--)
        {
            if (++index[i] < lengths[i])
            {
                return true;
            }

            index[i] = 0;
        }

        return false;
    }

    internal static void Check(bool condition, string context, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException($"{message}. {context}");
        }
    }

    /// <summary>
    /// A tensor view over guard-paged storage. Ref structs can't be stored, so the operand keeps its recipe and replays
    /// the same public view APIs (constructor, Unsqueeze/SqueezeDimension, Slice) whenever the view is needed.
    /// </summary>
    private sealed class Operand : IDisposable
    {
        private readonly PooledBoundedMemory<int> _memory;
        private nint[] _parentLengths = [], _parentStrides = [];
        private int _storageOffset;
        private int _unsqueezeDimension = -1;
        private NRange[]? _ranges;

        private Operand(PooledBoundedMemory<int> memory) => _memory = memory;

        public bool Failed { get; private set; }
        public Span<int> Storage => _memory.Span;

        public TensorSpan<int> View
        {
            get
            {
                TensorSpan<int> view = new TensorSpan<int>(Storage.Slice(_storageOffset), _parentLengths, _parentStrides);
                if (_unsqueezeDimension >= 0)
                {
                    view = view.Unsqueeze(_unsqueezeDimension).SqueezeDimension(_unsqueezeDimension);
                }

                if (_ranges is not null)
                {
                    view = view.Slice(_ranges);
                }

                return view;
            }
        }

        public bool HasAliasing
        {
            get
            {
                ReadOnlyTensorSpan<int> view = View;
                for (int i = 0; i < view.Rank; i++)
                {
                    if (view.Strides[i] == 0 && view.Lengths[i] > 1)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public static nint LengthAt(ReadOnlySpan<nint> lengths, int i) => i < 0 ? 1 : lengths[i];

        public static Operand Create(ref Reader reader, nint[] lengths, PoisonPagePlacement placement, int seed, bool allowZeroStrides)
        {
            int rank = lengths.Length;
            byte layout = reader.Byte();
            byte how = reader.Byte();

            // Optionally embed the view in a larger parent that is sliced back down to the requested lengths.
            bool slice = (how & 4) != 0 && rank > 0;
            nint[] parentLengths = new nint[rank];
            nint[] starts = new nint[rank];
            for (int i = 0; i < rank; i++)
            {
                starts[i] = slice && lengths[i] != 0 ? reader.Byte() % 3 : 0;
                parentLengths[i] = lengths[i] + starts[i];
            }

            // Physical layout: a permutation of the dimensions, padding between them, and (for sources) zero strides.
            int[] order = Enumerable.Range(0, rank).ToArray();
            if ((layout & 1) != 0)
            {
                for (int i = rank - 1; i > 0; i--)
                {
                    int j = reader.Byte() % (i + 1);
                    (order[i], order[j]) = (order[j], order[i]);
                }
            }

            nint[] strides = new nint[rank];
            nint next = 1;
            for (int k = rank - 1; k >= 0; k--)
            {
                int dim = order[k];
                bool zero = allowZeroStrides && (layout & 2) != 0 && (reader.Byte() & 3) == 0;
                if (parentLengths[dim] <= 1 || zero)
                {
                    strides[dim] = 0;
                    continue;
                }

                strides[dim] = next;
                next *= parentLengths[dim] + ((layout & 4) != 0 ? reader.Byte() % 3 : 0);
            }

            int storageOffset = (how & 1) != 0 ? reader.Byte() % 5 : 0;
            int storageLength = (int)Math.Min(next + storageOffset + reader.Byte() % 4, 4096);

            var operand = new Operand(PooledBoundedMemory<int>.Rent(storageLength, placement))
            {
                _parentLengths = parentLengths,
                _parentStrides = strides,
                _storageOffset = storageOffset,
            };

            Span<int> storage = operand.Storage;
            for (int i = 0; i < storage.Length; i++)
            {
                storage[i] = seed + i * 7 - 50;
            }

            if ((how & 2) != 0 && rank > 0)
            {
                operand._unsqueezeDimension = reader.Byte() % (rank + 1);
            }

            if (slice)
            {
                operand._ranges = new NRange[rank];
                for (int i = 0; i < rank; i++)
                {
                    operand._ranges[i] = new NRange(starts[i], starts[i] + lengths[i]);
                }
            }

            try
            {
                ReadOnlyTensorSpan<int> view = operand.View;
                Check(view.Lengths.SequenceEqual(lengths), operand.ToString(), "view does not have the requested lengths");
            }
            catch (ArgumentException)
            {
                operand.Failed = true;
            }

            return operand;
        }

        /// <summary>A strided <see cref="Tensor{T}"/> over a copy of the storage, with the same layout as the view.</summary>
        public Tensor<int> ToTensor()
        {
            TensorSpan<int> view = View;
            nint first = view.IsEmpty ? 0 : Unsafe.ByteOffset(ref MemoryMarshal.GetReference(Storage), ref view.GetPinnableReference()) / sizeof(int);
            return Tensor.Create(Storage.ToArray(), (int)first, view.Lengths, view.Strides);
        }

        public int At(ReadOnlySpan<nint> resultIndex)
        {
            // Right-aligned broadcasting: missing leading dimensions and length-1 dimensions read index 0.
            ReadOnlyTensorSpan<int> view = View;
            nint[] index = new nint[view.Rank];
            for (int i = 0; i < index.Length; i++)
            {
                nint r = resultIndex[resultIndex.Length - view.Rank + i];
                index[i] = view.Lengths[i] == 1 ? 0 : r;
            }

            return view[index];
        }

        public int[] Flatten()
        {
            ReadOnlyTensorSpan<int> view = View;
            int[] result = new int[view.FlattenedLength];
            nint[] index = new nint[view.Rank];
            for (int k = 0; k < result.Length; k++)
            {
                result[k] = view[index];
                Increment(index, view.Lengths);
            }

            return result;
        }

        public void CheckView(string context) => CheckViewOver(View, Storage, context);

        public static void CheckViewOver(ReadOnlyTensorSpan<int> view, ReadOnlySpan<int> storage, string context)
        {
            // Invariants the operation kernels rely on: length <= 1 implies stride 0, and every element is inside the storage.
            for (int i = 0; i < view.Rank; i++)
            {
                Check(view.Lengths[i] > 1 || view.Strides[i] == 0, context, $"dimension {i} has length {view.Lengths[i]} but stride {view.Strides[i]}");
            }

            if (view.IsEmpty)
            {
                return;
            }

            nint[] index = new nint[view.Rank];
            ref int start = ref MemoryMarshal.GetReference(storage);
            do
            {
                nint offset = Unsafe.ByteOffset(ref start, ref Unsafe.AsRef(in view[index])) / sizeof(int);
                Check(offset >= 0 && offset < storage.Length, context, $"element [{string.Join(", ", index)}] is at offset {offset}, outside storage of length {storage.Length}");
            }
            while (Increment(index, view.Lengths));
        }

        public void FillCanaries()
        {
            // Everything in the destination storage that the view can't address becomes a canary.
            bool[] reachable = Reachable();
            Span<int> storage = Storage;
            for (int i = 0; i < storage.Length; i++)
            {
                if (!reachable[i])
                {
                    storage[i] = Canary;
                }
            }
        }

        public void CheckCanaries(string context)
        {
            bool[] reachable = Reachable();
            Span<int> storage = Storage;
            for (int i = 0; i < storage.Length; i++)
            {
                Check(reachable[i] || storage[i] == Canary, context, $"destination storage offset {i} outside the view was overwritten with {storage[i]}");
            }
        }

        private bool[] Reachable()
        {
            Span<int> storage = Storage;
            bool[] reachable = new bool[storage.Length];
            TensorSpan<int> view = View;
            if (!view.IsEmpty)
            {
                nint[] index = new nint[view.Rank];
                do
                {
                    reachable[Unsafe.ByteOffset(ref MemoryMarshal.GetReference(storage), ref view[index]) / sizeof(int)] = true;
                }
                while (Increment(index, view.Lengths));
            }

            return reachable;
        }

        public override string ToString()
        {
            string recipe = $"ctor(storage[{_storageOffset}..{Storage.Length}], [{string.Join(", ", _parentLengths)}], [{string.Join(", ", _parentStrides)}])";
            if (_unsqueezeDimension >= 0)
            {
                recipe += $".Unsqueeze({_unsqueezeDimension}).SqueezeDimension({_unsqueezeDimension})";
            }

            if (_ranges is not null)
            {
                recipe += $".Slice({string.Join(", ", _ranges.Select(r => $"{r.Start.Value}..{r.End.Value}"))})";
            }

            if (Failed)
            {
                return recipe;
            }

            ReadOnlyTensorSpan<int> view = View;
            return $"{{{recipe} -> lengths [{string.Join(", ", view.Lengths.ToArray())}] strides [{string.Join(", ", view.Strides.ToArray())}] dense {view.IsDense}}}";
        }

        public void Dispose() => _memory.Dispose();
    }

    private ref struct Reader(ReadOnlySpan<byte> bytes)
    {
        private ReadOnlySpan<byte> _bytes = bytes;

        public byte Byte()
        {
            if (_bytes.IsEmpty)
            {
                return 0;
            }

            byte b = _bytes[0];
            _bytes = _bytes.Slice(1);
            return b;
        }
    }
}
