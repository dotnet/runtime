// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Reflection.Tests
{
    public class TypeTests
    {
        [Fact]
        public void FilterName_DelegateFiltersExpectedMembers()
        {
            Assert.NotNull(Type.FilterName);
            Assert.Same(Type.FilterName, Type.FilterName);
            Assert.NotSame(Type.FilterName, Type.FilterNameIgnoreCase);

            MethodInfo mi = typeof(TypeTests).GetMethod(nameof(FilterName_DelegateFiltersExpectedMembers));

            Assert.Throws<InvalidFilterCriteriaException>(() => Type.FilterName(mi, null));
            Assert.Throws<InvalidFilterCriteriaException>(() => Type.FilterName(mi, new object()));

            Assert.Empty(typeof(TypeTests).FindMembers(MemberTypes.Method, BindingFlags.Public | BindingFlags.Instance, Type.FilterName, "HelloWorld"));
            Assert.Equal(1, typeof(TypeTests).FindMembers(MemberTypes.Method, BindingFlags.Public | BindingFlags.Instance, Type.FilterName, "FilterName_DelegateFiltersExpectedMembers").Length);
            Assert.Equal(1, typeof(TypeTests).FindMembers(MemberTypes.Method, BindingFlags.Public | BindingFlags.Instance, Type.FilterName, "FilterName_Delegate*").Length);
            Assert.Equal(0, typeof(TypeTests).FindMembers(MemberTypes.Method, BindingFlags.Public | BindingFlags.Instance, Type.FilterName, "filterName_Delegate*").Length);

            Assert.True(Type.FilterName(mi, "FilterName_DelegateFiltersExpectedMembers"));
            Assert.True(Type.FilterName(mi, "     FilterName_DelegateFiltersExpectedMembers   "));
            Assert.True(Type.FilterName(mi, "*"));
            Assert.True(Type.FilterName(mi, "     *   "));
            Assert.True(Type.FilterName(mi, "     Filter*   "));
            Assert.True(Type.FilterName(mi, "FilterName_DelegateFiltersExpectedMembe*"));
            Assert.True(Type.FilterName(mi, "FilterName_DelegateFiltersExpectedMember*"));

            Assert.False(Type.FilterName(mi, "filterName_DelegateFiltersExpectedMembers"));
            Assert.False(Type.FilterName(mi, "FilterName_DelegateFiltersExpectedMemberss"));
            Assert.False(Type.FilterName(mi, "FilterName_DelegateFiltersExpectedMemberss*"));
            Assert.False(Type.FilterName(mi, "FilterName"));
            Assert.False(Type.FilterName(mi, "*FilterName"));
            Assert.False(Type.FilterName(mi, ""));
            Assert.False(Type.FilterName(mi, "     "));
        }

        [Fact]
        public void FilterNameIgnoreCase_DelegateFiltersExpectedMembers()
        {
            Assert.NotNull(Type.FilterNameIgnoreCase);
            Assert.Same(Type.FilterNameIgnoreCase, Type.FilterNameIgnoreCase);
            Assert.NotSame(Type.FilterNameIgnoreCase, Type.FilterName);

            MethodInfo mi = typeof(TypeTests).GetMethod(nameof(FilterNameIgnoreCase_DelegateFiltersExpectedMembers));

            Assert.Throws<InvalidFilterCriteriaException>(() => Type.FilterNameIgnoreCase(mi, null));
            Assert.Throws<InvalidFilterCriteriaException>(() => Type.FilterNameIgnoreCase(mi, new object()));

            Assert.Empty(typeof(TypeTests).FindMembers(MemberTypes.Method, BindingFlags.Public | BindingFlags.Instance, Type.FilterNameIgnoreCase, "HelloWorld"));
            Assert.Equal(1, typeof(TypeTests).FindMembers(MemberTypes.Method, BindingFlags.Public | BindingFlags.Instance, Type.FilterNameIgnoreCase, "FilterNameIgnoreCase_DelegateFiltersExpectedMembers").Length);
            Assert.Equal(1, typeof(TypeTests).FindMembers(MemberTypes.Method, BindingFlags.Public | BindingFlags.Instance, Type.FilterNameIgnoreCase, "FilterNameIgnoreCase_Delegate*").Length);
            Assert.Equal(1, typeof(TypeTests).FindMembers(MemberTypes.Method, BindingFlags.Public | BindingFlags.Instance, Type.FilterNameIgnoreCase, "filterName_Delegate*").Length);

            Assert.True(Type.FilterNameIgnoreCase(mi, "FilterNameIgnoreCase_DelegateFiltersExpectedMembers"));
            Assert.True(Type.FilterNameIgnoreCase(mi, "filternameignorecase_delegatefiltersexpectedmembers"));
            Assert.True(Type.FilterNameIgnoreCase(mi, "     filterNameIgnoreCase_DelegateFiltersexpectedMembers   "));
            Assert.True(Type.FilterNameIgnoreCase(mi, "*"));
            Assert.True(Type.FilterNameIgnoreCase(mi, "     *   "));
            Assert.True(Type.FilterNameIgnoreCase(mi, "     fIlTeR*   "));
            Assert.True(Type.FilterNameIgnoreCase(mi, "FilterNameIgnoreCase_delegateFiltersExpectedMembe*"));
            Assert.True(Type.FilterNameIgnoreCase(mi, "FilterNameIgnoreCase_delegateFiltersExpectedMember*"));

            Assert.False(Type.FilterNameIgnoreCase(mi, "filterName_DelegateFiltersExpectedMembers"));
            Assert.False(Type.FilterNameIgnoreCase(mi, "filterNameIgnoreCase_DelegateFiltersExpectedMemberss"));
            Assert.False(Type.FilterNameIgnoreCase(mi, "FilterNameIgnoreCase_DelegateFiltersExpectedMemberss*"));
            Assert.False(Type.FilterNameIgnoreCase(mi, "filterNameIgnoreCase"));
            Assert.False(Type.FilterNameIgnoreCase(mi, "*FilterNameIgnoreCase"));
            Assert.False(Type.FilterNameIgnoreCase(mi, ""));
            Assert.False(Type.FilterNameIgnoreCase(mi, "     "));
        }
    }
}
