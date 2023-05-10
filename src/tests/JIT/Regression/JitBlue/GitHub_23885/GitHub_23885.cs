// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Numerics;
using Xunit;

public class GitHub_23885
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    static void dummy(Vector<ulong> v1, Vector<ulong> v2, Vector<ulong> v3, Vector<ulong> v4,
                      Vector<ulong> v5, Vector<ulong> v6, Vector<ulong> v7, Vector<ulong> v8)
    {
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void CreateVectors()
    {
        Vector<ulong> vA = new Vector<ulong>((ulong)0xa);
        Vector<ulong> vB = new Vector<ulong>((ulong)0xb);
        Vector<ulong> vC = new Vector<ulong>((ulong)0xc);
        Vector<ulong> vD = new Vector<ulong>((ulong)0xd);
        Vector<ulong> vE = new Vector<ulong>((ulong)0xe);
        Vector<ulong> vF = new Vector<ulong>((ulong)0xf);
        Vector<ulong> vG = new Vector<ulong>((ulong)0x8);
        Vector<ulong> vH = new Vector<ulong>((ulong)0x9);
        dummy(vA, vB, vC, vD, vE, vF, vG, vH);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static double GetDouble()
    {
        return 1.0;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void ConsumeDouble(double d)
    {
    }

    [Fact]
    public static int TestEntryPoint()
    {
        int returnVal = 100;

        // Define a non-SIMD floating point value so that we've got an odd number of
        // callee-save floating-point/vector registers remaining.
        double d = GetDouble();

        // Declare and initialize a bunch of vectors.
        Vector<ulong> xA = Vector<ulong>.One;
        Vector<ulong> xB = Vector<ulong>.One;
        Vector<ulong> xC = Vector<ulong>.One;
        Vector<ulong> xD = Vector<ulong>.One;
        Vector<ulong> xE = Vector<ulong>.One;
        Vector<ulong> xF = Vector<ulong>.One;

        // Use d a few times to give it more weight than the SIMD values.
        ConsumeDouble((d * d) + d);
        d = d * GetDouble();

        // Use the vectors in computation to give them some weight.
        xA = xB + xC + xD + xE + xF;
        xB = xA + xC + xD + xE + xF;
        xC = xA + xB + xD + xE + xF;
        xD = xA + xC + xC + xE + xF;
        xE = xA + xC + xD + xD + xF;
        xF = xA + xC + xD + xE + xE;

        ConsumeDouble((d * d) + d);
        d = d * GetDouble();

        CreateVectors();

        Console.WriteLine("{0} {1} {2} {3} {4} {5}", xA, xB, xC, xD, xE, xF);
        if (!xA.Equals(new Vector<ulong>((ulong)5)) || !xB.Equals(new Vector<ulong>((ulong)9)) || !xC.Equals(new Vector<ulong>((ulong)17)) ||
            !xD.Equals(new Vector<ulong>((ulong)41)) || !xE.Equals(new Vector<ulong>((ulong)105)) || !xF.Equals(new Vector<ulong>((ulong)273)))
        {
            returnVal = -1;
        }

        // Now, create more vectors, so that even more will be spilled aross the next call.
        Vector<ulong> xG = Vector<ulong>.Zero;
        Vector<ulong> xH = Vector<ulong>.Zero;
        Vector<ulong> xI = Vector<ulong>.Zero;

        CreateVectors();

        Console.WriteLine("{0} {1} {2} {3} {4} {5} {6} {7} {8}", xA, xB, xC, xD, xE, xF, xG, xH, xI);

        ConsumeDouble((d * d) + d);
        d = d * GetDouble();

        if (!xA.Equals(new Vector<ulong>((ulong)5)) || !xB.Equals(new Vector<ulong>((ulong)9)) || !xC.Equals(new Vector<ulong>((ulong)17)) ||
            !xD.Equals(new Vector<ulong>((ulong)41)) || !xE.Equals(new Vector<ulong>((ulong)105)) || !xF.Equals(new Vector<ulong>((ulong)273)) ||
            !xG.Equals(Vector<ulong>.Zero) || !xH.Equals(Vector<ulong>.Zero) || !xI.Equals(Vector<ulong>.Zero))
        {
            returnVal = -1;
        }

        return returnVal;
    }
}

