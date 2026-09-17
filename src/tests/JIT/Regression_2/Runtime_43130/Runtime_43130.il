// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Test several simple combinations when we inline a method which takes an argument
// with an implicit cast, when we substitute the use as the argument in the inlined body
// we should be careful about handling the 'PUTARG_TYPE' node that shows the cast.

.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }
.assembly extern System.Runtime.Extensions
{
}
.assembly Runtime_43130
{
}
.assembly extern xunit.core {}

.class public auto ansi beforefieldinit Runtime_43130
       extends [System.Runtime]System.Object
{
  .method public hidebysig static int32  Test1(int32 i1) cil managed noinlining
  {
    // Code size       20 (0x14)
    .maxstack  2
    .locals init (int32 V_0,
             int32 V_1)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldc.i4.1
    IL_0003:  add
    IL_0004:  stloc.0
    IL_0005:  ldloc.0
    // IL_0006:  conv.i2
    IL_0007:  call       void Runtime_43130::Inline1(int16)
    IL_000c:  nop
    IL_000d:  ldc.i4.s   100
    IL_000f:  stloc.1
    IL_0010:  br.s       IL_0012

    IL_0012:  ldloc.1
    IL_0013:  ret
  } // end of method Runtime_43130::Test1

  .method public hidebysig static void  Inline1(int16 s) cil managed aggressiveinlining
  {
    // Code size       9 (0x9)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  call       void Runtime_43130::CallAMethod1(int16)
    IL_0007:  nop
    IL_0008:  ret
  } // end of method Runtime_43130::Inline1

  .method public hidebysig static void  CallAMethod1(int16 s) cil managed noinlining
  {
    // Code size       4 (0x4)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  br.s       IL_0003

    IL_0003:  ret
  } // end of method Runtime_43130::CallAMethod1

  .method public hidebysig static int32  Test2(int32 i1) cil managed noinlining
  {
    // Code size       17 (0x11)
    .maxstack  2
    .locals init (int32 V_0,
             int32 V_1)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldc.i4.1
    IL_0003:  add
    IL_0004:  stloc.0
    IL_0005:  ldloc.0
    // IL_0006:  conv.i2
    IL_0007:  call       int32 Runtime_43130::Inline2(int16)
    IL_000c:  stloc.1
    IL_000d:  br.s       IL_000f

    IL_000f:  ldloc.1
    IL_0010:  ret
  } // end of method Runtime_43130::Test2

  .method public hidebysig static int32  Inline2(int16 s) cil managed aggressiveinlining
  {
    // Code size       12 (0xc)
    .maxstack  2
    .locals init (int32 V_0,
             int32 V_1)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldc.i4.s   10
    IL_0004:  add
    IL_0005:  stloc.0
    IL_0006:  ldloc.0
    IL_0007:  stloc.1
    IL_0008:  br.s       IL_000a

    IL_000a:  ldloc.1
    IL_000b:  ret
  } // end of method Runtime_43130::Inline2

  .method public hidebysig static int32  Test3(int32 i1) cil managed noinlining
  {
    // Code size       20 (0x14)
    .maxstack  2
    .locals init (int32 V_0,
             int32 V_1)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldc.i4.1
    IL_0003:  add
    IL_0004:  stloc.0
    IL_0005:  ldloc.0
    // IL_0006:  conv.i2
    IL_0007:  call       int32 Runtime_43130::Inline3(int16)
    IL_000c:  pop
    IL_000d:  ldc.i4.s   100
    IL_000f:  stloc.1
    IL_0010:  br.s       IL_0012

    IL_0012:  ldloc.1
    IL_0013:  ret
  } // end of method Runtime_43130::Test3

  .method public hidebysig static int32  Inline3(int16 s) cil managed aggressiveinlining
  {
    // Code size       17 (0x11)
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  call       void Runtime_43130::CallAMethod3(int16)
    IL_0007:  nop
    IL_0008:  ldarg.0
    IL_0009:  ldc.i4.s   10
    IL_000b:  add
    IL_000c:  stloc.0
    IL_000d:  br.s       IL_000f

    IL_000f:  ldloc.0
    IL_0010:  ret
  } // end of method Runtime_43130::Inline3

  .method public hidebysig static void  CallAMethod3(int16 s) cil managed noinlining
  {
    // Code size       4 (0x4)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  br.s       IL_0003

    IL_0003:  ret
  } // end of method Runtime_43130::CallAMethod3

  .method public hidebysig static int32  Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       29 (0x1d)
    .maxstack  1
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldc.i4.1
    IL_0002:  call       int32 Runtime_43130::Test1(int32)
    IL_0007:  pop
    IL_0008:  ldc.i4.1
    IL_0009:  call       int32 Runtime_43130::Test2(int32)
    IL_000e:  pop
    IL_000f:  ldc.i4.1
    IL_0010:  call       int32 Runtime_43130::Test3(int32)
    IL_0015:  pop
    IL_0016:  ldc.i4.s   100
    IL_0018:  stloc.0
    IL_0019:  br.s       IL_001b

    IL_001b:  ldloc.0
    IL_001c:  ret
  } // end of method Runtime_43130::Main

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [System.Runtime]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  } // end of method Runtime_43130::.ctor

} // end of class Runtime_43130
