// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.ComponentModel.DataAnnotations.Tests
{
    public class DeniedValuesAttributeTests : ValidationAttributeTestBase
    {
        protected override IEnumerable<TestCase> InvalidValues()
        {
            var denyAttr = new DeniedValuesAttribute("apple", "banana", "cherry");
            yield return new TestCase(denyAttr, "apple");
            yield return new TestCase(denyAttr, "banana");
            yield return new TestCase(denyAttr, "cherry");

            denyAttr = new DeniedValuesAttribute(0, 1, 1, 2, 3, 5, 8, 13);
            yield return new TestCase(denyAttr, 0);
            yield return new TestCase(denyAttr, 1);
            yield return new TestCase(denyAttr, 3);
            yield return new TestCase(denyAttr, 5);
            yield return new TestCase(denyAttr, 8);
            yield return new TestCase(denyAttr, 13);

            denyAttr = new DeniedValuesAttribute(-1, false, 3.1, "str", null, new object(), new byte[] { 0xff });
            foreach (object? value in denyAttr.Values)
                yield return new TestCase(denyAttr, value);

            foreach (object? value in denyAttr.Values)
                yield return new TestCase(new DeniedValuesAttribute(value), value);

        }

        protected override IEnumerable<TestCase> ValidValues()
        {
            var denyAttr = new DeniedValuesAttribute("apple", "banana", "cherry");
            yield return new TestCase(denyAttr, null);
            yield return new TestCase(denyAttr, "mango");
            yield return new TestCase(denyAttr, 13);
            yield return new TestCase(denyAttr, false);

            denyAttr = new DeniedValuesAttribute(0, 1, 1, 2, 3, 5, 8, 13);
            yield return new TestCase(denyAttr, -1);
            yield return new TestCase(denyAttr, 4);
            yield return new TestCase(denyAttr, 7);
            yield return new TestCase(denyAttr, 10);
            yield return new TestCase(denyAttr, "mango");
            yield return new TestCase(denyAttr, false);

            denyAttr = new DeniedValuesAttribute(-1, false, 3.1, "str", null, new object(), new byte[] { 0xff });
            yield return new TestCase(denyAttr, 0);
            yield return new TestCase(denyAttr, true);
            yield return new TestCase(denyAttr, 3.11);
            yield return new TestCase(denyAttr, "str'");
            yield return new TestCase(denyAttr, new object()); // reference equality
            yield return new TestCase(denyAttr, new byte[] { 0xff }); // reference equality
        }

        [Fact]
        public void Ctor_NullParameter_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new DeniedValuesAttribute(values: null));
        }

        [Theory]
        [MemberData(nameof(Get_Ctor_ValuesPropertyReturnsTheSameArray))]
        public void Ctor_ValuesPropertyReturnsTheSameArray(object?[] inputs)
        {
            var attr = new DeniedValuesAttribute(values: inputs);
            Assert.Same(inputs, attr.Values);
        }

        public static IEnumerable<object[]> Get_Ctor_ValuesPropertyReturnsTheSameArray()
        {
            yield return new object?[][] { new object?[] { null } };
            yield return new object?[][] { new object?[] { 1, 2, 3 } };
            yield return new object?[][] { new object?[] { "apple", "banana", "mango", null } };
            yield return new object?[][] { new object?[] { null, false, 0, -0d, 1.1 } };
        }
    }
}
