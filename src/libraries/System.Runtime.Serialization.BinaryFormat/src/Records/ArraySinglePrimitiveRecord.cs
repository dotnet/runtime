// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection.Metadata;

namespace System.Runtime.Serialization.BinaryFormat;

/// <summary>
///  Single dimensional array of a primitive type.
/// </summary>
/// <remarks>
///  <para>
///   <see href="https://learn.microsoft.com/openspecs/windows_protocols/ms-nrbf/3a50a305-5f32-48a1-a42a-c34054db310b">
///    [MS-NRBF] 2.4.3.3
///   </see>
///  </para>
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
        => s_elementTypeName ??= TypeName.Parse(typeof(T).FullName.AsSpan()).WithAssemblyName(FormatterServices.GetAssemblyNameIncludingTypeForwards(typeof(T)));

    internal IReadOnlyList<T> Values { get; }

    public override bool IsTypeNameMatching(Type type) => typeof(T[]) == type;

    internal override bool IsElementType(Type typeElement) => typeElement == typeof(T);

    protected override T[] ToArrayOfT(bool allowNulls) => [.. Values];

    internal override (AllowedRecordTypes allowed, PrimitiveType primitiveType) GetAllowedRecordType()
        => throw new InvalidOperationException("This should never happen");

    private protected override void AddValue(object value)
        => throw new InvalidOperationException("This should never happen");

    internal static IReadOnlyList<T> ReadPrimitiveTypes(BinaryReader reader, int count)
    {
        if (typeof(T) == typeof(byte))
        {
            return (IReadOnlyList<T>)(object)ReadBytes(reader, count);
        }

        List<T> values = [];
        for (int i = 0; i < count; i++)
        {
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
                values.Add((T)(object)BinaryReaderExtensions.CreateDateTimeFromData(reader.ReadInt64()));
            }
            else if (typeof(T) == typeof(TimeSpan))
            {
                values.Add((T)(object)new TimeSpan(reader.ReadInt64()));
            }
            else
            {
                throw new SerializationException($"Failure trying to read primitive '{typeof(T)}'");
            }
        }

        return values;
    }

    private static IReadOnlyList<byte> ReadBytes(BinaryReader reader, int count)
    {
        // Special casing byte for performance.
        if (count <= DefaultMaxArrayLength)
        {
            return reader.ReadBytes(count);
        }

        // But only to certain degree, as the input is untrusted.
        List<byte> result = new(DefaultMaxArrayLength);
        byte[] bytes = ArrayPool<byte>.Shared.Rent(DefaultMaxArrayLength);

        while (count > 0)
        {
            int bytesRead = reader.Read(bytes, 0, Math.Min(count, bytes.Length));

            if (bytesRead <= 0)
            {
                ThrowHelper.ThrowEndOfStreamException();
            }
            else if (bytesRead == bytes.Length)
            {
                result.AddRange(bytes);
            }
            else
            {
                result.AddRange(new ArraySegment<byte>(bytes, 0, bytesRead));
            }

            count -= bytesRead;
        }

        ArrayPool<byte>.Shared.Return(bytes);
        return result;
    }
}
