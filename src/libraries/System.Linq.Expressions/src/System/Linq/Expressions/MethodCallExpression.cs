// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic.Utils;
using System.Reflection;

namespace System.Linq.Expressions
{
    /// <summary>Represents a call to either static or an instance method.</summary>
    /// <remarks>Use the <see cref="O:System.Linq.Expressions.Expression.Call" />, <see cref="O:System.Linq.Expressions.Expression.ArrayIndex" />, or <see cref="O:System.Linq.Expressions.Expression.ArrayIndex" /> factory method to create a <see cref="System.Linq.Expressions.MethodCallExpression" />.
    /// The value of the <see cref="O:System.Linq.Expressions.Expression.NodeType" /> property of a <see cref="System.Linq.Expressions.MethodCallExpression" /> object is <see cref="System.Linq.Expressions.ExpressionType.Call" />.</remarks>
    /// <example>The following example creates a <see cref="System.Linq.Expressions.MethodCallExpression" /> object that represents indexing into a two-dimensional array.
    /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Expressions.Expression/CS/Expression.cs" id="Snippet3":::
    /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Expressions.Expression/VB/Expression.vb" id="Snippet3":::</example>
    [DebuggerTypeProxy(typeof(MethodCallExpressionProxy))]
    public class MethodCallExpression : Expression, IArgumentProvider
    {
        internal MethodCallExpression(MethodInfo method)
        {
            Method = method;
        }

        internal virtual Expression? GetInstance() => null;

        /// <summary>Returns the node type of this <see cref="System.Linq.Expressions.Expression" />.</summary>
        /// <value>The <see cref="System.Linq.Expressions.ExpressionType" /> that represents this expression.</value>
        public sealed override ExpressionType NodeType => ExpressionType.Call;

        /// <summary>Gets the static type of the expression that this <see cref="System.Linq.Expressions.Expression" /> represents.</summary>
        /// <value>The <see cref="System.Linq.Expressions.MethodCallExpression.Type" /> that represents the static type of the expression.</value>
        public sealed override Type Type => Method.ReturnType;

        /// <summary>Gets the <see cref="System.Reflection.MethodInfo" /> for the method to be called.</summary>
        /// <value>The <see cref="System.Reflection.MethodInfo" /> that represents the called method.</value>
        public MethodInfo Method { get; }

        /// <summary>Gets the <see cref="System.Linq.Expressions.Expression" /> that represents the instance for instance method calls or null for static method calls.</summary>
        /// <value>An <see cref="System.Linq.Expressions.Expression" /> that represents the receiving object of the method.</value>
        /// <remarks>If a <see cref="System.Linq.Expressions.MethodCallExpression" /> object's <see cref="O:System.Linq.Expressions.MethodCallExpression.Method" /> property represents a <see langword="static" /> (`Shared` in Visual Basic) method, the <see cref="O:System.Linq.Expressions.MethodCallExpression.Object" /> property is <see langword="null" />.</remarks>
        public Expression? Object => GetInstance();

        /// <summary>Gets a collection of expressions that represent arguments of the called method.</summary>
        /// <value>A <see cref="System.Collections.ObjectModel.ReadOnlyCollection{T}" /> of <see cref="System.Linq.Expressions.Expression" /> objects which represent the arguments to the called method.</value>
        public ReadOnlyCollection<Expression> Arguments => GetOrMakeArguments();

        /// <summary>Creates a new expression that is like this one, but using the supplied children. If all of the children are the same, it will return this expression.</summary>
        /// <param name="object">The <see cref="System.Linq.Expressions.MethodCallExpression.Object" /> property of the result.</param>
        /// <param name="arguments">The <see cref="System.Linq.Expressions.MethodCallExpression.Arguments" /> property of the result.</param>
        /// <returns>This expression if no children are changed or an expression with the updated children.</returns>
        public MethodCallExpression Update(Expression? @object, IEnumerable<Expression>? arguments)
        {
            if (@object == Object)
            {
                // Ensure arguments is safe to enumerate twice.
                // (If this means a second call to ToReadOnly it will return quickly).
                ICollection<Expression>? args;
                if (arguments == null)
                {
                    args = null;
                }
                else
                {
                    args = arguments as ICollection<Expression>;
                    if (args == null)
                    {
                        arguments = args = arguments.ToReadOnly();
                    }
                }

                if (SameArguments(args))
                {
                    return this;
                }
            }

            return Call(@object, Method, arguments);
        }

        [ExcludeFromCodeCoverage(Justification = "Unreachable")]
        internal virtual bool SameArguments(ICollection<Expression>? arguments)
        {
            throw ContractUtils.Unreachable;
        }

        [ExcludeFromCodeCoverage(Justification = "Unreachable")]
        internal virtual ReadOnlyCollection<Expression> GetOrMakeArguments()
        {
            throw ContractUtils.Unreachable;
        }

        /// <summary>Dispatches to the specific visit method for this node type. For example, <see cref="System.Linq.Expressions.MethodCallExpression" /> calls the <see cref="System.Linq.Expressions.ExpressionVisitor.VisitMethodCall(System.Linq.Expressions.MethodCallExpression)" />.</summary>
        /// <param name="visitor">The visitor to visit this node with.</param>
        /// <returns>The result of visiting this node.</returns>
        /// <remarks>This default implementation for <see cref="System.Linq.Expressions.ExpressionType.Extension" /> nodes calls <see cref="O:System.Linq.Expressions.ExpressionVisitor.VisitExtension" />. Override this method to call into a more specific method on a derived visitor class of the <see cref="System.Linq.Expressions.ExpressionVisitor" /> class. However, it should still support unknown visitors by calling <see cref="O:System.Linq.Expressions.ExpressionVisitor.VisitExtension" />.</remarks>
        protected internal override Expression Accept(ExpressionVisitor visitor)
        {
            return visitor.VisitMethodCall(this);
        }

        /// <summary>
        /// Returns a new MethodCallExpression replacing the existing instance/args with the
        /// newly provided instance and args.    Arguments can be null to use the existing
        /// arguments.
        ///
        /// This helper is provided to allow re-writing of nodes to not depend on the specific optimized
        /// subclass of MethodCallExpression which is being used.
        /// </summary>
        [ExcludeFromCodeCoverage(Justification = "Unreachable")]
        internal virtual MethodCallExpression Rewrite(Expression instance, IReadOnlyList<Expression>? args)
        {
            throw ContractUtils.Unreachable;
        }

        #region IArgumentProvider Members

        /// <summary>
        /// Gets the argument expression with the specified <paramref name="index"/>.
        /// </summary>
        /// <param name="index">The index of the argument expression to get.</param>
        /// <returns>The expression representing the argument at the specified <paramref name="index"/>.</returns>
        [ExcludeFromCodeCoverage(Justification = "Unreachable")]
        public virtual Expression GetArgument(int index)
        {
            throw ContractUtils.Unreachable;
        }

        /// <summary>
        /// Gets the number of argument expressions of the node.
        /// </summary>
        [ExcludeFromCodeCoverage(Justification = "Unreachable")]
        public virtual int ArgumentCount
        {
            get { throw ContractUtils.Unreachable; }
        }

        #endregion
    }

    #region Specialized Subclasses

    internal class InstanceMethodCallExpression : MethodCallExpression, IArgumentProvider
    {
        private readonly Expression _instance;

        public InstanceMethodCallExpression(MethodInfo method, Expression instance)
            : base(method)
        {
            Debug.Assert(instance != null);

            _instance = instance;
        }

        internal override Expression GetInstance() => _instance;
    }

    internal sealed class MethodCallExpressionN : MethodCallExpression, IArgumentProvider
    {
        private IReadOnlyList<Expression> _arguments;

        public MethodCallExpressionN(MethodInfo method, IReadOnlyList<Expression> args)
            : base(method)
        {
            _arguments = args;
        }

        public override Expression GetArgument(int index) => _arguments[index];

        public override int ArgumentCount => _arguments.Count;

        internal override ReadOnlyCollection<Expression> GetOrMakeArguments()
        {
            return ExpressionUtils.ReturnReadOnly(ref _arguments);
        }

        internal override bool SameArguments(ICollection<Expression>? arguments) =>
            ExpressionUtils.SameElements(arguments, _arguments);

        internal override MethodCallExpression Rewrite(Expression? instance, IReadOnlyList<Expression>? args)
        {
            Debug.Assert(instance == null);
            Debug.Assert(args == null || args.Count == _arguments.Count);

            return Expression.Call(Method, args ?? _arguments);
        }
    }

    internal sealed class InstanceMethodCallExpressionN : InstanceMethodCallExpression, IArgumentProvider
    {
        private IReadOnlyList<Expression> _arguments;

        public InstanceMethodCallExpressionN(MethodInfo method, Expression instance, IReadOnlyList<Expression> args)
            : base(method, instance)
        {
            _arguments = args;
        }

        public override Expression GetArgument(int index) => _arguments[index];

        public override int ArgumentCount => _arguments.Count;

        internal override bool SameArguments(ICollection<Expression>? arguments) =>
            ExpressionUtils.SameElements(arguments, _arguments);

        internal override ReadOnlyCollection<Expression> GetOrMakeArguments()
        {
            return ExpressionUtils.ReturnReadOnly(ref _arguments);
        }

        internal override MethodCallExpression Rewrite(Expression instance, IReadOnlyList<Expression>? args)
        {
            Debug.Assert(instance != null);
            Debug.Assert(args == null || args.Count == _arguments.Count);

            return Expression.Call(instance, Method, args ?? _arguments);
        }
    }

    internal sealed class MethodCallExpression0 : MethodCallExpression, IArgumentProvider
    {
        public MethodCallExpression0(MethodInfo method)
            : base(method)
        {
        }

        public override Expression GetArgument(int index)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        public override int ArgumentCount => 0;

        internal override ReadOnlyCollection<Expression> GetOrMakeArguments()
        {
            return EmptyReadOnlyCollection<Expression>.Instance;
        }

        internal override bool SameArguments(ICollection<Expression>? arguments) =>
            arguments == null || arguments.Count == 0;

        internal override MethodCallExpression Rewrite(Expression? instance, IReadOnlyList<Expression>? args)
        {
            Debug.Assert(instance == null);
            Debug.Assert(args == null || args.Count == 0);

            return Expression.Call(Method);
        }
    }

    internal sealed class MethodCallExpression1 : MethodCallExpression, IArgumentProvider
    {
        private object _arg0;       // storage for the 1st argument or a read-only collection.  See IArgumentProvider

        public MethodCallExpression1(MethodInfo method, Expression arg0)
            : base(method)
        {
            _arg0 = arg0;
        }

        public override Expression GetArgument(int index) =>
            index switch
            {
                0 => ExpressionUtils.ReturnObject<Expression>(_arg0),
                _ => throw new ArgumentOutOfRangeException(nameof(index)),
            };

        public override int ArgumentCount => 1;

        internal override ReadOnlyCollection<Expression> GetOrMakeArguments()
        {
            return ExpressionUtils.ReturnReadOnly(this, ref _arg0);
        }

        internal override bool SameArguments(ICollection<Expression>? arguments)
        {
            if (arguments != null && arguments.Count == 1)
            {
                using (IEnumerator<Expression> en = arguments.GetEnumerator())
                {
                    en.MoveNext();
                    return en.Current == ExpressionUtils.ReturnObject<Expression>(_arg0);
                }
            }

            return false;
        }

        internal override MethodCallExpression Rewrite(Expression? instance, IReadOnlyList<Expression>? args)
        {
            Debug.Assert(instance == null);
            Debug.Assert(args == null || args.Count == 1);

            if (args != null)
            {
                return Expression.Call(Method, args[0]);
            }

            return Expression.Call(Method, ExpressionUtils.ReturnObject<Expression>(_arg0));
        }
    }

    internal sealed class MethodCallExpression2 : MethodCallExpression, IArgumentProvider
    {
        private object _arg0;               // storage for the 1st argument or a read-only collection.  See IArgumentProvider
        private readonly Expression _arg1;  // storage for the 2nd arg

        public MethodCallExpression2(MethodInfo method, Expression arg0, Expression arg1)
            : base(method)
        {
            _arg0 = arg0;
            _arg1 = arg1;
        }

        public override Expression GetArgument(int index)
        {
            return index switch
            {
                0 => ExpressionUtils.ReturnObject<Expression>(_arg0),
                1 => _arg1,
                _ => throw new ArgumentOutOfRangeException(nameof(index)),
            };
        }

        public override int ArgumentCount => 2;

        internal override bool SameArguments(ICollection<Expression>? arguments)
        {
            if (arguments != null && arguments.Count == 2)
            {
                if (_arg0 is ReadOnlyCollection<Expression> alreadyCollection)
                {
                    return ExpressionUtils.SameElements(arguments, alreadyCollection);
                }

                using (IEnumerator<Expression> en = arguments.GetEnumerator())
                {
                    en.MoveNext();
                    if (en.Current == _arg0)
                    {
                        en.MoveNext();
                        return en.Current == _arg1;
                    }
                }
            }

            return false;
        }

        internal override ReadOnlyCollection<Expression> GetOrMakeArguments()
        {
            return ExpressionUtils.ReturnReadOnly(this, ref _arg0);
        }

        internal override MethodCallExpression Rewrite(Expression? instance, IReadOnlyList<Expression>? args)
        {
            Debug.Assert(instance == null);
            Debug.Assert(args == null || args.Count == 2);

            if (args != null)
            {
                return Expression.Call(Method, args[0], args[1]);
            }
            return Expression.Call(Method, ExpressionUtils.ReturnObject<Expression>(_arg0), _arg1);
        }
    }

    internal sealed class MethodCallExpression3 : MethodCallExpression, IArgumentProvider
    {
        private object _arg0;           // storage for the 1st argument or a read-only collection.  See IArgumentProvider
        private readonly Expression _arg1, _arg2; // storage for the 2nd - 3rd args.

        public MethodCallExpression3(MethodInfo method, Expression arg0, Expression arg1, Expression arg2)
            : base(method)
        {
            _arg0 = arg0;
            _arg1 = arg1;
            _arg2 = arg2;
        }

        public override Expression GetArgument(int index)
        {
            return index switch
            {
                0 => ExpressionUtils.ReturnObject<Expression>(_arg0),
                1 => _arg1,
                2 => _arg2,
                _ => throw new ArgumentOutOfRangeException(nameof(index)),
            };
        }

        public override int ArgumentCount => 3;

        internal override bool SameArguments(ICollection<Expression>? arguments)
        {
            if (arguments != null && arguments.Count == 3)
            {
                if (_arg0 is ReadOnlyCollection<Expression> alreadyCollection)
                {
                    return ExpressionUtils.SameElements(arguments, alreadyCollection);
                }

                using (IEnumerator<Expression> en = arguments.GetEnumerator())
                {
                    en.MoveNext();
                    if (en.Current == _arg0)
                    {
                        en.MoveNext();
                        if (en.Current == _arg1)
                        {
                            en.MoveNext();
                            return en.Current == _arg2;
                        }
                    }
                }
            }

            return false;
        }

        internal override ReadOnlyCollection<Expression> GetOrMakeArguments()
        {
            return ExpressionUtils.ReturnReadOnly(this, ref _arg0);
        }

        internal override MethodCallExpression Rewrite(Expression? instance, IReadOnlyList<Expression>? args)
        {
            Debug.Assert(instance == null);
            Debug.Assert(args == null || args.Count == 3);

            if (args != null)
            {
                return Expression.Call(Method, args[0], args[1], args[2]);
            }
            return Expression.Call(Method, ExpressionUtils.ReturnObject<Expression>(_arg0), _arg1, _arg2);
        }
    }

    internal sealed class MethodCallExpression4 : MethodCallExpression, IArgumentProvider
    {
        private object _arg0;               // storage for the 1st argument or a read-only collection.  See IArgumentProvider
        private readonly Expression _arg1, _arg2, _arg3;  // storage for the 2nd - 4th args.

        public MethodCallExpression4(MethodInfo method, Expression arg0, Expression arg1, Expression arg2, Expression arg3)
            : base(method)
        {
            _arg0 = arg0;
            _arg1 = arg1;
            _arg2 = arg2;
            _arg3 = arg3;
        }

        public override Expression GetArgument(int index)
        {
            return index switch
            {
                0 => ExpressionUtils.ReturnObject<Expression>(_arg0),
                1 => _arg1,
                2 => _arg2,
                3 => _arg3,
                _ => throw new ArgumentOutOfRangeException(nameof(index)),
            };
        }

        public override int ArgumentCount => 4;

        internal override bool SameArguments(ICollection<Expression>? arguments)
        {
            if (arguments != null && arguments.Count == 4)
            {
                if (_arg0 is ReadOnlyCollection<Expression> alreadyCollection)
                {
                    return ExpressionUtils.SameElements(arguments, alreadyCollection);
                }

                using (IEnumerator<Expression> en = arguments.GetEnumerator())
                {
                    en.MoveNext();
                    if (en.Current == _arg0)
                    {
                        en.MoveNext();
                        if (en.Current == _arg1)
                        {
                            en.MoveNext();
                            if (en.Current == _arg2)
                            {
                                en.MoveNext();
                                return en.Current == _arg3;
                            }
                        }
                    }
                }
            }

            return false;
        }

        internal override ReadOnlyCollection<Expression> GetOrMakeArguments()
        {
            return ExpressionUtils.ReturnReadOnly(this, ref _arg0);
        }

        internal override MethodCallExpression Rewrite(Expression? instance, IReadOnlyList<Expression>? args)
        {
            Debug.Assert(instance == null);
            Debug.Assert(args == null || args.Count == 4);

            if (args != null)
            {
                return Expression.Call(Method, args[0], args[1], args[2], args[3]);
            }
            return Expression.Call(Method, ExpressionUtils.ReturnObject<Expression>(_arg0), _arg1, _arg2, _arg3);
        }
    }

    internal sealed class MethodCallExpression5 : MethodCallExpression, IArgumentProvider
    {
        private object _arg0;           // storage for the 1st argument or a read-only collection.  See IArgumentProvider
        private readonly Expression _arg1, _arg2, _arg3, _arg4;   // storage for the 2nd - 5th args.

        public MethodCallExpression5(MethodInfo method, Expression arg0, Expression arg1, Expression arg2, Expression arg3, Expression arg4)
            : base(method)
        {
            _arg0 = arg0;
            _arg1 = arg1;
            _arg2 = arg2;
            _arg3 = arg3;
            _arg4 = arg4;
        }

        public override Expression GetArgument(int index)
        {
            return index switch
            {
                0 => ExpressionUtils.ReturnObject<Expression>(_arg0),
                1 => _arg1,
                2 => _arg2,
                3 => _arg3,
                4 => _arg4,
                _ => throw new ArgumentOutOfRangeException(nameof(index)),
            };
        }

        public override int ArgumentCount => 5;

        internal override bool SameArguments(ICollection<Expression>? arguments)
        {
            if (arguments != null && arguments.Count == 5)
            {
                if (_arg0 is ReadOnlyCollection<Expression> alreadyCollection)
                {
                    return ExpressionUtils.SameElements(arguments, alreadyCollection);
                }

                using (IEnumerator<Expression> en = arguments.GetEnumerator())
                {
                    en.MoveNext();
                    if (en.Current == _arg0)
                    {
                        en.MoveNext();
                        if (en.Current == _arg1)
                        {
                            en.MoveNext();
                            if (en.Current == _arg2)
                            {
                                en.MoveNext();
                                if (en.Current == _arg3)
                                {
                                    en.MoveNext();
                                    return en.Current == _arg4;
                                }
                            }
                        }
                    }
                }
            }

            return false;
        }

        internal override ReadOnlyCollection<Expression> GetOrMakeArguments()
        {
            return ExpressionUtils.ReturnReadOnly(this, ref _arg0);
        }

        internal override MethodCallExpression Rewrite(Expression? instance, IReadOnlyList<Expression>? args)
        {
            Debug.Assert(instance == null);
            Debug.Assert(args == null || args.Count == 5);

            if (args != null)
            {
                return Expression.Call(Method, args[0], args[1], args[2], args[3], args[4]);
            }

            return Expression.Call(Method, ExpressionUtils.ReturnObject<Expression>(_arg0), _arg1, _arg2, _arg3, _arg4);
        }
    }

    internal sealed class InstanceMethodCallExpression0 : InstanceMethodCallExpression, IArgumentProvider
    {
        public InstanceMethodCallExpression0(MethodInfo method, Expression instance)
            : base(method, instance)
        {
        }

        public override Expression GetArgument(int index)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        public override int ArgumentCount => 0;

        internal override ReadOnlyCollection<Expression> GetOrMakeArguments()
        {
            return EmptyReadOnlyCollection<Expression>.Instance;
        }

        internal override bool SameArguments(ICollection<Expression>? arguments) =>
            arguments == null || arguments.Count == 0;

        internal override MethodCallExpression Rewrite(Expression instance, IReadOnlyList<Expression>? args)
        {
            Debug.Assert(instance != null);
            Debug.Assert(args == null || args.Count == 0);

            return Expression.Call(instance, Method);
        }
    }

    internal sealed class InstanceMethodCallExpression1 : InstanceMethodCallExpression, IArgumentProvider
    {
        private object _arg0;                // storage for the 1st argument or a read-only collection.  See IArgumentProvider

        public InstanceMethodCallExpression1(MethodInfo method, Expression instance, Expression arg0)
            : base(method, instance)
        {
            _arg0 = arg0;
        }

        public override Expression GetArgument(int index)
        {
            return index switch
            {
                0 => ExpressionUtils.ReturnObject<Expression>(_arg0),
                _ => throw new ArgumentOutOfRangeException(nameof(index)),
            };
        }

        public override int ArgumentCount => 1;

        internal override bool SameArguments(ICollection<Expression>? arguments)
        {
            if (arguments != null && arguments.Count == 1)
            {
                using (IEnumerator<Expression> en = arguments.GetEnumerator())
                {
                    en.MoveNext();
                    return en.Current == ExpressionUtils.ReturnObject<Expression>(_arg0);
                }
            }

            return false;
        }

        internal override ReadOnlyCollection<Expression> GetOrMakeArguments()
        {
            return ExpressionUtils.ReturnReadOnly(this, ref _arg0);
        }

        internal override MethodCallExpression Rewrite(Expression instance, IReadOnlyList<Expression>? args)
        {
            Debug.Assert(instance != null);
            Debug.Assert(args == null || args.Count == 1);

            if (args != null)
            {
                return Expression.Call(instance, Method, args[0]);
            }
            return Expression.Call(instance, Method, ExpressionUtils.ReturnObject<Expression>(_arg0));
        }
    }

    internal sealed class InstanceMethodCallExpression2 : InstanceMethodCallExpression, IArgumentProvider
    {
        private object _arg0;                // storage for the 1st argument or a read-only collection.  See IArgumentProvider
        private readonly Expression _arg1;   // storage for the 2nd argument

        public InstanceMethodCallExpression2(MethodInfo method, Expression instance, Expression arg0, Expression arg1)
            : base(method, instance)
        {
            _arg0 = arg0;
            _arg1 = arg1;
        }

        public override Expression GetArgument(int index)
        {
            return index switch
            {
                0 => ExpressionUtils.ReturnObject<Expression>(_arg0),
                1 => _arg1,
                _ => throw new ArgumentOutOfRangeException(nameof(index)),
            };
        }

        public override int ArgumentCount => 2;

        internal override bool SameArguments(ICollection<Expression>? arguments)
        {
            if (arguments != null && arguments.Count == 2)
            {
                if (_arg0 is ReadOnlyCollection<Expression> alreadyCollection)
                {
                    return ExpressionUtils.SameElements(arguments, alreadyCollection);
                }

                using (IEnumerator<Expression> en = arguments.GetEnumerator())
                {
                    en.MoveNext();
                    if (en.Current == _arg0)
                    {
                        en.MoveNext();
                        return en.Current == _arg1;
                    }
                }
            }

            return false;
        }

        internal override ReadOnlyCollection<Expression> GetOrMakeArguments()
        {
            return ExpressionUtils.ReturnReadOnly(this, ref _arg0);
        }

        internal override MethodCallExpression Rewrite(Expression instance, IReadOnlyList<Expression>? args)
        {
            Debug.Assert(instance != null);
            Debug.Assert(args == null || args.Count == 2);

            if (args != null)
            {
                return Expression.Call(instance, Method, args[0], args[1]);
            }
            return Expression.Call(instance, Method, ExpressionUtils.ReturnObject<Expression>(_arg0), _arg1);
        }
    }

    internal sealed class InstanceMethodCallExpression3 : InstanceMethodCallExpression, IArgumentProvider
    {
        private object _arg0;                       // storage for the 1st argument or a read-only collection.  See IArgumentProvider
        private readonly Expression _arg1, _arg2;   // storage for the 2nd - 3rd argument

        public InstanceMethodCallExpression3(MethodInfo method, Expression instance, Expression arg0, Expression arg1, Expression arg2)
            : base(method, instance)
        {
            _arg0 = arg0;
            _arg1 = arg1;
            _arg2 = arg2;
        }

        public override Expression GetArgument(int index)
        {
            return index switch
            {
                0 => ExpressionUtils.ReturnObject<Expression>(_arg0),
                1 => _arg1,
                2 => _arg2,
                _ => throw new ArgumentOutOfRangeException(nameof(index)),
            };
        }

        public override int ArgumentCount => 3;

        internal override bool SameArguments(ICollection<Expression>? arguments)
        {
            if (arguments != null && arguments.Count == 3)
            {
                if (_arg0 is ReadOnlyCollection<Expression> alreadyCollection)
                {
                    return ExpressionUtils.SameElements(arguments, alreadyCollection);
                }

                using (IEnumerator<Expression> en = arguments.GetEnumerator())
                {
                    en.MoveNext();
                    if (en.Current == _arg0)
                    {
                        en.MoveNext();
                        if (en.Current == _arg1)
                        {
                            en.MoveNext();
                            return en.Current == _arg2;
                        }
                    }
                }
            }

            return false;
        }

        internal override ReadOnlyCollection<Expression> GetOrMakeArguments()
        {
            return ExpressionUtils.ReturnReadOnly(this, ref _arg0);
        }

        internal override MethodCallExpression Rewrite(Expression instance, IReadOnlyList<Expression>? args)
        {
            Debug.Assert(instance != null);
            Debug.Assert(args == null || args.Count == 3);

            if (args != null)
            {
                return Expression.Call(instance, Method, args[0], args[1], args[2]);
            }
            return Expression.Call(instance, Method, ExpressionUtils.ReturnObject<Expression>(_arg0), _arg1, _arg2);
        }
    }

    #endregion

    /// <summary>Provides the base class from which the classes that represent expression tree nodes are derived. It also contains <see langword="static" /> (<see langword="Shared" /> in Visual Basic) factory methods to create the various node types. This is an <see langword="abstract" /> class.</summary>
    /// <remarks></remarks>
    /// <example>The following code example shows how to create a block expression. The block expression consists of two <see cref="System.Linq.Expressions.MethodCallExpression" /> objects and one <see cref="System.Linq.Expressions.ConstantExpression" /> object.
    /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/cs/program.cs" id="Snippet13":::
    /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/vb/module1.vb" id="Snippet13":::</example>
    public partial class Expression
    {
        #region Call

        /// <summary>Creates a <see cref="MethodCallExpression"/> that represents a call to a static method that takes no arguments.</summary>
        /// <returns>A <see cref="MethodCallExpression"/> that has the <see cref="NodeType"/> property equal to <see cref="ExpressionType.Call"/> and the <see cref="MethodCallExpression.Object"/> and <see cref="MethodCallExpression.Method"/> properties set to the specified values.</returns>
        /// <param name="method">A <see cref="MethodInfo"/> to set the <see cref="MethodCallExpression.Method"/> property equal to.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="method"/> is null.</exception>
        internal static MethodCallExpression Call(MethodInfo method)
        {
            // NB: Keeping this method as internal to avoid public API changes; it's internal for MethodCallExpression0.Rewrite.

            ContractUtils.RequiresNotNull(method, nameof(method));

            ParameterInfo[] pis = ValidateMethodAndGetParameters(null, method);

            ValidateArgumentCount(method, ExpressionType.Call, 0, pis);

            return new MethodCallExpression0(method);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.MethodCallExpression" /> that represents a call to a <see langword="static" /> (<see langword="Shared" /> in Visual Basic) method that takes one argument.</summary>
        /// <param name="method">A <see cref="System.Reflection.MethodInfo" /> to set the <see cref="System.Linq.Expressions.MethodCallExpression.Method" /> property equal to.</param>
        /// <param name="arg0">The <see cref="System.Linq.Expressions.Expression" /> that represents the first argument.</param>
        /// <returns>A <see cref="System.Linq.Expressions.MethodCallExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.Call" /> and the <see cref="System.Linq.Expressions.MethodCallExpression.Object" /> and <see cref="System.Linq.Expressions.MethodCallExpression.Method" /> properties set to the specified values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="method" /> is null.</exception>
        /// <remarks></remarks>
        /// <example>The following example demonstrates how to create an expression that calls a <see langword="static" /> (`Shared` in Visual Basic) method that takes one argument.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/cs/program.cs" id="Snippet16":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/vb/module1.vb" id="Snippet16":::</example>
        public static MethodCallExpression Call(MethodInfo method, Expression arg0)
        {
            ContractUtils.RequiresNotNull(method, nameof(method));
            ContractUtils.RequiresNotNull(arg0, nameof(arg0));

            ParameterInfo[] pis = ValidateMethodAndGetParameters(null, method);

            ValidateArgumentCount(method, ExpressionType.Call, 1, pis);

            arg0 = ValidateOneArgument(method, ExpressionType.Call, arg0, pis[0], nameof(method), nameof(arg0));

            return new MethodCallExpression1(method, arg0);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.MethodCallExpression" /> that represents a call to a static method that takes two arguments.</summary>
        /// <param name="method">A <see cref="System.Reflection.MethodInfo" /> to set the <see cref="System.Linq.Expressions.MethodCallExpression.Method" /> property equal to.</param>
        /// <param name="arg0">The <see cref="System.Linq.Expressions.Expression" /> that represents the first argument.</param>
        /// <param name="arg1">The <see cref="System.Linq.Expressions.Expression" /> that represents the second argument.</param>
        /// <returns>A <see cref="System.Linq.Expressions.MethodCallExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.Call" /> and the <see cref="System.Linq.Expressions.MethodCallExpression.Object" /> and <see cref="System.Linq.Expressions.MethodCallExpression.Method" /> properties set to the specified values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="method" /> is null.</exception>
        public static MethodCallExpression Call(MethodInfo method, Expression arg0, Expression arg1)
        {
            ContractUtils.RequiresNotNull(method, nameof(method));
            ContractUtils.RequiresNotNull(arg0, nameof(arg0));
            ContractUtils.RequiresNotNull(arg1, nameof(arg1));

            ParameterInfo[] pis = ValidateMethodAndGetParameters(null, method);

            ValidateArgumentCount(method, ExpressionType.Call, 2, pis);

            arg0 = ValidateOneArgument(method, ExpressionType.Call, arg0, pis[0], nameof(method), nameof(arg0));
            arg1 = ValidateOneArgument(method, ExpressionType.Call, arg1, pis[1], nameof(method), nameof(arg1));

            return new MethodCallExpression2(method, arg0, arg1);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.MethodCallExpression" /> that represents a call to a static method that takes three arguments.</summary>
        /// <param name="method">A <see cref="System.Reflection.MethodInfo" /> to set the <see cref="System.Linq.Expressions.MethodCallExpression.Method" /> property equal to.</param>
        /// <param name="arg0">The <see cref="System.Linq.Expressions.Expression" /> that represents the first argument.</param>
        /// <param name="arg1">The <see cref="System.Linq.Expressions.Expression" /> that represents the second argument.</param>
        /// <param name="arg2">The <see cref="System.Linq.Expressions.Expression" /> that represents the third argument.</param>
        /// <returns>A <see cref="System.Linq.Expressions.MethodCallExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.Call" /> and the <see cref="System.Linq.Expressions.MethodCallExpression.Object" /> and <see cref="System.Linq.Expressions.MethodCallExpression.Method" /> properties set to the specified values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="method" /> is null.</exception>
        public static MethodCallExpression Call(MethodInfo method, Expression arg0, Expression arg1, Expression arg2)
        {
            ContractUtils.RequiresNotNull(method, nameof(method));
            ContractUtils.RequiresNotNull(arg0, nameof(arg0));
            ContractUtils.RequiresNotNull(arg1, nameof(arg1));
            ContractUtils.RequiresNotNull(arg2, nameof(arg2));

            ParameterInfo[] pis = ValidateMethodAndGetParameters(null, method);

            ValidateArgumentCount(method, ExpressionType.Call, 3, pis);

            arg0 = ValidateOneArgument(method, ExpressionType.Call, arg0, pis[0], nameof(method), nameof(arg0));
            arg1 = ValidateOneArgument(method, ExpressionType.Call, arg1, pis[1], nameof(method), nameof(arg1));
            arg2 = ValidateOneArgument(method, ExpressionType.Call, arg2, pis[2], nameof(method), nameof(arg2));

            return new MethodCallExpression3(method, arg0, arg1, arg2);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.MethodCallExpression" /> that represents a call to a static method that takes four arguments.</summary>
        /// <param name="method">A <see cref="System.Reflection.MethodInfo" /> to set the <see cref="System.Linq.Expressions.MethodCallExpression.Method" /> property equal to.</param>
        /// <param name="arg0">The <see cref="System.Linq.Expressions.Expression" /> that represents the first argument.</param>
        /// <param name="arg1">The <see cref="System.Linq.Expressions.Expression" /> that represents the second argument.</param>
        /// <param name="arg2">The <see cref="System.Linq.Expressions.Expression" /> that represents the third argument.</param>
        /// <param name="arg3">The <see cref="System.Linq.Expressions.Expression" /> that represents the fourth argument.</param>
        /// <returns>A <see cref="System.Linq.Expressions.MethodCallExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.Call" /> and the <see cref="System.Linq.Expressions.MethodCallExpression.Object" /> and <see cref="System.Linq.Expressions.MethodCallExpression.Method" /> properties set to the specified values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="method" /> is null.</exception>
        public static MethodCallExpression Call(MethodInfo method, Expression arg0, Expression arg1, Expression arg2, Expression arg3)
        {
            ContractUtils.RequiresNotNull(method, nameof(method));
            ContractUtils.RequiresNotNull(arg0, nameof(arg0));
            ContractUtils.RequiresNotNull(arg1, nameof(arg1));
            ContractUtils.RequiresNotNull(arg2, nameof(arg2));
            ContractUtils.RequiresNotNull(arg3, nameof(arg3));

            ParameterInfo[] pis = ValidateMethodAndGetParameters(null, method);

            ValidateArgumentCount(method, ExpressionType.Call, 4, pis);

            arg0 = ValidateOneArgument(method, ExpressionType.Call, arg0, pis[0], nameof(method), nameof(arg0));
            arg1 = ValidateOneArgument(method, ExpressionType.Call, arg1, pis[1], nameof(method), nameof(arg1));
            arg2 = ValidateOneArgument(method, ExpressionType.Call, arg2, pis[2], nameof(method), nameof(arg2));
            arg3 = ValidateOneArgument(method, ExpressionType.Call, arg3, pis[3], nameof(method), nameof(arg3));

            return new MethodCallExpression4(method, arg0, arg1, arg2, arg3);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.MethodCallExpression" /> that represents a call to a static method that takes five arguments.</summary>
        /// <param name="method">A <see cref="System.Reflection.MethodInfo" /> to set the <see cref="System.Linq.Expressions.MethodCallExpression.Method" /> property equal to.</param>
        /// <param name="arg0">The <see cref="System.Linq.Expressions.Expression" /> that represents the first argument.</param>
        /// <param name="arg1">The <see cref="System.Linq.Expressions.Expression" /> that represents the second argument.</param>
        /// <param name="arg2">The <see cref="System.Linq.Expressions.Expression" /> that represents the third argument.</param>
        /// <param name="arg3">The <see cref="System.Linq.Expressions.Expression" /> that represents the fourth argument.</param>
        /// <param name="arg4">The <see cref="System.Linq.Expressions.Expression" /> that represents the fifth argument.</param>
        /// <returns>A <see cref="System.Linq.Expressions.MethodCallExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.Call" /> and the <see cref="System.Linq.Expressions.MethodCallExpression.Object" /> and <see cref="System.Linq.Expressions.MethodCallExpression.Method" /> properties set to the specified values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="method" /> is null.</exception>
        public static MethodCallExpression Call(MethodInfo method, Expression arg0, Expression arg1, Expression arg2, Expression arg3, Expression arg4)
        {
            ContractUtils.RequiresNotNull(method, nameof(method));
            ContractUtils.RequiresNotNull(arg0, nameof(arg0));
            ContractUtils.RequiresNotNull(arg1, nameof(arg1));
            ContractUtils.RequiresNotNull(arg2, nameof(arg2));
            ContractUtils.RequiresNotNull(arg3, nameof(arg3));
            ContractUtils.RequiresNotNull(arg4, nameof(arg4));

            ParameterInfo[] pis = ValidateMethodAndGetParameters(null, method);

            ValidateArgumentCount(method, ExpressionType.Call, 5, pis);

            arg0 = ValidateOneArgument(method, ExpressionType.Call, arg0, pis[0], nameof(method), nameof(arg0));
            arg1 = ValidateOneArgument(method, ExpressionType.Call, arg1, pis[1], nameof(method), nameof(arg1));
            arg2 = ValidateOneArgument(method, ExpressionType.Call, arg2, pis[2], nameof(method), nameof(arg2));
            arg3 = ValidateOneArgument(method, ExpressionType.Call, arg3, pis[3], nameof(method), nameof(arg3));
            arg4 = ValidateOneArgument(method, ExpressionType.Call, arg4, pis[4], nameof(method), nameof(arg4));

            return new MethodCallExpression5(method, arg0, arg1, arg2, arg3, arg4);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.MethodCallExpression" /> that represents a call to a <see langword="static" /> (<see langword="Shared" /> in Visual Basic) method that has arguments.</summary>
        /// <param name="method">A <see cref="System.Reflection.MethodInfo" /> that represents a <see langword="static" /> (<see langword="Shared" /> in Visual Basic) method to set the <see cref="System.Linq.Expressions.MethodCallExpression.Method" /> property equal to.</param>
        /// <param name="arguments">An array of <see cref="System.Linq.Expressions.Expression" /> objects to use to populate the <see cref="System.Linq.Expressions.MethodCallExpression.Arguments" /> collection.</param>
        /// <returns>A <see cref="System.Linq.Expressions.MethodCallExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.Call" /> and the <see cref="System.Linq.Expressions.MethodCallExpression.Method" /> and <see cref="System.Linq.Expressions.MethodCallExpression.Arguments" /> properties set to the specified values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="method" /> is <see langword="null" />.</exception>
        /// <exception cref="System.ArgumentException">The number of elements in <paramref name="arguments" /> does not equal the number of parameters for the method represented by <paramref name="method" />.
        /// -or-
        /// One or more of the elements of <paramref name="arguments" /> is not assignable to the corresponding parameter for the method represented by <paramref name="method" />.</exception>
        /// <remarks>If <paramref name="arguments" /> is not <see langword="null" />, it must have the same number of elements as the number of parameters for the method represented by <paramref name="method" />. Each element in <paramref name="arguments" /> must not be <see langword="null" /> and must be assignable to the corresponding parameter of <paramref name="method" />, possibly after *quoting*.
        /// <format type="text/markdown"><![CDATA[
        /// > [!NOTE]
        /// >  An element will be quoted only if the corresponding method parameter is of type <xref:System.Linq.Expressions.Expression>. Quoting means the element is wrapped in a <xref:System.Linq.Expressions.ExpressionType.Quote> node. The resulting node is a <xref:System.Linq.Expressions.UnaryExpression> whose <xref:System.Linq.Expressions.UnaryExpression.Operand%2A> property is the element of `arguments`.
        /// ]]></format>
        /// The <see cref="O:System.Linq.Expressions.MethodCallExpression.Arguments" /> property of the resulting <see cref="System.Linq.Expressions.MethodCallExpression" /> is empty if <paramref name="arguments" /> is <see langword="null" />. Otherwise, it contains the same elements as <paramref name="arguments" />, some of which may be quoted.
        /// The <see cref="O:System.Linq.Expressions.Expression.Type" /> property of the resulting <see cref="System.Linq.Expressions.MethodCallExpression" /> is equal to the return type of the method represented by <paramref name="method" />. The <see cref="O:System.Linq.Expressions.MethodCallExpression.Object" /> property is <see langword="null" />.</remarks>
        public static MethodCallExpression Call(MethodInfo method, params Expression[]? arguments)
        {
            return Call(null, method, arguments);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.MethodCallExpression" /> that represents a call to a static (Shared in Visual Basic) method.</summary>
        /// <param name="method">The <see cref="System.Reflection.MethodInfo" /> that represents the target method.</param>
        /// <param name="arguments">A collection of <see cref="System.Linq.Expressions.Expression" /> that represents the call arguments.</param>
        /// <returns>A <see cref="System.Linq.Expressions.MethodCallExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.Call" /> and the <see cref="System.Linq.Expressions.MethodCallExpression.Object" /> and <see cref="System.Linq.Expressions.MethodCallExpression.Method" /> properties set to the specified values.</returns>
        public static MethodCallExpression Call(MethodInfo method, IEnumerable<Expression>? arguments)
        {
            return Call(null, method, arguments);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.MethodCallExpression" /> that represents a call to a method that takes no arguments.</summary>
        /// <param name="instance">An <see cref="System.Linq.Expressions.Expression" /> that specifies the instance for an instance method call (pass <see langword="null" /> for a <see langword="static" /> (<see langword="Shared" /> in Visual Basic) method).</param>
        /// <param name="method">A <see cref="System.Reflection.MethodInfo" /> to set the <see cref="System.Linq.Expressions.MethodCallExpression.Method" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.MethodCallExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.Call" /> and the <see cref="System.Linq.Expressions.MethodCallExpression.Object" /> and <see cref="System.Linq.Expressions.MethodCallExpression.Method" /> properties set to the specified values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="method" /> is <see langword="null" />.
        /// -or-
        /// <paramref name="instance" /> is <see langword="null" /> and <paramref name="method" /> represents an instance method.</exception>
        /// <exception cref="System.ArgumentException"><paramref name="instance" />.Type is not assignable to the declaring type of the method represented by <paramref name="method" />.</exception>
        /// <remarks>To represent a call to a <see langword="static" /> (`Shared` in Visual Basic) method, pass in <see langword="null" /> for the <paramref name="instance" /> parameter when you call this method.
        /// If <paramref name="method" /> represents an instance method, the <see cref="O:System.Linq.Expressions.Expression.Type" /> property of <paramref name="instance" /> must be assignable to the declaring type of the method represented by <paramref name="method" />.
        /// The <see cref="O:System.Linq.Expressions.MethodCallExpression.Arguments" /> property of the resulting <see cref="System.Linq.Expressions.MethodCallExpression" /> is empty. The <see cref="O:System.Linq.Expressions.Expression.Type" /> property is equal to the return type of the method represented by <paramref name="method" />.</remarks>
        /// <example>The following code example shows how to create an expression that calls a method without arguments.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/cs/program.cs" id="Snippet15":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/vb/module1.vb" id="Snippet15":::</example>
        public static MethodCallExpression Call(Expression? instance, MethodInfo method)
        {
            ContractUtils.RequiresNotNull(method, nameof(method));

            ParameterInfo[] pis = ValidateMethodAndGetParameters(instance, method);

            ValidateArgumentCount(method, ExpressionType.Call, 0, pis);

            if (instance != null)
            {
                return new InstanceMethodCallExpression0(method, instance);
            }

            return new MethodCallExpression0(method);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.MethodCallExpression" /> that represents a call to a method that takes arguments.</summary>
        /// <param name="instance">An <see cref="System.Linq.Expressions.Expression" /> that specifies the instance for an instance method call (pass <see langword="null" /> for a <see langword="static" /> (<see langword="Shared" /> in Visual Basic) method).</param>
        /// <param name="method">A <see cref="System.Reflection.MethodInfo" /> to set the <see cref="System.Linq.Expressions.MethodCallExpression.Method" /> property equal to.</param>
        /// <param name="arguments">An array of <see cref="System.Linq.Expressions.Expression" /> objects to use to populate the <see cref="System.Linq.Expressions.MethodCallExpression.Arguments" /> collection.</param>
        /// <returns>A <see cref="System.Linq.Expressions.MethodCallExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.Call" /> and the <see cref="System.Linq.Expressions.MethodCallExpression.Object" />, <see cref="System.Linq.Expressions.MethodCallExpression.Method" />, and <see cref="System.Linq.Expressions.MethodCallExpression.Arguments" /> properties set to the specified values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="method" /> is <see langword="null" />.
        /// -or-
        /// <paramref name="instance" /> is <see langword="null" /> and <paramref name="method" /> represents an instance method.
        /// -or-
        /// <paramref name="arguments" /> is not <see langword="null" /> and one or more of its elements is <see langword="null" />.</exception>
        /// <exception cref="System.ArgumentException"><paramref name="instance" />.Type is not assignable to the declaring type of the method represented by <paramref name="method" />.
        /// -or-
        /// The number of elements in <paramref name="arguments" /> does not equal the number of parameters for the method represented by <paramref name="method" />.
        /// -or-
        /// One or more of the elements of <paramref name="arguments" /> is not assignable to the corresponding parameter for the method represented by <paramref name="method" />.</exception>
        /// <remarks>To represent a call to a <see langword="static" /> (`Shared` in Visual Basic) method, pass in <see langword="null" /> for the <paramref name="instance" /> parameter when you call this method, or call <see cref="O:System.Linq.Expressions.Expression.Call" /> instead.
        /// If <paramref name="method" /> represents an instance method, the <see cref="O:System.Linq.Expressions.Expression.Type" /> property of <paramref name="instance" /> must be assignable to the declaring type of the method represented by <paramref name="method" />.
        /// If <paramref name="arguments" /> is not <see langword="null" />, it must have the same number of elements as the number of parameters for the method represented by <paramref name="method" />. Each element in <paramref name="arguments" /> must not be <see langword="null" /> and must be assignable to the corresponding parameter of <paramref name="method" />, possibly after *quoting*.
        /// <format type="text/markdown"><![CDATA[
        /// > [!NOTE]
        /// >  An element will be quoted only if the corresponding method parameter is of type <xref:System.Linq.Expressions.Expression>. Quoting means the element is wrapped in a <xref:System.Linq.Expressions.ExpressionType.Quote> node. The resulting node is a <xref:System.Linq.Expressions.UnaryExpression> whose <xref:System.Linq.Expressions.UnaryExpression.Operand%2A> property is the element of `arguments`.
        /// ]]></format>
        /// The <see cref="O:System.Linq.Expressions.MethodCallExpression.Arguments" /> property of the resulting <see cref="System.Linq.Expressions.MethodCallExpression" /> is empty if <paramref name="arguments" /> is <see langword="null" />. Otherwise, it contains the same elements as <paramref name="arguments" />, some of which may be quoted.
        /// The <see cref="O:System.Linq.Expressions.Expression.Type" /> property of the resulting <see cref="System.Linq.Expressions.MethodCallExpression" /> is equal to the return type of the method represented by <paramref name="method" />.</remarks>
        public static MethodCallExpression Call(Expression? instance, MethodInfo method, params Expression[]? arguments)
        {
            return Call(instance, method, (IEnumerable<Expression>?)arguments);
        }

        /// <summary>
        /// Creates a <see cref="MethodCallExpression"/> that represents a call to a method that takes one argument.
        /// </summary>
        /// <param name="instance">An <see cref="Expression"/> that specifies the instance for an instance call. (pass null for a static (Shared in Visual Basic) method).</param>
        /// <param name="method">The <see cref="MethodInfo"/> that represents the target method.</param>
        /// <param name="arg0">The <see cref="Expression"/> that represents the first argument.</param>
        /// <returns>A <see cref="MethodCallExpression"/> that has the <see cref="NodeType"/> property equal to <see cref="ExpressionType.Call"/> and the <see cref="MethodCallExpression.Object"/> and <see cref="MethodCallExpression.Method"/> properties set to the specified values.</returns>
        internal static MethodCallExpression Call(Expression? instance, MethodInfo method, Expression arg0)
        {
            // COMPAT: This method is marked as non-public to ensure compile-time compatibility for Expression.Call(e, m, null).

            ContractUtils.RequiresNotNull(method, nameof(method));
            ContractUtils.RequiresNotNull(arg0, nameof(arg0));

            ParameterInfo[] pis = ValidateMethodAndGetParameters(instance, method);

            ValidateArgumentCount(method, ExpressionType.Call, 1, pis);

            arg0 = ValidateOneArgument(method, ExpressionType.Call, arg0, pis[0], nameof(method), nameof(arg0));

            if (instance != null)
            {
                return new InstanceMethodCallExpression1(method, instance, arg0);
            }

            return new MethodCallExpression1(method, arg0);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.MethodCallExpression" /> that represents a call to a method that takes two arguments.</summary>
        /// <param name="instance">An <see cref="System.Linq.Expressions.Expression" /> that specifies the instance for an instance call. (pass null for a static (Shared in Visual Basic) method).</param>
        /// <param name="method">The <see cref="System.Reflection.MethodInfo" /> that represents the target method.</param>
        /// <param name="arg0">The <see cref="System.Linq.Expressions.Expression" /> that represents the first argument.</param>
        /// <param name="arg1">The <see cref="System.Linq.Expressions.Expression" /> that represents the second argument.</param>
        /// <returns>A <see cref="System.Linq.Expressions.MethodCallExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.Call" /> and the <see cref="System.Linq.Expressions.MethodCallExpression.Object" /> and <see cref="System.Linq.Expressions.MethodCallExpression.Method" /> properties set to the specified values.</returns>
        /// <remarks></remarks>
        /// <example>The following code example shows how to create an expression that calls an instance method that has two arguments.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/cs/program.cs" id="Snippet17":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/vb/module1.vb" id="Snippet17":::</example>
        public static MethodCallExpression Call(Expression? instance, MethodInfo method, Expression arg0, Expression arg1)
        {
            ContractUtils.RequiresNotNull(method, nameof(method));
            ContractUtils.RequiresNotNull(arg0, nameof(arg0));
            ContractUtils.RequiresNotNull(arg1, nameof(arg1));

            ParameterInfo[] pis = ValidateMethodAndGetParameters(instance, method);

            ValidateArgumentCount(method, ExpressionType.Call, 2, pis);

            arg0 = ValidateOneArgument(method, ExpressionType.Call, arg0, pis[0], nameof(method), nameof(arg0));
            arg1 = ValidateOneArgument(method, ExpressionType.Call, arg1, pis[1], nameof(method), nameof(arg1));

            if (instance != null)
            {
                return new InstanceMethodCallExpression2(method, instance, arg0, arg1);
            }

            return new MethodCallExpression2(method, arg0, arg1);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.MethodCallExpression" /> that represents a call to a method that takes three arguments.</summary>
        /// <param name="instance">An <see cref="System.Linq.Expressions.Expression" /> that specifies the instance for an instance call. (pass null for a static (Shared in Visual Basic) method).</param>
        /// <param name="method">The <see cref="System.Reflection.MethodInfo" /> that represents the target method.</param>
        /// <param name="arg0">The <see cref="System.Linq.Expressions.Expression" /> that represents the first argument.</param>
        /// <param name="arg1">The <see cref="System.Linq.Expressions.Expression" /> that represents the second argument.</param>
        /// <param name="arg2">The <see cref="System.Linq.Expressions.Expression" /> that represents the third argument.</param>
        /// <returns>A <see cref="System.Linq.Expressions.MethodCallExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.Call" /> and the <see cref="System.Linq.Expressions.MethodCallExpression.Object" /> and <see cref="System.Linq.Expressions.MethodCallExpression.Method" /> properties set to the specified values.</returns>
        public static MethodCallExpression Call(Expression? instance, MethodInfo method, Expression arg0, Expression arg1, Expression arg2)
        {
            ContractUtils.RequiresNotNull(method, nameof(method));
            ContractUtils.RequiresNotNull(arg0, nameof(arg0));
            ContractUtils.RequiresNotNull(arg1, nameof(arg1));
            ContractUtils.RequiresNotNull(arg2, nameof(arg2));

            ParameterInfo[] pis = ValidateMethodAndGetParameters(instance, method);

            ValidateArgumentCount(method, ExpressionType.Call, 3, pis);

            arg0 = ValidateOneArgument(method, ExpressionType.Call, arg0, pis[0], nameof(method), nameof(arg0));
            arg1 = ValidateOneArgument(method, ExpressionType.Call, arg1, pis[1], nameof(method), nameof(arg1));
            arg2 = ValidateOneArgument(method, ExpressionType.Call, arg2, pis[2], nameof(method), nameof(arg2));

            if (instance != null)
            {
                return new InstanceMethodCallExpression3(method, instance, arg0, arg1, arg2);
            }
            return new MethodCallExpression3(method, arg0, arg1, arg2);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.MethodCallExpression" /> that represents a call to a method by calling the appropriate factory method.</summary>
        /// <param name="instance">An <see cref="System.Linq.Expressions.Expression" /> whose <see cref="System.Linq.Expressions.Expression.Type" /> property value will be searched for a specific method.</param>
        /// <param name="methodName">The name of the method.</param>
        /// <param name="typeArguments">An array of <see cref="System.Type" /> objects that specify the type parameters of the generic method. This argument should be null when methodName specifies a non-generic method.</param>
        /// <param name="arguments">An array of <see cref="System.Linq.Expressions.Expression" /> objects that represents the arguments to the method.</param>
        /// <returns>A <see cref="System.Linq.Expressions.MethodCallExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.Call" />, the <see cref="System.Linq.Expressions.MethodCallExpression.Object" /> property equal to <paramref name="instance" />, <see cref="System.Linq.Expressions.MethodCallExpression.Method" /> set to the <see cref="System.Reflection.MethodInfo" /> that represents the specified instance method, and <see cref="System.Linq.Expressions.MethodCallExpression.Arguments" /> set to the specified arguments.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="instance" /> or <paramref name="methodName" /> is <see langword="null" />.</exception>
        /// <exception cref="System.InvalidOperationException">No method whose name is <paramref name="methodName" />, whose type parameters match <paramref name="typeArguments" />, and whose parameter types match <paramref name="arguments" /> is found in <paramref name="instance" />.Type or its base types.
        /// -or-
        /// More than one method whose name is <paramref name="methodName" />, whose type parameters match <paramref name="typeArguments" />, and whose parameter types match <paramref name="arguments" /> is found in <paramref name="instance" />.Type or its base types.</exception>
        /// <remarks>The <see cref="O:System.Linq.Expressions.Expression.Type" /> property of the resulting <see cref="System.Linq.Expressions.MethodCallExpression" /> is equal to the return type of the method denoted by <paramref name="methodName" />.</remarks>
        [RequiresUnreferencedCode(ExpressionRequiresUnreferencedCode)]
        public static MethodCallExpression Call(Expression instance, string methodName, Type[]? typeArguments, params Expression[]? arguments)
        {
            ContractUtils.RequiresNotNull(instance, nameof(instance));
            ContractUtils.RequiresNotNull(methodName, nameof(methodName));
            if (arguments == null)
            {
                arguments = Array.Empty<Expression>();
            }

            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;
            return Expression.Call(instance, FindMethod(instance.Type, methodName, typeArguments, arguments, flags)!, arguments);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.MethodCallExpression" /> that represents a call to a <see langword="static" /> (<see langword="Shared" /> in Visual Basic) method by calling the appropriate factory method.</summary>
        /// <param name="type">The type that contains the specified <see langword="static" /> (<see langword="Shared" /> in Visual Basic) method.</param>
        /// <param name="methodName">The name of the method.</param>
        /// <param name="typeArguments">An array of <see cref="System.Type" /> objects that specify the type parameters of the generic method. This argument should be null when methodName specifies a non-generic method.</param>
        /// <param name="arguments">An array of <see cref="System.Linq.Expressions.Expression" /> objects that represent the arguments to the method.</param>
        /// <returns>A <see cref="System.Linq.Expressions.MethodCallExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.Call" />, the <see cref="System.Linq.Expressions.MethodCallExpression.Method" /> property set to the <see cref="System.Reflection.MethodInfo" /> that represents the specified <see langword="static" /> (<see langword="Shared" /> in Visual Basic) method, and the <see cref="System.Linq.Expressions.MethodCallExpression.Arguments" /> property set to the specified arguments.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="type" /> or <paramref name="methodName" /> is <see langword="null" />.</exception>
        /// <exception cref="System.InvalidOperationException">No method whose name is <paramref name="methodName" />, whose type parameters match <paramref name="typeArguments" />, and whose parameter types match <paramref name="arguments" /> is found in <paramref name="type" /> or its base types.
        /// -or-
        /// More than one method whose name is <paramref name="methodName" />, whose type parameters match <paramref name="typeArguments" />, and whose parameter types match <paramref name="arguments" /> is found in <paramref name="type" /> or its base types.</exception>
        /// <remarks>The <see cref="O:System.Linq.Expressions.Expression.Type" /> property of the resulting <see cref="System.Linq.Expressions.MethodCallExpression" /> is equal to the return type of the method denoted by <paramref name="methodName" />. The <see cref="O:System.Linq.Expressions.MethodCallExpression.Object" /> property is <see langword="null" />.</remarks>
        [RequiresUnreferencedCode(GenericMethodRequiresUnreferencedCode)]
        public static MethodCallExpression Call(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)] Type type,
            string methodName,
            Type[]? typeArguments,
            params Expression[]? arguments)
        {
            ContractUtils.RequiresNotNull(type, nameof(type));
            ContractUtils.RequiresNotNull(methodName, nameof(methodName));

            if (arguments == null) arguments = Array.Empty<Expression>();
            BindingFlags flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;
            return Expression.Call(null, FindMethod(type, methodName, typeArguments, arguments, flags)!, arguments);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.MethodCallExpression" /> that represents a call to a method that takes arguments.</summary>
        /// <param name="instance">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.MethodCallExpression.Object" /> property equal to (pass <see langword="null" /> for a <see langword="static" /> (<see langword="Shared" /> in Visual Basic) method).</param>
        /// <param name="method">A <see cref="System.Reflection.MethodInfo" /> to set the <see cref="System.Linq.Expressions.MethodCallExpression.Method" /> property equal to.</param>
        /// <param name="arguments">An <see cref="System.Collections.Generic.IEnumerable{T}" /> that contains <see cref="System.Linq.Expressions.Expression" /> objects to use to populate the <see cref="System.Linq.Expressions.MethodCallExpression.Arguments" /> collection.</param>
        /// <returns>A <see cref="System.Linq.Expressions.MethodCallExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.Call" /> and the <see cref="System.Linq.Expressions.MethodCallExpression.Object" />, <see cref="System.Linq.Expressions.MethodCallExpression.Method" />, and <see cref="System.Linq.Expressions.MethodCallExpression.Arguments" /> properties set to the specified values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="method" /> is <see langword="null" />.
        /// -or-
        /// <paramref name="instance" /> is <see langword="null" /> and <paramref name="method" /> represents an instance method.</exception>
        /// <exception cref="System.ArgumentException"><paramref name="instance" />.Type is not assignable to the declaring type of the method represented by <paramref name="method" />.
        /// -or-
        /// The number of elements in <paramref name="arguments" /> does not equal the number of parameters for the method represented by <paramref name="method" />.
        /// -or-
        /// One or more of the elements of <paramref name="arguments" /> is not assignable to the corresponding parameter for the method represented by <paramref name="method" />.</exception>
        /// <remarks>To represent a call to a <see langword="static" /> (`Shared` in Visual Basic) method, pass in <see langword="null" /> for the <paramref name="instance" /> parameter when you call this method, or call <see cref="O:System.Linq.Expressions.Expression.Call" /> instead.
        /// If <paramref name="method" /> represents an instance method, the <see cref="O:System.Linq.Expressions.Expression.Type" /> property of <paramref name="instance" /> must be assignable to the declaring type of the method represented by <paramref name="method" />.
        /// If <paramref name="arguments" /> is not <see langword="null" />, it must have the same number of elements as the number of parameters for the method represented by <paramref name="method" />. Each element in <paramref name="arguments" /> must not be <see langword="null" /> and must be assignable to the corresponding parameter of <paramref name="method" />, possibly after *quoting*.
        /// <format type="text/markdown"><![CDATA[
        /// > [!NOTE]
        /// >  An element will be quoted only if the corresponding method parameter is of type <xref:System.Linq.Expressions.Expression>. Quoting means the element is wrapped in a <xref:System.Linq.Expressions.ExpressionType.Quote> node. The resulting node is a <xref:System.Linq.Expressions.UnaryExpression> whose <xref:System.Linq.Expressions.UnaryExpression.Operand%2A> property is the element of `arguments`.
        /// ]]></format>
        /// The <see cref="O:System.Linq.Expressions.MethodCallExpression.Arguments" /> property of the resulting <see cref="System.Linq.Expressions.MethodCallExpression" /> is empty if <paramref name="arguments" /> is <see langword="null" />. Otherwise, it contains the same elements as <paramref name="arguments" />, some of which may be quoted.
        /// The <see cref="O:System.Linq.Expressions.Expression.Type" /> property of the resulting <see cref="System.Linq.Expressions.MethodCallExpression" /> is equal to the return type of the method represented by <paramref name="method" />.</remarks>
        public static MethodCallExpression Call(Expression? instance, MethodInfo method, IEnumerable<Expression>? arguments)
        {
            IReadOnlyList<Expression> argumentList = arguments as IReadOnlyList<Expression> ?? arguments.ToReadOnly();

            int argCount = argumentList.Count;

            switch (argCount)
            {
                case 0:
                    return Call(instance, method);
                case 1:
                    return Call(instance, method, argumentList[0]);
                case 2:
                    return Call(instance, method, argumentList[0], argumentList[1]);
                case 3:
                    return Call(instance, method, argumentList[0], argumentList[1], argumentList[2]);
            }

            if (instance == null)
            {
                switch (argCount)
                {
                    case 4:
                        return Call(method, argumentList[0], argumentList[1], argumentList[2], argumentList[3]);
                    case 5:
                        return Call(method, argumentList[0], argumentList[1], argumentList[2], argumentList[3], argumentList[4]);
                }
            }

            ContractUtils.RequiresNotNull(method, nameof(method));

            // If this has resulted in a duplicate call to ToReadOnly (any case except arguments being
            // a large IReadOnlyList that is not TrueReadOnlyCollection) it will return argumentList
            // back again quickly.
            ReadOnlyCollection<Expression> argList = argumentList.ToReadOnly();

            ValidateMethodInfo(method, nameof(method));
            ValidateStaticOrInstanceMethod(instance, method);
            ValidateArgumentTypes(method, ExpressionType.Call, ref argList, nameof(method));

            if (instance == null)
            {
                return new MethodCallExpressionN(method, argList);
            }
            else
            {
                return new InstanceMethodCallExpressionN(method, instance, argList);
            }
        }

        private static ParameterInfo[] ValidateMethodAndGetParameters(Expression? instance, MethodInfo method)
        {
            ValidateMethodInfo(method, nameof(method));
            ValidateStaticOrInstanceMethod(instance, method);

            return GetParametersForValidation(method, ExpressionType.Call);
        }

        private static void ValidateStaticOrInstanceMethod(Expression? instance, MethodInfo method)
        {
            if (method.IsStatic)
            {
                if (instance != null) throw Error.OnlyStaticMethodsHaveNullInstance();
            }
            else
            {
                if (instance == null) throw Error.OnlyStaticMethodsHaveNullInstance();
                ExpressionUtils.RequiresCanRead(instance, nameof(instance));
                ValidateCallInstanceType(instance.Type, method);
            }
        }

        private static void ValidateCallInstanceType(Type instanceType, MethodInfo method)
        {
            if (!TypeUtils.IsValidInstanceType(method, instanceType))
            {
                throw Error.InstanceAndMethodTypeMismatch(method, method.DeclaringType, instanceType);
            }
        }

        private static void ValidateArgumentTypes(MethodBase method, ExpressionType nodeKind, ref ReadOnlyCollection<Expression> arguments, string methodParamName)
        {
            ExpressionUtils.ValidateArgumentTypes(method, nodeKind, ref arguments, methodParamName);
        }

        private static ParameterInfo[] GetParametersForValidation(MethodBase method, ExpressionType nodeKind)
        {
            return ExpressionUtils.GetParametersForValidation(method, nodeKind);
        }

        private static void ValidateArgumentCount(MethodBase method, ExpressionType nodeKind, int count, ParameterInfo[] pis)
        {
            ExpressionUtils.ValidateArgumentCount(method, nodeKind, count, pis);
        }

        private static Expression ValidateOneArgument(MethodBase method, ExpressionType nodeKind, Expression arg, ParameterInfo pi, string methodParamName, string argumentParamName)
        {
            return ExpressionUtils.ValidateOneArgument(method, nodeKind, arg, pi, methodParamName, argumentParamName);
        }

        // Attempts to auto-quote the expression tree. Returns true if it succeeded, false otherwise.
        private static bool TryQuote(Type parameterType, ref Expression argument)
        {
            return ExpressionUtils.TryQuote(parameterType, ref argument);
        }

        [RequiresUnreferencedCode(GenericMethodRequiresUnreferencedCode)]
        private static MethodInfo? FindMethod(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)] Type type,
            string methodName,
            Type[]? typeArgs,
            Expression[] args,
            BindingFlags flags)
        {
            int count = 0;
            MethodInfo? method = null;

            foreach (MethodInfo mi in type.GetMethods(flags))
            {
                if (mi.Name.Equals(methodName, StringComparison.OrdinalIgnoreCase))
                {
                    MethodInfo? moo = ApplyTypeArgs(mi, typeArgs);
                    if (moo != null && IsCompatible(moo, args))
                    {
                        // favor public over non-public methods
                        if (method == null || (!method.IsPublic && moo.IsPublic))
                        {
                            method = moo;
                            count = 1;
                        }
                        // only count it as additional method if they both public or both non-public
                        else if (method.IsPublic == moo.IsPublic)
                        {
                            count++;
                        }
                    }
                }
            }

            if (count == 0)
            {
                if (typeArgs != null && typeArgs.Length > 0)
                {
                    throw Error.GenericMethodWithArgsDoesNotExistOnType(methodName, type);
                }
                else
                {
                    throw Error.MethodWithArgsDoesNotExistOnType(methodName, type);
                }
            }

            if (count > 1)
                throw Error.MethodWithMoreThanOneMatch(methodName, type);

            return method;
        }

        private static bool IsCompatible(MethodBase m, Expression[] arguments)
        {
            ParameterInfo[] parms = m.GetParametersCached();
            if (parms.Length != arguments.Length)
                return false;
            for (int i = 0; i < arguments.Length; i++)
            {
                Expression arg = arguments[i];
                ContractUtils.RequiresNotNull(arg, nameof(arguments));
                Type argType = arg.Type;
                Type pType = parms[i].ParameterType;
                if (pType.IsByRef)
                {
                    pType = pType.GetElementType()!;
                }
                if (!TypeUtils.AreReferenceAssignable(pType, argType) &&
                    !(TypeUtils.IsSameOrSubclass(typeof(LambdaExpression), pType) && pType.IsAssignableFrom(arg.GetType())))
                {
                    return false;
                }
            }
            return true;
        }

        [RequiresUnreferencedCode(GenericMethodRequiresUnreferencedCode)]
        private static MethodInfo? ApplyTypeArgs(MethodInfo m, Type[]? typeArgs)
        {
            if (typeArgs == null || typeArgs.Length == 0)
            {
                if (!m.IsGenericMethodDefinition)
                    return m;
            }
            else
            {
                if (m.IsGenericMethodDefinition && m.GetGenericArguments().Length == typeArgs.Length)
                    return m.MakeGenericMethod(typeArgs);
            }
            return null;
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.MethodCallExpression" /> that represents applying an array index operator to a multidimensional array.</summary>
        /// <param name="array">An array of <see cref="System.Linq.Expressions.Expression" /> instances - indexes for the array index operation.</param>
        /// <param name="indexes">An array of <see cref="System.Linq.Expressions.Expression" /> objects to use to populate the <see cref="System.Linq.Expressions.MethodCallExpression.Arguments" /> collection.</param>
        /// <returns>A <see cref="System.Linq.Expressions.MethodCallExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.Call" /> and the <see cref="System.Linq.Expressions.MethodCallExpression.Object" /> and <see cref="System.Linq.Expressions.MethodCallExpression.Arguments" /> properties set to the specified values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="array" /> or <paramref name="indexes" /> is <see langword="null" />.</exception>
        /// <exception cref="System.ArgumentException"><paramref name="array" />.Type does not represent an array type.
        /// -or-
        /// The rank of <paramref name="array" />.Type does not match the number of elements in <paramref name="indexes" />.
        /// -or-
        /// The <see cref="System.Linq.Expressions.Expression.Type" /> property of one or more elements of <paramref name="indexes" /> does not represent the <see cref="int" /> type.</exception>
        /// <remarks>Each element of <paramref name="indexes" /> must have <see cref="O:System.Linq.Expressions.Expression.Type" /> equal to <see cref="int" />. The <see cref="O:System.Linq.Expressions.Expression.Type" /> property of <paramref name="array" /> must represent an array type whose rank matches the number of elements in <paramref name="indexes" />.
        /// If the rank of <paramref name="array" />.Type is 1, this method returns a <see cref="System.Linq.Expressions.BinaryExpression" />. The <see cref="O:System.Linq.Expressions.BinaryExpression.Left" /> property is set to <paramref name="array" /> and the <see cref="O:System.Linq.Expressions.BinaryExpression.Right" /> property is set to the single element of <paramref name="indexes" />. The <see cref="O:System.Linq.Expressions.Expression.Type" /> property of the <see cref="System.Linq.Expressions.BinaryExpression" /> represents the element type of <paramref name="array" />.Type.
        /// If the rank of <paramref name="array" />.Type is more than one, this method returns a <see cref="System.Linq.Expressions.MethodCallExpression" />. The <see cref="O:System.Linq.Expressions.MethodCallExpression.Method" /> property is set to the <see cref="System.Reflection.MethodInfo" /> that describes the public instance method `Get` on the type represented by the <see cref="O:System.Linq.Expressions.Expression.Type" /> property of <paramref name="array" />.</remarks>
        /// <example>The following example demonstrates how to use the <see cref="System.Linq.Expressions.Expression.ArrayIndex(System.Linq.Expressions.Expression,System.Linq.Expressions.Expression[])" /> method to create a <see cref="System.Linq.Expressions.MethodCallExpression" /> that represents indexing into a two-dimensional array.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Expressions.Expression/CS/Expression.cs" id="Snippet3":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Expressions.Expression/VB/Expression.vb" id="Snippet3":::</example>
        public static MethodCallExpression ArrayIndex(Expression array, params Expression[] indexes)
        {
            return ArrayIndex(array, (IEnumerable<Expression>)indexes);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.MethodCallExpression" /> that represents applying an array index operator to an array of rank more than one.</summary>
        /// <param name="array">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.MethodCallExpression.Object" /> property equal to.</param>
        /// <param name="indexes">An <see cref="System.Collections.Generic.IEnumerable{T}" /> that contains <see cref="System.Linq.Expressions.Expression" /> objects to use to populate the <see cref="System.Linq.Expressions.MethodCallExpression.Arguments" /> collection.</param>
        /// <returns>A <see cref="System.Linq.Expressions.MethodCallExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.Call" /> and the <see cref="System.Linq.Expressions.MethodCallExpression.Object" /> and <see cref="System.Linq.Expressions.MethodCallExpression.Arguments" /> properties set to the specified values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="array" /> or <paramref name="indexes" /> is <see langword="null" />.</exception>
        /// <exception cref="System.ArgumentException"><paramref name="array" />.Type does not represent an array type.
        /// -or-
        /// The rank of <paramref name="array" />.Type does not match the number of elements in <paramref name="indexes" />.
        /// -or-
        /// The <see cref="System.Linq.Expressions.Expression.Type" /> property of one or more elements of <paramref name="indexes" /> does not represent the <see cref="int" /> type.</exception>
        /// <remarks>Each element of <paramref name="indexes" /> must have <see cref="O:System.Linq.Expressions.Expression.Type" /> equal to <see cref="int" />. The <see cref="O:System.Linq.Expressions.Expression.Type" /> property of <paramref name="array" /> must represent an array type whose rank matches the number of elements in <paramref name="indexes" />.
        /// If the rank of <paramref name="array" />.Type is 1, this method returns a <see cref="System.Linq.Expressions.BinaryExpression" />. The <see cref="O:System.Linq.Expressions.BinaryExpression.Left" /> property is set to <paramref name="array" /> and the <see cref="O:System.Linq.Expressions.BinaryExpression.Right" /> property is set to the single element of <paramref name="indexes" />. The <see cref="O:System.Linq.Expressions.Expression.Type" /> property of the <see cref="System.Linq.Expressions.BinaryExpression" /> represents the element type of <paramref name="array" />.Type.
        /// If the rank of <paramref name="array" />.Type is more than one, this method returns a <see cref="System.Linq.Expressions.MethodCallExpression" />. The <see cref="O:System.Linq.Expressions.MethodCallExpression.Method" /> property is set to the <see cref="System.Reflection.MethodInfo" /> that describes the public instance method `Get` on the type represented by the <see cref="O:System.Linq.Expressions.Expression.Type" /> property of <paramref name="array" />.</remarks>
        /// <example>The following example demonstrates how to use the <see cref="System.Linq.Expressions.Expression.ArrayIndex(System.Linq.Expressions.Expression,System.Linq.Expressions.Expression[])" /> method to create a <see cref="System.Linq.Expressions.MethodCallExpression" /> that represents indexing into a two-dimensional array.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Expressions.Expression/CS/Expression.cs" id="Snippet3":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Expressions.Expression/VB/Expression.vb" id="Snippet3":::</example>
        public static MethodCallExpression ArrayIndex(Expression array, IEnumerable<Expression> indexes)
        {
            ExpressionUtils.RequiresCanRead(array, nameof(array), -1);
            ContractUtils.RequiresNotNull(indexes, nameof(indexes));

            Type arrayType = array.Type;
            if (!arrayType.IsArray)
            {
                throw Error.ArgumentMustBeArray(nameof(array));
            }

            ReadOnlyCollection<Expression> indexList = indexes.ToReadOnly();
            if (arrayType.GetArrayRank() != indexList.Count)
            {
                throw Error.IncorrectNumberOfIndexes();
            }

            for (int i = 0, n = indexList.Count; i < n; i++)
            {
                Expression e = indexList[i];

                ExpressionUtils.RequiresCanRead(e, nameof(indexes), i);
                if (e.Type != typeof(int))
                {
                    throw Error.ArgumentMustBeArrayIndexType(nameof(indexes), i);
                }
            }

            MethodInfo mi = TypeUtils.GetArrayGetMethod(array.Type);
            return Call(array, mi, indexList);
        }

        #endregion
    }
}
