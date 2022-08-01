// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}





.assembly extern mscorlib
{
}
.assembly 'repro_good'
{

  // --- The following custom attribute is added automatically, do not uncomment -------
  //  .custom instance void [mscorlib]System.Diagnostics.DebuggableAttribute::.ctor(bool,
  //                                                                                bool) = ( 01 00 00 01 00 00 ) 

  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.assembly extern xunit.core {}
// MVID: {8652F29E-ADDD-4F29-9B4B-0D71E9CFC6D1}
.imagebase 0x00400000
.subsystem 0x00000003
.file alignment 512
.corflags 0x00000001
// Image base: 0x000006FBFED00000

// =============== CLASS MEMBERS DECLARATION ===================

.class private auto ansi beforefieldinit C1
       extends [mscorlib]System.Object
{
  .method public hidebysig static int32  Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       72 (0x48)
    .maxstack  2
    .locals init (int32 V_0,
             class [mscorlib]System.Exception V_1,
             int32 V_2,
             int32 V_3,
             int32 V_4)
    IL_0000:  ldc.i4.s   99
    IL_0002:  stloc.0
ldc.i4 1
localloc
pop
    .try
    {
      .try
      {
        IL_0007:  ldloc.0
        IL_0008:  call       void [System.Console]System.Console::WriteLine(int32)
        IL_000d:  ldstr      "TestEx"
        IL_0012:  newobj     instance void [mscorlib]System.Exception::.ctor(string)
        IL_0017:  throw

      }  // end .try
      catch [mscorlib]System.Exception 
      {
        IL_0018:  stloc.1
        IL_001f:  ldloc.0
        IL_0020:  call       void [System.Console]System.Console::WriteLine(int32)
        IL_0025:  leave.s    IL_0027

      }  // end handler
      IL_0027:  leave.s    IL_003a

    }  // end .try
    finally
    {
      IL_002f:  ldloc.0
      IL_0030:  call       void [System.Console]System.Console::WriteLine(int32)
      IL_0035:  endfinally
    }  // end handler
    IL_003a:  ldloc.0
    IL_003b:  call       void [System.Console]System.Console::WriteLine(int32)
    IL_0040:  ldc.i4 100
    IL_0041:  stloc.s    V_4
    IL_0043:  br.s       IL_0045

    IL_0045:  ldloc.s    V_4
    IL_0047:  ret
  } // end of method C1::Main

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method C1::.ctor

}
