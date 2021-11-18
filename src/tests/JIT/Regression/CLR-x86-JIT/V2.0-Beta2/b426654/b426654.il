// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


.module extern user32.dll
.module extern kernel32
.assembly extern System.Console { auto }
.assembly extern System.Threading.Thread { auto }
.assembly extern mscorlib { auto }

.assembly cs1
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 )
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.assembly extern xunit.core {}
.module cs1.exe
.imagebase 0x00400000
.file alignment 0x00000200
.subsystem 0x0003
.corflags 0x00000001



.class public auto ansi beforefieldinit cs1
       extends [mscorlib]System.Object
{
  .field private static literal int32 VAL = int32(0x00000005)
  .field static assembly int32 s_Zero
  .field static assembly int32 s_i
  .field private static class cs1 s_obj
  .field private static bool s_timeUp
  .field private object m_obj
  .field private object modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) m_objVolatile
  .field private object[] m_objs

  .method public hidebysig specialname rtspecialname
          instance void  .ctor() cil managed
  {
    .maxstack  3
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  nop
    IL_0008:  ldarg.0
    IL_0009:  ldc.i4.2
    IL_000a:  newarr     [mscorlib]System.Object
    IL_000f:  stfld      object[] cs1::m_objs
    IL_0014:  ldarg.0
    IL_0015:  ldfld      object[] cs1::m_objs
    IL_001a:  ldc.i4.0
    IL_001b:  ldc.i4.5
    IL_001c:  box        [mscorlib]System.Int32
    IL_0021:  stelem.ref
    IL_0022:  ldarg.0
    IL_0023:  ldfld      object[] cs1::m_objs
    IL_0028:  ldc.i4.1
    IL_0029:  ldstr      "Hello"
    IL_002e:  stelem.ref
    IL_002f:  ldarg.0
    IL_0030:  ldarg.0
    IL_0031:  ldfld      object[] cs1::m_objs
    IL_0036:  ldc.i4.0
    IL_0037:  ldelem.ref
    IL_0038:  stfld      object cs1::m_obj
    IL_003d:  ldarg.0
    IL_003e:  ldarg.0
    IL_003f:  ldfld      object[] cs1::m_objs
    IL_0044:  ldc.i4.1
    IL_0045:  ldelem.ref
    IL_0046:  volatile.
    IL_0048:  stfld      object modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) cs1::m_objVolatile
    IL_004d:  nop
    IL_004e:  ret
  }

  .method public hidebysig instance void
          CheckVal() cil managed noinlining
  {
    .maxstack  2
    .locals init (uint32 V_0,
             uint32 V_1,
             bool V_2)
    IL_0000:  nop
    IL_0001:  ldc.i4.0
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_006d

    IL_0005:  nop
    .try
    {
      IL_0006:  nop
      IL_0007:  ldarg.0
      IL_0008:  ldfld      object cs1::m_obj
      IL_000d:  unbox      [mscorlib]System.Int32
        ldc.i4 0
        stind.i4

      IL_0032:  ldarg.0
      IL_0033:  volatile.
      IL_0035:  ldfld      object modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) cs1::m_objVolatile
      IL_003a:  unbox      [mscorlib]System.Int32
        ldc.i4 0
        stind.i4

      IL_005f:  nop
      IL_0060:  leave.s    IL_0067

    }
    catch [mscorlib]System.InvalidCastException
    {
      IL_0062:  pop
      IL_0063:  nop
      IL_0064:  nop
      IL_0065:  leave.s    IL_0067

    }
    IL_0067:  nop
    IL_0068:  nop
    IL_0069:  ldloc.0
    IL_006a:  ldc.i4.1
    IL_006b:  xor
    IL_006c:  stloc.0
    IL_006d:  volatile.
              ldsfld     bool cs1::s_timeUp
    IL_0072:  ldc.i4.0
    IL_0073:  ceq
    IL_0075:  stloc.2
    IL_0076:  ldloc.2
    IL_0077:  brtrue.s   IL_0005

    IL_0079:  ret
  }

  .method private hidebysig instance void
          Flip() cil managed
  {
    .maxstack  4
    .locals init (uint32 V_0,
             string V_1,
             bool V_2)
    IL_0000:  nop
    IL_0001:  ldc.i4.0
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0051

    IL_0005:  nop
    IL_0006:  ldarg.0
    IL_0007:  ldfld      object[] cs1::m_objs
    IL_000c:  ldc.i4.1
    IL_000d:  ldelem.ref
    IL_000e:  castclass  [mscorlib]System.String
    IL_0013:  stloc.1
    IL_0014:  ldloc.1
    IL_0015:  ldc.i4.0
    IL_0016:  callvirt   instance char [mscorlib]System.String::get_Chars(int32)
    IL_001b:  ldc.i4.s   72
    IL_001d:  ceq
    IL_001f:  stloc.2
    IL_0020:  ldloc.2
    IL_0021:  brtrue.s   IL_002e

    IL_0023:  ldstr      "String is corrupted"
    IL_0028:  newobj     instance void [mscorlib]System.Exception::.ctor(string)
    IL_002d:  throw

    IL_002e:  ldarg.0
    IL_002f:  ldarg.0
    IL_0030:  ldfld      object cs1::m_obj
    IL_0035:  volatile.
    IL_0037:  stfld      object modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) cs1::m_objVolatile
    IL_003c:  ldarg.0
    IL_003d:  ldarg.0
    IL_003e:  ldfld      object[] cs1::m_objs
    IL_0043:  ldloc.0
    IL_0044:  ldc.i4.2
    IL_0045:  rem
    IL_0046:  ldelem.ref
    IL_0047:  stfld      object cs1::m_obj
    IL_004c:  nop
    IL_004d:  ldloc.0
    IL_004e:  ldc.i4.1
    IL_004f:  xor



    IL_0050:  stloc.0
    IL_0051:  volatile.
              ldsfld     bool cs1::s_timeUp
    IL_0056:  ldc.i4.0
    IL_0057:  ceq
    IL_0059:  stloc.2
    IL_005a:  ldloc.2
    IL_005b:  brtrue.s   IL_0005

    IL_005d:  ret
  }

  .method private hidebysig static void  SignalTimeUp(object stateInfo) cil managed
  {
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldc.i4.1
    IL_0002:  volatile.
              stsfld     bool cs1::s_timeUp
    IL_0007:  ret
  }

  .method public hidebysig static int32 Main(string[] args) cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    .maxstack  5
    .locals init (class cs1 V_0,
             class [System.Threading.Thread]System.Threading.Thread V_1,
             class [mscorlib]System.Threading.Timer V_2,
             class [System.Threading.Thread]System.Threading.Thread V_flip
        )
    IL_0000:  nop
    IL_0001:  newobj     instance void cs1::.ctor()
    IL_0006:  stloc.0
    IL_0007:  ldloc.0
    IL_0008:  ldftn      instance void cs1::CheckVal()
    IL_000e:  newobj     instance void [System.Threading.Thread]System.Threading.ThreadStart::.ctor(object,
                                                                                     native int)
    IL_0013:  newobj     instance void [System.Threading.Thread]System.Threading.Thread::.ctor(class [System.Threading.Thread]System.Threading.ThreadStart)
    IL_0018:  stloc.1
    IL_0019:  ldloc.1
    IL_001a:  callvirt   instance void [System.Threading.Thread]System.Threading.Thread::Start()
    IL_001f:  nop
    IL_0020:  ldc.i4.0
    IL_0021:  call       void [System.Threading.Thread]System.Threading.Thread::Sleep(int32)
    IL_0026:  nop
    IL_0027:  ldloc.0

    ldftn      instance void cs1::Flip()
    newobj     instance void [System.Threading.Thread]System.Threading.ThreadStart::.ctor(object,
                                                                                     native int)
    newobj     instance void [System.Threading.Thread]System.Threading.Thread::.ctor(class [System.Threading.Thread]System.Threading.ThreadStart)
    stloc V_flip
    ldloc V_flip
    callvirt   instance void [System.Threading.Thread]System.Threading.Thread::Start()
    nop


    IL_002d:  nop
    IL_002e:  ldnull
    IL_002f:  ldftn      void cs1::SignalTimeUp(object)
    IL_0035:  newobj     instance void [mscorlib]System.Threading.TimerCallback::.ctor(object,
                                                                                       native int)
    IL_003a:  ldnull
    IL_003b:  ldc.i4     0x1388
    IL_0040:  ldc.i4.m1
    IL_0041:  newobj     instance void [mscorlib]System.Threading.Timer::.ctor(class [mscorlib]System.Threading.TimerCallback,
                                                                               object,
                                                                               int32,
                                                                               int32)
    IL_0046:  stloc.2
    IL_0047:  ldloc.1
    IL_0048:  callvirt   instance void [System.Threading.Thread]System.Threading.Thread::Join()
    IL_004d:  nop

    ldloc V_flip
    callvirt   instance void [System.Threading.Thread]System.Threading.Thread::Join()
    nop

    IL_004e:  ldstr      "Test SUCCESS"
    IL_0053:  call       void [System.Console]System.Console::WriteLine(string)

    ldloc.2
    callvirt   instance void [mscorlib]System.IDisposable::Dispose()

    ldc.i4 100
    IL_0059:  ret
  }

  .method private hidebysig specialname rtspecialname static
          void  .cctor() cil managed
  {
    .maxstack  8
    IL_0000:  ldc.i4.0
    IL_0001:  stsfld     int32 cs1::s_Zero
    IL_0006:  ldc.i4.0
    IL_0007:  stsfld     int32 cs1::s_i
    IL_000c:  newobj     instance void cs1::.ctor()
    IL_0011:  stsfld     class cs1 cs1::s_obj
    IL_0016:  ldc.i4.0
    IL_0017:  volatile.
              stsfld     bool cs1::s_timeUp
    IL_001c:  ret
  }

}
