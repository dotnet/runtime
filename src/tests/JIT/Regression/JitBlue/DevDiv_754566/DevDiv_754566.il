// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


// Metadata version: v4.0.30319
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 4:0:0:0
}
.assembly 'DevDiv_754566'
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 )
  .custom instance void [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::.ctor() = ( 01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
                                                                                                             63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01 )       // ceptionThrows.
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.assembly extern xunit.core {}
// MVID: {A80A87C4-1DDB-4F93-AB31-444266FDFA55}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x0000024A58020000


// =============== CLASS MEMBERS DECLARATION ===================

.class private auto ansi beforefieldinit Program
       extends [mscorlib]System.Object
{
  .method public hidebysig instance string
          Test(int32 val) cil managed noinlining
  {
    // This testcase ensures that we don't perform devirtualization
    // via an unboxing optimization, for the callvirt below.

    // Code size       12 (0xc)
    .maxstack  8
    IL_0000:  ldarg.1
    IL_0001:  box        [mscorlib]System.Int32
              tail.
    IL_0006:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_000b:  ret
  } // end of method Program::Test

  .method private hidebysig static int32
          Main(string[] args) cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       73 (0x49)
    .maxstack  2
    .locals init (int32 V_0,
             class Program V_1,
             string V_2)
    IL_0000:  ldc.i4.m1
    IL_0001:  stloc.0
    IL_0002:  newobj     instance void Program::.ctor()
    IL_0007:  stloc.1
    IL_0008:  ldloc.1
    IL_0009:  ldc.i4.s   42
    IL_000b:  callvirt   instance string Program::Test(int32)
    IL_0010:  stloc.2
    IL_0011:  ldloc.2
    IL_0012:  ldstr      "42"
    IL_0017:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_001c:  brfalse.s  IL_002d

    IL_001e:  ldstr      "=== PASSED ==="
    IL_0023:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_0028:  ldc.i4.s   100
    IL_002a:  stloc.0
    IL_002b:  br.s       IL_0047

    IL_002d:  ldstr      "result should be 42, is= "
    IL_0032:  ldloc.2
    IL_0033:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_0038:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_003d:  ldstr      "+++ FAILED +++"
    IL_0042:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_0047:  ldloc.0
    IL_0048:  ret
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


// =============================================================

// *********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file test2.res
