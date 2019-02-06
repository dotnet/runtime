// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using Xunit;

namespace System.ComponentModel.Tests
{
    public class InvalidEnumArgumentExceptionTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var exception = new InvalidEnumArgumentException();
            Assert.NotEmpty(exception.Message);
            Assert.Null(exception.ParamName);
            Assert.Null(exception.InnerException);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("message")]
        public void Ctor_Message(string message)
        {
            var exception = new InvalidEnumArgumentException(message);
            if (message == null)
            {
                Assert.NotEmpty(exception.Message);
            }
            else
            {
                Assert.Equal(message, exception.Message);
            }
            Assert.Null(exception.ParamName);
            Assert.Null(exception.InnerException);
        }

        public static IEnumerable<object[]> Ctor_Message_Exception_TestData()
        {
            yield return new object[] { null, null };
            yield return new object[] { "", new DivideByZeroException() };
            yield return new object[] { "message", new ArgumentException() };
        }

        [Theory]
        [MemberData(nameof(Ctor_Message_Exception_TestData))]
        public void Ctor_Message_InnerException(string message, Exception innerException)
        {
            var exception = new InvalidEnumArgumentException(message, innerException);
            if (message == null)
            {
                Assert.NotEmpty(exception.Message);
            }
            else
            {
                Assert.Equal(message, exception.Message);
            }
            Assert.Null(exception.ParamName);
            Assert.Same(innerException, exception.InnerException);
        }

        [Theory]
        [InlineData(null, 0, typeof(int))]
        [InlineData("", 1, typeof(int))]
        [InlineData("argumentName", int.MaxValue, typeof(int))]
        public void Ctor_ArgumentName_InvalidValue_EnumClass(string argumentName, int invalidValue, Type enumClass)
        {
            var exception = new InvalidEnumArgumentException(argumentName, invalidValue, enumClass);
            if (argumentName != null)
            {
                Assert.Contains(argumentName, exception.Message);
            }
            Assert.Contains(invalidValue.ToString(), exception.Message);
            Assert.Contains(enumClass.Name, exception.Message);
            Assert.Equal(argumentName, exception.ParamName);
            Assert.Null(exception.InnerException);
        }

        [Fact]
        public void Ctor_NullEnumClass_ThrowsNullReferenceException()
        {
            Assert.Throws<NullReferenceException>(() => new InvalidEnumArgumentException("argumentName", 1, null));
        }

        [Fact]
        public void Ctor_SerializationInfo_StreamingContext()
        {
            using (var stream = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(stream, new InvalidEnumArgumentException());
                stream.Seek(0, SeekOrigin.Begin);

                Assert.IsType<InvalidEnumArgumentException>(formatter.Deserialize(stream));
            }
        }
    }
}
