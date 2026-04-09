// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Dynamic.Utils;
using System.Reflection;

namespace System.Linq.Expressions
{
    /// <summary>
    /// Represents assignment to a member of an object.
    /// </summary>
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

        /// <summary>
        /// Gets the <see cref="Expression"/> which represents the object whose member is being assigned to.
        /// </summary>
        public Expression Expression => _expression;

        /// <summary>
        /// Creates a new expression that is like this one, but using the
        /// supplied children. If all of the children are the same, it will
        /// return this expression.
        /// </summary>
        /// <param name="expression">The <see cref="Expression"/> property of the result.</param>
        /// <returns>This expression if no children changed, or an expression with the updated children.</returns>
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

    public partial class Expression
    {
        /// <summary>
        /// Creates a <see cref="MemberAssignment"/> binding the specified value to the given member.
        /// </summary>
        /// <param name="member">The <see cref="MemberInfo"/> for the member which is being assigned to.</param>
        /// <param name="expression">The value to be assigned to <paramref name="member"/>.</param>
        /// <returns>The created <see cref="MemberAssignment"/>.</returns>
        public static MemberAssignment Bind(MemberInfo member, Expression expression)
        {
            ArgumentNullException.ThrowIfNull(member);
            ExpressionUtils.RequiresCanRead(expression, nameof(expression));
            Type memberType;
            ValidateSettableFieldOrPropertyMember(member, out memberType);
            if (!memberType.IsAssignableFrom(expression.Type))
            {
                throw Error.ArgumentTypesMustMatch();
            }
            return new MemberAssignment(member, expression);
        }

        /// <summary>
        /// Creates a <see cref="MemberAssignment"/> binding the specified value to the given property.
        /// </summary>
        /// <param name="propertyAccessor">The <see cref="PropertyInfo"/> for the property which is being assigned to.</param>
        /// <param name="expression">The value to be assigned to <paramref name="propertyAccessor"/>.</param>
        /// <returns>The created <see cref="MemberAssignment"/>.</returns>
        [RequiresUnreferencedCode(PropertyFromAccessorRequiresUnreferencedCode)]
        public static MemberAssignment Bind(MethodInfo propertyAccessor, Expression expression)
        {
            ArgumentNullException.ThrowIfNull(propertyAccessor);
            ArgumentNullException.ThrowIfNull(expression);
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
