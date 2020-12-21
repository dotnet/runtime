// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib
{
}

.assembly extern System.Console as console
{
}

.assembly boxing001 
{
}

.class private sequential ansi sealed beforefieldinit V extends [mscorlib] System.ValueType
{
  .field public int32 m_i
}

.class public auto ansi beforefieldinit Z extends [mscorlib] System.Object
{

.field static int32 nFailures

.method static int32 Assert(int32 line, string f) cil managed
{
  .locals (int32 V_0)
   ldstr      "ASSERTION FAILED AT LINE"
   call       void [console]System.Console::Write(string)
   ldarg.0
   call       void [console]System.Console::Write(int32)
   ldstr      ": '"
   call       void [console]System.Console::Write(string)
   ldarg.1
   call       void [console]System.Console::Write(string)
   ldstr      "'"
   call       void [console]System.Console::WriteLine(string)
   ldsfld     int32 Z::nFailures
   ldc.i4.1
   add
   stsfld     int32 Z::nFailures
   ldsfld     int32 Z::nFailures
   stloc.0
   ldloc.0
   ret
}

.method public static int32 Main() cil managed
{
  .entrypoint
  .locals (int32 V_0,
           object V1,
           valuetype V V_2,
           valuetype V& V_3,
           object V_4,
           object V_5,
           object V_6,
           int32 V_7,
           int32 V_8,
           object V_9,
           object V_10,
           object V_11,
           object V_12,
           string V_13,
           string V_14,
           valuetype V& V_15,
           valuetype V V_16,
           valuetype V V_17)
  ldc.i4.0
  stloc.s    V_7
  ldnull
  stloc.1
  ldnull
  stloc.s    V_4
  ldnull
  stloc.s    V_14
  ldnull
  stloc.s    V_13
  ldnull
  stloc.s    V_6
  ldnull
  stloc.s    V_5
  ldnull
  stloc.s    V_12
  ldnull
  stloc.s    V_11
  ldloca.s   V_2
  initobj    V
  ldloca.s   V_2
  ldc.i4.5
  stfld      int32 V::m_i
  ldloc.2
  box        V
  stloc.1
  ldloc.1
  unbox      V
  ldfld      int32 V::m_i
  stloc.0
  ldloc.0
  ldc.i4.5
  beq.s      T1

  ldc.i4.s   50
  ldstr      "i == 5"
  call       int32 Z::Assert(int32, string)
  pop

T1:

  ldloc.1
  unbox      V
  stloc.3
  ldloc.3
  ldfld      int32 V::m_i
  ldc.i4.5
  beq.s      T2

  ldc.i4.s   53
  ldstr      "pv->m_i == 5"
  call       int32 Z::Assert(int32, string)
  pop

T2:

  ldloc.3
  ldfld      int32 V::m_i
  ldc.i4.5
  beq.s      T3

  ldc.i4.s   55
  ldstr      "(*pv).m_i == 5"
  call       int32 Z::Assert(int32, string)
  pop

T3:

  ldloc.3
  ldobj      V
  stloc.s    V_17
  ldloca.s   V_17
  ldc.i4.s   10
  stfld      int32 V::m_i
  ldloca.s   V_17
  ldfld      int32 V::m_i
  ldc.i4.s   10
  beq.s      T4

  ldc.i4.s   60
  ldstr      "v3.m_i == 10"
  call       int32 Z::Assert(int32, string)
  pop

T4:

  ldloc.3
  ldfld      int32 V::m_i
  ldc.i4.5
  beq.s      T5

  ldc.i4.s   61
  ldstr      "(*pv).m_i == 5"
  call       int32 Z::Assert(int32, string)
  pop

T5:

  ldloca.s   V_17
  stloc.3
  ldloc.3
  ldfld      int32 V::m_i
  ldc.i4.s   10
  beq.s      T6

  ldc.i4.s   64
  ldstr      "(*pv).m_i == 10"
  call       int32 Z::Assert(int32, string)
  pop

T6:

  ldloc.1
  unbox      V
  ldobj      V
  stloc.s    V_16
  ldloca.s   V_16
  ldfld      int32 V::m_i
  ldc.i4.5
  beq.s      T7

  ldc.i4.s   68
  ldstr      "v2.m_i == 5"
  call       int32 Z::Assert(int32, string)
  pop

T7:

  ldloc.1
  unbox      V
  ldc.i4.s   10
  stfld      int32 V::m_i
  ldloc.1
  unbox      V
  ldfld      int32 V::m_i
  stloc.0
  ldloc.0
  ldc.i4.s   10
  beq.s      T8

  ldc.i4.s   72
  ldstr      "i == 10"
  call       int32 Z::Assert(int32, string)
  pop

T8:

  ldloc.1
  unbox      V
  ldc.i4.s   123
  stfld      int32 V::m_i
  ldloc.1
  unbox      V
  ldobj      V
  stloc.2
  ldloca.s   V_2
  ldfld      int32 V::m_i
  ldc.i4.s   123
  beq.s      T9

  ldc.i4.s   76
  ldstr      "v.m_i == 123"
  call       int32 Z::Assert(int32, string)
  pop

T9:

  ldloc.2
  box        V
  stloc.s    V_4
  ldloc.s    V_4
  stloc.1
  ldloc.1
  unbox      V
  ldfld      int32 V::m_i
  stloc.0
  ldloc.0
  ldc.i4.s   123
  beq.s      T10

  ldc.i4.s   82
  ldstr      "i == 123"
  call       int32 Z::Assert(int32, string)
  pop

T10:

  ldstr      "V"
  stloc.s    V_14
  ldloc.1
  callvirt   instance string [mscorlib]System.ValueType::ToString()
  stloc.s    V_13
  ldloc.s    V_14
  ldloc.s    V_13
  callvirt   instance bool [mscorlib]System.String::Equals(string)
  brtrue.s   T11

  ldc.i4.s   86
  ldstr      "str1->Equals( str2 )"
  call       int32 Z::Assert(int32, string)
  pop

T11:

  ldloc.2
  box        V
  stloc.s    V_4
  ldloc.s    V_4
  stloc.s    V_10
  ldloc.s    V_10
  isinst     V
  brfalse.s  X0

  ldloc.s    V_10
  unbox      V
  br.s       X1

X0:

  ldc.i4.0
  conv.i

X1:

  stloc.s    V_15
  ldloc.s    V_15
  ldobj      V
  stloc.2
  ldloc.s    V_4
  stloc.s    V_9
  ldloc.s    V_9
  isinst     V
  brfalse.s  X2

  ldloc.s    V_9
  unbox      V
  br.s       X3

X2:

  ldc.i4.0
  conv.i

X3:

  ldobj      V
  stloc.2
  ldloc.1
  unbox      V
  ldobj      V
  box        V
  unbox      V
  ldobj      V
  box        V
  unbox      V
  ldobj      V
  stloc.2
  ldloca.s   V_2
  ldfld      int32 V::m_i
  stloc.0
  ldloc.1
  unbox      V
  ldobj      V
  box        V
  unbox      V
  ldobj      V
  box        V
  unbox      V
  ldobj      V
  box        V
  unbox      V
  ldobj      V
  box        V
  stloc.1
  ldloc.0
  ldloc.1
  unbox      V
  ldfld      int32 V::m_i
  sub
  stloc.0
  ldloc.0
  brfalse.s  T12

  ldc.i4.s   101
  ldstr      "i == 0"
  call       int32 Z::Assert(int32, string)
  pop

T12:
 
  ldc.i4.5
  stloc.s    V_8
  ldc.i4.0
  stloc.0
  ldc.i4.5
  box        [mscorlib]System.Int32
  stloc.s    V_6
  ldloc.0
  ldloc.s    V_6
  unbox      [mscorlib]System.Int32
  ldind.i4
  add
  stloc.0
  ldc.i4.5
  box        [mscorlib]System.Int32
  stloc.s    V_5
  ldloc.0
  ldloc.s    V_5
  unbox      [mscorlib]System.Int32
  ldind.i4
  add
  stloc.0
  ldloc.s    V_8
  box        [mscorlib]System.Int32
  stloc.s    V_12
  ldloc.0
  ldloc.s    V_12
  unbox      [mscorlib]System.Int32
  ldind.i4
  add
  stloc.0
  ldloc.s    V_8
  box        [mscorlib]System.Int32
  stloc.s    V_11
  ldloc.0
  ldloc.s    V_11
  unbox      [mscorlib]System.Int32
  ldind.i4
  add
  stloc.0
  ldloc.0
  ldc.i4.s   20
  beq.s      T13

  ldc.i4.s   114
  ldstr      "i == 5+5+5+5"
  call       int32 Z::Assert(int32, string)
  pop

T13:

  ldloc.s    V_5
  stloc.s    V_6
  ldsfld     int32 Z::nFailures
  brtrue.s   Fail

  ldstr      "PASSED"
  call       void [console]System.Console::WriteLine(string)
  ldc.i4.s   100
  stloc.s    V_7
  br.s       Done

Fail:

  ldstr      "FAILED"
  call       void [console]System.Console::WriteLine(string)
  ldsfld     int32 Z::nFailures
  stloc.s    V_7

Done:

  ldloc.s    V_7
  ret
}

}

