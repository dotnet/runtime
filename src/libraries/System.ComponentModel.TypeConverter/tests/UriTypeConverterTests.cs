// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.Design.Serialization;
using System.Globalization;
using Xunit;

namespace System.ComponentModel.Tests
{
    public class UriTypeConverterTests : ConverterTestBase
    {
        private static UriTypeConverter s_converter = new UriTypeConverter();

        [Fact]
        public static void CanConvertFrom_WithContext()
        {
            CanConvertFrom_WithContext(new object[2, 2]
                {
                    { typeof(string), true },
                    { typeof(Uri), true }
                },
                UriTypeConverterTests.s_converter);
        }

        [Fact]
        public static void CanConvertTo_WithContext()
        {
            CanConvertTo_WithContext(new object[3, 2]
                {
                    { typeof(string), true },
                    { typeof(Uri), true },
                    { typeof(InstanceDescriptor), true },
                },
                UriTypeConverterTests.s_converter);
        }

        [Fact]
        public static void ConvertFrom_WithContext()
        {
            ConvertFrom_WithContext(new object[2, 3]
                {
                    {"http://www.Microsoft.com/", new Uri("http://www.Microsoft.com/"),  CultureInfo.InvariantCulture},
                    {"mailto:?to=User2@Host2.com;cc=User3@Host3com", new Uri("mailto:?to=User2@Host2.com;cc=User3@Host3com"),  null}
                },
                UriTypeConverterTests.s_converter);
        }

        [Fact]
        public static void ConvertTo_WithContext()
        {
            ConvertTo_WithContext(new object[2, 3]
                {
                    {new Uri("http://www.Microsoft.com/"), "http://www.Microsoft.com/", CultureInfo.InvariantCulture},
                    {new Uri("mailto:?to=User2@Host2.com;cc=User3@Host3com"), "mailto:?to=User2@Host2.com;cc=User3@Host3com",  null}
                },
                UriTypeConverterTests.s_converter);

            var actualInstanceDescriptor = (InstanceDescriptor)UriTypeConverterTests.s_converter.ConvertTo(new Uri("http://www.Microsoft.com/"), typeof(InstanceDescriptor));
            var expectedMemberInfo = typeof(Uri).GetConstructor(new[] { typeof(string), typeof(UriKind) });
            Assert.Equal(expectedMemberInfo, actualInstanceDescriptor.MemberInfo);
            Assert.Equal(new object[] { "http://www.Microsoft.com/", UriKind.Absolute }, actualInstanceDescriptor.Arguments);
            Assert.True(actualInstanceDescriptor.IsComplete);
            Assert.Equal(new Uri("http://www.Microsoft.com/"), actualInstanceDescriptor.Invoke());

            var actualRelativeInstanceDescriptor = (InstanceDescriptor)UriTypeConverterTests.s_converter.ConvertTo(new Uri("relative", UriKind.Relative), typeof(InstanceDescriptor));
            Assert.Equal(expectedMemberInfo, actualRelativeInstanceDescriptor.MemberInfo);
            Assert.Equal(new object[] { "relative", UriKind.Relative }, actualRelativeInstanceDescriptor.Arguments);
            Assert.True(actualRelativeInstanceDescriptor.IsComplete);
            Assert.Equal(new Uri("relative", UriKind.Relative), actualRelativeInstanceDescriptor.Invoke());
        }

        [Fact]
        public static void ConvertFrom_WithContext_Negative()
        {
            Assert.Throws<NotSupportedException>(
                () => UriTypeConverterTests.s_converter.ConvertFrom(TypeConverterTests.s_context, null, null));
            Assert.Throws<UriFormatException>(
                () => UriTypeConverterTests.s_converter.ConvertFrom(TypeConverterTests.s_context, null, "mailto:User@"));
        }
    }
}
