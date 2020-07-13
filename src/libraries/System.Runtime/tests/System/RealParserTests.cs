// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;

namespace System.Tests
{
    public class RealParserTests : RealParserTestsBase
    {
        // The actual tests are defined in: src\Common\tests\System\RealParserTestsBase.cs

        protected override string InvariantToStringDouble(double d)
        {
            return d.ToString("G17", CultureInfo.InvariantCulture);
        }

        protected override string InvariantToStringSingle(float f)
        {
            return f.ToString("G9", CultureInfo.InvariantCulture);
        }

        protected override bool InvariantTryParseDouble(string s, out double result)
        {
            return double.TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out result);
        }

        protected override bool InvariantTryParseSingle(string s, out float result)
        {
            return float.TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out result);
        }
    }
}
