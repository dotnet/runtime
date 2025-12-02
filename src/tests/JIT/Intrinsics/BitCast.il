// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

// This test checks implicit floating point and small type casts in IL

.assembly extern System.Runtime {}
.assembly extern xunit.core {}
.assembly extern System.Console {}
.assembly Bitcast {}

.class public abstract auto ansi sealed beforefieldinit Program
       extends [System.Runtime]System.Object
{
  .field private static int32 exitCode
  .method public hidebysig static int32  Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       306 (0x132)
    .maxstack  3
    .locals init (uint64 V_0,
             int8* V_1,
             int16* V_2,
             int32* V_3)
    IL_0000:  ldc.i8     0x5555555555555555
    IL_0009:  stloc.0
    IL_000a:  ldloca.s   V_0
    IL_000c:  conv.u
    IL_000d:  ldc.i4.3
    IL_000e:  add
    IL_000f:  stloc.1
    IL_0010:  ldloc.1
    IL_0011:  ldc.i4.m1
    IL_0012:  stind.i1
    IL_0013:  ldc.i4     0xff
    IL_0018:  conv.i8
    IL_0019:  ldloc.1
    IL_001a:  call       !!0* Program::NoInline<int8>(!!0*)
    IL_001f:  ldind.i1
    IL_0020:  call       !!1 [System.Runtime]System.Runtime.CompilerServices.Unsafe::BitCast<uint8,uint8>(!!0)
    IL_0025:  conv.u8
    IL_0026:  ldc.i4.s   18
    IL_0028:  call       void Program::Verify(int64,
                                              int64,
                                              int32)
    IL_002d:  ldc.i4.m1
    IL_002e:  conv.i8
    IL_002f:  ldloc.1
    IL_0030:  call       !!0* Program::NoInline<uint8>(!!0*)
    IL_0035:  ldind.u1
    IL_0036:  call       !!1 [System.Runtime]System.Runtime.CompilerServices.Unsafe::BitCast<int8,int8>(!!0)
    IL_003b:  conv.i8
    IL_003c:  ldc.i4.s   19
    IL_003e:  call       void Program::Verify(int64,
                                              int64,
                                              int32)
    IL_0043:  ldloca.s   V_0
    IL_0045:  conv.u
    IL_0046:  ldc.i4.2
    IL_0047:  add
    IL_0048:  stloc.2
    IL_0049:  ldloc.2
    IL_004a:  ldc.i4.m1
    IL_004b:  stind.i2
    IL_004c:  ldc.i4     0xffff
    IL_0051:  conv.i8
    IL_0052:  ldloc.2
    IL_0053:  call       !!0* Program::NoInline<int16>(!!0*)
    IL_0058:  ldind.i2
    IL_0059:  call       !!1 [System.Runtime]System.Runtime.CompilerServices.Unsafe::BitCast<uint16,uint16>(!!0)
    IL_005e:  conv.u8
    IL_005f:  ldc.i4.s   23
    IL_0061:  call       void Program::Verify(int64,
                                              int64,
                                              int32)
    IL_0066:  ldc.i4.m1
    IL_0067:  conv.i8
    IL_0068:  ldloc.2
    IL_0069:  call       !!0* Program::NoInline<uint16>(!!0*)
    IL_006e:  ldind.u2
    IL_006f:  call       !!1 [System.Runtime]System.Runtime.CompilerServices.Unsafe::BitCast<int16,int16>(!!0)
    IL_0074:  conv.i8
    IL_0075:  ldc.i4.s   24
    IL_0077:  call       void Program::Verify(int64,
                                              int64,
                                              int32)
    IL_007c:  ldloc.2
    IL_007d:  stloc.3
    IL_007e:  ldloc.3
    IL_007f:  ldc.i4.m1
    IL_0080:  unaligned. 2
    IL_0083:  stind.i4
    IL_0084:  ldc.i4.m1
    IL_0085:  conv.u8
    IL_0086:  ldloc.3
    IL_0087:  call       !!0* Program::NoInline<int32>(!!0*)
    IL_008c:  unaligned. 2
    IL_008f:  ldind.i4
    IL_0090:  call       !!1 [System.Runtime]System.Runtime.CompilerServices.Unsafe::BitCast<uint32,uint32>(!!0)
    IL_0095:  conv.u8
    IL_0096:  ldc.i4.s   28
    IL_0098:  call       void Program::Verify(int64,
                                              int64,
                                              int32)
    IL_009d:  ldc.i4.m1
    IL_009e:  conv.i8
    IL_009f:  ldloc.3
    IL_00a0:  call       !!0* Program::NoInline<uint32>(!!0*)
    IL_00a5:  unaligned. 2
    IL_00a8:  ldind.u4
    IL_00a9:  call       !!1 [System.Runtime]System.Runtime.CompilerServices.Unsafe::BitCast<int32,int32>(!!0)
    IL_00ae:  conv.i8
    IL_00af:  ldc.i4.s   29
    IL_00b1:  call       void Program::Verify(int64,
                                              int64,
                                              int32)
    IL_00b6:  ldc.i4     0x3f800000
    IL_00bb:  conv.i8
    IL_00bc:  ldc.r8     1.
    IL_00c5:  call       !!1 [System.Runtime]System.Runtime.CompilerServices.Unsafe::BitCast<float32,int32>(!!0)
    IL_00ca:  conv.i8
    IL_00cb:  ldc.i4.s   31
    IL_00cd:  call       void Program::Verify(int64,
                                              int64,
                                              int32)
    IL_00d2:  ldc.i8     0x3ff0000000000000
    IL_00db:  ldc.r4     1.
    IL_00e0:  call       !!1 [System.Runtime]System.Runtime.CompilerServices.Unsafe::BitCast<float64,int64>(!!0)
    IL_00e5:  ldc.i4.s   32
    IL_00e7:  call       void Program::Verify(int64,
                                              int64,
                                              int32)
    IL_00ec:  ldc.i4     0x3f800000
    IL_00f1:  conv.i8
    IL_00f2:  ldc.r8     1.
    IL_00fb:  call       !!0 Program::NoInline<float64>(!!0)
    IL_0100:  call       !!1 [System.Runtime]System.Runtime.CompilerServices.Unsafe::BitCast<float32,int32>(!!0)
    IL_0105:  conv.i8
    IL_0106:  ldc.i4.s   33
    IL_0108:  call       void Program::Verify(int64,
                                              int64,
                                              int32)
    IL_010d:  ldc.i8     0x3ff0000000000000
    IL_0116:  ldc.r4     1.
    IL_011b:  call       !!0 Program::NoInline<float32>(!!0)
    IL_0120:  call       !!1 [System.Runtime]System.Runtime.CompilerServices.Unsafe::BitCast<float64,int64>(!!0)
    IL_0125:  ldc.i4.s   34
    IL_0127:  call       void Program::Verify(int64,
                                              int64,
                                              int32)
    IL_012c:  ldsfld     int32 Program::exitCode
    IL_0131:  ret
  } // end of method Program::Main

  .method private hidebysig static void  Verify(int64 expected,
                                                int64 actual,
                                                [opt] int32 line) cil managed noinlining
  {
    .param [3] = int32(0x00000000)
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.CallerLineNumberAttribute::.ctor() = ( 01 00 00 00 )
    // Code size       100 (0x64)
    .maxstack  3
    .locals init (valuetype [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler V_0)
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  bne.un.s   IL_0011

    IL_0004:  ldsfld     int32 Program::exitCode
    IL_0009:  ldc.i4.1
    IL_000a:  sub
    IL_000b:  stsfld     int32 Program::exitCode
    IL_0010:  ret

    IL_0011:  ldloca.s   V_0
    IL_0013:  ldc.i4.s   32
    IL_0015:  ldc.i4.3
    IL_0016:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::.ctor(int32,
                                                                                                                               int32)
    IL_001b:  ldloca.s   V_0
    IL_001d:  ldstr      "Failed at line "
    IL_0022:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendLiteral(string)
    IL_0027:  ldloca.s   V_0
    IL_0029:  ldarg.2
    IL_002a:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendFormatted<int32>(!!0)
    IL_002f:  ldloca.s   V_0
    IL_0031:  ldstr      ", expected "
    IL_0036:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendLiteral(string)
    IL_003b:  ldloca.s   V_0
    IL_003d:  ldarg.0
    IL_003e:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendFormatted<int64>(!!0)
    IL_0043:  ldloca.s   V_0
    IL_0045:  ldstr      ", got "
    IL_004a:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendLiteral(string)
    IL_004f:  ldloca.s   V_0
    IL_0051:  ldarg.1
    IL_0052:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendFormatted<int64>(!!0)
    IL_0057:  ldloca.s   V_0
    IL_0059:  call       instance string [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::ToStringAndClear()
    IL_005e:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0063:  ret
  } // end of method Program::Verify

  .method private hidebysig static !!T  NoInline<T>(!!T 'value') cil managed noinlining
  {
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ret
  } // end of method Program::NoInline

  .method private hidebysig static !!T*  NoInline<T>(!!T* 'value') cil managed noinlining
  {
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ret
  } // end of method Program::NoInline

  .method private hidebysig specialname rtspecialname static
          void  .cctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldc.i4.s   110
    IL_0002:  stsfld     int32 Program::exitCode
    IL_0007:  ret
  } // end of method Program::.cctor

} // end of class Program
