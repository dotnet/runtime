// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern xunit.core {}

.assembly extern mscorlib {}
.assembly extern tls1 {}
.assembly extern tls2 {}
.assembly tlstest {}
.namespace 'tls'
{
  .class public auto ansi beforefieldinit tlstest
         extends [mscorlib]System.Object
  {
    .method private hidebysig static uint8 
            f(uint8* p) cil managed
    {
      .maxstack  2
      .locals (uint8 V_0)
      IL_0000:  ldarg.0
      IL_0001:  ldind.u1
      IL_0002:  ldc.i4.1
      IL_0003:  add
      IL_0004:  conv.u1
      IL_0005:  stloc.0
      IL_0006:  br.s       IL_0008

      IL_0008:  ldloc.0
      IL_0009:  ret
    } // end of method tlstest::f

    .method public hidebysig static int32 Main() cil managed
    {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint
      .maxstack  2
      .locals (uint8& pinned V_0)
      IL_0000:  ldsfld     int32 [tls1]'tls'.tls1::i
      IL_0005:  ldsfld     uint8 [tls1]'tls'.tls1::b
      IL_000a:  add
      IL_000b:  ldsfld     uint8 [tls2]'tls'.tls2::b
      IL_0010:  add
      IL_0011:  call       void [System.Console]System.Console::WriteLine(int32)
      IL_0016:  ldsflda    uint8 [tls1]'tls'.tls1::b
      IL_001b:  stloc.0
      IL_001c:  ldsfld     int32 [tls1]'tls'.tls1::i
      IL_0021:  ldloc.0
      IL_0022:  conv.i
      IL_0023:  call       uint8 'tls'.tlstest::f(uint8*)
      IL_0028:  add
      IL_0029:  ldsfld     uint8 [tls2]'tls'.tls2::b
      IL_002e:  add
      IL_002f:  call       void [System.Console]System.Console::WriteLine(int32)
      IL_0034:  ldc.i4.0
      IL_0035:  conv.u
      IL_0036:  stloc.0
      			ldc.i4 100
      IL_0037:  ret
    } // end of method tlstest::Main

    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ret
    } // end of method tlstest::.ctor

  } // end of class tlstest

}
