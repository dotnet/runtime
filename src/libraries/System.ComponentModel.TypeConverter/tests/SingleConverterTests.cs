// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.Globalization;

namespace System.ComponentModel.Tests
{
    public class SingleConverterTests : BaseNumberConverterTests
    {
        public override TypeConverter Converter => new SingleConverter();

        public override IEnumerable<ConvertTest> ConvertToTestData()
        {
            yield return ConvertTest.Valid((float)-1, "-1");
            yield return ConvertTest.Valid(1.1f, 1.1f.ToString());
            yield return ConvertTest.Valid((float)2, (float)2, CultureInfo.InvariantCulture);
            yield return ConvertTest.Valid((float)3, (double)3);

            yield return ConvertTest.Valid((float)-1, "?1", new CustomPositiveSymbolCulture());

            yield return ConvertTest.CantConvertTo((float)3, typeof(InstanceDescriptor));
            yield return ConvertTest.CantConvertTo((float)3, typeof(object));
        }

        public override IEnumerable<ConvertTest> ConvertFromTestData()
        {
            yield return ConvertTest.Valid("1", (float)1);
            yield return ConvertTest.Valid(1.1.ToString(), 1.1f);
            yield return ConvertTest.Valid(" -1 ", (float)-1);
            yield return ConvertTest.Valid("+5", (float)5);
            yield return ConvertTest.Valid(" +5 ", (float)5);

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
