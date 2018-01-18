// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace Microsoft.Win32.SafeHandles
{
    public abstract partial class CriticalHandleMinusOneIsInvalid : System.Runtime.InteropServices.CriticalHandle
    {
        protected CriticalHandleMinusOneIsInvalid() : base (default(System.IntPtr)) { }
        public override bool IsInvalid { get { throw null; } }
    }
    public abstract partial class CriticalHandleZeroOrMinusOneIsInvalid : System.Runtime.InteropServices.CriticalHandle
    {
        protected CriticalHandleZeroOrMinusOneIsInvalid() : base (default(System.IntPtr)) { }
        public override bool IsInvalid { get { throw null; } }
    }
    public sealed partial class SafeFileHandle : Microsoft.Win32.SafeHandles.SafeHandleZeroOrMinusOneIsInvalid
    {
        public SafeFileHandle(System.IntPtr preexistingHandle, bool ownsHandle) : base (default(bool)) { }
        public override bool IsInvalid { get { throw null; } }
        protected override bool ReleaseHandle() { throw null; }
    }
    public abstract partial class SafeHandleMinusOneIsInvalid : System.Runtime.InteropServices.SafeHandle
    {
        protected SafeHandleMinusOneIsInvalid(bool ownsHandle) : base (default(System.IntPtr), default(bool)) { }
        public override bool IsInvalid { get { throw null; } }
    }
    public abstract partial class SafeHandleZeroOrMinusOneIsInvalid : System.Runtime.InteropServices.SafeHandle
    {
        protected SafeHandleZeroOrMinusOneIsInvalid(bool ownsHandle) : base (default(System.IntPtr), default(bool)) { }
        public override bool IsInvalid { get { throw null; } }
    }
    public sealed partial class SafeWaitHandle : Microsoft.Win32.SafeHandles.SafeHandleZeroOrMinusOneIsInvalid
    {
        public SafeWaitHandle(System.IntPtr existingHandle, bool ownsHandle) : base (default(bool)) { }
        protected override bool ReleaseHandle() { throw null; }
    }
}
namespace System
{
    public partial class AccessViolationException : System.SystemException
    {
        public AccessViolationException() { }
        protected AccessViolationException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public AccessViolationException(string message) { }
        public AccessViolationException(string message, System.Exception innerException) { }
    }
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
        public static object CreateInstance(System.Type type) { throw null; }
        public static object CreateInstance(System.Type type, bool nonPublic) { throw null; }
        public static object CreateInstance(System.Type type, params object[] args) { throw null; }
        public static object CreateInstance(System.Type type, object[] args, object[] activationAttributes) { throw null; }
        public static object CreateInstance(System.Type type, System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder binder, object[] args, System.Globalization.CultureInfo culture) { throw null; }
        public static object CreateInstance(System.Type type, System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder binder, object[] args, System.Globalization.CultureInfo culture, object[] activationAttributes) { throw null; }
        public static T CreateInstance<T>() { throw null; }
    }
    public partial class AggregateException : System.Exception
    {
        public AggregateException() { }
        public AggregateException(System.Collections.Generic.IEnumerable<System.Exception> innerExceptions) { }
        public AggregateException(params System.Exception[] innerExceptions) { }
        protected AggregateException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public AggregateException(string message) { }
        public AggregateException(string message, System.Collections.Generic.IEnumerable<System.Exception> innerExceptions) { }
        public AggregateException(string message, System.Exception innerException) { }
        public AggregateException(string message, params System.Exception[] innerExceptions) { }
        public System.Collections.ObjectModel.ReadOnlyCollection<System.Exception> InnerExceptions { get { throw null; } }
        public override string Message { get { throw null; } }
        public System.AggregateException Flatten() { throw null; }
        public override System.Exception GetBaseException() { throw null; }
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public void Handle(System.Func<System.Exception, bool> predicate) { }
        public override string ToString() { throw null; }
    }
    public static partial class AppContext
    {
        public static string BaseDirectory { get { throw null; } }
        public static string TargetFrameworkName { get { throw null; } }
        public static object GetData(string name) { throw null; }
        public static void SetSwitch(string switchName, bool isEnabled) { }
        public static bool TryGetSwitch(string switchName, out bool isEnabled) { throw null; }
    }
    public partial class ApplicationException : System.Exception
    {
        public ApplicationException() { }
        protected ApplicationException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public ApplicationException(string message) { }
        public ApplicationException(string message, System.Exception innerException) { }
    }
    public partial class ArgumentException : System.SystemException
    {
        public ArgumentException() { }
        protected ArgumentException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public ArgumentException(string message) { }
        public ArgumentException(string message, System.Exception innerException) { }
        public ArgumentException(string message, string paramName) { }
        public ArgumentException(string message, string paramName, System.Exception innerException) { }
        public override string Message { get { throw null; } }
        public virtual string ParamName { get { throw null; } }
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    }
    public partial class ArgumentNullException : System.ArgumentException
    {
        public ArgumentNullException() { }
        protected ArgumentNullException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public ArgumentNullException(string paramName) { }
        public ArgumentNullException(string message, System.Exception innerException) { }
        public ArgumentNullException(string paramName, string message) { }
    }
    public partial class ArgumentOutOfRangeException : System.ArgumentException
    {
        public ArgumentOutOfRangeException() { }
        protected ArgumentOutOfRangeException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public ArgumentOutOfRangeException(string paramName) { }
        public ArgumentOutOfRangeException(string message, System.Exception innerException) { }
        public ArgumentOutOfRangeException(string paramName, object actualValue, string message) { }
        public ArgumentOutOfRangeException(string paramName, string message) { }
        public virtual object ActualValue { get { throw null; } }
        public override string Message { get { throw null; } }
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    }
    public partial class ArithmeticException : System.SystemException
    {
        public ArithmeticException() { }
        protected ArithmeticException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public ArithmeticException(string message) { }
        public ArithmeticException(string message, System.Exception innerException) { }
    }
    public abstract partial class Array : System.Collections.ICollection, System.Collections.IEnumerable, System.Collections.IList, System.Collections.IStructuralComparable, System.Collections.IStructuralEquatable, System.ICloneable
    {
        internal Array() { }
        public bool IsFixedSize { get { throw null; } }
        public bool IsReadOnly { get { throw null; } }
        public bool IsSynchronized { get { throw null; } }
        public int Length { get { throw null; } }
        public long LongLength { get { throw null; } }
        public int Rank { get { throw null; } }
        public object SyncRoot { get { throw null; } }
        int System.Collections.ICollection.Count { get { throw null; } }
        object System.Collections.IList.this[int index] { get { throw null; } set { } }
        public static System.Collections.ObjectModel.ReadOnlyCollection<T> AsReadOnly<T>(T[] array) { throw null; }
        public static int BinarySearch(System.Array array, int index, int length, object value) { throw null; }
        public static int BinarySearch(System.Array array, int index, int length, object value, System.Collections.IComparer comparer) { throw null; }
        public static int BinarySearch(System.Array array, object value) { throw null; }
        public static int BinarySearch(System.Array array, object value, System.Collections.IComparer comparer) { throw null; }
        public static int BinarySearch<T>(T[] array, int index, int length, T value) { throw null; }
        public static int BinarySearch<T>(T[] array, int index, int length, T value, System.Collections.Generic.IComparer<T> comparer) { throw null; }
        public static int BinarySearch<T>(T[] array, T value) { throw null; }
        public static int BinarySearch<T>(T[] array, T value, System.Collections.Generic.IComparer<T> comparer) { throw null; }
        public static void Clear(System.Array array, int index, int length) { }
        public object Clone() { throw null; }
        public static void ConstrainedCopy(System.Array sourceArray, int sourceIndex, System.Array destinationArray, int destinationIndex, int length) { }
        public static TOutput[] ConvertAll<TInput, TOutput>(TInput[] array, System.Converter<TInput, TOutput> converter) { throw null; }
        public static void Copy(System.Array sourceArray, System.Array destinationArray, int length) { }
        public static void Copy(System.Array sourceArray, System.Array destinationArray, long length) { }
        public static void Copy(System.Array sourceArray, int sourceIndex, System.Array destinationArray, int destinationIndex, int length) { }
        public static void Copy(System.Array sourceArray, long sourceIndex, System.Array destinationArray, long destinationIndex, long length) { }
        public void CopyTo(System.Array array, int index) { }
        public void CopyTo(System.Array array, long index) { }
        public static System.Array CreateInstance(System.Type elementType, int length) { throw null; }
        public static System.Array CreateInstance(System.Type elementType, int length1, int length2) { throw null; }
        public static System.Array CreateInstance(System.Type elementType, int length1, int length2, int length3) { throw null; }
        public static System.Array CreateInstance(System.Type elementType, params int[] lengths) { throw null; }
        public static System.Array CreateInstance(System.Type elementType, int[] lengths, int[] lowerBounds) { throw null; }
        public static System.Array CreateInstance(System.Type elementType, params long[] lengths) { throw null; }
        public static T[] Empty<T>() { throw null; }
        public static bool Exists<T>(T[] array, System.Predicate<T> match) { throw null; }
        public static void Fill<T>(T[] array, T value) { }
        public static void Fill<T>(T[] array, T value, int startIndex, int count) { }
        public static T[] FindAll<T>(T[] array, System.Predicate<T> match) { throw null; }
        public static int FindIndex<T>(T[] array, int startIndex, int count, System.Predicate<T> match) { throw null; }
        public static int FindIndex<T>(T[] array, int startIndex, System.Predicate<T> match) { throw null; }
        public static int FindIndex<T>(T[] array, System.Predicate<T> match) { throw null; }
        public static int FindLastIndex<T>(T[] array, int startIndex, int count, System.Predicate<T> match) { throw null; }
        public static int FindLastIndex<T>(T[] array, int startIndex, System.Predicate<T> match) { throw null; }
        public static int FindLastIndex<T>(T[] array, System.Predicate<T> match) { throw null; }
        public static T FindLast<T>(T[] array, System.Predicate<T> match) { throw null; }
        public static T Find<T>(T[] array, System.Predicate<T> match) { throw null; }
        public static void ForEach<T>(T[] array, System.Action<T> action) { }
        public System.Collections.IEnumerator GetEnumerator() { throw null; }
        public int GetLength(int dimension) { throw null; }
        public long GetLongLength(int dimension) { throw null; }
        public int GetLowerBound(int dimension) { throw null; }
        public int GetUpperBound(int dimension) { throw null; }
        public object GetValue(int index) { throw null; }
        public object GetValue(int index1, int index2) { throw null; }
        public object GetValue(int index1, int index2, int index3) { throw null; }
        public object GetValue(params int[] indices) { throw null; }
        public object GetValue(long index) { throw null; }
        public object GetValue(long index1, long index2) { throw null; }
        public object GetValue(long index1, long index2, long index3) { throw null; }
        public object GetValue(params long[] indices) { throw null; }
        public static int IndexOf(System.Array array, object value) { throw null; }
        public static int IndexOf(System.Array array, object value, int startIndex) { throw null; }
        public static int IndexOf(System.Array array, object value, int startIndex, int count) { throw null; }
        public static int IndexOf<T>(T[] array, T value) { throw null; }
        public static int IndexOf<T>(T[] array, T value, int startIndex) { throw null; }
        public static int IndexOf<T>(T[] array, T value, int startIndex, int count) { throw null; }
        public void Initialize() { }
        public static int LastIndexOf(System.Array array, object value) { throw null; }
        public static int LastIndexOf(System.Array array, object value, int startIndex) { throw null; }
        public static int LastIndexOf(System.Array array, object value, int startIndex, int count) { throw null; }
        public static int LastIndexOf<T>(T[] array, T value) { throw null; }
        public static int LastIndexOf<T>(T[] array, T value, int startIndex) { throw null; }
        public static int LastIndexOf<T>(T[] array, T value, int startIndex, int count) { throw null; }
        public static void Resize<T>(ref T[] array, int newSize) { }
        public static void Reverse(System.Array array) { }
        public static void Reverse(System.Array array, int index, int length) { }
        public static void Reverse<T>(T[] array) { }
        public static void Reverse<T>(T[] array, int index, int length) { }
        public void SetValue(object value, int index) { }
        public void SetValue(object value, int index1, int index2) { }
        public void SetValue(object value, int index1, int index2, int index3) { }
        public void SetValue(object value, params int[] indices) { }
        public void SetValue(object value, long index) { }
        public void SetValue(object value, long index1, long index2) { }
        public void SetValue(object value, long index1, long index2, long index3) { }
        public void SetValue(object value, params long[] indices) { }
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
        int System.Collections.IList.Add(object value) { throw null; }
        void System.Collections.IList.Clear() { }
        bool System.Collections.IList.Contains(object value) { throw null; }
        int System.Collections.IList.IndexOf(object value) { throw null; }
        void System.Collections.IList.Insert(int index, object value) { }
        void System.Collections.IList.Remove(object value) { }
        void System.Collections.IList.RemoveAt(int index) { }
        int System.Collections.IStructuralComparable.CompareTo(object other, System.Collections.IComparer comparer) { throw null; }
        bool System.Collections.IStructuralEquatable.Equals(object other, System.Collections.IEqualityComparer comparer) { throw null; }
        int System.Collections.IStructuralEquatable.GetHashCode(System.Collections.IEqualityComparer comparer) { throw null; }
        public static bool TrueForAll<T>(T[] array, System.Predicate<T> match) { throw null; }
    }
    public readonly partial struct ArraySegment<T> : System.Collections.Generic.ICollection<T>, System.Collections.Generic.IEnumerable<T>, System.Collections.Generic.IList<T>, System.Collections.Generic.IReadOnlyCollection<T>, System.Collections.Generic.IReadOnlyList<T>, System.Collections.IEnumerable
    {
        private readonly T[] _array;
        public ArraySegment(T[] array) { throw null; }
        public ArraySegment(T[] array, int offset, int count) { throw null; }
        public T[] Array { get { throw null; } }
        public int Count { get { throw null; } }
        public static System.ArraySegment<T> Empty { get { throw null; } }
        public T this[int index] { get { throw null; } set { } }
        public int Offset { get { throw null; } }
        bool System.Collections.Generic.ICollection<T>.IsReadOnly { get { throw null; } }
        T System.Collections.Generic.IList<T>.this[int index] { get { throw null; } set { } }
        T System.Collections.Generic.IReadOnlyList<T>.this[int index] { get { throw null; } }
        public void CopyTo(System.ArraySegment<T> destination) { }
        public void CopyTo(T[] destination) { }
        public void CopyTo(T[] destination, int destinationIndex) { }
        public bool Equals(System.ArraySegment<T> obj) { throw null; }
        public override bool Equals(object obj) { throw null; }
        public System.ArraySegment<T>.Enumerator GetEnumerator() { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.ArraySegment<T> a, System.ArraySegment<T> b) { throw null; }
        public static implicit operator System.ArraySegment<T> (T[] array) { throw null; }
        public static bool operator !=(System.ArraySegment<T> a, System.ArraySegment<T> b) { throw null; }
        public System.ArraySegment<T> Slice(int index) { throw null; }
        public System.ArraySegment<T> Slice(int index, int count) { throw null; }
        void System.Collections.Generic.ICollection<T>.Add(T item) { }
        void System.Collections.Generic.ICollection<T>.Clear() { }
        bool System.Collections.Generic.ICollection<T>.Contains(T item) { throw null; }
        bool System.Collections.Generic.ICollection<T>.Remove(T item) { throw null; }
        System.Collections.Generic.IEnumerator<T> System.Collections.Generic.IEnumerable<T>.GetEnumerator() { throw null; }
        int System.Collections.Generic.IList<T>.IndexOf(T item) { throw null; }
        void System.Collections.Generic.IList<T>.Insert(int index, T item) { }
        void System.Collections.Generic.IList<T>.RemoveAt(int index) { }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        public T[] ToArray() { throw null; }
        public partial struct Enumerator : System.Collections.Generic.IEnumerator<T>, System.Collections.IEnumerator, System.IDisposable
        {
            private readonly T[] _array;
            public T Current { get { throw null; } }
            object System.Collections.IEnumerator.Current { get { throw null; } }
            public void Dispose() { }
            public bool MoveNext() { throw null; }
            void System.Collections.IEnumerator.Reset() { }
        }
    }
    public partial class ArrayTypeMismatchException : System.SystemException
    {
        public ArrayTypeMismatchException() { }
        protected ArrayTypeMismatchException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public ArrayTypeMismatchException(string message) { }
        public ArrayTypeMismatchException(string message, System.Exception innerException) { }
    }
    public delegate void AsyncCallback(System.IAsyncResult ar);
    [System.AttributeUsageAttribute((System.AttributeTargets)(32767), Inherited=true, AllowMultiple=false)]
    public abstract partial class Attribute
    {
        protected Attribute() { }
        public virtual object TypeId { get { throw null; } }
        public override bool Equals(object obj) { throw null; }
        public static System.Attribute GetCustomAttribute(System.Reflection.Assembly element, System.Type attributeType) { throw null; }
        public static System.Attribute GetCustomAttribute(System.Reflection.Assembly element, System.Type attributeType, bool inherit) { throw null; }
        public static System.Attribute GetCustomAttribute(System.Reflection.MemberInfo element, System.Type attributeType) { throw null; }
        public static System.Attribute GetCustomAttribute(System.Reflection.MemberInfo element, System.Type attributeType, bool inherit) { throw null; }
        public static System.Attribute GetCustomAttribute(System.Reflection.Module element, System.Type attributeType) { throw null; }
        public static System.Attribute GetCustomAttribute(System.Reflection.Module element, System.Type attributeType, bool inherit) { throw null; }
        public static System.Attribute GetCustomAttribute(System.Reflection.ParameterInfo element, System.Type attributeType) { throw null; }
        public static System.Attribute GetCustomAttribute(System.Reflection.ParameterInfo element, System.Type attributeType, bool inherit) { throw null; }
        public static System.Attribute[] GetCustomAttributes(System.Reflection.Assembly element) { throw null; }
        public static System.Attribute[] GetCustomAttributes(System.Reflection.Assembly element, bool inherit) { throw null; }
        public static System.Attribute[] GetCustomAttributes(System.Reflection.Assembly element, System.Type attributeType) { throw null; }
        public static System.Attribute[] GetCustomAttributes(System.Reflection.Assembly element, System.Type attributeType, bool inherit) { throw null; }
        public static System.Attribute[] GetCustomAttributes(System.Reflection.MemberInfo element) { throw null; }
        public static System.Attribute[] GetCustomAttributes(System.Reflection.MemberInfo element, bool inherit) { throw null; }
        public static System.Attribute[] GetCustomAttributes(System.Reflection.MemberInfo element, System.Type type) { throw null; }
        public static System.Attribute[] GetCustomAttributes(System.Reflection.MemberInfo element, System.Type type, bool inherit) { throw null; }
        public static System.Attribute[] GetCustomAttributes(System.Reflection.Module element) { throw null; }
        public static System.Attribute[] GetCustomAttributes(System.Reflection.Module element, bool inherit) { throw null; }
        public static System.Attribute[] GetCustomAttributes(System.Reflection.Module element, System.Type attributeType) { throw null; }
        public static System.Attribute[] GetCustomAttributes(System.Reflection.Module element, System.Type attributeType, bool inherit) { throw null; }
        public static System.Attribute[] GetCustomAttributes(System.Reflection.ParameterInfo element) { throw null; }
        public static System.Attribute[] GetCustomAttributes(System.Reflection.ParameterInfo element, bool inherit) { throw null; }
        public static System.Attribute[] GetCustomAttributes(System.Reflection.ParameterInfo element, System.Type attributeType) { throw null; }
        public static System.Attribute[] GetCustomAttributes(System.Reflection.ParameterInfo element, System.Type attributeType, bool inherit) { throw null; }
        public override int GetHashCode() { throw null; }
        public virtual bool IsDefaultAttribute() { throw null; }
        public static bool IsDefined(System.Reflection.Assembly element, System.Type attributeType) { throw null; }
        public static bool IsDefined(System.Reflection.Assembly element, System.Type attributeType, bool inherit) { throw null; }
        public static bool IsDefined(System.Reflection.MemberInfo element, System.Type attributeType) { throw null; }
        public static bool IsDefined(System.Reflection.MemberInfo element, System.Type attributeType, bool inherit) { throw null; }
        public static bool IsDefined(System.Reflection.Module element, System.Type attributeType) { throw null; }
        public static bool IsDefined(System.Reflection.Module element, System.Type attributeType, bool inherit) { throw null; }
        public static bool IsDefined(System.Reflection.ParameterInfo element, System.Type attributeType) { throw null; }
        public static bool IsDefined(System.Reflection.ParameterInfo element, System.Type attributeType, bool inherit) { throw null; }
        public virtual bool Match(object obj) { throw null; }
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
    [System.AttributeUsageAttribute((System.AttributeTargets)(4), Inherited=true)]
    public sealed partial class AttributeUsageAttribute : System.Attribute
    {
        public AttributeUsageAttribute(System.AttributeTargets validOn) { }
        public bool AllowMultiple { get { throw null; } set { } }
        public bool Inherited { get { throw null; } set { } }
        public System.AttributeTargets ValidOn { get { throw null; } }
    }
    public partial class BadImageFormatException : System.SystemException
    {
        public BadImageFormatException() { }
        protected BadImageFormatException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public BadImageFormatException(string message) { }
        public BadImageFormatException(string message, System.Exception inner) { }
        public BadImageFormatException(string message, string fileName) { }
        public BadImageFormatException(string message, string fileName, System.Exception inner) { }
        public string FileName { get { throw null; } }
        public string FusionLog { get { throw null; } }
        public override string Message { get { throw null; } }
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public override string ToString() { throw null; }
    }
    public partial struct Boolean : System.IComparable, System.IComparable<bool>, System.IConvertible, System.IEquatable<bool>
    {
        private bool _dummy;
        public static readonly string FalseString;
        public static readonly string TrueString;
        public int CompareTo(System.Boolean value) { throw null; }
        public int CompareTo(object obj) { throw null; }
        public System.Boolean Equals(System.Boolean obj) { throw null; }
        public override System.Boolean Equals(object obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public System.TypeCode GetTypeCode() { throw null; }
        public static System.Boolean Parse(System.ReadOnlySpan<char> value) { throw null; }
        public static System.Boolean Parse(string value) { throw null; }
        System.Boolean System.IConvertible.ToBoolean(System.IFormatProvider provider) { throw null; }
        byte System.IConvertible.ToByte(System.IFormatProvider provider) { throw null; }
        char System.IConvertible.ToChar(System.IFormatProvider provider) { throw null; }
        System.DateTime System.IConvertible.ToDateTime(System.IFormatProvider provider) { throw null; }
        decimal System.IConvertible.ToDecimal(System.IFormatProvider provider) { throw null; }
        double System.IConvertible.ToDouble(System.IFormatProvider provider) { throw null; }
        short System.IConvertible.ToInt16(System.IFormatProvider provider) { throw null; }
        int System.IConvertible.ToInt32(System.IFormatProvider provider) { throw null; }
        long System.IConvertible.ToInt64(System.IFormatProvider provider) { throw null; }
        sbyte System.IConvertible.ToSByte(System.IFormatProvider provider) { throw null; }
        float System.IConvertible.ToSingle(System.IFormatProvider provider) { throw null; }
        object System.IConvertible.ToType(System.Type type, System.IFormatProvider provider) { throw null; }
        ushort System.IConvertible.ToUInt16(System.IFormatProvider provider) { throw null; }
        uint System.IConvertible.ToUInt32(System.IFormatProvider provider) { throw null; }
        ulong System.IConvertible.ToUInt64(System.IFormatProvider provider) { throw null; }
        public override string ToString() { throw null; }
        public string ToString(System.IFormatProvider provider) { throw null; }
        public System.Boolean TryFormat(System.Span<char> destination, out int charsWritten) { throw null; }
        public static System.Boolean TryParse(System.ReadOnlySpan<char> value, out System.Boolean result) { throw null; }
        public static System.Boolean TryParse(string value, out System.Boolean result) { throw null; }
    }
    public static partial class Buffer
    {
        public static void BlockCopy(System.Array src, int srcOffset, System.Array dst, int dstOffset, int count) { }
        public static int ByteLength(System.Array array) { throw null; }
        public static byte GetByte(System.Array array, int index) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public unsafe static void MemoryCopy(void* source, void* destination, long destinationSizeInBytes, long sourceBytesToCopy) { }
        [System.CLSCompliantAttribute(false)]
        public unsafe static void MemoryCopy(void* source, void* destination, ulong destinationSizeInBytes, ulong sourceBytesToCopy) { }
        public static void SetByte(System.Array array, int index, byte value) { }
    }
    public partial struct Byte : System.IComparable, System.IComparable<byte>, System.IConvertible, System.IEquatable<byte>, System.IFormattable
    {
        private byte _dummy;
        public const byte MaxValue = (byte)255;
        public const byte MinValue = (byte)0;
        public int CompareTo(System.Byte value) { throw null; }
        public int CompareTo(object value) { throw null; }
        public bool Equals(System.Byte obj) { throw null; }
        public override bool Equals(object obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public System.TypeCode GetTypeCode() { throw null; }
        public static System.Byte Parse(System.ReadOnlySpan<char> s, System.Globalization.NumberStyles style=(System.Globalization.NumberStyles)(7), System.IFormatProvider provider=null) { throw null; }
        public static System.Byte Parse(string s) { throw null; }
        public static System.Byte Parse(string s, System.Globalization.NumberStyles style) { throw null; }
        public static System.Byte Parse(string s, System.Globalization.NumberStyles style, System.IFormatProvider provider) { throw null; }
        public static System.Byte Parse(string s, System.IFormatProvider provider) { throw null; }
        bool System.IConvertible.ToBoolean(System.IFormatProvider provider) { throw null; }
        System.Byte System.IConvertible.ToByte(System.IFormatProvider provider) { throw null; }
        char System.IConvertible.ToChar(System.IFormatProvider provider) { throw null; }
        System.DateTime System.IConvertible.ToDateTime(System.IFormatProvider provider) { throw null; }
        decimal System.IConvertible.ToDecimal(System.IFormatProvider provider) { throw null; }
        double System.IConvertible.ToDouble(System.IFormatProvider provider) { throw null; }
        short System.IConvertible.ToInt16(System.IFormatProvider provider) { throw null; }
        int System.IConvertible.ToInt32(System.IFormatProvider provider) { throw null; }
        long System.IConvertible.ToInt64(System.IFormatProvider provider) { throw null; }
        sbyte System.IConvertible.ToSByte(System.IFormatProvider provider) { throw null; }
        float System.IConvertible.ToSingle(System.IFormatProvider provider) { throw null; }
        object System.IConvertible.ToType(System.Type type, System.IFormatProvider provider) { throw null; }
        ushort System.IConvertible.ToUInt16(System.IFormatProvider provider) { throw null; }
        uint System.IConvertible.ToUInt32(System.IFormatProvider provider) { throw null; }
        ulong System.IConvertible.ToUInt64(System.IFormatProvider provider) { throw null; }
        public override string ToString() { throw null; }
        public string ToString(System.IFormatProvider provider) { throw null; }
        public string ToString(string format) { throw null; }
        public string ToString(string format, System.IFormatProvider provider) { throw null; }
        public bool TryFormat(System.Span<char> destination, out int charsWritten, System.ReadOnlySpan<char> format=default(System.ReadOnlySpan<char>), System.IFormatProvider provider=null) { throw null; }
        public static bool TryParse(System.ReadOnlySpan<char> s, out System.Byte result) { throw null; }
        public static bool TryParse(System.ReadOnlySpan<char> s, System.Globalization.NumberStyles style, System.IFormatProvider provider, out System.Byte result) { throw null; }
        public static bool TryParse(string s, out System.Byte result) { throw null; }
        public static bool TryParse(string s, System.Globalization.NumberStyles style, System.IFormatProvider provider, out System.Byte result) { throw null; }
    }
    public partial struct Char : System.IComparable, System.IComparable<char>, System.IConvertible, System.IEquatable<char>
    {
        private char _dummy;
        public const char MaxValue = '\uFFFF';
        public const char MinValue = '\0';
        public int CompareTo(System.Char value) { throw null; }
        public int CompareTo(object value) { throw null; }
        public static string ConvertFromUtf32(int utf32) { throw null; }
        public static int ConvertToUtf32(System.Char highSurrogate, System.Char lowSurrogate) { throw null; }
        public static int ConvertToUtf32(string s, int index) { throw null; }
        public bool Equals(System.Char obj) { throw null; }
        public override bool Equals(object obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public static double GetNumericValue(System.Char c) { throw null; }
        public static double GetNumericValue(string s, int index) { throw null; }
        public System.TypeCode GetTypeCode() { throw null; }
        public static System.Globalization.UnicodeCategory GetUnicodeCategory(System.Char c) { throw null; }
        public static System.Globalization.UnicodeCategory GetUnicodeCategory(string s, int index) { throw null; }
        public static bool IsControl(System.Char c) { throw null; }
        public static bool IsControl(string s, int index) { throw null; }
        public static bool IsDigit(System.Char c) { throw null; }
        public static bool IsDigit(string s, int index) { throw null; }
        public static bool IsHighSurrogate(System.Char c) { throw null; }
        public static bool IsHighSurrogate(string s, int index) { throw null; }
        public static bool IsLetter(System.Char c) { throw null; }
        public static bool IsLetter(string s, int index) { throw null; }
        public static bool IsLetterOrDigit(System.Char c) { throw null; }
        public static bool IsLetterOrDigit(string s, int index) { throw null; }
        public static bool IsLower(System.Char c) { throw null; }
        public static bool IsLower(string s, int index) { throw null; }
        public static bool IsLowSurrogate(System.Char c) { throw null; }
        public static bool IsLowSurrogate(string s, int index) { throw null; }
        public static bool IsNumber(System.Char c) { throw null; }
        public static bool IsNumber(string s, int index) { throw null; }
        public static bool IsPunctuation(System.Char c) { throw null; }
        public static bool IsPunctuation(string s, int index) { throw null; }
        public static bool IsSeparator(System.Char c) { throw null; }
        public static bool IsSeparator(string s, int index) { throw null; }
        public static bool IsSurrogate(System.Char c) { throw null; }
        public static bool IsSurrogate(string s, int index) { throw null; }
        public static bool IsSurrogatePair(System.Char highSurrogate, System.Char lowSurrogate) { throw null; }
        public static bool IsSurrogatePair(string s, int index) { throw null; }
        public static bool IsSymbol(System.Char c) { throw null; }
        public static bool IsSymbol(string s, int index) { throw null; }
        public static bool IsUpper(System.Char c) { throw null; }
        public static bool IsUpper(string s, int index) { throw null; }
        public static bool IsWhiteSpace(System.Char c) { throw null; }
        public static bool IsWhiteSpace(string s, int index) { throw null; }
        public static System.Char Parse(string s) { throw null; }
        bool System.IConvertible.ToBoolean(System.IFormatProvider provider) { throw null; }
        byte System.IConvertible.ToByte(System.IFormatProvider provider) { throw null; }
        System.Char System.IConvertible.ToChar(System.IFormatProvider provider) { throw null; }
        System.DateTime System.IConvertible.ToDateTime(System.IFormatProvider provider) { throw null; }
        decimal System.IConvertible.ToDecimal(System.IFormatProvider provider) { throw null; }
        double System.IConvertible.ToDouble(System.IFormatProvider provider) { throw null; }
        short System.IConvertible.ToInt16(System.IFormatProvider provider) { throw null; }
        int System.IConvertible.ToInt32(System.IFormatProvider provider) { throw null; }
        long System.IConvertible.ToInt64(System.IFormatProvider provider) { throw null; }
        sbyte System.IConvertible.ToSByte(System.IFormatProvider provider) { throw null; }
        float System.IConvertible.ToSingle(System.IFormatProvider provider) { throw null; }
        object System.IConvertible.ToType(System.Type type, System.IFormatProvider provider) { throw null; }
        ushort System.IConvertible.ToUInt16(System.IFormatProvider provider) { throw null; }
        uint System.IConvertible.ToUInt32(System.IFormatProvider provider) { throw null; }
        ulong System.IConvertible.ToUInt64(System.IFormatProvider provider) { throw null; }
        public static System.Char ToLower(System.Char c) { throw null; }
        public static System.Char ToLower(System.Char c, System.Globalization.CultureInfo culture) { throw null; }
        public static System.Char ToLowerInvariant(System.Char c) { throw null; }
        public override string ToString() { throw null; }
        public static string ToString(System.Char c) { throw null; }
        public string ToString(System.IFormatProvider provider) { throw null; }
        public static System.Char ToUpper(System.Char c) { throw null; }
        public static System.Char ToUpper(System.Char c, System.Globalization.CultureInfo culture) { throw null; }
        public static System.Char ToUpperInvariant(System.Char c) { throw null; }
        public static bool TryParse(string s, out System.Char result) { throw null; }
    }
    public sealed partial class CharEnumerator : System.Collections.Generic.IEnumerator<char>, System.Collections.IEnumerator, System.ICloneable, System.IDisposable
    {
        internal CharEnumerator() { }
        public char Current { get { throw null; } }
        object System.Collections.IEnumerator.Current { get { throw null; } }
        public object Clone() { throw null; }
        public void Dispose() { }
        public bool MoveNext() { throw null; }
        public void Reset() { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(32767), Inherited=true, AllowMultiple=false)]
    public sealed partial class CLSCompliantAttribute : System.Attribute
    {
        public CLSCompliantAttribute(bool isCompliant) { }
        public bool IsCompliant { get { throw null; } }
    }
    public delegate int Comparison<in T>(T x, T y);
    public delegate TOutput Converter<in TInput, out TOutput>(TInput input);
    public readonly partial struct DateTime : System.IComparable, System.IComparable<System.DateTime>, System.IConvertible, System.IEquatable<System.DateTime>, System.IFormattable, System.Runtime.Serialization.ISerializable
    {
        private readonly int _dummy;
        public static readonly System.DateTime MaxValue;
        public static readonly System.DateTime MinValue;
        public static readonly System.DateTime UnixEpoch;
        public DateTime(int year, int month, int day) { throw null; }
        public DateTime(int year, int month, int day, System.Globalization.Calendar calendar) { throw null; }
        public DateTime(int year, int month, int day, int hour, int minute, int second) { throw null; }
        public DateTime(int year, int month, int day, int hour, int minute, int second, System.DateTimeKind kind) { throw null; }
        public DateTime(int year, int month, int day, int hour, int minute, int second, System.Globalization.Calendar calendar) { throw null; }
        public DateTime(int year, int month, int day, int hour, int minute, int second, int millisecond) { throw null; }
        public DateTime(int year, int month, int day, int hour, int minute, int second, int millisecond, System.DateTimeKind kind) { throw null; }
        public DateTime(int year, int month, int day, int hour, int minute, int second, int millisecond, System.Globalization.Calendar calendar) { throw null; }
        public DateTime(int year, int month, int day, int hour, int minute, int second, int millisecond, System.Globalization.Calendar calendar, System.DateTimeKind kind) { throw null; }
        public DateTime(long ticks) { throw null; }
        public DateTime(long ticks, System.DateTimeKind kind) { throw null; }
        public System.DateTime Date { get { throw null; } }
        public int Day { get { throw null; } }
        public System.DayOfWeek DayOfWeek { get { throw null; } }
        public int DayOfYear { get { throw null; } }
        public int Hour { get { throw null; } }
        public System.DateTimeKind Kind { get { throw null; } }
        public int Millisecond { get { throw null; } }
        public int Minute { get { throw null; } }
        public int Month { get { throw null; } }
        public static System.DateTime Now { get { throw null; } }
        public int Second { get { throw null; } }
        public long Ticks { get { throw null; } }
        public System.TimeSpan TimeOfDay { get { throw null; } }
        public static System.DateTime Today { get { throw null; } }
        public static System.DateTime UtcNow { get { throw null; } }
        public int Year { get { throw null; } }
        public System.DateTime Add(System.TimeSpan value) { throw null; }
        public System.DateTime AddDays(double value) { throw null; }
        public System.DateTime AddHours(double value) { throw null; }
        public System.DateTime AddMilliseconds(double value) { throw null; }
        public System.DateTime AddMinutes(double value) { throw null; }
        public System.DateTime AddMonths(int months) { throw null; }
        public System.DateTime AddSeconds(double value) { throw null; }
        public System.DateTime AddTicks(long value) { throw null; }
        public System.DateTime AddYears(int value) { throw null; }
        public static int Compare(System.DateTime t1, System.DateTime t2) { throw null; }
        public int CompareTo(System.DateTime value) { throw null; }
        public int CompareTo(object value) { throw null; }
        public static int DaysInMonth(int year, int month) { throw null; }
        public bool Equals(System.DateTime value) { throw null; }
        public static bool Equals(System.DateTime t1, System.DateTime t2) { throw null; }
        public override bool Equals(object value) { throw null; }
        public static System.DateTime FromBinary(long dateData) { throw null; }
        public static System.DateTime FromFileTime(long fileTime) { throw null; }
        public static System.DateTime FromFileTimeUtc(long fileTime) { throw null; }
        public static System.DateTime FromOADate(double d) { throw null; }
        public string[] GetDateTimeFormats() { throw null; }
        public string[] GetDateTimeFormats(char format) { throw null; }
        public string[] GetDateTimeFormats(char format, System.IFormatProvider provider) { throw null; }
        public string[] GetDateTimeFormats(System.IFormatProvider provider) { throw null; }
        public override int GetHashCode() { throw null; }
        public System.TypeCode GetTypeCode() { throw null; }
        public bool IsDaylightSavingTime() { throw null; }
        public static bool IsLeapYear(int year) { throw null; }
        public static System.DateTime operator +(System.DateTime d, System.TimeSpan t) { throw null; }
        public static bool operator ==(System.DateTime d1, System.DateTime d2) { throw null; }
        public static bool operator >(System.DateTime t1, System.DateTime t2) { throw null; }
        public static bool operator >=(System.DateTime t1, System.DateTime t2) { throw null; }
        public static bool operator !=(System.DateTime d1, System.DateTime d2) { throw null; }
        public static bool operator <(System.DateTime t1, System.DateTime t2) { throw null; }
        public static bool operator <=(System.DateTime t1, System.DateTime t2) { throw null; }
        public static System.TimeSpan operator -(System.DateTime d1, System.DateTime d2) { throw null; }
        public static System.DateTime operator -(System.DateTime d, System.TimeSpan t) { throw null; }
        public static System.DateTime Parse(System.ReadOnlySpan<char> s, System.IFormatProvider provider=null, System.Globalization.DateTimeStyles styles=(System.Globalization.DateTimeStyles)(0)) { throw null; }
        public static System.DateTime Parse(string s) { throw null; }
        public static System.DateTime Parse(string s, System.IFormatProvider provider) { throw null; }
        public static System.DateTime Parse(string s, System.IFormatProvider provider, System.Globalization.DateTimeStyles styles) { throw null; }
        public static System.DateTime ParseExact(System.ReadOnlySpan<char> s, System.ReadOnlySpan<char> format, System.IFormatProvider provider, System.Globalization.DateTimeStyles style=(System.Globalization.DateTimeStyles)(0)) { throw null; }
        public static System.DateTime ParseExact(System.ReadOnlySpan<char> s, string[] formats, System.IFormatProvider provider, System.Globalization.DateTimeStyles style=(System.Globalization.DateTimeStyles)(0)) { throw null; }
        public static System.DateTime ParseExact(string s, string format, System.IFormatProvider provider) { throw null; }
        public static System.DateTime ParseExact(string s, string format, System.IFormatProvider provider, System.Globalization.DateTimeStyles style) { throw null; }
        public static System.DateTime ParseExact(string s, string[] formats, System.IFormatProvider provider, System.Globalization.DateTimeStyles style) { throw null; }
        public static System.DateTime SpecifyKind(System.DateTime value, System.DateTimeKind kind) { throw null; }
        public System.TimeSpan Subtract(System.DateTime value) { throw null; }
        public System.DateTime Subtract(System.TimeSpan value) { throw null; }
        bool System.IConvertible.ToBoolean(System.IFormatProvider provider) { throw null; }
        byte System.IConvertible.ToByte(System.IFormatProvider provider) { throw null; }
        char System.IConvertible.ToChar(System.IFormatProvider provider) { throw null; }
        System.DateTime System.IConvertible.ToDateTime(System.IFormatProvider provider) { throw null; }
        decimal System.IConvertible.ToDecimal(System.IFormatProvider provider) { throw null; }
        double System.IConvertible.ToDouble(System.IFormatProvider provider) { throw null; }
        short System.IConvertible.ToInt16(System.IFormatProvider provider) { throw null; }
        int System.IConvertible.ToInt32(System.IFormatProvider provider) { throw null; }
        long System.IConvertible.ToInt64(System.IFormatProvider provider) { throw null; }
        sbyte System.IConvertible.ToSByte(System.IFormatProvider provider) { throw null; }
        float System.IConvertible.ToSingle(System.IFormatProvider provider) { throw null; }
        object System.IConvertible.ToType(System.Type type, System.IFormatProvider provider) { throw null; }
        ushort System.IConvertible.ToUInt16(System.IFormatProvider provider) { throw null; }
        uint System.IConvertible.ToUInt32(System.IFormatProvider provider) { throw null; }
        ulong System.IConvertible.ToUInt64(System.IFormatProvider provider) { throw null; }
        void System.Runtime.Serialization.ISerializable.GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public long ToBinary() { throw null; }
        public long ToFileTime() { throw null; }
        public long ToFileTimeUtc() { throw null; }
        public System.DateTime ToLocalTime() { throw null; }
        public string ToLongDateString() { throw null; }
        public string ToLongTimeString() { throw null; }
        public double ToOADate() { throw null; }
        public string ToShortDateString() { throw null; }
        public string ToShortTimeString() { throw null; }
        public override string ToString() { throw null; }
        public string ToString(System.IFormatProvider provider) { throw null; }
        public string ToString(string format) { throw null; }
        public string ToString(string format, System.IFormatProvider provider) { throw null; }
        public System.DateTime ToUniversalTime() { throw null; }
        public bool TryFormat(System.Span<char> destination, out int charsWritten, System.ReadOnlySpan<char> format=default(System.ReadOnlySpan<char>), System.IFormatProvider provider=null) { throw null; }
        public static bool TryParse(System.ReadOnlySpan<char> s, out System.DateTime result) { throw null; }
        public static bool TryParse(System.ReadOnlySpan<char> s, System.IFormatProvider provider, System.Globalization.DateTimeStyles styles, out System.DateTime result) { throw null; }
        public static bool TryParse(string s, out System.DateTime result) { throw null; }
        public static bool TryParse(string s, System.IFormatProvider provider, System.Globalization.DateTimeStyles styles, out System.DateTime result) { throw null; }
        public static bool TryParseExact(System.ReadOnlySpan<char> s, System.ReadOnlySpan<char> format, System.IFormatProvider provider, System.Globalization.DateTimeStyles style, out System.DateTime result) { throw null; }
        public static bool TryParseExact(System.ReadOnlySpan<char> s, string[] formats, System.IFormatProvider provider, System.Globalization.DateTimeStyles style, out System.DateTime result) { throw null; }
        public static bool TryParseExact(string s, string format, System.IFormatProvider provider, System.Globalization.DateTimeStyles style, out System.DateTime result) { throw null; }
        public static bool TryParseExact(string s, string[] formats, System.IFormatProvider provider, System.Globalization.DateTimeStyles style, out System.DateTime result) { throw null; }
    }
    public enum DateTimeKind
    {
        Local = 2,
        Unspecified = 0,
        Utc = 1,
    }
    public partial struct DateTimeOffset : System.IComparable, System.IComparable<System.DateTimeOffset>, System.IEquatable<System.DateTimeOffset>, System.IFormattable, System.Runtime.Serialization.IDeserializationCallback, System.Runtime.Serialization.ISerializable
    {
        private int _dummy;
        public static readonly System.DateTimeOffset MaxValue;
        public static readonly System.DateTimeOffset MinValue;
        public static readonly System.DateTimeOffset UnixEpoch;
        public DateTimeOffset(System.DateTime dateTime) { throw null; }
        public DateTimeOffset(System.DateTime dateTime, System.TimeSpan offset) { throw null; }
        public DateTimeOffset(int year, int month, int day, int hour, int minute, int second, int millisecond, System.Globalization.Calendar calendar, System.TimeSpan offset) { throw null; }
        public DateTimeOffset(int year, int month, int day, int hour, int minute, int second, int millisecond, System.TimeSpan offset) { throw null; }
        public DateTimeOffset(int year, int month, int day, int hour, int minute, int second, System.TimeSpan offset) { throw null; }
        public DateTimeOffset(long ticks, System.TimeSpan offset) { throw null; }
        public System.DateTime Date { get { throw null; } }
        public System.DateTime DateTime { get { throw null; } }
        public int Day { get { throw null; } }
        public System.DayOfWeek DayOfWeek { get { throw null; } }
        public int DayOfYear { get { throw null; } }
        public int Hour { get { throw null; } }
        public System.DateTime LocalDateTime { get { throw null; } }
        public int Millisecond { get { throw null; } }
        public int Minute { get { throw null; } }
        public int Month { get { throw null; } }
        public static System.DateTimeOffset Now { get { throw null; } }
        public System.TimeSpan Offset { get { throw null; } }
        public int Second { get { throw null; } }
        public long Ticks { get { throw null; } }
        public System.TimeSpan TimeOfDay { get { throw null; } }
        public System.DateTime UtcDateTime { get { throw null; } }
        public static System.DateTimeOffset UtcNow { get { throw null; } }
        public long UtcTicks { get { throw null; } }
        public int Year { get { throw null; } }
        public System.DateTimeOffset Add(System.TimeSpan timeSpan) { throw null; }
        public System.DateTimeOffset AddDays(double days) { throw null; }
        public System.DateTimeOffset AddHours(double hours) { throw null; }
        public System.DateTimeOffset AddMilliseconds(double milliseconds) { throw null; }
        public System.DateTimeOffset AddMinutes(double minutes) { throw null; }
        public System.DateTimeOffset AddMonths(int months) { throw null; }
        public System.DateTimeOffset AddSeconds(double seconds) { throw null; }
        public System.DateTimeOffset AddTicks(long ticks) { throw null; }
        public System.DateTimeOffset AddYears(int years) { throw null; }
        public static int Compare(System.DateTimeOffset first, System.DateTimeOffset second) { throw null; }
        public int CompareTo(System.DateTimeOffset other) { throw null; }
        public bool Equals(System.DateTimeOffset other) { throw null; }
        public static bool Equals(System.DateTimeOffset first, System.DateTimeOffset second) { throw null; }
        public override bool Equals(object obj) { throw null; }
        public bool EqualsExact(System.DateTimeOffset other) { throw null; }
        public static System.DateTimeOffset FromFileTime(long fileTime) { throw null; }
        public static System.DateTimeOffset FromUnixTimeMilliseconds(long milliseconds) { throw null; }
        public static System.DateTimeOffset FromUnixTimeSeconds(long seconds) { throw null; }
        public override int GetHashCode() { throw null; }
        public static System.DateTimeOffset operator +(System.DateTimeOffset dateTimeOffset, System.TimeSpan timeSpan) { throw null; }
        public static bool operator ==(System.DateTimeOffset left, System.DateTimeOffset right) { throw null; }
        public static bool operator >(System.DateTimeOffset left, System.DateTimeOffset right) { throw null; }
        public static bool operator >=(System.DateTimeOffset left, System.DateTimeOffset right) { throw null; }
        public static implicit operator System.DateTimeOffset (System.DateTime dateTime) { throw null; }
        public static bool operator !=(System.DateTimeOffset left, System.DateTimeOffset right) { throw null; }
        public static bool operator <(System.DateTimeOffset left, System.DateTimeOffset right) { throw null; }
        public static bool operator <=(System.DateTimeOffset left, System.DateTimeOffset right) { throw null; }
        public static System.TimeSpan operator -(System.DateTimeOffset left, System.DateTimeOffset right) { throw null; }
        public static System.DateTimeOffset operator -(System.DateTimeOffset dateTimeOffset, System.TimeSpan timeSpan) { throw null; }
        public static System.DateTimeOffset Parse(System.ReadOnlySpan<char> input, System.IFormatProvider formatProvider=null, System.Globalization.DateTimeStyles styles=(System.Globalization.DateTimeStyles)(0)) { throw null; }
        public static System.DateTimeOffset Parse(string input) { throw null; }
        public static System.DateTimeOffset Parse(string input, System.IFormatProvider formatProvider) { throw null; }
        public static System.DateTimeOffset Parse(string input, System.IFormatProvider formatProvider, System.Globalization.DateTimeStyles styles) { throw null; }
        public static System.DateTimeOffset ParseExact(System.ReadOnlySpan<char> input, System.ReadOnlySpan<char> format, System.IFormatProvider formatProvider, System.Globalization.DateTimeStyles styles=(System.Globalization.DateTimeStyles)(0)) { throw null; }
        public static System.DateTimeOffset ParseExact(System.ReadOnlySpan<char> input, string[] formats, System.IFormatProvider formatProvider, System.Globalization.DateTimeStyles styles=(System.Globalization.DateTimeStyles)(0)) { throw null; }
        public static System.DateTimeOffset ParseExact(string input, string format, System.IFormatProvider formatProvider) { throw null; }
        public static System.DateTimeOffset ParseExact(string input, string format, System.IFormatProvider formatProvider, System.Globalization.DateTimeStyles styles) { throw null; }
        public static System.DateTimeOffset ParseExact(string input, string[] formats, System.IFormatProvider formatProvider, System.Globalization.DateTimeStyles styles) { throw null; }
        public System.TimeSpan Subtract(System.DateTimeOffset value) { throw null; }
        public System.DateTimeOffset Subtract(System.TimeSpan value) { throw null; }
        int System.IComparable.CompareTo(object obj) { throw null; }
        void System.Runtime.Serialization.IDeserializationCallback.OnDeserialization(object sender) { }
        void System.Runtime.Serialization.ISerializable.GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public long ToFileTime() { throw null; }
        public System.DateTimeOffset ToLocalTime() { throw null; }
        public System.DateTimeOffset ToOffset(System.TimeSpan offset) { throw null; }
        public override string ToString() { throw null; }
        public string ToString(System.IFormatProvider formatProvider) { throw null; }
        public string ToString(string format) { throw null; }
        public string ToString(string format, System.IFormatProvider formatProvider) { throw null; }
        public System.DateTimeOffset ToUniversalTime() { throw null; }
        public long ToUnixTimeMilliseconds() { throw null; }
        public long ToUnixTimeSeconds() { throw null; }
        public bool TryFormat(System.Span<char> destination, out int charsWritten, System.ReadOnlySpan<char> format=default(System.ReadOnlySpan<char>), System.IFormatProvider formatProvider=null) { throw null; }
        public static bool TryParse(System.ReadOnlySpan<char> input, out System.DateTimeOffset result) { throw null; }
        public static bool TryParse(System.ReadOnlySpan<char> input, System.IFormatProvider formatProvider, System.Globalization.DateTimeStyles styles, out System.DateTimeOffset result) { throw null; }
        public static bool TryParse(string input, out System.DateTimeOffset result) { throw null; }
        public static bool TryParse(string input, System.IFormatProvider formatProvider, System.Globalization.DateTimeStyles styles, out System.DateTimeOffset result) { throw null; }
        public static bool TryParseExact(System.ReadOnlySpan<char> input, System.ReadOnlySpan<char> format, System.IFormatProvider formatProvider, System.Globalization.DateTimeStyles styles, out System.DateTimeOffset result) { throw null; }
        public static bool TryParseExact(System.ReadOnlySpan<char> input, string[] formats, System.IFormatProvider formatProvider, System.Globalization.DateTimeStyles styles, out System.DateTimeOffset result) { throw null; }
        public static bool TryParseExact(string input, string format, System.IFormatProvider formatProvider, System.Globalization.DateTimeStyles styles, out System.DateTimeOffset result) { throw null; }
        public static bool TryParseExact(string input, string[] formats, System.IFormatProvider formatProvider, System.Globalization.DateTimeStyles styles, out System.DateTimeOffset result) { throw null; }
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
    public sealed partial class DBNull : System.IConvertible, System.Runtime.Serialization.ISerializable
    {
        internal DBNull() { }
        public static readonly System.DBNull Value;
        public void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public System.TypeCode GetTypeCode() { throw null; }
        bool System.IConvertible.ToBoolean(System.IFormatProvider provider) { throw null; }
        byte System.IConvertible.ToByte(System.IFormatProvider provider) { throw null; }
        char System.IConvertible.ToChar(System.IFormatProvider provider) { throw null; }
        System.DateTime System.IConvertible.ToDateTime(System.IFormatProvider provider) { throw null; }
        decimal System.IConvertible.ToDecimal(System.IFormatProvider provider) { throw null; }
        double System.IConvertible.ToDouble(System.IFormatProvider provider) { throw null; }
        short System.IConvertible.ToInt16(System.IFormatProvider provider) { throw null; }
        int System.IConvertible.ToInt32(System.IFormatProvider provider) { throw null; }
        long System.IConvertible.ToInt64(System.IFormatProvider provider) { throw null; }
        sbyte System.IConvertible.ToSByte(System.IFormatProvider provider) { throw null; }
        float System.IConvertible.ToSingle(System.IFormatProvider provider) { throw null; }
        object System.IConvertible.ToType(System.Type type, System.IFormatProvider provider) { throw null; }
        ushort System.IConvertible.ToUInt16(System.IFormatProvider provider) { throw null; }
        uint System.IConvertible.ToUInt32(System.IFormatProvider provider) { throw null; }
        ulong System.IConvertible.ToUInt64(System.IFormatProvider provider) { throw null; }
        public override string ToString() { throw null; }
        public string ToString(System.IFormatProvider provider) { throw null; }
    }
    public partial struct Decimal : System.IComparable, System.IComparable<decimal>, System.IConvertible, System.IEquatable<decimal>, System.IFormattable, System.Runtime.Serialization.IDeserializationCallback
    {
        private int _dummy;
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
        public Decimal(double value) { throw null; }
        public Decimal(int value) { throw null; }
        public Decimal(int lo, int mid, int hi, bool isNegative, byte scale) { throw null; }
        public Decimal(int[] bits) { throw null; }
        public Decimal(long value) { throw null; }
        public Decimal(float value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public Decimal(uint value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public Decimal(ulong value) { throw null; }
        public static System.Decimal Add(System.Decimal d1, System.Decimal d2) { throw null; }
        public static System.Decimal Ceiling(System.Decimal d) { throw null; }
        public static int Compare(System.Decimal d1, System.Decimal d2) { throw null; }
        public int CompareTo(System.Decimal value) { throw null; }
        public int CompareTo(object value) { throw null; }
        public static System.Decimal Divide(System.Decimal d1, System.Decimal d2) { throw null; }
        public bool Equals(System.Decimal value) { throw null; }
        public static bool Equals(System.Decimal d1, System.Decimal d2) { throw null; }
        public override bool Equals(object value) { throw null; }
        public static System.Decimal Floor(System.Decimal d) { throw null; }
        public static System.Decimal FromOACurrency(long cy) { throw null; }
        public static int[] GetBits(System.Decimal d) { throw null; }
        public override int GetHashCode() { throw null; }
        public System.TypeCode GetTypeCode() { throw null; }
        public static System.Decimal Multiply(System.Decimal d1, System.Decimal d2) { throw null; }
        public static System.Decimal Negate(System.Decimal d) { throw null; }
        public static System.Decimal operator +(System.Decimal d1, System.Decimal d2) { throw null; }
        public static System.Decimal operator --(System.Decimal d) { throw null; }
        public static System.Decimal operator /(System.Decimal d1, System.Decimal d2) { throw null; }
        public static bool operator ==(System.Decimal d1, System.Decimal d2) { throw null; }
        public static explicit operator byte (System.Decimal value) { throw null; }
        public static explicit operator char (System.Decimal value) { throw null; }
        public static explicit operator double (System.Decimal value) { throw null; }
        public static explicit operator short (System.Decimal value) { throw null; }
        public static explicit operator int (System.Decimal value) { throw null; }
        public static explicit operator long (System.Decimal value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator sbyte (System.Decimal value) { throw null; }
        public static explicit operator float (System.Decimal value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator ushort (System.Decimal value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator uint (System.Decimal value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator ulong (System.Decimal value) { throw null; }
        public static explicit operator System.Decimal (double value) { throw null; }
        public static explicit operator System.Decimal (float value) { throw null; }
        public static bool operator >(System.Decimal d1, System.Decimal d2) { throw null; }
        public static bool operator >=(System.Decimal d1, System.Decimal d2) { throw null; }
        public static implicit operator System.Decimal (byte value) { throw null; }
        public static implicit operator System.Decimal (char value) { throw null; }
        public static implicit operator System.Decimal (short value) { throw null; }
        public static implicit operator System.Decimal (int value) { throw null; }
        public static implicit operator System.Decimal (long value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static implicit operator System.Decimal (sbyte value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static implicit operator System.Decimal (ushort value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static implicit operator System.Decimal (uint value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static implicit operator System.Decimal (ulong value) { throw null; }
        public static System.Decimal operator ++(System.Decimal d) { throw null; }
        public static bool operator !=(System.Decimal d1, System.Decimal d2) { throw null; }
        public static bool operator <(System.Decimal d1, System.Decimal d2) { throw null; }
        public static bool operator <=(System.Decimal d1, System.Decimal d2) { throw null; }
        public static System.Decimal operator %(System.Decimal d1, System.Decimal d2) { throw null; }
        public static System.Decimal operator *(System.Decimal d1, System.Decimal d2) { throw null; }
        public static System.Decimal operator -(System.Decimal d1, System.Decimal d2) { throw null; }
        public static System.Decimal operator -(System.Decimal d) { throw null; }
        public static System.Decimal operator +(System.Decimal d) { throw null; }
        public static System.Decimal Parse(System.ReadOnlySpan<char> s, System.Globalization.NumberStyles style=(System.Globalization.NumberStyles)(7), System.IFormatProvider provider=null) { throw null; }
        public static System.Decimal Parse(string s) { throw null; }
        public static System.Decimal Parse(string s, System.Globalization.NumberStyles style) { throw null; }
        public static System.Decimal Parse(string s, System.Globalization.NumberStyles style, System.IFormatProvider provider) { throw null; }
        public static System.Decimal Parse(string s, System.IFormatProvider provider) { throw null; }
        public static System.Decimal Remainder(System.Decimal d1, System.Decimal d2) { throw null; }
        public static System.Decimal Round(System.Decimal d) { throw null; }
        public static System.Decimal Round(System.Decimal d, int decimals) { throw null; }
        public static System.Decimal Round(System.Decimal d, int decimals, System.MidpointRounding mode) { throw null; }
        public static System.Decimal Round(System.Decimal d, System.MidpointRounding mode) { throw null; }
        public static System.Decimal Subtract(System.Decimal d1, System.Decimal d2) { throw null; }
        bool System.IConvertible.ToBoolean(System.IFormatProvider provider) { throw null; }
        byte System.IConvertible.ToByte(System.IFormatProvider provider) { throw null; }
        char System.IConvertible.ToChar(System.IFormatProvider provider) { throw null; }
        System.DateTime System.IConvertible.ToDateTime(System.IFormatProvider provider) { throw null; }
        System.Decimal System.IConvertible.ToDecimal(System.IFormatProvider provider) { throw null; }
        double System.IConvertible.ToDouble(System.IFormatProvider provider) { throw null; }
        short System.IConvertible.ToInt16(System.IFormatProvider provider) { throw null; }
        int System.IConvertible.ToInt32(System.IFormatProvider provider) { throw null; }
        long System.IConvertible.ToInt64(System.IFormatProvider provider) { throw null; }
        sbyte System.IConvertible.ToSByte(System.IFormatProvider provider) { throw null; }
        float System.IConvertible.ToSingle(System.IFormatProvider provider) { throw null; }
        object System.IConvertible.ToType(System.Type type, System.IFormatProvider provider) { throw null; }
        ushort System.IConvertible.ToUInt16(System.IFormatProvider provider) { throw null; }
        uint System.IConvertible.ToUInt32(System.IFormatProvider provider) { throw null; }
        ulong System.IConvertible.ToUInt64(System.IFormatProvider provider) { throw null; }
        void System.Runtime.Serialization.IDeserializationCallback.OnDeserialization(object sender) { }
        public static byte ToByte(System.Decimal value) { throw null; }
        public static double ToDouble(System.Decimal d) { throw null; }
        public static short ToInt16(System.Decimal value) { throw null; }
        public static int ToInt32(System.Decimal d) { throw null; }
        public static long ToInt64(System.Decimal d) { throw null; }
        public static long ToOACurrency(System.Decimal value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static sbyte ToSByte(System.Decimal value) { throw null; }
        public static float ToSingle(System.Decimal d) { throw null; }
        public override string ToString() { throw null; }
        public string ToString(System.IFormatProvider provider) { throw null; }
        public string ToString(string format) { throw null; }
        public string ToString(string format, System.IFormatProvider provider) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ushort ToUInt16(System.Decimal value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static uint ToUInt32(System.Decimal d) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ulong ToUInt64(System.Decimal d) { throw null; }
        public static System.Decimal Truncate(System.Decimal d) { throw null; }
        public bool TryFormat(System.Span<char> destination, out int charsWritten, System.ReadOnlySpan<char> format=default(System.ReadOnlySpan<char>), System.IFormatProvider provider=null) { throw null; }
        public static bool TryParse(System.ReadOnlySpan<char> s, out System.Decimal result) { throw null; }
        public static bool TryParse(System.ReadOnlySpan<char> s, System.Globalization.NumberStyles style, System.IFormatProvider provider, out System.Decimal result) { throw null; }
        public static bool TryParse(string s, out System.Decimal result) { throw null; }
        public static bool TryParse(string s, System.Globalization.NumberStyles style, System.IFormatProvider provider, out System.Decimal result) { throw null; }
    }
    public abstract partial class Delegate : System.ICloneable, System.Runtime.Serialization.ISerializable
    {
        protected Delegate(object target, string method) { }
        protected Delegate(System.Type target, string method) { }
        public System.Reflection.MethodInfo Method { get { throw null; } }
        public object Target { get { throw null; } }
        public virtual object Clone() { throw null; }
        public static System.Delegate Combine(System.Delegate a, System.Delegate b) { throw null; }
        public static System.Delegate Combine(params System.Delegate[] delegates) { throw null; }
        protected virtual System.Delegate CombineImpl(System.Delegate d) { throw null; }
        public static System.Delegate CreateDelegate(System.Type type, object firstArgument, System.Reflection.MethodInfo method) { throw null; }
        public static System.Delegate CreateDelegate(System.Type type, object firstArgument, System.Reflection.MethodInfo method, bool throwOnBindFailure) { throw null; }
        public static System.Delegate CreateDelegate(System.Type type, object target, string method) { throw null; }
        public static System.Delegate CreateDelegate(System.Type type, object target, string method, bool ignoreCase) { throw null; }
        public static System.Delegate CreateDelegate(System.Type type, object target, string method, bool ignoreCase, bool throwOnBindFailure) { throw null; }
        public static System.Delegate CreateDelegate(System.Type type, System.Reflection.MethodInfo method) { throw null; }
        public static System.Delegate CreateDelegate(System.Type type, System.Reflection.MethodInfo method, bool throwOnBindFailure) { throw null; }
        public static System.Delegate CreateDelegate(System.Type type, System.Type target, string method) { throw null; }
        public static System.Delegate CreateDelegate(System.Type type, System.Type target, string method, bool ignoreCase) { throw null; }
        public static System.Delegate CreateDelegate(System.Type type, System.Type target, string method, bool ignoreCase, bool throwOnBindFailure) { throw null; }
        public object DynamicInvoke(params object[] args) { throw null; }
        protected virtual object DynamicInvokeImpl(object[] args) { throw null; }
        public override bool Equals(object obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public virtual System.Delegate[] GetInvocationList() { throw null; }
        protected virtual System.Reflection.MethodInfo GetMethodImpl() { throw null; }
        public virtual void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public static bool operator ==(System.Delegate d1, System.Delegate d2) { throw null; }
        public static bool operator !=(System.Delegate d1, System.Delegate d2) { throw null; }
        public static System.Delegate Remove(System.Delegate source, System.Delegate value) { throw null; }
        public static System.Delegate RemoveAll(System.Delegate source, System.Delegate value) { throw null; }
        protected virtual System.Delegate RemoveImpl(System.Delegate d) { throw null; }
    }
    public partial class DivideByZeroException : System.ArithmeticException
    {
        public DivideByZeroException() { }
        protected DivideByZeroException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public DivideByZeroException(string message) { }
        public DivideByZeroException(string message, System.Exception innerException) { }
    }
    public partial struct Double : System.IComparable, System.IComparable<double>, System.IConvertible, System.IEquatable<double>, System.IFormattable
    {
        private double _dummy;
        //public const double Epsilon = 4.94065645841247E-324;     -- defined in System.Runtime.Manual.cs
        //public const double MaxValue = 1.7976931348623157E+308;  -- defined in System.Runtime.Manual.cs
        //public const double MinValue = -1.7976931348623157E+308; -- defined in System.Runtime.Manual.cs
        //public const double NaN = 0.0 / 0.0;                     -- defined in System.Runtime.Manual.cs
        //public const double NegativeInfinity = -1.0 / 0.0;       -- defined in System.Runtime.Manual.cs
        //public const double PositiveInfinity = 1.0 / 0.0;        -- defined in System.Runtime.Manual.cs
        public int CompareTo(System.Double value) { throw null; }
        public int CompareTo(object value) { throw null; }
        public bool Equals(System.Double obj) { throw null; }
        public override bool Equals(object obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public System.TypeCode GetTypeCode() { throw null; }
        public static bool IsFinite(System.Double d) { throw null; }
        public static bool IsInfinity(System.Double d) { throw null; }
        public static bool IsNaN(System.Double d) { throw null; }
        public static bool IsNegative(System.Double d) { throw null; }
        public static bool IsNegativeInfinity(System.Double d) { throw null; }
        public static bool IsNormal(System.Double d) { throw null; }
        public static bool IsPositiveInfinity(System.Double d) { throw null; }
        public static bool IsSubnormal(System.Double d) { throw null; }
        public static bool operator ==(System.Double left, System.Double right) { throw null; }
        public static bool operator >(System.Double left, System.Double right) { throw null; }
        public static bool operator >=(System.Double left, System.Double right) { throw null; }
        public static bool operator !=(System.Double left, System.Double right) { throw null; }
        public static bool operator <(System.Double left, System.Double right) { throw null; }
        public static bool operator <=(System.Double left, System.Double right) { throw null; }
        public static System.Double Parse(System.ReadOnlySpan<char> s, System.Globalization.NumberStyles style=(System.Globalization.NumberStyles)(7), System.IFormatProvider provider=null) { throw null; }
        public static System.Double Parse(string s) { throw null; }
        public static System.Double Parse(string s, System.Globalization.NumberStyles style) { throw null; }
        public static System.Double Parse(string s, System.Globalization.NumberStyles style, System.IFormatProvider provider) { throw null; }
        public static System.Double Parse(string s, System.IFormatProvider provider) { throw null; }
        bool System.IConvertible.ToBoolean(System.IFormatProvider provider) { throw null; }
        byte System.IConvertible.ToByte(System.IFormatProvider provider) { throw null; }
        char System.IConvertible.ToChar(System.IFormatProvider provider) { throw null; }
        System.DateTime System.IConvertible.ToDateTime(System.IFormatProvider provider) { throw null; }
        decimal System.IConvertible.ToDecimal(System.IFormatProvider provider) { throw null; }
        System.Double System.IConvertible.ToDouble(System.IFormatProvider provider) { throw null; }
        short System.IConvertible.ToInt16(System.IFormatProvider provider) { throw null; }
        int System.IConvertible.ToInt32(System.IFormatProvider provider) { throw null; }
        long System.IConvertible.ToInt64(System.IFormatProvider provider) { throw null; }
        sbyte System.IConvertible.ToSByte(System.IFormatProvider provider) { throw null; }
        float System.IConvertible.ToSingle(System.IFormatProvider provider) { throw null; }
        object System.IConvertible.ToType(System.Type type, System.IFormatProvider provider) { throw null; }
        ushort System.IConvertible.ToUInt16(System.IFormatProvider provider) { throw null; }
        uint System.IConvertible.ToUInt32(System.IFormatProvider provider) { throw null; }
        ulong System.IConvertible.ToUInt64(System.IFormatProvider provider) { throw null; }
        public override string ToString() { throw null; }
        public string ToString(System.IFormatProvider provider) { throw null; }
        public string ToString(string format) { throw null; }
        public string ToString(string format, System.IFormatProvider provider) { throw null; }
        public bool TryFormat(System.Span<char> destination, out int charsWritten, System.ReadOnlySpan<char> format=default(System.ReadOnlySpan<char>), System.IFormatProvider provider=null) { throw null; }
        public static bool TryParse(System.ReadOnlySpan<char> s, out System.Double result) { throw null; }
        public static bool TryParse(System.ReadOnlySpan<char> s, System.Globalization.NumberStyles style, System.IFormatProvider provider, out System.Double result) { throw null; }
        public static bool TryParse(string s, out System.Double result) { throw null; }
        public static bool TryParse(string s, System.Globalization.NumberStyles style, System.IFormatProvider provider, out System.Double result) { throw null; }
    }
    public partial class DuplicateWaitObjectException : System.ArgumentException
    {
        public DuplicateWaitObjectException() { }
        protected DuplicateWaitObjectException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public DuplicateWaitObjectException(string parameterName) { }
        public DuplicateWaitObjectException(string message, System.Exception innerException) { }
        public DuplicateWaitObjectException(string parameterName, string message) { }
    }
    public partial class EntryPointNotFoundException : System.TypeLoadException
    {
        public EntryPointNotFoundException() { }
        protected EntryPointNotFoundException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public EntryPointNotFoundException(string message) { }
        public EntryPointNotFoundException(string message, System.Exception inner) { }
    }
    public abstract partial class Enum : System.ValueType, System.IComparable, System.IConvertible, System.IFormattable
    {
        protected Enum() { }
        public int CompareTo(object target) { throw null; }
        public override bool Equals(object obj) { throw null; }
        public static string Format(System.Type enumType, object value, string format) { throw null; }
        public override int GetHashCode() { throw null; }
        public static string GetName(System.Type enumType, object value) { throw null; }
        public static string[] GetNames(System.Type enumType) { throw null; }
        public System.TypeCode GetTypeCode() { throw null; }
        public static System.Type GetUnderlyingType(System.Type enumType) { throw null; }
        public static System.Array GetValues(System.Type enumType) { throw null; }
        public bool HasFlag(System.Enum flag) { throw null; }
        public static bool IsDefined(System.Type enumType, object value) { throw null; }
        public static object Parse(System.Type enumType, string value) { throw null; }
        public static object Parse(System.Type enumType, string value, bool ignoreCase) { throw null; }
        public static TEnum Parse<TEnum>(string value) where TEnum : struct { throw null; }
        public static TEnum Parse<TEnum>(string value, bool ignoreCase) where TEnum : struct { throw null; }
        bool System.IConvertible.ToBoolean(System.IFormatProvider provider) { throw null; }
        byte System.IConvertible.ToByte(System.IFormatProvider provider) { throw null; }
        char System.IConvertible.ToChar(System.IFormatProvider provider) { throw null; }
        System.DateTime System.IConvertible.ToDateTime(System.IFormatProvider provider) { throw null; }
        decimal System.IConvertible.ToDecimal(System.IFormatProvider provider) { throw null; }
        double System.IConvertible.ToDouble(System.IFormatProvider provider) { throw null; }
        short System.IConvertible.ToInt16(System.IFormatProvider provider) { throw null; }
        int System.IConvertible.ToInt32(System.IFormatProvider provider) { throw null; }
        long System.IConvertible.ToInt64(System.IFormatProvider provider) { throw null; }
        sbyte System.IConvertible.ToSByte(System.IFormatProvider provider) { throw null; }
        float System.IConvertible.ToSingle(System.IFormatProvider provider) { throw null; }
        object System.IConvertible.ToType(System.Type type, System.IFormatProvider provider) { throw null; }
        ushort System.IConvertible.ToUInt16(System.IFormatProvider provider) { throw null; }
        uint System.IConvertible.ToUInt32(System.IFormatProvider provider) { throw null; }
        ulong System.IConvertible.ToUInt64(System.IFormatProvider provider) { throw null; }
        public static object ToObject(System.Type enumType, byte value) { throw null; }
        public static object ToObject(System.Type enumType, short value) { throw null; }
        public static object ToObject(System.Type enumType, int value) { throw null; }
        public static object ToObject(System.Type enumType, long value) { throw null; }
        public static object ToObject(System.Type enumType, object value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static object ToObject(System.Type enumType, sbyte value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static object ToObject(System.Type enumType, ushort value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static object ToObject(System.Type enumType, uint value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static object ToObject(System.Type enumType, ulong value) { throw null; }
        public override string ToString() { throw null; }
        [System.ObsoleteAttribute("The provider argument is not used. Please use ToString().")]
        public string ToString(System.IFormatProvider provider) { throw null; }
        public string ToString(string format) { throw null; }
        [System.ObsoleteAttribute("The provider argument is not used. Please use ToString(String).")]
        public string ToString(string format, System.IFormatProvider provider) { throw null; }
        public static bool TryParse(System.Type enumType, string value, bool ignoreCase, out object result) { throw null; }
        public static bool TryParse(System.Type enumType, string value, out object result) { throw null; }
        public static bool TryParse<TEnum>(string value, bool ignoreCase, out TEnum result) where TEnum : struct { throw null; }
        public static bool TryParse<TEnum>(string value, out TEnum result) where TEnum : struct { throw null; }
    }
    public partial class EventArgs
    {
        public static readonly System.EventArgs Empty;
        public EventArgs() { }
    }
    public delegate void EventHandler(object sender, System.EventArgs e);
    public delegate void EventHandler<TEventArgs>(object sender, TEventArgs e);
    public partial class Exception : System.Runtime.Serialization.ISerializable
    {
        public Exception() { }
        protected Exception(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public Exception(string message) { }
        public Exception(string message, System.Exception innerException) { }
        public virtual System.Collections.IDictionary Data { get { throw null; } }
        public virtual string HelpLink { get { throw null; } set { } }
        public int HResult { get { throw null; } protected set { } }
        public System.Exception InnerException { get { throw null; } }
        public virtual string Message { get { throw null; } }
        public virtual string Source { get { throw null; } set { } }
        public virtual string StackTrace { get { throw null; } }
        public System.Reflection.MethodBase TargetSite { get { throw null; } }
        protected event System.EventHandler<System.Runtime.Serialization.SafeSerializationEventArgs> SerializeObjectState { add { } remove { } }
        public virtual System.Exception GetBaseException() { throw null; }
        public virtual void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public new System.Type GetType() { throw null; }
        public override string ToString() { throw null; }
    }
    [System.ObsoleteAttribute("This type previously indicated an unspecified fatal error in the runtime. The runtime no longer raises this exception so this type is obsolete.")]
    public sealed partial class ExecutionEngineException : System.SystemException
    {
        public ExecutionEngineException() { }
        public ExecutionEngineException(string message) { }
        public ExecutionEngineException(string message, System.Exception innerException) { }
    }
    public partial class FieldAccessException : System.MemberAccessException
    {
        public FieldAccessException() { }
        protected FieldAccessException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public FieldAccessException(string message) { }
        public FieldAccessException(string message, System.Exception inner) { }
    }
    public partial class FileStyleUriParser : System.UriParser
    {
        public FileStyleUriParser() { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(16), Inherited=false)]
    public partial class FlagsAttribute : System.Attribute
    {
        public FlagsAttribute() { }
    }
    public partial class FormatException : System.SystemException
    {
        public FormatException() { }
        protected FormatException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
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
        public static string Invariant(System.FormattableString formattable) { throw null; }
        string System.IFormattable.ToString(string ignored, System.IFormatProvider formatProvider) { throw null; }
        public override string ToString() { throw null; }
        public abstract string ToString(System.IFormatProvider formatProvider);
    }
    public partial class FtpStyleUriParser : System.UriParser
    {
        public FtpStyleUriParser() { }
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
        public static int MaxGeneration { get { throw null; } }
        public static void AddMemoryPressure(long bytesAllocated) { }
        public static void CancelFullGCNotification() { }
        public static void Collect() { }
        public static void Collect(int generation) { }
        public static void Collect(int generation, System.GCCollectionMode mode) { }
        public static void Collect(int generation, System.GCCollectionMode mode, bool blocking) { }
        public static void Collect(int generation, System.GCCollectionMode mode, bool blocking, bool compacting) { }
        public static int CollectionCount(int generation) { throw null; }
        public static void EndNoGCRegion() { }
        public static long GetAllocatedBytesForCurrentThread() { throw null; }
        public static int GetGeneration(object obj) { throw null; }
        public static int GetGeneration(System.WeakReference wo) { throw null; }
        public static long GetTotalMemory(bool forceFullCollection) { throw null; }
        public static void KeepAlive(object obj) { }
        public static void RegisterForFullGCNotification(int maxGenerationThreshold, int largeObjectHeapThreshold) { }
        public static void RemoveMemoryPressure(long bytesAllocated) { }
        public static void ReRegisterForFinalize(object obj) { }
        public static void SuppressFinalize(object obj) { }
        public static bool TryStartNoGCRegion(long totalSize) { throw null; }
        public static bool TryStartNoGCRegion(long totalSize, bool disallowFullBlockingGC) { throw null; }
        public static bool TryStartNoGCRegion(long totalSize, long lohSize) { throw null; }
        public static bool TryStartNoGCRegion(long totalSize, long lohSize, bool disallowFullBlockingGC) { throw null; }
        public static System.GCNotificationStatus WaitForFullGCApproach() { throw null; }
        public static System.GCNotificationStatus WaitForFullGCApproach(int millisecondsTimeout) { throw null; }
        public static System.GCNotificationStatus WaitForFullGCComplete() { throw null; }
        public static System.GCNotificationStatus WaitForFullGCComplete(int millisecondsTimeout) { throw null; }
        public static void WaitForPendingFinalizers() { }
    }
    public enum GCCollectionMode
    {
        Default = 0,
        Forced = 1,
        Optimized = 2,
    }
    public enum GCNotificationStatus
    {
        Canceled = 2,
        Failed = 1,
        NotApplicable = 4,
        Succeeded = 0,
        Timeout = 3,
    }
    public partial class GenericUriParser : System.UriParser
    {
        public GenericUriParser(System.GenericUriParserOptions options) { }
    }
    [System.FlagsAttribute]
    public enum GenericUriParserOptions
    {
        AllowEmptyAuthority = 2,
        Default = 0,
        DontCompressPath = 128,
        DontConvertPathBackslashes = 64,
        DontUnescapePathDotsAndSlashes = 256,
        GenericAuthority = 1,
        Idn = 512,
        IriParsing = 1024,
        NoFragment = 32,
        NoPort = 8,
        NoQuery = 16,
        NoUserInfo = 4,
    }
    public partial class GopherStyleUriParser : System.UriParser
    {
        public GopherStyleUriParser() { }
    }
    public partial struct Guid : System.IComparable, System.IComparable<System.Guid>, System.IEquatable<System.Guid>, System.IFormattable
    {
        private int _dummy;
        public static readonly System.Guid Empty;
        public Guid(byte[] b) { throw null; }
        public Guid(int a, short b, short c, byte d, byte e, byte f, byte g, byte h, byte i, byte j, byte k) { throw null; }
        public Guid(int a, short b, short c, byte[] d) { throw null; }
        public Guid(System.ReadOnlySpan<byte> b) { throw null; }
        public Guid(string g) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public Guid(uint a, ushort b, ushort c, byte d, byte e, byte f, byte g, byte h, byte i, byte j, byte k) { throw null; }
        public int CompareTo(System.Guid value) { throw null; }
        public int CompareTo(object value) { throw null; }
        public bool Equals(System.Guid g) { throw null; }
        public override bool Equals(object o) { throw null; }
        public override int GetHashCode() { throw null; }
        public static System.Guid NewGuid() { throw null; }
        public static bool operator ==(System.Guid a, System.Guid b) { throw null; }
        public static bool operator !=(System.Guid a, System.Guid b) { throw null; }
        public static System.Guid Parse(System.ReadOnlySpan<char> input) { throw null; }
        public static System.Guid Parse(string input) { throw null; }
        public static System.Guid ParseExact(System.ReadOnlySpan<char> input, System.ReadOnlySpan<char> format) { throw null; }
        public static System.Guid ParseExact(string input, string format) { throw null; }
        public byte[] ToByteArray() { throw null; }
        public override string ToString() { throw null; }
        public string ToString(string format) { throw null; }
        public string ToString(string format, System.IFormatProvider provider) { throw null; }
        public bool TryFormat(System.Span<char> destination, out int charsWritten, System.ReadOnlySpan<char> format=default(System.ReadOnlySpan<char>)) { throw null; }
        public static bool TryParse(System.ReadOnlySpan<char> input, out System.Guid result) { throw null; }
        public static bool TryParse(string input, out System.Guid result) { throw null; }
        public static bool TryParseExact(System.ReadOnlySpan<char> input, System.ReadOnlySpan<char> format, out System.Guid result) { throw null; }
        public static bool TryParseExact(string input, string format, out System.Guid result) { throw null; }
        public bool TryWriteBytes(System.Span<byte> destination) { throw null; }
    }
    public partial struct HashCode
    {
        private int _dummy;
        public void Add<T>(T value) { }
        public void Add<T>(T value, System.Collections.Generic.IEqualityComparer<T> comparer) { }
        public static int Combine<T1>(T1 value1) { throw null; }
        public static int Combine<T1, T2>(T1 value1, T2 value2) { throw null; }
        public static int Combine<T1, T2, T3>(T1 value1, T2 value2, T3 value3) { throw null; }
        public static int Combine<T1, T2, T3, T4>(T1 value1, T2 value2, T3 value3, T4 value4) { throw null; }
        public static int Combine<T1, T2, T3, T4, T5>(T1 value1, T2 value2, T3 value3, T4 value4, T5 value5) { throw null; }
        public static int Combine<T1, T2, T3, T4, T5, T6>(T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6) { throw null; }
        public static int Combine<T1, T2, T3, T4, T5, T6, T7>(T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7) { throw null; }
        public static int Combine<T1, T2, T3, T4, T5, T6, T7, T8>(T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8) { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
        [System.ObsoleteAttribute("HashCode is a mutable struct and should not be compared with other HashCodes.", true)]
        public override bool Equals(object obj) { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
        [System.ObsoleteAttribute("HashCode is a mutable struct and should not be compared with other HashCodes. Use ToHashCode to retrieve the computed hash code.", true)]
        public override int GetHashCode() { throw null; }
        public int ToHashCode() { throw null; }
    }
    public partial class HttpStyleUriParser : System.UriParser
    {
        public HttpStyleUriParser() { }
    }
    public partial interface IAsyncResult
    {
        object AsyncState { get; }
        System.Threading.WaitHandle AsyncWaitHandle { get; }
        bool CompletedSynchronously { get; }
        bool IsCompleted { get; }
    }
    public partial interface ICloneable
    {
        object Clone();
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
    public sealed partial class IndexOutOfRangeException : System.SystemException
    {
        public IndexOutOfRangeException() { }
        public IndexOutOfRangeException(string message) { }
        public IndexOutOfRangeException(string message, System.Exception innerException) { }
    }
    public sealed partial class InsufficientExecutionStackException : System.SystemException
    {
        public InsufficientExecutionStackException() { }
        public InsufficientExecutionStackException(string message) { }
        public InsufficientExecutionStackException(string message, System.Exception innerException) { }
    }
    public sealed partial class InsufficientMemoryException : System.OutOfMemoryException
    {
        public InsufficientMemoryException() { }
        public InsufficientMemoryException(string message) { }
        public InsufficientMemoryException(string message, System.Exception innerException) { }
    }
    public partial struct Int16 : System.IComparable, System.IComparable<short>, System.IConvertible, System.IEquatable<short>, System.IFormattable
    {
        private short _dummy;
        public const short MaxValue = (short)32767;
        public const short MinValue = (short)-32768;
        public int CompareTo(System.Int16 value) { throw null; }
        public int CompareTo(object value) { throw null; }
        public bool Equals(System.Int16 obj) { throw null; }
        public override bool Equals(object obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public System.TypeCode GetTypeCode() { throw null; }
        public static System.Int16 Parse(System.ReadOnlySpan<char> s, System.Globalization.NumberStyles style=(System.Globalization.NumberStyles)(7), System.IFormatProvider provider=null) { throw null; }
        public static System.Int16 Parse(string s) { throw null; }
        public static System.Int16 Parse(string s, System.Globalization.NumberStyles style) { throw null; }
        public static System.Int16 Parse(string s, System.Globalization.NumberStyles style, System.IFormatProvider provider) { throw null; }
        public static System.Int16 Parse(string s, System.IFormatProvider provider) { throw null; }
        bool System.IConvertible.ToBoolean(System.IFormatProvider provider) { throw null; }
        byte System.IConvertible.ToByte(System.IFormatProvider provider) { throw null; }
        char System.IConvertible.ToChar(System.IFormatProvider provider) { throw null; }
        System.DateTime System.IConvertible.ToDateTime(System.IFormatProvider provider) { throw null; }
        decimal System.IConvertible.ToDecimal(System.IFormatProvider provider) { throw null; }
        double System.IConvertible.ToDouble(System.IFormatProvider provider) { throw null; }
        System.Int16 System.IConvertible.ToInt16(System.IFormatProvider provider) { throw null; }
        int System.IConvertible.ToInt32(System.IFormatProvider provider) { throw null; }
        long System.IConvertible.ToInt64(System.IFormatProvider provider) { throw null; }
        sbyte System.IConvertible.ToSByte(System.IFormatProvider provider) { throw null; }
        float System.IConvertible.ToSingle(System.IFormatProvider provider) { throw null; }
        object System.IConvertible.ToType(System.Type type, System.IFormatProvider provider) { throw null; }
        ushort System.IConvertible.ToUInt16(System.IFormatProvider provider) { throw null; }
        uint System.IConvertible.ToUInt32(System.IFormatProvider provider) { throw null; }
        ulong System.IConvertible.ToUInt64(System.IFormatProvider provider) { throw null; }
        public override string ToString() { throw null; }
        public string ToString(System.IFormatProvider provider) { throw null; }
        public string ToString(string format) { throw null; }
        public string ToString(string format, System.IFormatProvider provider) { throw null; }
        public bool TryFormat(System.Span<char> destination, out int charsWritten, System.ReadOnlySpan<char> format=default(System.ReadOnlySpan<char>), System.IFormatProvider provider=null) { throw null; }
        public static bool TryParse(System.ReadOnlySpan<char> s, System.Globalization.NumberStyles style, System.IFormatProvider provider, out System.Int16 result) { throw null; }
        public static bool TryParse(System.ReadOnlySpan<char> s, out System.Int16 result) { throw null; }
        public static bool TryParse(string s, System.Globalization.NumberStyles style, System.IFormatProvider provider, out System.Int16 result) { throw null; }
        public static bool TryParse(string s, out System.Int16 result) { throw null; }
    }
    public partial struct Int32 : System.IComparable, System.IComparable<int>, System.IConvertible, System.IEquatable<int>, System.IFormattable
    {
        private int _dummy;
        public const int MaxValue = 2147483647;
        public const int MinValue = -2147483648;
        public System.Int32 CompareTo(System.Int32 value) { throw null; }
        public System.Int32 CompareTo(object value) { throw null; }
        public bool Equals(System.Int32 obj) { throw null; }
        public override bool Equals(object obj) { throw null; }
        public override System.Int32 GetHashCode() { throw null; }
        public System.TypeCode GetTypeCode() { throw null; }
        public static System.Int32 Parse(System.ReadOnlySpan<char> s, System.Globalization.NumberStyles style=(System.Globalization.NumberStyles)(7), System.IFormatProvider provider=null) { throw null; }
        public static System.Int32 Parse(string s) { throw null; }
        public static System.Int32 Parse(string s, System.Globalization.NumberStyles style) { throw null; }
        public static System.Int32 Parse(string s, System.Globalization.NumberStyles style, System.IFormatProvider provider) { throw null; }
        public static System.Int32 Parse(string s, System.IFormatProvider provider) { throw null; }
        bool System.IConvertible.ToBoolean(System.IFormatProvider provider) { throw null; }
        byte System.IConvertible.ToByte(System.IFormatProvider provider) { throw null; }
        char System.IConvertible.ToChar(System.IFormatProvider provider) { throw null; }
        System.DateTime System.IConvertible.ToDateTime(System.IFormatProvider provider) { throw null; }
        decimal System.IConvertible.ToDecimal(System.IFormatProvider provider) { throw null; }
        double System.IConvertible.ToDouble(System.IFormatProvider provider) { throw null; }
        short System.IConvertible.ToInt16(System.IFormatProvider provider) { throw null; }
        System.Int32 System.IConvertible.ToInt32(System.IFormatProvider provider) { throw null; }
        long System.IConvertible.ToInt64(System.IFormatProvider provider) { throw null; }
        sbyte System.IConvertible.ToSByte(System.IFormatProvider provider) { throw null; }
        float System.IConvertible.ToSingle(System.IFormatProvider provider) { throw null; }
        object System.IConvertible.ToType(System.Type type, System.IFormatProvider provider) { throw null; }
        ushort System.IConvertible.ToUInt16(System.IFormatProvider provider) { throw null; }
        uint System.IConvertible.ToUInt32(System.IFormatProvider provider) { throw null; }
        ulong System.IConvertible.ToUInt64(System.IFormatProvider provider) { throw null; }
        public override string ToString() { throw null; }
        public string ToString(System.IFormatProvider provider) { throw null; }
        public string ToString(string format) { throw null; }
        public string ToString(string format, System.IFormatProvider provider) { throw null; }
        public bool TryFormat(System.Span<char> destination, out System.Int32 charsWritten, System.ReadOnlySpan<char> format=default(System.ReadOnlySpan<char>), System.IFormatProvider provider=null) { throw null; }
        public static bool TryParse(System.ReadOnlySpan<char> s, System.Globalization.NumberStyles style, System.IFormatProvider provider, out System.Int32 result) { throw null; }
        public static bool TryParse(System.ReadOnlySpan<char> s, out System.Int32 result) { throw null; }
        public static bool TryParse(string s, System.Globalization.NumberStyles style, System.IFormatProvider provider, out System.Int32 result) { throw null; }
        public static bool TryParse(string s, out System.Int32 result) { throw null; }
    }
    public partial struct Int64 : System.IComparable, System.IComparable<long>, System.IConvertible, System.IEquatable<long>, System.IFormattable
    {
        private long _dummy;
        public const long MaxValue = (long)9223372036854775807;
        public const long MinValue = (long)-9223372036854775808;
        public int CompareTo(System.Int64 value) { throw null; }
        public int CompareTo(object value) { throw null; }
        public bool Equals(System.Int64 obj) { throw null; }
        public override bool Equals(object obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public System.TypeCode GetTypeCode() { throw null; }
        public static System.Int64 Parse(System.ReadOnlySpan<char> s, System.Globalization.NumberStyles style=(System.Globalization.NumberStyles)(7), System.IFormatProvider provider=null) { throw null; }
        public static System.Int64 Parse(string s) { throw null; }
        public static System.Int64 Parse(string s, System.Globalization.NumberStyles style) { throw null; }
        public static System.Int64 Parse(string s, System.Globalization.NumberStyles style, System.IFormatProvider provider) { throw null; }
        public static System.Int64 Parse(string s, System.IFormatProvider provider) { throw null; }
        bool System.IConvertible.ToBoolean(System.IFormatProvider provider) { throw null; }
        byte System.IConvertible.ToByte(System.IFormatProvider provider) { throw null; }
        char System.IConvertible.ToChar(System.IFormatProvider provider) { throw null; }
        System.DateTime System.IConvertible.ToDateTime(System.IFormatProvider provider) { throw null; }
        decimal System.IConvertible.ToDecimal(System.IFormatProvider provider) { throw null; }
        double System.IConvertible.ToDouble(System.IFormatProvider provider) { throw null; }
        short System.IConvertible.ToInt16(System.IFormatProvider provider) { throw null; }
        int System.IConvertible.ToInt32(System.IFormatProvider provider) { throw null; }
        System.Int64 System.IConvertible.ToInt64(System.IFormatProvider provider) { throw null; }
        sbyte System.IConvertible.ToSByte(System.IFormatProvider provider) { throw null; }
        float System.IConvertible.ToSingle(System.IFormatProvider provider) { throw null; }
        object System.IConvertible.ToType(System.Type type, System.IFormatProvider provider) { throw null; }
        ushort System.IConvertible.ToUInt16(System.IFormatProvider provider) { throw null; }
        uint System.IConvertible.ToUInt32(System.IFormatProvider provider) { throw null; }
        ulong System.IConvertible.ToUInt64(System.IFormatProvider provider) { throw null; }
        public override string ToString() { throw null; }
        public string ToString(System.IFormatProvider provider) { throw null; }
        public string ToString(string format) { throw null; }
        public string ToString(string format, System.IFormatProvider provider) { throw null; }
        public bool TryFormat(System.Span<char> destination, out int charsWritten, System.ReadOnlySpan<char> format=default(System.ReadOnlySpan<char>), System.IFormatProvider provider=null) { throw null; }
        public static bool TryParse(System.ReadOnlySpan<char> s, System.Globalization.NumberStyles style, System.IFormatProvider provider, out System.Int64 result) { throw null; }
        public static bool TryParse(System.ReadOnlySpan<char> s, out System.Int64 result) { throw null; }
        public static bool TryParse(string s, System.Globalization.NumberStyles style, System.IFormatProvider provider, out System.Int64 result) { throw null; }
        public static bool TryParse(string s, out System.Int64 result) { throw null; }
    }
    public partial struct IntPtr : System.IEquatable<System.IntPtr>, System.Runtime.Serialization.ISerializable
    {
        private int _dummy;
        public static readonly System.IntPtr Zero;
        public IntPtr(int value) { throw null; }
        public IntPtr(long value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public unsafe IntPtr(void* value) { throw null; }
        public static int Size { get { throw null; } }
        public static System.IntPtr Add(System.IntPtr pointer, int offset) { throw null; }
        public override bool Equals(object obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public static System.IntPtr operator +(System.IntPtr pointer, int offset) { throw null; }
        public static bool operator ==(System.IntPtr value1, System.IntPtr value2) { throw null; }
        public static explicit operator System.IntPtr (int value) { throw null; }
        public static explicit operator System.IntPtr (long value) { throw null; }
        public static explicit operator int (System.IntPtr value) { throw null; }
        public static explicit operator long (System.IntPtr value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public unsafe static explicit operator void* (System.IntPtr value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public unsafe static explicit operator System.IntPtr (void* value) { throw null; }
        public static bool operator !=(System.IntPtr value1, System.IntPtr value2) { throw null; }
        public static System.IntPtr operator -(System.IntPtr pointer, int offset) { throw null; }
        public static System.IntPtr Subtract(System.IntPtr pointer, int offset) { throw null; }
        bool System.IEquatable<System.IntPtr>.Equals(System.IntPtr value) { throw null; }
        void System.Runtime.Serialization.ISerializable.GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public int ToInt32() { throw null; }
        public long ToInt64() { throw null; }
        [System.CLSCompliantAttribute(false)]
        public unsafe void* ToPointer() { throw null; }
        public override string ToString() { throw null; }
        public string ToString(string format) { throw null; }
    }
    public partial class InvalidCastException : System.SystemException
    {
        public InvalidCastException() { }
        protected InvalidCastException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public InvalidCastException(string message) { }
        public InvalidCastException(string message, System.Exception innerException) { }
        public InvalidCastException(string message, int errorCode) { }
    }
    public partial class InvalidOperationException : System.SystemException
    {
        public InvalidOperationException() { }
        protected InvalidOperationException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public InvalidOperationException(string message) { }
        public InvalidOperationException(string message, System.Exception innerException) { }
    }
    public sealed partial class InvalidProgramException : System.SystemException
    {
        public InvalidProgramException() { }
        public InvalidProgramException(string message) { }
        public InvalidProgramException(string message, System.Exception inner) { }
    }
    public partial class InvalidTimeZoneException : System.Exception
    {
        public InvalidTimeZoneException() { }
        protected InvalidTimeZoneException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
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
        public Lazy(T value) { }
        public bool IsValueCreated { get { throw null; } }
        public T Value { get { throw null; } }
        public override string ToString() { throw null; }
    }
    public partial class Lazy<T, TMetadata> : System.Lazy<T>
    {
        public Lazy(System.Func<T> valueFactory, TMetadata metadata) { }
        public Lazy(System.Func<T> valueFactory, TMetadata metadata, bool isThreadSafe) { }
        public Lazy(System.Func<T> valueFactory, TMetadata metadata, System.Threading.LazyThreadSafetyMode mode) { }
        public Lazy(TMetadata metadata) { }
        public Lazy(TMetadata metadata, bool isThreadSafe) { }
        public Lazy(TMetadata metadata, System.Threading.LazyThreadSafetyMode mode) { }
        public TMetadata Metadata { get { throw null; } }
    }
    public partial class LdapStyleUriParser : System.UriParser
    {
        public LdapStyleUriParser() { }
    }
    public abstract partial class MarshalByRefObject
    {
        protected MarshalByRefObject() { }
        public object GetLifetimeService() { throw null; }
        public virtual object InitializeLifetimeService() { throw null; }
        protected System.MarshalByRefObject MemberwiseClone(bool cloneIdentity) { throw null; }
    }
    public partial class MemberAccessException : System.SystemException
    {
        public MemberAccessException() { }
        protected MemberAccessException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public MemberAccessException(string message) { }
        public MemberAccessException(string message, System.Exception inner) { }
    }
    public readonly partial struct Memory<T>
    {
        private readonly object _dummy;
        public Memory(T[] array) { throw null; }
        public Memory(T[] array, int start, int length) { throw null; }
        public static System.Memory<T> Empty { get { throw null; } }
        public bool IsEmpty { get { throw null; } }
        public int Length { get { throw null; } }
        public System.Span<T> Span { get { throw null; } }
        public void CopyTo(System.Memory<T> destination) { }
        public bool Equals(System.Memory<T> other) { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
        public override bool Equals(object obj) { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
        public override int GetHashCode() { throw null; }
        public static implicit operator System.Memory<T> (System.ArraySegment<T> arraySegment) { throw null; }
        public static implicit operator System.ReadOnlyMemory<T> (System.Memory<T> memory) { throw null; }
        public static implicit operator System.Memory<T> (T[] array) { throw null; }
        public System.Buffers.MemoryHandle Retain(bool pin=false) { throw null; }
        public System.Memory<T> Slice(int start) { throw null; }
        public System.Memory<T> Slice(int start, int length) { throw null; }
        public T[] ToArray() { throw null; }
        public bool TryCopyTo(System.Memory<T> destination) { throw null; }
        public bool TryGetArray(out System.ArraySegment<T> arraySegment) { throw null; }
    }
    public partial class MethodAccessException : System.MemberAccessException
    {
        public MethodAccessException() { }
        protected MethodAccessException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public MethodAccessException(string message) { }
        public MethodAccessException(string message, System.Exception inner) { }
    }
    public enum MidpointRounding
    {
        AwayFromZero = 1,
        ToEven = 0,
    }
    public partial class MissingFieldException : System.MissingMemberException, System.Runtime.Serialization.ISerializable
    {
        public MissingFieldException() { }
        protected MissingFieldException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public MissingFieldException(string message) { }
        public MissingFieldException(string message, System.Exception inner) { }
        public MissingFieldException(string className, string fieldName) { }
        public override string Message { get { throw null; } }
    }
    public partial class MissingMemberException : System.MemberAccessException, System.Runtime.Serialization.ISerializable
    {
        protected string ClassName;
        protected string MemberName;
        protected byte[] Signature;
        public MissingMemberException() { }
        protected MissingMemberException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public MissingMemberException(string message) { }
        public MissingMemberException(string message, System.Exception inner) { }
        public MissingMemberException(string className, string memberName) { }
        public override string Message { get { throw null; } }
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    }
    public partial class MissingMethodException : System.MissingMemberException
    {
        public MissingMethodException() { }
        protected MissingMethodException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public MissingMethodException(string message) { }
        public MissingMethodException(string message, System.Exception inner) { }
        public MissingMethodException(string className, string methodName) { }
        public override string Message { get { throw null; } }
    }
    public partial struct ModuleHandle
    {
        private object _dummy;
        public static readonly System.ModuleHandle EmptyHandle;
        public int MDStreamVersion { get { throw null; } }
        public bool Equals(System.ModuleHandle handle) { throw null; }
        public override bool Equals(object obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public System.RuntimeFieldHandle GetRuntimeFieldHandleFromMetadataToken(int fieldToken) { throw null; }
        public System.RuntimeMethodHandle GetRuntimeMethodHandleFromMetadataToken(int methodToken) { throw null; }
        public System.RuntimeTypeHandle GetRuntimeTypeHandleFromMetadataToken(int typeToken) { throw null; }
        public static bool operator ==(System.ModuleHandle left, System.ModuleHandle right) { throw null; }
        public static bool operator !=(System.ModuleHandle left, System.ModuleHandle right) { throw null; }
        public System.RuntimeFieldHandle ResolveFieldHandle(int fieldToken) { throw null; }
        public System.RuntimeFieldHandle ResolveFieldHandle(int fieldToken, System.RuntimeTypeHandle[] typeInstantiationContext, System.RuntimeTypeHandle[] methodInstantiationContext) { throw null; }
        public System.RuntimeMethodHandle ResolveMethodHandle(int methodToken) { throw null; }
        public System.RuntimeMethodHandle ResolveMethodHandle(int methodToken, System.RuntimeTypeHandle[] typeInstantiationContext, System.RuntimeTypeHandle[] methodInstantiationContext) { throw null; }
        public System.RuntimeTypeHandle ResolveTypeHandle(int typeToken) { throw null; }
        public System.RuntimeTypeHandle ResolveTypeHandle(int typeToken, System.RuntimeTypeHandle[] typeInstantiationContext, System.RuntimeTypeHandle[] methodInstantiationContext) { throw null; }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(64))]
    public sealed partial class MTAThreadAttribute : System.Attribute
    {
        public MTAThreadAttribute() { }
    }
    public abstract partial class MulticastDelegate : System.Delegate
    {
        protected MulticastDelegate(object target, string method) : base (default(object), default(string)) { }
        protected MulticastDelegate(System.Type target, string method) : base (default(object), default(string)) { }
        protected sealed override System.Delegate CombineImpl(System.Delegate follow) { throw null; }
        public sealed override bool Equals(object obj) { throw null; }
        public sealed override int GetHashCode() { throw null; }
        public sealed override System.Delegate[] GetInvocationList() { throw null; }
        protected override System.Reflection.MethodInfo GetMethodImpl() { throw null; }
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public static bool operator ==(System.MulticastDelegate d1, System.MulticastDelegate d2) { throw null; }
        public static bool operator !=(System.MulticastDelegate d1, System.MulticastDelegate d2) { throw null; }
        protected sealed override System.Delegate RemoveImpl(System.Delegate value) { throw null; }
    }
    public sealed partial class MulticastNotSupportedException : System.SystemException
    {
        public MulticastNotSupportedException() { }
        public MulticastNotSupportedException(string message) { }
        public MulticastNotSupportedException(string message, System.Exception inner) { }
    }
    public partial class NetPipeStyleUriParser : System.UriParser
    {
        public NetPipeStyleUriParser() { }
    }
    public partial class NetTcpStyleUriParser : System.UriParser
    {
        public NetTcpStyleUriParser() { }
    }
    public partial class NewsStyleUriParser : System.UriParser
    {
        public NewsStyleUriParser() { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(256), Inherited=false)]
    public sealed partial class NonSerializedAttribute : System.Attribute
    {
        public NonSerializedAttribute() { }
    }
    public partial class NotFiniteNumberException : System.ArithmeticException
    {
        public NotFiniteNumberException() { }
        public NotFiniteNumberException(double offendingNumber) { }
        protected NotFiniteNumberException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public NotFiniteNumberException(string message) { }
        public NotFiniteNumberException(string message, double offendingNumber) { }
        public NotFiniteNumberException(string message, double offendingNumber, System.Exception innerException) { }
        public NotFiniteNumberException(string message, System.Exception innerException) { }
        public double OffendingNumber { get { throw null; } }
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    }
    public partial class NotImplementedException : System.SystemException
    {
        public NotImplementedException() { }
        protected NotImplementedException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public NotImplementedException(string message) { }
        public NotImplementedException(string message, System.Exception inner) { }
    }
    public partial class NotSupportedException : System.SystemException
    {
        public NotSupportedException() { }
        protected NotSupportedException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public NotSupportedException(string message) { }
        public NotSupportedException(string message, System.Exception innerException) { }
    }
    public static partial class Nullable
    {
        public static int Compare<T>(System.Nullable<T> n1, System.Nullable<T> n2) where T : struct { throw null; }
        public static bool Equals<T>(System.Nullable<T> n1, System.Nullable<T> n2) where T : struct { throw null; }
        public static System.Type GetUnderlyingType(System.Type nullableType) { throw null; }
    }
    public partial struct Nullable<T> where T : struct
    {
        internal T value;
        public Nullable(T value) { throw null; }
        public bool HasValue { get { throw null; } }
        public T Value { get { throw null; } }
        public override bool Equals(object other) { throw null; }
        public override int GetHashCode() { throw null; }
        public T GetValueOrDefault() { throw null; }
        public T GetValueOrDefault(T defaultValue) { throw null; }
        public static explicit operator T (System.Nullable<T> value) { throw null; }
        public static implicit operator System.Nullable<T> (T value) { throw null; }
        public override string ToString() { throw null; }
    }
    public partial class NullReferenceException : System.SystemException
    {
        public NullReferenceException() { }
        protected NullReferenceException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public NullReferenceException(string message) { }
        public NullReferenceException(string message, System.Exception innerException) { }
    }
    public partial class Object
    {
        public Object() { }
        public virtual bool Equals(System.Object obj) { throw null; }
        public static bool Equals(System.Object objA, System.Object objB) { throw null; }
        ~Object() { }
        public virtual int GetHashCode() { throw null; }
        public System.Type GetType() { throw null; }
        protected System.Object MemberwiseClone() { throw null; }
        public static bool ReferenceEquals(System.Object objA, System.Object objB) { throw null; }
        public virtual string ToString() { throw null; }
    }
    public partial class ObjectDisposedException : System.InvalidOperationException
    {
        protected ObjectDisposedException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public ObjectDisposedException(string objectName) { }
        public ObjectDisposedException(string message, System.Exception innerException) { }
        public ObjectDisposedException(string objectName, string message) { }
        public override string Message { get { throw null; } }
        public string ObjectName { get { throw null; } }
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(6140), Inherited=false)]
    public sealed partial class ObsoleteAttribute : System.Attribute
    {
        public ObsoleteAttribute() { }
        public ObsoleteAttribute(string message) { }
        public ObsoleteAttribute(string message, bool error) { }
        public bool IsError { get { throw null; } }
        public string Message { get { throw null; } }
    }
    public partial class OutOfMemoryException : System.SystemException
    {
        public OutOfMemoryException() { }
        protected OutOfMemoryException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public OutOfMemoryException(string message) { }
        public OutOfMemoryException(string message, System.Exception innerException) { }
    }
    public partial class OverflowException : System.ArithmeticException
    {
        public OverflowException() { }
        protected OverflowException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public OverflowException(string message) { }
        public OverflowException(string message, System.Exception innerException) { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(2048), Inherited=true, AllowMultiple=false)]
    public sealed partial class ParamArrayAttribute : System.Attribute
    {
        public ParamArrayAttribute() { }
    }
    public partial class PlatformNotSupportedException : System.NotSupportedException
    {
        public PlatformNotSupportedException() { }
        protected PlatformNotSupportedException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public PlatformNotSupportedException(string message) { }
        public PlatformNotSupportedException(string message, System.Exception inner) { }
    }
    public delegate bool Predicate<in T>(T obj);
    public partial class RankException : System.SystemException
    {
        public RankException() { }
        protected RankException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public RankException(string message) { }
        public RankException(string message, System.Exception innerException) { }
    }
    public readonly partial struct ReadOnlyMemory<T>
    {
        private readonly object _dummy;
        public ReadOnlyMemory(T[] array) { throw null; }
        public ReadOnlyMemory(T[] array, int start, int length) { throw null; }
        public static System.ReadOnlyMemory<T> Empty { get { throw null; } }
        public bool IsEmpty { get { throw null; } }
        public int Length { get { throw null; } }
        public System.ReadOnlySpan<T> Span { get { throw null; } }
        public void CopyTo(System.Memory<T> destination) { }
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
        public override bool Equals(object obj) { throw null; }
        public bool Equals(System.ReadOnlyMemory<T> other) { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
        public override int GetHashCode() { throw null; }
        public static implicit operator System.ReadOnlyMemory<T> (System.ArraySegment<T> arraySegment) { throw null; }
        public static implicit operator System.ReadOnlyMemory<T> (T[] array) { throw null; }
        public System.Buffers.MemoryHandle Retain(bool pin=false) { throw null; }
        public System.ReadOnlyMemory<T> Slice(int start) { throw null; }
        public System.ReadOnlyMemory<T> Slice(int start, int length) { throw null; }
        public T[] ToArray() { throw null; }
        public bool TryCopyTo(System.Memory<T> destination) { throw null; }
    }
    public readonly ref partial struct ReadOnlySpan<T>
    {
        private readonly object _dummy;
        [System.CLSCompliantAttribute(false)]
        public unsafe ReadOnlySpan(void* pointer, int length) { throw null; }
        public ReadOnlySpan(T[] array) { throw null; }
        public ReadOnlySpan(T[] array, int start, int length) { throw null; }
        public static System.ReadOnlySpan<T> Empty { get { throw null; } }
        public bool IsEmpty { get { throw null; } }
        public ref readonly T this[int index] { get { throw null; } }
        public int Length { get { throw null; } }
        public void CopyTo(System.Span<T> destination) { }
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
        public static System.ReadOnlySpan<T> DangerousCreate(object obj, ref T objectData, int length) { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
        [System.ObsoleteAttribute("Equals() on ReadOnlySpan will always throw an exception. Use == instead.")]
        public override bool Equals(object obj) { throw null; }
        public System.ReadOnlySpan<T>.Enumerator GetEnumerator() { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
        [System.ObsoleteAttribute("GetHashCode() on ReadOnlySpan will always throw an exception.")]
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.ReadOnlySpan<T> left, System.ReadOnlySpan<T> right) { throw null; }
        public static implicit operator System.ReadOnlySpan<T> (System.ArraySegment<T> arraySegment) { throw null; }
        public static implicit operator System.ReadOnlySpan<T> (T[] array) { throw null; }
        public static bool operator !=(System.ReadOnlySpan<T> left, System.ReadOnlySpan<T> right) { throw null; }
        public System.ReadOnlySpan<T> Slice(int start) { throw null; }
        public System.ReadOnlySpan<T> Slice(int start, int length) { throw null; }
        public T[] ToArray() { throw null; }
        public bool TryCopyTo(System.Span<T> destination) { throw null; }
        public ref partial struct Enumerator
        {
            private object _dummy;
            public ref readonly T Current { get { throw null; } }
            public bool MoveNext() { throw null; }
        }
    }
    public partial class ResolveEventArgs : System.EventArgs
    {
        public ResolveEventArgs(string name) { }
        public ResolveEventArgs(string name, System.Reflection.Assembly requestingAssembly) { }
        public string Name { get { throw null; } }
        public System.Reflection.Assembly RequestingAssembly { get { throw null; } }
    }
    public ref partial struct RuntimeArgumentHandle
    {
        private int _dummy;
    }
    public partial struct RuntimeFieldHandle : System.Runtime.Serialization.ISerializable
    {
        private object _dummy;
        public System.IntPtr Value { get { throw null; } }
        public override bool Equals(object obj) { throw null; }
        public bool Equals(System.RuntimeFieldHandle handle) { throw null; }
        public override int GetHashCode() { throw null; }
        public void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public static bool operator ==(System.RuntimeFieldHandle left, System.RuntimeFieldHandle right) { throw null; }
        public static bool operator !=(System.RuntimeFieldHandle left, System.RuntimeFieldHandle right) { throw null; }
    }
    public partial struct RuntimeMethodHandle : System.Runtime.Serialization.ISerializable
    {
        private object _dummy;
        public System.IntPtr Value { get { throw null; } }
        public override bool Equals(object obj) { throw null; }
        public bool Equals(System.RuntimeMethodHandle handle) { throw null; }
        public System.IntPtr GetFunctionPointer() { throw null; }
        public override int GetHashCode() { throw null; }
        public void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public static bool operator ==(System.RuntimeMethodHandle left, System.RuntimeMethodHandle right) { throw null; }
        public static bool operator !=(System.RuntimeMethodHandle left, System.RuntimeMethodHandle right) { throw null; }
    }
    public partial struct RuntimeTypeHandle : System.Runtime.Serialization.ISerializable
    {
        private object _dummy;
        public System.IntPtr Value { get { throw null; } }
        public override bool Equals(object obj) { throw null; }
        public bool Equals(System.RuntimeTypeHandle handle) { throw null; }
        public override int GetHashCode() { throw null; }
        public System.ModuleHandle GetModuleHandle() { throw null; }
        public void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public static bool operator ==(object left, System.RuntimeTypeHandle right) { throw null; }
        public static bool operator ==(System.RuntimeTypeHandle left, object right) { throw null; }
        public static bool operator !=(object left, System.RuntimeTypeHandle right) { throw null; }
        public static bool operator !=(System.RuntimeTypeHandle left, object right) { throw null; }
    }
    [System.CLSCompliantAttribute(false)]
    public partial struct SByte : System.IComparable, System.IComparable<sbyte>, System.IConvertible, System.IEquatable<sbyte>, System.IFormattable
    {
        private sbyte _dummy;
        public const sbyte MaxValue = (sbyte)127;
        public const sbyte MinValue = (sbyte)-128;
        public int CompareTo(object obj) { throw null; }
        public int CompareTo(System.SByte value) { throw null; }
        public override bool Equals(object obj) { throw null; }
        public bool Equals(System.SByte obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public System.TypeCode GetTypeCode() { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.SByte Parse(System.ReadOnlySpan<char> s, System.Globalization.NumberStyles style=(System.Globalization.NumberStyles)(7), System.IFormatProvider provider=null) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.SByte Parse(string s) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.SByte Parse(string s, System.Globalization.NumberStyles style) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.SByte Parse(string s, System.Globalization.NumberStyles style, System.IFormatProvider provider) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.SByte Parse(string s, System.IFormatProvider provider) { throw null; }
        bool System.IConvertible.ToBoolean(System.IFormatProvider provider) { throw null; }
        byte System.IConvertible.ToByte(System.IFormatProvider provider) { throw null; }
        char System.IConvertible.ToChar(System.IFormatProvider provider) { throw null; }
        System.DateTime System.IConvertible.ToDateTime(System.IFormatProvider provider) { throw null; }
        decimal System.IConvertible.ToDecimal(System.IFormatProvider provider) { throw null; }
        double System.IConvertible.ToDouble(System.IFormatProvider provider) { throw null; }
        short System.IConvertible.ToInt16(System.IFormatProvider provider) { throw null; }
        int System.IConvertible.ToInt32(System.IFormatProvider provider) { throw null; }
        long System.IConvertible.ToInt64(System.IFormatProvider provider) { throw null; }
        System.SByte System.IConvertible.ToSByte(System.IFormatProvider provider) { throw null; }
        float System.IConvertible.ToSingle(System.IFormatProvider provider) { throw null; }
        object System.IConvertible.ToType(System.Type type, System.IFormatProvider provider) { throw null; }
        ushort System.IConvertible.ToUInt16(System.IFormatProvider provider) { throw null; }
        uint System.IConvertible.ToUInt32(System.IFormatProvider provider) { throw null; }
        ulong System.IConvertible.ToUInt64(System.IFormatProvider provider) { throw null; }
        public override string ToString() { throw null; }
        public string ToString(System.IFormatProvider provider) { throw null; }
        public string ToString(string format) { throw null; }
        public string ToString(string format, System.IFormatProvider provider) { throw null; }
        public bool TryFormat(System.Span<char> destination, out int charsWritten, System.ReadOnlySpan<char> format=default(System.ReadOnlySpan<char>), System.IFormatProvider provider=null) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static bool TryParse(System.ReadOnlySpan<char> s, System.Globalization.NumberStyles style, System.IFormatProvider provider, out System.SByte result) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static bool TryParse(System.ReadOnlySpan<char> s, out System.SByte result) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static bool TryParse(string s, System.Globalization.NumberStyles style, System.IFormatProvider provider, out System.SByte result) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static bool TryParse(string s, out System.SByte result) { throw null; }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(4124), Inherited=false)]
    public sealed partial class SerializableAttribute : System.Attribute
    {
        public SerializableAttribute() { }
    }
    public partial struct Single : System.IComparable, System.IComparable<float>, System.IConvertible, System.IEquatable<float>, System.IFormattable
    {
        private float _dummy;
        //public const float Epsilon = 1.401298E-45f;         -- defined in System.Runtime.Manual.cs
        //public const float MaxValue = 3.40282347E+38f;      -- defined in System.Runtime.Manual.cs
        //public const float MinValue = -3.40282347E+38f;     -- defined in System.Runtime.Manual.cs
        //public const float NaN = 0.0f / 0.0f;               -- defined in System.Runtime.Manual.cs
        //public const float NegativeInfinity = -1.0f / 0.0f; -- defined in System.Runtime.Manual.cs
        //public const float PositiveInfinity = 1.0f / 0.0f;  -- defined in System.Runtime.Manual.cs
        public int CompareTo(object value) { throw null; }
        public int CompareTo(System.Single value) { throw null; }
        public override bool Equals(object obj) { throw null; }
        public bool Equals(System.Single obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public System.TypeCode GetTypeCode() { throw null; }
        public static bool IsFinite(System.Single f) { throw null; }
        public static bool IsInfinity(System.Single f) { throw null; }
        public static bool IsNaN(System.Single f) { throw null; }
        public static bool IsNegative(System.Single f) { throw null; }
        public static bool IsNegativeInfinity(System.Single f) { throw null; }
        public static bool IsNormal(System.Single f) { throw null; }
        public static bool IsPositiveInfinity(System.Single f) { throw null; }
        public static bool IsSubnormal(System.Single f) { throw null; }
        public static bool operator ==(System.Single left, System.Single right) { throw null; }
        public static bool operator >(System.Single left, System.Single right) { throw null; }
        public static bool operator >=(System.Single left, System.Single right) { throw null; }
        public static bool operator !=(System.Single left, System.Single right) { throw null; }
        public static bool operator <(System.Single left, System.Single right) { throw null; }
        public static bool operator <=(System.Single left, System.Single right) { throw null; }
        public static System.Single Parse(System.ReadOnlySpan<char> s, System.Globalization.NumberStyles style=(System.Globalization.NumberStyles)(7), System.IFormatProvider provider=null) { throw null; }
        public static System.Single Parse(string s) { throw null; }
        public static System.Single Parse(string s, System.Globalization.NumberStyles style) { throw null; }
        public static System.Single Parse(string s, System.Globalization.NumberStyles style, System.IFormatProvider provider) { throw null; }
        public static System.Single Parse(string s, System.IFormatProvider provider) { throw null; }
        bool System.IConvertible.ToBoolean(System.IFormatProvider provider) { throw null; }
        byte System.IConvertible.ToByte(System.IFormatProvider provider) { throw null; }
        char System.IConvertible.ToChar(System.IFormatProvider provider) { throw null; }
        System.DateTime System.IConvertible.ToDateTime(System.IFormatProvider provider) { throw null; }
        decimal System.IConvertible.ToDecimal(System.IFormatProvider provider) { throw null; }
        double System.IConvertible.ToDouble(System.IFormatProvider provider) { throw null; }
        short System.IConvertible.ToInt16(System.IFormatProvider provider) { throw null; }
        int System.IConvertible.ToInt32(System.IFormatProvider provider) { throw null; }
        long System.IConvertible.ToInt64(System.IFormatProvider provider) { throw null; }
        sbyte System.IConvertible.ToSByte(System.IFormatProvider provider) { throw null; }
        System.Single System.IConvertible.ToSingle(System.IFormatProvider provider) { throw null; }
        object System.IConvertible.ToType(System.Type type, System.IFormatProvider provider) { throw null; }
        ushort System.IConvertible.ToUInt16(System.IFormatProvider provider) { throw null; }
        uint System.IConvertible.ToUInt32(System.IFormatProvider provider) { throw null; }
        ulong System.IConvertible.ToUInt64(System.IFormatProvider provider) { throw null; }
        public override string ToString() { throw null; }
        public string ToString(System.IFormatProvider provider) { throw null; }
        public string ToString(string format) { throw null; }
        public string ToString(string format, System.IFormatProvider provider) { throw null; }
        public bool TryFormat(System.Span<char> destination, out int charsWritten, System.ReadOnlySpan<char> format=default(System.ReadOnlySpan<char>), System.IFormatProvider provider=null) { throw null; }
        public static bool TryParse(System.ReadOnlySpan<char> s, System.Globalization.NumberStyles style, System.IFormatProvider provider, out System.Single result) { throw null; }
        public static bool TryParse(System.ReadOnlySpan<char> s, out System.Single result) { throw null; }
        public static bool TryParse(string s, System.Globalization.NumberStyles style, System.IFormatProvider provider, out System.Single result) { throw null; }
        public static bool TryParse(string s, out System.Single result) { throw null; }
    }
    public readonly ref partial struct Span<T>
    {
        private readonly object _dummy;
        [System.CLSCompliantAttribute(false)]
        public unsafe Span(void* pointer, int length) { throw null; }
        public Span(T[] array) { throw null; }
        public Span(T[] array, int start, int length) { throw null; }
        public static System.Span<T> Empty { get { throw null; } }
        public bool IsEmpty { get { throw null; } }
        public ref T this[int index] { get { throw null; } }
        public int Length { get { throw null; } }
        public void Clear() { }
        public void CopyTo(System.Span<T> destination) { }
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
        public static System.Span<T> DangerousCreate(object obj, ref T objectData, int length) { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
        [System.ObsoleteAttribute("Equals() on Span will always throw an exception. Use == instead.")]
        public override bool Equals(object obj) { throw null; }
        public void Fill(T value) { }
        public System.Span<T>.Enumerator GetEnumerator() { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
        [System.ObsoleteAttribute("GetHashCode() on Span will always throw an exception.")]
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Span<T> left, System.Span<T> right) { throw null; }
        public static implicit operator System.Span<T> (System.ArraySegment<T> arraySegment) { throw null; }
        public static implicit operator System.ReadOnlySpan<T> (System.Span<T> span) { throw null; }
        public static implicit operator System.Span<T> (T[] array) { throw null; }
        public static bool operator !=(System.Span<T> left, System.Span<T> right) { throw null; }
        public System.Span<T> Slice(int start) { throw null; }
        public System.Span<T> Slice(int start, int length) { throw null; }
        public T[] ToArray() { throw null; }
        public bool TryCopyTo(System.Span<T> destination) { throw null; }
        public ref partial struct Enumerator
        {
            private object _dummy;
            public ref T Current { get { throw null; } }
            public bool MoveNext() { throw null; }
        }
    }
    public sealed partial class StackOverflowException : System.SystemException
    {
        public StackOverflowException() { }
        public StackOverflowException(string message) { }
        public StackOverflowException(string message, System.Exception innerException) { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(64))]
    public sealed partial class STAThreadAttribute : System.Attribute
    {
        public STAThreadAttribute() { }
    }
    public sealed partial class String : System.Collections.Generic.IEnumerable<char>, System.Collections.IEnumerable, System.ICloneable, System.IComparable, System.IComparable<string>, System.IConvertible, System.IEquatable<string>
    {
        public static readonly string Empty;
        [System.CLSCompliantAttribute(false)]
        public unsafe String(char* value) { }
        [System.CLSCompliantAttribute(false)]
        public unsafe String(char* value, int startIndex, int length) { }
        public String(char c, int count) { }
        public String(char[] value) { }
        public String(char[] value, int startIndex, int length) { }
        public String(System.ReadOnlySpan<char> value) { }
        [System.CLSCompliantAttribute(false)]
        public unsafe String(sbyte* value) { }
        [System.CLSCompliantAttribute(false)]
        public unsafe String(sbyte* value, int startIndex, int length) { }
        [System.CLSCompliantAttribute(false)]
        public unsafe String(sbyte* value, int startIndex, int length, System.Text.Encoding enc) { }
        [System.Runtime.CompilerServices.IndexerName("Chars")]
        public char this[int index] { get { throw null; } }
        public int Length { get { throw null; } }
        public object Clone() { throw null; }
        public static int Compare(System.String strA, int indexA, System.String strB, int indexB, int length) { throw null; }
        public static int Compare(System.String strA, int indexA, System.String strB, int indexB, int length, bool ignoreCase) { throw null; }
        public static int Compare(System.String strA, int indexA, System.String strB, int indexB, int length, bool ignoreCase, System.Globalization.CultureInfo culture) { throw null; }
        public static int Compare(System.String strA, int indexA, System.String strB, int indexB, int length, System.Globalization.CultureInfo culture, System.Globalization.CompareOptions options) { throw null; }
        public static int Compare(System.String strA, int indexA, System.String strB, int indexB, int length, System.StringComparison comparisonType) { throw null; }
        public static int Compare(System.String strA, System.String strB) { throw null; }
        public static int Compare(System.String strA, System.String strB, bool ignoreCase) { throw null; }
        public static int Compare(System.String strA, System.String strB, bool ignoreCase, System.Globalization.CultureInfo culture) { throw null; }
        public static int Compare(System.String strA, System.String strB, System.Globalization.CultureInfo culture, System.Globalization.CompareOptions options) { throw null; }
        public static int Compare(System.String strA, System.String strB, System.StringComparison comparisonType) { throw null; }
        public static int CompareOrdinal(System.String strA, int indexA, System.String strB, int indexB, int length) { throw null; }
        public static int CompareOrdinal(System.String strA, System.String strB) { throw null; }
        public int CompareTo(object value) { throw null; }
        public int CompareTo(System.String strB) { throw null; }
        public static System.String Concat(System.Collections.Generic.IEnumerable<string> values) { throw null; }
        public static System.String Concat(object arg0) { throw null; }
        public static System.String Concat(object arg0, object arg1) { throw null; }
        public static System.String Concat(object arg0, object arg1, object arg2) { throw null; }
        public static System.String Concat(params object[] args) { throw null; }
        public static System.String Concat(System.String str0, System.String str1) { throw null; }
        public static System.String Concat(System.String str0, System.String str1, System.String str2) { throw null; }
        public static System.String Concat(System.String str0, System.String str1, System.String str2, System.String str3) { throw null; }
        public static System.String Concat(params string[] values) { throw null; }
        public static System.String Concat<T>(System.Collections.Generic.IEnumerable<T> values) { throw null; }
        public bool Contains(char value) { throw null; }
        public bool Contains(char value, System.StringComparison comparisonType) { throw null; }
        public bool Contains(System.String value) { throw null; }
        public bool Contains(System.String value, System.StringComparison comparisonType) { throw null; }
        public static System.String Copy(System.String str) { throw null; }
        public void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count) { }
        public static System.String Create<TState>(int length, TState state, System.Buffers.SpanAction<char, TState> action) { throw null; }
        public bool EndsWith(char value) { throw null; }
        public bool EndsWith(System.String value) { throw null; }
        public bool EndsWith(System.String value, bool ignoreCase, System.Globalization.CultureInfo culture) { throw null; }
        public bool EndsWith(System.String value, System.StringComparison comparisonType) { throw null; }
        public override bool Equals(object obj) { throw null; }
        public bool Equals(System.String value) { throw null; }
        public static bool Equals(System.String a, System.String b) { throw null; }
        public static bool Equals(System.String a, System.String b, System.StringComparison comparisonType) { throw null; }
        public bool Equals(System.String value, System.StringComparison comparisonType) { throw null; }
        public static System.String Format(System.IFormatProvider provider, System.String format, object arg0) { throw null; }
        public static System.String Format(System.IFormatProvider provider, System.String format, object arg0, object arg1) { throw null; }
        public static System.String Format(System.IFormatProvider provider, System.String format, object arg0, object arg1, object arg2) { throw null; }
        public static System.String Format(System.IFormatProvider provider, System.String format, params object[] args) { throw null; }
        public static System.String Format(System.String format, object arg0) { throw null; }
        public static System.String Format(System.String format, object arg0, object arg1) { throw null; }
        public static System.String Format(System.String format, object arg0, object arg1, object arg2) { throw null; }
        public static System.String Format(System.String format, params object[] args) { throw null; }
        public System.CharEnumerator GetEnumerator() { throw null; }
        public override int GetHashCode() { throw null; }
        public int GetHashCode(System.StringComparison comparisonType) { throw null; }
        public System.TypeCode GetTypeCode() { throw null; }
        public int IndexOf(char value) { throw null; }
        public int IndexOf(char value, int startIndex) { throw null; }
        public int IndexOf(char value, int startIndex, int count) { throw null; }
        public int IndexOf(char value, System.StringComparison comparisonType) { throw null; }
        public int IndexOf(System.String value) { throw null; }
        public int IndexOf(System.String value, int startIndex) { throw null; }
        public int IndexOf(System.String value, int startIndex, int count) { throw null; }
        public int IndexOf(System.String value, int startIndex, int count, System.StringComparison comparisonType) { throw null; }
        public int IndexOf(System.String value, int startIndex, System.StringComparison comparisonType) { throw null; }
        public int IndexOf(System.String value, System.StringComparison comparisonType) { throw null; }
        public int IndexOfAny(char[] anyOf) { throw null; }
        public int IndexOfAny(char[] anyOf, int startIndex) { throw null; }
        public int IndexOfAny(char[] anyOf, int startIndex, int count) { throw null; }
        public System.String Insert(int startIndex, System.String value) { throw null; }
        public static System.String Intern(System.String str) { throw null; }
        public static System.String IsInterned(System.String str) { throw null; }
        public bool IsNormalized() { throw null; }
        public bool IsNormalized(System.Text.NormalizationForm normalizationForm) { throw null; }
        public static bool IsNullOrEmpty(System.String value) { throw null; }
        public static bool IsNullOrWhiteSpace(System.String value) { throw null; }
        public static System.String Join(char separator, params object[] values) { throw null; }
        public static System.String Join(char separator, params string[] value) { throw null; }
        public static System.String Join(char separator, string[] value, int startIndex, int count) { throw null; }
        public static System.String Join(System.String separator, System.Collections.Generic.IEnumerable<string> values) { throw null; }
        public static System.String Join(System.String separator, params object[] values) { throw null; }
        public static System.String Join(System.String separator, params string[] value) { throw null; }
        public static System.String Join(System.String separator, string[] value, int startIndex, int count) { throw null; }
        public static System.String Join<T>(char separator, System.Collections.Generic.IEnumerable<T> values) { throw null; }
        public static System.String Join<T>(System.String separator, System.Collections.Generic.IEnumerable<T> values) { throw null; }
        public int LastIndexOf(char value) { throw null; }
        public int LastIndexOf(char value, int startIndex) { throw null; }
        public int LastIndexOf(char value, int startIndex, int count) { throw null; }
        public int LastIndexOf(System.String value) { throw null; }
        public int LastIndexOf(System.String value, int startIndex) { throw null; }
        public int LastIndexOf(System.String value, int startIndex, int count) { throw null; }
        public int LastIndexOf(System.String value, int startIndex, int count, System.StringComparison comparisonType) { throw null; }
        public int LastIndexOf(System.String value, int startIndex, System.StringComparison comparisonType) { throw null; }
        public int LastIndexOf(System.String value, System.StringComparison comparisonType) { throw null; }
        public int LastIndexOfAny(char[] anyOf) { throw null; }
        public int LastIndexOfAny(char[] anyOf, int startIndex) { throw null; }
        public int LastIndexOfAny(char[] anyOf, int startIndex, int count) { throw null; }
        public System.String Normalize() { throw null; }
        public System.String Normalize(System.Text.NormalizationForm normalizationForm) { throw null; }
        public static bool operator ==(System.String a, System.String b) { throw null; }
        public static implicit operator System.ReadOnlySpan<char> (System.String value) { throw null; }
        public static bool operator !=(System.String a, System.String b) { throw null; }
        public System.String PadLeft(int totalWidth) { throw null; }
        public System.String PadLeft(int totalWidth, char paddingChar) { throw null; }
        public System.String PadRight(int totalWidth) { throw null; }
        public System.String PadRight(int totalWidth, char paddingChar) { throw null; }
        public System.String Remove(int startIndex) { throw null; }
        public System.String Remove(int startIndex, int count) { throw null; }
        public System.String Replace(char oldChar, char newChar) { throw null; }
        public System.String Replace(System.String oldValue, System.String newValue) { throw null; }
        public System.String Replace(System.String oldValue, System.String newValue, bool ignoreCase, System.Globalization.CultureInfo culture) { throw null; }
        public System.String Replace(System.String oldValue, System.String newValue, System.StringComparison comparisonType) { throw null; }
        public string[] Split(char separator, int count, System.StringSplitOptions options=(System.StringSplitOptions)(0)) { throw null; }
        public string[] Split(char separator, System.StringSplitOptions options=(System.StringSplitOptions)(0)) { throw null; }
        public string[] Split(params char[] separator) { throw null; }
        public string[] Split(char[] separator, int count) { throw null; }
        public string[] Split(char[] separator, int count, System.StringSplitOptions options) { throw null; }
        public string[] Split(char[] separator, System.StringSplitOptions options) { throw null; }
        public string[] Split(System.String separator, int count, System.StringSplitOptions options=(System.StringSplitOptions)(0)) { throw null; }
        public string[] Split(System.String separator, System.StringSplitOptions options=(System.StringSplitOptions)(0)) { throw null; }
        public string[] Split(string[] separator, int count, System.StringSplitOptions options) { throw null; }
        public string[] Split(string[] separator, System.StringSplitOptions options) { throw null; }
        public bool StartsWith(char value) { throw null; }
        public bool StartsWith(System.String value) { throw null; }
        public bool StartsWith(System.String value, bool ignoreCase, System.Globalization.CultureInfo culture) { throw null; }
        public bool StartsWith(System.String value, System.StringComparison comparisonType) { throw null; }
        public System.String Substring(int startIndex) { throw null; }
        public System.String Substring(int startIndex, int length) { throw null; }
        System.Collections.Generic.IEnumerator<char> System.Collections.Generic.IEnumerable<System.Char>.GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        bool System.IConvertible.ToBoolean(System.IFormatProvider provider) { throw null; }
        byte System.IConvertible.ToByte(System.IFormatProvider provider) { throw null; }
        char System.IConvertible.ToChar(System.IFormatProvider provider) { throw null; }
        System.DateTime System.IConvertible.ToDateTime(System.IFormatProvider provider) { throw null; }
        decimal System.IConvertible.ToDecimal(System.IFormatProvider provider) { throw null; }
        double System.IConvertible.ToDouble(System.IFormatProvider provider) { throw null; }
        short System.IConvertible.ToInt16(System.IFormatProvider provider) { throw null; }
        int System.IConvertible.ToInt32(System.IFormatProvider provider) { throw null; }
        long System.IConvertible.ToInt64(System.IFormatProvider provider) { throw null; }
        sbyte System.IConvertible.ToSByte(System.IFormatProvider provider) { throw null; }
        float System.IConvertible.ToSingle(System.IFormatProvider provider) { throw null; }
        object System.IConvertible.ToType(System.Type type, System.IFormatProvider provider) { throw null; }
        ushort System.IConvertible.ToUInt16(System.IFormatProvider provider) { throw null; }
        uint System.IConvertible.ToUInt32(System.IFormatProvider provider) { throw null; }
        ulong System.IConvertible.ToUInt64(System.IFormatProvider provider) { throw null; }
        public char[] ToCharArray() { throw null; }
        public char[] ToCharArray(int startIndex, int length) { throw null; }
        public System.String ToLower() { throw null; }
        public System.String ToLower(System.Globalization.CultureInfo culture) { throw null; }
        public System.String ToLowerInvariant() { throw null; }
        public override System.String ToString() { throw null; }
        public System.String ToString(System.IFormatProvider provider) { throw null; }
        public System.String ToUpper() { throw null; }
        public System.String ToUpper(System.Globalization.CultureInfo culture) { throw null; }
        public System.String ToUpperInvariant() { throw null; }
        public System.String Trim() { throw null; }
        public System.String Trim(char trimChar) { throw null; }
        public System.String Trim(params char[] trimChars) { throw null; }
        public System.String TrimEnd() { throw null; }
        public System.String TrimEnd(char trimChar) { throw null; }
        public System.String TrimEnd(params char[] trimChars) { throw null; }
        public System.String TrimStart() { throw null; }
        public System.String TrimStart(char trimChar) { throw null; }
        public System.String TrimStart(params char[] trimChars) { throw null; }
    }
    public enum StringComparison
    {
        CurrentCulture = 0,
        CurrentCultureIgnoreCase = 1,
        InvariantCulture = 2,
        InvariantCultureIgnoreCase = 3,
        Ordinal = 4,
        OrdinalIgnoreCase = 5,
    }
    [System.FlagsAttribute]
    public enum StringSplitOptions
    {
        None = 0,
        RemoveEmptyEntries = 1,
    }
    public partial class SystemException : System.Exception
    {
        public SystemException() { }
        protected SystemException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public SystemException(string message) { }
        public SystemException(string message, System.Exception innerException) { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(256), Inherited=false)]
    public partial class ThreadStaticAttribute : System.Attribute
    {
        public ThreadStaticAttribute() { }
    }
    public partial class TimeoutException : System.SystemException
    {
        public TimeoutException() { }
        protected TimeoutException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public TimeoutException(string message) { }
        public TimeoutException(string message, System.Exception innerException) { }
    }
    public partial struct TimeSpan : System.IComparable, System.IComparable<System.TimeSpan>, System.IEquatable<System.TimeSpan>, System.IFormattable
    {
        private int _dummy;
        public static readonly System.TimeSpan MaxValue;
        public static readonly System.TimeSpan MinValue;
        public const long TicksPerDay = (long)864000000000;
        public const long TicksPerHour = (long)36000000000;
        public const long TicksPerMillisecond = (long)10000;
        public const long TicksPerMinute = (long)600000000;
        public const long TicksPerSecond = (long)10000000;
        public static readonly System.TimeSpan Zero;
        public TimeSpan(int hours, int minutes, int seconds) { throw null; }
        public TimeSpan(int days, int hours, int minutes, int seconds) { throw null; }
        public TimeSpan(int days, int hours, int minutes, int seconds, int milliseconds) { throw null; }
        public TimeSpan(long ticks) { throw null; }
        public int Days { get { throw null; } }
        public int Hours { get { throw null; } }
        public int Milliseconds { get { throw null; } }
        public int Minutes { get { throw null; } }
        public int Seconds { get { throw null; } }
        public long Ticks { get { throw null; } }
        public double TotalDays { get { throw null; } }
        public double TotalHours { get { throw null; } }
        public double TotalMilliseconds { get { throw null; } }
        public double TotalMinutes { get { throw null; } }
        public double TotalSeconds { get { throw null; } }
        public System.TimeSpan Add(System.TimeSpan ts) { throw null; }
        public static int Compare(System.TimeSpan t1, System.TimeSpan t2) { throw null; }
        public int CompareTo(object value) { throw null; }
        public int CompareTo(System.TimeSpan value) { throw null; }
        public System.TimeSpan Divide(double divisor) { throw null; }
        public double Divide(System.TimeSpan ts) { throw null; }
        public System.TimeSpan Duration() { throw null; }
        public override bool Equals(object value) { throw null; }
        public bool Equals(System.TimeSpan obj) { throw null; }
        public static bool Equals(System.TimeSpan t1, System.TimeSpan t2) { throw null; }
        public static System.TimeSpan FromDays(double value) { throw null; }
        public static System.TimeSpan FromHours(double value) { throw null; }
        public static System.TimeSpan FromMilliseconds(double value) { throw null; }
        public static System.TimeSpan FromMinutes(double value) { throw null; }
        public static System.TimeSpan FromSeconds(double value) { throw null; }
        public static System.TimeSpan FromTicks(long value) { throw null; }
        public override int GetHashCode() { throw null; }
        public System.TimeSpan Multiply(double factor) { throw null; }
        public System.TimeSpan Negate() { throw null; }
        public static System.TimeSpan operator +(System.TimeSpan t1, System.TimeSpan t2) { throw null; }
        public static System.TimeSpan operator /(System.TimeSpan timeSpan, double divisor) { throw null; }
        public static double operator /(System.TimeSpan t1, System.TimeSpan t2) { throw null; }
        public static bool operator ==(System.TimeSpan t1, System.TimeSpan t2) { throw null; }
        public static bool operator >(System.TimeSpan t1, System.TimeSpan t2) { throw null; }
        public static bool operator >=(System.TimeSpan t1, System.TimeSpan t2) { throw null; }
        public static bool operator !=(System.TimeSpan t1, System.TimeSpan t2) { throw null; }
        public static bool operator <(System.TimeSpan t1, System.TimeSpan t2) { throw null; }
        public static bool operator <=(System.TimeSpan t1, System.TimeSpan t2) { throw null; }
        public static System.TimeSpan operator *(double factor, System.TimeSpan timeSpan) { throw null; }
        public static System.TimeSpan operator *(System.TimeSpan timeSpan, double factor) { throw null; }
        public static System.TimeSpan operator -(System.TimeSpan t1, System.TimeSpan t2) { throw null; }
        public static System.TimeSpan operator -(System.TimeSpan t) { throw null; }
        public static System.TimeSpan operator +(System.TimeSpan t) { throw null; }
        public static System.TimeSpan Parse(System.ReadOnlySpan<char> input, System.IFormatProvider formatProvider=null) { throw null; }
        public static System.TimeSpan Parse(string s) { throw null; }
        public static System.TimeSpan Parse(string input, System.IFormatProvider formatProvider) { throw null; }
        public static System.TimeSpan ParseExact(System.ReadOnlySpan<char> input, System.ReadOnlySpan<char> format, System.IFormatProvider formatProvider, System.Globalization.TimeSpanStyles styles=(System.Globalization.TimeSpanStyles)(0)) { throw null; }
        public static System.TimeSpan ParseExact(System.ReadOnlySpan<char> input, string[] formats, System.IFormatProvider formatProvider, System.Globalization.TimeSpanStyles styles=(System.Globalization.TimeSpanStyles)(0)) { throw null; }
        public static System.TimeSpan ParseExact(string input, string format, System.IFormatProvider formatProvider) { throw null; }
        public static System.TimeSpan ParseExact(string input, string format, System.IFormatProvider formatProvider, System.Globalization.TimeSpanStyles styles) { throw null; }
        public static System.TimeSpan ParseExact(string input, string[] formats, System.IFormatProvider formatProvider) { throw null; }
        public static System.TimeSpan ParseExact(string input, string[] formats, System.IFormatProvider formatProvider, System.Globalization.TimeSpanStyles styles) { throw null; }
        public System.TimeSpan Subtract(System.TimeSpan ts) { throw null; }
        public override string ToString() { throw null; }
        public string ToString(string format) { throw null; }
        public string ToString(string format, System.IFormatProvider formatProvider) { throw null; }
        public bool TryFormat(System.Span<char> destination, out int charsWritten, System.ReadOnlySpan<char> format=default(System.ReadOnlySpan<char>), System.IFormatProvider formatProvider=null) { throw null; }
        public static bool TryParse(System.ReadOnlySpan<char> input, System.IFormatProvider formatProvider, out System.TimeSpan result) { throw null; }
        public static bool TryParse(System.ReadOnlySpan<char> s, out System.TimeSpan result) { throw null; }
        public static bool TryParse(string input, System.IFormatProvider formatProvider, out System.TimeSpan result) { throw null; }
        public static bool TryParse(string s, out System.TimeSpan result) { throw null; }
        public static bool TryParseExact(System.ReadOnlySpan<char> input, System.ReadOnlySpan<char> format, System.IFormatProvider formatProvider, System.Globalization.TimeSpanStyles styles, out System.TimeSpan result) { throw null; }
        public static bool TryParseExact(System.ReadOnlySpan<char> input, System.ReadOnlySpan<char> format, System.IFormatProvider formatProvider, out System.TimeSpan result) { throw null; }
        public static bool TryParseExact(System.ReadOnlySpan<char> input, string[] formats, System.IFormatProvider formatProvider, System.Globalization.TimeSpanStyles styles, out System.TimeSpan result) { throw null; }
        public static bool TryParseExact(System.ReadOnlySpan<char> input, string[] formats, System.IFormatProvider formatProvider, out System.TimeSpan result) { throw null; }
        public static bool TryParseExact(string input, string format, System.IFormatProvider formatProvider, System.Globalization.TimeSpanStyles styles, out System.TimeSpan result) { throw null; }
        public static bool TryParseExact(string input, string format, System.IFormatProvider formatProvider, out System.TimeSpan result) { throw null; }
        public static bool TryParseExact(string input, string[] formats, System.IFormatProvider formatProvider, System.Globalization.TimeSpanStyles styles, out System.TimeSpan result) { throw null; }
        public static bool TryParseExact(string input, string[] formats, System.IFormatProvider formatProvider, out System.TimeSpan result) { throw null; }
    }
    [System.ObsoleteAttribute("System.TimeZone has been deprecated.  Please investigate the use of System.TimeZoneInfo instead.")]
    public abstract partial class TimeZone
    {
        protected TimeZone() { }
        public static System.TimeZone CurrentTimeZone { get { throw null; } }
        public abstract string DaylightName { get; }
        public abstract string StandardName { get; }
        public abstract System.Globalization.DaylightTime GetDaylightChanges(int year);
        public abstract System.TimeSpan GetUtcOffset(System.DateTime time);
        public virtual bool IsDaylightSavingTime(System.DateTime time) { throw null; }
        public static bool IsDaylightSavingTime(System.DateTime time, System.Globalization.DaylightTime daylightTimes) { throw null; }
        public virtual System.DateTime ToLocalTime(System.DateTime time) { throw null; }
        public virtual System.DateTime ToUniversalTime(System.DateTime time) { throw null; }
    }
    public sealed partial class TimeZoneInfo : System.IEquatable<System.TimeZoneInfo>, System.Runtime.Serialization.IDeserializationCallback, System.Runtime.Serialization.ISerializable
    {
        internal TimeZoneInfo() { }
        public System.TimeSpan BaseUtcOffset { get { throw null; } }
        public string DaylightName { get { throw null; } }
        public string DisplayName { get { throw null; } }
        public string Id { get { throw null; } }
        public static System.TimeZoneInfo Local { get { throw null; } }
        public string StandardName { get { throw null; } }
        public bool SupportsDaylightSavingTime { get { throw null; } }
        public static System.TimeZoneInfo Utc { get { throw null; } }
        public static void ClearCachedData() { }
        public static System.DateTime ConvertTime(System.DateTime dateTime, System.TimeZoneInfo destinationTimeZone) { throw null; }
        public static System.DateTime ConvertTime(System.DateTime dateTime, System.TimeZoneInfo sourceTimeZone, System.TimeZoneInfo destinationTimeZone) { throw null; }
        public static System.DateTimeOffset ConvertTime(System.DateTimeOffset dateTimeOffset, System.TimeZoneInfo destinationTimeZone) { throw null; }
        public static System.DateTime ConvertTimeBySystemTimeZoneId(System.DateTime dateTime, string destinationTimeZoneId) { throw null; }
        public static System.DateTime ConvertTimeBySystemTimeZoneId(System.DateTime dateTime, string sourceTimeZoneId, string destinationTimeZoneId) { throw null; }
        public static System.DateTimeOffset ConvertTimeBySystemTimeZoneId(System.DateTimeOffset dateTimeOffset, string destinationTimeZoneId) { throw null; }
        public static System.DateTime ConvertTimeFromUtc(System.DateTime dateTime, System.TimeZoneInfo destinationTimeZone) { throw null; }
        public static System.DateTime ConvertTimeToUtc(System.DateTime dateTime) { throw null; }
        public static System.DateTime ConvertTimeToUtc(System.DateTime dateTime, System.TimeZoneInfo sourceTimeZone) { throw null; }
        public static System.TimeZoneInfo CreateCustomTimeZone(string id, System.TimeSpan baseUtcOffset, string displayName, string standardDisplayName) { throw null; }
        public static System.TimeZoneInfo CreateCustomTimeZone(string id, System.TimeSpan baseUtcOffset, string displayName, string standardDisplayName, string daylightDisplayName, System.TimeZoneInfo.AdjustmentRule[] adjustmentRules) { throw null; }
        public static System.TimeZoneInfo CreateCustomTimeZone(string id, System.TimeSpan baseUtcOffset, string displayName, string standardDisplayName, string daylightDisplayName, System.TimeZoneInfo.AdjustmentRule[] adjustmentRules, bool disableDaylightSavingTime) { throw null; }
        public override bool Equals(object obj) { throw null; }
        public bool Equals(System.TimeZoneInfo other) { throw null; }
        public static System.TimeZoneInfo FindSystemTimeZoneById(string id) { throw null; }
        public static System.TimeZoneInfo FromSerializedString(string source) { throw null; }
        public System.TimeZoneInfo.AdjustmentRule[] GetAdjustmentRules() { throw null; }
        public System.TimeSpan[] GetAmbiguousTimeOffsets(System.DateTime dateTime) { throw null; }
        public System.TimeSpan[] GetAmbiguousTimeOffsets(System.DateTimeOffset dateTimeOffset) { throw null; }
        public override int GetHashCode() { throw null; }
        public static System.Collections.ObjectModel.ReadOnlyCollection<System.TimeZoneInfo> GetSystemTimeZones() { throw null; }
        public System.TimeSpan GetUtcOffset(System.DateTime dateTime) { throw null; }
        public System.TimeSpan GetUtcOffset(System.DateTimeOffset dateTimeOffset) { throw null; }
        public bool HasSameRules(System.TimeZoneInfo other) { throw null; }
        public bool IsAmbiguousTime(System.DateTime dateTime) { throw null; }
        public bool IsAmbiguousTime(System.DateTimeOffset dateTimeOffset) { throw null; }
        public bool IsDaylightSavingTime(System.DateTime dateTime) { throw null; }
        public bool IsDaylightSavingTime(System.DateTimeOffset dateTimeOffset) { throw null; }
        public bool IsInvalidTime(System.DateTime dateTime) { throw null; }
        void System.Runtime.Serialization.IDeserializationCallback.OnDeserialization(object sender) { }
        void System.Runtime.Serialization.ISerializable.GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public string ToSerializedString() { throw null; }
        public override string ToString() { throw null; }
        public sealed partial class AdjustmentRule : System.IEquatable<System.TimeZoneInfo.AdjustmentRule>, System.Runtime.Serialization.IDeserializationCallback, System.Runtime.Serialization.ISerializable
        {
            internal AdjustmentRule() { }
            public System.DateTime DateEnd { get { throw null; } }
            public System.DateTime DateStart { get { throw null; } }
            public System.TimeSpan DaylightDelta { get { throw null; } }
            public System.TimeZoneInfo.TransitionTime DaylightTransitionEnd { get { throw null; } }
            public System.TimeZoneInfo.TransitionTime DaylightTransitionStart { get { throw null; } }
            public static System.TimeZoneInfo.AdjustmentRule CreateAdjustmentRule(System.DateTime dateStart, System.DateTime dateEnd, System.TimeSpan daylightDelta, System.TimeZoneInfo.TransitionTime daylightTransitionStart, System.TimeZoneInfo.TransitionTime daylightTransitionEnd) { throw null; }
            public bool Equals(System.TimeZoneInfo.AdjustmentRule other) { throw null; }
            public override int GetHashCode() { throw null; }
            void System.Runtime.Serialization.IDeserializationCallback.OnDeserialization(object sender) { }
            void System.Runtime.Serialization.ISerializable.GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        }
        public readonly partial struct TransitionTime : System.IEquatable<System.TimeZoneInfo.TransitionTime>, System.Runtime.Serialization.IDeserializationCallback, System.Runtime.Serialization.ISerializable
        {
            private readonly int _dummy;
            public int Day { get { throw null; } }
            public System.DayOfWeek DayOfWeek { get { throw null; } }
            public bool IsFixedDateRule { get { throw null; } }
            public int Month { get { throw null; } }
            public System.DateTime TimeOfDay { get { throw null; } }
            public int Week { get { throw null; } }
            public static System.TimeZoneInfo.TransitionTime CreateFixedDateRule(System.DateTime timeOfDay, int month, int day) { throw null; }
            public static System.TimeZoneInfo.TransitionTime CreateFloatingDateRule(System.DateTime timeOfDay, int month, int week, System.DayOfWeek dayOfWeek) { throw null; }
            public override bool Equals(object obj) { throw null; }
            public bool Equals(System.TimeZoneInfo.TransitionTime other) { throw null; }
            public override int GetHashCode() { throw null; }
            public static bool operator ==(System.TimeZoneInfo.TransitionTime t1, System.TimeZoneInfo.TransitionTime t2) { throw null; }
            public static bool operator !=(System.TimeZoneInfo.TransitionTime t1, System.TimeZoneInfo.TransitionTime t2) { throw null; }
            void System.Runtime.Serialization.IDeserializationCallback.OnDeserialization(object sender) { }
            void System.Runtime.Serialization.ISerializable.GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        }
    }
    public partial class TimeZoneNotFoundException : System.Exception
    {
        public TimeZoneNotFoundException() { }
        protected TimeZoneNotFoundException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public TimeZoneNotFoundException(string message) { }
        public TimeZoneNotFoundException(string message, System.Exception innerException) { }
    }
    public static partial class Tuple
    {
        public static System.Tuple<T1> Create<T1>(T1 item1) { throw null; }
        public static System.Tuple<T1, T2> Create<T1, T2>(T1 item1, T2 item2) { throw null; }
        public static System.Tuple<T1, T2, T3> Create<T1, T2, T3>(T1 item1, T2 item2, T3 item3) { throw null; }
        public static System.Tuple<T1, T2, T3, T4> Create<T1, T2, T3, T4>(T1 item1, T2 item2, T3 item3, T4 item4) { throw null; }
        public static System.Tuple<T1, T2, T3, T4, T5> Create<T1, T2, T3, T4, T5>(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5) { throw null; }
        public static System.Tuple<T1, T2, T3, T4, T5, T6> Create<T1, T2, T3, T4, T5, T6>(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6) { throw null; }
        public static System.Tuple<T1, T2, T3, T4, T5, T6, T7> Create<T1, T2, T3, T4, T5, T6, T7>(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7) { throw null; }
        public static System.Tuple<T1, T2, T3, T4, T5, T6, T7, System.Tuple<T8>> Create<T1, T2, T3, T4, T5, T6, T7, T8>(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, T8 item8) { throw null; }
    }
    public static partial class TupleExtensions
    {
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
        public static void Deconstruct<T1>(this System.Tuple<T1> value, out T1 item1) { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
        public static void Deconstruct<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(this System.Tuple<T1, T2, T3, T4, T5, T6, T7, System.Tuple<T8, T9, T10>> value, out T1 item1, out T2 item2, out T3 item3, out T4 item4, out T5 item5, out T6 item6, out T7 item7, out T8 item8, out T9 item9, out T10 item10) { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
        public static void Deconstruct<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(this System.Tuple<T1, T2, T3, T4, T5, T6, T7, System.Tuple<T8, T9, T10, T11>> value, out T1 item1, out T2 item2, out T3 item3, out T4 item4, out T5 item5, out T6 item6, out T7 item7, out T8 item8, out T9 item9, out T10 item10, out T11 item11) { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
        public static void Deconstruct<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(this System.Tuple<T1, T2, T3, T4, T5, T6, T7, System.Tuple<T8, T9, T10, T11, T12>> value, out T1 item1, out T2 item2, out T3 item3, out T4 item4, out T5 item5, out T6 item6, out T7 item7, out T8 item8, out T9 item9, out T10 item10, out T11 item11, out T12 item12) { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
        public static void Deconstruct<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(this System.Tuple<T1, T2, T3, T4, T5, T6, T7, System.Tuple<T8, T9, T10, T11, T12, T13>> value, out T1 item1, out T2 item2, out T3 item3, out T4 item4, out T5 item5, out T6 item6, out T7 item7, out T8 item8, out T9 item9, out T10 item10, out T11 item11, out T12 item12, out T13 item13) { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
        public static void Deconstruct<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(this System.Tuple<T1, T2, T3, T4, T5, T6, T7, System.Tuple<T8, T9, T10, T11, T12, T13, T14>> value, out T1 item1, out T2 item2, out T3 item3, out T4 item4, out T5 item5, out T6 item6, out T7 item7, out T8 item8, out T9 item9, out T10 item10, out T11 item11, out T12 item12, out T13 item13, out T14 item14) { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
        public static void Deconstruct<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(this System.Tuple<T1, T2, T3, T4, T5, T6, T7, System.Tuple<T8, T9, T10, T11, T12, T13, T14, System.Tuple<T15>>> value, out T1 item1, out T2 item2, out T3 item3, out T4 item4, out T5 item5, out T6 item6, out T7 item7, out T8 item8, out T9 item9, out T10 item10, out T11 item11, out T12 item12, out T13 item13, out T14 item14, out T15 item15) { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
        public static void Deconstruct<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(this System.Tuple<T1, T2, T3, T4, T5, T6, T7, System.Tuple<T8, T9, T10, T11, T12, T13, T14, System.Tuple<T15, T16>>> value, out T1 item1, out T2 item2, out T3 item3, out T4 item4, out T5 item5, out T6 item6, out T7 item7, out T8 item8, out T9 item9, out T10 item10, out T11 item11, out T12 item12, out T13 item13, out T14 item14, out T15 item15, out T16 item16) { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
        public static void Deconstruct<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17>(this System.Tuple<T1, T2, T3, T4, T5, T6, T7, System.Tuple<T8, T9, T10, T11, T12, T13, T14, System.Tuple<T15, T16, T17>>> value, out T1 item1, out T2 item2, out T3 item3, out T4 item4, out T5 item5, out T6 item6, out T7 item7, out T8 item8, out T9 item9, out T10 item10, out T11 item11, out T12 item12, out T13 item13, out T14 item14, out T15 item15, out T16 item16, out T17 item17) { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
        public static void Deconstruct<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18>(this System.Tuple<T1, T2, T3, T4, T5, T6, T7, System.Tuple<T8, T9, T10, T11, T12, T13, T14, System.Tuple<T15, T16, T17, T18>>> value, out T1 item1, out T2 item2, out T3 item3, out T4 item4, out T5 item5, out T6 item6, out T7 item7, out T8 item8, out T9 item9, out T10 item10, out T11 item11, out T12 item12, out T13 item13, out T14 item14, out T15 item15, out T16 item16, out T17 item17, out T18 item18) { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
        public static void Deconstruct<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19>(this System.Tuple<T1, T2, T3, T4, T5, T6, T7, System.Tuple<T8, T9, T10, T11, T12, T13, T14, System.Tuple<T15, T16, T17, T18, T19>>> value, out T1 item1, out T2 item2, out T3 item3, out T4 item4, out T5 item5, out T6 item6, out T7 item7, out T8 item8, out T9 item9, out T10 item10, out T11 item11, out T12 item12, out T13 item13, out T14 item14, out T15 item15, out T16 item16, out T17 item17, out T18 item18, out T19 item19) { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
        public static void Deconstruct<T1, T2>(this System.Tuple<T1, T2> value, out T1 item1, out T2 item2) { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
        public static void Deconstruct<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20>(this System.Tuple<T1, T2, T3, T4, T5, T6, T7, System.Tuple<T8, T9, T10, T11, T12, T13, T14, System.Tuple<T15, T16, T17, T18, T19, T20>>> value, out T1 item1, out T2 item2, out T3 item3, out T4 item4, out T5 item5, out T6 item6, out T7 item7, out T8 item8, out T9 item9, out T10 item10, out T11 item11, out T12 item12, out T13 item13, out T14 item14, out T15 item15, out T16 item16, out T17 item17, out T18 item18, out T19 item19, out T20 item20) { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
        public static void Deconstruct<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21>(this System.Tuple<T1, T2, T3, T4, T5, T6, T7, System.Tuple<T8, T9, T10, T11, T12, T13, T14, System.Tuple<T15, T16, T17, T18, T19, T20, T21>>> value, out T1 item1, out T2 item2, out T3 item3, out T4 item4, out T5 item5, out T6 item6, out T7 item7, out T8 item8, out T9 item9, out T10 item10, out T11 item11, out T12 item12, out T13 item13, out T14 item14, out T15 item15, out T16 item16, out T17 item17, out T18 item18, out T19 item19, out T20 item20, out T21 item21) { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
        public static void Deconstruct<T1, T2, T3>(this System.Tuple<T1, T2, T3> value, out T1 item1, out T2 item2, out T3 item3) { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
        public static void Deconstruct<T1, T2, T3, T4>(this System.Tuple<T1, T2, T3, T4> value, out T1 item1, out T2 item2, out T3 item3, out T4 item4) { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
        public static void Deconstruct<T1, T2, T3, T4, T5>(this System.Tuple<T1, T2, T3, T4, T5> value, out T1 item1, out T2 item2, out T3 item3, out T4 item4, out T5 item5) { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
        public static void Deconstruct<T1, T2, T3, T4, T5, T6>(this System.Tuple<T1, T2, T3, T4, T5, T6> value, out T1 item1, out T2 item2, out T3 item3, out T4 item4, out T5 item5, out T6 item6) { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
        public static void Deconstruct<T1, T2, T3, T4, T5, T6, T7>(this System.Tuple<T1, T2, T3, T4, T5, T6, T7> value, out T1 item1, out T2 item2, out T3 item3, out T4 item4, out T5 item5, out T6 item6, out T7 item7) { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
        public static void Deconstruct<T1, T2, T3, T4, T5, T6, T7, T8>(this System.Tuple<T1, T2, T3, T4, T5, T6, T7, System.Tuple<T8>> value, out T1 item1, out T2 item2, out T3 item3, out T4 item4, out T5 item5, out T6 item6, out T7 item7, out T8 item8) { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
        public static void Deconstruct<T1, T2, T3, T4, T5, T6, T7, T8, T9>(this System.Tuple<T1, T2, T3, T4, T5, T6, T7, System.Tuple<T8, T9>> value, out T1 item1, out T2 item2, out T3 item3, out T4 item4, out T5 item5, out T6 item6, out T7 item7, out T8 item8, out T9 item9) { throw null; }
        public static System.Tuple<T1> ToTuple<T1>(this System.ValueTuple<T1> value) { throw null; }
        public static System.Tuple<T1, T2, T3, T4, T5, T6, T7, System.Tuple<T8, T9, T10>> ToTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(this System.ValueTuple<T1, T2, T3, T4, T5, T6, T7, System.ValueTuple<T8, T9, T10>> value) { throw null; }
        public static System.Tuple<T1, T2, T3, T4, T5, T6, T7, System.Tuple<T8, T9, T10, T11>> ToTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(this System.ValueTuple<T1, T2, T3, T4, T5, T6, T7, System.ValueTuple<T8, T9, T10, T11>> value) { throw null; }
        public static System.Tuple<T1, T2, T3, T4, T5, T6, T7, System.Tuple<T8, T9, T10, T11, T12>> ToTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(this System.ValueTuple<T1, T2, T3, T4, T5, T6, T7, System.ValueTuple<T8, T9, T10, T11, T12>> value) { throw null; }
        public static System.Tuple<T1, T2, T3, T4, T5, T6, T7, System.Tuple<T8, T9, T10, T11, T12, T13>> ToTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(this System.ValueTuple<T1, T2, T3, T4, T5, T6, T7, System.ValueTuple<T8, T9, T10, T11, T12, T13>> value) { throw null; }
        public static System.Tuple<T1, T2, T3, T4, T5, T6, T7, System.Tuple<T8, T9, T10, T11, T12, T13, T14>> ToTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(this System.ValueTuple<T1, T2, T3, T4, T5, T6, T7, System.ValueTuple<T8, T9, T10, T11, T12, T13, T14>> value) { throw null; }
        public static System.Tuple<T1, T2, T3, T4, T5, T6, T7, System.Tuple<T8, T9, T10, T11, T12, T13, T14, System.Tuple<T15>>> ToTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(this System.ValueTuple<T1, T2, T3, T4, T5, T6, T7, System.ValueTuple<T8, T9, T10, T11, T12, T13, T14, System.ValueTuple<T15>>> value) { throw null; }
        public static System.Tuple<T1, T2, T3, T4, T5, T6, T7, System.Tuple<T8, T9, T10, T11, T12, T13, T14, System.Tuple<T15, T16>>> ToTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(this System.ValueTuple<T1, T2, T3, T4, T5, T6, T7, System.ValueTuple<T8, T9, T10, T11, T12, T13, T14, System.ValueTuple<T15, T16>>> value) { throw null; }
        public static System.Tuple<T1, T2, T3, T4, T5, T6, T7, System.Tuple<T8, T9, T10, T11, T12, T13, T14, System.Tuple<T15, T16, T17>>> ToTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17>(this System.ValueTuple<T1, T2, T3, T4, T5, T6, T7, System.ValueTuple<T8, T9, T10, T11, T12, T13, T14, System.ValueTuple<T15, T16, T17>>> value) { throw null; }
        public static System.Tuple<T1, T2, T3, T4, T5, T6, T7, System.Tuple<T8, T9, T10, T11, T12, T13, T14, System.Tuple<T15, T16, T17, T18>>> ToTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18>(this System.ValueTuple<T1, T2, T3, T4, T5, T6, T7, System.ValueTuple<T8, T9, T10, T11, T12, T13, T14, System.ValueTuple<T15, T16, T17, T18>>> value) { throw null; }
        public static System.Tuple<T1, T2, T3, T4, T5, T6, T7, System.Tuple<T8, T9, T10, T11, T12, T13, T14, System.Tuple<T15, T16, T17, T18, T19>>> ToTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19>(this System.ValueTuple<T1, T2, T3, T4, T5, T6, T7, System.ValueTuple<T8, T9, T10, T11, T12, T13, T14, System.ValueTuple<T15, T16, T17, T18, T19>>> value) { throw null; }
        public static System.Tuple<T1, T2> ToTuple<T1, T2>(this System.ValueTuple<T1, T2> value) { throw null; }
        public static System.Tuple<T1, T2, T3, T4, T5, T6, T7, System.Tuple<T8, T9, T10, T11, T12, T13, T14, System.Tuple<T15, T16, T17, T18, T19, T20>>> ToTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20>(this System.ValueTuple<T1, T2, T3, T4, T5, T6, T7, System.ValueTuple<T8, T9, T10, T11, T12, T13, T14, System.ValueTuple<T15, T16, T17, T18, T19, T20>>> value) { throw null; }
        public static System.Tuple<T1, T2, T3, T4, T5, T6, T7, System.Tuple<T8, T9, T10, T11, T12, T13, T14, System.Tuple<T15, T16, T17, T18, T19, T20, T21>>> ToTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21>(this System.ValueTuple<T1, T2, T3, T4, T5, T6, T7, System.ValueTuple<T8, T9, T10, T11, T12, T13, T14, System.ValueTuple<T15, T16, T17, T18, T19, T20, T21>>> value) { throw null; }
        public static System.Tuple<T1, T2, T3> ToTuple<T1, T2, T3>(this System.ValueTuple<T1, T2, T3> value) { throw null; }
        public static System.Tuple<T1, T2, T3, T4> ToTuple<T1, T2, T3, T4>(this System.ValueTuple<T1, T2, T3, T4> value) { throw null; }
        public static System.Tuple<T1, T2, T3, T4, T5> ToTuple<T1, T2, T3, T4, T5>(this System.ValueTuple<T1, T2, T3, T4, T5> value) { throw null; }
        public static System.Tuple<T1, T2, T3, T4, T5, T6> ToTuple<T1, T2, T3, T4, T5, T6>(this System.ValueTuple<T1, T2, T3, T4, T5, T6> value) { throw null; }
        public static System.Tuple<T1, T2, T3, T4, T5, T6, T7> ToTuple<T1, T2, T3, T4, T5, T6, T7>(this System.ValueTuple<T1, T2, T3, T4, T5, T6, T7> value) { throw null; }
        public static System.Tuple<T1, T2, T3, T4, T5, T6, T7, System.Tuple<T8>> ToTuple<T1, T2, T3, T4, T5, T6, T7, T8>(this System.ValueTuple<T1, T2, T3, T4, T5, T6, T7, System.ValueTuple<T8>> value) { throw null; }
        public static System.Tuple<T1, T2, T3, T4, T5, T6, T7, System.Tuple<T8, T9>> ToTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9>(this System.ValueTuple<T1, T2, T3, T4, T5, T6, T7, System.ValueTuple<T8, T9>> value) { throw null; }
        public static System.ValueTuple<T1> ToValueTuple<T1>(this System.Tuple<T1> value) { throw null; }
        public static System.ValueTuple<T1, T2, T3, T4, T5, T6, T7, System.ValueTuple<T8, T9, T10>> ToValueTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(this System.Tuple<T1, T2, T3, T4, T5, T6, T7, System.Tuple<T8, T9, T10>> value) { throw null; }
        public static System.ValueTuple<T1, T2, T3, T4, T5, T6, T7, System.ValueTuple<T8, T9, T10, T11>> ToValueTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(this System.Tuple<T1, T2, T3, T4, T5, T6, T7, System.Tuple<T8, T9, T10, T11>> value) { throw null; }
        public static System.ValueTuple<T1, T2, T3, T4, T5, T6, T7, System.ValueTuple<T8, T9, T10, T11, T12>> ToValueTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(this System.Tuple<T1, T2, T3, T4, T5, T6, T7, System.Tuple<T8, T9, T10, T11, T12>> value) { throw null; }
        public static System.ValueTuple<T1, T2, T3, T4, T5, T6, T7, System.ValueTuple<T8, T9, T10, T11, T12, T13>> ToValueTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(this System.Tuple<T1, T2, T3, T4, T5, T6, T7, System.Tuple<T8, T9, T10, T11, T12, T13>> value) { throw null; }
        public static System.ValueTuple<T1, T2, T3, T4, T5, T6, T7, System.ValueTuple<T8, T9, T10, T11, T12, T13, T14>> ToValueTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(this System.Tuple<T1, T2, T3, T4, T5, T6, T7, System.Tuple<T8, T9, T10, T11, T12, T13, T14>> value) { throw null; }
        public static System.ValueTuple<T1, T2, T3, T4, T5, T6, T7, System.ValueTuple<T8, T9, T10, T11, T12, T13, T14, System.ValueTuple<T15>>> ToValueTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(this System.Tuple<T1, T2, T3, T4, T5, T6, T7, System.Tuple<T8, T9, T10, T11, T12, T13, T14, System.Tuple<T15>>> value) { throw null; }
        public static System.ValueTuple<T1, T2, T3, T4, T5, T6, T7, System.ValueTuple<T8, T9, T10, T11, T12, T13, T14, System.ValueTuple<T15, T16>>> ToValueTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(this System.Tuple<T1, T2, T3, T4, T5, T6, T7, System.Tuple<T8, T9, T10, T11, T12, T13, T14, System.Tuple<T15, T16>>> value) { throw null; }
        public static System.ValueTuple<T1, T2, T3, T4, T5, T6, T7, System.ValueTuple<T8, T9, T10, T11, T12, T13, T14, System.ValueTuple<T15, T16, T17>>> ToValueTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17>(this System.Tuple<T1, T2, T3, T4, T5, T6, T7, System.Tuple<T8, T9, T10, T11, T12, T13, T14, System.Tuple<T15, T16, T17>>> value) { throw null; }
        public static System.ValueTuple<T1, T2, T3, T4, T5, T6, T7, System.ValueTuple<T8, T9, T10, T11, T12, T13, T14, System.ValueTuple<T15, T16, T17, T18>>> ToValueTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18>(this System.Tuple<T1, T2, T3, T4, T5, T6, T7, System.Tuple<T8, T9, T10, T11, T12, T13, T14, System.Tuple<T15, T16, T17, T18>>> value) { throw null; }
        public static System.ValueTuple<T1, T2, T3, T4, T5, T6, T7, System.ValueTuple<T8, T9, T10, T11, T12, T13, T14, System.ValueTuple<T15, T16, T17, T18, T19>>> ToValueTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19>(this System.Tuple<T1, T2, T3, T4, T5, T6, T7, System.Tuple<T8, T9, T10, T11, T12, T13, T14, System.Tuple<T15, T16, T17, T18, T19>>> value) { throw null; }
        public static System.ValueTuple<T1, T2> ToValueTuple<T1, T2>(this System.Tuple<T1, T2> value) { throw null; }
        public static System.ValueTuple<T1, T2, T3, T4, T5, T6, T7, System.ValueTuple<T8, T9, T10, T11, T12, T13, T14, System.ValueTuple<T15, T16, T17, T18, T19, T20>>> ToValueTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20>(this System.Tuple<T1, T2, T3, T4, T5, T6, T7, System.Tuple<T8, T9, T10, T11, T12, T13, T14, System.Tuple<T15, T16, T17, T18, T19, T20>>> value) { throw null; }
        public static System.ValueTuple<T1, T2, T3, T4, T5, T6, T7, System.ValueTuple<T8, T9, T10, T11, T12, T13, T14, System.ValueTuple<T15, T16, T17, T18, T19, T20, T21>>> ToValueTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21>(this System.Tuple<T1, T2, T3, T4, T5, T6, T7, System.Tuple<T8, T9, T10, T11, T12, T13, T14, System.Tuple<T15, T16, T17, T18, T19, T20, T21>>> value) { throw null; }
        public static System.ValueTuple<T1, T2, T3> ToValueTuple<T1, T2, T3>(this System.Tuple<T1, T2, T3> value) { throw null; }
        public static System.ValueTuple<T1, T2, T3, T4> ToValueTuple<T1, T2, T3, T4>(this System.Tuple<T1, T2, T3, T4> value) { throw null; }
        public static System.ValueTuple<T1, T2, T3, T4, T5> ToValueTuple<T1, T2, T3, T4, T5>(this System.Tuple<T1, T2, T3, T4, T5> value) { throw null; }
        public static System.ValueTuple<T1, T2, T3, T4, T5, T6> ToValueTuple<T1, T2, T3, T4, T5, T6>(this System.Tuple<T1, T2, T3, T4, T5, T6> value) { throw null; }
        public static System.ValueTuple<T1, T2, T3, T4, T5, T6, T7> ToValueTuple<T1, T2, T3, T4, T5, T6, T7>(this System.Tuple<T1, T2, T3, T4, T5, T6, T7> value) { throw null; }
        public static System.ValueTuple<T1, T2, T3, T4, T5, T6, T7, System.ValueTuple<T8>> ToValueTuple<T1, T2, T3, T4, T5, T6, T7, T8>(this System.Tuple<T1, T2, T3, T4, T5, T6, T7, System.Tuple<T8>> value) { throw null; }
        public static System.ValueTuple<T1, T2, T3, T4, T5, T6, T7, System.ValueTuple<T8, T9>> ToValueTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9>(this System.Tuple<T1, T2, T3, T4, T5, T6, T7, System.Tuple<T8, T9>> value) { throw null; }
    }
    public partial class Tuple<T1> : System.Collections.IStructuralComparable, System.Collections.IStructuralEquatable, System.IComparable, System.Runtime.CompilerServices.ITuple
    {
        public Tuple(T1 item1) { }
        public T1 Item1 { get { throw null; } }
        object System.Runtime.CompilerServices.ITuple.this[int index] { get { throw null; } }
        int System.Runtime.CompilerServices.ITuple.Length { get { throw null; } }
        public override bool Equals(object obj) { throw null; }
        public override int GetHashCode() { throw null; }
        int System.Collections.IStructuralComparable.CompareTo(object other, System.Collections.IComparer comparer) { throw null; }
        bool System.Collections.IStructuralEquatable.Equals(object other, System.Collections.IEqualityComparer comparer) { throw null; }
        int System.Collections.IStructuralEquatable.GetHashCode(System.Collections.IEqualityComparer comparer) { throw null; }
        int System.IComparable.CompareTo(object obj) { throw null; }
        public override string ToString() { throw null; }
    }
    public partial class Tuple<T1, T2> : System.Collections.IStructuralComparable, System.Collections.IStructuralEquatable, System.IComparable, System.Runtime.CompilerServices.ITuple
    {
        public Tuple(T1 item1, T2 item2) { }
        public T1 Item1 { get { throw null; } }
        public T2 Item2 { get { throw null; } }
        object System.Runtime.CompilerServices.ITuple.this[int index] { get { throw null; } }
        int System.Runtime.CompilerServices.ITuple.Length { get { throw null; } }
        public override bool Equals(object obj) { throw null; }
        public override int GetHashCode() { throw null; }
        int System.Collections.IStructuralComparable.CompareTo(object other, System.Collections.IComparer comparer) { throw null; }
        bool System.Collections.IStructuralEquatable.Equals(object other, System.Collections.IEqualityComparer comparer) { throw null; }
        int System.Collections.IStructuralEquatable.GetHashCode(System.Collections.IEqualityComparer comparer) { throw null; }
        int System.IComparable.CompareTo(object obj) { throw null; }
        public override string ToString() { throw null; }
    }
    public partial class Tuple<T1, T2, T3> : System.Collections.IStructuralComparable, System.Collections.IStructuralEquatable, System.IComparable, System.Runtime.CompilerServices.ITuple
    {
        public Tuple(T1 item1, T2 item2, T3 item3) { }
        public T1 Item1 { get { throw null; } }
        public T2 Item2 { get { throw null; } }
        public T3 Item3 { get { throw null; } }
        object System.Runtime.CompilerServices.ITuple.this[int index] { get { throw null; } }
        int System.Runtime.CompilerServices.ITuple.Length { get { throw null; } }
        public override bool Equals(object obj) { throw null; }
        public override int GetHashCode() { throw null; }
        int System.Collections.IStructuralComparable.CompareTo(object other, System.Collections.IComparer comparer) { throw null; }
        bool System.Collections.IStructuralEquatable.Equals(object other, System.Collections.IEqualityComparer comparer) { throw null; }
        int System.Collections.IStructuralEquatable.GetHashCode(System.Collections.IEqualityComparer comparer) { throw null; }
        int System.IComparable.CompareTo(object obj) { throw null; }
        public override string ToString() { throw null; }
    }
    public partial class Tuple<T1, T2, T3, T4> : System.Collections.IStructuralComparable, System.Collections.IStructuralEquatable, System.IComparable, System.Runtime.CompilerServices.ITuple
    {
        public Tuple(T1 item1, T2 item2, T3 item3, T4 item4) { }
        public T1 Item1 { get { throw null; } }
        public T2 Item2 { get { throw null; } }
        public T3 Item3 { get { throw null; } }
        public T4 Item4 { get { throw null; } }
        object System.Runtime.CompilerServices.ITuple.this[int index] { get { throw null; } }
        int System.Runtime.CompilerServices.ITuple.Length { get { throw null; } }
        public override bool Equals(object obj) { throw null; }
        public override int GetHashCode() { throw null; }
        int System.Collections.IStructuralComparable.CompareTo(object other, System.Collections.IComparer comparer) { throw null; }
        bool System.Collections.IStructuralEquatable.Equals(object other, System.Collections.IEqualityComparer comparer) { throw null; }
        int System.Collections.IStructuralEquatable.GetHashCode(System.Collections.IEqualityComparer comparer) { throw null; }
        int System.IComparable.CompareTo(object obj) { throw null; }
        public override string ToString() { throw null; }
    }
    public partial class Tuple<T1, T2, T3, T4, T5> : System.Collections.IStructuralComparable, System.Collections.IStructuralEquatable, System.IComparable, System.Runtime.CompilerServices.ITuple
    {
        public Tuple(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5) { }
        public T1 Item1 { get { throw null; } }
        public T2 Item2 { get { throw null; } }
        public T3 Item3 { get { throw null; } }
        public T4 Item4 { get { throw null; } }
        public T5 Item5 { get { throw null; } }
        object System.Runtime.CompilerServices.ITuple.this[int index] { get { throw null; } }
        int System.Runtime.CompilerServices.ITuple.Length { get { throw null; } }
        public override bool Equals(object obj) { throw null; }
        public override int GetHashCode() { throw null; }
        int System.Collections.IStructuralComparable.CompareTo(object other, System.Collections.IComparer comparer) { throw null; }
        bool System.Collections.IStructuralEquatable.Equals(object other, System.Collections.IEqualityComparer comparer) { throw null; }
        int System.Collections.IStructuralEquatable.GetHashCode(System.Collections.IEqualityComparer comparer) { throw null; }
        int System.IComparable.CompareTo(object obj) { throw null; }
        public override string ToString() { throw null; }
    }
    public partial class Tuple<T1, T2, T3, T4, T5, T6> : System.Collections.IStructuralComparable, System.Collections.IStructuralEquatable, System.IComparable, System.Runtime.CompilerServices.ITuple
    {
        public Tuple(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6) { }
        public T1 Item1 { get { throw null; } }
        public T2 Item2 { get { throw null; } }
        public T3 Item3 { get { throw null; } }
        public T4 Item4 { get { throw null; } }
        public T5 Item5 { get { throw null; } }
        public T6 Item6 { get { throw null; } }
        object System.Runtime.CompilerServices.ITuple.this[int index] { get { throw null; } }
        int System.Runtime.CompilerServices.ITuple.Length { get { throw null; } }
        public override bool Equals(object obj) { throw null; }
        public override int GetHashCode() { throw null; }
        int System.Collections.IStructuralComparable.CompareTo(object other, System.Collections.IComparer comparer) { throw null; }
        bool System.Collections.IStructuralEquatable.Equals(object other, System.Collections.IEqualityComparer comparer) { throw null; }
        int System.Collections.IStructuralEquatable.GetHashCode(System.Collections.IEqualityComparer comparer) { throw null; }
        int System.IComparable.CompareTo(object obj) { throw null; }
        public override string ToString() { throw null; }
    }
    public partial class Tuple<T1, T2, T3, T4, T5, T6, T7> : System.Collections.IStructuralComparable, System.Collections.IStructuralEquatable, System.IComparable, System.Runtime.CompilerServices.ITuple
    {
        public Tuple(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7) { }
        public T1 Item1 { get { throw null; } }
        public T2 Item2 { get { throw null; } }
        public T3 Item3 { get { throw null; } }
        public T4 Item4 { get { throw null; } }
        public T5 Item5 { get { throw null; } }
        public T6 Item6 { get { throw null; } }
        public T7 Item7 { get { throw null; } }
        object System.Runtime.CompilerServices.ITuple.this[int index] { get { throw null; } }
        int System.Runtime.CompilerServices.ITuple.Length { get { throw null; } }
        public override bool Equals(object obj) { throw null; }
        public override int GetHashCode() { throw null; }
        int System.Collections.IStructuralComparable.CompareTo(object other, System.Collections.IComparer comparer) { throw null; }
        bool System.Collections.IStructuralEquatable.Equals(object other, System.Collections.IEqualityComparer comparer) { throw null; }
        int System.Collections.IStructuralEquatable.GetHashCode(System.Collections.IEqualityComparer comparer) { throw null; }
        int System.IComparable.CompareTo(object obj) { throw null; }
        public override string ToString() { throw null; }
    }
    public partial class Tuple<T1, T2, T3, T4, T5, T6, T7, TRest> : System.Collections.IStructuralComparable, System.Collections.IStructuralEquatable, System.IComparable, System.Runtime.CompilerServices.ITuple
    {
        public Tuple(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, TRest rest) { }
        public T1 Item1 { get { throw null; } }
        public T2 Item2 { get { throw null; } }
        public T3 Item3 { get { throw null; } }
        public T4 Item4 { get { throw null; } }
        public T5 Item5 { get { throw null; } }
        public T6 Item6 { get { throw null; } }
        public T7 Item7 { get { throw null; } }
        public TRest Rest { get { throw null; } }
        object System.Runtime.CompilerServices.ITuple.this[int index] { get { throw null; } }
        int System.Runtime.CompilerServices.ITuple.Length { get { throw null; } }
        public override bool Equals(object obj) { throw null; }
        public override int GetHashCode() { throw null; }
        int System.Collections.IStructuralComparable.CompareTo(object other, System.Collections.IComparer comparer) { throw null; }
        bool System.Collections.IStructuralEquatable.Equals(object other, System.Collections.IEqualityComparer comparer) { throw null; }
        int System.Collections.IStructuralEquatable.GetHashCode(System.Collections.IEqualityComparer comparer) { throw null; }
        int System.IComparable.CompareTo(object obj) { throw null; }
        public override string ToString() { throw null; }
    }
    public abstract partial class Type : System.Reflection.MemberInfo, System.Reflection.IReflect
    {
        public static readonly char Delimiter;
        public static readonly System.Type[] EmptyTypes;
        public static readonly System.Reflection.MemberFilter FilterAttribute;
        public static readonly System.Reflection.MemberFilter FilterName;
        public static readonly System.Reflection.MemberFilter FilterNameIgnoreCase;
        public static readonly object Missing;
        protected Type() { }
        public abstract System.Reflection.Assembly Assembly { get; }
        public abstract string AssemblyQualifiedName { get; }
        public System.Reflection.TypeAttributes Attributes { get { throw null; } }
        public abstract System.Type BaseType { get; }
        public virtual bool ContainsGenericParameters { get { throw null; } }
        public virtual System.Reflection.MethodBase DeclaringMethod { get { throw null; } }
        public override System.Type DeclaringType { get { throw null; } }
        public static System.Reflection.Binder DefaultBinder { get { throw null; } }
        public abstract string FullName { get; }
        public virtual System.Reflection.GenericParameterAttributes GenericParameterAttributes { get { throw null; } }
        public virtual int GenericParameterPosition { get { throw null; } }
        public virtual System.Type[] GenericTypeArguments { get { throw null; } }
        public abstract System.Guid GUID { get; }
        public bool HasElementType { get { throw null; } }
        public bool IsAbstract { get { throw null; } }
        public bool IsAnsiClass { get { throw null; } }
        public bool IsArray { get { throw null; } }
        public bool IsAutoClass { get { throw null; } }
        public bool IsAutoLayout { get { throw null; } }
        public bool IsByRef { get { throw null; } }
        public virtual bool IsByRefLike { get { throw null; } }
        public bool IsClass { get { throw null; } }
        public bool IsCOMObject { get { throw null; } }
        public virtual bool IsConstructedGenericType { get { throw null; } }
        public bool IsContextful { get { throw null; } }
        public virtual bool IsEnum { get { throw null; } }
        public bool IsExplicitLayout { get { throw null; } }
        public virtual bool IsGenericMethodParameter { get { throw null; } }
        public virtual bool IsGenericParameter { get { throw null; } }
        public virtual bool IsGenericType { get { throw null; } }
        public virtual bool IsGenericTypeDefinition { get { throw null; } }
        public virtual bool IsGenericTypeParameter { get { throw null; } }
        public bool IsImport { get { throw null; } }
        public bool IsInterface { get { throw null; } }
        public bool IsLayoutSequential { get { throw null; } }
        public bool IsMarshalByRef { get { throw null; } }
        public bool IsNested { get { throw null; } }
        public bool IsNestedAssembly { get { throw null; } }
        public bool IsNestedFamANDAssem { get { throw null; } }
        public bool IsNestedFamily { get { throw null; } }
        public bool IsNestedFamORAssem { get { throw null; } }
        public bool IsNestedPrivate { get { throw null; } }
        public bool IsNestedPublic { get { throw null; } }
        public bool IsNotPublic { get { throw null; } }
        public bool IsPointer { get { throw null; } }
        public bool IsPrimitive { get { throw null; } }
        public bool IsPublic { get { throw null; } }
        public bool IsSealed { get { throw null; } }
        public virtual bool IsSecurityCritical { get { throw null; } }
        public virtual bool IsSecuritySafeCritical { get { throw null; } }
        public virtual bool IsSecurityTransparent { get { throw null; } }
        public virtual bool IsSerializable { get { throw null; } }
        public virtual bool IsSignatureType { get { throw null; } }
        public bool IsSpecialName { get { throw null; } }
        public virtual bool IsSZArray { get { throw null; } }
        public virtual bool IsTypeDefinition { get { throw null; } }
        public bool IsUnicodeClass { get { throw null; } }
        public bool IsValueType { get { throw null; } }
        public virtual bool IsVariableBoundArray { get { throw null; } }
        public bool IsVisible { get { throw null; } }
        public override System.Reflection.MemberTypes MemberType { get { throw null; } }
        public abstract new System.Reflection.Module Module { get; }
        public abstract string Namespace { get; }
        public override System.Type ReflectedType { get { throw null; } }
        public virtual System.Runtime.InteropServices.StructLayoutAttribute StructLayoutAttribute { get { throw null; } }
        public virtual System.RuntimeTypeHandle TypeHandle { get { throw null; } }
        public System.Reflection.ConstructorInfo TypeInitializer { get { throw null; } }
        public abstract System.Type UnderlyingSystemType { get; }
        public override bool Equals(object o) { throw null; }
        public virtual bool Equals(System.Type o) { throw null; }
        public virtual System.Type[] FindInterfaces(System.Reflection.TypeFilter filter, object filterCriteria) { throw null; }
        public virtual System.Reflection.MemberInfo[] FindMembers(System.Reflection.MemberTypes memberType, System.Reflection.BindingFlags bindingAttr, System.Reflection.MemberFilter filter, object filterCriteria) { throw null; }
        public virtual int GetArrayRank() { throw null; }
        protected abstract System.Reflection.TypeAttributes GetAttributeFlagsImpl();
        public System.Reflection.ConstructorInfo GetConstructor(System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder binder, System.Reflection.CallingConventions callConvention, System.Type[] types, System.Reflection.ParameterModifier[] modifiers) { throw null; }
        public System.Reflection.ConstructorInfo GetConstructor(System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder binder, System.Type[] types, System.Reflection.ParameterModifier[] modifiers) { throw null; }
        public System.Reflection.ConstructorInfo GetConstructor(System.Type[] types) { throw null; }
        protected abstract System.Reflection.ConstructorInfo GetConstructorImpl(System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder binder, System.Reflection.CallingConventions callConvention, System.Type[] types, System.Reflection.ParameterModifier[] modifiers);
        public System.Reflection.ConstructorInfo[] GetConstructors() { throw null; }
        public abstract System.Reflection.ConstructorInfo[] GetConstructors(System.Reflection.BindingFlags bindingAttr);
        public virtual System.Reflection.MemberInfo[] GetDefaultMembers() { throw null; }
        public abstract System.Type GetElementType();
        public virtual string GetEnumName(object value) { throw null; }
        public virtual string[] GetEnumNames() { throw null; }
        public virtual System.Type GetEnumUnderlyingType() { throw null; }
        public virtual System.Array GetEnumValues() { throw null; }
        public System.Reflection.EventInfo GetEvent(string name) { throw null; }
        public abstract System.Reflection.EventInfo GetEvent(string name, System.Reflection.BindingFlags bindingAttr);
        public virtual System.Reflection.EventInfo[] GetEvents() { throw null; }
        public abstract System.Reflection.EventInfo[] GetEvents(System.Reflection.BindingFlags bindingAttr);
        public System.Reflection.FieldInfo GetField(string name) { throw null; }
        public abstract System.Reflection.FieldInfo GetField(string name, System.Reflection.BindingFlags bindingAttr);
        public System.Reflection.FieldInfo[] GetFields() { throw null; }
        public abstract System.Reflection.FieldInfo[] GetFields(System.Reflection.BindingFlags bindingAttr);
        public virtual System.Type[] GetGenericArguments() { throw null; }
        public virtual System.Type[] GetGenericParameterConstraints() { throw null; }
        public virtual System.Type GetGenericTypeDefinition() { throw null; }
        public override int GetHashCode() { throw null; }
        public System.Type GetInterface(string name) { throw null; }
        public abstract System.Type GetInterface(string name, bool ignoreCase);
        public virtual System.Reflection.InterfaceMapping GetInterfaceMap(System.Type interfaceType) { throw null; }
        public abstract System.Type[] GetInterfaces();
        public System.Reflection.MemberInfo[] GetMember(string name) { throw null; }
        public virtual System.Reflection.MemberInfo[] GetMember(string name, System.Reflection.BindingFlags bindingAttr) { throw null; }
        public virtual System.Reflection.MemberInfo[] GetMember(string name, System.Reflection.MemberTypes type, System.Reflection.BindingFlags bindingAttr) { throw null; }
        public System.Reflection.MemberInfo[] GetMembers() { throw null; }
        public abstract System.Reflection.MemberInfo[] GetMembers(System.Reflection.BindingFlags bindingAttr);
        public System.Reflection.MethodInfo GetMethod(string name) { throw null; }
        public System.Reflection.MethodInfo GetMethod(string name, int genericParameterCount, System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder binder, System.Reflection.CallingConventions callConvention, System.Type[] types, System.Reflection.ParameterModifier[] modifiers) { throw null; }
        public System.Reflection.MethodInfo GetMethod(string name, int genericParameterCount, System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder binder, System.Type[] types, System.Reflection.ParameterModifier[] modifiers) { throw null; }
        public System.Reflection.MethodInfo GetMethod(string name, int genericParameterCount, System.Type[] types) { throw null; }
        public System.Reflection.MethodInfo GetMethod(string name, int genericParameterCount, System.Type[] types, System.Reflection.ParameterModifier[] modifiers) { throw null; }
        public System.Reflection.MethodInfo GetMethod(string name, System.Reflection.BindingFlags bindingAttr) { throw null; }
        public System.Reflection.MethodInfo GetMethod(string name, System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder binder, System.Reflection.CallingConventions callConvention, System.Type[] types, System.Reflection.ParameterModifier[] modifiers) { throw null; }
        public System.Reflection.MethodInfo GetMethod(string name, System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder binder, System.Type[] types, System.Reflection.ParameterModifier[] modifiers) { throw null; }
        public System.Reflection.MethodInfo GetMethod(string name, System.Type[] types) { throw null; }
        public System.Reflection.MethodInfo GetMethod(string name, System.Type[] types, System.Reflection.ParameterModifier[] modifiers) { throw null; }
        protected virtual System.Reflection.MethodInfo GetMethodImpl(string name, int genericParameterCount, System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder binder, System.Reflection.CallingConventions callConvention, System.Type[] types, System.Reflection.ParameterModifier[] modifiers) { throw null; }
        protected abstract System.Reflection.MethodInfo GetMethodImpl(string name, System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder binder, System.Reflection.CallingConventions callConvention, System.Type[] types, System.Reflection.ParameterModifier[] modifiers);
        public System.Reflection.MethodInfo[] GetMethods() { throw null; }
        public abstract System.Reflection.MethodInfo[] GetMethods(System.Reflection.BindingFlags bindingAttr);
        public System.Type GetNestedType(string name) { throw null; }
        public abstract System.Type GetNestedType(string name, System.Reflection.BindingFlags bindingAttr);
        public System.Type[] GetNestedTypes() { throw null; }
        public abstract System.Type[] GetNestedTypes(System.Reflection.BindingFlags bindingAttr);
        public System.Reflection.PropertyInfo[] GetProperties() { throw null; }
        public abstract System.Reflection.PropertyInfo[] GetProperties(System.Reflection.BindingFlags bindingAttr);
        public System.Reflection.PropertyInfo GetProperty(string name) { throw null; }
        public System.Reflection.PropertyInfo GetProperty(string name, System.Reflection.BindingFlags bindingAttr) { throw null; }
        public System.Reflection.PropertyInfo GetProperty(string name, System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder binder, System.Type returnType, System.Type[] types, System.Reflection.ParameterModifier[] modifiers) { throw null; }
        public System.Reflection.PropertyInfo GetProperty(string name, System.Type returnType) { throw null; }
        public System.Reflection.PropertyInfo GetProperty(string name, System.Type returnType, System.Type[] types) { throw null; }
        public System.Reflection.PropertyInfo GetProperty(string name, System.Type returnType, System.Type[] types, System.Reflection.ParameterModifier[] modifiers) { throw null; }
        public System.Reflection.PropertyInfo GetProperty(string name, System.Type[] types) { throw null; }
        protected abstract System.Reflection.PropertyInfo GetPropertyImpl(string name, System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder binder, System.Type returnType, System.Type[] types, System.Reflection.ParameterModifier[] modifiers);
        public new System.Type GetType() { throw null; }
        public static System.Type GetType(string typeName) { throw null; }
        public static System.Type GetType(string typeName, bool throwOnError) { throw null; }
        public static System.Type GetType(string typeName, bool throwOnError, bool ignoreCase) { throw null; }
        public static System.Type GetType(string typeName, System.Func<System.Reflection.AssemblyName, System.Reflection.Assembly> assemblyResolver, System.Func<System.Reflection.Assembly, string, bool, System.Type> typeResolver) { throw null; }
        public static System.Type GetType(string typeName, System.Func<System.Reflection.AssemblyName, System.Reflection.Assembly> assemblyResolver, System.Func<System.Reflection.Assembly, string, bool, System.Type> typeResolver, bool throwOnError) { throw null; }
        public static System.Type GetType(string typeName, System.Func<System.Reflection.AssemblyName, System.Reflection.Assembly> assemblyResolver, System.Func<System.Reflection.Assembly, string, bool, System.Type> typeResolver, bool throwOnError, bool ignoreCase) { throw null; }
        public static System.Type[] GetTypeArray(object[] args) { throw null; }
        public static System.TypeCode GetTypeCode(System.Type type) { throw null; }
        protected virtual System.TypeCode GetTypeCodeImpl() { throw null; }
        public static System.Type GetTypeFromCLSID(System.Guid clsid) { throw null; }
        public static System.Type GetTypeFromCLSID(System.Guid clsid, bool throwOnError) { throw null; }
        public static System.Type GetTypeFromCLSID(System.Guid clsid, string server) { throw null; }
        public static System.Type GetTypeFromCLSID(System.Guid clsid, string server, bool throwOnError) { throw null; }
        public static System.Type GetTypeFromHandle(System.RuntimeTypeHandle handle) { throw null; }
        public static System.Type GetTypeFromProgID(string progID) { throw null; }
        public static System.Type GetTypeFromProgID(string progID, bool throwOnError) { throw null; }
        public static System.Type GetTypeFromProgID(string progID, string server) { throw null; }
        public static System.Type GetTypeFromProgID(string progID, string server, bool throwOnError) { throw null; }
        public static System.RuntimeTypeHandle GetTypeHandle(object o) { throw null; }
        protected abstract bool HasElementTypeImpl();
        public object InvokeMember(string name, System.Reflection.BindingFlags invokeAttr, System.Reflection.Binder binder, object target, object[] args) { throw null; }
        public object InvokeMember(string name, System.Reflection.BindingFlags invokeAttr, System.Reflection.Binder binder, object target, object[] args, System.Globalization.CultureInfo culture) { throw null; }
        public abstract object InvokeMember(string name, System.Reflection.BindingFlags invokeAttr, System.Reflection.Binder binder, object target, object[] args, System.Reflection.ParameterModifier[] modifiers, System.Globalization.CultureInfo culture, string[] namedParameters);
        protected abstract bool IsArrayImpl();
        public virtual bool IsAssignableFrom(System.Type c) { throw null; }
        protected abstract bool IsByRefImpl();
        protected abstract bool IsCOMObjectImpl();
        protected virtual bool IsContextfulImpl() { throw null; }
        public virtual bool IsEnumDefined(object value) { throw null; }
        public virtual bool IsEquivalentTo(System.Type other) { throw null; }
        public virtual bool IsInstanceOfType(object o) { throw null; }
        protected virtual bool IsMarshalByRefImpl() { throw null; }
        protected abstract bool IsPointerImpl();
        protected abstract bool IsPrimitiveImpl();
        public virtual bool IsSubclassOf(System.Type c) { throw null; }
        protected virtual bool IsValueTypeImpl() { throw null; }
        public virtual System.Type MakeArrayType() { throw null; }
        public virtual System.Type MakeArrayType(int rank) { throw null; }
        public virtual System.Type MakeByRefType() { throw null; }
        public static System.Type MakeGenericMethodParameter(int position) { throw null; }
        public virtual System.Type MakeGenericType(params System.Type[] typeArguments) { throw null; }
        public virtual System.Type MakePointerType() { throw null; }
        public static bool operator ==(System.Type left, System.Type right) { throw null; }
        public static bool operator !=(System.Type left, System.Type right) { throw null; }
        public static System.Type ReflectionOnlyGetType(string typeName, bool throwIfNotFound, bool ignoreCase) { throw null; }
        public override string ToString() { throw null; }
    }
    public partial class TypeAccessException : System.TypeLoadException
    {
        public TypeAccessException() { }
        protected TypeAccessException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public TypeAccessException(string message) { }
        public TypeAccessException(string message, System.Exception inner) { }
    }
    public enum TypeCode
    {
        Boolean = 3,
        Byte = 6,
        Char = 4,
        DateTime = 16,
        DBNull = 2,
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
    [System.CLSCompliantAttribute(false)]
    public ref partial struct TypedReference
    {
        private int _dummy;
        public override bool Equals(object o) { throw null; }
        public override int GetHashCode() { throw null; }
        public static System.Type GetTargetType(System.TypedReference value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.TypedReference MakeTypedReference(object target, System.Reflection.FieldInfo[] flds) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static void SetTypedReference(System.TypedReference target, object value) { }
        public static System.RuntimeTypeHandle TargetTypeToken(System.TypedReference value) { throw null; }
        public static object ToObject(System.TypedReference value) { throw null; }
    }
    public sealed partial class TypeInitializationException : System.SystemException
    {
        public TypeInitializationException(string fullTypeName, System.Exception innerException) { }
        public string TypeName { get { throw null; } }
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    }
    public partial class TypeLoadException : System.SystemException, System.Runtime.Serialization.ISerializable
    {
        public TypeLoadException() { }
        protected TypeLoadException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public TypeLoadException(string message) { }
        public TypeLoadException(string message, System.Exception inner) { }
        public override string Message { get { throw null; } }
        public string TypeName { get { throw null; } }
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    }
    public partial class TypeUnloadedException : System.SystemException
    {
        public TypeUnloadedException() { }
        protected TypeUnloadedException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public TypeUnloadedException(string message) { }
        public TypeUnloadedException(string message, System.Exception innerException) { }
    }
    [System.CLSCompliantAttribute(false)]
    public partial struct UInt16 : System.IComparable, System.IComparable<ushort>, System.IConvertible, System.IEquatable<ushort>, System.IFormattable
    {
        private ushort _dummy;
        public const ushort MaxValue = (ushort)65535;
        public const ushort MinValue = (ushort)0;
        public int CompareTo(object value) { throw null; }
        public int CompareTo(System.UInt16 value) { throw null; }
        public override bool Equals(object obj) { throw null; }
        public bool Equals(System.UInt16 obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public System.TypeCode GetTypeCode() { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.UInt16 Parse(System.ReadOnlySpan<char> s, System.Globalization.NumberStyles style=(System.Globalization.NumberStyles)(7), System.IFormatProvider provider=null) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.UInt16 Parse(string s) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.UInt16 Parse(string s, System.Globalization.NumberStyles style) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.UInt16 Parse(string s, System.Globalization.NumberStyles style, System.IFormatProvider provider) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.UInt16 Parse(string s, System.IFormatProvider provider) { throw null; }
        bool System.IConvertible.ToBoolean(System.IFormatProvider provider) { throw null; }
        byte System.IConvertible.ToByte(System.IFormatProvider provider) { throw null; }
        char System.IConvertible.ToChar(System.IFormatProvider provider) { throw null; }
        System.DateTime System.IConvertible.ToDateTime(System.IFormatProvider provider) { throw null; }
        decimal System.IConvertible.ToDecimal(System.IFormatProvider provider) { throw null; }
        double System.IConvertible.ToDouble(System.IFormatProvider provider) { throw null; }
        short System.IConvertible.ToInt16(System.IFormatProvider provider) { throw null; }
        int System.IConvertible.ToInt32(System.IFormatProvider provider) { throw null; }
        long System.IConvertible.ToInt64(System.IFormatProvider provider) { throw null; }
        sbyte System.IConvertible.ToSByte(System.IFormatProvider provider) { throw null; }
        float System.IConvertible.ToSingle(System.IFormatProvider provider) { throw null; }
        object System.IConvertible.ToType(System.Type type, System.IFormatProvider provider) { throw null; }
        System.UInt16 System.IConvertible.ToUInt16(System.IFormatProvider provider) { throw null; }
        uint System.IConvertible.ToUInt32(System.IFormatProvider provider) { throw null; }
        ulong System.IConvertible.ToUInt64(System.IFormatProvider provider) { throw null; }
        public override string ToString() { throw null; }
        public string ToString(System.IFormatProvider provider) { throw null; }
        public string ToString(string format) { throw null; }
        public string ToString(string format, System.IFormatProvider provider) { throw null; }
        public bool TryFormat(System.Span<char> destination, out int charsWritten, System.ReadOnlySpan<char> format=default(System.ReadOnlySpan<char>), System.IFormatProvider provider=null) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static bool TryParse(System.ReadOnlySpan<char> s, System.Globalization.NumberStyles style, System.IFormatProvider provider, out System.UInt16 result) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static bool TryParse(System.ReadOnlySpan<char> s, out System.UInt16 result) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static bool TryParse(string s, System.Globalization.NumberStyles style, System.IFormatProvider provider, out System.UInt16 result) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static bool TryParse(string s, out System.UInt16 result) { throw null; }
    }
    [System.CLSCompliantAttribute(false)]
    public partial struct UInt32 : System.IComparable, System.IComparable<uint>, System.IConvertible, System.IEquatable<uint>, System.IFormattable
    {
        private uint _dummy;
        public const uint MaxValue = (uint)4294967295;
        public const uint MinValue = (uint)0;
        public int CompareTo(object value) { throw null; }
        public int CompareTo(System.UInt32 value) { throw null; }
        public override bool Equals(object obj) { throw null; }
        public bool Equals(System.UInt32 obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public System.TypeCode GetTypeCode() { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.UInt32 Parse(System.ReadOnlySpan<char> s, System.Globalization.NumberStyles style=(System.Globalization.NumberStyles)(7), System.IFormatProvider provider=null) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.UInt32 Parse(string s) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.UInt32 Parse(string s, System.Globalization.NumberStyles style) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.UInt32 Parse(string s, System.Globalization.NumberStyles style, System.IFormatProvider provider) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.UInt32 Parse(string s, System.IFormatProvider provider) { throw null; }
        bool System.IConvertible.ToBoolean(System.IFormatProvider provider) { throw null; }
        byte System.IConvertible.ToByte(System.IFormatProvider provider) { throw null; }
        char System.IConvertible.ToChar(System.IFormatProvider provider) { throw null; }
        System.DateTime System.IConvertible.ToDateTime(System.IFormatProvider provider) { throw null; }
        decimal System.IConvertible.ToDecimal(System.IFormatProvider provider) { throw null; }
        double System.IConvertible.ToDouble(System.IFormatProvider provider) { throw null; }
        short System.IConvertible.ToInt16(System.IFormatProvider provider) { throw null; }
        int System.IConvertible.ToInt32(System.IFormatProvider provider) { throw null; }
        long System.IConvertible.ToInt64(System.IFormatProvider provider) { throw null; }
        sbyte System.IConvertible.ToSByte(System.IFormatProvider provider) { throw null; }
        float System.IConvertible.ToSingle(System.IFormatProvider provider) { throw null; }
        object System.IConvertible.ToType(System.Type type, System.IFormatProvider provider) { throw null; }
        ushort System.IConvertible.ToUInt16(System.IFormatProvider provider) { throw null; }
        System.UInt32 System.IConvertible.ToUInt32(System.IFormatProvider provider) { throw null; }
        ulong System.IConvertible.ToUInt64(System.IFormatProvider provider) { throw null; }
        public override string ToString() { throw null; }
        public string ToString(System.IFormatProvider provider) { throw null; }
        public string ToString(string format) { throw null; }
        public string ToString(string format, System.IFormatProvider provider) { throw null; }
        public bool TryFormat(System.Span<char> destination, out int charsWritten, System.ReadOnlySpan<char> format=default(System.ReadOnlySpan<char>), System.IFormatProvider provider=null) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static bool TryParse(System.ReadOnlySpan<char> s, System.Globalization.NumberStyles style, System.IFormatProvider provider, out System.UInt32 result) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static bool TryParse(System.ReadOnlySpan<char> s, out System.UInt32 result) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static bool TryParse(string s, System.Globalization.NumberStyles style, System.IFormatProvider provider, out System.UInt32 result) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static bool TryParse(string s, out System.UInt32 result) { throw null; }
    }
    [System.CLSCompliantAttribute(false)]
    public partial struct UInt64 : System.IComparable, System.IComparable<ulong>, System.IConvertible, System.IEquatable<ulong>, System.IFormattable
    {
        private ulong _dummy;
        public const ulong MaxValue = (ulong)18446744073709551615;
        public const ulong MinValue = (ulong)0;
        public int CompareTo(object value) { throw null; }
        public int CompareTo(System.UInt64 value) { throw null; }
        public override bool Equals(object obj) { throw null; }
        public bool Equals(System.UInt64 obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public System.TypeCode GetTypeCode() { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.UInt64 Parse(System.ReadOnlySpan<char> s, System.Globalization.NumberStyles style=(System.Globalization.NumberStyles)(7), System.IFormatProvider provider=null) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.UInt64 Parse(string s) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.UInt64 Parse(string s, System.Globalization.NumberStyles style) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.UInt64 Parse(string s, System.Globalization.NumberStyles style, System.IFormatProvider provider) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.UInt64 Parse(string s, System.IFormatProvider provider) { throw null; }
        bool System.IConvertible.ToBoolean(System.IFormatProvider provider) { throw null; }
        byte System.IConvertible.ToByte(System.IFormatProvider provider) { throw null; }
        char System.IConvertible.ToChar(System.IFormatProvider provider) { throw null; }
        System.DateTime System.IConvertible.ToDateTime(System.IFormatProvider provider) { throw null; }
        decimal System.IConvertible.ToDecimal(System.IFormatProvider provider) { throw null; }
        double System.IConvertible.ToDouble(System.IFormatProvider provider) { throw null; }
        short System.IConvertible.ToInt16(System.IFormatProvider provider) { throw null; }
        int System.IConvertible.ToInt32(System.IFormatProvider provider) { throw null; }
        long System.IConvertible.ToInt64(System.IFormatProvider provider) { throw null; }
        sbyte System.IConvertible.ToSByte(System.IFormatProvider provider) { throw null; }
        float System.IConvertible.ToSingle(System.IFormatProvider provider) { throw null; }
        object System.IConvertible.ToType(System.Type type, System.IFormatProvider provider) { throw null; }
        ushort System.IConvertible.ToUInt16(System.IFormatProvider provider) { throw null; }
        uint System.IConvertible.ToUInt32(System.IFormatProvider provider) { throw null; }
        System.UInt64 System.IConvertible.ToUInt64(System.IFormatProvider provider) { throw null; }
        public override string ToString() { throw null; }
        public string ToString(System.IFormatProvider provider) { throw null; }
        public string ToString(string format) { throw null; }
        public string ToString(string format, System.IFormatProvider provider) { throw null; }
        public bool TryFormat(System.Span<char> destination, out int charsWritten, System.ReadOnlySpan<char> format=default(System.ReadOnlySpan<char>), System.IFormatProvider provider=null) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static bool TryParse(System.ReadOnlySpan<char> s, System.Globalization.NumberStyles style, System.IFormatProvider provider, out System.UInt64 result) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static bool TryParse(System.ReadOnlySpan<char> s, out System.UInt64 result) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static bool TryParse(string s, System.Globalization.NumberStyles style, System.IFormatProvider provider, out System.UInt64 result) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static bool TryParse(string s, out System.UInt64 result) { throw null; }
    }
    [System.CLSCompliantAttribute(false)]
    public partial struct UIntPtr : System.IEquatable<System.UIntPtr>, System.Runtime.Serialization.ISerializable
    {
        private int _dummy;
        public static readonly System.UIntPtr Zero;
        public UIntPtr(uint value) { throw null; }
        public UIntPtr(ulong value) { throw null; }
        public unsafe UIntPtr(void* value) { throw null; }
        public static int Size { get { throw null; } }
        public static System.UIntPtr Add(System.UIntPtr pointer, int offset) { throw null; }
        public override bool Equals(object obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public static System.UIntPtr operator +(System.UIntPtr pointer, int offset) { throw null; }
        public static bool operator ==(System.UIntPtr value1, System.UIntPtr value2) { throw null; }
        public static explicit operator System.UIntPtr (uint value) { throw null; }
        public static explicit operator System.UIntPtr (ulong value) { throw null; }
        public static explicit operator uint (System.UIntPtr value) { throw null; }
        public static explicit operator ulong (System.UIntPtr value) { throw null; }
        public unsafe static explicit operator void* (System.UIntPtr value) { throw null; }
        public unsafe static explicit operator System.UIntPtr (void* value) { throw null; }
        public static bool operator !=(System.UIntPtr value1, System.UIntPtr value2) { throw null; }
        public static System.UIntPtr operator -(System.UIntPtr pointer, int offset) { throw null; }
        public static System.UIntPtr Subtract(System.UIntPtr pointer, int offset) { throw null; }
        bool System.IEquatable<System.UIntPtr>.Equals(System.UIntPtr value) { throw null; }
        void System.Runtime.Serialization.ISerializable.GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public unsafe void* ToPointer() { throw null; }
        public override string ToString() { throw null; }
        public uint ToUInt32() { throw null; }
        public ulong ToUInt64() { throw null; }
    }
    public partial class UnauthorizedAccessException : System.SystemException
    {
        public UnauthorizedAccessException() { }
        protected UnauthorizedAccessException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public UnauthorizedAccessException(string message) { }
        public UnauthorizedAccessException(string message, System.Exception inner) { }
    }
    public partial class UnhandledExceptionEventArgs : System.EventArgs
    {
        public UnhandledExceptionEventArgs(object exception, bool isTerminating) { }
        public object ExceptionObject { get { throw null; } }
        public bool IsTerminating { get { throw null; } }
    }
    public delegate void UnhandledExceptionEventHandler(object sender, System.UnhandledExceptionEventArgs e);
    public partial class Uri : System.Runtime.Serialization.ISerializable
    {
        public static readonly string SchemeDelimiter;
        public static readonly string UriSchemeFile;
        public static readonly string UriSchemeFtp;
        public static readonly string UriSchemeGopher;
        public static readonly string UriSchemeHttp;
        public static readonly string UriSchemeHttps;
        public static readonly string UriSchemeMailto;
        public static readonly string UriSchemeNetPipe;
        public static readonly string UriSchemeNetTcp;
        public static readonly string UriSchemeNews;
        public static readonly string UriSchemeNntp;
        protected Uri(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) { }
        public Uri(string uriString) { }
        [System.ObsoleteAttribute("The constructor has been deprecated. Please use new Uri(string). The dontEscape parameter is deprecated and is always false. http://go.microsoft.com/fwlink/?linkid=14202")]
        public Uri(string uriString, bool dontEscape) { }
        public Uri(string uriString, System.UriKind uriKind) { }
        public Uri(System.Uri baseUri, string relativeUri) { }
        [System.ObsoleteAttribute("The constructor has been deprecated. Please new Uri(Uri, string). The dontEscape parameter is deprecated and is always false. http://go.microsoft.com/fwlink/?linkid=14202")]
        public Uri(System.Uri baseUri, string relativeUri, bool dontEscape) { }
        public Uri(System.Uri baseUri, System.Uri relativeUri) { }
        public string AbsolutePath { get { throw null; } }
        public string AbsoluteUri { get { throw null; } }
        public string Authority { get { throw null; } }
        public string DnsSafeHost { get { throw null; } }
        public string Fragment { get { throw null; } }
        public string Host { get { throw null; } }
        public System.UriHostNameType HostNameType { get { throw null; } }
        public string IdnHost { get { throw null; } }
        public bool IsAbsoluteUri { get { throw null; } }
        public bool IsDefaultPort { get { throw null; } }
        public bool IsFile { get { throw null; } }
        public bool IsLoopback { get { throw null; } }
        public bool IsUnc { get { throw null; } }
        public string LocalPath { get { throw null; } }
        public string OriginalString { get { throw null; } }
        public string PathAndQuery { get { throw null; } }
        public int Port { get { throw null; } }
        public string Query { get { throw null; } }
        public string Scheme { get { throw null; } }
        public string[] Segments { get { throw null; } }
        public bool UserEscaped { get { throw null; } }
        public string UserInfo { get { throw null; } }
        [System.ObsoleteAttribute("The method has been deprecated. It is not used by the system. http://go.microsoft.com/fwlink/?linkid=14202")]
        protected virtual void Canonicalize() { }
        public static System.UriHostNameType CheckHostName(string name) { throw null; }
        public static bool CheckSchemeName(string schemeName) { throw null; }
        [System.ObsoleteAttribute("The method has been deprecated. It is not used by the system. http://go.microsoft.com/fwlink/?linkid=14202")]
        protected virtual void CheckSecurity() { }
        public static int Compare(System.Uri uri1, System.Uri uri2, System.UriComponents partsToCompare, System.UriFormat compareFormat, System.StringComparison comparisonType) { throw null; }
        public override bool Equals(object comparand) { throw null; }
        [System.ObsoleteAttribute("The method has been deprecated. It is not used by the system. http://go.microsoft.com/fwlink/?linkid=14202")]
        protected virtual void Escape() { }
        public static string EscapeDataString(string stringToEscape) { throw null; }
        [System.ObsoleteAttribute("The method has been deprecated. Please use GetComponents() or static EscapeUriString() to escape a Uri component or a string. http://go.microsoft.com/fwlink/?linkid=14202")]
        protected static string EscapeString(string str) { throw null; }
        public static string EscapeUriString(string stringToEscape) { throw null; }
        public static int FromHex(char digit) { throw null; }
        public string GetComponents(System.UriComponents components, System.UriFormat format) { throw null; }
        public override int GetHashCode() { throw null; }
        public string GetLeftPart(System.UriPartial part) { throw null; }
        protected void GetObjectData(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) { }
        public static string HexEscape(char character) { throw null; }
        public static char HexUnescape(string pattern, ref int index) { throw null; }
        [System.ObsoleteAttribute("The method has been deprecated. It is not used by the system. http://go.microsoft.com/fwlink/?linkid=14202")]
        protected virtual bool IsBadFileSystemCharacter(char character) { throw null; }
        public bool IsBaseOf(System.Uri uri) { throw null; }
        [System.ObsoleteAttribute("The method has been deprecated. It is not used by the system. http://go.microsoft.com/fwlink/?linkid=14202")]
        protected static bool IsExcludedCharacter(char character) { throw null; }
        public static bool IsHexDigit(char character) { throw null; }
        public static bool IsHexEncoding(string pattern, int index) { throw null; }
        [System.ObsoleteAttribute("The method has been deprecated. It is not used by the system. http://go.microsoft.com/fwlink/?linkid=14202")]
        protected virtual bool IsReservedCharacter(char character) { throw null; }
        public bool IsWellFormedOriginalString() { throw null; }
        public static bool IsWellFormedUriString(string uriString, System.UriKind uriKind) { throw null; }
        [System.ObsoleteAttribute("The method has been deprecated. Please use MakeRelativeUri(Uri uri). http://go.microsoft.com/fwlink/?linkid=14202")]
        public string MakeRelative(System.Uri toUri) { throw null; }
        public System.Uri MakeRelativeUri(System.Uri uri) { throw null; }
        public static bool operator ==(System.Uri uri1, System.Uri uri2) { throw null; }
        public static bool operator !=(System.Uri uri1, System.Uri uri2) { throw null; }
        [System.ObsoleteAttribute("The method has been deprecated. It is not used by the system. http://go.microsoft.com/fwlink/?linkid=14202")]
        protected virtual void Parse() { }
        void System.Runtime.Serialization.ISerializable.GetObjectData(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) { }
        public override string ToString() { throw null; }
        public static bool TryCreate(string uriString, System.UriKind uriKind, out System.Uri result) { throw null; }
        public static bool TryCreate(System.Uri baseUri, string relativeUri, out System.Uri result) { throw null; }
        public static bool TryCreate(System.Uri baseUri, System.Uri relativeUri, out System.Uri result) { throw null; }
        [System.ObsoleteAttribute("The method has been deprecated. Please use GetComponents() or static UnescapeDataString() to unescape a Uri component or a string. http://go.microsoft.com/fwlink/?linkid=14202")]
        protected virtual string Unescape(string path) { throw null; }
        public static string UnescapeDataString(string stringToUnescape) { throw null; }
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
    public partial class UriFormatException : System.FormatException, System.Runtime.Serialization.ISerializable
    {
        public UriFormatException() { }
        protected UriFormatException(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) { }
        public UriFormatException(string textString) { }
        public UriFormatException(string textString, System.Exception e) { }
        void System.Runtime.Serialization.ISerializable.GetObjectData(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) { }
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
    public abstract partial class UriParser
    {
        protected UriParser() { }
        protected virtual string GetComponents(System.Uri uri, System.UriComponents components, System.UriFormat format) { throw null; }
        protected virtual void InitializeAndValidate(System.Uri uri, out System.UriFormatException parsingError) { throw null; }
        protected virtual bool IsBaseOf(System.Uri baseUri, System.Uri relativeUri) { throw null; }
        public static bool IsKnownScheme(string schemeName) { throw null; }
        protected virtual bool IsWellFormedOriginalString(System.Uri uri) { throw null; }
        protected virtual System.UriParser OnNewUri() { throw null; }
        protected virtual void OnRegister(string schemeName, int defaultPort) { }
        public static void Register(System.UriParser uriParser, string schemeName, int defaultPort) { }
        protected virtual string Resolve(System.Uri baseUri, System.Uri relativeUri, out System.UriFormatException parsingError) { throw null; }
    }
    public enum UriPartial
    {
        Authority = 1,
        Path = 2,
        Query = 3,
        Scheme = 0,
    }
    public partial struct ValueTuple : System.Collections.IStructuralComparable, System.Collections.IStructuralEquatable, System.IComparable, System.IComparable<System.ValueTuple>, System.IEquatable<System.ValueTuple>, System.Runtime.CompilerServices.ITuple
    {
        object System.Runtime.CompilerServices.ITuple.this[int index] { get { throw null; } }
        int System.Runtime.CompilerServices.ITuple.Length { get { throw null; } }
        public int CompareTo(System.ValueTuple other) { throw null; }
        public static System.ValueTuple Create() { throw null; }
        public static System.ValueTuple<T1> Create<T1>(T1 item1) { throw null; }
        public static System.ValueTuple<T1, T2> Create<T1, T2>(T1 item1, T2 item2) { throw null; }
        public static System.ValueTuple<T1, T2, T3> Create<T1, T2, T3>(T1 item1, T2 item2, T3 item3) { throw null; }
        public static System.ValueTuple<T1, T2, T3, T4> Create<T1, T2, T3, T4>(T1 item1, T2 item2, T3 item3, T4 item4) { throw null; }
        public static System.ValueTuple<T1, T2, T3, T4, T5> Create<T1, T2, T3, T4, T5>(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5) { throw null; }
        public static System.ValueTuple<T1, T2, T3, T4, T5, T6> Create<T1, T2, T3, T4, T5, T6>(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6) { throw null; }
        public static System.ValueTuple<T1, T2, T3, T4, T5, T6, T7> Create<T1, T2, T3, T4, T5, T6, T7>(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7) { throw null; }
        public static System.ValueTuple<T1, T2, T3, T4, T5, T6, T7, System.ValueTuple<T8>> Create<T1, T2, T3, T4, T5, T6, T7, T8>(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, T8 item8) { throw null; }
        public override bool Equals(object obj) { throw null; }
        public bool Equals(System.ValueTuple other) { throw null; }
        public override int GetHashCode() { throw null; }
        int System.Collections.IStructuralComparable.CompareTo(object other, System.Collections.IComparer comparer) { throw null; }
        bool System.Collections.IStructuralEquatable.Equals(object other, System.Collections.IEqualityComparer comparer) { throw null; }
        int System.Collections.IStructuralEquatable.GetHashCode(System.Collections.IEqualityComparer comparer) { throw null; }
        int System.IComparable.CompareTo(object other) { throw null; }
        public override string ToString() { throw null; }
    }
    public partial struct ValueTuple<T1> : System.Collections.IStructuralComparable, System.Collections.IStructuralEquatable, System.IComparable, System.IComparable<System.ValueTuple<T1>>, System.IEquatable<System.ValueTuple<T1>>, System.Runtime.CompilerServices.ITuple
    {
        public T1 Item1;
        public ValueTuple(T1 item1) { throw null; }
        object System.Runtime.CompilerServices.ITuple.this[int index] { get { throw null; } }
        int System.Runtime.CompilerServices.ITuple.Length { get { throw null; } }
        public int CompareTo(System.ValueTuple<T1> other) { throw null; }
        public override bool Equals(object obj) { throw null; }
        public bool Equals(System.ValueTuple<T1> other) { throw null; }
        public override int GetHashCode() { throw null; }
        int System.Collections.IStructuralComparable.CompareTo(object other, System.Collections.IComparer comparer) { throw null; }
        bool System.Collections.IStructuralEquatable.Equals(object other, System.Collections.IEqualityComparer comparer) { throw null; }
        int System.Collections.IStructuralEquatable.GetHashCode(System.Collections.IEqualityComparer comparer) { throw null; }
        int System.IComparable.CompareTo(object other) { throw null; }
        public override string ToString() { throw null; }
    }
    public partial struct ValueTuple<T1, T2> : System.Collections.IStructuralComparable, System.Collections.IStructuralEquatable, System.IComparable, System.IComparable<System.ValueTuple<T1, T2>>, System.IEquatable<System.ValueTuple<T1, T2>>, System.Runtime.CompilerServices.ITuple
    {
        public T1 Item1;
        public T2 Item2;
        public ValueTuple(T1 item1, T2 item2) { throw null; }
        object System.Runtime.CompilerServices.ITuple.this[int index] { get { throw null; } }
        int System.Runtime.CompilerServices.ITuple.Length { get { throw null; } }
        public int CompareTo(System.ValueTuple<T1, T2> other) { throw null; }
        public override bool Equals(object obj) { throw null; }
        public bool Equals(System.ValueTuple<T1, T2> other) { throw null; }
        public override int GetHashCode() { throw null; }
        int System.Collections.IStructuralComparable.CompareTo(object other, System.Collections.IComparer comparer) { throw null; }
        bool System.Collections.IStructuralEquatable.Equals(object other, System.Collections.IEqualityComparer comparer) { throw null; }
        int System.Collections.IStructuralEquatable.GetHashCode(System.Collections.IEqualityComparer comparer) { throw null; }
        int System.IComparable.CompareTo(object other) { throw null; }
        public override string ToString() { throw null; }
    }
    public partial struct ValueTuple<T1, T2, T3> : System.Collections.IStructuralComparable, System.Collections.IStructuralEquatable, System.IComparable, System.IComparable<System.ValueTuple<T1, T2, T3>>, System.IEquatable<System.ValueTuple<T1, T2, T3>>, System.Runtime.CompilerServices.ITuple
    {
        public T1 Item1;
        public T2 Item2;
        public T3 Item3;
        public ValueTuple(T1 item1, T2 item2, T3 item3) { throw null; }
        object System.Runtime.CompilerServices.ITuple.this[int index] { get { throw null; } }
        int System.Runtime.CompilerServices.ITuple.Length { get { throw null; } }
        public int CompareTo(System.ValueTuple<T1, T2, T3> other) { throw null; }
        public override bool Equals(object obj) { throw null; }
        public bool Equals(System.ValueTuple<T1, T2, T3> other) { throw null; }
        public override int GetHashCode() { throw null; }
        int System.Collections.IStructuralComparable.CompareTo(object other, System.Collections.IComparer comparer) { throw null; }
        bool System.Collections.IStructuralEquatable.Equals(object other, System.Collections.IEqualityComparer comparer) { throw null; }
        int System.Collections.IStructuralEquatable.GetHashCode(System.Collections.IEqualityComparer comparer) { throw null; }
        int System.IComparable.CompareTo(object other) { throw null; }
        public override string ToString() { throw null; }
    }
    public partial struct ValueTuple<T1, T2, T3, T4> : System.Collections.IStructuralComparable, System.Collections.IStructuralEquatable, System.IComparable, System.IComparable<System.ValueTuple<T1, T2, T3, T4>>, System.IEquatable<System.ValueTuple<T1, T2, T3, T4>>, System.Runtime.CompilerServices.ITuple
    {
        public T1 Item1;
        public T2 Item2;
        public T3 Item3;
        public T4 Item4;
        public ValueTuple(T1 item1, T2 item2, T3 item3, T4 item4) { throw null; }
        object System.Runtime.CompilerServices.ITuple.this[int index] { get { throw null; } }
        int System.Runtime.CompilerServices.ITuple.Length { get { throw null; } }
        public int CompareTo(System.ValueTuple<T1, T2, T3, T4> other) { throw null; }
        public override bool Equals(object obj) { throw null; }
        public bool Equals(System.ValueTuple<T1, T2, T3, T4> other) { throw null; }
        public override int GetHashCode() { throw null; }
        int System.Collections.IStructuralComparable.CompareTo(object other, System.Collections.IComparer comparer) { throw null; }
        bool System.Collections.IStructuralEquatable.Equals(object other, System.Collections.IEqualityComparer comparer) { throw null; }
        int System.Collections.IStructuralEquatable.GetHashCode(System.Collections.IEqualityComparer comparer) { throw null; }
        int System.IComparable.CompareTo(object other) { throw null; }
        public override string ToString() { throw null; }
    }
    public partial struct ValueTuple<T1, T2, T3, T4, T5> : System.Collections.IStructuralComparable, System.Collections.IStructuralEquatable, System.IComparable, System.IComparable<System.ValueTuple<T1, T2, T3, T4, T5>>, System.IEquatable<System.ValueTuple<T1, T2, T3, T4, T5>>, System.Runtime.CompilerServices.ITuple
    {
        public T1 Item1;
        public T2 Item2;
        public T3 Item3;
        public T4 Item4;
        public T5 Item5;
        public ValueTuple(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5) { throw null; }
        object System.Runtime.CompilerServices.ITuple.this[int index] { get { throw null; } }
        int System.Runtime.CompilerServices.ITuple.Length { get { throw null; } }
        public int CompareTo(System.ValueTuple<T1, T2, T3, T4, T5> other) { throw null; }
        public override bool Equals(object obj) { throw null; }
        public bool Equals(System.ValueTuple<T1, T2, T3, T4, T5> other) { throw null; }
        public override int GetHashCode() { throw null; }
        int System.Collections.IStructuralComparable.CompareTo(object other, System.Collections.IComparer comparer) { throw null; }
        bool System.Collections.IStructuralEquatable.Equals(object other, System.Collections.IEqualityComparer comparer) { throw null; }
        int System.Collections.IStructuralEquatable.GetHashCode(System.Collections.IEqualityComparer comparer) { throw null; }
        int System.IComparable.CompareTo(object other) { throw null; }
        public override string ToString() { throw null; }
    }
    public partial struct ValueTuple<T1, T2, T3, T4, T5, T6> : System.Collections.IStructuralComparable, System.Collections.IStructuralEquatable, System.IComparable, System.IComparable<System.ValueTuple<T1, T2, T3, T4, T5, T6>>, System.IEquatable<System.ValueTuple<T1, T2, T3, T4, T5, T6>>, System.Runtime.CompilerServices.ITuple
    {
        public T1 Item1;
        public T2 Item2;
        public T3 Item3;
        public T4 Item4;
        public T5 Item5;
        public T6 Item6;
        public ValueTuple(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6) { throw null; }
        object System.Runtime.CompilerServices.ITuple.this[int index] { get { throw null; } }
        int System.Runtime.CompilerServices.ITuple.Length { get { throw null; } }
        public int CompareTo(System.ValueTuple<T1, T2, T3, T4, T5, T6> other) { throw null; }
        public override bool Equals(object obj) { throw null; }
        public bool Equals(System.ValueTuple<T1, T2, T3, T4, T5, T6> other) { throw null; }
        public override int GetHashCode() { throw null; }
        int System.Collections.IStructuralComparable.CompareTo(object other, System.Collections.IComparer comparer) { throw null; }
        bool System.Collections.IStructuralEquatable.Equals(object other, System.Collections.IEqualityComparer comparer) { throw null; }
        int System.Collections.IStructuralEquatable.GetHashCode(System.Collections.IEqualityComparer comparer) { throw null; }
        int System.IComparable.CompareTo(object other) { throw null; }
        public override string ToString() { throw null; }
    }
    public partial struct ValueTuple<T1, T2, T3, T4, T5, T6, T7> : System.Collections.IStructuralComparable, System.Collections.IStructuralEquatable, System.IComparable, System.IComparable<System.ValueTuple<T1, T2, T3, T4, T5, T6, T7>>, System.IEquatable<System.ValueTuple<T1, T2, T3, T4, T5, T6, T7>>, System.Runtime.CompilerServices.ITuple
    {
        public T1 Item1;
        public T2 Item2;
        public T3 Item3;
        public T4 Item4;
        public T5 Item5;
        public T6 Item6;
        public T7 Item7;
        public ValueTuple(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7) { throw null; }
        object System.Runtime.CompilerServices.ITuple.this[int index] { get { throw null; } }
        int System.Runtime.CompilerServices.ITuple.Length { get { throw null; } }
        public int CompareTo(System.ValueTuple<T1, T2, T3, T4, T5, T6, T7> other) { throw null; }
        public override bool Equals(object obj) { throw null; }
        public bool Equals(System.ValueTuple<T1, T2, T3, T4, T5, T6, T7> other) { throw null; }
        public override int GetHashCode() { throw null; }
        int System.Collections.IStructuralComparable.CompareTo(object other, System.Collections.IComparer comparer) { throw null; }
        bool System.Collections.IStructuralEquatable.Equals(object other, System.Collections.IEqualityComparer comparer) { throw null; }
        int System.Collections.IStructuralEquatable.GetHashCode(System.Collections.IEqualityComparer comparer) { throw null; }
        int System.IComparable.CompareTo(object other) { throw null; }
        public override string ToString() { throw null; }
    }
    public partial struct ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest> : System.Collections.IStructuralComparable, System.Collections.IStructuralEquatable, System.IComparable, System.IComparable<System.ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest>>, System.IEquatable<System.ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest>>, System.Runtime.CompilerServices.ITuple where TRest : struct
    {
        public T1 Item1;
        public T2 Item2;
        public T3 Item3;
        public T4 Item4;
        public T5 Item5;
        public T6 Item6;
        public T7 Item7;
        public TRest Rest;
        public ValueTuple(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, TRest rest) { throw null; }
        object System.Runtime.CompilerServices.ITuple.this[int index] { get { throw null; } }
        int System.Runtime.CompilerServices.ITuple.Length { get { throw null; } }
        public int CompareTo(System.ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest> other) { throw null; }
        public override bool Equals(object obj) { throw null; }
        public bool Equals(System.ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest> other) { throw null; }
        public override int GetHashCode() { throw null; }
        int System.Collections.IStructuralComparable.CompareTo(object other, System.Collections.IComparer comparer) { throw null; }
        bool System.Collections.IStructuralEquatable.Equals(object other, System.Collections.IEqualityComparer comparer) { throw null; }
        int System.Collections.IStructuralEquatable.GetHashCode(System.Collections.IEqualityComparer comparer) { throw null; }
        int System.IComparable.CompareTo(object other) { throw null; }
        public override string ToString() { throw null; }
    }
    public abstract partial class ValueType
    {
        protected ValueType() { }
        public override bool Equals(object obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public override string ToString() { throw null; }
    }
    public sealed partial class Version : System.ICloneable, System.IComparable, System.IComparable<System.Version>, System.IEquatable<System.Version>
    {
        public Version() { }
        public Version(int major, int minor) { }
        public Version(int major, int minor, int build) { }
        public Version(int major, int minor, int build, int revision) { }
        public Version(string version) { }
        public int Build { get { throw null; } }
        public int Major { get { throw null; } }
        public short MajorRevision { get { throw null; } }
        public int Minor { get { throw null; } }
        public short MinorRevision { get { throw null; } }
        public int Revision { get { throw null; } }
        public object Clone() { throw null; }
        public int CompareTo(object version) { throw null; }
        public int CompareTo(System.Version value) { throw null; }
        public override bool Equals(object obj) { throw null; }
        public bool Equals(System.Version obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Version v1, System.Version v2) { throw null; }
        public static bool operator >(System.Version v1, System.Version v2) { throw null; }
        public static bool operator >=(System.Version v1, System.Version v2) { throw null; }
        public static bool operator !=(System.Version v1, System.Version v2) { throw null; }
        public static bool operator <(System.Version v1, System.Version v2) { throw null; }
        public static bool operator <=(System.Version v1, System.Version v2) { throw null; }
        public static System.Version Parse(System.ReadOnlySpan<char> input) { throw null; }
        public static System.Version Parse(string input) { throw null; }
        public override string ToString() { throw null; }
        public string ToString(int fieldCount) { throw null; }
        public bool TryFormat(System.Span<char> destination, int fieldCount, out int charsWritten) { throw null; }
        public bool TryFormat(System.Span<char> destination, out int charsWritten) { throw null; }
        public static bool TryParse(System.ReadOnlySpan<char> input, out System.Version result) { throw null; }
        public static bool TryParse(string input, out System.Version result) { throw null; }
    }
    public partial struct Void
    {
    }
    public partial class WeakReference : System.Runtime.Serialization.ISerializable
    {
        public WeakReference(object target) { }
        public WeakReference(object target, bool trackResurrection) { }
        protected WeakReference(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public virtual bool IsAlive { get { throw null; } }
        public virtual object Target { get { throw null; } set { } }
        public virtual bool TrackResurrection { get { throw null; } }
        ~WeakReference() { }
        public virtual void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    }
    public sealed partial class WeakReference<T> : System.Runtime.Serialization.ISerializable where T : class
    {
        public WeakReference(T target) { }
        public WeakReference(T target, bool trackResurrection) { }
        ~WeakReference() { }
        public void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public void SetTarget(T target) { }
        public bool TryGetTarget(out T target) { throw null; }
    }
}
namespace System.Buffers
{
    public partial interface IRetainable
    {
        bool Release();
        void Retain();
    }
    public partial struct MemoryHandle : System.IDisposable
    {
        private object _dummy;
        [System.CLSCompliantAttribute(false)]
        public unsafe MemoryHandle(System.Buffers.IRetainable owner, void* pointer=null, System.Runtime.InteropServices.GCHandle handle=default(System.Runtime.InteropServices.GCHandle)) { throw null; }
        public bool HasPointer { get { throw null; } }
        [System.CLSCompliantAttribute(false)]
        public unsafe void* Pointer { get { throw null; } }
        public void Dispose() { }
    }
    public abstract partial class OwnedMemory<T> : System.Buffers.IRetainable, System.IDisposable
    {
        protected OwnedMemory() { }
        public abstract bool IsDisposed { get; }
        protected abstract bool IsRetained { get; }
        public abstract int Length { get; }
        public System.Memory<T> Memory { get { throw null; } }
        public abstract System.Span<T> Span { get; }
        public void Dispose() { }
        protected abstract void Dispose(bool disposing);
        public abstract System.Buffers.MemoryHandle Pin();
        public abstract bool Release();
        public abstract void Retain();
        protected internal abstract bool TryGetArray(out System.ArraySegment<T> arraySegment);
    }
    public delegate void ReadOnlySpanAction<T, in TArg>(System.ReadOnlySpan<T> span, TArg arg);
    public delegate void SpanAction<T, in TArg>(System.Span<T> span, TArg arg);
}
namespace System.Collections
{
    public partial struct DictionaryEntry
    {
        private object _dummy;
        public DictionaryEntry(object key, object value) { throw null; }
        public object Key { get { throw null; } set { } }
        public object Value { get { throw null; } set { } }
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
        public void Deconstruct(out object key, out object value) { throw null; }
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
    public partial class KeyNotFoundException : System.SystemException
    {
        public KeyNotFoundException() { }
        protected KeyNotFoundException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public KeyNotFoundException(string message) { }
        public KeyNotFoundException(string message, System.Exception innerException) { }
    }
    public static partial class KeyValuePair
    {
        public static System.Collections.Generic.KeyValuePair<TKey, TValue> Create<TKey, TValue>(TKey key, TValue value) { throw null; }
    }
    public readonly partial struct KeyValuePair<TKey, TValue>
    {
        private readonly TKey key;
        private readonly TValue value;
        public KeyValuePair(TKey key, TValue value) { throw null; }
        public TKey Key { get { throw null; } }
        public TValue Value { get { throw null; } }
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
        public void Deconstruct(out TKey key, out TValue value) { throw null; }
        public override string ToString() { throw null; }
    }
}
namespace System.Collections.ObjectModel
{
    public partial class Collection<T> : System.Collections.Generic.ICollection<T>, System.Collections.Generic.IEnumerable<T>, System.Collections.Generic.IList<T>, System.Collections.Generic.IReadOnlyCollection<T>, System.Collections.Generic.IReadOnlyList<T>, System.Collections.ICollection, System.Collections.IEnumerable, System.Collections.IList
    {
        public Collection() { }
        public Collection(System.Collections.Generic.IList<T> list) { }
        public int Count { get { throw null; } }
        public T this[int index] { get { throw null; } set { } }
        protected System.Collections.Generic.IList<T> Items { get { throw null; } }
        bool System.Collections.Generic.ICollection<T>.IsReadOnly { get { throw null; } }
        bool System.Collections.ICollection.IsSynchronized { get { throw null; } }
        object System.Collections.ICollection.SyncRoot { get { throw null; } }
        bool System.Collections.IList.IsFixedSize { get { throw null; } }
        bool System.Collections.IList.IsReadOnly { get { throw null; } }
        object System.Collections.IList.this[int index] { get { throw null; } set { } }
        public void Add(T item) { }
        public void Clear() { }
        protected virtual void ClearItems() { }
        public bool Contains(T item) { throw null; }
        public void CopyTo(T[] array, int index) { }
        public System.Collections.Generic.IEnumerator<T> GetEnumerator() { throw null; }
        public int IndexOf(T item) { throw null; }
        public void Insert(int index, T item) { }
        protected virtual void InsertItem(int index, T item) { }
        public bool Remove(T item) { throw null; }
        public void RemoveAt(int index) { }
        protected virtual void RemoveItem(int index) { }
        protected virtual void SetItem(int index, T item) { }
        void System.Collections.ICollection.CopyTo(System.Array array, int index) { }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        int System.Collections.IList.Add(object value) { throw null; }
        bool System.Collections.IList.Contains(object value) { throw null; }
        int System.Collections.IList.IndexOf(object value) { throw null; }
        void System.Collections.IList.Insert(int index, object value) { }
        void System.Collections.IList.Remove(object value) { }
    }
    public partial class ReadOnlyCollection<T> : System.Collections.Generic.ICollection<T>, System.Collections.Generic.IEnumerable<T>, System.Collections.Generic.IList<T>, System.Collections.Generic.IReadOnlyCollection<T>, System.Collections.Generic.IReadOnlyList<T>, System.Collections.ICollection, System.Collections.IEnumerable, System.Collections.IList
    {
        public ReadOnlyCollection(System.Collections.Generic.IList<T> list) { }
        public int Count { get { throw null; } }
        public T this[int index] { get { throw null; } }
        protected System.Collections.Generic.IList<T> Items { get { throw null; } }
        bool System.Collections.Generic.ICollection<T>.IsReadOnly { get { throw null; } }
        T System.Collections.Generic.IList<T>.this[int index] { get { throw null; } set { } }
        bool System.Collections.ICollection.IsSynchronized { get { throw null; } }
        object System.Collections.ICollection.SyncRoot { get { throw null; } }
        bool System.Collections.IList.IsFixedSize { get { throw null; } }
        bool System.Collections.IList.IsReadOnly { get { throw null; } }
        object System.Collections.IList.this[int index] { get { throw null; } set { } }
        public bool Contains(T value) { throw null; }
        public void CopyTo(T[] array, int index) { }
        public System.Collections.Generic.IEnumerator<T> GetEnumerator() { throw null; }
        public int IndexOf(T value) { throw null; }
        void System.Collections.Generic.ICollection<T>.Add(T value) { }
        void System.Collections.Generic.ICollection<T>.Clear() { }
        bool System.Collections.Generic.ICollection<T>.Remove(T value) { throw null; }
        void System.Collections.Generic.IList<T>.Insert(int index, T value) { }
        void System.Collections.Generic.IList<T>.RemoveAt(int index) { }
        void System.Collections.ICollection.CopyTo(System.Array array, int index) { }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        int System.Collections.IList.Add(object value) { throw null; }
        void System.Collections.IList.Clear() { }
        bool System.Collections.IList.Contains(object value) { throw null; }
        int System.Collections.IList.IndexOf(object value) { throw null; }
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
        [System.CLSCompliantAttribute(false)]
        public DefaultValueAttribute(sbyte value) { }
        public DefaultValueAttribute(float value) { }
        public DefaultValueAttribute(string value) { }
        public DefaultValueAttribute(System.Type type, string value) { }
        [System.CLSCompliantAttribute(false)]
        public DefaultValueAttribute(ushort value) { }
        [System.CLSCompliantAttribute(false)]
        public DefaultValueAttribute(uint value) { }
        [System.CLSCompliantAttribute(false)]
        public DefaultValueAttribute(ulong value) { }
        public virtual object Value { get { throw null; } }
        public override bool Equals(object obj) { throw null; }
        public override int GetHashCode() { throw null; }
        protected void SetValue(object value) { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(6140))]
    public sealed partial class EditorBrowsableAttribute : System.Attribute
    {
        public EditorBrowsableAttribute() { }
        public EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState state) { }
        public System.ComponentModel.EditorBrowsableState State { get { throw null; } }
        public override bool Equals(object obj) { throw null; }
        public override int GetHashCode() { throw null; }
    }
    public enum EditorBrowsableState
    {
        Advanced = 2,
        Always = 0,
        Never = 1,
    }
}
namespace System.Configuration.Assemblies
{
    public enum AssemblyHashAlgorithm
    {
        MD5 = 32771,
        None = 0,
        SHA1 = 32772,
        SHA256 = 32780,
        SHA384 = 32781,
        SHA512 = 32782,
    }
    public enum AssemblyVersionCompatibility
    {
        SameDomain = 3,
        SameMachine = 1,
        SameProcess = 2,
    }
}
namespace System.Diagnostics
{
    [System.AttributeUsageAttribute((System.AttributeTargets)(68), AllowMultiple=true)]
    public sealed partial class ConditionalAttribute : System.Attribute
    {
        public ConditionalAttribute(string conditionString) { }
        public string ConditionString { get { throw null; } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(3), AllowMultiple=false)]
    public sealed partial class DebuggableAttribute : System.Attribute
    {
        public DebuggableAttribute(bool isJITTrackingEnabled, bool isJITOptimizerDisabled) { }
        public DebuggableAttribute(System.Diagnostics.DebuggableAttribute.DebuggingModes modes) { }
        public System.Diagnostics.DebuggableAttribute.DebuggingModes DebuggingFlags { get { throw null; } }
        public bool IsJITOptimizerDisabled { get { throw null; } }
        public bool IsJITTrackingEnabled { get { throw null; } }
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
    public abstract partial class Calendar : System.ICloneable
    {
        public const int CurrentEra = 0;
        protected Calendar() { }
        public virtual System.Globalization.CalendarAlgorithmType AlgorithmType { get { throw null; } }
        protected virtual int DaysInYearBeforeMinSupportedYear { get { throw null; } }
        public abstract int[] Eras { get; }
        public bool IsReadOnly { get { throw null; } }
        public virtual System.DateTime MaxSupportedDateTime { get { throw null; } }
        public virtual System.DateTime MinSupportedDateTime { get { throw null; } }
        public virtual int TwoDigitYearMax { get { throw null; } set { } }
        public virtual System.DateTime AddDays(System.DateTime time, int days) { throw null; }
        public virtual System.DateTime AddHours(System.DateTime time, int hours) { throw null; }
        public virtual System.DateTime AddMilliseconds(System.DateTime time, double milliseconds) { throw null; }
        public virtual System.DateTime AddMinutes(System.DateTime time, int minutes) { throw null; }
        public abstract System.DateTime AddMonths(System.DateTime time, int months);
        public virtual System.DateTime AddSeconds(System.DateTime time, int seconds) { throw null; }
        public virtual System.DateTime AddWeeks(System.DateTime time, int weeks) { throw null; }
        public abstract System.DateTime AddYears(System.DateTime time, int years);
        public virtual object Clone() { throw null; }
        public abstract int GetDayOfMonth(System.DateTime time);
        public abstract System.DayOfWeek GetDayOfWeek(System.DateTime time);
        public abstract int GetDayOfYear(System.DateTime time);
        public virtual int GetDaysInMonth(int year, int month) { throw null; }
        public abstract int GetDaysInMonth(int year, int month, int era);
        public virtual int GetDaysInYear(int year) { throw null; }
        public abstract int GetDaysInYear(int year, int era);
        public abstract int GetEra(System.DateTime time);
        public virtual int GetHour(System.DateTime time) { throw null; }
        public virtual int GetLeapMonth(int year) { throw null; }
        public virtual int GetLeapMonth(int year, int era) { throw null; }
        public virtual double GetMilliseconds(System.DateTime time) { throw null; }
        public virtual int GetMinute(System.DateTime time) { throw null; }
        public abstract int GetMonth(System.DateTime time);
        public virtual int GetMonthsInYear(int year) { throw null; }
        public abstract int GetMonthsInYear(int year, int era);
        public virtual int GetSecond(System.DateTime time) { throw null; }
        public virtual int GetWeekOfYear(System.DateTime time, System.Globalization.CalendarWeekRule rule, System.DayOfWeek firstDayOfWeek) { throw null; }
        public abstract int GetYear(System.DateTime time);
        public virtual bool IsLeapDay(int year, int month, int day) { throw null; }
        public abstract bool IsLeapDay(int year, int month, int day, int era);
        public virtual bool IsLeapMonth(int year, int month) { throw null; }
        public abstract bool IsLeapMonth(int year, int month, int era);
        public virtual bool IsLeapYear(int year) { throw null; }
        public abstract bool IsLeapYear(int year, int era);
        public static System.Globalization.Calendar ReadOnly(System.Globalization.Calendar calendar) { throw null; }
        public virtual System.DateTime ToDateTime(int year, int month, int day, int hour, int minute, int second, int millisecond) { throw null; }
        public abstract System.DateTime ToDateTime(int year, int month, int day, int hour, int minute, int second, int millisecond, int era);
        public virtual int ToFourDigitYear(int year) { throw null; }
    }
    public enum CalendarAlgorithmType
    {
        LunarCalendar = 2,
        LunisolarCalendar = 3,
        SolarCalendar = 1,
        Unknown = 0,
    }
    public enum CalendarWeekRule
    {
        FirstDay = 0,
        FirstFourDayWeek = 2,
        FirstFullWeek = 1,
    }
    public static partial class CharUnicodeInfo
    {
        public static int GetDecimalDigitValue(char ch) { throw null; }
        public static int GetDecimalDigitValue(string s, int index) { throw null; }
        public static int GetDigitValue(char ch) { throw null; }
        public static int GetDigitValue(string s, int index) { throw null; }
        public static double GetNumericValue(char ch) { throw null; }
        public static double GetNumericValue(string s, int index) { throw null; }
        public static System.Globalization.UnicodeCategory GetUnicodeCategory(char ch) { throw null; }
        public static System.Globalization.UnicodeCategory GetUnicodeCategory(string s, int index) { throw null; }
    }
    public partial class ChineseLunisolarCalendar : System.Globalization.EastAsianLunisolarCalendar
    {
        public const int ChineseEra = 1;
        public ChineseLunisolarCalendar() { }
        protected override int DaysInYearBeforeMinSupportedYear { get { throw null; } }
        public override int[] Eras { get { throw null; } }
        public override System.DateTime MaxSupportedDateTime { get { throw null; } }
        public override System.DateTime MinSupportedDateTime { get { throw null; } }
        public override int GetEra(System.DateTime time) { throw null; }
    }
    public partial class CompareInfo : System.Runtime.Serialization.IDeserializationCallback
    {
        internal CompareInfo() { }
        public int LCID { get { throw null; } }
        public virtual string Name { get { throw null; } }
        public System.Globalization.SortVersion Version { get { throw null; } }
        public virtual int Compare(string string1, int offset1, int length1, string string2, int offset2, int length2) { throw null; }
        public virtual int Compare(string string1, int offset1, int length1, string string2, int offset2, int length2, System.Globalization.CompareOptions options) { throw null; }
        public virtual int Compare(string string1, int offset1, string string2, int offset2) { throw null; }
        public virtual int Compare(string string1, int offset1, string string2, int offset2, System.Globalization.CompareOptions options) { throw null; }
        public virtual int Compare(string string1, string string2) { throw null; }
        public virtual int Compare(string string1, string string2, System.Globalization.CompareOptions options) { throw null; }
        public override bool Equals(object value) { throw null; }
        public static System.Globalization.CompareInfo GetCompareInfo(int culture) { throw null; }
        public static System.Globalization.CompareInfo GetCompareInfo(int culture, System.Reflection.Assembly assembly) { throw null; }
        public static System.Globalization.CompareInfo GetCompareInfo(string name) { throw null; }
        public static System.Globalization.CompareInfo GetCompareInfo(string name, System.Reflection.Assembly assembly) { throw null; }
        public override int GetHashCode() { throw null; }
        public virtual int GetHashCode(string source, System.Globalization.CompareOptions options) { throw null; }
        public virtual System.Globalization.SortKey GetSortKey(string source) { throw null; }
        public virtual System.Globalization.SortKey GetSortKey(string source, System.Globalization.CompareOptions options) { throw null; }
        public virtual int IndexOf(string source, char value) { throw null; }
        public virtual int IndexOf(string source, char value, System.Globalization.CompareOptions options) { throw null; }
        public virtual int IndexOf(string source, char value, int startIndex) { throw null; }
        public virtual int IndexOf(string source, char value, int startIndex, System.Globalization.CompareOptions options) { throw null; }
        public virtual int IndexOf(string source, char value, int startIndex, int count) { throw null; }
        public virtual int IndexOf(string source, char value, int startIndex, int count, System.Globalization.CompareOptions options) { throw null; }
        public virtual int IndexOf(string source, string value) { throw null; }
        public virtual int IndexOf(string source, string value, System.Globalization.CompareOptions options) { throw null; }
        public virtual int IndexOf(string source, string value, int startIndex) { throw null; }
        public virtual int IndexOf(string source, string value, int startIndex, System.Globalization.CompareOptions options) { throw null; }
        public virtual int IndexOf(string source, string value, int startIndex, int count) { throw null; }
        public virtual int IndexOf(string source, string value, int startIndex, int count, System.Globalization.CompareOptions options) { throw null; }
        public virtual bool IsPrefix(string source, string prefix) { throw null; }
        public virtual bool IsPrefix(string source, string prefix, System.Globalization.CompareOptions options) { throw null; }
        public static bool IsSortable(char ch) { throw null; }
        public static bool IsSortable(string text) { throw null; }
        public virtual bool IsSuffix(string source, string suffix) { throw null; }
        public virtual bool IsSuffix(string source, string suffix, System.Globalization.CompareOptions options) { throw null; }
        public virtual int LastIndexOf(string source, char value) { throw null; }
        public virtual int LastIndexOf(string source, char value, System.Globalization.CompareOptions options) { throw null; }
        public virtual int LastIndexOf(string source, char value, int startIndex) { throw null; }
        public virtual int LastIndexOf(string source, char value, int startIndex, System.Globalization.CompareOptions options) { throw null; }
        public virtual int LastIndexOf(string source, char value, int startIndex, int count) { throw null; }
        public virtual int LastIndexOf(string source, char value, int startIndex, int count, System.Globalization.CompareOptions options) { throw null; }
        public virtual int LastIndexOf(string source, string value) { throw null; }
        public virtual int LastIndexOf(string source, string value, System.Globalization.CompareOptions options) { throw null; }
        public virtual int LastIndexOf(string source, string value, int startIndex) { throw null; }
        public virtual int LastIndexOf(string source, string value, int startIndex, System.Globalization.CompareOptions options) { throw null; }
        public virtual int LastIndexOf(string source, string value, int startIndex, int count) { throw null; }
        public virtual int LastIndexOf(string source, string value, int startIndex, int count, System.Globalization.CompareOptions options) { throw null; }
        void System.Runtime.Serialization.IDeserializationCallback.OnDeserialization(object sender) { }
        public override string ToString() { throw null; }
    }
    [System.FlagsAttribute]
    public enum CompareOptions
    {
        IgnoreCase = 1,
        IgnoreKanaType = 8,
        IgnoreNonSpace = 2,
        IgnoreSymbols = 4,
        IgnoreWidth = 16,
        None = 0,
        Ordinal = 1073741824,
        OrdinalIgnoreCase = 268435456,
        StringSort = 536870912,
    }
    public partial class CultureInfo : System.ICloneable, System.IFormatProvider
    {
        public CultureInfo(int culture) { }
        public CultureInfo(int culture, bool useUserOverride) { }
        public CultureInfo(string name) { }
        public CultureInfo(string name, bool useUserOverride) { }
        public virtual System.Globalization.Calendar Calendar { get { throw null; } }
        public virtual System.Globalization.CompareInfo CompareInfo { get { throw null; } }
        public System.Globalization.CultureTypes CultureTypes { get { throw null; } }
        public static System.Globalization.CultureInfo CurrentCulture { get { throw null; } set { } }
        public static System.Globalization.CultureInfo CurrentUICulture { get { throw null; } set { } }
        public virtual System.Globalization.DateTimeFormatInfo DateTimeFormat { get { throw null; } set { } }
        public static System.Globalization.CultureInfo DefaultThreadCurrentCulture { get { throw null; } set { } }
        public static System.Globalization.CultureInfo DefaultThreadCurrentUICulture { get { throw null; } set { } }
        public virtual string DisplayName { get { throw null; } }
        public virtual string EnglishName { get { throw null; } }
        public string IetfLanguageTag { get { throw null; } }
        public static System.Globalization.CultureInfo InstalledUICulture { get { throw null; } }
        public static System.Globalization.CultureInfo InvariantCulture { get { throw null; } }
        public virtual bool IsNeutralCulture { get { throw null; } }
        public bool IsReadOnly { get { throw null; } }
        public virtual int KeyboardLayoutId { get { throw null; } }
        public virtual int LCID { get { throw null; } }
        public virtual string Name { get { throw null; } }
        public virtual string NativeName { get { throw null; } }
        public virtual System.Globalization.NumberFormatInfo NumberFormat { get { throw null; } set { } }
        public virtual System.Globalization.Calendar[] OptionalCalendars { get { throw null; } }
        public virtual System.Globalization.CultureInfo Parent { get { throw null; } }
        public virtual System.Globalization.TextInfo TextInfo { get { throw null; } }
        public virtual string ThreeLetterISOLanguageName { get { throw null; } }
        public virtual string ThreeLetterWindowsLanguageName { get { throw null; } }
        public virtual string TwoLetterISOLanguageName { get { throw null; } }
        public bool UseUserOverride { get { throw null; } }
        public void ClearCachedData() { }
        public virtual object Clone() { throw null; }
        public static System.Globalization.CultureInfo CreateSpecificCulture(string name) { throw null; }
        public override bool Equals(object value) { throw null; }
        public System.Globalization.CultureInfo GetConsoleFallbackUICulture() { throw null; }
        public static System.Globalization.CultureInfo GetCultureInfo(int culture) { throw null; }
        public static System.Globalization.CultureInfo GetCultureInfo(string name) { throw null; }
        public static System.Globalization.CultureInfo GetCultureInfo(string name, string altName) { throw null; }
        public static System.Globalization.CultureInfo GetCultureInfoByIetfLanguageTag(string name) { throw null; }
        public static System.Globalization.CultureInfo[] GetCultures(System.Globalization.CultureTypes types) { throw null; }
        public virtual object GetFormat(System.Type formatType) { throw null; }
        public override int GetHashCode() { throw null; }
        public static System.Globalization.CultureInfo ReadOnly(System.Globalization.CultureInfo ci) { throw null; }
        public override string ToString() { throw null; }
    }
    public partial class CultureNotFoundException : System.ArgumentException
    {
        public CultureNotFoundException() { }
        protected CultureNotFoundException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public CultureNotFoundException(string message) { }
        public CultureNotFoundException(string message, System.Exception innerException) { }
        public CultureNotFoundException(string message, int invalidCultureId, System.Exception innerException) { }
        public CultureNotFoundException(string paramName, int invalidCultureId, string message) { }
        public CultureNotFoundException(string paramName, string message) { }
        public CultureNotFoundException(string message, string invalidCultureName, System.Exception innerException) { }
        public CultureNotFoundException(string paramName, string invalidCultureName, string message) { }
        public virtual System.Nullable<int> InvalidCultureId { get { throw null; } }
        public virtual string InvalidCultureName { get { throw null; } }
        public override string Message { get { throw null; } }
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    }
    [System.FlagsAttribute]
    public enum CultureTypes
    {
        AllCultures = 7,
        [System.ObsoleteAttribute("This value has been deprecated.  Please use other values in CultureTypes.")]
        FrameworkCultures = 64,
        InstalledWin32Cultures = 4,
        NeutralCultures = 1,
        ReplacementCultures = 16,
        SpecificCultures = 2,
        UserCustomCulture = 8,
        [System.ObsoleteAttribute("This value has been deprecated.  Please use other values in CultureTypes.")]
        WindowsOnlyCultures = 32,
    }
    public sealed partial class DateTimeFormatInfo : System.ICloneable, System.IFormatProvider
    {
        public DateTimeFormatInfo() { }
        public string[] AbbreviatedDayNames { get { throw null; } set { } }
        public string[] AbbreviatedMonthGenitiveNames { get { throw null; } set { } }
        public string[] AbbreviatedMonthNames { get { throw null; } set { } }
        public string AMDesignator { get { throw null; } set { } }
        public System.Globalization.Calendar Calendar { get { throw null; } set { } }
        public System.Globalization.CalendarWeekRule CalendarWeekRule { get { throw null; } set { } }
        public static System.Globalization.DateTimeFormatInfo CurrentInfo { get { throw null; } }
        public string DateSeparator { get { throw null; } set { } }
        public string[] DayNames { get { throw null; } set { } }
        public System.DayOfWeek FirstDayOfWeek { get { throw null; } set { } }
        public string FullDateTimePattern { get { throw null; } set { } }
        public static System.Globalization.DateTimeFormatInfo InvariantInfo { get { throw null; } }
        public bool IsReadOnly { get { throw null; } }
        public string LongDatePattern { get { throw null; } set { } }
        public string LongTimePattern { get { throw null; } set { } }
        public string MonthDayPattern { get { throw null; } set { } }
        public string[] MonthGenitiveNames { get { throw null; } set { } }
        public string[] MonthNames { get { throw null; } set { } }
        public string NativeCalendarName { get { throw null; } }
        public string PMDesignator { get { throw null; } set { } }
        public string RFC1123Pattern { get { throw null; } }
        public string ShortDatePattern { get { throw null; } set { } }
        public string[] ShortestDayNames { get { throw null; } set { } }
        public string ShortTimePattern { get { throw null; } set { } }
        public string SortableDateTimePattern { get { throw null; } }
        public string TimeSeparator { get { throw null; } set { } }
        public string UniversalSortableDateTimePattern { get { throw null; } }
        public string YearMonthPattern { get { throw null; } set { } }
        public object Clone() { throw null; }
        public string GetAbbreviatedDayName(System.DayOfWeek dayofweek) { throw null; }
        public string GetAbbreviatedEraName(int era) { throw null; }
        public string GetAbbreviatedMonthName(int month) { throw null; }
        public string[] GetAllDateTimePatterns() { throw null; }
        public string[] GetAllDateTimePatterns(char format) { throw null; }
        public string GetDayName(System.DayOfWeek dayofweek) { throw null; }
        public int GetEra(string eraName) { throw null; }
        public string GetEraName(int era) { throw null; }
        public object GetFormat(System.Type formatType) { throw null; }
        public static System.Globalization.DateTimeFormatInfo GetInstance(System.IFormatProvider provider) { throw null; }
        public string GetMonthName(int month) { throw null; }
        public string GetShortestDayName(System.DayOfWeek dayOfWeek) { throw null; }
        public static System.Globalization.DateTimeFormatInfo ReadOnly(System.Globalization.DateTimeFormatInfo dtfi) { throw null; }
        public void SetAllDateTimePatterns(string[] patterns, char format) { }
    }
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
    public partial class DaylightTime
    {
        public DaylightTime(System.DateTime start, System.DateTime end, System.TimeSpan delta) { }
        public System.TimeSpan Delta { get { throw null; } }
        public System.DateTime End { get { throw null; } }
        public System.DateTime Start { get { throw null; } }
    }
    public enum DigitShapes
    {
        Context = 0,
        NativeNational = 2,
        None = 1,
    }
    public abstract partial class EastAsianLunisolarCalendar : System.Globalization.Calendar
    {
        internal EastAsianLunisolarCalendar() { }
        public override System.Globalization.CalendarAlgorithmType AlgorithmType { get { throw null; } }
        public override int TwoDigitYearMax { get { throw null; } set { } }
        public override System.DateTime AddMonths(System.DateTime time, int months) { throw null; }
        public override System.DateTime AddYears(System.DateTime time, int years) { throw null; }
        public int GetCelestialStem(int sexagenaryYear) { throw null; }
        public override int GetDayOfMonth(System.DateTime time) { throw null; }
        public override System.DayOfWeek GetDayOfWeek(System.DateTime time) { throw null; }
        public override int GetDayOfYear(System.DateTime time) { throw null; }
        public override int GetDaysInMonth(int year, int month, int era) { throw null; }
        public override int GetDaysInYear(int year, int era) { throw null; }
        public override int GetLeapMonth(int year, int era) { throw null; }
        public override int GetMonth(System.DateTime time) { throw null; }
        public override int GetMonthsInYear(int year, int era) { throw null; }
        public virtual int GetSexagenaryYear(System.DateTime time) { throw null; }
        public int GetTerrestrialBranch(int sexagenaryYear) { throw null; }
        public override int GetYear(System.DateTime time) { throw null; }
        public override bool IsLeapDay(int year, int month, int day, int era) { throw null; }
        public override bool IsLeapMonth(int year, int month, int era) { throw null; }
        public override bool IsLeapYear(int year, int era) { throw null; }
        public override System.DateTime ToDateTime(int year, int month, int day, int hour, int minute, int second, int millisecond, int era) { throw null; }
        public override int ToFourDigitYear(int year) { throw null; }
    }
    public partial class GregorianCalendar : System.Globalization.Calendar
    {
        public const int ADEra = 1;
        public GregorianCalendar() { }
        public GregorianCalendar(System.Globalization.GregorianCalendarTypes type) { }
        public override System.Globalization.CalendarAlgorithmType AlgorithmType { get { throw null; } }
        public virtual System.Globalization.GregorianCalendarTypes CalendarType { get { throw null; } set { } }
        public override int[] Eras { get { throw null; } }
        public override System.DateTime MaxSupportedDateTime { get { throw null; } }
        public override System.DateTime MinSupportedDateTime { get { throw null; } }
        public override int TwoDigitYearMax { get { throw null; } set { } }
        public override System.DateTime AddMonths(System.DateTime time, int months) { throw null; }
        public override System.DateTime AddYears(System.DateTime time, int years) { throw null; }
        public override int GetDayOfMonth(System.DateTime time) { throw null; }
        public override System.DayOfWeek GetDayOfWeek(System.DateTime time) { throw null; }
        public override int GetDayOfYear(System.DateTime time) { throw null; }
        public override int GetDaysInMonth(int year, int month, int era) { throw null; }
        public override int GetDaysInYear(int year, int era) { throw null; }
        public override int GetEra(System.DateTime time) { throw null; }
        public override int GetLeapMonth(int year, int era) { throw null; }
        public override int GetMonth(System.DateTime time) { throw null; }
        public override int GetMonthsInYear(int year, int era) { throw null; }
        public override int GetYear(System.DateTime time) { throw null; }
        public override bool IsLeapDay(int year, int month, int day, int era) { throw null; }
        public override bool IsLeapMonth(int year, int month, int era) { throw null; }
        public override bool IsLeapYear(int year, int era) { throw null; }
        public override System.DateTime ToDateTime(int year, int month, int day, int hour, int minute, int second, int millisecond, int era) { throw null; }
        public override int ToFourDigitYear(int year) { throw null; }
    }
    public enum GregorianCalendarTypes
    {
        Arabic = 10,
        Localized = 1,
        MiddleEastFrench = 9,
        TransliteratedEnglish = 11,
        TransliteratedFrench = 12,
        USEnglish = 2,
    }
    public partial class HebrewCalendar : System.Globalization.Calendar
    {
        public static readonly int HebrewEra;
        public HebrewCalendar() { }
        public override System.Globalization.CalendarAlgorithmType AlgorithmType { get { throw null; } }
        public override int[] Eras { get { throw null; } }
        public override System.DateTime MaxSupportedDateTime { get { throw null; } }
        public override System.DateTime MinSupportedDateTime { get { throw null; } }
        public override int TwoDigitYearMax { get { throw null; } set { } }
        public override System.DateTime AddMonths(System.DateTime time, int months) { throw null; }
        public override System.DateTime AddYears(System.DateTime time, int years) { throw null; }
        public override int GetDayOfMonth(System.DateTime time) { throw null; }
        public override System.DayOfWeek GetDayOfWeek(System.DateTime time) { throw null; }
        public override int GetDayOfYear(System.DateTime time) { throw null; }
        public override int GetDaysInMonth(int year, int month, int era) { throw null; }
        public override int GetDaysInYear(int year, int era) { throw null; }
        public override int GetEra(System.DateTime time) { throw null; }
        public override int GetLeapMonth(int year, int era) { throw null; }
        public override int GetMonth(System.DateTime time) { throw null; }
        public override int GetMonthsInYear(int year, int era) { throw null; }
        public override int GetYear(System.DateTime time) { throw null; }
        public override bool IsLeapDay(int year, int month, int day, int era) { throw null; }
        public override bool IsLeapMonth(int year, int month, int era) { throw null; }
        public override bool IsLeapYear(int year, int era) { throw null; }
        public override System.DateTime ToDateTime(int year, int month, int day, int hour, int minute, int second, int millisecond, int era) { throw null; }
        public override int ToFourDigitYear(int year) { throw null; }
    }
    public partial class HijriCalendar : System.Globalization.Calendar
    {
        public static readonly int HijriEra;
        public HijriCalendar() { }
        public override System.Globalization.CalendarAlgorithmType AlgorithmType { get { throw null; } }
        protected override int DaysInYearBeforeMinSupportedYear { get { throw null; } }
        public override int[] Eras { get { throw null; } }
        public int HijriAdjustment { get { throw null; } set { } }
        public override System.DateTime MaxSupportedDateTime { get { throw null; } }
        public override System.DateTime MinSupportedDateTime { get { throw null; } }
        public override int TwoDigitYearMax { get { throw null; } set { } }
        public override System.DateTime AddMonths(System.DateTime time, int months) { throw null; }
        public override System.DateTime AddYears(System.DateTime time, int years) { throw null; }
        public override int GetDayOfMonth(System.DateTime time) { throw null; }
        public override System.DayOfWeek GetDayOfWeek(System.DateTime time) { throw null; }
        public override int GetDayOfYear(System.DateTime time) { throw null; }
        public override int GetDaysInMonth(int year, int month, int era) { throw null; }
        public override int GetDaysInYear(int year, int era) { throw null; }
        public override int GetEra(System.DateTime time) { throw null; }
        public override int GetLeapMonth(int year, int era) { throw null; }
        public override int GetMonth(System.DateTime time) { throw null; }
        public override int GetMonthsInYear(int year, int era) { throw null; }
        public override int GetYear(System.DateTime time) { throw null; }
        public override bool IsLeapDay(int year, int month, int day, int era) { throw null; }
        public override bool IsLeapMonth(int year, int month, int era) { throw null; }
        public override bool IsLeapYear(int year, int era) { throw null; }
        public override System.DateTime ToDateTime(int year, int month, int day, int hour, int minute, int second, int millisecond, int era) { throw null; }
        public override int ToFourDigitYear(int year) { throw null; }
    }
    public sealed partial class IdnMapping
    {
        public IdnMapping() { }
        public bool AllowUnassigned { get { throw null; } set { } }
        public bool UseStd3AsciiRules { get { throw null; } set { } }
        public override bool Equals(object obj) { throw null; }
        public string GetAscii(string unicode) { throw null; }
        public string GetAscii(string unicode, int index) { throw null; }
        public string GetAscii(string unicode, int index, int count) { throw null; }
        public override int GetHashCode() { throw null; }
        public string GetUnicode(string ascii) { throw null; }
        public string GetUnicode(string ascii, int index) { throw null; }
        public string GetUnicode(string ascii, int index, int count) { throw null; }
    }
    public partial class JapaneseCalendar : System.Globalization.Calendar
    {
        public JapaneseCalendar() { }
        public override System.Globalization.CalendarAlgorithmType AlgorithmType { get { throw null; } }
        public override int[] Eras { get { throw null; } }
        public override System.DateTime MaxSupportedDateTime { get { throw null; } }
        public override System.DateTime MinSupportedDateTime { get { throw null; } }
        public override int TwoDigitYearMax { get { throw null; } set { } }
        public override System.DateTime AddMonths(System.DateTime time, int months) { throw null; }
        public override System.DateTime AddYears(System.DateTime time, int years) { throw null; }
        public override int GetDayOfMonth(System.DateTime time) { throw null; }
        public override System.DayOfWeek GetDayOfWeek(System.DateTime time) { throw null; }
        public override int GetDayOfYear(System.DateTime time) { throw null; }
        public override int GetDaysInMonth(int year, int month, int era) { throw null; }
        public override int GetDaysInYear(int year, int era) { throw null; }
        public override int GetEra(System.DateTime time) { throw null; }
        public override int GetLeapMonth(int year, int era) { throw null; }
        public override int GetMonth(System.DateTime time) { throw null; }
        public override int GetMonthsInYear(int year, int era) { throw null; }
        public override int GetWeekOfYear(System.DateTime time, System.Globalization.CalendarWeekRule rule, System.DayOfWeek firstDayOfWeek) { throw null; }
        public override int GetYear(System.DateTime time) { throw null; }
        public override bool IsLeapDay(int year, int month, int day, int era) { throw null; }
        public override bool IsLeapMonth(int year, int month, int era) { throw null; }
        public override bool IsLeapYear(int year, int era) { throw null; }
        public override System.DateTime ToDateTime(int year, int month, int day, int hour, int minute, int second, int millisecond, int era) { throw null; }
        public override int ToFourDigitYear(int year) { throw null; }
    }
    public partial class JapaneseLunisolarCalendar : System.Globalization.EastAsianLunisolarCalendar
    {
        public const int JapaneseEra = 1;
        public JapaneseLunisolarCalendar() { }
        protected override int DaysInYearBeforeMinSupportedYear { get { throw null; } }
        public override int[] Eras { get { throw null; } }
        public override System.DateTime MaxSupportedDateTime { get { throw null; } }
        public override System.DateTime MinSupportedDateTime { get { throw null; } }
        public override int GetEra(System.DateTime time) { throw null; }
    }
    public partial class JulianCalendar : System.Globalization.Calendar
    {
        public static readonly int JulianEra;
        public JulianCalendar() { }
        public override System.Globalization.CalendarAlgorithmType AlgorithmType { get { throw null; } }
        public override int[] Eras { get { throw null; } }
        public override System.DateTime MaxSupportedDateTime { get { throw null; } }
        public override System.DateTime MinSupportedDateTime { get { throw null; } }
        public override int TwoDigitYearMax { get { throw null; } set { } }
        public override System.DateTime AddMonths(System.DateTime time, int months) { throw null; }
        public override System.DateTime AddYears(System.DateTime time, int years) { throw null; }
        public override int GetDayOfMonth(System.DateTime time) { throw null; }
        public override System.DayOfWeek GetDayOfWeek(System.DateTime time) { throw null; }
        public override int GetDayOfYear(System.DateTime time) { throw null; }
        public override int GetDaysInMonth(int year, int month, int era) { throw null; }
        public override int GetDaysInYear(int year, int era) { throw null; }
        public override int GetEra(System.DateTime time) { throw null; }
        public override int GetLeapMonth(int year, int era) { throw null; }
        public override int GetMonth(System.DateTime time) { throw null; }
        public override int GetMonthsInYear(int year, int era) { throw null; }
        public override int GetYear(System.DateTime time) { throw null; }
        public override bool IsLeapDay(int year, int month, int day, int era) { throw null; }
        public override bool IsLeapMonth(int year, int month, int era) { throw null; }
        public override bool IsLeapYear(int year, int era) { throw null; }
        public override System.DateTime ToDateTime(int year, int month, int day, int hour, int minute, int second, int millisecond, int era) { throw null; }
        public override int ToFourDigitYear(int year) { throw null; }
    }
    public partial class KoreanCalendar : System.Globalization.Calendar
    {
        public const int KoreanEra = 1;
        public KoreanCalendar() { }
        public override System.Globalization.CalendarAlgorithmType AlgorithmType { get { throw null; } }
        public override int[] Eras { get { throw null; } }
        public override System.DateTime MaxSupportedDateTime { get { throw null; } }
        public override System.DateTime MinSupportedDateTime { get { throw null; } }
        public override int TwoDigitYearMax { get { throw null; } set { } }
        public override System.DateTime AddMonths(System.DateTime time, int months) { throw null; }
        public override System.DateTime AddYears(System.DateTime time, int years) { throw null; }
        public override int GetDayOfMonth(System.DateTime time) { throw null; }
        public override System.DayOfWeek GetDayOfWeek(System.DateTime time) { throw null; }
        public override int GetDayOfYear(System.DateTime time) { throw null; }
        public override int GetDaysInMonth(int year, int month, int era) { throw null; }
        public override int GetDaysInYear(int year, int era) { throw null; }
        public override int GetEra(System.DateTime time) { throw null; }
        public override int GetLeapMonth(int year, int era) { throw null; }
        public override int GetMonth(System.DateTime time) { throw null; }
        public override int GetMonthsInYear(int year, int era) { throw null; }
        public override int GetWeekOfYear(System.DateTime time, System.Globalization.CalendarWeekRule rule, System.DayOfWeek firstDayOfWeek) { throw null; }
        public override int GetYear(System.DateTime time) { throw null; }
        public override bool IsLeapDay(int year, int month, int day, int era) { throw null; }
        public override bool IsLeapMonth(int year, int month, int era) { throw null; }
        public override bool IsLeapYear(int year, int era) { throw null; }
        public override System.DateTime ToDateTime(int year, int month, int day, int hour, int minute, int second, int millisecond, int era) { throw null; }
        public override int ToFourDigitYear(int year) { throw null; }
    }
    public partial class KoreanLunisolarCalendar : System.Globalization.EastAsianLunisolarCalendar
    {
        public const int GregorianEra = 1;
        public KoreanLunisolarCalendar() { }
        protected override int DaysInYearBeforeMinSupportedYear { get { throw null; } }
        public override int[] Eras { get { throw null; } }
        public override System.DateTime MaxSupportedDateTime { get { throw null; } }
        public override System.DateTime MinSupportedDateTime { get { throw null; } }
        public override int GetEra(System.DateTime time) { throw null; }
    }
    public sealed partial class NumberFormatInfo : System.ICloneable, System.IFormatProvider
    {
        public NumberFormatInfo() { }
        public int CurrencyDecimalDigits { get { throw null; } set { } }
        public string CurrencyDecimalSeparator { get { throw null; } set { } }
        public string CurrencyGroupSeparator { get { throw null; } set { } }
        public int[] CurrencyGroupSizes { get { throw null; } set { } }
        public int CurrencyNegativePattern { get { throw null; } set { } }
        public int CurrencyPositivePattern { get { throw null; } set { } }
        public string CurrencySymbol { get { throw null; } set { } }
        public static System.Globalization.NumberFormatInfo CurrentInfo { get { throw null; } }
        public System.Globalization.DigitShapes DigitSubstitution { get { throw null; } set { } }
        public static System.Globalization.NumberFormatInfo InvariantInfo { get { throw null; } }
        public bool IsReadOnly { get { throw null; } }
        public string NaNSymbol { get { throw null; } set { } }
        public string[] NativeDigits { get { throw null; } set { } }
        public string NegativeInfinitySymbol { get { throw null; } set { } }
        public string NegativeSign { get { throw null; } set { } }
        public int NumberDecimalDigits { get { throw null; } set { } }
        public string NumberDecimalSeparator { get { throw null; } set { } }
        public string NumberGroupSeparator { get { throw null; } set { } }
        public int[] NumberGroupSizes { get { throw null; } set { } }
        public int NumberNegativePattern { get { throw null; } set { } }
        public int PercentDecimalDigits { get { throw null; } set { } }
        public string PercentDecimalSeparator { get { throw null; } set { } }
        public string PercentGroupSeparator { get { throw null; } set { } }
        public int[] PercentGroupSizes { get { throw null; } set { } }
        public int PercentNegativePattern { get { throw null; } set { } }
        public int PercentPositivePattern { get { throw null; } set { } }
        public string PercentSymbol { get { throw null; } set { } }
        public string PerMilleSymbol { get { throw null; } set { } }
        public string PositiveInfinitySymbol { get { throw null; } set { } }
        public string PositiveSign { get { throw null; } set { } }
        public object Clone() { throw null; }
        public object GetFormat(System.Type formatType) { throw null; }
        public static System.Globalization.NumberFormatInfo GetInstance(System.IFormatProvider formatProvider) { throw null; }
        public static System.Globalization.NumberFormatInfo ReadOnly(System.Globalization.NumberFormatInfo nfi) { throw null; }
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
    public partial class PersianCalendar : System.Globalization.Calendar
    {
        public static readonly int PersianEra;
        public PersianCalendar() { }
        public override System.Globalization.CalendarAlgorithmType AlgorithmType { get { throw null; } }
        public override int[] Eras { get { throw null; } }
        public override System.DateTime MaxSupportedDateTime { get { throw null; } }
        public override System.DateTime MinSupportedDateTime { get { throw null; } }
        public override int TwoDigitYearMax { get { throw null; } set { } }
        public override System.DateTime AddMonths(System.DateTime time, int months) { throw null; }
        public override System.DateTime AddYears(System.DateTime time, int years) { throw null; }
        public override int GetDayOfMonth(System.DateTime time) { throw null; }
        public override System.DayOfWeek GetDayOfWeek(System.DateTime time) { throw null; }
        public override int GetDayOfYear(System.DateTime time) { throw null; }
        public override int GetDaysInMonth(int year, int month, int era) { throw null; }
        public override int GetDaysInYear(int year, int era) { throw null; }
        public override int GetEra(System.DateTime time) { throw null; }
        public override int GetLeapMonth(int year, int era) { throw null; }
        public override int GetMonth(System.DateTime time) { throw null; }
        public override int GetMonthsInYear(int year, int era) { throw null; }
        public override int GetYear(System.DateTime time) { throw null; }
        public override bool IsLeapDay(int year, int month, int day, int era) { throw null; }
        public override bool IsLeapMonth(int year, int month, int era) { throw null; }
        public override bool IsLeapYear(int year, int era) { throw null; }
        public override System.DateTime ToDateTime(int year, int month, int day, int hour, int minute, int second, int millisecond, int era) { throw null; }
        public override int ToFourDigitYear(int year) { throw null; }
    }
    public partial class RegionInfo
    {
        public RegionInfo(int culture) { }
        public RegionInfo(string name) { }
        public virtual string CurrencyEnglishName { get { throw null; } }
        public virtual string CurrencyNativeName { get { throw null; } }
        public virtual string CurrencySymbol { get { throw null; } }
        public static System.Globalization.RegionInfo CurrentRegion { get { throw null; } }
        public virtual string DisplayName { get { throw null; } }
        public virtual string EnglishName { get { throw null; } }
        public virtual int GeoId { get { throw null; } }
        public virtual bool IsMetric { get { throw null; } }
        public virtual string ISOCurrencySymbol { get { throw null; } }
        public virtual string Name { get { throw null; } }
        public virtual string NativeName { get { throw null; } }
        public virtual string ThreeLetterISORegionName { get { throw null; } }
        public virtual string ThreeLetterWindowsRegionName { get { throw null; } }
        public virtual string TwoLetterISORegionName { get { throw null; } }
        public override bool Equals(object value) { throw null; }
        public override int GetHashCode() { throw null; }
        public override string ToString() { throw null; }
    }
    public partial class SortKey
    {
        internal SortKey() { }
        public virtual byte[] KeyData { get { throw null; } }
        public virtual string OriginalString { get { throw null; } }
        public static int Compare(System.Globalization.SortKey sortkey1, System.Globalization.SortKey sortkey2) { throw null; }
        public override bool Equals(object value) { throw null; }
        public override int GetHashCode() { throw null; }
        public override string ToString() { throw null; }
    }
    public sealed partial class SortVersion : System.IEquatable<System.Globalization.SortVersion>
    {
        public SortVersion(int fullVersion, System.Guid sortId) { }
        public int FullVersion { get { throw null; } }
        public System.Guid SortId { get { throw null; } }
        public bool Equals(System.Globalization.SortVersion other) { throw null; }
        public override bool Equals(object obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Globalization.SortVersion left, System.Globalization.SortVersion right) { throw null; }
        public static bool operator !=(System.Globalization.SortVersion left, System.Globalization.SortVersion right) { throw null; }
    }
    public partial class StringInfo
    {
        public StringInfo() { }
        public StringInfo(string value) { }
        public int LengthInTextElements { get { throw null; } }
        public string String { get { throw null; } set { } }
        public override bool Equals(object value) { throw null; }
        public override int GetHashCode() { throw null; }
        public static string GetNextTextElement(string str) { throw null; }
        public static string GetNextTextElement(string str, int index) { throw null; }
        public static System.Globalization.TextElementEnumerator GetTextElementEnumerator(string str) { throw null; }
        public static System.Globalization.TextElementEnumerator GetTextElementEnumerator(string str, int index) { throw null; }
        public static int[] ParseCombiningCharacters(string str) { throw null; }
        public string SubstringByTextElements(int startingTextElement) { throw null; }
        public string SubstringByTextElements(int startingTextElement, int lengthInTextElements) { throw null; }
    }
    public partial class TaiwanCalendar : System.Globalization.Calendar
    {
        public TaiwanCalendar() { }
        public override System.Globalization.CalendarAlgorithmType AlgorithmType { get { throw null; } }
        public override int[] Eras { get { throw null; } }
        public override System.DateTime MaxSupportedDateTime { get { throw null; } }
        public override System.DateTime MinSupportedDateTime { get { throw null; } }
        public override int TwoDigitYearMax { get { throw null; } set { } }
        public override System.DateTime AddMonths(System.DateTime time, int months) { throw null; }
        public override System.DateTime AddYears(System.DateTime time, int years) { throw null; }
        public override int GetDayOfMonth(System.DateTime time) { throw null; }
        public override System.DayOfWeek GetDayOfWeek(System.DateTime time) { throw null; }
        public override int GetDayOfYear(System.DateTime time) { throw null; }
        public override int GetDaysInMonth(int year, int month, int era) { throw null; }
        public override int GetDaysInYear(int year, int era) { throw null; }
        public override int GetEra(System.DateTime time) { throw null; }
        public override int GetLeapMonth(int year, int era) { throw null; }
        public override int GetMonth(System.DateTime time) { throw null; }
        public override int GetMonthsInYear(int year, int era) { throw null; }
        public override int GetWeekOfYear(System.DateTime time, System.Globalization.CalendarWeekRule rule, System.DayOfWeek firstDayOfWeek) { throw null; }
        public override int GetYear(System.DateTime time) { throw null; }
        public override bool IsLeapDay(int year, int month, int day, int era) { throw null; }
        public override bool IsLeapMonth(int year, int month, int era) { throw null; }
        public override bool IsLeapYear(int year, int era) { throw null; }
        public override System.DateTime ToDateTime(int year, int month, int day, int hour, int minute, int second, int millisecond, int era) { throw null; }
        public override int ToFourDigitYear(int year) { throw null; }
    }
    public partial class TaiwanLunisolarCalendar : System.Globalization.EastAsianLunisolarCalendar
    {
        public TaiwanLunisolarCalendar() { }
        protected override int DaysInYearBeforeMinSupportedYear { get { throw null; } }
        public override int[] Eras { get { throw null; } }
        public override System.DateTime MaxSupportedDateTime { get { throw null; } }
        public override System.DateTime MinSupportedDateTime { get { throw null; } }
        public override int GetEra(System.DateTime time) { throw null; }
    }
    public partial class TextElementEnumerator : System.Collections.IEnumerator
    {
        internal TextElementEnumerator() { }
        public object Current { get { throw null; } }
        public int ElementIndex { get { throw null; } }
        public string GetTextElement() { throw null; }
        public bool MoveNext() { throw null; }
        public void Reset() { }
    }
    public partial class TextInfo : System.ICloneable, System.Runtime.Serialization.IDeserializationCallback
    {
        internal TextInfo() { }
        public virtual int ANSICodePage { get { throw null; } }
        public string CultureName { get { throw null; } }
        public virtual int EBCDICCodePage { get { throw null; } }
        public bool IsReadOnly { get { throw null; } }
        public bool IsRightToLeft { get { throw null; } }
        public int LCID { get { throw null; } }
        public virtual string ListSeparator { get { throw null; } set { } }
        public virtual int MacCodePage { get { throw null; } }
        public virtual int OEMCodePage { get { throw null; } }
        public virtual object Clone() { throw null; }
        public override bool Equals(object obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public static System.Globalization.TextInfo ReadOnly(System.Globalization.TextInfo textInfo) { throw null; }
        void System.Runtime.Serialization.IDeserializationCallback.OnDeserialization(object sender) { }
        public virtual char ToLower(char c) { throw null; }
        public virtual string ToLower(string str) { throw null; }
        public override string ToString() { throw null; }
        public string ToTitleCase(string str) { throw null; }
        public virtual char ToUpper(char c) { throw null; }
        public virtual string ToUpper(string str) { throw null; }
    }
    public partial class ThaiBuddhistCalendar : System.Globalization.Calendar
    {
        public const int ThaiBuddhistEra = 1;
        public ThaiBuddhistCalendar() { }
        public override System.Globalization.CalendarAlgorithmType AlgorithmType { get { throw null; } }
        public override int[] Eras { get { throw null; } }
        public override System.DateTime MaxSupportedDateTime { get { throw null; } }
        public override System.DateTime MinSupportedDateTime { get { throw null; } }
        public override int TwoDigitYearMax { get { throw null; } set { } }
        public override System.DateTime AddMonths(System.DateTime time, int months) { throw null; }
        public override System.DateTime AddYears(System.DateTime time, int years) { throw null; }
        public override int GetDayOfMonth(System.DateTime time) { throw null; }
        public override System.DayOfWeek GetDayOfWeek(System.DateTime time) { throw null; }
        public override int GetDayOfYear(System.DateTime time) { throw null; }
        public override int GetDaysInMonth(int year, int month, int era) { throw null; }
        public override int GetDaysInYear(int year, int era) { throw null; }
        public override int GetEra(System.DateTime time) { throw null; }
        public override int GetLeapMonth(int year, int era) { throw null; }
        public override int GetMonth(System.DateTime time) { throw null; }
        public override int GetMonthsInYear(int year, int era) { throw null; }
        public override int GetWeekOfYear(System.DateTime time, System.Globalization.CalendarWeekRule rule, System.DayOfWeek firstDayOfWeek) { throw null; }
        public override int GetYear(System.DateTime time) { throw null; }
        public override bool IsLeapDay(int year, int month, int day, int era) { throw null; }
        public override bool IsLeapMonth(int year, int month, int era) { throw null; }
        public override bool IsLeapYear(int year, int era) { throw null; }
        public override System.DateTime ToDateTime(int year, int month, int day, int hour, int minute, int second, int millisecond, int era) { throw null; }
        public override int ToFourDigitYear(int year) { throw null; }
    }
    [System.FlagsAttribute]
    public enum TimeSpanStyles
    {
        AssumeNegative = 1,
        None = 0,
    }
    public partial class UmAlQuraCalendar : System.Globalization.Calendar
    {
        public const int UmAlQuraEra = 1;
        public UmAlQuraCalendar() { }
        public override System.Globalization.CalendarAlgorithmType AlgorithmType { get { throw null; } }
        protected override int DaysInYearBeforeMinSupportedYear { get { throw null; } }
        public override int[] Eras { get { throw null; } }
        public override System.DateTime MaxSupportedDateTime { get { throw null; } }
        public override System.DateTime MinSupportedDateTime { get { throw null; } }
        public override int TwoDigitYearMax { get { throw null; } set { } }
        public override System.DateTime AddMonths(System.DateTime time, int months) { throw null; }
        public override System.DateTime AddYears(System.DateTime time, int years) { throw null; }
        public override int GetDayOfMonth(System.DateTime time) { throw null; }
        public override System.DayOfWeek GetDayOfWeek(System.DateTime time) { throw null; }
        public override int GetDayOfYear(System.DateTime time) { throw null; }
        public override int GetDaysInMonth(int year, int month, int era) { throw null; }
        public override int GetDaysInYear(int year, int era) { throw null; }
        public override int GetEra(System.DateTime time) { throw null; }
        public override int GetLeapMonth(int year, int era) { throw null; }
        public override int GetMonth(System.DateTime time) { throw null; }
        public override int GetMonthsInYear(int year, int era) { throw null; }
        public override int GetYear(System.DateTime time) { throw null; }
        public override bool IsLeapDay(int year, int month, int day, int era) { throw null; }
        public override bool IsLeapMonth(int year, int month, int era) { throw null; }
        public override bool IsLeapYear(int year, int era) { throw null; }
        public override System.DateTime ToDateTime(int year, int month, int day, int hour, int minute, int second, int millisecond, int era) { throw null; }
        public override int ToFourDigitYear(int year) { throw null; }
    }
    public enum UnicodeCategory
    {
        ClosePunctuation = 21,
        ConnectorPunctuation = 18,
        Control = 14,
        CurrencySymbol = 26,
        DashPunctuation = 19,
        DecimalDigitNumber = 8,
        EnclosingMark = 7,
        FinalQuotePunctuation = 23,
        Format = 15,
        InitialQuotePunctuation = 22,
        LetterNumber = 9,
        LineSeparator = 12,
        LowercaseLetter = 1,
        MathSymbol = 25,
        ModifierLetter = 3,
        ModifierSymbol = 27,
        NonSpacingMark = 5,
        OpenPunctuation = 20,
        OtherLetter = 4,
        OtherNotAssigned = 29,
        OtherNumber = 10,
        OtherPunctuation = 24,
        OtherSymbol = 28,
        ParagraphSeparator = 13,
        PrivateUse = 17,
        SpaceSeparator = 11,
        SpacingCombiningMark = 6,
        Surrogate = 16,
        TitlecaseLetter = 2,
        UppercaseLetter = 0,
    }
}
namespace System.IO
{
    public partial class DirectoryNotFoundException : System.IO.IOException
    {
        public DirectoryNotFoundException() { }
        protected DirectoryNotFoundException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public DirectoryNotFoundException(string message) { }
        public DirectoryNotFoundException(string message, System.Exception innerException) { }
    }
    [System.FlagsAttribute]
    public enum FileAccess
    {
        Read = 1,
        ReadWrite = 3,
        Write = 2,
    }
    [System.FlagsAttribute]
    public enum FileAttributes
    {
        Archive = 32,
        Compressed = 2048,
        Device = 64,
        Directory = 16,
        Encrypted = 16384,
        Hidden = 2,
        IntegrityStream = 32768,
        Normal = 128,
        NoScrubData = 131072,
        NotContentIndexed = 8192,
        Offline = 4096,
        ReadOnly = 1,
        ReparsePoint = 1024,
        SparseFile = 512,
        System = 4,
        Temporary = 256,
    }
    public partial class FileLoadException : System.IO.IOException
    {
        public FileLoadException() { }
        protected FileLoadException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public FileLoadException(string message) { }
        public FileLoadException(string message, System.Exception inner) { }
        public FileLoadException(string message, string fileName) { }
        public FileLoadException(string message, string fileName, System.Exception inner) { }
        public string FileName { get { throw null; } }
        public string FusionLog { get { throw null; } }
        public override string Message { get { throw null; } }
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public override string ToString() { throw null; }
    }
    public enum FileMode
    {
        Append = 6,
        Create = 2,
        CreateNew = 1,
        Open = 3,
        OpenOrCreate = 4,
        Truncate = 5,
    }
    public partial class FileNotFoundException : System.IO.IOException
    {
        public FileNotFoundException() { }
        protected FileNotFoundException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public FileNotFoundException(string message) { }
        public FileNotFoundException(string message, System.Exception innerException) { }
        public FileNotFoundException(string message, string fileName) { }
        public FileNotFoundException(string message, string fileName, System.Exception innerException) { }
        public string FileName { get { throw null; } }
        public string FusionLog { get { throw null; } }
        public override string Message { get { throw null; } }
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public override string ToString() { throw null; }
    }
    [System.FlagsAttribute]
    public enum FileOptions
    {
        Asynchronous = 1073741824,
        DeleteOnClose = 67108864,
        Encrypted = 16384,
        None = 0,
        RandomAccess = 268435456,
        SequentialScan = 134217728,
        WriteThrough = -2147483648,
    }
    [System.FlagsAttribute]
    public enum FileShare
    {
        Delete = 4,
        Inheritable = 16,
        None = 0,
        Read = 1,
        ReadWrite = 3,
        Write = 2,
    }
    public partial class FileStream : System.IO.Stream
    {
        public FileStream(Microsoft.Win32.SafeHandles.SafeFileHandle handle, System.IO.FileAccess access) { }
        public FileStream(Microsoft.Win32.SafeHandles.SafeFileHandle handle, System.IO.FileAccess access, int bufferSize) { }
        public FileStream(Microsoft.Win32.SafeHandles.SafeFileHandle handle, System.IO.FileAccess access, int bufferSize, bool isAsync) { }
        [System.ObsoleteAttribute("This constructor has been deprecated.  Please use new FileStream(SafeFileHandle handle, FileAccess access) instead.  http://go.microsoft.com/fwlink/?linkid=14202")]
        public FileStream(System.IntPtr handle, System.IO.FileAccess access) { }
        [System.ObsoleteAttribute("This constructor has been deprecated.  Please use new FileStream(SafeFileHandle handle, FileAccess access) instead, and optionally make a new SafeFileHandle with ownsHandle=false if needed.  http://go.microsoft.com/fwlink/?linkid=14202")]
        public FileStream(System.IntPtr handle, System.IO.FileAccess access, bool ownsHandle) { }
        [System.ObsoleteAttribute("This constructor has been deprecated.  Please use new FileStream(SafeFileHandle handle, FileAccess access, int bufferSize) instead, and optionally make a new SafeFileHandle with ownsHandle=false if needed.  http://go.microsoft.com/fwlink/?linkid=14202")]
        public FileStream(System.IntPtr handle, System.IO.FileAccess access, bool ownsHandle, int bufferSize) { }
        [System.ObsoleteAttribute("This constructor has been deprecated.  Please use new FileStream(SafeFileHandle handle, FileAccess access, int bufferSize, bool isAsync) instead, and optionally make a new SafeFileHandle with ownsHandle=false if needed.  http://go.microsoft.com/fwlink/?linkid=14202")]
        public FileStream(System.IntPtr handle, System.IO.FileAccess access, bool ownsHandle, int bufferSize, bool isAsync) { }
        public FileStream(string path, System.IO.FileMode mode) { }
        public FileStream(string path, System.IO.FileMode mode, System.IO.FileAccess access) { }
        public FileStream(string path, System.IO.FileMode mode, System.IO.FileAccess access, System.IO.FileShare share) { }
        public FileStream(string path, System.IO.FileMode mode, System.IO.FileAccess access, System.IO.FileShare share, int bufferSize) { }
        public FileStream(string path, System.IO.FileMode mode, System.IO.FileAccess access, System.IO.FileShare share, int bufferSize, bool useAsync) { }
        public FileStream(string path, System.IO.FileMode mode, System.IO.FileAccess access, System.IO.FileShare share, int bufferSize, System.IO.FileOptions options) { }
        public override bool CanRead { get { throw null; } }
        public override bool CanSeek { get { throw null; } }
        public override bool CanWrite { get { throw null; } }
        [System.ObsoleteAttribute("This property has been deprecated.  Please use FileStream's SafeFileHandle property instead.  http://go.microsoft.com/fwlink/?linkid=14202")]
        public virtual System.IntPtr Handle { get { throw null; } }
        public virtual bool IsAsync { get { throw null; } }
        public override long Length { get { throw null; } }
        public virtual string Name { get { throw null; } }
        public override long Position { get { throw null; } set { } }
        public virtual Microsoft.Win32.SafeHandles.SafeFileHandle SafeFileHandle { get { throw null; } }
        public override System.IAsyncResult BeginRead(byte[] array, int offset, int numBytes, System.AsyncCallback callback, object state) { throw null; }
        public override System.IAsyncResult BeginWrite(byte[] array, int offset, int numBytes, System.AsyncCallback callback, object state) { throw null; }
        protected override void Dispose(bool disposing) { }
        public override int EndRead(System.IAsyncResult asyncResult) { throw null; }
        public override void EndWrite(System.IAsyncResult asyncResult) { }
        ~FileStream() { }
        public override void Flush() { }
        public virtual void Flush(bool flushToDisk) { }
        public override System.Threading.Tasks.Task FlushAsync(System.Threading.CancellationToken cancellationToken) { throw null; }
        public virtual void Lock(long position, long length) { }
        public override int Read(byte[] array, int offset, int count) { throw null; }
        public override System.Threading.Tasks.Task<int> ReadAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken) { throw null; }
        public override int ReadByte() { throw null; }
        public override long Seek(long offset, System.IO.SeekOrigin origin) { throw null; }
        public override void SetLength(long value) { }
        public virtual void Unlock(long position, long length) { }
        public override void Write(byte[] array, int offset, int count) { }
        public override System.Threading.Tasks.Task WriteAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken) { throw null; }
        public override void WriteByte(byte value) { }
    }
    public enum HandleInheritability
    {
        Inheritable = 1,
        None = 0,
    }
    public partial class IOException : System.SystemException
    {
        public IOException() { }
        protected IOException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public IOException(string message) { }
        public IOException(string message, System.Exception innerException) { }
        public IOException(string message, int hresult) { }
    }
    public partial class PathTooLongException : System.IO.IOException
    {
        public PathTooLongException() { }
        protected PathTooLongException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public PathTooLongException(string message) { }
        public PathTooLongException(string message, System.Exception innerException) { }
    }
    public enum SeekOrigin
    {
        Begin = 0,
        Current = 1,
        End = 2,
    }
    public abstract partial class Stream : System.MarshalByRefObject, System.IDisposable
    {
        public static readonly System.IO.Stream Null;
        protected Stream() { }
        public abstract bool CanRead { get; }
        public abstract bool CanSeek { get; }
        public virtual bool CanTimeout { get { throw null; } }
        public abstract bool CanWrite { get; }
        public abstract long Length { get; }
        public abstract long Position { get; set; }
        public virtual int ReadTimeout { get { throw null; } set { } }
        public virtual int WriteTimeout { get { throw null; } set { } }
        public virtual System.IAsyncResult BeginRead(byte[] buffer, int offset, int count, System.AsyncCallback callback, object state) { throw null; }
        public virtual System.IAsyncResult BeginWrite(byte[] buffer, int offset, int count, System.AsyncCallback callback, object state) { throw null; }
        public virtual void Close() { }
        public void CopyTo(System.IO.Stream destination) { }
        public virtual void CopyTo(System.IO.Stream destination, int bufferSize) { }
        public System.Threading.Tasks.Task CopyToAsync(System.IO.Stream destination) { throw null; }
        public System.Threading.Tasks.Task CopyToAsync(System.IO.Stream destination, int bufferSize) { throw null; }
        public virtual System.Threading.Tasks.Task CopyToAsync(System.IO.Stream destination, int bufferSize, System.Threading.CancellationToken cancellationToken) { throw null; }
        public System.Threading.Tasks.Task CopyToAsync(System.IO.Stream destination, System.Threading.CancellationToken cancellationToken) { throw null; }
        [System.ObsoleteAttribute("CreateWaitHandle will be removed eventually.  Please use \"new ManualResetEvent(false)\" instead.")]
        protected virtual System.Threading.WaitHandle CreateWaitHandle() { throw null; }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        public virtual int EndRead(System.IAsyncResult asyncResult) { throw null; }
        public virtual void EndWrite(System.IAsyncResult asyncResult) { }
        public abstract void Flush();
        public System.Threading.Tasks.Task FlushAsync() { throw null; }
        public virtual System.Threading.Tasks.Task FlushAsync(System.Threading.CancellationToken cancellationToken) { throw null; }
        [System.ObsoleteAttribute("Do not call or override this method.")]
        protected virtual void ObjectInvariant() { }
        public abstract int Read(byte[] buffer, int offset, int count);
        public virtual int Read(System.Span<byte> destination) { throw null; }
        public System.Threading.Tasks.Task<int> ReadAsync(byte[] buffer, int offset, int count) { throw null; }
        public virtual System.Threading.Tasks.Task<int> ReadAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken) { throw null; }
        public virtual System.Threading.Tasks.ValueTask<int> ReadAsync(System.Memory<byte> destination, System.Threading.CancellationToken cancellationToken=default(System.Threading.CancellationToken)) { throw null; }
        public virtual int ReadByte() { throw null; }
        public abstract long Seek(long offset, System.IO.SeekOrigin origin);
        public abstract void SetLength(long value);
        public static System.IO.Stream Synchronized(System.IO.Stream stream) { throw null; }
        public abstract void Write(byte[] buffer, int offset, int count);
        public virtual void Write(System.ReadOnlySpan<byte> source) { }
        public System.Threading.Tasks.Task WriteAsync(byte[] buffer, int offset, int count) { throw null; }
        public virtual System.Threading.Tasks.Task WriteAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken) { throw null; }
        public virtual System.Threading.Tasks.Task WriteAsync(System.ReadOnlyMemory<byte> source, System.Threading.CancellationToken cancellationToken=default(System.Threading.CancellationToken)) { throw null; }
        public virtual void WriteByte(byte value) { }
    }
}
namespace System.Reflection
{
    public sealed partial class AmbiguousMatchException : System.SystemException
    {
        public AmbiguousMatchException() { }
        public AmbiguousMatchException(string message) { }
        public AmbiguousMatchException(string message, System.Exception inner) { }
    }
    public abstract partial class Assembly : System.Reflection.ICustomAttributeProvider, System.Runtime.Serialization.ISerializable
    {
        protected Assembly() { }
        public virtual string CodeBase { get { throw null; } }
        public virtual System.Collections.Generic.IEnumerable<System.Reflection.CustomAttributeData> CustomAttributes { get { throw null; } }
        public virtual System.Collections.Generic.IEnumerable<System.Reflection.TypeInfo> DefinedTypes { get { throw null; } }
        public virtual System.Reflection.MethodInfo EntryPoint { get { throw null; } }
        public virtual string EscapedCodeBase { get { throw null; } }
        public virtual System.Collections.Generic.IEnumerable<System.Type> ExportedTypes { get { throw null; } }
        public virtual string FullName { get { throw null; } }
        public virtual bool GlobalAssemblyCache { get { throw null; } }
        public virtual long HostContext { get { throw null; } }
        public virtual string ImageRuntimeVersion { get { throw null; } }
        public virtual bool IsDynamic { get { throw null; } }
        public bool IsFullyTrusted { get { throw null; } }
        public virtual string Location { get { throw null; } }
        public virtual System.Reflection.Module ManifestModule { get { throw null; } }
        public virtual System.Collections.Generic.IEnumerable<System.Reflection.Module> Modules { get { throw null; } }
        public virtual bool ReflectionOnly { get { throw null; } }
        public virtual System.Security.SecurityRuleSet SecurityRuleSet { get { throw null; } }
        public virtual event System.Reflection.ModuleResolveEventHandler ModuleResolve { add { } remove { } }
        public object CreateInstance(string typeName) { throw null; }
        public object CreateInstance(string typeName, bool ignoreCase) { throw null; }
        public virtual object CreateInstance(string typeName, bool ignoreCase, System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder binder, object[] args, System.Globalization.CultureInfo culture, object[] activationAttributes) { throw null; }
        public static string CreateQualifiedName(string assemblyName, string typeName) { throw null; }
        public override bool Equals(object o) { throw null; }
        public static System.Reflection.Assembly GetAssembly(System.Type type) { throw null; }
        public static System.Reflection.Assembly GetCallingAssembly() { throw null; }
        public virtual object[] GetCustomAttributes(bool inherit) { throw null; }
        public virtual object[] GetCustomAttributes(System.Type attributeType, bool inherit) { throw null; }
        public virtual System.Collections.Generic.IList<System.Reflection.CustomAttributeData> GetCustomAttributesData() { throw null; }
        public static System.Reflection.Assembly GetEntryAssembly() { throw null; }
        public static System.Reflection.Assembly GetExecutingAssembly() { throw null; }
        public virtual System.Type[] GetExportedTypes() { throw null; }
        public virtual System.IO.FileStream GetFile(string name) { throw null; }
        public virtual System.IO.FileStream[] GetFiles() { throw null; }
        public virtual System.IO.FileStream[] GetFiles(bool getResourceModules) { throw null; }
        public virtual System.Type[] GetForwardedTypes() { throw null; }
        public override int GetHashCode() { throw null; }
        public System.Reflection.Module[] GetLoadedModules() { throw null; }
        public virtual System.Reflection.Module[] GetLoadedModules(bool getResourceModules) { throw null; }
        public virtual System.Reflection.ManifestResourceInfo GetManifestResourceInfo(string resourceName) { throw null; }
        public virtual string[] GetManifestResourceNames() { throw null; }
        public virtual System.IO.Stream GetManifestResourceStream(string name) { throw null; }
        public virtual System.IO.Stream GetManifestResourceStream(System.Type type, string name) { throw null; }
        public virtual System.Reflection.Module GetModule(string name) { throw null; }
        public System.Reflection.Module[] GetModules() { throw null; }
        public virtual System.Reflection.Module[] GetModules(bool getResourceModules) { throw null; }
        public virtual System.Reflection.AssemblyName GetName() { throw null; }
        public virtual System.Reflection.AssemblyName GetName(bool copiedName) { throw null; }
        public virtual void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public virtual System.Reflection.AssemblyName[] GetReferencedAssemblies() { throw null; }
        public virtual System.Reflection.Assembly GetSatelliteAssembly(System.Globalization.CultureInfo culture) { throw null; }
        public virtual System.Reflection.Assembly GetSatelliteAssembly(System.Globalization.CultureInfo culture, System.Version version) { throw null; }
        public virtual System.Type GetType(string name) { throw null; }
        public virtual System.Type GetType(string name, bool throwOnError) { throw null; }
        public virtual System.Type GetType(string name, bool throwOnError, bool ignoreCase) { throw null; }
        public virtual System.Type[] GetTypes() { throw null; }
        public virtual bool IsDefined(System.Type attributeType, bool inherit) { throw null; }
        public static System.Reflection.Assembly Load(byte[] rawAssembly) { throw null; }
        public static System.Reflection.Assembly Load(byte[] rawAssembly, byte[] rawSymbolStore) { throw null; }
        public static System.Reflection.Assembly Load(System.Reflection.AssemblyName assemblyRef) { throw null; }
        public static System.Reflection.Assembly Load(string assemblyString) { throw null; }
        public static System.Reflection.Assembly LoadFile(string path) { throw null; }
        public static System.Reflection.Assembly LoadFrom(string assemblyFile) { throw null; }
        public static System.Reflection.Assembly LoadFrom(string assemblyFile, byte[] hashValue, System.Configuration.Assemblies.AssemblyHashAlgorithm hashAlgorithm) { throw null; }
        public System.Reflection.Module LoadModule(string moduleName, byte[] rawModule) { throw null; }
        public virtual System.Reflection.Module LoadModule(string moduleName, byte[] rawModule, byte[] rawSymbolStore) { throw null; }
        [System.ObsoleteAttribute("This method has been deprecated. Please use Assembly.Load() instead. http://go.microsoft.com/fwlink/?linkid=14202")]
        public static System.Reflection.Assembly LoadWithPartialName(string partialName) { throw null; }
        public static bool operator ==(System.Reflection.Assembly left, System.Reflection.Assembly right) { throw null; }
        public static bool operator !=(System.Reflection.Assembly left, System.Reflection.Assembly right) { throw null; }
        public static System.Reflection.Assembly ReflectionOnlyLoad(byte[] rawAssembly) { throw null; }
        public static System.Reflection.Assembly ReflectionOnlyLoad(string assemblyString) { throw null; }
        public static System.Reflection.Assembly ReflectionOnlyLoadFrom(string assemblyFile) { throw null; }
        public override string ToString() { throw null; }
        public static System.Reflection.Assembly UnsafeLoadFrom(string assemblyFile) { throw null; }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(1), Inherited=false)]
    public sealed partial class AssemblyAlgorithmIdAttribute : System.Attribute
    {
        public AssemblyAlgorithmIdAttribute(System.Configuration.Assemblies.AssemblyHashAlgorithm algorithmId) { }
        [System.CLSCompliantAttribute(false)]
        public AssemblyAlgorithmIdAttribute(uint algorithmId) { }
        [System.CLSCompliantAttribute(false)]
        public uint AlgorithmId { get { throw null; } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(1), Inherited=false)]
    public sealed partial class AssemblyCompanyAttribute : System.Attribute
    {
        public AssemblyCompanyAttribute(string company) { }
        public string Company { get { throw null; } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(1), Inherited=false)]
    public sealed partial class AssemblyConfigurationAttribute : System.Attribute
    {
        public AssemblyConfigurationAttribute(string configuration) { }
        public string Configuration { get { throw null; } }
    }
    public enum AssemblyContentType
    {
        Default = 0,
        WindowsRuntime = 1,
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(1), Inherited=false)]
    public sealed partial class AssemblyCopyrightAttribute : System.Attribute
    {
        public AssemblyCopyrightAttribute(string copyright) { }
        public string Copyright { get { throw null; } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(1), Inherited=false)]
    public sealed partial class AssemblyCultureAttribute : System.Attribute
    {
        public AssemblyCultureAttribute(string culture) { }
        public string Culture { get { throw null; } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(1), Inherited=false)]
    public sealed partial class AssemblyDefaultAliasAttribute : System.Attribute
    {
        public AssemblyDefaultAliasAttribute(string defaultAlias) { }
        public string DefaultAlias { get { throw null; } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(1), Inherited=false)]
    public sealed partial class AssemblyDelaySignAttribute : System.Attribute
    {
        public AssemblyDelaySignAttribute(bool delaySign) { }
        public bool DelaySign { get { throw null; } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(1), Inherited=false)]
    public sealed partial class AssemblyDescriptionAttribute : System.Attribute
    {
        public AssemblyDescriptionAttribute(string description) { }
        public string Description { get { throw null; } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(1), Inherited=false)]
    public sealed partial class AssemblyFileVersionAttribute : System.Attribute
    {
        public AssemblyFileVersionAttribute(string version) { }
        public string Version { get { throw null; } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(1), Inherited=false)]
    public sealed partial class AssemblyFlagsAttribute : System.Attribute
    {
        [System.ObsoleteAttribute("This constructor has been deprecated. Please use AssemblyFlagsAttribute(AssemblyNameFlags) instead. http://go.microsoft.com/fwlink/?linkid=14202")]
        public AssemblyFlagsAttribute(int assemblyFlags) { }
        public AssemblyFlagsAttribute(System.Reflection.AssemblyNameFlags assemblyFlags) { }
        [System.CLSCompliantAttribute(false)]
        [System.ObsoleteAttribute("This constructor has been deprecated. Please use AssemblyFlagsAttribute(AssemblyNameFlags) instead. http://go.microsoft.com/fwlink/?linkid=14202")]
        public AssemblyFlagsAttribute(uint flags) { }
        public int AssemblyFlags { get { throw null; } }
        [System.CLSCompliantAttribute(false)]
        [System.ObsoleteAttribute("This property has been deprecated. Please use AssemblyFlags instead. http://go.microsoft.com/fwlink/?linkid=14202")]
        public uint Flags { get { throw null; } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(1), Inherited=false)]
    public sealed partial class AssemblyInformationalVersionAttribute : System.Attribute
    {
        public AssemblyInformationalVersionAttribute(string informationalVersion) { }
        public string InformationalVersion { get { throw null; } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(1), Inherited=false)]
    public sealed partial class AssemblyKeyFileAttribute : System.Attribute
    {
        public AssemblyKeyFileAttribute(string keyFile) { }
        public string KeyFile { get { throw null; } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(1), Inherited=false)]
    public sealed partial class AssemblyKeyNameAttribute : System.Attribute
    {
        public AssemblyKeyNameAttribute(string keyName) { }
        public string KeyName { get { throw null; } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(1), AllowMultiple=true, Inherited=false)]
    public sealed partial class AssemblyMetadataAttribute : System.Attribute
    {
        public AssemblyMetadataAttribute(string key, string value) { }
        public string Key { get { throw null; } }
        public string Value { get { throw null; } }
    }
    public sealed partial class AssemblyName : System.ICloneable, System.Runtime.Serialization.IDeserializationCallback, System.Runtime.Serialization.ISerializable
    {
        public AssemblyName() { }
        public AssemblyName(string assemblyName) { }
        public string CodeBase { get { throw null; } set { } }
        public System.Reflection.AssemblyContentType ContentType { get { throw null; } set { } }
        public System.Globalization.CultureInfo CultureInfo { get { throw null; } set { } }
        public string CultureName { get { throw null; } set { } }
        public string EscapedCodeBase { get { throw null; } }
        public System.Reflection.AssemblyNameFlags Flags { get { throw null; } set { } }
        public string FullName { get { throw null; } }
        public System.Configuration.Assemblies.AssemblyHashAlgorithm HashAlgorithm { get { throw null; } set { } }
        public System.Reflection.StrongNameKeyPair KeyPair { get { throw null; } set { } }
        public string Name { get { throw null; } set { } }
        public System.Reflection.ProcessorArchitecture ProcessorArchitecture { get { throw null; } set { } }
        public System.Version Version { get { throw null; } set { } }
        public System.Configuration.Assemblies.AssemblyVersionCompatibility VersionCompatibility { get { throw null; } set { } }
        public object Clone() { throw null; }
        public static System.Reflection.AssemblyName GetAssemblyName(string assemblyFile) { throw null; }
        public void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public byte[] GetPublicKey() { throw null; }
        public byte[] GetPublicKeyToken() { throw null; }
        public void OnDeserialization(object sender) { }
        public static bool ReferenceMatchesDefinition(System.Reflection.AssemblyName reference, System.Reflection.AssemblyName definition) { throw null; }
        public void SetPublicKey(byte[] publicKey) { }
        public void SetPublicKeyToken(byte[] publicKeyToken) { }
        public override string ToString() { throw null; }
    }
    [System.FlagsAttribute]
    public enum AssemblyNameFlags
    {
        EnableJITcompileOptimizer = 16384,
        EnableJITcompileTracking = 32768,
        None = 0,
        PublicKey = 1,
        Retargetable = 256,
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(1), Inherited=false)]
    public sealed partial class AssemblyProductAttribute : System.Attribute
    {
        public AssemblyProductAttribute(string product) { }
        public string Product { get { throw null; } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(1), Inherited=false, AllowMultiple=false)]
    public sealed partial class AssemblySignatureKeyAttribute : System.Attribute
    {
        public AssemblySignatureKeyAttribute(string publicKey, string countersignature) { }
        public string Countersignature { get { throw null; } }
        public string PublicKey { get { throw null; } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(1), Inherited=false)]
    public sealed partial class AssemblyTitleAttribute : System.Attribute
    {
        public AssemblyTitleAttribute(string title) { }
        public string Title { get { throw null; } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(1), Inherited=false)]
    public sealed partial class AssemblyTrademarkAttribute : System.Attribute
    {
        public AssemblyTrademarkAttribute(string trademark) { }
        public string Trademark { get { throw null; } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(1), Inherited=false)]
    public sealed partial class AssemblyVersionAttribute : System.Attribute
    {
        public AssemblyVersionAttribute(string version) { }
        public string Version { get { throw null; } }
    }
    public abstract partial class Binder
    {
        protected Binder() { }
        public abstract System.Reflection.FieldInfo BindToField(System.Reflection.BindingFlags bindingAttr, System.Reflection.FieldInfo[] match, object value, System.Globalization.CultureInfo culture);
        public abstract System.Reflection.MethodBase BindToMethod(System.Reflection.BindingFlags bindingAttr, System.Reflection.MethodBase[] match, ref object[] args, System.Reflection.ParameterModifier[] modifiers, System.Globalization.CultureInfo culture, string[] names, out object state);
        public abstract object ChangeType(object value, System.Type type, System.Globalization.CultureInfo culture);
        public abstract void ReorderArgumentArray(ref object[] args, object state);
        public abstract System.Reflection.MethodBase SelectMethod(System.Reflection.BindingFlags bindingAttr, System.Reflection.MethodBase[] match, System.Type[] types, System.Reflection.ParameterModifier[] modifiers);
        public abstract System.Reflection.PropertyInfo SelectProperty(System.Reflection.BindingFlags bindingAttr, System.Reflection.PropertyInfo[] match, System.Type returnType, System.Type[] indexes, System.Reflection.ParameterModifier[] modifiers);
    }
    [System.FlagsAttribute]
    public enum BindingFlags
    {
        CreateInstance = 512,
        DeclaredOnly = 2,
        Default = 0,
        DoNotWrapExceptions = 33554432,
        ExactBinding = 65536,
        FlattenHierarchy = 64,
        GetField = 1024,
        GetProperty = 4096,
        IgnoreCase = 1,
        IgnoreReturn = 16777216,
        Instance = 4,
        InvokeMethod = 256,
        NonPublic = 32,
        OptionalParamBinding = 262144,
        Public = 16,
        PutDispProperty = 16384,
        PutRefDispProperty = 32768,
        SetField = 2048,
        SetProperty = 8192,
        Static = 8,
        SuppressChangeType = 131072,
    }
    [System.FlagsAttribute]
    public enum CallingConventions
    {
        Any = 3,
        ExplicitThis = 64,
        HasThis = 32,
        Standard = 1,
        VarArgs = 2,
    }
    public abstract partial class ConstructorInfo : System.Reflection.MethodBase
    {
        public static readonly string ConstructorName;
        public static readonly string TypeConstructorName;
        protected ConstructorInfo() { }
        public override System.Reflection.MemberTypes MemberType { get { throw null; } }
        public override bool Equals(object obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public object Invoke(object[] parameters) { throw null; }
        public abstract object Invoke(System.Reflection.BindingFlags invokeAttr, System.Reflection.Binder binder, object[] parameters, System.Globalization.CultureInfo culture);
        public static bool operator ==(System.Reflection.ConstructorInfo left, System.Reflection.ConstructorInfo right) { throw null; }
        public static bool operator !=(System.Reflection.ConstructorInfo left, System.Reflection.ConstructorInfo right) { throw null; }
    }
    public partial class CustomAttributeData
    {
        protected CustomAttributeData() { }
        public System.Type AttributeType { get { throw null; } }
        public virtual System.Reflection.ConstructorInfo Constructor { get { throw null; } }
        public virtual System.Collections.Generic.IList<System.Reflection.CustomAttributeTypedArgument> ConstructorArguments { get { throw null; } }
        public virtual System.Collections.Generic.IList<System.Reflection.CustomAttributeNamedArgument> NamedArguments { get { throw null; } }
        public override bool Equals(object obj) { throw null; }
        public static System.Collections.Generic.IList<System.Reflection.CustomAttributeData> GetCustomAttributes(System.Reflection.Assembly target) { throw null; }
        public static System.Collections.Generic.IList<System.Reflection.CustomAttributeData> GetCustomAttributes(System.Reflection.MemberInfo target) { throw null; }
        public static System.Collections.Generic.IList<System.Reflection.CustomAttributeData> GetCustomAttributes(System.Reflection.Module target) { throw null; }
        public static System.Collections.Generic.IList<System.Reflection.CustomAttributeData> GetCustomAttributes(System.Reflection.ParameterInfo target) { throw null; }
        public override int GetHashCode() { throw null; }
        public override string ToString() { throw null; }
    }
    public static partial class CustomAttributeExtensions
    {
        public static System.Attribute GetCustomAttribute(this System.Reflection.Assembly element, System.Type attributeType) { throw null; }
        public static System.Attribute GetCustomAttribute(this System.Reflection.MemberInfo element, System.Type attributeType) { throw null; }
        public static System.Attribute GetCustomAttribute(this System.Reflection.MemberInfo element, System.Type attributeType, bool inherit) { throw null; }
        public static System.Attribute GetCustomAttribute(this System.Reflection.Module element, System.Type attributeType) { throw null; }
        public static System.Attribute GetCustomAttribute(this System.Reflection.ParameterInfo element, System.Type attributeType) { throw null; }
        public static System.Attribute GetCustomAttribute(this System.Reflection.ParameterInfo element, System.Type attributeType, bool inherit) { throw null; }
        public static System.Collections.Generic.IEnumerable<System.Attribute> GetCustomAttributes(this System.Reflection.Assembly element) { throw null; }
        public static System.Collections.Generic.IEnumerable<System.Attribute> GetCustomAttributes(this System.Reflection.Assembly element, System.Type attributeType) { throw null; }
        public static System.Collections.Generic.IEnumerable<System.Attribute> GetCustomAttributes(this System.Reflection.MemberInfo element) { throw null; }
        public static System.Collections.Generic.IEnumerable<System.Attribute> GetCustomAttributes(this System.Reflection.MemberInfo element, bool inherit) { throw null; }
        public static System.Collections.Generic.IEnumerable<System.Attribute> GetCustomAttributes(this System.Reflection.MemberInfo element, System.Type attributeType) { throw null; }
        public static System.Collections.Generic.IEnumerable<System.Attribute> GetCustomAttributes(this System.Reflection.MemberInfo element, System.Type attributeType, bool inherit) { throw null; }
        public static System.Collections.Generic.IEnumerable<System.Attribute> GetCustomAttributes(this System.Reflection.Module element) { throw null; }
        public static System.Collections.Generic.IEnumerable<System.Attribute> GetCustomAttributes(this System.Reflection.Module element, System.Type attributeType) { throw null; }
        public static System.Collections.Generic.IEnumerable<System.Attribute> GetCustomAttributes(this System.Reflection.ParameterInfo element) { throw null; }
        public static System.Collections.Generic.IEnumerable<System.Attribute> GetCustomAttributes(this System.Reflection.ParameterInfo element, bool inherit) { throw null; }
        public static System.Collections.Generic.IEnumerable<System.Attribute> GetCustomAttributes(this System.Reflection.ParameterInfo element, System.Type attributeType) { throw null; }
        public static System.Collections.Generic.IEnumerable<System.Attribute> GetCustomAttributes(this System.Reflection.ParameterInfo element, System.Type attributeType, bool inherit) { throw null; }
        public static System.Collections.Generic.IEnumerable<T> GetCustomAttributes<T>(this System.Reflection.Assembly element) where T : System.Attribute { throw null; }
        public static System.Collections.Generic.IEnumerable<T> GetCustomAttributes<T>(this System.Reflection.MemberInfo element) where T : System.Attribute { throw null; }
        public static System.Collections.Generic.IEnumerable<T> GetCustomAttributes<T>(this System.Reflection.MemberInfo element, bool inherit) where T : System.Attribute { throw null; }
        public static System.Collections.Generic.IEnumerable<T> GetCustomAttributes<T>(this System.Reflection.Module element) where T : System.Attribute { throw null; }
        public static System.Collections.Generic.IEnumerable<T> GetCustomAttributes<T>(this System.Reflection.ParameterInfo element) where T : System.Attribute { throw null; }
        public static System.Collections.Generic.IEnumerable<T> GetCustomAttributes<T>(this System.Reflection.ParameterInfo element, bool inherit) where T : System.Attribute { throw null; }
        public static T GetCustomAttribute<T>(this System.Reflection.Assembly element) where T : System.Attribute { throw null; }
        public static T GetCustomAttribute<T>(this System.Reflection.MemberInfo element) where T : System.Attribute { throw null; }
        public static T GetCustomAttribute<T>(this System.Reflection.MemberInfo element, bool inherit) where T : System.Attribute { throw null; }
        public static T GetCustomAttribute<T>(this System.Reflection.Module element) where T : System.Attribute { throw null; }
        public static T GetCustomAttribute<T>(this System.Reflection.ParameterInfo element) where T : System.Attribute { throw null; }
        public static T GetCustomAttribute<T>(this System.Reflection.ParameterInfo element, bool inherit) where T : System.Attribute { throw null; }
        public static bool IsDefined(this System.Reflection.Assembly element, System.Type attributeType) { throw null; }
        public static bool IsDefined(this System.Reflection.MemberInfo element, System.Type attributeType) { throw null; }
        public static bool IsDefined(this System.Reflection.MemberInfo element, System.Type attributeType, bool inherit) { throw null; }
        public static bool IsDefined(this System.Reflection.Module element, System.Type attributeType) { throw null; }
        public static bool IsDefined(this System.Reflection.ParameterInfo element, System.Type attributeType) { throw null; }
        public static bool IsDefined(this System.Reflection.ParameterInfo element, System.Type attributeType, bool inherit) { throw null; }
    }
    public partial class CustomAttributeFormatException : System.FormatException
    {
        public CustomAttributeFormatException() { }
        protected CustomAttributeFormatException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public CustomAttributeFormatException(string message) { }
        public CustomAttributeFormatException(string message, System.Exception inner) { }
    }
    public partial struct CustomAttributeNamedArgument
    {
        private object _dummy;
        public CustomAttributeNamedArgument(System.Reflection.MemberInfo memberInfo, object value) { throw null; }
        public CustomAttributeNamedArgument(System.Reflection.MemberInfo memberInfo, System.Reflection.CustomAttributeTypedArgument typedArgument) { throw null; }
        public bool IsField { get { throw null; } }
        public System.Reflection.MemberInfo MemberInfo { get { throw null; } }
        public string MemberName { get { throw null; } }
        public System.Reflection.CustomAttributeTypedArgument TypedValue { get { throw null; } }
        public override bool Equals(object obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Reflection.CustomAttributeNamedArgument left, System.Reflection.CustomAttributeNamedArgument right) { throw null; }
        public static bool operator !=(System.Reflection.CustomAttributeNamedArgument left, System.Reflection.CustomAttributeNamedArgument right) { throw null; }
        public override string ToString() { throw null; }
    }
    public partial struct CustomAttributeTypedArgument
    {
        private object _dummy;
        public CustomAttributeTypedArgument(object value) { throw null; }
        public CustomAttributeTypedArgument(System.Type argumentType, object value) { throw null; }
        public System.Type ArgumentType { get { throw null; } }
        public object Value { get { throw null; } }
        public override bool Equals(object obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Reflection.CustomAttributeTypedArgument left, System.Reflection.CustomAttributeTypedArgument right) { throw null; }
        public static bool operator !=(System.Reflection.CustomAttributeTypedArgument left, System.Reflection.CustomAttributeTypedArgument right) { throw null; }
        public override string ToString() { throw null; }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(1036))]
    public sealed partial class DefaultMemberAttribute : System.Attribute
    {
        public DefaultMemberAttribute(string memberName) { }
        public string MemberName { get { throw null; } }
    }
    [System.FlagsAttribute]
    public enum EventAttributes
    {
        None = 0,
        ReservedMask = 1024,
        RTSpecialName = 1024,
        SpecialName = 512,
    }
    public abstract partial class EventInfo : System.Reflection.MemberInfo
    {
        protected EventInfo() { }
        public virtual System.Reflection.MethodInfo AddMethod { get { throw null; } }
        public abstract System.Reflection.EventAttributes Attributes { get; }
        public virtual System.Type EventHandlerType { get { throw null; } }
        public virtual bool IsMulticast { get { throw null; } }
        public bool IsSpecialName { get { throw null; } }
        public override System.Reflection.MemberTypes MemberType { get { throw null; } }
        public virtual System.Reflection.MethodInfo RaiseMethod { get { throw null; } }
        public virtual System.Reflection.MethodInfo RemoveMethod { get { throw null; } }
        public virtual void AddEventHandler(object target, System.Delegate handler) { }
        public override bool Equals(object obj) { throw null; }
        public System.Reflection.MethodInfo GetAddMethod() { throw null; }
        public abstract System.Reflection.MethodInfo GetAddMethod(bool nonPublic);
        public override int GetHashCode() { throw null; }
        public System.Reflection.MethodInfo[] GetOtherMethods() { throw null; }
        public virtual System.Reflection.MethodInfo[] GetOtherMethods(bool nonPublic) { throw null; }
        public System.Reflection.MethodInfo GetRaiseMethod() { throw null; }
        public abstract System.Reflection.MethodInfo GetRaiseMethod(bool nonPublic);
        public System.Reflection.MethodInfo GetRemoveMethod() { throw null; }
        public abstract System.Reflection.MethodInfo GetRemoveMethod(bool nonPublic);
        public static bool operator ==(System.Reflection.EventInfo left, System.Reflection.EventInfo right) { throw null; }
        public static bool operator !=(System.Reflection.EventInfo left, System.Reflection.EventInfo right) { throw null; }
        public virtual void RemoveEventHandler(object target, System.Delegate handler) { }
    }
    public partial class ExceptionHandlingClause
    {
        protected ExceptionHandlingClause() { }
        public virtual System.Type CatchType { get { throw null; } }
        public virtual int FilterOffset { get { throw null; } }
        public virtual System.Reflection.ExceptionHandlingClauseOptions Flags { get { throw null; } }
        public virtual int HandlerLength { get { throw null; } }
        public virtual int HandlerOffset { get { throw null; } }
        public virtual int TryLength { get { throw null; } }
        public virtual int TryOffset { get { throw null; } }
        public override string ToString() { throw null; }
    }
    [System.FlagsAttribute]
    public enum ExceptionHandlingClauseOptions
    {
        Clause = 0,
        Fault = 4,
        Filter = 1,
        Finally = 2,
    }
    [System.FlagsAttribute]
    public enum FieldAttributes
    {
        Assembly = 3,
        FamANDAssem = 2,
        Family = 4,
        FamORAssem = 5,
        FieldAccessMask = 7,
        HasDefault = 32768,
        HasFieldMarshal = 4096,
        HasFieldRVA = 256,
        InitOnly = 32,
        Literal = 64,
        NotSerialized = 128,
        PinvokeImpl = 8192,
        Private = 1,
        PrivateScope = 0,
        Public = 6,
        ReservedMask = 38144,
        RTSpecialName = 1024,
        SpecialName = 512,
        Static = 16,
    }
    public abstract partial class FieldInfo : System.Reflection.MemberInfo
    {
        protected FieldInfo() { }
        public abstract System.Reflection.FieldAttributes Attributes { get; }
        public abstract System.RuntimeFieldHandle FieldHandle { get; }
        public abstract System.Type FieldType { get; }
        public bool IsAssembly { get { throw null; } }
        public bool IsFamily { get { throw null; } }
        public bool IsFamilyAndAssembly { get { throw null; } }
        public bool IsFamilyOrAssembly { get { throw null; } }
        public bool IsInitOnly { get { throw null; } }
        public bool IsLiteral { get { throw null; } }
        public bool IsNotSerialized { get { throw null; } }
        public bool IsPinvokeImpl { get { throw null; } }
        public bool IsPrivate { get { throw null; } }
        public bool IsPublic { get { throw null; } }
        public virtual bool IsSecurityCritical { get { throw null; } }
        public virtual bool IsSecuritySafeCritical { get { throw null; } }
        public virtual bool IsSecurityTransparent { get { throw null; } }
        public bool IsSpecialName { get { throw null; } }
        public bool IsStatic { get { throw null; } }
        public override System.Reflection.MemberTypes MemberType { get { throw null; } }
        public override bool Equals(object obj) { throw null; }
        public static System.Reflection.FieldInfo GetFieldFromHandle(System.RuntimeFieldHandle handle) { throw null; }
        public static System.Reflection.FieldInfo GetFieldFromHandle(System.RuntimeFieldHandle handle, System.RuntimeTypeHandle declaringType) { throw null; }
        public override int GetHashCode() { throw null; }
        public virtual System.Type[] GetOptionalCustomModifiers() { throw null; }
        public virtual object GetRawConstantValue() { throw null; }
        public virtual System.Type[] GetRequiredCustomModifiers() { throw null; }
        public abstract object GetValue(object obj);
        [System.CLSCompliantAttribute(false)]
        public virtual object GetValueDirect(System.TypedReference obj) { throw null; }
        public static bool operator ==(System.Reflection.FieldInfo left, System.Reflection.FieldInfo right) { throw null; }
        public static bool operator !=(System.Reflection.FieldInfo left, System.Reflection.FieldInfo right) { throw null; }
        public void SetValue(object obj, object value) { }
        public abstract void SetValue(object obj, object value, System.Reflection.BindingFlags invokeAttr, System.Reflection.Binder binder, System.Globalization.CultureInfo culture);
        [System.CLSCompliantAttribute(false)]
        public virtual void SetValueDirect(System.TypedReference obj, object value) { }
    }
    [System.FlagsAttribute]
    public enum GenericParameterAttributes
    {
        Contravariant = 2,
        Covariant = 1,
        DefaultConstructorConstraint = 16,
        None = 0,
        NotNullableValueTypeConstraint = 8,
        ReferenceTypeConstraint = 4,
        SpecialConstraintMask = 28,
        VarianceMask = 3,
    }
    public partial interface ICustomAttributeProvider
    {
        object[] GetCustomAttributes(bool inherit);
        object[] GetCustomAttributes(System.Type attributeType, bool inherit);
        bool IsDefined(System.Type attributeType, bool inherit);
    }
    public enum ImageFileMachine
    {
        AMD64 = 34404,
        ARM = 452,
        I386 = 332,
        IA64 = 512,
    }
    public partial struct InterfaceMapping
    {
        public System.Reflection.MethodInfo[] InterfaceMethods;
        public System.Type InterfaceType;
        public System.Reflection.MethodInfo[] TargetMethods;
        public System.Type TargetType;
    }
    public static partial class IntrospectionExtensions
    {
        public static System.Reflection.TypeInfo GetTypeInfo(this System.Type type) { throw null; }
    }
    public partial class InvalidFilterCriteriaException : System.ApplicationException
    {
        public InvalidFilterCriteriaException() { }
        protected InvalidFilterCriteriaException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public InvalidFilterCriteriaException(string message) { }
        public InvalidFilterCriteriaException(string message, System.Exception inner) { }
    }
    public partial interface IReflect
    {
        System.Type UnderlyingSystemType { get; }
        System.Reflection.FieldInfo GetField(string name, System.Reflection.BindingFlags bindingAttr);
        System.Reflection.FieldInfo[] GetFields(System.Reflection.BindingFlags bindingAttr);
        System.Reflection.MemberInfo[] GetMember(string name, System.Reflection.BindingFlags bindingAttr);
        System.Reflection.MemberInfo[] GetMembers(System.Reflection.BindingFlags bindingAttr);
        System.Reflection.MethodInfo GetMethod(string name, System.Reflection.BindingFlags bindingAttr);
        System.Reflection.MethodInfo GetMethod(string name, System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder binder, System.Type[] types, System.Reflection.ParameterModifier[] modifiers);
        System.Reflection.MethodInfo[] GetMethods(System.Reflection.BindingFlags bindingAttr);
        System.Reflection.PropertyInfo[] GetProperties(System.Reflection.BindingFlags bindingAttr);
        System.Reflection.PropertyInfo GetProperty(string name, System.Reflection.BindingFlags bindingAttr);
        System.Reflection.PropertyInfo GetProperty(string name, System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder binder, System.Type returnType, System.Type[] types, System.Reflection.ParameterModifier[] modifiers);
        object InvokeMember(string name, System.Reflection.BindingFlags invokeAttr, System.Reflection.Binder binder, object target, object[] args, System.Reflection.ParameterModifier[] modifiers, System.Globalization.CultureInfo culture, string[] namedParameters);
    }
    public partial interface IReflectableType
    {
        System.Reflection.TypeInfo GetTypeInfo();
    }
    public partial class LocalVariableInfo
    {
        protected LocalVariableInfo() { }
        public virtual bool IsPinned { get { throw null; } }
        public virtual int LocalIndex { get { throw null; } }
        public virtual System.Type LocalType { get { throw null; } }
        public override string ToString() { throw null; }
    }
    public partial class ManifestResourceInfo
    {
        public ManifestResourceInfo(System.Reflection.Assembly containingAssembly, string containingFileName, System.Reflection.ResourceLocation resourceLocation) { }
        public virtual string FileName { get { throw null; } }
        public virtual System.Reflection.Assembly ReferencedAssembly { get { throw null; } }
        public virtual System.Reflection.ResourceLocation ResourceLocation { get { throw null; } }
    }
    public delegate bool MemberFilter(System.Reflection.MemberInfo m, object filterCriteria);
    public abstract partial class MemberInfo : System.Reflection.ICustomAttributeProvider
    {
        protected MemberInfo() { }
        public virtual System.Collections.Generic.IEnumerable<System.Reflection.CustomAttributeData> CustomAttributes { get { throw null; } }
        public abstract System.Type DeclaringType { get; }
        public abstract System.Reflection.MemberTypes MemberType { get; }
        public virtual int MetadataToken { get { throw null; } }
        public virtual System.Reflection.Module Module { get { throw null; } }
        public abstract string Name { get; }
        public abstract System.Type ReflectedType { get; }
        public override bool Equals(object obj) { throw null; }
        public abstract object[] GetCustomAttributes(bool inherit);
        public abstract object[] GetCustomAttributes(System.Type attributeType, bool inherit);
        public virtual System.Collections.Generic.IList<System.Reflection.CustomAttributeData> GetCustomAttributesData() { throw null; }
        public override int GetHashCode() { throw null; }
        public virtual bool HasSameMetadataDefinitionAs(System.Reflection.MemberInfo other) { throw null; }
        public abstract bool IsDefined(System.Type attributeType, bool inherit);
        public static bool operator ==(System.Reflection.MemberInfo left, System.Reflection.MemberInfo right) { throw null; }
        public static bool operator !=(System.Reflection.MemberInfo left, System.Reflection.MemberInfo right) { throw null; }
    }
    [System.FlagsAttribute]
    public enum MemberTypes
    {
        All = 191,
        Constructor = 1,
        Custom = 64,
        Event = 2,
        Field = 4,
        Method = 8,
        NestedType = 128,
        Property = 16,
        TypeInfo = 32,
    }
    [System.FlagsAttribute]
    public enum MethodAttributes
    {
        Abstract = 1024,
        Assembly = 3,
        CheckAccessOnOverride = 512,
        FamANDAssem = 2,
        Family = 4,
        FamORAssem = 5,
        Final = 32,
        HasSecurity = 16384,
        HideBySig = 128,
        MemberAccessMask = 7,
        NewSlot = 256,
        PinvokeImpl = 8192,
        Private = 1,
        PrivateScope = 0,
        Public = 6,
        RequireSecObject = 32768,
        ReservedMask = 53248,
        ReuseSlot = 0,
        RTSpecialName = 4096,
        SpecialName = 2048,
        Static = 16,
        UnmanagedExport = 8,
        Virtual = 64,
        VtableLayoutMask = 256,
    }
    public abstract partial class MethodBase : System.Reflection.MemberInfo
    {
        protected MethodBase() { }
        public abstract System.Reflection.MethodAttributes Attributes { get; }
        public virtual System.Reflection.CallingConventions CallingConvention { get { throw null; } }
        public virtual bool ContainsGenericParameters { get { throw null; } }
        public bool IsAbstract { get { throw null; } }
        public bool IsAssembly { get { throw null; } }
        public virtual bool IsConstructedGenericMethod { get { throw null; } }
        public bool IsConstructor { get { throw null; } }
        public bool IsFamily { get { throw null; } }
        public bool IsFamilyAndAssembly { get { throw null; } }
        public bool IsFamilyOrAssembly { get { throw null; } }
        public bool IsFinal { get { throw null; } }
        public virtual bool IsGenericMethod { get { throw null; } }
        public virtual bool IsGenericMethodDefinition { get { throw null; } }
        public bool IsHideBySig { get { throw null; } }
        public bool IsPrivate { get { throw null; } }
        public bool IsPublic { get { throw null; } }
        public virtual bool IsSecurityCritical { get { throw null; } }
        public virtual bool IsSecuritySafeCritical { get { throw null; } }
        public virtual bool IsSecurityTransparent { get { throw null; } }
        public bool IsSpecialName { get { throw null; } }
        public bool IsStatic { get { throw null; } }
        public bool IsVirtual { get { throw null; } }
        public abstract System.RuntimeMethodHandle MethodHandle { get; }
        public virtual System.Reflection.MethodImplAttributes MethodImplementationFlags { get { throw null; } }
        public override bool Equals(object obj) { throw null; }
        public static System.Reflection.MethodBase GetCurrentMethod() { throw null; }
        public virtual System.Type[] GetGenericArguments() { throw null; }
        public override int GetHashCode() { throw null; }
        public virtual System.Reflection.MethodBody GetMethodBody() { throw null; }
        public static System.Reflection.MethodBase GetMethodFromHandle(System.RuntimeMethodHandle handle) { throw null; }
        public static System.Reflection.MethodBase GetMethodFromHandle(System.RuntimeMethodHandle handle, System.RuntimeTypeHandle declaringType) { throw null; }
        public abstract System.Reflection.MethodImplAttributes GetMethodImplementationFlags();
        public abstract System.Reflection.ParameterInfo[] GetParameters();
        public object Invoke(object obj, object[] parameters) { throw null; }
        public abstract object Invoke(object obj, System.Reflection.BindingFlags invokeAttr, System.Reflection.Binder binder, object[] parameters, System.Globalization.CultureInfo culture);
        public static bool operator ==(System.Reflection.MethodBase left, System.Reflection.MethodBase right) { throw null; }
        public static bool operator !=(System.Reflection.MethodBase left, System.Reflection.MethodBase right) { throw null; }
    }
    public partial class MethodBody
    {
        protected MethodBody() { }
        public virtual System.Collections.Generic.IList<System.Reflection.ExceptionHandlingClause> ExceptionHandlingClauses { get { throw null; } }
        public virtual bool InitLocals { get { throw null; } }
        public virtual int LocalSignatureMetadataToken { get { throw null; } }
        public virtual System.Collections.Generic.IList<System.Reflection.LocalVariableInfo> LocalVariables { get { throw null; } }
        public virtual int MaxStackSize { get { throw null; } }
        public virtual byte[] GetILAsByteArray() { throw null; }
    }
    public enum MethodImplAttributes
    {
        AggressiveInlining = 256,
        CodeTypeMask = 3,
        ForwardRef = 16,
        IL = 0,
        InternalCall = 4096,
        Managed = 0,
        ManagedMask = 4,
        MaxMethodImplVal = 65535,
        Native = 1,
        NoInlining = 8,
        NoOptimization = 64,
        OPTIL = 2,
        PreserveSig = 128,
        Runtime = 3,
        Synchronized = 32,
        Unmanaged = 4,
    }
    public abstract partial class MethodInfo : System.Reflection.MethodBase
    {
        protected MethodInfo() { }
        public override System.Reflection.MemberTypes MemberType { get { throw null; } }
        public virtual System.Reflection.ParameterInfo ReturnParameter { get { throw null; } }
        public virtual System.Type ReturnType { get { throw null; } }
        public abstract System.Reflection.ICustomAttributeProvider ReturnTypeCustomAttributes { get; }
        public virtual System.Delegate CreateDelegate(System.Type delegateType) { throw null; }
        public virtual System.Delegate CreateDelegate(System.Type delegateType, object target) { throw null; }
        public override bool Equals(object obj) { throw null; }
        public abstract System.Reflection.MethodInfo GetBaseDefinition();
        public override System.Type[] GetGenericArguments() { throw null; }
        public virtual System.Reflection.MethodInfo GetGenericMethodDefinition() { throw null; }
        public override int GetHashCode() { throw null; }
        public virtual System.Reflection.MethodInfo MakeGenericMethod(params System.Type[] typeArguments) { throw null; }
        public static bool operator ==(System.Reflection.MethodInfo left, System.Reflection.MethodInfo right) { throw null; }
        public static bool operator !=(System.Reflection.MethodInfo left, System.Reflection.MethodInfo right) { throw null; }
    }
    public sealed partial class Missing : System.Runtime.Serialization.ISerializable
    {
        internal Missing() { }
        public static readonly System.Reflection.Missing Value;
        void System.Runtime.Serialization.ISerializable.GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    }
    public abstract partial class Module : System.Reflection.ICustomAttributeProvider, System.Runtime.Serialization.ISerializable
    {
        public static readonly System.Reflection.TypeFilter FilterTypeName;
        public static readonly System.Reflection.TypeFilter FilterTypeNameIgnoreCase;
        protected Module() { }
        public virtual System.Reflection.Assembly Assembly { get { throw null; } }
        public virtual System.Collections.Generic.IEnumerable<System.Reflection.CustomAttributeData> CustomAttributes { get { throw null; } }
        public virtual string FullyQualifiedName { get { throw null; } }
        public virtual int MDStreamVersion { get { throw null; } }
        public virtual int MetadataToken { get { throw null; } }
        public System.ModuleHandle ModuleHandle { get { throw null; } }
        public virtual System.Guid ModuleVersionId { get { throw null; } }
        public virtual string Name { get { throw null; } }
        public virtual string ScopeName { get { throw null; } }
        public override bool Equals(object o) { throw null; }
        public virtual System.Type[] FindTypes(System.Reflection.TypeFilter filter, object filterCriteria) { throw null; }
        public virtual object[] GetCustomAttributes(bool inherit) { throw null; }
        public virtual object[] GetCustomAttributes(System.Type attributeType, bool inherit) { throw null; }
        public virtual System.Collections.Generic.IList<System.Reflection.CustomAttributeData> GetCustomAttributesData() { throw null; }
        public System.Reflection.FieldInfo GetField(string name) { throw null; }
        public virtual System.Reflection.FieldInfo GetField(string name, System.Reflection.BindingFlags bindingAttr) { throw null; }
        public System.Reflection.FieldInfo[] GetFields() { throw null; }
        public virtual System.Reflection.FieldInfo[] GetFields(System.Reflection.BindingFlags bindingFlags) { throw null; }
        public override int GetHashCode() { throw null; }
        public System.Reflection.MethodInfo GetMethod(string name) { throw null; }
        public System.Reflection.MethodInfo GetMethod(string name, System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder binder, System.Reflection.CallingConventions callConvention, System.Type[] types, System.Reflection.ParameterModifier[] modifiers) { throw null; }
        public System.Reflection.MethodInfo GetMethod(string name, System.Type[] types) { throw null; }
        protected virtual System.Reflection.MethodInfo GetMethodImpl(string name, System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder binder, System.Reflection.CallingConventions callConvention, System.Type[] types, System.Reflection.ParameterModifier[] modifiers) { throw null; }
        public System.Reflection.MethodInfo[] GetMethods() { throw null; }
        public virtual System.Reflection.MethodInfo[] GetMethods(System.Reflection.BindingFlags bindingFlags) { throw null; }
        public virtual void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public virtual void GetPEKind(out System.Reflection.PortableExecutableKinds peKind, out System.Reflection.ImageFileMachine machine) { throw null; }
        public virtual System.Type GetType(string className) { throw null; }
        public virtual System.Type GetType(string className, bool ignoreCase) { throw null; }
        public virtual System.Type GetType(string className, bool throwOnError, bool ignoreCase) { throw null; }
        public virtual System.Type[] GetTypes() { throw null; }
        public virtual bool IsDefined(System.Type attributeType, bool inherit) { throw null; }
        public virtual bool IsResource() { throw null; }
        public static bool operator ==(System.Reflection.Module left, System.Reflection.Module right) { throw null; }
        public static bool operator !=(System.Reflection.Module left, System.Reflection.Module right) { throw null; }
        public System.Reflection.FieldInfo ResolveField(int metadataToken) { throw null; }
        public virtual System.Reflection.FieldInfo ResolveField(int metadataToken, System.Type[] genericTypeArguments, System.Type[] genericMethodArguments) { throw null; }
        public System.Reflection.MemberInfo ResolveMember(int metadataToken) { throw null; }
        public virtual System.Reflection.MemberInfo ResolveMember(int metadataToken, System.Type[] genericTypeArguments, System.Type[] genericMethodArguments) { throw null; }
        public System.Reflection.MethodBase ResolveMethod(int metadataToken) { throw null; }
        public virtual System.Reflection.MethodBase ResolveMethod(int metadataToken, System.Type[] genericTypeArguments, System.Type[] genericMethodArguments) { throw null; }
        public virtual byte[] ResolveSignature(int metadataToken) { throw null; }
        public virtual string ResolveString(int metadataToken) { throw null; }
        public System.Type ResolveType(int metadataToken) { throw null; }
        public virtual System.Type ResolveType(int metadataToken, System.Type[] genericTypeArguments, System.Type[] genericMethodArguments) { throw null; }
        public override string ToString() { throw null; }
    }
    public delegate System.Reflection.Module ModuleResolveEventHandler(object sender, System.ResolveEventArgs e);
    [System.AttributeUsageAttribute((System.AttributeTargets)(1), AllowMultiple=false, Inherited=false)]
    public sealed partial class ObfuscateAssemblyAttribute : System.Attribute
    {
        public ObfuscateAssemblyAttribute(bool assemblyIsPrivate) { }
        public bool AssemblyIsPrivate { get { throw null; } }
        public bool StripAfterObfuscation { get { throw null; } set { } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(8157), AllowMultiple=true, Inherited=false)]
    public sealed partial class ObfuscationAttribute : System.Attribute
    {
        public ObfuscationAttribute() { }
        public bool ApplyToMembers { get { throw null; } set { } }
        public bool Exclude { get { throw null; } set { } }
        public string Feature { get { throw null; } set { } }
        public bool StripAfterObfuscation { get { throw null; } set { } }
    }
    [System.FlagsAttribute]
    public enum ParameterAttributes
    {
        HasDefault = 4096,
        HasFieldMarshal = 8192,
        In = 1,
        Lcid = 4,
        None = 0,
        Optional = 16,
        Out = 2,
        Reserved3 = 16384,
        Reserved4 = 32768,
        ReservedMask = 61440,
        Retval = 8,
    }
    public partial class ParameterInfo : System.Reflection.ICustomAttributeProvider, System.Runtime.Serialization.IObjectReference
    {
        protected System.Reflection.ParameterAttributes AttrsImpl;
        protected System.Type ClassImpl;
        protected object DefaultValueImpl;
        protected System.Reflection.MemberInfo MemberImpl;
        protected string NameImpl;
        protected int PositionImpl;
        protected ParameterInfo() { }
        public virtual System.Reflection.ParameterAttributes Attributes { get { throw null; } }
        public virtual System.Collections.Generic.IEnumerable<System.Reflection.CustomAttributeData> CustomAttributes { get { throw null; } }
        public virtual object DefaultValue { get { throw null; } }
        public virtual bool HasDefaultValue { get { throw null; } }
        public bool IsIn { get { throw null; } }
        public bool IsLcid { get { throw null; } }
        public bool IsOptional { get { throw null; } }
        public bool IsOut { get { throw null; } }
        public bool IsRetval { get { throw null; } }
        public virtual System.Reflection.MemberInfo Member { get { throw null; } }
        public virtual int MetadataToken { get { throw null; } }
        public virtual string Name { get { throw null; } }
        public virtual System.Type ParameterType { get { throw null; } }
        public virtual int Position { get { throw null; } }
        public virtual object RawDefaultValue { get { throw null; } }
        public virtual object[] GetCustomAttributes(bool inherit) { throw null; }
        public virtual object[] GetCustomAttributes(System.Type attributeType, bool inherit) { throw null; }
        public virtual System.Collections.Generic.IList<System.Reflection.CustomAttributeData> GetCustomAttributesData() { throw null; }
        public virtual System.Type[] GetOptionalCustomModifiers() { throw null; }
        public object GetRealObject(System.Runtime.Serialization.StreamingContext context) { throw null; }
        public virtual System.Type[] GetRequiredCustomModifiers() { throw null; }
        public virtual bool IsDefined(System.Type attributeType, bool inherit) { throw null; }
        public override string ToString() { throw null; }
    }
    public readonly partial struct ParameterModifier
    {
        private readonly object _dummy;
        public ParameterModifier(int parameterCount) { throw null; }
        public bool this[int index] { get { throw null; } set { } }
    }
    [System.CLSCompliantAttribute(false)]
    public sealed partial class Pointer : System.Runtime.Serialization.ISerializable
    {
        internal Pointer() { }
        public unsafe static object Box(void* ptr, System.Type type) { throw null; }
        void System.Runtime.Serialization.ISerializable.GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public unsafe static void* Unbox(object ptr) { throw null; }
    }
    [System.FlagsAttribute]
    public enum PortableExecutableKinds
    {
        ILOnly = 1,
        NotAPortableExecutableImage = 0,
        PE32Plus = 4,
        Preferred32Bit = 16,
        Required32Bit = 2,
        Unmanaged32Bit = 8,
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
    [System.FlagsAttribute]
    public enum PropertyAttributes
    {
        HasDefault = 4096,
        None = 0,
        Reserved2 = 8192,
        Reserved3 = 16384,
        Reserved4 = 32768,
        ReservedMask = 62464,
        RTSpecialName = 1024,
        SpecialName = 512,
    }
    public abstract partial class PropertyInfo : System.Reflection.MemberInfo
    {
        protected PropertyInfo() { }
        public abstract System.Reflection.PropertyAttributes Attributes { get; }
        public abstract bool CanRead { get; }
        public abstract bool CanWrite { get; }
        public virtual System.Reflection.MethodInfo GetMethod { get { throw null; } }
        public bool IsSpecialName { get { throw null; } }
        public override System.Reflection.MemberTypes MemberType { get { throw null; } }
        public abstract System.Type PropertyType { get; }
        public virtual System.Reflection.MethodInfo SetMethod { get { throw null; } }
        public override bool Equals(object obj) { throw null; }
        public System.Reflection.MethodInfo[] GetAccessors() { throw null; }
        public abstract System.Reflection.MethodInfo[] GetAccessors(bool nonPublic);
        public virtual object GetConstantValue() { throw null; }
        public System.Reflection.MethodInfo GetGetMethod() { throw null; }
        public abstract System.Reflection.MethodInfo GetGetMethod(bool nonPublic);
        public override int GetHashCode() { throw null; }
        public abstract System.Reflection.ParameterInfo[] GetIndexParameters();
        public virtual System.Type[] GetOptionalCustomModifiers() { throw null; }
        public virtual object GetRawConstantValue() { throw null; }
        public virtual System.Type[] GetRequiredCustomModifiers() { throw null; }
        public System.Reflection.MethodInfo GetSetMethod() { throw null; }
        public abstract System.Reflection.MethodInfo GetSetMethod(bool nonPublic);
        public object GetValue(object obj) { throw null; }
        public virtual object GetValue(object obj, object[] index) { throw null; }
        public abstract object GetValue(object obj, System.Reflection.BindingFlags invokeAttr, System.Reflection.Binder binder, object[] index, System.Globalization.CultureInfo culture);
        public static bool operator ==(System.Reflection.PropertyInfo left, System.Reflection.PropertyInfo right) { throw null; }
        public static bool operator !=(System.Reflection.PropertyInfo left, System.Reflection.PropertyInfo right) { throw null; }
        public void SetValue(object obj, object value) { }
        public virtual void SetValue(object obj, object value, object[] index) { }
        public abstract void SetValue(object obj, object value, System.Reflection.BindingFlags invokeAttr, System.Reflection.Binder binder, object[] index, System.Globalization.CultureInfo culture);
    }
    public abstract partial class ReflectionContext
    {
        protected ReflectionContext() { }
        public virtual System.Reflection.TypeInfo GetTypeForObject(object value) { throw null; }
        public abstract System.Reflection.Assembly MapAssembly(System.Reflection.Assembly assembly);
        public abstract System.Reflection.TypeInfo MapType(System.Reflection.TypeInfo type);
    }
    public sealed partial class ReflectionTypeLoadException : System.SystemException, System.Runtime.Serialization.ISerializable
    {
        public ReflectionTypeLoadException(System.Type[] classes, System.Exception[] exceptions) { }
        public ReflectionTypeLoadException(System.Type[] classes, System.Exception[] exceptions, string message) { }
        public System.Exception[] LoaderExceptions { get { throw null; } }
        public System.Type[] Types { get { throw null; } }
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    }
    [System.FlagsAttribute]
    public enum ResourceAttributes
    {
        Private = 2,
        Public = 1,
    }
    [System.FlagsAttribute]
    public enum ResourceLocation
    {
        ContainedInAnotherAssembly = 2,
        ContainedInManifestFile = 4,
        Embedded = 1,
    }
    public static partial class RuntimeReflectionExtensions
    {
        public static System.Reflection.MethodInfo GetMethodInfo(this System.Delegate del) { throw null; }
        public static System.Reflection.MethodInfo GetRuntimeBaseDefinition(this System.Reflection.MethodInfo method) { throw null; }
        public static System.Reflection.EventInfo GetRuntimeEvent(this System.Type type, string name) { throw null; }
        public static System.Collections.Generic.IEnumerable<System.Reflection.EventInfo> GetRuntimeEvents(this System.Type type) { throw null; }
        public static System.Reflection.FieldInfo GetRuntimeField(this System.Type type, string name) { throw null; }
        public static System.Collections.Generic.IEnumerable<System.Reflection.FieldInfo> GetRuntimeFields(this System.Type type) { throw null; }
        public static System.Reflection.InterfaceMapping GetRuntimeInterfaceMap(this System.Reflection.TypeInfo typeInfo, System.Type interfaceType) { throw null; }
        public static System.Reflection.MethodInfo GetRuntimeMethod(this System.Type type, string name, System.Type[] parameters) { throw null; }
        public static System.Collections.Generic.IEnumerable<System.Reflection.MethodInfo> GetRuntimeMethods(this System.Type type) { throw null; }
        public static System.Collections.Generic.IEnumerable<System.Reflection.PropertyInfo> GetRuntimeProperties(this System.Type type) { throw null; }
        public static System.Reflection.PropertyInfo GetRuntimeProperty(this System.Type type, string name) { throw null; }
    }
    public partial class StrongNameKeyPair : System.Runtime.Serialization.IDeserializationCallback, System.Runtime.Serialization.ISerializable
    {
        public StrongNameKeyPair(byte[] keyPairArray) { }
        public StrongNameKeyPair(System.IO.FileStream keyPairFile) { }
        protected StrongNameKeyPair(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public StrongNameKeyPair(string keyPairContainer) { }
        public byte[] PublicKey { get { throw null; } }
        void System.Runtime.Serialization.IDeserializationCallback.OnDeserialization(object sender) { }
        void System.Runtime.Serialization.ISerializable.GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    }
    public partial class TargetException : System.ApplicationException
    {
        public TargetException() { }
        protected TargetException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public TargetException(string message) { }
        public TargetException(string message, System.Exception inner) { }
    }
    public sealed partial class TargetInvocationException : System.ApplicationException
    {
        public TargetInvocationException(System.Exception inner) { }
        public TargetInvocationException(string message, System.Exception inner) { }
    }
    public sealed partial class TargetParameterCountException : System.ApplicationException
    {
        public TargetParameterCountException() { }
        public TargetParameterCountException(string message) { }
        public TargetParameterCountException(string message, System.Exception inner) { }
    }
    [System.FlagsAttribute]
    public enum TypeAttributes
    {
        Abstract = 128,
        AnsiClass = 0,
        AutoClass = 131072,
        AutoLayout = 0,
        BeforeFieldInit = 1048576,
        Class = 0,
        ClassSemanticsMask = 32,
        CustomFormatClass = 196608,
        CustomFormatMask = 12582912,
        ExplicitLayout = 16,
        HasSecurity = 262144,
        Import = 4096,
        Interface = 32,
        LayoutMask = 24,
        NestedAssembly = 5,
        NestedFamANDAssem = 6,
        NestedFamily = 4,
        NestedFamORAssem = 7,
        NestedPrivate = 3,
        NestedPublic = 2,
        NotPublic = 0,
        Public = 1,
        ReservedMask = 264192,
        RTSpecialName = 2048,
        Sealed = 256,
        SequentialLayout = 8,
        Serializable = 8192,
        SpecialName = 1024,
        StringFormatMask = 196608,
        UnicodeClass = 65536,
        VisibilityMask = 7,
        WindowsRuntime = 16384,
    }
    public partial class TypeDelegator : System.Reflection.TypeInfo
    {
        protected System.Type typeImpl;
        protected TypeDelegator() { }
        public TypeDelegator(System.Type delegatingType) { }
        public override System.Reflection.Assembly Assembly { get { throw null; } }
        public override string AssemblyQualifiedName { get { throw null; } }
        public override System.Type BaseType { get { throw null; } }
        public override string FullName { get { throw null; } }
        public override System.Guid GUID { get { throw null; } }
        public override bool IsByRefLike { get { throw null; } }
        public override bool IsConstructedGenericType { get { throw null; } }
        public override bool IsGenericMethodParameter { get { throw null; } }
        public override bool IsGenericTypeParameter { get { throw null; } }
        public override bool IsSZArray { get { throw null; } }
        public override bool IsTypeDefinition { get { throw null; } }
        public override bool IsVariableBoundArray { get { throw null; } }
        public override int MetadataToken { get { throw null; } }
        public override System.Reflection.Module Module { get { throw null; } }
        public override string Name { get { throw null; } }
        public override string Namespace { get { throw null; } }
        public override System.RuntimeTypeHandle TypeHandle { get { throw null; } }
        public override System.Type UnderlyingSystemType { get { throw null; } }
        protected override System.Reflection.TypeAttributes GetAttributeFlagsImpl() { throw null; }
        protected override System.Reflection.ConstructorInfo GetConstructorImpl(System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder binder, System.Reflection.CallingConventions callConvention, System.Type[] types, System.Reflection.ParameterModifier[] modifiers) { throw null; }
        public override System.Reflection.ConstructorInfo[] GetConstructors(System.Reflection.BindingFlags bindingAttr) { throw null; }
        public override object[] GetCustomAttributes(bool inherit) { throw null; }
        public override object[] GetCustomAttributes(System.Type attributeType, bool inherit) { throw null; }
        public override System.Type GetElementType() { throw null; }
        public override System.Reflection.EventInfo GetEvent(string name, System.Reflection.BindingFlags bindingAttr) { throw null; }
        public override System.Reflection.EventInfo[] GetEvents() { throw null; }
        public override System.Reflection.EventInfo[] GetEvents(System.Reflection.BindingFlags bindingAttr) { throw null; }
        public override System.Reflection.FieldInfo GetField(string name, System.Reflection.BindingFlags bindingAttr) { throw null; }
        public override System.Reflection.FieldInfo[] GetFields(System.Reflection.BindingFlags bindingAttr) { throw null; }
        public override System.Type GetInterface(string name, bool ignoreCase) { throw null; }
        public override System.Reflection.InterfaceMapping GetInterfaceMap(System.Type interfaceType) { throw null; }
        public override System.Type[] GetInterfaces() { throw null; }
        public override System.Reflection.MemberInfo[] GetMember(string name, System.Reflection.MemberTypes type, System.Reflection.BindingFlags bindingAttr) { throw null; }
        public override System.Reflection.MemberInfo[] GetMembers(System.Reflection.BindingFlags bindingAttr) { throw null; }
        protected override System.Reflection.MethodInfo GetMethodImpl(string name, System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder binder, System.Reflection.CallingConventions callConvention, System.Type[] types, System.Reflection.ParameterModifier[] modifiers) { throw null; }
        public override System.Reflection.MethodInfo[] GetMethods(System.Reflection.BindingFlags bindingAttr) { throw null; }
        public override System.Type GetNestedType(string name, System.Reflection.BindingFlags bindingAttr) { throw null; }
        public override System.Type[] GetNestedTypes(System.Reflection.BindingFlags bindingAttr) { throw null; }
        public override System.Reflection.PropertyInfo[] GetProperties(System.Reflection.BindingFlags bindingAttr) { throw null; }
        protected override System.Reflection.PropertyInfo GetPropertyImpl(string name, System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder binder, System.Type returnType, System.Type[] types, System.Reflection.ParameterModifier[] modifiers) { throw null; }
        protected override bool HasElementTypeImpl() { throw null; }
        public override object InvokeMember(string name, System.Reflection.BindingFlags invokeAttr, System.Reflection.Binder binder, object target, object[] args, System.Reflection.ParameterModifier[] modifiers, System.Globalization.CultureInfo culture, string[] namedParameters) { throw null; }
        protected override bool IsArrayImpl() { throw null; }
        public override bool IsAssignableFrom(System.Reflection.TypeInfo typeInfo) { throw null; }
        protected override bool IsByRefImpl() { throw null; }
        protected override bool IsCOMObjectImpl() { throw null; }
        public override bool IsDefined(System.Type attributeType, bool inherit) { throw null; }
        protected override bool IsPointerImpl() { throw null; }
        protected override bool IsPrimitiveImpl() { throw null; }
        protected override bool IsValueTypeImpl() { throw null; }
    }
    public delegate bool TypeFilter(System.Type m, object filterCriteria);
    public abstract partial class TypeInfo : System.Type, System.Reflection.IReflectableType
    {
        internal TypeInfo() { }
        public virtual System.Collections.Generic.IEnumerable<System.Reflection.ConstructorInfo> DeclaredConstructors { get { throw null; } }
        public virtual System.Collections.Generic.IEnumerable<System.Reflection.EventInfo> DeclaredEvents { get { throw null; } }
        public virtual System.Collections.Generic.IEnumerable<System.Reflection.FieldInfo> DeclaredFields { get { throw null; } }
        public virtual System.Collections.Generic.IEnumerable<System.Reflection.MemberInfo> DeclaredMembers { get { throw null; } }
        public virtual System.Collections.Generic.IEnumerable<System.Reflection.MethodInfo> DeclaredMethods { get { throw null; } }
        public virtual System.Collections.Generic.IEnumerable<System.Reflection.TypeInfo> DeclaredNestedTypes { get { throw null; } }
        public virtual System.Collections.Generic.IEnumerable<System.Reflection.PropertyInfo> DeclaredProperties { get { throw null; } }
        public virtual System.Type[] GenericTypeParameters { get { throw null; } }
        public virtual System.Collections.Generic.IEnumerable<System.Type> ImplementedInterfaces { get { throw null; } }
        public virtual System.Type AsType() { throw null; }
        public virtual System.Reflection.EventInfo GetDeclaredEvent(string name) { throw null; }
        public virtual System.Reflection.FieldInfo GetDeclaredField(string name) { throw null; }
        public virtual System.Reflection.MethodInfo GetDeclaredMethod(string name) { throw null; }
        public virtual System.Collections.Generic.IEnumerable<System.Reflection.MethodInfo> GetDeclaredMethods(string name) { throw null; }
        public virtual System.Reflection.TypeInfo GetDeclaredNestedType(string name) { throw null; }
        public virtual System.Reflection.PropertyInfo GetDeclaredProperty(string name) { throw null; }
        public virtual bool IsAssignableFrom(System.Reflection.TypeInfo typeInfo) { throw null; }
        System.Reflection.TypeInfo System.Reflection.IReflectableType.GetTypeInfo() { throw null; }
    }
}
namespace System.Runtime
{
    [System.AttributeUsageAttribute((System.AttributeTargets)(1), Inherited=false)]
    public sealed partial class AssemblyTargetedPatchBandAttribute : System.Attribute
    {
        public AssemblyTargetedPatchBandAttribute(string targetedPatchBand) { }
        public string TargetedPatchBand { get { throw null; } }
    }
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
        NoGCRegion = 4,
        SustainedLowLatency = 3,
    }
    public static partial class GCSettings
    {
        public static bool IsServerGC { get { throw null; } }
        public static System.Runtime.GCLargeObjectHeapCompactionMode LargeObjectHeapCompactionMode { get { throw null; } set { } }
        public static System.Runtime.GCLatencyMode LatencyMode { get { throw null; } set { } }
    }
    public sealed partial class MemoryFailPoint : System.Runtime.ConstrainedExecution.CriticalFinalizerObject, System.IDisposable
    {
        public MemoryFailPoint(int sizeInMegabytes) { }
        public void Dispose() { }
        ~MemoryFailPoint() { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(96), AllowMultiple=false, Inherited=false)]
    public sealed partial class TargetedPatchingOptOutAttribute : System.Attribute
    {
        public TargetedPatchingOptOutAttribute(string reason) { }
        public string Reason { get { throw null; } }
    }
}
namespace System.Runtime.CompilerServices
{
    [System.AttributeUsageAttribute((System.AttributeTargets)(256))]
    public sealed partial class AccessedThroughPropertyAttribute : System.Attribute
    {
        public AccessedThroughPropertyAttribute(string propertyName) { }
        public string PropertyName { get { throw null; } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(5148), Inherited=false, AllowMultiple=false)]
    public sealed partial class AsyncMethodBuilderAttribute : System.Attribute
    {
        public AsyncMethodBuilderAttribute(System.Type builderType) { }
        public System.Type BuilderType { get { throw null; } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(64), Inherited=false, AllowMultiple=false)]
    public sealed partial class AsyncStateMachineAttribute : System.Runtime.CompilerServices.StateMachineAttribute
    {
        public AsyncStateMachineAttribute(System.Type stateMachineType) : base (default(System.Type)) { }
    }
    public partial struct AsyncValueTaskMethodBuilder<TResult>
    {
        private TResult _result;
        public System.Threading.Tasks.ValueTask<TResult> Task { get { throw null; } }
        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : System.Runtime.CompilerServices.INotifyCompletion where TStateMachine : System.Runtime.CompilerServices.IAsyncStateMachine { }
        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : System.Runtime.CompilerServices.ICriticalNotifyCompletion where TStateMachine : System.Runtime.CompilerServices.IAsyncStateMachine { }
        public static System.Runtime.CompilerServices.AsyncValueTaskMethodBuilder<TResult> Create() { throw null; }
        public void SetException(System.Exception exception) { }
        public void SetResult(TResult result) { }
        public void SetStateMachine(System.Runtime.CompilerServices.IAsyncStateMachine stateMachine) { }
        public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : System.Runtime.CompilerServices.IAsyncStateMachine { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(2048), Inherited=false)]
    public sealed partial class CallerFilePathAttribute : System.Attribute
    {
        public CallerFilePathAttribute() { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(2048), Inherited=false)]
    public sealed partial class CallerLineNumberAttribute : System.Attribute
    {
        public CallerLineNumberAttribute() { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(2048), Inherited=false)]
    public sealed partial class CallerMemberNameAttribute : System.Attribute
    {
        public CallerMemberNameAttribute() { }
    }
    [System.FlagsAttribute]
    public enum CompilationRelaxations
    {
        NoStringInterning = 8,
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(71))]
    public partial class CompilationRelaxationsAttribute : System.Attribute
    {
        public CompilationRelaxationsAttribute(int relaxations) { }
        public CompilationRelaxationsAttribute(System.Runtime.CompilerServices.CompilationRelaxations relaxations) { }
        public int CompilationRelaxations { get { throw null; } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(32767), Inherited=true)]
    public sealed partial class CompilerGeneratedAttribute : System.Attribute
    {
        public CompilerGeneratedAttribute() { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(4))]
    public partial class CompilerGlobalScopeAttribute : System.Attribute
    {
        public CompilerGlobalScopeAttribute() { }
    }
    public sealed partial class ConditionalWeakTable<TKey, TValue> : System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<TKey, TValue>>, System.Collections.IEnumerable where TKey : class where TValue : class
    {
        public ConditionalWeakTable() { }
        public void Add(TKey key, TValue value) { }
        public void AddOrUpdate(TKey key, TValue value) { }
        public void Clear() { }
        ~ConditionalWeakTable() { }
        public TValue GetOrCreateValue(TKey key) { throw null; }
        public TValue GetValue(TKey key, System.Runtime.CompilerServices.ConditionalWeakTable<TKey, TValue>.CreateValueCallback createValueCallback) { throw null; }
        public bool Remove(TKey key) { throw null; }
        System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<TKey, TValue>> System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<TKey,TValue>>.GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        public bool TryGetValue(TKey key, out TValue value) { throw null; }
        public delegate TValue CreateValueCallback(TKey key);
    }
    public readonly partial struct ConfiguredTaskAwaitable
    {
        private readonly object _dummy;
        public System.Runtime.CompilerServices.ConfiguredTaskAwaitable.ConfiguredTaskAwaiter GetAwaiter() { throw null; }
        public readonly partial struct ConfiguredTaskAwaiter : System.Runtime.CompilerServices.ICriticalNotifyCompletion, System.Runtime.CompilerServices.INotifyCompletion
        {
            private readonly object _dummy;
            public bool IsCompleted { get { throw null; } }
            public void GetResult() { }
            public void OnCompleted(System.Action continuation) { }
            public void UnsafeOnCompleted(System.Action continuation) { }
        }
    }
    public readonly partial struct ConfiguredTaskAwaitable<TResult>
    {
        private readonly object _dummy;
        public System.Runtime.CompilerServices.ConfiguredTaskAwaitable<TResult>.ConfiguredTaskAwaiter GetAwaiter() { throw null; }
        public readonly partial struct ConfiguredTaskAwaiter : System.Runtime.CompilerServices.ICriticalNotifyCompletion, System.Runtime.CompilerServices.INotifyCompletion
        {
            private readonly object _dummy;
            public bool IsCompleted { get { throw null; } }
            public TResult GetResult() { throw null; }
            public void OnCompleted(System.Action continuation) { }
            public void UnsafeOnCompleted(System.Action continuation) { }
        }
    }
    public readonly partial struct ConfiguredValueTaskAwaitable<TResult>
    {
        private readonly object _dummy;
        public System.Runtime.CompilerServices.ConfiguredValueTaskAwaitable<TResult>.ConfiguredValueTaskAwaiter GetAwaiter() { throw null; }
        public partial struct ConfiguredValueTaskAwaiter : System.Runtime.CompilerServices.ICriticalNotifyCompletion, System.Runtime.CompilerServices.INotifyCompletion
        {
            private object _dummy;
            public bool IsCompleted { get { throw null; } }
            public TResult GetResult() { throw null; }
            public void OnCompleted(System.Action continuation) { }
            public void UnsafeOnCompleted(System.Action continuation) { }
        }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(2304), Inherited=false)]
    public abstract partial class CustomConstantAttribute : System.Attribute
    {
        protected CustomConstantAttribute() { }
        public abstract object Value { get; }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(2304), Inherited=false)]
    public sealed partial class DateTimeConstantAttribute : System.Runtime.CompilerServices.CustomConstantAttribute
    {
        public DateTimeConstantAttribute(long ticks) { }
        public override object Value { get { throw null; } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(2304), Inherited=false)]
    public sealed partial class DecimalConstantAttribute : System.Attribute
    {
        public DecimalConstantAttribute(byte scale, byte sign, int hi, int mid, int low) { }
        [System.CLSCompliantAttribute(false)]
        public DecimalConstantAttribute(byte scale, byte sign, uint hi, uint mid, uint low) { }
        public decimal Value { get { throw null; } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(1))]
    public sealed partial class DefaultDependencyAttribute : System.Attribute
    {
        public DefaultDependencyAttribute(System.Runtime.CompilerServices.LoadHint loadHintArgument) { }
        public System.Runtime.CompilerServices.LoadHint LoadHint { get { throw null; } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(1), AllowMultiple=true)]
    public sealed partial class DependencyAttribute : System.Attribute
    {
        public DependencyAttribute(string dependentAssemblyArgument, System.Runtime.CompilerServices.LoadHint loadHintArgument) { }
        public string DependentAssembly { get { throw null; } }
        public System.Runtime.CompilerServices.LoadHint LoadHint { get { throw null; } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(1), AllowMultiple=false, Inherited=false)]
    public sealed partial class DisablePrivateReflectionAttribute : System.Attribute
    {
        public DisablePrivateReflectionAttribute() { }
    }
    public partial class DiscardableAttribute : System.Attribute
    {
        public DiscardableAttribute() { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(69))]
    public sealed partial class ExtensionAttribute : System.Attribute
    {
        public ExtensionAttribute() { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(256))]
    public sealed partial class FixedAddressValueTypeAttribute : System.Attribute
    {
        public FixedAddressValueTypeAttribute() { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(256), Inherited=false)]
    public sealed partial class FixedBufferAttribute : System.Attribute
    {
        public FixedBufferAttribute(System.Type elementType, int length) { }
        public System.Type ElementType { get { throw null; } }
        public int Length { get { throw null; } }
    }
    public static partial class FormattableStringFactory
    {
        public static System.FormattableString Create(string format, params object[] arguments) { throw null; }
    }
    public partial interface IAsyncStateMachine
    {
        void MoveNext();
        void SetStateMachine(System.Runtime.CompilerServices.IAsyncStateMachine stateMachine);
    }
    public partial interface ICriticalNotifyCompletion : System.Runtime.CompilerServices.INotifyCompletion
    {
        void UnsafeOnCompleted(System.Action continuation);
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(128), Inherited=true)]
    public sealed partial class IndexerNameAttribute : System.Attribute
    {
        public IndexerNameAttribute(string indexerName) { }
    }
    public partial interface INotifyCompletion
    {
        void OnCompleted(System.Action continuation);
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(1), AllowMultiple=true, Inherited=false)]
    public sealed partial class InternalsVisibleToAttribute : System.Attribute
    {
        public InternalsVisibleToAttribute(string assemblyName) { }
        public bool AllInternalsVisible { get { throw null; } set { } }
        public string AssemblyName { get { throw null; } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(8))]
    [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
    public sealed partial class IsByRefLikeAttribute : System.Attribute
    {
        public IsByRefLikeAttribute() { }
    }
    public static partial class IsConst
    {
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(32767), Inherited=false)]
    [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
    public sealed partial class IsReadOnlyAttribute : System.Attribute
    {
        public IsReadOnlyAttribute() { }
    }
    public partial interface IStrongBox
    {
        object Value { get; set; }
    }
    public static partial class IsVolatile
    {
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(64), Inherited=false, AllowMultiple=false)]
    public sealed partial class IteratorStateMachineAttribute : System.Runtime.CompilerServices.StateMachineAttribute
    {
        public IteratorStateMachineAttribute(System.Type stateMachineType) : base (default(System.Type)) { }
    }
    public partial interface ITuple
    {
        object this[int index] { get; }
        int Length { get; }
    }
    public enum LoadHint
    {
        Always = 1,
        Default = 0,
        Sometimes = 2,
    }
    public enum MethodCodeType
    {
        IL = 0,
        Native = 1,
        OPTIL = 2,
        Runtime = 3,
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(96), Inherited=false)]
    public sealed partial class MethodImplAttribute : System.Attribute
    {
        public System.Runtime.CompilerServices.MethodCodeType MethodCodeType;
        public MethodImplAttribute() { }
        public MethodImplAttribute(short value) { }
        public MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions methodImplOptions) { }
        public System.Runtime.CompilerServices.MethodImplOptions Value { get { throw null; } }
    }
    [System.FlagsAttribute]
    public enum MethodImplOptions
    {
        AggressiveInlining = 256,
        ForwardRef = 16,
        InternalCall = 4096,
        NoInlining = 8,
        NoOptimization = 64,
        PreserveSig = 128,
        Synchronized = 32,
        Unmanaged = 4,
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(1), AllowMultiple=false)]
    public sealed partial class ReferenceAssemblyAttribute : System.Attribute
    {
        public ReferenceAssemblyAttribute() { }
        public ReferenceAssemblyAttribute(string description) { }
        public string Description { get { throw null; } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(1), Inherited=false, AllowMultiple=false)]
    public sealed partial class RuntimeCompatibilityAttribute : System.Attribute
    {
        public RuntimeCompatibilityAttribute() { }
        public bool WrapNonExceptionThrows { get { throw null; } set { } }
    }
    public static partial class RuntimeFeature
    {
        public const string PortablePdb = "PortablePdb";
#if FEATURE_DEFAULT_INTERFACES
        public const string DefaultImplementationsOfInterfaces = "DefaultImplementationsOfInterfaces";
#endif
        public static bool IsSupported(string feature) { throw null; }
    }
    public static partial class RuntimeHelpers
    {
        public static int OffsetToStringData { get { throw null; } }
        public static void EnsureSufficientExecutionStack() { }
        public static new bool Equals(object o1, object o2) { throw null; }
        public static void ExecuteCodeWithGuaranteedCleanup(System.Runtime.CompilerServices.RuntimeHelpers.TryCode code, System.Runtime.CompilerServices.RuntimeHelpers.CleanupCode backoutCode, object userData) { }
        public static int GetHashCode(object o) { throw null; }
        public static object GetObjectValue(object obj) { throw null; }
        public static object GetUninitializedObject(System.Type type) { throw null; }
        public static void InitializeArray(System.Array array, System.RuntimeFieldHandle fldHandle) { }
        public static bool IsReferenceOrContainsReferences<T>() { throw null; }
        public static void PrepareConstrainedRegions() { }
        public static void PrepareConstrainedRegionsNoOP() { }
        public static void PrepareContractedDelegate(System.Delegate d) { }
        public static void PrepareDelegate(System.Delegate d) { }
        public static void PrepareMethod(System.RuntimeMethodHandle method) { }
        public static void PrepareMethod(System.RuntimeMethodHandle method, System.RuntimeTypeHandle[] instantiation) { }
        public static void ProbeForSufficientStack() { }
        public static void RunClassConstructor(System.RuntimeTypeHandle type) { }
        public static void RunModuleConstructor(System.ModuleHandle module) { }
        public static bool TryEnsureSufficientExecutionStack() { throw null; }
        public delegate void CleanupCode(object userData, bool exceptionThrown);
        public delegate void TryCode(object userData);
    }
    public sealed partial class RuntimeWrappedException : System.Exception
    {
        public RuntimeWrappedException(object thrownObject) { }
        public object WrappedException { get { throw null; } }
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(972))]
    public sealed partial class SpecialNameAttribute : System.Attribute
    {
        public SpecialNameAttribute() { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(64), Inherited=false, AllowMultiple=false)]
    public partial class StateMachineAttribute : System.Attribute
    {
        public StateMachineAttribute(System.Type stateMachineType) { }
        public System.Type StateMachineType { get { throw null; } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(1), Inherited=false)]
    public sealed partial class StringFreezingAttribute : System.Attribute
    {
        public StringFreezingAttribute() { }
    }
    public partial class StrongBox<T> : System.Runtime.CompilerServices.IStrongBox
    {
        public T Value;
        public StrongBox() { }
        public StrongBox(T value) { }
        object System.Runtime.CompilerServices.IStrongBox.Value { get { throw null; } set { } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(3))]
    public sealed partial class SuppressIldasmAttribute : System.Attribute
    {
        public SuppressIldasmAttribute() { }
    }
    public readonly partial struct TaskAwaiter : System.Runtime.CompilerServices.ICriticalNotifyCompletion, System.Runtime.CompilerServices.INotifyCompletion
    {
        private readonly object _dummy;
        public bool IsCompleted { get { throw null; } }
        public void GetResult() { }
        public void OnCompleted(System.Action continuation) { }
        public void UnsafeOnCompleted(System.Action continuation) { }
    }
    public readonly partial struct TaskAwaiter<TResult> : System.Runtime.CompilerServices.ICriticalNotifyCompletion, System.Runtime.CompilerServices.INotifyCompletion
    {
        private readonly object _dummy;
        public bool IsCompleted { get { throw null; } }
        public TResult GetResult() { throw null; }
        public void OnCompleted(System.Action continuation) { }
        public void UnsafeOnCompleted(System.Action continuation) { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(11148))]
    [System.CLSCompliantAttribute(false)]
    public sealed partial class TupleElementNamesAttribute : System.Attribute
    {
        public TupleElementNamesAttribute(string[] transformNames) { }
        public System.Collections.Generic.IList<string> TransformNames { get { throw null; } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(5148), Inherited=false, AllowMultiple=false)]
    public sealed partial class TypeForwardedFromAttribute : System.Attribute
    {
        public TypeForwardedFromAttribute(string assemblyFullName) { }
        public string AssemblyFullName { get { throw null; } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(1), AllowMultiple=true, Inherited=false)]
    public sealed partial class TypeForwardedToAttribute : System.Attribute
    {
        public TypeForwardedToAttribute(System.Type destination) { }
        public System.Type Destination { get { throw null; } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(8))]
    public sealed partial class UnsafeValueTypeAttribute : System.Attribute
    {
        public UnsafeValueTypeAttribute() { }
    }
    public partial struct ValueTaskAwaiter<TResult> : System.Runtime.CompilerServices.ICriticalNotifyCompletion, System.Runtime.CompilerServices.INotifyCompletion
    {
        private object _dummy;
        public bool IsCompleted { get { throw null; } }
        public TResult GetResult() { throw null; }
        public void OnCompleted(System.Action continuation) { }
        public void UnsafeOnCompleted(System.Action continuation) { }
    }
    [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
    public readonly partial struct YieldAwaitable
    {
        public System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter GetAwaiter() { throw null; }
        public readonly partial struct YieldAwaiter : System.Runtime.CompilerServices.ICriticalNotifyCompletion, System.Runtime.CompilerServices.INotifyCompletion
        {
            public bool IsCompleted { get { throw null; } }
            public void GetResult() { }
            public void OnCompleted(System.Action continuation) { }
            public void UnsafeOnCompleted(System.Action continuation) { }
        }
    }
}
namespace System.Runtime.ConstrainedExecution
{
    public enum Cer
    {
        MayFail = 1,
        None = 0,
        Success = 2,
    }
    public enum Consistency
    {
        MayCorruptAppDomain = 1,
        MayCorruptInstance = 2,
        MayCorruptProcess = 0,
        WillNotCorruptState = 3,
    }
    public abstract partial class CriticalFinalizerObject
    {
        protected CriticalFinalizerObject() { }
        ~CriticalFinalizerObject() { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(96), Inherited=false)]
    public sealed partial class PrePrepareMethodAttribute : System.Attribute
    {
        public PrePrepareMethodAttribute() { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(1133), Inherited=false)]
    public sealed partial class ReliabilityContractAttribute : System.Attribute
    {
        public ReliabilityContractAttribute(System.Runtime.ConstrainedExecution.Consistency consistencyGuarantee, System.Runtime.ConstrainedExecution.Cer cer) { }
        public System.Runtime.ConstrainedExecution.Cer Cer { get { throw null; } }
        public System.Runtime.ConstrainedExecution.Consistency ConsistencyGuarantee { get { throw null; } }
    }
}
namespace System.Runtime.ExceptionServices
{
    public sealed partial class ExceptionDispatchInfo
    {
        internal ExceptionDispatchInfo() { }
        public System.Exception SourceException { get { throw null; } }
        public static System.Runtime.ExceptionServices.ExceptionDispatchInfo Capture(System.Exception source) { throw null; }
        public void Throw() { }
        public static void Throw(System.Exception source) { }
    }
    public partial class FirstChanceExceptionEventArgs : System.EventArgs
    {
        public FirstChanceExceptionEventArgs(System.Exception exception) { }
        public System.Exception Exception { get { throw null; } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(64), AllowMultiple=false, Inherited=false)]
    public sealed partial class HandleProcessCorruptedStateExceptionsAttribute : System.Attribute
    {
        public HandleProcessCorruptedStateExceptionsAttribute() { }
    }
}
namespace System.Runtime.InteropServices
{
    public enum CharSet
    {
        Ansi = 2,
        Auto = 4,
        None = 1,
        Unicode = 3,
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(5597), Inherited=false)]
    public sealed partial class ComVisibleAttribute : System.Attribute
    {
        public ComVisibleAttribute(bool visibility) { }
        public bool Value { get { throw null; } }
    }
    public abstract partial class CriticalHandle : System.Runtime.ConstrainedExecution.CriticalFinalizerObject, System.IDisposable
    {
        protected System.IntPtr handle;
        protected CriticalHandle(System.IntPtr invalidHandleValue) { }
        public bool IsClosed { get { throw null; } }
        public abstract bool IsInvalid { get; }
        public void Close() { }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        ~CriticalHandle() { }
        protected abstract bool ReleaseHandle();
        protected void SetHandle(System.IntPtr handle) { }
        public void SetHandleAsInvalid() { }
    }
    public partial class ExternalException : System.SystemException
    {
        public ExternalException() { }
        protected ExternalException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public ExternalException(string message) { }
        public ExternalException(string message, System.Exception inner) { }
        public ExternalException(string message, int errorCode) { }
        public virtual int ErrorCode { get { throw null; } }
        public override string ToString() { throw null; }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(256), Inherited=false)]
    public sealed partial class FieldOffsetAttribute : System.Attribute
    {
        public FieldOffsetAttribute(int offset) { }
        public int Value { get { throw null; } }
    }
    public partial struct GCHandle
    {
        private int _dummy;
        public bool IsAllocated { get { throw null; } }
        public object Target { get { throw null; } set { } }
        public System.IntPtr AddrOfPinnedObject() { throw null; }
        public static System.Runtime.InteropServices.GCHandle Alloc(object value) { throw null; }
        public static System.Runtime.InteropServices.GCHandle Alloc(object value, System.Runtime.InteropServices.GCHandleType type) { throw null; }
        public override bool Equals(object o) { throw null; }
        public void Free() { }
        public static System.Runtime.InteropServices.GCHandle FromIntPtr(System.IntPtr value) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Runtime.InteropServices.GCHandle a, System.Runtime.InteropServices.GCHandle b) { throw null; }
        public static explicit operator System.Runtime.InteropServices.GCHandle (System.IntPtr value) { throw null; }
        public static explicit operator System.IntPtr (System.Runtime.InteropServices.GCHandle value) { throw null; }
        public static bool operator !=(System.Runtime.InteropServices.GCHandle a, System.Runtime.InteropServices.GCHandle b) { throw null; }
        public static System.IntPtr ToIntPtr(System.Runtime.InteropServices.GCHandle value) { throw null; }
    }
    public enum GCHandleType
    {
        Normal = 2,
        Pinned = 3,
        Weak = 0,
        WeakTrackResurrection = 1,
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(2048), Inherited=false)]
    public sealed partial class InAttribute : System.Attribute
    {
        public InAttribute() { }
    }
    public enum LayoutKind
    {
        Auto = 3,
        Explicit = 2,
        Sequential = 0,
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(2048), Inherited=false)]
    public sealed partial class OutAttribute : System.Attribute
    {
        public OutAttribute() { }
    }
    public abstract partial class SafeHandle : System.Runtime.ConstrainedExecution.CriticalFinalizerObject, System.IDisposable
    {
        protected System.IntPtr handle;
        protected SafeHandle(System.IntPtr invalidHandleValue, bool ownsHandle) { }
        public bool IsClosed { get { throw null; } }
        public abstract bool IsInvalid { get; }
        public void Close() { }
        public void DangerousAddRef(ref bool success) { }
        public System.IntPtr DangerousGetHandle() { throw null; }
        public void DangerousRelease() { }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        ~SafeHandle() { }
        protected abstract bool ReleaseHandle();
        protected void SetHandle(System.IntPtr handle) { }
        public void SetHandleAsInvalid() { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(12), Inherited=false)]
    public sealed partial class StructLayoutAttribute : System.Attribute
    {
        public System.Runtime.InteropServices.CharSet CharSet;
        public int Pack;
        public int Size;
        public StructLayoutAttribute(short layoutKind) { }
        public StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind layoutKind) { }
        public System.Runtime.InteropServices.LayoutKind Value { get { throw null; } }
    }
}
namespace System.Runtime.Serialization
{
    public partial interface IDeserializationCallback
    {
        void OnDeserialization(object sender);
    }
    [System.CLSCompliantAttribute(false)]
    public partial interface IFormatterConverter
    {
        object Convert(object value, System.Type type);
        object Convert(object value, System.TypeCode typeCode);
        bool ToBoolean(object value);
        byte ToByte(object value);
        char ToChar(object value);
        System.DateTime ToDateTime(object value);
        decimal ToDecimal(object value);
        double ToDouble(object value);
        short ToInt16(object value);
        int ToInt32(object value);
        long ToInt64(object value);
        sbyte ToSByte(object value);
        float ToSingle(object value);
        string ToString(object value);
        ushort ToUInt16(object value);
        uint ToUInt32(object value);
        ulong ToUInt64(object value);
    }
    public partial interface IObjectReference
    {
        object GetRealObject(System.Runtime.Serialization.StreamingContext context);
    }
    public partial interface ISafeSerializationData
    {
        void CompleteDeserialization(object deserialized);
    }
    public partial interface ISerializable
    {
        void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context);
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(64), Inherited=false)]
    public sealed partial class OnDeserializedAttribute : System.Attribute
    {
        public OnDeserializedAttribute() { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(64), Inherited=false)]
    public sealed partial class OnDeserializingAttribute : System.Attribute
    {
        public OnDeserializingAttribute() { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(64), Inherited=false)]
    public sealed partial class OnSerializedAttribute : System.Attribute
    {
        public OnSerializedAttribute() { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(64), Inherited=false)]
    public sealed partial class OnSerializingAttribute : System.Attribute
    {
        public OnSerializingAttribute() { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(256), Inherited=false)]
    public sealed partial class OptionalFieldAttribute : System.Attribute
    {
        public OptionalFieldAttribute() { }
        public int VersionAdded { get { throw null; } set { } }
    }
    public sealed partial class SafeSerializationEventArgs : System.EventArgs
    {
        internal SafeSerializationEventArgs() { }
        public System.Runtime.Serialization.StreamingContext StreamingContext { get { throw null; } }
        public void AddSerializedState(System.Runtime.Serialization.ISafeSerializationData serializedState) { }
    }
    public partial struct SerializationEntry
    {
        private object _dummy;
        public string Name { get { throw null; } }
        public System.Type ObjectType { get { throw null; } }
        public object Value { get { throw null; } }
    }
    public partial class SerializationException : System.SystemException
    {
        public SerializationException() { }
        protected SerializationException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public SerializationException(string message) { }
        public SerializationException(string message, System.Exception innerException) { }
    }
    public sealed partial class SerializationInfo
    {
        [System.CLSCompliantAttribute(false)]
        public SerializationInfo(System.Type type, System.Runtime.Serialization.IFormatterConverter converter) { }
        [System.CLSCompliantAttribute(false)]
        public SerializationInfo(System.Type type, System.Runtime.Serialization.IFormatterConverter converter, bool requireSameTokenInPartialTrust) { }
        public string AssemblyName { get { throw null; } set { } }
        public string FullTypeName { get { throw null; } set { } }
        public bool IsAssemblyNameSetExplicit { get { throw null; } }
        public bool IsFullTypeNameSetExplicit { get { throw null; } }
        public int MemberCount { get { throw null; } }
        public System.Type ObjectType { get { throw null; } }
        public void AddValue(string name, bool value) { }
        public void AddValue(string name, byte value) { }
        public void AddValue(string name, char value) { }
        public void AddValue(string name, System.DateTime value) { }
        public void AddValue(string name, decimal value) { }
        public void AddValue(string name, double value) { }
        public void AddValue(string name, short value) { }
        public void AddValue(string name, int value) { }
        public void AddValue(string name, long value) { }
        public void AddValue(string name, object value) { }
        public void AddValue(string name, object value, System.Type type) { }
        [System.CLSCompliantAttribute(false)]
        public void AddValue(string name, sbyte value) { }
        public void AddValue(string name, float value) { }
        [System.CLSCompliantAttribute(false)]
        public void AddValue(string name, ushort value) { }
        [System.CLSCompliantAttribute(false)]
        public void AddValue(string name, uint value) { }
        [System.CLSCompliantAttribute(false)]
        public void AddValue(string name, ulong value) { }
        public bool GetBoolean(string name) { throw null; }
        public byte GetByte(string name) { throw null; }
        public char GetChar(string name) { throw null; }
        public System.DateTime GetDateTime(string name) { throw null; }
        public decimal GetDecimal(string name) { throw null; }
        public double GetDouble(string name) { throw null; }
        public System.Runtime.Serialization.SerializationInfoEnumerator GetEnumerator() { throw null; }
        public short GetInt16(string name) { throw null; }
        public int GetInt32(string name) { throw null; }
        public long GetInt64(string name) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public sbyte GetSByte(string name) { throw null; }
        public float GetSingle(string name) { throw null; }
        public string GetString(string name) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public ushort GetUInt16(string name) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public uint GetUInt32(string name) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public ulong GetUInt64(string name) { throw null; }
        public object GetValue(string name, System.Type type) { throw null; }
        public void SetType(System.Type type) { }
    }
    public sealed partial class SerializationInfoEnumerator : System.Collections.IEnumerator
    {
        internal SerializationInfoEnumerator() { }
        public System.Runtime.Serialization.SerializationEntry Current { get { throw null; } }
        public string Name { get { throw null; } }
        public System.Type ObjectType { get { throw null; } }
        object System.Collections.IEnumerator.Current { get { throw null; } }
        public object Value { get { throw null; } }
        public bool MoveNext() { throw null; }
        public void Reset() { }
    }
    public readonly partial struct StreamingContext
    {
        private readonly object _dummy;
        public StreamingContext(System.Runtime.Serialization.StreamingContextStates state) { throw null; }
        public StreamingContext(System.Runtime.Serialization.StreamingContextStates state, object additional) { throw null; }
        public object Context { get { throw null; } }
        public System.Runtime.Serialization.StreamingContextStates State { get { throw null; } }
        public override bool Equals(object obj) { throw null; }
        public override int GetHashCode() { throw null; }
    }
    [System.FlagsAttribute]
    public enum StreamingContextStates
    {
        All = 255,
        Clone = 64,
        CrossAppDomain = 128,
        CrossMachine = 2,
        CrossProcess = 1,
        File = 4,
        Other = 32,
        Persistence = 8,
        Remoting = 16,
    }
}
namespace System.Runtime.Versioning
{
    [System.AttributeUsageAttribute((System.AttributeTargets)(1), AllowMultiple=false, Inherited=false)]
    public sealed partial class TargetFrameworkAttribute : System.Attribute
    {
        public TargetFrameworkAttribute(string frameworkName) { }
        public string FrameworkDisplayName { get { throw null; } set { } }
        public string FrameworkName { get { throw null; } }
    }
}
namespace System.Security
{
    [System.AttributeUsageAttribute((System.AttributeTargets)(1), AllowMultiple=false, Inherited=false)]
    public sealed partial class AllowPartiallyTrustedCallersAttribute : System.Attribute
    {
        public AllowPartiallyTrustedCallersAttribute() { }
        public System.Security.PartialTrustVisibilityLevel PartialTrustVisibilityLevel { get { throw null; } set { } }
    }
    public enum PartialTrustVisibilityLevel
    {
        NotVisibleByDefault = 1,
        VisibleToAllHosts = 0,
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(5501), AllowMultiple=false, Inherited=false)]
    public sealed partial class SecurityCriticalAttribute : System.Attribute
    {
        public SecurityCriticalAttribute() { }
        public SecurityCriticalAttribute(System.Security.SecurityCriticalScope scope) { }
        [System.ObsoleteAttribute("SecurityCriticalScope is only used for .NET 2.0 transparency compatibility.")]
        public System.Security.SecurityCriticalScope Scope { get { throw null; } }
    }
    [System.ObsoleteAttribute("SecurityCriticalScope is only used for .NET 2.0 transparency compatibility.")]
    public enum SecurityCriticalScope
    {
        Everything = 1,
        Explicit = 0,
    }
    public partial class SecurityException : System.SystemException
    {
        public SecurityException() { }
        protected SecurityException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public SecurityException(string message) { }
        public SecurityException(string message, System.Exception inner) { }
        public SecurityException(string message, System.Type type) { }
        public SecurityException(string message, System.Type type, string state) { }
        public object Demanded { get { throw null; } set { } }
        public object DenySetInstance { get { throw null; } set { } }
        public System.Reflection.AssemblyName FailedAssemblyInfo { get { throw null; } set { } }
        public string GrantedSet { get { throw null; } set { } }
        public System.Reflection.MethodInfo Method { get { throw null; } set { } }
        public string PermissionState { get { throw null; } set { } }
        public System.Type PermissionType { get { throw null; } set { } }
        public object PermitOnlySetInstance { get { throw null; } set { } }
        public string RefusedSet { get { throw null; } set { } }
        public string Url { get { throw null; } set { } }
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public override string ToString() { throw null; }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(1), AllowMultiple=false)]
    public sealed partial class SecurityRulesAttribute : System.Attribute
    {
        public SecurityRulesAttribute(System.Security.SecurityRuleSet ruleSet) { }
        public System.Security.SecurityRuleSet RuleSet { get { throw null; } }
        public bool SkipVerificationInFullTrust { get { throw null; } set { } }
    }
    public enum SecurityRuleSet : byte
    {
        Level1 = (byte)1,
        Level2 = (byte)2,
        None = (byte)0,
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(5500), AllowMultiple=false, Inherited=false)]
    public sealed partial class SecuritySafeCriticalAttribute : System.Attribute
    {
        public SecuritySafeCriticalAttribute() { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(1), AllowMultiple=false, Inherited=false)]
    public sealed partial class SecurityTransparentAttribute : System.Attribute
    {
        public SecurityTransparentAttribute() { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(5501), AllowMultiple=false, Inherited=false)]
    [System.ObsoleteAttribute("SecurityTreatAsSafe is only used for .NET 2.0 transparency compatibility.  Please use the SecuritySafeCriticalAttribute instead.")]
    public sealed partial class SecurityTreatAsSafeAttribute : System.Attribute
    {
        public SecurityTreatAsSafeAttribute() { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(5188), AllowMultiple=true, Inherited=false)]
    public sealed partial class SuppressUnmanagedCodeSecurityAttribute : System.Attribute
    {
        public SuppressUnmanagedCodeSecurityAttribute() { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(2), AllowMultiple=true, Inherited=false)]
    public sealed partial class UnverifiableCodeAttribute : System.Attribute
    {
        public UnverifiableCodeAttribute() { }
    }
    public partial class VerificationException : System.SystemException
    {
        public VerificationException() { }
        protected VerificationException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public VerificationException(string message) { }
        public VerificationException(string message, System.Exception innerException) { }
    }
}
namespace System.Security.Cryptography
{
    public partial class CryptographicException : System.SystemException
    {
        public CryptographicException() { }
        public CryptographicException(int hr) { }
        protected CryptographicException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public CryptographicException(string message) { }
        public CryptographicException(string message, System.Exception inner) { }
        public CryptographicException(string format, string insert) { }
    }
}
namespace System.Text
{
    public abstract partial class Decoder
    {
        protected Decoder() { }
        public System.Text.DecoderFallback Fallback { get { throw null; } set { } }
        public System.Text.DecoderFallbackBuffer FallbackBuffer { get { throw null; } }
        [System.CLSCompliantAttribute(false)]
        public unsafe virtual void Convert(byte* bytes, int byteCount, char* chars, int charCount, bool flush, out int bytesUsed, out int charsUsed, out bool completed) { throw null; }
        public virtual void Convert(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex, int charCount, bool flush, out int bytesUsed, out int charsUsed, out bool completed) { throw null; }
        public virtual void Convert(System.ReadOnlySpan<byte> bytes, System.Span<char> chars, bool flush, out int bytesUsed, out int charsUsed, out bool completed) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public unsafe virtual int GetCharCount(byte* bytes, int count, bool flush) { throw null; }
        public abstract int GetCharCount(byte[] bytes, int index, int count);
        public virtual int GetCharCount(byte[] bytes, int index, int count, bool flush) { throw null; }
        public virtual int GetCharCount(System.ReadOnlySpan<byte> bytes, bool flush) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public unsafe virtual int GetChars(byte* bytes, int byteCount, char* chars, int charCount, bool flush) { throw null; }
        public abstract int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex);
        public virtual int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex, bool flush) { throw null; }
        public virtual int GetChars(System.ReadOnlySpan<byte> bytes, System.Span<char> chars, bool flush) { throw null; }
        public virtual void Reset() { }
    }
    public sealed partial class DecoderExceptionFallback : System.Text.DecoderFallback
    {
        public DecoderExceptionFallback() { }
        public override int MaxCharCount { get { throw null; } }
        public override System.Text.DecoderFallbackBuffer CreateFallbackBuffer() { throw null; }
        public override bool Equals(object value) { throw null; }
        public override int GetHashCode() { throw null; }
    }
    public sealed partial class DecoderExceptionFallbackBuffer : System.Text.DecoderFallbackBuffer
    {
        public DecoderExceptionFallbackBuffer() { }
        public override int Remaining { get { throw null; } }
        public override bool Fallback(byte[] bytesUnknown, int index) { throw null; }
        public override char GetNextChar() { throw null; }
        public override bool MovePrevious() { throw null; }
    }
    public abstract partial class DecoderFallback
    {
        protected DecoderFallback() { }
        public static System.Text.DecoderFallback ExceptionFallback { get { throw null; } }
        public abstract int MaxCharCount { get; }
        public static System.Text.DecoderFallback ReplacementFallback { get { throw null; } }
        public abstract System.Text.DecoderFallbackBuffer CreateFallbackBuffer();
    }
    public abstract partial class DecoderFallbackBuffer
    {
        protected DecoderFallbackBuffer() { }
        public abstract int Remaining { get; }
        public abstract bool Fallback(byte[] bytesUnknown, int index);
        public abstract char GetNextChar();
        public abstract bool MovePrevious();
        public virtual void Reset() { }
    }
    public sealed partial class DecoderFallbackException : System.ArgumentException
    {
        public DecoderFallbackException() { }
        public DecoderFallbackException(string message) { }
        public DecoderFallbackException(string message, byte[] bytesUnknown, int index) { }
        public DecoderFallbackException(string message, System.Exception innerException) { }
        public byte[] BytesUnknown { get { throw null; } }
        public int Index { get { throw null; } }
    }
    public sealed partial class DecoderReplacementFallback : System.Text.DecoderFallback
    {
        public DecoderReplacementFallback() { }
        public DecoderReplacementFallback(string replacement) { }
        public string DefaultString { get { throw null; } }
        public override int MaxCharCount { get { throw null; } }
        public override System.Text.DecoderFallbackBuffer CreateFallbackBuffer() { throw null; }
        public override bool Equals(object value) { throw null; }
        public override int GetHashCode() { throw null; }
    }
    public sealed partial class DecoderReplacementFallbackBuffer : System.Text.DecoderFallbackBuffer
    {
        public DecoderReplacementFallbackBuffer(System.Text.DecoderReplacementFallback fallback) { }
        public override int Remaining { get { throw null; } }
        public override bool Fallback(byte[] bytesUnknown, int index) { throw null; }
        public override char GetNextChar() { throw null; }
        public override bool MovePrevious() { throw null; }
        public override void Reset() { }
    }
    public abstract partial class Encoder
    {
        protected Encoder() { }
        public System.Text.EncoderFallback Fallback { get { throw null; } set { } }
        public System.Text.EncoderFallbackBuffer FallbackBuffer { get { throw null; } }
        [System.CLSCompliantAttribute(false)]
        public unsafe virtual void Convert(char* chars, int charCount, byte* bytes, int byteCount, bool flush, out int charsUsed, out int bytesUsed, out bool completed) { throw null; }
        public virtual void Convert(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex, int byteCount, bool flush, out int charsUsed, out int bytesUsed, out bool completed) { throw null; }
        public virtual void Convert(System.ReadOnlySpan<char> chars, System.Span<byte> bytes, bool flush, out int charsUsed, out int bytesUsed, out bool completed) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public unsafe virtual int GetByteCount(char* chars, int count, bool flush) { throw null; }
        public abstract int GetByteCount(char[] chars, int index, int count, bool flush);
        public virtual int GetByteCount(System.ReadOnlySpan<char> chars, bool flush) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public unsafe virtual int GetBytes(char* chars, int charCount, byte* bytes, int byteCount, bool flush) { throw null; }
        public abstract int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex, bool flush);
        public virtual int GetBytes(System.ReadOnlySpan<char> chars, System.Span<byte> bytes, bool flush) { throw null; }
        public virtual void Reset() { }
    }
    public sealed partial class EncoderExceptionFallback : System.Text.EncoderFallback
    {
        public EncoderExceptionFallback() { }
        public override int MaxCharCount { get { throw null; } }
        public override System.Text.EncoderFallbackBuffer CreateFallbackBuffer() { throw null; }
        public override bool Equals(object value) { throw null; }
        public override int GetHashCode() { throw null; }
    }
    public sealed partial class EncoderExceptionFallbackBuffer : System.Text.EncoderFallbackBuffer
    {
        public EncoderExceptionFallbackBuffer() { }
        public override int Remaining { get { throw null; } }
        public override bool Fallback(char charUnknownHigh, char charUnknownLow, int index) { throw null; }
        public override bool Fallback(char charUnknown, int index) { throw null; }
        public override char GetNextChar() { throw null; }
        public override bool MovePrevious() { throw null; }
    }
    public abstract partial class EncoderFallback
    {
        protected EncoderFallback() { }
        public static System.Text.EncoderFallback ExceptionFallback { get { throw null; } }
        public abstract int MaxCharCount { get; }
        public static System.Text.EncoderFallback ReplacementFallback { get { throw null; } }
        public abstract System.Text.EncoderFallbackBuffer CreateFallbackBuffer();
    }
    public abstract partial class EncoderFallbackBuffer
    {
        protected EncoderFallbackBuffer() { }
        public abstract int Remaining { get; }
        public abstract bool Fallback(char charUnknownHigh, char charUnknownLow, int index);
        public abstract bool Fallback(char charUnknown, int index);
        public abstract char GetNextChar();
        public abstract bool MovePrevious();
        public virtual void Reset() { }
    }
    public sealed partial class EncoderFallbackException : System.ArgumentException
    {
        public EncoderFallbackException() { }
        public EncoderFallbackException(string message) { }
        public EncoderFallbackException(string message, System.Exception innerException) { }
        public char CharUnknown { get { throw null; } }
        public char CharUnknownHigh { get { throw null; } }
        public char CharUnknownLow { get { throw null; } }
        public int Index { get { throw null; } }
        public bool IsUnknownSurrogate() { throw null; }
    }
    public sealed partial class EncoderReplacementFallback : System.Text.EncoderFallback
    {
        public EncoderReplacementFallback() { }
        public EncoderReplacementFallback(string replacement) { }
        public string DefaultString { get { throw null; } }
        public override int MaxCharCount { get { throw null; } }
        public override System.Text.EncoderFallbackBuffer CreateFallbackBuffer() { throw null; }
        public override bool Equals(object value) { throw null; }
        public override int GetHashCode() { throw null; }
    }
    public sealed partial class EncoderReplacementFallbackBuffer : System.Text.EncoderFallbackBuffer
    {
        public EncoderReplacementFallbackBuffer(System.Text.EncoderReplacementFallback fallback) { }
        public override int Remaining { get { throw null; } }
        public override bool Fallback(char charUnknownHigh, char charUnknownLow, int index) { throw null; }
        public override bool Fallback(char charUnknown, int index) { throw null; }
        public override char GetNextChar() { throw null; }
        public override bool MovePrevious() { throw null; }
        public override void Reset() { }
    }
    public abstract partial class Encoding : System.ICloneable
    {
        protected Encoding() { }
        protected Encoding(int codePage) { }
        protected Encoding(int codePage, System.Text.EncoderFallback encoderFallback, System.Text.DecoderFallback decoderFallback) { }
        public static System.Text.Encoding ASCII { get { throw null; } }
        public static System.Text.Encoding BigEndianUnicode { get { throw null; } }
        public virtual string BodyName { get { throw null; } }
        public virtual int CodePage { get { throw null; } }
        public System.Text.DecoderFallback DecoderFallback { get { throw null; } set { } }
        public static System.Text.Encoding Default { get { throw null; } }
        public System.Text.EncoderFallback EncoderFallback { get { throw null; } set { } }
        public virtual string EncodingName { get { throw null; } }
        public virtual string HeaderName { get { throw null; } }
        public virtual bool IsBrowserDisplay { get { throw null; } }
        public virtual bool IsBrowserSave { get { throw null; } }
        public virtual bool IsMailNewsDisplay { get { throw null; } }
        public virtual bool IsMailNewsSave { get { throw null; } }
        public bool IsReadOnly { get { throw null; } }
        public virtual bool IsSingleByte { get { throw null; } }
        public virtual System.ReadOnlySpan<byte> Preamble { get { throw null; } }
        public static System.Text.Encoding Unicode { get { throw null; } }
        public static System.Text.Encoding UTF32 { get { throw null; } }
        public static System.Text.Encoding UTF7 { get { throw null; } }
        public static System.Text.Encoding UTF8 { get { throw null; } }
        public virtual string WebName { get { throw null; } }
        public virtual int WindowsCodePage { get { throw null; } }
        public virtual object Clone() { throw null; }
        public static byte[] Convert(System.Text.Encoding srcEncoding, System.Text.Encoding dstEncoding, byte[] bytes) { throw null; }
        public static byte[] Convert(System.Text.Encoding srcEncoding, System.Text.Encoding dstEncoding, byte[] bytes, int index, int count) { throw null; }
        public override bool Equals(object value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public unsafe virtual int GetByteCount(char* chars, int count) { throw null; }
        public virtual int GetByteCount(char[] chars) { throw null; }
        public abstract int GetByteCount(char[] chars, int index, int count);
        public virtual int GetByteCount(System.ReadOnlySpan<char> chars) { throw null; }
        public virtual int GetByteCount(string s) { throw null; }
        public int GetByteCount(string s, int index, int count) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public unsafe virtual int GetBytes(char* chars, int charCount, byte* bytes, int byteCount) { throw null; }
        public virtual byte[] GetBytes(char[] chars) { throw null; }
        public virtual byte[] GetBytes(char[] chars, int index, int count) { throw null; }
        public abstract int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex);
        public virtual int GetBytes(System.ReadOnlySpan<char> chars, System.Span<byte> bytes) { throw null; }
        public virtual byte[] GetBytes(string s) { throw null; }
        public byte[] GetBytes(string s, int index, int count) { throw null; }
        public virtual int GetBytes(string s, int charIndex, int charCount, byte[] bytes, int byteIndex) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public unsafe virtual int GetCharCount(byte* bytes, int count) { throw null; }
        public virtual int GetCharCount(byte[] bytes) { throw null; }
        public abstract int GetCharCount(byte[] bytes, int index, int count);
        public virtual int GetCharCount(System.ReadOnlySpan<byte> bytes) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public unsafe virtual int GetChars(byte* bytes, int byteCount, char* chars, int charCount) { throw null; }
        public virtual char[] GetChars(byte[] bytes) { throw null; }
        public virtual char[] GetChars(byte[] bytes, int index, int count) { throw null; }
        public abstract int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex);
        public virtual int GetChars(System.ReadOnlySpan<byte> bytes, System.Span<char> chars) { throw null; }
        public virtual System.Text.Decoder GetDecoder() { throw null; }
        public virtual System.Text.Encoder GetEncoder() { throw null; }
        public static System.Text.Encoding GetEncoding(int codepage) { throw null; }
        public static System.Text.Encoding GetEncoding(int codepage, System.Text.EncoderFallback encoderFallback, System.Text.DecoderFallback decoderFallback) { throw null; }
        public static System.Text.Encoding GetEncoding(string name) { throw null; }
        public static System.Text.Encoding GetEncoding(string name, System.Text.EncoderFallback encoderFallback, System.Text.DecoderFallback decoderFallback) { throw null; }
        public static System.Text.EncodingInfo[] GetEncodings() { throw null; }
        public override int GetHashCode() { throw null; }
        public abstract int GetMaxByteCount(int charCount);
        public abstract int GetMaxCharCount(int byteCount);
        public virtual byte[] GetPreamble() { throw null; }
        [System.CLSCompliantAttribute(false)]
        public unsafe string GetString(byte* bytes, int byteCount) { throw null; }
        public virtual string GetString(byte[] bytes) { throw null; }
        public virtual string GetString(byte[] bytes, int index, int count) { throw null; }
        public string GetString(System.ReadOnlySpan<byte> bytes) { throw null; }
        public bool IsAlwaysNormalized() { throw null; }
        public virtual bool IsAlwaysNormalized(System.Text.NormalizationForm form) { throw null; }
        public static void RegisterProvider(System.Text.EncodingProvider provider) { }
    }
    public sealed partial class EncodingInfo
    {
        internal EncodingInfo() { }
        public int CodePage { get { throw null; } }
        public string DisplayName { get { throw null; } }
        public string Name { get { throw null; } }
        public override bool Equals(object value) { throw null; }
        public System.Text.Encoding GetEncoding() { throw null; }
        public override int GetHashCode() { throw null; }
    }
    public abstract partial class EncodingProvider
    {
        public EncodingProvider() { }
        public abstract System.Text.Encoding GetEncoding(int codepage);
        public virtual System.Text.Encoding GetEncoding(int codepage, System.Text.EncoderFallback encoderFallback, System.Text.DecoderFallback decoderFallback) { throw null; }
        public abstract System.Text.Encoding GetEncoding(string name);
        public virtual System.Text.Encoding GetEncoding(string name, System.Text.EncoderFallback encoderFallback, System.Text.DecoderFallback decoderFallback) { throw null; }
    }
    public enum NormalizationForm
    {
        FormC = 1,
        FormD = 2,
        FormKC = 5,
        FormKD = 6,
    }
    public sealed partial class StringBuilder : System.Runtime.Serialization.ISerializable
    {
        public StringBuilder() { }
        public StringBuilder(int capacity) { }
        public StringBuilder(int capacity, int maxCapacity) { }
        public StringBuilder(string value) { }
        public StringBuilder(string value, int capacity) { }
        public StringBuilder(string value, int startIndex, int length, int capacity) { }
        public int Capacity { get { throw null; } set { } }
        [System.Runtime.CompilerServices.IndexerName("Chars")]
        public char this[int index] { get { throw null; } set { } }
        public int Length { get { throw null; } set { } }
        public int MaxCapacity { get { throw null; } }
        public System.Text.StringBuilder Append(bool value) { throw null; }
        public System.Text.StringBuilder Append(byte value) { throw null; }
        public System.Text.StringBuilder Append(char value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public unsafe System.Text.StringBuilder Append(char* value, int valueCount) { throw null; }
        public System.Text.StringBuilder Append(char value, int repeatCount) { throw null; }
        public System.Text.StringBuilder Append(char[] value) { throw null; }
        public System.Text.StringBuilder Append(char[] value, int startIndex, int charCount) { throw null; }
        public System.Text.StringBuilder Append(decimal value) { throw null; }
        public System.Text.StringBuilder Append(double value) { throw null; }
        public System.Text.StringBuilder Append(short value) { throw null; }
        public System.Text.StringBuilder Append(int value) { throw null; }
        public System.Text.StringBuilder Append(long value) { throw null; }
        public System.Text.StringBuilder Append(object value) { throw null; }
        public System.Text.StringBuilder Append(System.ReadOnlySpan<char> value) { throw null; }
        public System.Text.StringBuilder Append(System.Text.StringBuilder value) { throw null; }
        public System.Text.StringBuilder Append(System.Text.StringBuilder value, int startIndex, int count) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public System.Text.StringBuilder Append(sbyte value) { throw null; }
        public System.Text.StringBuilder Append(float value) { throw null; }
        public System.Text.StringBuilder Append(string value) { throw null; }
        public System.Text.StringBuilder Append(string value, int startIndex, int count) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public System.Text.StringBuilder Append(ushort value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public System.Text.StringBuilder Append(uint value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public System.Text.StringBuilder Append(ulong value) { throw null; }
        public System.Text.StringBuilder AppendFormat(System.IFormatProvider provider, string format, object arg0) { throw null; }
        public System.Text.StringBuilder AppendFormat(System.IFormatProvider provider, string format, object arg0, object arg1) { throw null; }
        public System.Text.StringBuilder AppendFormat(System.IFormatProvider provider, string format, object arg0, object arg1, object arg2) { throw null; }
        public System.Text.StringBuilder AppendFormat(System.IFormatProvider provider, string format, params object[] args) { throw null; }
        public System.Text.StringBuilder AppendFormat(string format, object arg0) { throw null; }
        public System.Text.StringBuilder AppendFormat(string format, object arg0, object arg1) { throw null; }
        public System.Text.StringBuilder AppendFormat(string format, object arg0, object arg1, object arg2) { throw null; }
        public System.Text.StringBuilder AppendFormat(string format, params object[] args) { throw null; }
        public System.Text.StringBuilder AppendJoin(char separator, params object[] values) { throw null; }
        public System.Text.StringBuilder AppendJoin(char separator, params string[] values) { throw null; }
        public System.Text.StringBuilder AppendJoin(string separator, params object[] values) { throw null; }
        public System.Text.StringBuilder AppendJoin(string separator, params string[] values) { throw null; }
        public System.Text.StringBuilder AppendJoin<T>(char separator, System.Collections.Generic.IEnumerable<T> values) { throw null; }
        public System.Text.StringBuilder AppendJoin<T>(string separator, System.Collections.Generic.IEnumerable<T> values) { throw null; }
        public System.Text.StringBuilder AppendLine() { throw null; }
        public System.Text.StringBuilder AppendLine(string value) { throw null; }
        public System.Text.StringBuilder Clear() { throw null; }
        public void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count) { }
        public void CopyTo(int sourceIndex, System.Span<char> destination, int count) { }
        public int EnsureCapacity(int capacity) { throw null; }
        public bool Equals(System.Text.StringBuilder sb) { throw null; }
        public bool Equals(System.ReadOnlySpan<char> value) { throw null; }
        public System.Text.StringBuilder Insert(int index, bool value) { throw null; }
        public System.Text.StringBuilder Insert(int index, byte value) { throw null; }
        public System.Text.StringBuilder Insert(int index, char value) { throw null; }
        public System.Text.StringBuilder Insert(int index, char[] value) { throw null; }
        public System.Text.StringBuilder Insert(int index, char[] value, int startIndex, int charCount) { throw null; }
        public System.Text.StringBuilder Insert(int index, decimal value) { throw null; }
        public System.Text.StringBuilder Insert(int index, double value) { throw null; }
        public System.Text.StringBuilder Insert(int index, short value) { throw null; }
        public System.Text.StringBuilder Insert(int index, int value) { throw null; }
        public System.Text.StringBuilder Insert(int index, long value) { throw null; }
        public System.Text.StringBuilder Insert(int index, object value) { throw null; }
        public System.Text.StringBuilder Insert(int index, System.ReadOnlySpan<char> value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public System.Text.StringBuilder Insert(int index, sbyte value) { throw null; }
        public System.Text.StringBuilder Insert(int index, float value) { throw null; }
        public System.Text.StringBuilder Insert(int index, string value) { throw null; }
        public System.Text.StringBuilder Insert(int index, string value, int count) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public System.Text.StringBuilder Insert(int index, ushort value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public System.Text.StringBuilder Insert(int index, uint value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public System.Text.StringBuilder Insert(int index, ulong value) { throw null; }
        public System.Text.StringBuilder Remove(int startIndex, int length) { throw null; }
        public System.Text.StringBuilder Replace(char oldChar, char newChar) { throw null; }
        public System.Text.StringBuilder Replace(char oldChar, char newChar, int startIndex, int count) { throw null; }
        public System.Text.StringBuilder Replace(string oldValue, string newValue) { throw null; }
        public System.Text.StringBuilder Replace(string oldValue, string newValue, int startIndex, int count) { throw null; }
        void System.Runtime.Serialization.ISerializable.GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public override string ToString() { throw null; }
        public string ToString(int startIndex, int length) { throw null; }
    }
}
namespace System.Threading
{
    public readonly partial struct CancellationToken
    {
        private readonly object _dummy;
        public CancellationToken(bool canceled) { throw null; }
        public bool CanBeCanceled { get { throw null; } }
        public bool IsCancellationRequested { get { throw null; } }
        public static System.Threading.CancellationToken None { get { throw null; } }
        public System.Threading.WaitHandle WaitHandle { get { throw null; } }
        public override bool Equals(object other) { throw null; }
        public bool Equals(System.Threading.CancellationToken other) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Threading.CancellationToken left, System.Threading.CancellationToken right) { throw null; }
        public static bool operator !=(System.Threading.CancellationToken left, System.Threading.CancellationToken right) { throw null; }
        public System.Threading.CancellationTokenRegistration Register(System.Action callback) { throw null; }
        public System.Threading.CancellationTokenRegistration Register(System.Action callback, bool useSynchronizationContext) { throw null; }
        public System.Threading.CancellationTokenRegistration Register(System.Action<object> callback, object state) { throw null; }
        public System.Threading.CancellationTokenRegistration Register(System.Action<object> callback, object state, bool useSynchronizationContext) { throw null; }
        public void ThrowIfCancellationRequested() { }
    }
    public readonly partial struct CancellationTokenRegistration : System.IDisposable, System.IEquatable<System.Threading.CancellationTokenRegistration>
    {
        private readonly object _dummy;
        public System.Threading.CancellationToken Token { get { throw null; } }
        public void Dispose() { }
        public override bool Equals(object obj) { throw null; }
        public bool Equals(System.Threading.CancellationTokenRegistration other) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Threading.CancellationTokenRegistration left, System.Threading.CancellationTokenRegistration right) { throw null; }
        public static bool operator !=(System.Threading.CancellationTokenRegistration left, System.Threading.CancellationTokenRegistration right) { throw null; }
    }
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
    public abstract partial class WaitHandle : System.MarshalByRefObject, System.IDisposable
    {
        protected static readonly System.IntPtr InvalidHandle;
        public const int WaitTimeout = 258;
        protected WaitHandle() { }
        [System.ObsoleteAttribute("Use the SafeWaitHandle property instead.")]
        public virtual System.IntPtr Handle { get { throw null; } set { } }
        public Microsoft.Win32.SafeHandles.SafeWaitHandle SafeWaitHandle { get { throw null; } set { } }
        public virtual void Close() { }
        public void Dispose() { }
        protected virtual void Dispose(bool explicitDisposing) { }
        public static bool SignalAndWait(System.Threading.WaitHandle toSignal, System.Threading.WaitHandle toWaitOn) { throw null; }
        public static bool SignalAndWait(System.Threading.WaitHandle toSignal, System.Threading.WaitHandle toWaitOn, int millisecondsTimeout, bool exitContext) { throw null; }
        public static bool SignalAndWait(System.Threading.WaitHandle toSignal, System.Threading.WaitHandle toWaitOn, System.TimeSpan timeout, bool exitContext) { throw null; }
        public static bool WaitAll(System.Threading.WaitHandle[] waitHandles) { throw null; }
        public static bool WaitAll(System.Threading.WaitHandle[] waitHandles, int millisecondsTimeout) { throw null; }
        public static bool WaitAll(System.Threading.WaitHandle[] waitHandles, int millisecondsTimeout, bool exitContext) { throw null; }
        public static bool WaitAll(System.Threading.WaitHandle[] waitHandles, System.TimeSpan timeout) { throw null; }
        public static bool WaitAll(System.Threading.WaitHandle[] waitHandles, System.TimeSpan timeout, bool exitContext) { throw null; }
        public static int WaitAny(System.Threading.WaitHandle[] waitHandles) { throw null; }
        public static int WaitAny(System.Threading.WaitHandle[] waitHandles, int millisecondsTimeout) { throw null; }
        public static int WaitAny(System.Threading.WaitHandle[] waitHandles, int millisecondsTimeout, bool exitContext) { throw null; }
        public static int WaitAny(System.Threading.WaitHandle[] waitHandles, System.TimeSpan timeout) { throw null; }
        public static int WaitAny(System.Threading.WaitHandle[] waitHandles, System.TimeSpan timeout, bool exitContext) { throw null; }
        public virtual bool WaitOne() { throw null; }
        public virtual bool WaitOne(int millisecondsTimeout) { throw null; }
        public virtual bool WaitOne(int millisecondsTimeout, bool exitContext) { throw null; }
        public virtual bool WaitOne(System.TimeSpan timeout) { throw null; }
        public virtual bool WaitOne(System.TimeSpan timeout, bool exitContext) { throw null; }
    }
    public static partial class WaitHandleExtensions
    {
        public static Microsoft.Win32.SafeHandles.SafeWaitHandle GetSafeWaitHandle(this System.Threading.WaitHandle waitHandle) { throw null; }
        public static void SetSafeWaitHandle(this System.Threading.WaitHandle waitHandle, Microsoft.Win32.SafeHandles.SafeWaitHandle value) { }
    }
}
namespace System.Threading.Tasks
{
    public partial class Task : System.IAsyncResult, System.IDisposable
    {
        public Task(System.Action action) { }
        public Task(System.Action action, System.Threading.CancellationToken cancellationToken) { }
        public Task(System.Action action, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskCreationOptions creationOptions) { }
        public Task(System.Action action, System.Threading.Tasks.TaskCreationOptions creationOptions) { }
        public Task(System.Action<object> action, object state) { }
        public Task(System.Action<object> action, object state, System.Threading.CancellationToken cancellationToken) { }
        public Task(System.Action<object> action, object state, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskCreationOptions creationOptions) { }
        public Task(System.Action<object> action, object state, System.Threading.Tasks.TaskCreationOptions creationOptions) { }
        public object AsyncState { get { throw null; } }
        public static System.Threading.Tasks.Task CompletedTask { get { throw null; } }
        public System.Threading.Tasks.TaskCreationOptions CreationOptions { get { throw null; } }
        public static System.Nullable<int> CurrentId { get { throw null; } }
        public System.AggregateException Exception { get { throw null; } }
        public static System.Threading.Tasks.TaskFactory Factory { get { throw null; } }
        public int Id { get { throw null; } }
        public bool IsCanceled { get { throw null; } }
        public bool IsCompleted { get { throw null; } }
        public bool IsCompletedSuccessfully { get { throw null; } }
        public bool IsFaulted { get { throw null; } }
        public System.Threading.Tasks.TaskStatus Status { get { throw null; } }
        System.Threading.WaitHandle System.IAsyncResult.AsyncWaitHandle { get { throw null; } }
        bool System.IAsyncResult.CompletedSynchronously { get { throw null; } }
        public System.Runtime.CompilerServices.ConfiguredTaskAwaitable ConfigureAwait(bool continueOnCapturedContext) { throw null; }
        public System.Threading.Tasks.Task ContinueWith(System.Action<System.Threading.Tasks.Task, object> continuationAction, object state) { throw null; }
        public System.Threading.Tasks.Task ContinueWith(System.Action<System.Threading.Tasks.Task, object> continuationAction, object state, System.Threading.CancellationToken cancellationToken) { throw null; }
        public System.Threading.Tasks.Task ContinueWith(System.Action<System.Threading.Tasks.Task, object> continuationAction, object state, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskContinuationOptions continuationOptions, System.Threading.Tasks.TaskScheduler scheduler) { throw null; }
        public System.Threading.Tasks.Task ContinueWith(System.Action<System.Threading.Tasks.Task, object> continuationAction, object state, System.Threading.Tasks.TaskContinuationOptions continuationOptions) { throw null; }
        public System.Threading.Tasks.Task ContinueWith(System.Action<System.Threading.Tasks.Task, object> continuationAction, object state, System.Threading.Tasks.TaskScheduler scheduler) { throw null; }
        public System.Threading.Tasks.Task ContinueWith(System.Action<System.Threading.Tasks.Task> continuationAction) { throw null; }
        public System.Threading.Tasks.Task ContinueWith(System.Action<System.Threading.Tasks.Task> continuationAction, System.Threading.CancellationToken cancellationToken) { throw null; }
        public System.Threading.Tasks.Task ContinueWith(System.Action<System.Threading.Tasks.Task> continuationAction, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskContinuationOptions continuationOptions, System.Threading.Tasks.TaskScheduler scheduler) { throw null; }
        public System.Threading.Tasks.Task ContinueWith(System.Action<System.Threading.Tasks.Task> continuationAction, System.Threading.Tasks.TaskContinuationOptions continuationOptions) { throw null; }
        public System.Threading.Tasks.Task ContinueWith(System.Action<System.Threading.Tasks.Task> continuationAction, System.Threading.Tasks.TaskScheduler scheduler) { throw null; }
        public System.Threading.Tasks.Task<TResult> ContinueWith<TResult>(System.Func<System.Threading.Tasks.Task, object, TResult> continuationFunction, object state) { throw null; }
        public System.Threading.Tasks.Task<TResult> ContinueWith<TResult>(System.Func<System.Threading.Tasks.Task, object, TResult> continuationFunction, object state, System.Threading.CancellationToken cancellationToken) { throw null; }
        public System.Threading.Tasks.Task<TResult> ContinueWith<TResult>(System.Func<System.Threading.Tasks.Task, object, TResult> continuationFunction, object state, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskContinuationOptions continuationOptions, System.Threading.Tasks.TaskScheduler scheduler) { throw null; }
        public System.Threading.Tasks.Task<TResult> ContinueWith<TResult>(System.Func<System.Threading.Tasks.Task, object, TResult> continuationFunction, object state, System.Threading.Tasks.TaskContinuationOptions continuationOptions) { throw null; }
        public System.Threading.Tasks.Task<TResult> ContinueWith<TResult>(System.Func<System.Threading.Tasks.Task, object, TResult> continuationFunction, object state, System.Threading.Tasks.TaskScheduler scheduler) { throw null; }
        public System.Threading.Tasks.Task<TResult> ContinueWith<TResult>(System.Func<System.Threading.Tasks.Task, TResult> continuationFunction) { throw null; }
        public System.Threading.Tasks.Task<TResult> ContinueWith<TResult>(System.Func<System.Threading.Tasks.Task, TResult> continuationFunction, System.Threading.CancellationToken cancellationToken) { throw null; }
        public System.Threading.Tasks.Task<TResult> ContinueWith<TResult>(System.Func<System.Threading.Tasks.Task, TResult> continuationFunction, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskContinuationOptions continuationOptions, System.Threading.Tasks.TaskScheduler scheduler) { throw null; }
        public System.Threading.Tasks.Task<TResult> ContinueWith<TResult>(System.Func<System.Threading.Tasks.Task, TResult> continuationFunction, System.Threading.Tasks.TaskContinuationOptions continuationOptions) { throw null; }
        public System.Threading.Tasks.Task<TResult> ContinueWith<TResult>(System.Func<System.Threading.Tasks.Task, TResult> continuationFunction, System.Threading.Tasks.TaskScheduler scheduler) { throw null; }
        public static System.Threading.Tasks.Task Delay(int millisecondsDelay) { throw null; }
        public static System.Threading.Tasks.Task Delay(int millisecondsDelay, System.Threading.CancellationToken cancellationToken) { throw null; }
        public static System.Threading.Tasks.Task Delay(System.TimeSpan delay) { throw null; }
        public static System.Threading.Tasks.Task Delay(System.TimeSpan delay, System.Threading.CancellationToken cancellationToken) { throw null; }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        public static System.Threading.Tasks.Task FromCanceled(System.Threading.CancellationToken cancellationToken) { throw null; }
        public static System.Threading.Tasks.Task<TResult> FromCanceled<TResult>(System.Threading.CancellationToken cancellationToken) { throw null; }
        public static System.Threading.Tasks.Task FromException(System.Exception exception) { throw null; }
        public static System.Threading.Tasks.Task<TResult> FromException<TResult>(System.Exception exception) { throw null; }
        public static System.Threading.Tasks.Task<TResult> FromResult<TResult>(TResult result) { throw null; }
        public System.Runtime.CompilerServices.TaskAwaiter GetAwaiter() { throw null; }
        public static System.Threading.Tasks.Task Run(System.Action action) { throw null; }
        public static System.Threading.Tasks.Task Run(System.Action action, System.Threading.CancellationToken cancellationToken) { throw null; }
        public static System.Threading.Tasks.Task Run(System.Func<System.Threading.Tasks.Task> function) { throw null; }
        public static System.Threading.Tasks.Task Run(System.Func<System.Threading.Tasks.Task> function, System.Threading.CancellationToken cancellationToken) { throw null; }
        public void RunSynchronously() { }
        public void RunSynchronously(System.Threading.Tasks.TaskScheduler scheduler) { }
        public static System.Threading.Tasks.Task<TResult> Run<TResult>(System.Func<System.Threading.Tasks.Task<TResult>> function) { throw null; }
        public static System.Threading.Tasks.Task<TResult> Run<TResult>(System.Func<System.Threading.Tasks.Task<TResult>> function, System.Threading.CancellationToken cancellationToken) { throw null; }
        public static System.Threading.Tasks.Task<TResult> Run<TResult>(System.Func<TResult> function) { throw null; }
        public static System.Threading.Tasks.Task<TResult> Run<TResult>(System.Func<TResult> function, System.Threading.CancellationToken cancellationToken) { throw null; }
        public void Start() { }
        public void Start(System.Threading.Tasks.TaskScheduler scheduler) { }
        public void Wait() { }
        public bool Wait(int millisecondsTimeout) { throw null; }
        public bool Wait(int millisecondsTimeout, System.Threading.CancellationToken cancellationToken) { throw null; }
        public void Wait(System.Threading.CancellationToken cancellationToken) { }
        public bool Wait(System.TimeSpan timeout) { throw null; }
        public static void WaitAll(params System.Threading.Tasks.Task[] tasks) { }
        public static bool WaitAll(System.Threading.Tasks.Task[] tasks, int millisecondsTimeout) { throw null; }
        public static bool WaitAll(System.Threading.Tasks.Task[] tasks, int millisecondsTimeout, System.Threading.CancellationToken cancellationToken) { throw null; }
        public static void WaitAll(System.Threading.Tasks.Task[] tasks, System.Threading.CancellationToken cancellationToken) { }
        public static bool WaitAll(System.Threading.Tasks.Task[] tasks, System.TimeSpan timeout) { throw null; }
        public static int WaitAny(params System.Threading.Tasks.Task[] tasks) { throw null; }
        public static int WaitAny(System.Threading.Tasks.Task[] tasks, int millisecondsTimeout) { throw null; }
        public static int WaitAny(System.Threading.Tasks.Task[] tasks, int millisecondsTimeout, System.Threading.CancellationToken cancellationToken) { throw null; }
        public static int WaitAny(System.Threading.Tasks.Task[] tasks, System.Threading.CancellationToken cancellationToken) { throw null; }
        public static int WaitAny(System.Threading.Tasks.Task[] tasks, System.TimeSpan timeout) { throw null; }
        public static System.Threading.Tasks.Task WhenAll(System.Collections.Generic.IEnumerable<System.Threading.Tasks.Task> tasks) { throw null; }
        public static System.Threading.Tasks.Task WhenAll(params System.Threading.Tasks.Task[] tasks) { throw null; }
        public static System.Threading.Tasks.Task<TResult[]> WhenAll<TResult>(System.Collections.Generic.IEnumerable<System.Threading.Tasks.Task<TResult>> tasks) { throw null; }
        public static System.Threading.Tasks.Task<TResult[]> WhenAll<TResult>(params System.Threading.Tasks.Task<TResult>[] tasks) { throw null; }
        public static System.Threading.Tasks.Task<System.Threading.Tasks.Task> WhenAny(System.Collections.Generic.IEnumerable<System.Threading.Tasks.Task> tasks) { throw null; }
        public static System.Threading.Tasks.Task<System.Threading.Tasks.Task> WhenAny(params System.Threading.Tasks.Task[] tasks) { throw null; }
        public static System.Threading.Tasks.Task<System.Threading.Tasks.Task<TResult>> WhenAny<TResult>(System.Collections.Generic.IEnumerable<System.Threading.Tasks.Task<TResult>> tasks) { throw null; }
        public static System.Threading.Tasks.Task<System.Threading.Tasks.Task<TResult>> WhenAny<TResult>(params System.Threading.Tasks.Task<TResult>[] tasks) { throw null; }
        public static System.Runtime.CompilerServices.YieldAwaitable Yield() { throw null; }
    }
    [System.FlagsAttribute]
    public enum TaskContinuationOptions
    {
        AttachedToParent = 4,
        DenyChildAttach = 8,
        ExecuteSynchronously = 524288,
        HideScheduler = 16,
        LazyCancellation = 32,
        LongRunning = 2,
        None = 0,
        NotOnCanceled = 262144,
        NotOnFaulted = 131072,
        NotOnRanToCompletion = 65536,
        OnlyOnCanceled = 196608,
        OnlyOnFaulted = 327680,
        OnlyOnRanToCompletion = 393216,
        PreferFairness = 1,
        RunContinuationsAsynchronously = 64,
    }
    [System.FlagsAttribute]
    public enum TaskCreationOptions
    {
        AttachedToParent = 4,
        DenyChildAttach = 8,
        HideScheduler = 16,
        LongRunning = 2,
        None = 0,
        PreferFairness = 1,
        RunContinuationsAsynchronously = 64,
    }
    public partial class TaskFactory
    {
        public TaskFactory() { }
        public TaskFactory(System.Threading.CancellationToken cancellationToken) { }
        public TaskFactory(System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskCreationOptions creationOptions, System.Threading.Tasks.TaskContinuationOptions continuationOptions, System.Threading.Tasks.TaskScheduler scheduler) { }
        public TaskFactory(System.Threading.Tasks.TaskCreationOptions creationOptions, System.Threading.Tasks.TaskContinuationOptions continuationOptions) { }
        public TaskFactory(System.Threading.Tasks.TaskScheduler scheduler) { }
        public System.Threading.CancellationToken CancellationToken { get { throw null; } }
        public System.Threading.Tasks.TaskContinuationOptions ContinuationOptions { get { throw null; } }
        public System.Threading.Tasks.TaskCreationOptions CreationOptions { get { throw null; } }
        public System.Threading.Tasks.TaskScheduler Scheduler { get { throw null; } }
        public System.Threading.Tasks.Task ContinueWhenAll(System.Threading.Tasks.Task[] tasks, System.Action<System.Threading.Tasks.Task[]> continuationAction) { throw null; }
        public System.Threading.Tasks.Task ContinueWhenAll(System.Threading.Tasks.Task[] tasks, System.Action<System.Threading.Tasks.Task[]> continuationAction, System.Threading.CancellationToken cancellationToken) { throw null; }
        public System.Threading.Tasks.Task ContinueWhenAll(System.Threading.Tasks.Task[] tasks, System.Action<System.Threading.Tasks.Task[]> continuationAction, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskContinuationOptions continuationOptions, System.Threading.Tasks.TaskScheduler scheduler) { throw null; }
        public System.Threading.Tasks.Task ContinueWhenAll(System.Threading.Tasks.Task[] tasks, System.Action<System.Threading.Tasks.Task[]> continuationAction, System.Threading.Tasks.TaskContinuationOptions continuationOptions) { throw null; }
        public System.Threading.Tasks.Task<TResult> ContinueWhenAll<TResult>(System.Threading.Tasks.Task[] tasks, System.Func<System.Threading.Tasks.Task[], TResult> continuationFunction) { throw null; }
        public System.Threading.Tasks.Task<TResult> ContinueWhenAll<TResult>(System.Threading.Tasks.Task[] tasks, System.Func<System.Threading.Tasks.Task[], TResult> continuationFunction, System.Threading.CancellationToken cancellationToken) { throw null; }
        public System.Threading.Tasks.Task<TResult> ContinueWhenAll<TResult>(System.Threading.Tasks.Task[] tasks, System.Func<System.Threading.Tasks.Task[], TResult> continuationFunction, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskContinuationOptions continuationOptions, System.Threading.Tasks.TaskScheduler scheduler) { throw null; }
        public System.Threading.Tasks.Task<TResult> ContinueWhenAll<TResult>(System.Threading.Tasks.Task[] tasks, System.Func<System.Threading.Tasks.Task[], TResult> continuationFunction, System.Threading.Tasks.TaskContinuationOptions continuationOptions) { throw null; }
        public System.Threading.Tasks.Task ContinueWhenAll<TAntecedentResult>(System.Threading.Tasks.Task<TAntecedentResult>[] tasks, System.Action<System.Threading.Tasks.Task<TAntecedentResult>[]> continuationAction) { throw null; }
        public System.Threading.Tasks.Task ContinueWhenAll<TAntecedentResult>(System.Threading.Tasks.Task<TAntecedentResult>[] tasks, System.Action<System.Threading.Tasks.Task<TAntecedentResult>[]> continuationAction, System.Threading.CancellationToken cancellationToken) { throw null; }
        public System.Threading.Tasks.Task ContinueWhenAll<TAntecedentResult>(System.Threading.Tasks.Task<TAntecedentResult>[] tasks, System.Action<System.Threading.Tasks.Task<TAntecedentResult>[]> continuationAction, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskContinuationOptions continuationOptions, System.Threading.Tasks.TaskScheduler scheduler) { throw null; }
        public System.Threading.Tasks.Task ContinueWhenAll<TAntecedentResult>(System.Threading.Tasks.Task<TAntecedentResult>[] tasks, System.Action<System.Threading.Tasks.Task<TAntecedentResult>[]> continuationAction, System.Threading.Tasks.TaskContinuationOptions continuationOptions) { throw null; }
        public System.Threading.Tasks.Task<TResult> ContinueWhenAll<TAntecedentResult, TResult>(System.Threading.Tasks.Task<TAntecedentResult>[] tasks, System.Func<System.Threading.Tasks.Task<TAntecedentResult>[], TResult> continuationFunction) { throw null; }
        public System.Threading.Tasks.Task<TResult> ContinueWhenAll<TAntecedentResult, TResult>(System.Threading.Tasks.Task<TAntecedentResult>[] tasks, System.Func<System.Threading.Tasks.Task<TAntecedentResult>[], TResult> continuationFunction, System.Threading.CancellationToken cancellationToken) { throw null; }
        public System.Threading.Tasks.Task<TResult> ContinueWhenAll<TAntecedentResult, TResult>(System.Threading.Tasks.Task<TAntecedentResult>[] tasks, System.Func<System.Threading.Tasks.Task<TAntecedentResult>[], TResult> continuationFunction, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskContinuationOptions continuationOptions, System.Threading.Tasks.TaskScheduler scheduler) { throw null; }
        public System.Threading.Tasks.Task<TResult> ContinueWhenAll<TAntecedentResult, TResult>(System.Threading.Tasks.Task<TAntecedentResult>[] tasks, System.Func<System.Threading.Tasks.Task<TAntecedentResult>[], TResult> continuationFunction, System.Threading.Tasks.TaskContinuationOptions continuationOptions) { throw null; }
        public System.Threading.Tasks.Task ContinueWhenAny(System.Threading.Tasks.Task[] tasks, System.Action<System.Threading.Tasks.Task> continuationAction) { throw null; }
        public System.Threading.Tasks.Task ContinueWhenAny(System.Threading.Tasks.Task[] tasks, System.Action<System.Threading.Tasks.Task> continuationAction, System.Threading.CancellationToken cancellationToken) { throw null; }
        public System.Threading.Tasks.Task ContinueWhenAny(System.Threading.Tasks.Task[] tasks, System.Action<System.Threading.Tasks.Task> continuationAction, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskContinuationOptions continuationOptions, System.Threading.Tasks.TaskScheduler scheduler) { throw null; }
        public System.Threading.Tasks.Task ContinueWhenAny(System.Threading.Tasks.Task[] tasks, System.Action<System.Threading.Tasks.Task> continuationAction, System.Threading.Tasks.TaskContinuationOptions continuationOptions) { throw null; }
        public System.Threading.Tasks.Task<TResult> ContinueWhenAny<TResult>(System.Threading.Tasks.Task[] tasks, System.Func<System.Threading.Tasks.Task, TResult> continuationFunction) { throw null; }
        public System.Threading.Tasks.Task<TResult> ContinueWhenAny<TResult>(System.Threading.Tasks.Task[] tasks, System.Func<System.Threading.Tasks.Task, TResult> continuationFunction, System.Threading.CancellationToken cancellationToken) { throw null; }
        public System.Threading.Tasks.Task<TResult> ContinueWhenAny<TResult>(System.Threading.Tasks.Task[] tasks, System.Func<System.Threading.Tasks.Task, TResult> continuationFunction, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskContinuationOptions continuationOptions, System.Threading.Tasks.TaskScheduler scheduler) { throw null; }
        public System.Threading.Tasks.Task<TResult> ContinueWhenAny<TResult>(System.Threading.Tasks.Task[] tasks, System.Func<System.Threading.Tasks.Task, TResult> continuationFunction, System.Threading.Tasks.TaskContinuationOptions continuationOptions) { throw null; }
        public System.Threading.Tasks.Task ContinueWhenAny<TAntecedentResult>(System.Threading.Tasks.Task<TAntecedentResult>[] tasks, System.Action<System.Threading.Tasks.Task<TAntecedentResult>> continuationAction) { throw null; }
        public System.Threading.Tasks.Task ContinueWhenAny<TAntecedentResult>(System.Threading.Tasks.Task<TAntecedentResult>[] tasks, System.Action<System.Threading.Tasks.Task<TAntecedentResult>> continuationAction, System.Threading.CancellationToken cancellationToken) { throw null; }
        public System.Threading.Tasks.Task ContinueWhenAny<TAntecedentResult>(System.Threading.Tasks.Task<TAntecedentResult>[] tasks, System.Action<System.Threading.Tasks.Task<TAntecedentResult>> continuationAction, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskContinuationOptions continuationOptions, System.Threading.Tasks.TaskScheduler scheduler) { throw null; }
        public System.Threading.Tasks.Task ContinueWhenAny<TAntecedentResult>(System.Threading.Tasks.Task<TAntecedentResult>[] tasks, System.Action<System.Threading.Tasks.Task<TAntecedentResult>> continuationAction, System.Threading.Tasks.TaskContinuationOptions continuationOptions) { throw null; }
        public System.Threading.Tasks.Task<TResult> ContinueWhenAny<TAntecedentResult, TResult>(System.Threading.Tasks.Task<TAntecedentResult>[] tasks, System.Func<System.Threading.Tasks.Task<TAntecedentResult>, TResult> continuationFunction) { throw null; }
        public System.Threading.Tasks.Task<TResult> ContinueWhenAny<TAntecedentResult, TResult>(System.Threading.Tasks.Task<TAntecedentResult>[] tasks, System.Func<System.Threading.Tasks.Task<TAntecedentResult>, TResult> continuationFunction, System.Threading.CancellationToken cancellationToken) { throw null; }
        public System.Threading.Tasks.Task<TResult> ContinueWhenAny<TAntecedentResult, TResult>(System.Threading.Tasks.Task<TAntecedentResult>[] tasks, System.Func<System.Threading.Tasks.Task<TAntecedentResult>, TResult> continuationFunction, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskContinuationOptions continuationOptions, System.Threading.Tasks.TaskScheduler scheduler) { throw null; }
        public System.Threading.Tasks.Task<TResult> ContinueWhenAny<TAntecedentResult, TResult>(System.Threading.Tasks.Task<TAntecedentResult>[] tasks, System.Func<System.Threading.Tasks.Task<TAntecedentResult>, TResult> continuationFunction, System.Threading.Tasks.TaskContinuationOptions continuationOptions) { throw null; }
        public System.Threading.Tasks.Task FromAsync(System.Func<System.AsyncCallback, object, System.IAsyncResult> beginMethod, System.Action<System.IAsyncResult> endMethod, object state) { throw null; }
        public System.Threading.Tasks.Task FromAsync(System.Func<System.AsyncCallback, object, System.IAsyncResult> beginMethod, System.Action<System.IAsyncResult> endMethod, object state, System.Threading.Tasks.TaskCreationOptions creationOptions) { throw null; }
        public System.Threading.Tasks.Task FromAsync(System.IAsyncResult asyncResult, System.Action<System.IAsyncResult> endMethod) { throw null; }
        public System.Threading.Tasks.Task FromAsync(System.IAsyncResult asyncResult, System.Action<System.IAsyncResult> endMethod, System.Threading.Tasks.TaskCreationOptions creationOptions) { throw null; }
        public System.Threading.Tasks.Task FromAsync(System.IAsyncResult asyncResult, System.Action<System.IAsyncResult> endMethod, System.Threading.Tasks.TaskCreationOptions creationOptions, System.Threading.Tasks.TaskScheduler scheduler) { throw null; }
        public System.Threading.Tasks.Task<TResult> FromAsync<TResult>(System.Func<System.AsyncCallback, object, System.IAsyncResult> beginMethod, System.Func<System.IAsyncResult, TResult> endMethod, object state) { throw null; }
        public System.Threading.Tasks.Task<TResult> FromAsync<TResult>(System.Func<System.AsyncCallback, object, System.IAsyncResult> beginMethod, System.Func<System.IAsyncResult, TResult> endMethod, object state, System.Threading.Tasks.TaskCreationOptions creationOptions) { throw null; }
        public System.Threading.Tasks.Task FromAsync<TArg1>(System.Func<TArg1, System.AsyncCallback, object, System.IAsyncResult> beginMethod, System.Action<System.IAsyncResult> endMethod, TArg1 arg1, object state) { throw null; }
        public System.Threading.Tasks.Task FromAsync<TArg1>(System.Func<TArg1, System.AsyncCallback, object, System.IAsyncResult> beginMethod, System.Action<System.IAsyncResult> endMethod, TArg1 arg1, object state, System.Threading.Tasks.TaskCreationOptions creationOptions) { throw null; }
        public System.Threading.Tasks.Task<TResult> FromAsync<TResult>(System.IAsyncResult asyncResult, System.Func<System.IAsyncResult, TResult> endMethod) { throw null; }
        public System.Threading.Tasks.Task<TResult> FromAsync<TResult>(System.IAsyncResult asyncResult, System.Func<System.IAsyncResult, TResult> endMethod, System.Threading.Tasks.TaskCreationOptions creationOptions) { throw null; }
        public System.Threading.Tasks.Task<TResult> FromAsync<TResult>(System.IAsyncResult asyncResult, System.Func<System.IAsyncResult, TResult> endMethod, System.Threading.Tasks.TaskCreationOptions creationOptions, System.Threading.Tasks.TaskScheduler scheduler) { throw null; }
        public System.Threading.Tasks.Task<TResult> FromAsync<TArg1, TResult>(System.Func<TArg1, System.AsyncCallback, object, System.IAsyncResult> beginMethod, System.Func<System.IAsyncResult, TResult> endMethod, TArg1 arg1, object state) { throw null; }
        public System.Threading.Tasks.Task<TResult> FromAsync<TArg1, TResult>(System.Func<TArg1, System.AsyncCallback, object, System.IAsyncResult> beginMethod, System.Func<System.IAsyncResult, TResult> endMethod, TArg1 arg1, object state, System.Threading.Tasks.TaskCreationOptions creationOptions) { throw null; }
        public System.Threading.Tasks.Task FromAsync<TArg1, TArg2>(System.Func<TArg1, TArg2, System.AsyncCallback, object, System.IAsyncResult> beginMethod, System.Action<System.IAsyncResult> endMethod, TArg1 arg1, TArg2 arg2, object state) { throw null; }
        public System.Threading.Tasks.Task FromAsync<TArg1, TArg2>(System.Func<TArg1, TArg2, System.AsyncCallback, object, System.IAsyncResult> beginMethod, System.Action<System.IAsyncResult> endMethod, TArg1 arg1, TArg2 arg2, object state, System.Threading.Tasks.TaskCreationOptions creationOptions) { throw null; }
        public System.Threading.Tasks.Task<TResult> FromAsync<TArg1, TArg2, TResult>(System.Func<TArg1, TArg2, System.AsyncCallback, object, System.IAsyncResult> beginMethod, System.Func<System.IAsyncResult, TResult> endMethod, TArg1 arg1, TArg2 arg2, object state) { throw null; }
        public System.Threading.Tasks.Task<TResult> FromAsync<TArg1, TArg2, TResult>(System.Func<TArg1, TArg2, System.AsyncCallback, object, System.IAsyncResult> beginMethod, System.Func<System.IAsyncResult, TResult> endMethod, TArg1 arg1, TArg2 arg2, object state, System.Threading.Tasks.TaskCreationOptions creationOptions) { throw null; }
        public System.Threading.Tasks.Task FromAsync<TArg1, TArg2, TArg3>(System.Func<TArg1, TArg2, TArg3, System.AsyncCallback, object, System.IAsyncResult> beginMethod, System.Action<System.IAsyncResult> endMethod, TArg1 arg1, TArg2 arg2, TArg3 arg3, object state) { throw null; }
        public System.Threading.Tasks.Task FromAsync<TArg1, TArg2, TArg3>(System.Func<TArg1, TArg2, TArg3, System.AsyncCallback, object, System.IAsyncResult> beginMethod, System.Action<System.IAsyncResult> endMethod, TArg1 arg1, TArg2 arg2, TArg3 arg3, object state, System.Threading.Tasks.TaskCreationOptions creationOptions) { throw null; }
        public System.Threading.Tasks.Task<TResult> FromAsync<TArg1, TArg2, TArg3, TResult>(System.Func<TArg1, TArg2, TArg3, System.AsyncCallback, object, System.IAsyncResult> beginMethod, System.Func<System.IAsyncResult, TResult> endMethod, TArg1 arg1, TArg2 arg2, TArg3 arg3, object state) { throw null; }
        public System.Threading.Tasks.Task<TResult> FromAsync<TArg1, TArg2, TArg3, TResult>(System.Func<TArg1, TArg2, TArg3, System.AsyncCallback, object, System.IAsyncResult> beginMethod, System.Func<System.IAsyncResult, TResult> endMethod, TArg1 arg1, TArg2 arg2, TArg3 arg3, object state, System.Threading.Tasks.TaskCreationOptions creationOptions) { throw null; }
        public System.Threading.Tasks.Task StartNew(System.Action action) { throw null; }
        public System.Threading.Tasks.Task StartNew(System.Action action, System.Threading.CancellationToken cancellationToken) { throw null; }
        public System.Threading.Tasks.Task StartNew(System.Action action, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskCreationOptions creationOptions, System.Threading.Tasks.TaskScheduler scheduler) { throw null; }
        public System.Threading.Tasks.Task StartNew(System.Action action, System.Threading.Tasks.TaskCreationOptions creationOptions) { throw null; }
        public System.Threading.Tasks.Task StartNew(System.Action<object> action, object state) { throw null; }
        public System.Threading.Tasks.Task StartNew(System.Action<object> action, object state, System.Threading.CancellationToken cancellationToken) { throw null; }
        public System.Threading.Tasks.Task StartNew(System.Action<object> action, object state, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskCreationOptions creationOptions, System.Threading.Tasks.TaskScheduler scheduler) { throw null; }
        public System.Threading.Tasks.Task StartNew(System.Action<object> action, object state, System.Threading.Tasks.TaskCreationOptions creationOptions) { throw null; }
        public System.Threading.Tasks.Task<TResult> StartNew<TResult>(System.Func<object, TResult> function, object state) { throw null; }
        public System.Threading.Tasks.Task<TResult> StartNew<TResult>(System.Func<object, TResult> function, object state, System.Threading.CancellationToken cancellationToken) { throw null; }
        public System.Threading.Tasks.Task<TResult> StartNew<TResult>(System.Func<object, TResult> function, object state, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskCreationOptions creationOptions, System.Threading.Tasks.TaskScheduler scheduler) { throw null; }
        public System.Threading.Tasks.Task<TResult> StartNew<TResult>(System.Func<object, TResult> function, object state, System.Threading.Tasks.TaskCreationOptions creationOptions) { throw null; }
        public System.Threading.Tasks.Task<TResult> StartNew<TResult>(System.Func<TResult> function) { throw null; }
        public System.Threading.Tasks.Task<TResult> StartNew<TResult>(System.Func<TResult> function, System.Threading.CancellationToken cancellationToken) { throw null; }
        public System.Threading.Tasks.Task<TResult> StartNew<TResult>(System.Func<TResult> function, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskCreationOptions creationOptions, System.Threading.Tasks.TaskScheduler scheduler) { throw null; }
        public System.Threading.Tasks.Task<TResult> StartNew<TResult>(System.Func<TResult> function, System.Threading.Tasks.TaskCreationOptions creationOptions) { throw null; }
    }
    public partial class TaskFactory<TResult>
    {
        public TaskFactory() { }
        public TaskFactory(System.Threading.CancellationToken cancellationToken) { }
        public TaskFactory(System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskCreationOptions creationOptions, System.Threading.Tasks.TaskContinuationOptions continuationOptions, System.Threading.Tasks.TaskScheduler scheduler) { }
        public TaskFactory(System.Threading.Tasks.TaskCreationOptions creationOptions, System.Threading.Tasks.TaskContinuationOptions continuationOptions) { }
        public TaskFactory(System.Threading.Tasks.TaskScheduler scheduler) { }
        public System.Threading.CancellationToken CancellationToken { get { throw null; } }
        public System.Threading.Tasks.TaskContinuationOptions ContinuationOptions { get { throw null; } }
        public System.Threading.Tasks.TaskCreationOptions CreationOptions { get { throw null; } }
        public System.Threading.Tasks.TaskScheduler Scheduler { get { throw null; } }
        public System.Threading.Tasks.Task<TResult> ContinueWhenAll(System.Threading.Tasks.Task[] tasks, System.Func<System.Threading.Tasks.Task[], TResult> continuationFunction) { throw null; }
        public System.Threading.Tasks.Task<TResult> ContinueWhenAll(System.Threading.Tasks.Task[] tasks, System.Func<System.Threading.Tasks.Task[], TResult> continuationFunction, System.Threading.CancellationToken cancellationToken) { throw null; }
        public System.Threading.Tasks.Task<TResult> ContinueWhenAll(System.Threading.Tasks.Task[] tasks, System.Func<System.Threading.Tasks.Task[], TResult> continuationFunction, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskContinuationOptions continuationOptions, System.Threading.Tasks.TaskScheduler scheduler) { throw null; }
        public System.Threading.Tasks.Task<TResult> ContinueWhenAll(System.Threading.Tasks.Task[] tasks, System.Func<System.Threading.Tasks.Task[], TResult> continuationFunction, System.Threading.Tasks.TaskContinuationOptions continuationOptions) { throw null; }
        public System.Threading.Tasks.Task<TResult> ContinueWhenAll<TAntecedentResult>(System.Threading.Tasks.Task<TAntecedentResult>[] tasks, System.Func<System.Threading.Tasks.Task<TAntecedentResult>[], TResult> continuationFunction) { throw null; }
        public System.Threading.Tasks.Task<TResult> ContinueWhenAll<TAntecedentResult>(System.Threading.Tasks.Task<TAntecedentResult>[] tasks, System.Func<System.Threading.Tasks.Task<TAntecedentResult>[], TResult> continuationFunction, System.Threading.CancellationToken cancellationToken) { throw null; }
        public System.Threading.Tasks.Task<TResult> ContinueWhenAll<TAntecedentResult>(System.Threading.Tasks.Task<TAntecedentResult>[] tasks, System.Func<System.Threading.Tasks.Task<TAntecedentResult>[], TResult> continuationFunction, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskContinuationOptions continuationOptions, System.Threading.Tasks.TaskScheduler scheduler) { throw null; }
        public System.Threading.Tasks.Task<TResult> ContinueWhenAll<TAntecedentResult>(System.Threading.Tasks.Task<TAntecedentResult>[] tasks, System.Func<System.Threading.Tasks.Task<TAntecedentResult>[], TResult> continuationFunction, System.Threading.Tasks.TaskContinuationOptions continuationOptions) { throw null; }
        public System.Threading.Tasks.Task<TResult> ContinueWhenAny(System.Threading.Tasks.Task[] tasks, System.Func<System.Threading.Tasks.Task, TResult> continuationFunction) { throw null; }
        public System.Threading.Tasks.Task<TResult> ContinueWhenAny(System.Threading.Tasks.Task[] tasks, System.Func<System.Threading.Tasks.Task, TResult> continuationFunction, System.Threading.CancellationToken cancellationToken) { throw null; }
        public System.Threading.Tasks.Task<TResult> ContinueWhenAny(System.Threading.Tasks.Task[] tasks, System.Func<System.Threading.Tasks.Task, TResult> continuationFunction, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskContinuationOptions continuationOptions, System.Threading.Tasks.TaskScheduler scheduler) { throw null; }
        public System.Threading.Tasks.Task<TResult> ContinueWhenAny(System.Threading.Tasks.Task[] tasks, System.Func<System.Threading.Tasks.Task, TResult> continuationFunction, System.Threading.Tasks.TaskContinuationOptions continuationOptions) { throw null; }
        public System.Threading.Tasks.Task<TResult> ContinueWhenAny<TAntecedentResult>(System.Threading.Tasks.Task<TAntecedentResult>[] tasks, System.Func<System.Threading.Tasks.Task<TAntecedentResult>, TResult> continuationFunction) { throw null; }
        public System.Threading.Tasks.Task<TResult> ContinueWhenAny<TAntecedentResult>(System.Threading.Tasks.Task<TAntecedentResult>[] tasks, System.Func<System.Threading.Tasks.Task<TAntecedentResult>, TResult> continuationFunction, System.Threading.CancellationToken cancellationToken) { throw null; }
        public System.Threading.Tasks.Task<TResult> ContinueWhenAny<TAntecedentResult>(System.Threading.Tasks.Task<TAntecedentResult>[] tasks, System.Func<System.Threading.Tasks.Task<TAntecedentResult>, TResult> continuationFunction, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskContinuationOptions continuationOptions, System.Threading.Tasks.TaskScheduler scheduler) { throw null; }
        public System.Threading.Tasks.Task<TResult> ContinueWhenAny<TAntecedentResult>(System.Threading.Tasks.Task<TAntecedentResult>[] tasks, System.Func<System.Threading.Tasks.Task<TAntecedentResult>, TResult> continuationFunction, System.Threading.Tasks.TaskContinuationOptions continuationOptions) { throw null; }
        public System.Threading.Tasks.Task<TResult> FromAsync(System.Func<System.AsyncCallback, object, System.IAsyncResult> beginMethod, System.Func<System.IAsyncResult, TResult> endMethod, object state) { throw null; }
        public System.Threading.Tasks.Task<TResult> FromAsync(System.Func<System.AsyncCallback, object, System.IAsyncResult> beginMethod, System.Func<System.IAsyncResult, TResult> endMethod, object state, System.Threading.Tasks.TaskCreationOptions creationOptions) { throw null; }
        public System.Threading.Tasks.Task<TResult> FromAsync(System.IAsyncResult asyncResult, System.Func<System.IAsyncResult, TResult> endMethod) { throw null; }
        public System.Threading.Tasks.Task<TResult> FromAsync(System.IAsyncResult asyncResult, System.Func<System.IAsyncResult, TResult> endMethod, System.Threading.Tasks.TaskCreationOptions creationOptions) { throw null; }
        public System.Threading.Tasks.Task<TResult> FromAsync(System.IAsyncResult asyncResult, System.Func<System.IAsyncResult, TResult> endMethod, System.Threading.Tasks.TaskCreationOptions creationOptions, System.Threading.Tasks.TaskScheduler scheduler) { throw null; }
        public System.Threading.Tasks.Task<TResult> FromAsync<TArg1>(System.Func<TArg1, System.AsyncCallback, object, System.IAsyncResult> beginMethod, System.Func<System.IAsyncResult, TResult> endMethod, TArg1 arg1, object state) { throw null; }
        public System.Threading.Tasks.Task<TResult> FromAsync<TArg1>(System.Func<TArg1, System.AsyncCallback, object, System.IAsyncResult> beginMethod, System.Func<System.IAsyncResult, TResult> endMethod, TArg1 arg1, object state, System.Threading.Tasks.TaskCreationOptions creationOptions) { throw null; }
        public System.Threading.Tasks.Task<TResult> FromAsync<TArg1, TArg2>(System.Func<TArg1, TArg2, System.AsyncCallback, object, System.IAsyncResult> beginMethod, System.Func<System.IAsyncResult, TResult> endMethod, TArg1 arg1, TArg2 arg2, object state) { throw null; }
        public System.Threading.Tasks.Task<TResult> FromAsync<TArg1, TArg2>(System.Func<TArg1, TArg2, System.AsyncCallback, object, System.IAsyncResult> beginMethod, System.Func<System.IAsyncResult, TResult> endMethod, TArg1 arg1, TArg2 arg2, object state, System.Threading.Tasks.TaskCreationOptions creationOptions) { throw null; }
        public System.Threading.Tasks.Task<TResult> FromAsync<TArg1, TArg2, TArg3>(System.Func<TArg1, TArg2, TArg3, System.AsyncCallback, object, System.IAsyncResult> beginMethod, System.Func<System.IAsyncResult, TResult> endMethod, TArg1 arg1, TArg2 arg2, TArg3 arg3, object state) { throw null; }
        public System.Threading.Tasks.Task<TResult> FromAsync<TArg1, TArg2, TArg3>(System.Func<TArg1, TArg2, TArg3, System.AsyncCallback, object, System.IAsyncResult> beginMethod, System.Func<System.IAsyncResult, TResult> endMethod, TArg1 arg1, TArg2 arg2, TArg3 arg3, object state, System.Threading.Tasks.TaskCreationOptions creationOptions) { throw null; }
        public System.Threading.Tasks.Task<TResult> StartNew(System.Func<object, TResult> function, object state) { throw null; }
        public System.Threading.Tasks.Task<TResult> StartNew(System.Func<object, TResult> function, object state, System.Threading.CancellationToken cancellationToken) { throw null; }
        public System.Threading.Tasks.Task<TResult> StartNew(System.Func<object, TResult> function, object state, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskCreationOptions creationOptions, System.Threading.Tasks.TaskScheduler scheduler) { throw null; }
        public System.Threading.Tasks.Task<TResult> StartNew(System.Func<object, TResult> function, object state, System.Threading.Tasks.TaskCreationOptions creationOptions) { throw null; }
        public System.Threading.Tasks.Task<TResult> StartNew(System.Func<TResult> function) { throw null; }
        public System.Threading.Tasks.Task<TResult> StartNew(System.Func<TResult> function, System.Threading.CancellationToken cancellationToken) { throw null; }
        public System.Threading.Tasks.Task<TResult> StartNew(System.Func<TResult> function, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskCreationOptions creationOptions, System.Threading.Tasks.TaskScheduler scheduler) { throw null; }
        public System.Threading.Tasks.Task<TResult> StartNew(System.Func<TResult> function, System.Threading.Tasks.TaskCreationOptions creationOptions) { throw null; }
    }
    public abstract partial class TaskScheduler
    {
        protected TaskScheduler() { }
        public static System.Threading.Tasks.TaskScheduler Current { get { throw null; } }
        public static System.Threading.Tasks.TaskScheduler Default { get { throw null; } }
        public int Id { get { throw null; } }
        public virtual int MaximumConcurrencyLevel { get { throw null; } }
        public static event System.EventHandler<System.Threading.Tasks.UnobservedTaskExceptionEventArgs> UnobservedTaskException { add { } remove { } }
        public static System.Threading.Tasks.TaskScheduler FromCurrentSynchronizationContext() { throw null; }
        protected abstract System.Collections.Generic.IEnumerable<System.Threading.Tasks.Task> GetScheduledTasks();
        protected internal abstract void QueueTask(System.Threading.Tasks.Task task);
        protected internal virtual bool TryDequeue(System.Threading.Tasks.Task task) { throw null; }
        protected bool TryExecuteTask(System.Threading.Tasks.Task task) { throw null; }
        protected abstract bool TryExecuteTaskInline(System.Threading.Tasks.Task task, bool taskWasPreviouslyQueued);
    }
    public enum TaskStatus
    {
        Canceled = 6,
        Created = 0,
        Faulted = 7,
        RanToCompletion = 5,
        Running = 3,
        WaitingForActivation = 1,
        WaitingForChildrenToComplete = 4,
        WaitingToRun = 2,
    }
    public partial class Task<TResult> : System.Threading.Tasks.Task
    {
        public Task(System.Func<object, TResult> function, object state) : base (default(System.Action)) { }
        public Task(System.Func<object, TResult> function, object state, System.Threading.CancellationToken cancellationToken) : base (default(System.Action)) { }
        public Task(System.Func<object, TResult> function, object state, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskCreationOptions creationOptions) : base (default(System.Action)) { }
        public Task(System.Func<object, TResult> function, object state, System.Threading.Tasks.TaskCreationOptions creationOptions) : base (default(System.Action)) { }
        public Task(System.Func<TResult> function) : base (default(System.Action)) { }
        public Task(System.Func<TResult> function, System.Threading.CancellationToken cancellationToken) : base (default(System.Action)) { }
        public Task(System.Func<TResult> function, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskCreationOptions creationOptions) : base (default(System.Action)) { }
        public Task(System.Func<TResult> function, System.Threading.Tasks.TaskCreationOptions creationOptions) : base (default(System.Action)) { }
        public static new System.Threading.Tasks.TaskFactory<TResult> Factory { get { throw null; } }
        public TResult Result { get { throw null; } }
        public new System.Runtime.CompilerServices.ConfiguredTaskAwaitable<TResult> ConfigureAwait(bool continueOnCapturedContext) { throw null; }
        public System.Threading.Tasks.Task ContinueWith(System.Action<System.Threading.Tasks.Task<TResult>, object> continuationAction, object state) { throw null; }
        public System.Threading.Tasks.Task ContinueWith(System.Action<System.Threading.Tasks.Task<TResult>, object> continuationAction, object state, System.Threading.CancellationToken cancellationToken) { throw null; }
        public System.Threading.Tasks.Task ContinueWith(System.Action<System.Threading.Tasks.Task<TResult>, object> continuationAction, object state, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskContinuationOptions continuationOptions, System.Threading.Tasks.TaskScheduler scheduler) { throw null; }
        public System.Threading.Tasks.Task ContinueWith(System.Action<System.Threading.Tasks.Task<TResult>, object> continuationAction, object state, System.Threading.Tasks.TaskContinuationOptions continuationOptions) { throw null; }
        public System.Threading.Tasks.Task ContinueWith(System.Action<System.Threading.Tasks.Task<TResult>, object> continuationAction, object state, System.Threading.Tasks.TaskScheduler scheduler) { throw null; }
        public System.Threading.Tasks.Task ContinueWith(System.Action<System.Threading.Tasks.Task<TResult>> continuationAction) { throw null; }
        public System.Threading.Tasks.Task ContinueWith(System.Action<System.Threading.Tasks.Task<TResult>> continuationAction, System.Threading.CancellationToken cancellationToken) { throw null; }
        public System.Threading.Tasks.Task ContinueWith(System.Action<System.Threading.Tasks.Task<TResult>> continuationAction, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskContinuationOptions continuationOptions, System.Threading.Tasks.TaskScheduler scheduler) { throw null; }
        public System.Threading.Tasks.Task ContinueWith(System.Action<System.Threading.Tasks.Task<TResult>> continuationAction, System.Threading.Tasks.TaskContinuationOptions continuationOptions) { throw null; }
        public System.Threading.Tasks.Task ContinueWith(System.Action<System.Threading.Tasks.Task<TResult>> continuationAction, System.Threading.Tasks.TaskScheduler scheduler) { throw null; }
        public System.Threading.Tasks.Task<TNewResult> ContinueWith<TNewResult>(System.Func<System.Threading.Tasks.Task<TResult>, object, TNewResult> continuationFunction, object state) { throw null; }
        public System.Threading.Tasks.Task<TNewResult> ContinueWith<TNewResult>(System.Func<System.Threading.Tasks.Task<TResult>, object, TNewResult> continuationFunction, object state, System.Threading.CancellationToken cancellationToken) { throw null; }
        public System.Threading.Tasks.Task<TNewResult> ContinueWith<TNewResult>(System.Func<System.Threading.Tasks.Task<TResult>, object, TNewResult> continuationFunction, object state, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskContinuationOptions continuationOptions, System.Threading.Tasks.TaskScheduler scheduler) { throw null; }
        public System.Threading.Tasks.Task<TNewResult> ContinueWith<TNewResult>(System.Func<System.Threading.Tasks.Task<TResult>, object, TNewResult> continuationFunction, object state, System.Threading.Tasks.TaskContinuationOptions continuationOptions) { throw null; }
        public System.Threading.Tasks.Task<TNewResult> ContinueWith<TNewResult>(System.Func<System.Threading.Tasks.Task<TResult>, object, TNewResult> continuationFunction, object state, System.Threading.Tasks.TaskScheduler scheduler) { throw null; }
        public System.Threading.Tasks.Task<TNewResult> ContinueWith<TNewResult>(System.Func<System.Threading.Tasks.Task<TResult>, TNewResult> continuationFunction) { throw null; }
        public System.Threading.Tasks.Task<TNewResult> ContinueWith<TNewResult>(System.Func<System.Threading.Tasks.Task<TResult>, TNewResult> continuationFunction, System.Threading.CancellationToken cancellationToken) { throw null; }
        public System.Threading.Tasks.Task<TNewResult> ContinueWith<TNewResult>(System.Func<System.Threading.Tasks.Task<TResult>, TNewResult> continuationFunction, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskContinuationOptions continuationOptions, System.Threading.Tasks.TaskScheduler scheduler) { throw null; }
        public System.Threading.Tasks.Task<TNewResult> ContinueWith<TNewResult>(System.Func<System.Threading.Tasks.Task<TResult>, TNewResult> continuationFunction, System.Threading.Tasks.TaskContinuationOptions continuationOptions) { throw null; }
        public System.Threading.Tasks.Task<TNewResult> ContinueWith<TNewResult>(System.Func<System.Threading.Tasks.Task<TResult>, TNewResult> continuationFunction, System.Threading.Tasks.TaskScheduler scheduler) { throw null; }
        public new System.Runtime.CompilerServices.TaskAwaiter<TResult> GetAwaiter() { throw null; }
    }
    public partial class UnobservedTaskExceptionEventArgs : System.EventArgs
    {
        public UnobservedTaskExceptionEventArgs(System.AggregateException exception) { }
        public System.AggregateException Exception { get { throw null; } }
        public bool Observed { get { throw null; } }
        public void SetObserved() { }
    }
    [System.Runtime.CompilerServices.AsyncMethodBuilderAttribute(typeof(System.Runtime.CompilerServices.AsyncValueTaskMethodBuilder<>))]
    public readonly partial struct ValueTask<TResult> : System.IEquatable<System.Threading.Tasks.ValueTask<TResult>>
    {
        internal readonly TResult _result;
        public ValueTask(System.Threading.Tasks.Task<TResult> task) { throw null; }
        public ValueTask(TResult result) { throw null; }
        public bool IsCanceled { get { throw null; } }
        public bool IsCompleted { get { throw null; } }
        public bool IsCompletedSuccessfully { get { throw null; } }
        public bool IsFaulted { get { throw null; } }
        public TResult Result { get { throw null; } }
        public System.Threading.Tasks.Task<TResult> AsTask() { throw null; }
        public System.Runtime.CompilerServices.ConfiguredValueTaskAwaitable<TResult> ConfigureAwait(bool continueOnCapturedContext) { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
        public static System.Runtime.CompilerServices.AsyncValueTaskMethodBuilder<TResult> CreateAsyncMethodBuilder() { throw null; }
        public override bool Equals(object obj) { throw null; }
        public bool Equals(System.Threading.Tasks.ValueTask<TResult> other) { throw null; }
        public System.Runtime.CompilerServices.ValueTaskAwaiter<TResult> GetAwaiter() { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Threading.Tasks.ValueTask<TResult> left, System.Threading.Tasks.ValueTask<TResult> right) { throw null; }
        public static bool operator !=(System.Threading.Tasks.ValueTask<TResult> left, System.Threading.Tasks.ValueTask<TResult> right) { throw null; }
        public override string ToString() { throw null; }
    }
}
