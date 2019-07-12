// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Xunit;

namespace Microsoft.VisualBasic.Tests
{
    public class FinancialTests
    {
        private static bool IsNotArmOrAlpine() => !PlatformDetection.IsArmOrArm64Process && !PlatformDetection.IsAlpine;

        /// <summary>
        /// The accuracy of some numeric parsing and formatting has been increased,
        /// so this will use correct value if the tests run on Full Framework.
        /// </summary>
        private static void AreEqual<T>(T expectedOld, T expectedNew, T actual)
        {
            T expected = PlatformDetection.IsFullFramework ? expectedOld : expectedNew;
            Assert.Equal(expected, actual);
        }

        [ConditionalTheory(nameof(IsNotArmOrAlpine))] // some tests fail due to precision
        [InlineData(0, 1.0, 1.0, 1.0, 1.0, 0, 0)]
        [InlineData(2000.0, 500.0, 2.0, 1.0, 2.0, 1500.0, 1500.0)]
        [InlineData(10000.0, 4350.0, 84.0, 35.0, 2.0, 57.3268063538875, 57.32680635388748)]
        [InlineData(15006.0, 6350.0, 81.0, 23.0, 1.5, 184.194898366662, 184.19489836666187)]
        [InlineData(11606.0, 6350.0, 74.0, 17.0, 2.1, 207.790109365989, 207.79010936598854)]
        [InlineData(11606.0, 16245.0, 71.0, 17.0, 2.0, 0, 0)] // cost < salvage
        [InlineData(10100.0, 10100.0, 70.0, 20.0, 2.0, 0, 0)] // cost = salvage
        public void DDB(double Cost, double Salvage, double Life, double Period, double Factor, double expectedOld, double expectedNew)
        {
            AreEqual(expectedOld, expectedNew, Financial.DDB(Cost, Salvage, Life, Period, Factor));
        }

        [Fact]
        public void DDB_Default()
        {
            AreEqual(57.3268063538875, 57.32680635388748, Financial.DDB(10000.0, 4350.0, 84.0, 35.0));
        }

        [Theory]
        [InlineData(-10000, 5211, -81, 35, 2)]
        public void DDB_Invalid(double Cost, double Salvage, double Life, double Period, double Factor)
        {
            Assert.Throws<ArgumentException>(() => Financial.DDB(Cost, Salvage, Life, Period, Factor));
        }

        [Theory]
        [InlineData(0, 0, 0, 0, DueDate.EndOfPeriod, 0, 0)]
        [InlineData(0.02 / 12, 12.0, -100.0, -100.0, DueDate.BegOfPeriod, 1315.09821202641, 1315.0982120264073)]
        [InlineData(0.0083, 15, 263.0, 0, DueDate.EndOfPeriod, -4182.65729113816, -4182.657291138164)]
        [InlineData(0.013, 90, 81.0, 5000.0, DueDate.BegOfPeriod, -29860.9051541809, -29860.905154180855)]
        [InlineData(0.01, 37, 100.0, 0, DueDate.BegOfPeriod, -4495.27236141772, -4495.2723614177185)]
        [InlineData(-0.0083, 15, 263.0, 0, DueDate.EndOfPeriod, -3723.83765008048, -3723.837650080481)] // rate < 0
        [InlineData(0, 15, 263, 0, DueDate.EndOfPeriod, -3945, -3945)] // rate = 0
        [InlineData(0.0083, 15, 263.0, 0, 8, -4217.37334665461, -4217.37334665461)] // type <> 0 and type <> 1
        [InlineData(1e+25, 12, 1797, 0, 1, -1.797e+303, -1.797000000000002e+303)] // overFlow
        public void FV(double Rate, double NPer, double Pmt, double PV, DueDate Due, double expectedOld, double expectedNew)
        {
            AreEqual(expectedOld, expectedNew, Financial.FV(Rate, NPer, Pmt, PV, Due));
        }

        [Fact]
        public void FV_Default()
        {
            AreEqual(-4182.65729113816, -4182.657291138164, Financial.FV(0.0083, 15, 263.0, 0));
            AreEqual(-4182.65729113816, -4182.657291138164, Financial.FV(0.0083, 15, 263.0));
        }

        [ConditionalTheory(nameof(IsNotArmOrAlpine))] // some tests fail due to precision
        [InlineData(0, 1.0, 1.0, 0, 0, DueDate.EndOfPeriod, 0, 0)]
        [InlineData(0.1 / 12, 12.0, 48.0, -20000.0, 0, DueDate.BegOfPeriod, 133.00409235109, 133.00409235108953)]
        [InlineData(0.008, 4, 12, 3000, 0, 0, -18.2133959841799, -18.21339598417987)]
        [InlineData(0.012, 15, 79, 2387, 200, 1, -24.7440714828291, -24.74407148282907)]
        [InlineData(0.0096, 54, 123, 4760, 0, 0, -32.2391587542905, -32.23915875429054)]
        [InlineData(-0.008, 4, 12, 3000, 0, 0, 17.7814213334481, 17.781421333448126)] // rate < 0
        [InlineData(0, 4, 12, 3000, 0, 0, 0, 0)] // rate = 0
        [InlineData(0.008, 4, 12, 3000, 0, 7, -18.0688452224006, -18.068845222400633)] // type <> 0 and type <> 1
        public void IPmt(double Rate, double Per, double NPer, double PV, double FV, DueDate Due, double expectedOld, double expectedNew)
        {
            AreEqual(expectedOld, expectedNew, Financial.IPmt(Rate, Per, NPer, PV, FV, Due));
        }

        [Fact]
        public void IPmt_Default()
        {
            const double expectedOld = -18.2133959841799;
            const double expectedNew = -18.21339598417987;
            AreEqual(expectedOld, expectedNew, Financial.IPmt(0.008, 4, 12, 3000, 0));
            AreEqual(expectedOld, expectedNew, Financial.IPmt(0.008, 4, 12, 3000));
        }

        [Theory]
        [InlineData(0.008, -4, 12, 3000, 0, 0)]
        [InlineData(0.008, 4, -12, 3000, 0, 0)]
        [InlineData(0.008, 12, 4, 3000, 0, 0)]
        public void IPmt_Invalid(double Rate, double Per, double NPer, double PV, double FV, DueDate Due)
        {
            Assert.Throws<ArgumentException>(() => Financial.IPmt(Rate, Per, NPer, PV, FV, Due));
        }

        [Theory]
        [InlineData(new double[] { -1, 1 }, 0, 0, 0)]
        [InlineData(new double[] { -70000.0, 22000.0, 25000.0, 28000.0, 31000.0 }, 0.1, 0.177435884421108, 0.177435884421108)]
        [InlineData(new double[] { -10000.0, 6000.0, -2000.0, 7000.0, 1000.0 }, 0.1, 0.0866720474291716, 0.08667204742917164)]
        [InlineData(new double[] { -30000.0, -10000.0, 25000.0, 12000.0, 15000.0 }, 0.1, 0.10928101434576, 0.10928101434575987)]
        public void IRR(double[] ValueArray, double Guess, double expectedOld, double expectedNew)
        {
            AreEqual(expectedOld, expectedNew, Financial.IRR(ref ValueArray, Guess));
        }

        [Fact]
        public void IRR_Default()
        {
            double[] ValueArray = new double[] { -70000.0, 22000.0, 25000.0, 28000.0, 31000.0 };
            AreEqual(0.177435884421108, 0.177435884421108, Financial.IRR(ref ValueArray));
        }

        [Theory]
        [InlineData(new double[0])]
        [InlineData(new double[] { 70000, 22000, 25000, 28000, 31000 })]
        [InlineData(new double[] { -70000, -22000, -25000, -28000, -31000 })]
        public void IRR_Invalid(double[] ValueArray)
        {
            Assert.Throws<ArgumentException>(() => Financial.IRR(ref ValueArray));
        }

        [Theory]
        [InlineData(new double[] { -1, 1 }, 0, 0, 0, 0)]
        [InlineData(new double[] { -70000.0, 22000.0, 25000.0, 28000.0, 31000.0 }, 0.1, 0.12, 0.155127062819277, 0.15512706281927668)]
        public void MIRR(double[] ValueArray, double FinanceRate, double ReinvestRate, double expectedOld, double expectedNew)
        {
            AreEqual(expectedOld, expectedNew, Financial.MIRR(ref ValueArray, FinanceRate, ReinvestRate));
        }

        [Theory]
        [InlineData(new double[] { 70000, 22000, 25000, 28000, 31000 }, 0.1, 0.12)]
        public void MIRR_Invalid(double[] ValueArray, double FinanceRate, double ReinvestRate)
        {
            Assert.Throws<DivideByZeroException>(() => Financial.MIRR(ref ValueArray, FinanceRate, ReinvestRate));
        }

        [Theory]
        [InlineData(0, 1.0, 0, 0, DueDate.EndOfPeriod, 0, 0)]
        [InlineData(0.02 / 12, -800.0, 10000, 0, DueDate.BegOfPeriod, 12.6213107881059, 12.621310788105905)]
        [InlineData(0.0072, -350.0, 7000.0, 0, DueDate.EndOfPeriod, 21.6727748893013, 21.672774889301333)]
        [InlineData(0.018, -982.0, 33000.0, 2387, DueDate.BegOfPeriod, 52.9127060554833, 52.91270605548329)]
        [InlineData(0.0096, 1500.0, -70000.0, 10000, DueDate.EndOfPeriod, 55.2706372559078, 55.2706372559078)]
        [InlineData(-0.0072, -350.0, 7000.0, 0, DueDate.EndOfPeriod, 18.6174997871788, 18.617499787178836)] // rate < 0
        [InlineData(0, -350.0, 7000.0, 0, DueDate.EndOfPeriod, 20, 20)] // rate = 0
        [InlineData(0.0072, -350.0, 7000.0, 0, 7, 21.5052532943763, 21.505253294376303)] // type <> 0 and type <> 1
        [InlineData(0.0072, -9000.0, 200.0, 0, DueDate.EndOfPeriod, 0.0223039109268324, 0.02230391092683241)] // pmt > pv
        public void NPer(double Rate, double Pmt, double PV, double FV, DueDate Due, double expectedOld, double expectedNew)
        {
            AreEqual(expectedOld, expectedNew, Financial.NPer(Rate, Pmt, PV, FV, Due));
        }

        [Fact]
        public void NPer_Default()
        {
            AreEqual(21.6727748893013, 21.672774889301333, Financial.NPer(0.0072, -350.0, 7000.0, 0));
            AreEqual(21.6727748893013, 21.672774889301333, Financial.NPer(0.0072, -350.0, 7000.0));
        }

        [Theory]
        [InlineData(0, new double[] { 0 }, 0, 0)]
        [InlineData(0.1, new double[] { -70000.0, 22000.0, 25000.0, 28000.0, 31000.0 }, 11701.2623330498, 11701.262333049774)]
        [InlineData(0.0625, new double[] { -70000.0, 22000.0, 25000.0, 28000.0, 31000.0 }, 19312.5702095352, 19312.570209535177)]
        [InlineData(0.089, new double[] { -10000, 6000, -2000, 7000, 1000 }, -41.8655316029477, -41.865531602947726)]
        [InlineData(0.011, new double[] { -30000, -10000, 25000, 12000, 15000 }, 10423.4025719865, 10423.40257198653)]
        [InlineData(-0.011, new double[] { -30000, -10000, 25000, 12000, 15000 }, 13681.9191276061, 13681.919127606126)] // rate < 0
        [InlineData(0.0625, new double[] { 70000.0, 22000.0, 25000.0, 28000.0, 31000.0 }, 151077.276091888, 151077.27609188814)] // all positive cash flows
        [InlineData(0.0625, new double[] { -70000.0, -22000.0, -25000.0, -28000.0, -31000.0 }, -151077.276091888, -151077.27609188814)] // all negative cash flows
        public void NPV(double Rate, double[] ValueArray, double expectedOld, double expectedNew)
        {
            AreEqual(expectedOld, expectedNew, Financial.NPV(Rate, ref ValueArray));
        }

        [Theory]
        [InlineData(-1, new double[0])]
        [InlineData(0.12, new double[0])]
        public void NPV_Invalid(double Rate, double[] ValueArray)
        {
            Assert.Throws<ArgumentException>(() => Financial.NPV(Rate, ref ValueArray));
        }

        [Theory]
        [InlineData(0, 1.0, 0, 0, DueDate.EndOfPeriod, 0, 0)]
        [InlineData(0.02 / 12, 24, -10000, 0, DueDate.BegOfPeriod, 424.694809003121, 424.6948090031214)]
        [InlineData(0.007, 25, -3000, 0, DueDate.EndOfPeriod, 131.224540233228, 131.2245402332282)]
        [InlineData(0.019, 70, 80000, 20000, DueDate.BegOfPeriod, -2173.61322345131, -2173.613223451309)]
        [InlineData(0.0012, 5, 500, 0, DueDate.EndOfPeriod, -100.360287827152, -100.36028782715209)]
        [InlineData(-0.007, 25, -3000, 0, DueDate.EndOfPeriod, 109.386677326841, 109.38667732684138)] // rate < 0
        [InlineData(0.007, -25, 3000, 0, DueDate.EndOfPeriod, 110.224540233228, 110.22454023322811)] // nper < 0
        [InlineData(0.007, 25, 3000, 0, 7, -130.31235375693, -130.31235375692967)] // type <> 0 and type <> 1
        [InlineData(0, 25, 3000, 0, DueDate.EndOfPeriod, -120, -120)] // rate = 0
        public void Pmt(double Rate, double NPer, double PV, double FV, DueDate Due, double expectedOld, double expectedNew)
        {
            AreEqual(expectedOld, expectedNew, Financial.Pmt(Rate, NPer, PV, FV, Due));
        }

        [Fact]
        public void Pmt_Default()
        {
            AreEqual(131.224540233228, 131.2245402332282, Financial.Pmt(0.007, 25, -3000, 0));
            AreEqual(131.224540233228, 131.2245402332282, Financial.Pmt(0.007, 25, -3000));
        }

        [ConditionalTheory(nameof(IsNotArmOrAlpine))] // some tests fail due to precision
        [InlineData(0, 1.0, 1.0, 0, 0, DueDate.EndOfPeriod, 0, 0)]
        [InlineData(0.02 / 12, 1.0, 24, -10000, 0, DueDate.BegOfPeriod, 424.694809003121, 424.6948090031214)]
        [InlineData(0.008, 4, 12, 3000, 0, DueDate.EndOfPeriod, -244.976482926292, -244.97648292629228)]
        [InlineData(0.012, 15, 79, 2387, 200, DueDate.BegOfPeriod, -23.1486833595771, -23.14868335957714)]
        [InlineData(0.0096, 54, 123, 4760, 0, DueDate.EndOfPeriod, -33.8688007923638, -33.86880079236377)]
        [InlineData(-0.008, 4, 12, 3000, 0, DueDate.EndOfPeriod, -254.972824918514, -254.97282491851354)] // rate < 0
        [InlineData(0.008, 4, 12, 3000, 0, 7, -243.03222512529, -243.03222512529004)] // type <> 0 and type <> 1
        public void PPmt(double Rate, double Per, double NPer, double PV, double FV, DueDate Due, double expectedOld, double expectedNew)
        {
            AreEqual(expectedOld, expectedNew, Financial.PPmt(Rate, Per, NPer, PV, FV, Due));
        }

        [Fact]
        public void PPmt_Default()
        {
            AreEqual(-244.976482926292, -244.97648292629228, Financial.PPmt(0.008, 4, 12, 3000, 0));
            AreEqual(-244.976482926292, -244.97648292629228, Financial.PPmt(0.008, 4, 12, 3000));
        }

        [ConditionalTheory(nameof(IsNotArmOrAlpine))] // some tests fail due to precision
        [InlineData(0, 0, 0, 0, DueDate.EndOfPeriod, 0, 0)]
        [InlineData(0.02 / 12, 12.0, -100.0, -100.0, DueDate.BegOfPeriod, 1287.10048252122, 1287.1004825212165)]
        [InlineData(0.008, 31, 2000.0, 0, DueDate.EndOfPeriod, -54717.4159104136, -54717.41591041358)]
        [InlineData(0.012, 15, 780.0, 2000.0, DueDate.BegOfPeriod, -12449.3560902104, -12449.356090210378)]
        [InlineData(0.0096, 54, 123.0, 4760.0, DueDate.EndOfPeriod, -8005.58690087404, -8005.586900874043)]
        [InlineData(-0.008, 31, 2000.0, 0, DueDate.EndOfPeriod, -70684.6496700943, -70684.64967009431)] // rate < 0
        [InlineData(0, 31, 2000.0, 0, DueDate.EndOfPeriod, -62000, -62000)] // rate = 0
        [InlineData(0.008, -31, 2000.0, 0, DueDate.EndOfPeriod, 70049.0217362546, 70049.02173625457)] // nper < 0
        [InlineData(1E25, 12, 1797, 0, DueDate.BegOfPeriod, -1797, -1797)] // overflow
        public void PV(double Rate, double NPer, double Pmt, double FV, DueDate Due, double expectedOld, double expectedNew)
        {
            AreEqual(expectedOld, expectedNew, Financial.PV(Rate, NPer, Pmt, FV, Due));
        }

        [Fact]
        public void PV_Default()
        {
            AreEqual(-2952.94485232014, -2952.944852320145, Financial.PV(0.008, 4, 12, 3000, 0));
            AreEqual(-2952.94485232014, -2952.944852320145, Financial.PV(0.008, 4, 12, 3000));
        }

        [ConditionalTheory(nameof(IsNotArmOrAlpine))] // some tests fail due to precision
        [InlineData(1.0, 1.0, 1.0, 0, DueDate.EndOfPeriod, 0, -2, -2)]
        [InlineData(24.0, -800.0, 10000.0, 0.0, DueDate.BegOfPeriod, 0.1, 0.0676702786565114, 0.06767027865651142)]
        [InlineData(12, -263.0, 3000, 0, DueDate.EndOfPeriod, 0.1, 0.00788643837763396, 0.007886438377633958)]
        [InlineData(48, -570, 24270.0, 0, DueDate.BegOfPeriod, 0.1, 0.00522401643399906, 0.005224016433999085)]
        [InlineData(96, -1000.0, 56818, 0, DueDate.EndOfPeriod, 0.1, 0.0120002073302547, 0.012000207330254708)]
        [InlineData(12, -3000.0, 300, 0, DueDate.EndOfPeriod, 0.1, -1.98502387722876, -1.9850238772287565)] // pmt > pv
        public void Rate(double NPer, double Pmt, double PV, double FV, DueDate Due, double Guess, double expectedOld, double expectedNew)
        {
            AreEqual(expectedOld, expectedNew, Financial.Rate(NPer, Pmt, PV, FV, Due, Guess));
        }

        [Fact]
        public void Rate_Default()
        {
            AreEqual(0.00788643837763396, 0.007886438377633958, Financial.Rate(12, -263.0, 3000, 0));
            AreEqual(0.00788643837763396, 0.007886438377633958, Financial.Rate(12, -263.0, 3000));
        }

        [Fact]
        public void Rate_Invalid()
        {
            Assert.Throws<ArgumentException>(() => Financial.Rate(-12, -263.0, 3000, 0, 0, 0.1));
        }

        [Theory]
        [InlineData(1.0, 1.0, 1.0, 0, 0)]
        [InlineData(1000.0, 800.0, 50.0, 4.0, 4.0)]
        [InlineData(5000, 1000, 20, 200, 200)]
        [InlineData(54870, 21008, 7, 4837.42857142857, 4837.428571428572)]
        [InlineData(2, 1.1, 12, 0.075, 0.075)]
        [InlineData(1000, 5000, 20, -200, -200)] // salvage > cost
        [InlineData(5000, 1000, -20, -200, -200)] // life < 0
        [InlineData(5000, 0, 12, 416.666666666667, 416.6666666666667)] // salvage = 0
        [InlineData(-5000, -1000, -20, 200, 200)] // all parameter -ve
        public void SLN(double Cost, double Salvage, double Life, double expectedOld, double expectedNew)
        {
            AreEqual(expectedOld, expectedNew, Financial.SLN(Cost, Salvage, Life));
        }

        [Theory]
        [InlineData(1.0, 1.0, 1.0, 1.0, 0, 0)]
        [InlineData(1000.0, 800.0, 50, 2.0, 7.68627450980392, 7.686274509803922)]
        [InlineData(4322.0, 1009.0, 73, 23, 62.5557201036653, 62.55572010366531)]
        [InlineData(78000.0, 21008, 8, 2, 11081.7777777778, 11081.777777777777)]
        [InlineData(23.0, 7.0, 21, 9, 0.9004329004329, 0.9004329004329005)]
        [InlineData(9.9999999999999e+305, 0, 100, 10, 1.80198019801978e+304, 1.801980198019784e+304)] // overflow
        [InlineData(1009.0, 4322.0, 73, 23, -62.5557201036653, -62.55572010366531)] // salvage > cost
        public void SYD(double Cost, double Salvage, double Life, double Period, double expectedOld, double expectedNew)
        {
            AreEqual(expectedOld, expectedNew, Financial.SYD(Cost, Salvage, Life, Period));
        }

        [Theory]
        [InlineData(4322.0, 1009.0, 23, 73)]
        [InlineData(4322.0, 1009.0, 0, 23)]
        [InlineData(4322.0, 1009.0, 73, 0)]
        [InlineData(-4322.0, -1009.0, -73, -23)]
        [InlineData(0.0, 0.0, 0, 0)]
        public void SYD_Invalid(double Cost, double Salvage, double Life, double Period)
        {
            Assert.Throws<ArgumentException>(() => Financial.SYD(Cost, Salvage, Life, Period));
        }
    }
}
