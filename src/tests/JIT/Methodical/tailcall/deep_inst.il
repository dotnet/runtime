// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ASSEMBLY_NAME { }
.assembly extern xunit.core {}
.namespace JitTest_compat_deep_inst
{
  .class public auto ansi beforefieldinit TestClass
         extends [mscorlib]System.Object
  {
    .field public int32 m_fld
    .method private hidebysig  instance class System.String
            Method0() il managed
    {
      // Code size       39 (0x27)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  dup
      IL_0002:  ldfld      int32 JitTest_compat_deep_inst.TestClass::m_fld
      IL_0007:  ldc.i4     0x2
      IL_000c:  rem
      IL_000d:  brtrue.s   IL_001f

      IL_000f:  call       instance class System.String JitTest_compat_deep_inst.TestClass::Method1()
      IL_0014:  ldstr      " 2"
      IL_0019:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_001e:  ret

      IL_001f:  tail.
      IL_0021:  call       instance class System.String JitTest_compat_deep_inst.TestClass::Method1()
      IL_0026:  ret
    } // end of method TestClass::Method0

    .method private hidebysig  instance class System.String
            Method1() il managed
    {
      // Code size       37 (0x25)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  dup
      IL_0002:  ldfld      int32 JitTest_compat_deep_inst.TestClass::m_fld
      IL_0007:  ldc.i4     0x3
      IL_000c:  rem
      IL_000d:  brtrue.s   IL_001f

      IL_000f:  call       instance class System.String JitTest_compat_deep_inst.TestClass::Method2()
      IL_0014:  ldstr      " 3"
      IL_0019:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_001e:  ret

      IL_001f:  pop
      IL_0020:  jmp        instance class System.String JitTest_compat_deep_inst.TestClass::Method2()
    } // end of method TestClass::Method1

    .method private hidebysig  instance class System.String
            Method2() il managed
    {
      // Code size       39 (0x27)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  dup
      IL_0002:  ldfld      int32 JitTest_compat_deep_inst.TestClass::m_fld
      IL_0007:  ldc.i4     0x4
      IL_000c:  rem
      IL_000d:  brtrue.s   IL_001f

      IL_000f:  call       instance class System.String JitTest_compat_deep_inst.TestClass::Method3()
      IL_0014:  ldstr      " 4"
      IL_0019:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_001e:  ret

      IL_001f:  tail.
      IL_0021:  call       instance class System.String JitTest_compat_deep_inst.TestClass::Method3()
      IL_0026:  ret
    } // end of method TestClass::Method2

    .method private hidebysig  instance class System.String
            Method3() il managed
    {
      // Code size       37 (0x25)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  dup
      IL_0002:  ldfld      int32 JitTest_compat_deep_inst.TestClass::m_fld
      IL_0007:  ldc.i4     0x5
      IL_000c:  rem
      IL_000d:  brtrue.s   IL_001f

      IL_000f:  call       instance class System.String JitTest_compat_deep_inst.TestClass::Method4()
      IL_0014:  ldstr      " 5"
      IL_0019:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_001e:  ret

      IL_001f:  pop
      IL_0020:  jmp        instance class System.String JitTest_compat_deep_inst.TestClass::Method4()
    } // end of method TestClass::Method3

    .method private hidebysig  instance class System.String
            Method4() il managed
    {
      // Code size       39 (0x27)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  dup
      IL_0002:  ldfld      int32 JitTest_compat_deep_inst.TestClass::m_fld
      IL_0007:  ldc.i4     0x6
      IL_000c:  rem
      IL_000d:  brtrue.s   IL_001f

      IL_000f:  call       instance class System.String JitTest_compat_deep_inst.TestClass::Method5()
      IL_0014:  ldstr      " 6"
      IL_0019:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_001e:  ret

      IL_001f:  tail.
      IL_0021:  call       instance class System.String JitTest_compat_deep_inst.TestClass::Method5()
      IL_0026:  ret
    } // end of method TestClass::Method4

    .method private hidebysig  instance class System.String
            Method5() il managed
    {
      // Code size       37 (0x25)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  dup
      IL_0002:  ldfld      int32 JitTest_compat_deep_inst.TestClass::m_fld
      IL_0007:  ldc.i4     0x7
      IL_000c:  rem
      IL_000d:  brtrue.s   IL_001f

      IL_000f:  call       instance class System.String JitTest_compat_deep_inst.TestClass::Method6()
      IL_0014:  ldstr      " 7"
      IL_0019:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_001e:  ret

      IL_001f:  pop
      IL_0020:  jmp        instance class System.String JitTest_compat_deep_inst.TestClass::Method6()
    } // end of method TestClass::Method5

    .method private hidebysig  instance class System.String
            Method6() il managed
    {
      // Code size       39 (0x27)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  dup
      IL_0002:  ldfld      int32 JitTest_compat_deep_inst.TestClass::m_fld
      IL_0007:  ldc.i4     0x8
      IL_000c:  rem
      IL_000d:  brtrue.s   IL_001f

      IL_000f:  call       instance class System.String JitTest_compat_deep_inst.TestClass::Method7()
      IL_0014:  ldstr      " 8"
      IL_0019:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_001e:  ret

      IL_001f:  tail.
      IL_0021:  call       instance class System.String JitTest_compat_deep_inst.TestClass::Method7()
      IL_0026:  ret
    } // end of method TestClass::Method6

    .method private hidebysig  instance class System.String
            Method7() il managed
    {
      // Code size       37 (0x25)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  dup
      IL_0002:  ldfld      int32 JitTest_compat_deep_inst.TestClass::m_fld
      IL_0007:  ldc.i4     0x9
      IL_000c:  rem
      IL_000d:  brtrue.s   IL_001f

      IL_000f:  call       instance class System.String JitTest_compat_deep_inst.TestClass::Method8()
      IL_0014:  ldstr      " 9"
      IL_0019:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_001e:  ret

      IL_001f:  pop
      IL_0020:  jmp        instance class System.String JitTest_compat_deep_inst.TestClass::Method8()
    } // end of method TestClass::Method7

    .method private hidebysig  instance class System.String
            Method8() il managed
    {
      // Code size       39 (0x27)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  dup
      IL_0002:  ldfld      int32 JitTest_compat_deep_inst.TestClass::m_fld
      IL_0007:  ldc.i4     0xa
      IL_000c:  rem
      IL_000d:  brtrue.s   IL_001f

      IL_000f:  call       instance class System.String JitTest_compat_deep_inst.TestClass::Method9()
      IL_0014:  ldstr      " 10"
      IL_0019:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_001e:  ret

      IL_001f:  tail.
      IL_0021:  call       instance class System.String JitTest_compat_deep_inst.TestClass::Method9()
      IL_0026:  ret
    } // end of method TestClass::Method8

    .method private hidebysig  instance class System.String
            Method9() il managed
    {
      // Code size       37 (0x25)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  dup
      IL_0002:  ldfld      int32 JitTest_compat_deep_inst.TestClass::m_fld
      IL_0007:  ldc.i4     0xb
      IL_000c:  rem
      IL_000d:  brtrue.s   IL_001f

      IL_000f:  call       instance class System.String JitTest_compat_deep_inst.TestClass::Method10()
      IL_0014:  ldstr      " 11"
      IL_0019:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_001e:  ret

      IL_001f:  pop
      IL_0020:  jmp        instance class System.String JitTest_compat_deep_inst.TestClass::Method10()
    } // end of method TestClass::Method9

    .method private hidebysig  instance class System.String
            Method10() il managed
    {
      // Code size       39 (0x27)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  dup
      IL_0002:  ldfld      int32 JitTest_compat_deep_inst.TestClass::m_fld
      IL_0007:  ldc.i4     0xc
      IL_000c:  rem
      IL_000d:  brtrue.s   IL_001f

      IL_000f:  call       instance class System.String JitTest_compat_deep_inst.TestClass::Method11()
      IL_0014:  ldstr      " 12"
      IL_0019:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_001e:  ret

      IL_001f:  tail.
      IL_0021:  call       instance class System.String JitTest_compat_deep_inst.TestClass::Method11()
      IL_0026:  ret
    } // end of method TestClass::Method10

    .method private hidebysig  instance class System.String
            Method11() il managed
    {
      // Code size       37 (0x25)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  dup
      IL_0002:  ldfld      int32 JitTest_compat_deep_inst.TestClass::m_fld
      IL_0007:  ldc.i4     0xd
      IL_000c:  rem
      IL_000d:  brtrue.s   IL_001f

      IL_000f:  call       instance class System.String JitTest_compat_deep_inst.TestClass::Method12()
      IL_0014:  ldstr      " 13"
      IL_0019:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_001e:  ret

      IL_001f:  pop
      IL_0020:  jmp        instance class System.String JitTest_compat_deep_inst.TestClass::Method12()
    } // end of method TestClass::Method11

    .method private hidebysig  instance class System.String
            Method12() il managed
    {
      // Code size       39 (0x27)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  dup
      IL_0002:  ldfld      int32 JitTest_compat_deep_inst.TestClass::m_fld
      IL_0007:  ldc.i4     0xe
      IL_000c:  rem
      IL_000d:  brtrue.s   IL_001f

      IL_000f:  call       instance class System.String JitTest_compat_deep_inst.TestClass::Method13()
      IL_0014:  ldstr      " 14"
      IL_0019:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_001e:  ret

      IL_001f:  tail.
      IL_0021:  call       instance class System.String JitTest_compat_deep_inst.TestClass::Method13()
      IL_0026:  ret
    } // end of method TestClass::Method12

    .method private hidebysig  instance class System.String
            Method13() il managed
    {
      // Code size       37 (0x25)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  dup
      IL_0002:  ldfld      int32 JitTest_compat_deep_inst.TestClass::m_fld
      IL_0007:  ldc.i4     0xf
      IL_000c:  rem
      IL_000d:  brtrue.s   IL_001f

      IL_000f:  call       instance class System.String JitTest_compat_deep_inst.TestClass::Method14()
      IL_0014:  ldstr      " 15"
      IL_0019:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_001e:  ret

      IL_001f:  pop
      IL_0020:  jmp        instance class System.String JitTest_compat_deep_inst.TestClass::Method14()
    } // end of method TestClass::Method13

    .method private hidebysig  instance class System.String
            Method14() il managed
    {
      // Code size       39 (0x27)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  dup
      IL_0002:  ldfld      int32 JitTest_compat_deep_inst.TestClass::m_fld
      IL_0007:  ldc.i4     0x10
      IL_000c:  rem
      IL_000d:  brtrue.s   IL_001f

      IL_000f:  call       instance class System.String JitTest_compat_deep_inst.TestClass::Method15()
      IL_0014:  ldstr      " 16"
      IL_0019:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_001e:  ret

      IL_001f:  tail.
      IL_0021:  call       instance class System.String JitTest_compat_deep_inst.TestClass::Method15()
      IL_0026:  ret
    } // end of method TestClass::Method14

    .method private hidebysig  instance class System.String
            Method15() il managed
    {
      // Code size       37 (0x25)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  dup
      IL_0002:  ldfld      int32 JitTest_compat_deep_inst.TestClass::m_fld
      IL_0007:  ldc.i4     0x11
      IL_000c:  rem
      IL_000d:  brtrue.s   IL_001f

      IL_000f:  call       instance class System.String JitTest_compat_deep_inst.TestClass::Method16()
      IL_0014:  ldstr      " 17"
      IL_0019:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_001e:  ret

      IL_001f:  pop
      IL_0020:  jmp        instance class System.String JitTest_compat_deep_inst.TestClass::Method16()
    } // end of method TestClass::Method15

    .method private hidebysig  instance class System.String
            Method16() il managed
    {
      // Code size       39 (0x27)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  dup
      IL_0002:  ldfld      int32 JitTest_compat_deep_inst.TestClass::m_fld
      IL_0007:  ldc.i4     0x12
      IL_000c:  rem
      IL_000d:  brtrue.s   IL_001f

      IL_000f:  call       instance class System.String JitTest_compat_deep_inst.TestClass::Method17()
      IL_0014:  ldstr      " 18"
      IL_0019:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_001e:  ret

      IL_001f:  tail.
      IL_0021:  call       instance class System.String JitTest_compat_deep_inst.TestClass::Method17()
      IL_0026:  ret
    } // end of method TestClass::Method16

    .method private hidebysig  instance class System.String
            Method17() il managed
    {
      // Code size       37 (0x25)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  dup
      IL_0002:  ldfld      int32 JitTest_compat_deep_inst.TestClass::m_fld
      IL_0007:  ldc.i4     0x13
      IL_000c:  rem
      IL_000d:  brtrue.s   IL_001f

      IL_000f:  call       instance class System.String JitTest_compat_deep_inst.TestClass::Method18()
      IL_0014:  ldstr      " 19"
      IL_0019:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_001e:  ret

      IL_001f:  pop
      IL_0020:  jmp        instance class System.String JitTest_compat_deep_inst.TestClass::Method18()
    } // end of method TestClass::Method17

    .method private hidebysig  instance class System.String
            Method18() il managed
    {
      // Code size       39 (0x27)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  dup
      IL_0002:  ldfld      int32 JitTest_compat_deep_inst.TestClass::m_fld
      IL_0007:  ldc.i4     0x14
      IL_000c:  rem
      IL_000d:  brtrue.s   IL_001f

      IL_000f:  call       instance class System.String JitTest_compat_deep_inst.TestClass::Method19()
      IL_0014:  ldstr      " 20"
      IL_0019:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_001e:  ret

      IL_001f:  tail.
      IL_0021:  call       instance class System.String JitTest_compat_deep_inst.TestClass::Method19()
      IL_0026:  ret
    } // end of method TestClass::Method18

    .method private hidebysig  instance class System.String
            Method19() il managed
    {
      // Code size       37 (0x25)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  dup
      IL_0002:  ldfld      int32 JitTest_compat_deep_inst.TestClass::m_fld
      IL_0007:  ldc.i4     0x15
      IL_000c:  rem
      IL_000d:  brtrue.s   IL_001f

      IL_000f:  call       instance class System.String JitTest_compat_deep_inst.TestClass::Method20()
      IL_0014:  ldstr      " 21"
      IL_0019:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_001e:  ret

      IL_001f:  pop
      IL_0020:  jmp        instance class System.String JitTest_compat_deep_inst.TestClass::Method20()
    } // end of method TestClass::Method19

    .method private hidebysig  instance class System.String
            Method20() il managed
    {
      // Code size       39 (0x27)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  dup
      IL_0002:  ldfld      int32 JitTest_compat_deep_inst.TestClass::m_fld
      IL_0007:  ldc.i4     0x16
      IL_000c:  rem
      IL_000d:  brtrue.s   IL_001f

      IL_000f:  call       instance class System.String JitTest_compat_deep_inst.TestClass::Method21()
      IL_0014:  ldstr      " 22"
      IL_0019:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_001e:  ret

      IL_001f:  tail.
      IL_0021:  call       instance class System.String JitTest_compat_deep_inst.TestClass::Method21()
      IL_0026:  ret
    } // end of method TestClass::Method20

    .method private hidebysig  instance class System.String
            Method21() il managed
    {
      // Code size       37 (0x25)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  dup
      IL_0002:  ldfld      int32 JitTest_compat_deep_inst.TestClass::m_fld
      IL_0007:  ldc.i4     0x17
      IL_000c:  rem
      IL_000d:  brtrue.s   IL_001f

      IL_000f:  call       instance class System.String JitTest_compat_deep_inst.TestClass::Method22()
      IL_0014:  ldstr      " 23"
      IL_0019:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_001e:  ret

      IL_001f:  pop
      IL_0020:  jmp        instance class System.String JitTest_compat_deep_inst.TestClass::Method22()
    } // end of method TestClass::Method21

    .method private hidebysig  instance class System.String
            Method22() il managed
    {
      // Code size       39 (0x27)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  dup
      IL_0002:  ldfld      int32 JitTest_compat_deep_inst.TestClass::m_fld
      IL_0007:  ldc.i4     0x18
      IL_000c:  rem
      IL_000d:  brtrue.s   IL_001f

      IL_000f:  call       instance class System.String JitTest_compat_deep_inst.TestClass::Method23()
      IL_0014:  ldstr      " 24"
      IL_0019:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_001e:  ret

      IL_001f:  tail.
      IL_0021:  call       instance class System.String JitTest_compat_deep_inst.TestClass::Method23()
      IL_0026:  ret
    } // end of method TestClass::Method22

    .method private hidebysig  instance class System.String
            Method23() il managed
    {
      // Code size       37 (0x25)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  dup
      IL_0002:  ldfld      int32 JitTest_compat_deep_inst.TestClass::m_fld
      IL_0007:  ldc.i4     0x19
      IL_000c:  rem
      IL_000d:  brtrue.s   IL_001f

      IL_000f:  call       instance class System.String JitTest_compat_deep_inst.TestClass::Method24()
      IL_0014:  ldstr      " 25"
      IL_0019:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_001e:  ret

      IL_001f:  pop
      IL_0020:  jmp        instance class System.String JitTest_compat_deep_inst.TestClass::Method24()
    } // end of method TestClass::Method23

    .method private hidebysig  instance class System.String
            Method24() il managed
    {
      // Code size       39 (0x27)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  dup
      IL_0002:  ldfld      int32 JitTest_compat_deep_inst.TestClass::m_fld
      IL_0007:  ldc.i4     0x1a
      IL_000c:  rem
      IL_000d:  brtrue.s   IL_001f

      IL_000f:  call       instance class System.String JitTest_compat_deep_inst.TestClass::Method25()
      IL_0014:  ldstr      " 26"
      IL_0019:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_001e:  ret

      IL_001f:  tail.
      IL_0021:  call       instance class System.String JitTest_compat_deep_inst.TestClass::Method25()
      IL_0026:  ret
    } // end of method TestClass::Method24

    .method private hidebysig  instance class System.String
            Method25() il managed
    {
      // Code size       37 (0x25)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  dup
      IL_0002:  ldfld      int32 JitTest_compat_deep_inst.TestClass::m_fld
      IL_0007:  ldc.i4     0x1b
      IL_000c:  rem
      IL_000d:  brtrue.s   IL_001f

      IL_000f:  call       instance class System.String JitTest_compat_deep_inst.TestClass::Method26()
      IL_0014:  ldstr      " 27"
      IL_0019:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_001e:  ret

      IL_001f:  pop
      IL_0020:  jmp        instance class System.String JitTest_compat_deep_inst.TestClass::Method26()
    } // end of method TestClass::Method25

    .method private hidebysig  instance class System.String
            Method26() il managed
    {
      // Code size       39 (0x27)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  dup
      IL_0002:  ldfld      int32 JitTest_compat_deep_inst.TestClass::m_fld
      IL_0007:  ldc.i4     0x1c
      IL_000c:  rem
      IL_000d:  brtrue.s   IL_001f

      IL_000f:  call       instance class System.String JitTest_compat_deep_inst.TestClass::Method27()
      IL_0014:  ldstr      " 28"
      IL_0019:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_001e:  ret

      IL_001f:  tail.
      IL_0021:  call       instance class System.String JitTest_compat_deep_inst.TestClass::Method27()
      IL_0026:  ret
    } // end of method TestClass::Method26

    .method private hidebysig  instance class System.String
            Method27() il managed
    {
      // Code size       37 (0x25)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  dup
      IL_0002:  ldfld      int32 JitTest_compat_deep_inst.TestClass::m_fld
      IL_0007:  ldc.i4     0x1d
      IL_000c:  rem
      IL_000d:  brtrue.s   IL_001f

      IL_000f:  call       instance class System.String JitTest_compat_deep_inst.TestClass::Method28()
      IL_0014:  ldstr      " 29"
      IL_0019:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_001e:  ret

      IL_001f:  pop
      IL_0020:  jmp        instance class System.String JitTest_compat_deep_inst.TestClass::Method28()
    } // end of method TestClass::Method27

    .method private hidebysig  instance class System.String
            Method28() il managed
    {
      // Code size       39 (0x27)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  dup
      IL_0002:  ldfld      int32 JitTest_compat_deep_inst.TestClass::m_fld
      IL_0007:  ldc.i4     0x1e
      IL_000c:  rem
      IL_000d:  brtrue.s   IL_001f

      IL_000f:  call       instance class System.String JitTest_compat_deep_inst.TestClass::Method29()
      IL_0014:  ldstr      " 30"
      IL_0019:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_001e:  ret

      IL_001f:  tail.
      IL_0021:  call       instance class System.String JitTest_compat_deep_inst.TestClass::Method29()
      IL_0026:  ret
    } // end of method TestClass::Method28

    .method private hidebysig  instance class System.String
            Method29() il managed
    {
      // Code size       37 (0x25)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  dup
      IL_0002:  ldfld      int32 JitTest_compat_deep_inst.TestClass::m_fld
      IL_0007:  ldc.i4     0x1f
      IL_000c:  rem
      IL_000d:  brtrue.s   IL_001f

      IL_000f:  call       instance class System.String JitTest_compat_deep_inst.TestClass::Method30()
      IL_0014:  ldstr      " 31"
      IL_0019:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_001e:  ret

      IL_001f:  pop
      IL_0020:  jmp        instance class System.String JitTest_compat_deep_inst.TestClass::Method30()
    } // end of method TestClass::Method29

    .method private hidebysig  instance class System.String
            Method30() il managed
    {
      // Code size       39 (0x27)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  dup
      IL_0002:  ldfld      int32 JitTest_compat_deep_inst.TestClass::m_fld
      IL_0007:  ldc.i4     0x20
      IL_000c:  rem
      IL_000d:  brtrue.s   IL_001f

      IL_000f:  call       instance class System.String JitTest_compat_deep_inst.TestClass::Method31()
      IL_0014:  ldstr      " 32"
      IL_0019:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_001e:  ret

      IL_001f:  tail.
      IL_0021:  call       instance class System.String JitTest_compat_deep_inst.TestClass::Method31()
      IL_0026:  ret
    } // end of method TestClass::Method30

    .method private hidebysig  instance class System.String
            Method31() il managed
    {
      // Code size       37 (0x25)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  dup
      IL_0002:  ldfld      int32 JitTest_compat_deep_inst.TestClass::m_fld
      IL_0007:  ldc.i4     0x21
      IL_000c:  rem
      IL_000d:  brtrue.s   IL_001f

      IL_000f:  call       instance class System.String JitTest_compat_deep_inst.TestClass::Method32()
      IL_0014:  ldstr      " 33"
      IL_0019:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_001e:  ret

      IL_001f:  pop
      IL_0020:  jmp        instance class System.String JitTest_compat_deep_inst.TestClass::Method32()
    } // end of method TestClass::Method31

    .method private hidebysig  instance class System.String
            Method32() il managed
    {
      // Code size       39 (0x27)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  dup
      IL_0002:  ldfld      int32 JitTest_compat_deep_inst.TestClass::m_fld
      IL_0007:  ldc.i4     0x22
      IL_000c:  rem
      IL_000d:  brtrue.s   IL_001f

      IL_000f:  call       instance class System.String JitTest_compat_deep_inst.TestClass::Method33()
      IL_0014:  ldstr      " 34"
      IL_0019:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_001e:  ret

      IL_001f:  tail.
      IL_0021:  call       instance class System.String JitTest_compat_deep_inst.TestClass::Method33()
      IL_0026:  ret
    } // end of method TestClass::Method32

    .method private hidebysig  instance class System.String
            Method33() il managed
    {
      // Code size       37 (0x25)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  dup
      IL_0002:  ldfld      int32 JitTest_compat_deep_inst.TestClass::m_fld
      IL_0007:  ldc.i4     0x23
      IL_000c:  rem
      IL_000d:  brtrue.s   IL_001f

      IL_000f:  call       instance class System.String JitTest_compat_deep_inst.TestClass::Method34()
      IL_0014:  ldstr      " 35"
      IL_0019:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_001e:  ret

      IL_001f:  pop
      IL_0020:  jmp        instance class System.String JitTest_compat_deep_inst.TestClass::Method34()
    } // end of method TestClass::Method33

    .method private hidebysig  instance class System.String
            Method34() il managed
    {
      // Code size       39 (0x27)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  dup
      IL_0002:  ldfld      int32 JitTest_compat_deep_inst.TestClass::m_fld
      IL_0007:  ldc.i4     0x24
      IL_000c:  rem
      IL_000d:  brtrue.s   IL_001f

      IL_000f:  call       instance class System.String JitTest_compat_deep_inst.TestClass::Method35()
      IL_0014:  ldstr      " 36"
      IL_0019:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_001e:  ret

      IL_001f:  tail.
      IL_0021:  call       instance class System.String JitTest_compat_deep_inst.TestClass::Method35()
      IL_0026:  ret
    } // end of method TestClass::Method34

    .method private hidebysig  instance class System.String
            Method35() il managed
    {
      // Code size       37 (0x25)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  dup
      IL_0002:  ldfld      int32 JitTest_compat_deep_inst.TestClass::m_fld
      IL_0007:  ldc.i4     0x25
      IL_000c:  rem
      IL_000d:  brtrue.s   IL_001f

      IL_000f:  call       instance class System.String JitTest_compat_deep_inst.TestClass::Method36()
      IL_0014:  ldstr      " 37"
      IL_0019:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_001e:  ret

      IL_001f:  pop
      IL_0020:  jmp        instance class System.String JitTest_compat_deep_inst.TestClass::Method36()
    } // end of method TestClass::Method35

    .method private hidebysig  instance class System.String
            Method36() il managed
    {
      // Code size       39 (0x27)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  dup
      IL_0002:  ldfld      int32 JitTest_compat_deep_inst.TestClass::m_fld
      IL_0007:  ldc.i4     0x26
      IL_000c:  rem
      IL_000d:  brtrue.s   IL_001f

      IL_000f:  call       instance class System.String JitTest_compat_deep_inst.TestClass::Method37()
      IL_0014:  ldstr      " 38"
      IL_0019:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_001e:  ret

      IL_001f:  tail.
      IL_0021:  call       instance class System.String JitTest_compat_deep_inst.TestClass::Method37()
      IL_0026:  ret
    } // end of method TestClass::Method36

    .method private hidebysig  instance class System.String
            Method37() il managed
    {
      // Code size       37 (0x25)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  dup
      IL_0002:  ldfld      int32 JitTest_compat_deep_inst.TestClass::m_fld
      IL_0007:  ldc.i4     0x27
      IL_000c:  rem
      IL_000d:  brtrue.s   IL_001f

      IL_000f:  call       instance class System.String JitTest_compat_deep_inst.TestClass::Method38()
      IL_0014:  ldstr      " 39"
      IL_0019:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_001e:  ret

      IL_001f:  pop
      IL_0020:  jmp        instance class System.String JitTest_compat_deep_inst.TestClass::Method38()
    } // end of method TestClass::Method37

    .method private hidebysig  instance class System.String
            Method38() il managed
    {
      // Code size       39 (0x27)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  dup
      IL_0002:  ldfld      int32 JitTest_compat_deep_inst.TestClass::m_fld
      IL_0007:  ldc.i4     0x28
      IL_000c:  rem
      IL_000d:  brtrue.s   IL_001f

      IL_000f:  call       instance class System.String JitTest_compat_deep_inst.TestClass::Method39()
      IL_0014:  ldstr      " 40"
      IL_0019:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_001e:  ret

      IL_001f:  tail.
      IL_0021:  call       instance class System.String JitTest_compat_deep_inst.TestClass::Method39()
      IL_0026:  ret
    } // end of method TestClass::Method38

    .method private hidebysig  instance class System.String
            Method39() il managed
    {
      // Code size       37 (0x25)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  dup
      IL_0002:  ldfld      int32 JitTest_compat_deep_inst.TestClass::m_fld
      IL_0007:  ldc.i4     0x29
      IL_000c:  rem
      IL_000d:  brtrue.s   IL_001f

      IL_000f:  call       instance class System.String JitTest_compat_deep_inst.TestClass::Method40()
      IL_0014:  ldstr      " 41"
      IL_0019:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_001e:  ret

      IL_001f:  pop
      IL_0020:  jmp        instance class System.String JitTest_compat_deep_inst.TestClass::Method40()
    } // end of method TestClass::Method39

    .method private hidebysig  instance class System.String
            Method40() il managed
    {
      // Code size       39 (0x27)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  dup
      IL_0002:  ldfld      int32 JitTest_compat_deep_inst.TestClass::m_fld
      IL_0007:  ldc.i4     0x2a
      IL_000c:  rem
      IL_000d:  brtrue.s   IL_001f

      IL_000f:  call       instance class System.String JitTest_compat_deep_inst.TestClass::Method41()
      IL_0014:  ldstr      " 42"
      IL_0019:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_001e:  ret

      IL_001f:  tail.
      IL_0021:  call       instance class System.String JitTest_compat_deep_inst.TestClass::Method41()
      IL_0026:  ret
    } // end of method TestClass::Method40

    .method private hidebysig  instance class System.String
            Method41() il managed
    {
      // Code size       37 (0x25)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  dup
      IL_0002:  ldfld      int32 JitTest_compat_deep_inst.TestClass::m_fld
      IL_0007:  ldc.i4     0x2b
      IL_000c:  rem
      IL_000d:  brtrue.s   IL_001f

      IL_000f:  call       instance class System.String JitTest_compat_deep_inst.TestClass::Method42()
      IL_0014:  ldstr      " 43"
      IL_0019:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_001e:  ret

      IL_001f:  pop
      IL_0020:  jmp        instance class System.String JitTest_compat_deep_inst.TestClass::Method42()
    } // end of method TestClass::Method41

    .method private hidebysig  instance class System.String
            Method42() il managed
    {
      // Code size       39 (0x27)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  dup
      IL_0002:  ldfld      int32 JitTest_compat_deep_inst.TestClass::m_fld
      IL_0007:  ldc.i4     0x2c
      IL_000c:  rem
      IL_000d:  brtrue.s   IL_001f

      IL_000f:  call       instance class System.String JitTest_compat_deep_inst.TestClass::Method43()
      IL_0014:  ldstr      " 44"
      IL_0019:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_001e:  ret

      IL_001f:  tail.
      IL_0021:  call       instance class System.String JitTest_compat_deep_inst.TestClass::Method43()
      IL_0026:  ret
    } // end of method TestClass::Method42

    .method private hidebysig  instance class System.String
            Method43() il managed
    {
      // Code size       37 (0x25)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  dup
      IL_0002:  ldfld      int32 JitTest_compat_deep_inst.TestClass::m_fld
      IL_0007:  ldc.i4     0x2d
      IL_000c:  rem
      IL_000d:  brtrue.s   IL_001f

      IL_000f:  call       instance class System.String JitTest_compat_deep_inst.TestClass::Method44()
      IL_0014:  ldstr      " 45"
      IL_0019:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_001e:  ret

      IL_001f:  pop
      IL_0020:  jmp        instance class System.String JitTest_compat_deep_inst.TestClass::Method44()
    } // end of method TestClass::Method43

    .method private hidebysig  instance class System.String
            Method44() il managed
    {
      // Code size       39 (0x27)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  dup
      IL_0002:  ldfld      int32 JitTest_compat_deep_inst.TestClass::m_fld
      IL_0007:  ldc.i4     0x2e
      IL_000c:  rem
      IL_000d:  brtrue.s   IL_001f

      IL_000f:  call       instance class System.String JitTest_compat_deep_inst.TestClass::Method45()
      IL_0014:  ldstr      " 46"
      IL_0019:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_001e:  ret

      IL_001f:  tail.
      IL_0021:  call       instance class System.String JitTest_compat_deep_inst.TestClass::Method45()
      IL_0026:  ret
    } // end of method TestClass::Method44

    .method private hidebysig  instance class System.String
            Method45() il managed
    {
      // Code size       37 (0x25)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  dup
      IL_0002:  ldfld      int32 JitTest_compat_deep_inst.TestClass::m_fld
      IL_0007:  ldc.i4     0x2f
      IL_000c:  rem
      IL_000d:  brtrue.s   IL_001f

      IL_000f:  call       instance class System.String JitTest_compat_deep_inst.TestClass::Method46()
      IL_0014:  ldstr      " 47"
      IL_0019:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_001e:  ret

      IL_001f:  pop
      IL_0020:  jmp        instance class System.String JitTest_compat_deep_inst.TestClass::Method46()
    } // end of method TestClass::Method45

    .method private hidebysig  instance class System.String
            Method46() il managed
    {
      // Code size       39 (0x27)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  dup
      IL_0002:  ldfld      int32 JitTest_compat_deep_inst.TestClass::m_fld
      IL_0007:  ldc.i4     0x30
      IL_000c:  rem
      IL_000d:  brtrue.s   IL_001f

      IL_000f:  call       instance class System.String JitTest_compat_deep_inst.TestClass::Method47()
      IL_0014:  ldstr      " 48"
      IL_0019:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_001e:  ret

      IL_001f:  tail.
      IL_0021:  call       instance class System.String JitTest_compat_deep_inst.TestClass::Method47()
      IL_0026:  ret
    } // end of method TestClass::Method46

    .method private hidebysig  instance class System.String
            Method47() il managed
    {
      // Code size       37 (0x25)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  dup
      IL_0002:  ldfld      int32 JitTest_compat_deep_inst.TestClass::m_fld
      IL_0007:  ldc.i4     0x31
      IL_000c:  rem
      IL_000d:  brtrue.s   IL_001f

      IL_000f:  call       instance class System.String JitTest_compat_deep_inst.TestClass::Method48()
      IL_0014:  ldstr      " 49"
      IL_0019:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_001e:  ret

      IL_001f:  pop
      IL_0020:  jmp        instance class System.String JitTest_compat_deep_inst.TestClass::Method48()
    } // end of method TestClass::Method47

    .method private hidebysig  instance class System.String
            Method48() il managed
    {
      // Code size       39 (0x27)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  dup
      IL_0002:  ldfld      int32 JitTest_compat_deep_inst.TestClass::m_fld
      IL_0007:  ldc.i4     0x32
      IL_000c:  rem
      IL_000d:  brtrue.s   IL_001f

      IL_000f:  call       instance class System.String JitTest_compat_deep_inst.TestClass::Method49()
      IL_0014:  ldstr      " 50"
      IL_0019:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_001e:  ret

      IL_001f:  tail.
      IL_0021:  call       instance class System.String JitTest_compat_deep_inst.TestClass::Method49()
      IL_0026:  ret
    } // end of method TestClass::Method48

    .method private hidebysig  instance class System.String
            Method49() il managed
    {
      // Code size       37 (0x25)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  dup
      IL_0002:  ldfld      int32 JitTest_compat_deep_inst.TestClass::m_fld
      IL_0007:  ldc.i4     0x33
      IL_000c:  rem
      IL_000d:  brtrue.s   IL_001f

      IL_000f:  call       instance class System.String JitTest_compat_deep_inst.TestClass::Method50()
      IL_0014:  ldstr      " 51"
      IL_0019:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_001e:  ret

      IL_001f:  pop
      IL_0020:  jmp        instance class System.String JitTest_compat_deep_inst.TestClass::Method50()
    } // end of method TestClass::Method49

    .method private hidebysig  instance class System.String
            Method50() il managed
    {
      // Code size       2 (0x2)
      .maxstack  8
      IL_0000:  ldnull
      IL_0001:  ret
    } // end of method TestClass::Method50

    .method public hidebysig  static 
            int32 Main() il managed
    {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint
      // Code size       78 (0x4e)
      .maxstack  8
      .locals (class JitTest_compat_deep_inst.TestClass V_0)
      IL_0000:  ldc.i4.2
      IL_0001:  newobj     instance void JitTest_compat_deep_inst.TestClass::.ctor(int32)
      IL_0006:  stloc.0
      IL_0007:  ldloc.0
      IL_0008:  call       instance class System.String JitTest_compat_deep_inst.TestClass::Method0()
      IL_000d:  brtrue     IL_0027

      IL_0012:  ldloc.0
      IL_0013:  ldfld      int32 JitTest_compat_deep_inst.TestClass::m_fld
      IL_0018:  call       void [System.Console]System.Console::Write(int32)
      IL_001d:  ldstr      " - prime!"
      IL_0022:  call       void [System.Console]System.Console::WriteLine(class System.String)
      IL_0027:  ldloc.0
      IL_0028:  dup
      IL_0029:  ldfld      int32 JitTest_compat_deep_inst.TestClass::m_fld
      IL_002e:  ldc.i4.1
      IL_002f:  add
      IL_0030:  stfld      int32 JitTest_compat_deep_inst.TestClass::m_fld
      IL_0035:  ldloc.0
      IL_0036:  ldfld      int32 JitTest_compat_deep_inst.TestClass::m_fld
      IL_003b:  ldc.i4     0x64
      IL_0040:  ceq
      IL_0042:  brfalse    IL_0007

      IL_0047:  ldloc.0
      IL_0048:  ldfld      int32 JitTest_compat_deep_inst.TestClass::m_fld
      IL_004d:  ret
    } // end of method TestClass::Main

    .method public hidebysig  specialname rtspecialname 
            instance void  .ctor(int32 A_0) il managed
    {
      // Code size       14 (0xe)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ldarg.0
      IL_0007:  ldarg.1
      IL_0008:  stfld      int32 JitTest_compat_deep_inst.TestClass::m_fld
      IL_000d:  ret
    } // end of method TestClass::.ctor

  } // end of class TestClass

} // end of namespace JitTest_compat_deep_inst

//*********** DISASSEMBLY COMPLETE ***********************
