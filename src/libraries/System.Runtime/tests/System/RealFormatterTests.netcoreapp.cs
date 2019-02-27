// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;

namespace System.Tests
{
    public class RealFormatterTests : RealFormatterTestsBase
    {
        // The actual tests are defined in: src\Common\tests\System\RealFormatterTestsBase.netcoreapp.cs

        protected override string InvariantToStringDouble(double d, string format)
        {
            return d.ToString(format, CultureInfo.InvariantCulture);
        }

        protected override string InvariantToStringSingle(float f, string format)
        {
            return f.ToString(format, CultureInfo.InvariantCulture);
        }
    }
}
