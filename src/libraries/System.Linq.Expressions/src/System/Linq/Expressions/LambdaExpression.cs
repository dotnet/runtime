// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
    /// Creates a <see cref="LambdaExpression"/> node.
    /// This captures a block of code that is similar to a .NET method body.
    /// </summary>
    /// <remarks>
    /// Lambda expressions take input through parameters and are expected to be fully bound.
    /// </remarks>
    [DebuggerTypeProxy(typeof(LambdaExpressionProxy))]
    public abstract class LambdaExpression : Expression, IParameterProvider
    {
        private static readonly MethodInfo s_expressionCompileMethodInfo = typeof(Expression<>).GetMethod("Compile", Type.EmptyTypes)!;

        private readonly Expression _body;

        // This can be flipped to false using feature switches at publishing time
        // Relies on IsDynamicCodeSupported substitution.
        [FeatureCheck(typeof(RequiresDynamicCodeAttribute))]
        // TODO: this doesn't respect the feature if set in appcontext!!! :(
        public static bool CanCompileToIL => RuntimeFeature.IsDynamicCodeSupported;

        // This could be flipped to false using feature switches at publishing time
        public static bool CanInterpret => true;

        internal LambdaExpression(Expression body)
        {
            _body = body;
        }

        /// <summary>
        /// Gets the static type of the expression that this <see cref="Expression"/> represents. (Inherited from <see cref="Expression"/>.)
        /// </summary>
        /// <returns>The <see cref="Type"/> that represents the static type of the expression.</returns>
        public sealed override Type Type => TypeCore;

        internal abstract Type TypeCore { get; }

        internal abstract Type PublicType { get; }

        /// <summary>
        /// Returns the node type of this <see cref="Expression"/>. (Inherited from <see cref="Expression"/>.)
        /// </summary>
        /// <returns>The <see cref="ExpressionType"/> that represents this expression.</returns>
        public sealed override ExpressionType NodeType => ExpressionType.Lambda;

        /// <summary>
        /// Gets the parameters of the lambda expression.
        /// </summary>
        public ReadOnlyCollection<ParameterExpression> Parameters => GetOrMakeParameters();

        /// <summary>
        /// Gets the name of the lambda expression.
        /// </summary>
        /// <remarks>Used for debugging purposes.</remarks>
        public string? Name => NameCore;

        internal virtual string? NameCore => null;

        /// <summary>
        /// Gets the body of the lambda expression.
        /// </summary>
        public Expression Body => _body;

        /// <summary>
        /// Gets the return type of the lambda expression.
        /// </summary>
        public Type ReturnType => Type.GetInvokeMethod().ReturnType;

        /// <summary>
        /// Gets the value that indicates if the lambda expression will be compiled with
        /// tail call optimization.
        /// </summary>
        public bool TailCall => TailCallCore;

        internal virtual bool TailCallCore => false;

        [ExcludeFromCodeCoverage(Justification = "Unreachable")]
        internal virtual ReadOnlyCollection<ParameterExpression> GetOrMakeParameters()
        {
            throw ContractUtils.Unreachable;
        }

        [ExcludeFromCodeCoverage(Justification = "Unreachable")]
        ParameterExpression IParameterProvider.GetParameter(int index) => GetParameter(index);

        [ExcludeFromCodeCoverage(Justification = "Unreachable")]
        internal virtual ParameterExpression GetParameter(int index)
        {
            throw ContractUtils.Unreachable;
        }

        [ExcludeFromCodeCoverage(Justification = "Unreachable")]
        int IParameterProvider.ParameterCount => ParameterCount;

        [ExcludeFromCodeCoverage(Justification = "Unreachable")]
        internal virtual int ParameterCount
        {
            get
            {
                throw ContractUtils.Unreachable;
            }
        }

        /// <summary>
        /// Gets the Compile() MethodInfo on the specified LambdaExpression type.
        /// </summary>
        /// <remarks>
        /// Note that Expression{TDelegate} defines a 'new' Compile() method that hides the base
        /// LambdaExpression.Compile() method.
        /// </remarks>
        internal static MethodInfo GetCompileMethod(Type lambdaExpressionType)
        {
            Debug.Assert(lambdaExpressionType.IsAssignableTo(typeof(LambdaExpression)));

            if (lambdaExpressionType == typeof(LambdaExpression))
            {
                // use a hard-coded type directly so the method doesn't get trimmed
                return typeof(LambdaExpression).GetMethod("Compile", Type.EmptyTypes)!;
            }

            return (MethodInfo)lambdaExpressionType.GetMemberWithSameMetadataDefinitionAs(s_expressionCompileMethodInfo);
        }

        /// <summary>
        /// Produces a delegate that represents the lambda expression.
        /// </summary>
        /// <returns>A delegate containing the compiled version of the lambda.</returns>
        public Delegate Compile()
        {
            if (CanCompileToIL)
            {
#pragma warning disable IL3050
                // Analyzer doesn't yet understand feature switches
                return Compiler.LambdaCompiler.Compile(this);
#pragma warning restore IL3050
            }
            else
            {
                Debug.Assert(CanInterpret);
                return new Interpreter.LightCompiler().CompileTop(this).CreateDelegate();
            }
        }

        /// <summary>
        /// Produces a delegate that represents the lambda expression.
        /// </summary>
        /// <param name="preferInterpretation">A <see cref="bool"/> that indicates if the expression should be compiled to an interpreted form, if available.</param>
        /// <returns>A delegate containing the compiled version of the lambda.</returns>
        public Delegate Compile(bool preferInterpretation)
        {
            if (CanInterpret && preferInterpretation)
            {
                return new Interpreter.LightCompiler().CompileTop(this).CreateDelegate();
            }

            return Compile();
        }

#if FEATURE_COMPILE_TO_METHODBUILDER
        /// <summary>
        /// Compiles the lambda into a method definition.
        /// </summary>
        /// <param name="method">A <see cref="Emit.MethodBuilder"/> which will be used to hold the lambda's IL.</param>
        public void CompileToMethod(System.Reflection.Emit.MethodBuilder method)
        {
            ArgumentNullException.ThrowIfNull(method);
            ContractUtils.Requires(method.IsStatic, nameof(method));
            var type = method.DeclaringType as System.Reflection.Emit.TypeBuilder;
            if (type == null) throw Error.MethodBuilderDoesNotHaveTypeBuilder();

            Compiler.LambdaCompiler.Compile(this, method);
        }
#endif

        internal abstract LambdaExpression Accept(Compiler.StackSpiller spiller);

        /// <summary>
        /// Produces a delegate that represents the lambda expression.
        /// </summary>
        /// <param name="debugInfoGenerator">Debugging information generator used by the compiler to mark sequence points and annotate local variables.</param>
        /// <returns>A delegate containing the compiled version of the lambda.</returns>
        public Delegate Compile(DebugInfoGenerator debugInfoGenerator)
        {
            return Compile();
        }
    }

    /// <summary>
    /// Defines a <see cref="Expression{TDelegate}"/> node.
    /// This captures a block of code that is similar to a .NET method body.
    /// </summary>
    /// <typeparam name="TDelegate">The type of the delegate.</typeparam>
    /// <remarks>
    /// Lambda expressions take input through parameters and are expected to be fully bound.
    /// </remarks>
    public class Expression<TDelegate> : LambdaExpression
    {
        internal Expression(Expression body)
            : base(body)
        {
        }

        internal sealed override Type TypeCore => typeof(TDelegate);

        internal override Type PublicType => typeof(Expression<TDelegate>);

        /// <summary>
        /// Produces a delegate that represents the lambda expression.
        /// </summary>
        /// <returns>A delegate containing the compiled version of the lambda.</returns>
        public new TDelegate Compile()
        {
            if (CanCompileToIL)
            {
#pragma warning disable IL3050
                // Analyzer doesn't yet understand feature switches
                return (TDelegate)(object)Compiler.LambdaCompiler.Compile(this);
#pragma warning restore IL3050
            }
            else
            {
                Debug.Assert(CanInterpret);
                return (TDelegate)(object)new Interpreter.LightCompiler().CompileTop(this).CreateDelegate();
            }
        }

        /// <summary>
        /// Produces a delegate that represents the lambda expression.
        /// </summary>
        /// <param name="preferInterpretation">A <see cref="bool"/> that indicates if the expression should be compiled to an interpreted form, if available.</param>
        /// <returns>A delegate containing the compiled version of the lambda.</returns>
        public new TDelegate Compile(bool preferInterpretation)
        {
            if (CanInterpret && preferInterpretation)
            {
                return (TDelegate)(object)new Interpreter.LightCompiler().CompileTop(this).CreateDelegate();
            }

            return Compile();
        }

        /// <summary>
        /// Creates a new expression that is like this one, but using the
        /// supplied children. If all of the children are the same, it will
        /// return this expression.
        /// </summary>
        /// <param name="body">The <see cref="LambdaExpression.Body" /> property of the result.</param>
        /// <param name="parameters">The <see cref="LambdaExpression.Parameters" /> property of the result.</param>
        /// <returns>This expression if no children changed, or an expression with the updated children.</returns>
        public Expression<TDelegate> Update(Expression body, IEnumerable<ParameterExpression>? parameters)
        {
            if (body == Body)
            {
                // Ensure parameters is safe to enumerate twice.
                // (If this means a second call to ToReadOnly it will return quickly).
                ICollection<ParameterExpression>? pars;
                if (parameters == null)
                {
                    pars = null;
                }
                else
                {
                    pars = parameters as ICollection<ParameterExpression>;
                    if (pars == null)
                    {
                        parameters = pars = parameters.ToReadOnly();
                    }
                }

                if (SameParameters(pars))
                {
                    return this;
                }
            }

            return Lambda<TDelegate>(body, Name, TailCall, parameters);
        }

        [ExcludeFromCodeCoverage(Justification = "Unreachable")]
        internal virtual bool SameParameters(ICollection<ParameterExpression>? parameters)
        {
            throw ContractUtils.Unreachable;
        }

        [ExcludeFromCodeCoverage(Justification = "Unreachable")]
        internal virtual Expression<TDelegate> Rewrite(Expression body, ParameterExpression[]? parameters)
        {
            throw ContractUtils.Unreachable;
        }

        /// <summary>
        /// Dispatches to the specific visit method for this node type.
        /// </summary>
        protected internal override Expression Accept(ExpressionVisitor visitor)
        {
            return visitor.VisitLambda(this);
        }

        internal override LambdaExpression Accept(Compiler.StackSpiller spiller)
        {
            return spiller.Rewrite(this);
        }

        internal static Expression<TDelegate> Create(Expression body, string? name, bool tailCall, IReadOnlyList<ParameterExpression> parameters)
        {
            if (name == null && !tailCall)
            {
                return parameters.Count switch
                {
                    0 => new Expression0<TDelegate>(body),
                    1 => new Expression1<TDelegate>(body, parameters[0]),
                    2 => new Expression2<TDelegate>(body, parameters[0], parameters[1]),
                    3 => new Expression3<TDelegate>(body, parameters[0], parameters[1], parameters[2]),
                    _ => new ExpressionN<TDelegate>(body, parameters),
                };
            }

            return new FullExpression<TDelegate>(body, name, tailCall, parameters);
        }

        /// <summary>
        /// Produces a delegate that represents the lambda expression.
        /// </summary>
        /// <param name="debugInfoGenerator">Debugging information generator used by the compiler to mark sequence points and annotate local variables.</param>
        /// <returns>A delegate containing the compiled version of the lambda.</returns>
        public new TDelegate Compile(DebugInfoGenerator debugInfoGenerator)
        {
            return Compile();
        }
    }

    // Separate expression creation class to hide the CreateExpressionFunc function from users reflecting on Expression<T>
    internal static class ExpressionCreator<TDelegate>
    {
        public static Expression<TDelegate> CreateExpressionFunc(Expression body, string? name, bool tailCall, ReadOnlyCollection<ParameterExpression> parameters)
        {
            if (name == null && !tailCall)
            {
                switch (parameters.Count)
                {
                    case 0: return new Expression0<TDelegate>(body);
                    case 1: return new Expression1<TDelegate>(body, parameters[0]);
                    case 2: return new Expression2<TDelegate>(body, parameters[0], parameters[1]);
                    case 3: return new Expression3<TDelegate>(body, parameters[0], parameters[1], parameters[2]);
                    default: return new ExpressionN<TDelegate>(body, parameters);
                }
            }

            return new FullExpression<TDelegate>(body, name, tailCall, parameters);
        }
    }

    internal sealed class Expression0<TDelegate> : Expression<TDelegate>
    {
        public Expression0(Expression body)
            : base(body)
        {
        }

        internal override int ParameterCount => 0;

        internal override bool SameParameters(ICollection<ParameterExpression>? parameters) =>
            parameters == null || parameters.Count == 0;

        internal override ParameterExpression GetParameter(int index)
        {
            throw Error.ArgumentOutOfRange(nameof(index));
        }

        internal override ReadOnlyCollection<ParameterExpression> GetOrMakeParameters() => ReadOnlyCollection<ParameterExpression>.Empty;

        internal override Expression<TDelegate> Rewrite(Expression body, ParameterExpression[]? parameters)
        {
            Debug.Assert(body != null);
            Debug.Assert(parameters == null || parameters.Length == 0);

            return Expression.Lambda<TDelegate>(body, parameters);
        }
    }

    internal sealed class Expression1<TDelegate> : Expression<TDelegate>
    {
        private object _par0;

        public Expression1(Expression body, ParameterExpression par0)
            : base(body)
        {
            _par0 = par0;
        }

        internal override int ParameterCount => 1;

        internal override ParameterExpression GetParameter(int index) =>
            index switch
            {
                0 => ExpressionUtils.ReturnObject<ParameterExpression>(_par0),
                _ => throw Error.ArgumentOutOfRange(nameof(index)),
            };

        internal override bool SameParameters(ICollection<ParameterExpression>? parameters)
        {
            if (parameters != null && parameters.Count == 1)
            {
                using (IEnumerator<ParameterExpression> en = parameters.GetEnumerator())
                {
                    en.MoveNext();
                    return en.Current == ExpressionUtils.ReturnObject<ParameterExpression>(_par0);
                }
            }

            return false;
        }

        internal override ReadOnlyCollection<ParameterExpression> GetOrMakeParameters() => ExpressionUtils.ReturnReadOnly(this, ref _par0);

        internal override Expression<TDelegate> Rewrite(Expression body, ParameterExpression[]? parameters)
        {
            Debug.Assert(body != null);
            Debug.Assert(parameters == null || parameters.Length == 1);

            if (parameters != null)
            {
                return Expression.Lambda<TDelegate>(body, parameters);
            }

            return Expression.Lambda<TDelegate>(body, ExpressionUtils.ReturnObject<ParameterExpression>(_par0));
        }
    }

    internal sealed class Expression2<TDelegate> : Expression<TDelegate>
    {
        private object _par0;
        private readonly ParameterExpression _par1;

        public Expression2(Expression body, ParameterExpression par0, ParameterExpression par1)
            : base(body)
        {
            _par0 = par0;
            _par1 = par1;
        }

        internal override int ParameterCount => 2;

        internal override ParameterExpression GetParameter(int index) =>
            index switch
            {
                0 => ExpressionUtils.ReturnObject<ParameterExpression>(_par0),
                1 => _par1,
                _ => throw Error.ArgumentOutOfRange(nameof(index)),
            };

        internal override bool SameParameters(ICollection<ParameterExpression>? parameters)
        {
            if (parameters != null && parameters.Count == 2)
            {
                if (_par0 is ReadOnlyCollection<ParameterExpression> alreadyCollection)
                {
                    return ExpressionUtils.SameElements(parameters, alreadyCollection);
                }

                using (IEnumerator<ParameterExpression> en = parameters.GetEnumerator())
                {
                    en.MoveNext();
                    if (en.Current == _par0)
                    {
                        en.MoveNext();
                        return en.Current == _par1;
                    }
                }
            }

            return false;
        }


        internal override ReadOnlyCollection<ParameterExpression> GetOrMakeParameters() => ExpressionUtils.ReturnReadOnly(this, ref _par0);

        internal override Expression<TDelegate> Rewrite(Expression body, ParameterExpression[]? parameters)
        {
            Debug.Assert(body != null);
            Debug.Assert(parameters == null || parameters.Length == 2);

            if (parameters != null)
            {
                return Expression.Lambda<TDelegate>(body, parameters);
            }

            return Expression.Lambda<TDelegate>(body, ExpressionUtils.ReturnObject<ParameterExpression>(_par0), _par1);
        }
    }

    internal sealed class Expression3<TDelegate> : Expression<TDelegate>
    {
        private object _par0;
        private readonly ParameterExpression _par1;
        private readonly ParameterExpression _par2;

        public Expression3(Expression body, ParameterExpression par0, ParameterExpression par1, ParameterExpression par2)
            : base(body)
        {
            _par0 = par0;
            _par1 = par1;
            _par2 = par2;
        }

        internal override int ParameterCount => 3;

        internal override ParameterExpression GetParameter(int index) =>
            index switch
            {
                0 => ExpressionUtils.ReturnObject<ParameterExpression>(_par0),
                1 => _par1,
                2 => _par2,
                _ => throw Error.ArgumentOutOfRange(nameof(index)),
            };

        internal override bool SameParameters(ICollection<ParameterExpression>? parameters)
        {
            if (parameters != null && parameters.Count == 3)
            {
                if (_par0 is ReadOnlyCollection<ParameterExpression> alreadyCollection)
                {
                    return ExpressionUtils.SameElements(parameters, alreadyCollection);
                }

                using (IEnumerator<ParameterExpression> en = parameters.GetEnumerator())
                {
                    en.MoveNext();
                    if (en.Current == _par0)
                    {
                        en.MoveNext();
                        if (en.Current == _par1)
                        {
                            en.MoveNext();
                            return en.Current == _par2;
                        }
                    }
                }
            }

            return false;
        }

        internal override ReadOnlyCollection<ParameterExpression> GetOrMakeParameters() => ExpressionUtils.ReturnReadOnly(this, ref _par0);

        internal override Expression<TDelegate> Rewrite(Expression body, ParameterExpression[]? parameters)
        {
            Debug.Assert(body != null);
            Debug.Assert(parameters == null || parameters.Length == 3);

            if (parameters != null)
            {
                return Expression.Lambda<TDelegate>(body, parameters);
            }

            return Expression.Lambda<TDelegate>(body, ExpressionUtils.ReturnObject<ParameterExpression>(_par0), _par1, _par2);
        }
    }

    internal class ExpressionN<TDelegate> : Expression<TDelegate>
    {
        private IReadOnlyList<ParameterExpression> _parameters;

        public ExpressionN(Expression body, IReadOnlyList<ParameterExpression> parameters)
            : base(body)
        {
            _parameters = parameters;
        }

        internal override int ParameterCount => _parameters.Count;

        internal override ParameterExpression GetParameter(int index) => _parameters[index];

        internal override bool SameParameters(ICollection<ParameterExpression>? parameters) =>
            ExpressionUtils.SameElements(parameters, _parameters);

        internal override ReadOnlyCollection<ParameterExpression> GetOrMakeParameters() => ExpressionUtils.ReturnReadOnly(ref _parameters);

        internal override Expression<TDelegate> Rewrite(Expression body, ParameterExpression[]? parameters)
        {
            Debug.Assert(body != null);
            Debug.Assert(parameters == null || parameters.Length == _parameters.Count);

            return Expression.Lambda<TDelegate>(body, Name, TailCall, parameters ?? _parameters);
        }
    }

    internal sealed class FullExpression<TDelegate> : ExpressionN<TDelegate>
    {
        public FullExpression(Expression body, string? name, bool tailCall, IReadOnlyList<ParameterExpression> parameters)
            : base(body, parameters)
        {
            NameCore = name;
            TailCallCore = tailCall;
        }

        internal override string? NameCore { get; }
        internal override bool TailCallCore { get; }
    }

    public partial class Expression
    {
        /// <summary>
        /// Creates an Expression{T} given the delegate type. Caches the
        /// factory method to speed up repeated creations for the same T.
        /// </summary>
        [UnconditionalSuppressMessage("DynamicCode", "IL3050",
            Justification = "MakeGenericType is only used for a Type that should be a delegate type, which are always reference types.")]
        internal static LambdaExpression CreateLambda(Type delegateType, Expression body, string? name, bool tailCall, ReadOnlyCollection<ParameterExpression> parameters)
        {
            // Get or create a delegate to the public Expression.Lambda<T>
            // method and call that will be used for creating instances of this
            // delegate type
            Func<Expression, string?, bool, ReadOnlyCollection<ParameterExpression>, LambdaExpression>? fastPath;
            CacheDict<Type, Func<Expression, string?, bool, ReadOnlyCollection<ParameterExpression>, LambdaExpression>>? factories =
                s_lambdaFactories ??= new CacheDict<Type, Func<Expression, string?, bool, ReadOnlyCollection<ParameterExpression>, LambdaExpression>>(50);

            if (!factories.TryGetValue(delegateType, out fastPath))
            {
                MethodInfo create;
                if (LambdaExpression.CanCompileToIL)
                {
#pragma warning disable IL3050
                    // Analyzer doesn't yet understand feature switches
                    create = typeof(Expression<>).MakeGenericType(delegateType).GetMethod("Create", BindingFlags.Static | BindingFlags.NonPublic)!;
#pragma warning restore IL3050
                }
                else
                {
                    create = typeof(ExpressionCreator<>).MakeGenericType(delegateType).GetMethod("CreateExpressionFunc", BindingFlags.Static | BindingFlags.Public)!;
                }

                if (delegateType.IsCollectible)
                {
                    return (LambdaExpression)create.Invoke(null, new object?[] { body, name, tailCall, parameters })!;
                }

                factories[delegateType] = fastPath = (Func<Expression, string?, bool, ReadOnlyCollection<ParameterExpression>, LambdaExpression>)create.CreateDelegate(typeof(Func<Expression, string?, bool, ReadOnlyCollection<ParameterExpression>, LambdaExpression>));
            }

            return fastPath(body, name, tailCall, parameters);
        }

        /// <summary>
        /// Creates an <see cref="Expression{TDelegate}"/> where the delegate type is known at compile time.
        /// </summary>
        /// <typeparam name="TDelegate">The delegate type.</typeparam>
        /// <param name="body">An <see cref="Expression"/> to set the <see cref="LambdaExpression.Body"/> property equal to.</param>
        /// <param name="parameters">An array that contains <see cref="ParameterExpression"/> objects to use to populate the <see cref="LambdaExpression.Parameters"/> collection.</param>
        /// <returns>An <see cref="Expression{TDelegate}"/> that has the <see cref="NodeType"/> property equal to <see cref="ExpressionType.Lambda"/> and the <see cref="LambdaExpression.Body"/> and <see cref="LambdaExpression.Parameters"/> properties set to the specified values.</returns>
        public static Expression<TDelegate> Lambda<TDelegate>(Expression body, params ParameterExpression[]? parameters)
        {
            return Lambda<TDelegate>(body, false, (IEnumerable<ParameterExpression>?)parameters);
        }

        /// <summary>
        /// Creates an <see cref="Expression{TDelegate}"/> where the delegate type is known at compile time.
        /// </summary>
        /// <typeparam name="TDelegate">The delegate type.</typeparam>
        /// <param name="body">An <see cref="Expression"/> to set the <see cref="LambdaExpression.Body"/> property equal to.</param>
        /// <param name="tailCall">A <see cref="bool"/> that indicates if tail call optimization will be applied when compiling the created expression.</param>
        /// <param name="parameters">An array that contains <see cref="ParameterExpression"/> objects to use to populate the <see cref="LambdaExpression.Parameters"/> collection.</param>
        /// <returns>An <see cref="Expression{TDelegate}"/> that has the <see cref="NodeType"/> property equal to <see cref="ExpressionType.Lambda"/> and the <see cref="LambdaExpression.Body"/> and <see cref="LambdaExpression.Parameters"/> properties set to the specified values.</returns>
        public static Expression<TDelegate> Lambda<TDelegate>(Expression body, bool tailCall, params ParameterExpression[]? parameters)
        {
            return Lambda<TDelegate>(body, tailCall, (IEnumerable<ParameterExpression>?)parameters);
        }

        /// <summary>
        /// Creates an <see cref="Expression{TDelegate}"/> where the delegate type is known at compile time.
        /// </summary>
        /// <typeparam name="TDelegate">The delegate type.</typeparam>
        /// <param name="body">An <see cref="Expression"/> to set the <see cref="LambdaExpression.Body"/> property equal to.</param>
        /// <param name="parameters">An <see cref="IEnumerable{T}"/> that contains <see cref="ParameterExpression"/> objects to use to populate the <see cref="LambdaExpression.Parameters"/> collection.</param>
        /// <returns>An <see cref="Expression{TDelegate}"/> that has the <see cref="NodeType"/> property equal to <see cref="ExpressionType.Lambda"/> and the <see cref="LambdaExpression.Body"/> and <see cref="LambdaExpression.Parameters"/> properties set to the specified values.</returns>
        public static Expression<TDelegate> Lambda<TDelegate>(Expression body, IEnumerable<ParameterExpression>? parameters)
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
        public static Expression<TDelegate> Lambda<TDelegate>(Expression body, bool tailCall, IEnumerable<ParameterExpression>? parameters)
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
        public static Expression<TDelegate> Lambda<TDelegate>(Expression body, string? name, IEnumerable<ParameterExpression>? parameters)
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
        public static Expression<TDelegate> Lambda<TDelegate>(Expression body, string? name, bool tailCall, IEnumerable<ParameterExpression>? parameters)
        {
            ReadOnlyCollection<ParameterExpression> parameterList = parameters.ToReadOnly();
            ValidateLambdaArgs(typeof(TDelegate), ref body, parameterList, nameof(TDelegate));
            if (LambdaExpression.CanCompileToIL)
            {
                return Expression<TDelegate>.Create(body, name, tailCall, parameterList);
            }
            else
            {
                return ExpressionCreator<TDelegate>.CreateExpressionFunc(body, name, tailCall, parameterList);
            }
        }

        /// <summary>
        /// Creates a <see cref="LambdaExpression"/> by first constructing a delegate type.
        /// </summary>
        /// <param name="body">An <see cref="Expression"/> to set the <see cref="LambdaExpression.Body"/> property equal to.</param>
        /// <param name="parameters">An array that contains <see cref="ParameterExpression"/> objects to use to populate the <see cref="LambdaExpression.Parameters"/> collection.</param>
        /// <returns>A <see cref="LambdaExpression"/> that has the <see cref="NodeType"/> property equal to <see cref="ExpressionType.Lambda"/> and the <see cref="LambdaExpression.Body"/> and <see cref="LambdaExpression.Parameters"/> properties set to the specified values.</returns>
        [RequiresDynamicCode(Expression.DelegateCreationRequiresDynamicCode)]
        public static LambdaExpression Lambda(Expression body, params ParameterExpression[]? parameters)
        {
            return Lambda(body, false, (IEnumerable<ParameterExpression>?)parameters);
        }

        /// <summary>
        /// Creates a <see cref="LambdaExpression"/> by first constructing a delegate type.
        /// </summary>
        /// <param name="body">An <see cref="Expression"/> to set the <see cref="LambdaExpression.Body"/> property equal to.</param>
        /// <param name="tailCall">A <see cref="bool"/> that indicates if tail call optimization will be applied when compiling the created expression.</param>
        /// <param name="parameters">An array that contains <see cref="ParameterExpression"/> objects to use to populate the <see cref="LambdaExpression.Parameters"/> collection.</param>
        /// <returns>A <see cref="LambdaExpression"/> that has the <see cref="NodeType"/> property equal to <see cref="ExpressionType.Lambda"/> and the <see cref="LambdaExpression.Body"/> and <see cref="LambdaExpression.Parameters"/> properties set to the specified values.</returns>
        [RequiresDynamicCode(Expression.DelegateCreationRequiresDynamicCode)]
        public static LambdaExpression Lambda(Expression body, bool tailCall, params ParameterExpression[]? parameters)
        {
            return Lambda(body, tailCall, (IEnumerable<ParameterExpression>?)parameters);
        }

        /// <summary>
        /// Creates a <see cref="LambdaExpression"/> by first constructing a delegate type.
        /// </summary>
        /// <param name="body">An <see cref="Expression"/> to set the <see cref="LambdaExpression.Body"/> property equal to.</param>
        /// <param name="parameters">An <see cref="IEnumerable{T}"/> that contains <see cref="ParameterExpression"/> objects to use to populate the <see cref="LambdaExpression.Parameters"/> collection.</param>
        /// <returns>A <see cref="LambdaExpression"/> that has the <see cref="NodeType"/> property equal to <see cref="ExpressionType.Lambda"/> and the <see cref="LambdaExpression.Body"/> and <see cref="LambdaExpression.Parameters"/> properties set to the specified values.</returns>
        [RequiresDynamicCode(Expression.DelegateCreationRequiresDynamicCode)]
        public static LambdaExpression Lambda(Expression body, IEnumerable<ParameterExpression>? parameters)
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
        [RequiresDynamicCode(Expression.DelegateCreationRequiresDynamicCode)]
        public static LambdaExpression Lambda(Expression body, bool tailCall, IEnumerable<ParameterExpression>? parameters)
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
        public static LambdaExpression Lambda(Type delegateType, Expression body, params ParameterExpression[]? parameters)
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
        public static LambdaExpression Lambda(Type delegateType, Expression body, bool tailCall, params ParameterExpression[]? parameters)
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
        public static LambdaExpression Lambda(Type delegateType, Expression body, IEnumerable<ParameterExpression>? parameters)
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
        public static LambdaExpression Lambda(Type delegateType, Expression body, bool tailCall, IEnumerable<ParameterExpression>? parameters)
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
        [RequiresDynamicCode(Expression.DelegateCreationRequiresDynamicCode)]
        public static LambdaExpression Lambda(Expression body, string? name, IEnumerable<ParameterExpression>? parameters)
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
        [RequiresDynamicCode(Expression.DelegateCreationRequiresDynamicCode)]
        public static LambdaExpression Lambda(Expression body, string? name, bool tailCall, IEnumerable<ParameterExpression>? parameters)
        {
            ArgumentNullException.ThrowIfNull(body);

            ReadOnlyCollection<ParameterExpression> parameterList = parameters.ToReadOnly();

            int paramCount = parameterList.Count;
            Type[] typeArgs = new Type[paramCount + 1];
            if (paramCount > 0)
            {
                var set = new HashSet<ParameterExpression>();
                for (int i = 0; i < paramCount; i++)
                {
                    ParameterExpression param = parameterList[i];
                    ArgumentNullException.ThrowIfNull(param, "parameter");
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
        public static LambdaExpression Lambda(Type delegateType, Expression body, string? name, IEnumerable<ParameterExpression>? parameters)
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
        public static LambdaExpression Lambda(Type delegateType, Expression body, string? name, bool tailCall, IEnumerable<ParameterExpression>? parameters)
        {
            ReadOnlyCollection<ParameterExpression> paramList = parameters.ToReadOnly();
            ValidateLambdaArgs(delegateType, ref body, paramList, nameof(delegateType));

            return CreateLambda(delegateType, body, name, tailCall, paramList);
        }

        private static void ValidateLambdaArgs(Type delegateType, ref Expression body, ReadOnlyCollection<ParameterExpression> parameters, string paramName)
        {
            ArgumentNullException.ThrowIfNull(delegateType);
            ExpressionUtils.RequiresCanRead(body, nameof(body));

            if (!typeof(MulticastDelegate).IsAssignableFrom(delegateType) || delegateType == typeof(MulticastDelegate))
            {
                throw Error.LambdaTypeMustBeDerivedFromSystemDelegate(paramName);
            }

            TypeUtils.ValidateType(delegateType, nameof(delegateType), allowByRef: true, allowPointer: true);

            CacheDict<Type, MethodInfo> ldc = s_lambdaDelegateCache;
            if (!ldc.TryGetValue(delegateType, out MethodInfo? mi))
            {
                mi = delegateType.GetInvokeMethod();
                if (!delegateType.IsCollectible)
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
                    ExpressionUtils.RequiresCanRead(pex, nameof(parameters), i);
                    Type pType = pi.ParameterType;
                    if (pex.IsByRef)
                    {
                        if (!pType.IsByRef)
                        {
                            //We cannot pass a parameter of T& to a delegate that takes T or any non-ByRef type.
                            throw Error.ParameterExpressionNotValidAsDelegate(pex.Type.MakeByRefType(), pType);
                        }
                        pType = pType.GetElementType()!;
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

        private static TryGetFuncActionArgsResult ValidateTryGetFuncActionArgs(Type[]? typeArgs)
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
        [RequiresDynamicCode(Expression.DelegateCreationRequiresDynamicCode)]
        public static Type GetFuncType(params Type[]? typeArgs)
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
        [RequiresDynamicCode(Expression.DelegateCreationRequiresDynamicCode)]
        public static bool TryGetFuncType(Type[] typeArgs, [NotNullWhen(true)] out Type? funcType)
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
        [RequiresDynamicCode(Expression.DelegateCreationRequiresDynamicCode)]
        public static Type GetActionType(params Type[]? typeArgs)
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
        [RequiresDynamicCode(Expression.DelegateCreationRequiresDynamicCode)]
        public static bool TryGetActionType(Type[] typeArgs, [NotNullWhen(true)] out Type? actionType)
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
        [RequiresDynamicCode(Expression.DelegateCreationRequiresDynamicCode)]
        public static Type GetDelegateType(params Type[] typeArgs)
        {
            ContractUtils.RequiresNotEmpty(typeArgs, nameof(typeArgs));
            ContractUtils.RequiresNotNullItems(typeArgs, nameof(typeArgs));
            return Compiler.DelegateHelpers.MakeDelegateType(typeArgs);
        }
    }
}
