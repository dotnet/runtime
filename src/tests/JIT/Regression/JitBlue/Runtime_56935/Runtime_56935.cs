// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

namespace Runtime_56935
{
    public class Program
    {
        static int clsFld;

        public static int Main()
        {
            int zeroVal = 0;

            // The rhs of the following statement on Arm64 will be transformed into the following tree
            //
            // N012 ( 27, 14) [000009] ---X--------  *  ADD       int    $42
            // N010 ( 25, 11) [000029] ---X--------  +--*  ADD       int    $40
            // N008 ( 23,  8) [000032] ---X--------  |  +--*  NEG       int    $41
            // N007 ( 22,  7) [000008] ---X--------  |  |  \--*  DIV       int    $42
            // N003 (  1,  2) [000004] ------------  |  |     +--*  CNS_INT   int    1 $42
            // N006 (  1,  2) [000024] ------------  |  |     \--*  COMMA     int    $42
            // N004 (  0,  0) [000022] ------------  |  |        +--*  NOP       void   $100
            // N005 (  1,  2) [000023] ------------  |  |        \--*  CNS_INT   int    1 $42
            // N009 (  1,  2) [000028] ------------  |  \--*  CNS_INT   int    1 $42
            // N011 (  1,  2) [000003] ------------  \--*  CNS_INT   int    1 $42
            //
            // Then, during optValnumCSE() the tree is transformed even further by fgMorphCommutative()
            //
            // N014 ( 25, 11) [000029] ---X--------  *  ADD       int    $40
            // N012 ( 23,  8) [000032] ---X--------  +--*  NEG       int    $41
            // N011 ( 22,  7) [000008] ---X--------  |  \--*  DIV       int    $42
            // N007 (  1,  2) [000004] ------------  |     +--*  CNS_INT   int    1 $42
            // N010 (  1,  2) [000024] ------------  |     \--*  COMMA     int    $42
            // N008 (  0,  0) [000022] ------------  |        +--*  NOP       void   $100
            // N009 (  1,  2) [000023] ------------  |        \--*  CNS_INT   int    1 $42
            // N013 (  1,  2) [000028] ------------  \--*  CNS_INT   int    2 $42
            //
            // The issue is that VN for [000028] has not been updated ($42 corresponds to CnsInt(1)).
            // As a result, during optVNAssertionPropCurStmt() the whole tree is **incorrecly** folded to
            //
            // After constant propagation on [000029]:
            // N007 (  1,  2) [000040] ------------  *  CNS_INT   int    0 $40

            clsFld = 1 + (1 % (zeroVal + 1));
            return (clsFld == 1) ? 100 : 0;
        }
    }
}
