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
    /// <summary>Represents initializing the elements of a collection member of a newly created object.</summary>
    /// <remarks>Use the <see cref="O:System.Linq.Expressions.Expression.ListBind" /> factory methods to create a <see cref="System.Linq.Expressions.MemberListBinding" />.
    /// A <see cref="System.Linq.Expressions.MemberListBinding" /> has the <see cref="O:System.Linq.Expressions.MemberBinding.BindingType" /> property equal to <see cref="System.Linq.Expressions.MemberBindingType.ListBinding" />.</remarks>
    public sealed class MemberListBinding : MemberBinding
    {
        internal MemberListBinding(MemberInfo member, ReadOnlyCollection<ElementInit> initializers)
#pragma warning disable 618
            : base(MemberBindingType.ListBinding, member)
        {
#pragma warning restore 618
            Initializers = initializers;
        }

        /// <summary>Gets the element initializers for initializing a collection member of a newly created object.</summary>
        /// <value>A <see cref="System.Collections.ObjectModel.ReadOnlyCollection{T}" /> of <see cref="System.Linq.Expressions.ElementInit" /> objects to initialize a collection member with.</value>
        public ReadOnlyCollection<ElementInit> Initializers { get; }

        /// <summary>Creates a new expression that is like this one, but using the supplied children. If all of the children are the same, it will return this expression.</summary>
        /// <param name="initializers">The <see cref="System.Linq.Expressions.MemberListBinding.Initializers" /> property of the result.</param>
        /// <returns>This expression if no children are changed or an expression with the updated children.</returns>
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

    /// <summary>Provides the base class from which the classes that represent expression tree nodes are derived. It also contains <see langword="static" /> (<see langword="Shared" /> in Visual Basic) factory methods to create the various node types. This is an <see langword="abstract" /> class.</summary>
    /// <remarks></remarks>
    /// <example>The following code example shows how to create a block expression. The block expression consists of two <see cref="System.Linq.Expressions.MethodCallExpression" /> objects and one <see cref="System.Linq.Expressions.ConstantExpression" /> object.
    /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/cs/program.cs" id="Snippet13":::
    /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/vb/module1.vb" id="Snippet13":::</example>
    public partial class Expression
    {
        /// <summary>Creates a <see cref="System.Linq.Expressions.MemberListBinding" /> where the member is a field or property.</summary>
        /// <param name="member">A <see cref="System.Reflection.MemberInfo" /> that represents a field or property to set the <see cref="System.Linq.Expressions.MemberBinding.Member" /> property equal to.</param>
        /// <param name="initializers">An array of <see cref="System.Linq.Expressions.ElementInit" /> objects to use to populate the <see cref="System.Linq.Expressions.MemberListBinding.Initializers" /> collection.</param>
        /// <returns>A <see cref="System.Linq.Expressions.MemberListBinding" /> that has the <see cref="System.Linq.Expressions.MemberBinding.BindingType" /> property equal to <see cref="System.Linq.Expressions.MemberBindingType.ListBinding" /> and the <see cref="System.Linq.Expressions.MemberBinding.Member" /> and <see cref="System.Linq.Expressions.MemberListBinding.Initializers" /> properties set to the specified values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="member" /> is <see langword="null" />.
        /// -or-
        /// One or more elements of <paramref name="initializers" /> are <see langword="null" />.</exception>
        /// <exception cref="System.ArgumentException"><paramref name="member" /> does not represent a field or property.
        /// -or-
        /// The <see cref="System.Reflection.FieldInfo.FieldType" /> or <see cref="System.Reflection.PropertyInfo.PropertyType" /> of the field or property that <paramref name="member" /> represents does not implement <see cref="System.Collections.IEnumerable" />.</exception>
        public static MemberListBinding ListBind(MemberInfo member, params ElementInit[] initializers)
        {
            return ListBind(member, (IEnumerable<ElementInit>)initializers);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.MemberListBinding" /> where the member is a field or property.</summary>
        /// <param name="member">A <see cref="System.Reflection.MemberInfo" /> that represents a field or property to set the <see cref="System.Linq.Expressions.MemberBinding.Member" /> property equal to.</param>
        /// <param name="initializers">An <see cref="System.Collections.Generic.IEnumerable{T}" /> that contains <see cref="System.Linq.Expressions.ElementInit" /> objects to use to populate the <see cref="System.Linq.Expressions.MemberListBinding.Initializers" /> collection.</param>
        /// <returns>A <see cref="System.Linq.Expressions.MemberListBinding" /> that has the <see cref="System.Linq.Expressions.MemberBinding.BindingType" /> property equal to <see cref="System.Linq.Expressions.MemberBindingType.ListBinding" /> and the <see cref="System.Linq.Expressions.MemberBinding.Member" /> and <see cref="System.Linq.Expressions.MemberListBinding.Initializers" /> properties set to the specified values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="member" /> is <see langword="null" />.
        /// -or-
        /// One or more elements of <paramref name="initializers" /> are <see langword="null" />.</exception>
        /// <exception cref="System.ArgumentException"><paramref name="member" /> does not represent a field or property.
        /// -or-
        /// The <see cref="System.Reflection.FieldInfo.FieldType" /> or <see cref="System.Reflection.PropertyInfo.PropertyType" /> of the field or property that <paramref name="member" /> represents does not implement <see cref="System.Collections.IEnumerable" />.</exception>
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

        /// <summary>Creates a <see cref="System.Linq.Expressions.MemberListBinding" /> object based on a specified property accessor method.</summary>
        /// <param name="propertyAccessor">A <see cref="System.Reflection.MethodInfo" /> that represents a property accessor method.</param>
        /// <param name="initializers">An array of <see cref="System.Linq.Expressions.ElementInit" /> objects to use to populate the <see cref="System.Linq.Expressions.MemberListBinding.Initializers" /> collection.</param>
        /// <returns>A <see cref="System.Linq.Expressions.MemberListBinding" /> that has the <see cref="System.Linq.Expressions.MemberBinding.BindingType" /> property equal to <see cref="System.Linq.Expressions.MemberBindingType.ListBinding" />, the <see cref="System.Linq.Expressions.MemberBinding.Member" /> property set to the <see cref="System.Reflection.MemberInfo" /> that represents the property accessed in <paramref name="propertyAccessor" />, and <see cref="System.Linq.Expressions.MemberListBinding.Initializers" /> populated with the elements of <paramref name="initializers" />.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="propertyAccessor" /> is <see langword="null" />.
        /// -or-
        /// One or more elements of <paramref name="initializers" /> are <see langword="null" />.</exception>
        /// <exception cref="System.ArgumentException"><paramref name="propertyAccessor" /> does not represent a property accessor method.
        /// -or-
        /// The <see cref="System.Reflection.PropertyInfo.PropertyType" /> of the property that the method represented by <paramref name="propertyAccessor" /> accesses does not implement <see cref="System.Collections.IEnumerable" />.</exception>
        [RequiresUnreferencedCode(PropertyFromAccessorRequiresUnreferencedCode)]
        public static MemberListBinding ListBind(MethodInfo propertyAccessor, params ElementInit[] initializers)
        {
            return ListBind(propertyAccessor, (IEnumerable<ElementInit>)initializers);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.MemberListBinding" /> based on a specified property accessor method.</summary>
        /// <param name="propertyAccessor">A <see cref="System.Reflection.MethodInfo" /> that represents a property accessor method.</param>
        /// <param name="initializers">An <see cref="System.Collections.Generic.IEnumerable{T}" /> that contains <see cref="System.Linq.Expressions.ElementInit" /> objects to use to populate the <see cref="System.Linq.Expressions.MemberListBinding.Initializers" /> collection.</param>
        /// <returns>A <see cref="System.Linq.Expressions.MemberListBinding" /> that has the <see cref="System.Linq.Expressions.MemberBinding.BindingType" /> property equal to <see cref="System.Linq.Expressions.MemberBindingType.ListBinding" />, the <see cref="System.Linq.Expressions.MemberBinding.Member" /> property set to the <see cref="System.Reflection.MemberInfo" /> that represents the property accessed in <paramref name="propertyAccessor" />, and <see cref="System.Linq.Expressions.MemberListBinding.Initializers" /> populated with the elements of <paramref name="initializers" />.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="propertyAccessor" /> is <see langword="null" />.
        /// -or-
        /// One or more elements of <paramref name="initializers" /> are <see langword="null" />.</exception>
        /// <exception cref="System.ArgumentException"><paramref name="propertyAccessor" /> does not represent a property accessor method.
        /// -or-
        /// The <see cref="System.Reflection.PropertyInfo.PropertyType" /> of the property that the method represented by <paramref name="propertyAccessor" /> accesses does not implement <see cref="System.Collections.IEnumerable" />.</exception>
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
