// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly b598031 {}
.assembly extern xunit.core {}

.class public auto ansi beforefieldinit Program
       extends [mscorlib]System.Object
{
  .method private hidebysig static void  ThrowException(int32 errorCode) cil managed
  {
    // Code size       75 (0x4b)
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  stloc.0
    IL_0003:  ldloc.0
    IL_0004:  ldc.i4.s   -10
    IL_0006:  beq.s      IL_0035

    IL_0008:  ldloc.0
    IL_0009:  ldc.i4.s   -3
    IL_000b:  sub
    IL_000c:  switch     ( 
                          IL_002a,
                          IL_0040,
                          IL_001f)
    IL_001d:  br.s       IL_0040

    IL_001f:  ldstr      "-1"
    IL_0024:  newobj     instance void [mscorlib]System.Exception::.ctor(string)
    IL_0029:  throw

    IL_002a:  ldstr      "-3"
    IL_002f:  newobj     instance void [mscorlib]System.Exception::.ctor(string)
    IL_0034:  throw

    IL_0035:  ldstr      "-10"
    IL_003a:  newobj     instance void [mscorlib]System.Exception::.ctor(string)
    IL_003f:  throw

    IL_0040:  ldstr      "others"
    IL_0045:  newobj     instance void [mscorlib]System.Exception::.ctor(string)
    IL_004a:  throw
  } // end of method Program::ThrowException

  .method public hidebysig static int32 
          Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       46 (0x2e)
    .maxstack  1
    .locals init (int32 V_0)
    IL_0000:  nop
    .try
    {
      IL_0001:  nop
      IL_0002:  ldc.i4.s   11
      IL_0004:  call       void Program::ThrowException(int32)
      IL_0009:  nop
      IL_000a:  ldstr      "Unexcepted result."
      IL_000f:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0014:  nop
      IL_0015:  ldc.i4.1
      IL_0016:  stloc.0
      IL_0017:  leave.s    IL_002b

    }  // end .try
    catch [mscorlib]System.Object 
    {
      IL_0019:  pop
      IL_001a:  nop
      IL_001b:  ldstr      "Excepted result."
      IL_0020:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0025:  nop
      IL_0026:  ldc.i4.s   100
      IL_0028:  stloc.0
      IL_0029:  leave.s    IL_002b

    }  // end handler
    IL_002b:  nop
    IL_002c:  ldloc.0
    IL_002d:  ret
  } // end of method Program::Main

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Program::.ctor

} // end of class Program
