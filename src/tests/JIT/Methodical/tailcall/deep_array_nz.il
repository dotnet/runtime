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
.namespace JitTest_compat_deep_array_nz
{
  .class public auto ansi beforefieldinit TestClass extends [mscorlib]System.Object
  {
//-----BEGIN AUTO GENERATED CODE-----//
.method private hidebysig static class [mscorlib]System.String Method0(int32[100...]) il managed {
  .maxstack 8
  ldarg.0
  dup
  ldc.i4 100
  call instance int32 int32[100...]::Get(int32)
  ldc.i4 2
  rem
  brtrue.s REM
  call class [mscorlib]System.String JitTest_compat_deep_array_nz.TestClass::Method1(int32[100...])
  ldstr " 2"
  call class [mscorlib]System.String [mscorlib]System.String::Concat(class [mscorlib]System.String, class [mscorlib]System.String)
  ret
REM:
  tail. call class [mscorlib]System.String JitTest_compat_deep_array_nz.TestClass::Method1(int32[100...])
  ret
}
.method private hidebysig static class [mscorlib]System.String Method1(int32[100...]) il managed {
  .maxstack 8
  ldarg.0
  dup
  ldc.i4 100
  call instance int32 int32[100...]::Get(int32)
  ldc.i4 3
  rem
  brtrue.s REM
  call class [mscorlib]System.String JitTest_compat_deep_array_nz.TestClass::Method2(int32[100...])
  ldstr " 3"
  call class [mscorlib]System.String [mscorlib]System.String::Concat(class [mscorlib]System.String, class [mscorlib]System.String)
  ret
REM:
  pop
  jmp class [mscorlib]System.String JitTest_compat_deep_array_nz.TestClass::Method2(int32[100...])
}
.method private hidebysig static class [mscorlib]System.String Method2(int32[100...]) il managed {
  .maxstack 8
  ldarg.0
  dup
  ldc.i4 100
  call instance int32 int32[100...]::Get(int32)
  ldc.i4 4
  rem
  brtrue.s REM
  call class [mscorlib]System.String JitTest_compat_deep_array_nz.TestClass::Method3(int32[100...])
  ldstr " 4"
  call class [mscorlib]System.String [mscorlib]System.String::Concat(class [mscorlib]System.String, class [mscorlib]System.String)
  ret
REM:
  tail. call class [mscorlib]System.String JitTest_compat_deep_array_nz.TestClass::Method3(int32[100...])
  ret
}
.method private hidebysig static class [mscorlib]System.String Method3(int32[100...]) il managed {
  .maxstack 8
  ldarg.0
  dup
  ldc.i4 100
  call instance int32 int32[100...]::Get(int32)
  ldc.i4 5
  rem
  brtrue.s REM
  call class [mscorlib]System.String JitTest_compat_deep_array_nz.TestClass::Method4(int32[100...])
  ldstr " 5"
  call class [mscorlib]System.String [mscorlib]System.String::Concat(class [mscorlib]System.String, class [mscorlib]System.String)
  ret
REM:
  pop
  jmp class [mscorlib]System.String JitTest_compat_deep_array_nz.TestClass::Method4(int32[100...])
}
.method private hidebysig static class [mscorlib]System.String Method4(int32[100...]) il managed {
  .maxstack 8
  ldarg.0
  dup
  ldc.i4 100
  call instance int32 int32[100...]::Get(int32)
  ldc.i4 6
  rem
  brtrue.s REM
  call class [mscorlib]System.String JitTest_compat_deep_array_nz.TestClass::Method5(int32[100...])
  ldstr " 6"
  call class [mscorlib]System.String [mscorlib]System.String::Concat(class [mscorlib]System.String, class [mscorlib]System.String)
  ret
REM:
  tail. call class [mscorlib]System.String JitTest_compat_deep_array_nz.TestClass::Method5(int32[100...])
  ret
}
.method private hidebysig static class [mscorlib]System.String Method5(int32[100...]) il managed {
  .maxstack 8
  ldarg.0
  dup
  ldc.i4 100
  call instance int32 int32[100...]::Get(int32)
  ldc.i4 7
  rem
  brtrue.s REM
  call class [mscorlib]System.String JitTest_compat_deep_array_nz.TestClass::Method6(int32[100...])
  ldstr " 7"
  call class [mscorlib]System.String [mscorlib]System.String::Concat(class [mscorlib]System.String, class [mscorlib]System.String)
  ret
REM:
  pop
  jmp class [mscorlib]System.String JitTest_compat_deep_array_nz.TestClass::Method6(int32[100...])
}
.method private hidebysig static class [mscorlib]System.String Method6(int32[100...]) il managed {
  .maxstack 8
  ldarg.0
  dup
  ldc.i4 100
  call instance int32 int32[100...]::Get(int32)
  ldc.i4 8
  rem
  brtrue.s REM
  call class [mscorlib]System.String JitTest_compat_deep_array_nz.TestClass::Method7(int32[100...])
  ldstr " 8"
  call class [mscorlib]System.String [mscorlib]System.String::Concat(class [mscorlib]System.String, class [mscorlib]System.String)
  ret
REM:
  tail. call class [mscorlib]System.String JitTest_compat_deep_array_nz.TestClass::Method7(int32[100...])
  ret
}
.method private hidebysig static class [mscorlib]System.String Method7(int32[100...]) il managed {
  .maxstack 8
  ldarg.0
  dup
  ldc.i4 100
  call instance int32 int32[100...]::Get(int32)
  ldc.i4 9
  rem
  brtrue.s REM
  call class [mscorlib]System.String JitTest_compat_deep_array_nz.TestClass::Method8(int32[100...])
  ldstr " 9"
  call class [mscorlib]System.String [mscorlib]System.String::Concat(class [mscorlib]System.String, class [mscorlib]System.String)
  ret
REM:
  pop
  jmp class [mscorlib]System.String JitTest_compat_deep_array_nz.TestClass::Method8(int32[100...])
}
.method private hidebysig static class [mscorlib]System.String Method8(int32[100...]) il managed {
  .maxstack 8
  ldarg.0
  dup
  ldc.i4 100
  call instance int32 int32[100...]::Get(int32)
  ldc.i4 10
  rem
  brtrue.s REM
  call class [mscorlib]System.String JitTest_compat_deep_array_nz.TestClass::Method9(int32[100...])
  ldstr " 10"
  call class [mscorlib]System.String [mscorlib]System.String::Concat(class [mscorlib]System.String, class [mscorlib]System.String)
  ret
REM:
  tail. call class [mscorlib]System.String JitTest_compat_deep_array_nz.TestClass::Method9(int32[100...])
  ret
}
.method private hidebysig static class [mscorlib]System.String Method9(int32[100...]) il managed {
  .maxstack 8
  ldarg.0
  dup
  ldc.i4 100
  call instance int32 int32[100...]::Get(int32)
  ldc.i4 11
  rem
  brtrue.s REM
  call class [mscorlib]System.String JitTest_compat_deep_array_nz.TestClass::Method10(int32[100...])
  ldstr " 11"
  call class [mscorlib]System.String [mscorlib]System.String::Concat(class [mscorlib]System.String, class [mscorlib]System.String)
  ret
REM:
  pop
  jmp class [mscorlib]System.String JitTest_compat_deep_array_nz.TestClass::Method10(int32[100...])
}
.method private hidebysig static class [mscorlib]System.String Method10(int32[100...]) il managed {
  .maxstack 8
  ldarg.0
  dup
  ldc.i4 100
  call instance int32 int32[100...]::Get(int32)
  ldc.i4 12
  rem
  brtrue.s REM
  call class [mscorlib]System.String JitTest_compat_deep_array_nz.TestClass::Method11(int32[100...])
  ldstr " 12"
  call class [mscorlib]System.String [mscorlib]System.String::Concat(class [mscorlib]System.String, class [mscorlib]System.String)
  ret
REM:
  tail. call class [mscorlib]System.String JitTest_compat_deep_array_nz.TestClass::Method11(int32[100...])
  ret
}
.method private hidebysig static class [mscorlib]System.String Method11(int32[100...]) il managed {
  .maxstack 8
  ldarg.0
  dup
  ldc.i4 100
  call instance int32 int32[100...]::Get(int32)
  ldc.i4 13
  rem
  brtrue.s REM
  call class [mscorlib]System.String JitTest_compat_deep_array_nz.TestClass::Method12(int32[100...])
  ldstr " 13"
  call class [mscorlib]System.String [mscorlib]System.String::Concat(class [mscorlib]System.String, class [mscorlib]System.String)
  ret
REM:
  pop
  jmp class [mscorlib]System.String JitTest_compat_deep_array_nz.TestClass::Method12(int32[100...])
}
.method private hidebysig static class [mscorlib]System.String Method12(int32[100...]) il managed {
  .maxstack 8
  ldarg.0
  dup
  ldc.i4 100
  call instance int32 int32[100...]::Get(int32)
  ldc.i4 14
  rem
  brtrue.s REM
  call class [mscorlib]System.String JitTest_compat_deep_array_nz.TestClass::Method13(int32[100...])
  ldstr " 14"
  call class [mscorlib]System.String [mscorlib]System.String::Concat(class [mscorlib]System.String, class [mscorlib]System.String)
  ret
REM:
  tail. call class [mscorlib]System.String JitTest_compat_deep_array_nz.TestClass::Method13(int32[100...])
  ret
}
.method private hidebysig static class [mscorlib]System.String Method13(int32[100...]) il managed {
  .maxstack 8
  ldarg.0
  dup
  ldc.i4 100
  call instance int32 int32[100...]::Get(int32)
  ldc.i4 15
  rem
  brtrue.s REM
  call class [mscorlib]System.String JitTest_compat_deep_array_nz.TestClass::Method14(int32[100...])
  ldstr " 15"
  call class [mscorlib]System.String [mscorlib]System.String::Concat(class [mscorlib]System.String, class [mscorlib]System.String)
  ret
REM:
  pop
  jmp class [mscorlib]System.String JitTest_compat_deep_array_nz.TestClass::Method14(int32[100...])
}
.method private hidebysig static class [mscorlib]System.String Method14(int32[100...]) il managed {
  .maxstack 8
  ldarg.0
  dup
  ldc.i4 100
  call instance int32 int32[100...]::Get(int32)
  ldc.i4 16
  rem
  brtrue.s REM
  call class [mscorlib]System.String JitTest_compat_deep_array_nz.TestClass::Method15(int32[100...])
  ldstr " 16"
  call class [mscorlib]System.String [mscorlib]System.String::Concat(class [mscorlib]System.String, class [mscorlib]System.String)
  ret
REM:
  tail. call class [mscorlib]System.String JitTest_compat_deep_array_nz.TestClass::Method15(int32[100...])
  ret
}
.method private hidebysig static class [mscorlib]System.String Method15(int32[100...]) il managed {
  .maxstack 8
  ldarg.0
  dup
  ldc.i4 100
  call instance int32 int32[100...]::Get(int32)
  ldc.i4 17
  rem
  brtrue.s REM
  call class [mscorlib]System.String JitTest_compat_deep_array_nz.TestClass::Method16(int32[100...])
  ldstr " 17"
  call class [mscorlib]System.String [mscorlib]System.String::Concat(class [mscorlib]System.String, class [mscorlib]System.String)
  ret
REM:
  pop
  jmp class [mscorlib]System.String JitTest_compat_deep_array_nz.TestClass::Method16(int32[100...])
}
.method private hidebysig static class [mscorlib]System.String Method16(int32[100...]) il managed {
  .maxstack 8
  ldarg.0
  dup
  ldc.i4 100
  call instance int32 int32[100...]::Get(int32)
  ldc.i4 18
  rem
  brtrue.s REM
  call class [mscorlib]System.String JitTest_compat_deep_array_nz.TestClass::Method17(int32[100...])
  ldstr " 18"
  call class [mscorlib]System.String [mscorlib]System.String::Concat(class [mscorlib]System.String, class [mscorlib]System.String)
  ret
REM:
  tail. call class [mscorlib]System.String JitTest_compat_deep_array_nz.TestClass::Method17(int32[100...])
  ret
}
.method private hidebysig static class [mscorlib]System.String Method17(int32[100...]) il managed {
  .maxstack 8
  ldarg.0
  dup
  ldc.i4 100
  call instance int32 int32[100...]::Get(int32)
  ldc.i4 19
  rem
  brtrue.s REM
  call class [mscorlib]System.String JitTest_compat_deep_array_nz.TestClass::Method18(int32[100...])
  ldstr " 19"
  call class [mscorlib]System.String [mscorlib]System.String::Concat(class [mscorlib]System.String, class [mscorlib]System.String)
  ret
REM:
  pop
  jmp class [mscorlib]System.String JitTest_compat_deep_array_nz.TestClass::Method18(int32[100...])
}
.method private hidebysig static class [mscorlib]System.String Method18(int32[100...]) il managed {
  .maxstack 8
  ldarg.0
  dup
  ldc.i4 100
  call instance int32 int32[100...]::Get(int32)
  ldc.i4 20
  rem
  brtrue.s REM
  call class [mscorlib]System.String JitTest_compat_deep_array_nz.TestClass::Method19(int32[100...])
  ldstr " 20"
  call class [mscorlib]System.String [mscorlib]System.String::Concat(class [mscorlib]System.String, class [mscorlib]System.String)
  ret
REM:
  tail. call class [mscorlib]System.String JitTest_compat_deep_array_nz.TestClass::Method19(int32[100...])
  ret
}
.method private hidebysig static class [mscorlib]System.String Method19(int32[100...]) il managed {
  .maxstack 8
  ldarg.0
  dup
  ldc.i4 100
  call instance int32 int32[100...]::Get(int32)
  ldc.i4 21
  rem
  brtrue.s REM
  call class [mscorlib]System.String JitTest_compat_deep_array_nz.TestClass::Method20(int32[100...])
  ldstr " 21"
  call class [mscorlib]System.String [mscorlib]System.String::Concat(class [mscorlib]System.String, class [mscorlib]System.String)
  ret
REM:
  pop
  jmp class [mscorlib]System.String JitTest_compat_deep_array_nz.TestClass::Method20(int32[100...])
}
.method private hidebysig static class [mscorlib]System.String Method20(int32[100...]) il managed {
  .maxstack 8
  ldarg.0
  dup
  ldc.i4 100
  call instance int32 int32[100...]::Get(int32)
  ldc.i4 22
  rem
  brtrue.s REM
  call class [mscorlib]System.String JitTest_compat_deep_array_nz.TestClass::Method21(int32[100...])
  ldstr " 22"
  call class [mscorlib]System.String [mscorlib]System.String::Concat(class [mscorlib]System.String, class [mscorlib]System.String)
  ret
REM:
  tail. call class [mscorlib]System.String JitTest_compat_deep_array_nz.TestClass::Method21(int32[100...])
  ret
}
.method private hidebysig static class [mscorlib]System.String Method21(int32[100...]) il managed {
  .maxstack 8
  ldarg.0
  dup
  ldc.i4 100
  call instance int32 int32[100...]::Get(int32)
  ldc.i4 23
  rem
  brtrue.s REM
  call class [mscorlib]System.String JitTest_compat_deep_array_nz.TestClass::Method22(int32[100...])
  ldstr " 23"
  call class [mscorlib]System.String [mscorlib]System.String::Concat(class [mscorlib]System.String, class [mscorlib]System.String)
  ret
REM:
  pop
  jmp class [mscorlib]System.String JitTest_compat_deep_array_nz.TestClass::Method22(int32[100...])
}
.method private hidebysig static class [mscorlib]System.String Method22(int32[100...]) il managed {
  .maxstack 8
  ldarg.0
  dup
  ldc.i4 100
  call instance int32 int32[100...]::Get(int32)
  ldc.i4 24
  rem
  brtrue.s REM
  call class [mscorlib]System.String JitTest_compat_deep_array_nz.TestClass::Method23(int32[100...])
  ldstr " 24"
  call class [mscorlib]System.String [mscorlib]System.String::Concat(class [mscorlib]System.String, class [mscorlib]System.String)
  ret
REM:
  tail. call class [mscorlib]System.String JitTest_compat_deep_array_nz.TestClass::Method23(int32[100...])
  ret
}
.method private hidebysig static class [mscorlib]System.String Method23(int32[100...]) il managed {
  .maxstack 8
  ldarg.0
  dup
  ldc.i4 100
  call instance int32 int32[100...]::Get(int32)
  ldc.i4 25
  rem
  brtrue.s REM
  call class [mscorlib]System.String JitTest_compat_deep_array_nz.TestClass::Method24(int32[100...])
  ldstr " 25"
  call class [mscorlib]System.String [mscorlib]System.String::Concat(class [mscorlib]System.String, class [mscorlib]System.String)
  ret
REM:
  pop
  jmp class [mscorlib]System.String JitTest_compat_deep_array_nz.TestClass::Method24(int32[100...])
}
.method private hidebysig static class [mscorlib]System.String Method24(int32[100...]) il managed {
  .maxstack 8
  ldarg.0
  dup
  ldc.i4 100
  call instance int32 int32[100...]::Get(int32)
  ldc.i4 26
  rem
  brtrue.s REM
  call class [mscorlib]System.String JitTest_compat_deep_array_nz.TestClass::Method25(int32[100...])
  ldstr " 26"
  call class [mscorlib]System.String [mscorlib]System.String::Concat(class [mscorlib]System.String, class [mscorlib]System.String)
  ret
REM:
  tail. call class [mscorlib]System.String JitTest_compat_deep_array_nz.TestClass::Method25(int32[100...])
  ret
}
.method private hidebysig static class [mscorlib]System.String Method25(int32[100...]) il managed {
  .maxstack 8
  ldarg.0
  dup
  ldc.i4 100
  call instance int32 int32[100...]::Get(int32)
  ldc.i4 27
  rem
  brtrue.s REM
  call class [mscorlib]System.String JitTest_compat_deep_array_nz.TestClass::Method26(int32[100...])
  ldstr " 27"
  call class [mscorlib]System.String [mscorlib]System.String::Concat(class [mscorlib]System.String, class [mscorlib]System.String)
  ret
REM:
  pop
  jmp class [mscorlib]System.String JitTest_compat_deep_array_nz.TestClass::Method26(int32[100...])
}
.method private hidebysig static class [mscorlib]System.String Method26(int32[100...]) il managed {
  .maxstack 8
  ldarg.0
  dup
  ldc.i4 100
  call instance int32 int32[100...]::Get(int32)
  ldc.i4 28
  rem
  brtrue.s REM
  call class [mscorlib]System.String JitTest_compat_deep_array_nz.TestClass::Method27(int32[100...])
  ldstr " 28"
  call class [mscorlib]System.String [mscorlib]System.String::Concat(class [mscorlib]System.String, class [mscorlib]System.String)
  ret
REM:
  tail. call class [mscorlib]System.String JitTest_compat_deep_array_nz.TestClass::Method27(int32[100...])
  ret
}
.method private hidebysig static class [mscorlib]System.String Method27(int32[100...]) il managed {
  .maxstack 8
  ldarg.0
  dup
  ldc.i4 100
  call instance int32 int32[100...]::Get(int32)
  ldc.i4 29
  rem
  brtrue.s REM
  call class [mscorlib]System.String JitTest_compat_deep_array_nz.TestClass::Method28(int32[100...])
  ldstr " 29"
  call class [mscorlib]System.String [mscorlib]System.String::Concat(class [mscorlib]System.String, class [mscorlib]System.String)
  ret
REM:
  pop
  jmp class [mscorlib]System.String JitTest_compat_deep_array_nz.TestClass::Method28(int32[100...])
}
.method private hidebysig static class [mscorlib]System.String Method28(int32[100...]) il managed {
  .maxstack 8
  ldarg.0
  dup
  ldc.i4 100
  call instance int32 int32[100...]::Get(int32)
  ldc.i4 30
  rem
  brtrue.s REM
  call class [mscorlib]System.String JitTest_compat_deep_array_nz.TestClass::Method29(int32[100...])
  ldstr " 30"
  call class [mscorlib]System.String [mscorlib]System.String::Concat(class [mscorlib]System.String, class [mscorlib]System.String)
  ret
REM:
  tail. call class [mscorlib]System.String JitTest_compat_deep_array_nz.TestClass::Method29(int32[100...])
  ret
}
.method private hidebysig static class [mscorlib]System.String Method29(int32[100...]) il managed {
  .maxstack 8
  ldarg.0
  dup
  ldc.i4 100
  call instance int32 int32[100...]::Get(int32)
  ldc.i4 31
  rem
  brtrue.s REM
  call class [mscorlib]System.String JitTest_compat_deep_array_nz.TestClass::Method30(int32[100...])
  ldstr " 31"
  call class [mscorlib]System.String [mscorlib]System.String::Concat(class [mscorlib]System.String, class [mscorlib]System.String)
  ret
REM:
  pop
  jmp class [mscorlib]System.String JitTest_compat_deep_array_nz.TestClass::Method30(int32[100...])
}
.method private hidebysig static class [mscorlib]System.String Method30(int32[100...]) il managed {
  .maxstack 8
  ldarg.0
  dup
  ldc.i4 100
  call instance int32 int32[100...]::Get(int32)
  ldc.i4 32
  rem
  brtrue.s REM
  call class [mscorlib]System.String JitTest_compat_deep_array_nz.TestClass::Method31(int32[100...])
  ldstr " 32"
  call class [mscorlib]System.String [mscorlib]System.String::Concat(class [mscorlib]System.String, class [mscorlib]System.String)
  ret
REM:
  tail. call class [mscorlib]System.String JitTest_compat_deep_array_nz.TestClass::Method31(int32[100...])
  ret
}
.method private hidebysig static class [mscorlib]System.String Method31(int32[100...]) il managed {
  .maxstack 8
  ldarg.0
  dup
  ldc.i4 100
  call instance int32 int32[100...]::Get(int32)
  ldc.i4 33
  rem
  brtrue.s REM
  call class [mscorlib]System.String JitTest_compat_deep_array_nz.TestClass::Method32(int32[100...])
  ldstr " 33"
  call class [mscorlib]System.String [mscorlib]System.String::Concat(class [mscorlib]System.String, class [mscorlib]System.String)
  ret
REM:
  pop
  jmp class [mscorlib]System.String JitTest_compat_deep_array_nz.TestClass::Method32(int32[100...])
}
.method private hidebysig static class [mscorlib]System.String Method32(int32[100...]) il managed {
  .maxstack 8
  ldarg.0
  dup
  ldc.i4 100
  call instance int32 int32[100...]::Get(int32)
  ldc.i4 34
  rem
  brtrue.s REM
  call class [mscorlib]System.String JitTest_compat_deep_array_nz.TestClass::Method33(int32[100...])
  ldstr " 34"
  call class [mscorlib]System.String [mscorlib]System.String::Concat(class [mscorlib]System.String, class [mscorlib]System.String)
  ret
REM:
  tail. call class [mscorlib]System.String JitTest_compat_deep_array_nz.TestClass::Method33(int32[100...])
  ret
}
.method private hidebysig static class [mscorlib]System.String Method33(int32[100...]) il managed {
  .maxstack 8
  ldarg.0
  dup
  ldc.i4 100
  call instance int32 int32[100...]::Get(int32)
  ldc.i4 35
  rem
  brtrue.s REM
  call class [mscorlib]System.String JitTest_compat_deep_array_nz.TestClass::Method34(int32[100...])
  ldstr " 35"
  call class [mscorlib]System.String [mscorlib]System.String::Concat(class [mscorlib]System.String, class [mscorlib]System.String)
  ret
REM:
  pop
  jmp class [mscorlib]System.String JitTest_compat_deep_array_nz.TestClass::Method34(int32[100...])
}
.method private hidebysig static class [mscorlib]System.String Method34(int32[100...]) il managed {
  .maxstack 8
  ldarg.0
  dup
  ldc.i4 100
  call instance int32 int32[100...]::Get(int32)
  ldc.i4 36
  rem
  brtrue.s REM
  call class [mscorlib]System.String JitTest_compat_deep_array_nz.TestClass::Method35(int32[100...])
  ldstr " 36"
  call class [mscorlib]System.String [mscorlib]System.String::Concat(class [mscorlib]System.String, class [mscorlib]System.String)
  ret
REM:
  tail. call class [mscorlib]System.String JitTest_compat_deep_array_nz.TestClass::Method35(int32[100...])
  ret
}
.method private hidebysig static class [mscorlib]System.String Method35(int32[100...]) il managed {
  .maxstack 8
  ldarg.0
  dup
  ldc.i4 100
  call instance int32 int32[100...]::Get(int32)
  ldc.i4 37
  rem
  brtrue.s REM
  call class [mscorlib]System.String JitTest_compat_deep_array_nz.TestClass::Method36(int32[100...])
  ldstr " 37"
  call class [mscorlib]System.String [mscorlib]System.String::Concat(class [mscorlib]System.String, class [mscorlib]System.String)
  ret
REM:
  pop
  jmp class [mscorlib]System.String JitTest_compat_deep_array_nz.TestClass::Method36(int32[100...])
}
.method private hidebysig static class [mscorlib]System.String Method36(int32[100...]) il managed {
  .maxstack 8
  ldarg.0
  dup
  ldc.i4 100
  call instance int32 int32[100...]::Get(int32)
  ldc.i4 38
  rem
  brtrue.s REM
  call class [mscorlib]System.String JitTest_compat_deep_array_nz.TestClass::Method37(int32[100...])
  ldstr " 38"
  call class [mscorlib]System.String [mscorlib]System.String::Concat(class [mscorlib]System.String, class [mscorlib]System.String)
  ret
REM:
  tail. call class [mscorlib]System.String JitTest_compat_deep_array_nz.TestClass::Method37(int32[100...])
  ret
}
.method private hidebysig static class [mscorlib]System.String Method37(int32[100...]) il managed {
  .maxstack 8
  ldarg.0
  dup
  ldc.i4 100
  call instance int32 int32[100...]::Get(int32)
  ldc.i4 39
  rem
  brtrue.s REM
  call class [mscorlib]System.String JitTest_compat_deep_array_nz.TestClass::Method38(int32[100...])
  ldstr " 39"
  call class [mscorlib]System.String [mscorlib]System.String::Concat(class [mscorlib]System.String, class [mscorlib]System.String)
  ret
REM:
  pop
  jmp class [mscorlib]System.String JitTest_compat_deep_array_nz.TestClass::Method38(int32[100...])
}
.method private hidebysig static class [mscorlib]System.String Method38(int32[100...]) il managed {
  .maxstack 8
  ldarg.0
  dup
  ldc.i4 100
  call instance int32 int32[100...]::Get(int32)
  ldc.i4 40
  rem
  brtrue.s REM
  call class [mscorlib]System.String JitTest_compat_deep_array_nz.TestClass::Method39(int32[100...])
  ldstr " 40"
  call class [mscorlib]System.String [mscorlib]System.String::Concat(class [mscorlib]System.String, class [mscorlib]System.String)
  ret
REM:
  tail. call class [mscorlib]System.String JitTest_compat_deep_array_nz.TestClass::Method39(int32[100...])
  ret
}
.method private hidebysig static class [mscorlib]System.String Method39(int32[100...]) il managed {
  .maxstack 8
  ldarg.0
  dup
  ldc.i4 100
  call instance int32 int32[100...]::Get(int32)
  ldc.i4 41
  rem
  brtrue.s REM
  call class [mscorlib]System.String JitTest_compat_deep_array_nz.TestClass::Method40(int32[100...])
  ldstr " 41"
  call class [mscorlib]System.String [mscorlib]System.String::Concat(class [mscorlib]System.String, class [mscorlib]System.String)
  ret
REM:
  pop
  jmp class [mscorlib]System.String JitTest_compat_deep_array_nz.TestClass::Method40(int32[100...])
}
.method private hidebysig static class [mscorlib]System.String Method40(int32[100...]) il managed {
  .maxstack 8
  ldarg.0
  dup
  ldc.i4 100
  call instance int32 int32[100...]::Get(int32)
  ldc.i4 42
  rem
  brtrue.s REM
  call class [mscorlib]System.String JitTest_compat_deep_array_nz.TestClass::Method41(int32[100...])
  ldstr " 42"
  call class [mscorlib]System.String [mscorlib]System.String::Concat(class [mscorlib]System.String, class [mscorlib]System.String)
  ret
REM:
  tail. call class [mscorlib]System.String JitTest_compat_deep_array_nz.TestClass::Method41(int32[100...])
  ret
}
.method private hidebysig static class [mscorlib]System.String Method41(int32[100...]) il managed {
  .maxstack 8
  ldarg.0
  dup
  ldc.i4 100
  call instance int32 int32[100...]::Get(int32)
  ldc.i4 43
  rem
  brtrue.s REM
  call class [mscorlib]System.String JitTest_compat_deep_array_nz.TestClass::Method42(int32[100...])
  ldstr " 43"
  call class [mscorlib]System.String [mscorlib]System.String::Concat(class [mscorlib]System.String, class [mscorlib]System.String)
  ret
REM:
  pop
  jmp class [mscorlib]System.String JitTest_compat_deep_array_nz.TestClass::Method42(int32[100...])
}
.method private hidebysig static class [mscorlib]System.String Method42(int32[100...]) il managed {
  .maxstack 8
  ldarg.0
  dup
  ldc.i4 100
  call instance int32 int32[100...]::Get(int32)
  ldc.i4 44
  rem
  brtrue.s REM
  call class [mscorlib]System.String JitTest_compat_deep_array_nz.TestClass::Method43(int32[100...])
  ldstr " 44"
  call class [mscorlib]System.String [mscorlib]System.String::Concat(class [mscorlib]System.String, class [mscorlib]System.String)
  ret
REM:
  tail. call class [mscorlib]System.String JitTest_compat_deep_array_nz.TestClass::Method43(int32[100...])
  ret
}
.method private hidebysig static class [mscorlib]System.String Method43(int32[100...]) il managed {
  .maxstack 8
  ldarg.0
  dup
  ldc.i4 100
  call instance int32 int32[100...]::Get(int32)
  ldc.i4 45
  rem
  brtrue.s REM
  call class [mscorlib]System.String JitTest_compat_deep_array_nz.TestClass::Method44(int32[100...])
  ldstr " 45"
  call class [mscorlib]System.String [mscorlib]System.String::Concat(class [mscorlib]System.String, class [mscorlib]System.String)
  ret
REM:
  pop
  jmp class [mscorlib]System.String JitTest_compat_deep_array_nz.TestClass::Method44(int32[100...])
}
.method private hidebysig static class [mscorlib]System.String Method44(int32[100...]) il managed {
  .maxstack 8
  ldarg.0
  dup
  ldc.i4 100
  call instance int32 int32[100...]::Get(int32)
  ldc.i4 46
  rem
  brtrue.s REM
  call class [mscorlib]System.String JitTest_compat_deep_array_nz.TestClass::Method45(int32[100...])
  ldstr " 46"
  call class [mscorlib]System.String [mscorlib]System.String::Concat(class [mscorlib]System.String, class [mscorlib]System.String)
  ret
REM:
  tail. call class [mscorlib]System.String JitTest_compat_deep_array_nz.TestClass::Method45(int32[100...])
  ret
}
.method private hidebysig static class [mscorlib]System.String Method45(int32[100...]) il managed {
  .maxstack 8
  ldarg.0
  dup
  ldc.i4 100
  call instance int32 int32[100...]::Get(int32)
  ldc.i4 47
  rem
  brtrue.s REM
  call class [mscorlib]System.String JitTest_compat_deep_array_nz.TestClass::Method46(int32[100...])
  ldstr " 47"
  call class [mscorlib]System.String [mscorlib]System.String::Concat(class [mscorlib]System.String, class [mscorlib]System.String)
  ret
REM:
  pop
  jmp class [mscorlib]System.String JitTest_compat_deep_array_nz.TestClass::Method46(int32[100...])
}
.method private hidebysig static class [mscorlib]System.String Method46(int32[100...]) il managed {
  .maxstack 8
  ldarg.0
  dup
  ldc.i4 100
  call instance int32 int32[100...]::Get(int32)
  ldc.i4 48
  rem
  brtrue.s REM
  call class [mscorlib]System.String JitTest_compat_deep_array_nz.TestClass::Method47(int32[100...])
  ldstr " 48"
  call class [mscorlib]System.String [mscorlib]System.String::Concat(class [mscorlib]System.String, class [mscorlib]System.String)
  ret
REM:
  tail. call class [mscorlib]System.String JitTest_compat_deep_array_nz.TestClass::Method47(int32[100...])
  ret
}
.method private hidebysig static class [mscorlib]System.String Method47(int32[100...]) il managed {
  .maxstack 8
  ldarg.0
  dup
  ldc.i4 100
  call instance int32 int32[100...]::Get(int32)
  ldc.i4 49
  rem
  brtrue.s REM
  call class [mscorlib]System.String JitTest_compat_deep_array_nz.TestClass::Method48(int32[100...])
  ldstr " 49"
  call class [mscorlib]System.String [mscorlib]System.String::Concat(class [mscorlib]System.String, class [mscorlib]System.String)
  ret
REM:
  pop
  jmp class [mscorlib]System.String JitTest_compat_deep_array_nz.TestClass::Method48(int32[100...])
}
.method private hidebysig static class [mscorlib]System.String Method48(int32[100...]) il managed {
  .maxstack 8
  ldarg.0
  dup
  ldc.i4 100
  call instance int32 int32[100...]::Get(int32)
  ldc.i4 50
  rem
  brtrue.s REM
  call class [mscorlib]System.String JitTest_compat_deep_array_nz.TestClass::Method49(int32[100...])
  ldstr " 50"
  call class [mscorlib]System.String [mscorlib]System.String::Concat(class [mscorlib]System.String, class [mscorlib]System.String)
  ret
REM:
  tail. call class [mscorlib]System.String JitTest_compat_deep_array_nz.TestClass::Method49(int32[100...])
  ret
}
.method private hidebysig static class [mscorlib]System.String Method49(int32[100...]) il managed {
  .maxstack 8
  ldarg.0
  dup
  ldc.i4 100
  call instance int32 int32[100...]::Get(int32)
  ldc.i4 51
  rem
  brtrue.s REM
  call class [mscorlib]System.String JitTest_compat_deep_array_nz.TestClass::Method50(int32[100...])
  ldstr " 51"
  call class [mscorlib]System.String [mscorlib]System.String::Concat(class [mscorlib]System.String, class [mscorlib]System.String)
  ret
REM:
  pop
  jmp class [mscorlib]System.String JitTest_compat_deep_array_nz.TestClass::Method50(int32[100...])
}
//-----END AUTO GENERATED CODE-----//

    .method private hidebysig static class System.String Method50(int32[100...]) il managed
    {
      .maxstack  8
      ldnull
      ret
    }

    .method public hidebysig static 
            int32 Main() il managed
    {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint
      // Code size       78 (0x4e)
      .maxstack  8
      .locals (int32[100...])
      ldc.i4 100
      ldc.i4.1
      newobj instance void int32[100...]::.ctor(int32, int32)
      stloc.0

      ldloc.0
      ldc.i4 100
      ldc.i4.2
      call instance void int32[100...]::Set(int32, int32)
      
IL_0007:
      ldloc.0
      call       class System.String JitTest_compat_deep_array_nz.TestClass::Method0(int32[100...])
      brtrue     IL_0027

      ldloc.0
      ldc.i4 100
      call instance int32 int32[100...]::Get(int32)
      call       void [System.Console]System.Console::Write(int32)
      ldstr      " - prime!"
      call       void [System.Console]System.Console::WriteLine(class System.String)
      
IL_0027:
      ldloc.0
      ldc.i4 100
      ldloc.0
      ldc.i4 100
      call instance int32 int32[100...]::Get(int32)
      ldc.i4.1
      add
      call instance void int32[100...]::Set(int32, int32)

      ldloc.0
      ldc.i4 100
      call instance int32 int32[100...]::Get(int32)
      ldc.i4     0x64
      ceq
      brfalse    IL_0007

      ldloc.0
      ldc.i4 100
      call instance int32 int32[100...]::Get(int32)
      ret
    } // end of method TestClass::Main

    .method public hidebysig specialname rtspecialname 
            instance void  .ctor(int32 A_0) il managed
    {
      // Code size       14 (0xe)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_000d:  ret
    } // end of method TestClass::.ctor

  } // end of class TestClass

} // end of namespace JitTest_compat_deep_array_nz

//*********** DISASSEMBLY COMPLETE ***********************
