// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// arrays.il
.assembly extern mscorlib { }
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ASSEMBLY_NAME { }
.class public auto ansi Test_arrays extends [mscorlib] System.Object {
.method public static int32 main() cil managed
{
  .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
      01 00 00 00
  )
  .entrypoint
  .maxstack  8
  .locals (float64[] V_0,
           int32[] V_1,
           float64[] V_2,
           int32 V_3,
           int32[] V_4,
           int32 V_5,
           int32 V_6,
           class [mscorlib]System.InvalidCastException V_7,
           float64[] V_8,
           class [mscorlib]System.InvalidCastException V_9,
           int32[] V_10,
           class [mscorlib]System.InvalidCastException V_11,
           float64[] V_12,
           class [mscorlib]System.InvalidCastException V_13,
           int32[] V_14)
  .try
  {
    IL_0000:  ldc.i4.4
    IL_0001:  newarr     [mscorlib]System.Int32
    IL_0015:  isinst     [mscorlib]System.Array
    IL_001a:  isinst     int32[]
    IL_001f:  call       void [System.Console]System.Console::WriteLine(object)
    IL_0024:  leave.s    IL_003f
  }
  catch [mscorlib]System.InvalidCastException 
  {
    IL_0026:  stloc.s    V_13
    IL_0028:  ldstr "fail(1)"
    IL_0032:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0037:  ldc.i4.s   101
    IL_0039:  stloc.3
    IL_003a:  leave      IL_0118
  }
  .try
  {
    IL_003f:  ldc.i4.4
    IL_0040:  newarr     [mscorlib]System.Double
    IL_0050:  isinst     [mscorlib]System.Array
    IL_0055:  isinst     int32[]
    IL_005a:  brfalse.s  IL_0073
    IL_005c:  ldstr "fail(2)"
    IL_0066:  call       void [System.Console]System.Console::WriteLine(string)
    IL_006b:  ldc.i4.s   101
    IL_006d:  stloc.3
    IL_006e:  leave      IL_0118
    IL_0073:  leave.s    IL_008f
  }
  catch [mscorlib]System.InvalidCastException 
  {
    IL_0075:  stloc.s    V_11
    IL_0077:  ldstr "fail(3)"
    IL_0081:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0086:  ldc.i4.s   101
    IL_0088:  stloc.s    V_6
    IL_008a:  leave      IL_0115
  }
  .try
  {
    IL_008f:  ldc.i4.4
    IL_0090:  newarr     [mscorlib]System.Int32
    IL_00a0:  castclass  [mscorlib]System.Array
    IL_00a5:  castclass  int32[]
    IL_00aa:  call       void [System.Console]System.Console::WriteLine(object)
    IL_00af:  leave.s    IL_00c8
  }
  catch [mscorlib]System.InvalidCastException 
  {
    IL_00b1:  stloc.s    V_9
    IL_00b3:  ldstr "fail(4)"
    IL_00bd:  call       void [System.Console]System.Console::WriteLine(string)
    IL_00c2:  ldc.i4.s   101
    IL_00c4:  stloc.s    V_5
    IL_00c6:  leave.s    IL_0112
  }
  .try
  {
    IL_00c8:  ldc.i4.4
    IL_00c9:  newarr     [mscorlib]System.Double
    IL_00d9:  castclass  [mscorlib]System.Array
    IL_00de:  castclass  int32[]
    IL_00e3:  call       void [System.Console]System.Console::WriteLine(object)
    IL_00e8:  ldstr "fail(5)"
    IL_00f2:  call       void [System.Console]System.Console::WriteLine(string)
    IL_00f7:  ldc.i4.s   101
    IL_00f9:  stloc.3
    IL_00fa:  leave.s    IL_0118
  }
  catch [mscorlib]System.InvalidCastException 
  {
    IL_00fc:  stloc.s    V_7
    IL_00fe:  leave.s    IL_0100
  }
  IL_0100:  ldstr "success"
  IL_010a:  call       void [System.Console]System.Console::WriteLine(string)
  IL_010f:  ldc.i4.s   100
  IL_0111:  ret
  IL_0112:  ldloc.s    V_5
  IL_0114:  ret
  IL_0115:  ldloc.s    V_6
  IL_0117:  ret
  IL_0118:  ldloc.3
  IL_0119:  ret
}
}
