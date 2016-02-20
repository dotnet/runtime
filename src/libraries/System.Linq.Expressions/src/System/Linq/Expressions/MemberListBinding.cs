// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic.Utils;
using System.Reflection;

namespace System.Linq.Expressions
{
    /// <summary>
    /// Represents initializing the elements of a collection member of a newly created object. 
    /// </summary>
    public sealed class MemberListBinding : MemberBinding
    {
        private ReadOnlyCollection<ElementInit> _initializers;
        internal MemberListBinding(MemberInfo member, ReadOnlyCollection<ElementInit> initializers)
#pragma warning disable 618
            : base(MemberBindingType.ListBinding, member)
        {
#pragma warning restore 618
            _initializers = initializers;
        }

        /// <summary>
        /// Gets the element initializers for initializing a collection member of a newly created object.
        /// </summary>
        public ReadOnlyCollection<ElementInit> Initializers
        {
            get { return _initializers; }
        }

        /// <summary>
        /// Creates a new expression that is like this one, but using the
        /// supplied children. If all of the children are the same, it will
        /// return this expression.
        /// </summary>
        /// <param name="initializers">The <see cref="Initializers" /> property of the result.</param>
        /// <returns>This expression if no children changed, or an expression with the updated children.</returns>
        public MemberListBinding Update(IEnumerable<ElementInit> initializers)
        {
            if (initializers == Initializers)
            {
                return this;
            }
            return Expression.ListBind(Member, initializers);
        }
    }


    public partial class Expression
    {
        ///<summary>Creates a <see cref="T:System.Linq.Expressions.MemberListBinding" /> where the member is a field or property.</summary>
        ///<returns>A <see cref="T:System.Linq.Expressions.MemberListBinding" /> that has the <see cref="P:System.Linq.Expressions.MemberBinding.BindingType" /> property equal to <see cref="F:System.Linq.Expressions.MemberBindingType.ListBinding" /> and the <see cref="P:System.Linq.Expressions.MemberBinding.Member" /> and <see cref="P:System.Linq.Expressions.MemberListBinding.Initializers" /> properties set to the specified values.</returns>
        ///<param name="member">A <see cref="T:System.Reflection.MemberInfo" /> that represents a field or property to set the <see cref="P:System.Linq.Expressions.MemberBinding.Member" /> property equal to.</param>
        ///<param name="initializers">An array of <see cref="T:System.Linq.Expressions.ElementInit" /> objects to use to populate the <see cref="P:System.Linq.Expressions.MemberListBinding.Initializers" /> collection.</param>
        ///<exception cref="T:System.ArgumentNullException">
        ///<paramref name="member" /> is null. -or-One or more elements of <paramref name="initializers" /> is null.</exception>
        ///<exception cref="T:System.ArgumentException">
        ///<paramref name="member" /> does not represent a field or property.-or-The <see cref="P:System.Reflection.FieldInfo.FieldType" /> or <see cref="P:System.Reflection.PropertyInfo.PropertyType" /> of the field or property that <paramref name="member" /> represents does not implement <see cref="T:System.Collections.IEnumerable" />.</exception>
        public static MemberListBinding ListBind(MemberInfo member, params ElementInit[] initializers)
        {
            return ListBind(member, (IEnumerable<ElementInit>)initializers);
        }

        ///<summary>Creates a <see cref="T:System.Linq.Expressions.MemberListBinding" /> where the member is a field or property.</summary>
        ///<returns>A <see cref="T:System.Linq.Expressions.MemberListBinding" /> that has the <see cref="P:System.Linq.Expressions.MemberBinding.BindingType" /> property equal to <see cref="F:System.Linq.Expressions.MemberBindingType.ListBinding" /> and the <see cref="P:System.Linq.Expressions.MemberBinding.Member" /> and <see cref="P:System.Linq.Expressions.MemberListBinding.Initializers" /> properties set to the specified values.</returns>
        ///<param name="member">A <see cref="T:System.Reflection.MemberInfo" /> that represents a field or property to set the <see cref="P:System.Linq.Expressions.MemberBinding.Member" /> property equal to.</param>
        ///<param name="initializers">An <see cref="T:System.Collections.Generic.IEnumerable`1" /> that contains <see cref="T:System.Linq.Expressions.ElementInit" /> objects to use to populate the <see cref="P:System.Linq.Expressions.MemberListBinding.Initializers" /> collection.</param>
        ///<exception cref="T:System.ArgumentNullException">
        ///<paramref name="member" /> is null. -or-One or more elements of <paramref name="initializers" /> is null.</exception>
        ///<exception cref="T:System.ArgumentException">
        ///<paramref name="member" /> does not represent a field or property.-or-The <see cref="P:System.Reflection.FieldInfo.FieldType" /> or <see cref="P:System.Reflection.PropertyInfo.PropertyType" /> of the field or property that <paramref name="member" /> represents does not implement <see cref="T:System.Collections.IEnumerable" />.</exception>
        public static MemberListBinding ListBind(MemberInfo member, IEnumerable<ElementInit> initializers)
        {
            ContractUtils.RequiresNotNull(member, nameof(member));
            ContractUtils.RequiresNotNull(initializers, nameof(initializers));
            Type memberType;
            ValidateGettableFieldOrPropertyMember(member, out memberType);
            var initList = initializers.ToReadOnly();
            ValidateListInitArgs(memberType, initList);
            return new MemberListBinding(member, initList);
        }

        ///<summary>Creates a <see cref="T:System.Linq.Expressions.MemberListBinding" /> object based on a specified property accessor method.</summary>
        ///<returns>A <see cref="T:System.Linq.Expressions.MemberListBinding" /> that has the <see cref="P:System.Linq.Expressions.MemberBinding.BindingType" /> property equal to <see cref="F:System.Linq.Expressions.MemberBindingType.ListBinding" />, the <see cref="P:System.Linq.Expressions.MemberBinding.Member" /> property set to the <see cref="T:System.Reflection.MemberInfo" /> that represents the property accessed in <paramref name="propertyAccessor" />, and <see cref="P:System.Linq.Expressions.MemberListBinding.Initializers" /> populated with the elements of <paramref name="initializers" />.</returns>
        ///<param name="propertyAccessor">A <see cref="T:System.Reflection.MethodInfo" /> that represents a property accessor method.</param>
        ///<param name="initializers">An array of <see cref="T:System.Linq.Expressions.ElementInit" /> objects to use to populate the <see cref="P:System.Linq.Expressions.MemberListBinding.Initializers" /> collection.</param>
        ///<exception cref="T:System.ArgumentNullException">
        ///<paramref name="propertyAccessor" /> is null. -or-One or more elements of <paramref name="initializers" /> is null.</exception>
        ///<exception cref="T:System.ArgumentException">
        ///<paramref name="propertyAccessor" /> does not represent a property accessor method.-or-The <see cref="P:System.Reflection.PropertyInfo.PropertyType" /> of the property that the method represented by <paramref name="propertyAccessor" /> accesses does not implement <see cref="T:System.Collections.IEnumerable" />.</exception>  
        public static MemberListBinding ListBind(MethodInfo propertyAccessor, params ElementInit[] initializers)
        {
            return ListBind(propertyAccessor, (IEnumerable<ElementInit>)initializers);
        }

        ///<summary>Creates a <see cref="T:System.Linq.Expressions.MemberListBinding" /> based on a specified property accessor method.</summary>
        ///<returns>A <see cref="T:System.Linq.Expressions.MemberListBinding" /> that has the <see cref="P:System.Linq.Expressions.MemberBinding.BindingType" /> property equal to <see cref="F:System.Linq.Expressions.MemberBindingType.ListBinding" />, the <see cref="P:System.Linq.Expressions.MemberBinding.Member" /> property set to the <see cref="T:System.Reflection.MemberInfo" /> that represents the property accessed in <paramref name="propertyAccessor" />, and <see cref="P:System.Linq.Expressions.MemberListBinding.Initializers" /> populated with the elements of <paramref name="initializers" />.</returns>
        ///<param name="propertyAccessor">A <see cref="T:System.Reflection.MethodInfo" /> that represents a property accessor method.</param>
        ///<param name="initializers">An <see cref="T:System.Collections.Generic.IEnumerable`1" /> that contains <see cref="T:System.Linq.Expressions.ElementInit" /> objects to use to populate the <see cref="P:System.Linq.Expressions.MemberListBinding.Initializers" /> collection.</param>
        ///<exception cref="T:System.ArgumentNullException">
        ///<paramref name="propertyAccessor" /> is null. -or-One or more elements of <paramref name="initializers" /> are null.</exception>
        ///<exception cref="T:System.ArgumentException">
        ///<paramref name="propertyAccessor" /> does not represent a property accessor method.-or-The <see cref="P:System.Reflection.PropertyInfo.PropertyType" /> of the property that the method represented by <paramref name="propertyAccessor" /> accesses does not implement <see cref="T:System.Collections.IEnumerable" />.</exception>        
        public static MemberListBinding ListBind(MethodInfo propertyAccessor, IEnumerable<ElementInit> initializers)
        {
            ContractUtils.RequiresNotNull(propertyAccessor, nameof(propertyAccessor));
            ContractUtils.RequiresNotNull(initializers, nameof(initializers));
            return ListBind(GetProperty(propertyAccessor), initializers);
        }

        private static void ValidateListInitArgs(Type listType, ReadOnlyCollection<ElementInit> initializers)
        {
            if (!typeof(IEnumerable).IsAssignableFrom(listType))
            {
                throw Error.TypeNotIEnumerable(listType);
            }
            for (int i = 0, n = initializers.Count; i < n; i++)
            {
                ElementInit element = initializers[i];
                ContractUtils.RequiresNotNull(element, nameof(initializers));
                ValidateCallInstanceType(listType, element.AddMethod);
            }
        }
    }
}
