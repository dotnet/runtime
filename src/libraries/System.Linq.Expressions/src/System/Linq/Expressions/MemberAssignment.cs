// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Dynamic.Utils;
using System.Reflection;

namespace System.Linq.Expressions
{
    /// <summary>Represents assignment operation for a field or property of an object.</summary>
    /// <remarks>Use the <see cref="O:System.Linq.Expressions.Expression.Bind" /> factory methods to create a <see cref="System.Linq.Expressions.MemberAssignment" />.
    /// A <see cref="System.Linq.Expressions.MemberAssignment" /> has the <see cref="O:System.Linq.Expressions.MemberBinding.BindingType" /> property equal to <see cref="System.Linq.Expressions.MemberBindingType.Assignment" />.</remarks>
    public sealed class MemberAssignment : MemberBinding
    {
        private readonly Expression _expression;

        internal MemberAssignment(MemberInfo member, Expression expression)
#pragma warning disable 618
            : base(MemberBindingType.Assignment, member)
        {
#pragma warning restore 618
            _expression = expression;
        }

        /// <summary>Gets the expression to assign to the field or property.</summary>
        /// <value>The <see cref="System.Linq.Expressions.Expression" /> that represents the value to assign to the field or property.</value>
        public Expression Expression => _expression;

        /// <summary>Creates a new expression that is like this one, but using the supplied children. If all of the children are the same, it will return this expression.</summary>
        /// <param name="expression">The <see cref="System.Linq.Expressions.MemberAssignment.Expression" /> property of the result.</param>
        /// <returns>This expression if no children are changed or an expression with the updated children.</returns>
        public MemberAssignment Update(Expression expression)
        {
            if (expression == Expression)
            {
                return this;
            }
            return Expression.Bind(Member, expression);
        }

        internal override void ValidateAsDefinedHere(int index)
        {
        }
    }

    /// <summary>Provides the base class from which the classes that represent expression tree nodes are derived. It also contains <see langword="static" /> (<see langword="Shared" /> in Visual Basic) factory methods to create the various node types. This is an <see langword="abstract" /> class.</summary>
    /// <remarks></remarks>
    /// <example>The following code example shows how to create a block expression. The block expression consists of two <see cref="System.Linq.Expressions.MethodCallExpression" /> objects and one <see cref="System.Linq.Expressions.ConstantExpression" /> object.
    /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/cs/program.cs" id="Snippet13":::
    /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/vb/module1.vb" id="Snippet13":::</example>
    public partial class Expression
    {
        /// <summary>Creates a <see cref="System.Linq.Expressions.MemberAssignment" /> that represents the initialization of a field or property.</summary>
        /// <param name="member">A <see cref="System.Reflection.MemberInfo" /> to set the <see cref="System.Linq.Expressions.MemberBinding.Member" /> property equal to.</param>
        /// <param name="expression">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.MemberAssignment.Expression" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.MemberAssignment" /> that has <see cref="System.Linq.Expressions.MemberBinding.BindingType" /> equal to <see cref="System.Linq.Expressions.MemberBindingType.Assignment" /> and the <see cref="System.Linq.Expressions.MemberBinding.Member" /> and <see cref="System.Linq.Expressions.MemberAssignment.Expression" /> properties set to the specified values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="member" /> or <paramref name="expression" /> is <see langword="null" />.</exception>
        /// <exception cref="System.ArgumentException"><paramref name="member" /> does not represent a field or property.
        /// -or-
        /// The property represented by <paramref name="member" /> does not have a <see langword="set" /> accessor.
        /// -or-
        /// <paramref name="expression" />.Type is not assignable to the type of the field or property that <paramref name="member" /> represents.</exception>
        /// <remarks>The <see cref="O:System.Linq.Expressions.Expression.Type" /> property of <paramref name="expression" /> must be assignable to the type represented by the <see cref="O:System.Reflection.FieldInfo.FieldType" /> or <see cref="O:System.Reflection.PropertyInfo.PropertyType" /> property of <paramref name="member" />.</remarks>
        public static MemberAssignment Bind(MemberInfo member, Expression expression)
        {
            ContractUtils.RequiresNotNull(member, nameof(member));
            ExpressionUtils.RequiresCanRead(expression, nameof(expression));
            Type memberType;
            ValidateSettableFieldOrPropertyMember(member, out memberType);
            if (!memberType.IsAssignableFrom(expression.Type))
            {
                throw Error.ArgumentTypesMustMatch();
            }
            return new MemberAssignment(member, expression);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.MemberAssignment" /> that represents the initialization of a member by using a property accessor method.</summary>
        /// <param name="propertyAccessor">A <see cref="System.Reflection.MethodInfo" /> that represents a property accessor method.</param>
        /// <param name="expression">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.MemberAssignment.Expression" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.MemberAssignment" /> that has the <see cref="System.Linq.Expressions.MemberBinding.BindingType" /> property equal to <see cref="System.Linq.Expressions.MemberBindingType.Assignment" />, the <see cref="System.Linq.Expressions.MemberBinding.Member" /> property set to the <see cref="System.Reflection.PropertyInfo" /> that represents the property accessed in <paramref name="propertyAccessor" />, and the <see cref="System.Linq.Expressions.MemberAssignment.Expression" /> property set to <paramref name="expression" />.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="propertyAccessor" /> or <paramref name="expression" /> is <see langword="null" />.</exception>
        /// <exception cref="System.ArgumentException"><paramref name="propertyAccessor" /> does not represent a property accessor method.
        /// -or-
        /// The property accessed by <paramref name="propertyAccessor" /> does not have a <see langword="set" /> accessor.
        /// -or-
        /// <paramref name="expression" />.Type is not assignable to the type of the field or property that <paramref name="propertyAccessor" /> represents.</exception>
        /// <remarks>The <see cref="O:System.Linq.Expressions.Expression.Type" /> property of <paramref name="expression" /> must be assignable to the type represented by the <see cref="O:System.Reflection.PropertyInfo.PropertyType" /> property of the property accessed in <paramref name="propertyAccessor" />.</remarks>
        [RequiresUnreferencedCode(PropertyFromAccessorRequiresUnreferencedCode)]
        public static MemberAssignment Bind(MethodInfo propertyAccessor, Expression expression)
        {
            ContractUtils.RequiresNotNull(propertyAccessor, nameof(propertyAccessor));
            ContractUtils.RequiresNotNull(expression, nameof(expression));
            ValidateMethodInfo(propertyAccessor, nameof(propertyAccessor));
            return Bind(GetProperty(propertyAccessor, nameof(propertyAccessor)), expression);
        }

        private static void ValidateSettableFieldOrPropertyMember(MemberInfo member, out Type memberType)
        {
            Type? decType = member.DeclaringType;
            if (decType == null)
            {
                throw Error.NotAMemberOfAnyType(member, nameof(member));
            }

            // Null paramName as there are two paths here with different parameter names at the API
            TypeUtils.ValidateType(decType, null);
            switch (member)
            {
                case PropertyInfo pi:
                    if (!pi.CanWrite)
                    {
                        throw Error.PropertyDoesNotHaveSetter(pi, nameof(member));
                    }

                    memberType = pi.PropertyType;
                    break;

                case FieldInfo fi:
                    memberType = fi.FieldType;
                    break;

                default:
                    throw Error.ArgumentMustBeFieldInfoOrPropertyInfo(nameof(member));
            }
        }
    }
}
