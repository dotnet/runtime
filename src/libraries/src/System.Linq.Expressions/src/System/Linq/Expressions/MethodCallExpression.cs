// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic.Utils;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace System.Linq.Expressions
{
    /// <summary>
    /// Represents a call to either static or an instance method.
    /// </summary>
    [DebuggerTypeProxy(typeof(Expression.MethodCallExpressionProxy))]
    public class MethodCallExpression : Expression, IArgumentProvider
    {
        private readonly MethodInfo _method;

        internal MethodCallExpression(MethodInfo method)
        {
            _method = method;
        }

        internal virtual Expression GetInstance()
        {
            return null;
        }

        /// <summary>
        /// Returns the node type of this <see cref="Expression" />. (Inherited from <see cref="Expression" />.)
        /// </summary>
        /// <returns>The <see cref="ExpressionType"/> that represents this expression.</returns>
        public sealed override ExpressionType NodeType
        {
            get { return ExpressionType.Call; }
        }

        /// <summary>
        /// Gets the static type of the expression that this <see cref="Expression" /> represents. (Inherited from <see cref="Expression"/>.)
        /// </summary>
        /// <returns>The <see cref="Type"/> that represents the static type of the expression.</returns>
        public sealed override Type Type
        {
            get { return _method.ReturnType; }
        }

        /// <summary>
        /// Gets the <see cref="MethodInfo" /> for the method to be called.
        /// </summary>
        public MethodInfo Method
        {
            get { return _method; }
        }

        /// <summary>
        /// Gets the <see cref="Expression" /> that represents the instance 
        /// for instance method calls or null for static method cals.
        /// </summary>
        public Expression Object
        {
            get { return GetInstance(); }
        }

        /// <summary>
        /// Gets a collection of expressions that represent arguments to the method call.
        /// </summary>
        public ReadOnlyCollection<Expression> Arguments
        {
            get { return GetOrMakeArguments(); }
        }

        /// <summary>
        /// Creates a new expression that is like this one, but using the
        /// supplied children. If all of the children are the same, it will
        /// return this expression.
        /// </summary>
        /// <param name="object">The <see cref="Object" /> property of the result.</param>
        /// <param name="arguments">The <see cref="Arguments" /> property of the result.</param>
        /// <returns>This expression if no children changed, or an expression with the updated children.</returns>
        public MethodCallExpression Update(Expression @object, IEnumerable<Expression> arguments)
        {
            if (@object == Object && arguments == Arguments)
            {
                return this;
            }
            return Expression.Call(@object, Method, arguments);
        }

        [ExcludeFromCodeCoverage] // Unreachable
        internal virtual ReadOnlyCollection<Expression> GetOrMakeArguments()
        {
            throw ContractUtils.Unreachable;
        }

        /// <summary>
        /// Dispatches to the specific visit method for this node type.
        /// </summary>
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
        [ExcludeFromCodeCoverage] // Unreachable
        internal virtual MethodCallExpression Rewrite(Expression instance, IList<Expression> args)
        {
            throw ContractUtils.Unreachable;
        }

        #region IArgumentProvider Members

        [ExcludeFromCodeCoverage] // Unreachable
        public virtual Expression GetArgument(int index)
        {
            throw ContractUtils.Unreachable;
        }

        [ExcludeFromCodeCoverage] // Unreachable
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

        internal override Expression GetInstance()
        {
            return _instance;
        }
    }

    internal sealed class MethodCallExpressionN : MethodCallExpression, IArgumentProvider
    {
        private IList<Expression> _arguments;

        public MethodCallExpressionN(MethodInfo method, IList<Expression> args)
            : base(method)
        {
            _arguments = args;
        }

        public override Expression GetArgument(int index)
        {
            return _arguments[index];
        }

        public override int ArgumentCount
        {
            get
            {
                return _arguments.Count;
            }
        }

        internal override ReadOnlyCollection<Expression> GetOrMakeArguments()
        {
            return ReturnReadOnly(ref _arguments);
        }

        internal override MethodCallExpression Rewrite(Expression instance, IList<Expression> args)
        {
            Debug.Assert(instance == null);
            Debug.Assert(args == null || args.Count == _arguments.Count);

            return Expression.Call(Method, args ?? _arguments);
        }
    }

    internal sealed class InstanceMethodCallExpressionN : InstanceMethodCallExpression, IArgumentProvider
    {
        private IList<Expression> _arguments;

        public InstanceMethodCallExpressionN(MethodInfo method, Expression instance, IList<Expression> args)
            : base(method, instance)
        {
            _arguments = args;
        }

        public override Expression GetArgument(int index)
        {
            return _arguments[index];
        }

        public override int ArgumentCount
        {
            get
            {
                return _arguments.Count;
            }
        }

        internal override ReadOnlyCollection<Expression> GetOrMakeArguments()
        {
            return ReturnReadOnly(ref _arguments);
        }

        internal override MethodCallExpression Rewrite(Expression instance, IList<Expression> args)
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
            throw new InvalidOperationException();
        }

        public override int ArgumentCount
        {
            get
            {
                return 0;
            }
        }

        internal override ReadOnlyCollection<Expression> GetOrMakeArguments()
        {
            return EmptyReadOnlyCollection<Expression>.Instance;
        }

        internal override MethodCallExpression Rewrite(Expression instance, IList<Expression> args)
        {
            Debug.Assert(instance == null);
            Debug.Assert(args == null || args.Count == 0);

            return Expression.Call(Method);
        }
    }

    internal sealed class MethodCallExpression1 : MethodCallExpression, IArgumentProvider
    {
        private object _arg0;       // storage for the 1st argument or a readonly collection.  See IArgumentProvider

        public MethodCallExpression1(MethodInfo method, Expression arg0)
            : base(method)
        {
            _arg0 = arg0;
        }

        public override Expression GetArgument(int index)
        {
            switch (index)
            {
                case 0: return ReturnObject<Expression>(_arg0);
                default: throw new InvalidOperationException();
            }
        }

        public override int ArgumentCount
        {
            get
            {
                return 1;
            }
        }

        internal override ReadOnlyCollection<Expression> GetOrMakeArguments()
        {
            return ReturnReadOnly(this, ref _arg0);
        }

        internal override MethodCallExpression Rewrite(Expression instance, IList<Expression> args)
        {
            Debug.Assert(instance == null);
            Debug.Assert(args == null || args.Count == 1);

            if (args != null)
            {
                return Expression.Call(Method, args[0]);
            }

            return Expression.Call(Method, ReturnObject<Expression>(_arg0));
        }
    }

    internal sealed class MethodCallExpression2 : MethodCallExpression, IArgumentProvider
    {
        private object _arg0;               // storage for the 1st argument or a readonly collection.  See IArgumentProvider
        private readonly Expression _arg1;  // storage for the 2nd arg

        public MethodCallExpression2(MethodInfo method, Expression arg0, Expression arg1)
            : base(method)
        {
            _arg0 = arg0;
            _arg1 = arg1;
        }

        public override Expression GetArgument(int index)
        {
            switch (index)
            {
                case 0: return ReturnObject<Expression>(_arg0);
                case 1: return _arg1;
                default: throw new InvalidOperationException();
            }
        }

        public override int ArgumentCount
        {
            get
            {
                return 2;
            }
        }

        internal override ReadOnlyCollection<Expression> GetOrMakeArguments()
        {
            return ReturnReadOnly(this, ref _arg0);
        }

        internal override MethodCallExpression Rewrite(Expression instance, IList<Expression> args)
        {
            Debug.Assert(instance == null);
            Debug.Assert(args == null || args.Count == 2);

            if (args != null)
            {
                return Expression.Call(Method, args[0], args[1]);
            }
            return Expression.Call(Method, ReturnObject<Expression>(_arg0), _arg1);
        }
    }

    internal sealed class MethodCallExpression3 : MethodCallExpression, IArgumentProvider
    {
        private object _arg0;           // storage for the 1st argument or a readonly collection.  See IArgumentProvider
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
            switch (index)
            {
                case 0: return ReturnObject<Expression>(_arg0);
                case 1: return _arg1;
                case 2: return _arg2;
                default: throw new InvalidOperationException();
            }
        }

        public override int ArgumentCount
        {
            get
            {
                return 3;
            }
        }

        internal override ReadOnlyCollection<Expression> GetOrMakeArguments()
        {
            return ReturnReadOnly(this, ref _arg0);
        }

        internal override MethodCallExpression Rewrite(Expression instance, IList<Expression> args)
        {
            Debug.Assert(instance == null);
            Debug.Assert(args == null || args.Count == 3);

            if (args != null)
            {
                return Expression.Call(Method, args[0], args[1], args[2]);
            }
            return Expression.Call(Method, ReturnObject<Expression>(_arg0), _arg1, _arg2);
        }
    }

    internal sealed class MethodCallExpression4 : MethodCallExpression, IArgumentProvider
    {
        private object _arg0;               // storage for the 1st argument or a readonly collection.  See IArgumentProvider
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
            switch (index)
            {
                case 0: return ReturnObject<Expression>(_arg0);
                case 1: return _arg1;
                case 2: return _arg2;
                case 3: return _arg3;
                default: throw new InvalidOperationException();
            }
        }

        public override int ArgumentCount
        {
            get
            {
                return 4;
            }
        }

        internal override ReadOnlyCollection<Expression> GetOrMakeArguments()
        {
            return ReturnReadOnly(this, ref _arg0);
        }

        internal override MethodCallExpression Rewrite(Expression instance, IList<Expression> args)
        {
            Debug.Assert(instance == null);
            Debug.Assert(args == null || args.Count == 4);

            if (args != null)
            {
                return Expression.Call(Method, args[0], args[1], args[2], args[3]);
            }
            return Expression.Call(Method, ReturnObject<Expression>(_arg0), _arg1, _arg2, _arg3);
        }
    }

    internal sealed class MethodCallExpression5 : MethodCallExpression, IArgumentProvider
    {
        private object _arg0;           // storage for the 1st argument or a readonly collection.  See IArgumentProvider
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
            switch (index)
            {
                case 0: return ReturnObject<Expression>(_arg0);
                case 1: return _arg1;
                case 2: return _arg2;
                case 3: return _arg3;
                case 4: return _arg4;
                default: throw new InvalidOperationException();
            }
        }

        public override int ArgumentCount
        {
            get
            {
                return 5;
            }
        }

        internal override ReadOnlyCollection<Expression> GetOrMakeArguments()
        {
            return ReturnReadOnly(this, ref _arg0);
        }

        internal override MethodCallExpression Rewrite(Expression instance, IList<Expression> args)
        {
            Debug.Assert(instance == null);
            Debug.Assert(args == null || args.Count == 5);

            if (args != null)
            {
                return Expression.Call(Method, args[0], args[1], args[2], args[3], args[4]);
            }

            return Expression.Call(Method, ReturnObject<Expression>(_arg0), _arg1, _arg2, _arg3, _arg4);
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
            throw new InvalidOperationException();
        }

        public override int ArgumentCount
        {
            get
            {
                return 0;
            }
        }

        internal override ReadOnlyCollection<Expression> GetOrMakeArguments()
        {
            return EmptyReadOnlyCollection<Expression>.Instance;
        }

        internal override MethodCallExpression Rewrite(Expression instance, IList<Expression> args)
        {
            Debug.Assert(instance != null);
            Debug.Assert(args == null || args.Count == 0);

            return Expression.Call(instance, Method);
        }
    }

    internal sealed class InstanceMethodCallExpression1 : InstanceMethodCallExpression, IArgumentProvider
    {
        private object _arg0;                // storage for the 1st argument or a readonly collection.  See IArgumentProvider

        public InstanceMethodCallExpression1(MethodInfo method, Expression instance, Expression arg0)
            : base(method, instance)
        {
            _arg0 = arg0;
        }

        public override Expression GetArgument(int index)
        {
            switch (index)
            {
                case 0: return ReturnObject<Expression>(_arg0);
                default: throw new InvalidOperationException();
            }
        }

        public override int ArgumentCount
        {
            get
            {
                return 1;
            }
        }

        internal override ReadOnlyCollection<Expression> GetOrMakeArguments()
        {
            return ReturnReadOnly(this, ref _arg0);
        }

        internal override MethodCallExpression Rewrite(Expression instance, IList<Expression> args)
        {
            Debug.Assert(instance != null);
            Debug.Assert(args == null || args.Count == 1);

            if (args != null)
            {
                return Expression.Call(instance, Method, args[0]);
            }
            return Expression.Call(instance, Method, ReturnObject<Expression>(_arg0));
        }
    }

    internal sealed class InstanceMethodCallExpression2 : InstanceMethodCallExpression, IArgumentProvider
    {
        private object _arg0;                // storage for the 1st argument or a readonly collection.  See IArgumentProvider
        private readonly Expression _arg1;   // storage for the 2nd argument

        public InstanceMethodCallExpression2(MethodInfo method, Expression instance, Expression arg0, Expression arg1)
            : base(method, instance)
        {
            _arg0 = arg0;
            _arg1 = arg1;
        }

        public override Expression GetArgument(int index)
        {
            switch (index)
            {
                case 0: return ReturnObject<Expression>(_arg0);
                case 1: return _arg1;
                default: throw new InvalidOperationException();
            }
        }

        public override int ArgumentCount
        {
            get
            {
                return 2;
            }
        }

        internal override ReadOnlyCollection<Expression> GetOrMakeArguments()
        {
            return ReturnReadOnly(this, ref _arg0);
        }

        internal override MethodCallExpression Rewrite(Expression instance, IList<Expression> args)
        {
            Debug.Assert(instance != null);
            Debug.Assert(args == null || args.Count == 2);

            if (args != null)
            {
                return Expression.Call(instance, Method, args[0], args[1]);
            }
            return Expression.Call(instance, Method, ReturnObject<Expression>(_arg0), _arg1);
        }
    }

    internal sealed class InstanceMethodCallExpression3 : InstanceMethodCallExpression, IArgumentProvider
    {
        private object _arg0;                       // storage for the 1st argument or a readonly collection.  See IArgumentProvider
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
            switch (index)
            {
                case 0: return ReturnObject<Expression>(_arg0);
                case 1: return _arg1;
                case 2: return _arg2;
                default: throw new InvalidOperationException();
            }
        }

        public override int ArgumentCount
        {
            get
            {
                return 3;
            }
        }

        internal override ReadOnlyCollection<Expression> GetOrMakeArguments()
        {
            return ReturnReadOnly(this, ref _arg0);
        }

        internal override MethodCallExpression Rewrite(Expression instance, IList<Expression> args)
        {
            Debug.Assert(instance != null);
            Debug.Assert(args == null || args.Count == 3);

            if (args != null)
            {
                return Expression.Call(instance, Method, args[0], args[1], args[2]);
            }
            return Expression.Call(instance, Method, ReturnObject<Expression>(_arg0), _arg1, _arg2);
        }
    }

    #endregion

    public partial class Expression
    {
        #region Call

        ///<summary>Creates a <see cref="T:System.Linq.Expressions.MethodCallExpression" /> that represents a call to a static method that takes no arguments.</summary>
        ///<returns>A <see cref="T:System.Linq.Expressions.MethodCallExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.Call" /> and the <see cref="P:System.Linq.Expressions.MethodCallExpression.Object" /> and <see cref="P:System.Linq.Expressions.MethodCallExpression.Method" /> properties set to the specified values.</returns>
        ///<param name="method">A <see cref="T:System.Reflection.MethodInfo" /> to set the <see cref="P:System.Linq.Expressions.MethodCallExpression.Method" /> property equal to.</param>
        ///<exception cref="T:System.ArgumentNullException">
        ///<paramref name="method" /> is null.</exception>
        internal static MethodCallExpression Call(MethodInfo method)
        {
            // NB: Keeping this method as internal to avoid public API changes; it's internal for MethodCallExpression0.Rewrite.

            ContractUtils.RequiresNotNull(method, "method");

            ParameterInfo[] pis = ValidateMethodAndGetParameters(null, method);

            ValidateArgumentCount(method, ExpressionType.Call, 0, pis);

            return new MethodCallExpression0(method);
        }

        ///<summary>Creates a <see cref="T:System.Linq.Expressions.MethodCallExpression" /> that represents a call to a static method that takes one argument.</summary>
        ///<returns>A <see cref="T:System.Linq.Expressions.MethodCallExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.Call" /> and the <see cref="P:System.Linq.Expressions.MethodCallExpression.Object" /> and <see cref="P:System.Linq.Expressions.MethodCallExpression.Method" /> properties set to the specified values.</returns>
        ///<param name="method">A <see cref="T:System.Reflection.MethodInfo" /> to set the <see cref="P:System.Linq.Expressions.MethodCallExpression.Method" /> property equal to.</param>
        ///<param name="arg0">The <see cref="Expression" /> that represents the first argument.</param>
        ///<exception cref="T:System.ArgumentNullException">
        ///<paramref name="method" /> is null.</exception>
        public static MethodCallExpression Call(MethodInfo method, Expression arg0)
        {
            ContractUtils.RequiresNotNull(method, "method");
            ContractUtils.RequiresNotNull(arg0, "arg0");

            ParameterInfo[] pis = ValidateMethodAndGetParameters(null, method);

            ValidateArgumentCount(method, ExpressionType.Call, 1, pis);

            arg0 = ValidateOneArgument(method, ExpressionType.Call, arg0, pis[0]);

            return new MethodCallExpression1(method, arg0);
        }

        ///<summary>Creates a <see cref="T:System.Linq.Expressions.MethodCallExpression" /> that represents a call to a static method that takes two arguments.</summary>
        ///<returns>A <see cref="T:System.Linq.Expressions.MethodCallExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.Call" /> and the <see cref="P:System.Linq.Expressions.MethodCallExpression.Object" /> and <see cref="P:System.Linq.Expressions.MethodCallExpression.Method" /> properties set to the specified values.</returns>
        ///<param name="method">A <see cref="T:System.Reflection.MethodInfo" /> to set the <see cref="P:System.Linq.Expressions.MethodCallExpression.Method" /> property equal to.</param>
        ///<param name="arg0">The <see cref="Expression" /> that represents the first argument.</param>
        ///<param name="arg1">The <see cref="Expression" /> that represents the second argument.</param>
        ///<exception cref="T:System.ArgumentNullException">
        ///<paramref name="method" /> is null.</exception>
        public static MethodCallExpression Call(MethodInfo method, Expression arg0, Expression arg1)
        {
            ContractUtils.RequiresNotNull(method, "method");
            ContractUtils.RequiresNotNull(arg0, "arg0");
            ContractUtils.RequiresNotNull(arg1, "arg1");

            ParameterInfo[] pis = ValidateMethodAndGetParameters(null, method);

            ValidateArgumentCount(method, ExpressionType.Call, 2, pis);

            arg0 = ValidateOneArgument(method, ExpressionType.Call, arg0, pis[0]);
            arg1 = ValidateOneArgument(method, ExpressionType.Call, arg1, pis[1]);

            return new MethodCallExpression2(method, arg0, arg1);
        }

        ///<summary>Creates a <see cref="T:System.Linq.Expressions.MethodCallExpression" /> that represents a call to a static method that takes three arguments.</summary>
        ///<returns>A <see cref="T:System.Linq.Expressions.MethodCallExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.Call" /> and the <see cref="P:System.Linq.Expressions.MethodCallExpression.Object" /> and <see cref="P:System.Linq.Expressions.MethodCallExpression.Method" /> properties set to the specified values.</returns>
        ///<param name="method">A <see cref="T:System.Reflection.MethodInfo" /> to set the <see cref="P:System.Linq.Expressions.MethodCallExpression.Method" /> property equal to.</param>
        ///<param name="arg0">The <see cref="Expression" /> that represents the first argument.</param>
        ///<param name="arg1">The <see cref="Expression" /> that represents the second argument.</param>
        ///<param name="arg2">The <see cref="Expression" /> that represents the third argument.</param>
        ///<exception cref="T:System.ArgumentNullException">
        ///<paramref name="method" /> is null.</exception>
        public static MethodCallExpression Call(MethodInfo method, Expression arg0, Expression arg1, Expression arg2)
        {
            ContractUtils.RequiresNotNull(method, "method");
            ContractUtils.RequiresNotNull(arg0, "arg0");
            ContractUtils.RequiresNotNull(arg1, "arg1");
            ContractUtils.RequiresNotNull(arg2, "arg2");

            ParameterInfo[] pis = ValidateMethodAndGetParameters(null, method);

            ValidateArgumentCount(method, ExpressionType.Call, 3, pis);

            arg0 = ValidateOneArgument(method, ExpressionType.Call, arg0, pis[0]);
            arg1 = ValidateOneArgument(method, ExpressionType.Call, arg1, pis[1]);
            arg2 = ValidateOneArgument(method, ExpressionType.Call, arg2, pis[2]);

            return new MethodCallExpression3(method, arg0, arg1, arg2);
        }

        ///<summary>Creates a <see cref="T:System.Linq.Expressions.MethodCallExpression" /> that represents a call to a static method that takes four arguments.</summary>
        ///<returns>A <see cref="T:System.Linq.Expressions.MethodCallExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.Call" /> and the <see cref="P:System.Linq.Expressions.MethodCallExpression.Object" /> and <see cref="P:System.Linq.Expressions.MethodCallExpression.Method" /> properties set to the specified values.</returns>
        ///<param name="method">A <see cref="T:System.Reflection.MethodInfo" /> to set the <see cref="P:System.Linq.Expressions.MethodCallExpression.Method" /> property equal to.</param>
        ///<param name="arg0">The <see cref="Expression" /> that represents the first argument.</param>
        ///<param name="arg1">The <see cref="Expression" /> that represents the second argument.</param>
        ///<param name="arg2">The <see cref="Expression" /> that represents the third argument.</param>
        ///<param name="arg3">The <see cref="Expression" /> that represents the fourth argument.</param>
        ///<exception cref="T:System.ArgumentNullException">
        ///<paramref name="method" /> is null.</exception>
        public static MethodCallExpression Call(MethodInfo method, Expression arg0, Expression arg1, Expression arg2, Expression arg3)
        {
            ContractUtils.RequiresNotNull(method, "method");
            ContractUtils.RequiresNotNull(arg0, "arg0");
            ContractUtils.RequiresNotNull(arg1, "arg1");
            ContractUtils.RequiresNotNull(arg2, "arg2");
            ContractUtils.RequiresNotNull(arg3, "arg3");

            ParameterInfo[] pis = ValidateMethodAndGetParameters(null, method);

            ValidateArgumentCount(method, ExpressionType.Call, 4, pis);

            arg0 = ValidateOneArgument(method, ExpressionType.Call, arg0, pis[0]);
            arg1 = ValidateOneArgument(method, ExpressionType.Call, arg1, pis[1]);
            arg2 = ValidateOneArgument(method, ExpressionType.Call, arg2, pis[2]);
            arg3 = ValidateOneArgument(method, ExpressionType.Call, arg3, pis[3]);

            return new MethodCallExpression4(method, arg0, arg1, arg2, arg3);
        }

        ///<summary>Creates a <see cref="T:System.Linq.Expressions.MethodCallExpression" /> that represents a call to a static method that takes five arguments.</summary>
        ///<returns>A <see cref="T:System.Linq.Expressions.MethodCallExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.Call" /> and the <see cref="P:System.Linq.Expressions.MethodCallExpression.Object" /> and <see cref="P:System.Linq.Expressions.MethodCallExpression.Method" /> properties set to the specified values.</returns>
        ///<param name="method">A <see cref="T:System.Reflection.MethodInfo" /> to set the <see cref="P:System.Linq.Expressions.MethodCallExpression.Method" /> property equal to.</param>
        ///<param name="arg0">The <see cref="Expression" /> that represents the first argument.</param>
        ///<param name="arg1">The <see cref="Expression" /> that represents the second argument.</param>
        ///<param name="arg2">The <see cref="Expression" /> that represents the third argument.</param>
        ///<param name="arg3">The <see cref="Expression" /> that represents the fourth argument.</param>
        ///<param name="arg4">The <see cref="Expression" /> that represents the fifth argument.</param>
        ///<exception cref="T:System.ArgumentNullException">
        ///<paramref name="method" /> is null.</exception>
        ///<returns>A <see cref="T:System.Linq.Expressions.MethodCallExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.Call" /> and the <see cref="P:System.Linq.Expressions.MethodCallExpression.Object" /> and <see cref="P:System.Linq.Expressions.MethodCallExpression.Method" /> properties set to the specified values.</returns>
        public static MethodCallExpression Call(MethodInfo method, Expression arg0, Expression arg1, Expression arg2, Expression arg3, Expression arg4)
        {
            ContractUtils.RequiresNotNull(method, "method");
            ContractUtils.RequiresNotNull(arg0, "arg0");
            ContractUtils.RequiresNotNull(arg1, "arg1");
            ContractUtils.RequiresNotNull(arg2, "arg2");
            ContractUtils.RequiresNotNull(arg3, "arg3");
            ContractUtils.RequiresNotNull(arg4, "arg4");

            ParameterInfo[] pis = ValidateMethodAndGetParameters(null, method);

            ValidateArgumentCount(method, ExpressionType.Call, 5, pis);

            arg0 = ValidateOneArgument(method, ExpressionType.Call, arg0, pis[0]);
            arg1 = ValidateOneArgument(method, ExpressionType.Call, arg1, pis[1]);
            arg2 = ValidateOneArgument(method, ExpressionType.Call, arg2, pis[2]);
            arg3 = ValidateOneArgument(method, ExpressionType.Call, arg3, pis[3]);
            arg4 = ValidateOneArgument(method, ExpressionType.Call, arg4, pis[4]);

            return new MethodCallExpression5(method, arg0, arg1, arg2, arg3, arg4);
        }

        /// <summary>
        /// Creates a <see cref="MethodCallExpression" /> that represents a call to a static (Shared in Visual Basic) method.
        /// </summary>
        /// <param name="method">The <see cref="MethodInfo" /> that represents the target method.</param>
        /// <param name="arguments">The array of one or more of <see cref="Expression" /> that represents the call arguments.</param>
        ///<returns>A <see cref="T:System.Linq.Expressions.MethodCallExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.Call" /> and the <see cref="P:System.Linq.Expressions.MethodCallExpression.Object" /> and <see cref="P:System.Linq.Expressions.MethodCallExpression.Method" /> properties set to the specified values.</returns>
        public static MethodCallExpression Call(MethodInfo method, params Expression[] arguments)
        {
            return Call(null, method, arguments);
        }

        /// <summary>
        /// Creates a <see cref="MethodCallExpression" /> that represents a call to a static (Shared in Visual Basic) method.
        /// </summary>
        /// <param name="method">The <see cref="MethodInfo" /> that represents the target method.</param>
        /// <param name="arguments">A collection of <see cref="Expression" /> that represents the call arguments.</param>
        ///<returns>A <see cref="T:System.Linq.Expressions.MethodCallExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.Call" /> and the <see cref="P:System.Linq.Expressions.MethodCallExpression.Object" /> and <see cref="P:System.Linq.Expressions.MethodCallExpression.Method" /> properties set to the specified values.</returns>
        public static MethodCallExpression Call(MethodInfo method, IEnumerable<Expression> arguments)
        {
            return Call(null, method, arguments);
        }

        /// <summary>
        /// Creates a <see cref="MethodCallExpression" /> that represents a call to a method that takes no arguments.
        /// </summary>
        /// <param name="instance">An <see cref="Expression" /> that specifies the instance for an instance call. (pass null for a static (Shared in Visual Basic) method).</param>
        /// <param name="method">The <see cref="MethodInfo" /> that represents the target method.</param>
        ///<returns>A <see cref="T:System.Linq.Expressions.MethodCallExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.Call" /> and the <see cref="P:System.Linq.Expressions.MethodCallExpression.Object" /> and <see cref="P:System.Linq.Expressions.MethodCallExpression.Method" /> properties set to the specified values.</returns>
        public static MethodCallExpression Call(Expression instance, MethodInfo method)
        {
            ContractUtils.RequiresNotNull(method, "method");

            ParameterInfo[] pis = ValidateMethodAndGetParameters(instance, method);

            ValidateArgumentCount(method, ExpressionType.Call, 0, pis);

            if (instance != null)
            {
                return new InstanceMethodCallExpression0(method, instance);
            }

            return new MethodCallExpression0(method);
        }

        /// <summary>
        /// Creates a <see cref="MethodCallExpression" /> that represents a method call.
        /// </summary>
        /// <param name="instance">An <see cref="Expression" /> that specifies the instance for an instance call. (pass null for a static (Shared in Visual Basic) method).</param>
        /// <param name="method">The <see cref="MethodInfo" /> that represents the target method.</param>
        /// <param name="arguments">An array of one or more of <see cref="Expression" /> that represents the call arguments.</param>
        ///<returns>A <see cref="T:System.Linq.Expressions.MethodCallExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.Call" /> and the <see cref="P:System.Linq.Expressions.MethodCallExpression.Object" /> and <see cref="P:System.Linq.Expressions.MethodCallExpression.Method" /> properties set to the specified values.</returns>
        public static MethodCallExpression Call(Expression instance, MethodInfo method, params Expression[] arguments)
        {
            return Call(instance, method, (IEnumerable<Expression>)arguments);
        }

        /// <summary>
        /// Creates a <see cref="MethodCallExpression" /> that represents a call to a method that takes one argument.
        /// </summary>
        /// <param name="instance">An <see cref="Expression" /> that specifies the instance for an instance call. (pass null for a static (Shared in Visual Basic) method).</param>
        /// <param name="method">The <see cref="MethodInfo" /> that represents the target method.</param>
        /// <param name="arg0">The <see cref="Expression" /> that represents the first argument.</param>
        ///<returns>A <see cref="T:System.Linq.Expressions.MethodCallExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.Call" /> and the <see cref="P:System.Linq.Expressions.MethodCallExpression.Object" /> and <see cref="P:System.Linq.Expressions.MethodCallExpression.Method" /> properties set to the specified values.</returns>
        internal static MethodCallExpression Call(Expression instance, MethodInfo method, Expression arg0)
        {
            // COMPAT: This method is marked as non-public to ensure compile-time compatibility for Expression.Call(e, m, null).

            ContractUtils.RequiresNotNull(method, "method");
            ContractUtils.RequiresNotNull(arg0, "arg0");

            ParameterInfo[] pis = ValidateMethodAndGetParameters(instance, method);

            ValidateArgumentCount(method, ExpressionType.Call, 1, pis);

            arg0 = ValidateOneArgument(method, ExpressionType.Call, arg0, pis[0]);

            if (instance != null)
            {
                return new InstanceMethodCallExpression1(method, instance, arg0);
            }

            return new MethodCallExpression1(method, arg0);
        }

        /// <summary>
        /// Creates a <see cref="MethodCallExpression" /> that represents a call to a method that takes two arguments.
        /// </summary>
        /// <param name="instance">An <see cref="Expression" /> that specifies the instance for an instance call. (pass null for a static (Shared in Visual Basic) method).</param>
        /// <param name="method">The <see cref="MethodInfo" /> that represents the target method.</param>
        /// <param name="arg0">The <see cref="Expression" /> that represents the first argument.</param>
        /// <param name="arg1">The <see cref="Expression" /> that represents the second argument.</param>
        ///<returns>A <see cref="T:System.Linq.Expressions.MethodCallExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.Call" /> and the <see cref="P:System.Linq.Expressions.MethodCallExpression.Object" /> and <see cref="P:System.Linq.Expressions.MethodCallExpression.Method" /> properties set to the specified values.</returns>
        public static MethodCallExpression Call(Expression instance, MethodInfo method, Expression arg0, Expression arg1)
        {
            ContractUtils.RequiresNotNull(method, "method");
            ContractUtils.RequiresNotNull(arg0, "arg0");
            ContractUtils.RequiresNotNull(arg1, "arg1");

            ParameterInfo[] pis = ValidateMethodAndGetParameters(instance, method);

            ValidateArgumentCount(method, ExpressionType.Call, 2, pis);

            arg0 = ValidateOneArgument(method, ExpressionType.Call, arg0, pis[0]);
            arg1 = ValidateOneArgument(method, ExpressionType.Call, arg1, pis[1]);

            if (instance != null)
            {
                return new InstanceMethodCallExpression2(method, instance, arg0, arg1);
            }

            return new MethodCallExpression2(method, arg0, arg1);
        }

        /// <summary>
        /// Creates a <see cref="MethodCallExpression" /> that represents a call to a method that takes three arguments.
        /// </summary>
        /// <param name="instance">An <see cref="Expression" /> that specifies the instance for an instance call. (pass null for a static (Shared in Visual Basic) method).</param>
        /// <param name="method">The <see cref="MethodInfo" /> that represents the target method.</param>
        /// <param name="arg0">The <see cref="Expression" /> that represents the first argument.</param>
        /// <param name="arg1">The <see cref="Expression" /> that represents the second argument.</param>
        /// <param name="arg2">The <see cref="Expression" /> that represents the third argument.</param>
        ///<returns>A <see cref="T:System.Linq.Expressions.MethodCallExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.Call" /> and the <see cref="P:System.Linq.Expressions.MethodCallExpression.Object" /> and <see cref="P:System.Linq.Expressions.MethodCallExpression.Method" /> properties set to the specified values.</returns>
        public static MethodCallExpression Call(Expression instance, MethodInfo method, Expression arg0, Expression arg1, Expression arg2)
        {
            ContractUtils.RequiresNotNull(method, "method");
            ContractUtils.RequiresNotNull(arg0, "arg0");
            ContractUtils.RequiresNotNull(arg1, "arg1");
            ContractUtils.RequiresNotNull(arg2, "arg2");

            ParameterInfo[] pis = ValidateMethodAndGetParameters(instance, method);

            ValidateArgumentCount(method, ExpressionType.Call, 3, pis);

            arg0 = ValidateOneArgument(method, ExpressionType.Call, arg0, pis[0]);
            arg1 = ValidateOneArgument(method, ExpressionType.Call, arg1, pis[1]);
            arg2 = ValidateOneArgument(method, ExpressionType.Call, arg2, pis[2]);

            if (instance != null)
            {
                return new InstanceMethodCallExpression3(method, instance, arg0, arg1, arg2);
            }
            return new MethodCallExpression3(method, arg0, arg1, arg2);
        }

        ///<summary>Creates a <see cref="T:System.Linq.Expressions.MethodCallExpression" /> that represents a call to an instance method by calling the appropriate factory method.</summary>
        ///<returns>A <see cref="T:System.Linq.Expressions.MethodCallExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.Call" />, the <see cref="P:System.Linq.Expressions.MethodCallExpression.Object" /> property equal to <paramref name="instance" />, <see cref="P:System.Linq.Expressions.MethodCallExpression.Method" /> set to the <see cref="T:System.Reflection.MethodInfo" /> that represents the specified instance method, and <see cref="P:System.Linq.Expressions.MethodCallExpression.Arguments" /> set to the specified arguments.</returns>
        ///<param name="instance">An <see cref="T:System.Linq.Expressions.Expression" /> whose <see cref="P:System.Linq.Expressions.Expression.Type" /> property value will be searched for a specific method.</param>
        ///<param name="methodName">The name of the method.</param>
        ///<param name="typeArguments">
        ///An array of <see cref="T:System.Type" /> objects that specify the type parameters of the generic method.
        ///This argument should be null when <paramref name="methodName" /> specifies a non-generic method.
        ///</param>
        ///<param name="arguments">An array of <see cref="T:System.Linq.Expressions.Expression" /> objects that represents the arguments to the method.</param>
        ///<exception cref="T:System.ArgumentNullException">
        ///<paramref name="instance" /> or <paramref name="methodName" /> is null.</exception>
        ///<exception cref="T:System.InvalidOperationException">No method whose name is <paramref name="methodName" />, whose type parameters match <paramref name="typeArguments" />, and whose parameter types match <paramref name="arguments" /> is found in <paramref name="instance" />.Type or its base types.-or-More than one method whose name is <paramref name="methodName" />, whose type parameters match <paramref name="typeArguments" />, and whose parameter types match <paramref name="arguments" /> is found in <paramref name="instance" />.Type or its base types.</exception>
        public static MethodCallExpression Call(Expression instance, string methodName, Type[] typeArguments, params Expression[] arguments)
        {
            ContractUtils.RequiresNotNull(instance, "instance");
            ContractUtils.RequiresNotNull(methodName, "methodName");
            if (arguments == null)
            {
                arguments = Array.Empty<Expression>();
            }

            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;
            return Expression.Call(instance, FindMethod(instance.Type, methodName, typeArguments, arguments, flags), arguments);
        }

        ///<summary>Creates a <see cref="T:System.Linq.Expressions.MethodCallExpression" /> that represents a call to a static (Shared in Visual Basic) method by calling the appropriate factory method.</summary>
        ///<returns>A <see cref="T:System.Linq.Expressions.MethodCallExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.Call" />, the <see cref="P:System.Linq.Expressions.MethodCallExpression.Method" /> property set to the <see cref="T:System.Reflection.MethodInfo" /> that represents the specified static (Shared in Visual Basic) method, and the <see cref="P:System.Linq.Expressions.MethodCallExpression.Arguments" /> property set to the specified arguments.</returns>
        ///<param name="type">The <see cref="T:System.Type" /> that specifies the type that contains the specified static (Shared in Visual Basic) method.</param>
        ///<param name="methodName">The name of the method.</param>
        ///<param name="typeArguments">
        ///An array of <see cref="T:System.Type" /> objects that specify the type parameters of the generic method.
        ///This argument should be null when <paramref name="methodName" /> specifies a non-generic method.
        ///</param>
        ///<param name="arguments">An array of <see cref="T:System.Linq.Expressions.Expression" /> objects that represent the arguments to the method.</param>
        ///<exception cref="T:System.ArgumentNullException">
        ///<paramref name="type" /> or <paramref name="methodName" /> is null.</exception>
        ///<exception cref="T:System.InvalidOperationException">No method whose name is <paramref name="methodName" />, whose type parameters match <paramref name="typeArguments" />, and whose parameter types match <paramref name="arguments" /> is found in <paramref name="type" /> or its base types.-or-More than one method whose name is <paramref name="methodName" />, whose type parameters match <paramref name="typeArguments" />, and whose parameter types match <paramref name="arguments" /> is found in <paramref name="type" /> or its base types.</exception>
        public static MethodCallExpression Call(Type type, string methodName, Type[] typeArguments, params Expression[] arguments)
        {
            ContractUtils.RequiresNotNull(type, "type");
            ContractUtils.RequiresNotNull(methodName, "methodName");

            if (arguments == null) arguments = Array.Empty<Expression>();
            BindingFlags flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;
            return Expression.Call(null, FindMethod(type, methodName, typeArguments, arguments, flags), arguments);
        }

        ///<summary>Creates a <see cref="T:System.Linq.Expressions.MethodCallExpression" /> that represents a method call.</summary>
        ///<returns>A <see cref="T:System.Linq.Expressions.MethodCallExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.Call" /> and the <see cref="P:System.Linq.Expressions.MethodCallExpression.Object" />, <see cref="P:System.Linq.Expressions.MethodCallExpression.Method" />, and <see cref="P:System.Linq.Expressions.MethodCallExpression.Arguments" /> properties set to the specified values.</returns>
        ///<param name="instance">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.MethodCallExpression.Object" /> property equal to (pass null for a static (Shared in Visual Basic) method).</param>
        ///<param name="method">A <see cref="T:System.Reflection.MethodInfo" /> to set the <see cref="P:System.Linq.Expressions.MethodCallExpression.Method" /> property equal to.</param>
        ///<param name="arguments">An <see cref="T:System.Collections.Generic.IEnumerable`1" /> that contains <see cref="T:System.Linq.Expressions.Expression" /> objects to use to populate the <see cref="P:System.Linq.Expressions.MethodCallExpression.Arguments" /> collection.</param>
        ///<exception cref="T:System.ArgumentNullException">
        ///<paramref name="method" /> is null.-or-<paramref name="instance" /> is null and <paramref name="method" /> represents an instance method.</exception>
        ///<exception cref="T:System.ArgumentException">
        ///<paramref name="instance" />.Type is not assignable to the declaring type of the method represented by <paramref name="method" />.-or-The number of elements in <paramref name="arguments" /> does not equal the number of parameters for the method represented by <paramref name="method" />.-or-One or more of the elements of <paramref name="arguments" /> is not assignable to the corresponding parameter for the method represented by <paramref name="method" />.</exception>
        public static MethodCallExpression Call(Expression instance, MethodInfo method, IEnumerable<Expression> arguments)
        {
            IReadOnlyList<Expression> argumentList = arguments as IReadOnlyList<Expression>;

            if (argumentList != null)
            {
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
            }

            ContractUtils.RequiresNotNull(method, "method");

            ReadOnlyCollection<Expression> argList = arguments.ToReadOnly();

            ValidateMethodInfo(method);
            ValidateStaticOrInstanceMethod(instance, method);
            ValidateArgumentTypes(method, ExpressionType.Call, ref argList);

            if (instance == null)
            {
                return new MethodCallExpressionN(method, argList);
            }
            else
            {
                return new InstanceMethodCallExpressionN(method, instance, argList);
            }
        }

        private static ParameterInfo[] ValidateMethodAndGetParameters(Expression instance, MethodInfo method)
        {
            ValidateMethodInfo(method);
            ValidateStaticOrInstanceMethod(instance, method);

            return GetParametersForValidation(method, ExpressionType.Call);
        }

        private static void ValidateStaticOrInstanceMethod(Expression instance, MethodInfo method)
        {
            if (method.IsStatic)
            {
                if (instance != null) throw new ArgumentException(Strings.OnlyStaticMethodsHaveNullInstance, nameof(instance));
            }
            else
            {
                if (instance == null) throw new ArgumentException(Strings.OnlyStaticMethodsHaveNullInstance, nameof(method));
                RequiresCanRead(instance, "instance");
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

        public static void ValidateArgumentTypes(MethodBase method, ExpressionType nodeKind, ref ReadOnlyCollection<Expression> arguments)
        {
            ExpressionUtils.ValidateArgumentTypes(method, nodeKind, ref arguments);
        }

        private static ParameterInfo[] GetParametersForValidation(MethodBase method, ExpressionType nodeKind)
        {
            return ExpressionUtils.GetParametersForValidation(method, nodeKind);
        }

        public static void ValidateArgumentCount(MethodBase method, ExpressionType nodeKind, int count, ParameterInfo[] pis)
        {
            ExpressionUtils.ValidateArgumentCount(method, nodeKind, count, pis);
        }

        public static Expression ValidateOneArgument(MethodBase method, ExpressionType nodeKind, Expression arg, ParameterInfo pi)
        {
            return ExpressionUtils.ValidateOneArgument(method, nodeKind, arg, pi);
        }

        // Attempts to auto-quote the expression tree. Returns true if it succeeded, false otherwise.
        private static bool TryQuote(Type parameterType, ref Expression argument)
        {
            return ExpressionUtils.TryQuote(parameterType, ref argument);
        }

        private static MethodInfo FindMethod(Type type, string methodName, Type[] typeArgs, Expression[] args, BindingFlags flags)
        {
            MemberInfo[] members = type.GetMethodsIgnoreCase(flags, methodName);
            if (members == null || members.Length == 0)
                throw Error.MethodDoesNotExistOnType(methodName, type);

            MethodInfo method;

            var methodInfos = members.Map(t => (MethodInfo)t);
            int count = FindBestMethod(methodInfos, typeArgs, args, out method);

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

        private static int FindBestMethod(IEnumerable<MethodInfo> methods, Type[] typeArgs, Expression[] args, out MethodInfo method)
        {
            int count = 0;
            method = null;
            foreach (MethodInfo mi in methods)
            {
                MethodInfo moo = ApplyTypeArgs(mi, typeArgs);
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
            return count;
        }

        private static bool IsCompatible(MethodBase m, Expression[] args)
        {
            ParameterInfo[] parms = m.GetParametersCached();
            if (parms.Length != args.Length)
                return false;
            for (int i = 0; i < args.Length; i++)
            {
                Expression arg = args[i];
                ContractUtils.RequiresNotNull(arg, "argument");
                Type argType = arg.Type;
                Type pType = parms[i].ParameterType;
                if (pType.IsByRef)
                {
                    pType = pType.GetElementType();
                }
                if (!TypeUtils.AreReferenceAssignable(pType, argType) &&
                    !(TypeUtils.IsSameOrSubclass(typeof(LambdaExpression), pType) && pType.IsAssignableFrom(arg.GetType())))
                {
                    return false;
                }
            }
            return true;
        }

        private static MethodInfo ApplyTypeArgs(MethodInfo m, Type[] typeArgs)
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


        #endregion

        #region ArrayIndex

        ///<summary>Creates a <see cref="T:System.Linq.Expressions.MethodCallExpression" /> that represents applying an array index operator to a multi-dimensional array.</summary>
        ///<returns>A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.ArrayIndex" /> and the <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> and <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> properties set to the specified values.</returns>
        ///<param name="array">An array of <see cref="T:System.Linq.Expressions.Expression" /> instances - indexes for the array index operation.</param>
        ///<param name="indexes">An array that contains <see cref="T:System.Linq.Expressions.Expression" /> objects to use to populate the <see cref="P:System.Linq.Expressions.MethodCallExpression.Arguments" /> collection.</param>
        public static MethodCallExpression ArrayIndex(Expression array, params Expression[] indexes)
        {
            return ArrayIndex(array, (IEnumerable<Expression>)indexes);
        }

        ///<summary>Creates a <see cref="T:System.Linq.Expressions.MethodCallExpression" /> that represents applying an array index operator to an array of rank more than one.</summary>
        ///<returns>A <see cref="T:System.Linq.Expressions.MethodCallExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.Call" /> and the <see cref="P:System.Linq.Expressions.MethodCallExpression.Object" /> and <see cref="P:System.Linq.Expressions.MethodCallExpression.Arguments" /> properties set to the specified values.</returns>
        ///<param name="array">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.MethodCallExpression.Object" /> property equal to.</param>
        ///<param name="indexes">An <see cref="T:System.Collections.Generic.IEnumerable`1" /> that contains <see cref="T:System.Linq.Expressions.Expression" /> objects to use to populate the <see cref="P:System.Linq.Expressions.MethodCallExpression.Arguments" /> collection.</param>
        ///<exception cref="T:System.ArgumentNullException">
        ///<paramref name="array" /> or <paramref name="indexes" /> is null.</exception>
        ///<exception cref="T:System.ArgumentException">
        ///<paramref name="array" />.Type does not represent an array type.-or-The rank of <paramref name="array" />.Type does not match the number of elements in <paramref name="indexes" />.-or-The <see cref="P:System.Linq.Expressions.Expression.Type" /> property of one or more elements of <paramref name="indexes" /> does not represent the <see cref="T:System.Int32" /> type.</exception>
        public static MethodCallExpression ArrayIndex(Expression array, IEnumerable<Expression> indexes)
        {
            RequiresCanRead(array, "array");
            ContractUtils.RequiresNotNull(indexes, "indexes");

            Type arrayType = array.Type;
            if (!arrayType.IsArray)
            {
                throw Error.ArgumentMustBeArray();
            }

            ReadOnlyCollection<Expression> indexList = indexes.ToReadOnly();
            if (arrayType.GetArrayRank() != indexList.Count)
            {
                throw Error.IncorrectNumberOfIndexes();
            }

            foreach (Expression e in indexList)
            {
                RequiresCanRead(e, "indexes");
                if (e.Type != typeof(int))
                {
                    throw Error.ArgumentMustBeArrayIndexType();
                }
            }

            MethodInfo mi = array.Type.GetMethod("Get", BindingFlags.Public | BindingFlags.Instance);
            return Call(array, mi, indexList);
        }
        #endregion

    }
}
