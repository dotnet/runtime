// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.




.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}

.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 4:0:0:0
}
.assembly retbuf_bug
{


  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::.ctor() = ( 01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
                                                                                                             63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01 )       // ceptionThrows.
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.module retbuf_bug.exe
.custom instance void [mscorlib]System.Security.UnverifiableCodeAttribute::.ctor() = ( 01 00 00 00 ) 
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY



.class private sequential ansi sealed beforefieldinit RetBuff
       extends [mscorlib]System.ValueType
{
  .field public uint64 l1
  .field public uint64 l2
} // end of class RetBuff

.class private abstract auto ansi sealed beforefieldinit RetBufferBug
       extends [mscorlib]System.Object
{
  .field private static class [mscorlib]System.Threading.ManualResetEvent foundStack
  .field private static class [mscorlib]System.Threading.ManualResetEvent finishedTest
  .field private static int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) threadCount
  .field private static int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) result
  .method private hidebysig static void  ThreadProc() cil managed
  {
    .maxstack  3
    .locals init ([0] object o,
             [1] uint8* b)
    .language '{3F5162F8-07C6-11D3-9053-00C04FA302A1}', '{994B45C4-E6E9-11D2-903F-00C04FA302A1}', '{5A869D0B-6611-11D3-BD2A-0000F80849BD}'
    .line 39,39 : 3,27 'c:\\tests\\retbuf_bug.cs'
    IL_0000:  newobj     instance void [mscorlib]System.Object::.ctor()
    IL_0005:  stloc.0
    .line 40,40 : 3,33 ''
    IL_0006:  ldc.i4.s   24
    IL_0008:  conv.u
    IL_0009:  localloc
    IL_000b:  stloc.1
    .line 41,41 : 3,51 ''
    IL_000c:  ldloc.1
    IL_000d:  conv.i
    IL_0017:  ldloc.0
    IL_0018:  conv.i
    IL_001e:  ble.s      IL_0031

    .line 42,42 : 4,21 ''
    IL_0020:  ldsfld     class [mscorlib]System.Threading.ManualResetEvent RetBufferBug::foundStack
    IL_0025:  callvirt   instance bool [mscorlib]System.Threading.EventWaitHandle::Set()
    IL_002a:  pop
    .line 43,43 : 4,13 ''
    IL_002b:  call       void RetBufferBug::DoTest()
    .line 16707566,16707566 : 0,0 ''
    IL_0030:  ret

    .line 45,45 : 8,33 ''
    IL_0031:  volatile.
    IL_0033:  ldsfld     int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) RetBufferBug::threadCount
    IL_0038:  ldc.i4.1
    IL_0039:  add
    IL_003a:  dup
    IL_003b:  volatile.
    IL_003d:  stsfld     int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) RetBufferBug::threadCount
    IL_0042:  ldc.i4     0x3e8
    IL_0047:  ble.s      IL_0069

    .line 47,47 : 4,16 ''
    IL_0049:  ldc.i4.s   99
    IL_004b:  volatile.
    IL_004d:  stsfld     int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) RetBufferBug::result
    .line 48,48 : 4,21 ''
    IL_0052:  ldsfld     class [mscorlib]System.Threading.ManualResetEvent RetBufferBug::foundStack
    IL_0057:  callvirt   instance bool [mscorlib]System.Threading.EventWaitHandle::Set()
    IL_005c:  pop
    .line 49,49 : 4,23 ''
    IL_005d:  ldsfld     class [mscorlib]System.Threading.ManualResetEvent RetBufferBug::finishedTest
    IL_0062:  callvirt   instance bool [mscorlib]System.Threading.EventWaitHandle::Set()
    IL_0067:  pop
    .line 16707566,16707566 : 0,0 ''
    IL_0068:  ret

    .line 52,52 : 4,33 ''
    IL_0069:  volatile.
    IL_006b:  ldsfld     int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) RetBufferBug::threadCount
    IL_0070:  ldc.i4.s   15
    IL_0072:  and
    IL_0073:  ldc.i4.s   15
    IL_0075:  bne.un.s   IL_0081

    .line 53,53 : 5,24 ''
    IL_0077:  ldstr      "."
    IL_007c:  call       void [System.Console]System.Console::Write(string)
    .line 54,54 : 4,35 ''
    IL_0081:  ldnull
    IL_0082:  ldftn      void RetBufferBug::ThreadProc()
    IL_0088:  newobj     instance void [mscorlib]System.Threading.ThreadStart::.ctor(object,
                                                                                     native int)
    IL_008d:  newobj     instance void [mscorlib]System.Threading.Thread::.ctor(class [mscorlib]System.Threading.ThreadStart)
    IL_0092:  call       instance void [mscorlib]System.Threading.Thread::Start()
    .line 57,57 : 4,25 ''
    IL_0097:  ldsfld     class [mscorlib]System.Threading.ManualResetEvent RetBufferBug::foundStack
    IL_009c:  callvirt   instance bool [mscorlib]System.Threading.WaitHandle::WaitOne()
    IL_00a1:  pop
    .line 59,59 : 3,10 ''
    IL_00a2:  ret
  } // end of method RetBufferBug::ThreadProc

  .method private hidebysig static int32 
          Main() cil managed
  {
    .entrypoint
    .maxstack  8
    .line 63,63 : 3,16 ''
    IL_0000:  call       void RetBufferBug::ThreadProc()
    .line 64,64 : 3,26 ''
    IL_0005:  ldsfld     class [mscorlib]System.Threading.ManualResetEvent RetBufferBug::finishedTest
    IL_000a:  callvirt   instance bool [mscorlib]System.Threading.WaitHandle::WaitOne()
    IL_000f:  pop
    .line 65,65 : 3,17 ''
    IL_0010:  volatile.
    IL_0012:  ldsfld     int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) RetBufferBug::result
    IL_0017:  ret
  } // end of method RetBufferBug::Main

  .method private hidebysig static void  DoTest() cil managed
  {
    .maxstack  3
    .locals init ([0] valuetype RetBuff rb)
    .line 69,69 : 3,96 ''
    IL_0000:  ldstr      "\nIt took {0} thread(s) to find a stack above the G"
    + "C heap."
    IL_0005:  volatile.
    IL_0007:  ldsfld     int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) RetBufferBug::threadCount
    IL_000c:  box        [mscorlib]System.Int32
    IL_0011:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object)
    .line 70,70 : 3,89 ''
    IL_0016:  ldtoken    RetBufferBug
    IL_001b:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_0020:  ldstr      "TailCaller"
    IL_0025:  call       instance class [mscorlib]System.Reflection.MethodInfo [mscorlib]System.Type::GetMethod(string)
    IL_002a:  ldnull
    IL_002b:  ldnull
    IL_002c:  callvirt   instance object [mscorlib]System.Reflection.MethodBase::Invoke(object,
                                                                                        object[])
    IL_0031:  unbox.any  RetBuff
    IL_0036:  stloc.0
    .line 71,71 : 3,75 ''
    IL_0037:  ldstr      "RetBuff.l1 = {0:X}, RetBuff.l2 = {1:X}"
    IL_003c:  ldloca.s   rb
    IL_003e:  ldfld      uint64 RetBuff::l1
    IL_0043:  box        [mscorlib]System.UInt64
    IL_0048:  ldloca.s   rb
    IL_004a:  ldfld      uint64 RetBuff::l2
    IL_004f:  box        [mscorlib]System.UInt64
    IL_0054:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    .line 72,72 : 3,63 ''
    IL_0059:  ldloca.s   rb
    IL_005b:  ldfld      uint64 RetBuff::l1
    IL_0060:  ldc.i4     0xbaadf00d
    IL_0065:  conv.u8
    IL_0066:  bne.un.s   IL_008f

    IL_0068:  ldloca.s   rb
    IL_006a:  ldfld      uint64 RetBuff::l2
    IL_006f:  ldc.i8     0x123456789abcdef
    IL_0078:  bne.un.s   IL_008f

    .line 73,73 : 4,17 ''
    IL_007a:  ldc.i4.s   100
    IL_007c:  volatile.
    IL_007e:  stsfld     int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) RetBufferBug::result
    .line 74,74 : 4,30 ''
    IL_0083:  ldstr      "PASS"
    IL_0088:  call       void [System.Console]System.Console::WriteLine(string)
    .line 16707566,16707566 : 0,0 ''
    IL_008d:  br.s       IL_00a2

    .line 77,77 : 4,16 ''
    IL_008f:  ldc.i4.s   99
    IL_0091:  volatile.
    IL_0093:  stsfld     int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) RetBufferBug::result
    .line 78,78 : 4,30 ''
    IL_0098:  ldstr      "Fail"
    IL_009d:  call       void [System.Console]System.Console::WriteLine(string)
    .line 80,80 : 3,22 ''
    IL_00a2:  ldsfld     class [mscorlib]System.Threading.ManualResetEvent RetBufferBug::finishedTest
    IL_00a7:  callvirt   instance bool [mscorlib]System.Threading.EventWaitHandle::Set()
    IL_00ac:  pop
    .line 81,81 : 2,3 ''
    IL_00ad:  ret
  } // end of method RetBufferBug::DoTest

  .method public hidebysig static valuetype RetBuff 
          TailCaller() cil managed
  {
    .maxstack  8
    .line 84,84 : 3,38 ''
    IL_0000:  ldstr      "In TailCaller"
    IL_0005:  call       void [System.Console]System.Console::WriteLine(string)
    .line 85,85 : 3,65 ''
    IL_000a:  ldc.i4     0xbaadf00d
    IL_000f:  ldc.i4     0x123
    IL_0014:  ldc.i4     0x4567
    IL_0019:  ldc.i4     0x89ab
    IL_001e:  ldc.i4     0xcdef
    IL_0023:  tail. call valuetype RetBuff RetBufferBug::TailCallee(uint32,
                                                                    uint16,
                                                                    uint16,
                                                                    uint16,
                                                                    uint16)
    IL_0028:  ret
  } // end of method RetBufferBug::TailCaller

  .method private hidebysig static valuetype RetBuff 
          TailCallee(uint32 i1,
                     uint16 s1,
                     uint16 s2,
                     uint16 s3,
                     uint16 s4) cil managed noinlining
  {
    .maxstack  4
    .locals init ([0] valuetype RetBuff result)
    .line 90,90 : 3,18 ''
    IL_0000:  ldloca.s   result
    IL_0002:  ldarg.0
    IL_0003:  conv.u8
    IL_0004:  stfld      uint64 RetBuff::l1
    .line 91,91 : 3,85 ''
    IL_0009:  ldloca.s   result
    IL_000b:  ldarg.1
    IL_000c:  conv.u8
    IL_000d:  ldc.i4.s   48
    IL_000f:  shl
    IL_0010:  ldarg.2
    IL_0011:  conv.u8
    IL_0012:  ldc.i4.s   32
    IL_0014:  shl
    IL_0015:  or
    IL_0016:  ldarg.3
    IL_0017:  conv.u8
    IL_0018:  ldc.i4.s   16
    IL_001a:  shl
    IL_001b:  or
    IL_001c:  ldarg.s    s4
    IL_001e:  conv.u8
    IL_001f:  or
    IL_0020:  stfld      uint64 RetBuff::l2
    .line 92,92 : 3,93 ''
    IL_0025:  ldstr      "Returning RetBuff.l1 = {0:X}, RetBuff.l2 = {1:X}"
    IL_002a:  ldloca.s   result
    IL_002c:  ldfld      uint64 RetBuff::l1
    IL_0031:  box        [mscorlib]System.UInt64
    IL_0036:  ldloca.s   result
    IL_0038:  ldfld      uint64 RetBuff::l2
    IL_003d:  box        [mscorlib]System.UInt64
    IL_0042:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    .line 93,93 : 3,17 ''
    IL_0047:  ldloc.0
    IL_0048:  ret
  } // end of method RetBufferBug::TailCallee

  .method private hidebysig specialname rtspecialname static 
          void  .cctor() cil managed
  {
    .maxstack  8
    .line 33,33 : 2,67 ''
    IL_0000:  ldc.i4.0
    IL_0001:  newobj     instance void [mscorlib]System.Threading.ManualResetEvent::.ctor(bool)
    IL_0006:  stsfld     class [mscorlib]System.Threading.ManualResetEvent RetBufferBug::foundStack
    .line 34,34 : 2,69 ''
    IL_000b:  ldc.i4.0
    IL_000c:  newobj     instance void [mscorlib]System.Threading.ManualResetEvent::.ctor(bool)
    IL_0011:  stsfld     class [mscorlib]System.Threading.ManualResetEvent RetBufferBug::finishedTest
    .line 35,35 : 2,38 ''
    IL_0016:  ldc.i4.0
    IL_0017:  volatile.
    IL_0019:  stsfld     int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) RetBufferBug::threadCount
    .line 36,36 : 2,33 ''
    IL_001e:  ldc.i4.0
    IL_001f:  volatile.
    IL_0021:  stsfld     int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) RetBufferBug::result
    .line 16707566,16707566 : 0,0 ''
    IL_0026:  ret
  } // end of method RetBufferBug::.cctor

} // end of class RetBufferBug



