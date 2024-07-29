// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly legacy library 'inl001' {}

.class public auto ansi beforefieldinit Test_inl001
       extends [mscorlib]System.Object
{
  .method public hidebysig static int32  foo() cil managed
  {
    // Code size       10 (0xa)
    .maxstack  2
    .locals init (int32 V_0,
             int32 V_1)
    IL_0000:  ldloc.0
    IL_0001:  ldc.i4.1
    IL_0002:  add
    IL_0003:  stloc.0
    IL_0004:  ldloc.0
    IL_0005:  stloc.1
    IL_0006:  br.s       IL_0008

    IL_0008:  ldloc.1
    IL_0009:  ret
  } // end of method Test::foo

  .method public hidebysig static int32  Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       45 (0x2d)
    .maxstack  2
    .locals init (int32 V_0,
             int32 V_1,
             int32 V_2)
    IL_0000:  ldc.i4.0
    IL_0001:  stloc.1
    IL_0002:  ldc.i4.0
    IL_0003:  stloc.0
    IL_0004:  br.s       IL_0012

    IL_0006:  ldloc.1
    IL_0007:  call       int32 Test_inl001::foo()
    IL_000c:  add
    IL_000d:  stloc.1
    IL_000e:  ldloc.0
    IL_000f:  ldc.i4.1
    IL_0010:  add
    IL_0011:  stloc.0
    IL_0012:  ldloc.0
    IL_0013:  ldc.i4.s   10
    IL_0015:  blt.s      IL_0006

    IL_0017:  ldloc.1
    IL_0018:  call       void [System.Console]System.Console::WriteLine(int32)
    IL_001d:  ldloc.1
    IL_001e:  ldc.i4.s   10
    IL_0020:  bne.un.s   IL_0027

    IL_0022:  ldc.i4.s   100
    IL_0024:  stloc.2
    IL_0025:  br.s       IL_002b

    IL_0027:  ldc.i4.1
    IL_0028:  stloc.2
    IL_0029:  br.s       IL_002b

    IL_002b:  ldloc.2
    IL_002c:  ret
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
