// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Tests
{
    public class ObsoleteAttributeTests
    {
        [Fact]
        public static void Ctor_Default()
        {
            var attribute = new ObsoleteAttribute();
            Assert.Null(attribute.Message);
            Assert.False(attribute.IsError);
            Assert.Null(attribute.DiagnosticId);
            Assert.Null(attribute.UrlFormat);
        }

        [Theory]
        [InlineData(null, null)]
        [InlineData("", "BCL0006")]
        [InlineData("message", "")]
        public void Ctor_String_Id(string message, string id)
        {
            var attribute = new ObsoleteAttribute(message) { DiagnosticId = id};
            Assert.Equal(message, attribute.Message);
            Assert.False(attribute.IsError);
            Assert.Equal(id, attribute.DiagnosticId);
            Assert.Null(attribute.UrlFormat);
        }

        [Theory]
        [InlineData(null, true, "")]
        [InlineData("", false, null)]
        [InlineData("message", true, "https://aka.ms/obsolete/{0}")]
        public void Ctor_String_Bool_Url(string message, bool error, string url)
        {
            var attribute = new ObsoleteAttribute(message, error) { UrlFormat = url};
            Assert.Equal(message, attribute.Message);
            Assert.Equal(error, attribute.IsError);
            Assert.Null(attribute.DiagnosticId);
            Assert.Equal(url, attribute.UrlFormat);
        }

        [Theory]
        [MemberData(nameof(TestData_Attributes))]
        public void TestAttributeValuesWithReflection(Type testType, string message, bool error, string id, string url) {

            Attribute attribute = Attribute.GetCustomAttributes(testType, typeof(Attribute)).Where((e) => e.GetType() == typeof(ObsoleteAttribute)).SingleOrDefault();

            Assert.NotNull(attribute);
            Assert.Equal(message, attribute.GetType().GetProperty("Message").GetValue(attribute));
            Assert.Equal(error, attribute.GetType().GetProperty("IsError").GetValue(attribute));
            Assert.Equal(id, attribute.GetType().GetProperty("DiagnosticId").GetValue(attribute));
            Assert.Equal(url, attribute.GetType().GetProperty("UrlFormat").GetValue(attribute));
        }

        public static IEnumerable<object[]> TestData_Attributes()
        {
#pragma warning disable
            yield return new object[] { typeof(ClassMessageAndDiagnosticIdPropertySet), "Don't use", false, "BCL0006", null };
            yield return new object[] { typeof(ClassEmptyConstructorOptionalPropertiesSet), null, false, "BCL0003", "https://aka.ms/obsolete3/{0}" };
            yield return new object[] { typeof(ClassMessageAndUrlFormatPropertySet), "Obsolete", false, null, "https://aka.ms/obsolete2/{0}" };
            yield return new object[] { typeof(ClassAllFieldPropertiesSet), "Deprecated", false, "BCL0001", "https://aka.ms/obsolete1/{0}" };
#pragma warning restore
        }

        [Obsolete("Don't use", DiagnosticId = "BCL0006")]
        private class ClassMessageAndDiagnosticIdPropertySet { }

        [Obsolete( DiagnosticId = "BCL0003", UrlFormat = "https://aka.ms/obsolete3/{0}")]
        private class ClassEmptyConstructorOptionalPropertiesSet { }

        [Obsolete("Obsolete", UrlFormat = "https://aka.ms/obsolete2/{0}")]
        private class ClassMessageAndUrlFormatPropertySet { }

        [Obsolete("Deprecated", false, UrlFormat = "https://aka.ms/obsolete1/{0}", DiagnosticId = "BCL0001")]
        private class ClassAllFieldPropertiesSet { }
    }
}
