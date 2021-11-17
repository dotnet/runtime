// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         
  .ver 2:0:3600:0
}
.assembly template
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 )
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.assembly extern xunit.core {}

.class public auto ansi beforefieldinit Test_static_ref
       extends [mscorlib]System.Object
{
  .method public hidebysig static int32  Main(string[] args) cil managed noinlining
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    .maxstack  5
    .locals init (int32 V_0,
             int32 V_1,
             int32 V_2,
             int32 V_3,
             bool V_4)
    IL_0000:  ldc.i4.1
    IL_0001:  stloc.0
    IL_0002:  ldc.i4.s   100
    IL_0004:  stloc.1
    IL_0005:  ldloca.s   V_0
    IL_0007:  ldloc.1
    IL_0008:  ldc.i4.1
    IL_0009:  ldftn       int32 [mscorlib]System.Threading.Interlocked::CompareExchange(int32&,
                                                                                       int32,
                                                                                       int32)
    IL_000a:  calli       int32 (int32&,
                                                                                       int32,
                                                                                       int32)
    IL_000e:  stloc.2
    IL_000f:  ldloc.0
    IL_0010:  ldloc.1
    IL_0011:  ceq
    IL_0013:  ldc.i4.0
    IL_0014:  ceq
    IL_0016:  stloc.s    V_4
    IL_0018:  ldloc.s    V_4
    IL_001a:  brtrue.s   IL_002c

    IL_001c:  ldstr      "passed"
    IL_0021:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0026:  nop
    IL_0027:  ldc.i4.s   100
    IL_0029:  stloc.3
    IL_002a:  br.s       IL_003b

    IL_002c:  ldstr      "failed"
    IL_0031:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0036:  nop
    IL_0037:  ldc.i4.1
    IL_0038:  stloc.3
    IL_0039:  br.s       IL_003b

    IL_003b:  ldloc.3
    IL_003c:  ret
  } 

  .method public hidebysig specialname rtspecialname
          instance void  .ctor() cil managed
  {
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } 

} 
