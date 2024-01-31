// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.ComponentModel.DataAnnotations.Tests
{
    public class NotAllowedValuesAttributeTests : ValidationAttributeTestBase
    {
        protected override IEnumerable<TestCase> ValidValues()
        {
            var notAllowAttr = new NotAllowedValuesAttribute("apple", "banana", "cherry");
            yield return new TestCase(notAllowAttr, null);
            yield return new TestCase(notAllowAttr, "mango");
            yield return new TestCase(notAllowAttr, 13);
            yield return new TestCase(notAllowAttr, false);

            notAllowAttr = new NotAllowedValuesAttribute(0, 1, 1, 2, 3, 5, 8, 13);
            yield return new TestCase(notAllowAttr, -1);
            yield return new TestCase(notAllowAttr, 4);
            yield return new TestCase(notAllowAttr, 7);
            yield return new TestCase(notAllowAttr, 10);
            yield return new TestCase(notAllowAttr, "mango");
            yield return new TestCase(notAllowAttr, false);

            notAllowAttr = new NotAllowedValuesAttribute(-1, false, 3.1, "str", null, new object(), new byte[] { 0xff });
            yield return new TestCase(notAllowAttr, 0);
            yield return new TestCase(notAllowAttr, true);
            yield return new TestCase(notAllowAttr, 3.11);
            yield return new TestCase(notAllowAttr, "str'");
            yield return new TestCase(notAllowAttr, new object()); // reference equality
            yield return new TestCase(notAllowAttr, new byte[] { 0xff }); // reference equality
        }

        protected override IEnumerable<TestCase> InvalidValues()
        {
            var notAllowAttr = new NotAllowedValuesAttribute("apple", "banana", "cherry");
            yield return new TestCase(notAllowAttr, "apple");
            yield return new TestCase(notAllowAttr, "banana");
            yield return new TestCase(notAllowAttr, "cherry");

            notAllowAttr = new NotAllowedValuesAttribute(0, 1, 1, 2, 3, 5, 8, 13);
            yield return new TestCase(notAllowAttr, 0);
            yield return new TestCase(notAllowAttr, 1);
            yield return new TestCase(notAllowAttr, 3);
            yield return new TestCase(notAllowAttr, 5);
            yield return new TestCase(notAllowAttr, 8);
            yield return new TestCase(notAllowAttr, 13);

            notAllowAttr = new NotAllowedValuesAttribute(-1, false, 3.1, "str", null, new object(), new byte[] { 0xff });
            foreach (object? value in notAllowAttr.Values)
                yield return new TestCase(notAllowAttr, value);

            foreach (object? value in notAllowAttr.Values)
                yield return new TestCase(new NotAllowedValuesAttribute(value), value);
        }

        [Fact]
        public void Ctor_NullParameter_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new NotAllowedValuesAttribute(values: null));
        }

        [Theory]
        [MemberData(nameof(Get_Ctor_ValuesPropertyReturnsTheSameArray))]
        public void Ctor_ValuesPropertyReturnsTheSameArray(object?[] inputs)
        {
            var attr = new NotAllowedValuesAttribute(values: inputs);
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
