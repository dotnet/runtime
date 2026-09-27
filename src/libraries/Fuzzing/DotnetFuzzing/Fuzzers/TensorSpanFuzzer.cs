// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Numerics;
using System.Numerics.Tensors;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DotnetFuzzing.Fuzzers;

/// <summary>
/// Fuzzes the public tensor constructors that take a primitive array, span or pointer plus lengths/strides/start, and checks
/// that whatever shape is accepted only ever addresses elements inside the backing storage: through the indexer, the enumerator,
/// FlattenTo, Slice, GetSpan/TryGetSpan and ToString. Span and pointer storage lives in guard-paged memory, so an escaped read faults.
/// </summary>
/// <remarks>
/// Input layout: [0] constructor, [1] element type, [2] storage length, [3] rank, then fuzzed integers (start, per-dimension
/// lengths and strides, slice start indexes, GetSpan length). Each integer is one byte: a small value, a small negative value,
/// or an entry of a table of boundary values (int/nint limits, powers of two) chosen to provoke overflow in the shape validation.
/// </remarks>
internal sealed class TensorSpanFuzzer : IFuzzer
{
    private const int MaxVisitedElements = 2048;

    // Known contract issues on main, tolerated unless TENSOR_FUZZ_STRICT=1:
    // * Constructors throw OverflowException (from checked arithmetic in TensorShape) for very large lengths/strides,
    //   although only ArgumentOutOfRangeException is documented.
    // * Slice(params ReadOnlySpan<nint>) throws IndexOutOfRangeException for an out-of-range start index, although
    //   ArgumentOutOfRangeException is documented.
    private static readonly bool s_strict = Environment.GetEnvironmentVariable("TENSOR_FUZZ_STRICT") == "1";

    public string[] TargetAssemblies { get; } = ["System.Numerics.Tensors"];
    public string[] TargetCoreLibPrefixes => [];

    private static readonly long[] s_boundaryValues =
    [
        0, 1, -1, 2, int.MaxValue, int.MinValue, (long)int.MaxValue + 1, uint.MaxValue,
        long.MaxValue, long.MinValue, long.MaxValue - 1, long.MaxValue / 2, long.MaxValue / 2 + 1, long.MaxValue / 4 + 1,
        1L << 31, 1L << 32, 1L << 33, 1L << 61, 1L << 62, (1L << 62) + 1, 255, 256, 65535, 65536,
        int.MaxValue / 2 + 1, int.MaxValue / 4 + 1, int.MaxValue / 8 + 1, long.MaxValue / 8 + 1, 3, 1000, -2, -1000,
    ];

    public void FuzzTarget(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < 4)
        {
            return;
        }

        switch (bytes[1] % 3)
        {
            case 0: Run<byte>(bytes); break;
            case 1: Run<int>(bytes); break;
            default: Run<long>(bytes); break;
        }
    }

    private static unsafe void Run<T>(ReadOnlySpan<byte> bytes)
        where T : unmanaged, INumberBase<T>
    {
        int variant = bytes[0] % 9;
        int dataLength = bytes[2];
        int rank = bytes[3] % 7;
        var reader = new Reader(bytes.Slice(4));

        nint start = reader.Next();
        nint[] lengths = new nint[rank];
        nint[] strides = new nint[rank];
        for (int i = 0; i < rank; i++)
        {
            lengths[i] = reader.Next();
            strides[i] = reader.Next();
        }

        nint[] sliceStart = new nint[rank];
        for (int i = 0; i < rank; i++)
        {
            sliceStart[i] = reader.Next();
        }

        nint getSpanLength = reader.Next();

        T[] array = new T[dataLength];
        for (int i = 0; i < array.Length; i++)
        {
            array[i] = T.CreateTruncating(i * 7 + 1);
        }

        using var bounded = PooledBoundedMemory<T>.Rent(array, (bytes[0] & 0x80) != 0 ? PoisonPagePlacement.Before : PoisonPagePlacement.After);
        Span<T> span = bounded.Span;

        // Multi-dimensional storage for the System.Array constructor: a [dataLength / 4, 4] array, when that is non-empty.
        Array? mdArray = null;
        if (dataLength >= 4)
        {
            mdArray = Array.CreateInstance(typeof(T), dataLength / 4, 4);
            array.AsSpan(0, dataLength / 4 * 4).CopyTo(MemoryMarshal.CreateSpan(ref Unsafe.As<byte, T>(ref MemoryMarshal.GetArrayDataReference(mdArray)), dataLength / 4 * 4));
        }

        ReadOnlyTensorSpan<T> tensor;
        ref T storage = ref MemoryMarshal.GetArrayDataReference(array);
        nint storageLength = dataLength;
        nint firstOffset = 0;
        string ctor;

        try
        {
            switch (variant)
            {
                case 0:
                    ctor = "ReadOnlyTensorSpan(T[], int, lengths, strides)";
                    tensor = new ReadOnlyTensorSpan<T>(array, (int)start, lengths, strides);
                    firstOffset = (int)start;
                    break;
                case 1:
                    ctor = "ReadOnlyTensorSpan(T[], lengths, strides)";
                    tensor = new ReadOnlyTensorSpan<T>(array, lengths, strides);
                    break;
                case 2:
                    ctor = "ReadOnlyTensorSpan(T[], lengths)";
                    tensor = new ReadOnlyTensorSpan<T>(array, lengths);
                    break;
                case 3:
                    ctor = "ReadOnlyTensorSpan(ReadOnlySpan<T>, lengths, strides)";
                    tensor = new ReadOnlyTensorSpan<T>((ReadOnlySpan<T>)span, lengths, strides);
                    storage = ref MemoryMarshal.GetReference(span);
                    break;
                case 4:
                    ctor = "ReadOnlyTensorSpan(T*, nint, lengths, strides)";
                    storage = ref MemoryMarshal.GetReference(span);
                    tensor = new ReadOnlyTensorSpan<T>((T*)Unsafe.AsPointer(ref storage), span.Length, lengths, strides);
                    break;
                case 5:
                    ctor = "TensorSpan(T[], int, lengths, strides)";
                    tensor = new TensorSpan<T>(array, (int)start, lengths, strides);
                    firstOffset = (int)start;
                    break;
                case 6:
                    ctor = "TensorSpan(Span<T>, lengths, strides)";
                    tensor = new TensorSpan<T>(span, lengths, strides);
                    storage = ref MemoryMarshal.GetReference(span);
                    break;
                case 7:
                    ctor = "Tensor.Create(T[], int, lengths, strides)";
                    tensor = Tensor.Create(array, (int)start, lengths, strides).AsReadOnlyTensorSpan();
                    firstOffset = (int)start;
                    break;
                default:
                    ctor = "ReadOnlyTensorSpan(Array, int[], lengths, strides)";
                    if (mdArray is null)
                    {
                        return;
                    }

                    int[] mdStart = [(int)start, (int)sliceStart.ElementAtOrDefault(0)];
                    tensor = new ReadOnlyTensorSpan<T>(mdArray, mdStart, lengths, strides);
                    storage = ref Unsafe.As<byte, T>(ref MemoryMarshal.GetArrayDataReference(mdArray));
                    storageLength = mdArray.Length;
                    firstOffset = -1; // Derived from the tensor itself below.
                    break;
            }
        }
        catch (ArgumentException)
        {
            return; // Rejected: fine.
        }
        catch (OverflowException) when (!s_strict)
        {
            return;
        }

        string context = $"{ctor}<{typeof(T).Name}> storage={storageLength} start={start} lengths=[{string.Join(", ", lengths)}] strides=[{string.Join(", ", strides)}] -> lengths=[{string.Join(", ", tensor.Lengths.ToArray())}] strides=[{string.Join(", ", tensor.Strides.ToArray())}]";

        if (firstOffset < 0)
        {
            firstOffset = tensor.IsEmpty ? 0 : OffsetOf(ref storage, in tensor[new nint[tensor.Rank]]);
        }

        Verify(tensor, ref storage, storageLength, firstOffset, context);

        // Slicing with fuzzed start indexes must either be rejected or produce a view of the same storage.
        if (tensor.Rank == rank && rank > 0)
        {
            ReadOnlyTensorSpan<T> slice;
            try
            {
                slice = tensor.Slice(sliceStart);
            }
            catch (Exception ex) when (ex is ArgumentException || (!s_strict && ex is IndexOutOfRangeException))
            {
                slice = default;
                rank = -1;
            }

            if (rank > 0)
            {
                for (int i = 0; i < rank; i++)
                {
                    Check(sliceStart[i] >= 0 && sliceStart[i] <= tensor.Lengths[i], context, $"Slice accepted start index {sliceStart[i]} for dimension {i}");
                }

                nint sliceOffset = firstOffset;
                for (int i = 0; i < rank; i++)
                {
                    sliceOffset += sliceStart[i] * tensor.Strides[i];
                }

                Verify(slice, ref storage, storageLength, sliceOffset, context + $" .Slice([{string.Join(", ", sliceStart)}])");
            }
        }

        // GetSpan/TryGetSpan hand out a contiguous span; it must stay inside the storage.
        if (tensor.Rank > 0 && !tensor.IsEmpty)
        {
            nint[] zero = new nint[tensor.Rank];
            if (tensor.TryGetSpan(zero, (int)getSpanLength, out ReadOnlySpan<T> contiguous) && !contiguous.IsEmpty)
            {
                nint first = OffsetOf(ref storage, in contiguous[0]);
                Check(first == firstOffset, context, $"TryGetSpan returned a span starting at offset {first}, expected {firstOffset}");
                Check(first >= 0 && first + contiguous.Length <= storageLength, context, $"TryGetSpan(length {getSpanLength}) returned [{first}, {first + contiguous.Length}) outside the storage");
            }
        }
    }

    private static void Verify<T>(ReadOnlyTensorSpan<T> tensor, ref T storage, nint storageLength, nint firstOffset, string context)
        where T : unmanaged, INumberBase<T>
    {
        ReadOnlySpan<nint> lengths = tensor.Lengths;
        ReadOnlySpan<nint> strides = tensor.Strides;
        Check(lengths.Length == tensor.Rank && strides.Length == tensor.Rank, context, "Lengths/Strides do not match Rank");

        Int128 flattened = 1;
        foreach (nint length in lengths)
        {
            Check(length >= 0, context, $"negative length {length}");
            flattened *= length;
        }

        Check(tensor.Rank == 0 ? tensor.FlattenedLength == 0 : flattened == tensor.FlattenedLength, context, $"FlattenedLength {tensor.FlattenedLength} != product of lengths {flattened}");
        Check(tensor.IsEmpty == (tensor.FlattenedLength == 0), context, "IsEmpty disagrees with FlattenedLength");

        if (tensor.IsEmpty)
        {
            return;
        }

        // The extreme element offsets reachable through this shape must lie inside the storage.
        Int128 minOffset = firstOffset, maxOffset = firstOffset;
        for (int i = 0; i < lengths.Length; i++)
        {
            Int128 extent = (Int128)(lengths[i] - 1) * strides[i];
            if (extent < 0)
            {
                minOffset += extent;
            }
            else
            {
                maxOffset += extent;
            }
        }

        Check(minOffset >= 0 && maxOffset < storageLength, context, $"shape can address offsets [{minOffset}, {maxOffset}] outside storage of length {storageLength}");

        // Walk the first elements in row-major order through the indexer and the enumerator, plus the last element.
        nint[] index = new nint[tensor.Rank];
        ReadOnlyTensorSpan<T>.Enumerator enumerator = tensor.GetEnumerator();
        nint visited = 0;
        while (true)
        {
            nint expected = ExpectedOffset(firstOffset, index, strides);
            nint viaIndexer = OffsetOf(ref storage, in tensor[index]);
            Check(viaIndexer == expected, context, $"indexer [{string.Join(", ", index)}] reached offset {viaIndexer}, expected {expected}");

            Check(enumerator.MoveNext(), context, $"enumerator ended after {visited} elements");
            nint viaEnumerator = OffsetOf(ref storage, in enumerator.Current);
            Check(viaEnumerator == expected, context, $"enumerator element {visited} reached offset {viaEnumerator}, expected {expected}");

            if (++visited >= MaxVisitedElements || !Increment(index, lengths))
            {
                break;
            }
        }

        if (visited == tensor.FlattenedLength)
        {
            Check(!enumerator.MoveNext(), context, "enumerator yielded more than FlattenedLength elements");
        }

        for (int i = 0; i < index.Length; i++)
        {
            index[i] = lengths[i] - 1;
        }

        nint last = OffsetOf(ref storage, in tensor[index]);
        Check(last == ExpectedOffset(firstOffset, index, strides), context, $"last element reached offset {last}");

        // FlattenTo must copy exactly the elements the indexer sees.
        if (tensor.FlattenedLength <= 1 << 16)
        {
            T[] flattenedCopy = new T[tensor.FlattenedLength];
            tensor.FlattenTo(flattenedCopy);

            Array.Clear(index);
            for (int k = 0; k < Math.Min(flattenedCopy.Length, MaxVisitedElements); k++)
            {
                T expected = Unsafe.Add(ref storage, ExpectedOffset(firstOffset, index, strides));
                Check(EqualityComparer<T>.Default.Equals(expected, flattenedCopy[k]), context, $"FlattenTo element {k} is {flattenedCopy[k]}, expected {expected}");
                Increment(index, lengths);
            }
        }

        nint[] maximumLengths = new nint[tensor.Rank];
        maximumLengths.AsSpan().Fill(3);
        _ = tensor.ToString(maximumLengths);
    }

    private static nint ExpectedOffset(nint firstOffset, ReadOnlySpan<nint> index, ReadOnlySpan<nint> strides)
    {
        nint offset = firstOffset;
        for (int i = 0; i < index.Length; i++)
        {
            offset += index[i] * strides[i];
        }

        return offset;
    }

    private static bool Increment(Span<nint> index, ReadOnlySpan<nint> lengths)
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

    private static nint OffsetOf<T>(ref T storage, ref readonly T element) =>
        Unsafe.ByteOffset(ref storage, ref Unsafe.AsRef(in element)) / Unsafe.SizeOf<T>();

    private static void Check(bool condition, string context, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException($"{message}. {context}");
        }
    }

    private ref struct Reader(ReadOnlySpan<byte> bytes)
    {
        private ReadOnlySpan<byte> _bytes = bytes;

        public nint Next()
        {
            if (_bytes.IsEmpty)
            {
                return 0;
            }

            byte b = _bytes[0];
            _bytes = _bytes.Slice(1);

            return b switch
            {
                < 0xA0 => b % 24,                  // small non-negative
                < 0xC0 => -(b - 0x9F),              // small negative
                _ => (nint)s_boundaryValues[(b - 0xC0) % s_boundaryValues.Length],
            };
        }
    }
}
