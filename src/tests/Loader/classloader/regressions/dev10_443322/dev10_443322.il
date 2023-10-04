// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console { }
.assembly extern xunit.core {}
//
// Non-virtual call of System.Type.TypeHandle on result of ldtoken should not trigger typeof(XXX).TypeHandle optimization
//

/*

using System;

class My {

static int Main() {
    try {
        typeof(My).TypeHandle.ToString(); // Change to call get_TypeHandle non-virtually
    }
    catch (Exception e) {
        Console.WriteLine("PASSED");
        return 100;   
    }
    Console.WriteLine("FAILED");
    return 1;
}

}

*/

.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 4:0:0:0
}
.assembly 'dev10_443322'
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::.ctor() = ( 01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
                                                                                                             63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01 )       // ceptionThrows.
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
// MVID: {EDF83A15-9E74-47F0-B94B-197894607E38}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x004D0000


// =============== CLASS MEMBERS DECLARATION ===================

.class private auto ansi beforefieldinit My
       extends [mscorlib]System.Object
{
  .method private hidebysig static int32 
          Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       62 (0x3e)
    .maxstack  1
    .locals init (int32 V_0,
             valuetype [mscorlib]System.RuntimeTypeHandle V_1)
    .try
    {
      IL_0000:  ldtoken    My
      IL_0005:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
//      IL_000a:  callvirt   instance valuetype [mscorlib]System.RuntimeTypeHandle [mscorlib]System.Type::get_TypeHandle()
      IL_000a:  call   instance valuetype [mscorlib]System.RuntimeTypeHandle [mscorlib]System.Type::get_TypeHandle()
      IL_000f:  stloc.1
      IL_0010:  ldloca.s   V_1
      IL_0012:  constrained. [mscorlib]System.RuntimeTypeHandle
      IL_0018:  callvirt   instance string [mscorlib]System.Object::ToString()
      IL_001d:  pop
      IL_001e:  leave.s    IL_0030

    }  // end .try
    catch [mscorlib]System.Exception 
    {
      IL_0020:  pop
      IL_0021:  ldstr      "PASSED"
      IL_0026:  call       void [System.Console]System.Console::WriteLine(string)
      IL_002b:  ldc.i4.s   100
      IL_002d:  stloc.0
      IL_002e:  leave.s    IL_003c

    }  // end handler
    IL_0030:  ldstr      "FAILED"
    IL_0035:  call       void [System.Console]System.Console::WriteLine(string)
    IL_003a:  ldc.i4.1
    IL_003b:  ret

    IL_003c:  ldloc.0
    IL_003d:  ret
  } // end of method My::Main

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method My::.ctor

} // end of class My


// =============================================================

// *********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file bug443322.res
