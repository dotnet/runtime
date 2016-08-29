// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Internal.Runtime.Augments
{
    public static partial class EnvironmentAugments
    {
        public static int CurrentManagedThreadId { get { throw null; } }
        public static int ExitCode { get { throw null; } set { } }
        public static bool HasShutdownStarted { get { throw null; } }
        public static string StackTrace { get { throw null; } }
        public static int TickCount { get { throw null; } }
        public static void Exit(int exitCode) { }
        public static void FailFast(string message, System.Exception error) { }
        public static string[] GetCommandLineArgs() { throw null; }
    }
}
namespace Microsoft.Win32.SafeHandles
{
    [System.Security.SecurityCriticalAttribute]
    public abstract partial class SafeHandleMinusOneIsInvalid : System.Runtime.InteropServices.SafeHandle
    {
        protected SafeHandleMinusOneIsInvalid(bool ownsHandle) { }
        public override bool IsInvalid { [System.Security.SecurityCriticalAttribute]get { throw null; } }
    }
    [System.Security.SecurityCriticalAttribute]
    public abstract partial class SafeHandleZeroOrMinusOneIsInvalid : System.Runtime.InteropServices.SafeHandle
    {
        protected SafeHandleZeroOrMinusOneIsInvalid(bool ownsHandle) { }
        public override bool IsInvalid { [System.Security.SecurityCriticalAttribute]get { throw null; } }
    }
    [System.Security.SecurityCriticalAttribute]
    public sealed partial class SafeWaitHandle : Microsoft.Win32.SafeHandles.SafeHandleZeroOrMinusOneIsInvalid
    {
        public SafeWaitHandle(System.IntPtr existingHandle, bool ownsHandle) : base(ownsHandle) { }
        [System.Security.SecurityCriticalAttribute]
        protected override bool ReleaseHandle() { throw null; }
    }
}
namespace System
{
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class AccessViolationException : System.SystemException
    {
        public AccessViolationException() { }
        public AccessViolationException(string message) { }
        public AccessViolationException(string message, System.Exception innerException) { }
        protected AccessViolationException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
    [System.Runtime.CompilerServices.TypeForwardedFromAttribute("System.Core, Version=2.0.5.0, Culture=Neutral, PublicKeyToken=7cec85d7bea7798e")]
    public delegate void Action();
    public delegate void Action<in T>(T obj);
    [System.Runtime.CompilerServices.TypeForwardedFromAttribute("System.Core, Version=2.0.5.0, Culture=Neutral, PublicKeyToken=7cec85d7bea7798e")]
    public delegate void Action<in T1, in T2>(T1 arg1, T2 arg2);
    [System.Runtime.CompilerServices.TypeForwardedFromAttribute("System.Core, Version=2.0.5.0, Culture=Neutral, PublicKeyToken=7cec85d7bea7798e")]
    public delegate void Action<in T1, in T2, in T3>(T1 arg1, T2 arg2, T3 arg3);
    [System.Runtime.CompilerServices.TypeForwardedFromAttribute("System.Core, Version=2.0.5.0, Culture=Neutral, PublicKeyToken=7cec85d7bea7798e")]
    public delegate void Action<in T1, in T2, in T3, in T4>(T1 arg1, T2 arg2, T3 arg3, T4 arg4);
    public delegate void Action<in T1, in T2, in T3, in T4, in T5>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5);
    public delegate void Action<in T1, in T2, in T3, in T4, in T5, in T6>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6);
    public delegate void Action<in T1, in T2, in T3, in T4, in T5, in T6, in T7>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7);
    public delegate void Action<in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8);
    [System.Runtime.InteropServices.ClassInterfaceAttribute((System.Runtime.InteropServices.ClassInterfaceType)(0))]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class Activator
    {
        internal Activator() { }
        public static System.Runtime.Remoting.ObjectHandle CreateComInstanceFrom(string assemblyName, string typeName) { throw null; }
        public static System.Runtime.Remoting.ObjectHandle CreateComInstanceFrom(string assemblyName, string typeName, byte[] hashValue, System.Configuration.Assemblies.AssemblyHashAlgorithm hashAlgorithm) { throw null; }
        [System.Security.SecurityCriticalAttribute]
        public static System.Runtime.Remoting.ObjectHandle CreateInstance(System.AppDomain domain, string assemblyName, string typeName) { throw null; }
        [System.Security.SecurityCriticalAttribute]
        public static System.Runtime.Remoting.ObjectHandle CreateInstance(System.AppDomain domain, string assemblyName, string typeName, bool ignoreCase, System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder binder, object[] args, System.Globalization.CultureInfo culture, object[] activationAttributes) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        [System.Security.SecuritySafeCriticalAttribute]
        public static System.Runtime.Remoting.ObjectHandle CreateInstance(string assemblyName, string typeName) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        [System.Security.SecuritySafeCriticalAttribute]
        public static System.Runtime.Remoting.ObjectHandle CreateInstance(string assemblyName, string typeName, bool ignoreCase, System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder binder, object[] args, System.Globalization.CultureInfo culture, object[] activationAttributes) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        [System.Security.SecuritySafeCriticalAttribute]
        public static System.Runtime.Remoting.ObjectHandle CreateInstance(string assemblyName, string typeName, object[] activationAttributes) { throw null; }
        public static object CreateInstance(System.Type type) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public static object CreateInstance(System.Type type, bool nonPublic) { throw null; }
        public static object CreateInstance(System.Type type, params object[] args) { throw null; }
        public static object CreateInstance(System.Type type, object[] args, object[] activationAttributes) { throw null; }
        public static object CreateInstance(System.Type type, System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder binder, object[] args, System.Globalization.CultureInfo culture) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        [System.Security.SecuritySafeCriticalAttribute]
        public static object CreateInstance(System.Type type, System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder binder, object[] args, System.Globalization.CultureInfo culture, object[] activationAttributes) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public static T CreateInstance<T>() { throw null; }
        [System.Security.SecurityCriticalAttribute]
        public static System.Runtime.Remoting.ObjectHandle CreateInstanceFrom(System.AppDomain domain, string assemblyFile, string typeName) { throw null; }
        [System.Security.SecurityCriticalAttribute]
        public static System.Runtime.Remoting.ObjectHandle CreateInstanceFrom(System.AppDomain domain, string assemblyFile, string typeName, bool ignoreCase, System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder binder, object[] args, System.Globalization.CultureInfo culture, object[] activationAttributes) { throw null; }
        public static System.Runtime.Remoting.ObjectHandle CreateInstanceFrom(string assemblyFile, string typeName) { throw null; }
        public static System.Runtime.Remoting.ObjectHandle CreateInstanceFrom(string assemblyFile, string typeName, bool ignoreCase, System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder binder, object[] args, System.Globalization.CultureInfo culture, object[] activationAttributes) { throw null; }
        public static System.Runtime.Remoting.ObjectHandle CreateInstanceFrom(string assemblyFile, string typeName, object[] activationAttributes) { throw null; }
    }
    [System.Diagnostics.DebuggerDisplayAttribute("Count = {InnerExceptionCount}")]
    public partial class AggregateException : System.Exception
    {
        public AggregateException() { }
        public AggregateException(System.Collections.Generic.IEnumerable<System.Exception> innerExceptions) { }
        public AggregateException(params System.Exception[] innerExceptions) { }
        public AggregateException(string message) { }
        public AggregateException(string message, System.Collections.Generic.IEnumerable<System.Exception> innerExceptions) { }
        public AggregateException(string message, System.Exception innerException) { }
        public AggregateException(string message, params System.Exception[] innerExceptions) { }
        protected AggregateException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
        public System.Collections.ObjectModel.ReadOnlyCollection<System.Exception> InnerExceptions { get { throw null; } }
        public override string Message { get { throw null; } }
        public System.AggregateException Flatten() { throw null; }
        public override System.Exception GetBaseException() { throw null; }
        [System.Security.SecurityCriticalAttribute]
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public void Handle(System.Func<System.Exception, bool> predicate) { }
        public override string ToString() { throw null; }
    }
    public static partial class AppContext
    {
        public static string BaseDirectory { [System.Security.SecuritySafeCriticalAttribute]get { throw null; } }
        public static string TargetFrameworkName { get { throw null; } }
        [System.Security.SecuritySafeCriticalAttribute]
        public static object GetData(string name) { throw null; }
        public static void SetSwitch(string switchName, bool isEnabled) { }
        public static bool TryGetSwitch(string switchName, out bool isEnabled) { isEnabled = default(bool); throw null; }
    }
    [System.Runtime.InteropServices.ClassInterfaceAttribute((System.Runtime.InteropServices.ClassInterfaceType)(0))]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class AppDomain
    {
        internal AppDomain() { }
        public string BaseDirectory { [System.Security.SecurityCriticalAttribute]get { throw null; } }
        public static System.AppDomain CurrentDomain { get { throw null; } }
        public bool IsFullyTrusted { [System.Security.SecuritySafeCriticalAttribute]get { throw null; } }
        [System.Security.SecurityCriticalAttribute]
        public object GetData(string name) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public override string ToString() { throw null; }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    [System.Security.SecurityCriticalAttribute]
    public partial class AppDomainManager
    {
        public AppDomainManager() { }
        public virtual bool CheckSecuritySettings(System.Security.SecurityState state) { throw null; }
        [System.Security.SecurityCriticalAttribute]
        public virtual void InitializeNewDomain(System.AppDomainSetup appDomainInfo) { }
    }
    [System.Runtime.InteropServices.ClassInterfaceAttribute((System.Runtime.InteropServices.ClassInterfaceType)(0))]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class AppDomainSetup
    {
        internal AppDomainSetup() { }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class AppDomainUnloadedException : System.SystemException
    {
        public AppDomainUnloadedException() { }
        public AppDomainUnloadedException(string message) { }
        public AppDomainUnloadedException(string message, System.Exception innerException) { }
        protected AppDomainUnloadedException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class ApplicationId
    {
        public ApplicationId(byte[] publicKeyToken, string name, System.Version version, string processorArchitecture, string culture) { }
        public string Culture { get { throw null; } }
        public string Name { get { throw null; } }
        public string ProcessorArchitecture { get { throw null; } }
        public byte[] PublicKeyToken { get { throw null; } }
        public System.Version Version { get { throw null; } }
        public System.ApplicationId Copy() { throw null; }
        public override bool Equals(object o) { throw null; }
        public override int GetHashCode() { throw null; }
        public override string ToString() { throw null; }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class ApplicationException : System.Exception
    {
        public ApplicationException() { }
        public ApplicationException(string message) { }
        public ApplicationException(string message, System.Exception innerException) { }
        protected ApplicationException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct ArgIterator
    {
        public override bool Equals(object o) { throw null; }
        public override int GetHashCode() { throw null; }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class ArgumentException : System.SystemException, System.Runtime.Serialization.ISerializable
    {
        public ArgumentException() { }
        public ArgumentException(string message) { }
        public ArgumentException(string message, System.Exception innerException) { }
        public ArgumentException(string message, string paramName) { }
        public ArgumentException(string message, string paramName, System.Exception innerException) { }
        protected ArgumentException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
        public override string Message { get { throw null; } }
        public virtual string ParamName { get { throw null; } }
        [System.Security.SecurityCriticalAttribute]
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class ArgumentNullException : System.ArgumentException
    {
        public ArgumentNullException() { }
        public ArgumentNullException(string paramName) { }
        public ArgumentNullException(string message, System.Exception innerException) { }
        public ArgumentNullException(string paramName, string message) { }
        protected ArgumentNullException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class ArgumentOutOfRangeException : System.ArgumentException, System.Runtime.Serialization.ISerializable
    {
        public ArgumentOutOfRangeException() { }
        public ArgumentOutOfRangeException(string paramName) { }
        public ArgumentOutOfRangeException(string message, System.Exception innerException) { }
        public ArgumentOutOfRangeException(string paramName, object actualValue, string message) { }
        public ArgumentOutOfRangeException(string paramName, string message) { }
        protected ArgumentOutOfRangeException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
        public virtual object ActualValue { get { throw null; } }
        public override string Message { get { throw null; } }
        [System.Security.SecurityCriticalAttribute]
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class ArithmeticException : System.SystemException
    {
        public ArithmeticException() { }
        public ArithmeticException(string message) { }
        public ArithmeticException(string message, System.Exception innerException) { }
        protected ArithmeticException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public abstract partial class Array : System.Collections.ICollection, System.Collections.IEnumerable, System.Collections.IList, System.Collections.IStructuralComparable, System.Collections.IStructuralEquatable, System.ICloneable
    {
        internal Array() { }
        public bool IsFixedSize { get { throw null; } }
        public bool IsReadOnly { get { throw null; } }
        public bool IsSynchronized { get { throw null; } }
        public int Length { [System.Security.SecuritySafeCriticalAttribute][System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)]get { throw null; } }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public long LongLength { [System.Security.SecuritySafeCriticalAttribute][System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)]get { throw null; } }
        public int Rank { [System.Security.SecuritySafeCriticalAttribute][System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)]get { throw null; } }
        public object SyncRoot { get { throw null; } }
        int System.Collections.ICollection.Count { get { throw null; } }
        object System.Collections.IList.this[int index] { get { throw null; } set { } }
        public static System.Collections.ObjectModel.ReadOnlyCollection<T> AsReadOnly<T>(T[] array) { throw null; }
        public static int BinarySearch(System.Array array, int index, int length, object value) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public static int BinarySearch(System.Array array, int index, int length, object value, System.Collections.IComparer comparer) { throw null; }
        public static int BinarySearch(System.Array array, object value) { throw null; }
        public static int BinarySearch(System.Array array, object value, System.Collections.IComparer comparer) { throw null; }
        public static int BinarySearch<T>(T[] array, T value) { throw null; }
        public static int BinarySearch<T>(T[] array, T value, System.Collections.Generic.IComparer<T> comparer) { throw null; }
        public static int BinarySearch<T>(T[] array, int index, int length, T value) { throw null; }
        public static int BinarySearch<T>(T[] array, int index, int length, T value, System.Collections.Generic.IComparer<T> comparer) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)][System.Security.SecuritySafeCriticalAttribute]
        public static void Clear(System.Array array, int index, int length) { }
        public object Clone() { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public static void ConstrainedCopy(System.Array sourceArray, int sourceIndex, System.Array destinationArray, int destinationIndex, int length) { }
        public static TOutput[] ConvertAll<TInput, TOutput>(TInput[] array, System.Converter<TInput, TOutput> converter) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public static void Copy(System.Array sourceArray, System.Array destinationArray, int length) { }
        public static void Copy(System.Array sourceArray, System.Array destinationArray, long length) { }
        [System.Security.SecuritySafeCriticalAttribute]
        public static void Copy(System.Array sourceArray, int sourceIndex, System.Array destinationArray, int destinationIndex, int length) { }
        public static void Copy(System.Array sourceArray, long sourceIndex, System.Array destinationArray, long destinationIndex, long length) { }
        public void CopyTo(System.Array array, int index) { }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public void CopyTo(System.Array array, long index) { }
        [System.Security.SecuritySafeCriticalAttribute]
        public static System.Array CreateInstance(System.Type elementType, int length) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public static System.Array CreateInstance(System.Type elementType, int length1, int length2) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public static System.Array CreateInstance(System.Type elementType, int length1, int length2, int length3) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public static System.Array CreateInstance(System.Type elementType, params int[] lengths) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public static System.Array CreateInstance(System.Type elementType, int[] lengths, int[] lowerBounds) { throw null; }
        public static System.Array CreateInstance(System.Type elementType, params long[] lengths) { throw null; }
        public static T[] Empty<T>() { throw null; }
        public static bool Exists<T>(T[] array, System.Predicate<T> match) { throw null; }
        public static T Find<T>(T[] array, System.Predicate<T> match) { throw null; }
        public static T[] FindAll<T>(T[] array, System.Predicate<T> match) { throw null; }
        public static int FindIndex<T>(T[] array, int startIndex, int count, System.Predicate<T> match) { throw null; }
        public static int FindIndex<T>(T[] array, int startIndex, System.Predicate<T> match) { throw null; }
        public static int FindIndex<T>(T[] array, System.Predicate<T> match) { throw null; }
        public static T FindLast<T>(T[] array, System.Predicate<T> match) { throw null; }
        public static int FindLastIndex<T>(T[] array, int startIndex, int count, System.Predicate<T> match) { throw null; }
        public static int FindLastIndex<T>(T[] array, int startIndex, System.Predicate<T> match) { throw null; }
        public static int FindLastIndex<T>(T[] array, System.Predicate<T> match) { throw null; }
        public static void ForEach<T>(T[] array, System.Action<T> action) { }
        public System.Collections.IEnumerator GetEnumerator() { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)][System.Security.SecuritySafeCriticalAttribute]
        public int GetLength(int dimension) { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public long GetLongLength(int dimension) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)][System.Security.SecuritySafeCriticalAttribute]
        public int GetLowerBound(int dimension) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)][System.Security.SecuritySafeCriticalAttribute]
        public int GetUpperBound(int dimension) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public object GetValue(int index) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public object GetValue(int index1, int index2) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public object GetValue(int index1, int index2, int index3) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public object GetValue(params int[] indices) { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public object GetValue(long index) { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public object GetValue(long index1, long index2) { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public object GetValue(long index1, long index2, long index3) { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public object GetValue(params long[] indices) { throw null; }
        public static int IndexOf(System.Array array, object value) { throw null; }
        public static int IndexOf(System.Array array, object value, int startIndex) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public static int IndexOf(System.Array array, object value, int startIndex, int count) { throw null; }
        public static int IndexOf<T>(T[] array, T value) { throw null; }
        public static int IndexOf<T>(T[] array, T value, int startIndex) { throw null; }
        public static int IndexOf<T>(T[] array, T value, int startIndex, int count) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)][System.Security.SecuritySafeCriticalAttribute]
        public void Initialize() { }
        public static int LastIndexOf(System.Array array, object value) { throw null; }
        public static int LastIndexOf(System.Array array, object value, int startIndex) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public static int LastIndexOf(System.Array array, object value, int startIndex, int count) { throw null; }
        public static int LastIndexOf<T>(T[] array, T value) { throw null; }
        public static int LastIndexOf<T>(T[] array, T value, int startIndex) { throw null; }
        public static int LastIndexOf<T>(T[] array, T value, int startIndex, int count) { throw null; }
        public static void Resize<T>(ref T[] array, int newSize) { }
        public static void Reverse(System.Array array) { }
        [System.Security.SecuritySafeCriticalAttribute]
        public static void Reverse(System.Array array, int index, int length) { }
        [System.Security.SecuritySafeCriticalAttribute]
        public void SetValue(object value, int index) { }
        [System.Security.SecuritySafeCriticalAttribute]
        public void SetValue(object value, int index1, int index2) { }
        [System.Security.SecuritySafeCriticalAttribute]
        public void SetValue(object value, int index1, int index2, int index3) { }
        [System.Security.SecuritySafeCriticalAttribute]
        public void SetValue(object value, params int[] indices) { }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public void SetValue(object value, long index) { }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public void SetValue(object value, long index1, long index2) { }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public void SetValue(object value, long index1, long index2, long index3) { }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public void SetValue(object value, params long[] indices) { }
        public static void Sort(System.Array array) { }
        public static void Sort(System.Array keys, System.Array items) { }
        public static void Sort(System.Array keys, System.Array items, System.Collections.IComparer comparer) { }
        public static void Sort(System.Array keys, System.Array items, int index, int length) { }
        [System.Security.SecuritySafeCriticalAttribute]
        public static void Sort(System.Array keys, System.Array items, int index, int length, System.Collections.IComparer comparer) { }
        public static void Sort(System.Array array, System.Collections.IComparer comparer) { }
        public static void Sort(System.Array array, int index, int length) { }
        public static void Sort(System.Array array, int index, int length, System.Collections.IComparer comparer) { }
        public static void Sort<T>(T[] array) { }
        public static void Sort<T>(T[] array, System.Collections.Generic.IComparer<T> comparer) { }
        public static void Sort<T>(T[] array, System.Comparison<T> comparison) { }
        public static void Sort<T>(T[] array, int index, int length) { }
        [System.Security.SecuritySafeCriticalAttribute]
        public static void Sort<T>(T[] array, int index, int length, System.Collections.Generic.IComparer<T> comparer) { }
        public static void Sort<TKey, TValue>(TKey[] keys, TValue[] items) { }
        public static void Sort<TKey, TValue>(TKey[] keys, TValue[] items, System.Collections.Generic.IComparer<TKey> comparer) { }
        public static void Sort<TKey, TValue>(TKey[] keys, TValue[] items, int index, int length) { }
        [System.Security.SecuritySafeCriticalAttribute]
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
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct ArraySegment<T> : System.Collections.Generic.ICollection<T>, System.Collections.Generic.IEnumerable<T>, System.Collections.Generic.IList<T>, System.Collections.Generic.IReadOnlyCollection<T>, System.Collections.Generic.IReadOnlyList<T>, System.Collections.IEnumerable
    {
        public ArraySegment(T[] array) { throw null;}
        public ArraySegment(T[] array, int offset, int count) { throw null;}
        public T[] Array { get { throw null; } }
        public int Count { get { throw null; } }
        public int Offset { get { throw null; } }
        bool System.Collections.Generic.ICollection<T>.IsReadOnly { get { throw null; } }
        T System.Collections.Generic.IList<T>.this[int index] { get { throw null; } set { } }
        T System.Collections.Generic.IReadOnlyList<T>.this[int index] { get { throw null; } }
        public bool Equals(System.ArraySegment<T> obj) { throw null; }
        public override bool Equals(object obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.ArraySegment<T> a, System.ArraySegment<T> b) { throw null; }
        public static bool operator !=(System.ArraySegment<T> a, System.ArraySegment<T> b) { throw null; }
        void System.Collections.Generic.ICollection<T>.Add(T item) { }
        void System.Collections.Generic.ICollection<T>.Clear() { }
        bool System.Collections.Generic.ICollection<T>.Contains(T item) { throw null; }
        void System.Collections.Generic.ICollection<T>.CopyTo(T[] array, int arrayIndex) { }
        bool System.Collections.Generic.ICollection<T>.Remove(T item) { throw null; }
        System.Collections.Generic.IEnumerator<T> System.Collections.Generic.IEnumerable<T>.GetEnumerator() { throw null; }
        int System.Collections.Generic.IList<T>.IndexOf(T item) { throw null; }
        void System.Collections.Generic.IList<T>.Insert(int index, T item) { }
        void System.Collections.Generic.IList<T>.RemoveAt(int index) { }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class ArrayTypeMismatchException : System.SystemException
    {
        public ArrayTypeMismatchException() { }
        public ArrayTypeMismatchException(string message) { }
        public ArrayTypeMismatchException(string message, System.Exception innerException) { }
        protected ArrayTypeMismatchException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class AssemblyLoadEventArgs : System.EventArgs
    {
        public AssemblyLoadEventArgs(System.Reflection.Assembly loadedAssembly) { }
        public System.Reflection.Assembly LoadedAssembly { get { throw null; } }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public delegate void AssemblyLoadEventHandler(object sender, System.AssemblyLoadEventArgs args);
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public delegate void AsyncCallback(System.IAsyncResult ar);
    [System.AttributeUsageAttribute((System.AttributeTargets)(32767), Inherited = true, AllowMultiple = false)]
    [System.Runtime.InteropServices.ClassInterfaceAttribute((System.Runtime.InteropServices.ClassInterfaceType)(0))]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public abstract partial class Attribute
    {
        protected Attribute() { }
        public virtual object TypeId { get { throw null; } }
        [System.Security.SecuritySafeCriticalAttribute]
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
        [System.Security.SecuritySafeCriticalAttribute]
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
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
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
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class AttributeUsageAttribute : System.Attribute
    {
        public AttributeUsageAttribute(System.AttributeTargets validOn) { }
        public bool AllowMultiple { get { throw null; } set { } }
        public bool Inherited { get { throw null; } set { } }
        public System.AttributeTargets ValidOn { get { throw null; } }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class BadImageFormatException : System.SystemException
    {
        public BadImageFormatException() { }
        protected BadImageFormatException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public BadImageFormatException(string message) { }
        public BadImageFormatException(string message, System.Exception inner) { }
        public BadImageFormatException(string message, string fileName) { }
        public BadImageFormatException(string message, string fileName, System.Exception inner) { }
        public string FileName { get { throw null; } }
        public string FusionLog { [System.Security.SecuritySafeCriticalAttribute]get { throw null; } }
        public override string Message { get { throw null; } }
        [System.Security.SecurityCriticalAttribute]
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public override string ToString() { throw null; }
    }
    [System.FlagsAttribute]
    public enum Base64FormattingOptions
    {
        InsertLineBreaks = 1,
        None = 0,
    }
    public static partial class BitConverter
    {
        public static readonly bool IsLittleEndian;
        [System.Security.SecuritySafeCriticalAttribute]
        public static long DoubleToInt64Bits(double value) { throw null; }
        public static byte[] GetBytes(bool value) { throw null; }
        public static byte[] GetBytes(char value) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public static byte[] GetBytes(double value) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public static byte[] GetBytes(short value) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public static byte[] GetBytes(int value) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public static byte[] GetBytes(long value) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public static byte[] GetBytes(float value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static byte[] GetBytes(ushort value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static byte[] GetBytes(uint value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static byte[] GetBytes(ulong value) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public static double Int64BitsToDouble(long value) { throw null; }
        public static bool ToBoolean(byte[] value, int startIndex) { throw null; }
        public static char ToChar(byte[] value, int startIndex) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public static double ToDouble(byte[] value, int startIndex) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public static short ToInt16(byte[] value, int startIndex) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public static int ToInt32(byte[] value, int startIndex) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public static long ToInt64(byte[] value, int startIndex) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public static float ToSingle(byte[] value, int startIndex) { throw null; }
        public static string ToString(byte[] value) { throw null; }
        public static string ToString(byte[] value, int startIndex) { throw null; }
        public static string ToString(byte[] value, int startIndex, int length) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ushort ToUInt16(byte[] value, int startIndex) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static uint ToUInt32(byte[] value, int startIndex) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ulong ToUInt64(byte[] value, int startIndex) { throw null; }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct Boolean : System.IComparable, System.IComparable<bool>, System.IConvertible, System.IEquatable<bool>
    {
        public static readonly string FalseString;
        public static readonly string TrueString;
        public int CompareTo(System.Boolean value) { throw null; }
        public int CompareTo(object obj) { throw null; }
        public System.Boolean Equals(System.Boolean obj) { throw null; }
        public override System.Boolean Equals(object obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public System.TypeCode GetTypeCode() { throw null; }
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
        public static System.Boolean TryParse(string value, out System.Boolean result) { result = default(bool); throw null; }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public static partial class Buffer
    {
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)][System.Security.SecuritySafeCriticalAttribute]
        public static void BlockCopy(System.Array src, int srcOffset, System.Array dst, int dstOffset, int count) { }
        [System.Security.SecuritySafeCriticalAttribute]
        public static int ByteLength(System.Array array) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public static byte GetByte(System.Array array, int index) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)][System.CLSCompliantAttribute(false)]
        [System.Security.SecurityCriticalAttribute]
        public unsafe static void MemoryCopy(void* source, void* destination, long destinationSizeInBytes, long sourceBytesToCopy) { }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)][System.CLSCompliantAttribute(false)]
        [System.Security.SecurityCriticalAttribute]
        public unsafe static void MemoryCopy(void* source, void* destination, ulong destinationSizeInBytes, ulong sourceBytesToCopy) { }
        [System.Security.SecuritySafeCriticalAttribute]
        public static void SetByte(System.Array array, int index, byte value) { }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct Byte : System.IComparable, System.IComparable<byte>, System.IConvertible, System.IEquatable<byte>, System.IFormattable
    {
        public const byte MaxValue = (byte)255;
        public const byte MinValue = (byte)0;
        public int CompareTo(System.Byte value) { throw null; }
        public int CompareTo(object value) { throw null; }
        public bool Equals(System.Byte obj) { throw null; }
        public override bool Equals(object obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public System.TypeCode GetTypeCode() { throw null; }
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
        [System.Security.SecuritySafeCriticalAttribute]
        public override string ToString() { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public string ToString(System.IFormatProvider provider) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public string ToString(string format) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public string ToString(string format, System.IFormatProvider provider) { throw null; }
        public static bool TryParse(string s, out System.Byte result) { result = default(byte); throw null; }
        public static bool TryParse(string s, System.Globalization.NumberStyles style, System.IFormatProvider provider, out System.Byte result) { result = default(byte); throw null; }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class CannotUnloadAppDomainException : System.SystemException
    {
        public CannotUnloadAppDomainException() { }
        public CannotUnloadAppDomainException(string message) { }
        public CannotUnloadAppDomainException(string message, System.Exception innerException) { }
        protected CannotUnloadAppDomainException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct Char : System.IComparable, System.IComparable<char>, System.IConvertible, System.IEquatable<char>
    {
        public const char MaxValue = '\uFFFF';
        public const char MinValue = '\0';
        public int CompareTo(System.Char value) { throw null; }
        public int CompareTo(object value) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
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
        public static bool TryParse(string s, out System.Char result) { result = default(char); throw null; }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
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
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class CLSCompliantAttribute : System.Attribute
    {
        public CLSCompliantAttribute(bool isCompliant) { }
        public bool IsCompliant { get { throw null; } }
    }
    public delegate int Comparison<in T>(T x, T y);
    public static partial class Console
    {
        [System.Security.SecuritySafeCriticalAttribute]
        public static void Write(string s) { }
        public static void WriteLine() { }
        public static void WriteLine(string s) { }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public abstract partial class ContextBoundObject
    {
        protected ContextBoundObject() { }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class ContextMarshalException : System.SystemException
    {
        public ContextMarshalException() { }
        public ContextMarshalException(string message) { }
        public ContextMarshalException(string message, System.Exception inner) { }
        protected ContextMarshalException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(256), Inherited=false)]
    [System.ObsoleteAttribute("ContextStaticAttribute is not supported in this release. It has been left in so that legacy tools can be used with this release, but it cannot be used in your code.", true)]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class ContextStaticAttribute : System.Attribute
    {
        public ContextStaticAttribute() { }
    }
    public static partial class Convert
    {
        public static readonly object DBNull;
        public static object ChangeType(object value, System.Type conversionType) { throw null; }
        public static object ChangeType(object value, System.Type conversionType, System.IFormatProvider provider) { throw null; }
        public static object ChangeType(object value, System.TypeCode typeCode) { throw null; }
        public static object ChangeType(object value, System.TypeCode typeCode, System.IFormatProvider provider) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public static byte[] FromBase64CharArray(char[] inArray, int offset, int length) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public static byte[] FromBase64String(string s) { throw null; }
        public static System.TypeCode GetTypeCode(object value) { throw null; }
        public static bool IsDBNull(object value) { throw null; }
        public static int ToBase64CharArray(byte[] inArray, int offsetIn, int length, char[] outArray, int offsetOut) { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        [System.Security.SecuritySafeCriticalAttribute]
        public static int ToBase64CharArray(byte[] inArray, int offsetIn, int length, char[] outArray, int offsetOut, System.Base64FormattingOptions options) { throw null; }
        public static string ToBase64String(byte[] inArray) { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public static string ToBase64String(byte[] inArray, System.Base64FormattingOptions options) { throw null; }
        public static string ToBase64String(byte[] inArray, int offset, int length) { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        [System.Security.SecuritySafeCriticalAttribute]
        public static string ToBase64String(byte[] inArray, int offset, int length, System.Base64FormattingOptions options) { throw null; }
        public static bool ToBoolean(bool value) { throw null; }
        public static bool ToBoolean(byte value) { throw null; }
        public static bool ToBoolean(char value) { throw null; }
        public static bool ToBoolean(System.DateTime value) { throw null; }
        public static bool ToBoolean(decimal value) { throw null; }
        public static bool ToBoolean(double value) { throw null; }
        public static bool ToBoolean(short value) { throw null; }
        public static bool ToBoolean(int value) { throw null; }
        public static bool ToBoolean(long value) { throw null; }
        public static bool ToBoolean(object value) { throw null; }
        public static bool ToBoolean(object value, System.IFormatProvider provider) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static bool ToBoolean(sbyte value) { throw null; }
        public static bool ToBoolean(float value) { throw null; }
        public static bool ToBoolean(string value) { throw null; }
        public static bool ToBoolean(string value, System.IFormatProvider provider) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static bool ToBoolean(ushort value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static bool ToBoolean(uint value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static bool ToBoolean(ulong value) { throw null; }
        public static byte ToByte(bool value) { throw null; }
        public static byte ToByte(byte value) { throw null; }
        public static byte ToByte(char value) { throw null; }
        public static byte ToByte(System.DateTime value) { throw null; }
        public static byte ToByte(decimal value) { throw null; }
        public static byte ToByte(double value) { throw null; }
        public static byte ToByte(short value) { throw null; }
        public static byte ToByte(int value) { throw null; }
        public static byte ToByte(long value) { throw null; }
        public static byte ToByte(object value) { throw null; }
        public static byte ToByte(object value, System.IFormatProvider provider) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static byte ToByte(sbyte value) { throw null; }
        public static byte ToByte(float value) { throw null; }
        public static byte ToByte(string value) { throw null; }
        public static byte ToByte(string value, System.IFormatProvider provider) { throw null; }
        public static byte ToByte(string value, int fromBase) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static byte ToByte(ushort value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static byte ToByte(uint value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static byte ToByte(ulong value) { throw null; }
        public static char ToChar(bool value) { throw null; }
        public static char ToChar(byte value) { throw null; }
        public static char ToChar(char value) { throw null; }
        public static char ToChar(System.DateTime value) { throw null; }
        public static char ToChar(decimal value) { throw null; }
        public static char ToChar(double value) { throw null; }
        public static char ToChar(short value) { throw null; }
        public static char ToChar(int value) { throw null; }
        public static char ToChar(long value) { throw null; }
        public static char ToChar(object value) { throw null; }
        public static char ToChar(object value, System.IFormatProvider provider) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static char ToChar(sbyte value) { throw null; }
        public static char ToChar(float value) { throw null; }
        public static char ToChar(string value) { throw null; }
        public static char ToChar(string value, System.IFormatProvider provider) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static char ToChar(ushort value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static char ToChar(uint value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static char ToChar(ulong value) { throw null; }
        public static System.DateTime ToDateTime(bool value) { throw null; }
        public static System.DateTime ToDateTime(byte value) { throw null; }
        public static System.DateTime ToDateTime(char value) { throw null; }
        public static System.DateTime ToDateTime(System.DateTime value) { throw null; }
        public static System.DateTime ToDateTime(decimal value) { throw null; }
        public static System.DateTime ToDateTime(double value) { throw null; }
        public static System.DateTime ToDateTime(short value) { throw null; }
        public static System.DateTime ToDateTime(int value) { throw null; }
        public static System.DateTime ToDateTime(long value) { throw null; }
        public static System.DateTime ToDateTime(object value) { throw null; }
        public static System.DateTime ToDateTime(object value, System.IFormatProvider provider) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.DateTime ToDateTime(sbyte value) { throw null; }
        public static System.DateTime ToDateTime(float value) { throw null; }
        public static System.DateTime ToDateTime(string value) { throw null; }
        public static System.DateTime ToDateTime(string value, System.IFormatProvider provider) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.DateTime ToDateTime(ushort value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.DateTime ToDateTime(uint value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.DateTime ToDateTime(ulong value) { throw null; }
        public static decimal ToDecimal(bool value) { throw null; }
        public static decimal ToDecimal(byte value) { throw null; }
        public static decimal ToDecimal(char value) { throw null; }
        public static decimal ToDecimal(System.DateTime value) { throw null; }
        public static decimal ToDecimal(decimal value) { throw null; }
        public static decimal ToDecimal(double value) { throw null; }
        public static decimal ToDecimal(short value) { throw null; }
        public static decimal ToDecimal(int value) { throw null; }
        public static decimal ToDecimal(long value) { throw null; }
        public static decimal ToDecimal(object value) { throw null; }
        public static decimal ToDecimal(object value, System.IFormatProvider provider) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static decimal ToDecimal(sbyte value) { throw null; }
        public static decimal ToDecimal(float value) { throw null; }
        public static decimal ToDecimal(string value) { throw null; }
        public static decimal ToDecimal(string value, System.IFormatProvider provider) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static decimal ToDecimal(ushort value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static decimal ToDecimal(uint value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static decimal ToDecimal(ulong value) { throw null; }
        public static double ToDouble(bool value) { throw null; }
        public static double ToDouble(byte value) { throw null; }
        public static double ToDouble(char value) { throw null; }
        public static double ToDouble(System.DateTime value) { throw null; }
        public static double ToDouble(decimal value) { throw null; }
        public static double ToDouble(double value) { throw null; }
        public static double ToDouble(short value) { throw null; }
        public static double ToDouble(int value) { throw null; }
        public static double ToDouble(long value) { throw null; }
        public static double ToDouble(object value) { throw null; }
        public static double ToDouble(object value, System.IFormatProvider provider) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static double ToDouble(sbyte value) { throw null; }
        public static double ToDouble(float value) { throw null; }
        public static double ToDouble(string value) { throw null; }
        public static double ToDouble(string value, System.IFormatProvider provider) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static double ToDouble(ushort value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static double ToDouble(uint value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static double ToDouble(ulong value) { throw null; }
        public static short ToInt16(bool value) { throw null; }
        public static short ToInt16(byte value) { throw null; }
        public static short ToInt16(char value) { throw null; }
        public static short ToInt16(System.DateTime value) { throw null; }
        public static short ToInt16(decimal value) { throw null; }
        public static short ToInt16(double value) { throw null; }
        public static short ToInt16(short value) { throw null; }
        public static short ToInt16(int value) { throw null; }
        public static short ToInt16(long value) { throw null; }
        public static short ToInt16(object value) { throw null; }
        public static short ToInt16(object value, System.IFormatProvider provider) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static short ToInt16(sbyte value) { throw null; }
        public static short ToInt16(float value) { throw null; }
        public static short ToInt16(string value) { throw null; }
        public static short ToInt16(string value, System.IFormatProvider provider) { throw null; }
        public static short ToInt16(string value, int fromBase) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static short ToInt16(ushort value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static short ToInt16(uint value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static short ToInt16(ulong value) { throw null; }
        public static int ToInt32(bool value) { throw null; }
        public static int ToInt32(byte value) { throw null; }
        public static int ToInt32(char value) { throw null; }
        public static int ToInt32(System.DateTime value) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public static int ToInt32(decimal value) { throw null; }
        public static int ToInt32(double value) { throw null; }
        public static int ToInt32(short value) { throw null; }
        public static int ToInt32(int value) { throw null; }
        public static int ToInt32(long value) { throw null; }
        public static int ToInt32(object value) { throw null; }
        public static int ToInt32(object value, System.IFormatProvider provider) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static int ToInt32(sbyte value) { throw null; }
        public static int ToInt32(float value) { throw null; }
        public static int ToInt32(string value) { throw null; }
        public static int ToInt32(string value, System.IFormatProvider provider) { throw null; }
        public static int ToInt32(string value, int fromBase) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static int ToInt32(ushort value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static int ToInt32(uint value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static int ToInt32(ulong value) { throw null; }
        public static long ToInt64(bool value) { throw null; }
        public static long ToInt64(byte value) { throw null; }
        public static long ToInt64(char value) { throw null; }
        public static long ToInt64(System.DateTime value) { throw null; }
        public static long ToInt64(decimal value) { throw null; }
        public static long ToInt64(double value) { throw null; }
        public static long ToInt64(short value) { throw null; }
        public static long ToInt64(int value) { throw null; }
        public static long ToInt64(long value) { throw null; }
        public static long ToInt64(object value) { throw null; }
        public static long ToInt64(object value, System.IFormatProvider provider) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static long ToInt64(sbyte value) { throw null; }
        public static long ToInt64(float value) { throw null; }
        public static long ToInt64(string value) { throw null; }
        public static long ToInt64(string value, System.IFormatProvider provider) { throw null; }
        public static long ToInt64(string value, int fromBase) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static long ToInt64(ushort value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static long ToInt64(uint value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static long ToInt64(ulong value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static sbyte ToSByte(bool value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static sbyte ToSByte(byte value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static sbyte ToSByte(char value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static sbyte ToSByte(System.DateTime value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static sbyte ToSByte(decimal value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static sbyte ToSByte(double value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static sbyte ToSByte(short value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static sbyte ToSByte(int value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static sbyte ToSByte(long value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static sbyte ToSByte(object value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static sbyte ToSByte(object value, System.IFormatProvider provider) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static sbyte ToSByte(sbyte value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static sbyte ToSByte(float value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static sbyte ToSByte(string value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static sbyte ToSByte(string value, System.IFormatProvider provider) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static sbyte ToSByte(string value, int fromBase) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static sbyte ToSByte(ushort value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static sbyte ToSByte(uint value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static sbyte ToSByte(ulong value) { throw null; }
        public static float ToSingle(bool value) { throw null; }
        public static float ToSingle(byte value) { throw null; }
        public static float ToSingle(char value) { throw null; }
        public static float ToSingle(System.DateTime value) { throw null; }
        public static float ToSingle(decimal value) { throw null; }
        public static float ToSingle(double value) { throw null; }
        public static float ToSingle(short value) { throw null; }
        public static float ToSingle(int value) { throw null; }
        public static float ToSingle(long value) { throw null; }
        public static float ToSingle(object value) { throw null; }
        public static float ToSingle(object value, System.IFormatProvider provider) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static float ToSingle(sbyte value) { throw null; }
        public static float ToSingle(float value) { throw null; }
        public static float ToSingle(string value) { throw null; }
        public static float ToSingle(string value, System.IFormatProvider provider) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static float ToSingle(ushort value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static float ToSingle(uint value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static float ToSingle(ulong value) { throw null; }
        public static string ToString(bool value) { throw null; }
        public static string ToString(bool value, System.IFormatProvider provider) { throw null; }
        public static string ToString(byte value) { throw null; }
        public static string ToString(byte value, System.IFormatProvider provider) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public static string ToString(byte value, int toBase) { throw null; }
        public static string ToString(char value) { throw null; }
        public static string ToString(char value, System.IFormatProvider provider) { throw null; }
        public static string ToString(System.DateTime value) { throw null; }
        public static string ToString(System.DateTime value, System.IFormatProvider provider) { throw null; }
        public static string ToString(decimal value) { throw null; }
        public static string ToString(decimal value, System.IFormatProvider provider) { throw null; }
        public static string ToString(double value) { throw null; }
        public static string ToString(double value, System.IFormatProvider provider) { throw null; }
        public static string ToString(short value) { throw null; }
        public static string ToString(short value, System.IFormatProvider provider) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public static string ToString(short value, int toBase) { throw null; }
        public static string ToString(int value) { throw null; }
        public static string ToString(int value, System.IFormatProvider provider) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public static string ToString(int value, int toBase) { throw null; }
        public static string ToString(long value) { throw null; }
        public static string ToString(long value, System.IFormatProvider provider) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public static string ToString(long value, int toBase) { throw null; }
        public static string ToString(object value) { throw null; }
        public static string ToString(object value, System.IFormatProvider provider) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static string ToString(sbyte value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static string ToString(sbyte value, System.IFormatProvider provider) { throw null; }
        public static string ToString(float value) { throw null; }
        public static string ToString(float value, System.IFormatProvider provider) { throw null; }
        public static string ToString(string value) { throw null; }
        public static string ToString(string value, System.IFormatProvider provider) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static string ToString(ushort value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static string ToString(ushort value, System.IFormatProvider provider) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static string ToString(uint value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static string ToString(uint value, System.IFormatProvider provider) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static string ToString(ulong value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static string ToString(ulong value, System.IFormatProvider provider) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ushort ToUInt16(bool value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ushort ToUInt16(byte value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ushort ToUInt16(char value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ushort ToUInt16(System.DateTime value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ushort ToUInt16(decimal value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ushort ToUInt16(double value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ushort ToUInt16(short value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ushort ToUInt16(int value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ushort ToUInt16(long value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ushort ToUInt16(object value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ushort ToUInt16(object value, System.IFormatProvider provider) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ushort ToUInt16(sbyte value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ushort ToUInt16(float value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ushort ToUInt16(string value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ushort ToUInt16(string value, System.IFormatProvider provider) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ushort ToUInt16(string value, int fromBase) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ushort ToUInt16(ushort value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ushort ToUInt16(uint value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ushort ToUInt16(ulong value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static uint ToUInt32(bool value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static uint ToUInt32(byte value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static uint ToUInt32(char value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static uint ToUInt32(System.DateTime value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static uint ToUInt32(decimal value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static uint ToUInt32(double value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static uint ToUInt32(short value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static uint ToUInt32(int value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static uint ToUInt32(long value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static uint ToUInt32(object value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static uint ToUInt32(object value, System.IFormatProvider provider) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static uint ToUInt32(sbyte value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static uint ToUInt32(float value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static uint ToUInt32(string value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static uint ToUInt32(string value, System.IFormatProvider provider) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static uint ToUInt32(string value, int fromBase) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static uint ToUInt32(ushort value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static uint ToUInt32(uint value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static uint ToUInt32(ulong value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ulong ToUInt64(bool value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ulong ToUInt64(byte value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ulong ToUInt64(char value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ulong ToUInt64(System.DateTime value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ulong ToUInt64(decimal value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ulong ToUInt64(double value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ulong ToUInt64(short value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ulong ToUInt64(int value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ulong ToUInt64(long value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ulong ToUInt64(object value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ulong ToUInt64(object value, System.IFormatProvider provider) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ulong ToUInt64(sbyte value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ulong ToUInt64(float value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ulong ToUInt64(string value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ulong ToUInt64(string value, System.IFormatProvider provider) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ulong ToUInt64(string value, int fromBase) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ulong ToUInt64(ushort value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ulong ToUInt64(uint value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ulong ToUInt64(ulong value) { throw null; }
    }
    public delegate TOutput Converter<in TInput, out TOutput>(TInput input);
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class DataMisalignedException : System.SystemException
    {
        public DataMisalignedException() { }
        public DataMisalignedException(string message) { }
        public DataMisalignedException(string message, System.Exception innerException) { }
    }
    public partial struct DateTime : System.IComparable, System.IComparable<System.DateTime>, System.IConvertible, System.IEquatable<System.DateTime>, System.IFormattable, System.Runtime.Serialization.ISerializable
    {
        public static readonly System.DateTime MaxValue;
        public static readonly System.DateTime MinValue;
        public DateTime(int year, int month, int day) { throw null;}
        public DateTime(int year, int month, int day, System.Globalization.Calendar calendar) { throw null;}
        public DateTime(int year, int month, int day, int hour, int minute, int second) { throw null;}
        public DateTime(int year, int month, int day, int hour, int minute, int second, System.DateTimeKind kind) { throw null;}
        public DateTime(int year, int month, int day, int hour, int minute, int second, System.Globalization.Calendar calendar) { throw null;}
        public DateTime(int year, int month, int day, int hour, int minute, int second, int millisecond) { throw null;}
        public DateTime(int year, int month, int day, int hour, int minute, int second, int millisecond, System.DateTimeKind kind) { throw null;}
        public DateTime(int year, int month, int day, int hour, int minute, int second, int millisecond, System.Globalization.Calendar calendar) { throw null;}
        public DateTime(int year, int month, int day, int hour, int minute, int second, int millisecond, System.Globalization.Calendar calendar, System.DateTimeKind kind) { throw null;}
        public DateTime(long ticks) { throw null;}
        public DateTime(long ticks, System.DateTimeKind kind) { throw null;}
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
        public static System.DateTime UtcNow { [System.Security.SecuritySafeCriticalAttribute]get { throw null; } }
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
        public static System.DateTime Parse(string s) { throw null; }
        public static System.DateTime Parse(string s, System.IFormatProvider provider) { throw null; }
        public static System.DateTime Parse(string s, System.IFormatProvider provider, System.Globalization.DateTimeStyles styles) { throw null; }
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
        [System.Security.SecurityCriticalAttribute]
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
        public static bool TryParse(string s, out System.DateTime result) { result = default(System.DateTime); throw null; }
        public static bool TryParse(string s, System.IFormatProvider provider, System.Globalization.DateTimeStyles styles, out System.DateTime result) { result = default(System.DateTime); throw null; }
        public static bool TryParseExact(string s, string format, System.IFormatProvider provider, System.Globalization.DateTimeStyles style, out System.DateTime result) { result = default(System.DateTime); throw null; }
        public static bool TryParseExact(string s, string[] formats, System.IFormatProvider provider, System.Globalization.DateTimeStyles style, out System.DateTime result) { result = default(System.DateTime); throw null; }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public enum DateTimeKind
    {
        Local = 2,
        Unspecified = 0,
        Utc = 1,
    }
    public partial struct DateTimeOffset : System.IComparable, System.IComparable<System.DateTimeOffset>, System.IEquatable<System.DateTimeOffset>, System.IFormattable, System.Runtime.Serialization.IDeserializationCallback, System.Runtime.Serialization.ISerializable
    {
        public static readonly System.DateTimeOffset MaxValue;
        public static readonly System.DateTimeOffset MinValue;
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
        public static implicit operator System.DateTimeOffset(System.DateTime dateTime) { throw null; }
        public static bool operator !=(System.DateTimeOffset left, System.DateTimeOffset right) { throw null; }
        public static bool operator <(System.DateTimeOffset left, System.DateTimeOffset right) { throw null; }
        public static bool operator <=(System.DateTimeOffset left, System.DateTimeOffset right) { throw null; }
        public static System.TimeSpan operator -(System.DateTimeOffset left, System.DateTimeOffset right) { throw null; }
        public static System.DateTimeOffset operator -(System.DateTimeOffset dateTimeOffset, System.TimeSpan timeSpan) { throw null; }
        public static System.DateTimeOffset Parse(string input) { throw null; }
        public static System.DateTimeOffset Parse(string input, System.IFormatProvider formatProvider) { throw null; }
        public static System.DateTimeOffset Parse(string input, System.IFormatProvider formatProvider, System.Globalization.DateTimeStyles styles) { throw null; }
        public static System.DateTimeOffset ParseExact(string input, string format, System.IFormatProvider formatProvider) { throw null; }
        public static System.DateTimeOffset ParseExact(string input, string format, System.IFormatProvider formatProvider, System.Globalization.DateTimeStyles styles) { throw null; }
        public static System.DateTimeOffset ParseExact(string input, string[] formats, System.IFormatProvider formatProvider, System.Globalization.DateTimeStyles styles) { throw null; }
        public System.TimeSpan Subtract(System.DateTimeOffset value) { throw null; }
        public System.DateTimeOffset Subtract(System.TimeSpan value) { throw null; }
        int System.IComparable.CompareTo(object obj) { throw null; }
        void System.Runtime.Serialization.IDeserializationCallback.OnDeserialization(object sender) { }
        [System.Security.SecurityCriticalAttribute]
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
        public static bool TryParse(string input, out System.DateTimeOffset result) { result = default(System.DateTimeOffset); throw null; }
        public static bool TryParse(string input, System.IFormatProvider formatProvider, System.Globalization.DateTimeStyles styles, out System.DateTimeOffset result) { result = default(System.DateTimeOffset); throw null; }
        public static bool TryParseExact(string input, string format, System.IFormatProvider formatProvider, System.Globalization.DateTimeStyles styles, out System.DateTimeOffset result) { result = default(System.DateTimeOffset); throw null; }
        public static bool TryParseExact(string input, string[] formats, System.IFormatProvider formatProvider, System.Globalization.DateTimeStyles styles, out System.DateTimeOffset result) { result = default(System.DateTimeOffset); throw null; }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
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
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class DBNull : System.IConvertible, System.Runtime.Serialization.ISerializable
    {
        internal DBNull() { }
        public static readonly System.DBNull Value;
        [System.Security.SecurityCriticalAttribute]
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
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct Decimal : System.IComparable, System.IComparable<decimal>, System.IConvertible, System.IEquatable<decimal>, System.IFormattable, System.Runtime.Serialization.IDeserializationCallback
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
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)]
        [System.Security.SecuritySafeCriticalAttribute]
        public Decimal(double value) { throw null; }
        public Decimal(int value) { throw null; }
        public Decimal(int lo, int mid, int hi, bool isNegative, byte scale) { throw null; }
        public Decimal(int[] bits) { throw null; }
        public Decimal(long value) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)]
        [System.Security.SecuritySafeCriticalAttribute]
        public Decimal(float value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public Decimal(uint value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public Decimal(ulong value) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public static System.Decimal Add(System.Decimal d1, System.Decimal d2) { throw null; }
        public static System.Decimal Ceiling(System.Decimal d) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public static int Compare(System.Decimal d1, System.Decimal d2) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public int CompareTo(System.Decimal value) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public int CompareTo(object value) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public static System.Decimal Divide(System.Decimal d1, System.Decimal d2) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public bool Equals(System.Decimal value) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public static bool Equals(System.Decimal d1, System.Decimal d2) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public override bool Equals(object value) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public static System.Decimal Floor(System.Decimal d) { throw null; }
        public static System.Decimal FromOACurrency(long cy) { throw null; }
        public static int[] GetBits(System.Decimal d) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)]
        [System.Security.SecuritySafeCriticalAttribute]
        public override int GetHashCode() { throw null; }
        public System.TypeCode GetTypeCode() { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public static System.Decimal Multiply(System.Decimal d1, System.Decimal d2) { throw null; }
        public static System.Decimal Negate(System.Decimal d) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public static System.Decimal operator +(System.Decimal d1, System.Decimal d2) { throw null; }
        public static System.Decimal operator --(System.Decimal d) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public static System.Decimal operator /(System.Decimal d1, System.Decimal d2) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public static bool operator ==(System.Decimal d1, System.Decimal d2) { throw null; }
        public static explicit operator byte(System.Decimal value) { throw null; }
        public static explicit operator char(System.Decimal value) { throw null; }
        public static explicit operator double(System.Decimal value) { throw null; }
        public static explicit operator short(System.Decimal value) { throw null; }
        public static explicit operator int(System.Decimal value) { throw null; }
        public static explicit operator long(System.Decimal value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator sbyte(System.Decimal value) { throw null; }
        public static explicit operator float(System.Decimal value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator ushort(System.Decimal value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator uint(System.Decimal value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator ulong(System.Decimal value) { throw null; }
        public static explicit operator System.Decimal(double value) { throw null; }
        public static explicit operator System.Decimal(float value) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public static bool operator >(System.Decimal d1, System.Decimal d2) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public static bool operator >=(System.Decimal d1, System.Decimal d2) { throw null; }
        public static implicit operator System.Decimal(byte value) { throw null; }
        public static implicit operator System.Decimal(char value) { throw null; }
        public static implicit operator System.Decimal(short value) { throw null; }
        public static implicit operator System.Decimal(int value) { throw null; }
        public static implicit operator System.Decimal(long value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static implicit operator System.Decimal(sbyte value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static implicit operator System.Decimal(ushort value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static implicit operator System.Decimal(uint value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static implicit operator System.Decimal(ulong value) { throw null; }
        public static System.Decimal operator ++(System.Decimal d) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public static bool operator !=(System.Decimal d1, System.Decimal d2) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public static bool operator <(System.Decimal d1, System.Decimal d2) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public static bool operator <=(System.Decimal d1, System.Decimal d2) { throw null; }
        public static System.Decimal operator %(System.Decimal d1, System.Decimal d2) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public static System.Decimal operator *(System.Decimal d1, System.Decimal d2) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public static System.Decimal operator -(System.Decimal d1, System.Decimal d2) { throw null; }
        public static System.Decimal operator -(System.Decimal d) { throw null; }
        public static System.Decimal operator +(System.Decimal d) { throw null; }
        public static System.Decimal Parse(string s) { throw null; }
        public static System.Decimal Parse(string s, System.Globalization.NumberStyles style) { throw null; }
        public static System.Decimal Parse(string s, System.Globalization.NumberStyles style, System.IFormatProvider provider) { throw null; }
        public static System.Decimal Parse(string s, System.IFormatProvider provider) { throw null; }
        public static System.Decimal Remainder(System.Decimal d1, System.Decimal d2) { throw null; }
        public static System.Decimal Round(System.Decimal d) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public static System.Decimal Round(System.Decimal d, int decimals) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public static System.Decimal Round(System.Decimal d, int decimals, System.MidpointRounding mode) { throw null; }
        public static System.Decimal Round(System.Decimal d, System.MidpointRounding mode) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
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
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)]
        [System.Security.SecuritySafeCriticalAttribute]
        public static double ToDouble(System.Decimal d) { throw null; }
        public static short ToInt16(System.Decimal value) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public static int ToInt32(System.Decimal d) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public static long ToInt64(System.Decimal d) { throw null; }
        public static long ToOACurrency(System.Decimal value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static sbyte ToSByte(System.Decimal value) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)]
        [System.Security.SecuritySafeCriticalAttribute]
        public static float ToSingle(System.Decimal d) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public override string ToString() { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public string ToString(System.IFormatProvider provider) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public string ToString(string format) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public string ToString(string format, System.IFormatProvider provider) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ushort ToUInt16(System.Decimal value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        [System.Security.SecuritySafeCriticalAttribute]
        public static uint ToUInt32(System.Decimal d) { throw null; }
        [System.CLSCompliantAttribute(false)]
        [System.Security.SecuritySafeCriticalAttribute]
        public static ulong ToUInt64(System.Decimal d) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public static System.Decimal Truncate(System.Decimal d) { throw null; }
        public static bool TryParse(string s, out System.Decimal result) { result = default(decimal); throw null; }
        public static bool TryParse(string s, System.Globalization.NumberStyles style, System.IFormatProvider provider, out System.Decimal result) { result = default(decimal); throw null; }
    }
    [System.Runtime.InteropServices.ClassInterfaceAttribute((System.Runtime.InteropServices.ClassInterfaceType)(2))]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public abstract partial class Delegate : System.ICloneable, System.Runtime.Serialization.ISerializable
    {
        [System.Security.SecuritySafeCriticalAttribute]
        protected Delegate(object target, string method) { }
        [System.Security.SecuritySafeCriticalAttribute]
        protected Delegate(System.Type target, string method) { }
        public System.Reflection.MethodInfo Method { get { throw null; } }
        public object Target { get { throw null; } }
        public virtual object Clone() { throw null; }
        public static System.Delegate Combine(System.Delegate a, System.Delegate b) { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(true)]
        public static System.Delegate Combine(params System.Delegate[] delegates) { throw null; }
        protected virtual System.Delegate CombineImpl(System.Delegate d) { throw null; }
        public static System.Delegate CreateDelegate(System.Type type, object firstArgument, System.Reflection.MethodInfo method) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)][System.Security.SecuritySafeCriticalAttribute]
        public static System.Delegate CreateDelegate(System.Type type, object firstArgument, System.Reflection.MethodInfo method, bool throwOnBindFailure) { throw null; }
        public static System.Delegate CreateDelegate(System.Type type, object target, string method) { throw null; }
        public static System.Delegate CreateDelegate(System.Type type, object target, string method, bool ignoreCase) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public static System.Delegate CreateDelegate(System.Type type, object target, string method, bool ignoreCase, bool throwOnBindFailure) { throw null; }
        public static System.Delegate CreateDelegate(System.Type type, System.Reflection.MethodInfo method) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)][System.Security.SecuritySafeCriticalAttribute]
        public static System.Delegate CreateDelegate(System.Type type, System.Reflection.MethodInfo method, bool throwOnBindFailure) { throw null; }
        public static System.Delegate CreateDelegate(System.Type type, System.Type target, string method) { throw null; }
        public static System.Delegate CreateDelegate(System.Type type, System.Type target, string method, bool ignoreCase) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public static System.Delegate CreateDelegate(System.Type type, System.Type target, string method, bool ignoreCase, bool throwOnBindFailure) { throw null; }
        public object DynamicInvoke(params object[] args) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        protected virtual object DynamicInvokeImpl(object[] args) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public override bool Equals(object obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public virtual System.Delegate[] GetInvocationList() { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        protected virtual System.Reflection.MethodInfo GetMethodImpl() { throw null; }
        [System.Security.SecurityCriticalAttribute]
        public virtual void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public static bool operator ==(System.Delegate d1, System.Delegate d2) { throw null; }
        public static bool operator !=(System.Delegate d1, System.Delegate d2) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public static System.Delegate Remove(System.Delegate source, System.Delegate value) { throw null; }
        public static System.Delegate RemoveAll(System.Delegate source, System.Delegate value) { throw null; }
        protected virtual System.Delegate RemoveImpl(System.Delegate d) { throw null; }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class DivideByZeroException : System.ArithmeticException
    {
        public DivideByZeroException() { }
        public DivideByZeroException(string message) { }
        public DivideByZeroException(string message, System.Exception innerException) { }
        protected DivideByZeroException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class DllNotFoundException : System.TypeLoadException
    {
        public DllNotFoundException() { }
        public DllNotFoundException(string message) { }
        public DllNotFoundException(string message, System.Exception inner) { }
        protected DllNotFoundException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct Double : System.IComparable, System.IComparable<double>, System.IConvertible, System.IEquatable<double>, System.IFormattable
    {
        public const double Epsilon = 4.94065645841247E-324;
        public const double MaxValue = 1.7976931348623157E+308;
        public const double MinValue = -1.7976931348623157E+308;
        public const double NaN = 0.0 / 0.0;
        public const double NegativeInfinity = -1.0 / 0.0;
        public const double PositiveInfinity = 1.0 / 0.0;
        public int CompareTo(System.Double value) { throw null; }
        public int CompareTo(object value) { throw null; }
        public bool Equals(System.Double obj) { throw null; }
        public override bool Equals(object obj) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public override int GetHashCode() { throw null; }
        public System.TypeCode GetTypeCode() { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public static bool IsInfinity(System.Double d) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public static bool IsNaN(System.Double d) { throw null; }
        public static bool IsNegativeInfinity(System.Double d) { throw null; }
        public static bool IsPositiveInfinity(System.Double d) { throw null; }
        public static bool operator ==(System.Double left, System.Double right) { throw null; }
        public static bool operator >(System.Double left, System.Double right) { throw null; }
        public static bool operator >=(System.Double left, System.Double right) { throw null; }
        public static bool operator !=(System.Double left, System.Double right) { throw null; }
        public static bool operator <(System.Double left, System.Double right) { throw null; }
        public static bool operator <=(System.Double left, System.Double right) { throw null; }
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
        [System.Security.SecuritySafeCriticalAttribute]
        public override string ToString() { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public string ToString(System.IFormatProvider provider) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public string ToString(string format) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public string ToString(string format, System.IFormatProvider provider) { throw null; }
        public static bool TryParse(string s, out System.Double result) { result = default(double); throw null; }
        public static bool TryParse(string s, System.Globalization.NumberStyles style, System.IFormatProvider provider, out System.Double result) { result = default(double); throw null; }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class DuplicateWaitObjectException : System.ArgumentException
    {
        public DuplicateWaitObjectException() { }
        public DuplicateWaitObjectException(string parameterName) { }
        public DuplicateWaitObjectException(string message, System.Exception innerException) { }
        public DuplicateWaitObjectException(string parameterName, string message) { }
        protected DuplicateWaitObjectException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class EntryPointNotFoundException : System.TypeLoadException
    {
        public EntryPointNotFoundException() { }
        public EntryPointNotFoundException(string message) { }
        public EntryPointNotFoundException(string message, System.Exception inner) { }
        protected EntryPointNotFoundException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public abstract partial class Enum : System.ValueType, System.IComparable, System.IConvertible, System.IFormattable
    {
        protected Enum() { }
        [System.Security.SecuritySafeCriticalAttribute]
        public int CompareTo(object target) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)][System.Security.SecuritySafeCriticalAttribute]
        public override bool Equals(object obj) { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(true)]
        public static string Format(System.Type enumType, object value, string format) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public override int GetHashCode() { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(true)]
        public static string GetName(System.Type enumType, object value) { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(true)]
        public static string[] GetNames(System.Type enumType) { throw null; }
        public System.TypeCode GetTypeCode() { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(true)]
        public static System.Type GetUnderlyingType(System.Type enumType) { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(true)]
        public static System.Array GetValues(System.Type enumType) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public bool HasFlag(System.Enum flag) { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(true)]
        public static bool IsDefined(System.Type enumType, object value) { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(true)]
        public static object Parse(System.Type enumType, string value) { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(true)]
        public static object Parse(System.Type enumType, string value, bool ignoreCase) { throw null; }
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
        [System.Runtime.InteropServices.ComVisibleAttribute(true)]
        [System.Security.SecuritySafeCriticalAttribute]
        public static object ToObject(System.Type enumType, byte value) { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(true)]
        [System.Security.SecuritySafeCriticalAttribute]
        public static object ToObject(System.Type enumType, short value) { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(true)]
        [System.Security.SecuritySafeCriticalAttribute]
        public static object ToObject(System.Type enumType, int value) { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(true)]
        [System.Security.SecuritySafeCriticalAttribute]
        public static object ToObject(System.Type enumType, long value) { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(true)]
        public static object ToObject(System.Type enumType, object value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        [System.Runtime.InteropServices.ComVisibleAttribute(true)]
        [System.Security.SecuritySafeCriticalAttribute]
        public static object ToObject(System.Type enumType, sbyte value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        [System.Runtime.InteropServices.ComVisibleAttribute(true)]
        [System.Security.SecuritySafeCriticalAttribute]
        public static object ToObject(System.Type enumType, ushort value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        [System.Runtime.InteropServices.ComVisibleAttribute(true)]
        [System.Security.SecuritySafeCriticalAttribute]
        public static object ToObject(System.Type enumType, uint value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        [System.Runtime.InteropServices.ComVisibleAttribute(true)]
        [System.Security.SecuritySafeCriticalAttribute]
        public static object ToObject(System.Type enumType, ulong value) { throw null; }
        public override string ToString() { throw null; }
        [System.ObsoleteAttribute("The provider argument is not used. Please use ToString().")]
        public string ToString(System.IFormatProvider provider) { throw null; }
        public string ToString(string format) { throw null; }
        [System.ObsoleteAttribute("The provider argument is not used. Please use ToString(String).")]
        public string ToString(string format, System.IFormatProvider provider) { throw null; }
        public static bool TryParse<TEnum>(string value, out TEnum result) where TEnum : struct { result = default(TEnum); throw null; }
        public static bool TryParse<TEnum>(string value, bool ignoreCase, out TEnum result) where TEnum : struct { result = default(TEnum); throw null; }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public static partial class Environment
    {
        public static int CurrentManagedThreadId { get { throw null; } }
        public static int ExitCode { [System.Security.SecuritySafeCriticalAttribute][System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)]get { throw null; } [System.Security.SecuritySafeCriticalAttribute][System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)]set { } }
        public static bool HasShutdownStarted { [System.Security.SecuritySafeCriticalAttribute][System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)]get { throw null; } }
        public static string MachineName { [System.Security.SecuritySafeCriticalAttribute]get { throw null; } }
        public static string NewLine { get { throw null; } }
        public static System.OperatingSystem OSVersion { [System.Security.SecuritySafeCriticalAttribute]get { throw null; } }
        public static int ProcessorCount { [System.Security.SecuritySafeCriticalAttribute]get { throw null; } }
        public static string StackTrace { [System.Security.SecuritySafeCriticalAttribute]get { throw null; } }
        public static int TickCount { [System.Security.SecuritySafeCriticalAttribute][System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)]get { throw null; } }
        public static System.Version Version { get { throw null; } }
        [System.Security.SecuritySafeCriticalAttribute]
        [System.Security.Permissions.SecurityPermissionAttribute(System.Security.Permissions.SecurityAction.Demand, Flags=(System.Security.Permissions.SecurityPermissionFlag)(2))]
        public static void Exit(int exitCode) { }
        [System.Security.SecuritySafeCriticalAttribute]
        public static string ExpandEnvironmentVariables(string name) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)][System.Security.SecurityCriticalAttribute]
        public static void FailFast(string message) { }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)][System.Security.SecurityCriticalAttribute]
        public static void FailFast(string message, System.Exception exception) { }
        [System.Security.SecuritySafeCriticalAttribute]
        public static string[] GetCommandLineArgs() { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public static string GetEnvironmentVariable(string variable) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public static System.Collections.IDictionary GetEnvironmentVariables() { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public static void SetEnvironmentVariable(string variable, string value) { }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class EventArgs
    {
        public static readonly System.EventArgs Empty;
        public EventArgs() { }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public delegate void EventHandler(object sender, System.EventArgs e);
    public delegate void EventHandler<TEventArgs>(object sender, TEventArgs e);
    [System.Runtime.InteropServices.ClassInterfaceAttribute((System.Runtime.InteropServices.ClassInterfaceType)(0))]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class Exception : System.Runtime.Serialization.ISerializable
    {
        public Exception() { }
        [System.Security.SecuritySafeCriticalAttribute]
        protected Exception(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public Exception(string message) { }
        public Exception(string message, System.Exception innerException) { }
        public virtual System.Collections.IDictionary Data { [System.Security.SecuritySafeCriticalAttribute]get { throw null; } }
        public virtual string HelpLink { get { throw null; } set { } }
        public int HResult { get { throw null; } protected set { } }
        public System.Exception InnerException { get { throw null; } }
        public virtual string Message { get { throw null; } }
        public virtual string Source { [System.Security.SecurityCriticalAttribute]get { throw null; } [System.Security.SecurityCriticalAttribute]set { } }
        public virtual string StackTrace { [System.Security.SecuritySafeCriticalAttribute]get { throw null; } }
        public System.Reflection.MethodBase TargetSite { [System.Security.SecuritySafeCriticalAttribute]get { throw null; } }
        public virtual System.Exception GetBaseException() { throw null; }
        [System.Security.SecurityCriticalAttribute]
        public virtual void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public new System.Type GetType() { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public override string ToString() { throw null; }
    }
    [System.ObsoleteAttribute("This type previously indicated an unspecified fatal error in the runtime. The runtime no longer raises this exception so this type is obsolete.")]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class ExecutionEngineException : System.SystemException
    {
        public ExecutionEngineException() { }
        public ExecutionEngineException(string message) { }
        public ExecutionEngineException(string message, System.Exception innerException) { }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class FieldAccessException : System.MemberAccessException
    {
        public FieldAccessException() { }
        public FieldAccessException(string message) { }
        public FieldAccessException(string message, System.Exception inner) { }
        protected FieldAccessException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(16), Inherited=false)]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class FlagsAttribute : System.Attribute
    {
        public FlagsAttribute() { }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class FormatException : System.SystemException
    {
        public FormatException() { }
        public FormatException(string message) { }
        public FormatException(string message, System.Exception innerException) { }
        protected FormatException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
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
    [System.Runtime.CompilerServices.TypeForwardedFromAttribute("System.Core, Version=2.0.5.0, Culture=Neutral, PublicKeyToken=7cec85d7bea7798e")]
    public delegate TResult Func<out TResult>();
    [System.Runtime.CompilerServices.TypeForwardedFromAttribute("System.Core, Version=2.0.5.0, Culture=Neutral, PublicKeyToken=7cec85d7bea7798e")]
    public delegate TResult Func<in T, out TResult>(T arg);
    [System.Runtime.CompilerServices.TypeForwardedFromAttribute("System.Core, Version=2.0.5.0, Culture=Neutral, PublicKeyToken=7cec85d7bea7798e")]
    public delegate TResult Func<in T1, in T2, out TResult>(T1 arg1, T2 arg2);
    [System.Runtime.CompilerServices.TypeForwardedFromAttribute("System.Core, Version=2.0.5.0, Culture=Neutral, PublicKeyToken=7cec85d7bea7798e")]
    public delegate TResult Func<in T1, in T2, in T3, out TResult>(T1 arg1, T2 arg2, T3 arg3);
    [System.Runtime.CompilerServices.TypeForwardedFromAttribute("System.Core, Version=2.0.5.0, Culture=Neutral, PublicKeyToken=7cec85d7bea7798e")]
    public delegate TResult Func<in T1, in T2, in T3, in T4, out TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4);
    public delegate TResult Func<in T1, in T2, in T3, in T4, in T5, out TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5);
    public delegate TResult Func<in T1, in T2, in T3, in T4, in T5, in T6, out TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6);
    public delegate TResult Func<in T1, in T2, in T3, in T4, in T5, in T6, in T7, out TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7);
    public delegate TResult Func<in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, out TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8);
    public static partial class GC
    {
        public static int MaxGeneration { [System.Security.SecuritySafeCriticalAttribute]get { throw null; } }
        [System.Security.SecurityCriticalAttribute]
        public static void AddMemoryPressure(long bytesAllocated) { }
        [System.Security.SecurityCriticalAttribute]
        public static void CancelFullGCNotification() { }
        [System.Security.SecuritySafeCriticalAttribute]
        public static void Collect() { }
        public static void Collect(int generation) { }
        [System.Security.SecuritySafeCriticalAttribute]
        public static void Collect(int generation, System.GCCollectionMode mode) { }
        [System.Security.SecuritySafeCriticalAttribute]
        public static void Collect(int generation, System.GCCollectionMode mode, bool blocking) { }
        [System.Security.SecuritySafeCriticalAttribute]
        public static void Collect(int generation, System.GCCollectionMode mode, bool blocking, bool compacting) { }
        [System.Security.SecuritySafeCriticalAttribute]
        public static int CollectionCount(int generation) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)]
        [System.Security.SecuritySafeCriticalAttribute]
        public static int GetGeneration(object obj) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public static int GetGeneration(System.WeakReference wo) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public static long GetTotalMemory(bool forceFullCollection) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public static void KeepAlive(object obj) { }
        [System.Security.SecurityCriticalAttribute]
        public static void RegisterForFullGCNotification(int maxGenerationThreshold, int largeObjectHeapThreshold) { }
        [System.Security.SecurityCriticalAttribute]
        public static void RemoveMemoryPressure(long bytesAllocated) { }
        [System.Security.SecuritySafeCriticalAttribute]
        public static void ReRegisterForFinalize(object obj) { }
        [System.Security.SecuritySafeCriticalAttribute]
        public static void SuppressFinalize(object obj) { }
        [System.Security.SecurityCriticalAttribute]
        public static System.GCNotificationStatus WaitForFullGCApproach() { throw null; }
        [System.Security.SecurityCriticalAttribute]
        public static System.GCNotificationStatus WaitForFullGCApproach(int millisecondsTimeout) { throw null; }
        [System.Security.SecurityCriticalAttribute]
        public static System.GCNotificationStatus WaitForFullGCComplete() { throw null; }
        [System.Security.SecurityCriticalAttribute]
        public static System.GCNotificationStatus WaitForFullGCComplete(int millisecondsTimeout) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public static void WaitForPendingFinalizers() { }
    }
    public enum GCNotificationStatus
    {
        Canceled = 2,
        Failed = 1,
        NotApplicable = 4,
        Succeeded = 0,
        Timeout = 3,
    }
    public enum GCCollectionMode
    {
        Default = 0,
        Forced = 1,
        Optimized = 2,
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct Guid : System.IComparable, System.IComparable<System.Guid>, System.IEquatable<System.Guid>, System.IFormattable
    {
        public static readonly System.Guid Empty;
        public Guid(byte[] b) { throw null;}
        public Guid(int a, short b, short c, byte d, byte e, byte f, byte g, byte h, byte i, byte j, byte k) { throw null;}
        public Guid(int a, short b, short c, byte[] d) { throw null;}
        public Guid(string g) { throw null;}
        [System.CLSCompliantAttribute(false)]
        public Guid(uint a, ushort b, ushort c, byte d, byte e, byte f, byte g, byte h, byte i, byte j, byte k) { throw null;}
        public int CompareTo(System.Guid value) { throw null; }
        public int CompareTo(object value) { throw null; }
        public bool Equals(System.Guid g) { throw null; }
        public override bool Equals(object o) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public override int GetHashCode() { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public static System.Guid NewGuid() { throw null; }
        public static bool operator ==(System.Guid a, System.Guid b) { throw null; }
        public static bool operator !=(System.Guid a, System.Guid b) { throw null; }
        public static System.Guid Parse(string input) { throw null; }
        public static System.Guid ParseExact(string input, string format) { throw null; }
        public byte[] ToByteArray() { throw null; }
        public override string ToString() { throw null; }
        public string ToString(string format) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public string ToString(string format, System.IFormatProvider provider) { throw null; }
        public static bool TryParse(string input, out System.Guid result) { result = default(System.Guid); throw null; }
        public static bool TryParseExact(string input, string format, out System.Guid result) { result = default(System.Guid); throw null; }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial interface IAsyncResult
    {
        object AsyncState { get; }
        System.Threading.WaitHandle AsyncWaitHandle { get; }
        bool CompletedSynchronously { get; }
        bool IsCompleted { get; }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial interface ICloneable
    {
        object Clone();
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial interface IComparable
    {
        int CompareTo(object obj);
    }
    public partial interface IComparable<in T>
    {
        int CompareTo(T other);
    }
    [System.CLSCompliantAttribute(false)]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
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
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial interface ICustomFormatter
    {
        string Format(string format, object arg, System.IFormatProvider formatProvider);
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial interface IDisposable
    {
        void Dispose();
    }
    public partial interface IEquatable<T>
    {
        bool Equals(T other);
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial interface IFormatProvider
    {
        object GetFormat(System.Type formatType);
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial interface IFormattable
    {
        string ToString(string format, System.IFormatProvider formatProvider);
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
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
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct Int16 : System.IComparable, System.IComparable<short>, System.IConvertible, System.IEquatable<short>, System.IFormattable
    {
        public const short MaxValue = (short)32767;
        public const short MinValue = (short)-32768;
        public int CompareTo(System.Int16 value) { throw null; }
        public int CompareTo(object value) { throw null; }
        public bool Equals(System.Int16 obj) { throw null; }
        public override bool Equals(object obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public System.TypeCode GetTypeCode() { throw null; }
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
        [System.Security.SecuritySafeCriticalAttribute]
        public override string ToString() { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public string ToString(System.IFormatProvider provider) { throw null; }
        public string ToString(string format) { throw null; }
        public string ToString(string format, System.IFormatProvider provider) { throw null; }
        public static bool TryParse(string s, System.Globalization.NumberStyles style, System.IFormatProvider provider, out System.Int16 result) { result = default(short); throw null; }
        public static bool TryParse(string s, out System.Int16 result) { result = default(short); throw null; }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct Int32 : System.IComparable, System.IComparable<int>, System.IConvertible, System.IEquatable<int>, System.IFormattable
    {
        public const int MaxValue = 2147483647;
        public const int MinValue = -2147483648;
        public System.Int32 CompareTo(System.Int32 value) { throw null; }
        public System.Int32 CompareTo(object value) { throw null; }
        public bool Equals(System.Int32 obj) { throw null; }
        public override bool Equals(object obj) { throw null; }
        public override System.Int32 GetHashCode() { throw null; }
        public System.TypeCode GetTypeCode() { throw null; }
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
        [System.Security.SecuritySafeCriticalAttribute]
        public override string ToString() { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public string ToString(System.IFormatProvider provider) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public string ToString(string format) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public string ToString(string format, System.IFormatProvider provider) { throw null; }
        public static bool TryParse(string s, System.Globalization.NumberStyles style, System.IFormatProvider provider, out System.Int32 result) { result = default(int); throw null; }
        public static bool TryParse(string s, out System.Int32 result) { result = default(int); throw null; }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct Int64 : System.IComparable, System.IComparable<long>, System.IConvertible, System.IEquatable<long>, System.IFormattable
    {
        public const long MaxValue = (long)9223372036854775807;
        public const long MinValue = (long)-9223372036854775808;
        public int CompareTo(System.Int64 value) { throw null; }
        public int CompareTo(object value) { throw null; }
        public bool Equals(System.Int64 obj) { throw null; }
        public override bool Equals(object obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public System.TypeCode GetTypeCode() { throw null; }
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
        [System.Security.SecuritySafeCriticalAttribute]
        public override string ToString() { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public string ToString(System.IFormatProvider provider) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public string ToString(string format) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public string ToString(string format, System.IFormatProvider provider) { throw null; }
        public static bool TryParse(string s, System.Globalization.NumberStyles style, System.IFormatProvider provider, out System.Int64 result) { result = default(long); throw null; }
        public static bool TryParse(string s, out System.Int64 result) { result = default(long); throw null; }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct IntPtr : System.Runtime.Serialization.ISerializable
    {
        public static readonly System.IntPtr Zero;
        [System.Security.SecuritySafeCriticalAttribute]
        public IntPtr(int value) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public IntPtr(long value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        [System.Security.SecurityCriticalAttribute]
        public unsafe IntPtr(void* value) { throw null; }
        public static int Size { get { throw null; } }
        public static System.IntPtr Add(System.IntPtr pointer, int offset) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public override bool Equals(object obj) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public override int GetHashCode() { throw null; }
        public static System.IntPtr operator +(System.IntPtr pointer, int offset) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public static bool operator ==(System.IntPtr value1, System.IntPtr value2) { throw null; }
        public static explicit operator System.IntPtr(int value) { throw null; }
        public static explicit operator System.IntPtr(long value) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public static explicit operator int(System.IntPtr value) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public static explicit operator long(System.IntPtr value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        [System.Security.SecuritySafeCriticalAttribute]
        public unsafe static explicit operator void* (System.IntPtr value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        [System.Security.SecurityCriticalAttribute]
        public unsafe static explicit operator System.IntPtr(void* value) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public static bool operator !=(System.IntPtr value1, System.IntPtr value2) { throw null; }
        public static System.IntPtr operator -(System.IntPtr pointer, int offset) { throw null; }
        public static System.IntPtr Subtract(System.IntPtr pointer, int offset) { throw null; }
        [System.Security.SecurityCriticalAttribute]
        void System.Runtime.Serialization.ISerializable.GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        [System.Security.SecuritySafeCriticalAttribute]
        public int ToInt32() { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public long ToInt64() { throw null; }
        [System.CLSCompliantAttribute(false)]
        [System.Security.SecuritySafeCriticalAttribute]
        public unsafe void* ToPointer() { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public override string ToString() { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public string ToString(string format) { throw null; }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class InvalidCastException : System.SystemException
    {
        public InvalidCastException() { }
        public InvalidCastException(string message) { }
        public InvalidCastException(string message, System.Exception innerException) { }
        public InvalidCastException(string message, int errorCode) { }
        protected InvalidCastException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class InvalidOperationException : System.SystemException
    {
        public InvalidOperationException() { }
        public InvalidOperationException(string message) { }
        public InvalidOperationException(string message, System.Exception innerException) { }
        protected InvalidOperationException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class InvalidProgramException : System.SystemException
    {
        public InvalidProgramException() { }
        public InvalidProgramException(string message) { }
        public InvalidProgramException(string message, System.Exception inner) { }
    }
    [System.Runtime.CompilerServices.TypeForwardedFromAttribute("System.Core, Version=3.5.0.0, Culture=Neutral, PublicKeyToken=b77a5c561934e089")]
    public partial class InvalidTimeZoneException : System.Exception
    {
        public InvalidTimeZoneException() { }
        public InvalidTimeZoneException(string message) { }
        public InvalidTimeZoneException(string message, System.Exception innerException) { }
        protected InvalidTimeZoneException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
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
    public partial interface IServiceProvider
    {
        object GetService(System.Type serviceType);
    }
    [System.Diagnostics.DebuggerDisplayAttribute("ThreadSafetyMode={Mode}, IsValueCreated={IsValueCreated}, IsValueFaulted={IsValueFaulted}, Value={ValueForDebugDisplay}")]
    [System.Runtime.InteropServices.ComVisibleAttribute(false)]
    public partial class Lazy<T>
    {
        public Lazy() { }
        public Lazy(T value) { }
        public Lazy(bool isThreadSafe) { }
        public Lazy(System.Func<T> valueFactory) { }
        public Lazy(System.Func<T> valueFactory, bool isThreadSafe) { }
        public Lazy(System.Func<T> valueFactory, System.Threading.LazyThreadSafetyMode mode) { }
        public Lazy(System.Threading.LazyThreadSafetyMode mode) { }
        public bool IsValueCreated { get { throw null; } }
        [System.Diagnostics.DebuggerBrowsableAttribute((System.Diagnostics.DebuggerBrowsableState)(0))]
        public T Value { get { throw null; } }
        public override string ToString() { throw null; }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public enum LoaderOptimization
    {
        [System.ObsoleteAttribute("This method has been deprecated. Please use Assembly.Load() instead. http://go.microsoft.com/fwlink/?linkid=14202")]
        DisallowBindings = 4,
        [System.ObsoleteAttribute("This method has been deprecated. Please use Assembly.Load() instead. http://go.microsoft.com/fwlink/?linkid=14202")]
        DomainMask = 3,
        MultiDomain = 2,
        MultiDomainHost = 3,
        NotSpecified = 0,
        SingleDomain = 1,
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(64))]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class LoaderOptimizationAttribute : System.Attribute
    {
        public LoaderOptimizationAttribute(byte value) { }
        public LoaderOptimizationAttribute(System.LoaderOptimization value) { }
        public System.LoaderOptimization Value { get { throw null; } }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class LocalDataStoreSlot
    {
        internal LocalDataStoreSlot() { }
        ~LocalDataStoreSlot() { }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public abstract partial class MarshalByRefObject
    {
        protected MarshalByRefObject() { }
    }
    public static partial class Math
    {
        public const double E = 2.7182818284590451;
        public const double PI = 3.1415926535897931;
        public static decimal Abs(decimal value) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)][System.Security.SecuritySafeCriticalAttribute]
        public static double Abs(double value) { throw null; }
        public static short Abs(short value) { throw null; }
        public static int Abs(int value) { throw null; }
        public static long Abs(long value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static sbyte Abs(sbyte value) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)][System.Security.SecuritySafeCriticalAttribute]
        public static float Abs(float value) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)][System.Security.SecuritySafeCriticalAttribute]
        public static double Acos(double d) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)][System.Security.SecuritySafeCriticalAttribute]
        public static double Asin(double d) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)][System.Security.SecuritySafeCriticalAttribute]
        public static double Atan(double d) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)][System.Security.SecuritySafeCriticalAttribute]
        public static double Atan2(double y, double x) { throw null; }
        public static long BigMul(int a, int b) { throw null; }
        public static decimal Ceiling(decimal d) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)][System.Security.SecuritySafeCriticalAttribute]
        public static double Ceiling(double a) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)][System.Security.SecuritySafeCriticalAttribute]
        public static double Cos(double d) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)][System.Security.SecuritySafeCriticalAttribute]
        public static double Cosh(double value) { throw null; }
        public static int DivRem(int a, int b, out int result) { result = default(int); throw null; }
        public static long DivRem(long a, long b, out long result) { result = default(long); throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)][System.Security.SecuritySafeCriticalAttribute]
        public static double Exp(double d) { throw null; }
        public static decimal Floor(decimal d) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)][System.Security.SecuritySafeCriticalAttribute]
        public static double Floor(double d) { throw null; }
        public static double IEEERemainder(double x, double y) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)][System.Security.SecuritySafeCriticalAttribute]
        public static double Log(double d) { throw null; }
        public static double Log(double a, double newBase) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)][System.Security.SecuritySafeCriticalAttribute]
        public static double Log10(double d) { throw null; }
        public static byte Max(byte val1, byte val2) { throw null; }
        public static decimal Max(decimal val1, decimal val2) { throw null; }
        public static double Max(double val1, double val2) { throw null; }
        public static short Max(short val1, short val2) { throw null; }
        public static int Max(int val1, int val2) { throw null; }
        public static long Max(long val1, long val2) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static sbyte Max(sbyte val1, sbyte val2) { throw null; }
        public static float Max(float val1, float val2) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ushort Max(ushort val1, ushort val2) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static uint Max(uint val1, uint val2) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ulong Max(ulong val1, ulong val2) { throw null; }
        public static byte Min(byte val1, byte val2) { throw null; }
        public static decimal Min(decimal val1, decimal val2) { throw null; }
        public static double Min(double val1, double val2) { throw null; }
        public static short Min(short val1, short val2) { throw null; }
        public static int Min(int val1, int val2) { throw null; }
        public static long Min(long val1, long val2) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static sbyte Min(sbyte val1, sbyte val2) { throw null; }
        public static float Min(float val1, float val2) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ushort Min(ushort val1, ushort val2) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static uint Min(uint val1, uint val2) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ulong Min(ulong val1, ulong val2) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)][System.Security.SecuritySafeCriticalAttribute]
        public static double Pow(double x, double y) { throw null; }
        public static decimal Round(decimal d) { throw null; }
        public static decimal Round(decimal d, int decimals) { throw null; }
        public static decimal Round(decimal d, int decimals, System.MidpointRounding mode) { throw null; }
        public static decimal Round(decimal d, System.MidpointRounding mode) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)][System.Security.SecuritySafeCriticalAttribute]
        public static double Round(double a) { throw null; }
        public static double Round(double value, int digits) { throw null; }
        public static double Round(double value, int digits, System.MidpointRounding mode) { throw null; }
        public static double Round(double value, System.MidpointRounding mode) { throw null; }
        public static int Sign(decimal value) { throw null; }
        public static int Sign(double value) { throw null; }
        public static int Sign(short value) { throw null; }
        public static int Sign(int value) { throw null; }
        public static int Sign(long value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static int Sign(sbyte value) { throw null; }
        public static int Sign(float value) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)][System.Security.SecuritySafeCriticalAttribute]
        public static double Sin(double a) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)][System.Security.SecuritySafeCriticalAttribute]
        public static double Sinh(double value) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)][System.Security.SecuritySafeCriticalAttribute]
        public static double Sqrt(double d) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)][System.Security.SecuritySafeCriticalAttribute]
        public static double Tan(double a) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)][System.Security.SecuritySafeCriticalAttribute]
        public static double Tanh(double value) { throw null; }
        public static decimal Truncate(decimal d) { throw null; }
        public static double Truncate(double d) { throw null; }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class MemberAccessException : System.SystemException
    {
        public MemberAccessException() { }
        public MemberAccessException(string message) { }
        public MemberAccessException(string message, System.Exception inner) { }
        protected MemberAccessException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class MethodAccessException : System.MemberAccessException
    {
        public MethodAccessException() { }
        public MethodAccessException(string message) { }
        public MethodAccessException(string message, System.Exception inner) { }
        protected MethodAccessException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public enum MidpointRounding
    {
        AwayFromZero = 1,
        ToEven = 0,
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class MissingFieldException : System.MissingMemberException, System.Runtime.Serialization.ISerializable
    {
        public MissingFieldException() { }
        protected MissingFieldException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public MissingFieldException(string message) { }
        public MissingFieldException(string message, System.Exception inner) { }
        public MissingFieldException(string className, string fieldName) { }
        public override string Message { [System.Security.SecuritySafeCriticalAttribute]get { throw null; } }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
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
        public override string Message { [System.Security.SecuritySafeCriticalAttribute]get { throw null; } }
        [System.Security.SecurityCriticalAttribute]
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class MissingMethodException : System.MissingMemberException, System.Runtime.Serialization.ISerializable
    {
        public MissingMethodException() { }
        protected MissingMethodException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public MissingMethodException(string message) { }
        public MissingMethodException(string message, System.Exception inner) { }
        public MissingMethodException(string className, string methodName) { }
        public override string Message { [System.Security.SecuritySafeCriticalAttribute]get { throw null; } }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public unsafe struct ModuleHandle
    {
        public static readonly System.ModuleHandle EmptyHandle;
        public int MDStreamVersion { get { throw null; } }
        public override int GetHashCode() { throw null; }
        public override bool Equals(object obj) { throw null; }
        public unsafe bool Equals(System.ModuleHandle handle) { throw null; }
        public static bool operator ==(System.ModuleHandle left, System.ModuleHandle right) { throw null; }
        public static bool operator !=(System.ModuleHandle left, System.ModuleHandle right) { throw null; }
        public System.RuntimeTypeHandle GetRuntimeTypeHandleFromMetadataToken(int typeToken) { throw null; }
        public System.RuntimeTypeHandle ResolveTypeHandle(int typeToken) { throw null; }
        public System.RuntimeTypeHandle ResolveTypeHandle(int typeToken, System.RuntimeTypeHandle[] typeInstantiationContext, System.RuntimeTypeHandle[] methodInstantiationContext) { throw null; }
        public System.RuntimeMethodHandle GetRuntimeMethodHandleFromMetadataToken(int methodToken) { throw null; }
        public System.RuntimeMethodHandle ResolveMethodHandle(int methodToken) { throw null; }
        public System.RuntimeMethodHandle ResolveMethodHandle(int methodToken, System.RuntimeTypeHandle[] typeInstantiationContext, System.RuntimeTypeHandle[] methodInstantiationContext) { throw null; }
        public System.RuntimeFieldHandle GetRuntimeFieldHandleFromMetadataToken(int fieldToken) { throw null; }
        public System.RuntimeFieldHandle ResolveFieldHandle(int fieldToken) { throw null; }
        public System.RuntimeFieldHandle ResolveFieldHandle(int fieldToken, System.RuntimeTypeHandle[] typeInstantiationContext, System.RuntimeTypeHandle[] methodInstantiationContext) { throw null; }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(64))]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class MTAThreadAttribute : System.Attribute
    {
        public MTAThreadAttribute() { }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public abstract partial class MulticastDelegate : System.Delegate
    {
        protected MulticastDelegate(object target, string method) : base (default(object), default(string)) { }
        protected MulticastDelegate(System.Type target, string method) : base (default(object), default(string)) { }
        [System.Security.SecuritySafeCriticalAttribute]
        protected sealed override System.Delegate CombineImpl(System.Delegate follow) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public sealed override bool Equals(object obj) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public sealed override int GetHashCode() { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public sealed override System.Delegate[] GetInvocationList() { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        protected override System.Reflection.MethodInfo GetMethodImpl() { throw null; }
        [System.Security.SecurityCriticalAttribute]
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public static bool operator ==(System.MulticastDelegate d1, System.MulticastDelegate d2) { throw null; }
        public static bool operator !=(System.MulticastDelegate d1, System.MulticastDelegate d2) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        protected sealed override System.Delegate RemoveImpl(System.Delegate value) { throw null; }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class MulticastNotSupportedException : System.SystemException
    {
        public MulticastNotSupportedException() { }
        public MulticastNotSupportedException(string message) { }
        public MulticastNotSupportedException(string message, System.Exception inner) { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(256), Inherited=false)]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class NonSerializedAttribute : System.Attribute
    {
        public NonSerializedAttribute() { }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class NotFiniteNumberException : System.ArithmeticException
    {
        public NotFiniteNumberException() { }
        public NotFiniteNumberException(double offendingNumber) { }
        public NotFiniteNumberException(string message) { }
        public NotFiniteNumberException(string message, double offendingNumber) { }
        public NotFiniteNumberException(string message, double offendingNumber, System.Exception innerException) { }
        public NotFiniteNumberException(string message, System.Exception innerException) { }
        protected NotFiniteNumberException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
        public double OffendingNumber { get { throw null; } }
        [System.Security.SecurityCriticalAttribute]
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class NotImplementedException : System.SystemException
    {
        public NotImplementedException() { }
        public NotImplementedException(string message) { }
        public NotImplementedException(string message, System.Exception inner) { }
        protected NotImplementedException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class NotSupportedException : System.SystemException
    {
        public NotSupportedException() { }
        public NotSupportedException(string message) { }
        public NotSupportedException(string message, System.Exception innerException) { }
        protected NotSupportedException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public static partial class Nullable
    {
        [System.Runtime.InteropServices.ComVisibleAttribute(true)]
        public static int Compare<T>(System.Nullable<T> n1, System.Nullable<T> n2) where T : struct { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(true)]
        public static bool Equals<T>(System.Nullable<T> n1, System.Nullable<T> n2) where T : struct { throw null; }
        public static System.Type GetUnderlyingType(System.Type nullableType) { throw null; }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct Nullable<T> where T : struct
    {
        public Nullable(T value) { throw null;}
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
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class NullReferenceException : System.SystemException
    {
        public NullReferenceException() { }
        public NullReferenceException(string message) { }
        public NullReferenceException(string message, System.Exception innerException) { }
        protected NullReferenceException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
    [System.Runtime.InteropServices.ClassInterfaceAttribute((System.Runtime.InteropServices.ClassInterfaceType)(2))]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class Object
    {
        public Object() { }
        public virtual bool Equals(System.Object obj) { throw null; }
        public static bool Equals(System.Object objA, System.Object objB) { throw null; }
        ~Object() { }
        public virtual int GetHashCode() { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)][System.Security.SecuritySafeCriticalAttribute]
        public System.Type GetType() { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)][System.Security.SecuritySafeCriticalAttribute]
        protected System.Object MemberwiseClone() { throw null; }
        public static bool ReferenceEquals(System.Object objA, System.Object objB) { throw null; }
        public virtual string ToString() { throw null; }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class ObjectDisposedException : System.InvalidOperationException
    {
        public ObjectDisposedException(string objectName) { }
        public ObjectDisposedException(string message, System.Exception innerException) { }
        public ObjectDisposedException(string objectName, string message) { }
        protected ObjectDisposedException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
        public override string Message { get { throw null; } }
        public string ObjectName { get { throw null; } }
        [System.Security.SecurityCriticalAttribute]
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(6140), Inherited=false)]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class ObsoleteAttribute : System.Attribute
    {
        public ObsoleteAttribute() { }
        public ObsoleteAttribute(string message) { }
        public ObsoleteAttribute(string message, bool error) { }
        public bool IsError { get { throw null; } }
        public string Message { get { throw null; } }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class OperatingSystem : System.ICloneable, System.Runtime.Serialization.ISerializable
    {
        public OperatingSystem(System.PlatformID platform, System.Version version) { }
        public System.PlatformID Platform { get { throw null; } }
        public string ServicePack { get { throw null; } }
        public System.Version Version { get { throw null; } }
        public string VersionString { get { throw null; } }
        public object Clone() { throw null; }
        [System.Security.SecurityCriticalAttribute]
        public void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public override string ToString() { throw null; }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class OperationCanceledException : System.SystemException
    {
        public OperationCanceledException() { }
        public OperationCanceledException(string message) { }
        public OperationCanceledException(string message, System.Exception innerException) { }
        public OperationCanceledException(string message, System.Exception innerException, System.Threading.CancellationToken token) { }
        public OperationCanceledException(string message, System.Threading.CancellationToken token) { }
        public OperationCanceledException(System.Threading.CancellationToken token) { }
        protected OperationCanceledException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
        public System.Threading.CancellationToken CancellationToken { get { throw null; } }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class OutOfMemoryException : System.SystemException
    {
        public OutOfMemoryException() { }
        public OutOfMemoryException(string message) { }
        public OutOfMemoryException(string message, System.Exception innerException) { }
        protected OutOfMemoryException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class OverflowException : System.ArithmeticException
    {
        public OverflowException() { }
        public OverflowException(string message) { }
        public OverflowException(string message, System.Exception innerException) { }
        protected OverflowException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(2048), Inherited=true, AllowMultiple=false)]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class ParamArrayAttribute : System.Attribute
    {
        public ParamArrayAttribute() { }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public enum PlatformID
    {
        MacOSX = 6,
        Unix = 4,
        Win32NT = 2,
        Win32S = 0,
        Win32Windows = 1,
        WinCE = 3,
        Xbox = 5,
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class PlatformNotSupportedException : System.NotSupportedException
    {
        public PlatformNotSupportedException() { }
        public PlatformNotSupportedException(string message) { }
        public PlatformNotSupportedException(string message, System.Exception inner) { }
        protected PlatformNotSupportedException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
    public delegate bool Predicate<in T>(T obj);
    public partial class Progress<T> : System.IProgress<T>
    {
        public Progress() { }
        public Progress(System.Action<T> handler) { }
        public event System.EventHandler<T> ProgressChanged { add { } remove { } }
        protected virtual void OnReport(T value) { }
        void System.IProgress<T>.Report(T value) { }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class Random
    {
        public Random() { }
        public Random(int Seed) { }
        public virtual int Next() { throw null; }
        public virtual int Next(int maxValue) { throw null; }
        public virtual int Next(int minValue, int maxValue) { throw null; }
        public virtual void NextBytes(byte[] buffer) { }
        public virtual double NextDouble() { throw null; }
        protected virtual double Sample() { throw null; }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class RankException : System.SystemException
    {
        public RankException() { }
        public RankException(string message) { }
        public RankException(string message, System.Exception innerException) { }
        protected RankException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class ResolveEventArgs : System.EventArgs
    {
        public ResolveEventArgs(string name) { }
        public ResolveEventArgs(string name, System.Reflection.Assembly requestingAssembly) { }
        public string Name { get { throw null; } }
        public System.Reflection.Assembly RequestingAssembly { get { throw null; } }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    [System.Security.SecurityCriticalAttribute]
    public delegate System.Reflection.Assembly ResolveEventHandler(object sender, System.ResolveEventArgs args);
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct RuntimeArgumentHandle
    {
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct RuntimeFieldHandle : System.Runtime.Serialization.ISerializable
    {
        public System.IntPtr Value { [System.Security.SecurityCriticalAttribute]get { throw null; } }
        [System.Security.SecuritySafeCriticalAttribute]
        public override bool Equals(object obj) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public bool Equals(System.RuntimeFieldHandle handle) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public override int GetHashCode() { throw null; }
        [System.Security.SecurityCriticalAttribute]
        public void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public static bool operator ==(System.RuntimeFieldHandle left, System.RuntimeFieldHandle right) { throw null; }
        public static bool operator !=(System.RuntimeFieldHandle left, System.RuntimeFieldHandle right) { throw null; }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct RuntimeMethodHandle : System.Runtime.Serialization.ISerializable
    {
        public System.IntPtr Value { [System.Security.SecurityCriticalAttribute]get { throw null; } }
        [System.Security.SecuritySafeCriticalAttribute]
        public override bool Equals(object obj) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public bool Equals(System.RuntimeMethodHandle handle) { throw null; }
        [System.Security.SecurityCriticalAttribute]
        public System.IntPtr GetFunctionPointer() { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public override int GetHashCode() { throw null; }
        [System.Security.SecurityCriticalAttribute]
        public void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public static bool operator ==(System.RuntimeMethodHandle left, System.RuntimeMethodHandle right) { throw null; }
        public static bool operator !=(System.RuntimeMethodHandle left, System.RuntimeMethodHandle right) { throw null; }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct RuntimeTypeHandle : System.Runtime.Serialization.ISerializable
    {
        public System.IntPtr Value { [System.Security.SecurityCriticalAttribute]get { throw null; } }
        public override bool Equals(object obj) { throw null; }
        public bool Equals(System.RuntimeTypeHandle handle) { throw null; }
        public override int GetHashCode() { throw null; }
        [System.CLSCompliantAttribute(false)]
        public System.ModuleHandle GetModuleHandle() { throw null; }
        [System.Security.SecurityCriticalAttribute]
        public void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public static bool operator ==(object left, System.RuntimeTypeHandle right) { throw null; }
        public static bool operator ==(System.RuntimeTypeHandle left, object right) { throw null; }
        public static bool operator !=(object left, System.RuntimeTypeHandle right) { throw null; }
        public static bool operator !=(System.RuntimeTypeHandle left, object right) { throw null; }
    }
    [System.CLSCompliantAttribute(false)]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct SByte : System.IComparable, System.IComparable<sbyte>, System.IConvertible, System.IEquatable<sbyte>, System.IFormattable
    {
        public const sbyte MaxValue = (sbyte)127;
        public const sbyte MinValue = (sbyte)-128;
        public int CompareTo(object obj) { throw null; }
        public int CompareTo(System.SByte value) { throw null; }
        public override bool Equals(object obj) { throw null; }
        public bool Equals(System.SByte obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public System.TypeCode GetTypeCode() { throw null; }
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
        [System.Security.SecuritySafeCriticalAttribute]
        public override string ToString() { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public string ToString(System.IFormatProvider provider) { throw null; }
        public string ToString(string format) { throw null; }
        public string ToString(string format, System.IFormatProvider provider) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static bool TryParse(string s, System.Globalization.NumberStyles style, System.IFormatProvider provider, out System.SByte result) { result = default(sbyte); throw null; }
        [System.CLSCompliantAttribute(false)]
        public static bool TryParse(string s, out System.SByte result) { result = default(sbyte); throw null; }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(4124), Inherited=false)]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class SerializableAttribute : System.Attribute
    {
        public SerializableAttribute() { }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct Single : System.IComparable, System.IComparable<float>, System.IConvertible, System.IEquatable<float>, System.IFormattable
    {
        public const float Epsilon = 1.401298E-45f;
        public const float MaxValue = 3.40282347E+38f;
        public const float MinValue = -3.40282347E+38f;
        public const float NaN = 0.0f / 0.0f;
        public const float NegativeInfinity = -1.0f / 0.0f;
        public const float PositiveInfinity = 1.0f / 0.0f;
        public int CompareTo(object value) { throw null; }
        public int CompareTo(System.Single value) { throw null; }
        public override bool Equals(object obj) { throw null; }
        public bool Equals(System.Single obj) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public override int GetHashCode() { throw null; }
        public System.TypeCode GetTypeCode() { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public static bool IsInfinity(System.Single f) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public static bool IsNaN(System.Single f) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public static bool IsNegativeInfinity(System.Single f) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public static bool IsPositiveInfinity(System.Single f) { throw null; }
        public static bool operator ==(System.Single left, System.Single right) { throw null; }
        public static bool operator >(System.Single left, System.Single right) { throw null; }
        public static bool operator >=(System.Single left, System.Single right) { throw null; }
        public static bool operator !=(System.Single left, System.Single right) { throw null; }
        public static bool operator <(System.Single left, System.Single right) { throw null; }
        public static bool operator <=(System.Single left, System.Single right) { throw null; }
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
        [System.Security.SecuritySafeCriticalAttribute]
        public override string ToString() { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public string ToString(System.IFormatProvider provider) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public string ToString(string format) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public string ToString(string format, System.IFormatProvider provider) { throw null; }
        public static bool TryParse(string s, System.Globalization.NumberStyles style, System.IFormatProvider provider, out System.Single result) { result = default(float); throw null; }
        public static bool TryParse(string s, out System.Single result) { result = default(float); throw null; }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class StackOverflowException : System.SystemException
    {
        public StackOverflowException() { }
        public StackOverflowException(string message) { }
        public StackOverflowException(string message, System.Exception innerException) { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(64))]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class STAThreadAttribute : System.Attribute
    {
        public STAThreadAttribute() { }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class String : System.Collections.Generic.IEnumerable<char>, System.Collections.IEnumerable, System.ICloneable, System.IComparable, System.IComparable<string>, System.IConvertible, System.IEquatable<string>
    {
        public static readonly string Empty;
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)]
        [System.CLSCompliantAttribute(false)]
        [System.Security.SecurityCriticalAttribute]
        public unsafe String(char* value) { }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)]
        [System.CLSCompliantAttribute(false)]
        [System.Security.SecurityCriticalAttribute]
        public unsafe String(char* value, int startIndex, int length) { }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)]
        [System.Security.SecuritySafeCriticalAttribute]
        public String(char c, int count) { }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)]
        [System.Security.SecuritySafeCriticalAttribute]
        public String(char[] value) { }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)]
        [System.Security.SecuritySafeCriticalAttribute]
        public String(char[] value, int startIndex, int length) { }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)]
        [System.CLSCompliantAttribute(false)]
        [System.Security.SecurityCriticalAttribute]
        public unsafe String(sbyte* value) { }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)]
        [System.CLSCompliantAttribute(false)]
        [System.Security.SecurityCriticalAttribute]
        public unsafe String(sbyte* value, int startIndex, int length) { }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)]
        [System.CLSCompliantAttribute(false)]
        [System.Security.SecurityCriticalAttribute]
        public unsafe String(sbyte* value, int startIndex, int length, System.Text.Encoding enc) { }
        [System.Runtime.CompilerServices.IndexerName("Chars")]
        public char this[int index] { [System.Security.SecuritySafeCriticalAttribute][System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)]get { throw null; } }
        public int Length { [System.Security.SecuritySafeCriticalAttribute][System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)]get { throw null; } }
        public object Clone() { throw null; }
        public static int Compare(System.String strA, int indexA, System.String strB, int indexB, int length) { throw null; }
        public static int Compare(System.String strA, int indexA, System.String strB, int indexB, int length, bool ignoreCase) { throw null; }
        public static int Compare(System.String strA, int indexA, System.String strB, int indexB, int length, bool ignoreCase, System.Globalization.CultureInfo culture) { throw null; }
        public static int Compare(System.String strA, int indexA, System.String strB, int indexB, int length, System.Globalization.CultureInfo culture, System.Globalization.CompareOptions options) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public static int Compare(System.String strA, int indexA, System.String strB, int indexB, int length, System.StringComparison comparisonType) { throw null; }
        public static int Compare(System.String strA, System.String strB) { throw null; }
        public static int Compare(System.String strA, System.String strB, bool ignoreCase) { throw null; }
        public static int Compare(System.String strA, System.String strB, bool ignoreCase, System.Globalization.CultureInfo culture) { throw null; }
        public static int Compare(System.String strA, System.String strB, System.Globalization.CultureInfo culture, System.Globalization.CompareOptions options) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public static int Compare(System.String strA, System.String strB, System.StringComparison comparisonType) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public static int CompareOrdinal(System.String strA, int indexA, System.String strB, int indexB, int length) { throw null; }
        public static int CompareOrdinal(System.String strA, System.String strB) { throw null; }
        public int CompareTo(object value) { throw null; }
        public int CompareTo(System.String strB) { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public static System.String Concat(System.Collections.Generic.IEnumerable<string> values) { throw null; }
        public static System.String Concat(object arg0) { throw null; }
        public static System.String Concat(object arg0, object arg1) { throw null; }
        public static System.String Concat(object arg0, object arg1, object arg2) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public static System.String Concat(params object[] args) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public static System.String Concat(System.String str0, System.String str1) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public static System.String Concat(System.String str0, System.String str1, System.String str2) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public static System.String Concat(System.String str0, System.String str1, System.String str2, System.String str3) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public static System.String Concat(params string[] values) { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public static System.String Concat<T>(System.Collections.Generic.IEnumerable<T> values) { throw null; }
        public bool Contains(System.String value) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public static System.String Copy(System.String str) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count) { }
        public bool EndsWith(System.String value) { throw null; }
        public bool EndsWith(System.String value, bool ignoreCase, System.Globalization.CultureInfo culture) { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        [System.Security.SecuritySafeCriticalAttribute]
        public bool EndsWith(System.String value, System.StringComparison comparisonType) { throw null; }
        public override bool Equals(object obj) { throw null; }
        public bool Equals(System.String value) { throw null; }
        public static bool Equals(System.String a, System.String b) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public static bool Equals(System.String a, System.String b, System.StringComparison comparisonType) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public bool Equals(System.String value, System.StringComparison comparisonType) { throw null; }
        public static System.String Format(System.IFormatProvider provider, System.String format, object arg0) { throw null; }
        public static System.String Format(System.IFormatProvider provider, System.String format, object arg0, object arg1) { throw null; }
        public static System.String Format(System.IFormatProvider provider, System.String format, object arg0, object arg1, object arg2) { throw null; }
        public static System.String Format(System.IFormatProvider provider, System.String format, params object[] args) { throw null; }
        public static System.String Format(System.String format, object arg0) { throw null; }
        public static System.String Format(System.String format, object arg0, object arg1) { throw null; }
        public static System.String Format(System.String format, object arg0, object arg1, object arg2) { throw null; }
        public static System.String Format(System.String format, params object[] args) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public override int GetHashCode() { throw null; }
        public System.TypeCode GetTypeCode() { throw null; }
        public int IndexOf(char value) { throw null; }
        public int IndexOf(char value, int startIndex) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public int IndexOf(char value, int startIndex, int count) { throw null; }
        public int IndexOf(System.String value) { throw null; }
        public int IndexOf(System.String value, int startIndex) { throw null; }
        public int IndexOf(System.String value, int startIndex, int count) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public int IndexOf(System.String value, int startIndex, int count, System.StringComparison comparisonType) { throw null; }
        public int IndexOf(System.String value, int startIndex, System.StringComparison comparisonType) { throw null; }
        public int IndexOf(System.String value, System.StringComparison comparisonType) { throw null; }
        public int IndexOfAny(char[] anyOf) { throw null; }
        public int IndexOfAny(char[] anyOf, int startIndex) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)]
        [System.Security.SecuritySafeCriticalAttribute]
        public int IndexOfAny(char[] anyOf, int startIndex, int count) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public System.String Insert(int startIndex, System.String value) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public static System.String Intern(System.String str) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public static System.String IsInterned(System.String str) { throw null; }
        public static bool IsNullOrEmpty(System.String value) { throw null; }
        public static bool IsNullOrWhiteSpace(System.String value) { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public static System.String Join(System.String separator, System.Collections.Generic.IEnumerable<string> values) { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public static System.String Join(System.String separator, params object[] values) { throw null; }
        public static System.String Join(System.String separator, params string[] value) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public static System.String Join(System.String separator, string[] value, int startIndex, int count) { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public static System.String Join<T>(System.String separator, System.Collections.Generic.IEnumerable<T> values) { throw null; }
        public int LastIndexOf(char value) { throw null; }
        public int LastIndexOf(char value, int startIndex) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public int LastIndexOf(char value, int startIndex, int count) { throw null; }
        public int LastIndexOf(System.String value) { throw null; }
        public int LastIndexOf(System.String value, int startIndex) { throw null; }
        public int LastIndexOf(System.String value, int startIndex, int count) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public int LastIndexOf(System.String value, int startIndex, int count, System.StringComparison comparisonType) { throw null; }
        public int LastIndexOf(System.String value, int startIndex, System.StringComparison comparisonType) { throw null; }
        public int LastIndexOf(System.String value, System.StringComparison comparisonType) { throw null; }
        public int LastIndexOfAny(char[] anyOf) { throw null; }
        public int LastIndexOfAny(char[] anyOf, int startIndex) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)]
        [System.Security.SecuritySafeCriticalAttribute]
        public int LastIndexOfAny(char[] anyOf, int startIndex, int count) { throw null; }
        public static bool operator ==(System.String a, System.String b) { throw null; }
        public static bool operator !=(System.String a, System.String b) { throw null; }
        public System.String PadLeft(int totalWidth) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public System.String PadLeft(int totalWidth, char paddingChar) { throw null; }
        public System.String PadRight(int totalWidth) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public System.String PadRight(int totalWidth, char paddingChar) { throw null; }
        public System.String Remove(int startIndex) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public System.String Remove(int startIndex, int count) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public System.String Replace(char oldChar, char newChar) { throw null; }
        public System.String Replace(System.String oldValue, System.String newValue) { throw null; }
        public string[] Split(params char[] separator) { throw null; }
        public string[] Split(char[] separator, int count) { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public string[] Split(char[] separator, int count, System.StringSplitOptions options) { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public string[] Split(char[] separator, System.StringSplitOptions options) { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public string[] Split(string[] separator, int count, System.StringSplitOptions options) { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public string[] Split(string[] separator, System.StringSplitOptions options) { throw null; }
        public bool StartsWith(System.String value) { throw null; }
        public bool StartsWith(System.String value, bool ignoreCase, System.Globalization.CultureInfo culture) { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        [System.Security.SecuritySafeCriticalAttribute]
        public bool StartsWith(System.String value, System.StringComparison comparisonType) { throw null; }
        public System.String Substring(int startIndex) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
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
        [System.Security.SecuritySafeCriticalAttribute]
        public char[] ToCharArray() { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
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
        public System.String Trim(params char[] trimChars) { throw null; }
        public System.String TrimEnd(params char[] trimChars) { throw null; }
        public System.String TrimStart(params char[] trimChars) { throw null; }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public abstract partial class StringComparer : System.Collections.Generic.IComparer<string>, System.Collections.Generic.IEqualityComparer<string>, System.Collections.IComparer, System.Collections.IEqualityComparer
    {
        protected StringComparer() { }
        public static System.StringComparer CurrentCulture { get { throw null; } }
        public static System.StringComparer CurrentCultureIgnoreCase { get { throw null; } }
        public static System.StringComparer InvariantCulture { get { throw null; } }
        public static System.StringComparer InvariantCultureIgnoreCase { get { throw null; } }
        public static System.StringComparer Ordinal { get { throw null; } }
        public static System.StringComparer OrdinalIgnoreCase { get { throw null; } }
        public int Compare(object x, object y) { throw null; }
        public abstract int Compare(string x, string y);
        public static System.StringComparer Create(System.Globalization.CultureInfo culture, bool ignoreCase) { throw null; }
        public new bool Equals(object x, object y) { throw null; }
        public abstract bool Equals(string x, string y);
        public int GetHashCode(object obj) { throw null; }
        public abstract int GetHashCode(string obj);
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
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
    [System.Runtime.InteropServices.ComVisibleAttribute(false)]
    public enum StringSplitOptions
    {
        None = 0,
        RemoveEmptyEntries = 1,
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class SystemException : System.Exception
    {
        public SystemException() { }
        public SystemException(string message) { }
        public SystemException(string message, System.Exception innerException) { }
        protected SystemException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(256), Inherited=false)]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class ThreadStaticAttribute : System.Attribute
    {
        public ThreadStaticAttribute() { }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class TimeoutException : System.SystemException
    {
        public TimeoutException() { }
        public TimeoutException(string message) { }
        public TimeoutException(string message, System.Exception innerException) { }
        protected TimeoutException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
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
        public TimeSpan(int hours, int minutes, int seconds) { throw null;}
        public TimeSpan(int days, int hours, int minutes, int seconds) { throw null;}
        public TimeSpan(int days, int hours, int minutes, int seconds, int milliseconds) { throw null;}
        public TimeSpan(long ticks) { throw null;}
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
        public System.TimeSpan Negate() { throw null; }
        public static System.TimeSpan operator +(System.TimeSpan t1, System.TimeSpan t2) { throw null; }
        public static bool operator ==(System.TimeSpan t1, System.TimeSpan t2) { throw null; }
        public static bool operator >(System.TimeSpan t1, System.TimeSpan t2) { throw null; }
        public static bool operator >=(System.TimeSpan t1, System.TimeSpan t2) { throw null; }
        public static bool operator !=(System.TimeSpan t1, System.TimeSpan t2) { throw null; }
        public static bool operator <(System.TimeSpan t1, System.TimeSpan t2) { throw null; }
        public static bool operator <=(System.TimeSpan t1, System.TimeSpan t2) { throw null; }
        public static System.TimeSpan operator -(System.TimeSpan t1, System.TimeSpan t2) { throw null; }
        public static System.TimeSpan operator -(System.TimeSpan t) { throw null; }
        public static System.TimeSpan operator +(System.TimeSpan t) { throw null; }
        public static System.TimeSpan Parse(string s) { throw null; }
        public static System.TimeSpan Parse(string input, System.IFormatProvider formatProvider) { throw null; }
        public static System.TimeSpan ParseExact(string input, string format, System.IFormatProvider formatProvider) { throw null; }
        public static System.TimeSpan ParseExact(string input, string format, System.IFormatProvider formatProvider, System.Globalization.TimeSpanStyles styles) { throw null; }
        public static System.TimeSpan ParseExact(string input, string[] formats, System.IFormatProvider formatProvider) { throw null; }
        public static System.TimeSpan ParseExact(string input, string[] formats, System.IFormatProvider formatProvider, System.Globalization.TimeSpanStyles styles) { throw null; }
        public System.TimeSpan Subtract(System.TimeSpan ts) { throw null; }
        public override string ToString() { throw null; }
        public string ToString(string format) { throw null; }
        public string ToString(string format, System.IFormatProvider formatProvider) { throw null; }
        public static bool TryParse(string input, System.IFormatProvider formatProvider, out System.TimeSpan result) { result = default(System.TimeSpan); throw null; }
        public static bool TryParse(string s, out System.TimeSpan result) { result = default(System.TimeSpan); throw null; }
        public static bool TryParseExact(string input, string format, System.IFormatProvider formatProvider, System.Globalization.TimeSpanStyles styles, out System.TimeSpan result) { result = default(System.TimeSpan); throw null; }
        public static bool TryParseExact(string input, string format, System.IFormatProvider formatProvider, out System.TimeSpan result) { result = default(System.TimeSpan); throw null; }
        public static bool TryParseExact(string input, string[] formats, System.IFormatProvider formatProvider, System.Globalization.TimeSpanStyles styles, out System.TimeSpan result) { result = default(System.TimeSpan); throw null; }
        public static bool TryParseExact(string input, string[] formats, System.IFormatProvider formatProvider, out System.TimeSpan result) { result = default(System.TimeSpan); throw null; }
    }
    [System.ObsoleteAttribute("System.TimeZone has been deprecated.  Please investigate the use of System.TimeZoneInfo instead.")]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public abstract partial class TimeZone
    {
        protected TimeZone() { }
        public abstract string DaylightName { get; }
        public abstract string StandardName { get; }
        public abstract System.Globalization.DaylightTime GetDaylightChanges(int year);
        public abstract System.TimeSpan GetUtcOffset(System.DateTime time);
        public virtual bool IsDaylightSavingTime(System.DateTime time) { throw null; }
        public static bool IsDaylightSavingTime(System.DateTime time, System.Globalization.DaylightTime daylightTimes) { throw null; }
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
        [System.Security.SecuritySafeCriticalAttribute]
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
        [System.Security.SecurityCriticalAttribute]
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
            [System.Security.SecurityCriticalAttribute]
            void System.Runtime.Serialization.ISerializable.GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        }
        [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
        public partial struct TransitionTime : System.IEquatable<System.TimeZoneInfo.TransitionTime>, System.Runtime.Serialization.IDeserializationCallback, System.Runtime.Serialization.ISerializable
        {
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
            [System.Security.SecurityCriticalAttribute]
            void System.Runtime.Serialization.ISerializable.GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        }
    }
    [System.Runtime.CompilerServices.TypeForwardedFromAttribute("System.Core, Version=3.5.0.0, Culture=Neutral, PublicKeyToken=b77a5c561934e089")]
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
    public partial class Tuple<T1> : System.Collections.IStructuralComparable, System.Collections.IStructuralEquatable, System.IComparable
    {
        public Tuple(T1 item1) { }
        public T1 Item1 { get { throw null; } }
        public override bool Equals(object obj) { throw null; }
        public override int GetHashCode() { throw null; }
        int System.Collections.IStructuralComparable.CompareTo(object other, System.Collections.IComparer comparer) { throw null; }
        bool System.Collections.IStructuralEquatable.Equals(object other, System.Collections.IEqualityComparer comparer) { throw null; }
        int System.Collections.IStructuralEquatable.GetHashCode(System.Collections.IEqualityComparer comparer) { throw null; }
        int System.IComparable.CompareTo(object obj) { throw null; }
        public override string ToString() { throw null; }
    }
    public partial class Tuple<T1, T2> : System.Collections.IStructuralComparable, System.Collections.IStructuralEquatable, System.IComparable
    {
        public Tuple(T1 item1, T2 item2) { }
        public T1 Item1 { get { throw null; } }
        public T2 Item2 { get { throw null; } }
        public override bool Equals(object obj) { throw null; }
        public override int GetHashCode() { throw null; }
        int System.Collections.IStructuralComparable.CompareTo(object other, System.Collections.IComparer comparer) { throw null; }
        bool System.Collections.IStructuralEquatable.Equals(object other, System.Collections.IEqualityComparer comparer) { throw null; }
        int System.Collections.IStructuralEquatable.GetHashCode(System.Collections.IEqualityComparer comparer) { throw null; }
        int System.IComparable.CompareTo(object obj) { throw null; }
        public override string ToString() { throw null; }
    }
    public partial class Tuple<T1, T2, T3> : System.Collections.IStructuralComparable, System.Collections.IStructuralEquatable, System.IComparable
    {
        public Tuple(T1 item1, T2 item2, T3 item3) { }
        public T1 Item1 { get { throw null; } }
        public T2 Item2 { get { throw null; } }
        public T3 Item3 { get { throw null; } }
        public override bool Equals(object obj) { throw null; }
        public override int GetHashCode() { throw null; }
        int System.Collections.IStructuralComparable.CompareTo(object other, System.Collections.IComparer comparer) { throw null; }
        bool System.Collections.IStructuralEquatable.Equals(object other, System.Collections.IEqualityComparer comparer) { throw null; }
        int System.Collections.IStructuralEquatable.GetHashCode(System.Collections.IEqualityComparer comparer) { throw null; }
        int System.IComparable.CompareTo(object obj) { throw null; }
        public override string ToString() { throw null; }
    }
    public partial class Tuple<T1, T2, T3, T4> : System.Collections.IStructuralComparable, System.Collections.IStructuralEquatable, System.IComparable
    {
        public Tuple(T1 item1, T2 item2, T3 item3, T4 item4) { }
        public T1 Item1 { get { throw null; } }
        public T2 Item2 { get { throw null; } }
        public T3 Item3 { get { throw null; } }
        public T4 Item4 { get { throw null; } }
        public override bool Equals(object obj) { throw null; }
        public override int GetHashCode() { throw null; }
        int System.Collections.IStructuralComparable.CompareTo(object other, System.Collections.IComparer comparer) { throw null; }
        bool System.Collections.IStructuralEquatable.Equals(object other, System.Collections.IEqualityComparer comparer) { throw null; }
        int System.Collections.IStructuralEquatable.GetHashCode(System.Collections.IEqualityComparer comparer) { throw null; }
        int System.IComparable.CompareTo(object obj) { throw null; }
        public override string ToString() { throw null; }
    }
    public partial class Tuple<T1, T2, T3, T4, T5> : System.Collections.IStructuralComparable, System.Collections.IStructuralEquatable, System.IComparable
    {
        public Tuple(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5) { }
        public T1 Item1 { get { throw null; } }
        public T2 Item2 { get { throw null; } }
        public T3 Item3 { get { throw null; } }
        public T4 Item4 { get { throw null; } }
        public T5 Item5 { get { throw null; } }
        public override bool Equals(object obj) { throw null; }
        public override int GetHashCode() { throw null; }
        int System.Collections.IStructuralComparable.CompareTo(object other, System.Collections.IComparer comparer) { throw null; }
        bool System.Collections.IStructuralEquatable.Equals(object other, System.Collections.IEqualityComparer comparer) { throw null; }
        int System.Collections.IStructuralEquatable.GetHashCode(System.Collections.IEqualityComparer comparer) { throw null; }
        int System.IComparable.CompareTo(object obj) { throw null; }
        public override string ToString() { throw null; }
    }
    public partial class Tuple<T1, T2, T3, T4, T5, T6> : System.Collections.IStructuralComparable, System.Collections.IStructuralEquatable, System.IComparable
    {
        public Tuple(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6) { }
        public T1 Item1 { get { throw null; } }
        public T2 Item2 { get { throw null; } }
        public T3 Item3 { get { throw null; } }
        public T4 Item4 { get { throw null; } }
        public T5 Item5 { get { throw null; } }
        public T6 Item6 { get { throw null; } }
        public override bool Equals(object obj) { throw null; }
        public override int GetHashCode() { throw null; }
        int System.Collections.IStructuralComparable.CompareTo(object other, System.Collections.IComparer comparer) { throw null; }
        bool System.Collections.IStructuralEquatable.Equals(object other, System.Collections.IEqualityComparer comparer) { throw null; }
        int System.Collections.IStructuralEquatable.GetHashCode(System.Collections.IEqualityComparer comparer) { throw null; }
        int System.IComparable.CompareTo(object obj) { throw null; }
        public override string ToString() { throw null; }
    }
    public partial class Tuple<T1, T2, T3, T4, T5, T6, T7> : System.Collections.IStructuralComparable, System.Collections.IStructuralEquatable, System.IComparable
    {
        public Tuple(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7) { }
        public T1 Item1 { get { throw null; } }
        public T2 Item2 { get { throw null; } }
        public T3 Item3 { get { throw null; } }
        public T4 Item4 { get { throw null; } }
        public T5 Item5 { get { throw null; } }
        public T6 Item6 { get { throw null; } }
        public T7 Item7 { get { throw null; } }
        public override bool Equals(object obj) { throw null; }
        public override int GetHashCode() { throw null; }
        int System.Collections.IStructuralComparable.CompareTo(object other, System.Collections.IComparer comparer) { throw null; }
        bool System.Collections.IStructuralEquatable.Equals(object other, System.Collections.IEqualityComparer comparer) { throw null; }
        int System.Collections.IStructuralEquatable.GetHashCode(System.Collections.IEqualityComparer comparer) { throw null; }
        int System.IComparable.CompareTo(object obj) { throw null; }
        public override string ToString() { throw null; }
    }
    public partial class Tuple<T1, T2, T3, T4, T5, T6, T7, TRest> : System.Collections.IStructuralComparable, System.Collections.IStructuralEquatable, System.IComparable
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
        public override bool Equals(object obj) { throw null; }
        public override int GetHashCode() { throw null; }
        int System.Collections.IStructuralComparable.CompareTo(object other, System.Collections.IComparer comparer) { throw null; }
        bool System.Collections.IStructuralEquatable.Equals(object other, System.Collections.IEqualityComparer comparer) { throw null; }
        int System.Collections.IStructuralEquatable.GetHashCode(System.Collections.IEqualityComparer comparer) { throw null; }
        int System.IComparable.CompareTo(object obj) { throw null; }
        public override string ToString() { throw null; }
    }
    [System.Runtime.InteropServices.ClassInterfaceAttribute((System.Runtime.InteropServices.ClassInterfaceType)(0))]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public abstract partial class Type : System.Reflection.MemberInfo, System.Reflection.IReflect
    {
        public static readonly char Delimiter;
        public static readonly System.Type[] EmptyTypes;
        public static readonly System.Reflection.MemberFilter FilterAttribute;
        public static readonly System.Reflection.MemberFilter FilterName;
        public static readonly System.Reflection.MemberFilter FilterNameIgnoreCase;
        public static readonly object Missing;
        protected Type() { }
        [System.Security.SecuritySafeCriticalAttribute][System.Diagnostics.Contracts.PureAttribute][System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)]
        public static bool operator ==(System.Type left, System.Type right) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute][System.Diagnostics.Contracts.PureAttribute][System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)]
        public static bool operator !=(System.Type left, System.Type right) { throw null; }
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
        public bool IsClass { get { throw null; } }
        public bool IsCOMObject { get { throw null; } }
        public virtual bool IsConstructedGenericType { get { throw null; } }
        public bool IsContextful { [System.Diagnostics.Contracts.PureAttribute]get { throw null;} }
        public bool IsEnum { get { throw null; } }
        public bool IsExplicitLayout { get { throw null; } }
        public virtual bool IsGenericParameter { get { throw null; } }
        public virtual bool IsGenericType { get { throw null; } }
        public virtual bool IsGenericTypeDefinition { get { throw null; } }
        public bool IsImport { get { throw null; } }
        public bool IsInterface { [System.Security.SecuritySafeCriticalAttribute]get { throw null; } }
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
        public virtual bool IsSerializable { get { throw null; } }
        public bool IsSpecialName { get { throw null; } }
        public bool IsUnicodeClass { get { throw null; } }
        public bool IsValueType { get { throw null; } }
        public bool IsVisible { get { throw null; } }
        public override System.Reflection.MemberTypes MemberType { get { throw null; } }
        public abstract new System.Reflection.Module Module { get; }
        public abstract string Namespace { get; }
        public override System.Type ReflectedType { get { throw null; } }
        public virtual System.Runtime.InteropServices.StructLayoutAttribute StructLayoutAttribute { get { throw null; } }
        public virtual System.RuntimeTypeHandle TypeHandle { get { throw null; } }
        [System.Runtime.InteropServices.ComVisibleAttribute(true)]
        public System.Reflection.ConstructorInfo TypeInitializer { get { throw null; } }
        public abstract System.Type UnderlyingSystemType { get; }
        public override bool Equals(object o) { throw null; }
        public bool Equals(System.Type o) { throw null; }
        public virtual System.Type[] FindInterfaces(System.Reflection.TypeFilter filter, object filterCriteria) { throw null; }
        public virtual System.Reflection.MemberInfo[] FindMembers(System.Reflection.MemberTypes memberType, System.Reflection.BindingFlags bindingAttr, System.Reflection.MemberFilter filter, object filterCriteria) { throw null; }
        public virtual int GetArrayRank() { throw null; }
        protected abstract System.Reflection.TypeAttributes GetAttributeFlagsImpl();
        [System.Runtime.InteropServices.ComVisibleAttribute(true)]
        public System.Reflection.ConstructorInfo GetConstructor(System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder binder, System.Type[] types, System.Reflection.ParameterModifier[] modifiers) { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(true)]
        public System.Reflection.ConstructorInfo GetConstructor(System.Type[] types) { throw null; }
        protected abstract System.Reflection.ConstructorInfo GetConstructorImpl(System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder binder, System.Reflection.CallingConventions callConvention, System.Type[] types, System.Reflection.ParameterModifier[] modifiers);
        [System.Runtime.InteropServices.ComVisibleAttribute(true)]
        public System.Reflection.ConstructorInfo[] GetConstructors() { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(true)]
        public abstract System.Reflection.ConstructorInfo[] GetConstructors(System.Reflection.BindingFlags bindingAttr);
        [System.Runtime.InteropServices.ComVisibleAttribute(true)]
        public System.Reflection.ConstructorInfo GetConstructor(System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder binder, System.Reflection.CallingConventions callConvention, System.Type[] types, System.Reflection.ParameterModifier[] modifiers) { throw null; }
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
        [System.Runtime.InteropServices.ComVisibleAttribute(true)]
        public virtual System.Reflection.InterfaceMapping GetInterfaceMap(System.Type interfaceType) { throw null; }
        public abstract System.Type[] GetInterfaces();
        public System.Reflection.MemberInfo[] GetMember(string name) { throw null; }
        public virtual System.Reflection.MemberInfo[] GetMember(string name, System.Reflection.BindingFlags bindingAttr) { throw null; }
        public virtual System.Reflection.MemberInfo[] GetMember(string name, System.Reflection.MemberTypes type, System.Reflection.BindingFlags bindingAttr) { throw null; }
        public System.Reflection.MemberInfo[] GetMembers() { throw null; }
        public abstract System.Reflection.MemberInfo[] GetMembers(System.Reflection.BindingFlags bindingAttr);
        public System.Reflection.MethodInfo GetMethod(string name) { throw null; }
        public System.Reflection.MethodInfo GetMethod(string name, System.Reflection.BindingFlags bindingAttr) { throw null; }
        public System.Reflection.MethodInfo GetMethod(string name, System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder binder, System.Reflection.CallingConventions callConvention, System.Type[] types, System.Reflection.ParameterModifier[] modifiers) { throw null; }
        public System.Reflection.MethodInfo GetMethod(string name, System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder binder, System.Type[] types, System.Reflection.ParameterModifier[] modifiers) { throw null; }
        public System.Reflection.MethodInfo GetMethod(string name, System.Type[] types) { throw null; }
        public System.Reflection.MethodInfo GetMethod(string name, System.Type[] types, System.Reflection.ParameterModifier[] modifiers) { throw null; }
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
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public static System.Type GetType(string typeName) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public static System.Type GetType(string typeName, bool throwOnError) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public static System.Type GetType(string typeName, bool throwOnError, bool ignoreCase) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public static System.Type GetType(string typeName, System.Func<System.Reflection.AssemblyName, System.Reflection.Assembly> assemblyResolver, System.Func<System.Reflection.Assembly, string, bool, System.Type> typeResolver) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public static System.Type GetType(string typeName, System.Func<System.Reflection.AssemblyName, System.Reflection.Assembly> assemblyResolver, System.Func<System.Reflection.Assembly, string, bool, System.Type> typeResolver, bool throwOnError) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public static System.Type GetType(string typeName, System.Func<System.Reflection.AssemblyName, System.Reflection.Assembly> assemblyResolver, System.Func<System.Reflection.Assembly, string, bool, System.Type> typeResolver, bool throwOnError, bool ignoreCase) { throw null; }
        public static System.Type[] GetTypeArray(System.Object[] args) { throw null; }
        public static System.TypeCode GetTypeCode(System.Type type) { throw null; }
        protected virtual System.TypeCode GetTypeCodeImpl() { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]public static System.Type GetTypeFromCLSID(System.Guid clsid) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]public static System.Type GetTypeFromCLSID(System.Guid clsid, bool throwOnError) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]public static System.Type GetTypeFromCLSID(System.Guid clsid, string server) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]public static System.Type GetTypeFromCLSID(System.Guid clsid, string server, bool throwOnError) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)][System.Security.SecuritySafeCriticalAttribute]
        public static System.Type GetTypeFromHandle(System.RuntimeTypeHandle handle) { throw null; }
        [System.Security.SecurityCriticalAttribute]public static System.Type GetTypeFromProgID(string progID) { throw null; }
        [System.Security.SecurityCriticalAttribute]public static System.Type GetTypeFromProgID(string progID, bool throwOnError) { throw null; }
        [System.Security.SecurityCriticalAttribute]public static System.Type GetTypeFromProgID(string progID, string server) { throw null; }
        [System.Security.SecurityCriticalAttribute]public static System.Type GetTypeFromProgID(string progID, string server, bool throwOnError) { throw null; }
        public static System.RuntimeTypeHandle GetTypeHandle(object o) { throw null; }
        protected abstract bool HasElementTypeImpl();
        [System.Diagnostics.DebuggerHiddenAttribute]
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public object InvokeMember(string name, System.Reflection.BindingFlags invokeAttr, System.Reflection.Binder binder, object target, object[] args) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute][System.Diagnostics.DebuggerHiddenAttribute]
        public System.Object InvokeMember(System.String name, System.Reflection.BindingFlags invokeAttr, System.Reflection.Binder binder, System.Object target, System.Object[] args, System.Globalization.CultureInfo culture) { throw null; }
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
        [System.Runtime.InteropServices.ComVisibleAttribute(true)]
        public virtual bool IsSubclassOf(System.Type c) { throw null; }
        protected virtual bool IsValueTypeImpl() { throw null; }
        public virtual System.Type MakeArrayType() { throw null; }
        public virtual System.Type MakeArrayType(int rank) { throw null; }
        public virtual System.Type MakeByRefType() { throw null; }
        public virtual System.Type MakeGenericType(params System.Type[] typeArguments) { throw null; }
        public virtual System.Type MakePointerType() { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public static System.Type ReflectionOnlyGetType(System.String typeName, bool throwIfNotFound, bool ignoreCase) { throw null; }
        public override string ToString() { throw null; }
    }
    public partial class TypeAccessException : System.TypeLoadException
    {
        public TypeAccessException() { }
        public TypeAccessException(string message) { }
        public TypeAccessException(string message, System.Exception inner) { }
        protected TypeAccessException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
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
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct TypedReference
    {
        public override bool Equals(object o) { throw null; }
        public override int GetHashCode() { throw null; }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class TypeInitializationException : System.SystemException
    {
        public TypeInitializationException(string fullTypeName, System.Exception innerException) { }
        public string TypeName { get { throw null; } }
        [System.Security.SecurityCriticalAttribute]
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class TypeLoadException : System.SystemException, System.Runtime.Serialization.ISerializable
    {
        public TypeLoadException() { }
        public TypeLoadException(string message) { }
        public TypeLoadException(string message, System.Exception inner) { }
        protected TypeLoadException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
        public override string Message { [System.Security.SecuritySafeCriticalAttribute]get { throw null; } }
        public string TypeName { get { throw null; } }
        [System.Security.SecurityCriticalAttribute]
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class TypeUnloadedException : System.SystemException
    {
        public TypeUnloadedException() { }
        protected TypeUnloadedException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public TypeUnloadedException(string message) { }
        public TypeUnloadedException(string message, System.Exception innerException) { }
    }
    internal sealed class TypeNameParser : System.IDisposable
    {
        [System.Security.SecuritySafeCriticalAttribute]public void Dispose() { throw null; }
    }
    [System.Security.SecurityCriticalAttribute]
    internal class SafeTypeNameParserHandle : Microsoft.Win32.SafeHandles.SafeHandleZeroOrMinusOneIsInvalid
    {
        public SafeTypeNameParserHandle(): base(default(bool)) { }
        [System.Security.SecurityCriticalAttribute]
        protected override bool ReleaseHandle() { throw null; }
    }
    [System.CLSCompliantAttribute(false)]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct UInt16 : System.IComparable, System.IComparable<ushort>, System.IConvertible, System.IEquatable<ushort>, System.IFormattable
    {
        public const ushort MaxValue = (ushort)65535;
        public const ushort MinValue = (ushort)0;
        public int CompareTo(object value) { throw null; }
        public int CompareTo(System.UInt16 value) { throw null; }
        public override bool Equals(object obj) { throw null; }
        public bool Equals(System.UInt16 obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public System.TypeCode GetTypeCode() { throw null; }
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
        [System.Security.SecuritySafeCriticalAttribute]
        public override string ToString() { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public string ToString(System.IFormatProvider provider) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public string ToString(string format) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public string ToString(string format, System.IFormatProvider provider) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static bool TryParse(string s, System.Globalization.NumberStyles style, System.IFormatProvider provider, out System.UInt16 result) { result = default(ushort); throw null; }
        [System.CLSCompliantAttribute(false)]
        public static bool TryParse(string s, out System.UInt16 result) { result = default(ushort); throw null; }
    }
    [System.CLSCompliantAttribute(false)]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct UInt32 : System.IComparable, System.IComparable<uint>, System.IConvertible, System.IEquatable<uint>, System.IFormattable
    {
        public const uint MaxValue = (uint)4294967295;
        public const uint MinValue = (uint)0;
        public int CompareTo(object value) { throw null; }
        public int CompareTo(System.UInt32 value) { throw null; }
        public override bool Equals(object obj) { throw null; }
        public bool Equals(System.UInt32 obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public System.TypeCode GetTypeCode() { throw null; }
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
        [System.Security.SecuritySafeCriticalAttribute]
        public override string ToString() { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public string ToString(System.IFormatProvider provider) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public string ToString(string format) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public string ToString(string format, System.IFormatProvider provider) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static bool TryParse(string s, System.Globalization.NumberStyles style, System.IFormatProvider provider, out System.UInt32 result) { result = default(uint); throw null; }
        [System.CLSCompliantAttribute(false)]
        public static bool TryParse(string s, out System.UInt32 result) { result = default(uint); throw null; }
    }
    [System.CLSCompliantAttribute(false)]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct UInt64 : System.IComparable, System.IComparable<ulong>, System.IConvertible, System.IEquatable<ulong>, System.IFormattable
    {
        public const ulong MaxValue = (ulong)18446744073709551615;
        public const ulong MinValue = (ulong)0;
        public int CompareTo(object value) { throw null; }
        public int CompareTo(System.UInt64 value) { throw null; }
        public override bool Equals(object obj) { throw null; }
        public bool Equals(System.UInt64 obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public System.TypeCode GetTypeCode() { throw null; }
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
        [System.Security.SecuritySafeCriticalAttribute]
        public override string ToString() { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public string ToString(System.IFormatProvider provider) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public string ToString(string format) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public string ToString(string format, System.IFormatProvider provider) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static bool TryParse(string s, System.Globalization.NumberStyles style, System.IFormatProvider provider, out System.UInt64 result) { result = default(ulong); throw null; }
        [System.CLSCompliantAttribute(false)]
        public static bool TryParse(string s, out System.UInt64 result) { result = default(ulong); throw null; }
    }
    [System.CLSCompliantAttribute(false)]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct UIntPtr : System.Runtime.Serialization.ISerializable
    {
        public static readonly System.UIntPtr Zero;
        [System.Security.SecuritySafeCriticalAttribute]
        public UIntPtr(uint value) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public UIntPtr(ulong value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        [System.Security.SecurityCriticalAttribute]
        public unsafe UIntPtr(void* value) { throw null; }
        public static int Size { get { throw null; } }
        public static System.UIntPtr Add(System.UIntPtr pointer, int offset) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public override bool Equals(object obj) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public override int GetHashCode() { throw null; }
        public static System.UIntPtr operator +(System.UIntPtr pointer, int offset) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public static bool operator ==(System.UIntPtr value1, System.UIntPtr value2) { throw null; }
        public static explicit operator System.UIntPtr(uint value) { throw null; }
        public static explicit operator System.UIntPtr(ulong value) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public static explicit operator uint(System.UIntPtr value) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public static explicit operator ulong(System.UIntPtr value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        [System.Security.SecurityCriticalAttribute]
        public unsafe static explicit operator void* (System.UIntPtr value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        [System.Security.SecurityCriticalAttribute]
        public unsafe static explicit operator System.UIntPtr(void* value) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public static bool operator !=(System.UIntPtr value1, System.UIntPtr value2) { throw null; }
        public static System.UIntPtr operator -(System.UIntPtr pointer, int offset) { throw null; }
        public static System.UIntPtr Subtract(System.UIntPtr pointer, int offset) { throw null; }
        [System.Security.SecurityCriticalAttribute]
        void System.Runtime.Serialization.ISerializable.GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        [System.CLSCompliantAttribute(false)]
        [System.Security.SecuritySafeCriticalAttribute]
        public unsafe void* ToPointer() { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public override string ToString() { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public uint ToUInt32() { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public ulong ToUInt64() { throw null; }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class UnauthorizedAccessException : System.SystemException
    {
        public UnauthorizedAccessException() { }
        public UnauthorizedAccessException(string message) { }
        public UnauthorizedAccessException(string message, System.Exception inner) { }
        protected UnauthorizedAccessException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class UnhandledExceptionEventArgs : System.EventArgs
    {
        public UnhandledExceptionEventArgs(object exception, bool isTerminating) { }
        public object ExceptionObject { get { throw null; } }
        public bool IsTerminating { get { throw null; } }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    [System.Security.SecurityCriticalAttribute]
    public delegate void UnhandledExceptionEventHandler(object sender, System.UnhandledExceptionEventArgs e);
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public abstract partial class ValueType
    {
        protected ValueType() { }
        [System.Security.SecuritySafeCriticalAttribute]
        public override bool Equals(object obj) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)][System.Security.SecuritySafeCriticalAttribute]
        public override int GetHashCode() { throw null; }
        public override string ToString() { throw null; }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class Version : System.ICloneable, System.IComparable, System.IComparable<System.Version>, System.IEquatable<System.Version>
    {
        public Version(int major, int minor) { }
        public Version(int major, int minor, int build) { }
        public Version(int major, int minor, int build, int revision) { }
        public Version(string version) { }
        public Version() { }
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
        public static System.Version Parse(string input) { throw null; }
        public override string ToString() { throw null; }
        public string ToString(int fieldCount) { throw null; }
        public static bool TryParse(string input, out System.Version result) { result = default(System.Version); throw null; }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential, Size=1)]
    public partial struct Void
    {
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class WeakReference : System.Runtime.Serialization.ISerializable
    {
        [System.Security.SecuritySafeCriticalAttribute]
        protected WeakReference() { }
        public WeakReference(object target) { }
        public WeakReference(object target, bool trackResurrection) { }
        protected WeakReference(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public virtual bool IsAlive { [System.Security.SecuritySafeCriticalAttribute][System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)]get { throw null; } }
        public virtual object Target { [System.Security.SecuritySafeCriticalAttribute][System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)]get { throw null; } [System.Security.SecuritySafeCriticalAttribute][System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)]set { } }
        public virtual void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public virtual bool TrackResurrection { get { throw null; } }
        ~WeakReference() { }
    }
    public sealed partial class WeakReference<T> : System.Runtime.Serialization.ISerializable where T : class
    {
        public WeakReference(T target) { }
        public WeakReference(T target, bool trackResurrection) { }
        ~WeakReference() { }
        public void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public void SetTarget(T target) { }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]public bool TryGetTarget(out T target) { target = default(T); throw null; }
    }
}
namespace System.Collections
{
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class BitArray : System.Collections.ICollection, System.Collections.IEnumerable, System.ICloneable
    {
        public BitArray(bool[] values) { }
        public BitArray(byte[] bytes) { }
        public BitArray(System.Collections.BitArray bits) { }
        public BitArray(int length) { }
        public BitArray(int length, bool defaultValue) { }
        public BitArray(int[] values) { }
        public int Count { get { throw null; } }
        public bool IsReadOnly { get { throw null; } }
        public bool IsSynchronized { get { throw null; } }
        public bool this[int index] { get { throw null; } set { } }
        public int Length { get { throw null; } set { } }
        public object SyncRoot { get { throw null; } }
        public System.Collections.BitArray And(System.Collections.BitArray value) { throw null; }
        public object Clone() { throw null; }
        public void CopyTo(System.Array array, int index) { }
        public bool Get(int index) { throw null; }
        public System.Collections.IEnumerator GetEnumerator() { throw null; }
        public System.Collections.BitArray Not() { throw null; }
        public System.Collections.BitArray Or(System.Collections.BitArray value) { throw null; }
        public void Set(int index, bool value) { }
        public void SetAll(bool value) { }
        public System.Collections.BitArray Xor(System.Collections.BitArray value) { throw null; }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public abstract partial class CollectionBase : System.Collections.ICollection, System.Collections.IEnumerable, System.Collections.IList
    {
        protected CollectionBase() { }
        public int Count { get { throw null; } }
        protected System.Collections.IList List { get { throw null; } }
        bool System.Collections.ICollection.IsSynchronized { get { throw null; } }
        object System.Collections.ICollection.SyncRoot { get { throw null; } }
        bool System.Collections.IList.IsFixedSize { get { throw null; } }
        bool System.Collections.IList.IsReadOnly { get { throw null; } }
        object System.Collections.IList.this[int index] { get { throw null; } set { } }
        public void Clear() { }
        public System.Collections.IEnumerator GetEnumerator() { throw null; }
        public void RemoveAt(int index) { }
        void System.Collections.ICollection.CopyTo(System.Array array, int index) { }
        int System.Collections.IList.Add(object value) { throw null; }
        bool System.Collections.IList.Contains(object value) { throw null; }
        int System.Collections.IList.IndexOf(object value) { throw null; }
        void System.Collections.IList.Insert(int index, object value) { }
        void System.Collections.IList.Remove(object value) { }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct DictionaryEntry
    {
        public DictionaryEntry(object key, object value) { throw null;}
        public object Key { get { throw null; } set { } }
        public object Value { get { throw null; } set { } }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial interface ICollection : System.Collections.IEnumerable
    {
        int Count { get; }
        bool IsSynchronized { get; }
        object SyncRoot { get; }
        void CopyTo(System.Array array, int index);
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial interface IComparer
    {
        int Compare(object x, object y);
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
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
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial interface IDictionaryEnumerator : System.Collections.IEnumerator
    {
        System.Collections.DictionaryEntry Entry { get; }
        object Key { get; }
        object Value { get; }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    [System.Runtime.InteropServices.GuidAttribute("496B0ABE-CDEE-11d3-88E8-00902754C43A")]
    public partial interface IEnumerable
    {
        [System.Runtime.InteropServices.DispIdAttribute(-4)]
        System.Collections.IEnumerator GetEnumerator();
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    [System.Runtime.InteropServices.GuidAttribute("496B0ABF-CDEE-11d3-88E8-00902754C43A")]
    public partial interface IEnumerator
    {
        object Current { get; }
        bool MoveNext();
        void Reset();
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial interface IEqualityComparer
    {
        bool Equals(object x, object y);
        int GetHashCode(object obj);
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
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
    public static partial class StructuralComparisons
    {
        public static System.Collections.IComparer StructuralComparer { get { throw null; } }
        public static System.Collections.IEqualityComparer StructuralEqualityComparer { get { throw null; } }
    }
}
namespace System.Collections.Generic
{
    public abstract partial class Comparer<T> : System.Collections.Generic.IComparer<T>, System.Collections.IComparer
    {
        protected Comparer() { }
        public static System.Collections.Generic.Comparer<T> Default { get { throw null; } }
        public abstract int Compare(T x, T y);
        public static System.Collections.Generic.Comparer<T> Create(System.Comparison<T> comparison) { throw null; }
        int System.Collections.IComparer.Compare(object x, object y) { throw null; }
    }
    [System.Diagnostics.DebuggerDisplayAttribute("Count = {Count}")]
    [System.Runtime.InteropServices.ComVisibleAttribute(false)]
    public partial class Dictionary<TKey, TValue> : System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<TKey, TValue>>, System.Collections.Generic.IDictionary<TKey, TValue>, System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<TKey, TValue>>, System.Collections.Generic.IReadOnlyCollection<System.Collections.Generic.KeyValuePair<TKey, TValue>>, System.Collections.Generic.IReadOnlyDictionary<TKey, TValue>, System.Collections.ICollection, System.Collections.IDictionary, System.Collections.IEnumerable, System.Runtime.Serialization.IDeserializationCallback, System.Runtime.Serialization.ISerializable
    {
        public Dictionary() { }
        public Dictionary(System.Collections.Generic.IDictionary<TKey, TValue> dictionary) { }
        public Dictionary(System.Collections.Generic.IDictionary<TKey, TValue> dictionary, System.Collections.Generic.IEqualityComparer<TKey> comparer) { }
        public Dictionary(System.Collections.Generic.IEqualityComparer<TKey> comparer) { }
        public Dictionary(int capacity) { }
        public Dictionary(int capacity, System.Collections.Generic.IEqualityComparer<TKey> comparer) { }
        protected Dictionary(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public System.Collections.Generic.IEqualityComparer<TKey> Comparer { get { throw null; } }
        public int Count { get { throw null; } }
        public TValue this[TKey key] { get { throw null; } set { } }
        public System.Collections.Generic.Dictionary<TKey, TValue>.KeyCollection Keys { get { throw null; } }
        bool System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<TKey,TValue>>.IsReadOnly { get { throw null; } }
        System.Collections.Generic.ICollection<TKey> System.Collections.Generic.IDictionary<TKey,TValue>.Keys { get { throw null; } }
        System.Collections.Generic.ICollection<TValue> System.Collections.Generic.IDictionary<TKey,TValue>.Values { get { throw null; } }
        System.Collections.Generic.IEnumerable<TKey> System.Collections.Generic.IReadOnlyDictionary<TKey,TValue>.Keys { get { throw null; } }
        System.Collections.Generic.IEnumerable<TValue> System.Collections.Generic.IReadOnlyDictionary<TKey,TValue>.Values { get { throw null; } }
        bool System.Collections.ICollection.IsSynchronized { get { throw null; } }
        object System.Collections.ICollection.SyncRoot { get { throw null; } }
        bool System.Collections.IDictionary.IsFixedSize { get { throw null; } }
        bool System.Collections.IDictionary.IsReadOnly { get { throw null; } }
        object System.Collections.IDictionary.this[object key] { get { throw null; } set { } }
        System.Collections.ICollection System.Collections.IDictionary.Keys { get { throw null; } }
        System.Collections.ICollection System.Collections.IDictionary.Values { get { throw null; } }
        public System.Collections.Generic.Dictionary<TKey, TValue>.ValueCollection Values { get { throw null; } }
        public void Add(TKey key, TValue value) { }
        public void Clear() { }
        public bool ContainsKey(TKey key) { throw null; }
        public bool ContainsValue(TValue value) { throw null; }
        public System.Collections.Generic.Dictionary<TKey, TValue>.Enumerator GetEnumerator() { throw null; }
        [System.Security.SecurityCriticalAttribute]
        public virtual void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public virtual void OnDeserialization(object sender) { }
        public bool Remove(TKey key) { throw null; }
        void System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<TKey,TValue>>.Add(System.Collections.Generic.KeyValuePair<TKey, TValue> keyValuePair) { }
        bool System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<TKey,TValue>>.Contains(System.Collections.Generic.KeyValuePair<TKey, TValue> keyValuePair) { throw null; }
        void System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<TKey,TValue>>.CopyTo(System.Collections.Generic.KeyValuePair<TKey, TValue>[] array, int index) { }
        bool System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<TKey,TValue>>.Remove(System.Collections.Generic.KeyValuePair<TKey, TValue> keyValuePair) { throw null; }
        System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<TKey, TValue>> System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<TKey,TValue>>.GetEnumerator() { throw null; }
        void System.Collections.ICollection.CopyTo(System.Array array, int index) { }
        void System.Collections.IDictionary.Add(object key, object value) { }
        bool System.Collections.IDictionary.Contains(object key) { throw null; }
        System.Collections.IDictionaryEnumerator System.Collections.IDictionary.GetEnumerator() { throw null; }
        void System.Collections.IDictionary.Remove(object key) { }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        public bool TryGetValue(TKey key, out TValue value) { value = default(TValue); throw null; }
        [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
        public partial struct Enumerator : System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<TKey, TValue>>, System.Collections.IDictionaryEnumerator, System.Collections.IEnumerator, System.IDisposable
        {
            public System.Collections.Generic.KeyValuePair<TKey, TValue> Current { get { throw null; } }
            System.Collections.DictionaryEntry System.Collections.IDictionaryEnumerator.Entry { get { throw null; } }
            object System.Collections.IDictionaryEnumerator.Key { get { throw null; } }
            object System.Collections.IDictionaryEnumerator.Value { get { throw null; } }
            object System.Collections.IEnumerator.Current { get { throw null; } }
            public void Dispose() { }
            public bool MoveNext() { throw null; }
            void System.Collections.IEnumerator.Reset() { }
        }
        [System.Diagnostics.DebuggerDisplayAttribute("Count = {Count}")]
        public sealed partial class KeyCollection : System.Collections.Generic.ICollection<TKey>, System.Collections.Generic.IEnumerable<TKey>, System.Collections.Generic.IReadOnlyCollection<TKey>, System.Collections.ICollection, System.Collections.IEnumerable
        {
            public KeyCollection(System.Collections.Generic.Dictionary<TKey, TValue> dictionary) { }
            public int Count { get { throw null; } }
            bool System.Collections.Generic.ICollection<TKey>.IsReadOnly { get { throw null; } }
            bool System.Collections.ICollection.IsSynchronized { get { throw null; } }
            object System.Collections.ICollection.SyncRoot { get { throw null; } }
            public void CopyTo(TKey[] array, int index) { }
            public System.Collections.Generic.Dictionary<TKey, TValue>.KeyCollection.Enumerator GetEnumerator() { throw null; }
            void System.Collections.Generic.ICollection<TKey>.Add(TKey item) { }
            void System.Collections.Generic.ICollection<TKey>.Clear() { }
            bool System.Collections.Generic.ICollection<TKey>.Contains(TKey item) { throw null; }
            bool System.Collections.Generic.ICollection<TKey>.Remove(TKey item) { throw null; }
            System.Collections.Generic.IEnumerator<TKey> System.Collections.Generic.IEnumerable<TKey>.GetEnumerator() { throw null; }
            void System.Collections.ICollection.CopyTo(System.Array array, int index) { }
            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
            [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
            public partial struct Enumerator : System.Collections.Generic.IEnumerator<TKey>, System.Collections.IEnumerator, System.IDisposable
            {
                public TKey Current { get { throw null; } }
                object System.Collections.IEnumerator.Current { get { throw null; } }
                public void Dispose() { }
                public bool MoveNext() { throw null; }
                void System.Collections.IEnumerator.Reset() { }
            }
        }
        [System.Diagnostics.DebuggerDisplayAttribute("Count = {Count}")]
        public sealed partial class ValueCollection : System.Collections.Generic.ICollection<TValue>, System.Collections.Generic.IEnumerable<TValue>, System.Collections.Generic.IReadOnlyCollection<TValue>, System.Collections.ICollection, System.Collections.IEnumerable
        {
            public ValueCollection(System.Collections.Generic.Dictionary<TKey, TValue> dictionary) { }
            public int Count { get { throw null; } }
            bool System.Collections.Generic.ICollection<TValue>.IsReadOnly { get { throw null; } }
            bool System.Collections.ICollection.IsSynchronized { get { throw null; } }
            object System.Collections.ICollection.SyncRoot { get { throw null; } }
            public void CopyTo(TValue[] array, int index) { }
            public System.Collections.Generic.Dictionary<TKey, TValue>.ValueCollection.Enumerator GetEnumerator() { throw null; }
            void System.Collections.Generic.ICollection<TValue>.Add(TValue item) { }
            void System.Collections.Generic.ICollection<TValue>.Clear() { }
            bool System.Collections.Generic.ICollection<TValue>.Contains(TValue item) { throw null; }
            bool System.Collections.Generic.ICollection<TValue>.Remove(TValue item) { throw null; }
            System.Collections.Generic.IEnumerator<TValue> System.Collections.Generic.IEnumerable<TValue>.GetEnumerator() { throw null; }
            void System.Collections.ICollection.CopyTo(System.Array array, int index) { }
            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
            [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
            public partial struct Enumerator : System.Collections.Generic.IEnumerator<TValue>, System.Collections.IEnumerator, System.IDisposable
            {
                public TValue Current { get { throw null; } }
                object System.Collections.IEnumerator.Current { get { throw null; } }
                public void Dispose() { }
                public bool MoveNext() { throw null; }
                void System.Collections.IEnumerator.Reset() { }
            }
        }
    }
    public abstract partial class EqualityComparer<T> : System.Collections.Generic.IEqualityComparer<T>, System.Collections.IEqualityComparer
    {
        protected EqualityComparer() { }
        public static System.Collections.Generic.EqualityComparer<T> Default { get { throw null; } }
        public abstract bool Equals(T x, T y);
        public abstract int GetHashCode(T obj);
        bool System.Collections.IEqualityComparer.Equals(object x, object y) { throw null; }
        int System.Collections.IEqualityComparer.GetHashCode(object obj) { throw null; }
    }
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
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class KeyNotFoundException : System.SystemException, System.Runtime.Serialization.ISerializable
    {
        public KeyNotFoundException() { }
        public KeyNotFoundException(string message) { }
        public KeyNotFoundException(string message, System.Exception innerException) { }
        protected KeyNotFoundException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct KeyValuePair<TKey, TValue>
    {
        public KeyValuePair(TKey key, TValue value) { throw null;}
        public TKey Key { get { throw null; } }
        public TValue Value { get { throw null; } }
        public override string ToString() { throw null; }
    }
    [System.Diagnostics.DebuggerDisplayAttribute("Count = {Count}")]
    public partial class List<T> : System.Collections.Generic.ICollection<T>, System.Collections.Generic.IEnumerable<T>, System.Collections.Generic.IList<T>, System.Collections.Generic.IReadOnlyCollection<T>, System.Collections.Generic.IReadOnlyList<T>, System.Collections.ICollection, System.Collections.IEnumerable, System.Collections.IList
    {
        public List() { }
        public List(System.Collections.Generic.IEnumerable<T> collection) { }
        public List(int capacity) { }
        public int Capacity { get { throw null; } set { } }
        public int Count { get { throw null; } }
        public T this[int index] { get { throw null; } set { } }
        bool System.Collections.Generic.ICollection<T>.IsReadOnly { get { throw null; } }
        bool System.Collections.ICollection.IsSynchronized { get { throw null; } }
        object System.Collections.ICollection.SyncRoot { get { throw null; } }
        bool System.Collections.IList.IsFixedSize { get { throw null; } }
        bool System.Collections.IList.IsReadOnly { get { throw null; } }
        object System.Collections.IList.this[int index] { get { throw null; } set { } }
        public void Add(T item) { }
        public void AddRange(System.Collections.Generic.IEnumerable<T> collection) { }
        public System.Collections.ObjectModel.ReadOnlyCollection<T> AsReadOnly() { throw null; }
        public int BinarySearch(T item) { throw null; }
        public int BinarySearch(T item, System.Collections.Generic.IComparer<T> comparer) { throw null; }
        public int BinarySearch(int index, int count, T item, System.Collections.Generic.IComparer<T> comparer) { throw null; }
        public void Clear() { }
        public bool Contains(T item) { throw null; }
        public System.Collections.Generic.List<TOutput> ConvertAll<TOutput>(System.Converter<T, TOutput> converter) { throw null; }
        public void CopyTo(T[] array) { }
        public void CopyTo(T[] array, int arrayIndex) { }
        public void CopyTo(int index, T[] array, int arrayIndex, int count) { }
        public bool Exists(System.Predicate<T> match) { throw null; }
        public T Find(System.Predicate<T> match) { throw null; }
        public System.Collections.Generic.List<T> FindAll(System.Predicate<T> match) { throw null; }
        public int FindIndex(int startIndex, int count, System.Predicate<T> match) { throw null; }
        public int FindIndex(int startIndex, System.Predicate<T> match) { throw null; }
        public int FindIndex(System.Predicate<T> match) { throw null; }
        public T FindLast(System.Predicate<T> match) { throw null; }
        public int FindLastIndex(int startIndex, int count, System.Predicate<T> match) { throw null; }
        public int FindLastIndex(int startIndex, System.Predicate<T> match) { throw null; }
        public int FindLastIndex(System.Predicate<T> match) { throw null; }
        public void ForEach(System.Action<T> action) { }
        public System.Collections.Generic.List<T>.Enumerator GetEnumerator() { throw null; }
        public System.Collections.Generic.List<T> GetRange(int index, int count) { throw null; }
        public int IndexOf(T item) { throw null; }
        public int IndexOf(T item, int index) { throw null; }
        public int IndexOf(T item, int index, int count) { throw null; }
        public void Insert(int index, T item) { }
        public void InsertRange(int index, System.Collections.Generic.IEnumerable<T> collection) { }
        public int LastIndexOf(T item) { throw null; }
        public int LastIndexOf(T item, int index) { throw null; }
        public int LastIndexOf(T item, int index, int count) { throw null; }
        public bool Remove(T item) { throw null; }
        public int RemoveAll(System.Predicate<T> match) { throw null; }
        public void RemoveAt(int index) { }
        public void RemoveRange(int index, int count) { }
        public void Reverse() { }
        public void Reverse(int index, int count) { }
        public void Sort() { }
        public void Sort(System.Collections.Generic.IComparer<T> comparer) { }
        public void Sort(System.Comparison<T> comparison) { }
        public void Sort(int index, int count, System.Collections.Generic.IComparer<T> comparer) { }
        System.Collections.Generic.IEnumerator<T> System.Collections.Generic.IEnumerable<T>.GetEnumerator() { throw null; }
        void System.Collections.ICollection.CopyTo(System.Array array, int arrayIndex) { }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        int System.Collections.IList.Add(object item) { throw null; }
        bool System.Collections.IList.Contains(object item) { throw null; }
        int System.Collections.IList.IndexOf(object item) { throw null; }
        void System.Collections.IList.Insert(int index, object item) { }
        void System.Collections.IList.Remove(object item) { }
        public T[] ToArray() { throw null; }
        public void TrimExcess() { }
        public bool TrueForAll(System.Predicate<T> match) { throw null; }
        [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
        public partial struct Enumerator : System.Collections.Generic.IEnumerator<T>, System.Collections.IEnumerator, System.IDisposable
        {
            public T Current { get { throw null; } }
            object System.Collections.IEnumerator.Current { get { throw null; } }
            public void Dispose() { }
            public bool MoveNext() { throw null; }
            void System.Collections.IEnumerator.Reset() { }
        }
    }
}
namespace System.Collections.ObjectModel
{
    [System.Diagnostics.DebuggerDisplayAttribute("Count = {Count}")]
    [System.Runtime.InteropServices.ComVisibleAttribute(false)]
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
    [System.Diagnostics.DebuggerDisplayAttribute("Count = {Count}")]
    [System.Runtime.InteropServices.ComVisibleAttribute(false)]
    public abstract partial class KeyedCollection<TKey, TItem> : System.Collections.ObjectModel.Collection<TItem>
    {
        protected KeyedCollection() { }
        protected KeyedCollection(System.Collections.Generic.IEqualityComparer<TKey> comparer) { }
        protected KeyedCollection(System.Collections.Generic.IEqualityComparer<TKey> comparer, int dictionaryCreationThreshold) { }
        public System.Collections.Generic.IEqualityComparer<TKey> Comparer { get { throw null; } }
        protected System.Collections.Generic.IDictionary<TKey, TItem> Dictionary { get { throw null; } }
        public TItem this[TKey key] { get { throw null; } }
        protected void ChangeItemKey(TItem item, TKey newKey) { }
        protected override void ClearItems() { }
        public bool Contains(TKey key) { throw null; }
        protected abstract TKey GetKeyForItem(TItem item);
        protected override void InsertItem(int index, TItem item) { }
        public bool Remove(TKey key) { throw null; }
        protected override void RemoveItem(int index) { }
        protected override void SetItem(int index, TItem item) { }
    }
    [System.Diagnostics.DebuggerDisplayAttribute("Count = {Count}")]
    [System.Runtime.InteropServices.ComVisibleAttribute(false)]
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
    [System.Diagnostics.DebuggerDisplayAttribute("Count = {Count}")]
    public partial class ReadOnlyDictionary<TKey, TValue> : System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<TKey, TValue>>, System.Collections.Generic.IDictionary<TKey, TValue>, System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<TKey, TValue>>, System.Collections.Generic.IReadOnlyCollection<System.Collections.Generic.KeyValuePair<TKey, TValue>>, System.Collections.Generic.IReadOnlyDictionary<TKey, TValue>, System.Collections.ICollection, System.Collections.IDictionary, System.Collections.IEnumerable
    {
        public ReadOnlyDictionary(System.Collections.Generic.IDictionary<TKey, TValue> dictionary) { }
        public int Count { get { throw null; } }
        protected System.Collections.Generic.IDictionary<TKey, TValue> Dictionary { get { throw null; } }
        public TValue this[TKey key] { get { throw null; } }
        public System.Collections.ObjectModel.ReadOnlyDictionary<TKey, TValue>.KeyCollection Keys { get { throw null; } }
        bool System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<TKey,TValue>>.IsReadOnly { get { throw null; } }
        TValue System.Collections.Generic.IDictionary<TKey,TValue>.this[TKey key] { get { throw null; } set { } }
        System.Collections.Generic.ICollection<TKey> System.Collections.Generic.IDictionary<TKey,TValue>.Keys { get { throw null; } }
        System.Collections.Generic.ICollection<TValue> System.Collections.Generic.IDictionary<TKey,TValue>.Values { get { throw null; } }
        System.Collections.Generic.IEnumerable<TKey> System.Collections.Generic.IReadOnlyDictionary<TKey,TValue>.Keys { get { throw null; } }
        System.Collections.Generic.IEnumerable<TValue> System.Collections.Generic.IReadOnlyDictionary<TKey,TValue>.Values { get { throw null; } }
        bool System.Collections.ICollection.IsSynchronized { get { throw null; } }
        object System.Collections.ICollection.SyncRoot { get { throw null; } }
        bool System.Collections.IDictionary.IsFixedSize { get { throw null; } }
        bool System.Collections.IDictionary.IsReadOnly { get { throw null; } }
        object System.Collections.IDictionary.this[object key] { get { throw null; } set { } }
        System.Collections.ICollection System.Collections.IDictionary.Keys { get { throw null; } }
        System.Collections.ICollection System.Collections.IDictionary.Values { get { throw null; } }
        public System.Collections.ObjectModel.ReadOnlyDictionary<TKey, TValue>.ValueCollection Values { get { throw null; } }
        public bool ContainsKey(TKey key) { throw null; }
        public System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<TKey, TValue>> GetEnumerator() { throw null; }
        void System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<TKey,TValue>>.Add(System.Collections.Generic.KeyValuePair<TKey, TValue> item) { }
        void System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<TKey,TValue>>.Clear() { }
        bool System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<TKey,TValue>>.Contains(System.Collections.Generic.KeyValuePair<TKey, TValue> item) { throw null; }
        void System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<TKey,TValue>>.CopyTo(System.Collections.Generic.KeyValuePair<TKey, TValue>[] array, int arrayIndex) { }
        bool System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<TKey,TValue>>.Remove(System.Collections.Generic.KeyValuePair<TKey, TValue> item) { throw null; }
        void System.Collections.Generic.IDictionary<TKey,TValue>.Add(TKey key, TValue value) { }
        bool System.Collections.Generic.IDictionary<TKey,TValue>.Remove(TKey key) { throw null; }
        void System.Collections.ICollection.CopyTo(System.Array array, int index) { }
        void System.Collections.IDictionary.Add(object key, object value) { }
        void System.Collections.IDictionary.Clear() { }
        bool System.Collections.IDictionary.Contains(object key) { throw null; }
        System.Collections.IDictionaryEnumerator System.Collections.IDictionary.GetEnumerator() { throw null; }
        void System.Collections.IDictionary.Remove(object key) { }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        public bool TryGetValue(TKey key, out TValue value) { value = default(TValue); throw null; }
        [System.Diagnostics.DebuggerDisplayAttribute("Count = {Count}")]
        public sealed partial class KeyCollection : System.Collections.Generic.ICollection<TKey>, System.Collections.Generic.IEnumerable<TKey>, System.Collections.Generic.IReadOnlyCollection<TKey>, System.Collections.ICollection, System.Collections.IEnumerable
        {
            internal KeyCollection() { }
            public int Count { get { throw null; } }
            bool System.Collections.Generic.ICollection<TKey>.IsReadOnly { get { throw null; } }
            bool System.Collections.ICollection.IsSynchronized { get { throw null; } }
            object System.Collections.ICollection.SyncRoot { get { throw null; } }
            public void CopyTo(TKey[] array, int arrayIndex) { }
            public System.Collections.Generic.IEnumerator<TKey> GetEnumerator() { throw null; }
            void System.Collections.Generic.ICollection<TKey>.Add(TKey item) { }
            void System.Collections.Generic.ICollection<TKey>.Clear() { }
            bool System.Collections.Generic.ICollection<TKey>.Contains(TKey item) { throw null; }
            bool System.Collections.Generic.ICollection<TKey>.Remove(TKey item) { throw null; }
            void System.Collections.ICollection.CopyTo(System.Array array, int index) { }
            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        }
        [System.Diagnostics.DebuggerDisplayAttribute("Count = {Count}")]
        public sealed partial class ValueCollection : System.Collections.Generic.ICollection<TValue>, System.Collections.Generic.IEnumerable<TValue>, System.Collections.Generic.IReadOnlyCollection<TValue>, System.Collections.ICollection, System.Collections.IEnumerable
        {
            internal ValueCollection() { }
            public int Count { get { throw null; } }
            bool System.Collections.Generic.ICollection<TValue>.IsReadOnly { get { throw null; } }
            bool System.Collections.ICollection.IsSynchronized { get { throw null; } }
            object System.Collections.ICollection.SyncRoot { get { throw null; } }
            public void CopyTo(TValue[] array, int arrayIndex) { }
            public System.Collections.Generic.IEnumerator<TValue> GetEnumerator() { throw null; }
            void System.Collections.Generic.ICollection<TValue>.Add(TValue item) { }
            void System.Collections.Generic.ICollection<TValue>.Clear() { }
            bool System.Collections.Generic.ICollection<TValue>.Contains(TValue item) { throw null; }
            bool System.Collections.Generic.ICollection<TValue>.Remove(TValue item) { throw null; }
            void System.Collections.ICollection.CopyTo(System.Array array, int index) { }
            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        }
    }
}
namespace System.Configuration.Assemblies
{
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public enum AssemblyHashAlgorithm
    {
        MD5 = 32771,
        None = 0,
        SHA1 = 32772,
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        SHA256 = 32780,
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        SHA384 = 32781,
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        SHA512 = 32782,
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public enum AssemblyVersionCompatibility
    {
        SameMachine         = 1,
        SameProcess         = 2,
        SameDomain          = 3,
    }
}
namespace System.Diagnostics
{
    [System.AttributeUsageAttribute((System.AttributeTargets)(68), AllowMultiple=true)]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class ConditionalAttribute : System.Attribute
    {
        public ConditionalAttribute(string conditionString) { }
        public string ConditionString { get { throw null; } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(3), AllowMultiple=false)]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class DebuggableAttribute : System.Attribute
    {
        public DebuggableAttribute(bool isJITTrackingEnabled, bool isJITOptimizerDisabled) { }
        public DebuggableAttribute(System.Diagnostics.DebuggableAttribute.DebuggingModes modes) { }
        public System.Diagnostics.DebuggableAttribute.DebuggingModes DebuggingFlags { get { throw null; } }
        public bool IsJITOptimizerDisabled { get { throw null; } }
        public bool IsJITTrackingEnabled { get { throw null; } }
        [System.FlagsAttribute]
        [System.Runtime.InteropServices.ComVisibleAttribute(true)]
        public enum DebuggingModes
        {
            Default = 1,
            DisableOptimizations = 256,
            EnableEditAndContinue = 4,
            IgnoreSymbolStoreSequencePoints = 2,
            None = 0,
        }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class Debugger
    {
        [System.ObsoleteAttribute("Do not create instances of the Debugger class.  Call the static methods directly on this type instead", true)]
        public Debugger() { }
        public static bool IsAttached { [System.Security.SecuritySafeCriticalAttribute][System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)]get { throw null; } }
        [System.Security.SecuritySafeCriticalAttribute]
        public static void Break() { }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)][System.Security.SecuritySafeCriticalAttribute]
        public static bool IsLogging() { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public static bool Launch() { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)][System.Security.SecuritySafeCriticalAttribute]
        public static void Log(int level, string category, string message) { }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public static void NotifyOfCrossThreadDependency() { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(384), AllowMultiple=false)]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class DebuggerBrowsableAttribute : System.Attribute
    {
        public DebuggerBrowsableAttribute(System.Diagnostics.DebuggerBrowsableState state) { }
        public System.Diagnostics.DebuggerBrowsableState State { get { throw null; } }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public enum DebuggerBrowsableState
    {
        Collapsed = 2,
        Never = 0,
        RootHidden = 3,
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(4509), AllowMultiple=true)]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class DebuggerDisplayAttribute : System.Attribute
    {
        public DebuggerDisplayAttribute(string value) { }
        public string Name { get { throw null; } set { } }
        public System.Type Target { get { throw null; } set { } }
        public string TargetTypeName { get { throw null; } set { } }
        public string Type { get { throw null; } set { } }
        public string Value { get { throw null; } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(224), Inherited=false)]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class DebuggerHiddenAttribute : System.Attribute
    {
        public DebuggerHiddenAttribute() { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(236), Inherited=false)]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class DebuggerNonUserCodeAttribute : System.Attribute
    {
        public DebuggerNonUserCodeAttribute() { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(108), Inherited=false)]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class DebuggerStepThroughAttribute : System.Attribute
    {
        public DebuggerStepThroughAttribute() { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(13), AllowMultiple=true)]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class DebuggerTypeProxyAttribute : System.Attribute
    {
        public DebuggerTypeProxyAttribute(string typeName) { }
        public DebuggerTypeProxyAttribute(System.Type type) { }
        public string ProxyTypeName { get { throw null; } }
        public System.Type Target { get { throw null; } set { } }
        public string TargetTypeName { get { throw null; } set { } }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class StackFrame
    {
        public const int OFFSET_UNKNOWN = -1;
        [System.Security.SecuritySafeCriticalAttribute]
        public StackFrame() { }
        [System.Security.SecurityCriticalAttribute]
        public StackFrame(bool fNeedFileInfo) { }
        public StackFrame(int skipFrames) { }
        [System.Security.SecurityCriticalAttribute]
        public StackFrame(int skipFrames, bool fNeedFileInfo) { }
        public StackFrame(string fileName, int lineNumber) { }
        public StackFrame(string fileName, int lineNumber, int colNumber) { }
        public virtual int GetFileColumnNumber() { throw null; }
        public virtual int GetFileLineNumber() { throw null; }
        [System.Security.SecurityCriticalAttribute]
        public virtual string GetFileName() { throw null; }
        public virtual int GetILOffset() { throw null; }
        public virtual System.Reflection.MethodBase GetMethod() { throw null; }
        public virtual int GetNativeOffset() { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public override string ToString() { throw null; }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class StackTrace
    {
        public const int METHODS_TO_SKIP = 0;
        [System.Security.SecuritySafeCriticalAttribute]
        public StackTrace() { }
        [System.Security.SecurityCriticalAttribute]
        public StackTrace(bool fNeedFileInfo) { }
        public StackTrace(System.Diagnostics.StackFrame frame) { }
        public StackTrace(System.Exception e) { }
        [System.Security.SecurityCriticalAttribute]
        public StackTrace(System.Exception e, bool fNeedFileInfo) { }
        [System.Security.SecurityCriticalAttribute]
        public StackTrace(System.Exception e, int skipFrames) { }
        [System.Security.SecurityCriticalAttribute]
        public StackTrace(System.Exception e, int skipFrames, bool fNeedFileInfo) { }
        [System.Security.SecurityCriticalAttribute]
        public StackTrace(int skipFrames) { }
        [System.Security.SecurityCriticalAttribute]
        public StackTrace(int skipFrames, bool fNeedFileInfo) { }
        [System.ObsoleteAttribute("This constructor has been deprecated.  Please use a constructor that does not require a Thread parameter.  http://go.microsoft.com/fwlink/?linkid=14202")]
        [System.Security.SecurityCriticalAttribute]
        public StackTrace(System.Threading.Thread targetThread, bool needFileInfo) { }
        public virtual int FrameCount { get { throw null; } }
        public virtual System.Diagnostics.StackFrame GetFrame(int index) { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public virtual System.Diagnostics.StackFrame[] GetFrames() { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public override string ToString() { throw null; }
    }
}
namespace System.Diagnostics.CodeAnalysis
{
    [System.AttributeUsageAttribute((System.AttributeTargets)(32767), Inherited=false, AllowMultiple=true)]
    [System.Diagnostics.ConditionalAttribute("CODE_ANALYSIS")]
    public sealed partial class SuppressMessageAttribute : System.Attribute
    {
        public SuppressMessageAttribute(string category, string checkId) { }
        public string Category { get { throw null; } }
        public string CheckId { get { throw null; } }
        public string Justification { get { throw null; } set { } }
        public string MessageId { get { throw null; } set { } }
        public string Scope { get { throw null; } set { } }
        public string Target { get { throw null; } set { } }
    }
}
namespace System.Diagnostics.Contracts
{
    public static partial class Contract
    {
        public static event System.EventHandler<System.Diagnostics.Contracts.ContractFailedEventArgs> ContractFailed { add { } remove { } }
        [System.Diagnostics.ConditionalAttribute("CONTRACTS_FULL")]
        [System.Diagnostics.ConditionalAttribute("DEBUG")]
        public static void Assert(bool condition) { }
        [System.Diagnostics.ConditionalAttribute("CONTRACTS_FULL")]
        [System.Diagnostics.ConditionalAttribute("DEBUG")]
        public static void Assert(bool condition, string userMessage) { }
        [System.Diagnostics.ConditionalAttribute("CONTRACTS_FULL")]
        [System.Diagnostics.ConditionalAttribute("DEBUG")]
        public static void Assume(bool condition) { }
        [System.Diagnostics.ConditionalAttribute("CONTRACTS_FULL")]
        [System.Diagnostics.ConditionalAttribute("DEBUG")]
        public static void Assume(bool condition, string userMessage) { }
        [System.Diagnostics.ConditionalAttribute("CONTRACTS_FULL")]
        public static void EndContractBlock() { }
        [System.Diagnostics.ConditionalAttribute("CONTRACTS_FULL")]
        public static void Ensures(bool condition) { }
        [System.Diagnostics.ConditionalAttribute("CONTRACTS_FULL")]
        public static void Ensures(bool condition, string userMessage) { }
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter", Justification="Exception type used in tools.")]
        [System.Diagnostics.ConditionalAttribute("CONTRACTS_FULL")]
        public static void EnsuresOnThrow<TException>(bool condition) where TException : System.Exception { }
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter", Justification="Exception type used in tools.")]
        [System.Diagnostics.ConditionalAttribute("CONTRACTS_FULL")]
        public static void EnsuresOnThrow<TException>(bool condition, string userMessage) where TException : System.Exception { }
        public static bool Exists(int fromInclusive, int toExclusive, System.Predicate<int> predicate) { throw null; }
        public static bool Exists<T>(System.Collections.Generic.IEnumerable<T> collection, System.Predicate<T> predicate) { throw null; }
        public static bool ForAll(int fromInclusive, int toExclusive, System.Predicate<int> predicate) { throw null; }
        public static bool ForAll<T>(System.Collections.Generic.IEnumerable<T> collection, System.Predicate<T> predicate) { throw null; }
        [System.Diagnostics.ConditionalAttribute("CONTRACTS_FULL")]
        public static void Invariant(bool condition) { }
        [System.Diagnostics.ConditionalAttribute("CONTRACTS_FULL")]
        public static void Invariant(bool condition, string userMessage) { }
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId="value")]
        public static T OldValue<T>(T value) { throw null; }
        [System.Diagnostics.ConditionalAttribute("CONTRACTS_FULL")]
        public static void Requires(bool condition) { }
        [System.Diagnostics.ConditionalAttribute("CONTRACTS_FULL")]
        public static void Requires(bool condition, string userMessage) { }
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId="condition")]
        public static void Requires<TException>(bool condition) where TException : System.Exception { }
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId="condition")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId="userMessage")]
        public static void Requires<TException>(bool condition, string userMessage) where TException : System.Exception { }
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter", Justification="Not intended to be called at runtime.")]
        public static T Result<T>() { throw null; }
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId="0#", Justification="Not intended to be called at runtime.")]
        public static T ValueAtReturn<T>(out T value) { value = default(T); throw null; }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(64), AllowMultiple=false)]
    [System.Diagnostics.ConditionalAttribute("CONTRACTS_FULL")]
    public sealed partial class ContractAbbreviatorAttribute : System.Attribute
    {
        public ContractAbbreviatorAttribute() { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(64), AllowMultiple=false)]
    [System.Diagnostics.ConditionalAttribute("CONTRACTS_FULL")]
    public sealed partial class ContractArgumentValidatorAttribute : System.Attribute
    {
        public ContractArgumentValidatorAttribute() { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(5124), AllowMultiple=false, Inherited=false)]
    [System.Diagnostics.ConditionalAttribute("CONTRACTS_FULL")]
    [System.Diagnostics.ConditionalAttribute("DEBUG")]
    public sealed partial class ContractClassAttribute : System.Attribute
    {
        public ContractClassAttribute(System.Type typeContainingContracts) { }
        public System.Type TypeContainingContracts { get { throw null; } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(4), AllowMultiple=false, Inherited=false)]
    [System.Diagnostics.ConditionalAttribute("CONTRACTS_FULL")]
    public sealed partial class ContractClassForAttribute : System.Attribute
    {
        public ContractClassForAttribute(System.Type typeContractsAreFor) { }
        public System.Type TypeContractsAreFor { get { throw null; } }
    }
    public sealed partial class ContractFailedEventArgs : System.EventArgs
    {
        public ContractFailedEventArgs(System.Diagnostics.Contracts.ContractFailureKind failureKind, string message, string condition, System.Exception originalException) { }
        public string Condition { get { throw null; } }
        public System.Diagnostics.Contracts.ContractFailureKind FailureKind { get { throw null; } }
        public bool Handled { get { throw null; } }
        public string Message { get { throw null; } }
        public System.Exception OriginalException { get { throw null; } }
        public bool Unwind { get { throw null; } }
        [System.Security.SecurityCriticalAttribute]
        public void SetHandled() { }
        [System.Security.SecurityCriticalAttribute]
        public void SetUnwind() { }
    }
    public enum ContractFailureKind
    {
        Assert = 4,
        Assume = 5,
        Invariant = 3,
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId="Postcondition")]
        Postcondition = 1,
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId="Postcondition")]
        PostconditionOnException = 2,
        Precondition = 0,
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(64), AllowMultiple=false, Inherited=false)]
    [System.Diagnostics.ConditionalAttribute("CONTRACTS_FULL")]
    public sealed partial class ContractInvariantMethodAttribute : System.Attribute
    {
        public ContractInvariantMethodAttribute() { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(32767), AllowMultiple=true, Inherited=false)]
    [System.Diagnostics.ConditionalAttribute("CONTRACTS_FULL")]
    public sealed partial class ContractOptionAttribute : System.Attribute
    {
        public ContractOptionAttribute(string category, string setting, bool enabled) { }
        public ContractOptionAttribute(string category, string setting, string value) { }
        public string Category { get { throw null; } }
        public bool Enabled { get { throw null; } }
        public string Setting { get { throw null; } }
        public string Value { get { throw null; } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(256))]
    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Design", "CA1019:DefineAccessorsForAttributeArguments", Justification="Thank you very much, but we like the names we've defined for the accessors")]
    [System.Diagnostics.ConditionalAttribute("CONTRACTS_FULL")]
    public sealed partial class ContractPublicPropertyNameAttribute : System.Attribute
    {
        public ContractPublicPropertyNameAttribute(string name) { }
        public string Name { get { throw null; } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(1))]
    public sealed partial class ContractReferenceAssemblyAttribute : System.Attribute
    {
        public ContractReferenceAssemblyAttribute() { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(192), AllowMultiple=false, Inherited=true)]
    [System.Diagnostics.ConditionalAttribute("CONTRACTS_FULL")]
    public sealed partial class ContractRuntimeIgnoredAttribute : System.Attribute
    {
        public ContractRuntimeIgnoredAttribute() { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(237))]
    [System.Diagnostics.ConditionalAttribute("CONTRACTS_FULL")]
    public sealed partial class ContractVerificationAttribute : System.Attribute
    {
        public ContractVerificationAttribute(bool value) { }
        public bool Value { get { throw null; } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(6884), AllowMultiple=false, Inherited=true)]
    [System.Diagnostics.ConditionalAttribute("CONTRACTS_FULL")]
    public sealed partial class PureAttribute : System.Attribute
    {
        public PureAttribute() { }
    }
}
namespace System.Diagnostics.Contracts.Internal
{
    [System.ObsoleteAttribute("Use the ContractHelper class in the System.Runtime.CompilerServices namespace instead.")]
    public static partial class ContractHelper
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Design", "CA1030:UseEventsWhereAppropriate")]
        [System.Diagnostics.DebuggerNonUserCodeAttribute]
        public static string RaiseContractFailedEvent(System.Diagnostics.Contracts.ContractFailureKind failureKind, string userMessage, string conditionText, System.Exception innerException) { throw null; }
        [System.Diagnostics.DebuggerNonUserCodeAttribute]
        public static void TriggerFailure(System.Diagnostics.Contracts.ContractFailureKind kind, string displayMessage, string userMessage, string conditionText, System.Exception innerException) { }
    }
}
namespace System.Diagnostics.SymbolStore
{
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial interface ISymbolDocumentWriter
    {
        void SetCheckSum(System.Guid algorithmId, byte[] checkSum);
        void SetSource(byte[] source);
    }
}
namespace System.Diagnostics.Tracing
{
    [System.FlagsAttribute]
    public enum EventActivityOptions
    {
        Detachable = 8,
        Disable = 2,
        None = 0,
        Recursive = 4,
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(64))]
    public sealed partial class EventAttribute : System.Attribute
    {
        public EventAttribute(int eventId) { }
        public System.Diagnostics.Tracing.EventActivityOptions ActivityOptions { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Diagnostics.Tracing.EventChannel Channel { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public int EventId { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public System.Diagnostics.Tracing.EventKeywords Keywords { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Diagnostics.Tracing.EventLevel Level { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string Message { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Diagnostics.Tracing.EventOpcode Opcode { get { throw null; } set { } }
        public System.Diagnostics.Tracing.EventTags Tags { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Diagnostics.Tracing.EventTask Task { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public byte Version { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Design", "CA1028:EnumStorageShouldBeInt32", Justification="Backwards compatibility")]
    public enum EventChannel : byte
    {
        Admin = (byte)16,
        Analytic = (byte)18,
        Debug = (byte)19,
        None = (byte)0,
        Operational = (byte)17,
    }
    public enum EventCommand
    {
        Disable = -3,
        Enable = -2,
        SendManifest = -1,
        Update = 0,
    }
    public partial class EventCommandEventArgs : System.EventArgs
    {
        internal EventCommandEventArgs() { }
        public System.Collections.Generic.IDictionary<string, string> Arguments { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public System.Diagnostics.Tracing.EventCommand Command { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public bool DisableEvent(int eventId) { throw null; }
        public bool EnableEvent(int eventId) { throw null; }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(12), Inherited=false)]
    public partial class EventDataAttribute : System.Attribute
    {
        public EventDataAttribute() { }
        public string Name { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(128))]
    public partial class EventFieldAttribute : System.Attribute
    {
        public EventFieldAttribute() { }
        public System.Diagnostics.Tracing.EventFieldFormat Format { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Diagnostics.Tracing.EventFieldTags Tags { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    public enum EventFieldFormat
    {
        Boolean = 3,
        Default = 0,
        Hexadecimal = 4,
        HResult = 15,
        Json = 12,
        String = 2,
        Xml = 11,
    }
    [System.FlagsAttribute]
    public enum EventFieldTags
    {
        None = 0,
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(128))]
    public partial class EventIgnoreAttribute : System.Attribute
    {
        public EventIgnoreAttribute() { }
    }
    [System.FlagsAttribute]
    public enum EventKeywords : long
    {
        All = (long)-1,
        AuditFailure = (long)4503599627370496,
        AuditSuccess = (long)9007199254740992,
        CorrelationHint = (long)4503599627370496,
        EventLogClassic = (long)36028797018963968,
        MicrosoftTelemetry = (long)562949953421312,
        None = (long)0,
        Sqm = (long)2251799813685248,
        WdiContext = (long)562949953421312,
        WdiDiagnostic = (long)1125899906842624,
    }
    public enum EventLevel
    {
        Critical = 1,
        Error = 2,
        Informational = 4,
        LogAlways = 0,
        Verbose = 5,
        Warning = 3,
    }
    public partial class EventListener : System.IDisposable
    {
        public EventListener() { }
        public void DisableEvents(System.Diagnostics.Tracing.EventSource eventSource) { }
        public virtual void Dispose() { }
        public void EnableEvents(System.Diagnostics.Tracing.EventSource eventSource, System.Diagnostics.Tracing.EventLevel level) { }
        public void EnableEvents(System.Diagnostics.Tracing.EventSource eventSource, System.Diagnostics.Tracing.EventLevel level, System.Diagnostics.Tracing.EventKeywords matchAnyKeyword) { }
        public void EnableEvents(System.Diagnostics.Tracing.EventSource eventSource, System.Diagnostics.Tracing.EventLevel level, System.Diagnostics.Tracing.EventKeywords matchAnyKeyword, System.Collections.Generic.IDictionary<string, string> arguments) { }
        public static int EventSourceIndex(System.Diagnostics.Tracing.EventSource eventSource) { throw null; }
        protected internal virtual void OnEventSourceCreated(System.Diagnostics.Tracing.EventSource eventSource) { }
        protected internal virtual void OnEventWritten(System.Diagnostics.Tracing.EventWrittenEventArgs eventData) { }
    }
    [System.FlagsAttribute]
    public enum EventManifestOptions
    {
        AllCultures = 2,
        AllowEventSourceOverride = 8,
        None = 0,
        OnlyIfNeededForRegistration = 4,
        Strict = 1,
    }
    public enum EventOpcode
    {
        DataCollectionStart = 3,
        DataCollectionStop = 4,
        Extension = 5,
        Info = 0,
        Receive = 240,
        Reply = 6,
        Resume = 7,
        Send = 9,
        Start = 1,
        Stop = 2,
        Suspend = 8,
    }
    public partial class EventSource : System.IDisposable
    {
        protected EventSource() { }
        protected EventSource(bool throwOnEventWriteErrors) { }
        protected EventSource(System.Diagnostics.Tracing.EventSourceSettings settings) { }
        protected EventSource(System.Diagnostics.Tracing.EventSourceSettings settings, params string[] traits) { }
        public EventSource(string eventSourceName) { }
        public EventSource(string eventSourceName, System.Diagnostics.Tracing.EventSourceSettings config) { }
        public EventSource(string eventSourceName, System.Diagnostics.Tracing.EventSourceSettings config, params string[] traits) { }
        public System.Exception ConstructionException { get { throw null; } }
        public static System.Guid CurrentThreadActivityId { [System.Security.SecuritySafeCriticalAttribute]get { throw null; } }
        public System.Guid Guid { get { throw null; } }
        public string Name { get { throw null; } }
        public System.Diagnostics.Tracing.EventSourceSettings Settings { get { throw null; } }
        public event System.EventHandler<System.Diagnostics.Tracing.EventCommandEventArgs> EventCommandExecuted { add { } remove { } }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        ~EventSource() { }
        public static string GenerateManifest(System.Type eventSourceType, string assemblyPathToIncludeInManifest) { throw null; }
        public static string GenerateManifest(System.Type eventSourceType, string assemblyPathToIncludeInManifest, System.Diagnostics.Tracing.EventManifestOptions flags) { throw null; }
        public static System.Guid GetGuid(System.Type eventSourceType) { throw null; }
        public static string GetName(System.Type eventSourceType) { throw null; }
        public static System.Collections.Generic.IEnumerable<System.Diagnostics.Tracing.EventSource> GetSources() { throw null; }
        public string GetTrait(string key) { throw null; }
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Concurrency", "CA8001", Justification="This does not need to be correct when racing with other threads")]
        public bool IsEnabled() { throw null; }
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Concurrency", "CA8001", Justification="This does not need to be correct when racing with other threads")]
        public bool IsEnabled(System.Diagnostics.Tracing.EventLevel level, System.Diagnostics.Tracing.EventKeywords keywords) { throw null; }
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Concurrency", "CA8001", Justification="This does not need to be correct when racing with other threads")]
        public bool IsEnabled(System.Diagnostics.Tracing.EventLevel level, System.Diagnostics.Tracing.EventKeywords keywords, System.Diagnostics.Tracing.EventChannel channel) { throw null; }
        protected virtual void OnEventCommand(System.Diagnostics.Tracing.EventCommandEventArgs command) { }
        public static void SendCommand(System.Diagnostics.Tracing.EventSource eventSource, System.Diagnostics.Tracing.EventCommand command, System.Collections.Generic.IDictionary<string, string> commandArguments) { }
        [System.Security.SecuritySafeCriticalAttribute]
        public static void SetCurrentThreadActivityId(System.Guid activityId) { }
        [System.Security.SecuritySafeCriticalAttribute]
        public static void SetCurrentThreadActivityId(System.Guid activityId, out System.Guid oldActivityThatWillContinue) { oldActivityThatWillContinue = default(System.Guid); }
        public override string ToString() { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public void Write(string eventName) { }
        [System.Security.SecuritySafeCriticalAttribute]
        public void Write(string eventName, System.Diagnostics.Tracing.EventSourceOptions options) { }
        [System.Security.SecuritySafeCriticalAttribute]
        public void Write<T>(string eventName, T data) { }
        [System.Security.SecuritySafeCriticalAttribute]
        public void Write<T>(string eventName, System.Diagnostics.Tracing.EventSourceOptions options, T data) { }
        [System.Security.SecuritySafeCriticalAttribute]
        public void Write<T>(string eventName, ref System.Diagnostics.Tracing.EventSourceOptions options, ref T data) { }
        [System.Security.SecuritySafeCriticalAttribute]
        public void Write<T>(string eventName, ref System.Diagnostics.Tracing.EventSourceOptions options, ref System.Guid activityId, ref System.Guid relatedActivityId, ref T data) { }
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Concurrency", "CA8001", Justification="This does not need to be correct when racing with other threads")]
        [System.Security.SecuritySafeCriticalAttribute]
        protected void WriteEvent(int eventId) { }
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Concurrency", "CA8001", Justification="This does not need to be correct when racing with other threads")]
        [System.Security.SecuritySafeCriticalAttribute]
        protected void WriteEvent(int eventId, byte[] arg1) { }
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Concurrency", "CA8001", Justification="This does not need to be correct when racing with other threads")]
        [System.Security.SecuritySafeCriticalAttribute]
        protected void WriteEvent(int eventId, int arg1) { }
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Concurrency", "CA8001", Justification="This does not need to be correct when racing with other threads")]
        [System.Security.SecuritySafeCriticalAttribute]
        protected void WriteEvent(int eventId, int arg1, int arg2) { }
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Concurrency", "CA8001", Justification="This does not need to be correct when racing with other threads")]
        [System.Security.SecuritySafeCriticalAttribute]
        protected void WriteEvent(int eventId, int arg1, int arg2, int arg3) { }
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Concurrency", "CA8001", Justification="This does not need to be correct when racing with other threads")]
        [System.Security.SecuritySafeCriticalAttribute]
        protected void WriteEvent(int eventId, int arg1, string arg2) { }
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Concurrency", "CA8001", Justification="This does not need to be correct when racing with other threads")]
        [System.Security.SecuritySafeCriticalAttribute]
        protected void WriteEvent(int eventId, long arg1) { }
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Concurrency", "CA8001", Justification="This does not need to be correct when racing with other threads")]
        [System.Security.SecuritySafeCriticalAttribute]
        protected void WriteEvent(int eventId, long arg1, byte[] arg2) { }
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Concurrency", "CA8001", Justification="This does not need to be correct when racing with other threads")]
        [System.Security.SecuritySafeCriticalAttribute]
        protected void WriteEvent(int eventId, long arg1, long arg2) { }
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Concurrency", "CA8001", Justification="This does not need to be correct when racing with other threads")]
        [System.Security.SecuritySafeCriticalAttribute]
        protected void WriteEvent(int eventId, long arg1, long arg2, long arg3) { }
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Concurrency", "CA8001", Justification="This does not need to be correct when racing with other threads")]
        [System.Security.SecuritySafeCriticalAttribute]
        protected void WriteEvent(int eventId, long arg1, string arg2) { }
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Concurrency", "CA8001", Justification="This does not need to be correct when racing with other threads")]
        [System.Security.SecuritySafeCriticalAttribute]
        protected void WriteEvent(int eventId, params object[] args) { }
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Concurrency", "CA8001", Justification="This does not need to be correct when racing with other threads")]
        [System.Security.SecuritySafeCriticalAttribute]
        protected void WriteEvent(int eventId, string arg1) { }
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Concurrency", "CA8001", Justification="This does not need to be correct when racing with other threads")]
        [System.Security.SecuritySafeCriticalAttribute]
        protected void WriteEvent(int eventId, string arg1, int arg2) { }
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Concurrency", "CA8001", Justification="This does not need to be correct when racing with other threads")]
        [System.Security.SecuritySafeCriticalAttribute]
        protected void WriteEvent(int eventId, string arg1, int arg2, int arg3) { }
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Concurrency", "CA8001", Justification="This does not need to be correct when racing with other threads")]
        [System.Security.SecuritySafeCriticalAttribute]
        protected void WriteEvent(int eventId, string arg1, long arg2) { }
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Concurrency", "CA8001", Justification="This does not need to be correct when racing with other threads")]
        [System.Security.SecuritySafeCriticalAttribute]
        protected void WriteEvent(int eventId, string arg1, string arg2) { }
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Concurrency", "CA8001", Justification="This does not need to be correct when racing with other threads")]
        [System.Security.SecuritySafeCriticalAttribute]
        protected void WriteEvent(int eventId, string arg1, string arg2, string arg3) { }
        [System.CLSCompliantAttribute(false)]
        [System.Security.SecurityCriticalAttribute]
        protected unsafe void WriteEventCore(int eventId, int eventDataCount, System.Diagnostics.Tracing.EventSource.EventData* data) { }
        [System.Security.SecuritySafeCriticalAttribute]
        protected void WriteEventWithRelatedActivityId(int eventId, System.Guid relatedActivityId, params object[] args) { }
        [System.CLSCompliantAttribute(false)]
        [System.Security.SecurityCriticalAttribute]
        protected unsafe void WriteEventWithRelatedActivityIdCore(int eventId, System.Guid* relatedActivityId, int eventDataCount, System.Diagnostics.Tracing.EventSource.EventData* data) { }
        [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
        protected internal partial struct EventData
        {
            public System.IntPtr DataPointer { get { throw null; } set { } }
            public int Size { get { throw null; } set { } }
        }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(4))]
    public sealed partial class EventSourceAttribute : System.Attribute
    {
        public EventSourceAttribute() { }
        public string Guid { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string LocalizationResources { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string Name { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    public partial class EventSourceException : System.Exception
    {
        public EventSourceException() { }
        public EventSourceException(string message) { }
        public EventSourceException(string message, System.Exception innerException) { }
        protected EventSourceException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct EventSourceOptions
    {
        public System.Diagnostics.Tracing.EventActivityOptions ActivityOptions { get { throw null; } set { } }
        public System.Diagnostics.Tracing.EventKeywords Keywords { get { throw null; } set { } }
        public System.Diagnostics.Tracing.EventLevel Level { get { throw null; } set { } }
        public System.Diagnostics.Tracing.EventOpcode Opcode { get { throw null; } set { } }
        public System.Diagnostics.Tracing.EventTags Tags { get { throw null; } set { } }
    }
    [System.FlagsAttribute]
    public enum EventSourceSettings
    {
        Default = 0,
        EtwManifestEventFormat = 4,
        EtwSelfDescribingEventFormat = 8,
        ThrowOnEventWriteErrors = 1,
    }
    [System.FlagsAttribute]
    public enum EventTags
    {
        None = 0,
    }
    public enum EventTask
    {
        None = 0,
    }
    public partial class EventWrittenEventArgs : System.EventArgs
    {
        internal EventWrittenEventArgs() { }
        public System.Guid ActivityId { [System.Security.SecurityCriticalAttribute]get { throw null; } }
        public System.Diagnostics.Tracing.EventChannel Channel { get { throw null; } }
        public int EventId { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public string EventName { get { throw null; } }
        public System.Diagnostics.Tracing.EventSource EventSource { get { throw null; } }
        public System.Diagnostics.Tracing.EventKeywords Keywords { get { throw null; } }
        public System.Diagnostics.Tracing.EventLevel Level { get { throw null; } }
        public string Message { get { throw null; } }
        public System.Diagnostics.Tracing.EventOpcode Opcode { get { throw null; } }
        public System.Collections.ObjectModel.ReadOnlyCollection<object> Payload { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public System.Collections.ObjectModel.ReadOnlyCollection<string> PayloadNames { get { throw null; } }
        public System.Guid RelatedActivityId { [System.Runtime.CompilerServices.CompilerGeneratedAttribute, System.Security.SecurityCriticalAttribute]get { throw null; } }
        public System.Diagnostics.Tracing.EventTags Tags { get { throw null; } }
        public System.Diagnostics.Tracing.EventTask Task { get { throw null; } }
        public byte Version { get { throw null; } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(64))]
    public sealed partial class NonEventAttribute : System.Attribute
    {
        public NonEventAttribute() { }
    }
}
namespace System.Globalization
{
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public abstract partial class Calendar : System.ICloneable
    {
        public const int CurrentEra = 0;
        protected Calendar() { }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public virtual System.Globalization.CalendarAlgorithmType AlgorithmType { get { throw null; } }
        protected virtual int DaysInYearBeforeMinSupportedYear { get { throw null; } }
        public abstract int[] Eras { get; }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public bool IsReadOnly { get { throw null; } }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public virtual System.DateTime MaxSupportedDateTime { get { throw null; } }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
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
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
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
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public virtual int GetLeapMonth(int year) { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
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
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public static System.Globalization.Calendar ReadOnly(System.Globalization.Calendar calendar) { throw null; }
        public virtual System.DateTime ToDateTime(int year, int month, int day, int hour, int minute, int second, int millisecond) { throw null; }
        public abstract System.DateTime ToDateTime(int year, int month, int day, int hour, int minute, int second, int millisecond, int era);
        public virtual int ToFourDigitYear(int year) { throw null; }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public enum CalendarAlgorithmType
    {
        LunarCalendar = 2,
        LunisolarCalendar = 3,
        SolarCalendar = 1,
        Unknown = 0,
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
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
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public override int[] Eras { get { throw null; } }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public override System.DateTime MaxSupportedDateTime { get { throw null; } }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public override System.DateTime MinSupportedDateTime { get { throw null; } }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public override int GetEra(System.DateTime time) { throw null; }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class CompareInfo : System.Runtime.Serialization.IDeserializationCallback
    {
        public int LCID { get { throw null; } }
        internal CompareInfo() { }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public virtual string Name { get { throw null; } }
        public virtual int Compare(string string1, int offset1, int length1, string string2, int offset2, int length2) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public virtual int Compare(string string1, int offset1, int length1, string string2, int offset2, int length2, System.Globalization.CompareOptions options) { throw null; }
        public virtual int Compare(string string1, int offset1, string string2, int offset2) { throw null; }
        public virtual int Compare(string string1, int offset1, string string2, int offset2, System.Globalization.CompareOptions options) { throw null; }
        public virtual int Compare(string string1, string string2) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
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
        [System.Security.SecuritySafeCriticalAttribute]
        public virtual int IndexOf(string source, char value, int startIndex, int count, System.Globalization.CompareOptions options) { throw null; }
        public virtual int IndexOf(string source, string value) { throw null; }
        public virtual int IndexOf(string source, string value, System.Globalization.CompareOptions options) { throw null; }
        public virtual int IndexOf(string source, string value, int startIndex) { throw null; }
        public virtual int IndexOf(string source, string value, int startIndex, System.Globalization.CompareOptions options) { throw null; }
        public virtual int IndexOf(string source, string value, int startIndex, int count) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public virtual int IndexOf(string source, string value, int startIndex, int count, System.Globalization.CompareOptions options) { throw null; }
        public virtual bool IsPrefix(string source, string prefix) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public virtual bool IsPrefix(string source, string prefix, System.Globalization.CompareOptions options) { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public static bool IsSortable(char ch) { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        [System.Security.SecuritySafeCriticalAttribute]
        public static bool IsSortable(string text) { throw null; }
        public virtual bool IsSuffix(string source, string suffix) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public virtual bool IsSuffix(string source, string suffix, System.Globalization.CompareOptions options) { throw null; }
        public virtual int LastIndexOf(string source, char value) { throw null; }
        public virtual int LastIndexOf(string source, char value, System.Globalization.CompareOptions options) { throw null; }
        public virtual int LastIndexOf(string source, char value, int startIndex) { throw null; }
        public virtual int LastIndexOf(string source, char value, int startIndex, System.Globalization.CompareOptions options) { throw null; }
        public virtual int LastIndexOf(string source, char value, int startIndex, int count) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public virtual int LastIndexOf(string source, char value, int startIndex, int count, System.Globalization.CompareOptions options) { throw null; }
        public virtual int LastIndexOf(string source, string value) { throw null; }
        public virtual int LastIndexOf(string source, string value, System.Globalization.CompareOptions options) { throw null; }
        public virtual int LastIndexOf(string source, string value, int startIndex) { throw null; }
        public virtual int LastIndexOf(string source, string value, int startIndex, System.Globalization.CompareOptions options) { throw null; }
        public virtual int LastIndexOf(string source, string value, int startIndex, int count) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public virtual int LastIndexOf(string source, string value, int startIndex, int count, System.Globalization.CompareOptions options) { throw null; }
        void System.Runtime.Serialization.IDeserializationCallback.OnDeserialization(object sender) { }
        public override string ToString() { throw null; }
    }
    [System.FlagsAttribute]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
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
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class CultureInfo : System.ICloneable, System.IFormatProvider
    {
        public CultureInfo(int culture) { throw null; }
        public CultureInfo(int culture, bool useUserOverride) { throw null; }
        public CultureInfo(string name) { }
        public CultureInfo(string name, bool useUserOverride) { throw null; }
        public virtual System.Globalization.Calendar Calendar { get { throw null; } }
        public virtual System.Globalization.CompareInfo CompareInfo { get { throw null; } }
        public static System.Globalization.CultureInfo CurrentCulture { get { throw null; } set { } }
        public static System.Globalization.CultureInfo CurrentUICulture { get { throw null; } set { } }
        public virtual System.Globalization.DateTimeFormatInfo DateTimeFormat { get { throw null; } set { } }
        public static System.Globalization.CultureInfo DefaultThreadCurrentCulture { get { throw null; } [System.Security.SecuritySafeCriticalAttribute]set { } }
        public static System.Globalization.CultureInfo DefaultThreadCurrentUICulture { get { throw null; } [System.Security.SecuritySafeCriticalAttribute]set { } }
        public virtual string DisplayName { [System.Security.SecuritySafeCriticalAttribute]get { throw null; } }
        public virtual string EnglishName { [System.Security.SecuritySafeCriticalAttribute]get { throw null; } }
        public static System.Globalization.CultureInfo InstalledUICulture { get { throw null; } }
        public static System.Globalization.CultureInfo InvariantCulture { get { throw null; } }
        public virtual bool IsNeutralCulture { get { throw null; } }
        public bool IsReadOnly { get { throw null; } }
        public virtual int LCID { get { throw null; } }
        public virtual string Name { get { throw null; } }
        public virtual string NativeName { [System.Security.SecuritySafeCriticalAttribute]get { throw null; } }
        public virtual System.Globalization.NumberFormatInfo NumberFormat { get { throw null; } set { } }
        public virtual System.Globalization.Calendar[] OptionalCalendars { get { throw null; } }
        public virtual System.Globalization.CultureInfo Parent { [System.Security.SecuritySafeCriticalAttribute]get { throw null; } }
        public virtual System.Globalization.TextInfo TextInfo { get { throw null; } }
        public virtual string ThreeLetterISOLanguageName { get { throw null; } }
        public virtual string ThreeLetterWindowsLanguageName { get { throw null; } }
        public virtual string TwoLetterISOLanguageName { [System.Security.SecuritySafeCriticalAttribute]get { throw null; } }
        public bool UseUserOverride { get { throw null; } }
        public void ClearCachedData() { throw null; }
        public virtual object Clone() { throw null; }
        public static System.Globalization.CultureInfo CreateSpecificCulture(string name) { throw null; }
        public override bool Equals(object value) { throw null; }
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
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class CultureNotFoundException : System.ArgumentException, System.Runtime.Serialization.ISerializable
    {
        public CultureNotFoundException() { }
        public CultureNotFoundException(string message) { }
        public CultureNotFoundException(string message, System.Exception innerException) { }
        public CultureNotFoundException(string message, int invalidCultureId, System.Exception innerException) { }
        public CultureNotFoundException(string paramName, int invalidCultureId, string message) { }
        public CultureNotFoundException(string paramName, string message) { }
        public CultureNotFoundException(string message, string invalidCultureName, System.Exception innerException) { }
        public CultureNotFoundException(string paramName, string invalidCultureName, string message) { }
        protected CultureNotFoundException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
        public virtual System.Nullable<int> InvalidCultureId { get { throw null; } }
        public virtual string InvalidCultureName { get { throw null; } }
        public override string Message { get { throw null; } }
        [System.Security.SecurityCriticalAttribute]
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class DateTimeFormatInfo : System.ICloneable, System.IFormatProvider
    {
        public DateTimeFormatInfo() { }
        public string[] AbbreviatedDayNames { get { throw null; } set { } }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public string[] AbbreviatedMonthGenitiveNames { get { throw null; } set { } }
        public string[] AbbreviatedMonthNames { get { throw null; } set { } }
        public string AMDesignator { [System.Security.SecuritySafeCriticalAttribute]get { throw null; } set { } }
        public System.Globalization.Calendar Calendar { get { throw null; } set { } }
        public System.Globalization.CalendarWeekRule CalendarWeekRule { get { throw null; } set { } }
        public static System.Globalization.DateTimeFormatInfo CurrentInfo { get { throw null; } }
        public string DateSeparator { get { throw null; } set { throw null; } }
        public string[] DayNames { get { throw null; } set { } }
        public System.DayOfWeek FirstDayOfWeek { get { throw null; } set { } }
        public string FullDateTimePattern { get { throw null; } set { } }
        public static System.Globalization.DateTimeFormatInfo InvariantInfo { get { throw null; } }
        public bool IsReadOnly { get { throw null; } }
        public string LongDatePattern { get { throw null; } set { } }
        public string LongTimePattern { get { throw null; } set { } }
        public string MonthDayPattern { get { throw null; } set { } }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public string[] MonthGenitiveNames { get { throw null; } set { } }
        public string[] MonthNames { get { throw null; } set { } }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public string NativeCalendarName { get { throw null; } }
        public string PMDesignator { [System.Security.SecuritySafeCriticalAttribute]get { throw null; } set { } }
        public string RFC1123Pattern { get { throw null; } }
        public string ShortDatePattern { get { throw null; } set { } }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public string[] ShortestDayNames { get { throw null; } set { } }
        public string ShortTimePattern { get { throw null; } set { } }
        public string SortableDateTimePattern { get { throw null; } }
        public string TimeSeparator { get { throw null; } set { throw null; } }
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
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public string GetShortestDayName(System.DayOfWeek dayOfWeek) { throw null; }
        public static System.Globalization.DateTimeFormatInfo ReadOnly(System.Globalization.DateTimeFormatInfo dtfi) { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public void SetAllDateTimePatterns(string[] patterns, char format) { throw null; }
    }
    [System.FlagsAttribute]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
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
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class DaylightTime
    {
        public DaylightTime(System.DateTime start, System.DateTime end, System.TimeSpan delta) { throw null; }
        public System.TimeSpan Delta { get { throw null; } }
        public System.DateTime End { get { throw null; } }
        public System.DateTime Start { get { throw null; } }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public enum DigitShapes
    {
        Context = 0,
        NativeNational = 2,
        None = 1,
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public abstract partial class EastAsianLunisolarCalendar : System.Globalization.Calendar
    {
        internal EastAsianLunisolarCalendar() { }
#if FEATURE_COREFX_GLOBALIZATION
        public override System.Globalization.CalendarAlgorithmType AlgorithmType { get { throw null; } }
#endif
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
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class GregorianCalendar : System.Globalization.Calendar
    {
        public const int ADEra = 1;
        public GregorianCalendar() { }
        public GregorianCalendar(System.Globalization.GregorianCalendarTypes type) { }
#if FEATURE_COREFX_GLOBALIZATION
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public override System.Globalization.CalendarAlgorithmType AlgorithmType { get { throw null; } }
#endif
        public virtual System.Globalization.GregorianCalendarTypes CalendarType { get { throw null; } set { } }
        public override int[] Eras { get { throw null; } }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public override System.DateTime MaxSupportedDateTime { get { throw null; } }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
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
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
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
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public enum GregorianCalendarTypes
    {
        Arabic = 10,
        Localized = 1,
        MiddleEastFrench = 9,
        TransliteratedEnglish = 11,
        TransliteratedFrench = 12,
        USEnglish = 2,
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class HebrewCalendar : System.Globalization.Calendar
    {
        public static readonly int HebrewEra;
        public HebrewCalendar() { }
#if FEATURE_COREFX_GLOBALIZATION
        public override System.Globalization.CalendarAlgorithmType AlgorithmType { get { throw null; } }
#endif
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
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class HijriCalendar : System.Globalization.Calendar
    {
        public static readonly int HijriEra;
        public HijriCalendar() { }
#if FEATURE_COREFX_GLOBALIZATION
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public override System.Globalization.CalendarAlgorithmType AlgorithmType { get { throw null; } }
#endif
        protected override int DaysInYearBeforeMinSupportedYear { get { throw null; } }
        public override int[] Eras { get { throw null; } }
        public int HijriAdjustment { [System.Security.SecuritySafeCriticalAttribute]get { throw null; } set { } }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public override System.DateTime MaxSupportedDateTime { get { throw null; } }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
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
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
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
        public bool AllowUnassigned { get { throw null; } set { throw null; } }
        public bool UseStd3AsciiRules { get { throw null; } set { throw null; } }
        public override bool Equals(object obj) { throw null; }
        public string GetAscii(string unicode) { throw null; }
        public string GetAscii(string unicode, int index) { throw null; }
        public string GetAscii(string unicode, int index, int count) { throw null; }
        public override int GetHashCode() { throw null; }
        public string GetUnicode(string ascii) { throw null; }
        public string GetUnicode(string ascii, int index) { throw null; }
        public string GetUnicode(string ascii, int index, int count) { throw null; }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class JapaneseCalendar : System.Globalization.Calendar
    {
        public JapaneseCalendar() { }
#if FEATURE_COREFX_GLOBALIZATION
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public override System.Globalization.CalendarAlgorithmType AlgorithmType { get { throw null; } }
#endif
        public override int[] Eras { get { throw null; } }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public override System.DateTime MaxSupportedDateTime { get { throw null; } }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
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
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public override int GetLeapMonth(int year, int era) { throw null; }
        public override int GetMonth(System.DateTime time) { throw null; }
        public override int GetMonthsInYear(int year, int era) { throw null; }
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Contracts", "CC1055")]
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
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
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class JulianCalendar : System.Globalization.Calendar
    {
        public static readonly int JulianEra;
        public JulianCalendar() { }
#if FEATURE_COREFX_GLOBALIZATION
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public override System.Globalization.CalendarAlgorithmType AlgorithmType { get { throw null; } }
#endif
        public override int[] Eras { get { throw null; } }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public override System.DateTime MaxSupportedDateTime { get { throw null; } }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
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
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
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
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class KoreanCalendar : System.Globalization.Calendar
    {
        public const int KoreanEra = 1;
        public KoreanCalendar() { }
#if FEATURE_COREFX_GLOBALIZATION
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public override System.Globalization.CalendarAlgorithmType AlgorithmType { get { throw null; } }
#endif
        public override int[] Eras { get { throw null; } }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public override System.DateTime MaxSupportedDateTime { get { throw null; } }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
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
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public override int GetLeapMonth(int year, int era) { throw null; }
        public override int GetMonth(System.DateTime time) { throw null; }
        public override int GetMonthsInYear(int year, int era) { throw null; }
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Contracts", "CC1055")]
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
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
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
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
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public System.Globalization.DigitShapes DigitSubstitution { get { throw null; } set { throw null; } }
        public static System.Globalization.NumberFormatInfo InvariantInfo { get { throw null; } }
        public bool IsReadOnly { get { throw null; } }
        public string NaNSymbol { get { throw null; } set { } }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public string[] NativeDigits { get { throw null; } set { throw null; } }
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
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
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
#if FEATURE_COREFX_GLOBALIZATION
        public override System.Globalization.CalendarAlgorithmType AlgorithmType { get { throw null; } }
#endif
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
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class RegionInfo
    {
        public RegionInfo(int culture) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public RegionInfo(string name) { }
        public virtual string CurrencySymbol { [System.Security.SecuritySafeCriticalAttribute]get { throw null; } }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public virtual string CurrencyEnglishName { get { throw null; } }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public virtual string CurrencyNativeName { get { throw null; } }
        public static System.Globalization.RegionInfo CurrentRegion { [System.Security.SecuritySafeCriticalAttribute]get { throw null; } }
        public virtual string DisplayName { [System.Security.SecuritySafeCriticalAttribute]get { throw null; } }
        public virtual string EnglishName { [System.Security.SecuritySafeCriticalAttribute]get { throw null; } }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public virtual int GeoId { get { throw null; } }
        public virtual bool IsMetric { get { throw null; } }
        public virtual string ISOCurrencySymbol { [System.Security.SecuritySafeCriticalAttribute]get { throw null; } }
        public virtual string Name { get { throw null; } }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public virtual string NativeName { [System.Security.SecuritySafeCriticalAttribute]get { throw null; } }
        public virtual string ThreeLetterISORegionName { get { throw null; } }
        public virtual string ThreeLetterWindowsRegionName { get { throw null; } }
        public virtual string TwoLetterISORegionName { [System.Security.SecuritySafeCriticalAttribute]get { throw null; } }
        public override bool Equals(object value) { throw null; }
        public override int GetHashCode() { throw null; }
        public override string ToString() { throw null; }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class SortKey
    {
        internal SortKey() { throw null; }
        public virtual byte[] KeyData { get { throw null; } }
        public virtual string OriginalString { get { throw null; } }
        public static int Compare(System.Globalization.SortKey sortkey1, System.Globalization.SortKey sortkey2) { throw null; }
        public override bool Equals(object value) { throw null; }
        public override int GetHashCode() { throw null; }
        public override string ToString() { throw null; }
    }
    public sealed partial class SortVersion : System.IEquatable<System.Globalization.SortVersion>
    {
        public SortVersion(int fullVersion, System.Guid sortId) { throw null; }
        public int FullVersion { get { throw null; } }
        public System.Guid SortId { get { throw null; } }
        public bool Equals(System.Globalization.SortVersion other) { throw null; }
        public override bool Equals(object obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Globalization.SortVersion left, System.Globalization.SortVersion right) { throw null; }
        public static bool operator !=(System.Globalization.SortVersion left, System.Globalization.SortVersion right) { throw null; }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class StringInfo
    {
        public StringInfo() { }
        public StringInfo(string value) { }
        public int LengthInTextElements { get { throw null; } }
        public string String { get { throw null; } set { } }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public override bool Equals(object value) { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public override int GetHashCode() { throw null; }
        public static string GetNextTextElement(string str) { throw null; }
        public static string GetNextTextElement(string str, int index) { throw null; }
        public static System.Globalization.TextElementEnumerator GetTextElementEnumerator(string str) { throw null; }
        public static System.Globalization.TextElementEnumerator GetTextElementEnumerator(string str, int index) { throw null; }
        public static int[] ParseCombiningCharacters(string str) { throw null; }
        public string SubstringByTextElements(int startingTextElement) { throw null; }
        public string SubstringByTextElements(int startingTextElement, int lengthInTextElements) { throw null; }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class TaiwanCalendar : System.Globalization.Calendar
    {
        public TaiwanCalendar() { }
#if FEATURE_COREFX_GLOBALIZATION
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public override System.Globalization.CalendarAlgorithmType AlgorithmType { get { throw null; } }
#endif
        public override int[] Eras { get { throw null; } }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public override System.DateTime MaxSupportedDateTime { get { throw null; } }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
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
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public override int GetLeapMonth(int year, int era) { throw null; }
        public override int GetMonth(System.DateTime time) { throw null; }
        public override int GetMonthsInYear(int year, int era) { throw null; }
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Contracts", "CC1055")]
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
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
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class TextElementEnumerator : System.Collections.IEnumerator
    {
        internal TextElementEnumerator() { }
        public object Current { get { throw null; } }
        public int ElementIndex { get { throw null; } }
        public string GetTextElement() { throw null; }
        public bool MoveNext() { throw null; }
        public void Reset() { }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class TextInfo : System.ICloneable, System.Runtime.Serialization.IDeserializationCallback
    {
        internal TextInfo() { }
        public virtual int ANSICodePage { get { throw null; } }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public string CultureName { get { throw null; } }
        public virtual int EBCDICCodePage { get { throw null; } }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public bool IsReadOnly { get { throw null; } }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public bool IsRightToLeft { get { throw null; } }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public int LCID { get { throw null; } }
        public virtual string ListSeparator { [System.Security.SecuritySafeCriticalAttribute]get { throw null; } [System.Runtime.InteropServices.ComVisibleAttribute(false)]set { } }
        public virtual int MacCodePage { get { throw null; } }
        public virtual int OEMCodePage { get { throw null; } }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public virtual object Clone() { throw null; }
        public override bool Equals(object obj) { throw null; }
        public override int GetHashCode() { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public static System.Globalization.TextInfo ReadOnly(System.Globalization.TextInfo textInfo) { throw null; }
        void System.Runtime.Serialization.IDeserializationCallback.OnDeserialization(object sender) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public virtual char ToLower(char c) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public virtual string ToLower(string str) { throw null; }
        public override string ToString() { throw null; }
        public string ToTitleCase(string str) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public virtual char ToUpper(char c) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public virtual string ToUpper(string str) { throw null; }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class ThaiBuddhistCalendar : System.Globalization.Calendar
    {
        public const int ThaiBuddhistEra = 1;
        public ThaiBuddhistCalendar() { }
#if FEATURE_COREFX_GLOBALIZATION
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public override System.Globalization.CalendarAlgorithmType AlgorithmType { get { throw null; } }
#endif
        public override int[] Eras { get { throw null; } }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public override System.DateTime MaxSupportedDateTime { get { throw null; } }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
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
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public override int GetLeapMonth(int year, int era) { throw null; }
        public override int GetMonth(System.DateTime time) { throw null; }
        public override int GetMonthsInYear(int year, int era) { throw null; }
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Contracts", "CC1055")]
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
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
#if FEATURE_COREFX_GLOBALIZATION
        public override System.Globalization.CalendarAlgorithmType AlgorithmType { get { throw null; } }
#endif
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
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
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
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class BinaryReader : System.IDisposable
    {
        public BinaryReader(System.IO.Stream input) { }
        public BinaryReader(System.IO.Stream input, System.Text.Encoding encoding) { }
        public BinaryReader(System.IO.Stream input, System.Text.Encoding encoding, bool leaveOpen) { }
        public virtual System.IO.Stream BaseStream { get { throw null; } }
        public virtual void Close() { }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        protected virtual void FillBuffer(int numBytes) { }
        public virtual int PeekChar() { throw null; }
        public virtual int Read() { throw null; }
        public virtual int Read(byte[] buffer, int index, int count) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public virtual int Read(char[] buffer, int index, int count) { throw null; }
        protected internal int Read7BitEncodedInt() { throw null; }
        public virtual bool ReadBoolean() { throw null; }
        public virtual byte ReadByte() { throw null; }
        public virtual byte[] ReadBytes(int count) { throw null; }
        public virtual char ReadChar() { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public virtual char[] ReadChars(int count) { throw null; }
        public virtual decimal ReadDecimal() { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public virtual double ReadDouble() { throw null; }
        public virtual short ReadInt16() { throw null; }
        public virtual int ReadInt32() { throw null; }
        public virtual long ReadInt64() { throw null; }
        [System.CLSCompliantAttribute(false)]
        public virtual sbyte ReadSByte() { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public virtual float ReadSingle() { throw null; }
        public virtual string ReadString() { throw null; }
        [System.CLSCompliantAttribute(false)]
        public virtual ushort ReadUInt16() { throw null; }
        [System.CLSCompliantAttribute(false)]
        public virtual uint ReadUInt32() { throw null; }
        [System.CLSCompliantAttribute(false)]
        public virtual ulong ReadUInt64() { throw null; }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class BinaryWriter : System.IDisposable
    {
        public static readonly System.IO.BinaryWriter Null;
        protected System.IO.Stream OutStream;
        protected BinaryWriter() { }
        public BinaryWriter(System.IO.Stream output) { }
        public BinaryWriter(System.IO.Stream output, System.Text.Encoding encoding) { }
        public BinaryWriter(System.IO.Stream output, System.Text.Encoding encoding, bool leaveOpen) { }
        public virtual System.IO.Stream BaseStream { get { throw null; } }
        public virtual void Close() { }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        public virtual void Flush() { }
        public virtual long Seek(int offset, System.IO.SeekOrigin origin) { throw null; }
        public virtual void Write(bool value) { }
        public virtual void Write(byte value) { }
        public virtual void Write(byte[] buffer) { }
        public virtual void Write(byte[] buffer, int index, int count) { }
        [System.Security.SecuritySafeCriticalAttribute]
        public virtual void Write(char ch) { }
        public virtual void Write(char[] chars) { }
        public virtual void Write(char[] chars, int index, int count) { }
        public virtual void Write(decimal value) { }
        [System.Security.SecuritySafeCriticalAttribute]
        public virtual void Write(double value) { }
        public virtual void Write(short value) { }
        public virtual void Write(int value) { }
        public virtual void Write(long value) { }
        [System.CLSCompliantAttribute(false)]
        public virtual void Write(sbyte value) { }
        [System.Security.SecuritySafeCriticalAttribute]
        public virtual void Write(float value) { }
        [System.Security.SecuritySafeCriticalAttribute]
        public virtual void Write(string value) { }
        [System.CLSCompliantAttribute(false)]
        public virtual void Write(ushort value) { }
        [System.CLSCompliantAttribute(false)]
        public virtual void Write(uint value) { }
        [System.CLSCompliantAttribute(false)]
        public virtual void Write(ulong value) { }
        protected void Write7BitEncodedInt(int value) { }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class DirectoryNotFoundException : System.IO.IOException
    {
        public DirectoryNotFoundException() { }
        public DirectoryNotFoundException(string message) { }
        public DirectoryNotFoundException(string message, System.Exception innerException) { }
        protected DirectoryNotFoundException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class EndOfStreamException : System.IO.IOException
    {
        public EndOfStreamException() { }
        public EndOfStreamException(string message) { }
        public EndOfStreamException(string message, System.Exception innerException) { }
        protected EndOfStreamException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
    [System.FlagsAttribute]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public enum FileAccess
    {
        Read = 1,
        ReadWrite = 3,
        Write = 2,
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class FileLoadException : System.IO.IOException
    {
        public FileLoadException() { }
        public FileLoadException(string message) { }
        public FileLoadException(string message, System.Exception inner) { }
        public FileLoadException(string message, string fileName) { }
        public FileLoadException(string message, string fileName, System.Exception inner) { }
        protected FileLoadException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
        public string FileName { get { throw null; } }
        public override string Message { get { throw null; } }
        public override string ToString() { throw null; }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class FileNotFoundException : System.IO.IOException
    {
        public FileNotFoundException() { }
        public FileNotFoundException(string message) { }
        public FileNotFoundException(string message, System.Exception innerException) { }
        public FileNotFoundException(string message, string fileName) { }
        public FileNotFoundException(string message, string fileName, System.Exception innerException) { }
        protected FileNotFoundException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
        public string FileName { get { throw null; } }
        public override string Message { get { throw null; } }
        public override string ToString() { throw null; }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class IOException : System.SystemException
    {
        public IOException() { }
        public IOException(string message) { }
        public IOException(string message, System.Exception innerException) { }
        public IOException(string message, int hresult) { }
        protected IOException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class MemoryStream : System.IO.Stream
    {
        public MemoryStream() { }
        public MemoryStream(byte[] buffer) { }
        public MemoryStream(byte[] buffer, bool writable) { }
        public MemoryStream(byte[] buffer, int index, int count) { }
        public MemoryStream(byte[] buffer, int index, int count, bool writable) { }
        public MemoryStream(byte[] buffer, int index, int count, bool writable, bool publiclyVisible) { }
        public MemoryStream(int capacity) { }
        public override bool CanRead { get { throw null; } }
        public override bool CanSeek { get { throw null; } }
        public override bool CanWrite { get { throw null; } }
        public virtual int Capacity { get { throw null; } set { } }
        public override long Length { get { throw null; } }
        public override long Position { get { throw null; } set { } }
        public override System.Threading.Tasks.Task CopyToAsync(System.IO.Stream destination, int bufferSize, System.Threading.CancellationToken cancellationToken) { throw null; }
        protected override void Dispose(bool disposing) { }
        public override void Flush() { }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public override System.Threading.Tasks.Task FlushAsync(System.Threading.CancellationToken cancellationToken) { throw null; }
        public virtual byte[] GetBuffer() { throw null; }
        public override int Read(byte[] buffer, int offset, int count) { buffer = default(byte[]); throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public override System.Threading.Tasks.Task<int> ReadAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken) { throw null; }
        public override int ReadByte() { throw null; }
        public override long Seek(long offset, System.IO.SeekOrigin loc) { throw null; }
        public override void SetLength(long value) { }
        public virtual byte[] ToArray() { throw null; }
        public virtual bool TryGetBuffer(out System.ArraySegment<byte> buffer) { buffer = default(System.ArraySegment<byte>); throw null; }
        public override void Write(byte[] buffer, int offset, int count) { }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public override System.Threading.Tasks.Task WriteAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken) { throw null; }
        public override void WriteByte(byte value) { }
        public virtual void WriteTo(System.IO.Stream stream) { }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public static partial class Path
    {
        public static readonly char AltDirectorySeparatorChar;
        public static readonly char DirectorySeparatorChar;
        public static readonly char PathSeparator;
        public static readonly char VolumeSeparatorChar;
        public static string ChangeExtension(string path, string extension) { throw null; }
        public static string Combine(string path1, string path2) { throw null; }
        public static string Combine(string path1, string path2, string path3) { throw null; }
        public static string Combine(params string[] paths) { throw null; }
        public static string GetDirectoryName(string path) { throw null; }
        public static string GetExtension(string path) { throw null; }
        public static string GetFileName(string path) { throw null; }
        public static string GetFileNameWithoutExtension(string path) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public static string GetFullPath(string path) { throw null; }
        public static char[] GetInvalidFileNameChars() { throw null; }
        public static char[] GetInvalidPathChars() { throw null; }
        public static string GetPathRoot(string path) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public static string GetRandomFileName() { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public static string GetTempFileName() { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public static string GetTempPath() { throw null; }
        public static bool HasExtension(string path) { throw null; }
        public static bool IsPathRooted(string path) { throw null; }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class PathTooLongException : System.IO.IOException
    {
        public PathTooLongException() { }
        public PathTooLongException(string message) { }
        public PathTooLongException(string message, System.Exception innerException) { }
        protected PathTooLongException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public enum SeekOrigin
    {
        Begin = 0,
        Current = 1,
        End = 2,
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public abstract partial class Stream : System.IDisposable
    {
        public static readonly System.IO.Stream Null;
        protected Stream() { }
        public abstract bool CanRead { get; }
        public abstract bool CanSeek { get; }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public virtual bool CanTimeout { get { throw null; } }
        public abstract bool CanWrite { get; }
        public abstract long Length { get; }
        public abstract long Position { get; set; }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public virtual int ReadTimeout { get { throw null; } set { } }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public virtual int WriteTimeout { get { throw null; } set { } }
        public virtual System.IAsyncResult BeginRead(byte[] buffer, int offset, int count, System.AsyncCallback callback, object state) { throw null; }
        public virtual System.IAsyncResult BeginWrite(byte[] buffer, int offset, int count, System.AsyncCallback callback, object state) { throw null; }
        public virtual void Close() { }
        public void CopyTo(System.IO.Stream destination) { }
        public void CopyTo(System.IO.Stream destination, int bufferSize) { }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public System.Threading.Tasks.Task CopyToAsync(System.IO.Stream destination) { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public System.Threading.Tasks.Task CopyToAsync(System.IO.Stream destination, int bufferSize) { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public virtual System.Threading.Tasks.Task CopyToAsync(System.IO.Stream destination, int bufferSize, System.Threading.CancellationToken cancellationToken) { throw null; }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        public virtual int EndRead(System.IAsyncResult asyncResult) { throw null; }
        public virtual void EndWrite(System.IAsyncResult asyncResult) { }
        public abstract void Flush();
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public System.Threading.Tasks.Task FlushAsync() { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public virtual System.Threading.Tasks.Task FlushAsync(System.Threading.CancellationToken cancellationToken) { throw null; }
        public abstract int Read(byte[] buffer, int offset, int count);
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public System.Threading.Tasks.Task<int> ReadAsync(byte[] buffer, int offset, int count) { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public virtual System.Threading.Tasks.Task<int> ReadAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken) { throw null; }
        public virtual int ReadByte() { throw null; }
        public abstract long Seek(long offset, System.IO.SeekOrigin origin);
        public abstract void SetLength(long value);
        public abstract void Write(byte[] buffer, int offset, int count);
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public System.Threading.Tasks.Task WriteAsync(byte[] buffer, int offset, int count) { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public virtual System.Threading.Tasks.Task WriteAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken) { throw null; }
        public virtual void WriteByte(byte value) { }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class StreamReader : System.IO.TextReader
    {
        public static readonly new System.IO.StreamReader Null;
        public StreamReader(System.IO.Stream stream) { }
        public StreamReader(System.IO.Stream stream, bool detectEncodingFromByteOrderMarks) { }
        public StreamReader(System.IO.Stream stream, System.Text.Encoding encoding) { }
        public StreamReader(System.IO.Stream stream, System.Text.Encoding encoding, bool detectEncodingFromByteOrderMarks) { }
        public StreamReader(System.IO.Stream stream, System.Text.Encoding encoding, bool detectEncodingFromByteOrderMarks, int bufferSize) { }
        public StreamReader(System.IO.Stream stream, System.Text.Encoding encoding, bool detectEncodingFromByteOrderMarks, int bufferSize, bool leaveOpen) { }
        public StreamReader(string path) { }
        public StreamReader(string path, bool detectEncodingFromByteOrderMarks) { }
        public StreamReader(string path, System.Text.Encoding encoding) { }
        public StreamReader(string path, System.Text.Encoding encoding, bool detectEncodingFromByteOrderMarks) { }
        [System.Security.SecuritySafeCriticalAttribute]
        public StreamReader(string path, System.Text.Encoding encoding, bool detectEncodingFromByteOrderMarks, int bufferSize) { }
        public virtual System.IO.Stream BaseStream { get { throw null; } }
        public virtual System.Text.Encoding CurrentEncoding { get { throw null; } }
        public bool EndOfStream { get { throw null; } }
        public override void Close() { }
        public void DiscardBufferedData() { }
        protected override void Dispose(bool disposing) { }
        public override int Peek() { throw null; }
        public override int Read() { throw null; }
        public override int Read(char[] buffer, int index, int count) { buffer = default(char[]); throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public override System.Threading.Tasks.Task<int> ReadAsync(char[] buffer, int index, int count) { throw null; }
        public override int ReadBlock(char[] buffer, int index, int count) { buffer = default(char[]); throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public override System.Threading.Tasks.Task<int> ReadBlockAsync(char[] buffer, int index, int count) { throw null; }
        public override string ReadLine() { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public override System.Threading.Tasks.Task<string> ReadLineAsync() { throw null; }
        public override string ReadToEnd() { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public override System.Threading.Tasks.Task<string> ReadToEndAsync() { throw null; }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class StreamWriter : System.IO.TextWriter
    {
        public static readonly new System.IO.StreamWriter Null;
        public StreamWriter(System.IO.Stream stream) { }
        public StreamWriter(System.IO.Stream stream, System.Text.Encoding encoding) { }
        public StreamWriter(System.IO.Stream stream, System.Text.Encoding encoding, int bufferSize) { }
        public StreamWriter(System.IO.Stream stream, System.Text.Encoding encoding, int bufferSize, bool leaveOpen) { }
        public StreamWriter(string path) { }
        public StreamWriter(string path, bool append) { }
        public StreamWriter(string path, bool append, System.Text.Encoding encoding) { }
        [System.Security.SecuritySafeCriticalAttribute]
        public StreamWriter(string path, bool append, System.Text.Encoding encoding, int bufferSize) { }
        public virtual bool AutoFlush { get { throw null; } set { } }
        public virtual System.IO.Stream BaseStream { get { throw null; } }
        public override System.Text.Encoding Encoding { get { throw null; } }
        public override void Close() { }
        protected override void Dispose(bool disposing) { }
        public override void Flush() { }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public override System.Threading.Tasks.Task FlushAsync() { throw null; }
        public override void Write(char value) { }
        public override void Write(char[] buffer) { }
        public override void Write(char[] buffer, int index, int count) { }
        public override void Write(string value) { }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public override System.Threading.Tasks.Task WriteAsync(char value) { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public override System.Threading.Tasks.Task WriteAsync(char[] buffer, int index, int count) { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public override System.Threading.Tasks.Task WriteAsync(string value) { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public override System.Threading.Tasks.Task WriteLineAsync() { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public override System.Threading.Tasks.Task WriteLineAsync(char value) { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public override System.Threading.Tasks.Task WriteLineAsync(char[] buffer, int index, int count) { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public override System.Threading.Tasks.Task WriteLineAsync(string value) { throw null; }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class StringReader : System.IO.TextReader
    {
        public StringReader(string s) { }
        public override void Close() { }
        protected override void Dispose(bool disposing) { }
        public override int Peek() { throw null; }
        public override int Read() { throw null; }
        public override int Read(char[] buffer, int index, int count) { buffer = default(char[]); throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public override System.Threading.Tasks.Task<int> ReadAsync(char[] buffer, int index, int count) { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public override System.Threading.Tasks.Task<int> ReadBlockAsync(char[] buffer, int index, int count) { throw null; }
        public override string ReadLine() { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public override System.Threading.Tasks.Task<string> ReadLineAsync() { throw null; }
        public override string ReadToEnd() { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public override System.Threading.Tasks.Task<string> ReadToEndAsync() { throw null; }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class StringWriter : System.IO.TextWriter
    {
        public StringWriter() { }
        public StringWriter(System.IFormatProvider formatProvider) { }
        public StringWriter(System.Text.StringBuilder sb) { }
        public StringWriter(System.Text.StringBuilder sb, System.IFormatProvider formatProvider) { }
        public override System.Text.Encoding Encoding { get { throw null; } }
        public override void Close() { }
        protected override void Dispose(bool disposing) { }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public override System.Threading.Tasks.Task FlushAsync() { throw null; }
        public virtual System.Text.StringBuilder GetStringBuilder() { throw null; }
        public override string ToString() { throw null; }
        public override void Write(char value) { }
        public override void Write(char[] buffer, int index, int count) { }
        public override void Write(string value) { }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public override System.Threading.Tasks.Task WriteAsync(char value) { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public override System.Threading.Tasks.Task WriteAsync(char[] buffer, int index, int count) { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public override System.Threading.Tasks.Task WriteAsync(string value) { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public override System.Threading.Tasks.Task WriteLineAsync(char value) { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public override System.Threading.Tasks.Task WriteLineAsync(char[] buffer, int index, int count) { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public override System.Threading.Tasks.Task WriteLineAsync(string value) { throw null; }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public abstract partial class TextReader : System.IDisposable
    {
        public static readonly System.IO.TextReader Null;
        protected TextReader() { }
        public virtual void Close() { }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        public virtual int Peek() { throw null; }
        public virtual int Read() { throw null; }
        public virtual int Read(char[] buffer, int index, int count) { buffer = default(char[]); throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public virtual System.Threading.Tasks.Task<int> ReadAsync(char[] buffer, int index, int count) { throw null; }
        public virtual int ReadBlock(char[] buffer, int index, int count) { buffer = default(char[]); throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public virtual System.Threading.Tasks.Task<int> ReadBlockAsync(char[] buffer, int index, int count) { throw null; }
        public virtual string ReadLine() { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public virtual System.Threading.Tasks.Task<string> ReadLineAsync() { throw null; }
        public virtual string ReadToEnd() { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public virtual System.Threading.Tasks.Task<string> ReadToEndAsync() { throw null; }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public abstract partial class TextWriter : System.IDisposable
    {
        protected char[] CoreNewLine;
        public static readonly System.IO.TextWriter Null;
        protected TextWriter() { }
        protected TextWriter(System.IFormatProvider formatProvider) { }
        public abstract System.Text.Encoding Encoding { get; }
        public virtual System.IFormatProvider FormatProvider { get { throw null; } }
        public virtual string NewLine { get { throw null; } set { } }
        public virtual void Close() { }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        public virtual void Flush() { }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public virtual System.Threading.Tasks.Task FlushAsync() { throw null; }
        public virtual void Write(bool value) { }
        public virtual void Write(char value) { }
        public virtual void Write(char[] buffer) { }
        public virtual void Write(char[] buffer, int index, int count) { }
        public virtual void Write(decimal value) { }
        public virtual void Write(double value) { }
        public virtual void Write(int value) { }
        public virtual void Write(long value) { }
        public virtual void Write(object value) { }
        public virtual void Write(float value) { }
        public virtual void Write(string value) { }
        public virtual void Write(string format, object arg0) { }
        public virtual void Write(string format, object arg0, object arg1) { }
        public virtual void Write(string format, object arg0, object arg1, object arg2) { }
        public virtual void Write(string format, params object[] arg) { }
        [System.CLSCompliantAttribute(false)]
        public virtual void Write(uint value) { }
        [System.CLSCompliantAttribute(false)]
        public virtual void Write(ulong value) { }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public virtual System.Threading.Tasks.Task WriteAsync(char value) { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public System.Threading.Tasks.Task WriteAsync(char[] buffer) { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public virtual System.Threading.Tasks.Task WriteAsync(char[] buffer, int index, int count) { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public virtual System.Threading.Tasks.Task WriteAsync(string value) { throw null; }
        public virtual void WriteLine() { }
        public virtual void WriteLine(bool value) { }
        public virtual void WriteLine(char value) { }
        public virtual void WriteLine(char[] buffer) { }
        public virtual void WriteLine(char[] buffer, int index, int count) { }
        public virtual void WriteLine(decimal value) { }
        public virtual void WriteLine(double value) { }
        public virtual void WriteLine(int value) { }
        public virtual void WriteLine(long value) { }
        public virtual void WriteLine(object value) { }
        public virtual void WriteLine(float value) { }
        public virtual void WriteLine(string value) { }
        public virtual void WriteLine(string format, object arg0) { }
        public virtual void WriteLine(string format, object arg0, object arg1) { }
        public virtual void WriteLine(string format, object arg0, object arg1, object arg2) { }
        public virtual void WriteLine(string format, params object[] arg) { }
        [System.CLSCompliantAttribute(false)]
        public virtual void WriteLine(uint value) { }
        [System.CLSCompliantAttribute(false)]
        public virtual void WriteLine(ulong value) { }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public virtual System.Threading.Tasks.Task WriteLineAsync() { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public virtual System.Threading.Tasks.Task WriteLineAsync(char value) { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public System.Threading.Tasks.Task WriteLineAsync(char[] buffer) { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public virtual System.Threading.Tasks.Task WriteLineAsync(char[] buffer, int index, int count) { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public virtual System.Threading.Tasks.Task WriteLineAsync(string value) { throw null; }
    }
    public partial class UnmanagedMemoryAccessor : System.IDisposable
    {
        protected UnmanagedMemoryAccessor() { }
        [System.Security.SecuritySafeCriticalAttribute]
        public UnmanagedMemoryAccessor(System.Runtime.InteropServices.SafeBuffer buffer, long offset, long capacity) { }
        [System.Security.SecuritySafeCriticalAttribute]
        public UnmanagedMemoryAccessor(System.Runtime.InteropServices.SafeBuffer buffer, long offset, long capacity, System.IO.FileAccess access) { }
        public bool CanRead { get { throw null; } }
        public bool CanWrite { get { throw null; } }
        public long Capacity { get { throw null; } }
        protected bool IsOpen { get { throw null; } }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        [System.Security.SecuritySafeCriticalAttribute]
        [System.Security.Permissions.SecurityPermissionAttribute(System.Security.Permissions.SecurityAction.Demand, Flags=(System.Security.Permissions.SecurityPermissionFlag)(2))]
        protected void Initialize(System.Runtime.InteropServices.SafeBuffer buffer, long offset, long capacity, System.IO.FileAccess access) { }
        public bool ReadBoolean(long position) { throw null; }
        public byte ReadByte(long position) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public char ReadChar(long position) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public decimal ReadDecimal(long position) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public double ReadDouble(long position) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public short ReadInt16(long position) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public int ReadInt32(long position) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public long ReadInt64(long position) { throw null; }
        [System.CLSCompliantAttribute(false)]
        [System.Security.SecuritySafeCriticalAttribute]
        public sbyte ReadSByte(long position) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public float ReadSingle(long position) { throw null; }
        [System.CLSCompliantAttribute(false)]
        [System.Security.SecuritySafeCriticalAttribute]
        public ushort ReadUInt16(long position) { throw null; }
        [System.CLSCompliantAttribute(false)]
        [System.Security.SecuritySafeCriticalAttribute]
        public uint ReadUInt32(long position) { throw null; }
        [System.CLSCompliantAttribute(false)]
        [System.Security.SecuritySafeCriticalAttribute]
        public ulong ReadUInt64(long position) { throw null; }
        public void Write(long position, bool value) { }
        public void Write(long position, byte value) { }
        [System.Security.SecuritySafeCriticalAttribute]
        public void Write(long position, char value) { }
        [System.Security.SecuritySafeCriticalAttribute]
        public void Write(long position, decimal value) { }
        [System.Security.SecuritySafeCriticalAttribute]
        public void Write(long position, double value) { }
        [System.Security.SecuritySafeCriticalAttribute]
        public void Write(long position, short value) { }
        [System.Security.SecuritySafeCriticalAttribute]
        public void Write(long position, int value) { }
        [System.Security.SecuritySafeCriticalAttribute]
        public void Write(long position, long value) { }
        [System.CLSCompliantAttribute(false)]
        [System.Security.SecuritySafeCriticalAttribute]
        public void Write(long position, sbyte value) { }
        [System.Security.SecuritySafeCriticalAttribute]
        public void Write(long position, float value) { }
        [System.CLSCompliantAttribute(false)]
        [System.Security.SecuritySafeCriticalAttribute]
        public void Write(long position, ushort value) { }
        [System.CLSCompliantAttribute(false)]
        [System.Security.SecuritySafeCriticalAttribute]
        public void Write(long position, uint value) { }
        [System.CLSCompliantAttribute(false)]
        [System.Security.SecuritySafeCriticalAttribute]
        public void Write(long position, ulong value) { }
    }
    public partial class UnmanagedMemoryStream : System.IO.Stream
    {
        [System.Security.SecuritySafeCriticalAttribute]
        protected UnmanagedMemoryStream() { }
        [System.CLSCompliantAttribute(false)]
        [System.Security.SecurityCriticalAttribute]
        public unsafe UnmanagedMemoryStream(byte* pointer, long length) { }
        [System.CLSCompliantAttribute(false)]
        [System.Security.SecurityCriticalAttribute]
        public unsafe UnmanagedMemoryStream(byte* pointer, long length, long capacity, System.IO.FileAccess access) { }
        [System.Security.SecuritySafeCriticalAttribute]
        public UnmanagedMemoryStream(System.Runtime.InteropServices.SafeBuffer buffer, long offset, long length) { }
        [System.Security.SecuritySafeCriticalAttribute]
        public UnmanagedMemoryStream(System.Runtime.InteropServices.SafeBuffer buffer, long offset, long length, System.IO.FileAccess access) { }
        public override bool CanRead { get { throw null; } }
        public override bool CanSeek { get { throw null; } }
        public override bool CanWrite { get { throw null; } }
        public long Capacity { get { throw null; } }
        public override long Length { get { throw null; } }
        public override long Position { get { throw null; } [System.Security.SecuritySafeCriticalAttribute]set { } }
        [System.CLSCompliantAttribute(false)]
        public unsafe byte* PositionPointer { [System.Security.SecurityCriticalAttribute]get { throw null; } [System.Security.SecurityCriticalAttribute]set { } }
        [System.Security.SecuritySafeCriticalAttribute]
        protected override void Dispose(bool disposing) { }
        public override void Flush() { }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public override System.Threading.Tasks.Task FlushAsync(System.Threading.CancellationToken cancellationToken) { throw null; }
        [System.CLSCompliantAttribute(false)]
        [System.Security.SecurityCriticalAttribute]
        protected unsafe void Initialize(byte* pointer, long length, long capacity, System.IO.FileAccess access) { }
        [System.Security.SecuritySafeCriticalAttribute]
        protected void Initialize(System.Runtime.InteropServices.SafeBuffer buffer, long offset, long length, System.IO.FileAccess access) { }
        [System.Security.SecuritySafeCriticalAttribute]
        public override int Read(byte[] buffer, int offset, int count) { buffer = default(byte[]); throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public override System.Threading.Tasks.Task<int> ReadAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public override int ReadByte() { throw null; }
        public override long Seek(long offset, System.IO.SeekOrigin loc) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public override void SetLength(long value) { }
        [System.Security.SecuritySafeCriticalAttribute]
        public override void Write(byte[] buffer, int offset, int count) { }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public override System.Threading.Tasks.Task WriteAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public override void WriteByte(byte value) { }
    }
}
namespace System.Reflection
{
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class AmbiguousMatchException : System.SystemException
    {
        public AmbiguousMatchException() { }
        public AmbiguousMatchException(string message) { }
        public AmbiguousMatchException(string message, System.Exception inner) { }
    }
    [System.Runtime.InteropServices.ClassInterfaceAttribute((System.Runtime.InteropServices.ClassInterfaceType)(0))]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public abstract partial class Assembly : System.Reflection.ICustomAttributeProvider, System.Runtime.Serialization.ISerializable
    {
        protected Assembly() { }
        public virtual string CodeBase { [System.Security.SecurityCriticalAttribute]get { throw null; } }
        public virtual System.Collections.Generic.IEnumerable<System.Reflection.CustomAttributeData> CustomAttributes { get { throw null; } }
        public virtual System.Collections.Generic.IEnumerable<System.Reflection.TypeInfo> DefinedTypes { get { throw null; } }
        public virtual System.Reflection.MethodInfo EntryPoint { get { throw null; } }
        public virtual System.Collections.Generic.IEnumerable<System.Type> ExportedTypes { get { throw null; } }
        public virtual string FullName { get { throw null; } }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public virtual string ImageRuntimeVersion { get { throw null; } }
        public virtual bool IsDynamic { get { throw null; } }
        public virtual string Location { [System.Security.SecurityCriticalAttribute]get { throw null; } }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public virtual System.Reflection.Module ManifestModule { get { throw null; } }
        public virtual event ModuleResolveEventHandler ModuleResolve { [System.Security.SecurityCriticalAttribute]add { } [System.Security.SecurityCriticalAttribute]remove { } }
        public virtual System.Collections.Generic.IEnumerable<System.Reflection.Module> Modules { get { throw null; } }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public virtual bool ReflectionOnly { get { throw null; } }
        public object CreateInstance(string typeName) { throw null; }
        public object CreateInstance(string typeName, bool ignoreCase) { throw null; }
        public virtual object CreateInstance(String typeName, bool ignoreCase, BindingFlags bindingAttr, Binder binder, Object[] args, System.Globalization.CultureInfo culture, Object[] activationAttributes) { throw null; }
        public static string CreateQualifiedName(string assemblyName, string typeName) { throw null; }
        public override bool Equals(object o) { throw null; }
        public static Assembly GetAssembly(Type type) { throw null; }
        public static bool operator ==(System.Reflection.Assembly left, System.Reflection.Assembly right) { throw null; }
        public static bool operator !=(System.Reflection.Assembly left, System.Reflection.Assembly right) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)][System.Security.SecuritySafeCriticalAttribute]
        public static System.Reflection.Assembly GetCallingAssembly() { throw null; }
        public virtual object[] GetCustomAttributes(bool inherit) { throw null; }
        public virtual object[] GetCustomAttributes(System.Type attributeType, bool inherit) { throw null; }
        public virtual System.Collections.Generic.IList<CustomAttributeData> GetCustomAttributesData() { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public static System.Reflection.Assembly GetEntryAssembly() { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)][System.Security.SecuritySafeCriticalAttribute]
        public static System.Reflection.Assembly GetExecutingAssembly() { throw null; }
        public virtual System.Type[] GetExportedTypes() { throw null; }
        public override int GetHashCode() { throw null; }
        public System.Reflection.Module[] GetLoadedModules() { throw null; }
        public virtual System.Reflection.Module[] GetLoadedModules(bool getResourceModules) { throw null; }
        public virtual System.Reflection.ManifestResourceInfo GetManifestResourceInfo(string resourceName) { throw null; }
        public virtual string[] GetManifestResourceNames() { throw null; }
        public virtual System.IO.Stream GetManifestResourceStream(string name) { throw null; }
        public virtual System.IO.Stream GetManifestResourceStream(System.Type type, string name) { throw null; }
        public virtual System.Reflection.Module GetModule(String name) { throw null; }
        public System.Reflection.Module[] GetModules() { throw null; }
        public virtual System.Reflection.Module[] GetModules(bool getResourceModules) { throw null; }
        [System.Security.SecurityCriticalAttribute]
        public virtual System.Reflection.AssemblyName GetName(bool copiedName) { throw null; }
        [System.Security.SecurityCriticalAttribute]
        public virtual System.Reflection.AssemblyName GetName() { throw null; }
        [System.Security.SecurityCriticalAttribute]
        public virtual void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public virtual System.Reflection.AssemblyName[] GetReferencedAssemblies() { throw null; }
        public virtual System.Reflection.Assembly GetSatelliteAssembly(System.Globalization.CultureInfo culture) { throw null; }
        public virtual System.Reflection.Assembly GetSatelliteAssembly(System.Globalization.CultureInfo culture, System.Version version) { throw null; }
        public virtual System.Type GetType(string name) { throw null; }
        public virtual System.Type GetType(string name, bool throwOnError) { throw null; }
        public virtual System.Type GetType(string name, bool throwOnError, bool ignoreCase) { throw null; }
        public virtual System.Type[] GetTypes() { throw null; }
        public virtual bool IsDefined(System.Type attributeType, bool inherit) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)][System.Security.SecurityCriticalAttribute]
        public static System.Reflection.Assembly Load(byte[] rawAssembly) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)][System.Security.SecurityCriticalAttribute]
        public static System.Reflection.Assembly Load(byte[] rawAssembly, byte[] rawSymbolStore) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)][System.Security.SecurityCriticalAttribute]
        public static System.Reflection.Assembly Load(System.Reflection.AssemblyName assemblyRef) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)][System.Security.SecuritySafeCriticalAttribute]
        public static System.Reflection.Assembly Load(string assemblyString) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute][System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public static Assembly ReflectionOnlyLoad(byte[] rawAssembly) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute][System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public static Assembly ReflectionOnlyLoad(String assemblyString) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute][System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public static Assembly ReflectionOnlyLoadFrom(string assemblyFile) { throw null; }
        public override string ToString() { throw null; }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(1), Inherited=false)]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class AssemblyAlgorithmIdAttribute : System.Attribute
    {
        public AssemblyAlgorithmIdAttribute(System.Configuration.Assemblies.AssemblyHashAlgorithm algorithmId) { }
        [System.CLSCompliantAttribute(false)]
        public AssemblyAlgorithmIdAttribute(uint algorithmId) { }
        [System.CLSCompliantAttribute(false)]
        public uint AlgorithmId { get { throw null; } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(1), Inherited=false)]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class AssemblyCompanyAttribute : System.Attribute
    {
        public AssemblyCompanyAttribute(string company) { }
        public string Company { get { throw null; } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(1), Inherited=false)]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class AssemblyConfigurationAttribute : System.Attribute
    {
        public AssemblyConfigurationAttribute(string configuration) { }
        public string Configuration { get { throw null; } }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(false)]
    public enum AssemblyContentType
    {
        Default = 0,
        WindowsRuntime = 1,
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(1), Inherited=false)]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class AssemblyCopyrightAttribute : System.Attribute
    {
        public AssemblyCopyrightAttribute(string copyright) { }
        public string Copyright { get { throw null; } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(1), Inherited=false)]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class AssemblyCultureAttribute : System.Attribute
    {
        public AssemblyCultureAttribute(string culture) { }
        public string Culture { get { throw null; } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(1), Inherited=false)]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class AssemblyDefaultAliasAttribute : System.Attribute
    {
        public AssemblyDefaultAliasAttribute(string defaultAlias) { }
        public string DefaultAlias { get { throw null; } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(1), Inherited=false)]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class AssemblyDelaySignAttribute : System.Attribute
    {
        public AssemblyDelaySignAttribute(bool delaySign) { }
        public bool DelaySign { get { throw null; } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(1), Inherited=false)]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class AssemblyDescriptionAttribute : System.Attribute
    {
        public AssemblyDescriptionAttribute(string description) { }
        public string Description { get { throw null; } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(1), Inherited=false)]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class AssemblyFileVersionAttribute : System.Attribute
    {
        public AssemblyFileVersionAttribute(string version) { }
        public string Version { get { throw null; } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(1), Inherited=false)]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class AssemblyFlagsAttribute : System.Attribute
    {
        [System.CLSCompliantAttribute(false)]
        public AssemblyFlagsAttribute(uint flags) { }
        public AssemblyFlagsAttribute(int assemblyFlags) { }
        public AssemblyFlagsAttribute(System.Reflection.AssemblyNameFlags assemblyFlags) { }
        public int AssemblyFlags { get { throw null; } }
        [System.CLSCompliantAttribute(false)]
        public uint Flags { get { throw null; } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(1), Inherited=false)]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class AssemblyInformationalVersionAttribute : System.Attribute
    {
        public AssemblyInformationalVersionAttribute(string informationalVersion) { }
        public string InformationalVersion { get { throw null; } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(1), Inherited=false)]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class AssemblyKeyFileAttribute : System.Attribute
    {
        public AssemblyKeyFileAttribute(string keyFile) { }
        public string KeyFile { get { throw null; } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(1), Inherited=false)]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
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
    [System.Runtime.InteropServices.ClassInterfaceAttribute((System.Runtime.InteropServices.ClassInterfaceType)(0))]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class AssemblyName : System.ICloneable, System.Runtime.Serialization.ISerializable, System.Runtime.Serialization.IDeserializationCallback
    {
        public AssemblyName() { }
        [System.Security.SecuritySafeCriticalAttribute]
        public AssemblyName(string assemblyName) { }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public System.Reflection.AssemblyContentType ContentType { get { throw null; } set { } }
        public System.Globalization.CultureInfo CultureInfo { get { throw null; } set { } }
        public string CultureName { [System.Security.SecurityCriticalAttribute]get { throw null; } [System.Security.SecurityCriticalAttribute]set { } }
        public string CodeBase { get { throw null; } set { } }
        public System.Reflection.AssemblyNameFlags Flags { get { throw null; } set { } }
        public string FullName { [System.Security.SecuritySafeCriticalAttribute]get { throw null; } }
        public System.Configuration.Assemblies.AssemblyHashAlgorithm HashAlgorithm { get { throw null; } set { } }
        public System.Configuration.Assemblies.AssemblyVersionCompatibility VersionCompatibility { get { throw null; } set { } }
        public string Name { get { throw null; } set { } }
        public System.Reflection.ProcessorArchitecture ProcessorArchitecture { get { throw null; } set { } }
        public System.Version Version { get { throw null; } set { } }
        public object Clone() { throw null; }
        public byte[] GetPublicKey() { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public byte[] GetPublicKeyToken() { throw null; }
        public void SetPublicKey(byte[] publicKey) { }
        public void SetPublicKeyToken(byte[] publicKeyToken) { }
        public override string ToString() { throw null; }
        [System.Security.SecurityCriticalAttribute]
        public void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { throw null; }
        public void OnDeserialization(Object sender) { throw null; }
    }
    [System.FlagsAttribute]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public enum AssemblyNameFlags
    {
        EnableJITcompileOptimizer = 16384,
        EnableJITcompileTracking = 32768,
        None = 0,
        PublicKey = 1,
        Retargetable = 256,
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public class AssemblyNameProxy : System.MarshalByRefObject
    {
        internal AssemblyNameProxy() { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(1), Inherited=false)]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
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
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class AssemblyTitleAttribute : System.Attribute
    {
        public AssemblyTitleAttribute(string title) { }
        public string Title { get { throw null; } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(1), Inherited=false)]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class AssemblyTrademarkAttribute : System.Attribute
    {
        public AssemblyTrademarkAttribute(string trademark) { }
        public string Trademark { get { throw null; } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(1), Inherited=false)]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class AssemblyVersionAttribute : System.Attribute
    {
        public AssemblyVersionAttribute(string version) { }
        public string Version { get { throw null; } }
    }
    [System.Runtime.InteropServices.ClassInterfaceAttribute((System.Runtime.InteropServices.ClassInterfaceType)(2))]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public abstract partial class Binder
    {
        protected Binder() { }
        public abstract System.Reflection.FieldInfo BindToField(System.Reflection.BindingFlags bindingAttr, System.Reflection.FieldInfo[] match, object value, System.Globalization.CultureInfo culture);
        public abstract System.Reflection.MethodBase BindToMethod(System.Reflection.BindingFlags bindingAttr, System.Reflection.MethodBase[] match, ref object[] args, System.Reflection.ParameterModifier[] modifiers, System.Globalization.CultureInfo culture, string[] names, out object state);
        public abstract object ChangeType(object value, System.Type type, System.Globalization.CultureInfo culture);
        public abstract void ReorderArgumentArray(ref object[] args, object state);
        public abstract System.Reflection.MethodBase SelectMethod(System.Reflection.BindingFlags bindingAttr, System.Reflection.MethodBase[] match, System.Type[] types, System.Reflection.ParameterModifier[] modifiers);
        public abstract System.Reflection.PropertyInfo SelectProperty(System.Reflection.BindingFlags bindingAttr, System.Reflection.PropertyInfo[] match, System.Type returnType, System.Type[] indexes, System.Reflection.ParameterModifier[] modifiers);
        public virtual bool CanChangeType(System.Object value,System.Type type,System.Globalization.CultureInfo culture) { throw null; }
    }
    [System.FlagsAttribute]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public enum BindingFlags
    {
        CreateInstance = 512,
        DeclaredOnly = 2,
        Default = 0,
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
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public enum CallingConventions
    {
        Any = 3,
        ExplicitThis = 64,
        HasThis = 32,
        Standard = 1,
        VarArgs = 2,
    }
    [System.Runtime.InteropServices.ClassInterfaceAttribute((System.Runtime.InteropServices.ClassInterfaceType)(0))]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public abstract partial class ConstructorInfo : System.Reflection.MethodBase
    {
        public static bool operator ==(System.Reflection.ConstructorInfo left, System.Reflection.ConstructorInfo right) { throw null; }
        public static bool operator !=(System.Reflection.ConstructorInfo left, System.Reflection.ConstructorInfo right) { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(true)]
        public static readonly string ConstructorName;
        [System.Runtime.InteropServices.ComVisibleAttribute(true)]
        public static readonly string TypeConstructorName;
        protected ConstructorInfo() { }
        [System.Runtime.InteropServices.ComVisibleAttribute(true)]
        public override System.Reflection.MemberTypes MemberType { get { throw null; } }
        public override bool Equals(object obj) { throw null; }
        public override int GetHashCode() { throw null; }
        [System.Diagnostics.DebuggerHiddenAttribute]
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public object Invoke(object[] parameters) { throw null; }
        public abstract object Invoke(System.Reflection.BindingFlags invokeAttr, System.Reflection.Binder binder, object[] parameters, System.Globalization.CultureInfo culture);
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class CustomAttributeData
    {
        protected CustomAttributeData() { }
        public System.Type AttributeType { get { throw null; } }
        [System.Runtime.InteropServices.ComVisibleAttribute(true)]
        public virtual System.Reflection.ConstructorInfo Constructor { get { throw null; } }
        [System.Runtime.InteropServices.ComVisibleAttribute(true)]
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
        public static T GetCustomAttribute<T>(this System.Reflection.Assembly element) where T : System.Attribute { throw null; }
        public static T GetCustomAttribute<T>(this System.Reflection.MemberInfo element) where T : System.Attribute { throw null; }
        public static T GetCustomAttribute<T>(this System.Reflection.MemberInfo element, bool inherit) where T : System.Attribute { throw null; }
        public static T GetCustomAttribute<T>(this System.Reflection.Module element) where T : System.Attribute { throw null; }
        public static T GetCustomAttribute<T>(this System.Reflection.ParameterInfo element) where T : System.Attribute { throw null; }
        public static T GetCustomAttribute<T>(this System.Reflection.ParameterInfo element, bool inherit) where T : System.Attribute { throw null; }
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
        public static bool IsDefined(this System.Reflection.Assembly element, System.Type attributeType) { throw null; }
        public static bool IsDefined(this System.Reflection.MemberInfo element, System.Type attributeType) { throw null; }
        public static bool IsDefined(this System.Reflection.MemberInfo element, System.Type attributeType, bool inherit) { throw null; }
        public static bool IsDefined(this System.Reflection.Module element, System.Type attributeType) { throw null; }
        public static bool IsDefined(this System.Reflection.ParameterInfo element, System.Type attributeType) { throw null; }
        public static bool IsDefined(this System.Reflection.ParameterInfo element, System.Type attributeType, bool inherit) { throw null; }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class CustomAttributeFormatException : System.FormatException
    {
        public CustomAttributeFormatException() { }
        public CustomAttributeFormatException(string message) { }
        public CustomAttributeFormatException(string message, System.Exception inner) { }
        protected CustomAttributeFormatException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct CustomAttributeNamedArgument
    {
        public CustomAttributeNamedArgument(System.Reflection.MemberInfo memberInfo, object value) { }
        public CustomAttributeNamedArgument(System.Reflection.MemberInfo memberInfo, System.Reflection.CustomAttributeTypedArgument typedArgument) { }
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
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct CustomAttributeTypedArgument
    {
        public CustomAttributeTypedArgument(System.Type argumentType, object value) { }
        public CustomAttributeTypedArgument(object value) { }
        public System.Type ArgumentType { get { throw null; } }
        public object Value { get { throw null; } }
        public override bool Equals(object obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Reflection.CustomAttributeTypedArgument left, System.Reflection.CustomAttributeTypedArgument right) { throw null; }
        public static bool operator !=(System.Reflection.CustomAttributeTypedArgument left, System.Reflection.CustomAttributeTypedArgument right) { throw null; }
        public override string ToString() { throw null; }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(1036))]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class DefaultMemberAttribute : System.Attribute
    {
        public DefaultMemberAttribute(string memberName) { }
        public string MemberName { get { throw null; } }
    }
    [System.FlagsAttribute]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public enum EventAttributes
    {
        None = 0,
        ReservedMask = 1024,
        RTSpecialName = 1024,
        SpecialName = 512,
    }
    [System.Runtime.InteropServices.ClassInterfaceAttribute((System.Runtime.InteropServices.ClassInterfaceType)(0))]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public abstract partial class EventInfo : System.Reflection.MemberInfo
    {
        protected EventInfo() { }
        public static bool operator ==(System.Reflection.EventInfo left, System.Reflection.EventInfo right) { throw null; }
        public static bool operator !=(System.Reflection.EventInfo left, System.Reflection.EventInfo right) { throw null; }
        public virtual System.Reflection.MethodInfo AddMethod { get { throw null; } }
        public abstract System.Reflection.EventAttributes Attributes { get; }
        public virtual System.Type EventHandlerType { get { throw null; } }
        public virtual bool IsMulticast { get { throw null; } }
        public bool IsSpecialName { get { throw null; } }
        public override System.Reflection.MemberTypes MemberType { get { throw null; } }
        public virtual System.Reflection.MethodInfo RaiseMethod { get { throw null; } }
        public virtual System.Reflection.MethodInfo RemoveMethod { get { throw null; } }
        [System.Diagnostics.DebuggerHiddenAttribute]
        [System.Diagnostics.DebuggerStepThroughAttribute]
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
        [System.Diagnostics.DebuggerHiddenAttribute]
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual void RemoveEventHandler(object target, System.Delegate handler) { }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public class ExceptionHandlingClause
    {
        protected ExceptionHandlingClause() { }
        public virtual System.Reflection.ExceptionHandlingClauseOptions Flags { get { throw null; } }
        public virtual int TryOffset { get { throw null; } }
        public virtual int TryLength { get { throw null; } }
        public virtual int HandlerOffset { get { throw null; } }
        public virtual int HandlerLength { get { throw null; } }
        public virtual int FilterOffset { get { throw null; } }
        public virtual System.Type CatchType { get { throw null; } }
        public override string ToString() { throw null; }
    }
    [System.FlagsAttribute]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public enum ExceptionHandlingClauseOptions: int
    {
        Clause = 0,
        Filter = 1,
        Finally = 2,
        Fault = 4,
    }
    [System.FlagsAttribute]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
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
    [System.Runtime.InteropServices.ClassInterfaceAttribute((System.Runtime.InteropServices.ClassInterfaceType)(0))]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public abstract partial class FieldInfo : System.Reflection.MemberInfo
    {
        protected FieldInfo() { }
        public static bool operator ==(System.Reflection.FieldInfo left, System.Reflection.FieldInfo right) { throw null; }
        public static bool operator !=(System.Reflection.FieldInfo left, System.Reflection.FieldInfo right) { throw null; }
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
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public static System.Reflection.FieldInfo GetFieldFromHandle(System.RuntimeFieldHandle handle, System.RuntimeTypeHandle declaringType) { throw null; }
        public override int GetHashCode() { throw null; }
        public virtual System.Type[] GetOptionalCustomModifiers() { throw null; }
        public virtual object GetRawConstantValue() { throw null; }
        public virtual System.Type[] GetRequiredCustomModifiers() { throw null; }
        public abstract object GetValue(object obj);
        [System.Diagnostics.DebuggerHiddenAttribute]
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public void SetValue(object obj, object value) { }
        public abstract void SetValue(object obj, object value, System.Reflection.BindingFlags invokeAttr, System.Reflection.Binder binder, System.Globalization.CultureInfo culture);
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
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial interface ICustomAttributeProvider
    {
        object[] GetCustomAttributes(bool inherit);
        object[] GetCustomAttributes(System.Type attributeType, bool inherit);
        bool IsDefined(System.Type attributeType, bool inherit);
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public enum ImageFileMachine
    {
        I386    = 332,
        IA64    = 512,
        AMD64   = 34404,
        ARM     = 452,
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct InterfaceMapping
    {
        [System.Runtime.InteropServices.ComVisibleAttribute(true)]
        public System.Reflection.MethodInfo[] InterfaceMethods;
        [System.Runtime.InteropServices.ComVisibleAttribute(true)]
        public System.Type InterfaceType;
        [System.Runtime.InteropServices.ComVisibleAttribute(true)]
        public System.Reflection.MethodInfo[] TargetMethods;
        [System.Runtime.InteropServices.ComVisibleAttribute(true)]
        public System.Type TargetType;
    }
    public static partial class IntrospectionExtensions
    {
        public static System.Reflection.TypeInfo GetTypeInfo(this System.Type type) { throw null; }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class InvalidFilterCriteriaException : System.Exception
    {
        public InvalidFilterCriteriaException() { }
        public InvalidFilterCriteriaException(string message) { }
        public InvalidFilterCriteriaException(string message, System.Exception inner) { }
        protected InvalidFilterCriteriaException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    [System.Runtime.InteropServices.GuidAttribute("AFBF15E5-C37C-11d2-B88E-00A0C9B471B8")]
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
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class LocalVariableInfo
    {
        protected LocalVariableInfo() { }
        public virtual bool IsPinned { get { throw null; } }
        public virtual int LocalIndex { get { throw null; } }
        public virtual System.Type LocalType { get { throw null; } }
        public override string ToString() { throw null; }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class ManifestResourceInfo
    {
        public ManifestResourceInfo(System.Reflection.Assembly containingAssembly, string containingFileName, System.Reflection.ResourceLocation resourceLocation) { }
        public virtual string FileName { get { throw null; } }
        public virtual System.Reflection.Assembly ReferencedAssembly { get { throw null; } }
        public virtual System.Reflection.ResourceLocation ResourceLocation { get { throw null; } }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public delegate bool MemberFilter(System.Reflection.MemberInfo m, object filterCriteria);
    [System.Runtime.InteropServices.ClassInterfaceAttribute((System.Runtime.InteropServices.ClassInterfaceType)(0))]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public abstract partial class MemberInfo : System.Reflection.ICustomAttributeProvider
    {
        protected MemberInfo() { }
        public static bool operator ==(System.Reflection.MemberInfo left, System.Reflection.MemberInfo right) { throw null; }
        public static bool operator !=(System.Reflection.MemberInfo left, System.Reflection.MemberInfo right) { throw null; }
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
        public abstract bool IsDefined(System.Type attributeType, bool inherit);
    }
    [System.FlagsAttribute]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
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
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
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
    [System.Runtime.InteropServices.ClassInterfaceAttribute((System.Runtime.InteropServices.ClassInterfaceType)(0))]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public abstract partial class MethodBase : System.Reflection.MemberInfo
    {
        protected MethodBase() { }
        public abstract System.Reflection.MethodAttributes Attributes { get; }
        public virtual System.Reflection.CallingConventions CallingConvention { get { throw null; } }
        public virtual bool ContainsGenericParameters { get { throw null; } }
        public bool IsAbstract { get { throw null; } }
        public bool IsAssembly { get { throw null; } }
        [System.Runtime.InteropServices.ComVisibleAttribute(true)]
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
        public bool IsSpecialName { get { throw null; } }
        public bool IsStatic { get { throw null; } }
        public bool IsVirtual { get { throw null; } }
        public abstract System.RuntimeMethodHandle MethodHandle { get; }
        public virtual System.Reflection.MethodImplAttributes MethodImplementationFlags { get { throw null; } }
        public override bool Equals(object obj) { throw null; }
        public static bool operator ==(System.Reflection.MethodBase left, System.Reflection.MethodBase right) { throw null; }
        public static bool operator !=(System.Reflection.MethodBase left, System.Reflection.MethodBase right) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public static System.Reflection.MethodBase GetCurrentMethod() { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(true)]
        public virtual System.Type[] GetGenericArguments() { throw null; }
        public override int GetHashCode() { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public virtual System.Reflection.MethodBody GetMethodBody() { throw null; }
        public static System.Reflection.MethodBase GetMethodFromHandle(System.RuntimeMethodHandle handle) { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public static System.Reflection.MethodBase GetMethodFromHandle(System.RuntimeMethodHandle handle, System.RuntimeTypeHandle declaringType) { throw null; }
        public abstract System.Reflection.MethodImplAttributes GetMethodImplementationFlags();
        public abstract System.Reflection.ParameterInfo[] GetParameters();
        [System.Diagnostics.DebuggerHiddenAttribute]
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public object Invoke(object obj, object[] parameters) { throw null; }
        public abstract object Invoke(object obj, System.Reflection.BindingFlags invokeAttr, System.Reflection.Binder binder, object[] parameters, System.Globalization.CultureInfo culture);
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public class MethodBody
    {
        protected MethodBody() { }
        public virtual int LocalSignatureMetadataToken { get { throw null; } }
        public virtual System.Collections.Generic.IList<LocalVariableInfo> LocalVariables { get { throw null; } }
        public virtual int MaxStackSize { get { throw null; } }
        public virtual bool InitLocals { get { throw null; } }
        public virtual byte[] GetILAsByteArray() { throw null; }
        public virtual System.Collections.Generic.IList<ExceptionHandlingClause> ExceptionHandlingClauses { get { throw null; } }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public enum MethodImplAttributes
    {
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
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
    [System.Runtime.InteropServices.ClassInterfaceAttribute((System.Runtime.InteropServices.ClassInterfaceType)(0))]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public abstract partial class MethodInfo : System.Reflection.MethodBase
    {
        protected MethodInfo() { }
        public static bool operator ==(System.Reflection.MethodInfo left, System.Reflection.MethodInfo right) { throw null; }
        public static bool operator !=(System.Reflection.MethodInfo left, System.Reflection.MethodInfo right) { throw null; }
        public override System.Reflection.MemberTypes MemberType { get { throw null; } }
        public virtual System.Reflection.ParameterInfo ReturnParameter { get { throw null; } }
        public virtual System.Type ReturnType { get { throw null; } }
        public abstract System.Reflection.ICustomAttributeProvider ReturnTypeCustomAttributes { get; }
        public virtual System.Delegate CreateDelegate(System.Type delegateType) { throw null; }
        public virtual System.Delegate CreateDelegate(System.Type delegateType, object target) { throw null; }
        public override bool Equals(object obj) { throw null; }
        public abstract System.Reflection.MethodInfo GetBaseDefinition();
        [System.Runtime.InteropServices.ComVisibleAttribute(true)]
        public override System.Type[] GetGenericArguments() { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(true)]
        public virtual System.Reflection.MethodInfo GetGenericMethodDefinition() { throw null; }
        public override int GetHashCode() { throw null; }
        public virtual System.Reflection.MethodInfo MakeGenericMethod(params System.Type[] typeArguments) { throw null; }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class Missing : System.Runtime.Serialization.ISerializable
    {
        internal Missing() { }
        public static readonly System.Reflection.Missing Value;
        [System.Security.SecurityCriticalAttribute]
        void System.Runtime.Serialization.ISerializable.GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    }
    [System.Runtime.InteropServices.ClassInterfaceAttribute((System.Runtime.InteropServices.ClassInterfaceType)(0))]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public abstract partial class Module : System.Reflection.ICustomAttributeProvider, System.Runtime.Serialization.ISerializable
    {
        public static readonly System.Reflection.TypeFilter FilterTypeName;
        public static readonly System.Reflection.TypeFilter FilterTypeNameIgnoreCase;
        protected Module() { }
        public static bool operator ==(System.Reflection.Module left, System.Reflection.Module right) { throw null; }
        public static bool operator !=(System.Reflection.Module left, System.Reflection.Module right) { throw null; }
        public virtual System.Reflection.Assembly Assembly { get { throw null; } }
        public System.ModuleHandle ModuleHandle { get { throw null; } }
        public virtual System.Collections.Generic.IEnumerable<System.Reflection.CustomAttributeData> CustomAttributes { get { throw null; } }
        public virtual string FullyQualifiedName { [System.Security.SecurityCriticalAttribute]get { throw null; } }
        public virtual int MDStreamVersion { get { throw null; } }
        public virtual int MetadataToken { get { throw null; } }
        public virtual System.Guid ModuleVersionId { get { throw null; } }
        public virtual string Name { get { throw null; } }
        public virtual string ScopeName { get { throw null; } }
        public override bool Equals(object o) { throw null; }
        public virtual System.Type[] FindTypes(System.Reflection.TypeFilter filter, object filterCriteria) { throw null; }
        public virtual object[] GetCustomAttributes(bool inherit) { throw null; }
        public virtual object[] GetCustomAttributes(System.Type attributeType, bool inherit) { throw null; }
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
        [System.Security.SecurityCriticalAttribute]
        public virtual void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public virtual void GetPEKind(out System.Reflection.PortableExecutableKinds peKind, out System.Reflection.ImageFileMachine machine) { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(true)]
        public virtual System.Type GetType(string className) { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(true)]
        public virtual System.Type GetType(string className, bool ignoreCase) { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(true)]
        public virtual System.Type GetType(string className, bool throwOnError, bool ignoreCase) { throw null; }
        public virtual System.Type[] GetTypes() { throw null; }
        public virtual bool IsDefined(System.Type attributeType, bool inherit) { throw null; }
        public virtual bool IsResource() { throw null; }
        public virtual System.Collections.Generic.IList<CustomAttributeData> GetCustomAttributesData() { throw null; }
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
    [System.AttributeUsageAttribute((System.AttributeTargets)(1), AllowMultiple=false, Inherited=false)]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed class ObfuscateAssemblyAttribute : System.Attribute
    {
        public ObfuscateAssemblyAttribute(bool assemblyIsPrivate) { throw null; }
        public bool AssemblyIsPrivate { get { throw null; } }
        public bool StripAfterObfuscation { get { throw null; } set { } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(1) | (System.AttributeTargets)(4) | (System.AttributeTargets)(8) | (System.AttributeTargets)(64) | (System.AttributeTargets)(2048) | (System.AttributeTargets)(256) | (System.AttributeTargets)(128) | (System.AttributeTargets)(512) | (System.AttributeTargets)(1024) | (System.AttributeTargets)(16) | (System.AttributeTargets)(4096),
    AllowMultiple = true, Inherited = false)]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed class ObfuscationAttribute: System.Attribute
    {
        public ObfuscationAttribute() { }
        public bool StripAfterObfuscation { get { throw null; } set { } }
        public bool Exclude { get { throw null; } set { } }
        public bool ApplyToMembers { get { throw null; } set { } }
        public string Feature { get { throw null; } set { } }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public delegate System.Reflection.Module ModuleResolveEventHandler(object sender, System.ResolveEventArgs e);
    [System.FlagsAttribute]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
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
    [System.Runtime.InteropServices.ClassInterfaceAttribute((System.Runtime.InteropServices.ClassInterfaceType)(0))]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class ParameterInfo : System.Reflection.ICustomAttributeProvider, System.Runtime.Serialization.IObjectReference
    {
        protected String NameImpl;
        protected Type ClassImpl;
        protected int PositionImpl;
        protected System.Reflection.ParameterAttributes AttrsImpl;
        protected Object DefaultValueImpl;
        protected MemberInfo MemberImpl;
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
        [System.Security.SecurityCriticalAttribute]
        public object GetRealObject(System.Runtime.Serialization.StreamingContext context) { throw null; }
        public virtual System.Type[] GetRequiredCustomModifiers() { throw null; }
        public virtual bool IsDefined(System.Type attributeType, bool inherit) { throw null; }
        public override string ToString() { throw null; }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct ParameterModifier
    {
        public ParameterModifier(int parameterCount) { throw null;}
        public bool this[int index] { get { throw null; } set { } }
    }
    [System.CLSCompliantAttribute(false)]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed class Pointer : System.Runtime.Serialization.ISerializable
    {
        [System.Security.SecurityCriticalAttribute]
        public static unsafe Object Box(void *ptr, System.Type type) { throw null; }
        [System.Security.SecurityCriticalAttribute]
        public static unsafe void* Unbox(object ptr) { throw null; }
        [System.Security.SecurityCriticalAttribute]
        unsafe void System.Runtime.Serialization.ISerializable.GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    }
    [System.FlagsAttribute]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public enum PortableExecutableKinds
    {
        NotAPortableExecutableImage = 0,
        ILOnly                      = 1,
        Required32Bit               = 2,
        PE32Plus                    = 4,
        Unmanaged32Bit              = 8,
        [System.Runtime.InteropServices.ComVisible(false)]
        Preferred32Bit              = 16,
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
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
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
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
    [System.Runtime.InteropServices.ClassInterfaceAttribute((System.Runtime.InteropServices.ClassInterfaceType)(0))]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public abstract partial class PropertyInfo : System.Reflection.MemberInfo
    {
        protected PropertyInfo() { }
        public static bool operator ==(System.Reflection.PropertyInfo left, System.Reflection.PropertyInfo right) { throw null; }
        public static bool operator !=(System.Reflection.PropertyInfo left, System.Reflection.PropertyInfo right) { throw null; }
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
        [System.Diagnostics.DebuggerHiddenAttribute]
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public object GetValue(object obj) { throw null; }
        [System.Diagnostics.DebuggerHiddenAttribute]
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual object GetValue(object obj, object[] index) { throw null; }
        public abstract object GetValue(object obj, System.Reflection.BindingFlags invokeAttr, System.Reflection.Binder binder, object[] index, System.Globalization.CultureInfo culture);
        [System.Diagnostics.DebuggerHiddenAttribute]
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public void SetValue(object obj, object value) { }
        [System.Diagnostics.DebuggerHiddenAttribute]
        [System.Diagnostics.DebuggerStepThroughAttribute]
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
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class ReflectionTypeLoadException : System.SystemException, System.Runtime.Serialization.ISerializable
    {
        public ReflectionTypeLoadException(System.Type[] classes, System.Exception[] exceptions) { }
        public ReflectionTypeLoadException(System.Type[] classes, System.Exception[] exceptions, string message) { }
        public System.Exception[] LoaderExceptions { get { throw null; } }
        public System.Type[] Types { get { throw null; } }
        [System.Security.SecurityCriticalAttribute]
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    }
    [System.FlagsAttribute]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public enum ResourceAttributes
    {
        Private = 2,
        Public = 1,
    }
    [System.FlagsAttribute]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
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
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class TargetException : System.Exception
    {
        public TargetException() { }
        public TargetException(string message) { }
        public TargetException(string message, System.Exception inner) { }
        protected TargetException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class TargetInvocationException : System.Exception
    {
        public TargetInvocationException(System.Exception inner) { }
        public TargetInvocationException(string message, System.Exception inner) { }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class TargetParameterCountException : System.Exception
    {
        public TargetParameterCountException() { }
        public TargetParameterCountException(string message) { }
        public TargetParameterCountException(string message, System.Exception inner) { }
    }
    [System.FlagsAttribute]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
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
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        WindowsRuntime = 16384,
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public class TypeDelegator : System.Reflection.TypeInfo
    {
        protected System.Type typeImpl;
        public override bool IsAssignableFrom(System.Reflection.TypeInfo typeInfo) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        protected TypeDelegator() { }
        public TypeDelegator(System.Type delegatingType) { }
        public override System.Guid GUID { get { throw null; } }
        public override int MetadataToken { get { throw null; } }
        public override object InvokeMember(System.String name,System.Reflection.BindingFlags invokeAttr,System.Reflection.Binder binder,System.Object target, System.Object[] args,System.Reflection.ParameterModifier[] modifiers,System.Globalization.CultureInfo culture,System.String[] namedParameters) { throw null; }
        public override System.Reflection.Module Module { get { throw null; } }
        public override System.Reflection.Assembly Assembly { get { throw null; } }
        public override System.RuntimeTypeHandle TypeHandle { get { throw null; } }
        public override System.String Name { get { throw null; } }
        public override System.String FullName { get { throw null; } }
        public override System.String Namespace { get { throw null; } }
        public override System.String AssemblyQualifiedName { get { throw null; } }
        public override System.Type BaseType { get { throw null; } }
        protected override System.Reflection.ConstructorInfo GetConstructorImpl(System.Reflection.BindingFlags bindingAttr,System.Reflection.Binder binder, System.Reflection.CallingConventions callConvention, System.Type[] types, System.Reflection.ParameterModifier[] modifiers) { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(true)]
        public override System.Reflection.ConstructorInfo[] GetConstructors(System.Reflection.BindingFlags bindingAttr) { throw null; }
        protected override System.Reflection.MethodInfo GetMethodImpl(System.String name,System.Reflection.BindingFlags bindingAttr,System.Reflection.Binder binder, System.Reflection.CallingConventions callConvention, System.Type[] types,System.Reflection.ParameterModifier[] modifiers) { throw null; }
        public override System.Reflection.MethodInfo[] GetMethods(System.Reflection.BindingFlags bindingAttr) { throw null; }
        public override System.Reflection.FieldInfo GetField(System.String name, System.Reflection.BindingFlags bindingAttr) { throw null; }
        public override System.Reflection.FieldInfo[] GetFields(System.Reflection.BindingFlags bindingAttr) { throw null; }
        public override System.Type GetInterface(System.String name, bool ignoreCase) { throw null; }
        public override System.Type[] GetInterfaces() { throw null; }
        public override System.Reflection.EventInfo GetEvent(System.String name,System.Reflection.BindingFlags bindingAttr) { throw null; }
        public override System.Reflection.EventInfo[] GetEvents() { throw null; }
        protected override System.Reflection.PropertyInfo GetPropertyImpl(System.String name,System.Reflection.BindingFlags bindingAttr,System.Reflection.Binder binder, System.Type returnType, System.Type[] types, System.Reflection.ParameterModifier[] modifiers) { throw null; }
        public override System.Reflection.PropertyInfo[] GetProperties(System.Reflection.BindingFlags bindingAttr) { throw null; }
        public override System.Reflection.EventInfo[] GetEvents(System.Reflection.BindingFlags bindingAttr) { throw null; }
        public override System.Type[] GetNestedTypes(System.Reflection.BindingFlags bindingAttr) { throw null; }
        public override System.Type GetNestedType(System.String name, System.Reflection.BindingFlags bindingAttr) { throw null; }
        public override System.Reflection.MemberInfo[] GetMember(System.String name,  System.Reflection.MemberTypes type, System.Reflection.BindingFlags bindingAttr) { throw null; }
        public override System.Reflection.MemberInfo[] GetMembers(System.Reflection.BindingFlags bindingAttr) { throw null; }
        protected override TypeAttributes GetAttributeFlagsImpl() { throw null; }
        protected override bool IsArrayImpl() { throw null; }
        protected override bool IsPrimitiveImpl() { throw null; }
        protected override bool IsByRefImpl() { throw null; }
        protected override bool IsPointerImpl() { throw null; }
        protected override bool IsValueTypeImpl() { throw null; }
        protected override bool IsCOMObjectImpl() { throw null; }
        public override bool IsConstructedGenericType { get { throw null; } }
        public override System.Type GetElementType() { throw null; }
        protected override bool HasElementTypeImpl() { throw null; }
        public override System.Type UnderlyingSystemType { get { throw null; } }
        public override System.Object[] GetCustomAttributes(bool inherit) { throw null; }
        public override System.Object[] GetCustomAttributes(System.Type attributeType, bool inherit) { throw null; }
        public override bool IsDefined(System.Type attributeType, bool inherit) { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(true)]
        public override System.Reflection.InterfaceMapping GetInterfaceMap(System.Type interfaceType) { throw null; }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public delegate bool TypeFilter(System.Type m, object filterCriteria);
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
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
namespace System.Reflection.Emit
{
    [System.Runtime.InteropServices.ClassInterfaceAttribute((System.Runtime.InteropServices.ClassInterfaceType)(0))]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class AssemblyBuilder : System.Reflection.Assembly
    {
        internal AssemblyBuilder() { }
        public override string CodeBase { [System.Security.SecurityCriticalAttribute]get { throw null; } }
        public override System.Reflection.MethodInfo EntryPoint { get { throw null; } }
        public override string FullName { get { throw null; } }
        public override string ImageRuntimeVersion { get { throw null; } }
        public override bool IsDynamic { get { throw null; } }
        public override string Location { [System.Security.SecurityCriticalAttribute]get { throw null; } }
        public override System.Reflection.Module ManifestModule { get { throw null; } }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)][System.Security.SecuritySafeCriticalAttribute]
        public static System.Reflection.Emit.AssemblyBuilder DefineDynamicAssembly(System.Reflection.AssemblyName name, System.Reflection.Emit.AssemblyBuilderAccess access) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)][System.Security.SecuritySafeCriticalAttribute]
        public static System.Reflection.Emit.AssemblyBuilder DefineDynamicAssembly(System.Reflection.AssemblyName name, System.Reflection.Emit.AssemblyBuilderAccess access, System.Collections.Generic.IEnumerable<System.Reflection.Emit.CustomAttributeBuilder> assemblyAttributes) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)][System.Security.SecuritySafeCriticalAttribute]
        public System.Reflection.Emit.ModuleBuilder DefineDynamicModule(string name) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)][System.Security.SecuritySafeCriticalAttribute]
        public System.Reflection.Emit.ModuleBuilder DefineDynamicModule(string name, bool emitSymbolInfo) { throw null; }
        public override bool Equals(object obj) { throw null; }
        public override object[] GetCustomAttributes(bool inherit) { throw null; }
        public override object[] GetCustomAttributes(System.Type attributeType, bool inherit) { throw null; }
        public System.Reflection.Emit.ModuleBuilder GetDynamicModule(string name) { throw null; }
        public override System.Type[] GetExportedTypes() { throw null; }
        public override int GetHashCode() { throw null; }
        public override System.Reflection.ManifestResourceInfo GetManifestResourceInfo(string resourceName) { throw null; }
        public override string[] GetManifestResourceNames() { throw null; }
        public override System.IO.Stream GetManifestResourceStream(string name) { throw null; }
        public override System.IO.Stream GetManifestResourceStream(System.Type type, string name) { throw null; }
        public override System.Reflection.AssemblyName[] GetReferencedAssemblies() { throw null; }
        public override System.Type GetType(string name, bool throwOnError, bool ignoreCase) { throw null; }
        public override bool IsDefined(System.Type attributeType, bool inherit) { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(true)]
        [System.Security.SecurityCriticalAttribute]
        public void SetCustomAttribute(System.Reflection.ConstructorInfo con, byte[] binaryAttribute) { }
        [System.Security.SecuritySafeCriticalAttribute]
        public void SetCustomAttribute(System.Reflection.Emit.CustomAttributeBuilder customBuilder) { }
    }
    [System.FlagsAttribute]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public enum AssemblyBuilderAccess
    {
        Run = 1,
        RunAndCollect = 9,
    }
    [System.Runtime.InteropServices.ClassInterfaceAttribute((System.Runtime.InteropServices.ClassInterfaceType)(0))]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class ConstructorBuilder : System.Reflection.ConstructorInfo
    {
        internal ConstructorBuilder() { }
        public override System.Reflection.MethodAttributes Attributes { get { throw null; } }
        public override System.Reflection.CallingConventions CallingConvention { get { throw null; } }
        public override System.Type DeclaringType { get { throw null; } }
        public bool InitLocals { get { throw null; } set { } }
        public override System.RuntimeMethodHandle MethodHandle { get { throw null; } }
        public override System.Reflection.Module Module { get { throw null; } }
        public override string Name { get { throw null; } }
        public override System.Type ReflectedType { get { throw null; } }
        public string Signature { get { throw null; } }
        public System.Reflection.Emit.ParameterBuilder DefineParameter(int iSequence, System.Reflection.ParameterAttributes attributes, string strParamName) { throw null; }
        public override object[] GetCustomAttributes(bool inherit) { throw null; }
        public override object[] GetCustomAttributes(System.Type attributeType, bool inherit) { throw null; }
        public System.Reflection.Emit.ILGenerator GetILGenerator() { throw null; }
        public System.Reflection.Emit.ILGenerator GetILGenerator(int streamSize) { throw null; }
        public override System.Reflection.MethodImplAttributes GetMethodImplementationFlags() { throw null; }
        public System.Reflection.Module GetModule() { throw null; }
        public override System.Reflection.ParameterInfo[] GetParameters() { throw null; }
        public System.Reflection.Emit.MethodToken GetToken() { throw null; }
        public override object Invoke(object obj, System.Reflection.BindingFlags invokeAttr, System.Reflection.Binder binder, object[] parameters, System.Globalization.CultureInfo culture) { throw null; }
        public override object Invoke(System.Reflection.BindingFlags invokeAttr, System.Reflection.Binder binder, object[] parameters, System.Globalization.CultureInfo culture) { throw null; }
        public override bool IsDefined(System.Type attributeType, bool inherit) { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(true)]
        [System.Security.SecurityCriticalAttribute]
        public void SetCustomAttribute(System.Reflection.ConstructorInfo con, byte[] binaryAttribute) { }
        public void SetCustomAttribute(System.Reflection.Emit.CustomAttributeBuilder customBuilder) { }
        public void SetImplementationFlags(System.Reflection.MethodImplAttributes attributes) { }
        public override string ToString() { throw null; }
    }
    [System.Runtime.InteropServices.ClassInterfaceAttribute((System.Runtime.InteropServices.ClassInterfaceType)(0))]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class CustomAttributeBuilder
    {
        public CustomAttributeBuilder(System.Reflection.ConstructorInfo con, object[] constructorArgs) { }
        public CustomAttributeBuilder(System.Reflection.ConstructorInfo con, object[] constructorArgs, System.Reflection.FieldInfo[] namedFields, object[] fieldValues) { }
        public CustomAttributeBuilder(System.Reflection.ConstructorInfo con, object[] constructorArgs, System.Reflection.PropertyInfo[] namedProperties, object[] propertyValues) { }
        public CustomAttributeBuilder(System.Reflection.ConstructorInfo con, object[] constructorArgs, System.Reflection.PropertyInfo[] namedProperties, object[] propertyValues, System.Reflection.FieldInfo[] namedFields, object[] fieldValues) { }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class DynamicMethod : System.Reflection.MethodInfo
    {
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)][System.Security.SecurityCriticalAttribute]
        public DynamicMethod(string name, System.Reflection.MethodAttributes attributes, System.Reflection.CallingConventions callingConvention, System.Type returnType, System.Type[] parameterTypes, System.Reflection.Module m, bool skipVisibility) { }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)][System.Security.SecurityCriticalAttribute]
        public DynamicMethod(string name, System.Reflection.MethodAttributes attributes, System.Reflection.CallingConventions callingConvention, System.Type returnType, System.Type[] parameterTypes, System.Type owner, bool skipVisibility) { }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)][System.Security.SecuritySafeCriticalAttribute]
        public DynamicMethod(string name, System.Type returnType, System.Type[] parameterTypes) { }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)][System.Security.SecuritySafeCriticalAttribute]
        public DynamicMethod(string name, System.Type returnType, System.Type[] parameterTypes, bool restrictedSkipVisibility) { }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)][System.Security.SecurityCriticalAttribute]
        public DynamicMethod(string name, System.Type returnType, System.Type[] parameterTypes, System.Reflection.Module m) { }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)][System.Security.SecurityCriticalAttribute]
        public DynamicMethod(string name, System.Type returnType, System.Type[] parameterTypes, System.Reflection.Module m, bool skipVisibility) { }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)][System.Security.SecurityCriticalAttribute]
        public DynamicMethod(string name, System.Type returnType, System.Type[] parameterTypes, System.Type owner) { }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)][System.Security.SecurityCriticalAttribute]
        public DynamicMethod(string name, System.Type returnType, System.Type[] parameterTypes, System.Type owner, bool skipVisibility) { }
        public override System.Reflection.MethodAttributes Attributes { get { throw null; } }
        public override System.Reflection.CallingConventions CallingConvention { get { throw null; } }
        public override System.Type DeclaringType { get { throw null; } }
        public bool InitLocals { get { throw null; } set { } }
        public override System.RuntimeMethodHandle MethodHandle { get { throw null; } }
        public override System.Reflection.Module Module { get { throw null; } }
        public override string Name { get { throw null; } }
        public override System.Type ReflectedType { get { throw null; } }
        public override System.Reflection.ParameterInfo ReturnParameter { get { throw null; } }
        public override System.Type ReturnType { get { throw null; } }
        public override System.Reflection.ICustomAttributeProvider ReturnTypeCustomAttributes { get { throw null; } }
        [System.Runtime.InteropServices.ComVisibleAttribute(true)]
        [System.Security.SecuritySafeCriticalAttribute]
        public sealed override System.Delegate CreateDelegate(System.Type delegateType) { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(true)]
        [System.Security.SecuritySafeCriticalAttribute]
        public sealed override System.Delegate CreateDelegate(System.Type delegateType, object target) { throw null; }
        public System.Reflection.Emit.ParameterBuilder DefineParameter(int position, System.Reflection.ParameterAttributes attributes, string parameterName) { throw null; }
        public override System.Reflection.MethodInfo GetBaseDefinition() { throw null; }
        public override object[] GetCustomAttributes(bool inherit) { throw null; }
        public override object[] GetCustomAttributes(System.Type attributeType, bool inherit) { throw null; }
        public System.Reflection.Emit.ILGenerator GetILGenerator() { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public System.Reflection.Emit.ILGenerator GetILGenerator(int streamSize) { throw null; }
        public override System.Reflection.MethodImplAttributes GetMethodImplementationFlags() { throw null; }
        public override System.Reflection.ParameterInfo[] GetParameters() { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public override object Invoke(object obj, System.Reflection.BindingFlags invokeAttr, System.Reflection.Binder binder, object[] parameters, System.Globalization.CultureInfo culture) { throw null; }
        public override bool IsDefined(System.Type attributeType, bool inherit) { throw null; }
        public override string ToString() { throw null; }
    }
    [System.Runtime.InteropServices.ClassInterfaceAttribute((System.Runtime.InteropServices.ClassInterfaceType)(0))]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class EnumBuilder : System.Reflection.TypeInfo
    {
        internal EnumBuilder() { }
        public override System.Reflection.Assembly Assembly { get { throw null; } }
        public override string AssemblyQualifiedName { get { throw null; } }
        public override System.Type BaseType { get { throw null; } }
        public override System.Type DeclaringType { get { throw null; } }
        public override string FullName { get { throw null; } }
        public override System.Guid GUID { get { throw null; } }
        public override bool IsConstructedGenericType { get { throw null; } }
        public override System.Reflection.Module Module { get { throw null; } }
        public override string Name { get { throw null; } }
        public override string Namespace { get { throw null; } }
        public override System.Type ReflectedType { get { throw null; } }
        public override System.RuntimeTypeHandle TypeHandle { get { throw null; } }
        public System.Reflection.Emit.TypeToken TypeToken { get { throw null; } }
        public System.Reflection.Emit.FieldBuilder UnderlyingField { get { throw null; } }
        public override System.Type UnderlyingSystemType { get { throw null; } }
        public System.Type CreateType() { throw null; }
        public System.Reflection.TypeInfo CreateTypeInfo() { throw null; }
        public System.Reflection.Emit.FieldBuilder DefineLiteral(string literalName, object literalValue) { throw null; }
        protected override System.Reflection.TypeAttributes GetAttributeFlagsImpl() { throw null; }
        protected override System.Reflection.ConstructorInfo GetConstructorImpl(System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder binder, System.Reflection.CallingConventions callConvention, System.Type[] types, System.Reflection.ParameterModifier[] modifiers) { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(true)]
        public override System.Reflection.ConstructorInfo[] GetConstructors(System.Reflection.BindingFlags bindingAttr) { throw null; }
        public override object[] GetCustomAttributes(bool inherit) { throw null; }
        public override object[] GetCustomAttributes(System.Type attributeType, bool inherit) { throw null; }
        public override System.Type GetElementType() { throw null; }
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Contracts", "CC1055")]
        public override System.Type GetEnumUnderlyingType() { throw null; }
        public override System.Reflection.EventInfo GetEvent(string name, System.Reflection.BindingFlags bindingAttr) { throw null; }
        public override System.Reflection.EventInfo[] GetEvents() { throw null; }
        public override System.Reflection.EventInfo[] GetEvents(System.Reflection.BindingFlags bindingAttr) { throw null; }
        public override System.Reflection.FieldInfo GetField(string name, System.Reflection.BindingFlags bindingAttr) { throw null; }
        public override System.Reflection.FieldInfo[] GetFields(System.Reflection.BindingFlags bindingAttr) { throw null; }
        public override System.Type GetInterface(string name, bool ignoreCase) { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(true)]
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
        public override System.Type MakeArrayType() { throw null; }
        public override System.Type MakeArrayType(int rank) { throw null; }
        public override System.Type MakeByRefType() { throw null; }
        public override System.Type MakePointerType() { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(true)]
        [System.Security.SecurityCriticalAttribute]
        public void SetCustomAttribute(System.Reflection.ConstructorInfo con, byte[] binaryAttribute) { }
        public void SetCustomAttribute(System.Reflection.Emit.CustomAttributeBuilder customBuilder) { }
    }
    [System.Runtime.InteropServices.ClassInterfaceAttribute((System.Runtime.InteropServices.ClassInterfaceType)(0))]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class EventBuilder
    {
        internal EventBuilder() { }
        [System.Security.SecuritySafeCriticalAttribute]
        public void AddOtherMethod(System.Reflection.Emit.MethodBuilder mdBuilder) { }
        public System.Reflection.Emit.EventToken GetEventToken() { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public void SetAddOnMethod(System.Reflection.Emit.MethodBuilder mdBuilder) { }
        [System.Runtime.InteropServices.ComVisibleAttribute(true)]
        [System.Security.SecurityCriticalAttribute]
        public void SetCustomAttribute(System.Reflection.ConstructorInfo con, byte[] binaryAttribute) { }
        [System.Security.SecuritySafeCriticalAttribute]
        public void SetCustomAttribute(System.Reflection.Emit.CustomAttributeBuilder customBuilder) { }
        [System.Security.SecuritySafeCriticalAttribute]
        public void SetRaiseMethod(System.Reflection.Emit.MethodBuilder mdBuilder) { }
        [System.Security.SecuritySafeCriticalAttribute]
        public void SetRemoveOnMethod(System.Reflection.Emit.MethodBuilder mdBuilder) { }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct EventToken
    {
        public static readonly System.Reflection.Emit.EventToken Empty;
        public int Token { get { throw null; } }
        public override bool Equals(object obj) { throw null; }
        public bool Equals(System.Reflection.Emit.EventToken obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Reflection.Emit.EventToken a, System.Reflection.Emit.EventToken b) { throw null; }
        public static bool operator !=(System.Reflection.Emit.EventToken a, System.Reflection.Emit.EventToken b) { throw null; }
    }
    [System.Runtime.InteropServices.ClassInterfaceAttribute((System.Runtime.InteropServices.ClassInterfaceType)(0))]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class FieldBuilder : System.Reflection.FieldInfo
    {
        internal FieldBuilder() { }
        public override System.Reflection.FieldAttributes Attributes { get { throw null; } }
        public override System.Type DeclaringType { get { throw null; } }
        public override System.RuntimeFieldHandle FieldHandle { get { throw null; } }
        public override System.Type FieldType { get { throw null; } }
        public override System.Reflection.Module Module { get { throw null; } }
        public override string Name { get { throw null; } }
        public override System.Type ReflectedType { get { throw null; } }
        public override object[] GetCustomAttributes(bool inherit) { throw null; }
        public override object[] GetCustomAttributes(System.Type attributeType, bool inherit) { throw null; }
        public System.Reflection.Emit.FieldToken GetToken() { throw null; }
        public override object GetValue(object obj) { throw null; }
        public override bool IsDefined(System.Type attributeType, bool inherit) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public void SetConstant(object defaultValue) { }
        [System.Runtime.InteropServices.ComVisibleAttribute(true)]
        [System.Security.SecurityCriticalAttribute]
        public void SetCustomAttribute(System.Reflection.ConstructorInfo con, byte[] binaryAttribute) { }
        [System.Security.SecuritySafeCriticalAttribute]
        public void SetCustomAttribute(System.Reflection.Emit.CustomAttributeBuilder customBuilder) { }
        [System.Security.SecurityCriticalAttribute]
        public void SetOffset(int iOffset) { }
        public override void SetValue(object obj, object val, System.Reflection.BindingFlags invokeAttr, System.Reflection.Binder binder, System.Globalization.CultureInfo culture) { }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct FieldToken
    {
        public static readonly System.Reflection.Emit.FieldToken Empty;
        public int Token { get { throw null; } }
        public override bool Equals(object obj) { throw null; }
        public bool Equals(System.Reflection.Emit.FieldToken obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Reflection.Emit.FieldToken a, System.Reflection.Emit.FieldToken b) { throw null; }
        public static bool operator !=(System.Reflection.Emit.FieldToken a, System.Reflection.Emit.FieldToken b) { throw null; }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public enum FlowControl
    {
        Branch = 0,
        Break = 1,
        Call = 2,
        Cond_Branch = 3,
        Meta = 4,
        Next = 5,
        Return = 7,
        Throw = 8,
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class GenericTypeParameterBuilder : System.Reflection.TypeInfo
    {
        internal GenericTypeParameterBuilder() { }
        public override System.Reflection.Assembly Assembly { get { throw null; } }
        public override string AssemblyQualifiedName { get { throw null; } }
        public override System.Type BaseType { get { throw null; } }
        public override bool ContainsGenericParameters { get { throw null; } }
        public override System.Reflection.MethodBase DeclaringMethod { get { throw null; } }
        public override System.Type DeclaringType { get { throw null; } }
        public override string FullName { get { throw null; } }
        public override System.Reflection.GenericParameterAttributes GenericParameterAttributes { get { throw null; } }
        public override int GenericParameterPosition { get { throw null; } }
        public override System.Guid GUID { get { throw null; } }
        public override bool IsConstructedGenericType { get { throw null; } }
        public override bool IsGenericParameter { get { throw null; } }
        public override bool IsGenericType { get { throw null; } }
        public override bool IsGenericTypeDefinition { get { throw null; } }
        public override System.Reflection.Module Module { get { throw null; } }
        public override string Name { get { throw null; } }
        public override string Namespace { get { throw null; } }
        public override System.Type ReflectedType { get { throw null; } }
        public override System.RuntimeTypeHandle TypeHandle { get { throw null; } }
        public override System.Type UnderlyingSystemType { get { throw null; } }
        public override bool Equals(object o) { throw null; }
        protected override System.Reflection.TypeAttributes GetAttributeFlagsImpl() { throw null; }
        protected override System.Reflection.ConstructorInfo GetConstructorImpl(System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder binder, System.Reflection.CallingConventions callConvention, System.Type[] types, System.Reflection.ParameterModifier[] modifiers) { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(true)]
        public override System.Reflection.ConstructorInfo[] GetConstructors(System.Reflection.BindingFlags bindingAttr) { throw null; }
        public override object[] GetCustomAttributes(bool inherit) { throw null; }
        public override object[] GetCustomAttributes(System.Type attributeType, bool inherit) { throw null; }
        public override System.Type GetElementType() { throw null; }
        public override System.Reflection.EventInfo GetEvent(string name, System.Reflection.BindingFlags bindingAttr) { throw null; }
        public override System.Reflection.EventInfo[] GetEvents() { throw null; }
        public override System.Reflection.EventInfo[] GetEvents(System.Reflection.BindingFlags bindingAttr) { throw null; }
        public override System.Reflection.FieldInfo GetField(string name, System.Reflection.BindingFlags bindingAttr) { throw null; }
        public override System.Reflection.FieldInfo[] GetFields(System.Reflection.BindingFlags bindingAttr) { throw null; }
        public override System.Type[] GetGenericArguments() { throw null; }
        public override System.Type GetGenericTypeDefinition() { throw null; }
        public override int GetHashCode() { throw null; }
        public override System.Type GetInterface(string name, bool ignoreCase) { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(true)]
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
        public override bool IsAssignableFrom(System.Type c) { throw null; }
        protected override bool IsByRefImpl() { throw null; }
        protected override bool IsCOMObjectImpl() { throw null; }
        public override bool IsDefined(System.Type attributeType, bool inherit) { throw null; }
        protected override bool IsPointerImpl() { throw null; }
        protected override bool IsPrimitiveImpl() { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(true)]
        public override bool IsSubclassOf(System.Type c) { throw null; }
        protected override bool IsValueTypeImpl() { throw null; }
        public override System.Type MakeArrayType() { throw null; }
        public override System.Type MakeArrayType(int rank) { throw null; }
        public override System.Type MakeByRefType() { throw null; }
        public override System.Type MakeGenericType(params System.Type[] typeArguments) { throw null; }
        public override System.Type MakePointerType() { throw null; }
        public void SetBaseTypeConstraint(System.Type baseTypeConstraint) { }
        [System.Security.SecurityCriticalAttribute]
        public void SetCustomAttribute(System.Reflection.ConstructorInfo con, byte[] binaryAttribute) { }
        public void SetCustomAttribute(System.Reflection.Emit.CustomAttributeBuilder customBuilder) { }
        public void SetGenericParameterAttributes(System.Reflection.GenericParameterAttributes genericParameterAttributes) { }
        [System.Runtime.InteropServices.ComVisibleAttribute(true)]
        public void SetInterfaceConstraints(params System.Type[] interfaceConstraints) { }
        public override string ToString() { throw null; }
    }
    [System.Runtime.InteropServices.ClassInterfaceAttribute((System.Runtime.InteropServices.ClassInterfaceType)(0))]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class ILGenerator
    {
        internal ILGenerator() { }
        public virtual int ILOffset { get { throw null; } }
        public virtual void BeginCatchBlock(System.Type exceptionType) { }
        public virtual void BeginExceptFilterBlock() { }
        public virtual System.Reflection.Emit.Label BeginExceptionBlock() { throw null; }
        public virtual void BeginFaultBlock() { }
        public virtual void BeginFinallyBlock() { }
        public virtual void BeginScope() { }
        public virtual System.Reflection.Emit.LocalBuilder DeclareLocal(System.Type localType) { throw null; }
        public virtual System.Reflection.Emit.LocalBuilder DeclareLocal(System.Type localType, bool pinned) { throw null; }
        public virtual System.Reflection.Emit.Label DefineLabel() { throw null; }
        public virtual void Emit(System.Reflection.Emit.OpCode opcode) { }
        public virtual void Emit(System.Reflection.Emit.OpCode opcode, byte arg) { }
        [System.Security.SecuritySafeCriticalAttribute]
        public virtual void Emit(System.Reflection.Emit.OpCode opcode, double arg) { }
        public virtual void Emit(System.Reflection.Emit.OpCode opcode, short arg) { }
        public virtual void Emit(System.Reflection.Emit.OpCode opcode, int arg) { }
        public virtual void Emit(System.Reflection.Emit.OpCode opcode, long arg) { }
        [System.Runtime.InteropServices.ComVisibleAttribute(true)]
        [System.Security.SecuritySafeCriticalAttribute]
        public virtual void Emit(System.Reflection.Emit.OpCode opcode, System.Reflection.ConstructorInfo con) { }
        public virtual void Emit(System.Reflection.Emit.OpCode opcode, System.Reflection.Emit.Label label) { }
        public virtual void Emit(System.Reflection.Emit.OpCode opcode, System.Reflection.Emit.Label[] labels) { }
        public virtual void Emit(System.Reflection.Emit.OpCode opcode, System.Reflection.Emit.LocalBuilder local) { }
        public virtual void Emit(System.Reflection.Emit.OpCode opcode, System.Reflection.Emit.SignatureHelper signature) { }
        public virtual void Emit(System.Reflection.Emit.OpCode opcode, System.Reflection.FieldInfo field) { }
        [System.Security.SecuritySafeCriticalAttribute]
        public virtual void Emit(System.Reflection.Emit.OpCode opcode, System.Reflection.MethodInfo meth) { }
        [System.CLSCompliantAttribute(false)]
        public void Emit(System.Reflection.Emit.OpCode opcode, sbyte arg) { }
        [System.Security.SecuritySafeCriticalAttribute]
        public virtual void Emit(System.Reflection.Emit.OpCode opcode, float arg) { }
        public virtual void Emit(System.Reflection.Emit.OpCode opcode, string str) { }
        [System.Security.SecuritySafeCriticalAttribute]
        public virtual void Emit(System.Reflection.Emit.OpCode opcode, System.Type cls) { }
        [System.Security.SecuritySafeCriticalAttribute]
        public virtual void EmitCall(System.Reflection.Emit.OpCode opcode, System.Reflection.MethodInfo methodInfo, System.Type[] optionalParameterTypes) { }
        [System.Security.SecuritySafeCriticalAttribute]
        public virtual void EmitCalli(System.Reflection.Emit.OpCode opcode, System.Reflection.CallingConventions callingConvention, System.Type returnType, System.Type[] parameterTypes, System.Type[] optionalParameterTypes) { }
        public virtual void EmitWriteLine(System.Reflection.Emit.LocalBuilder localBuilder) { }
        public virtual void EmitWriteLine(System.Reflection.FieldInfo fld) { }
        public virtual void EmitWriteLine(string value) { }
        public virtual void EndExceptionBlock() { }
        public virtual void EndScope() { }
        public virtual void MarkLabel(System.Reflection.Emit.Label loc) { }
        public virtual void MarkSequencePoint(System.Diagnostics.SymbolStore.ISymbolDocumentWriter document, int startLine, int startColumn, int endLine, int endColumn) { }
        public virtual void ThrowException(System.Type excType) { }
        public virtual void UsingNamespace(string usingNamespace) { }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct Label
    {
        public override bool Equals(object obj) { throw null; }
        public bool Equals(System.Reflection.Emit.Label obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Reflection.Emit.Label a, System.Reflection.Emit.Label b) { throw null; }
        public static bool operator !=(System.Reflection.Emit.Label a, System.Reflection.Emit.Label b) { throw null; }
    }
    [System.Runtime.InteropServices.ClassInterfaceAttribute((System.Runtime.InteropServices.ClassInterfaceType)(0))]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class LocalBuilder : System.Reflection.LocalVariableInfo
    {
        internal LocalBuilder() { }
        public override bool IsPinned { get { throw null; } }
        public override int LocalIndex { get { throw null; } }
        public override System.Type LocalType { get { throw null; } }
        public void SetLocalSymInfo(string name) { }
        public void SetLocalSymInfo(string name, int startOffset, int endOffset) { }
    }
    [System.Runtime.InteropServices.ClassInterfaceAttribute((System.Runtime.InteropServices.ClassInterfaceType)(0))]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class MethodBuilder : System.Reflection.MethodInfo
    {
        internal MethodBuilder() { }
        public override System.Reflection.MethodAttributes Attributes { get { throw null; } }
        public override System.Reflection.CallingConventions CallingConvention { get { throw null; } }
        public override bool ContainsGenericParameters { get { throw null; } }
        public override System.Type DeclaringType { get { throw null; } }
        public bool InitLocals { get { throw null; } set { } }
        public override bool IsGenericMethod { get { throw null; } }
        public override bool IsGenericMethodDefinition { get { throw null; } }
        public override System.RuntimeMethodHandle MethodHandle { get { throw null; } }
        public override System.Reflection.Module Module { get { throw null; } }
        public override string Name { get { throw null; } }
        public override System.Type ReflectedType { get { throw null; } }
        public override System.Reflection.ParameterInfo ReturnParameter { get { throw null; } }
        public override System.Type ReturnType { get { throw null; } }
        public override System.Reflection.ICustomAttributeProvider ReturnTypeCustomAttributes { get { throw null; } }
        public string Signature { [System.Security.SecuritySafeCriticalAttribute]get { throw null; } }
        public System.Reflection.Emit.GenericTypeParameterBuilder[] DefineGenericParameters(params string[] names) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public System.Reflection.Emit.ParameterBuilder DefineParameter(int position, System.Reflection.ParameterAttributes attributes, string strParamName) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public override bool Equals(object obj) { throw null; }
        public override System.Reflection.MethodInfo GetBaseDefinition() { throw null; }
        public override object[] GetCustomAttributes(bool inherit) { throw null; }
        public override object[] GetCustomAttributes(System.Type attributeType, bool inherit) { throw null; }
        public override System.Type[] GetGenericArguments() { throw null; }
        public override System.Reflection.MethodInfo GetGenericMethodDefinition() { throw null; }
        public override int GetHashCode() { throw null; }
        public System.Reflection.Emit.ILGenerator GetILGenerator() { throw null; }
        public System.Reflection.Emit.ILGenerator GetILGenerator(int size) { throw null; }
        public override System.Reflection.MethodImplAttributes GetMethodImplementationFlags() { throw null; }
        public System.Reflection.Module GetModule() { throw null; }
        public override System.Reflection.ParameterInfo[] GetParameters() { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public System.Reflection.Emit.MethodToken GetToken() { throw null; }
        public override object Invoke(object obj, System.Reflection.BindingFlags invokeAttr, System.Reflection.Binder binder, object[] parameters, System.Globalization.CultureInfo culture) { throw null; }
        public override bool IsDefined(System.Type attributeType, bool inherit) { throw null; }
        public override System.Reflection.MethodInfo MakeGenericMethod(params System.Type[] typeArguments) { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(true)]
        [System.Security.SecurityCriticalAttribute]
        public void SetCustomAttribute(System.Reflection.ConstructorInfo con, byte[] binaryAttribute) { }
        [System.Security.SecuritySafeCriticalAttribute]
        public void SetCustomAttribute(System.Reflection.Emit.CustomAttributeBuilder customBuilder) { }
        [System.Security.SecuritySafeCriticalAttribute]
        public void SetImplementationFlags(System.Reflection.MethodImplAttributes attributes) { }
        public void SetParameters(params System.Type[] parameterTypes) { }
        public void SetReturnType(System.Type returnType) { }
        public void SetSignature(System.Type returnType, System.Type[] returnTypeRequiredCustomModifiers, System.Type[] returnTypeOptionalCustomModifiers, System.Type[] parameterTypes, System.Type[][] parameterTypeRequiredCustomModifiers, System.Type[][] parameterTypeOptionalCustomModifiers) { }
        [System.Security.SecuritySafeCriticalAttribute]
        public override string ToString() { throw null; }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct MethodToken
    {
        public static readonly System.Reflection.Emit.MethodToken Empty;
        public int Token { get { throw null; } }
        public override bool Equals(object obj) { throw null; }
        public bool Equals(System.Reflection.Emit.MethodToken obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Reflection.Emit.MethodToken a, System.Reflection.Emit.MethodToken b) { throw null; }
        public static bool operator !=(System.Reflection.Emit.MethodToken a, System.Reflection.Emit.MethodToken b) { throw null; }
    }
    [System.Runtime.InteropServices.ClassInterfaceAttribute((System.Runtime.InteropServices.ClassInterfaceType)(0))]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class ModuleBuilder : System.Reflection.Module
    {
        internal ModuleBuilder() { }
        public override System.Reflection.Assembly Assembly { get { throw null; } }
        public override string FullyQualifiedName { [System.Security.SecurityCriticalAttribute]get { throw null; } }
        public override int MetadataToken { get { throw null; } }
        public override System.Guid ModuleVersionId { get { throw null; } }
        public override string Name { get { throw null; } }
        public override string ScopeName { get { throw null; } }
        public void CreateGlobalFunctions() { }
        [System.Security.SecuritySafeCriticalAttribute]
        public System.Diagnostics.SymbolStore.ISymbolDocumentWriter DefineDocument(string url, System.Guid language, System.Guid languageVendor, System.Guid documentType) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public System.Reflection.Emit.EnumBuilder DefineEnum(string name, System.Reflection.TypeAttributes visibility, System.Type underlyingType) { throw null; }
        public System.Reflection.Emit.MethodBuilder DefineGlobalMethod(string name, System.Reflection.MethodAttributes attributes, System.Reflection.CallingConventions callingConvention, System.Type returnType, System.Type[] parameterTypes) { throw null; }
        public System.Reflection.Emit.MethodBuilder DefineGlobalMethod(string name, System.Reflection.MethodAttributes attributes, System.Reflection.CallingConventions callingConvention, System.Type returnType, System.Type[] requiredReturnTypeCustomModifiers, System.Type[] optionalReturnTypeCustomModifiers, System.Type[] parameterTypes, System.Type[][] requiredParameterTypeCustomModifiers, System.Type[][] optionalParameterTypeCustomModifiers) { throw null; }
        public System.Reflection.Emit.MethodBuilder DefineGlobalMethod(string name, System.Reflection.MethodAttributes attributes, System.Type returnType, System.Type[] parameterTypes) { throw null; }
        [System.Security.SecurityCriticalAttribute]
        public System.Reflection.Emit.FieldBuilder DefineInitializedData(string name, byte[] data, System.Reflection.FieldAttributes attributes) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public System.Reflection.Emit.TypeBuilder DefineType(string name) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public System.Reflection.Emit.TypeBuilder DefineType(string name, System.Reflection.TypeAttributes attr) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public System.Reflection.Emit.TypeBuilder DefineType(string name, System.Reflection.TypeAttributes attr, System.Type parent) { throw null; }
        [System.Security.SecurityCriticalAttribute]
        public System.Reflection.Emit.TypeBuilder DefineType(string name, System.Reflection.TypeAttributes attr, System.Type parent, int typesize) { throw null; }
        [System.Security.SecurityCriticalAttribute]
        public System.Reflection.Emit.TypeBuilder DefineType(string name, System.Reflection.TypeAttributes attr, System.Type parent, System.Reflection.Emit.PackingSize packsize) { throw null; }
        [System.Security.SecurityCriticalAttribute]
        public System.Reflection.Emit.TypeBuilder DefineType(string name, System.Reflection.TypeAttributes attr, System.Type parent, System.Reflection.Emit.PackingSize packingSize, int typesize) { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(true)]
        [System.Security.SecuritySafeCriticalAttribute]
        public System.Reflection.Emit.TypeBuilder DefineType(string name, System.Reflection.TypeAttributes attr, System.Type parent, System.Type[] interfaces) { throw null; }
        [System.Security.SecurityCriticalAttribute]
        public System.Reflection.Emit.FieldBuilder DefineUninitializedData(string name, int size, System.Reflection.FieldAttributes attributes) { throw null; }
        public override bool Equals(object obj) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public System.Reflection.MethodInfo GetArrayMethod(System.Type arrayClass, string methodName, System.Reflection.CallingConventions callingConvention, System.Type returnType, System.Type[] parameterTypes) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public System.Reflection.Emit.MethodToken GetArrayMethodToken(System.Type arrayClass, string methodName, System.Reflection.CallingConventions callingConvention, System.Type returnType, System.Type[] parameterTypes) { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(true)]
        [System.Security.SecuritySafeCriticalAttribute]
        public System.Reflection.Emit.MethodToken GetConstructorToken(System.Reflection.ConstructorInfo con) { throw null; }
        public override object[] GetCustomAttributes(bool inherit) { throw null; }
        public override object[] GetCustomAttributes(System.Type attributeType, bool inherit) { throw null; }
        public override System.Reflection.FieldInfo GetField(string name, System.Reflection.BindingFlags bindingAttr) { throw null; }
        public override System.Reflection.FieldInfo[] GetFields(System.Reflection.BindingFlags bindingFlags) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public System.Reflection.Emit.FieldToken GetFieldToken(System.Reflection.FieldInfo field) { throw null; }
        public override int GetHashCode() { throw null; }
        protected override System.Reflection.MethodInfo GetMethodImpl(string name, System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder binder, System.Reflection.CallingConventions callConvention, System.Type[] types, System.Reflection.ParameterModifier[] modifiers) { throw null; }
        public override System.Reflection.MethodInfo[] GetMethods(System.Reflection.BindingFlags bindingFlags) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public System.Reflection.Emit.MethodToken GetMethodToken(System.Reflection.MethodInfo method) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public System.Reflection.Emit.SignatureToken GetSignatureToken(byte[] sigBytes, int sigLength) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public System.Reflection.Emit.SignatureToken GetSignatureToken(System.Reflection.Emit.SignatureHelper sigHelper) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public System.Reflection.Emit.StringToken GetStringConstant(string str) { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(true)]
        public override System.Type GetType(string className) { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(true)]
        public override System.Type GetType(string className, bool ignoreCase) { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(true)]
        public override System.Type GetType(string className, bool throwOnError, bool ignoreCase) { throw null; }
        public override System.Type[] GetTypes() { throw null; }
        public System.Reflection.Emit.TypeToken GetTypeToken(string name) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public System.Reflection.Emit.TypeToken GetTypeToken(System.Type type) { throw null; }
        public override bool IsDefined(System.Type attributeType, bool inherit) { throw null; }
        public bool IsTransient() { throw null; }
        public override System.Reflection.FieldInfo ResolveField(int metadataToken, System.Type[] genericTypeArguments, System.Type[] genericMethodArguments) { throw null; }
        public override System.Reflection.MethodBase ResolveMethod(int metadataToken, System.Type[] genericTypeArguments, System.Type[] genericMethodArguments) { throw null; }
        public override string ResolveString(int metadataToken) { throw null; }
        public override System.Type ResolveType(int metadataToken, System.Type[] genericTypeArguments, System.Type[] genericMethodArguments) { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(true)]
        [System.Security.SecurityCriticalAttribute]
        public void SetCustomAttribute(System.Reflection.ConstructorInfo con, byte[] binaryAttribute) { }
        [System.Security.SecuritySafeCriticalAttribute]
        public void SetCustomAttribute(System.Reflection.Emit.CustomAttributeBuilder customBuilder) { }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct OpCode
    {
        public System.Reflection.Emit.FlowControl FlowControl { get { throw null; } }
        public string Name { get { throw null; } }
        public System.Reflection.Emit.OpCodeType OpCodeType { get { throw null; } }
        public System.Reflection.Emit.OperandType OperandType { get { throw null; } }
        public int Size { get { throw null; } }
        public System.Reflection.Emit.StackBehaviour StackBehaviourPop { get { throw null; } }
        public System.Reflection.Emit.StackBehaviour StackBehaviourPush { get { throw null; } }
        public short Value { get { throw null; } }
        public override bool Equals(object obj) { throw null; }
        public bool Equals(System.Reflection.Emit.OpCode obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Reflection.Emit.OpCode a, System.Reflection.Emit.OpCode b) { throw null; }
        public static bool operator !=(System.Reflection.Emit.OpCode a, System.Reflection.Emit.OpCode b) { throw null; }
        public override string ToString() { throw null; }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class OpCodes
    {
        internal OpCodes() { }
        public static readonly System.Reflection.Emit.OpCode Add;
        public static readonly System.Reflection.Emit.OpCode Add_Ovf;
        public static readonly System.Reflection.Emit.OpCode Add_Ovf_Un;
        public static readonly System.Reflection.Emit.OpCode And;
        public static readonly System.Reflection.Emit.OpCode Arglist;
        public static readonly System.Reflection.Emit.OpCode Beq;
        public static readonly System.Reflection.Emit.OpCode Beq_S;
        public static readonly System.Reflection.Emit.OpCode Bge;
        public static readonly System.Reflection.Emit.OpCode Bge_S;
        public static readonly System.Reflection.Emit.OpCode Bge_Un;
        public static readonly System.Reflection.Emit.OpCode Bge_Un_S;
        public static readonly System.Reflection.Emit.OpCode Bgt;
        public static readonly System.Reflection.Emit.OpCode Bgt_S;
        public static readonly System.Reflection.Emit.OpCode Bgt_Un;
        public static readonly System.Reflection.Emit.OpCode Bgt_Un_S;
        public static readonly System.Reflection.Emit.OpCode Ble;
        public static readonly System.Reflection.Emit.OpCode Ble_S;
        public static readonly System.Reflection.Emit.OpCode Ble_Un;
        public static readonly System.Reflection.Emit.OpCode Ble_Un_S;
        public static readonly System.Reflection.Emit.OpCode Blt;
        public static readonly System.Reflection.Emit.OpCode Blt_S;
        public static readonly System.Reflection.Emit.OpCode Blt_Un;
        public static readonly System.Reflection.Emit.OpCode Blt_Un_S;
        public static readonly System.Reflection.Emit.OpCode Bne_Un;
        public static readonly System.Reflection.Emit.OpCode Bne_Un_S;
        public static readonly System.Reflection.Emit.OpCode Box;
        public static readonly System.Reflection.Emit.OpCode Br;
        public static readonly System.Reflection.Emit.OpCode Br_S;
        public static readonly System.Reflection.Emit.OpCode Break;
        public static readonly System.Reflection.Emit.OpCode Brfalse;
        public static readonly System.Reflection.Emit.OpCode Brfalse_S;
        public static readonly System.Reflection.Emit.OpCode Brtrue;
        public static readonly System.Reflection.Emit.OpCode Brtrue_S;
        public static readonly System.Reflection.Emit.OpCode Call;
        public static readonly System.Reflection.Emit.OpCode Calli;
        public static readonly System.Reflection.Emit.OpCode Callvirt;
        [System.Runtime.InteropServices.ComVisibleAttribute(true)]
        public static readonly System.Reflection.Emit.OpCode Castclass;
        public static readonly System.Reflection.Emit.OpCode Ceq;
        public static readonly System.Reflection.Emit.OpCode Cgt;
        public static readonly System.Reflection.Emit.OpCode Cgt_Un;
        public static readonly System.Reflection.Emit.OpCode Ckfinite;
        public static readonly System.Reflection.Emit.OpCode Clt;
        public static readonly System.Reflection.Emit.OpCode Clt_Un;
        public static readonly System.Reflection.Emit.OpCode Constrained;
        public static readonly System.Reflection.Emit.OpCode Conv_I;
        public static readonly System.Reflection.Emit.OpCode Conv_I1;
        public static readonly System.Reflection.Emit.OpCode Conv_I2;
        public static readonly System.Reflection.Emit.OpCode Conv_I4;
        public static readonly System.Reflection.Emit.OpCode Conv_I8;
        public static readonly System.Reflection.Emit.OpCode Conv_Ovf_I;
        public static readonly System.Reflection.Emit.OpCode Conv_Ovf_I_Un;
        public static readonly System.Reflection.Emit.OpCode Conv_Ovf_I1;
        public static readonly System.Reflection.Emit.OpCode Conv_Ovf_I1_Un;
        public static readonly System.Reflection.Emit.OpCode Conv_Ovf_I2;
        public static readonly System.Reflection.Emit.OpCode Conv_Ovf_I2_Un;
        public static readonly System.Reflection.Emit.OpCode Conv_Ovf_I4;
        public static readonly System.Reflection.Emit.OpCode Conv_Ovf_I4_Un;
        public static readonly System.Reflection.Emit.OpCode Conv_Ovf_I8;
        public static readonly System.Reflection.Emit.OpCode Conv_Ovf_I8_Un;
        public static readonly System.Reflection.Emit.OpCode Conv_Ovf_U;
        public static readonly System.Reflection.Emit.OpCode Conv_Ovf_U_Un;
        public static readonly System.Reflection.Emit.OpCode Conv_Ovf_U1;
        public static readonly System.Reflection.Emit.OpCode Conv_Ovf_U1_Un;
        public static readonly System.Reflection.Emit.OpCode Conv_Ovf_U2;
        public static readonly System.Reflection.Emit.OpCode Conv_Ovf_U2_Un;
        public static readonly System.Reflection.Emit.OpCode Conv_Ovf_U4;
        public static readonly System.Reflection.Emit.OpCode Conv_Ovf_U4_Un;
        public static readonly System.Reflection.Emit.OpCode Conv_Ovf_U8;
        public static readonly System.Reflection.Emit.OpCode Conv_Ovf_U8_Un;
        public static readonly System.Reflection.Emit.OpCode Conv_R_Un;
        public static readonly System.Reflection.Emit.OpCode Conv_R4;
        public static readonly System.Reflection.Emit.OpCode Conv_R8;
        public static readonly System.Reflection.Emit.OpCode Conv_U;
        public static readonly System.Reflection.Emit.OpCode Conv_U1;
        public static readonly System.Reflection.Emit.OpCode Conv_U2;
        public static readonly System.Reflection.Emit.OpCode Conv_U4;
        public static readonly System.Reflection.Emit.OpCode Conv_U8;
        public static readonly System.Reflection.Emit.OpCode Cpblk;
        public static readonly System.Reflection.Emit.OpCode Cpobj;
        public static readonly System.Reflection.Emit.OpCode Div;
        public static readonly System.Reflection.Emit.OpCode Div_Un;
        public static readonly System.Reflection.Emit.OpCode Dup;
        public static readonly System.Reflection.Emit.OpCode Endfilter;
        public static readonly System.Reflection.Emit.OpCode Endfinally;
        public static readonly System.Reflection.Emit.OpCode Initblk;
        public static readonly System.Reflection.Emit.OpCode Initobj;
        public static readonly System.Reflection.Emit.OpCode Isinst;
        public static readonly System.Reflection.Emit.OpCode Jmp;
        public static readonly System.Reflection.Emit.OpCode Ldarg;
        public static readonly System.Reflection.Emit.OpCode Ldarg_0;
        public static readonly System.Reflection.Emit.OpCode Ldarg_1;
        public static readonly System.Reflection.Emit.OpCode Ldarg_2;
        public static readonly System.Reflection.Emit.OpCode Ldarg_3;
        public static readonly System.Reflection.Emit.OpCode Ldarg_S;
        public static readonly System.Reflection.Emit.OpCode Ldarga;
        public static readonly System.Reflection.Emit.OpCode Ldarga_S;
        public static readonly System.Reflection.Emit.OpCode Ldc_I4;
        public static readonly System.Reflection.Emit.OpCode Ldc_I4_0;
        public static readonly System.Reflection.Emit.OpCode Ldc_I4_1;
        public static readonly System.Reflection.Emit.OpCode Ldc_I4_2;
        public static readonly System.Reflection.Emit.OpCode Ldc_I4_3;
        public static readonly System.Reflection.Emit.OpCode Ldc_I4_4;
        public static readonly System.Reflection.Emit.OpCode Ldc_I4_5;
        public static readonly System.Reflection.Emit.OpCode Ldc_I4_6;
        public static readonly System.Reflection.Emit.OpCode Ldc_I4_7;
        public static readonly System.Reflection.Emit.OpCode Ldc_I4_8;
        public static readonly System.Reflection.Emit.OpCode Ldc_I4_M1;
        public static readonly System.Reflection.Emit.OpCode Ldc_I4_S;
        public static readonly System.Reflection.Emit.OpCode Ldc_I8;
        public static readonly System.Reflection.Emit.OpCode Ldc_R4;
        public static readonly System.Reflection.Emit.OpCode Ldc_R8;
        public static readonly System.Reflection.Emit.OpCode Ldelem;
        public static readonly System.Reflection.Emit.OpCode Ldelem_I;
        public static readonly System.Reflection.Emit.OpCode Ldelem_I1;
        public static readonly System.Reflection.Emit.OpCode Ldelem_I2;
        public static readonly System.Reflection.Emit.OpCode Ldelem_I4;
        public static readonly System.Reflection.Emit.OpCode Ldelem_I8;
        public static readonly System.Reflection.Emit.OpCode Ldelem_R4;
        public static readonly System.Reflection.Emit.OpCode Ldelem_R8;
        public static readonly System.Reflection.Emit.OpCode Ldelem_Ref;
        public static readonly System.Reflection.Emit.OpCode Ldelem_U1;
        public static readonly System.Reflection.Emit.OpCode Ldelem_U2;
        public static readonly System.Reflection.Emit.OpCode Ldelem_U4;
        public static readonly System.Reflection.Emit.OpCode Ldelema;
        public static readonly System.Reflection.Emit.OpCode Ldfld;
        public static readonly System.Reflection.Emit.OpCode Ldflda;
        public static readonly System.Reflection.Emit.OpCode Ldftn;
        public static readonly System.Reflection.Emit.OpCode Ldind_I;
        public static readonly System.Reflection.Emit.OpCode Ldind_I1;
        public static readonly System.Reflection.Emit.OpCode Ldind_I2;
        public static readonly System.Reflection.Emit.OpCode Ldind_I4;
        public static readonly System.Reflection.Emit.OpCode Ldind_I8;
        public static readonly System.Reflection.Emit.OpCode Ldind_R4;
        public static readonly System.Reflection.Emit.OpCode Ldind_R8;
        public static readonly System.Reflection.Emit.OpCode Ldind_Ref;
        public static readonly System.Reflection.Emit.OpCode Ldind_U1;
        public static readonly System.Reflection.Emit.OpCode Ldind_U2;
        public static readonly System.Reflection.Emit.OpCode Ldind_U4;
        public static readonly System.Reflection.Emit.OpCode Ldlen;
        public static readonly System.Reflection.Emit.OpCode Ldloc;
        public static readonly System.Reflection.Emit.OpCode Ldloc_0;
        public static readonly System.Reflection.Emit.OpCode Ldloc_1;
        public static readonly System.Reflection.Emit.OpCode Ldloc_2;
        public static readonly System.Reflection.Emit.OpCode Ldloc_3;
        public static readonly System.Reflection.Emit.OpCode Ldloc_S;
        public static readonly System.Reflection.Emit.OpCode Ldloca;
        public static readonly System.Reflection.Emit.OpCode Ldloca_S;
        public static readonly System.Reflection.Emit.OpCode Ldnull;
        public static readonly System.Reflection.Emit.OpCode Ldobj;
        public static readonly System.Reflection.Emit.OpCode Ldsfld;
        public static readonly System.Reflection.Emit.OpCode Ldsflda;
        public static readonly System.Reflection.Emit.OpCode Ldstr;
        public static readonly System.Reflection.Emit.OpCode Ldtoken;
        public static readonly System.Reflection.Emit.OpCode Ldvirtftn;
        public static readonly System.Reflection.Emit.OpCode Leave;
        public static readonly System.Reflection.Emit.OpCode Leave_S;
        public static readonly System.Reflection.Emit.OpCode Localloc;
        public static readonly System.Reflection.Emit.OpCode Mkrefany;
        public static readonly System.Reflection.Emit.OpCode Mul;
        public static readonly System.Reflection.Emit.OpCode Mul_Ovf;
        public static readonly System.Reflection.Emit.OpCode Mul_Ovf_Un;
        public static readonly System.Reflection.Emit.OpCode Neg;
        public static readonly System.Reflection.Emit.OpCode Newarr;
        public static readonly System.Reflection.Emit.OpCode Newobj;
        public static readonly System.Reflection.Emit.OpCode Nop;
        public static readonly System.Reflection.Emit.OpCode Not;
        public static readonly System.Reflection.Emit.OpCode Or;
        public static readonly System.Reflection.Emit.OpCode Pop;
        public static readonly System.Reflection.Emit.OpCode Prefix1;
        public static readonly System.Reflection.Emit.OpCode Prefix2;
        public static readonly System.Reflection.Emit.OpCode Prefix3;
        public static readonly System.Reflection.Emit.OpCode Prefix4;
        public static readonly System.Reflection.Emit.OpCode Prefix5;
        public static readonly System.Reflection.Emit.OpCode Prefix6;
        public static readonly System.Reflection.Emit.OpCode Prefix7;
        public static readonly System.Reflection.Emit.OpCode Prefixref;
        public static readonly System.Reflection.Emit.OpCode Readonly;
        public static readonly System.Reflection.Emit.OpCode Refanytype;
        public static readonly System.Reflection.Emit.OpCode Refanyval;
        public static readonly System.Reflection.Emit.OpCode Rem;
        public static readonly System.Reflection.Emit.OpCode Rem_Un;
        public static readonly System.Reflection.Emit.OpCode Ret;
        public static readonly System.Reflection.Emit.OpCode Rethrow;
        public static readonly System.Reflection.Emit.OpCode Shl;
        public static readonly System.Reflection.Emit.OpCode Shr;
        public static readonly System.Reflection.Emit.OpCode Shr_Un;
        public static readonly System.Reflection.Emit.OpCode Sizeof;
        public static readonly System.Reflection.Emit.OpCode Starg;
        public static readonly System.Reflection.Emit.OpCode Starg_S;
        public static readonly System.Reflection.Emit.OpCode Stelem;
        public static readonly System.Reflection.Emit.OpCode Stelem_I;
        public static readonly System.Reflection.Emit.OpCode Stelem_I1;
        public static readonly System.Reflection.Emit.OpCode Stelem_I2;
        public static readonly System.Reflection.Emit.OpCode Stelem_I4;
        public static readonly System.Reflection.Emit.OpCode Stelem_I8;
        public static readonly System.Reflection.Emit.OpCode Stelem_R4;
        public static readonly System.Reflection.Emit.OpCode Stelem_R8;
        public static readonly System.Reflection.Emit.OpCode Stelem_Ref;
        public static readonly System.Reflection.Emit.OpCode Stfld;
        public static readonly System.Reflection.Emit.OpCode Stind_I;
        public static readonly System.Reflection.Emit.OpCode Stind_I1;
        public static readonly System.Reflection.Emit.OpCode Stind_I2;
        public static readonly System.Reflection.Emit.OpCode Stind_I4;
        public static readonly System.Reflection.Emit.OpCode Stind_I8;
        public static readonly System.Reflection.Emit.OpCode Stind_R4;
        public static readonly System.Reflection.Emit.OpCode Stind_R8;
        public static readonly System.Reflection.Emit.OpCode Stind_Ref;
        public static readonly System.Reflection.Emit.OpCode Stloc;
        public static readonly System.Reflection.Emit.OpCode Stloc_0;
        public static readonly System.Reflection.Emit.OpCode Stloc_1;
        public static readonly System.Reflection.Emit.OpCode Stloc_2;
        public static readonly System.Reflection.Emit.OpCode Stloc_3;
        public static readonly System.Reflection.Emit.OpCode Stloc_S;
        public static readonly System.Reflection.Emit.OpCode Stobj;
        public static readonly System.Reflection.Emit.OpCode Stsfld;
        public static readonly System.Reflection.Emit.OpCode Sub;
        public static readonly System.Reflection.Emit.OpCode Sub_Ovf;
        public static readonly System.Reflection.Emit.OpCode Sub_Ovf_Un;
        public static readonly System.Reflection.Emit.OpCode Switch;
        public static readonly System.Reflection.Emit.OpCode Tailcall;
        public static readonly System.Reflection.Emit.OpCode Throw;
        public static readonly System.Reflection.Emit.OpCode Unaligned;
        public static readonly System.Reflection.Emit.OpCode Unbox;
        public static readonly System.Reflection.Emit.OpCode Unbox_Any;
        public static readonly System.Reflection.Emit.OpCode Volatile;
        public static readonly System.Reflection.Emit.OpCode Xor;
        public static bool TakesSingleByteArgument(System.Reflection.Emit.OpCode inst) { throw null; }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public enum OpCodeType
    {
        Macro = 1,
        Nternal = 2,
        Objmodel = 3,
        Prefix = 4,
        Primitive = 5,
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public enum OperandType
    {
        InlineBrTarget = 0,
        InlineField = 1,
        InlineI = 2,
        InlineI8 = 3,
        InlineMethod = 4,
        InlineNone = 5,
        InlineR = 7,
        InlineSig = 9,
        InlineString = 10,
        InlineSwitch = 11,
        InlineTok = 12,
        InlineType = 13,
        InlineVar = 14,
        ShortInlineBrTarget = 15,
        ShortInlineI = 16,
        ShortInlineR = 17,
        ShortInlineVar = 18,
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public enum PackingSize
    {
        Size1 = 1,
        Size128 = 128,
        Size16 = 16,
        Size2 = 2,
        Size32 = 32,
        Size4 = 4,
        Size64 = 64,
        Size8 = 8,
        Unspecified = 0,
    }
    [System.Runtime.InteropServices.ClassInterfaceAttribute((System.Runtime.InteropServices.ClassInterfaceType)(0))]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class ParameterBuilder
    {
        internal ParameterBuilder() { }
        public virtual int Attributes { get { throw null; } }
        public bool IsIn { get { throw null; } }
        public bool IsOptional { get { throw null; } }
        public bool IsOut { get { throw null; } }
        public virtual string Name { get { throw null; } }
        public virtual int Position { get { throw null; } }
        public virtual System.Reflection.Emit.ParameterToken GetToken() { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public virtual void SetConstant(object defaultValue) { }
        [System.Runtime.InteropServices.ComVisibleAttribute(true)]
        [System.Security.SecuritySafeCriticalAttribute]
        public void SetCustomAttribute(System.Reflection.ConstructorInfo con, byte[] binaryAttribute) { }
        [System.Security.SecuritySafeCriticalAttribute]
        public void SetCustomAttribute(System.Reflection.Emit.CustomAttributeBuilder customBuilder) { }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct ParameterToken
    {
        public static readonly System.Reflection.Emit.ParameterToken Empty;
        public int Token { get { throw null; } }
        public override bool Equals(object obj) { throw null; }
        public bool Equals(System.Reflection.Emit.ParameterToken obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Reflection.Emit.ParameterToken a, System.Reflection.Emit.ParameterToken b) { throw null; }
        public static bool operator !=(System.Reflection.Emit.ParameterToken a, System.Reflection.Emit.ParameterToken b) { throw null; }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public enum PEFileKinds
    {
        ConsoleApplication = 2,
        Dll = 1,
        WindowApplication = 3,
    }
    [System.Runtime.InteropServices.ClassInterfaceAttribute((System.Runtime.InteropServices.ClassInterfaceType)(0))]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class PropertyBuilder : System.Reflection.PropertyInfo
    {
        internal PropertyBuilder() { }
        public override System.Reflection.PropertyAttributes Attributes { get { throw null; } }
        public override bool CanRead { get { throw null; } }
        public override bool CanWrite { get { throw null; } }
        public override System.Type DeclaringType { get { throw null; } }
        public override System.Reflection.Module Module { get { throw null; } }
        public override string Name { get { throw null; } }
        public System.Reflection.Emit.PropertyToken PropertyToken { get { throw null; } }
        public override System.Type PropertyType { get { throw null; } }
        public override System.Type ReflectedType { get { throw null; } }
        [System.Security.SecuritySafeCriticalAttribute]
        public void AddOtherMethod(System.Reflection.Emit.MethodBuilder mdBuilder) { }
        public override System.Reflection.MethodInfo[] GetAccessors(bool nonPublic) { throw null; }
        public override object[] GetCustomAttributes(bool inherit) { throw null; }
        public override object[] GetCustomAttributes(System.Type attributeType, bool inherit) { throw null; }
        public override System.Reflection.MethodInfo GetGetMethod(bool nonPublic) { throw null; }
        public override System.Reflection.ParameterInfo[] GetIndexParameters() { throw null; }
        public override System.Reflection.MethodInfo GetSetMethod(bool nonPublic) { throw null; }
        public override object GetValue(object obj, object[] index) { throw null; }
        public override object GetValue(object obj, System.Reflection.BindingFlags invokeAttr, System.Reflection.Binder binder, object[] index, System.Globalization.CultureInfo culture) { throw null; }
        public override bool IsDefined(System.Type attributeType, bool inherit) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public void SetConstant(object defaultValue) { }
        [System.Runtime.InteropServices.ComVisibleAttribute(true)]
        [System.Security.SecurityCriticalAttribute]
        public void SetCustomAttribute(System.Reflection.ConstructorInfo con, byte[] binaryAttribute) { }
        [System.Security.SecuritySafeCriticalAttribute]
        public void SetCustomAttribute(System.Reflection.Emit.CustomAttributeBuilder customBuilder) { }
        [System.Security.SecuritySafeCriticalAttribute]
        public void SetGetMethod(System.Reflection.Emit.MethodBuilder mdBuilder) { }
        [System.Security.SecuritySafeCriticalAttribute]
        public void SetSetMethod(System.Reflection.Emit.MethodBuilder mdBuilder) { }
        public override void SetValue(object obj, object value, object[] index) { }
        public override void SetValue(object obj, object value, System.Reflection.BindingFlags invokeAttr, System.Reflection.Binder binder, object[] index, System.Globalization.CultureInfo culture) { }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct PropertyToken
    {
        public static readonly System.Reflection.Emit.PropertyToken Empty;
        public int Token { get { throw null; } }
        public override bool Equals(object obj) { throw null; }
        public bool Equals(System.Reflection.Emit.PropertyToken obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Reflection.Emit.PropertyToken a, System.Reflection.Emit.PropertyToken b) { throw null; }
        public static bool operator !=(System.Reflection.Emit.PropertyToken a, System.Reflection.Emit.PropertyToken b) { throw null; }
    }
    [System.Runtime.InteropServices.ClassInterfaceAttribute((System.Runtime.InteropServices.ClassInterfaceType)(0))]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class SignatureHelper
    {
        internal SignatureHelper() { }
        public void AddArgument(System.Type clsArgument) { }
        [System.Security.SecuritySafeCriticalAttribute]
        public void AddArgument(System.Type argument, bool pinned) { }
        [System.Security.SecuritySafeCriticalAttribute]
        public void AddArgument(System.Type argument, System.Type[] requiredCustomModifiers, System.Type[] optionalCustomModifiers) { }
        public void AddArguments(System.Type[] arguments, System.Type[][] requiredCustomModifiers, System.Type[][] optionalCustomModifiers) { }
        public void AddSentinel() { }
        public override bool Equals(object obj) { throw null; }
        public static System.Reflection.Emit.SignatureHelper GetFieldSigHelper(System.Reflection.Module mod) { throw null; }
        public override int GetHashCode() { throw null; }
        public static System.Reflection.Emit.SignatureHelper GetLocalVarSigHelper() { throw null; }
        public static System.Reflection.Emit.SignatureHelper GetLocalVarSigHelper(System.Reflection.Module mod) { throw null; }
        public static System.Reflection.Emit.SignatureHelper GetMethodSigHelper(System.Reflection.CallingConventions callingConvention, System.Type returnType) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public static System.Reflection.Emit.SignatureHelper GetMethodSigHelper(System.Reflection.Module mod, System.Reflection.CallingConventions callingConvention, System.Type returnType) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public static System.Reflection.Emit.SignatureHelper GetMethodSigHelper(System.Reflection.Module mod, System.Runtime.InteropServices.CallingConvention unmanagedCallConv, System.Type returnType) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public static System.Reflection.Emit.SignatureHelper GetMethodSigHelper(System.Reflection.Module mod, System.Type returnType, System.Type[] parameterTypes) { throw null; }
        public static System.Reflection.Emit.SignatureHelper GetMethodSigHelper(System.Runtime.InteropServices.CallingConvention unmanagedCallingConvention, System.Type returnType) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public static System.Reflection.Emit.SignatureHelper GetPropertySigHelper(System.Reflection.Module mod, System.Reflection.CallingConventions callingConvention, System.Type returnType, System.Type[] requiredReturnTypeCustomModifiers, System.Type[] optionalReturnTypeCustomModifiers, System.Type[] parameterTypes, System.Type[][] requiredParameterTypeCustomModifiers, System.Type[][] optionalParameterTypeCustomModifiers) { throw null; }
        public static System.Reflection.Emit.SignatureHelper GetPropertySigHelper(System.Reflection.Module mod, System.Type returnType, System.Type[] parameterTypes) { throw null; }
        public static System.Reflection.Emit.SignatureHelper GetPropertySigHelper(System.Reflection.Module mod, System.Type returnType, System.Type[] requiredReturnTypeCustomModifiers, System.Type[] optionalReturnTypeCustomModifiers, System.Type[] parameterTypes, System.Type[][] requiredParameterTypeCustomModifiers, System.Type[][] optionalParameterTypeCustomModifiers) { throw null; }
        public byte[] GetSignature() { throw null; }
        public override string ToString() { throw null; }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct SignatureToken
    {
        public static readonly System.Reflection.Emit.SignatureToken Empty;
        public int Token { get { throw null; } }
        public override bool Equals(object obj) { throw null; }
        public bool Equals(System.Reflection.Emit.SignatureToken obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Reflection.Emit.SignatureToken a, System.Reflection.Emit.SignatureToken b) { throw null; }
        public static bool operator !=(System.Reflection.Emit.SignatureToken a, System.Reflection.Emit.SignatureToken b) { throw null; }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public enum StackBehaviour
    {
        Pop0 = 0,
        Pop1 = 1,
        Pop1_pop1 = 2,
        Popi = 3,
        Popi_pop1 = 4,
        Popi_popi = 5,
        Popi_popi_popi = 7,
        Popi_popi8 = 6,
        Popi_popr4 = 8,
        Popi_popr8 = 9,
        Popref = 10,
        Popref_pop1 = 11,
        Popref_popi = 12,
        Popref_popi_pop1 = 28,
        Popref_popi_popi = 13,
        Popref_popi_popi8 = 14,
        Popref_popi_popr4 = 15,
        Popref_popi_popr8 = 16,
        Popref_popi_popref = 17,
        Push0 = 18,
        Push1 = 19,
        Push1_push1 = 20,
        Pushi = 21,
        Pushi8 = 22,
        Pushr4 = 23,
        Pushr8 = 24,
        Pushref = 25,
        Varpop = 26,
        Varpush = 27,
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct StringToken
    {
        public int Token { get { throw null; } }
        public override bool Equals(object obj) { throw null; }
        public bool Equals(System.Reflection.Emit.StringToken obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Reflection.Emit.StringToken a, System.Reflection.Emit.StringToken b) { throw null; }
        public static bool operator !=(System.Reflection.Emit.StringToken a, System.Reflection.Emit.StringToken b) { throw null; }
    }
    [System.Runtime.InteropServices.ClassInterfaceAttribute((System.Runtime.InteropServices.ClassInterfaceType)(0))]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class TypeBuilder : System.Reflection.TypeInfo
    {
        internal TypeBuilder() { }
        public const int UnspecifiedTypeSize = 0;
        public override System.Reflection.Assembly Assembly { get { throw null; } }
        public override string AssemblyQualifiedName { get { throw null; } }
        public override System.Type BaseType { get { throw null; } }
        public override System.Reflection.MethodBase DeclaringMethod { get { throw null; } }
        public override System.Type DeclaringType { get { throw null; } }
        public override string FullName { get { throw null; } }
        public override System.Reflection.GenericParameterAttributes GenericParameterAttributes { get { throw null; } }
        public override int GenericParameterPosition { get { throw null; } }
        public override System.Guid GUID { get { throw null; } }
        public override bool IsConstructedGenericType { get { throw null; } }
        public override bool IsGenericParameter { get { throw null; } }
        public override bool IsGenericType { get { throw null; } }
        public override bool IsGenericTypeDefinition { get { throw null; } }
        public override System.Reflection.Module Module { get { throw null; } }
        public override string Name { get { throw null; } }
        public override string Namespace { get { throw null; } }
        public System.Reflection.Emit.PackingSize PackingSize { get { throw null; } }
        public override System.Type ReflectedType { get { throw null; } }
        public int Size { get { throw null; } }
        public override System.RuntimeTypeHandle TypeHandle { get { throw null; } }
        public System.Reflection.Emit.TypeToken TypeToken { get { throw null; } }
        public override System.Type UnderlyingSystemType { get { throw null; } }
        [System.Runtime.InteropServices.ComVisibleAttribute(true)]
        [System.Security.SecuritySafeCriticalAttribute]
        public void AddInterfaceImplementation(System.Type interfaceType) { }
        [System.Security.SecuritySafeCriticalAttribute]
        public System.Type CreateType() { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public System.Reflection.TypeInfo CreateTypeInfo() { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(true)]
        public System.Reflection.Emit.ConstructorBuilder DefineConstructor(System.Reflection.MethodAttributes attributes, System.Reflection.CallingConventions callingConvention, System.Type[] parameterTypes) { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(true)]
        [System.Security.SecuritySafeCriticalAttribute]
        public System.Reflection.Emit.ConstructorBuilder DefineConstructor(System.Reflection.MethodAttributes attributes, System.Reflection.CallingConventions callingConvention, System.Type[] parameterTypes, System.Type[][] requiredCustomModifiers, System.Type[][] optionalCustomModifiers) { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(true)]
        public System.Reflection.Emit.ConstructorBuilder DefineDefaultConstructor(System.Reflection.MethodAttributes attributes) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public System.Reflection.Emit.EventBuilder DefineEvent(string name, System.Reflection.EventAttributes attributes, System.Type eventtype) { throw null; }
        public System.Reflection.Emit.FieldBuilder DefineField(string fieldName, System.Type type, System.Reflection.FieldAttributes attributes) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public System.Reflection.Emit.FieldBuilder DefineField(string fieldName, System.Type type, System.Type[] requiredCustomModifiers, System.Type[] optionalCustomModifiers, System.Reflection.FieldAttributes attributes) { throw null; }
        public System.Reflection.Emit.GenericTypeParameterBuilder[] DefineGenericParameters(params string[] names) { throw null; }
        [System.Security.SecurityCriticalAttribute]
        public System.Reflection.Emit.FieldBuilder DefineInitializedData(string name, byte[] data, System.Reflection.FieldAttributes attributes) { throw null; }
        public System.Reflection.Emit.MethodBuilder DefineMethod(string name, System.Reflection.MethodAttributes attributes) { throw null; }
        public System.Reflection.Emit.MethodBuilder DefineMethod(string name, System.Reflection.MethodAttributes attributes, System.Reflection.CallingConventions callingConvention) { throw null; }
        public System.Reflection.Emit.MethodBuilder DefineMethod(string name, System.Reflection.MethodAttributes attributes, System.Reflection.CallingConventions callingConvention, System.Type returnType, System.Type[] parameterTypes) { throw null; }
        public System.Reflection.Emit.MethodBuilder DefineMethod(string name, System.Reflection.MethodAttributes attributes, System.Reflection.CallingConventions callingConvention, System.Type returnType, System.Type[] returnTypeRequiredCustomModifiers, System.Type[] returnTypeOptionalCustomModifiers, System.Type[] parameterTypes, System.Type[][] parameterTypeRequiredCustomModifiers, System.Type[][] parameterTypeOptionalCustomModifiers) { throw null; }
        public System.Reflection.Emit.MethodBuilder DefineMethod(string name, System.Reflection.MethodAttributes attributes, System.Type returnType, System.Type[] parameterTypes) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public void DefineMethodOverride(System.Reflection.MethodInfo methodInfoBody, System.Reflection.MethodInfo methodInfoDeclaration) { }
        [System.Security.SecuritySafeCriticalAttribute]
        public System.Reflection.Emit.TypeBuilder DefineNestedType(string name) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public System.Reflection.Emit.TypeBuilder DefineNestedType(string name, System.Reflection.TypeAttributes attr) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public System.Reflection.Emit.TypeBuilder DefineNestedType(string name, System.Reflection.TypeAttributes attr, System.Type parent) { throw null; }
        [System.Security.SecurityCriticalAttribute]
        public System.Reflection.Emit.TypeBuilder DefineNestedType(string name, System.Reflection.TypeAttributes attr, System.Type parent, int typeSize) { throw null; }
        [System.Security.SecurityCriticalAttribute]
        public System.Reflection.Emit.TypeBuilder DefineNestedType(string name, System.Reflection.TypeAttributes attr, System.Type parent, System.Reflection.Emit.PackingSize packSize) { throw null; }
        [System.Security.SecurityCriticalAttribute]
        public System.Reflection.Emit.TypeBuilder DefineNestedType(string name, System.Reflection.TypeAttributes attr, System.Type parent, System.Reflection.Emit.PackingSize packSize, int typeSize) { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(true)]
        [System.Security.SecuritySafeCriticalAttribute]
        public System.Reflection.Emit.TypeBuilder DefineNestedType(string name, System.Reflection.TypeAttributes attr, System.Type parent, System.Type[] interfaces) { throw null; }
        public System.Reflection.Emit.PropertyBuilder DefineProperty(string name, System.Reflection.PropertyAttributes attributes, System.Reflection.CallingConventions callingConvention, System.Type returnType, System.Type[] parameterTypes) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public System.Reflection.Emit.PropertyBuilder DefineProperty(string name, System.Reflection.PropertyAttributes attributes, System.Reflection.CallingConventions callingConvention, System.Type returnType, System.Type[] returnTypeRequiredCustomModifiers, System.Type[] returnTypeOptionalCustomModifiers, System.Type[] parameterTypes, System.Type[][] parameterTypeRequiredCustomModifiers, System.Type[][] parameterTypeOptionalCustomModifiers) { throw null; }
        public System.Reflection.Emit.PropertyBuilder DefineProperty(string name, System.Reflection.PropertyAttributes attributes, System.Type returnType, System.Type[] parameterTypes) { throw null; }
        public System.Reflection.Emit.PropertyBuilder DefineProperty(string name, System.Reflection.PropertyAttributes attributes, System.Type returnType, System.Type[] returnTypeRequiredCustomModifiers, System.Type[] returnTypeOptionalCustomModifiers, System.Type[] parameterTypes, System.Type[][] parameterTypeRequiredCustomModifiers, System.Type[][] parameterTypeOptionalCustomModifiers) { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(true)]
        [System.Security.SecuritySafeCriticalAttribute]
        public System.Reflection.Emit.ConstructorBuilder DefineTypeInitializer() { throw null; }
        [System.Security.SecurityCriticalAttribute]
        public System.Reflection.Emit.FieldBuilder DefineUninitializedData(string name, int size, System.Reflection.FieldAttributes attributes) { throw null; }
        protected override System.Reflection.TypeAttributes GetAttributeFlagsImpl() { throw null; }
        public static System.Reflection.ConstructorInfo GetConstructor(System.Type type, System.Reflection.ConstructorInfo constructor) { throw null; }
        protected override System.Reflection.ConstructorInfo GetConstructorImpl(System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder binder, System.Reflection.CallingConventions callConvention, System.Type[] types, System.Reflection.ParameterModifier[] modifiers) { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(true)]
        public override System.Reflection.ConstructorInfo[] GetConstructors(System.Reflection.BindingFlags bindingAttr) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public override object[] GetCustomAttributes(bool inherit) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public override object[] GetCustomAttributes(System.Type attributeType, bool inherit) { throw null; }
        public override System.Type GetElementType() { throw null; }
        public override System.Reflection.EventInfo GetEvent(string name, System.Reflection.BindingFlags bindingAttr) { throw null; }
        public override System.Reflection.EventInfo[] GetEvents() { throw null; }
        public override System.Reflection.EventInfo[] GetEvents(System.Reflection.BindingFlags bindingAttr) { throw null; }
        public override System.Reflection.FieldInfo GetField(string name, System.Reflection.BindingFlags bindingAttr) { throw null; }
        public static System.Reflection.FieldInfo GetField(System.Type type, System.Reflection.FieldInfo field) { throw null; }
        public override System.Reflection.FieldInfo[] GetFields(System.Reflection.BindingFlags bindingAttr) { throw null; }
        public override System.Type[] GetGenericArguments() { throw null; }
        public override System.Type GetGenericTypeDefinition() { throw null; }
        public override System.Type GetInterface(string name, bool ignoreCase) { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(true)]
        public override System.Reflection.InterfaceMapping GetInterfaceMap(System.Type interfaceType) { throw null; }
        public override System.Type[] GetInterfaces() { throw null; }
        public override System.Reflection.MemberInfo[] GetMember(string name, System.Reflection.MemberTypes type, System.Reflection.BindingFlags bindingAttr) { throw null; }
        public override System.Reflection.MemberInfo[] GetMembers(System.Reflection.BindingFlags bindingAttr) { throw null; }
        public static System.Reflection.MethodInfo GetMethod(System.Type type, System.Reflection.MethodInfo method) { throw null; }
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
        public override bool IsAssignableFrom(System.Type c) { throw null; }
        protected override bool IsByRefImpl() { throw null; }
        protected override bool IsCOMObjectImpl() { throw null; }
        public bool IsCreated() { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public override bool IsDefined(System.Type attributeType, bool inherit) { throw null; }
        protected override bool IsPointerImpl() { throw null; }
        protected override bool IsPrimitiveImpl() { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(true)]
        public override bool IsSubclassOf(System.Type c) { throw null; }
        public override System.Type MakeArrayType() { throw null; }
        public override System.Type MakeArrayType(int rank) { throw null; }
        public override System.Type MakeByRefType() { throw null; }
        public override System.Type MakeGenericType(params System.Type[] typeArguments) { throw null; }
        public override System.Type MakePointerType() { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(true)]
        [System.Security.SecurityCriticalAttribute]
        public void SetCustomAttribute(System.Reflection.ConstructorInfo con, byte[] binaryAttribute) { }
        [System.Security.SecuritySafeCriticalAttribute]
        public void SetCustomAttribute(System.Reflection.Emit.CustomAttributeBuilder customBuilder) { }
        public void SetParent(System.Type parent) { }
        public override string ToString() { throw null; }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct TypeToken
    {
        public static readonly System.Reflection.Emit.TypeToken Empty;
        public int Token { get { throw null; } }
        public override bool Equals(object obj) { throw null; }
        public bool Equals(System.Reflection.Emit.TypeToken obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Reflection.Emit.TypeToken a, System.Reflection.Emit.TypeToken b) { throw null; }
        public static bool operator !=(System.Reflection.Emit.TypeToken a, System.Reflection.Emit.TypeToken b) { throw null; }
    }
}
namespace System.Reflection.Metadata
{
    public static partial class AssemblyExtensions
    {
        [System.CLSCompliantAttribute(false)]
        [System.Security.SecurityCriticalAttribute]
        public unsafe static bool TryGetRawMetadata(this System.Reflection.Assembly assembly, out byte* blob, out int length) { blob = default(byte*); length = default(int); throw null; }
    }
}
namespace System.Resources
{
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial interface IResourceReader : System.Collections.IEnumerable, System.IDisposable
    {
        new System.Collections.IDictionaryEnumerator GetEnumerator();
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class MissingManifestResourceException : System.SystemException
    {
        public MissingManifestResourceException() { }
        public MissingManifestResourceException(string message) { }
        public MissingManifestResourceException(string message, System.Exception inner) { }
        protected MissingManifestResourceException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(1), AllowMultiple=false)]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class NeutralResourcesLanguageAttribute : System.Attribute
    {
        public NeutralResourcesLanguageAttribute(string cultureName) { }
        public NeutralResourcesLanguageAttribute(string cultureName, System.Resources.UltimateResourceFallbackLocation location) { }
        public string CultureName { get { throw null; } }
        public System.Resources.UltimateResourceFallbackLocation Location { get { throw null; } }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class ResourceManager
    {
        protected string BaseNameField;
        public static readonly int HeaderVersionNumber;
        public static readonly int MagicNumber;
        protected System.Reflection.Assembly MainAssembly;
        protected ResourceManager() { }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public ResourceManager(string baseName, System.Reflection.Assembly assembly) { }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public ResourceManager(string baseName, System.Reflection.Assembly assembly, System.Type usingResourceSet) { }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public ResourceManager(System.Type resourceSource) { }
        public virtual string BaseName { get { throw null; } }
        public virtual bool IgnoreCase { get { throw null; } set { } }
        public virtual System.Type ResourceSetType { get { throw null; } }
        [System.Security.SecuritySafeCriticalAttribute]
        protected static System.Globalization.CultureInfo GetNeutralResourcesLanguage(System.Reflection.Assembly a) { throw null; }
        public virtual object GetObject(string name) { throw null; }
        public virtual object GetObject(string name, System.Globalization.CultureInfo culture) { throw null; }
        protected virtual string GetResourceFileName(System.Globalization.CultureInfo culture) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)][System.Security.SecuritySafeCriticalAttribute]
        public virtual System.Resources.ResourceSet GetResourceSet(System.Globalization.CultureInfo culture, bool createIfNotExists, bool tryParents) { throw null; }
        protected static System.Version GetSatelliteContractVersion(System.Reflection.Assembly a) { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public System.IO.UnmanagedMemoryStream GetStream(string name) { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public System.IO.UnmanagedMemoryStream GetStream(string name, System.Globalization.CultureInfo culture) { throw null; }
        public virtual string GetString(string name) { throw null; }
        public virtual string GetString(string name, System.Globalization.CultureInfo culture) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)][System.Security.SecuritySafeCriticalAttribute]
        protected virtual System.Resources.ResourceSet InternalGetResourceSet(System.Globalization.CultureInfo culture, bool createIfNotExists, bool tryParents) { throw null; }
        public virtual void ReleaseAllResources() { }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class ResourceSet : System.Collections.IEnumerable, System.IDisposable
    {
        protected System.Resources.IResourceReader Reader;
        protected ResourceSet() { }
        [System.Security.SecurityCriticalAttribute]
        public ResourceSet(System.IO.Stream stream) { }
        [System.Security.SecurityCriticalAttribute]
        public ResourceSet(string fileName) { }
        public virtual void Close() { }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        public virtual System.Type GetDefaultReader() { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public virtual System.Collections.IDictionaryEnumerator GetEnumerator() { throw null; }
        public virtual object GetObject(string name) { throw null; }
        public virtual object GetObject(string name, bool ignoreCase) { throw null; }
        public virtual string GetString(string name) { throw null; }
        public virtual string GetString(string name, bool ignoreCase) { throw null; }
        protected virtual void ReadResources() { }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(1), AllowMultiple=false)]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class SatelliteContractVersionAttribute : System.Attribute
    {
        public SatelliteContractVersionAttribute(string version) { }
        public string Version { get { throw null; } }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public enum UltimateResourceFallbackLocation
    {
        MainAssembly = 0,
        Satellite = 1,
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
        NoGCRegion = 4,
        SustainedLowLatency = 3,
    }
    public static partial class GCSettings
    {
        public static bool IsServerGC { [System.Security.SecuritySafeCriticalAttribute]get { throw null; } }
        public static System.Runtime.GCLargeObjectHeapCompactionMode LargeObjectHeapCompactionMode { [System.Security.SecuritySafeCriticalAttribute]get { throw null; } [System.Security.SecurityCriticalAttribute]set { } }
        public static System.Runtime.GCLatencyMode LatencyMode { [System.Security.SecuritySafeCriticalAttribute]get { throw null; } [System.Security.SecurityCriticalAttribute]set { } }
    }
}
namespace System.Runtime.CompilerServices
{
    [System.AttributeUsageAttribute((System.AttributeTargets)(256))]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class AccessedThroughPropertyAttribute : System.Attribute
    {
        public AccessedThroughPropertyAttribute(string propertyName) { }
        public string PropertyName { get { throw null; } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(64), Inherited=false, AllowMultiple=false)]
    public sealed partial class AsyncStateMachineAttribute : System.Runtime.CompilerServices.StateMachineAttribute
    {
        public AsyncStateMachineAttribute(System.Type stateMachineType) : base (default(System.Type)) { }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct AsyncTaskMethodBuilder
    {
        public System.Threading.Tasks.Task Task { get { throw null; } }
        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : System.Runtime.CompilerServices.INotifyCompletion where TStateMachine : System.Runtime.CompilerServices.IAsyncStateMachine { }
        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : System.Runtime.CompilerServices.ICriticalNotifyCompletion where TStateMachine : System.Runtime.CompilerServices.IAsyncStateMachine { }
        public static System.Runtime.CompilerServices.AsyncTaskMethodBuilder Create() { throw null; }
        public void SetException(System.Exception exception) { }
        public void SetResult() { }
        public void SetStateMachine(System.Runtime.CompilerServices.IAsyncStateMachine stateMachine) { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        [System.Security.SecuritySafeCriticalAttribute]
        public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : System.Runtime.CompilerServices.IAsyncStateMachine { }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct AsyncTaskMethodBuilder<TResult>
    {
        public System.Threading.Tasks.Task<TResult> Task { get { throw null; } }
        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : System.Runtime.CompilerServices.INotifyCompletion where TStateMachine : System.Runtime.CompilerServices.IAsyncStateMachine { }
        [System.Security.SecuritySafeCriticalAttribute]
        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : System.Runtime.CompilerServices.ICriticalNotifyCompletion where TStateMachine : System.Runtime.CompilerServices.IAsyncStateMachine { }
        public static System.Runtime.CompilerServices.AsyncTaskMethodBuilder<TResult> Create() { throw null; }
        public void SetException(System.Exception exception) { }
        public void SetResult(TResult result) { }
        public void SetStateMachine(System.Runtime.CompilerServices.IAsyncStateMachine stateMachine) { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        [System.Security.SecuritySafeCriticalAttribute]
        public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : System.Runtime.CompilerServices.IAsyncStateMachine { }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct AsyncVoidMethodBuilder
    {
        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : System.Runtime.CompilerServices.INotifyCompletion where TStateMachine : System.Runtime.CompilerServices.IAsyncStateMachine { }
        [System.Security.SecuritySafeCriticalAttribute]
        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : System.Runtime.CompilerServices.ICriticalNotifyCompletion where TStateMachine : System.Runtime.CompilerServices.IAsyncStateMachine { }
        public static System.Runtime.CompilerServices.AsyncVoidMethodBuilder Create() { throw null; }
        public void SetException(System.Exception exception) { }
        public void SetResult() { }
        public void SetStateMachine(System.Runtime.CompilerServices.IAsyncStateMachine stateMachine) { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        [System.Security.SecuritySafeCriticalAttribute]
        public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : System.Runtime.CompilerServices.IAsyncStateMachine { }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class CallConvCdecl
    {
        internal CallConvCdecl() { }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class CallConvFastcall
    {
        internal CallConvFastcall() { }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class CallConvStdcall
    {
        internal CallConvStdcall() { }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class CallConvThiscall
    {
        internal CallConvThiscall() { }
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
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public enum CompilationRelaxations
    {
        NoStringInterning = 8,
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(71))]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
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
    public static partial class CompilerMarshalOverride
    {
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(false)]
    public sealed partial class ConditionalWeakTable<TKey, TValue> where TKey : class where TValue : class
    {
        [System.Security.SecuritySafeCriticalAttribute]
        public ConditionalWeakTable() { }
        [System.Security.SecuritySafeCriticalAttribute]
        public void Add(TKey key, TValue value) { }
        ~ConditionalWeakTable() { }
        public TValue GetOrCreateValue(TKey key) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public TValue GetValue(TKey key, System.Runtime.CompilerServices.ConditionalWeakTable<TKey, TValue>.CreateValueCallback createValueCallback) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public bool Remove(TKey key) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public bool TryGetValue(TKey key, out TValue value) { value = default(TValue); throw null; }
        public delegate TValue CreateValueCallback(TKey key);
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct ConfiguredTaskAwaitable
    {
        public System.Runtime.CompilerServices.ConfiguredTaskAwaitable.ConfiguredTaskAwaiter GetAwaiter() { throw null; }
        [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
        public partial struct ConfiguredTaskAwaiter : System.Runtime.CompilerServices.ICriticalNotifyCompletion, System.Runtime.CompilerServices.INotifyCompletion
        {
            public bool IsCompleted { get { throw null; } }
            public void GetResult() { }
            [System.Security.SecuritySafeCriticalAttribute]
            public void OnCompleted(System.Action continuation) { }
            [System.Security.SecurityCriticalAttribute]
            public void UnsafeOnCompleted(System.Action continuation) { }
        }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct ConfiguredTaskAwaitable<TResult>
    {
        public System.Runtime.CompilerServices.ConfiguredTaskAwaitable<TResult>.ConfiguredTaskAwaiter GetAwaiter() { throw null; }
        [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
        public partial struct ConfiguredTaskAwaiter : System.Runtime.CompilerServices.ICriticalNotifyCompletion, System.Runtime.CompilerServices.INotifyCompletion
        {
            public bool IsCompleted { get { throw null; } }
            public TResult GetResult() { throw null; }
            [System.Security.SecuritySafeCriticalAttribute]
            public void OnCompleted(System.Action continuation) { }
            [System.Security.SecurityCriticalAttribute]
            public void UnsafeOnCompleted(System.Action continuation) { }
        }
    }
    public static partial class ContractHelper
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Design", "CA1030:UseEventsWhereAppropriate")]
        [System.Diagnostics.DebuggerNonUserCodeAttribute]
        public static string RaiseContractFailedEvent(System.Diagnostics.Contracts.ContractFailureKind failureKind, string userMessage, string conditionText, System.Exception innerException) { throw null; }
        [System.Diagnostics.DebuggerNonUserCodeAttribute]
        public static void TriggerFailure(System.Diagnostics.Contracts.ContractFailureKind kind, string displayMessage, string userMessage, string conditionText, System.Exception innerException) { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(2304), Inherited=false)]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public abstract partial class CustomConstantAttribute : System.Attribute
    {
        protected CustomConstantAttribute() { }
        public abstract object Value { get; }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(2304), Inherited=false)]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class DateTimeConstantAttribute : System.Runtime.CompilerServices.CustomConstantAttribute
    {
        public DateTimeConstantAttribute(long ticks) { }
        public override object Value { get { throw null; } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(2304), Inherited=false)]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class DecimalConstantAttribute : System.Attribute
    {
        public DecimalConstantAttribute(byte scale, byte sign, int hi, int mid, int low) { }
        [System.CLSCompliantAttribute(false)]
        public DecimalConstantAttribute(byte scale, byte sign, uint hi, uint mid, uint low) { }
        public decimal Value { get { throw null; } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(1), AllowMultiple=false, Inherited=false)]
    public sealed partial class DisablePrivateReflectionAttribute : System.Attribute
    {
        public DisablePrivateReflectionAttribute() { }
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
    public partial interface ICastable
    {
        System.RuntimeTypeHandle GetImplType(System.RuntimeTypeHandle interfaceType);
        bool IsInstanceOfInterface(System.RuntimeTypeHandle interfaceType, out System.Exception castError);
    }
    public partial interface ICriticalNotifyCompletion : System.Runtime.CompilerServices.INotifyCompletion
    {
        [System.Security.SecurityCriticalAttribute]
        void UnsafeOnCompleted(System.Action continuation);
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(128), Inherited=true)]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
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
    public static partial class IsBoxed
    {
    }
    public static partial class IsByValue
    {
    }
    public static partial class IsConst
    {
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public static partial class IsCopyConstructed
    {
    }
    public static partial class IsExplicitlyDereferenced
    {
    }
    public static partial class IsImplicitlyDereferenced
    {
    }
    public static partial class IsJitIntrinsic
    {
    }
    public static partial class IsLong
    {
    }
    public static partial class IsSignUnspecifiedByte
    {
    }
    public static partial class IsUdtReturn
    {
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public static partial class IsVolatile
    {
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(64), Inherited=false, AllowMultiple=false)]
    public sealed partial class IteratorStateMachineAttribute : System.Runtime.CompilerServices.StateMachineAttribute
    {
        public IteratorStateMachineAttribute(System.Type stateMachineType) : base (default(System.Type)) { }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public enum MethodCodeType
    {
        IL = 0,
        Native = 1,
        OPTIL = 2,
        Runtime = 3,
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(96), Inherited=false)]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class MethodImplAttribute : System.Attribute
    {
        public System.Runtime.CompilerServices.MethodCodeType MethodCodeType;
        public MethodImplAttribute() { }
        public MethodImplAttribute(short value) { }
        public MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions methodImplOptions) { }
        public System.Runtime.CompilerServices.MethodImplOptions Value { get { throw null; } }
    }
    [System.FlagsAttribute]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public enum MethodImplOptions
    {
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        AggressiveInlining = 256,
        ForwardRef = 16,
        InternalCall = 4096,
        NoInlining = 8,
        NoOptimization = 64,
        PreserveSig = 128,
        Synchronized = 32,
        Unmanaged = 4,
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(8), Inherited=true)]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class NativeCppClassAttribute : System.Attribute
    {
        public NativeCppClassAttribute() { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(1), AllowMultiple=false)]
    public sealed partial class ReferenceAssemblyAttribute : System.Attribute
    {
        public ReferenceAssemblyAttribute() { }
        public ReferenceAssemblyAttribute(string description) { }
        public string Description { get { throw null; } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(1052), AllowMultiple=true, Inherited=false)]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class RequiredAttributeAttribute : System.Attribute
    {
        public RequiredAttributeAttribute(System.Type requiredContract) { }
        public System.Type RequiredContract { get { throw null; } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(1), Inherited=false, AllowMultiple=false)]
    public sealed partial class RuntimeCompatibilityAttribute : System.Attribute
    {
        public RuntimeCompatibilityAttribute() { }
        public bool WrapNonExceptionThrows { get { throw null; } set { } }
    }
    public static partial class RuntimeHelpers
    {
        public static int OffsetToStringData { get { throw null; } }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)][System.Security.SecuritySafeCriticalAttribute]
        public static void EnsureSufficientExecutionStack() { }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)][System.Security.SecuritySafeCriticalAttribute]
        public static int GetHashCode(object o) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)][System.Security.SecuritySafeCriticalAttribute]
        public static object GetObjectValue(object obj) { throw null; }
        public static object GetUninitializedObject(System.Type type) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)][System.Security.SecuritySafeCriticalAttribute]
        public static void InitializeArray(System.Array array, System.RuntimeFieldHandle fldHandle) { }
        public static void RunClassConstructor(System.RuntimeTypeHandle type) { }
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
        public System.Type StateMachineType { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
    }
    [System.AttributeUsage((System.AttributeTargets)(1) | (System.AttributeTargets)(2))]
    public sealed partial class SuppressIldasmAttribute : System.Attribute
    {
        public SuppressIldasmAttribute() { }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct TaskAwaiter : System.Runtime.CompilerServices.ICriticalNotifyCompletion, System.Runtime.CompilerServices.INotifyCompletion
    {
        public bool IsCompleted { get { throw null; } }
        public void GetResult() { }
        [System.Security.SecuritySafeCriticalAttribute]
        public void OnCompleted(System.Action continuation) { }
        [System.Security.SecurityCriticalAttribute]
        public void UnsafeOnCompleted(System.Action continuation) { }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct TaskAwaiter<TResult> : System.Runtime.CompilerServices.ICriticalNotifyCompletion, System.Runtime.CompilerServices.INotifyCompletion
    {
        public bool IsCompleted { get { throw null; } }
        public TResult GetResult() { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public void OnCompleted(System.Action continuation) { }
        [System.Security.SecurityCriticalAttribute]
        public void UnsafeOnCompleted(System.Action continuation) { }
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
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential, Size=1)]
    public partial struct YieldAwaitable
    {
        public System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter GetAwaiter() { throw null; }
        [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential, Size=1)]
        public partial struct YieldAwaiter : System.Runtime.CompilerServices.ICriticalNotifyCompletion, System.Runtime.CompilerServices.INotifyCompletion
        {
            public bool IsCompleted { get { throw null; } }
            public void GetResult() { }
            [System.Security.SecuritySafeCriticalAttribute]
            public void OnCompleted(System.Action continuation) { }
            [System.Security.SecurityCriticalAttribute]
            public void UnsafeOnCompleted(System.Action continuation) { }
        }
    }
}
namespace System.Runtime.ConstrainedExecution
{
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public abstract partial class CriticalFinalizerObject
    {
        [System.Security.SecuritySafeCriticalAttribute]
        protected CriticalFinalizerObject() { }
        ~CriticalFinalizerObject() { }
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
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(64), AllowMultiple=false, Inherited=false)]
    public sealed partial class HandleProcessCorruptedStateExceptionsAttribute : System.Attribute
    {
        public HandleProcessCorruptedStateExceptionsAttribute() { }
    }
}
namespace System.Runtime.InteropServices
{
    [System.AttributeUsageAttribute((System.AttributeTargets)(64), AllowMultiple=false, Inherited=false)]
    public sealed partial class AllowReversePInvokeCallsAttribute : System.Attribute
    {
        public AllowReversePInvokeCallsAttribute() { }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct ArrayWithOffset
    {
        [System.Security.SecuritySafeCriticalAttribute]
        public ArrayWithOffset(object array, int offset) { throw null;}
        public override bool Equals(object obj) { throw null; }
        public bool Equals(System.Runtime.InteropServices.ArrayWithOffset obj) { throw null; }
        public object GetArray() { throw null; }
        public override int GetHashCode() { throw null; }
        public int GetOffset() { throw null; }
        public static bool operator ==(System.Runtime.InteropServices.ArrayWithOffset a, System.Runtime.InteropServices.ArrayWithOffset b) { throw null; }
        public static bool operator !=(System.Runtime.InteropServices.ArrayWithOffset a, System.Runtime.InteropServices.ArrayWithOffset b) { throw null; }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(1037), Inherited=false)]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class BestFitMappingAttribute : System.Attribute
    {
        public bool ThrowOnUnmappableChar;
        public BestFitMappingAttribute(bool BestFitMapping) { }
        public bool BestFitMapping { get { throw null; } }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class BStrWrapper
    {
        [System.Security.SecuritySafeCriticalAttribute]
        [System.Security.Permissions.SecurityPermissionAttribute(System.Security.Permissions.SecurityAction.Demand, Flags=(System.Security.Permissions.SecurityPermissionFlag)(2))]
        public BStrWrapper(object value) { }
        [System.Security.SecuritySafeCriticalAttribute]
        [System.Security.Permissions.SecurityPermissionAttribute(System.Security.Permissions.SecurityAction.Demand, Flags=(System.Security.Permissions.SecurityPermissionFlag)(2))]
        public BStrWrapper(string value) { }
        public string WrappedObject { get { throw null; } }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public enum CallingConvention
    {
        Cdecl = 2,
        FastCall = 5,
        StdCall = 3,
        ThisCall = 4,
        Winapi = 1,
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public enum CharSet
    {
        Ansi = 2,
        Auto = 4,
        None = 1,
        Unicode = 3,
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(5), Inherited=false)]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class ClassInterfaceAttribute : System.Attribute
    {
        public ClassInterfaceAttribute(short classInterfaceType) { }
        public ClassInterfaceAttribute(System.Runtime.InteropServices.ClassInterfaceType classInterfaceType) { }
        public System.Runtime.InteropServices.ClassInterfaceType Value { get { throw null; } }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public enum ClassInterfaceType
    {
        AutoDispatch = 1,
        AutoDual = 2,
        None = 0,
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(1024), Inherited=false)]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class CoClassAttribute : System.Attribute
    {
        public CoClassAttribute(System.Type coClass) { }
        public System.Type CoClass { get { throw null; } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(4), Inherited=false)]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class ComDefaultInterfaceAttribute : System.Attribute
    {
        public ComDefaultInterfaceAttribute(System.Type defaultInterface) { }
        public System.Type Value { get { throw null; } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(1024), Inherited=false)]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class ComEventInterfaceAttribute : System.Attribute
    {
        public ComEventInterfaceAttribute(System.Type SourceInterface, System.Type EventProvider) { }
        public System.Type EventProvider { get { throw null; } }
        public System.Type SourceInterface { get { throw null; } }
    }
    public static partial class ComEventsHelper
    {
        [System.Security.SecurityCriticalAttribute]
        public static void Combine(object rcw, System.Guid iid, int dispid, System.Delegate d) { }
        [System.Security.SecurityCriticalAttribute]
        public static System.Delegate Remove(object rcw, System.Guid iid, int dispid, System.Delegate d) { throw null; }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class COMException : System.Runtime.InteropServices.ExternalException
    {
        public COMException() { }
        public COMException(string message) { }
        public COMException(string message, System.Exception inner) { }
        public COMException(string message, int errorCode) { }
        protected COMException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
        public override string ToString() { throw null; }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(1028), Inherited=false)]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class ComImportAttribute : System.Attribute
    {
        public ComImportAttribute() { }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public enum ComInterfaceType
    {
        InterfaceIsDual = 0,
        InterfaceIsIDispatch = 2,
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        InterfaceIsIInspectable = 3,
        InterfaceIsIUnknown = 1,
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public enum ComMemberType
    {
        Method = 0,
        PropGet = 1,
        PropSet = 2,
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(4), Inherited=true)]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class ComSourceInterfacesAttribute : System.Attribute
    {
        public ComSourceInterfacesAttribute(string sourceInterfaces) { }
        public ComSourceInterfacesAttribute(System.Type sourceInterface) { }
        public ComSourceInterfacesAttribute(System.Type sourceInterface1, System.Type sourceInterface2) { }
        public ComSourceInterfacesAttribute(System.Type sourceInterface1, System.Type sourceInterface2, System.Type sourceInterface3) { }
        public ComSourceInterfacesAttribute(System.Type sourceInterface1, System.Type sourceInterface2, System.Type sourceInterface3, System.Type sourceInterface4) { }
        public string Value { get { throw null; } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(5597), Inherited=false)]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class ComVisibleAttribute : System.Attribute
    {
        public ComVisibleAttribute(bool visibility) { }
        public bool Value { get { throw null; } }
    }
    [System.Security.SecurityCriticalAttribute]
    public abstract partial class CriticalHandle : System.Runtime.ConstrainedExecution.CriticalFinalizerObject, System.IDisposable
    {
        protected System.IntPtr handle;
        protected CriticalHandle(System.IntPtr invalidHandleValue) { }
        public bool IsClosed { get { throw null; } }
        public abstract bool IsInvalid { get; }
        [System.Security.SecurityCriticalAttribute]
        public void Close() { }
        [System.Security.SecuritySafeCriticalAttribute]
        public void Dispose() { }
        [System.Security.SecurityCriticalAttribute]
        protected virtual void Dispose(bool disposing) { }
        ~CriticalHandle() { }
        protected abstract bool ReleaseHandle();
        protected void SetHandle(System.IntPtr handle) { }
        public void SetHandleAsInvalid() { }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class CurrencyWrapper
    {
        public CurrencyWrapper(decimal obj) { }
        public CurrencyWrapper(object obj) { }
        public decimal WrappedObject { get { throw null; } }
    }
    public enum CustomQueryInterfaceMode
    {
        Allow = 1,
        Ignore = 0,
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(false)]
    public enum CustomQueryInterfaceResult
    {
        Failed = 2,
        Handled = 0,
        NotHandled = 1,
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(2), Inherited=false)]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class DefaultCharSetAttribute : System.Attribute
    {
        public DefaultCharSetAttribute(System.Runtime.InteropServices.CharSet charSet) { }
        public System.Runtime.InteropServices.CharSet CharSet { get { throw null; } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(65), AllowMultiple=false)]
    [System.Runtime.InteropServices.ComVisibleAttribute(false)]
    public sealed partial class DefaultDllImportSearchPathsAttribute : System.Attribute
    {
        public DefaultDllImportSearchPathsAttribute(System.Runtime.InteropServices.DllImportSearchPath paths) { }
        public System.Runtime.InteropServices.DllImportSearchPath Paths { get { throw null; } }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class DispatchWrapper
    {
        [System.Security.SecuritySafeCriticalAttribute]
        [System.Security.Permissions.SecurityPermissionAttribute(System.Security.Permissions.SecurityAction.Demand, Flags=(System.Security.Permissions.SecurityPermissionFlag)(2))]
        public DispatchWrapper(object obj) { }
        public object WrappedObject { get { throw null; } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(960), Inherited=false)]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class DispIdAttribute : System.Attribute
    {
        public DispIdAttribute(int dispId) { }
        public int Value { get { throw null; } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(64), Inherited=false)]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class DllImportAttribute : System.Attribute
    {
        public bool BestFitMapping;
        public System.Runtime.InteropServices.CallingConvention CallingConvention;
        public System.Runtime.InteropServices.CharSet CharSet;
        public string EntryPoint;
        public bool ExactSpelling;
        public bool PreserveSig;
        public bool SetLastError;
        public bool ThrowOnUnmappableChar;
        public DllImportAttribute(string dllName) { }
        public string Value { get { throw null; } }
    }
    [System.FlagsAttribute]
    public enum DllImportSearchPath
    {
        ApplicationDirectory = 512,
        AssemblyDirectory = 2,
        LegacyBehavior = 0,
        SafeDirectories = 4096,
        System32 = 2048,
        UseDllDirectoryForDependencies = 256,
        UserDirectories = 1024,
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class ErrorWrapper
    {
        [System.Security.SecuritySafeCriticalAttribute]
        [System.Security.Permissions.SecurityPermissionAttribute(System.Security.Permissions.SecurityAction.Demand, Flags=(System.Security.Permissions.SecurityPermissionFlag)(2))]
        public ErrorWrapper(System.Exception e) { }
        public ErrorWrapper(int errorCode) { }
        public ErrorWrapper(object errorCode) { }
        public int ErrorCode { get { throw null; } }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class ExternalException : System.SystemException
    {
        public ExternalException() { }
        protected ExternalException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public ExternalException(string message) { }
        public ExternalException(string message, System.Exception inner) { }
        public ExternalException(string message, int errorCode) { }
        public virtual int ErrorCode { get { throw null; } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(256), Inherited=false)]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class FieldOffsetAttribute : System.Attribute
    {
        public FieldOffsetAttribute(int offset) { }
        public int Value { get { throw null; } }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct GCHandle
    {
        public bool IsAllocated { get { throw null; } }
        public object Target { [System.Security.SecurityCriticalAttribute]get { throw null; } [System.Security.SecurityCriticalAttribute]set { } }
        [System.Security.SecurityCriticalAttribute]
        public System.IntPtr AddrOfPinnedObject() { throw null; }
        [System.Security.SecurityCriticalAttribute]
        public static System.Runtime.InteropServices.GCHandle Alloc(object value) { throw null; }
        [System.Security.SecurityCriticalAttribute]
        public static System.Runtime.InteropServices.GCHandle Alloc(object value, System.Runtime.InteropServices.GCHandleType type) { throw null; }
        public override bool Equals(object o) { throw null; }
        [System.Security.SecurityCriticalAttribute]
        public void Free() { }
        [System.Security.SecurityCriticalAttribute]
        public static System.Runtime.InteropServices.GCHandle FromIntPtr(System.IntPtr value) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Runtime.InteropServices.GCHandle a, System.Runtime.InteropServices.GCHandle b) { throw null; }
        [System.Security.SecurityCriticalAttribute]
        public static explicit operator System.Runtime.InteropServices.GCHandle (System.IntPtr value) { throw null; }
        public static explicit operator System.IntPtr (System.Runtime.InteropServices.GCHandle value) { throw null; }
        public static bool operator !=(System.Runtime.InteropServices.GCHandle a, System.Runtime.InteropServices.GCHandle b) { throw null; }
        public static System.IntPtr ToIntPtr(System.Runtime.InteropServices.GCHandle value) { throw null; }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public enum GCHandleType
    {
        Normal = 2,
        Pinned = 3,
        Weak = 0,
        WeakTrackResurrection = 1,
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(5149), Inherited=false)]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class GuidAttribute : System.Attribute
    {
        public GuidAttribute(string guid) { }
        public string Value { get { throw null; } }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial interface ICustomAdapter
    {
        object GetUnderlyingObject();
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(false)]
    public partial interface ICustomQueryInterface
    {
        [System.Security.SecurityCriticalAttribute]
        System.Runtime.InteropServices.CustomQueryInterfaceResult GetInterface(ref System.Guid iid, out System.IntPtr ppv);
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(2048), Inherited=false)]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class InAttribute : System.Attribute
    {
        public InAttribute() { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(1024), Inherited=false)]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class InterfaceTypeAttribute : System.Attribute
    {
        public InterfaceTypeAttribute(short interfaceType) { }
        public InterfaceTypeAttribute(System.Runtime.InteropServices.ComInterfaceType interfaceType) { }
        public System.Runtime.InteropServices.ComInterfaceType Value { get { throw null; } }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class InvalidComObjectException : System.SystemException
    {
        public InvalidComObjectException() { }
        public InvalidComObjectException(string message) { }
        public InvalidComObjectException(string message, System.Exception inner) { }
        protected InvalidComObjectException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class InvalidOleVariantTypeException : System.SystemException
    {
        public InvalidOleVariantTypeException() { }
        public InvalidOleVariantTypeException(string message) { }
        public InvalidOleVariantTypeException(string message, System.Exception inner) { }
        protected InvalidOleVariantTypeException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public enum LayoutKind
    {
        Auto = 3,
        Explicit = 2,
        Sequential = 0,
    }
    [System.Security.SecurityCriticalAttribute]
    public static partial class Marshal
    {
        public static readonly int SystemDefaultCharSize;
        public static readonly int SystemMaxDBCSCharSize;
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)][System.Security.SecurityCriticalAttribute]
        public static int AddRef(System.IntPtr pUnk) { throw null; }
        [System.Security.SecurityCriticalAttribute]
        public static System.IntPtr AllocCoTaskMem(int cb) { throw null; }
        [System.Security.SecurityCriticalAttribute]
        public static System.IntPtr AllocHGlobal(int cb) { throw null; }
        [System.Security.SecurityCriticalAttribute]
        public static System.IntPtr AllocHGlobal(System.IntPtr cb) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)][System.Security.SecurityCriticalAttribute]
        public static bool AreComObjectsAvailableForCleanup() { throw null; }
        [System.Security.SecurityCriticalAttribute]
        public static void Copy(byte[] source, int startIndex, System.IntPtr destination, int length) { }
        [System.Security.SecurityCriticalAttribute]
        public static void Copy(char[] source, int startIndex, System.IntPtr destination, int length) { }
        [System.Security.SecurityCriticalAttribute]
        public static void Copy(double[] source, int startIndex, System.IntPtr destination, int length) { }
        [System.Security.SecurityCriticalAttribute]
        public static void Copy(short[] source, int startIndex, System.IntPtr destination, int length) { }
        [System.Security.SecurityCriticalAttribute]
        public static void Copy(int[] source, int startIndex, System.IntPtr destination, int length) { }
        [System.Security.SecurityCriticalAttribute]
        public static void Copy(long[] source, int startIndex, System.IntPtr destination, int length) { }
        [System.Security.SecurityCriticalAttribute]
        public static void Copy(System.IntPtr source, byte[] destination, int startIndex, int length) { }
        [System.Security.SecurityCriticalAttribute]
        public static void Copy(System.IntPtr source, char[] destination, int startIndex, int length) { }
        [System.Security.SecurityCriticalAttribute]
        public static void Copy(System.IntPtr source, double[] destination, int startIndex, int length) { }
        [System.Security.SecurityCriticalAttribute]
        public static void Copy(System.IntPtr source, short[] destination, int startIndex, int length) { }
        [System.Security.SecurityCriticalAttribute]
        public static void Copy(System.IntPtr source, int[] destination, int startIndex, int length) { }
        [System.Security.SecurityCriticalAttribute]
        public static void Copy(System.IntPtr source, long[] destination, int startIndex, int length) { }
        [System.Security.SecurityCriticalAttribute]
        public static void Copy(System.IntPtr source, System.IntPtr[] destination, int startIndex, int length) { }
        [System.Security.SecurityCriticalAttribute]
        public static void Copy(System.IntPtr source, float[] destination, int startIndex, int length) { }
        [System.Security.SecurityCriticalAttribute]
        public static void Copy(System.IntPtr[] source, int startIndex, System.IntPtr destination, int length) { }
        [System.Security.SecurityCriticalAttribute]
        public static void Copy(float[] source, int startIndex, System.IntPtr destination, int length) { }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)][System.Security.SecurityCriticalAttribute]
        public static System.IntPtr CreateAggregatedObject(System.IntPtr pOuter, object o) { throw null; }
        [System.Security.SecurityCriticalAttribute]
        public static System.IntPtr CreateAggregatedObject<T>(System.IntPtr pOuter, T o) { throw null; }
        [System.Security.SecurityCriticalAttribute]
        public static object CreateWrapperOfType(object o, System.Type t) { throw null; }
        [System.Security.SecurityCriticalAttribute]
        public static TWrapper CreateWrapperOfType<T, TWrapper>(T o) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)][System.Runtime.InteropServices.ComVisibleAttribute(true)]
        [System.Security.SecurityCriticalAttribute]
        public static void DestroyStructure(System.IntPtr ptr, System.Type structuretype) { }
        [System.Security.SecurityCriticalAttribute]
        public static void DestroyStructure<T>(System.IntPtr ptr) { }
        [System.Security.SecurityCriticalAttribute]
        public static int FinalReleaseComObject(object o) { throw null; }
        [System.Security.SecurityCriticalAttribute]
        public static void FreeBSTR(System.IntPtr ptr) { }
        [System.Security.SecurityCriticalAttribute]
        public static void FreeCoTaskMem(System.IntPtr ptr) { }
        [System.Security.SecurityCriticalAttribute]
        public static void FreeHGlobal(System.IntPtr hglobal) { }
        [System.Security.SecurityCriticalAttribute]
        public static System.IntPtr GetComInterfaceForObject(object o, System.Type T) { throw null; }
        [System.Security.SecurityCriticalAttribute]
        public static System.IntPtr GetComInterfaceForObject(object o, System.Type T, System.Runtime.InteropServices.CustomQueryInterfaceMode mode) { throw null; }
        [System.Security.SecurityCriticalAttribute]
        public static System.IntPtr GetComInterfaceForObject<T, TInterface>(T o) { throw null; }
        [System.Security.SecurityCriticalAttribute]
        public static System.Delegate GetDelegateForFunctionPointer(System.IntPtr ptr, System.Type t) { throw null; }
        [System.Security.SecurityCriticalAttribute]
        public static TDelegate GetDelegateForFunctionPointer<TDelegate>(System.IntPtr ptr) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)][System.Security.SecurityCriticalAttribute]
        public static int GetExceptionCode() { throw null; }
        [System.Security.SecurityCriticalAttribute]
        public static System.Exception GetExceptionForHR(int errorCode) { throw null; }
        [System.Security.SecurityCriticalAttribute]
        public static System.Exception GetExceptionForHR(int errorCode, System.IntPtr errorInfo) { throw null; }
        [System.Security.SecurityCriticalAttribute]
        public static System.IntPtr GetFunctionPointerForDelegate(System.Delegate d) { throw null; }
        [System.Security.SecurityCriticalAttribute]
        public static System.IntPtr GetFunctionPointerForDelegate<TDelegate>(TDelegate d) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)][System.Security.SecurityCriticalAttribute]
        public static int GetHRForException(System.Exception e) { throw null; }
        [System.Security.SecurityCriticalAttribute]
        public static int GetHRForLastWin32Error() { throw null; }
        [System.Security.SecurityCriticalAttribute]
        public static System.IntPtr GetIUnknownForObject(object o) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)][System.Security.SecurityCriticalAttribute]
        public static int GetLastWin32Error() { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)][System.Security.SecurityCriticalAttribute]
        public static void GetNativeVariantForObject(object obj, System.IntPtr pDstNativeVariant) { }
        [System.Security.SecurityCriticalAttribute]
        public static void GetNativeVariantForObject<T>(T obj, System.IntPtr pDstNativeVariant) { }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)][System.Security.SecurityCriticalAttribute]
        public static object GetObjectForIUnknown(System.IntPtr pUnk) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)][System.Security.SecurityCriticalAttribute]
        public static object GetObjectForNativeVariant(System.IntPtr pSrcNativeVariant) { throw null; }
        [System.Security.SecurityCriticalAttribute]
        public static T GetObjectForNativeVariant<T>(System.IntPtr pSrcNativeVariant) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)][System.Security.SecurityCriticalAttribute]
        public static object[] GetObjectsForNativeVariants(System.IntPtr aSrcNativeVariant, int cVars) { throw null; }
        [System.Security.SecurityCriticalAttribute]
        public static T[] GetObjectsForNativeVariants<T>(System.IntPtr aSrcNativeVariant, int cVars) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)][System.Security.SecurityCriticalAttribute]
        public static int GetStartComSlot(System.Type t) { throw null; }
        public static System.Type GetTypeFromCLSID(System.Guid clsid) { throw null; }
        [System.Security.SecurityCriticalAttribute]
        public static string GetTypeInfoName(System.Runtime.InteropServices.ComTypes.ITypeInfo typeInfo) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)][System.Security.SecurityCriticalAttribute]
        public static object GetUniqueObjectForIUnknown(System.IntPtr unknown) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)]public static bool IsComObject(object o) { throw null; }
        public static System.IntPtr OffsetOf(System.Type t, string fieldName) { throw null; }
        public static System.IntPtr OffsetOf<T>(string fieldName) { throw null; }
        [System.Security.SecurityCriticalAttribute]
        public static void Prelink(System.Reflection.MethodInfo m) { }
        [System.Security.SecurityCriticalAttribute]
        public static void PrelinkAll(System.Type c) { }
        [System.Security.SecurityCriticalAttribute]
        public static string PtrToStringAnsi(System.IntPtr ptr) { throw null; }
        [System.Security.SecurityCriticalAttribute]
        public static string PtrToStringAnsi(System.IntPtr ptr, int len) { throw null; }
        [System.Security.SecurityCriticalAttribute]
        public static string PtrToStringBSTR(System.IntPtr ptr) { throw null; }
        [System.Security.SecurityCriticalAttribute]
        public static string PtrToStringUni(System.IntPtr ptr) { throw null; }
        [System.Security.SecurityCriticalAttribute]
        public static string PtrToStringUni(System.IntPtr ptr, int len) { throw null; }
        [System.Security.SecurityCriticalAttribute]
        public static string PtrToStringUTF8(System.IntPtr ptr) { throw null; }
        [System.Security.SecurityCriticalAttribute]
        public static string PtrToStringUTF8(System.IntPtr ptr, int byteLen) { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(true)]
        [System.Security.SecurityCriticalAttribute]
        public static void PtrToStructure(System.IntPtr ptr, object structure) { }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)][System.Runtime.InteropServices.ComVisibleAttribute(true)]
        [System.Security.SecurityCriticalAttribute]
        public static object PtrToStructure(System.IntPtr ptr, System.Type structureType) { throw null; }
        [System.Security.SecurityCriticalAttribute]
        public static T PtrToStructure<T>(System.IntPtr ptr) { throw null; }
        [System.Security.SecurityCriticalAttribute]
        public static void PtrToStructure<T>(System.IntPtr ptr, T structure) { }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)][System.Security.SecurityCriticalAttribute]
        public static int QueryInterface(System.IntPtr pUnk, ref System.Guid iid, out System.IntPtr ppv) { ppv = default(System.IntPtr); throw null; }
        [System.Security.SecurityCriticalAttribute]
        public static byte ReadByte(System.IntPtr ptr) { throw null; }
        [System.Security.SecurityCriticalAttribute]
        public static byte ReadByte(System.IntPtr ptr, int ofs) { throw null; }
        [System.Security.SecurityCriticalAttribute]
        public static byte ReadByte(object ptr, int ofs) { throw null; }
        [System.Security.SecurityCriticalAttribute]
        public static short ReadInt16(System.IntPtr ptr) { throw null; }
        [System.Security.SecurityCriticalAttribute]
        public static short ReadInt16(System.IntPtr ptr, int ofs) { throw null; }
        [System.Security.SecurityCriticalAttribute]
        public static short ReadInt16(object ptr, int ofs) { throw null; }
        [System.Security.SecurityCriticalAttribute]
        public static int ReadInt32(System.IntPtr ptr) { throw null; }
        [System.Security.SecurityCriticalAttribute]
        public static int ReadInt32(System.IntPtr ptr, int ofs) { throw null; }
        [System.Security.SecurityCriticalAttribute]
        public static int ReadInt32(object ptr, int ofs) { throw null; }
        [System.Security.SecurityCriticalAttribute]
        public static long ReadInt64(System.IntPtr ptr) { throw null; }
        [System.Security.SecurityCriticalAttribute]
        public static long ReadInt64(System.IntPtr ptr, int ofs) { throw null; }
        [System.Security.SecurityCriticalAttribute]
        public static long ReadInt64(object ptr, int ofs) { throw null; }
        [System.Security.SecurityCriticalAttribute]
        public static System.IntPtr ReadIntPtr(System.IntPtr ptr) { throw null; }
        [System.Security.SecurityCriticalAttribute]
        public static System.IntPtr ReadIntPtr(System.IntPtr ptr, int ofs) { throw null; }
        [System.Security.SecurityCriticalAttribute]
        public static System.IntPtr ReadIntPtr(object ptr, int ofs) { throw null; }
        [System.Security.SecurityCriticalAttribute]
        public static System.IntPtr ReAllocCoTaskMem(System.IntPtr pv, int cb) { throw null; }
        [System.Security.SecurityCriticalAttribute]
        public static System.IntPtr ReAllocHGlobal(System.IntPtr pv, System.IntPtr cb) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)][System.Security.SecurityCriticalAttribute]
        public static int Release(System.IntPtr pUnk) { throw null; }
        [System.Security.SecurityCriticalAttribute]
        public static int ReleaseComObject(object o) { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(true)]
        public static int SizeOf(object structure) { throw null; }
        public static int SizeOf(System.Type t) { throw null; }
        public static int SizeOf<T>() { throw null; }
        public static int SizeOf<T>(T structure) { throw null; }
        [System.Security.SecurityCriticalAttribute]
        public static System.IntPtr StringToBSTR(string s) { throw null; }
        [System.Security.SecurityCriticalAttribute]
        public static System.IntPtr StringToCoTaskMemAnsi(string s) { throw null; }
        [System.Security.SecurityCriticalAttribute]
        public static System.IntPtr StringToCoTaskMemUni(string s) { throw null; }
        [System.Security.SecurityCriticalAttribute]
        public static System.IntPtr StringToCoTaskMemUTF8(string s) { throw null; }
        [System.Security.SecurityCriticalAttribute]
        public static System.IntPtr StringToHGlobalAnsi(string s) { throw null; }
        [System.Security.SecurityCriticalAttribute]
        public static System.IntPtr StringToHGlobalUni(string s) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)][System.Runtime.InteropServices.ComVisibleAttribute(true)]
        [System.Security.SecurityCriticalAttribute]
        public static void StructureToPtr(object structure, System.IntPtr ptr, bool fDeleteOld) { }
        [System.Security.SecurityCriticalAttribute]
        public static void StructureToPtr<T>(T structure, System.IntPtr ptr, bool fDeleteOld) { }
        [System.Security.SecurityCriticalAttribute]
        public static void ThrowExceptionForHR(int errorCode) { }
        [System.Security.SecurityCriticalAttribute]
        public static void ThrowExceptionForHR(int errorCode, System.IntPtr errorInfo) { }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)][System.Security.SecurityCriticalAttribute]
        public static System.IntPtr UnsafeAddrOfPinnedArrayElement(System.Array arr, int index) { throw null; }
        [System.Security.SecurityCriticalAttribute]
        public static System.IntPtr UnsafeAddrOfPinnedArrayElement<T>(T[] arr, int index) { throw null; }
        [System.Security.SecurityCriticalAttribute]
        public static void WriteByte(System.IntPtr ptr, byte val) { }
        [System.Security.SecurityCriticalAttribute]
        public static void WriteByte(System.IntPtr ptr, int ofs, byte val) { }
        [System.Security.SecurityCriticalAttribute]
        public static void WriteByte(object ptr, int ofs, byte val) { ptr = default(object); }
        [System.Security.SecurityCriticalAttribute]
        public static void WriteInt16(System.IntPtr ptr, char val) { }
        [System.Security.SecurityCriticalAttribute]
        public static void WriteInt16(System.IntPtr ptr, short val) { }
        [System.Security.SecurityCriticalAttribute]
        public static void WriteInt16(System.IntPtr ptr, int ofs, char val) { }
        [System.Security.SecurityCriticalAttribute]
        public static void WriteInt16(System.IntPtr ptr, int ofs, short val) { }
        [System.Security.SecurityCriticalAttribute]
        public static void WriteInt16(object ptr, int ofs, char val) { ptr = default(object); }
        [System.Security.SecurityCriticalAttribute]
        public static void WriteInt16(object ptr, int ofs, short val) { ptr = default(object); }
        [System.Security.SecurityCriticalAttribute]
        public static void WriteInt32(System.IntPtr ptr, int val) { }
        [System.Security.SecurityCriticalAttribute]
        public static void WriteInt32(System.IntPtr ptr, int ofs, int val) { }
        [System.Security.SecurityCriticalAttribute]
        public static void WriteInt32(object ptr, int ofs, int val) { ptr = default(object); }
        [System.Security.SecurityCriticalAttribute]
        public static void WriteInt64(System.IntPtr ptr, int ofs, long val) { }
        [System.Security.SecurityCriticalAttribute]
        public static void WriteInt64(System.IntPtr ptr, long val) { }
        [System.Security.SecurityCriticalAttribute]
        public static void WriteInt64(object ptr, int ofs, long val) { ptr = default(object); }
        [System.Security.SecurityCriticalAttribute]
        public static void WriteIntPtr(System.IntPtr ptr, int ofs, System.IntPtr val) { }
        [System.Security.SecurityCriticalAttribute]
        public static void WriteIntPtr(System.IntPtr ptr, System.IntPtr val) { }
        [System.Security.SecurityCriticalAttribute]
        public static void WriteIntPtr(object ptr, int ofs, System.IntPtr val) { ptr = default(object); }
        [System.Security.SecurityCriticalAttribute]
        public static void ZeroFreeBSTR(System.IntPtr s) { }
        [System.Security.SecurityCriticalAttribute]
        public static void ZeroFreeCoTaskMemAnsi(System.IntPtr s) { }
        [System.Security.SecurityCriticalAttribute]
        public static void ZeroFreeCoTaskMemUnicode(System.IntPtr s) { }
        [System.Security.SecurityCriticalAttribute]
        public static void ZeroFreeCoTaskMemUTF8(System.IntPtr s) { }
        [System.Security.SecurityCriticalAttribute]
        public static void ZeroFreeGlobalAllocAnsi(System.IntPtr s) { }
        [System.Security.SecurityCriticalAttribute]
        public static void ZeroFreeGlobalAllocUnicode(System.IntPtr s) { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(10496), Inherited=false)]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class MarshalAsAttribute : System.Attribute
    {
        public System.Runtime.InteropServices.UnmanagedType ArraySubType;
        public int IidParameterIndex;
        public string MarshalCookie;
        [System.Runtime.InteropServices.ComVisibleAttribute(true)]
        public string MarshalType;
        [System.Runtime.InteropServices.ComVisibleAttribute(true)]
        public System.Type MarshalTypeRef;
        public System.Runtime.InteropServices.VarEnum SafeArraySubType;
        public System.Type SafeArrayUserDefinedSubType;
        public int SizeConst;
        public short SizeParamIndex;
        public MarshalAsAttribute(short unmanagedType) { }
        public MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType unmanagedType) { }
        public System.Runtime.InteropServices.UnmanagedType Value { get { throw null; } }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class MarshalDirectiveException : System.SystemException
    {
        public MarshalDirectiveException() { }
        public MarshalDirectiveException(string message) { }
        public MarshalDirectiveException(string message, System.Exception inner) { }
        protected MarshalDirectiveException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(64))]
    public sealed partial class NativeCallableAttribute : System.Attribute
    {
        public System.Runtime.InteropServices.CallingConvention CallingConvention;
        public string EntryPoint;
        public NativeCallableAttribute() { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(2048), Inherited=false)]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class OptionalAttribute : System.Attribute
    {
        public OptionalAttribute() { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(2048), Inherited=false)]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class OutAttribute : System.Attribute
    {
        public OutAttribute() { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(64), Inherited=false)]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class PreserveSigAttribute : System.Attribute
    {
        public PreserveSigAttribute() { }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public static partial class RuntimeEnvironment
    {
        [System.Security.SecuritySafeCriticalAttribute]
        public static string GetRuntimeDirectory() { throw null; }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class SafeArrayRankMismatchException : System.SystemException
    {
        public SafeArrayRankMismatchException() { }
        public SafeArrayRankMismatchException(string message) { }
        public SafeArrayRankMismatchException(string message, System.Exception inner) { }
        protected SafeArrayRankMismatchException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class SafeArrayTypeMismatchException : System.SystemException
    {
        public SafeArrayTypeMismatchException() { }
        public SafeArrayTypeMismatchException(string message) { }
        public SafeArrayTypeMismatchException(string message, System.Exception inner) { }
        protected SafeArrayTypeMismatchException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
    [System.Security.SecurityCriticalAttribute]
    public abstract partial class SafeBuffer : Microsoft.Win32.SafeHandles.SafeHandleZeroOrMinusOneIsInvalid
    {
        protected SafeBuffer(bool ownsHandle) : base(ownsHandle) { }
        [System.CLSCompliantAttribute(false)]
        public ulong ByteLength { get { throw null; } }
        [System.CLSCompliantAttribute(false)]
        public unsafe void AcquirePointer(ref byte* pointer) { }
        [System.CLSCompliantAttribute(false)]
        public void Initialize(uint numElements, uint sizeOfEachElement) { }
        [System.CLSCompliantAttribute(false)]
        public void Initialize(ulong numBytes) { }
        [System.CLSCompliantAttribute(false)]
        public void Initialize<T>(uint numElements) where T : struct { }
        [System.CLSCompliantAttribute(false)]
        public T Read<T>(ulong byteOffset) where T : struct { throw null; }
        [System.CLSCompliantAttribute(false)]
        public void ReadArray<T>(ulong byteOffset, T[] array, int index, int count) where T : struct { }
        public void ReleasePointer() { }
        [System.CLSCompliantAttribute(false)]
        public void Write<T>(ulong byteOffset, T value) where T : struct { }
        [System.CLSCompliantAttribute(false)]
        public void WriteArray<T>(ulong byteOffset, T[] array, int index, int count) where T : struct { }
    }
    [System.Security.SecurityCriticalAttribute]
    public abstract partial class SafeHandle : System.Runtime.ConstrainedExecution.CriticalFinalizerObject, System.IDisposable
    {
        protected System.IntPtr handle;
        protected SafeHandle() { }
        protected SafeHandle(System.IntPtr invalidHandleValue, bool ownsHandle) { }
        public bool IsClosed { get { throw null; } }
        public abstract bool IsInvalid { get; }
        [System.Security.SecurityCriticalAttribute]
        public void Close() { }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)][System.Security.SecurityCriticalAttribute]
        public void DangerousAddRef(ref bool success) { }
        public System.IntPtr DangerousGetHandle() { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)][System.Security.SecurityCriticalAttribute]
        public void DangerousRelease() { }
        [System.Security.SecuritySafeCriticalAttribute]
        public void Dispose() { }
        [System.Security.SecurityCriticalAttribute]
        protected virtual void Dispose(bool disposing) { }
        ~SafeHandle() { }
        protected abstract bool ReleaseHandle();
        protected void SetHandle(System.IntPtr handle) { }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)][System.Security.SecurityCriticalAttribute]
        public void SetHandleAsInvalid() { }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class SEHException : System.Runtime.InteropServices.ExternalException
    {
        public SEHException() { }
        public SEHException(string message) { }
        public SEHException(string message, System.Exception inner) { }
        protected SEHException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
        public virtual bool CanResume() { throw null; }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(12), Inherited=false)]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class StructLayoutAttribute : System.Attribute
    {
        public System.Runtime.InteropServices.CharSet CharSet;
        public int Pack;
        public int Size;
        public StructLayoutAttribute(short layoutKind) { }
        public StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind layoutKind) { }
        public System.Runtime.InteropServices.LayoutKind Value { get { throw null; } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(5144), AllowMultiple=false, Inherited=false)]
    [System.Runtime.InteropServices.ComVisibleAttribute(false)]
    public sealed partial class TypeIdentifierAttribute : System.Attribute
    {
        public TypeIdentifierAttribute() { }
        public TypeIdentifierAttribute(string scope, string identifier) { }
        public string Identifier { get { throw null; } }
        public string Scope { get { throw null; } }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class UnknownWrapper
    {
        public UnknownWrapper(object obj) { }
        public object WrappedObject { get { throw null; } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(4096), AllowMultiple=false, Inherited=false)]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class UnmanagedFunctionPointerAttribute : System.Attribute
    {
        public bool BestFitMapping;
        public System.Runtime.InteropServices.CharSet CharSet;
        public bool SetLastError;
        public bool ThrowOnUnmappableChar;
        public UnmanagedFunctionPointerAttribute(System.Runtime.InteropServices.CallingConvention callingConvention) { }
        public System.Runtime.InteropServices.CallingConvention CallingConvention { get { throw null; } }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public enum UnmanagedType
    {
        AnsiBStr = 35,
        AsAny = 40,
        Bool = 2,
        BStr = 19,
        ByValArray = 30,
        ByValTStr = 23,
        Currency = 15,
        CustomMarshaler = 44,
        Error = 45,
        FunctionPtr = 38,
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        HString = 47,
        I1 = 3,
        I2 = 5,
        I4 = 7,
        I8 = 9,
        IDispatch = 26,
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        IInspectable = 46,
        Interface = 28,
        IUnknown = 25,
        LPArray = 42,
        LPStr = 20,
        LPStruct = 43,
        LPTStr = 22,
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        LPUTF8Str = 48,
        LPWStr = 21,
        R4 = 11,
        R8 = 12,
        SafeArray = 29,
        Struct = 27,
        SysInt = 31,
        SysUInt = 32,
        TBStr = 36,
        U1 = 4,
        U2 = 6,
        U4 = 8,
        U8 = 10,
        VariantBool = 37,
        VBByRefStr = 34,
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public enum VarEnum
    {
        VT_ARRAY = 8192,
        VT_BLOB = 65,
        VT_BLOB_OBJECT = 70,
        VT_BOOL = 11,
        VT_BSTR = 8,
        VT_BYREF = 16384,
        VT_CARRAY = 28,
        VT_CF = 71,
        VT_CLSID = 72,
        VT_CY = 6,
        VT_DATE = 7,
        VT_DECIMAL = 14,
        VT_DISPATCH = 9,
        VT_EMPTY = 0,
        VT_ERROR = 10,
        VT_FILETIME = 64,
        VT_HRESULT = 25,
        VT_I1 = 16,
        VT_I2 = 2,
        VT_I4 = 3,
        VT_I8 = 20,
        VT_INT = 22,
        VT_LPSTR = 30,
        VT_LPWSTR = 31,
        VT_NULL = 1,
        VT_PTR = 26,
        VT_R4 = 4,
        VT_R8 = 5,
        VT_RECORD = 36,
        VT_SAFEARRAY = 27,
        VT_STORAGE = 67,
        VT_STORED_OBJECT = 69,
        VT_STREAM = 66,
        VT_STREAMED_OBJECT = 68,
        VT_UI1 = 17,
        VT_UI2 = 18,
        VT_UI4 = 19,
        VT_UI8 = 21,
        VT_UINT = 23,
        VT_UNKNOWN = 13,
        VT_USERDEFINED = 29,
        VT_VARIANT = 12,
        VT_VECTOR = 4096,
        VT_VOID = 24,
    }
    public sealed partial class VariantWrapper
    {
        public VariantWrapper(object obj) { }
        public object WrappedObject { get { throw null; } }
    }
}
namespace System.Runtime.InteropServices.ComTypes
{
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct BIND_OPTS
    {
        public int cbStruct;
        public int dwTickCountDeadline;
        public int grfFlags;
        public int grfMode;
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Explicit)]
    public partial struct BINDPTR
    {
        [System.Runtime.InteropServices.FieldOffsetAttribute(0)]
        public System.IntPtr lpfuncdesc;
        [System.Runtime.InteropServices.FieldOffsetAttribute(0)]
        public System.IntPtr lptcomp;
        [System.Runtime.InteropServices.FieldOffsetAttribute(0)]
        public System.IntPtr lpvardesc;
    }
    public enum CALLCONV
    {
        CC_CDECL = 1,
        CC_MACPASCAL = 3,
        CC_MAX = 9,
        CC_MPWCDECL = 7,
        CC_MPWPASCAL = 8,
        CC_MSCPASCAL = 2,
        CC_PASCAL = 2,
        CC_RESERVED = 5,
        CC_STDCALL = 4,
        CC_SYSCALL = 6,
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct CONNECTDATA
    {
        public int dwCookie;
        public object pUnk;
    }
    public enum DESCKIND
    {
        DESCKIND_FUNCDESC = 1,
        DESCKIND_IMPLICITAPPOBJ = 4,
        DESCKIND_MAX = 5,
        DESCKIND_NONE = 0,
        DESCKIND_TYPECOMP = 3,
        DESCKIND_VARDESC = 2,
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct DISPPARAMS
    {
        public int cArgs;
        public int cNamedArgs;
        public System.IntPtr rgdispidNamedArgs;
        public System.IntPtr rgvarg;
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct ELEMDESC
    {
        public System.Runtime.InteropServices.ComTypes.ELEMDESC.DESCUNION desc;
        public System.Runtime.InteropServices.ComTypes.TYPEDESC tdesc;
        [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Explicit)]
        public partial struct DESCUNION
        {
            [System.Runtime.InteropServices.FieldOffsetAttribute(0)]
            public System.Runtime.InteropServices.ComTypes.IDLDESC idldesc;
            [System.Runtime.InteropServices.FieldOffsetAttribute(0)]
            public System.Runtime.InteropServices.ComTypes.PARAMDESC paramdesc;
        }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct EXCEPINFO
    {
        public string bstrDescription;
        public string bstrHelpFile;
        public string bstrSource;
        public int dwHelpContext;
        public System.IntPtr pfnDeferredFillIn;
        public System.IntPtr pvReserved;
        public int scode;
        public short wCode;
        public short wReserved;
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct FILETIME
    {
        public int dwHighDateTime;
        public int dwLowDateTime;
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct FUNCDESC
    {
        public System.Runtime.InteropServices.ComTypes.CALLCONV callconv;
        public short cParams;
        public short cParamsOpt;
        public short cScodes;
        public System.Runtime.InteropServices.ComTypes.ELEMDESC elemdescFunc;
        public System.Runtime.InteropServices.ComTypes.FUNCKIND funckind;
        public System.Runtime.InteropServices.ComTypes.INVOKEKIND invkind;
        public System.IntPtr lprgelemdescParam;
        public System.IntPtr lprgscode;
        public int memid;
        public short oVft;
        public short wFuncFlags;
    }
    [System.FlagsAttribute]
    public enum FUNCFLAGS : short
    {
        FUNCFLAG_FBINDABLE = (short)4,
        FUNCFLAG_FDEFAULTBIND = (short)32,
        FUNCFLAG_FDEFAULTCOLLELEM = (short)256,
        FUNCFLAG_FDISPLAYBIND = (short)16,
        FUNCFLAG_FHIDDEN = (short)64,
        FUNCFLAG_FIMMEDIATEBIND = (short)4096,
        FUNCFLAG_FNONBROWSABLE = (short)1024,
        FUNCFLAG_FREPLACEABLE = (short)2048,
        FUNCFLAG_FREQUESTEDIT = (short)8,
        FUNCFLAG_FRESTRICTED = (short)1,
        FUNCFLAG_FSOURCE = (short)2,
        FUNCFLAG_FUIDEFAULT = (short)512,
        FUNCFLAG_FUSESGETLASTERROR = (short)128,
    }
    public enum FUNCKIND
    {
        FUNC_DISPATCH = 4,
        FUNC_NONVIRTUAL = 2,
        FUNC_PUREVIRTUAL = 1,
        FUNC_STATIC = 3,
        FUNC_VIRTUAL = 0,
    }
    [System.Runtime.InteropServices.GuidAttribute("0000000e-0000-0000-C000-000000000046")]
    [System.Runtime.InteropServices.InterfaceTypeAttribute((System.Runtime.InteropServices.ComInterfaceType)(1))]
    public partial interface IBindCtx
    {
        void EnumObjectParam(out System.Runtime.InteropServices.ComTypes.IEnumString ppenum);
        void GetBindOptions(ref System.Runtime.InteropServices.ComTypes.BIND_OPTS pbindopts);
        void GetObjectParam(string pszKey, out object ppunk);
        void GetRunningObjectTable(out System.Runtime.InteropServices.ComTypes.IRunningObjectTable pprot);
        void RegisterObjectBound(object punk);
        void RegisterObjectParam(string pszKey, object punk);
        void ReleaseBoundObjects();
        void RevokeObjectBound(object punk);
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.PreserveSig)]int RevokeObjectParam(string pszKey);
        void SetBindOptions(ref System.Runtime.InteropServices.ComTypes.BIND_OPTS pbindopts);
    }
    [System.Runtime.InteropServices.GuidAttribute("B196B286-BAB4-101A-B69C-00AA00341D07")]
    [System.Runtime.InteropServices.InterfaceTypeAttribute((System.Runtime.InteropServices.ComInterfaceType)(1))]
    public partial interface IConnectionPoint
    {
        void Advise(object pUnkSink, out int pdwCookie);
        void EnumConnections(out System.Runtime.InteropServices.ComTypes.IEnumConnections ppEnum);
        void GetConnectionInterface(out System.Guid pIID);
        void GetConnectionPointContainer(out System.Runtime.InteropServices.ComTypes.IConnectionPointContainer ppCPC);
        void Unadvise(int dwCookie);
    }
    [System.Runtime.InteropServices.GuidAttribute("B196B284-BAB4-101A-B69C-00AA00341D07")]
    [System.Runtime.InteropServices.InterfaceTypeAttribute((System.Runtime.InteropServices.ComInterfaceType)(1))]
    public partial interface IConnectionPointContainer
    {
        void EnumConnectionPoints(out System.Runtime.InteropServices.ComTypes.IEnumConnectionPoints ppEnum);
        void FindConnectionPoint(ref System.Guid riid, out System.Runtime.InteropServices.ComTypes.IConnectionPoint ppCP);
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct IDLDESC
    {
        public System.IntPtr dwReserved;
        public System.Runtime.InteropServices.ComTypes.IDLFLAG wIDLFlags;
    }
    [System.FlagsAttribute]
    public enum IDLFLAG : short
    {
        IDLFLAG_FIN = (short)1,
        IDLFLAG_FLCID = (short)4,
        IDLFLAG_FOUT = (short)2,
        IDLFLAG_FRETVAL = (short)8,
        IDLFLAG_NONE = (short)0,
    }
    [System.Runtime.InteropServices.GuidAttribute("B196B285-BAB4-101A-B69C-00AA00341D07")]
    [System.Runtime.InteropServices.InterfaceTypeAttribute((System.Runtime.InteropServices.ComInterfaceType)(1))]
    public partial interface IEnumConnectionPoints
    {
        void Clone(out System.Runtime.InteropServices.ComTypes.IEnumConnectionPoints ppenum);
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.PreserveSig)]int Next(int celt, System.Runtime.InteropServices.ComTypes.IConnectionPoint[] rgelt, System.IntPtr pceltFetched);
        void Reset();
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.PreserveSig)]int Skip(int celt);
    }
    [System.Runtime.InteropServices.GuidAttribute("B196B287-BAB4-101A-B69C-00AA00341D07")]
    [System.Runtime.InteropServices.InterfaceTypeAttribute((System.Runtime.InteropServices.ComInterfaceType)(1))]
    public partial interface IEnumConnections
    {
        void Clone(out System.Runtime.InteropServices.ComTypes.IEnumConnections ppenum);
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.PreserveSig)]int Next(int celt, System.Runtime.InteropServices.ComTypes.CONNECTDATA[] rgelt, System.IntPtr pceltFetched);
        void Reset();
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.PreserveSig)]int Skip(int celt);
    }
    [System.Runtime.InteropServices.GuidAttribute("00000102-0000-0000-C000-000000000046")]
    [System.Runtime.InteropServices.InterfaceTypeAttribute((System.Runtime.InteropServices.ComInterfaceType)(1))]
    public partial interface IEnumMoniker
    {
        void Clone(out System.Runtime.InteropServices.ComTypes.IEnumMoniker ppenum);
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.PreserveSig)]int Next(int celt, System.Runtime.InteropServices.ComTypes.IMoniker[] rgelt, System.IntPtr pceltFetched);
        void Reset();
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.PreserveSig)]int Skip(int celt);
    }
    [System.Runtime.InteropServices.GuidAttribute("00000101-0000-0000-C000-000000000046")]
    [System.Runtime.InteropServices.InterfaceTypeAttribute((System.Runtime.InteropServices.ComInterfaceType)(1))]
    public partial interface IEnumString
    {
        void Clone(out System.Runtime.InteropServices.ComTypes.IEnumString ppenum);
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.PreserveSig)]int Next(int celt, string[] rgelt, System.IntPtr pceltFetched);
        void Reset();
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.PreserveSig)]int Skip(int celt);
    }
    [System.Runtime.InteropServices.GuidAttribute("00020404-0000-0000-C000-000000000046")]
    [System.Runtime.InteropServices.InterfaceTypeAttribute((System.Runtime.InteropServices.ComInterfaceType)(1))]
    public partial interface IEnumVARIANT
    {
        System.Runtime.InteropServices.ComTypes.IEnumVARIANT Clone();
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.PreserveSig)]int Next(int celt, object[] rgVar, System.IntPtr pceltFetched);
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.PreserveSig)]int Reset();
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.PreserveSig)]int Skip(int celt);
    }
    [System.Runtime.InteropServices.GuidAttribute("0000000f-0000-0000-C000-000000000046")]
    [System.Runtime.InteropServices.InterfaceTypeAttribute((System.Runtime.InteropServices.ComInterfaceType)(1))]
    public partial interface IMoniker
    {
        void BindToObject(System.Runtime.InteropServices.ComTypes.IBindCtx pbc, System.Runtime.InteropServices.ComTypes.IMoniker pmkToLeft, ref System.Guid riidResult, out object ppvResult);
        void BindToStorage(System.Runtime.InteropServices.ComTypes.IBindCtx pbc, System.Runtime.InteropServices.ComTypes.IMoniker pmkToLeft, ref System.Guid riid, out object ppvObj);
        void CommonPrefixWith(System.Runtime.InteropServices.ComTypes.IMoniker pmkOther, out System.Runtime.InteropServices.ComTypes.IMoniker ppmkPrefix);
        void ComposeWith(System.Runtime.InteropServices.ComTypes.IMoniker pmkRight, bool fOnlyIfNotGeneric, out System.Runtime.InteropServices.ComTypes.IMoniker ppmkComposite);
        void Enum(bool fForward, out System.Runtime.InteropServices.ComTypes.IEnumMoniker ppenumMoniker);
        void GetClassID(out System.Guid pClassID);
        void GetDisplayName(System.Runtime.InteropServices.ComTypes.IBindCtx pbc, System.Runtime.InteropServices.ComTypes.IMoniker pmkToLeft, out string ppszDisplayName);
        void GetSizeMax(out long pcbSize);
        void GetTimeOfLastChange(System.Runtime.InteropServices.ComTypes.IBindCtx pbc, System.Runtime.InteropServices.ComTypes.IMoniker pmkToLeft, out System.Runtime.InteropServices.ComTypes.FILETIME pFileTime);
        void Hash(out int pdwHash);
        void Inverse(out System.Runtime.InteropServices.ComTypes.IMoniker ppmk);
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.PreserveSig)]int IsDirty();
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.PreserveSig)]int IsEqual(System.Runtime.InteropServices.ComTypes.IMoniker pmkOtherMoniker);
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.PreserveSig)]int IsRunning(System.Runtime.InteropServices.ComTypes.IBindCtx pbc, System.Runtime.InteropServices.ComTypes.IMoniker pmkToLeft, System.Runtime.InteropServices.ComTypes.IMoniker pmkNewlyRunning);
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.PreserveSig)]int IsSystemMoniker(out int pdwMksys);
        void Load(System.Runtime.InteropServices.ComTypes.IStream pStm);
        void ParseDisplayName(System.Runtime.InteropServices.ComTypes.IBindCtx pbc, System.Runtime.InteropServices.ComTypes.IMoniker pmkToLeft, string pszDisplayName, out int pchEaten, out System.Runtime.InteropServices.ComTypes.IMoniker ppmkOut);
        void Reduce(System.Runtime.InteropServices.ComTypes.IBindCtx pbc, int dwReduceHowFar, ref System.Runtime.InteropServices.ComTypes.IMoniker ppmkToLeft, out System.Runtime.InteropServices.ComTypes.IMoniker ppmkReduced);
        void RelativePathTo(System.Runtime.InteropServices.ComTypes.IMoniker pmkOther, out System.Runtime.InteropServices.ComTypes.IMoniker ppmkRelPath);
        void Save(System.Runtime.InteropServices.ComTypes.IStream pStm, bool fClearDirty);
    }
    [System.FlagsAttribute]
    public enum IMPLTYPEFLAGS
    {
        IMPLTYPEFLAG_FDEFAULT = 1,
        IMPLTYPEFLAG_FDEFAULTVTABLE = 8,
        IMPLTYPEFLAG_FRESTRICTED = 4,
        IMPLTYPEFLAG_FSOURCE = 2,
    }
    [System.FlagsAttribute]
    public enum INVOKEKIND
    {
        INVOKE_FUNC = 1,
        INVOKE_PROPERTYGET = 2,
        INVOKE_PROPERTYPUT = 4,
        INVOKE_PROPERTYPUTREF = 8,
    }
    [System.Runtime.InteropServices.GuidAttribute("0000010b-0000-0000-C000-000000000046")]
    [System.Runtime.InteropServices.InterfaceTypeAttribute((System.Runtime.InteropServices.ComInterfaceType)(1))]
    public partial interface IPersistFile
    {
        void GetClassID(out System.Guid pClassID);
        void GetCurFile(out string ppszFileName);
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.PreserveSig)]int IsDirty();
        void Load(string pszFileName, int dwMode);
        void Save(string pszFileName, bool fRemember);
        void SaveCompleted(string pszFileName);
    }
    [System.Runtime.InteropServices.GuidAttribute("00000010-0000-0000-C000-000000000046")]
    [System.Runtime.InteropServices.InterfaceTypeAttribute((System.Runtime.InteropServices.ComInterfaceType)(1))]
    public partial interface IRunningObjectTable
    {
        void EnumRunning(out System.Runtime.InteropServices.ComTypes.IEnumMoniker ppenumMoniker);
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.PreserveSig)]int GetObject(System.Runtime.InteropServices.ComTypes.IMoniker pmkObjectName, out object ppunkObject);
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.PreserveSig)]int GetTimeOfLastChange(System.Runtime.InteropServices.ComTypes.IMoniker pmkObjectName, out System.Runtime.InteropServices.ComTypes.FILETIME pfiletime);
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.PreserveSig)]int IsRunning(System.Runtime.InteropServices.ComTypes.IMoniker pmkObjectName);
        void NoteChangeTime(int dwRegister, ref System.Runtime.InteropServices.ComTypes.FILETIME pfiletime);
        int Register(int grfFlags, object punkObject, System.Runtime.InteropServices.ComTypes.IMoniker pmkObjectName);
        void Revoke(int dwRegister);
    }
    [System.Runtime.InteropServices.GuidAttribute("0000000c-0000-0000-C000-000000000046")]
    [System.Runtime.InteropServices.InterfaceTypeAttribute((System.Runtime.InteropServices.ComInterfaceType)(1))]
    public partial interface IStream
    {
        void Clone(out System.Runtime.InteropServices.ComTypes.IStream ppstm);
        void Commit(int grfCommitFlags);
        void CopyTo(System.Runtime.InteropServices.ComTypes.IStream pstm, long cb, System.IntPtr pcbRead, System.IntPtr pcbWritten);
        void LockRegion(long libOffset, long cb, int dwLockType);
        void Read(byte[] pv, int cb, System.IntPtr pcbRead);
        void Revert();
        void Seek(long dlibMove, int dwOrigin, System.IntPtr plibNewPosition);
        void SetSize(long libNewSize);
        void Stat(out System.Runtime.InteropServices.ComTypes.STATSTG pstatstg, int grfStatFlag);
        void UnlockRegion(long libOffset, long cb, int dwLockType);
        void Write(byte[] pv, int cb, System.IntPtr pcbWritten);
    }
    [System.Runtime.InteropServices.GuidAttribute("00020403-0000-0000-C000-000000000046")]
    [System.Runtime.InteropServices.InterfaceTypeAttribute((System.Runtime.InteropServices.ComInterfaceType)(1))]
    public partial interface ITypeComp
    {
        void Bind(string szName, int lHashVal, short wFlags, out System.Runtime.InteropServices.ComTypes.ITypeInfo ppTInfo, out System.Runtime.InteropServices.ComTypes.DESCKIND pDescKind, out System.Runtime.InteropServices.ComTypes.BINDPTR pBindPtr);
        void BindType(string szName, int lHashVal, out System.Runtime.InteropServices.ComTypes.ITypeInfo ppTInfo, out System.Runtime.InteropServices.ComTypes.ITypeComp ppTComp);
    }
    [System.Runtime.InteropServices.GuidAttribute("00020401-0000-0000-C000-000000000046")]
    [System.Runtime.InteropServices.InterfaceTypeAttribute((System.Runtime.InteropServices.ComInterfaceType)(1))]
    public partial interface ITypeInfo
    {
        void AddressOfMember(int memid, System.Runtime.InteropServices.ComTypes.INVOKEKIND invKind, out System.IntPtr ppv);
        void CreateInstance(object pUnkOuter, ref System.Guid riid, out object ppvObj);
        void GetContainingTypeLib(out System.Runtime.InteropServices.ComTypes.ITypeLib ppTLB, out int pIndex);
        void GetDllEntry(int memid, System.Runtime.InteropServices.ComTypes.INVOKEKIND invKind, System.IntPtr pBstrDllName, System.IntPtr pBstrName, System.IntPtr pwOrdinal);
        void GetDocumentation(int index, out string strName, out string strDocString, out int dwHelpContext, out string strHelpFile);
        void GetFuncDesc(int index, out System.IntPtr ppFuncDesc);
        void GetIDsOfNames(string[] rgszNames, int cNames, int[] pMemId);
        void GetImplTypeFlags(int index, out System.Runtime.InteropServices.ComTypes.IMPLTYPEFLAGS pImplTypeFlags);
        void GetMops(int memid, out string pBstrMops);
        void GetNames(int memid, string[] rgBstrNames, int cMaxNames, out int pcNames);
        void GetRefTypeInfo(int hRef, out System.Runtime.InteropServices.ComTypes.ITypeInfo ppTI);
        void GetRefTypeOfImplType(int index, out int href);
        void GetTypeAttr(out System.IntPtr ppTypeAttr);
        void GetTypeComp(out System.Runtime.InteropServices.ComTypes.ITypeComp ppTComp);
        void GetVarDesc(int index, out System.IntPtr ppVarDesc);
        void Invoke(object pvInstance, int memid, short wFlags, ref System.Runtime.InteropServices.ComTypes.DISPPARAMS pDispParams, System.IntPtr pVarResult, System.IntPtr pExcepInfo, out int puArgErr);
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.PreserveSig)]void ReleaseFuncDesc(System.IntPtr pFuncDesc);
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.PreserveSig)]void ReleaseTypeAttr(System.IntPtr pTypeAttr);
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.PreserveSig)]void ReleaseVarDesc(System.IntPtr pVarDesc);
    }
    [System.Runtime.InteropServices.GuidAttribute("00020412-0000-0000-C000-000000000046")]
    [System.Runtime.InteropServices.InterfaceTypeAttribute((System.Runtime.InteropServices.ComInterfaceType)(1))]
    public partial interface ITypeInfo2 : System.Runtime.InteropServices.ComTypes.ITypeInfo
    {
        new void AddressOfMember(int memid, System.Runtime.InteropServices.ComTypes.INVOKEKIND invKind, out System.IntPtr ppv);
        new void CreateInstance(object pUnkOuter, ref System.Guid riid, out object ppvObj);
        void GetAllCustData(System.IntPtr pCustData);
        void GetAllFuncCustData(int index, System.IntPtr pCustData);
        void GetAllImplTypeCustData(int index, System.IntPtr pCustData);
        void GetAllParamCustData(int indexFunc, int indexParam, System.IntPtr pCustData);
        void GetAllVarCustData(int index, System.IntPtr pCustData);
        new void GetContainingTypeLib(out System.Runtime.InteropServices.ComTypes.ITypeLib ppTLB, out int pIndex);
        void GetCustData(ref System.Guid guid, out object pVarVal);
        new void GetDllEntry(int memid, System.Runtime.InteropServices.ComTypes.INVOKEKIND invKind, System.IntPtr pBstrDllName, System.IntPtr pBstrName, System.IntPtr pwOrdinal);
        new void GetDocumentation(int index, out string strName, out string strDocString, out int dwHelpContext, out string strHelpFile);
        void GetDocumentation2(int memid, out string pbstrHelpString, out int pdwHelpStringContext, out string pbstrHelpStringDll);
        void GetFuncCustData(int index, ref System.Guid guid, out object pVarVal);
        new void GetFuncDesc(int index, out System.IntPtr ppFuncDesc);
        void GetFuncIndexOfMemId(int memid, System.Runtime.InteropServices.ComTypes.INVOKEKIND invKind, out int pFuncIndex);
        new void GetIDsOfNames(string[] rgszNames, int cNames, int[] pMemId);
        void GetImplTypeCustData(int index, ref System.Guid guid, out object pVarVal);
        new void GetImplTypeFlags(int index, out System.Runtime.InteropServices.ComTypes.IMPLTYPEFLAGS pImplTypeFlags);
        new void GetMops(int memid, out string pBstrMops);
        new void GetNames(int memid, string[] rgBstrNames, int cMaxNames, out int pcNames);
        void GetParamCustData(int indexFunc, int indexParam, ref System.Guid guid, out object pVarVal);
        new void GetRefTypeInfo(int hRef, out System.Runtime.InteropServices.ComTypes.ITypeInfo ppTI);
        new void GetRefTypeOfImplType(int index, out int href);
        new void GetTypeAttr(out System.IntPtr ppTypeAttr);
        new void GetTypeComp(out System.Runtime.InteropServices.ComTypes.ITypeComp ppTComp);
        void GetTypeFlags(out int pTypeFlags);
        void GetTypeKind(out System.Runtime.InteropServices.ComTypes.TYPEKIND pTypeKind);
        void GetVarCustData(int index, ref System.Guid guid, out object pVarVal);
        new void GetVarDesc(int index, out System.IntPtr ppVarDesc);
        void GetVarIndexOfMemId(int memid, out int pVarIndex);
        new void Invoke(object pvInstance, int memid, short wFlags, ref System.Runtime.InteropServices.ComTypes.DISPPARAMS pDispParams, System.IntPtr pVarResult, System.IntPtr pExcepInfo, out int puArgErr);
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.PreserveSig)]new void ReleaseFuncDesc(System.IntPtr pFuncDesc);
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.PreserveSig)]new void ReleaseTypeAttr(System.IntPtr pTypeAttr);
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.PreserveSig)]new void ReleaseVarDesc(System.IntPtr pVarDesc);
    }
    [System.Runtime.InteropServices.GuidAttribute("00020402-0000-0000-C000-000000000046")]
    [System.Runtime.InteropServices.InterfaceTypeAttribute((System.Runtime.InteropServices.ComInterfaceType)(1))]
    public partial interface ITypeLib
    {
        void FindName(string szNameBuf, int lHashVal, System.Runtime.InteropServices.ComTypes.ITypeInfo[] ppTInfo, int[] rgMemId, ref short pcFound);
        void GetDocumentation(int index, out string strName, out string strDocString, out int dwHelpContext, out string strHelpFile);
        void GetLibAttr(out System.IntPtr ppTLibAttr);
        void GetTypeComp(out System.Runtime.InteropServices.ComTypes.ITypeComp ppTComp);
        void GetTypeInfo(int index, out System.Runtime.InteropServices.ComTypes.ITypeInfo ppTI);
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.PreserveSig)]int GetTypeInfoCount();
        void GetTypeInfoOfGuid(ref System.Guid guid, out System.Runtime.InteropServices.ComTypes.ITypeInfo ppTInfo);
        void GetTypeInfoType(int index, out System.Runtime.InteropServices.ComTypes.TYPEKIND pTKind);
        bool IsName(string szNameBuf, int lHashVal);
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.PreserveSig)]void ReleaseTLibAttr(System.IntPtr pTLibAttr);
    }
    [System.Runtime.InteropServices.GuidAttribute("00020411-0000-0000-C000-000000000046")]
    [System.Runtime.InteropServices.InterfaceTypeAttribute((System.Runtime.InteropServices.ComInterfaceType)(1))]
    public partial interface ITypeLib2 : System.Runtime.InteropServices.ComTypes.ITypeLib
    {
        new void FindName(string szNameBuf, int lHashVal, System.Runtime.InteropServices.ComTypes.ITypeInfo[] ppTInfo, int[] rgMemId, ref short pcFound);
        void GetAllCustData(System.IntPtr pCustData);
        void GetCustData(ref System.Guid guid, out object pVarVal);
        new void GetDocumentation(int index, out string strName, out string strDocString, out int dwHelpContext, out string strHelpFile);
        void GetDocumentation2(int index, out string pbstrHelpString, out int pdwHelpStringContext, out string pbstrHelpStringDll);
        new void GetLibAttr(out System.IntPtr ppTLibAttr);
        void GetLibStatistics(System.IntPtr pcUniqueNames, out int pcchUniqueNames);
        new void GetTypeComp(out System.Runtime.InteropServices.ComTypes.ITypeComp ppTComp);
        new void GetTypeInfo(int index, out System.Runtime.InteropServices.ComTypes.ITypeInfo ppTI);
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.PreserveSig)]new int GetTypeInfoCount();
        new void GetTypeInfoOfGuid(ref System.Guid guid, out System.Runtime.InteropServices.ComTypes.ITypeInfo ppTInfo);
        new void GetTypeInfoType(int index, out System.Runtime.InteropServices.ComTypes.TYPEKIND pTKind);
        new bool IsName(string szNameBuf, int lHashVal);
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.PreserveSig)]new void ReleaseTLibAttr(System.IntPtr pTLibAttr);
    }
    [System.FlagsAttribute]
    public enum LIBFLAGS : short
    {
        LIBFLAG_FCONTROL = (short)2,
        LIBFLAG_FHASDISKIMAGE = (short)8,
        LIBFLAG_FHIDDEN = (short)4,
        LIBFLAG_FRESTRICTED = (short)1,
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct PARAMDESC
    {
        public System.IntPtr lpVarValue;
        public System.Runtime.InteropServices.ComTypes.PARAMFLAG wParamFlags;
    }
    [System.FlagsAttribute]
    public enum PARAMFLAG : short
    {
        PARAMFLAG_FHASCUSTDATA = (short)64,
        PARAMFLAG_FHASDEFAULT = (short)32,
        PARAMFLAG_FIN = (short)1,
        PARAMFLAG_FLCID = (short)4,
        PARAMFLAG_FOPT = (short)16,
        PARAMFLAG_FOUT = (short)2,
        PARAMFLAG_FRETVAL = (short)8,
        PARAMFLAG_NONE = (short)0,
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct STATSTG
    {
        public System.Runtime.InteropServices.ComTypes.FILETIME atime;
        public long cbSize;
        public System.Guid clsid;
        public System.Runtime.InteropServices.ComTypes.FILETIME ctime;
        public int grfLocksSupported;
        public int grfMode;
        public int grfStateBits;
        public System.Runtime.InteropServices.ComTypes.FILETIME mtime;
        public string pwcsName;
        public int reserved;
        public int type;
    }
    public enum SYSKIND
    {
        SYS_MAC = 2,
        SYS_WIN16 = 0,
        SYS_WIN32 = 1,
        SYS_WIN64 = 3,
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct TYPEATTR
    {
        public short cbAlignment;
        public int cbSizeInstance;
        public short cbSizeVft;
        public short cFuncs;
        public short cImplTypes;
        public short cVars;
        public int dwReserved;
        public System.Guid guid;
        public System.Runtime.InteropServices.ComTypes.IDLDESC idldescType;
        public int lcid;
        public System.IntPtr lpstrSchema;
        public const int MEMBER_ID_NIL = -1;
        public int memidConstructor;
        public int memidDestructor;
        public System.Runtime.InteropServices.ComTypes.TYPEDESC tdescAlias;
        public System.Runtime.InteropServices.ComTypes.TYPEKIND typekind;
        public short wMajorVerNum;
        public short wMinorVerNum;
        public System.Runtime.InteropServices.ComTypes.TYPEFLAGS wTypeFlags;
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct TYPEDESC
    {
        public System.IntPtr lpValue;
        public short vt;
    }
    [System.FlagsAttribute]
    public enum TYPEFLAGS : short
    {
        TYPEFLAG_FAGGREGATABLE = (short)1024,
        TYPEFLAG_FAPPOBJECT = (short)1,
        TYPEFLAG_FCANCREATE = (short)2,
        TYPEFLAG_FCONTROL = (short)32,
        TYPEFLAG_FDISPATCHABLE = (short)4096,
        TYPEFLAG_FDUAL = (short)64,
        TYPEFLAG_FHIDDEN = (short)16,
        TYPEFLAG_FLICENSED = (short)4,
        TYPEFLAG_FNONEXTENSIBLE = (short)128,
        TYPEFLAG_FOLEAUTOMATION = (short)256,
        TYPEFLAG_FPREDECLID = (short)8,
        TYPEFLAG_FPROXY = (short)16384,
        TYPEFLAG_FREPLACEABLE = (short)2048,
        TYPEFLAG_FRESTRICTED = (short)512,
        TYPEFLAG_FREVERSEBIND = (short)8192,
    }
    public enum TYPEKIND
    {
        TKIND_ALIAS = 6,
        TKIND_COCLASS = 5,
        TKIND_DISPATCH = 4,
        TKIND_ENUM = 0,
        TKIND_INTERFACE = 3,
        TKIND_MAX = 8,
        TKIND_MODULE = 2,
        TKIND_RECORD = 1,
        TKIND_UNION = 7,
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct TYPELIBATTR
    {
        public System.Guid guid;
        public int lcid;
        public System.Runtime.InteropServices.ComTypes.SYSKIND syskind;
        public System.Runtime.InteropServices.ComTypes.LIBFLAGS wLibFlags;
        public short wMajorVerNum;
        public short wMinorVerNum;
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct VARDESC
    {
        public System.Runtime.InteropServices.ComTypes.VARDESC.DESCUNION desc;
        public System.Runtime.InteropServices.ComTypes.ELEMDESC elemdescVar;
        public string lpstrSchema;
        public int memid;
        public System.Runtime.InteropServices.ComTypes.VARKIND varkind;
        public short wVarFlags;
        [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Explicit)]
        public partial struct DESCUNION
        {
            [System.Runtime.InteropServices.FieldOffsetAttribute(0)]
            public System.IntPtr lpvarValue;
            [System.Runtime.InteropServices.FieldOffsetAttribute(0)]
            public int oInst;
        }
    }
    [System.FlagsAttribute]
    public enum VARFLAGS : short
    {
        VARFLAG_FBINDABLE = (short)4,
        VARFLAG_FDEFAULTBIND = (short)32,
        VARFLAG_FDEFAULTCOLLELEM = (short)256,
        VARFLAG_FDISPLAYBIND = (short)16,
        VARFLAG_FHIDDEN = (short)64,
        VARFLAG_FIMMEDIATEBIND = (short)4096,
        VARFLAG_FNONBROWSABLE = (short)1024,
        VARFLAG_FREADONLY = (short)1,
        VARFLAG_FREPLACEABLE = (short)2048,
        VARFLAG_FREQUESTEDIT = (short)8,
        VARFLAG_FRESTRICTED = (short)128,
        VARFLAG_FSOURCE = (short)2,
        VARFLAG_FUIDEFAULT = (short)512,
    }
    public enum VARKIND
    {
        VAR_CONST = 2,
        VAR_DISPATCH = 3,
        VAR_PERINSTANCE = 0,
        VAR_STATIC = 1,
    }
}
namespace System.Runtime.Loader
{
    [System.Security.SecuritySafeCriticalAttribute]
    public abstract partial class AssemblyLoadContext
    {
        protected AssemblyLoadContext() { }
        public static System.Runtime.Loader.AssemblyLoadContext Default { get { throw null; } }
        public event System.Func<System.Runtime.Loader.AssemblyLoadContext, System.Reflection.AssemblyName, System.Reflection.Assembly> Resolving { add { } remove { } }
        public event System.Action<System.Runtime.Loader.AssemblyLoadContext> Unloading { add { } remove { } }
        public static System.Reflection.AssemblyName GetAssemblyName(string assemblyPath) { throw null; }
        public static System.Runtime.Loader.AssemblyLoadContext GetLoadContext(System.Reflection.Assembly assembly) { throw null; }
        protected abstract System.Reflection.Assembly Load(System.Reflection.AssemblyName assemblyName);
        public System.Reflection.Assembly LoadFromAssemblyName(System.Reflection.AssemblyName assemblyName) { throw null; }
        public System.Reflection.Assembly LoadFromAssemblyPath(string assemblyPath) { throw null; }
        public System.Reflection.Assembly LoadFromNativeImagePath(string nativeImagePath, string assemblyPath) { throw null; }
        public System.Reflection.Assembly LoadFromStream(System.IO.Stream assembly) { throw null; }
        public System.Reflection.Assembly LoadFromStream(System.IO.Stream assembly, System.IO.Stream assemblySymbols) { throw null; }
        protected virtual System.IntPtr LoadUnmanagedDll(string unmanagedDllName) { throw null; }
        protected System.IntPtr LoadUnmanagedDllFromPath(string unmanagedDllPath) { throw null; }
        public void SetProfileOptimizationRoot(string directoryPath) { }
        public void StartProfileOptimization(string profile) { }
    }
}
namespace System.Runtime.Remoting
{
    [System.Runtime.InteropServices.ClassInterfaceAttribute((System.Runtime.InteropServices.ClassInterfaceType)(2))]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class ObjectHandle
    {
        internal ObjectHandle() { }
    }
}
namespace System.Runtime.Serialization
{
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public static partial class FormatterServices
    {
        [System.Security.SecurityCriticalAttribute]
        public static object GetUninitializedObject(System.Type type) { throw null; }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial interface IDeserializationCallback
    {
        void OnDeserialization(object sender);
    }
    [System.CLSCompliantAttribute(false)]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
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
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial interface IObjectReference
    {
        [System.Security.SecurityCriticalAttribute]
        object GetRealObject(System.Runtime.Serialization.StreamingContext context);
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial interface ISerializable
    {
        [System.Security.SecurityCriticalAttribute]
        void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context);
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(64), Inherited=false)]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class OnDeserializedAttribute : System.Attribute
    {
        public OnDeserializedAttribute() { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(64), Inherited=false)]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class OnDeserializingAttribute : System.Attribute
    {
        public OnDeserializingAttribute() { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(64), Inherited=false)]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class OnSerializedAttribute : System.Attribute
    {
        public OnSerializedAttribute() { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(64), Inherited=false)]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class OnSerializingAttribute : System.Attribute
    {
        public OnSerializingAttribute() { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(256), Inherited=false)]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class OptionalFieldAttribute : System.Attribute
    {
        public OptionalFieldAttribute() { }
        public int VersionAdded { get { throw null; } set { } }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct SerializationEntry
    {
        public string Name { get { throw null; } }
        public System.Type ObjectType { get { throw null; } }
        public object Value { get { throw null; } }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class SerializationException : System.SystemException
    {
        public SerializationException() { }
        public SerializationException(string message) { }
        public SerializationException(string message, System.Exception innerException) { }
        protected SerializationException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class SerializationInfo
    {
        [System.CLSCompliantAttribute(false)]
        public SerializationInfo(System.Type type, System.Runtime.Serialization.IFormatterConverter converter) { }
        [System.CLSCompliantAttribute(false)]
        public SerializationInfo(System.Type type, System.Runtime.Serialization.IFormatterConverter converter, bool requireSameTokenInPartialTrust) { }
        public string AssemblyName { get { throw null; } [System.Security.SecuritySafeCriticalAttribute]set { } }
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
        [System.Security.SecuritySafeCriticalAttribute]
        public object GetValue(string name, System.Type type) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public void SetType(System.Type type) { }
        public void UpdateValue(string name, object value, System.Type type) { }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
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
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct StreamingContext
    {
        public StreamingContext(System.Runtime.Serialization.StreamingContextStates state) { throw null;}
        public StreamingContext(System.Runtime.Serialization.StreamingContextStates state, object additional) { throw null;}
        public object Context { get { throw null; } }
        public System.Runtime.Serialization.StreamingContextStates State { get { throw null; } }
        public override bool Equals(object obj) { throw null; }
        public override int GetHashCode() { throw null; }
    }
    [System.FlagsAttribute]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
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
    [System.AttributeUsageAttribute((System.AttributeTargets)(5887), AllowMultiple=false, Inherited=false)]
    public sealed partial class ComponentGuaranteesAttribute : System.Attribute
    {
        public ComponentGuaranteesAttribute(System.Runtime.Versioning.ComponentGuaranteesOptions guarantees) { }
        public System.Runtime.Versioning.ComponentGuaranteesOptions Guarantees { get { throw null; } }
    }
    [System.FlagsAttribute]
    public enum ComponentGuaranteesOptions
    {
        Exchange = 1,
        None = 0,
        SideBySide = 4,
        Stable = 2,
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(480), Inherited=false)]
    [System.Diagnostics.ConditionalAttribute("RESOURCE_ANNOTATION_WORK")]
    public sealed partial class ResourceExposureAttribute : System.Attribute
    {
        public ResourceExposureAttribute(System.Runtime.Versioning.ResourceScope exposureLevel) { }
        public System.Runtime.Versioning.ResourceScope ResourceExposureLevel { get { throw null; } }
    }
    [System.FlagsAttribute]
    public enum ResourceScope
    {
        AppDomain = 4,
        Assembly = 32,
        Library = 8,
        Machine = 1,
        None = 0,
        Private = 16,
        Process = 2,
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(1), AllowMultiple=false, Inherited=false)]
    public sealed partial class TargetFrameworkAttribute : System.Attribute
    {
        public TargetFrameworkAttribute(string frameworkName) { }
        public string FrameworkDisplayName { get { throw null; } set { } }
        public string FrameworkName { get { throw null; } }
    }
    public static partial class VersioningHelper
    {
        public static string MakeVersionSafeName(string name, System.Runtime.Versioning.ResourceScope from, System.Runtime.Versioning.ResourceScope to) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public static string MakeVersionSafeName(string name, System.Runtime.Versioning.ResourceScope from, System.Runtime.Versioning.ResourceScope to, System.Type type) { throw null; }
    }
}
namespace System.Security
{
    [System.AttributeUsageAttribute((System.AttributeTargets)(1), AllowMultiple=false, Inherited=false)]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class AllowPartiallyTrustedCallersAttribute : System.Attribute
    {
        public AllowPartiallyTrustedCallersAttribute() { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(5501), AllowMultiple=false, Inherited=false)]
    public sealed partial class SecurityCriticalAttribute : System.Attribute
    {
        public SecurityCriticalAttribute() { }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class SecurityException : System.SystemException
    {
        public SecurityException() { }
        public SecurityException(string message) { }
        public SecurityException(string message, System.Exception inner) { }
        protected SecurityException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
        [System.Security.SecurityCriticalAttribute]
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public override string ToString() { throw null; }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(5500), AllowMultiple=false, Inherited=false)]
    public sealed partial class SecuritySafeCriticalAttribute : System.Attribute
    {
        public SecuritySafeCriticalAttribute() { }
    }
    [System.Security.SecurityCriticalAttribute]
    public abstract partial class SecurityState
    {
        protected SecurityState() { }
        public abstract void EnsureState();
        [System.Security.SecurityCriticalAttribute]
        public bool IsStateAvailable() { throw null; }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(1), AllowMultiple=false, Inherited=false)]
    public sealed partial class SecurityTransparentAttribute : System.Attribute
    {
        public SecurityTransparentAttribute() { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(2), AllowMultiple=true, Inherited=false)]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class UnverifiableCodeAttribute : System.Attribute
    {
        public UnverifiableCodeAttribute() { }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class VerificationException : System.SystemException
    {
        public VerificationException() { }
        public VerificationException(string message) { }
        public VerificationException(string message, System.Exception innerException) { }
        protected VerificationException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
namespace System.Security.Permissions
{
    [System.AttributeUsageAttribute((System.AttributeTargets)(109), AllowMultiple=true, Inherited=false)]
    [System.ObsoleteAttribute("CodeAccessSecurityAttribute is no longer accessible to application code.")]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public abstract partial class CodeAccessSecurityAttribute : System.Security.Permissions.SecurityAttribute
    {
        protected CodeAccessSecurityAttribute(System.Security.Permissions.SecurityAction action) : base (default(System.Security.Permissions.SecurityAction)) { }
    }
    [System.ObsoleteAttribute("SecurityAction is no longer accessible to application code.")]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public enum SecurityAction
    {
        Assert = 3,
        Demand = 2,
        [System.ObsoleteAttribute("Deny is obsolete and will be removed in a future release of the .NET Framework. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.")]
        Deny = 4,
        InheritanceDemand = 7,
        LinkDemand = 6,
        PermitOnly = 5,
        [System.ObsoleteAttribute("Assembly level declarative security is obsolete and is no longer enforced by the CLR by default. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.")]
        RequestMinimum = 8,
        [System.ObsoleteAttribute("Assembly level declarative security is obsolete and is no longer enforced by the CLR by default. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.")]
        RequestOptional = 9,
        [System.ObsoleteAttribute("Assembly level declarative security is obsolete and is no longer enforced by the CLR by default. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.")]
        RequestRefuse = 10,
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(109), AllowMultiple=true, Inherited=false)]
    [System.ObsoleteAttribute("SecurityAttribute is no longer accessible to application code.")]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public abstract partial class SecurityAttribute : System.Attribute
    {
        protected SecurityAttribute(System.Security.Permissions.SecurityAction action) { }
        public System.Security.Permissions.SecurityAction Action { get { throw null; } set { } }
        public bool Unrestricted { get { throw null; } set { } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(109), AllowMultiple=true, Inherited=false)]
    [System.ObsoleteAttribute("SecurityPermissionAttribute is no longer accessible to application code.")]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class SecurityPermissionAttribute : System.Security.Permissions.CodeAccessSecurityAttribute
    {
        public SecurityPermissionAttribute(System.Security.Permissions.SecurityAction action) : base (default(System.Security.Permissions.SecurityAction)) { }
        public bool ControlAppDomain { get { throw null; } set { } }
        public bool ControlDomainPolicy { get { throw null; } set { } }
        public bool ControlEvidence { get { throw null; } set { } }
        public bool ControlPolicy { get { throw null; } set { } }
        public bool ControlThread { get { throw null; } set { } }
        public System.Security.Permissions.SecurityPermissionFlag Flags { get { throw null; } set { } }
        public bool SkipVerification { get { throw null; } set { } }
        public bool UnmanagedCode { get { throw null; } set { } }
    }
    [System.FlagsAttribute]
    [System.ObsoleteAttribute("SecurityPermissionFlag is no longer accessible to application code.")]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public enum SecurityPermissionFlag
    {
        AllFlags = 16383,
        Assertion = 1,
        BindingRedirects = 8192,
        ControlAppDomain = 1024,
        ControlDomainPolicy = 256,
        ControlEvidence = 32,
        ControlPolicy = 64,
        ControlPrincipal = 512,
        ControlThread = 16,
        Execution = 8,
        Infrastructure = 4096,
        NoFlags = 0,
        RemotingConfiguration = 2048,
        SerializationFormatter = 128,
        SkipVerification = 4,
        UnmanagedCode = 2,
    }
}
namespace System.Security.Principal
{
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial interface IIdentity
    {
        string AuthenticationType { get; }
        bool IsAuthenticated { get; }
        string Name { get; }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial interface IPrincipal
    {
        System.Security.Principal.IIdentity Identity { get; }
        bool IsInRole(string role);
    }
    public enum TokenImpersonationLevel
    {
        Anonymous = 1,
        Delegation = 4,
        Identification = 2,
        Impersonation = 3,
        None = 0,
    }
}
namespace System.Text
{
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class ASCIIEncoding : System.Text.Encoding
    {
        public ASCIIEncoding() { }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public override bool IsSingleByte { get { throw null; } }
        [System.CLSCompliantAttribute(false)]
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        [System.Security.SecurityCriticalAttribute]
        public unsafe override int GetByteCount(char* chars, int count) { throw null; }
        public override int GetByteCount(char[] chars, int index, int count) { throw null; }
        public override int GetByteCount(string chars) { throw null; }
        [System.CLSCompliantAttribute(false)]
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        [System.Security.SecurityCriticalAttribute]
        public unsafe override int GetBytes(char* chars, int charCount, byte* bytes, int byteCount) { throw null; }
        public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex) { throw null; }
        public override int GetBytes(string chars, int charIndex, int charCount, byte[] bytes, int byteIndex) { throw null; }
        [System.CLSCompliantAttribute(false)]
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        [System.Security.SecurityCriticalAttribute]
        public unsafe override int GetCharCount(byte* bytes, int count) { throw null; }
        public override int GetCharCount(byte[] bytes, int index, int count) { throw null; }
        [System.CLSCompliantAttribute(false)]
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        [System.Security.SecurityCriticalAttribute]
        public unsafe override int GetChars(byte* bytes, int byteCount, char* chars, int charCount) { throw null; }
        public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex) { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public override System.Text.Decoder GetDecoder() { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public override System.Text.Encoder GetEncoder() { throw null; }
        public override int GetMaxByteCount(int charCount) { throw null; }
        public override int GetMaxCharCount(int byteCount) { throw null; }
        public override string GetString(byte[] bytes, int byteIndex, int byteCount) { throw null; }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public abstract partial class Decoder
    {
        protected Decoder() { }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public System.Text.DecoderFallback Fallback { get { throw null; } set { } }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public System.Text.DecoderFallbackBuffer FallbackBuffer { get { throw null; } }
        [System.CLSCompliantAttribute(false)]
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        [System.Security.SecurityCriticalAttribute]
        public unsafe virtual void Convert(byte* bytes, int byteCount, char* chars, int charCount, bool flush, out int bytesUsed, out int charsUsed, out bool completed) { bytesUsed = default(int); charsUsed = default(int); completed = default(bool); }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public virtual void Convert(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex, int charCount, bool flush, out int bytesUsed, out int charsUsed, out bool completed) { bytesUsed = default(int); charsUsed = default(int); completed = default(bool); }
        [System.CLSCompliantAttribute(false)]
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        [System.Security.SecurityCriticalAttribute]
        public unsafe virtual int GetCharCount(byte* bytes, int count, bool flush) { throw null; }
        public abstract int GetCharCount(byte[] bytes, int index, int count);
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public virtual int GetCharCount(byte[] bytes, int index, int count, bool flush) { throw null; }
        [System.CLSCompliantAttribute(false)]
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        [System.Security.SecurityCriticalAttribute]
        public unsafe virtual int GetChars(byte* bytes, int byteCount, char* chars, int charCount, bool flush) { throw null; }
        public abstract int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex);
        public virtual int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex, bool flush) { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
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
        [System.Security.SecuritySafeCriticalAttribute]
        public override void Reset() { }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public abstract partial class Encoder
    {
        protected Encoder() { }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public System.Text.EncoderFallback Fallback { get { throw null; } set { } }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public System.Text.EncoderFallbackBuffer FallbackBuffer { get { throw null; } }
        [System.CLSCompliantAttribute(false)]
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        [System.Security.SecurityCriticalAttribute]
        public unsafe virtual void Convert(char* chars, int charCount, byte* bytes, int byteCount, bool flush, out int charsUsed, out int bytesUsed, out bool completed) { charsUsed = default(int); bytesUsed = default(int); completed = default(bool); }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public virtual void Convert(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex, int byteCount, bool flush, out int charsUsed, out int bytesUsed, out bool completed) { charsUsed = default(int); bytesUsed = default(int); completed = default(bool); }
        [System.CLSCompliantAttribute(false)]
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        [System.Security.SecurityCriticalAttribute]
        public unsafe virtual int GetByteCount(char* chars, int count, bool flush) { throw null; }
        public abstract int GetByteCount(char[] chars, int index, int count, bool flush);
        [System.CLSCompliantAttribute(false)]
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        [System.Security.SecurityCriticalAttribute]
        public unsafe virtual int GetBytes(char* chars, int charCount, byte* bytes, int byteCount, bool flush) { throw null; }
        public abstract int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex, bool flush);
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
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
        [System.Security.SecuritySafeCriticalAttribute]
        public override void Reset() { }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public abstract partial class Encoding : System.ICloneable
    {
        protected Encoding() { }
        protected Encoding(int codePage) { }
        protected Encoding(int codePage, System.Text.EncoderFallback encoderFallback, System.Text.DecoderFallback decoderFallback) { }
        public static System.Text.Encoding ASCII { get { throw null; } }
        public static System.Text.Encoding BigEndianUnicode { get { throw null; } }
        public virtual string BodyName { get { throw null; } }
        public virtual int CodePage { get { throw null; } }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public System.Text.DecoderFallback DecoderFallback { get { throw null; } set { } }
        public static System.Text.Encoding Default { [System.Security.SecuritySafeCriticalAttribute]get { throw null; } }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public System.Text.EncoderFallback EncoderFallback { get { throw null; } set { } }
        public virtual string EncodingName { get { throw null; } }
        public virtual string HeaderName { get { throw null; } }
        public virtual bool IsBrowserDisplay { get { throw null; } }
        public virtual bool IsBrowserSave { get { throw null; } }
        public virtual bool IsMailNewsDisplay { get { throw null; } }
        public virtual bool IsMailNewsSave { get { throw null; } }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public bool IsReadOnly { get { throw null; } }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public virtual bool IsSingleByte { get { throw null; } }
        public static System.Text.Encoding Unicode { get { throw null; } }
        public static System.Text.Encoding UTF32 { get { throw null; } }
        public static System.Text.Encoding UTF7 { get { throw null; } }
        public static System.Text.Encoding UTF8 { get { throw null; } }
        public virtual string WebName { get { throw null; } }
        public virtual int WindowsCodePage { get { throw null; } }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public virtual object Clone() { throw null; }
        public static byte[] Convert(System.Text.Encoding srcEncoding, System.Text.Encoding dstEncoding, byte[] bytes) { throw null; }
        public static byte[] Convert(System.Text.Encoding srcEncoding, System.Text.Encoding dstEncoding, byte[] bytes, int index, int count) { throw null; }
        public override bool Equals(object value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        [System.Security.SecurityCriticalAttribute]
        public unsafe virtual int GetByteCount(char* chars, int count) { throw null; }
        public virtual int GetByteCount(char[] chars) { throw null; }
        public abstract int GetByteCount(char[] chars, int index, int count);
        public virtual int GetByteCount(string s) { throw null; }
        [System.CLSCompliantAttribute(false)]
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        [System.Security.SecurityCriticalAttribute]
        public unsafe virtual int GetBytes(char* chars, int charCount, byte* bytes, int byteCount) { throw null; }
        public virtual byte[] GetBytes(char[] chars) { throw null; }
        public virtual byte[] GetBytes(char[] chars, int index, int count) { throw null; }
        public abstract int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex);
        public virtual byte[] GetBytes(string s) { throw null; }
        public virtual int GetBytes(string s, int charIndex, int charCount, byte[] bytes, int byteIndex) { throw null; }
        [System.CLSCompliantAttribute(false)]
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        [System.Security.SecurityCriticalAttribute]
        public unsafe virtual int GetCharCount(byte* bytes, int count) { throw null; }
        public virtual int GetCharCount(byte[] bytes) { throw null; }
        public abstract int GetCharCount(byte[] bytes, int index, int count);
        [System.CLSCompliantAttribute(false)]
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        [System.Security.SecurityCriticalAttribute]
        public unsafe virtual int GetChars(byte* bytes, int byteCount, char* chars, int charCount) { throw null; }
        public virtual char[] GetChars(byte[] bytes) { throw null; }
        public virtual char[] GetChars(byte[] bytes, int index, int count) { throw null; }
        public abstract int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex);
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
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        [System.Security.SecurityCriticalAttribute]
        public unsafe string GetString(byte* bytes, int byteCount) { throw null; }
        public virtual string GetString(byte[] bytes) { throw null; }
        public virtual string GetString(byte[] bytes, int index, int count) { throw null; }
        public static void RegisterProvider(System.Text.EncodingProvider provider) { }
    }
    public sealed partial class EncodingInfo
    {
        internal EncodingInfo() { }
        public override bool Equals(object value) { throw null; }
        public override int GetHashCode() { throw null; }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public abstract partial class EncodingProvider
    {
        public EncodingProvider() { }
        public abstract System.Text.Encoding GetEncoding(int codepage);
        public virtual System.Text.Encoding GetEncoding(int codepage, System.Text.EncoderFallback encoderFallback, System.Text.DecoderFallback decoderFallback) { throw null; }
        public abstract System.Text.Encoding GetEncoding(string name);
        public virtual System.Text.Encoding GetEncoding(string name, System.Text.EncoderFallback encoderFallback, System.Text.DecoderFallback decoderFallback) { throw null; }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class StringBuilder
    {
        public StringBuilder() { }
        public StringBuilder(int capacity) { }
        public StringBuilder(int capacity, int maxCapacity) { }
        public StringBuilder(string value) { }
        public StringBuilder(string value, int capacity) { }
        [System.Security.SecuritySafeCriticalAttribute]
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
        [System.Security.SecurityCriticalAttribute]
        public unsafe System.Text.StringBuilder Append(char* value, int valueCount) { throw null; }
        public System.Text.StringBuilder Append(char value, int repeatCount) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public System.Text.StringBuilder Append(char[] value) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public System.Text.StringBuilder Append(char[] value, int startIndex, int charCount) { throw null; }
        public System.Text.StringBuilder Append(decimal value) { throw null; }
        public System.Text.StringBuilder Append(double value) { throw null; }
        public System.Text.StringBuilder Append(short value) { throw null; }
        public System.Text.StringBuilder Append(int value) { throw null; }
        public System.Text.StringBuilder Append(long value) { throw null; }
        public System.Text.StringBuilder Append(object value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public System.Text.StringBuilder Append(sbyte value) { throw null; }
        public System.Text.StringBuilder Append(float value) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public System.Text.StringBuilder Append(string value) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
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
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public System.Text.StringBuilder AppendLine() { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public System.Text.StringBuilder AppendLine(string value) { throw null; }
        public System.Text.StringBuilder Clear() { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        [System.Security.SecuritySafeCriticalAttribute]
        public void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count) { }
        public int EnsureCapacity(int capacity) { throw null; }
        public bool Equals(System.Text.StringBuilder sb) { throw null; }
        public System.Text.StringBuilder Insert(int index, bool value) { throw null; }
        public System.Text.StringBuilder Insert(int index, byte value) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public System.Text.StringBuilder Insert(int index, char value) { throw null; }
        public System.Text.StringBuilder Insert(int index, char[] value) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public System.Text.StringBuilder Insert(int index, char[] value, int startIndex, int charCount) { throw null; }
        public System.Text.StringBuilder Insert(int index, decimal value) { throw null; }
        public System.Text.StringBuilder Insert(int index, double value) { throw null; }
        public System.Text.StringBuilder Insert(int index, short value) { throw null; }
        public System.Text.StringBuilder Insert(int index, int value) { throw null; }
        public System.Text.StringBuilder Insert(int index, long value) { throw null; }
        public System.Text.StringBuilder Insert(int index, object value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public System.Text.StringBuilder Insert(int index, sbyte value) { throw null; }
        public System.Text.StringBuilder Insert(int index, float value) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public System.Text.StringBuilder Insert(int index, string value) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
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
        [System.Security.SecuritySafeCriticalAttribute]
        public override string ToString() { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public string ToString(int startIndex, int length) { throw null; }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class UnicodeEncoding : System.Text.Encoding
    {
        public UnicodeEncoding() { }
        public UnicodeEncoding(bool bigEndian, bool byteOrderMark) { }
        public UnicodeEncoding(bool bigEndian, bool byteOrderMark, bool throwOnInvalidBytes) { }
        public override bool Equals(object value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        [System.Security.SecurityCriticalAttribute]
        public unsafe override int GetByteCount(char* chars, int count) { throw null; }
        public override int GetByteCount(char[] chars, int index, int count) { throw null; }
        public override int GetByteCount(string s) { throw null; }
        [System.CLSCompliantAttribute(false)]
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        [System.Security.SecurityCriticalAttribute]
        public unsafe override int GetBytes(char* chars, int charCount, byte* bytes, int byteCount) { throw null; }
        public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex) { throw null; }
        public override int GetBytes(string s, int charIndex, int charCount, byte[] bytes, int byteIndex) { throw null; }
        [System.CLSCompliantAttribute(false)]
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        [System.Security.SecurityCriticalAttribute]
        public unsafe override int GetCharCount(byte* bytes, int count) { throw null; }
        public override int GetCharCount(byte[] bytes, int index, int count) { throw null; }
        [System.CLSCompliantAttribute(false)]
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        [System.Security.SecurityCriticalAttribute]
        public unsafe override int GetChars(byte* bytes, int byteCount, char* chars, int charCount) { throw null; }
        public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex) { throw null; }
        public override System.Text.Decoder GetDecoder() { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public override System.Text.Encoder GetEncoder() { throw null; }
        public override int GetHashCode() { throw null; }
        public override int GetMaxByteCount(int charCount) { throw null; }
        public override int GetMaxCharCount(int byteCount) { throw null; }
        public override byte[] GetPreamble() { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public override string GetString(byte[] bytes, int index, int count) { throw null; }
    }
    public sealed partial class UTF32Encoding : System.Text.Encoding
    {
        public UTF32Encoding() { }
        public UTF32Encoding(bool bigEndian, bool byteOrderMark) { }
        public UTF32Encoding(bool bigEndian, bool byteOrderMark, bool throwOnInvalidCharacters) { }
        public override bool Equals(object value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        [System.Security.SecurityCriticalAttribute]
        public unsafe override int GetByteCount(char* chars, int count) { throw null; }
        public override int GetByteCount(char[] chars, int index, int count) { throw null; }
        public override int GetByteCount(string s) { throw null; }
        [System.CLSCompliantAttribute(false)]
        [System.Security.SecurityCriticalAttribute]
        public unsafe override int GetBytes(char* chars, int charCount, byte* bytes, int byteCount) { throw null; }
        public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex) { throw null; }
        public override int GetBytes(string s, int charIndex, int charCount, byte[] bytes, int byteIndex) { throw null; }
        [System.CLSCompliantAttribute(false)]
        [System.Security.SecurityCriticalAttribute]
        public unsafe override int GetCharCount(byte* bytes, int count) { throw null; }
        public override int GetCharCount(byte[] bytes, int index, int count) { throw null; }
        [System.CLSCompliantAttribute(false)]
        [System.Security.SecurityCriticalAttribute]
        public unsafe override int GetChars(byte* bytes, int byteCount, char* chars, int charCount) { throw null; }
        public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex) { throw null; }
        public override System.Text.Decoder GetDecoder() { throw null; }
        public override System.Text.Encoder GetEncoder() { throw null; }
        public override int GetHashCode() { throw null; }
        public override int GetMaxByteCount(int charCount) { throw null; }
        public override int GetMaxCharCount(int byteCount) { throw null; }
        public override byte[] GetPreamble() { throw null; }
        public override string GetString(byte[] bytes, int index, int count) { throw null; }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class UTF7Encoding : System.Text.Encoding
    {
        public UTF7Encoding() { }
        public UTF7Encoding(bool allowOptionals) { }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public override bool Equals(object value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        [System.Security.SecurityCriticalAttribute]
        public unsafe override int GetByteCount(char* chars, int count) { throw null; }
        public override int GetByteCount(char[] chars, int index, int count) { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public override int GetByteCount(string s) { throw null; }
        [System.CLSCompliantAttribute(false)]
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        [System.Security.SecurityCriticalAttribute]
        public unsafe override int GetBytes(char* chars, int charCount, byte* bytes, int byteCount) { throw null; }
        public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex) { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public override int GetBytes(string s, int charIndex, int charCount, byte[] bytes, int byteIndex) { throw null; }
        [System.CLSCompliantAttribute(false)]
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        [System.Security.SecurityCriticalAttribute]
        public unsafe override int GetCharCount(byte* bytes, int count) { throw null; }
        public override int GetCharCount(byte[] bytes, int index, int count) { throw null; }
        [System.CLSCompliantAttribute(false)]
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        [System.Security.SecurityCriticalAttribute]
        public unsafe override int GetChars(byte* bytes, int byteCount, char* chars, int charCount) { throw null; }
        public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex) { throw null; }
        public override System.Text.Decoder GetDecoder() { throw null; }
        public override System.Text.Encoder GetEncoder() { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public override int GetHashCode() { throw null; }
        public override int GetMaxByteCount(int charCount) { throw null; }
        public override int GetMaxCharCount(int byteCount) { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public override string GetString(byte[] bytes, int index, int count) { throw null; }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class UTF8Encoding : System.Text.Encoding
    {
        public UTF8Encoding() { }
        public UTF8Encoding(bool encoderShouldEmitUTF8Identifier) { }
        public UTF8Encoding(bool encoderShouldEmitUTF8Identifier, bool throwOnInvalidBytes) { }
        public override bool Equals(object value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        [System.Security.SecurityCriticalAttribute]
        public unsafe override int GetByteCount(char* chars, int count) { throw null; }
        public override int GetByteCount(char[] chars, int index, int count) { throw null; }
        public override int GetByteCount(string chars) { throw null; }
        [System.CLSCompliantAttribute(false)]
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        [System.Security.SecurityCriticalAttribute]
        public unsafe override int GetBytes(char* chars, int charCount, byte* bytes, int byteCount) { throw null; }
        public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex) { throw null; }
        public override int GetBytes(string s, int charIndex, int charCount, byte[] bytes, int byteIndex) { throw null; }
        [System.CLSCompliantAttribute(false)]
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        [System.Security.SecurityCriticalAttribute]
        public unsafe override int GetCharCount(byte* bytes, int count) { throw null; }
        public override int GetCharCount(byte[] bytes, int index, int count) { throw null; }
        [System.CLSCompliantAttribute(false)]
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        [System.Security.SecurityCriticalAttribute]
        public unsafe override int GetChars(byte* bytes, int byteCount, char* chars, int charCount) { throw null; }
        public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex) { throw null; }
        public override System.Text.Decoder GetDecoder() { throw null; }
        public override System.Text.Encoder GetEncoder() { throw null; }
        public override int GetHashCode() { throw null; }
        public override int GetMaxByteCount(int charCount) { throw null; }
        public override int GetMaxCharCount(int byteCount) { throw null; }
        public override byte[] GetPreamble() { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public override string GetString(byte[] bytes, int index, int count) { throw null; }
    }
}
namespace System.Threading
{
    [System.Runtime.InteropServices.ComVisibleAttribute(false)]
    public partial class AbandonedMutexException : System.SystemException
    {
        public AbandonedMutexException() { }
        public AbandonedMutexException(int location, System.Threading.WaitHandle handle) { }
        public AbandonedMutexException(string message) { }
        public AbandonedMutexException(string message, System.Exception inner) { }
        public AbandonedMutexException(string message, System.Exception inner, int location, System.Threading.WaitHandle handle) { }
        public AbandonedMutexException(string message, int location, System.Threading.WaitHandle handle) { }
        protected AbandonedMutexException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
        public System.Threading.Mutex Mutex { get { throw null; } }
        public int MutexIndex { get { throw null; } }
    }
    public sealed partial class AsyncLocal<T>
    {
        public AsyncLocal() { }
        [System.Security.SecurityCriticalAttribute]
        public AsyncLocal(System.Action<System.Threading.AsyncLocalValueChangedArgs<T>> valueChangedHandler) { }
        public T Value { [System.Security.SecuritySafeCriticalAttribute]get { throw null; } [System.Security.SecuritySafeCriticalAttribute]set { } }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct AsyncLocalValueChangedArgs<T>
    {
        public T CurrentValue { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public T PreviousValue { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public bool ThreadContextChanged { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class AutoResetEvent : System.Threading.EventWaitHandle
    {
        public AutoResetEvent(bool initialState) : base (default(bool), default(System.Threading.EventResetMode)) { }
    }
    [System.Diagnostics.DebuggerDisplayAttribute("IsCancellationRequested = {IsCancellationRequested}")]
    [System.Runtime.InteropServices.ComVisibleAttribute(false)]
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct CancellationToken
    {
        public CancellationToken(bool canceled) { throw null;}
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
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct CancellationTokenRegistration : System.IDisposable, System.IEquatable<System.Threading.CancellationTokenRegistration>
    {
        public void Dispose() { }
        public override bool Equals(object obj) { throw null; }
        public bool Equals(System.Threading.CancellationTokenRegistration other) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Threading.CancellationTokenRegistration left, System.Threading.CancellationTokenRegistration right) { throw null; }
        public static bool operator !=(System.Threading.CancellationTokenRegistration left, System.Threading.CancellationTokenRegistration right) { throw null; }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(false)]
    public partial class CancellationTokenSource : System.IDisposable
    {
        public CancellationTokenSource() { }
        public CancellationTokenSource(int millisecondsDelay) { }
        public CancellationTokenSource(System.TimeSpan delay) { }
        public bool IsCancellationRequested { get { throw null; } }
        public System.Threading.CancellationToken Token { get { throw null; } }
        public void Cancel() { }
        public void Cancel(bool throwOnFirstException) { }
        public void CancelAfter(int millisecondsDelay) { }
        public void CancelAfter(System.TimeSpan delay) { }
        public static System.Threading.CancellationTokenSource CreateLinkedTokenSource(System.Threading.CancellationToken token1, System.Threading.CancellationToken token2) { throw null; }
        public static System.Threading.CancellationTokenSource CreateLinkedTokenSource(params System.Threading.CancellationToken[] tokens) { throw null; }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    [System.Security.SecurityCriticalAttribute]
    public delegate void ContextCallback(object state);
    [System.Diagnostics.DebuggerDisplayAttribute("Initial Count={InitialCount}, Current Count={CurrentCount}")]
    [System.Runtime.InteropServices.ComVisibleAttribute(false)]
    public partial class CountdownEvent : System.IDisposable
    {
        public CountdownEvent(int initialCount) { }
        public int CurrentCount { get { throw null; } }
        public int InitialCount { get { throw null; } }
        public bool IsSet { get { throw null; } }
        public System.Threading.WaitHandle WaitHandle { get { throw null; } }
        public void AddCount() { }
        public void AddCount(int signalCount) { }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        public void Reset() { }
        public void Reset(int count) { }
        public bool Signal() { throw null; }
        public bool Signal(int signalCount) { throw null; }
        public bool TryAddCount() { throw null; }
        public bool TryAddCount(int signalCount) { throw null; }
        public void Wait() { }
        public bool Wait(int millisecondsTimeout) { throw null; }
        public bool Wait(int millisecondsTimeout, System.Threading.CancellationToken cancellationToken) { throw null; }
        public void Wait(System.Threading.CancellationToken cancellationToken) { }
        public bool Wait(System.TimeSpan timeout) { throw null; }
        public bool Wait(System.TimeSpan timeout, System.Threading.CancellationToken cancellationToken) { throw null; }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(false)]
    public enum EventResetMode
    {
        AutoReset = 0,
        ManualReset = 1,
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class EventWaitHandle : System.Threading.WaitHandle
    {
        [System.Security.SecuritySafeCriticalAttribute]
        public EventWaitHandle(bool initialState, System.Threading.EventResetMode mode) { }
        [System.Security.SecurityCriticalAttribute]
        public EventWaitHandle(bool initialState, System.Threading.EventResetMode mode, string name) { }
        [System.Security.SecurityCriticalAttribute]
        public EventWaitHandle(bool initialState, System.Threading.EventResetMode mode, string name, out bool createdNew) { createdNew = default(bool); }
        [System.Security.SecurityCriticalAttribute]
        public static System.Threading.EventWaitHandle OpenExisting(string name) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public bool Reset() { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public bool Set() { throw null; }
        [System.Security.SecurityCriticalAttribute]
        public static bool TryOpenExisting(string name, out System.Threading.EventWaitHandle result) { result = default(System.Threading.EventWaitHandle); throw null; }
    }
    public sealed partial class ExecutionContext : System.IDisposable
    {
        internal ExecutionContext() { }
        [System.Security.SecuritySafeCriticalAttribute]
        public static System.Threading.ExecutionContext Capture() { throw null; }
        public void Dispose() { }
        [System.Runtime.ExceptionServices.HandleProcessCorruptedStateExceptionsAttribute]
        [System.Security.SecurityCriticalAttribute]
        public static void Run(System.Threading.ExecutionContext executionContext, System.Threading.ContextCallback callback, object state) { }
    }
    public static partial class Interlocked
    {
        public static int Add(ref int location1, int value) { throw null; }
        public static long Add(ref long location1, long value) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)][System.Security.SecuritySafeCriticalAttribute]
        public static double CompareExchange(ref double location1, double value, double comparand) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)][System.Security.SecuritySafeCriticalAttribute]
        public static int CompareExchange(ref int location1, int value, int comparand) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)][System.Security.SecuritySafeCriticalAttribute]
        public static long CompareExchange(ref long location1, long value, long comparand) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)][System.Security.SecuritySafeCriticalAttribute]
        public static System.IntPtr CompareExchange(ref System.IntPtr location1, System.IntPtr value, System.IntPtr comparand) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)][System.Security.SecuritySafeCriticalAttribute]
        public static object CompareExchange(ref object location1, object value, object comparand) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)][System.Security.SecuritySafeCriticalAttribute]
        public static float CompareExchange(ref float location1, float value, float comparand) { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        [System.Security.SecuritySafeCriticalAttribute]
        public static T CompareExchange<T>(ref T location1, T value, T comparand) where T : class { throw null; }
        public static int Decrement(ref int location) { throw null; }
        public static long Decrement(ref long location) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)][System.Security.SecuritySafeCriticalAttribute]
        public static double Exchange(ref double location1, double value) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)][System.Security.SecuritySafeCriticalAttribute]
        public static int Exchange(ref int location1, int value) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)][System.Security.SecuritySafeCriticalAttribute]
        public static long Exchange(ref long location1, long value) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)][System.Security.SecuritySafeCriticalAttribute]
        public static System.IntPtr Exchange(ref System.IntPtr location1, System.IntPtr value) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)][System.Security.SecuritySafeCriticalAttribute]
        public static object Exchange(ref object location1, object value) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)][System.Security.SecuritySafeCriticalAttribute]
        public static float Exchange(ref float location1, float value) { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        [System.Security.SecuritySafeCriticalAttribute]
        public static T Exchange<T>(ref T location1, T value) where T : class { throw null; }
        public static int Increment(ref int location) { throw null; }
        public static long Increment(ref long location) { throw null; }
        public static void MemoryBarrier() { }
        public static long Read(ref long location) { throw null; }
    }
    [System.CLSCompliantAttribute(false)]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    [System.Security.SecurityCriticalAttribute]
    public unsafe delegate void IOCompletionCallback(uint errorCode, uint numBytes, System.Threading.NativeOverlapped* pOVERLAP);
    public static partial class LazyInitializer
    {
        public static T EnsureInitialized<T>(ref T target) where T : class { throw null; }
        public static T EnsureInitialized<T>(ref T target, ref bool initialized, ref object syncLock) { throw null; }
        public static T EnsureInitialized<T>(ref T target, ref bool initialized, ref object syncLock, System.Func<T> valueFactory) { throw null; }
        public static T EnsureInitialized<T>(ref T target, System.Func<T> valueFactory) where T : class { throw null; }
    }
    public enum LazyThreadSafetyMode
    {
        ExecutionAndPublication = 2,
        None = 0,
        PublicationOnly = 1,
    }
    [System.Runtime.CompilerServices.TypeForwardedFromAttribute("System.Core, Version=3.5.0.0, Culture=Neutral, PublicKeyToken=b77a5c561934e089")]
    public partial class LockRecursionException : System.Exception
    {
        public LockRecursionException() { }
        public LockRecursionException(string message) { }
        public LockRecursionException(string message, System.Exception innerException) { }
        protected LockRecursionException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class ManualResetEvent : System.Threading.EventWaitHandle
    {
        public ManualResetEvent(bool initialState) : base (default(bool), default(System.Threading.EventResetMode)) { }
    }
    [System.Diagnostics.DebuggerDisplayAttribute("Set = {IsSet}")]
    [System.Runtime.InteropServices.ComVisibleAttribute(false)]
    public partial class ManualResetEventSlim : System.IDisposable
    {
        public ManualResetEventSlim() { }
        public ManualResetEventSlim(bool initialState) { }
        public ManualResetEventSlim(bool initialState, int spinCount) { }
        public bool IsSet { get { throw null; } }
        public int SpinCount { get { throw null; } }
        public System.Threading.WaitHandle WaitHandle { get { throw null; } }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        public void Reset() { }
        public void Set() { }
        public void Wait() { }
        public bool Wait(int millisecondsTimeout) { throw null; }
        public bool Wait(int millisecondsTimeout, System.Threading.CancellationToken cancellationToken) { throw null; }
        public void Wait(System.Threading.CancellationToken cancellationToken) { }
        public bool Wait(System.TimeSpan timeout) { throw null; }
        public bool Wait(System.TimeSpan timeout, System.Threading.CancellationToken cancellationToken) { throw null; }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public static partial class Monitor
    {
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)][System.Security.SecuritySafeCriticalAttribute]
        public static void Enter(object obj) { }
        public static void Enter(object obj, ref bool lockTaken) { }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)][System.Security.SecuritySafeCriticalAttribute]
        public static void Exit(object obj) { }
        [System.Security.SecuritySafeCriticalAttribute]
        public static bool IsEntered(object obj) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public static void Pulse(object obj) { }
        [System.Security.SecuritySafeCriticalAttribute]
        public static void PulseAll(object obj) { }
        public static bool TryEnter(object obj) { throw null; }
        public static void TryEnter(object obj, ref bool lockTaken) { }
        public static bool TryEnter(object obj, int millisecondsTimeout) { throw null; }
        public static void TryEnter(object obj, int millisecondsTimeout, ref bool lockTaken) { }
        public static bool TryEnter(object obj, System.TimeSpan timeout) { throw null; }
        public static void TryEnter(object obj, System.TimeSpan timeout, ref bool lockTaken) { }
        public static bool Wait(object obj) { throw null; }
        public static bool Wait(object obj, int millisecondsTimeout) { throw null; }
        public static bool Wait(object obj, System.TimeSpan timeout) { throw null; }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class Mutex : System.Threading.WaitHandle
    {
        [System.Security.SecuritySafeCriticalAttribute]
        public Mutex() { }
        [System.Security.SecuritySafeCriticalAttribute]
        public Mutex(bool initiallyOwned) { }
        [System.Security.SecurityCriticalAttribute]
        public Mutex(bool initiallyOwned, string name) { }
        [System.Security.SecurityCriticalAttribute]
        public Mutex(bool initiallyOwned, string name, out bool createdNew) { createdNew = default(bool); }
        [System.Security.SecurityCriticalAttribute]
        public static System.Threading.Mutex OpenExisting(string name) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public void ReleaseMutex() { }
        [System.Security.SecurityCriticalAttribute]
        public static bool TryOpenExisting(string name, out System.Threading.Mutex result) { result = default(System.Threading.Mutex); throw null; }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct NativeOverlapped
    {
        public System.IntPtr EventHandle;
        public System.IntPtr InternalHigh;
        public System.IntPtr InternalLow;
        public int OffsetHigh;
        public int OffsetLow;
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class Overlapped
    {
        [System.Security.SecuritySafeCriticalAttribute]
        public Overlapped() { }
        public System.IAsyncResult AsyncResult { get { throw null; } set { } }
        public int OffsetHigh { get { throw null; } set { } }
        public int OffsetLow { get { throw null; } set { } }
        [System.CLSCompliantAttribute(false)]
        [System.Security.SecurityCriticalAttribute]
        public unsafe static void Free(System.Threading.NativeOverlapped* nativeOverlappedPtr) { }
        [System.CLSCompliantAttribute(false)]
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        [System.Security.SecurityCriticalAttribute]
        public unsafe System.Threading.NativeOverlapped* Pack(System.Threading.IOCompletionCallback iocb, object userData) { throw null; }
        [System.CLSCompliantAttribute(false)]
        [System.Security.SecurityCriticalAttribute]
        public unsafe static System.Threading.Overlapped Unpack(System.Threading.NativeOverlapped* nativeOverlappedPtr) { throw null; }
        [System.CLSCompliantAttribute(false)]
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        [System.Security.SecurityCriticalAttribute]
        public unsafe System.Threading.NativeOverlapped* UnsafePack(System.Threading.IOCompletionCallback iocb, object userData) { throw null; }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(false)]
    public delegate void ParameterizedThreadStart(object obj);
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class RegisteredWaitHandle
    {
        internal RegisteredWaitHandle() { }
        [System.Runtime.InteropServices.ComVisibleAttribute(true)]
        [System.Security.SecuritySafeCriticalAttribute]
        public bool Unregister(System.Threading.WaitHandle waitObject) { throw null; }
    }
    public sealed partial class Semaphore : System.Threading.WaitHandle
    {
        [System.Security.SecuritySafeCriticalAttribute]
        public Semaphore(int initialCount, int maximumCount) { }
        [System.Security.SecurityCriticalAttribute]
        public Semaphore(int initialCount, int maximumCount, string name) { }
        [System.Security.SecurityCriticalAttribute]
        public Semaphore(int initialCount, int maximumCount, string name, out bool createdNew) { createdNew = default(bool); }
        [System.Security.SecurityCriticalAttribute]
        public static System.Threading.Semaphore OpenExisting(string name) { throw null; }
        public int Release() { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public int Release(int releaseCount) { throw null; }
        [System.Security.SecurityCriticalAttribute]
        public static bool TryOpenExisting(string name, out System.Threading.Semaphore result) { result = default(System.Threading.Semaphore); throw null; }
    }
    [System.Runtime.CompilerServices.TypeForwardedFromAttribute("System, Version=2.0.0.0, Culture=Neutral, PublicKeyToken=b77a5c561934e089")]
    [System.Runtime.InteropServices.ComVisibleAttribute(false)]
    public partial class SemaphoreFullException : System.SystemException
    {
        public SemaphoreFullException() { }
        public SemaphoreFullException(string message) { }
        public SemaphoreFullException(string message, System.Exception innerException) { }
        protected SemaphoreFullException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
    [System.Diagnostics.DebuggerDisplayAttribute("Current Count = {m_currentCount}")]
    [System.Runtime.InteropServices.ComVisibleAttribute(false)]
    public partial class SemaphoreSlim : System.IDisposable
    {
        public SemaphoreSlim(int initialCount) { }
        public SemaphoreSlim(int initialCount, int maxCount) { }
        public System.Threading.WaitHandle AvailableWaitHandle { get { throw null; } }
        public int CurrentCount { get { throw null; } }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        public int Release() { throw null; }
        public int Release(int releaseCount) { throw null; }
        public void Wait() { }
        public bool Wait(int millisecondsTimeout) { throw null; }
        public bool Wait(int millisecondsTimeout, System.Threading.CancellationToken cancellationToken) { throw null; }
        public void Wait(System.Threading.CancellationToken cancellationToken) { }
        public bool Wait(System.TimeSpan timeout) { throw null; }
        public bool Wait(System.TimeSpan timeout, System.Threading.CancellationToken cancellationToken) { throw null; }
        public System.Threading.Tasks.Task WaitAsync() { throw null; }
        public System.Threading.Tasks.Task<bool> WaitAsync(int millisecondsTimeout) { throw null; }
        public System.Threading.Tasks.Task<bool> WaitAsync(int millisecondsTimeout, System.Threading.CancellationToken cancellationToken) { throw null; }
        public System.Threading.Tasks.Task WaitAsync(System.Threading.CancellationToken cancellationToken) { throw null; }
        public System.Threading.Tasks.Task<bool> WaitAsync(System.TimeSpan timeout) { throw null; }
        public System.Threading.Tasks.Task<bool> WaitAsync(System.TimeSpan timeout, System.Threading.CancellationToken cancellationToken) { throw null; }
    }
    public delegate void SendOrPostCallback(object state);
    [System.Diagnostics.DebuggerDisplayAttribute("IsHeld = {IsHeld}")]
    [System.Runtime.InteropServices.ComVisibleAttribute(false)]
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct SpinLock
    {
        public SpinLock(bool enableThreadOwnerTracking) { throw null;}
        public bool IsHeld { get { throw null; } }
        public bool IsHeldByCurrentThread { get { throw null; } }
        public bool IsThreadOwnerTrackingEnabled { get { throw null; } }
        public void Enter(ref bool lockTaken) { }
        public void Exit() { }
        public void Exit(bool useMemoryBarrier) { }
        public void TryEnter(ref bool lockTaken) { }
        public void TryEnter(int millisecondsTimeout, ref bool lockTaken) { }
        public void TryEnter(System.TimeSpan timeout, ref bool lockTaken) { }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct SpinWait
    {
        public int Count { get { throw null; } }
        public bool NextSpinWillYield { get { throw null; } }
        public void Reset() { }
        public void SpinOnce() { }
        public static void SpinUntil(System.Func<bool> condition) { }
        public static bool SpinUntil(System.Func<bool> condition, int millisecondsTimeout) { throw null; }
        public static bool SpinUntil(System.Func<bool> condition, System.TimeSpan timeout) { throw null; }
    }
    public partial class SynchronizationContext
    {
        public SynchronizationContext() { }
        public static System.Threading.SynchronizationContext Current { get { throw null; } }
        public virtual System.Threading.SynchronizationContext CreateCopy() { throw null; }
        public virtual void OperationCompleted() { }
        public virtual void OperationStarted() { }
        public virtual void Post(System.Threading.SendOrPostCallback d, object state) { }
        public virtual void Send(System.Threading.SendOrPostCallback d, object state) { }
        [System.Security.SecurityCriticalAttribute]
        public static void SetSynchronizationContext(System.Threading.SynchronizationContext syncContext) { }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class SynchronizationLockException : System.SystemException
    {
        public SynchronizationLockException() { }
        public SynchronizationLockException(string message) { }
        public SynchronizationLockException(string message, System.Exception innerException) { }
        protected SynchronizationLockException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
    [System.Runtime.InteropServices.ClassInterfaceAttribute((System.Runtime.InteropServices.ClassInterfaceType)(0))]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class Thread : System.Runtime.ConstrainedExecution.CriticalFinalizerObject
    {
        [System.Security.SecuritySafeCriticalAttribute]
        public Thread(System.Threading.ParameterizedThreadStart start) { }
        [System.Security.SecuritySafeCriticalAttribute]
        public Thread(System.Threading.ThreadStart start) { }
        public System.Globalization.CultureInfo CurrentCulture { get { throw null; } [System.Security.SecuritySafeCriticalAttribute]set { } }
        public static System.Threading.Thread CurrentThread { [System.Security.SecuritySafeCriticalAttribute]get { throw null; } }
        public System.Globalization.CultureInfo CurrentUICulture { get { throw null; } [System.Security.SecuritySafeCriticalAttribute]set { } }
        public bool IsAlive { [System.Security.SecuritySafeCriticalAttribute][System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)]get { throw null; } }
        public bool IsBackground { [System.Security.SecuritySafeCriticalAttribute]get { throw null; } [System.Security.SecuritySafeCriticalAttribute]set { } }
        public int ManagedThreadId { [System.Security.SecuritySafeCriticalAttribute][System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)]get { throw null; } }
        public string Name { get { throw null; } [System.Security.SecuritySafeCriticalAttribute]set { } }
        public System.Threading.ThreadState ThreadState { [System.Security.SecuritySafeCriticalAttribute]get { throw null; } }
        ~Thread() { }
        [System.Security.SecuritySafeCriticalAttribute]
        public static System.AppDomain GetDomain() { throw null; }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public override int GetHashCode() { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public void Join() { }
        [System.Security.SecuritySafeCriticalAttribute]
        public bool Join(int millisecondsTimeout) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)][System.Security.SecuritySafeCriticalAttribute]
        public static void MemoryBarrier() { }
        [System.Security.SecuritySafeCriticalAttribute]
        public static void Sleep(int millisecondsTimeout) { }
        public static void Sleep(System.TimeSpan timeout) { }
        [System.Security.SecuritySafeCriticalAttribute]
        public static void SpinWait(int iterations) { }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public void Start() { }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public void Start(object parameter) { }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class ThreadAbortException : System.SystemException
    {
        internal ThreadAbortException() { }
    }
    [System.Diagnostics.DebuggerDisplayAttribute("IsValueCreated={IsValueCreated}, Value={ValueForDebugDisplay}, Count={ValuesCountForDebugDisplay}")]
    public partial class ThreadLocal<T> : System.IDisposable
    {
        public ThreadLocal() { }
        public ThreadLocal(bool trackAllValues) { }
        public ThreadLocal(System.Func<T> valueFactory) { }
        public ThreadLocal(System.Func<T> valueFactory, bool trackAllValues) { }
        public bool IsValueCreated { get { throw null; } }
        [System.Diagnostics.DebuggerBrowsableAttribute((System.Diagnostics.DebuggerBrowsableState)(0))]
        public T Value { get { throw null; } set { } }
        public System.Collections.Generic.IList<T> Values { get { throw null; } }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        ~ThreadLocal() { }
        public override string ToString() { throw null; }
    }
    public static partial class ThreadPool
    {
        [System.Security.SecurityCriticalAttribute]
        [System.Security.Permissions.SecurityPermissionAttribute(System.Security.Permissions.SecurityAction.Demand, Flags=(System.Security.Permissions.SecurityPermissionFlag)(2))]
        public static bool BindHandle(System.Runtime.InteropServices.SafeHandle osHandle) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public static void GetMaxThreads(out int workerThreads, out int completionPortThreads) { workerThreads = default(int); completionPortThreads = default(int); }
        [System.Security.SecuritySafeCriticalAttribute]
        public static void GetMinThreads(out int workerThreads, out int completionPortThreads) { workerThreads = default(int); completionPortThreads = default(int); }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)][System.Security.SecuritySafeCriticalAttribute]
        public static bool QueueUserWorkItem(System.Threading.WaitCallback callBack) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)][System.Security.SecuritySafeCriticalAttribute]
        public static bool QueueUserWorkItem(System.Threading.WaitCallback callBack, object state) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)][System.Security.SecuritySafeCriticalAttribute]
        public static System.Threading.RegisteredWaitHandle RegisterWaitForSingleObject(System.Threading.WaitHandle waitObject, System.Threading.WaitOrTimerCallback callBack, object state, int millisecondsTimeOutInterval, bool executeOnlyOnce) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)][System.Security.SecuritySafeCriticalAttribute]
        public static System.Threading.RegisteredWaitHandle RegisterWaitForSingleObject(System.Threading.WaitHandle waitObject, System.Threading.WaitOrTimerCallback callBack, object state, long millisecondsTimeOutInterval, bool executeOnlyOnce) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)][System.Security.SecuritySafeCriticalAttribute]
        public static System.Threading.RegisteredWaitHandle RegisterWaitForSingleObject(System.Threading.WaitHandle waitObject, System.Threading.WaitOrTimerCallback callBack, object state, System.TimeSpan timeout, bool executeOnlyOnce) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)][System.CLSCompliantAttribute(false)]
        [System.Security.SecuritySafeCriticalAttribute]
        public static System.Threading.RegisteredWaitHandle RegisterWaitForSingleObject(System.Threading.WaitHandle waitObject, System.Threading.WaitOrTimerCallback callBack, object state, uint millisecondsTimeOutInterval, bool executeOnlyOnce) { throw null; }
        [System.Security.SecurityCriticalAttribute]
        [System.Security.Permissions.SecurityPermissionAttribute(System.Security.Permissions.SecurityAction.Demand, ControlThread=true)]
        public static bool SetMaxThreads(int workerThreads, int completionPortThreads) { throw null; }
        [System.Security.SecurityCriticalAttribute]
        [System.Security.Permissions.SecurityPermissionAttribute(System.Security.Permissions.SecurityAction.Demand, ControlThread=true)]
        public static bool SetMinThreads(int workerThreads, int completionPortThreads) { throw null; }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public delegate void ThreadStart();
    public sealed partial class ThreadStartException : System.SystemException
    {
        internal ThreadStartException() { }
    }
    [System.FlagsAttribute]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public enum ThreadState
    {
        Aborted = 256,
        AbortRequested = 128,
        Background = 4,
        Running = 0,
        Stopped = 16,
        StopRequested = 1,
        Suspended = 64,
        SuspendRequested = 2,
        Unstarted = 8,
        WaitSleepJoin = 32,
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class ThreadStateException : System.SystemException
    {
        public ThreadStateException() { }
        public ThreadStateException(string message) { }
        public ThreadStateException(string message, System.Exception innerException) { }
        protected ThreadStateException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public static partial class Timeout
    {
        public const int Infinite = -1;
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public static readonly System.TimeSpan InfiniteTimeSpan;
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class Timer : System.IDisposable
    {
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)][System.Security.SecuritySafeCriticalAttribute]
        public Timer(System.Threading.TimerCallback callback) { }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)][System.Security.SecuritySafeCriticalAttribute]
        public Timer(System.Threading.TimerCallback callback, object state, int dueTime, int period) { }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)][System.Security.SecuritySafeCriticalAttribute]
        public Timer(System.Threading.TimerCallback callback, object state, long dueTime, long period) { }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)][System.Security.SecuritySafeCriticalAttribute]
        public Timer(System.Threading.TimerCallback callback, object state, System.TimeSpan dueTime, System.TimeSpan period) { }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)][System.CLSCompliantAttribute(false)]
        [System.Security.SecuritySafeCriticalAttribute]
        public Timer(System.Threading.TimerCallback callback, object state, uint dueTime, uint period) { }
        public bool Change(int dueTime, int period) { throw null; }
        public bool Change(long dueTime, long period) { throw null; }
        public bool Change(System.TimeSpan dueTime, System.TimeSpan period) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public bool Change(uint dueTime, uint period) { throw null; }
        public void Dispose() { }
        public bool Dispose(System.Threading.WaitHandle notifyObject) { throw null; }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public delegate void TimerCallback(object state);
    public static partial class Volatile
    {
        public static bool Read(ref bool location) { throw null; }
        public static byte Read(ref byte location) { throw null; }
        public static double Read(ref double location) { throw null; }
        public static short Read(ref short location) { throw null; }
        public static int Read(ref int location) { throw null; }
        public static long Read(ref long location) { throw null; }
        public static System.IntPtr Read(ref System.IntPtr location) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static sbyte Read(ref sbyte location) { throw null; }
        public static float Read(ref float location) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ushort Read(ref ushort location) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static uint Read(ref uint location) { throw null; }
        [System.CLSCompliantAttribute(false)]
        [System.Security.SecuritySafeCriticalAttribute]
        public static ulong Read(ref ulong location) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.UIntPtr Read(ref System.UIntPtr location) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public static T Read<T>(ref T location) where T : class { throw null; }
        public static void Write(ref bool location, bool value) { }
        public static void Write(ref byte location, byte value) { }
        public static void Write(ref double location, double value) { }
        public static void Write(ref short location, short value) { }
        public static void Write(ref int location, int value) { }
        public static void Write(ref long location, long value) { }
        public static void Write(ref System.IntPtr location, System.IntPtr value) { }
        [System.CLSCompliantAttribute(false)]
        public static void Write(ref sbyte location, sbyte value) { }
        public static void Write(ref float location, float value) { }
        [System.CLSCompliantAttribute(false)]
        public static void Write(ref ushort location, ushort value) { }
        [System.CLSCompliantAttribute(false)]
        public static void Write(ref uint location, uint value) { }
        [System.CLSCompliantAttribute(false)]
        [System.Security.SecuritySafeCriticalAttribute]
        public static void Write(ref ulong location, ulong value) { }
        [System.CLSCompliantAttribute(false)]
        public static void Write(ref System.UIntPtr location, System.UIntPtr value) { }
        [System.Security.SecuritySafeCriticalAttribute]
        public static void Write<T>(ref T location, T value) where T : class { }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public delegate void WaitCallback(object state);
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public abstract partial class WaitHandle : System.IDisposable
    {
        protected static readonly System.IntPtr InvalidHandle;
        public const int WaitTimeout = 258;
        protected WaitHandle() { }
        [System.ObsoleteAttribute("Use the SafeWaitHandle property instead.")]
        public virtual System.IntPtr Handle { [System.Security.SecuritySafeCriticalAttribute]get { return default(System.IntPtr); } [System.Security.SecurityCriticalAttribute]set { } }
        public Microsoft.Win32.SafeHandles.SafeWaitHandle SafeWaitHandle { [System.Security.SecurityCriticalAttribute]get { throw null; } [System.Security.SecurityCriticalAttribute]set { } }
        public virtual void Close() { }
        public void Dispose() { }
        [System.Security.SecuritySafeCriticalAttribute]
        protected virtual void Dispose(bool explicitDisposing) { }
        public static bool SignalAndWait(System.Threading.WaitHandle toSignal, System.Threading.WaitHandle toWaitOn) { return default(bool); }
        public static bool SignalAndWait(System.Threading.WaitHandle toSignal, System.Threading.WaitHandle toWaitOn, int millisecondsTimeout, bool exitContext) { return default(bool); }
        public static bool SignalAndWait(System.Threading.WaitHandle toSignal, System.Threading.WaitHandle toWaitOn, System.TimeSpan timeout, bool exitContext) { return default(bool); }
        public static bool WaitAll(System.Threading.WaitHandle[] waitHandles) { throw null; }
        public static bool WaitAll(System.Threading.WaitHandle[] waitHandles, int millisecondsTimeout) { throw null; }
        public static bool WaitAll(System.Threading.WaitHandle[] waitHandles, int millisecondsTimeout, bool exitContext) { return default(bool); }
        public static bool WaitAll(System.Threading.WaitHandle[] waitHandles, System.TimeSpan timeout) { throw null; }
        public static bool WaitAll(System.Threading.WaitHandle[] waitHandles, System.TimeSpan timeout, bool exitContext) { return default(bool); }
        public static int WaitAny(System.Threading.WaitHandle[] waitHandles) { throw null; }
        public static int WaitAny(System.Threading.WaitHandle[] waitHandles, int millisecondsTimeout) { throw null; }
        public static int WaitAny(System.Threading.WaitHandle[] waitHandles, int millisecondsTimeout, bool exitContext) { return default(int); }
        public static int WaitAny(System.Threading.WaitHandle[] waitHandles, System.TimeSpan timeout) { throw null; }
        public static int WaitAny(System.Threading.WaitHandle[] waitHandles, System.TimeSpan timeout, bool exitContext) { return default(int); }
        public virtual bool WaitOne() { throw null; }
        public virtual bool WaitOne(int millisecondsTimeout) { throw null; }
        public virtual bool WaitOne(int millisecondsTimeout, bool exitContext) { return default(bool); }
        public virtual bool WaitOne(System.TimeSpan timeout) { throw null; }
        public virtual bool WaitOne(System.TimeSpan timeout, bool exitContext) { return default(bool); }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(false)]
    public partial class WaitHandleCannotBeOpenedException : System.Exception
    {
        public WaitHandleCannotBeOpenedException() { }
        public WaitHandleCannotBeOpenedException(string message) { }
        public WaitHandleCannotBeOpenedException(string message, System.Exception innerException) { }
        protected WaitHandleCannotBeOpenedException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
    public static partial class WaitHandleExtensions
    {
        [System.Security.SecurityCriticalAttribute]
        public static Microsoft.Win32.SafeHandles.SafeWaitHandle GetSafeWaitHandle(this System.Threading.WaitHandle waitHandle) { throw null; }
        [System.Security.SecurityCriticalAttribute]
        public static void SetSafeWaitHandle(this System.Threading.WaitHandle waitHandle, Microsoft.Win32.SafeHandles.SafeWaitHandle value) { }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public delegate void WaitOrTimerCallback(object state, bool timedOut);
}
namespace System.Threading.Tasks
{
    [System.Diagnostics.DebuggerDisplayAttribute("Concurrent={ConcurrentTaskCountForDebugger}, Exclusive={ExclusiveTaskCountForDebugger}, Mode={ModeForDebugger}")]
    public partial class ConcurrentExclusiveSchedulerPair
    {
        public ConcurrentExclusiveSchedulerPair() { }
        public ConcurrentExclusiveSchedulerPair(System.Threading.Tasks.TaskScheduler taskScheduler) { }
        public ConcurrentExclusiveSchedulerPair(System.Threading.Tasks.TaskScheduler taskScheduler, int maxConcurrencyLevel) { }
        public ConcurrentExclusiveSchedulerPair(System.Threading.Tasks.TaskScheduler taskScheduler, int maxConcurrencyLevel, int maxItemsPerTask) { }
        public System.Threading.Tasks.Task Completion { get { throw null; } }
        public System.Threading.Tasks.TaskScheduler ConcurrentScheduler { get { throw null; } }
        public System.Threading.Tasks.TaskScheduler ExclusiveScheduler { get { throw null; } }
        public void Complete() { }
    }
    [System.Diagnostics.DebuggerDisplayAttribute("Id = {Id}, Status = {Status}, Method = {DebuggerDisplayMethodDescription}")]
    public partial class Task : System.IAsyncResult, System.IDisposable
    {
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public Task(System.Action action) { }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public Task(System.Action action, System.Threading.CancellationToken cancellationToken) { }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public Task(System.Action action, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskCreationOptions creationOptions) { }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public Task(System.Action action, System.Threading.Tasks.TaskCreationOptions creationOptions) { }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public Task(System.Action<object> action, object state) { }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public Task(System.Action<object> action, object state, System.Threading.CancellationToken cancellationToken) { }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public Task(System.Action<object> action, object state, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskCreationOptions creationOptions) { }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public Task(System.Action<object> action, object state, System.Threading.Tasks.TaskCreationOptions creationOptions) { }
        public object AsyncState { get { throw null; } }
        public static System.Threading.Tasks.Task CompletedTask { get { throw null; } }
        public System.Threading.Tasks.TaskCreationOptions CreationOptions { get { throw null; } }
        public static System.Nullable<int> CurrentId { get { throw null; } }
        public System.AggregateException Exception { get { throw null; } }
        public static System.Threading.Tasks.TaskFactory Factory { get { throw null; } }
        public int Id { get { throw null; } }
        public bool IsCanceled { get { throw null; } }
        public bool IsCompleted { get { throw null; } }
        public bool IsFaulted { get { throw null; } }
        public System.Threading.Tasks.TaskStatus Status { get { throw null; } }
        System.Threading.WaitHandle System.IAsyncResult.AsyncWaitHandle { get { throw null; } }
        bool System.IAsyncResult.CompletedSynchronously { get { throw null; } }
        public System.Runtime.CompilerServices.ConfiguredTaskAwaitable ConfigureAwait(bool continueOnCapturedContext) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task ContinueWith(System.Action<System.Threading.Tasks.Task, object> continuationAction, object state) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task ContinueWith(System.Action<System.Threading.Tasks.Task, object> continuationAction, object state, System.Threading.CancellationToken cancellationToken) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task ContinueWith(System.Action<System.Threading.Tasks.Task, object> continuationAction, object state, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskContinuationOptions continuationOptions, System.Threading.Tasks.TaskScheduler scheduler) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task ContinueWith(System.Action<System.Threading.Tasks.Task, object> continuationAction, object state, System.Threading.Tasks.TaskContinuationOptions continuationOptions) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task ContinueWith(System.Action<System.Threading.Tasks.Task, object> continuationAction, object state, System.Threading.Tasks.TaskScheduler scheduler) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task ContinueWith(System.Action<System.Threading.Tasks.Task> continuationAction) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task ContinueWith(System.Action<System.Threading.Tasks.Task> continuationAction, System.Threading.CancellationToken cancellationToken) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task ContinueWith(System.Action<System.Threading.Tasks.Task> continuationAction, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskContinuationOptions continuationOptions, System.Threading.Tasks.TaskScheduler scheduler) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task ContinueWith(System.Action<System.Threading.Tasks.Task> continuationAction, System.Threading.Tasks.TaskContinuationOptions continuationOptions) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task ContinueWith(System.Action<System.Threading.Tasks.Task> continuationAction, System.Threading.Tasks.TaskScheduler scheduler) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task<TResult> ContinueWith<TResult>(System.Func<System.Threading.Tasks.Task, TResult> continuationFunction) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task<TResult> ContinueWith<TResult>(System.Func<System.Threading.Tasks.Task, TResult> continuationFunction, System.Threading.CancellationToken cancellationToken) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task<TResult> ContinueWith<TResult>(System.Func<System.Threading.Tasks.Task, TResult> continuationFunction, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskContinuationOptions continuationOptions, System.Threading.Tasks.TaskScheduler scheduler) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task<TResult> ContinueWith<TResult>(System.Func<System.Threading.Tasks.Task, TResult> continuationFunction, System.Threading.Tasks.TaskContinuationOptions continuationOptions) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task<TResult> ContinueWith<TResult>(System.Func<System.Threading.Tasks.Task, TResult> continuationFunction, System.Threading.Tasks.TaskScheduler scheduler) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task<TResult> ContinueWith<TResult>(System.Func<System.Threading.Tasks.Task, object, TResult> continuationFunction, object state) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task<TResult> ContinueWith<TResult>(System.Func<System.Threading.Tasks.Task, object, TResult> continuationFunction, object state, System.Threading.CancellationToken cancellationToken) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task<TResult> ContinueWith<TResult>(System.Func<System.Threading.Tasks.Task, object, TResult> continuationFunction, object state, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskContinuationOptions continuationOptions, System.Threading.Tasks.TaskScheduler scheduler) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task<TResult> ContinueWith<TResult>(System.Func<System.Threading.Tasks.Task, object, TResult> continuationFunction, object state, System.Threading.Tasks.TaskContinuationOptions continuationOptions) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task<TResult> ContinueWith<TResult>(System.Func<System.Threading.Tasks.Task, object, TResult> continuationFunction, object state, System.Threading.Tasks.TaskScheduler scheduler) { throw null; }
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
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public static System.Threading.Tasks.Task Run(System.Action action) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public static System.Threading.Tasks.Task Run(System.Action action, System.Threading.CancellationToken cancellationToken) { throw null; }
        public static System.Threading.Tasks.Task Run(System.Func<System.Threading.Tasks.Task> function) { throw null; }
        public static System.Threading.Tasks.Task Run(System.Func<System.Threading.Tasks.Task> function, System.Threading.CancellationToken cancellationToken) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public static System.Threading.Tasks.Task<TResult> Run<TResult>(System.Func<TResult> function) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public static System.Threading.Tasks.Task<TResult> Run<TResult>(System.Func<TResult> function, System.Threading.CancellationToken cancellationToken) { throw null; }
        public static System.Threading.Tasks.Task<TResult> Run<TResult>(System.Func<System.Threading.Tasks.Task<TResult>> function) { throw null; }
        public static System.Threading.Tasks.Task<TResult> Run<TResult>(System.Func<System.Threading.Tasks.Task<TResult>> function, System.Threading.CancellationToken cancellationToken) { throw null; }
        public void RunSynchronously() { }
        public void RunSynchronously(System.Threading.Tasks.TaskScheduler scheduler) { }
        public void Start() { }
        public void Start(System.Threading.Tasks.TaskScheduler scheduler) { }
        public void Wait() { }
        public bool Wait(int millisecondsTimeout) { throw null; }
        public bool Wait(int millisecondsTimeout, System.Threading.CancellationToken cancellationToken) { throw null; }
        public void Wait(System.Threading.CancellationToken cancellationToken) { }
        public bool Wait(System.TimeSpan timeout) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoOptimization)]public static void WaitAll(params System.Threading.Tasks.Task[] tasks) { }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoOptimization)]public static bool WaitAll(System.Threading.Tasks.Task[] tasks, int millisecondsTimeout) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoOptimization)]public static bool WaitAll(System.Threading.Tasks.Task[] tasks, int millisecondsTimeout, System.Threading.CancellationToken cancellationToken) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoOptimization)]public static void WaitAll(System.Threading.Tasks.Task[] tasks, System.Threading.CancellationToken cancellationToken) { }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoOptimization)]public static bool WaitAll(System.Threading.Tasks.Task[] tasks, System.TimeSpan timeout) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoOptimization)]public static int WaitAny(params System.Threading.Tasks.Task[] tasks) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoOptimization)]public static int WaitAny(System.Threading.Tasks.Task[] tasks, int millisecondsTimeout) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoOptimization)]public static int WaitAny(System.Threading.Tasks.Task[] tasks, int millisecondsTimeout, System.Threading.CancellationToken cancellationToken) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoOptimization)]public static int WaitAny(System.Threading.Tasks.Task[] tasks, System.Threading.CancellationToken cancellationToken) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoOptimization)]public static int WaitAny(System.Threading.Tasks.Task[] tasks, System.TimeSpan timeout) { throw null; }
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
    [System.Diagnostics.DebuggerDisplayAttribute("Id = {Id}, Status = {Status}, Method = {DebuggerDisplayMethodDescription}, Result = {DebuggerDisplayResultDescription}")]
    public partial class Task<TResult> : System.Threading.Tasks.Task
    {
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public Task(System.Func<TResult> function) : base (default(System.Action)) { }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public Task(System.Func<TResult> function, System.Threading.CancellationToken cancellationToken) : base (default(System.Action)) { }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public Task(System.Func<TResult> function, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskCreationOptions creationOptions) : base (default(System.Action)) { }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public Task(System.Func<TResult> function, System.Threading.Tasks.TaskCreationOptions creationOptions) : base (default(System.Action)) { }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public Task(System.Func<object, TResult> function, object state) : base (default(System.Action)) { }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public Task(System.Func<object, TResult> function, object state, System.Threading.CancellationToken cancellationToken) : base (default(System.Action)) { }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public Task(System.Func<object, TResult> function, object state, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskCreationOptions creationOptions) : base (default(System.Action)) { }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public Task(System.Func<object, TResult> function, object state, System.Threading.Tasks.TaskCreationOptions creationOptions) : base (default(System.Action)) { }
        public static new System.Threading.Tasks.TaskFactory<TResult> Factory { get { throw null; } }
        [System.Diagnostics.DebuggerBrowsableAttribute((System.Diagnostics.DebuggerBrowsableState)(0))]
        public TResult Result { get { throw null; } }
        public new System.Runtime.CompilerServices.ConfiguredTaskAwaitable<TResult> ConfigureAwait(bool continueOnCapturedContext) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task ContinueWith(System.Action<System.Threading.Tasks.Task<TResult>, object> continuationAction, object state) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task ContinueWith(System.Action<System.Threading.Tasks.Task<TResult>, object> continuationAction, object state, System.Threading.CancellationToken cancellationToken) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task ContinueWith(System.Action<System.Threading.Tasks.Task<TResult>, object> continuationAction, object state, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskContinuationOptions continuationOptions, System.Threading.Tasks.TaskScheduler scheduler) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task ContinueWith(System.Action<System.Threading.Tasks.Task<TResult>, object> continuationAction, object state, System.Threading.Tasks.TaskContinuationOptions continuationOptions) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task ContinueWith(System.Action<System.Threading.Tasks.Task<TResult>, object> continuationAction, object state, System.Threading.Tasks.TaskScheduler scheduler) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task ContinueWith(System.Action<System.Threading.Tasks.Task<TResult>> continuationAction) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task ContinueWith(System.Action<System.Threading.Tasks.Task<TResult>> continuationAction, System.Threading.CancellationToken cancellationToken) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task ContinueWith(System.Action<System.Threading.Tasks.Task<TResult>> continuationAction, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskContinuationOptions continuationOptions, System.Threading.Tasks.TaskScheduler scheduler) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task ContinueWith(System.Action<System.Threading.Tasks.Task<TResult>> continuationAction, System.Threading.Tasks.TaskContinuationOptions continuationOptions) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task ContinueWith(System.Action<System.Threading.Tasks.Task<TResult>> continuationAction, System.Threading.Tasks.TaskScheduler scheduler) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task<TNewResult> ContinueWith<TNewResult>(System.Func<System.Threading.Tasks.Task<TResult>, TNewResult> continuationFunction) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task<TNewResult> ContinueWith<TNewResult>(System.Func<System.Threading.Tasks.Task<TResult>, TNewResult> continuationFunction, System.Threading.CancellationToken cancellationToken) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task<TNewResult> ContinueWith<TNewResult>(System.Func<System.Threading.Tasks.Task<TResult>, TNewResult> continuationFunction, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskContinuationOptions continuationOptions, System.Threading.Tasks.TaskScheduler scheduler) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task<TNewResult> ContinueWith<TNewResult>(System.Func<System.Threading.Tasks.Task<TResult>, TNewResult> continuationFunction, System.Threading.Tasks.TaskContinuationOptions continuationOptions) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task<TNewResult> ContinueWith<TNewResult>(System.Func<System.Threading.Tasks.Task<TResult>, TNewResult> continuationFunction, System.Threading.Tasks.TaskScheduler scheduler) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task<TNewResult> ContinueWith<TNewResult>(System.Func<System.Threading.Tasks.Task<TResult>, object, TNewResult> continuationFunction, object state) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task<TNewResult> ContinueWith<TNewResult>(System.Func<System.Threading.Tasks.Task<TResult>, object, TNewResult> continuationFunction, object state, System.Threading.CancellationToken cancellationToken) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task<TNewResult> ContinueWith<TNewResult>(System.Func<System.Threading.Tasks.Task<TResult>, object, TNewResult> continuationFunction, object state, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskContinuationOptions continuationOptions, System.Threading.Tasks.TaskScheduler scheduler) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task<TNewResult> ContinueWith<TNewResult>(System.Func<System.Threading.Tasks.Task<TResult>, object, TNewResult> continuationFunction, object state, System.Threading.Tasks.TaskContinuationOptions continuationOptions) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task<TNewResult> ContinueWith<TNewResult>(System.Func<System.Threading.Tasks.Task<TResult>, object, TNewResult> continuationFunction, object state, System.Threading.Tasks.TaskScheduler scheduler) { throw null; }
        public new System.Runtime.CompilerServices.TaskAwaiter<TResult> GetAwaiter() { throw null; }
    }
    public partial class TaskCanceledException : System.OperationCanceledException
    {
        public TaskCanceledException() { }
        public TaskCanceledException(string message) { }
        public TaskCanceledException(string message, System.Exception innerException) { }
        public TaskCanceledException(System.Threading.Tasks.Task task) { }
        protected TaskCanceledException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
        public System.Threading.Tasks.Task Task { get { throw null; } }
    }
    public partial class TaskCompletionSource<TResult>
    {
        public TaskCompletionSource() { }
        public TaskCompletionSource(object state) { }
        public TaskCompletionSource(object state, System.Threading.Tasks.TaskCreationOptions creationOptions) { }
        public TaskCompletionSource(System.Threading.Tasks.TaskCreationOptions creationOptions) { }
        public System.Threading.Tasks.Task<TResult> Task { get { throw null; } }
        public void SetCanceled() { }
        public void SetException(System.Collections.Generic.IEnumerable<System.Exception> exceptions) { }
        public void SetException(System.Exception exception) { }
        public void SetResult(TResult result) { }
        public bool TrySetCanceled() { throw null; }
        public bool TrySetCanceled(System.Threading.CancellationToken cancellationToken) { throw null; }
        public bool TrySetException(System.Collections.Generic.IEnumerable<System.Exception> exceptions) { throw null; }
        public bool TrySetException(System.Exception exception) { throw null; }
        public bool TrySetResult(TResult result) { throw null; }
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
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task ContinueWhenAll(System.Threading.Tasks.Task[] tasks, System.Action<System.Threading.Tasks.Task[]> continuationAction) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task ContinueWhenAll(System.Threading.Tasks.Task[] tasks, System.Action<System.Threading.Tasks.Task[]> continuationAction, System.Threading.CancellationToken cancellationToken) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task ContinueWhenAll(System.Threading.Tasks.Task[] tasks, System.Action<System.Threading.Tasks.Task[]> continuationAction, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskContinuationOptions continuationOptions, System.Threading.Tasks.TaskScheduler scheduler) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task ContinueWhenAll(System.Threading.Tasks.Task[] tasks, System.Action<System.Threading.Tasks.Task[]> continuationAction, System.Threading.Tasks.TaskContinuationOptions continuationOptions) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task<TResult> ContinueWhenAll<TResult>(System.Threading.Tasks.Task[] tasks, System.Func<System.Threading.Tasks.Task[], TResult> continuationFunction) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task<TResult> ContinueWhenAll<TResult>(System.Threading.Tasks.Task[] tasks, System.Func<System.Threading.Tasks.Task[], TResult> continuationFunction, System.Threading.CancellationToken cancellationToken) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task<TResult> ContinueWhenAll<TResult>(System.Threading.Tasks.Task[] tasks, System.Func<System.Threading.Tasks.Task[], TResult> continuationFunction, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskContinuationOptions continuationOptions, System.Threading.Tasks.TaskScheduler scheduler) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task<TResult> ContinueWhenAll<TResult>(System.Threading.Tasks.Task[] tasks, System.Func<System.Threading.Tasks.Task[], TResult> continuationFunction, System.Threading.Tasks.TaskContinuationOptions continuationOptions) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task ContinueWhenAll<TAntecedentResult>(System.Threading.Tasks.Task<TAntecedentResult>[] tasks, System.Action<System.Threading.Tasks.Task<TAntecedentResult>[]> continuationAction) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task ContinueWhenAll<TAntecedentResult>(System.Threading.Tasks.Task<TAntecedentResult>[] tasks, System.Action<System.Threading.Tasks.Task<TAntecedentResult>[]> continuationAction, System.Threading.CancellationToken cancellationToken) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task ContinueWhenAll<TAntecedentResult>(System.Threading.Tasks.Task<TAntecedentResult>[] tasks, System.Action<System.Threading.Tasks.Task<TAntecedentResult>[]> continuationAction, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskContinuationOptions continuationOptions, System.Threading.Tasks.TaskScheduler scheduler) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task ContinueWhenAll<TAntecedentResult>(System.Threading.Tasks.Task<TAntecedentResult>[] tasks, System.Action<System.Threading.Tasks.Task<TAntecedentResult>[]> continuationAction, System.Threading.Tasks.TaskContinuationOptions continuationOptions) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task<TResult> ContinueWhenAll<TAntecedentResult, TResult>(System.Threading.Tasks.Task<TAntecedentResult>[] tasks, System.Func<System.Threading.Tasks.Task<TAntecedentResult>[], TResult> continuationFunction) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task<TResult> ContinueWhenAll<TAntecedentResult, TResult>(System.Threading.Tasks.Task<TAntecedentResult>[] tasks, System.Func<System.Threading.Tasks.Task<TAntecedentResult>[], TResult> continuationFunction, System.Threading.CancellationToken cancellationToken) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task<TResult> ContinueWhenAll<TAntecedentResult, TResult>(System.Threading.Tasks.Task<TAntecedentResult>[] tasks, System.Func<System.Threading.Tasks.Task<TAntecedentResult>[], TResult> continuationFunction, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskContinuationOptions continuationOptions, System.Threading.Tasks.TaskScheduler scheduler) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task<TResult> ContinueWhenAll<TAntecedentResult, TResult>(System.Threading.Tasks.Task<TAntecedentResult>[] tasks, System.Func<System.Threading.Tasks.Task<TAntecedentResult>[], TResult> continuationFunction, System.Threading.Tasks.TaskContinuationOptions continuationOptions) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task ContinueWhenAny(System.Threading.Tasks.Task[] tasks, System.Action<System.Threading.Tasks.Task> continuationAction) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task ContinueWhenAny(System.Threading.Tasks.Task[] tasks, System.Action<System.Threading.Tasks.Task> continuationAction, System.Threading.CancellationToken cancellationToken) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task ContinueWhenAny(System.Threading.Tasks.Task[] tasks, System.Action<System.Threading.Tasks.Task> continuationAction, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskContinuationOptions continuationOptions, System.Threading.Tasks.TaskScheduler scheduler) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task ContinueWhenAny(System.Threading.Tasks.Task[] tasks, System.Action<System.Threading.Tasks.Task> continuationAction, System.Threading.Tasks.TaskContinuationOptions continuationOptions) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task<TResult> ContinueWhenAny<TResult>(System.Threading.Tasks.Task[] tasks, System.Func<System.Threading.Tasks.Task, TResult> continuationFunction) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task<TResult> ContinueWhenAny<TResult>(System.Threading.Tasks.Task[] tasks, System.Func<System.Threading.Tasks.Task, TResult> continuationFunction, System.Threading.CancellationToken cancellationToken) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task<TResult> ContinueWhenAny<TResult>(System.Threading.Tasks.Task[] tasks, System.Func<System.Threading.Tasks.Task, TResult> continuationFunction, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskContinuationOptions continuationOptions, System.Threading.Tasks.TaskScheduler scheduler) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task<TResult> ContinueWhenAny<TResult>(System.Threading.Tasks.Task[] tasks, System.Func<System.Threading.Tasks.Task, TResult> continuationFunction, System.Threading.Tasks.TaskContinuationOptions continuationOptions) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task ContinueWhenAny<TAntecedentResult>(System.Threading.Tasks.Task<TAntecedentResult>[] tasks, System.Action<System.Threading.Tasks.Task<TAntecedentResult>> continuationAction) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task ContinueWhenAny<TAntecedentResult>(System.Threading.Tasks.Task<TAntecedentResult>[] tasks, System.Action<System.Threading.Tasks.Task<TAntecedentResult>> continuationAction, System.Threading.CancellationToken cancellationToken) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task ContinueWhenAny<TAntecedentResult>(System.Threading.Tasks.Task<TAntecedentResult>[] tasks, System.Action<System.Threading.Tasks.Task<TAntecedentResult>> continuationAction, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskContinuationOptions continuationOptions, System.Threading.Tasks.TaskScheduler scheduler) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task ContinueWhenAny<TAntecedentResult>(System.Threading.Tasks.Task<TAntecedentResult>[] tasks, System.Action<System.Threading.Tasks.Task<TAntecedentResult>> continuationAction, System.Threading.Tasks.TaskContinuationOptions continuationOptions) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task<TResult> ContinueWhenAny<TAntecedentResult, TResult>(System.Threading.Tasks.Task<TAntecedentResult>[] tasks, System.Func<System.Threading.Tasks.Task<TAntecedentResult>, TResult> continuationFunction) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task<TResult> ContinueWhenAny<TAntecedentResult, TResult>(System.Threading.Tasks.Task<TAntecedentResult>[] tasks, System.Func<System.Threading.Tasks.Task<TAntecedentResult>, TResult> continuationFunction, System.Threading.CancellationToken cancellationToken) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task<TResult> ContinueWhenAny<TAntecedentResult, TResult>(System.Threading.Tasks.Task<TAntecedentResult>[] tasks, System.Func<System.Threading.Tasks.Task<TAntecedentResult>, TResult> continuationFunction, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskContinuationOptions continuationOptions, System.Threading.Tasks.TaskScheduler scheduler) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task<TResult> ContinueWhenAny<TAntecedentResult, TResult>(System.Threading.Tasks.Task<TAntecedentResult>[] tasks, System.Func<System.Threading.Tasks.Task<TAntecedentResult>, TResult> continuationFunction, System.Threading.Tasks.TaskContinuationOptions continuationOptions) { throw null; }
        public System.Threading.Tasks.Task FromAsync(System.Func<System.AsyncCallback, object, System.IAsyncResult> beginMethod, System.Action<System.IAsyncResult> endMethod, object state) { throw null; }
        public System.Threading.Tasks.Task FromAsync(System.Func<System.AsyncCallback, object, System.IAsyncResult> beginMethod, System.Action<System.IAsyncResult> endMethod, object state, System.Threading.Tasks.TaskCreationOptions creationOptions) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task FromAsync(System.IAsyncResult asyncResult, System.Action<System.IAsyncResult> endMethod) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task FromAsync(System.IAsyncResult asyncResult, System.Action<System.IAsyncResult> endMethod, System.Threading.Tasks.TaskCreationOptions creationOptions) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task FromAsync(System.IAsyncResult asyncResult, System.Action<System.IAsyncResult> endMethod, System.Threading.Tasks.TaskCreationOptions creationOptions, System.Threading.Tasks.TaskScheduler scheduler) { throw null; }
        public System.Threading.Tasks.Task FromAsync<TArg1>(System.Func<TArg1, System.AsyncCallback, object, System.IAsyncResult> beginMethod, System.Action<System.IAsyncResult> endMethod, TArg1 arg1, object state) { throw null; }
        public System.Threading.Tasks.Task FromAsync<TArg1>(System.Func<TArg1, System.AsyncCallback, object, System.IAsyncResult> beginMethod, System.Action<System.IAsyncResult> endMethod, TArg1 arg1, object state, System.Threading.Tasks.TaskCreationOptions creationOptions) { throw null; }
        public System.Threading.Tasks.Task<TResult> FromAsync<TResult>(System.Func<System.AsyncCallback, object, System.IAsyncResult> beginMethod, System.Func<System.IAsyncResult, TResult> endMethod, object state) { throw null; }
        public System.Threading.Tasks.Task<TResult> FromAsync<TResult>(System.Func<System.AsyncCallback, object, System.IAsyncResult> beginMethod, System.Func<System.IAsyncResult, TResult> endMethod, object state, System.Threading.Tasks.TaskCreationOptions creationOptions) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task<TResult> FromAsync<TResult>(System.IAsyncResult asyncResult, System.Func<System.IAsyncResult, TResult> endMethod) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task<TResult> FromAsync<TResult>(System.IAsyncResult asyncResult, System.Func<System.IAsyncResult, TResult> endMethod, System.Threading.Tasks.TaskCreationOptions creationOptions) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task<TResult> FromAsync<TResult>(System.IAsyncResult asyncResult, System.Func<System.IAsyncResult, TResult> endMethod, System.Threading.Tasks.TaskCreationOptions creationOptions, System.Threading.Tasks.TaskScheduler scheduler) { throw null; }
        public System.Threading.Tasks.Task FromAsync<TArg1, TArg2>(System.Func<TArg1, TArg2, System.AsyncCallback, object, System.IAsyncResult> beginMethod, System.Action<System.IAsyncResult> endMethod, TArg1 arg1, TArg2 arg2, object state) { throw null; }
        public System.Threading.Tasks.Task FromAsync<TArg1, TArg2>(System.Func<TArg1, TArg2, System.AsyncCallback, object, System.IAsyncResult> beginMethod, System.Action<System.IAsyncResult> endMethod, TArg1 arg1, TArg2 arg2, object state, System.Threading.Tasks.TaskCreationOptions creationOptions) { throw null; }
        public System.Threading.Tasks.Task<TResult> FromAsync<TArg1, TResult>(System.Func<TArg1, System.AsyncCallback, object, System.IAsyncResult> beginMethod, System.Func<System.IAsyncResult, TResult> endMethod, TArg1 arg1, object state) { throw null; }
        public System.Threading.Tasks.Task<TResult> FromAsync<TArg1, TResult>(System.Func<TArg1, System.AsyncCallback, object, System.IAsyncResult> beginMethod, System.Func<System.IAsyncResult, TResult> endMethod, TArg1 arg1, object state, System.Threading.Tasks.TaskCreationOptions creationOptions) { throw null; }
        public System.Threading.Tasks.Task FromAsync<TArg1, TArg2, TArg3>(System.Func<TArg1, TArg2, TArg3, System.AsyncCallback, object, System.IAsyncResult> beginMethod, System.Action<System.IAsyncResult> endMethod, TArg1 arg1, TArg2 arg2, TArg3 arg3, object state) { throw null; }
        public System.Threading.Tasks.Task FromAsync<TArg1, TArg2, TArg3>(System.Func<TArg1, TArg2, TArg3, System.AsyncCallback, object, System.IAsyncResult> beginMethod, System.Action<System.IAsyncResult> endMethod, TArg1 arg1, TArg2 arg2, TArg3 arg3, object state, System.Threading.Tasks.TaskCreationOptions creationOptions) { throw null; }
        public System.Threading.Tasks.Task<TResult> FromAsync<TArg1, TArg2, TResult>(System.Func<TArg1, TArg2, System.AsyncCallback, object, System.IAsyncResult> beginMethod, System.Func<System.IAsyncResult, TResult> endMethod, TArg1 arg1, TArg2 arg2, object state) { throw null; }
        public System.Threading.Tasks.Task<TResult> FromAsync<TArg1, TArg2, TResult>(System.Func<TArg1, TArg2, System.AsyncCallback, object, System.IAsyncResult> beginMethod, System.Func<System.IAsyncResult, TResult> endMethod, TArg1 arg1, TArg2 arg2, object state, System.Threading.Tasks.TaskCreationOptions creationOptions) { throw null; }
        public System.Threading.Tasks.Task<TResult> FromAsync<TArg1, TArg2, TArg3, TResult>(System.Func<TArg1, TArg2, TArg3, System.AsyncCallback, object, System.IAsyncResult> beginMethod, System.Func<System.IAsyncResult, TResult> endMethod, TArg1 arg1, TArg2 arg2, TArg3 arg3, object state) { throw null; }
        public System.Threading.Tasks.Task<TResult> FromAsync<TArg1, TArg2, TArg3, TResult>(System.Func<TArg1, TArg2, TArg3, System.AsyncCallback, object, System.IAsyncResult> beginMethod, System.Func<System.IAsyncResult, TResult> endMethod, TArg1 arg1, TArg2 arg2, TArg3 arg3, object state, System.Threading.Tasks.TaskCreationOptions creationOptions) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task StartNew(System.Action action) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task StartNew(System.Action action, System.Threading.CancellationToken cancellationToken) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task StartNew(System.Action action, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskCreationOptions creationOptions, System.Threading.Tasks.TaskScheduler scheduler) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task StartNew(System.Action action, System.Threading.Tasks.TaskCreationOptions creationOptions) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task StartNew(System.Action<object> action, object state) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task StartNew(System.Action<object> action, object state, System.Threading.CancellationToken cancellationToken) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task StartNew(System.Action<object> action, object state, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskCreationOptions creationOptions, System.Threading.Tasks.TaskScheduler scheduler) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task StartNew(System.Action<object> action, object state, System.Threading.Tasks.TaskCreationOptions creationOptions) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task<TResult> StartNew<TResult>(System.Func<TResult> function) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task<TResult> StartNew<TResult>(System.Func<TResult> function, System.Threading.CancellationToken cancellationToken) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task<TResult> StartNew<TResult>(System.Func<TResult> function, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskCreationOptions creationOptions, System.Threading.Tasks.TaskScheduler scheduler) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task<TResult> StartNew<TResult>(System.Func<TResult> function, System.Threading.Tasks.TaskCreationOptions creationOptions) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task<TResult> StartNew<TResult>(System.Func<object, TResult> function, object state) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task<TResult> StartNew<TResult>(System.Func<object, TResult> function, object state, System.Threading.CancellationToken cancellationToken) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task<TResult> StartNew<TResult>(System.Func<object, TResult> function, object state, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskCreationOptions creationOptions, System.Threading.Tasks.TaskScheduler scheduler) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task<TResult> StartNew<TResult>(System.Func<object, TResult> function, object state, System.Threading.Tasks.TaskCreationOptions creationOptions) { throw null; }
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
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task<TResult> ContinueWhenAll(System.Threading.Tasks.Task[] tasks, System.Func<System.Threading.Tasks.Task[], TResult> continuationFunction) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task<TResult> ContinueWhenAll(System.Threading.Tasks.Task[] tasks, System.Func<System.Threading.Tasks.Task[], TResult> continuationFunction, System.Threading.CancellationToken cancellationToken) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task<TResult> ContinueWhenAll(System.Threading.Tasks.Task[] tasks, System.Func<System.Threading.Tasks.Task[], TResult> continuationFunction, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskContinuationOptions continuationOptions, System.Threading.Tasks.TaskScheduler scheduler) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task<TResult> ContinueWhenAll(System.Threading.Tasks.Task[] tasks, System.Func<System.Threading.Tasks.Task[], TResult> continuationFunction, System.Threading.Tasks.TaskContinuationOptions continuationOptions) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task<TResult> ContinueWhenAll<TAntecedentResult>(System.Threading.Tasks.Task<TAntecedentResult>[] tasks, System.Func<System.Threading.Tasks.Task<TAntecedentResult>[], TResult> continuationFunction) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task<TResult> ContinueWhenAll<TAntecedentResult>(System.Threading.Tasks.Task<TAntecedentResult>[] tasks, System.Func<System.Threading.Tasks.Task<TAntecedentResult>[], TResult> continuationFunction, System.Threading.CancellationToken cancellationToken) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task<TResult> ContinueWhenAll<TAntecedentResult>(System.Threading.Tasks.Task<TAntecedentResult>[] tasks, System.Func<System.Threading.Tasks.Task<TAntecedentResult>[], TResult> continuationFunction, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskContinuationOptions continuationOptions, System.Threading.Tasks.TaskScheduler scheduler) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task<TResult> ContinueWhenAll<TAntecedentResult>(System.Threading.Tasks.Task<TAntecedentResult>[] tasks, System.Func<System.Threading.Tasks.Task<TAntecedentResult>[], TResult> continuationFunction, System.Threading.Tasks.TaskContinuationOptions continuationOptions) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task<TResult> ContinueWhenAny(System.Threading.Tasks.Task[] tasks, System.Func<System.Threading.Tasks.Task, TResult> continuationFunction) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task<TResult> ContinueWhenAny(System.Threading.Tasks.Task[] tasks, System.Func<System.Threading.Tasks.Task, TResult> continuationFunction, System.Threading.CancellationToken cancellationToken) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task<TResult> ContinueWhenAny(System.Threading.Tasks.Task[] tasks, System.Func<System.Threading.Tasks.Task, TResult> continuationFunction, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskContinuationOptions continuationOptions, System.Threading.Tasks.TaskScheduler scheduler) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task<TResult> ContinueWhenAny(System.Threading.Tasks.Task[] tasks, System.Func<System.Threading.Tasks.Task, TResult> continuationFunction, System.Threading.Tasks.TaskContinuationOptions continuationOptions) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task<TResult> ContinueWhenAny<TAntecedentResult>(System.Threading.Tasks.Task<TAntecedentResult>[] tasks, System.Func<System.Threading.Tasks.Task<TAntecedentResult>, TResult> continuationFunction) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task<TResult> ContinueWhenAny<TAntecedentResult>(System.Threading.Tasks.Task<TAntecedentResult>[] tasks, System.Func<System.Threading.Tasks.Task<TAntecedentResult>, TResult> continuationFunction, System.Threading.CancellationToken cancellationToken) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task<TResult> ContinueWhenAny<TAntecedentResult>(System.Threading.Tasks.Task<TAntecedentResult>[] tasks, System.Func<System.Threading.Tasks.Task<TAntecedentResult>, TResult> continuationFunction, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskContinuationOptions continuationOptions, System.Threading.Tasks.TaskScheduler scheduler) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task<TResult> ContinueWhenAny<TAntecedentResult>(System.Threading.Tasks.Task<TAntecedentResult>[] tasks, System.Func<System.Threading.Tasks.Task<TAntecedentResult>, TResult> continuationFunction, System.Threading.Tasks.TaskContinuationOptions continuationOptions) { throw null; }
        public System.Threading.Tasks.Task<TResult> FromAsync(System.Func<System.AsyncCallback, object, System.IAsyncResult> beginMethod, System.Func<System.IAsyncResult, TResult> endMethod, object state) { throw null; }
        public System.Threading.Tasks.Task<TResult> FromAsync(System.Func<System.AsyncCallback, object, System.IAsyncResult> beginMethod, System.Func<System.IAsyncResult, TResult> endMethod, object state, System.Threading.Tasks.TaskCreationOptions creationOptions) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task<TResult> FromAsync(System.IAsyncResult asyncResult, System.Func<System.IAsyncResult, TResult> endMethod) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task<TResult> FromAsync(System.IAsyncResult asyncResult, System.Func<System.IAsyncResult, TResult> endMethod, System.Threading.Tasks.TaskCreationOptions creationOptions) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task<TResult> FromAsync(System.IAsyncResult asyncResult, System.Func<System.IAsyncResult, TResult> endMethod, System.Threading.Tasks.TaskCreationOptions creationOptions, System.Threading.Tasks.TaskScheduler scheduler) { throw null; }
        public System.Threading.Tasks.Task<TResult> FromAsync<TArg1>(System.Func<TArg1, System.AsyncCallback, object, System.IAsyncResult> beginMethod, System.Func<System.IAsyncResult, TResult> endMethod, TArg1 arg1, object state) { throw null; }
        public System.Threading.Tasks.Task<TResult> FromAsync<TArg1>(System.Func<TArg1, System.AsyncCallback, object, System.IAsyncResult> beginMethod, System.Func<System.IAsyncResult, TResult> endMethod, TArg1 arg1, object state, System.Threading.Tasks.TaskCreationOptions creationOptions) { throw null; }
        public System.Threading.Tasks.Task<TResult> FromAsync<TArg1, TArg2>(System.Func<TArg1, TArg2, System.AsyncCallback, object, System.IAsyncResult> beginMethod, System.Func<System.IAsyncResult, TResult> endMethod, TArg1 arg1, TArg2 arg2, object state) { throw null; }
        public System.Threading.Tasks.Task<TResult> FromAsync<TArg1, TArg2>(System.Func<TArg1, TArg2, System.AsyncCallback, object, System.IAsyncResult> beginMethod, System.Func<System.IAsyncResult, TResult> endMethod, TArg1 arg1, TArg2 arg2, object state, System.Threading.Tasks.TaskCreationOptions creationOptions) { throw null; }
        public System.Threading.Tasks.Task<TResult> FromAsync<TArg1, TArg2, TArg3>(System.Func<TArg1, TArg2, TArg3, System.AsyncCallback, object, System.IAsyncResult> beginMethod, System.Func<System.IAsyncResult, TResult> endMethod, TArg1 arg1, TArg2 arg2, TArg3 arg3, object state) { throw null; }
        public System.Threading.Tasks.Task<TResult> FromAsync<TArg1, TArg2, TArg3>(System.Func<TArg1, TArg2, TArg3, System.AsyncCallback, object, System.IAsyncResult> beginMethod, System.Func<System.IAsyncResult, TResult> endMethod, TArg1 arg1, TArg2 arg2, TArg3 arg3, object state, System.Threading.Tasks.TaskCreationOptions creationOptions) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task<TResult> StartNew(System.Func<TResult> function) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task<TResult> StartNew(System.Func<TResult> function, System.Threading.CancellationToken cancellationToken) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task<TResult> StartNew(System.Func<TResult> function, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskCreationOptions creationOptions, System.Threading.Tasks.TaskScheduler scheduler) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task<TResult> StartNew(System.Func<TResult> function, System.Threading.Tasks.TaskCreationOptions creationOptions) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task<TResult> StartNew(System.Func<object, TResult> function, object state) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task<TResult> StartNew(System.Func<object, TResult> function, object state, System.Threading.CancellationToken cancellationToken) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task<TResult> StartNew(System.Func<object, TResult> function, object state, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskCreationOptions creationOptions, System.Threading.Tasks.TaskScheduler scheduler) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]public System.Threading.Tasks.Task<TResult> StartNew(System.Func<object, TResult> function, object state, System.Threading.Tasks.TaskCreationOptions creationOptions) { throw null; }
    }
    [System.Diagnostics.DebuggerDisplayAttribute("Id={Id}")]
    public abstract partial class TaskScheduler
    {
        protected TaskScheduler() { }
        public static System.Threading.Tasks.TaskScheduler Current { get { throw null; } }
        public static System.Threading.Tasks.TaskScheduler Default { get { throw null; } }
        public int Id { get { throw null; } }
        public virtual int MaximumConcurrencyLevel { get { throw null; } }
        public static event System.EventHandler<System.Threading.Tasks.UnobservedTaskExceptionEventArgs> UnobservedTaskException { add { } remove { } }
        public static System.Threading.Tasks.TaskScheduler FromCurrentSynchronizationContext() { throw null; }
        [System.Security.SecurityCriticalAttribute]
        protected abstract System.Collections.Generic.IEnumerable<System.Threading.Tasks.Task> GetScheduledTasks();
        [System.Security.SecurityCriticalAttribute]
        protected internal abstract void QueueTask(System.Threading.Tasks.Task task);
        [System.Security.SecurityCriticalAttribute]
        protected internal virtual bool TryDequeue(System.Threading.Tasks.Task task) { throw null; }
        [System.Security.SecurityCriticalAttribute]
        protected bool TryExecuteTask(System.Threading.Tasks.Task task) { throw null; }
        [System.Security.SecurityCriticalAttribute]
        protected abstract bool TryExecuteTaskInline(System.Threading.Tasks.Task task, bool taskWasPreviouslyQueued);
    }
    public partial class TaskSchedulerException : System.Exception
    {
        public TaskSchedulerException() { }
        public TaskSchedulerException(System.Exception innerException) { }
        public TaskSchedulerException(string message) { }
        public TaskSchedulerException(string message, System.Exception innerException) { }
        protected TaskSchedulerException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
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
    public partial class UnobservedTaskExceptionEventArgs : System.EventArgs
    {
        public UnobservedTaskExceptionEventArgs(System.AggregateException exception) { }
        public System.AggregateException Exception { get { throw null; } }
        public bool Observed { get { throw null; } }
        public void SetObserved() { }
    }
}
