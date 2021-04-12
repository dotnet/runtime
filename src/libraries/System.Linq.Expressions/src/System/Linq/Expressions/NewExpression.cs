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
    /// <summary>Represents a constructor call.</summary>
    /// <remarks>Use the <see cref="O:System.Linq.Expressions.Expression.New" /> factory methods to create a <see cref="System.Linq.Expressions.NewExpression" />.
    /// The value of the <see cref="O:System.Linq.Expressions.Expression.NodeType" /> property of a <see cref="System.Linq.Expressions.NewExpression" /> object is <see cref="System.Linq.Expressions.ExpressionType.New" />.</remarks>
    /// <example>The following example creates a <see cref="System.Linq.Expressions.NewExpression" /> that represents the construction of a new instance of a dictionary object.
    /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Expressions.Expression/CS/Expression.cs" id="Snippet10":::
    /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Expressions.Expression/VB/Expression.vb" id="Snippet10":::</example>
    [DebuggerTypeProxy(typeof(NewExpressionProxy))]
    public class NewExpression : Expression, IArgumentProvider
    {
        private IReadOnlyList<Expression> _arguments;

        internal NewExpression(ConstructorInfo? constructor, IReadOnlyList<Expression> arguments, ReadOnlyCollection<MemberInfo>? members)
        {
            Constructor = constructor;
            _arguments = arguments;
            Members = members;
        }

        /// <summary>Gets the static type of the expression that this <see cref="System.Linq.Expressions.Expression" /> represents.</summary>
        /// <value>The <see cref="System.Linq.Expressions.NewExpression.Type" /> that represents the static type of the expression.</value>
        public override Type Type => Constructor!.DeclaringType!;

        /// <summary>Returns the node type of this <see cref="System.Linq.Expressions.Expression" />.</summary>
        /// <value>The <see cref="System.Linq.Expressions.ExpressionType" /> that represents this expression.</value>
        public sealed override ExpressionType NodeType => ExpressionType.New;

        /// <summary>Gets the called constructor.</summary>
        /// <value>The <see cref="System.Reflection.ConstructorInfo" /> that represents the called constructor.</value>
        public ConstructorInfo? Constructor { get; }

        /// <summary>Gets the arguments to the constructor.</summary>
        /// <value>A collection of <see cref="System.Linq.Expressions.Expression" /> objects that represent the arguments to the constructor.</value>
        /// <remarks>The <see cref="O:System.Linq.Expressions.NewExpression.Arguments" /> property is an empty collection if the constructor takes no arguments.</remarks>
        public ReadOnlyCollection<Expression> Arguments => ExpressionUtils.ReturnReadOnly(ref _arguments);

        /// <summary>
        /// Gets the argument expression with the specified <paramref name="index"/>.
        /// </summary>
        /// <param name="index">The index of the argument expression to get.</param>
        /// <returns>The expression representing the argument at the specified <paramref name="index"/>.</returns>
        public Expression GetArgument(int index) => _arguments[index];

        /// <summary>
        /// Gets the number of argument expressions of the node.
        /// </summary>
        public int ArgumentCount => _arguments.Count;

        /// <summary>Gets the members that can retrieve the values of the fields that were initialized with constructor arguments.</summary>
        /// <value>A collection of <see cref="System.Reflection.MemberInfo" /> objects that represent the members that can retrieve the values of the fields that were initialized with constructor arguments.</value>
        /// <remarks>The <see cref="O:System.Linq.Expressions.NewExpression.Members" /> property provides a mapping between the constructor arguments and the type members that correspond to those values. In the case of the construction of an anonymous type, this property maps the constructor arguments to the properties that are exposed by the anonymous type. This mapping information is important because the fields that are initialized by the construction of an anonymous type, or the properties that access those fields, are not discoverable through the <see cref="O:System.Linq.Expressions.NewExpression.Constructor" /> or <see cref="O:System.Linq.Expressions.NewExpression.Arguments" /> properties of a <see cref="System.Linq.Expressions.NewExpression" /> node.</remarks>
        public ReadOnlyCollection<MemberInfo>? Members { get; }

        /// <summary>Dispatches to the specific visit method for this node type. For example, <see cref="System.Linq.Expressions.MethodCallExpression" /> calls the <see cref="System.Linq.Expressions.ExpressionVisitor.VisitMethodCall(System.Linq.Expressions.MethodCallExpression)" />.</summary>
        /// <param name="visitor">The visitor to visit this node with.</param>
        /// <returns>The result of visiting this node.</returns>
        /// <remarks>This default implementation for <see cref="System.Linq.Expressions.ExpressionType.Extension" /> nodes calls <see cref="O:System.Linq.Expressions.ExpressionVisitor.VisitExtension" />. Override this method to call into a more specific method on a derived visitor class of the <see cref="System.Linq.Expressions.ExpressionVisitor" /> class. However, it should still support unknown visitors by calling <see cref="O:System.Linq.Expressions.ExpressionVisitor.VisitExtension" />.</remarks>
        protected internal override Expression Accept(ExpressionVisitor visitor)
        {
            return visitor.VisitNew(this);
        }

        /// <summary>Creates a new expression that is like this one, but using the supplied children. If all of the children are the same, it will return this expression.</summary>
        /// <param name="arguments">The <see cref="System.Linq.Expressions.NewExpression.Arguments" /> property of the result.</param>
        /// <returns>This expression if no children are changed or an expression with the updated children.</returns>
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "A NewExpression has already been created. The original creator will get a warning that it is not trim compatible.")]
        public NewExpression Update(IEnumerable<Expression>? arguments)
        {
            if (ExpressionUtils.SameElements(ref arguments, Arguments))
            {
                return this;
            }

            return Members != null ? New(Constructor!, arguments, Members) : New(Constructor!, arguments);
        }
    }

    internal sealed class NewValueTypeExpression : NewExpression
    {
        internal NewValueTypeExpression(Type type, ReadOnlyCollection<Expression> arguments, ReadOnlyCollection<MemberInfo>? members)
            : base(null, arguments, members)
        {
            Type = type;
        }

        public sealed override Type Type { get; }
    }

    /// <summary>Provides the base class from which the classes that represent expression tree nodes are derived. It also contains <see langword="static" /> (<see langword="Shared" /> in Visual Basic) factory methods to create the various node types. This is an <see langword="abstract" /> class.</summary>
    /// <remarks></remarks>
    /// <example>The following code example shows how to create a block expression. The block expression consists of two <see cref="System.Linq.Expressions.MethodCallExpression" /> objects and one <see cref="System.Linq.Expressions.ConstantExpression" /> object.
    /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/cs/program.cs" id="Snippet13":::
    /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/vb/module1.vb" id="Snippet13":::</example>
    public partial class Expression
    {
        /// <summary>Creates a <see cref="System.Linq.Expressions.NewExpression" /> that represents calling the specified constructor that takes no arguments.</summary>
        /// <param name="constructor">The <see cref="System.Reflection.ConstructorInfo" /> to set the <see cref="System.Linq.Expressions.NewExpression.Constructor" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.NewExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.New" /> and the <see cref="System.Linq.Expressions.NewExpression.Constructor" /> property set to the specified value.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="constructor" /> is <see langword="null" />.</exception>
        /// <exception cref="System.ArgumentException">The constructor that <paramref name="constructor" /> represents has at least one parameter.</exception>
        /// <remarks>The <see cref="O:System.Linq.Expressions.NewExpression.Arguments" /> and <see cref="O:System.Linq.Expressions.NewExpression.Members" /> properties of the resulting <see cref="System.Linq.Expressions.NewExpression" /> are empty collections. The <see cref="O:System.Linq.Expressions.Expression.Type" /> property represents the declaring type of the constructor represented by <paramref name="constructor" />.</remarks>
        public static NewExpression New(ConstructorInfo constructor)
        {
            return New(constructor, (IEnumerable<Expression>?)null);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.NewExpression" /> that represents calling the specified constructor with the specified arguments.</summary>
        /// <param name="constructor">The <see cref="System.Reflection.ConstructorInfo" /> to set the <see cref="System.Linq.Expressions.NewExpression.Constructor" /> property equal to.</param>
        /// <param name="arguments">An array of <see cref="System.Linq.Expressions.Expression" /> objects to use to populate the <see cref="System.Linq.Expressions.NewExpression.Arguments" /> collection.</param>
        /// <returns>A <see cref="System.Linq.Expressions.NewExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.New" /> and the <see cref="System.Linq.Expressions.NewExpression.Constructor" /> and <see cref="System.Linq.Expressions.NewExpression.Arguments" /> properties set to the specified values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="constructor" /> is <see langword="null" />.
        /// -or-
        /// An element of <paramref name="arguments" /> is <see langword="null" />.</exception>
        /// <exception cref="System.ArgumentException">The length of <paramref name="arguments" /> does match the number of parameters for the constructor that <paramref name="constructor" /> represents.
        /// -or-
        /// The <see cref="System.Linq.Expressions.Expression.Type" /> property of an element of <paramref name="arguments" /> is not assignable to the type of the corresponding parameter of the constructor that <paramref name="constructor" /> represents.</exception>
        /// <remarks>The <paramref name="arguments" /> parameter must contain the same number of elements as the number of parameters for the constructor represented by <paramref name="constructor" />. If <paramref name="arguments" /> is <see langword="null" />, it is considered empty, and the <see cref="O:System.Linq.Expressions.NewExpression.Arguments" /> property of the resulting <see cref="System.Linq.Expressions.NewExpression" /> is an empty collection.
        /// The <see cref="O:System.Linq.Expressions.Expression.Type" /> property of the resulting <see cref="System.Linq.Expressions.NewExpression" /> represents the declaring type of the constructor represented by <paramref name="constructor" />. The <see cref="O:System.Linq.Expressions.NewExpression.Members" /> property is an empty collection.</remarks>
        public static NewExpression New(ConstructorInfo constructor, params Expression[]? arguments)
        {
            return New(constructor, (IEnumerable<Expression>?)arguments);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.NewExpression" /> that represents calling the specified constructor with the specified arguments.</summary>
        /// <param name="constructor">The <see cref="System.Reflection.ConstructorInfo" /> to set the <see cref="System.Linq.Expressions.NewExpression.Constructor" /> property equal to.</param>
        /// <param name="arguments">An <see cref="System.Collections.Generic.IEnumerable{T}" /> that contains <see cref="System.Linq.Expressions.Expression" /> objects to use to populate the <see cref="System.Linq.Expressions.NewExpression.Arguments" /> collection.</param>
        /// <returns>A <see cref="System.Linq.Expressions.NewExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.New" /> and the <see cref="System.Linq.Expressions.NewExpression.Constructor" /> and <see cref="System.Linq.Expressions.NewExpression.Arguments" /> properties set to the specified values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="constructor" /> is <see langword="null" />.
        /// -or-
        /// An element of <paramref name="arguments" /> is <see langword="null" />.</exception>
        /// <exception cref="System.ArgumentException">The <paramref name="arguments" /> parameter does not contain the same number of elements as the number of parameters for the constructor that <paramref name="constructor" /> represents.
        /// -or-
        /// The <see cref="System.Linq.Expressions.Expression.Type" /> property of an element of <paramref name="arguments" /> is not assignable to the type of the corresponding parameter of the constructor that <paramref name="constructor" /> represents.</exception>
        /// <remarks>The <paramref name="arguments" /> parameter must contain the same number of elements as the number of parameters for the constructor represented by <paramref name="constructor" />. If <paramref name="arguments" /> is <see langword="null" />, it is considered empty, and the <see cref="O:System.Linq.Expressions.NewExpression.Arguments" /> property of the resulting <see cref="System.Linq.Expressions.NewExpression" /> is an empty collection.
        /// The <see cref="O:System.Linq.Expressions.Expression.Type" /> property of the resulting <see cref="System.Linq.Expressions.NewExpression" /> represents the declaring type of the constructor represented by <paramref name="constructor" />. The <see cref="O:System.Linq.Expressions.NewExpression.Members" /> property is an empty collection.</remarks>
        public static NewExpression New(ConstructorInfo constructor, IEnumerable<Expression>? arguments)
        {
            ContractUtils.RequiresNotNull(constructor, nameof(constructor));
            ContractUtils.RequiresNotNull(constructor.DeclaringType!, nameof(constructor) + "." + nameof(constructor.DeclaringType));
            TypeUtils.ValidateType(constructor.DeclaringType!, nameof(constructor), allowByRef: true, allowPointer: true);
            ValidateConstructor(constructor, nameof(constructor));
            ReadOnlyCollection<Expression> argList = arguments.ToReadOnly();
            ValidateArgumentTypes(constructor, ExpressionType.New, ref argList, nameof(constructor));

            return new NewExpression(constructor, argList, null);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.NewExpression" /> that represents calling the specified constructor with the specified arguments. The members that access the constructor initialized fields are specified.</summary>
        /// <param name="constructor">The <see cref="System.Reflection.ConstructorInfo" /> to set the <see cref="System.Linq.Expressions.NewExpression.Constructor" /> property equal to.</param>
        /// <param name="arguments">An <see cref="System.Collections.Generic.IEnumerable{T}" /> that contains <see cref="System.Linq.Expressions.Expression" /> objects to use to populate the <see cref="System.Linq.Expressions.NewExpression.Arguments" /> collection.</param>
        /// <param name="members">An <see cref="System.Collections.Generic.IEnumerable{T}" /> that contains <see cref="System.Reflection.MemberInfo" /> objects to use to populate the <see cref="System.Linq.Expressions.NewExpression.Members" /> collection.</param>
        /// <returns>A <see cref="System.Linq.Expressions.NewExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.New" /> and the <see cref="System.Linq.Expressions.NewExpression.Constructor" />, <see cref="System.Linq.Expressions.NewExpression.Arguments" /> and <see cref="System.Linq.Expressions.NewExpression.Members" /> properties set to the specified values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="constructor" /> is <see langword="null" />.
        /// -or-
        /// An element of <paramref name="arguments" /> is <see langword="null" />.
        /// -or-
        /// An element of <paramref name="members" /> is <see langword="null" />.</exception>
        /// <exception cref="System.ArgumentException">The <paramref name="arguments" /> parameter does not contain the same number of elements as the number of parameters for the constructor that <paramref name="constructor" /> represents.
        /// -or-
        /// The <see cref="System.Linq.Expressions.Expression.Type" /> property of an element of <paramref name="arguments" /> is not assignable to the type of the corresponding parameter of the constructor that <paramref name="constructor" /> represents.
        /// -or-
        /// The <paramref name="members" /> parameter does not have the same number of elements as <paramref name="arguments" />.
        /// -or-
        /// An element of <paramref name="arguments" /> has a <see cref="System.Linq.Expressions.Expression.Type" /> property that represents a type that is not assignable to the type of the member that is represented by the corresponding element of <paramref name="members" />.</exception>
        /// <remarks>The <paramref name="arguments" /> parameter must contain the same number of elements as the number of parameters for the constructor represented by <paramref name="constructor" />. If <paramref name="arguments" /> is <see langword="null" />, it is considered empty, and the <see cref="O:System.Linq.Expressions.NewExpression.Arguments" /> property of the resulting <see cref="System.Linq.Expressions.NewExpression" /> is an empty collection.
        /// If <paramref name="members" /> is <see langword="null" />, the <see cref="O:System.Linq.Expressions.NewExpression.Members" /> property of the resulting <see cref="System.Linq.Expressions.NewExpression" /> is an empty collection. If <paramref name="members" /> is not <see langword="null" />, it must have the same number of elements as <paramref name="arguments" /> and each element must not be <see langword="null" />. Each element of <paramref name="members" /> must be a <see cref="System.Reflection.PropertyInfo" />, <see cref="System.Reflection.FieldInfo" /> or <see cref="System.Reflection.MethodInfo" /> that represents an instance member on the declaring type of the constructor represented by <paramref name="constructor" />. If it represents a property, the property must have a `get` accessor. The corresponding element of <paramref name="arguments" /> for each element of <paramref name="members" /> must have a <see cref="O:System.Linq.Expressions.Expression.Type" /> property that represents a type that is assignable to the type of the member that the <paramref name="members" /> element represents.
        /// The <see cref="O:System.Linq.Expressions.Expression.Type" /> property of the resulting <see cref="System.Linq.Expressions.NewExpression" /> represents the declaring type of the constructor that <paramref name="constructor" /> represents.</remarks>
        [RequiresUnreferencedCode(PropertyFromAccessorRequiresUnreferencedCode)]
        public static NewExpression New(ConstructorInfo constructor, IEnumerable<Expression>? arguments, IEnumerable<MemberInfo>? members)
        {
            ContractUtils.RequiresNotNull(constructor, nameof(constructor));
            ContractUtils.RequiresNotNull(constructor.DeclaringType!, nameof(constructor) + "." + nameof(constructor.DeclaringType));
            TypeUtils.ValidateType(constructor.DeclaringType!, nameof(constructor), allowByRef: true, allowPointer: true);
            ValidateConstructor(constructor, nameof(constructor));
            ReadOnlyCollection<MemberInfo> memberList = members.ToReadOnly();
            ReadOnlyCollection<Expression> argList = arguments.ToReadOnly();
            ValidateNewArgs(constructor, ref argList, ref memberList);
            return new NewExpression(constructor, argList, memberList);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.NewExpression" /> that represents calling the specified constructor with the specified arguments. The members that access the constructor initialized fields are specified as an array.</summary>
        /// <param name="constructor">The <see cref="System.Reflection.ConstructorInfo" /> to set the <see cref="System.Linq.Expressions.NewExpression.Constructor" /> property equal to.</param>
        /// <param name="arguments">An <see cref="System.Collections.Generic.IEnumerable{T}" /> that contains <see cref="System.Linq.Expressions.Expression" /> objects to use to populate the <see cref="System.Linq.Expressions.NewExpression.Arguments" /> collection.</param>
        /// <param name="members">An array of <see cref="System.Reflection.MemberInfo" /> objects to use to populate the <see cref="System.Linq.Expressions.NewExpression.Members" /> collection.</param>
        /// <returns>A <see cref="System.Linq.Expressions.NewExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.New" /> and the <see cref="System.Linq.Expressions.NewExpression.Constructor" />, <see cref="System.Linq.Expressions.NewExpression.Arguments" /> and <see cref="System.Linq.Expressions.NewExpression.Members" /> properties set to the specified values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="constructor" /> is <see langword="null" />.
        /// -or-
        /// An element of <paramref name="arguments" /> is <see langword="null" />.
        /// -or-
        /// An element of <paramref name="members" /> is <see langword="null" />.</exception>
        /// <exception cref="System.ArgumentException">The <paramref name="arguments" /> parameter does not contain the same number of elements as the number of parameters for the constructor that <paramref name="constructor" /> represents.
        /// -or-
        /// The <see cref="System.Linq.Expressions.Expression.Type" /> property of an element of <paramref name="arguments" /> is not assignable to the type of the corresponding parameter of the constructor that <paramref name="constructor" /> represents.
        /// -or-
        /// The <paramref name="members" /> parameter does not have the same number of elements as <paramref name="arguments" />.
        /// -or-
        /// An element of <paramref name="arguments" /> has a <see cref="System.Linq.Expressions.Expression.Type" /> property that represents a type that is not assignable to the type of the member that is represented by the corresponding element of <paramref name="members" />.</exception>
        /// <remarks>The <paramref name="arguments" /> parameter must contain the same number of elements as the number of parameters for the constructor represented by <paramref name="constructor" />. If <paramref name="arguments" /> is <see langword="null" />, it is considered empty, and the <see cref="O:System.Linq.Expressions.NewExpression.Arguments" /> property of the resulting <see cref="System.Linq.Expressions.NewExpression" /> is an empty collection.
        /// If <paramref name="members" /> is <see langword="null" />, the <see cref="O:System.Linq.Expressions.NewExpression.Members" /> property of the resulting <see cref="System.Linq.Expressions.NewExpression" /> is an empty collection. If <paramref name="members" /> is not <see langword="null" />, it must have the same number of elements as <paramref name="arguments" /> and each element must not be <see langword="null" />. Each element of <paramref name="members" /> must be a <see cref="System.Reflection.PropertyInfo" />, <see cref="System.Reflection.FieldInfo" /> or <see cref="System.Reflection.MethodInfo" /> that represents an instance member on the declaring type of the constructor represented by <paramref name="constructor" />. If it represents a property, the property must be able to retrieve the value of the associated field. The corresponding element of <paramref name="arguments" /> for each element of <paramref name="members" /> must have a <see cref="O:System.Linq.Expressions.Expression.Type" /> property that represents a type that is assignable to the type of the member that the <paramref name="members" /> element represents.
        /// The <see cref="O:System.Linq.Expressions.Expression.Type" /> property of the resulting <see cref="System.Linq.Expressions.NewExpression" /> represents the declaring type of the constructor that <paramref name="constructor" /> represents.</remarks>
        [RequiresUnreferencedCode(PropertyFromAccessorRequiresUnreferencedCode)]
        public static NewExpression New(ConstructorInfo constructor, IEnumerable<Expression>? arguments, params MemberInfo[]? members)
        {
            return New(constructor, arguments, (IEnumerable<MemberInfo>?)members);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.NewExpression" /> that represents calling the parameterless constructor of the specified type.</summary>
        /// <param name="type">A <see cref="System.Type" /> that has a constructor that takes no arguments.</param>
        /// <returns>A <see cref="System.Linq.Expressions.NewExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.New" /> and the <see cref="System.Linq.Expressions.NewExpression.Constructor" /> property set to the <see cref="System.Reflection.ConstructorInfo" /> that represents the constructor without parameters for the specified type.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="type" /> is <see langword="null" />.</exception>
        /// <exception cref="System.ArgumentException">The type that <paramref name="type" /> represents does not have a constructor without parameters.</exception>
        /// <remarks>The <paramref name="type" /> parameter must represent a type that has a constructor without parameters.
        /// The <see cref="O:System.Linq.Expressions.NewExpression.Arguments" /> and <see cref="O:System.Linq.Expressions.NewExpression.Members" /> properties of the resulting <see cref="System.Linq.Expressions.NewExpression" /> are empty collections. The <see cref="O:System.Linq.Expressions.Expression.Type" /> property is equal to <paramref name="type" />.</remarks>
        /// <example>The following example demonstrates how to use the <see cref="System.Linq.Expressions.Expression.New(System.Type)" /> method to create a <see cref="System.Linq.Expressions.NewExpression" /> that represents constructing a new instance of a dictionary object by calling the constructor without parameters.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Expressions.Expression/CS/Expression.cs" id="Snippet10":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Expressions.Expression/VB/Expression.vb" id="Snippet10":::</example>
        public static NewExpression New(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] Type type)
        {
            ContractUtils.RequiresNotNull(type, nameof(type));
            if (type == typeof(void))
            {
                throw Error.ArgumentCannotBeOfTypeVoid(nameof(type));
            }
            TypeUtils.ValidateType(type, nameof(type));

            if (!type.IsValueType)
            {
                ConstructorInfo? ci = type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SingleOrDefault(c => c.GetParametersCached().Length == 0);
                if (ci == null)
                {
                    throw Error.TypeMissingDefaultConstructor(type, nameof(type));
                }
                return New(ci);
            }
            return new NewValueTypeExpression(type, EmptyReadOnlyCollection<Expression>.Instance, null);
        }

        [RequiresUnreferencedCode(PropertyFromAccessorRequiresUnreferencedCode)]
        private static void ValidateNewArgs(ConstructorInfo constructor, ref ReadOnlyCollection<Expression> arguments, ref ReadOnlyCollection<MemberInfo> members)
        {
            ParameterInfo[] pis;
            if ((pis = constructor.GetParametersCached()).Length > 0)
            {
                if (arguments.Count != pis.Length)
                {
                    throw Error.IncorrectNumberOfConstructorArguments();
                }
                if (arguments.Count != members.Count)
                {
                    throw Error.IncorrectNumberOfArgumentsForMembers();
                }
                Expression[]? newArguments = null;
                MemberInfo[]? newMembers = null;
                for (int i = 0, n = arguments.Count; i < n; i++)
                {
                    Expression arg = arguments[i];
                    ExpressionUtils.RequiresCanRead(arg, nameof(arguments), i);
                    MemberInfo member = members[i];
                    ContractUtils.RequiresNotNull(member, nameof(members), i);
                    if (!TypeUtils.AreEquivalent(member.DeclaringType, constructor.DeclaringType))
                    {
                        throw Error.ArgumentMemberNotDeclOnType(member.Name, constructor.DeclaringType!.Name, nameof(members), i);
                    }
                    Type memberType;
                    ValidateAnonymousTypeMember(ref member, out memberType, nameof(members), i);
                    if (!TypeUtils.AreReferenceAssignable(memberType, arg.Type))
                    {
                        if (!TryQuote(memberType, ref arg))
                        {
                            throw Error.ArgumentTypeDoesNotMatchMember(arg.Type, memberType, nameof(arguments), i);
                        }
                    }
                    ParameterInfo pi = pis[i];
                    Type pType = pi.ParameterType;
                    if (pType.IsByRef)
                    {
                        pType = pType.GetElementType()!;
                    }
                    if (!TypeUtils.AreReferenceAssignable(pType, arg.Type))
                    {
                        if (!TryQuote(pType, ref arg))
                        {
                            throw Error.ExpressionTypeDoesNotMatchConstructorParameter(arg.Type, pType, nameof(arguments), i);
                        }
                    }
                    if (newArguments == null && arg != arguments[i])
                    {
                        newArguments = new Expression[arguments.Count];
                        for (int j = 0; j < i; j++)
                        {
                            newArguments[j] = arguments[j];
                        }
                    }
                    if (newArguments != null)
                    {
                        newArguments[i] = arg;
                    }

                    if (newMembers == null && member != members[i])
                    {
                        newMembers = new MemberInfo[members.Count];
                        for (int j = 0; j < i; j++)
                        {
                            newMembers[j] = members[j];
                        }
                    }
                    if (newMembers != null)
                    {
                        newMembers[i] = member;
                    }
                }
                if (newArguments != null)
                {
                    arguments = new TrueReadOnlyCollection<Expression>(newArguments);
                }
                if (newMembers != null)
                {
                    members = new TrueReadOnlyCollection<MemberInfo>(newMembers);
                }
            }
            else if (arguments != null && arguments.Count > 0)
            {
                throw Error.IncorrectNumberOfConstructorArguments();
            }
            else if (members != null && members.Count > 0)
            {
                throw Error.IncorrectNumberOfMembersForGivenConstructor();
            }
        }

        [RequiresUnreferencedCode(PropertyFromAccessorRequiresUnreferencedCode)]
        private static void ValidateAnonymousTypeMember(ref MemberInfo member, out Type memberType, string paramName, int index)
        {
            if (member is FieldInfo field)
            {
                if (field.IsStatic)
                {
                    throw Error.ArgumentMustBeInstanceMember(paramName, index);
                }
                memberType = field.FieldType;
                return;
            }

            if (member is PropertyInfo pi)
            {
                if (!pi.CanRead)
                {
                    throw Error.PropertyDoesNotHaveGetter(pi, paramName, index);
                }
                if (pi.GetGetMethod()!.IsStatic)
                {
                    throw Error.ArgumentMustBeInstanceMember(paramName, index);
                }
                memberType = pi.PropertyType;
                return;
            }

            if (member is MethodInfo method)
            {
                if (method.IsStatic)
                {
                    throw Error.ArgumentMustBeInstanceMember(paramName, index);
                }

                PropertyInfo prop = GetProperty(method, paramName, index);
                member = prop;
                memberType = prop.PropertyType;
                return;
            }
            throw Error.ArgumentMustBeFieldInfoOrPropertyInfoOrMethod(paramName, index);
        }

        private static void ValidateConstructor(ConstructorInfo constructor, string paramName)
        {
            if (constructor.IsStatic)
                throw Error.NonStaticConstructorRequired(paramName);
        }
    }
}
