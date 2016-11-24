// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
    /// <summary>
    /// The base type for all nodes in Expression Trees.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling")]
    public abstract partial class Expression
    {
        private static readonly CacheDict<Type, MethodInfo> s_lambdaDelegateCache = new CacheDict<Type, MethodInfo>(40);
        private static volatile CacheDict<Type, Func<Expression, string, bool, ReadOnlyCollection<ParameterExpression>, LambdaExpression>> s_lambdaFactories;

        // For 4.0, many frequently used Expression nodes have had their memory
        // footprint reduced by removing the Type and NodeType fields. This has
        // large performance benefits to all users of Expression Trees.
        //
        // To support the 3.5 protected constructor, we store the fields that
        // used to be here in a ConditionalWeakTable.

        private class ExtensionInfo
        {
            public ExtensionInfo(ExpressionType nodeType, Type type)
            {
                NodeType = nodeType;
                Type = type;
            }

            internal readonly ExpressionType NodeType;
            internal readonly Type Type;
        }

        private static ConditionalWeakTable<Expression, ExtensionInfo> s_legacyCtorSupportTable;

        /// <summary>
        /// Constructs a new instance of <see cref="Expression"/>.
        /// </summary>
        /// <param name="nodeType">The <see ctype="ExpressionType"/> of the <see cref="Expression"/>.</param>
        /// <param name="type">The <see cref="Type"/> of the <see cref="Expression"/>.</param>
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

        /// <summary>
        /// Constructs a new instance of <see cref="Expression"/>.
        /// </summary>
        protected Expression()
        {
        }

        /// <summary>
        /// The <see cref="ExpressionType"/> of the <see cref="Expression"/>.
        /// </summary>
        public virtual ExpressionType NodeType
        {
            get
            {
                ExtensionInfo extInfo;
                if (s_legacyCtorSupportTable != null && s_legacyCtorSupportTable.TryGetValue(this, out extInfo))
                {
                    return extInfo.NodeType;
                }

                // the extension expression failed to override NodeType
                throw Error.ExtensionNodeMustOverrideProperty("Expression.NodeType");
            }
        }


        /// <summary>
        /// The <see cref="Type"/> of the value represented by this <see cref="Expression"/>.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1721:PropertyNamesShouldNotMatchGetMethods")]
        public virtual Type Type
        {
            get
            {
                ExtensionInfo extInfo;
                if (s_legacyCtorSupportTable != null && s_legacyCtorSupportTable.TryGetValue(this, out extInfo))
                {
                    return extInfo.Type;
                }

                // the extension expression failed to override Type
                throw Error.ExtensionNodeMustOverrideProperty("Expression.Type");
            }
        }

        /// <summary>
        /// Indicates that the node can be reduced to a simpler node. If this
        /// returns true, Reduce() can be called to produce the reduced form.
        /// </summary>
        public virtual bool CanReduce => false;

        /// <summary>
        /// Reduces this node to a simpler expression. If CanReduce returns
        /// true, this should return a valid expression. This method is
        /// allowed to return another node which itself must be reduced.
        /// </summary>
        /// <returns>The reduced expression.</returns>
        public virtual Expression Reduce()
        {
            if (CanReduce) throw Error.ReducibleMustOverrideReduce();
            return this;
        }

        /// <summary>
        /// Reduces the node and then calls the <see cref="ExpressionVisitor.Visit(Expression)"/> method passing the reduced expression.
        /// Throws an exception if the node isn't reducible.
        /// </summary>
        /// <param name="visitor">An instance of <see cref="ExpressionVisitor"/>.</param>
        /// <returns>The expression being visited, or an expression which should replace it in the tree.</returns>
        /// <remarks>
        /// Override this method to provide logic to walk the node's children.
        /// A typical implementation will call visitor.Visit on each of its
        /// children, and if any of them change, should return a new copy of
        /// itself with the modified children.
        /// </remarks>
        protected internal virtual Expression VisitChildren(ExpressionVisitor visitor)
        {
            if (!CanReduce) throw Error.MustBeReducible();
            return visitor.Visit(ReduceAndCheck());
        }

        /// <summary>
        /// Dispatches to the specific visit method for this node type. For
        /// example, <see cref="MethodCallExpression"/> will call into
        /// <see cref="ExpressionVisitor.VisitMethodCall"/>.
        /// </summary>
        /// <param name="visitor">The visitor to visit this node with.</param>
        /// <returns>The result of visiting this node.</returns>
        /// <remarks>
        /// This default implementation for <see cref="ExpressionType.Extension"/>
        /// nodes will call <see cref="ExpressionVisitor.VisitExtension"/>.
        /// Override this method to call into a more specific method on a derived
        /// visitor class of ExprressionVisitor. However, it should still
        /// support unknown visitors by calling VisitExtension.
        /// </remarks>
        protected internal virtual Expression Accept(ExpressionVisitor visitor)
        {
            return visitor.VisitExtension(this);
        }

        /// <summary>
        /// Reduces this node to a simpler expression. If CanReduce returns
        /// true, this should return a valid expression. This method is
        /// allowed to return another node which itself must be reduced.
        /// </summary>
        /// <returns>The reduced expression.</returns>
        /// <remarks >
        /// Unlike Reduce, this method checks that the reduced node satisfies
        /// certain invariants.
        /// </remarks>
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

        /// <summary>
        /// Reduces the expression to a known node type (i.e. not an Extension node)
        /// or simply returns the expression if it is already a known type.
        /// </summary>
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

        /// <summary>
        /// Creates a <see cref="String"/> representation of the Expression.
        /// </summary>
        /// <returns>A <see cref="String"/> representation of the Expression.</returns>
        public override string ToString()
        {
            return ExpressionStringBuilder.ExpressionToString(this);
        }

        /// <summary>
        /// Creates a <see cref="String"/> representation of the Expression.
        /// </summary>
        /// <returns>A <see cref="String"/> representation of the Expression.</returns>
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

        /// <summary>
        /// Helper used for ensuring we only return 1 instance of a ReadOnlyCollection of T.
        ///
        /// This is called from various methods where we internally hold onto an IList of T
        /// or a readonly collection of T.  We check to see if we've already returned a
        /// readonly collection of T and if so simply return the other one.  Otherwise we do
        /// a thread-safe replacement of the list w/ a readonly collection which wraps it.
        ///
        /// Ultimately this saves us from having to allocate a ReadOnlyCollection for our
        /// data types because the compiler is capable of going directly to the IList of T.
        /// </summary>
        internal static ReadOnlyCollection<T> ReturnReadOnly<T>(ref IReadOnlyList<T> collection)
        {
            return ExpressionUtils.ReturnReadOnly<T>(ref collection);
        }

        /// <summary>
        /// Helper used for ensuring we only return 1 instance of a ReadOnlyCollection of T.
        ///
        /// This is similar to the ReturnReadOnly of T. This version supports nodes which hold
        /// onto multiple Expressions where one is typed to object.  That object field holds either
        /// an expression or a ReadOnlyCollection of Expressions.  When it holds a ReadOnlyCollection
        /// the IList which backs it is a ListArgumentProvider which uses the Expression which
        /// implements IArgumentProvider to get 2nd and additional values.  The ListArgumentProvider
        /// continues to hold onto the 1st expression.
        ///
        /// This enables users to get the ReadOnlyCollection w/o it consuming more memory than if
        /// it was just an array.  Meanwhile The DLR internally avoids accessing  which would force
        /// the readonly collection to be created resulting in a typical memory savings.
        /// </summary>
        internal static ReadOnlyCollection<Expression> ReturnReadOnly(IArgumentProvider provider, ref object collection)
        {
            return ExpressionUtils.ReturnReadOnly(provider, ref collection);
        }

        /// <summary>
        /// See overload with <see cref="IArgumentProvider"/> for more information. 
        /// </summary>
        internal static ReadOnlyCollection<ParameterExpression> ReturnReadOnly(IParameterProvider provider, ref object collection)
        {
            return ExpressionUtils.ReturnReadOnly(provider, ref collection);
        }

        /// <summary>
        /// Helper which is used for specialized subtypes which use ReturnReadOnly(ref object, ...).
        /// This is the reverse version of ReturnReadOnly which takes an IArgumentProvider.
        ///
        /// This is used to return the 1st argument.  The 1st argument is typed as object and either
        /// contains a ReadOnlyCollection or the Expression.  We check for the Expression and if it's
        /// present we return that, otherwise we return the 1st element of the ReadOnlyCollection.
        /// </summary>
        internal static T ReturnObject<T>(object collectionOrT) where T : class
        {
            return ExpressionUtils.ReturnObject<T>(collectionOrT);
        }

        private static void RequiresCanRead(Expression expression, string paramName)
        {
            ExpressionUtils.RequiresCanRead(expression, paramName, -1);
        }

        private static void RequiresCanRead(Expression expression, string paramName, int index)
        {
            ExpressionUtils.RequiresCanRead(expression, paramName, index);
        }

        private static void RequiresCanRead(IReadOnlyList<Expression> items, string paramName)
        {
            Debug.Assert(items != null);
            // this is called a lot, avoid allocating an enumerator if we can...
            for (int i = 0, n = items.Count; i < n; i++)
            {
                RequiresCanRead(items[i], paramName, i);
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
                    PropertyInfo indexer = ((IndexExpression)expression).Indexer;
                    if (indexer == null || indexer.CanWrite)
                    {
                        return;
                    }
                    break;
                case ExpressionType.MemberAccess:
                    MemberInfo member = ((MemberExpression)expression).Member;
                    PropertyInfo prop = member as PropertyInfo;
                    if (prop != null)
                    {
                        if(prop.CanWrite)
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
    }
}
