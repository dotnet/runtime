// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { auto }
.assembly extern System.Console {auto}
.assembly UnusedValueBug { }

// This bug was found on desktop CLR using ILGEN.
// The expression ending in the 'mul' below is all dead, but not all the dead code
// gets eliminated. On ARM, there is a SETCC resulting from the first 'cgt' that remains,
// and it is marked as an 'UnusedValue', but the BuildSimple method wasn't creating a def
// for an unused value. This led to an assert in the code generator.
// 
.class private auto ansi beforefieldinit GitHub_18295
       extends [mscorlib]System.Object
{
    .method private hidebysig static int32 Test(int64 l, int32 i) cil managed
    {
      .locals init ([0]  int32   loc6,
                    [1]  float32 loc8)

        ldloc 0
        ldloc 1
        conv.ovf.i8.un
        ldarg 0
        not
        cgt
        cgt.un
        ldloc 0
        nop
        not
        not
        mul
        ret
    }

    .method private hidebysig static int32 Main() cil managed
    {
        .entrypoint
      .vtentry 11 : 1
      // Code size       131 (0x83)
      .maxstack  4
      .locals ([0] int32     returnVal)

        // returnVal = 100;
        ldc.i4.s   100
        stloc      returnVal

        // if (Test(1,1) != 1) goto F1
        ldc.i8     1
        ldc.i4     1
        call       int32 GitHub_18295::Test(int64, int32)

        ldc.i4.0
        beq.s      L2
    
    F1:
        // Console.WriteLine("FAIL");
        ldstr "FAIL"
        call void [System.Console]System.Console::WriteLine(string)
        // returnVal = -1;
        ldc.i4.m1
        stloc      returnVal
        br         L3
    
    L2:
        // Console.WriteLine("PASS");
        ldstr "PASS"
        call void [System.Console]System.Console::WriteLine(string)

    L3:
        ldloc     returnVal
        ret
    }
}
