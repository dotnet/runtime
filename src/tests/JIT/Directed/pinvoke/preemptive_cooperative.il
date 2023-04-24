// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


.assembly extern System.Console { auto }
.assembly extern xunit.core {}
.assembly extern System.Threading.Thread { auto }
.assembly extern mscorlib { auto }

.assembly preemptive_cooperative
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 )
  .custom instance void [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::.ctor() = ( 01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78
                                                                                                             63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01 )
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.module preemptive_cooperative.exe
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003
.corflags 0x00000001



.class public auto ansi beforefieldinit Repro
       extends [mscorlib]System.Object
{
  .field private static class [mscorlib]System.Threading.EventWaitHandle e1
  .field private static class [mscorlib]System.Threading.EventWaitHandle e2
  .field private static object modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) garbage
  .method private hidebysig specialname rtspecialname
          instance void  .ctor() cil managed
  {
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  nop
    IL_0008:  ldstr      "Created"
    IL_000d:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0012:  nop
    IL_0013:  nop
    IL_0014:  ret
  }

  .method family hidebysig virtual instance void
          Finalize() cil managed
  {
    .maxstack  1
    .try
    {
      IL_0000:  nop
      IL_0001:  ldstr      "Destroyed"
      IL_0006:  call       void [System.Console]System.Console::WriteLine(string)
      IL_000b:  nop
      IL_000c:  nop
      IL_000d:  leave.s    IL_0017

    }
    finally
    {
      IL_000f:  ldarg.0
      IL_0010:  call       instance void [mscorlib]System.Object::Finalize()
      IL_0015:  nop
      IL_0016:  endfinally
    }
    IL_0017:  nop
    IL_0018:  ret
  }

  .method private hidebysig static void  CreateGarbage() cil managed noinlining
  {
    .maxstack  1
    IL_0000:  nop
    IL_0001:  newobj     instance void [mscorlib]System.Object::.ctor()
    IL_0006:  volatile.
    IL_0008:  stsfld     object modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) Repro::garbage
    IL_000d:  newobj     instance void [mscorlib]System.Object::.ctor()
    IL_0012:  volatile.
    IL_0014:  stsfld     object modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) Repro::garbage
    IL_0019:  newobj     instance void [mscorlib]System.Object::.ctor()
    IL_001e:  volatile.
    IL_0020:  stsfld     object modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) Repro::garbage
    IL_0025:  newobj     instance void [mscorlib]System.Object::.ctor()
    IL_002a:  volatile.
    IL_002c:  stsfld     object modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) Repro::garbage
    IL_0031:  newobj     instance void [mscorlib]System.Object::.ctor()
    IL_0036:  volatile.
    IL_0038:  stsfld     object modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) Repro::garbage
    IL_003d:  newobj     instance void [mscorlib]System.Object::.ctor()
    IL_0042:  volatile.
    IL_0044:  stsfld     object modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) Repro::garbage
    IL_0049:  newobj     instance void [mscorlib]System.Object::.ctor()
    IL_004e:  volatile.
    IL_0050:  stsfld     object modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) Repro::garbage
    IL_0055:  newobj     instance void [mscorlib]System.Object::.ctor()
    IL_005a:  volatile.
    IL_005c:  stsfld     object modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) Repro::garbage
    IL_0061:  ret
  }

  .method private hidebysig static pinvokeimpl("kernel32.dll" as "WaitForSingleObject" nomangle winapi)
          uint32 modopt([mscorlib]System.Runtime.CompilerServices.IsJitIntrinsic) atoi(native int handle,
                       uint32 dwMilliseconds) cil managed preservesig
  {
    .custom instance void [mscorlib]System.Security.SuppressUnmanagedCodeSecurityAttribute::.ctor() = ( 01 00 00 00 )
  }
  .method private hidebysig static void  GCHole() cil managed
  {
    .maxstack  2
    .locals init (class Repro V_0)
    IL_0000:  nop
    IL_0001:  ldsfld     class [mscorlib]System.Threading.EventWaitHandle Repro::e1
    IL_0006:  callvirt   instance bool [mscorlib]System.Threading.WaitHandle::WaitOne()
    IL_000b:  pop
    IL_000c:  newobj     instance void Repro::.ctor()
    IL_0011:  stloc.0
    IL_0012:  call       void Repro::CreateGarbage()
    IL_0017:  nop
    IL_0018:  ldsfld     class [mscorlib]System.Threading.EventWaitHandle Repro::e2
    IL_001d:  callvirt   instance bool [mscorlib]System.Threading.EventWaitHandle::Set()
    IL_0022:  pop
    IL_0023:  ldsfld     class [mscorlib]System.Threading.EventWaitHandle Repro::e1
    IL_0028:  callvirt   instance native int [mscorlib]System.Threading.WaitHandle::get_Handle()
    IL_002d:  ldc.i4.m1
    IL_002e:  call       uint32 modopt([mscorlib]System.Runtime.CompilerServices.IsJitIntrinsic) Repro::atoi(native int,
                                            uint32)
    IL_0033:  pop
    IL_0034:  call       void Repro::CreateGarbage()
    IL_0039:  nop
    IL_003a:  ldloc.0
    IL_003b:  callvirt   instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
    IL_0040:  call       void [System.Console]System.Console::WriteLine(object)
    IL_0045:  nop
    IL_0046:  ret
  }

  .method private hidebysig static void  Trigger() cil managed
  {
    .maxstack  1
    IL_0000:  nop
    IL_0001:  call       void [mscorlib]System.GC::Collect()
    IL_0006:  nop
    IL_0007:  call       void [mscorlib]System.GC::WaitForPendingFinalizers()
    IL_000c:  nop
    IL_000d:  ldsfld     class [mscorlib]System.Threading.EventWaitHandle Repro::e1
    IL_0012:  callvirt   instance bool [mscorlib]System.Threading.EventWaitHandle::Set()
    IL_0017:  pop
    IL_0018:  ldsfld     class [mscorlib]System.Threading.EventWaitHandle Repro::e2
    IL_001d:  callvirt   instance bool [mscorlib]System.Threading.WaitHandle::WaitOne()
    IL_0022:  pop
    IL_0023:  ldc.i4     0x3e8
    IL_0028:  call       void [System.Threading.Thread]System.Threading.Thread::Sleep(int32)
    IL_002d:  nop
    IL_002e:  call       void [mscorlib]System.GC::Collect()
    IL_0033:  nop
    IL_0034:  call       void [mscorlib]System.GC::WaitForPendingFinalizers()
    IL_0039:  nop
    IL_003a:  ldsfld     class [mscorlib]System.Threading.EventWaitHandle Repro::e1
    IL_003f:  callvirt   instance bool [mscorlib]System.Threading.EventWaitHandle::Set()
    IL_0044:  pop
    IL_0045:  ret
  }

  .method public hidebysig static int32 Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    .maxstack  3
    .locals init (class [System.Threading.Thread]System.Threading.Thread V_0,
             int32 V_1)
    IL_0000:  nop
    IL_0001:  ldnull
    IL_0002:  ldftn      void Repro::GCHole()
    IL_0008:  newobj     instance void [System.Threading.Thread]System.Threading.ThreadStart::.ctor(object,
                                                                                     native int)
    IL_000d:  newobj     instance void [System.Threading.Thread]System.Threading.Thread::.ctor(class [System.Threading.Thread]System.Threading.ThreadStart)
    IL_0012:  stloc.0
    IL_0013:  ldloc.0
    IL_0014:  callvirt   instance void [System.Threading.Thread]System.Threading.Thread::Start()
    IL_0019:  nop
    IL_001a:  call       void Repro::Trigger()
    IL_001f:  nop
    IL_0020:  ldloc.0
    IL_0021:  callvirt   instance void [System.Threading.Thread]System.Threading.Thread::Join()
    IL_0026:  nop
    IL_0027:  ldc.i4.s   100
    IL_0029:  stloc.1
    IL_002a:  br.s       IL_002c

    IL_002c:  ldloc.1
    IL_002d:  ret
  }

  .method private hidebysig specialname rtspecialname static
          void  .cctor() cil managed
  {
    .maxstack  8
    IL_0000:  ldc.i4.0
    IL_0001:  ldc.i4.0
    IL_0002:  newobj     instance void [mscorlib]System.Threading.EventWaitHandle::.ctor(bool,
                                                                                         valuetype [mscorlib]System.Threading.EventResetMode)
    IL_0007:  stsfld     class [mscorlib]System.Threading.EventWaitHandle Repro::e1
    IL_000c:  ldc.i4.0
    IL_000d:  ldc.i4.0
    IL_000e:  newobj     instance void [mscorlib]System.Threading.EventWaitHandle::.ctor(bool,
                                                                                         valuetype [mscorlib]System.Threading.EventResetMode)
    IL_0013:  stsfld     class [mscorlib]System.Threading.EventWaitHandle Repro::e2
    IL_0018:  ldnull
    IL_0019:  volatile.
    IL_001b:  stsfld     object modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) Repro::garbage
    IL_0020:  ret
  }

}
