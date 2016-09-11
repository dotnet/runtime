// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using Xunit;

namespace System.ComponentModel.DataAnnotations.Tests
{
    public class MinLengthAttributeTests : ValidationAttributeTestBase
    {
        protected override IEnumerable<TestCase> ValidValues()
        {
            yield return new TestCase(new MinLengthAttribute(10), null);
            yield return new TestCase(new MinLengthAttribute(0), "");
            yield return new TestCase(new MinLengthAttribute(12), "OverMinLength");
            yield return new TestCase(new MinLengthAttribute(16), "EqualToMinLength");

            yield return new TestCase(new MinLengthAttribute(0), new int[0]);
            yield return new TestCase(new MinLengthAttribute(12), new int[14]);
            yield return new TestCase(new MinLengthAttribute(16), new string[16]);

            yield return new TestCase(new MinLengthAttribute(0), new Collection<int>(new int[0]));
            yield return new TestCase(new MinLengthAttribute(12), new Collection<int>(new int[14]));
            yield return new TestCase(new MinLengthAttribute(16), new Collection<string>(new string[16]));

            yield return new TestCase(new MinLengthAttribute(0), new List<int>(new int[0]));
            yield return new TestCase(new MinLengthAttribute(12), new List<int>(new int[14]));
            yield return new TestCase(new MinLengthAttribute(16), new List<string>(new string[16]));
        }

        protected override IEnumerable<TestCase> InvalidValues()
        {
            yield return new TestCase(new MinLengthAttribute(15), "UnderMinLength");
            yield return new TestCase(new MinLengthAttribute(15), new byte[14]);
            yield return new TestCase(new MinLengthAttribute(15), new Collection<byte>(new byte[14]));
            yield return new TestCase(new MinLengthAttribute(15), new List<byte>(new byte[14]));

            yield return new TestCase(new MinLengthAttribute(12), new int[3, 3]);
        }
        
        [Theory]
        [InlineData(10)]
        [InlineData(0)]
        [InlineData(-1)]
        public static void Ctor(int length)
        {
            Assert.Equal(length, new MinLengthAttribute(length).Length);
        }

        [Fact]
        public static void GetValidationResult_InvalidLength_ThrowsInvalidOperationException()
        {
            var attribute = new MinLengthAttribute(-1);
            Assert.Throws<InvalidOperationException>(() => attribute.GetValidationResult("Rincewind", new ValidationContext(new object())));
        }

        [Fact]
        public static void GetValidationResult_ValueNotStringOrICollection_ThrowsInvalidCastException()
        {
            Assert.Throws<InvalidCastException>(() => new MinLengthAttribute(0).GetValidationResult(new Random(), new ValidationContext(new object())));
        }

        [Fact]
        public static void GetValidationResult_ValueGenericICollection_ThrowsInvalidCastException()
        {
            Assert.Throws<InvalidCastException>(() => new MinLengthAttribute(0).GetValidationResult(new GenericICollectionClass(), new ValidationContext(new object())));
        }
    }
}
