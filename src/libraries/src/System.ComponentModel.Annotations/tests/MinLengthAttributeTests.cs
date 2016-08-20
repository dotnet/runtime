// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using Xunit;

namespace System.ComponentModel.DataAnnotations.Tests
{
    public class MinLengthAttributeTests
    {
        private static readonly ValidationContext s_testValidationContext = new ValidationContext(new object());

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
            Assert.Throws<InvalidOperationException>(() => attribute.GetValidationResult("Rincewind", s_testValidationContext));
        }

        [Fact]
        public static void GetValidationResult_ValueNotStringOrICollection_ThrowsInvalidCastException()
        {
            Assert.Throws<InvalidCastException>(() => new MinLengthAttribute(0).GetValidationResult(new Random(), s_testValidationContext));
        }

        [Fact]
        public static void GetValidationResult_ValueGenericICollection_ThrowsInvalidCastException()
        {
            Assert.Throws<InvalidCastException>(() => new MinLengthAttribute(0).GetValidationResult(new GenericICollectionClass(), s_testValidationContext));
        }

        public static IEnumerable<object[]> Valid_TestData()
        {
            yield return new object[] { 10, null };
            yield return new object[] { 0, "" };
            yield return new object[] { 12, "OverMinLength" };
            yield return new object[] { 16, "EqualToMinLength" };

            yield return new object[] { 0, new int[0] };
            yield return new object[] { 12, new int[13] };
            yield return new object[] { 16, new string[16] };

            yield return new object[] { 0, new Collection<int>(new int[0]) };
            yield return new object[] { 12, new Collection<int>(new int[13]) };
            yield return new object[] { 16, new Collection<string>(new string[16]) };

            yield return new object[] { 0, new List<int>(new int[0]) };
            yield return new object[] { 12, new List<int>(new int[13]) };
            yield return new object[] { 16, new List<string>(new string[16]) };
        }

        [Theory]
        [MemberData(nameof(Valid_TestData))]
        public static void GetValidationResult_ValidValue_ReturnsSuccess(int length, object value)
        {
            Assert.Equal(ValidationResult.Success, new MinLengthAttribute(length).GetValidationResult(value, s_testValidationContext));
        }

        public static IEnumerable<object[]> Invalid_TestData()
        {
            yield return new object[] { 15, "UnderMinLength" };
            yield return new object[] { 15, new byte[14] };
            yield return new object[] { 15, new Collection<byte>(new byte[14]) };
            yield return new object[] { 15, new List<byte>(new byte[14]) };

            yield return new object[] { 12, new int[3, 3] };
        }

        [Theory]
        [MemberData(nameof(Invalid_TestData))]
        public static void GetValidationResult_InvalidValue_ReturnsNotNull(int length, object value)
        {
            ValidationResult result = new MinLengthAttribute(length).GetValidationResult(value, s_testValidationContext);
            Assert.NotNull(result.ErrorMessage);
        }
    }
}
