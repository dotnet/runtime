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
    /// <summary>Represents initializing members of a member of a newly created object.</summary>
    /// <remarks>Use the <see cref="O:System.Linq.Expressions.Expression.MemberBind" /> factory methods to create a <see cref="System.Linq.Expressions.MemberMemberBinding" />.
    /// The value of the <see cref="O:System.Linq.Expressions.MemberBinding.BindingType" /> property of a <see cref="System.Linq.Expressions.MemberMemberBinding" /> object is <see cref="System.Linq.Expressions.MemberBindingType.MemberBinding" />.</remarks>
    public sealed class MemberMemberBinding : MemberBinding
    {
        internal MemberMemberBinding(MemberInfo member, ReadOnlyCollection<MemberBinding> bindings)
#pragma warning disable 618
            : base(MemberBindingType.MemberBinding, member)
        {
#pragma warning restore 618
            Bindings = bindings;
        }

        /// <summary>Gets the bindings that describe how to initialize the members of a member.</summary>
        /// <value>A <see cref="System.Collections.ObjectModel.ReadOnlyCollection{T}" /> of <see cref="System.Linq.Expressions.MemberBinding" /> objects that describe how to initialize the members of the member.</value>
        public ReadOnlyCollection<MemberBinding> Bindings { get; }

        /// <summary>Creates a new expression that is like this one, but using the supplied children. If all of the children are the same, it will return this expression.</summary>
        /// <param name="bindings">The <see cref="System.Linq.Expressions.MemberMemberBinding.Bindings" /> property of the result.</param>
        /// <returns>This expression if no children are changed or an expression with the updated children.</returns>
        public MemberMemberBinding Update(IEnumerable<MemberBinding> bindings)
        {
            if (bindings != null)
            {
                if (ExpressionUtils.SameElements(ref bindings!, Bindings))
                {
                    return this;
                }
            }

            return Expression.MemberBind(Member, bindings!);
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
        /// <summary>Creates a <see cref="System.Linq.Expressions.MemberMemberBinding" /> that represents the recursive initialization of members of a field or property.</summary>
        /// <param name="member">The <see cref="System.Reflection.MemberInfo" /> to set the <see cref="System.Linq.Expressions.MemberBinding.Member" /> property equal to.</param>
        /// <param name="bindings">An array of <see cref="System.Linq.Expressions.MemberBinding" /> objects to use to populate the <see cref="System.Linq.Expressions.MemberMemberBinding.Bindings" /> collection.</param>
        /// <returns>A <see cref="System.Linq.Expressions.MemberMemberBinding" /> that has the <see cref="System.Linq.Expressions.MemberBinding.BindingType" /> property equal to <see cref="System.Linq.Expressions.MemberBindingType.MemberBinding" /> and the <see cref="System.Linq.Expressions.MemberBinding.Member" /> and <see cref="System.Linq.Expressions.MemberMemberBinding.Bindings" /> properties set to the specified values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="member" /> or <paramref name="bindings" /> is <see langword="null" />.</exception>
        /// <exception cref="System.ArgumentException"><paramref name="member" /> does not represent a field or property.
        /// -or-
        /// The <see cref="System.Linq.Expressions.MemberBinding.Member" /> property of an element of <paramref name="bindings" /> does not represent a member of the type of the field or property that <paramref name="member" /> represents.</exception>
        /// <remarks>The <paramref name="member" /> parameter must represent a field or property.</remarks>
        public static MemberMemberBinding MemberBind(MemberInfo member, params MemberBinding[] bindings)
        {
            return MemberBind(member, (IEnumerable<MemberBinding>)bindings);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.MemberMemberBinding" /> that represents the recursive initialization of members of a field or property.</summary>
        /// <param name="member">The <see cref="System.Reflection.MemberInfo" /> to set the <see cref="System.Linq.Expressions.MemberBinding.Member" /> property equal to.</param>
        /// <param name="bindings">An <see cref="System.Collections.Generic.IEnumerable{T}" /> that contains <see cref="System.Linq.Expressions.MemberBinding" /> objects to use to populate the <see cref="System.Linq.Expressions.MemberMemberBinding.Bindings" /> collection.</param>
        /// <returns>A <see cref="System.Linq.Expressions.MemberMemberBinding" /> that has the <see cref="System.Linq.Expressions.MemberBinding.BindingType" /> property equal to <see cref="System.Linq.Expressions.MemberBindingType.MemberBinding" /> and the <see cref="System.Linq.Expressions.MemberBinding.Member" /> and <see cref="System.Linq.Expressions.MemberMemberBinding.Bindings" /> properties set to the specified values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="member" /> or <paramref name="bindings" /> is <see langword="null" />.</exception>
        /// <exception cref="System.ArgumentException"><paramref name="member" /> does not represent a field or property.
        /// -or-
        /// The <see cref="System.Linq.Expressions.MemberBinding.Member" /> property of an element of <paramref name="bindings" /> does not represent a member of the type of the field or property that <paramref name="member" /> represents.</exception>
        /// <remarks>The <paramref name="member" /> parameter must represent a field or property.</remarks>
        public static MemberMemberBinding MemberBind(MemberInfo member, IEnumerable<MemberBinding> bindings)
        {
            ContractUtils.RequiresNotNull(member, nameof(member));
            ContractUtils.RequiresNotNull(bindings, nameof(bindings));
            ReadOnlyCollection<MemberBinding> roBindings = bindings.ToReadOnly();
            Type memberType;
            ValidateGettableFieldOrPropertyMember(member, out memberType);
            ValidateMemberInitArgs(memberType, roBindings);
            return new MemberMemberBinding(member, roBindings);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.MemberMemberBinding" /> that represents the recursive initialization of members of a member that is accessed by using a property accessor method.</summary>
        /// <param name="propertyAccessor">The <see cref="System.Reflection.MethodInfo" /> that represents a property accessor method.</param>
        /// <param name="bindings">An array of <see cref="System.Linq.Expressions.MemberBinding" /> objects to use to populate the <see cref="System.Linq.Expressions.MemberMemberBinding.Bindings" /> collection.</param>
        /// <returns>A <see cref="System.Linq.Expressions.MemberMemberBinding" /> that has the <see cref="System.Linq.Expressions.MemberBinding.BindingType" /> property equal to <see cref="System.Linq.Expressions.MemberBindingType.MemberBinding" />, the <see cref="System.Linq.Expressions.MemberBinding.Member" /> property set to the <see cref="System.Reflection.PropertyInfo" /> that represents the property accessed in <paramref name="propertyAccessor" />, and <see cref="System.Linq.Expressions.MemberMemberBinding.Bindings" /> properties set to the specified values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="propertyAccessor" /> or <paramref name="bindings" /> is <see langword="null" />.</exception>
        /// <exception cref="System.ArgumentException"><paramref name="propertyAccessor" /> does not represent a property accessor method.
        /// -or-
        /// The <see cref="System.Linq.Expressions.MemberBinding.Member" /> property of an element of <paramref name="bindings" /> does not represent a member of the type of the property accessed by the method that <paramref name="propertyAccessor" /> represents.</exception>
        [RequiresUnreferencedCode(PropertyFromAccessorRequiresUnreferencedCode)]
        public static MemberMemberBinding MemberBind(MethodInfo propertyAccessor, params MemberBinding[] bindings)
        {
            return MemberBind(propertyAccessor, (IEnumerable<MemberBinding>)bindings);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.MemberMemberBinding" /> that represents the recursive initialization of members of a member that is accessed by using a property accessor method.</summary>
        /// <param name="propertyAccessor">The <see cref="System.Reflection.MethodInfo" /> that represents a property accessor method.</param>
        /// <param name="bindings">An <see cref="System.Collections.Generic.IEnumerable{T}" /> that contains <see cref="System.Linq.Expressions.MemberBinding" /> objects to use to populate the <see cref="System.Linq.Expressions.MemberMemberBinding.Bindings" /> collection.</param>
        /// <returns>A <see cref="System.Linq.Expressions.MemberMemberBinding" /> that has the <see cref="System.Linq.Expressions.MemberBinding.BindingType" /> property equal to <see cref="System.Linq.Expressions.MemberBindingType.MemberBinding" />, the <see cref="System.Linq.Expressions.MemberBinding.Member" /> property set to the <see cref="System.Reflection.PropertyInfo" /> that represents the property accessed in <paramref name="propertyAccessor" />, and <see cref="System.Linq.Expressions.MemberMemberBinding.Bindings" /> properties set to the specified values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="propertyAccessor" /> or <paramref name="bindings" /> is <see langword="null" />.</exception>
        /// <exception cref="System.ArgumentException"><paramref name="propertyAccessor" /> does not represent a property accessor method.
        /// -or-
        /// The <see cref="System.Linq.Expressions.MemberBinding.Member" /> property of an element of <paramref name="bindings" /> does not represent a member of the type of the property accessed by the method that <paramref name="propertyAccessor" /> represents.</exception>
        [RequiresUnreferencedCode(PropertyFromAccessorRequiresUnreferencedCode)]
        public static MemberMemberBinding MemberBind(MethodInfo propertyAccessor, IEnumerable<MemberBinding> bindings)
        {
            ContractUtils.RequiresNotNull(propertyAccessor, nameof(propertyAccessor));
            return MemberBind(GetProperty(propertyAccessor, nameof(propertyAccessor)), bindings);
        }

        private static void ValidateGettableFieldOrPropertyMember(MemberInfo member, out Type memberType)
        {
            Type? decType = member.DeclaringType;
            if (decType == null)
            {
                throw Error.NotAMemberOfAnyType(member, nameof(member));
            }

            // Null paramName as there are several paths here with different parameter names at the API
            TypeUtils.ValidateType(decType, null, allowByRef: true, allowPointer: true);
            switch (member)
            {
                case PropertyInfo pi:
                    if (!pi.CanRead)
                    {
                        throw Error.PropertyDoesNotHaveGetter(pi, nameof(member));
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

        private static void ValidateMemberInitArgs(Type type, ReadOnlyCollection<MemberBinding> bindings)
        {
            for (int i = 0, n = bindings.Count; i < n; i++)
            {
                MemberBinding b = bindings[i];
                ContractUtils.RequiresNotNull(b, nameof(bindings));
                b.ValidateAsDefinedHere(i);
                if (!b.Member.DeclaringType!.IsAssignableFrom(type))
                {
                    throw Error.NotAMemberOfType(b.Member.Name, type, nameof(bindings), i);
                }
            }
        }
    }
}
