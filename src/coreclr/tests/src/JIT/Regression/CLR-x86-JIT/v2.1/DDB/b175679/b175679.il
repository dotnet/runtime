// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         
  .ver 2:0:0:0
}

.assembly nan
{
}

.class private abstract auto ansi sealed beforefieldinit NaN
       extends [mscorlib]System.Object
{
  .method private hidebysig static int32  Main() cil managed
  {
    .entrypoint
    .maxstack  10
    .locals init (float64 nnan, float64 pnan, float64 pzero, float64 nzero, int32 returnValue)
    ldc.i4.s   100
    stloc.s    returnValue
    ldc.r8     float64 ( 0xfff8000020000000 )
    stloc.0
    ldloc.0
    neg
    stloc.1
    ldc.r8     float64 ( 0x0 )
    stloc.2
    ldloc.2
    neg
    stloc.3
    ldloc.0
    call       void [System.Console]System.Console::WriteLine(float64)
    ldloc.1
    call       void [System.Console]System.Console::WriteLine(float64)
    ldloc.2
    call       void [System.Console]System.Console::WriteLine(float64)
    ldloc.3
    call       void [System.Console]System.Console::WriteLine(float64)
    ldloca.s   0
    ldind.u8
    call       void [System.Console]System.Console::WriteLine(uint64)
    ldc.i8     18444492274432737280
    ldloca.s   0
    ldind.u8
    ceq
    brtrue.s   IL1
    ldc.i4.s   101
    stloc.s    returnValue
    ldstr "FAILED: Previous value should have been 18444492274432737280"
    call       void [System.Console]System.Console::WriteLine(string)
IL1: 
    ldloca.s   1
    ldind.u8
    call       void [System.Console]System.Console::WriteLine(uint64)
    ldc.i8     9221120237577961472
    ldloca.s   1
    ldind.u8
    ceq
    brtrue.s   IL2
    ldc.i4.s   101
    stloc.s    returnValue
    ldstr "FAILED: Previous value should have been 9221120237577961472"
    call       void [System.Console]System.Console::WriteLine(string)
IL2: 
    ldloca.s   2
    ldind.u8
    call       void [System.Console]System.Console::WriteLine(uint64)
    ldc.i8     0
    ldloca.s   2
    ldind.u8
    ceq
    brtrue.s   IL3
    ldc.i4.s   101
    stloc.s    returnValue
    ldstr "FAILED: Previous value should have been 0"
    call       void [System.Console]System.Console::WriteLine(string)
IL3: 
    ldloca.s   3
    ldind.u8
    call       void [System.Console]System.Console::WriteLine(uint64)
    ldc.i8     9223372036854775808
    ldloca.s   3
    ldind.u8
    ceq
    brtrue.s   IL4
    ldc.i4.s   101
    stloc.s    returnValue
    ldstr "FAILED: Previous value should have been 9223372036854775808"
    call       void [System.Console]System.Console::WriteLine(string)
IL4: 

    ldc.i4.s 100
    ldloc.s    returnValue
    ceq
    brfalse.s IL5
    ldstr "!!!! Test Passed !!!"
    call       void [System.Console]System.Console::WriteLine(string)
IL5:
    ldloc.s    returnValue
    ret
  } 

} 
