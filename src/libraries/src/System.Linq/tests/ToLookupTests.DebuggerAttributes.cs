// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Xunit;

namespace System.Linq.Tests
{
    public partial class ToLookupTests : EnumerableTests
    {
        [Theory]
        [MemberData(nameof(DebuggerAttributesValid_Data))]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Lookup<T> doesn't have a Debugger proxy in the full .NET framework. See https://github.com/dotnet/corefx/issues/14790.")]
        public void DebuggerAttributesValid<TKey, TElement>(ILookup<TKey, TElement> lookup, TKey dummy1, TElement dummy2)
        {
            // The dummy parameters can be removed once https://github.com/dotnet/buildtools/pull/1300 is brought in.
            Assert.Equal($"Count = {lookup.Count}", DebuggerAttributes.ValidateDebuggerDisplayReferences(lookup));
            
            object proxyObject = DebuggerAttributes.GetProxyObject(lookup);

            // Validate proxy fields
            Assert.Empty(DebuggerAttributes.GetDebuggerVisibleFields(proxyObject.GetType()));

            // Validate proxy properties
            IEnumerable<PropertyInfo> properties = DebuggerAttributes.GetDebuggerVisibleProperties(proxyObject.GetType());
            Assert.Equal(1, properties.Count());

            // Groupings
            PropertyInfo groupingsProperty = properties.Single(property => property.Name == "Groupings");
            Assert.Equal(DebuggerBrowsableState.RootHidden, DebuggerAttributes.GetDebuggerBrowsableState(groupingsProperty));
            var groupings = (IGrouping<TKey, TElement>[])groupingsProperty.GetValue(proxyObject);
            Assert.IsType<IGrouping<TKey, TElement>[]>(groupings); // Arrays can be covariant / of assignment-compatible types

            Assert.All(groupings.Zip(lookup, (l, r) => Tuple.Create(l, r)), tuple =>
            {
                Assert.Same(tuple.Item1, tuple.Item2);
            });

            Assert.Same(groupings, groupingsProperty.GetValue(proxyObject)); // The result should be cached, as Lookup is immutable.
        }

        public static IEnumerable<object[]> DebuggerAttributesValid_Data()
        {
            IEnumerable<int> source = new[] { 1 };
            yield return new object[] { source.ToLookup(i => i), 0, 0 };
            yield return new object[] { source.ToLookup(i => i.ToString(), i => i), string.Empty, 0 };
            yield return new object[] { source.ToLookup(i => TimeSpan.FromSeconds(i), i => i), TimeSpan.Zero, 0 };

            yield return new object[] { new string[] { null }.ToLookup(x => x), string.Empty, string.Empty };
            // This test won't even work with the work-around because nullables lose their type once boxed, so xUnit sees an `int` and thinks
            // we're trying to pass an ILookup<int, int> rather than an ILookup<int?, int?>.
            // However, it should also be fixed once that PR is brought in, so leaving in this comment.
            // yield return new object[] { new int?[] { null }.ToLookup(x => x), new int?(0), new int?(0) };
        }
    }
}
