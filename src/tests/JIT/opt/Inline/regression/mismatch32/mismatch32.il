// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// When the jit considers inlining B it can get itself into
// trouble because of the type mismatch. This test tries to
// ensure the jit backs out of the inline successfully, then
// throws exception when calls a method with mismatch, then catches it.
//

.assembly extern mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console {}
.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }
.assembly 'mismatch32' {}

.class public sealed F extends System.ValueType
{

.method public instance void .ctor()
{
   ret
}

.field public string A

.method private static int32 B(int32 X, int32 Y)
{
   ldarg.0
   ret
}

.method private static void Test() cil managed
{
   .locals init (valuetype F v_0)
   newobj instance void F::.ctor()
   ldc.i4 33
   call  int32 F::B(int32, int32)
   pop
}

.method public hidebysig static int32 Main() cil managed
{
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint
      .locals init (int32 v_0)
      // Code size       28 (0x1c)
      .maxstack  2
      IL_0000:  nop
      .try
      {
        IL_0005:  call void F::Test()
        IL_000c:  leave.s    IL_0015
      }  // end .try
      catch [System.Runtime]System.InvalidProgramException 
      {
        IL_000e:  pop
        IL_000f:  nop
        IL_0010:  ldc.i4.s   100
        IL_0012:  stloc.0
        IL_0013:  leave.s    IL_001a
      }  // end handler
      IL_0015:  ldc.i4.s   101
      IL_0017:  stloc.0
      IL_0018:  br.s       IL_001a
      IL_001a:  ldloc.0
      IL_001b:  ret
}
}