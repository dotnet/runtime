// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
namespace System.MathBenchmarks
{
    public partial class MathTests
    {
        [Fact]
        public static int TestEntryPoint()
        {
            var result = 100;

            var doubleTests = new Double();
            result += Test(doubleTests.Abs);
            result += Test(doubleTests.Acos);
            result += Test(doubleTests.Acosh);
            result += Test(doubleTests.Asin);
            result += Test(doubleTests.Asinh);
            result += Test(doubleTests.Atan);
            result += Test(doubleTests.Atanh);
            result += Test(doubleTests.Atan2);
            result += Test(doubleTests.Cbrt);
            result += Test(doubleTests.Ceiling);
            result += Test(doubleTests.CopySign);
            result += Test(doubleTests.Cos);
            result += Test(doubleTests.Cosh);
            result += Test(doubleTests.Exp);
            result += Test(doubleTests.Floor);
            result += Test(doubleTests.FusedMultiplyAdd);
            result += Test(doubleTests.ILogB);
            result += Test(doubleTests.Log);
            result += Test(doubleTests.Log2);
            result += Test(doubleTests.Log10);
            result += Test(doubleTests.Max);
            result += Test(doubleTests.Min);
            result += Test(doubleTests.Pow);
            result += Test(doubleTests.Round);
            result += Test(doubleTests.ScaleB);
            result += Test(doubleTests.Sin);
            result += Test(doubleTests.Sinh);
            result += Test(doubleTests.Sqrt);
            result += Test(doubleTests.Tan);
            result += Test(doubleTests.Tanh);

            var singleTests = new Single();
            result += Test(singleTests.Abs);
            result += Test(singleTests.Acos);
            result += Test(singleTests.Acosh);
            result += Test(singleTests.Asin);
            result += Test(singleTests.Asinh);
            result += Test(singleTests.Atan);
            result += Test(singleTests.Atanh);
            result += Test(singleTests.Atan2);
            result += Test(singleTests.Cbrt);
            result += Test(singleTests.Ceiling);
            result += Test(singleTests.CopySign);
            result += Test(singleTests.Cos);
            result += Test(singleTests.Cosh);
            result += Test(singleTests.Exp);
            result += Test(singleTests.Floor);
            result += Test(singleTests.FusedMultiplyAdd);
            result += Test(singleTests.ILogB);
            result += Test(singleTests.Log);
            result += Test(singleTests.Log2);
            result += Test(singleTests.Log10);
            result += Test(singleTests.Max);
            result += Test(singleTests.Min);
            result += Test(singleTests.Pow);
            result += Test(singleTests.Round);
            result += Test(singleTests.ScaleB);
            result += Test(singleTests.Sin);
            result += Test(singleTests.Sinh);
            result += Test(singleTests.Sqrt);
            result += Test(singleTests.Tan);
            result += Test(singleTests.Tanh);

            result += Test(DivideByConst.Test);

            return result;
        }

        private static int Test(Action action)
        {
            try
            {
                action();
            }
            catch
            {
                return -1;
            }

            return 0;
        }
    }
}
