// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------


namespace System.Linq
{
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
}
namespace System.Linq.Expressions
{
    public partial class BinaryExpression : System.Linq.Expressions.Expression
    {
        internal BinaryExpression() { }
        public override bool CanReduce { get { return default(bool); } }
        public System.Linq.Expressions.LambdaExpression Conversion { get { return default(System.Linq.Expressions.LambdaExpression); } }
        public bool IsLifted { get { return default(bool); } }
        public bool IsLiftedToNull { get { return default(bool); } }
        public System.Linq.Expressions.Expression Left { get { return default(System.Linq.Expressions.Expression); } }
        public System.Reflection.MethodInfo Method { get { return default(System.Reflection.MethodInfo); } }
        public System.Linq.Expressions.Expression Right { get { return default(System.Linq.Expressions.Expression); } }
        protected internal override System.Linq.Expressions.Expression Accept(System.Linq.Expressions.ExpressionVisitor visitor) { return default(System.Linq.Expressions.Expression); }
        public override System.Linq.Expressions.Expression Reduce() { return default(System.Linq.Expressions.Expression); }
        public System.Linq.Expressions.BinaryExpression Update(System.Linq.Expressions.Expression left, System.Linq.Expressions.LambdaExpression conversion, System.Linq.Expressions.Expression right) { return default(System.Linq.Expressions.BinaryExpression); }
    }
    public partial class BlockExpression : System.Linq.Expressions.Expression
    {
        internal BlockExpression() { }
        public System.Collections.ObjectModel.ReadOnlyCollection<System.Linq.Expressions.Expression> Expressions { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Linq.Expressions.Expression>); } }
        public sealed override System.Linq.Expressions.ExpressionType NodeType { get { return default(System.Linq.Expressions.ExpressionType); } }
        public System.Linq.Expressions.Expression Result { get { return default(System.Linq.Expressions.Expression); } }
        public override System.Type Type { get { return default(System.Type); } }
        public System.Collections.ObjectModel.ReadOnlyCollection<System.Linq.Expressions.ParameterExpression> Variables { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Linq.Expressions.ParameterExpression>); } }
        protected internal override System.Linq.Expressions.Expression Accept(System.Linq.Expressions.ExpressionVisitor visitor) { return default(System.Linq.Expressions.Expression); }
        public System.Linq.Expressions.BlockExpression Update(System.Collections.Generic.IEnumerable<System.Linq.Expressions.ParameterExpression> variables, System.Collections.Generic.IEnumerable<System.Linq.Expressions.Expression> expressions) { return default(System.Linq.Expressions.BlockExpression); }
    }
    public sealed partial class CatchBlock
    {
        internal CatchBlock() { }
        public System.Linq.Expressions.Expression Body { get { return default(System.Linq.Expressions.Expression); } }
        public System.Linq.Expressions.Expression Filter { get { return default(System.Linq.Expressions.Expression); } }
        public System.Type Test { get { return default(System.Type); } }
        public System.Linq.Expressions.ParameterExpression Variable { get { return default(System.Linq.Expressions.ParameterExpression); } }
        public override string ToString() { return default(string); }
        public System.Linq.Expressions.CatchBlock Update(System.Linq.Expressions.ParameterExpression variable, System.Linq.Expressions.Expression filter, System.Linq.Expressions.Expression body) { return default(System.Linq.Expressions.CatchBlock); }
    }
    public partial class ConditionalExpression : System.Linq.Expressions.Expression
    {
        internal ConditionalExpression() { }
        public System.Linq.Expressions.Expression IfFalse { get { return default(System.Linq.Expressions.Expression); } }
        public System.Linq.Expressions.Expression IfTrue { get { return default(System.Linq.Expressions.Expression); } }
        public sealed override System.Linq.Expressions.ExpressionType NodeType { get { return default(System.Linq.Expressions.ExpressionType); } }
        public System.Linq.Expressions.Expression Test { get { return default(System.Linq.Expressions.Expression); } }
        public override System.Type Type { get { return default(System.Type); } }
        protected internal override System.Linq.Expressions.Expression Accept(System.Linq.Expressions.ExpressionVisitor visitor) { return default(System.Linq.Expressions.Expression); }
        public System.Linq.Expressions.ConditionalExpression Update(System.Linq.Expressions.Expression test, System.Linq.Expressions.Expression ifTrue, System.Linq.Expressions.Expression ifFalse) { return default(System.Linq.Expressions.ConditionalExpression); }
    }
    public partial class ConstantExpression : System.Linq.Expressions.Expression
    {
        internal ConstantExpression() { }
        public sealed override System.Linq.Expressions.ExpressionType NodeType { get { return default(System.Linq.Expressions.ExpressionType); } }
        public override System.Type Type { get { return default(System.Type); } }
        public object Value { get { return default(object); } }
        protected internal override System.Linq.Expressions.Expression Accept(System.Linq.Expressions.ExpressionVisitor visitor) { return default(System.Linq.Expressions.Expression); }
    }
    public partial class DebugInfoExpression : System.Linq.Expressions.Expression
    {
        internal DebugInfoExpression() { }
        public System.Linq.Expressions.SymbolDocumentInfo Document { get { return default(System.Linq.Expressions.SymbolDocumentInfo); } }
        public virtual int EndColumn { get { return default(int); } }
        public virtual int EndLine { get { return default(int); } }
        public virtual bool IsClear { get { return default(bool); } }
        public sealed override System.Linq.Expressions.ExpressionType NodeType { get { return default(System.Linq.Expressions.ExpressionType); } }
        public virtual int StartColumn { get { return default(int); } }
        public virtual int StartLine { get { return default(int); } }
        public sealed override System.Type Type { get { return default(System.Type); } }
        protected internal override System.Linq.Expressions.Expression Accept(System.Linq.Expressions.ExpressionVisitor visitor) { return default(System.Linq.Expressions.Expression); }
    }
    public sealed partial class DefaultExpression : System.Linq.Expressions.Expression
    {
        internal DefaultExpression() { }
        public sealed override System.Linq.Expressions.ExpressionType NodeType { get { return default(System.Linq.Expressions.ExpressionType); } }
        public sealed override System.Type Type { get { return default(System.Type); } }
        protected internal override System.Linq.Expressions.Expression Accept(System.Linq.Expressions.ExpressionVisitor visitor) { return default(System.Linq.Expressions.Expression); }
    }
    public sealed partial class ElementInit : System.Linq.Expressions.IArgumentProvider
    {
        internal ElementInit() { }
        public System.Reflection.MethodInfo AddMethod { get { return default(System.Reflection.MethodInfo); } }
        public System.Collections.ObjectModel.ReadOnlyCollection<System.Linq.Expressions.Expression> Arguments { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Linq.Expressions.Expression>); } }
        int System.Linq.Expressions.IArgumentProvider.ArgumentCount { get { return default(int); } }
        System.Linq.Expressions.Expression System.Linq.Expressions.IArgumentProvider.GetArgument(int index) { return default(System.Linq.Expressions.Expression); }
        public override string ToString() { return default(string); }
        public System.Linq.Expressions.ElementInit Update(System.Collections.Generic.IEnumerable<System.Linq.Expressions.Expression> arguments) { return default(System.Linq.Expressions.ElementInit); }
    }
    public abstract partial class Expression
    {
        protected Expression() { }
        public virtual bool CanReduce { get { return default(bool); } }
        public virtual System.Linq.Expressions.ExpressionType NodeType { get { return default(System.Linq.Expressions.ExpressionType); } }
        public virtual System.Type Type { get { return default(System.Type); } }
        protected internal virtual System.Linq.Expressions.Expression Accept(System.Linq.Expressions.ExpressionVisitor visitor) { return default(System.Linq.Expressions.Expression); }
        public static System.Linq.Expressions.BinaryExpression Add(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right) { return default(System.Linq.Expressions.BinaryExpression); }
        public static System.Linq.Expressions.BinaryExpression Add(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right, System.Reflection.MethodInfo method) { return default(System.Linq.Expressions.BinaryExpression); }
        public static System.Linq.Expressions.BinaryExpression AddAssign(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right) { return default(System.Linq.Expressions.BinaryExpression); }
        public static System.Linq.Expressions.BinaryExpression AddAssign(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right, System.Reflection.MethodInfo method) { return default(System.Linq.Expressions.BinaryExpression); }
        public static System.Linq.Expressions.BinaryExpression AddAssign(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right, System.Reflection.MethodInfo method, System.Linq.Expressions.LambdaExpression conversion) { return default(System.Linq.Expressions.BinaryExpression); }
        public static System.Linq.Expressions.BinaryExpression AddAssignChecked(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right) { return default(System.Linq.Expressions.BinaryExpression); }
        public static System.Linq.Expressions.BinaryExpression AddAssignChecked(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right, System.Reflection.MethodInfo method) { return default(System.Linq.Expressions.BinaryExpression); }
        public static System.Linq.Expressions.BinaryExpression AddAssignChecked(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right, System.Reflection.MethodInfo method, System.Linq.Expressions.LambdaExpression conversion) { return default(System.Linq.Expressions.BinaryExpression); }
        public static System.Linq.Expressions.BinaryExpression AddChecked(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right) { return default(System.Linq.Expressions.BinaryExpression); }
        public static System.Linq.Expressions.BinaryExpression AddChecked(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right, System.Reflection.MethodInfo method) { return default(System.Linq.Expressions.BinaryExpression); }
        public static System.Linq.Expressions.BinaryExpression And(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right) { return default(System.Linq.Expressions.BinaryExpression); }
        public static System.Linq.Expressions.BinaryExpression And(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right, System.Reflection.MethodInfo method) { return default(System.Linq.Expressions.BinaryExpression); }
        public static System.Linq.Expressions.BinaryExpression AndAlso(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right) { return default(System.Linq.Expressions.BinaryExpression); }
        public static System.Linq.Expressions.BinaryExpression AndAlso(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right, System.Reflection.MethodInfo method) { return default(System.Linq.Expressions.BinaryExpression); }
        public static System.Linq.Expressions.BinaryExpression AndAssign(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right) { return default(System.Linq.Expressions.BinaryExpression); }
        public static System.Linq.Expressions.BinaryExpression AndAssign(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right, System.Reflection.MethodInfo method) { return default(System.Linq.Expressions.BinaryExpression); }
        public static System.Linq.Expressions.BinaryExpression AndAssign(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right, System.Reflection.MethodInfo method, System.Linq.Expressions.LambdaExpression conversion) { return default(System.Linq.Expressions.BinaryExpression); }
        public static System.Linq.Expressions.IndexExpression ArrayAccess(System.Linq.Expressions.Expression array, System.Collections.Generic.IEnumerable<System.Linq.Expressions.Expression> indexes) { return default(System.Linq.Expressions.IndexExpression); }
        public static System.Linq.Expressions.IndexExpression ArrayAccess(System.Linq.Expressions.Expression array, params System.Linq.Expressions.Expression[] indexes) { return default(System.Linq.Expressions.IndexExpression); }
        public static System.Linq.Expressions.MethodCallExpression ArrayIndex(System.Linq.Expressions.Expression array, System.Collections.Generic.IEnumerable<System.Linq.Expressions.Expression> indexes) { return default(System.Linq.Expressions.MethodCallExpression); }
        public static System.Linq.Expressions.BinaryExpression ArrayIndex(System.Linq.Expressions.Expression array, System.Linq.Expressions.Expression index) { return default(System.Linq.Expressions.BinaryExpression); }
        public static System.Linq.Expressions.MethodCallExpression ArrayIndex(System.Linq.Expressions.Expression array, params System.Linq.Expressions.Expression[] indexes) { return default(System.Linq.Expressions.MethodCallExpression); }
        public static System.Linq.Expressions.UnaryExpression ArrayLength(System.Linq.Expressions.Expression array) { return default(System.Linq.Expressions.UnaryExpression); }
        public static System.Linq.Expressions.BinaryExpression Assign(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right) { return default(System.Linq.Expressions.BinaryExpression); }
        public static System.Linq.Expressions.MemberAssignment Bind(System.Reflection.MemberInfo member, System.Linq.Expressions.Expression expression) { return default(System.Linq.Expressions.MemberAssignment); }
        public static System.Linq.Expressions.MemberAssignment Bind(System.Reflection.MethodInfo propertyAccessor, System.Linq.Expressions.Expression expression) { return default(System.Linq.Expressions.MemberAssignment); }
        public static System.Linq.Expressions.BlockExpression Block(System.Collections.Generic.IEnumerable<System.Linq.Expressions.Expression> expressions) { return default(System.Linq.Expressions.BlockExpression); }
        public static System.Linq.Expressions.BlockExpression Block(System.Collections.Generic.IEnumerable<System.Linq.Expressions.ParameterExpression> variables, System.Collections.Generic.IEnumerable<System.Linq.Expressions.Expression> expressions) { return default(System.Linq.Expressions.BlockExpression); }
        public static System.Linq.Expressions.BlockExpression Block(System.Collections.Generic.IEnumerable<System.Linq.Expressions.ParameterExpression> variables, params System.Linq.Expressions.Expression[] expressions) { return default(System.Linq.Expressions.BlockExpression); }
        public static System.Linq.Expressions.BlockExpression Block(System.Linq.Expressions.Expression arg0, System.Linq.Expressions.Expression arg1) { return default(System.Linq.Expressions.BlockExpression); }
        public static System.Linq.Expressions.BlockExpression Block(System.Linq.Expressions.Expression arg0, System.Linq.Expressions.Expression arg1, System.Linq.Expressions.Expression arg2) { return default(System.Linq.Expressions.BlockExpression); }
        public static System.Linq.Expressions.BlockExpression Block(System.Linq.Expressions.Expression arg0, System.Linq.Expressions.Expression arg1, System.Linq.Expressions.Expression arg2, System.Linq.Expressions.Expression arg3) { return default(System.Linq.Expressions.BlockExpression); }
        public static System.Linq.Expressions.BlockExpression Block(System.Linq.Expressions.Expression arg0, System.Linq.Expressions.Expression arg1, System.Linq.Expressions.Expression arg2, System.Linq.Expressions.Expression arg3, System.Linq.Expressions.Expression arg4) { return default(System.Linq.Expressions.BlockExpression); }
        public static System.Linq.Expressions.BlockExpression Block(params System.Linq.Expressions.Expression[] expressions) { return default(System.Linq.Expressions.BlockExpression); }
        public static System.Linq.Expressions.BlockExpression Block(System.Type type, System.Collections.Generic.IEnumerable<System.Linq.Expressions.Expression> expressions) { return default(System.Linq.Expressions.BlockExpression); }
        public static System.Linq.Expressions.BlockExpression Block(System.Type type, System.Collections.Generic.IEnumerable<System.Linq.Expressions.ParameterExpression> variables, System.Collections.Generic.IEnumerable<System.Linq.Expressions.Expression> expressions) { return default(System.Linq.Expressions.BlockExpression); }
        public static System.Linq.Expressions.BlockExpression Block(System.Type type, System.Collections.Generic.IEnumerable<System.Linq.Expressions.ParameterExpression> variables, params System.Linq.Expressions.Expression[] expressions) { return default(System.Linq.Expressions.BlockExpression); }
        public static System.Linq.Expressions.BlockExpression Block(System.Type type, params System.Linq.Expressions.Expression[] expressions) { return default(System.Linq.Expressions.BlockExpression); }
        public static System.Linq.Expressions.GotoExpression Break(System.Linq.Expressions.LabelTarget target) { return default(System.Linq.Expressions.GotoExpression); }
        public static System.Linq.Expressions.GotoExpression Break(System.Linq.Expressions.LabelTarget target, System.Linq.Expressions.Expression value) { return default(System.Linq.Expressions.GotoExpression); }
        public static System.Linq.Expressions.GotoExpression Break(System.Linq.Expressions.LabelTarget target, System.Linq.Expressions.Expression value, System.Type type) { return default(System.Linq.Expressions.GotoExpression); }
        public static System.Linq.Expressions.GotoExpression Break(System.Linq.Expressions.LabelTarget target, System.Type type) { return default(System.Linq.Expressions.GotoExpression); }
        public static System.Linq.Expressions.MethodCallExpression Call(System.Linq.Expressions.Expression instance, System.Reflection.MethodInfo method) { return default(System.Linq.Expressions.MethodCallExpression); }
        public static System.Linq.Expressions.MethodCallExpression Call(System.Linq.Expressions.Expression instance, System.Reflection.MethodInfo method, System.Collections.Generic.IEnumerable<System.Linq.Expressions.Expression> arguments) { return default(System.Linq.Expressions.MethodCallExpression); }
        public static System.Linq.Expressions.MethodCallExpression Call(System.Linq.Expressions.Expression instance, System.Reflection.MethodInfo method, System.Linq.Expressions.Expression arg0, System.Linq.Expressions.Expression arg1) { return default(System.Linq.Expressions.MethodCallExpression); }
        public static System.Linq.Expressions.MethodCallExpression Call(System.Linq.Expressions.Expression instance, System.Reflection.MethodInfo method, System.Linq.Expressions.Expression arg0, System.Linq.Expressions.Expression arg1, System.Linq.Expressions.Expression arg2) { return default(System.Linq.Expressions.MethodCallExpression); }
        public static System.Linq.Expressions.MethodCallExpression Call(System.Linq.Expressions.Expression instance, System.Reflection.MethodInfo method, params System.Linq.Expressions.Expression[] arguments) { return default(System.Linq.Expressions.MethodCallExpression); }
        public static System.Linq.Expressions.MethodCallExpression Call(System.Linq.Expressions.Expression instance, string methodName, System.Type[] typeArguments, params System.Linq.Expressions.Expression[] arguments) { return default(System.Linq.Expressions.MethodCallExpression); }
        public static System.Linq.Expressions.MethodCallExpression Call(System.Reflection.MethodInfo method, System.Collections.Generic.IEnumerable<System.Linq.Expressions.Expression> arguments) { return default(System.Linq.Expressions.MethodCallExpression); }
        public static System.Linq.Expressions.MethodCallExpression Call(System.Reflection.MethodInfo method, System.Linq.Expressions.Expression arg0) { return default(System.Linq.Expressions.MethodCallExpression); }
        public static System.Linq.Expressions.MethodCallExpression Call(System.Reflection.MethodInfo method, System.Linq.Expressions.Expression arg0, System.Linq.Expressions.Expression arg1) { return default(System.Linq.Expressions.MethodCallExpression); }
        public static System.Linq.Expressions.MethodCallExpression Call(System.Reflection.MethodInfo method, System.Linq.Expressions.Expression arg0, System.Linq.Expressions.Expression arg1, System.Linq.Expressions.Expression arg2) { return default(System.Linq.Expressions.MethodCallExpression); }
        public static System.Linq.Expressions.MethodCallExpression Call(System.Reflection.MethodInfo method, System.Linq.Expressions.Expression arg0, System.Linq.Expressions.Expression arg1, System.Linq.Expressions.Expression arg2, System.Linq.Expressions.Expression arg3) { return default(System.Linq.Expressions.MethodCallExpression); }
        public static System.Linq.Expressions.MethodCallExpression Call(System.Reflection.MethodInfo method, System.Linq.Expressions.Expression arg0, System.Linq.Expressions.Expression arg1, System.Linq.Expressions.Expression arg2, System.Linq.Expressions.Expression arg3, System.Linq.Expressions.Expression arg4) { return default(System.Linq.Expressions.MethodCallExpression); }
        public static System.Linq.Expressions.MethodCallExpression Call(System.Reflection.MethodInfo method, params System.Linq.Expressions.Expression[] arguments) { return default(System.Linq.Expressions.MethodCallExpression); }
        public static System.Linq.Expressions.MethodCallExpression Call(System.Type type, string methodName, System.Type[] typeArguments, params System.Linq.Expressions.Expression[] arguments) { return default(System.Linq.Expressions.MethodCallExpression); }
        public static System.Linq.Expressions.CatchBlock Catch(System.Linq.Expressions.ParameterExpression variable, System.Linq.Expressions.Expression body) { return default(System.Linq.Expressions.CatchBlock); }
        public static System.Linq.Expressions.CatchBlock Catch(System.Linq.Expressions.ParameterExpression variable, System.Linq.Expressions.Expression body, System.Linq.Expressions.Expression filter) { return default(System.Linq.Expressions.CatchBlock); }
        public static System.Linq.Expressions.CatchBlock Catch(System.Type type, System.Linq.Expressions.Expression body) { return default(System.Linq.Expressions.CatchBlock); }
        public static System.Linq.Expressions.CatchBlock Catch(System.Type type, System.Linq.Expressions.Expression body, System.Linq.Expressions.Expression filter) { return default(System.Linq.Expressions.CatchBlock); }
        public static System.Linq.Expressions.DebugInfoExpression ClearDebugInfo(System.Linq.Expressions.SymbolDocumentInfo document) { return default(System.Linq.Expressions.DebugInfoExpression); }
        public static System.Linq.Expressions.BinaryExpression Coalesce(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right) { return default(System.Linq.Expressions.BinaryExpression); }
        public static System.Linq.Expressions.BinaryExpression Coalesce(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right, System.Linq.Expressions.LambdaExpression conversion) { return default(System.Linq.Expressions.BinaryExpression); }
        public static System.Linq.Expressions.ConditionalExpression Condition(System.Linq.Expressions.Expression test, System.Linq.Expressions.Expression ifTrue, System.Linq.Expressions.Expression ifFalse) { return default(System.Linq.Expressions.ConditionalExpression); }
        public static System.Linq.Expressions.ConditionalExpression Condition(System.Linq.Expressions.Expression test, System.Linq.Expressions.Expression ifTrue, System.Linq.Expressions.Expression ifFalse, System.Type type) { return default(System.Linq.Expressions.ConditionalExpression); }
        public static System.Linq.Expressions.ConstantExpression Constant(object value) { return default(System.Linq.Expressions.ConstantExpression); }
        public static System.Linq.Expressions.ConstantExpression Constant(object value, System.Type type) { return default(System.Linq.Expressions.ConstantExpression); }
        public static System.Linq.Expressions.GotoExpression Continue(System.Linq.Expressions.LabelTarget target) { return default(System.Linq.Expressions.GotoExpression); }
        public static System.Linq.Expressions.GotoExpression Continue(System.Linq.Expressions.LabelTarget target, System.Type type) { return default(System.Linq.Expressions.GotoExpression); }
        public static System.Linq.Expressions.UnaryExpression Convert(System.Linq.Expressions.Expression expression, System.Type type) { return default(System.Linq.Expressions.UnaryExpression); }
        public static System.Linq.Expressions.UnaryExpression Convert(System.Linq.Expressions.Expression expression, System.Type type, System.Reflection.MethodInfo method) { return default(System.Linq.Expressions.UnaryExpression); }
        public static System.Linq.Expressions.UnaryExpression ConvertChecked(System.Linq.Expressions.Expression expression, System.Type type) { return default(System.Linq.Expressions.UnaryExpression); }
        public static System.Linq.Expressions.UnaryExpression ConvertChecked(System.Linq.Expressions.Expression expression, System.Type type, System.Reflection.MethodInfo method) { return default(System.Linq.Expressions.UnaryExpression); }
        public static System.Linq.Expressions.DebugInfoExpression DebugInfo(System.Linq.Expressions.SymbolDocumentInfo document, int startLine, int startColumn, int endLine, int endColumn) { return default(System.Linq.Expressions.DebugInfoExpression); }
        public static System.Linq.Expressions.UnaryExpression Decrement(System.Linq.Expressions.Expression expression) { return default(System.Linq.Expressions.UnaryExpression); }
        public static System.Linq.Expressions.UnaryExpression Decrement(System.Linq.Expressions.Expression expression, System.Reflection.MethodInfo method) { return default(System.Linq.Expressions.UnaryExpression); }
        public static System.Linq.Expressions.DefaultExpression Default(System.Type type) { return default(System.Linq.Expressions.DefaultExpression); }
        public static System.Linq.Expressions.BinaryExpression Divide(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right) { return default(System.Linq.Expressions.BinaryExpression); }
        public static System.Linq.Expressions.BinaryExpression Divide(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right, System.Reflection.MethodInfo method) { return default(System.Linq.Expressions.BinaryExpression); }
        public static System.Linq.Expressions.BinaryExpression DivideAssign(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right) { return default(System.Linq.Expressions.BinaryExpression); }
        public static System.Linq.Expressions.BinaryExpression DivideAssign(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right, System.Reflection.MethodInfo method) { return default(System.Linq.Expressions.BinaryExpression); }
        public static System.Linq.Expressions.BinaryExpression DivideAssign(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right, System.Reflection.MethodInfo method, System.Linq.Expressions.LambdaExpression conversion) { return default(System.Linq.Expressions.BinaryExpression); }
        public static System.Linq.Expressions.ElementInit ElementInit(System.Reflection.MethodInfo addMethod, System.Collections.Generic.IEnumerable<System.Linq.Expressions.Expression> arguments) { return default(System.Linq.Expressions.ElementInit); }
        public static System.Linq.Expressions.ElementInit ElementInit(System.Reflection.MethodInfo addMethod, params System.Linq.Expressions.Expression[] arguments) { return default(System.Linq.Expressions.ElementInit); }
        public static System.Linq.Expressions.DefaultExpression Empty() { return default(System.Linq.Expressions.DefaultExpression); }
        public static System.Linq.Expressions.BinaryExpression Equal(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right) { return default(System.Linq.Expressions.BinaryExpression); }
        public static System.Linq.Expressions.BinaryExpression Equal(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right, bool liftToNull, System.Reflection.MethodInfo method) { return default(System.Linq.Expressions.BinaryExpression); }
        public static System.Linq.Expressions.BinaryExpression ExclusiveOr(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right) { return default(System.Linq.Expressions.BinaryExpression); }
        public static System.Linq.Expressions.BinaryExpression ExclusiveOr(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right, System.Reflection.MethodInfo method) { return default(System.Linq.Expressions.BinaryExpression); }
        public static System.Linq.Expressions.BinaryExpression ExclusiveOrAssign(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right) { return default(System.Linq.Expressions.BinaryExpression); }
        public static System.Linq.Expressions.BinaryExpression ExclusiveOrAssign(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right, System.Reflection.MethodInfo method) { return default(System.Linq.Expressions.BinaryExpression); }
        public static System.Linq.Expressions.BinaryExpression ExclusiveOrAssign(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right, System.Reflection.MethodInfo method, System.Linq.Expressions.LambdaExpression conversion) { return default(System.Linq.Expressions.BinaryExpression); }
        public static System.Linq.Expressions.MemberExpression Field(System.Linq.Expressions.Expression expression, System.Reflection.FieldInfo field) { return default(System.Linq.Expressions.MemberExpression); }
        public static System.Linq.Expressions.MemberExpression Field(System.Linq.Expressions.Expression expression, string fieldName) { return default(System.Linq.Expressions.MemberExpression); }
        public static System.Linq.Expressions.MemberExpression Field(System.Linq.Expressions.Expression expression, System.Type type, string fieldName) { return default(System.Linq.Expressions.MemberExpression); }
        public static System.Type GetActionType(params System.Type[] typeArgs) { return default(System.Type); }
        public static System.Type GetDelegateType(params System.Type[] typeArgs) { return default(System.Type); }
        public static System.Type GetFuncType(params System.Type[] typeArgs) { return default(System.Type); }
        public static System.Linq.Expressions.GotoExpression Goto(System.Linq.Expressions.LabelTarget target) { return default(System.Linq.Expressions.GotoExpression); }
        public static System.Linq.Expressions.GotoExpression Goto(System.Linq.Expressions.LabelTarget target, System.Linq.Expressions.Expression value) { return default(System.Linq.Expressions.GotoExpression); }
        public static System.Linq.Expressions.GotoExpression Goto(System.Linq.Expressions.LabelTarget target, System.Linq.Expressions.Expression value, System.Type type) { return default(System.Linq.Expressions.GotoExpression); }
        public static System.Linq.Expressions.GotoExpression Goto(System.Linq.Expressions.LabelTarget target, System.Type type) { return default(System.Linq.Expressions.GotoExpression); }
        public static System.Linq.Expressions.BinaryExpression GreaterThan(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right) { return default(System.Linq.Expressions.BinaryExpression); }
        public static System.Linq.Expressions.BinaryExpression GreaterThan(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right, bool liftToNull, System.Reflection.MethodInfo method) { return default(System.Linq.Expressions.BinaryExpression); }
        public static System.Linq.Expressions.BinaryExpression GreaterThanOrEqual(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right) { return default(System.Linq.Expressions.BinaryExpression); }
        public static System.Linq.Expressions.BinaryExpression GreaterThanOrEqual(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right, bool liftToNull, System.Reflection.MethodInfo method) { return default(System.Linq.Expressions.BinaryExpression); }
        public static System.Linq.Expressions.ConditionalExpression IfThen(System.Linq.Expressions.Expression test, System.Linq.Expressions.Expression ifTrue) { return default(System.Linq.Expressions.ConditionalExpression); }
        public static System.Linq.Expressions.ConditionalExpression IfThenElse(System.Linq.Expressions.Expression test, System.Linq.Expressions.Expression ifTrue, System.Linq.Expressions.Expression ifFalse) { return default(System.Linq.Expressions.ConditionalExpression); }
        public static System.Linq.Expressions.UnaryExpression Increment(System.Linq.Expressions.Expression expression) { return default(System.Linq.Expressions.UnaryExpression); }
        public static System.Linq.Expressions.UnaryExpression Increment(System.Linq.Expressions.Expression expression, System.Reflection.MethodInfo method) { return default(System.Linq.Expressions.UnaryExpression); }
        public static System.Linq.Expressions.InvocationExpression Invoke(System.Linq.Expressions.Expression expression, System.Collections.Generic.IEnumerable<System.Linq.Expressions.Expression> arguments) { return default(System.Linq.Expressions.InvocationExpression); }
        public static System.Linq.Expressions.InvocationExpression Invoke(System.Linq.Expressions.Expression expression, params System.Linq.Expressions.Expression[] arguments) { return default(System.Linq.Expressions.InvocationExpression); }
        public static System.Linq.Expressions.UnaryExpression IsFalse(System.Linq.Expressions.Expression expression) { return default(System.Linq.Expressions.UnaryExpression); }
        public static System.Linq.Expressions.UnaryExpression IsFalse(System.Linq.Expressions.Expression expression, System.Reflection.MethodInfo method) { return default(System.Linq.Expressions.UnaryExpression); }
        public static System.Linq.Expressions.UnaryExpression IsTrue(System.Linq.Expressions.Expression expression) { return default(System.Linq.Expressions.UnaryExpression); }
        public static System.Linq.Expressions.UnaryExpression IsTrue(System.Linq.Expressions.Expression expression, System.Reflection.MethodInfo method) { return default(System.Linq.Expressions.UnaryExpression); }
        public static System.Linq.Expressions.LabelTarget Label() { return default(System.Linq.Expressions.LabelTarget); }
        public static System.Linq.Expressions.LabelExpression Label(System.Linq.Expressions.LabelTarget target) { return default(System.Linq.Expressions.LabelExpression); }
        public static System.Linq.Expressions.LabelExpression Label(System.Linq.Expressions.LabelTarget target, System.Linq.Expressions.Expression defaultValue) { return default(System.Linq.Expressions.LabelExpression); }
        public static System.Linq.Expressions.LabelTarget Label(string name) { return default(System.Linq.Expressions.LabelTarget); }
        public static System.Linq.Expressions.LabelTarget Label(System.Type type) { return default(System.Linq.Expressions.LabelTarget); }
        public static System.Linq.Expressions.LabelTarget Label(System.Type type, string name) { return default(System.Linq.Expressions.LabelTarget); }
        public static System.Linq.Expressions.LambdaExpression Lambda(System.Linq.Expressions.Expression body, bool tailCall, System.Collections.Generic.IEnumerable<System.Linq.Expressions.ParameterExpression> parameters) { return default(System.Linq.Expressions.LambdaExpression); }
        public static System.Linq.Expressions.LambdaExpression Lambda(System.Linq.Expressions.Expression body, bool tailCall, params System.Linq.Expressions.ParameterExpression[] parameters) { return default(System.Linq.Expressions.LambdaExpression); }
        public static System.Linq.Expressions.LambdaExpression Lambda(System.Linq.Expressions.Expression body, System.Collections.Generic.IEnumerable<System.Linq.Expressions.ParameterExpression> parameters) { return default(System.Linq.Expressions.LambdaExpression); }
        public static System.Linq.Expressions.LambdaExpression Lambda(System.Linq.Expressions.Expression body, params System.Linq.Expressions.ParameterExpression[] parameters) { return default(System.Linq.Expressions.LambdaExpression); }
        public static System.Linq.Expressions.LambdaExpression Lambda(System.Linq.Expressions.Expression body, string name, bool tailCall, System.Collections.Generic.IEnumerable<System.Linq.Expressions.ParameterExpression> parameters) { return default(System.Linq.Expressions.LambdaExpression); }
        public static System.Linq.Expressions.LambdaExpression Lambda(System.Linq.Expressions.Expression body, string name, System.Collections.Generic.IEnumerable<System.Linq.Expressions.ParameterExpression> parameters) { return default(System.Linq.Expressions.LambdaExpression); }
        public static System.Linq.Expressions.LambdaExpression Lambda(System.Type delegateType, System.Linq.Expressions.Expression body, bool tailCall, System.Collections.Generic.IEnumerable<System.Linq.Expressions.ParameterExpression> parameters) { return default(System.Linq.Expressions.LambdaExpression); }
        public static System.Linq.Expressions.LambdaExpression Lambda(System.Type delegateType, System.Linq.Expressions.Expression body, bool tailCall, params System.Linq.Expressions.ParameterExpression[] parameters) { return default(System.Linq.Expressions.LambdaExpression); }
        public static System.Linq.Expressions.LambdaExpression Lambda(System.Type delegateType, System.Linq.Expressions.Expression body, System.Collections.Generic.IEnumerable<System.Linq.Expressions.ParameterExpression> parameters) { return default(System.Linq.Expressions.LambdaExpression); }
        public static System.Linq.Expressions.LambdaExpression Lambda(System.Type delegateType, System.Linq.Expressions.Expression body, params System.Linq.Expressions.ParameterExpression[] parameters) { return default(System.Linq.Expressions.LambdaExpression); }
        public static System.Linq.Expressions.LambdaExpression Lambda(System.Type delegateType, System.Linq.Expressions.Expression body, string name, bool tailCall, System.Collections.Generic.IEnumerable<System.Linq.Expressions.ParameterExpression> parameters) { return default(System.Linq.Expressions.LambdaExpression); }
        public static System.Linq.Expressions.LambdaExpression Lambda(System.Type delegateType, System.Linq.Expressions.Expression body, string name, System.Collections.Generic.IEnumerable<System.Linq.Expressions.ParameterExpression> parameters) { return default(System.Linq.Expressions.LambdaExpression); }
        public static System.Linq.Expressions.Expression<TDelegate> Lambda<TDelegate>(System.Linq.Expressions.Expression body, bool tailCall, System.Collections.Generic.IEnumerable<System.Linq.Expressions.ParameterExpression> parameters) { return default(System.Linq.Expressions.Expression<TDelegate>); }
        public static System.Linq.Expressions.Expression<TDelegate> Lambda<TDelegate>(System.Linq.Expressions.Expression body, bool tailCall, params System.Linq.Expressions.ParameterExpression[] parameters) { return default(System.Linq.Expressions.Expression<TDelegate>); }
        public static System.Linq.Expressions.Expression<TDelegate> Lambda<TDelegate>(System.Linq.Expressions.Expression body, System.Collections.Generic.IEnumerable<System.Linq.Expressions.ParameterExpression> parameters) { return default(System.Linq.Expressions.Expression<TDelegate>); }
        public static System.Linq.Expressions.Expression<TDelegate> Lambda<TDelegate>(System.Linq.Expressions.Expression body, params System.Linq.Expressions.ParameterExpression[] parameters) { return default(System.Linq.Expressions.Expression<TDelegate>); }
        public static System.Linq.Expressions.Expression<TDelegate> Lambda<TDelegate>(System.Linq.Expressions.Expression body, string name, bool tailCall, System.Collections.Generic.IEnumerable<System.Linq.Expressions.ParameterExpression> parameters) { return default(System.Linq.Expressions.Expression<TDelegate>); }
        public static System.Linq.Expressions.Expression<TDelegate> Lambda<TDelegate>(System.Linq.Expressions.Expression body, string name, System.Collections.Generic.IEnumerable<System.Linq.Expressions.ParameterExpression> parameters) { return default(System.Linq.Expressions.Expression<TDelegate>); }
        public static System.Linq.Expressions.BinaryExpression LeftShift(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right) { return default(System.Linq.Expressions.BinaryExpression); }
        public static System.Linq.Expressions.BinaryExpression LeftShift(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right, System.Reflection.MethodInfo method) { return default(System.Linq.Expressions.BinaryExpression); }
        public static System.Linq.Expressions.BinaryExpression LeftShiftAssign(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right) { return default(System.Linq.Expressions.BinaryExpression); }
        public static System.Linq.Expressions.BinaryExpression LeftShiftAssign(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right, System.Reflection.MethodInfo method) { return default(System.Linq.Expressions.BinaryExpression); }
        public static System.Linq.Expressions.BinaryExpression LeftShiftAssign(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right, System.Reflection.MethodInfo method, System.Linq.Expressions.LambdaExpression conversion) { return default(System.Linq.Expressions.BinaryExpression); }
        public static System.Linq.Expressions.BinaryExpression LessThan(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right) { return default(System.Linq.Expressions.BinaryExpression); }
        public static System.Linq.Expressions.BinaryExpression LessThan(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right, bool liftToNull, System.Reflection.MethodInfo method) { return default(System.Linq.Expressions.BinaryExpression); }
        public static System.Linq.Expressions.BinaryExpression LessThanOrEqual(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right) { return default(System.Linq.Expressions.BinaryExpression); }
        public static System.Linq.Expressions.BinaryExpression LessThanOrEqual(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right, bool liftToNull, System.Reflection.MethodInfo method) { return default(System.Linq.Expressions.BinaryExpression); }
        public static System.Linq.Expressions.MemberListBinding ListBind(System.Reflection.MemberInfo member, System.Collections.Generic.IEnumerable<System.Linq.Expressions.ElementInit> initializers) { return default(System.Linq.Expressions.MemberListBinding); }
        public static System.Linq.Expressions.MemberListBinding ListBind(System.Reflection.MemberInfo member, params System.Linq.Expressions.ElementInit[] initializers) { return default(System.Linq.Expressions.MemberListBinding); }
        public static System.Linq.Expressions.MemberListBinding ListBind(System.Reflection.MethodInfo propertyAccessor, System.Collections.Generic.IEnumerable<System.Linq.Expressions.ElementInit> initializers) { return default(System.Linq.Expressions.MemberListBinding); }
        public static System.Linq.Expressions.MemberListBinding ListBind(System.Reflection.MethodInfo propertyAccessor, params System.Linq.Expressions.ElementInit[] initializers) { return default(System.Linq.Expressions.MemberListBinding); }
        public static System.Linq.Expressions.ListInitExpression ListInit(System.Linq.Expressions.NewExpression newExpression, System.Collections.Generic.IEnumerable<System.Linq.Expressions.ElementInit> initializers) { return default(System.Linq.Expressions.ListInitExpression); }
        public static System.Linq.Expressions.ListInitExpression ListInit(System.Linq.Expressions.NewExpression newExpression, System.Collections.Generic.IEnumerable<System.Linq.Expressions.Expression> initializers) { return default(System.Linq.Expressions.ListInitExpression); }
        public static System.Linq.Expressions.ListInitExpression ListInit(System.Linq.Expressions.NewExpression newExpression, params System.Linq.Expressions.ElementInit[] initializers) { return default(System.Linq.Expressions.ListInitExpression); }
        public static System.Linq.Expressions.ListInitExpression ListInit(System.Linq.Expressions.NewExpression newExpression, params System.Linq.Expressions.Expression[] initializers) { return default(System.Linq.Expressions.ListInitExpression); }
        public static System.Linq.Expressions.ListInitExpression ListInit(System.Linq.Expressions.NewExpression newExpression, System.Reflection.MethodInfo addMethod, System.Collections.Generic.IEnumerable<System.Linq.Expressions.Expression> initializers) { return default(System.Linq.Expressions.ListInitExpression); }
        public static System.Linq.Expressions.ListInitExpression ListInit(System.Linq.Expressions.NewExpression newExpression, System.Reflection.MethodInfo addMethod, params System.Linq.Expressions.Expression[] initializers) { return default(System.Linq.Expressions.ListInitExpression); }
        public static System.Linq.Expressions.LoopExpression Loop(System.Linq.Expressions.Expression body) { return default(System.Linq.Expressions.LoopExpression); }
        public static System.Linq.Expressions.LoopExpression Loop(System.Linq.Expressions.Expression body, System.Linq.Expressions.LabelTarget @break) { return default(System.Linq.Expressions.LoopExpression); }
        public static System.Linq.Expressions.LoopExpression Loop(System.Linq.Expressions.Expression body, System.Linq.Expressions.LabelTarget @break, System.Linq.Expressions.LabelTarget @continue) { return default(System.Linq.Expressions.LoopExpression); }
        public static System.Linq.Expressions.BinaryExpression MakeBinary(System.Linq.Expressions.ExpressionType binaryType, System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right) { return default(System.Linq.Expressions.BinaryExpression); }
        public static System.Linq.Expressions.BinaryExpression MakeBinary(System.Linq.Expressions.ExpressionType binaryType, System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right, bool liftToNull, System.Reflection.MethodInfo method) { return default(System.Linq.Expressions.BinaryExpression); }
        public static System.Linq.Expressions.BinaryExpression MakeBinary(System.Linq.Expressions.ExpressionType binaryType, System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right, bool liftToNull, System.Reflection.MethodInfo method, System.Linq.Expressions.LambdaExpression conversion) { return default(System.Linq.Expressions.BinaryExpression); }
        public static System.Linq.Expressions.CatchBlock MakeCatchBlock(System.Type type, System.Linq.Expressions.ParameterExpression variable, System.Linq.Expressions.Expression body, System.Linq.Expressions.Expression filter) { return default(System.Linq.Expressions.CatchBlock); }
        public static System.Linq.Expressions.GotoExpression MakeGoto(System.Linq.Expressions.GotoExpressionKind kind, System.Linq.Expressions.LabelTarget target, System.Linq.Expressions.Expression value, System.Type type) { return default(System.Linq.Expressions.GotoExpression); }
        public static System.Linq.Expressions.IndexExpression MakeIndex(System.Linq.Expressions.Expression instance, System.Reflection.PropertyInfo indexer, System.Collections.Generic.IEnumerable<System.Linq.Expressions.Expression> arguments) { return default(System.Linq.Expressions.IndexExpression); }
        public static System.Linq.Expressions.MemberExpression MakeMemberAccess(System.Linq.Expressions.Expression expression, System.Reflection.MemberInfo member) { return default(System.Linq.Expressions.MemberExpression); }
        public static System.Linq.Expressions.TryExpression MakeTry(System.Type type, System.Linq.Expressions.Expression body, System.Linq.Expressions.Expression @finally, System.Linq.Expressions.Expression fault, System.Collections.Generic.IEnumerable<System.Linq.Expressions.CatchBlock> handlers) { return default(System.Linq.Expressions.TryExpression); }
        public static System.Linq.Expressions.UnaryExpression MakeUnary(System.Linq.Expressions.ExpressionType unaryType, System.Linq.Expressions.Expression operand, System.Type type) { return default(System.Linq.Expressions.UnaryExpression); }
        public static System.Linq.Expressions.UnaryExpression MakeUnary(System.Linq.Expressions.ExpressionType unaryType, System.Linq.Expressions.Expression operand, System.Type type, System.Reflection.MethodInfo method) { return default(System.Linq.Expressions.UnaryExpression); }
        public static System.Linq.Expressions.MemberMemberBinding MemberBind(System.Reflection.MemberInfo member, System.Collections.Generic.IEnumerable<System.Linq.Expressions.MemberBinding> bindings) { return default(System.Linq.Expressions.MemberMemberBinding); }
        public static System.Linq.Expressions.MemberMemberBinding MemberBind(System.Reflection.MemberInfo member, params System.Linq.Expressions.MemberBinding[] bindings) { return default(System.Linq.Expressions.MemberMemberBinding); }
        public static System.Linq.Expressions.MemberMemberBinding MemberBind(System.Reflection.MethodInfo propertyAccessor, System.Collections.Generic.IEnumerable<System.Linq.Expressions.MemberBinding> bindings) { return default(System.Linq.Expressions.MemberMemberBinding); }
        public static System.Linq.Expressions.MemberMemberBinding MemberBind(System.Reflection.MethodInfo propertyAccessor, params System.Linq.Expressions.MemberBinding[] bindings) { return default(System.Linq.Expressions.MemberMemberBinding); }
        public static System.Linq.Expressions.MemberInitExpression MemberInit(System.Linq.Expressions.NewExpression newExpression, System.Collections.Generic.IEnumerable<System.Linq.Expressions.MemberBinding> bindings) { return default(System.Linq.Expressions.MemberInitExpression); }
        public static System.Linq.Expressions.MemberInitExpression MemberInit(System.Linq.Expressions.NewExpression newExpression, params System.Linq.Expressions.MemberBinding[] bindings) { return default(System.Linq.Expressions.MemberInitExpression); }
        public static System.Linq.Expressions.BinaryExpression Modulo(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right) { return default(System.Linq.Expressions.BinaryExpression); }
        public static System.Linq.Expressions.BinaryExpression Modulo(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right, System.Reflection.MethodInfo method) { return default(System.Linq.Expressions.BinaryExpression); }
        public static System.Linq.Expressions.BinaryExpression ModuloAssign(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right) { return default(System.Linq.Expressions.BinaryExpression); }
        public static System.Linq.Expressions.BinaryExpression ModuloAssign(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right, System.Reflection.MethodInfo method) { return default(System.Linq.Expressions.BinaryExpression); }
        public static System.Linq.Expressions.BinaryExpression ModuloAssign(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right, System.Reflection.MethodInfo method, System.Linq.Expressions.LambdaExpression conversion) { return default(System.Linq.Expressions.BinaryExpression); }
        public static System.Linq.Expressions.BinaryExpression Multiply(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right) { return default(System.Linq.Expressions.BinaryExpression); }
        public static System.Linq.Expressions.BinaryExpression Multiply(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right, System.Reflection.MethodInfo method) { return default(System.Linq.Expressions.BinaryExpression); }
        public static System.Linq.Expressions.BinaryExpression MultiplyAssign(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right) { return default(System.Linq.Expressions.BinaryExpression); }
        public static System.Linq.Expressions.BinaryExpression MultiplyAssign(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right, System.Reflection.MethodInfo method) { return default(System.Linq.Expressions.BinaryExpression); }
        public static System.Linq.Expressions.BinaryExpression MultiplyAssign(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right, System.Reflection.MethodInfo method, System.Linq.Expressions.LambdaExpression conversion) { return default(System.Linq.Expressions.BinaryExpression); }
        public static System.Linq.Expressions.BinaryExpression MultiplyAssignChecked(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right) { return default(System.Linq.Expressions.BinaryExpression); }
        public static System.Linq.Expressions.BinaryExpression MultiplyAssignChecked(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right, System.Reflection.MethodInfo method) { return default(System.Linq.Expressions.BinaryExpression); }
        public static System.Linq.Expressions.BinaryExpression MultiplyAssignChecked(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right, System.Reflection.MethodInfo method, System.Linq.Expressions.LambdaExpression conversion) { return default(System.Linq.Expressions.BinaryExpression); }
        public static System.Linq.Expressions.BinaryExpression MultiplyChecked(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right) { return default(System.Linq.Expressions.BinaryExpression); }
        public static System.Linq.Expressions.BinaryExpression MultiplyChecked(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right, System.Reflection.MethodInfo method) { return default(System.Linq.Expressions.BinaryExpression); }
        public static System.Linq.Expressions.UnaryExpression Negate(System.Linq.Expressions.Expression expression) { return default(System.Linq.Expressions.UnaryExpression); }
        public static System.Linq.Expressions.UnaryExpression Negate(System.Linq.Expressions.Expression expression, System.Reflection.MethodInfo method) { return default(System.Linq.Expressions.UnaryExpression); }
        public static System.Linq.Expressions.UnaryExpression NegateChecked(System.Linq.Expressions.Expression expression) { return default(System.Linq.Expressions.UnaryExpression); }
        public static System.Linq.Expressions.UnaryExpression NegateChecked(System.Linq.Expressions.Expression expression, System.Reflection.MethodInfo method) { return default(System.Linq.Expressions.UnaryExpression); }
        public static System.Linq.Expressions.NewExpression New(System.Reflection.ConstructorInfo constructor) { return default(System.Linq.Expressions.NewExpression); }
        public static System.Linq.Expressions.NewExpression New(System.Reflection.ConstructorInfo constructor, System.Collections.Generic.IEnumerable<System.Linq.Expressions.Expression> arguments) { return default(System.Linq.Expressions.NewExpression); }
        public static System.Linq.Expressions.NewExpression New(System.Reflection.ConstructorInfo constructor, System.Collections.Generic.IEnumerable<System.Linq.Expressions.Expression> arguments, System.Collections.Generic.IEnumerable<System.Reflection.MemberInfo> members) { return default(System.Linq.Expressions.NewExpression); }
        public static System.Linq.Expressions.NewExpression New(System.Reflection.ConstructorInfo constructor, System.Collections.Generic.IEnumerable<System.Linq.Expressions.Expression> arguments, params System.Reflection.MemberInfo[] members) { return default(System.Linq.Expressions.NewExpression); }
        public static System.Linq.Expressions.NewExpression New(System.Reflection.ConstructorInfo constructor, params System.Linq.Expressions.Expression[] arguments) { return default(System.Linq.Expressions.NewExpression); }
        public static System.Linq.Expressions.NewExpression New(System.Type type) { return default(System.Linq.Expressions.NewExpression); }
        public static System.Linq.Expressions.NewArrayExpression NewArrayBounds(System.Type type, System.Collections.Generic.IEnumerable<System.Linq.Expressions.Expression> bounds) { return default(System.Linq.Expressions.NewArrayExpression); }
        public static System.Linq.Expressions.NewArrayExpression NewArrayBounds(System.Type type, params System.Linq.Expressions.Expression[] bounds) { return default(System.Linq.Expressions.NewArrayExpression); }
        public static System.Linq.Expressions.NewArrayExpression NewArrayInit(System.Type type, System.Collections.Generic.IEnumerable<System.Linq.Expressions.Expression> initializers) { return default(System.Linq.Expressions.NewArrayExpression); }
        public static System.Linq.Expressions.NewArrayExpression NewArrayInit(System.Type type, params System.Linq.Expressions.Expression[] initializers) { return default(System.Linq.Expressions.NewArrayExpression); }
        public static System.Linq.Expressions.UnaryExpression Not(System.Linq.Expressions.Expression expression) { return default(System.Linq.Expressions.UnaryExpression); }
        public static System.Linq.Expressions.UnaryExpression Not(System.Linq.Expressions.Expression expression, System.Reflection.MethodInfo method) { return default(System.Linq.Expressions.UnaryExpression); }
        public static System.Linq.Expressions.BinaryExpression NotEqual(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right) { return default(System.Linq.Expressions.BinaryExpression); }
        public static System.Linq.Expressions.BinaryExpression NotEqual(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right, bool liftToNull, System.Reflection.MethodInfo method) { return default(System.Linq.Expressions.BinaryExpression); }
        public static System.Linq.Expressions.UnaryExpression OnesComplement(System.Linq.Expressions.Expression expression) { return default(System.Linq.Expressions.UnaryExpression); }
        public static System.Linq.Expressions.UnaryExpression OnesComplement(System.Linq.Expressions.Expression expression, System.Reflection.MethodInfo method) { return default(System.Linq.Expressions.UnaryExpression); }
        public static System.Linq.Expressions.BinaryExpression Or(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right) { return default(System.Linq.Expressions.BinaryExpression); }
        public static System.Linq.Expressions.BinaryExpression Or(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right, System.Reflection.MethodInfo method) { return default(System.Linq.Expressions.BinaryExpression); }
        public static System.Linq.Expressions.BinaryExpression OrAssign(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right) { return default(System.Linq.Expressions.BinaryExpression); }
        public static System.Linq.Expressions.BinaryExpression OrAssign(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right, System.Reflection.MethodInfo method) { return default(System.Linq.Expressions.BinaryExpression); }
        public static System.Linq.Expressions.BinaryExpression OrAssign(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right, System.Reflection.MethodInfo method, System.Linq.Expressions.LambdaExpression conversion) { return default(System.Linq.Expressions.BinaryExpression); }
        public static System.Linq.Expressions.BinaryExpression OrElse(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right) { return default(System.Linq.Expressions.BinaryExpression); }
        public static System.Linq.Expressions.BinaryExpression OrElse(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right, System.Reflection.MethodInfo method) { return default(System.Linq.Expressions.BinaryExpression); }
        public static System.Linq.Expressions.ParameterExpression Parameter(System.Type type) { return default(System.Linq.Expressions.ParameterExpression); }
        public static System.Linq.Expressions.ParameterExpression Parameter(System.Type type, string name) { return default(System.Linq.Expressions.ParameterExpression); }
        public static System.Linq.Expressions.UnaryExpression PostDecrementAssign(System.Linq.Expressions.Expression expression) { return default(System.Linq.Expressions.UnaryExpression); }
        public static System.Linq.Expressions.UnaryExpression PostDecrementAssign(System.Linq.Expressions.Expression expression, System.Reflection.MethodInfo method) { return default(System.Linq.Expressions.UnaryExpression); }
        public static System.Linq.Expressions.UnaryExpression PostIncrementAssign(System.Linq.Expressions.Expression expression) { return default(System.Linq.Expressions.UnaryExpression); }
        public static System.Linq.Expressions.UnaryExpression PostIncrementAssign(System.Linq.Expressions.Expression expression, System.Reflection.MethodInfo method) { return default(System.Linq.Expressions.UnaryExpression); }
        public static System.Linq.Expressions.BinaryExpression Power(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right) { return default(System.Linq.Expressions.BinaryExpression); }
        public static System.Linq.Expressions.BinaryExpression Power(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right, System.Reflection.MethodInfo method) { return default(System.Linq.Expressions.BinaryExpression); }
        public static System.Linq.Expressions.BinaryExpression PowerAssign(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right) { return default(System.Linq.Expressions.BinaryExpression); }
        public static System.Linq.Expressions.BinaryExpression PowerAssign(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right, System.Reflection.MethodInfo method) { return default(System.Linq.Expressions.BinaryExpression); }
        public static System.Linq.Expressions.BinaryExpression PowerAssign(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right, System.Reflection.MethodInfo method, System.Linq.Expressions.LambdaExpression conversion) { return default(System.Linq.Expressions.BinaryExpression); }
        public static System.Linq.Expressions.UnaryExpression PreDecrementAssign(System.Linq.Expressions.Expression expression) { return default(System.Linq.Expressions.UnaryExpression); }
        public static System.Linq.Expressions.UnaryExpression PreDecrementAssign(System.Linq.Expressions.Expression expression, System.Reflection.MethodInfo method) { return default(System.Linq.Expressions.UnaryExpression); }
        public static System.Linq.Expressions.UnaryExpression PreIncrementAssign(System.Linq.Expressions.Expression expression) { return default(System.Linq.Expressions.UnaryExpression); }
        public static System.Linq.Expressions.UnaryExpression PreIncrementAssign(System.Linq.Expressions.Expression expression, System.Reflection.MethodInfo method) { return default(System.Linq.Expressions.UnaryExpression); }
        public static System.Linq.Expressions.MemberExpression Property(System.Linq.Expressions.Expression expression, System.Reflection.MethodInfo propertyAccessor) { return default(System.Linq.Expressions.MemberExpression); }
        public static System.Linq.Expressions.MemberExpression Property(System.Linq.Expressions.Expression expression, System.Reflection.PropertyInfo property) { return default(System.Linq.Expressions.MemberExpression); }
        public static System.Linq.Expressions.IndexExpression Property(System.Linq.Expressions.Expression instance, System.Reflection.PropertyInfo indexer, System.Collections.Generic.IEnumerable<System.Linq.Expressions.Expression> arguments) { return default(System.Linq.Expressions.IndexExpression); }
        public static System.Linq.Expressions.IndexExpression Property(System.Linq.Expressions.Expression instance, System.Reflection.PropertyInfo indexer, params System.Linq.Expressions.Expression[] arguments) { return default(System.Linq.Expressions.IndexExpression); }
        public static System.Linq.Expressions.MemberExpression Property(System.Linq.Expressions.Expression expression, string propertyName) { return default(System.Linq.Expressions.MemberExpression); }
        public static System.Linq.Expressions.IndexExpression Property(System.Linq.Expressions.Expression instance, string propertyName, params System.Linq.Expressions.Expression[] arguments) { return default(System.Linq.Expressions.IndexExpression); }
        public static System.Linq.Expressions.MemberExpression Property(System.Linq.Expressions.Expression expression, System.Type type, string propertyName) { return default(System.Linq.Expressions.MemberExpression); }
        public static System.Linq.Expressions.MemberExpression PropertyOrField(System.Linq.Expressions.Expression expression, string propertyOrFieldName) { return default(System.Linq.Expressions.MemberExpression); }
        public static System.Linq.Expressions.UnaryExpression Quote(System.Linq.Expressions.Expression expression) { return default(System.Linq.Expressions.UnaryExpression); }
        public virtual System.Linq.Expressions.Expression Reduce() { return default(System.Linq.Expressions.Expression); }
        public System.Linq.Expressions.Expression ReduceAndCheck() { return default(System.Linq.Expressions.Expression); }
        public System.Linq.Expressions.Expression ReduceExtensions() { return default(System.Linq.Expressions.Expression); }
        public static System.Linq.Expressions.BinaryExpression ReferenceEqual(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right) { return default(System.Linq.Expressions.BinaryExpression); }
        public static System.Linq.Expressions.BinaryExpression ReferenceNotEqual(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right) { return default(System.Linq.Expressions.BinaryExpression); }
        public static System.Linq.Expressions.UnaryExpression Rethrow() { return default(System.Linq.Expressions.UnaryExpression); }
        public static System.Linq.Expressions.UnaryExpression Rethrow(System.Type type) { return default(System.Linq.Expressions.UnaryExpression); }
        public static System.Linq.Expressions.GotoExpression Return(System.Linq.Expressions.LabelTarget target) { return default(System.Linq.Expressions.GotoExpression); }
        public static System.Linq.Expressions.GotoExpression Return(System.Linq.Expressions.LabelTarget target, System.Linq.Expressions.Expression value) { return default(System.Linq.Expressions.GotoExpression); }
        public static System.Linq.Expressions.GotoExpression Return(System.Linq.Expressions.LabelTarget target, System.Linq.Expressions.Expression value, System.Type type) { return default(System.Linq.Expressions.GotoExpression); }
        public static System.Linq.Expressions.GotoExpression Return(System.Linq.Expressions.LabelTarget target, System.Type type) { return default(System.Linq.Expressions.GotoExpression); }
        public static System.Linq.Expressions.BinaryExpression RightShift(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right) { return default(System.Linq.Expressions.BinaryExpression); }
        public static System.Linq.Expressions.BinaryExpression RightShift(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right, System.Reflection.MethodInfo method) { return default(System.Linq.Expressions.BinaryExpression); }
        public static System.Linq.Expressions.BinaryExpression RightShiftAssign(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right) { return default(System.Linq.Expressions.BinaryExpression); }
        public static System.Linq.Expressions.BinaryExpression RightShiftAssign(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right, System.Reflection.MethodInfo method) { return default(System.Linq.Expressions.BinaryExpression); }
        public static System.Linq.Expressions.BinaryExpression RightShiftAssign(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right, System.Reflection.MethodInfo method, System.Linq.Expressions.LambdaExpression conversion) { return default(System.Linq.Expressions.BinaryExpression); }
        public static System.Linq.Expressions.RuntimeVariablesExpression RuntimeVariables(System.Collections.Generic.IEnumerable<System.Linq.Expressions.ParameterExpression> variables) { return default(System.Linq.Expressions.RuntimeVariablesExpression); }
        public static System.Linq.Expressions.RuntimeVariablesExpression RuntimeVariables(params System.Linq.Expressions.ParameterExpression[] variables) { return default(System.Linq.Expressions.RuntimeVariablesExpression); }
        public static System.Linq.Expressions.BinaryExpression Subtract(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right) { return default(System.Linq.Expressions.BinaryExpression); }
        public static System.Linq.Expressions.BinaryExpression Subtract(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right, System.Reflection.MethodInfo method) { return default(System.Linq.Expressions.BinaryExpression); }
        public static System.Linq.Expressions.BinaryExpression SubtractAssign(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right) { return default(System.Linq.Expressions.BinaryExpression); }
        public static System.Linq.Expressions.BinaryExpression SubtractAssign(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right, System.Reflection.MethodInfo method) { return default(System.Linq.Expressions.BinaryExpression); }
        public static System.Linq.Expressions.BinaryExpression SubtractAssign(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right, System.Reflection.MethodInfo method, System.Linq.Expressions.LambdaExpression conversion) { return default(System.Linq.Expressions.BinaryExpression); }
        public static System.Linq.Expressions.BinaryExpression SubtractAssignChecked(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right) { return default(System.Linq.Expressions.BinaryExpression); }
        public static System.Linq.Expressions.BinaryExpression SubtractAssignChecked(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right, System.Reflection.MethodInfo method) { return default(System.Linq.Expressions.BinaryExpression); }
        public static System.Linq.Expressions.BinaryExpression SubtractAssignChecked(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right, System.Reflection.MethodInfo method, System.Linq.Expressions.LambdaExpression conversion) { return default(System.Linq.Expressions.BinaryExpression); }
        public static System.Linq.Expressions.BinaryExpression SubtractChecked(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right) { return default(System.Linq.Expressions.BinaryExpression); }
        public static System.Linq.Expressions.BinaryExpression SubtractChecked(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right, System.Reflection.MethodInfo method) { return default(System.Linq.Expressions.BinaryExpression); }
        public static System.Linq.Expressions.SwitchExpression Switch(System.Linq.Expressions.Expression switchValue, System.Linq.Expressions.Expression defaultBody, params System.Linq.Expressions.SwitchCase[] cases) { return default(System.Linq.Expressions.SwitchExpression); }
        public static System.Linq.Expressions.SwitchExpression Switch(System.Linq.Expressions.Expression switchValue, System.Linq.Expressions.Expression defaultBody, System.Reflection.MethodInfo comparison, System.Collections.Generic.IEnumerable<System.Linq.Expressions.SwitchCase> cases) { return default(System.Linq.Expressions.SwitchExpression); }
        public static System.Linq.Expressions.SwitchExpression Switch(System.Linq.Expressions.Expression switchValue, System.Linq.Expressions.Expression defaultBody, System.Reflection.MethodInfo comparison, params System.Linq.Expressions.SwitchCase[] cases) { return default(System.Linq.Expressions.SwitchExpression); }
        public static System.Linq.Expressions.SwitchExpression Switch(System.Linq.Expressions.Expression switchValue, params System.Linq.Expressions.SwitchCase[] cases) { return default(System.Linq.Expressions.SwitchExpression); }
        public static System.Linq.Expressions.SwitchExpression Switch(System.Type type, System.Linq.Expressions.Expression switchValue, System.Linq.Expressions.Expression defaultBody, System.Reflection.MethodInfo comparison, System.Collections.Generic.IEnumerable<System.Linq.Expressions.SwitchCase> cases) { return default(System.Linq.Expressions.SwitchExpression); }
        public static System.Linq.Expressions.SwitchExpression Switch(System.Type type, System.Linq.Expressions.Expression switchValue, System.Linq.Expressions.Expression defaultBody, System.Reflection.MethodInfo comparison, params System.Linq.Expressions.SwitchCase[] cases) { return default(System.Linq.Expressions.SwitchExpression); }
        public static System.Linq.Expressions.SwitchCase SwitchCase(System.Linq.Expressions.Expression body, System.Collections.Generic.IEnumerable<System.Linq.Expressions.Expression> testValues) { return default(System.Linq.Expressions.SwitchCase); }
        public static System.Linq.Expressions.SwitchCase SwitchCase(System.Linq.Expressions.Expression body, params System.Linq.Expressions.Expression[] testValues) { return default(System.Linq.Expressions.SwitchCase); }
        public static System.Linq.Expressions.SymbolDocumentInfo SymbolDocument(string fileName) { return default(System.Linq.Expressions.SymbolDocumentInfo); }
        public static System.Linq.Expressions.SymbolDocumentInfo SymbolDocument(string fileName, System.Guid language) { return default(System.Linq.Expressions.SymbolDocumentInfo); }
        public static System.Linq.Expressions.SymbolDocumentInfo SymbolDocument(string fileName, System.Guid language, System.Guid languageVendor) { return default(System.Linq.Expressions.SymbolDocumentInfo); }
        public static System.Linq.Expressions.SymbolDocumentInfo SymbolDocument(string fileName, System.Guid language, System.Guid languageVendor, System.Guid documentType) { return default(System.Linq.Expressions.SymbolDocumentInfo); }
        public static System.Linq.Expressions.UnaryExpression Throw(System.Linq.Expressions.Expression value) { return default(System.Linq.Expressions.UnaryExpression); }
        public static System.Linq.Expressions.UnaryExpression Throw(System.Linq.Expressions.Expression value, System.Type type) { return default(System.Linq.Expressions.UnaryExpression); }
        public override string ToString() { return default(string); }
        public static System.Linq.Expressions.TryExpression TryCatch(System.Linq.Expressions.Expression body, params System.Linq.Expressions.CatchBlock[] handlers) { return default(System.Linq.Expressions.TryExpression); }
        public static System.Linq.Expressions.TryExpression TryCatchFinally(System.Linq.Expressions.Expression body, System.Linq.Expressions.Expression @finally, params System.Linq.Expressions.CatchBlock[] handlers) { return default(System.Linq.Expressions.TryExpression); }
        public static System.Linq.Expressions.TryExpression TryFault(System.Linq.Expressions.Expression body, System.Linq.Expressions.Expression fault) { return default(System.Linq.Expressions.TryExpression); }
        public static System.Linq.Expressions.TryExpression TryFinally(System.Linq.Expressions.Expression body, System.Linq.Expressions.Expression @finally) { return default(System.Linq.Expressions.TryExpression); }
        public static bool TryGetActionType(System.Type[] typeArgs, out System.Type actionType) { actionType = default(System.Type); return default(bool); }
        public static bool TryGetFuncType(System.Type[] typeArgs, out System.Type funcType) { funcType = default(System.Type); return default(bool); }
        public static System.Linq.Expressions.UnaryExpression TypeAs(System.Linq.Expressions.Expression expression, System.Type type) { return default(System.Linq.Expressions.UnaryExpression); }
        public static System.Linq.Expressions.TypeBinaryExpression TypeEqual(System.Linq.Expressions.Expression expression, System.Type type) { return default(System.Linq.Expressions.TypeBinaryExpression); }
        public static System.Linq.Expressions.TypeBinaryExpression TypeIs(System.Linq.Expressions.Expression expression, System.Type type) { return default(System.Linq.Expressions.TypeBinaryExpression); }
        public static System.Linq.Expressions.UnaryExpression UnaryPlus(System.Linq.Expressions.Expression expression) { return default(System.Linq.Expressions.UnaryExpression); }
        public static System.Linq.Expressions.UnaryExpression UnaryPlus(System.Linq.Expressions.Expression expression, System.Reflection.MethodInfo method) { return default(System.Linq.Expressions.UnaryExpression); }
        public static System.Linq.Expressions.UnaryExpression Unbox(System.Linq.Expressions.Expression expression, System.Type type) { return default(System.Linq.Expressions.UnaryExpression); }
        public static System.Linq.Expressions.ParameterExpression Variable(System.Type type) { return default(System.Linq.Expressions.ParameterExpression); }
        public static System.Linq.Expressions.ParameterExpression Variable(System.Type type, string name) { return default(System.Linq.Expressions.ParameterExpression); }
        protected internal virtual System.Linq.Expressions.Expression VisitChildren(System.Linq.Expressions.ExpressionVisitor visitor) { return default(System.Linq.Expressions.Expression); }
    }
    public sealed partial class Expression<TDelegate> : System.Linq.Expressions.LambdaExpression
    {
        internal Expression() { }
        protected internal override System.Linq.Expressions.Expression Accept(System.Linq.Expressions.ExpressionVisitor visitor) { return default(System.Linq.Expressions.Expression); }
        public new TDelegate Compile() { return default(TDelegate); }
        public new TDelegate Compile(bool preferInterpretation) { return default(TDelegate); }
        public System.Linq.Expressions.Expression<TDelegate> Update(System.Linq.Expressions.Expression body, System.Collections.Generic.IEnumerable<System.Linq.Expressions.ParameterExpression> parameters) { return default(System.Linq.Expressions.Expression<TDelegate>); }
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
        public System.Collections.ObjectModel.ReadOnlyCollection<System.Linq.Expressions.Expression> Visit(System.Collections.ObjectModel.ReadOnlyCollection<System.Linq.Expressions.Expression> nodes) { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Linq.Expressions.Expression>); }
        public virtual System.Linq.Expressions.Expression Visit(System.Linq.Expressions.Expression node) { return default(System.Linq.Expressions.Expression); }
        public static System.Collections.ObjectModel.ReadOnlyCollection<T> Visit<T>(System.Collections.ObjectModel.ReadOnlyCollection<T> nodes, System.Func<T, T> elementVisitor) { return default(System.Collections.ObjectModel.ReadOnlyCollection<T>); }
        public T VisitAndConvert<T>(T node, string callerName) where T : System.Linq.Expressions.Expression { return default(T); }
        public System.Collections.ObjectModel.ReadOnlyCollection<T> VisitAndConvert<T>(System.Collections.ObjectModel.ReadOnlyCollection<T> nodes, string callerName) where T : System.Linq.Expressions.Expression { return default(System.Collections.ObjectModel.ReadOnlyCollection<T>); }
        protected internal virtual System.Linq.Expressions.Expression VisitBinary(System.Linq.Expressions.BinaryExpression node) { return default(System.Linq.Expressions.Expression); }
        protected internal virtual System.Linq.Expressions.Expression VisitBlock(System.Linq.Expressions.BlockExpression node) { return default(System.Linq.Expressions.Expression); }
        protected virtual System.Linq.Expressions.CatchBlock VisitCatchBlock(System.Linq.Expressions.CatchBlock node) { return default(System.Linq.Expressions.CatchBlock); }
        protected internal virtual System.Linq.Expressions.Expression VisitConditional(System.Linq.Expressions.ConditionalExpression node) { return default(System.Linq.Expressions.Expression); }
        protected internal virtual System.Linq.Expressions.Expression VisitConstant(System.Linq.Expressions.ConstantExpression node) { return default(System.Linq.Expressions.Expression); }
        protected internal virtual System.Linq.Expressions.Expression VisitDebugInfo(System.Linq.Expressions.DebugInfoExpression node) { return default(System.Linq.Expressions.Expression); }
        protected internal virtual System.Linq.Expressions.Expression VisitDefault(System.Linq.Expressions.DefaultExpression node) { return default(System.Linq.Expressions.Expression); }
        protected virtual System.Linq.Expressions.ElementInit VisitElementInit(System.Linq.Expressions.ElementInit node) { return default(System.Linq.Expressions.ElementInit); }
        protected internal virtual System.Linq.Expressions.Expression VisitExtension(System.Linq.Expressions.Expression node) { return default(System.Linq.Expressions.Expression); }
        protected internal virtual System.Linq.Expressions.Expression VisitGoto(System.Linq.Expressions.GotoExpression node) { return default(System.Linq.Expressions.Expression); }
        protected internal virtual System.Linq.Expressions.Expression VisitIndex(System.Linq.Expressions.IndexExpression node) { return default(System.Linq.Expressions.Expression); }
        protected internal virtual System.Linq.Expressions.Expression VisitInvocation(System.Linq.Expressions.InvocationExpression node) { return default(System.Linq.Expressions.Expression); }
        protected internal virtual System.Linq.Expressions.Expression VisitLabel(System.Linq.Expressions.LabelExpression node) { return default(System.Linq.Expressions.Expression); }
        protected virtual System.Linq.Expressions.LabelTarget VisitLabelTarget(System.Linq.Expressions.LabelTarget node) { return default(System.Linq.Expressions.LabelTarget); }
        protected internal virtual System.Linq.Expressions.Expression VisitLambda<T>(System.Linq.Expressions.Expression<T> node) { return default(System.Linq.Expressions.Expression); }
        protected internal virtual System.Linq.Expressions.Expression VisitListInit(System.Linq.Expressions.ListInitExpression node) { return default(System.Linq.Expressions.Expression); }
        protected internal virtual System.Linq.Expressions.Expression VisitLoop(System.Linq.Expressions.LoopExpression node) { return default(System.Linq.Expressions.Expression); }
        protected internal virtual System.Linq.Expressions.Expression VisitMember(System.Linq.Expressions.MemberExpression node) { return default(System.Linq.Expressions.Expression); }
        protected virtual System.Linq.Expressions.MemberAssignment VisitMemberAssignment(System.Linq.Expressions.MemberAssignment node) { return default(System.Linq.Expressions.MemberAssignment); }
        protected virtual System.Linq.Expressions.MemberBinding VisitMemberBinding(System.Linq.Expressions.MemberBinding node) { return default(System.Linq.Expressions.MemberBinding); }
        protected internal virtual System.Linq.Expressions.Expression VisitMemberInit(System.Linq.Expressions.MemberInitExpression node) { return default(System.Linq.Expressions.Expression); }
        protected virtual System.Linq.Expressions.MemberListBinding VisitMemberListBinding(System.Linq.Expressions.MemberListBinding node) { return default(System.Linq.Expressions.MemberListBinding); }
        protected virtual System.Linq.Expressions.MemberMemberBinding VisitMemberMemberBinding(System.Linq.Expressions.MemberMemberBinding node) { return default(System.Linq.Expressions.MemberMemberBinding); }
        protected internal virtual System.Linq.Expressions.Expression VisitMethodCall(System.Linq.Expressions.MethodCallExpression node) { return default(System.Linq.Expressions.Expression); }
        protected internal virtual System.Linq.Expressions.Expression VisitNew(System.Linq.Expressions.NewExpression node) { return default(System.Linq.Expressions.Expression); }
        protected internal virtual System.Linq.Expressions.Expression VisitNewArray(System.Linq.Expressions.NewArrayExpression node) { return default(System.Linq.Expressions.Expression); }
        protected internal virtual System.Linq.Expressions.Expression VisitParameter(System.Linq.Expressions.ParameterExpression node) { return default(System.Linq.Expressions.Expression); }
        protected internal virtual System.Linq.Expressions.Expression VisitRuntimeVariables(System.Linq.Expressions.RuntimeVariablesExpression node) { return default(System.Linq.Expressions.Expression); }
        protected internal virtual System.Linq.Expressions.Expression VisitSwitch(System.Linq.Expressions.SwitchExpression node) { return default(System.Linq.Expressions.Expression); }
        protected virtual System.Linq.Expressions.SwitchCase VisitSwitchCase(System.Linq.Expressions.SwitchCase node) { return default(System.Linq.Expressions.SwitchCase); }
        protected internal virtual System.Linq.Expressions.Expression VisitTry(System.Linq.Expressions.TryExpression node) { return default(System.Linq.Expressions.Expression); }
        protected internal virtual System.Linq.Expressions.Expression VisitTypeBinary(System.Linq.Expressions.TypeBinaryExpression node) { return default(System.Linq.Expressions.Expression); }
        protected internal virtual System.Linq.Expressions.Expression VisitUnary(System.Linq.Expressions.UnaryExpression node) { return default(System.Linq.Expressions.Expression); }
    }
    public sealed partial class GotoExpression : System.Linq.Expressions.Expression
    {
        internal GotoExpression() { }
        public System.Linq.Expressions.GotoExpressionKind Kind { get { return default(System.Linq.Expressions.GotoExpressionKind); } }
        public sealed override System.Linq.Expressions.ExpressionType NodeType { get { return default(System.Linq.Expressions.ExpressionType); } }
        public System.Linq.Expressions.LabelTarget Target { get { return default(System.Linq.Expressions.LabelTarget); } }
        public sealed override System.Type Type { get { return default(System.Type); } }
        public System.Linq.Expressions.Expression Value { get { return default(System.Linq.Expressions.Expression); } }
        protected internal override System.Linq.Expressions.Expression Accept(System.Linq.Expressions.ExpressionVisitor visitor) { return default(System.Linq.Expressions.Expression); }
        public System.Linq.Expressions.GotoExpression Update(System.Linq.Expressions.LabelTarget target, System.Linq.Expressions.Expression value) { return default(System.Linq.Expressions.GotoExpression); }
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
        public System.Collections.ObjectModel.ReadOnlyCollection<System.Linq.Expressions.Expression> Arguments { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Linq.Expressions.Expression>); } }
        public System.Reflection.PropertyInfo Indexer { get { return default(System.Reflection.PropertyInfo); } }
        public sealed override System.Linq.Expressions.ExpressionType NodeType { get { return default(System.Linq.Expressions.ExpressionType); } }
        public System.Linq.Expressions.Expression Object { get { return default(System.Linq.Expressions.Expression); } }
        int System.Linq.Expressions.IArgumentProvider.ArgumentCount { get { return default(int); } }
        public sealed override System.Type Type { get { return default(System.Type); } }
        protected internal override System.Linq.Expressions.Expression Accept(System.Linq.Expressions.ExpressionVisitor visitor) { return default(System.Linq.Expressions.Expression); }
        System.Linq.Expressions.Expression System.Linq.Expressions.IArgumentProvider.GetArgument(int index) { return default(System.Linq.Expressions.Expression); }
        public System.Linq.Expressions.IndexExpression Update(System.Linq.Expressions.Expression @object, System.Collections.Generic.IEnumerable<System.Linq.Expressions.Expression> arguments) { return default(System.Linq.Expressions.IndexExpression); }
    }
    public sealed partial class InvocationExpression : System.Linq.Expressions.Expression, System.Linq.Expressions.IArgumentProvider
    {
        internal InvocationExpression() { }
        public System.Collections.ObjectModel.ReadOnlyCollection<System.Linq.Expressions.Expression> Arguments { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Linq.Expressions.Expression>); } }
        public System.Linq.Expressions.Expression Expression { get { return default(System.Linq.Expressions.Expression); } }
        public sealed override System.Linq.Expressions.ExpressionType NodeType { get { return default(System.Linq.Expressions.ExpressionType); } }
        int System.Linq.Expressions.IArgumentProvider.ArgumentCount { get { return default(int); } }
        public sealed override System.Type Type { get { return default(System.Type); } }
        protected internal override System.Linq.Expressions.Expression Accept(System.Linq.Expressions.ExpressionVisitor visitor) { return default(System.Linq.Expressions.Expression); }
        System.Linq.Expressions.Expression System.Linq.Expressions.IArgumentProvider.GetArgument(int index) { return default(System.Linq.Expressions.Expression); }
        public System.Linq.Expressions.InvocationExpression Update(System.Linq.Expressions.Expression expression, System.Collections.Generic.IEnumerable<System.Linq.Expressions.Expression> arguments) { return default(System.Linq.Expressions.InvocationExpression); }
    }
    public sealed partial class LabelExpression : System.Linq.Expressions.Expression
    {
        internal LabelExpression() { }
        public System.Linq.Expressions.Expression DefaultValue { get { return default(System.Linq.Expressions.Expression); } }
        public sealed override System.Linq.Expressions.ExpressionType NodeType { get { return default(System.Linq.Expressions.ExpressionType); } }
        public System.Linq.Expressions.LabelTarget Target { get { return default(System.Linq.Expressions.LabelTarget); } }
        public sealed override System.Type Type { get { return default(System.Type); } }
        protected internal override System.Linq.Expressions.Expression Accept(System.Linq.Expressions.ExpressionVisitor visitor) { return default(System.Linq.Expressions.Expression); }
        public System.Linq.Expressions.LabelExpression Update(System.Linq.Expressions.LabelTarget target, System.Linq.Expressions.Expression defaultValue) { return default(System.Linq.Expressions.LabelExpression); }
    }
    public sealed partial class LabelTarget
    {
        internal LabelTarget() { }
        public string Name { get { return default(string); } }
        public System.Type Type { get { return default(System.Type); } }
        public override string ToString() { return default(string); }
    }
    public abstract partial class LambdaExpression : System.Linq.Expressions.Expression
    {
        internal LambdaExpression() { }
        public System.Linq.Expressions.Expression Body { get { return default(System.Linq.Expressions.Expression); } }
        public string Name { get { return default(string); } }
        public sealed override System.Linq.Expressions.ExpressionType NodeType { get { return default(System.Linq.Expressions.ExpressionType); } }
        public System.Collections.ObjectModel.ReadOnlyCollection<System.Linq.Expressions.ParameterExpression> Parameters { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Linq.Expressions.ParameterExpression>); } }
        public System.Type ReturnType { get { return default(System.Type); } }
        public bool TailCall { get { return default(bool); } }
        public sealed override System.Type Type { get { return default(System.Type); } }
        public System.Delegate Compile() { return default(System.Delegate); }
        public System.Delegate Compile(bool preferInterpretation) { return default(System.Delegate); }
    }
    public sealed partial class ListInitExpression : System.Linq.Expressions.Expression
    {
        internal ListInitExpression() { }
        public override bool CanReduce { get { return default(bool); } }
        public System.Collections.ObjectModel.ReadOnlyCollection<System.Linq.Expressions.ElementInit> Initializers { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Linq.Expressions.ElementInit>); } }
        public System.Linq.Expressions.NewExpression NewExpression { get { return default(System.Linq.Expressions.NewExpression); } }
        public sealed override System.Linq.Expressions.ExpressionType NodeType { get { return default(System.Linq.Expressions.ExpressionType); } }
        public sealed override System.Type Type { get { return default(System.Type); } }
        protected internal override System.Linq.Expressions.Expression Accept(System.Linq.Expressions.ExpressionVisitor visitor) { return default(System.Linq.Expressions.Expression); }
        public override System.Linq.Expressions.Expression Reduce() { return default(System.Linq.Expressions.Expression); }
        public System.Linq.Expressions.ListInitExpression Update(System.Linq.Expressions.NewExpression newExpression, System.Collections.Generic.IEnumerable<System.Linq.Expressions.ElementInit> initializers) { return default(System.Linq.Expressions.ListInitExpression); }
    }
    public sealed partial class LoopExpression : System.Linq.Expressions.Expression
    {
        internal LoopExpression() { }
        public System.Linq.Expressions.Expression Body { get { return default(System.Linq.Expressions.Expression); } }
        public System.Linq.Expressions.LabelTarget BreakLabel { get { return default(System.Linq.Expressions.LabelTarget); } }
        public System.Linq.Expressions.LabelTarget ContinueLabel { get { return default(System.Linq.Expressions.LabelTarget); } }
        public sealed override System.Linq.Expressions.ExpressionType NodeType { get { return default(System.Linq.Expressions.ExpressionType); } }
        public sealed override System.Type Type { get { return default(System.Type); } }
        protected internal override System.Linq.Expressions.Expression Accept(System.Linq.Expressions.ExpressionVisitor visitor) { return default(System.Linq.Expressions.Expression); }
        public System.Linq.Expressions.LoopExpression Update(System.Linq.Expressions.LabelTarget breakLabel, System.Linq.Expressions.LabelTarget continueLabel, System.Linq.Expressions.Expression body) { return default(System.Linq.Expressions.LoopExpression); }
    }
    public sealed partial class MemberAssignment : System.Linq.Expressions.MemberBinding
    {
        internal MemberAssignment() { }
        public System.Linq.Expressions.Expression Expression { get { return default(System.Linq.Expressions.Expression); } }
        public System.Linq.Expressions.MemberAssignment Update(System.Linq.Expressions.Expression expression) { return default(System.Linq.Expressions.MemberAssignment); }
    }
    public abstract partial class MemberBinding
    {
        internal MemberBinding() { }
        public System.Linq.Expressions.MemberBindingType BindingType { get { return default(System.Linq.Expressions.MemberBindingType); } }
        public System.Reflection.MemberInfo Member { get { return default(System.Reflection.MemberInfo); } }
        public override string ToString() { return default(string); }
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
        public System.Linq.Expressions.Expression Expression { get { return default(System.Linq.Expressions.Expression); } }
        public System.Reflection.MemberInfo Member { get { return default(System.Reflection.MemberInfo); } }
        public sealed override System.Linq.Expressions.ExpressionType NodeType { get { return default(System.Linq.Expressions.ExpressionType); } }
        protected internal override System.Linq.Expressions.Expression Accept(System.Linq.Expressions.ExpressionVisitor visitor) { return default(System.Linq.Expressions.Expression); }
        public System.Linq.Expressions.MemberExpression Update(System.Linq.Expressions.Expression expression) { return default(System.Linq.Expressions.MemberExpression); }
    }
    public sealed partial class MemberInitExpression : System.Linq.Expressions.Expression
    {
        internal MemberInitExpression() { }
        public System.Collections.ObjectModel.ReadOnlyCollection<System.Linq.Expressions.MemberBinding> Bindings { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Linq.Expressions.MemberBinding>); } }
        public override bool CanReduce { get { return default(bool); } }
        public System.Linq.Expressions.NewExpression NewExpression { get { return default(System.Linq.Expressions.NewExpression); } }
        public sealed override System.Linq.Expressions.ExpressionType NodeType { get { return default(System.Linq.Expressions.ExpressionType); } }
        public sealed override System.Type Type { get { return default(System.Type); } }
        protected internal override System.Linq.Expressions.Expression Accept(System.Linq.Expressions.ExpressionVisitor visitor) { return default(System.Linq.Expressions.Expression); }
        public override System.Linq.Expressions.Expression Reduce() { return default(System.Linq.Expressions.Expression); }
        public System.Linq.Expressions.MemberInitExpression Update(System.Linq.Expressions.NewExpression newExpression, System.Collections.Generic.IEnumerable<System.Linq.Expressions.MemberBinding> bindings) { return default(System.Linq.Expressions.MemberInitExpression); }
    }
    public sealed partial class MemberListBinding : System.Linq.Expressions.MemberBinding
    {
        internal MemberListBinding() { }
        public System.Collections.ObjectModel.ReadOnlyCollection<System.Linq.Expressions.ElementInit> Initializers { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Linq.Expressions.ElementInit>); } }
        public System.Linq.Expressions.MemberListBinding Update(System.Collections.Generic.IEnumerable<System.Linq.Expressions.ElementInit> initializers) { return default(System.Linq.Expressions.MemberListBinding); }
    }
    public sealed partial class MemberMemberBinding : System.Linq.Expressions.MemberBinding
    {
        internal MemberMemberBinding() { }
        public System.Collections.ObjectModel.ReadOnlyCollection<System.Linq.Expressions.MemberBinding> Bindings { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Linq.Expressions.MemberBinding>); } }
        public System.Linq.Expressions.MemberMemberBinding Update(System.Collections.Generic.IEnumerable<System.Linq.Expressions.MemberBinding> bindings) { return default(System.Linq.Expressions.MemberMemberBinding); }
    }
    public partial class MethodCallExpression : System.Linq.Expressions.Expression, System.Linq.Expressions.IArgumentProvider
    {
        internal MethodCallExpression() { }
        public System.Collections.ObjectModel.ReadOnlyCollection<System.Linq.Expressions.Expression> Arguments { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Linq.Expressions.Expression>); } }
        public System.Reflection.MethodInfo Method { get { return default(System.Reflection.MethodInfo); } }
        public sealed override System.Linq.Expressions.ExpressionType NodeType { get { return default(System.Linq.Expressions.ExpressionType); } }
        public System.Linq.Expressions.Expression Object { get { return default(System.Linq.Expressions.Expression); } }
        int System.Linq.Expressions.IArgumentProvider.ArgumentCount { get { return default(int); } }
        public sealed override System.Type Type { get { return default(System.Type); } }
        protected internal override System.Linq.Expressions.Expression Accept(System.Linq.Expressions.ExpressionVisitor visitor) { return default(System.Linq.Expressions.Expression); }
        System.Linq.Expressions.Expression System.Linq.Expressions.IArgumentProvider.GetArgument(int index) { return default(System.Linq.Expressions.Expression); }
        public System.Linq.Expressions.MethodCallExpression Update(System.Linq.Expressions.Expression @object, System.Collections.Generic.IEnumerable<System.Linq.Expressions.Expression> arguments) { return default(System.Linq.Expressions.MethodCallExpression); }
    }
    public partial class NewArrayExpression : System.Linq.Expressions.Expression
    {
        internal NewArrayExpression() { }
        public System.Collections.ObjectModel.ReadOnlyCollection<System.Linq.Expressions.Expression> Expressions { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Linq.Expressions.Expression>); } }
        public sealed override System.Type Type { get { return default(System.Type); } }
        protected internal override System.Linq.Expressions.Expression Accept(System.Linq.Expressions.ExpressionVisitor visitor) { return default(System.Linq.Expressions.Expression); }
        public System.Linq.Expressions.NewArrayExpression Update(System.Collections.Generic.IEnumerable<System.Linq.Expressions.Expression> expressions) { return default(System.Linq.Expressions.NewArrayExpression); }
    }
    public partial class NewExpression : System.Linq.Expressions.Expression, System.Linq.Expressions.IArgumentProvider
    {
        internal NewExpression() { }
        public System.Collections.ObjectModel.ReadOnlyCollection<System.Linq.Expressions.Expression> Arguments { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Linq.Expressions.Expression>); } }
        public System.Reflection.ConstructorInfo Constructor { get { return default(System.Reflection.ConstructorInfo); } }
        public System.Collections.ObjectModel.ReadOnlyCollection<System.Reflection.MemberInfo> Members { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Reflection.MemberInfo>); } }
        public sealed override System.Linq.Expressions.ExpressionType NodeType { get { return default(System.Linq.Expressions.ExpressionType); } }
        int System.Linq.Expressions.IArgumentProvider.ArgumentCount { get { return default(int); } }
        public override System.Type Type { get { return default(System.Type); } }
        protected internal override System.Linq.Expressions.Expression Accept(System.Linq.Expressions.ExpressionVisitor visitor) { return default(System.Linq.Expressions.Expression); }
        System.Linq.Expressions.Expression System.Linq.Expressions.IArgumentProvider.GetArgument(int index) { return default(System.Linq.Expressions.Expression); }
        public System.Linq.Expressions.NewExpression Update(System.Collections.Generic.IEnumerable<System.Linq.Expressions.Expression> arguments) { return default(System.Linq.Expressions.NewExpression); }
    }
    public partial class ParameterExpression : System.Linq.Expressions.Expression
    {
        internal ParameterExpression() { }
        public bool IsByRef { get { return default(bool); } }
        public string Name { get { return default(string); } }
        public sealed override System.Linq.Expressions.ExpressionType NodeType { get { return default(System.Linq.Expressions.ExpressionType); } }
        public override System.Type Type { get { return default(System.Type); } }
        protected internal override System.Linq.Expressions.Expression Accept(System.Linq.Expressions.ExpressionVisitor visitor) { return default(System.Linq.Expressions.Expression); }
    }
    public sealed partial class RuntimeVariablesExpression : System.Linq.Expressions.Expression
    {
        internal RuntimeVariablesExpression() { }
        public sealed override System.Linq.Expressions.ExpressionType NodeType { get { return default(System.Linq.Expressions.ExpressionType); } }
        public sealed override System.Type Type { get { return default(System.Type); } }
        public System.Collections.ObjectModel.ReadOnlyCollection<System.Linq.Expressions.ParameterExpression> Variables { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Linq.Expressions.ParameterExpression>); } }
        protected internal override System.Linq.Expressions.Expression Accept(System.Linq.Expressions.ExpressionVisitor visitor) { return default(System.Linq.Expressions.Expression); }
        public System.Linq.Expressions.RuntimeVariablesExpression Update(System.Collections.Generic.IEnumerable<System.Linq.Expressions.ParameterExpression> variables) { return default(System.Linq.Expressions.RuntimeVariablesExpression); }
    }
    public sealed partial class SwitchCase
    {
        internal SwitchCase() { }
        public System.Linq.Expressions.Expression Body { get { return default(System.Linq.Expressions.Expression); } }
        public System.Collections.ObjectModel.ReadOnlyCollection<System.Linq.Expressions.Expression> TestValues { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Linq.Expressions.Expression>); } }
        public override string ToString() { return default(string); }
        public System.Linq.Expressions.SwitchCase Update(System.Collections.Generic.IEnumerable<System.Linq.Expressions.Expression> testValues, System.Linq.Expressions.Expression body) { return default(System.Linq.Expressions.SwitchCase); }
    }
    public sealed partial class SwitchExpression : System.Linq.Expressions.Expression
    {
        internal SwitchExpression() { }
        public System.Collections.ObjectModel.ReadOnlyCollection<System.Linq.Expressions.SwitchCase> Cases { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Linq.Expressions.SwitchCase>); } }
        public System.Reflection.MethodInfo Comparison { get { return default(System.Reflection.MethodInfo); } }
        public System.Linq.Expressions.Expression DefaultBody { get { return default(System.Linq.Expressions.Expression); } }
        public sealed override System.Linq.Expressions.ExpressionType NodeType { get { return default(System.Linq.Expressions.ExpressionType); } }
        public System.Linq.Expressions.Expression SwitchValue { get { return default(System.Linq.Expressions.Expression); } }
        public sealed override System.Type Type { get { return default(System.Type); } }
        protected internal override System.Linq.Expressions.Expression Accept(System.Linq.Expressions.ExpressionVisitor visitor) { return default(System.Linq.Expressions.Expression); }
        public System.Linq.Expressions.SwitchExpression Update(System.Linq.Expressions.Expression switchValue, System.Collections.Generic.IEnumerable<System.Linq.Expressions.SwitchCase> cases, System.Linq.Expressions.Expression defaultBody) { return default(System.Linq.Expressions.SwitchExpression); }
    }
    public partial class SymbolDocumentInfo
    {
        internal SymbolDocumentInfo() { }
        public virtual System.Guid DocumentType { get { return default(System.Guid); } }
        public string FileName { get { return default(string); } }
        public virtual System.Guid Language { get { return default(System.Guid); } }
        public virtual System.Guid LanguageVendor { get { return default(System.Guid); } }
    }
    public sealed partial class TryExpression : System.Linq.Expressions.Expression
    {
        internal TryExpression() { }
        public System.Linq.Expressions.Expression Body { get { return default(System.Linq.Expressions.Expression); } }
        public System.Linq.Expressions.Expression Fault { get { return default(System.Linq.Expressions.Expression); } }
        public System.Linq.Expressions.Expression Finally { get { return default(System.Linq.Expressions.Expression); } }
        public System.Collections.ObjectModel.ReadOnlyCollection<System.Linq.Expressions.CatchBlock> Handlers { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Linq.Expressions.CatchBlock>); } }
        public sealed override System.Linq.Expressions.ExpressionType NodeType { get { return default(System.Linq.Expressions.ExpressionType); } }
        public sealed override System.Type Type { get { return default(System.Type); } }
        protected internal override System.Linq.Expressions.Expression Accept(System.Linq.Expressions.ExpressionVisitor visitor) { return default(System.Linq.Expressions.Expression); }
        public System.Linq.Expressions.TryExpression Update(System.Linq.Expressions.Expression body, System.Collections.Generic.IEnumerable<System.Linq.Expressions.CatchBlock> handlers, System.Linq.Expressions.Expression @finally, System.Linq.Expressions.Expression fault) { return default(System.Linq.Expressions.TryExpression); }
    }
    public sealed partial class TypeBinaryExpression : System.Linq.Expressions.Expression
    {
        internal TypeBinaryExpression() { }
        public System.Linq.Expressions.Expression Expression { get { return default(System.Linq.Expressions.Expression); } }
        public sealed override System.Linq.Expressions.ExpressionType NodeType { get { return default(System.Linq.Expressions.ExpressionType); } }
        public sealed override System.Type Type { get { return default(System.Type); } }
        public System.Type TypeOperand { get { return default(System.Type); } }
        protected internal override System.Linq.Expressions.Expression Accept(System.Linq.Expressions.ExpressionVisitor visitor) { return default(System.Linq.Expressions.Expression); }
        public System.Linq.Expressions.TypeBinaryExpression Update(System.Linq.Expressions.Expression expression) { return default(System.Linq.Expressions.TypeBinaryExpression); }
    }
    public sealed partial class UnaryExpression : System.Linq.Expressions.Expression
    {
        internal UnaryExpression() { }
        public override bool CanReduce { get { return default(bool); } }
        public bool IsLifted { get { return default(bool); } }
        public bool IsLiftedToNull { get { return default(bool); } }
        public System.Reflection.MethodInfo Method { get { return default(System.Reflection.MethodInfo); } }
        public sealed override System.Linq.Expressions.ExpressionType NodeType { get { return default(System.Linq.Expressions.ExpressionType); } }
        public System.Linq.Expressions.Expression Operand { get { return default(System.Linq.Expressions.Expression); } }
        public sealed override System.Type Type { get { return default(System.Type); } }
        protected internal override System.Linq.Expressions.Expression Accept(System.Linq.Expressions.ExpressionVisitor visitor) { return default(System.Linq.Expressions.Expression); }
        public override System.Linq.Expressions.Expression Reduce() { return default(System.Linq.Expressions.Expression); }
        public System.Linq.Expressions.UnaryExpression Update(System.Linq.Expressions.Expression operand) { return default(System.Linq.Expressions.UnaryExpression); }
    }
}
