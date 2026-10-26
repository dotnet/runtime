// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Internal.NativeFormat;

namespace Internal.Metadata.NativeFormat.Writer;

public sealed class MetadataBlob : IEquatable<MetadataBlob>
{
    private readonly List<byte> _bytes = new List<byte>();
    private readonly List<HandleReference> _references = new List<HandleReference>();
    private bool _isReadOnly;

    private readonly struct HandleReference(int offset, MetadataRecord record)
    {
        public readonly int Offset = offset;
        public readonly MetadataRecord Record = record;
        public uint EncodedValue => Record is null ? 0 :
            (uint)Record.HandleType | ((uint)Record.HandleOffset << 7);
    }

    public void WriteByte(byte value)
    {
        Debug.Assert(!_isReadOnly);
        _bytes.Add(value);
    }

    public void WriteBytes(ReadOnlySpan<byte> bytes)
    {
        Debug.Assert(!_isReadOnly);
        foreach (byte value in bytes)
            _bytes.Add(value);
    }

    public void WriteString(string value)
    {
        Debug.Assert(!_isReadOnly);
        // ECMA-335 II.23.3: SerString is a compressed UTF8 byte count, or 0xFF for null.
        if (value is null)
        {
            WriteByte(0xff);
            return;
        }

        byte[] bytes = Encoding.UTF8.GetBytes(value);
        int length = bytes.Length;
        Debug.Assert(length <= 0x1fffffff);
        if (length > 0x3fff)
        {
            WriteByte((byte)((length >> 24) | 0xc0));
            WriteByte((byte)(length >> 16));
            WriteByte((byte)(length >> 8));
        }
        else if (length > 0x7f)
        {
            WriteByte((byte)((length >> 8) | 0x80));
        }
        WriteByte((byte)length);
        WriteBytes(bytes);
    }

    public void WriteType(MetadataRecord value)
    {
        Debug.Assert(!_isReadOnly);
        Debug.Assert(value is null || value.HandleType is
            HandleType.TypeDefinition or HandleType.TypeReference or HandleType.TypeSpecification);
        _references.Add(new HandleReference(_bytes.Count, value));
    }

    internal void Visit(MetadataRecord owner, IRecordVisitor visitor)
    {
        _isReadOnly = true;
        for (int i = 0; i < _references.Count; i++)
        {
            HandleReference reference = _references[i];
            _references[i] = new HandleReference(
                reference.Offset, visitor.Visit(owner, reference.Record));
        }
    }

    public bool Equals(MetadataBlob other)
    {
        if (ReferenceEquals(this, other))
            return true;

        if (other is null || _bytes.Count != other._bytes.Count || _references.Count != other._references.Count)
            return false;

        for (int i = 0; i < _bytes.Count; i++)
        {
            if (_bytes[i] != other._bytes[i])
                return false;
        }

        for (int i = 0; i < _references.Count; i++)
        {
            HandleReference left = _references[i];
            HandleReference right = other._references[i];
            if (left.Offset != right.Offset || !Equals(left.Record, right.Record))
                return false;
        }

        return true;
    }

    public override bool Equals(object obj) => obj is MetadataBlob other && Equals(other);

    public override int GetHashCode()
    {
        _isReadOnly = true;
        int hash = 17;
        foreach (byte value in _bytes)
            hash = unchecked(hash * 31 + value);

        foreach (HandleReference reference in _references)
        {
            hash = unchecked(hash * 31 + reference.Offset);
            hash = unchecked(hash * 31 + (reference.Record?.GetHashCode() ?? 0));
        }

        return hash;
    }

    internal void Save(NativeWriter writer)
    {
        uint length = (uint)_bytes.Count;
        foreach (HandleReference reference in _references)
            length = checked(length + (uint)NativePrimitiveEncoder.GetUnsignedEncodingSize(reference.EncodedValue));

        writer.WriteUnsigned(length);
        int offset = 0;
        foreach (HandleReference reference in _references)
        {
            while (offset < reference.Offset)
                writer.WriteByte(_bytes[offset++]);

            writer.WriteUnsigned(reference.EncodedValue);
        }

        while (offset < _bytes.Count)
            writer.WriteByte(_bytes[offset++]);
    }
}
