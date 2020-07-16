// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Xunit;

namespace System.ComponentModel.Tests
{
    public class LicenseExceptionTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData(typeof(int))]
        public void Ctor_Type(Type type)
        {
            var exception = new LicenseException(type);
            Assert.Null(exception.InnerException);
            Assert.Equal(-2146232063, exception.HResult);
            Assert.Equal(type, exception.LicensedType);
            Assert.NotEmpty(exception.Message);
        }

        public static IEnumerable<object[]> Ctor_Type_Object_TestData()
        {
            yield return new object[] { null, null };
            yield return new object[] { typeof(int), new object() };
        }

        [Theory]
        [MemberData(nameof(Ctor_Type_Object_TestData))]
        public void Ctor_Type_Object(Type type, object instance)
        {
            var exception = new LicenseException(type, instance);
            Assert.Null(exception.InnerException);
            Assert.Equal(-2146232063, exception.HResult);
            Assert.Equal(type, exception.LicensedType);
            Assert.NotEmpty(exception.Message);
        }

        public static IEnumerable<object[]> Ctor_Type_Object_String_TestData()
        {
            yield return new object[] { null, null, "message" };
            yield return new object[] { typeof(LicenseException), new object(), "message" };
        }

        [Theory]
        [MemberData(nameof(Ctor_Type_Object_String_TestData))]
        public void Ctor_Type_Object_String(Type type, object instance, string message)
        {
            var exception = new LicenseException(type, instance, message);
            Assert.Null(exception.InnerException);
            Assert.Equal(-2146232063, exception.HResult);
            Assert.Equal(type, exception.LicensedType);
            Assert.Equal(message, exception.Message);
        }

        public static IEnumerable<object[]> Ctor_Type_Object_String_Exception_TestData()
        {
            yield return new object[] { null, null, "message", null };
            yield return new object[] { typeof(LicenseException), new object(), "message", new DivideByZeroException() };
        }

        [Theory]
        [MemberData(nameof(Ctor_Type_Object_String_Exception_TestData))]
        public void Ctor_Type_Object_String_Exception(Type type, object instance, string message, Exception innerException)
        {
            var exception = new LicenseException(type, instance, message, innerException);
            Assert.Same(innerException, exception.InnerException);
            Assert.Equal(-2146232063, exception.HResult);
            Assert.Equal(type, exception.LicensedType);
            Assert.Equal(message, exception.Message);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsBinaryFormatterSupported))]
        public void Ctor_SerializationInfo_StreamingContext()
        {
            using (var stream = new MemoryStream())
            {
                var binaryFormatter = new BinaryFormatter();
                binaryFormatter.Serialize(stream, new LicenseException(typeof(int)));

                stream.Seek(0, SeekOrigin.Begin);
                Assert.IsType<LicenseException>(binaryFormatter.Deserialize(stream));
            }
        }
    }
}
