// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// NOTE: Types/members which are not publicly exposed in System.Runtime.dll but are forwarded from the mscorlib.dll shim.
//       Manually maintained.

namespace System
{
    public sealed partial class CultureAwareComparer : System.StringComparer, System.Runtime.Serialization.ISerializable
    {
        internal CultureAwareComparer() { }
        public override int Compare(string? x, string? y) { throw null; }
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] object? obj) { throw null; }
        public override bool Equals(string? x, string? y) { throw null; }
        public override int GetHashCode() { throw null; }
        public override int GetHashCode(string obj) { throw null; }
        public void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    }
    public partial class OrdinalComparer : System.StringComparer
    {
        internal OrdinalComparer() { }
        public override int Compare(string? x, string? y) { throw null; }
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] object? obj) { throw null; }
        public override bool Equals(string? x, string? y) { throw null; }
        public override int GetHashCode() { throw null; }
        public override int GetHashCode(string obj) { throw null; }
    }
    public sealed partial class UnitySerializationHolder : System.Runtime.Serialization.IObjectReference, System.Runtime.Serialization.ISerializable
    {
        public UnitySerializationHolder(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public object GetRealObject(System.Runtime.Serialization.StreamingContext context) { throw null; }
    }
}
namespace System.Collections
{
    public partial class ListDictionaryInternal : System.Collections.ICollection, System.Collections.IDictionary, System.Collections.IEnumerable
    {
        public ListDictionaryInternal() { }
        public int Count { get { throw null; } }
        public bool IsFixedSize { get { throw null; } }
        public bool IsReadOnly { get { throw null; } }
        public bool IsSynchronized { get { throw null; } }
        public object? this[object key] { get { throw null; } set { } }
        public System.Collections.ICollection Keys { get { throw null; } }
        public object SyncRoot { get { throw null; } }
        public System.Collections.ICollection Values { get { throw null; } }
        public void Add(object key, object? value) { }
        public void Clear() { }
        public bool Contains(object key) { throw null; }
        public void CopyTo(System.Array array, int index) { }
        public System.Collections.IDictionaryEnumerator GetEnumerator() { throw null; }
        public void Remove(object key) { }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
    }
}
namespace System.Collections.Generic
{
    public sealed partial class ByteEqualityComparer : System.Collections.Generic.EqualityComparer<byte>
    {
        public ByteEqualityComparer() { }
        public override bool Equals(byte x, byte y) { throw null; }
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] object? obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public override int GetHashCode(byte b) { throw null; }
    }
    public sealed partial class EnumEqualityComparer<T> : System.Collections.Generic.EqualityComparer<T>, System.Runtime.Serialization.ISerializable where T : struct
    {
        public EnumEqualityComparer() { }
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] object? obj) { throw null; }
        public override bool Equals(T x, T y) { throw null; }
        public override int GetHashCode() { throw null; }
        public override int GetHashCode(T obj) { throw null; }
        public void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    }
    public sealed partial class GenericComparer<T> : System.Collections.Generic.Comparer<T> where T : System.IComparable<T>
    {
        public GenericComparer() { }
        public override int Compare(T? x, T? y) { throw null; }
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] object? obj) { throw null; }
        public override int GetHashCode() { throw null; }
    }
    public sealed partial class GenericEqualityComparer<T> : System.Collections.Generic.EqualityComparer<T> where T : System.IEquatable<T>
    {
        public GenericEqualityComparer() { }
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] object? obj) { throw null; }
        public override bool Equals(T? x, T? y) { throw null; }
        public override int GetHashCode() { throw null; }
        public override int GetHashCode([System.Diagnostics.CodeAnalysis.DisallowNullAttribute] T obj) { throw null; }
    }
    public partial class NonRandomizedStringEqualityComparer : System.Collections.Generic.IEqualityComparer<string?>, System.Runtime.Serialization.ISerializable
    {
        protected NonRandomizedStringEqualityComparer(System.Runtime.Serialization.SerializationInfo information, System.Runtime.Serialization.StreamingContext context) { }
        public virtual bool Equals(string? x, string? y) { throw null; }
        public virtual int GetHashCode(string? obj) { throw null; }
        public static System.Collections.Generic.IEqualityComparer<string>? GetStringComparer(object? comparer) { throw null; }
        public virtual System.Collections.Generic.IEqualityComparer<string?> GetUnderlyingEqualityComparer() { throw null; }
        void System.Runtime.Serialization.ISerializable.GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    }
    public sealed partial class NullableComparer<T> : System.Collections.Generic.Comparer<T?>, System.Runtime.Serialization.ISerializable where T : struct
    {
        public NullableComparer() { }
        public override int Compare(T? x, T? y) { throw null; }
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] object? obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    }
    public sealed partial class NullableEqualityComparer<T> : System.Collections.Generic.EqualityComparer<T?>, System.Runtime.Serialization.ISerializable where T : struct
    {
        public NullableEqualityComparer() { }
        public override bool Equals(T? x, T? y) { throw null; }
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] object? obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public override int GetHashCode(T? obj) { throw null; }
        public void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    }
    public sealed partial class ObjectComparer<T> : System.Collections.Generic.Comparer<T>
    {
        public ObjectComparer() { }
        public override int Compare(T? x, T? y) { throw null; }
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] object? obj) { throw null; }
        public override int GetHashCode() { throw null; }
    }
    public sealed partial class ObjectEqualityComparer<T> : System.Collections.Generic.EqualityComparer<T>
    {
        public ObjectEqualityComparer() { }
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] object? obj) { throw null; }
        public override bool Equals(T? x, T? y) { throw null; }
        public override int GetHashCode() { throw null; }
        public override int GetHashCode([System.Diagnostics.CodeAnalysis.DisallowNullAttribute] T obj) { throw null; }
    }
}
namespace System.Diagnostics.Contracts
{
    public sealed partial class ContractException : System.Exception
    {
        public ContractException(System.Diagnostics.Contracts.ContractFailureKind kind, string? failure, string? userMessage, string? condition, System.Exception? innerException) { }
        public string? Condition { get { throw null; } }
        public string Failure { get { throw null; } }
        public System.Diagnostics.Contracts.ContractFailureKind Kind { get { throw null; } }
        public string? UserMessage { get { throw null; } }
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    }
}
namespace System.Reflection.Emit
{
    public enum PEFileKinds
    {
        Dll = 1,
        ConsoleApplication = 2,
        WindowApplication = 3,
    }
}
