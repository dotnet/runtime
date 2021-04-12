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
    /// <summary>Describes a lambda expression. This captures a block of code that is similar to a .NET method body.</summary>
    /// <remarks>The <see cref="System.Linq.Expressions.LambdaExpression" /> type represents a lambda expression in the form of an expression tree. The <see cref="System.Linq.Expressions.Expression{T}" /> type, which derives from <see cref="System.Linq.Expressions.LambdaExpression" /> and captures the type of the lambda expression more explicitly, can also be used to represent a lambda expression. At runtime, an expression tree node that represents a lambda expression is always of type <see cref="System.Linq.Expressions.Expression{T}" />.
    /// The value of the <see cref="O:System.Linq.Expressions.Expression.NodeType" /> property of a <see cref="System.Linq.Expressions.LambdaExpression" /> is <see cref="System.Linq.Expressions.ExpressionType.Lambda" />.
    /// Use the <see cref="O:System.Linq.Expressions.Expression.Lambda" /> factory methods to create a <see cref="System.Linq.Expressions.LambdaExpression" /> object.</remarks>
    /// <example>The following example demonstrates how to create an expression that represents a lambda expression that adds 1 to the passed argument by using the <see cref="O:System.Linq.Expressions.Expression.Lambda" /> method.
    /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/cs/program.cs" id="Snippet42":::
    /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/vb/module1.vb" id="Snippet42":::</example>
    [DebuggerTypeProxy(typeof(LambdaExpressionProxy))]
    public abstract class LambdaExpression : Expression, IParameterProvider
    {
        private readonly Expression _body;

        internal LambdaExpression(Expression body)
        {
            _body = body;
        }

        /// <summary>Gets the static type of the expression that this <see cref="System.Linq.Expressions.Expression" /> represents.</summary>
        /// <value>The <see cref="System.Linq.Expressions.LambdaExpression.Type" /> that represents the static type of the expression.</value>
        public sealed override Type Type => TypeCore;

        internal abstract Type TypeCore { get; }

        internal abstract Type PublicType { get; }

        /// <summary>Returns the node type of this <see cref="System.Linq.Expressions.Expression" />.</summary>
        /// <value>The <see cref="System.Linq.Expressions.ExpressionType" /> that represents this expression.</value>
        public sealed override ExpressionType NodeType => ExpressionType.Lambda;

        /// <summary>Gets the parameters of the lambda expression.</summary>
        /// <value>A <see cref="System.Collections.ObjectModel.ReadOnlyCollection{T}" /> of <see cref="System.Linq.Expressions.ParameterExpression" /> objects that represent the parameters of the lambda expression.</value>
        public ReadOnlyCollection<ParameterExpression> Parameters => GetOrMakeParameters();

        /// <summary>Gets the name of the lambda expression.</summary>
        /// <value>The name of the lambda expression.</value>
        /// <remarks>Used for debugging.</remarks>
        public string? Name => NameCore;

        internal virtual string? NameCore => null;

        /// <summary>Gets the body of the lambda expression.</summary>
        /// <value>An <see cref="System.Linq.Expressions.Expression" /> that represents the body of the lambda expression.</value>
        public Expression Body => _body;

        /// <summary>Gets the return type of the lambda expression.</summary>
        /// <value>The <see cref="System.Type" /> object representing the type of the lambda expression.</value>
        public Type ReturnType => Type.GetInvokeMethod().ReturnType;

        /// <summary>Gets the value that indicates if the lambda expression will be compiled with the tail call optimization.</summary>
        /// <value><see langword="true" /> if the lambda expression will be compiled with the tail call optimization; otherwise, <see langword="false" />.</value>
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

            return GetDerivedCompileMethod(lambdaExpressionType);
        }

        [DynamicDependency("Compile()", typeof(Expression<>))]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2070:UnrecognizedReflectionPattern",
            Justification = "The 'Compile' method will be preserved by the DynamicDependency.")]
        private static MethodInfo GetDerivedCompileMethod(Type lambdaExpressionType)
        {
            Debug.Assert(lambdaExpressionType.IsAssignableTo(typeof(LambdaExpression)) && lambdaExpressionType != typeof(LambdaExpression));

            MethodInfo result = lambdaExpressionType.GetMethod("Compile", Type.EmptyTypes)!;
            Debug.Assert(result.DeclaringType!.IsGenericType && result.DeclaringType.GetGenericTypeDefinition() == typeof(Expression<>));

            return result;
        }

        /// <summary>Produces a delegate that represents the lambda expression.</summary>
        /// <returns>A <see cref="System.Delegate" /> that contains the compiled version of the lambda expression.</returns>
        /// <remarks>The <see cref="O:System.Linq.Expressions.LambdaExpression.Compile" /> method can be used to convert a <see cref="System.Linq.Expressions.LambdaExpression" /> expression tree into the delegate that it represents.</remarks>
        public Delegate Compile()
        {
#if FEATURE_COMPILE
            return Compiler.LambdaCompiler.Compile(this);
#else
            return new Interpreter.LightCompiler().CompileTop(this).CreateDelegate();
#endif
        }

        /// <summary>Produces an interpreted or compiled delegate that represents the lambda expression.</summary>
        /// <param name="preferInterpretation"><see langword="true" /> to indicate that the expression should be compiled to an interpreted form, if it's available; otherwise, <see langword="false" />.</param>
        /// <returns>A delegate that represents the compiled lambda expression described by the <see cref="System.Linq.Expressions.LambdaExpression" /> object.</returns>
        public Delegate Compile(bool preferInterpretation)
        {
#if FEATURE_COMPILE && FEATURE_INTERPRET
            if (preferInterpretation)
            {
                return new Interpreter.LightCompiler().CompileTop(this).CreateDelegate();
            }
#endif
            return Compile();
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
            var type = method.DeclaringType as System.Reflection.Emit.TypeBuilder;
            if (type == null) throw Error.MethodBuilderDoesNotHaveTypeBuilder();

            Compiler.LambdaCompiler.Compile(this, method);
        }
#endif


#if FEATURE_COMPILE
        internal abstract LambdaExpression Accept(Compiler.StackSpiller spiller);
#endif
        /// <summary>Produces a delegate that represents the lambda expression.</summary>
        /// <param name="debugInfoGenerator">Debugging information generator used by the compiler to mark sequence points and annotate local variables.</param>
        /// <returns>A delegate containing the compiled version of the lambda.</returns>
        public Delegate Compile(DebugInfoGenerator debugInfoGenerator)
        {
            return Compile();
        }
    }

    /// <summary>Represents a strongly typed lambda expression as a data structure in the form of an expression tree. This class cannot be inherited.</summary>
    /// <typeparam name="TDelegate">The type of the delegate that the <see cref="System.Linq.Expressions.Expression{T}" /> represents.</typeparam>
    /// <remarks>When a lambda expression is assigned to a variable, field, or parameter whose type is <see cref="System.Linq.Expressions.Expression{T}" />, the compiler emits instructions to build an expression tree.
    /// <format type="text/markdown"><![CDATA[
    /// > [!NOTE]
    /// >  A conversion from a lambda expression to type `Expression<D>` (`Expression(Of D)` in Visual Basic) exists if a conversion from the lambda expression to a delegate of type `D` exists. However, the conversion may fail, for example, if the body of the lambda expression is a block. This means that delegates and expression trees behave similarly with regard to overload resolution.
    /// ]]></format>
    /// The expression tree is an in-memory data representation of the lambda expression. The expression tree makes the structure of the lambda expression transparent and explicit. You can interact with the data in the expression tree just as you can with any other data structure.
    /// The ability to treat expressions as data structures enables APIs to receive user code in a format that can be inspected, transformed, and processed in a custom manner. For example, the LINQ to SQL data access implementation uses this facility to translate expression trees to Transact-SQL statements that can be evaluated by the database.
    /// Many standard query operators defined in the <see langword="System.Linq.Queryable" /> class have one or more parameters of type <see cref="System.Linq.Expressions.Expression{T}" />.
    /// The <see cref="O:System.Linq.Expressions.Expression.NodeType" /> of an <see cref="System.Linq.Expressions.Expression{T}" /> is <see cref="System.Linq.Expressions.ExpressionType.Lambda" />.
    /// Use the <see cref="System.Linq.Expressions.Expression.Lambda{T}(System.Linq.Expressions.Expression,System.Collections.Generic.IEnumerable{System.Linq.Expressions.ParameterExpression})" /> or <see cref="System.Linq.Expressions.Expression.Lambda{T}(System.Linq.Expressions.Expression,System.Linq.Expressions.ParameterExpression[])" /> method to create an <see cref="System.Linq.Expressions.Expression{T}" /> object.</remarks>
    /// <example>The following code example demonstrates how to represent a lambda expression both as executable code in the form of a delegate and as data in the form of an expression tree. It also demonstrates how to turn the expression tree back into executable code by using the <see cref="O:System.Linq.Expressions.Expression{T}.Compile" /> method.
    /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Expressions.ExpressionT/CS/ExpressionT.cs" id="Snippet1":::
    /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Expressions.ExpressionT/VB/ExpressionT.vb" id="Snippet1":::</example>
    /// <related type="Article" href="/dotnet/csharp/programming-guide/statements-expressions-operators/lambda-expressions">Lambda Expressions (C# Programming Guide)</related>
    /// <related type="Article" href="https://msdn.microsoft.com/library/fb1d3ed8-d5b0-4211-a71f-dd271529294b">Expression Trees</related>
    public class Expression<TDelegate> : LambdaExpression
    {
        internal Expression(Expression body)
            : base(body)
        {
        }

        internal sealed override Type TypeCore => typeof(TDelegate);

        internal override Type PublicType => typeof(Expression<TDelegate>);

        /// <summary>Compiles the lambda expression described by the expression tree into executable code and produces a delegate that represents the lambda expression.</summary>
        /// <returns>A delegate of type <typeparamref name="TDelegate" /> that represents the compiled lambda expression described by the <see cref="System.Linq.Expressions.Expression{T}" />.</returns>
        /// <remarks>The <see cref="O:System.Linq.Expressions.Expression{T}.Compile" /> method produces a delegate of type `TDelegate` at runtime. When that delegate is executed, it has the behavior described by the semantics of the <see cref="System.Linq.Expressions.Expression{T}" />.
        /// The <see cref="O:System.Linq.Expressions.Expression{T}.Compile" /> method can be used to obtain the value of any expression tree. First, create a lambda expression that has the expression as its body by using the <see cref="O:System.Linq.Expressions.Expression.Lambda" /> method. Then call <see cref="O:System.Linq.Expressions.Expression{T}.Compile" /> to obtain a delegate, and execute the delegate to obtain the value of the expression.</remarks>
        /// <example>The following code example demonstrates how <see cref="O:System.Linq.Expressions.Expression{T}.Compile" /> is used to execute an expression tree.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Expressions.ExpressionT/CS/ExpressionT.cs" id="Snippet2":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Expressions.ExpressionT/VB/ExpressionT.vb" id="Snippet2":::</example>
        public new TDelegate Compile()
        {
#if FEATURE_COMPILE
            return (TDelegate)(object)Compiler.LambdaCompiler.Compile(this);
#else
            return (TDelegate)(object)new Interpreter.LightCompiler().CompileTop(this).CreateDelegate();
#endif
        }

        /// <summary>Compiles the lambda expression described by the expression tree into interpreted or compiled code and produces a delegate that represents the lambda expression.</summary>
        /// <param name="preferInterpretation"><see langword="true" /> to indicate that the expression should be compiled to an interpreted form, if it is available; <see langword="false" /> otherwise.</param>
        /// <returns>A delegate that represents the compiled lambda expression described by the <see cref="System.Linq.Expressions.Expression{T}" />.</returns>
        public new TDelegate Compile(bool preferInterpretation)
        {
#if FEATURE_COMPILE && FEATURE_INTERPRET
            if (preferInterpretation)
            {
                return (TDelegate)(object)new Interpreter.LightCompiler().CompileTop(this).CreateDelegate();
            }
#endif
            return Compile();
        }

        /// <summary>Creates a new expression that is like this one, but using the supplied children. If all of the children are the same, it will return this expression.</summary>
        /// <param name="body">The <see cref="System.Linq.Expressions.LambdaExpression.Body" /> property of the result.</param>
        /// <param name="parameters">The <see cref="System.Linq.Expressions.LambdaExpression.Parameters" /> property of the result.</param>
        /// <returns>This expression if no children are changed or an expression with the updated children.</returns>
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

        /// <summary>Dispatches to the specific visit method for this node type.</summary>
        /// <param name="visitor">The visitor to visit this node with.</param>
        /// <returns>The result of visiting this node.</returns>
        protected internal override Expression Accept(ExpressionVisitor visitor)
        {
            return visitor.VisitLambda(this);
        }

#if FEATURE_COMPILE
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
#endif

        /// <summary>Produces a delegate that represents the lambda expression.</summary>
        /// <param name="debugInfoGenerator">Debugging information generator used by the compiler to mark sequence points and annotate local variables.</param>
        /// <returns>A delegate containing the compiled version of the lambda.</returns>
        public new TDelegate Compile(DebugInfoGenerator debugInfoGenerator)
        {
            return Compile();
        }
    }

#if !FEATURE_COMPILE
    // Separate expression creation class to hide the CreateExpressionFunc function from users reflecting on Expression<T>
    public class ExpressionCreator<TDelegate>
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
#endif

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

        internal override ReadOnlyCollection<ParameterExpression> GetOrMakeParameters() => EmptyReadOnlyCollection<ParameterExpression>.Instance;

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

    /// <summary>Provides the base class from which the classes that represent expression tree nodes are derived. It also contains <see langword="static" /> (<see langword="Shared" /> in Visual Basic) factory methods to create the various node types. This is an <see langword="abstract" /> class.</summary>
    /// <remarks></remarks>
    /// <example>The following code example shows how to create a block expression. The block expression consists of two <see cref="System.Linq.Expressions.MethodCallExpression" /> objects and one <see cref="System.Linq.Expressions.ConstantExpression" /> object.
    /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/cs/program.cs" id="Snippet13":::
    /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/vb/module1.vb" id="Snippet13":::</example>
    public partial class Expression
    {
        /// <summary>
        /// Creates an Expression{T} given the delegate type. Caches the
        /// factory method to speed up repeated creations for the same T.
        /// </summary>
        internal static LambdaExpression CreateLambda(Type delegateType, Expression body, string? name, bool tailCall, ReadOnlyCollection<ParameterExpression> parameters)
        {
            // Get or create a delegate to the public Expression.Lambda<T>
            // method and call that will be used for creating instances of this
            // delegate type
            Func<Expression, string?, bool, ReadOnlyCollection<ParameterExpression>, LambdaExpression>? fastPath;
            CacheDict<Type, Func<Expression, string?, bool, ReadOnlyCollection<ParameterExpression>, LambdaExpression>>? factories = s_lambdaFactories;
            if (factories == null)
            {
                s_lambdaFactories = factories = new CacheDict<Type, Func<Expression, string?, bool, ReadOnlyCollection<ParameterExpression>, LambdaExpression>>(50);
            }

            if (!factories.TryGetValue(delegateType, out fastPath))
            {
#if FEATURE_COMPILE
                MethodInfo create = typeof(Expression<>).MakeGenericType(delegateType).GetMethod("Create", BindingFlags.Static | BindingFlags.NonPublic)!;
#else
                MethodInfo create = typeof(ExpressionCreator<>).MakeGenericType(delegateType).GetMethod("CreateExpressionFunc", BindingFlags.Static | BindingFlags.Public)!;
#endif
                if (delegateType.IsCollectible)
                {
                    return (LambdaExpression)create.Invoke(null, new object?[] { body, name, tailCall, parameters })!;
                }

                factories[delegateType] = fastPath = (Func<Expression, string?, bool, ReadOnlyCollection<ParameterExpression>, LambdaExpression>)create.CreateDelegate(typeof(Func<Expression, string?, bool, ReadOnlyCollection<ParameterExpression>, LambdaExpression>));
            }

            return fastPath(body, name, tailCall, parameters);
        }

        /// <summary>Creates an <see cref="System.Linq.Expressions.Expression{T}" /> where the delegate type is known at compile time, with an array of parameter expressions.</summary>
        /// <typeparam name="TDelegate">A delegate type.</typeparam>
        /// <param name="body">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.LambdaExpression.Body" /> property equal to.</param>
        /// <param name="parameters">An array of <see cref="System.Linq.Expressions.ParameterExpression" /> objects to use to populate the <see cref="System.Linq.Expressions.LambdaExpression.Parameters" /> collection.</param>
        /// <returns>An <see cref="System.Linq.Expressions.Expression{T}" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.Lambda" /> and the <see cref="System.Linq.Expressions.LambdaExpression.Body" /> and <see cref="System.Linq.Expressions.LambdaExpression.Parameters" /> properties set to the specified values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="body" /> is <see langword="null" />.
        /// -or-
        /// One or more elements in <paramref name="parameters" /> are <see langword="null" />.</exception>
        /// <exception cref="System.ArgumentException"><typeparamref name="TDelegate" /> is not a delegate type.
        /// -or-
        /// <paramref name="body" />.Type represents a type that is not assignable to the return type of <typeparamref name="TDelegate" />.
        /// -or-
        /// <paramref name="parameters" /> does not contain the same number of elements as the list of parameters for <typeparamref name="TDelegate" />.
        /// -or-
        /// The <see cref="System.Linq.Expressions.Expression.Type" /> property of an element of <paramref name="parameters" /> is not assignable from the type of the corresponding parameter type of <typeparamref name="TDelegate" />.</exception>
        /// <remarks>The number of parameters for the delegate type <typeparamref name="TDelegate" /> must equal the number of elements in <paramref name="parameters" />.
        /// The elements of <paramref name="parameters" /> must be reference equal to the parameter expressions in<paramref name="body" />.
        /// The <see cref="O:System.Linq.Expressions.Expression.Type" /> property of the resulting object represents the type <typeparamref name="TDelegate" />. If <paramref name="parameters" /> is <see langword="null" />, the <see cref="O:System.Linq.Expressions.LambdaExpression.Parameters" /> property of the resulting object is an empty collection.</remarks>
        public static Expression<TDelegate> Lambda<TDelegate>(Expression body, params ParameterExpression[]? parameters)
        {
            return Lambda<TDelegate>(body, false, (IEnumerable<ParameterExpression>?)parameters);
        }

        /// <summary>Creates an <see cref="System.Linq.Expressions.Expression{T}" /> where the delegate type is known at compile time, with a parameter that indicates whether tail call optimization will be applied, and an array of parameter expressions.</summary>
        /// <typeparam name="TDelegate">The delegate type.</typeparam>
        /// <param name="body">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.LambdaExpression.Body" /> property equal to.</param>
        /// <param name="tailCall">A <see cref="bool" /> that indicates if tail call optimization will be applied when compiling the created expression.</param>
        /// <param name="parameters">An array that contains <see cref="System.Linq.Expressions.ParameterExpression" /> objects to use to populate the <see cref="System.Linq.Expressions.LambdaExpression.Parameters" /> collection.</param>
        /// <returns>An <see cref="System.Linq.Expressions.Expression{T}" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.Lambda" /> and the <see cref="System.Linq.Expressions.LambdaExpression.Body" /> and <see cref="System.Linq.Expressions.LambdaExpression.Parameters" /> properties set to the specified values.</returns>
        public static Expression<TDelegate> Lambda<TDelegate>(Expression body, bool tailCall, params ParameterExpression[]? parameters)
        {
            return Lambda<TDelegate>(body, tailCall, (IEnumerable<ParameterExpression>?)parameters);
        }

        /// <summary>Creates an <see cref="System.Linq.Expressions.Expression{T}" /> where the delegate type is known at compile time, with an enumerable collection of parameter expressions.</summary>
        /// <typeparam name="TDelegate">A delegate type.</typeparam>
        /// <param name="body">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.LambdaExpression.Body" /> property equal to.</param>
        /// <param name="parameters">An <see cref="System.Collections.Generic.IEnumerable{T}" /> that contains <see cref="System.Linq.Expressions.ParameterExpression" /> objects to use to populate the <see cref="System.Linq.Expressions.LambdaExpression.Parameters" /> collection.</param>
        /// <returns>An <see cref="System.Linq.Expressions.Expression{T}" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.Lambda" /> and the <see cref="System.Linq.Expressions.LambdaExpression.Body" /> and <see cref="System.Linq.Expressions.LambdaExpression.Parameters" /> properties set to the specified values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="body" /> is <see langword="null" />.
        /// -or-
        /// One or more elements in <paramref name="parameters" /> are <see langword="null" />.</exception>
        /// <exception cref="System.ArgumentException"><typeparamref name="TDelegate" /> is not a delegate type.
        /// -or-
        /// <paramref name="body" />.Type represents a type that is not assignable to the return type of <typeparamref name="TDelegate" />.
        /// -or-
        /// <paramref name="parameters" /> does not contain the same number of elements as the list of parameters for <typeparamref name="TDelegate" />.
        /// -or-
        /// The <see cref="System.Linq.Expressions.Expression.Type" /> property of an element of <paramref name="parameters" /> is not assignable from the type of the corresponding parameter type of <typeparamref name="TDelegate" />.</exception>
        /// <remarks>The number of parameters for the delegate type <typeparamref name="TDelegate" /> must equal the number of elements in <paramref name="parameters" />.
        /// The elements of <paramref name="parameters" /> must be reference equal to the parameter expressions in <paramref name="body" />.
        /// The <see cref="O:System.Linq.Expressions.Expression.Type" /> property of the resulting object represents the type <typeparamref name="TDelegate" />. If <paramref name="parameters" /> is <see langword="null" />, the <see cref="O:System.Linq.Expressions.LambdaExpression.Parameters" /> property of the resulting object is an empty collection.</remarks>
        public static Expression<TDelegate> Lambda<TDelegate>(Expression body, IEnumerable<ParameterExpression>? parameters)
        {
            return Lambda<TDelegate>(body, null, false, parameters);
        }

        /// <summary>Creates an <see cref="System.Linq.Expressions.Expression{T}" /> where the delegate type is known at compile time, with a parameter that indicates whether tail call optimization will be applied, and an enumerable collection of parameter expressions.</summary>
        /// <typeparam name="TDelegate">The delegate type.</typeparam>
        /// <param name="body">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.LambdaExpression.Body" /> property equal to.</param>
        /// <param name="tailCall">A <see cref="bool" /> that indicates if tail call optimization will be applied when compiling the created expression.</param>
        /// <param name="parameters">An <see cref="System.Collections.Generic.IEnumerable{T}" /> that contains <see cref="System.Linq.Expressions.ParameterExpression" /> objects to use to populate the <see cref="System.Linq.Expressions.LambdaExpression.Parameters" /> collection.</param>
        /// <returns>An <see cref="System.Linq.Expressions.Expression{T}" /> that has the <see cref="System.Linq.Expressions.LambdaExpression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.Lambda" /> and the <see cref="System.Linq.Expressions.LambdaExpression.Body" /> and <see cref="System.Linq.Expressions.LambdaExpression.Parameters" /> properties set to the specified values.</returns>
        public static Expression<TDelegate> Lambda<TDelegate>(Expression body, bool tailCall, IEnumerable<ParameterExpression>? parameters)
        {
            return Lambda<TDelegate>(body, null, tailCall, parameters);
        }

        /// <summary>Creates an <see cref="System.Linq.Expressions.Expression{T}" /> where the delegate type is known at compile time, with the name for the lambda, and an enumerable collection of parameter expressions.</summary>
        /// <typeparam name="TDelegate">The delegate type.</typeparam>
        /// <param name="body">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.LambdaExpression.Body" /> property equal to.</param>
        /// <param name="name">The name of the lambda. Used for generating debugging information.</param>
        /// <param name="parameters">An <see cref="System.Collections.Generic.IEnumerable{T}" /> that contains <see cref="System.Linq.Expressions.ParameterExpression" /> objects to use to populate the <see cref="System.Linq.Expressions.LambdaExpression.Parameters" /> collection.</param>
        /// <returns>An <see cref="System.Linq.Expressions.Expression{T}" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.Lambda" /> and the <see cref="System.Linq.Expressions.LambdaExpression.Body" /> and <see cref="System.Linq.Expressions.LambdaExpression.Parameters" /> properties set to the specified values.</returns>
        public static Expression<TDelegate> Lambda<TDelegate>(Expression body, string? name, IEnumerable<ParameterExpression>? parameters)
        {
            return Lambda<TDelegate>(body, name, false, parameters);
        }

        /// <summary>Creates an <see cref="System.Linq.Expressions.Expression{T}" /> where the delegate type is known at compile time, with the name for the lambda, a parameter that indicates whether tail call optimization will be applied, and an enumerable collection of parameter expressions.</summary>
        /// <typeparam name="TDelegate">The delegate type.</typeparam>
        /// <param name="body">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.LambdaExpression.Body" /> property equal to.</param>
        /// <param name="name">The name of the lambda. Used for generating debugging info.</param>
        /// <param name="tailCall">A <see cref="bool" /> that indicates if tail call optimization will be applied when compiling the created expression.</param>
        /// <param name="parameters">An <see cref="System.Collections.Generic.IEnumerable{T}" /> that contains <see cref="System.Linq.Expressions.ParameterExpression" /> objects to use to populate the <see cref="System.Linq.Expressions.LambdaExpression.Parameters" /> collection.</param>
        /// <returns>An <see cref="System.Linq.Expressions.Expression{T}" /> that has the <see cref="System.Linq.Expressions.LambdaExpression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.Lambda" /> and the <see cref="System.Linq.Expressions.LambdaExpression.Body" /> and <see cref="System.Linq.Expressions.LambdaExpression.Parameters" /> properties set to the specified values.</returns>
        public static Expression<TDelegate> Lambda<TDelegate>(Expression body, string? name, bool tailCall, IEnumerable<ParameterExpression>? parameters)
        {
            ReadOnlyCollection<ParameterExpression> parameterList = parameters.ToReadOnly();
            ValidateLambdaArgs(typeof(TDelegate), ref body, parameterList, nameof(TDelegate));
#if FEATURE_COMPILE
            return Expression<TDelegate>.Create(body, name, tailCall, parameterList);
#else
            return ExpressionCreator<TDelegate>.CreateExpressionFunc(body, name, tailCall, parameterList);
#endif
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.LambdaExpression" /> by first constructing a delegate type from the expression body, and an array of parameter expressions. It can be used when the delegate type is not known at compile time.</summary>
        /// <param name="body">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.LambdaExpression.Body" /> property equal to.</param>
        /// <param name="parameters">An array of <see cref="System.Linq.Expressions.ParameterExpression" /> objects to use to populate the <see cref="System.Linq.Expressions.LambdaExpression.Parameters" /> collection.</param>
        /// <returns>A <see cref="System.Linq.Expressions.LambdaExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.Lambda" /> and the <see cref="System.Linq.Expressions.LambdaExpression.Body" /> and <see cref="System.Linq.Expressions.LambdaExpression.Parameters" /> properties set to the specified values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="body" /> is <see langword="null" />.
        /// -or-
        /// One or more elements of <paramref name="parameters" /> are <see langword="null" />.</exception>
        /// <exception cref="System.ArgumentException"><paramref name="parameters" /> contains more than sixteen elements.</exception>
        /// <remarks>The <paramref name="parameters" /> parameter must not have more than sixteen elements.
        /// The elements of <paramref name="parameters" /> must be reference equal to the parameter expressions in <paramref name="body" />.
        /// This method constructs an appropriate delegate type from one of the `System.Func` generic delegates. It then passes the delegate type to one of the <see cref="System.Linq.Expressions.ExpressionType.Lambda" /> factory methods to create a <see cref="System.Linq.Expressions.LambdaExpression" />.</remarks>
        public static LambdaExpression Lambda(Expression body, params ParameterExpression[]? parameters)
        {
            return Lambda(body, false, (IEnumerable<ParameterExpression>?)parameters);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.LambdaExpression" /> by first constructing a delegate type from the expression body, a parameter that indicates whether tail call optimization will be applied, and an array of parameter expressions. It can be used when the delegate type is not known at compile time.</summary>
        /// <param name="body">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.LambdaExpression.Body" /> property equal to.</param>
        /// <param name="tailCall">A <see cref="bool" /> that indicates if tail call optimization will be applied when compiling the created expression.</param>
        /// <param name="parameters">An array that contains <see cref="System.Linq.Expressions.ParameterExpression" /> objects to use to populate the <see cref="System.Linq.Expressions.LambdaExpression.Parameters" /> collection.</param>
        /// <returns>A <see cref="System.Linq.Expressions.LambdaExpression" /> that has the <see cref="System.Linq.Expressions.LambdaExpression.NodeType" /> property equal to Lambda and the <see cref="System.Linq.Expressions.LambdaExpression.Body" /> and <see cref="System.Linq.Expressions.LambdaExpression.Parameters" /> properties set to the specified values.</returns>
        public static LambdaExpression Lambda(Expression body, bool tailCall, params ParameterExpression[]? parameters)
        {
            return Lambda(body, tailCall, (IEnumerable<ParameterExpression>?)parameters);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.LambdaExpression" /> by first constructing a delegate type from the expression body, and an enumerable collection of parameter expressions. It can be used when the delegate type is not known at compile time.</summary>
        /// <param name="body">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.LambdaExpression.Body" /> property equal to.</param>
        /// <param name="parameters">An <see cref="System.Collections.Generic.IEnumerable{T}" /> that contains <see cref="System.Linq.Expressions.ParameterExpression" /> objects to use to populate the <see cref="System.Linq.Expressions.LambdaExpression.Parameters" /> collection.</param>
        /// <returns>A <see cref="System.Linq.Expressions.LambdaExpression" /> that has the <see cref="System.Linq.Expressions.LambdaExpression.NodeType" /> property equal to Lambda and the <see cref="System.Linq.Expressions.LambdaExpression.Body" /> and <see cref="System.Linq.Expressions.LambdaExpression.Parameters" /> properties set to the specified values.</returns>
        public static LambdaExpression Lambda(Expression body, IEnumerable<ParameterExpression>? parameters)
        {
            return Lambda(body, null, false, parameters);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.LambdaExpression" /> by first constructing a delegate type from the expression body, a parameter that indicates whether tail call optimization will be applied, and an enumerable collection of parameter expressions. It can be used when the delegate type is not known at compile time.</summary>
        /// <param name="body">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.LambdaExpression.Body" /> property equal to.</param>
        /// <param name="tailCall">A <see cref="bool" /> that indicates if tail call optimization will be applied when compiling the created expression.</param>
        /// <param name="parameters">An <see cref="System.Collections.Generic.IEnumerable{T}" /> that contains <see cref="System.Linq.Expressions.ParameterExpression" /> objects to use to populate the <see cref="System.Linq.Expressions.LambdaExpression.Parameters" /> collection.</param>
        /// <returns>A <see cref="System.Linq.Expressions.LambdaExpression" /> that has the <see cref="System.Linq.Expressions.LambdaExpression.NodeType" /> property equal to Lambda and the <see cref="System.Linq.Expressions.LambdaExpression.Body" /> and <see cref="System.Linq.Expressions.LambdaExpression.Parameters" /> properties set to the specified values.</returns>
        public static LambdaExpression Lambda(Expression body, bool tailCall, IEnumerable<ParameterExpression>? parameters)
        {
            return Lambda(body, null, tailCall, parameters);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.LambdaExpression" /> where the delegate type is known at compile time, with an array of parameter expressions.</summary>
        /// <param name="delegateType">A <see cref="System.Type" /> that represents a delegate signature for the lambda.</param>
        /// <param name="body">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.LambdaExpression.Body" /> property equal to.</param>
        /// <param name="parameters">An array of <see cref="System.Linq.Expressions.ParameterExpression" /> objects to use to populate the <see cref="System.Linq.Expressions.LambdaExpression.Parameters" /> collection.</param>
        /// <returns>An object that represents a lambda expression which has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.Lambda" /> and the <see cref="System.Linq.Expressions.LambdaExpression.Body" /> and <see cref="System.Linq.Expressions.LambdaExpression.Parameters" /> properties set to the specified values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="delegateType" /> or <paramref name="body" /> is <see langword="null" />.
        /// -or-
        /// One or more elements in <paramref name="parameters" /> are <see langword="null" />.</exception>
        /// <exception cref="System.ArgumentException"><paramref name="delegateType" /> does not represent a delegate type.
        /// -or-
        /// <paramref name="body" />.Type represents a type that is not assignable to the return type of the delegate type represented by <paramref name="delegateType" />.
        /// -or-
        /// <paramref name="parameters" /> does not contain the same number of elements as the list of parameters for the delegate type represented by <paramref name="delegateType" />.
        /// -or-
        /// The <see cref="System.Linq.Expressions.Expression.Type" /> property of an element of <paramref name="parameters" /> is not assignable from the type of the corresponding parameter type of the delegate type represented by <paramref name="delegateType" />.</exception>
        /// <remarks>The object that is returned from this function is of type <see cref="System.Linq.Expressions.Expression{T}" />. The <see cref="System.Linq.Expressions.LambdaExpression" /> type is used to represent the returned object because the concrete type of the lambda expression is not known at compile time.
        /// The number of parameters for the delegate type represented by <paramref name="delegateType" /> must equal the length of <paramref name="parameters" />.
        /// The elements of <paramref name="parameters" /> must be reference equal to the parameter expressions in <paramref name="body" />.
        /// The <see cref="O:System.Linq.Expressions.Expression.Type" /> property of the resulting object is equal to <paramref name="delegateType" />. If <paramref name="parameters" /> is <see langword="null" />, the <see cref="O:System.Linq.Expressions.LambdaExpression.Parameters" /> property of the resulting object is an empty collection.</remarks>
        public static LambdaExpression Lambda(Type delegateType, Expression body, params ParameterExpression[]? parameters)
        {
            return Lambda(delegateType, body, null, false, parameters);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.LambdaExpression" /> where the delegate type is known at compile time, with a parameter that indicates whether tail call optimization will be applied, and an array of parameter expressions.</summary>
        /// <param name="delegateType">A <see cref="System.Linq.Expressions.Expression.Type" /> representing the delegate signature for the lambda.</param>
        /// <param name="body">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.LambdaExpression.Body" /> property equal to.</param>
        /// <param name="tailCall">A <see cref="bool" /> that indicates if tail call optimization will be applied when compiling the created expression.</param>
        /// <param name="parameters">An array that contains <see cref="System.Linq.Expressions.ParameterExpression" /> objects to use to populate the <see cref="System.Linq.Expressions.LambdaExpression.Parameters" /> collection.</param>
        /// <returns>A <see cref="System.Linq.Expressions.LambdaExpression" /> that has the <see cref="System.Linq.Expressions.LambdaExpression.NodeType" /> property equal to Lambda and the <see cref="System.Linq.Expressions.LambdaExpression.Body" /> and <see cref="System.Linq.Expressions.LambdaExpression.Parameters" /> properties set to the specified values.</returns>
        public static LambdaExpression Lambda(Type delegateType, Expression body, bool tailCall, params ParameterExpression[]? parameters)
        {
            return Lambda(delegateType, body, null, tailCall, parameters);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.LambdaExpression" /> where the delegate type is known at compile time, with an enumerable collection of parameter expressions.</summary>
        /// <param name="delegateType">A <see cref="System.Type" /> that represents a delegate signature for the lambda.</param>
        /// <param name="body">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.LambdaExpression.Body" /> property equal to.</param>
        /// <param name="parameters">An <see cref="System.Collections.Generic.IEnumerable{T}" /> that contains <see cref="System.Linq.Expressions.ParameterExpression" /> objects to use to populate the <see cref="System.Linq.Expressions.LambdaExpression.Parameters" /> collection.</param>
        /// <returns>An object that represents a lambda expression which has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.Lambda" /> and the <see cref="System.Linq.Expressions.LambdaExpression.Body" /> and <see cref="System.Linq.Expressions.LambdaExpression.Parameters" /> properties set to the specified values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="delegateType" /> or <paramref name="body" /> is <see langword="null" />.
        /// -or-
        /// One or more elements in <paramref name="parameters" /> are <see langword="null" />.</exception>
        /// <exception cref="System.ArgumentException"><paramref name="delegateType" /> does not represent a delegate type.
        /// -or-
        /// <paramref name="body" />.Type represents a type that is not assignable to the return type of the delegate type represented by <paramref name="delegateType" />.
        /// -or-
        /// <paramref name="parameters" /> does not contain the same number of elements as the list of parameters for the delegate type represented by <paramref name="delegateType" />.
        /// -or-
        /// The <see cref="System.Linq.Expressions.Expression.Type" /> property of an element of <paramref name="parameters" /> is not assignable from the type of the corresponding parameter type of the delegate type represented by <paramref name="delegateType" />.</exception>
        /// <remarks>The object that is returned from this function is of type <see cref="System.Linq.Expressions.Expression{T}" />. The <see cref="System.Linq.Expressions.LambdaExpression" /> type is used to represent the returned object because the concrete type of the lambda expression is not known at compile time.
        /// The number of parameters for the delegate type represented by<paramref name="delegateType" /> must equal the length of <paramref name="parameters" />.
        /// The elements of <paramref name="parameters" /> must be reference equal to the parameter expressions in <paramref name="body" />.
        /// The <see cref="O:System.Linq.Expressions.Expression.Type" /> property of the resulting object is equal to <paramref name="delegateType" />. If <paramref name="parameters" /> is <see langword="null" />, the <see cref="O:System.Linq.Expressions.LambdaExpression.Parameters" /> property of the resulting object is an empty collection.</remarks>
        /// <example>The following example demonstrates how to create an expression that represents a lambda expression that adds 1 to the passed argument.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/cs/program.cs" id="Snippet42":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/vb/module1.vb" id="Snippet42":::</example>
        public static LambdaExpression Lambda(Type delegateType, Expression body, IEnumerable<ParameterExpression>? parameters)
        {
            return Lambda(delegateType, body, null, false, parameters);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.LambdaExpression" /> where the delegate type is known at compile time, with a parameter that indicates whether tail call optimization will be applied, and an enumerable collection of parameter expressions.</summary>
        /// <param name="delegateType">A <see cref="System.Linq.Expressions.Expression.Type" /> representing the delegate signature for the lambda.</param>
        /// <param name="body">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.LambdaExpression.Body" /> property equal to.</param>
        /// <param name="tailCall">A <see cref="bool" /> that indicates if tail call optimization will be applied when compiling the created expression.</param>
        /// <param name="parameters">An <see cref="System.Collections.Generic.IEnumerable{T}" /> that contains <see cref="System.Linq.Expressions.ParameterExpression" /> objects to use to populate the <see cref="System.Linq.Expressions.LambdaExpression.Parameters" /> collection.</param>
        /// <returns>A <see cref="System.Linq.Expressions.LambdaExpression" /> that has the <see cref="System.Linq.Expressions.LambdaExpression.NodeType" /> property equal to Lambda and the <see cref="System.Linq.Expressions.LambdaExpression.Body" /> and <see cref="System.Linq.Expressions.LambdaExpression.Parameters" /> properties set to the specified values.</returns>
        public static LambdaExpression Lambda(Type delegateType, Expression body, bool tailCall, IEnumerable<ParameterExpression>? parameters)
        {
            return Lambda(delegateType, body, null, tailCall, parameters);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.LambdaExpression" /> by first constructing a delegate type from the expression body, the name for the lambda, and an enumerable collection of parameter expressions. It can be used when the delegate type is not known at compile time.</summary>
        /// <param name="body">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.LambdaExpression.Body" /> property equal to.</param>
        /// <param name="name">The name for the lambda. Used for emitting debug information.</param>
        /// <param name="parameters">An <see cref="System.Collections.Generic.IEnumerable{T}" /> that contains <see cref="System.Linq.Expressions.ParameterExpression" /> objects to use to populate the <see cref="System.Linq.Expressions.LambdaExpression.Parameters" /> collection.</param>
        /// <returns>A <see cref="System.Linq.Expressions.LambdaExpression" /> that has the <see cref="System.Linq.Expressions.LambdaExpression.NodeType" /> property equal to Lambda and the <see cref="System.Linq.Expressions.LambdaExpression.Body" /> and <see cref="System.Linq.Expressions.LambdaExpression.Parameters" /> properties set to the specified values.</returns>
        public static LambdaExpression Lambda(Expression body, string? name, IEnumerable<ParameterExpression>? parameters)
        {
            return Lambda(body, name, false, parameters);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.LambdaExpression" /> by first constructing a delegate type from the expression body, the name for the lambda, a parameter that indicates whether tail call optimization will be applied, and an enumerable collection of parameter expressions. It can be used when the delegate type is not known at compile time.</summary>
        /// <param name="body">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.LambdaExpression.Body" /> property equal to.</param>
        /// <param name="name">The name for the lambda. Used for emitting debug information.</param>
        /// <param name="tailCall">A <see cref="bool" /> that indicates if tail call optimization will be applied when compiling the created expression.</param>
        /// <param name="parameters">An <see cref="System.Collections.Generic.IEnumerable{T}" /> that contains <see cref="System.Linq.Expressions.ParameterExpression" /> objects to use to populate the <see cref="System.Linq.Expressions.LambdaExpression.Parameters" /> collection.</param>
        /// <returns>A <see cref="System.Linq.Expressions.LambdaExpression" /> that has the <see cref="System.Linq.Expressions.LambdaExpression.NodeType" /> property equal to Lambda and the <see cref="System.Linq.Expressions.LambdaExpression.Body" /> and <see cref="System.Linq.Expressions.LambdaExpression.Parameters" /> properties set to the specified values.</returns>
        public static LambdaExpression Lambda(Expression body, string? name, bool tailCall, IEnumerable<ParameterExpression>? parameters)
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

        /// <summary>Creates a <see cref="System.Linq.Expressions.LambdaExpression" /> where the delegate type is known at compile time, with the name for the lambda, and an enumerable collection of parameter expressions.</summary>
        /// <param name="delegateType">A <see cref="System.Linq.Expressions.Expression.Type" /> representing the delegate signature for the lambda.</param>
        /// <param name="body">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.LambdaExpression.Body" /> property equal to.</param>
        /// <param name="name">The name for the lambda. Used for emitting debug information.</param>
        /// <param name="parameters">An <see cref="System.Collections.Generic.IEnumerable{T}" /> that contains <see cref="System.Linq.Expressions.ParameterExpression" /> objects to use to populate the <see cref="System.Linq.Expressions.LambdaExpression.Parameters" /> collection.</param>
        /// <returns>A <see cref="System.Linq.Expressions.LambdaExpression" /> that has the <see cref="System.Linq.Expressions.LambdaExpression.NodeType" /> property equal to Lambda and the <see cref="System.Linq.Expressions.LambdaExpression.Body" /> and <see cref="System.Linq.Expressions.LambdaExpression.Parameters" /> properties set to the specified values.</returns>
        public static LambdaExpression Lambda(Type delegateType, Expression body, string? name, IEnumerable<ParameterExpression>? parameters)
        {
            ReadOnlyCollection<ParameterExpression> paramList = parameters.ToReadOnly();
            ValidateLambdaArgs(delegateType, ref body, paramList, nameof(delegateType));

            return CreateLambda(delegateType, body, name, false, paramList);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.LambdaExpression" /> where the delegate type is known at compile time, with the name for the lambda, a parameter that indicates whether tail call optimization will be applied, and an enumerable collection of parameter expressions.</summary>
        /// <param name="delegateType">A <see cref="System.Linq.Expressions.Expression.Type" /> representing the delegate signature for the lambda.</param>
        /// <param name="body">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.LambdaExpression.Body" /> property equal to.</param>
        /// <param name="name">The name for the lambda. Used for emitting debug information.</param>
        /// <param name="tailCall">A <see cref="bool" /> that indicates if tail call optimization will be applied when compiling the created expression.</param>
        /// <param name="parameters">An <see cref="System.Collections.Generic.IEnumerable{T}" /> that contains <see cref="System.Linq.Expressions.ParameterExpression" /> objects to use to populate the <see cref="System.Linq.Expressions.LambdaExpression.Parameters" /> collection.</param>
        /// <returns>A <see cref="System.Linq.Expressions.LambdaExpression" /> that has the <see cref="System.Linq.Expressions.LambdaExpression.NodeType" /> property equal to Lambda and the <see cref="System.Linq.Expressions.LambdaExpression.Body" /> and <see cref="System.Linq.Expressions.LambdaExpression.Parameters" /> properties set to the specified values.</returns>
        public static LambdaExpression Lambda(Type delegateType, Expression body, string? name, bool tailCall, IEnumerable<ParameterExpression>? parameters)
        {
            ReadOnlyCollection<ParameterExpression> paramList = parameters.ToReadOnly();
            ValidateLambdaArgs(delegateType, ref body, paramList, nameof(delegateType));

            return CreateLambda(delegateType, body, name, tailCall, paramList);
        }

        private static void ValidateLambdaArgs(Type delegateType, ref Expression body, ReadOnlyCollection<ParameterExpression> parameters, string paramName)
        {
            ContractUtils.RequiresNotNull(delegateType, nameof(delegateType));
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

        /// <summary>Creates a <see cref="System.Linq.Expressions.Expression.Type" /> object that represents a generic <c>System.Func</c> delegate type that has specific type arguments. The last type argument specifies the return type of the created delegate.</summary>
        /// <param name="typeArgs">An array of one to seventeen <see cref="System.Type" /> objects that specify the type arguments for the <see langword="System.Func" /> delegate type.</param>
        /// <returns>The type of a <c>System.Func</c> delegate that has the specified type arguments.</returns>
        /// <exception cref="System.ArgumentException"><paramref name="typeArgs" /> contains fewer than one or more than seventeen elements.</exception>
        /// <exception cref="System.ArgumentNullException"><paramref name="typeArgs" /> is <see langword="null" />.</exception>
        /// <remarks><paramref name="typeArgs" /> must contain at least one and at most seventeen elements.
        /// As an example, if the elements of <paramref name="typeArgs" /> represent the types `T1…Tn`, the resulting <see cref="System.Type" /> object represents the constructed delegate type `System.Func&lt;T1,…,Tn&gt;` in C# or `System.Func(Of T1,…,Tn)` in Visual Basic.</remarks>
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

        /// <summary>Creates a <see cref="System.Linq.Expressions.Expression.Type" /> object that represents a generic System.Func delegate type that has specific type arguments. The last type argument specifies the return type of the created delegate.</summary>
        /// <param name="typeArgs">An array of Type objects that specify the type arguments for the System.Func delegate type.</param>
        /// <param name="funcType">When this method returns, contains the generic System.Func delegate type that has specific type arguments. Contains null if there is no generic System.Func delegate that matches the <paramref name="typeArgs" />. This parameter is passed uninitialized.</param>
        /// <returns><see langword="true" /> if generic System.Func delegate type was created for specific <paramref name="typeArgs" />; otherwise, <see langword="false" />.</returns>
        public static bool TryGetFuncType(Type[] typeArgs, [NotNullWhen(true)] out Type? funcType)
        {
            if (ValidateTryGetFuncActionArgs(typeArgs) == TryGetFuncActionArgsResult.Valid)
            {
                return (funcType = Compiler.DelegateHelpers.GetFuncType(typeArgs)) != null;
            }

            funcType = null;
            return false;
        }

        /// <summary>Creates a <see cref="System.Type" /> object that represents a generic <c>System.Action</c> delegate type that has specific type arguments.</summary>
        /// <param name="typeArgs">An array of up to sixteen <see cref="System.Type" /> objects that specify the type arguments for the <see langword="System.Action" /> delegate type.</param>
        /// <returns>The type of a <c>System.Action</c> delegate that has the specified type arguments.</returns>
        /// <exception cref="System.ArgumentException"><paramref name="typeArgs" /> contains more than sixteen elements.</exception>
        /// <exception cref="System.ArgumentNullException"><paramref name="typeArgs" /> is <see langword="null" />.</exception>
        /// <remarks>As an example, if the elements of <paramref name="typeArgs" /> represent the types `T1…Tn`, the resulting <see cref="System.Type" /> object represents the constructed delegate type `System.Action&lt;T1,…,Tn&gt;` in C# or `System.Action(Of T1,…,Tn)` in Visual Basic.</remarks>
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

        /// <summary>Creates a <see cref="System.Linq.Expressions.Expression.Type" /> object that represents a generic System.Action delegate type that has specific type arguments.</summary>
        /// <param name="typeArgs">An array of Type objects that specify the type arguments for the System.Action delegate type.</param>
        /// <param name="actionType">When this method returns, contains the generic System.Action delegate type that has specific type arguments. Contains null if there is no generic System.Action delegate that matches the <paramref name="typeArgs" />. This parameter is passed uninitialized.</param>
        /// <returns><see langword="true" /> if generic System.Action delegate type was created for specific <paramref name="typeArgs" />; otherwise, <see langword="false" />.</returns>
        public static bool TryGetActionType(Type[] typeArgs, [NotNullWhen(true)] out Type? actionType)
        {
            if (ValidateTryGetFuncActionArgs(typeArgs) == TryGetFuncActionArgsResult.Valid)
            {
                return (actionType = Compiler.DelegateHelpers.GetActionType(typeArgs)) != null;
            }

            actionType = null;
            return false;
        }

        /// <summary>Gets a <see cref="System.Linq.Expressions.Expression.Type" /> object that represents a generic <c>System.Func</c> or <c>System.Action</c> delegate type that has specific type arguments.</summary>
        /// <param name="typeArgs">The type arguments of the delegate.</param>
        /// <returns>The delegate type.</returns>
        /// <remarks>The last type argument determines the return type of the delegate. If no Func or Action is large enough, it will generate a custom delegate type.
        /// As with Func, the last argument is the return type. It can be set to System.Void to produce an Action.</remarks>
        public static Type GetDelegateType(params Type[] typeArgs)
        {
            ContractUtils.RequiresNotEmpty(typeArgs, nameof(typeArgs));
            ContractUtils.RequiresNotNullItems(typeArgs, nameof(typeArgs));
            return Compiler.DelegateHelpers.MakeDelegateType(typeArgs);
        }
    }
}
