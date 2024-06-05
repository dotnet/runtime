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
using System.Runtime.Serialization.BinaryFormat.Utils;

namespace System.Runtime.Serialization.BinaryFormat;

/// <summary>
/// Represents a single-dimensional array of a primitive type.
/// </summary>
/// <remarks>
/// ArraySinglePrimitive records are described in <see href="https://learn.microsoft.com/openspecs/windows_protocols/ms-nrbf/3a50a305-5f32-48a1-a42a-c34054db310b">[MS-NRBF] 2.4.3.3</see>.
/// </remarks>
internal sealed class ArraySinglePrimitiveRecord<T> : ArrayRecord<T>
    where T : unmanaged
{
    private static TypeName? s_elementTypeName;

    internal ArraySinglePrimitiveRecord(ArrayInfo arrayInfo, IReadOnlyList<T> values) : base(arrayInfo)
    {
        Values = values;
        ValuesToRead = 0; // there is nothing to read anymore
    }

    public override RecordType RecordType => RecordType.ArraySinglePrimitive;

    public override TypeName ElementTypeName
        => s_elementTypeName ??= TypeName.Parse(typeof(T).FullName.AsSpan()).WithAssemblyName(typeof(T).GetAssemblyNameIncludingTypeForwards());

    internal IReadOnlyList<T> Values { get; }

    public override bool IsTypeNameMatching(Type type) => typeof(T[]) == type;

    internal override bool IsElementType(Type typeElement) => typeElement == typeof(T);

    /// <inheritdoc/>
    public override T[] GetArray(bool allowNulls = true)
        => Values is T[] array ? array : Values.ToArray();

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
        if (typeof(T) == typeof(byte) && reader.IsDataAvailable(count))
        {
            T[] result = (T[])(object)reader.ReadBytes(count);
            // This might be less than the number of bytes requested if the end of the stream is reached.
            if (result.Length != count)
            {
                ThrowHelper.ThrowEndOfStreamException();
            }
            return result;
        }
        else if (typeof(T) == typeof(char) && reader.IsDataAvailable(count))
        {
            T[] result = (T[])(object)reader.ReadChars(count);
            if (result.Length != count)
            {
                ThrowHelper.ThrowEndOfStreamException();
            }
            return result;
        }

        // Most of the tests use MemoryStream or FileStream and they both allow for executing the fast path.
        // To ensure the slow path is tested as well, the fast path is executed only for optimized builds.
#if NETCOREAPP
        if (typeof(T) != typeof(decimal) && typeof(T) != typeof(DateTime) && typeof(T) != typeof(TimeSpan) // not optimized
            && reader.IsDataAvailable(count * Unsafe.SizeOf<T>()))
        {
            return DecodePrimitiveTypesToArray(reader, count);
        }
#endif
        return DecodePrimitiveTypesToList(reader, count);
    }

    private static List<T> DecodePrimitiveTypesToList(BinaryReader reader, int count)
    {
        List<T> values = [];
        for (int i = 0; i < count; i++)
        {
            if (typeof(T) == typeof(byte))
            {
                values.Add((T)(object)reader.ReadByte());
            }
            if (typeof(T) == typeof(bool))
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
            else if (typeof(T) == typeof(decimal))
            {
                values.Add((T)(object)decimal.Parse(reader.ReadString(), CultureInfo.InvariantCulture));
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

#if NETCOREAPP
    private static T[] DecodePrimitiveTypesToArray(BinaryReader reader, int count)
    {
        T[] result = new T[count];
        Span<byte> bytes = MemoryMarshal.AsBytes<T>(result);
        reader.BaseStream.ReadExactly(bytes);

        if (!BitConverter.IsLittleEndian)
        {
            if (typeof(T) == typeof(short))
            {
                Span<short> span = MemoryMarshal.Cast<T, short>(result);
                BinaryPrimitives.ReverseEndianness(span, span);
            }
            else if (typeof(T) == typeof(ushort))
            {
                Span<ushort> span = MemoryMarshal.Cast<T, ushort>(result);
                BinaryPrimitives.ReverseEndianness(span, span);
            }
            else if (typeof(T) == typeof(int) || typeof(T) == typeof(float))
            {
                Span<int> span = MemoryMarshal.Cast<T, int>(result);
                BinaryPrimitives.ReverseEndianness(span, span);
            }
            else if (typeof(T) == typeof(uint))
            {
                Span<uint> span = MemoryMarshal.Cast<T, uint>(result);
                BinaryPrimitives.ReverseEndianness(span, span);
            }
            else if (typeof(T) == typeof(long) || typeof(T) == typeof(double))
            {
                Span<long> span = MemoryMarshal.Cast<T, long>(result);
                BinaryPrimitives.ReverseEndianness(span, span);
            }
            else if (typeof(T) == typeof(ulong))
            {
                Span<ulong> span = MemoryMarshal.Cast<T, ulong>(result);
                BinaryPrimitives.ReverseEndianness(span, span);
            }
        }

        return result;
    }
#endif
}
