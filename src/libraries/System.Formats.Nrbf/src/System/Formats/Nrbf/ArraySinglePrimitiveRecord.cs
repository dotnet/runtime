// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Formats.Nrbf.Utils;

namespace System.Formats.Nrbf;

/// <summary>
/// Represents a single-dimensional array of a primitive type.
/// </summary>
/// <remarks>
/// ArraySinglePrimitive records are described in <see href="https://learn.microsoft.com/openspecs/windows_protocols/ms-nrbf/3a50a305-5f32-48a1-a42a-c34054db310b">[MS-NRBF] 2.4.3.3</see>.
/// </remarks>
internal sealed class ArraySinglePrimitiveRecord<T> : SZArrayRecord<T>
    where T : unmanaged
{
    internal ArraySinglePrimitiveRecord(ArrayInfo arrayInfo, IReadOnlyList<T> values) : base(arrayInfo)
    {
        Values = values;
        ValuesToRead = 0; // there is nothing to read anymore
    }

    public override SerializationRecordType RecordType => SerializationRecordType.ArraySinglePrimitive;

    /// <inheritdoc />
    public override TypeName TypeName => TypeNameHelpers.GetPrimitiveSZArrayTypeName(TypeNameHelpers.GetPrimitiveType<T>());

    internal IReadOnlyList<T> Values { get; }

    /// <inheritdoc/>
    public override T[] GetArray(bool allowNulls = true)
        => (T[])(_arrayNullsNotAllowed ??= (Values is T[] array ? array : Values.ToArray()));

    internal override (AllowedRecordTypes allowed, PrimitiveType primitiveType) GetAllowedRecordType() => throw new InvalidOperationException();

    private protected override void AddValue(object value) => throw new InvalidOperationException();

    internal static IReadOnlyList<T> DecodePrimitiveTypes(BinaryReader reader, int count)
    {
        // For decimals, the input is provided as strings, so we can't compute the required size up-front.
        if (typeof(T) == typeof(decimal))
        {
            return (List<T>)(object)DecodeDecimals(reader, count);
        }

        // char[] has a unique representation in NRBF streams. Typical strings are transcoded
        // to UTF-8 and prefixed with the number of bytes in the UTF-8 representation. char[]
        // is also serialized as UTF-8, but it is instead prefixed with the number of chars
        // in the UTF-16 representation, not the number of bytes in the UTF-8 representation.
        // This number doesn't directly precede the UTF-8 contents in the NRBF stream; it's
        // instead contained within the ArrayInfo structure (passed to this method as the
        // 'count' argument).
        //
        // The practical consequence of this is that we don't actually know how many UTF-8
        // bytes we need to consume in order to ensure we've read 'count' chars. We know that
        // an n-length UTF-16 string turns into somewhere between [n .. 3n] UTF-8 bytes.
        // The best we can do is that when reading an n-element char[], we'll ensure that
        // there are at least n bytes remaining in the input stream. We'll still need to
        // account for that even with this check, we might hit EOF before fully populating
        // the char[]. But from a safety perspective, it does appropriately limit our
        // allocations to be proportional to the amount of data present in the input stream,
        // which is a sufficient defense against DoS.

        long requiredBytes = count;
        if (typeof(T) == typeof(DateTime) || typeof(T) == typeof(TimeSpan))
        {
            // We can't assume DateTime as represented by the runtime is 8 bytes.
            // The only assumption we can make is that it's 8 bytes on the wire.
            requiredBytes *= 8;
        }
        else if (typeof(T) != typeof(char))
        {
            requiredBytes *= Unsafe.SizeOf<T>();
        }

        bool? isDataAvailable = reader.IsDataAvailable(requiredBytes);
        if (!isDataAvailable.HasValue)
        {
            return DecodeFromNonSeekableStream(reader, count);
        }

        if (!isDataAvailable.Value)
        {
            // We are sure there is not enough data.
            ThrowHelper.ThrowEndOfStreamException();
        }

        if (typeof(T) == typeof(byte))
        {
            return (T[])(object)reader.ReadBytes(count);
        }
        else if (typeof(T) == typeof(char))
        {
            return (T[])(object)reader.ParseChars(count);
        }
        else if (typeof(T) == typeof(TimeSpan) || typeof(T) == typeof(DateTime))
        {
            return DecodeTime(reader, count);
        }

        // It's safe to pre-allocate, as we have ensured there is enough bytes in the stream.
        T[] result = new T[count];
        Span<byte> resultAsBytes = MemoryMarshal.AsBytes<T>(result);
#if NET
        reader.BaseStream.ReadExactly(resultAsBytes);
#else
        byte[] bytes = ArrayPool<byte>.Shared.Rent((int)Math.Min(requiredBytes, 256_000));

        while (!resultAsBytes.IsEmpty)
        {
            int bytesRead = reader.Read(bytes, 0, Math.Min(resultAsBytes.Length, bytes.Length));
            if (bytesRead <= 0)
            {
                ArrayPool<byte>.Shared.Return(bytes);
                ThrowHelper.ThrowEndOfStreamException();
            }

            bytes.AsSpan(0, bytesRead).CopyTo(resultAsBytes);
            resultAsBytes = resultAsBytes.Slice(bytesRead);
        }

        ArrayPool<byte>.Shared.Return(bytes);
#endif

        if (!BitConverter.IsLittleEndian)
        {
            if (typeof(T) == typeof(short) || typeof(T) == typeof(ushort))
            {
                Span<short> span = MemoryMarshal.Cast<T, short>(result);
#if NET
                BinaryPrimitives.ReverseEndianness(span, span);
#else
                for (int i = 0; i < span.Length; i++)
                {
                    span[i] = BinaryPrimitives.ReverseEndianness(span[i]);
                }
#endif
            }
            else if (typeof(T) == typeof(int) || typeof(T) == typeof(uint) || typeof(T) == typeof(float))
            {
                Span<int> span = MemoryMarshal.Cast<T, int>(result);
#if NET
                BinaryPrimitives.ReverseEndianness(span, span);
#else
                for (int i = 0; i < span.Length; i++)
                {
                    span[i] = BinaryPrimitives.ReverseEndianness(span[i]);
                }
#endif
            }
            else if (typeof(T) == typeof(long) || typeof(T) == typeof(ulong) || typeof(T) == typeof(double))
            {
                Span<long> span = MemoryMarshal.Cast<T, long>(result);
#if NET
                BinaryPrimitives.ReverseEndianness(span, span);
#else
                for (int i = 0; i < span.Length; i++)
                {
                    span[i] = BinaryPrimitives.ReverseEndianness(span[i]);
                }
#endif
            }
        }

        if (typeof(T) == typeof(bool))
        {
            // See DontCastBytesToBooleans test to see what could go wrong.
            bool[] booleans = (bool[])(object)result;
            resultAsBytes = MemoryMarshal.AsBytes<T>(result);
            for (int i = 0; i < booleans.Length; i++)
            {
                // We don't use the bool array to get the value, as an optimizing compiler or JIT could elide this.
                if (resultAsBytes[i] != 0) // it can be any byte different than 0
                {
                    booleans[i] = true; // set it to 1 in explicit way
                }
            }
        }

        return result;
    }

    private static List<decimal> DecodeDecimals(BinaryReader reader, int count)
    {
        List<decimal> values = new();
        for (int i = 0; i < count; i++)
        {
            values.Add(reader.ParseDecimal());
        }
        return values;
    }

    private static T[] DecodeTime(BinaryReader reader, int count)
    {
        T[] values = new T[count];
        for (int i = 0; i < values.Length; i++)
        {
            if (typeof(T) == typeof(DateTime))
            {
                values[i] = (T)(object)Utils.BinaryReaderExtensions.CreateDateTimeFromData(reader.ReadUInt64());
            }
            else if (typeof(T) == typeof(TimeSpan))
            {
                values[i] = (T)(object)new TimeSpan(reader.ReadInt64());
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        return values;
    }

    private static List<T> DecodeFromNonSeekableStream(BinaryReader reader, int count)
    {
        // The count arg could originate from untrusted input, so we shouldn't
        // pass it as-is to the ctor's capacity arg. We'll instead rely on
        // List<T>.Add's O(1) amortization to keep the entire loop O(count).

        List<T> values = new List<T>(Math.Min(count, 4));
        for (int i = 0; i < count; i++)
        {
            if (typeof(T) == typeof(byte))
            {
                values.Add((T)(object)reader.ReadByte());
            }
            else if (typeof(T) == typeof(bool))
            {
                values.Add((T)(object)reader.ReadBoolean());
            }
            else if (typeof(T) == typeof(sbyte))
            {
                values.Add((T)(object)reader.ReadSByte());
            }
            else if (typeof(T) == typeof(char))
            {
                values.Add((T)(object)reader.ParseChar());
            }
            else if (typeof(T) == typeof(short))
            {
                values.Add((T)(object)reader.ReadInt16());
            }
            else if (typeof(T) == typeof(ushort))
            {
                values.Add((T)(object)reader.ReadUInt16());
            }
            else if (typeof(T) == typeof(int))
            {
                values.Add((T)(object)reader.ReadInt32());
            }
            else if (typeof(T) == typeof(uint))
            {
                values.Add((T)(object)reader.ReadUInt32());
            }
            else if (typeof(T) == typeof(long))
            {
                values.Add((T)(object)reader.ReadInt64());
            }
            else if (typeof(T) == typeof(ulong))
            {
                values.Add((T)(object)reader.ReadUInt64());
            }
            else if (typeof(T) == typeof(float))
            {
                values.Add((T)(object)reader.ReadSingle());
            }
            else if (typeof(T) == typeof(double))
            {
                values.Add((T)(object)reader.ReadDouble());
            }
            else if (typeof(T) == typeof(DateTime))
            {
                values.Add((T)(object)Utils.BinaryReaderExtensions.CreateDateTimeFromData(reader.ReadUInt64()));
            }
            else if (typeof(T) == typeof(TimeSpan))
            {
                values.Add((T)(object)new TimeSpan(reader.ReadInt64()));
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        return values;
    }
}
