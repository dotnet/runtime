// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;

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
        => Read<uint>(GetFieldSlice(fieldName));

    protected ulong ReadUInt64Field(string fieldName)
        => Read<ulong>(GetFieldSlice(fieldName));

    protected void WritePointerField(string fieldName, ulong value)
        => WritePointer(GetFieldSlice(fieldName), value);

    protected void WriteUInt32Field(string fieldName, uint value)
        => Write(GetFieldSlice(fieldName), value);

    protected void WriteUInt64Field(string fieldName, ulong value)
        => Write(GetFieldSlice(fieldName), value);

    protected ulong GetFieldAddress(string fieldName)
        => Address + (ulong)Layout.GetField(fieldName).Offset;

    protected Span<byte> GetFieldSlice(string fieldName)
    {
        LayoutField field = Layout.GetField(fieldName);
        return Memory.Span.Slice(field.Offset, field.Size);
    }

    protected ulong ReadPointer(ReadOnlySpan<byte> source)
        => Architecture.Is64Bit ? Read<ulong>(source) : Read<uint>(source);

    protected void WritePointer(Span<byte> destination, ulong value)
    {
        if (Architecture.Is64Bit)
        {
            Write(destination, value);
            return;
        }

        Write(destination, unchecked((uint)value));
    }

    private T Read<T>(ReadOnlySpan<byte> source)
        where T : unmanaged, IBinaryInteger<T>, IMinMaxValue<T>
    {
        bool success = Architecture.IsLittleEndian
            ? T.TryReadLittleEndian(source, isUnsigned: true, out T value)
            : T.TryReadBigEndian(source, isUnsigned: true, out value);

        if (!success)
        {
            throw new InvalidOperationException($"Failed to read a {typeof(T).Name} value.");
        }

        return value;
    }

    private void Write<T>(Span<byte> destination, T value)
        where T : unmanaged, IBinaryInteger<T>, IMinMaxValue<T>
    {
        bool success = Architecture.IsLittleEndian
            ? value.TryWriteLittleEndian(destination, out int bytesWritten)
            : value.TryWriteBigEndian(destination, out bytesWritten);

        if (!success || bytesWritten != destination.Length)
        {
            throw new InvalidOperationException($"Failed to write a {typeof(T).Name} value.");
        }
    }
}
