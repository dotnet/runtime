// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Xunit;

namespace System.ComponentModel.DataAnnotations.Tests
{
    public class ValidationExceptionTests
    {
        [Fact]
        public static void Ctor_Empty()
        {
            ValidationException ex = new ValidationException();
            Assert.NotEmpty(ex.Message);
            Assert.Null(ex.InnerException);

            Assert.NotEmpty(ex.ValidationResult.ErrorMessage);
            Assert.Null(ex.Value);
            Assert.Null(ex.ValidationAttribute);
        }

        [Theory]
        [InlineData("ErrorMessage")]
        public static void Ctor_String(string errorMessage)
        {
            ValidationException ex = new ValidationException(errorMessage);
            Assert.Equal(errorMessage, ex.Message);
            Assert.Null(ex.InnerException);

            Assert.Equal(errorMessage, ex.ValidationResult.ErrorMessage);
            Assert.Null(ex.ValidationAttribute);
            Assert.Null(ex.Value);
        }

        [Theory]
        [InlineData("ErrorMessage")]
        public static void Ctor_String_Exception(string errorMessage)
        {
            Exception innerException = new ArithmeticException();
            ValidationException ex = new ValidationException(errorMessage, innerException);
            Assert.Equal(errorMessage, ex.Message);
            Assert.Same(innerException, ex.InnerException);

            Assert.Equal(errorMessage, ex.ValidationResult.ErrorMessage);
            Assert.Null(ex.ValidationAttribute);
            Assert.Null(ex.Value);
        }

        [Fact]
        public static void Ctor_ValidationResult_ValidationAttribute_Object()
        {
            ValidationResult validationResult = new ValidationResult("ErrorMessage");
            ValidationAttribute validatingAttribute = new UrlAttribute();
            object value = new object();

            ValidationException ex = new ValidationException(validationResult, validatingAttribute, value);
            Assert.Equal(validationResult.ErrorMessage, ex.Message);
            Assert.Null(ex.InnerException);

            Assert.Same(validationResult, ex.ValidationResult);
            Assert.Same(validatingAttribute, ex.ValidationAttribute);
            Assert.Same(value, ex.Value);
        }

        [Theory]
        [InlineData("ErrorMessage")]
        public static void Ctor_String_ValidationAttribute_Object(string errorMessage)
        {
            ValidationAttribute validatingAttribute = new UrlAttribute();
            object value = new object();

            ValidationException ex = new ValidationException(errorMessage, validatingAttribute, value);
            Assert.Equal(errorMessage, ex.Message);
            Assert.Null(ex.InnerException);

            Assert.Equal(errorMessage, ex.ValidationResult.ErrorMessage);
            Assert.Same(validatingAttribute, ex.ValidationAttribute);
            Assert.Same(value, ex.Value);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsBinaryFormatterSupported))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/50882", TestPlatforms.Android)]
        public void Ctor_SerializationInfo_StreamingContext()
        {
            using (var stream = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(stream, new ValidationException());

                stream.Seek(0, SeekOrigin.Begin);
                Assert.IsType<ValidationException>(formatter.Deserialize(stream));
            }
        }
    }
}
