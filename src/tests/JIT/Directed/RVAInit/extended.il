// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.





.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern xunit.core {}
.assembly extern mscorlib
{
}
.assembly extended
{
}
.custom instance void [mscorlib]System.Security.UnverifiableCodeAttribute::.ctor() = ( 01 00 00 00 ) 

.file alignment 512

.namespace JitTest
{
  .class public auto ansi beforefieldinit Test
         extends [mscorlib]System.Object
  {
     .field private static unsigned int8 buffer at D_1

     .class explicit ansi sealed nested private '__StaticArrayInitTypeSize=1024'
         extends [mscorlib]System.ValueType
     {
        .pack 1
        .size 1024
     } 

    .field private static valuetype JitTest.Test/'__StaticArrayInitTypeSize=1024' buffer2 at D_1

    .method public hidebysig static void 
            fillmem(unsigned int8& a) cil managed
    {
      .maxstack  2
      .locals (unsigned int8& pinned V_0,
               int32 V_1)
      IL_0000:  ldarg.0
      IL_0001:  stloc.0
      IL_0002:  ldloc.0
      IL_0003:  conv.i
      IL_0004:  ldc.i4     0x3ff
      IL_0009:  add
      IL_000a:  ldind.u1
      IL_000b:  ldc.i4.s   123
      IL_000d:  beq.s      IL_0015

      IL_000f:  newobj     instance void [mscorlib]System.Exception::.ctor()
      IL_0014:  throw

      IL_0015:  ldc.i4.0
      IL_0016:  stloc.1
      IL_0017:  br.s       IL_0024

      IL_0019:  ldloc.0
      IL_001a:  conv.i
      IL_001b:  ldloc.1
      IL_001c:  add
      IL_001d:  ldloc.1
      IL_001e:  conv.u1
      IL_001f:  stind.i1
      IL_0020:  ldloc.1
      IL_0021:  ldc.i4.1
      IL_0022:  add
      IL_0023:  stloc.1
      IL_0024:  ldloc.1
      IL_0025:  ldc.i4     0x400
      IL_002a:  blt.s      IL_0019

      IL_002c:  ldc.i4.0
      IL_002d:  conv.u
      IL_002e:  stloc.0
      IL_002f:  ret
    } 

    .method public hidebysig static void 
            chekmem(unsigned int8& a) cil managed
    {
      .maxstack  2
      .locals (unsigned int8& pinned V_0,
               int32 V_1)
      IL_0000:  ldarg.0
      IL_0001:  stloc.0
      IL_0002:  ldc.i4.0
      IL_0003:  stloc.1
      IL_0004:  br.s       IL_0019

      IL_0006:  ldloc.0
      IL_0007:  conv.i
      IL_0008:  ldloc.1
      IL_0009:  add
      IL_000a:  ldind.u1
      IL_000b:  ldloc.1
      IL_000c:  conv.u1
      IL_000d:  beq.s      IL_0015

      IL_000f:  newobj     instance void [mscorlib]System.Exception::.ctor()
      IL_0014:  throw

      IL_0015:  ldloc.1
      IL_0016:  ldc.i4.1
      IL_0017:  add
      IL_0018:  stloc.1
      IL_0019:  ldloc.1
      IL_001a:  ldc.i4     0x400
      IL_001f:  blt.s      IL_0006

      IL_0021:  ldc.i4.0
      IL_0022:  conv.u
      IL_0023:  stloc.0
      IL_0024:  ret
    } 

    .method public hidebysig static int32 Main() cil managed
    {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint
      .maxstack  2
      .locals (int32 V_0)
      IL_0000:  ldsfld     unsigned int8 JitTest.Test::buffer
      IL_0005:  ldc.i4.s   11
      IL_0007:  beq.s      IL_000f

      IL_0009:  newobj     instance void [mscorlib]System.Exception::.ctor()
      IL_000e:  throw

      IL_000f:  ldsflda    unsigned int8 JitTest.Test::buffer
      IL_0014:  call       void JitTest.Test::fillmem(unsigned int8&)
      IL_0019:  ldsflda    unsigned int8 JitTest.Test::buffer
      IL_001e:  call       void JitTest.Test::chekmem(unsigned int8&)
      IL_0023:  ldstr      "Passed => 100"
      IL_0028:  call       void [System.Console]System.Console::WriteLine(string)
      IL_002d:  ldc.i4.s   100
      IL_002f:  stloc.0
      IL_0030:  br.s       IL_0032

      IL_0032:  ldloc.0
      IL_0033:  ret
    } 

    .method public hidebysig specialname rtspecialname 
            instance void .ctor() cil managed
    {
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ret
    } 

  } 

} 
.data D_1 = bytearray(
	0B 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 
	00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 
	00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 
	00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 
	00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 
	00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 
	00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 
	00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 
	00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 
	00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 
	00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 
	00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 
	00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 
	00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 
	00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 
	00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 

	00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 
	00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 
	00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 
	00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 
	00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 
	00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 
	00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 
	00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 
	00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 
	00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 
	00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 
	00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 
	00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 
	00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 
	00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 
	00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 

	00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 
	00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 
	00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 
	00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 
	00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 
	00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 
	00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 
	00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 
	00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 
	00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 
	00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 
	00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 
	00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 
	00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 
	00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 
	00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 

	00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 
	00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 
	00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 
	00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 
	00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 
	00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 
	00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 
	00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 
	00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 
	00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 
	00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 
	00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 
	00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 
	00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 
	00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 
	00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 7B 
)
