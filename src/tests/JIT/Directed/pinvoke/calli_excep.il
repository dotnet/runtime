// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern xunit.core {}

.assembly extern mscorlib { }
.assembly calli_excep { }
.namespace JitTest
{
  .class public auto ansi beforefieldinit Test
         extends [mscorlib]System.Object
  {
    .method private hidebysig static pinvokeimpl("kernel32" winapi) 
            void  RaiseException(unsigned int32 code,
                                 unsigned int32 ,
                                 unsigned int32 argNum,
                                 native uint argsPtr) cil managed preservesig
    {
    }
    .method public hidebysig static int32 Main() cil managed
    {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint
      .custom instance void [mscorlib]System.Runtime.ExceptionServices.HandleProcessCorruptedStateExceptionsAttribute::.ctor() = ( 01 00 00 00 ) 
      .custom instance void [mscorlib]System.Security.SecurityCriticalAttribute::.ctor() = ( 01 00 00 00 ) 
      .maxstack  8
      .locals (class [mscorlib]System.NullReferenceException V_0,
               int32 V_1)
      .try
      {
			ldc.i4   0xC0000017
			ldc.i4.0
			dup
			dup
			ldftn      void JitTest.Test::RaiseException(unsigned int32,unsigned int32,unsigned int32,native uint)
			ldc.i4 100
			newarr  float32
			pop
			calli   void (unsigned int32, unsigned int32, unsigned int32, native uint)
			leave.s    IL_001d
      }  // end .try
      catch [mscorlib]System.OutOfMemoryException 
      {
        IL_000f:  stloc.0
        IL_0010:  ldloc.0
        IL_0011:  callvirt   instance string [mscorlib]System.Exception::ToString()
        IL_0016:  call       void [System.Console]System.Console::WriteLine(string)
        IL_001b:  leave.s    IL_002c

      }  // end handler
      IL_001d:  ldstr      "=== FAILED ==="
      IL_0022:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0027:  ldc.i4.s   101
      IL_0029:  stloc.1
      IL_002a:  br.s       IL_003b

      IL_002c:  ldstr      "=== PASSED ==="
      IL_0031:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0036:  ldc.i4.s   100
      IL_0038:  stloc.1
      IL_0039:  br.s       IL_003b

      IL_003b:  ldloc.1
      IL_003c:  ret
    } // end of method Test::Main

    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ret
    } // end of method Test::.ctor

  } // end of class Test

} // end of namespace JitTest

