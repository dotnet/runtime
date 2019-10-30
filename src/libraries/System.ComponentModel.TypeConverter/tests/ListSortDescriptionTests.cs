// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace System.ComponentModel.Tests
{
    public class ListSortDescriptionTests
    {
        public static IEnumerable<object[]> Ctor_PropertyDescriptor_ListSortDirection_TestData()
        {
            yield return new object[] { null, ListSortDirection.Ascending };
            yield return new object[] { new MockPropertyDescriptor(), ListSortDirection.Ascending };
            yield return new object[] { new MockPropertyDescriptor(), ListSortDirection.Ascending };
            yield return new object[] { new MockPropertyDescriptor(), ListSortDirection.Ascending };
        }

        [Theory]
        [MemberData(nameof(Ctor_PropertyDescriptor_ListSortDirection_TestData))]
        public void Ctor_PropertyDescriptor_ListSortDirection(PropertyDescriptor property, ListSortDirection direction)
        {
            var sortDescription = new ListSortDescription(property, direction);
            Assert.Same(property, sortDescription.PropertyDescriptor);
            Assert.Equal(direction, sortDescription.SortDirection);
        }
    }
}
