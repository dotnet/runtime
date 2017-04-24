// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Xunit;

namespace System.Linq.Tests
{
    public partial class GroupByTests : EnumerableTests
    {
        [Theory]
        [MemberData(nameof(DebuggerAttributesValid_Data))]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Grouping<TKey, TValue> doesn't have a Debugger proxy in the full .NET Framework. See https://github.com/dotnet/corefx/issues/14790.")]
        public void DebuggerAttributesValid<TKey, TElement>(IGrouping<TKey, TElement> grouping, string keyString, TKey dummy1, TElement dummy2)
        {
            // The dummy parameters can be removed once https://github.com/dotnet/buildtools/pull/1300 is brought in.
            Assert.Equal($"Key = {keyString}", DebuggerAttributes.ValidateDebuggerDisplayReferences(grouping));
            
            object proxyObject = DebuggerAttributes.GetProxyObject(grouping);
            
            // Validate proxy fields
            Assert.Empty(DebuggerAttributes.GetDebuggerVisibleFields(proxyObject.GetType()));

            // Validate proxy properties
            IEnumerable<PropertyInfo> properties = DebuggerAttributes.GetDebuggerVisibleProperties(proxyObject.GetType());
            Assert.Equal(2, properties.Count());
            
            // Key
            TKey key = (TKey)properties.Single(property => property.Name == "Key").GetValue(proxyObject);
            Assert.Equal(grouping.Key, key);

            // Values
            PropertyInfo valuesProperty = properties.Single(property => property.Name == "Values");
            Assert.Equal(DebuggerBrowsableState.RootHidden, DebuggerAttributes.GetDebuggerBrowsableState(valuesProperty));
            TElement[] values = (TElement[])valuesProperty.GetValue(proxyObject);
            Assert.IsType<TElement[]>(values); // Arrays can be covariant / of assignment-compatible types
            Assert.Equal(grouping, values);
            Assert.Same(values, valuesProperty.GetValue(proxyObject)); // The result should be cached, as Grouping is immutable.
        }

        public static IEnumerable<object[]> DebuggerAttributesValid_Data()
        {
            IEnumerable<int> source = new[] { 1 };
            yield return new object[] { source.GroupBy(i => i).Single(), "1", 0, 0 };
            yield return new object[] { source.GroupBy(i => i.ToString(), i => i).Single(), @"""1""", string.Empty, 0 };
            yield return new object[] { source.GroupBy(i => TimeSpan.FromSeconds(i), i => i).Single(), "{00:00:01}", TimeSpan.Zero, 0 };

            yield return new object[] { new string[] { null }.GroupBy(x => x).Single(), "null", string.Empty, string.Empty };
            // This test won't even work with the work-around because nullables lose their type once boxed, so xUnit sees an `int` and thinks
            // we're trying to pass an IGrouping<int, int> rather than an IGrouping<int?, int?>.
            // However, it should also be fixed once that PR is brought in, so leaving in this comment.
            // yield return new object[] { new int?[] { null }.GroupBy(x => x).Single(), "null", new int?(0), new int?(0) };
        }
    }
}
