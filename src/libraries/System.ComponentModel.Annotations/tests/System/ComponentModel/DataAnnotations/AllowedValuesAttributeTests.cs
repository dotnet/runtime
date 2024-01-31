// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.ComponentModel.DataAnnotations.Tests
{
    public class AllowedValuesAttributeTests : ValidationAttributeTestBase
    {
        protected override IEnumerable<TestCase> ValidValues()
        {
            var allowAttr = new AllowedValuesAttribute("apple", "banana", "cherry");
            yield return new TestCase(allowAttr, "apple");
            yield return new TestCase(allowAttr, "banana");
            yield return new TestCase(allowAttr, "cherry");

            allowAttr = new AllowedValuesAttribute(0, 1, 1, 2, 3, 5, 8, 13);
            yield return new TestCase(allowAttr, 0);
            yield return new TestCase(allowAttr, 1);
            yield return new TestCase(allowAttr, 3);
            yield return new TestCase(allowAttr, 5);
            yield return new TestCase(allowAttr, 8);
            yield return new TestCase(allowAttr, 13);

            allowAttr = new AllowedValuesAttribute(-1, false, 3.1, "str", null, new object(), new byte[] { 0xff });
            foreach (object? value in allowAttr.Values)
                yield return new TestCase(allowAttr, value);

            foreach (object? value in allowAttr.Values)
                yield return new TestCase(new AllowedValuesAttribute(value), value);

        }

        protected override IEnumerable<TestCase> InvalidValues()
        {
            var allowAttr = new AllowedValuesAttribute("apple", "banana", "cherry");
            yield return new TestCase(allowAttr, null);
            yield return new TestCase(allowAttr, "mango");
            yield return new TestCase(allowAttr, 13);
            yield return new TestCase(allowAttr, false);

            allowAttr = new AllowedValuesAttribute(0, 1, 1, 2, 3, 5, 8, 13);
            yield return new TestCase(allowAttr, -1);
            yield return new TestCase(allowAttr, 4);
            yield return new TestCase(allowAttr, 7);
            yield return new TestCase(allowAttr, 10);
            yield return new TestCase(allowAttr, "mango");
            yield return new TestCase(allowAttr, false);

            allowAttr = new AllowedValuesAttribute(-1, false, 3.1, "str", null, new object(), new byte[] { 0xff });
            yield return new TestCase(allowAttr, 0);
            yield return new TestCase(allowAttr, true);
            yield return new TestCase(allowAttr, 3.11);
            yield return new TestCase(allowAttr, "str'");
            yield return new TestCase(allowAttr, new object()); // reference equality
            yield return new TestCase(allowAttr, new byte[] { 0xff }); // reference equality
        }

        [Fact]
        public void Ctor_NullParameter_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new AllowedValuesAttribute(values: null));
        }

        [Theory]
        [MemberData(nameof(Get_Ctor_ValuesPropertyReturnsTheSameArray))]
        public void Ctor_ValuesPropertyReturnsTheSameArray(object?[] inputs)
        {
            var attr = new AllowedValuesAttribute(values: inputs);
            Assert.Same(inputs, attr.Values);
        }

        public static IEnumerable<object[]> Get_Ctor_ValuesPropertyReturnsTheSameArray()
        {
            yield return new object?[][] { [null] };
            yield return new object?[][] { new object?[] { 1, 2, 3 } };
            yield return new object?[][] { new object?[] { "apple", "banana", "mango", null } };
            yield return new object?[][] { new object?[] { null, false, 0, -0d, 1.1 } };
        }
    }
}
