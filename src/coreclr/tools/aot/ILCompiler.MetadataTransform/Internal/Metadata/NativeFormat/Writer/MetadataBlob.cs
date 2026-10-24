// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Internal.NativeFormat;

namespace Internal.Metadata.NativeFormat.Writer;

public sealed class MetadataBlob : IEquatable<MetadataBlob>
{
    private readonly List<byte> _bytes = new List<byte>();
    private readonly List<HandleReference> _references = new List<HandleReference>();
    private bool _isReadOnly;

    private readonly struct HandleReference(int offset, MetadataRecord record, bool isTyped)
    {
        public readonly int Offset = offset;
        public readonly MetadataRecord Record = record;
        public readonly bool IsTyped = isTyped;

        public uint EncodedValue => Record is null ? 0 :
            IsTyped ? (uint)Record.HandleOffset : (uint)Record.HandleType | ((uint)Record.HandleOffset << 7);
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

    public void WriteString(ConstantStringValue value)
    {
        Debug.Assert(!_isReadOnly);
        Debug.Assert(value is null || value.Value is not null);
        _references.Add(new HandleReference(_bytes.Count, value, isTyped: true));
    }

    public void WriteType(MetadataRecord value)
    {
        Debug.Assert(!_isReadOnly);
        Debug.Assert(value is null || value.HandleType is
            HandleType.TypeDefinition or HandleType.TypeReference or HandleType.TypeSpecification);
        _references.Add(new HandleReference(_bytes.Count, value, isTyped: false));
    }

    internal void Visit(MetadataRecord owner, IRecordVisitor visitor)
    {
        _isReadOnly = true;
        for (int i = 0; i < _references.Count; i++)
        {
            HandleReference reference = _references[i];
            _references[i] = new HandleReference(
                reference.Offset, visitor.Visit(owner, reference.Record), reference.IsTyped);
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
            if (left.Offset != right.Offset || left.IsTyped != right.IsTyped || !Equals(left.Record, right.Record))
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
            hash = unchecked(hash * 31 + (reference.IsTyped ? 1 : 0));
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
