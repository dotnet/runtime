// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Microsoft.VisualBasic.Tests
{
    public class FinancialTests
    {
        private static bool IsArmOrArm64OrAlpine { get => PlatformDetection.IsAlpine || PlatformDetection.IsArmOrArm64Process; }
        private static bool IsNotArmNorArm64NorAlpine { get => !IsArmOrArm64OrAlpine; }

        // The accuracy to which we can validate some numeric test cases depends on the platform.
        private static readonly int s_precision = IsArmOrArm64OrAlpine ? 12 :
            (PlatformDetection.IsBrowser || PlatformDetection.IsNetFramework) ? 14 : 15;

        [Theory]
        [InlineData(0, 1.0, 1.0, 1.0, 1.0, 0, 0)]
        [InlineData(2000.0, 500.0, 2.0, 1.0, 2.0, 1500.0, -4)]
        [InlineData(10000.0, 4350.0, 84.0, 35.0, 2.0, 57.32680635388748, -2)]
        [InlineData(15006.0, 6350.0, 81.0, 23.0, 1.5, 184.19489836666187, -3)]
        [InlineData(11606.0, 6350.0, 74.0, 17.0, 2.1, 207.79010936598854, -3)]
        [InlineData(11606.0, 16245.0, 71.0, 17.0, 2.0, 0, -1)] // cost < salvage
        [InlineData(10100.0, 10100.0, 70.0, 20.0, 2.0, 0, -1)] // cost = salvage
        public void DDB(double Cost, double Salvage, double Life, double Period, double Factor, double expected, int relativePrecision)
        {
            Assert.Equal(expected, Financial.DDB(Cost, Salvage, Life, Period, Factor), s_precision + relativePrecision);
        }

        [Fact]
        public void DDB_Default()
        {
            Assert.Equal(57.32680635388748, Financial.DDB(10000.0, 4350.0, 84.0, 35.0), s_precision - 2);
        }

        [Theory]
        [InlineData(-10000, 5211, -81, 35, 2)]
        public void DDB_Invalid(double Cost, double Salvage, double Life, double Period, double Factor)
        {
            Assert.Throws<ArgumentException>(() => Financial.DDB(Cost, Salvage, Life, Period, Factor));
        }

        [Theory]
        [InlineData(0, 0, 0, 0, DueDate.EndOfPeriod, 0, 0)]
        [InlineData(0.02 / 12, 12.0, -100.0, -100.0, DueDate.BegOfPeriod, 1315.0982120264073, -4)]
        [InlineData(0.0083, 15, 263.0, 0, DueDate.EndOfPeriod, -4182.657291138164, -4)]
        [InlineData(0.013, 90, 81.0, 5000.0, DueDate.BegOfPeriod, -29860.905154180855, -5)]
        [InlineData(0.01, 37, 100.0, 0, DueDate.BegOfPeriod, -4495.2723614177185, -4)]
        [InlineData(-0.0083, 15, 263.0, 0, DueDate.EndOfPeriod, -3723.837650080481, -4)] // rate < 0
        [InlineData(0, 15, 263, 0, DueDate.EndOfPeriod, -3945, -4)] // rate = 0
        [InlineData(0.0083, 15, 263.0, 0, (DueDate)8, -4217.37334665461, -4)] // type <> 0 and type <> 1
        public void FV(double Rate, double NPer, double Pmt, double PV, DueDate Due, double expected, int relativePrecision)
        {
            Assert.Equal(expected, Financial.FV(Rate, NPer, Pmt, PV, Due), s_precision + relativePrecision);
        }

        [ConditionalTheory(nameof(IsNotArmNorArm64NorAlpine))]
        [InlineData(1e+25, 12, 1797, 0, (DueDate)1, -1.797000000000002e+303, -10)] // overFlow
        public void FV_Overflow(double Rate, double NPer, double Pmt, double PV, DueDate Due, double expected, int relativePrecision)
        {
            Assert.Equal(expected, Financial.FV(Rate, NPer, Pmt, PV, Due), s_precision + relativePrecision);
        }

        [Fact]
        public void FV_Default()
        {
            Assert.Equal(-4182.657291138164, Financial.FV(0.0083, 15, 263.0, 0), s_precision - 4);
            Assert.Equal(-4182.657291138164, Financial.FV(0.0083, 15, 263.0), s_precision - 4);
        }

        [Theory]
        [InlineData(0, 1.0, 1.0, 0, 0, DueDate.EndOfPeriod, 0, 0)]
        [InlineData(0.1 / 12, 12.0, 48.0, -20000.0, 0, DueDate.BegOfPeriod, 133.00409235108953, -3)]
        [InlineData(0.008, 4, 12, 3000, 0, (DueDate)0, -18.21339598417987, -2)]
        [InlineData(0.012, 15, 79, 2387, 200, (DueDate)1, -24.74407148282907, -2)]
        [InlineData(0.0096, 54, 123, 4760, 0, (DueDate)0, -32.23915875429054, -2)]
        [InlineData(-0.008, 4, 12, 3000, 0, (DueDate)0, 17.781421333448126, -2)] // rate < 0
        [InlineData(0, 4, 12, 3000, 0, (DueDate)0, 0, 0)] // rate = 0
        [InlineData(0.008, 4, 12, 3000, 0, (DueDate)7, -18.068845222400633, -2)] // type <> 0 and type <> 1
        public void IPmt(double Rate, double Per, double NPer, double PV, double FV, DueDate Due, double expected, int relativePrecision)
        {
            Assert.Equal(expected, Financial.IPmt(Rate, Per, NPer, PV, FV, Due), s_precision + relativePrecision);
        }

        [Fact]
        public void IPmt_Default()
        {
            Assert.Equal(-18.21339598417987, Financial.IPmt(0.008, 4, 12, 3000, 0), s_precision - 2);
            Assert.Equal(-18.21339598417987, Financial.IPmt(0.008, 4, 12, 3000), s_precision - 2);
        }

        [Theory]
        [InlineData(0.008, -4, 12, 3000, 0, (DueDate)0)]
        [InlineData(0.008, 4, -12, 3000, 0, (DueDate)0)]
        [InlineData(0.008, 12, 4, 3000, 0, (DueDate)0)]
        public void IPmt_Invalid(double Rate, double Per, double NPer, double PV, double FV, DueDate Due)
        {
            Assert.Throws<ArgumentException>(() => Financial.IPmt(Rate, Per, NPer, PV, FV, Due));
        }

        [Theory]
        [InlineData(new double[] { -1, 1 }, 0, 0, 0)]
        [InlineData(new double[] { -70000.0, 22000.0, 25000.0, 28000.0, 31000.0 }, 0.1, 0.177435884421108, 0)]
        [InlineData(new double[] { -10000.0, 6000.0, -2000.0, 7000.0, 1000.0 }, 0.1, 0.08667204742917164, 0)]
        [InlineData(new double[] { -30000.0, -10000.0, 25000.0, 12000.0, 15000.0 }, 0.1, 0.10928101434575987, 0)]
        public void IRR(double[] ValueArray, double Guess, double expected, int relativePrecision)
        {
            Assert.Equal(expected, Financial.IRR(ref ValueArray, Guess), s_precision + relativePrecision);
        }

        [Fact]
        public void IRR_Default()
        {
            double[] ValueArray = new double[] { -70000.0, 22000.0, 25000.0, 28000.0, 31000.0 };
            Assert.Equal(0.177435884421108, Financial.IRR(ref ValueArray), s_precision - 0);
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
        [InlineData(new double[] { -70000.0, 22000.0, 25000.0, 28000.0, 31000.0 }, 0.1, 0.12, 0.15512706281927668, 0)]
        public void MIRR(double[] ValueArray, double FinanceRate, double ReinvestRate, double expected, int relativePrecision)
        {
            Assert.Equal(expected, Financial.MIRR(ref ValueArray, FinanceRate, ReinvestRate), s_precision + relativePrecision);
        }

        [Theory]
        [InlineData(new double[] { 70000, 22000, 25000, 28000, 31000 }, 0.1, 0.12)]
        public void MIRR_Invalid(double[] ValueArray, double FinanceRate, double ReinvestRate)
        {
            Assert.Throws<DivideByZeroException>(() => Financial.MIRR(ref ValueArray, FinanceRate, ReinvestRate));
        }

        [Theory]
        [InlineData(0, 1.0, 0, 0, DueDate.EndOfPeriod, 0, 0)]
        [InlineData(0.02 / 12, -800.0, 10000, 0, DueDate.BegOfPeriod, 12.621310788105905, -2)]
        [InlineData(0.0072, -350.0, 7000.0, 0, DueDate.EndOfPeriod, 21.672774889301333, -2)]
        [InlineData(0.018, -982.0, 33000.0, 2387, DueDate.BegOfPeriod, 52.91270605548329, -2)]
        [InlineData(0.0096, 1500.0, -70000.0, 10000, DueDate.EndOfPeriod, 55.2706372559078, -2)]
        [InlineData(-0.0072, -350.0, 7000.0, 0, DueDate.EndOfPeriod, 18.617499787178836, -2)] // rate < 0
        [InlineData(0, -350.0, 7000.0, 0, DueDate.EndOfPeriod, 20, -2)] // rate = 0
        [InlineData(0.0072, -350.0, 7000.0, 0, (DueDate)7, 21.505253294376303, -2)] // type <> 0 and type <> 1
        [InlineData(0.0072, -9000.0, 200.0, 0, DueDate.EndOfPeriod, 0.02230391092683241, 0)] // pmt > pv
        public void NPer(double Rate, double Pmt, double PV, double FV, DueDate Due, double expected, int relativePrecision)
        {
            Assert.Equal(expected, Financial.NPer(Rate, Pmt, PV, FV, Due), s_precision + relativePrecision);
        }

        [Fact]
        public void NPer_Default()
        {
            Assert.Equal(21.672774889301333, Financial.NPer(0.0072, -350.0, 7000.0, 0), s_precision - 2);
            Assert.Equal(21.672774889301333, Financial.NPer(0.0072, -350.0, 7000.0), s_precision - 2);
        }

        [Theory]
        [InlineData(0, new double[] { 0 }, 0, 0)]
        [InlineData(0.1, new double[] { -70000.0, 22000.0, 25000.0, 28000.0, 31000.0 }, 11701.262333049774, -5)]
        [InlineData(0.0625, new double[] { -70000.0, 22000.0, 25000.0, 28000.0, 31000.0 }, 19312.570209535177, -5)]
        [InlineData(0.089, new double[] { -10000, 6000, -2000, 7000, 1000 }, -41.865531602947726, -2)]
        [InlineData(0.011, new double[] { -30000, -10000, 25000, 12000, 15000 }, 10423.40257198653, -5)]
        [InlineData(-0.011, new double[] { -30000, -10000, 25000, 12000, 15000 }, 13681.919127606126, -5)] // rate < 0
        [InlineData(0.0625, new double[] { 70000.0, 22000.0, 25000.0, 28000.0, 31000.0 }, 151077.27609188814, -6)] // all positive cash flows
        [InlineData(0.0625, new double[] { -70000.0, -22000.0, -25000.0, -28000.0, -31000.0 }, -151077.27609188814, -6)] // all negative cash flows
        public void NPV(double Rate, double[] ValueArray, double expected, int relativePrecision)
        {
            Assert.Equal(expected, Financial.NPV(Rate, ref ValueArray), s_precision + relativePrecision);
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
        [InlineData(0.02 / 12, 24, -10000, 0, DueDate.BegOfPeriod, 424.6948090031214, -3)]
        [InlineData(0.007, 25, -3000, 0, DueDate.EndOfPeriod, 131.2245402332282, -3)]
        [InlineData(0.019, 70, 80000, 20000, DueDate.BegOfPeriod, -2173.613223451309, -4)]
        [InlineData(0.0012, 5, 500, 0, DueDate.EndOfPeriod, -100.36028782715209, -3)]
        [InlineData(-0.007, 25, -3000, 0, DueDate.EndOfPeriod, 109.38667732684138, -3)] // rate < 0
        [InlineData(0.007, -25, 3000, 0, DueDate.EndOfPeriod, 110.22454023322811, -3)] // nper < 0
        [InlineData(0.007, 25, 3000, 0, (DueDate)7, -130.31235375692967, -3)] // type <> 0 and type <> 1
        [InlineData(0, 25, 3000, 0, DueDate.EndOfPeriod, -120, -3)] // rate = 0
        public void Pmt(double Rate, double NPer, double PV, double FV, DueDate Due, double expected, int relativePrecision)
        {
            Assert.Equal(expected, Financial.Pmt(Rate, NPer, PV, FV, Due), s_precision + relativePrecision);
        }

        [Fact]
        public void Pmt_Default()
        {
            Assert.Equal(131.2245402332282, Financial.Pmt(0.007, 25, -3000, 0), s_precision - 3);
            Assert.Equal(131.2245402332282, Financial.Pmt(0.007, 25, -3000), s_precision - 3);
        }

        [Theory]
        [InlineData(0, 1.0, 1.0, 0, 0, DueDate.EndOfPeriod, 0, 0)]
        [InlineData(0.02 / 12, 1.0, 24, -10000, 0, DueDate.BegOfPeriod, 424.6948090031214, -3)]
        [InlineData(0.008, 4, 12, 3000, 0, DueDate.EndOfPeriod, -244.97648292629228, -3)]
        [InlineData(0.012, 15, 79, 2387, 200, DueDate.BegOfPeriod, -23.14868335957714, -2)]
        [InlineData(0.0096, 54, 123, 4760, 0, DueDate.EndOfPeriod, -33.86880079236377, -2)]
        [InlineData(-0.008, 4, 12, 3000, 0, DueDate.EndOfPeriod, -254.97282491851354, -3)] // rate < 0
        [InlineData(0.008, 4, 12, 3000, 0, (DueDate)7, -243.03222512529004, -3)] // type <> 0 and type <> 1
        public void PPmt(double Rate, double Per, double NPer, double PV, double FV, DueDate Due, double expected, int relativePrecision)
        {
            Assert.Equal(expected, Financial.PPmt(Rate, Per, NPer, PV, FV, Due), s_precision + relativePrecision);
        }

        [Fact]
        public void PPmt_Default()
        {
            Assert.Equal(-244.97648292629228, Financial.PPmt(0.008, 4, 12, 3000, 0), s_precision - 3);
            Assert.Equal(-244.97648292629228, Financial.PPmt(0.008, 4, 12, 3000), s_precision - 3);
        }

        [Theory]
        [InlineData(0, 0, 0, 0, DueDate.EndOfPeriod, 0, 0)]
        [InlineData(0.02 / 12, 12.0, -100.0, -100.0, DueDate.BegOfPeriod, 1287.1004825212165, -4)]
        [InlineData(0.008, 31, 2000.0, 0, DueDate.EndOfPeriod, -54717.41591041358, -5)]
        [InlineData(0.012, 15, 780.0, 2000.0, DueDate.BegOfPeriod, -12449.356090210378, -5)]
        [InlineData(0.0096, 54, 123.0, 4760.0, DueDate.EndOfPeriod, -8005.586900874043, -4)]
        [InlineData(-0.008, 31, 2000.0, 0, DueDate.EndOfPeriod, -70684.64967009431, -5)] // rate < 0
        [InlineData(0, 31, 2000.0, 0, DueDate.EndOfPeriod, -62000, -5)] // rate = 0
        [InlineData(0.008, -31, 2000.0, 0, DueDate.EndOfPeriod, 70049.02173625457, -5)] // nper < 0
        [InlineData(1E25, 12, 1797, 0, DueDate.BegOfPeriod, -1797, -4)] // overflow
        public void PV(double Rate, double NPer, double Pmt, double FV, DueDate Due, double expected, int relativePrecision)
        {
            Assert.Equal(expected, Financial.PV(Rate, NPer, Pmt, FV, Due), s_precision + relativePrecision);
        }

        [Fact]
        public void PV_Default()
        {
            Assert.Equal(-2952.944852320145, Financial.PV(0.008, 4, 12, 3000, 0), s_precision - 4);
            Assert.Equal(-2952.944852320145, Financial.PV(0.008, 4, 12, 3000), s_precision - 4);
        }

        [Theory]
        [InlineData(1.0, 1.0, 1.0, 0, DueDate.EndOfPeriod, 0, -2, 0)]
        [InlineData(24.0, -800.0, 10000.0, 0.0, DueDate.BegOfPeriod, 0.1, 0.06767027865651142, 0)]
        [InlineData(12, -263.0, 3000, 0, DueDate.EndOfPeriod, 0.1, 0.007886438377633958, 0)]
        [InlineData(48, -570, 24270.0, 0, DueDate.BegOfPeriod, 0.1, 0.005224016433999085, 0)]
        [InlineData(96, -1000.0, 56818, 0, DueDate.EndOfPeriod, 0.1, 0.012000207330254708, 0)]
        [InlineData(12, -3000.0, 300, 0, DueDate.EndOfPeriod, 0.1, -1.9850238772287565, -1)] // pmt > pv
        public void Rate(double NPer, double Pmt, double PV, double FV, DueDate Due, double Guess, double expected, int relativePrecision)
        {
            Assert.Equal(expected, Financial.Rate(NPer, Pmt, PV, FV, Due, Guess), s_precision + relativePrecision);
        }

        [Fact]
        public void Rate_Default()
        {
            Assert.Equal(0.007886438377633958, Financial.Rate(12, -263.0, 3000, 0), s_precision - 0);
            Assert.Equal(0.007886438377633958, Financial.Rate(12, -263.0, 3000), s_precision - 0);
        }

        [Fact]
        public void Rate_Invalid()
        {
            Assert.Throws<ArgumentException>(() => Financial.Rate(-12, -263.0, 3000, 0, 0, 0.1));
        }

        [Theory]
        [InlineData(1.0, 1.0, 1.0, 0, 0)]
        [InlineData(1000.0, 800.0, 50.0, 4.0, -1)]
        [InlineData(5000, 1000, 20, 200, -3)]
        [InlineData(54870, 21008, 7, 4837.428571428572, -4)]
        [InlineData(2, 1.1, 12, 0.075, 0)]
        [InlineData(1000, 5000, 20, -200, -3)] // salvage > cost
        [InlineData(5000, 1000, -20, -200, -3)] // life < 0
        [InlineData(5000, 0, 12, 416.6666666666667, -3)] // salvage = 0
        [InlineData(-5000, -1000, -20, 200, -3)] // all parameter -ve
        public void SLN(double Cost, double Salvage, double Life, double expected, int relativePrecision)
        {
            Assert.Equal(expected, Financial.SLN(Cost, Salvage, Life), s_precision + relativePrecision);
        }

        [Theory]
        [InlineData(1.0, 1.0, 1.0, 1.0, 0, 0)]
        [InlineData(1000.0, 800.0, 50, 2.0, 7.686274509803922, -1)]
        [InlineData(4322.0, 1009.0, 73, 23, 62.55572010366531, -2)]
        [InlineData(78000.0, 21008, 8, 2, 11081.777777777777, -5)]
        [InlineData(23.0, 7.0, 21, 9, 0.9004329004329005, 0)]
        [InlineData(1009.0, 4322.0, 73, 23, -62.55572010366531, -2)] // salvage > cost
        public void SYD(double Cost, double Salvage, double Life, double Period, double expected, int relativePrecision)
        {
            Assert.Equal(expected, Financial.SYD(Cost, Salvage, Life, Period), s_precision + relativePrecision);
        }

        [ConditionalTheory(nameof(IsNotArmNorArm64NorAlpine))]
        [InlineData(9.9999999999999e+305, 0, 100, 10, 1.801980198019784e+304, -10)] // overflow
        public void SYD_Overflow(double Cost, double Salvage, double Life, double Period, double expected, int relativePrecision)
        {
            Assert.Equal(expected, Financial.SYD(Cost, Salvage, Life, Period), s_precision + relativePrecision);
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
