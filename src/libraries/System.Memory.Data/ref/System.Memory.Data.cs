// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System
{
    public partial class BinaryData
    {
        public BinaryData(byte[] data) { }
        public BinaryData(object? jsonSerializable, System.Text.Json.JsonSerializerOptions? options = null, System.Type? type = null) { }
        public BinaryData(System.ReadOnlyMemory<byte> data) { }
        public BinaryData(string data) { }
        public static BinaryData Empty { get; }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] object? obj) { throw null; }
        public static System.BinaryData FromBytes(byte[] data) { throw null; }
        public static System.BinaryData FromBytes(System.ReadOnlyMemory<byte> data) { throw null; }
        public static System.BinaryData FromObjectAsJson<T>(T jsonSerializable, System.Text.Json.JsonSerializerOptions? options = null) { throw null; }
        public static System.BinaryData FromStream(System.IO.Stream stream) { throw null; }
        public static System.Threading.Tasks.Task<System.BinaryData> FromStreamAsync(System.IO.Stream stream, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static System.BinaryData FromString(string data) { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public override int GetHashCode() { throw null; }
        public static implicit operator System.ReadOnlyMemory<byte> (System.BinaryData? data) { throw null; }
        public static implicit operator System.ReadOnlySpan<byte> (System.BinaryData? data) { throw null; }
        public byte[] ToArray() { throw null; }
        public System.ReadOnlyMemory<byte> ToMemory() { throw null; }
        public T? ToObjectFromJson<T>(System.Text.Json.JsonSerializerOptions? options = null) { throw null; }
        public System.IO.Stream ToStream() { throw null; }
        public override string ToString() { throw null; }
    }
}
