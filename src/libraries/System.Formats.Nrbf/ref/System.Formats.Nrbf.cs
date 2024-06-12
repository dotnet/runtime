// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Formats.Nrbf
{
    public abstract partial class ArrayRecord : System.Formats.Nrbf.SerializationRecord
    {
        internal ArrayRecord() { }
        public override System.Formats.Nrbf.SerializationRecordId Id { get { throw null; } }
        public abstract System.ReadOnlySpan<int> Lengths { get; }
        public int Rank { get { throw null; } }
        [System.Diagnostics.CodeAnalysis.RequiresDynamicCode("The code for an array of the specified type might not be available.")]
        public System.Array GetArray(System.Type expectedArrayType, bool allowNulls = true) { throw null; }
    }
    public abstract partial class ClassRecord : System.Formats.Nrbf.SerializationRecord
    {
        internal ClassRecord() { }
        public override System.Formats.Nrbf.SerializationRecordId Id { get { throw null; } }
        public System.Collections.Generic.IEnumerable<string> MemberNames { get { throw null; } }
        public override System.Reflection.Metadata.TypeName TypeName { get { throw null; } }
        public System.Formats.Nrbf.ArrayRecord? GetArrayRecord(string memberName) { throw null; }
        public bool GetBoolean(string memberName) { throw null; }
        public byte GetByte(string memberName) { throw null; }
        public char GetChar(string memberName) { throw null; }
        public System.Formats.Nrbf.ClassRecord? GetClassRecord(string memberName) { throw null; }
        public System.DateTime GetDateTime(string memberName) { throw null; }
        public decimal GetDecimal(string memberName) { throw null; }
        public double GetDouble(string memberName) { throw null; }
        public short GetInt16(string memberName) { throw null; }
        public int GetInt32(string memberName) { throw null; }
        public long GetInt64(string memberName) { throw null; }
        public object? GetRawValue(string memberName) { throw null; }
        public sbyte GetSByte(string memberName) { throw null; }
        public System.Formats.Nrbf.SerializationRecord? GetSerializationRecord(string memberName) { throw null; }
        public float GetSingle(string memberName) { throw null; }
        public string? GetString(string memberName) { throw null; }
        public System.TimeSpan GetTimeSpan(string memberName) { throw null; }
        public ushort GetUInt16(string memberName) { throw null; }
        public uint GetUInt32(string memberName) { throw null; }
        public ulong GetUInt64(string memberName) { throw null; }
        public bool HasMember(string memberName) { throw null; }
    }
    public static partial class NrbfDecoder
    {
        public static System.Formats.Nrbf.SerializationRecord Decode(System.IO.Stream payload, out System.Collections.Generic.IReadOnlyDictionary<System.Formats.Nrbf.SerializationRecordId, System.Formats.Nrbf.SerializationRecord> recordMap, System.Formats.Nrbf.PayloadOptions options=null, bool leaveOpen=false) { throw null; }
        public static System.Formats.Nrbf.SerializationRecord Decode(System.IO.Stream payload, System.Formats.Nrbf.PayloadOptions? options=null, bool leaveOpen=false) { throw null; }
        public static System.Formats.Nrbf.ClassRecord DecodeClassRecord(System.IO.Stream payload, System.Formats.Nrbf.PayloadOptions? options=null, bool leaveOpen=false) { throw null; }
        public static bool StartsWithPayloadHeader(byte[] bytes) { throw null; }
        public static bool StartsWithPayloadHeader(System.IO.Stream stream) { throw null; }
    }
    public sealed partial class PayloadOptions
    {
        public PayloadOptions() { }
        public System.Reflection.Metadata.TypeNameParseOptions? TypeNameParseOptions { get { throw null; } set { } }
        public bool UndoTruncatedTypeNames { get { throw null; } set { } }
    }
    public abstract partial class PrimitiveTypeRecord : System.Formats.Nrbf.SerializationRecord
    {
        internal PrimitiveTypeRecord() { }
        public object Value { get { throw null; } }
    }
    public abstract partial class PrimitiveTypeRecord<T> : System.Formats.Nrbf.PrimitiveTypeRecord
    {
        internal PrimitiveTypeRecord() { }
        public override System.Reflection.Metadata.TypeName TypeName { get { throw null; } }
        public new T Value { get { throw null; } }
    }
    public abstract partial class SerializationRecord
    {
        internal SerializationRecord() { }
        public abstract System.Formats.Nrbf.SerializationRecordId Id { get; }
        public abstract System.Formats.Nrbf.SerializationRecordType RecordType { get; }
        public abstract System.Reflection.Metadata.TypeName TypeName { get; }
        public bool TypeNameMatches(System.Type type) { throw null; }
    }
    public partial struct SerializationRecordId : System.IEquatable<System.Formats.Nrbf.SerializationRecordId>
    {
        public bool Equals(System.Formats.Nrbf.SerializationRecordId other) { throw null; }
        public override bool Equals(object? obj) { throw null; }
        public override int GetHashCode() { throw null; }
    }
    public enum SerializationRecordType
    {
        SerializedStreamHeader = 0,
        ClassWithId = 1,
        SystemClassWithMembers = 2,
        ClassWithMembers = 3,
        SystemClassWithMembersAndTypes = 4,
        ClassWithMembersAndTypes = 5,
        BinaryObjectString = 6,
        BinaryArray = 7,
        MemberPrimitiveTyped = 8,
        MemberReference = 9,
        ObjectNull = 10,
        MessageEnd = 11,
        BinaryLibrary = 12,
        ObjectNullMultiple256 = 13,
        ObjectNullMultiple = 14,
        ArraySinglePrimitive = 15,
        ArraySingleObject = 16,
        ArraySingleString = 17,
        MethodCall = 21,
        MethodReturn = 22,
    }
    public abstract partial class SZArrayRecord<T> : System.Formats.Nrbf.ArrayRecord
    {
        internal SZArrayRecord() { }
        public int Length { get { throw null; } }
        public override System.ReadOnlySpan<int> Lengths { get { throw null; } }
        public abstract T?[] GetArray(bool allowNulls = true);
    }
}
