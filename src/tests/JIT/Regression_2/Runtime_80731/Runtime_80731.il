// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }
.assembly extern xunit.core {}
.assembly extern System.Console {}
.assembly Runtime_80731 {}

.class public auto ansi beforefieldinit Runtime_80731
       extends [System.Runtime]System.Object
{
  .class sequential ansi sealed nested private beforefieldinit S16
         extends [System.Runtime]System.ValueType
  {
    .field public int32 A
    .field public int32 B
    .field public int32 C
    .field public int32 D
  } // end of class S16

  .method public hidebysig static int32 
          Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       99 (0x63)
    .maxstack  2
    .locals init (int32 V_0,
             int32 V_1,
             int32 V_2,
             valuetype Runtime_80731/S16 V_3)
    IL_0000:  ldc.i4.s   100
    IL_0002:  stloc.0
    IL_0003:  ldloca.s   V_3
    IL_0005:  initobj    Runtime_80731/S16
    IL_000b:  ldloca.s   V_3
    IL_000d:  ldc.i4     0x162e
    IL_0012:  stfld      int32 Runtime_80731/S16::A
    IL_0017:  ldloc.3
    IL_0018:  call       int32 Runtime_80731::ImplicitByref(valuetype Runtime_80731/S16)
    IL_001d:  stloc.1
    IL_001e:  ldloc.1
    IL_001f:  ldc.i4     0x162e
    IL_0024:  beq.s      IL_003a

    IL_0026:  ldstr      "FAIL: ImplicitByref returned {0}"
    IL_002b:  ldloc.1
    IL_002c:  box        [System.Runtime]System.Int32
    IL_0031:  call       void [System.Console]System.Console::WriteLine(string,
                                                                        object)
    IL_0036:  ldloc.0
    IL_0037:  ldc.i4.1
    IL_0038:  or
    IL_0039:  stloc.0
    IL_003a:  ldc.i4     0x4d2
    IL_003f:  call       int32 Runtime_80731::ForwardSub(int32)
    IL_0044:  stloc.2
    IL_0045:  ldloc.2
    IL_0046:  ldc.i4     0x162e
    IL_004b:  beq.s      IL_0061

    IL_004d:  ldstr      "FAIL: ForwardSub returned {0}"
    IL_0052:  ldloc.2
    IL_0053:  box        [System.Runtime]System.Int32
    IL_0058:  call       void [System.Console]System.Console::WriteLine(string,
                                                                        object)
    IL_005d:  ldloc.0
    IL_005e:  ldc.i4.2
    IL_005f:  or
    IL_0060:  stloc.0
    IL_0061:  ldloc.0
    IL_0062:  ret
  } // end of method Runtime_80731::Main

  .method private hidebysig static int32 
          ImplicitByref(valuetype Runtime_80731/S16 s) cil managed noinlining
  {
    // Code size       11 (0xb)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       void Runtime_80731::Modify(valuetype Runtime_80731/S16)
    IL_0006:  jmp        int32 Runtime_80731::ImplicitByrefCallee(valuetype Runtime_80731/S16)
  } // end of method Runtime_80731::ImplicitByref

  .method private hidebysig static void  Modify(valuetype Runtime_80731/S16 s) cil managed noinlining
  {
    // Code size       19 (0x13)
    .maxstack  8
    IL_0000:  ldarga.s   s
    IL_0002:  ldc.i4     0x4d2
    IL_0007:  stfld      int32 Runtime_80731/S16::A
    IL_000c:  ldarg.0
    IL_000d:  call       void Runtime_80731::Consume<valuetype Runtime_80731/S16>(!!0)
    IL_0012:  ret
  } // end of method Runtime_80731::Modify

  .method private hidebysig static void  Consume<T>(!!T val) cil managed noinlining
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Runtime_80731::Consume

  .method private hidebysig static int32 
          ImplicitByrefCallee(valuetype Runtime_80731/S16 s) cil managed noinlining
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldfld      int32 Runtime_80731/S16::A
    IL_0006:  ret
  } // end of method Runtime_80731::ImplicitByrefCallee

  .method private hidebysig static int32 
          ForwardSub(int32 a) cil managed noinlining
  {
    // Code size       20 (0x14)
    .maxstack  8
    IL_0000:  ldc.i4     0x162e
    IL_0005:  starg      a
    IL_0009:  ldarg.0
    IL_000a:  call       void Runtime_80731::Consume<int32>(!!0)
    IL_000f:  jmp        int32 Runtime_80731::ForwardSubCallee(int32)
  } // end of method Runtime_80731::ForwardSub

  .method private hidebysig static int32 
          ForwardSubCallee(int32 a) cil managed noinlining
  {
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ret
  } // end of method Runtime_80731::ForwardSubCallee

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [System.Runtime]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Runtime_80731::.ctor

} // end of class Runtime_80731

// =============================================================

// *********** DISASSEMBLY COMPLETE ***********************
