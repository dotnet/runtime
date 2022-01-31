// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


.assembly extern System.Console { auto }
.assembly extern System.Runtime.Extensions { auto }
.assembly extern System.Threading.Thread { auto }
.assembly extern legacy library mscorlib { auto }

.assembly 'SyncGCHole' {}
.assembly extern xunit.core {}
.class private auto ansi beforefieldinit ExternalClass
       extends [mscorlib]System.Object
{
  .field private class ExternalException ee
  .method public hidebysig instance void
          ThrowException() cil managed synchronized
  {
    .maxstack  1
    IL_0000:  ldarg.0
    IL_0001:  ldfld      class ExternalException ExternalClass::ee
    IL_0006:  throw
  }

  .method public hidebysig specialname rtspecialname
          instance void  .ctor() cil managed
  {
    .maxstack  2
    IL_0000:  ldarg.0
    IL_0001:  newobj     instance void ExternalException::.ctor()
    IL_0006:  stfld      class ExternalException ExternalClass::ee
    IL_000b:  ldarg.0
    IL_000c:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0011:  ret
  }

}

.class public auto ansi beforefieldinit ExternalException
       extends [mscorlib]System.Exception
{
  .method public hidebysig static void  Main(string[] args) cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    .maxstack  3
    .locals init (class [System.Threading.Thread]System.Threading.Thread V_0,
             class ExternalException V_1,
             int32 V_2)
    IL_0000:  newobj     instance void ExternalException::.ctor()
    IL_0005:  stloc.1
    IL_0006:  ldc.i4.0
    IL_0007:  stloc.2
    IL_0008:  br.s       IL_0035

    IL_000a:  ldloc.1
    IL_000b:  ldftn      instance void ExternalException::runtest()
    IL_0011:  newobj     instance void [System.Threading.Thread]System.Threading.ThreadStart::.ctor(object,
                                                                                     native int)
    IL_0016:  newobj     instance void [System.Threading.Thread]System.Threading.Thread::.ctor(class [System.Threading.Thread]System.Threading.ThreadStart)
    IL_001b:  stloc.0
    .try
    {
      IL_001c:  ldloc.0
      IL_001d:  callvirt   instance void [System.Threading.Thread]System.Threading.Thread::Start()
      IL_0022:  leave.s    IL_0031

    }
    catch [mscorlib]System.Exception
    {
      IL_0024:  pop
      IL_0025:  ldstr      "Exception was caught in main"
      IL_002a:  call       void [System.Console]System.Console::WriteLine(string)
      IL_002f:  leave.s    IL_0031

    }
    IL_0031:  ldloc.2
    IL_0032:  ldc.i4.1
    IL_0033:  add
    IL_0034:  stloc.2
    IL_0035:  ldloc.2
    IL_0036:  ldc.i4.s   10
    IL_0038:  blt.s      IL_000a

    IL_003a:  call       int32 ['System.Runtime.Extensions']System.Environment::get_ExitCode()
              call       void ['System.Runtime.Extensions']System.Environment::Exit(int32)
              ret
  }

  .method public hidebysig instance void
          runtest() cil managed
  {
    .maxstack  3
    .locals init (int32 V_0,
             int32 V_1,
             class ExternalException V_2)
    IL_0000:  ldc.i4.0
    IL_0001:  stloc.0
    IL_0002:  ldc.i4.0
    IL_0003:  stloc.1
    IL_0004:  br.s       IL_0035

    .try
    {
      .try
      {
        IL_0006:  ldloc.1
        IL_0007:  ldc.i4.2
        IL_0008:  rem
        IL_0009:  brtrue.s   IL_0013

        IL_000b:  ldloc.1
        IL_000c:  ldloc.1
        IL_000d:  ldc.i4.2
        IL_000e:  rem
        IL_000f:  div
        IL_0010:  stloc.0
        IL_0011:  br.s       IL_001a

        IL_0013:  ldarg.0
        IL_0014:  ldc.i4.0
        IL_0015:  call       instance void ExternalException::recurse(int32)
        IL_001a:  leave.s    IL_002a

      }
      catch [mscorlib]System.ArithmeticException
      {
        IL_001c:  pop
        IL_001d:  ldloc.0
        IL_001e:  ldc.i4.1
        IL_001f:  add
        IL_0020:  stloc.0
        IL_0021:  leave.s    IL_0031

      }
      catch ExternalException
      {
        IL_0023:  pop
        IL_0024:  ldloc.0
        IL_0025:  ldc.i4.1
        IL_0026:  sub
        IL_0027:  stloc.0
        IL_0028:  leave.s    IL_0031

      }
      IL_002a:  leave.s    IL_0031

    }
    finally
    {
      IL_002c:  ldloc.0
      IL_002d:  ldc.i4.1
      IL_002e:  add
      IL_002f:  stloc.0
      IL_0030:  endfinally
    }
    IL_0031:  ldloc.1
    IL_0032:  ldc.i4.1
    IL_0033:  add
    IL_0034:  stloc.1
    IL_0035:  ldloc.1
    IL_0036:  ldc.i4.s   100
    IL_0038:  blt.s      IL_0006

    IL_003a:  ldloc.0
    IL_003b:  ldc.i4.s   100
    IL_003d:  bne.un.s   IL_0062

    IL_003f:  ldarg.0
    IL_0040:  dup
    IL_0041:  stloc.2
    IL_0042:  call       void [mscorlib]System.Threading.Monitor::Enter(object)
    .try
    {
      IL_0047:  ldstr      "TryCatch Test Passed"
      IL_004c:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0051:  ldc.i4 100
      IL_0052:  call       void [System.Runtime.Extensions]System.Environment::set_ExitCode(int32)
      IL_0057:  leave.s    IL_0060

    }
    finally
    {
      IL_0059:  ldloc.2
      IL_005a:  call       void [mscorlib]System.Threading.Monitor::Exit(object)
      IL_005f:  endfinally
    }
    IL_0060:  br.s       IL_0089

    IL_0062:  ldarg.0
    IL_0063:  dup
    IL_0064:  stloc.2
    IL_0065:  call       void [mscorlib]System.Threading.Monitor::Enter(object)
    .try
    {
      IL_006a:  ldstr      "TryCatch Test Failed"
      IL_006f:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0074:  ldloc.0
      IL_0075:  call       void [System.Console]System.Console::WriteLine(int32)
      IL_007a:  ldc.i4.1
      IL_007b:  call       void [System.Runtime.Extensions]System.Environment::set_ExitCode(int32)
      IL_0080:  leave.s    IL_0089

    }
    finally
    {
      IL_0082:  ldloc.2
      IL_0083:  call       void [mscorlib]System.Threading.Monitor::Exit(object)
      IL_0088:  endfinally
    }
    IL_0089:  ret
  }

  .method public hidebysig instance void
          recurse(int32 counter) cil managed
  {
    .maxstack  3
    .locals init (char[] V_0)
    IL_0000:  ldc.i4.s   100
    IL_0002:  newarr     [mscorlib]System.Char
    IL_0007:  stloc.0
    IL_0008:  ldarg.1
    IL_0009:  ldc.i4.s   100
    IL_000b:  bne.un.s   IL_0019

    IL_000d:  newobj     instance void ExternalClass::.ctor()
    IL_0012:  call       instance void ExternalClass::ThrowException()
    IL_0017:  br.s       IL_0025

    IL_0019:  ldarg.0
    IL_001a:  ldarg.1
    IL_001b:  ldc.i4.1
    IL_001c:  add
    IL_001d:  dup
    IL_001e:  starg.s    counter
    IL_0020:  call       instance void ExternalException::recurse(int32)
    IL_0025:  ret
  }

  .method public hidebysig specialname rtspecialname
          instance void  .ctor() cil managed
  {
    .maxstack  1
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Exception::.ctor()
    IL_0006:  ret
  }
}
