// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic.Utils;
using System.Threading;

namespace System.Linq.Expressions
{
    /// <summary>
    /// Represents a block that contains a sequence of expressions where variables can be defined.
    /// </summary>
    [DebuggerTypeProxy(typeof(BlockExpressionProxy))]
    public class BlockExpression : Expression
    {
        /// <summary>
        /// Gets the expressions in this block.
        /// </summary>
        public ReadOnlyCollection<Expression> Expressions => GetOrMakeExpressions();

        /// <summary>
        /// Gets the variables defined in this block.
        /// </summary>
        public ReadOnlyCollection<ParameterExpression> Variables => GetOrMakeVariables();

        /// <summary>
        /// Gets the last expression in this block.
        /// </summary>
        public Expression Result
        {
            get
            {
                Debug.Assert(ExpressionCount > 0);
                return GetExpression(ExpressionCount - 1);
            }
        }

        internal BlockExpression()
        {
        }

        /// <summary>
        /// Dispatches to the specific visit method for this node type.
        /// </summary>
        protected internal override Expression Accept(ExpressionVisitor visitor)
        {
            return visitor.VisitBlock(this);
        }

        /// <summary>
        /// Returns the node type of this Expression. Extension nodes should return
        /// ExpressionType.Extension when overriding this method.
        /// </summary>
        /// <returns>The <see cref="ExpressionType"/> of the expression.</returns>
        public sealed override ExpressionType NodeType => ExpressionType.Block;

        /// <summary>
        /// Gets the static type of the expression that this <see cref="Expression"/> represents.
        /// </summary>
        /// <returns>The <see cref="System.Type"/> that represents the static type of the expression.</returns>
        public override Type Type => GetExpression(ExpressionCount - 1).Type;

        /// <summary>
        /// Creates a new expression that is like this one, but using the
        /// supplied children. If all of the children are the same, it will
        /// return this expression.
        /// </summary>
        /// <param name="variables">The <see cref="Variables"/> property of the result.</param>
        /// <param name="expressions">The <see cref="Expressions"/> property of the result.</param>
        /// <returns>This expression if no children changed, or an expression with the updated children.</returns>
        public BlockExpression Update(IEnumerable<ParameterExpression> variables, IEnumerable<Expression> expressions)
        {
            if (variables == Variables && expressions == Expressions)
            {
                return this;
            }

            return Expression.Block(Type, variables, expressions);
        }

        [ExcludeFromCodeCoverage] // Unreachable
        internal virtual Expression GetExpression(int index)
        {
            throw ContractUtils.Unreachable;
        }

        [ExcludeFromCodeCoverage] // Unreachable
        internal virtual int ExpressionCount
        {
            get
            {
                throw ContractUtils.Unreachable;
            }
        }

        [ExcludeFromCodeCoverage] // Unreachable
        internal virtual ReadOnlyCollection<Expression> GetOrMakeExpressions()
        {
            throw ContractUtils.Unreachable;
        }

        internal virtual ReadOnlyCollection<ParameterExpression> GetOrMakeVariables()
        {
            return EmptyReadOnlyCollection<ParameterExpression>.Instance;
        }

        /// <summary>
        /// Makes a copy of this node replacing the parameters/args with the provided values.  The 
        /// shape of the parameters/args needs to match the shape of the current block - in other
        /// words there should be the same # of parameters and args.
        /// 
        /// parameters can be null in which case the existing parameters are used.
        /// 
        /// This helper is provided to allow re-writing of nodes to not depend on the specific optimized
        /// subclass of BlockExpression which is being used. 
        /// </summary>
        [ExcludeFromCodeCoverage] // Unreachable
        internal virtual BlockExpression Rewrite(ReadOnlyCollection<ParameterExpression> variables, Expression[] args)
        {
            throw ContractUtils.Unreachable;
        }

        /// <summary>
        /// Helper used for ensuring we only return 1 instance of a ReadOnlyCollection of T.
        /// 
        /// This is similar to the ReturnReadOnly which only takes a single argument. This version
        /// supports nodes which hold onto 5 Expressions and puts all of the arguments into the
        /// ReadOnlyCollection.
        /// 
        /// Ultimately this means if we create the readonly collection we will be slightly more wasteful as we'll
        /// have a readonly collection + some fields in the type.  The DLR internally avoids accessing anything
        /// which would force the readonly collection to be created.
        /// 
        /// This is used by BlockExpression5 and MethodCallExpression5.
        /// </summary>
        internal static ReadOnlyCollection<Expression> ReturnReadOnlyExpressions(BlockExpression provider, ref object collection)
        {
            Expression tObj = collection as Expression;
            if (tObj != null)
            {
                // otherwise make sure only one readonly collection ever gets exposed
                Interlocked.CompareExchange(
                    ref collection,
                    new ReadOnlyCollection<Expression>(new BlockExpressionList(provider, tObj)),
                    tObj
                );
            }

            // and return what is not guaranteed to be a readonly collection
            return (ReadOnlyCollection<Expression>)collection;
        }
    }

    #region Specialized Subclasses

    internal sealed class Block2 : BlockExpression
    {
        private object _arg0;                   // storage for the 1st argument or a readonly collection.  See IArgumentProvider
        private readonly Expression _arg1;      // storage for the 2nd argument.

        internal Block2(Expression arg0, Expression arg1)
        {
            _arg0 = arg0;
            _arg1 = arg1;
        }

        internal override Expression GetExpression(int index)
        {
            switch (index)
            {
                case 0: return ReturnObject<Expression>(_arg0);
                case 1: return _arg1;
                default: throw Error.ArgumentOutOfRange(nameof(index));
            }
        }

        internal override int ExpressionCount => 2;

        internal override ReadOnlyCollection<Expression> GetOrMakeExpressions()
        {
            return ReturnReadOnlyExpressions(this, ref _arg0);
        }

        internal override BlockExpression Rewrite(ReadOnlyCollection<ParameterExpression> variables, Expression[] args)
        {
            Debug.Assert(args != null);
            Debug.Assert(args.Length == 2);
            Debug.Assert(variables == null || variables.Count == 0);

            return new Block2(args[0], args[1]);
        }
    }

    internal sealed class Block3 : BlockExpression
    {
        private object _arg0;                       // storage for the 1st argument or a readonly collection.  See IArgumentProvider
        private readonly Expression _arg1, _arg2;   // storage for the 2nd and 3rd arguments.

        internal Block3(Expression arg0, Expression arg1, Expression arg2)
        {
            _arg0 = arg0;
            _arg1 = arg1;
            _arg2 = arg2;
        }

        internal override Expression GetExpression(int index)
        {
            switch (index)
            {
                case 0: return ReturnObject<Expression>(_arg0);
                case 1: return _arg1;
                case 2: return _arg2;
                default: throw Error.ArgumentOutOfRange(nameof(index));
            }
        }

        internal override int ExpressionCount => 3;

        internal override ReadOnlyCollection<Expression> GetOrMakeExpressions()
        {
            return ReturnReadOnlyExpressions(this, ref _arg0);
        }

        internal override BlockExpression Rewrite(ReadOnlyCollection<ParameterExpression> variables, Expression[] args)
        {
            Debug.Assert(args != null);
            Debug.Assert(args.Length == 3);
            Debug.Assert(variables == null || variables.Count == 0);

            return new Block3(args[0], args[1], args[2]);
        }
    }

    internal sealed class Block4 : BlockExpression
    {
        private object _arg0;                               // storage for the 1st argument or a readonly collection.  See IArgumentProvider
        private readonly Expression _arg1, _arg2, _arg3;    // storarg for the 2nd, 3rd, and 4th arguments.

        internal Block4(Expression arg0, Expression arg1, Expression arg2, Expression arg3)
        {
            _arg0 = arg0;
            _arg1 = arg1;
            _arg2 = arg2;
            _arg3 = arg3;
        }

        internal override Expression GetExpression(int index)
        {
            switch (index)
            {
                case 0: return ReturnObject<Expression>(_arg0);
                case 1: return _arg1;
                case 2: return _arg2;
                case 3: return _arg3;
                default: throw Error.ArgumentOutOfRange(nameof(index));
            }
        }

        internal override int ExpressionCount => 4;

        internal override ReadOnlyCollection<Expression> GetOrMakeExpressions()
        {
            return ReturnReadOnlyExpressions(this, ref _arg0);
        }

        internal override BlockExpression Rewrite(ReadOnlyCollection<ParameterExpression> variables, Expression[] args)
        {
            Debug.Assert(args != null);
            Debug.Assert(args.Length == 4);
            Debug.Assert(variables == null || variables.Count == 0);

            return new Block4(args[0], args[1], args[2], args[3]);
        }
    }

    internal sealed class Block5 : BlockExpression
    {
        private object _arg0;                                       // storage for the 1st argument or a readonly collection.  See IArgumentProvider
        private readonly Expression _arg1, _arg2, _arg3, _arg4;     // storage for the 2nd - 5th args.

        internal Block5(Expression arg0, Expression arg1, Expression arg2, Expression arg3, Expression arg4)
        {
            _arg0 = arg0;
            _arg1 = arg1;
            _arg2 = arg2;
            _arg3 = arg3;
            _arg4 = arg4;
        }

        internal override Expression GetExpression(int index)
        {
            switch (index)
            {
                case 0: return ReturnObject<Expression>(_arg0);
                case 1: return _arg1;
                case 2: return _arg2;
                case 3: return _arg3;
                case 4: return _arg4;
                default: throw Error.ArgumentOutOfRange(nameof(index));
            }
        }

        internal override int ExpressionCount => 5;

        internal override ReadOnlyCollection<Expression> GetOrMakeExpressions()
        {
            return ReturnReadOnlyExpressions(this, ref _arg0);
        }

        internal override BlockExpression Rewrite(ReadOnlyCollection<ParameterExpression> variables, Expression[] args)
        {
            Debug.Assert(args != null);
            Debug.Assert(args.Length == 5);
            Debug.Assert(variables == null || variables.Count == 0);

            return new Block5(args[0], args[1], args[2], args[3], args[4]);
        }
    }

    internal class BlockN : BlockExpression
    {
        private IReadOnlyList<Expression> _expressions;         // either the original IList<Expression> or a ReadOnlyCollection if the user has accessed it.

        internal BlockN(IReadOnlyList<Expression> expressions)
        {
            Debug.Assert(expressions.Count != 0);

            _expressions = expressions;
        }

        internal override Expression GetExpression(int index)
        {
            Debug.Assert(index >= 0 && index < _expressions.Count);

            return _expressions[index];
        }

        internal override int ExpressionCount => _expressions.Count;

        internal override ReadOnlyCollection<Expression> GetOrMakeExpressions()
        {
            return ReturnReadOnly(ref _expressions);
        }

        internal override BlockExpression Rewrite(ReadOnlyCollection<ParameterExpression> variables, Expression[] args)
        {
            Debug.Assert(variables == null || variables.Count == 0);
            Debug.Assert(args != null);

            return new BlockN(args);
        }
    }

    internal class ScopeExpression : BlockExpression
    {
        private IReadOnlyList<ParameterExpression> _variables;      // list of variables or ReadOnlyCollection if the user has accessed the readonly collection

        internal ScopeExpression(IReadOnlyList<ParameterExpression> variables)
        {
            _variables = variables;
        }

        internal override ReadOnlyCollection<ParameterExpression> GetOrMakeVariables()
        {
            return ReturnReadOnly(ref _variables);
        }

        protected IReadOnlyList<ParameterExpression> VariablesList => _variables;

        // Used for rewrite of the nodes to either reuse existing set of variables if not rewritten.
        internal IReadOnlyList<ParameterExpression> ReuseOrValidateVariables(ReadOnlyCollection<ParameterExpression> variables)
        {
            if (variables != null && variables != VariablesList)
            {
                // Need to validate the new variables (uniqueness, not byref)
                ValidateVariables(variables, nameof(variables));
                return variables;
            }
            else
            {
                return VariablesList;
            }
        }
    }

    internal sealed class Scope1 : ScopeExpression
    {
        private object _body;

        internal Scope1(IReadOnlyList<ParameterExpression> variables, Expression body)
            : this(variables, (object)body)
        {
        }

        private Scope1(IReadOnlyList<ParameterExpression> variables, object body)
            : base(variables)
        {
            _body = body;
        }

        internal override Expression GetExpression(int index)
        {
            switch (index)
            {
                case 0: return ReturnObject<Expression>(_body);
                default: throw Error.ArgumentOutOfRange(nameof(index));
            }
        }

        internal override int ExpressionCount => 1;

        internal override ReadOnlyCollection<Expression> GetOrMakeExpressions()
        {
            return ReturnReadOnlyExpressions(this, ref _body);
        }

        internal override BlockExpression Rewrite(ReadOnlyCollection<ParameterExpression> variables, Expression[] args)
        {
            if (args == null)
            {
                Debug.Assert(variables.Count == Variables.Count);
                ValidateVariables(variables, nameof(variables));
                return new Scope1(variables, _body);
            }
            Debug.Assert(args.Length == 1);
            Debug.Assert(variables == null || variables.Count == Variables.Count);

            return new Scope1(ReuseOrValidateVariables(variables), args[0]);
        }
    }

    internal class ScopeN : ScopeExpression
    {
        private IReadOnlyList<Expression> _body;

        internal ScopeN(IReadOnlyList<ParameterExpression> variables, IReadOnlyList<Expression> body)
            : base(variables)
        {
            _body = body;
        }

        protected IReadOnlyList<Expression> Body => _body;

        internal override Expression GetExpression(int index) => _body[index];

        internal override int ExpressionCount => _body.Count;

        internal override ReadOnlyCollection<Expression> GetOrMakeExpressions()
        {
            return ReturnReadOnly(ref _body);
        }

        internal override BlockExpression Rewrite(ReadOnlyCollection<ParameterExpression> variables, Expression[] args)
        {
            if (args == null)
            {
                Debug.Assert(variables.Count == Variables.Count);
                ValidateVariables(variables, nameof(variables));
                return new ScopeN(variables, _body);
            }
            Debug.Assert(args.Length == ExpressionCount);
            Debug.Assert(variables == null || variables.Count == Variables.Count);

            return new ScopeN(ReuseOrValidateVariables(variables), args);
        }
    }

    internal sealed class ScopeWithType : ScopeN
    {
        internal ScopeWithType(IReadOnlyList<ParameterExpression> variables, IReadOnlyList<Expression> expressions, Type type)
            : base(variables, expressions)
        {
            Type = type;
        }

        public sealed override Type Type { get; }

        internal override BlockExpression Rewrite(ReadOnlyCollection<ParameterExpression> variables, Expression[] args)
        {
            if (args == null)
            {
                Debug.Assert(variables.Count == Variables.Count);
                ValidateVariables(variables, nameof(variables));
                return new ScopeWithType(variables, Body, Type);
            }
            Debug.Assert(args.Length == ExpressionCount);
            Debug.Assert(variables == null || variables.Count == Variables.Count);

            return new ScopeWithType(ReuseOrValidateVariables(variables), args, Type);
        }
    }

    #endregion

    #region Block List Classes

    /// <summary>
    /// Provides a wrapper around an IArgumentProvider which exposes the argument providers
    /// members out as an IList of Expression.  This is used to avoid allocating an array
    /// which needs to be stored inside of a ReadOnlyCollection.  Instead this type has
    /// the same amount of overhead as an array without duplicating the storage of the
    /// elements.  This ensures that internally we can avoid creating and copying arrays
    /// while users of the Expression trees also don't pay a size penalty for this internal
    /// optimization.  See IArgumentProvider for more general information on the Expression
    /// tree optimizations being used here.
    /// </summary>
    internal class BlockExpressionList : IList<Expression>
    {
        private readonly BlockExpression _block;
        private readonly Expression _arg0;

        internal BlockExpressionList(BlockExpression provider, Expression arg0)
        {
            _block = provider;
            _arg0 = arg0;
        }

        #region IList<Expression> Members

        public int IndexOf(Expression item)
        {
            if (_arg0 == item)
            {
                return 0;
            }

            for (int i = 1; i < _block.ExpressionCount; i++)
            {
                if (_block.GetExpression(i) == item)
                {
                    return i;
                }
            }

            return -1;
        }

        [ExcludeFromCodeCoverage] // Unreachable
        public void Insert(int index, Expression item)
        {
            throw ContractUtils.Unreachable;
        }

        [ExcludeFromCodeCoverage] // Unreachable
        public void RemoveAt(int index)
        {
            throw ContractUtils.Unreachable;
        }

        public Expression this[int index]
        {
            get
            {
                if (index == 0)
                {
                    return _arg0;
                }

                return _block.GetExpression(index);
            }
            [ExcludeFromCodeCoverage] // Unreachable
            set
            {
                throw ContractUtils.Unreachable;
            }
        }

        #endregion

        #region ICollection<Expression> Members

        [ExcludeFromCodeCoverage] // Unreachable
        public void Add(Expression item)
        {
            throw ContractUtils.Unreachable;
        }

        [ExcludeFromCodeCoverage] // Unreachable
        public void Clear()
        {
            throw ContractUtils.Unreachable;
        }

        public bool Contains(Expression item)
        {
            return IndexOf(item) != -1;
        }

        public void CopyTo(Expression[] array, int arrayIndex)
        {
            array[arrayIndex++] = _arg0;
            for (int i = 1; i < _block.ExpressionCount; i++)
            {
                array[arrayIndex++] = _block.GetExpression(i);
            }
        }

        public int Count => _block.ExpressionCount;

        [ExcludeFromCodeCoverage] // Unreachable
        public bool IsReadOnly
        {
            get
            {
                throw ContractUtils.Unreachable;
            }
        }

        [ExcludeFromCodeCoverage] // Unreachable
        public bool Remove(Expression item)
        {
            throw ContractUtils.Unreachable;
        }

        #endregion

        #region IEnumerable<Expression> Members

        public IEnumerator<Expression> GetEnumerator()
        {
            yield return _arg0;

            for (int i = 1; i < _block.ExpressionCount; i++)
            {
                yield return _block.GetExpression(i);
            }
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }

    #endregion

    public partial class Expression
    {
        /// <summary>
        /// Creates a <see cref="BlockExpression"/> that contains two expressions and has no variables.
        /// </summary>
        /// <param name="arg0">The first expression in the block.</param>
        /// <param name="arg1">The second expression in the block.</param>
        /// <returns>The created <see cref="BlockExpression"/>.</returns>
        public static BlockExpression Block(Expression arg0, Expression arg1)
        {
            RequiresCanRead(arg0, nameof(arg0));
            RequiresCanRead(arg1, nameof(arg1));

            return new Block2(arg0, arg1);
        }

        /// <summary>
        /// Creates a <see cref="BlockExpression"/> that contains three expressions and has no variables.
        /// </summary>
        /// <param name="arg0">The first expression in the block.</param>
        /// <param name="arg1">The second expression in the block.</param>
        /// <param name="arg2">The third expression in the block.</param>
        /// <returns>The created <see cref="BlockExpression"/>.</returns>
        public static BlockExpression Block(Expression arg0, Expression arg1, Expression arg2)
        {
            RequiresCanRead(arg0, nameof(arg0));
            RequiresCanRead(arg1, nameof(arg1));
            RequiresCanRead(arg2, nameof(arg2));
            return new Block3(arg0, arg1, arg2);
        }

        /// <summary>
        /// Creates a <see cref="BlockExpression"/> that contains four expressions and has no variables.
        /// </summary>
        /// <param name="arg0">The first expression in the block.</param>
        /// <param name="arg1">The second expression in the block.</param>
        /// <param name="arg2">The third expression in the block.</param>
        /// <param name="arg3">The fourth expression in the block.</param>
        /// <returns>The created <see cref="BlockExpression"/>.</returns>
        public static BlockExpression Block(Expression arg0, Expression arg1, Expression arg2, Expression arg3)
        {
            RequiresCanRead(arg0, nameof(arg0));
            RequiresCanRead(arg1, nameof(arg1));
            RequiresCanRead(arg2, nameof(arg2));
            RequiresCanRead(arg3, nameof(arg3));
            return new Block4(arg0, arg1, arg2, arg3);
        }

        /// <summary>
        /// Creates a <see cref="BlockExpression"/> that contains five expressions and has no variables.
        /// </summary>
        /// <param name="arg0">The first expression in the block.</param>
        /// <param name="arg1">The second expression in the block.</param>
        /// <param name="arg2">The third expression in the block.</param>
        /// <param name="arg3">The fourth expression in the block.</param>
        /// <param name="arg4">The fifth expression in the block.</param>
        /// <returns>The created <see cref="BlockExpression"/>.</returns>
        public static BlockExpression Block(Expression arg0, Expression arg1, Expression arg2, Expression arg3, Expression arg4)
        {
            RequiresCanRead(arg0, nameof(arg0));
            RequiresCanRead(arg1, nameof(arg1));
            RequiresCanRead(arg2, nameof(arg2));
            RequiresCanRead(arg3, nameof(arg3));
            RequiresCanRead(arg4, nameof(arg4));

            return new Block5(arg0, arg1, arg2, arg3, arg4);
        }

        /// <summary>
        /// Creates a <see cref="BlockExpression"/> that contains the given expressions and has no variables.
        /// </summary>
        /// <param name="expressions">The expressions in the block.</param>
        /// <returns>The created <see cref="BlockExpression"/>.</returns>
        public static BlockExpression Block(params Expression[] expressions)
        {
            ContractUtils.RequiresNotNull(expressions, nameof(expressions));
            RequiresCanRead(expressions, nameof(expressions));

            return GetOptimizedBlockExpression(expressions);
        }

        /// <summary>
        /// Creates a <see cref="BlockExpression"/> that contains the given expressions and has no variables.
        /// </summary>
        /// <param name="expressions">The expressions in the block.</param>
        /// <returns>The created <see cref="BlockExpression"/>.</returns>
        public static BlockExpression Block(IEnumerable<Expression> expressions)
        {
            return Block(EmptyReadOnlyCollection<ParameterExpression>.Instance, expressions);
        }

        /// <summary>
        /// Creates a <see cref="BlockExpression"/> that contains the given expressions, has no variables and has specific result type.
        /// </summary>
        /// <param name="type">The result type of the block.</param>
        /// <param name="expressions">The expressions in the block.</param>
        /// <returns>The created <see cref="BlockExpression"/>.</returns>
        public static BlockExpression Block(Type type, params Expression[] expressions)
        {
            ContractUtils.RequiresNotNull(expressions, nameof(expressions));
            return Block(type, (IEnumerable<Expression>)expressions);
        }

        /// <summary>
        /// Creates a <see cref="BlockExpression"/> that contains the given expressions, has no variables and has specific result type.
        /// </summary>
        /// <param name="type">The result type of the block.</param>
        /// <param name="expressions">The expressions in the block.</param>
        /// <returns>The created <see cref="BlockExpression"/>.</returns>
        public static BlockExpression Block(Type type, IEnumerable<Expression> expressions)
        {
            return Block(type, EmptyReadOnlyCollection<ParameterExpression>.Instance, expressions);
        }

        /// <summary>
        /// Creates a <see cref="BlockExpression"/> that contains the given variables and expressions.
        /// </summary>
        /// <param name="variables">The variables in the block.</param>
        /// <param name="expressions">The expressions in the block.</param>
        /// <returns>The created <see cref="BlockExpression"/>.</returns>
        public static BlockExpression Block(IEnumerable<ParameterExpression> variables, params Expression[] expressions)
        {
            return Block(variables, (IEnumerable<Expression>)expressions);
        }

        /// <summary>
        /// Creates a <see cref="BlockExpression"/> that contains the given variables and expressions.
        /// </summary>
        /// <param name="type">The result type of the block.</param>
        /// <param name="variables">The variables in the block.</param>
        /// <param name="expressions">The expressions in the block.</param>
        /// <returns>The created <see cref="BlockExpression"/>.</returns>
        public static BlockExpression Block(Type type, IEnumerable<ParameterExpression> variables, params Expression[] expressions)
        {
            return Block(type, variables, (IEnumerable<Expression>)expressions);
        }

        /// <summary>
        /// Creates a <see cref="BlockExpression"/> that contains the given variables and expressions.
        /// </summary>
        /// <param name="variables">The variables in the block.</param>
        /// <param name="expressions">The expressions in the block.</param>
        /// <returns>The created <see cref="BlockExpression"/>.</returns>
        public static BlockExpression Block(IEnumerable<ParameterExpression> variables, IEnumerable<Expression> expressions)
        {
            ContractUtils.RequiresNotNull(expressions, nameof(expressions));
            ReadOnlyCollection<ParameterExpression> variableList = variables.ToReadOnly();

            if (variableList.Count == 0)
            {
                IReadOnlyList<Expression> expressionList = expressions as IReadOnlyList<Expression> ?? expressions.ToReadOnly();
                RequiresCanRead(expressionList, nameof(expressions));

                return GetOptimizedBlockExpression(expressionList);
            }
            else
            {
                ReadOnlyCollection<Expression> expressionList = expressions.ToReadOnly();
                RequiresCanRead(expressionList, nameof(expressions));

                return BlockCore(null, variableList, expressionList);
            }
        }

        /// <summary>
        /// Creates a <see cref="BlockExpression"/> that contains the given variables and expressions.
        /// </summary>
        /// <param name="type">The result type of the block.</param>
        /// <param name="variables">The variables in the block.</param>
        /// <param name="expressions">The expressions in the block.</param>
        /// <returns>The created <see cref="BlockExpression"/>.</returns>
        public static BlockExpression Block(Type type, IEnumerable<ParameterExpression> variables, IEnumerable<Expression> expressions)
        {
            ContractUtils.RequiresNotNull(type, nameof(type));
            ContractUtils.RequiresNotNull(expressions, nameof(expressions));

            ReadOnlyCollection<Expression> expressionList = expressions.ToReadOnly();
            RequiresCanRead(expressionList, nameof(expressions));

            ReadOnlyCollection<ParameterExpression> variableList = variables.ToReadOnly();

            if (variableList.Count == 0 && expressionList.Count != 0)
            {
                int expressionCount = expressionList.Count;

                if (expressionCount != 0)
                {
                    Expression lastExpression = expressionList[expressionCount - 1];

                    if (lastExpression.Type == type)
                    {
                        return GetOptimizedBlockExpression(expressionList);
                    }
                }
            }

            return BlockCore(type, variableList, expressionList);
        }

        private static BlockExpression BlockCore(Type type, ReadOnlyCollection<ParameterExpression> variables, ReadOnlyCollection<Expression> expressions)
        {
            ValidateVariables(variables, nameof(variables));

            if (type != null)
            {
                if (expressions.Count == 0)
                {
                    if (type != typeof(void))
                    {
                        throw Error.ArgumentTypesMustMatch();
                    }

                    return new ScopeWithType(variables, expressions, type);
                }
                Expression last = expressions.Last();
                if (type != typeof(void))
                {
                    if (!TypeUtils.AreReferenceAssignable(type, last.Type))
                    {
                        throw Error.ArgumentTypesMustMatch();
                    }
                }

                if (!TypeUtils.AreEquivalent(type, last.Type))
                {
                    return new ScopeWithType(variables, expressions, type);
                }
            }

            switch (expressions.Count)
            {
                case 0:
                    return new ScopeWithType(variables, expressions, typeof(void));
                case 1:
                    return new Scope1(variables, expressions[0]);
                default:
                    return new ScopeN(variables, expressions);
            }
        }

        // Checks that all variables are non-null, not byref, and unique.
        internal static void ValidateVariables(ReadOnlyCollection<ParameterExpression> varList, string collectionName)
        {
            int count = varList.Count;
            if (count != 0)
            {
                var set = new HashSet<ParameterExpression>();
                for (int i = 0; i < count; i++)
                {
                    ParameterExpression v = varList[i];
                    ContractUtils.RequiresNotNull(v, collectionName, i);
                    if (v.IsByRef)
                    {
                        throw Error.VariableMustNotBeByRef(v, v.Type, collectionName, i);
                    }
                    if (!set.Add(v))
                    {
                        throw Error.DuplicateVariable(v, collectionName, i);
                    }
                }
            }
        }

        private static BlockExpression GetOptimizedBlockExpression(IReadOnlyList<Expression> expressions)
        {
            switch (expressions.Count)
            {
                case 0: return BlockCore(typeof(void), EmptyReadOnlyCollection<ParameterExpression>.Instance, EmptyReadOnlyCollection<Expression>.Instance);
                case 2: return new Block2(expressions[0], expressions[1]);
                case 3: return new Block3(expressions[0], expressions[1], expressions[2]);
                case 4: return new Block4(expressions[0], expressions[1], expressions[2], expressions[3]);
                case 5: return new Block5(expressions[0], expressions[1], expressions[2], expressions[3], expressions[4]);
                default:
                    return new BlockN(expressions as ReadOnlyCollection<Expression> ?? (IReadOnlyList<Expression>)expressions.ToArray());
            }
        }
    }
}
