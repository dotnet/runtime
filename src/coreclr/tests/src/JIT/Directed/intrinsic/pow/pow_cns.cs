using System;
using System.Runtime.CompilerServices;

class MathPowTests
{
    static int returnCode = 100;

    static int Main(string[] args)
    {
        var tests = new MathPowTests();
        tests.TestCorrectnessDouble();
        tests.TestCorrectnessFloat();
        tests.TestFieldArg();
        tests.TestCallArg1();
        tests.TestCallArgN1();
        tests.TestCallArg2();

        float x = 0, y = 0;
        tests.TestRefArgs(ref x, ref y);
        return returnCode;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static T ToVar<T>(T arg) => arg;

    public static void AssertEquals(double a, double b)
    {
        if (BitConverter.DoubleToInt64Bits(a) !=
            BitConverter.DoubleToInt64Bits(b))
        {
            returnCode++;
            Console.WriteLine($"Failed: {a} != {b}");
        }
    }

    public static void AssertEquals(float a, float b)
    {
        if (BitConverter.SingleToInt32Bits(a) !=
            BitConverter.SingleToInt32Bits(b))
        {
            returnCode++;
            Console.WriteLine($"Failed: {a} != {b}");
        }
    }

    public void TestCorrectnessDouble()
    {
        double[] testDblValues =
            {
                -1000, -2, -1, -0.0, 0, 1, 2, 1000,
                Math.PI, Math.E,
                double.MinValue, double.MaxValue,
                double.PositiveInfinity, double.NegativeInfinity
            };

        foreach (double testValue in testDblValues)
        {
            AssertEquals(Math.Pow(testValue,  0.0), Math.Pow(testValue, ToVar( 0.0)));
            AssertEquals(Math.Pow(testValue, -0.0), Math.Pow(testValue, ToVar(-0.0)));
            AssertEquals(Math.Pow(testValue,  1.0), Math.Pow(testValue, ToVar( 1.0)));
            AssertEquals(Math.Pow(testValue, -1.0), Math.Pow(testValue, ToVar(-1.0)));
            AssertEquals(Math.Pow(testValue,  2.0), Math.Pow(testValue, ToVar( 2.0)));
            AssertEquals(Math.Pow(testValue,  3.0), Math.Pow(testValue, ToVar( 3.0)));
            AssertEquals(Math.Pow(testValue,  4.0), Math.Pow(testValue, ToVar( 4.0)));
        }
    }

    public void TestCorrectnessFloat()
    {
        float[] testFltValues =
            {
                -1000, -2, -1, -0.0f, 0, 1, 2, 1000,
                MathF.PI, MathF.E,
                float.MinValue, float.MaxValue,
                float.PositiveInfinity, float.NegativeInfinity
            };

        foreach (float testValue in testFltValues)
        {
            AssertEquals(MathF.Pow(testValue,  0.0f), MathF.Pow(testValue, ToVar( 0.0f)));
            AssertEquals(MathF.Pow(testValue, -0.0f), MathF.Pow(testValue, ToVar(-0.0f)));
            AssertEquals(MathF.Pow(testValue,  1.0f), MathF.Pow(testValue, ToVar( 1.0f)));
            AssertEquals(MathF.Pow(testValue, -1.0f), MathF.Pow(testValue, ToVar(-1.0f)));
            AssertEquals(MathF.Pow(testValue,  2.0f), MathF.Pow(testValue, ToVar( 2.0f)));
            AssertEquals(MathF.Pow(testValue,  3.0f), MathF.Pow(testValue, ToVar( 3.0f)));
            AssertEquals(MathF.Pow(testValue,  4.0f), MathF.Pow(testValue, ToVar( 4.0f)));
        }
    }


    private float testField1 = MathF.PI;
    private float testField2 = MathF.PI;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void TestFieldArg()
    {
        AssertEquals(MathF.Pow(testField1, 2.0f), MathF.Pow(testField2, ToVar(2.0f)));
    }


    private int sideeffects = 0;
    [MethodImpl(MethodImplOptions.NoInlining)]
    private double TestCall1()
    {
        sideeffects++;
        return Math.PI;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private double TestCall2() => Math.PI;


    [MethodImpl(MethodImplOptions.NoInlining)]
    public void TestCallArg1()
    {
        sideeffects = 0;
        AssertEquals(Math.Pow(TestCall1(), 1.0), Math.Pow(TestCall2(), ToVar(1.0)));
        if (sideeffects != 1)
            returnCode++;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void TestCallArgN1()
    {
        sideeffects = 0;
        AssertEquals(Math.Pow(TestCall1(), -1.0), Math.Pow(TestCall2(), ToVar(-1.0)));
        if (sideeffects != 1)
            returnCode++;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void TestCallArg2()
    {
        sideeffects = 0;
        AssertEquals(Math.Pow(TestCall1(), 2.0), Math.Pow(TestCall2(), ToVar(2.0)));
        // make sure it's not optimized into
        // `TestCall1() * TestCall1()`:
        if (sideeffects != 1)
            returnCode++;
    }


    [MethodImpl(MethodImplOptions.NoInlining)]
    public void TestRefArgs(ref float x, ref float  y)
    {
        AssertEquals(Math.Pow(x, 2.0), Math.Pow(y, ToVar(2.0)));
    }
}