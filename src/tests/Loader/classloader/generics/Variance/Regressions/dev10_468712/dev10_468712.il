// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console { }
.assembly extern xunit.core {}

.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 4:0:0:0
}
.assembly dev10_468712
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::.ctor() = ( 01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
                                                                                                             63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01 )       // ceptionThrows.

  // --- The following custom attribute is added automatically, do not uncomment -------
  //  .custom instance void [mscorlib]System.Diagnostics.DebuggableAttribute::.ctor(valuetype [mscorlib]System.Diagnostics.DebuggableAttribute/DebuggingModes) = ( 01 00 07 01 00 00 00 00 ) 

  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
// MVID: {A08CCC44-9903-487B-892B-8613075988F7}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x03EC0000


// =============== CLASS MEMBERS DECLARATION ===================


.class interface private abstract auto ansi ConsoleApplicationVB.ICovariant`1<+ T>
{
  .method public newslot abstract strict virtual instance void  f<U>() cil managed  { }

} 


.class private auto ansi ConsoleApplicationVB.C`1<T> extends [mscorlib]System.Object implements class ConsoleApplicationVB.ICovariant`1<!T>
{
  .method public specialname rtspecialname instance void  .ctor() cil managed
  {
    .custom instance void [mscorlib]System.Diagnostics.DebuggerNonUserCodeAttribute::.ctor() = ( 01 00 00 00 ) 
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  nop
    IL_0008:  ret
  }

  .method public newslot strict virtual final instance void  f<U>() cil managed
  {
    .override  method instance void class ConsoleApplicationVB.ICovariant`1<!T>::f<[1]>()
    .maxstack  8
    IL_0000:  nop
    IL_0001:  nop
    IL_0002:  ret
  } 
} 


.class private auto ansi sealed ConsoleApplicationVB.Module1 extends [mscorlib]System.Object
{
  .method public static int32  Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    .maxstack  1
    .locals init ([0] class ConsoleApplicationVB.ICovariant`1<object> y)
    IL_0000:  nop
    IL_0001:  newobj     instance void class ConsoleApplicationVB.C`1<string>::.ctor()
    IL_0006:  stloc.0
    IL_0007:  ldloc.0
    IL_0008:  callvirt   instance void class ConsoleApplicationVB.ICovariant`1<object>::f<class [mscorlib]System.Exception>()
    IL_000d:  nop
    
	ldstr "PASS"
	call void class [System.Console]System.Console::WriteLine(string)
	
    ldc.i4.s 100
	ret
  }
} 


// =============================================================

