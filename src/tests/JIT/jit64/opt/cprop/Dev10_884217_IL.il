// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.



 
// Metadata version: v4.0.20926
.assembly extern mscorlib { }
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly Dev10_884217_IL
{
}
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x0000000000170000


// =============== CLASS MEMBERS DECLARATION ===================

.class public abstract auto ansi sealed beforefieldinit Repro
       extends [mscorlib]System.Object
{
  .method private hidebysig static int32 
          CbitLowZero(uint32 i) cil managed noinlining
  {
    // Code size       24 (0x18)
    .maxstack  2
    .locals init (int32 count)
    IL_0000:  ldc.i4.0
    IL_0001:  stloc.0
    IL_0002:  ldarg.0
    IL_0003:  brtrue.s   IL_0011

    IL_0005:  ldc.i4.s   32
    IL_0007:  ret

    IL_0008:  ldloc.0
    IL_0009:  ldc.i4.1
    IL_000a:  add
    IL_000b:  stloc.0
    IL_000c:  ldarg.0
    IL_000d:  ldc.i4.1
    IL_000e:  shr.un
    IL_000f:  starg.s    i
    IL_0011:  ldarg.0
    IL_0012:  ldc.i4.1
    IL_0013:  and
    IL_0014:  brfalse.s  IL_0008

    IL_0016:  ldloc.0
    IL_0017:  ret
  } // end of method Repro::CbitLowZero

  .method public hidebysig static int32 Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       81 (0x51)
    .maxstack  3
    .locals init (string arg,
                  uint64 l,
                  int32  zeroes)
    IL_0000:  ldstr      "4294967296"
    IL_0005:  stloc.0
    IL_0006:  ldloc.0
    IL_0007:  call       uint64 [mscorlib]System.UInt64::Parse(string)
    IL_000c:  stloc.1
    IL_000d:  ldc.i4.0
    IL_000e:  stloc.2
    IL_000f:  ldloca.s   l
    IL_0011:  ldc.i4.0
    IL_0012:  stind.i4   
    IL_0013:  ldloc.1
    IL_0014:  ldc.i4.s   32
    IL_0016:  shr.un
    IL_0017:  conv.u4
    IL_0018:  call       int32 Repro::CbitLowZero(uint32)
    IL_001d:  ldc.i4.s   32
    IL_001f:  add
    IL_0020:  stloc.2
    IL_0021:  ldstr      "{0} has {1} low zero(s)"
    IL_0026:  ldloc.0
    IL_0027:  ldloc.2
    IL_0028:  box        [mscorlib]System.Int32
    IL_002d:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_0032:  ldloc.2
    IL_0033:  ldc.i4.s   32
    IL_0035:  bne.un.s   IL_0044

    IL_0037:  ldstr      "pass"
    IL_003c:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0041:  ldc.i4.s   100
    IL_0043:  ret

    IL_0044:  ldstr      "FAILED!"
    IL_0049:  call       void [System.Console]System.Console::WriteLine(string)
    IL_004e:  ldc.i4.s   -99
    IL_0050:  ret
  } // end of method Repro::Main

} // end of class Repro


