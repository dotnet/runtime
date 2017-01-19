// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            ContractUtils.RequiresNotNull(member, nameof(member));
            RequiresCanRead(expression, nameof(expression));
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
        public static MemberAssignment Bind(MethodInfo propertyAccessor, Expression expression)
        {
            ContractUtils.RequiresNotNull(propertyAccessor, nameof(propertyAccessor));
            ContractUtils.RequiresNotNull(expression, nameof(expression));
            ValidateMethodInfo(propertyAccessor, nameof(propertyAccessor));
            return Bind(GetProperty(propertyAccessor, nameof(propertyAccessor)), expression);
        }

        private static void ValidateSettableFieldOrPropertyMember(MemberInfo member, out Type memberType)
        {
            FieldInfo fi = member as FieldInfo;
            if (fi == null)
            {
                PropertyInfo pi = member as PropertyInfo;
                if (pi == null)
                {
                    throw Error.ArgumentMustBeFieldInfoOrPropertyInfo(nameof(member));
                }
                if (!pi.CanWrite)
                {
                    throw Error.PropertyDoesNotHaveSetter(pi, nameof(member));
                }
                memberType = pi.PropertyType;
            }
            else
            {
                memberType = fi.FieldType;
            }
        }
    }
}
