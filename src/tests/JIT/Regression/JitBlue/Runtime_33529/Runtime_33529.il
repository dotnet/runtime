// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib {}
.assembly Runtime_33529 {}

// Test case where the jit finds a tail call to loop opportunity after
// suppressing zero initialization for a struct.

// S is too large to enregister and has gc fields

.class public sequential ansi sealed beforefieldinit S
       extends [mscorlib]System.ValueType
{
  .field public static int32 s_i0
  .field public int32 x
  .field public int32 a
  .field public int32 i0
  .field public int32 i1
  .field public string s
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(int32 _x,
                               int32 _a) cil managed
  {
    // Code size       46 (0x2e)
    .maxstack  8

    // Modify S.ctor to read from i0 before initializing it        

    ldarg.0
    ldfld int32 S::i0

    IL_0001:  stsfld     int32 S::s_i0
    IL_0006:  ldarg.0
    IL_0007:  ldarg.1
    IL_0008:  stfld      int32 S::x
    IL_000d:  ldarg.0
    IL_000e:  ldarg.2
    IL_000f:  stfld      int32 S::a
    IL_0014:  ldarg.0
    IL_0015:  ldc.i4.3
    IL_0016:  stfld      int32 S::i0
    IL_001b:  ldarg.0
    IL_001c:  ldc.i4.4
    IL_001d:  stfld      int32 S::i1
    IL_0022:  ldarg.0
    IL_0023:  ldstr      "a string"
    IL_0028:  stfld      string S::s
    IL_002d:  ret
  } // end of method S::.ctor

} // end of class S

.class private auto ansi beforefieldinit X
       extends [mscorlib]System.Object
{
  .method public hidebysig static int32  F(int32 x,
                                           int32 a) cil managed
  {
    // Code size       18 (0x12)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  brtrue.s   IL_0005

    IL_0003:  ldarg.1
    IL_0004:  ret

    IL_0005:  ldarg.0
    IL_0006:  ldarg.1

    // This newobj will require a jit temp; the jit will suppress
    // zero init assuming prolog zeroing will suffice.

    IL_0007:  newobj     instance void S::.ctor(int32,int32)

    // Inlining G will introduce a recursive tail call to F which
    // the jit will optimize into a loop

    IL_000c:  call       int32 X::G(valuetype S)
    IL_0011:  ret
  } // end of method X::F

  .method public hidebysig static int32  G(valuetype S s) cil managed
  {
    // Code size       27 (0x1b)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldfld      int32 S::x
    IL_0006:  ldc.i4.1
    IL_0007:  sub
    IL_0008:  ldarg.0
    IL_0009:  ldfld      int32 S::a
    IL_000e:  ldarg.0
    IL_000f:  ldfld      int32 S::x
    IL_0014:  mul
    IL_0015:  call       int32 X::F(int32,
                                    int32)
    IL_001a:  ret
  } // end of method X::G

  .method public hidebysig static int32  H(int32 x) cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldc.i4.1
    IL_0002:  call       int32 X::F(int32,
                                    int32)
    IL_0007:  ret
  } // end of method X::H

  .method public hidebysig static int32  Main() cil managed
  {
    .entrypoint
    // Code size       78 (0x4e)
    .maxstack  4
    .locals init ([0] int32 r)
    IL_0000:  ldc.i4.6
    IL_0001:  call       int32 X::H(int32)
    IL_0006:  stloc.0
    IL_0007:  ldsfld     int32 S::s_i0
    IL_000c:  brtrue.s   IL_0018

    IL_000e:  ldloc.0
    IL_000f:  ldc.i4     0x2d0
    IL_0014:  ceq
    IL_0016:  br.s       IL_0019

    IL_0018:  ldc.i4.0
    IL_0019:  dup
    IL_001a:  brfalse.s  IL_0028

    IL_001c:  ldstr      "Pass"
    IL_0021:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_0026:  br.s       IL_0047

    IL_0028:  ldstr      "Fail, expected S.s_i0 == 0, got {0}, expected r =="
    + " 720, got {1}"
    IL_002d:  ldsfld     int32 S::s_i0
    IL_0032:  box        [mscorlib]System.Int32
    IL_0037:  ldloc.0
    IL_0038:  box        [mscorlib]System.Int32
    IL_003d:  call       string [mscorlib]System.String::Format(string,
                                                                object,
                                                                object)
    IL_0042:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_0047:  brtrue.s   IL_004b

    IL_0049:  ldc.i4.m1
    IL_004a:  ret

    IL_004b:  ldc.i4.s   100
    IL_004d:  ret
  } // end of method X::Main

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method X::.ctor

} // end of class X


// =============================================================

// *********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file ixs.res
