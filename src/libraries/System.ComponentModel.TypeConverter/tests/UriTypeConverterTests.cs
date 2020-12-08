// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel.Design.Serialization;
using System.Globalization;
using Xunit;

namespace System.ComponentModel.Tests
{
    public class UriTypeConverterTests : ConverterTestBase
    {
        private static UriTypeConverter s_converter = new UriTypeConverter();

        [Fact]
        public static void CanConvertFrom_WithContext_UriTypeConverter()
        {
            CanConvertFrom_WithContext(new object[3, 2]
                {
                    { typeof(string), true },
                    { typeof(Uri), true },
                    { typeof(InstanceDescriptor), true }
                },
                UriTypeConverterTests.s_converter);
        }

        [Fact]
        public static void CanConvertTo_WithContext_UriTypeConverter()
        {
            CanConvertTo_WithContext(new object[3, 2]
                {
                    { typeof(string), true },
                    { typeof(Uri), true },
                    { typeof(InstanceDescriptor), true }
                },
                UriTypeConverterTests.s_converter);
        }

        [Fact]
        public static void ConvertFrom_WithContext_UriTypeConverter()
        {
            ConvertFrom_WithContext(new object[5, 3]
                {
                    {"http://www.Microsoft.com/", new Uri("http://www.Microsoft.com/"),  CultureInfo.InvariantCulture},
                    {"/relative",  new Uri("/relative", UriKind.Relative),  CultureInfo.InvariantCulture},
                    {new Uri("http://www.Microsoft.com/"), new Uri("http://www.Microsoft.com/"),  null},
                    {new Uri("/relative", UriKind.Relative), new Uri("/relative", UriKind.Relative),  null},
                    {"mailto:?to=User2@Host2.com;cc=User3@Host3com", new Uri("mailto:?to=User2@Host2.com;cc=User3@Host3com"),  null}
                },
                UriTypeConverterTests.s_converter);
        }

        [Fact]
        public static void ConvertTo_WithContext_UriTypeConverter()
        {
            ConvertTo_WithContext(new object[3, 3]
                {
                    {new Uri("http://www.Microsoft.com/"), "http://www.Microsoft.com/", CultureInfo.InvariantCulture},
                    {new Uri("/relative", UriKind.Relative), "/relative", CultureInfo.InvariantCulture},
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
