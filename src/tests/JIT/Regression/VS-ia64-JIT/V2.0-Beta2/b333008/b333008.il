// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}





// Metadata version: v2.0.40621
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 2:0:3600:0
}
.assembly 'b333008'
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.assembly extern xunit.core {}
// MVID: {71EBFD38-3F6F-4607-8A53-CC82B4C15BE3}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x00000000054F0000


// =============== CLASS MEMBERS DECLARATION ===================

.class private auto ansi beforefieldinit foo
       extends [mscorlib]System.Object
{
  .field public static int32 x
  .method public hidebysig static int32 Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       11 (0xb)
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  ldc.i4.0
    IL_0001:  stloc.0
    IL_0002:  ldloc.0
    IL_0003:  ldc.i4.1
    IL_0004:  add
    IL_0005:  stloc.0

              ldc.i4.0
              switch(IL_0006)
    IL_0006:  ldloc.0
    IL_0007:  ldc.i4.2
    IL_0008:  blt.s      IL_0002
              ldloc.0
              ldc.i4.2
	      beq SUCCESS
		ldstr "Test FAILURE"
	      	call void [System.Console]System.Console::WriteLine(string)
		ldc.i4 101
		ret
    SUCCESS:
		ldstr "Test SUCCESS"
	      	call void [System.Console]System.Console::WriteLine(string)
		ldc.i4 100
		ret

  } // end of method foo::Main

  .method private hidebysig specialname rtspecialname static 
          void  .cctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldc.i4.0
    IL_0001:  stsfld     int32 foo::x
    IL_0006:  ret
  } // end of method foo::.cctor

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method foo::.ctor

} // end of class foo


// =============================================================

// *********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file foo.res
