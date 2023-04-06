// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern xunit.core {}
.assembly extern mscorlib { }
.assembly 'b32879' { }
.class public auto ansi HiDad extends [mscorlib]System.Object
{
  .method public static void main() il managed forwardref
  {
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldc.i4.1
    IL_0002:  call       class [mscorlib]System.String HiDad::X(int32)
    IL_0007:  call       void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
    IL_000c:  nop
    IL_000d:  ret
  } 

  .method private static class [mscorlib]System.String 
          X(int32 n) il managed forwardref
  {
    .maxstack  2
    .locals init ([0] int32 i,
             [1] class [mscorlib]System.String X,
             [2] int32 _Vb_t_i4_0)
    IL_0000:  nop
    IL_0001:  ldc.i4.1
    IL_0002:  stloc.0
    IL_0003:  ldarg.0
    IL_0004:  stloc.2
    IL_0005:  br.s       IL_0018

    IL_0007:  ldloc.1
    IL_0008:  ldstr      "BOB"
    IL_000d:  call       class [mscorlib]System.String [mscorlib]System.String::Concat(class [mscorlib]System.String,
                                                                             class [mscorlib]System.String)
    IL_0012:  stloc.1
    IL_0013:  nop
    IL_0014:  ldloc.0
    IL_0015:  ldc.i4.1
    IL_0016:  add.ovf
    IL_0017:  stloc.0
    IL_0018:  ldloc.0
    IL_0019:  ldloc.2
    IL_001a:  ble.s      IL_0007

    IL_001c:  ldloc.1
    IL_001d:  nop
    IL_001e:  ret
  } 

} 

.class public auto ansi _vbProject extends [mscorlib]System.Object
{
  .field static assembly class [mscorlib]System.Collections.Dictionary _vbEventCollection
  .method public static int32 _main() il managed forwardref
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    .maxstack  8
    IL_0000:  call       void HiDad::main()
	ldc.i4 100
    IL_0005:  ret
  } 

} 
