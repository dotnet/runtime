// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Microsoft.Diagnostics.DataContractReader.TestInfrastructure;
public unsafe class TargetTestHelpers
{
    public MockTarget.Architecture Arch { get; init; }

    public TargetTestHelpers(MockTarget.Architecture arch)
    {
        Arch = arch;
    }

    public int PointerSize => Arch.Is64Bit ? sizeof(ulong) : sizeof(uint);
    public ulong MaxSignedTargetAddress => (ulong)(Arch.Is64Bit ? long.MaxValue : int.MaxValue);

    #region Mock memory initialization

    public uint ObjHeaderSize => (uint)(Arch.Is64Bit ? 2 * sizeof(uint) /*alignpad + syncblock*/: sizeof(uint) /* syncblock */);
    public uint ObjectSize => (uint)PointerSize /* methtab */;

    public uint ObjectBaseSize => ObjHeaderSize + ObjectSize;

    public uint ArrayBaseSize => Arch.Is64Bit ? ObjectSize + sizeof(uint) /* numComponents */ + sizeof(uint) /* pad*/ : ObjectSize + sizeof(uint) /* numComponents */;

    public uint ArrayBaseBaseSize => ObjHeaderSize + ArrayBaseSize;

    public uint StringBaseSize => ObjectBaseSize + sizeof(uint) /* length */ + sizeof(char) /* nul terminator */;

    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Kept as instance for consistent overload resolution with other Write overloads.")]
    public void Write(Span<byte> dest, byte b) => dest[0] = b;
    public void Write(Span<byte> dest, ushort u)
    {
        if (Arch.IsLittleEndian)
        {
            BinaryPrimitives.WriteUInt16LittleEndian(dest, u);
        }
        else
        {
            BinaryPrimitives.WriteUInt16BigEndian(dest, u);
        }
    }

    public void Write(Span<byte> dest, int i)
    {
        if (Arch.IsLittleEndian)
        {
            BinaryPrimitives.WriteInt32LittleEndian(dest, i);
        }
        else
        {
            BinaryPrimitives.WriteInt32BigEndian(dest, i);
        }
    }

    public void Write(Span<byte> dest, uint u)
    {
        if (Arch.IsLittleEndian)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(dest, u);
        }
        else
        {
            BinaryPrimitives.WriteUInt32BigEndian(dest, u);
        }
    }

    public void Write(Span<byte> dest, ulong u)
    {
        if (Arch.IsLittleEndian)
        {
            BinaryPrimitives.WriteUInt64LittleEndian(dest, u);
        }
        else
        {
            BinaryPrimitives.WriteUInt64BigEndian(dest, u);
        }
    }

    public void WritePointer(Span<byte> dest, ulong value)
    {
        if (Arch.Is64Bit)
        {
            if (Arch.IsLittleEndian)
            {
                BinaryPrimitives.WriteUInt64LittleEndian(dest, value);
            }
            else
            {
                BinaryPrimitives.WriteUInt64BigEndian(dest, value);
            }
        }
        else
        {
            if (Arch.IsLittleEndian)
            {
                BinaryPrimitives.WriteUInt32LittleEndian(dest, (uint)value);
            }
            else
            {
                BinaryPrimitives.WriteUInt32BigEndian(dest, (uint)value);
            }
        }
    }

    public void WriteNUInt(Span<byte> dest, TargetNUInt targetNUInt) => WritePointer(dest, targetNUInt.Value);

    public TargetPointer ReadPointer(ReadOnlySpan<byte> src)
    {
        if (Arch.Is64Bit)
        {
            return Arch.IsLittleEndian ? BinaryPrimitives.ReadUInt64LittleEndian(src) : BinaryPrimitives.ReadUInt64BigEndian(src);
        }
        else
        {
            return Arch.IsLittleEndian ? BinaryPrimitives.ReadUInt32LittleEndian(src) : BinaryPrimitives.ReadUInt32BigEndian(src);
        }
    }

    public void WriteUtf16String(Span<byte> dest, string value)
    {
        Encoding encoding = Arch.IsLittleEndian ? Encoding.Unicode : Encoding.BigEndianUnicode;
        byte[] valueBytes = encoding.GetBytes(value);
        int len = valueBytes.Length + sizeof(char);
        if (dest.Length < len)
            throw new InvalidOperationException($"Destination is too short to write '{value}'. Required length: {len}, actual: {dest.Length}");

        valueBytes.AsSpan().CopyTo(dest);
        dest[^2] = 0;
        dest[^1] = 0;
    }

    public int SizeOfPrimitive(DataType type)
    {
        return type switch
        {
            DataType.uint8 or DataType.int8 => sizeof(byte),
            DataType.uint16 or DataType.int16 => sizeof(ushort),
            DataType.uint32 or DataType.int32 => sizeof(uint),
            DataType.uint64 or DataType.int64 => sizeof(ulong),
            DataType.pointer or DataType.nint or DataType.nuint => PointerSize,
            _ => throw new InvalidOperationException($"Not a primitive: {type}"),
        };
    }

    #endregion Mock memory initialization

    private static int AlignUp(int offset, int align)
    {
        return (offset + align - 1) & ~(align - 1);
    }

    public enum FieldLayout
    {
        CIsh, /* align each field to its size */
        Packed, /* pack fields contiguously */
    }

    public readonly struct LayoutResult
    {
        public Dictionary<string, Target.FieldInfo> Fields { get; init; }
        /* offset between elements of this type in an array */
        public uint Stride { get; init; }
        /* maximum alignment of any field */
        public readonly uint MaxAlign { get; init; }
    }

    public record Field(string Name, DataType Type, uint? Size = null);

    // Implements a simple layout algorithm that aligns fields to their size
    // and aligns the structure to the largest field size.
    public LayoutResult LayoutFields(Field[] fields)
        => LayoutFields(FieldLayout.CIsh, fields);

    // Layout the fields of a structure according to the specified layout style.
    public LayoutResult  LayoutFields(FieldLayout style, Field[] fields)
    {
        int offset = 0;
        int maxAlign = 1;
        return LayoutFieldsWorker(style, fields, ref offset, ref maxAlign);
    }

    private LayoutResult LayoutFieldsWorker(FieldLayout style, Field[] fields, ref int offset, ref int maxAlign)
    {
        Dictionary<string, Target.FieldInfo> fieldInfos = [];
        for (int i = 0; i < fields.Length; i++)
        {
            var (name, type, sizeMaybe) = fields[i];
            int size = sizeMaybe.HasValue ? (int)sizeMaybe.Value : SizeOfPrimitive(type);
            int align = size;
            if (align > maxAlign)
            {
                maxAlign = align;
            }
            offset = style switch
            {
                FieldLayout.CIsh => AlignUp(offset, align),
                FieldLayout.Packed => offset,
                _ => throw new InvalidOperationException("Unknown layout style"),
            };
            fieldInfos[name] = new Target.FieldInfo {
                Offset = offset,
                TypeName = type.ToString(),
            };
            offset += size;
        }
        int stride = style switch {
            FieldLayout.CIsh => AlignUp(offset, maxAlign),
            FieldLayout.Packed => offset,
            _ => throw new InvalidOperationException("Unknown layout style"),
        };
        return new LayoutResult() { Fields = fieldInfos, Stride = (uint)stride, MaxAlign = (uint)maxAlign};
    }

    // Extend the layout of a base class with additional fields.
    public LayoutResult ExtendLayout(Field[] fields, LayoutResult baseClass) => ExtendLayout(FieldLayout.CIsh, fields, baseClass);

    public LayoutResult ExtendLayout(FieldLayout fieldLayout, Field[] fields, LayoutResult baseClass)
    {
        int offset = (int)baseClass.Stride;
        int maxAlign = (int)baseClass.MaxAlign;
        return LayoutFieldsWorker(fieldLayout, fields, ref offset, ref maxAlign);
    }

    public static Target.TypeInfo CreateTypeInfo(Layout layout)
    {
        Dictionary<string, Target.FieldInfo> fields = new(layout.Fields.Length, StringComparer.Ordinal);
        foreach (LayoutField field in layout.Fields)
        {
            fields[field.Name] = new Target.FieldInfo
            {
                Offset = field.Offset,
            };
        }

        return new Target.TypeInfo
        {
            Size = (uint)layout.Size,
            Fields = fields,
        };
    }

}
