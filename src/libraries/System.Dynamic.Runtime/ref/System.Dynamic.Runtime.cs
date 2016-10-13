// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------


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
        int System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<System.String, System.Object>>.Count { get { throw null; } }
        bool System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<System.String, System.Object>>.IsReadOnly { get { throw null; } }
        object System.Collections.Generic.IDictionary<System.String, System.Object>.this[string key] { get { throw null; } set { } }
        System.Collections.Generic.ICollection<string> System.Collections.Generic.IDictionary<System.String, System.Object>.Keys { get { throw null; } }
        System.Collections.Generic.ICollection<object> System.Collections.Generic.IDictionary<System.String, System.Object>.Values { get { throw null; } }
        event System.ComponentModel.PropertyChangedEventHandler System.ComponentModel.INotifyPropertyChanged.PropertyChanged { add { } remove { } }
        void System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<System.String, System.Object>>.Add(System.Collections.Generic.KeyValuePair<string, object> item) { }
        void System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<System.String, System.Object>>.Clear() { }
        bool System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<System.String, System.Object>>.Contains(System.Collections.Generic.KeyValuePair<string, object> item) { throw null; }
        void System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<System.String, System.Object>>.CopyTo(System.Collections.Generic.KeyValuePair<string, object>[] array, int arrayIndex) { }
        bool System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<System.String, System.Object>>.Remove(System.Collections.Generic.KeyValuePair<string, object> item) { throw null; }
        void System.Collections.Generic.IDictionary<System.String, System.Object>.Add(string key, object value) { }
        bool System.Collections.Generic.IDictionary<System.String, System.Object>.ContainsKey(string key) { throw null; }
        bool System.Collections.Generic.IDictionary<System.String, System.Object>.Remove(string key) { throw null; }
        bool System.Collections.Generic.IDictionary<System.String, System.Object>.TryGetValue(string key, out object value) { throw null; }
        System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<string, object>> System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<System.String, System.Object>>.GetEnumerator() { throw null; }
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
        public sealed override System.Dynamic.DynamicMetaObject Bind(System.Dynamic.DynamicMetaObject target, System.Dynamic.DynamicMetaObject[] args) { throw null; }
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
namespace System.Linq.Expressions
{
    public partial class DynamicExpression : System.Linq.Expressions.Expression, System.Linq.Expressions.IArgumentProvider, System.Linq.Expressions.IDynamicExpression
    {
        internal DynamicExpression() { }
        public System.Collections.ObjectModel.ReadOnlyCollection<System.Linq.Expressions.Expression> Arguments { get { throw null; } }
        public System.Runtime.CompilerServices.CallSiteBinder Binder { get { throw null; } }
        public System.Type DelegateType { get { throw null; } }
        public sealed override System.Linq.Expressions.ExpressionType NodeType { get { throw null; } }
        int System.Linq.Expressions.IArgumentProvider.ArgumentCount { get { throw null; } }
        public override System.Type Type { get { throw null; } }
        protected override System.Linq.Expressions.Expression Accept(System.Linq.Expressions.ExpressionVisitor visitor) { throw null; }
        public static System.Linq.Expressions.DynamicExpression Dynamic(System.Runtime.CompilerServices.CallSiteBinder binder, System.Type returnType, System.Collections.Generic.IEnumerable<System.Linq.Expressions.Expression> arguments) { throw null; }
        public static System.Linq.Expressions.DynamicExpression Dynamic(System.Runtime.CompilerServices.CallSiteBinder binder, System.Type returnType, System.Linq.Expressions.Expression arg0) { throw null; }
        public static System.Linq.Expressions.DynamicExpression Dynamic(System.Runtime.CompilerServices.CallSiteBinder binder, System.Type returnType, System.Linq.Expressions.Expression arg0, System.Linq.Expressions.Expression arg1) { throw null; }
        public static System.Linq.Expressions.DynamicExpression Dynamic(System.Runtime.CompilerServices.CallSiteBinder binder, System.Type returnType, System.Linq.Expressions.Expression arg0, System.Linq.Expressions.Expression arg1, System.Linq.Expressions.Expression arg2) { throw null; }
        public static System.Linq.Expressions.DynamicExpression Dynamic(System.Runtime.CompilerServices.CallSiteBinder binder, System.Type returnType, System.Linq.Expressions.Expression arg0, System.Linq.Expressions.Expression arg1, System.Linq.Expressions.Expression arg2, System.Linq.Expressions.Expression arg3) { throw null; }
        public static System.Linq.Expressions.DynamicExpression Dynamic(System.Runtime.CompilerServices.CallSiteBinder binder, System.Type returnType, params System.Linq.Expressions.Expression[] arguments) { throw null; }
        public static System.Linq.Expressions.DynamicExpression MakeDynamic(System.Type delegateType, System.Runtime.CompilerServices.CallSiteBinder binder, System.Collections.Generic.IEnumerable<System.Linq.Expressions.Expression> arguments) { throw null; }
        public static System.Linq.Expressions.DynamicExpression MakeDynamic(System.Type delegateType, System.Runtime.CompilerServices.CallSiteBinder binder, System.Linq.Expressions.Expression arg0) { throw null; }
        public static System.Linq.Expressions.DynamicExpression MakeDynamic(System.Type delegateType, System.Runtime.CompilerServices.CallSiteBinder binder, System.Linq.Expressions.Expression arg0, System.Linq.Expressions.Expression arg1) { throw null; }
        public static System.Linq.Expressions.DynamicExpression MakeDynamic(System.Type delegateType, System.Runtime.CompilerServices.CallSiteBinder binder, System.Linq.Expressions.Expression arg0, System.Linq.Expressions.Expression arg1, System.Linq.Expressions.Expression arg2) { throw null; }
        public static System.Linq.Expressions.DynamicExpression MakeDynamic(System.Type delegateType, System.Runtime.CompilerServices.CallSiteBinder binder, System.Linq.Expressions.Expression arg0, System.Linq.Expressions.Expression arg1, System.Linq.Expressions.Expression arg2, System.Linq.Expressions.Expression arg3) { throw null; }
        public static System.Linq.Expressions.DynamicExpression MakeDynamic(System.Type delegateType, System.Runtime.CompilerServices.CallSiteBinder binder, params System.Linq.Expressions.Expression[] arguments) { throw null; }
        System.Linq.Expressions.Expression System.Linq.Expressions.IArgumentProvider.GetArgument(int index) { throw null; }
        object System.Linq.Expressions.IDynamicExpression.CreateCallSite() { throw null; }
        System.Linq.Expressions.Expression System.Linq.Expressions.IDynamicExpression.Rewrite(System.Linq.Expressions.Expression[] args) { throw null; }
        public System.Linq.Expressions.DynamicExpression Update(System.Collections.Generic.IEnumerable<System.Linq.Expressions.Expression> arguments) { throw null; }
    }
    public abstract partial class DynamicExpressionVisitor : System.Linq.Expressions.ExpressionVisitor
    {
        protected DynamicExpressionVisitor() { }
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
    [System.AttributeUsageAttribute((System.AttributeTargets)(10636))]
    public sealed partial class DynamicAttribute : System.Attribute
    {
        public DynamicAttribute() { }
        public DynamicAttribute(bool[] transformFlags) { }
        public System.Collections.Generic.IList<bool> TransformFlags { get { throw null; } }
    }
}
