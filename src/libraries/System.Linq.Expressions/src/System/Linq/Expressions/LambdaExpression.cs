// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Dynamic.Utils;
using System.Reflection;

namespace System.Linq.Expressions
{
    /// <summary>
    /// Creates a <see cref="LambdaExpression"/> node.
    /// This captures a block of code that is similar to a .NET method body.
    /// </summary>
    /// <remarks>
    /// Lambda expressions take input through parameters and are expected to be fully bound.
    /// </remarks>
    [DebuggerTypeProxy(typeof(LambdaExpressionProxy))]
    public abstract partial class LambdaExpression : Expression
    {
        private readonly string _name;
        private readonly Expression _body;
        private readonly ReadOnlyCollection<ParameterExpression> _parameters;
        private readonly Type _delegateType;
        private readonly bool _tailCall;

        internal LambdaExpression(
            Type delegateType,
            string name,
            Expression body,
            bool tailCall,
            ReadOnlyCollection<ParameterExpression> parameters
        )
        {
            Debug.Assert(delegateType != null);

            _name = name;
            _body = body;
            _parameters = parameters;
            _delegateType = delegateType;
            _tailCall = tailCall;
        }

        /// <summary>
        /// Gets the static type of the expression that this <see cref="Expression"/> represents. (Inherited from <see cref="Expression"/>.)
        /// </summary>
        /// <returns>The <see cref="System.Type"/> that represents the static type of the expression.</returns>
        public sealed override Type Type => _delegateType;

        /// <summary>
        /// Returns the node type of this <see cref="Expression"/>. (Inherited from <see cref="Expression"/>.)
        /// </summary>
        /// <returns>The <see cref="ExpressionType"/> that represents this expression.</returns>
        public sealed override ExpressionType NodeType => ExpressionType.Lambda;

        /// <summary>
        /// Gets the parameters of the lambda expression.
        /// </summary>
        public ReadOnlyCollection<ParameterExpression> Parameters => _parameters;

        /// <summary>
        /// Gets the name of the lambda expression.
        /// </summary>
        /// <remarks>Used for debugging purposes.</remarks>
        public string Name => _name;

        /// <summary>
        /// Gets the body of the lambda expression.
        /// </summary>
        public Expression Body => _body;

        /// <summary>
        /// Gets the return type of the lambda expression.
        /// </summary>
        public Type ReturnType => Type.GetMethod("Invoke").ReturnType;

        /// <summary>
        /// Gets the value that indicates if the lambda expression will be compiled with
        /// tail call optimization.
        /// </summary>
        public bool TailCall => _tailCall;

        /// <summary>
        /// Produces a delegate that represents the lambda expression.
        /// </summary>
        /// <returns>A delegate containing the compiled version of the lambda.</returns>
        public Delegate Compile()
        {
            return Compile(preferInterpretation: false);
        }

        /// <summary>
        /// Produces a delegate that represents the lambda expression.
        /// </summary>
        /// <param name="preferInterpretation">A <see cref="bool"/> that indicates if the expression should be compiled to an interpreted form, if available.</param>
        /// <returns>A delegate containing the compiled version of the lambda.</returns>
        public Delegate Compile(bool preferInterpretation)
        {
#if FEATURE_COMPILE
#if FEATURE_INTERPRET
            if (preferInterpretation)
            {
                return new Interpreter.LightCompiler().CompileTop(this).CreateDelegate();
            }
#endif
            return Compiler.LambdaCompiler.Compile(this);
#else
            return new Interpreter.LightCompiler().CompileTop(this).CreateDelegate();
#endif
        }

#if FEATURE_COMPILE_TO_METHODBUILDER
        /// <summary>
        /// Compiles the lambda into a method definition.
        /// </summary>
        /// <param name="method">A <see cref="Emit.MethodBuilder"/> which will be used to hold the lambda's IL.</param>
        public void CompileToMethod(System.Reflection.Emit.MethodBuilder method)
        {
            ContractUtils.RequiresNotNull(method, nameof(method));
            ContractUtils.Requires(method.IsStatic, nameof(method));
            var type = method.DeclaringType.GetTypeInfo() as System.Reflection.Emit.TypeBuilder;
            if (type == null) throw Error.MethodBuilderDoesNotHaveTypeBuilder();

            Compiler.LambdaCompiler.Compile(this, method);
        }
#endif


#if FEATURE_COMPILE
        internal abstract LambdaExpression Accept(Compiler.StackSpiller spiller);
#endif
    }

    /// <summary>
    /// Defines a <see cref="Expression{TDelegate}"/> node.
    /// This captures a block of code that is similar to a .NET method body.
    /// </summary>
    /// <typeparam name="TDelegate">The type of the delegate.</typeparam>
    /// <remarks>
    /// Lambda expressions take input through parameters and are expected to be fully bound.
    /// </remarks>
    public sealed partial class Expression<TDelegate> : LambdaExpression
    {
        internal Expression(Expression body, string name, bool tailCall, ReadOnlyCollection<ParameterExpression> parameters)
            : base(typeof(TDelegate), name, body, tailCall, parameters)
        {
        }

        /// <summary>
        /// Produces a delegate that represents the lambda expression.
        /// </summary>
        /// <returns>A delegate containing the compiled version of the lambda.</returns>
        public new TDelegate Compile()
        {
            return Compile(preferInterpretation: false);
        }

        /// <summary>
        /// Produces a delegate that represents the lambda expression.
        /// </summary>
        /// <param name="preferInterpretation">A <see cref="bool"/> that indicates if the expression should be compiled to an interpreted form, if available.</param>
        /// <returns>A delegate containing the compiled version of the lambda.</returns>
        public new TDelegate Compile(bool preferInterpretation)
        {
#if FEATURE_COMPILE
#if FEATURE_INTERPRET
            if (preferInterpretation)
            {
                return (TDelegate)(object)new Interpreter.LightCompiler().CompileTop(this).CreateDelegate();
            }
#endif
            return (TDelegate)(object)Compiler.LambdaCompiler.Compile(this);
#else
            return (TDelegate)(object)new Interpreter.LightCompiler().CompileTop(this).CreateDelegate();
#endif
        }

        /// <summary>
        /// Creates a new expression that is like this one, but using the
        /// supplied children. If all of the children are the same, it will
        /// return this expression.
        /// </summary>
        /// <param name="body">The <see cref="LambdaExpression.Body" /> property of the result.</param>
        /// <param name="parameters">The <see cref="LambdaExpression.Parameters" /> property of the result.</param>
        /// <returns>This expression if no children changed, or an expression with the updated children.</returns>
        public Expression<TDelegate> Update(Expression body, IEnumerable<ParameterExpression> parameters)
        {
            if (body == Body && parameters == Parameters)
            {
                return this;
            }
            return Expression.Lambda<TDelegate>(body, Name, TailCall, parameters);
        }

        /// <summary>
        /// Dispatches to the specific visit method for this node type.
        /// </summary>
        protected internal override Expression Accept(ExpressionVisitor visitor)
        {
            return visitor.VisitLambda(this);
        }

#if FEATURE_COMPILE
        internal override LambdaExpression Accept(Compiler.StackSpiller spiller)
        {
            return spiller.Rewrite(this);
        }

        internal static LambdaExpression Create(Expression body, string name, bool tailCall, ReadOnlyCollection<ParameterExpression> parameters)
        {
            return new Expression<TDelegate>(body, name, tailCall, parameters);
        }
#endif
    }

#if !FEATURE_COMPILE
    // Separate expression creation class to hide the CreateExpressionFunc function from users reflecting on Expression<T>
    public class ExpressionCreator<TDelegate>
    {
        public static LambdaExpression CreateExpressionFunc(Expression body, string name, bool tailCall, ReadOnlyCollection<ParameterExpression> parameters)
        {
            return new Expression<TDelegate>(body, name, tailCall, parameters);
        }
    }
#endif

    public partial class Expression
    {
        /// <summary>
        /// Creates an Expression{T} given the delegate type. Caches the
        /// factory method to speed up repeated creations for the same T.
        /// </summary>
        internal static LambdaExpression CreateLambda(Type delegateType, Expression body, string name, bool tailCall, ReadOnlyCollection<ParameterExpression> parameters)
        {
            // Get or create a delegate to the public Expression.Lambda<T>
            // method and call that will be used for creating instances of this
            // delegate type
            Func<Expression, string, bool, ReadOnlyCollection<ParameterExpression>, LambdaExpression> fastPath;
            CacheDict<Type, Func<Expression, string, bool, ReadOnlyCollection<ParameterExpression>, LambdaExpression>> factories = s_lambdaFactories;
            if (factories == null)
            {
                s_lambdaFactories = factories = new CacheDict<Type, Func<Expression, string, bool, ReadOnlyCollection<ParameterExpression>, LambdaExpression>>(50);
            }

            MethodInfo create = null;
            if (!factories.TryGetValue(delegateType, out fastPath))
            {
#if FEATURE_COMPILE
                create = typeof(Expression<>).MakeGenericType(delegateType).GetMethod("Create", BindingFlags.Static | BindingFlags.NonPublic);
#else
                create = typeof(ExpressionCreator<>).MakeGenericType(delegateType).GetMethod("CreateExpressionFunc", BindingFlags.Static | BindingFlags.Public);
#endif
                if (delegateType.CanCache())
                {
                    factories[delegateType] = fastPath = (Func<Expression, string, bool, ReadOnlyCollection<ParameterExpression>, LambdaExpression>)create.CreateDelegate(typeof(Func<Expression, string, bool, ReadOnlyCollection<ParameterExpression>, LambdaExpression>));
                }
            }

            if (fastPath != null)
            {
                return fastPath(body, name, tailCall, parameters);
            }

            Debug.Assert(create != null);
            return (LambdaExpression)create.Invoke(null, new object[] { body, name, tailCall, parameters });
        }

        /// <summary>
        /// Creates an <see cref="Expression{TDelegate}"/> where the delegate type is known at compile time.
        /// </summary>
        /// <typeparam name="TDelegate">The delegate type.</typeparam>
        /// <param name="body">An <see cref="Expression"/> to set the <see cref="LambdaExpression.Body"/> property equal to.</param>
        /// <param name="parameters">An array that contains <see cref="ParameterExpression"/> objects to use to populate the <see cref="LambdaExpression.Parameters"/> collection.</param>
        /// <returns>An <see cref="Expression{TDelegate}"/> that has the <see cref="NodeType"/> property equal to <see cref="ExpressionType.Lambda"/> and the <see cref="LambdaExpression.Body"/> and <see cref="LambdaExpression.Parameters"/> properties set to the specified values.</returns>
        public static Expression<TDelegate> Lambda<TDelegate>(Expression body, params ParameterExpression[] parameters)
        {
            return Lambda<TDelegate>(body, false, (IEnumerable<ParameterExpression>)parameters);
        }

        /// <summary>
        /// Creates an <see cref="Expression{TDelegate}"/> where the delegate type is known at compile time.
        /// </summary>
        /// <typeparam name="TDelegate">The delegate type.</typeparam>
        /// <param name="body">An <see cref="Expression"/> to set the <see cref="LambdaExpression.Body"/> property equal to.</param>
        /// <param name="tailCall">A <see cref="bool"/> that indicates if tail call optimization will be applied when compiling the created expression.</param>
        /// <param name="parameters">An array that contains <see cref="ParameterExpression"/> objects to use to populate the <see cref="LambdaExpression.Parameters"/> collection.</param>
        /// <returns>An <see cref="Expression{TDelegate}"/> that has the <see cref="NodeType"/> property equal to <see cref="ExpressionType.Lambda"/> and the <see cref="LambdaExpression.Body"/> and <see cref="LambdaExpression.Parameters"/> properties set to the specified values.</returns>
        public static Expression<TDelegate> Lambda<TDelegate>(Expression body, bool tailCall, params ParameterExpression[] parameters)
        {
            return Lambda<TDelegate>(body, tailCall, (IEnumerable<ParameterExpression>)parameters);
        }

        /// <summary>
        /// Creates an <see cref="Expression{TDelegate}"/> where the delegate type is known at compile time.
        /// </summary>
        /// <typeparam name="TDelegate">The delegate type.</typeparam>
        /// <param name="body">An <see cref="Expression"/> to set the <see cref="LambdaExpression.Body"/> property equal to.</param>
        /// <param name="parameters">An <see cref="IEnumerable{T}"/> that contains <see cref="ParameterExpression"/> objects to use to populate the <see cref="LambdaExpression.Parameters"/> collection.</param>
        /// <returns>An <see cref="Expression{TDelegate}"/> that has the <see cref="NodeType"/> property equal to <see cref="ExpressionType.Lambda"/> and the <see cref="LambdaExpression.Body"/> and <see cref="LambdaExpression.Parameters"/> properties set to the specified values.</returns>
        public static Expression<TDelegate> Lambda<TDelegate>(Expression body, IEnumerable<ParameterExpression> parameters)
        {
            return Lambda<TDelegate>(body, null, false, parameters);
        }

        /// <summary>
        /// Creates an <see cref="Expression{TDelegate}"/> where the delegate type is known at compile time.
        /// </summary>
        /// <typeparam name="TDelegate">The delegate type.</typeparam>
        /// <param name="body">An <see cref="Expression"/> to set the <see cref="LambdaExpression.Body"/> property equal to.</param>
        /// <param name="tailCall">A <see cref="bool"/> that indicates if tail call optimization will be applied when compiling the created expression.</param>
        /// <param name="parameters">An <see cref="IEnumerable{T}"/> that contains <see cref="ParameterExpression"/> objects to use to populate the <see cref="LambdaExpression.Parameters"/> collection.</param>
        /// <returns>An <see cref="Expression{TDelegate}"/> that has the <see cref="NodeType"/> property equal to <see cref="ExpressionType.Lambda"/> and the <see cref="LambdaExpression.Body"/> and <see cref="LambdaExpression.Parameters"/> properties set to the specified values.</returns>
        public static Expression<TDelegate> Lambda<TDelegate>(Expression body, bool tailCall, IEnumerable<ParameterExpression> parameters)
        {
            return Lambda<TDelegate>(body, null, tailCall, parameters);
        }

        /// <summary>
        /// Creates an <see cref="Expression{TDelegate}"/> where the delegate type is known at compile time.
        /// </summary>
        /// <typeparam name="TDelegate">The delegate type.</typeparam>
        /// <param name="body">An <see cref="Expression"/> to set the <see cref="LambdaExpression.Body"/> property equal to.</param>
        /// <param name="parameters">An <see cref="IEnumerable{T}"/> that contains <see cref="ParameterExpression"/> objects to use to populate the <see cref="LambdaExpression.Parameters"/> collection.</param>
        /// <param name="name">The name of the lambda. Used for generating debugging info.</param>
        /// <returns>An <see cref="Expression{TDelegate}"/> that has the <see cref="NodeType"/> property equal to <see cref="ExpressionType.Lambda"/> and the <see cref="LambdaExpression.Body"/> and <see cref="LambdaExpression.Parameters"/> properties set to the specified values.</returns>
        public static Expression<TDelegate> Lambda<TDelegate>(Expression body, string name, IEnumerable<ParameterExpression> parameters)
        {
            return Lambda<TDelegate>(body, name, false, parameters);
        }

        /// <summary>
        /// Creates an <see cref="Expression{TDelegate}"/> where the delegate type is known at compile time.
        /// </summary>
        /// <typeparam name="TDelegate">The delegate type.</typeparam>
        /// <param name="body">An <see cref="Expression"/> to set the <see cref="LambdaExpression.Body"/> property equal to.</param>
        /// <param name="name">The name of the lambda. Used for generating debugging info.</param>
        /// <param name="parameters">An <see cref="IEnumerable{T}"/> that contains <see cref="ParameterExpression"/> objects to use to populate the <see cref="LambdaExpression.Parameters"/> collection.</param>
        /// <param name="tailCall">A <see cref="bool"/> that indicates if tail call optimization will be applied when compiling the created expression.</param>
        /// <returns>An <see cref="Expression{TDelegate}"/> that has the <see cref="NodeType"/> property equal to <see cref="ExpressionType.Lambda"/> and the <see cref="LambdaExpression.Body"/> and <see cref="LambdaExpression.Parameters"/> properties set to the specified values.</returns>
        public static Expression<TDelegate> Lambda<TDelegate>(Expression body, string name, bool tailCall, IEnumerable<ParameterExpression> parameters)
        {
            ReadOnlyCollection<ParameterExpression> parameterList = parameters.ToReadOnly();
            ValidateLambdaArgs(typeof(TDelegate), ref body, parameterList, nameof(TDelegate));
            return new Expression<TDelegate>(body, name, tailCall, parameterList);
        }

        /// <summary>
        /// Creates a <see cref="LambdaExpression"/> by first constructing a delegate type.
        /// </summary>
        /// <param name="body">An <see cref="Expression"/> to set the <see cref="LambdaExpression.Body"/> property equal to.</param>
        /// <param name="parameters">An array that contains <see cref="ParameterExpression"/> objects to use to populate the <see cref="LambdaExpression.Parameters"/> collection.</param>
        /// <returns>A <see cref="LambdaExpression"/> that has the <see cref="NodeType"/> property equal to <see cref="ExpressionType.Lambda"/> and the <see cref="LambdaExpression.Body"/> and <see cref="LambdaExpression.Parameters"/> properties set to the specified values.</returns>
        public static LambdaExpression Lambda(Expression body, params ParameterExpression[] parameters)
        {
            return Lambda(body, false, (IEnumerable<ParameterExpression>)parameters);
        }

        /// <summary>
        /// Creates a <see cref="LambdaExpression"/> by first constructing a delegate type.
        /// </summary>
        /// <param name="body">An <see cref="Expression"/> to set the <see cref="LambdaExpression.Body"/> property equal to.</param>
        /// <param name="tailCall">A <see cref="bool"/> that indicates if tail call optimization will be applied when compiling the created expression.</param>
        /// <param name="parameters">An array that contains <see cref="ParameterExpression"/> objects to use to populate the <see cref="LambdaExpression.Parameters"/> collection.</param>
        /// <returns>A <see cref="LambdaExpression"/> that has the <see cref="NodeType"/> property equal to <see cref="ExpressionType.Lambda"/> and the <see cref="LambdaExpression.Body"/> and <see cref="LambdaExpression.Parameters"/> properties set to the specified values.</returns>
        public static LambdaExpression Lambda(Expression body, bool tailCall, params ParameterExpression[] parameters)
        {
            return Lambda(body, tailCall, (IEnumerable<ParameterExpression>)parameters);
        }

        /// <summary>
        /// Creates a <see cref="LambdaExpression"/> by first constructing a delegate type.
        /// </summary>
        /// <param name="body">An <see cref="Expression"/> to set the <see cref="LambdaExpression.Body"/> property equal to.</param>
        /// <param name="parameters">An <see cref="IEnumerable{T}"/> that contains <see cref="ParameterExpression"/> objects to use to populate the <see cref="LambdaExpression.Parameters"/> collection.</param>
        /// <returns>A <see cref="LambdaExpression"/> that has the <see cref="NodeType"/> property equal to <see cref="ExpressionType.Lambda"/> and the <see cref="LambdaExpression.Body"/> and <see cref="LambdaExpression.Parameters"/> properties set to the specified values.</returns>
        public static LambdaExpression Lambda(Expression body, IEnumerable<ParameterExpression> parameters)
        {
            return Lambda(body, null, false, parameters);
        }

        /// <summary>
        /// Creates a <see cref="LambdaExpression"/> by first constructing a delegate type.
        /// </summary>
        /// <param name="body">An <see cref="Expression"/> to set the <see cref="LambdaExpression.Body"/> property equal to.</param>
        /// <param name="tailCall">A <see cref="bool"/> that indicates if tail call optimization will be applied when compiling the created expression.</param>
        /// <param name="parameters">An <see cref="IEnumerable{T}"/> that contains <see cref="ParameterExpression"/> objects to use to populate the <see cref="LambdaExpression.Parameters"/> collection.</param>
        /// <returns>A <see cref="LambdaExpression"/> that has the <see cref="NodeType"/> property equal to <see cref="ExpressionType.Lambda"/> and the <see cref="LambdaExpression.Body"/> and <see cref="LambdaExpression.Parameters"/> properties set to the specified values.</returns>
        public static LambdaExpression Lambda(Expression body, bool tailCall, IEnumerable<ParameterExpression> parameters)
        {
            return Lambda(body, null, tailCall, parameters);
        }

        /// <summary>
        /// Creates a <see cref="LambdaExpression"/> by first constructing a delegate type.
        /// </summary>
        /// <param name="body">An <see cref="Expression"/> to set the <see cref="LambdaExpression.Body"/> property equal to.</param>
        /// <param name="parameters">An array that contains <see cref="ParameterExpression"/> objects to use to populate the <see cref="LambdaExpression.Parameters"/> collection.</param>
        /// <param name="delegateType">A <see cref="Type"/> representing the delegate signature for the lambda.</param>
        /// <returns>A <see cref="LambdaExpression"/> that has the <see cref="NodeType"/> property equal to <see cref="ExpressionType.Lambda"/> and the <see cref="LambdaExpression.Body"/> and <see cref="LambdaExpression.Parameters"/> properties set to the specified values.</returns>
        public static LambdaExpression Lambda(Type delegateType, Expression body, params ParameterExpression[] parameters)
        {
            return Lambda(delegateType, body, null, false, parameters);
        }

        /// <summary>
        /// Creates a <see cref="LambdaExpression"/> by first constructing a delegate type.
        /// </summary>
        /// <param name="body">An <see cref="Expression"/> to set the <see cref="LambdaExpression.Body"/> property equal to.</param>
        /// <param name="tailCall">A <see cref="bool"/> that indicates if tail call optimization will be applied when compiling the created expression.</param>
        /// <param name="parameters">An array that contains <see cref="ParameterExpression"/> objects to use to populate the <see cref="LambdaExpression.Parameters"/> collection.</param>
        /// <param name="delegateType">A <see cref="Type"/> representing the delegate signature for the lambda.</param>
        /// <returns>A <see cref="LambdaExpression"/> that has the <see cref="NodeType"/> property equal to <see cref="ExpressionType.Lambda"/> and the <see cref="LambdaExpression.Body"/> and <see cref="LambdaExpression.Parameters"/> properties set to the specified values.</returns>
        public static LambdaExpression Lambda(Type delegateType, Expression body, bool tailCall, params ParameterExpression[] parameters)
        {
            return Lambda(delegateType, body, null, tailCall, parameters);
        }

        /// <summary>
        /// Creates a <see cref="LambdaExpression"/> by first constructing a delegate type.
        /// </summary>
        /// <param name="body">An <see cref="Expression"/> to set the <see cref="LambdaExpression.Body"/> property equal to.</param>
        /// <param name="parameters">An <see cref="IEnumerable{T}"/> that contains <see cref="ParameterExpression"/> objects to use to populate the <see cref="LambdaExpression.Parameters"/> collection.</param>
        /// <param name="delegateType">A <see cref="Type"/> representing the delegate signature for the lambda.</param>
        /// <returns>A <see cref="LambdaExpression"/> that has the <see cref="NodeType"/> property equal to <see cref="ExpressionType.Lambda"/> and the <see cref="LambdaExpression.Body"/> and <see cref="LambdaExpression.Parameters"/> properties set to the specified values.</returns>
        public static LambdaExpression Lambda(Type delegateType, Expression body, IEnumerable<ParameterExpression> parameters)
        {
            return Lambda(delegateType, body, null, false, parameters);
        }

        /// <summary>
        /// Creates a <see cref="LambdaExpression"/> by first constructing a delegate type.
        /// </summary>
        /// <param name="body">An <see cref="Expression"/> to set the <see cref="LambdaExpression.Body"/> property equal to.</param>
        /// <param name="tailCall">A <see cref="bool"/> that indicates if tail call optimization will be applied when compiling the created expression.</param>
        /// <param name="parameters">An <see cref="IEnumerable{T}"/> that contains <see cref="ParameterExpression"/> objects to use to populate the <see cref="LambdaExpression.Parameters"/> collection.</param>
        /// <param name="delegateType">A <see cref="Type"/> representing the delegate signature for the lambda.</param>
        /// <returns>A <see cref="LambdaExpression"/> that has the <see cref="NodeType"/> property equal to <see cref="ExpressionType.Lambda"/> and the <see cref="LambdaExpression.Body"/> and <see cref="LambdaExpression.Parameters"/> properties set to the specified values.</returns>
        public static LambdaExpression Lambda(Type delegateType, Expression body, bool tailCall, IEnumerable<ParameterExpression> parameters)
        {
            return Lambda(delegateType, body, null, tailCall, parameters);
        }

        /// <summary>
        /// Creates a <see cref="LambdaExpression"/> by first constructing a delegate type.
        /// </summary>
        /// <param name="body">An <see cref="Expression"/> to set the <see cref="LambdaExpression.Body"/> property equal to.</param>
        /// <param name="parameters">An <see cref="IEnumerable{T}"/> that contains <see cref="ParameterExpression"/> objects to use to populate the <see cref="LambdaExpression.Parameters"/> collection.</param>
        /// <param name="name">The name for the lambda. Used for emitting debug information.</param>
        /// <returns>A <see cref="LambdaExpression"/> that has the <see cref="NodeType"/> property equal to <see cref="ExpressionType.Lambda"/> and the <see cref="LambdaExpression.Body"/> and <see cref="LambdaExpression.Parameters"/> properties set to the specified values.</returns>
        public static LambdaExpression Lambda(Expression body, string name, IEnumerable<ParameterExpression> parameters)
        {
            return Lambda(body, name, false, parameters);
        }

        /// <summary>
        /// Creates a <see cref="LambdaExpression"/> by first constructing a delegate type.
        /// </summary>
        /// <param name="body">An <see cref="Expression"/> to set the <see cref="LambdaExpression.Body"/> property equal to.</param>
        /// <param name="name">The name for the lambda. Used for emitting debug information.</param>
        /// <param name="tailCall">A <see cref="bool"/> that indicates if tail call optimization will be applied when compiling the created expression.</param>
        /// <param name="parameters">An <see cref="IEnumerable{T}"/> that contains <see cref="ParameterExpression"/> objects to use to populate the <see cref="LambdaExpression.Parameters"/> collection.</param>
        /// <returns>A <see cref="LambdaExpression"/> that has the <see cref="NodeType"/> property equal to <see cref="ExpressionType.Lambda"/> and the <see cref="LambdaExpression.Body"/> and <see cref="LambdaExpression.Parameters"/> properties set to the specified values.</returns>
        public static LambdaExpression Lambda(Expression body, string name, bool tailCall, IEnumerable<ParameterExpression> parameters)
        {
            ContractUtils.RequiresNotNull(body, nameof(body));

            ReadOnlyCollection<ParameterExpression> parameterList = parameters.ToReadOnly();

            int paramCount = parameterList.Count;
            Type[] typeArgs = new Type[paramCount + 1];
            if (paramCount > 0)
            {
                var set = new HashSet<ParameterExpression>();
                for (int i = 0; i < paramCount; i++)
                {
                    ParameterExpression param = parameterList[i];
                    ContractUtils.RequiresNotNull(param, "parameter");
                    typeArgs[i] = param.IsByRef ? param.Type.MakeByRefType() : param.Type;
                    if (!set.Add(param))
                    {
                        throw Error.DuplicateVariable(param, nameof(parameters), i);
                    }
                }
            }
            typeArgs[paramCount] = body.Type;

            Type delegateType = Compiler.DelegateHelpers.MakeDelegateType(typeArgs);

            return CreateLambda(delegateType, body, name, tailCall, parameterList);
        }

        /// <summary>
        /// Creates a <see cref="LambdaExpression"/> by first constructing a delegate type.
        /// </summary>
        /// <param name="body">An <see cref="Expression"/> to set the <see cref="LambdaExpression.Body"/> property equal to.</param>
        /// <param name="parameters">An <see cref="IEnumerable{T}"/> that contains <see cref="ParameterExpression"/> objects to use to populate the <see cref="LambdaExpression.Parameters"/> collection.</param>
        /// <param name="name">The name for the lambda. Used for emitting debug information.</param>
        /// <param name="delegateType">A <see cref="Type"/> representing the delegate signature for the lambda.</param>
        /// <returns>A <see cref="LambdaExpression"/> that has the <see cref="NodeType"/> property equal to <see cref="ExpressionType.Lambda"/> and the <see cref="LambdaExpression.Body"/> and <see cref="LambdaExpression.Parameters"/> properties set to the specified values.</returns>
        public static LambdaExpression Lambda(Type delegateType, Expression body, string name, IEnumerable<ParameterExpression> parameters)
        {
            ReadOnlyCollection<ParameterExpression> paramList = parameters.ToReadOnly();
            ValidateLambdaArgs(delegateType, ref body, paramList, nameof(delegateType));

            return CreateLambda(delegateType, body, name, false, paramList);
        }

        /// <summary>
        /// Creates a <see cref="LambdaExpression"/> by first constructing a delegate type.
        /// </summary>
        /// <param name="delegateType">A <see cref="Type"/> representing the delegate signature for the lambda.</param>
        /// <param name="body">An <see cref="Expression"/> to set the <see cref="LambdaExpression.Body"/> property equal to.</param>
        /// <param name="name">The name for the lambda. Used for emitting debug information.</param>
        /// <param name="tailCall">A <see cref="bool"/> that indicates if tail call optimization will be applied when compiling the created expression.</param>
        /// <param name="parameters">An <see cref="IEnumerable{T}"/> that contains <see cref="ParameterExpression"/> objects to use to populate the <see cref="LambdaExpression.Parameters"/> collection.</param>
        /// <returns>A <see cref="LambdaExpression"/> that has the <see cref="NodeType"/> property equal to <see cref="ExpressionType.Lambda"/> and the <see cref="LambdaExpression.Body"/> and <see cref="LambdaExpression.Parameters"/> properties set to the specified values.</returns>
        public static LambdaExpression Lambda(Type delegateType, Expression body, string name, bool tailCall, IEnumerable<ParameterExpression> parameters)
        {
            ReadOnlyCollection<ParameterExpression> paramList = parameters.ToReadOnly();
            ValidateLambdaArgs(delegateType, ref body, paramList, nameof(delegateType));

            return CreateLambda(delegateType, body, name, tailCall, paramList);
        }

        private static void ValidateLambdaArgs(Type delegateType, ref Expression body, ReadOnlyCollection<ParameterExpression> parameters, string paramName)
        {
            ContractUtils.RequiresNotNull(delegateType, nameof(delegateType));
            RequiresCanRead(body, nameof(body));

            if (!typeof(MulticastDelegate).IsAssignableFrom(delegateType) || delegateType == typeof(MulticastDelegate))
            {
                throw Error.LambdaTypeMustBeDerivedFromSystemDelegate(paramName);
            }

            MethodInfo mi;
            CacheDict<Type, MethodInfo> ldc = s_lambdaDelegateCache;
            if (!ldc.TryGetValue(delegateType, out mi))
            {
                mi = delegateType.GetMethod("Invoke");
                if (delegateType.CanCache())
                {
                    ldc[delegateType] = mi;
                }
            }

            ParameterInfo[] pis = mi.GetParametersCached();

            if (pis.Length > 0)
            {
                if (pis.Length != parameters.Count)
                {
                    throw Error.IncorrectNumberOfLambdaDeclarationParameters();
                }
                var set = new HashSet<ParameterExpression>();
                for (int i = 0, n = pis.Length; i < n; i++)
                {
                    ParameterExpression pex = parameters[i];
                    ParameterInfo pi = pis[i];
                    RequiresCanRead(pex, nameof(parameters), i);
                    Type pType = pi.ParameterType;
                    if (pex.IsByRef)
                    {
                        if (!pType.IsByRef)
                        {
                            //We cannot pass a parameter of T& to a delegate that takes T or any non-ByRef type.
                            throw Error.ParameterExpressionNotValidAsDelegate(pex.Type.MakeByRefType(), pType);
                        }
                        pType = pType.GetElementType();
                    }
                    if (!TypeUtils.AreReferenceAssignable(pex.Type, pType))
                    {
                        throw Error.ParameterExpressionNotValidAsDelegate(pex.Type, pType);
                    }
                    if (!set.Add(pex))
                    {
                        throw Error.DuplicateVariable(pex, nameof(parameters), i);
                    }
                }
            }
            else if (parameters.Count > 0)
            {
                throw Error.IncorrectNumberOfLambdaDeclarationParameters();
            }
            if (mi.ReturnType != typeof(void) && !TypeUtils.AreReferenceAssignable(mi.ReturnType, body.Type))
            {
                if (!TryQuote(mi.ReturnType, ref body))
                {
                    throw Error.ExpressionTypeDoesNotMatchReturn(body.Type, mi.ReturnType);
                }
            }
        }

        private enum TryGetFuncActionArgsResult
        {
            Valid,
            ArgumentNull,
            ByRef,
            PointerOrVoid
        }

        private static TryGetFuncActionArgsResult ValidateTryGetFuncActionArgs(Type[] typeArgs)
        {
            if (typeArgs == null)
            {
                return TryGetFuncActionArgsResult.ArgumentNull;
            }

            for (int i = 0; i < typeArgs.Length; i++)
            {
                Type a = typeArgs[i];
                if (a == null)
                {
                    return TryGetFuncActionArgsResult.ArgumentNull;
                }

                if (a.IsByRef)
                {
                    return TryGetFuncActionArgsResult.ByRef;
                }

                if (a == typeof(void) || a.IsPointer)
                {
                    return TryGetFuncActionArgsResult.PointerOrVoid;
                }
            }

            return TryGetFuncActionArgsResult.Valid;
        }

        /// <summary>
        /// Creates a <see cref="System.Type"/> object that represents a generic System.Func delegate type that has specific type arguments.
        /// The last type argument specifies the return type of the created delegate.
        /// </summary>
        /// <param name="typeArgs">An array of <see cref="System.Type"/> objects that specify the type arguments for the System.Func delegate type.</param>
        /// <returns>The type of a System.Func delegate that has the specified type arguments.</returns>
        public static Type GetFuncType(params Type[] typeArgs)
        {
            switch (ValidateTryGetFuncActionArgs(typeArgs))
            {
                case TryGetFuncActionArgsResult.ArgumentNull:
                    throw new ArgumentNullException(nameof(typeArgs));
                case TryGetFuncActionArgsResult.ByRef:
                    throw Error.TypeMustNotBeByRef(nameof(typeArgs));
                default:

                    // This includes pointers or void. We allow the exception that comes
                    // from trying to use them as generic arguments to pass through.
                    Type result = Compiler.DelegateHelpers.GetFuncType(typeArgs);
                    if (result == null)
                    {
                        throw Error.IncorrectNumberOfTypeArgsForFunc(nameof(typeArgs));
                    }

                    return result;
            }
        }

        /// <summary>
        /// Creates a <see cref="System.Type"/> object that represents a generic System.Func delegate type that has specific type arguments.
        /// The last type argument specifies the return type of the created delegate.
        /// </summary>
        /// <param name="typeArgs">An array of <see cref="System.Type"/> objects that specify the type arguments for the System.Func delegate type.</param>
        /// <param name="funcType">When this method returns, contains the generic System.Func delegate type that has specific type arguments. Contains null if there is no generic System.Func delegate that matches the <paramref name="typeArgs"/>.This parameter is passed uninitialized.</param>
        /// <returns>true if generic System.Func delegate type was created for specific <paramref name="typeArgs"/>; false otherwise.</returns>
        public static bool TryGetFuncType(Type[] typeArgs, out Type funcType)
        {
            if (ValidateTryGetFuncActionArgs(typeArgs) == TryGetFuncActionArgsResult.Valid)
            {
                return (funcType = Compiler.DelegateHelpers.GetFuncType(typeArgs)) != null;
            }

            funcType = null;
            return false;
        }

        /// <summary>
        /// Creates a <see cref="System.Type"/> object that represents a generic System.Action delegate type that has specific type arguments.
        /// </summary>
        /// <param name="typeArgs">An array of <see cref="System.Type"/> objects that specify the type arguments for the System.Action delegate type.</param>
        /// <returns>The type of a System.Action delegate that has the specified type arguments.</returns>
        public static Type GetActionType(params Type[] typeArgs)
        {
            switch (ValidateTryGetFuncActionArgs(typeArgs))
            {
                case TryGetFuncActionArgsResult.ArgumentNull:
                    throw new ArgumentNullException(nameof(typeArgs));
                case TryGetFuncActionArgsResult.ByRef:
                    throw Error.TypeMustNotBeByRef(nameof(typeArgs));
                default:

                    // This includes pointers or void. We allow the exception that comes
                    // from trying to use them as generic arguments to pass through.
                    Type result = Compiler.DelegateHelpers.GetActionType(typeArgs);
                    if (result == null)
                    {
                        throw Error.IncorrectNumberOfTypeArgsForAction(nameof(typeArgs));
                    }

                    return result;
            }
        }

        /// <summary>
        /// Creates a <see cref="System.Type"/> object that represents a generic System.Action delegate type that has specific type arguments.
        /// </summary>
        /// <param name="typeArgs">An array of <see cref="System.Type"/> objects that specify the type arguments for the System.Action delegate type.</param>
        /// <param name="actionType">When this method returns, contains the generic System.Action delegate type that has specific type arguments. Contains null if there is no generic System.Action delegate that matches the <paramref name="typeArgs"/>.This parameter is passed uninitialized.</param>
        /// <returns>true if generic System.Action delegate type was created for specific <paramref name="typeArgs"/>; false otherwise.</returns>
        public static bool TryGetActionType(Type[] typeArgs, out Type actionType)
        {
            if (ValidateTryGetFuncActionArgs(typeArgs) == TryGetFuncActionArgsResult.Valid)
            {
                return (actionType = Compiler.DelegateHelpers.GetActionType(typeArgs)) != null;
            }

            actionType = null;
            return false;
        }

        /// <summary>
        /// Gets a <see cref="System.Type"/> object that represents a generic System.Func or System.Action delegate type that has specific type arguments.
        /// The last type argument determines the return type of the delegate. If no Func or Action is large enough, it will generate a custom
        /// delegate type.
        /// </summary>
        /// <param name="typeArgs">An array of <see cref="System.Type"/> objects that specify the type arguments of the delegate type.</param>
        /// <returns>The delegate type.</returns>
        /// <remarks>
        /// As with Func, the last argument is the return type. It can be set
        /// to <see cref="System.Void"/> to produce an Action.</remarks>
        public static Type GetDelegateType(params Type[] typeArgs)
        {
            ContractUtils.RequiresNotEmpty(typeArgs, nameof(typeArgs));
            ContractUtils.RequiresNotNullItems(typeArgs, nameof(typeArgs));
            return Compiler.DelegateHelpers.MakeDelegateType(typeArgs);
        }
    }
}
