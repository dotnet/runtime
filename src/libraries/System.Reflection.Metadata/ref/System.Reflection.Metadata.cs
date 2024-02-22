// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Reflection
{
    [System.FlagsAttribute]
    public enum AssemblyFlags
    {
        PublicKey = 1,
        Retargetable = 256,
        WindowsRuntime = 512,
        ContentTypeMask = 3584,
        DisableJitCompileOptimizer = 16384,
        EnableJitCompileTracking = 32768,
    }
    public enum AssemblyHashAlgorithm
    {
        None = 0,
        MD5 = 32771,
        Sha1 = 32772,
        Sha256 = 32780,
        Sha384 = 32781,
        Sha512 = 32782,
    }
    public enum DeclarativeSecurityAction : short
    {
        None = (short)0,
        Demand = (short)2,
        Assert = (short)3,
        Deny = (short)4,
        PermitOnly = (short)5,
        LinkDemand = (short)6,
        InheritanceDemand = (short)7,
        RequestMinimum = (short)8,
        RequestOptional = (short)9,
        RequestRefuse = (short)10,
    }
    [System.FlagsAttribute]
    public enum ManifestResourceAttributes
    {
        Public = 1,
        Private = 2,
        VisibilityMask = 7,
    }
    [System.FlagsAttribute]
    public enum MethodImportAttributes : short
    {
        None = (short)0,
        ExactSpelling = (short)1,
        CharSetAnsi = (short)2,
        CharSetUnicode = (short)4,
        CharSetAuto = (short)6,
        CharSetMask = (short)6,
        BestFitMappingEnable = (short)16,
        BestFitMappingDisable = (short)32,
        BestFitMappingMask = (short)48,
        SetLastError = (short)64,
        CallingConventionWinApi = (short)256,
        CallingConventionCDecl = (short)512,
        CallingConventionStdCall = (short)768,
        CallingConventionThisCall = (short)1024,
        CallingConventionFastCall = (short)1280,
        CallingConventionMask = (short)1792,
        ThrowOnUnmappableCharEnable = (short)4096,
        ThrowOnUnmappableCharDisable = (short)8192,
        ThrowOnUnmappableCharMask = (short)12288,
    }
    [System.FlagsAttribute]
    public enum MethodSemanticsAttributes
    {
        Setter = 1,
        Getter = 2,
        Other = 4,
        Adder = 8,
        Remover = 16,
        Raiser = 32,
    }
}
namespace System.Reflection.Metadata
{
    public readonly partial struct ArrayShape
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public ArrayShape(int rank, System.Collections.Immutable.ImmutableArray<int> sizes, System.Collections.Immutable.ImmutableArray<int> lowerBounds) { throw null; }
        public System.Collections.Immutable.ImmutableArray<int> LowerBounds { get { throw null; } }
        public int Rank { get { throw null; } }
        public System.Collections.Immutable.ImmutableArray<int> Sizes { get { throw null; } }
    }
    public readonly partial struct AssemblyDefinition
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public System.Reflection.Metadata.StringHandle Culture { get { throw null; } }
        public System.Reflection.AssemblyFlags Flags { get { throw null; } }
        public System.Reflection.AssemblyHashAlgorithm HashAlgorithm { get { throw null; } }
        public System.Reflection.Metadata.StringHandle Name { get { throw null; } }
        public System.Reflection.Metadata.BlobHandle PublicKey { get { throw null; } }
        public System.Version Version { get { throw null; } }
        public System.Reflection.Metadata.CustomAttributeHandleCollection GetCustomAttributes() { throw null; }
        public System.Reflection.Metadata.DeclarativeSecurityAttributeHandleCollection GetDeclarativeSecurityAttributes() { throw null; }
    }
    public readonly partial struct AssemblyDefinitionHandle : System.IEquatable<System.Reflection.Metadata.AssemblyDefinitionHandle>
    {
        private readonly int _dummyPrimitive;
        public bool IsNil { get { throw null; } }
        public override bool Equals(object? obj) { throw null; }
        public bool Equals(System.Reflection.Metadata.AssemblyDefinitionHandle other) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Reflection.Metadata.AssemblyDefinitionHandle left, System.Reflection.Metadata.AssemblyDefinitionHandle right) { throw null; }
        public static explicit operator System.Reflection.Metadata.AssemblyDefinitionHandle (System.Reflection.Metadata.EntityHandle handle) { throw null; }
        public static explicit operator System.Reflection.Metadata.AssemblyDefinitionHandle (System.Reflection.Metadata.Handle handle) { throw null; }
        public static implicit operator System.Reflection.Metadata.EntityHandle (System.Reflection.Metadata.AssemblyDefinitionHandle handle) { throw null; }
        public static implicit operator System.Reflection.Metadata.Handle (System.Reflection.Metadata.AssemblyDefinitionHandle handle) { throw null; }
        public static bool operator !=(System.Reflection.Metadata.AssemblyDefinitionHandle left, System.Reflection.Metadata.AssemblyDefinitionHandle right) { throw null; }
    }
    public readonly partial struct AssemblyFile
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public bool ContainsMetadata { get { throw null; } }
        public System.Reflection.Metadata.BlobHandle HashValue { get { throw null; } }
        public System.Reflection.Metadata.StringHandle Name { get { throw null; } }
        public System.Reflection.Metadata.CustomAttributeHandleCollection GetCustomAttributes() { throw null; }
    }
    public readonly partial struct AssemblyFileHandle : System.IEquatable<System.Reflection.Metadata.AssemblyFileHandle>
    {
        private readonly int _dummyPrimitive;
        public bool IsNil { get { throw null; } }
        public override bool Equals(object? obj) { throw null; }
        public bool Equals(System.Reflection.Metadata.AssemblyFileHandle other) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Reflection.Metadata.AssemblyFileHandle left, System.Reflection.Metadata.AssemblyFileHandle right) { throw null; }
        public static explicit operator System.Reflection.Metadata.AssemblyFileHandle (System.Reflection.Metadata.EntityHandle handle) { throw null; }
        public static explicit operator System.Reflection.Metadata.AssemblyFileHandle (System.Reflection.Metadata.Handle handle) { throw null; }
        public static implicit operator System.Reflection.Metadata.EntityHandle (System.Reflection.Metadata.AssemblyFileHandle handle) { throw null; }
        public static implicit operator System.Reflection.Metadata.Handle (System.Reflection.Metadata.AssemblyFileHandle handle) { throw null; }
        public static bool operator !=(System.Reflection.Metadata.AssemblyFileHandle left, System.Reflection.Metadata.AssemblyFileHandle right) { throw null; }
    }
    public readonly partial struct AssemblyFileHandleCollection : System.Collections.Generic.IEnumerable<System.Reflection.Metadata.AssemblyFileHandle>, System.Collections.Generic.IReadOnlyCollection<System.Reflection.Metadata.AssemblyFileHandle>, System.Collections.IEnumerable
    {
        private readonly int _dummyPrimitive;
        public int Count { get { throw null; } }
        public System.Reflection.Metadata.AssemblyFileHandleCollection.Enumerator GetEnumerator() { throw null; }
        System.Collections.Generic.IEnumerator<System.Reflection.Metadata.AssemblyFileHandle> System.Collections.Generic.IEnumerable<System.Reflection.Metadata.AssemblyFileHandle>.GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        public partial struct Enumerator : System.Collections.Generic.IEnumerator<System.Reflection.Metadata.AssemblyFileHandle>, System.Collections.IEnumerator, System.IDisposable
        {
            private int _dummyPrimitive;
            public System.Reflection.Metadata.AssemblyFileHandle Current { get { throw null; } }
            object System.Collections.IEnumerator.Current { get { throw null; } }
            public bool MoveNext() { throw null; }
            void System.Collections.IEnumerator.Reset() { }
            void System.IDisposable.Dispose() { }
        }
    }
    public readonly partial struct AssemblyReference
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public System.Reflection.Metadata.StringHandle Culture { get { throw null; } }
        public System.Reflection.AssemblyFlags Flags { get { throw null; } }
        public System.Reflection.Metadata.BlobHandle HashValue { get { throw null; } }
        public System.Reflection.Metadata.StringHandle Name { get { throw null; } }
        public System.Reflection.Metadata.BlobHandle PublicKeyOrToken { get { throw null; } }
        public System.Version Version { get { throw null; } }
        public System.Reflection.Metadata.CustomAttributeHandleCollection GetCustomAttributes() { throw null; }
    }
    public readonly partial struct AssemblyReferenceHandle : System.IEquatable<System.Reflection.Metadata.AssemblyReferenceHandle>
    {
        private readonly int _dummyPrimitive;
        public bool IsNil { get { throw null; } }
        public override bool Equals(object? obj) { throw null; }
        public bool Equals(System.Reflection.Metadata.AssemblyReferenceHandle other) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Reflection.Metadata.AssemblyReferenceHandle left, System.Reflection.Metadata.AssemblyReferenceHandle right) { throw null; }
        public static explicit operator System.Reflection.Metadata.AssemblyReferenceHandle (System.Reflection.Metadata.EntityHandle handle) { throw null; }
        public static explicit operator System.Reflection.Metadata.AssemblyReferenceHandle (System.Reflection.Metadata.Handle handle) { throw null; }
        public static implicit operator System.Reflection.Metadata.EntityHandle (System.Reflection.Metadata.AssemblyReferenceHandle handle) { throw null; }
        public static implicit operator System.Reflection.Metadata.Handle (System.Reflection.Metadata.AssemblyReferenceHandle handle) { throw null; }
        public static bool operator !=(System.Reflection.Metadata.AssemblyReferenceHandle left, System.Reflection.Metadata.AssemblyReferenceHandle right) { throw null; }
    }
    public readonly partial struct AssemblyReferenceHandleCollection : System.Collections.Generic.IEnumerable<System.Reflection.Metadata.AssemblyReferenceHandle>, System.Collections.Generic.IReadOnlyCollection<System.Reflection.Metadata.AssemblyReferenceHandle>, System.Collections.IEnumerable
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public int Count { get { throw null; } }
        public System.Reflection.Metadata.AssemblyReferenceHandleCollection.Enumerator GetEnumerator() { throw null; }
        System.Collections.Generic.IEnumerator<System.Reflection.Metadata.AssemblyReferenceHandle> System.Collections.Generic.IEnumerable<System.Reflection.Metadata.AssemblyReferenceHandle>.GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        public partial struct Enumerator : System.Collections.Generic.IEnumerator<System.Reflection.Metadata.AssemblyReferenceHandle>, System.Collections.IEnumerator, System.IDisposable
        {
            private object _dummy;
            private int _dummyPrimitive;
            public System.Reflection.Metadata.AssemblyReferenceHandle Current { get { throw null; } }
            object System.Collections.IEnumerator.Current { get { throw null; } }
            public bool MoveNext() { throw null; }
            void System.Collections.IEnumerator.Reset() { }
            void System.IDisposable.Dispose() { }
        }
    }
    public readonly partial struct Blob
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public bool IsDefault { get { throw null; } }
        public int Length { get { throw null; } }
        public System.ArraySegment<byte> GetBytes() { throw null; }
    }
    public partial class BlobBuilder
    {
        public BlobBuilder(int capacity = 256) { }
        protected internal int ChunkCapacity { get { throw null; } }
        public int Count { get { throw null; } }
        protected int FreeBytes { get { throw null; } }
        public void Align(int alignment) { }
        protected virtual System.Reflection.Metadata.BlobBuilder AllocateChunk(int minimalSize) { throw null; }
        public void Clear() { }
        public bool ContentEquals(System.Reflection.Metadata.BlobBuilder other) { throw null; }
        protected void Free() { }
        protected virtual void FreeChunk() { }
        public System.Reflection.Metadata.BlobBuilder.Blobs GetBlobs() { throw null; }
        public void LinkPrefix(System.Reflection.Metadata.BlobBuilder prefix) { }
        public void LinkSuffix(System.Reflection.Metadata.BlobBuilder suffix) { }
        public void PadTo(int position) { }
        public System.Reflection.Metadata.Blob ReserveBytes(int byteCount) { throw null; }
        public byte[] ToArray() { throw null; }
        public byte[] ToArray(int start, int byteCount) { throw null; }
        public System.Collections.Immutable.ImmutableArray<byte> ToImmutableArray() { throw null; }
        public System.Collections.Immutable.ImmutableArray<byte> ToImmutableArray(int start, int byteCount) { throw null; }
        public int TryWriteBytes(System.IO.Stream source, int byteCount) { throw null; }
        public void WriteBoolean(bool value) { }
        public void WriteByte(byte value) { }
        public unsafe void WriteBytes(byte* buffer, int byteCount) { }
        public void WriteBytes(byte value, int byteCount) { }
        public void WriteBytes(byte[] buffer) { }
        public void WriteBytes(byte[] buffer, int start, int byteCount) { }
        public void WriteBytes(System.Collections.Immutable.ImmutableArray<byte> buffer) { }
        public void WriteBytes(System.Collections.Immutable.ImmutableArray<byte> buffer, int start, int byteCount) { }
        public void WriteCompressedInteger(int value) { }
        public void WriteCompressedSignedInteger(int value) { }
        public void WriteConstant(object? value) { }
        public void WriteContentTo(System.IO.Stream destination) { }
        public void WriteContentTo(System.Reflection.Metadata.BlobBuilder destination) { }
        public void WriteContentTo(ref System.Reflection.Metadata.BlobWriter destination) { }
        public void WriteDateTime(System.DateTime value) { }
        public void WriteDecimal(decimal value) { }
        public void WriteDouble(double value) { }
        public void WriteGuid(System.Guid value) { }
        public void WriteInt16(short value) { }
        public void WriteInt16BE(short value) { }
        public void WriteInt32(int value) { }
        public void WriteInt32BE(int value) { }
        public void WriteInt64(long value) { }
        public void WriteReference(int reference, bool isSmall) { }
        public void WriteSByte(sbyte value) { }
        public void WriteSerializedString(string? value) { }
        public void WriteSingle(float value) { }
        public void WriteUInt16(ushort value) { }
        public void WriteUInt16BE(ushort value) { }
        public void WriteUInt32(uint value) { }
        public void WriteUInt32BE(uint value) { }
        public void WriteUInt64(ulong value) { }
        public void WriteUserString(string value) { }
        public void WriteUTF16(char[] value) { }
        public void WriteUTF16(string value) { }
        public void WriteUTF8(string value, bool allowUnpairedSurrogates = true) { }
        public partial struct Blobs : System.Collections.Generic.IEnumerable<System.Reflection.Metadata.Blob>, System.Collections.Generic.IEnumerator<System.Reflection.Metadata.Blob>, System.Collections.IEnumerable, System.Collections.IEnumerator, System.IDisposable
        {
            private object _dummy;
            private int _dummyPrimitive;
            public System.Reflection.Metadata.Blob Current { get { throw null; } }
            object System.Collections.IEnumerator.Current { get { throw null; } }
            public System.Reflection.Metadata.BlobBuilder.Blobs GetEnumerator() { throw null; }
            public bool MoveNext() { throw null; }
            public void Reset() { }
            System.Collections.Generic.IEnumerator<System.Reflection.Metadata.Blob> System.Collections.Generic.IEnumerable<System.Reflection.Metadata.Blob>.GetEnumerator() { throw null; }
            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
            void System.IDisposable.Dispose() { }
        }
    }
    public readonly partial struct BlobContentId : System.IEquatable<System.Reflection.Metadata.BlobContentId>
    {
        private readonly int _dummyPrimitive;
        public BlobContentId(byte[] id) { throw null; }
        public BlobContentId(System.Collections.Immutable.ImmutableArray<byte> id) { throw null; }
        public BlobContentId(System.Guid guid, uint stamp) { throw null; }
        public System.Guid Guid { get { throw null; } }
        public bool IsDefault { get { throw null; } }
        public uint Stamp { get { throw null; } }
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] object? obj) { throw null; }
        public bool Equals(System.Reflection.Metadata.BlobContentId other) { throw null; }
        public static System.Reflection.Metadata.BlobContentId FromHash(byte[] hashCode) { throw null; }
        public static System.Reflection.Metadata.BlobContentId FromHash(System.Collections.Immutable.ImmutableArray<byte> hashCode) { throw null; }
        public override int GetHashCode() { throw null; }
        public static System.Func<System.Collections.Generic.IEnumerable<System.Reflection.Metadata.Blob>, System.Reflection.Metadata.BlobContentId> GetTimeBasedProvider() { throw null; }
        public static bool operator ==(System.Reflection.Metadata.BlobContentId left, System.Reflection.Metadata.BlobContentId right) { throw null; }
        public static bool operator !=(System.Reflection.Metadata.BlobContentId left, System.Reflection.Metadata.BlobContentId right) { throw null; }
    }
    public readonly partial struct BlobHandle : System.IEquatable<System.Reflection.Metadata.BlobHandle>
    {
        private readonly int _dummyPrimitive;
        public bool IsNil { get { throw null; } }
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] object? obj) { throw null; }
        public bool Equals(System.Reflection.Metadata.BlobHandle other) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Reflection.Metadata.BlobHandle left, System.Reflection.Metadata.BlobHandle right) { throw null; }
        public static explicit operator System.Reflection.Metadata.BlobHandle (System.Reflection.Metadata.Handle handle) { throw null; }
        public static implicit operator System.Reflection.Metadata.Handle (System.Reflection.Metadata.BlobHandle handle) { throw null; }
        public static bool operator !=(System.Reflection.Metadata.BlobHandle left, System.Reflection.Metadata.BlobHandle right) { throw null; }
    }
    public partial struct BlobReader
    {
        private int _dummyPrimitive;
        public unsafe BlobReader(byte* buffer, int length) { throw null; }
        public unsafe byte* CurrentPointer { get { throw null; } }
        public int Length { get { throw null; } }
        public int Offset { get { throw null; } set { } }
        public int RemainingBytes { get { throw null; } }
        public unsafe byte* StartPointer { get { throw null; } }
        public void Align(byte alignment) { }
        public int IndexOf(byte value) { throw null; }
        public System.Reflection.Metadata.BlobHandle ReadBlobHandle() { throw null; }
        public bool ReadBoolean() { throw null; }
        public byte ReadByte() { throw null; }
        public byte[] ReadBytes(int byteCount) { throw null; }
        public void ReadBytes(int byteCount, byte[] buffer, int bufferOffset) { }
        public char ReadChar() { throw null; }
        public int ReadCompressedInteger() { throw null; }
        public int ReadCompressedSignedInteger() { throw null; }
        public object? ReadConstant(System.Reflection.Metadata.ConstantTypeCode typeCode) { throw null; }
        public System.DateTime ReadDateTime() { throw null; }
        public decimal ReadDecimal() { throw null; }
        public double ReadDouble() { throw null; }
        public System.Guid ReadGuid() { throw null; }
        public short ReadInt16() { throw null; }
        public int ReadInt32() { throw null; }
        public long ReadInt64() { throw null; }
        public sbyte ReadSByte() { throw null; }
        public System.Reflection.Metadata.SerializationTypeCode ReadSerializationTypeCode() { throw null; }
        public string? ReadSerializedString() { throw null; }
        public System.Reflection.Metadata.SignatureHeader ReadSignatureHeader() { throw null; }
        public System.Reflection.Metadata.SignatureTypeCode ReadSignatureTypeCode() { throw null; }
        public float ReadSingle() { throw null; }
        public System.Reflection.Metadata.EntityHandle ReadTypeHandle() { throw null; }
        public ushort ReadUInt16() { throw null; }
        public uint ReadUInt32() { throw null; }
        public ulong ReadUInt64() { throw null; }
        public string ReadUTF16(int byteCount) { throw null; }
        public string ReadUTF8(int byteCount) { throw null; }
        public void Reset() { }
        public bool TryReadCompressedInteger(out int value) { throw null; }
        public bool TryReadCompressedSignedInteger(out int value) { throw null; }
    }
    public partial struct BlobWriter
    {
        private object _dummy;
        private int _dummyPrimitive;
        public BlobWriter(byte[] buffer) { throw null; }
        public BlobWriter(byte[] buffer, int start, int count) { throw null; }
        public BlobWriter(int size) { throw null; }
        public BlobWriter(System.Reflection.Metadata.Blob blob) { throw null; }
        public System.Reflection.Metadata.Blob Blob { get { throw null; } }
        public int Length { get { throw null; } }
        public int Offset { get { throw null; } set { } }
        public int RemainingBytes { get { throw null; } }
        public void Align(int alignment) { }
        public void Clear() { }
        public bool ContentEquals(System.Reflection.Metadata.BlobWriter other) { throw null; }
        public void PadTo(int offset) { }
        public byte[] ToArray() { throw null; }
        public byte[] ToArray(int start, int byteCount) { throw null; }
        public System.Collections.Immutable.ImmutableArray<byte> ToImmutableArray() { throw null; }
        public System.Collections.Immutable.ImmutableArray<byte> ToImmutableArray(int start, int byteCount) { throw null; }
        public void WriteBoolean(bool value) { }
        public void WriteByte(byte value) { }
        public unsafe void WriteBytes(byte* buffer, int byteCount) { }
        public void WriteBytes(byte value, int byteCount) { }
        public void WriteBytes(byte[] buffer) { }
        public void WriteBytes(byte[] buffer, int start, int byteCount) { }
        public void WriteBytes(System.Collections.Immutable.ImmutableArray<byte> buffer) { }
        public void WriteBytes(System.Collections.Immutable.ImmutableArray<byte> buffer, int start, int byteCount) { }
        public int WriteBytes(System.IO.Stream source, int byteCount) { throw null; }
        public void WriteBytes(System.Reflection.Metadata.BlobBuilder source) { }
        public void WriteCompressedInteger(int value) { }
        public void WriteCompressedSignedInteger(int value) { }
        public void WriteConstant(object? value) { }
        public void WriteDateTime(System.DateTime value) { }
        public void WriteDecimal(decimal value) { }
        public void WriteDouble(double value) { }
        public void WriteGuid(System.Guid value) { }
        public void WriteInt16(short value) { }
        public void WriteInt16BE(short value) { }
        public void WriteInt32(int value) { }
        public void WriteInt32BE(int value) { }
        public void WriteInt64(long value) { }
        public void WriteReference(int reference, bool isSmall) { }
        public void WriteSByte(sbyte value) { }
        public void WriteSerializedString(string? str) { }
        public void WriteSingle(float value) { }
        public void WriteUInt16(ushort value) { }
        public void WriteUInt16BE(ushort value) { }
        public void WriteUInt32(uint value) { }
        public void WriteUInt32BE(uint value) { }
        public void WriteUInt64(ulong value) { }
        public void WriteUserString(string value) { }
        public void WriteUTF16(char[] value) { }
        public void WriteUTF16(string value) { }
        public void WriteUTF8(string value, bool allowUnpairedSurrogates) { }
    }
    public readonly partial struct Constant
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public System.Reflection.Metadata.EntityHandle Parent { get { throw null; } }
        public System.Reflection.Metadata.ConstantTypeCode TypeCode { get { throw null; } }
        public System.Reflection.Metadata.BlobHandle Value { get { throw null; } }
    }
    public readonly partial struct ConstantHandle : System.IEquatable<System.Reflection.Metadata.ConstantHandle>
    {
        private readonly int _dummyPrimitive;
        public bool IsNil { get { throw null; } }
        public override bool Equals(object? obj) { throw null; }
        public bool Equals(System.Reflection.Metadata.ConstantHandle other) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Reflection.Metadata.ConstantHandle left, System.Reflection.Metadata.ConstantHandle right) { throw null; }
        public static explicit operator System.Reflection.Metadata.ConstantHandle (System.Reflection.Metadata.EntityHandle handle) { throw null; }
        public static explicit operator System.Reflection.Metadata.ConstantHandle (System.Reflection.Metadata.Handle handle) { throw null; }
        public static implicit operator System.Reflection.Metadata.EntityHandle (System.Reflection.Metadata.ConstantHandle handle) { throw null; }
        public static implicit operator System.Reflection.Metadata.Handle (System.Reflection.Metadata.ConstantHandle handle) { throw null; }
        public static bool operator !=(System.Reflection.Metadata.ConstantHandle left, System.Reflection.Metadata.ConstantHandle right) { throw null; }
    }
    public enum ConstantTypeCode : byte
    {
        Invalid = (byte)0,
        Boolean = (byte)2,
        Char = (byte)3,
        SByte = (byte)4,
        Byte = (byte)5,
        Int16 = (byte)6,
        UInt16 = (byte)7,
        Int32 = (byte)8,
        UInt32 = (byte)9,
        Int64 = (byte)10,
        UInt64 = (byte)11,
        Single = (byte)12,
        Double = (byte)13,
        String = (byte)14,
        NullReference = (byte)18,
    }
    public readonly partial struct CustomAttribute
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public System.Reflection.Metadata.EntityHandle Constructor { get { throw null; } }
        public System.Reflection.Metadata.EntityHandle Parent { get { throw null; } }
        public System.Reflection.Metadata.BlobHandle Value { get { throw null; } }
        public System.Reflection.Metadata.CustomAttributeValue<TType> DecodeValue<TType>(System.Reflection.Metadata.ICustomAttributeTypeProvider<TType> provider) { throw null; }
    }
    public readonly partial struct CustomAttributeHandle : System.IEquatable<System.Reflection.Metadata.CustomAttributeHandle>
    {
        private readonly int _dummyPrimitive;
        public bool IsNil { get { throw null; } }
        public override bool Equals(object? obj) { throw null; }
        public bool Equals(System.Reflection.Metadata.CustomAttributeHandle other) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Reflection.Metadata.CustomAttributeHandle left, System.Reflection.Metadata.CustomAttributeHandle right) { throw null; }
        public static explicit operator System.Reflection.Metadata.CustomAttributeHandle (System.Reflection.Metadata.EntityHandle handle) { throw null; }
        public static explicit operator System.Reflection.Metadata.CustomAttributeHandle (System.Reflection.Metadata.Handle handle) { throw null; }
        public static implicit operator System.Reflection.Metadata.EntityHandle (System.Reflection.Metadata.CustomAttributeHandle handle) { throw null; }
        public static implicit operator System.Reflection.Metadata.Handle (System.Reflection.Metadata.CustomAttributeHandle handle) { throw null; }
        public static bool operator !=(System.Reflection.Metadata.CustomAttributeHandle left, System.Reflection.Metadata.CustomAttributeHandle right) { throw null; }
    }
    public readonly partial struct CustomAttributeHandleCollection : System.Collections.Generic.IEnumerable<System.Reflection.Metadata.CustomAttributeHandle>, System.Collections.Generic.IReadOnlyCollection<System.Reflection.Metadata.CustomAttributeHandle>, System.Collections.IEnumerable
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public int Count { get { throw null; } }
        public System.Reflection.Metadata.CustomAttributeHandleCollection.Enumerator GetEnumerator() { throw null; }
        System.Collections.Generic.IEnumerator<System.Reflection.Metadata.CustomAttributeHandle> System.Collections.Generic.IEnumerable<System.Reflection.Metadata.CustomAttributeHandle>.GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        public partial struct Enumerator : System.Collections.Generic.IEnumerator<System.Reflection.Metadata.CustomAttributeHandle>, System.Collections.IEnumerator, System.IDisposable
        {
            private object _dummy;
            private int _dummyPrimitive;
            public System.Reflection.Metadata.CustomAttributeHandle Current { get { throw null; } }
            object System.Collections.IEnumerator.Current { get { throw null; } }
            public bool MoveNext() { throw null; }
            void System.Collections.IEnumerator.Reset() { }
            void System.IDisposable.Dispose() { }
        }
    }
    public enum CustomAttributeNamedArgumentKind : byte
    {
        Field = (byte)83,
        Property = (byte)84,
    }
    public readonly partial struct CustomAttributeNamedArgument<TType>
    {
        private readonly TType _Type_k__BackingField;
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public CustomAttributeNamedArgument(string? name, System.Reflection.Metadata.CustomAttributeNamedArgumentKind kind, TType type, object? value) { throw null; }
        public System.Reflection.Metadata.CustomAttributeNamedArgumentKind Kind { get { throw null; } }
        public string? Name { get { throw null; } }
        public TType Type { get { throw null; } }
        public object? Value { get { throw null; } }
    }
    public readonly partial struct CustomAttributeTypedArgument<TType>
    {
        private readonly TType _Type_k__BackingField;
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public CustomAttributeTypedArgument(TType type, object? value) { throw null; }
        public TType Type { get { throw null; } }
        public object? Value { get { throw null; } }
    }
    public readonly partial struct CustomAttributeValue<TType>
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public CustomAttributeValue(System.Collections.Immutable.ImmutableArray<System.Reflection.Metadata.CustomAttributeTypedArgument<TType>> fixedArguments, System.Collections.Immutable.ImmutableArray<System.Reflection.Metadata.CustomAttributeNamedArgument<TType>> namedArguments) { throw null; }
        public System.Collections.Immutable.ImmutableArray<System.Reflection.Metadata.CustomAttributeTypedArgument<TType>> FixedArguments { get { throw null; } }
        public System.Collections.Immutable.ImmutableArray<System.Reflection.Metadata.CustomAttributeNamedArgument<TType>> NamedArguments { get { throw null; } }
    }
    public readonly partial struct CustomDebugInformation
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public System.Reflection.Metadata.GuidHandle Kind { get { throw null; } }
        public System.Reflection.Metadata.EntityHandle Parent { get { throw null; } }
        public System.Reflection.Metadata.BlobHandle Value { get { throw null; } }
    }
    public readonly partial struct CustomDebugInformationHandle : System.IEquatable<System.Reflection.Metadata.CustomDebugInformationHandle>
    {
        private readonly int _dummyPrimitive;
        public bool IsNil { get { throw null; } }
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] object? obj) { throw null; }
        public bool Equals(System.Reflection.Metadata.CustomDebugInformationHandle other) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Reflection.Metadata.CustomDebugInformationHandle left, System.Reflection.Metadata.CustomDebugInformationHandle right) { throw null; }
        public static explicit operator System.Reflection.Metadata.CustomDebugInformationHandle (System.Reflection.Metadata.EntityHandle handle) { throw null; }
        public static explicit operator System.Reflection.Metadata.CustomDebugInformationHandle (System.Reflection.Metadata.Handle handle) { throw null; }
        public static implicit operator System.Reflection.Metadata.EntityHandle (System.Reflection.Metadata.CustomDebugInformationHandle handle) { throw null; }
        public static implicit operator System.Reflection.Metadata.Handle (System.Reflection.Metadata.CustomDebugInformationHandle handle) { throw null; }
        public static bool operator !=(System.Reflection.Metadata.CustomDebugInformationHandle left, System.Reflection.Metadata.CustomDebugInformationHandle right) { throw null; }
    }
    public readonly partial struct CustomDebugInformationHandleCollection : System.Collections.Generic.IEnumerable<System.Reflection.Metadata.CustomDebugInformationHandle>, System.Collections.Generic.IReadOnlyCollection<System.Reflection.Metadata.CustomDebugInformationHandle>, System.Collections.IEnumerable
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public int Count { get { throw null; } }
        public System.Reflection.Metadata.CustomDebugInformationHandleCollection.Enumerator GetEnumerator() { throw null; }
        System.Collections.Generic.IEnumerator<System.Reflection.Metadata.CustomDebugInformationHandle> System.Collections.Generic.IEnumerable<System.Reflection.Metadata.CustomDebugInformationHandle>.GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        public partial struct Enumerator : System.Collections.Generic.IEnumerator<System.Reflection.Metadata.CustomDebugInformationHandle>, System.Collections.IEnumerator, System.IDisposable
        {
            private object _dummy;
            private int _dummyPrimitive;
            public System.Reflection.Metadata.CustomDebugInformationHandle Current { get { throw null; } }
            object System.Collections.IEnumerator.Current { get { throw null; } }
            public bool MoveNext() { throw null; }
            void System.Collections.IEnumerator.Reset() { }
            void System.IDisposable.Dispose() { }
        }
    }
    public sealed partial class DebugMetadataHeader
    {
        internal DebugMetadataHeader() { }
        public System.Reflection.Metadata.MethodDefinitionHandle EntryPoint { get { throw null; } }
        public System.Collections.Immutable.ImmutableArray<byte> Id { get { throw null; } }
        public int IdStartOffset { get { throw null; } }
    }
    public readonly partial struct DeclarativeSecurityAttribute
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public System.Reflection.DeclarativeSecurityAction Action { get { throw null; } }
        public System.Reflection.Metadata.EntityHandle Parent { get { throw null; } }
        public System.Reflection.Metadata.BlobHandle PermissionSet { get { throw null; } }
    }
    public readonly partial struct DeclarativeSecurityAttributeHandle : System.IEquatable<System.Reflection.Metadata.DeclarativeSecurityAttributeHandle>
    {
        private readonly int _dummyPrimitive;
        public bool IsNil { get { throw null; } }
        public override bool Equals(object? obj) { throw null; }
        public bool Equals(System.Reflection.Metadata.DeclarativeSecurityAttributeHandle other) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Reflection.Metadata.DeclarativeSecurityAttributeHandle left, System.Reflection.Metadata.DeclarativeSecurityAttributeHandle right) { throw null; }
        public static explicit operator System.Reflection.Metadata.DeclarativeSecurityAttributeHandle (System.Reflection.Metadata.EntityHandle handle) { throw null; }
        public static explicit operator System.Reflection.Metadata.DeclarativeSecurityAttributeHandle (System.Reflection.Metadata.Handle handle) { throw null; }
        public static implicit operator System.Reflection.Metadata.EntityHandle (System.Reflection.Metadata.DeclarativeSecurityAttributeHandle handle) { throw null; }
        public static implicit operator System.Reflection.Metadata.Handle (System.Reflection.Metadata.DeclarativeSecurityAttributeHandle handle) { throw null; }
        public static bool operator !=(System.Reflection.Metadata.DeclarativeSecurityAttributeHandle left, System.Reflection.Metadata.DeclarativeSecurityAttributeHandle right) { throw null; }
    }
    public readonly partial struct DeclarativeSecurityAttributeHandleCollection : System.Collections.Generic.IEnumerable<System.Reflection.Metadata.DeclarativeSecurityAttributeHandle>, System.Collections.Generic.IReadOnlyCollection<System.Reflection.Metadata.DeclarativeSecurityAttributeHandle>, System.Collections.IEnumerable
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public int Count { get { throw null; } }
        public System.Reflection.Metadata.DeclarativeSecurityAttributeHandleCollection.Enumerator GetEnumerator() { throw null; }
        System.Collections.Generic.IEnumerator<System.Reflection.Metadata.DeclarativeSecurityAttributeHandle> System.Collections.Generic.IEnumerable<System.Reflection.Metadata.DeclarativeSecurityAttributeHandle>.GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        public partial struct Enumerator : System.Collections.Generic.IEnumerator<System.Reflection.Metadata.DeclarativeSecurityAttributeHandle>, System.Collections.IEnumerator, System.IDisposable
        {
            private object _dummy;
            private int _dummyPrimitive;
            public System.Reflection.Metadata.DeclarativeSecurityAttributeHandle Current { get { throw null; } }
            object System.Collections.IEnumerator.Current { get { throw null; } }
            public bool MoveNext() { throw null; }
            void System.Collections.IEnumerator.Reset() { }
            void System.IDisposable.Dispose() { }
        }
    }
    public readonly partial struct Document
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public System.Reflection.Metadata.BlobHandle Hash { get { throw null; } }
        public System.Reflection.Metadata.GuidHandle HashAlgorithm { get { throw null; } }
        public System.Reflection.Metadata.GuidHandle Language { get { throw null; } }
        public System.Reflection.Metadata.DocumentNameBlobHandle Name { get { throw null; } }
    }
    public readonly partial struct DocumentHandle : System.IEquatable<System.Reflection.Metadata.DocumentHandle>
    {
        private readonly int _dummyPrimitive;
        public bool IsNil { get { throw null; } }
        public override bool Equals(object? obj) { throw null; }
        public bool Equals(System.Reflection.Metadata.DocumentHandle other) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Reflection.Metadata.DocumentHandle left, System.Reflection.Metadata.DocumentHandle right) { throw null; }
        public static explicit operator System.Reflection.Metadata.DocumentHandle (System.Reflection.Metadata.EntityHandle handle) { throw null; }
        public static explicit operator System.Reflection.Metadata.DocumentHandle (System.Reflection.Metadata.Handle handle) { throw null; }
        public static implicit operator System.Reflection.Metadata.EntityHandle (System.Reflection.Metadata.DocumentHandle handle) { throw null; }
        public static implicit operator System.Reflection.Metadata.Handle (System.Reflection.Metadata.DocumentHandle handle) { throw null; }
        public static bool operator !=(System.Reflection.Metadata.DocumentHandle left, System.Reflection.Metadata.DocumentHandle right) { throw null; }
    }
    public readonly partial struct DocumentHandleCollection : System.Collections.Generic.IEnumerable<System.Reflection.Metadata.DocumentHandle>, System.Collections.Generic.IReadOnlyCollection<System.Reflection.Metadata.DocumentHandle>, System.Collections.IEnumerable
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public int Count { get { throw null; } }
        public System.Reflection.Metadata.DocumentHandleCollection.Enumerator GetEnumerator() { throw null; }
        System.Collections.Generic.IEnumerator<System.Reflection.Metadata.DocumentHandle> System.Collections.Generic.IEnumerable<System.Reflection.Metadata.DocumentHandle>.GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        public partial struct Enumerator : System.Collections.Generic.IEnumerator<System.Reflection.Metadata.DocumentHandle>, System.Collections.IEnumerator, System.IDisposable
        {
            private object _dummy;
            private int _dummyPrimitive;
            public System.Reflection.Metadata.DocumentHandle Current { get { throw null; } }
            object System.Collections.IEnumerator.Current { get { throw null; } }
            public bool MoveNext() { throw null; }
            void System.Collections.IEnumerator.Reset() { }
            void System.IDisposable.Dispose() { }
        }
    }
    public readonly partial struct DocumentNameBlobHandle : System.IEquatable<System.Reflection.Metadata.DocumentNameBlobHandle>
    {
        private readonly int _dummyPrimitive;
        public bool IsNil { get { throw null; } }
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] object? obj) { throw null; }
        public bool Equals(System.Reflection.Metadata.DocumentNameBlobHandle other) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Reflection.Metadata.DocumentNameBlobHandle left, System.Reflection.Metadata.DocumentNameBlobHandle right) { throw null; }
        public static explicit operator System.Reflection.Metadata.DocumentNameBlobHandle (System.Reflection.Metadata.BlobHandle handle) { throw null; }
        public static implicit operator System.Reflection.Metadata.BlobHandle (System.Reflection.Metadata.DocumentNameBlobHandle handle) { throw null; }
        public static bool operator !=(System.Reflection.Metadata.DocumentNameBlobHandle left, System.Reflection.Metadata.DocumentNameBlobHandle right) { throw null; }
    }
    public readonly partial struct EntityHandle : System.IEquatable<System.Reflection.Metadata.EntityHandle>
    {
        private readonly int _dummyPrimitive;
        public static readonly System.Reflection.Metadata.AssemblyDefinitionHandle AssemblyDefinition;
        public static readonly System.Reflection.Metadata.ModuleDefinitionHandle ModuleDefinition;
        public bool IsNil { get { throw null; } }
        public System.Reflection.Metadata.HandleKind Kind { get { throw null; } }
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] object? obj) { throw null; }
        public bool Equals(System.Reflection.Metadata.EntityHandle other) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Reflection.Metadata.EntityHandle left, System.Reflection.Metadata.EntityHandle right) { throw null; }
        public static explicit operator System.Reflection.Metadata.EntityHandle (System.Reflection.Metadata.Handle handle) { throw null; }
        public static implicit operator System.Reflection.Metadata.Handle (System.Reflection.Metadata.EntityHandle handle) { throw null; }
        public static bool operator !=(System.Reflection.Metadata.EntityHandle left, System.Reflection.Metadata.EntityHandle right) { throw null; }
    }
    public readonly partial struct EventAccessors
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public System.Reflection.Metadata.MethodDefinitionHandle Adder { get { throw null; } }
        public System.Collections.Immutable.ImmutableArray<System.Reflection.Metadata.MethodDefinitionHandle> Others { get { throw null; } }
        public System.Reflection.Metadata.MethodDefinitionHandle Raiser { get { throw null; } }
        public System.Reflection.Metadata.MethodDefinitionHandle Remover { get { throw null; } }
    }
    public readonly partial struct EventDefinition
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public System.Reflection.EventAttributes Attributes { get { throw null; } }
        public System.Reflection.Metadata.StringHandle Name { get { throw null; } }
        public System.Reflection.Metadata.EntityHandle Type { get { throw null; } }
        public System.Reflection.Metadata.EventAccessors GetAccessors() { throw null; }
        public System.Reflection.Metadata.CustomAttributeHandleCollection GetCustomAttributes() { throw null; }
    }
    public readonly partial struct EventDefinitionHandle : System.IEquatable<System.Reflection.Metadata.EventDefinitionHandle>
    {
        private readonly int _dummyPrimitive;
        public bool IsNil { get { throw null; } }
        public override bool Equals(object? obj) { throw null; }
        public bool Equals(System.Reflection.Metadata.EventDefinitionHandle other) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Reflection.Metadata.EventDefinitionHandle left, System.Reflection.Metadata.EventDefinitionHandle right) { throw null; }
        public static explicit operator System.Reflection.Metadata.EventDefinitionHandle (System.Reflection.Metadata.EntityHandle handle) { throw null; }
        public static explicit operator System.Reflection.Metadata.EventDefinitionHandle (System.Reflection.Metadata.Handle handle) { throw null; }
        public static implicit operator System.Reflection.Metadata.EntityHandle (System.Reflection.Metadata.EventDefinitionHandle handle) { throw null; }
        public static implicit operator System.Reflection.Metadata.Handle (System.Reflection.Metadata.EventDefinitionHandle handle) { throw null; }
        public static bool operator !=(System.Reflection.Metadata.EventDefinitionHandle left, System.Reflection.Metadata.EventDefinitionHandle right) { throw null; }
    }
    public readonly partial struct EventDefinitionHandleCollection : System.Collections.Generic.IEnumerable<System.Reflection.Metadata.EventDefinitionHandle>, System.Collections.Generic.IReadOnlyCollection<System.Reflection.Metadata.EventDefinitionHandle>, System.Collections.IEnumerable
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public int Count { get { throw null; } }
        public System.Reflection.Metadata.EventDefinitionHandleCollection.Enumerator GetEnumerator() { throw null; }
        System.Collections.Generic.IEnumerator<System.Reflection.Metadata.EventDefinitionHandle> System.Collections.Generic.IEnumerable<System.Reflection.Metadata.EventDefinitionHandle>.GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        public partial struct Enumerator : System.Collections.Generic.IEnumerator<System.Reflection.Metadata.EventDefinitionHandle>, System.Collections.IEnumerator, System.IDisposable
        {
            private object _dummy;
            private int _dummyPrimitive;
            public System.Reflection.Metadata.EventDefinitionHandle Current { get { throw null; } }
            object System.Collections.IEnumerator.Current { get { throw null; } }
            public bool MoveNext() { throw null; }
            void System.Collections.IEnumerator.Reset() { }
            void System.IDisposable.Dispose() { }
        }
    }
    public readonly partial struct ExceptionRegion
    {
        private readonly int _dummyPrimitive;
        public System.Reflection.Metadata.EntityHandle CatchType { get { throw null; } }
        public int FilterOffset { get { throw null; } }
        public int HandlerLength { get { throw null; } }
        public int HandlerOffset { get { throw null; } }
        public System.Reflection.Metadata.ExceptionRegionKind Kind { get { throw null; } }
        public int TryLength { get { throw null; } }
        public int TryOffset { get { throw null; } }
    }
    public enum ExceptionRegionKind : ushort
    {
        Catch = (ushort)0,
        Filter = (ushort)1,
        Finally = (ushort)2,
        Fault = (ushort)4,
    }
    public readonly partial struct ExportedType
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public System.Reflection.TypeAttributes Attributes { get { throw null; } }
        public System.Reflection.Metadata.EntityHandle Implementation { get { throw null; } }
        public bool IsForwarder { get { throw null; } }
        public System.Reflection.Metadata.StringHandle Name { get { throw null; } }
        public System.Reflection.Metadata.StringHandle Namespace { get { throw null; } }
        public System.Reflection.Metadata.NamespaceDefinitionHandle NamespaceDefinition { get { throw null; } }
        public System.Reflection.Metadata.CustomAttributeHandleCollection GetCustomAttributes() { throw null; }
    }
    public readonly partial struct ExportedTypeHandle : System.IEquatable<System.Reflection.Metadata.ExportedTypeHandle>
    {
        private readonly int _dummyPrimitive;
        public bool IsNil { get { throw null; } }
        public override bool Equals(object? obj) { throw null; }
        public bool Equals(System.Reflection.Metadata.ExportedTypeHandle other) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Reflection.Metadata.ExportedTypeHandle left, System.Reflection.Metadata.ExportedTypeHandle right) { throw null; }
        public static explicit operator System.Reflection.Metadata.ExportedTypeHandle (System.Reflection.Metadata.EntityHandle handle) { throw null; }
        public static explicit operator System.Reflection.Metadata.ExportedTypeHandle (System.Reflection.Metadata.Handle handle) { throw null; }
        public static implicit operator System.Reflection.Metadata.EntityHandle (System.Reflection.Metadata.ExportedTypeHandle handle) { throw null; }
        public static implicit operator System.Reflection.Metadata.Handle (System.Reflection.Metadata.ExportedTypeHandle handle) { throw null; }
        public static bool operator !=(System.Reflection.Metadata.ExportedTypeHandle left, System.Reflection.Metadata.ExportedTypeHandle right) { throw null; }
    }
    public readonly partial struct ExportedTypeHandleCollection : System.Collections.Generic.IEnumerable<System.Reflection.Metadata.ExportedTypeHandle>, System.Collections.Generic.IReadOnlyCollection<System.Reflection.Metadata.ExportedTypeHandle>, System.Collections.IEnumerable
    {
        private readonly int _dummyPrimitive;
        public int Count { get { throw null; } }
        public System.Reflection.Metadata.ExportedTypeHandleCollection.Enumerator GetEnumerator() { throw null; }
        System.Collections.Generic.IEnumerator<System.Reflection.Metadata.ExportedTypeHandle> System.Collections.Generic.IEnumerable<System.Reflection.Metadata.ExportedTypeHandle>.GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        public partial struct Enumerator : System.Collections.Generic.IEnumerator<System.Reflection.Metadata.ExportedTypeHandle>, System.Collections.IEnumerator, System.IDisposable
        {
            private int _dummyPrimitive;
            public System.Reflection.Metadata.ExportedTypeHandle Current { get { throw null; } }
            object System.Collections.IEnumerator.Current { get { throw null; } }
            public bool MoveNext() { throw null; }
            void System.Collections.IEnumerator.Reset() { }
            void System.IDisposable.Dispose() { }
        }
    }
    public readonly partial struct FieldDefinition
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public System.Reflection.FieldAttributes Attributes { get { throw null; } }
        public System.Reflection.Metadata.StringHandle Name { get { throw null; } }
        public System.Reflection.Metadata.BlobHandle Signature { get { throw null; } }
        public TType DecodeSignature<TType, TGenericContext>(System.Reflection.Metadata.ISignatureTypeProvider<TType, TGenericContext> provider, TGenericContext genericContext) { throw null; }
        public System.Reflection.Metadata.CustomAttributeHandleCollection GetCustomAttributes() { throw null; }
        public System.Reflection.Metadata.TypeDefinitionHandle GetDeclaringType() { throw null; }
        public System.Reflection.Metadata.ConstantHandle GetDefaultValue() { throw null; }
        public System.Reflection.Metadata.BlobHandle GetMarshallingDescriptor() { throw null; }
        public int GetOffset() { throw null; }
        public int GetRelativeVirtualAddress() { throw null; }
    }
    public readonly partial struct FieldDefinitionHandle : System.IEquatable<System.Reflection.Metadata.FieldDefinitionHandle>
    {
        private readonly int _dummyPrimitive;
        public bool IsNil { get { throw null; } }
        public override bool Equals(object? obj) { throw null; }
        public bool Equals(System.Reflection.Metadata.FieldDefinitionHandle other) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Reflection.Metadata.FieldDefinitionHandle left, System.Reflection.Metadata.FieldDefinitionHandle right) { throw null; }
        public static explicit operator System.Reflection.Metadata.FieldDefinitionHandle (System.Reflection.Metadata.EntityHandle handle) { throw null; }
        public static explicit operator System.Reflection.Metadata.FieldDefinitionHandle (System.Reflection.Metadata.Handle handle) { throw null; }
        public static implicit operator System.Reflection.Metadata.EntityHandle (System.Reflection.Metadata.FieldDefinitionHandle handle) { throw null; }
        public static implicit operator System.Reflection.Metadata.Handle (System.Reflection.Metadata.FieldDefinitionHandle handle) { throw null; }
        public static bool operator !=(System.Reflection.Metadata.FieldDefinitionHandle left, System.Reflection.Metadata.FieldDefinitionHandle right) { throw null; }
    }
    public readonly partial struct FieldDefinitionHandleCollection : System.Collections.Generic.IEnumerable<System.Reflection.Metadata.FieldDefinitionHandle>, System.Collections.Generic.IReadOnlyCollection<System.Reflection.Metadata.FieldDefinitionHandle>, System.Collections.IEnumerable
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public int Count { get { throw null; } }
        public System.Reflection.Metadata.FieldDefinitionHandleCollection.Enumerator GetEnumerator() { throw null; }
        System.Collections.Generic.IEnumerator<System.Reflection.Metadata.FieldDefinitionHandle> System.Collections.Generic.IEnumerable<System.Reflection.Metadata.FieldDefinitionHandle>.GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        public partial struct Enumerator : System.Collections.Generic.IEnumerator<System.Reflection.Metadata.FieldDefinitionHandle>, System.Collections.IEnumerator, System.IDisposable
        {
            private object _dummy;
            private int _dummyPrimitive;
            public System.Reflection.Metadata.FieldDefinitionHandle Current { get { throw null; } }
            object System.Collections.IEnumerator.Current { get { throw null; } }
            public bool MoveNext() { throw null; }
            void System.Collections.IEnumerator.Reset() { }
            void System.IDisposable.Dispose() { }
        }
    }
    public readonly partial struct GenericParameter
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public System.Reflection.GenericParameterAttributes Attributes { get { throw null; } }
        public int Index { get { throw null; } }
        public System.Reflection.Metadata.StringHandle Name { get { throw null; } }
        public System.Reflection.Metadata.EntityHandle Parent { get { throw null; } }
        public System.Reflection.Metadata.GenericParameterConstraintHandleCollection GetConstraints() { throw null; }
        public System.Reflection.Metadata.CustomAttributeHandleCollection GetCustomAttributes() { throw null; }
    }
    public readonly partial struct GenericParameterConstraint
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public System.Reflection.Metadata.GenericParameterHandle Parameter { get { throw null; } }
        public System.Reflection.Metadata.EntityHandle Type { get { throw null; } }
        public System.Reflection.Metadata.CustomAttributeHandleCollection GetCustomAttributes() { throw null; }
    }
    public readonly partial struct GenericParameterConstraintHandle : System.IEquatable<System.Reflection.Metadata.GenericParameterConstraintHandle>
    {
        private readonly int _dummyPrimitive;
        public bool IsNil { get { throw null; } }
        public override bool Equals(object? obj) { throw null; }
        public bool Equals(System.Reflection.Metadata.GenericParameterConstraintHandle other) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Reflection.Metadata.GenericParameterConstraintHandle left, System.Reflection.Metadata.GenericParameterConstraintHandle right) { throw null; }
        public static explicit operator System.Reflection.Metadata.GenericParameterConstraintHandle (System.Reflection.Metadata.EntityHandle handle) { throw null; }
        public static explicit operator System.Reflection.Metadata.GenericParameterConstraintHandle (System.Reflection.Metadata.Handle handle) { throw null; }
        public static implicit operator System.Reflection.Metadata.EntityHandle (System.Reflection.Metadata.GenericParameterConstraintHandle handle) { throw null; }
        public static implicit operator System.Reflection.Metadata.Handle (System.Reflection.Metadata.GenericParameterConstraintHandle handle) { throw null; }
        public static bool operator !=(System.Reflection.Metadata.GenericParameterConstraintHandle left, System.Reflection.Metadata.GenericParameterConstraintHandle right) { throw null; }
    }
    public readonly partial struct GenericParameterConstraintHandleCollection : System.Collections.Generic.IEnumerable<System.Reflection.Metadata.GenericParameterConstraintHandle>, System.Collections.Generic.IReadOnlyCollection<System.Reflection.Metadata.GenericParameterConstraintHandle>, System.Collections.Generic.IReadOnlyList<System.Reflection.Metadata.GenericParameterConstraintHandle>, System.Collections.IEnumerable
    {
        private readonly int _dummyPrimitive;
        public int Count { get { throw null; } }
        public System.Reflection.Metadata.GenericParameterConstraintHandle this[int index] { get { throw null; } }
        public System.Reflection.Metadata.GenericParameterConstraintHandleCollection.Enumerator GetEnumerator() { throw null; }
        System.Collections.Generic.IEnumerator<System.Reflection.Metadata.GenericParameterConstraintHandle> System.Collections.Generic.IEnumerable<System.Reflection.Metadata.GenericParameterConstraintHandle>.GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        public partial struct Enumerator : System.Collections.Generic.IEnumerator<System.Reflection.Metadata.GenericParameterConstraintHandle>, System.Collections.IEnumerator, System.IDisposable
        {
            private int _dummyPrimitive;
            public System.Reflection.Metadata.GenericParameterConstraintHandle Current { get { throw null; } }
            object System.Collections.IEnumerator.Current { get { throw null; } }
            public bool MoveNext() { throw null; }
            void System.Collections.IEnumerator.Reset() { }
            void System.IDisposable.Dispose() { }
        }
    }
    public readonly partial struct GenericParameterHandle : System.IEquatable<System.Reflection.Metadata.GenericParameterHandle>
    {
        private readonly int _dummyPrimitive;
        public bool IsNil { get { throw null; } }
        public override bool Equals(object? obj) { throw null; }
        public bool Equals(System.Reflection.Metadata.GenericParameterHandle other) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Reflection.Metadata.GenericParameterHandle left, System.Reflection.Metadata.GenericParameterHandle right) { throw null; }
        public static explicit operator System.Reflection.Metadata.GenericParameterHandle (System.Reflection.Metadata.EntityHandle handle) { throw null; }
        public static explicit operator System.Reflection.Metadata.GenericParameterHandle (System.Reflection.Metadata.Handle handle) { throw null; }
        public static implicit operator System.Reflection.Metadata.EntityHandle (System.Reflection.Metadata.GenericParameterHandle handle) { throw null; }
        public static implicit operator System.Reflection.Metadata.Handle (System.Reflection.Metadata.GenericParameterHandle handle) { throw null; }
        public static bool operator !=(System.Reflection.Metadata.GenericParameterHandle left, System.Reflection.Metadata.GenericParameterHandle right) { throw null; }
    }
    public readonly partial struct GenericParameterHandleCollection : System.Collections.Generic.IEnumerable<System.Reflection.Metadata.GenericParameterHandle>, System.Collections.Generic.IReadOnlyCollection<System.Reflection.Metadata.GenericParameterHandle>, System.Collections.Generic.IReadOnlyList<System.Reflection.Metadata.GenericParameterHandle>, System.Collections.IEnumerable
    {
        private readonly int _dummyPrimitive;
        public int Count { get { throw null; } }
        public System.Reflection.Metadata.GenericParameterHandle this[int index] { get { throw null; } }
        public System.Reflection.Metadata.GenericParameterHandleCollection.Enumerator GetEnumerator() { throw null; }
        System.Collections.Generic.IEnumerator<System.Reflection.Metadata.GenericParameterHandle> System.Collections.Generic.IEnumerable<System.Reflection.Metadata.GenericParameterHandle>.GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        public partial struct Enumerator : System.Collections.Generic.IEnumerator<System.Reflection.Metadata.GenericParameterHandle>, System.Collections.IEnumerator, System.IDisposable
        {
            private int _dummyPrimitive;
            public System.Reflection.Metadata.GenericParameterHandle Current { get { throw null; } }
            object System.Collections.IEnumerator.Current { get { throw null; } }
            public bool MoveNext() { throw null; }
            void System.Collections.IEnumerator.Reset() { }
            void System.IDisposable.Dispose() { }
        }
    }
    public readonly partial struct GuidHandle : System.IEquatable<System.Reflection.Metadata.GuidHandle>
    {
        private readonly int _dummyPrimitive;
        public bool IsNil { get { throw null; } }
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] object? obj) { throw null; }
        public bool Equals(System.Reflection.Metadata.GuidHandle other) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Reflection.Metadata.GuidHandle left, System.Reflection.Metadata.GuidHandle right) { throw null; }
        public static explicit operator System.Reflection.Metadata.GuidHandle (System.Reflection.Metadata.Handle handle) { throw null; }
        public static implicit operator System.Reflection.Metadata.Handle (System.Reflection.Metadata.GuidHandle handle) { throw null; }
        public static bool operator !=(System.Reflection.Metadata.GuidHandle left, System.Reflection.Metadata.GuidHandle right) { throw null; }
    }
    public readonly partial struct Handle : System.IEquatable<System.Reflection.Metadata.Handle>
    {
        private readonly int _dummyPrimitive;
        public static readonly System.Reflection.Metadata.AssemblyDefinitionHandle AssemblyDefinition;
        public static readonly System.Reflection.Metadata.ModuleDefinitionHandle ModuleDefinition;
        public bool IsNil { get { throw null; } }
        public System.Reflection.Metadata.HandleKind Kind { get { throw null; } }
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] object? obj) { throw null; }
        public bool Equals(System.Reflection.Metadata.Handle other) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Reflection.Metadata.Handle left, System.Reflection.Metadata.Handle right) { throw null; }
        public static bool operator !=(System.Reflection.Metadata.Handle left, System.Reflection.Metadata.Handle right) { throw null; }
    }
    public sealed partial class HandleComparer : System.Collections.Generic.IComparer<System.Reflection.Metadata.EntityHandle>, System.Collections.Generic.IComparer<System.Reflection.Metadata.Handle>, System.Collections.Generic.IEqualityComparer<System.Reflection.Metadata.EntityHandle>, System.Collections.Generic.IEqualityComparer<System.Reflection.Metadata.Handle>
    {
        internal HandleComparer() { }
        public static System.Reflection.Metadata.HandleComparer Default { get { throw null; } }
        public int Compare(System.Reflection.Metadata.EntityHandle x, System.Reflection.Metadata.EntityHandle y) { throw null; }
        public int Compare(System.Reflection.Metadata.Handle x, System.Reflection.Metadata.Handle y) { throw null; }
        public bool Equals(System.Reflection.Metadata.EntityHandle x, System.Reflection.Metadata.EntityHandle y) { throw null; }
        public bool Equals(System.Reflection.Metadata.Handle x, System.Reflection.Metadata.Handle y) { throw null; }
        public int GetHashCode(System.Reflection.Metadata.EntityHandle obj) { throw null; }
        public int GetHashCode(System.Reflection.Metadata.Handle obj) { throw null; }
    }
    public enum HandleKind : byte
    {
        ModuleDefinition = (byte)0,
        TypeReference = (byte)1,
        TypeDefinition = (byte)2,
        FieldDefinition = (byte)4,
        MethodDefinition = (byte)6,
        Parameter = (byte)8,
        InterfaceImplementation = (byte)9,
        MemberReference = (byte)10,
        Constant = (byte)11,
        CustomAttribute = (byte)12,
        DeclarativeSecurityAttribute = (byte)14,
        StandaloneSignature = (byte)17,
        EventDefinition = (byte)20,
        PropertyDefinition = (byte)23,
        MethodImplementation = (byte)25,
        ModuleReference = (byte)26,
        TypeSpecification = (byte)27,
        AssemblyDefinition = (byte)32,
        AssemblyReference = (byte)35,
        AssemblyFile = (byte)38,
        ExportedType = (byte)39,
        ManifestResource = (byte)40,
        GenericParameter = (byte)42,
        MethodSpecification = (byte)43,
        GenericParameterConstraint = (byte)44,
        Document = (byte)48,
        MethodDebugInformation = (byte)49,
        LocalScope = (byte)50,
        LocalVariable = (byte)51,
        LocalConstant = (byte)52,
        ImportScope = (byte)53,
        CustomDebugInformation = (byte)55,
        UserString = (byte)112,
        Blob = (byte)113,
        Guid = (byte)114,
        String = (byte)120,
        NamespaceDefinition = (byte)124,
    }
    public partial interface IConstructedTypeProvider<TType> : System.Reflection.Metadata.ISZArrayTypeProvider<TType>
    {
        TType GetArrayType(TType elementType, System.Reflection.Metadata.ArrayShape shape);
        TType GetByReferenceType(TType elementType);
        TType GetGenericInstantiation(TType genericType, System.Collections.Immutable.ImmutableArray<TType> typeArguments);
        TType GetPointerType(TType elementType);
    }
    public partial interface ICustomAttributeTypeProvider<TType> : System.Reflection.Metadata.ISimpleTypeProvider<TType>, System.Reflection.Metadata.ISZArrayTypeProvider<TType>
    {
        TType GetSystemType();
        TType GetTypeFromSerializedName(string name);
        System.Reflection.Metadata.PrimitiveTypeCode GetUnderlyingEnumType(TType type);
        bool IsSystemType(TType type);
    }
    public enum ILOpCode : ushort
    {
        Nop = (ushort)0,
        Break = (ushort)1,
        Ldarg_0 = (ushort)2,
        Ldarg_1 = (ushort)3,
        Ldarg_2 = (ushort)4,
        Ldarg_3 = (ushort)5,
        Ldloc_0 = (ushort)6,
        Ldloc_1 = (ushort)7,
        Ldloc_2 = (ushort)8,
        Ldloc_3 = (ushort)9,
        Stloc_0 = (ushort)10,
        Stloc_1 = (ushort)11,
        Stloc_2 = (ushort)12,
        Stloc_3 = (ushort)13,
        Ldarg_s = (ushort)14,
        Ldarga_s = (ushort)15,
        Starg_s = (ushort)16,
        Ldloc_s = (ushort)17,
        Ldloca_s = (ushort)18,
        Stloc_s = (ushort)19,
        Ldnull = (ushort)20,
        Ldc_i4_m1 = (ushort)21,
        Ldc_i4_0 = (ushort)22,
        Ldc_i4_1 = (ushort)23,
        Ldc_i4_2 = (ushort)24,
        Ldc_i4_3 = (ushort)25,
        Ldc_i4_4 = (ushort)26,
        Ldc_i4_5 = (ushort)27,
        Ldc_i4_6 = (ushort)28,
        Ldc_i4_7 = (ushort)29,
        Ldc_i4_8 = (ushort)30,
        Ldc_i4_s = (ushort)31,
        Ldc_i4 = (ushort)32,
        Ldc_i8 = (ushort)33,
        Ldc_r4 = (ushort)34,
        Ldc_r8 = (ushort)35,
        Dup = (ushort)37,
        Pop = (ushort)38,
        Jmp = (ushort)39,
        Call = (ushort)40,
        Calli = (ushort)41,
        Ret = (ushort)42,
        Br_s = (ushort)43,
        Brfalse_s = (ushort)44,
        Brtrue_s = (ushort)45,
        Beq_s = (ushort)46,
        Bge_s = (ushort)47,
        Bgt_s = (ushort)48,
        Ble_s = (ushort)49,
        Blt_s = (ushort)50,
        Bne_un_s = (ushort)51,
        Bge_un_s = (ushort)52,
        Bgt_un_s = (ushort)53,
        Ble_un_s = (ushort)54,
        Blt_un_s = (ushort)55,
        Br = (ushort)56,
        Brfalse = (ushort)57,
        Brtrue = (ushort)58,
        Beq = (ushort)59,
        Bge = (ushort)60,
        Bgt = (ushort)61,
        Ble = (ushort)62,
        Blt = (ushort)63,
        Bne_un = (ushort)64,
        Bge_un = (ushort)65,
        Bgt_un = (ushort)66,
        Ble_un = (ushort)67,
        Blt_un = (ushort)68,
        Switch = (ushort)69,
        Ldind_i1 = (ushort)70,
        Ldind_u1 = (ushort)71,
        Ldind_i2 = (ushort)72,
        Ldind_u2 = (ushort)73,
        Ldind_i4 = (ushort)74,
        Ldind_u4 = (ushort)75,
        Ldind_i8 = (ushort)76,
        Ldind_i = (ushort)77,
        Ldind_r4 = (ushort)78,
        Ldind_r8 = (ushort)79,
        Ldind_ref = (ushort)80,
        Stind_ref = (ushort)81,
        Stind_i1 = (ushort)82,
        Stind_i2 = (ushort)83,
        Stind_i4 = (ushort)84,
        Stind_i8 = (ushort)85,
        Stind_r4 = (ushort)86,
        Stind_r8 = (ushort)87,
        Add = (ushort)88,
        Sub = (ushort)89,
        Mul = (ushort)90,
        Div = (ushort)91,
        Div_un = (ushort)92,
        Rem = (ushort)93,
        Rem_un = (ushort)94,
        And = (ushort)95,
        Or = (ushort)96,
        Xor = (ushort)97,
        Shl = (ushort)98,
        Shr = (ushort)99,
        Shr_un = (ushort)100,
        Neg = (ushort)101,
        Not = (ushort)102,
        Conv_i1 = (ushort)103,
        Conv_i2 = (ushort)104,
        Conv_i4 = (ushort)105,
        Conv_i8 = (ushort)106,
        Conv_r4 = (ushort)107,
        Conv_r8 = (ushort)108,
        Conv_u4 = (ushort)109,
        Conv_u8 = (ushort)110,
        Callvirt = (ushort)111,
        Cpobj = (ushort)112,
        Ldobj = (ushort)113,
        Ldstr = (ushort)114,
        Newobj = (ushort)115,
        Castclass = (ushort)116,
        Isinst = (ushort)117,
        Conv_r_un = (ushort)118,
        Unbox = (ushort)121,
        Throw = (ushort)122,
        Ldfld = (ushort)123,
        Ldflda = (ushort)124,
        Stfld = (ushort)125,
        Ldsfld = (ushort)126,
        Ldsflda = (ushort)127,
        Stsfld = (ushort)128,
        Stobj = (ushort)129,
        Conv_ovf_i1_un = (ushort)130,
        Conv_ovf_i2_un = (ushort)131,
        Conv_ovf_i4_un = (ushort)132,
        Conv_ovf_i8_un = (ushort)133,
        Conv_ovf_u1_un = (ushort)134,
        Conv_ovf_u2_un = (ushort)135,
        Conv_ovf_u4_un = (ushort)136,
        Conv_ovf_u8_un = (ushort)137,
        Conv_ovf_i_un = (ushort)138,
        Conv_ovf_u_un = (ushort)139,
        Box = (ushort)140,
        Newarr = (ushort)141,
        Ldlen = (ushort)142,
        Ldelema = (ushort)143,
        Ldelem_i1 = (ushort)144,
        Ldelem_u1 = (ushort)145,
        Ldelem_i2 = (ushort)146,
        Ldelem_u2 = (ushort)147,
        Ldelem_i4 = (ushort)148,
        Ldelem_u4 = (ushort)149,
        Ldelem_i8 = (ushort)150,
        Ldelem_i = (ushort)151,
        Ldelem_r4 = (ushort)152,
        Ldelem_r8 = (ushort)153,
        Ldelem_ref = (ushort)154,
        Stelem_i = (ushort)155,
        Stelem_i1 = (ushort)156,
        Stelem_i2 = (ushort)157,
        Stelem_i4 = (ushort)158,
        Stelem_i8 = (ushort)159,
        Stelem_r4 = (ushort)160,
        Stelem_r8 = (ushort)161,
        Stelem_ref = (ushort)162,
        Ldelem = (ushort)163,
        Stelem = (ushort)164,
        Unbox_any = (ushort)165,
        Conv_ovf_i1 = (ushort)179,
        Conv_ovf_u1 = (ushort)180,
        Conv_ovf_i2 = (ushort)181,
        Conv_ovf_u2 = (ushort)182,
        Conv_ovf_i4 = (ushort)183,
        Conv_ovf_u4 = (ushort)184,
        Conv_ovf_i8 = (ushort)185,
        Conv_ovf_u8 = (ushort)186,
        Refanyval = (ushort)194,
        Ckfinite = (ushort)195,
        Mkrefany = (ushort)198,
        Ldtoken = (ushort)208,
        Conv_u2 = (ushort)209,
        Conv_u1 = (ushort)210,
        Conv_i = (ushort)211,
        Conv_ovf_i = (ushort)212,
        Conv_ovf_u = (ushort)213,
        Add_ovf = (ushort)214,
        Add_ovf_un = (ushort)215,
        Mul_ovf = (ushort)216,
        Mul_ovf_un = (ushort)217,
        Sub_ovf = (ushort)218,
        Sub_ovf_un = (ushort)219,
        Endfinally = (ushort)220,
        Leave = (ushort)221,
        Leave_s = (ushort)222,
        Stind_i = (ushort)223,
        Conv_u = (ushort)224,
        Arglist = (ushort)65024,
        Ceq = (ushort)65025,
        Cgt = (ushort)65026,
        Cgt_un = (ushort)65027,
        Clt = (ushort)65028,
        Clt_un = (ushort)65029,
        Ldftn = (ushort)65030,
        Ldvirtftn = (ushort)65031,
        Ldarg = (ushort)65033,
        Ldarga = (ushort)65034,
        Starg = (ushort)65035,
        Ldloc = (ushort)65036,
        Ldloca = (ushort)65037,
        Stloc = (ushort)65038,
        Localloc = (ushort)65039,
        Endfilter = (ushort)65041,
        Unaligned = (ushort)65042,
        Volatile = (ushort)65043,
        Tail = (ushort)65044,
        Initobj = (ushort)65045,
        Constrained = (ushort)65046,
        Cpblk = (ushort)65047,
        Initblk = (ushort)65048,
        Rethrow = (ushort)65050,
        Sizeof = (ushort)65052,
        Refanytype = (ushort)65053,
        Readonly = (ushort)65054,
    }
    public static partial class ILOpCodeExtensions
    {
        public static int GetBranchOperandSize(this System.Reflection.Metadata.ILOpCode opCode) { throw null; }
        public static System.Reflection.Metadata.ILOpCode GetLongBranch(this System.Reflection.Metadata.ILOpCode opCode) { throw null; }
        public static System.Reflection.Metadata.ILOpCode GetShortBranch(this System.Reflection.Metadata.ILOpCode opCode) { throw null; }
        public static bool IsBranch(this System.Reflection.Metadata.ILOpCode opCode) { throw null; }
    }
    public partial class ImageFormatLimitationException : System.Exception
    {
        public ImageFormatLimitationException() { }
        public ImageFormatLimitationException(string? message) { }
        public ImageFormatLimitationException(string? message, System.Exception? innerException) { }
    }
    public readonly partial struct ImportDefinition
    {
        private readonly int _dummyPrimitive;
        public System.Reflection.Metadata.BlobHandle Alias { get { throw null; } }
        public System.Reflection.Metadata.ImportDefinitionKind Kind { get { throw null; } }
        public System.Reflection.Metadata.AssemblyReferenceHandle TargetAssembly { get { throw null; } }
        public System.Reflection.Metadata.BlobHandle TargetNamespace { get { throw null; } }
        public System.Reflection.Metadata.EntityHandle TargetType { get { throw null; } }
    }
    public readonly partial struct ImportDefinitionCollection : System.Collections.Generic.IEnumerable<System.Reflection.Metadata.ImportDefinition>, System.Collections.IEnumerable
    {
        private readonly int _dummyPrimitive;
        public System.Reflection.Metadata.ImportDefinitionCollection.Enumerator GetEnumerator() { throw null; }
        System.Collections.Generic.IEnumerator<System.Reflection.Metadata.ImportDefinition> System.Collections.Generic.IEnumerable<System.Reflection.Metadata.ImportDefinition>.GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        public partial struct Enumerator : System.Collections.Generic.IEnumerator<System.Reflection.Metadata.ImportDefinition>, System.Collections.IEnumerator, System.IDisposable
        {
            private int _dummyPrimitive;
            public System.Reflection.Metadata.ImportDefinition Current { get { throw null; } }
            object System.Collections.IEnumerator.Current { get { throw null; } }
            public bool MoveNext() { throw null; }
            public void Reset() { }
            void System.IDisposable.Dispose() { }
        }
    }
    public enum ImportDefinitionKind
    {
        ImportNamespace = 1,
        ImportAssemblyNamespace = 2,
        ImportType = 3,
        ImportXmlNamespace = 4,
        ImportAssemblyReferenceAlias = 5,
        AliasAssemblyReference = 6,
        AliasNamespace = 7,
        AliasAssemblyNamespace = 8,
        AliasType = 9,
    }
    public readonly partial struct ImportScope
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public System.Reflection.Metadata.BlobHandle ImportsBlob { get { throw null; } }
        public System.Reflection.Metadata.ImportScopeHandle Parent { get { throw null; } }
        public System.Reflection.Metadata.ImportDefinitionCollection GetImports() { throw null; }
    }
    public readonly partial struct ImportScopeCollection : System.Collections.Generic.IEnumerable<System.Reflection.Metadata.ImportScopeHandle>, System.Collections.Generic.IReadOnlyCollection<System.Reflection.Metadata.ImportScopeHandle>, System.Collections.IEnumerable
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public int Count { get { throw null; } }
        public System.Reflection.Metadata.ImportScopeCollection.Enumerator GetEnumerator() { throw null; }
        System.Collections.Generic.IEnumerator<System.Reflection.Metadata.ImportScopeHandle> System.Collections.Generic.IEnumerable<System.Reflection.Metadata.ImportScopeHandle>.GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        public partial struct Enumerator : System.Collections.Generic.IEnumerator<System.Reflection.Metadata.ImportScopeHandle>, System.Collections.IEnumerator, System.IDisposable
        {
            private object _dummy;
            private int _dummyPrimitive;
            public System.Reflection.Metadata.ImportScopeHandle Current { get { throw null; } }
            object System.Collections.IEnumerator.Current { get { throw null; } }
            public bool MoveNext() { throw null; }
            void System.Collections.IEnumerator.Reset() { }
            void System.IDisposable.Dispose() { }
        }
    }
    public readonly partial struct ImportScopeHandle : System.IEquatable<System.Reflection.Metadata.ImportScopeHandle>
    {
        private readonly int _dummyPrimitive;
        public bool IsNil { get { throw null; } }
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] object? obj) { throw null; }
        public bool Equals(System.Reflection.Metadata.ImportScopeHandle other) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Reflection.Metadata.ImportScopeHandle left, System.Reflection.Metadata.ImportScopeHandle right) { throw null; }
        public static explicit operator System.Reflection.Metadata.ImportScopeHandle (System.Reflection.Metadata.EntityHandle handle) { throw null; }
        public static explicit operator System.Reflection.Metadata.ImportScopeHandle (System.Reflection.Metadata.Handle handle) { throw null; }
        public static implicit operator System.Reflection.Metadata.EntityHandle (System.Reflection.Metadata.ImportScopeHandle handle) { throw null; }
        public static implicit operator System.Reflection.Metadata.Handle (System.Reflection.Metadata.ImportScopeHandle handle) { throw null; }
        public static bool operator !=(System.Reflection.Metadata.ImportScopeHandle left, System.Reflection.Metadata.ImportScopeHandle right) { throw null; }
    }
    public readonly partial struct InterfaceImplementation
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public System.Reflection.Metadata.EntityHandle Interface { get { throw null; } }
        public System.Reflection.Metadata.CustomAttributeHandleCollection GetCustomAttributes() { throw null; }
    }
    public readonly partial struct InterfaceImplementationHandle : System.IEquatable<System.Reflection.Metadata.InterfaceImplementationHandle>
    {
        private readonly int _dummyPrimitive;
        public bool IsNil { get { throw null; } }
        public override bool Equals(object? obj) { throw null; }
        public bool Equals(System.Reflection.Metadata.InterfaceImplementationHandle other) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Reflection.Metadata.InterfaceImplementationHandle left, System.Reflection.Metadata.InterfaceImplementationHandle right) { throw null; }
        public static explicit operator System.Reflection.Metadata.InterfaceImplementationHandle (System.Reflection.Metadata.EntityHandle handle) { throw null; }
        public static explicit operator System.Reflection.Metadata.InterfaceImplementationHandle (System.Reflection.Metadata.Handle handle) { throw null; }
        public static implicit operator System.Reflection.Metadata.EntityHandle (System.Reflection.Metadata.InterfaceImplementationHandle handle) { throw null; }
        public static implicit operator System.Reflection.Metadata.Handle (System.Reflection.Metadata.InterfaceImplementationHandle handle) { throw null; }
        public static bool operator !=(System.Reflection.Metadata.InterfaceImplementationHandle left, System.Reflection.Metadata.InterfaceImplementationHandle right) { throw null; }
    }
    public readonly partial struct InterfaceImplementationHandleCollection : System.Collections.Generic.IEnumerable<System.Reflection.Metadata.InterfaceImplementationHandle>, System.Collections.Generic.IReadOnlyCollection<System.Reflection.Metadata.InterfaceImplementationHandle>, System.Collections.IEnumerable
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public int Count { get { throw null; } }
        public System.Reflection.Metadata.InterfaceImplementationHandleCollection.Enumerator GetEnumerator() { throw null; }
        System.Collections.Generic.IEnumerator<System.Reflection.Metadata.InterfaceImplementationHandle> System.Collections.Generic.IEnumerable<System.Reflection.Metadata.InterfaceImplementationHandle>.GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        public partial struct Enumerator : System.Collections.Generic.IEnumerator<System.Reflection.Metadata.InterfaceImplementationHandle>, System.Collections.IEnumerator, System.IDisposable
        {
            private object _dummy;
            private int _dummyPrimitive;
            public System.Reflection.Metadata.InterfaceImplementationHandle Current { get { throw null; } }
            object System.Collections.IEnumerator.Current { get { throw null; } }
            public bool MoveNext() { throw null; }
            void System.Collections.IEnumerator.Reset() { }
            void System.IDisposable.Dispose() { }
        }
    }
    public partial interface ISignatureTypeProvider<TType, TGenericContext> : System.Reflection.Metadata.IConstructedTypeProvider<TType>, System.Reflection.Metadata.ISimpleTypeProvider<TType>, System.Reflection.Metadata.ISZArrayTypeProvider<TType>
    {
        TType GetFunctionPointerType(System.Reflection.Metadata.MethodSignature<TType> signature);
        TType GetGenericMethodParameter(TGenericContext genericContext, int index);
        TType GetGenericTypeParameter(TGenericContext genericContext, int index);
        TType GetModifiedType(TType modifier, TType unmodifiedType, bool isRequired);
        TType GetPinnedType(TType elementType);
        TType GetTypeFromSpecification(System.Reflection.Metadata.MetadataReader reader, TGenericContext genericContext, System.Reflection.Metadata.TypeSpecificationHandle handle, byte rawTypeKind);
    }
    public partial interface ISimpleTypeProvider<TType>
    {
        TType GetPrimitiveType(System.Reflection.Metadata.PrimitiveTypeCode typeCode);
        TType GetTypeFromDefinition(System.Reflection.Metadata.MetadataReader reader, System.Reflection.Metadata.TypeDefinitionHandle handle, byte rawTypeKind);
        TType GetTypeFromReference(System.Reflection.Metadata.MetadataReader reader, System.Reflection.Metadata.TypeReferenceHandle handle, byte rawTypeKind);
    }
    public partial interface ISZArrayTypeProvider<TType>
    {
        TType GetSZArrayType(TType elementType);
    }
    public readonly partial struct LocalConstant
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public System.Reflection.Metadata.StringHandle Name { get { throw null; } }
        public System.Reflection.Metadata.BlobHandle Signature { get { throw null; } }
    }
    public readonly partial struct LocalConstantHandle : System.IEquatable<System.Reflection.Metadata.LocalConstantHandle>
    {
        private readonly int _dummyPrimitive;
        public bool IsNil { get { throw null; } }
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] object? obj) { throw null; }
        public bool Equals(System.Reflection.Metadata.LocalConstantHandle other) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Reflection.Metadata.LocalConstantHandle left, System.Reflection.Metadata.LocalConstantHandle right) { throw null; }
        public static explicit operator System.Reflection.Metadata.LocalConstantHandle (System.Reflection.Metadata.EntityHandle handle) { throw null; }
        public static explicit operator System.Reflection.Metadata.LocalConstantHandle (System.Reflection.Metadata.Handle handle) { throw null; }
        public static implicit operator System.Reflection.Metadata.EntityHandle (System.Reflection.Metadata.LocalConstantHandle handle) { throw null; }
        public static implicit operator System.Reflection.Metadata.Handle (System.Reflection.Metadata.LocalConstantHandle handle) { throw null; }
        public static bool operator !=(System.Reflection.Metadata.LocalConstantHandle left, System.Reflection.Metadata.LocalConstantHandle right) { throw null; }
    }
    public readonly partial struct LocalConstantHandleCollection : System.Collections.Generic.IEnumerable<System.Reflection.Metadata.LocalConstantHandle>, System.Collections.Generic.IReadOnlyCollection<System.Reflection.Metadata.LocalConstantHandle>, System.Collections.IEnumerable
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public int Count { get { throw null; } }
        public System.Reflection.Metadata.LocalConstantHandleCollection.Enumerator GetEnumerator() { throw null; }
        System.Collections.Generic.IEnumerator<System.Reflection.Metadata.LocalConstantHandle> System.Collections.Generic.IEnumerable<System.Reflection.Metadata.LocalConstantHandle>.GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        public partial struct Enumerator : System.Collections.Generic.IEnumerator<System.Reflection.Metadata.LocalConstantHandle>, System.Collections.IEnumerator, System.IDisposable
        {
            private object _dummy;
            private int _dummyPrimitive;
            public System.Reflection.Metadata.LocalConstantHandle Current { get { throw null; } }
            object System.Collections.IEnumerator.Current { get { throw null; } }
            public bool MoveNext() { throw null; }
            void System.Collections.IEnumerator.Reset() { }
            void System.IDisposable.Dispose() { }
        }
    }
    public readonly partial struct LocalScope
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public int EndOffset { get { throw null; } }
        public System.Reflection.Metadata.ImportScopeHandle ImportScope { get { throw null; } }
        public int Length { get { throw null; } }
        public System.Reflection.Metadata.MethodDefinitionHandle Method { get { throw null; } }
        public int StartOffset { get { throw null; } }
        public System.Reflection.Metadata.LocalScopeHandleCollection.ChildrenEnumerator GetChildren() { throw null; }
        public System.Reflection.Metadata.LocalConstantHandleCollection GetLocalConstants() { throw null; }
        public System.Reflection.Metadata.LocalVariableHandleCollection GetLocalVariables() { throw null; }
    }
    public readonly partial struct LocalScopeHandle : System.IEquatable<System.Reflection.Metadata.LocalScopeHandle>
    {
        private readonly int _dummyPrimitive;
        public bool IsNil { get { throw null; } }
        public override bool Equals(object? obj) { throw null; }
        public bool Equals(System.Reflection.Metadata.LocalScopeHandle other) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Reflection.Metadata.LocalScopeHandle left, System.Reflection.Metadata.LocalScopeHandle right) { throw null; }
        public static explicit operator System.Reflection.Metadata.LocalScopeHandle (System.Reflection.Metadata.EntityHandle handle) { throw null; }
        public static explicit operator System.Reflection.Metadata.LocalScopeHandle (System.Reflection.Metadata.Handle handle) { throw null; }
        public static implicit operator System.Reflection.Metadata.EntityHandle (System.Reflection.Metadata.LocalScopeHandle handle) { throw null; }
        public static implicit operator System.Reflection.Metadata.Handle (System.Reflection.Metadata.LocalScopeHandle handle) { throw null; }
        public static bool operator !=(System.Reflection.Metadata.LocalScopeHandle left, System.Reflection.Metadata.LocalScopeHandle right) { throw null; }
    }
    public readonly partial struct LocalScopeHandleCollection : System.Collections.Generic.IEnumerable<System.Reflection.Metadata.LocalScopeHandle>, System.Collections.Generic.IReadOnlyCollection<System.Reflection.Metadata.LocalScopeHandle>, System.Collections.IEnumerable
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public int Count { get { throw null; } }
        public System.Reflection.Metadata.LocalScopeHandleCollection.Enumerator GetEnumerator() { throw null; }
        System.Collections.Generic.IEnumerator<System.Reflection.Metadata.LocalScopeHandle> System.Collections.Generic.IEnumerable<System.Reflection.Metadata.LocalScopeHandle>.GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        public partial struct ChildrenEnumerator : System.Collections.Generic.IEnumerator<System.Reflection.Metadata.LocalScopeHandle>, System.Collections.IEnumerator, System.IDisposable
        {
            private object _dummy;
            private int _dummyPrimitive;
            public System.Reflection.Metadata.LocalScopeHandle Current { get { throw null; } }
            object System.Collections.IEnumerator.Current { get { throw null; } }
            public bool MoveNext() { throw null; }
            void System.Collections.IEnumerator.Reset() { }
            void System.IDisposable.Dispose() { }
        }
        public partial struct Enumerator : System.Collections.Generic.IEnumerator<System.Reflection.Metadata.LocalScopeHandle>, System.Collections.IEnumerator, System.IDisposable
        {
            private object _dummy;
            private int _dummyPrimitive;
            public System.Reflection.Metadata.LocalScopeHandle Current { get { throw null; } }
            object System.Collections.IEnumerator.Current { get { throw null; } }
            public bool MoveNext() { throw null; }
            void System.Collections.IEnumerator.Reset() { }
            void System.IDisposable.Dispose() { }
        }
    }
    public readonly partial struct LocalVariable
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public System.Reflection.Metadata.LocalVariableAttributes Attributes { get { throw null; } }
        public int Index { get { throw null; } }
        public System.Reflection.Metadata.StringHandle Name { get { throw null; } }
    }
    [System.FlagsAttribute]
    public enum LocalVariableAttributes
    {
        None = 0,
        DebuggerHidden = 1,
    }
    public readonly partial struct LocalVariableHandle : System.IEquatable<System.Reflection.Metadata.LocalVariableHandle>
    {
        private readonly int _dummyPrimitive;
        public bool IsNil { get { throw null; } }
        public override bool Equals(object? obj) { throw null; }
        public bool Equals(System.Reflection.Metadata.LocalVariableHandle other) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Reflection.Metadata.LocalVariableHandle left, System.Reflection.Metadata.LocalVariableHandle right) { throw null; }
        public static explicit operator System.Reflection.Metadata.LocalVariableHandle (System.Reflection.Metadata.EntityHandle handle) { throw null; }
        public static explicit operator System.Reflection.Metadata.LocalVariableHandle (System.Reflection.Metadata.Handle handle) { throw null; }
        public static implicit operator System.Reflection.Metadata.EntityHandle (System.Reflection.Metadata.LocalVariableHandle handle) { throw null; }
        public static implicit operator System.Reflection.Metadata.Handle (System.Reflection.Metadata.LocalVariableHandle handle) { throw null; }
        public static bool operator !=(System.Reflection.Metadata.LocalVariableHandle left, System.Reflection.Metadata.LocalVariableHandle right) { throw null; }
    }
    public readonly partial struct LocalVariableHandleCollection : System.Collections.Generic.IEnumerable<System.Reflection.Metadata.LocalVariableHandle>, System.Collections.Generic.IReadOnlyCollection<System.Reflection.Metadata.LocalVariableHandle>, System.Collections.IEnumerable
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public int Count { get { throw null; } }
        public System.Reflection.Metadata.LocalVariableHandleCollection.Enumerator GetEnumerator() { throw null; }
        System.Collections.Generic.IEnumerator<System.Reflection.Metadata.LocalVariableHandle> System.Collections.Generic.IEnumerable<System.Reflection.Metadata.LocalVariableHandle>.GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        public partial struct Enumerator : System.Collections.Generic.IEnumerator<System.Reflection.Metadata.LocalVariableHandle>, System.Collections.IEnumerator, System.IDisposable
        {
            private object _dummy;
            private int _dummyPrimitive;
            public System.Reflection.Metadata.LocalVariableHandle Current { get { throw null; } }
            object System.Collections.IEnumerator.Current { get { throw null; } }
            public bool MoveNext() { throw null; }
            void System.Collections.IEnumerator.Reset() { }
            void System.IDisposable.Dispose() { }
        }
    }
    public readonly partial struct ManifestResource
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public System.Reflection.ManifestResourceAttributes Attributes { get { throw null; } }
        public System.Reflection.Metadata.EntityHandle Implementation { get { throw null; } }
        public System.Reflection.Metadata.StringHandle Name { get { throw null; } }
        public long Offset { get { throw null; } }
        public System.Reflection.Metadata.CustomAttributeHandleCollection GetCustomAttributes() { throw null; }
    }
    public readonly partial struct ManifestResourceHandle : System.IEquatable<System.Reflection.Metadata.ManifestResourceHandle>
    {
        private readonly int _dummyPrimitive;
        public bool IsNil { get { throw null; } }
        public override bool Equals(object? obj) { throw null; }
        public bool Equals(System.Reflection.Metadata.ManifestResourceHandle other) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Reflection.Metadata.ManifestResourceHandle left, System.Reflection.Metadata.ManifestResourceHandle right) { throw null; }
        public static explicit operator System.Reflection.Metadata.ManifestResourceHandle (System.Reflection.Metadata.EntityHandle handle) { throw null; }
        public static explicit operator System.Reflection.Metadata.ManifestResourceHandle (System.Reflection.Metadata.Handle handle) { throw null; }
        public static implicit operator System.Reflection.Metadata.EntityHandle (System.Reflection.Metadata.ManifestResourceHandle handle) { throw null; }
        public static implicit operator System.Reflection.Metadata.Handle (System.Reflection.Metadata.ManifestResourceHandle handle) { throw null; }
        public static bool operator !=(System.Reflection.Metadata.ManifestResourceHandle left, System.Reflection.Metadata.ManifestResourceHandle right) { throw null; }
    }
    public readonly partial struct ManifestResourceHandleCollection : System.Collections.Generic.IEnumerable<System.Reflection.Metadata.ManifestResourceHandle>, System.Collections.Generic.IReadOnlyCollection<System.Reflection.Metadata.ManifestResourceHandle>, System.Collections.IEnumerable
    {
        private readonly int _dummyPrimitive;
        public int Count { get { throw null; } }
        public System.Reflection.Metadata.ManifestResourceHandleCollection.Enumerator GetEnumerator() { throw null; }
        System.Collections.Generic.IEnumerator<System.Reflection.Metadata.ManifestResourceHandle> System.Collections.Generic.IEnumerable<System.Reflection.Metadata.ManifestResourceHandle>.GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        public partial struct Enumerator : System.Collections.Generic.IEnumerator<System.Reflection.Metadata.ManifestResourceHandle>, System.Collections.IEnumerator, System.IDisposable
        {
            private int _dummyPrimitive;
            public System.Reflection.Metadata.ManifestResourceHandle Current { get { throw null; } }
            object System.Collections.IEnumerator.Current { get { throw null; } }
            public bool MoveNext() { throw null; }
            void System.Collections.IEnumerator.Reset() { }
            void System.IDisposable.Dispose() { }
        }
    }
    public readonly partial struct MemberReference
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public System.Reflection.Metadata.StringHandle Name { get { throw null; } }
        public System.Reflection.Metadata.EntityHandle Parent { get { throw null; } }
        public System.Reflection.Metadata.BlobHandle Signature { get { throw null; } }
        public TType DecodeFieldSignature<TType, TGenericContext>(System.Reflection.Metadata.ISignatureTypeProvider<TType, TGenericContext> provider, TGenericContext genericContext) { throw null; }
        public System.Reflection.Metadata.MethodSignature<TType> DecodeMethodSignature<TType, TGenericContext>(System.Reflection.Metadata.ISignatureTypeProvider<TType, TGenericContext> provider, TGenericContext genericContext) { throw null; }
        public System.Reflection.Metadata.CustomAttributeHandleCollection GetCustomAttributes() { throw null; }
        public System.Reflection.Metadata.MemberReferenceKind GetKind() { throw null; }
    }
    public readonly partial struct MemberReferenceHandle : System.IEquatable<System.Reflection.Metadata.MemberReferenceHandle>
    {
        private readonly int _dummyPrimitive;
        public bool IsNil { get { throw null; } }
        public override bool Equals(object? obj) { throw null; }
        public bool Equals(System.Reflection.Metadata.MemberReferenceHandle other) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Reflection.Metadata.MemberReferenceHandle left, System.Reflection.Metadata.MemberReferenceHandle right) { throw null; }
        public static explicit operator System.Reflection.Metadata.MemberReferenceHandle (System.Reflection.Metadata.EntityHandle handle) { throw null; }
        public static explicit operator System.Reflection.Metadata.MemberReferenceHandle (System.Reflection.Metadata.Handle handle) { throw null; }
        public static implicit operator System.Reflection.Metadata.EntityHandle (System.Reflection.Metadata.MemberReferenceHandle handle) { throw null; }
        public static implicit operator System.Reflection.Metadata.Handle (System.Reflection.Metadata.MemberReferenceHandle handle) { throw null; }
        public static bool operator !=(System.Reflection.Metadata.MemberReferenceHandle left, System.Reflection.Metadata.MemberReferenceHandle right) { throw null; }
    }
    public readonly partial struct MemberReferenceHandleCollection : System.Collections.Generic.IEnumerable<System.Reflection.Metadata.MemberReferenceHandle>, System.Collections.Generic.IReadOnlyCollection<System.Reflection.Metadata.MemberReferenceHandle>, System.Collections.IEnumerable
    {
        private readonly int _dummyPrimitive;
        public int Count { get { throw null; } }
        public System.Reflection.Metadata.MemberReferenceHandleCollection.Enumerator GetEnumerator() { throw null; }
        System.Collections.Generic.IEnumerator<System.Reflection.Metadata.MemberReferenceHandle> System.Collections.Generic.IEnumerable<System.Reflection.Metadata.MemberReferenceHandle>.GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        public partial struct Enumerator : System.Collections.Generic.IEnumerator<System.Reflection.Metadata.MemberReferenceHandle>, System.Collections.IEnumerator, System.IDisposable
        {
            private int _dummyPrimitive;
            public System.Reflection.Metadata.MemberReferenceHandle Current { get { throw null; } }
            object System.Collections.IEnumerator.Current { get { throw null; } }
            public bool MoveNext() { throw null; }
            void System.Collections.IEnumerator.Reset() { }
            void System.IDisposable.Dispose() { }
        }
    }
    public enum MemberReferenceKind
    {
        Method = 0,
        Field = 1,
    }
    public enum MetadataKind
    {
        Ecma335 = 0,
        WindowsMetadata = 1,
        ManagedWindowsMetadata = 2,
    }
    public sealed partial class MetadataReader
    {
        public unsafe MetadataReader(byte* metadata, int length) { }
        public unsafe MetadataReader(byte* metadata, int length, System.Reflection.Metadata.MetadataReaderOptions options) { }
        public unsafe MetadataReader(byte* metadata, int length, System.Reflection.Metadata.MetadataReaderOptions options, System.Reflection.Metadata.MetadataStringDecoder? utf8Decoder) { }
        public System.Reflection.Metadata.AssemblyFileHandleCollection AssemblyFiles { get { throw null; } }
        public System.Reflection.Metadata.AssemblyReferenceHandleCollection AssemblyReferences { get { throw null; } }
        public System.Reflection.Metadata.CustomAttributeHandleCollection CustomAttributes { get { throw null; } }
        public System.Reflection.Metadata.CustomDebugInformationHandleCollection CustomDebugInformation { get { throw null; } }
        public System.Reflection.Metadata.DebugMetadataHeader? DebugMetadataHeader { get { throw null; } }
        public System.Reflection.Metadata.DeclarativeSecurityAttributeHandleCollection DeclarativeSecurityAttributes { get { throw null; } }
        public System.Reflection.Metadata.DocumentHandleCollection Documents { get { throw null; } }
        public System.Reflection.Metadata.EventDefinitionHandleCollection EventDefinitions { get { throw null; } }
        public System.Reflection.Metadata.ExportedTypeHandleCollection ExportedTypes { get { throw null; } }
        public System.Reflection.Metadata.FieldDefinitionHandleCollection FieldDefinitions { get { throw null; } }
        public System.Reflection.Metadata.ImportScopeCollection ImportScopes { get { throw null; } }
        public bool IsAssembly { get { throw null; } }
        public System.Reflection.Metadata.LocalConstantHandleCollection LocalConstants { get { throw null; } }
        public System.Reflection.Metadata.LocalScopeHandleCollection LocalScopes { get { throw null; } }
        public System.Reflection.Metadata.LocalVariableHandleCollection LocalVariables { get { throw null; } }
        public System.Reflection.Metadata.ManifestResourceHandleCollection ManifestResources { get { throw null; } }
        public System.Reflection.Metadata.MemberReferenceHandleCollection MemberReferences { get { throw null; } }
        public System.Reflection.Metadata.MetadataKind MetadataKind { get { throw null; } }
        public int MetadataLength { get { throw null; } }
        public unsafe byte* MetadataPointer { get { throw null; } }
        public string MetadataVersion { get { throw null; } }
        public System.Reflection.Metadata.MethodDebugInformationHandleCollection MethodDebugInformation { get { throw null; } }
        public System.Reflection.Metadata.MethodDefinitionHandleCollection MethodDefinitions { get { throw null; } }
        public System.Reflection.Metadata.MetadataReaderOptions Options { get { throw null; } }
        public System.Reflection.Metadata.PropertyDefinitionHandleCollection PropertyDefinitions { get { throw null; } }
        public System.Reflection.Metadata.MetadataStringComparer StringComparer { get { throw null; } }
        public System.Reflection.Metadata.TypeDefinitionHandleCollection TypeDefinitions { get { throw null; } }
        public System.Reflection.Metadata.TypeReferenceHandleCollection TypeReferences { get { throw null; } }
        public System.Reflection.Metadata.MetadataStringDecoder UTF8Decoder { get { throw null; } }
        public System.Reflection.Metadata.AssemblyDefinition GetAssemblyDefinition() { throw null; }
        public System.Reflection.Metadata.AssemblyFile GetAssemblyFile(System.Reflection.Metadata.AssemblyFileHandle handle) { throw null; }
        public static System.Reflection.AssemblyName GetAssemblyName(string assemblyFile) { throw null; }
        public System.Reflection.Metadata.AssemblyReference GetAssemblyReference(System.Reflection.Metadata.AssemblyReferenceHandle handle) { throw null; }
        public byte[] GetBlobBytes(System.Reflection.Metadata.BlobHandle handle) { throw null; }
        public System.Collections.Immutable.ImmutableArray<byte> GetBlobContent(System.Reflection.Metadata.BlobHandle handle) { throw null; }
        public System.Reflection.Metadata.BlobReader GetBlobReader(System.Reflection.Metadata.BlobHandle handle) { throw null; }
        public System.Reflection.Metadata.BlobReader GetBlobReader(System.Reflection.Metadata.StringHandle handle) { throw null; }
        public System.Reflection.Metadata.Constant GetConstant(System.Reflection.Metadata.ConstantHandle handle) { throw null; }
        public System.Reflection.Metadata.CustomAttribute GetCustomAttribute(System.Reflection.Metadata.CustomAttributeHandle handle) { throw null; }
        public System.Reflection.Metadata.CustomAttributeHandleCollection GetCustomAttributes(System.Reflection.Metadata.EntityHandle handle) { throw null; }
        public System.Reflection.Metadata.CustomDebugInformation GetCustomDebugInformation(System.Reflection.Metadata.CustomDebugInformationHandle handle) { throw null; }
        public System.Reflection.Metadata.CustomDebugInformationHandleCollection GetCustomDebugInformation(System.Reflection.Metadata.EntityHandle handle) { throw null; }
        public System.Reflection.Metadata.DeclarativeSecurityAttribute GetDeclarativeSecurityAttribute(System.Reflection.Metadata.DeclarativeSecurityAttributeHandle handle) { throw null; }
        public System.Reflection.Metadata.Document GetDocument(System.Reflection.Metadata.DocumentHandle handle) { throw null; }
        public System.Reflection.Metadata.EventDefinition GetEventDefinition(System.Reflection.Metadata.EventDefinitionHandle handle) { throw null; }
        public System.Reflection.Metadata.ExportedType GetExportedType(System.Reflection.Metadata.ExportedTypeHandle handle) { throw null; }
        public System.Reflection.Metadata.FieldDefinition GetFieldDefinition(System.Reflection.Metadata.FieldDefinitionHandle handle) { throw null; }
        public System.Reflection.Metadata.GenericParameter GetGenericParameter(System.Reflection.Metadata.GenericParameterHandle handle) { throw null; }
        public System.Reflection.Metadata.GenericParameterConstraint GetGenericParameterConstraint(System.Reflection.Metadata.GenericParameterConstraintHandle handle) { throw null; }
        public System.Guid GetGuid(System.Reflection.Metadata.GuidHandle handle) { throw null; }
        public System.Reflection.Metadata.ImportScope GetImportScope(System.Reflection.Metadata.ImportScopeHandle handle) { throw null; }
        public System.Reflection.Metadata.InterfaceImplementation GetInterfaceImplementation(System.Reflection.Metadata.InterfaceImplementationHandle handle) { throw null; }
        public System.Reflection.Metadata.LocalConstant GetLocalConstant(System.Reflection.Metadata.LocalConstantHandle handle) { throw null; }
        public System.Reflection.Metadata.LocalScope GetLocalScope(System.Reflection.Metadata.LocalScopeHandle handle) { throw null; }
        public System.Reflection.Metadata.LocalScopeHandleCollection GetLocalScopes(System.Reflection.Metadata.MethodDebugInformationHandle handle) { throw null; }
        public System.Reflection.Metadata.LocalScopeHandleCollection GetLocalScopes(System.Reflection.Metadata.MethodDefinitionHandle handle) { throw null; }
        public System.Reflection.Metadata.LocalVariable GetLocalVariable(System.Reflection.Metadata.LocalVariableHandle handle) { throw null; }
        public System.Reflection.Metadata.ManifestResource GetManifestResource(System.Reflection.Metadata.ManifestResourceHandle handle) { throw null; }
        public System.Reflection.Metadata.MemberReference GetMemberReference(System.Reflection.Metadata.MemberReferenceHandle handle) { throw null; }
        public System.Reflection.Metadata.MethodDebugInformation GetMethodDebugInformation(System.Reflection.Metadata.MethodDebugInformationHandle handle) { throw null; }
        public System.Reflection.Metadata.MethodDebugInformation GetMethodDebugInformation(System.Reflection.Metadata.MethodDefinitionHandle handle) { throw null; }
        public System.Reflection.Metadata.MethodDefinition GetMethodDefinition(System.Reflection.Metadata.MethodDefinitionHandle handle) { throw null; }
        public System.Reflection.Metadata.MethodImplementation GetMethodImplementation(System.Reflection.Metadata.MethodImplementationHandle handle) { throw null; }
        public System.Reflection.Metadata.MethodSpecification GetMethodSpecification(System.Reflection.Metadata.MethodSpecificationHandle handle) { throw null; }
        public System.Reflection.Metadata.ModuleDefinition GetModuleDefinition() { throw null; }
        public System.Reflection.Metadata.ModuleReference GetModuleReference(System.Reflection.Metadata.ModuleReferenceHandle handle) { throw null; }
        public System.Reflection.Metadata.NamespaceDefinition GetNamespaceDefinition(System.Reflection.Metadata.NamespaceDefinitionHandle handle) { throw null; }
        public System.Reflection.Metadata.NamespaceDefinition GetNamespaceDefinitionRoot() { throw null; }
        public System.Reflection.Metadata.Parameter GetParameter(System.Reflection.Metadata.ParameterHandle handle) { throw null; }
        public System.Reflection.Metadata.PropertyDefinition GetPropertyDefinition(System.Reflection.Metadata.PropertyDefinitionHandle handle) { throw null; }
        public System.Reflection.Metadata.StandaloneSignature GetStandaloneSignature(System.Reflection.Metadata.StandaloneSignatureHandle handle) { throw null; }
        public string GetString(System.Reflection.Metadata.DocumentNameBlobHandle handle) { throw null; }
        public string GetString(System.Reflection.Metadata.NamespaceDefinitionHandle handle) { throw null; }
        public string GetString(System.Reflection.Metadata.StringHandle handle) { throw null; }
        public System.Reflection.Metadata.TypeDefinition GetTypeDefinition(System.Reflection.Metadata.TypeDefinitionHandle handle) { throw null; }
        public System.Reflection.Metadata.TypeReference GetTypeReference(System.Reflection.Metadata.TypeReferenceHandle handle) { throw null; }
        public System.Reflection.Metadata.TypeSpecification GetTypeSpecification(System.Reflection.Metadata.TypeSpecificationHandle handle) { throw null; }
        public string GetUserString(System.Reflection.Metadata.UserStringHandle handle) { throw null; }
    }
    [System.FlagsAttribute]
    public enum MetadataReaderOptions
    {
        None = 0,
        ApplyWindowsRuntimeProjections = 1,
        Default = 1,
    }
    public sealed partial class MetadataReaderProvider : System.IDisposable
    {
        internal MetadataReaderProvider() { }
        public void Dispose() { }
        public unsafe static System.Reflection.Metadata.MetadataReaderProvider FromMetadataImage(byte* start, int size) { throw null; }
        public static System.Reflection.Metadata.MetadataReaderProvider FromMetadataImage(System.Collections.Immutable.ImmutableArray<byte> image) { throw null; }
        public static System.Reflection.Metadata.MetadataReaderProvider FromMetadataStream(System.IO.Stream stream, System.Reflection.Metadata.MetadataStreamOptions options = System.Reflection.Metadata.MetadataStreamOptions.Default, int size = 0) { throw null; }
        public unsafe static System.Reflection.Metadata.MetadataReaderProvider FromPortablePdbImage(byte* start, int size) { throw null; }
        public static System.Reflection.Metadata.MetadataReaderProvider FromPortablePdbImage(System.Collections.Immutable.ImmutableArray<byte> image) { throw null; }
        public static System.Reflection.Metadata.MetadataReaderProvider FromPortablePdbStream(System.IO.Stream stream, System.Reflection.Metadata.MetadataStreamOptions options = System.Reflection.Metadata.MetadataStreamOptions.Default, int size = 0) { throw null; }
        public System.Reflection.Metadata.MetadataReader GetMetadataReader(System.Reflection.Metadata.MetadataReaderOptions options = System.Reflection.Metadata.MetadataReaderOptions.Default, System.Reflection.Metadata.MetadataStringDecoder? utf8Decoder = null) { throw null; }
    }
    [System.FlagsAttribute]
    public enum MetadataStreamOptions
    {
        Default = 0,
        LeaveOpen = 1,
        PrefetchMetadata = 2,
    }
    public readonly partial struct MetadataStringComparer
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public bool Equals(System.Reflection.Metadata.DocumentNameBlobHandle handle, string value) { throw null; }
        public bool Equals(System.Reflection.Metadata.DocumentNameBlobHandle handle, string value, bool ignoreCase) { throw null; }
        public bool Equals(System.Reflection.Metadata.NamespaceDefinitionHandle handle, string value) { throw null; }
        public bool Equals(System.Reflection.Metadata.NamespaceDefinitionHandle handle, string value, bool ignoreCase) { throw null; }
        public bool Equals(System.Reflection.Metadata.StringHandle handle, string value) { throw null; }
        public bool Equals(System.Reflection.Metadata.StringHandle handle, string value, bool ignoreCase) { throw null; }
        public bool StartsWith(System.Reflection.Metadata.StringHandle handle, string value) { throw null; }
        public bool StartsWith(System.Reflection.Metadata.StringHandle handle, string value, bool ignoreCase) { throw null; }
    }
    public partial class MetadataStringDecoder
    {
        public MetadataStringDecoder(System.Text.Encoding encoding) { }
        public static System.Reflection.Metadata.MetadataStringDecoder DefaultUTF8 { get { throw null; } }
        public System.Text.Encoding Encoding { get { throw null; } }
        public unsafe virtual string GetString(byte* bytes, int byteCount) { throw null; }
    }
    public sealed partial class MethodBodyBlock
    {
        internal MethodBodyBlock() { }
        public System.Collections.Immutable.ImmutableArray<System.Reflection.Metadata.ExceptionRegion> ExceptionRegions { get { throw null; } }
        public System.Reflection.Metadata.StandaloneSignatureHandle LocalSignature { get { throw null; } }
        public bool LocalVariablesInitialized { get { throw null; } }
        public int MaxStack { get { throw null; } }
        public int Size { get { throw null; } }
        public static System.Reflection.Metadata.MethodBodyBlock Create(System.Reflection.Metadata.BlobReader reader) { throw null; }
        public byte[]? GetILBytes() { throw null; }
        public System.Collections.Immutable.ImmutableArray<byte> GetILContent() { throw null; }
        public System.Reflection.Metadata.BlobReader GetILReader() { throw null; }
    }
    public readonly partial struct MethodDebugInformation
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public System.Reflection.Metadata.DocumentHandle Document { get { throw null; } }
        public System.Reflection.Metadata.StandaloneSignatureHandle LocalSignature { get { throw null; } }
        public System.Reflection.Metadata.BlobHandle SequencePointsBlob { get { throw null; } }
        public System.Reflection.Metadata.SequencePointCollection GetSequencePoints() { throw null; }
        public System.Reflection.Metadata.MethodDefinitionHandle GetStateMachineKickoffMethod() { throw null; }
    }
    public readonly partial struct MethodDebugInformationHandle : System.IEquatable<System.Reflection.Metadata.MethodDebugInformationHandle>
    {
        private readonly int _dummyPrimitive;
        public bool IsNil { get { throw null; } }
        public override bool Equals(object? obj) { throw null; }
        public bool Equals(System.Reflection.Metadata.MethodDebugInformationHandle other) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Reflection.Metadata.MethodDebugInformationHandle left, System.Reflection.Metadata.MethodDebugInformationHandle right) { throw null; }
        public static explicit operator System.Reflection.Metadata.MethodDebugInformationHandle (System.Reflection.Metadata.EntityHandle handle) { throw null; }
        public static explicit operator System.Reflection.Metadata.MethodDebugInformationHandle (System.Reflection.Metadata.Handle handle) { throw null; }
        public static implicit operator System.Reflection.Metadata.EntityHandle (System.Reflection.Metadata.MethodDebugInformationHandle handle) { throw null; }
        public static implicit operator System.Reflection.Metadata.Handle (System.Reflection.Metadata.MethodDebugInformationHandle handle) { throw null; }
        public static bool operator !=(System.Reflection.Metadata.MethodDebugInformationHandle left, System.Reflection.Metadata.MethodDebugInformationHandle right) { throw null; }
        public System.Reflection.Metadata.MethodDefinitionHandle ToDefinitionHandle() { throw null; }
    }
    public readonly partial struct MethodDebugInformationHandleCollection : System.Collections.Generic.IEnumerable<System.Reflection.Metadata.MethodDebugInformationHandle>, System.Collections.Generic.IReadOnlyCollection<System.Reflection.Metadata.MethodDebugInformationHandle>, System.Collections.IEnumerable
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public int Count { get { throw null; } }
        public System.Reflection.Metadata.MethodDebugInformationHandleCollection.Enumerator GetEnumerator() { throw null; }
        System.Collections.Generic.IEnumerator<System.Reflection.Metadata.MethodDebugInformationHandle> System.Collections.Generic.IEnumerable<System.Reflection.Metadata.MethodDebugInformationHandle>.GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        public partial struct Enumerator : System.Collections.Generic.IEnumerator<System.Reflection.Metadata.MethodDebugInformationHandle>, System.Collections.IEnumerator, System.IDisposable
        {
            private object _dummy;
            private int _dummyPrimitive;
            public System.Reflection.Metadata.MethodDebugInformationHandle Current { get { throw null; } }
            object System.Collections.IEnumerator.Current { get { throw null; } }
            public bool MoveNext() { throw null; }
            void System.Collections.IEnumerator.Reset() { }
            void System.IDisposable.Dispose() { }
        }
    }
    public readonly partial struct MethodDefinition
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public System.Reflection.MethodAttributes Attributes { get { throw null; } }
        public System.Reflection.MethodImplAttributes ImplAttributes { get { throw null; } }
        public System.Reflection.Metadata.StringHandle Name { get { throw null; } }
        public int RelativeVirtualAddress { get { throw null; } }
        public System.Reflection.Metadata.BlobHandle Signature { get { throw null; } }
        public System.Reflection.Metadata.MethodSignature<TType> DecodeSignature<TType, TGenericContext>(System.Reflection.Metadata.ISignatureTypeProvider<TType, TGenericContext> provider, TGenericContext genericContext) { throw null; }
        public System.Reflection.Metadata.CustomAttributeHandleCollection GetCustomAttributes() { throw null; }
        public System.Reflection.Metadata.DeclarativeSecurityAttributeHandleCollection GetDeclarativeSecurityAttributes() { throw null; }
        public System.Reflection.Metadata.TypeDefinitionHandle GetDeclaringType() { throw null; }
        public System.Reflection.Metadata.GenericParameterHandleCollection GetGenericParameters() { throw null; }
        public System.Reflection.Metadata.MethodImport GetImport() { throw null; }
        public System.Reflection.Metadata.ParameterHandleCollection GetParameters() { throw null; }
    }
    public readonly partial struct MethodDefinitionHandle : System.IEquatable<System.Reflection.Metadata.MethodDefinitionHandle>
    {
        private readonly int _dummyPrimitive;
        public bool IsNil { get { throw null; } }
        public override bool Equals(object? obj) { throw null; }
        public bool Equals(System.Reflection.Metadata.MethodDefinitionHandle other) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Reflection.Metadata.MethodDefinitionHandle left, System.Reflection.Metadata.MethodDefinitionHandle right) { throw null; }
        public static explicit operator System.Reflection.Metadata.MethodDefinitionHandle (System.Reflection.Metadata.EntityHandle handle) { throw null; }
        public static explicit operator System.Reflection.Metadata.MethodDefinitionHandle (System.Reflection.Metadata.Handle handle) { throw null; }
        public static implicit operator System.Reflection.Metadata.EntityHandle (System.Reflection.Metadata.MethodDefinitionHandle handle) { throw null; }
        public static implicit operator System.Reflection.Metadata.Handle (System.Reflection.Metadata.MethodDefinitionHandle handle) { throw null; }
        public static bool operator !=(System.Reflection.Metadata.MethodDefinitionHandle left, System.Reflection.Metadata.MethodDefinitionHandle right) { throw null; }
        public System.Reflection.Metadata.MethodDebugInformationHandle ToDebugInformationHandle() { throw null; }
    }
    public readonly partial struct MethodDefinitionHandleCollection : System.Collections.Generic.IEnumerable<System.Reflection.Metadata.MethodDefinitionHandle>, System.Collections.Generic.IReadOnlyCollection<System.Reflection.Metadata.MethodDefinitionHandle>, System.Collections.IEnumerable
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public int Count { get { throw null; } }
        public System.Reflection.Metadata.MethodDefinitionHandleCollection.Enumerator GetEnumerator() { throw null; }
        System.Collections.Generic.IEnumerator<System.Reflection.Metadata.MethodDefinitionHandle> System.Collections.Generic.IEnumerable<System.Reflection.Metadata.MethodDefinitionHandle>.GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        public partial struct Enumerator : System.Collections.Generic.IEnumerator<System.Reflection.Metadata.MethodDefinitionHandle>, System.Collections.IEnumerator, System.IDisposable
        {
            private object _dummy;
            private int _dummyPrimitive;
            public System.Reflection.Metadata.MethodDefinitionHandle Current { get { throw null; } }
            object System.Collections.IEnumerator.Current { get { throw null; } }
            public bool MoveNext() { throw null; }
            void System.Collections.IEnumerator.Reset() { }
            void System.IDisposable.Dispose() { }
        }
    }
    public readonly partial struct MethodImplementation
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public System.Reflection.Metadata.EntityHandle MethodBody { get { throw null; } }
        public System.Reflection.Metadata.EntityHandle MethodDeclaration { get { throw null; } }
        public System.Reflection.Metadata.TypeDefinitionHandle Type { get { throw null; } }
        public System.Reflection.Metadata.CustomAttributeHandleCollection GetCustomAttributes() { throw null; }
    }
    public readonly partial struct MethodImplementationHandle : System.IEquatable<System.Reflection.Metadata.MethodImplementationHandle>
    {
        private readonly int _dummyPrimitive;
        public bool IsNil { get { throw null; } }
        public override bool Equals(object? obj) { throw null; }
        public bool Equals(System.Reflection.Metadata.MethodImplementationHandle other) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Reflection.Metadata.MethodImplementationHandle left, System.Reflection.Metadata.MethodImplementationHandle right) { throw null; }
        public static explicit operator System.Reflection.Metadata.MethodImplementationHandle (System.Reflection.Metadata.EntityHandle handle) { throw null; }
        public static explicit operator System.Reflection.Metadata.MethodImplementationHandle (System.Reflection.Metadata.Handle handle) { throw null; }
        public static implicit operator System.Reflection.Metadata.EntityHandle (System.Reflection.Metadata.MethodImplementationHandle handle) { throw null; }
        public static implicit operator System.Reflection.Metadata.Handle (System.Reflection.Metadata.MethodImplementationHandle handle) { throw null; }
        public static bool operator !=(System.Reflection.Metadata.MethodImplementationHandle left, System.Reflection.Metadata.MethodImplementationHandle right) { throw null; }
    }
    public readonly partial struct MethodImplementationHandleCollection : System.Collections.Generic.IEnumerable<System.Reflection.Metadata.MethodImplementationHandle>, System.Collections.Generic.IReadOnlyCollection<System.Reflection.Metadata.MethodImplementationHandle>, System.Collections.IEnumerable
    {
        private readonly int _dummyPrimitive;
        public int Count { get { throw null; } }
        public System.Reflection.Metadata.MethodImplementationHandleCollection.Enumerator GetEnumerator() { throw null; }
        System.Collections.Generic.IEnumerator<System.Reflection.Metadata.MethodImplementationHandle> System.Collections.Generic.IEnumerable<System.Reflection.Metadata.MethodImplementationHandle>.GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        public partial struct Enumerator : System.Collections.Generic.IEnumerator<System.Reflection.Metadata.MethodImplementationHandle>, System.Collections.IEnumerator, System.IDisposable
        {
            private int _dummyPrimitive;
            public System.Reflection.Metadata.MethodImplementationHandle Current { get { throw null; } }
            object System.Collections.IEnumerator.Current { get { throw null; } }
            public bool MoveNext() { throw null; }
            void System.Collections.IEnumerator.Reset() { }
            void System.IDisposable.Dispose() { }
        }
    }
    public readonly partial struct MethodImport
    {
        private readonly int _dummyPrimitive;
        public System.Reflection.MethodImportAttributes Attributes { get { throw null; } }
        public System.Reflection.Metadata.ModuleReferenceHandle Module { get { throw null; } }
        public System.Reflection.Metadata.StringHandle Name { get { throw null; } }
    }
    public readonly partial struct MethodSignature<TType>
    {
        private readonly TType _ReturnType_k__BackingField;
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public MethodSignature(System.Reflection.Metadata.SignatureHeader header, TType returnType, int requiredParameterCount, int genericParameterCount, System.Collections.Immutable.ImmutableArray<TType> parameterTypes) { throw null; }
        public int GenericParameterCount { get { throw null; } }
        public System.Reflection.Metadata.SignatureHeader Header { get { throw null; } }
        public System.Collections.Immutable.ImmutableArray<TType> ParameterTypes { get { throw null; } }
        public int RequiredParameterCount { get { throw null; } }
        public TType ReturnType { get { throw null; } }
    }
    public readonly partial struct MethodSpecification
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public System.Reflection.Metadata.EntityHandle Method { get { throw null; } }
        public System.Reflection.Metadata.BlobHandle Signature { get { throw null; } }
        public System.Collections.Immutable.ImmutableArray<TType> DecodeSignature<TType, TGenericContext>(System.Reflection.Metadata.ISignatureTypeProvider<TType, TGenericContext> provider, TGenericContext genericContext) { throw null; }
        public System.Reflection.Metadata.CustomAttributeHandleCollection GetCustomAttributes() { throw null; }
    }
    public readonly partial struct MethodSpecificationHandle : System.IEquatable<System.Reflection.Metadata.MethodSpecificationHandle>
    {
        private readonly int _dummyPrimitive;
        public bool IsNil { get { throw null; } }
        public override bool Equals(object? obj) { throw null; }
        public bool Equals(System.Reflection.Metadata.MethodSpecificationHandle other) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Reflection.Metadata.MethodSpecificationHandle left, System.Reflection.Metadata.MethodSpecificationHandle right) { throw null; }
        public static explicit operator System.Reflection.Metadata.MethodSpecificationHandle (System.Reflection.Metadata.EntityHandle handle) { throw null; }
        public static explicit operator System.Reflection.Metadata.MethodSpecificationHandle (System.Reflection.Metadata.Handle handle) { throw null; }
        public static implicit operator System.Reflection.Metadata.EntityHandle (System.Reflection.Metadata.MethodSpecificationHandle handle) { throw null; }
        public static implicit operator System.Reflection.Metadata.Handle (System.Reflection.Metadata.MethodSpecificationHandle handle) { throw null; }
        public static bool operator !=(System.Reflection.Metadata.MethodSpecificationHandle left, System.Reflection.Metadata.MethodSpecificationHandle right) { throw null; }
    }
    public readonly partial struct ModuleDefinition
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public System.Reflection.Metadata.GuidHandle BaseGenerationId { get { throw null; } }
        public int Generation { get { throw null; } }
        public System.Reflection.Metadata.GuidHandle GenerationId { get { throw null; } }
        public System.Reflection.Metadata.GuidHandle Mvid { get { throw null; } }
        public System.Reflection.Metadata.StringHandle Name { get { throw null; } }
        public System.Reflection.Metadata.CustomAttributeHandleCollection GetCustomAttributes() { throw null; }
    }
    public readonly partial struct ModuleDefinitionHandle : System.IEquatable<System.Reflection.Metadata.ModuleDefinitionHandle>
    {
        private readonly int _dummyPrimitive;
        public bool IsNil { get { throw null; } }
        public override bool Equals(object? obj) { throw null; }
        public bool Equals(System.Reflection.Metadata.ModuleDefinitionHandle other) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Reflection.Metadata.ModuleDefinitionHandle left, System.Reflection.Metadata.ModuleDefinitionHandle right) { throw null; }
        public static explicit operator System.Reflection.Metadata.ModuleDefinitionHandle (System.Reflection.Metadata.EntityHandle handle) { throw null; }
        public static explicit operator System.Reflection.Metadata.ModuleDefinitionHandle (System.Reflection.Metadata.Handle handle) { throw null; }
        public static implicit operator System.Reflection.Metadata.EntityHandle (System.Reflection.Metadata.ModuleDefinitionHandle handle) { throw null; }
        public static implicit operator System.Reflection.Metadata.Handle (System.Reflection.Metadata.ModuleDefinitionHandle handle) { throw null; }
        public static bool operator !=(System.Reflection.Metadata.ModuleDefinitionHandle left, System.Reflection.Metadata.ModuleDefinitionHandle right) { throw null; }
    }
    public readonly partial struct ModuleReference
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public System.Reflection.Metadata.StringHandle Name { get { throw null; } }
        public System.Reflection.Metadata.CustomAttributeHandleCollection GetCustomAttributes() { throw null; }
    }
    public readonly partial struct ModuleReferenceHandle : System.IEquatable<System.Reflection.Metadata.ModuleReferenceHandle>
    {
        private readonly int _dummyPrimitive;
        public bool IsNil { get { throw null; } }
        public override bool Equals(object? obj) { throw null; }
        public bool Equals(System.Reflection.Metadata.ModuleReferenceHandle other) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Reflection.Metadata.ModuleReferenceHandle left, System.Reflection.Metadata.ModuleReferenceHandle right) { throw null; }
        public static explicit operator System.Reflection.Metadata.ModuleReferenceHandle (System.Reflection.Metadata.EntityHandle handle) { throw null; }
        public static explicit operator System.Reflection.Metadata.ModuleReferenceHandle (System.Reflection.Metadata.Handle handle) { throw null; }
        public static implicit operator System.Reflection.Metadata.EntityHandle (System.Reflection.Metadata.ModuleReferenceHandle handle) { throw null; }
        public static implicit operator System.Reflection.Metadata.Handle (System.Reflection.Metadata.ModuleReferenceHandle handle) { throw null; }
        public static bool operator !=(System.Reflection.Metadata.ModuleReferenceHandle left, System.Reflection.Metadata.ModuleReferenceHandle right) { throw null; }
    }
    public partial struct NamespaceDefinition
    {
        private object _dummy;
        private int _dummyPrimitive;
        public System.Collections.Immutable.ImmutableArray<System.Reflection.Metadata.ExportedTypeHandle> ExportedTypes { get { throw null; } }
        public System.Reflection.Metadata.StringHandle Name { get { throw null; } }
        public System.Collections.Immutable.ImmutableArray<System.Reflection.Metadata.NamespaceDefinitionHandle> NamespaceDefinitions { get { throw null; } }
        public System.Reflection.Metadata.NamespaceDefinitionHandle Parent { get { throw null; } }
        public System.Collections.Immutable.ImmutableArray<System.Reflection.Metadata.TypeDefinitionHandle> TypeDefinitions { get { throw null; } }
    }
    public readonly partial struct NamespaceDefinitionHandle : System.IEquatable<System.Reflection.Metadata.NamespaceDefinitionHandle>
    {
        private readonly int _dummyPrimitive;
        public bool IsNil { get { throw null; } }
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] object? obj) { throw null; }
        public bool Equals(System.Reflection.Metadata.NamespaceDefinitionHandle other) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Reflection.Metadata.NamespaceDefinitionHandle left, System.Reflection.Metadata.NamespaceDefinitionHandle right) { throw null; }
        public static explicit operator System.Reflection.Metadata.NamespaceDefinitionHandle (System.Reflection.Metadata.Handle handle) { throw null; }
        public static implicit operator System.Reflection.Metadata.Handle (System.Reflection.Metadata.NamespaceDefinitionHandle handle) { throw null; }
        public static bool operator !=(System.Reflection.Metadata.NamespaceDefinitionHandle left, System.Reflection.Metadata.NamespaceDefinitionHandle right) { throw null; }
    }
    public readonly partial struct Parameter
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public System.Reflection.ParameterAttributes Attributes { get { throw null; } }
        public System.Reflection.Metadata.StringHandle Name { get { throw null; } }
        public int SequenceNumber { get { throw null; } }
        public System.Reflection.Metadata.CustomAttributeHandleCollection GetCustomAttributes() { throw null; }
        public System.Reflection.Metadata.ConstantHandle GetDefaultValue() { throw null; }
        public System.Reflection.Metadata.BlobHandle GetMarshallingDescriptor() { throw null; }
    }
    public readonly partial struct ParameterHandle : System.IEquatable<System.Reflection.Metadata.ParameterHandle>
    {
        private readonly int _dummyPrimitive;
        public bool IsNil { get { throw null; } }
        public override bool Equals(object? obj) { throw null; }
        public bool Equals(System.Reflection.Metadata.ParameterHandle other) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Reflection.Metadata.ParameterHandle left, System.Reflection.Metadata.ParameterHandle right) { throw null; }
        public static explicit operator System.Reflection.Metadata.ParameterHandle (System.Reflection.Metadata.EntityHandle handle) { throw null; }
        public static explicit operator System.Reflection.Metadata.ParameterHandle (System.Reflection.Metadata.Handle handle) { throw null; }
        public static implicit operator System.Reflection.Metadata.EntityHandle (System.Reflection.Metadata.ParameterHandle handle) { throw null; }
        public static implicit operator System.Reflection.Metadata.Handle (System.Reflection.Metadata.ParameterHandle handle) { throw null; }
        public static bool operator !=(System.Reflection.Metadata.ParameterHandle left, System.Reflection.Metadata.ParameterHandle right) { throw null; }
    }
    public readonly partial struct ParameterHandleCollection : System.Collections.Generic.IEnumerable<System.Reflection.Metadata.ParameterHandle>, System.Collections.Generic.IReadOnlyCollection<System.Reflection.Metadata.ParameterHandle>, System.Collections.IEnumerable
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public int Count { get { throw null; } }
        public System.Reflection.Metadata.ParameterHandleCollection.Enumerator GetEnumerator() { throw null; }
        System.Collections.Generic.IEnumerator<System.Reflection.Metadata.ParameterHandle> System.Collections.Generic.IEnumerable<System.Reflection.Metadata.ParameterHandle>.GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        public partial struct Enumerator : System.Collections.Generic.IEnumerator<System.Reflection.Metadata.ParameterHandle>, System.Collections.IEnumerator, System.IDisposable
        {
            private object _dummy;
            private int _dummyPrimitive;
            public System.Reflection.Metadata.ParameterHandle Current { get { throw null; } }
            object System.Collections.IEnumerator.Current { get { throw null; } }
            public bool MoveNext() { throw null; }
            void System.Collections.IEnumerator.Reset() { }
            void System.IDisposable.Dispose() { }
        }
    }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public static partial class PEReaderExtensions
    {
        public static System.Reflection.Metadata.MetadataReader GetMetadataReader(this System.Reflection.PortableExecutable.PEReader peReader) { throw null; }
        public static System.Reflection.Metadata.MetadataReader GetMetadataReader(this System.Reflection.PortableExecutable.PEReader peReader, System.Reflection.Metadata.MetadataReaderOptions options) { throw null; }
        public static System.Reflection.Metadata.MetadataReader GetMetadataReader(this System.Reflection.PortableExecutable.PEReader peReader, System.Reflection.Metadata.MetadataReaderOptions options, System.Reflection.Metadata.MetadataStringDecoder? utf8Decoder) { throw null; }
        public static System.Reflection.Metadata.MethodBodyBlock GetMethodBody(this System.Reflection.PortableExecutable.PEReader peReader, int relativeVirtualAddress) { throw null; }
    }
    public enum PrimitiveSerializationTypeCode : byte
    {
        Boolean = (byte)2,
        Char = (byte)3,
        SByte = (byte)4,
        Byte = (byte)5,
        Int16 = (byte)6,
        UInt16 = (byte)7,
        Int32 = (byte)8,
        UInt32 = (byte)9,
        Int64 = (byte)10,
        UInt64 = (byte)11,
        Single = (byte)12,
        Double = (byte)13,
        String = (byte)14,
    }
    public enum PrimitiveTypeCode : byte
    {
        Void = (byte)1,
        Boolean = (byte)2,
        Char = (byte)3,
        SByte = (byte)4,
        Byte = (byte)5,
        Int16 = (byte)6,
        UInt16 = (byte)7,
        Int32 = (byte)8,
        UInt32 = (byte)9,
        Int64 = (byte)10,
        UInt64 = (byte)11,
        Single = (byte)12,
        Double = (byte)13,
        String = (byte)14,
        TypedReference = (byte)22,
        IntPtr = (byte)24,
        UIntPtr = (byte)25,
        Object = (byte)28,
    }
    public readonly partial struct PropertyAccessors
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public System.Reflection.Metadata.MethodDefinitionHandle Getter { get { throw null; } }
        public System.Collections.Immutable.ImmutableArray<System.Reflection.Metadata.MethodDefinitionHandle> Others { get { throw null; } }
        public System.Reflection.Metadata.MethodDefinitionHandle Setter { get { throw null; } }
    }
    public readonly partial struct PropertyDefinition
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public System.Reflection.PropertyAttributes Attributes { get { throw null; } }
        public System.Reflection.Metadata.StringHandle Name { get { throw null; } }
        public System.Reflection.Metadata.BlobHandle Signature { get { throw null; } }
        public System.Reflection.Metadata.MethodSignature<TType> DecodeSignature<TType, TGenericContext>(System.Reflection.Metadata.ISignatureTypeProvider<TType, TGenericContext> provider, TGenericContext genericContext) { throw null; }
        public System.Reflection.Metadata.PropertyAccessors GetAccessors() { throw null; }
        public System.Reflection.Metadata.CustomAttributeHandleCollection GetCustomAttributes() { throw null; }
        public System.Reflection.Metadata.ConstantHandle GetDefaultValue() { throw null; }
    }
    public readonly partial struct PropertyDefinitionHandle : System.IEquatable<System.Reflection.Metadata.PropertyDefinitionHandle>
    {
        private readonly int _dummyPrimitive;
        public bool IsNil { get { throw null; } }
        public override bool Equals(object? obj) { throw null; }
        public bool Equals(System.Reflection.Metadata.PropertyDefinitionHandle other) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Reflection.Metadata.PropertyDefinitionHandle left, System.Reflection.Metadata.PropertyDefinitionHandle right) { throw null; }
        public static explicit operator System.Reflection.Metadata.PropertyDefinitionHandle (System.Reflection.Metadata.EntityHandle handle) { throw null; }
        public static explicit operator System.Reflection.Metadata.PropertyDefinitionHandle (System.Reflection.Metadata.Handle handle) { throw null; }
        public static implicit operator System.Reflection.Metadata.EntityHandle (System.Reflection.Metadata.PropertyDefinitionHandle handle) { throw null; }
        public static implicit operator System.Reflection.Metadata.Handle (System.Reflection.Metadata.PropertyDefinitionHandle handle) { throw null; }
        public static bool operator !=(System.Reflection.Metadata.PropertyDefinitionHandle left, System.Reflection.Metadata.PropertyDefinitionHandle right) { throw null; }
    }
    public readonly partial struct PropertyDefinitionHandleCollection : System.Collections.Generic.IEnumerable<System.Reflection.Metadata.PropertyDefinitionHandle>, System.Collections.Generic.IReadOnlyCollection<System.Reflection.Metadata.PropertyDefinitionHandle>, System.Collections.IEnumerable
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public int Count { get { throw null; } }
        public System.Reflection.Metadata.PropertyDefinitionHandleCollection.Enumerator GetEnumerator() { throw null; }
        System.Collections.Generic.IEnumerator<System.Reflection.Metadata.PropertyDefinitionHandle> System.Collections.Generic.IEnumerable<System.Reflection.Metadata.PropertyDefinitionHandle>.GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        public partial struct Enumerator : System.Collections.Generic.IEnumerator<System.Reflection.Metadata.PropertyDefinitionHandle>, System.Collections.IEnumerator, System.IDisposable
        {
            private object _dummy;
            private int _dummyPrimitive;
            public System.Reflection.Metadata.PropertyDefinitionHandle Current { get { throw null; } }
            object System.Collections.IEnumerator.Current { get { throw null; } }
            public bool MoveNext() { throw null; }
            void System.Collections.IEnumerator.Reset() { }
            void System.IDisposable.Dispose() { }
        }
    }
    public readonly partial struct ReservedBlob<THandle> where THandle : struct
    {
        private readonly THandle _Handle_k__BackingField;
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public System.Reflection.Metadata.Blob Content { get { throw null; } }
        public THandle Handle { get { throw null; } }
        public System.Reflection.Metadata.BlobWriter CreateWriter() { throw null; }
    }
    public readonly partial struct SequencePoint : System.IEquatable<System.Reflection.Metadata.SequencePoint>
    {
        private readonly int _dummyPrimitive;
        public const int HiddenLine = 16707566;
        public System.Reflection.Metadata.DocumentHandle Document { get { throw null; } }
        public int EndColumn { get { throw null; } }
        public int EndLine { get { throw null; } }
        public bool IsHidden { get { throw null; } }
        public int Offset { get { throw null; } }
        public int StartColumn { get { throw null; } }
        public int StartLine { get { throw null; } }
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] object? obj) { throw null; }
        public bool Equals(System.Reflection.Metadata.SequencePoint other) { throw null; }
        public override int GetHashCode() { throw null; }
    }
    public readonly partial struct SequencePointCollection : System.Collections.Generic.IEnumerable<System.Reflection.Metadata.SequencePoint>, System.Collections.IEnumerable
    {
        private readonly int _dummyPrimitive;
        public System.Reflection.Metadata.SequencePointCollection.Enumerator GetEnumerator() { throw null; }
        System.Collections.Generic.IEnumerator<System.Reflection.Metadata.SequencePoint> System.Collections.Generic.IEnumerable<System.Reflection.Metadata.SequencePoint>.GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        public partial struct Enumerator : System.Collections.Generic.IEnumerator<System.Reflection.Metadata.SequencePoint>, System.Collections.IEnumerator, System.IDisposable
        {
            private int _dummyPrimitive;
            public System.Reflection.Metadata.SequencePoint Current { get { throw null; } }
            object System.Collections.IEnumerator.Current { get { throw null; } }
            public bool MoveNext() { throw null; }
            public void Reset() { }
            void System.IDisposable.Dispose() { }
        }
    }
    public enum SerializationTypeCode : byte
    {
        Invalid = (byte)0,
        Boolean = (byte)2,
        Char = (byte)3,
        SByte = (byte)4,
        Byte = (byte)5,
        Int16 = (byte)6,
        UInt16 = (byte)7,
        Int32 = (byte)8,
        UInt32 = (byte)9,
        Int64 = (byte)10,
        UInt64 = (byte)11,
        Single = (byte)12,
        Double = (byte)13,
        String = (byte)14,
        SZArray = (byte)29,
        Type = (byte)80,
        TaggedObject = (byte)81,
        Enum = (byte)85,
    }
    [System.FlagsAttribute]
    public enum SignatureAttributes : byte
    {
        None = (byte)0,
        Generic = (byte)16,
        Instance = (byte)32,
        ExplicitThis = (byte)64,
    }
    public enum SignatureCallingConvention : byte
    {
        Default = (byte)0,
        CDecl = (byte)1,
        StdCall = (byte)2,
        ThisCall = (byte)3,
        FastCall = (byte)4,
        VarArgs = (byte)5,
        Unmanaged = (byte)9,
    }
    public partial struct SignatureHeader : System.IEquatable<System.Reflection.Metadata.SignatureHeader>
    {
        private int _dummyPrimitive;
        public const byte CallingConventionOrKindMask = (byte)15;
        public SignatureHeader(byte rawValue) { throw null; }
        public SignatureHeader(System.Reflection.Metadata.SignatureKind kind, System.Reflection.Metadata.SignatureCallingConvention convention, System.Reflection.Metadata.SignatureAttributes attributes) { throw null; }
        public System.Reflection.Metadata.SignatureAttributes Attributes { get { throw null; } }
        public System.Reflection.Metadata.SignatureCallingConvention CallingConvention { get { throw null; } }
        public bool HasExplicitThis { get { throw null; } }
        public bool IsGeneric { get { throw null; } }
        public bool IsInstance { get { throw null; } }
        public System.Reflection.Metadata.SignatureKind Kind { get { throw null; } }
        public byte RawValue { get { throw null; } }
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] object? obj) { throw null; }
        public bool Equals(System.Reflection.Metadata.SignatureHeader other) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Reflection.Metadata.SignatureHeader left, System.Reflection.Metadata.SignatureHeader right) { throw null; }
        public static bool operator !=(System.Reflection.Metadata.SignatureHeader left, System.Reflection.Metadata.SignatureHeader right) { throw null; }
        public override string ToString() { throw null; }
    }
    public enum SignatureKind : byte
    {
        Method = (byte)0,
        Field = (byte)6,
        LocalVariables = (byte)7,
        Property = (byte)8,
        MethodSpecification = (byte)10,
    }
    public enum SignatureTypeCode : byte
    {
        Invalid = (byte)0,
        Void = (byte)1,
        Boolean = (byte)2,
        Char = (byte)3,
        SByte = (byte)4,
        Byte = (byte)5,
        Int16 = (byte)6,
        UInt16 = (byte)7,
        Int32 = (byte)8,
        UInt32 = (byte)9,
        Int64 = (byte)10,
        UInt64 = (byte)11,
        Single = (byte)12,
        Double = (byte)13,
        String = (byte)14,
        Pointer = (byte)15,
        ByReference = (byte)16,
        GenericTypeParameter = (byte)19,
        Array = (byte)20,
        GenericTypeInstance = (byte)21,
        TypedReference = (byte)22,
        IntPtr = (byte)24,
        UIntPtr = (byte)25,
        FunctionPointer = (byte)27,
        Object = (byte)28,
        SZArray = (byte)29,
        GenericMethodParameter = (byte)30,
        RequiredModifier = (byte)31,
        OptionalModifier = (byte)32,
        TypeHandle = (byte)64,
        Sentinel = (byte)65,
        Pinned = (byte)69,
    }
    public enum SignatureTypeKind : byte
    {
        Unknown = (byte)0,
        ValueType = (byte)17,
        Class = (byte)18,
    }
    public readonly partial struct StandaloneSignature
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public System.Reflection.Metadata.BlobHandle Signature { get { throw null; } }
        public System.Collections.Immutable.ImmutableArray<TType> DecodeLocalSignature<TType, TGenericContext>(System.Reflection.Metadata.ISignatureTypeProvider<TType, TGenericContext> provider, TGenericContext genericContext) { throw null; }
        public System.Reflection.Metadata.MethodSignature<TType> DecodeMethodSignature<TType, TGenericContext>(System.Reflection.Metadata.ISignatureTypeProvider<TType, TGenericContext> provider, TGenericContext genericContext) { throw null; }
        public System.Reflection.Metadata.CustomAttributeHandleCollection GetCustomAttributes() { throw null; }
        public System.Reflection.Metadata.StandaloneSignatureKind GetKind() { throw null; }
    }
    public readonly partial struct StandaloneSignatureHandle : System.IEquatable<System.Reflection.Metadata.StandaloneSignatureHandle>
    {
        private readonly int _dummyPrimitive;
        public bool IsNil { get { throw null; } }
        public override bool Equals(object? obj) { throw null; }
        public bool Equals(System.Reflection.Metadata.StandaloneSignatureHandle other) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Reflection.Metadata.StandaloneSignatureHandle left, System.Reflection.Metadata.StandaloneSignatureHandle right) { throw null; }
        public static explicit operator System.Reflection.Metadata.StandaloneSignatureHandle (System.Reflection.Metadata.EntityHandle handle) { throw null; }
        public static explicit operator System.Reflection.Metadata.StandaloneSignatureHandle (System.Reflection.Metadata.Handle handle) { throw null; }
        public static implicit operator System.Reflection.Metadata.EntityHandle (System.Reflection.Metadata.StandaloneSignatureHandle handle) { throw null; }
        public static implicit operator System.Reflection.Metadata.Handle (System.Reflection.Metadata.StandaloneSignatureHandle handle) { throw null; }
        public static bool operator !=(System.Reflection.Metadata.StandaloneSignatureHandle left, System.Reflection.Metadata.StandaloneSignatureHandle right) { throw null; }
    }
    public enum StandaloneSignatureKind
    {
        Method = 0,
        LocalVariables = 1,
    }
    public readonly partial struct StringHandle : System.IEquatable<System.Reflection.Metadata.StringHandle>
    {
        private readonly int _dummyPrimitive;
        public bool IsNil { get { throw null; } }
        public override bool Equals(object? obj) { throw null; }
        public bool Equals(System.Reflection.Metadata.StringHandle other) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Reflection.Metadata.StringHandle left, System.Reflection.Metadata.StringHandle right) { throw null; }
        public static explicit operator System.Reflection.Metadata.StringHandle (System.Reflection.Metadata.Handle handle) { throw null; }
        public static implicit operator System.Reflection.Metadata.Handle (System.Reflection.Metadata.StringHandle handle) { throw null; }
        public static bool operator !=(System.Reflection.Metadata.StringHandle left, System.Reflection.Metadata.StringHandle right) { throw null; }
    }
    public readonly partial struct TypeDefinition
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public System.Reflection.TypeAttributes Attributes { get { throw null; } }
        public System.Reflection.Metadata.EntityHandle BaseType { get { throw null; } }
        public bool IsNested { get { throw null; } }
        public System.Reflection.Metadata.StringHandle Name { get { throw null; } }
        public System.Reflection.Metadata.StringHandle Namespace { get { throw null; } }
        public System.Reflection.Metadata.NamespaceDefinitionHandle NamespaceDefinition { get { throw null; } }
        public System.Reflection.Metadata.CustomAttributeHandleCollection GetCustomAttributes() { throw null; }
        public System.Reflection.Metadata.DeclarativeSecurityAttributeHandleCollection GetDeclarativeSecurityAttributes() { throw null; }
        public System.Reflection.Metadata.TypeDefinitionHandle GetDeclaringType() { throw null; }
        public System.Reflection.Metadata.EventDefinitionHandleCollection GetEvents() { throw null; }
        public System.Reflection.Metadata.FieldDefinitionHandleCollection GetFields() { throw null; }
        public System.Reflection.Metadata.GenericParameterHandleCollection GetGenericParameters() { throw null; }
        public System.Reflection.Metadata.InterfaceImplementationHandleCollection GetInterfaceImplementations() { throw null; }
        public System.Reflection.Metadata.TypeLayout GetLayout() { throw null; }
        public System.Reflection.Metadata.MethodImplementationHandleCollection GetMethodImplementations() { throw null; }
        public System.Reflection.Metadata.MethodDefinitionHandleCollection GetMethods() { throw null; }
        public System.Collections.Immutable.ImmutableArray<System.Reflection.Metadata.TypeDefinitionHandle> GetNestedTypes() { throw null; }
        public System.Reflection.Metadata.PropertyDefinitionHandleCollection GetProperties() { throw null; }
    }
    public readonly partial struct TypeDefinitionHandle : System.IEquatable<System.Reflection.Metadata.TypeDefinitionHandle>
    {
        private readonly int _dummyPrimitive;
        public bool IsNil { get { throw null; } }
        public override bool Equals(object? obj) { throw null; }
        public bool Equals(System.Reflection.Metadata.TypeDefinitionHandle other) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Reflection.Metadata.TypeDefinitionHandle left, System.Reflection.Metadata.TypeDefinitionHandle right) { throw null; }
        public static explicit operator System.Reflection.Metadata.TypeDefinitionHandle (System.Reflection.Metadata.EntityHandle handle) { throw null; }
        public static explicit operator System.Reflection.Metadata.TypeDefinitionHandle (System.Reflection.Metadata.Handle handle) { throw null; }
        public static implicit operator System.Reflection.Metadata.EntityHandle (System.Reflection.Metadata.TypeDefinitionHandle handle) { throw null; }
        public static implicit operator System.Reflection.Metadata.Handle (System.Reflection.Metadata.TypeDefinitionHandle handle) { throw null; }
        public static bool operator !=(System.Reflection.Metadata.TypeDefinitionHandle left, System.Reflection.Metadata.TypeDefinitionHandle right) { throw null; }
    }
    public readonly partial struct TypeDefinitionHandleCollection : System.Collections.Generic.IEnumerable<System.Reflection.Metadata.TypeDefinitionHandle>, System.Collections.Generic.IReadOnlyCollection<System.Reflection.Metadata.TypeDefinitionHandle>, System.Collections.IEnumerable
    {
        private readonly int _dummyPrimitive;
        public int Count { get { throw null; } }
        public System.Reflection.Metadata.TypeDefinitionHandleCollection.Enumerator GetEnumerator() { throw null; }
        System.Collections.Generic.IEnumerator<System.Reflection.Metadata.TypeDefinitionHandle> System.Collections.Generic.IEnumerable<System.Reflection.Metadata.TypeDefinitionHandle>.GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        public partial struct Enumerator : System.Collections.Generic.IEnumerator<System.Reflection.Metadata.TypeDefinitionHandle>, System.Collections.IEnumerator, System.IDisposable
        {
            private int _dummyPrimitive;
            public System.Reflection.Metadata.TypeDefinitionHandle Current { get { throw null; } }
            object System.Collections.IEnumerator.Current { get { throw null; } }
            public bool MoveNext() { throw null; }
            void System.Collections.IEnumerator.Reset() { }
            void System.IDisposable.Dispose() { }
        }
    }
    public readonly partial struct TypeLayout
    {
        private readonly int _dummyPrimitive;
        public TypeLayout(int size, int packingSize) { throw null; }
        public bool IsDefault { get { throw null; } }
        public int PackingSize { get { throw null; } }
        public int Size { get { throw null; } }
    }
    public sealed partial class TypeName
    {
        internal TypeName() { }
        public string AssemblyQualifiedName { get { throw null; } }
        public System.Reflection.Metadata.TypeName? ContainingType { get { throw null; } }
        public bool IsArray { get { throw null; } }
        public bool IsConstructedGenericType { get { throw null; } }
        public bool IsElementalType { get { throw null; } }
        public bool IsManagedPointerType { get { throw null; } }
        public bool IsNestedType { get { throw null; } }
        public bool IsSzArrayType { get { throw null; } }
        public bool IsUnmanagedPointerType { get { throw null; } }
        public bool IsVariableBoundArrayType { get { throw null; } }
        public string Name { get { throw null; } }
        public int TotalComplexity { get; }
        public System.Reflection.Metadata.TypeName? UnderlyingType { get { throw null; } }
        public static System.Reflection.Metadata.TypeName Parse(System.ReadOnlySpan<char> typeName, System.Reflection.Metadata.TypeNameParserOptions? options = null) { throw null; }
        public static bool TryParse(System.ReadOnlySpan<char> typeName, [System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] out System.Reflection.Metadata.TypeName? result, System.Reflection.Metadata.TypeNameParserOptions? options = null) { throw null; }
        public int GetArrayRank() { throw null; }
        public System.Reflection.AssemblyName? GetAssemblyName() { throw null; }
        public System.ReadOnlySpan<System.Reflection.Metadata.TypeName> GetGenericArguments() { throw null; }
    }
    public sealed partial class TypeNameParserOptions
    {
        public TypeNameParserOptions() { }
        public bool AllowFullyQualifiedName { get { throw null; } set { } }
        public int MaxRecursiveDepth { get { throw null; } set { } }
        public bool StrictValidation { get; set; }
    }
    public readonly partial struct TypeReference
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public System.Reflection.Metadata.StringHandle Name { get { throw null; } }
        public System.Reflection.Metadata.StringHandle Namespace { get { throw null; } }
        public System.Reflection.Metadata.EntityHandle ResolutionScope { get { throw null; } }
    }
    public readonly partial struct TypeReferenceHandle : System.IEquatable<System.Reflection.Metadata.TypeReferenceHandle>
    {
        private readonly int _dummyPrimitive;
        public bool IsNil { get { throw null; } }
        public override bool Equals(object? obj) { throw null; }
        public bool Equals(System.Reflection.Metadata.TypeReferenceHandle other) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Reflection.Metadata.TypeReferenceHandle left, System.Reflection.Metadata.TypeReferenceHandle right) { throw null; }
        public static explicit operator System.Reflection.Metadata.TypeReferenceHandle (System.Reflection.Metadata.EntityHandle handle) { throw null; }
        public static explicit operator System.Reflection.Metadata.TypeReferenceHandle (System.Reflection.Metadata.Handle handle) { throw null; }
        public static implicit operator System.Reflection.Metadata.EntityHandle (System.Reflection.Metadata.TypeReferenceHandle handle) { throw null; }
        public static implicit operator System.Reflection.Metadata.Handle (System.Reflection.Metadata.TypeReferenceHandle handle) { throw null; }
        public static bool operator !=(System.Reflection.Metadata.TypeReferenceHandle left, System.Reflection.Metadata.TypeReferenceHandle right) { throw null; }
    }
    public readonly partial struct TypeReferenceHandleCollection : System.Collections.Generic.IEnumerable<System.Reflection.Metadata.TypeReferenceHandle>, System.Collections.Generic.IReadOnlyCollection<System.Reflection.Metadata.TypeReferenceHandle>, System.Collections.IEnumerable
    {
        private readonly int _dummyPrimitive;
        public int Count { get { throw null; } }
        public System.Reflection.Metadata.TypeReferenceHandleCollection.Enumerator GetEnumerator() { throw null; }
        System.Collections.Generic.IEnumerator<System.Reflection.Metadata.TypeReferenceHandle> System.Collections.Generic.IEnumerable<System.Reflection.Metadata.TypeReferenceHandle>.GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        public partial struct Enumerator : System.Collections.Generic.IEnumerator<System.Reflection.Metadata.TypeReferenceHandle>, System.Collections.IEnumerator, System.IDisposable
        {
            private int _dummyPrimitive;
            public System.Reflection.Metadata.TypeReferenceHandle Current { get { throw null; } }
            object System.Collections.IEnumerator.Current { get { throw null; } }
            public bool MoveNext() { throw null; }
            void System.Collections.IEnumerator.Reset() { }
            void System.IDisposable.Dispose() { }
        }
    }
    public readonly partial struct TypeSpecification
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public System.Reflection.Metadata.BlobHandle Signature { get { throw null; } }
        public TType DecodeSignature<TType, TGenericContext>(System.Reflection.Metadata.ISignatureTypeProvider<TType, TGenericContext> provider, TGenericContext genericContext) { throw null; }
        public System.Reflection.Metadata.CustomAttributeHandleCollection GetCustomAttributes() { throw null; }
    }
    public readonly partial struct TypeSpecificationHandle : System.IEquatable<System.Reflection.Metadata.TypeSpecificationHandle>
    {
        private readonly int _dummyPrimitive;
        public bool IsNil { get { throw null; } }
        public override bool Equals(object? obj) { throw null; }
        public bool Equals(System.Reflection.Metadata.TypeSpecificationHandle other) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Reflection.Metadata.TypeSpecificationHandle left, System.Reflection.Metadata.TypeSpecificationHandle right) { throw null; }
        public static explicit operator System.Reflection.Metadata.TypeSpecificationHandle (System.Reflection.Metadata.EntityHandle handle) { throw null; }
        public static explicit operator System.Reflection.Metadata.TypeSpecificationHandle (System.Reflection.Metadata.Handle handle) { throw null; }
        public static implicit operator System.Reflection.Metadata.EntityHandle (System.Reflection.Metadata.TypeSpecificationHandle handle) { throw null; }
        public static implicit operator System.Reflection.Metadata.Handle (System.Reflection.Metadata.TypeSpecificationHandle handle) { throw null; }
        public static bool operator !=(System.Reflection.Metadata.TypeSpecificationHandle left, System.Reflection.Metadata.TypeSpecificationHandle right) { throw null; }
    }
    public readonly partial struct UserStringHandle : System.IEquatable<System.Reflection.Metadata.UserStringHandle>
    {
        private readonly int _dummyPrimitive;
        public bool IsNil { get { throw null; } }
        public override bool Equals(object? obj) { throw null; }
        public bool Equals(System.Reflection.Metadata.UserStringHandle other) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Reflection.Metadata.UserStringHandle left, System.Reflection.Metadata.UserStringHandle right) { throw null; }
        public static explicit operator System.Reflection.Metadata.UserStringHandle (System.Reflection.Metadata.Handle handle) { throw null; }
        public static implicit operator System.Reflection.Metadata.Handle (System.Reflection.Metadata.UserStringHandle handle) { throw null; }
        public static bool operator !=(System.Reflection.Metadata.UserStringHandle left, System.Reflection.Metadata.UserStringHandle right) { throw null; }
    }
}
namespace System.Reflection.Metadata.Ecma335
{
    public readonly partial struct ArrayShapeEncoder
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public ArrayShapeEncoder(System.Reflection.Metadata.BlobBuilder builder) { throw null; }
        public System.Reflection.Metadata.BlobBuilder Builder { get { throw null; } }
        public void Shape(int rank, System.Collections.Immutable.ImmutableArray<int> sizes, System.Collections.Immutable.ImmutableArray<int> lowerBounds) { }
    }
    public readonly partial struct BlobEncoder
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public BlobEncoder(System.Reflection.Metadata.BlobBuilder builder) { throw null; }
        public System.Reflection.Metadata.BlobBuilder Builder { get { throw null; } }
        public void CustomAttributeSignature(System.Action<System.Reflection.Metadata.Ecma335.FixedArgumentsEncoder> fixedArguments, System.Action<System.Reflection.Metadata.Ecma335.CustomAttributeNamedArgumentsEncoder> namedArguments) { }
        public void CustomAttributeSignature(out System.Reflection.Metadata.Ecma335.FixedArgumentsEncoder fixedArguments, out System.Reflection.Metadata.Ecma335.CustomAttributeNamedArgumentsEncoder namedArguments) { throw null; }
        public System.Reflection.Metadata.Ecma335.FieldTypeEncoder Field() { throw null; }
        public System.Reflection.Metadata.Ecma335.SignatureTypeEncoder FieldSignature() { throw null; }
        public System.Reflection.Metadata.Ecma335.LocalVariablesEncoder LocalVariableSignature(int variableCount) { throw null; }
        public System.Reflection.Metadata.Ecma335.MethodSignatureEncoder MethodSignature(System.Reflection.Metadata.SignatureCallingConvention convention = System.Reflection.Metadata.SignatureCallingConvention.Default, int genericParameterCount = 0, bool isInstanceMethod = false) { throw null; }
        public System.Reflection.Metadata.Ecma335.GenericTypeArgumentsEncoder MethodSpecificationSignature(int genericArgumentCount) { throw null; }
        public System.Reflection.Metadata.Ecma335.NamedArgumentsEncoder PermissionSetArguments(int argumentCount) { throw null; }
        public System.Reflection.Metadata.Ecma335.PermissionSetEncoder PermissionSetBlob(int attributeCount) { throw null; }
        public System.Reflection.Metadata.Ecma335.MethodSignatureEncoder PropertySignature(bool isInstanceProperty = false) { throw null; }
        public System.Reflection.Metadata.Ecma335.SignatureTypeEncoder TypeSpecificationSignature() { throw null; }
    }
    public static partial class CodedIndex
    {
        public static int CustomAttributeType(System.Reflection.Metadata.EntityHandle handle) { throw null; }
        public static int HasConstant(System.Reflection.Metadata.EntityHandle handle) { throw null; }
        public static int HasCustomAttribute(System.Reflection.Metadata.EntityHandle handle) { throw null; }
        public static int HasCustomDebugInformation(System.Reflection.Metadata.EntityHandle handle) { throw null; }
        public static int HasDeclSecurity(System.Reflection.Metadata.EntityHandle handle) { throw null; }
        public static int HasFieldMarshal(System.Reflection.Metadata.EntityHandle handle) { throw null; }
        public static int HasSemantics(System.Reflection.Metadata.EntityHandle handle) { throw null; }
        public static int Implementation(System.Reflection.Metadata.EntityHandle handle) { throw null; }
        public static int MemberForwarded(System.Reflection.Metadata.EntityHandle handle) { throw null; }
        public static int MemberRefParent(System.Reflection.Metadata.EntityHandle handle) { throw null; }
        public static int MethodDefOrRef(System.Reflection.Metadata.EntityHandle handle) { throw null; }
        public static int ResolutionScope(System.Reflection.Metadata.EntityHandle handle) { throw null; }
        public static int TypeDefOrRef(System.Reflection.Metadata.EntityHandle handle) { throw null; }
        public static int TypeDefOrRefOrSpec(System.Reflection.Metadata.EntityHandle handle) { throw null; }
        public static int TypeOrMethodDef(System.Reflection.Metadata.EntityHandle handle) { throw null; }
    }
    public sealed partial class ControlFlowBuilder
    {
        public ControlFlowBuilder() { }
        public void AddCatchRegion(System.Reflection.Metadata.Ecma335.LabelHandle tryStart, System.Reflection.Metadata.Ecma335.LabelHandle tryEnd, System.Reflection.Metadata.Ecma335.LabelHandle handlerStart, System.Reflection.Metadata.Ecma335.LabelHandle handlerEnd, System.Reflection.Metadata.EntityHandle catchType) { }
        public void AddFaultRegion(System.Reflection.Metadata.Ecma335.LabelHandle tryStart, System.Reflection.Metadata.Ecma335.LabelHandle tryEnd, System.Reflection.Metadata.Ecma335.LabelHandle handlerStart, System.Reflection.Metadata.Ecma335.LabelHandle handlerEnd) { }
        public void AddFilterRegion(System.Reflection.Metadata.Ecma335.LabelHandle tryStart, System.Reflection.Metadata.Ecma335.LabelHandle tryEnd, System.Reflection.Metadata.Ecma335.LabelHandle handlerStart, System.Reflection.Metadata.Ecma335.LabelHandle handlerEnd, System.Reflection.Metadata.Ecma335.LabelHandle filterStart) { }
        public void AddFinallyRegion(System.Reflection.Metadata.Ecma335.LabelHandle tryStart, System.Reflection.Metadata.Ecma335.LabelHandle tryEnd, System.Reflection.Metadata.Ecma335.LabelHandle handlerStart, System.Reflection.Metadata.Ecma335.LabelHandle handlerEnd) { }
        public void Clear() { }
    }
    public readonly partial struct CustomAttributeArrayTypeEncoder
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public CustomAttributeArrayTypeEncoder(System.Reflection.Metadata.BlobBuilder builder) { throw null; }
        public System.Reflection.Metadata.BlobBuilder Builder { get { throw null; } }
        public System.Reflection.Metadata.Ecma335.CustomAttributeElementTypeEncoder ElementType() { throw null; }
        public void ObjectArray() { }
    }
    public readonly partial struct CustomAttributeElementTypeEncoder
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public CustomAttributeElementTypeEncoder(System.Reflection.Metadata.BlobBuilder builder) { throw null; }
        public System.Reflection.Metadata.BlobBuilder Builder { get { throw null; } }
        public void Boolean() { }
        public void Byte() { }
        public void Char() { }
        public void Double() { }
        public void Enum(string enumTypeName) { }
        public void Int16() { }
        public void Int32() { }
        public void Int64() { }
        public void PrimitiveType(System.Reflection.Metadata.PrimitiveSerializationTypeCode type) { }
        public void SByte() { }
        public void Single() { }
        public void String() { }
        public void SystemType() { }
        public void UInt16() { }
        public void UInt32() { }
        public void UInt64() { }
    }
    public readonly partial struct CustomAttributeNamedArgumentsEncoder
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public CustomAttributeNamedArgumentsEncoder(System.Reflection.Metadata.BlobBuilder builder) { throw null; }
        public System.Reflection.Metadata.BlobBuilder Builder { get { throw null; } }
        public System.Reflection.Metadata.Ecma335.NamedArgumentsEncoder Count(int count) { throw null; }
    }
    public readonly partial struct CustomModifiersEncoder
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public CustomModifiersEncoder(System.Reflection.Metadata.BlobBuilder builder) { throw null; }
        public System.Reflection.Metadata.BlobBuilder Builder { get { throw null; } }
        public System.Reflection.Metadata.Ecma335.CustomModifiersEncoder AddModifier(System.Reflection.Metadata.EntityHandle type, bool isOptional) { throw null; }
    }
    public readonly partial struct EditAndContinueLogEntry : System.IEquatable<System.Reflection.Metadata.Ecma335.EditAndContinueLogEntry>
    {
        private readonly int _dummyPrimitive;
        public EditAndContinueLogEntry(System.Reflection.Metadata.EntityHandle handle, System.Reflection.Metadata.Ecma335.EditAndContinueOperation operation) { throw null; }
        public System.Reflection.Metadata.EntityHandle Handle { get { throw null; } }
        public System.Reflection.Metadata.Ecma335.EditAndContinueOperation Operation { get { throw null; } }
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] object? obj) { throw null; }
        public bool Equals(System.Reflection.Metadata.Ecma335.EditAndContinueLogEntry other) { throw null; }
        public override int GetHashCode() { throw null; }
    }
    public enum EditAndContinueOperation
    {
        Default = 0,
        AddMethod = 1,
        AddField = 2,
        AddParameter = 3,
        AddProperty = 4,
        AddEvent = 5,
    }
    public readonly partial struct ExceptionRegionEncoder
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public System.Reflection.Metadata.BlobBuilder Builder { get { throw null; } }
        public bool HasSmallFormat { get { throw null; } }
        public System.Reflection.Metadata.Ecma335.ExceptionRegionEncoder Add(System.Reflection.Metadata.ExceptionRegionKind kind, int tryOffset, int tryLength, int handlerOffset, int handlerLength, System.Reflection.Metadata.EntityHandle catchType = default(System.Reflection.Metadata.EntityHandle), int filterOffset = 0) { throw null; }
        public System.Reflection.Metadata.Ecma335.ExceptionRegionEncoder AddCatch(int tryOffset, int tryLength, int handlerOffset, int handlerLength, System.Reflection.Metadata.EntityHandle catchType) { throw null; }
        public System.Reflection.Metadata.Ecma335.ExceptionRegionEncoder AddFault(int tryOffset, int tryLength, int handlerOffset, int handlerLength) { throw null; }
        public System.Reflection.Metadata.Ecma335.ExceptionRegionEncoder AddFilter(int tryOffset, int tryLength, int handlerOffset, int handlerLength, int filterOffset) { throw null; }
        public System.Reflection.Metadata.Ecma335.ExceptionRegionEncoder AddFinally(int tryOffset, int tryLength, int handlerOffset, int handlerLength) { throw null; }
        public static bool IsSmallExceptionRegion(int startOffset, int length) { throw null; }
        public static bool IsSmallRegionCount(int exceptionRegionCount) { throw null; }
    }
    public static partial class ExportedTypeExtensions
    {
        public static int GetTypeDefinitionId(this System.Reflection.Metadata.ExportedType exportedType) { throw null; }
    }
    public readonly partial struct FieldTypeEncoder
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public FieldTypeEncoder(System.Reflection.Metadata.BlobBuilder builder) { throw null;  }
        public System.Reflection.Metadata.BlobBuilder Builder { get { throw null; } }
        public System.Reflection.Metadata.Ecma335.CustomModifiersEncoder CustomModifiers() { throw null; }
        public System.Reflection.Metadata.Ecma335.SignatureTypeEncoder Type(bool isByRef = false) { throw null; }
        public void TypedReference() { throw null; }
    }
    public readonly partial struct FixedArgumentsEncoder
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public FixedArgumentsEncoder(System.Reflection.Metadata.BlobBuilder builder) { throw null; }
        public System.Reflection.Metadata.BlobBuilder Builder { get { throw null; } }
        public System.Reflection.Metadata.Ecma335.LiteralEncoder AddArgument() { throw null; }
    }
    public enum FunctionPointerAttributes
    {
        None = 0,
        HasThis = 32,
        HasExplicitThis = 96,
    }
    public readonly partial struct GenericTypeArgumentsEncoder
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public GenericTypeArgumentsEncoder(System.Reflection.Metadata.BlobBuilder builder) { throw null; }
        public System.Reflection.Metadata.BlobBuilder Builder { get { throw null; } }
        public System.Reflection.Metadata.Ecma335.SignatureTypeEncoder AddArgument() { throw null; }
    }
    public enum HeapIndex
    {
        UserString = 0,
        String = 1,
        Blob = 2,
        Guid = 3,
    }
    public readonly partial struct InstructionEncoder
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public InstructionEncoder(System.Reflection.Metadata.BlobBuilder codeBuilder, System.Reflection.Metadata.Ecma335.ControlFlowBuilder? controlFlowBuilder = null) { throw null; }
        public System.Reflection.Metadata.BlobBuilder CodeBuilder { get { throw null; } }
        public System.Reflection.Metadata.Ecma335.ControlFlowBuilder? ControlFlowBuilder { get { throw null; } }
        public int Offset { get { throw null; } }
        public void Branch(System.Reflection.Metadata.ILOpCode code, System.Reflection.Metadata.Ecma335.LabelHandle label) { }
        public void Call(System.Reflection.Metadata.EntityHandle methodHandle) { }
        public void Call(System.Reflection.Metadata.MemberReferenceHandle methodHandle) { }
        public void Call(System.Reflection.Metadata.MethodDefinitionHandle methodHandle) { }
        public void Call(System.Reflection.Metadata.MethodSpecificationHandle methodHandle) { }
        public void CallIndirect(System.Reflection.Metadata.StandaloneSignatureHandle signature) { }
        public System.Reflection.Metadata.Ecma335.LabelHandle DefineLabel() { throw null; }
        public void LoadArgument(int argumentIndex) { }
        public void LoadArgumentAddress(int argumentIndex) { }
        public void LoadConstantI4(int value) { }
        public void LoadConstantI8(long value) { }
        public void LoadConstantR4(float value) { }
        public void LoadConstantR8(double value) { }
        public void LoadLocal(int slotIndex) { }
        public void LoadLocalAddress(int slotIndex) { }
        public void LoadString(System.Reflection.Metadata.UserStringHandle handle) { }
        public void MarkLabel(System.Reflection.Metadata.Ecma335.LabelHandle label) { }
        public void OpCode(System.Reflection.Metadata.ILOpCode code) { }
        public void StoreArgument(int argumentIndex) { }
        public void StoreLocal(int slotIndex) { }
        public System.Reflection.Metadata.Ecma335.SwitchInstructionEncoder Switch(int branchCount) { throw null; }
        public void Token(int token) { }
        public void Token(System.Reflection.Metadata.EntityHandle handle) { }
    }
    public readonly partial struct LabelHandle : System.IEquatable<System.Reflection.Metadata.Ecma335.LabelHandle>
    {
        private readonly int _dummyPrimitive;
        public int Id { get { throw null; } }
        public bool IsNil { get { throw null; } }
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] object? obj) { throw null; }
        public bool Equals(System.Reflection.Metadata.Ecma335.LabelHandle other) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Reflection.Metadata.Ecma335.LabelHandle left, System.Reflection.Metadata.Ecma335.LabelHandle right) { throw null; }
        public static bool operator !=(System.Reflection.Metadata.Ecma335.LabelHandle left, System.Reflection.Metadata.Ecma335.LabelHandle right) { throw null; }
    }
    public readonly partial struct LiteralEncoder
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public LiteralEncoder(System.Reflection.Metadata.BlobBuilder builder) { throw null; }
        public System.Reflection.Metadata.BlobBuilder Builder { get { throw null; } }
        public System.Reflection.Metadata.Ecma335.ScalarEncoder Scalar() { throw null; }
        public void TaggedScalar(System.Action<System.Reflection.Metadata.Ecma335.CustomAttributeElementTypeEncoder> type, System.Action<System.Reflection.Metadata.Ecma335.ScalarEncoder> scalar) { }
        public void TaggedScalar(out System.Reflection.Metadata.Ecma335.CustomAttributeElementTypeEncoder type, out System.Reflection.Metadata.Ecma335.ScalarEncoder scalar) { throw null; }
        public void TaggedVector(System.Action<System.Reflection.Metadata.Ecma335.CustomAttributeArrayTypeEncoder> arrayType, System.Action<System.Reflection.Metadata.Ecma335.VectorEncoder> vector) { }
        public void TaggedVector(out System.Reflection.Metadata.Ecma335.CustomAttributeArrayTypeEncoder arrayType, out System.Reflection.Metadata.Ecma335.VectorEncoder vector) { throw null; }
        public System.Reflection.Metadata.Ecma335.VectorEncoder Vector() { throw null; }
    }
    public readonly partial struct LiteralsEncoder
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public LiteralsEncoder(System.Reflection.Metadata.BlobBuilder builder) { throw null; }
        public System.Reflection.Metadata.BlobBuilder Builder { get { throw null; } }
        public System.Reflection.Metadata.Ecma335.LiteralEncoder AddLiteral() { throw null; }
    }
    public readonly partial struct LocalVariablesEncoder
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public LocalVariablesEncoder(System.Reflection.Metadata.BlobBuilder builder) { throw null; }
        public System.Reflection.Metadata.BlobBuilder Builder { get { throw null; } }
        public System.Reflection.Metadata.Ecma335.LocalVariableTypeEncoder AddVariable() { throw null; }
    }
    public readonly partial struct LocalVariableTypeEncoder
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public LocalVariableTypeEncoder(System.Reflection.Metadata.BlobBuilder builder) { throw null; }
        public System.Reflection.Metadata.BlobBuilder Builder { get { throw null; } }
        public System.Reflection.Metadata.Ecma335.CustomModifiersEncoder CustomModifiers() { throw null; }
        public System.Reflection.Metadata.Ecma335.SignatureTypeEncoder Type(bool isByRef = false, bool isPinned = false) { throw null; }
        public void TypedReference() { }
    }
    public sealed partial class MetadataAggregator
    {
        public MetadataAggregator(System.Collections.Generic.IReadOnlyList<int>? baseTableRowCounts, System.Collections.Generic.IReadOnlyList<int>? baseHeapSizes, System.Collections.Generic.IReadOnlyList<System.Reflection.Metadata.MetadataReader>? deltaReaders) { }
        public MetadataAggregator(System.Reflection.Metadata.MetadataReader baseReader, System.Collections.Generic.IReadOnlyList<System.Reflection.Metadata.MetadataReader> deltaReaders) { }
        public System.Reflection.Metadata.Handle GetGenerationHandle(System.Reflection.Metadata.Handle handle, out int generation) { throw null; }
    }
    public sealed partial class MetadataBuilder
    {
        public MetadataBuilder(int userStringHeapStartOffset = 0, int stringHeapStartOffset = 0, int blobHeapStartOffset = 0, int guidHeapStartOffset = 0) { }
        public System.Reflection.Metadata.AssemblyDefinitionHandle AddAssembly(System.Reflection.Metadata.StringHandle name, System.Version version, System.Reflection.Metadata.StringHandle culture, System.Reflection.Metadata.BlobHandle publicKey, System.Reflection.AssemblyFlags flags, System.Reflection.AssemblyHashAlgorithm hashAlgorithm) { throw null; }
        public System.Reflection.Metadata.AssemblyFileHandle AddAssemblyFile(System.Reflection.Metadata.StringHandle name, System.Reflection.Metadata.BlobHandle hashValue, bool containsMetadata) { throw null; }
        public System.Reflection.Metadata.AssemblyReferenceHandle AddAssemblyReference(System.Reflection.Metadata.StringHandle name, System.Version version, System.Reflection.Metadata.StringHandle culture, System.Reflection.Metadata.BlobHandle publicKeyOrToken, System.Reflection.AssemblyFlags flags, System.Reflection.Metadata.BlobHandle hashValue) { throw null; }
        public System.Reflection.Metadata.ConstantHandle AddConstant(System.Reflection.Metadata.EntityHandle parent, object? value) { throw null; }
        public System.Reflection.Metadata.CustomAttributeHandle AddCustomAttribute(System.Reflection.Metadata.EntityHandle parent, System.Reflection.Metadata.EntityHandle constructor, System.Reflection.Metadata.BlobHandle value) { throw null; }
        public System.Reflection.Metadata.CustomDebugInformationHandle AddCustomDebugInformation(System.Reflection.Metadata.EntityHandle parent, System.Reflection.Metadata.GuidHandle kind, System.Reflection.Metadata.BlobHandle value) { throw null; }
        public System.Reflection.Metadata.DeclarativeSecurityAttributeHandle AddDeclarativeSecurityAttribute(System.Reflection.Metadata.EntityHandle parent, System.Reflection.DeclarativeSecurityAction action, System.Reflection.Metadata.BlobHandle permissionSet) { throw null; }
        public System.Reflection.Metadata.DocumentHandle AddDocument(System.Reflection.Metadata.BlobHandle name, System.Reflection.Metadata.GuidHandle hashAlgorithm, System.Reflection.Metadata.BlobHandle hash, System.Reflection.Metadata.GuidHandle language) { throw null; }
        public void AddEncLogEntry(System.Reflection.Metadata.EntityHandle entity, System.Reflection.Metadata.Ecma335.EditAndContinueOperation code) { }
        public void AddEncMapEntry(System.Reflection.Metadata.EntityHandle entity) { }
        public System.Reflection.Metadata.EventDefinitionHandle AddEvent(System.Reflection.EventAttributes attributes, System.Reflection.Metadata.StringHandle name, System.Reflection.Metadata.EntityHandle type) { throw null; }
        public void AddEventMap(System.Reflection.Metadata.TypeDefinitionHandle declaringType, System.Reflection.Metadata.EventDefinitionHandle eventList) { }
        public System.Reflection.Metadata.ExportedTypeHandle AddExportedType(System.Reflection.TypeAttributes attributes, System.Reflection.Metadata.StringHandle @namespace, System.Reflection.Metadata.StringHandle name, System.Reflection.Metadata.EntityHandle implementation, int typeDefinitionId) { throw null; }
        public System.Reflection.Metadata.FieldDefinitionHandle AddFieldDefinition(System.Reflection.FieldAttributes attributes, System.Reflection.Metadata.StringHandle name, System.Reflection.Metadata.BlobHandle signature) { throw null; }
        public void AddFieldLayout(System.Reflection.Metadata.FieldDefinitionHandle field, int offset) { }
        public void AddFieldRelativeVirtualAddress(System.Reflection.Metadata.FieldDefinitionHandle field, int offset) { }
        public System.Reflection.Metadata.GenericParameterHandle AddGenericParameter(System.Reflection.Metadata.EntityHandle parent, System.Reflection.GenericParameterAttributes attributes, System.Reflection.Metadata.StringHandle name, int index) { throw null; }
        public System.Reflection.Metadata.GenericParameterConstraintHandle AddGenericParameterConstraint(System.Reflection.Metadata.GenericParameterHandle genericParameter, System.Reflection.Metadata.EntityHandle constraint) { throw null; }
        public System.Reflection.Metadata.ImportScopeHandle AddImportScope(System.Reflection.Metadata.ImportScopeHandle parentScope, System.Reflection.Metadata.BlobHandle imports) { throw null; }
        public System.Reflection.Metadata.InterfaceImplementationHandle AddInterfaceImplementation(System.Reflection.Metadata.TypeDefinitionHandle type, System.Reflection.Metadata.EntityHandle implementedInterface) { throw null; }
        public System.Reflection.Metadata.LocalConstantHandle AddLocalConstant(System.Reflection.Metadata.StringHandle name, System.Reflection.Metadata.BlobHandle signature) { throw null; }
        public System.Reflection.Metadata.LocalScopeHandle AddLocalScope(System.Reflection.Metadata.MethodDefinitionHandle method, System.Reflection.Metadata.ImportScopeHandle importScope, System.Reflection.Metadata.LocalVariableHandle variableList, System.Reflection.Metadata.LocalConstantHandle constantList, int startOffset, int length) { throw null; }
        public System.Reflection.Metadata.LocalVariableHandle AddLocalVariable(System.Reflection.Metadata.LocalVariableAttributes attributes, int index, System.Reflection.Metadata.StringHandle name) { throw null; }
        public System.Reflection.Metadata.ManifestResourceHandle AddManifestResource(System.Reflection.ManifestResourceAttributes attributes, System.Reflection.Metadata.StringHandle name, System.Reflection.Metadata.EntityHandle implementation, uint offset) { throw null; }
        public void AddMarshallingDescriptor(System.Reflection.Metadata.EntityHandle parent, System.Reflection.Metadata.BlobHandle descriptor) { }
        public System.Reflection.Metadata.MemberReferenceHandle AddMemberReference(System.Reflection.Metadata.EntityHandle parent, System.Reflection.Metadata.StringHandle name, System.Reflection.Metadata.BlobHandle signature) { throw null; }
        public System.Reflection.Metadata.MethodDebugInformationHandle AddMethodDebugInformation(System.Reflection.Metadata.DocumentHandle document, System.Reflection.Metadata.BlobHandle sequencePoints) { throw null; }
        public System.Reflection.Metadata.MethodDefinitionHandle AddMethodDefinition(System.Reflection.MethodAttributes attributes, System.Reflection.MethodImplAttributes implAttributes, System.Reflection.Metadata.StringHandle name, System.Reflection.Metadata.BlobHandle signature, int bodyOffset, System.Reflection.Metadata.ParameterHandle parameterList) { throw null; }
        public System.Reflection.Metadata.MethodImplementationHandle AddMethodImplementation(System.Reflection.Metadata.TypeDefinitionHandle type, System.Reflection.Metadata.EntityHandle methodBody, System.Reflection.Metadata.EntityHandle methodDeclaration) { throw null; }
        public void AddMethodImport(System.Reflection.Metadata.MethodDefinitionHandle method, System.Reflection.MethodImportAttributes attributes, System.Reflection.Metadata.StringHandle name, System.Reflection.Metadata.ModuleReferenceHandle module) { }
        public void AddMethodSemantics(System.Reflection.Metadata.EntityHandle association, System.Reflection.MethodSemanticsAttributes semantics, System.Reflection.Metadata.MethodDefinitionHandle methodDefinition) { }
        public System.Reflection.Metadata.MethodSpecificationHandle AddMethodSpecification(System.Reflection.Metadata.EntityHandle method, System.Reflection.Metadata.BlobHandle instantiation) { throw null; }
        public System.Reflection.Metadata.ModuleDefinitionHandle AddModule(int generation, System.Reflection.Metadata.StringHandle moduleName, System.Reflection.Metadata.GuidHandle mvid, System.Reflection.Metadata.GuidHandle encId, System.Reflection.Metadata.GuidHandle encBaseId) { throw null; }
        public System.Reflection.Metadata.ModuleReferenceHandle AddModuleReference(System.Reflection.Metadata.StringHandle moduleName) { throw null; }
        public void AddNestedType(System.Reflection.Metadata.TypeDefinitionHandle type, System.Reflection.Metadata.TypeDefinitionHandle enclosingType) { }
        public System.Reflection.Metadata.ParameterHandle AddParameter(System.Reflection.ParameterAttributes attributes, System.Reflection.Metadata.StringHandle name, int sequenceNumber) { throw null; }
        public System.Reflection.Metadata.PropertyDefinitionHandle AddProperty(System.Reflection.PropertyAttributes attributes, System.Reflection.Metadata.StringHandle name, System.Reflection.Metadata.BlobHandle signature) { throw null; }
        public void AddPropertyMap(System.Reflection.Metadata.TypeDefinitionHandle declaringType, System.Reflection.Metadata.PropertyDefinitionHandle propertyList) { }
        public System.Reflection.Metadata.StandaloneSignatureHandle AddStandaloneSignature(System.Reflection.Metadata.BlobHandle signature) { throw null; }
        public void AddStateMachineMethod(System.Reflection.Metadata.MethodDefinitionHandle moveNextMethod, System.Reflection.Metadata.MethodDefinitionHandle kickoffMethod) { }
        public System.Reflection.Metadata.TypeDefinitionHandle AddTypeDefinition(System.Reflection.TypeAttributes attributes, System.Reflection.Metadata.StringHandle @namespace, System.Reflection.Metadata.StringHandle name, System.Reflection.Metadata.EntityHandle baseType, System.Reflection.Metadata.FieldDefinitionHandle fieldList, System.Reflection.Metadata.MethodDefinitionHandle methodList) { throw null; }
        public void AddTypeLayout(System.Reflection.Metadata.TypeDefinitionHandle type, ushort packingSize, uint size) { }
        public System.Reflection.Metadata.TypeReferenceHandle AddTypeReference(System.Reflection.Metadata.EntityHandle resolutionScope, System.Reflection.Metadata.StringHandle @namespace, System.Reflection.Metadata.StringHandle name) { throw null; }
        public System.Reflection.Metadata.TypeSpecificationHandle AddTypeSpecification(System.Reflection.Metadata.BlobHandle signature) { throw null; }
        public System.Reflection.Metadata.BlobHandle GetOrAddBlob(byte[] value) { throw null; }
        public System.Reflection.Metadata.BlobHandle GetOrAddBlob(System.Collections.Immutable.ImmutableArray<byte> value) { throw null; }
        public System.Reflection.Metadata.BlobHandle GetOrAddBlob(System.Reflection.Metadata.BlobBuilder value) { throw null; }
        public System.Reflection.Metadata.BlobHandle GetOrAddBlobUTF16(string value) { throw null; }
        public System.Reflection.Metadata.BlobHandle GetOrAddBlobUTF8(string value, bool allowUnpairedSurrogates = true) { throw null; }
        public System.Reflection.Metadata.BlobHandle GetOrAddConstantBlob(object? value) { throw null; }
        public System.Reflection.Metadata.BlobHandle GetOrAddDocumentName(string value) { throw null; }
        public System.Reflection.Metadata.GuidHandle GetOrAddGuid(System.Guid guid) { throw null; }
        public System.Reflection.Metadata.StringHandle GetOrAddString(string value) { throw null; }
        public System.Reflection.Metadata.UserStringHandle GetOrAddUserString(string value) { throw null; }
        public int GetRowCount(System.Reflection.Metadata.Ecma335.TableIndex table) { throw null; }
        public System.Collections.Immutable.ImmutableArray<int> GetRowCounts() { throw null; }
        public System.Reflection.Metadata.ReservedBlob<System.Reflection.Metadata.GuidHandle> ReserveGuid() { throw null; }
        public System.Reflection.Metadata.ReservedBlob<System.Reflection.Metadata.UserStringHandle> ReserveUserString(int length) { throw null; }
        public void SetCapacity(System.Reflection.Metadata.Ecma335.HeapIndex heap, int byteCount) { }
        public void SetCapacity(System.Reflection.Metadata.Ecma335.TableIndex table, int rowCount) { }
    }
    public static partial class MetadataReaderExtensions
    {
        public static System.Collections.Generic.IEnumerable<System.Reflection.Metadata.Ecma335.EditAndContinueLogEntry> GetEditAndContinueLogEntries(this System.Reflection.Metadata.MetadataReader reader) { throw null; }
        public static System.Collections.Generic.IEnumerable<System.Reflection.Metadata.EntityHandle> GetEditAndContinueMapEntries(this System.Reflection.Metadata.MetadataReader reader) { throw null; }
        public static int GetHeapMetadataOffset(this System.Reflection.Metadata.MetadataReader reader, System.Reflection.Metadata.Ecma335.HeapIndex heapIndex) { throw null; }
        public static int GetHeapSize(this System.Reflection.Metadata.MetadataReader reader, System.Reflection.Metadata.Ecma335.HeapIndex heapIndex) { throw null; }
        public static System.Reflection.Metadata.BlobHandle GetNextHandle(this System.Reflection.Metadata.MetadataReader reader, System.Reflection.Metadata.BlobHandle handle) { throw null; }
        public static System.Reflection.Metadata.StringHandle GetNextHandle(this System.Reflection.Metadata.MetadataReader reader, System.Reflection.Metadata.StringHandle handle) { throw null; }
        public static System.Reflection.Metadata.UserStringHandle GetNextHandle(this System.Reflection.Metadata.MetadataReader reader, System.Reflection.Metadata.UserStringHandle handle) { throw null; }
        public static int GetTableMetadataOffset(this System.Reflection.Metadata.MetadataReader reader, System.Reflection.Metadata.Ecma335.TableIndex tableIndex) { throw null; }
        public static int GetTableRowCount(this System.Reflection.Metadata.MetadataReader reader, System.Reflection.Metadata.Ecma335.TableIndex tableIndex) { throw null; }
        public static int GetTableRowSize(this System.Reflection.Metadata.MetadataReader reader, System.Reflection.Metadata.Ecma335.TableIndex tableIndex) { throw null; }
        public static System.Collections.Generic.IEnumerable<System.Reflection.Metadata.TypeDefinitionHandle> GetTypesWithEvents(this System.Reflection.Metadata.MetadataReader reader) { throw null; }
        public static System.Collections.Generic.IEnumerable<System.Reflection.Metadata.TypeDefinitionHandle> GetTypesWithProperties(this System.Reflection.Metadata.MetadataReader reader) { throw null; }
        public static System.Reflection.Metadata.SignatureTypeKind ResolveSignatureTypeKind(this System.Reflection.Metadata.MetadataReader reader, System.Reflection.Metadata.EntityHandle typeHandle, byte rawTypeKind) { throw null; }
    }
    public sealed partial class MetadataRootBuilder
    {
        public MetadataRootBuilder(System.Reflection.Metadata.Ecma335.MetadataBuilder tablesAndHeaps, string? metadataVersion = null, bool suppressValidation = false) { }
        public string MetadataVersion { get { throw null; } }
        public System.Reflection.Metadata.Ecma335.MetadataSizes Sizes { get { throw null; } }
        public bool SuppressValidation { get { throw null; } }
        public void Serialize(System.Reflection.Metadata.BlobBuilder builder, int methodBodyStreamRva, int mappedFieldDataStreamRva) { }
    }
    public sealed partial class MetadataSizes
    {
        internal MetadataSizes() { }
        public System.Collections.Immutable.ImmutableArray<int> ExternalRowCounts { get { throw null; } }
        public System.Collections.Immutable.ImmutableArray<int> HeapSizes { get { throw null; } }
        public System.Collections.Immutable.ImmutableArray<int> RowCounts { get { throw null; } }
        public int GetAlignedHeapSize(System.Reflection.Metadata.Ecma335.HeapIndex index) { throw null; }
    }
    public static partial class MetadataTokens
    {
        public static readonly int HeapCount;
        public static readonly int TableCount;
        public static System.Reflection.Metadata.AssemblyFileHandle AssemblyFileHandle(int rowNumber) { throw null; }
        public static System.Reflection.Metadata.AssemblyReferenceHandle AssemblyReferenceHandle(int rowNumber) { throw null; }
        public static System.Reflection.Metadata.BlobHandle BlobHandle(int offset) { throw null; }
        public static System.Reflection.Metadata.ConstantHandle ConstantHandle(int rowNumber) { throw null; }
        public static System.Reflection.Metadata.CustomAttributeHandle CustomAttributeHandle(int rowNumber) { throw null; }
        public static System.Reflection.Metadata.CustomDebugInformationHandle CustomDebugInformationHandle(int rowNumber) { throw null; }
        public static System.Reflection.Metadata.DeclarativeSecurityAttributeHandle DeclarativeSecurityAttributeHandle(int rowNumber) { throw null; }
        public static System.Reflection.Metadata.DocumentHandle DocumentHandle(int rowNumber) { throw null; }
        public static System.Reflection.Metadata.DocumentNameBlobHandle DocumentNameBlobHandle(int offset) { throw null; }
        public static System.Reflection.Metadata.EntityHandle EntityHandle(int token) { throw null; }
        public static System.Reflection.Metadata.EntityHandle EntityHandle(System.Reflection.Metadata.Ecma335.TableIndex tableIndex, int rowNumber) { throw null; }
        public static System.Reflection.Metadata.EventDefinitionHandle EventDefinitionHandle(int rowNumber) { throw null; }
        public static System.Reflection.Metadata.ExportedTypeHandle ExportedTypeHandle(int rowNumber) { throw null; }
        public static System.Reflection.Metadata.FieldDefinitionHandle FieldDefinitionHandle(int rowNumber) { throw null; }
        public static System.Reflection.Metadata.GenericParameterConstraintHandle GenericParameterConstraintHandle(int rowNumber) { throw null; }
        public static System.Reflection.Metadata.GenericParameterHandle GenericParameterHandle(int rowNumber) { throw null; }
        public static int GetHeapOffset(System.Reflection.Metadata.BlobHandle handle) { throw null; }
        public static int GetHeapOffset(System.Reflection.Metadata.GuidHandle handle) { throw null; }
        public static int GetHeapOffset(System.Reflection.Metadata.Handle handle) { throw null; }
        public static int GetHeapOffset(this System.Reflection.Metadata.MetadataReader reader, System.Reflection.Metadata.Handle handle) { throw null; }
        public static int GetHeapOffset(System.Reflection.Metadata.StringHandle handle) { throw null; }
        public static int GetHeapOffset(System.Reflection.Metadata.UserStringHandle handle) { throw null; }
        public static int GetRowNumber(System.Reflection.Metadata.EntityHandle handle) { throw null; }
        public static int GetRowNumber(this System.Reflection.Metadata.MetadataReader reader, System.Reflection.Metadata.EntityHandle handle) { throw null; }
        public static int GetToken(System.Reflection.Metadata.EntityHandle handle) { throw null; }
        public static int GetToken(System.Reflection.Metadata.Handle handle) { throw null; }
        public static int GetToken(this System.Reflection.Metadata.MetadataReader reader, System.Reflection.Metadata.EntityHandle handle) { throw null; }
        public static int GetToken(this System.Reflection.Metadata.MetadataReader reader, System.Reflection.Metadata.Handle handle) { throw null; }
        public static System.Reflection.Metadata.GuidHandle GuidHandle(int offset) { throw null; }
        public static System.Reflection.Metadata.Handle Handle(int token) { throw null; }
        public static System.Reflection.Metadata.EntityHandle Handle(System.Reflection.Metadata.Ecma335.TableIndex tableIndex, int rowNumber) { throw null; }
        public static System.Reflection.Metadata.ImportScopeHandle ImportScopeHandle(int rowNumber) { throw null; }
        public static System.Reflection.Metadata.InterfaceImplementationHandle InterfaceImplementationHandle(int rowNumber) { throw null; }
        public static System.Reflection.Metadata.LocalConstantHandle LocalConstantHandle(int rowNumber) { throw null; }
        public static System.Reflection.Metadata.LocalScopeHandle LocalScopeHandle(int rowNumber) { throw null; }
        public static System.Reflection.Metadata.LocalVariableHandle LocalVariableHandle(int rowNumber) { throw null; }
        public static System.Reflection.Metadata.ManifestResourceHandle ManifestResourceHandle(int rowNumber) { throw null; }
        public static System.Reflection.Metadata.MemberReferenceHandle MemberReferenceHandle(int rowNumber) { throw null; }
        public static System.Reflection.Metadata.MethodDebugInformationHandle MethodDebugInformationHandle(int rowNumber) { throw null; }
        public static System.Reflection.Metadata.MethodDefinitionHandle MethodDefinitionHandle(int rowNumber) { throw null; }
        public static System.Reflection.Metadata.MethodImplementationHandle MethodImplementationHandle(int rowNumber) { throw null; }
        public static System.Reflection.Metadata.MethodSpecificationHandle MethodSpecificationHandle(int rowNumber) { throw null; }
        public static System.Reflection.Metadata.ModuleReferenceHandle ModuleReferenceHandle(int rowNumber) { throw null; }
        public static System.Reflection.Metadata.ParameterHandle ParameterHandle(int rowNumber) { throw null; }
        public static System.Reflection.Metadata.PropertyDefinitionHandle PropertyDefinitionHandle(int rowNumber) { throw null; }
        public static System.Reflection.Metadata.StandaloneSignatureHandle StandaloneSignatureHandle(int rowNumber) { throw null; }
        public static System.Reflection.Metadata.StringHandle StringHandle(int offset) { throw null; }
        public static bool TryGetHeapIndex(System.Reflection.Metadata.HandleKind type, out System.Reflection.Metadata.Ecma335.HeapIndex index) { throw null; }
        public static bool TryGetTableIndex(System.Reflection.Metadata.HandleKind type, out System.Reflection.Metadata.Ecma335.TableIndex index) { throw null; }
        public static System.Reflection.Metadata.TypeDefinitionHandle TypeDefinitionHandle(int rowNumber) { throw null; }
        public static System.Reflection.Metadata.TypeReferenceHandle TypeReferenceHandle(int rowNumber) { throw null; }
        public static System.Reflection.Metadata.TypeSpecificationHandle TypeSpecificationHandle(int rowNumber) { throw null; }
        public static System.Reflection.Metadata.UserStringHandle UserStringHandle(int offset) { throw null; }
    }
    [System.FlagsAttribute]
    public enum MethodBodyAttributes
    {
        None = 0,
        InitLocals = 1,
    }
    public readonly partial struct MethodBodyStreamEncoder
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public MethodBodyStreamEncoder(System.Reflection.Metadata.BlobBuilder builder) { throw null; }
        public System.Reflection.Metadata.BlobBuilder Builder { get { throw null; } }
        public System.Reflection.Metadata.Ecma335.MethodBodyStreamEncoder.MethodBody AddMethodBody(int codeSize, int maxStack, int exceptionRegionCount, bool hasSmallExceptionRegions, System.Reflection.Metadata.StandaloneSignatureHandle localVariablesSignature, System.Reflection.Metadata.Ecma335.MethodBodyAttributes attributes) { throw null; }
        public System.Reflection.Metadata.Ecma335.MethodBodyStreamEncoder.MethodBody AddMethodBody(int codeSize, int maxStack = 8, int exceptionRegionCount = 0, bool hasSmallExceptionRegions = true, System.Reflection.Metadata.StandaloneSignatureHandle localVariablesSignature = default(System.Reflection.Metadata.StandaloneSignatureHandle), System.Reflection.Metadata.Ecma335.MethodBodyAttributes attributes = System.Reflection.Metadata.Ecma335.MethodBodyAttributes.InitLocals, bool hasDynamicStackAllocation = false) { throw null; }
        public int AddMethodBody(System.Reflection.Metadata.Ecma335.InstructionEncoder instructionEncoder, int maxStack, System.Reflection.Metadata.StandaloneSignatureHandle localVariablesSignature, System.Reflection.Metadata.Ecma335.MethodBodyAttributes attributes) { throw null; }
        public int AddMethodBody(System.Reflection.Metadata.Ecma335.InstructionEncoder instructionEncoder, int maxStack = 8, System.Reflection.Metadata.StandaloneSignatureHandle localVariablesSignature = default(System.Reflection.Metadata.StandaloneSignatureHandle), System.Reflection.Metadata.Ecma335.MethodBodyAttributes attributes = System.Reflection.Metadata.Ecma335.MethodBodyAttributes.InitLocals, bool hasDynamicStackAllocation = false) { throw null; }
        public readonly partial struct MethodBody
        {
            private readonly object _dummy;
            private readonly int _dummyPrimitive;
            public System.Reflection.Metadata.Ecma335.ExceptionRegionEncoder ExceptionRegions { get { throw null; } }
            public System.Reflection.Metadata.Blob Instructions { get { throw null; } }
            public int Offset { get { throw null; } }
        }
    }
    public readonly partial struct MethodSignatureEncoder
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public MethodSignatureEncoder(System.Reflection.Metadata.BlobBuilder builder, bool hasVarArgs) { throw null; }
        public System.Reflection.Metadata.BlobBuilder Builder { get { throw null; } }
        public bool HasVarArgs { get { throw null; } }
        public void Parameters(int parameterCount, System.Action<System.Reflection.Metadata.Ecma335.ReturnTypeEncoder> returnType, System.Action<System.Reflection.Metadata.Ecma335.ParametersEncoder> parameters) { }
        public void Parameters(int parameterCount, out System.Reflection.Metadata.Ecma335.ReturnTypeEncoder returnType, out System.Reflection.Metadata.Ecma335.ParametersEncoder parameters) { throw null; }
    }
    public readonly partial struct NamedArgumentsEncoder
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public NamedArgumentsEncoder(System.Reflection.Metadata.BlobBuilder builder) { throw null; }
        public System.Reflection.Metadata.BlobBuilder Builder { get { throw null; } }
        public void AddArgument(bool isField, System.Action<System.Reflection.Metadata.Ecma335.NamedArgumentTypeEncoder> type, System.Action<System.Reflection.Metadata.Ecma335.NameEncoder> name, System.Action<System.Reflection.Metadata.Ecma335.LiteralEncoder> literal) { }
        public void AddArgument(bool isField, out System.Reflection.Metadata.Ecma335.NamedArgumentTypeEncoder type, out System.Reflection.Metadata.Ecma335.NameEncoder name, out System.Reflection.Metadata.Ecma335.LiteralEncoder literal) { throw null; }
    }
    public readonly partial struct NamedArgumentTypeEncoder
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public NamedArgumentTypeEncoder(System.Reflection.Metadata.BlobBuilder builder) { throw null; }
        public System.Reflection.Metadata.BlobBuilder Builder { get { throw null; } }
        public void Object() { }
        public System.Reflection.Metadata.Ecma335.CustomAttributeElementTypeEncoder ScalarType() { throw null; }
        public System.Reflection.Metadata.Ecma335.CustomAttributeArrayTypeEncoder SZArray() { throw null; }
    }
    public readonly partial struct NameEncoder
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public NameEncoder(System.Reflection.Metadata.BlobBuilder builder) { throw null; }
        public System.Reflection.Metadata.BlobBuilder Builder { get { throw null; } }
        public void Name(string name) { }
    }
    public readonly partial struct ParametersEncoder
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public ParametersEncoder(System.Reflection.Metadata.BlobBuilder builder, bool hasVarArgs = false) { throw null; }
        public System.Reflection.Metadata.BlobBuilder Builder { get { throw null; } }
        public bool HasVarArgs { get { throw null; } }
        public System.Reflection.Metadata.Ecma335.ParameterTypeEncoder AddParameter() { throw null; }
        public System.Reflection.Metadata.Ecma335.ParametersEncoder StartVarArgs() { throw null; }
    }
    public readonly partial struct ParameterTypeEncoder
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public ParameterTypeEncoder(System.Reflection.Metadata.BlobBuilder builder) { throw null; }
        public System.Reflection.Metadata.BlobBuilder Builder { get { throw null; } }
        public System.Reflection.Metadata.Ecma335.CustomModifiersEncoder CustomModifiers() { throw null; }
        public System.Reflection.Metadata.Ecma335.SignatureTypeEncoder Type(bool isByRef = false) { throw null; }
        public void TypedReference() { }
    }
    public readonly partial struct PermissionSetEncoder
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public PermissionSetEncoder(System.Reflection.Metadata.BlobBuilder builder) { throw null; }
        public System.Reflection.Metadata.BlobBuilder Builder { get { throw null; } }
        public System.Reflection.Metadata.Ecma335.PermissionSetEncoder AddPermission(string typeName, System.Collections.Immutable.ImmutableArray<byte> encodedArguments) { throw null; }
        public System.Reflection.Metadata.Ecma335.PermissionSetEncoder AddPermission(string typeName, System.Reflection.Metadata.BlobBuilder encodedArguments) { throw null; }
    }
    public sealed partial class PortablePdbBuilder
    {
        public PortablePdbBuilder(System.Reflection.Metadata.Ecma335.MetadataBuilder tablesAndHeaps, System.Collections.Immutable.ImmutableArray<int> typeSystemRowCounts, System.Reflection.Metadata.MethodDefinitionHandle entryPoint, System.Func<System.Collections.Generic.IEnumerable<System.Reflection.Metadata.Blob>, System.Reflection.Metadata.BlobContentId>? idProvider = null) { }
        public ushort FormatVersion { get { throw null; } }
        public System.Func<System.Collections.Generic.IEnumerable<System.Reflection.Metadata.Blob>, System.Reflection.Metadata.BlobContentId> IdProvider { get { throw null; } }
        public string MetadataVersion { get { throw null; } }
        public System.Reflection.Metadata.BlobContentId Serialize(System.Reflection.Metadata.BlobBuilder builder) { throw null; }
    }
    public readonly partial struct ReturnTypeEncoder
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public ReturnTypeEncoder(System.Reflection.Metadata.BlobBuilder builder) { throw null; }
        public System.Reflection.Metadata.BlobBuilder Builder { get { throw null; } }
        public System.Reflection.Metadata.Ecma335.CustomModifiersEncoder CustomModifiers() { throw null; }
        public System.Reflection.Metadata.Ecma335.SignatureTypeEncoder Type(bool isByRef = false) { throw null; }
        public void TypedReference() { }
        public void Void() { }
    }
    public readonly partial struct ScalarEncoder
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public ScalarEncoder(System.Reflection.Metadata.BlobBuilder builder) { throw null; }
        public System.Reflection.Metadata.BlobBuilder Builder { get { throw null; } }
        public void Constant(object? value) { }
        public void NullArray() { }
        public void SystemType(string? serializedTypeName) { }
    }
    public readonly partial struct SignatureDecoder<TType, TGenericContext>
    {
        private readonly TGenericContext _genericContext;
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public SignatureDecoder(System.Reflection.Metadata.ISignatureTypeProvider<TType, TGenericContext> provider, System.Reflection.Metadata.MetadataReader metadataReader, TGenericContext genericContext) { throw null; }
        public TType DecodeFieldSignature(ref System.Reflection.Metadata.BlobReader blobReader) { throw null; }
        public System.Collections.Immutable.ImmutableArray<TType> DecodeLocalSignature(ref System.Reflection.Metadata.BlobReader blobReader) { throw null; }
        public System.Reflection.Metadata.MethodSignature<TType> DecodeMethodSignature(ref System.Reflection.Metadata.BlobReader blobReader) { throw null; }
        public System.Collections.Immutable.ImmutableArray<TType> DecodeMethodSpecificationSignature(ref System.Reflection.Metadata.BlobReader blobReader) { throw null; }
        public TType DecodeType(ref System.Reflection.Metadata.BlobReader blobReader, bool allowTypeSpecifications = false) { throw null; }
    }
    public readonly partial struct SignatureTypeEncoder
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public SignatureTypeEncoder(System.Reflection.Metadata.BlobBuilder builder) { throw null; }
        public System.Reflection.Metadata.BlobBuilder Builder { get { throw null; } }
        public void Array(System.Action<System.Reflection.Metadata.Ecma335.SignatureTypeEncoder> elementType, System.Action<System.Reflection.Metadata.Ecma335.ArrayShapeEncoder> arrayShape) { }
        public void Array(out System.Reflection.Metadata.Ecma335.SignatureTypeEncoder elementType, out System.Reflection.Metadata.Ecma335.ArrayShapeEncoder arrayShape) { throw null; }
        public void Boolean() { }
        public void Byte() { }
        public void Char() { }
        public System.Reflection.Metadata.Ecma335.CustomModifiersEncoder CustomModifiers() { throw null; }
        public void Double() { }
        public System.Reflection.Metadata.Ecma335.MethodSignatureEncoder FunctionPointer(System.Reflection.Metadata.SignatureCallingConvention convention = System.Reflection.Metadata.SignatureCallingConvention.Default, System.Reflection.Metadata.Ecma335.FunctionPointerAttributes attributes = System.Reflection.Metadata.Ecma335.FunctionPointerAttributes.None, int genericParameterCount = 0) { throw null; }
        public System.Reflection.Metadata.Ecma335.GenericTypeArgumentsEncoder GenericInstantiation(System.Reflection.Metadata.EntityHandle genericType, int genericArgumentCount, bool isValueType) { throw null; }
        public void GenericMethodTypeParameter(int parameterIndex) { }
        public void GenericTypeParameter(int parameterIndex) { }
        public void Int16() { }
        public void Int32() { }
        public void Int64() { }
        public void IntPtr() { }
        public void Object() { }
        public System.Reflection.Metadata.Ecma335.SignatureTypeEncoder Pointer() { throw null; }
        public void PrimitiveType(System.Reflection.Metadata.PrimitiveTypeCode type) { }
        public void SByte() { }
        public void Single() { }
        public void String() { }
        public System.Reflection.Metadata.Ecma335.SignatureTypeEncoder SZArray() { throw null; }
        public void Type(System.Reflection.Metadata.EntityHandle type, bool isValueType) { }
        public void TypedReference() { }
        public void UInt16() { }
        public void UInt32() { }
        public void UInt64() { }
        public void UIntPtr() { }
        public void VoidPointer() { }
    }
    public readonly struct SwitchInstructionEncoder
    {
        public void Branch(System.Reflection.Metadata.Ecma335.LabelHandle label) { }
    }
    public enum TableIndex : byte
    {
        Module = (byte)0,
        TypeRef = (byte)1,
        TypeDef = (byte)2,
        FieldPtr = (byte)3,
        Field = (byte)4,
        MethodPtr = (byte)5,
        MethodDef = (byte)6,
        ParamPtr = (byte)7,
        Param = (byte)8,
        InterfaceImpl = (byte)9,
        MemberRef = (byte)10,
        Constant = (byte)11,
        CustomAttribute = (byte)12,
        FieldMarshal = (byte)13,
        DeclSecurity = (byte)14,
        ClassLayout = (byte)15,
        FieldLayout = (byte)16,
        StandAloneSig = (byte)17,
        EventMap = (byte)18,
        EventPtr = (byte)19,
        Event = (byte)20,
        PropertyMap = (byte)21,
        PropertyPtr = (byte)22,
        Property = (byte)23,
        MethodSemantics = (byte)24,
        MethodImpl = (byte)25,
        ModuleRef = (byte)26,
        TypeSpec = (byte)27,
        ImplMap = (byte)28,
        FieldRva = (byte)29,
        EncLog = (byte)30,
        EncMap = (byte)31,
        Assembly = (byte)32,
        AssemblyProcessor = (byte)33,
        AssemblyOS = (byte)34,
        AssemblyRef = (byte)35,
        AssemblyRefProcessor = (byte)36,
        AssemblyRefOS = (byte)37,
        File = (byte)38,
        ExportedType = (byte)39,
        ManifestResource = (byte)40,
        NestedClass = (byte)41,
        GenericParam = (byte)42,
        MethodSpec = (byte)43,
        GenericParamConstraint = (byte)44,
        Document = (byte)48,
        MethodDebugInformation = (byte)49,
        LocalScope = (byte)50,
        LocalVariable = (byte)51,
        LocalConstant = (byte)52,
        ImportScope = (byte)53,
        StateMachineMethod = (byte)54,
        CustomDebugInformation = (byte)55,
    }
    public readonly partial struct VectorEncoder
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public VectorEncoder(System.Reflection.Metadata.BlobBuilder builder) { throw null; }
        public System.Reflection.Metadata.BlobBuilder Builder { get { throw null; } }
        public System.Reflection.Metadata.Ecma335.LiteralsEncoder Count(int count) { throw null; }
    }
}
namespace System.Reflection.PortableExecutable
{
    [System.FlagsAttribute]
    public enum Characteristics : ushort
    {
        RelocsStripped = (ushort)1,
        ExecutableImage = (ushort)2,
        LineNumsStripped = (ushort)4,
        LocalSymsStripped = (ushort)8,
        AggressiveWSTrim = (ushort)16,
        LargeAddressAware = (ushort)32,
        BytesReversedLo = (ushort)128,
        Bit32Machine = (ushort)256,
        DebugStripped = (ushort)512,
        RemovableRunFromSwap = (ushort)1024,
        NetRunFromSwap = (ushort)2048,
        System = (ushort)4096,
        Dll = (ushort)8192,
        UpSystemOnly = (ushort)16384,
        BytesReversedHi = (ushort)32768,
    }
    public readonly partial struct CodeViewDebugDirectoryData
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public int Age { get { throw null; } }
        public System.Guid Guid { get { throw null; } }
        public string Path { get { throw null; } }
    }
    public sealed partial class CoffHeader
    {
        internal CoffHeader() { }
        public System.Reflection.PortableExecutable.Characteristics Characteristics { get { throw null; } }
        public System.Reflection.PortableExecutable.Machine Machine { get { throw null; } }
        public short NumberOfSections { get { throw null; } }
        public int NumberOfSymbols { get { throw null; } }
        public int PointerToSymbolTable { get { throw null; } }
        public short SizeOfOptionalHeader { get { throw null; } }
        public int TimeDateStamp { get { throw null; } }
    }
    [System.FlagsAttribute]
    public enum CorFlags
    {
        ILOnly = 1,
        Requires32Bit = 2,
        ILLibrary = 4,
        StrongNameSigned = 8,
        NativeEntryPoint = 16,
        TrackDebugData = 65536,
        Prefers32Bit = 131072,
    }
    public sealed partial class CorHeader
    {
        internal CorHeader() { }
        public System.Reflection.PortableExecutable.DirectoryEntry CodeManagerTableDirectory { get { throw null; } }
        public int EntryPointTokenOrRelativeVirtualAddress { get { throw null; } }
        public System.Reflection.PortableExecutable.DirectoryEntry ExportAddressTableJumpsDirectory { get { throw null; } }
        public System.Reflection.PortableExecutable.CorFlags Flags { get { throw null; } }
        public ushort MajorRuntimeVersion { get { throw null; } }
        public System.Reflection.PortableExecutable.DirectoryEntry ManagedNativeHeaderDirectory { get { throw null; } }
        public System.Reflection.PortableExecutable.DirectoryEntry MetadataDirectory { get { throw null; } }
        public ushort MinorRuntimeVersion { get { throw null; } }
        public System.Reflection.PortableExecutable.DirectoryEntry ResourcesDirectory { get { throw null; } }
        public System.Reflection.PortableExecutable.DirectoryEntry StrongNameSignatureDirectory { get { throw null; } }
        public System.Reflection.PortableExecutable.DirectoryEntry VtableFixupsDirectory { get { throw null; } }
    }
    public sealed partial class DebugDirectoryBuilder
    {
        public DebugDirectoryBuilder() { }
        public void AddCodeViewEntry(string pdbPath, System.Reflection.Metadata.BlobContentId pdbContentId, ushort portablePdbVersion) { }
        public void AddCodeViewEntry(string pdbPath, System.Reflection.Metadata.BlobContentId pdbContentId, ushort portablePdbVersion, int age) { }
        public void AddEmbeddedPortablePdbEntry(System.Reflection.Metadata.BlobBuilder debugMetadata, ushort portablePdbVersion) { }
        public void AddEntry(System.Reflection.PortableExecutable.DebugDirectoryEntryType type, uint version, uint stamp) { }
        public void AddEntry<TData>(System.Reflection.PortableExecutable.DebugDirectoryEntryType type, uint version, uint stamp, TData data, System.Action<System.Reflection.Metadata.BlobBuilder, TData> dataSerializer) { }
        public void AddPdbChecksumEntry(string algorithmName, System.Collections.Immutable.ImmutableArray<byte> checksum) { }
        public void AddReproducibleEntry() { }
    }
    public readonly partial struct DebugDirectoryEntry
    {
        private readonly int _dummyPrimitive;
        public DebugDirectoryEntry(uint stamp, ushort majorVersion, ushort minorVersion, System.Reflection.PortableExecutable.DebugDirectoryEntryType type, int dataSize, int dataRelativeVirtualAddress, int dataPointer) { throw null; }
        public int DataPointer { get { throw null; } }
        public int DataRelativeVirtualAddress { get { throw null; } }
        public int DataSize { get { throw null; } }
        public bool IsPortableCodeView { get { throw null; } }
        public ushort MajorVersion { get { throw null; } }
        public ushort MinorVersion { get { throw null; } }
        public uint Stamp { get { throw null; } }
        public System.Reflection.PortableExecutable.DebugDirectoryEntryType Type { get { throw null; } }
    }
    public enum DebugDirectoryEntryType
    {
        Unknown = 0,
        Coff = 1,
        CodeView = 2,
        Reproducible = 16,
        EmbeddedPortablePdb = 17,
        PdbChecksum = 19,
    }
    public readonly partial struct DirectoryEntry
    {
        public readonly int RelativeVirtualAddress;
        public readonly int Size;
        public DirectoryEntry(int relativeVirtualAddress, int size) { throw null; }
    }
    [System.FlagsAttribute]
    public enum DllCharacteristics : ushort
    {
        ProcessInit = (ushort)1,
        ProcessTerm = (ushort)2,
        ThreadInit = (ushort)4,
        ThreadTerm = (ushort)8,
        HighEntropyVirtualAddressSpace = (ushort)32,
        DynamicBase = (ushort)64,
        ForceIntegrity = (ushort)128,
        NxCompatible = (ushort)256,
        NoIsolation = (ushort)512,
        NoSeh = (ushort)1024,
        NoBind = (ushort)2048,
        AppContainer = (ushort)4096,
        WdmDriver = (ushort)8192,
        ControlFlowGuard = (ushort)16384,
        TerminalServerAware = (ushort)32768,
    }
    public enum Machine : ushort
    {
        Unknown = (ushort)0,
        I386 = (ushort)332,
        WceMipsV2 = (ushort)361,
        Alpha = (ushort)388,
        SH3 = (ushort)418,
        SH3Dsp = (ushort)419,
        SH3E = (ushort)420,
        SH4 = (ushort)422,
        SH5 = (ushort)424,
        Arm = (ushort)448,
        Thumb = (ushort)450,
        ArmThumb2 = (ushort)452,
        AM33 = (ushort)467,
        PowerPC = (ushort)496,
        PowerPCFP = (ushort)497,
        IA64 = (ushort)512,
        MIPS16 = (ushort)614,
        Alpha64 = (ushort)644,
        MipsFpu = (ushort)870,
        MipsFpu16 = (ushort)1126,
        Tricore = (ushort)1312,
        Ebc = (ushort)3772,
        Amd64 = (ushort)34404,
        M32R = (ushort)36929,
        Arm64 = (ushort)43620,
        LoongArch32 = (ushort)25138,
        LoongArch64 = (ushort)25188,
        RiscV32 = (ushort)20530,
        RiscV64 = (ushort)20580,
        RiscV128 = (ushort)20776,
    }
    public partial class ManagedPEBuilder : System.Reflection.PortableExecutable.PEBuilder
    {
        public const int ManagedResourcesDataAlignment = 8;
        public const int MappedFieldDataAlignment = 8;
        public ManagedPEBuilder(System.Reflection.PortableExecutable.PEHeaderBuilder header, System.Reflection.Metadata.Ecma335.MetadataRootBuilder metadataRootBuilder, System.Reflection.Metadata.BlobBuilder ilStream, System.Reflection.Metadata.BlobBuilder? mappedFieldData = null, System.Reflection.Metadata.BlobBuilder? managedResources = null, System.Reflection.PortableExecutable.ResourceSectionBuilder? nativeResources = null, System.Reflection.PortableExecutable.DebugDirectoryBuilder? debugDirectoryBuilder = null, int strongNameSignatureSize = 128, System.Reflection.Metadata.MethodDefinitionHandle entryPoint = default(System.Reflection.Metadata.MethodDefinitionHandle), System.Reflection.PortableExecutable.CorFlags flags = System.Reflection.PortableExecutable.CorFlags.ILOnly, System.Func<System.Collections.Generic.IEnumerable<System.Reflection.Metadata.Blob>, System.Reflection.Metadata.BlobContentId>? deterministicIdProvider = null) : base (default(System.Reflection.PortableExecutable.PEHeaderBuilder), default(System.Func<System.Collections.Generic.IEnumerable<System.Reflection.Metadata.Blob>, System.Reflection.Metadata.BlobContentId>)) { }
        protected override System.Collections.Immutable.ImmutableArray<System.Reflection.PortableExecutable.PEBuilder.Section> CreateSections() { throw null; }
        protected internal override System.Reflection.PortableExecutable.PEDirectoriesBuilder GetDirectories() { throw null; }
        protected override System.Reflection.Metadata.BlobBuilder SerializeSection(string name, System.Reflection.PortableExecutable.SectionLocation location) { throw null; }
        public void Sign(System.Reflection.Metadata.BlobBuilder peImage, System.Func<System.Collections.Generic.IEnumerable<System.Reflection.Metadata.Blob>, byte[]> signatureProvider) { }
    }
    public readonly partial struct PdbChecksumDebugDirectoryData
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public string AlgorithmName { get { throw null; } }
        public System.Collections.Immutable.ImmutableArray<byte> Checksum { get { throw null; } }
    }
    public abstract partial class PEBuilder
    {
        protected PEBuilder(System.Reflection.PortableExecutable.PEHeaderBuilder header, System.Func<System.Collections.Generic.IEnumerable<System.Reflection.Metadata.Blob>, System.Reflection.Metadata.BlobContentId>? deterministicIdProvider) { }
        public System.Reflection.PortableExecutable.PEHeaderBuilder Header { get { throw null; } }
        public System.Func<System.Collections.Generic.IEnumerable<System.Reflection.Metadata.Blob>, System.Reflection.Metadata.BlobContentId> IdProvider { get { throw null; } }
        public bool IsDeterministic { get { throw null; } }
        protected abstract System.Collections.Immutable.ImmutableArray<System.Reflection.PortableExecutable.PEBuilder.Section> CreateSections();
        protected internal abstract System.Reflection.PortableExecutable.PEDirectoriesBuilder GetDirectories();
        protected System.Collections.Immutable.ImmutableArray<System.Reflection.PortableExecutable.PEBuilder.Section> GetSections() { throw null; }
        public System.Reflection.Metadata.BlobContentId Serialize(System.Reflection.Metadata.BlobBuilder builder) { throw null; }
        protected abstract System.Reflection.Metadata.BlobBuilder SerializeSection(string name, System.Reflection.PortableExecutable.SectionLocation location);
        protected readonly partial struct Section
        {
            public readonly System.Reflection.PortableExecutable.SectionCharacteristics Characteristics;
            public readonly string Name;
            public Section(string name, System.Reflection.PortableExecutable.SectionCharacteristics characteristics) { throw null; }
        }
    }
    public sealed partial class PEDirectoriesBuilder
    {
        public PEDirectoriesBuilder() { }
        public int AddressOfEntryPoint { get { throw null; } set { } }
        public System.Reflection.PortableExecutable.DirectoryEntry BaseRelocationTable { get { throw null; } set { } }
        public System.Reflection.PortableExecutable.DirectoryEntry BoundImportTable { get { throw null; } set { } }
        public System.Reflection.PortableExecutable.DirectoryEntry CopyrightTable { get { throw null; } set { } }
        public System.Reflection.PortableExecutable.DirectoryEntry CorHeaderTable { get { throw null; } set { } }
        public System.Reflection.PortableExecutable.DirectoryEntry DebugTable { get { throw null; } set { } }
        public System.Reflection.PortableExecutable.DirectoryEntry DelayImportTable { get { throw null; } set { } }
        public System.Reflection.PortableExecutable.DirectoryEntry ExceptionTable { get { throw null; } set { } }
        public System.Reflection.PortableExecutable.DirectoryEntry ExportTable { get { throw null; } set { } }
        public System.Reflection.PortableExecutable.DirectoryEntry GlobalPointerTable { get { throw null; } set { } }
        public System.Reflection.PortableExecutable.DirectoryEntry ImportAddressTable { get { throw null; } set { } }
        public System.Reflection.PortableExecutable.DirectoryEntry ImportTable { get { throw null; } set { } }
        public System.Reflection.PortableExecutable.DirectoryEntry LoadConfigTable { get { throw null; } set { } }
        public System.Reflection.PortableExecutable.DirectoryEntry ResourceTable { get { throw null; } set { } }
        public System.Reflection.PortableExecutable.DirectoryEntry ThreadLocalStorageTable { get { throw null; } set { } }
    }
    public sealed partial class PEHeader
    {
        internal PEHeader() { }
        public int AddressOfEntryPoint { get { throw null; } }
        public int BaseOfCode { get { throw null; } }
        public int BaseOfData { get { throw null; } }
        public System.Reflection.PortableExecutable.DirectoryEntry BaseRelocationTableDirectory { get { throw null; } }
        public System.Reflection.PortableExecutable.DirectoryEntry BoundImportTableDirectory { get { throw null; } }
        public System.Reflection.PortableExecutable.DirectoryEntry CertificateTableDirectory { get { throw null; } }
        public uint CheckSum { get { throw null; } }
        public System.Reflection.PortableExecutable.DirectoryEntry CopyrightTableDirectory { get { throw null; } }
        public System.Reflection.PortableExecutable.DirectoryEntry CorHeaderTableDirectory { get { throw null; } }
        public System.Reflection.PortableExecutable.DirectoryEntry DebugTableDirectory { get { throw null; } }
        public System.Reflection.PortableExecutable.DirectoryEntry DelayImportTableDirectory { get { throw null; } }
        public System.Reflection.PortableExecutable.DllCharacteristics DllCharacteristics { get { throw null; } }
        public System.Reflection.PortableExecutable.DirectoryEntry ExceptionTableDirectory { get { throw null; } }
        public System.Reflection.PortableExecutable.DirectoryEntry ExportTableDirectory { get { throw null; } }
        public int FileAlignment { get { throw null; } }
        public System.Reflection.PortableExecutable.DirectoryEntry GlobalPointerTableDirectory { get { throw null; } }
        public ulong ImageBase { get { throw null; } }
        public System.Reflection.PortableExecutable.DirectoryEntry ImportAddressTableDirectory { get { throw null; } }
        public System.Reflection.PortableExecutable.DirectoryEntry ImportTableDirectory { get { throw null; } }
        public System.Reflection.PortableExecutable.DirectoryEntry LoadConfigTableDirectory { get { throw null; } }
        public System.Reflection.PortableExecutable.PEMagic Magic { get { throw null; } }
        public ushort MajorImageVersion { get { throw null; } }
        public byte MajorLinkerVersion { get { throw null; } }
        public ushort MajorOperatingSystemVersion { get { throw null; } }
        public ushort MajorSubsystemVersion { get { throw null; } }
        public ushort MinorImageVersion { get { throw null; } }
        public byte MinorLinkerVersion { get { throw null; } }
        public ushort MinorOperatingSystemVersion { get { throw null; } }
        public ushort MinorSubsystemVersion { get { throw null; } }
        public int NumberOfRvaAndSizes { get { throw null; } }
        public System.Reflection.PortableExecutable.DirectoryEntry ResourceTableDirectory { get { throw null; } }
        public int SectionAlignment { get { throw null; } }
        public int SizeOfCode { get { throw null; } }
        public int SizeOfHeaders { get { throw null; } }
        public ulong SizeOfHeapCommit { get { throw null; } }
        public ulong SizeOfHeapReserve { get { throw null; } }
        public int SizeOfImage { get { throw null; } }
        public int SizeOfInitializedData { get { throw null; } }
        public ulong SizeOfStackCommit { get { throw null; } }
        public ulong SizeOfStackReserve { get { throw null; } }
        public int SizeOfUninitializedData { get { throw null; } }
        public System.Reflection.PortableExecutable.Subsystem Subsystem { get { throw null; } }
        public System.Reflection.PortableExecutable.DirectoryEntry ThreadLocalStorageTableDirectory { get { throw null; } }
    }
    public sealed partial class PEHeaderBuilder
    {
        public PEHeaderBuilder(System.Reflection.PortableExecutable.Machine machine = System.Reflection.PortableExecutable.Machine.Unknown, int sectionAlignment = 8192, int fileAlignment = 512, ulong imageBase = (ulong)4194304, byte majorLinkerVersion = (byte)48, byte minorLinkerVersion = (byte)0, ushort majorOperatingSystemVersion = (ushort)4, ushort minorOperatingSystemVersion = (ushort)0, ushort majorImageVersion = (ushort)0, ushort minorImageVersion = (ushort)0, ushort majorSubsystemVersion = (ushort)4, ushort minorSubsystemVersion = (ushort)0, System.Reflection.PortableExecutable.Subsystem subsystem = System.Reflection.PortableExecutable.Subsystem.WindowsCui, System.Reflection.PortableExecutable.DllCharacteristics dllCharacteristics = System.Reflection.PortableExecutable.DllCharacteristics.DynamicBase | System.Reflection.PortableExecutable.DllCharacteristics.NoSeh | System.Reflection.PortableExecutable.DllCharacteristics.NxCompatible | System.Reflection.PortableExecutable.DllCharacteristics.TerminalServerAware, System.Reflection.PortableExecutable.Characteristics imageCharacteristics = System.Reflection.PortableExecutable.Characteristics.Dll, ulong sizeOfStackReserve = (ulong)1048576, ulong sizeOfStackCommit = (ulong)4096, ulong sizeOfHeapReserve = (ulong)1048576, ulong sizeOfHeapCommit = (ulong)4096) { }
        public System.Reflection.PortableExecutable.DllCharacteristics DllCharacteristics { get { throw null; } }
        public int FileAlignment { get { throw null; } }
        public ulong ImageBase { get { throw null; } }
        public System.Reflection.PortableExecutable.Characteristics ImageCharacteristics { get { throw null; } }
        public System.Reflection.PortableExecutable.Machine Machine { get { throw null; } }
        public ushort MajorImageVersion { get { throw null; } }
        public byte MajorLinkerVersion { get { throw null; } }
        public ushort MajorOperatingSystemVersion { get { throw null; } }
        public ushort MajorSubsystemVersion { get { throw null; } }
        public ushort MinorImageVersion { get { throw null; } }
        public byte MinorLinkerVersion { get { throw null; } }
        public ushort MinorOperatingSystemVersion { get { throw null; } }
        public ushort MinorSubsystemVersion { get { throw null; } }
        public int SectionAlignment { get { throw null; } }
        public ulong SizeOfHeapCommit { get { throw null; } }
        public ulong SizeOfHeapReserve { get { throw null; } }
        public ulong SizeOfStackCommit { get { throw null; } }
        public ulong SizeOfStackReserve { get { throw null; } }
        public System.Reflection.PortableExecutable.Subsystem Subsystem { get { throw null; } }
        public static System.Reflection.PortableExecutable.PEHeaderBuilder CreateExecutableHeader() { throw null; }
        public static System.Reflection.PortableExecutable.PEHeaderBuilder CreateLibraryHeader() { throw null; }
    }
    public sealed partial class PEHeaders
    {
        public PEHeaders(System.IO.Stream peStream) { }
        public PEHeaders(System.IO.Stream peStream, int size) { }
        public PEHeaders(System.IO.Stream peStream, int size, bool isLoadedImage) { }
        public System.Reflection.PortableExecutable.CoffHeader CoffHeader { get { throw null; } }
        public int CoffHeaderStartOffset { get { throw null; } }
        public System.Reflection.PortableExecutable.CorHeader? CorHeader { get { throw null; } }
        public int CorHeaderStartOffset { get { throw null; } }
        public bool IsCoffOnly { get { throw null; } }
        public bool IsConsoleApplication { get { throw null; } }
        public bool IsDll { get { throw null; } }
        public bool IsExe { get { throw null; } }
        public int MetadataSize { get { throw null; } }
        public int MetadataStartOffset { get { throw null; } }
        public System.Reflection.PortableExecutable.PEHeader? PEHeader { get { throw null; } }
        public int PEHeaderStartOffset { get { throw null; } }
        public System.Collections.Immutable.ImmutableArray<System.Reflection.PortableExecutable.SectionHeader> SectionHeaders { get { throw null; } }
        public int GetContainingSectionIndex(int relativeVirtualAddress) { throw null; }
        public bool TryGetDirectoryOffset(System.Reflection.PortableExecutable.DirectoryEntry directory, out int offset) { throw null; }
    }
    public enum PEMagic : ushort
    {
        PE32 = (ushort)267,
        PE32Plus = (ushort)523,
    }
    public readonly partial struct PEMemoryBlock
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public int Length { get { throw null; } }
        public unsafe byte* Pointer { get { throw null; } }
        public System.Collections.Immutable.ImmutableArray<byte> GetContent() { throw null; }
        public System.Collections.Immutable.ImmutableArray<byte> GetContent(int start, int length) { throw null; }
        public System.Reflection.Metadata.BlobReader GetReader() { throw null; }
        public System.Reflection.Metadata.BlobReader GetReader(int start, int length) { throw null; }
    }
    public sealed partial class PEReader : System.IDisposable
    {
        public unsafe PEReader(byte* peImage, int size) { }
        public unsafe PEReader(byte* peImage, int size, bool isLoadedImage) { }
        public PEReader(System.Collections.Immutable.ImmutableArray<byte> peImage) { }
        public PEReader(System.IO.Stream peStream) { }
        public PEReader(System.IO.Stream peStream, System.Reflection.PortableExecutable.PEStreamOptions options) { }
        public PEReader(System.IO.Stream peStream, System.Reflection.PortableExecutable.PEStreamOptions options, int size) { }
        public bool HasMetadata { get { throw null; } }
        public bool IsEntireImageAvailable { get { throw null; } }
        public bool IsLoadedImage { get { throw null; } }
        public System.Reflection.PortableExecutable.PEHeaders PEHeaders { get { throw null; } }
        public void Dispose() { }
        public System.Reflection.PortableExecutable.PEMemoryBlock GetEntireImage() { throw null; }
        public System.Reflection.PortableExecutable.PEMemoryBlock GetMetadata() { throw null; }
        public System.Reflection.PortableExecutable.PEMemoryBlock GetSectionData(int relativeVirtualAddress) { throw null; }
        public System.Reflection.PortableExecutable.PEMemoryBlock GetSectionData(string sectionName) { throw null; }
        public System.Reflection.PortableExecutable.CodeViewDebugDirectoryData ReadCodeViewDebugDirectoryData(System.Reflection.PortableExecutable.DebugDirectoryEntry entry) { throw null; }
        public System.Collections.Immutable.ImmutableArray<System.Reflection.PortableExecutable.DebugDirectoryEntry> ReadDebugDirectory() { throw null; }
        public System.Reflection.Metadata.MetadataReaderProvider ReadEmbeddedPortablePdbDebugDirectoryData(System.Reflection.PortableExecutable.DebugDirectoryEntry entry) { throw null; }
        public System.Reflection.PortableExecutable.PdbChecksumDebugDirectoryData ReadPdbChecksumDebugDirectoryData(System.Reflection.PortableExecutable.DebugDirectoryEntry entry) { throw null; }
        public bool TryOpenAssociatedPortablePdb(string peImagePath, System.Func<string, System.IO.Stream?> pdbFileStreamProvider, out System.Reflection.Metadata.MetadataReaderProvider? pdbReaderProvider, out string? pdbPath) { throw null; }
    }
    [System.FlagsAttribute]
    public enum PEStreamOptions
    {
        Default = 0,
        LeaveOpen = 1,
        PrefetchMetadata = 2,
        PrefetchEntireImage = 4,
        IsLoadedImage = 8,
    }
    public abstract partial class ResourceSectionBuilder
    {
        protected ResourceSectionBuilder() { }
        protected internal abstract void Serialize(System.Reflection.Metadata.BlobBuilder builder, System.Reflection.PortableExecutable.SectionLocation location);
    }
    [System.FlagsAttribute]
    public enum SectionCharacteristics : uint
    {
        TypeReg = (uint)0,
        TypeDSect = (uint)1,
        TypeNoLoad = (uint)2,
        TypeGroup = (uint)4,
        TypeNoPad = (uint)8,
        TypeCopy = (uint)16,
        ContainsCode = (uint)32,
        ContainsInitializedData = (uint)64,
        ContainsUninitializedData = (uint)128,
        LinkerOther = (uint)256,
        LinkerInfo = (uint)512,
        TypeOver = (uint)1024,
        LinkerRemove = (uint)2048,
        LinkerComdat = (uint)4096,
        MemProtected = (uint)16384,
        NoDeferSpecExc = (uint)16384,
        GPRel = (uint)32768,
        MemFardata = (uint)32768,
        MemSysheap = (uint)65536,
        Mem16Bit = (uint)131072,
        MemPurgeable = (uint)131072,
        MemLocked = (uint)262144,
        MemPreload = (uint)524288,
        Align1Bytes = (uint)1048576,
        Align2Bytes = (uint)2097152,
        Align4Bytes = (uint)3145728,
        Align8Bytes = (uint)4194304,
        Align16Bytes = (uint)5242880,
        Align32Bytes = (uint)6291456,
        Align64Bytes = (uint)7340032,
        Align128Bytes = (uint)8388608,
        Align256Bytes = (uint)9437184,
        Align512Bytes = (uint)10485760,
        Align1024Bytes = (uint)11534336,
        Align2048Bytes = (uint)12582912,
        Align4096Bytes = (uint)13631488,
        Align8192Bytes = (uint)14680064,
        AlignMask = (uint)15728640,
        LinkerNRelocOvfl = (uint)16777216,
        MemDiscardable = (uint)33554432,
        MemNotCached = (uint)67108864,
        MemNotPaged = (uint)134217728,
        MemShared = (uint)268435456,
        MemExecute = (uint)536870912,
        MemRead = (uint)1073741824,
        MemWrite = (uint)2147483648,
    }
    public readonly partial struct SectionHeader
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public string Name { get { throw null; } }
        public ushort NumberOfLineNumbers { get { throw null; } }
        public ushort NumberOfRelocations { get { throw null; } }
        public int PointerToLineNumbers { get { throw null; } }
        public int PointerToRawData { get { throw null; } }
        public int PointerToRelocations { get { throw null; } }
        public System.Reflection.PortableExecutable.SectionCharacteristics SectionCharacteristics { get { throw null; } }
        public int SizeOfRawData { get { throw null; } }
        public int VirtualAddress { get { throw null; } }
        public int VirtualSize { get { throw null; } }
    }
    public readonly partial struct SectionLocation
    {
        private readonly int _dummyPrimitive;
        public SectionLocation(int relativeVirtualAddress, int pointerToRawData) { throw null; }
        public int PointerToRawData { get { throw null; } }
        public int RelativeVirtualAddress { get { throw null; } }
    }
    public enum Subsystem : ushort
    {
        Unknown = (ushort)0,
        Native = (ushort)1,
        WindowsGui = (ushort)2,
        WindowsCui = (ushort)3,
        OS2Cui = (ushort)5,
        PosixCui = (ushort)7,
        NativeWindows = (ushort)8,
        WindowsCEGui = (ushort)9,
        EfiApplication = (ushort)10,
        EfiBootServiceDriver = (ushort)11,
        EfiRuntimeDriver = (ushort)12,
        EfiRom = (ushort)13,
        Xbox = (ushort)14,
        WindowsBootApplication = (ushort)16,
    }
}
