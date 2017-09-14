// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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
        }

        protected static IEnumerable<object[]> ValidValues_ICollection()
        {
            yield return new object[] { new MinLengthAttribute(0), new Collection<int>(new int[0]) };
            yield return new object[] { new MinLengthAttribute(12), new Collection<int>(new int[14]) };
            yield return new object[] { new MinLengthAttribute(16), new Collection<string>(new string[16]) };

            yield return new object[] { new MinLengthAttribute(0), new List<int>(new int[0]) };
            yield return new object[] { new MinLengthAttribute(12), new List<int>(new int[14]) };
            yield return new object[] { new MinLengthAttribute(16), new List<string>(new string[16]) };

            //ICollection<T> but not ICollection
            yield return new object[] { new MinLengthAttribute(0), new HashSet<int>() };
            yield return new object[] { new MinLengthAttribute(12), new HashSet<int>(Enumerable.Range(1, 14)) };
            yield return new object[] { new MinLengthAttribute(16), new HashSet<string>(Enumerable.Range(1, 16).Select(i => i.ToString())) };

            //ICollection but not ICollection<T>
            yield return new object[] { new MinLengthAttribute(0), new ArrayList(new int[0]) };
            yield return new object[] { new MinLengthAttribute(12), new ArrayList(new int[14]) };
            yield return new object[] { new MinLengthAttribute(16), new ArrayList(new string[16]) };

            //Multi ICollection<T>
            yield return new object[] { new MinLengthAttribute(0), new MultiCollection() };
        }

        protected override IEnumerable<TestCase> InvalidValues()
        {
            yield return new TestCase(new MinLengthAttribute(15), "UnderMinLength");
            yield return new TestCase(new MinLengthAttribute(15), new byte[14]);

            yield return new TestCase(new MinLengthAttribute(12), new int[3, 3]);
        }

        protected static IEnumerable<object[]> InvalidValues_ICollection()
        {
            yield return new object[] { new MinLengthAttribute(15), new Collection<byte>(new byte[14]) };
            yield return new object[] { new MinLengthAttribute(15), new List<byte>(new byte[14]) };
        }

        [Theory]
        [InlineData(10)]
        [InlineData(0)]
        [InlineData(-1)]
        public static void Ctor(int length)
        {
            Assert.Equal(length, new MinLengthAttribute(length).Length);
        }

        [Theory]
        [MemberData(nameof(ValidValues_ICollection))]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "MinLengthAttribute in the .NET Framework doesn't support ICollection.Count. See https://github.com/dotnet/corefx/issues/18361")]
        public void Validate_ICollection_NetCore_Valid(MinLengthAttribute attribute, object value)
        {
            attribute.Validate(value, new ValidationContext(new object()));
            Assert.True(attribute.IsValid(value));
        }

        [Theory]
        [MemberData(nameof(InvalidValues_ICollection))]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "MinLengthAttribute in the .NET Framework doesn't support ICollection.Count. See https://github.com/dotnet/corefx/issues/18361")]
        public void Validate_ICollection_NetCore_Invalid(MinLengthAttribute attribute, object value)
        {
            Assert.Throws<ValidationException>(() => attribute.Validate(value, new ValidationContext(new object())));
            Assert.False(attribute.IsValid(value));
        }

        [Theory]
        [MemberData(nameof(ValidValues_ICollection))]
        [MemberData(nameof(InvalidValues_ICollection))]
        [SkipOnTargetFramework(~TargetFrameworkMonikers.NetFramework, "MinLengthAttribute in .NET Core supports ICollection.Count. See https://github.com/dotnet/corefx/issues/18361")]
        public void Validate_ICollection_NetFx_ThrowsInvalidCastException(MinLengthAttribute attribute, object value)
        {
            Assert.Throws<InvalidCastException>(() => attribute.Validate(value, new ValidationContext(new object())));
            Assert.Throws<InvalidCastException>(() => attribute.IsValid(value));
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
        public static void GetValidationResult_ValueGenericIEnumerable_ThrowsInvalidCastException()
        {
            Assert.Throws<InvalidCastException>(() => new MinLengthAttribute(0).GetValidationResult(new GenericIEnumerableClass(), new ValidationContext(new object())));
        }
    }
}
