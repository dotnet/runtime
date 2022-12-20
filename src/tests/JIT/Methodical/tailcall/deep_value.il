// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly deep_value { }
.assembly extern xunit.core {}
.namespace JitTest_compat_deep_value
{
  .class public auto ansi sealed ValueClass
         extends [mscorlib]System.ValueType
  {
    .field public int32 m_fld
  } // end of class ValueClass

  .class public auto ansi beforefieldinit TestClass
         extends [mscorlib]System.Object
  {
    .method private hidebysig static 
            class System.String  Method0(value class JitTest_compat_deep_value.ValueClass A_0) il managed
    {
      // Code size       52 (0x34)
      .maxstack  8
      IL_0000:  ldarga     A_0
      IL_0004:  dup
      IL_0005:  ldfld      int32 JitTest_compat_deep_value.ValueClass::m_fld
      IL_000a:  ldc.i4     0x2
      IL_000f:  rem
      IL_0010:  brtrue.s   IL_0027

      IL_0012:  ldobj      JitTest_compat_deep_value.ValueClass
      IL_0017:  call       class System.String JitTest_compat_deep_value.TestClass::Method1(value class JitTest_compat_deep_value.ValueClass)
      IL_001c:  ldstr      " 2"
      IL_0021:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_0026:  ret

      IL_0027:  ldobj      JitTest_compat_deep_value.ValueClass
      IL_002c:  tail.
      IL_002e:  call       class System.String JitTest_compat_deep_value.TestClass::Method1(value class JitTest_compat_deep_value.ValueClass)
      IL_0033:  ret
    } // end of method TestClass::Method0

    .method private hidebysig static 
            class System.String  Method1(value class JitTest_compat_deep_value.ValueClass A_0) il managed
    {
      // Code size       45 (0x2d)
      .maxstack  8
      IL_0000:  ldarga     A_0
      IL_0004:  dup
      IL_0005:  ldfld      int32 JitTest_compat_deep_value.ValueClass::m_fld
      IL_000a:  ldc.i4     0x3
      IL_000f:  rem
      IL_0010:  brtrue.s   IL_0027

      IL_0012:  ldobj      JitTest_compat_deep_value.ValueClass
      IL_0017:  call       class System.String JitTest_compat_deep_value.TestClass::Method2(value class JitTest_compat_deep_value.ValueClass)
      IL_001c:  ldstr      " 3"
      IL_0021:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_0026:  ret

      IL_0027:  pop
      IL_0028:  jmp        class System.String JitTest_compat_deep_value.TestClass::Method2(value class JitTest_compat_deep_value.ValueClass)
    } // end of method TestClass::Method1

    .method private hidebysig static 
            class System.String  Method2(value class JitTest_compat_deep_value.ValueClass A_0) il managed
    {
      // Code size       52 (0x34)
      .maxstack  8
      IL_0000:  ldarga     A_0
      IL_0004:  dup
      IL_0005:  ldfld      int32 JitTest_compat_deep_value.ValueClass::m_fld
      IL_000a:  ldc.i4     0x4
      IL_000f:  rem
      IL_0010:  brtrue.s   IL_0027

      IL_0012:  ldobj      JitTest_compat_deep_value.ValueClass
      IL_0017:  call       class System.String JitTest_compat_deep_value.TestClass::Method3(value class JitTest_compat_deep_value.ValueClass)
      IL_001c:  ldstr      " 4"
      IL_0021:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_0026:  ret

      IL_0027:  ldobj      JitTest_compat_deep_value.ValueClass
      IL_002c:  tail.
      IL_002e:  call       class System.String JitTest_compat_deep_value.TestClass::Method3(value class JitTest_compat_deep_value.ValueClass)
      IL_0033:  ret
    } // end of method TestClass::Method2

    .method private hidebysig static 
            class System.String  Method3(value class JitTest_compat_deep_value.ValueClass A_0) il managed
    {
      // Code size       45 (0x2d)
      .maxstack  8
      IL_0000:  ldarga     A_0
      IL_0004:  dup
      IL_0005:  ldfld      int32 JitTest_compat_deep_value.ValueClass::m_fld
      IL_000a:  ldc.i4     0x5
      IL_000f:  rem
      IL_0010:  brtrue.s   IL_0027

      IL_0012:  ldobj      JitTest_compat_deep_value.ValueClass
      IL_0017:  call       class System.String JitTest_compat_deep_value.TestClass::Method4(value class JitTest_compat_deep_value.ValueClass)
      IL_001c:  ldstr      " 5"
      IL_0021:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_0026:  ret

      IL_0027:  pop
      IL_0028:  jmp        class System.String JitTest_compat_deep_value.TestClass::Method4(value class JitTest_compat_deep_value.ValueClass)
    } // end of method TestClass::Method3

    .method private hidebysig static 
            class System.String  Method4(value class JitTest_compat_deep_value.ValueClass A_0) il managed
    {
      // Code size       52 (0x34)
      .maxstack  8
      IL_0000:  ldarga     A_0
      IL_0004:  dup
      IL_0005:  ldfld      int32 JitTest_compat_deep_value.ValueClass::m_fld
      IL_000a:  ldc.i4     0x6
      IL_000f:  rem
      IL_0010:  brtrue.s   IL_0027

      IL_0012:  ldobj      JitTest_compat_deep_value.ValueClass
      IL_0017:  call       class System.String JitTest_compat_deep_value.TestClass::Method5(value class JitTest_compat_deep_value.ValueClass)
      IL_001c:  ldstr      " 6"
      IL_0021:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_0026:  ret

      IL_0027:  ldobj      JitTest_compat_deep_value.ValueClass
      IL_002c:  tail.
      IL_002e:  call       class System.String JitTest_compat_deep_value.TestClass::Method5(value class JitTest_compat_deep_value.ValueClass)
      IL_0033:  ret
    } // end of method TestClass::Method4

    .method private hidebysig static 
            class System.String  Method5(value class JitTest_compat_deep_value.ValueClass A_0) il managed
    {
      // Code size       45 (0x2d)
      .maxstack  8
      IL_0000:  ldarga     A_0
      IL_0004:  dup
      IL_0005:  ldfld      int32 JitTest_compat_deep_value.ValueClass::m_fld
      IL_000a:  ldc.i4     0x7
      IL_000f:  rem
      IL_0010:  brtrue.s   IL_0027

      IL_0012:  ldobj      JitTest_compat_deep_value.ValueClass
      IL_0017:  call       class System.String JitTest_compat_deep_value.TestClass::Method6(value class JitTest_compat_deep_value.ValueClass)
      IL_001c:  ldstr      " 7"
      IL_0021:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_0026:  ret

      IL_0027:  pop
      IL_0028:  jmp        class System.String JitTest_compat_deep_value.TestClass::Method6(value class JitTest_compat_deep_value.ValueClass)
    } // end of method TestClass::Method5

    .method private hidebysig static 
            class System.String  Method6(value class JitTest_compat_deep_value.ValueClass A_0) il managed
    {
      // Code size       52 (0x34)
      .maxstack  8
      IL_0000:  ldarga     A_0
      IL_0004:  dup
      IL_0005:  ldfld      int32 JitTest_compat_deep_value.ValueClass::m_fld
      IL_000a:  ldc.i4     0x8
      IL_000f:  rem
      IL_0010:  brtrue.s   IL_0027

      IL_0012:  ldobj      JitTest_compat_deep_value.ValueClass
      IL_0017:  call       class System.String JitTest_compat_deep_value.TestClass::Method7(value class JitTest_compat_deep_value.ValueClass)
      IL_001c:  ldstr      " 8"
      IL_0021:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_0026:  ret

      IL_0027:  ldobj      JitTest_compat_deep_value.ValueClass
      IL_002c:  tail.
      IL_002e:  call       class System.String JitTest_compat_deep_value.TestClass::Method7(value class JitTest_compat_deep_value.ValueClass)
      IL_0033:  ret
    } // end of method TestClass::Method6

    .method private hidebysig static 
            class System.String  Method7(value class JitTest_compat_deep_value.ValueClass A_0) il managed
    {
      // Code size       45 (0x2d)
      .maxstack  8
      IL_0000:  ldarga     A_0
      IL_0004:  dup
      IL_0005:  ldfld      int32 JitTest_compat_deep_value.ValueClass::m_fld
      IL_000a:  ldc.i4     0x9
      IL_000f:  rem
      IL_0010:  brtrue.s   IL_0027

      IL_0012:  ldobj      JitTest_compat_deep_value.ValueClass
      IL_0017:  call       class System.String JitTest_compat_deep_value.TestClass::Method8(value class JitTest_compat_deep_value.ValueClass)
      IL_001c:  ldstr      " 9"
      IL_0021:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_0026:  ret

      IL_0027:  pop
      IL_0028:  jmp        class System.String JitTest_compat_deep_value.TestClass::Method8(value class JitTest_compat_deep_value.ValueClass)
    } // end of method TestClass::Method7

    .method private hidebysig static 
            class System.String  Method8(value class JitTest_compat_deep_value.ValueClass A_0) il managed
    {
      // Code size       52 (0x34)
      .maxstack  8
      IL_0000:  ldarga     A_0
      IL_0004:  dup
      IL_0005:  ldfld      int32 JitTest_compat_deep_value.ValueClass::m_fld
      IL_000a:  ldc.i4     0xa
      IL_000f:  rem
      IL_0010:  brtrue.s   IL_0027

      IL_0012:  ldobj      JitTest_compat_deep_value.ValueClass
      IL_0017:  call       class System.String JitTest_compat_deep_value.TestClass::Method9(value class JitTest_compat_deep_value.ValueClass)
      IL_001c:  ldstr      " 10"
      IL_0021:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_0026:  ret

      IL_0027:  ldobj      JitTest_compat_deep_value.ValueClass
      IL_002c:  tail.
      IL_002e:  call       class System.String JitTest_compat_deep_value.TestClass::Method9(value class JitTest_compat_deep_value.ValueClass)
      IL_0033:  ret
    } // end of method TestClass::Method8

    .method private hidebysig static 
            class System.String  Method9(value class JitTest_compat_deep_value.ValueClass A_0) il managed
    {
      // Code size       45 (0x2d)
      .maxstack  8
      IL_0000:  ldarga     A_0
      IL_0004:  dup
      IL_0005:  ldfld      int32 JitTest_compat_deep_value.ValueClass::m_fld
      IL_000a:  ldc.i4     0xb
      IL_000f:  rem
      IL_0010:  brtrue.s   IL_0027

      IL_0012:  ldobj      JitTest_compat_deep_value.ValueClass
      IL_0017:  call       class System.String JitTest_compat_deep_value.TestClass::Method10(value class JitTest_compat_deep_value.ValueClass)
      IL_001c:  ldstr      " 11"
      IL_0021:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_0026:  ret

      IL_0027:  pop
      IL_0028:  jmp        class System.String JitTest_compat_deep_value.TestClass::Method10(value class JitTest_compat_deep_value.ValueClass)
    } // end of method TestClass::Method9

    .method private hidebysig static 
            class System.String  Method10(value class JitTest_compat_deep_value.ValueClass A_0) il managed
    {
      // Code size       52 (0x34)
      .maxstack  8
      IL_0000:  ldarga     A_0
      IL_0004:  dup
      IL_0005:  ldfld      int32 JitTest_compat_deep_value.ValueClass::m_fld
      IL_000a:  ldc.i4     0xc
      IL_000f:  rem
      IL_0010:  brtrue.s   IL_0027

      IL_0012:  ldobj      JitTest_compat_deep_value.ValueClass
      IL_0017:  call       class System.String JitTest_compat_deep_value.TestClass::Method11(value class JitTest_compat_deep_value.ValueClass)
      IL_001c:  ldstr      " 12"
      IL_0021:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_0026:  ret

      IL_0027:  ldobj      JitTest_compat_deep_value.ValueClass
      IL_002c:  tail.
      IL_002e:  call       class System.String JitTest_compat_deep_value.TestClass::Method11(value class JitTest_compat_deep_value.ValueClass)
      IL_0033:  ret
    } // end of method TestClass::Method10

    .method private hidebysig static 
            class System.String  Method11(value class JitTest_compat_deep_value.ValueClass A_0) il managed
    {
      // Code size       45 (0x2d)
      .maxstack  8
      IL_0000:  ldarga     A_0
      IL_0004:  dup
      IL_0005:  ldfld      int32 JitTest_compat_deep_value.ValueClass::m_fld
      IL_000a:  ldc.i4     0xd
      IL_000f:  rem
      IL_0010:  brtrue.s   IL_0027

      IL_0012:  ldobj      JitTest_compat_deep_value.ValueClass
      IL_0017:  call       class System.String JitTest_compat_deep_value.TestClass::Method12(value class JitTest_compat_deep_value.ValueClass)
      IL_001c:  ldstr      " 13"
      IL_0021:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_0026:  ret

      IL_0027:  pop
      IL_0028:  jmp        class System.String JitTest_compat_deep_value.TestClass::Method12(value class JitTest_compat_deep_value.ValueClass)
    } // end of method TestClass::Method11

    .method private hidebysig static 
            class System.String  Method12(value class JitTest_compat_deep_value.ValueClass A_0) il managed
    {
      // Code size       52 (0x34)
      .maxstack  8
      IL_0000:  ldarga     A_0
      IL_0004:  dup
      IL_0005:  ldfld      int32 JitTest_compat_deep_value.ValueClass::m_fld
      IL_000a:  ldc.i4     0xe
      IL_000f:  rem
      IL_0010:  brtrue.s   IL_0027

      IL_0012:  ldobj      JitTest_compat_deep_value.ValueClass
      IL_0017:  call       class System.String JitTest_compat_deep_value.TestClass::Method13(value class JitTest_compat_deep_value.ValueClass)
      IL_001c:  ldstr      " 14"
      IL_0021:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_0026:  ret

      IL_0027:  ldobj      JitTest_compat_deep_value.ValueClass
      IL_002c:  tail.
      IL_002e:  call       class System.String JitTest_compat_deep_value.TestClass::Method13(value class JitTest_compat_deep_value.ValueClass)
      IL_0033:  ret
    } // end of method TestClass::Method12

    .method private hidebysig static 
            class System.String  Method13(value class JitTest_compat_deep_value.ValueClass A_0) il managed
    {
      // Code size       45 (0x2d)
      .maxstack  8
      IL_0000:  ldarga     A_0
      IL_0004:  dup
      IL_0005:  ldfld      int32 JitTest_compat_deep_value.ValueClass::m_fld
      IL_000a:  ldc.i4     0xf
      IL_000f:  rem
      IL_0010:  brtrue.s   IL_0027

      IL_0012:  ldobj      JitTest_compat_deep_value.ValueClass
      IL_0017:  call       class System.String JitTest_compat_deep_value.TestClass::Method14(value class JitTest_compat_deep_value.ValueClass)
      IL_001c:  ldstr      " 15"
      IL_0021:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_0026:  ret

      IL_0027:  pop
      IL_0028:  jmp        class System.String JitTest_compat_deep_value.TestClass::Method14(value class JitTest_compat_deep_value.ValueClass)
    } // end of method TestClass::Method13

    .method private hidebysig static 
            class System.String  Method14(value class JitTest_compat_deep_value.ValueClass A_0) il managed
    {
      // Code size       52 (0x34)
      .maxstack  8
      IL_0000:  ldarga     A_0
      IL_0004:  dup
      IL_0005:  ldfld      int32 JitTest_compat_deep_value.ValueClass::m_fld
      IL_000a:  ldc.i4     0x10
      IL_000f:  rem
      IL_0010:  brtrue.s   IL_0027

      IL_0012:  ldobj      JitTest_compat_deep_value.ValueClass
      IL_0017:  call       class System.String JitTest_compat_deep_value.TestClass::Method15(value class JitTest_compat_deep_value.ValueClass)
      IL_001c:  ldstr      " 16"
      IL_0021:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_0026:  ret

      IL_0027:  ldobj      JitTest_compat_deep_value.ValueClass
      IL_002c:  tail.
      IL_002e:  call       class System.String JitTest_compat_deep_value.TestClass::Method15(value class JitTest_compat_deep_value.ValueClass)
      IL_0033:  ret
    } // end of method TestClass::Method14

    .method private hidebysig static 
            class System.String  Method15(value class JitTest_compat_deep_value.ValueClass A_0) il managed
    {
      // Code size       45 (0x2d)
      .maxstack  8
      IL_0000:  ldarga     A_0
      IL_0004:  dup
      IL_0005:  ldfld      int32 JitTest_compat_deep_value.ValueClass::m_fld
      IL_000a:  ldc.i4     0x11
      IL_000f:  rem
      IL_0010:  brtrue.s   IL_0027

      IL_0012:  ldobj      JitTest_compat_deep_value.ValueClass
      IL_0017:  call       class System.String JitTest_compat_deep_value.TestClass::Method16(value class JitTest_compat_deep_value.ValueClass)
      IL_001c:  ldstr      " 17"
      IL_0021:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_0026:  ret

      IL_0027:  pop
      IL_0028:  jmp        class System.String JitTest_compat_deep_value.TestClass::Method16(value class JitTest_compat_deep_value.ValueClass)
    } // end of method TestClass::Method15

    .method private hidebysig static 
            class System.String  Method16(value class JitTest_compat_deep_value.ValueClass A_0) il managed
    {
      // Code size       52 (0x34)
      .maxstack  8
      IL_0000:  ldarga     A_0
      IL_0004:  dup
      IL_0005:  ldfld      int32 JitTest_compat_deep_value.ValueClass::m_fld
      IL_000a:  ldc.i4     0x12
      IL_000f:  rem
      IL_0010:  brtrue.s   IL_0027

      IL_0012:  ldobj      JitTest_compat_deep_value.ValueClass
      IL_0017:  call       class System.String JitTest_compat_deep_value.TestClass::Method17(value class JitTest_compat_deep_value.ValueClass)
      IL_001c:  ldstr      " 18"
      IL_0021:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_0026:  ret

      IL_0027:  ldobj      JitTest_compat_deep_value.ValueClass
      IL_002c:  tail.
      IL_002e:  call       class System.String JitTest_compat_deep_value.TestClass::Method17(value class JitTest_compat_deep_value.ValueClass)
      IL_0033:  ret
    } // end of method TestClass::Method16

    .method private hidebysig static 
            class System.String  Method17(value class JitTest_compat_deep_value.ValueClass A_0) il managed
    {
      // Code size       45 (0x2d)
      .maxstack  8
      IL_0000:  ldarga     A_0
      IL_0004:  dup
      IL_0005:  ldfld      int32 JitTest_compat_deep_value.ValueClass::m_fld
      IL_000a:  ldc.i4     0x13
      IL_000f:  rem
      IL_0010:  brtrue.s   IL_0027

      IL_0012:  ldobj      JitTest_compat_deep_value.ValueClass
      IL_0017:  call       class System.String JitTest_compat_deep_value.TestClass::Method18(value class JitTest_compat_deep_value.ValueClass)
      IL_001c:  ldstr      " 19"
      IL_0021:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_0026:  ret

      IL_0027:  pop
      IL_0028:  jmp        class System.String JitTest_compat_deep_value.TestClass::Method18(value class JitTest_compat_deep_value.ValueClass)
    } // end of method TestClass::Method17

    .method private hidebysig static 
            class System.String  Method18(value class JitTest_compat_deep_value.ValueClass A_0) il managed
    {
      // Code size       52 (0x34)
      .maxstack  8
      IL_0000:  ldarga     A_0
      IL_0004:  dup
      IL_0005:  ldfld      int32 JitTest_compat_deep_value.ValueClass::m_fld
      IL_000a:  ldc.i4     0x14
      IL_000f:  rem
      IL_0010:  brtrue.s   IL_0027

      IL_0012:  ldobj      JitTest_compat_deep_value.ValueClass
      IL_0017:  call       class System.String JitTest_compat_deep_value.TestClass::Method19(value class JitTest_compat_deep_value.ValueClass)
      IL_001c:  ldstr      " 20"
      IL_0021:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_0026:  ret

      IL_0027:  ldobj      JitTest_compat_deep_value.ValueClass
      IL_002c:  tail.
      IL_002e:  call       class System.String JitTest_compat_deep_value.TestClass::Method19(value class JitTest_compat_deep_value.ValueClass)
      IL_0033:  ret
    } // end of method TestClass::Method18

    .method private hidebysig static 
            class System.String  Method19(value class JitTest_compat_deep_value.ValueClass A_0) il managed
    {
      // Code size       45 (0x2d)
      .maxstack  8
      IL_0000:  ldarga     A_0
      IL_0004:  dup
      IL_0005:  ldfld      int32 JitTest_compat_deep_value.ValueClass::m_fld
      IL_000a:  ldc.i4     0x15
      IL_000f:  rem
      IL_0010:  brtrue.s   IL_0027

      IL_0012:  ldobj      JitTest_compat_deep_value.ValueClass
      IL_0017:  call       class System.String JitTest_compat_deep_value.TestClass::Method20(value class JitTest_compat_deep_value.ValueClass)
      IL_001c:  ldstr      " 21"
      IL_0021:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_0026:  ret

      IL_0027:  pop
      IL_0028:  jmp        class System.String JitTest_compat_deep_value.TestClass::Method20(value class JitTest_compat_deep_value.ValueClass)
    } // end of method TestClass::Method19

    .method private hidebysig static 
            class System.String  Method20(value class JitTest_compat_deep_value.ValueClass A_0) il managed
    {
      // Code size       52 (0x34)
      .maxstack  8
      IL_0000:  ldarga     A_0
      IL_0004:  dup
      IL_0005:  ldfld      int32 JitTest_compat_deep_value.ValueClass::m_fld
      IL_000a:  ldc.i4     0x16
      IL_000f:  rem
      IL_0010:  brtrue.s   IL_0027

      IL_0012:  ldobj      JitTest_compat_deep_value.ValueClass
      IL_0017:  call       class System.String JitTest_compat_deep_value.TestClass::Method21(value class JitTest_compat_deep_value.ValueClass)
      IL_001c:  ldstr      " 22"
      IL_0021:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_0026:  ret

      IL_0027:  ldobj      JitTest_compat_deep_value.ValueClass
      IL_002c:  tail.
      IL_002e:  call       class System.String JitTest_compat_deep_value.TestClass::Method21(value class JitTest_compat_deep_value.ValueClass)
      IL_0033:  ret
    } // end of method TestClass::Method20

    .method private hidebysig static 
            class System.String  Method21(value class JitTest_compat_deep_value.ValueClass A_0) il managed
    {
      // Code size       45 (0x2d)
      .maxstack  8
      IL_0000:  ldarga     A_0
      IL_0004:  dup
      IL_0005:  ldfld      int32 JitTest_compat_deep_value.ValueClass::m_fld
      IL_000a:  ldc.i4     0x17
      IL_000f:  rem
      IL_0010:  brtrue.s   IL_0027

      IL_0012:  ldobj      JitTest_compat_deep_value.ValueClass
      IL_0017:  call       class System.String JitTest_compat_deep_value.TestClass::Method22(value class JitTest_compat_deep_value.ValueClass)
      IL_001c:  ldstr      " 23"
      IL_0021:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_0026:  ret

      IL_0027:  pop
      IL_0028:  jmp        class System.String JitTest_compat_deep_value.TestClass::Method22(value class JitTest_compat_deep_value.ValueClass)
    } // end of method TestClass::Method21

    .method private hidebysig static 
            class System.String  Method22(value class JitTest_compat_deep_value.ValueClass A_0) il managed
    {
      // Code size       52 (0x34)
      .maxstack  8
      IL_0000:  ldarga     A_0
      IL_0004:  dup
      IL_0005:  ldfld      int32 JitTest_compat_deep_value.ValueClass::m_fld
      IL_000a:  ldc.i4     0x18
      IL_000f:  rem
      IL_0010:  brtrue.s   IL_0027

      IL_0012:  ldobj      JitTest_compat_deep_value.ValueClass
      IL_0017:  call       class System.String JitTest_compat_deep_value.TestClass::Method23(value class JitTest_compat_deep_value.ValueClass)
      IL_001c:  ldstr      " 24"
      IL_0021:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_0026:  ret

      IL_0027:  ldobj      JitTest_compat_deep_value.ValueClass
      IL_002c:  tail.
      IL_002e:  call       class System.String JitTest_compat_deep_value.TestClass::Method23(value class JitTest_compat_deep_value.ValueClass)
      IL_0033:  ret
    } // end of method TestClass::Method22

    .method private hidebysig static 
            class System.String  Method23(value class JitTest_compat_deep_value.ValueClass A_0) il managed
    {
      // Code size       45 (0x2d)
      .maxstack  8
      IL_0000:  ldarga     A_0
      IL_0004:  dup
      IL_0005:  ldfld      int32 JitTest_compat_deep_value.ValueClass::m_fld
      IL_000a:  ldc.i4     0x19
      IL_000f:  rem
      IL_0010:  brtrue.s   IL_0027

      IL_0012:  ldobj      JitTest_compat_deep_value.ValueClass
      IL_0017:  call       class System.String JitTest_compat_deep_value.TestClass::Method24(value class JitTest_compat_deep_value.ValueClass)
      IL_001c:  ldstr      " 25"
      IL_0021:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_0026:  ret

      IL_0027:  pop
      IL_0028:  jmp        class System.String JitTest_compat_deep_value.TestClass::Method24(value class JitTest_compat_deep_value.ValueClass)
    } // end of method TestClass::Method23

    .method private hidebysig static 
            class System.String  Method24(value class JitTest_compat_deep_value.ValueClass A_0) il managed
    {
      // Code size       52 (0x34)
      .maxstack  8
      IL_0000:  ldarga     A_0
      IL_0004:  dup
      IL_0005:  ldfld      int32 JitTest_compat_deep_value.ValueClass::m_fld
      IL_000a:  ldc.i4     0x1a
      IL_000f:  rem
      IL_0010:  brtrue.s   IL_0027

      IL_0012:  ldobj      JitTest_compat_deep_value.ValueClass
      IL_0017:  call       class System.String JitTest_compat_deep_value.TestClass::Method25(value class JitTest_compat_deep_value.ValueClass)
      IL_001c:  ldstr      " 26"
      IL_0021:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_0026:  ret

      IL_0027:  ldobj      JitTest_compat_deep_value.ValueClass
      IL_002c:  tail.
      IL_002e:  call       class System.String JitTest_compat_deep_value.TestClass::Method25(value class JitTest_compat_deep_value.ValueClass)
      IL_0033:  ret
    } // end of method TestClass::Method24

    .method private hidebysig static 
            class System.String  Method25(value class JitTest_compat_deep_value.ValueClass A_0) il managed
    {
      // Code size       45 (0x2d)
      .maxstack  8
      IL_0000:  ldarga     A_0
      IL_0004:  dup
      IL_0005:  ldfld      int32 JitTest_compat_deep_value.ValueClass::m_fld
      IL_000a:  ldc.i4     0x1b
      IL_000f:  rem
      IL_0010:  brtrue.s   IL_0027

      IL_0012:  ldobj      JitTest_compat_deep_value.ValueClass
      IL_0017:  call       class System.String JitTest_compat_deep_value.TestClass::Method26(value class JitTest_compat_deep_value.ValueClass)
      IL_001c:  ldstr      " 27"
      IL_0021:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_0026:  ret

      IL_0027:  pop
      IL_0028:  jmp        class System.String JitTest_compat_deep_value.TestClass::Method26(value class JitTest_compat_deep_value.ValueClass)
    } // end of method TestClass::Method25

    .method private hidebysig static 
            class System.String  Method26(value class JitTest_compat_deep_value.ValueClass A_0) il managed
    {
      // Code size       52 (0x34)
      .maxstack  8
      IL_0000:  ldarga     A_0
      IL_0004:  dup
      IL_0005:  ldfld      int32 JitTest_compat_deep_value.ValueClass::m_fld
      IL_000a:  ldc.i4     0x1c
      IL_000f:  rem
      IL_0010:  brtrue.s   IL_0027

      IL_0012:  ldobj      JitTest_compat_deep_value.ValueClass
      IL_0017:  call       class System.String JitTest_compat_deep_value.TestClass::Method27(value class JitTest_compat_deep_value.ValueClass)
      IL_001c:  ldstr      " 28"
      IL_0021:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_0026:  ret

      IL_0027:  ldobj      JitTest_compat_deep_value.ValueClass
      IL_002c:  tail.
      IL_002e:  call       class System.String JitTest_compat_deep_value.TestClass::Method27(value class JitTest_compat_deep_value.ValueClass)
      IL_0033:  ret
    } // end of method TestClass::Method26

    .method private hidebysig static 
            class System.String  Method27(value class JitTest_compat_deep_value.ValueClass A_0) il managed
    {
      // Code size       45 (0x2d)
      .maxstack  8
      IL_0000:  ldarga     A_0
      IL_0004:  dup
      IL_0005:  ldfld      int32 JitTest_compat_deep_value.ValueClass::m_fld
      IL_000a:  ldc.i4     0x1d
      IL_000f:  rem
      IL_0010:  brtrue.s   IL_0027

      IL_0012:  ldobj      JitTest_compat_deep_value.ValueClass
      IL_0017:  call       class System.String JitTest_compat_deep_value.TestClass::Method28(value class JitTest_compat_deep_value.ValueClass)
      IL_001c:  ldstr      " 29"
      IL_0021:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_0026:  ret

      IL_0027:  pop
      IL_0028:  jmp        class System.String JitTest_compat_deep_value.TestClass::Method28(value class JitTest_compat_deep_value.ValueClass)
    } // end of method TestClass::Method27

    .method private hidebysig static 
            class System.String  Method28(value class JitTest_compat_deep_value.ValueClass A_0) il managed
    {
      // Code size       52 (0x34)
      .maxstack  8
      IL_0000:  ldarga     A_0
      IL_0004:  dup
      IL_0005:  ldfld      int32 JitTest_compat_deep_value.ValueClass::m_fld
      IL_000a:  ldc.i4     0x1e
      IL_000f:  rem
      IL_0010:  brtrue.s   IL_0027

      IL_0012:  ldobj      JitTest_compat_deep_value.ValueClass
      IL_0017:  call       class System.String JitTest_compat_deep_value.TestClass::Method29(value class JitTest_compat_deep_value.ValueClass)
      IL_001c:  ldstr      " 30"
      IL_0021:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_0026:  ret

      IL_0027:  ldobj      JitTest_compat_deep_value.ValueClass
      IL_002c:  tail.
      IL_002e:  call       class System.String JitTest_compat_deep_value.TestClass::Method29(value class JitTest_compat_deep_value.ValueClass)
      IL_0033:  ret
    } // end of method TestClass::Method28

    .method private hidebysig static 
            class System.String  Method29(value class JitTest_compat_deep_value.ValueClass A_0) il managed
    {
      // Code size       45 (0x2d)
      .maxstack  8
      IL_0000:  ldarga     A_0
      IL_0004:  dup
      IL_0005:  ldfld      int32 JitTest_compat_deep_value.ValueClass::m_fld
      IL_000a:  ldc.i4     0x1f
      IL_000f:  rem
      IL_0010:  brtrue.s   IL_0027

      IL_0012:  ldobj      JitTest_compat_deep_value.ValueClass
      IL_0017:  call       class System.String JitTest_compat_deep_value.TestClass::Method30(value class JitTest_compat_deep_value.ValueClass)
      IL_001c:  ldstr      " 31"
      IL_0021:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_0026:  ret

      IL_0027:  pop
      IL_0028:  jmp        class System.String JitTest_compat_deep_value.TestClass::Method30(value class JitTest_compat_deep_value.ValueClass)
    } // end of method TestClass::Method29

    .method private hidebysig static 
            class System.String  Method30(value class JitTest_compat_deep_value.ValueClass A_0) il managed
    {
      // Code size       52 (0x34)
      .maxstack  8
      IL_0000:  ldarga     A_0
      IL_0004:  dup
      IL_0005:  ldfld      int32 JitTest_compat_deep_value.ValueClass::m_fld
      IL_000a:  ldc.i4     0x20
      IL_000f:  rem
      IL_0010:  brtrue.s   IL_0027

      IL_0012:  ldobj      JitTest_compat_deep_value.ValueClass
      IL_0017:  call       class System.String JitTest_compat_deep_value.TestClass::Method31(value class JitTest_compat_deep_value.ValueClass)
      IL_001c:  ldstr      " 32"
      IL_0021:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_0026:  ret

      IL_0027:  ldobj      JitTest_compat_deep_value.ValueClass
      IL_002c:  tail.
      IL_002e:  call       class System.String JitTest_compat_deep_value.TestClass::Method31(value class JitTest_compat_deep_value.ValueClass)
      IL_0033:  ret
    } // end of method TestClass::Method30

    .method private hidebysig static 
            class System.String  Method31(value class JitTest_compat_deep_value.ValueClass A_0) il managed
    {
      // Code size       45 (0x2d)
      .maxstack  8
      IL_0000:  ldarga     A_0
      IL_0004:  dup
      IL_0005:  ldfld      int32 JitTest_compat_deep_value.ValueClass::m_fld
      IL_000a:  ldc.i4     0x21
      IL_000f:  rem
      IL_0010:  brtrue.s   IL_0027

      IL_0012:  ldobj      JitTest_compat_deep_value.ValueClass
      IL_0017:  call       class System.String JitTest_compat_deep_value.TestClass::Method32(value class JitTest_compat_deep_value.ValueClass)
      IL_001c:  ldstr      " 33"
      IL_0021:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_0026:  ret

      IL_0027:  pop
      IL_0028:  jmp        class System.String JitTest_compat_deep_value.TestClass::Method32(value class JitTest_compat_deep_value.ValueClass)
    } // end of method TestClass::Method31

    .method private hidebysig static 
            class System.String  Method32(value class JitTest_compat_deep_value.ValueClass A_0) il managed
    {
      // Code size       52 (0x34)
      .maxstack  8
      IL_0000:  ldarga     A_0
      IL_0004:  dup
      IL_0005:  ldfld      int32 JitTest_compat_deep_value.ValueClass::m_fld
      IL_000a:  ldc.i4     0x22
      IL_000f:  rem
      IL_0010:  brtrue.s   IL_0027

      IL_0012:  ldobj      JitTest_compat_deep_value.ValueClass
      IL_0017:  call       class System.String JitTest_compat_deep_value.TestClass::Method33(value class JitTest_compat_deep_value.ValueClass)
      IL_001c:  ldstr      " 34"
      IL_0021:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_0026:  ret

      IL_0027:  ldobj      JitTest_compat_deep_value.ValueClass
      IL_002c:  tail.
      IL_002e:  call       class System.String JitTest_compat_deep_value.TestClass::Method33(value class JitTest_compat_deep_value.ValueClass)
      IL_0033:  ret
    } // end of method TestClass::Method32

    .method private hidebysig static 
            class System.String  Method33(value class JitTest_compat_deep_value.ValueClass A_0) il managed
    {
      // Code size       45 (0x2d)
      .maxstack  8
      IL_0000:  ldarga     A_0
      IL_0004:  dup
      IL_0005:  ldfld      int32 JitTest_compat_deep_value.ValueClass::m_fld
      IL_000a:  ldc.i4     0x23
      IL_000f:  rem
      IL_0010:  brtrue.s   IL_0027

      IL_0012:  ldobj      JitTest_compat_deep_value.ValueClass
      IL_0017:  call       class System.String JitTest_compat_deep_value.TestClass::Method34(value class JitTest_compat_deep_value.ValueClass)
      IL_001c:  ldstr      " 35"
      IL_0021:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_0026:  ret

      IL_0027:  pop
      IL_0028:  jmp        class System.String JitTest_compat_deep_value.TestClass::Method34(value class JitTest_compat_deep_value.ValueClass)
    } // end of method TestClass::Method33

    .method private hidebysig static 
            class System.String  Method34(value class JitTest_compat_deep_value.ValueClass A_0) il managed
    {
      // Code size       52 (0x34)
      .maxstack  8
      IL_0000:  ldarga     A_0
      IL_0004:  dup
      IL_0005:  ldfld      int32 JitTest_compat_deep_value.ValueClass::m_fld
      IL_000a:  ldc.i4     0x24
      IL_000f:  rem
      IL_0010:  brtrue.s   IL_0027

      IL_0012:  ldobj      JitTest_compat_deep_value.ValueClass
      IL_0017:  call       class System.String JitTest_compat_deep_value.TestClass::Method35(value class JitTest_compat_deep_value.ValueClass)
      IL_001c:  ldstr      " 36"
      IL_0021:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_0026:  ret

      IL_0027:  ldobj      JitTest_compat_deep_value.ValueClass
      IL_002c:  tail.
      IL_002e:  call       class System.String JitTest_compat_deep_value.TestClass::Method35(value class JitTest_compat_deep_value.ValueClass)
      IL_0033:  ret
    } // end of method TestClass::Method34

    .method private hidebysig static 
            class System.String  Method35(value class JitTest_compat_deep_value.ValueClass A_0) il managed
    {
      // Code size       45 (0x2d)
      .maxstack  8
      IL_0000:  ldarga     A_0
      IL_0004:  dup
      IL_0005:  ldfld      int32 JitTest_compat_deep_value.ValueClass::m_fld
      IL_000a:  ldc.i4     0x25
      IL_000f:  rem
      IL_0010:  brtrue.s   IL_0027

      IL_0012:  ldobj      JitTest_compat_deep_value.ValueClass
      IL_0017:  call       class System.String JitTest_compat_deep_value.TestClass::Method36(value class JitTest_compat_deep_value.ValueClass)
      IL_001c:  ldstr      " 37"
      IL_0021:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_0026:  ret

      IL_0027:  pop
      IL_0028:  jmp        class System.String JitTest_compat_deep_value.TestClass::Method36(value class JitTest_compat_deep_value.ValueClass)
    } // end of method TestClass::Method35

    .method private hidebysig static 
            class System.String  Method36(value class JitTest_compat_deep_value.ValueClass A_0) il managed
    {
      // Code size       52 (0x34)
      .maxstack  8
      IL_0000:  ldarga     A_0
      IL_0004:  dup
      IL_0005:  ldfld      int32 JitTest_compat_deep_value.ValueClass::m_fld
      IL_000a:  ldc.i4     0x26
      IL_000f:  rem
      IL_0010:  brtrue.s   IL_0027

      IL_0012:  ldobj      JitTest_compat_deep_value.ValueClass
      IL_0017:  call       class System.String JitTest_compat_deep_value.TestClass::Method37(value class JitTest_compat_deep_value.ValueClass)
      IL_001c:  ldstr      " 38"
      IL_0021:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_0026:  ret

      IL_0027:  ldobj      JitTest_compat_deep_value.ValueClass
      IL_002c:  tail.
      IL_002e:  call       class System.String JitTest_compat_deep_value.TestClass::Method37(value class JitTest_compat_deep_value.ValueClass)
      IL_0033:  ret
    } // end of method TestClass::Method36

    .method private hidebysig static 
            class System.String  Method37(value class JitTest_compat_deep_value.ValueClass A_0) il managed
    {
      // Code size       45 (0x2d)
      .maxstack  8
      IL_0000:  ldarga     A_0
      IL_0004:  dup
      IL_0005:  ldfld      int32 JitTest_compat_deep_value.ValueClass::m_fld
      IL_000a:  ldc.i4     0x27
      IL_000f:  rem
      IL_0010:  brtrue.s   IL_0027

      IL_0012:  ldobj      JitTest_compat_deep_value.ValueClass
      IL_0017:  call       class System.String JitTest_compat_deep_value.TestClass::Method38(value class JitTest_compat_deep_value.ValueClass)
      IL_001c:  ldstr      " 39"
      IL_0021:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_0026:  ret

      IL_0027:  pop
      IL_0028:  jmp        class System.String JitTest_compat_deep_value.TestClass::Method38(value class JitTest_compat_deep_value.ValueClass)
    } // end of method TestClass::Method37

    .method private hidebysig static 
            class System.String  Method38(value class JitTest_compat_deep_value.ValueClass A_0) il managed
    {
      // Code size       52 (0x34)
      .maxstack  8
      IL_0000:  ldarga     A_0
      IL_0004:  dup
      IL_0005:  ldfld      int32 JitTest_compat_deep_value.ValueClass::m_fld
      IL_000a:  ldc.i4     0x28
      IL_000f:  rem
      IL_0010:  brtrue.s   IL_0027

      IL_0012:  ldobj      JitTest_compat_deep_value.ValueClass
      IL_0017:  call       class System.String JitTest_compat_deep_value.TestClass::Method39(value class JitTest_compat_deep_value.ValueClass)
      IL_001c:  ldstr      " 40"
      IL_0021:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_0026:  ret

      IL_0027:  ldobj      JitTest_compat_deep_value.ValueClass
      IL_002c:  tail.
      IL_002e:  call       class System.String JitTest_compat_deep_value.TestClass::Method39(value class JitTest_compat_deep_value.ValueClass)
      IL_0033:  ret
    } // end of method TestClass::Method38

    .method private hidebysig static 
            class System.String  Method39(value class JitTest_compat_deep_value.ValueClass A_0) il managed
    {
      // Code size       45 (0x2d)
      .maxstack  8
      IL_0000:  ldarga     A_0
      IL_0004:  dup
      IL_0005:  ldfld      int32 JitTest_compat_deep_value.ValueClass::m_fld
      IL_000a:  ldc.i4     0x29
      IL_000f:  rem
      IL_0010:  brtrue.s   IL_0027

      IL_0012:  ldobj      JitTest_compat_deep_value.ValueClass
      IL_0017:  call       class System.String JitTest_compat_deep_value.TestClass::Method40(value class JitTest_compat_deep_value.ValueClass)
      IL_001c:  ldstr      " 41"
      IL_0021:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_0026:  ret

      IL_0027:  pop
      IL_0028:  jmp        class System.String JitTest_compat_deep_value.TestClass::Method40(value class JitTest_compat_deep_value.ValueClass)
    } // end of method TestClass::Method39

    .method private hidebysig static 
            class System.String  Method40(value class JitTest_compat_deep_value.ValueClass A_0) il managed
    {
      // Code size       52 (0x34)
      .maxstack  8
      IL_0000:  ldarga     A_0
      IL_0004:  dup
      IL_0005:  ldfld      int32 JitTest_compat_deep_value.ValueClass::m_fld
      IL_000a:  ldc.i4     0x2a
      IL_000f:  rem
      IL_0010:  brtrue.s   IL_0027

      IL_0012:  ldobj      JitTest_compat_deep_value.ValueClass
      IL_0017:  call       class System.String JitTest_compat_deep_value.TestClass::Method41(value class JitTest_compat_deep_value.ValueClass)
      IL_001c:  ldstr      " 42"
      IL_0021:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_0026:  ret

      IL_0027:  ldobj      JitTest_compat_deep_value.ValueClass
      IL_002c:  tail.
      IL_002e:  call       class System.String JitTest_compat_deep_value.TestClass::Method41(value class JitTest_compat_deep_value.ValueClass)
      IL_0033:  ret
    } // end of method TestClass::Method40

    .method private hidebysig static 
            class System.String  Method41(value class JitTest_compat_deep_value.ValueClass A_0) il managed
    {
      // Code size       45 (0x2d)
      .maxstack  8
      IL_0000:  ldarga     A_0
      IL_0004:  dup
      IL_0005:  ldfld      int32 JitTest_compat_deep_value.ValueClass::m_fld
      IL_000a:  ldc.i4     0x2b
      IL_000f:  rem
      IL_0010:  brtrue.s   IL_0027

      IL_0012:  ldobj      JitTest_compat_deep_value.ValueClass
      IL_0017:  call       class System.String JitTest_compat_deep_value.TestClass::Method42(value class JitTest_compat_deep_value.ValueClass)
      IL_001c:  ldstr      " 43"
      IL_0021:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_0026:  ret

      IL_0027:  pop
      IL_0028:  jmp        class System.String JitTest_compat_deep_value.TestClass::Method42(value class JitTest_compat_deep_value.ValueClass)
    } // end of method TestClass::Method41

    .method private hidebysig static 
            class System.String  Method42(value class JitTest_compat_deep_value.ValueClass A_0) il managed
    {
      // Code size       52 (0x34)
      .maxstack  8
      IL_0000:  ldarga     A_0
      IL_0004:  dup
      IL_0005:  ldfld      int32 JitTest_compat_deep_value.ValueClass::m_fld
      IL_000a:  ldc.i4     0x2c
      IL_000f:  rem
      IL_0010:  brtrue.s   IL_0027

      IL_0012:  ldobj      JitTest_compat_deep_value.ValueClass
      IL_0017:  call       class System.String JitTest_compat_deep_value.TestClass::Method43(value class JitTest_compat_deep_value.ValueClass)
      IL_001c:  ldstr      " 44"
      IL_0021:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_0026:  ret

      IL_0027:  ldobj      JitTest_compat_deep_value.ValueClass
      IL_002c:  tail.
      IL_002e:  call       class System.String JitTest_compat_deep_value.TestClass::Method43(value class JitTest_compat_deep_value.ValueClass)
      IL_0033:  ret
    } // end of method TestClass::Method42

    .method private hidebysig static 
            class System.String  Method43(value class JitTest_compat_deep_value.ValueClass A_0) il managed
    {
      // Code size       45 (0x2d)
      .maxstack  8
      IL_0000:  ldarga     A_0
      IL_0004:  dup
      IL_0005:  ldfld      int32 JitTest_compat_deep_value.ValueClass::m_fld
      IL_000a:  ldc.i4     0x2d
      IL_000f:  rem
      IL_0010:  brtrue.s   IL_0027

      IL_0012:  ldobj      JitTest_compat_deep_value.ValueClass
      IL_0017:  call       class System.String JitTest_compat_deep_value.TestClass::Method44(value class JitTest_compat_deep_value.ValueClass)
      IL_001c:  ldstr      " 45"
      IL_0021:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_0026:  ret

      IL_0027:  pop
      IL_0028:  jmp        class System.String JitTest_compat_deep_value.TestClass::Method44(value class JitTest_compat_deep_value.ValueClass)
    } // end of method TestClass::Method43

    .method private hidebysig static 
            class System.String  Method44(value class JitTest_compat_deep_value.ValueClass A_0) il managed
    {
      // Code size       52 (0x34)
      .maxstack  8
      IL_0000:  ldarga     A_0
      IL_0004:  dup
      IL_0005:  ldfld      int32 JitTest_compat_deep_value.ValueClass::m_fld
      IL_000a:  ldc.i4     0x2e
      IL_000f:  rem
      IL_0010:  brtrue.s   IL_0027

      IL_0012:  ldobj      JitTest_compat_deep_value.ValueClass
      IL_0017:  call       class System.String JitTest_compat_deep_value.TestClass::Method45(value class JitTest_compat_deep_value.ValueClass)
      IL_001c:  ldstr      " 46"
      IL_0021:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_0026:  ret

      IL_0027:  ldobj      JitTest_compat_deep_value.ValueClass
      IL_002c:  tail.
      IL_002e:  call       class System.String JitTest_compat_deep_value.TestClass::Method45(value class JitTest_compat_deep_value.ValueClass)
      IL_0033:  ret
    } // end of method TestClass::Method44

    .method private hidebysig static 
            class System.String  Method45(value class JitTest_compat_deep_value.ValueClass A_0) il managed
    {
      // Code size       45 (0x2d)
      .maxstack  8
      IL_0000:  ldarga     A_0
      IL_0004:  dup
      IL_0005:  ldfld      int32 JitTest_compat_deep_value.ValueClass::m_fld
      IL_000a:  ldc.i4     0x2f
      IL_000f:  rem
      IL_0010:  brtrue.s   IL_0027

      IL_0012:  ldobj      JitTest_compat_deep_value.ValueClass
      IL_0017:  call       class System.String JitTest_compat_deep_value.TestClass::Method46(value class JitTest_compat_deep_value.ValueClass)
      IL_001c:  ldstr      " 47"
      IL_0021:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_0026:  ret

      IL_0027:  pop
      IL_0028:  jmp        class System.String JitTest_compat_deep_value.TestClass::Method46(value class JitTest_compat_deep_value.ValueClass)
    } // end of method TestClass::Method45

    .method private hidebysig static 
            class System.String  Method46(value class JitTest_compat_deep_value.ValueClass A_0) il managed
    {
      // Code size       52 (0x34)
      .maxstack  8
      IL_0000:  ldarga     A_0
      IL_0004:  dup
      IL_0005:  ldfld      int32 JitTest_compat_deep_value.ValueClass::m_fld
      IL_000a:  ldc.i4     0x30
      IL_000f:  rem
      IL_0010:  brtrue.s   IL_0027

      IL_0012:  ldobj      JitTest_compat_deep_value.ValueClass
      IL_0017:  call       class System.String JitTest_compat_deep_value.TestClass::Method47(value class JitTest_compat_deep_value.ValueClass)
      IL_001c:  ldstr      " 48"
      IL_0021:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_0026:  ret

      IL_0027:  ldobj      JitTest_compat_deep_value.ValueClass
      IL_002c:  tail.
      IL_002e:  call       class System.String JitTest_compat_deep_value.TestClass::Method47(value class JitTest_compat_deep_value.ValueClass)
      IL_0033:  ret
    } // end of method TestClass::Method46

    .method private hidebysig static 
            class System.String  Method47(value class JitTest_compat_deep_value.ValueClass A_0) il managed
    {
      // Code size       45 (0x2d)
      .maxstack  8
      IL_0000:  ldarga     A_0
      IL_0004:  dup
      IL_0005:  ldfld      int32 JitTest_compat_deep_value.ValueClass::m_fld
      IL_000a:  ldc.i4     0x31
      IL_000f:  rem
      IL_0010:  brtrue.s   IL_0027

      IL_0012:  ldobj      JitTest_compat_deep_value.ValueClass
      IL_0017:  call       class System.String JitTest_compat_deep_value.TestClass::Method48(value class JitTest_compat_deep_value.ValueClass)
      IL_001c:  ldstr      " 49"
      IL_0021:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_0026:  ret

      IL_0027:  pop
      IL_0028:  jmp        class System.String JitTest_compat_deep_value.TestClass::Method48(value class JitTest_compat_deep_value.ValueClass)
    } // end of method TestClass::Method47

    .method private hidebysig static 
            class System.String  Method48(value class JitTest_compat_deep_value.ValueClass A_0) il managed
    {
      // Code size       52 (0x34)
      .maxstack  8
      IL_0000:  ldarga     A_0
      IL_0004:  dup
      IL_0005:  ldfld      int32 JitTest_compat_deep_value.ValueClass::m_fld
      IL_000a:  ldc.i4     0x32
      IL_000f:  rem
      IL_0010:  brtrue.s   IL_0027

      IL_0012:  ldobj      JitTest_compat_deep_value.ValueClass
      IL_0017:  call       class System.String JitTest_compat_deep_value.TestClass::Method49(value class JitTest_compat_deep_value.ValueClass)
      IL_001c:  ldstr      " 50"
      IL_0021:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_0026:  ret

      IL_0027:  ldobj      JitTest_compat_deep_value.ValueClass
      IL_002c:  tail.
      IL_002e:  call       class System.String JitTest_compat_deep_value.TestClass::Method49(value class JitTest_compat_deep_value.ValueClass)
      IL_0033:  ret
    } // end of method TestClass::Method48

    .method private hidebysig static 
            class System.String  Method49(value class JitTest_compat_deep_value.ValueClass A_0) il managed
    {
      // Code size       45 (0x2d)
      .maxstack  8
      IL_0000:  ldarga     A_0
      IL_0004:  dup
      IL_0005:  ldfld      int32 JitTest_compat_deep_value.ValueClass::m_fld
      IL_000a:  ldc.i4     0x33
      IL_000f:  rem
      IL_0010:  brtrue.s   IL_0027

      IL_0012:  ldobj      JitTest_compat_deep_value.ValueClass
      IL_0017:  call       class System.String JitTest_compat_deep_value.TestClass::Method50(value class JitTest_compat_deep_value.ValueClass)
      IL_001c:  ldstr      " 51"
      IL_0021:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_0026:  ret

      IL_0027:  pop
      IL_0028:  jmp        class System.String JitTest_compat_deep_value.TestClass::Method50(value class JitTest_compat_deep_value.ValueClass)
    } // end of method TestClass::Method49

    .method private hidebysig static 
            class System.String  Method50(value class JitTest_compat_deep_value.ValueClass A_0) il managed
    {
      // Code size       2 (0x2)
      .maxstack  8
      IL_0000:  ldnull
      IL_0001:  ret
    } // end of method TestClass::Method50

    .method public hidebysig static 
            int32 Main() il managed
    {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint
      // Code size       93 (0x5d)
      .maxstack  8
      .locals init (value class JitTest_compat_deep_value.ValueClass V_0)
      IL_0000:  ldloca     V_0
      IL_0004:  ldc.i4.2
      IL_0005:  stfld      int32 JitTest_compat_deep_value.ValueClass::m_fld
      IL_000a:  ldloc.0
      IL_000b:  call       class System.String JitTest_compat_deep_value.TestClass::Method0(value class JitTest_compat_deep_value.ValueClass)
      IL_0010:  brtrue     IL_002d

      IL_0015:  ldloca     V_0
      IL_0019:  ldfld      int32 JitTest_compat_deep_value.ValueClass::m_fld
      IL_001e:  call       void [System.Console]System.Console::Write(int32)
      IL_0023:  ldstr      " - prime!"
      IL_0028:  call       void [System.Console]System.Console::WriteLine(class System.String)
      IL_002d:  ldloca     V_0
      IL_0031:  dup
      IL_0032:  ldfld      int32 JitTest_compat_deep_value.ValueClass::m_fld
      IL_0037:  ldc.i4.1
      IL_0038:  add
      IL_0039:  stfld      int32 JitTest_compat_deep_value.ValueClass::m_fld
      IL_003e:  ldloca     V_0
      IL_0042:  ldfld      int32 JitTest_compat_deep_value.ValueClass::m_fld
      IL_0047:  ldc.i4     0x64
      IL_004c:  ceq
      IL_004e:  brfalse    IL_000a

      IL_0053:  ldloca     V_0
      IL_0057:  ldfld      int32 JitTest_compat_deep_value.ValueClass::m_fld
      IL_005c:  ret
    } // end of method TestClass::Main

    .method public hidebysig specialname rtspecialname 
            instance void .ctor() il managed
    {
      // Code size       7 (0x7)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ret
    } // end of method TestClass::.ctor

  } // end of class TestClass

} // end of namespace JitTest_compat_deep_value
