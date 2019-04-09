// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.Globalization;
using System.Linq;
using Xunit;

namespace System.ComponentModel.Tests
{
    public class CharConverterTests : TypeConverterTestBase
    {
        public override TypeConverter Converter => new CharConverter();

        public override IEnumerable<ConvertTest> ConvertToTestData()
        {
            yield return ConvertTest.Valid('a', "a");
            yield return ConvertTest.Valid('\u20AC', "\u20AC", CultureInfo.InvariantCulture);
            yield return ConvertTest.Valid('\0', string.Empty);

            yield return ConvertTest.CantConvertTo('a', typeof(char));
            yield return ConvertTest.CantConvertTo('a', typeof(InstanceDescriptor));
            yield return ConvertTest.CantConvertTo('a', typeof(object));
        }

        public override IEnumerable<ConvertTest> ConvertFromTestData()
        {
            yield return ConvertTest.Valid("  a  ", 'a');
            yield return ConvertTest.Valid("    ", '\0');
            yield return ConvertTest.Valid("", '\0');

            yield return ConvertTest.Throws<FormatException>("aa");

            yield return ConvertTest.CantConvertFrom(1);
            yield return ConvertTest.CantConvertFrom(null);
        }
    }
}