// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly 'b18049' {}
.assembly extern xunit.core {}
.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}

.data D_a = bytearray (30 31 32 33)
.data D_b = bytearray (30 31 32 33)

.field public static int32 a at D_a
.field public static int32 b at D_b

.class public auto ansi Test_b18049 extends [mscorlib] System.Object {
.method static public int32 main() cil managed
{ .entrypoint
  .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
      01 00 00 00
  )
  .maxstack  3
  .locals (int32* V_0)
 
 
  ldsflda    int32 a
  stloc.0
  ldloc.0
  ldsflda    int32 b
  bge.un.s   Label_Exit

Label_Loop:  
  ldloc.0
  ldc.i4.4
  add
  stloc.0
  ldloc.0
  ldsflda    int32 b
  blt.un.s   Label_Loop

Label_Exit:  
  ldstr "PASS"
  call void [System.Console]System.Console::WriteLine(class System.String)
  ldc.i4 100
  ret
}
}
