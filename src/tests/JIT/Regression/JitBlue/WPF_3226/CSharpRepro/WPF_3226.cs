// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

[StructLayout(LayoutKind.Sequential)]
class POINT 
{
    public int x;
    public int y;
    public override string ToString() => $"{{{x}, {y}}}";
}

[StructLayout(LayoutKind.Sequential)]
class MINMAXINFO 
{
    public POINT ptMinTrackSize = new POINT();
    public POINT ptMaxTrackSize = new POINT();
}

public class Test_WPF_3226
{
    static void WmGetMinMaxInfo(IntPtr lParam)
    {
        MINMAXINFO mmi = new MINMAXINFO();

        mmi.ptMinTrackSize.x = 100101;
        mmi.ptMinTrackSize.y = 102103;
        mmi.ptMaxTrackSize.x = 200201;
        mmi.ptMaxTrackSize.y = 202203;

        Marshal.StructureToPtr(mmi, lParam, true);
    }

    [Fact]
    public unsafe static int TestEntryPoint()
    {
        MINMAXINFO mmi = new MINMAXINFO();
        IntPtr pmmi = Marshal.AllocHGlobal(Marshal.SizeOf(mmi));
        WmGetMinMaxInfo(pmmi);
        mmi = (MINMAXINFO) Marshal.PtrToStructure(pmmi, typeof(MINMAXINFO));
        bool valid =  mmi.ptMinTrackSize.x == 100101 &&  mmi.ptMinTrackSize.y == 102103 &&  mmi.ptMaxTrackSize.x == 200201 && mmi.ptMaxTrackSize.y == 202203;
        if (!valid)
        {
            Console.WriteLine($"Got {mmi.ptMinTrackSize}, expected {{100101, 102103}}");
            Console.WriteLine($"Got {mmi.ptMaxTrackSize}, expected {{200201, 202203}}");
        }
        return valid ? 100 : -1;
    }
}
