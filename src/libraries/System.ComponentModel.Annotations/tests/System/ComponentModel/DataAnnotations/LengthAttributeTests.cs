// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Xunit;

namespace System.ComponentModel.DataAnnotations.Tests
{
    public class LengthAttributeTests : ValidationAttributeTestBase
    {
        protected override IEnumerable<TestCase> ValidValues()
        {
            yield return new TestCase(new LengthAttribute(10, 20), null);
            yield return new TestCase(new LengthAttribute(0, 0), "");
            yield return new TestCase(new LengthAttribute(12, 20), "OverMinLength");
            yield return new TestCase(new LengthAttribute(16, 16), "EqualToMinLength");
            yield return new TestCase(new LengthAttribute(12, 16), "EqualToMaxLength");

            yield return new TestCase(new LengthAttribute(0, 0), new int[0]);
            yield return new TestCase(new LengthAttribute(12, 16), new int[14]);
            yield return new TestCase(new LengthAttribute(16, 20), new string[16]);
        }

        public static IEnumerable<object[]> ValidValues_ICollection()
        {
            yield return new object[] { new LengthAttribute(0, 0), new Collection<int>(new int[0]) };
            yield return new object[] { new LengthAttribute(12, 16), new Collection<int>(new int[14]) };
            yield return new object[] { new LengthAttribute(16, 20), new Collection<string>(new string[16]) };

            yield return new object[] { new LengthAttribute(0, 2), new List<int>(new int[0]) };
            yield return new object[] { new LengthAttribute(12, 16), new List<int>(new int[14]) };
            yield return new object[] { new LengthAttribute(16, 16), new List<string>(new string[16]) };

            //ICollection<T> but not ICollection
            yield return new object[] { new LengthAttribute(0, 5), new HashSet<int>() };
            yield return new object[] { new LengthAttribute(12, 14), new HashSet<int>(Enumerable.Range(1, 14)) };
            yield return new object[] { new LengthAttribute(16, 20), new HashSet<string>(Enumerable.Range(1, 16).Select(i => i.ToString())) };

            //ICollection but not ICollection<T>
            yield return new object[] { new LengthAttribute(0, 1), new ArrayList(new int[0]) };
            yield return new object[] { new LengthAttribute(12, 16), new ArrayList(new int[14]) };
            yield return new object[] { new LengthAttribute(16, 16), new ArrayList(new string[16]) };

            //Multi ICollection<T>
            yield return new object[] { new LengthAttribute(0, 0), new MultiCollection() };
        }

        protected override IEnumerable<TestCase> InvalidValues()
        {
            yield return new TestCase(new LengthAttribute(15, 20), "UnderMinLength");
            yield return new TestCase(new LengthAttribute(10, 12), "OverMaxLength");
            yield return new TestCase(new LengthAttribute(15, 20), new byte[14]);
            yield return new TestCase(new LengthAttribute(15, 20), new byte[21]);

            yield return new TestCase(new LengthAttribute(12, 20), new int[3, 3]);
            yield return new TestCase(new LengthAttribute(12, 20), new int[3, 7]);
        }

        public static IEnumerable<object[]> InvalidValues_ICollection()
        {
            yield return new object[] { new LengthAttribute(15, 20), new Collection<byte>(new byte[14]) };
            yield return new object[] { new LengthAttribute(15, 20), new Collection<byte>(new byte[21]) };
            yield return new object[] { new LengthAttribute(15, 20), new List<byte>(new byte[14]) };
            yield return new object[] { new LengthAttribute(15, 20), new List<byte>(new byte[21]) };
        }

        [Theory]
        [InlineData(-2, -3)]
        [InlineData(21, 1)]
        [InlineData(128, -1)]
        [InlineData(-1, 12)]
        [InlineData(0, 0)]
        [InlineData(0, 10)]
        public void Ctor(int minimumLength, int maximumLength)
        {
            var attr = new LengthAttribute(minimumLength, maximumLength);
            Assert.Equal(minimumLength, attr.MinimumLength);
            Assert.Equal(maximumLength, attr.MaximumLength);
        }

        [Theory]
        [MemberData(nameof(ValidValues_ICollection))]
        public void Validate_ICollection_Valid(LengthAttribute attribute, object value)
        {
            attribute.Validate(value, new ValidationContext(new object()));
            Assert.True(attribute.IsValid(value));
        }

        [Theory]
        [MemberData(nameof(InvalidValues_ICollection))]
        public void Validate_ICollection_Invalid(LengthAttribute attribute, object value)
        {
            Assert.Throws<ValidationException>(() => attribute.Validate(value, new ValidationContext(new object())));
            Assert.False(attribute.IsValid(value));
        }

        [Theory]
        [InlineData(-1, 0)]
        [InlineData(0, -1)]
        [InlineData(10, 5)]
        public void GetValidationResult_InvalidLength_ThrowsInvalidOperationException(int minimumLength, int maximumLength)
        {
            var attribute = new LengthAttribute(minimumLength, maximumLength);
            Assert.Throws<InvalidOperationException>(() => attribute.GetValidationResult("Rincewind", new ValidationContext(new object())));
        }

        [Fact]
        public void GetValidationResult_ValueNotStringOrICollection_ThrowsInvalidCastException()
        {
            Assert.Throws<InvalidCastException>(() => new LengthAttribute(0, 0).GetValidationResult(new Random(), new ValidationContext(new object())));
        }

        [Fact]
        public void GetValidationResult_ValueGenericIEnumerable_ThrowsInvalidCastException()
        {
            Assert.Throws<InvalidCastException>(() => new LengthAttribute(0, 0).GetValidationResult(new GenericIEnumerableClass(), new ValidationContext(new object())));
        }
    }
}
