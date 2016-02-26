// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*
a good test would be to create a big object with very sparse pointers embedded.
For example, you can create an array of value types and each value type has
mostly integers, say 100 integers and only 2 pointers. When we push stuff onto
the mark stack we first push all the 1st level sub pointers onto the stack at
once. So you want to create an object with that big array embedded in it. This
is a test program that I used to test the change but you'd have to modify it a
little bit to see a noticable difference. But it's good enough to illustrate
the point.
*/

using System;
using System.Runtime;
using System.Runtime.InteropServices;

public class A
{
    public int a;
    public A()
    {
        a = 1;
    }
}

public class B
{
    public int b;
    public B()
    {
        b = 2;
    }
}

[StructLayout(LayoutKind.Sequential)]
public struct C
{

    int i00;
    int i01;
    int i02;
    int i03;
    int i04;
    int i05;
    int i06;
    int i07;
    int i08;
    int i09;
    int i10;
    int i11;
    int i12;
    int i13;
    int i14;
    int i15;
    int i16;
    int i17;
    int i18;
    int i19;
    int i20;
    int i21;
    int i22;
    int i23;
    int i24;
    int i25;
    int i26;
    int i27;
    int i28;
    int i29;
    int i30;
    int i31;
    int i32;
    int i33;

    public A t1;

    int i34;
    int i35;
    int i36;
    int i37;
    int i38;
    int i39;
    int i40;
    int i41;
    int i42;
    int i43;
    int i44;
    int i45;
    int i46;
    int i47;
    int i48;
    int i49;
    int i50;
    int i51;
    int i52;
    int i53;
    int i54;
    int i55;
    int i56;
    int i57;
    int i58;
    int i59;
    int i60;
    int i61;
    int i62;
    int i63;
    int i64;
    int i65;
    int i66;

    public B t2;

    int i67;
    int i68;
    int i69;
    int i70;
    int i71;
    int i72;
    int i73;
    int i74;
    int i75;
    int i76;
    int i77;
    int i78;
    int i79;
    int i80;
    int i81;
    int i82;
    int i83;
    int i84;
    int i85;
    int i86;
    int i87;
    int i88;
    int i89;
    int i90;
    int i91;
    int i92;
    int i93;
    int i94;
    int i95;
    int i96;
    int i97;
    int i98;
    int i99;
    int j00;
    int j01;
    int j02;
    int j03;
    int j04;
    int j05;
    int j06;
    int j07;
    int j08;
    int j09;
    int j10;
    int j11;
    int j12;
    int j13;
    int j14;
    int j15;
    int j16;
    int j17;
    int j18;
    int j19;
    int j20;
    int j21;
    int j22;
    int j23;
    int j24;
    int j25;
    int j26;
    int j27;
    int j28;
    int j29;
    int j30;
    int j31;
    int j32;
    int j33;

    public A t3;

    int j34;
    int j35;
    int j36;
    int j37;
    int j38;
    int j39;
    int j40;
    int j41;
    int j42;
    int j43;
    int j44;
    int j45;
    int j46;
    int j47;
    int j48;
    int j49;
    int j50;
    int j51;
    int j52;
    int j53;
    int j54;
    int j55;
    int j56;
    int j57;
    int j58;
    int j59;
    int j60;
    int j61;
    int j62;
    int j63;
    int j64;
    int j65;
    int j66;

    public B t4;

    int j67;
    int j68;
    int j69;
    int j70;
    int j71;
    int j72;
    int j73;
    int j74;
    int j75;
    int j76;
    int j77;
    int j78;
    int j79;
    int j80;
    int j81;
    int j82;
    int j83;
    int j84;
    int j85;
    int j86;
    int j87;
    int j88;
    int j89;
    int j90;
    int j91;
    int j92;
    int j93;
    int j94;
    int j95;
    int j96;
    int j97;
    int j98;
    int j99;
}




class CC
{
    public C[] array;
    public CC(int size)
    {
        array = new C[size];
    }
}


class TestMark
{

    public static int Main(string[] arg)
    {

        Console.WriteLine("Before allocation: {0}", TestMark.GetCommitted());

        CC cc = new CC(500000);
        cc.array[1024].t1 = new A();
        cc.array[1024].t1.a = 3;
        cc.array[1024].t2 = new B();
        cc.array[1024].t2.b = 4;

        long a = TestMark.GetCommitted();
        Console.WriteLine("After allocation: {0}", a);
        Console.WriteLine();
        Console.WriteLine("Collecting...");
        for (int i=0; i<100; i++)
        {
            GC.Collect();
        }

        long b= TestMark.GetCommitted();
        Console.WriteLine("After 100 Collections: {0}", b);
        GC.KeepAlive(cc);

        if (Math.Abs(b- a) > (a/2))
        {
            Console.WriteLine("failed");
            return 0;
        }

        Console.WriteLine("passed");
        return 100;


    }

    [DllImport( "Kernel32.dll", CharSet=CharSet.Ansi )]
    public static extern bool GlobalMemoryStatusEx( MemoryStatusEx memStatus);

    public static long GetCommitted()
    {
        MemoryStatusEx mex = new MemoryStatusEx();
        mex.length = Marshal.SizeOf(mex);
        GlobalMemoryStatusEx(mex);
        return mex.totalPageFile - mex.availPageFile;
    }

}

[ StructLayout( LayoutKind.Sequential, CharSet=CharSet.Ansi )]
public class MemoryStatusEx
{
    public int length = 0;
    public int memoryLoad = 0;
    public long totalPhys = 0;
    public long availPhys = 0;
    public long totalPageFile = 0;
    public long availPageFile = 0;
    public long totalVirtual = 0;
    public long availVirtual = 0;
    public long availExtendedVirtual = 0;
}
