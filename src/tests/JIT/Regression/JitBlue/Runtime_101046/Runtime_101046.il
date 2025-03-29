// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime { }
.assembly extern xunit.core { }
.assembly Runtime_101046 { }

.class public auto ansi beforefieldinit Runtime_101046
       extends [System.Runtime]System.Object
{
  .method public hidebysig static int32  TestEntryPoint() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       23 (0x17)
    .entrypoint
    .maxstack  2
    .locals init (uint16 V_0)
    IL_0000:  ldc.i4     0xff85
    IL_0005:  stloc.0
    IL_0006:  ldloca.s   V_0
    IL_0008:  call       int32 Runtime_101046::Test(uint16&)
    IL_000d:  ldc.i4.s   -123
    IL_000f:  beq.s      IL_0014

    IL_0011:  ldc.i4.s   101
    IL_0013:  ret

    IL_0014:  ldc.i4.s   100
    IL_0016:  ret
  } // end of method Runtime_101046::TestEntryPoint

  .method private hidebysig static int32 
          Test(uint16& p) cil managed noinlining
  {
    // Code size       9 (0x9)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldind.u2
    //IL_0002:  conv.i2
    IL_0003:  call       int32 Runtime101046Native::ReturnExtendedShort(int16)
    IL_0008:  ret
  } // end of method Runtime_101046::Test
} // end of class Runtime_101046

.class private abstract auto ansi sealed beforefieldinit Runtime101046Native
       extends [System.Runtime]System.Object
{
  .method public hidebysig static pinvokeimpl("Runtime101046Native" winapi) 
          int32  ReturnExtendedShort(int16 s) cil managed preservesig
  {
  }
} // end of class Runtime101046Native
