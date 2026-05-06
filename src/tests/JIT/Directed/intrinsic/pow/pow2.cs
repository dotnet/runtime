// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

//testing plain double, array member, struct member or class member

using System;
using Xunit;

internal struct vt
{
    public double[,] x;
}

internal class cl
{
    public double[,,] x;
    public cl()
    {
        x = new double[6, 5, 4];
    }
}
public class pow2
{
    private static double CalculateSinh(double x)
    {
        double a = Math.Pow(Math.E, x);
        double b = Math.Pow(Math.E, -x);
        return (a - b) / 2;
    }

    private static double CalculateSinh(double[] x)
    {
        double a = Math.Pow(Math.E, x[1]);
        double b = Math.Pow(Math.E, -x[1]);
        return (a - b) / 2;
    }

    private static double CalculateSinh(double[,] x)
    {
        double a = Math.Pow(Math.E, x[1, 1]);
        double b = Math.Pow(Math.E, -x[1, 1]);
        return (a - b) / 2;
    }

    private static double CalculateSinh(double[,,] x)
    {
        double a = Math.Pow(Math.E, x[2, 1, 1]);
        double b = Math.Pow(Math.E, -x[2, 1, 1]);
        return (a - b) / 2;
    }

    private static double CalculateSinh(double[][,] x)
    {
        double a = Math.Pow(Math.E, x[2][1, 1]);
        double b = Math.Pow(Math.E, -x[2][1, 1]);
        return (a - b) / 2;
    }

    private static double CalculateSinh(vt vt1)
    {
        double a = Math.Pow(Math.E, vt1.x[1, 1]);
        double b = Math.Pow(Math.E, -vt1.x[1, 1]);
        return (a - b) / 2;
    }

    private static double CalculateSinh(cl cl1)
    {
        double a = Math.Pow(Math.E, cl1.x[5, 1, 3]);
        double b = Math.Pow(Math.E, -cl1.x[5, 1, 3]);
        return (a - b) / 2;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        bool pass = true;

        double x, y, z;

        //Instead of using 10 * Double.Epsilon as the maximum allowed Delta
        //we can use a small value based on the precision of double (15-16 digits)
        //to accommodate slight differences in the pow intrinsic on ARM
        double maxDelta = 9.9E-16;

        //straight
        x = 1.2;
        y = Math.Sinh(x);
        z = CalculateSinh(x);
        if ((y - z) > maxDelta)
        {
            Console.WriteLine("x: {0}, Sinh(x): {1}, (Pow(E,x)-Pow(E,-x))/2: {2}", x, y, z);
            pass = false;
        }

        //array 1d
        double[] arr1d = new double[3];
        for (int i = 0; i < 3; i++)
            arr1d[i] = i + 0.2;
        z = CalculateSinh(arr1d);
        if ((y - z) > maxDelta)
        {
            Console.WriteLine("x: {0}, Sinh(x): {1}, (Pow(E,x)-Pow(E,-x))/2: {2}", x, y, z);
            pass = false;
        }

        //array 2d
        double[,] arr2d = new double[3, 2];
        for (int i = 0; i < 3; i++)
            arr2d[i, 1] = i + 0.2;
        z = CalculateSinh(arr2d);
        if ((y - z) > maxDelta)
        {
            Console.WriteLine("x: {0}, Sinh(x): {1}, (Pow(E,x)-Pow(E,-x))/2: {2}", x, y, z);
            pass = false;
        }

        //array 3d
        double[,,] arr3d = new double[3, 4, 2];
        for (int i = 0; i < 3; i++)
            arr3d[2, i, 1] = i + 0.2;
        z = CalculateSinh(arr3d);
        if ((y - z) > maxDelta)
        {
            Console.WriteLine("x: {0}, Sinh(x): {1}, (Pow(E,x)-Pow(E,-x))/2: {2}", x, y, z);
            pass = false;
        }

        //jagged array
        double[][,] jaggedarr = new double[3][,];
        jaggedarr[2] = new double[4, 3];
        for (int i = 0; i < 3; i++)
            jaggedarr[2][i, 1] = i + 0.2;
        z = CalculateSinh(jaggedarr);
        if ((y - z) > maxDelta)
        {
            Console.WriteLine("x: {0}, Sinh(x): {1}, (Pow(E,x)-Pow(E,-x))/2: {2}", x, y, z);
            pass = false;
        }

        //struct
        vt vt1;
        vt1.x = new double[4, 5];
        for (int i = 0; i < 3; i++)
            vt1.x[i, 1] = i + 0.2;
        z = CalculateSinh(vt1);
        if ((y - z) > maxDelta)
        {
            Console.WriteLine("x: {0}, Sinh(x): {1}, (Pow(E,x)-Pow(E,-x))/2: {2}", x, y, z);
            pass = false;
        }

        //class
        cl cl1 = new cl();
        for (int i = 0; i < 3; i++)
            cl1.x[5, i, 3] = i + 0.2;
        z = CalculateSinh(cl1);
        if ((y - z) > maxDelta)
        {
            Console.WriteLine("x: {0}, Sinh(x): {1}, (Pow(E,x)-Pow(E,-x))/2: {2}", x, y, z);
            pass = false;
        }

        if (pass)
        {
            Console.WriteLine("PASSED");
            return 100;
        }
        else
        {
            Console.WriteLine("FAILED");
            return 1;
        }
    }
}
