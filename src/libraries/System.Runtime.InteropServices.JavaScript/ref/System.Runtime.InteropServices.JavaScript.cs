// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Runtime.InteropServices.JavaScript
{
    public abstract partial class AnyRef : Microsoft.Win32.SafeHandles.SafeHandleMinusOneIsInvalid
    {
        internal AnyRef() : base (default(bool)) { }
        public int JSHandle { get { throw null; } }
        protected void FreeGCHandle() { }
    }
    public partial class Array : System.Runtime.InteropServices.JavaScript.CoreObject
    {
        public Array(params object[] _params) : base (default(int)) { }
        public object this[int i] { get { throw null; } set { } }
        public int IndexOf(object searchElement, int fromIndex = 0) { throw null; }
        public int LastIndexOf(object searchElement) { throw null; }
        public int LastIndexOf(object searchElement, int endIndex) { throw null; }
        public object Pop() { throw null; }
        public int Push(params object[] elements) { throw null; }
        public object Shift() { throw null; }
        public int UnShift(params object[] elements) { throw null; }
    }
    public partial class ArrayBuffer : System.Runtime.InteropServices.JavaScript.CoreObject
    {
        public ArrayBuffer() : base (default(int)) { }
        public ArrayBuffer(int length) : base (default(int)) { }
        public int ByteLength { get { throw null; } }
        public bool IsView { get { throw null; } }
        public System.Runtime.InteropServices.JavaScript.ArrayBuffer Slice(int begin) { throw null; }
        public System.Runtime.InteropServices.JavaScript.ArrayBuffer Slice(int begin, int end) { throw null; }
    }
    public abstract partial class CoreObject : System.Runtime.InteropServices.JavaScript.JSObject
    {
        protected CoreObject(int jsHandle) { }
    }
    public partial class DataView : System.Runtime.InteropServices.JavaScript.CoreObject
    {
        public DataView(System.Runtime.InteropServices.JavaScript.ArrayBuffer buffer) : base (default(int)) { }
        public DataView(System.Runtime.InteropServices.JavaScript.ArrayBuffer buffer, int byteOffset) : base (default(int)) { }
        public DataView(System.Runtime.InteropServices.JavaScript.ArrayBuffer buffer, int byteOffset, int byteLength) : base (default(int)) { }
        public DataView(System.Runtime.InteropServices.JavaScript.SharedArrayBuffer buffer) : base (default(int)) { }
        public DataView(System.Runtime.InteropServices.JavaScript.SharedArrayBuffer buffer, int byteOffset) : base (default(int)) { }
        public DataView(System.Runtime.InteropServices.JavaScript.SharedArrayBuffer buffer, int byteOffset, int byteLength) : base (default(int)) { }
        public System.Runtime.InteropServices.JavaScript.ArrayBuffer Buffer { get { throw null; } }
        public int ByteLength { get { throw null; } }
        public int ByteOffset { get { throw null; } }
        public float GetFloat32(int byteOffset, bool littleEndian = false) { throw null; }
        public double GetFloat64(int byteOffset, bool littleEndian = false) { throw null; }
        public short GetInt16(int byteOffset, bool littleEndian = false) { throw null; }
        public int GetInt32(int byteOffset, bool littleEndian = false) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public sbyte GetInt8(int byteOffset, bool littleEndian = false) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public ushort GetUint16(int byteOffset, bool littleEndian = false) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public uint GetUint32(int byteOffset, bool littleEndian = false) { throw null; }
        public byte GetUint8(int byteOffset, bool littleEndian = false) { throw null; }
        public void SetFloat32(int byteOffset, float value, bool littleEndian = false) { }
        public void SetFloat64(int byteOffset, double value, bool littleEndian = false) { }
        public void SetInt16(int byteOffset, short value, bool littleEndian = false) { }
        public void SetInt32(int byteOffset, int value, bool littleEndian = false) { }
        [System.CLSCompliantAttribute(false)]
        public void SetInt8(int byteOffset, sbyte value, bool littleEndian = false) { }
        [System.CLSCompliantAttribute(false)]
        public void SetUint16(int byteOffset, ushort value, bool littleEndian = false) { }
        [System.CLSCompliantAttribute(false)]
        public void SetUint32(int byteOffset, uint value, bool littleEndian = false) { }
        public void SetUint8(int byteOffset, byte value, bool littleEndian = false) { }
    }
    public sealed partial class Float32Array : System.Runtime.InteropServices.JavaScript.TypedArray<System.Runtime.InteropServices.JavaScript.Float32Array, float>
    {
        public Float32Array() { }
        public Float32Array(int length) { }
        public Float32Array(System.Runtime.InteropServices.JavaScript.ArrayBuffer buffer) { }
        public Float32Array(System.Runtime.InteropServices.JavaScript.ArrayBuffer buffer, int byteOffset) { }
        public Float32Array(System.Runtime.InteropServices.JavaScript.ArrayBuffer buffer, int byteOffset, int length) { }
        public Float32Array(System.Runtime.InteropServices.JavaScript.SharedArrayBuffer buffer) { }
        public Float32Array(System.Runtime.InteropServices.JavaScript.SharedArrayBuffer buffer, int byteOffset) { }
        public Float32Array(System.Runtime.InteropServices.JavaScript.SharedArrayBuffer buffer, int byteOffset, int length) { }
        public static implicit operator System.Span<float> (System.Runtime.InteropServices.JavaScript.Float32Array typedarray) { throw null; }
        public static implicit operator System.Runtime.InteropServices.JavaScript.Float32Array (System.Span<float> span) { throw null; }
    }
    public sealed partial class Float64Array : System.Runtime.InteropServices.JavaScript.TypedArray<System.Runtime.InteropServices.JavaScript.Float64Array, double>
    {
        public Float64Array() { }
        public Float64Array(int length) { }
        public Float64Array(System.Runtime.InteropServices.JavaScript.ArrayBuffer buffer) { }
        public Float64Array(System.Runtime.InteropServices.JavaScript.ArrayBuffer buffer, int byteOffset) { }
        public Float64Array(System.Runtime.InteropServices.JavaScript.ArrayBuffer buffer, int byteOffset, int length) { }
        public Float64Array(System.Runtime.InteropServices.JavaScript.SharedArrayBuffer buffer) { }
        public Float64Array(System.Runtime.InteropServices.JavaScript.SharedArrayBuffer buffer, int byteOffset) { }
        public Float64Array(System.Runtime.InteropServices.JavaScript.SharedArrayBuffer buffer, int byteOffset, int length) { }
        public static implicit operator System.Span<double> (System.Runtime.InteropServices.JavaScript.Float64Array typedarray) { throw null; }
        public static implicit operator System.Runtime.InteropServices.JavaScript.Float64Array (System.Span<double> span) { throw null; }
    }
    public partial class Function : System.Runtime.InteropServices.JavaScript.CoreObject
    {
        public Function(params object[] args) : base (default(int)) { }
        public object Apply(object? thisArg = null, object[]? argsArray = null) { throw null; }
        public System.Runtime.InteropServices.JavaScript.Function Bind(object? thisArg = null, object[]? argsArray = null) { throw null; }
        public object Call(object? thisArg = null, params object[] argsArray) { throw null; }
    }
    public partial class HostObject : System.Runtime.InteropServices.JavaScript.HostObjectBase
    {
        public HostObject(string hostName, params object[] _params) : base (default(int)) { }
    }
    public abstract partial class HostObjectBase : System.Runtime.InteropServices.JavaScript.JSObject, System.Runtime.InteropServices.JavaScript.IHostObject
    {
        protected HostObjectBase(int jHandle) { }
    }
    public partial interface IHostObject
    {
    }
    public partial interface IJSObject
    {
        int JSHandle { get; }
        int Length { get; }
    }
    public sealed partial class Int16Array : System.Runtime.InteropServices.JavaScript.TypedArray<System.Runtime.InteropServices.JavaScript.Int16Array, short>
    {
        public Int16Array() { }
        public Int16Array(int length) { }
        public Int16Array(System.Runtime.InteropServices.JavaScript.ArrayBuffer buffer) { }
        public Int16Array(System.Runtime.InteropServices.JavaScript.ArrayBuffer buffer, int byteOffset) { }
        public Int16Array(System.Runtime.InteropServices.JavaScript.ArrayBuffer buffer, int byteOffset, int length) { }
        public Int16Array(System.Runtime.InteropServices.JavaScript.SharedArrayBuffer buffer) { }
        public Int16Array(System.Runtime.InteropServices.JavaScript.SharedArrayBuffer buffer, int byteOffset) { }
        public Int16Array(System.Runtime.InteropServices.JavaScript.SharedArrayBuffer buffer, int byteOffset, int length) { }
        [System.CLSCompliantAttribute(false)]
        public static implicit operator System.Span<short> (System.Runtime.InteropServices.JavaScript.Int16Array typedarray) { throw null; }
        public static implicit operator System.Runtime.InteropServices.JavaScript.Int16Array (System.Span<short> span) { throw null; }
    }
    public sealed partial class Int32Array : System.Runtime.InteropServices.JavaScript.TypedArray<System.Runtime.InteropServices.JavaScript.Int32Array, int>
    {
        public Int32Array() { }
        public Int32Array(int length) { }
        public Int32Array(System.Runtime.InteropServices.JavaScript.ArrayBuffer buffer) { }
        public Int32Array(System.Runtime.InteropServices.JavaScript.ArrayBuffer buffer, int byteOffset) { }
        public Int32Array(System.Runtime.InteropServices.JavaScript.ArrayBuffer buffer, int byteOffset, int length) { }
        public Int32Array(System.Runtime.InteropServices.JavaScript.SharedArrayBuffer buffer) { }
        public Int32Array(System.Runtime.InteropServices.JavaScript.SharedArrayBuffer buffer, int byteOffset) { }
        public Int32Array(System.Runtime.InteropServices.JavaScript.SharedArrayBuffer buffer, int byteOffset, int length) { }
        public static implicit operator System.Span<int> (System.Runtime.InteropServices.JavaScript.Int32Array typedarray) { throw null; }
        public static implicit operator System.Runtime.InteropServices.JavaScript.Int32Array (System.Span<int> span) { throw null; }
    }
    [System.CLSCompliantAttribute(false)]
    public sealed partial class Int8Array : System.Runtime.InteropServices.JavaScript.TypedArray<System.Runtime.InteropServices.JavaScript.Int8Array, sbyte>
    {
        public Int8Array() { }
        public Int8Array(int length) { }
        public Int8Array(System.Runtime.InteropServices.JavaScript.ArrayBuffer buffer) { }
        public Int8Array(System.Runtime.InteropServices.JavaScript.ArrayBuffer buffer, int byteOffset) { }
        public Int8Array(System.Runtime.InteropServices.JavaScript.ArrayBuffer buffer, int byteOffset, int length) { }
        public Int8Array(System.Runtime.InteropServices.JavaScript.SharedArrayBuffer buffer) { }
        public Int8Array(System.Runtime.InteropServices.JavaScript.SharedArrayBuffer buffer, int byteOffset) { }
        public Int8Array(System.Runtime.InteropServices.JavaScript.SharedArrayBuffer buffer, int byteOffset, int length) { }
        [System.CLSCompliantAttribute(false)]
        public static implicit operator System.Span<sbyte> (System.Runtime.InteropServices.JavaScript.Int8Array typedarray) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static implicit operator System.Runtime.InteropServices.JavaScript.Int8Array (System.Span<sbyte> span) { throw null; }
    }
    public partial interface ITypedArray
    {
        System.Runtime.InteropServices.JavaScript.ArrayBuffer Buffer { get; }
        int ByteLength { get; }
        int BytesPerElement { get; }
        string Name { get; }
        System.Runtime.InteropServices.JavaScript.TypedArrayTypeCode GetTypedArrayType();
        void Set(System.Runtime.InteropServices.JavaScript.Array array);
        void Set(System.Runtime.InteropServices.JavaScript.Array array, int offset);
        void Set(System.Runtime.InteropServices.JavaScript.ITypedArray typedArray);
        void Set(System.Runtime.InteropServices.JavaScript.ITypedArray typedArray, int offset);
    }
    public partial interface ITypedArray<T, U> where U : struct
    {
        T Slice();
        T Slice(int begin);
        T Slice(int begin, int end);
        T SubArray();
        T SubArray(int begin);
        T SubArray(int begin, int end);
    }
    public partial class JSException : System.Exception
    {
        public JSException(string msg) { }
    }
    public partial class JSObject : System.Runtime.InteropServices.JavaScript.AnyRef, System.IDisposable, System.Runtime.InteropServices.JavaScript.IJSObject
    {
        public JSObject() { }
        public bool IsDisposed { get { throw null; } }
        public int Length { get { throw null; } set { } }
        public override bool Equals(object? obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public object GetObjectProperty(string name) { throw null; }
        public bool HasOwnProperty(string prop) { throw null; }
        public object Invoke(string method, params object?[] args) { throw null; }
        public bool PropertyIsEnumerable(string prop) { throw null; }
        protected override bool ReleaseHandle() { throw null; }
        public void SetObjectProperty(string name, object value, bool createIfNotExists = true, bool hasOwnProperty = false) { }
        public override string ToString() { throw null; }
    }
    public partial class Map : System.Runtime.InteropServices.JavaScript.CoreObject, System.Collections.ICollection, System.Collections.IDictionary, System.Collections.IEnumerable
    {
        public Map() : base (default(int)) { }
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
    public static partial class Runtime
    {
        public static System.Runtime.InteropServices.JavaScript.Function? CompileFunction(string snippet) { throw null; }
        public static void FreeObject(object obj) { }
        public static object GetGlobalObject(string? str = null) { throw null; }
        public static string InvokeJS(string str) { throw null; }
        public static int New(string hostClassName, params object[] parms) { throw null; }
        public static int New<T>(params object[] parms) { throw null; }
        public static System.IntPtr SafeHandleGetHandle(System.Runtime.InteropServices.SafeHandle safeHandle, bool addRef) { throw null; }
    }
    public partial class SharedArrayBuffer : System.Runtime.InteropServices.JavaScript.CoreObject
    {
        public SharedArrayBuffer(int length) : base (default(int)) { }
        public int ByteLength { get { throw null; } }
        public System.Runtime.InteropServices.JavaScript.SharedArrayBuffer Slice(int begin, int end) { throw null; }
    }
    public enum TypedArrayTypeCode
    {
        Int8Array = 5,
        Uint8Array = 6,
        Int16Array = 7,
        Uint16Array = 8,
        Int32Array = 9,
        Uint32Array = 10,
        Float32Array = 13,
        Float64Array = 14,
        Uint8ClampedArray = 15,
    }
    public abstract partial class TypedArray<T, U> : System.Runtime.InteropServices.JavaScript.CoreObject, System.Runtime.InteropServices.JavaScript.ITypedArray, System.Runtime.InteropServices.JavaScript.ITypedArray<T, U> where U : struct
    {
        protected TypedArray() : base (default(int)) { }
        protected TypedArray(int length) : base (default(int)) { }
        protected TypedArray(System.Runtime.InteropServices.JavaScript.ArrayBuffer buffer) : base (default(int)) { }
        protected TypedArray(System.Runtime.InteropServices.JavaScript.ArrayBuffer buffer, int byteOffset) : base (default(int)) { }
        protected TypedArray(System.Runtime.InteropServices.JavaScript.ArrayBuffer buffer, int byteOffset, int length) : base (default(int)) { }
        protected TypedArray(System.Runtime.InteropServices.JavaScript.SharedArrayBuffer buffer) : base (default(int)) { }
        protected TypedArray(System.Runtime.InteropServices.JavaScript.SharedArrayBuffer buffer, int byteOffset) : base (default(int)) { }
        protected TypedArray(System.Runtime.InteropServices.JavaScript.SharedArrayBuffer buffer, int byteOffset, int length) : base (default(int)) { }
        public System.Runtime.InteropServices.JavaScript.ArrayBuffer Buffer { get { throw null; } }
        public int ByteLength { get { throw null; } }
        public int BytesPerElement { get { throw null; } }
        public U? this[int i] { get { throw null; } set { } }
        public string Name { get { throw null; } }
        public int CopyFrom(System.ReadOnlySpan<U> span) { throw null; }
        public int CopyTo(System.Span<U> span) { throw null; }
        public void Fill(U value) { }
        public void Fill(U value, int start) { }
        public void Fill(U value, int start, int end) { }
        public static T From(System.ReadOnlySpan<U> span) { throw null; }
        public System.Runtime.InteropServices.JavaScript.TypedArrayTypeCode GetTypedArrayType() { throw null; }
        public void Set(System.Runtime.InteropServices.JavaScript.Array array) { }
        public void Set(System.Runtime.InteropServices.JavaScript.Array array, int offset) { }
        public void Set(System.Runtime.InteropServices.JavaScript.ITypedArray typedArray) { }
        public void Set(System.Runtime.InteropServices.JavaScript.ITypedArray typedArray, int offset) { }
        public T Slice() { throw null; }
        public T Slice(int begin) { throw null; }
        public T Slice(int begin, int end) { throw null; }
        public T SubArray() { throw null; }
        public T SubArray(int begin) { throw null; }
        public T SubArray(int begin, int end) { throw null; }
        public U[] ToArray() { throw null; }
    }
    [System.CLSCompliantAttribute(false)]
    public sealed partial class Uint16Array : System.Runtime.InteropServices.JavaScript.TypedArray<System.Runtime.InteropServices.JavaScript.Uint16Array, ushort>
    {
        public Uint16Array() { }
        public Uint16Array(int length) { }
        public Uint16Array(System.Runtime.InteropServices.JavaScript.ArrayBuffer buffer) { }
        public Uint16Array(System.Runtime.InteropServices.JavaScript.ArrayBuffer buffer, int byteOffset) { }
        public Uint16Array(System.Runtime.InteropServices.JavaScript.ArrayBuffer buffer, int byteOffset, int length) { }
        public Uint16Array(System.Runtime.InteropServices.JavaScript.SharedArrayBuffer buffer) { }
        public Uint16Array(System.Runtime.InteropServices.JavaScript.SharedArrayBuffer buffer, int byteOffset) { }
        public Uint16Array(System.Runtime.InteropServices.JavaScript.SharedArrayBuffer buffer, int byteOffset, int length) { }
        public static implicit operator System.Span<ushort> (System.Runtime.InteropServices.JavaScript.Uint16Array typedarray) { throw null; }
        public static implicit operator System.Runtime.InteropServices.JavaScript.Uint16Array (System.Span<ushort> span) { throw null; }
    }
    [System.CLSCompliantAttribute(false)]
    public sealed partial class Uint32Array : System.Runtime.InteropServices.JavaScript.TypedArray<System.Runtime.InteropServices.JavaScript.Uint32Array, uint>
    {
        public Uint32Array() { }
        public Uint32Array(int length) { }
        public Uint32Array(System.Runtime.InteropServices.JavaScript.ArrayBuffer buffer) { }
        public Uint32Array(System.Runtime.InteropServices.JavaScript.ArrayBuffer buffer, int byteOffset) { }
        public Uint32Array(System.Runtime.InteropServices.JavaScript.ArrayBuffer buffer, int byteOffset, int length) { }
        public Uint32Array(System.Runtime.InteropServices.JavaScript.SharedArrayBuffer buffer) { }
        public Uint32Array(System.Runtime.InteropServices.JavaScript.SharedArrayBuffer buffer, int byteOffset) { }
        public Uint32Array(System.Runtime.InteropServices.JavaScript.SharedArrayBuffer buffer, int byteOffset, int length) { }
        public static implicit operator System.Span<uint> (System.Runtime.InteropServices.JavaScript.Uint32Array typedarray) { throw null; }
        public static implicit operator System.Runtime.InteropServices.JavaScript.Uint32Array (System.Span<uint> span) { throw null; }
    }
    public sealed partial class Uint8Array : System.Runtime.InteropServices.JavaScript.TypedArray<System.Runtime.InteropServices.JavaScript.Uint8Array, byte>
    {
        public Uint8Array() { }
        public Uint8Array(int length) { }
        public Uint8Array(System.Runtime.InteropServices.JavaScript.ArrayBuffer buffer) { }
        public Uint8Array(System.Runtime.InteropServices.JavaScript.ArrayBuffer buffer, int byteOffset) { }
        public Uint8Array(System.Runtime.InteropServices.JavaScript.ArrayBuffer buffer, int byteOffset, int length) { }
        public Uint8Array(System.Runtime.InteropServices.JavaScript.SharedArrayBuffer buffer) { }
        public Uint8Array(System.Runtime.InteropServices.JavaScript.SharedArrayBuffer buffer, int byteOffset) { }
        public Uint8Array(System.Runtime.InteropServices.JavaScript.SharedArrayBuffer buffer, int byteOffset, int length) { }
        public static implicit operator System.Span<byte> (System.Runtime.InteropServices.JavaScript.Uint8Array typedarray) { throw null; }
        public static implicit operator System.Runtime.InteropServices.JavaScript.Uint8Array (System.Span<byte> span) { throw null; }
    }
    public sealed partial class Uint8ClampedArray : System.Runtime.InteropServices.JavaScript.TypedArray<System.Runtime.InteropServices.JavaScript.Uint8ClampedArray, byte>
    {
        public Uint8ClampedArray() { }
        public Uint8ClampedArray(int length) { }
        public Uint8ClampedArray(System.Runtime.InteropServices.JavaScript.ArrayBuffer buffer) { }
        public Uint8ClampedArray(System.Runtime.InteropServices.JavaScript.ArrayBuffer buffer, int byteOffset) { }
        public Uint8ClampedArray(System.Runtime.InteropServices.JavaScript.ArrayBuffer buffer, int byteOffset, int length) { }
        public Uint8ClampedArray(System.Runtime.InteropServices.JavaScript.SharedArrayBuffer buffer) { }
        public Uint8ClampedArray(System.Runtime.InteropServices.JavaScript.SharedArrayBuffer buffer, int byteOffset) { }
        public Uint8ClampedArray(System.Runtime.InteropServices.JavaScript.SharedArrayBuffer buffer, int byteOffset, int length) { }
        public static implicit operator System.Span<byte> (System.Runtime.InteropServices.JavaScript.Uint8ClampedArray typedarray) { throw null; }
        public static implicit operator System.Runtime.InteropServices.JavaScript.Uint8ClampedArray (System.Span<byte> span) { throw null; }
    }
}
