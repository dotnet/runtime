// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.Globalization;

namespace System.ComponentModel.Tests
{
    public class DecimalConverterTests : BaseNumberConverterTests
    {
        public override TypeConverter Converter => new DecimalConverter();

        public override IEnumerable<ConvertTest> ConvertToTestData()
        {
            yield return ConvertTest.Valid((decimal)-1, "-1");
            yield return ConvertTest.Valid(1.1m, 1.1m.ToString());
            yield return ConvertTest.Valid(3.3m, (float)3.3);

            yield return ConvertTest.Valid((decimal)-1, "?1", new CustomPositiveSymbolCulture());

            yield return ConvertTest.Valid((decimal)3, new InstanceDescriptor(
                typeof(decimal).GetConstructor(new Type[] { typeof(int[]) }),
                new object[] { new int[] { 3, 0, 0, 0 } }
            ));

            yield return ConvertTest.CantConvertTo((decimal)3, typeof(decimal));
            yield return ConvertTest.CantConvertTo((decimal)3, typeof(object));
        }

        public override IEnumerable<ConvertTest> ConvertFromTestData()
        {
            yield return ConvertTest.Valid("1", (decimal)1);
            yield return ConvertTest.Valid(1.1.ToString(), 1.1m);
            yield return ConvertTest.Valid(" -1 ", (decimal)-1);
            yield return ConvertTest.Valid("+5", (decimal)5);
            yield return ConvertTest.Valid(" +5 ", (decimal)5);

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
