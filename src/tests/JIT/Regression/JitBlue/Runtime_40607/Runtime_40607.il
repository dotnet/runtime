// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console
{
}
.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }
.assembly Runtime_40607
{
}
.assembly extern xunit.core {}

.class public auto ansi beforefieldinit Runtime_40607.Program extends [System.Runtime]System.Object
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

  .method private hidebysig static int32 SignExtendsWrittenValue(int8 valueToWrite) cil managed noinlining
  {
    .maxstack  8
    .locals (int8 V_0)

    IL_0000:  br.s       IL_0007

    IL_0002:  ldloca.s   V_0
    IL_0004:  ldind.i1
    IL_0005:  br.s       IL_000d

    IL_0007:  ldloca.s   V_0
    IL_0009:  ldarg.0
    IL_000a:  stind.i1
    IL_000b:  br.s       IL_0002

    IL_000d:  ret
  }

  .method private hidebysig static int32 ZeroExtendsWrittenValue(int8 valueToWrite) cil managed noinlining
  {
    .maxstack  8
    .locals (int8 V_0)

    IL_0000:  br.s       IL_0007

    IL_0002:  ldloca.s   V_0
    IL_0004:  ldind.u1
    IL_0005:  br.s       IL_000d

    IL_0007:  ldloca.s   V_0
    IL_0009:  ldarg.0
    IL_000a:  stind.i1
    IL_000b:  br.s       IL_0002

    IL_000d:  ret
  }

  .method private hidebysig static int32 SignExtendsWrittenValue(uint8 valueToWrite) cil managed noinlining
  {
    .maxstack  8
    .locals (uint8 V_0)

    IL_0000:  br.s       IL_0007

    IL_0002:  ldloca.s   V_0
    IL_0004:  ldind.i1
    IL_0005:  br.s       IL_000d

    IL_0007:  ldloca.s   V_0
    IL_0009:  ldarg.0
    IL_000a:  stind.i1
    IL_000b:  br.s       IL_0002

    IL_000d:  ret
  }

  .method private hidebysig static int32 ZeroExtendsWrittenValue(uint8 valueToWrite) cil managed noinlining
  {
    .maxstack  8
    .locals (uint8 V_0)

    IL_0000:  br.s       IL_0007

    IL_0002:  ldloca.s   V_0
    IL_0004:  ldind.u1
    IL_0005:  br.s       IL_000d

    IL_0007:  ldloca.s   V_0
    IL_0009:  ldarg.0
    IL_000a:  stind.i1
    IL_000b:  br.s       IL_0002

    IL_000d:  ret
  }

  .method private hidebysig static int32 SignExtendsWrittenValue(int16 valueToWrite) cil managed noinlining
  {
    .maxstack  8
    .locals (int16 V_0)

    IL_0000:  br.s       IL_0007

    IL_0002:  ldloca.s   V_0
    IL_0004:  ldind.i2
    IL_0005:  br.s       IL_000d

    IL_0007:  ldloca.s   V_0
    IL_0009:  ldarg.0
    IL_000a:  stind.i2
    IL_000b:  br.s       IL_0002

    IL_000d:  ret
  }

  .method private hidebysig static int32 ZeroExtendsWrittenValue(int16 valueToWrite) cil managed noinlining
  {
    .maxstack  8
    .locals (int16 V_0)

    IL_0000:  br.s       IL_0007

    IL_0002:  ldloca.s   V_0
    IL_0004:  ldind.u2
    IL_0005:  br.s       IL_000d

    IL_0007:  ldloca.s   V_0
    IL_0009:  ldarg.0
    IL_000a:  stind.i2
    IL_000b:  br.s       IL_0002

    IL_000d:  ret
  }

  .method private hidebysig static int32 SignExtendsWrittenValue(uint16 valueToWrite) cil managed noinlining
  {
    .maxstack  8
    .locals (uint16 V_0)

    IL_0000:  br.s       IL_0007

    IL_0002:  ldloca.s   V_0
    IL_0004:  ldind.i2
    IL_0005:  br.s       IL_000d

    IL_0007:  ldloca.s   V_0
    IL_0009:  ldarg.0
    IL_000a:  stind.i2
    IL_000b:  br.s       IL_0002

    IL_000d:  ret
  }

  .method private hidebysig static int32 ZeroExtendsWrittenValue(uint16 valueToWrite) cil managed noinlining
  {
    .maxstack  8
    .locals (uint16 V_0)

    IL_0000:  br.s       IL_0007

    IL_0002:  ldloca.s   V_0
    IL_0004:  ldind.u2
    IL_0005:  br.s       IL_000d

    IL_0007:  ldloca.s   V_0
    IL_0009:  ldarg.0
    IL_000a:  stind.i2
    IL_000b:  br.s       IL_0002

    IL_000d:  ret
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

  .method public hidebysig static int32 Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    .maxstack  2
    .locals init (int32 V_0)

    ldc.i4.s   100
    stloc.0
// BEGIN_DependsOnUnInitValue_bool
    ldc.i4     0xdeadbeef
    call       int32 Runtime_40607.Program::PoisonStackWith(int32)
    pop

    ldc.i4.0
    call       int32 Runtime_40607.Program::DependsOnUnInitValue(bool)
    brtrue.s   END_DependsOnUnInitValue_bool

    ldstr      "DependsOnUnInitValue(bool) has failed"
    call       void [System.Console]System.Console::WriteLine(string)

    ldc.i4.0
    stloc.0
END_DependsOnUnInitValue_bool:

// BEGIN_DependsOnUnInitValue_int8
    ldc.i4     0xdeadbeef
    call       int32 Runtime_40607.Program::PoisonStackWith(int32)
    pop

    ldc.i4.0
    call       int32 Runtime_40607.Program::DependsOnUnInitValue(int8)
    brtrue.s   END_DependsOnUnInitValue_int8

    ldstr      "DependsOnUnInitValue(int8) has failed"
    call       void [System.Console]System.Console::WriteLine(string)

    ldc.i4.0
    stloc.0
END_DependsOnUnInitValue_int8:

// BEGIN_DependsOnUnInitValue_uint8
    ldc.i4     0xdeadbeef
    call       int32 Runtime_40607.Program::PoisonStackWith(int32)
    pop

    ldc.i4.0
    call       int32 Runtime_40607.Program::DependsOnUnInitValue(uint8)
    brtrue.s   END_DependsOnUnInitValue_uint8

    ldstr      "DependsOnUnInitValue(uint8) has failed"
    call       void [System.Console]System.Console::WriteLine(string)

    ldc.i4.0
    stloc.0
END_DependsOnUnInitValue_uint8:

// BEGIN_DependsOnUnInitValue_int16
    ldc.i4     0xdeadbeef
    call       int32 Runtime_40607.Program::PoisonStackWith(int32)
    pop

    ldc.i4.0
    call       int32 Runtime_40607.Program::DependsOnUnInitValue(int16)
    brtrue.s   END_DependsOnUnInitValue_int16

    ldstr      "DependsOnUnInitValue(int16) has failed"
    call       void [System.Console]System.Console::WriteLine(string)

    ldc.i4.0
    stloc.0
END_DependsOnUnInitValue_int16:

// BEGIN_DependsOnUnInitValue_uint16
    ldc.i4     0xdeadbeef
    call       int32 Runtime_40607.Program::PoisonStackWith(int32)
    pop

    ldc.i4.0
    call       int32 Runtime_40607.Program::DependsOnUnInitValue(uint16)
    brtrue.s   END_DependsOnUnInitValue_uint16

    ldstr      "DependsOnUnInitValue(uint16) has failed"
    call       void [System.Console]System.Console::WriteLine(string)

    ldc.i4.0
    stloc.0
END_DependsOnUnInitValue_uint16:

// BEGIN_DependsOnUnInitValue_int32
    ldc.i4     0xdeadbeef
    call       int32 Runtime_40607.Program::PoisonStackWith(int32)
    pop

    ldc.i4.0
    call       int32 Runtime_40607.Program::DependsOnUnInitValue(int32)
    brtrue.s   END_DependsOnUnInitValue_int32

    ldstr      "DependsOnUnInitValue(int32) has failed"
    call       void [System.Console]System.Console::WriteLine(string)

    ldc.i4.0
    stloc.0
END_DependsOnUnInitValue_int32:

// BEGIN_DependsOnUnInitValue_uint32
    ldc.i4     0xdeadbeef
    call       int32 Runtime_40607.Program::PoisonStackWith(int32)
    pop

    ldc.i4.0
    call       int32 Runtime_40607.Program::DependsOnUnInitValue(uint32)
    brtrue.s   END_DependsOnUnInitValue_uint32

    ldstr      "DependsOnUnInitValue(uint32) has failed"
    call       void [System.Console]System.Console::WriteLine(string)

    ldc.i4.0
    stloc.0
END_DependsOnUnInitValue_uint32:

// BEGIN_SignExtendsWrittenValue_int8_0x7f:
    ldc.i4     0x7f
    call       int32 Runtime_40607.Program::SignExtendsWrittenValue(int8)
    ldc.i4     0x7f
    beq.s      END_SignExtendsWrittenValue_int8_0x7f

    ldstr      "SignExtendsWrittenValue(int8) has failed"
    call       void [System.Console]System.Console::WriteLine(string)

    ldc.i4.0
    stloc.0
END_SignExtendsWrittenValue_int8_0x7f:

// BEGIN_ZeroExtendsWrittenValue_int8_0x7f:
    ldc.i4     0x7f
    call       int32 Runtime_40607.Program::ZeroExtendsWrittenValue(int8)
    ldc.i4     0x7f
    beq.s      END_ZeroExtendsWrittenValue_int8_0x7f

    ldstr      "ZeroExtendsWrittenValue(int8) has failed"
    call       void [System.Console]System.Console::WriteLine(string)

    ldc.i4.0
    stloc.0
END_ZeroExtendsWrittenValue_int8_0x7f:

// BEGIN_SignExtendsWrittenValue_int8_0x80:
    ldc.i4     0x80
    call       int32 Runtime_40607.Program::SignExtendsWrittenValue(int8)
    ldc.i4     0xffffff80
    beq.s      END_SignExtendsWrittenValue_int8_0x80

    ldstr      "SignExtendsWrittenValue(int8) has failed"
    call       void [System.Console]System.Console::WriteLine(string)

    ldc.i4.0
    stloc.0
END_SignExtendsWrittenValue_int8_0x80:

// BEGIN_ZeroExtendsWrittenValue_int8_0x80:
    ldc.i4     0x80
    call       int32 Runtime_40607.Program::ZeroExtendsWrittenValue(int8)
    ldc.i4     0x80
    beq.s      END_ZeroExtendsWrittenValue_int8_0x80

    ldstr      "ZeroExtendsWrittenValue(int8) has failed"
    call       void [System.Console]System.Console::WriteLine(string)

    ldc.i4.0
    stloc.0
END_ZeroExtendsWrittenValue_int8_0x80:

// BEGIN_SignExtendsWrittenValue_uint8_0x7f:
    ldc.i4     0x7f
    call       int32 Runtime_40607.Program::SignExtendsWrittenValue(uint8)
    ldc.i4     0x7f
    beq.s      END_SignExtendsWrittenValue_uint8_0x7f

    ldstr      "SignExtendsWrittenValue(uint8) has failed"
    call       void [System.Console]System.Console::WriteLine(string)

    ldc.i4.0
    stloc.0
END_SignExtendsWrittenValue_uint8_0x7f:

// BEGIN_ZeroExtendsWrittenValue_uint8_0x7f:
    ldc.i4     0x7f
    call       int32 Runtime_40607.Program::ZeroExtendsWrittenValue(uint8)
    ldc.i4     0x7f
    beq.s      END_ZeroExtendsWrittenValue_uint8_0x7f

    ldstr      "ZeroExtendsWrittenValue(uint8) has failed"
    call       void [System.Console]System.Console::WriteLine(string)

    ldc.i4.0
    stloc.0
END_ZeroExtendsWrittenValue_uint8_0x7f:

// BEGIN_SignExtendsWrittenValue_uint8_0x80:
    ldc.i4     0x80
    call       int32 Runtime_40607.Program::SignExtendsWrittenValue(uint8)
    ldc.i4     0xffffff80
    beq.s      END_SignExtendsWrittenValue_uint8_0x80

    ldstr      "SignExtendsWrittenValue(uint8) has failed"
    call       void [System.Console]System.Console::WriteLine(string)

    ldc.i4.0
    stloc.0
END_SignExtendsWrittenValue_uint8_0x80:

// BEGIN_ZeroExtendsWrittenValue_uint8_0x80:
    ldc.i4     0x80
    call       int32 Runtime_40607.Program::ZeroExtendsWrittenValue(uint8)
    ldc.i4     0x80
    beq.s      END_ZeroExtendsWrittenValue_uint8_0x80

    ldstr      "ZeroExtendsWrittenValue(uint8) has failed"
    call       void [System.Console]System.Console::WriteLine(string)

    ldc.i4.0
    stloc.0
END_ZeroExtendsWrittenValue_uint8_0x80:

// BEGIN_SignExtendsWrittenValue_int16_0x7fff:
    ldc.i4     0x7fff
    call       int32 Runtime_40607.Program::SignExtendsWrittenValue(int16)
    ldc.i4     0x7fff
    beq.s      END_SignExtendsWrittenValue_int16_0x7fff

    ldstr      "SignExtendsWrittenValue(int16) has failed"
    call       void [System.Console]System.Console::WriteLine(string)

    ldc.i4.0
    stloc.0
END_SignExtendsWrittenValue_int16_0x7fff:

// BEGIN_ZeroExtendsWrittenValue_int16_0x7fff:
    ldc.i4     0x7fff
    call       int32 Runtime_40607.Program::ZeroExtendsWrittenValue(int16)
    ldc.i4     0x7fff
    beq.s      END_ZeroExtendsWrittenValue_int16_0x7fff

    ldstr      "ZeroExtendsWrittenValue(int16) has failed"
    call       void [System.Console]System.Console::WriteLine(string)

    ldc.i4.0
    stloc.0
END_ZeroExtendsWrittenValue_int16_0x7fff:

// BEGIN_SignExtendsWrittenValue_int16_0x8000:
    ldc.i4     0x8000
    call       int32 Runtime_40607.Program::SignExtendsWrittenValue(int16)
    ldc.i4     0xffff8000
    beq.s      END_SignExtendsWrittenValue_int16_0x8000

    ldstr      "SignExtendsWrittenValue(int16) has failed"
    call       void [System.Console]System.Console::WriteLine(string)

    ldc.i4.0
    stloc.0
END_SignExtendsWrittenValue_int16_0x8000:

// BEGIN_ZeroExtendsWrittenValue_int16_0x8000:
    ldc.i4     0x8000
    call       int32 Runtime_40607.Program::ZeroExtendsWrittenValue(int16)
    ldc.i4     0x8000
    beq.s      END_ZeroExtendsWrittenValue_int16_0x8000

    ldstr      "ZeroExtendsWrittenValue(int16) has failed"
    call       void [System.Console]System.Console::WriteLine(string)

    ldc.i4.0
    stloc.0
END_ZeroExtendsWrittenValue_int16_0x8000:

// BEGIN_SignExtendsWrittenValue_uint16_0x7fff:
    ldc.i4     0x7fff
    call       int32 Runtime_40607.Program::SignExtendsWrittenValue(uint16)
    ldc.i4     0x7fff
    beq.s      END_SignExtendsWrittenValue_uint16_0x7fff

    ldstr      "SignExtendsWrittenValue(uint16) has failed"
    call       void [System.Console]System.Console::WriteLine(string)

    ldc.i4.0
    stloc.0
END_SignExtendsWrittenValue_uint16_0x7fff:

// BEGIN_ZeroExtendsWrittenValue_uint16_0x7fff:
    ldc.i4     0x7fff
    call       int32 Runtime_40607.Program::ZeroExtendsWrittenValue(uint16)
    ldc.i4     0x7fff
    beq.s      END_ZeroExtendsWrittenValue_uint16_0x7fff

    ldstr      "ZeroExtendsWrittenValue(uint16) has failed"
    call       void [System.Console]System.Console::WriteLine(string)

    ldc.i4.0
    stloc.0
END_ZeroExtendsWrittenValue_uint16_0x7fff:

// BEGIN_SignExtendsWrittenValue_uint16_0x8000:
    ldc.i4     0x8000
    call       int32 Runtime_40607.Program::SignExtendsWrittenValue(uint16)
    ldc.i4     0xffff8000
    beq.s      END_SignExtendsWrittenValue_uint16_0x8000

    ldstr      "SignExtendsWrittenValue(uint16) has failed"
    call       void [System.Console]System.Console::WriteLine(string)

    ldc.i4.0
    stloc.0
END_SignExtendsWrittenValue_uint16_0x8000:

// BEGIN_ZeroExtendsWrittenValue_uint16_0x8000:
    ldc.i4     0x8000
    call       int32 Runtime_40607.Program::ZeroExtendsWrittenValue(uint16)
    ldc.i4     0x8000
    beq.s      END_ZeroExtendsWrittenValue_uint16_0x8000

    ldstr      "ZeroExtendsWrittenValue(uint16) has failed"
    call       void [System.Console]System.Console::WriteLine(string)

    ldc.i4.0
    stloc.0
END_ZeroExtendsWrittenValue_uint16_0x8000:

    ldloc.0
    ret
  }
}

