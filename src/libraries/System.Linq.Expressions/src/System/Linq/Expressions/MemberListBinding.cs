// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic.Utils;
using System.Reflection;

namespace System.Linq.Expressions
{
    /// <summary>
    /// Represents initializing the elements of a collection member of a newly created object.
    /// </summary>
    public sealed class MemberListBinding : MemberBinding
    {
        internal MemberListBinding(MemberInfo member, ReadOnlyCollection<ElementInit> initializers)
#pragma warning disable 618
            : base(MemberBindingType.ListBinding, member)
        {
#pragma warning restore 618
            Initializers = initializers;
        }

        /// <summary>
        /// Gets the element initializers for initializing a collection member of a newly created object.
        /// </summary>
        public ReadOnlyCollection<ElementInit> Initializers { get; }

        /// <summary>
        /// Creates a new expression that is like this one, but using the
        /// supplied children. If all of the children are the same, it will
        /// return this expression.
        /// </summary>
        /// <param name="initializers">The <see cref="Initializers"/> property of the result.</param>
        /// <returns>This expression if no children changed, or an expression with the updated children.</returns>
        public MemberListBinding Update(IEnumerable<ElementInit> initializers)
        {
            if (initializers != null)
            {
                if (ExpressionUtils.SameElements(ref initializers!, Initializers))
                {
                    return this;
                }
            }

            return Expression.ListBind(Member, initializers!);
        }

        internal override void ValidateAsDefinedHere(int index)
        {
        }
    }

    public partial class Expression
    {
        /// <summary>Creates a <see cref="MemberListBinding"/> where the member is a field or property.</summary>
        /// <returns>A <see cref="MemberListBinding"/> that has the <see cref="MemberBinding.BindingType"/> property equal to <see cref="MemberBindingType.ListBinding"/> and the <see cref="MemberBinding.Member"/> and <see cref="MemberListBinding.Initializers"/> properties set to the specified values.</returns>
        /// <param name="member">A <see cref="MemberInfo"/> that represents a field or property to set the <see cref="MemberBinding.Member"/> property equal to.</param>
        /// <param name="initializers">An array of <see cref="System.Linq.Expressions.ElementInit"/> objects to use to populate the <see cref="MemberListBinding.Initializers"/> collection.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="member"/> is null. -or-One or more elements of <paramref name="initializers"/> is null.</exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="member"/> does not represent a field or property.-or-The <see cref="FieldInfo.FieldType"/> or <see cref="PropertyInfo.PropertyType"/> of the field or property that <paramref name="member"/> represents does not implement <see cref="Collections.IEnumerable"/>.</exception>
        public static MemberListBinding ListBind(MemberInfo member, params ElementInit[] initializers)
        {
            return ListBind(member, (IEnumerable<ElementInit>)initializers);
        }

        /// <summary>Creates a <see cref="MemberListBinding"/> where the member is a field or property.</summary>
        /// <returns>A <see cref="MemberListBinding"/> that has the <see cref="MemberBinding.BindingType"/> property equal to <see cref="MemberBindingType.ListBinding"/> and the <see cref="MemberBinding.Member"/> and <see cref="MemberListBinding.Initializers"/> properties set to the specified values.</returns>
        /// <param name="member">A <see cref="MemberInfo"/> that represents a field or property to set the <see cref="MemberBinding.Member"/> property equal to.</param>
        /// <param name="initializers">An <see cref="IEnumerable{T}"/> that contains <see cref="System.Linq.Expressions.ElementInit"/> objects to use to populate the <see cref="MemberListBinding.Initializers"/> collection.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="member"/> is null. -or-One or more elements of <paramref name="initializers"/> is null.</exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="member"/> does not represent a field or property.-or-The <see cref="FieldInfo.FieldType"/> or <see cref="PropertyInfo.PropertyType"/> of the field or property that <paramref name="member"/> represents does not implement <see cref="Collections.IEnumerable"/>.</exception>
        public static MemberListBinding ListBind(MemberInfo member, IEnumerable<ElementInit> initializers)
        {
            ContractUtils.RequiresNotNull(member, nameof(member));
            ContractUtils.RequiresNotNull(initializers, nameof(initializers));
            Type memberType;
            ValidateGettableFieldOrPropertyMember(member, out memberType);
            ReadOnlyCollection<ElementInit> initList = initializers.ToReadOnly();
            ValidateListInitArgs(memberType, initList, nameof(member));
            return new MemberListBinding(member, initList);
        }

        /// <summary>Creates a <see cref="MemberListBinding"/> object based on a specified property accessor method.</summary>
        /// <returns>A <see cref="MemberListBinding"/> that has the <see cref="MemberBinding.BindingType"/> property equal to <see cref="MemberBindingType.ListBinding"/>, the <see cref="MemberBinding.Member"/> property set to the <see cref="MemberInfo"/> that represents the property accessed in <paramref name="propertyAccessor"/>, and <see cref="MemberListBinding.Initializers"/> populated with the elements of <paramref name="initializers"/>.</returns>
        /// <param name="propertyAccessor">A <see cref="MethodInfo"/> that represents a property accessor method.</param>
        /// <param name="initializers">An array of <see cref="Expressions.ElementInit"/> objects to use to populate the <see cref="MemberListBinding.Initializers"/> collection.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="propertyAccessor"/> is null. -or-One or more elements of <paramref name="initializers"/> is null.</exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="propertyAccessor"/> does not represent a property accessor method.-or-The <see cref="PropertyInfo.PropertyType"/> of the property that the method represented by <paramref name="propertyAccessor"/> accesses does not implement <see cref="IEnumerable"/>.</exception>
        [RequiresUnreferencedCode(PropertyFromAccessorRequiresUnreferencedCode)]
        public static MemberListBinding ListBind(MethodInfo propertyAccessor, params ElementInit[] initializers)
        {
            return ListBind(propertyAccessor, (IEnumerable<ElementInit>)initializers);
        }

        /// <summary>Creates a <see cref="MemberListBinding"/> based on a specified property accessor method.</summary>
        /// <returns>A <see cref="MemberListBinding"/> that has the <see cref="MemberBinding.BindingType"/> property equal to <see cref="MemberBindingType.ListBinding"/>, the <see cref="MemberBinding.Member"/> property set to the <see cref="MemberInfo"/> that represents the property accessed in <paramref name="propertyAccessor"/>, and <see cref="MemberListBinding.Initializers"/> populated with the elements of <paramref name="initializers"/>.</returns>
        /// <param name="propertyAccessor">A <see cref="MethodInfo"/> that represents a property accessor method.</param>
        /// <param name="initializers">An <see cref="IEnumerable{T}"/> that contains <see cref="Expressions.ElementInit"/> objects to use to populate the <see cref="MemberListBinding.Initializers"/> collection.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="propertyAccessor"/> is null. -or-One or more elements of <paramref name="initializers"/> are null.</exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="propertyAccessor"/> does not represent a property accessor method.-or-The <see cref="PropertyInfo.PropertyType"/> of the property that the method represented by <paramref name="propertyAccessor"/> accesses does not implement <see cref="IEnumerable"/>.</exception>
        [RequiresUnreferencedCode(PropertyFromAccessorRequiresUnreferencedCode)]
        public static MemberListBinding ListBind(MethodInfo propertyAccessor, IEnumerable<ElementInit> initializers)
        {
            ContractUtils.RequiresNotNull(propertyAccessor, nameof(propertyAccessor));
            ContractUtils.RequiresNotNull(initializers, nameof(initializers));
            return ListBind(GetProperty(propertyAccessor, nameof(propertyAccessor)), initializers);
        }

        private static void ValidateListInitArgs(Type listType, ReadOnlyCollection<ElementInit> initializers, string listTypeParamName)
        {
            if (!typeof(IEnumerable).IsAssignableFrom(listType))
            {
                throw Error.TypeNotIEnumerable(listType, listTypeParamName);
            }
            for (int i = 0, n = initializers.Count; i < n; i++)
            {
                ElementInit element = initializers[i];
                ContractUtils.RequiresNotNull(element, nameof(initializers), i);
                ValidateCallInstanceType(listType, element.AddMethod);
            }
        }
    }
}
