// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.Globalization;
using Xunit;

namespace System.ComponentModel.Tests
{
    public abstract class BaseNumberConverterTests : TypeConverterTestBase
    {
        public override IEnumerable<ConvertTest> ConvertFromTestData()
        {
            yield return ConvertTest.Throws<ArgumentException, Exception>("");
            yield return ConvertTest.Throws<ArgumentException, Exception>("bad");
            yield return ConvertTest.Throws<ArgumentException, Exception>("0x");
            yield return ConvertTest.Throws<ArgumentException, Exception>("0X");
            yield return ConvertTest.Throws<ArgumentException, Exception>("1x1");
            yield return ConvertTest.Throws<ArgumentException, Exception>("0y1");
            yield return ConvertTest.Throws<ArgumentException, Exception>("&h");
            yield return ConvertTest.Throws<ArgumentException, Exception>("&H");
            yield return ConvertTest.Throws<ArgumentException, Exception>("0h1");
            yield return ConvertTest.Throws<ArgumentException, Exception>("&i1");

            yield return ConvertTest.CantConvertFrom(new object());
        }

        public class CustomPositiveSymbolCulture : CultureInfo
        {
            public CustomPositiveSymbolCulture() : base("en-GB")
            {
            }

            public override object GetFormat(Type formatType)
            {
                Assert.Equal(typeof(NumberFormatInfo), formatType);

                return new NumberFormatInfo
                {
                    PositiveSign = "!",
                    NegativeSign = "?"
                };
            }
        }
    }
}
