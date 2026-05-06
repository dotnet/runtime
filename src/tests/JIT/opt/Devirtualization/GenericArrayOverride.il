
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


// Metadata version: v4.0.30319
.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }
.assembly extern xunit.core {}
.assembly extern System.Collections
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )                         // .?_....:
  .ver 5:0:0:0
}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )                         // .?_....:
  .ver 5:0:0:0
}
.assembly GenericArrayOverride
{
  .custom instance void [System.Runtime]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
  .custom instance void [System.Runtime]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::.ctor() = ( 01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
                                                                                                                   63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01 )       // ceptionThrows.

  // --- The following custom attribute is added automatically, do not uncomment -------
  //  .custom instance void [System.Runtime]System.Diagnostics.DebuggableAttribute::.ctor(valuetype [System.Runtime]System.Diagnostics.DebuggableAttribute/DebuggingModes) = ( 01 00 07 01 00 00 00 00 ) 

  .custom instance void [System.Runtime]System.Runtime.Versioning.TargetFrameworkAttribute::.ctor(string) = ( 01 00 18 2E 4E 45 54 43 6F 72 65 41 70 70 2C 56   // ....NETCoreApp,V
                                                                                                              65 72 73 69 6F 6E 3D 76 35 2E 30 01 00 54 0E 14   // ersion=v5.0..T..
                                                                                                              46 72 61 6D 65 77 6F 72 6B 44 69 73 70 6C 61 79   // FrameworkDisplay
                                                                                                              4E 61 6D 65 00 )                                  // Name.
  .custom instance void [System.Runtime]System.Reflection.AssemblyCompanyAttribute::.ctor(string) = ( 01 00 14 47 65 6E 65 72 69 63 41 72 72 61 79 4F   // ...GenericArrayO
                                                                                                      76 65 72 72 69 64 65 00 00 )                      // verride..
  .custom instance void [System.Runtime]System.Reflection.AssemblyConfigurationAttribute::.ctor(string) = ( 01 00 05 44 65 62 75 67 00 00 )                   // ...Debug..
  .custom instance void [System.Runtime]System.Reflection.AssemblyFileVersionAttribute::.ctor(string) = ( 01 00 07 31 2E 30 2E 30 2E 30 00 00 )             // ...1.0.0.0..
  .custom instance void [System.Runtime]System.Reflection.AssemblyInformationalVersionAttribute::.ctor(string) = ( 01 00 05 31 2E 30 2E 30 00 00 )                   // ...1.0.0..
  .custom instance void [System.Runtime]System.Reflection.AssemblyProductAttribute::.ctor(string) = ( 01 00 14 47 65 6E 65 72 69 63 41 72 72 61 79 4F   // ...GenericArrayO
                                                                                                      76 65 72 72 69 64 65 00 00 )                      // verride..
  .custom instance void [System.Runtime]System.Reflection.AssemblyTitleAttribute::.ctor(string) = ( 01 00 14 47 65 6E 65 72 69 63 41 72 72 61 79 4F   // ...GenericArrayO
                                                                                                    76 65 72 72 69 64 65 00 00 )                      // verride..
  .hash algorithm 0x00008004
  .ver 1:0:0:0
}
// MVID: {36F29C33-E2D3-47BA-B888-D30052D5C374}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x06DD0000


// =============== CLASS MEMBERS DECLARATION ===================

.class private auto ansi beforefieldinit GenericArrayOverride.Base`1<T>
       extends [System.Runtime]System.Object
{
  .method public hidebysig newslot virtual 
          instance string  Function(!T param) cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "Base"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method Base`1::Function

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [System.Runtime]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  } // end of method Base`1::.ctor

} // end of class GenericArrayOverride.Base`1

.class interface private abstract auto ansi GenericArrayOverride.IFace`1<T>
{
  .method public hidebysig newslot abstract virtual 
          instance string  IFaceFunction(class [System.Collections]System.Collections.Generic.List`1<!T> param) cil managed
  {
  } // end of method IFace`1::IFaceFunction

} // end of class GenericArrayOverride.IFace`1

.class private auto ansi beforefieldinit GenericArrayOverride.Derived
       extends class GenericArrayOverride.Base`1<class GenericArrayOverride.Derived[0...,0...,0...,0...]>
       implements class GenericArrayOverride.IFace`1<class GenericArrayOverride.Derived[0...,0...]>
{
  .method public hidebysig virtual instance string 
          Function(class GenericArrayOverride.Derived[1...,0...,0...,0...] param) cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "DerivedWrong1"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method Derived::Function

  .method public hidebysig virtual instance string 
          Function(class GenericArrayOverride.Derived[0...,0...,0...,0...] param) cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "Derived"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method Derived::Function

  .method public hidebysig virtual instance string 
          Function(class GenericArrayOverride.Derived[2...,0...,0...,0...] param) cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "DerivedWrong2"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method Derived::Function

  .method public hidebysig newslot virtual 
          instance string  IFaceFunction(class [System.Collections]System.Collections.Generic.List`1<class GenericArrayOverride.Derived[1...,0...]> param) cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "IFaceFuncWrong1"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method Derived::IFaceFunction

  .method public hidebysig newslot virtual 
          instance string  IFaceFunction(class [System.Collections]System.Collections.Generic.List`1<class GenericArrayOverride.Derived[0...,0...]> param) cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "IFaceFunc"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method Derived::IFaceFunction

  .method public hidebysig newslot virtual 
          instance string  IFaceFunction(class [System.Collections]System.Collections.Generic.List`1<class GenericArrayOverride.Derived[2...,0...]> param) cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "IFaceFuncWrong2"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method Derived::IFaceFunction

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void class GenericArrayOverride.Base`1<class GenericArrayOverride.Derived[0...,0...,0...,0...]>::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  } // end of method Derived::.ctor

} // end of class GenericArrayOverride.Derived

.class public auto ansi beforefieldinit GenericArrayOverride.Program
       extends [System.Runtime]System.Object
{
  .method public hidebysig static int32 
          Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       120 (0x78)
    .maxstack  5
    .locals init (class GenericArrayOverride.Base`1<class GenericArrayOverride.Derived[0...,0...,0...,0...]> V_0,
             class GenericArrayOverride.IFace`1<class GenericArrayOverride.Derived[0...,0...]> V_1,
             bool V_2,
             int32 V_3,
             bool V_4)
    IL_0000:  nop
    IL_0001:  newobj     instance void GenericArrayOverride.Derived::.ctor()
    IL_0006:  stloc.0
    IL_0007:  ldloc.0
    IL_0008:  castclass  class GenericArrayOverride.IFace`1<class GenericArrayOverride.Derived[0...,0...]>
    IL_000d:  stloc.1
    IL_000e:  ldloc.0
    IL_000f:  ldc.i4.1
    IL_0010:  ldc.i4.1
    IL_0011:  ldc.i4.1
    IL_0012:  ldc.i4.1
    IL_0013:  newobj     instance void class GenericArrayOverride.Derived[0...,0...,0...,0...]::.ctor(int32,
                                                                         int32,
                                                                         int32,
                                                                         int32)
    IL_0018:  callvirt   instance string class GenericArrayOverride.Base`1<class GenericArrayOverride.Derived[0...,0...,0...,0...]>::Function(!0)
    IL_001d:  ldstr      "Derived"
    IL_0022:  call       bool [System.Runtime]System.String::op_Inequality(string,
                                                                           string)
    IL_0027:  stloc.2
    IL_0028:  ldloc.2
    IL_0029:  brfalse.s  IL_003b

    IL_002b:  nop
    IL_002c:  ldstr      "Virtual override failed"
    IL_0031:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0036:  nop
    IL_0037:  ldc.i4.1
    IL_0038:  stloc.3
    IL_0039:  br.s       IL_0076

    IL_003b:  ldloc.1
    IL_003c:  newobj     instance void class [System.Collections]System.Collections.Generic.List`1<class GenericArrayOverride.Derived[0...,0...]>::.ctor()
    IL_0041:  callvirt   instance string class GenericArrayOverride.IFace`1<class GenericArrayOverride.Derived[0...,0...]>::IFaceFunction(class [System.Collections]System.Collections.Generic.List`1<!0>)
    IL_0046:  ldstr      "IFaceFunc"
    IL_004b:  call       bool [System.Runtime]System.String::op_Inequality(string,
                                                                           string)
    IL_0050:  stloc.s    V_4
    IL_0052:  ldloc.s    V_4
    IL_0054:  brfalse.s  IL_0066

    IL_0056:  nop
    IL_0057:  ldstr      "Interface override failed"
    IL_005c:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0061:  nop
    IL_0062:  ldc.i4.2
    IL_0063:  stloc.3
    IL_0064:  br.s       IL_0076

    IL_0066:  ldstr      "PASS"
    IL_006b:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0070:  nop
    IL_0071:  ldc.i4.s   100
    IL_0073:  stloc.3
    IL_0074:  br.s       IL_0076

    IL_0076:  ldloc.3
    IL_0077:  ret
  } // end of method Program::Main

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [System.Runtime]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  } // end of method Program::.ctor

} // end of class GenericArrayOverride.Program


// =============================================================

// *********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file C:\git\runtime\src\tests\JIT\opt\Devirtualization\GenericArrayOverride.res
