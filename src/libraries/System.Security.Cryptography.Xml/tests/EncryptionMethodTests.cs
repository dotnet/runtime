// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Xml;
using Xunit;

namespace System.Security.Cryptography.Xml.Tests
{
    public class EncryptionMethodTests
    {
        [Fact]
        public void Ctor_Default()
        {
            EncryptionMethod method = new EncryptionMethod();
            Assert.Equal(0, method.KeySize);
            Assert.Null(method.KeyAlgorithm);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("algorithm")]
        public void Ctor_String(string algorithm)
        {
            EncryptionMethod method = new EncryptionMethod(algorithm);
            Assert.Equal(0, method.KeySize);
            Assert.Equal(algorithm, method.KeyAlgorithm);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(int.MaxValue)]
        public void KeySize_Set_SetsValue(int value)
        {
            EncryptionMethod method = new EncryptionMethod() { KeySize = value };
            Assert.Equal(value, method.KeySize);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        public void KeySize_SetNegativeValue_ThrowsArgumentOutOfRangeException(int value)
        {
            EncryptionMethod method = new EncryptionMethod();
            if (PlatformDetection.IsFullFramework)
                AssertExtensions.Throws<ArgumentOutOfRangeException>("The key size should be a non negative integer.", () => method.KeySize = value);
            else
                AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => method.KeySize = value);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("algorithm")]
        public void KeyAlgorithm_Set_SetsValue(string value)
        {
            EncryptionMethod method = new EncryptionMethod() { KeyAlgorithm = value };
            Assert.Equal(value, method.KeyAlgorithm);
        }

        public static IEnumerable<object[]> GetXml_TestData()
        {
            yield return new object[]
            {
                new EncryptionMethod(),
                "<EncryptionMethod xmlns=\"http://www.w3.org/2001/04/xmlenc#\" />"
            };

            yield return new object[]
            {
                new EncryptionMethod("algorithm"),
                "<EncryptionMethod Algorithm=\"algorithm\" xmlns=\"http://www.w3.org/2001/04/xmlenc#\" />"
            };

            yield return new object[]
            {
                new EncryptionMethod("algorithm") { KeySize = 1 },
                "<EncryptionMethod Algorithm=\"algorithm\" xmlns=\"http://www.w3.org/2001/04/xmlenc#\"><KeySize>1</KeySize></EncryptionMethod>"
            };
        }

        [Theory]
        [MemberData(nameof(GetXml_TestData))]
        public void GetXml(EncryptionMethod method, string xml)
        {
            XmlElement element = method.GetXml();
            Assert.Equal(xml, element.OuterXml);
        }

        [Fact]
        public void GetXml_FromConstructor_DoesntCacheXml()
        {
            EncryptionMethod method = new EncryptionMethod();
            Assert.NotSame(method.GetXml(), method.GetXml());
        }

        [Fact]
        public void GetXml_FromLoadXml_CachesXml()
        {
            EncryptionMethod method = new EncryptionMethod();

            XmlDocument document = new XmlDocument();
            XmlElement value = document.CreateElement("EncryptionMethod");

            method.LoadXml(value);
            Assert.Same(method.GetXml(), method.GetXml());
        }

        public static IEnumerable<object[]> LoadXml_TestData()
        {
            yield return new object[] { "<name />", null, 0 };
            yield return new object[] { "<name Algorithm=\"algorithm\"/>", "algorithm", 0 };
            yield return new object[] { "<name xmlns:enc=\"http://www.w3.org/2001/04/xmlenc#\"><enc:KeySize>1</enc:KeySize></name>", null, 1 };
            yield return new object[] { "<name xmlns:enc=\"http://www.w3.org/2001/04/xmlenc#\"><enc:KeySize>  1   </enc:KeySize></name>", null, 1 };
            yield return new object[] { "<name xmlns:enc=\"http://www.w3.org/2001/04/xmlenc#\" Algorithm=\"algorithm\" ><enc:KeySize>1</enc:KeySize></name>", "algorithm", 1 };

            // Custom namespace
            yield return new object[] { "<name xmlns:enc=\"http://www.w3.org/2001/04/xmlenc#\" enc:Algorithm=\"algorithm\"/>", "algorithm", 0 };
            yield return new object[] { "<name xmlns:abc=\"http://www.w3.org/2001/04/xmlenc#\" abc:Algorithm=\"algorithm\"/>", "algorithm", 0 };
            yield return new object[] { "<name xmlns:abc=\"http://www.w3.org/2001/04/xmlenc#\"><abc:KeySize>1</abc:KeySize></name>", null, 1 };
            yield return new object[] { "<name Algorithm=\"originalAlgorithm\" xmlns:enc=\"http://www.w3.org/2001/04/xmlenc#\" enc:Algorithm=\"namespacedAlgorith\"/>", "originalAlgorithm", 0 };
            yield return new object[] { "<name xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:Algorithm=\"algorithm\"/>", null, 0 };


            yield return new object[] { "<name algorithm=\"algorithm\"/>", null, 0 };
            yield return new object[] { "<name Algorithm=\"algorithm\"><KeySize>1</KeySize></name>", "algorithm", 0 };
            yield return new object[] { "<name xmlns:enc=\"http://www.w3.org/2001/04/xmlenc#\"><KeySize>1</KeySize></name>", null, 0 };
        }

        [Theory]
        [MemberData(nameof(LoadXml_TestData))]
        public void LoadXml(string xml, string expectedKeyAlgorithm, int expectedKeySize)
        {
            XmlDocument document = new XmlDocument();
            document.LoadXml(xml);
            XmlElement value = (XmlElement)document.FirstChild;

            EncryptionMethod method = new EncryptionMethod();
            method.LoadXml(value);

            Assert.Equal(expectedKeyAlgorithm, method.KeyAlgorithm);
            Assert.Equal(expectedKeySize, method.KeySize);

            Assert.Same(value, method.GetXml());
        }

        [Fact]
        public void LoadXml_NullValue_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("value", () => new EncryptionMethod().LoadXml(null));
        }

        [Theory]
        [InlineData("-1", typeof(ArgumentOutOfRangeException))]
        [InlineData("0", typeof(ArgumentOutOfRangeException))]
        [InlineData("abc", typeof(FormatException))]
        [InlineData("2147483648", typeof(OverflowException))]
        public void LoadXml_NegativeKeySize_Throws(string keySize, Type exceptionType)
        {
            XmlDocument document = new XmlDocument();
            document.LoadXml($"<name xmlns:enc=\"http://www.w3.org/2001/04/xmlenc#\"><enc:KeySize>{keySize}</enc:KeySize></name>");
            XmlElement value = (XmlElement)document.FirstChild;

            EncryptionMethod method = new EncryptionMethod();
            Assert.Throws(exceptionType, () => method.LoadXml(value));
        }
    }
}
