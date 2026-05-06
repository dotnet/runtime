
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Metadata version: v4.0.30319
.assembly extern System.Runtime
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )                         // .?_....:
  .ver 5:0:0:0
}
.assembly extern xunit.core {}
.assembly 'GitHub_41571'
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
  .custom instance void [System.Runtime]System.Reflection.AssemblyCompanyAttribute::.ctor(string) = ( 01 00 05 34 31 35 37 31 00 00 )                   // ...41571..
  .custom instance void [System.Runtime]System.Reflection.AssemblyConfigurationAttribute::.ctor(string) = ( 01 00 05 44 65 62 75 67 00 00 )                   // ...Debug..
  .custom instance void [System.Runtime]System.Reflection.AssemblyFileVersionAttribute::.ctor(string) = ( 01 00 07 31 2E 30 2E 30 2E 30 00 00 )             // ...1.0.0.0..
  .custom instance void [System.Runtime]System.Reflection.AssemblyInformationalVersionAttribute::.ctor(string) = ( 01 00 05 31 2E 30 2E 30 00 00 )                   // ...1.0.0..
  .custom instance void [System.Runtime]System.Reflection.AssemblyProductAttribute::.ctor(string) = ( 01 00 05 34 31 35 37 31 00 00 )                   // ...41571..
  .custom instance void [System.Runtime]System.Reflection.AssemblyTitleAttribute::.ctor(string) = ( 01 00 05 34 31 35 37 31 00 00 )                   // ...41571..
  .hash algorithm 0x00008004
  .ver 1:0:0:0
}
// MVID: {7F828A34-F3E8-43DB-A60B-950719B2314B}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x000001926B4D0000


// =============== CLASS MEMBERS DECLARATION ===================

.class public auto ansi beforefieldinit GitHub_41571.Program
       extends [System.Runtime]System.Object
{
  .method public hidebysig static int32 
          Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       19 (0x13)
    .maxstack  1
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  newobj     instance void GitHub_41571.Base::.ctor()
    IL_0006:  call       instance class GitHub_41571.Derived GitHub_41571.Base::CrashInCallingScope()
    IL_000b:  pop
    IL_000c:  ldc.i4.s   100
    IL_000e:  stloc.0
    IL_000f:  br.s       IL_0011

    IL_0011:  ldloc.0
    IL_0012:  ret
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

} // end of class GitHub_41571.Program

.class private auto ansi beforefieldinit GitHub_41571.Base
       extends [System.Runtime]System.Object
{
  .method public hidebysig instance class GitHub_41571.Derived 
          CrashInCallingScope() cil managed
  {
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  ldnull
    IL_0001:  ret
  } // end of method Base::CrashInCallingScope

  .method public hidebysig newslot virtual 
          instance class GitHub_41571.Base 
          NeededToCrash() cil managed
  {
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  ldnull
    IL_0001:  ret
  } // end of method Base::NeededToCrash

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [System.Runtime]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  } // end of method Base::.ctor

} // end of class GitHub_41571.Base

.class private auto ansi beforefieldinit GitHub_41571.Derived
       extends GitHub_41571.Base
{
  .method public hidebysig newslot virtual 
          instance class GitHub_41571.Derived 
          NeededToCrash() cil managed
  {
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.PreserveBaseOverridesAttribute::.ctor() = ( 01 00 00 00 ) 
    .override  method instance class GitHub_41571.Base GitHub_41571.Base::NeededToCrash()
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  ldnull
    IL_0001:  ret
  } // end of method Derived::NeededToCrash

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void GitHub_41571.Base::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  } // end of method Derived::.ctor

} // end of class GitHub_41571.Derived
