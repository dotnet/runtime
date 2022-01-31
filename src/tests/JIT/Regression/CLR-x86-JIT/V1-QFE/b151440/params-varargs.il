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
.assembly 'params-varargs'
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 )
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.assembly extern xunit.core {}

.class public auto ansi beforefieldinit Test_params_varargs
       extends [mscorlib]System.Object
{
  .method public hidebysig static int32  Main() cil managed noinlining
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       161 (0xa1)
    .maxstack  4
    .locals init (string V_0,
             string[] V_1,
             int32 V_2,
             bool V_3)
      ldstr      "this,is.a list:of words"
      stloc.0
      ldloc.0
      ldc.i4.4
      newarr     [mscorlib]System.Char
      dup
      ldtoken    field int64 '<PrivateImplementationDetails>{CC5C6C5B-921F-42AE-99FB-B4C7E6281BDC}'::'$$method0x6000001-1'
      call       void [mscorlib]System.Runtime.CompilerServices.RuntimeHelpers::InitializeArray(class [mscorlib]System.Array,
                                                                                                        valuetype [mscorlib]System.RuntimeFieldHandle)
      ldftn   instance string[] [mscorlib]System.String::Split(char[])
      calli		instance string[](char[])
      stloc.1
    IL_001e:  ldloc.1
      ldc.i4.0
      ldelem.ref
      ldstr      "this"
      callvirt   instance bool [mscorlib]System.String::Equals(string)
      brfalse.s  IL_007b

      ldloc.1
      ldc.i4.1
      ldelem.ref
      ldstr      "is"
      callvirt   instance bool [mscorlib]System.String::Equals(string)
      brfalse.s  IL_007b

      ldloc.1
      ldc.i4.2
      ldelem.ref
      ldstr      "a"
      callvirt   instance bool [mscorlib]System.String::Equals(string)
      brfalse.s  IL_007b

      ldloc.1
      ldc.i4.3
      ldelem.ref
      ldstr      "list"
      callvirt   instance bool [mscorlib]System.String::Equals(string)
      brfalse.s  IL_007b

      ldloc.1
      ldc.i4.4
      ldelem.ref
      ldstr      "of"
      callvirt   instance bool [mscorlib]System.String::Equals(string)
      brfalse.s  IL_007b

      ldloc.1
      ldc.i4.5
      ldelem.ref
      ldstr      "words"
      callvirt   instance bool [mscorlib]System.String::Equals(string)
      ldc.i4.0
      ceq
      br.s       IL_007c

    IL_007b:  ldc.i4.1
    IL_007c:  stloc.3
      ldloc.3
      brtrue.s   IL_0090

      ldstr      "Passed"
      call       void [System.Console]System.Console::WriteLine(string)
      nop
      ldc.i4.s   100
      stloc.2
      br.s       IL_009f

    IL_0090:  ldstr      "Failed"
      call       void [System.Console]System.Console::WriteLine(string)
      nop
      ldc.i4.1
      stloc.2
      br.s       IL_009f

    IL_009f:  ldloc.2
      ret
  } // end of method Test::Main

  .method public hidebysig specialname rtspecialname
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Test::.ctor

} // end of class Test

.class private auto ansi '<PrivateImplementationDetails>{CC5C6C5B-921F-42AE-99FB-B4C7E6281BDC}'
       extends [mscorlib]System.Object
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
  .field static assembly int64 '$$method0x6000001-1' at I_00002050
} // end of class '<PrivateImplementationDetails>{CC5C6C5B-921F-42AE-99FB-B4C7E6281BDC}'


// =============================================================

.data cil I_00002050 = bytearray (
                 20 00 2C 00 2E 00 3A 00)                         //  .,...:.
