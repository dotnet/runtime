// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Xunit;

namespace System.Tests
{
    public class RealFormatterTests : RealFormatterTestsBase
    {
        // The actual tests are defined in: src\Common\tests\System\RealFormatterTestsBase.cs

        protected override string InvariantToStringDouble(double d, string format)
        {
            return d.ToString(format, CultureInfo.InvariantCulture);
        }

        protected override string InvariantToStringSingle(float f, string format)
        {
            return f.ToString(format, CultureInfo.InvariantCulture);
        }

        [Theory]
        [InlineData("##.#", double.Epsilon, "")]
        [InlineData("##.#", double.MaxValue, "179769313486232000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000")]
        [InlineData("##.#", Math.E, "2.7")]
        [InlineData("##.#", Math.PI, "3.1")]
        [InlineData("##.#", 0.0, "")]
        [InlineData("##.#", 0.0046, "")]
        [InlineData("##.#", 0.005, "")]
        [InlineData("##.#", 0.125, ".1")]
        [InlineData("##.#", 0.5, ".5")]
        [InlineData("##.#", 0.51, ".5")]
        [InlineData("##.#", 0.56789, ".6")]
        [InlineData("##.#", 0.84551240822557006, ".8")]
        [InlineData("##.#", 1.0, "1")]
        [InlineData("##.#", 46.5f, "46.5")]
        [InlineData("##.#", 505.0, "505")]
        [InlineData("##.#", 506.0, "506")]
        [InlineData("##.#", 545.0, "545")]
        [InlineData("##.#", 545.1, "545.1")]
        [InlineData("##.#", 555.0, "555")]
        [InlineData("##.#", 46500.0f, "46500")]
        [InlineData("##.#", 65747.125, "65747.1")]
        [InlineData("##.#", 1844674407370955.25, "1844674407370960")]
        [InlineData("#,###.00", double.Epsilon, ".00")]
        [InlineData("#,###.00", double.MaxValue, "179,769,313,486,232,000,000,000,000,000,000,000,000,000,000,000,000,000,000,000,000,000,000,000,000,000,000,000,000,000,000,000,000,000,000,000,000,000,000,000,000,000,000,000,000,000,000,000,000,000,000,000,000,000,000,000,000,000,000,000,000,000,000,000,000,000,000,000,000,000,000,000,000,000,000,000,000,000,000,000,000,000,000,000,000,000,000,000,000,000,000,000,000,000,000,000,000,000,000,000,000,000,000.00")]
        [InlineData("#,###.00", Math.E, "2.72")]
        [InlineData("#,###.00", Math.PI, "3.14")]
        [InlineData("#,###.00", 0.0, ".00")]
        [InlineData("#,###.00", 0.0046, ".00")]
        [InlineData("#,###.00", 0.005, ".01")]
        [InlineData("#,###.00", 0.125, ".13")]
        [InlineData("#,###.00", 0.5, ".50")]
        [InlineData("#,###.00", 0.51, ".51")]
        [InlineData("#,###.00", 0.56789, ".57")]
        [InlineData("#,###.00", 0.84551240822557006, ".85")]
        [InlineData("#,###.00", 1.0, "1.00")]
        [InlineData("#,###.00", 46.5f, "46.50")]
        [InlineData("#,###.00", 505.0, "505.00")]
        [InlineData("#,###.00", 506.0, "506.00")]
        [InlineData("#,###.00", 545.0, "545.00")]
        [InlineData("#,###.00", 545.1, "545.10")]
        [InlineData("#,###.00", 555.0, "555.00")]
        [InlineData("#,###.00", 46500.0f, "46,500.00")]
        [InlineData("#,###.00", 65747.125, "65,747.13")]
        [InlineData("#,###.00", 1844674407370955.25, "1,844,674,407,370,960.00")]
        public void TestFormatterDouble_Custom(string format, double value, string expectedResult) => TestFormatterDouble_Standard(value, format, expectedResult);

        [Theory]
        [InlineData("##.#", float.Epsilon, "")]
        [InlineData("##.#", float.MaxValue, "340282300000000000000000000000000000000")]
        [InlineData("##.#", MathF.E, "2.7")]
        [InlineData("##.#", MathF.PI, "3.1")]
        [InlineData("##.#", 0.0f, "")]
        [InlineData("##.#", 0.0046f, "")]
        [InlineData("##.#", 0.005f, "")]
        [InlineData("##.#", 0.125f, ".1")]
        [InlineData("##.#", 0.5f, ".5")]
        [InlineData("##.#", 0.51f, ".5")]
        [InlineData("##.#", 0.56789f, ".6")]
        [InlineData("##.#", 0.845512390f, ".8")]
        [InlineData("##.#", 1.0f, "1")]
        [InlineData("##.#", 46.5f, "46.5")]
        [InlineData("##.#", 505.0f, "505")]
        [InlineData("##.#", 506.0f, "506")]
        [InlineData("##.#", 545.0f, "545")]
        [InlineData("##.#", 545.1f, "545.1")]
        [InlineData("##.#", 555.0f, "555")]
        [InlineData("##.#", 46500.0f, "46500")]
        [InlineData("##.#", 65747.125f, "65747.1")]
        [InlineData("##.#", 429496.72f, "429496.7")]
        [InlineData("#,###.00", float.Epsilon, ".00")]
        [InlineData("#,###.00", float.MaxValue, "340,282,300,000,000,000,000,000,000,000,000,000,000.00")]
        [InlineData("#,###.00", MathF.E, "2.72")]
        [InlineData("#,###.00", MathF.PI, "3.14")]
        [InlineData("#,###.00", 0.0f, ".00")]
        [InlineData("#,###.00", 0.0046f, ".00")]
        [InlineData("#,###.00", 0.005f, ".01")]
        [InlineData("#,###.00", 0.125f, ".13")]
        [InlineData("#,###.00", 0.5f, ".50")]
        [InlineData("#,###.00", 0.51f, ".51")]
        [InlineData("#,###.00", 0.56789f, ".57")]
        [InlineData("#,###.00", 0.845512390f, ".85")]
        [InlineData("#,###.00", 1.0f, "1.00")]
        [InlineData("#,###.00", 46.5f, "46.50")]
        [InlineData("#,###.00", 505.0f, "505.00")]
        [InlineData("#,###.00", 506.0f, "506.00")]
        [InlineData("#,###.00", 545.0f, "545.00")]
        [InlineData("#,###.00", 545.1f, "545.10")]
        [InlineData("#,###.00", 555.0f, "555.00")]
        [InlineData("#,###.00", 46500.0f, "46,500.00")]
        [InlineData("#,###.00", 65747.125f, "65,747.12")]
        [InlineData("#,###.00", 429496.72f, "429,496.70")]
        public void TestFormatteSingle_Custom(string format, float value, string expectedResult) => TestFormatterSingle_Standard(value, format, expectedResult);
    }
}
