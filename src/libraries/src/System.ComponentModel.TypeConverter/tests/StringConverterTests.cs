// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.ComponentModel.Tests
{
    public class StringConverterTests : TypeConverterTestBase
    {
        public override TypeConverter Converter => new StringConverter();

        public override bool CanConvertFromNull => true;

        public override IEnumerable<ConvertTest> ConvertFromTestData()
        {
            yield return ConvertTest.Valid("string", "string");
            yield return ConvertTest.Valid(string.Empty, string.Empty);
            yield return ConvertTest.Valid(null, string.Empty);

            yield return ConvertTest.CantConvertFrom(1);
            yield return ConvertTest.CantConvertFrom(new object());
        }
    }
}
