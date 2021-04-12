// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Dynamic.Utils;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;

namespace System.Linq.Expressions
{
    /// <summary>Provides the base class from which the classes that represent expression tree nodes are derived. It also contains <see langword="static" /> (<see langword="Shared" /> in Visual Basic) factory methods to create the various node types. This is an <see langword="abstract" /> class.</summary>
    /// <remarks></remarks>
    /// <example>The following code example shows how to create a block expression. The block expression consists of two <see cref="System.Linq.Expressions.MethodCallExpression" /> objects and one <see cref="System.Linq.Expressions.ConstantExpression" /> object.
    /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/cs/program.cs" id="Snippet13":::
    /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/vb/module1.vb" id="Snippet13":::</example>
    public abstract partial class Expression
    {
        internal const string ExpressionRequiresUnreferencedCode = "Creating Expressions requires unreferenced code because the members being referenced by the Expression may be trimmed.";
        internal const string PropertyFromAccessorRequiresUnreferencedCode = "The Property metadata or other accessor may be trimmed.";
        internal const string GenericMethodRequiresUnreferencedCode = "Calling a generic method cannot be statically analyzed. It's not possible to guarantee the availability of requirements of the generic method. This can be suppressed if the method is not generic.";

        private static readonly CacheDict<Type, MethodInfo> s_lambdaDelegateCache = new CacheDict<Type, MethodInfo>(40);
        private static volatile CacheDict<Type, Func<Expression, string?, bool, ReadOnlyCollection<ParameterExpression>, LambdaExpression>>? s_lambdaFactories;

        // For 4.0, many frequently used Expression nodes have had their memory
        // footprint reduced by removing the Type and NodeType fields. This has
        // large performance benefits to all users of Expression Trees.
        //
        // To support the 3.5 protected constructor, we store the fields that
        // used to be here in a ConditionalWeakTable.

        private sealed class ExtensionInfo
        {
            public ExtensionInfo(ExpressionType nodeType, Type type)
            {
                NodeType = nodeType;
                Type = type;
            }

            internal readonly ExpressionType NodeType;
            internal readonly Type Type;
        }

        private static ConditionalWeakTable<Expression, ExtensionInfo>? s_legacyCtorSupportTable;

        /// <summary>Initializes a new instance of the <see cref="System.Linq.Expressions.Expression" /> class.</summary>
        /// <param name="nodeType">The <see cref="System.Linq.Expressions.ExpressionType" /> to set as the node type.</param>
        /// <param name="type">The <see cref="System.Linq.Expressions.Expression.Type" /> of this <see cref="System.Linq.Expressions.Expression" />.</param>
        /// <remarks>This constructor is called from constructors in derived classes.</remarks>
        [Obsolete("use a different constructor that does not take ExpressionType. Then override NodeType and Type properties to provide the values that would be specified to this constructor.")]
        protected Expression(ExpressionType nodeType, Type type)
        {
            // Can't enforce anything that V1 didn't
            if (s_legacyCtorSupportTable == null)
            {
                Interlocked.CompareExchange(
                    ref s_legacyCtorSupportTable,
                    new ConditionalWeakTable<Expression, ExtensionInfo>(),
comparand: null
                );
            }

            s_legacyCtorSupportTable.Add(this, new ExtensionInfo(nodeType, type));
        }

        /// <summary>Constructs a new instance of <see cref="System.Linq.Expressions.Expression" />.</summary>
        protected Expression()
        {
        }

        /// <summary>Gets the node type of this <see cref="System.Linq.Expressions.Expression" />.</summary>
        /// <value>One of the <see cref="System.Linq.Expressions.ExpressionType" /> values.</value>
        /// <remarks>The <see cref="O:System.Linq.Expressions.Expression.NodeType" /> property provides a more specialized description of an <see cref="System.Linq.Expressions.Expression" /> than just its derived type. For example, a <see cref="System.Linq.Expressions.BinaryExpression" /> can be used to represent many different kinds of binary expressions, such as a division operation or a "greater than" operation. The <see cref="O:System.Linq.Expressions.Expression.NodeType" /> property would describe these binary expressions as <see cref="System.Linq.Expressions.ExpressionType.Divide" /> and <see cref="System.Linq.Expressions.ExpressionType.GreaterThan" />, respectively.
        /// The static CLR type of the expression that the <see cref="System.Linq.Expressions.Expression" /> object represents is represented by the <see cref="O:System.Linq.Expressions.Expression.Type" /> property.</remarks>
        public virtual ExpressionType NodeType
        {
            get
            {
                if (s_legacyCtorSupportTable != null && s_legacyCtorSupportTable.TryGetValue(this, out ExtensionInfo? extInfo))
                {
                    return extInfo.NodeType;
                }

                // the extension expression failed to override NodeType
                throw Error.ExtensionNodeMustOverrideProperty("Expression.NodeType");
            }
        }

        /// <summary>Gets the static type of the expression that this <see cref="System.Linq.Expressions.Expression" /> represents.</summary>
        /// <value>The <see cref="System.Type" /> that represents the static type of the expression.</value>
        /// <remarks>The <see cref="O:System.Linq.Expressions.Expression.NodeType" /> is the type of the expression tree node, whereas the <see cref="O:System.Linq.Expressions.Expression.Type" /> represents the static common language runtime (CLR) type of the expression that the node represents. For example, two nodes with different node types can have the same <see cref="O:System.Linq.Expressions.Expression.Type" />, as shown in the following code example.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/cs/program.cs" id="Snippet36":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/vb/module1.vb" id="Snippet36":::</remarks>
        public virtual Type Type
        {
            get
            {
                if (s_legacyCtorSupportTable != null && s_legacyCtorSupportTable.TryGetValue(this, out ExtensionInfo? extInfo))
                {
                    return extInfo.Type;
                }

                // the extension expression failed to override Type
                throw Error.ExtensionNodeMustOverrideProperty("Expression.Type");
            }
        }

        /// <summary>Indicates that the node can be reduced to a simpler node. If this returns true, Reduce() can be called to produce the reduced form.</summary>
        /// <value><see langword="true" /> if the node can be reduced; otherwise, <see langword="false" />.</value>
        public virtual bool CanReduce => false;

        /// <summary>Reduces this node to a simpler expression. If CanReduce returns true, this should return a valid expression. This method can return another node which itself must be reduced.</summary>
        /// <returns>The reduced expression.</returns>
        public virtual Expression Reduce()
        {
            if (CanReduce) throw Error.ReducibleMustOverrideReduce();
            return this;
        }

        /// <summary>Reduces the node and then calls the visitor delegate on the reduced expression. The method throws an exception if the node is not reducible.</summary>
        /// <param name="visitor">An instance of <see cref="System.Func{T1,T2}" />.</param>
        /// <returns>The expression being visited, or an expression which should replace it in the tree.</returns>
        /// <remarks>Override this method to provide logic to walk the node's children. A typical implementation will call visitor.Visit on each of its children, and if any of them change, should return a new copy of itself with the modified children.</remarks>
        protected internal virtual Expression VisitChildren(ExpressionVisitor visitor)
        {
            if (!CanReduce) throw Error.MustBeReducible();
            return visitor.Visit(ReduceAndCheck());
        }

        /// <summary>Dispatches to the specific visit method for this node type. For example, <see cref="System.Linq.Expressions.MethodCallExpression" /> calls the <see cref="System.Linq.Expressions.ExpressionVisitor.VisitMethodCall(System.Linq.Expressions.MethodCallExpression)" />.</summary>
        /// <param name="visitor">The visitor to visit this node with.</param>
        /// <returns>The result of visiting this node.</returns>
        /// <remarks>This default implementation for <see cref="System.Linq.Expressions.ExpressionType.Extension" /> nodes calls <see cref="O:System.Linq.Expressions.ExpressionVisitor.VisitExtension" />. Override this method to call into a more specific method on a derived visitor class of the <see cref="System.Linq.Expressions.ExpressionVisitor" /> class. However, it should still support unknown visitors by calling <see cref="O:System.Linq.Expressions.ExpressionVisitor.VisitExtension" />.</remarks>
        protected internal virtual Expression Accept(ExpressionVisitor visitor)
        {
            return visitor.VisitExtension(this);
        }

        /// <summary>Reduces this node to a simpler expression. If CanReduce returns true, this should return a valid expression. This method can return another node which itself must be reduced.</summary>
        /// <returns>The reduced expression.</returns>
        /// <remarks>Unlike Reduce, this method checks that the reduced node satisfies certain invariants.</remarks>
        public Expression ReduceAndCheck()
        {
            if (!CanReduce) throw Error.MustBeReducible();

            Expression newNode = Reduce();

            // 1. Reduction must return a new, non-null node
            // 2. Reduction must return a new node whose result type can be assigned to the type of the original node
            if (newNode == null || newNode == this) throw Error.MustReduceToDifferent();
            if (!TypeUtils.AreReferenceAssignable(Type, newNode.Type)) throw Error.ReducedNotCompatible();
            return newNode;
        }

        /// <summary>Reduces the expression to a known node type (that is not an Extension node) or just returns the expression if it is already a known type.</summary>
        /// <returns>The reduced expression.</returns>
        public Expression ReduceExtensions()
        {
            Expression node = this;
            while (node.NodeType == ExpressionType.Extension)
            {
                node = node.ReduceAndCheck();
            }
            return node;
        }

        /// <summary>Returns a textual representation of the <see cref="System.Linq.Expressions.Expression" />.</summary>
        /// <returns>A textual representation of the <see cref="System.Linq.Expressions.Expression" />.</returns>
        public override string ToString()
        {
            return ExpressionStringBuilder.ExpressionToString(this);
        }

        /// <summary>
        /// Creates a <see cref="string"/> representation of the Expression.
        /// </summary>
        /// <returns>A <see cref="string"/> representation of the Expression.</returns>
        private string DebugView
        {
            // Note that this property is often accessed using reflection. As such it will have more dependencies than one
            // might surmise from its being internal, and removing it requires greater caution than with other internal methods.
            get
            {
                using (System.IO.StringWriter writer = new System.IO.StringWriter(CultureInfo.CurrentCulture))
                {
                    DebugViewWriter.WriteTo(this, writer);
                    return writer.ToString();
                }
            }
        }

        private static void RequiresCanRead(IReadOnlyList<Expression> items, string paramName)
        {
            Debug.Assert(items != null);
            // this is called a lot, avoid allocating an enumerator if we can...
            for (int i = 0, n = items.Count; i < n; i++)
            {
                ExpressionUtils.RequiresCanRead(items[i], paramName, i);
            }
        }

        private static void RequiresCanWrite(Expression expression, string paramName)
        {
            if (expression == null)
            {
                throw new ArgumentNullException(paramName);
            }

            switch (expression.NodeType)
            {
                case ExpressionType.Index:
                    PropertyInfo? indexer = ((IndexExpression)expression).Indexer;
                    if (indexer == null || indexer.CanWrite)
                    {
                        return;
                    }
                    break;
                case ExpressionType.MemberAccess:
                    MemberInfo member = ((MemberExpression)expression).Member;
                    if (member is PropertyInfo prop)
                    {
                        if (prop.CanWrite)
                        {
                            return;
                        }
                    }
                    else
                    {
                        Debug.Assert(member is FieldInfo);
                        FieldInfo field = (FieldInfo)member;
                        if (!(field.IsInitOnly || field.IsLiteral))
                        {
                            return;
                        }
                    }
                    break;
                case ExpressionType.Parameter:
                    return;
            }

            throw Error.ExpressionMustBeWriteable(paramName);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.DynamicExpression" /> that represents a dynamic operation bound by the provided <see cref="System.Runtime.CompilerServices.CallSiteBinder" />.</summary>
        /// <param name="binder">The runtime binder for the dynamic operation.</param>
        /// <param name="returnType">The result type of the dynamic expression.</param>
        /// <param name="arguments">The arguments to the dynamic operation.</param>
        /// <returns>A <see cref="System.Linq.Expressions.DynamicExpression" /> that has <see cref="System.Linq.Expressions.Expression.NodeType" /> equal to <see cref="System.Linq.Expressions.ExpressionType.Dynamic" /> and has the <see cref="System.Linq.Expressions.DynamicExpression.Binder" /> and <see cref="System.Linq.Expressions.DynamicExpression.Arguments" /> set to the specified values.</returns>
        /// <remarks>The <see cref="O:System.Linq.Expressions.DynamicExpression.DelegateType" /> property of the result will be inferred from the types of the arguments and the specified return type.</remarks>
        public static DynamicExpression Dynamic(CallSiteBinder binder, Type returnType, IEnumerable<Expression> arguments) =>
            DynamicExpression.Dynamic(binder, returnType, arguments);

        /// <summary>Creates a <see cref="System.Linq.Expressions.DynamicExpression" /> that represents a dynamic operation bound by the provided <see cref="System.Runtime.CompilerServices.CallSiteBinder" />.</summary>
        /// <param name="binder">The runtime binder for the dynamic operation.</param>
        /// <param name="returnType">The result type of the dynamic expression.</param>
        /// <param name="arg0">The first argument to the dynamic operation.</param>
        /// <returns>A <see cref="System.Linq.Expressions.DynamicExpression" /> that has <see cref="System.Linq.Expressions.Expression.NodeType" /> equal to <see cref="System.Linq.Expressions.ExpressionType.Dynamic" /> and has the <see cref="System.Linq.Expressions.DynamicExpression.Binder" /> and <see cref="System.Linq.Expressions.DynamicExpression.Arguments" /> set to the specified values.</returns>
        /// <remarks>The <see cref="O:System.Linq.Expressions.DynamicExpression.DelegateType" /> property of the result will be inferred from the types of the arguments and the specified return type.</remarks>
        public static DynamicExpression Dynamic(CallSiteBinder binder, Type returnType, Expression arg0) =>
            DynamicExpression.Dynamic(binder, returnType, arg0);

        /// <summary>Creates a <see cref="System.Linq.Expressions.DynamicExpression" /> that represents a dynamic operation bound by the provided <see cref="System.Runtime.CompilerServices.CallSiteBinder" />.</summary>
        /// <param name="binder">The runtime binder for the dynamic operation.</param>
        /// <param name="returnType">The result type of the dynamic expression.</param>
        /// <param name="arg0">The first argument to the dynamic operation.</param>
        /// <param name="arg1">The second argument to the dynamic operation.</param>
        /// <returns>A <see cref="System.Linq.Expressions.DynamicExpression" /> that has <see cref="System.Linq.Expressions.Expression.NodeType" /> equal to <see cref="System.Linq.Expressions.ExpressionType.Dynamic" /> and has the <see cref="System.Linq.Expressions.DynamicExpression.Binder" /> and <see cref="System.Linq.Expressions.DynamicExpression.Arguments" /> set to the specified values.</returns>
        /// <remarks>The <see cref="O:System.Linq.Expressions.DynamicExpression.DelegateType" /> property of the result will be inferred from the types of the arguments and the specified return type.</remarks>
        public static DynamicExpression Dynamic(CallSiteBinder binder, Type returnType, Expression arg0, Expression arg1) =>
            DynamicExpression.Dynamic(binder, returnType, arg0, arg1);

        /// <summary>Creates a <see cref="System.Linq.Expressions.DynamicExpression" /> that represents a dynamic operation bound by the provided <see cref="System.Runtime.CompilerServices.CallSiteBinder" />.</summary>
        /// <param name="binder">The runtime binder for the dynamic operation.</param>
        /// <param name="returnType">The result type of the dynamic expression.</param>
        /// <param name="arg0">The first argument to the dynamic operation.</param>
        /// <param name="arg1">The second argument to the dynamic operation.</param>
        /// <param name="arg2">The third argument to the dynamic operation.</param>
        /// <returns>A <see cref="System.Linq.Expressions.DynamicExpression" /> that has <see cref="System.Linq.Expressions.Expression.NodeType" /> equal to <see cref="System.Linq.Expressions.ExpressionType.Dynamic" /> and has the <see cref="System.Linq.Expressions.DynamicExpression.Binder" /> and <see cref="System.Linq.Expressions.DynamicExpression.Arguments" /> set to the specified values.</returns>
        /// <remarks>The <see cref="O:System.Linq.Expressions.DynamicExpression.DelegateType" /> property of the result will be inferred from the types of the arguments and the specified return type.</remarks>
        public static DynamicExpression Dynamic(CallSiteBinder binder, Type returnType, Expression arg0, Expression arg1, Expression arg2) =>
            DynamicExpression.Dynamic(binder, returnType, arg0, arg1, arg2);

        /// <summary>Creates a <see cref="System.Linq.Expressions.DynamicExpression" /> that represents a dynamic operation bound by the provided <see cref="System.Runtime.CompilerServices.CallSiteBinder" />.</summary>
        /// <param name="binder">The runtime binder for the dynamic operation.</param>
        /// <param name="returnType">The result type of the dynamic expression.</param>
        /// <param name="arg0">The first argument to the dynamic operation.</param>
        /// <param name="arg1">The second argument to the dynamic operation.</param>
        /// <param name="arg2">The third argument to the dynamic operation.</param>
        /// <param name="arg3">The fourth argument to the dynamic operation.</param>
        /// <returns>A <see cref="System.Linq.Expressions.DynamicExpression" /> that has <see cref="System.Linq.Expressions.Expression.NodeType" /> equal to <see cref="System.Linq.Expressions.ExpressionType.Dynamic" /> and has the <see cref="System.Linq.Expressions.DynamicExpression.Binder" /> and <see cref="System.Linq.Expressions.DynamicExpression.Arguments" /> set to the specified values.</returns>
        /// <remarks>The <see cref="O:System.Linq.Expressions.DynamicExpression.DelegateType" /> property of the result will be inferred from the types of the arguments and the specified return type.</remarks>
        public static DynamicExpression Dynamic(CallSiteBinder binder, Type returnType, Expression arg0, Expression arg1, Expression arg2, Expression arg3) =>
            DynamicExpression.Dynamic(binder, returnType, arg0, arg1, arg2, arg3);

        /// <summary>Creates a <see cref="System.Linq.Expressions.DynamicExpression" /> that represents a dynamic operation bound by the provided <see cref="System.Runtime.CompilerServices.CallSiteBinder" />.</summary>
        /// <param name="binder">The runtime binder for the dynamic operation.</param>
        /// <param name="returnType">The result type of the dynamic expression.</param>
        /// <param name="arguments">The arguments to the dynamic operation.</param>
        /// <returns>A <see cref="System.Linq.Expressions.DynamicExpression" /> that has <see cref="System.Linq.Expressions.Expression.NodeType" /> equal to <see cref="System.Linq.Expressions.ExpressionType.Dynamic" /> and has the <see cref="System.Linq.Expressions.DynamicExpression.Binder" /> and <see cref="System.Linq.Expressions.DynamicExpression.Arguments" /> set to the specified values.</returns>
        /// <remarks>The <see cref="O:System.Linq.Expressions.DynamicExpression.DelegateType" /> property of the result will be inferred from the types of the arguments and the specified return type.</remarks>
        public static DynamicExpression Dynamic(CallSiteBinder binder, Type returnType, params Expression[] arguments) =>
            DynamicExpression.Dynamic(binder, returnType, arguments);

        /// <summary>Creates a <see cref="System.Linq.Expressions.DynamicExpression" /> that represents a dynamic operation bound by the provided <see cref="System.Runtime.CompilerServices.CallSiteBinder" />.</summary>
        /// <param name="delegateType">The type of the delegate used by the <see cref="System.Runtime.CompilerServices.CallSite" />.</param>
        /// <param name="binder">The runtime binder for the dynamic operation.</param>
        /// <param name="arguments">The arguments to the dynamic operation.</param>
        /// <returns>A <see cref="System.Linq.Expressions.DynamicExpression" /> that has <see cref="System.Linq.Expressions.Expression.NodeType" /> equal to <see cref="System.Linq.Expressions.ExpressionType.Dynamic" /> and has the <see cref="System.Linq.Expressions.DynamicExpression.DelegateType" />, <see cref="System.Linq.Expressions.DynamicExpression.Binder" />, and <see cref="System.Linq.Expressions.DynamicExpression.Arguments" /> set to the specified values.</returns>
        public static DynamicExpression MakeDynamic(Type delegateType, CallSiteBinder binder, IEnumerable<Expression>? arguments) =>
            DynamicExpression.MakeDynamic(delegateType, binder, arguments);

        /// <summary>Creates a <see cref="System.Linq.Expressions.DynamicExpression" /> that represents a dynamic operation bound by the provided <see cref="System.Runtime.CompilerServices.CallSiteBinder" /> and one argument.</summary>
        /// <param name="delegateType">The type of the delegate used by the <see cref="System.Runtime.CompilerServices.CallSite" />.</param>
        /// <param name="binder">The runtime binder for the dynamic operation.</param>
        /// <param name="arg0">The argument to the dynamic operation.</param>
        /// <returns>A <see cref="System.Linq.Expressions.DynamicExpression" /> that has <see cref="System.Linq.Expressions.Expression.NodeType" /> equal to <see cref="System.Linq.Expressions.ExpressionType.Dynamic" /> and has the <see cref="System.Linq.Expressions.DynamicExpression.DelegateType" />, <see cref="System.Linq.Expressions.DynamicExpression.Binder" />, and <see cref="System.Linq.Expressions.DynamicExpression.Arguments" /> set to the specified values.</returns>
        public static DynamicExpression MakeDynamic(Type delegateType, CallSiteBinder binder, Expression arg0) =>
            DynamicExpression.MakeDynamic(delegateType, binder, arg0);

        /// <summary>Creates a <see cref="System.Linq.Expressions.DynamicExpression" /> that represents a dynamic operation bound by the provided <see cref="System.Runtime.CompilerServices.CallSiteBinder" /> and two arguments.</summary>
        /// <param name="delegateType">The type of the delegate used by the <see cref="System.Runtime.CompilerServices.CallSite" />.</param>
        /// <param name="binder">The runtime binder for the dynamic operation.</param>
        /// <param name="arg0">The first argument to the dynamic operation.</param>
        /// <param name="arg1">The second argument to the dynamic operation.</param>
        /// <returns>A <see cref="System.Linq.Expressions.DynamicExpression" /> that has <see cref="System.Linq.Expressions.Expression.NodeType" /> equal to <see cref="System.Linq.Expressions.ExpressionType.Dynamic" /> and has the <see cref="System.Linq.Expressions.DynamicExpression.DelegateType" />, <see cref="System.Linq.Expressions.DynamicExpression.Binder" />, and <see cref="System.Linq.Expressions.DynamicExpression.Arguments" /> set to the specified values.</returns>
        public static DynamicExpression MakeDynamic(Type delegateType, CallSiteBinder binder, Expression arg0, Expression arg1) =>
            DynamicExpression.MakeDynamic(delegateType, binder, arg0, arg1);

        /// <summary>Creates a <see cref="System.Linq.Expressions.DynamicExpression" /> that represents a dynamic operation bound by the provided <see cref="System.Runtime.CompilerServices.CallSiteBinder" /> and three arguments.</summary>
        /// <param name="delegateType">The type of the delegate used by the <see cref="System.Runtime.CompilerServices.CallSite" />.</param>
        /// <param name="binder">The runtime binder for the dynamic operation.</param>
        /// <param name="arg0">The first argument to the dynamic operation.</param>
        /// <param name="arg1">The second argument to the dynamic operation.</param>
        /// <param name="arg2">The third argument to the dynamic operation.</param>
        /// <returns>A <see cref="System.Linq.Expressions.DynamicExpression" /> that has <see cref="System.Linq.Expressions.Expression.NodeType" /> equal to <see cref="System.Linq.Expressions.ExpressionType.Dynamic" /> and has the <see cref="System.Linq.Expressions.DynamicExpression.DelegateType" />, <see cref="System.Linq.Expressions.DynamicExpression.Binder" />, and <see cref="System.Linq.Expressions.DynamicExpression.Arguments" /> set to the specified values.</returns>
        public static DynamicExpression MakeDynamic(Type delegateType, CallSiteBinder binder, Expression arg0, Expression arg1, Expression arg2) =>
            DynamicExpression.MakeDynamic(delegateType, binder, arg0, arg1, arg2);

        /// <summary>Creates a <see cref="System.Linq.Expressions.DynamicExpression" /> that represents a dynamic operation bound by the provided <see cref="System.Runtime.CompilerServices.CallSiteBinder" /> and four arguments.</summary>
        /// <param name="delegateType">The type of the delegate used by the <see cref="System.Runtime.CompilerServices.CallSite" />.</param>
        /// <param name="binder">The runtime binder for the dynamic operation.</param>
        /// <param name="arg0">The first argument to the dynamic operation.</param>
        /// <param name="arg1">The second argument to the dynamic operation.</param>
        /// <param name="arg2">The third argument to the dynamic operation.</param>
        /// <param name="arg3">The fourth argument to the dynamic operation.</param>
        /// <returns>A <see cref="System.Linq.Expressions.DynamicExpression" /> that has <see cref="System.Linq.Expressions.Expression.NodeType" /> equal to <see cref="System.Linq.Expressions.ExpressionType.Dynamic" /> and has the <see cref="System.Linq.Expressions.DynamicExpression.DelegateType" />, <see cref="System.Linq.Expressions.DynamicExpression.Binder" />, and <see cref="System.Linq.Expressions.DynamicExpression.Arguments" /> set to the specified values.</returns>
        public static DynamicExpression MakeDynamic(Type delegateType, CallSiteBinder binder, Expression arg0, Expression arg1, Expression arg2, Expression arg3) =>
            DynamicExpression.MakeDynamic(delegateType, binder, arg0, arg1, arg2, arg3);

        /// <summary>Creates a <see cref="System.Linq.Expressions.DynamicExpression" /> that represents a dynamic operation bound by the provided <see cref="System.Runtime.CompilerServices.CallSiteBinder" />.</summary>
        /// <param name="delegateType">The type of the delegate used by the <see cref="System.Runtime.CompilerServices.CallSite" />.</param>
        /// <param name="binder">The runtime binder for the dynamic operation.</param>
        /// <param name="arguments">The arguments to the dynamic operation.</param>
        /// <returns>A <see cref="System.Linq.Expressions.DynamicExpression" /> that has <see cref="System.Linq.Expressions.Expression.NodeType" /> equal to <see cref="System.Linq.Expressions.ExpressionType.Dynamic" /> and has the <see cref="System.Linq.Expressions.DynamicExpression.DelegateType" />, <see cref="System.Linq.Expressions.DynamicExpression.Binder" />, and <see cref="System.Linq.Expressions.DynamicExpression.Arguments" /> set to the specified values.</returns>
        public static DynamicExpression MakeDynamic(Type delegateType, CallSiteBinder binder, params Expression[]? arguments) =>
            MakeDynamic(delegateType, binder, (IEnumerable<Expression>?)arguments);
    }
}
