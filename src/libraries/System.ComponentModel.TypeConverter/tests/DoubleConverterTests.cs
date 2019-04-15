// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.Globalization;

namespace System.ComponentModel.Tests
{
    public class DoubleConverterTests : BaseNumberConverterTests
    {
        public override TypeConverter Converter => new DoubleConverter();

        public override IEnumerable<ConvertTest> ConvertToTestData()
        {
            yield return ConvertTest.Valid((double)-1, "-1");
            yield return ConvertTest.Valid(1.1, 1.1.ToString());
            yield return ConvertTest.Valid((double)2, (double)2, CultureInfo.InvariantCulture);
            yield return ConvertTest.Valid((double)3.1, (float)3.1);

            yield return ConvertTest.Valid((double)-1, "?1", new CustomPositiveSymbolCulture());

            yield return ConvertTest.CantConvertTo((double)3, typeof(InstanceDescriptor));
            yield return ConvertTest.CantConvertTo((double)3, typeof(object));
        }

        public override IEnumerable<ConvertTest> ConvertFromTestData()
        {
            yield return ConvertTest.Valid("1", (double)1);
            yield return ConvertTest.Valid(1.1.ToString(), 1.1);
            yield return ConvertTest.Valid(" -1 ", (double)-1);
            yield return ConvertTest.Valid("+5", (double)5);
            yield return ConvertTest.Valid(" +5 ", (double)5);

            yield return ConvertTest.Throws<ArgumentException, Exception>("#2");
            yield return ConvertTest.Throws<ArgumentException, Exception>(" #2 ");
            yield return ConvertTest.Throws<ArgumentException, Exception>("0x3");
            if (!PlatformDetection.IsFullFramework)
            {
                yield return ConvertTest.Throws<ArgumentException>("0X3");
                yield return ConvertTest.Throws<ArgumentException>(" 0X3 ");
                yield return ConvertTest.Throws<ArgumentException>("&h4");
                yield return ConvertTest.Throws<ArgumentException>("&H4");
                yield return ConvertTest.Throws<ArgumentException>(" &H4 ");
            }

            foreach (ConvertTest test in base.ConvertFromTestData())
            {
                yield return test;
            }
        }
    }
}
