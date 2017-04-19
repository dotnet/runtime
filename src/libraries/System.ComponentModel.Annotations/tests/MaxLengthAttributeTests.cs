// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Xunit;

namespace System.ComponentModel.DataAnnotations.Tests
{
    public class MaxLengthAttributeTests : ValidationAttributeTestBase
    {
        protected override IEnumerable<TestCase> ValidValues()
        {
            yield return new TestCase(new MaxLengthAttribute(10), null);
            yield return new TestCase(new MaxLengthAttribute(15), "UnderMaxLength");
            yield return new TestCase(new MaxLengthAttribute(16), "EqualToMaxLength");
            yield return new TestCase(new MaxLengthAttribute(-1), "SpecifiedMaximumMaxLength");
            yield return new TestCase(new MaxLengthAttribute(-1), new int[20]);
            yield return new TestCase(new MaxLengthAttribute(15), new string[14]);
            yield return new TestCase(new MaxLengthAttribute(16), new string[16]);

            yield return new TestCase(new MaxLengthAttribute(16), new int[4, 4]);
            yield return new TestCase(new MaxLengthAttribute(16), new string[3, 4]);
        }

        protected static IEnumerable<object[]> ValidValues_ICollection()
        {
            yield return new object[] { new MaxLengthAttribute(-1), new Collection<int>(new int[20]) };
            yield return new object[] { new MaxLengthAttribute(15), new Collection<string>(new string[14]) };
            yield return new object[] { new MaxLengthAttribute(16), new Collection<string>(new string[16]) };
            yield return new object[] { new MaxLengthAttribute(-1), new List<int>(new int[20]) };
            yield return new object[] { new MaxLengthAttribute(15), new List<string>(new string[14]) };
            yield return new object[] { new MaxLengthAttribute(16), new List<string>(new string[16]) };
        }

        protected override IEnumerable<TestCase> InvalidValues()
        {
            yield return new TestCase(new MaxLengthAttribute(12), "OverMaxLength");
            yield return new TestCase(new MaxLengthAttribute(12), new byte[13]);

            yield return new TestCase(new MaxLengthAttribute(12), new int[4, 4]);
        }

        protected static IEnumerable<object> InvalidValues_ICollection()
        {
            yield return new object[] { new MaxLengthAttribute(12), new Collection<byte>(new byte[13]) };
            yield return new object[] { new MaxLengthAttribute(12), new List<byte>(new byte[13]) };
        }

        [Fact]
        public static void Ctor()
        {
            Assert.Equal(-1, new MaxLengthAttribute().Length);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(10)]
        [InlineData(0)]
        [InlineData(-10)]
        public static void Ctor_Int(int length)
        {
            Assert.Equal(length, new MaxLengthAttribute(length).Length);
        }

        [Theory]
        [MemberData(nameof(ValidValues_ICollection))]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "MaxLengthAttribute in the .NET Framework doesn't support ICollection.Count. See https://github.com/dotnet/corefx/issues/18361")]
        public void Validate_ICollection_NetCore_Valid(MaxLengthAttribute attribute, object value)
        {
            attribute.Validate(value, new ValidationContext(new object()));
            Assert.True(attribute.IsValid(value));
        }

        [Theory]
        [MemberData(nameof(InvalidValues_ICollection))]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "MaxLengthAttribute in the .NET Framework doesn't support ICollection.Count. See https://github.com/dotnet/corefx/issues/18361")]
        public void Validate_ICollection_NetCore_Invalid(MaxLengthAttribute attribute, object value)
        {
            Assert.Throws<ValidationException>(() => attribute.Validate(value, new ValidationContext(new object())));
            Assert.False(attribute.IsValid(value));
        }

        [Theory]
        [MemberData(nameof(ValidValues_ICollection))]
        [MemberData(nameof(InvalidValues_ICollection))]
        [SkipOnTargetFramework(~TargetFrameworkMonikers.NetFramework, "MaxLengthAttribute in the .NET Core supports ICollection.Count. See https://github.com/dotnet/corefx/issues/18361")]
        public void Validate_ICollection_NetFx_ThrowsInvalidCastException(MaxLengthAttribute attribute, object value)
        {
            Assert.Throws<InvalidCastException>(() => attribute.Validate(value, new ValidationContext(new object())));
            Assert.Throws<InvalidCastException>(() => attribute.IsValid(value));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-10)]
        public static void GetValidationResult_InvalidLength_ThrowsInvalidOperationException(int length)
        {
            var attribute = new MaxLengthAttribute(length);
            Assert.Throws<InvalidOperationException>(() => attribute.Validate("Twoflower", new ValidationContext(new object())));
        }

        [Fact]
        public static void GetValidationResult_ValueNotStringOrICollection_ThrowsInvalidCastException()
        {
            Assert.Throws<InvalidCastException>(() => new MaxLengthAttribute().GetValidationResult(new Random(), new ValidationContext(new object())));
        }

        [Fact]
        public static void GetValidationResult_ValueGenericICollection_ThrowsInvalidCastException()
        {
            Assert.Throws<InvalidCastException>(() => new MaxLengthAttribute().GetValidationResult(new GenericICollectionClass(), new ValidationContext(new object())));
        }
    }

    class GenericICollectionClass : ICollection<int>
    {
        public int Count { get; set; }
        public bool IsReadOnly => false;
        public void Add(int item) { }
        public void Clear() { }
        public bool Contains(int item) => false;
        public void CopyTo(int[] array, int arrayIndex) { }
        public IEnumerator<int> GetEnumerator() => new List<int>(new int[Count]).GetEnumerator();
        public bool Remove(int item) => false;
        IEnumerator IEnumerable.GetEnumerator() => new List<int>(new int[Count]).GetEnumerator();
    }
}
