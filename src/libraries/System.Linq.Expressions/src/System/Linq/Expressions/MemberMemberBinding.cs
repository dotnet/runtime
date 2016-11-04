// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic.Utils;
using System.Reflection;

namespace System.Linq.Expressions
{
    /// <summary>
    /// Represents initializing members of a member of a newly created object.
    /// </summary>
    /// <remarks>
    /// Use the <see cref="Expression.MemberBind"/> factory methods to create a <see cref="MemberMemberBinding"/>.
    /// The value of the <see cref="MemberBinding.BindingType"/> property of a <see cref="MemberMemberBinding"/> object is <see cref="MemberBinding"/>. 
    /// </remarks>
    public sealed class MemberMemberBinding : MemberBinding
    {
        internal MemberMemberBinding(MemberInfo member, ReadOnlyCollection<MemberBinding> bindings)
#pragma warning disable 618
            : base(MemberBindingType.MemberBinding, member)
        {
#pragma warning restore 618
            Bindings = bindings;
        }

        /// <summary>
        /// Gets the bindings that describe how to initialize the members of a member. 
        /// </summary>
        public ReadOnlyCollection<MemberBinding> Bindings { get; }

        /// <summary>
        /// Creates a new expression that is like this one, but using the
        /// supplied children. If all of the children are the same, it will
        /// return this expression.
        /// </summary>
        /// <param name="bindings">The <see cref="Bindings"/> property of the result.</param>
        /// <returns>This expression if no children changed, or an expression with the updated children.</returns>
        public MemberMemberBinding Update(IEnumerable<MemberBinding> bindings)
        {
            if (bindings == Bindings)
            {
                return this;
            }
            return Expression.MemberBind(Member, bindings);
        }
    }

    public partial class Expression
    {
        /// <summary>
        /// Creates a <see cref="MemberMemberBinding"/> that represents the recursive initialization of members of a field or property.
        /// </summary>
        /// <param name="member">The <see cref="MemberInfo"/> to set the <see cref="MemberBinding.Member"/> property equal to.</param>
        /// <param name="bindings">An array of <see cref="MemberBinding"/> objects to use to populate the <see cref="MemberMemberBinding.Bindings"/> collection.</param>
        /// <returns>A <see cref="MemberMemberBinding"/> that has the <see cref="MemberBinding.BindingType"/> property equal to <see cref="MemberBinding"/> and the <see cref="MemberBinding.Member"/> and <see cref="MemberMemberBinding.Bindings"/> properties set to the specified values.</returns>
        public static MemberMemberBinding MemberBind(MemberInfo member, params MemberBinding[] bindings)
        {
            return MemberBind(member, (IEnumerable<MemberBinding>)bindings);
        }

        /// <summary>
        /// Creates a <see cref="MemberMemberBinding"/> that represents the recursive initialization of members of a field or property.
        /// </summary>
        /// <param name="member">The <see cref="MemberInfo"/> to set the <see cref="MemberBinding.Member"/> property equal to.</param>
        /// <param name="bindings">An <see cref="IEnumerable{T}"/> that contains <see cref="MemberBinding"/> objects to use to populate the <see cref="MemberMemberBinding.Bindings"/> collection.</param>
        /// <returns>A <see cref="MemberMemberBinding"/> that has the <see cref="MemberBinding.BindingType"/> property equal to <see cref="MemberBinding"/> and the <see cref="MemberBinding.Member"/> and <see cref="MemberMemberBinding.Bindings"/> properties set to the specified values.</returns>
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

        /// <summary>
        /// Creates a <see cref="MemberMemberBinding"/> that represents the recursive initialization of members of a member that is accessed by using a property accessor method.
        /// </summary>
        /// <param name="propertyAccessor">The <see cref="MemberInfo"/> that represents a property accessor method.</param>
        /// <param name="bindings">An <see cref="IEnumerable{T}"/> that contains <see cref="MemberBinding"/> objects to use to populate the <see cref="MemberMemberBinding.Bindings"/> collection.</param>
        /// <returns>
        /// A <see cref="MemberMemberBinding"/> that has the <see cref="MemberBinding.BindingType"/> property equal to <see cref="MemberBinding"/>,
        /// the Member property set to the <see cref="PropertyInfo"/> that represents the property accessed in <paramref name="propertyAccessor"/>,
        /// and <see cref="MemberMemberBinding.Bindings"/> properties set to the specified values.
        /// </returns>
        public static MemberMemberBinding MemberBind(MethodInfo propertyAccessor, params MemberBinding[] bindings)
        {
            return MemberBind(propertyAccessor, (IEnumerable<MemberBinding>)bindings);
        }

        /// <summary>
        /// Creates a <see cref="MemberMemberBinding"/> that represents the recursive initialization of members of a member that is accessed by using a property accessor method.
        /// </summary>
        /// <param name="propertyAccessor">The <see cref="MemberInfo"/> that represents a property accessor method.</param>
        /// <param name="bindings">An <see cref="IEnumerable{T}"/> that contains <see cref="MemberBinding"/> objects to use to populate the <see cref="MemberMemberBinding.Bindings"/> collection.</param>
        /// <returns>
        /// A <see cref="MemberMemberBinding"/> that has the <see cref="MemberBinding.BindingType"/> property equal to <see cref="MemberBinding"/>,
        /// the Member property set to the <see cref="PropertyInfo"/> that represents the property accessed in <paramref name="propertyAccessor"/>,
        /// and <see cref="MemberMemberBinding.Bindings"/> properties set to the specified values.
        /// </returns>
        public static MemberMemberBinding MemberBind(MethodInfo propertyAccessor, IEnumerable<MemberBinding> bindings)
        {
            ContractUtils.RequiresNotNull(propertyAccessor, nameof(propertyAccessor));
            return MemberBind(GetProperty(propertyAccessor, nameof(propertyAccessor)), bindings);
        }

        private static void ValidateGettableFieldOrPropertyMember(MemberInfo member, out Type memberType)
        {
            FieldInfo fi = member as FieldInfo;
            if (fi == null)
            {
                PropertyInfo pi = member as PropertyInfo;
                if (pi == null)
                {
                    throw Error.ArgumentMustBeFieldInfoOrPropertyInfo(nameof(member));
                }
                if (!pi.CanRead)
                {
                    throw Error.PropertyDoesNotHaveGetter(pi, nameof(member));
                }
                memberType = pi.PropertyType;
            }
            else
            {
                memberType = fi.FieldType;
            }
        }

        private static void ValidateMemberInitArgs(Type type, ReadOnlyCollection<MemberBinding> bindings)
        {
            for (int i = 0, n = bindings.Count; i < n; i++)
            {
                MemberBinding b = bindings[i];
                ContractUtils.RequiresNotNull(b, nameof(bindings));
                if (!b.Member.DeclaringType.IsAssignableFrom(type))
                {
                    throw Error.NotAMemberOfType(b.Member.Name, type, nameof(bindings), i);
                }
            }
        }
    }
}
