// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { auto }
.assembly extern xunit.core {}
.assembly extern System.Console {auto}
.assembly 'GitHub_17969' { }

.class private sequential ansi sealed MyStruct
       extends [mscorlib]System.ValueType
{
  .pack 0
  .size 84
}

.class private sequential ansi sealed MyArray1
       extends [mscorlib]System.ValueType
{
  .pack 0
  .size 160
}

.class private sequential ansi sealed MyArray2
       extends [mscorlib]System.ValueType
{
  .pack 0
  .size 1680
}

.class public auto ansi beforefieldinit Test
       extends [mscorlib]System.Object
{
    .method public static unmanagedexp int32
            main() cil managed
    {
        .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
            01 00 00 00
        )
        .entrypoint
      .vtentry 11 : 1
      // Code size       131 (0x83)
      .maxstack  4
      .locals ([0] int32 i,
               [1] int32 j,
               [2] valuetype MyStruct s2,
               [3] valuetype MyArray2 s4,
               [4] int32     returnVal)

        // returnVal = 100;
        ldc.i4.s   100
        stloc      returnVal

        // i = 10;
        ldc.i4.s   10
        stloc.0

        // j = 6;
        ldc.i4.6
        stloc.1

        // s4[j].a2 = 16;
        ldloca.s   s4
        ldloc      j
        conv.i
        ldc.i4.s   84
        conv.i
        mul
        add
        ldc.i4.s   80
        conv.i
        add
        ldc.i4.s   16
        stind.i4

        // s4[i] = s4[j];
        ldloca.s   s4
        ldloc      i
        conv.i
        ldc.i4.s   84
        conv.i
        mul
        add
        ldloca.s   s4
        ldloc      j
        conv.i
        ldc.i4.s   84
        conv.i
        mul
        add
        ldc.i4.s   84
        unaligned. 4
        cpblk

        // s2 = s4[i];
        ldloca.s   s2
        ldloca.s   s4
        ldloc      i
        conv.i
        ldc.i4.s   84
        conv.i
        mul
        add
        ldc.i4.s   84
        unaligned. 4
        cpblk

        // if (s4[i].a2 != 16) goto F1
        ldloca.s   s4
        ldloc      i
        conv.i
        ldc.i4.s   84
        conv.i
        mul
        add
        ldc.i4.s   80
        conv.i
        add
        ldind.i4
        ldc.i4.s   16
        beq.s      L2
    
    F1:
        // Console.WriteLine("FAIL");
        ldstr "FAIL"
        call void [System.Console]System.Console::WriteLine(string)
        // returnVal = -1;
        ldc.i4.m1
        stloc      returnVal
    
    L2:
        // if (s2.a2 != 16) goto F2
        ldloca.s   s2
        ldc.i4.s   80
        add
        ldind.i4
        ldc.i4.s   16
        beq.s      L3
    
    F2:
        // Console.WriteLine("FAIL");
        ldstr "FAIL"
        call void [System.Console]System.Console::WriteLine(string)
        // returnVal = -1;
        ldc.i4.m1
        stloc      returnVal
        br         L4

    L3:
        // Console.WriteLine("PASS");
        ldstr "PASS"
        call void [System.Console]System.Console::WriteLine(string)

    L4:
        ldloc     returnVal
        ret
    }
}
