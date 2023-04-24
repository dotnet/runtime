// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }

.assembly IndirectIL { }

.class public abstract auto ansi sealed beforefieldinit ILTests
       extends [System.Runtime]System.Object
{
  .method public hidebysig static int32  StaticClass() cil managed noinlining aggressiveoptimization
  {
    // Code size       12 (0xc)
    .maxstack  8
    IL_0000:  ldftn      int32 Cl::B()
    IL_0006:  calli      int32()
    IL_000b:  ret
  } // end of method ILTests::A

  .method public hidebysig static int32  InstanceClass() cil managed noinlining aggressiveoptimization
  {
    // Code size       17 (0x11)
    .maxstack  2
    .locals init (class Cl V_0)
    IL_0000:  newobj     instance void Cl::.ctor()
    IL_0005:  ldftn      instance int32 Cl::A()
    IL_000b:  calli      instance int32()
    IL_0010:  ret
  } // end of method ILTests::B

  .method public hidebysig static int32  InstanceExplicitClass() cil managed noinlining aggressiveoptimization
  {
    // Code size       17 (0x11)
    .maxstack  2
    .locals init (class Cl V_0)
    IL_0000:  newobj     instance void Cl::.ctor()
    IL_0005:  ldftn      instance int32 Cl::A()
    IL_000b:  calli      explicit instance int32(class Cl)
    IL_0010:  ret
  } // end of method ILTests::C

  .method public hidebysig static int32  StaticClassParam() cil managed noinlining aggressiveoptimization
  {
    // Code size       15 (0xf)
    .maxstack  2
    .locals init (method int32 *(int32) V_0)
    IL_0000:  ldftn      int32 Cl::D(int32)
    IL_0006:  stloc.0
    IL_0007:  ldc.i4.0
    IL_0008:  ldloc.0
    IL_0009:  calli      int32(int32)
    IL_000e:  ret
  } // end of method ILTests::D

  .method public hidebysig static int32  InstanceClassParam() cil managed noinlining aggressiveoptimization
  {
    // Code size       22 (0x16)
    .maxstack  3
    .locals init (class Cl V_0,
             method int32 *(int32) V_1)
    IL_0000:  newobj     instance void Cl::.ctor()
    IL_0005:  stloc.0
    IL_0006:  ldftn      instance int32 Cl::C(int32)
    IL_000c:  stloc.1
    IL_000d:  ldloc.0
    IL_000e:  ldc.i4.0
    IL_000f:  ldloc.1
    IL_0010:  calli      instance int32(int32)
    IL_0015:  ret
  } // end of method ILTests::E

  .method public hidebysig static int32  InstanceExplicitClassParam() cil managed noinlining aggressiveoptimization
  {
    // Code size       22 (0x16)
    .maxstack  3
    .locals init (class Cl V_0,
             method int32 *(int32) V_1)
    IL_0000:  newobj     instance void Cl::.ctor()
    IL_0005:  stloc.0
    IL_0006:  ldftn      instance int32 Cl::C(int32)
    IL_000c:  stloc.1
    IL_000d:  ldloc.0
    IL_000e:  ldc.i4.0
    IL_000f:  ldloc.1
    IL_0010:  calli      explicit instance int32(class Cl,int32)
    IL_0015:  ret
  } // end of method ILTests::F

  .method public hidebysig static int32  StaticStruct() cil managed noinlining aggressiveoptimization
  {
    // Code size       12 (0xc)
    .maxstack  8
    IL_0000:  ldftn      int32 St::B()
    IL_0006:  calli      int32()
    IL_000b:  ret
  } // end of method ILTests::G

  .method public hidebysig static int32  InstanceStruct() cil managed noinlining aggressiveoptimization
  {
    // Code size       22 (0x16)
    .maxstack  2
    .locals init (valuetype St V_0)
    IL_0000:  ldloca.s   V_0
    IL_0002:  initobj    St
    IL_0008:  ldloca.s   V_0
    IL_000a:  ldftn      instance int32 St::A()
    IL_0010:  calli      instance int32()
    IL_0015:  ret
  } // end of method ILTests::H

  .method public hidebysig static int32  InstanceExplicitStruct() cil managed noinlining aggressiveoptimization
  {
    // Code size       22 (0x16)
    .maxstack  2
    .locals init (valuetype St V_0)
    IL_0000:  ldloca.s   V_0
    IL_0002:  initobj    St
    IL_0008:  ldloca.s   V_0
    IL_000a:  ldftn      instance int32 St::A()
    IL_0010:  calli      explicit instance int32(valuetype St&)
    IL_0015:  ret
  } // end of method ILTests::I

  .method public hidebysig static int32  StaticStructParam() cil managed noinlining aggressiveoptimization
  {
    // Code size       15 (0xf)
    .maxstack  2
    .locals init (method int32 *(int32) V_0)
    IL_0000:  ldftn      int32 St::D(int32)
    IL_0006:  stloc.0
    IL_0007:  ldc.i4.0
    IL_0008:  ldloc.0
    IL_0009:  calli      int32(int32)
    IL_000e:  ret
  } // end of method ILTests::J

  .method public hidebysig static int32  InstanceStructParam() cil managed noinlining aggressiveoptimization
  {
    // Code size       25 (0x19)
    .maxstack  3
    .locals init (valuetype St V_0,
             method int32 *(int32) V_1)
    IL_0000:  ldloca.s   V_0
    IL_0002:  initobj    St
    IL_0008:  ldftn      instance int32 St::C(int32)
    IL_000e:  stloc.1
    IL_000f:  ldloca.s   V_0
    IL_0011:  ldc.i4.0
    IL_0012:  ldloc.1
    IL_0013:  calli      instance int32(int32)
    IL_0018:  ret
  } // end of method ILTests::K

  .method public hidebysig static int32  InstanceExplicitStructParam() cil managed noinlining aggressiveoptimization
  {
    // Code size       25 (0x19)
    .maxstack  3
    .locals init (valuetype St V_0,
             method int32 *(int32) V_1)
    IL_0000:  ldloca.s   V_0
    IL_0002:  initobj    St
    IL_0008:  ldftn      instance int32 St::C(int32)
    IL_000e:  stloc.1
    IL_000f:  ldloca.s   V_0
    IL_0011:  ldc.i4.0
    IL_0012:  ldloc.1
    IL_0013:  calli      explicit instance int32(valuetype St&,int32)
    IL_0018:  ret
  } // end of method ILTests::L

} // end of class ILTests

.class public auto ansi sealed beforefieldinit Cl
       extends [System.Runtime]System.Object
{
  .method public hidebysig instance int32
          A() cil managed aggressiveinlining aggressiveoptimization
  {
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  ldc.i4.1
    IL_0001:  ret
  } // end of method Cl::A

  .method public hidebysig static int32  B() cil managed aggressiveinlining aggressiveoptimization
  {
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  ldc.i4.2
    IL_0001:  ret
  } // end of method Cl::B

  .method public hidebysig instance int32
          C(int32 a) cil managed aggressiveinlining aggressiveoptimization
  {
    // Code size       4 (0x4)
    .maxstack  8
    IL_0000:  ldc.i4.3
    IL_0001:  ldarg.1
    IL_0002:  add
    IL_0003:  ret
  } // end of method Cl::C

  .method public hidebysig static int32  D(int32 a) cil managed aggressiveinlining aggressiveoptimization
  {
    // Code size       4 (0x4)
    .maxstack  8
    IL_0000:  ldc.i4.4
    IL_0001:  ldarg.0
    IL_0002:  add
    IL_0003:  ret
  } // end of method Cl::D

  .method public hidebysig specialname rtspecialname
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [System.Runtime]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Cl::.ctor

} // end of class Cl

.class public sequential ansi sealed beforefieldinit St
       extends [System.Runtime]System.ValueType
{
  .pack 0
  .size 1
  .custom instance void [System.Runtime]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = ( 01 00 00 00 )
  .method public hidebysig instance int32
          A() cil managed aggressiveinlining aggressiveoptimization
  {
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  ldc.i4.1
    IL_0001:  ret
  } // end of method St::A

  .method public hidebysig static int32  B() cil managed aggressiveinlining aggressiveoptimization
  {
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  ldc.i4.2
    IL_0001:  ret
  } // end of method St::B

  .method public hidebysig instance int32
          C(int32 a) cil managed aggressiveinlining aggressiveoptimization
  {
    // Code size       4 (0x4)
    .maxstack  8
    IL_0000:  ldc.i4.3
    IL_0001:  ldarg.1
    IL_0002:  add
    IL_0003:  ret
  } // end of method St::C

  .method public hidebysig static int32  D(int32 a) cil managed aggressiveinlining aggressiveoptimization
  {
    // Code size       4 (0x4)
    .maxstack  8
    IL_0000:  ldc.i4.4
    IL_0001:  ldarg.0
    IL_0002:  add
    IL_0003:  ret
  } // end of method St::D

} // end of class St
