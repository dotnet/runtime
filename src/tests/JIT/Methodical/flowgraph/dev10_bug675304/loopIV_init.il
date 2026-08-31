// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.




.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 4:0:0:0
}
.assembly loopIV_init
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::.ctor() = ( 01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
                                                                                                             63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01 )       // ceptionThrows.
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.assembly extern xunit.core {}
.custom instance void [mscorlib]System.Security.UnverifiableCodeAttribute::.ctor() = ( 01 00 00 00 ) 
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY



.namespace Test_loopIV_init_il
{
.class private sequential ansi sealed beforefieldinit V
       extends [mscorlib]System.ValueType
{
  .class sequential ansi sealed nested public beforefieldinit '<x>e__FixedBuffer0'
         extends [mscorlib]System.ValueType
  {
    .pack 0
    .size 8000
    .custom instance void [mscorlib]System.Runtime.CompilerServices.UnsafeValueTypeAttribute::.ctor() = ( 01 00 00 00 ) 
    .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
    .field public int64 FixedElementField
  } // end of class '<x>e__FixedBuffer0'

  .field public valuetype Test_loopIV_init_il.V/'<x>e__FixedBuffer0' x
  .custom instance void [mscorlib]System.Runtime.CompilerServices.FixedBufferAttribute::.ctor(class [mscorlib]System.Type,
                                                                                              int32) = ( 01 00 59 53 79 73 74 65 6D 2E 49 6E 74 36 34 2C   // ..YSystem.Int64,
                                                                                                         20 6D 73 63 6F 72 6C 69 62 2C 20 56 65 72 73 69   //  mscorlib, Versi
                                                                                                         6F 6E 3D 34 2E 30 2E 30 2E 30 2C 20 43 75 6C 74   // on=4.0.0.0, Cult
                                                                                                         75 72 65 3D 6E 65 75 74 72 61 6C 2C 20 50 75 62   // ure=neutral, Pub
                                                                                                         6C 69 63 4B 65 79 54 6F 6B 65 6E 3D 62 37 37 61   // licKeyToken=b77a
                                                                                                         35 63 35 36 31 39 33 34 65 30 38 39 E8 03 00 00   // 5c561934e089....
                                                                                                         00 00 ) 
} // end of class Test_loopIV_init_il.V

.class public abstract auto ansi sealed beforefieldinit Repro
       extends [mscorlib]System.Object
{
  .method public hidebysig static int32 Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    .maxstack  3
    .locals init (valuetype Test_loopIV_init_il.V V_0,
             valuetype Test_loopIV_init_il.V/'<x>e__FixedBuffer0'& V_1,
             int32 V_2)
    IL_0000:  ldloca.s   V_0
    IL_0002:  initobj    Test_loopIV_init_il.V
    IL_0008:  ldloca.s   V_0
    IL_000a:  ldflda     valuetype Test_loopIV_init_il.V/'<x>e__FixedBuffer0' Test_loopIV_init_il.V::x
    IL_0016:  stloc.1
    IL_0017:  ldc.i4.0
    IL_0018:  stloc.2

    IL_0019:  ldloc.1
    IL_0021:  ldloc.2
    IL_0022:  ldloc.2
    IL_0023:  mul
    IL_0024:  conv.i8
    IL_0025:  stind.i8
    IL_0026:  ldloc.2
    IL_0027:  ldc.i4.1
    IL_0028:  add
    IL_0029:  stloc.2
    IL_001b:  ldloc.1
    IL_001d:  ldc.i4.8
    IL_001e:  conv.i
    IL_001f:  add
    IL_0020:  stloc.1
    IL_002a:  ldloc.2
    IL_002b:  ldc.i4     0x3e8
    IL_0030:  blt.s      IL_0019
    IL_0032:  ldc.i4 100
    IL_0034:  ret
  } // end of method Test_loopIV_init_il.Repro::Main

} // end of class Test_loopIV_init_il.Repro



}
