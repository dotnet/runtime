// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console
{
}
.assembly extern System.Runtime
{
}
.assembly Runtime_40607
{
}
.module Runtime_40607.dll

.class private auto ansi beforefieldinit Runtime_40607.Program extends [System.Runtime]System.Object
{
  .method private hidebysig static int32 DependsOnUnInitValue(bool valueToWrite) cil managed noinlining
  {
    .maxstack  8
    .locals (int32 V_0, bool V_1)

    IL_0000:  ldc.i4.1
    IL_0001:  stloc.0
    IL_0002:  br.s       IL_000b

    IL_0004:  ldloc.1
    IL_0005:  brfalse.s  IL_0011

    IL_0007:  ldc.i4.0
    IL_0008:  stloc.0
    IL_0009:  br.s       IL_0011

    IL_000b:  ldloca.s   V_1
    IL_000d:  ldarg.0
    IL_000e:  stind.i1
    IL_000f:  br.s       IL_0004

    IL_0011:  ldloc.0
    IL_0012:  ret
  }

  .method private hidebysig static int32 DependsOnUnInitValue(int8 valueToWrite) cil managed noinlining
  {
    .maxstack  8
    .locals (int32 V_0, int8 V_1)

    IL_0000:  ldc.i4.1
    IL_0001:  stloc.0
    IL_0002:  br.s       IL_000b

    IL_0004:  ldloc.1
    IL_0005:  brfalse.s  IL_0011

    IL_0007:  ldc.i4.0
    IL_0008:  stloc.0
    IL_0009:  br.s       IL_0011

    IL_000b:  ldloca.s   V_1
    IL_000d:  ldarg.0
    IL_000e:  stind.i1
    IL_000f:  br.s       IL_0004

    IL_0011:  ldloc.0
    IL_0012:  ret
  }

  .method private hidebysig static int32 DependsOnUnInitValue(uint8 valueToWrite) cil managed noinlining
  {
    .maxstack  8
    .locals (int32 V_0, uint8 V_1)

    IL_0000:  ldc.i4.1
    IL_0001:  stloc.0
    IL_0002:  br.s       IL_000b

    IL_0004:  ldloc.1
    IL_0005:  brfalse.s  IL_0011

    IL_0007:  ldc.i4.0
    IL_0008:  stloc.0
    IL_0009:  br.s       IL_0011

    IL_000b:  ldloca.s   V_1
    IL_000d:  ldarg.0
    IL_000e:  stind.i1
    IL_000f:  br.s       IL_0004

    IL_0011:  ldloc.0
    IL_0012:  ret
  }

  .method private hidebysig static int32 DependsOnUnInitValue(int16 valueToWrite) cil managed noinlining
  {
    .maxstack  8
    .locals (int32 V_0, int16 V_1)

    IL_0000:  ldc.i4.1
    IL_0001:  stloc.0
    IL_0002:  br.s       IL_000b

    IL_0004:  ldloc.1
    IL_0005:  brfalse.s  IL_0011

    IL_0007:  ldc.i4.0
    IL_0008:  stloc.0
    IL_0009:  br.s       IL_0011

    IL_000b:  ldloca.s   V_1
    IL_000d:  ldarg.0
    IL_000e:  stind.i2
    IL_000f:  br.s       IL_0004

    IL_0011:  ldloc.0
    IL_0012:  ret
  }

  .method private hidebysig static int32 DependsOnUnInitValue(uint16 valueToWrite) cil managed noinlining
  {
    .maxstack  8
    .locals (int32 V_0, uint16 V_1)

    IL_0000:  ldc.i4.1
    IL_0001:  stloc.0
    IL_0002:  br.s       IL_000b

    IL_0004:  ldloc.1
    IL_0005:  brfalse.s  IL_0011

    IL_0007:  ldc.i4.0
    IL_0008:  stloc.0
    IL_0009:  br.s       IL_0011

    IL_000b:  ldloca.s   V_1
    IL_000d:  ldarg.0
    IL_000e:  stind.i2
    IL_000f:  br.s       IL_0004

    IL_0011:  ldloc.0
    IL_0012:  ret
  }

  .method private hidebysig static int32 DependsOnUnInitValue(int32 valueToWrite) cil managed noinlining
  {
    .maxstack  8
    .locals (int32 V_0, int32 V_1)

    IL_0000:  ldc.i4.1
    IL_0001:  stloc.0
    IL_0002:  br.s       IL_000b

    IL_0004:  ldloc.1
    IL_0005:  brfalse.s  IL_0011

    IL_0007:  ldc.i4.0
    IL_0008:  stloc.0
    IL_0009:  br.s       IL_0011

    IL_000b:  ldloca.s   V_1
    IL_000d:  ldarg.0
    IL_000e:  stind.i4
    IL_000f:  br.s       IL_0004

    IL_0011:  ldloc.0
    IL_0012:  ret
  }

  .method private hidebysig static int32 DependsOnUnInitValue(uint32 valueToWrite) cil managed noinlining
  {
    .maxstack  8
    .locals (int32 V_0, uint32 V_1)

    IL_0000:  ldc.i4.1
    IL_0001:  stloc.0
    IL_0002:  br.s       IL_000b

    IL_0004:  ldloc.1
    IL_0005:  brfalse.s  IL_0011

    IL_0007:  ldc.i4.0
    IL_0008:  stloc.0
    IL_0009:  br.s       IL_0011

    IL_000b:  ldloca.s   V_1
    IL_000d:  ldarg.0
    IL_000e:  stind.i4
    IL_000f:  br.s       IL_0004

    IL_0011:  ldloc.0
    IL_0012:  ret
  }


  .method private hidebysig static int32 PoisonStackWith(int32 valueToWrite) cil managed noinlining
  {
    .maxstack  8
    .locals (int32 V_0, int32 V_1)

    IL_0000:  ldc.i4.1
    IL_0001:  stloc.0
    IL_0002:  br.s       IL_000b

    IL_0004:  ldloc.1
    IL_0005:  brfalse.s  IL_0014

    IL_0007:  ldc.i4.0
    IL_0008:  stloc.0
    IL_0009:  br.s       IL_0014

    IL_000b:  ldloca.s   V_1
    IL_000d:  conv.u
    IL_000e:  ldc.i4.0
    IL_000f:  add
    IL_0010:  ldarg.0
    IL_0011:  stind.i4
    IL_0012:  br.s       IL_0004

    IL_0014:  ldloc.0
    IL_0015:  ret
  }

  .method private hidebysig static int32 Main(string[] args) cil managed
  {
    .entrypoint
    // Code size       75 (0x4b)
    .maxstack  2
    .locals init (int32 V_0)

    ldc.i4.s   100
    stloc.0
    ldc.i4     0xdeadbeef
    call       int32 Runtime_40607.Program::PoisonStackWith(int32)
    pop

    ldc.i4.0
    call       int32 Runtime_40607.Program::DependsOnUnInitValue(bool)
    ldc.i4.1
    beq.s      IL_0000

    ldstr      "DependsOnUnInitValue(bool) has failed"
    call       void [System.Console]System.Console::WriteLine(string)

    ldc.i4.0
    stloc.0

IL_0000:
    ldc.i4     0xdeadbeef
    call       int32 Runtime_40607.Program::PoisonStackWith(int32)
    pop

    ldc.i4.0
    call       int32 Runtime_40607.Program::DependsOnUnInitValue(int8)
    ldc.i4.1
    beq.s      IL_0001

    ldstr      "DependsOnUnInitValue(int8) has failed"
    call       void [System.Console]System.Console::WriteLine(string)

    ldc.i4.0
    stloc.0

IL_0001:
    ldc.i4     0xdeadbeef
    call       int32 Runtime_40607.Program::PoisonStackWith(int32)
    pop

    ldc.i4.0
    call       int32 Runtime_40607.Program::DependsOnUnInitValue(uint8)
    ldc.i4.1
    beq.s      IL_0002

    ldstr      "DependsOnUnInitValue(uint8) has failed"
    call       void [System.Console]System.Console::WriteLine(string)

    ldc.i4.0
    stloc.0

IL_0002:
    ldc.i4     0xdeadbeef
    call       int32 Runtime_40607.Program::PoisonStackWith(int32)
    pop

    ldc.i4.0
    call       int32 Runtime_40607.Program::DependsOnUnInitValue(int16)
    ldc.i4.1
    beq.s      IL_0003

    ldstr      "DependsOnUnInitValue(int16) has failed"
    call       void [System.Console]System.Console::WriteLine(string)

    ldc.i4.0
    stloc.0

IL_0003:
    ldc.i4     0xdeadbeef
    call       int32 Runtime_40607.Program::PoisonStackWith(int32)
    pop

    ldc.i4.0
    call       int32 Runtime_40607.Program::DependsOnUnInitValue(uint16)
    ldc.i4.1
    beq.s      IL_0004

    ldstr      "DependsOnUnInitValue(uint16) has failed"
    call       void [System.Console]System.Console::WriteLine(string)

    ldc.i4.0
    stloc.0

IL_0004:
    ldc.i4     0xdeadbeef
    call       int32 Runtime_40607.Program::PoisonStackWith(int32)
    pop

    ldc.i4.0
    call       int32 Runtime_40607.Program::DependsOnUnInitValue(int32)
    ldc.i4.1
    beq.s      IL_0005

    ldstr      "DependsOnUnInitValue(int32) has failed"
    call       void [System.Console]System.Console::WriteLine(string)

    ldc.i4.0
    stloc.0

IL_0005:
    ldc.i4     0xdeadbeef
    call       int32 Runtime_40607.Program::PoisonStackWith(int32)
    pop

    ldc.i4.0
    call       int32 Runtime_40607.Program::DependsOnUnInitValue(uint32)
    ldc.i4.1
    beq.s      IL_0006

    ldstr      "DependsOnUnInitValue(uint32) has failed"
    call       void [System.Console]System.Console::WriteLine(string)

    ldc.i4.0
    stloc.0

IL_0006:
    ldloc.0
    ret
  }
}
