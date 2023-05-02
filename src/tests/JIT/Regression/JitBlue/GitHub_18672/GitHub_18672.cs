// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Xunit;

struct S4
{
    int FI1;
}

struct S24
{
    public int FI2;
    public ulong FU3;
    public S4 FS4;
    public S24(int i, S4 s): this()
    {
        FI2 = i;
        FS4 = s;
    }
}

// A 24-byte struct that simply wraps S24
struct S24W
{
    public S24 FS24;
    public S24W(S24 s): this()
    {
        FS24 = s;
    }
}

public class Program
{
    [Fact]
    public static int TestEntryPoint()
    {
        S4    s4   = new S4();
        S24   s24  = new S24(1, s4);
        S24W  s24W = new S24W(s24);
        S24   s24X = s24;

        int   fI2  = s24.FI2;
        int   fI2W = s24W.FS24.FI2;
        int   fI2X = s24X.FI2;

        M();

        if ((fI2 == 1) && (fI2W == 1) && (fI2X == 1))
        {
            System.Console.WriteLine("Passed");
            return 100;
        }
        else
        {
            // Before the fix we would fail with:
            //
            System.Console.WriteLine(fI2);   // 1 in debug, 1 in release  OK
            System.Console.WriteLine(fI2W);  // 1 in debug, 0 in release  BAD
            System.Console.WriteLine(fI2X);  // 1 in debug, 1 in release  OK

            System.Console.WriteLine("Failed");
            return 101;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void M(){}
}
