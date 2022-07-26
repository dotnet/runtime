// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console { auto }
.assembly extern xunit.core {}
.assembly extern System.Threading.Thread { auto }
// Metadata version: v2.0.50103
.assembly extern mscorlib { auto }

.assembly 'CircularCctorFourThreadsBFI'
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 )
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
// MVID: {DCC29B4C-2098-4C6E-A788-8284AF4F4942}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x03090000


// =============== CLASS MEMBERS DECLARATION ===================

.class public sequential ansi sealed beforefieldinit A
       extends [mscorlib]System.ValueType
{
  .pack 0
  .size 1
  .field public static int32 i
  .method private hidebysig specialname rtspecialname static
          void  .cctor() cil managed
  {
    // Code size       50 (0x32)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldc.i4     0x7d0
    IL_0006:  call       void [System.Threading.Thread]System.Threading.Thread::Sleep(int32)
    IL_000b:  nop
    IL_000c:  ldstr      "In A.cctor: thread {0}: B.i {1}"
    IL_0011:  call       class [System.Threading.Thread]System.Threading.Thread [System.Threading.Thread]System.Threading.Thread::get_CurrentThread()
    IL_0016:  callvirt   instance string [System.Threading.Thread]System.Threading.Thread::get_Name()
    IL_001b:  ldsfld     int32 B::i
    IL_0020:  box        [mscorlib]System.Int32
    IL_0025:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_002a:  nop
    IL_002b:  ldc.i4.5
    IL_002c:  stsfld     int32 A::i
    IL_0031:  ret
  } // end of method A::.cctor

  .method public hidebysig static void  SomeMethod() cil managed noinlining
  {
    // Code size       23 (0x17)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "In MyClass.SomeMethod(): thread {0}"
    IL_0006:  call       class [System.Threading.Thread]System.Threading.Thread [System.Threading.Thread]System.Threading.Thread::get_CurrentThread()
    IL_000b:  callvirt   instance string [System.Threading.Thread]System.Threading.Thread::get_Name()
    IL_0010:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object)
    IL_0015:  nop
    IL_0016:  ret
  } // end of method A::SomeMethod

} // end of class A

.class public auto ansi beforefieldinit B
       extends [mscorlib]System.Object
{
  .field public static int32 i
  .method private hidebysig specialname rtspecialname static
          void  .cctor() cil managed
  {
    // Code size       39 (0x27)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "In B.cctor: thread {0}: C.i {1}"
    IL_0006:  call       class [System.Threading.Thread]System.Threading.Thread [System.Threading.Thread]System.Threading.Thread::get_CurrentThread()
    IL_000b:  callvirt   instance string [System.Threading.Thread]System.Threading.Thread::get_Name()
    IL_0010:  ldsfld     int32 C::i
    IL_0015:  box        [mscorlib]System.Int32
    IL_001a:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_001f:  nop
    IL_0020:  ldc.i4.6
    IL_0021:  stsfld     int32 B::i
    IL_0026:  ret
  } // end of method B::.cctor

  .method public hidebysig static void  SomeMethod() cil managed noinlining
  {
    // Code size       23 (0x17)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "In MyClass.SomeMethod(): thread {0}"
    IL_0006:  call       class [System.Threading.Thread]System.Threading.Thread [System.Threading.Thread]System.Threading.Thread::get_CurrentThread()
    IL_000b:  callvirt   instance string [System.Threading.Thread]System.Threading.Thread::get_Name()
    IL_0010:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object)
    IL_0015:  nop
    IL_0016:  ret
  } // end of method B::SomeMethod

  .method public hidebysig specialname rtspecialname
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method B::.ctor

} // end of class B

.class public sequential ansi sealed beforefieldinit C
       extends [mscorlib]System.ValueType
{
  .pack 0
  .size 1
  .field public static int32 i
  .method private hidebysig specialname rtspecialname static
          void  .cctor() cil managed
  {
    // Code size       50 (0x32)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldc.i4     0x7d0
    IL_0006:  call       void [System.Threading.Thread]System.Threading.Thread::Sleep(int32)
    IL_000b:  nop
    IL_000c:  ldstr      "In C.cctor: thread {0}: D.i {1}"
    IL_0011:  call       class [System.Threading.Thread]System.Threading.Thread [System.Threading.Thread]System.Threading.Thread::get_CurrentThread()
    IL_0016:  callvirt   instance string [System.Threading.Thread]System.Threading.Thread::get_Name()
    IL_001b:  ldsfld     int32 D::i
    IL_0020:  box        [mscorlib]System.Int32
    IL_0025:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_002a:  nop
    IL_002b:  ldc.i4.7
    IL_002c:  stsfld     int32 C::i
    IL_0031:  ret
  } // end of method C::.cctor

  .method public hidebysig static void  SomeMethod() cil managed noinlining
  {
    // Code size       23 (0x17)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "In MyClass.SomeMethod(): thread {0}"
    IL_0006:  call       class [System.Threading.Thread]System.Threading.Thread [System.Threading.Thread]System.Threading.Thread::get_CurrentThread()
    IL_000b:  callvirt   instance string [System.Threading.Thread]System.Threading.Thread::get_Name()
    IL_0010:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object)
    IL_0015:  nop
    IL_0016:  ret
  } // end of method C::SomeMethod

} // end of class C

.class public auto ansi beforefieldinit D
       extends [mscorlib]System.Object
{
  .field public static int32 i
  .method private hidebysig specialname rtspecialname static
          void  .cctor() cil managed
  {
    // Code size       50 (0x32)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldc.i4     0x1388
    IL_0006:  call       void [System.Threading.Thread]System.Threading.Thread::Sleep(int32)
    IL_000b:  nop
    IL_000c:  ldstr      "In D.cctor: thread {0}: E.i {1}"
    IL_0011:  call       class [System.Threading.Thread]System.Threading.Thread [System.Threading.Thread]System.Threading.Thread::get_CurrentThread()
    IL_0016:  callvirt   instance string [System.Threading.Thread]System.Threading.Thread::get_Name()
    IL_001b:  ldsfld     int32 E::i
    IL_0020:  box        [mscorlib]System.Int32
    IL_0025:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_002a:  nop
    IL_002b:  ldc.i4.8
    IL_002c:  stsfld     int32 D::i
    IL_0031:  ret
  } // end of method D::.cctor

  .method public hidebysig static void  SomeMethod() cil managed noinlining
  {
    // Code size       23 (0x17)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "In MyClass.SomeMethod(): thread {0}"
    IL_0006:  call       class [System.Threading.Thread]System.Threading.Thread [System.Threading.Thread]System.Threading.Thread::get_CurrentThread()
    IL_000b:  callvirt   instance string [System.Threading.Thread]System.Threading.Thread::get_Name()
    IL_0010:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object)
    IL_0015:  nop
    IL_0016:  ret
  } // end of method D::SomeMethod

  .method public hidebysig specialname rtspecialname
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method D::.ctor

} // end of class D

.class public sequential ansi sealed beforefieldinit E
       extends [mscorlib]System.ValueType
{
  .pack 0
  .size 1
  .field public static int32 i
  .method private hidebysig specialname rtspecialname static
          void  .cctor() cil managed
  {
    // Code size       40 (0x28)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "In E.cctor: thread {0}: A.i {1}"
    IL_0006:  call       class [System.Threading.Thread]System.Threading.Thread [System.Threading.Thread]System.Threading.Thread::get_CurrentThread()
    IL_000b:  callvirt   instance string [System.Threading.Thread]System.Threading.Thread::get_Name()
    IL_0010:  ldsfld     int32 A::i
    IL_0015:  box        [mscorlib]System.Int32
    IL_001a:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_001f:  nop
    IL_0020:  ldc.i4.s   9
    IL_0022:  stsfld     int32 E::i
    IL_0027:  ret
  } // end of method E::.cctor

  .method public hidebysig static void  SomeMethod() cil managed noinlining
  {
    // Code size       23 (0x17)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "In MyClass.SomeMethod(): thread {0}"
    IL_0006:  call       class [System.Threading.Thread]System.Threading.Thread [System.Threading.Thread]System.Threading.Thread::get_CurrentThread()
    IL_000b:  callvirt   instance string [System.Threading.Thread]System.Threading.Thread::get_Name()
    IL_0010:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object)
    IL_0015:  nop
    IL_0016:  ret
  } // end of method E::SomeMethod

} // end of class E

.class public auto ansi beforefieldinit Test_CircularCctorFourThreadsBFI
       extends [mscorlib]System.Object
{
  .method public hidebysig static void  RunGetA() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  call       void A::SomeMethod()
    IL_0006:  nop
    IL_0007:  ret
  } // end of method Test::RunGetA

  .method public hidebysig static void  RunGetC() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  call       void C::SomeMethod()
    IL_0006:  nop
    IL_0007:  ret
  } // end of method Test::RunGetC

  .method public hidebysig static void  RunGetD() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  call       void D::SomeMethod()
    IL_0006:  nop
    IL_0007:  ret
  } // end of method Test::RunGetD

  .method public hidebysig static void  RunGetE() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  call       void E::SomeMethod()
    IL_0006:  nop
    IL_0007:  ret
  } // end of method Test::RunGetE

  .method public hidebysig static int32  Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       302 (0x12e)
    .maxstack  3
    .locals init (class [System.Threading.Thread]System.Threading.Thread V_0,
             class [System.Threading.Thread]System.Threading.Thread V_1,
             class [System.Threading.Thread]System.Threading.Thread V_2,
             class [System.Threading.Thread]System.Threading.Thread V_3,
             int32 V_4,
             bool V_5)
    IL_0000:  nop
    IL_0001:  ldnull
    IL_0002:  ldftn      void Test_CircularCctorFourThreadsBFI::RunGetA()
    IL_0008:  newobj     instance void [System.Threading.Thread]System.Threading.ThreadStart::.ctor(object,
                                                                                     native int)
    IL_000d:  newobj     instance void [System.Threading.Thread]System.Threading.Thread::.ctor(class [System.Threading.Thread]System.Threading.ThreadStart)
    IL_0012:  stloc.0
    IL_0013:  ldloc.0
    IL_0014:  ldstr      "T1"
    IL_0019:  callvirt   instance void [System.Threading.Thread]System.Threading.Thread::set_Name(string)
    IL_001e:  nop
    IL_001f:  ldnull
    IL_0020:  ldftn      void Test_CircularCctorFourThreadsBFI::RunGetC()
    IL_0026:  newobj     instance void [System.Threading.Thread]System.Threading.ThreadStart::.ctor(object,
                                                                                     native int)
    IL_002b:  newobj     instance void [System.Threading.Thread]System.Threading.Thread::.ctor(class [System.Threading.Thread]System.Threading.ThreadStart)
    IL_0030:  stloc.1
    IL_0031:  ldloc.1
    IL_0032:  ldstr      "T2"
    IL_0037:  callvirt   instance void [System.Threading.Thread]System.Threading.Thread::set_Name(string)
    IL_003c:  nop
    IL_003d:  ldnull
    IL_003e:  ldftn      void Test_CircularCctorFourThreadsBFI::RunGetD()
    IL_0044:  newobj     instance void [System.Threading.Thread]System.Threading.ThreadStart::.ctor(object,
                                                                                     native int)
    IL_0049:  newobj     instance void [System.Threading.Thread]System.Threading.Thread::.ctor(class [System.Threading.Thread]System.Threading.ThreadStart)
    IL_004e:  stloc.2
    IL_004f:  ldloc.2
    IL_0050:  ldstr      "T3"
    IL_0055:  callvirt   instance void [System.Threading.Thread]System.Threading.Thread::set_Name(string)
    IL_005a:  nop
    IL_005b:  ldnull
    IL_005c:  ldftn      void Test_CircularCctorFourThreadsBFI::RunGetE()
    IL_0062:  newobj     instance void [System.Threading.Thread]System.Threading.ThreadStart::.ctor(object,
                                                                                     native int)
    IL_0067:  newobj     instance void [System.Threading.Thread]System.Threading.Thread::.ctor(class [System.Threading.Thread]System.Threading.ThreadStart)
    IL_006c:  stloc.3
    IL_006d:  ldloc.3
    IL_006e:  ldstr      "T4"
    IL_0073:  callvirt   instance void [System.Threading.Thread]System.Threading.Thread::set_Name(string)
    IL_0078:  nop
    IL_0079:  ldloc.0
    IL_007a:  callvirt   instance void [System.Threading.Thread]System.Threading.Thread::Start()
    IL_007f:  nop
    IL_0080:  ldc.i4     0x3e8
    IL_0085:  call       void [System.Threading.Thread]System.Threading.Thread::Sleep(int32)
    IL_008a:  nop
    IL_008b:  ldloc.1
    IL_008c:  callvirt   instance void [System.Threading.Thread]System.Threading.Thread::Start()
    IL_0091:  nop
    IL_0092:  ldc.i4     0x3e8
    IL_0097:  call       void [System.Threading.Thread]System.Threading.Thread::Sleep(int32)
    IL_009c:  nop
    IL_009d:  ldloc.2
    IL_009e:  callvirt   instance void [System.Threading.Thread]System.Threading.Thread::Start()
    IL_00a3:  nop
    IL_00a4:  ldc.i4     0x3e8
    IL_00a9:  call       void [System.Threading.Thread]System.Threading.Thread::Sleep(int32)
    IL_00ae:  nop
    IL_00af:  ldloc.3
    IL_00b0:  callvirt   instance void [System.Threading.Thread]System.Threading.Thread::Start()
    IL_00b5:  nop
    IL_00b6:  ldloc.3
    IL_00b7:  callvirt   instance void [System.Threading.Thread]System.Threading.Thread::Join()
    IL_00bc:  nop
    IL_00bd:  ldloc.2
    IL_00be:  callvirt   instance void [System.Threading.Thread]System.Threading.Thread::Join()
    IL_00c3:  nop
    IL_00c4:  ldloc.1
    IL_00c5:  callvirt   instance void [System.Threading.Thread]System.Threading.Thread::Join()
    IL_00ca:  nop
    IL_00cb:  ldloc.0
    IL_00cc:  callvirt   instance void [System.Threading.Thread]System.Threading.Thread::Join()
    IL_00d1:  nop
    IL_00d2:  ldsfld     int32 A::i
    IL_00d7:  ldc.i4.5
    IL_00d8:  bne.un.s   IL_0100

    IL_00da:  ldsfld     int32 B::i
    IL_00df:  ldc.i4.6
    IL_00e0:  bne.un.s   IL_0100

    IL_00e2:  ldsfld     int32 C::i
    IL_00e7:  ldc.i4.7
    IL_00e8:  bne.un.s   IL_0100

    IL_00ea:  ldsfld     int32 D::i
    IL_00ef:  ldc.i4.8
    IL_00f0:  bne.un.s   IL_0100

    IL_00f2:  ldsfld     int32 E::i
    IL_00f7:  ldc.i4.s   9
    IL_00f9:  ceq
    IL_00fb:  ldc.i4.0
    IL_00fc:  ceq
    IL_00fe:  br.s       IL_0101

    IL_0100:  ldc.i4.1
    IL_0101:  stloc.s    V_5
    IL_0103:  ldloc.s    V_5
    IL_0105:  brtrue.s   IL_0119

    IL_0107:  nop
    IL_0108:  ldstr      "PASS"
    IL_010d:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0112:  nop
    IL_0113:  ldc.i4.s   100
    IL_0115:  stloc.s    V_4
    IL_0117:  br.s       IL_012b

    IL_0119:  nop
    IL_011a:  ldstr      "FAIL"
    IL_011f:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0124:  nop
    IL_0125:  ldc.i4.s   101
    IL_0127:  stloc.s    V_4
    IL_0129:  br.s       IL_012b

    IL_012b:  ldloc.s    V_4
    IL_012d:  ret
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


// =============================================================

// *********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file CircularCctorFourThreads.res
