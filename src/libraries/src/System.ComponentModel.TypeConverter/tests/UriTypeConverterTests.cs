// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        public static void ConvertFrom_WithContext_Negative()
        {
            Assert.Throws<NotSupportedException>(
                () => UriTypeConverterTests.s_converter.ConvertFrom(TypeConverterTests.s_context, null, null));
            Assert.Throws<UriFormatException>(
                () => UriTypeConverterTests.s_converter.ConvertFrom(TypeConverterTests.s_context, null, "mailto:User@"));
        }
    }
}
