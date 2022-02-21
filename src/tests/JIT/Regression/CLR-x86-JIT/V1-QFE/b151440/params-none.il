// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}

.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 2:0:3600:0
}
.assembly 'params-none'
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 )
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.assembly extern xunit.core {}

.class public auto ansi beforefieldinit Test_params_none
       extends [mscorlib]System.Object
{
  .field public int32 i
  .method public hidebysig specialname rtspecialname
          instance void  .ctor(int32 i) cil managed
  {
    // Code size       16 (0x10)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  ldarg.0
    IL_0008:  ldarg.1
    IL_0009:  stfld      int32 Test_params_none::i
    IL_000e:  nop
    IL_000f:  ret
  } // end of method Test::.ctor

  .method public hidebysig specialname rtspecialname
          instance void  .ctor() cil managed
  {
    // Code size       16 (0x10)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  ldarg.0
    IL_0008:  ldc.i4.1
    IL_0009:  stfld      int32 Test_params_none::i
    IL_000e:  nop
    IL_000f:  ret
  } // end of method Test::.ctor

  .method public hidebysig static int32  Main() cil managed noinlining
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       78 (0x4e)
    .maxstack  2
    .locals init (class Test_params_none V_0,
             class Test_params_none V_1,
             int32 V_2,
             bool V_3)
      ldc.i4.s   100
      newobj     instance void Test_params_none::.ctor(int32)
      stloc.0
      newobj     instance void Test_params_none::.ctor()
      stloc.1
      ldloc.0
      ldftn   instance object [mscorlib]System.Object::MemberwiseClone()
      calli		instance object()
      castclass  Test_params_none
      stloc.1
      ldloc.1
      ldfld      int32 Test_params_none::i
      ldc.i4.s   100
      ceq
      ldc.i4.0
      ceq
      stloc.3
      ldloc.3
      brtrue.s   IL_0038

      ldstr      "Passed"
      call       void [System.Console]System.Console::WriteLine(string)
      nop
      br.s       IL_0043

    IL_0038:  ldstr      "Failed"
      call       void [System.Console]System.Console::WriteLine(string)
      nop
    IL_0043:  ldloc.1
      ldfld      int32 Test_params_none::i
      stloc.2
      br.s       IL_004c

    IL_004c:  ldloc.2
      ret
  } // end of method Test::Main

} // end of class Test

