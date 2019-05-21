// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Tests
{
    public abstract class RealFormatterTestsBase
    {
        // The following methods need to be implemented for the tests to run:
        protected abstract string InvariantToStringDouble(double d, string format);
        protected abstract string InvariantToStringSingle(float f, string format);

        [Theory]
        [InlineData(double.Epsilon, "¤0.00")]
        [InlineData(double.MaxValue, "¤179,769,313,486,231,570,814,527,423,731,704,356,798,070,567,525,844,996,598,917,476,803,157,260,780,028,538,760,589,558,632,766,878,171,540,458,953,514,382,464,234,321,326,889,464,182,768,467,546,703,537,516,986,049,910,576,551,282,076,245,490,090,389,328,944,075,868,508,455,133,942,304,583,236,903,222,948,165,808,559,332,123,348,274,797,826,204,144,723,168,738,177,180,919,299,881,250,404,026,184,124,858,368.00")]
        [InlineData(Math.E, "¤2.72")]
        [InlineData(Math.PI, "¤3.14")]
        [InlineData(0.0, "¤0.00")]
        [InlineData(0.0046, "¤0.00")]
        [InlineData(0.125, "¤0.12")]
        [InlineData(0.84551240822557006, "¤0.85")]
        [InlineData(1.0, "¤1.00")]
        [InlineData(1844674407370955.25, "¤1,844,674,407,370,955.25")]
        public void TestFormatterDouble_C(double value, string expectedResult) => TestFormatterDouble_Standard(value, "C", expectedResult);

        [Theory]
        [InlineData(double.Epsilon, "¤0.0000")]
        [InlineData(double.MaxValue, "¤179,769,313,486,231,570,814,527,423,731,704,356,798,070,567,525,844,996,598,917,476,803,157,260,780,028,538,760,589,558,632,766,878,171,540,458,953,514,382,464,234,321,326,889,464,182,768,467,546,703,537,516,986,049,910,576,551,282,076,245,490,090,389,328,944,075,868,508,455,133,942,304,583,236,903,222,948,165,808,559,332,123,348,274,797,826,204,144,723,168,738,177,180,919,299,881,250,404,026,184,124,858,368.0000")]
        [InlineData(Math.E, "¤2.7183")]
        [InlineData(Math.PI, "¤3.1416")]
        [InlineData(0.0, "¤0.0000")]
        [InlineData(0.0046, "¤0.0046")]
        [InlineData(0.125, "¤0.1250")]
        [InlineData(0.84551240822557006, "¤0.8455")]
        [InlineData(1.0, "¤1.0000")]
        [InlineData(1844674407370955.25, "¤1,844,674,407,370,955.2500")]
        public void TestFormatterDouble_C4(double value, string expectedResult) => TestFormatterDouble_Standard(value, "C4", expectedResult);

        [Theory]
        [InlineData(double.Epsilon, "¤0.00000000000000000000")]
        [InlineData(double.MaxValue, "¤179,769,313,486,231,570,814,527,423,731,704,356,798,070,567,525,844,996,598,917,476,803,157,260,780,028,538,760,589,558,632,766,878,171,540,458,953,514,382,464,234,321,326,889,464,182,768,467,546,703,537,516,986,049,910,576,551,282,076,245,490,090,389,328,944,075,868,508,455,133,942,304,583,236,903,222,948,165,808,559,332,123,348,274,797,826,204,144,723,168,738,177,180,919,299,881,250,404,026,184,124,858,368.00000000000000000000")]
        [InlineData(Math.E, "¤2.71828182845904509080")]
        [InlineData(Math.PI, "¤3.14159265358979311600")]
        [InlineData(0.0, "¤0.00000000000000000000")]
        [InlineData(0.0046, "¤0.00459999999999999992")]
        [InlineData(0.125, "¤0.12500000000000000000")]
        [InlineData(0.84551240822557006, "¤0.84551240822557005572")]
        [InlineData(1.0, "¤1.00000000000000000000")]
        [InlineData(1844674407370955.25, "¤1,844,674,407,370,955.25000000000000000000")]
        public void TestFormatterDouble_C20(double value, string expectedResult) => TestFormatterDouble_Standard(value, "C20", expectedResult);

        [Theory]
        [InlineData(double.Epsilon, "4.940656E-324")]
        [InlineData(double.MaxValue, "1.797693E+308")]
        [InlineData(Math.E, "2.718282E+000")]
        [InlineData(Math.PI, "3.141593E+000")]
        [InlineData(0.0, "0.000000E+000")]
        [InlineData(0.0046, "4.600000E-003")]
        [InlineData(0.125, "1.250000E-001")]
        [InlineData(0.84551240822557006, "8.455124E-001")]
        [InlineData(1.0, "1.000000E+000")]
        [InlineData(1844674407370955.25, "1.844674E+015")]
        public void TestFormatterDouble_E(double value, string expectedResult) => TestFormatterDouble_Standard(value, "E", expectedResult);

        [Theory]
        [InlineData(double.Epsilon, "4.9407E-324")]
        [InlineData(double.MaxValue, "1.7977E+308")]
        [InlineData(Math.E, "2.7183E+000")]
        [InlineData(Math.PI, "3.1416E+000")]
        [InlineData(0.0, "0.0000E+000")]
        [InlineData(0.0046, "4.6000E-003")]
        [InlineData(0.125, "1.2500E-001")]
        [InlineData(0.84551240822557006, "8.4551E-001")]
        [InlineData(1.0, "1.0000E+000")]
        [InlineData(1844674407370955.25, "1.8447E+015")]
        public void TestFormatterDouble_E4(double value, string expectedResult) => TestFormatterDouble_Standard(value, "E4", expectedResult);

        [Theory]
        [InlineData(double.Epsilon, "4.94065645841246544177E-324")]
        [InlineData(double.MaxValue, "1.79769313486231570815E+308")]
        [InlineData(Math.E, "2.71828182845904509080E+000")]
        [InlineData(Math.PI, "3.14159265358979311600E+000")]
        [InlineData(0.0, "0.00000000000000000000E+000")]
        [InlineData(0.0046, "4.59999999999999992228E-003")]
        [InlineData(0.125, "1.25000000000000000000E-001")]
        [InlineData(0.84551240822557006, "8.45512408225570055720E-001")]
        [InlineData(1.0, "1.00000000000000000000E+000")]
        [InlineData(1844674407370955.25, "1.84467440737095525000E+015")]
        public void TestFormatterDouble_E20(double value, string expectedResult) => TestFormatterDouble_Standard(value, "E20", expectedResult);

        [Theory]
        [InlineData(double.Epsilon, "0.00")]
        [InlineData(double.MaxValue, "179769313486231570814527423731704356798070567525844996598917476803157260780028538760589558632766878171540458953514382464234321326889464182768467546703537516986049910576551282076245490090389328944075868508455133942304583236903222948165808559332123348274797826204144723168738177180919299881250404026184124858368.00")]
        [InlineData(Math.E, "2.72")]
        [InlineData(Math.PI, "3.14")]
        [InlineData(0.0, "0.00")]
        [InlineData(0.0046, "0.00")]
        [InlineData(0.125, "0.12")]
        [InlineData(0.84551240822557006, "0.85")]
        [InlineData(1.0, "1.00")]
        [InlineData(1844674407370955.25, "1844674407370955.25")]
        public void TestFormatterDouble_F(double value, string expectedResult) => TestFormatterDouble_Standard(value, "F", expectedResult);

        [Theory]
        [InlineData(double.Epsilon, "0.0000")]
        [InlineData(double.MaxValue, "179769313486231570814527423731704356798070567525844996598917476803157260780028538760589558632766878171540458953514382464234321326889464182768467546703537516986049910576551282076245490090389328944075868508455133942304583236903222948165808559332123348274797826204144723168738177180919299881250404026184124858368.0000")]
        [InlineData(Math.E, "2.7183")]
        [InlineData(Math.PI, "3.1416")]
        [InlineData(0.0, "0.0000")]
        [InlineData(0.0046, "0.0046")]
        [InlineData(0.125, "0.1250")]
        [InlineData(0.84551240822557006, "0.8455")]
        [InlineData(1.0, "1.0000")]
        [InlineData(1844674407370955.25, "1844674407370955.2500")]
        public void TestFormatterDouble_F4(double value, string expectedResult) => TestFormatterDouble_Standard(value, "F4", expectedResult);

        [Theory]
        [InlineData(double.Epsilon, "0.00000000000000000000")]
        [InlineData(double.MaxValue, "179769313486231570814527423731704356798070567525844996598917476803157260780028538760589558632766878171540458953514382464234321326889464182768467546703537516986049910576551282076245490090389328944075868508455133942304583236903222948165808559332123348274797826204144723168738177180919299881250404026184124858368.00000000000000000000")]
        [InlineData(Math.E, "2.71828182845904509080")]
        [InlineData(Math.PI, "3.14159265358979311600")]
        [InlineData(0.0, "0.00000000000000000000")]
        [InlineData(0.0046, "0.00459999999999999992")]
        [InlineData(0.125, "0.12500000000000000000")]
        [InlineData(0.84551240822557006, "0.84551240822557005572")]
        [InlineData(1.0, "1.00000000000000000000")]
        [InlineData(1844674407370955.25, "1844674407370955.25000000000000000000")]
        public void TestFormatterDouble_F20(double value, string expectedResult) => TestFormatterDouble_Standard(value, "F20", expectedResult);

        [Theory]
        [InlineData(double.Epsilon, "5E-324")]
        [InlineData(double.MaxValue, "1.7976931348623157E+308")]
        [InlineData(Math.E, "2.718281828459045")]
        [InlineData(Math.PI, "3.141592653589793")]
        [InlineData(0.0, "0")]
        [InlineData(0.0046, "0.0046")]
        [InlineData(0.125, "0.125")]
        [InlineData(0.84551240822557006, "0.8455124082255701")]
        [InlineData(1.0, "1")]
        [InlineData(1844674407370955.25, "1844674407370955.2")]
        public void TestFormatterDouble_G(double value, string expectedResult) => TestFormatterDouble_Standard(value, "G", expectedResult);

        [Theory]
        [InlineData(double.Epsilon, "4.941E-324")]
        [InlineData(double.MaxValue, "1.798E+308")]
        [InlineData(Math.E, "2.718")]
        [InlineData(Math.PI, "3.142")]
        [InlineData(0.0, "0")]
        [InlineData(0.0046, "0.0046")]
        [InlineData(0.125, "0.125")]
        [InlineData(0.84551240822557006, "0.8455")]
        [InlineData(1.0, "1")]
        [InlineData(1844674407370955.25, "1.845E+15")]
        public void TestFormatterDouble_G4(double value, string expectedResult) => TestFormatterDouble_Standard(value, "G4", expectedResult);

        [Theory]
        [InlineData(double.Epsilon, "4.9406564584124654418E-324")]
        [InlineData(double.MaxValue, "1.7976931348623157081E+308")]
        [InlineData(Math.E, "2.7182818284590450908")]
        [InlineData(Math.PI, "3.141592653589793116")]
        [InlineData(0.0, "0")]
        [InlineData(0.0046, "0.0045999999999999999223")]
        [InlineData(0.125, "0.125")]
        [InlineData(0.84551240822557006, "0.84551240822557005572")]
        [InlineData(1.0, "1")]
        [InlineData(1844674407370955.25, "1844674407370955.25")]
        public void TestFormatterDouble_G20(double value, string expectedResult) => TestFormatterDouble_Standard(value, "G20", expectedResult);

        [Theory]
        [InlineData(double.Epsilon, "0.00")]
        [InlineData(double.MaxValue, "179,769,313,486,231,570,814,527,423,731,704,356,798,070,567,525,844,996,598,917,476,803,157,260,780,028,538,760,589,558,632,766,878,171,540,458,953,514,382,464,234,321,326,889,464,182,768,467,546,703,537,516,986,049,910,576,551,282,076,245,490,090,389,328,944,075,868,508,455,133,942,304,583,236,903,222,948,165,808,559,332,123,348,274,797,826,204,144,723,168,738,177,180,919,299,881,250,404,026,184,124,858,368.00")]
        [InlineData(Math.E, "2.72")]
        [InlineData(Math.PI, "3.14")]
        [InlineData(0.0, "0.00")]
        [InlineData(0.0046, "0.00")]
        [InlineData(0.125, "0.12")]
        [InlineData(0.84551240822557006, "0.85")]
        [InlineData(1.0, "1.00")]
        [InlineData(1844674407370955.25, "1,844,674,407,370,955.25")]
        public void TestFormatterDouble_N(double value, string expectedResult) => TestFormatterDouble_Standard(value, "N", expectedResult);

        [Theory]
        [InlineData(double.Epsilon, "0.0000")]
        [InlineData(double.MaxValue, "179,769,313,486,231,570,814,527,423,731,704,356,798,070,567,525,844,996,598,917,476,803,157,260,780,028,538,760,589,558,632,766,878,171,540,458,953,514,382,464,234,321,326,889,464,182,768,467,546,703,537,516,986,049,910,576,551,282,076,245,490,090,389,328,944,075,868,508,455,133,942,304,583,236,903,222,948,165,808,559,332,123,348,274,797,826,204,144,723,168,738,177,180,919,299,881,250,404,026,184,124,858,368.0000")]
        [InlineData(Math.E, "2.7183")]
        [InlineData(Math.PI, "3.1416")]
        [InlineData(0.0, "0.0000")]
        [InlineData(0.0046, "0.0046")]
        [InlineData(0.125, "0.1250")]
        [InlineData(0.84551240822557006, "0.8455")]
        [InlineData(1.0, "1.0000")]
        [InlineData(1844674407370955.25, "1,844,674,407,370,955.2500")]
        public void TestFormatterDouble_N4(double value, string expectedResult) => TestFormatterDouble_Standard(value, "N4", expectedResult);

        [Theory]
        [InlineData(double.Epsilon, "0.00000000000000000000")]
        [InlineData(double.MaxValue, "179,769,313,486,231,570,814,527,423,731,704,356,798,070,567,525,844,996,598,917,476,803,157,260,780,028,538,760,589,558,632,766,878,171,540,458,953,514,382,464,234,321,326,889,464,182,768,467,546,703,537,516,986,049,910,576,551,282,076,245,490,090,389,328,944,075,868,508,455,133,942,304,583,236,903,222,948,165,808,559,332,123,348,274,797,826,204,144,723,168,738,177,180,919,299,881,250,404,026,184,124,858,368.00000000000000000000")]
        [InlineData(Math.E, "2.71828182845904509080")]
        [InlineData(Math.PI, "3.14159265358979311600")]
        [InlineData(0.0, "0.00000000000000000000")]
        [InlineData(0.0046, "0.00459999999999999992")]
        [InlineData(0.125, "0.12500000000000000000")]
        [InlineData(0.84551240822557006, "0.84551240822557005572")]
        [InlineData(1.0, "1.00000000000000000000")]
        [InlineData(1844674407370955.25, "1,844,674,407,370,955.25000000000000000000")]
        public void TestFormatterDouble_N20(double value, string expectedResult) => TestFormatterDouble_Standard(value, "N20", expectedResult);

        [Theory]
        [InlineData(double.Epsilon, "0.00 %")]
        [InlineData(double.MaxValue, "17,976,931,348,623,157,081,452,742,373,170,435,679,807,056,752,584,499,659,891,747,680,315,726,078,002,853,876,058,955,863,276,687,817,154,045,895,351,438,246,423,432,132,688,946,418,276,846,754,670,353,751,698,604,991,057,655,128,207,624,549,009,038,932,894,407,586,850,845,513,394,230,458,323,690,322,294,816,580,855,933,212,334,827,479,782,620,414,472,316,873,817,718,091,929,988,125,040,402,618,412,485,836,800.00 %")]
        [InlineData(Math.E, "271.83 %")]
        [InlineData(Math.PI, "314.16 %")]
        [InlineData(0.0, "0.00 %")]
        [InlineData(0.0046, "0.46 %")]
        [InlineData(0.125, "12.50 %")]
        [InlineData(0.84551240822557006, "84.55 %")]
        [InlineData(1.0, "100.00 %")]
        [InlineData(1844674407370955.25, "184,467,440,737,095,525.00 %")]
        public void TestFormatterDouble_P(double value, string expectedResult) => TestFormatterDouble_Standard(value, "P", expectedResult);

        [Theory]
        [InlineData(double.Epsilon, "0.0000 %")]
        [InlineData(double.MaxValue, "17,976,931,348,623,157,081,452,742,373,170,435,679,807,056,752,584,499,659,891,747,680,315,726,078,002,853,876,058,955,863,276,687,817,154,045,895,351,438,246,423,432,132,688,946,418,276,846,754,670,353,751,698,604,991,057,655,128,207,624,549,009,038,932,894,407,586,850,845,513,394,230,458,323,690,322,294,816,580,855,933,212,334,827,479,782,620,414,472,316,873,817,718,091,929,988,125,040,402,618,412,485,836,800.0000 %")]
        [InlineData(Math.E, "271.8282 %")]
        [InlineData(Math.PI, "314.1593 %")]
        [InlineData(0.0, "0.0000 %")]
        [InlineData(0.0046, "0.4600 %")]
        [InlineData(0.125, "12.5000 %")]
        [InlineData(0.84551240822557006, "84.5512 %")]
        [InlineData(1.0, "100.0000 %")]
        [InlineData(1844674407370955.25, "184,467,440,737,095,525.0000 %")]
        public void TestFormatterDouble_P4(double value, string expectedResult) => TestFormatterDouble_Standard(value, "P4", expectedResult);

        [Theory]
        [InlineData(double.Epsilon, "0.00000000000000000000 %")]
        [InlineData(double.MaxValue, "17,976,931,348,623,157,081,452,742,373,170,435,679,807,056,752,584,499,659,891,747,680,315,726,078,002,853,876,058,955,863,276,687,817,154,045,895,351,438,246,423,432,132,688,946,418,276,846,754,670,353,751,698,604,991,057,655,128,207,624,549,009,038,932,894,407,586,850,845,513,394,230,458,323,690,322,294,816,580,855,933,212,334,827,479,782,620,414,472,316,873,817,718,091,929,988,125,040,402,618,412,485,836,800.00000000000000000000 %")]
        [InlineData(Math.E, "271.82818284590450907956 %")]
        [InlineData(Math.PI, "314.15926535897931159980 %")]
        [InlineData(0.0, "0.00000000000000000000 %")]
        [InlineData(0.0046, "0.45999999999999999223 %")]
        [InlineData(0.125, "12.50000000000000000000 %")]
        [InlineData(0.84551240822557006, "84.55124082255700557198 %")]
        [InlineData(1.0, "100.00000000000000000000 %")]
        [InlineData(1844674407370955.25, "184,467,440,737,095,525.00000000000000000000 %")]
        public void TestFormatterDouble_P20(double value, string expectedResult) => TestFormatterDouble_Standard(value, "P20", expectedResult);

        [Theory]
        [InlineData(double.Epsilon, "5E-324")]
        [InlineData(double.MaxValue, "1.7976931348623157E+308")]
        [InlineData(Math.E, "2.718281828459045")]
        [InlineData(Math.PI, "3.141592653589793")]
        [InlineData(0.0, "0")]
        [InlineData(0.0046, "0.0046")]
        [InlineData(0.125, "0.125")]
        [InlineData(0.84551240822557006, "0.8455124082255701")]
        [InlineData(1.0, "1")]
        [InlineData(1844674407370955.25, "1844674407370955.2")]
        public void TestFormatterDouble_R(double value, string expectedResult) => TestFormatterDouble_Standard(value, "R", expectedResult);

        [Theory]
        [InlineData(double.Epsilon, "5E-324")]
        [InlineData(double.MaxValue, "1.7976931348623157E+308")]
        [InlineData(Math.E, "2.718281828459045")]
        [InlineData(Math.PI, "3.141592653589793")]
        [InlineData(0.0, "0")]
        [InlineData(0.0046, "0.0046")]
        [InlineData(0.125, "0.125")]
        [InlineData(0.84551240822557006, "0.8455124082255701")]
        [InlineData(1.0, "1")]
        [InlineData(1844674407370955.25, "1844674407370955.2")]
        public void TestFormatterDouble_R4(double value, string expectedResult) => TestFormatterDouble_Standard(value, "R4", expectedResult);

        [Theory]
        [InlineData(double.Epsilon, "5E-324")]
        [InlineData(double.MaxValue, "1.7976931348623157E+308")]
        [InlineData(Math.E, "2.718281828459045")]
        [InlineData(Math.PI, "3.141592653589793")]
        [InlineData(0.0, "0")]
        [InlineData(0.0046, "0.0046")]
        [InlineData(0.125, "0.125")]
        [InlineData(0.84551240822557006, "0.8455124082255701")]
        [InlineData(1.0, "1")]
        [InlineData(1844674407370955.25, "1844674407370955.2")]
        public void TestFormatterDouble_R20(double value, string expectedResult) => TestFormatterDouble_Standard(value, "R20", expectedResult);

        public static IEnumerable<object[]> TestFormatterDouble_InvalidMemberData =>
            from value in new[] { double.Epsilon, double.MaxValue, Math.E, Math.PI, 0.0, 0.84551240822557006, 1.0, 1844674407370955.25 }
            from format in new[] { "D", "D4", "D20", "X", "X4", "X20" }
            select new object[] { value, format };

        [Theory]
        [MemberData(nameof(TestFormatterDouble_InvalidMemberData))]
        public void TestFormatterDouble_Invalid(double value, string format) => Assert.Throws<FormatException>(() => InvariantToStringDouble(value, format));

        private void TestFormatterDouble_Standard(double value, string format, string expectedResult)
        {
            string actualResult = InvariantToStringDouble(value, format);
            Assert.Equal(expectedResult, actualResult);
        }

        [Theory]
        [InlineData(float.Epsilon, "¤0.00")]
        [InlineData(float.MaxValue, "¤340,282,346,638,528,859,811,704,183,484,516,925,440.00")]
        [InlineData(MathF.E, "¤2.72")]
        [InlineData(MathF.PI, "¤3.14")]
        [InlineData(0.0f, "¤0.00")]
        [InlineData(0.0046f, "¤0.00")]
        [InlineData(0.125f, "¤0.12")]
        [InlineData(0.845512390, "¤0.85")]
        [InlineData(1.0f, "¤1.00")]
        [InlineData(429496.72, "¤429,496.72")]
        public void TestFormatterSingle_C(float value, string expectedResult) => TestFormatterSingle_Standard(value, "C", expectedResult);

        [Theory]
        [InlineData(float.Epsilon, "¤0.0000")]
        [InlineData(float.MaxValue, "¤340,282,346,638,528,859,811,704,183,484,516,925,440.0000")]
        [InlineData(MathF.E, "¤2.7183")]
        [InlineData(MathF.PI, "¤3.1416")]
        [InlineData(0.0f, "¤0.0000")]
        [InlineData(0.0046f, "¤0.0046")]
        [InlineData(0.125f, "¤0.1250")]
        [InlineData(0.845512390, "¤0.8455")]
        [InlineData(1.0f, "¤1.0000")]
        [InlineData(429496.72, "¤429,496.7188")]
        public void TestFormatterSingle_C4(float value, string expectedResult) => TestFormatterSingle_Standard(value, "C4", expectedResult);

        [Theory]
        [InlineData(float.Epsilon, "¤0.00000000000000000000")]
        [InlineData(float.MaxValue, "¤340,282,346,638,528,859,811,704,183,484,516,925,440.00000000000000000000")]
        [InlineData(MathF.E, "¤2.71828174591064453125")]
        [InlineData(MathF.PI, "¤3.14159274101257324219")]
        [InlineData(0.0f, "¤0.00000000000000000000")]
        [InlineData(0.0046f, "¤0.00460000010207295418")]
        [InlineData(0.125f, "¤0.12500000000000000000")]
        [InlineData(0.845512390, "¤0.84551239013671875000")]
        [InlineData(1.0f, "¤1.00000000000000000000")]
        [InlineData(429496.72, "¤429,496.71875000000000000000")]
        public void TestFormatterSingle_C20(float value, string expectedResult) => TestFormatterSingle_Standard(value, "C20", expectedResult);

        [Theory]
        [InlineData(float.Epsilon, "1.401298E-045")]
        [InlineData(float.MaxValue, "3.402823E+038")]
        [InlineData(MathF.E, "2.718282E+000")]
        [InlineData(MathF.PI, "3.141593E+000")]
        [InlineData(0.0f, "0.000000E+000")]
        [InlineData(0.0046f, "4.600000E-003")]
        [InlineData(0.125f, "1.250000E-001")]
        [InlineData(0.845512390, "8.455124E-001")]
        [InlineData(1.0f, "1.000000E+000")]
        [InlineData(429496.72, "4.294967E+005")]
        public void TestFormatterSingle_E(float value, string expectedResult) => TestFormatterSingle_Standard(value, "E", expectedResult);

        [Theory]
        [InlineData(float.Epsilon, "1.4013E-045")]
        [InlineData(float.MaxValue, "3.4028E+038")]
        [InlineData(MathF.E, "2.7183E+000")]
        [InlineData(MathF.PI, "3.1416E+000")]
        [InlineData(0.0f, "0.0000E+000")]
        [InlineData(0.0046f, "4.6000E-003")]
        [InlineData(0.125f, "1.2500E-001")]
        [InlineData(0.845512390, "8.4551E-001")]
        [InlineData(1.0f, "1.0000E+000")]
        [InlineData(429496.72, "4.2950E+005")]
        public void TestFormatterSingle_E4(float value, string expectedResult) => TestFormatterSingle_Standard(value, "E4", expectedResult);

        [Theory]
        [InlineData(float.Epsilon, "1.40129846432481707092E-045")]
        [InlineData(float.MaxValue, "3.40282346638528859812E+038")]
        [InlineData(MathF.E, "2.71828174591064453125E+000")]
        [InlineData(MathF.PI, "3.14159274101257324219E+000")]
        [InlineData(0.0f, "0.00000000000000000000E+000")]
        [InlineData(0.0046f, "4.60000010207295417786E-003")]
        [InlineData(0.125f, "1.25000000000000000000E-001")]
        [InlineData(0.845512390, "8.45512390136718750000E-001")]
        [InlineData(1.0f, "1.00000000000000000000E+000")]
        [InlineData(429496.72, "4.29496718750000000000E+005")]
        public void TestFormatterSingle_E20(float value, string expectedResult) => TestFormatterSingle_Standard(value, "E20", expectedResult);

        [Theory]
        [InlineData(float.Epsilon, "0.00")]
        [InlineData(float.MaxValue, "340282346638528859811704183484516925440.00")]
        [InlineData(MathF.E, "2.72")]
        [InlineData(MathF.PI, "3.14")]
        [InlineData(0.0f, "0.00")]
        [InlineData(0.0046f, "0.00")]
        [InlineData(0.125f, "0.12")]
        [InlineData(0.845512390, "0.85")]
        [InlineData(1.0f, "1.00")]
        [InlineData(429496.72, "429496.72")]
        public void TestFormatterSingle_F(float value, string expectedResult) => TestFormatterSingle_Standard(value, "F", expectedResult);

        [Theory]
        [InlineData(float.Epsilon, "0.0000")]
        [InlineData(float.MaxValue, "340282346638528859811704183484516925440.0000")]
        [InlineData(MathF.E, "2.7183")]
        [InlineData(MathF.PI, "3.1416")]
        [InlineData(0.0f, "0.0000")]
        [InlineData(0.0046f, "0.0046")]
        [InlineData(0.125f, "0.1250")]
        [InlineData(0.845512390, "0.8455")]
        [InlineData(1.0f, "1.0000")]
        [InlineData(429496.72, "429496.7188")]
        public void TestFormatterSingle_F4(float value, string expectedResult) => TestFormatterSingle_Standard(value, "F4", expectedResult);

        [Theory]
        [InlineData(float.Epsilon, "0.00000000000000000000")]
        [InlineData(float.MaxValue, "340282346638528859811704183484516925440.00000000000000000000")]
        [InlineData(MathF.E, "2.71828174591064453125")]
        [InlineData(MathF.PI, "3.14159274101257324219")]
        [InlineData(0.0f, "0.00000000000000000000")]
        [InlineData(0.0046f, "0.00460000010207295418")]
        [InlineData(0.125f, "0.12500000000000000000")]
        [InlineData(0.845512390, "0.84551239013671875000")]
        [InlineData(1.0f, "1.00000000000000000000")]
        [InlineData(429496.72, "429496.71875000000000000000")]
        public void TestFormatterSingle_F20(float value, string expectedResult) => TestFormatterSingle_Standard(value, "F20", expectedResult);

        [Theory]
        [InlineData(float.Epsilon, "1E-45")]
        [InlineData(float.MaxValue, "3.4028235E+38")]
        [InlineData(MathF.E, "2.7182817")]
        [InlineData(MathF.PI, "3.1415927")]
        [InlineData(0.0f, "0")]
        [InlineData(0.0046f, "0.0046")]
        [InlineData(0.125f, "0.125")]
        [InlineData(0.845512390, "0.8455124")]
        [InlineData(1.0f, "1")]
        [InlineData(429496.72, "429496.72")]
        public void TestFormatterSingle_G(float value, string expectedResult) => TestFormatterSingle_Standard(value, "G", expectedResult);

        [Theory]
        [InlineData(float.Epsilon, "1.401E-45")]
        [InlineData(float.MaxValue, "3.403E+38")]
        [InlineData(MathF.E, "2.718")]
        [InlineData(MathF.PI, "3.142")]
        [InlineData(0.0f, "0")]
        [InlineData(0.0046f, "0.0046")]
        [InlineData(0.125f, "0.125")]
        [InlineData(0.845512390, "0.8455")]
        [InlineData(1.0f, "1")]
        [InlineData(429496.72, "4.295E+05")]
        public void TestFormatterSingle_G4(float value, string expectedResult) => TestFormatterSingle_Standard(value, "G4", expectedResult);

        [Theory]
        [InlineData(float.Epsilon, "1.4012984643248170709E-45")]
        [InlineData(float.MaxValue, "3.4028234663852885981E+38")]
        [InlineData(MathF.E, "2.7182817459106445312")]
        [InlineData(MathF.PI, "3.1415927410125732422")]
        [InlineData(0.0f, "0")]
        [InlineData(0.0046f, "0.0046000001020729541779")]
        [InlineData(0.125f, "0.125")]
        [InlineData(0.845512390, "0.84551239013671875")]
        [InlineData(1.0f, "1")]
        [InlineData(429496.72, "429496.71875")]
        public void TestFormatterSingle_G20(float value, string expectedResult) => TestFormatterSingle_Standard(value, "G20", expectedResult);

        [Theory]
        [InlineData(float.Epsilon, "0.00")]
        [InlineData(float.MaxValue, "340,282,346,638,528,859,811,704,183,484,516,925,440.00")]
        [InlineData(MathF.E, "2.72")]
        [InlineData(MathF.PI, "3.14")]
        [InlineData(0.0f, "0.00")]
        [InlineData(0.0046f, "0.00")]
        [InlineData(0.125f, "0.12")]
        [InlineData(0.845512390, "0.85")]
        [InlineData(1.0f, "1.00")]
        [InlineData(429496.72, "429,496.72")]
        public void TestFormatterSingle_N(float value, string expectedResult) => TestFormatterSingle_Standard(value, "N", expectedResult);

        [Theory]
        [InlineData(float.Epsilon, "0.0000")]
        [InlineData(float.MaxValue, "340,282,346,638,528,859,811,704,183,484,516,925,440.0000")]
        [InlineData(MathF.E, "2.7183")]
        [InlineData(MathF.PI, "3.1416")]
        [InlineData(0.0f, "0.0000")]
        [InlineData(0.0046f, "0.0046")]
        [InlineData(0.125f, "0.1250")]
        [InlineData(0.845512390, "0.8455")]
        [InlineData(1.0f, "1.0000")]
        [InlineData(429496.72, "429,496.7188")]
        public void TestFormatterSingle_N4(float value, string expectedResult) => TestFormatterSingle_Standard(value, "N4", expectedResult);

        [Theory]
        [InlineData(float.Epsilon, "0.00000000000000000000")]
        [InlineData(float.MaxValue, "340,282,346,638,528,859,811,704,183,484,516,925,440.00000000000000000000")]
        [InlineData(MathF.E, "2.71828174591064453125")]
        [InlineData(MathF.PI, "3.14159274101257324219")]
        [InlineData(0.0f, "0.00000000000000000000")]
        [InlineData(0.0046f, "0.00460000010207295418")]
        [InlineData(0.125f, "0.12500000000000000000")]
        [InlineData(0.845512390, "0.84551239013671875000")]
        [InlineData(1.0f, "1.00000000000000000000")]
        [InlineData(429496.72, "429,496.71875000000000000000")]
        public void TestFormatterSingle_N20(float value, string expectedResult) => TestFormatterSingle_Standard(value, "N20", expectedResult);

        [Theory]
        [InlineData(float.Epsilon, "0.00 %")]
        [InlineData(float.MaxValue, "34,028,234,663,852,885,981,170,418,348,451,692,544,000.00 %")]
        [InlineData(MathF.E, "271.83 %")]
        [InlineData(MathF.PI, "314.16 %")]
        [InlineData(0.0f, "0.00 %")]
        [InlineData(0.0046f, "0.46 %")]
        [InlineData(0.125f, "12.50 %")]
        [InlineData(0.845512390, "84.55 %")]
        [InlineData(1.0f, "100.00 %")]
        [InlineData(429496.72, "42,949,671.88 %")]
        public void TestFormatterSingle_P(float value, string expectedResult) => TestFormatterSingle_Standard(value, "P", expectedResult);

        [Theory]
        [InlineData(float.Epsilon, "0.0000 %")]
        [InlineData(float.MaxValue, "34,028,234,663,852,885,981,170,418,348,451,692,544,000.0000 %")]
        [InlineData(MathF.E, "271.8282 %")]
        [InlineData(MathF.PI, "314.1593 %")]
        [InlineData(0.0f, "0.0000 %")]
        [InlineData(0.0046f, "0.4600 %")]
        [InlineData(0.125f, "12.5000 %")]
        [InlineData(0.845512390, "84.5512 %")]
        [InlineData(1.0f, "100.0000 %")]
        [InlineData(429496.72, "42,949,671.8750 %")]
        public void TestFormatterSingle_P4(float value, string expectedResult) => TestFormatterSingle_Standard(value, "P4", expectedResult);

        [Theory]
        [InlineData(float.Epsilon, "0.00000000000000000000 %")]
        [InlineData(float.MaxValue, "34,028,234,663,852,885,981,170,418,348,451,692,544,000.00000000000000000000 %")]
        [InlineData(MathF.E, "271.82817459106445312500 %")]
        [InlineData(MathF.PI, "314.15927410125732421875 %")]
        [InlineData(0.0f, "0.00000000000000000000 %")]
        [InlineData(0.0046f, "0.46000001020729541779 %")]
        [InlineData(0.125f, "12.50000000000000000000 %")]
        [InlineData(0.845512390, "84.55123901367187500000 %")]
        [InlineData(1.0f, "100.00000000000000000000 %")]
        [InlineData(429496.72, "42,949,671.87500000000000000000 %")]
        public void TestFormatterSingle_P20(float value, string expectedResult) => TestFormatterSingle_Standard(value, "P20", expectedResult);

        [Theory]
        [InlineData(float.Epsilon, "1E-45")]
        [InlineData(float.MaxValue, "3.4028235E+38")]
        [InlineData(MathF.E, "2.7182817")]
        [InlineData(MathF.PI, "3.1415927")]
        [InlineData(0.0f, "0")]
        [InlineData(0.0046f, "0.0046")]
        [InlineData(0.125f, "0.125")]
        [InlineData(0.845512390, "0.8455124")]
        [InlineData(1.0f, "1")]
        [InlineData(429496.72, "429496.72")]
        public void TestFormatterSingle_R(float value, string expectedResult) => TestFormatterSingle_Standard(value, "R", expectedResult);

        [Theory]
        [InlineData(float.Epsilon, "1E-45")]
        [InlineData(float.MaxValue, "3.4028235E+38")]
        [InlineData(MathF.E, "2.7182817")]
        [InlineData(MathF.PI, "3.1415927")]
        [InlineData(0.0f, "0")]
        [InlineData(0.0046f, "0.0046")]
        [InlineData(0.125f, "0.125")]
        [InlineData(0.845512390, "0.8455124")]
        [InlineData(1.0f, "1")]
        [InlineData(429496.72, "429496.72")]
        public void TestFormatterSingle_R4(float value, string expectedResult) => TestFormatterSingle_Standard(value, "R4", expectedResult);

        [Theory]
        [InlineData(float.Epsilon, "1E-45")]
        [InlineData(float.MaxValue, "3.4028235E+38")]
        [InlineData(MathF.E, "2.7182817")]
        [InlineData(MathF.PI, "3.1415927")]
        [InlineData(0.0f, "0")]
        [InlineData(0.0046f, "0.0046")]
        [InlineData(0.125f, "0.125")]
        [InlineData(0.845512390, "0.8455124")]
        [InlineData(1.0f, "1")]
        [InlineData(429496.72, "429496.72")]
        public void TestFormatterSingle_R20(float value, string expectedResult) => TestFormatterSingle_Standard(value, "R20", expectedResult);

        public static IEnumerable<object[]> TestFormatterSingle_InvalidMemberData =>
            from value in new[] { float.Epsilon, float.MaxValue, MathF.E, MathF.PI, 0.0, 0.845512390, 1.0, 429496.72 }
            from format in new[] { "D", "D4", "D20", "X", "X4", "X20" }
            select new object[] { value, format };

        [Theory]
        [MemberData(nameof(TestFormatterSingle_InvalidMemberData))]
        public void TestFormatterSingle_Invalid(float value, string format) => Assert.Throws<FormatException>(() => InvariantToStringSingle(value, format));

        private void TestFormatterSingle_Standard(float value, string format, string expectedResult)
        {
            string actualResult = InvariantToStringSingle(value, format);
            Assert.Equal(expectedResult, actualResult);
        }
    }
}
