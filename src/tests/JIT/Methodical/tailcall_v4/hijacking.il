// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.



// Metadata version: v4.0.AMD64chk
.assembly extern System.Runtime
{
    .ver 4:0:0:0
    .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A)
}
.assembly extern System.Console
{
    .ver 4:0:0:0
    .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A)
}
.assembly extern System.Threading
{
    .ver 4:0:0:0
    .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A)
}
.assembly extern System.Threading.Thread
{
    .ver 4:0:0:0
    .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A)
}
.assembly extern mscorlib { }
.assembly hijacking
{
  .custom instance void [System.Runtime]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
  .custom instance void [System.Runtime]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::.ctor() = ( 01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
                                                                                                             63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01 )       // ceptionThrows.
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.assembly extern xunit.core {}
// MVID: {7A6D7819-76F7-4311-967C-7CC804775914}
.custom instance void [mscorlib]System.Security.UnverifiableCodeAttribute::.ctor() = ( 01 00 00 00 ) 
.imagebase 0x0000000000400000
.file alignment 0x00000200
.stackreserve 0x0000000000400000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x0000000002910000


// =============== CLASS MEMBERS DECLARATION ===================

.namespace Test_hijacking_il
{
.class private sequential ansi sealed beforefieldinit ForceHelper
       extends [System.Runtime]System.ValueType
{
  .field public valuetype Test_hijacking_il.ForceHelper* me1
  .field public valuetype Test_hijacking_il.ForceHelper* me2
  .field public int64 counter
} // end of class Test_hijacking_il.ForceHelper

.class public auto ansi beforefieldinit Repro
       extends [System.Runtime]System.Object
{
  .field private bool modreq([System.Runtime]System.Runtime.CompilerServices.IsVolatile) stop
  .field private bool failed
  .field private int64 counts
  .field private int64 gc
  .method private hidebysig instance void 
          TailHelper1(valuetype Test_hijacking_il.ForceHelper h) cil managed
  {
    // Code size       123 (0x7b)
    .maxstack  3
    IL_0000:  ldarga.s   h
    IL_0002:  dup
    IL_0003:  ldfld      int64 Test_hijacking_il.ForceHelper::counter
    IL_0008:  ldc.i4.1
    IL_0009:  conv.i8
    IL_000a:  add
    IL_000b:  stfld      int64 Test_hijacking_il.ForceHelper::counter
    IL_0010:  ldarg.0
    IL_0011:  volatile.
    IL_0013:  ldfld      bool modreq([System.Runtime]System.Runtime.CompilerServices.IsVolatile) Test_hijacking_il.Repro::stop
    IL_0018:  brfalse.s  IL_002e

    IL_001a:  ldarg.0
    IL_001b:  ldflda     int64 Test_hijacking_il.Repro::counts
    IL_0020:  ldarga.s   h
    IL_0022:  ldfld      int64 Test_hijacking_il.ForceHelper::counter
    IL_0027:  call       int64 [System.Threading]System.Threading.Interlocked::Add(int64&,
                                                                           int64)
    IL_002c:  pop
    IL_002d:  ret

    IL_002e:  ldarga.s   h
    IL_0030:  conv.u
    IL_0031:  ldarga.s   h
    IL_0033:  ldfld      valuetype Test_hijacking_il.ForceHelper* Test_hijacking_il.ForceHelper::me1
    IL_0038:  beq.s      IL_0073

    IL_003a:  ldarga.s   h
    IL_003c:  ldfld      int64 Test_hijacking_il.ForceHelper::counter
    IL_0041:  ldc.i4.3
    IL_0042:  conv.i8
    IL_0043:  ble.s      IL_0069

    IL_0045:  ldarg.0
    IL_0046:  ldc.i4.1
    IL_0047:  stfld      bool Test_hijacking_il.Repro::failed
    IL_004c:  ldarg.0
    IL_004d:  ldc.i4.1
    IL_004e:  volatile.
    IL_0050:  stfld      bool modreq([System.Runtime]System.Runtime.CompilerServices.IsVolatile) Test_hijacking_il.Repro::stop
    IL_0055:  ldarg.0
    IL_0056:  ldflda     int64 Test_hijacking_il.Repro::counts
    IL_005b:  ldarga.s   h
    IL_005d:  ldfld      int64 Test_hijacking_il.ForceHelper::counter
    IL_0062:  call       int64 [System.Threading]System.Threading.Interlocked::Add(int64&,
                                                                           int64)
    IL_0067:  pop
    IL_0068:  ret

    IL_0069:  ldarga.s   h
    IL_006b:  ldarga.s   h
    IL_006d:  conv.u
    IL_006e:  stfld      valuetype Test_hijacking_il.ForceHelper* Test_hijacking_il.ForceHelper::me1
    IL_0073:  ldarg.0
    IL_0074:  ldarg.1
    IL_0075:  ldarg.1
    IL_0076:  tail. call       instance void Test_hijacking_il.Repro::TailHelper2(valuetype Test_hijacking_il.ForceHelper, valuetype Test_hijacking_il.ForceHelper)
    IL_007b:  ret
  } // end of method Test_hijacking_il.Repro::TailHelper1

  .method private hidebysig instance void 
          TailHelper2(valuetype Test_hijacking_il.ForceHelper h, valuetype Test_hijacking_il.ForceHelper h2) cil managed
  {
    // Code size       123 (0x7b)
    .maxstack  3
    IL_0000:  ldarga.s   h
    IL_0002:  dup
    IL_0003:  ldfld      int64 Test_hijacking_il.ForceHelper::counter
    IL_0008:  ldc.i4.1
    IL_0009:  conv.i8
    IL_000a:  add
    IL_000b:  stfld      int64 Test_hijacking_il.ForceHelper::counter
    IL_0010:  ldarg.0
    IL_0011:  volatile.
    IL_0013:  ldfld      bool modreq([System.Runtime]System.Runtime.CompilerServices.IsVolatile) Test_hijacking_il.Repro::stop
    IL_0018:  brfalse.s  IL_002e

    IL_001a:  ldarg.0
    IL_001b:  ldflda     int64 Test_hijacking_il.Repro::counts
    IL_0020:  ldarga.s   h
    IL_0022:  ldfld      int64 Test_hijacking_il.ForceHelper::counter
    IL_0027:  call       int64 [System.Threading]System.Threading.Interlocked::Add(int64&,
                                                                           int64)
    IL_002c:  pop
    IL_002d:  ret

    IL_002e:  ldarga.s   h
    IL_0030:  conv.u
    IL_0031:  ldarga.s   h
    IL_0033:  ldfld      valuetype Test_hijacking_il.ForceHelper* Test_hijacking_il.ForceHelper::me2
    IL_0038:  beq.s      IL_0073

    IL_003a:  ldarga.s   h
    IL_003c:  ldfld      int64 Test_hijacking_il.ForceHelper::counter
    IL_0041:  ldc.i4.3
    IL_0042:  conv.i8
    IL_0043:  ble.s      IL_0069

    IL_0045:  ldarg.0
    IL_0046:  ldc.i4.1
    IL_0047:  stfld      bool Test_hijacking_il.Repro::failed
    IL_004c:  ldarg.0
    IL_004d:  ldc.i4.1
    IL_004e:  volatile.
    IL_0050:  stfld      bool modreq([System.Runtime]System.Runtime.CompilerServices.IsVolatile) Test_hijacking_il.Repro::stop
    IL_0055:  ldarg.0
    IL_0056:  ldflda     int64 Test_hijacking_il.Repro::counts
    IL_005b:  ldarga.s   h
    IL_005d:  ldfld      int64 Test_hijacking_il.ForceHelper::counter
    IL_0062:  call       int64 [System.Threading]System.Threading.Interlocked::Add(int64&,
                                                                           int64)
    IL_0067:  pop
    IL_0068:  ret

    IL_0069:  ldarga.s   h
    IL_006b:  ldarga.s   h
    IL_006d:  conv.u
    IL_006e:  stfld      valuetype Test_hijacking_il.ForceHelper* Test_hijacking_il.ForceHelper::me2
    IL_0073:  ldarg.0
    IL_0074:  ldarg.1
    IL_0075:  tail. call       instance void Test_hijacking_il.Repro::TailHelper1(valuetype Test_hijacking_il.ForceHelper)
    IL_007a:  ret
  } // end of method Test_hijacking_il.Repro::TailHelper2

  .method private hidebysig instance void 
          TailCallThread() cil managed
  {
    // Code size       16 (0x10)
    .maxstack  2
    .locals init (valuetype Test_hijacking_il.ForceHelper V_0)
    IL_0000:  ldloca.s   V_0
    IL_0002:  initobj    Test_hijacking_il.ForceHelper
    IL_0008:  ldarg.0
    IL_0009:  ldloc.0
    IL_000a:  tail. call       instance void Test_hijacking_il.Repro::TailHelper1(valuetype Test_hijacking_il.ForceHelper)
    IL_000f:  ret
  } // end of method Test_hijacking_il.Repro::TailCallThread

  .method private hidebysig instance void 
          Collector() cil managed
  {
    // Code size       33 (0x21)
    .maxstack  8
    IL_0000:  br.s       IL_0016

    IL_0002:  call       void [System.Runtime]System.GC::Collect()
    IL_0007:  ldarg.0
    IL_0008:  dup
    IL_0009:  ldfld      int64 Test_hijacking_il.Repro::gc
    IL_000e:  ldc.i4.1
    IL_000f:  conv.i8
    IL_0010:  add
    IL_0011:  stfld      int64 Test_hijacking_il.Repro::gc
              ldc.i4.1
              call       void [System.Threading.Thread]System.Threading.Thread::Sleep(int32)
    IL_0016:  ldarg.0
    IL_0017:  volatile.
    IL_0019:  ldfld      bool modreq([System.Runtime]System.Runtime.CompilerServices.IsVolatile) Test_hijacking_il.Repro::stop
    IL_001e:  brfalse.s  IL_0002

    IL_0020:  ret
  } // end of method Test_hijacking_il.Repro::Collector

  .method public hidebysig static int32 
          Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       300 (0x12c)
    .maxstack  5
    .locals init (class Test_hijacking_il.Repro V_0,
             class [System.Threading.Thread]System.Threading.Thread[] V_1,
             class [System.Threading.Thread]System.Threading.Thread V_2,
             int32 V_3,
             class [System.Threading.Thread]System.Threading.Thread V_4,
             class [System.Threading.Thread]System.Threading.Thread[] V_5,
             class [System.Threading.Thread]System.Threading.Thread[] V_6,
             int32 V_7,
             class [System.Threading.Thread]System.Threading.Thread[] V_8,
             int32 V_9)
    IL_0000:  newobj     instance void Test_hijacking_il.Repro::.ctor()
    IL_0005:  stloc.0
    IL_0006:  ldc.i4.2
    IL_0007:  newarr     [System.Threading.Thread]System.Threading.Thread
    IL_000c:  stloc.s    V_5
    IL_000e:  ldloc.s    V_5
    IL_0010:  ldc.i4.0
    IL_0011:  ldloc.0
    IL_0012:  ldftn      instance void Test_hijacking_il.Repro::Collector()
    IL_0018:  newobj     instance void [System.Threading.Thread]System.Threading.ThreadStart::.ctor(object,
                                                                                     native int)
    IL_001d:  newobj     instance void [System.Threading.Thread]System.Threading.Thread::.ctor(class [System.Threading.Thread]System.Threading.ThreadStart)
    IL_0022:  stelem.ref
    IL_0023:  ldloc.s    V_5
    IL_0025:  ldc.i4.1
    IL_0026:  ldloc.0
    IL_0027:  ldftn      instance void Test_hijacking_il.Repro::TailCallThread()
    IL_002d:  newobj     instance void [System.Threading.Thread]System.Threading.ThreadStart::.ctor(object,
                                                                                     native int)
    IL_0032:  newobj     instance void [System.Threading.Thread]System.Threading.Thread::.ctor(class [System.Threading.Thread]System.Threading.ThreadStart)
    IL_0037:  stelem.ref
    IL_0062:  ldloc.s    V_5
    IL_0064:  stloc.1
    IL_0065:  ldloc.1
    IL_0066:  stloc.s    V_6
    IL_0068:  ldc.i4.0
    IL_0069:  stloc.s    V_7
    IL_006b:  br.s       IL_007f

    IL_006d:  ldloc.s    V_6
    IL_006f:  ldloc.s    V_7
    IL_0071:  ldelem.ref
    IL_0072:  stloc.2
    IL_0073:  ldloc.2
    IL_0074:  callvirt   instance void [System.Threading.Thread]System.Threading.Thread::Start()
    IL_0079:  ldloc.s    V_7
    IL_007b:  ldc.i4.1
    IL_007c:  add
    IL_007d:  stloc.s    V_7
    IL_007f:  ldloc.s    V_7
    IL_0081:  ldloc.s    V_6
    IL_0083:  ldlen
    IL_0084:  conv.i4
    IL_0085:  blt.s      IL_006d

    IL_0087:  ldc.i4.0
    IL_0088:  stloc.3
    IL_0089:  br.s       IL_00a3

    IL_008b:  ldc.i4     0x3e8
    IL_0090:  call       void [System.Threading.Thread]System.Threading.Thread::Sleep(int32)
    IL_0095:  ldloc.0
    IL_0096:  volatile.
    IL_0098:  ldfld      bool modreq([System.Runtime]System.Runtime.CompilerServices.IsVolatile) Test_hijacking_il.Repro::stop
    IL_009d:  brtrue.s   IL_00a8

    IL_009f:  ldloc.3
    IL_00a0:  ldc.i4.1
    IL_00a1:  add
    IL_00a2:  stloc.3
    IL_00a3:  ldloc.3
    IL_00a4:  ldc.i4.s   30
    IL_00a6:  blt.s      IL_008b

    IL_00a8:  ldloc.0
    IL_00a9:  ldc.i4.1
    IL_00aa:  volatile.
    IL_00ac:  stfld      bool modreq([System.Runtime]System.Runtime.CompilerServices.IsVolatile) Test_hijacking_il.Repro::stop
    IL_00b1:  ldloc.1
    IL_00b2:  stloc.s    V_8
    IL_00b4:  ldc.i4.0
    IL_00b5:  stloc.s    V_9
    IL_00b7:  br.s       IL_00cd

    IL_00b9:  ldloc.s    V_8
    IL_00bb:  ldloc.s    V_9
    IL_00bd:  ldelem.ref
    IL_00be:  stloc.s    V_4
    IL_00c0:  ldloc.s    V_4
    IL_00c2:  callvirt   instance void [System.Threading.Thread]System.Threading.Thread::Join()
    IL_00c7:  ldloc.s    V_9
    IL_00c9:  ldc.i4.1
    IL_00ca:  add
    IL_00cb:  stloc.s    V_9
    IL_00cd:  ldloc.s    V_9
    IL_00cf:  ldloc.s    V_8
    IL_00d1:  ldlen
    IL_00d2:  conv.i4
    IL_00d3:  blt.s      IL_00b9

    IL_00d5:  ldstr      "Executed {0} GCs"
    IL_00da:  ldloc.0
    IL_00db:  ldfld      int64 Test_hijacking_il.Repro::gc
    IL_00e0:  box        [System.Runtime]System.Int64
    IL_00e5:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object)
    IL_00ea:  ldloc.0
    IL_00eb:  ldfld      bool Test_hijacking_il.Repro::failed
    IL_00f0:  brfalse.s  IL_010a

    IL_00f2:  ldstr      "Executed {0} tail calls before failing"
    IL_00f7:  ldloc.0
    IL_00f8:  ldfld      int64 Test_hijacking_il.Repro::counts
    IL_00fd:  box        [System.Runtime]System.Int64
    IL_0102:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object)
    IL_0107:  ldc.i4.s   101
    IL_0109:  ret

    IL_010a:  ldstr      "Executed {0} tail calls"
    IL_010f:  ldloc.0
    IL_0110:  ldfld      int64 Test_hijacking_il.Repro::counts
    IL_0115:  box        [System.Runtime]System.Int64
    IL_011a:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object)
    IL_011f:  ldstr      "PASS"
    IL_0124:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0129:  ldc.i4.s   100
    IL_012b:  ret
  } // end of method Test_hijacking_il.Repro::Main

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [System.Runtime]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Test_hijacking_il.Repro::.ctor

} // end of class Test_hijacking_il.Repro


// =============================================================

// *********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file hijacking.res
}
