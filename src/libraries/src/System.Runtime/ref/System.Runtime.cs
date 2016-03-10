// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------


namespace System
{
    public delegate void Action();
    public delegate void Action<in T>(T obj);
    public delegate void Action<in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, in T9, in T10>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10);
    public delegate void Action<in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, in T9, in T10, in T11>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11);
    public delegate void Action<in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, in T9, in T10, in T11, in T12>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12);
    public delegate void Action<in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, in T9, in T10, in T11, in T12, in T13>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13);
    public delegate void Action<in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, in T9, in T10, in T11, in T12, in T13, in T14>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14);
    public delegate void Action<in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, in T9, in T10, in T11, in T12, in T13, in T14, in T15>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, T15 arg15);
    public delegate void Action<in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, in T9, in T10, in T11, in T12, in T13, in T14, in T15, in T16>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, T15 arg15, T16 arg16);
    public delegate void Action<in T1, in T2>(T1 arg1, T2 arg2);
    public delegate void Action<in T1, in T2, in T3>(T1 arg1, T2 arg2, T3 arg3);
    public delegate void Action<in T1, in T2, in T3, in T4>(T1 arg1, T2 arg2, T3 arg3, T4 arg4);
    public delegate void Action<in T1, in T2, in T3, in T4, in T5>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5);
    public delegate void Action<in T1, in T2, in T3, in T4, in T5, in T6>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6);
    public delegate void Action<in T1, in T2, in T3, in T4, in T5, in T6, in T7>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7);
    public delegate void Action<in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8);
    public delegate void Action<in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, in T9>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9);
    public static partial class Activator
    {
        public static object CreateInstance(System.Type type) { return default(object); }
        public static object CreateInstance(System.Type type, params object[] args) { return default(object); }
        public static T CreateInstance<T>() { return default(T); }
    }
    public partial class ArgumentException : System.Exception
    {
        public ArgumentException() { }
        public ArgumentException(string message) { }
        public ArgumentException(string message, System.Exception innerException) { }
        public ArgumentException(string message, string paramName) { }
        public ArgumentException(string message, string paramName, System.Exception innerException) { }
        public override string Message { get { return default(string); } }
        public virtual string ParamName { get { return default(string); } }
    }
    public partial class ArgumentNullException : System.ArgumentException
    {
        public ArgumentNullException() { }
        public ArgumentNullException(string paramName) { }
        public ArgumentNullException(string message, System.Exception innerException) { }
        public ArgumentNullException(string paramName, string message) { }
    }
    public partial class ArgumentOutOfRangeException : System.ArgumentException
    {
        public ArgumentOutOfRangeException() { }
        public ArgumentOutOfRangeException(string paramName) { }
        public ArgumentOutOfRangeException(string message, System.Exception innerException) { }
        public ArgumentOutOfRangeException(string paramName, object actualValue, string message) { }
        public ArgumentOutOfRangeException(string paramName, string message) { }
        public virtual object ActualValue { get { return default(object); } }
        public override string Message { get { return default(string); } }
    }
    public partial class ArithmeticException : System.Exception
    {
        public ArithmeticException() { }
        public ArithmeticException(string message) { }
        public ArithmeticException(string message, System.Exception innerException) { }
    }
    public abstract partial class Array : System.Collections.ICollection, System.Collections.IEnumerable, System.Collections.IList, System.Collections.IStructuralComparable, System.Collections.IStructuralEquatable
    {
        internal Array() { }
        public int Length { get { return default(int); } }
        public int Rank { get { return default(int); } }
        int System.Collections.ICollection.Count { get { return default(int); } }
        bool System.Collections.ICollection.IsSynchronized { get { return default(bool); } }
        object System.Collections.ICollection.SyncRoot { get { return default(object); } }
        bool System.Collections.IList.IsFixedSize { get { return default(bool); } }
        bool System.Collections.IList.IsReadOnly { get { return default(bool); } }
        object System.Collections.IList.this[int index] { get { return default(object); } set { } }
        public static int BinarySearch(System.Array array, int index, int length, object value) { return default(int); }
        public static int BinarySearch(System.Array array, int index, int length, object value, System.Collections.IComparer comparer) { return default(int); }
        public static int BinarySearch(System.Array array, object value) { return default(int); }
        public static int BinarySearch(System.Array array, object value, System.Collections.IComparer comparer) { return default(int); }
        public static int BinarySearch<T>(T[] array, T value) { return default(int); }
        public static int BinarySearch<T>(T[] array, T value, System.Collections.Generic.IComparer<T> comparer) { return default(int); }
        public static int BinarySearch<T>(T[] array, int index, int length, T value) { return default(int); }
        public static int BinarySearch<T>(T[] array, int index, int length, T value, System.Collections.Generic.IComparer<T> comparer) { return default(int); }
        public static void Clear(System.Array array, int index, int length) { }
        public object Clone() { return default(object); }
        public static void ConstrainedCopy(System.Array sourceArray, int sourceIndex, System.Array destinationArray, int destinationIndex, int length) { }
        public static void Copy(System.Array sourceArray, System.Array destinationArray, int length) { }
        public static void Copy(System.Array sourceArray, int sourceIndex, System.Array destinationArray, int destinationIndex, int length) { }
        public void CopyTo(System.Array array, int index) { }
        public static System.Array CreateInstance(System.Type elementType, int length) { return default(System.Array); }
        public static System.Array CreateInstance(System.Type elementType, params int[] lengths) { return default(System.Array); }
        public static System.Array CreateInstance(System.Type elementType, int[] lengths, int[] lowerBounds) { return default(System.Array); }
        public static T[] Empty<T>() { return default(T[]); }
        public static bool Exists<T>(T[] array, System.Predicate<T> match) { return default(bool); }
        public static T Find<T>(T[] array, System.Predicate<T> match) { return default(T); }
        public static T[] FindAll<T>(T[] array, System.Predicate<T> match) { return default(T[]); }
        public static int FindIndex<T>(T[] array, int startIndex, int count, System.Predicate<T> match) { return default(int); }
        public static int FindIndex<T>(T[] array, int startIndex, System.Predicate<T> match) { return default(int); }
        public static int FindIndex<T>(T[] array, System.Predicate<T> match) { return default(int); }
        public static T FindLast<T>(T[] array, System.Predicate<T> match) { return default(T); }
        public static int FindLastIndex<T>(T[] array, int startIndex, int count, System.Predicate<T> match) { return default(int); }
        public static int FindLastIndex<T>(T[] array, int startIndex, System.Predicate<T> match) { return default(int); }
        public static int FindLastIndex<T>(T[] array, System.Predicate<T> match) { return default(int); }
        public System.Collections.IEnumerator GetEnumerator() { return default(System.Collections.IEnumerator); }
        public int GetLength(int dimension) { return default(int); }
        public int GetLowerBound(int dimension) { return default(int); }
        public int GetUpperBound(int dimension) { return default(int); }
        public object GetValue(int index) { return default(object); }
        public object GetValue(params int[] indices) { return default(object); }
        public static int IndexOf(System.Array array, object value) { return default(int); }
        public static int IndexOf(System.Array array, object value, int startIndex) { return default(int); }
        public static int IndexOf(System.Array array, object value, int startIndex, int count) { return default(int); }
        public static int IndexOf<T>(T[] array, T value) { return default(int); }
        public static int IndexOf<T>(T[] array, T value, int startIndex) { return default(int); }
        public static int IndexOf<T>(T[] array, T value, int startIndex, int count) { return default(int); }
        public void Initialize() { }
        public static int LastIndexOf(System.Array array, object value) { return default(int); }
        public static int LastIndexOf(System.Array array, object value, int startIndex) { return default(int); }
        public static int LastIndexOf(System.Array array, object value, int startIndex, int count) { return default(int); }
        public static int LastIndexOf<T>(T[] array, T value) { return default(int); }
        public static int LastIndexOf<T>(T[] array, T value, int startIndex) { return default(int); }
        public static int LastIndexOf<T>(T[] array, T value, int startIndex, int count) { return default(int); }
        public static void Resize<T>(ref T[] array, int newSize) { }
        public static void Reverse(System.Array array) { }
        public static void Reverse(System.Array array, int index, int length) { }
        public void SetValue(object value, int index) { }
        public void SetValue(object value, params int[] indices) { }
        public static void Sort(System.Array array) { }
        public static void Sort(System.Array keys, System.Array items) { }
        public static void Sort(System.Array keys, System.Array items, System.Collections.IComparer comparer) { }
        public static void Sort(System.Array keys, System.Array items, int index, int length) { }
        public static void Sort(System.Array keys, System.Array items, int index, int length, System.Collections.IComparer comparer) { }
        public static void Sort(System.Array array, System.Collections.IComparer comparer) { }
        public static void Sort(System.Array array, int index, int length) { }
        public static void Sort(System.Array array, int index, int length, System.Collections.IComparer comparer) { }
        public static void Sort<T>(T[] array) { }
        public static void Sort<T>(T[] array, System.Collections.Generic.IComparer<T> comparer) { }
        public static void Sort<T>(T[] array, System.Comparison<T> comparison) { }
        public static void Sort<T>(T[] array, int index, int length) { }
        public static void Sort<T>(T[] array, int index, int length, System.Collections.Generic.IComparer<T> comparer) { }
        public static void Sort<TKey, TValue>(TKey[] keys, TValue[] items) { }
        public static void Sort<TKey, TValue>(TKey[] keys, TValue[] items, System.Collections.Generic.IComparer<TKey> comparer) { }
        public static void Sort<TKey, TValue>(TKey[] keys, TValue[] items, int index, int length) { }
        public static void Sort<TKey, TValue>(TKey[] keys, TValue[] items, int index, int length, System.Collections.Generic.IComparer<TKey> comparer) { }
        int System.Collections.IList.Add(object value) { return default(int); }
        void System.Collections.IList.Clear() { }
        bool System.Collections.IList.Contains(object value) { return default(bool); }
        int System.Collections.IList.IndexOf(object value) { return default(int); }
        void System.Collections.IList.Insert(int index, object value) { }
        void System.Collections.IList.Remove(object value) { }
        void System.Collections.IList.RemoveAt(int index) { }
        int System.Collections.IStructuralComparable.CompareTo(object other, System.Collections.IComparer comparer) { return default(int); }
        bool System.Collections.IStructuralEquatable.Equals(object other, System.Collections.IEqualityComparer comparer) { return default(bool); }
        int System.Collections.IStructuralEquatable.GetHashCode(System.Collections.IEqualityComparer comparer) { return default(int); }
        public static bool TrueForAll<T>(T[] array, System.Predicate<T> match) { return default(bool); }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct ArraySegment<T> : System.Collections.Generic.ICollection<T>, System.Collections.Generic.IEnumerable<T>, System.Collections.Generic.IList<T>, System.Collections.Generic.IReadOnlyCollection<T>, System.Collections.Generic.IReadOnlyList<T>, System.Collections.IEnumerable
    {
        public ArraySegment(T[] array) { throw new System.NotImplementedException(); }
        public ArraySegment(T[] array, int offset, int count) { throw new System.NotImplementedException(); }
        public T[] Array { get { return default(T[]); } }
        public int Count { get { return default(int); } }
        public int Offset { get { return default(int); } }
        bool System.Collections.Generic.ICollection<T>.IsReadOnly { get { return default(bool); } }
        T System.Collections.Generic.IList<T>.this[int index] { get { return default(T); } set { } }
        T System.Collections.Generic.IReadOnlyList<T>.this[int index] { get { return default(T); } }
        public bool Equals(System.ArraySegment<T> obj) { return default(bool); }
        public override bool Equals(object obj) { return default(bool); }
        public override int GetHashCode() { return default(int); }
        public static bool operator ==(System.ArraySegment<T> a, System.ArraySegment<T> b) { return default(bool); }
        public static bool operator !=(System.ArraySegment<T> a, System.ArraySegment<T> b) { return default(bool); }
        void System.Collections.Generic.ICollection<T>.Add(T item) { }
        void System.Collections.Generic.ICollection<T>.Clear() { }
        bool System.Collections.Generic.ICollection<T>.Contains(T item) { return default(bool); }
        void System.Collections.Generic.ICollection<T>.CopyTo(T[] array, int arrayIndex) { }
        bool System.Collections.Generic.ICollection<T>.Remove(T item) { return default(bool); }
        System.Collections.Generic.IEnumerator<T> System.Collections.Generic.IEnumerable<T>.GetEnumerator() { return default(System.Collections.Generic.IEnumerator<T>); }
        int System.Collections.Generic.IList<T>.IndexOf(T item) { return default(int); }
        void System.Collections.Generic.IList<T>.Insert(int index, T item) { }
        void System.Collections.Generic.IList<T>.RemoveAt(int index) { }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return default(System.Collections.IEnumerator); }
    }
    public partial class ArrayTypeMismatchException : System.Exception
    {
        public ArrayTypeMismatchException() { }
        public ArrayTypeMismatchException(string message) { }
        public ArrayTypeMismatchException(string message, System.Exception innerException) { }
    }
    public delegate void AsyncCallback(System.IAsyncResult ar);
    [System.AttributeUsageAttribute((System.AttributeTargets)(32767), Inherited = true, AllowMultiple = false)]
    public abstract partial class Attribute
    {
        protected Attribute() { }
        public override bool Equals(object obj) { return default(bool); }
        public override int GetHashCode() { return default(int); }
    }
    [System.FlagsAttribute]
    public enum AttributeTargets
    {
        All = 32767,
        Assembly = 1,
        Class = 4,
        Constructor = 32,
        Delegate = 4096,
        Enum = 16,
        Event = 512,
        Field = 256,
        GenericParameter = 16384,
        Interface = 1024,
        Method = 64,
        Module = 2,
        Parameter = 2048,
        Property = 128,
        ReturnValue = 8192,
        Struct = 8,
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(4), Inherited = true)]
    public sealed partial class AttributeUsageAttribute : System.Attribute
    {
        public AttributeUsageAttribute(System.AttributeTargets validOn) { }
        public bool AllowMultiple { get { return default(bool); } set { } }
        public bool Inherited { get { return default(bool); } set { } }
        public System.AttributeTargets ValidOn { get { return default(System.AttributeTargets); } }
    }
    public partial class BadImageFormatException : System.Exception
    {
        public BadImageFormatException() { }
        public BadImageFormatException(string message) { }
        public BadImageFormatException(string message, System.Exception inner) { }
        public BadImageFormatException(string message, string fileName) { }
        public BadImageFormatException(string message, string fileName, System.Exception inner) { }
        public string FileName { get { return default(string); } }
        public override string Message { get { return default(string); } }
        public override string ToString() { return default(string); }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct Boolean : System.IComparable, System.IComparable<bool>, System.IConvertible, System.IEquatable<bool>
    {
        public static readonly string FalseString;
        public static readonly string TrueString;
        public int CompareTo(bool value) { return default(int); }
        public bool Equals(bool obj) { return default(bool); }
        public override bool Equals(object obj) { return default(bool); }
        public override int GetHashCode() { return default(int); }
        public static bool Parse(string value) { return default(bool); }
        int System.IComparable.CompareTo(object obj) { return default(int); }
        System.TypeCode System.IConvertible.GetTypeCode() { return default(System.TypeCode); }
        bool System.IConvertible.ToBoolean(System.IFormatProvider provider) { return default(bool); }
        byte System.IConvertible.ToByte(System.IFormatProvider provider) { return default(byte); }
        char System.IConvertible.ToChar(System.IFormatProvider provider) { return default(char); }
        System.DateTime System.IConvertible.ToDateTime(System.IFormatProvider provider) { return default(System.DateTime); }
        decimal System.IConvertible.ToDecimal(System.IFormatProvider provider) { return default(decimal); }
        double System.IConvertible.ToDouble(System.IFormatProvider provider) { return default(double); }
        short System.IConvertible.ToInt16(System.IFormatProvider provider) { return default(short); }
        int System.IConvertible.ToInt32(System.IFormatProvider provider) { return default(int); }
        long System.IConvertible.ToInt64(System.IFormatProvider provider) { return default(long); }
        sbyte System.IConvertible.ToSByte(System.IFormatProvider provider) { return default(sbyte); }
        float System.IConvertible.ToSingle(System.IFormatProvider provider) { return default(float); }
        string System.IConvertible.ToString(System.IFormatProvider provider) { return default(string); }
        object System.IConvertible.ToType(System.Type type, System.IFormatProvider provider) { return default(object); }
        ushort System.IConvertible.ToUInt16(System.IFormatProvider provider) { return default(ushort); }
        uint System.IConvertible.ToUInt32(System.IFormatProvider provider) { return default(uint); }
        ulong System.IConvertible.ToUInt64(System.IFormatProvider provider) { return default(ulong); }
        public override string ToString() { return default(string); }
        public static bool TryParse(string value, out bool result) { result = default(bool); return default(bool); }
    }
    public static partial class Buffer
    {
        public static void BlockCopy(System.Array src, int srcOffset, System.Array dst, int dstOffset, int count) { }
        public static int ByteLength(System.Array array) { return default(int); }
        public static byte GetByte(System.Array array, int index) { return default(byte); }
        [System.CLSCompliantAttribute(false)]
        [System.Security.SecurityCriticalAttribute]
        public unsafe static void MemoryCopy(void* source, void* destination, long destinationSizeInBytes, long sourceBytesToCopy) { }
        [System.CLSCompliantAttribute(false)]
        [System.Security.SecurityCriticalAttribute]
        public unsafe static void MemoryCopy(void* source, void* destination, ulong destinationSizeInBytes, ulong sourceBytesToCopy) { }
        public static void SetByte(System.Array array, int index, byte value) { }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct Byte : System.IComparable, System.IComparable<byte>, System.IConvertible, System.IEquatable<byte>, System.IFormattable
    {
        public const byte MaxValue = (byte)255;
        public const byte MinValue = (byte)0;
        public int CompareTo(byte value) { return default(int); }
        public bool Equals(byte obj) { return default(bool); }
        public override bool Equals(object obj) { return default(bool); }
        public override int GetHashCode() { return default(int); }
        public static byte Parse(string s) { return default(byte); }
        public static byte Parse(string s, System.Globalization.NumberStyles style) { return default(byte); }
        public static byte Parse(string s, System.Globalization.NumberStyles style, System.IFormatProvider provider) { return default(byte); }
        public static byte Parse(string s, System.IFormatProvider provider) { return default(byte); }
        int System.IComparable.CompareTo(object value) { return default(int); }
        System.TypeCode System.IConvertible.GetTypeCode() { return default(System.TypeCode); }
        bool System.IConvertible.ToBoolean(System.IFormatProvider provider) { return default(bool); }
        byte System.IConvertible.ToByte(System.IFormatProvider provider) { return default(byte); }
        char System.IConvertible.ToChar(System.IFormatProvider provider) { return default(char); }
        System.DateTime System.IConvertible.ToDateTime(System.IFormatProvider provider) { return default(System.DateTime); }
        decimal System.IConvertible.ToDecimal(System.IFormatProvider provider) { return default(decimal); }
        double System.IConvertible.ToDouble(System.IFormatProvider provider) { return default(double); }
        short System.IConvertible.ToInt16(System.IFormatProvider provider) { return default(short); }
        int System.IConvertible.ToInt32(System.IFormatProvider provider) { return default(int); }
        long System.IConvertible.ToInt64(System.IFormatProvider provider) { return default(long); }
        sbyte System.IConvertible.ToSByte(System.IFormatProvider provider) { return default(sbyte); }
        float System.IConvertible.ToSingle(System.IFormatProvider provider) { return default(float); }
        object System.IConvertible.ToType(System.Type type, System.IFormatProvider provider) { return default(object); }
        ushort System.IConvertible.ToUInt16(System.IFormatProvider provider) { return default(ushort); }
        uint System.IConvertible.ToUInt32(System.IFormatProvider provider) { return default(uint); }
        ulong System.IConvertible.ToUInt64(System.IFormatProvider provider) { return default(ulong); }
        public override string ToString() { return default(string); }
        public string ToString(System.IFormatProvider provider) { return default(string); }
        public string ToString(string format) { return default(string); }
        public string ToString(string format, System.IFormatProvider provider) { return default(string); }
        public static bool TryParse(string s, out byte result) { result = default(byte); return default(bool); }
        public static bool TryParse(string s, System.Globalization.NumberStyles style, System.IFormatProvider provider, out byte result) { result = default(byte); return default(bool); }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct Char : System.IComparable, System.IComparable<char>, System.IConvertible, System.IEquatable<char>
    {
        public const char MaxValue = '\uFFFF';
        public const char MinValue = '\0';
        public int CompareTo(char value) { return default(int); }
        public static string ConvertFromUtf32(int utf32) { return default(string); }
        public static int ConvertToUtf32(char highSurrogate, char lowSurrogate) { return default(int); }
        public static int ConvertToUtf32(string s, int index) { return default(int); }
        public bool Equals(char obj) { return default(bool); }
        public override bool Equals(object obj) { return default(bool); }
        public override int GetHashCode() { return default(int); }
        public static double GetNumericValue(char c) { return default(double); }
        public static double GetNumericValue(string s, int index) { return default(double); }
        public static bool IsControl(char c) { return default(bool); }
        public static bool IsControl(string s, int index) { return default(bool); }
        public static bool IsDigit(char c) { return default(bool); }
        public static bool IsDigit(string s, int index) { return default(bool); }
        public static bool IsHighSurrogate(char c) { return default(bool); }
        public static bool IsHighSurrogate(string s, int index) { return default(bool); }
        public static bool IsLetter(char c) { return default(bool); }
        public static bool IsLetter(string s, int index) { return default(bool); }
        public static bool IsLetterOrDigit(char c) { return default(bool); }
        public static bool IsLetterOrDigit(string s, int index) { return default(bool); }
        public static bool IsLower(char c) { return default(bool); }
        public static bool IsLower(string s, int index) { return default(bool); }
        public static bool IsLowSurrogate(char c) { return default(bool); }
        public static bool IsLowSurrogate(string s, int index) { return default(bool); }
        public static bool IsNumber(char c) { return default(bool); }
        public static bool IsNumber(string s, int index) { return default(bool); }
        public static bool IsPunctuation(char c) { return default(bool); }
        public static bool IsPunctuation(string s, int index) { return default(bool); }
        public static bool IsSeparator(char c) { return default(bool); }
        public static bool IsSeparator(string s, int index) { return default(bool); }
        public static bool IsSurrogate(char c) { return default(bool); }
        public static bool IsSurrogate(string s, int index) { return default(bool); }
        public static bool IsSurrogatePair(char highSurrogate, char lowSurrogate) { return default(bool); }
        public static bool IsSurrogatePair(string s, int index) { return default(bool); }
        public static bool IsSymbol(char c) { return default(bool); }
        public static bool IsSymbol(string s, int index) { return default(bool); }
        public static bool IsUpper(char c) { return default(bool); }
        public static bool IsUpper(string s, int index) { return default(bool); }
        public static bool IsWhiteSpace(char c) { return default(bool); }
        public static bool IsWhiteSpace(string s, int index) { return default(bool); }
        public static char Parse(string s) { return default(char); }
        int System.IComparable.CompareTo(object value) { return default(int); }
        System.TypeCode System.IConvertible.GetTypeCode() { return default(System.TypeCode); }
        bool System.IConvertible.ToBoolean(System.IFormatProvider provider) { return default(bool); }
        byte System.IConvertible.ToByte(System.IFormatProvider provider) { return default(byte); }
        char System.IConvertible.ToChar(System.IFormatProvider provider) { return default(char); }
        System.DateTime System.IConvertible.ToDateTime(System.IFormatProvider provider) { return default(System.DateTime); }
        decimal System.IConvertible.ToDecimal(System.IFormatProvider provider) { return default(decimal); }
        double System.IConvertible.ToDouble(System.IFormatProvider provider) { return default(double); }
        short System.IConvertible.ToInt16(System.IFormatProvider provider) { return default(short); }
        int System.IConvertible.ToInt32(System.IFormatProvider provider) { return default(int); }
        long System.IConvertible.ToInt64(System.IFormatProvider provider) { return default(long); }
        sbyte System.IConvertible.ToSByte(System.IFormatProvider provider) { return default(sbyte); }
        float System.IConvertible.ToSingle(System.IFormatProvider provider) { return default(float); }
        string System.IConvertible.ToString(System.IFormatProvider provider) { return default(string); }
        object System.IConvertible.ToType(System.Type type, System.IFormatProvider provider) { return default(object); }
        ushort System.IConvertible.ToUInt16(System.IFormatProvider provider) { return default(ushort); }
        uint System.IConvertible.ToUInt32(System.IFormatProvider provider) { return default(uint); }
        ulong System.IConvertible.ToUInt64(System.IFormatProvider provider) { return default(ulong); }
        public static char ToLower(char c) { return default(char); }
        public static char ToLowerInvariant(char c) { return default(char); }
        public override string ToString() { return default(string); }
        public static string ToString(char c) { return default(string); }
        public static char ToUpper(char c) { return default(char); }
        public static char ToUpperInvariant(char c) { return default(char); }
        public static bool TryParse(string s, out char result) { result = default(char); return default(bool); }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(32767), Inherited = true, AllowMultiple = false)]
    public sealed partial class CLSCompliantAttribute : System.Attribute
    {
        public CLSCompliantAttribute(bool isCompliant) { }
        public bool IsCompliant { get { return default(bool); } }
    }
    public delegate int Comparison<in T>(T x, T y);
    public partial struct DateTime : System.IComparable, System.IComparable<System.DateTime>, System.IConvertible, System.IEquatable<System.DateTime>, System.IFormattable
    {
        public static readonly System.DateTime MaxValue;
        public static readonly System.DateTime MinValue;
        public DateTime(int year, int month, int day) { throw new System.NotImplementedException(); }
        public DateTime(int year, int month, int day, int hour, int minute, int second) { throw new System.NotImplementedException(); }
        public DateTime(int year, int month, int day, int hour, int minute, int second, System.DateTimeKind kind) { throw new System.NotImplementedException(); }
        public DateTime(int year, int month, int day, int hour, int minute, int second, int millisecond) { throw new System.NotImplementedException(); }
        public DateTime(int year, int month, int day, int hour, int minute, int second, int millisecond, System.DateTimeKind kind) { throw new System.NotImplementedException(); }
        public DateTime(long ticks) { throw new System.NotImplementedException(); }
        public DateTime(long ticks, System.DateTimeKind kind) { throw new System.NotImplementedException(); }
        public System.DateTime Date { get { return default(System.DateTime); } }
        public int Day { get { return default(int); } }
        public System.DayOfWeek DayOfWeek { get { return default(System.DayOfWeek); } }
        public int DayOfYear { get { return default(int); } }
        public int Hour { get { return default(int); } }
        public System.DateTimeKind Kind { get { return default(System.DateTimeKind); } }
        public int Millisecond { get { return default(int); } }
        public int Minute { get { return default(int); } }
        public int Month { get { return default(int); } }
        public static System.DateTime Now { get { return default(System.DateTime); } }
        public int Second { get { return default(int); } }
        public long Ticks { get { return default(long); } }
        public System.TimeSpan TimeOfDay { get { return default(System.TimeSpan); } }
        public static System.DateTime Today { get { return default(System.DateTime); } }
        public static System.DateTime UtcNow { get { return default(System.DateTime); } }
        public int Year { get { return default(int); } }
        public System.DateTime Add(System.TimeSpan value) { return default(System.DateTime); }
        public System.DateTime AddDays(double value) { return default(System.DateTime); }
        public System.DateTime AddHours(double value) { return default(System.DateTime); }
        public System.DateTime AddMilliseconds(double value) { return default(System.DateTime); }
        public System.DateTime AddMinutes(double value) { return default(System.DateTime); }
        public System.DateTime AddMonths(int months) { return default(System.DateTime); }
        public System.DateTime AddSeconds(double value) { return default(System.DateTime); }
        public System.DateTime AddTicks(long value) { return default(System.DateTime); }
        public System.DateTime AddYears(int value) { return default(System.DateTime); }
        public static int Compare(System.DateTime t1, System.DateTime t2) { return default(int); }
        public int CompareTo(System.DateTime value) { return default(int); }
        public static int DaysInMonth(int year, int month) { return default(int); }
        public bool Equals(System.DateTime value) { return default(bool); }
        public static bool Equals(System.DateTime t1, System.DateTime t2) { return default(bool); }
        public override bool Equals(object value) { return default(bool); }
        public static System.DateTime FromBinary(long dateData) { return default(System.DateTime); }
        public static System.DateTime FromFileTime(long fileTime) { return default(System.DateTime); }
        public static System.DateTime FromFileTimeUtc(long fileTime) { return default(System.DateTime); }
        public string[] GetDateTimeFormats() { return default(string[]); }
        public string[] GetDateTimeFormats(char format) { return default(string[]); }
        public string[] GetDateTimeFormats(char format, System.IFormatProvider provider) { return default(string[]); }
        public string[] GetDateTimeFormats(System.IFormatProvider provider) { return default(string[]); }
        public override int GetHashCode() { return default(int); }
        public bool IsDaylightSavingTime() { return default(bool); }
        public static bool IsLeapYear(int year) { return default(bool); }
        public static System.DateTime operator +(System.DateTime d, System.TimeSpan t) { return default(System.DateTime); }
        public static bool operator ==(System.DateTime d1, System.DateTime d2) { return default(bool); }
        public static bool operator >(System.DateTime t1, System.DateTime t2) { return default(bool); }
        public static bool operator >=(System.DateTime t1, System.DateTime t2) { return default(bool); }
        public static bool operator !=(System.DateTime d1, System.DateTime d2) { return default(bool); }
        public static bool operator <(System.DateTime t1, System.DateTime t2) { return default(bool); }
        public static bool operator <=(System.DateTime t1, System.DateTime t2) { return default(bool); }
        public static System.TimeSpan operator -(System.DateTime d1, System.DateTime d2) { return default(System.TimeSpan); }
        public static System.DateTime operator -(System.DateTime d, System.TimeSpan t) { return default(System.DateTime); }
        public static System.DateTime Parse(string s) { return default(System.DateTime); }
        public static System.DateTime Parse(string s, System.IFormatProvider provider) { return default(System.DateTime); }
        public static System.DateTime Parse(string s, System.IFormatProvider provider, System.Globalization.DateTimeStyles styles) { return default(System.DateTime); }
        public static System.DateTime ParseExact(string s, string format, System.IFormatProvider provider) { return default(System.DateTime); }
        public static System.DateTime ParseExact(string s, string format, System.IFormatProvider provider, System.Globalization.DateTimeStyles style) { return default(System.DateTime); }
        public static System.DateTime ParseExact(string s, string[] formats, System.IFormatProvider provider, System.Globalization.DateTimeStyles style) { return default(System.DateTime); }
        public static System.DateTime SpecifyKind(System.DateTime value, System.DateTimeKind kind) { return default(System.DateTime); }
        public System.TimeSpan Subtract(System.DateTime value) { return default(System.TimeSpan); }
        public System.DateTime Subtract(System.TimeSpan value) { return default(System.DateTime); }
        int System.IComparable.CompareTo(object value) { return default(int); }
        System.TypeCode System.IConvertible.GetTypeCode() { return default(System.TypeCode); }
        bool System.IConvertible.ToBoolean(System.IFormatProvider provider) { return default(bool); }
        byte System.IConvertible.ToByte(System.IFormatProvider provider) { return default(byte); }
        char System.IConvertible.ToChar(System.IFormatProvider provider) { return default(char); }
        System.DateTime System.IConvertible.ToDateTime(System.IFormatProvider provider) { return default(System.DateTime); }
        decimal System.IConvertible.ToDecimal(System.IFormatProvider provider) { return default(decimal); }
        double System.IConvertible.ToDouble(System.IFormatProvider provider) { return default(double); }
        short System.IConvertible.ToInt16(System.IFormatProvider provider) { return default(short); }
        int System.IConvertible.ToInt32(System.IFormatProvider provider) { return default(int); }
        long System.IConvertible.ToInt64(System.IFormatProvider provider) { return default(long); }
        sbyte System.IConvertible.ToSByte(System.IFormatProvider provider) { return default(sbyte); }
        float System.IConvertible.ToSingle(System.IFormatProvider provider) { return default(float); }
        object System.IConvertible.ToType(System.Type type, System.IFormatProvider provider) { return default(object); }
        ushort System.IConvertible.ToUInt16(System.IFormatProvider provider) { return default(ushort); }
        uint System.IConvertible.ToUInt32(System.IFormatProvider provider) { return default(uint); }
        ulong System.IConvertible.ToUInt64(System.IFormatProvider provider) { return default(ulong); }
        public long ToBinary() { return default(long); }
        public long ToFileTime() { return default(long); }
        public long ToFileTimeUtc() { return default(long); }
        public System.DateTime ToLocalTime() { return default(System.DateTime); }
        public override string ToString() { return default(string); }
        public string ToString(System.IFormatProvider provider) { return default(string); }
        public string ToString(string format) { return default(string); }
        public string ToString(string format, System.IFormatProvider provider) { return default(string); }
        public System.DateTime ToUniversalTime() { return default(System.DateTime); }
        public static bool TryParse(string s, out System.DateTime result) { result = default(System.DateTime); return default(bool); }
        public static bool TryParse(string s, System.IFormatProvider provider, System.Globalization.DateTimeStyles styles, out System.DateTime result) { result = default(System.DateTime); return default(bool); }
        public static bool TryParseExact(string s, string format, System.IFormatProvider provider, System.Globalization.DateTimeStyles style, out System.DateTime result) { result = default(System.DateTime); return default(bool); }
        public static bool TryParseExact(string s, string[] formats, System.IFormatProvider provider, System.Globalization.DateTimeStyles style, out System.DateTime result) { result = default(System.DateTime); return default(bool); }
    }
    public enum DateTimeKind
    {
        Local = 2,
        Unspecified = 0,
        Utc = 1,
    }
    public partial struct DateTimeOffset : System.IComparable, System.IComparable<System.DateTimeOffset>, System.IEquatable<System.DateTimeOffset>, System.IFormattable
    {
        public static readonly System.DateTimeOffset MaxValue;
        public static readonly System.DateTimeOffset MinValue;
        public DateTimeOffset(System.DateTime dateTime) { throw new System.NotImplementedException(); }
        public DateTimeOffset(System.DateTime dateTime, System.TimeSpan offset) { throw new System.NotImplementedException(); }
        public DateTimeOffset(int year, int month, int day, int hour, int minute, int second, int millisecond, System.TimeSpan offset) { throw new System.NotImplementedException(); }
        public DateTimeOffset(int year, int month, int day, int hour, int minute, int second, System.TimeSpan offset) { throw new System.NotImplementedException(); }
        public DateTimeOffset(long ticks, System.TimeSpan offset) { throw new System.NotImplementedException(); }
        public System.DateTime Date { get { return default(System.DateTime); } }
        public System.DateTime DateTime { get { return default(System.DateTime); } }
        public int Day { get { return default(int); } }
        public System.DayOfWeek DayOfWeek { get { return default(System.DayOfWeek); } }
        public int DayOfYear { get { return default(int); } }
        public int Hour { get { return default(int); } }
        public System.DateTime LocalDateTime { get { return default(System.DateTime); } }
        public int Millisecond { get { return default(int); } }
        public int Minute { get { return default(int); } }
        public int Month { get { return default(int); } }
        public static System.DateTimeOffset Now { get { return default(System.DateTimeOffset); } }
        public System.TimeSpan Offset { get { return default(System.TimeSpan); } }
        public int Second { get { return default(int); } }
        public long Ticks { get { return default(long); } }
        public System.TimeSpan TimeOfDay { get { return default(System.TimeSpan); } }
        public System.DateTime UtcDateTime { get { return default(System.DateTime); } }
        public static System.DateTimeOffset UtcNow { get { return default(System.DateTimeOffset); } }
        public long UtcTicks { get { return default(long); } }
        public int Year { get { return default(int); } }
        public System.DateTimeOffset Add(System.TimeSpan timeSpan) { return default(System.DateTimeOffset); }
        public System.DateTimeOffset AddDays(double days) { return default(System.DateTimeOffset); }
        public System.DateTimeOffset AddHours(double hours) { return default(System.DateTimeOffset); }
        public System.DateTimeOffset AddMilliseconds(double milliseconds) { return default(System.DateTimeOffset); }
        public System.DateTimeOffset AddMinutes(double minutes) { return default(System.DateTimeOffset); }
        public System.DateTimeOffset AddMonths(int months) { return default(System.DateTimeOffset); }
        public System.DateTimeOffset AddSeconds(double seconds) { return default(System.DateTimeOffset); }
        public System.DateTimeOffset AddTicks(long ticks) { return default(System.DateTimeOffset); }
        public System.DateTimeOffset AddYears(int years) { return default(System.DateTimeOffset); }
        public static int Compare(System.DateTimeOffset first, System.DateTimeOffset second) { return default(int); }
        public int CompareTo(System.DateTimeOffset other) { return default(int); }
        public bool Equals(System.DateTimeOffset other) { return default(bool); }
        public static bool Equals(System.DateTimeOffset first, System.DateTimeOffset second) { return default(bool); }
        public override bool Equals(object obj) { return default(bool); }
        public bool EqualsExact(System.DateTimeOffset other) { return default(bool); }
        public static System.DateTimeOffset FromFileTime(long fileTime) { return default(System.DateTimeOffset); }
        public static System.DateTimeOffset FromUnixTimeMilliseconds(long milliseconds) { return default(System.DateTimeOffset); }
        public static System.DateTimeOffset FromUnixTimeSeconds(long seconds) { return default(System.DateTimeOffset); }
        public override int GetHashCode() { return default(int); }
        public static System.DateTimeOffset operator +(System.DateTimeOffset dateTimeOffset, System.TimeSpan timeSpan) { return default(System.DateTimeOffset); }
        public static bool operator ==(System.DateTimeOffset left, System.DateTimeOffset right) { return default(bool); }
        public static bool operator >(System.DateTimeOffset left, System.DateTimeOffset right) { return default(bool); }
        public static bool operator >=(System.DateTimeOffset left, System.DateTimeOffset right) { return default(bool); }
        public static implicit operator System.DateTimeOffset(System.DateTime dateTime) { return default(System.DateTimeOffset); }
        public static bool operator !=(System.DateTimeOffset left, System.DateTimeOffset right) { return default(bool); }
        public static bool operator <(System.DateTimeOffset left, System.DateTimeOffset right) { return default(bool); }
        public static bool operator <=(System.DateTimeOffset left, System.DateTimeOffset right) { return default(bool); }
        public static System.TimeSpan operator -(System.DateTimeOffset left, System.DateTimeOffset right) { return default(System.TimeSpan); }
        public static System.DateTimeOffset operator -(System.DateTimeOffset dateTimeOffset, System.TimeSpan timeSpan) { return default(System.DateTimeOffset); }
        public static System.DateTimeOffset Parse(string input) { return default(System.DateTimeOffset); }
        public static System.DateTimeOffset Parse(string input, System.IFormatProvider formatProvider) { return default(System.DateTimeOffset); }
        public static System.DateTimeOffset Parse(string input, System.IFormatProvider formatProvider, System.Globalization.DateTimeStyles styles) { return default(System.DateTimeOffset); }
        public static System.DateTimeOffset ParseExact(string input, string format, System.IFormatProvider formatProvider) { return default(System.DateTimeOffset); }
        public static System.DateTimeOffset ParseExact(string input, string format, System.IFormatProvider formatProvider, System.Globalization.DateTimeStyles styles) { return default(System.DateTimeOffset); }
        public static System.DateTimeOffset ParseExact(string input, string[] formats, System.IFormatProvider formatProvider, System.Globalization.DateTimeStyles styles) { return default(System.DateTimeOffset); }
        public System.TimeSpan Subtract(System.DateTimeOffset value) { return default(System.TimeSpan); }
        public System.DateTimeOffset Subtract(System.TimeSpan value) { return default(System.DateTimeOffset); }
        int System.IComparable.CompareTo(object obj) { return default(int); }
        public long ToFileTime() { return default(long); }
        public System.DateTimeOffset ToLocalTime() { return default(System.DateTimeOffset); }
        public System.DateTimeOffset ToOffset(System.TimeSpan offset) { return default(System.DateTimeOffset); }
        public override string ToString() { return default(string); }
        public string ToString(System.IFormatProvider formatProvider) { return default(string); }
        public string ToString(string format) { return default(string); }
        public string ToString(string format, System.IFormatProvider formatProvider) { return default(string); }
        public System.DateTimeOffset ToUniversalTime() { return default(System.DateTimeOffset); }
        public long ToUnixTimeMilliseconds() { return default(long); }
        public long ToUnixTimeSeconds() { return default(long); }
        public static bool TryParse(string input, out System.DateTimeOffset result) { result = default(System.DateTimeOffset); return default(bool); }
        public static bool TryParse(string input, System.IFormatProvider formatProvider, System.Globalization.DateTimeStyles styles, out System.DateTimeOffset result) { result = default(System.DateTimeOffset); return default(bool); }
        public static bool TryParseExact(string input, string format, System.IFormatProvider formatProvider, System.Globalization.DateTimeStyles styles, out System.DateTimeOffset result) { result = default(System.DateTimeOffset); return default(bool); }
        public static bool TryParseExact(string input, string[] formats, System.IFormatProvider formatProvider, System.Globalization.DateTimeStyles styles, out System.DateTimeOffset result) { result = default(System.DateTimeOffset); return default(bool); }
    }
    public enum DayOfWeek
    {
        Friday = 5,
        Monday = 1,
        Saturday = 6,
        Sunday = 0,
        Thursday = 4,
        Tuesday = 2,
        Wednesday = 3,
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct Decimal : System.IComparable, System.IComparable<decimal>, System.IConvertible, System.IEquatable<decimal>, System.IFormattable
    {
        [System.Runtime.CompilerServices.DecimalConstantAttribute((byte)0, (byte)0, (uint)4294967295, (uint)4294967295, (uint)4294967295)]
        public static readonly decimal MaxValue;
        [System.Runtime.CompilerServices.DecimalConstantAttribute((byte)0, (byte)128, (uint)0, (uint)0, (uint)1)]
        public static readonly decimal MinusOne;
        [System.Runtime.CompilerServices.DecimalConstantAttribute((byte)0, (byte)128, (uint)4294967295, (uint)4294967295, (uint)4294967295)]
        public static readonly decimal MinValue;
        [System.Runtime.CompilerServices.DecimalConstantAttribute((byte)0, (byte)0, (uint)0, (uint)0, (uint)1)]
        public static readonly decimal One;
        [System.Runtime.CompilerServices.DecimalConstantAttribute((byte)0, (byte)0, (uint)0, (uint)0, (uint)0)]
        public static readonly decimal Zero;
        public Decimal(double value) { throw new System.NotImplementedException(); }
        public Decimal(int value) { throw new System.NotImplementedException(); }
        public Decimal(int lo, int mid, int hi, bool isNegative, byte scale) { throw new System.NotImplementedException(); }
        public Decimal(int[] bits) { throw new System.NotImplementedException(); }
        public Decimal(long value) { throw new System.NotImplementedException(); }
        public Decimal(float value) { throw new System.NotImplementedException(); }
        [System.CLSCompliantAttribute(false)]
        public Decimal(uint value) { throw new System.NotImplementedException(); }
        [System.CLSCompliantAttribute(false)]
        public Decimal(ulong value) { throw new System.NotImplementedException(); }
        public static decimal Add(decimal d1, decimal d2) { return default(decimal); }
        public static decimal Ceiling(decimal d) { return default(decimal); }
        public static int Compare(decimal d1, decimal d2) { return default(int); }
        public int CompareTo(decimal value) { return default(int); }
        public static decimal Divide(decimal d1, decimal d2) { return default(decimal); }
        public bool Equals(decimal value) { return default(bool); }
        public static bool Equals(decimal d1, decimal d2) { return default(bool); }
        public override bool Equals(object value) { return default(bool); }
        public static decimal Floor(decimal d) { return default(decimal); }
        public static int[] GetBits(decimal d) { return default(int[]); }
        public override int GetHashCode() { return default(int); }
        public static decimal Multiply(decimal d1, decimal d2) { return default(decimal); }
        public static decimal Negate(decimal d) { return default(decimal); }
        public static decimal operator +(decimal d1, decimal d2) { return default(decimal); }
        public static decimal operator --(decimal d) { return default(decimal); }
        public static decimal operator /(decimal d1, decimal d2) { return default(decimal); }
        public static bool operator ==(decimal d1, decimal d2) { return default(bool); }
        public static explicit operator byte (decimal value) { return default(byte); }
        public static explicit operator char (decimal value) { return default(char); }
        public static explicit operator double (decimal value) { return default(double); }
        public static explicit operator short (decimal value) { return default(short); }
        public static explicit operator int (decimal value) { return default(int); }
        public static explicit operator long (decimal value) { return default(long); }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator sbyte (decimal value) { return default(sbyte); }
        public static explicit operator float (decimal value) { return default(float); }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator ushort (decimal value) { return default(ushort); }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator uint (decimal value) { return default(uint); }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator ulong (decimal value) { return default(ulong); }
        public static explicit operator decimal (double value) { return default(decimal); }
        public static explicit operator decimal (float value) { return default(decimal); }
        public static bool operator >(decimal d1, decimal d2) { return default(bool); }
        public static bool operator >=(decimal d1, decimal d2) { return default(bool); }
        public static implicit operator decimal (byte value) { return default(decimal); }
        public static implicit operator decimal (char value) { return default(decimal); }
        public static implicit operator decimal (short value) { return default(decimal); }
        public static implicit operator decimal (int value) { return default(decimal); }
        public static implicit operator decimal (long value) { return default(decimal); }
        [System.CLSCompliantAttribute(false)]
        public static implicit operator decimal (sbyte value) { return default(decimal); }
        [System.CLSCompliantAttribute(false)]
        public static implicit operator decimal (ushort value) { return default(decimal); }
        [System.CLSCompliantAttribute(false)]
        public static implicit operator decimal (uint value) { return default(decimal); }
        [System.CLSCompliantAttribute(false)]
        public static implicit operator decimal (ulong value) { return default(decimal); }
        public static decimal operator ++(decimal d) { return default(decimal); }
        public static bool operator !=(decimal d1, decimal d2) { return default(bool); }
        public static bool operator <(decimal d1, decimal d2) { return default(bool); }
        public static bool operator <=(decimal d1, decimal d2) { return default(bool); }
        public static decimal operator %(decimal d1, decimal d2) { return default(decimal); }
        public static decimal operator *(decimal d1, decimal d2) { return default(decimal); }
        public static decimal operator -(decimal d1, decimal d2) { return default(decimal); }
        public static decimal operator -(decimal d) { return default(decimal); }
        public static decimal operator +(decimal d) { return default(decimal); }
        public static decimal Parse(string s) { return default(decimal); }
        public static decimal Parse(string s, System.Globalization.NumberStyles style) { return default(decimal); }
        public static decimal Parse(string s, System.Globalization.NumberStyles style, System.IFormatProvider provider) { return default(decimal); }
        public static decimal Parse(string s, System.IFormatProvider provider) { return default(decimal); }
        public static decimal Remainder(decimal d1, decimal d2) { return default(decimal); }
        public static decimal Subtract(decimal d1, decimal d2) { return default(decimal); }
        int System.IComparable.CompareTo(object value) { return default(int); }
        System.TypeCode System.IConvertible.GetTypeCode() { return default(System.TypeCode); }
        bool System.IConvertible.ToBoolean(System.IFormatProvider provider) { return default(bool); }
        byte System.IConvertible.ToByte(System.IFormatProvider provider) { return default(byte); }
        char System.IConvertible.ToChar(System.IFormatProvider provider) { return default(char); }
        System.DateTime System.IConvertible.ToDateTime(System.IFormatProvider provider) { return default(System.DateTime); }
        decimal System.IConvertible.ToDecimal(System.IFormatProvider provider) { return default(decimal); }
        double System.IConvertible.ToDouble(System.IFormatProvider provider) { return default(double); }
        short System.IConvertible.ToInt16(System.IFormatProvider provider) { return default(short); }
        int System.IConvertible.ToInt32(System.IFormatProvider provider) { return default(int); }
        long System.IConvertible.ToInt64(System.IFormatProvider provider) { return default(long); }
        sbyte System.IConvertible.ToSByte(System.IFormatProvider provider) { return default(sbyte); }
        float System.IConvertible.ToSingle(System.IFormatProvider provider) { return default(float); }
        object System.IConvertible.ToType(System.Type type, System.IFormatProvider provider) { return default(object); }
        ushort System.IConvertible.ToUInt16(System.IFormatProvider provider) { return default(ushort); }
        uint System.IConvertible.ToUInt32(System.IFormatProvider provider) { return default(uint); }
        ulong System.IConvertible.ToUInt64(System.IFormatProvider provider) { return default(ulong); }
        public static byte ToByte(decimal value) { return default(byte); }
        public static double ToDouble(decimal d) { return default(double); }
        public static short ToInt16(decimal value) { return default(short); }
        public static int ToInt32(decimal d) { return default(int); }
        public static long ToInt64(decimal d) { return default(long); }
        [System.CLSCompliantAttribute(false)]
        public static sbyte ToSByte(decimal value) { return default(sbyte); }
        public static float ToSingle(decimal d) { return default(float); }
        public override string ToString() { return default(string); }
        public string ToString(System.IFormatProvider provider) { return default(string); }
        public string ToString(string format) { return default(string); }
        public string ToString(string format, System.IFormatProvider provider) { return default(string); }
        [System.CLSCompliantAttribute(false)]
        public static ushort ToUInt16(decimal value) { return default(ushort); }
        [System.CLSCompliantAttribute(false)]
        public static uint ToUInt32(decimal d) { return default(uint); }
        [System.CLSCompliantAttribute(false)]
        public static ulong ToUInt64(decimal d) { return default(ulong); }
        public static decimal Truncate(decimal d) { return default(decimal); }
        public static bool TryParse(string s, out decimal result) { result = default(decimal); return default(bool); }
        public static bool TryParse(string s, System.Globalization.NumberStyles style, System.IFormatProvider provider, out decimal result) { result = default(decimal); return default(bool); }
    }
    public abstract partial class Delegate
    {
        internal Delegate() { }
        public object Target { get { return default(object); } }
        public static System.Delegate Combine(System.Delegate a, System.Delegate b) { return default(System.Delegate); }
        public static System.Delegate Combine(params System.Delegate[] delegates) { return default(System.Delegate); }
        public object DynamicInvoke(params object[] args) { return default(object); }
        public override bool Equals(object obj) { return default(bool); }
        public override int GetHashCode() { return default(int); }
        public virtual System.Delegate[] GetInvocationList() { return default(System.Delegate[]); }
        public static bool operator ==(System.Delegate d1, System.Delegate d2) { return default(bool); }
        public static bool operator !=(System.Delegate d1, System.Delegate d2) { return default(bool); }
        public static System.Delegate Remove(System.Delegate source, System.Delegate value) { return default(System.Delegate); }
        public static System.Delegate RemoveAll(System.Delegate source, System.Delegate value) { return default(System.Delegate); }
    }
    public partial class DivideByZeroException : System.ArithmeticException
    {
        public DivideByZeroException() { }
        public DivideByZeroException(string message) { }
        public DivideByZeroException(string message, System.Exception innerException) { }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct Double : System.IComparable, System.IComparable<double>, System.IConvertible, System.IEquatable<double>, System.IFormattable
    {
        public int CompareTo(double value) { return default(int); }
        public bool Equals(double obj) { return default(bool); }
        public override bool Equals(object obj) { return default(bool); }
        public override int GetHashCode() { return default(int); }
        public static bool IsInfinity(double d) { return default(bool); }
        public static bool IsNaN(double d) { return default(bool); }
        public static bool IsNegativeInfinity(double d) { return default(bool); }
        public static bool IsPositiveInfinity(double d) { return default(bool); }
        public static bool operator ==(double left, double right) { return default(bool); }
        public static bool operator >(double left, double right) { return default(bool); }
        public static bool operator >=(double left, double right) { return default(bool); }
        public static bool operator !=(double left, double right) { return default(bool); }
        public static bool operator <(double left, double right) { return default(bool); }
        public static bool operator <=(double left, double right) { return default(bool); }
        public static double Parse(string s) { return default(double); }
        public static double Parse(string s, System.Globalization.NumberStyles style) { return default(double); }
        public static double Parse(string s, System.Globalization.NumberStyles style, System.IFormatProvider provider) { return default(double); }
        public static double Parse(string s, System.IFormatProvider provider) { return default(double); }
        int System.IComparable.CompareTo(object value) { return default(int); }
        System.TypeCode System.IConvertible.GetTypeCode() { return default(System.TypeCode); }
        bool System.IConvertible.ToBoolean(System.IFormatProvider provider) { return default(bool); }
        byte System.IConvertible.ToByte(System.IFormatProvider provider) { return default(byte); }
        char System.IConvertible.ToChar(System.IFormatProvider provider) { return default(char); }
        System.DateTime System.IConvertible.ToDateTime(System.IFormatProvider provider) { return default(System.DateTime); }
        decimal System.IConvertible.ToDecimal(System.IFormatProvider provider) { return default(decimal); }
        double System.IConvertible.ToDouble(System.IFormatProvider provider) { return default(double); }
        short System.IConvertible.ToInt16(System.IFormatProvider provider) { return default(short); }
        int System.IConvertible.ToInt32(System.IFormatProvider provider) { return default(int); }
        long System.IConvertible.ToInt64(System.IFormatProvider provider) { return default(long); }
        sbyte System.IConvertible.ToSByte(System.IFormatProvider provider) { return default(sbyte); }
        float System.IConvertible.ToSingle(System.IFormatProvider provider) { return default(float); }
        object System.IConvertible.ToType(System.Type type, System.IFormatProvider provider) { return default(object); }
        ushort System.IConvertible.ToUInt16(System.IFormatProvider provider) { return default(ushort); }
        uint System.IConvertible.ToUInt32(System.IFormatProvider provider) { return default(uint); }
        ulong System.IConvertible.ToUInt64(System.IFormatProvider provider) { return default(ulong); }
        public override string ToString() { return default(string); }
        public string ToString(System.IFormatProvider provider) { return default(string); }
        public string ToString(string format) { return default(string); }
        public string ToString(string format, System.IFormatProvider provider) { return default(string); }
        public static bool TryParse(string s, out double result) { result = default(double); return default(bool); }
        public static bool TryParse(string s, System.Globalization.NumberStyles style, System.IFormatProvider provider, out double result) { result = default(double); return default(bool); }
    }
    public abstract partial class Enum : System.ValueType, System.IComparable, System.IConvertible, System.IFormattable
    {
        protected Enum() { }
        public int CompareTo(object target) { return default(int); }
        public override bool Equals(object obj) { return default(bool); }
        public static string Format(System.Type enumType, object value, string format) { return default(string); }
        public override int GetHashCode() { return default(int); }
        public static string GetName(System.Type enumType, object value) { return default(string); }
        public static string[] GetNames(System.Type enumType) { return default(string[]); }
        public static System.Type GetUnderlyingType(System.Type enumType) { return default(System.Type); }
        public static System.Array GetValues(System.Type enumType) { return default(System.Array); }
        public bool HasFlag(System.Enum flag) { return default(bool); }
        public static bool IsDefined(System.Type enumType, object value) { return default(bool); }
        public static object Parse(System.Type enumType, string value) { return default(object); }
        public static object Parse(System.Type enumType, string value, bool ignoreCase) { return default(object); }
        System.TypeCode System.IConvertible.GetTypeCode() { return default(System.TypeCode); }
        bool System.IConvertible.ToBoolean(System.IFormatProvider provider) { return default(bool); }
        byte System.IConvertible.ToByte(System.IFormatProvider provider) { return default(byte); }
        char System.IConvertible.ToChar(System.IFormatProvider provider) { return default(char); }
        System.DateTime System.IConvertible.ToDateTime(System.IFormatProvider provider) { return default(System.DateTime); }
        decimal System.IConvertible.ToDecimal(System.IFormatProvider provider) { return default(decimal); }
        double System.IConvertible.ToDouble(System.IFormatProvider provider) { return default(double); }
        short System.IConvertible.ToInt16(System.IFormatProvider provider) { return default(short); }
        int System.IConvertible.ToInt32(System.IFormatProvider provider) { return default(int); }
        long System.IConvertible.ToInt64(System.IFormatProvider provider) { return default(long); }
        sbyte System.IConvertible.ToSByte(System.IFormatProvider provider) { return default(sbyte); }
        float System.IConvertible.ToSingle(System.IFormatProvider provider) { return default(float); }
        [System.ObsoleteAttribute("The provider argument is not used. Please use ToString().")]
        string System.IConvertible.ToString(System.IFormatProvider provider) { return default(string); }
        object System.IConvertible.ToType(System.Type type, System.IFormatProvider provider) { return default(object); }
        ushort System.IConvertible.ToUInt16(System.IFormatProvider provider) { return default(ushort); }
        uint System.IConvertible.ToUInt32(System.IFormatProvider provider) { return default(uint); }
        ulong System.IConvertible.ToUInt64(System.IFormatProvider provider) { return default(ulong); }
        [System.ObsoleteAttribute("The provider argument is not used. Please use ToString(String).")]
        string System.IFormattable.ToString(string format, System.IFormatProvider provider) { return default(string); }
        public static object ToObject(System.Type enumType, object value) { return default(object); }
        public override string ToString() { return default(string); }
        public string ToString(string format) { return default(string); }
        public static bool TryParse<TEnum>(string value, out TEnum result) where TEnum : struct { result = default(TEnum); return default(bool); }
        public static bool TryParse<TEnum>(string value, bool ignoreCase, out TEnum result) where TEnum : struct { result = default(TEnum); return default(bool); }
    }
    public partial class EventArgs
    {
        public static readonly System.EventArgs Empty;
        public EventArgs() { }
    }
    public delegate void EventHandler(object sender, System.EventArgs e);
    public delegate void EventHandler<TEventArgs>(object sender, TEventArgs e);
    public partial class Exception
    {
        public Exception() { }
        public Exception(string message) { }
        public Exception(string message, System.Exception innerException) { }
        public virtual System.Collections.IDictionary Data { get { return default(System.Collections.IDictionary); } }
        public virtual string HelpLink { get { return default(string); } set { } }
        public int HResult { get { return default(int); } protected set { } }
        public System.Exception InnerException { get { return default(System.Exception); } }
        public virtual string Message { get { return default(string); } }
        public virtual string Source { get { return default(string); } set { } }
        public virtual string StackTrace { get { return default(string); } }
        public virtual System.Exception GetBaseException() { return default(System.Exception); }
        public override string ToString() { return default(string); }
    }
    public partial class FieldAccessException : System.MemberAccessException
    {
        public FieldAccessException() { }
        public FieldAccessException(string message) { }
        public FieldAccessException(string message, System.Exception inner) { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(16), Inherited = false)]
    public partial class FlagsAttribute : System.Attribute
    {
        public FlagsAttribute() { }
    }
    public partial class FormatException : System.Exception
    {
        public FormatException() { }
        public FormatException(string message) { }
        public FormatException(string message, System.Exception innerException) { }
    }
    public abstract partial class FormattableString : System.IFormattable
    {
        protected FormattableString() { }
        public abstract int ArgumentCount { get; }
        public abstract string Format { get; }
        public abstract object GetArgument(int index);
        public abstract object[] GetArguments();
        public static string Invariant(System.FormattableString formattable) { return default(string); }
        string System.IFormattable.ToString(string ignored, System.IFormatProvider formatProvider) { return default(string); }
        public override string ToString() { return default(string); }
        public abstract string ToString(System.IFormatProvider formatProvider);
    }
    public delegate TResult Func<out TResult>();
    public delegate TResult Func<in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, in T9, out TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9);
    public delegate TResult Func<in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, in T9, in T10, out TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10);
    public delegate TResult Func<in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, in T9, in T10, in T11, out TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11);
    public delegate TResult Func<in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, in T9, in T10, in T11, in T12, out TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12);
    public delegate TResult Func<in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, in T9, in T10, in T11, in T12, in T13, out TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13);
    public delegate TResult Func<in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, in T9, in T10, in T11, in T12, in T13, in T14, out TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14);
    public delegate TResult Func<in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, in T9, in T10, in T11, in T12, in T13, in T14, in T15, out TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, T15 arg15);
    public delegate TResult Func<in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, in T9, in T10, in T11, in T12, in T13, in T14, in T15, in T16, out TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, T15 arg15, T16 arg16);
    public delegate TResult Func<in T, out TResult>(T arg);
    public delegate TResult Func<in T1, in T2, out TResult>(T1 arg1, T2 arg2);
    public delegate TResult Func<in T1, in T2, in T3, out TResult>(T1 arg1, T2 arg2, T3 arg3);
    public delegate TResult Func<in T1, in T2, in T3, in T4, out TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4);
    public delegate TResult Func<in T1, in T2, in T3, in T4, in T5, out TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5);
    public delegate TResult Func<in T1, in T2, in T3, in T4, in T5, in T6, out TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6);
    public delegate TResult Func<in T1, in T2, in T3, in T4, in T5, in T6, in T7, out TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7);
    public delegate TResult Func<in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, out TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8);
    public static partial class GC
    {
        public static int MaxGeneration { get { return default(int); } }
        [System.Security.SecurityCriticalAttribute]
        public static void AddMemoryPressure(long bytesAllocated) { }
        public static void Collect() { }
        public static void Collect(int generation) { }
        public static void Collect(int generation, System.GCCollectionMode mode) { }
        public static void Collect(int generation, System.GCCollectionMode mode, bool blocking) { }
        public static int CollectionCount(int generation) { return default(int); }
        public static int GetGeneration(object obj) { return default(int); }
        public static long GetTotalMemory(bool forceFullCollection) { return default(long); }
        public static void KeepAlive(object obj) { }
        [System.Security.SecurityCriticalAttribute]
        public static void RemoveMemoryPressure(long bytesAllocated) { }
        public static void ReRegisterForFinalize(object obj) { }
        public static void SuppressFinalize(object obj) { }
        public static void WaitForPendingFinalizers() { }
    }
    public enum GCCollectionMode
    {
        Default = 0,
        Forced = 1,
        Optimized = 2,
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct Guid : System.IComparable, System.IComparable<System.Guid>, System.IEquatable<System.Guid>, System.IFormattable
    {
        public static readonly System.Guid Empty;
        public Guid(byte[] b) { throw new System.NotImplementedException(); }
        public Guid(int a, short b, short c, byte d, byte e, byte f, byte g, byte h, byte i, byte j, byte k) { throw new System.NotImplementedException(); }
        public Guid(int a, short b, short c, byte[] d) { throw new System.NotImplementedException(); }
        public Guid(string g) { throw new System.NotImplementedException(); }
        [System.CLSCompliantAttribute(false)]
        public Guid(uint a, ushort b, ushort c, byte d, byte e, byte f, byte g, byte h, byte i, byte j, byte k) { throw new System.NotImplementedException(); }
        public int CompareTo(System.Guid value) { return default(int); }
        public bool Equals(System.Guid g) { return default(bool); }
        public override bool Equals(object o) { return default(bool); }
        public override int GetHashCode() { return default(int); }
        public static System.Guid NewGuid() { return default(System.Guid); }
        public static bool operator ==(System.Guid a, System.Guid b) { return default(bool); }
        public static bool operator !=(System.Guid a, System.Guid b) { return default(bool); }
        public static System.Guid Parse(string input) { return default(System.Guid); }
        public static System.Guid ParseExact(string input, string format) { return default(System.Guid); }
        int System.IComparable.CompareTo(object value) { return default(int); }
        string System.IFormattable.ToString(string format, System.IFormatProvider provider) { return default(string); }
        public byte[] ToByteArray() { return default(byte[]); }
        public override string ToString() { return default(string); }
        public string ToString(string format) { return default(string); }
        public static bool TryParse(string input, out System.Guid result) { result = default(System.Guid); return default(bool); }
        public static bool TryParseExact(string input, string format, out System.Guid result) { result = default(System.Guid); return default(bool); }
    }
    public partial interface IAsyncResult
    {
        object AsyncState { get; }
        System.Threading.WaitHandle AsyncWaitHandle { get; }
        bool CompletedSynchronously { get; }
        bool IsCompleted { get; }
    }
    public partial interface IComparable
    {
        int CompareTo(object obj);
    }
    public partial interface IComparable<in T>
    {
        int CompareTo(T other);
    }
    [System.CLSCompliantAttribute(false)]
    public partial interface IConvertible
    {
        System.TypeCode GetTypeCode();
        bool ToBoolean(System.IFormatProvider provider);
        byte ToByte(System.IFormatProvider provider);
        char ToChar(System.IFormatProvider provider);
        System.DateTime ToDateTime(System.IFormatProvider provider);
        decimal ToDecimal(System.IFormatProvider provider);
        double ToDouble(System.IFormatProvider provider);
        short ToInt16(System.IFormatProvider provider);
        int ToInt32(System.IFormatProvider provider);
        long ToInt64(System.IFormatProvider provider);
        sbyte ToSByte(System.IFormatProvider provider);
        float ToSingle(System.IFormatProvider provider);
        string ToString(System.IFormatProvider provider);
        object ToType(System.Type conversionType, System.IFormatProvider provider);
        ushort ToUInt16(System.IFormatProvider provider);
        uint ToUInt32(System.IFormatProvider provider);
        ulong ToUInt64(System.IFormatProvider provider);
    }
    public partial interface ICustomFormatter
    {
        string Format(string format, object arg, System.IFormatProvider formatProvider);
    }
    public partial interface IDisposable
    {
        void Dispose();
    }
    public partial interface IEquatable<T>
    {
        bool Equals(T other);
    }
    public partial interface IFormatProvider
    {
        object GetFormat(System.Type formatType);
    }
    public partial interface IFormattable
    {
        string ToString(string format, System.IFormatProvider formatProvider);
    }
    public sealed partial class IndexOutOfRangeException : System.Exception
    {
        public IndexOutOfRangeException() { }
        public IndexOutOfRangeException(string message) { }
        public IndexOutOfRangeException(string message, System.Exception innerException) { }
    }
    public sealed partial class InsufficientExecutionStackException : System.Exception
    {
        public InsufficientExecutionStackException() { }
        public InsufficientExecutionStackException(string message) { }
        public InsufficientExecutionStackException(string message, System.Exception innerException) { }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct Int16 : System.IComparable, System.IComparable<short>, System.IConvertible, System.IEquatable<short>, System.IFormattable
    {
        public const short MaxValue = (short)32767;
        public const short MinValue = (short)-32768;
        public int CompareTo(short value) { return default(int); }
        public bool Equals(short obj) { return default(bool); }
        public override bool Equals(object obj) { return default(bool); }
        public override int GetHashCode() { return default(int); }
        public static short Parse(string s) { return default(short); }
        public static short Parse(string s, System.Globalization.NumberStyles style) { return default(short); }
        public static short Parse(string s, System.Globalization.NumberStyles style, System.IFormatProvider provider) { return default(short); }
        public static short Parse(string s, System.IFormatProvider provider) { return default(short); }
        int System.IComparable.CompareTo(object value) { return default(int); }
        System.TypeCode System.IConvertible.GetTypeCode() { return default(System.TypeCode); }
        bool System.IConvertible.ToBoolean(System.IFormatProvider provider) { return default(bool); }
        byte System.IConvertible.ToByte(System.IFormatProvider provider) { return default(byte); }
        char System.IConvertible.ToChar(System.IFormatProvider provider) { return default(char); }
        System.DateTime System.IConvertible.ToDateTime(System.IFormatProvider provider) { return default(System.DateTime); }
        decimal System.IConvertible.ToDecimal(System.IFormatProvider provider) { return default(decimal); }
        double System.IConvertible.ToDouble(System.IFormatProvider provider) { return default(double); }
        short System.IConvertible.ToInt16(System.IFormatProvider provider) { return default(short); }
        int System.IConvertible.ToInt32(System.IFormatProvider provider) { return default(int); }
        long System.IConvertible.ToInt64(System.IFormatProvider provider) { return default(long); }
        sbyte System.IConvertible.ToSByte(System.IFormatProvider provider) { return default(sbyte); }
        float System.IConvertible.ToSingle(System.IFormatProvider provider) { return default(float); }
        object System.IConvertible.ToType(System.Type type, System.IFormatProvider provider) { return default(object); }
        ushort System.IConvertible.ToUInt16(System.IFormatProvider provider) { return default(ushort); }
        uint System.IConvertible.ToUInt32(System.IFormatProvider provider) { return default(uint); }
        ulong System.IConvertible.ToUInt64(System.IFormatProvider provider) { return default(ulong); }
        public override string ToString() { return default(string); }
        public string ToString(System.IFormatProvider provider) { return default(string); }
        public string ToString(string format) { return default(string); }
        public string ToString(string format, System.IFormatProvider provider) { return default(string); }
        public static bool TryParse(string s, System.Globalization.NumberStyles style, System.IFormatProvider provider, out short result) { result = default(short); return default(bool); }
        public static bool TryParse(string s, out short result) { result = default(short); return default(bool); }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct Int32 : System.IComparable, System.IComparable<int>, System.IConvertible, System.IEquatable<int>, System.IFormattable
    {
        public const int MaxValue = 2147483647;
        public const int MinValue = -2147483648;
        public int CompareTo(int value) { return default(int); }
        public bool Equals(int obj) { return default(bool); }
        public override bool Equals(object obj) { return default(bool); }
        public override int GetHashCode() { return default(int); }
        public static int Parse(string s) { return default(int); }
        public static int Parse(string s, System.Globalization.NumberStyles style) { return default(int); }
        public static int Parse(string s, System.Globalization.NumberStyles style, System.IFormatProvider provider) { return default(int); }
        public static int Parse(string s, System.IFormatProvider provider) { return default(int); }
        int System.IComparable.CompareTo(object value) { return default(int); }
        System.TypeCode System.IConvertible.GetTypeCode() { return default(System.TypeCode); }
        bool System.IConvertible.ToBoolean(System.IFormatProvider provider) { return default(bool); }
        byte System.IConvertible.ToByte(System.IFormatProvider provider) { return default(byte); }
        char System.IConvertible.ToChar(System.IFormatProvider provider) { return default(char); }
        System.DateTime System.IConvertible.ToDateTime(System.IFormatProvider provider) { return default(System.DateTime); }
        decimal System.IConvertible.ToDecimal(System.IFormatProvider provider) { return default(decimal); }
        double System.IConvertible.ToDouble(System.IFormatProvider provider) { return default(double); }
        short System.IConvertible.ToInt16(System.IFormatProvider provider) { return default(short); }
        int System.IConvertible.ToInt32(System.IFormatProvider provider) { return default(int); }
        long System.IConvertible.ToInt64(System.IFormatProvider provider) { return default(long); }
        sbyte System.IConvertible.ToSByte(System.IFormatProvider provider) { return default(sbyte); }
        float System.IConvertible.ToSingle(System.IFormatProvider provider) { return default(float); }
        object System.IConvertible.ToType(System.Type type, System.IFormatProvider provider) { return default(object); }
        ushort System.IConvertible.ToUInt16(System.IFormatProvider provider) { return default(ushort); }
        uint System.IConvertible.ToUInt32(System.IFormatProvider provider) { return default(uint); }
        ulong System.IConvertible.ToUInt64(System.IFormatProvider provider) { return default(ulong); }
        public override string ToString() { return default(string); }
        public string ToString(System.IFormatProvider provider) { return default(string); }
        public string ToString(string format) { return default(string); }
        public string ToString(string format, System.IFormatProvider provider) { return default(string); }
        public static bool TryParse(string s, System.Globalization.NumberStyles style, System.IFormatProvider provider, out int result) { result = default(int); return default(bool); }
        public static bool TryParse(string s, out int result) { result = default(int); return default(bool); }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct Int64 : System.IComparable, System.IComparable<long>, System.IConvertible, System.IEquatable<long>, System.IFormattable
    {
        public const long MaxValue = (long)9223372036854775807;
        public const long MinValue = (long)-9223372036854775808;
        public int CompareTo(long value) { return default(int); }
        public bool Equals(long obj) { return default(bool); }
        public override bool Equals(object obj) { return default(bool); }
        public override int GetHashCode() { return default(int); }
        public static long Parse(string s) { return default(long); }
        public static long Parse(string s, System.Globalization.NumberStyles style) { return default(long); }
        public static long Parse(string s, System.Globalization.NumberStyles style, System.IFormatProvider provider) { return default(long); }
        public static long Parse(string s, System.IFormatProvider provider) { return default(long); }
        int System.IComparable.CompareTo(object value) { return default(int); }
        System.TypeCode System.IConvertible.GetTypeCode() { return default(System.TypeCode); }
        bool System.IConvertible.ToBoolean(System.IFormatProvider provider) { return default(bool); }
        byte System.IConvertible.ToByte(System.IFormatProvider provider) { return default(byte); }
        char System.IConvertible.ToChar(System.IFormatProvider provider) { return default(char); }
        System.DateTime System.IConvertible.ToDateTime(System.IFormatProvider provider) { return default(System.DateTime); }
        decimal System.IConvertible.ToDecimal(System.IFormatProvider provider) { return default(decimal); }
        double System.IConvertible.ToDouble(System.IFormatProvider provider) { return default(double); }
        short System.IConvertible.ToInt16(System.IFormatProvider provider) { return default(short); }
        int System.IConvertible.ToInt32(System.IFormatProvider provider) { return default(int); }
        long System.IConvertible.ToInt64(System.IFormatProvider provider) { return default(long); }
        sbyte System.IConvertible.ToSByte(System.IFormatProvider provider) { return default(sbyte); }
        float System.IConvertible.ToSingle(System.IFormatProvider provider) { return default(float); }
        object System.IConvertible.ToType(System.Type type, System.IFormatProvider provider) { return default(object); }
        ushort System.IConvertible.ToUInt16(System.IFormatProvider provider) { return default(ushort); }
        uint System.IConvertible.ToUInt32(System.IFormatProvider provider) { return default(uint); }
        ulong System.IConvertible.ToUInt64(System.IFormatProvider provider) { return default(ulong); }
        public override string ToString() { return default(string); }
        public string ToString(System.IFormatProvider provider) { return default(string); }
        public string ToString(string format) { return default(string); }
        public string ToString(string format, System.IFormatProvider provider) { return default(string); }
        public static bool TryParse(string s, System.Globalization.NumberStyles style, System.IFormatProvider provider, out long result) { result = default(long); return default(bool); }
        public static bool TryParse(string s, out long result) { result = default(long); return default(bool); }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct IntPtr
    {
        public static readonly System.IntPtr Zero;
        public IntPtr(int value) { throw new System.NotImplementedException(); }
        public IntPtr(long value) { throw new System.NotImplementedException(); }
        [System.CLSCompliantAttribute(false)]
        [System.Security.SecurityCriticalAttribute]
        public unsafe IntPtr(void* value) { throw new System.NotImplementedException(); }
        public static int Size { get { return default(int); } }
        public static System.IntPtr Add(System.IntPtr pointer, int offset) { return default(System.IntPtr); }
        public override bool Equals(object obj) { return default(bool); }
        public override int GetHashCode() { return default(int); }
        public static System.IntPtr operator +(System.IntPtr pointer, int offset) { return default(System.IntPtr); }
        public static bool operator ==(System.IntPtr value1, System.IntPtr value2) { return default(bool); }
        public static explicit operator System.IntPtr(int value) { return default(System.IntPtr); }
        public static explicit operator System.IntPtr(long value) { return default(System.IntPtr); }
        public static explicit operator int (System.IntPtr value) { return default(int); }
        public static explicit operator long (System.IntPtr value) { return default(long); }
        [System.CLSCompliantAttribute(false)]
        public unsafe static explicit operator void* (System.IntPtr value) { return default(void*); }
        [System.CLSCompliantAttribute(false)]
        [System.Security.SecurityCriticalAttribute]
        public unsafe static explicit operator System.IntPtr(void* value) { return default(System.IntPtr); }
        public static bool operator !=(System.IntPtr value1, System.IntPtr value2) { return default(bool); }
        public static System.IntPtr operator -(System.IntPtr pointer, int offset) { return default(System.IntPtr); }
        public static System.IntPtr Subtract(System.IntPtr pointer, int offset) { return default(System.IntPtr); }
        public int ToInt32() { return default(int); }
        public long ToInt64() { return default(long); }
        [System.CLSCompliantAttribute(false)]
        public unsafe void* ToPointer() { return default(void*); }
        public override string ToString() { return default(string); }
        public string ToString(string format) { return default(string); }
    }
    public partial class InvalidCastException : System.Exception
    {
        public InvalidCastException() { }
        public InvalidCastException(string message) { }
        public InvalidCastException(string message, System.Exception innerException) { }
        public InvalidCastException(string message, int errorCode) { }
    }
    public partial class InvalidOperationException : System.Exception
    {
        public InvalidOperationException() { }
        public InvalidOperationException(string message) { }
        public InvalidOperationException(string message, System.Exception innerException) { }
    }
    public sealed partial class InvalidProgramException : System.Exception
    {
        public InvalidProgramException() { }
        public InvalidProgramException(string message) { }
        public InvalidProgramException(string message, System.Exception inner) { }
    }
    public partial class InvalidTimeZoneException : System.Exception
    {
        public InvalidTimeZoneException() { }
        public InvalidTimeZoneException(string message) { }
        public InvalidTimeZoneException(string message, System.Exception innerException) { }
    }
    public partial interface IObservable<out T>
    {
        System.IDisposable Subscribe(System.IObserver<T> observer);
    }
    public partial interface IObserver<in T>
    {
        void OnCompleted();
        void OnError(System.Exception error);
        void OnNext(T value);
    }
    public partial interface IProgress<in T>
    {
        void Report(T value);
    }
    public partial class Lazy<T>
    {
        public Lazy() { }
        public Lazy(bool isThreadSafe) { }
        public Lazy(System.Func<T> valueFactory) { }
        public Lazy(System.Func<T> valueFactory, bool isThreadSafe) { }
        public Lazy(System.Func<T> valueFactory, System.Threading.LazyThreadSafetyMode mode) { }
        public Lazy(System.Threading.LazyThreadSafetyMode mode) { }
        public bool IsValueCreated { get { return default(bool); } }
        public T Value { get { return default(T); } }
        public override string ToString() { return default(string); }
    }
    public partial class Lazy<T, TMetadata> : System.Lazy<T>
    {
        public Lazy(TMetadata metadata) { }
        public Lazy(TMetadata metadata, bool isThreadSafe) { }
        public Lazy(TMetadata metadata, System.Threading.LazyThreadSafetyMode mode) { }
        public Lazy(System.Func<T> valueFactory, TMetadata metadata) { }
        public Lazy(System.Func<T> valueFactory, TMetadata metadata, bool isThreadSafe) { }
        public Lazy(System.Func<T> valueFactory, TMetadata metadata, System.Threading.LazyThreadSafetyMode mode) { }
        public TMetadata Metadata { get { return default(TMetadata); } }
    }
    public partial class MemberAccessException : System.Exception
    {
        public MemberAccessException() { }
        public MemberAccessException(string message) { }
        public MemberAccessException(string message, System.Exception inner) { }
    }
    public partial class MethodAccessException : System.MemberAccessException
    {
        public MethodAccessException() { }
        public MethodAccessException(string message) { }
        public MethodAccessException(string message, System.Exception inner) { }
    }
    public partial class MissingFieldException : System.MissingMemberException
    {
        public MissingFieldException() { }
        public MissingFieldException(string message) { }
        public MissingFieldException(string message, System.Exception inner) { }
        public override string Message { get { return default(string); } }
    }
    public partial class MissingMemberException : System.MemberAccessException
    {
        public MissingMemberException() { }
        public MissingMemberException(string message) { }
        public MissingMemberException(string message, System.Exception inner) { }
        public override string Message { get { return default(string); } }
    }
    public partial class MissingMethodException : System.MissingMemberException
    {
        public MissingMethodException() { }
        public MissingMethodException(string message) { }
        public MissingMethodException(string message, System.Exception inner) { }
        public override string Message { get { return default(string); } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(64))]
    public sealed partial class MTAThreadAttribute : System.Attribute
    {
        public MTAThreadAttribute() { }
    }
    public abstract partial class MulticastDelegate : System.Delegate
    {
        internal MulticastDelegate() { }
        public sealed override bool Equals(object obj) { return default(bool); }
        public sealed override int GetHashCode() { return default(int); }
        public sealed override System.Delegate[] GetInvocationList() { return default(System.Delegate[]); }
        public static bool operator ==(System.MulticastDelegate d1, System.MulticastDelegate d2) { return default(bool); }
        public static bool operator !=(System.MulticastDelegate d1, System.MulticastDelegate d2) { return default(bool); }
    }
    public partial class NotImplementedException : System.Exception
    {
        public NotImplementedException() { }
        public NotImplementedException(string message) { }
        public NotImplementedException(string message, System.Exception inner) { }
    }
    public partial class NotSupportedException : System.Exception
    {
        public NotSupportedException() { }
        public NotSupportedException(string message) { }
        public NotSupportedException(string message, System.Exception innerException) { }
    }
    public static partial class Nullable
    {
        public static int Compare<T>(System.Nullable<T> n1, System.Nullable<T> n2) where T : struct { return default(int); }
        public static bool Equals<T>(System.Nullable<T> n1, System.Nullable<T> n2) where T : struct { return default(bool); }
        public static System.Type GetUnderlyingType(System.Type nullableType) { return default(System.Type); }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct Nullable<T> where T : struct
    {
        public Nullable(T value) { throw new System.NotImplementedException(); }
        public bool HasValue { get { return default(bool); } }
        public T Value { get { return default(T); } }
        public override bool Equals(object other) { return default(bool); }
        public override int GetHashCode() { return default(int); }
        public T GetValueOrDefault() { return default(T); }
        public T GetValueOrDefault(T defaultValue) { return default(T); }
        public static explicit operator T(System.Nullable<T> value) { return default(T); }
        public static implicit operator System.Nullable<T>(T value) { return default(System.Nullable<T>); }
        public override string ToString() { return default(string); }
    }
    public partial class NullReferenceException : System.Exception
    {
        public NullReferenceException() { }
        public NullReferenceException(string message) { }
        public NullReferenceException(string message, System.Exception innerException) { }
    }
    public partial class Object
    {
        public Object() { }
        public virtual bool Equals(object obj) { return default(bool); }
        public static bool Equals(object objA, object objB) { return default(bool); }
        ~Object() { }
        public virtual int GetHashCode() { return default(int); }
        public System.Type GetType() { return default(System.Type); }
        protected object MemberwiseClone() { return default(object); }
        public static bool ReferenceEquals(object objA, object objB) { return default(bool); }
        public virtual string ToString() { return default(string); }
    }
    public partial class ObjectDisposedException : System.InvalidOperationException
    {
        public ObjectDisposedException(string objectName) { }
        public ObjectDisposedException(string message, System.Exception innerException) { }
        public ObjectDisposedException(string objectName, string message) { }
        public override string Message { get { return default(string); } }
        public string ObjectName { get { return default(string); } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(6140), Inherited = false)]
    public sealed partial class ObsoleteAttribute : System.Attribute
    {
        public ObsoleteAttribute() { }
        public ObsoleteAttribute(string message) { }
        public ObsoleteAttribute(string message, bool error) { }
        public bool IsError { get { return default(bool); } }
        public string Message { get { return default(string); } }
    }
    public partial class OutOfMemoryException : System.Exception
    {
        public OutOfMemoryException() { }
        public OutOfMemoryException(string message) { }
        public OutOfMemoryException(string message, System.Exception innerException) { }
    }
    public partial class OverflowException : System.ArithmeticException
    {
        public OverflowException() { }
        public OverflowException(string message) { }
        public OverflowException(string message, System.Exception innerException) { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(2048), Inherited = true, AllowMultiple = false)]
    public sealed partial class ParamArrayAttribute : System.Attribute
    {
        public ParamArrayAttribute() { }
    }
    public partial class PlatformNotSupportedException : System.NotSupportedException
    {
        public PlatformNotSupportedException() { }
        public PlatformNotSupportedException(string message) { }
        public PlatformNotSupportedException(string message, System.Exception inner) { }
    }
    public delegate bool Predicate<in T>(T obj);
    public partial class RankException : System.Exception
    {
        public RankException() { }
        public RankException(string message) { }
        public RankException(string message, System.Exception innerException) { }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct RuntimeFieldHandle
    {
        public override bool Equals(object obj) { return default(bool); }
        public bool Equals(System.RuntimeFieldHandle handle) { return default(bool); }
        public override int GetHashCode() { return default(int); }
        public static bool operator ==(System.RuntimeFieldHandle left, System.RuntimeFieldHandle right) { return default(bool); }
        public static bool operator !=(System.RuntimeFieldHandle left, System.RuntimeFieldHandle right) { return default(bool); }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct RuntimeMethodHandle
    {
        public override bool Equals(object obj) { return default(bool); }
        public bool Equals(System.RuntimeMethodHandle handle) { return default(bool); }
        public override int GetHashCode() { return default(int); }
        public static bool operator ==(System.RuntimeMethodHandle left, System.RuntimeMethodHandle right) { return default(bool); }
        public static bool operator !=(System.RuntimeMethodHandle left, System.RuntimeMethodHandle right) { return default(bool); }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct RuntimeTypeHandle
    {
        public override bool Equals(object obj) { return default(bool); }
        public bool Equals(System.RuntimeTypeHandle handle) { return default(bool); }
        public override int GetHashCode() { return default(int); }
        public static bool operator ==(object left, System.RuntimeTypeHandle right) { return default(bool); }
        public static bool operator ==(System.RuntimeTypeHandle left, object right) { return default(bool); }
        public static bool operator !=(object left, System.RuntimeTypeHandle right) { return default(bool); }
        public static bool operator !=(System.RuntimeTypeHandle left, object right) { return default(bool); }
    }
    [System.CLSCompliantAttribute(false)]
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct SByte : System.IComparable, System.IComparable<sbyte>, System.IConvertible, System.IEquatable<sbyte>, System.IFormattable
    {
        public const sbyte MaxValue = (sbyte)127;
        public const sbyte MinValue = (sbyte)-128;
        public int CompareTo(sbyte value) { return default(int); }
        public override bool Equals(object obj) { return default(bool); }
        public bool Equals(sbyte obj) { return default(bool); }
        public override int GetHashCode() { return default(int); }
        [System.CLSCompliantAttribute(false)]
        public static sbyte Parse(string s) { return default(sbyte); }
        [System.CLSCompliantAttribute(false)]
        public static sbyte Parse(string s, System.Globalization.NumberStyles style) { return default(sbyte); }
        [System.CLSCompliantAttribute(false)]
        public static sbyte Parse(string s, System.Globalization.NumberStyles style, System.IFormatProvider provider) { return default(sbyte); }
        [System.CLSCompliantAttribute(false)]
        public static sbyte Parse(string s, System.IFormatProvider provider) { return default(sbyte); }
        int System.IComparable.CompareTo(object obj) { return default(int); }
        System.TypeCode System.IConvertible.GetTypeCode() { return default(System.TypeCode); }
        bool System.IConvertible.ToBoolean(System.IFormatProvider provider) { return default(bool); }
        byte System.IConvertible.ToByte(System.IFormatProvider provider) { return default(byte); }
        char System.IConvertible.ToChar(System.IFormatProvider provider) { return default(char); }
        System.DateTime System.IConvertible.ToDateTime(System.IFormatProvider provider) { return default(System.DateTime); }
        decimal System.IConvertible.ToDecimal(System.IFormatProvider provider) { return default(decimal); }
        double System.IConvertible.ToDouble(System.IFormatProvider provider) { return default(double); }
        short System.IConvertible.ToInt16(System.IFormatProvider provider) { return default(short); }
        int System.IConvertible.ToInt32(System.IFormatProvider provider) { return default(int); }
        long System.IConvertible.ToInt64(System.IFormatProvider provider) { return default(long); }
        sbyte System.IConvertible.ToSByte(System.IFormatProvider provider) { return default(sbyte); }
        float System.IConvertible.ToSingle(System.IFormatProvider provider) { return default(float); }
        object System.IConvertible.ToType(System.Type type, System.IFormatProvider provider) { return default(object); }
        ushort System.IConvertible.ToUInt16(System.IFormatProvider provider) { return default(ushort); }
        uint System.IConvertible.ToUInt32(System.IFormatProvider provider) { return default(uint); }
        ulong System.IConvertible.ToUInt64(System.IFormatProvider provider) { return default(ulong); }
        public override string ToString() { return default(string); }
        public string ToString(System.IFormatProvider provider) { return default(string); }
        public string ToString(string format) { return default(string); }
        public string ToString(string format, System.IFormatProvider provider) { return default(string); }
        [System.CLSCompliantAttribute(false)]
        public static bool TryParse(string s, System.Globalization.NumberStyles style, System.IFormatProvider provider, out sbyte result) { result = default(sbyte); return default(bool); }
        [System.CLSCompliantAttribute(false)]
        public static bool TryParse(string s, out sbyte result) { result = default(sbyte); return default(bool); }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct Single : System.IComparable, System.IComparable<float>, System.IConvertible, System.IEquatable<float>, System.IFormattable
    {
        public int CompareTo(float value) { return default(int); }
        public override bool Equals(object obj) { return default(bool); }
        public bool Equals(float obj) { return default(bool); }
        public override int GetHashCode() { return default(int); }
        public static bool IsInfinity(float f) { return default(bool); }
        public static bool IsNaN(float f) { return default(bool); }
        public static bool IsNegativeInfinity(float f) { return default(bool); }
        public static bool IsPositiveInfinity(float f) { return default(bool); }
        public static bool operator ==(float left, float right) { return default(bool); }
        public static bool operator >(float left, float right) { return default(bool); }
        public static bool operator >=(float left, float right) { return default(bool); }
        public static bool operator !=(float left, float right) { return default(bool); }
        public static bool operator <(float left, float right) { return default(bool); }
        public static bool operator <=(float left, float right) { return default(bool); }
        public static float Parse(string s) { return default(float); }
        public static float Parse(string s, System.Globalization.NumberStyles style) { return default(float); }
        public static float Parse(string s, System.Globalization.NumberStyles style, System.IFormatProvider provider) { return default(float); }
        public static float Parse(string s, System.IFormatProvider provider) { return default(float); }
        int System.IComparable.CompareTo(object value) { return default(int); }
        System.TypeCode System.IConvertible.GetTypeCode() { return default(System.TypeCode); }
        bool System.IConvertible.ToBoolean(System.IFormatProvider provider) { return default(bool); }
        byte System.IConvertible.ToByte(System.IFormatProvider provider) { return default(byte); }
        char System.IConvertible.ToChar(System.IFormatProvider provider) { return default(char); }
        System.DateTime System.IConvertible.ToDateTime(System.IFormatProvider provider) { return default(System.DateTime); }
        decimal System.IConvertible.ToDecimal(System.IFormatProvider provider) { return default(decimal); }
        double System.IConvertible.ToDouble(System.IFormatProvider provider) { return default(double); }
        short System.IConvertible.ToInt16(System.IFormatProvider provider) { return default(short); }
        int System.IConvertible.ToInt32(System.IFormatProvider provider) { return default(int); }
        long System.IConvertible.ToInt64(System.IFormatProvider provider) { return default(long); }
        sbyte System.IConvertible.ToSByte(System.IFormatProvider provider) { return default(sbyte); }
        float System.IConvertible.ToSingle(System.IFormatProvider provider) { return default(float); }
        object System.IConvertible.ToType(System.Type type, System.IFormatProvider provider) { return default(object); }
        ushort System.IConvertible.ToUInt16(System.IFormatProvider provider) { return default(ushort); }
        uint System.IConvertible.ToUInt32(System.IFormatProvider provider) { return default(uint); }
        ulong System.IConvertible.ToUInt64(System.IFormatProvider provider) { return default(ulong); }
        public override string ToString() { return default(string); }
        public string ToString(System.IFormatProvider provider) { return default(string); }
        public string ToString(string format) { return default(string); }
        public string ToString(string format, System.IFormatProvider provider) { return default(string); }
        public static bool TryParse(string s, System.Globalization.NumberStyles style, System.IFormatProvider provider, out float result) { result = default(float); return default(bool); }
        public static bool TryParse(string s, out float result) { result = default(float); return default(bool); }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(64))]
    public sealed partial class STAThreadAttribute : System.Attribute
    {
        public STAThreadAttribute() { }
    }
    public sealed partial class String : System.Collections.Generic.IEnumerable<char>, System.Collections.IEnumerable, System.IComparable, System.IComparable<string>, System.IConvertible, System.IEquatable<string>
    {
        public static readonly string Empty;
        [System.CLSCompliantAttribute(false)]
        [System.Security.SecurityCriticalAttribute]
        public unsafe String(char* value) { }
        [System.CLSCompliantAttribute(false)]
        [System.Security.SecurityCriticalAttribute]
        public unsafe String(char* value, int startIndex, int length) { }
        public String(char c, int count) { }
        public String(char[] value) { }
        public String(char[] value, int startIndex, int length) { }
        [System.Runtime.CompilerServices.IndexerName("Chars")]
        public char this[int index] { get { return default(char); } }
        public int Length { get { return default(int); } }
        public static int Compare(string strA, int indexA, string strB, int indexB, int length) { return default(int); }
        public static int Compare(string strA, int indexA, string strB, int indexB, int length, System.StringComparison comparisonType) { return default(int); }
        public static int Compare(string strA, string strB) { return default(int); }
        public static int Compare(string strA, string strB, bool ignoreCase) { return default(int); }
        public static int Compare(string strA, string strB, System.StringComparison comparisonType) { return default(int); }
        public static int CompareOrdinal(string strA, int indexA, string strB, int indexB, int length) { return default(int); }
        public static int CompareOrdinal(string strA, string strB) { return default(int); }
        public int CompareTo(string strB) { return default(int); }
        public static string Concat(System.Collections.Generic.IEnumerable<string> values) { return default(string); }
        public static string Concat(object arg0) { return default(string); }
        public static string Concat(object arg0, object arg1) { return default(string); }
        public static string Concat(object arg0, object arg1, object arg2) { return default(string); }
        public static string Concat(params object[] args) { return default(string); }
        public static string Concat(string str0, string str1) { return default(string); }
        public static string Concat(string str0, string str1, string str2) { return default(string); }
        public static string Concat(string str0, string str1, string str2, string str3) { return default(string); }
        public static string Concat(params string[] values) { return default(string); }
        public static string Concat<T>(System.Collections.Generic.IEnumerable<T> values) { return default(string); }
        public bool Contains(string value) { return default(bool); }
        public void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count) { }
        public bool EndsWith(string value) { return default(bool); }
        public bool EndsWith(string value, System.StringComparison comparisonType) { return default(bool); }
        public override bool Equals(object obj) { return default(bool); }
        public bool Equals(string value) { return default(bool); }
        public static bool Equals(string a, string b) { return default(bool); }
        public static bool Equals(string a, string b, System.StringComparison comparisonType) { return default(bool); }
        public bool Equals(string value, System.StringComparison comparisonType) { return default(bool); }
        public static string Format(System.IFormatProvider provider, string format, object arg0) { return default(string); }
        public static string Format(System.IFormatProvider provider, string format, object arg0, object arg1) { return default(string); }
        public static string Format(System.IFormatProvider provider, string format, object arg0, object arg1, object arg2) { return default(string); }
        public static string Format(System.IFormatProvider provider, string format, params object[] args) { return default(string); }
        public static string Format(string format, object arg0) { return default(string); }
        public static string Format(string format, object arg0, object arg1) { return default(string); }
        public static string Format(string format, object arg0, object arg1, object arg2) { return default(string); }
        public static string Format(string format, params object[] args) { return default(string); }
        public override int GetHashCode() { return default(int); }
        public int IndexOf(char value) { return default(int); }
        public int IndexOf(char value, int startIndex) { return default(int); }
        public int IndexOf(char value, int startIndex, int count) { return default(int); }
        public int IndexOf(string value) { return default(int); }
        public int IndexOf(string value, int startIndex) { return default(int); }
        public int IndexOf(string value, int startIndex, int count) { return default(int); }
        public int IndexOf(string value, int startIndex, int count, System.StringComparison comparisonType) { return default(int); }
        public int IndexOf(string value, int startIndex, System.StringComparison comparisonType) { return default(int); }
        public int IndexOf(string value, System.StringComparison comparisonType) { return default(int); }
        public int IndexOfAny(char[] anyOf) { return default(int); }
        public int IndexOfAny(char[] anyOf, int startIndex) { return default(int); }
        public int IndexOfAny(char[] anyOf, int startIndex, int count) { return default(int); }
        public string Insert(int startIndex, string value) { return default(string); }
        public static bool IsNullOrEmpty(string value) { return default(bool); }
        public static bool IsNullOrWhiteSpace(string value) { return default(bool); }
        public static string Join(string separator, System.Collections.Generic.IEnumerable<string> values) { return default(string); }
        public static string Join(string separator, params object[] values) { return default(string); }
        public static string Join(string separator, params string[] value) { return default(string); }
        public static string Join(string separator, string[] value, int startIndex, int count) { return default(string); }
        public static string Join<T>(string separator, System.Collections.Generic.IEnumerable<T> values) { return default(string); }
        public int LastIndexOf(char value) { return default(int); }
        public int LastIndexOf(char value, int startIndex) { return default(int); }
        public int LastIndexOf(char value, int startIndex, int count) { return default(int); }
        public int LastIndexOf(string value) { return default(int); }
        public int LastIndexOf(string value, int startIndex) { return default(int); }
        public int LastIndexOf(string value, int startIndex, int count) { return default(int); }
        public int LastIndexOf(string value, int startIndex, int count, System.StringComparison comparisonType) { return default(int); }
        public int LastIndexOf(string value, int startIndex, System.StringComparison comparisonType) { return default(int); }
        public int LastIndexOf(string value, System.StringComparison comparisonType) { return default(int); }
        public int LastIndexOfAny(char[] anyOf) { return default(int); }
        public int LastIndexOfAny(char[] anyOf, int startIndex) { return default(int); }
        public int LastIndexOfAny(char[] anyOf, int startIndex, int count) { return default(int); }
        public static bool operator ==(string a, string b) { return default(bool); }
        public static bool operator !=(string a, string b) { return default(bool); }
        public string PadLeft(int totalWidth) { return default(string); }
        public string PadLeft(int totalWidth, char paddingChar) { return default(string); }
        public string PadRight(int totalWidth) { return default(string); }
        public string PadRight(int totalWidth, char paddingChar) { return default(string); }
        public string Remove(int startIndex) { return default(string); }
        public string Remove(int startIndex, int count) { return default(string); }
        public string Replace(char oldChar, char newChar) { return default(string); }
        public string Replace(string oldValue, string newValue) { return default(string); }
        public string[] Split(params char[] separator) { return default(string[]); }
        public string[] Split(char[] separator, int count) { return default(string[]); }
        public string[] Split(char[] separator, int count, System.StringSplitOptions options) { return default(string[]); }
        public string[] Split(char[] separator, System.StringSplitOptions options) { return default(string[]); }
        public string[] Split(string[] separator, int count, System.StringSplitOptions options) { return default(string[]); }
        public string[] Split(string[] separator, System.StringSplitOptions options) { return default(string[]); }
        public bool StartsWith(string value) { return default(bool); }
        public bool StartsWith(string value, System.StringComparison comparisonType) { return default(bool); }
        public string Substring(int startIndex) { return default(string); }
        public string Substring(int startIndex, int length) { return default(string); }
        System.Collections.Generic.IEnumerator<char> System.Collections.Generic.IEnumerable<System.Char>.GetEnumerator() { return default(System.Collections.Generic.IEnumerator<char>); }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return default(System.Collections.IEnumerator); }
        int System.IComparable.CompareTo(object value) { return default(int); }
        System.TypeCode System.IConvertible.GetTypeCode() { return default(System.TypeCode); }
        bool System.IConvertible.ToBoolean(System.IFormatProvider provider) { return default(bool); }
        byte System.IConvertible.ToByte(System.IFormatProvider provider) { return default(byte); }
        char System.IConvertible.ToChar(System.IFormatProvider provider) { return default(char); }
        System.DateTime System.IConvertible.ToDateTime(System.IFormatProvider provider) { return default(System.DateTime); }
        decimal System.IConvertible.ToDecimal(System.IFormatProvider provider) { return default(decimal); }
        double System.IConvertible.ToDouble(System.IFormatProvider provider) { return default(double); }
        short System.IConvertible.ToInt16(System.IFormatProvider provider) { return default(short); }
        int System.IConvertible.ToInt32(System.IFormatProvider provider) { return default(int); }
        long System.IConvertible.ToInt64(System.IFormatProvider provider) { return default(long); }
        sbyte System.IConvertible.ToSByte(System.IFormatProvider provider) { return default(sbyte); }
        float System.IConvertible.ToSingle(System.IFormatProvider provider) { return default(float); }
        string System.IConvertible.ToString(System.IFormatProvider provider) { return default(string); }
        object System.IConvertible.ToType(System.Type type, System.IFormatProvider provider) { return default(object); }
        ushort System.IConvertible.ToUInt16(System.IFormatProvider provider) { return default(ushort); }
        uint System.IConvertible.ToUInt32(System.IFormatProvider provider) { return default(uint); }
        ulong System.IConvertible.ToUInt64(System.IFormatProvider provider) { return default(ulong); }
        public char[] ToCharArray() { return default(char[]); }
        public char[] ToCharArray(int startIndex, int length) { return default(char[]); }
        public string ToLower() { return default(string); }
        public string ToLowerInvariant() { return default(string); }
        public override string ToString() { return default(string); }
        public string ToUpper() { return default(string); }
        public string ToUpperInvariant() { return default(string); }
        public string Trim() { return default(string); }
        public string Trim(params char[] trimChars) { return default(string); }
        public string TrimEnd(params char[] trimChars) { return default(string); }
        public string TrimStart(params char[] trimChars) { return default(string); }
    }
    public enum StringComparison
    {
        CurrentCulture = 0,
        CurrentCultureIgnoreCase = 1,
        Ordinal = 4,
        OrdinalIgnoreCase = 5,
    }
    [System.FlagsAttribute]
    public enum StringSplitOptions
    {
        None = 0,
        RemoveEmptyEntries = 1,
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(256), Inherited = false)]
    public partial class ThreadStaticAttribute : System.Attribute
    {
        public ThreadStaticAttribute() { }
    }
    public partial class TimeoutException : System.Exception
    {
        public TimeoutException() { }
        public TimeoutException(string message) { }
        public TimeoutException(string message, System.Exception innerException) { }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct TimeSpan : System.IComparable, System.IComparable<System.TimeSpan>, System.IEquatable<System.TimeSpan>, System.IFormattable
    {
        public static readonly System.TimeSpan MaxValue;
        public static readonly System.TimeSpan MinValue;
        public const long TicksPerDay = (long)864000000000;
        public const long TicksPerHour = (long)36000000000;
        public const long TicksPerMillisecond = (long)10000;
        public const long TicksPerMinute = (long)600000000;
        public const long TicksPerSecond = (long)10000000;
        public static readonly System.TimeSpan Zero;
        public TimeSpan(int hours, int minutes, int seconds) { throw new System.NotImplementedException(); }
        public TimeSpan(int days, int hours, int minutes, int seconds) { throw new System.NotImplementedException(); }
        public TimeSpan(int days, int hours, int minutes, int seconds, int milliseconds) { throw new System.NotImplementedException(); }
        public TimeSpan(long ticks) { throw new System.NotImplementedException(); }
        public int Days { get { return default(int); } }
        public int Hours { get { return default(int); } }
        public int Milliseconds { get { return default(int); } }
        public int Minutes { get { return default(int); } }
        public int Seconds { get { return default(int); } }
        public long Ticks { get { return default(long); } }
        public double TotalDays { get { return default(double); } }
        public double TotalHours { get { return default(double); } }
        public double TotalMilliseconds { get { return default(double); } }
        public double TotalMinutes { get { return default(double); } }
        public double TotalSeconds { get { return default(double); } }
        public System.TimeSpan Add(System.TimeSpan ts) { return default(System.TimeSpan); }
        public static int Compare(System.TimeSpan t1, System.TimeSpan t2) { return default(int); }
        public int CompareTo(System.TimeSpan value) { return default(int); }
        public System.TimeSpan Duration() { return default(System.TimeSpan); }
        public override bool Equals(object value) { return default(bool); }
        public bool Equals(System.TimeSpan obj) { return default(bool); }
        public static bool Equals(System.TimeSpan t1, System.TimeSpan t2) { return default(bool); }
        public static System.TimeSpan FromDays(double value) { return default(System.TimeSpan); }
        public static System.TimeSpan FromHours(double value) { return default(System.TimeSpan); }
        public static System.TimeSpan FromMilliseconds(double value) { return default(System.TimeSpan); }
        public static System.TimeSpan FromMinutes(double value) { return default(System.TimeSpan); }
        public static System.TimeSpan FromSeconds(double value) { return default(System.TimeSpan); }
        public static System.TimeSpan FromTicks(long value) { return default(System.TimeSpan); }
        public override int GetHashCode() { return default(int); }
        public System.TimeSpan Negate() { return default(System.TimeSpan); }
        public static System.TimeSpan operator +(System.TimeSpan t1, System.TimeSpan t2) { return default(System.TimeSpan); }
        public static bool operator ==(System.TimeSpan t1, System.TimeSpan t2) { return default(bool); }
        public static bool operator >(System.TimeSpan t1, System.TimeSpan t2) { return default(bool); }
        public static bool operator >=(System.TimeSpan t1, System.TimeSpan t2) { return default(bool); }
        public static bool operator !=(System.TimeSpan t1, System.TimeSpan t2) { return default(bool); }
        public static bool operator <(System.TimeSpan t1, System.TimeSpan t2) { return default(bool); }
        public static bool operator <=(System.TimeSpan t1, System.TimeSpan t2) { return default(bool); }
        public static System.TimeSpan operator -(System.TimeSpan t1, System.TimeSpan t2) { return default(System.TimeSpan); }
        public static System.TimeSpan operator -(System.TimeSpan t) { return default(System.TimeSpan); }
        public static System.TimeSpan operator +(System.TimeSpan t) { return default(System.TimeSpan); }
        public static System.TimeSpan Parse(string s) { return default(System.TimeSpan); }
        public static System.TimeSpan Parse(string input, System.IFormatProvider formatProvider) { return default(System.TimeSpan); }
        public static System.TimeSpan ParseExact(string input, string format, System.IFormatProvider formatProvider) { return default(System.TimeSpan); }
        public static System.TimeSpan ParseExact(string input, string format, System.IFormatProvider formatProvider, System.Globalization.TimeSpanStyles styles) { return default(System.TimeSpan); }
        public static System.TimeSpan ParseExact(string input, string[] formats, System.IFormatProvider formatProvider) { return default(System.TimeSpan); }
        public static System.TimeSpan ParseExact(string input, string[] formats, System.IFormatProvider formatProvider, System.Globalization.TimeSpanStyles styles) { return default(System.TimeSpan); }
        public System.TimeSpan Subtract(System.TimeSpan ts) { return default(System.TimeSpan); }
        int System.IComparable.CompareTo(object value) { return default(int); }
        public override string ToString() { return default(string); }
        public string ToString(string format) { return default(string); }
        public string ToString(string format, System.IFormatProvider formatProvider) { return default(string); }
        public static bool TryParse(string input, System.IFormatProvider formatProvider, out System.TimeSpan result) { result = default(System.TimeSpan); return default(bool); }
        public static bool TryParse(string s, out System.TimeSpan result) { result = default(System.TimeSpan); return default(bool); }
        public static bool TryParseExact(string input, string format, System.IFormatProvider formatProvider, System.Globalization.TimeSpanStyles styles, out System.TimeSpan result) { result = default(System.TimeSpan); return default(bool); }
        public static bool TryParseExact(string input, string format, System.IFormatProvider formatProvider, out System.TimeSpan result) { result = default(System.TimeSpan); return default(bool); }
        public static bool TryParseExact(string input, string[] formats, System.IFormatProvider formatProvider, System.Globalization.TimeSpanStyles styles, out System.TimeSpan result) { result = default(System.TimeSpan); return default(bool); }
        public static bool TryParseExact(string input, string[] formats, System.IFormatProvider formatProvider, out System.TimeSpan result) { result = default(System.TimeSpan); return default(bool); }
    }
    public sealed partial class TimeZoneInfo : System.IEquatable<System.TimeZoneInfo>
    {
        internal TimeZoneInfo() { }
        public System.TimeSpan BaseUtcOffset { get { return default(System.TimeSpan); } }
        public string DaylightName { get { return default(string); } }
        public string DisplayName { get { return default(string); } }
        public string Id { get { return default(string); } }
        public static System.TimeZoneInfo Local { get { return default(System.TimeZoneInfo); } }
        public string StandardName { get { return default(string); } }
        public bool SupportsDaylightSavingTime { get { return default(bool); } }
        public static System.TimeZoneInfo Utc { get { return default(System.TimeZoneInfo); } }
        public static System.DateTime ConvertTime(System.DateTime dateTime, System.TimeZoneInfo destinationTimeZone) { return default(System.DateTime); }
        public static System.DateTime ConvertTime(System.DateTime dateTime, System.TimeZoneInfo sourceTimeZone, System.TimeZoneInfo destinationTimeZone) { return default(System.DateTime); }
        public static System.DateTimeOffset ConvertTime(System.DateTimeOffset dateTimeOffset, System.TimeZoneInfo destinationTimeZone) { return default(System.DateTimeOffset); }
        public bool Equals(System.TimeZoneInfo other) { return default(bool); }
        public static System.TimeZoneInfo FindSystemTimeZoneById(string id) { return default(System.TimeZoneInfo); }
        public System.TimeSpan[] GetAmbiguousTimeOffsets(System.DateTime dateTime) { return default(System.TimeSpan[]); }
        public System.TimeSpan[] GetAmbiguousTimeOffsets(System.DateTimeOffset dateTimeOffset) { return default(System.TimeSpan[]); }
        public override int GetHashCode() { return default(int); }
        public static System.Collections.ObjectModel.ReadOnlyCollection<System.TimeZoneInfo> GetSystemTimeZones() { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.TimeZoneInfo>); }
        public System.TimeSpan GetUtcOffset(System.DateTime dateTime) { return default(System.TimeSpan); }
        public System.TimeSpan GetUtcOffset(System.DateTimeOffset dateTimeOffset) { return default(System.TimeSpan); }
        public bool IsAmbiguousTime(System.DateTime dateTime) { return default(bool); }
        public bool IsAmbiguousTime(System.DateTimeOffset dateTimeOffset) { return default(bool); }
        public bool IsDaylightSavingTime(System.DateTime dateTime) { return default(bool); }
        public bool IsDaylightSavingTime(System.DateTimeOffset dateTimeOffset) { return default(bool); }
        public bool IsInvalidTime(System.DateTime dateTime) { return default(bool); }
        public override string ToString() { return default(string); }
    }
    public static partial class Tuple
    {
        public static System.Tuple<T1> Create<T1>(T1 item1) { return default(System.Tuple<T1>); }
        public static System.Tuple<T1, T2> Create<T1, T2>(T1 item1, T2 item2) { return default(System.Tuple<T1, T2>); }
        public static System.Tuple<T1, T2, T3> Create<T1, T2, T3>(T1 item1, T2 item2, T3 item3) { return default(System.Tuple<T1, T2, T3>); }
        public static System.Tuple<T1, T2, T3, T4> Create<T1, T2, T3, T4>(T1 item1, T2 item2, T3 item3, T4 item4) { return default(System.Tuple<T1, T2, T3, T4>); }
        public static System.Tuple<T1, T2, T3, T4, T5> Create<T1, T2, T3, T4, T5>(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5) { return default(System.Tuple<T1, T2, T3, T4, T5>); }
        public static System.Tuple<T1, T2, T3, T4, T5, T6> Create<T1, T2, T3, T4, T5, T6>(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6) { return default(System.Tuple<T1, T2, T3, T4, T5, T6>); }
        public static System.Tuple<T1, T2, T3, T4, T5, T6, T7> Create<T1, T2, T3, T4, T5, T6, T7>(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7) { return default(System.Tuple<T1, T2, T3, T4, T5, T6, T7>); }
        public static System.Tuple<T1, T2, T3, T4, T5, T6, T7, System.Tuple<T8>> Create<T1, T2, T3, T4, T5, T6, T7, T8>(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, T8 item8) { return default(System.Tuple<T1, T2, T3, T4, T5, T6, T7, System.Tuple<T8>>); }
    }
    public partial class Tuple<T1> : System.Collections.IStructuralComparable, System.Collections.IStructuralEquatable, System.IComparable
    {
        public Tuple(T1 item1) { }
        public T1 Item1 { get { return default(T1); } }
        public override bool Equals(object obj) { return default(bool); }
        public override int GetHashCode() { return default(int); }
        int System.Collections.IStructuralComparable.CompareTo(object other, System.Collections.IComparer comparer) { return default(int); }
        bool System.Collections.IStructuralEquatable.Equals(object other, System.Collections.IEqualityComparer comparer) { return default(bool); }
        int System.Collections.IStructuralEquatable.GetHashCode(System.Collections.IEqualityComparer comparer) { return default(int); }
        int System.IComparable.CompareTo(object obj) { return default(int); }
        public override string ToString() { return default(string); }
    }
    public partial class Tuple<T1, T2> : System.Collections.IStructuralComparable, System.Collections.IStructuralEquatable, System.IComparable
    {
        public Tuple(T1 item1, T2 item2) { }
        public T1 Item1 { get { return default(T1); } }
        public T2 Item2 { get { return default(T2); } }
        public override bool Equals(object obj) { return default(bool); }
        public override int GetHashCode() { return default(int); }
        int System.Collections.IStructuralComparable.CompareTo(object other, System.Collections.IComparer comparer) { return default(int); }
        bool System.Collections.IStructuralEquatable.Equals(object other, System.Collections.IEqualityComparer comparer) { return default(bool); }
        int System.Collections.IStructuralEquatable.GetHashCode(System.Collections.IEqualityComparer comparer) { return default(int); }
        int System.IComparable.CompareTo(object obj) { return default(int); }
        public override string ToString() { return default(string); }
    }
    public partial class Tuple<T1, T2, T3> : System.Collections.IStructuralComparable, System.Collections.IStructuralEquatable, System.IComparable
    {
        public Tuple(T1 item1, T2 item2, T3 item3) { }
        public T1 Item1 { get { return default(T1); } }
        public T2 Item2 { get { return default(T2); } }
        public T3 Item3 { get { return default(T3); } }
        public override bool Equals(object obj) { return default(bool); }
        public override int GetHashCode() { return default(int); }
        int System.Collections.IStructuralComparable.CompareTo(object other, System.Collections.IComparer comparer) { return default(int); }
        bool System.Collections.IStructuralEquatable.Equals(object other, System.Collections.IEqualityComparer comparer) { return default(bool); }
        int System.Collections.IStructuralEquatable.GetHashCode(System.Collections.IEqualityComparer comparer) { return default(int); }
        int System.IComparable.CompareTo(object obj) { return default(int); }
        public override string ToString() { return default(string); }
    }
    public partial class Tuple<T1, T2, T3, T4> : System.Collections.IStructuralComparable, System.Collections.IStructuralEquatable, System.IComparable
    {
        public Tuple(T1 item1, T2 item2, T3 item3, T4 item4) { }
        public T1 Item1 { get { return default(T1); } }
        public T2 Item2 { get { return default(T2); } }
        public T3 Item3 { get { return default(T3); } }
        public T4 Item4 { get { return default(T4); } }
        public override bool Equals(object obj) { return default(bool); }
        public override int GetHashCode() { return default(int); }
        int System.Collections.IStructuralComparable.CompareTo(object other, System.Collections.IComparer comparer) { return default(int); }
        bool System.Collections.IStructuralEquatable.Equals(object other, System.Collections.IEqualityComparer comparer) { return default(bool); }
        int System.Collections.IStructuralEquatable.GetHashCode(System.Collections.IEqualityComparer comparer) { return default(int); }
        int System.IComparable.CompareTo(object obj) { return default(int); }
        public override string ToString() { return default(string); }
    }
    public partial class Tuple<T1, T2, T3, T4, T5> : System.Collections.IStructuralComparable, System.Collections.IStructuralEquatable, System.IComparable
    {
        public Tuple(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5) { }
        public T1 Item1 { get { return default(T1); } }
        public T2 Item2 { get { return default(T2); } }
        public T3 Item3 { get { return default(T3); } }
        public T4 Item4 { get { return default(T4); } }
        public T5 Item5 { get { return default(T5); } }
        public override bool Equals(object obj) { return default(bool); }
        public override int GetHashCode() { return default(int); }
        int System.Collections.IStructuralComparable.CompareTo(object other, System.Collections.IComparer comparer) { return default(int); }
        bool System.Collections.IStructuralEquatable.Equals(object other, System.Collections.IEqualityComparer comparer) { return default(bool); }
        int System.Collections.IStructuralEquatable.GetHashCode(System.Collections.IEqualityComparer comparer) { return default(int); }
        int System.IComparable.CompareTo(object obj) { return default(int); }
        public override string ToString() { return default(string); }
    }
    public partial class Tuple<T1, T2, T3, T4, T5, T6> : System.Collections.IStructuralComparable, System.Collections.IStructuralEquatable, System.IComparable
    {
        public Tuple(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6) { }
        public T1 Item1 { get { return default(T1); } }
        public T2 Item2 { get { return default(T2); } }
        public T3 Item3 { get { return default(T3); } }
        public T4 Item4 { get { return default(T4); } }
        public T5 Item5 { get { return default(T5); } }
        public T6 Item6 { get { return default(T6); } }
        public override bool Equals(object obj) { return default(bool); }
        public override int GetHashCode() { return default(int); }
        int System.Collections.IStructuralComparable.CompareTo(object other, System.Collections.IComparer comparer) { return default(int); }
        bool System.Collections.IStructuralEquatable.Equals(object other, System.Collections.IEqualityComparer comparer) { return default(bool); }
        int System.Collections.IStructuralEquatable.GetHashCode(System.Collections.IEqualityComparer comparer) { return default(int); }
        int System.IComparable.CompareTo(object obj) { return default(int); }
        public override string ToString() { return default(string); }
    }
    public partial class Tuple<T1, T2, T3, T4, T5, T6, T7> : System.Collections.IStructuralComparable, System.Collections.IStructuralEquatable, System.IComparable
    {
        public Tuple(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7) { }
        public T1 Item1 { get { return default(T1); } }
        public T2 Item2 { get { return default(T2); } }
        public T3 Item3 { get { return default(T3); } }
        public T4 Item4 { get { return default(T4); } }
        public T5 Item5 { get { return default(T5); } }
        public T6 Item6 { get { return default(T6); } }
        public T7 Item7 { get { return default(T7); } }
        public override bool Equals(object obj) { return default(bool); }
        public override int GetHashCode() { return default(int); }
        int System.Collections.IStructuralComparable.CompareTo(object other, System.Collections.IComparer comparer) { return default(int); }
        bool System.Collections.IStructuralEquatable.Equals(object other, System.Collections.IEqualityComparer comparer) { return default(bool); }
        int System.Collections.IStructuralEquatable.GetHashCode(System.Collections.IEqualityComparer comparer) { return default(int); }
        int System.IComparable.CompareTo(object obj) { return default(int); }
        public override string ToString() { return default(string); }
    }
    public partial class Tuple<T1, T2, T3, T4, T5, T6, T7, TRest> : System.Collections.IStructuralComparable, System.Collections.IStructuralEquatable, System.IComparable
    {
        public Tuple(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, TRest rest) { }
        public T1 Item1 { get { return default(T1); } }
        public T2 Item2 { get { return default(T2); } }
        public T3 Item3 { get { return default(T3); } }
        public T4 Item4 { get { return default(T4); } }
        public T5 Item5 { get { return default(T5); } }
        public T6 Item6 { get { return default(T6); } }
        public T7 Item7 { get { return default(T7); } }
        public TRest Rest { get { return default(TRest); } }
        public override bool Equals(object obj) { return default(bool); }
        public override int GetHashCode() { return default(int); }
        int System.Collections.IStructuralComparable.CompareTo(object other, System.Collections.IComparer comparer) { return default(int); }
        bool System.Collections.IStructuralEquatable.Equals(object other, System.Collections.IEqualityComparer comparer) { return default(bool); }
        int System.Collections.IStructuralEquatable.GetHashCode(System.Collections.IEqualityComparer comparer) { return default(int); }
        int System.IComparable.CompareTo(object obj) { return default(int); }
        public override string ToString() { return default(string); }
    }
    public abstract partial class Type
    {
        public static readonly System.Type[] EmptyTypes;
        public static readonly object Missing;
        public static readonly char Delimiter;
        internal Type() { }
        public abstract string AssemblyQualifiedName { get; }
        public abstract string FullName { get; }
        public abstract int GenericParameterPosition { get; }
        public abstract System.Type[] GenericTypeArguments { get; }
        public bool HasElementType { get { return default(bool); } }
        public virtual bool IsArray { get { return default(bool); } }
        public virtual bool IsByRef { get { return default(bool); } }
        public abstract bool IsConstructedGenericType { get; }
        public abstract bool IsGenericParameter { get; }
        public bool IsNested { get { return default(bool); } }
        public virtual bool IsPointer { get { return default(bool); } }
        public abstract string Namespace { get; }
        public virtual System.RuntimeTypeHandle TypeHandle { get { return default(System.RuntimeTypeHandle); } }
        public override bool Equals(object o) { return default(bool); }
        public bool Equals(System.Type o) { return default(bool); }
        public abstract int GetArrayRank();
        public abstract System.Type GetElementType();
        public abstract System.Type GetGenericTypeDefinition();
        public override int GetHashCode() { return default(int); }
        public static System.Type GetType(string typeName) { return default(System.Type); }
        public static System.Type GetType(string typeName, bool throwOnError) { return default(System.Type); }
        public static System.Type GetType(string typeName, bool throwOnError, bool ignoreCase) { return default(System.Type); }
        public static System.TypeCode GetTypeCode(System.Type type) { return default(System.TypeCode);  }
        public static System.Type GetTypeFromHandle(System.RuntimeTypeHandle handle) { return default(System.Type); }
        public abstract System.Type MakeArrayType();
        public abstract System.Type MakeArrayType(int rank);
        public abstract System.Type MakeByRefType();
        public abstract System.Type MakeGenericType(params System.Type[] typeArguments);
        public abstract System.Type MakePointerType();
        public override string ToString() { return default(string); }
    }
    public partial class TypeAccessException : System.TypeLoadException
    {
        public TypeAccessException() { }
        public TypeAccessException(string message) { }
        public TypeAccessException(string message, System.Exception inner) { }
    }
    public enum TypeCode
    {
        Boolean = 3,
        Byte = 6,
        Char = 4,
        DateTime = 16,
        Decimal = 15,
        Double = 14,
        Empty = 0,
        Int16 = 7,
        Int32 = 9,
        Int64 = 11,
        Object = 1,
        SByte = 5,
        Single = 13,
        String = 18,
        UInt16 = 8,
        UInt32 = 10,
        UInt64 = 12,
    }
    public sealed partial class TypeInitializationException : System.Exception
    {
        public TypeInitializationException(string fullTypeName, System.Exception innerException) { }
        public string TypeName { get { return default(string); } }
    }
    public partial class TypeLoadException : System.Exception
    {
        public TypeLoadException() { }
        public TypeLoadException(string message) { }
        public TypeLoadException(string message, System.Exception inner) { }
        public override string Message { get { return default(string); } }
        public string TypeName { get { return default(string); } }
    }
    [System.CLSCompliantAttribute(false)]
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct UInt16 : System.IComparable, System.IComparable<ushort>, System.IConvertible, System.IEquatable<ushort>, System.IFormattable
    {
        public const ushort MaxValue = (ushort)65535;
        public const ushort MinValue = (ushort)0;
        public int CompareTo(ushort value) { return default(int); }
        public override bool Equals(object obj) { return default(bool); }
        public bool Equals(ushort obj) { return default(bool); }
        public override int GetHashCode() { return default(int); }
        [System.CLSCompliantAttribute(false)]
        public static ushort Parse(string s) { return default(ushort); }
        [System.CLSCompliantAttribute(false)]
        public static ushort Parse(string s, System.Globalization.NumberStyles style) { return default(ushort); }
        [System.CLSCompliantAttribute(false)]
        public static ushort Parse(string s, System.Globalization.NumberStyles style, System.IFormatProvider provider) { return default(ushort); }
        [System.CLSCompliantAttribute(false)]
        public static ushort Parse(string s, System.IFormatProvider provider) { return default(ushort); }
        int System.IComparable.CompareTo(object value) { return default(int); }
        System.TypeCode System.IConvertible.GetTypeCode() { return default(System.TypeCode); }
        bool System.IConvertible.ToBoolean(System.IFormatProvider provider) { return default(bool); }
        byte System.IConvertible.ToByte(System.IFormatProvider provider) { return default(byte); }
        char System.IConvertible.ToChar(System.IFormatProvider provider) { return default(char); }
        System.DateTime System.IConvertible.ToDateTime(System.IFormatProvider provider) { return default(System.DateTime); }
        decimal System.IConvertible.ToDecimal(System.IFormatProvider provider) { return default(decimal); }
        double System.IConvertible.ToDouble(System.IFormatProvider provider) { return default(double); }
        short System.IConvertible.ToInt16(System.IFormatProvider provider) { return default(short); }
        int System.IConvertible.ToInt32(System.IFormatProvider provider) { return default(int); }
        long System.IConvertible.ToInt64(System.IFormatProvider provider) { return default(long); }
        sbyte System.IConvertible.ToSByte(System.IFormatProvider provider) { return default(sbyte); }
        float System.IConvertible.ToSingle(System.IFormatProvider provider) { return default(float); }
        object System.IConvertible.ToType(System.Type type, System.IFormatProvider provider) { return default(object); }
        ushort System.IConvertible.ToUInt16(System.IFormatProvider provider) { return default(ushort); }
        uint System.IConvertible.ToUInt32(System.IFormatProvider provider) { return default(uint); }
        ulong System.IConvertible.ToUInt64(System.IFormatProvider provider) { return default(ulong); }
        public override string ToString() { return default(string); }
        public string ToString(System.IFormatProvider provider) { return default(string); }
        public string ToString(string format) { return default(string); }
        public string ToString(string format, System.IFormatProvider provider) { return default(string); }
        [System.CLSCompliantAttribute(false)]
        public static bool TryParse(string s, System.Globalization.NumberStyles style, System.IFormatProvider provider, out ushort result) { result = default(ushort); return default(bool); }
        [System.CLSCompliantAttribute(false)]
        public static bool TryParse(string s, out ushort result) { result = default(ushort); return default(bool); }
    }
    [System.CLSCompliantAttribute(false)]
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct UInt32 : System.IComparable, System.IComparable<uint>, System.IConvertible, System.IEquatable<uint>, System.IFormattable
    {
        public const uint MaxValue = (uint)4294967295;
        public const uint MinValue = (uint)0;
        public int CompareTo(uint value) { return default(int); }
        public override bool Equals(object obj) { return default(bool); }
        public bool Equals(uint obj) { return default(bool); }
        public override int GetHashCode() { return default(int); }
        [System.CLSCompliantAttribute(false)]
        public static uint Parse(string s) { return default(uint); }
        [System.CLSCompliantAttribute(false)]
        public static uint Parse(string s, System.Globalization.NumberStyles style) { return default(uint); }
        [System.CLSCompliantAttribute(false)]
        public static uint Parse(string s, System.Globalization.NumberStyles style, System.IFormatProvider provider) { return default(uint); }
        [System.CLSCompliantAttribute(false)]
        public static uint Parse(string s, System.IFormatProvider provider) { return default(uint); }
        int System.IComparable.CompareTo(object value) { return default(int); }
        System.TypeCode System.IConvertible.GetTypeCode() { return default(System.TypeCode); }
        bool System.IConvertible.ToBoolean(System.IFormatProvider provider) { return default(bool); }
        byte System.IConvertible.ToByte(System.IFormatProvider provider) { return default(byte); }
        char System.IConvertible.ToChar(System.IFormatProvider provider) { return default(char); }
        System.DateTime System.IConvertible.ToDateTime(System.IFormatProvider provider) { return default(System.DateTime); }
        decimal System.IConvertible.ToDecimal(System.IFormatProvider provider) { return default(decimal); }
        double System.IConvertible.ToDouble(System.IFormatProvider provider) { return default(double); }
        short System.IConvertible.ToInt16(System.IFormatProvider provider) { return default(short); }
        int System.IConvertible.ToInt32(System.IFormatProvider provider) { return default(int); }
        long System.IConvertible.ToInt64(System.IFormatProvider provider) { return default(long); }
        sbyte System.IConvertible.ToSByte(System.IFormatProvider provider) { return default(sbyte); }
        float System.IConvertible.ToSingle(System.IFormatProvider provider) { return default(float); }
        object System.IConvertible.ToType(System.Type type, System.IFormatProvider provider) { return default(object); }
        ushort System.IConvertible.ToUInt16(System.IFormatProvider provider) { return default(ushort); }
        uint System.IConvertible.ToUInt32(System.IFormatProvider provider) { return default(uint); }
        ulong System.IConvertible.ToUInt64(System.IFormatProvider provider) { return default(ulong); }
        public override string ToString() { return default(string); }
        public string ToString(System.IFormatProvider provider) { return default(string); }
        public string ToString(string format) { return default(string); }
        public string ToString(string format, System.IFormatProvider provider) { return default(string); }
        [System.CLSCompliantAttribute(false)]
        public static bool TryParse(string s, System.Globalization.NumberStyles style, System.IFormatProvider provider, out uint result) { result = default(uint); return default(bool); }
        [System.CLSCompliantAttribute(false)]
        public static bool TryParse(string s, out uint result) { result = default(uint); return default(bool); }
    }
    [System.CLSCompliantAttribute(false)]
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct UInt64 : System.IComparable, System.IComparable<ulong>, System.IConvertible, System.IEquatable<ulong>, System.IFormattable
    {
        public const ulong MaxValue = (ulong)18446744073709551615;
        public const ulong MinValue = (ulong)0;
        public int CompareTo(ulong value) { return default(int); }
        public override bool Equals(object obj) { return default(bool); }
        public bool Equals(ulong obj) { return default(bool); }
        public override int GetHashCode() { return default(int); }
        [System.CLSCompliantAttribute(false)]
        public static ulong Parse(string s) { return default(ulong); }
        [System.CLSCompliantAttribute(false)]
        public static ulong Parse(string s, System.Globalization.NumberStyles style) { return default(ulong); }
        [System.CLSCompliantAttribute(false)]
        public static ulong Parse(string s, System.Globalization.NumberStyles style, System.IFormatProvider provider) { return default(ulong); }
        [System.CLSCompliantAttribute(false)]
        public static ulong Parse(string s, System.IFormatProvider provider) { return default(ulong); }
        int System.IComparable.CompareTo(object value) { return default(int); }
        System.TypeCode System.IConvertible.GetTypeCode() { return default(System.TypeCode); }
        bool System.IConvertible.ToBoolean(System.IFormatProvider provider) { return default(bool); }
        byte System.IConvertible.ToByte(System.IFormatProvider provider) { return default(byte); }
        char System.IConvertible.ToChar(System.IFormatProvider provider) { return default(char); }
        System.DateTime System.IConvertible.ToDateTime(System.IFormatProvider provider) { return default(System.DateTime); }
        decimal System.IConvertible.ToDecimal(System.IFormatProvider provider) { return default(decimal); }
        double System.IConvertible.ToDouble(System.IFormatProvider provider) { return default(double); }
        short System.IConvertible.ToInt16(System.IFormatProvider provider) { return default(short); }
        int System.IConvertible.ToInt32(System.IFormatProvider provider) { return default(int); }
        long System.IConvertible.ToInt64(System.IFormatProvider provider) { return default(long); }
        sbyte System.IConvertible.ToSByte(System.IFormatProvider provider) { return default(sbyte); }
        float System.IConvertible.ToSingle(System.IFormatProvider provider) { return default(float); }
        object System.IConvertible.ToType(System.Type type, System.IFormatProvider provider) { return default(object); }
        ushort System.IConvertible.ToUInt16(System.IFormatProvider provider) { return default(ushort); }
        uint System.IConvertible.ToUInt32(System.IFormatProvider provider) { return default(uint); }
        ulong System.IConvertible.ToUInt64(System.IFormatProvider provider) { return default(ulong); }
        public override string ToString() { return default(string); }
        public string ToString(System.IFormatProvider provider) { return default(string); }
        public string ToString(string format) { return default(string); }
        public string ToString(string format, System.IFormatProvider provider) { return default(string); }
        [System.CLSCompliantAttribute(false)]
        public static bool TryParse(string s, System.Globalization.NumberStyles style, System.IFormatProvider provider, out ulong result) { result = default(ulong); return default(bool); }
        [System.CLSCompliantAttribute(false)]
        public static bool TryParse(string s, out ulong result) { result = default(ulong); return default(bool); }
    }
    [System.CLSCompliantAttribute(false)]
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct UIntPtr
    {
        public static readonly System.UIntPtr Zero;
        public UIntPtr(uint value) { throw new System.NotImplementedException(); }
        public UIntPtr(ulong value) { throw new System.NotImplementedException(); }
        [System.CLSCompliantAttribute(false)]
        [System.Security.SecurityCriticalAttribute]
        public unsafe UIntPtr(void* value) { throw new System.NotImplementedException(); }
        public static int Size { get { return default(int); } }
        public static System.UIntPtr Add(System.UIntPtr pointer, int offset) { return default(System.UIntPtr); }
        public override bool Equals(object obj) { return default(bool); }
        public override int GetHashCode() { return default(int); }
        public static System.UIntPtr operator +(System.UIntPtr pointer, int offset) { return default(System.UIntPtr); }
        public static bool operator ==(System.UIntPtr value1, System.UIntPtr value2) { return default(bool); }
        public static explicit operator System.UIntPtr(uint value) { return default(System.UIntPtr); }
        public static explicit operator System.UIntPtr(ulong value) { return default(System.UIntPtr); }
        public static explicit operator uint (System.UIntPtr value) { return default(uint); }
        public static explicit operator ulong (System.UIntPtr value) { return default(ulong); }
        [System.CLSCompliantAttribute(false)]
        [System.Security.SecurityCriticalAttribute]
        public unsafe static explicit operator void* (System.UIntPtr value) { return default(void*); }
        [System.CLSCompliantAttribute(false)]
        [System.Security.SecurityCriticalAttribute]
        public unsafe static explicit operator System.UIntPtr(void* value) { return default(System.UIntPtr); }
        public static bool operator !=(System.UIntPtr value1, System.UIntPtr value2) { return default(bool); }
        public static System.UIntPtr operator -(System.UIntPtr pointer, int offset) { return default(System.UIntPtr); }
        public static System.UIntPtr Subtract(System.UIntPtr pointer, int offset) { return default(System.UIntPtr); }
        [System.CLSCompliantAttribute(false)]
        public unsafe void* ToPointer() { return default(void*); }
        public override string ToString() { return default(string); }
        public uint ToUInt32() { return default(uint); }
        public ulong ToUInt64() { return default(ulong); }
    }
    public partial class UnauthorizedAccessException : System.Exception
    {
        public UnauthorizedAccessException() { }
        public UnauthorizedAccessException(string message) { }
        public UnauthorizedAccessException(string message, System.Exception inner) { }
    }
    public partial class Uri
    {
        public Uri(string uriString) { }
        public Uri(string uriString, System.UriKind uriKind) { }
        public Uri(System.Uri baseUri, string relativeUri) { }
        public Uri(System.Uri baseUri, System.Uri relativeUri) { }
        public string AbsolutePath { get { return default(string); } }
        public string AbsoluteUri { get { return default(string); } }
        public string Authority { get { return default(string); } }
        public string DnsSafeHost { get { return default(string); } }
        public string Fragment { get { return default(string); } }
        public string Host { get { return default(string); } }
        public System.UriHostNameType HostNameType { get { return default(System.UriHostNameType); } }
        public string IdnHost { get { return default(string); } }
        public bool IsAbsoluteUri { get { return default(bool); } }
        public bool IsDefaultPort { get { return default(bool); } }
        public bool IsFile { get { return default(bool); } }
        public bool IsLoopback { get { return default(bool); } }
        public bool IsUnc { get { return default(bool); } }
        public string LocalPath { get { return default(string); } }
        public string OriginalString { get { return default(string); } }
        public string PathAndQuery { get { return default(string); } }
        public int Port { get { return default(int); } }
        public string Query { get { return default(string); } }
        public string Scheme { get { return default(string); } }
        public string[] Segments { get { return default(string[]); } }
        public bool UserEscaped { get { return default(bool); } }
        public string UserInfo { get { return default(string); } }
        public static System.UriHostNameType CheckHostName(string name) { return default(System.UriHostNameType); }
        public static bool CheckSchemeName(string schemeName) { return default(bool); }
        public static int Compare(System.Uri uri1, System.Uri uri2, System.UriComponents partsToCompare, System.UriFormat compareFormat, System.StringComparison comparisonType) { return default(int); }
        public override bool Equals(object comparand) { return default(bool); }
        public static string EscapeDataString(string stringToEscape) { return default(string); }
        public static string EscapeUriString(string stringToEscape) { return default(string); }
        public string GetComponents(System.UriComponents components, System.UriFormat format) { return default(string); }
        public override int GetHashCode() { return default(int); }
        public bool IsBaseOf(System.Uri uri) { return default(bool); }
        public bool IsWellFormedOriginalString() { return default(bool); }
        public static bool IsWellFormedUriString(string uriString, System.UriKind uriKind) { return default(bool); }
        public System.Uri MakeRelativeUri(System.Uri uri) { return default(System.Uri); }
        public static bool operator ==(System.Uri uri1, System.Uri uri2) { return default(bool); }
        public static bool operator !=(System.Uri uri1, System.Uri uri2) { return default(bool); }
        public override string ToString() { return default(string); }
        public static bool TryCreate(string uriString, System.UriKind uriKind, out System.Uri result) { result = default(System.Uri); return default(bool); }
        public static bool TryCreate(System.Uri baseUri, string relativeUri, out System.Uri result) { result = default(System.Uri); return default(bool); }
        public static bool TryCreate(System.Uri baseUri, System.Uri relativeUri, out System.Uri result) { result = default(System.Uri); return default(bool); }
        public static string UnescapeDataString(string stringToUnescape) { return default(string); }
    }
    [System.FlagsAttribute]
    public enum UriComponents
    {
        AbsoluteUri = 127,
        Fragment = 64,
        Host = 4,
        HostAndPort = 132,
        HttpRequestUrl = 61,
        KeepDelimiter = 1073741824,
        NormalizedHost = 256,
        Path = 16,
        PathAndQuery = 48,
        Port = 8,
        Query = 32,
        Scheme = 1,
        SchemeAndServer = 13,
        SerializationInfoString = -2147483648,
        StrongAuthority = 134,
        StrongPort = 128,
        UserInfo = 2,
    }
    public enum UriFormat
    {
        SafeUnescaped = 3,
        Unescaped = 2,
        UriEscaped = 1,
    }
    public partial class UriFormatException : System.FormatException
    {
        public UriFormatException() { }
        public UriFormatException(string textString) { }
        public UriFormatException(string textString, System.Exception e) { }
    }
    public enum UriHostNameType
    {
        Basic = 1,
        Dns = 2,
        IPv4 = 3,
        IPv6 = 4,
        Unknown = 0,
    }
    public enum UriKind
    {
        Absolute = 1,
        Relative = 2,
        RelativeOrAbsolute = 0,
    }
    public abstract partial class ValueType
    {
        protected ValueType() { }
        public override bool Equals(object obj) { return default(bool); }
        public override int GetHashCode() { return default(int); }
        public override string ToString() { return default(string); }
    }
    public sealed partial class Version : System.IComparable, System.IComparable<System.Version>, System.IEquatable<System.Version>
    {
        public Version(int major, int minor) { }
        public Version(int major, int minor, int build) { }
        public Version(int major, int minor, int build, int revision) { }
        public Version(string version) { }
        public int Build { get { return default(int); } }
        public int Major { get { return default(int); } }
        public short MajorRevision { get { return default(short); } }
        public int Minor { get { return default(int); } }
        public short MinorRevision { get { return default(short); } }
        public int Revision { get { return default(int); } }
        public int CompareTo(System.Version value) { return default(int); }
        public override bool Equals(object obj) { return default(bool); }
        public bool Equals(System.Version obj) { return default(bool); }
        public override int GetHashCode() { return default(int); }
        public static bool operator ==(System.Version v1, System.Version v2) { return default(bool); }
        public static bool operator >(System.Version v1, System.Version v2) { return default(bool); }
        public static bool operator >=(System.Version v1, System.Version v2) { return default(bool); }
        public static bool operator !=(System.Version v1, System.Version v2) { return default(bool); }
        public static bool operator <(System.Version v1, System.Version v2) { return default(bool); }
        public static bool operator <=(System.Version v1, System.Version v2) { return default(bool); }
        public static System.Version Parse(string input) { return default(System.Version); }
        int System.IComparable.CompareTo(object version) { return default(int); }
        public override string ToString() { return default(string); }
        public string ToString(int fieldCount) { return default(string); }
        public static bool TryParse(string input, out System.Version result) { result = default(System.Version); return default(bool); }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential, Size = 1)]
    public partial struct Void
    {
    }
    public partial class WeakReference
    {
        public WeakReference(object target) { }
        public WeakReference(object target, bool trackResurrection) { }
        public virtual bool IsAlive { get { return default(bool); } }
        public virtual object Target { get { return default(object); } set { } }
        public virtual bool TrackResurrection { get { return default(bool); } }
        ~WeakReference() { }
    }
    public sealed partial class WeakReference<T> where T : class
    {
        public WeakReference(T target) { }
        public WeakReference(T target, bool trackResurrection) { }
        ~WeakReference() { }
        public void SetTarget(T target) { }
        public bool TryGetTarget(out T target) { target = default(T); return default(bool); }
    }
}
namespace System.Collections
{
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct DictionaryEntry
    {
        public DictionaryEntry(object key, object value) { throw new System.NotImplementedException(); }
        public object Key { get { return default(object); } set { } }
        public object Value { get { return default(object); } set { } }
    }
    public partial interface ICollection : System.Collections.IEnumerable
    {
        int Count { get; }
        bool IsSynchronized { get; }
        object SyncRoot { get; }
        void CopyTo(System.Array array, int index);
    }
    public partial interface IComparer
    {
        int Compare(object x, object y);
    }
    public partial interface IDictionary : System.Collections.ICollection, System.Collections.IEnumerable
    {
        bool IsFixedSize { get; }
        bool IsReadOnly { get; }
        object this[object key] { get; set; }
        System.Collections.ICollection Keys { get; }
        System.Collections.ICollection Values { get; }
        void Add(object key, object value);
        void Clear();
        bool Contains(object key);
        new System.Collections.IDictionaryEnumerator GetEnumerator();
        void Remove(object key);
    }
    public partial interface IDictionaryEnumerator : System.Collections.IEnumerator
    {
        System.Collections.DictionaryEntry Entry { get; }
        object Key { get; }
        object Value { get; }
    }
    public partial interface IEnumerable
    {
        System.Collections.IEnumerator GetEnumerator();
    }
    public partial interface IEnumerator
    {
        object Current { get; }
        bool MoveNext();
        void Reset();
    }
    public partial interface IEqualityComparer
    {
        bool Equals(object x, object y);
        int GetHashCode(object obj);
    }
    public partial interface IList : System.Collections.ICollection, System.Collections.IEnumerable
    {
        bool IsFixedSize { get; }
        bool IsReadOnly { get; }
        object this[int index] { get; set; }
        int Add(object value);
        void Clear();
        bool Contains(object value);
        int IndexOf(object value);
        void Insert(int index, object value);
        void Remove(object value);
        void RemoveAt(int index);
    }
    public partial interface IStructuralComparable
    {
        int CompareTo(object other, System.Collections.IComparer comparer);
    }
    public partial interface IStructuralEquatable
    {
        bool Equals(object other, System.Collections.IEqualityComparer comparer);
        int GetHashCode(System.Collections.IEqualityComparer comparer);
    }
}
namespace System.Collections.Generic
{
    public partial interface ICollection<T> : System.Collections.Generic.IEnumerable<T>, System.Collections.IEnumerable
    {
        int Count { get; }
        bool IsReadOnly { get; }
        void Add(T item);
        void Clear();
        bool Contains(T item);
        void CopyTo(T[] array, int arrayIndex);
        bool Remove(T item);
    }
    public partial interface IComparer<in T>
    {
        int Compare(T x, T y);
    }
    public partial interface IDictionary<TKey, TValue> : System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<TKey, TValue>>, System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<TKey, TValue>>, System.Collections.IEnumerable
    {
        TValue this[TKey key] { get; set; }
        System.Collections.Generic.ICollection<TKey> Keys { get; }
        System.Collections.Generic.ICollection<TValue> Values { get; }
        void Add(TKey key, TValue value);
        bool ContainsKey(TKey key);
        bool Remove(TKey key);
        bool TryGetValue(TKey key, out TValue value);
    }
    public partial interface IEnumerable<out T> : System.Collections.IEnumerable
    {
        new System.Collections.Generic.IEnumerator<T> GetEnumerator();
    }
    public partial interface IEnumerator<out T> : System.Collections.IEnumerator, System.IDisposable
    {
        new T Current { get; }
    }
    public partial interface IEqualityComparer<in T>
    {
        bool Equals(T x, T y);
        int GetHashCode(T obj);
    }
    public partial interface IList<T> : System.Collections.Generic.ICollection<T>, System.Collections.Generic.IEnumerable<T>, System.Collections.IEnumerable
    {
        T this[int index] { get; set; }
        int IndexOf(T item);
        void Insert(int index, T item);
        void RemoveAt(int index);
    }
    public partial interface IReadOnlyCollection<out T> : System.Collections.Generic.IEnumerable<T>, System.Collections.IEnumerable
    {
        int Count { get; }
    }
    public partial interface IReadOnlyDictionary<TKey, TValue> : System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<TKey, TValue>>, System.Collections.Generic.IReadOnlyCollection<System.Collections.Generic.KeyValuePair<TKey, TValue>>, System.Collections.IEnumerable
    {
        TValue this[TKey key] { get; }
        System.Collections.Generic.IEnumerable<TKey> Keys { get; }
        System.Collections.Generic.IEnumerable<TValue> Values { get; }
        bool ContainsKey(TKey key);
        bool TryGetValue(TKey key, out TValue value);
    }
    public partial interface IReadOnlyList<out T> : System.Collections.Generic.IEnumerable<T>, System.Collections.Generic.IReadOnlyCollection<T>, System.Collections.IEnumerable
    {
        T this[int index] { get; }
    }
    public partial interface ISet<T> : System.Collections.Generic.ICollection<T>, System.Collections.Generic.IEnumerable<T>, System.Collections.IEnumerable
    {
        new bool Add(T item);
        void ExceptWith(System.Collections.Generic.IEnumerable<T> other);
        void IntersectWith(System.Collections.Generic.IEnumerable<T> other);
        bool IsProperSubsetOf(System.Collections.Generic.IEnumerable<T> other);
        bool IsProperSupersetOf(System.Collections.Generic.IEnumerable<T> other);
        bool IsSubsetOf(System.Collections.Generic.IEnumerable<T> other);
        bool IsSupersetOf(System.Collections.Generic.IEnumerable<T> other);
        bool Overlaps(System.Collections.Generic.IEnumerable<T> other);
        bool SetEquals(System.Collections.Generic.IEnumerable<T> other);
        void SymmetricExceptWith(System.Collections.Generic.IEnumerable<T> other);
        void UnionWith(System.Collections.Generic.IEnumerable<T> other);
    }
    public partial class KeyNotFoundException : System.Exception
    {
        public KeyNotFoundException() { }
        public KeyNotFoundException(string message) { }
        public KeyNotFoundException(string message, System.Exception innerException) { }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct KeyValuePair<TKey, TValue>
    {
        public KeyValuePair(TKey key, TValue value) { throw new System.NotImplementedException(); }
        public TKey Key { get { return default(TKey); } }
        public TValue Value { get { return default(TValue); } }
        public override string ToString() { return default(string); }
    }
}
namespace System.Collections.ObjectModel
{
    public partial class Collection<T> : System.Collections.Generic.ICollection<T>, System.Collections.Generic.IEnumerable<T>, System.Collections.Generic.IList<T>, System.Collections.Generic.IReadOnlyCollection<T>, System.Collections.Generic.IReadOnlyList<T>, System.Collections.ICollection, System.Collections.IEnumerable, System.Collections.IList
    {
        public Collection() { }
        public Collection(System.Collections.Generic.IList<T> list) { }
        public int Count { get { return default(int); } }
        public T this[int index] { get { return default(T); } set { } }
        protected System.Collections.Generic.IList<T> Items { get { return default(System.Collections.Generic.IList<T>); } }
        bool System.Collections.Generic.ICollection<T>.IsReadOnly { get { return default(bool); } }
        bool System.Collections.ICollection.IsSynchronized { get { return default(bool); } }
        object System.Collections.ICollection.SyncRoot { get { return default(object); } }
        bool System.Collections.IList.IsFixedSize { get { return default(bool); } }
        bool System.Collections.IList.IsReadOnly { get { return default(bool); } }
        object System.Collections.IList.this[int index] { get { return default(object); } set { } }
        public void Add(T item) { }
        public void Clear() { }
        protected virtual void ClearItems() { }
        public bool Contains(T item) { return default(bool); }
        public void CopyTo(T[] array, int index) { }
        public System.Collections.Generic.IEnumerator<T> GetEnumerator() { return default(System.Collections.Generic.IEnumerator<T>); }
        public int IndexOf(T item) { return default(int); }
        public void Insert(int index, T item) { }
        protected virtual void InsertItem(int index, T item) { }
        public bool Remove(T item) { return default(bool); }
        public void RemoveAt(int index) { }
        protected virtual void RemoveItem(int index) { }
        protected virtual void SetItem(int index, T item) { }
        void System.Collections.ICollection.CopyTo(System.Array array, int index) { }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return default(System.Collections.IEnumerator); }
        int System.Collections.IList.Add(object value) { return default(int); }
        bool System.Collections.IList.Contains(object value) { return default(bool); }
        int System.Collections.IList.IndexOf(object value) { return default(int); }
        void System.Collections.IList.Insert(int index, object value) { }
        void System.Collections.IList.Remove(object value) { }
    }
    public partial class ReadOnlyCollection<T> : System.Collections.Generic.ICollection<T>, System.Collections.Generic.IEnumerable<T>, System.Collections.Generic.IList<T>, System.Collections.Generic.IReadOnlyCollection<T>, System.Collections.Generic.IReadOnlyList<T>, System.Collections.ICollection, System.Collections.IEnumerable, System.Collections.IList
    {
        public ReadOnlyCollection(System.Collections.Generic.IList<T> list) { }
        public int Count { get { return default(int); } }
        public T this[int index] { get { return default(T); } }
        protected System.Collections.Generic.IList<T> Items { get { return default(System.Collections.Generic.IList<T>); } }
        bool System.Collections.Generic.ICollection<T>.IsReadOnly { get { return default(bool); } }
        T System.Collections.Generic.IList<T>.this[int index] { get { return default(T); } set { } }
        bool System.Collections.ICollection.IsSynchronized { get { return default(bool); } }
        object System.Collections.ICollection.SyncRoot { get { return default(object); } }
        bool System.Collections.IList.IsFixedSize { get { return default(bool); } }
        bool System.Collections.IList.IsReadOnly { get { return default(bool); } }
        object System.Collections.IList.this[int index] { get { return default(object); } set { } }
        public bool Contains(T value) { return default(bool); }
        public void CopyTo(T[] array, int index) { }
        public System.Collections.Generic.IEnumerator<T> GetEnumerator() { return default(System.Collections.Generic.IEnumerator<T>); }
        public int IndexOf(T value) { return default(int); }
        void System.Collections.Generic.ICollection<T>.Add(T value) { }
        void System.Collections.Generic.ICollection<T>.Clear() { }
        bool System.Collections.Generic.ICollection<T>.Remove(T value) { return default(bool); }
        void System.Collections.Generic.IList<T>.Insert(int index, T value) { }
        void System.Collections.Generic.IList<T>.RemoveAt(int index) { }
        void System.Collections.ICollection.CopyTo(System.Array array, int index) { }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return default(System.Collections.IEnumerator); }
        int System.Collections.IList.Add(object value) { return default(int); }
        void System.Collections.IList.Clear() { }
        bool System.Collections.IList.Contains(object value) { return default(bool); }
        int System.Collections.IList.IndexOf(object value) { return default(int); }
        void System.Collections.IList.Insert(int index, object value) { }
        void System.Collections.IList.Remove(object value) { }
        void System.Collections.IList.RemoveAt(int index) { }
    }
}
namespace System.ComponentModel
{
    [System.AttributeUsageAttribute((System.AttributeTargets)(32767))]
    public partial class DefaultValueAttribute : System.Attribute
    {
        public DefaultValueAttribute(bool value) { }
        public DefaultValueAttribute(byte value) { }
        public DefaultValueAttribute(char value) { }
        public DefaultValueAttribute(double value) { }
        public DefaultValueAttribute(short value) { }
        public DefaultValueAttribute(int value) { }
        public DefaultValueAttribute(long value) { }
        public DefaultValueAttribute(object value) { }
        public DefaultValueAttribute(float value) { }
        public DefaultValueAttribute(string value) { }
        public DefaultValueAttribute(System.Type type, string value) { }
        public virtual object Value { get { return default(object); } }
        public override bool Equals(object obj) { return default(bool); }
        public override int GetHashCode() { return default(int); }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(6140))]
    public sealed partial class EditorBrowsableAttribute : System.Attribute
    {
        public EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState state) { }
        public System.ComponentModel.EditorBrowsableState State { get { return default(System.ComponentModel.EditorBrowsableState); } }
        public override bool Equals(object obj) { return default(bool); }
        public override int GetHashCode() { return default(int); }
    }
    public enum EditorBrowsableState
    {
        Advanced = 2,
        Always = 0,
        Never = 1,
    }
}
namespace System.Diagnostics
{
    [System.AttributeUsageAttribute((System.AttributeTargets)(68), AllowMultiple = true)]
    public sealed partial class ConditionalAttribute : System.Attribute
    {
        public ConditionalAttribute(string conditionString) { }
        public string ConditionString { get { return default(string); } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(3), AllowMultiple = false)]
    public sealed partial class DebuggableAttribute : System.Attribute
    {
        public DebuggableAttribute(System.Diagnostics.DebuggableAttribute.DebuggingModes modes) { }
        [System.FlagsAttribute]
        public enum DebuggingModes
        {
            Default = 1,
            DisableOptimizations = 256,
            EnableEditAndContinue = 4,
            IgnoreSymbolStoreSequencePoints = 2,
            None = 0,
        }
    }
}
namespace System.Globalization
{
    [System.FlagsAttribute]
    public enum DateTimeStyles
    {
        AdjustToUniversal = 16,
        AllowInnerWhite = 4,
        AllowLeadingWhite = 1,
        AllowTrailingWhite = 2,
        AllowWhiteSpaces = 7,
        AssumeLocal = 32,
        AssumeUniversal = 64,
        NoCurrentDateDefault = 8,
        None = 0,
        RoundtripKind = 128,
    }
    [System.FlagsAttribute]
    public enum NumberStyles
    {
        AllowCurrencySymbol = 256,
        AllowDecimalPoint = 32,
        AllowExponent = 128,
        AllowHexSpecifier = 512,
        AllowLeadingSign = 4,
        AllowLeadingWhite = 1,
        AllowParentheses = 16,
        AllowThousands = 64,
        AllowTrailingSign = 8,
        AllowTrailingWhite = 2,
        Any = 511,
        Currency = 383,
        Float = 167,
        HexNumber = 515,
        Integer = 7,
        None = 0,
        Number = 111,
    }
    [System.FlagsAttribute]
    public enum TimeSpanStyles
    {
        AssumeNegative = 1,
        None = 0,
    }
}
namespace System.IO
{
    public partial class DirectoryNotFoundException : System.IO.IOException
    {
        public DirectoryNotFoundException() { }
        public DirectoryNotFoundException(string message) { }
        public DirectoryNotFoundException(string message, System.Exception innerException) { }
    }
    public partial class FileLoadException : System.IO.IOException
    {
        public FileLoadException() { }
        public FileLoadException(string message) { }
        public FileLoadException(string message, System.Exception inner) { }
        public FileLoadException(string message, string fileName) { }
        public FileLoadException(string message, string fileName, System.Exception inner) { }
        public string FileName { get { return default(string); } }
        public override string Message { get { return default(string); } }
        public override string ToString() { return default(string); }
    }
    public partial class FileNotFoundException : System.IO.IOException
    {
        public FileNotFoundException() { }
        public FileNotFoundException(string message) { }
        public FileNotFoundException(string message, System.Exception innerException) { }
        public FileNotFoundException(string message, string fileName) { }
        public FileNotFoundException(string message, string fileName, System.Exception innerException) { }
        public string FileName { get { return default(string); } }
        public override string Message { get { return default(string); } }
        public override string ToString() { return default(string); }
    }
    public partial class IOException : System.Exception
    {
        public IOException() { }
        public IOException(string message) { }
        public IOException(string message, System.Exception innerException) { }
        public IOException(string message, int hresult) { }
    }
    public partial class PathTooLongException : System.IO.IOException
    {
        public PathTooLongException() { }
        public PathTooLongException(string message) { }
        public PathTooLongException(string message, System.Exception innerException) { }
    }
}
namespace System.Reflection
{
    [System.AttributeUsageAttribute((System.AttributeTargets)(1), Inherited = false)]
    public sealed partial class AssemblyCompanyAttribute : System.Attribute
    {
        public AssemblyCompanyAttribute(string company) { }
        public string Company { get { return default(string); } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(1), Inherited = false)]
    public sealed partial class AssemblyConfigurationAttribute : System.Attribute
    {
        public AssemblyConfigurationAttribute(string configuration) { }
        public string Configuration { get { return default(string); } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(1), Inherited = false)]
    public sealed partial class AssemblyCopyrightAttribute : System.Attribute
    {
        public AssemblyCopyrightAttribute(string copyright) { }
        public string Copyright { get { return default(string); } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(1), Inherited = false)]
    public sealed partial class AssemblyCultureAttribute : System.Attribute
    {
        public AssemblyCultureAttribute(string culture) { }
        public string Culture { get { return default(string); } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(1), Inherited = false)]
    public sealed partial class AssemblyDefaultAliasAttribute : System.Attribute
    {
        public AssemblyDefaultAliasAttribute(string defaultAlias) { }
        public string DefaultAlias { get { return default(string); } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(1), Inherited = false)]
    public sealed partial class AssemblyDelaySignAttribute : System.Attribute
    {
        public AssemblyDelaySignAttribute(bool delaySign) { }
        public bool DelaySign { get { return default(bool); } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(1), Inherited = false)]
    public sealed partial class AssemblyDescriptionAttribute : System.Attribute
    {
        public AssemblyDescriptionAttribute(string description) { }
        public string Description { get { return default(string); } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(1), Inherited = false)]
    public sealed partial class AssemblyFileVersionAttribute : System.Attribute
    {
        public AssemblyFileVersionAttribute(string version) { }
        public string Version { get { return default(string); } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(1), Inherited = false)]
    public sealed partial class AssemblyFlagsAttribute : System.Attribute
    {
        public AssemblyFlagsAttribute(System.Reflection.AssemblyNameFlags assemblyFlags) { }
        public int AssemblyFlags { get { return default(int); } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(1), Inherited = false)]
    public sealed partial class AssemblyInformationalVersionAttribute : System.Attribute
    {
        public AssemblyInformationalVersionAttribute(string informationalVersion) { }
        public string InformationalVersion { get { return default(string); } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(1), Inherited = false)]
    public sealed partial class AssemblyKeyFileAttribute : System.Attribute
    {
        public AssemblyKeyFileAttribute(string keyFile) { }
        public string KeyFile { get { return default(string); } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(1), Inherited = false)]
    public sealed partial class AssemblyKeyNameAttribute : System.Attribute
    {
        public AssemblyKeyNameAttribute(string keyName) { }
        public string KeyName { get { return default(string); } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(1), AllowMultiple = true, Inherited = false)]
    public sealed partial class AssemblyMetadataAttribute : System.Attribute
    {
        public AssemblyMetadataAttribute(string key, string value) { }
        public string Key { get { return default(string); } }
        public string Value { get { return default(string); } }
    }
    [System.FlagsAttribute]
    public enum AssemblyNameFlags
    {
        None = 0,
        PublicKey = 1,
        Retargetable = 256,
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(1), Inherited = false)]
    public sealed partial class AssemblyProductAttribute : System.Attribute
    {
        public AssemblyProductAttribute(string product) { }
        public string Product { get { return default(string); } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(1), Inherited = false, AllowMultiple = false)]
    public sealed partial class AssemblySignatureKeyAttribute : System.Attribute
    {
        public AssemblySignatureKeyAttribute(string publicKey, string countersignature) { }
        public string Countersignature { get { return default(string); } }
        public string PublicKey { get { return default(string); } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(1), Inherited = false)]
    public sealed partial class AssemblyTitleAttribute : System.Attribute
    {
        public AssemblyTitleAttribute(string title) { }
        public string Title { get { return default(string); } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(1), Inherited = false)]
    public sealed partial class AssemblyTrademarkAttribute : System.Attribute
    {
        public AssemblyTrademarkAttribute(string trademark) { }
        public string Trademark { get { return default(string); } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(1), Inherited = false)]
    public sealed partial class AssemblyVersionAttribute : System.Attribute
    {
        public AssemblyVersionAttribute(string version) { }
        public string Version { get { return default(string); } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(1036))]
    public sealed partial class DefaultMemberAttribute : System.Attribute
    {
        public DefaultMemberAttribute(string memberName) { }
        public string MemberName { get { return default(string); } }
    }
    public enum ProcessorArchitecture
    {
        Amd64 = 4,
        Arm = 5,
        IA64 = 3,
        MSIL = 1,
        None = 0,
        X86 = 2,
    }
}
namespace System.Runtime
{
    public enum GCLargeObjectHeapCompactionMode
    {
        CompactOnce = 2,
        Default = 1,
    }
    public enum GCLatencyMode
    {
        Batch = 0,
        Interactive = 1,
        LowLatency = 2,
        SustainedLowLatency = 3,
    }
    public static partial class GCSettings
    {
        public static bool IsServerGC { get { return default(bool); } }
        public static System.Runtime.GCLargeObjectHeapCompactionMode LargeObjectHeapCompactionMode { get { return default(System.Runtime.GCLargeObjectHeapCompactionMode); }[System.Security.SecurityCriticalAttribute]set { } }
        public static System.Runtime.GCLatencyMode LatencyMode { get { return default(System.Runtime.GCLatencyMode); }[System.Security.SecurityCriticalAttribute]set { } }
    }
}
namespace System.Runtime.CompilerServices
{
    [System.AttributeUsageAttribute((System.AttributeTargets)(256))]
    public sealed partial class AccessedThroughPropertyAttribute : System.Attribute
    {
        public AccessedThroughPropertyAttribute(string propertyName) { }
        public string PropertyName { get { return default(string); } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(64), Inherited = false, AllowMultiple = false)]
    public sealed partial class AsyncStateMachineAttribute : System.Runtime.CompilerServices.StateMachineAttribute
    {
        public AsyncStateMachineAttribute(System.Type stateMachineType) : base(default(System.Type)) { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(2048), Inherited = false)]
    public sealed partial class CallerFilePathAttribute : System.Attribute
    {
        public CallerFilePathAttribute() { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(2048), Inherited = false)]
    public sealed partial class CallerLineNumberAttribute : System.Attribute
    {
        public CallerLineNumberAttribute() { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(2048), Inherited = false)]
    public sealed partial class CallerMemberNameAttribute : System.Attribute
    {
        public CallerMemberNameAttribute() { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(71))]
    public partial class CompilationRelaxationsAttribute : System.Attribute
    {
        public CompilationRelaxationsAttribute(int relaxations) { }
        public int CompilationRelaxations { get { return default(int); } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(32767), Inherited = true)]
    public sealed partial class CompilerGeneratedAttribute : System.Attribute
    {
        public CompilerGeneratedAttribute() { }
    }
    public sealed partial class ConditionalWeakTable<TKey, TValue> where TKey : class where TValue : class
    {
        public ConditionalWeakTable() { }
        public void Add(TKey key, TValue value) { }
        ~ConditionalWeakTable() { }
        public TValue GetOrCreateValue(TKey key) { return default(TValue); }
        public TValue GetValue(TKey key, System.Runtime.CompilerServices.ConditionalWeakTable<TKey, TValue>.CreateValueCallback createValueCallback) { return default(TValue); }
        public bool Remove(TKey key) { return default(bool); }
        public bool TryGetValue(TKey key, out TValue value) { value = default(TValue); return default(bool); }
        public delegate TValue CreateValueCallback(TKey key);
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(2304), Inherited = false)]
    public abstract partial class CustomConstantAttribute : System.Attribute
    {
        protected CustomConstantAttribute() { }
        public abstract object Value { get; }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(2304), Inherited = false)]
    public sealed partial class DateTimeConstantAttribute : System.Runtime.CompilerServices.CustomConstantAttribute
    {
        public DateTimeConstantAttribute(long ticks) { }
        public override object Value { get { return default(object); } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(2304), Inherited = false)]
    public sealed partial class DecimalConstantAttribute : System.Attribute
    {
        public DecimalConstantAttribute(byte scale, byte sign, int hi, int mid, int low) { }
        [System.CLSCompliantAttribute(false)]
        public DecimalConstantAttribute(byte scale, byte sign, uint hi, uint mid, uint low) { }
        public decimal Value { get { return default(decimal); } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(1), AllowMultiple = false, Inherited = false)]
    public sealed partial class DisablePrivateReflectionAttribute : System.Attribute
    {
        public DisablePrivateReflectionAttribute() { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(69))]
    public sealed partial class ExtensionAttribute : System.Attribute
    {
        public ExtensionAttribute() { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(256), Inherited = false)]
    public sealed partial class FixedBufferAttribute : System.Attribute
    {
        public FixedBufferAttribute(System.Type elementType, int length) { }
        public System.Type ElementType { get { return default(System.Type); } }
        public int Length { get { return default(int); } }
    }
    public static partial class FormattableStringFactory
    {
        public static System.FormattableString Create(string format, params object[] arguments) { return default(System.FormattableString); }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(128), Inherited = true)]
    public sealed partial class IndexerNameAttribute : System.Attribute
    {
        public IndexerNameAttribute(string indexerName) { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(1), AllowMultiple = true, Inherited = false)]
    public sealed partial class InternalsVisibleToAttribute : System.Attribute
    {
        public InternalsVisibleToAttribute(string assemblyName) { }
        public string AssemblyName { get { return default(string); } }
    }
    public static partial class IsConst
    {
    }
    public partial interface IStrongBox
    {
        object Value { get; set; }
    }
    public static partial class IsVolatile
    {
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(64), Inherited = false, AllowMultiple = false)]
    public sealed partial class IteratorStateMachineAttribute : System.Runtime.CompilerServices.StateMachineAttribute
    {
        public IteratorStateMachineAttribute(System.Type stateMachineType) : base(default(System.Type)) { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(96), Inherited = false)]
    public sealed partial class MethodImplAttribute : System.Attribute
    {
        public MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions methodImplOptions) { }
        public System.Runtime.CompilerServices.MethodImplOptions Value { get { return default(System.Runtime.CompilerServices.MethodImplOptions); } }
    }
    [System.FlagsAttribute]
    public enum MethodImplOptions
    {
        AggressiveInlining = 256,
        NoInlining = 8,
        NoOptimization = 64,
        PreserveSig = 128,
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(1), AllowMultiple = false)]
    public sealed partial class ReferenceAssemblyAttribute : System.Attribute
    {
        public ReferenceAssemblyAttribute() { }
        public ReferenceAssemblyAttribute(string description) { }
        public string Description { get { return default(string); } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(1), Inherited = false, AllowMultiple = false)]
    public sealed partial class RuntimeCompatibilityAttribute : System.Attribute
    {
        public RuntimeCompatibilityAttribute() { }
        public bool WrapNonExceptionThrows { get { return default(bool); } set { } }
    }
    public static partial class RuntimeHelpers
    {
        public static int OffsetToStringData { get { return default(int); } }
        public static void EnsureSufficientExecutionStack() { }
        public static int GetHashCode(object o) { return default(int); }
        public static object GetObjectValue(object obj) { return default(object); }
        public static void InitializeArray(System.Array array, System.RuntimeFieldHandle fldHandle) { }
        public static void RunClassConstructor(System.RuntimeTypeHandle type) { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(64), Inherited = false, AllowMultiple = false)]
    public partial class StateMachineAttribute : System.Attribute
    {
        public StateMachineAttribute(System.Type stateMachineType) { }
        public System.Type StateMachineType { get { return default(System.Type); } }
    }
    public partial class StrongBox<T> : System.Runtime.CompilerServices.IStrongBox
    {
        public T Value;
        public StrongBox() { }
        public StrongBox(T value) { }
        object System.Runtime.CompilerServices.IStrongBox.Value { get { return default(object); } set { } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(5148), Inherited = false, AllowMultiple = false)]
    public sealed partial class TypeForwardedFromAttribute : System.Attribute
    {
        public TypeForwardedFromAttribute(string assemblyFullName) { }
        public string AssemblyFullName { get { return default(string); } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(1), AllowMultiple = true, Inherited = false)]
    public sealed partial class TypeForwardedToAttribute : System.Attribute
    {
        public TypeForwardedToAttribute(System.Type destination) { }
        public System.Type Destination { get { return default(System.Type); } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(8))]
    public sealed partial class UnsafeValueTypeAttribute : System.Attribute
    {
        public UnsafeValueTypeAttribute() { }
    }
}
namespace System.Runtime.ExceptionServices
{
    public sealed partial class ExceptionDispatchInfo
    {
        internal ExceptionDispatchInfo() { }
        public System.Exception SourceException { get { return default(System.Exception); } }
        public static System.Runtime.ExceptionServices.ExceptionDispatchInfo Capture(System.Exception source) { return default(System.Runtime.ExceptionServices.ExceptionDispatchInfo); }
        public void Throw() { }
    }
}
namespace System.Runtime.InteropServices
{
    public enum CharSet
    {
        Ansi = 2,
        Unicode = 3,
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(5597), Inherited = false)]
    public sealed partial class ComVisibleAttribute : System.Attribute
    {
        public ComVisibleAttribute(bool visibility) { }
        public bool Value { get { return default(bool); } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(256), Inherited = false)]
    public sealed partial class FieldOffsetAttribute : System.Attribute
    {
        public FieldOffsetAttribute(int offset) { }
        public int Value { get { return default(int); } }
    }
    public enum LayoutKind
    {
        Auto = 3,
        Explicit = 2,
        Sequential = 0,
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(2048), Inherited = false)]
    public sealed partial class OutAttribute : System.Attribute
    {
        public OutAttribute() { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(12), Inherited = false)]
    public sealed partial class StructLayoutAttribute : System.Attribute
    {
        public System.Runtime.InteropServices.CharSet CharSet;
        public int Pack;
        public int Size;
        public StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind layoutKind) { }
        public System.Runtime.InteropServices.LayoutKind Value { get { return default(System.Runtime.InteropServices.LayoutKind); } }
    }
}
namespace System.Runtime.Versioning
{
    [System.AttributeUsageAttribute((System.AttributeTargets)(1), AllowMultiple = false, Inherited = false)]
    public sealed partial class TargetFrameworkAttribute : System.Attribute
    {
        public TargetFrameworkAttribute(string frameworkName) { }
        public string FrameworkDisplayName { get { return default(string); } set { } }
        public string FrameworkName { get { return default(string); } }
    }
}
namespace System.Security
{
    [System.AttributeUsageAttribute((System.AttributeTargets)(1), AllowMultiple = false, Inherited = false)]
    public sealed partial class AllowPartiallyTrustedCallersAttribute : System.Attribute
    {
        public AllowPartiallyTrustedCallersAttribute() { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(5501), AllowMultiple = false, Inherited = false)]
    public sealed partial class SecurityCriticalAttribute : System.Attribute
    {
        public SecurityCriticalAttribute() { }
    }
    public partial class SecurityException : System.Exception
    {
        public SecurityException() { }
        public SecurityException(string message) { }
        public SecurityException(string message, System.Exception inner) { }
        public override string ToString() { return default(string); }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(5500), AllowMultiple = false, Inherited = false)]
    public sealed partial class SecuritySafeCriticalAttribute : System.Attribute
    {
        public SecuritySafeCriticalAttribute() { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(1), AllowMultiple = false, Inherited = false)]
    public sealed partial class SecurityTransparentAttribute : System.Attribute
    {
        public SecurityTransparentAttribute() { }
    }
    public partial class VerificationException : System.Exception
    {
        public VerificationException() { }
        public VerificationException(string message) { }
        public VerificationException(string message, System.Exception innerException) { }
    }
}
namespace System.Text
{
    public sealed partial class StringBuilder
    {
        public StringBuilder() { }
        public StringBuilder(int capacity) { }
        public StringBuilder(int capacity, int maxCapacity) { }
        public StringBuilder(string value) { }
        public StringBuilder(string value, int capacity) { }
        public StringBuilder(string value, int startIndex, int length, int capacity) { }
        public int Capacity { get { return default(int); } set { } }
        [System.Runtime.CompilerServices.IndexerName("Chars")]
        public char this[int index] { get { return default(char); } set { } }
        public int Length { get { return default(int); } set { } }
        public int MaxCapacity { get { return default(int); } }
        public System.Text.StringBuilder Append(bool value) { return default(System.Text.StringBuilder); }
        public System.Text.StringBuilder Append(byte value) { return default(System.Text.StringBuilder); }
        public System.Text.StringBuilder Append(char value) { return default(System.Text.StringBuilder); }
        [System.CLSCompliantAttribute(false)]
        [System.Security.SecurityCriticalAttribute]
        public unsafe System.Text.StringBuilder Append(char* value, int valueCount) { return default(System.Text.StringBuilder); }
        public System.Text.StringBuilder Append(char value, int repeatCount) { return default(System.Text.StringBuilder); }
        public System.Text.StringBuilder Append(char[] value) { return default(System.Text.StringBuilder); }
        public System.Text.StringBuilder Append(char[] value, int startIndex, int charCount) { return default(System.Text.StringBuilder); }
        public System.Text.StringBuilder Append(decimal value) { return default(System.Text.StringBuilder); }
        public System.Text.StringBuilder Append(double value) { return default(System.Text.StringBuilder); }
        public System.Text.StringBuilder Append(short value) { return default(System.Text.StringBuilder); }
        public System.Text.StringBuilder Append(int value) { return default(System.Text.StringBuilder); }
        public System.Text.StringBuilder Append(long value) { return default(System.Text.StringBuilder); }
        public System.Text.StringBuilder Append(object value) { return default(System.Text.StringBuilder); }
        [System.CLSCompliantAttribute(false)]
        public System.Text.StringBuilder Append(sbyte value) { return default(System.Text.StringBuilder); }
        public System.Text.StringBuilder Append(float value) { return default(System.Text.StringBuilder); }
        public System.Text.StringBuilder Append(string value) { return default(System.Text.StringBuilder); }
        public System.Text.StringBuilder Append(string value, int startIndex, int count) { return default(System.Text.StringBuilder); }
        [System.CLSCompliantAttribute(false)]
        public System.Text.StringBuilder Append(ushort value) { return default(System.Text.StringBuilder); }
        [System.CLSCompliantAttribute(false)]
        public System.Text.StringBuilder Append(uint value) { return default(System.Text.StringBuilder); }
        [System.CLSCompliantAttribute(false)]
        public System.Text.StringBuilder Append(ulong value) { return default(System.Text.StringBuilder); }
        public System.Text.StringBuilder AppendFormat(System.IFormatProvider provider, string format, object arg0) { return default(System.Text.StringBuilder); }
        public System.Text.StringBuilder AppendFormat(System.IFormatProvider provider, string format, object arg0, object arg1) { return default(System.Text.StringBuilder); }
        public System.Text.StringBuilder AppendFormat(System.IFormatProvider provider, string format, object arg0, object arg1, object arg2) { return default(System.Text.StringBuilder); }
        public System.Text.StringBuilder AppendFormat(System.IFormatProvider provider, string format, params object[] args) { return default(System.Text.StringBuilder); }
        public System.Text.StringBuilder AppendFormat(string format, object arg0) { return default(System.Text.StringBuilder); }
        public System.Text.StringBuilder AppendFormat(string format, object arg0, object arg1) { return default(System.Text.StringBuilder); }
        public System.Text.StringBuilder AppendFormat(string format, object arg0, object arg1, object arg2) { return default(System.Text.StringBuilder); }
        public System.Text.StringBuilder AppendFormat(string format, params object[] args) { return default(System.Text.StringBuilder); }
        public System.Text.StringBuilder AppendLine() { return default(System.Text.StringBuilder); }
        public System.Text.StringBuilder AppendLine(string value) { return default(System.Text.StringBuilder); }
        public System.Text.StringBuilder Clear() { return default(System.Text.StringBuilder); }
        public void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count) { }
        public int EnsureCapacity(int capacity) { return default(int); }
        public bool Equals(System.Text.StringBuilder sb) { return default(bool); }
        public System.Text.StringBuilder Insert(int index, bool value) { return default(System.Text.StringBuilder); }
        public System.Text.StringBuilder Insert(int index, byte value) { return default(System.Text.StringBuilder); }
        public System.Text.StringBuilder Insert(int index, char value) { return default(System.Text.StringBuilder); }
        public System.Text.StringBuilder Insert(int index, char[] value) { return default(System.Text.StringBuilder); }
        public System.Text.StringBuilder Insert(int index, char[] value, int startIndex, int charCount) { return default(System.Text.StringBuilder); }
        public System.Text.StringBuilder Insert(int index, decimal value) { return default(System.Text.StringBuilder); }
        public System.Text.StringBuilder Insert(int index, double value) { return default(System.Text.StringBuilder); }
        public System.Text.StringBuilder Insert(int index, short value) { return default(System.Text.StringBuilder); }
        public System.Text.StringBuilder Insert(int index, int value) { return default(System.Text.StringBuilder); }
        public System.Text.StringBuilder Insert(int index, long value) { return default(System.Text.StringBuilder); }
        public System.Text.StringBuilder Insert(int index, object value) { return default(System.Text.StringBuilder); }
        [System.CLSCompliantAttribute(false)]
        public System.Text.StringBuilder Insert(int index, sbyte value) { return default(System.Text.StringBuilder); }
        public System.Text.StringBuilder Insert(int index, float value) { return default(System.Text.StringBuilder); }
        public System.Text.StringBuilder Insert(int index, string value) { return default(System.Text.StringBuilder); }
        public System.Text.StringBuilder Insert(int index, string value, int count) { return default(System.Text.StringBuilder); }
        [System.CLSCompliantAttribute(false)]
        public System.Text.StringBuilder Insert(int index, ushort value) { return default(System.Text.StringBuilder); }
        [System.CLSCompliantAttribute(false)]
        public System.Text.StringBuilder Insert(int index, uint value) { return default(System.Text.StringBuilder); }
        [System.CLSCompliantAttribute(false)]
        public System.Text.StringBuilder Insert(int index, ulong value) { return default(System.Text.StringBuilder); }
        public System.Text.StringBuilder Remove(int startIndex, int length) { return default(System.Text.StringBuilder); }
        public System.Text.StringBuilder Replace(char oldChar, char newChar) { return default(System.Text.StringBuilder); }
        public System.Text.StringBuilder Replace(char oldChar, char newChar, int startIndex, int count) { return default(System.Text.StringBuilder); }
        public System.Text.StringBuilder Replace(string oldValue, string newValue) { return default(System.Text.StringBuilder); }
        public System.Text.StringBuilder Replace(string oldValue, string newValue, int startIndex, int count) { return default(System.Text.StringBuilder); }
        public override string ToString() { return default(string); }
        public string ToString(int startIndex, int length) { return default(string); }
    }
}
namespace System.Threading
{
    public enum LazyThreadSafetyMode
    {
        ExecutionAndPublication = 2,
        None = 0,
        PublicationOnly = 1,
    }
    public static partial class Timeout
    {
        public const int Infinite = -1;
        public static readonly System.TimeSpan InfiniteTimeSpan;
    }
    public abstract partial class WaitHandle : System.IDisposable
    {
        protected static readonly System.IntPtr InvalidHandle;
        public const int WaitTimeout = 258;
        protected WaitHandle() { }
        public void Dispose() { }
        protected virtual void Dispose(bool explicitDisposing) { }
        public static bool WaitAll(System.Threading.WaitHandle[] waitHandles) { return default(bool); }
        public static bool WaitAll(System.Threading.WaitHandle[] waitHandles, int millisecondsTimeout) { return default(bool); }
        public static bool WaitAll(System.Threading.WaitHandle[] waitHandles, System.TimeSpan timeout) { return default(bool); }
        public static int WaitAny(System.Threading.WaitHandle[] waitHandles) { return default(int); }
        public static int WaitAny(System.Threading.WaitHandle[] waitHandles, int millisecondsTimeout) { return default(int); }
        public static int WaitAny(System.Threading.WaitHandle[] waitHandles, System.TimeSpan timeout) { return default(int); }
        public virtual bool WaitOne() { return default(bool); }
        public virtual bool WaitOne(int millisecondsTimeout) { return default(bool); }
        public virtual bool WaitOne(System.TimeSpan timeout) { return default(bool); }
    }
}
