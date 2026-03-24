// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

internal abstract class TypedView
{
    public ulong Address { get; private set; }

    public Memory<byte> Memory { get; private set; }

    public Layout Layout { get; private set; } = null!;

    protected MockTarget.Architecture Architecture { get; private set; }

    internal void Init(Memory<byte> memory, ulong address, Layout layout)
    {
        ArgumentNullException.ThrowIfNull(layout);
        ArgumentOutOfRangeException.ThrowIfLessThan(memory.Length, layout.Size);

        Address = address;
        Memory = memory;
        Layout = layout;
        Architecture = layout.Architecture;
    }

    protected ulong ReadPointerField(string fieldName)
        => ReadPointer(GetFieldSlice(fieldName));

    protected uint ReadUInt32Field(string fieldName)
    {
        ReadOnlySpan<byte> source = GetFieldSlice(fieldName);
        return Architecture.IsLittleEndian
            ? BinaryPrimitives.ReadUInt32LittleEndian(source)
            : BinaryPrimitives.ReadUInt32BigEndian(source);
    }

    protected ulong ReadUInt64Field(string fieldName)
    {
        ReadOnlySpan<byte> source = GetFieldSlice(fieldName);
        return Architecture.IsLittleEndian
            ? BinaryPrimitives.ReadUInt64LittleEndian(source)
            : BinaryPrimitives.ReadUInt64BigEndian(source);
    }

    protected void WritePointerField(string fieldName, ulong value)
        => WritePointer(GetFieldSlice(fieldName), value);

    protected void WriteUInt32Field(string fieldName, uint value)
        => WriteUInt32(GetFieldSlice(fieldName), value);

    protected void WriteUInt64Field(string fieldName, ulong value)
        => WriteUInt64(GetFieldSlice(fieldName), value);

    protected ulong GetFieldAddress(string fieldName)
        => Address + (ulong)Layout.GetField(fieldName).Offset;

    protected Span<byte> GetFieldSlice(string fieldName)
    {
        LayoutField field = Layout.GetField(fieldName);
        return Memory.Span.Slice(field.Offset, field.Size);
    }

    protected ulong ReadPointer(ReadOnlySpan<byte> source)
    {
        if (Architecture.Is64Bit)
        {
            return Architecture.IsLittleEndian
                ? BinaryPrimitives.ReadUInt64LittleEndian(source)
                : BinaryPrimitives.ReadUInt64BigEndian(source);
        }

        return Architecture.IsLittleEndian
            ? BinaryPrimitives.ReadUInt32LittleEndian(source)
            : BinaryPrimitives.ReadUInt32BigEndian(source);
    }

    protected void WritePointer(Span<byte> destination, ulong value)
    {
        if (Architecture.Is64Bit)
        {
            if (Architecture.IsLittleEndian)
            {
                BinaryPrimitives.WriteUInt64LittleEndian(destination, value);
            }
            else
            {
                BinaryPrimitives.WriteUInt64BigEndian(destination, value);
            }

            return;
        }

        uint truncatedValue = unchecked((uint)value);
        if (Architecture.IsLittleEndian)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(destination, truncatedValue);
        }
        else
        {
            BinaryPrimitives.WriteUInt32BigEndian(destination, truncatedValue);
        }
    }

    protected void WriteUInt32(Span<byte> destination, uint value)
    {
        if (Architecture.IsLittleEndian)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(destination, value);
        }
        else
        {
            BinaryPrimitives.WriteUInt32BigEndian(destination, value);
        }
    }

    protected void WriteUInt64(Span<byte> destination, ulong value)
    {
        if (Architecture.IsLittleEndian)
        {
            BinaryPrimitives.WriteUInt64LittleEndian(destination, value);
        }
        else
        {
            BinaryPrimitives.WriteUInt64BigEndian(destination, value);
        }
    }
}
