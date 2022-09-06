// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This test tries to produce a TYP_LONG GT_STORE_LCL_VAR tree that contains an embedded statement:
//
// *  stmtExpr  void  (top level) (IL 0x000...0x00B)
// |           /--*  lclVar    int    V01 arg1
// |           +--*  const     int    4
// |        /--*  +         int
// |        |  {  *  stmtExpr  void  (embedded)
// |        |  {  |        /--*  lclVar    ref    V00 this
// |        |  {  |        +--*  const     int    4 field offset Fseq[i]
// |        |  {  |     /--*  +         byref
// |        |  {  |  /--*  indir     int
// |        |  {  \--*  st.lclVar int    V03 cse0
// |        +--*  lclVar    int    V03 cse0
// |     /--*  -         int
// |  /--*  cast      long <- ulong <- uint
// \--*  st.lclVar long   V02 loc0
//
// This requires decomposition of GT_STORE_LCL_VAR to properly detect the insertion point
// for a statement it creates.

using System;
using System.Runtime.CompilerServices;

class Program
{
    uint i;

    [MethodImpl(MethodImplOptions.NoInlining)]
    ulong Test(uint h)
    {
        uint x = h + 4;
        ulong f = checked((ulong)unchecked(x - i));
        if (i > h)
            return 0;
        return f;
    }

    static int Main()
    {
        const int Pass = 100;
        const int Fail = -1;

        if (new Program().Test(42) == 46)
        {
            Console.WriteLine("Passed");
            return Pass;
        }
        else
        {
            Console.WriteLine("Failed");
            return Fail;
        }
    }
}
