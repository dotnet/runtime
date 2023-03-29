// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { }
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'tail_zeroinit' { }
.class public auto ansi beforefieldinit AA
       extends [mscorlib]System.Object
{
  .method private hidebysig static void  Callee1(int64& B) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  2
    .locals init (int64 V_0)
    ldarg.0
    ldloca.s 0
    ldind.i8
    stind.i8
    ret
  } // end of method AA::Callee1

  .method private hidebysig static void  Caller1(int64& A,
                                                 int64& B) cil managed
  {
    .maxstack  2
    .locals init (int64 V_0)
    ldarg.0
    ldloca.s 0
    ldind.i8
    stind.i8
    ldarg.1
    tail. call       void AA::Callee1(int64&)
    ret
  } // end of method AA::Caller1

  .method private hidebysig static int64 
          Callee2() cil managed
  {
    // Code size       9 (0x9)
    .maxstack  1
    .locals init (int64 V_0)
    ldloca.s 0
    ldind.i8
    ret
  } // end of method AA::Callee2

  .method private hidebysig static int64 
          Caller2() cil managed
  {
    // Code size       15 (0xf)
    .maxstack  2
    .locals init (int64 V_0)
    ldloca.s 0
    ldind.i8
    brfalse.s IL_JMP
    newobj instance void [mscorlib]System.Exception::.ctor()
    throw
   IL_JMP:
    jmp  int64 AA::Callee2()
  } // end of method AA::Caller2

  .method public hidebysig static int32 Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       81 (0x51)
    .maxstack  2
    .locals init (int64 V_0,
             int64 V_1,
             int32 V_2)
    IL_0000:  ldc.i4.0
    IL_0001:  conv.i8
    IL_0002:  stloc.0
    IL_0003:  ldc.i4.0
    IL_0004:  conv.i8
    IL_0005:  stloc.1
    IL_0006:  ldloca.s   V_0
    IL_0008:  ldloca.s   V_1
    IL_000a:  call       void AA::Caller1(int64&,
                                          int64&)
    IL_000f:  ldloc.0
    IL_0010:  ldc.i4.0
    IL_0011:  conv.i8
    IL_0012:  bne.un.s   IL_0019

    IL_0014:  ldloc.1
    IL_0015:  ldc.i4.0
    IL_0016:  conv.i8
    IL_0017:  beq.s      IL_0028

    IL_0019:  ldstr      "Error 101"
    IL_001e:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0023:  ldc.i4.s   101
    IL_0025:  stloc.2
    IL_0026:  br.s       IL_004f

    IL_0028:  call       int64 AA::Caller2()
    IL_002d:  ldc.i4.0
    IL_002e:  conv.i8
    IL_002f:  beq.s      IL_0040

    IL_0031:  ldstr      "Error 102"
    IL_0036:  call       void [System.Console]System.Console::WriteLine(string)
    IL_003b:  ldc.i4.s   102
    IL_003d:  stloc.2
    IL_003e:  br.s       IL_004f

    IL_0040:  ldstr      "Passed"
    IL_0045:  call       void [System.Console]System.Console::WriteLine(string)
    IL_004a:  ldc.i4.s   100
    IL_004c:  stloc.2
    IL_004d:  br.s       IL_004f

    IL_004f:  ldloc.2
    IL_0050:  ret
  } // end of method AA::Main

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method AA::.ctor

} // end of class AA


// =============================================================

//*********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file tail.res
