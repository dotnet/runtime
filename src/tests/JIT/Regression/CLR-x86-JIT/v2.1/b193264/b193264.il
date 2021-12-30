// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}





// Metadata version: v2.0.30327
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 2:0:0:0
}
.assembly 'b193264'
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::.ctor() = ( 01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
                                                                                                             63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01 )       // ceptionThrows.
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.assembly extern xunit.core {}
// MVID: {D6DF3AAB-BC31-4E88-8EC0-2E585288A52B}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x03010000


// =============== CLASS MEMBERS DECLARATION ===================

.class private auto ansi beforefieldinit TypeTest.SomeClass
       extends [mscorlib]System.Object
{
  .field private int32 f1
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       17 (0x11)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  nop
    IL_0008:  ldarg.0
    IL_0009:  ldc.i4.0
    IL_000a:  stfld      int32 TypeTest.SomeClass::f1
    IL_000f:  nop
    IL_0010:  ret
  } // end of method SomeClass::.ctor

} // end of class TypeTest.SomeClass

.class private auto ansi beforefieldinit TypeTest.TestMain
       extends [mscorlib]System.Object
{
  .method private hidebysig static int32 
          Main(string[] args) cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       76 (0x4c)
    .maxstack  2
    .locals init (int32 V_0,
             class TypeTest.SomeClass[] pinned V_1,
             class [mscorlib]System.Type V_2,
             class [mscorlib]System.Type V_3,
             int32 V_4,
             bool V_5)
    IL_0000:  nop
    IL_0001:  ldc.i4.s   100
    IL_0003:  stloc.0
    IL_0004:  ldc.i4.s   10
    IL_0006:  newarr     TypeTest.SomeClass
    IL_000b:  stloc.1
    IL_000c:  ldloc.1
    IL_000d:  callvirt   instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
    IL_0012:  stloc.2
    IL_0013:  ldtoken    class TypeTest.SomeClass[]
    IL_0018:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_001d:  stloc.3
    IL_001e:  ldloc.2
    IL_001f:  ldloc.3
    IL_0020:  ceq
    IL_0022:  stloc.s    V_5
    IL_0024:  ldloc.s    V_5
    IL_0026:  brtrue.s   IL_0037

    IL_0028:  nop
    IL_0029:  ldstr      "NOT same types"
    IL_002e:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0033:  ldc.i4.s 0
    IL_0034:  stloc.0
    IL_0035:  br.s       IL_0044

    IL_0037:  nop
    IL_0038:  ldstr      "same types"
    IL_003d:  call       void [System.Console]System.Console::WriteLine(string)
              ldloc.1
              castclass  class TypeTest.SomeClass[] pinned
              stloc.1
    IL_0042:  nop
    IL_0043:  nop
    IL_0044:  ldloc.0
    IL_0045:  stloc.s    V_4
    IL_0047:  br.s       IL_0049

    IL_0049:  ldloc.s    V_4
    IL_004b:  ret
  } // end of method TestMain::Main

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method TestMain::.ctor

} // end of class TypeTest.TestMain


// =============================================================

// *********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file p.res
