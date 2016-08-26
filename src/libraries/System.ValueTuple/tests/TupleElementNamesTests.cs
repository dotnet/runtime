// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace System.Tests
{
    public class TupleElementNamesAttributeTests
    {
        [Fact]
        public static void Constructor()
        {
            var attribute = new TupleElementNamesAttribute(new string[] { "name1", "name2" });
            Assert.NotNull(attribute.TransformNames);
            Assert.Equal(new string[] { "name1", "name2" }, attribute.TransformNames);

            Assert.Throws<ArgumentNullException>(() => new TupleElementNamesAttribute(null));
        }

        [TupleElementNames(new string[] { null, "name1", "name2" })]
        public object appliedToField = null;

        public static void AppliedToParameter([TupleElementNames(new string[] { "name1", null })] object parameter) { }

        [TupleElementNames(new string[] { null, "name1", "name2" })]
        public static object AppliedToProperty { get; set; }

        [event: TupleElementNames(new[] { null, "name1", "name2" })]
        public static event Func<int> AppliedToEvent;

        [return: TupleElementNames(new[] { null, "name1", "name2" })]
        public static void AppliedToReturn()
        {
            AppliedToEvent();
        }

        [TupleElementNames(new string[] { null, "name1", "name2" })]
        public class AppliedToClass { }

        [TupleElementNames(new string[] { null, "name1", "name2" })]
        public class AppliedToStruct { }
    }
}
