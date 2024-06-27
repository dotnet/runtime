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
    private static TypeName? s_typeName;

    internal ArraySinglePrimitiveRecord(ArrayInfo arrayInfo, IReadOnlyList<T> values) : base(arrayInfo)
    {
        Values = values;
        ValuesToRead = 0; // there is nothing to read anymore
    }

    public override SerializationRecordType RecordType => SerializationRecordType.ArraySinglePrimitive;

    /// <inheritdoc />
    public override TypeName TypeName
        => s_typeName ??= TypeName.Parse((typeof(T[]).FullName + "," + TypeNameExtensions.CoreLibAssemblyName).AsSpan());

    internal IReadOnlyList<T> Values { get; }

    /// <inheritdoc/>
    public override T[] GetArray(bool allowNulls = true)
        => (T[])(_arrayNullsNotAllowed ??= (Values is T[] array ? array : Values.ToArray()));

    internal override (AllowedRecordTypes allowed, PrimitiveType primitiveType) GetAllowedRecordType()
    {
        Debug.Fail("GetAllowedRecordType should never be called on ArraySinglePrimitiveRecord");
        throw new InvalidOperationException();
    }

    private protected override void AddValue(object value)
    {
        Debug.Fail("AddValue should never be called on ArraySinglePrimitiveRecord");
        throw new InvalidOperationException();
    }

    internal static IReadOnlyList<T> DecodePrimitiveTypes(BinaryReader reader, int count)
    {
        // For decimals, the input is provided as strings, so we can't compute the required size up-front.
        if (typeof(T) == typeof(decimal))
        {
            return (List<T>)(object)DecodeDecimals(reader, count);
        }

        long requiredBytes = count;
        if (typeof(T) != typeof(char)) // the input is UTF8
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
            return (T[])(object)reader.ReadChars(count);
        }

        // It's safe to pre-allocate, as we have ensured there is enough bytes in the stream.
        T[] result = new T[count];
        Span<byte> resultAsBytes = MemoryMarshal.AsBytes<T>(result);
#if NET
        reader.BaseStream.ReadExactly(resultAsBytes);
#else
        byte[] bytes = ArrayPool<byte>.Shared.Rent(Math.Min(count * Unsafe.SizeOf<T>(), 256_000));

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
            else if (typeof(T) == typeof(long) || typeof(T) == typeof(ulong) || typeof(T) == typeof(double)
                  || typeof(T) == typeof(DateTime) || typeof(T) == typeof(TimeSpan))
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

        return result;
    }

    private static List<decimal> DecodeDecimals(BinaryReader reader, int count)
    {
        List<decimal> values = new();
#if NET
        Span<byte> buffer = stackalloc byte[256];
        for (int i = 0; i < count; i++)
        {
            int stringLength = reader.Read7BitEncodedInt();
            if (!(stringLength > 0 && stringLength <= buffer.Length))
            {
                ThrowHelper.ThrowInvalidValue(stringLength);
            }

            reader.BaseStream.ReadExactly(buffer.Slice(0, stringLength));

            values.Add(decimal.Parse(buffer.Slice(0, stringLength), CultureInfo.InvariantCulture));
        }
#else
        for (int i = 0; i < count; i++)
        {
            values.Add(decimal.Parse(reader.ReadString(), CultureInfo.InvariantCulture));
        }
#endif
        return values;
    }

    private static List<T> DecodeFromNonSeekableStream(BinaryReader reader, int count)
    {
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
                values.Add((T)(object)reader.ReadChar());
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
                values.Add((T)(object)Utils.BinaryReaderExtensions.CreateDateTimeFromData(reader.ReadInt64()));
            }
            else
            {
                Debug.Assert(typeof(T) == typeof(TimeSpan));

                values.Add((T)(object)new TimeSpan(reader.ReadInt64()));
            }
        }

        return values;
    }
}
