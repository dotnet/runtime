// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Runtime.CompilerServices.ExtensionAttribute))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Action))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Action<,>))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Action<,,>))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Action<,,,>))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Func<>))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Func<,>))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Func<,,>))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Func<,,,>))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Func<,,,,>))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.InvalidTimeZoneException))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Security.Cryptography.Aes))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.TimeZoneInfo))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.TimeZoneNotFoundException))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Threading.LazyThreadSafetyMode))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Lazy<>))]

namespace Microsoft.Win32.SafeHandles
{
    public sealed partial class SafeMemoryMappedFileHandle : Microsoft.Win32.SafeHandles.SafeHandleZeroOrMinusOneIsInvalid
    {
        public SafeMemoryMappedFileHandle(System.IntPtr preexistingHandle, bool ownsHandle) : base (default(bool)) { }
        protected override bool ReleaseHandle() { throw null; }
    }
    public sealed partial class SafeMemoryMappedViewHandle : System.Runtime.InteropServices.SafeBuffer
    {
        internal SafeMemoryMappedViewHandle() : base (default(bool)) { }
        protected override bool ReleaseHandle() { throw null; }
    }
}
namespace System
{
    public delegate void Action<in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, in T9, in T10>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10);
    public delegate void Action<in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, in T9, in T10, in T11>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11);
    public delegate void Action<in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, in T9, in T10, in T11, in T12>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12);
    public delegate void Action<in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, in T9, in T10, in T11, in T12, in T13>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13);
    public delegate void Action<in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, in T9, in T10, in T11, in T12, in T13, in T14>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14);
    public delegate void Action<in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, in T9, in T10, in T11, in T12, in T13, in T14, in T15>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, T15 arg15);
    public delegate void Action<in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, in T9, in T10, in T11, in T12, in T13, in T14, in T15, in T16>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, T15 arg15, T16 arg16);
    public delegate void Action<in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, in T9>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9);
    public delegate TResult Func<in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, in T9, out TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9);
    public delegate TResult Func<in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, in T9, in T10, out TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10);
    public delegate TResult Func<in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, in T9, in T10, in T11, out TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11);
    public delegate TResult Func<in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, in T9, in T10, in T11, in T12, out TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12);
    public delegate TResult Func<in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, in T9, in T10, in T11, in T12, in T13, out TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13);
    public delegate TResult Func<in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, in T9, in T10, in T11, in T12, in T13, in T14, out TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14);
    public delegate TResult Func<in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, in T9, in T10, in T11, in T12, in T13, in T14, in T15, out TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, T15 arg15);
    public delegate TResult Func<in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, in T9, in T10, in T11, in T12, in T13, in T14, in T15, in T16, out TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, T15 arg15, T16 arg16);
}
namespace System.Collections.Generic
{
    [System.Diagnostics.DebuggerDisplayAttribute("Count = {Count}")]
    [System.Security.Permissions.HostProtectionAttribute(System.Security.Permissions.SecurityAction.LinkDemand, MayLeakOnAbort=true)]
    public partial class HashSet<T> : System.Collections.Generic.ICollection<T>, System.Collections.Generic.IEnumerable<T>, System.Collections.Generic.IReadOnlyCollection<T>, System.Collections.Generic.ISet<T>, System.Collections.IEnumerable, System.Runtime.Serialization.IDeserializationCallback, System.Runtime.Serialization.ISerializable
    {
        public HashSet() { }
        public HashSet(System.Collections.Generic.IEnumerable<T> collection) { }
        public HashSet(System.Collections.Generic.IEnumerable<T> collection, System.Collections.Generic.IEqualityComparer<T> comparer) { }
        public HashSet(System.Collections.Generic.IEqualityComparer<T> comparer) { }
        protected HashSet(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public System.Collections.Generic.IEqualityComparer<T> Comparer { get { throw null; } }
        public int Count { get { throw null; } }
        bool System.Collections.Generic.ICollection<T>.IsReadOnly { get { throw null; } }
        public bool Add(T item) { throw null; }
        public void Clear() { }
        public bool Contains(T item) { throw null; }
        public void CopyTo(T[] array) { }
        public void CopyTo(T[] array, int arrayIndex) { }
        public void CopyTo(T[] array, int arrayIndex, int count) { }
        public static System.Collections.Generic.IEqualityComparer<System.Collections.Generic.HashSet<T>> CreateSetComparer() { throw null; }
        public void ExceptWith(System.Collections.Generic.IEnumerable<T> other) { }
        public System.Collections.Generic.HashSet<T>.Enumerator GetEnumerator() { throw null; }
        [System.Security.SecurityCriticalAttribute]
        [System.Security.Permissions.SecurityPermissionAttribute(System.Security.Permissions.SecurityAction.LinkDemand, Flags=(System.Security.Permissions.SecurityPermissionFlag)(128))]
        public virtual void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public void IntersectWith(System.Collections.Generic.IEnumerable<T> other) { }
        public bool IsProperSubsetOf(System.Collections.Generic.IEnumerable<T> other) { throw null; }
        public bool IsProperSupersetOf(System.Collections.Generic.IEnumerable<T> other) { throw null; }
        public bool IsSubsetOf(System.Collections.Generic.IEnumerable<T> other) { throw null; }
        public bool IsSupersetOf(System.Collections.Generic.IEnumerable<T> other) { throw null; }
        public virtual void OnDeserialization(object sender) { }
        public bool Overlaps(System.Collections.Generic.IEnumerable<T> other) { throw null; }
        public bool Remove(T item) { throw null; }
        public int RemoveWhere(System.Predicate<T> match) { throw null; }
        public bool SetEquals(System.Collections.Generic.IEnumerable<T> other) { throw null; }
        public void SymmetricExceptWith(System.Collections.Generic.IEnumerable<T> other) { }
        void System.Collections.Generic.ICollection<T>.Add(T item) { }
        System.Collections.Generic.IEnumerator<T> System.Collections.Generic.IEnumerable<T>.GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        public void TrimExcess() { }
        public void UnionWith(System.Collections.Generic.IEnumerable<T> other) { }
        [System.Security.Permissions.HostProtectionAttribute(System.Security.Permissions.SecurityAction.LinkDemand, MayLeakOnAbort=true)]
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
namespace System.Dynamic
{
    public abstract partial class BinaryOperationBinder : System.Dynamic.DynamicMetaObjectBinder
    {
        protected BinaryOperationBinder(System.Linq.Expressions.ExpressionType operation) { }
        public System.Linq.Expressions.ExpressionType Operation { get { throw null; } }
        public sealed override System.Type ReturnType { get { throw null; } }
        public sealed override System.Dynamic.DynamicMetaObject Bind(System.Dynamic.DynamicMetaObject target, System.Dynamic.DynamicMetaObject[] args) { throw null; }
        public System.Dynamic.DynamicMetaObject FallbackBinaryOperation(System.Dynamic.DynamicMetaObject target, System.Dynamic.DynamicMetaObject arg) { throw null; }
        public abstract System.Dynamic.DynamicMetaObject FallbackBinaryOperation(System.Dynamic.DynamicMetaObject target, System.Dynamic.DynamicMetaObject arg, System.Dynamic.DynamicMetaObject errorSuggestion);
    }
    [System.Diagnostics.DebuggerDisplayAttribute("{DebugView}")]
    public abstract partial class BindingRestrictions
    {
        internal BindingRestrictions() { }
        public static readonly System.Dynamic.BindingRestrictions Empty;
        public static System.Dynamic.BindingRestrictions Combine(System.Collections.Generic.IList<System.Dynamic.DynamicMetaObject> contributingObjects) { throw null; }
        public static System.Dynamic.BindingRestrictions GetExpressionRestriction(System.Linq.Expressions.Expression expression) { throw null; }
        public static System.Dynamic.BindingRestrictions GetInstanceRestriction(System.Linq.Expressions.Expression expression, object instance) { throw null; }
        public static System.Dynamic.BindingRestrictions GetTypeRestriction(System.Linq.Expressions.Expression expression, System.Type type) { throw null; }
        public System.Dynamic.BindingRestrictions Merge(System.Dynamic.BindingRestrictions restrictions) { throw null; }
        public System.Linq.Expressions.Expression ToExpression() { throw null; }
    }
    public sealed partial class CallInfo
    {
        public CallInfo(int argCount, System.Collections.Generic.IEnumerable<string> argNames) { }
        public CallInfo(int argCount, params string[] argNames) { }
        public int ArgumentCount { get { throw null; } }
        public System.Collections.ObjectModel.ReadOnlyCollection<string> ArgumentNames { get { throw null; } }
        public override bool Equals(object obj) { throw null; }
        public override int GetHashCode() { throw null; }
    }
    public abstract partial class ConvertBinder : System.Dynamic.DynamicMetaObjectBinder
    {
        protected ConvertBinder(System.Type type, bool @explicit) { }
        public bool Explicit { get { throw null; } }
        public sealed override System.Type ReturnType { get { throw null; } }
        public System.Type Type { get { throw null; } }
        public sealed override System.Dynamic.DynamicMetaObject Bind(System.Dynamic.DynamicMetaObject target, System.Dynamic.DynamicMetaObject[] args) { throw null; }
        public System.Dynamic.DynamicMetaObject FallbackConvert(System.Dynamic.DynamicMetaObject target) { throw null; }
        public abstract System.Dynamic.DynamicMetaObject FallbackConvert(System.Dynamic.DynamicMetaObject target, System.Dynamic.DynamicMetaObject errorSuggestion);
    }
    public abstract partial class CreateInstanceBinder : System.Dynamic.DynamicMetaObjectBinder
    {
        protected CreateInstanceBinder(System.Dynamic.CallInfo callInfo) { }
        public System.Dynamic.CallInfo CallInfo { get { throw null; } }
        public sealed override System.Type ReturnType { get { throw null; } }
        public sealed override System.Dynamic.DynamicMetaObject Bind(System.Dynamic.DynamicMetaObject target, System.Dynamic.DynamicMetaObject[] args) { throw null; }
        public System.Dynamic.DynamicMetaObject FallbackCreateInstance(System.Dynamic.DynamicMetaObject target, System.Dynamic.DynamicMetaObject[] args) { throw null; }
        public abstract System.Dynamic.DynamicMetaObject FallbackCreateInstance(System.Dynamic.DynamicMetaObject target, System.Dynamic.DynamicMetaObject[] args, System.Dynamic.DynamicMetaObject errorSuggestion);
    }
    public abstract partial class DeleteIndexBinder : System.Dynamic.DynamicMetaObjectBinder
    {
        protected DeleteIndexBinder(System.Dynamic.CallInfo callInfo) { }
        public System.Dynamic.CallInfo CallInfo { get { throw null; } }
        public sealed override System.Type ReturnType { get { throw null; } }
        public sealed override System.Dynamic.DynamicMetaObject Bind(System.Dynamic.DynamicMetaObject target, System.Dynamic.DynamicMetaObject[] args) { throw null; }
        public System.Dynamic.DynamicMetaObject FallbackDeleteIndex(System.Dynamic.DynamicMetaObject target, System.Dynamic.DynamicMetaObject[] indexes) { throw null; }
        public abstract System.Dynamic.DynamicMetaObject FallbackDeleteIndex(System.Dynamic.DynamicMetaObject target, System.Dynamic.DynamicMetaObject[] indexes, System.Dynamic.DynamicMetaObject errorSuggestion);
    }
    public abstract partial class DeleteMemberBinder : System.Dynamic.DynamicMetaObjectBinder
    {
        protected DeleteMemberBinder(string name, bool ignoreCase) { }
        public bool IgnoreCase { get { throw null; } }
        public string Name { get { throw null; } }
        public sealed override System.Type ReturnType { get { throw null; } }
        public sealed override System.Dynamic.DynamicMetaObject Bind(System.Dynamic.DynamicMetaObject target, System.Dynamic.DynamicMetaObject[] args) { throw null; }
        public System.Dynamic.DynamicMetaObject FallbackDeleteMember(System.Dynamic.DynamicMetaObject target) { throw null; }
        public abstract System.Dynamic.DynamicMetaObject FallbackDeleteMember(System.Dynamic.DynamicMetaObject target, System.Dynamic.DynamicMetaObject errorSuggestion);
    }
    public partial class DynamicMetaObject
    {
        public static readonly System.Dynamic.DynamicMetaObject[] EmptyMetaObjects;
        public DynamicMetaObject(System.Linq.Expressions.Expression expression, System.Dynamic.BindingRestrictions restrictions) { }
        public DynamicMetaObject(System.Linq.Expressions.Expression expression, System.Dynamic.BindingRestrictions restrictions, object value) { }
        public System.Linq.Expressions.Expression Expression { get { throw null; } }
        public bool HasValue { get { throw null; } }
        public System.Type LimitType { get { throw null; } }
        public System.Dynamic.BindingRestrictions Restrictions { get { throw null; } }
        public System.Type RuntimeType { get { throw null; } }
        public object Value { get { throw null; } }
        public virtual System.Dynamic.DynamicMetaObject BindBinaryOperation(System.Dynamic.BinaryOperationBinder binder, System.Dynamic.DynamicMetaObject arg) { throw null; }
        public virtual System.Dynamic.DynamicMetaObject BindConvert(System.Dynamic.ConvertBinder binder) { throw null; }
        public virtual System.Dynamic.DynamicMetaObject BindCreateInstance(System.Dynamic.CreateInstanceBinder binder, System.Dynamic.DynamicMetaObject[] args) { throw null; }
        public virtual System.Dynamic.DynamicMetaObject BindDeleteIndex(System.Dynamic.DeleteIndexBinder binder, System.Dynamic.DynamicMetaObject[] indexes) { throw null; }
        public virtual System.Dynamic.DynamicMetaObject BindDeleteMember(System.Dynamic.DeleteMemberBinder binder) { throw null; }
        public virtual System.Dynamic.DynamicMetaObject BindGetIndex(System.Dynamic.GetIndexBinder binder, System.Dynamic.DynamicMetaObject[] indexes) { throw null; }
        public virtual System.Dynamic.DynamicMetaObject BindGetMember(System.Dynamic.GetMemberBinder binder) { throw null; }
        public virtual System.Dynamic.DynamicMetaObject BindInvoke(System.Dynamic.InvokeBinder binder, System.Dynamic.DynamicMetaObject[] args) { throw null; }
        public virtual System.Dynamic.DynamicMetaObject BindInvokeMember(System.Dynamic.InvokeMemberBinder binder, System.Dynamic.DynamicMetaObject[] args) { throw null; }
        public virtual System.Dynamic.DynamicMetaObject BindSetIndex(System.Dynamic.SetIndexBinder binder, System.Dynamic.DynamicMetaObject[] indexes, System.Dynamic.DynamicMetaObject value) { throw null; }
        public virtual System.Dynamic.DynamicMetaObject BindSetMember(System.Dynamic.SetMemberBinder binder, System.Dynamic.DynamicMetaObject value) { throw null; }
        public virtual System.Dynamic.DynamicMetaObject BindUnaryOperation(System.Dynamic.UnaryOperationBinder binder) { throw null; }
        public static System.Dynamic.DynamicMetaObject Create(object value, System.Linq.Expressions.Expression expression) { throw null; }
        public virtual System.Collections.Generic.IEnumerable<string> GetDynamicMemberNames() { throw null; }
    }
    public abstract partial class DynamicMetaObjectBinder : System.Runtime.CompilerServices.CallSiteBinder
    {
        protected DynamicMetaObjectBinder() { }
        public virtual System.Type ReturnType { get { throw null; } }
        public abstract System.Dynamic.DynamicMetaObject Bind(System.Dynamic.DynamicMetaObject target, System.Dynamic.DynamicMetaObject[] args);
        public sealed override System.Linq.Expressions.Expression Bind(object[] args, System.Collections.ObjectModel.ReadOnlyCollection<System.Linq.Expressions.ParameterExpression> parameters, System.Linq.Expressions.LabelTarget returnLabel) { throw null; }
        public System.Dynamic.DynamicMetaObject Defer(System.Dynamic.DynamicMetaObject target, params System.Dynamic.DynamicMetaObject[] args) { throw null; }
        public System.Dynamic.DynamicMetaObject Defer(params System.Dynamic.DynamicMetaObject[] args) { throw null; }
        public System.Linq.Expressions.Expression GetUpdateExpression(System.Type type) { throw null; }
    }
    public partial class DynamicObject : System.Dynamic.IDynamicMetaObjectProvider
    {
        protected DynamicObject() { }
        public virtual System.Collections.Generic.IEnumerable<string> GetDynamicMemberNames() { throw null; }
        public virtual System.Dynamic.DynamicMetaObject GetMetaObject(System.Linq.Expressions.Expression parameter) { throw null; }
        public virtual bool TryBinaryOperation(System.Dynamic.BinaryOperationBinder binder, object arg, out object result) { throw null; }
        public virtual bool TryConvert(System.Dynamic.ConvertBinder binder, out object result) { throw null; }
        public virtual bool TryCreateInstance(System.Dynamic.CreateInstanceBinder binder, object[] args, out object result) { throw null; }
        public virtual bool TryDeleteIndex(System.Dynamic.DeleteIndexBinder binder, object[] indexes) { throw null; }
        public virtual bool TryDeleteMember(System.Dynamic.DeleteMemberBinder binder) { throw null; }
        public virtual bool TryGetIndex(System.Dynamic.GetIndexBinder binder, object[] indexes, out object result) { throw null; }
        public virtual bool TryGetMember(System.Dynamic.GetMemberBinder binder, out object result) { throw null; }
        public virtual bool TryInvoke(System.Dynamic.InvokeBinder binder, object[] args, out object result) { throw null; }
        public virtual bool TryInvokeMember(System.Dynamic.InvokeMemberBinder binder, object[] args, out object result) { throw null; }
        public virtual bool TrySetIndex(System.Dynamic.SetIndexBinder binder, object[] indexes, object value) { throw null; }
        public virtual bool TrySetMember(System.Dynamic.SetMemberBinder binder, object value) { throw null; }
        public virtual bool TryUnaryOperation(System.Dynamic.UnaryOperationBinder binder, out object result) { throw null; }
    }
    public sealed partial class ExpandoObject : System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<string, object>>, System.Collections.Generic.IDictionary<string, object>, System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, object>>, System.Collections.IEnumerable, System.ComponentModel.INotifyPropertyChanged, System.Dynamic.IDynamicMetaObjectProvider
    {
        public ExpandoObject() { }
        int System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<string,object>>.Count { get { throw null; } }
        bool System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<string,object>>.IsReadOnly { get { throw null; } }
        object System.Collections.Generic.IDictionary<string,object>.this[string key] { get { throw null; } set { } }
        System.Collections.Generic.ICollection<string> System.Collections.Generic.IDictionary<string,object>.Keys { get { throw null; } }
        System.Collections.Generic.ICollection<object> System.Collections.Generic.IDictionary<string,object>.Values { get { throw null; } }
        event System.ComponentModel.PropertyChangedEventHandler System.ComponentModel.INotifyPropertyChanged.PropertyChanged { add { } remove { } }
        void System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<string,object>>.Add(System.Collections.Generic.KeyValuePair<string, object> item) { }
        void System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<string,object>>.Clear() { }
        bool System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<string,object>>.Contains(System.Collections.Generic.KeyValuePair<string, object> item) { throw null; }
        void System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<string,object>>.CopyTo(System.Collections.Generic.KeyValuePair<string, object>[] array, int arrayIndex) { }
        bool System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<string,object>>.Remove(System.Collections.Generic.KeyValuePair<string, object> item) { throw null; }
        void System.Collections.Generic.IDictionary<string,object>.Add(string key, object value) { }
        bool System.Collections.Generic.IDictionary<string,object>.ContainsKey(string key) { throw null; }
        bool System.Collections.Generic.IDictionary<string,object>.Remove(string key) { throw null; }
        bool System.Collections.Generic.IDictionary<string,object>.TryGetValue(string key, out object value) { throw null; }
        System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<string, object>> System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string,object>>.GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        System.Dynamic.DynamicMetaObject System.Dynamic.IDynamicMetaObjectProvider.GetMetaObject(System.Linq.Expressions.Expression parameter) { throw null; }
    }
    public abstract partial class GetIndexBinder : System.Dynamic.DynamicMetaObjectBinder
    {
        protected GetIndexBinder(System.Dynamic.CallInfo callInfo) { }
        public System.Dynamic.CallInfo CallInfo { get { throw null; } }
        public sealed override System.Type ReturnType { get { throw null; } }
        public sealed override System.Dynamic.DynamicMetaObject Bind(System.Dynamic.DynamicMetaObject target, System.Dynamic.DynamicMetaObject[] args) { throw null; }
        public System.Dynamic.DynamicMetaObject FallbackGetIndex(System.Dynamic.DynamicMetaObject target, System.Dynamic.DynamicMetaObject[] indexes) { throw null; }
        public abstract System.Dynamic.DynamicMetaObject FallbackGetIndex(System.Dynamic.DynamicMetaObject target, System.Dynamic.DynamicMetaObject[] indexes, System.Dynamic.DynamicMetaObject errorSuggestion);
    }
    public abstract partial class GetMemberBinder : System.Dynamic.DynamicMetaObjectBinder
    {
        protected GetMemberBinder(string name, bool ignoreCase) { }
        public bool IgnoreCase { get { throw null; } }
        public string Name { get { throw null; } }
        public sealed override System.Type ReturnType { get { throw null; } }
        public sealed override System.Dynamic.DynamicMetaObject Bind(System.Dynamic.DynamicMetaObject target, params System.Dynamic.DynamicMetaObject[] args) { throw null; }
        public System.Dynamic.DynamicMetaObject FallbackGetMember(System.Dynamic.DynamicMetaObject target) { throw null; }
        public abstract System.Dynamic.DynamicMetaObject FallbackGetMember(System.Dynamic.DynamicMetaObject target, System.Dynamic.DynamicMetaObject errorSuggestion);
    }
    public partial interface IDynamicMetaObjectProvider
    {
        System.Dynamic.DynamicMetaObject GetMetaObject(System.Linq.Expressions.Expression parameter);
    }
    public partial interface IInvokeOnGetBinder
    {
        bool InvokeOnGet { get; }
    }
    public abstract partial class InvokeBinder : System.Dynamic.DynamicMetaObjectBinder
    {
        protected InvokeBinder(System.Dynamic.CallInfo callInfo) { }
        public System.Dynamic.CallInfo CallInfo { get { throw null; } }
        public sealed override System.Type ReturnType { get { throw null; } }
        public sealed override System.Dynamic.DynamicMetaObject Bind(System.Dynamic.DynamicMetaObject target, System.Dynamic.DynamicMetaObject[] args) { throw null; }
        public System.Dynamic.DynamicMetaObject FallbackInvoke(System.Dynamic.DynamicMetaObject target, System.Dynamic.DynamicMetaObject[] args) { throw null; }
        public abstract System.Dynamic.DynamicMetaObject FallbackInvoke(System.Dynamic.DynamicMetaObject target, System.Dynamic.DynamicMetaObject[] args, System.Dynamic.DynamicMetaObject errorSuggestion);
    }
    public abstract partial class InvokeMemberBinder : System.Dynamic.DynamicMetaObjectBinder
    {
        protected InvokeMemberBinder(string name, bool ignoreCase, System.Dynamic.CallInfo callInfo) { }
        public System.Dynamic.CallInfo CallInfo { get { throw null; } }
        public bool IgnoreCase { get { throw null; } }
        public string Name { get { throw null; } }
        public sealed override System.Type ReturnType { get { throw null; } }
        public sealed override System.Dynamic.DynamicMetaObject Bind(System.Dynamic.DynamicMetaObject target, System.Dynamic.DynamicMetaObject[] args) { throw null; }
        public abstract System.Dynamic.DynamicMetaObject FallbackInvoke(System.Dynamic.DynamicMetaObject target, System.Dynamic.DynamicMetaObject[] args, System.Dynamic.DynamicMetaObject errorSuggestion);
        public System.Dynamic.DynamicMetaObject FallbackInvokeMember(System.Dynamic.DynamicMetaObject target, System.Dynamic.DynamicMetaObject[] args) { throw null; }
        public abstract System.Dynamic.DynamicMetaObject FallbackInvokeMember(System.Dynamic.DynamicMetaObject target, System.Dynamic.DynamicMetaObject[] args, System.Dynamic.DynamicMetaObject errorSuggestion);
    }
    public abstract partial class SetIndexBinder : System.Dynamic.DynamicMetaObjectBinder
    {
        protected SetIndexBinder(System.Dynamic.CallInfo callInfo) { }
        public System.Dynamic.CallInfo CallInfo { get { throw null; } }
        public sealed override System.Type ReturnType { get { throw null; } }
        public sealed override System.Dynamic.DynamicMetaObject Bind(System.Dynamic.DynamicMetaObject target, System.Dynamic.DynamicMetaObject[] args) { throw null; }
        public System.Dynamic.DynamicMetaObject FallbackSetIndex(System.Dynamic.DynamicMetaObject target, System.Dynamic.DynamicMetaObject[] indexes, System.Dynamic.DynamicMetaObject value) { throw null; }
        public abstract System.Dynamic.DynamicMetaObject FallbackSetIndex(System.Dynamic.DynamicMetaObject target, System.Dynamic.DynamicMetaObject[] indexes, System.Dynamic.DynamicMetaObject value, System.Dynamic.DynamicMetaObject errorSuggestion);
    }
    public abstract partial class SetMemberBinder : System.Dynamic.DynamicMetaObjectBinder
    {
        protected SetMemberBinder(string name, bool ignoreCase) { }
        public bool IgnoreCase { get { throw null; } }
        public string Name { get { throw null; } }
        public sealed override System.Type ReturnType { get { throw null; } }
        public sealed override System.Dynamic.DynamicMetaObject Bind(System.Dynamic.DynamicMetaObject target, System.Dynamic.DynamicMetaObject[] args) { throw null; }
        public System.Dynamic.DynamicMetaObject FallbackSetMember(System.Dynamic.DynamicMetaObject target, System.Dynamic.DynamicMetaObject value) { throw null; }
        public abstract System.Dynamic.DynamicMetaObject FallbackSetMember(System.Dynamic.DynamicMetaObject target, System.Dynamic.DynamicMetaObject value, System.Dynamic.DynamicMetaObject errorSuggestion);
    }
    public abstract partial class UnaryOperationBinder : System.Dynamic.DynamicMetaObjectBinder
    {
        protected UnaryOperationBinder(System.Linq.Expressions.ExpressionType operation) { }
        public System.Linq.Expressions.ExpressionType Operation { get { throw null; } }
        public sealed override System.Type ReturnType { get { throw null; } }
        public sealed override System.Dynamic.DynamicMetaObject Bind(System.Dynamic.DynamicMetaObject target, System.Dynamic.DynamicMetaObject[] args) { throw null; }
        public System.Dynamic.DynamicMetaObject FallbackUnaryOperation(System.Dynamic.DynamicMetaObject target) { throw null; }
        public abstract System.Dynamic.DynamicMetaObject FallbackUnaryOperation(System.Dynamic.DynamicMetaObject target, System.Dynamic.DynamicMetaObject errorSuggestion);
    }
}
namespace System.IO
{
    public enum HandleInheritability
    {
        Inheritable = 1,
        None = 0,
    }
}
namespace System.IO.MemoryMappedFiles
{
    public partial class MemoryMappedFile : System.IDisposable
    {
        internal MemoryMappedFile() { }
        public Microsoft.Win32.SafeHandles.SafeMemoryMappedFileHandle SafeMemoryMappedFileHandle { get { throw null; } }
        public static System.IO.MemoryMappedFiles.MemoryMappedFile CreateFromFile(System.IO.FileStream fileStream, string mapName, long capacity, System.IO.MemoryMappedFiles.MemoryMappedFileAccess access, System.IO.MemoryMappedFiles.MemoryMappedFileSecurity memoryMappedFileSecurity, System.IO.HandleInheritability inheritability, bool leaveOpen) { throw null; }
        public static System.IO.MemoryMappedFiles.MemoryMappedFile CreateFromFile(string path) { throw null; }
        public static System.IO.MemoryMappedFiles.MemoryMappedFile CreateFromFile(string path, System.IO.FileMode mode) { throw null; }
        public static System.IO.MemoryMappedFiles.MemoryMappedFile CreateFromFile(string path, System.IO.FileMode mode, string mapName) { throw null; }
        public static System.IO.MemoryMappedFiles.MemoryMappedFile CreateFromFile(string path, System.IO.FileMode mode, string mapName, long capacity) { throw null; }
        public static System.IO.MemoryMappedFiles.MemoryMappedFile CreateFromFile(string path, System.IO.FileMode mode, string mapName, long capacity, System.IO.MemoryMappedFiles.MemoryMappedFileAccess access) { throw null; }
        public static System.IO.MemoryMappedFiles.MemoryMappedFile CreateNew(string mapName, long capacity) { throw null; }
        public static System.IO.MemoryMappedFiles.MemoryMappedFile CreateNew(string mapName, long capacity, System.IO.MemoryMappedFiles.MemoryMappedFileAccess access) { throw null; }
        public static System.IO.MemoryMappedFiles.MemoryMappedFile CreateNew(string mapName, long capacity, System.IO.MemoryMappedFiles.MemoryMappedFileAccess access, System.IO.MemoryMappedFiles.MemoryMappedFileOptions options, System.IO.MemoryMappedFiles.MemoryMappedFileSecurity memoryMappedFileSecurity, System.IO.HandleInheritability inheritability) { throw null; }
        public static System.IO.MemoryMappedFiles.MemoryMappedFile CreateOrOpen(string mapName, long capacity) { throw null; }
        public static System.IO.MemoryMappedFiles.MemoryMappedFile CreateOrOpen(string mapName, long capacity, System.IO.MemoryMappedFiles.MemoryMappedFileAccess access) { throw null; }
        public static System.IO.MemoryMappedFiles.MemoryMappedFile CreateOrOpen(string mapName, long capacity, System.IO.MemoryMappedFiles.MemoryMappedFileAccess access, System.IO.MemoryMappedFiles.MemoryMappedFileOptions options, System.IO.MemoryMappedFiles.MemoryMappedFileSecurity memoryMappedFileSecurity, System.IO.HandleInheritability inheritability) { throw null; }
        public System.IO.MemoryMappedFiles.MemoryMappedViewAccessor CreateViewAccessor() { throw null; }
        public System.IO.MemoryMappedFiles.MemoryMappedViewAccessor CreateViewAccessor(long offset, long size) { throw null; }
        public System.IO.MemoryMappedFiles.MemoryMappedViewAccessor CreateViewAccessor(long offset, long size, System.IO.MemoryMappedFiles.MemoryMappedFileAccess access) { throw null; }
        public System.IO.MemoryMappedFiles.MemoryMappedViewStream CreateViewStream() { throw null; }
        public System.IO.MemoryMappedFiles.MemoryMappedViewStream CreateViewStream(long offset, long size) { throw null; }
        public System.IO.MemoryMappedFiles.MemoryMappedViewStream CreateViewStream(long offset, long size, System.IO.MemoryMappedFiles.MemoryMappedFileAccess access) { throw null; }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        public System.IO.MemoryMappedFiles.MemoryMappedFileSecurity GetAccessControl() { throw null; }
        public static System.IO.MemoryMappedFiles.MemoryMappedFile OpenExisting(string mapName) { throw null; }
        public static System.IO.MemoryMappedFiles.MemoryMappedFile OpenExisting(string mapName, System.IO.MemoryMappedFiles.MemoryMappedFileRights desiredAccessRights) { throw null; }
        public static System.IO.MemoryMappedFiles.MemoryMappedFile OpenExisting(string mapName, System.IO.MemoryMappedFiles.MemoryMappedFileRights desiredAccessRights, System.IO.HandleInheritability inheritability) { throw null; }
        public void SetAccessControl(System.IO.MemoryMappedFiles.MemoryMappedFileSecurity memoryMappedFileSecurity) { }
    }
    public enum MemoryMappedFileAccess
    {
        CopyOnWrite = 3,
        Read = 1,
        ReadExecute = 4,
        ReadWrite = 0,
        ReadWriteExecute = 5,
        Write = 2,
    }
    [System.FlagsAttribute]
    public enum MemoryMappedFileOptions
    {
        DelayAllocatePages = 67108864,
        None = 0,
    }
    [System.FlagsAttribute]
    public enum MemoryMappedFileRights
    {
        AccessSystemSecurity = 16777216,
        ChangePermissions = 262144,
        CopyOnWrite = 1,
        Delete = 65536,
        Execute = 8,
        FullControl = 983055,
        Read = 4,
        ReadExecute = 12,
        ReadPermissions = 131072,
        ReadWrite = 6,
        ReadWriteExecute = 14,
        TakeOwnership = 524288,
        Write = 2,
    }
    public partial class MemoryMappedFileSecurity : System.Security.AccessControl.ObjectSecurity<System.IO.MemoryMappedFiles.MemoryMappedFileRights>
    {
        public MemoryMappedFileSecurity() : base (default(bool), default(System.Security.AccessControl.ResourceType)) { }
    }
    public sealed partial class MemoryMappedViewAccessor : System.IO.UnmanagedMemoryAccessor
    {
        internal MemoryMappedViewAccessor() { }
        public long PointerOffset { get { throw null; } }
        public Microsoft.Win32.SafeHandles.SafeMemoryMappedViewHandle SafeMemoryMappedViewHandle { [System.Security.SecurityCriticalAttribute]get { throw null; } }
        [System.Security.SecuritySafeCriticalAttribute]
        protected override void Dispose(bool disposing) { }
        [System.Security.SecurityCriticalAttribute]
        public void Flush() { }
    }
    public sealed partial class MemoryMappedViewStream : System.IO.UnmanagedMemoryStream
    {
        internal MemoryMappedViewStream() { }
        public long PointerOffset { get { throw null; } }
        public Microsoft.Win32.SafeHandles.SafeMemoryMappedViewHandle SafeMemoryMappedViewHandle { [System.Security.SecurityCriticalAttribute]get { throw null; } }
        [System.Security.SecuritySafeCriticalAttribute]
        protected override void Dispose(bool disposing) { }
        [System.Security.SecurityCriticalAttribute]
        public override void Flush() { }
        public override void SetLength(long value) { }
    }
}
namespace System.Linq
{
    public static partial class Enumerable
    {
        public static TSource Aggregate<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, TSource, TSource> func) { throw null; }
        public static TAccumulate Aggregate<TSource, TAccumulate>(this System.Collections.Generic.IEnumerable<TSource> source, TAccumulate seed, System.Func<TAccumulate, TSource, TAccumulate> func) { throw null; }
        public static TResult Aggregate<TSource, TAccumulate, TResult>(this System.Collections.Generic.IEnumerable<TSource> source, TAccumulate seed, System.Func<TAccumulate, TSource, TAccumulate> func, System.Func<TAccumulate, TResult> resultSelector) { throw null; }
        public static bool All<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, bool> predicate) { throw null; }
        public static bool Any<TSource>(this System.Collections.Generic.IEnumerable<TSource> source) { throw null; }
        public static bool Any<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, bool> predicate) { throw null; }
        public static System.Collections.Generic.IEnumerable<TSource> AsEnumerable<TSource>(this System.Collections.Generic.IEnumerable<TSource> source) { throw null; }
        public static System.Collections.Generic.IEnumerable<TSource> Append<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, TSource element) { throw null; }
        public static System.Collections.Generic.IEnumerable<TSource> Prepend<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, TSource element) { throw null; }
        public static decimal Average(this System.Collections.Generic.IEnumerable<decimal> source) { throw null; }
        public static double Average(this System.Collections.Generic.IEnumerable<double> source) { throw null; }
        public static double Average(this System.Collections.Generic.IEnumerable<int> source) { throw null; }
        public static double Average(this System.Collections.Generic.IEnumerable<long> source) { throw null; }
        public static System.Nullable<decimal> Average(this System.Collections.Generic.IEnumerable<System.Nullable<decimal>> source) { throw null; }
        public static System.Nullable<double> Average(this System.Collections.Generic.IEnumerable<System.Nullable<double>> source) { throw null; }
        public static System.Nullable<double> Average(this System.Collections.Generic.IEnumerable<System.Nullable<int>> source) { throw null; }
        public static System.Nullable<double> Average(this System.Collections.Generic.IEnumerable<System.Nullable<long>> source) { throw null; }
        public static System.Nullable<float> Average(this System.Collections.Generic.IEnumerable<System.Nullable<float>> source) { throw null; }
        public static float Average(this System.Collections.Generic.IEnumerable<float> source) { throw null; }
        public static decimal Average<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, decimal> selector) { throw null; }
        public static double Average<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, double> selector) { throw null; }
        public static double Average<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, int> selector) { throw null; }
        public static double Average<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, long> selector) { throw null; }
        public static System.Nullable<decimal> Average<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Nullable<decimal>> selector) { throw null; }
        public static System.Nullable<double> Average<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Nullable<double>> selector) { throw null; }
        public static System.Nullable<double> Average<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Nullable<int>> selector) { throw null; }
        public static System.Nullable<double> Average<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Nullable<long>> selector) { throw null; }
        public static System.Nullable<float> Average<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Nullable<float>> selector) { throw null; }
        public static float Average<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, float> selector) { throw null; }
        public static System.Collections.Generic.IEnumerable<TResult> Cast<TResult>(this System.Collections.IEnumerable source) { throw null; }
        public static System.Collections.Generic.IEnumerable<TSource> Concat<TSource>(this System.Collections.Generic.IEnumerable<TSource> first, System.Collections.Generic.IEnumerable<TSource> second) { throw null; }
        public static bool Contains<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, TSource value) { throw null; }
        public static bool Contains<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, TSource value, System.Collections.Generic.IEqualityComparer<TSource> comparer) { throw null; }
        public static int Count<TSource>(this System.Collections.Generic.IEnumerable<TSource> source) { throw null; }
        public static int Count<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, bool> predicate) { throw null; }
        public static System.Collections.Generic.IEnumerable<TSource> DefaultIfEmpty<TSource>(this System.Collections.Generic.IEnumerable<TSource> source) { throw null; }
        public static System.Collections.Generic.IEnumerable<TSource> DefaultIfEmpty<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, TSource defaultValue) { throw null; }
        public static System.Collections.Generic.IEnumerable<TSource> Distinct<TSource>(this System.Collections.Generic.IEnumerable<TSource> source) { throw null; }
        public static System.Collections.Generic.IEnumerable<TSource> Distinct<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Collections.Generic.IEqualityComparer<TSource> comparer) { throw null; }
        public static TSource ElementAt<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, int index) { throw null; }
        public static TSource ElementAtOrDefault<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, int index) { throw null; }
        public static System.Collections.Generic.IEnumerable<TResult> Empty<TResult>() { throw null; }
        public static System.Collections.Generic.IEnumerable<TSource> Except<TSource>(this System.Collections.Generic.IEnumerable<TSource> first, System.Collections.Generic.IEnumerable<TSource> second) { throw null; }
        public static System.Collections.Generic.IEnumerable<TSource> Except<TSource>(this System.Collections.Generic.IEnumerable<TSource> first, System.Collections.Generic.IEnumerable<TSource> second, System.Collections.Generic.IEqualityComparer<TSource> comparer) { throw null; }
        public static TSource First<TSource>(this System.Collections.Generic.IEnumerable<TSource> source) { throw null; }
        public static TSource First<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, bool> predicate) { throw null; }
        public static TSource FirstOrDefault<TSource>(this System.Collections.Generic.IEnumerable<TSource> source) { throw null; }
        public static TSource FirstOrDefault<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, bool> predicate) { throw null; }
        public static System.Collections.Generic.IEnumerable<System.Linq.IGrouping<TKey, TSource>> GroupBy<TSource, TKey>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, TKey> keySelector) { throw null; }
        public static System.Collections.Generic.IEnumerable<System.Linq.IGrouping<TKey, TSource>> GroupBy<TSource, TKey>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, TKey> keySelector, System.Collections.Generic.IEqualityComparer<TKey> comparer) { throw null; }
        public static System.Collections.Generic.IEnumerable<System.Linq.IGrouping<TKey, TElement>> GroupBy<TSource, TKey, TElement>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, TKey> keySelector, System.Func<TSource, TElement> elementSelector) { throw null; }
        public static System.Collections.Generic.IEnumerable<System.Linq.IGrouping<TKey, TElement>> GroupBy<TSource, TKey, TElement>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, TKey> keySelector, System.Func<TSource, TElement> elementSelector, System.Collections.Generic.IEqualityComparer<TKey> comparer) { throw null; }
        public static System.Collections.Generic.IEnumerable<TResult> GroupBy<TSource, TKey, TResult>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, TKey> keySelector, System.Func<TKey, System.Collections.Generic.IEnumerable<TSource>, TResult> resultSelector) { throw null; }
        public static System.Collections.Generic.IEnumerable<TResult> GroupBy<TSource, TKey, TResult>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, TKey> keySelector, System.Func<TKey, System.Collections.Generic.IEnumerable<TSource>, TResult> resultSelector, System.Collections.Generic.IEqualityComparer<TKey> comparer) { throw null; }
        public static System.Collections.Generic.IEnumerable<TResult> GroupBy<TSource, TKey, TElement, TResult>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, TKey> keySelector, System.Func<TSource, TElement> elementSelector, System.Func<TKey, System.Collections.Generic.IEnumerable<TElement>, TResult> resultSelector) { throw null; }
        public static System.Collections.Generic.IEnumerable<TResult> GroupBy<TSource, TKey, TElement, TResult>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, TKey> keySelector, System.Func<TSource, TElement> elementSelector, System.Func<TKey, System.Collections.Generic.IEnumerable<TElement>, TResult> resultSelector, System.Collections.Generic.IEqualityComparer<TKey> comparer) { throw null; }
        public static System.Collections.Generic.IEnumerable<TResult> GroupJoin<TOuter, TInner, TKey, TResult>(this System.Collections.Generic.IEnumerable<TOuter> outer, System.Collections.Generic.IEnumerable<TInner> inner, System.Func<TOuter, TKey> outerKeySelector, System.Func<TInner, TKey> innerKeySelector, System.Func<TOuter, System.Collections.Generic.IEnumerable<TInner>, TResult> resultSelector) { throw null; }
        public static System.Collections.Generic.IEnumerable<TResult> GroupJoin<TOuter, TInner, TKey, TResult>(this System.Collections.Generic.IEnumerable<TOuter> outer, System.Collections.Generic.IEnumerable<TInner> inner, System.Func<TOuter, TKey> outerKeySelector, System.Func<TInner, TKey> innerKeySelector, System.Func<TOuter, System.Collections.Generic.IEnumerable<TInner>, TResult> resultSelector, System.Collections.Generic.IEqualityComparer<TKey> comparer) { throw null; }
        public static System.Collections.Generic.IEnumerable<TSource> Intersect<TSource>(this System.Collections.Generic.IEnumerable<TSource> first, System.Collections.Generic.IEnumerable<TSource> second) { throw null; }
        public static System.Collections.Generic.IEnumerable<TSource> Intersect<TSource>(this System.Collections.Generic.IEnumerable<TSource> first, System.Collections.Generic.IEnumerable<TSource> second, System.Collections.Generic.IEqualityComparer<TSource> comparer) { throw null; }
        public static System.Collections.Generic.IEnumerable<TResult> Join<TOuter, TInner, TKey, TResult>(this System.Collections.Generic.IEnumerable<TOuter> outer, System.Collections.Generic.IEnumerable<TInner> inner, System.Func<TOuter, TKey> outerKeySelector, System.Func<TInner, TKey> innerKeySelector, System.Func<TOuter, TInner, TResult> resultSelector) { throw null; }
        public static System.Collections.Generic.IEnumerable<TResult> Join<TOuter, TInner, TKey, TResult>(this System.Collections.Generic.IEnumerable<TOuter> outer, System.Collections.Generic.IEnumerable<TInner> inner, System.Func<TOuter, TKey> outerKeySelector, System.Func<TInner, TKey> innerKeySelector, System.Func<TOuter, TInner, TResult> resultSelector, System.Collections.Generic.IEqualityComparer<TKey> comparer) { throw null; }
        public static TSource Last<TSource>(this System.Collections.Generic.IEnumerable<TSource> source) { throw null; }
        public static TSource Last<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, bool> predicate) { throw null; }
        public static TSource LastOrDefault<TSource>(this System.Collections.Generic.IEnumerable<TSource> source) { throw null; }
        public static TSource LastOrDefault<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, bool> predicate) { throw null; }
        public static long LongCount<TSource>(this System.Collections.Generic.IEnumerable<TSource> source) { throw null; }
        public static long LongCount<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, bool> predicate) { throw null; }
        public static decimal Max(this System.Collections.Generic.IEnumerable<decimal> source) { throw null; }
        public static double Max(this System.Collections.Generic.IEnumerable<double> source) { throw null; }
        public static int Max(this System.Collections.Generic.IEnumerable<int> source) { throw null; }
        public static long Max(this System.Collections.Generic.IEnumerable<long> source) { throw null; }
        public static System.Nullable<decimal> Max(this System.Collections.Generic.IEnumerable<System.Nullable<decimal>> source) { throw null; }
        public static System.Nullable<double> Max(this System.Collections.Generic.IEnumerable<System.Nullable<double>> source) { throw null; }
        public static System.Nullable<int> Max(this System.Collections.Generic.IEnumerable<System.Nullable<int>> source) { throw null; }
        public static System.Nullable<long> Max(this System.Collections.Generic.IEnumerable<System.Nullable<long>> source) { throw null; }
        public static System.Nullable<float> Max(this System.Collections.Generic.IEnumerable<System.Nullable<float>> source) { throw null; }
        public static float Max(this System.Collections.Generic.IEnumerable<float> source) { throw null; }
        public static TSource Max<TSource>(this System.Collections.Generic.IEnumerable<TSource> source) { throw null; }
        public static decimal Max<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, decimal> selector) { throw null; }
        public static double Max<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, double> selector) { throw null; }
        public static int Max<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, int> selector) { throw null; }
        public static long Max<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, long> selector) { throw null; }
        public static System.Nullable<decimal> Max<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Nullable<decimal>> selector) { throw null; }
        public static System.Nullable<double> Max<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Nullable<double>> selector) { throw null; }
        public static System.Nullable<int> Max<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Nullable<int>> selector) { throw null; }
        public static System.Nullable<long> Max<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Nullable<long>> selector) { throw null; }
        public static System.Nullable<float> Max<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Nullable<float>> selector) { throw null; }
        public static float Max<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, float> selector) { throw null; }
        public static TResult Max<TSource, TResult>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, TResult> selector) { throw null; }
        public static decimal Min(this System.Collections.Generic.IEnumerable<decimal> source) { throw null; }
        public static double Min(this System.Collections.Generic.IEnumerable<double> source) { throw null; }
        public static int Min(this System.Collections.Generic.IEnumerable<int> source) { throw null; }
        public static long Min(this System.Collections.Generic.IEnumerable<long> source) { throw null; }
        public static System.Nullable<decimal> Min(this System.Collections.Generic.IEnumerable<System.Nullable<decimal>> source) { throw null; }
        public static System.Nullable<double> Min(this System.Collections.Generic.IEnumerable<System.Nullable<double>> source) { throw null; }
        public static System.Nullable<int> Min(this System.Collections.Generic.IEnumerable<System.Nullable<int>> source) { throw null; }
        public static System.Nullable<long> Min(this System.Collections.Generic.IEnumerable<System.Nullable<long>> source) { throw null; }
        public static System.Nullable<float> Min(this System.Collections.Generic.IEnumerable<System.Nullable<float>> source) { throw null; }
        public static float Min(this System.Collections.Generic.IEnumerable<float> source) { throw null; }
        public static TSource Min<TSource>(this System.Collections.Generic.IEnumerable<TSource> source) { throw null; }
        public static decimal Min<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, decimal> selector) { throw null; }
        public static double Min<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, double> selector) { throw null; }
        public static int Min<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, int> selector) { throw null; }
        public static long Min<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, long> selector) { throw null; }
        public static System.Nullable<decimal> Min<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Nullable<decimal>> selector) { throw null; }
        public static System.Nullable<double> Min<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Nullable<double>> selector) { throw null; }
        public static System.Nullable<int> Min<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Nullable<int>> selector) { throw null; }
        public static System.Nullable<long> Min<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Nullable<long>> selector) { throw null; }
        public static System.Nullable<float> Min<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Nullable<float>> selector) { throw null; }
        public static float Min<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, float> selector) { throw null; }
        public static TResult Min<TSource, TResult>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, TResult> selector) { throw null; }
        public static System.Collections.Generic.IEnumerable<TResult> OfType<TResult>(this System.Collections.IEnumerable source) { throw null; }
        public static System.Linq.IOrderedEnumerable<TSource> OrderBy<TSource, TKey>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, TKey> keySelector) { throw null; }
        public static System.Linq.IOrderedEnumerable<TSource> OrderBy<TSource, TKey>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, TKey> keySelector, System.Collections.Generic.IComparer<TKey> comparer) { throw null; }
        public static System.Linq.IOrderedEnumerable<TSource> OrderByDescending<TSource, TKey>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, TKey> keySelector) { throw null; }
        public static System.Linq.IOrderedEnumerable<TSource> OrderByDescending<TSource, TKey>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, TKey> keySelector, System.Collections.Generic.IComparer<TKey> comparer) { throw null; }
        public static System.Collections.Generic.IEnumerable<int> Range(int start, int count) { throw null; }
        public static System.Collections.Generic.IEnumerable<TResult> Repeat<TResult>(TResult element, int count) { throw null; }
        public static System.Collections.Generic.IEnumerable<TSource> Reverse<TSource>(this System.Collections.Generic.IEnumerable<TSource> source) { throw null; }
        public static System.Collections.Generic.IEnumerable<TResult> Select<TSource, TResult>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, TResult> selector) { throw null; }
        public static System.Collections.Generic.IEnumerable<TResult> Select<TSource, TResult>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, int, TResult> selector) { throw null; }
        public static System.Collections.Generic.IEnumerable<TResult> SelectMany<TSource, TResult>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Collections.Generic.IEnumerable<TResult>> selector) { throw null; }
        public static System.Collections.Generic.IEnumerable<TResult> SelectMany<TSource, TResult>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, int, System.Collections.Generic.IEnumerable<TResult>> selector) { throw null; }
        public static System.Collections.Generic.IEnumerable<TResult> SelectMany<TSource, TCollection, TResult>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Collections.Generic.IEnumerable<TCollection>> collectionSelector, System.Func<TSource, TCollection, TResult> resultSelector) { throw null; }
        public static System.Collections.Generic.IEnumerable<TResult> SelectMany<TSource, TCollection, TResult>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, int, System.Collections.Generic.IEnumerable<TCollection>> collectionSelector, System.Func<TSource, TCollection, TResult> resultSelector) { throw null; }
        public static bool SequenceEqual<TSource>(this System.Collections.Generic.IEnumerable<TSource> first, System.Collections.Generic.IEnumerable<TSource> second) { throw null; }
        public static bool SequenceEqual<TSource>(this System.Collections.Generic.IEnumerable<TSource> first, System.Collections.Generic.IEnumerable<TSource> second, System.Collections.Generic.IEqualityComparer<TSource> comparer) { throw null; }
        public static TSource Single<TSource>(this System.Collections.Generic.IEnumerable<TSource> source) { throw null; }
        public static TSource Single<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, bool> predicate) { throw null; }
        public static TSource SingleOrDefault<TSource>(this System.Collections.Generic.IEnumerable<TSource> source) { throw null; }
        public static TSource SingleOrDefault<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, bool> predicate) { throw null; }
        public static System.Collections.Generic.IEnumerable<TSource> Skip<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, int count) { throw null; }
        public static System.Collections.Generic.IEnumerable<TSource> SkipWhile<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, bool> predicate) { throw null; }
        public static System.Collections.Generic.IEnumerable<TSource> SkipWhile<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, int, bool> predicate) { throw null; }
        public static decimal Sum(this System.Collections.Generic.IEnumerable<decimal> source) { throw null; }
        public static double Sum(this System.Collections.Generic.IEnumerable<double> source) { throw null; }
        public static int Sum(this System.Collections.Generic.IEnumerable<int> source) { throw null; }
        public static long Sum(this System.Collections.Generic.IEnumerable<long> source) { throw null; }
        public static System.Nullable<decimal> Sum(this System.Collections.Generic.IEnumerable<System.Nullable<decimal>> source) { throw null; }
        public static System.Nullable<double> Sum(this System.Collections.Generic.IEnumerable<System.Nullable<double>> source) { throw null; }
        public static System.Nullable<int> Sum(this System.Collections.Generic.IEnumerable<System.Nullable<int>> source) { throw null; }
        public static System.Nullable<long> Sum(this System.Collections.Generic.IEnumerable<System.Nullable<long>> source) { throw null; }
        public static System.Nullable<float> Sum(this System.Collections.Generic.IEnumerable<System.Nullable<float>> source) { throw null; }
        public static float Sum(this System.Collections.Generic.IEnumerable<float> source) { throw null; }
        public static decimal Sum<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, decimal> selector) { throw null; }
        public static double Sum<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, double> selector) { throw null; }
        public static int Sum<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, int> selector) { throw null; }
        public static long Sum<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, long> selector) { throw null; }
        public static System.Nullable<decimal> Sum<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Nullable<decimal>> selector) { throw null; }
        public static System.Nullable<double> Sum<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Nullable<double>> selector) { throw null; }
        public static System.Nullable<int> Sum<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Nullable<int>> selector) { throw null; }
        public static System.Nullable<long> Sum<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Nullable<long>> selector) { throw null; }
        public static System.Nullable<float> Sum<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Nullable<float>> selector) { throw null; }
        public static float Sum<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, float> selector) { throw null; }
        public static System.Collections.Generic.IEnumerable<TSource> Take<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, int count) { throw null; }
        public static System.Collections.Generic.IEnumerable<TSource> TakeWhile<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, bool> predicate) { throw null; }
        public static System.Collections.Generic.IEnumerable<TSource> TakeWhile<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, int, bool> predicate) { throw null; }
        public static System.Linq.IOrderedEnumerable<TSource> ThenBy<TSource, TKey>(this System.Linq.IOrderedEnumerable<TSource> source, System.Func<TSource, TKey> keySelector) { throw null; }
        public static System.Linq.IOrderedEnumerable<TSource> ThenBy<TSource, TKey>(this System.Linq.IOrderedEnumerable<TSource> source, System.Func<TSource, TKey> keySelector, System.Collections.Generic.IComparer<TKey> comparer) { throw null; }
        public static System.Linq.IOrderedEnumerable<TSource> ThenByDescending<TSource, TKey>(this System.Linq.IOrderedEnumerable<TSource> source, System.Func<TSource, TKey> keySelector) { throw null; }
        public static System.Linq.IOrderedEnumerable<TSource> ThenByDescending<TSource, TKey>(this System.Linq.IOrderedEnumerable<TSource> source, System.Func<TSource, TKey> keySelector, System.Collections.Generic.IComparer<TKey> comparer) { throw null; }
        public static TSource[] ToArray<TSource>(this System.Collections.Generic.IEnumerable<TSource> source) { throw null; }
        public static System.Collections.Generic.Dictionary<TKey, TSource> ToDictionary<TSource, TKey>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, TKey> keySelector) { throw null; }
        public static System.Collections.Generic.Dictionary<TKey, TSource> ToDictionary<TSource, TKey>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, TKey> keySelector, System.Collections.Generic.IEqualityComparer<TKey> comparer) { throw null; }
        public static System.Collections.Generic.Dictionary<TKey, TElement> ToDictionary<TSource, TKey, TElement>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, TKey> keySelector, System.Func<TSource, TElement> elementSelector) { throw null; }
        public static System.Collections.Generic.Dictionary<TKey, TElement> ToDictionary<TSource, TKey, TElement>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, TKey> keySelector, System.Func<TSource, TElement> elementSelector, System.Collections.Generic.IEqualityComparer<TKey> comparer) { throw null; }
        public static System.Collections.Generic.List<TSource> ToList<TSource>(this System.Collections.Generic.IEnumerable<TSource> source) { throw null; }
        public static System.Linq.ILookup<TKey, TSource> ToLookup<TSource, TKey>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, TKey> keySelector) { throw null; }
        public static System.Linq.ILookup<TKey, TSource> ToLookup<TSource, TKey>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, TKey> keySelector, System.Collections.Generic.IEqualityComparer<TKey> comparer) { throw null; }
        public static System.Linq.ILookup<TKey, TElement> ToLookup<TSource, TKey, TElement>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, TKey> keySelector, System.Func<TSource, TElement> elementSelector) { throw null; }
        public static System.Linq.ILookup<TKey, TElement> ToLookup<TSource, TKey, TElement>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, TKey> keySelector, System.Func<TSource, TElement> elementSelector, System.Collections.Generic.IEqualityComparer<TKey> comparer) { throw null; }
        public static System.Collections.Generic.IEnumerable<TSource> Union<TSource>(this System.Collections.Generic.IEnumerable<TSource> first, System.Collections.Generic.IEnumerable<TSource> second) { throw null; }
        public static System.Collections.Generic.IEnumerable<TSource> Union<TSource>(this System.Collections.Generic.IEnumerable<TSource> first, System.Collections.Generic.IEnumerable<TSource> second, System.Collections.Generic.IEqualityComparer<TSource> comparer) { throw null; }
        public static System.Collections.Generic.IEnumerable<TSource> Where<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, bool> predicate) { throw null; }
        public static System.Collections.Generic.IEnumerable<TSource> Where<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, int, bool> predicate) { throw null; }
        public static System.Collections.Generic.IEnumerable<TResult> Zip<TFirst, TSecond, TResult>(this System.Collections.Generic.IEnumerable<TFirst> first, System.Collections.Generic.IEnumerable<TSecond> second, System.Func<TFirst, TSecond, TResult> resultSelector) { throw null; }
    }
    public abstract partial class EnumerableExecutor
    {
        protected EnumerableExecutor() { }
    }
    public partial class EnumerableExecutor<T> : System.Linq.EnumerableExecutor
    {
        public EnumerableExecutor(System.Linq.Expressions.Expression expression) { }
    }
    public abstract partial class EnumerableQuery
    {
        protected EnumerableQuery() { }
    }
    public partial class EnumerableQuery<T> : System.Linq.EnumerableQuery, System.Collections.Generic.IEnumerable<T>, System.Collections.IEnumerable, System.Linq.IOrderedQueryable, System.Linq.IOrderedQueryable<T>, System.Linq.IQueryable, System.Linq.IQueryable<T>, System.Linq.IQueryProvider
    {
        public EnumerableQuery(System.Collections.Generic.IEnumerable<T> enumerable) { }
        public EnumerableQuery(System.Linq.Expressions.Expression expression) { }
        System.Type System.Linq.IQueryable.ElementType { get { throw null; } }
        System.Linq.Expressions.Expression System.Linq.IQueryable.Expression { get { throw null; } }
        System.Linq.IQueryProvider System.Linq.IQueryable.Provider { get { throw null; } }
        System.Collections.Generic.IEnumerator<T> System.Collections.Generic.IEnumerable<T>.GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        System.Linq.IQueryable System.Linq.IQueryProvider.CreateQuery(System.Linq.Expressions.Expression expression) { throw null; }
        System.Linq.IQueryable<S> System.Linq.IQueryProvider.CreateQuery<S>(System.Linq.Expressions.Expression expression) { throw null; }
        object System.Linq.IQueryProvider.Execute(System.Linq.Expressions.Expression expression) { throw null; }
        S System.Linq.IQueryProvider.Execute<S>(System.Linq.Expressions.Expression expression) { throw null; }
        public override string ToString() { throw null; }
    }
    public partial interface IGrouping<out TKey, out TElement> : System.Collections.Generic.IEnumerable<TElement>, System.Collections.IEnumerable
    {
        TKey Key { get; }
    }
    public partial interface ILookup<TKey, TElement> : System.Collections.Generic.IEnumerable<System.Linq.IGrouping<TKey, TElement>>, System.Collections.IEnumerable
    {
        int Count { get; }
        System.Collections.Generic.IEnumerable<TElement> this[TKey key] { get; }
        bool Contains(TKey key);
    }
    public partial interface IOrderedEnumerable<TElement> : System.Collections.Generic.IEnumerable<TElement>, System.Collections.IEnumerable
    {
        System.Linq.IOrderedEnumerable<TElement> CreateOrderedEnumerable<TKey>(System.Func<TElement, TKey> keySelector, System.Collections.Generic.IComparer<TKey> comparer, bool descending);
    }
    public partial interface IOrderedQueryable : System.Collections.IEnumerable, System.Linq.IQueryable
    {
    }
    public partial interface IOrderedQueryable<out T> : System.Collections.Generic.IEnumerable<T>, System.Collections.IEnumerable, System.Linq.IOrderedQueryable, System.Linq.IQueryable, System.Linq.IQueryable<T>
    {
    }
    public partial interface IQueryable : System.Collections.IEnumerable
    {
        System.Type ElementType { get; }
        System.Linq.Expressions.Expression Expression { get; }
        System.Linq.IQueryProvider Provider { get; }
    }
    public partial interface IQueryable<out T> : System.Collections.Generic.IEnumerable<T>, System.Collections.IEnumerable, System.Linq.IQueryable
    {
    }
    public partial interface IQueryProvider
    {
        System.Linq.IQueryable CreateQuery(System.Linq.Expressions.Expression expression);
        System.Linq.IQueryable<TElement> CreateQuery<TElement>(System.Linq.Expressions.Expression expression);
        object Execute(System.Linq.Expressions.Expression expression);
        TResult Execute<TResult>(System.Linq.Expressions.Expression expression);
    }
    public partial class Lookup<TKey, TElement> : System.Collections.Generic.IEnumerable<System.Linq.IGrouping<TKey, TElement>>, System.Collections.IEnumerable, System.Linq.ILookup<TKey, TElement>
    {
        internal Lookup() { }
        public int Count { get { throw null; } }
        public System.Collections.Generic.IEnumerable<TElement> this[TKey key] { get { throw null; } }
        [System.Diagnostics.DebuggerHiddenAttribute]
        public System.Collections.Generic.IEnumerable<TResult> ApplyResultSelector<TResult>(System.Func<TKey, System.Collections.Generic.IEnumerable<TElement>, TResult> resultSelector) { throw null; }
        public bool Contains(TKey key) { throw null; }
        [System.Diagnostics.DebuggerHiddenAttribute]
        public System.Collections.Generic.IEnumerator<System.Linq.IGrouping<TKey, TElement>> GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
    }
    public partial class OrderedParallelQuery<TSource> : System.Linq.ParallelQuery<TSource>
    {
        internal OrderedParallelQuery() { }
        public override System.Collections.Generic.IEnumerator<TSource> GetEnumerator() { throw null; }
    }
    public static partial class ParallelEnumerable
    {
        public static TSource Aggregate<TSource>(this System.Linq.ParallelQuery<TSource> source, System.Func<TSource, TSource, TSource> func) { throw null; }
        public static TAccumulate Aggregate<TSource, TAccumulate>(this System.Linq.ParallelQuery<TSource> source, TAccumulate seed, System.Func<TAccumulate, TSource, TAccumulate> func) { throw null; }
        public static TResult Aggregate<TSource, TAccumulate, TResult>(this System.Linq.ParallelQuery<TSource> source, TAccumulate seed, System.Func<TAccumulate, TSource, TAccumulate> updateAccumulatorFunc, System.Func<TAccumulate, TAccumulate, TAccumulate> combineAccumulatorsFunc, System.Func<TAccumulate, TResult> resultSelector) { throw null; }
        public static TResult Aggregate<TSource, TAccumulate, TResult>(this System.Linq.ParallelQuery<TSource> source, TAccumulate seed, System.Func<TAccumulate, TSource, TAccumulate> func, System.Func<TAccumulate, TResult> resultSelector) { throw null; }
        public static TResult Aggregate<TSource, TAccumulate, TResult>(this System.Linq.ParallelQuery<TSource> source, System.Func<TAccumulate> seedFactory, System.Func<TAccumulate, TSource, TAccumulate> updateAccumulatorFunc, System.Func<TAccumulate, TAccumulate, TAccumulate> combineAccumulatorsFunc, System.Func<TAccumulate, TResult> resultSelector) { throw null; }
        public static bool All<TSource>(this System.Linq.ParallelQuery<TSource> source, System.Func<TSource, bool> predicate) { throw null; }
        public static bool Any<TSource>(this System.Linq.ParallelQuery<TSource> source) { throw null; }
        public static bool Any<TSource>(this System.Linq.ParallelQuery<TSource> source, System.Func<TSource, bool> predicate) { throw null; }
        public static System.Collections.Generic.IEnumerable<TSource> AsEnumerable<TSource>(this System.Linq.ParallelQuery<TSource> source) { throw null; }
        public static System.Linq.ParallelQuery AsOrdered(this System.Linq.ParallelQuery source) { throw null; }
        public static System.Linq.ParallelQuery<TSource> AsOrdered<TSource>(this System.Linq.ParallelQuery<TSource> source) { throw null; }
        public static System.Linq.ParallelQuery AsParallel(this System.Collections.IEnumerable source) { throw null; }
        public static System.Linq.ParallelQuery<TSource> AsParallel<TSource>(this System.Collections.Concurrent.Partitioner<TSource> source) { throw null; }
        public static System.Linq.ParallelQuery<TSource> AsParallel<TSource>(this System.Collections.Generic.IEnumerable<TSource> source) { throw null; }
        public static System.Collections.Generic.IEnumerable<TSource> AsSequential<TSource>(this System.Linq.ParallelQuery<TSource> source) { throw null; }
        public static System.Linq.ParallelQuery<TSource> AsUnordered<TSource>(this System.Linq.ParallelQuery<TSource> source) { throw null; }
        public static decimal Average(this System.Linq.ParallelQuery<decimal> source) { throw null; }
        public static double Average(this System.Linq.ParallelQuery<double> source) { throw null; }
        public static double Average(this System.Linq.ParallelQuery<int> source) { throw null; }
        public static double Average(this System.Linq.ParallelQuery<long> source) { throw null; }
        public static System.Nullable<decimal> Average(this System.Linq.ParallelQuery<System.Nullable<decimal>> source) { throw null; }
        public static System.Nullable<double> Average(this System.Linq.ParallelQuery<System.Nullable<double>> source) { throw null; }
        public static System.Nullable<double> Average(this System.Linq.ParallelQuery<System.Nullable<int>> source) { throw null; }
        public static System.Nullable<double> Average(this System.Linq.ParallelQuery<System.Nullable<long>> source) { throw null; }
        public static System.Nullable<float> Average(this System.Linq.ParallelQuery<System.Nullable<float>> source) { throw null; }
        public static float Average(this System.Linq.ParallelQuery<float> source) { throw null; }
        public static decimal Average<TSource>(this System.Linq.ParallelQuery<TSource> source, System.Func<TSource, decimal> selector) { throw null; }
        public static double Average<TSource>(this System.Linq.ParallelQuery<TSource> source, System.Func<TSource, double> selector) { throw null; }
        public static double Average<TSource>(this System.Linq.ParallelQuery<TSource> source, System.Func<TSource, int> selector) { throw null; }
        public static double Average<TSource>(this System.Linq.ParallelQuery<TSource> source, System.Func<TSource, long> selector) { throw null; }
        public static System.Nullable<decimal> Average<TSource>(this System.Linq.ParallelQuery<TSource> source, System.Func<TSource, System.Nullable<decimal>> selector) { throw null; }
        public static System.Nullable<double> Average<TSource>(this System.Linq.ParallelQuery<TSource> source, System.Func<TSource, System.Nullable<double>> selector) { throw null; }
        public static System.Nullable<double> Average<TSource>(this System.Linq.ParallelQuery<TSource> source, System.Func<TSource, System.Nullable<int>> selector) { throw null; }
        public static System.Nullable<double> Average<TSource>(this System.Linq.ParallelQuery<TSource> source, System.Func<TSource, System.Nullable<long>> selector) { throw null; }
        public static System.Nullable<float> Average<TSource>(this System.Linq.ParallelQuery<TSource> source, System.Func<TSource, System.Nullable<float>> selector) { throw null; }
        public static float Average<TSource>(this System.Linq.ParallelQuery<TSource> source, System.Func<TSource, float> selector) { throw null; }
        public static System.Linq.ParallelQuery<TResult> Cast<TResult>(this System.Linq.ParallelQuery source) { throw null; }
        [System.ObsoleteAttribute("The second data source of a binary operator must be of type System.Linq.ParallelQuery<T> rather than System.Collections.Generic.IEnumerable<T>. To fix this problem, use the AsParallel() extension method to convert the right data source to System.Linq.ParallelQuery<T>.")]
        public static System.Linq.ParallelQuery<TSource> Concat<TSource>(this System.Linq.ParallelQuery<TSource> first, System.Collections.Generic.IEnumerable<TSource> second) { throw null; }
        public static System.Linq.ParallelQuery<TSource> Concat<TSource>(this System.Linq.ParallelQuery<TSource> first, System.Linq.ParallelQuery<TSource> second) { throw null; }
        public static bool Contains<TSource>(this System.Linq.ParallelQuery<TSource> source, TSource value) { throw null; }
        public static bool Contains<TSource>(this System.Linq.ParallelQuery<TSource> source, TSource value, System.Collections.Generic.IEqualityComparer<TSource> comparer) { throw null; }
        public static int Count<TSource>(this System.Linq.ParallelQuery<TSource> source) { throw null; }
        public static int Count<TSource>(this System.Linq.ParallelQuery<TSource> source, System.Func<TSource, bool> predicate) { throw null; }
        public static System.Linq.ParallelQuery<TSource> DefaultIfEmpty<TSource>(this System.Linq.ParallelQuery<TSource> source) { throw null; }
        public static System.Linq.ParallelQuery<TSource> DefaultIfEmpty<TSource>(this System.Linq.ParallelQuery<TSource> source, TSource defaultValue) { throw null; }
        public static System.Linq.ParallelQuery<TSource> Distinct<TSource>(this System.Linq.ParallelQuery<TSource> source) { throw null; }
        public static System.Linq.ParallelQuery<TSource> Distinct<TSource>(this System.Linq.ParallelQuery<TSource> source, System.Collections.Generic.IEqualityComparer<TSource> comparer) { throw null; }
        public static TSource ElementAt<TSource>(this System.Linq.ParallelQuery<TSource> source, int index) { throw null; }
        public static TSource ElementAtOrDefault<TSource>(this System.Linq.ParallelQuery<TSource> source, int index) { throw null; }
        public static System.Linq.ParallelQuery<TResult> Empty<TResult>() { throw null; }
        [System.ObsoleteAttribute("The second data source of a binary operator must be of type System.Linq.ParallelQuery<T> rather than System.Collections.Generic.IEnumerable<T>. To fix this problem, use the AsParallel() extension method to convert the right data source to System.Linq.ParallelQuery<T>.")]
        public static System.Linq.ParallelQuery<TSource> Except<TSource>(this System.Linq.ParallelQuery<TSource> first, System.Collections.Generic.IEnumerable<TSource> second) { throw null; }
        [System.ObsoleteAttribute("The second data source of a binary operator must be of type System.Linq.ParallelQuery<T> rather than System.Collections.Generic.IEnumerable<T>. To fix this problem, use the AsParallel() extension method to convert the right data source to System.Linq.ParallelQuery<T>.")]
        public static System.Linq.ParallelQuery<TSource> Except<TSource>(this System.Linq.ParallelQuery<TSource> first, System.Collections.Generic.IEnumerable<TSource> second, System.Collections.Generic.IEqualityComparer<TSource> comparer) { throw null; }
        public static System.Linq.ParallelQuery<TSource> Except<TSource>(this System.Linq.ParallelQuery<TSource> first, System.Linq.ParallelQuery<TSource> second) { throw null; }
        public static System.Linq.ParallelQuery<TSource> Except<TSource>(this System.Linq.ParallelQuery<TSource> first, System.Linq.ParallelQuery<TSource> second, System.Collections.Generic.IEqualityComparer<TSource> comparer) { throw null; }
        public static TSource First<TSource>(this System.Linq.ParallelQuery<TSource> source) { throw null; }
        public static TSource First<TSource>(this System.Linq.ParallelQuery<TSource> source, System.Func<TSource, bool> predicate) { throw null; }
        public static TSource FirstOrDefault<TSource>(this System.Linq.ParallelQuery<TSource> source) { throw null; }
        public static TSource FirstOrDefault<TSource>(this System.Linq.ParallelQuery<TSource> source, System.Func<TSource, bool> predicate) { throw null; }
        public static void ForAll<TSource>(this System.Linq.ParallelQuery<TSource> source, System.Action<TSource> action) { }
        public static System.Linq.ParallelQuery<System.Linq.IGrouping<TKey, TSource>> GroupBy<TSource, TKey>(this System.Linq.ParallelQuery<TSource> source, System.Func<TSource, TKey> keySelector) { throw null; }
        public static System.Linq.ParallelQuery<System.Linq.IGrouping<TKey, TSource>> GroupBy<TSource, TKey>(this System.Linq.ParallelQuery<TSource> source, System.Func<TSource, TKey> keySelector, System.Collections.Generic.IEqualityComparer<TKey> comparer) { throw null; }
        public static System.Linq.ParallelQuery<System.Linq.IGrouping<TKey, TElement>> GroupBy<TSource, TKey, TElement>(this System.Linq.ParallelQuery<TSource> source, System.Func<TSource, TKey> keySelector, System.Func<TSource, TElement> elementSelector) { throw null; }
        public static System.Linq.ParallelQuery<System.Linq.IGrouping<TKey, TElement>> GroupBy<TSource, TKey, TElement>(this System.Linq.ParallelQuery<TSource> source, System.Func<TSource, TKey> keySelector, System.Func<TSource, TElement> elementSelector, System.Collections.Generic.IEqualityComparer<TKey> comparer) { throw null; }
        public static System.Linq.ParallelQuery<TResult> GroupBy<TSource, TKey, TResult>(this System.Linq.ParallelQuery<TSource> source, System.Func<TSource, TKey> keySelector, System.Func<TKey, System.Collections.Generic.IEnumerable<TSource>, TResult> resultSelector) { throw null; }
        public static System.Linq.ParallelQuery<TResult> GroupBy<TSource, TKey, TResult>(this System.Linq.ParallelQuery<TSource> source, System.Func<TSource, TKey> keySelector, System.Func<TKey, System.Collections.Generic.IEnumerable<TSource>, TResult> resultSelector, System.Collections.Generic.IEqualityComparer<TKey> comparer) { throw null; }
        public static System.Linq.ParallelQuery<TResult> GroupBy<TSource, TKey, TElement, TResult>(this System.Linq.ParallelQuery<TSource> source, System.Func<TSource, TKey> keySelector, System.Func<TSource, TElement> elementSelector, System.Func<TKey, System.Collections.Generic.IEnumerable<TElement>, TResult> resultSelector) { throw null; }
        public static System.Linq.ParallelQuery<TResult> GroupBy<TSource, TKey, TElement, TResult>(this System.Linq.ParallelQuery<TSource> source, System.Func<TSource, TKey> keySelector, System.Func<TSource, TElement> elementSelector, System.Func<TKey, System.Collections.Generic.IEnumerable<TElement>, TResult> resultSelector, System.Collections.Generic.IEqualityComparer<TKey> comparer) { throw null; }
        [System.ObsoleteAttribute("The second data source of a binary operator must be of type System.Linq.ParallelQuery<T> rather than System.Collections.Generic.IEnumerable<T>. To fix this problem, use the AsParallel() extension method to convert the right data source to System.Linq.ParallelQuery<T>.")]
        public static System.Linq.ParallelQuery<TResult> GroupJoin<TOuter, TInner, TKey, TResult>(this System.Linq.ParallelQuery<TOuter> outer, System.Collections.Generic.IEnumerable<TInner> inner, System.Func<TOuter, TKey> outerKeySelector, System.Func<TInner, TKey> innerKeySelector, System.Func<TOuter, System.Collections.Generic.IEnumerable<TInner>, TResult> resultSelector) { throw null; }
        [System.ObsoleteAttribute("The second data source of a binary operator must be of type System.Linq.ParallelQuery<T> rather than System.Collections.Generic.IEnumerable<T>. To fix this problem, use the AsParallel() extension method to convert the right data source to System.Linq.ParallelQuery<T>.")]
        public static System.Linq.ParallelQuery<TResult> GroupJoin<TOuter, TInner, TKey, TResult>(this System.Linq.ParallelQuery<TOuter> outer, System.Collections.Generic.IEnumerable<TInner> inner, System.Func<TOuter, TKey> outerKeySelector, System.Func<TInner, TKey> innerKeySelector, System.Func<TOuter, System.Collections.Generic.IEnumerable<TInner>, TResult> resultSelector, System.Collections.Generic.IEqualityComparer<TKey> comparer) { throw null; }
        public static System.Linq.ParallelQuery<TResult> GroupJoin<TOuter, TInner, TKey, TResult>(this System.Linq.ParallelQuery<TOuter> outer, System.Linq.ParallelQuery<TInner> inner, System.Func<TOuter, TKey> outerKeySelector, System.Func<TInner, TKey> innerKeySelector, System.Func<TOuter, System.Collections.Generic.IEnumerable<TInner>, TResult> resultSelector) { throw null; }
        public static System.Linq.ParallelQuery<TResult> GroupJoin<TOuter, TInner, TKey, TResult>(this System.Linq.ParallelQuery<TOuter> outer, System.Linq.ParallelQuery<TInner> inner, System.Func<TOuter, TKey> outerKeySelector, System.Func<TInner, TKey> innerKeySelector, System.Func<TOuter, System.Collections.Generic.IEnumerable<TInner>, TResult> resultSelector, System.Collections.Generic.IEqualityComparer<TKey> comparer) { throw null; }
        [System.ObsoleteAttribute("The second data source of a binary operator must be of type System.Linq.ParallelQuery<T> rather than System.Collections.Generic.IEnumerable<T>. To fix this problem, use the AsParallel() extension method to convert the right data source to System.Linq.ParallelQuery<T>.")]
        public static System.Linq.ParallelQuery<TSource> Intersect<TSource>(this System.Linq.ParallelQuery<TSource> first, System.Collections.Generic.IEnumerable<TSource> second) { throw null; }
        [System.ObsoleteAttribute("The second data source of a binary operator must be of type System.Linq.ParallelQuery<T> rather than System.Collections.Generic.IEnumerable<T>. To fix this problem, use the AsParallel() extension method to convert the right data source to System.Linq.ParallelQuery<T>.")]
        public static System.Linq.ParallelQuery<TSource> Intersect<TSource>(this System.Linq.ParallelQuery<TSource> first, System.Collections.Generic.IEnumerable<TSource> second, System.Collections.Generic.IEqualityComparer<TSource> comparer) { throw null; }
        public static System.Linq.ParallelQuery<TSource> Intersect<TSource>(this System.Linq.ParallelQuery<TSource> first, System.Linq.ParallelQuery<TSource> second) { throw null; }
        public static System.Linq.ParallelQuery<TSource> Intersect<TSource>(this System.Linq.ParallelQuery<TSource> first, System.Linq.ParallelQuery<TSource> second, System.Collections.Generic.IEqualityComparer<TSource> comparer) { throw null; }
        [System.ObsoleteAttribute("The second data source of a binary operator must be of type System.Linq.ParallelQuery<T> rather than System.Collections.Generic.IEnumerable<T>. To fix this problem, use the AsParallel() extension method to convert the right data source to System.Linq.ParallelQuery<T>.")]
        public static System.Linq.ParallelQuery<TResult> Join<TOuter, TInner, TKey, TResult>(this System.Linq.ParallelQuery<TOuter> outer, System.Collections.Generic.IEnumerable<TInner> inner, System.Func<TOuter, TKey> outerKeySelector, System.Func<TInner, TKey> innerKeySelector, System.Func<TOuter, TInner, TResult> resultSelector) { throw null; }
        [System.ObsoleteAttribute("The second data source of a binary operator must be of type System.Linq.ParallelQuery<T> rather than System.Collections.Generic.IEnumerable<T>. To fix this problem, use the AsParallel() extension method to convert the right data source to System.Linq.ParallelQuery<T>.")]
        public static System.Linq.ParallelQuery<TResult> Join<TOuter, TInner, TKey, TResult>(this System.Linq.ParallelQuery<TOuter> outer, System.Collections.Generic.IEnumerable<TInner> inner, System.Func<TOuter, TKey> outerKeySelector, System.Func<TInner, TKey> innerKeySelector, System.Func<TOuter, TInner, TResult> resultSelector, System.Collections.Generic.IEqualityComparer<TKey> comparer) { throw null; }
        public static System.Linq.ParallelQuery<TResult> Join<TOuter, TInner, TKey, TResult>(this System.Linq.ParallelQuery<TOuter> outer, System.Linq.ParallelQuery<TInner> inner, System.Func<TOuter, TKey> outerKeySelector, System.Func<TInner, TKey> innerKeySelector, System.Func<TOuter, TInner, TResult> resultSelector) { throw null; }
        public static System.Linq.ParallelQuery<TResult> Join<TOuter, TInner, TKey, TResult>(this System.Linq.ParallelQuery<TOuter> outer, System.Linq.ParallelQuery<TInner> inner, System.Func<TOuter, TKey> outerKeySelector, System.Func<TInner, TKey> innerKeySelector, System.Func<TOuter, TInner, TResult> resultSelector, System.Collections.Generic.IEqualityComparer<TKey> comparer) { throw null; }
        public static TSource Last<TSource>(this System.Linq.ParallelQuery<TSource> source) { throw null; }
        public static TSource Last<TSource>(this System.Linq.ParallelQuery<TSource> source, System.Func<TSource, bool> predicate) { throw null; }
        public static TSource LastOrDefault<TSource>(this System.Linq.ParallelQuery<TSource> source) { throw null; }
        public static TSource LastOrDefault<TSource>(this System.Linq.ParallelQuery<TSource> source, System.Func<TSource, bool> predicate) { throw null; }
        public static long LongCount<TSource>(this System.Linq.ParallelQuery<TSource> source) { throw null; }
        public static long LongCount<TSource>(this System.Linq.ParallelQuery<TSource> source, System.Func<TSource, bool> predicate) { throw null; }
        public static decimal Max(this System.Linq.ParallelQuery<decimal> source) { throw null; }
        public static double Max(this System.Linq.ParallelQuery<double> source) { throw null; }
        public static int Max(this System.Linq.ParallelQuery<int> source) { throw null; }
        public static long Max(this System.Linq.ParallelQuery<long> source) { throw null; }
        public static System.Nullable<decimal> Max(this System.Linq.ParallelQuery<System.Nullable<decimal>> source) { throw null; }
        public static System.Nullable<double> Max(this System.Linq.ParallelQuery<System.Nullable<double>> source) { throw null; }
        public static System.Nullable<int> Max(this System.Linq.ParallelQuery<System.Nullable<int>> source) { throw null; }
        public static System.Nullable<long> Max(this System.Linq.ParallelQuery<System.Nullable<long>> source) { throw null; }
        public static System.Nullable<float> Max(this System.Linq.ParallelQuery<System.Nullable<float>> source) { throw null; }
        public static float Max(this System.Linq.ParallelQuery<float> source) { throw null; }
        public static TSource Max<TSource>(this System.Linq.ParallelQuery<TSource> source) { throw null; }
        public static decimal Max<TSource>(this System.Linq.ParallelQuery<TSource> source, System.Func<TSource, decimal> selector) { throw null; }
        public static double Max<TSource>(this System.Linq.ParallelQuery<TSource> source, System.Func<TSource, double> selector) { throw null; }
        public static int Max<TSource>(this System.Linq.ParallelQuery<TSource> source, System.Func<TSource, int> selector) { throw null; }
        public static long Max<TSource>(this System.Linq.ParallelQuery<TSource> source, System.Func<TSource, long> selector) { throw null; }
        public static System.Nullable<decimal> Max<TSource>(this System.Linq.ParallelQuery<TSource> source, System.Func<TSource, System.Nullable<decimal>> selector) { throw null; }
        public static System.Nullable<double> Max<TSource>(this System.Linq.ParallelQuery<TSource> source, System.Func<TSource, System.Nullable<double>> selector) { throw null; }
        public static System.Nullable<int> Max<TSource>(this System.Linq.ParallelQuery<TSource> source, System.Func<TSource, System.Nullable<int>> selector) { throw null; }
        public static System.Nullable<long> Max<TSource>(this System.Linq.ParallelQuery<TSource> source, System.Func<TSource, System.Nullable<long>> selector) { throw null; }
        public static System.Nullable<float> Max<TSource>(this System.Linq.ParallelQuery<TSource> source, System.Func<TSource, System.Nullable<float>> selector) { throw null; }
        public static float Max<TSource>(this System.Linq.ParallelQuery<TSource> source, System.Func<TSource, float> selector) { throw null; }
        public static TResult Max<TSource, TResult>(this System.Linq.ParallelQuery<TSource> source, System.Func<TSource, TResult> selector) { throw null; }
        public static decimal Min(this System.Linq.ParallelQuery<decimal> source) { throw null; }
        public static double Min(this System.Linq.ParallelQuery<double> source) { throw null; }
        public static int Min(this System.Linq.ParallelQuery<int> source) { throw null; }
        public static long Min(this System.Linq.ParallelQuery<long> source) { throw null; }
        public static System.Nullable<decimal> Min(this System.Linq.ParallelQuery<System.Nullable<decimal>> source) { throw null; }
        public static System.Nullable<double> Min(this System.Linq.ParallelQuery<System.Nullable<double>> source) { throw null; }
        public static System.Nullable<int> Min(this System.Linq.ParallelQuery<System.Nullable<int>> source) { throw null; }
        public static System.Nullable<long> Min(this System.Linq.ParallelQuery<System.Nullable<long>> source) { throw null; }
        public static System.Nullable<float> Min(this System.Linq.ParallelQuery<System.Nullable<float>> source) { throw null; }
        public static float Min(this System.Linq.ParallelQuery<float> source) { throw null; }
        public static TSource Min<TSource>(this System.Linq.ParallelQuery<TSource> source) { throw null; }
        public static decimal Min<TSource>(this System.Linq.ParallelQuery<TSource> source, System.Func<TSource, decimal> selector) { throw null; }
        public static double Min<TSource>(this System.Linq.ParallelQuery<TSource> source, System.Func<TSource, double> selector) { throw null; }
        public static int Min<TSource>(this System.Linq.ParallelQuery<TSource> source, System.Func<TSource, int> selector) { throw null; }
        public static long Min<TSource>(this System.Linq.ParallelQuery<TSource> source, System.Func<TSource, long> selector) { throw null; }
        public static System.Nullable<decimal> Min<TSource>(this System.Linq.ParallelQuery<TSource> source, System.Func<TSource, System.Nullable<decimal>> selector) { throw null; }
        public static System.Nullable<double> Min<TSource>(this System.Linq.ParallelQuery<TSource> source, System.Func<TSource, System.Nullable<double>> selector) { throw null; }
        public static System.Nullable<int> Min<TSource>(this System.Linq.ParallelQuery<TSource> source, System.Func<TSource, System.Nullable<int>> selector) { throw null; }
        public static System.Nullable<long> Min<TSource>(this System.Linq.ParallelQuery<TSource> source, System.Func<TSource, System.Nullable<long>> selector) { throw null; }
        public static System.Nullable<float> Min<TSource>(this System.Linq.ParallelQuery<TSource> source, System.Func<TSource, System.Nullable<float>> selector) { throw null; }
        public static float Min<TSource>(this System.Linq.ParallelQuery<TSource> source, System.Func<TSource, float> selector) { throw null; }
        public static TResult Min<TSource, TResult>(this System.Linq.ParallelQuery<TSource> source, System.Func<TSource, TResult> selector) { throw null; }
        public static System.Linq.ParallelQuery<TResult> OfType<TResult>(this System.Linq.ParallelQuery source) { throw null; }
        public static System.Linq.OrderedParallelQuery<TSource> OrderBy<TSource, TKey>(this System.Linq.ParallelQuery<TSource> source, System.Func<TSource, TKey> keySelector) { throw null; }
        public static System.Linq.OrderedParallelQuery<TSource> OrderBy<TSource, TKey>(this System.Linq.ParallelQuery<TSource> source, System.Func<TSource, TKey> keySelector, System.Collections.Generic.IComparer<TKey> comparer) { throw null; }
        public static System.Linq.OrderedParallelQuery<TSource> OrderByDescending<TSource, TKey>(this System.Linq.ParallelQuery<TSource> source, System.Func<TSource, TKey> keySelector) { throw null; }
        public static System.Linq.OrderedParallelQuery<TSource> OrderByDescending<TSource, TKey>(this System.Linq.ParallelQuery<TSource> source, System.Func<TSource, TKey> keySelector, System.Collections.Generic.IComparer<TKey> comparer) { throw null; }
        public static System.Linq.ParallelQuery<int> Range(int start, int count) { throw null; }
        public static System.Linq.ParallelQuery<TResult> Repeat<TResult>(TResult element, int count) { throw null; }
        public static System.Linq.ParallelQuery<TSource> Reverse<TSource>(this System.Linq.ParallelQuery<TSource> source) { throw null; }
        public static System.Linq.ParallelQuery<TResult> Select<TSource, TResult>(this System.Linq.ParallelQuery<TSource> source, System.Func<TSource, TResult> selector) { throw null; }
        public static System.Linq.ParallelQuery<TResult> Select<TSource, TResult>(this System.Linq.ParallelQuery<TSource> source, System.Func<TSource, int, TResult> selector) { throw null; }
        public static System.Linq.ParallelQuery<TResult> SelectMany<TSource, TResult>(this System.Linq.ParallelQuery<TSource> source, System.Func<TSource, System.Collections.Generic.IEnumerable<TResult>> selector) { throw null; }
        public static System.Linq.ParallelQuery<TResult> SelectMany<TSource, TResult>(this System.Linq.ParallelQuery<TSource> source, System.Func<TSource, int, System.Collections.Generic.IEnumerable<TResult>> selector) { throw null; }
        public static System.Linq.ParallelQuery<TResult> SelectMany<TSource, TCollection, TResult>(this System.Linq.ParallelQuery<TSource> source, System.Func<TSource, System.Collections.Generic.IEnumerable<TCollection>> collectionSelector, System.Func<TSource, TCollection, TResult> resultSelector) { throw null; }
        public static System.Linq.ParallelQuery<TResult> SelectMany<TSource, TCollection, TResult>(this System.Linq.ParallelQuery<TSource> source, System.Func<TSource, int, System.Collections.Generic.IEnumerable<TCollection>> collectionSelector, System.Func<TSource, TCollection, TResult> resultSelector) { throw null; }
        [System.ObsoleteAttribute("The second data source of a binary operator must be of type System.Linq.ParallelQuery<T> rather than System.Collections.Generic.IEnumerable<T>. To fix this problem, use the AsParallel() extension method to convert the right data source to System.Linq.ParallelQuery<T>.")]
        public static bool SequenceEqual<TSource>(this System.Linq.ParallelQuery<TSource> first, System.Collections.Generic.IEnumerable<TSource> second) { throw null; }
        [System.ObsoleteAttribute("The second data source of a binary operator must be of type System.Linq.ParallelQuery<T> rather than System.Collections.Generic.IEnumerable<T>. To fix this problem, use the AsParallel() extension method to convert the right data source to System.Linq.ParallelQuery<T>.")]
        public static bool SequenceEqual<TSource>(this System.Linq.ParallelQuery<TSource> first, System.Collections.Generic.IEnumerable<TSource> second, System.Collections.Generic.IEqualityComparer<TSource> comparer) { throw null; }
        public static bool SequenceEqual<TSource>(this System.Linq.ParallelQuery<TSource> first, System.Linq.ParallelQuery<TSource> second) { throw null; }
        public static bool SequenceEqual<TSource>(this System.Linq.ParallelQuery<TSource> first, System.Linq.ParallelQuery<TSource> second, System.Collections.Generic.IEqualityComparer<TSource> comparer) { throw null; }
        public static TSource Single<TSource>(this System.Linq.ParallelQuery<TSource> source) { throw null; }
        public static TSource Single<TSource>(this System.Linq.ParallelQuery<TSource> source, System.Func<TSource, bool> predicate) { throw null; }
        public static TSource SingleOrDefault<TSource>(this System.Linq.ParallelQuery<TSource> source) { throw null; }
        public static TSource SingleOrDefault<TSource>(this System.Linq.ParallelQuery<TSource> source, System.Func<TSource, bool> predicate) { throw null; }
        public static System.Linq.ParallelQuery<TSource> Skip<TSource>(this System.Linq.ParallelQuery<TSource> source, int count) { throw null; }
        public static System.Linq.ParallelQuery<TSource> SkipWhile<TSource>(this System.Linq.ParallelQuery<TSource> source, System.Func<TSource, bool> predicate) { throw null; }
        public static System.Linq.ParallelQuery<TSource> SkipWhile<TSource>(this System.Linq.ParallelQuery<TSource> source, System.Func<TSource, int, bool> predicate) { throw null; }
        public static decimal Sum(this System.Linq.ParallelQuery<decimal> source) { throw null; }
        public static double Sum(this System.Linq.ParallelQuery<double> source) { throw null; }
        public static int Sum(this System.Linq.ParallelQuery<int> source) { throw null; }
        public static long Sum(this System.Linq.ParallelQuery<long> source) { throw null; }
        public static System.Nullable<decimal> Sum(this System.Linq.ParallelQuery<System.Nullable<decimal>> source) { throw null; }
        public static System.Nullable<double> Sum(this System.Linq.ParallelQuery<System.Nullable<double>> source) { throw null; }
        public static System.Nullable<int> Sum(this System.Linq.ParallelQuery<System.Nullable<int>> source) { throw null; }
        public static System.Nullable<long> Sum(this System.Linq.ParallelQuery<System.Nullable<long>> source) { throw null; }
        public static System.Nullable<float> Sum(this System.Linq.ParallelQuery<System.Nullable<float>> source) { throw null; }
        public static float Sum(this System.Linq.ParallelQuery<float> source) { throw null; }
        public static decimal Sum<TSource>(this System.Linq.ParallelQuery<TSource> source, System.Func<TSource, decimal> selector) { throw null; }
        public static double Sum<TSource>(this System.Linq.ParallelQuery<TSource> source, System.Func<TSource, double> selector) { throw null; }
        public static int Sum<TSource>(this System.Linq.ParallelQuery<TSource> source, System.Func<TSource, int> selector) { throw null; }
        public static long Sum<TSource>(this System.Linq.ParallelQuery<TSource> source, System.Func<TSource, long> selector) { throw null; }
        public static System.Nullable<decimal> Sum<TSource>(this System.Linq.ParallelQuery<TSource> source, System.Func<TSource, System.Nullable<decimal>> selector) { throw null; }
        public static System.Nullable<double> Sum<TSource>(this System.Linq.ParallelQuery<TSource> source, System.Func<TSource, System.Nullable<double>> selector) { throw null; }
        public static System.Nullable<int> Sum<TSource>(this System.Linq.ParallelQuery<TSource> source, System.Func<TSource, System.Nullable<int>> selector) { throw null; }
        public static System.Nullable<long> Sum<TSource>(this System.Linq.ParallelQuery<TSource> source, System.Func<TSource, System.Nullable<long>> selector) { throw null; }
        public static System.Nullable<float> Sum<TSource>(this System.Linq.ParallelQuery<TSource> source, System.Func<TSource, System.Nullable<float>> selector) { throw null; }
        public static float Sum<TSource>(this System.Linq.ParallelQuery<TSource> source, System.Func<TSource, float> selector) { throw null; }
        public static System.Linq.ParallelQuery<TSource> Take<TSource>(this System.Linq.ParallelQuery<TSource> source, int count) { throw null; }
        public static System.Linq.ParallelQuery<TSource> TakeWhile<TSource>(this System.Linq.ParallelQuery<TSource> source, System.Func<TSource, bool> predicate) { throw null; }
        public static System.Linq.ParallelQuery<TSource> TakeWhile<TSource>(this System.Linq.ParallelQuery<TSource> source, System.Func<TSource, int, bool> predicate) { throw null; }
        public static System.Linq.OrderedParallelQuery<TSource> ThenBy<TSource, TKey>(this System.Linq.OrderedParallelQuery<TSource> source, System.Func<TSource, TKey> keySelector) { throw null; }
        public static System.Linq.OrderedParallelQuery<TSource> ThenBy<TSource, TKey>(this System.Linq.OrderedParallelQuery<TSource> source, System.Func<TSource, TKey> keySelector, System.Collections.Generic.IComparer<TKey> comparer) { throw null; }
        public static System.Linq.OrderedParallelQuery<TSource> ThenByDescending<TSource, TKey>(this System.Linq.OrderedParallelQuery<TSource> source, System.Func<TSource, TKey> keySelector) { throw null; }
        public static System.Linq.OrderedParallelQuery<TSource> ThenByDescending<TSource, TKey>(this System.Linq.OrderedParallelQuery<TSource> source, System.Func<TSource, TKey> keySelector, System.Collections.Generic.IComparer<TKey> comparer) { throw null; }
        public static TSource[] ToArray<TSource>(this System.Linq.ParallelQuery<TSource> source) { throw null; }
        public static System.Collections.Generic.Dictionary<TKey, TSource> ToDictionary<TSource, TKey>(this System.Linq.ParallelQuery<TSource> source, System.Func<TSource, TKey> keySelector) { throw null; }
        public static System.Collections.Generic.Dictionary<TKey, TSource> ToDictionary<TSource, TKey>(this System.Linq.ParallelQuery<TSource> source, System.Func<TSource, TKey> keySelector, System.Collections.Generic.IEqualityComparer<TKey> comparer) { throw null; }
        public static System.Collections.Generic.Dictionary<TKey, TElement> ToDictionary<TSource, TKey, TElement>(this System.Linq.ParallelQuery<TSource> source, System.Func<TSource, TKey> keySelector, System.Func<TSource, TElement> elementSelector) { throw null; }
        public static System.Collections.Generic.Dictionary<TKey, TElement> ToDictionary<TSource, TKey, TElement>(this System.Linq.ParallelQuery<TSource> source, System.Func<TSource, TKey> keySelector, System.Func<TSource, TElement> elementSelector, System.Collections.Generic.IEqualityComparer<TKey> comparer) { throw null; }
        public static System.Collections.Generic.List<TSource> ToList<TSource>(this System.Linq.ParallelQuery<TSource> source) { throw null; }
        public static System.Linq.ILookup<TKey, TSource> ToLookup<TSource, TKey>(this System.Linq.ParallelQuery<TSource> source, System.Func<TSource, TKey> keySelector) { throw null; }
        public static System.Linq.ILookup<TKey, TSource> ToLookup<TSource, TKey>(this System.Linq.ParallelQuery<TSource> source, System.Func<TSource, TKey> keySelector, System.Collections.Generic.IEqualityComparer<TKey> comparer) { throw null; }
        public static System.Linq.ILookup<TKey, TElement> ToLookup<TSource, TKey, TElement>(this System.Linq.ParallelQuery<TSource> source, System.Func<TSource, TKey> keySelector, System.Func<TSource, TElement> elementSelector) { throw null; }
        public static System.Linq.ILookup<TKey, TElement> ToLookup<TSource, TKey, TElement>(this System.Linq.ParallelQuery<TSource> source, System.Func<TSource, TKey> keySelector, System.Func<TSource, TElement> elementSelector, System.Collections.Generic.IEqualityComparer<TKey> comparer) { throw null; }
        [System.ObsoleteAttribute("The second data source of a binary operator must be of type System.Linq.ParallelQuery<T> rather than System.Collections.Generic.IEnumerable<T>. To fix this problem, use the AsParallel() extension method to convert the right data source to System.Linq.ParallelQuery<T>.")]
        public static System.Linq.ParallelQuery<TSource> Union<TSource>(this System.Linq.ParallelQuery<TSource> first, System.Collections.Generic.IEnumerable<TSource> second) { throw null; }
        [System.ObsoleteAttribute("The second data source of a binary operator must be of type System.Linq.ParallelQuery<T> rather than System.Collections.Generic.IEnumerable<T>. To fix this problem, use the AsParallel() extension method to convert the right data source to System.Linq.ParallelQuery<T>.")]
        public static System.Linq.ParallelQuery<TSource> Union<TSource>(this System.Linq.ParallelQuery<TSource> first, System.Collections.Generic.IEnumerable<TSource> second, System.Collections.Generic.IEqualityComparer<TSource> comparer) { throw null; }
        public static System.Linq.ParallelQuery<TSource> Union<TSource>(this System.Linq.ParallelQuery<TSource> first, System.Linq.ParallelQuery<TSource> second) { throw null; }
        public static System.Linq.ParallelQuery<TSource> Union<TSource>(this System.Linq.ParallelQuery<TSource> first, System.Linq.ParallelQuery<TSource> second, System.Collections.Generic.IEqualityComparer<TSource> comparer) { throw null; }
        public static System.Linq.ParallelQuery<TSource> Where<TSource>(this System.Linq.ParallelQuery<TSource> source, System.Func<TSource, bool> predicate) { throw null; }
        public static System.Linq.ParallelQuery<TSource> Where<TSource>(this System.Linq.ParallelQuery<TSource> source, System.Func<TSource, int, bool> predicate) { throw null; }
        public static System.Linq.ParallelQuery<TSource> WithCancellation<TSource>(this System.Linq.ParallelQuery<TSource> source, System.Threading.CancellationToken cancellationToken) { throw null; }
        public static System.Linq.ParallelQuery<TSource> WithDegreeOfParallelism<TSource>(this System.Linq.ParallelQuery<TSource> source, int degreeOfParallelism) { throw null; }
        public static System.Linq.ParallelQuery<TSource> WithExecutionMode<TSource>(this System.Linq.ParallelQuery<TSource> source, System.Linq.ParallelExecutionMode executionMode) { throw null; }
        public static System.Linq.ParallelQuery<TSource> WithMergeOptions<TSource>(this System.Linq.ParallelQuery<TSource> source, System.Linq.ParallelMergeOptions mergeOptions) { throw null; }
        [System.ObsoleteAttribute("The second data source of a binary operator must be of type System.Linq.ParallelQuery<T> rather than System.Collections.Generic.IEnumerable<T>. To fix this problem, use the AsParallel() extension method to convert the right data source to System.Linq.ParallelQuery<T>.")]
        public static System.Linq.ParallelQuery<TResult> Zip<TFirst, TSecond, TResult>(this System.Linq.ParallelQuery<TFirst> first, System.Collections.Generic.IEnumerable<TSecond> second, System.Func<TFirst, TSecond, TResult> resultSelector) { throw null; }
        public static System.Linq.ParallelQuery<TResult> Zip<TFirst, TSecond, TResult>(this System.Linq.ParallelQuery<TFirst> first, System.Linq.ParallelQuery<TSecond> second, System.Func<TFirst, TSecond, TResult> resultSelector) { throw null; }
    }
    public enum ParallelExecutionMode
    {
        Default = 0,
        ForceParallelism = 1,
    }
    public enum ParallelMergeOptions
    {
        AutoBuffered = 2,
        Default = 0,
        FullyBuffered = 3,
        NotBuffered = 1,
    }
    public partial class ParallelQuery : System.Collections.IEnumerable
    {
        internal ParallelQuery() { }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
    }
    public partial class ParallelQuery<TSource> : System.Linq.ParallelQuery, System.Collections.Generic.IEnumerable<TSource>, System.Collections.IEnumerable
    {
        internal ParallelQuery() { }
        public virtual System.Collections.Generic.IEnumerator<TSource> GetEnumerator() { throw null; }
    }
    public static partial class Queryable
    {
        public static TSource Aggregate<TSource>(this System.Linq.IQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, TSource, TSource>> func) { throw null; }
        public static TAccumulate Aggregate<TSource, TAccumulate>(this System.Linq.IQueryable<TSource> source, TAccumulate seed, System.Linq.Expressions.Expression<System.Func<TAccumulate, TSource, TAccumulate>> func) { throw null; }
        public static TResult Aggregate<TSource, TAccumulate, TResult>(this System.Linq.IQueryable<TSource> source, TAccumulate seed, System.Linq.Expressions.Expression<System.Func<TAccumulate, TSource, TAccumulate>> func, System.Linq.Expressions.Expression<System.Func<TAccumulate, TResult>> selector) { throw null; }
        public static bool All<TSource>(this System.Linq.IQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, bool>> predicate) { throw null; }
        public static bool Any<TSource>(this System.Linq.IQueryable<TSource> source) { throw null; }
        public static bool Any<TSource>(this System.Linq.IQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, bool>> predicate) { throw null; }
        public static System.Linq.IQueryable AsQueryable(this System.Collections.IEnumerable source) { throw null; }
        public static System.Linq.IQueryable<TElement> AsQueryable<TElement>(this System.Collections.Generic.IEnumerable<TElement> source) { throw null; }
        public static decimal Average(this System.Linq.IQueryable<decimal> source) { throw null; }
        public static double Average(this System.Linq.IQueryable<double> source) { throw null; }
        public static double Average(this System.Linq.IQueryable<int> source) { throw null; }
        public static double Average(this System.Linq.IQueryable<long> source) { throw null; }
        public static System.Nullable<decimal> Average(this System.Linq.IQueryable<System.Nullable<decimal>> source) { throw null; }
        public static System.Nullable<double> Average(this System.Linq.IQueryable<System.Nullable<double>> source) { throw null; }
        public static System.Nullable<double> Average(this System.Linq.IQueryable<System.Nullable<int>> source) { throw null; }
        public static System.Nullable<double> Average(this System.Linq.IQueryable<System.Nullable<long>> source) { throw null; }
        public static System.Nullable<float> Average(this System.Linq.IQueryable<System.Nullable<float>> source) { throw null; }
        public static float Average(this System.Linq.IQueryable<float> source) { throw null; }
        public static decimal Average<TSource>(this System.Linq.IQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, decimal>> selector) { throw null; }
        public static double Average<TSource>(this System.Linq.IQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, double>> selector) { throw null; }
        public static double Average<TSource>(this System.Linq.IQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, int>> selector) { throw null; }
        public static double Average<TSource>(this System.Linq.IQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, long>> selector) { throw null; }
        public static System.Nullable<decimal> Average<TSource>(this System.Linq.IQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, System.Nullable<decimal>>> selector) { throw null; }
        public static System.Nullable<double> Average<TSource>(this System.Linq.IQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, System.Nullable<double>>> selector) { throw null; }
        public static System.Nullable<double> Average<TSource>(this System.Linq.IQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, System.Nullable<int>>> selector) { throw null; }
        public static System.Nullable<double> Average<TSource>(this System.Linq.IQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, System.Nullable<long>>> selector) { throw null; }
        public static System.Nullable<float> Average<TSource>(this System.Linq.IQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, System.Nullable<float>>> selector) { throw null; }
        public static float Average<TSource>(this System.Linq.IQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, float>> selector) { throw null; }
        public static System.Linq.IQueryable<TResult> Cast<TResult>(this System.Linq.IQueryable source) { throw null; }
        public static System.Linq.IQueryable<TSource> Concat<TSource>(this System.Linq.IQueryable<TSource> source1, System.Collections.Generic.IEnumerable<TSource> source2) { throw null; }
        public static bool Contains<TSource>(this System.Linq.IQueryable<TSource> source, TSource item) { throw null; }
        public static bool Contains<TSource>(this System.Linq.IQueryable<TSource> source, TSource item, System.Collections.Generic.IEqualityComparer<TSource> comparer) { throw null; }
        public static int Count<TSource>(this System.Linq.IQueryable<TSource> source) { throw null; }
        public static int Count<TSource>(this System.Linq.IQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, bool>> predicate) { throw null; }
        public static System.Linq.IQueryable<TSource> DefaultIfEmpty<TSource>(this System.Linq.IQueryable<TSource> source) { throw null; }
        public static System.Linq.IQueryable<TSource> DefaultIfEmpty<TSource>(this System.Linq.IQueryable<TSource> source, TSource defaultValue) { throw null; }
        public static System.Linq.IQueryable<TSource> Distinct<TSource>(this System.Linq.IQueryable<TSource> source) { throw null; }
        public static System.Linq.IQueryable<TSource> Distinct<TSource>(this System.Linq.IQueryable<TSource> source, System.Collections.Generic.IEqualityComparer<TSource> comparer) { throw null; }
        public static TSource ElementAt<TSource>(this System.Linq.IQueryable<TSource> source, int index) { throw null; }
        public static TSource ElementAtOrDefault<TSource>(this System.Linq.IQueryable<TSource> source, int index) { throw null; }
        public static System.Linq.IQueryable<TSource> Except<TSource>(this System.Linq.IQueryable<TSource> source1, System.Collections.Generic.IEnumerable<TSource> source2) { throw null; }
        public static System.Linq.IQueryable<TSource> Except<TSource>(this System.Linq.IQueryable<TSource> source1, System.Collections.Generic.IEnumerable<TSource> source2, System.Collections.Generic.IEqualityComparer<TSource> comparer) { throw null; }
        public static TSource First<TSource>(this System.Linq.IQueryable<TSource> source) { throw null; }
        public static TSource First<TSource>(this System.Linq.IQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, bool>> predicate) { throw null; }
        public static TSource FirstOrDefault<TSource>(this System.Linq.IQueryable<TSource> source) { throw null; }
        public static TSource FirstOrDefault<TSource>(this System.Linq.IQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, bool>> predicate) { throw null; }
        public static System.Linq.IQueryable<System.Linq.IGrouping<TKey, TSource>> GroupBy<TSource, TKey>(this System.Linq.IQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, TKey>> keySelector) { throw null; }
        public static System.Linq.IQueryable<System.Linq.IGrouping<TKey, TSource>> GroupBy<TSource, TKey>(this System.Linq.IQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, TKey>> keySelector, System.Collections.Generic.IEqualityComparer<TKey> comparer) { throw null; }
        public static System.Linq.IQueryable<System.Linq.IGrouping<TKey, TElement>> GroupBy<TSource, TKey, TElement>(this System.Linq.IQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, TKey>> keySelector, System.Linq.Expressions.Expression<System.Func<TSource, TElement>> elementSelector) { throw null; }
        public static System.Linq.IQueryable<System.Linq.IGrouping<TKey, TElement>> GroupBy<TSource, TKey, TElement>(this System.Linq.IQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, TKey>> keySelector, System.Linq.Expressions.Expression<System.Func<TSource, TElement>> elementSelector, System.Collections.Generic.IEqualityComparer<TKey> comparer) { throw null; }
        public static System.Linq.IQueryable<TResult> GroupBy<TSource, TKey, TResult>(this System.Linq.IQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, TKey>> keySelector, System.Linq.Expressions.Expression<System.Func<TKey, System.Collections.Generic.IEnumerable<TSource>, TResult>> resultSelector) { throw null; }
        public static System.Linq.IQueryable<TResult> GroupBy<TSource, TKey, TResult>(this System.Linq.IQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, TKey>> keySelector, System.Linq.Expressions.Expression<System.Func<TKey, System.Collections.Generic.IEnumerable<TSource>, TResult>> resultSelector, System.Collections.Generic.IEqualityComparer<TKey> comparer) { throw null; }
        public static System.Linq.IQueryable<TResult> GroupBy<TSource, TKey, TElement, TResult>(this System.Linq.IQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, TKey>> keySelector, System.Linq.Expressions.Expression<System.Func<TSource, TElement>> elementSelector, System.Linq.Expressions.Expression<System.Func<TKey, System.Collections.Generic.IEnumerable<TElement>, TResult>> resultSelector) { throw null; }
        public static System.Linq.IQueryable<TResult> GroupBy<TSource, TKey, TElement, TResult>(this System.Linq.IQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, TKey>> keySelector, System.Linq.Expressions.Expression<System.Func<TSource, TElement>> elementSelector, System.Linq.Expressions.Expression<System.Func<TKey, System.Collections.Generic.IEnumerable<TElement>, TResult>> resultSelector, System.Collections.Generic.IEqualityComparer<TKey> comparer) { throw null; }
        public static System.Linq.IQueryable<TResult> GroupJoin<TOuter, TInner, TKey, TResult>(this System.Linq.IQueryable<TOuter> outer, System.Collections.Generic.IEnumerable<TInner> inner, System.Linq.Expressions.Expression<System.Func<TOuter, TKey>> outerKeySelector, System.Linq.Expressions.Expression<System.Func<TInner, TKey>> innerKeySelector, System.Linq.Expressions.Expression<System.Func<TOuter, System.Collections.Generic.IEnumerable<TInner>, TResult>> resultSelector) { throw null; }
        public static System.Linq.IQueryable<TResult> GroupJoin<TOuter, TInner, TKey, TResult>(this System.Linq.IQueryable<TOuter> outer, System.Collections.Generic.IEnumerable<TInner> inner, System.Linq.Expressions.Expression<System.Func<TOuter, TKey>> outerKeySelector, System.Linq.Expressions.Expression<System.Func<TInner, TKey>> innerKeySelector, System.Linq.Expressions.Expression<System.Func<TOuter, System.Collections.Generic.IEnumerable<TInner>, TResult>> resultSelector, System.Collections.Generic.IEqualityComparer<TKey> comparer) { throw null; }
        public static System.Linq.IQueryable<TSource> Intersect<TSource>(this System.Linq.IQueryable<TSource> source1, System.Collections.Generic.IEnumerable<TSource> source2) { throw null; }
        public static System.Linq.IQueryable<TSource> Intersect<TSource>(this System.Linq.IQueryable<TSource> source1, System.Collections.Generic.IEnumerable<TSource> source2, System.Collections.Generic.IEqualityComparer<TSource> comparer) { throw null; }
        public static System.Linq.IQueryable<TResult> Join<TOuter, TInner, TKey, TResult>(this System.Linq.IQueryable<TOuter> outer, System.Collections.Generic.IEnumerable<TInner> inner, System.Linq.Expressions.Expression<System.Func<TOuter, TKey>> outerKeySelector, System.Linq.Expressions.Expression<System.Func<TInner, TKey>> innerKeySelector, System.Linq.Expressions.Expression<System.Func<TOuter, TInner, TResult>> resultSelector) { throw null; }
        public static System.Linq.IQueryable<TResult> Join<TOuter, TInner, TKey, TResult>(this System.Linq.IQueryable<TOuter> outer, System.Collections.Generic.IEnumerable<TInner> inner, System.Linq.Expressions.Expression<System.Func<TOuter, TKey>> outerKeySelector, System.Linq.Expressions.Expression<System.Func<TInner, TKey>> innerKeySelector, System.Linq.Expressions.Expression<System.Func<TOuter, TInner, TResult>> resultSelector, System.Collections.Generic.IEqualityComparer<TKey> comparer) { throw null; }
        public static TSource Last<TSource>(this System.Linq.IQueryable<TSource> source) { throw null; }
        public static TSource Last<TSource>(this System.Linq.IQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, bool>> predicate) { throw null; }
        public static TSource LastOrDefault<TSource>(this System.Linq.IQueryable<TSource> source) { throw null; }
        public static TSource LastOrDefault<TSource>(this System.Linq.IQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, bool>> predicate) { throw null; }
        public static long LongCount<TSource>(this System.Linq.IQueryable<TSource> source) { throw null; }
        public static long LongCount<TSource>(this System.Linq.IQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, bool>> predicate) { throw null; }
        public static TSource Max<TSource>(this System.Linq.IQueryable<TSource> source) { throw null; }
        public static TResult Max<TSource, TResult>(this System.Linq.IQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, TResult>> selector) { throw null; }
        public static TSource Min<TSource>(this System.Linq.IQueryable<TSource> source) { throw null; }
        public static TResult Min<TSource, TResult>(this System.Linq.IQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, TResult>> selector) { throw null; }
        public static System.Linq.IQueryable<TResult> OfType<TResult>(this System.Linq.IQueryable source) { throw null; }
        public static System.Linq.IOrderedQueryable<TSource> OrderBy<TSource, TKey>(this System.Linq.IQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, TKey>> keySelector) { throw null; }
        public static System.Linq.IOrderedQueryable<TSource> OrderBy<TSource, TKey>(this System.Linq.IQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, TKey>> keySelector, System.Collections.Generic.IComparer<TKey> comparer) { throw null; }
        public static System.Linq.IOrderedQueryable<TSource> OrderByDescending<TSource, TKey>(this System.Linq.IQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, TKey>> keySelector) { throw null; }
        public static System.Linq.IOrderedQueryable<TSource> OrderByDescending<TSource, TKey>(this System.Linq.IQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, TKey>> keySelector, System.Collections.Generic.IComparer<TKey> comparer) { throw null; }
        public static System.Linq.IQueryable<TSource> Reverse<TSource>(this System.Linq.IQueryable<TSource> source) { throw null; }
        public static System.Linq.IQueryable<TResult> Select<TSource, TResult>(this System.Linq.IQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, TResult>> selector) { throw null; }
        public static System.Linq.IQueryable<TResult> Select<TSource, TResult>(this System.Linq.IQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, int, TResult>> selector) { throw null; }
        public static System.Linq.IQueryable<TResult> SelectMany<TSource, TResult>(this System.Linq.IQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, System.Collections.Generic.IEnumerable<TResult>>> selector) { throw null; }
        public static System.Linq.IQueryable<TResult> SelectMany<TSource, TResult>(this System.Linq.IQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, int, System.Collections.Generic.IEnumerable<TResult>>> selector) { throw null; }
        public static System.Linq.IQueryable<TResult> SelectMany<TSource, TCollection, TResult>(this System.Linq.IQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, System.Collections.Generic.IEnumerable<TCollection>>> collectionSelector, System.Linq.Expressions.Expression<System.Func<TSource, TCollection, TResult>> resultSelector) { throw null; }
        public static System.Linq.IQueryable<TResult> SelectMany<TSource, TCollection, TResult>(this System.Linq.IQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, int, System.Collections.Generic.IEnumerable<TCollection>>> collectionSelector, System.Linq.Expressions.Expression<System.Func<TSource, TCollection, TResult>> resultSelector) { throw null; }
        public static bool SequenceEqual<TSource>(this System.Linq.IQueryable<TSource> source1, System.Collections.Generic.IEnumerable<TSource> source2) { throw null; }
        public static bool SequenceEqual<TSource>(this System.Linq.IQueryable<TSource> source1, System.Collections.Generic.IEnumerable<TSource> source2, System.Collections.Generic.IEqualityComparer<TSource> comparer) { throw null; }
        public static TSource Single<TSource>(this System.Linq.IQueryable<TSource> source) { throw null; }
        public static TSource Single<TSource>(this System.Linq.IQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, bool>> predicate) { throw null; }
        public static TSource SingleOrDefault<TSource>(this System.Linq.IQueryable<TSource> source) { throw null; }
        public static TSource SingleOrDefault<TSource>(this System.Linq.IQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, bool>> predicate) { throw null; }
        public static System.Linq.IQueryable<TSource> Skip<TSource>(this System.Linq.IQueryable<TSource> source, int count) { throw null; }
        public static System.Linq.IQueryable<TSource> SkipWhile<TSource>(this System.Linq.IQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, bool>> predicate) { throw null; }
        public static System.Linq.IQueryable<TSource> SkipWhile<TSource>(this System.Linq.IQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, int, bool>> predicate) { throw null; }
        public static decimal Sum(this System.Linq.IQueryable<decimal> source) { throw null; }
        public static double Sum(this System.Linq.IQueryable<double> source) { throw null; }
        public static int Sum(this System.Linq.IQueryable<int> source) { throw null; }
        public static long Sum(this System.Linq.IQueryable<long> source) { throw null; }
        public static System.Nullable<decimal> Sum(this System.Linq.IQueryable<System.Nullable<decimal>> source) { throw null; }
        public static System.Nullable<double> Sum(this System.Linq.IQueryable<System.Nullable<double>> source) { throw null; }
        public static System.Nullable<int> Sum(this System.Linq.IQueryable<System.Nullable<int>> source) { throw null; }
        public static System.Nullable<long> Sum(this System.Linq.IQueryable<System.Nullable<long>> source) { throw null; }
        public static System.Nullable<float> Sum(this System.Linq.IQueryable<System.Nullable<float>> source) { throw null; }
        public static float Sum(this System.Linq.IQueryable<float> source) { throw null; }
        public static decimal Sum<TSource>(this System.Linq.IQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, decimal>> selector) { throw null; }
        public static double Sum<TSource>(this System.Linq.IQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, double>> selector) { throw null; }
        public static int Sum<TSource>(this System.Linq.IQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, int>> selector) { throw null; }
        public static long Sum<TSource>(this System.Linq.IQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, long>> selector) { throw null; }
        public static System.Nullable<decimal> Sum<TSource>(this System.Linq.IQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, System.Nullable<decimal>>> selector) { throw null; }
        public static System.Nullable<double> Sum<TSource>(this System.Linq.IQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, System.Nullable<double>>> selector) { throw null; }
        public static System.Nullable<int> Sum<TSource>(this System.Linq.IQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, System.Nullable<int>>> selector) { throw null; }
        public static System.Nullable<long> Sum<TSource>(this System.Linq.IQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, System.Nullable<long>>> selector) { throw null; }
        public static System.Nullable<float> Sum<TSource>(this System.Linq.IQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, System.Nullable<float>>> selector) { throw null; }
        public static float Sum<TSource>(this System.Linq.IQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, float>> selector) { throw null; }
        public static System.Linq.IQueryable<TSource> Take<TSource>(this System.Linq.IQueryable<TSource> source, int count) { throw null; }
        public static System.Linq.IQueryable<TSource> TakeWhile<TSource>(this System.Linq.IQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, bool>> predicate) { throw null; }
        public static System.Linq.IQueryable<TSource> TakeWhile<TSource>(this System.Linq.IQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, int, bool>> predicate) { throw null; }
        public static System.Linq.IOrderedQueryable<TSource> ThenBy<TSource, TKey>(this System.Linq.IOrderedQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, TKey>> keySelector) { throw null; }
        public static System.Linq.IOrderedQueryable<TSource> ThenBy<TSource, TKey>(this System.Linq.IOrderedQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, TKey>> keySelector, System.Collections.Generic.IComparer<TKey> comparer) { throw null; }
        public static System.Linq.IOrderedQueryable<TSource> ThenByDescending<TSource, TKey>(this System.Linq.IOrderedQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, TKey>> keySelector) { throw null; }
        public static System.Linq.IOrderedQueryable<TSource> ThenByDescending<TSource, TKey>(this System.Linq.IOrderedQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, TKey>> keySelector, System.Collections.Generic.IComparer<TKey> comparer) { throw null; }
        public static System.Linq.IQueryable<TSource> Union<TSource>(this System.Linq.IQueryable<TSource> source1, System.Collections.Generic.IEnumerable<TSource> source2) { throw null; }
        public static System.Linq.IQueryable<TSource> Union<TSource>(this System.Linq.IQueryable<TSource> source1, System.Collections.Generic.IEnumerable<TSource> source2, System.Collections.Generic.IEqualityComparer<TSource> comparer) { throw null; }
        public static System.Linq.IQueryable<TSource> Where<TSource>(this System.Linq.IQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, bool>> predicate) { throw null; }
        public static System.Linq.IQueryable<TSource> Where<TSource>(this System.Linq.IQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, int, bool>> predicate) { throw null; }
        public static System.Linq.IQueryable<TResult> Zip<TFirst, TSecond, TResult>(this System.Linq.IQueryable<TFirst> source1, System.Collections.Generic.IEnumerable<TSecond> source2, System.Linq.Expressions.Expression<System.Func<TFirst, TSecond, TResult>> resultSelector) { throw null; }
    }
}
namespace System.Linq.Expressions
{
    public partial class BinaryExpression : System.Linq.Expressions.Expression
    {
        internal BinaryExpression() { }
        public override bool CanReduce { get { throw null; } }
        public System.Linq.Expressions.LambdaExpression Conversion { get { throw null; } }
        public bool IsLifted { get { throw null; } }
        public bool IsLiftedToNull { get { throw null; } }
        public System.Linq.Expressions.Expression Left { get { throw null; } }
        public System.Reflection.MethodInfo Method { get { throw null; } }
        public System.Linq.Expressions.Expression Right { get { throw null; } }
        protected internal override System.Linq.Expressions.Expression Accept(System.Linq.Expressions.ExpressionVisitor visitor) { throw null; }
        public override System.Linq.Expressions.Expression Reduce() { throw null; }
        public System.Linq.Expressions.BinaryExpression Update(System.Linq.Expressions.Expression left, System.Linq.Expressions.LambdaExpression conversion, System.Linq.Expressions.Expression right) { throw null; }
    }
    public partial class BlockExpression : System.Linq.Expressions.Expression
    {
        internal BlockExpression() { }
        public System.Collections.ObjectModel.ReadOnlyCollection<System.Linq.Expressions.Expression> Expressions { get { throw null; } }
        public sealed override System.Linq.Expressions.ExpressionType NodeType { get { throw null; } }
        public System.Linq.Expressions.Expression Result { get { throw null; } }
        public override System.Type Type { get { throw null; } }
        public System.Collections.ObjectModel.ReadOnlyCollection<System.Linq.Expressions.ParameterExpression> Variables { get { throw null; } }
        protected internal override System.Linq.Expressions.Expression Accept(System.Linq.Expressions.ExpressionVisitor visitor) { throw null; }
        public System.Linq.Expressions.BlockExpression Update(System.Collections.Generic.IEnumerable<System.Linq.Expressions.ParameterExpression> variables, System.Collections.Generic.IEnumerable<System.Linq.Expressions.Expression> expressions) { throw null; }
    }
    public sealed partial class CatchBlock
    {
        internal CatchBlock() { }
        public System.Linq.Expressions.Expression Body { get { throw null; } }
        public System.Linq.Expressions.Expression Filter { get { throw null; } }
        public System.Type Test { get { throw null; } }
        public System.Linq.Expressions.ParameterExpression Variable { get { throw null; } }
        public override string ToString() { throw null; }
        public System.Linq.Expressions.CatchBlock Update(System.Linq.Expressions.ParameterExpression variable, System.Linq.Expressions.Expression filter, System.Linq.Expressions.Expression body) { throw null; }
    }
    public partial class ConditionalExpression : System.Linq.Expressions.Expression
    {
        internal ConditionalExpression() { }
        public System.Linq.Expressions.Expression IfFalse { get { throw null; } }
        public System.Linq.Expressions.Expression IfTrue { get { throw null; } }
        public sealed override System.Linq.Expressions.ExpressionType NodeType { get { throw null; } }
        public System.Linq.Expressions.Expression Test { get { throw null; } }
        public override System.Type Type { get { throw null; } }
        protected internal override System.Linq.Expressions.Expression Accept(System.Linq.Expressions.ExpressionVisitor visitor) { throw null; }
        public System.Linq.Expressions.ConditionalExpression Update(System.Linq.Expressions.Expression test, System.Linq.Expressions.Expression ifTrue, System.Linq.Expressions.Expression ifFalse) { throw null; }
    }
    public partial class ConstantExpression : System.Linq.Expressions.Expression
    {
        internal ConstantExpression() { }
        public sealed override System.Linq.Expressions.ExpressionType NodeType { get { throw null; } }
        public override System.Type Type { get { throw null; } }
        public object Value { get { throw null; } }
        protected internal override System.Linq.Expressions.Expression Accept(System.Linq.Expressions.ExpressionVisitor visitor) { throw null; }
    }
    public partial class DebugInfoExpression : System.Linq.Expressions.Expression
    {
        internal DebugInfoExpression() { }
        public System.Linq.Expressions.SymbolDocumentInfo Document { get { throw null; } }
        public virtual int EndColumn { get { throw null; } }
        public virtual int EndLine { get { throw null; } }
        public virtual bool IsClear { get { throw null; } }
        public sealed override System.Linq.Expressions.ExpressionType NodeType { get { throw null; } }
        public virtual int StartColumn { get { throw null; } }
        public virtual int StartLine { get { throw null; } }
        public sealed override System.Type Type { get { throw null; } }
        protected internal override System.Linq.Expressions.Expression Accept(System.Linq.Expressions.ExpressionVisitor visitor) { throw null; }
    }
    public sealed partial class DefaultExpression : System.Linq.Expressions.Expression
    {
        internal DefaultExpression() { }
        public sealed override System.Linq.Expressions.ExpressionType NodeType { get { throw null; } }
        public sealed override System.Type Type { get { throw null; } }
        protected internal override System.Linq.Expressions.Expression Accept(System.Linq.Expressions.ExpressionVisitor visitor) { throw null; }
    }
    public partial class DynamicExpression : System.Linq.Expressions.Expression, System.Linq.Expressions.IArgumentProvider, System.Linq.Expressions.IDynamicExpression
    {
        internal DynamicExpression() { }
        public System.Collections.ObjectModel.ReadOnlyCollection<System.Linq.Expressions.Expression> Arguments { get { throw null; } }
        public System.Runtime.CompilerServices.CallSiteBinder Binder { get { throw null; } }
        public System.Type DelegateType { get { throw null; } }
        public sealed override System.Linq.Expressions.ExpressionType NodeType { get { throw null; } }
        int System.Linq.Expressions.IArgumentProvider.ArgumentCount { get { throw null; } }
        public override System.Type Type { get { throw null; } }
        protected internal override System.Linq.Expressions.Expression Accept(System.Linq.Expressions.ExpressionVisitor visitor) { throw null; }
        public static new System.Linq.Expressions.DynamicExpression Dynamic(System.Runtime.CompilerServices.CallSiteBinder binder, System.Type returnType, System.Collections.Generic.IEnumerable<System.Linq.Expressions.Expression> arguments) { throw null; }
        public static new System.Linq.Expressions.DynamicExpression Dynamic(System.Runtime.CompilerServices.CallSiteBinder binder, System.Type returnType, System.Linq.Expressions.Expression arg0) { throw null; }
        public static new System.Linq.Expressions.DynamicExpression Dynamic(System.Runtime.CompilerServices.CallSiteBinder binder, System.Type returnType, System.Linq.Expressions.Expression arg0, System.Linq.Expressions.Expression arg1) { throw null; }
        public static new System.Linq.Expressions.DynamicExpression Dynamic(System.Runtime.CompilerServices.CallSiteBinder binder, System.Type returnType, System.Linq.Expressions.Expression arg0, System.Linq.Expressions.Expression arg1, System.Linq.Expressions.Expression arg2) { throw null; }
        public static new System.Linq.Expressions.DynamicExpression Dynamic(System.Runtime.CompilerServices.CallSiteBinder binder, System.Type returnType, System.Linq.Expressions.Expression arg0, System.Linq.Expressions.Expression arg1, System.Linq.Expressions.Expression arg2, System.Linq.Expressions.Expression arg3) { throw null; }
        public static new System.Linq.Expressions.DynamicExpression Dynamic(System.Runtime.CompilerServices.CallSiteBinder binder, System.Type returnType, params System.Linq.Expressions.Expression[] arguments) { throw null; }
        public static new System.Linq.Expressions.DynamicExpression MakeDynamic(System.Type delegateType, System.Runtime.CompilerServices.CallSiteBinder binder, System.Collections.Generic.IEnumerable<System.Linq.Expressions.Expression> arguments) { throw null; }
        public static new System.Linq.Expressions.DynamicExpression MakeDynamic(System.Type delegateType, System.Runtime.CompilerServices.CallSiteBinder binder, System.Linq.Expressions.Expression arg0) { throw null; }
        public static new System.Linq.Expressions.DynamicExpression MakeDynamic(System.Type delegateType, System.Runtime.CompilerServices.CallSiteBinder binder, System.Linq.Expressions.Expression arg0, System.Linq.Expressions.Expression arg1) { throw null; }
        public static new System.Linq.Expressions.DynamicExpression MakeDynamic(System.Type delegateType, System.Runtime.CompilerServices.CallSiteBinder binder, System.Linq.Expressions.Expression arg0, System.Linq.Expressions.Expression arg1, System.Linq.Expressions.Expression arg2) { throw null; }
        public static new System.Linq.Expressions.DynamicExpression MakeDynamic(System.Type delegateType, System.Runtime.CompilerServices.CallSiteBinder binder, System.Linq.Expressions.Expression arg0, System.Linq.Expressions.Expression arg1, System.Linq.Expressions.Expression arg2, System.Linq.Expressions.Expression arg3) { throw null; }
        public static new System.Linq.Expressions.DynamicExpression MakeDynamic(System.Type delegateType, System.Runtime.CompilerServices.CallSiteBinder binder, params System.Linq.Expressions.Expression[] arguments) { throw null; }
        System.Linq.Expressions.Expression System.Linq.Expressions.IArgumentProvider.GetArgument(int index) { throw null; }
        object System.Linq.Expressions.IDynamicExpression.CreateCallSite() { throw null; }
        System.Linq.Expressions.Expression System.Linq.Expressions.IDynamicExpression.Rewrite(System.Linq.Expressions.Expression[] args) { throw null; }
        public System.Linq.Expressions.DynamicExpression Update(System.Collections.Generic.IEnumerable<System.Linq.Expressions.Expression> arguments) { throw null; }
    }
    public abstract partial class DynamicExpressionVisitor : System.Linq.Expressions.ExpressionVisitor
    {
        protected DynamicExpressionVisitor() { }
        protected internal override System.Linq.Expressions.Expression VisitDynamic(System.Linq.Expressions.DynamicExpression node) { throw null; }
    }
    public sealed partial class ElementInit : System.Linq.Expressions.IArgumentProvider
    {
        internal ElementInit() { }
        public System.Reflection.MethodInfo AddMethod { get { throw null; } }
        public System.Collections.ObjectModel.ReadOnlyCollection<System.Linq.Expressions.Expression> Arguments { get { throw null; } }
        int System.Linq.Expressions.IArgumentProvider.ArgumentCount { get { throw null; } }
        System.Linq.Expressions.Expression System.Linq.Expressions.IArgumentProvider.GetArgument(int index) { throw null; }
        public override string ToString() { throw null; }
        public System.Linq.Expressions.ElementInit Update(System.Collections.Generic.IEnumerable<System.Linq.Expressions.Expression> arguments) { throw null; }
    }
    public abstract partial class Expression
    {
        protected Expression() { }
        [System.ObsoleteAttribute("use a different constructor that does not take ExpressionType. Then override NodeType and Type properties to provide the values that would be specified to this constructor.")]
        protected Expression(System.Linq.Expressions.ExpressionType nodeType, System.Type type) { }
        public virtual bool CanReduce { get { throw null; } }
        public virtual System.Linq.Expressions.ExpressionType NodeType { get { throw null; } }
        public virtual System.Type Type { get { throw null; } }
        protected internal virtual System.Linq.Expressions.Expression Accept(System.Linq.Expressions.ExpressionVisitor visitor) { throw null; }
        public static System.Linq.Expressions.BinaryExpression Add(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right) { throw null; }
        public static System.Linq.Expressions.BinaryExpression Add(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right, System.Reflection.MethodInfo method) { throw null; }
        public static System.Linq.Expressions.BinaryExpression AddAssign(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right) { throw null; }
        public static System.Linq.Expressions.BinaryExpression AddAssign(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right, System.Reflection.MethodInfo method) { throw null; }
        public static System.Linq.Expressions.BinaryExpression AddAssign(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right, System.Reflection.MethodInfo method, System.Linq.Expressions.LambdaExpression conversion) { throw null; }
        public static System.Linq.Expressions.BinaryExpression AddAssignChecked(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right) { throw null; }
        public static System.Linq.Expressions.BinaryExpression AddAssignChecked(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right, System.Reflection.MethodInfo method) { throw null; }
        public static System.Linq.Expressions.BinaryExpression AddAssignChecked(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right, System.Reflection.MethodInfo method, System.Linq.Expressions.LambdaExpression conversion) { throw null; }
        public static System.Linq.Expressions.BinaryExpression AddChecked(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right) { throw null; }
        public static System.Linq.Expressions.BinaryExpression AddChecked(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right, System.Reflection.MethodInfo method) { throw null; }
        public static System.Linq.Expressions.BinaryExpression And(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right) { throw null; }
        public static System.Linq.Expressions.BinaryExpression And(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right, System.Reflection.MethodInfo method) { throw null; }
        public static System.Linq.Expressions.BinaryExpression AndAlso(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right) { throw null; }
        public static System.Linq.Expressions.BinaryExpression AndAlso(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right, System.Reflection.MethodInfo method) { throw null; }
        public static System.Linq.Expressions.BinaryExpression AndAssign(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right) { throw null; }
        public static System.Linq.Expressions.BinaryExpression AndAssign(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right, System.Reflection.MethodInfo method) { throw null; }
        public static System.Linq.Expressions.BinaryExpression AndAssign(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right, System.Reflection.MethodInfo method, System.Linq.Expressions.LambdaExpression conversion) { throw null; }
        public static System.Linq.Expressions.IndexExpression ArrayAccess(System.Linq.Expressions.Expression array, System.Collections.Generic.IEnumerable<System.Linq.Expressions.Expression> indexes) { throw null; }
        public static System.Linq.Expressions.IndexExpression ArrayAccess(System.Linq.Expressions.Expression array, params System.Linq.Expressions.Expression[] indexes) { throw null; }
        public static System.Linq.Expressions.MethodCallExpression ArrayIndex(System.Linq.Expressions.Expression array, System.Collections.Generic.IEnumerable<System.Linq.Expressions.Expression> indexes) { throw null; }
        public static System.Linq.Expressions.BinaryExpression ArrayIndex(System.Linq.Expressions.Expression array, System.Linq.Expressions.Expression index) { throw null; }
        public static System.Linq.Expressions.MethodCallExpression ArrayIndex(System.Linq.Expressions.Expression array, params System.Linq.Expressions.Expression[] indexes) { throw null; }
        public static System.Linq.Expressions.UnaryExpression ArrayLength(System.Linq.Expressions.Expression array) { throw null; }
        public static System.Linq.Expressions.BinaryExpression Assign(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right) { throw null; }
        public static System.Linq.Expressions.MemberAssignment Bind(System.Reflection.MemberInfo member, System.Linq.Expressions.Expression expression) { throw null; }
        public static System.Linq.Expressions.MemberAssignment Bind(System.Reflection.MethodInfo propertyAccessor, System.Linq.Expressions.Expression expression) { throw null; }
        public static System.Linq.Expressions.BlockExpression Block(System.Collections.Generic.IEnumerable<System.Linq.Expressions.Expression> expressions) { throw null; }
        public static System.Linq.Expressions.BlockExpression Block(System.Collections.Generic.IEnumerable<System.Linq.Expressions.ParameterExpression> variables, System.Collections.Generic.IEnumerable<System.Linq.Expressions.Expression> expressions) { throw null; }
        public static System.Linq.Expressions.BlockExpression Block(System.Collections.Generic.IEnumerable<System.Linq.Expressions.ParameterExpression> variables, params System.Linq.Expressions.Expression[] expressions) { throw null; }
        public static System.Linq.Expressions.BlockExpression Block(System.Linq.Expressions.Expression arg0, System.Linq.Expressions.Expression arg1) { throw null; }
        public static System.Linq.Expressions.BlockExpression Block(System.Linq.Expressions.Expression arg0, System.Linq.Expressions.Expression arg1, System.Linq.Expressions.Expression arg2) { throw null; }
        public static System.Linq.Expressions.BlockExpression Block(System.Linq.Expressions.Expression arg0, System.Linq.Expressions.Expression arg1, System.Linq.Expressions.Expression arg2, System.Linq.Expressions.Expression arg3) { throw null; }
        public static System.Linq.Expressions.BlockExpression Block(System.Linq.Expressions.Expression arg0, System.Linq.Expressions.Expression arg1, System.Linq.Expressions.Expression arg2, System.Linq.Expressions.Expression arg3, System.Linq.Expressions.Expression arg4) { throw null; }
        public static System.Linq.Expressions.BlockExpression Block(params System.Linq.Expressions.Expression[] expressions) { throw null; }
        public static System.Linq.Expressions.BlockExpression Block(System.Type type, System.Collections.Generic.IEnumerable<System.Linq.Expressions.Expression> expressions) { throw null; }
        public static System.Linq.Expressions.BlockExpression Block(System.Type type, System.Collections.Generic.IEnumerable<System.Linq.Expressions.ParameterExpression> variables, System.Collections.Generic.IEnumerable<System.Linq.Expressions.Expression> expressions) { throw null; }
        public static System.Linq.Expressions.BlockExpression Block(System.Type type, System.Collections.Generic.IEnumerable<System.Linq.Expressions.ParameterExpression> variables, params System.Linq.Expressions.Expression[] expressions) { throw null; }
        public static System.Linq.Expressions.BlockExpression Block(System.Type type, params System.Linq.Expressions.Expression[] expressions) { throw null; }
        public static System.Linq.Expressions.GotoExpression Break(System.Linq.Expressions.LabelTarget target) { throw null; }
        public static System.Linq.Expressions.GotoExpression Break(System.Linq.Expressions.LabelTarget target, System.Linq.Expressions.Expression value) { throw null; }
        public static System.Linq.Expressions.GotoExpression Break(System.Linq.Expressions.LabelTarget target, System.Linq.Expressions.Expression value, System.Type type) { throw null; }
        public static System.Linq.Expressions.GotoExpression Break(System.Linq.Expressions.LabelTarget target, System.Type type) { throw null; }
        public static System.Linq.Expressions.MethodCallExpression Call(System.Linq.Expressions.Expression instance, System.Reflection.MethodInfo method) { throw null; }
        public static System.Linq.Expressions.MethodCallExpression Call(System.Linq.Expressions.Expression instance, System.Reflection.MethodInfo method, System.Collections.Generic.IEnumerable<System.Linq.Expressions.Expression> arguments) { throw null; }
        public static System.Linq.Expressions.MethodCallExpression Call(System.Linq.Expressions.Expression instance, System.Reflection.MethodInfo method, System.Linq.Expressions.Expression arg0, System.Linq.Expressions.Expression arg1) { throw null; }
        public static System.Linq.Expressions.MethodCallExpression Call(System.Linq.Expressions.Expression instance, System.Reflection.MethodInfo method, System.Linq.Expressions.Expression arg0, System.Linq.Expressions.Expression arg1, System.Linq.Expressions.Expression arg2) { throw null; }
        public static System.Linq.Expressions.MethodCallExpression Call(System.Linq.Expressions.Expression instance, System.Reflection.MethodInfo method, params System.Linq.Expressions.Expression[] arguments) { throw null; }
        public static System.Linq.Expressions.MethodCallExpression Call(System.Linq.Expressions.Expression instance, string methodName, System.Type[] typeArguments, params System.Linq.Expressions.Expression[] arguments) { throw null; }
        public static System.Linq.Expressions.MethodCallExpression Call(System.Reflection.MethodInfo method, System.Collections.Generic.IEnumerable<System.Linq.Expressions.Expression> arguments) { throw null; }
        public static System.Linq.Expressions.MethodCallExpression Call(System.Reflection.MethodInfo method, System.Linq.Expressions.Expression arg0) { throw null; }
        public static System.Linq.Expressions.MethodCallExpression Call(System.Reflection.MethodInfo method, System.Linq.Expressions.Expression arg0, System.Linq.Expressions.Expression arg1) { throw null; }
        public static System.Linq.Expressions.MethodCallExpression Call(System.Reflection.MethodInfo method, System.Linq.Expressions.Expression arg0, System.Linq.Expressions.Expression arg1, System.Linq.Expressions.Expression arg2) { throw null; }
        public static System.Linq.Expressions.MethodCallExpression Call(System.Reflection.MethodInfo method, System.Linq.Expressions.Expression arg0, System.Linq.Expressions.Expression arg1, System.Linq.Expressions.Expression arg2, System.Linq.Expressions.Expression arg3) { throw null; }
        public static System.Linq.Expressions.MethodCallExpression Call(System.Reflection.MethodInfo method, System.Linq.Expressions.Expression arg0, System.Linq.Expressions.Expression arg1, System.Linq.Expressions.Expression arg2, System.Linq.Expressions.Expression arg3, System.Linq.Expressions.Expression arg4) { throw null; }
        public static System.Linq.Expressions.MethodCallExpression Call(System.Reflection.MethodInfo method, params System.Linq.Expressions.Expression[] arguments) { throw null; }
        public static System.Linq.Expressions.MethodCallExpression Call(System.Type type, string methodName, System.Type[] typeArguments, params System.Linq.Expressions.Expression[] arguments) { throw null; }
        public static System.Linq.Expressions.CatchBlock Catch(System.Linq.Expressions.ParameterExpression variable, System.Linq.Expressions.Expression body) { throw null; }
        public static System.Linq.Expressions.CatchBlock Catch(System.Linq.Expressions.ParameterExpression variable, System.Linq.Expressions.Expression body, System.Linq.Expressions.Expression filter) { throw null; }
        public static System.Linq.Expressions.CatchBlock Catch(System.Type type, System.Linq.Expressions.Expression body) { throw null; }
        public static System.Linq.Expressions.CatchBlock Catch(System.Type type, System.Linq.Expressions.Expression body, System.Linq.Expressions.Expression filter) { throw null; }
        public static System.Linq.Expressions.DebugInfoExpression ClearDebugInfo(System.Linq.Expressions.SymbolDocumentInfo document) { throw null; }
        public static System.Linq.Expressions.BinaryExpression Coalesce(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right) { throw null; }
        public static System.Linq.Expressions.BinaryExpression Coalesce(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right, System.Linq.Expressions.LambdaExpression conversion) { throw null; }
        public static System.Linq.Expressions.ConditionalExpression Condition(System.Linq.Expressions.Expression test, System.Linq.Expressions.Expression ifTrue, System.Linq.Expressions.Expression ifFalse) { throw null; }
        public static System.Linq.Expressions.ConditionalExpression Condition(System.Linq.Expressions.Expression test, System.Linq.Expressions.Expression ifTrue, System.Linq.Expressions.Expression ifFalse, System.Type type) { throw null; }
        public static System.Linq.Expressions.ConstantExpression Constant(object value) { throw null; }
        public static System.Linq.Expressions.ConstantExpression Constant(object value, System.Type type) { throw null; }
        public static System.Linq.Expressions.GotoExpression Continue(System.Linq.Expressions.LabelTarget target) { throw null; }
        public static System.Linq.Expressions.GotoExpression Continue(System.Linq.Expressions.LabelTarget target, System.Type type) { throw null; }
        public static System.Linq.Expressions.UnaryExpression Convert(System.Linq.Expressions.Expression expression, System.Type type) { throw null; }
        public static System.Linq.Expressions.UnaryExpression Convert(System.Linq.Expressions.Expression expression, System.Type type, System.Reflection.MethodInfo method) { throw null; }
        public static System.Linq.Expressions.UnaryExpression ConvertChecked(System.Linq.Expressions.Expression expression, System.Type type) { throw null; }
        public static System.Linq.Expressions.UnaryExpression ConvertChecked(System.Linq.Expressions.Expression expression, System.Type type, System.Reflection.MethodInfo method) { throw null; }
        public static System.Linq.Expressions.DebugInfoExpression DebugInfo(System.Linq.Expressions.SymbolDocumentInfo document, int startLine, int startColumn, int endLine, int endColumn) { throw null; }
        public static System.Linq.Expressions.UnaryExpression Decrement(System.Linq.Expressions.Expression expression) { throw null; }
        public static System.Linq.Expressions.UnaryExpression Decrement(System.Linq.Expressions.Expression expression, System.Reflection.MethodInfo method) { throw null; }
        public static System.Linq.Expressions.DefaultExpression Default(System.Type type) { throw null; }
        public static System.Linq.Expressions.BinaryExpression Divide(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right) { throw null; }
        public static System.Linq.Expressions.BinaryExpression Divide(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right, System.Reflection.MethodInfo method) { throw null; }
        public static System.Linq.Expressions.BinaryExpression DivideAssign(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right) { throw null; }
        public static System.Linq.Expressions.BinaryExpression DivideAssign(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right, System.Reflection.MethodInfo method) { throw null; }
        public static System.Linq.Expressions.BinaryExpression DivideAssign(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right, System.Reflection.MethodInfo method, System.Linq.Expressions.LambdaExpression conversion) { throw null; }
        public static System.Linq.Expressions.DynamicExpression Dynamic(System.Runtime.CompilerServices.CallSiteBinder binder, System.Type returnType, System.Collections.Generic.IEnumerable<System.Linq.Expressions.Expression> arguments) { throw null; }
        public static System.Linq.Expressions.DynamicExpression Dynamic(System.Runtime.CompilerServices.CallSiteBinder binder, System.Type returnType, System.Linq.Expressions.Expression arg0) { throw null; }
        public static System.Linq.Expressions.DynamicExpression Dynamic(System.Runtime.CompilerServices.CallSiteBinder binder, System.Type returnType, System.Linq.Expressions.Expression arg0, System.Linq.Expressions.Expression arg1) { throw null; }
        public static System.Linq.Expressions.DynamicExpression Dynamic(System.Runtime.CompilerServices.CallSiteBinder binder, System.Type returnType, System.Linq.Expressions.Expression arg0, System.Linq.Expressions.Expression arg1, System.Linq.Expressions.Expression arg2) { throw null; }
        public static System.Linq.Expressions.DynamicExpression Dynamic(System.Runtime.CompilerServices.CallSiteBinder binder, System.Type returnType, System.Linq.Expressions.Expression arg0, System.Linq.Expressions.Expression arg1, System.Linq.Expressions.Expression arg2, System.Linq.Expressions.Expression arg3) { throw null; }
        public static System.Linq.Expressions.DynamicExpression Dynamic(System.Runtime.CompilerServices.CallSiteBinder binder, System.Type returnType, params System.Linq.Expressions.Expression[] arguments) { throw null; }
        public static System.Linq.Expressions.ElementInit ElementInit(System.Reflection.MethodInfo addMethod, System.Collections.Generic.IEnumerable<System.Linq.Expressions.Expression> arguments) { throw null; }
        public static System.Linq.Expressions.ElementInit ElementInit(System.Reflection.MethodInfo addMethod, params System.Linq.Expressions.Expression[] arguments) { throw null; }
        public static System.Linq.Expressions.DefaultExpression Empty() { throw null; }
        public static System.Linq.Expressions.BinaryExpression Equal(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right) { throw null; }
        public static System.Linq.Expressions.BinaryExpression Equal(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right, bool liftToNull, System.Reflection.MethodInfo method) { throw null; }
        public static System.Linq.Expressions.BinaryExpression ExclusiveOr(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right) { throw null; }
        public static System.Linq.Expressions.BinaryExpression ExclusiveOr(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right, System.Reflection.MethodInfo method) { throw null; }
        public static System.Linq.Expressions.BinaryExpression ExclusiveOrAssign(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right) { throw null; }
        public static System.Linq.Expressions.BinaryExpression ExclusiveOrAssign(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right, System.Reflection.MethodInfo method) { throw null; }
        public static System.Linq.Expressions.BinaryExpression ExclusiveOrAssign(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right, System.Reflection.MethodInfo method, System.Linq.Expressions.LambdaExpression conversion) { throw null; }
        public static System.Linq.Expressions.MemberExpression Field(System.Linq.Expressions.Expression expression, System.Reflection.FieldInfo field) { throw null; }
        public static System.Linq.Expressions.MemberExpression Field(System.Linq.Expressions.Expression expression, string fieldName) { throw null; }
        public static System.Linq.Expressions.MemberExpression Field(System.Linq.Expressions.Expression expression, System.Type type, string fieldName) { throw null; }
        public static System.Type GetActionType(params System.Type[] typeArgs) { throw null; }
        public static System.Type GetDelegateType(params System.Type[] typeArgs) { throw null; }
        public static System.Type GetFuncType(params System.Type[] typeArgs) { throw null; }
        public static System.Linq.Expressions.GotoExpression Goto(System.Linq.Expressions.LabelTarget target) { throw null; }
        public static System.Linq.Expressions.GotoExpression Goto(System.Linq.Expressions.LabelTarget target, System.Linq.Expressions.Expression value) { throw null; }
        public static System.Linq.Expressions.GotoExpression Goto(System.Linq.Expressions.LabelTarget target, System.Linq.Expressions.Expression value, System.Type type) { throw null; }
        public static System.Linq.Expressions.GotoExpression Goto(System.Linq.Expressions.LabelTarget target, System.Type type) { throw null; }
        public static System.Linq.Expressions.BinaryExpression GreaterThan(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right) { throw null; }
        public static System.Linq.Expressions.BinaryExpression GreaterThan(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right, bool liftToNull, System.Reflection.MethodInfo method) { throw null; }
        public static System.Linq.Expressions.BinaryExpression GreaterThanOrEqual(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right) { throw null; }
        public static System.Linq.Expressions.BinaryExpression GreaterThanOrEqual(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right, bool liftToNull, System.Reflection.MethodInfo method) { throw null; }
        public static System.Linq.Expressions.ConditionalExpression IfThen(System.Linq.Expressions.Expression test, System.Linq.Expressions.Expression ifTrue) { throw null; }
        public static System.Linq.Expressions.ConditionalExpression IfThenElse(System.Linq.Expressions.Expression test, System.Linq.Expressions.Expression ifTrue, System.Linq.Expressions.Expression ifFalse) { throw null; }
        public static System.Linq.Expressions.UnaryExpression Increment(System.Linq.Expressions.Expression expression) { throw null; }
        public static System.Linq.Expressions.UnaryExpression Increment(System.Linq.Expressions.Expression expression, System.Reflection.MethodInfo method) { throw null; }
        public static System.Linq.Expressions.InvocationExpression Invoke(System.Linq.Expressions.Expression expression, System.Collections.Generic.IEnumerable<System.Linq.Expressions.Expression> arguments) { throw null; }
        public static System.Linq.Expressions.InvocationExpression Invoke(System.Linq.Expressions.Expression expression, params System.Linq.Expressions.Expression[] arguments) { throw null; }
        public static System.Linq.Expressions.UnaryExpression IsFalse(System.Linq.Expressions.Expression expression) { throw null; }
        public static System.Linq.Expressions.UnaryExpression IsFalse(System.Linq.Expressions.Expression expression, System.Reflection.MethodInfo method) { throw null; }
        public static System.Linq.Expressions.UnaryExpression IsTrue(System.Linq.Expressions.Expression expression) { throw null; }
        public static System.Linq.Expressions.UnaryExpression IsTrue(System.Linq.Expressions.Expression expression, System.Reflection.MethodInfo method) { throw null; }
        public static System.Linq.Expressions.LabelTarget Label() { throw null; }
        public static System.Linq.Expressions.LabelExpression Label(System.Linq.Expressions.LabelTarget target) { throw null; }
        public static System.Linq.Expressions.LabelExpression Label(System.Linq.Expressions.LabelTarget target, System.Linq.Expressions.Expression defaultValue) { throw null; }
        public static System.Linq.Expressions.LabelTarget Label(string name) { throw null; }
        public static System.Linq.Expressions.LabelTarget Label(System.Type type) { throw null; }
        public static System.Linq.Expressions.LabelTarget Label(System.Type type, string name) { throw null; }
        public static System.Linq.Expressions.LambdaExpression Lambda(System.Linq.Expressions.Expression body, bool tailCall, System.Collections.Generic.IEnumerable<System.Linq.Expressions.ParameterExpression> parameters) { throw null; }
        public static System.Linq.Expressions.LambdaExpression Lambda(System.Linq.Expressions.Expression body, bool tailCall, params System.Linq.Expressions.ParameterExpression[] parameters) { throw null; }
        public static System.Linq.Expressions.LambdaExpression Lambda(System.Linq.Expressions.Expression body, System.Collections.Generic.IEnumerable<System.Linq.Expressions.ParameterExpression> parameters) { throw null; }
        public static System.Linq.Expressions.LambdaExpression Lambda(System.Linq.Expressions.Expression body, params System.Linq.Expressions.ParameterExpression[] parameters) { throw null; }
        public static System.Linq.Expressions.LambdaExpression Lambda(System.Linq.Expressions.Expression body, string name, bool tailCall, System.Collections.Generic.IEnumerable<System.Linq.Expressions.ParameterExpression> parameters) { throw null; }
        public static System.Linq.Expressions.LambdaExpression Lambda(System.Linq.Expressions.Expression body, string name, System.Collections.Generic.IEnumerable<System.Linq.Expressions.ParameterExpression> parameters) { throw null; }
        public static System.Linq.Expressions.LambdaExpression Lambda(System.Type delegateType, System.Linq.Expressions.Expression body, bool tailCall, System.Collections.Generic.IEnumerable<System.Linq.Expressions.ParameterExpression> parameters) { throw null; }
        public static System.Linq.Expressions.LambdaExpression Lambda(System.Type delegateType, System.Linq.Expressions.Expression body, bool tailCall, params System.Linq.Expressions.ParameterExpression[] parameters) { throw null; }
        public static System.Linq.Expressions.LambdaExpression Lambda(System.Type delegateType, System.Linq.Expressions.Expression body, System.Collections.Generic.IEnumerable<System.Linq.Expressions.ParameterExpression> parameters) { throw null; }
        public static System.Linq.Expressions.LambdaExpression Lambda(System.Type delegateType, System.Linq.Expressions.Expression body, params System.Linq.Expressions.ParameterExpression[] parameters) { throw null; }
        public static System.Linq.Expressions.LambdaExpression Lambda(System.Type delegateType, System.Linq.Expressions.Expression body, string name, bool tailCall, System.Collections.Generic.IEnumerable<System.Linq.Expressions.ParameterExpression> parameters) { throw null; }
        public static System.Linq.Expressions.LambdaExpression Lambda(System.Type delegateType, System.Linq.Expressions.Expression body, string name, System.Collections.Generic.IEnumerable<System.Linq.Expressions.ParameterExpression> parameters) { throw null; }
        public static System.Linq.Expressions.Expression<TDelegate> Lambda<TDelegate>(System.Linq.Expressions.Expression body, bool tailCall, System.Collections.Generic.IEnumerable<System.Linq.Expressions.ParameterExpression> parameters) { throw null; }
        public static System.Linq.Expressions.Expression<TDelegate> Lambda<TDelegate>(System.Linq.Expressions.Expression body, bool tailCall, params System.Linq.Expressions.ParameterExpression[] parameters) { throw null; }
        public static System.Linq.Expressions.Expression<TDelegate> Lambda<TDelegate>(System.Linq.Expressions.Expression body, System.Collections.Generic.IEnumerable<System.Linq.Expressions.ParameterExpression> parameters) { throw null; }
        public static System.Linq.Expressions.Expression<TDelegate> Lambda<TDelegate>(System.Linq.Expressions.Expression body, params System.Linq.Expressions.ParameterExpression[] parameters) { throw null; }
        public static System.Linq.Expressions.Expression<TDelegate> Lambda<TDelegate>(System.Linq.Expressions.Expression body, string name, bool tailCall, System.Collections.Generic.IEnumerable<System.Linq.Expressions.ParameterExpression> parameters) { throw null; }
        public static System.Linq.Expressions.Expression<TDelegate> Lambda<TDelegate>(System.Linq.Expressions.Expression body, string name, System.Collections.Generic.IEnumerable<System.Linq.Expressions.ParameterExpression> parameters) { throw null; }
        public static System.Linq.Expressions.BinaryExpression LeftShift(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right) { throw null; }
        public static System.Linq.Expressions.BinaryExpression LeftShift(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right, System.Reflection.MethodInfo method) { throw null; }
        public static System.Linq.Expressions.BinaryExpression LeftShiftAssign(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right) { throw null; }
        public static System.Linq.Expressions.BinaryExpression LeftShiftAssign(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right, System.Reflection.MethodInfo method) { throw null; }
        public static System.Linq.Expressions.BinaryExpression LeftShiftAssign(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right, System.Reflection.MethodInfo method, System.Linq.Expressions.LambdaExpression conversion) { throw null; }
        public static System.Linq.Expressions.BinaryExpression LessThan(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right) { throw null; }
        public static System.Linq.Expressions.BinaryExpression LessThan(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right, bool liftToNull, System.Reflection.MethodInfo method) { throw null; }
        public static System.Linq.Expressions.BinaryExpression LessThanOrEqual(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right) { throw null; }
        public static System.Linq.Expressions.BinaryExpression LessThanOrEqual(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right, bool liftToNull, System.Reflection.MethodInfo method) { throw null; }
        public static System.Linq.Expressions.MemberListBinding ListBind(System.Reflection.MemberInfo member, System.Collections.Generic.IEnumerable<System.Linq.Expressions.ElementInit> initializers) { throw null; }
        public static System.Linq.Expressions.MemberListBinding ListBind(System.Reflection.MemberInfo member, params System.Linq.Expressions.ElementInit[] initializers) { throw null; }
        public static System.Linq.Expressions.MemberListBinding ListBind(System.Reflection.MethodInfo propertyAccessor, System.Collections.Generic.IEnumerable<System.Linq.Expressions.ElementInit> initializers) { throw null; }
        public static System.Linq.Expressions.MemberListBinding ListBind(System.Reflection.MethodInfo propertyAccessor, params System.Linq.Expressions.ElementInit[] initializers) { throw null; }
        public static System.Linq.Expressions.ListInitExpression ListInit(System.Linq.Expressions.NewExpression newExpression, System.Collections.Generic.IEnumerable<System.Linq.Expressions.ElementInit> initializers) { throw null; }
        public static System.Linq.Expressions.ListInitExpression ListInit(System.Linq.Expressions.NewExpression newExpression, System.Collections.Generic.IEnumerable<System.Linq.Expressions.Expression> initializers) { throw null; }
        public static System.Linq.Expressions.ListInitExpression ListInit(System.Linq.Expressions.NewExpression newExpression, params System.Linq.Expressions.ElementInit[] initializers) { throw null; }
        public static System.Linq.Expressions.ListInitExpression ListInit(System.Linq.Expressions.NewExpression newExpression, params System.Linq.Expressions.Expression[] initializers) { throw null; }
        public static System.Linq.Expressions.ListInitExpression ListInit(System.Linq.Expressions.NewExpression newExpression, System.Reflection.MethodInfo addMethod, System.Collections.Generic.IEnumerable<System.Linq.Expressions.Expression> initializers) { throw null; }
        public static System.Linq.Expressions.ListInitExpression ListInit(System.Linq.Expressions.NewExpression newExpression, System.Reflection.MethodInfo addMethod, params System.Linq.Expressions.Expression[] initializers) { throw null; }
        public static System.Linq.Expressions.LoopExpression Loop(System.Linq.Expressions.Expression body) { throw null; }
        public static System.Linq.Expressions.LoopExpression Loop(System.Linq.Expressions.Expression body, System.Linq.Expressions.LabelTarget @break) { throw null; }
        public static System.Linq.Expressions.LoopExpression Loop(System.Linq.Expressions.Expression body, System.Linq.Expressions.LabelTarget @break, System.Linq.Expressions.LabelTarget @continue) { throw null; }
        public static System.Linq.Expressions.BinaryExpression MakeBinary(System.Linq.Expressions.ExpressionType binaryType, System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right) { throw null; }
        public static System.Linq.Expressions.BinaryExpression MakeBinary(System.Linq.Expressions.ExpressionType binaryType, System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right, bool liftToNull, System.Reflection.MethodInfo method) { throw null; }
        public static System.Linq.Expressions.BinaryExpression MakeBinary(System.Linq.Expressions.ExpressionType binaryType, System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right, bool liftToNull, System.Reflection.MethodInfo method, System.Linq.Expressions.LambdaExpression conversion) { throw null; }
        public static System.Linq.Expressions.CatchBlock MakeCatchBlock(System.Type type, System.Linq.Expressions.ParameterExpression variable, System.Linq.Expressions.Expression body, System.Linq.Expressions.Expression filter) { throw null; }
        public static System.Linq.Expressions.DynamicExpression MakeDynamic(System.Type delegateType, System.Runtime.CompilerServices.CallSiteBinder binder, System.Collections.Generic.IEnumerable<System.Linq.Expressions.Expression> arguments) { throw null; }
        public static System.Linq.Expressions.DynamicExpression MakeDynamic(System.Type delegateType, System.Runtime.CompilerServices.CallSiteBinder binder, System.Linq.Expressions.Expression arg0) { throw null; }
        public static System.Linq.Expressions.DynamicExpression MakeDynamic(System.Type delegateType, System.Runtime.CompilerServices.CallSiteBinder binder, System.Linq.Expressions.Expression arg0, System.Linq.Expressions.Expression arg1) { throw null; }
        public static System.Linq.Expressions.DynamicExpression MakeDynamic(System.Type delegateType, System.Runtime.CompilerServices.CallSiteBinder binder, System.Linq.Expressions.Expression arg0, System.Linq.Expressions.Expression arg1, System.Linq.Expressions.Expression arg2) { throw null; }
        public static System.Linq.Expressions.DynamicExpression MakeDynamic(System.Type delegateType, System.Runtime.CompilerServices.CallSiteBinder binder, System.Linq.Expressions.Expression arg0, System.Linq.Expressions.Expression arg1, System.Linq.Expressions.Expression arg2, System.Linq.Expressions.Expression arg3) { throw null; }
        public static System.Linq.Expressions.DynamicExpression MakeDynamic(System.Type delegateType, System.Runtime.CompilerServices.CallSiteBinder binder, params System.Linq.Expressions.Expression[] arguments) { throw null; }
        public static System.Linq.Expressions.GotoExpression MakeGoto(System.Linq.Expressions.GotoExpressionKind kind, System.Linq.Expressions.LabelTarget target, System.Linq.Expressions.Expression value, System.Type type) { throw null; }
        public static System.Linq.Expressions.IndexExpression MakeIndex(System.Linq.Expressions.Expression instance, System.Reflection.PropertyInfo indexer, System.Collections.Generic.IEnumerable<System.Linq.Expressions.Expression> arguments) { throw null; }
        public static System.Linq.Expressions.MemberExpression MakeMemberAccess(System.Linq.Expressions.Expression expression, System.Reflection.MemberInfo member) { throw null; }
        public static System.Linq.Expressions.TryExpression MakeTry(System.Type type, System.Linq.Expressions.Expression body, System.Linq.Expressions.Expression @finally, System.Linq.Expressions.Expression fault, System.Collections.Generic.IEnumerable<System.Linq.Expressions.CatchBlock> handlers) { throw null; }
        public static System.Linq.Expressions.UnaryExpression MakeUnary(System.Linq.Expressions.ExpressionType unaryType, System.Linq.Expressions.Expression operand, System.Type type) { throw null; }
        public static System.Linq.Expressions.UnaryExpression MakeUnary(System.Linq.Expressions.ExpressionType unaryType, System.Linq.Expressions.Expression operand, System.Type type, System.Reflection.MethodInfo method) { throw null; }
        public static System.Linq.Expressions.MemberMemberBinding MemberBind(System.Reflection.MemberInfo member, System.Collections.Generic.IEnumerable<System.Linq.Expressions.MemberBinding> bindings) { throw null; }
        public static System.Linq.Expressions.MemberMemberBinding MemberBind(System.Reflection.MemberInfo member, params System.Linq.Expressions.MemberBinding[] bindings) { throw null; }
        public static System.Linq.Expressions.MemberMemberBinding MemberBind(System.Reflection.MethodInfo propertyAccessor, System.Collections.Generic.IEnumerable<System.Linq.Expressions.MemberBinding> bindings) { throw null; }
        public static System.Linq.Expressions.MemberMemberBinding MemberBind(System.Reflection.MethodInfo propertyAccessor, params System.Linq.Expressions.MemberBinding[] bindings) { throw null; }
        public static System.Linq.Expressions.MemberInitExpression MemberInit(System.Linq.Expressions.NewExpression newExpression, System.Collections.Generic.IEnumerable<System.Linq.Expressions.MemberBinding> bindings) { throw null; }
        public static System.Linq.Expressions.MemberInitExpression MemberInit(System.Linq.Expressions.NewExpression newExpression, params System.Linq.Expressions.MemberBinding[] bindings) { throw null; }
        public static System.Linq.Expressions.BinaryExpression Modulo(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right) { throw null; }
        public static System.Linq.Expressions.BinaryExpression Modulo(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right, System.Reflection.MethodInfo method) { throw null; }
        public static System.Linq.Expressions.BinaryExpression ModuloAssign(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right) { throw null; }
        public static System.Linq.Expressions.BinaryExpression ModuloAssign(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right, System.Reflection.MethodInfo method) { throw null; }
        public static System.Linq.Expressions.BinaryExpression ModuloAssign(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right, System.Reflection.MethodInfo method, System.Linq.Expressions.LambdaExpression conversion) { throw null; }
        public static System.Linq.Expressions.BinaryExpression Multiply(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right) { throw null; }
        public static System.Linq.Expressions.BinaryExpression Multiply(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right, System.Reflection.MethodInfo method) { throw null; }
        public static System.Linq.Expressions.BinaryExpression MultiplyAssign(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right) { throw null; }
        public static System.Linq.Expressions.BinaryExpression MultiplyAssign(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right, System.Reflection.MethodInfo method) { throw null; }
        public static System.Linq.Expressions.BinaryExpression MultiplyAssign(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right, System.Reflection.MethodInfo method, System.Linq.Expressions.LambdaExpression conversion) { throw null; }
        public static System.Linq.Expressions.BinaryExpression MultiplyAssignChecked(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right) { throw null; }
        public static System.Linq.Expressions.BinaryExpression MultiplyAssignChecked(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right, System.Reflection.MethodInfo method) { throw null; }
        public static System.Linq.Expressions.BinaryExpression MultiplyAssignChecked(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right, System.Reflection.MethodInfo method, System.Linq.Expressions.LambdaExpression conversion) { throw null; }
        public static System.Linq.Expressions.BinaryExpression MultiplyChecked(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right) { throw null; }
        public static System.Linq.Expressions.BinaryExpression MultiplyChecked(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right, System.Reflection.MethodInfo method) { throw null; }
        public static System.Linq.Expressions.UnaryExpression Negate(System.Linq.Expressions.Expression expression) { throw null; }
        public static System.Linq.Expressions.UnaryExpression Negate(System.Linq.Expressions.Expression expression, System.Reflection.MethodInfo method) { throw null; }
        public static System.Linq.Expressions.UnaryExpression NegateChecked(System.Linq.Expressions.Expression expression) { throw null; }
        public static System.Linq.Expressions.UnaryExpression NegateChecked(System.Linq.Expressions.Expression expression, System.Reflection.MethodInfo method) { throw null; }
        public static System.Linq.Expressions.NewExpression New(System.Reflection.ConstructorInfo constructor) { throw null; }
        public static System.Linq.Expressions.NewExpression New(System.Reflection.ConstructorInfo constructor, System.Collections.Generic.IEnumerable<System.Linq.Expressions.Expression> arguments) { throw null; }
        public static System.Linq.Expressions.NewExpression New(System.Reflection.ConstructorInfo constructor, System.Collections.Generic.IEnumerable<System.Linq.Expressions.Expression> arguments, System.Collections.Generic.IEnumerable<System.Reflection.MemberInfo> members) { throw null; }
        public static System.Linq.Expressions.NewExpression New(System.Reflection.ConstructorInfo constructor, System.Collections.Generic.IEnumerable<System.Linq.Expressions.Expression> arguments, params System.Reflection.MemberInfo[] members) { throw null; }
        public static System.Linq.Expressions.NewExpression New(System.Reflection.ConstructorInfo constructor, params System.Linq.Expressions.Expression[] arguments) { throw null; }
        public static System.Linq.Expressions.NewExpression New(System.Type type) { throw null; }
        public static System.Linq.Expressions.NewArrayExpression NewArrayBounds(System.Type type, System.Collections.Generic.IEnumerable<System.Linq.Expressions.Expression> bounds) { throw null; }
        public static System.Linq.Expressions.NewArrayExpression NewArrayBounds(System.Type type, params System.Linq.Expressions.Expression[] bounds) { throw null; }
        public static System.Linq.Expressions.NewArrayExpression NewArrayInit(System.Type type, System.Collections.Generic.IEnumerable<System.Linq.Expressions.Expression> initializers) { throw null; }
        public static System.Linq.Expressions.NewArrayExpression NewArrayInit(System.Type type, params System.Linq.Expressions.Expression[] initializers) { throw null; }
        public static System.Linq.Expressions.UnaryExpression Not(System.Linq.Expressions.Expression expression) { throw null; }
        public static System.Linq.Expressions.UnaryExpression Not(System.Linq.Expressions.Expression expression, System.Reflection.MethodInfo method) { throw null; }
        public static System.Linq.Expressions.BinaryExpression NotEqual(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right) { throw null; }
        public static System.Linq.Expressions.BinaryExpression NotEqual(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right, bool liftToNull, System.Reflection.MethodInfo method) { throw null; }
        public static System.Linq.Expressions.UnaryExpression OnesComplement(System.Linq.Expressions.Expression expression) { throw null; }
        public static System.Linq.Expressions.UnaryExpression OnesComplement(System.Linq.Expressions.Expression expression, System.Reflection.MethodInfo method) { throw null; }
        public static System.Linq.Expressions.BinaryExpression Or(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right) { throw null; }
        public static System.Linq.Expressions.BinaryExpression Or(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right, System.Reflection.MethodInfo method) { throw null; }
        public static System.Linq.Expressions.BinaryExpression OrAssign(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right) { throw null; }
        public static System.Linq.Expressions.BinaryExpression OrAssign(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right, System.Reflection.MethodInfo method) { throw null; }
        public static System.Linq.Expressions.BinaryExpression OrAssign(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right, System.Reflection.MethodInfo method, System.Linq.Expressions.LambdaExpression conversion) { throw null; }
        public static System.Linq.Expressions.BinaryExpression OrElse(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right) { throw null; }
        public static System.Linq.Expressions.BinaryExpression OrElse(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right, System.Reflection.MethodInfo method) { throw null; }
        public static System.Linq.Expressions.ParameterExpression Parameter(System.Type type) { throw null; }
        public static System.Linq.Expressions.ParameterExpression Parameter(System.Type type, string name) { throw null; }
        public static System.Linq.Expressions.UnaryExpression PostDecrementAssign(System.Linq.Expressions.Expression expression) { throw null; }
        public static System.Linq.Expressions.UnaryExpression PostDecrementAssign(System.Linq.Expressions.Expression expression, System.Reflection.MethodInfo method) { throw null; }
        public static System.Linq.Expressions.UnaryExpression PostIncrementAssign(System.Linq.Expressions.Expression expression) { throw null; }
        public static System.Linq.Expressions.UnaryExpression PostIncrementAssign(System.Linq.Expressions.Expression expression, System.Reflection.MethodInfo method) { throw null; }
        public static System.Linq.Expressions.BinaryExpression Power(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right) { throw null; }
        public static System.Linq.Expressions.BinaryExpression Power(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right, System.Reflection.MethodInfo method) { throw null; }
        public static System.Linq.Expressions.BinaryExpression PowerAssign(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right) { throw null; }
        public static System.Linq.Expressions.BinaryExpression PowerAssign(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right, System.Reflection.MethodInfo method) { throw null; }
        public static System.Linq.Expressions.BinaryExpression PowerAssign(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right, System.Reflection.MethodInfo method, System.Linq.Expressions.LambdaExpression conversion) { throw null; }
        public static System.Linq.Expressions.UnaryExpression PreDecrementAssign(System.Linq.Expressions.Expression expression) { throw null; }
        public static System.Linq.Expressions.UnaryExpression PreDecrementAssign(System.Linq.Expressions.Expression expression, System.Reflection.MethodInfo method) { throw null; }
        public static System.Linq.Expressions.UnaryExpression PreIncrementAssign(System.Linq.Expressions.Expression expression) { throw null; }
        public static System.Linq.Expressions.UnaryExpression PreIncrementAssign(System.Linq.Expressions.Expression expression, System.Reflection.MethodInfo method) { throw null; }
        public static System.Linq.Expressions.MemberExpression Property(System.Linq.Expressions.Expression expression, System.Reflection.MethodInfo propertyAccessor) { throw null; }
        public static System.Linq.Expressions.MemberExpression Property(System.Linq.Expressions.Expression expression, System.Reflection.PropertyInfo property) { throw null; }
        public static System.Linq.Expressions.IndexExpression Property(System.Linq.Expressions.Expression instance, System.Reflection.PropertyInfo indexer, System.Collections.Generic.IEnumerable<System.Linq.Expressions.Expression> arguments) { throw null; }
        public static System.Linq.Expressions.IndexExpression Property(System.Linq.Expressions.Expression instance, System.Reflection.PropertyInfo indexer, params System.Linq.Expressions.Expression[] arguments) { throw null; }
        public static System.Linq.Expressions.MemberExpression Property(System.Linq.Expressions.Expression expression, string propertyName) { throw null; }
        public static System.Linq.Expressions.IndexExpression Property(System.Linq.Expressions.Expression instance, string propertyName, params System.Linq.Expressions.Expression[] arguments) { throw null; }
        public static System.Linq.Expressions.MemberExpression Property(System.Linq.Expressions.Expression expression, System.Type type, string propertyName) { throw null; }
        public static System.Linq.Expressions.MemberExpression PropertyOrField(System.Linq.Expressions.Expression expression, string propertyOrFieldName) { throw null; }
        public static System.Linq.Expressions.UnaryExpression Quote(System.Linq.Expressions.Expression expression) { throw null; }
        public virtual System.Linq.Expressions.Expression Reduce() { throw null; }
        public System.Linq.Expressions.Expression ReduceAndCheck() { throw null; }
        public System.Linq.Expressions.Expression ReduceExtensions() { throw null; }
        public static System.Linq.Expressions.BinaryExpression ReferenceEqual(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right) { throw null; }
        public static System.Linq.Expressions.BinaryExpression ReferenceNotEqual(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right) { throw null; }
        public static System.Linq.Expressions.UnaryExpression Rethrow() { throw null; }
        public static System.Linq.Expressions.UnaryExpression Rethrow(System.Type type) { throw null; }
        public static System.Linq.Expressions.GotoExpression Return(System.Linq.Expressions.LabelTarget target) { throw null; }
        public static System.Linq.Expressions.GotoExpression Return(System.Linq.Expressions.LabelTarget target, System.Linq.Expressions.Expression value) { throw null; }
        public static System.Linq.Expressions.GotoExpression Return(System.Linq.Expressions.LabelTarget target, System.Linq.Expressions.Expression value, System.Type type) { throw null; }
        public static System.Linq.Expressions.GotoExpression Return(System.Linq.Expressions.LabelTarget target, System.Type type) { throw null; }
        public static System.Linq.Expressions.BinaryExpression RightShift(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right) { throw null; }
        public static System.Linq.Expressions.BinaryExpression RightShift(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right, System.Reflection.MethodInfo method) { throw null; }
        public static System.Linq.Expressions.BinaryExpression RightShiftAssign(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right) { throw null; }
        public static System.Linq.Expressions.BinaryExpression RightShiftAssign(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right, System.Reflection.MethodInfo method) { throw null; }
        public static System.Linq.Expressions.BinaryExpression RightShiftAssign(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right, System.Reflection.MethodInfo method, System.Linq.Expressions.LambdaExpression conversion) { throw null; }
        public static System.Linq.Expressions.RuntimeVariablesExpression RuntimeVariables(System.Collections.Generic.IEnumerable<System.Linq.Expressions.ParameterExpression> variables) { throw null; }
        public static System.Linq.Expressions.RuntimeVariablesExpression RuntimeVariables(params System.Linq.Expressions.ParameterExpression[] variables) { throw null; }
        public static System.Linq.Expressions.BinaryExpression Subtract(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right) { throw null; }
        public static System.Linq.Expressions.BinaryExpression Subtract(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right, System.Reflection.MethodInfo method) { throw null; }
        public static System.Linq.Expressions.BinaryExpression SubtractAssign(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right) { throw null; }
        public static System.Linq.Expressions.BinaryExpression SubtractAssign(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right, System.Reflection.MethodInfo method) { throw null; }
        public static System.Linq.Expressions.BinaryExpression SubtractAssign(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right, System.Reflection.MethodInfo method, System.Linq.Expressions.LambdaExpression conversion) { throw null; }
        public static System.Linq.Expressions.BinaryExpression SubtractAssignChecked(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right) { throw null; }
        public static System.Linq.Expressions.BinaryExpression SubtractAssignChecked(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right, System.Reflection.MethodInfo method) { throw null; }
        public static System.Linq.Expressions.BinaryExpression SubtractAssignChecked(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right, System.Reflection.MethodInfo method, System.Linq.Expressions.LambdaExpression conversion) { throw null; }
        public static System.Linq.Expressions.BinaryExpression SubtractChecked(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right) { throw null; }
        public static System.Linq.Expressions.BinaryExpression SubtractChecked(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right, System.Reflection.MethodInfo method) { throw null; }
        public static System.Linq.Expressions.SwitchExpression Switch(System.Linq.Expressions.Expression switchValue, System.Linq.Expressions.Expression defaultBody, params System.Linq.Expressions.SwitchCase[] cases) { throw null; }
        public static System.Linq.Expressions.SwitchExpression Switch(System.Linq.Expressions.Expression switchValue, System.Linq.Expressions.Expression defaultBody, System.Reflection.MethodInfo comparison, System.Collections.Generic.IEnumerable<System.Linq.Expressions.SwitchCase> cases) { throw null; }
        public static System.Linq.Expressions.SwitchExpression Switch(System.Linq.Expressions.Expression switchValue, System.Linq.Expressions.Expression defaultBody, System.Reflection.MethodInfo comparison, params System.Linq.Expressions.SwitchCase[] cases) { throw null; }
        public static System.Linq.Expressions.SwitchExpression Switch(System.Linq.Expressions.Expression switchValue, params System.Linq.Expressions.SwitchCase[] cases) { throw null; }
        public static System.Linq.Expressions.SwitchExpression Switch(System.Type type, System.Linq.Expressions.Expression switchValue, System.Linq.Expressions.Expression defaultBody, System.Reflection.MethodInfo comparison, System.Collections.Generic.IEnumerable<System.Linq.Expressions.SwitchCase> cases) { throw null; }
        public static System.Linq.Expressions.SwitchExpression Switch(System.Type type, System.Linq.Expressions.Expression switchValue, System.Linq.Expressions.Expression defaultBody, System.Reflection.MethodInfo comparison, params System.Linq.Expressions.SwitchCase[] cases) { throw null; }
        public static System.Linq.Expressions.SwitchCase SwitchCase(System.Linq.Expressions.Expression body, System.Collections.Generic.IEnumerable<System.Linq.Expressions.Expression> testValues) { throw null; }
        public static System.Linq.Expressions.SwitchCase SwitchCase(System.Linq.Expressions.Expression body, params System.Linq.Expressions.Expression[] testValues) { throw null; }
        public static System.Linq.Expressions.SymbolDocumentInfo SymbolDocument(string fileName) { throw null; }
        public static System.Linq.Expressions.SymbolDocumentInfo SymbolDocument(string fileName, System.Guid language) { throw null; }
        public static System.Linq.Expressions.SymbolDocumentInfo SymbolDocument(string fileName, System.Guid language, System.Guid languageVendor) { throw null; }
        public static System.Linq.Expressions.SymbolDocumentInfo SymbolDocument(string fileName, System.Guid language, System.Guid languageVendor, System.Guid documentType) { throw null; }
        public static System.Linq.Expressions.UnaryExpression Throw(System.Linq.Expressions.Expression value) { throw null; }
        public static System.Linq.Expressions.UnaryExpression Throw(System.Linq.Expressions.Expression value, System.Type type) { throw null; }
        public override string ToString() { throw null; }
        public static System.Linq.Expressions.TryExpression TryCatch(System.Linq.Expressions.Expression body, params System.Linq.Expressions.CatchBlock[] handlers) { throw null; }
        public static System.Linq.Expressions.TryExpression TryCatchFinally(System.Linq.Expressions.Expression body, System.Linq.Expressions.Expression @finally, params System.Linq.Expressions.CatchBlock[] handlers) { throw null; }
        public static System.Linq.Expressions.TryExpression TryFault(System.Linq.Expressions.Expression body, System.Linq.Expressions.Expression fault) { throw null; }
        public static System.Linq.Expressions.TryExpression TryFinally(System.Linq.Expressions.Expression body, System.Linq.Expressions.Expression @finally) { throw null; }
        public static bool TryGetActionType(System.Type[] typeArgs, out System.Type actionType) { throw null; }
        public static bool TryGetFuncType(System.Type[] typeArgs, out System.Type funcType) { throw null; }
        public static System.Linq.Expressions.UnaryExpression TypeAs(System.Linq.Expressions.Expression expression, System.Type type) { throw null; }
        public static System.Linq.Expressions.TypeBinaryExpression TypeEqual(System.Linq.Expressions.Expression expression, System.Type type) { throw null; }
        public static System.Linq.Expressions.TypeBinaryExpression TypeIs(System.Linq.Expressions.Expression expression, System.Type type) { throw null; }
        public static System.Linq.Expressions.UnaryExpression UnaryPlus(System.Linq.Expressions.Expression expression) { throw null; }
        public static System.Linq.Expressions.UnaryExpression UnaryPlus(System.Linq.Expressions.Expression expression, System.Reflection.MethodInfo method) { throw null; }
        public static System.Linq.Expressions.UnaryExpression Unbox(System.Linq.Expressions.Expression expression, System.Type type) { throw null; }
        public static System.Linq.Expressions.ParameterExpression Variable(System.Type type) { throw null; }
        public static System.Linq.Expressions.ParameterExpression Variable(System.Type type, string name) { throw null; }
        protected internal virtual System.Linq.Expressions.Expression VisitChildren(System.Linq.Expressions.ExpressionVisitor visitor) { throw null; }
    }
    public sealed partial class Expression<TDelegate> : System.Linq.Expressions.LambdaExpression
    {
        internal Expression() { }
        protected internal override System.Linq.Expressions.Expression Accept(System.Linq.Expressions.ExpressionVisitor visitor) { throw null; }
        public new TDelegate Compile() { throw null; }
        public new TDelegate Compile(bool preferInterpretation) { throw null; }
        public new TDelegate Compile(System.Runtime.CompilerServices.DebugInfoGenerator debugInfoGenerator) { throw null; }
        public System.Linq.Expressions.Expression<TDelegate> Update(System.Linq.Expressions.Expression body, System.Collections.Generic.IEnumerable<System.Linq.Expressions.ParameterExpression> parameters) { throw null; }
    }
    public enum ExpressionType
    {
        Add = 0,
        AddAssign = 63,
        AddAssignChecked = 74,
        AddChecked = 1,
        And = 2,
        AndAlso = 3,
        AndAssign = 64,
        ArrayIndex = 5,
        ArrayLength = 4,
        Assign = 46,
        Block = 47,
        Call = 6,
        Coalesce = 7,
        Conditional = 8,
        Constant = 9,
        Convert = 10,
        ConvertChecked = 11,
        DebugInfo = 48,
        Decrement = 49,
        Default = 51,
        Divide = 12,
        DivideAssign = 65,
        Dynamic = 50,
        Equal = 13,
        ExclusiveOr = 14,
        ExclusiveOrAssign = 66,
        Extension = 52,
        Goto = 53,
        GreaterThan = 15,
        GreaterThanOrEqual = 16,
        Increment = 54,
        Index = 55,
        Invoke = 17,
        IsFalse = 84,
        IsTrue = 83,
        Label = 56,
        Lambda = 18,
        LeftShift = 19,
        LeftShiftAssign = 67,
        LessThan = 20,
        LessThanOrEqual = 21,
        ListInit = 22,
        Loop = 58,
        MemberAccess = 23,
        MemberInit = 24,
        Modulo = 25,
        ModuloAssign = 68,
        Multiply = 26,
        MultiplyAssign = 69,
        MultiplyAssignChecked = 75,
        MultiplyChecked = 27,
        Negate = 28,
        NegateChecked = 30,
        New = 31,
        NewArrayBounds = 33,
        NewArrayInit = 32,
        Not = 34,
        NotEqual = 35,
        OnesComplement = 82,
        Or = 36,
        OrAssign = 70,
        OrElse = 37,
        Parameter = 38,
        PostDecrementAssign = 80,
        PostIncrementAssign = 79,
        Power = 39,
        PowerAssign = 71,
        PreDecrementAssign = 78,
        PreIncrementAssign = 77,
        Quote = 40,
        RightShift = 41,
        RightShiftAssign = 72,
        RuntimeVariables = 57,
        Subtract = 42,
        SubtractAssign = 73,
        SubtractAssignChecked = 76,
        SubtractChecked = 43,
        Switch = 59,
        Throw = 60,
        Try = 61,
        TypeAs = 44,
        TypeEqual = 81,
        TypeIs = 45,
        UnaryPlus = 29,
        Unbox = 62,
    }
    public abstract partial class ExpressionVisitor
    {
        protected ExpressionVisitor() { }
        public System.Collections.ObjectModel.ReadOnlyCollection<System.Linq.Expressions.Expression> Visit(System.Collections.ObjectModel.ReadOnlyCollection<System.Linq.Expressions.Expression> nodes) { throw null; }
        public virtual System.Linq.Expressions.Expression Visit(System.Linq.Expressions.Expression node) { throw null; }
        public static System.Collections.ObjectModel.ReadOnlyCollection<T> Visit<T>(System.Collections.ObjectModel.ReadOnlyCollection<T> nodes, System.Func<T, T> elementVisitor) { throw null; }
        public T VisitAndConvert<T>(T node, string callerName) where T : System.Linq.Expressions.Expression { throw null; }
        public System.Collections.ObjectModel.ReadOnlyCollection<T> VisitAndConvert<T>(System.Collections.ObjectModel.ReadOnlyCollection<T> nodes, string callerName) where T : System.Linq.Expressions.Expression { throw null; }
        protected internal virtual System.Linq.Expressions.Expression VisitBinary(System.Linq.Expressions.BinaryExpression node) { throw null; }
        protected internal virtual System.Linq.Expressions.Expression VisitBlock(System.Linq.Expressions.BlockExpression node) { throw null; }
        protected virtual System.Linq.Expressions.CatchBlock VisitCatchBlock(System.Linq.Expressions.CatchBlock node) { throw null; }
        protected internal virtual System.Linq.Expressions.Expression VisitConditional(System.Linq.Expressions.ConditionalExpression node) { throw null; }
        protected internal virtual System.Linq.Expressions.Expression VisitConstant(System.Linq.Expressions.ConstantExpression node) { throw null; }
        protected internal virtual System.Linq.Expressions.Expression VisitDebugInfo(System.Linq.Expressions.DebugInfoExpression node) { throw null; }
        protected internal virtual System.Linq.Expressions.Expression VisitDefault(System.Linq.Expressions.DefaultExpression node) { throw null; }
        protected internal virtual System.Linq.Expressions.Expression VisitDynamic(System.Linq.Expressions.DynamicExpression node) { throw null; }
        protected virtual System.Linq.Expressions.ElementInit VisitElementInit(System.Linq.Expressions.ElementInit node) { throw null; }
        protected internal virtual System.Linq.Expressions.Expression VisitExtension(System.Linq.Expressions.Expression node) { throw null; }
        protected internal virtual System.Linq.Expressions.Expression VisitGoto(System.Linq.Expressions.GotoExpression node) { throw null; }
        protected internal virtual System.Linq.Expressions.Expression VisitIndex(System.Linq.Expressions.IndexExpression node) { throw null; }
        protected internal virtual System.Linq.Expressions.Expression VisitInvocation(System.Linq.Expressions.InvocationExpression node) { throw null; }
        protected internal virtual System.Linq.Expressions.Expression VisitLabel(System.Linq.Expressions.LabelExpression node) { throw null; }
        protected virtual System.Linq.Expressions.LabelTarget VisitLabelTarget(System.Linq.Expressions.LabelTarget node) { throw null; }
        protected internal virtual System.Linq.Expressions.Expression VisitLambda<T>(System.Linq.Expressions.Expression<T> node) { throw null; }
        protected internal virtual System.Linq.Expressions.Expression VisitListInit(System.Linq.Expressions.ListInitExpression node) { throw null; }
        protected internal virtual System.Linq.Expressions.Expression VisitLoop(System.Linq.Expressions.LoopExpression node) { throw null; }
        protected internal virtual System.Linq.Expressions.Expression VisitMember(System.Linq.Expressions.MemberExpression node) { throw null; }
        protected virtual System.Linq.Expressions.MemberAssignment VisitMemberAssignment(System.Linq.Expressions.MemberAssignment node) { throw null; }
        protected virtual System.Linq.Expressions.MemberBinding VisitMemberBinding(System.Linq.Expressions.MemberBinding node) { throw null; }
        protected internal virtual System.Linq.Expressions.Expression VisitMemberInit(System.Linq.Expressions.MemberInitExpression node) { throw null; }
        protected virtual System.Linq.Expressions.MemberListBinding VisitMemberListBinding(System.Linq.Expressions.MemberListBinding node) { throw null; }
        protected virtual System.Linq.Expressions.MemberMemberBinding VisitMemberMemberBinding(System.Linq.Expressions.MemberMemberBinding node) { throw null; }
        protected internal virtual System.Linq.Expressions.Expression VisitMethodCall(System.Linq.Expressions.MethodCallExpression node) { throw null; }
        protected internal virtual System.Linq.Expressions.Expression VisitNew(System.Linq.Expressions.NewExpression node) { throw null; }
        protected internal virtual System.Linq.Expressions.Expression VisitNewArray(System.Linq.Expressions.NewArrayExpression node) { throw null; }
        protected internal virtual System.Linq.Expressions.Expression VisitParameter(System.Linq.Expressions.ParameterExpression node) { throw null; }
        protected internal virtual System.Linq.Expressions.Expression VisitRuntimeVariables(System.Linq.Expressions.RuntimeVariablesExpression node) { throw null; }
        protected internal virtual System.Linq.Expressions.Expression VisitSwitch(System.Linq.Expressions.SwitchExpression node) { throw null; }
        protected virtual System.Linq.Expressions.SwitchCase VisitSwitchCase(System.Linq.Expressions.SwitchCase node) { throw null; }
        protected internal virtual System.Linq.Expressions.Expression VisitTry(System.Linq.Expressions.TryExpression node) { throw null; }
        protected internal virtual System.Linq.Expressions.Expression VisitTypeBinary(System.Linq.Expressions.TypeBinaryExpression node) { throw null; }
        protected internal virtual System.Linq.Expressions.Expression VisitUnary(System.Linq.Expressions.UnaryExpression node) { throw null; }
    }
    public sealed partial class GotoExpression : System.Linq.Expressions.Expression
    {
        internal GotoExpression() { }
        public System.Linq.Expressions.GotoExpressionKind Kind { get { throw null; } }
        public sealed override System.Linq.Expressions.ExpressionType NodeType { get { throw null; } }
        public System.Linq.Expressions.LabelTarget Target { get { throw null; } }
        public sealed override System.Type Type { get { throw null; } }
        public System.Linq.Expressions.Expression Value { get { throw null; } }
        protected internal override System.Linq.Expressions.Expression Accept(System.Linq.Expressions.ExpressionVisitor visitor) { throw null; }
        public System.Linq.Expressions.GotoExpression Update(System.Linq.Expressions.LabelTarget target, System.Linq.Expressions.Expression value) { throw null; }
    }
    public enum GotoExpressionKind
    {
        Break = 2,
        Continue = 3,
        Goto = 0,
        Return = 1,
    }
    public partial interface IArgumentProvider
    {
        int ArgumentCount { get; }
        System.Linq.Expressions.Expression GetArgument(int index);
    }
    public partial interface IDynamicExpression : System.Linq.Expressions.IArgumentProvider
    {
        System.Type DelegateType { get; }
        object CreateCallSite();
        System.Linq.Expressions.Expression Rewrite(System.Linq.Expressions.Expression[] args);
    }
    public sealed partial class IndexExpression : System.Linq.Expressions.Expression, System.Linq.Expressions.IArgumentProvider
    {
        internal IndexExpression() { }
        public System.Collections.ObjectModel.ReadOnlyCollection<System.Linq.Expressions.Expression> Arguments { get { throw null; } }
        public System.Reflection.PropertyInfo Indexer { get { throw null; } }
        public sealed override System.Linq.Expressions.ExpressionType NodeType { get { throw null; } }
        public System.Linq.Expressions.Expression Object { get { throw null; } }
        int System.Linq.Expressions.IArgumentProvider.ArgumentCount { get { throw null; } }
        public sealed override System.Type Type { get { throw null; } }
        protected internal override System.Linq.Expressions.Expression Accept(System.Linq.Expressions.ExpressionVisitor visitor) { throw null; }
        System.Linq.Expressions.Expression System.Linq.Expressions.IArgumentProvider.GetArgument(int index) { throw null; }
        public System.Linq.Expressions.IndexExpression Update(System.Linq.Expressions.Expression @object, System.Collections.Generic.IEnumerable<System.Linq.Expressions.Expression> arguments) { throw null; }
    }
    public sealed partial class InvocationExpression : System.Linq.Expressions.Expression, System.Linq.Expressions.IArgumentProvider
    {
        internal InvocationExpression() { }
        public System.Collections.ObjectModel.ReadOnlyCollection<System.Linq.Expressions.Expression> Arguments { get { throw null; } }
        public System.Linq.Expressions.Expression Expression { get { throw null; } }
        public sealed override System.Linq.Expressions.ExpressionType NodeType { get { throw null; } }
        int System.Linq.Expressions.IArgumentProvider.ArgumentCount { get { throw null; } }
        public sealed override System.Type Type { get { throw null; } }
        protected internal override System.Linq.Expressions.Expression Accept(System.Linq.Expressions.ExpressionVisitor visitor) { throw null; }
        System.Linq.Expressions.Expression System.Linq.Expressions.IArgumentProvider.GetArgument(int index) { throw null; }
        public System.Linq.Expressions.InvocationExpression Update(System.Linq.Expressions.Expression expression, System.Collections.Generic.IEnumerable<System.Linq.Expressions.Expression> arguments) { throw null; }
    }
    public sealed partial class LabelExpression : System.Linq.Expressions.Expression
    {
        internal LabelExpression() { }
        public System.Linq.Expressions.Expression DefaultValue { get { throw null; } }
        public sealed override System.Linq.Expressions.ExpressionType NodeType { get { throw null; } }
        public System.Linq.Expressions.LabelTarget Target { get { throw null; } }
        public sealed override System.Type Type { get { throw null; } }
        protected internal override System.Linq.Expressions.Expression Accept(System.Linq.Expressions.ExpressionVisitor visitor) { throw null; }
        public System.Linq.Expressions.LabelExpression Update(System.Linq.Expressions.LabelTarget target, System.Linq.Expressions.Expression defaultValue) { throw null; }
    }
    public sealed partial class LabelTarget
    {
        internal LabelTarget() { }
        public string Name { get { throw null; } }
        public System.Type Type { get { throw null; } }
        public override string ToString() { throw null; }
    }
    public abstract partial class LambdaExpression : System.Linq.Expressions.Expression
    {
        internal LambdaExpression() { }
        public System.Linq.Expressions.Expression Body { get { throw null; } }
        public string Name { get { throw null; } }
        public sealed override System.Linq.Expressions.ExpressionType NodeType { get { throw null; } }
        public System.Collections.ObjectModel.ReadOnlyCollection<System.Linq.Expressions.ParameterExpression> Parameters { get { throw null; } }
        public System.Type ReturnType { get { throw null; } }
        public bool TailCall { get { throw null; } }
        public sealed override System.Type Type { get { throw null; } }
        public System.Delegate Compile() { throw null; }
        public System.Delegate Compile(bool preferInterpretation) { throw null; }
        public System.Delegate Compile(System.Runtime.CompilerServices.DebugInfoGenerator debugInfoGenerator) { throw null; }
        public void CompileToMethod(System.Reflection.Emit.MethodBuilder method) { }
        public void CompileToMethod(System.Reflection.Emit.MethodBuilder method, System.Runtime.CompilerServices.DebugInfoGenerator debugInfoGenerator) { }
    }
    public sealed partial class ListInitExpression : System.Linq.Expressions.Expression
    {
        internal ListInitExpression() { }
        public override bool CanReduce { get { throw null; } }
        public System.Collections.ObjectModel.ReadOnlyCollection<System.Linq.Expressions.ElementInit> Initializers { get { throw null; } }
        public System.Linq.Expressions.NewExpression NewExpression { get { throw null; } }
        public sealed override System.Linq.Expressions.ExpressionType NodeType { get { throw null; } }
        public sealed override System.Type Type { get { throw null; } }
        protected internal override System.Linq.Expressions.Expression Accept(System.Linq.Expressions.ExpressionVisitor visitor) { throw null; }
        public override System.Linq.Expressions.Expression Reduce() { throw null; }
        public System.Linq.Expressions.ListInitExpression Update(System.Linq.Expressions.NewExpression newExpression, System.Collections.Generic.IEnumerable<System.Linq.Expressions.ElementInit> initializers) { throw null; }
    }
    public sealed partial class LoopExpression : System.Linq.Expressions.Expression
    {
        internal LoopExpression() { }
        public System.Linq.Expressions.Expression Body { get { throw null; } }
        public System.Linq.Expressions.LabelTarget BreakLabel { get { throw null; } }
        public System.Linq.Expressions.LabelTarget ContinueLabel { get { throw null; } }
        public sealed override System.Linq.Expressions.ExpressionType NodeType { get { throw null; } }
        public sealed override System.Type Type { get { throw null; } }
        protected internal override System.Linq.Expressions.Expression Accept(System.Linq.Expressions.ExpressionVisitor visitor) { throw null; }
        public System.Linq.Expressions.LoopExpression Update(System.Linq.Expressions.LabelTarget breakLabel, System.Linq.Expressions.LabelTarget continueLabel, System.Linq.Expressions.Expression body) { throw null; }
    }
    public sealed partial class MemberAssignment : System.Linq.Expressions.MemberBinding
    {
        internal MemberAssignment() : base (default(System.Linq.Expressions.MemberBindingType), default(System.Reflection.MemberInfo)) { }
        public System.Linq.Expressions.Expression Expression { get { throw null; } }
        public System.Linq.Expressions.MemberAssignment Update(System.Linq.Expressions.Expression expression) { throw null; }
    }
    public abstract partial class MemberBinding
    {
        protected MemberBinding(System.Linq.Expressions.MemberBindingType type, System.Reflection.MemberInfo member) { }
        public System.Linq.Expressions.MemberBindingType BindingType { get { throw null; } }
        public System.Reflection.MemberInfo Member { get { throw null; } }
        public override string ToString() { throw null; }
    }
    public enum MemberBindingType
    {
        Assignment = 0,
        ListBinding = 2,
        MemberBinding = 1,
    }
    public partial class MemberExpression : System.Linq.Expressions.Expression
    {
        internal MemberExpression() { }
        public System.Linq.Expressions.Expression Expression { get { throw null; } }
        public System.Reflection.MemberInfo Member { get { throw null; } }
        public sealed override System.Linq.Expressions.ExpressionType NodeType { get { throw null; } }
        protected internal override System.Linq.Expressions.Expression Accept(System.Linq.Expressions.ExpressionVisitor visitor) { throw null; }
        public System.Linq.Expressions.MemberExpression Update(System.Linq.Expressions.Expression expression) { throw null; }
    }
    public sealed partial class MemberInitExpression : System.Linq.Expressions.Expression
    {
        internal MemberInitExpression() { }
        public System.Collections.ObjectModel.ReadOnlyCollection<System.Linq.Expressions.MemberBinding> Bindings { get { throw null; } }
        public override bool CanReduce { get { throw null; } }
        public System.Linq.Expressions.NewExpression NewExpression { get { throw null; } }
        public sealed override System.Linq.Expressions.ExpressionType NodeType { get { throw null; } }
        public sealed override System.Type Type { get { throw null; } }
        protected internal override System.Linq.Expressions.Expression Accept(System.Linq.Expressions.ExpressionVisitor visitor) { throw null; }
        public override System.Linq.Expressions.Expression Reduce() { throw null; }
        public System.Linq.Expressions.MemberInitExpression Update(System.Linq.Expressions.NewExpression newExpression, System.Collections.Generic.IEnumerable<System.Linq.Expressions.MemberBinding> bindings) { throw null; }
    }
    public sealed partial class MemberListBinding : System.Linq.Expressions.MemberBinding
    {
        internal MemberListBinding() : base (default(System.Linq.Expressions.MemberBindingType), default(System.Reflection.MemberInfo)) { }
        public System.Collections.ObjectModel.ReadOnlyCollection<System.Linq.Expressions.ElementInit> Initializers { get { throw null; } }
        public System.Linq.Expressions.MemberListBinding Update(System.Collections.Generic.IEnumerable<System.Linq.Expressions.ElementInit> initializers) { throw null; }
    }
    public sealed partial class MemberMemberBinding : System.Linq.Expressions.MemberBinding
    {
        internal MemberMemberBinding() : base (default(System.Linq.Expressions.MemberBindingType), default(System.Reflection.MemberInfo)) { }
        public System.Collections.ObjectModel.ReadOnlyCollection<System.Linq.Expressions.MemberBinding> Bindings { get { throw null; } }
        public System.Linq.Expressions.MemberMemberBinding Update(System.Collections.Generic.IEnumerable<System.Linq.Expressions.MemberBinding> bindings) { throw null; }
    }
    public partial class MethodCallExpression : System.Linq.Expressions.Expression, System.Linq.Expressions.IArgumentProvider
    {
        internal MethodCallExpression() { }
        public System.Collections.ObjectModel.ReadOnlyCollection<System.Linq.Expressions.Expression> Arguments { get { throw null; } }
        public System.Reflection.MethodInfo Method { get { throw null; } }
        public sealed override System.Linq.Expressions.ExpressionType NodeType { get { throw null; } }
        public System.Linq.Expressions.Expression Object { get { throw null; } }
        int System.Linq.Expressions.IArgumentProvider.ArgumentCount { get { throw null; } }
        public sealed override System.Type Type { get { throw null; } }
        protected internal override System.Linq.Expressions.Expression Accept(System.Linq.Expressions.ExpressionVisitor visitor) { throw null; }
        System.Linq.Expressions.Expression System.Linq.Expressions.IArgumentProvider.GetArgument(int index) { throw null; }
        public System.Linq.Expressions.MethodCallExpression Update(System.Linq.Expressions.Expression @object, System.Collections.Generic.IEnumerable<System.Linq.Expressions.Expression> arguments) { throw null; }
    }
    public partial class NewArrayExpression : System.Linq.Expressions.Expression
    {
        internal NewArrayExpression() { }
        public System.Collections.ObjectModel.ReadOnlyCollection<System.Linq.Expressions.Expression> Expressions { get { throw null; } }
        public sealed override System.Type Type { get { throw null; } }
        protected internal override System.Linq.Expressions.Expression Accept(System.Linq.Expressions.ExpressionVisitor visitor) { throw null; }
        public System.Linq.Expressions.NewArrayExpression Update(System.Collections.Generic.IEnumerable<System.Linq.Expressions.Expression> expressions) { throw null; }
    }
    public partial class NewExpression : System.Linq.Expressions.Expression, System.Linq.Expressions.IArgumentProvider
    {
        internal NewExpression() { }
        public System.Collections.ObjectModel.ReadOnlyCollection<System.Linq.Expressions.Expression> Arguments { get { throw null; } }
        public System.Reflection.ConstructorInfo Constructor { get { throw null; } }
        public System.Collections.ObjectModel.ReadOnlyCollection<System.Reflection.MemberInfo> Members { get { throw null; } }
        public sealed override System.Linq.Expressions.ExpressionType NodeType { get { throw null; } }
        int System.Linq.Expressions.IArgumentProvider.ArgumentCount { get { throw null; } }
        public override System.Type Type { get { throw null; } }
        protected internal override System.Linq.Expressions.Expression Accept(System.Linq.Expressions.ExpressionVisitor visitor) { throw null; }
        System.Linq.Expressions.Expression System.Linq.Expressions.IArgumentProvider.GetArgument(int index) { throw null; }
        public System.Linq.Expressions.NewExpression Update(System.Collections.Generic.IEnumerable<System.Linq.Expressions.Expression> arguments) { throw null; }
    }
    public partial class ParameterExpression : System.Linq.Expressions.Expression
    {
        internal ParameterExpression() { }
        public bool IsByRef { get { throw null; } }
        public string Name { get { throw null; } }
        public sealed override System.Linq.Expressions.ExpressionType NodeType { get { throw null; } }
        public override System.Type Type { get { throw null; } }
        protected internal override System.Linq.Expressions.Expression Accept(System.Linq.Expressions.ExpressionVisitor visitor) { throw null; }
    }
    public sealed partial class RuntimeVariablesExpression : System.Linq.Expressions.Expression
    {
        internal RuntimeVariablesExpression() { }
        public sealed override System.Linq.Expressions.ExpressionType NodeType { get { throw null; } }
        public sealed override System.Type Type { get { throw null; } }
        public System.Collections.ObjectModel.ReadOnlyCollection<System.Linq.Expressions.ParameterExpression> Variables { get { throw null; } }
        protected internal override System.Linq.Expressions.Expression Accept(System.Linq.Expressions.ExpressionVisitor visitor) { throw null; }
        public System.Linq.Expressions.RuntimeVariablesExpression Update(System.Collections.Generic.IEnumerable<System.Linq.Expressions.ParameterExpression> variables) { throw null; }
    }
    public sealed partial class SwitchCase
    {
        internal SwitchCase() { }
        public System.Linq.Expressions.Expression Body { get { throw null; } }
        public System.Collections.ObjectModel.ReadOnlyCollection<System.Linq.Expressions.Expression> TestValues { get { throw null; } }
        public override string ToString() { throw null; }
        public System.Linq.Expressions.SwitchCase Update(System.Collections.Generic.IEnumerable<System.Linq.Expressions.Expression> testValues, System.Linq.Expressions.Expression body) { throw null; }
    }
    public sealed partial class SwitchExpression : System.Linq.Expressions.Expression
    {
        internal SwitchExpression() { }
        public System.Collections.ObjectModel.ReadOnlyCollection<System.Linq.Expressions.SwitchCase> Cases { get { throw null; } }
        public System.Reflection.MethodInfo Comparison { get { throw null; } }
        public System.Linq.Expressions.Expression DefaultBody { get { throw null; } }
        public sealed override System.Linq.Expressions.ExpressionType NodeType { get { throw null; } }
        public System.Linq.Expressions.Expression SwitchValue { get { throw null; } }
        public sealed override System.Type Type { get { throw null; } }
        protected internal override System.Linq.Expressions.Expression Accept(System.Linq.Expressions.ExpressionVisitor visitor) { throw null; }
        public System.Linq.Expressions.SwitchExpression Update(System.Linq.Expressions.Expression switchValue, System.Collections.Generic.IEnumerable<System.Linq.Expressions.SwitchCase> cases, System.Linq.Expressions.Expression defaultBody) { throw null; }
    }
    public partial class SymbolDocumentInfo
    {
        internal SymbolDocumentInfo() { }
        public virtual System.Guid DocumentType { get { throw null; } }
        public string FileName { get { throw null; } }
        public virtual System.Guid Language { get { throw null; } }
        public virtual System.Guid LanguageVendor { get { throw null; } }
    }
    public sealed partial class TryExpression : System.Linq.Expressions.Expression
    {
        internal TryExpression() { }
        public System.Linq.Expressions.Expression Body { get { throw null; } }
        public System.Linq.Expressions.Expression Fault { get { throw null; } }
        public System.Linq.Expressions.Expression Finally { get { throw null; } }
        public System.Collections.ObjectModel.ReadOnlyCollection<System.Linq.Expressions.CatchBlock> Handlers { get { throw null; } }
        public sealed override System.Linq.Expressions.ExpressionType NodeType { get { throw null; } }
        public sealed override System.Type Type { get { throw null; } }
        protected internal override System.Linq.Expressions.Expression Accept(System.Linq.Expressions.ExpressionVisitor visitor) { throw null; }
        public System.Linq.Expressions.TryExpression Update(System.Linq.Expressions.Expression body, System.Collections.Generic.IEnumerable<System.Linq.Expressions.CatchBlock> handlers, System.Linq.Expressions.Expression @finally, System.Linq.Expressions.Expression fault) { throw null; }
    }
    public sealed partial class TypeBinaryExpression : System.Linq.Expressions.Expression
    {
        internal TypeBinaryExpression() { }
        public System.Linq.Expressions.Expression Expression { get { throw null; } }
        public sealed override System.Linq.Expressions.ExpressionType NodeType { get { throw null; } }
        public sealed override System.Type Type { get { throw null; } }
        public System.Type TypeOperand { get { throw null; } }
        protected internal override System.Linq.Expressions.Expression Accept(System.Linq.Expressions.ExpressionVisitor visitor) { throw null; }
        public System.Linq.Expressions.TypeBinaryExpression Update(System.Linq.Expressions.Expression expression) { throw null; }
    }
    public sealed partial class UnaryExpression : System.Linq.Expressions.Expression
    {
        internal UnaryExpression() { }
        public override bool CanReduce { get { throw null; } }
        public bool IsLifted { get { throw null; } }
        public bool IsLiftedToNull { get { throw null; } }
        public System.Reflection.MethodInfo Method { get { throw null; } }
        public sealed override System.Linq.Expressions.ExpressionType NodeType { get { throw null; } }
        public System.Linq.Expressions.Expression Operand { get { throw null; } }
        public sealed override System.Type Type { get { throw null; } }
        protected internal override System.Linq.Expressions.Expression Accept(System.Linq.Expressions.ExpressionVisitor visitor) { throw null; }
        public override System.Linq.Expressions.Expression Reduce() { throw null; }
        public System.Linq.Expressions.UnaryExpression Update(System.Linq.Expressions.Expression operand) { throw null; }
    }
}
namespace System.Runtime.CompilerServices
{
    public partial class CallSite
    {
        internal CallSite() { }
        public System.Runtime.CompilerServices.CallSiteBinder Binder { get { throw null; } }
        public static System.Runtime.CompilerServices.CallSite Create(System.Type delegateType, System.Runtime.CompilerServices.CallSiteBinder binder) { throw null; }
    }
    public partial class CallSite<T> : System.Runtime.CompilerServices.CallSite where T : class
    {
        internal CallSite() { }
        public T Target;
        public T Update { get { throw null; } }
        public static System.Runtime.CompilerServices.CallSite<T> Create(System.Runtime.CompilerServices.CallSiteBinder binder) { throw null; }
    }
    public abstract partial class CallSiteBinder
    {
        protected CallSiteBinder() { }
        public static System.Linq.Expressions.LabelTarget UpdateLabel { get { throw null; } }
        public abstract System.Linq.Expressions.Expression Bind(object[] args, System.Collections.ObjectModel.ReadOnlyCollection<System.Linq.Expressions.ParameterExpression> parameters, System.Linq.Expressions.LabelTarget returnLabel);
        public virtual T BindDelegate<T>(System.Runtime.CompilerServices.CallSite<T> site, object[] args) where T : class { throw null; }
        protected void CacheTarget<T>(T target) where T : class { }
    }
    public static partial class CallSiteHelpers
    {
        public static bool IsInternalFrame(System.Reflection.MethodBase mb) { throw null; }
    }
    [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
    [System.Diagnostics.DebuggerStepThroughAttribute]
    public static partial class CallSiteOps
    {
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
        [System.ObsoleteAttribute("do not use this method", true)]
        public static void AddRule<T>(System.Runtime.CompilerServices.CallSite<T> site, T rule) where T : class { }
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
        [System.ObsoleteAttribute("do not use this method", true)]
        public static T Bind<T>(System.Runtime.CompilerServices.CallSiteBinder binder, System.Runtime.CompilerServices.CallSite<T> site, object[] args) where T : class { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
        [System.ObsoleteAttribute("do not use this method", true)]
        public static void ClearMatch(System.Runtime.CompilerServices.CallSite site) { }
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
        [System.ObsoleteAttribute("do not use this method", true)]
        public static System.Runtime.CompilerServices.CallSite<T> CreateMatchmaker<T>(System.Runtime.CompilerServices.CallSite<T> site) where T : class { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
        [System.ObsoleteAttribute("do not use this method", true)]
        public static T[] GetCachedRules<T>(System.Runtime.CompilerServices.RuleCache<T> cache) where T : class { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
        [System.ObsoleteAttribute("do not use this method", true)]
        public static bool GetMatch(System.Runtime.CompilerServices.CallSite site) { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
        [System.ObsoleteAttribute("do not use this method", true)]
        public static System.Runtime.CompilerServices.RuleCache<T> GetRuleCache<T>(System.Runtime.CompilerServices.CallSite<T> site) where T : class { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
        [System.ObsoleteAttribute("do not use this method", true)]
        public static T[] GetRules<T>(System.Runtime.CompilerServices.CallSite<T> site) where T : class { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
        [System.ObsoleteAttribute("do not use this method", true)]
        public static void MoveRule<T>(System.Runtime.CompilerServices.RuleCache<T> cache, T rule, int i) where T : class { }
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
        [System.ObsoleteAttribute("do not use this method", true)]
        public static bool SetNotMatched(System.Runtime.CompilerServices.CallSite site) { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
        [System.ObsoleteAttribute("do not use this method", true)]
        public static void UpdateRules<T>(System.Runtime.CompilerServices.CallSite<T> @this, int matched) where T : class { }
    }
    [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
    [System.Diagnostics.DebuggerStepThroughAttribute]
    public sealed partial class Closure
    {
        public readonly object[] Constants;
        public readonly object[] Locals;
        public Closure(object[] constants, object[] locals) { }
    }
    public abstract partial class DebugInfoGenerator
    {
        protected DebugInfoGenerator() { }
        public abstract void MarkSequencePoint(System.Linq.Expressions.LambdaExpression method, int ilOffset, System.Linq.Expressions.DebugInfoExpression sequencePoint);
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(10636))]
    public sealed partial class DynamicAttribute : System.Attribute
    {
        public DynamicAttribute() { }
        public DynamicAttribute(bool[] transformFlags) { }
        public System.Collections.Generic.IList<bool> TransformFlags { get { throw null; } }
    }
    [System.ObsoleteAttribute("do not use this type", true)]
    public partial class ExecutionScope
    {
        internal ExecutionScope() { }
        public object[] Globals;
        public object[] Locals;
        public System.Runtime.CompilerServices.ExecutionScope Parent;
        public System.Delegate CreateDelegate(int indexLambda, object[] locals) { throw null; }
        public object[] CreateHoistedLocals() { throw null; }
        public System.Linq.Expressions.Expression IsolateExpression(System.Linq.Expressions.Expression expression, object[] locals) { throw null; }
    }
    public partial interface IRuntimeVariables
    {
        int Count { get; }
        object this[int index] { get; set; }
    }
    public partial interface IStrongBox
    {
        object Value { get; set; }
    }
    public sealed partial class ReadOnlyCollectionBuilder<T> : System.Collections.Generic.ICollection<T>, System.Collections.Generic.IEnumerable<T>, System.Collections.Generic.IList<T>, System.Collections.ICollection, System.Collections.IEnumerable, System.Collections.IList
    {
        public ReadOnlyCollectionBuilder() { }
        public ReadOnlyCollectionBuilder(System.Collections.Generic.IEnumerable<T> collection) { }
        public ReadOnlyCollectionBuilder(int capacity) { }
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
        public void Clear() { }
        public bool Contains(T item) { throw null; }
        public void CopyTo(T[] array, int arrayIndex) { }
        public System.Collections.Generic.IEnumerator<T> GetEnumerator() { throw null; }
        public int IndexOf(T item) { throw null; }
        public void Insert(int index, T item) { }
        public bool Remove(T item) { throw null; }
        public void RemoveAt(int index) { }
        public void Reverse() { }
        public void Reverse(int index, int count) { }
        void System.Collections.ICollection.CopyTo(System.Array array, int index) { }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        int System.Collections.IList.Add(object value) { throw null; }
        bool System.Collections.IList.Contains(object value) { throw null; }
        int System.Collections.IList.IndexOf(object value) { throw null; }
        void System.Collections.IList.Insert(int index, object value) { }
        void System.Collections.IList.Remove(object value) { }
        public T[] ToArray() { throw null; }
        public System.Collections.ObjectModel.ReadOnlyCollection<T> ToReadOnlyCollection() { throw null; }
    }
    [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
    [System.Diagnostics.DebuggerStepThroughAttribute]
    public partial class RuleCache<T> where T : class
    {
        internal RuleCache() { }
    }
    [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
    [System.Diagnostics.DebuggerStepThroughAttribute]
    public static partial class RuntimeOps
    {
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
        [System.ObsoleteAttribute("do not use this method", true)]
        public static System.Runtime.CompilerServices.IRuntimeVariables CreateRuntimeVariables() { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
        [System.ObsoleteAttribute("do not use this method", true)]
        public static System.Runtime.CompilerServices.IRuntimeVariables CreateRuntimeVariables(object[] data, long[] indexes) { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
        [System.ObsoleteAttribute("do not use this method", true)]
        public static bool ExpandoCheckVersion(System.Dynamic.ExpandoObject expando, object version) { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
        [System.ObsoleteAttribute("do not use this method", true)]
        public static void ExpandoPromoteClass(System.Dynamic.ExpandoObject expando, object oldClass, object newClass) { }
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
        [System.ObsoleteAttribute("do not use this method", true)]
        public static bool ExpandoTryDeleteValue(System.Dynamic.ExpandoObject expando, object indexClass, int index, string name, bool ignoreCase) { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
        [System.ObsoleteAttribute("do not use this method", true)]
        public static bool ExpandoTryGetValue(System.Dynamic.ExpandoObject expando, object indexClass, int index, string name, bool ignoreCase, out object value) { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
        [System.ObsoleteAttribute("do not use this method", true)]
        public static object ExpandoTrySetValue(System.Dynamic.ExpandoObject expando, object indexClass, int index, object value, string name, bool ignoreCase) { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
        [System.ObsoleteAttribute("do not use this method", true)]
        public static System.Runtime.CompilerServices.IRuntimeVariables MergeRuntimeVariables(System.Runtime.CompilerServices.IRuntimeVariables first, System.Runtime.CompilerServices.IRuntimeVariables second, int[] indexes) { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
        [System.ObsoleteAttribute("do not use this method", true)]
        public static System.Linq.Expressions.Expression Quote(System.Linq.Expressions.Expression expression, object hoistedLocals, object[] locals) { throw null; }
    }
    public partial class StrongBox<T> : System.Runtime.CompilerServices.IStrongBox
    {
        public T Value;
        public StrongBox() { }
        public StrongBox(T value) { }
        object System.Runtime.CompilerServices.IStrongBox.Value { get { throw null; } set { } }
    }
}
namespace System.Security.Cryptography
{
    [System.Security.Permissions.HostProtectionAttribute(System.Security.Permissions.SecurityAction.LinkDemand, MayLeakOnAbort=true)]
    public sealed partial class AesCryptoServiceProvider : System.Security.Cryptography.Aes
    {
        public AesCryptoServiceProvider() { }
        public override int FeedbackSize { get { throw null; } set { } }
        public override byte[] IV { get { throw null; } set { } }
        public override byte[] Key { get { throw null; } set { } }
        public override int KeySize { get { throw null; } set { } }
        public override System.Security.Cryptography.CipherMode Mode { get { throw null; } set { } }
        public override System.Security.Cryptography.PaddingMode Padding { get { throw null; } set { } }
        public override System.Security.Cryptography.ICryptoTransform CreateDecryptor() { throw null; }
        public override System.Security.Cryptography.ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[] rgbIV) { throw null; }
        public override System.Security.Cryptography.ICryptoTransform CreateEncryptor() { throw null; }
        public override System.Security.Cryptography.ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[] rgbIV) { throw null; }
        protected override void Dispose(bool disposing) { }
        public override void GenerateIV() { }
        public override void GenerateKey() { }
    }
    public sealed partial class AesManaged : System.Security.Cryptography.Aes
    {
        public AesManaged() { }
        public override int FeedbackSize { get { throw null; } set { } }
        public override byte[] IV { get { throw null; } set { } }
        public override byte[] Key { get { throw null; } set { } }
        public override int KeySize { get { throw null; } set { } }
        public override System.Security.Cryptography.CipherMode Mode { get { throw null; } set { } }
        public override System.Security.Cryptography.PaddingMode Padding { get { throw null; } set { } }
        public override System.Security.Cryptography.ICryptoTransform CreateDecryptor() { throw null; }
        public override System.Security.Cryptography.ICryptoTransform CreateDecryptor(byte[] key, byte[] iv) { throw null; }
        public override System.Security.Cryptography.ICryptoTransform CreateEncryptor() { throw null; }
        public override System.Security.Cryptography.ICryptoTransform CreateEncryptor(byte[] key, byte[] iv) { throw null; }
        protected override void Dispose(bool disposing) { }
        public override void GenerateIV() { }
        public override void GenerateKey() { }
    }
    [System.Security.Permissions.HostProtectionAttribute(System.Security.Permissions.SecurityAction.LinkDemand, MayLeakOnAbort=true)]
    public abstract partial class ECDiffieHellmanPublicKey : System.IDisposable
    {
        protected ECDiffieHellmanPublicKey(byte[] keyBlob) { }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        public virtual byte[] ToByteArray() { throw null; }
        public abstract string ToXmlString();
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct ECPoint
    {
        public byte[] X;
        public byte[] Y;
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct ECCurve
    {
        public byte[] A;
        public byte[] B;
        public byte[] Cofactor;
        public System.Security.Cryptography.ECCurve.ECCurveType CurveType;
        public System.Security.Cryptography.ECPoint G;
        public System.Nullable<System.Security.Cryptography.HashAlgorithmName> Hash;
        public byte[] Order;
        public byte[] Polynomial;
        public byte[] Prime;
        public byte[] Seed;
        public bool IsCharacteristic2 { get { throw null; } }
        public bool IsExplicit { get { throw null; } }
        public bool IsNamed { get { throw null; } }
        public bool IsPrime { get { throw null; } }
        public System.Security.Cryptography.Oid Oid { get { throw null; } }
        public static System.Security.Cryptography.ECCurve CreateFromFriendlyName(string oidFriendlyName) { throw null; }
        public static System.Security.Cryptography.ECCurve CreateFromOid(System.Security.Cryptography.Oid curveOid) { throw null; }
        public static System.Security.Cryptography.ECCurve CreateFromValue(string oidValue) { throw null; }
        public void Validate() { }
        public enum ECCurveType
        {
            Characteristic2 = 4,
            Implicit = 0,
            Named = 5,
            PrimeMontgomery = 3,
            PrimeShortWeierstrass = 1,
            PrimeTwistedEdwards = 2,
        }
        public static partial class NamedCurves
        {
            public static System.Security.Cryptography.ECCurve brainpoolP160r1 { get { throw null; } }
            public static System.Security.Cryptography.ECCurve brainpoolP160t1 { get { throw null; } }
            public static System.Security.Cryptography.ECCurve brainpoolP192r1 { get { throw null; } }
            public static System.Security.Cryptography.ECCurve brainpoolP192t1 { get { throw null; } }
            public static System.Security.Cryptography.ECCurve brainpoolP224r1 { get { throw null; } }
            public static System.Security.Cryptography.ECCurve brainpoolP224t1 { get { throw null; } }
            public static System.Security.Cryptography.ECCurve brainpoolP256r1 { get { throw null; } }
            public static System.Security.Cryptography.ECCurve brainpoolP256t1 { get { throw null; } }
            public static System.Security.Cryptography.ECCurve brainpoolP320r1 { get { throw null; } }
            public static System.Security.Cryptography.ECCurve brainpoolP320t1 { get { throw null; } }
            public static System.Security.Cryptography.ECCurve brainpoolP384r1 { get { throw null; } }
            public static System.Security.Cryptography.ECCurve brainpoolP384t1 { get { throw null; } }
            public static System.Security.Cryptography.ECCurve brainpoolP512r1 { get { throw null; } }
            public static System.Security.Cryptography.ECCurve brainpoolP512t1 { get { throw null; } }
            public static System.Security.Cryptography.ECCurve nistP256 { get { throw null; } }
            public static System.Security.Cryptography.ECCurve nistP384 { get { throw null; } }
            public static System.Security.Cryptography.ECCurve nistP521 { get { throw null; } }
        }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct ECParameters
    {
        public System.Security.Cryptography.ECCurve Curve;
        public byte[] D;
        public System.Security.Cryptography.ECPoint Q;
        public void Validate() { }
    }
    [System.Security.Permissions.HostProtectionAttribute(System.Security.Permissions.SecurityAction.LinkDemand, MayLeakOnAbort=true)]
    public abstract partial class ECDsa : System.Security.Cryptography.AsymmetricAlgorithm
    {
        protected ECDsa() { }
        public override string KeyExchangeAlgorithm { get { throw null; } }
        public override string SignatureAlgorithm { get { throw null; } }
        public static new System.Security.Cryptography.ECDsa Create() { throw null; }
        public static new System.Security.Cryptography.ECDsa Create(string algorithm) { throw null; }
        public static System.Security.Cryptography.ECDsa Create(System.Security.Cryptography.ECCurve curve) { throw null; }
        public static System.Security.Cryptography.ECDsa Create(System.Security.Cryptography.ECParameters parameters) { throw null; }
        public virtual System.Security.Cryptography.ECParameters ExportExplicitParameters(bool includePrivateParameters) { throw null; }
        public virtual System.Security.Cryptography.ECParameters ExportParameters(bool includePrivateParameters) { throw null; }
        public virtual void GenerateKey(System.Security.Cryptography.ECCurve curve) { }
        public virtual void ImportParameters(System.Security.Cryptography.ECParameters parameters) { }
        protected virtual byte[] HashData(byte[] data, int offset, int count, System.Security.Cryptography.HashAlgorithmName hashAlgorithm) { throw null; }
        protected virtual byte[] HashData(System.IO.Stream data, System.Security.Cryptography.HashAlgorithmName hashAlgorithm) { throw null; }
        public virtual byte[] SignData(byte[] data, int offset, int count, System.Security.Cryptography.HashAlgorithmName hashAlgorithm) { throw null; }
        public virtual byte[] SignData(byte[] data, System.Security.Cryptography.HashAlgorithmName hashAlgorithm) { throw null; }
        public virtual byte[] SignData(System.IO.Stream data, System.Security.Cryptography.HashAlgorithmName hashAlgorithm) { throw null; }
        public abstract byte[] SignHash(byte[] hash);
        public bool VerifyData(byte[] data, byte[] signature, System.Security.Cryptography.HashAlgorithmName hashAlgorithm) { throw null; }
        public virtual bool VerifyData(byte[] data, int offset, int count, byte[] signature, System.Security.Cryptography.HashAlgorithmName hashAlgorithm) { throw null; }
        public bool VerifyData(System.IO.Stream data, byte[] signature, System.Security.Cryptography.HashAlgorithmName hashAlgorithm) { throw null; }
        public abstract bool VerifyHash(byte[] hash, byte[] signature);
    }
}
namespace System.Threading
{
    public enum LockRecursionPolicy
    {
        NoRecursion = 0,
        SupportsRecursion = 1,
    }
    [System.Security.Permissions.HostProtectionAttribute(System.Security.Permissions.SecurityAction.LinkDemand, MayLeakOnAbort=true)]
    public partial class ReaderWriterLockSlim : System.IDisposable
    {
        public ReaderWriterLockSlim() { }
        public ReaderWriterLockSlim(System.Threading.LockRecursionPolicy recursionPolicy) { }
        public int CurrentReadCount { get { throw null; } }
        public bool IsReadLockHeld { get { throw null; } }
        public bool IsUpgradeableReadLockHeld { get { throw null; } }
        public bool IsWriteLockHeld { get { throw null; } }
        public System.Threading.LockRecursionPolicy RecursionPolicy { get { throw null; } }
        public int RecursiveReadCount { get { throw null; } }
        public int RecursiveUpgradeCount { get { throw null; } }
        public int RecursiveWriteCount { get { throw null; } }
        public int WaitingReadCount { get { throw null; } }
        public int WaitingUpgradeCount { get { throw null; } }
        public int WaitingWriteCount { get { throw null; } }
        public void Dispose() { }
        public void EnterReadLock() { }
        public void EnterUpgradeableReadLock() { }
        public void EnterWriteLock() { }
        public void ExitReadLock() { }
        public void ExitUpgradeableReadLock() { }
        public void ExitWriteLock() { }
        public bool TryEnterReadLock(int millisecondsTimeout) { throw null; }
        public bool TryEnterReadLock(System.TimeSpan timeout) { throw null; }
        public bool TryEnterUpgradeableReadLock(int millisecondsTimeout) { throw null; }
        public bool TryEnterUpgradeableReadLock(System.TimeSpan timeout) { throw null; }
        public bool TryEnterWriteLock(int millisecondsTimeout) { throw null; }
        public bool TryEnterWriteLock(System.TimeSpan timeout) { throw null; }
    }
}
namespace System.Threading.Tasks
{
    public static partial class TaskExtensions
    {
        public static System.Threading.Tasks.Task Unwrap(this System.Threading.Tasks.Task<System.Threading.Tasks.Task> task) { throw null; }
        public static System.Threading.Tasks.Task<TResult> Unwrap<TResult>(this System.Threading.Tasks.Task<System.Threading.Tasks.Task<TResult>> task) { throw null; }
    }
}
