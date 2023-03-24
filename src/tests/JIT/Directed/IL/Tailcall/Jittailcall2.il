// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.



.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'JitTailcall2'// as "aaa"
{
  
  
}
.namespace DefaultNamespace
{
  .class public auto ansi dotailcall extends [mscorlib]System.Object
  {
    .field private static int32 ifoo
    .field private static int32 iboo
    .field private static int32 igoo
    .method public instance int32  foo(int32 i,
                                       float32 f,
                                       wchar c) il managed
    {
      // Code size       45 (0x2d)
      .maxstack  6
      .locals (float32 V_0,
               int64 V_1,
               int16 V_2,
               int16 V_3)
      IL_0000:  ldsfld     int32 DefaultNamespace.dotailcall::ifoo
      IL_0005:  ldc.i4.1
      IL_0006:  add
      IL_0007:  stsfld     int32 DefaultNamespace.dotailcall::ifoo
      IL_000c:  ldarg.2
      IL_000d:  stloc.0
      IL_000e:  ldarg.2
      IL_000f:  conv.i8
      IL_0010:  stloc.1
      IL_0011:  ldarg.3
      IL_0012:  conv.i2
      IL_0013:  stloc.2
      IL_0014:  ldarg.3
      IL_0015:  conv.i2
      IL_0016:  stloc.3
      IL_0017:  ldarg.1
      IL_0018:  ldc.i4.0
      IL_0019:  bne.un.s   IL_001d

      IL_001b:  ldc.i4.0
      IL_001c:  ret

      IL_001d:  ldarg.0
      IL_001e:  ldarg.1
      IL_001f:  ldc.i4.1
      IL_0020:  sub
      IL_0021:  ldloc.0
      IL_0022:  ldloc.1
      IL_0023:  ldloc.2
      IL_0024:  ldloc.3
      IL_0025:  tail. call       instance int32 DefaultNamespace.dotailcall::boo(int32,
                                                                           float32,
                                                                           int64,
                                                                           int16,
                                                                           int16)
//      IL_002a:  pop
//    IL_002b:  ldc.i4.1
      IL_002c:  ret
    } // end of method 'dotailcall::foo'

    .method public instance int32  boo(int32 i,
                                       float32 d,
                                       int64 l,
                                       int16 s,
                                       int16 v16) il managed
    {
      // Code size       66 (0x42)
      .maxstack  5
      .locals (class [mscorlib]System.Object V_0,
               bool V_1,
               class [mscorlib]System.String V_2)
      IL_0000:  ldsfld     int32 DefaultNamespace.dotailcall::iboo
      IL_0005:  ldc.i4.1
      IL_0006:  add
      IL_0007:  stsfld     int32 DefaultNamespace.dotailcall::iboo
      IL_000c:  newobj     instance void [mscorlib]System.Object::.ctor()
      IL_0011:  stloc.0
      IL_0012:  ldarg.s   d
      IL_0014:  box        [mscorlib]System.Single
      IL_0019:  stloc.0
      IL_001a:  ldarg.s    v16
      IL_001c:  ldc.i4.0
      IL_001d:  beq.s      IL_0023

      IL_001f:  ldc.i4.1
      IL_0020:  stloc.1
      IL_0021:  br.s       IL_0025

      IL_0023:  ldc.i4.0
      IL_0024:  stloc.1
      IL_0025:  ldarga.s   s
      IL_0027:  call       instance class [mscorlib]System.String [mscorlib]System.Int16::ToString()
      IL_002c:  stloc.2
      IL_002d:  ldarg.1
      IL_002e:  ldc.i4.0
      IL_002f:  bne.un.s   IL_0033

      IL_0031:  ldc.i4.0
      IL_0032:  ret

      IL_0033:  ldarg.0
      IL_0034:  ldarg.1
      IL_0035:  ldc.i4.1
      IL_0036:  sub
      IL_0037:  ldloc.0
      IL_0038:  ldloc.2
      IL_0039:  ldloc.1
      IL_003a:  tail. call       instance int32 DefaultNamespace.dotailcall::goo(int32,
                                                                           class [mscorlib]System.Object,
                                                                           class [mscorlib]System.String,
                                                                           bool)
//      IL_003f:  pop
//      IL_0040:  ldc.i4.1
      IL_0041:  ret
    } // end of method 'dotailcall::boo'

    .method public instance int32  goo(int32 i,
                                       class [mscorlib]System.Object v,
                                       class [mscorlib]System.String str,
                                       bool b) il managed
    {
      // Code size       41 (0x29)
      .maxstack  4
      .locals (float32 V_0,
               wchar V_1)
      IL_0000:  ldsfld     int32 DefaultNamespace.dotailcall::igoo
      IL_0005:  ldc.i4.1
      IL_0006:  add
      IL_0007:  stsfld     int32 DefaultNamespace.dotailcall::igoo
      IL_000c:  ldarg.2
      IL_000d:  unbox      [mscorlib]System.Single
      IL_0012:  ldind.r4
      IL_0013:  stloc.0
      IL_0014:  ldarg.1
      IL_0015:  conv.u2
      IL_0016:  stloc.1
      IL_0017:  ldarg.1
      IL_0018:  ldc.i4.0
      IL_0019:  bne.un.s   IL_001d

      IL_001b:  ldc.i4.0
      IL_001c:  ret

      IL_001d:  ldarg.0
      IL_001e:  ldarg.1
      IL_001f:  ldc.i4.1
      IL_0020:  sub
      IL_0021:  ldloc.0
      IL_0022:  ldloc.1
      IL_0023:  tail. call       instance int32 DefaultNamespace.dotailcall::foo(int32,
                                                                           float32,
                                                                           wchar)
      IL_0028:  ret
    } // end of method 'dotailcall::goo'

    .method public static int32 Main() il managed
    {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint
      // Code size       117 (0x75)
      .maxstack  4
      .locals (int32 V_0,
               float32 V_1,
               wchar V_2,
               class DefaultNamespace.dotailcall V_3)
      IL_0000:  ldc.i4     10000
      IL_0005:  stloc.0
      IL_0006:  ldc.r4     (CD FC F9 44)
      IL_000b:  stloc.1
      IL_000c:  ldc.i4.s   10
      IL_000e:  stloc.2
      IL_000f:  newobj     instance void DefaultNamespace.dotailcall::.ctor()
      IL_0014:  stloc.3
      IL_0015:  ldloc.3
      IL_0016:  ldloc.0
      IL_0017:  ldloc.1
      IL_0018:  ldloc.2
      IL_0019:  call       instance int32 DefaultNamespace.dotailcall::foo(int32,
                                                                           float32,
                                                                           wchar)
      IL_001e:  pop
      IL_001f:  ldstr      "How many times foo() was called: "
      IL_0024:  ldsfld    int32 DefaultNamespace.dotailcall::ifoo
      IL_0029:  box        [mscorlib]System.Int32
      IL_002e:  call       class [mscorlib]System.String [mscorlib]System.String::Concat(class [mscorlib]System.Object,
                                                                               class [mscorlib]System.Object)
      IL_0033:  call       void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
      IL_0038:  ldstr      "How many times boo() was called: "
      IL_003d:  ldsfld    int32 DefaultNamespace.dotailcall::iboo
      IL_0042:  box        [mscorlib]System.Int32
      IL_0047:  call       class [mscorlib]System.String [mscorlib]System.String::Concat(class [mscorlib]System.Object,
                                                                               class [mscorlib]System.Object)
      IL_004c:  call       void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
      IL_0051:  ldstr      "How many times goo() was called: "
      IL_0056:  ldsfld    int32 DefaultNamespace.dotailcall::igoo
      IL_005b:  box        [mscorlib]System.Int32
      IL_0060:  call       class [mscorlib]System.String [mscorlib]System.String::Concat(class [mscorlib]System.Object,
                                                                               class [mscorlib]System.Object)
      IL_0065:  call       void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
      IL_006a:  ldstr      "Passed"
      IL_006f:  call       void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
      IL_0074:  ldc.i4    0x64
      IL_0079:  ret
    } // end of method 'dotailcall::Main'

    .method public specialname rtspecialname static void .cctor() il managed
    {
      // Code size       19 (0x13)
      .maxstack  8
      IL_0000:  ldc.i4.0
      IL_0001:  stsfld     int32 DefaultNamespace.dotailcall::ifoo
      IL_0006:  ldc.i4.0
      IL_0007:  stsfld     int32 DefaultNamespace.dotailcall::iboo
      IL_000c:  ldc.i4.0
      IL_000d:  stsfld     int32 DefaultNamespace.dotailcall::igoo
      IL_0012:  ret
    } // end of method 'dotailcall::.cctor'

    .method public specialname rtspecialname instance void .ctor() il managed
    {
      // Code size       7 (0x7)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ret
    } // end of method 'dotailcall::.ctor'

  } // end of class 'dotailcall'

} // end of namespace 'DefaultNamespace'

//*********** DISASSEMBLY COMPLETE ***********************
