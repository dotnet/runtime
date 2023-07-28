// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}



// Metadata version: v2.0.20609
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 2:0:0:0
}
.assembly 'b176032'
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::.ctor() = ( 01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
                                                                                                             63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01 )       // ceptionThrows.
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.assembly extern xunit.core {}
// MVID: {67428BBB-6A8A-45FA-BAF9-F8CACEC55247}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x004C0000


.class interface public abstract auto ansi IWebPropertyAccessor
{
  .method public hidebysig newslot abstract virtual 
          instance object  GetProperty(object target) cil managed
  {
  } // end of method IWebPropertyAccessor::GetProperty

  .method public hidebysig newslot abstract virtual 
          instance void  SetProperty(object target,
                                     object 'value') cil managed
  {
  } // end of method IWebPropertyAccessor::SetProperty

} // end of class IWebPropertyAccessor

.class public auto ansi beforefieldinit My
       extends [mscorlib]System.Object
{
  .field private valuetype [mscorlib]System.Nullable`1<bool> f
  .method public hidebysig specialname instance valuetype [mscorlib]System.Nullable`1<bool> 
          get_MyProperty() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldfld      valuetype [mscorlib]System.Nullable`1<bool> My::f
    IL_0006:  ret
  } // end of method My::get_MyProperty

  .method public hidebysig specialname instance void 
          set_MyProperty(valuetype [mscorlib]System.Nullable`1<bool> 'value') cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  stfld      valuetype [mscorlib]System.Nullable`1<bool> My::f
    IL_0007:  ret
  } // end of method My::set_MyProperty

  .method public hidebysig static int32  Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       41 (0x29)
    .maxstack  3
    .locals init (class IWebPropertyAccessor V_0,
             class My V_1)
    IL_000f:  newobj     instance void MyProperty::.ctor()
    IL_0014:  stloc.0
    IL_0015:  newobj     instance void My::.ctor()
    IL_001a:  stloc.1
    IL_001b:  ldloc.0
    IL_001c:  ldloc.1
    IL_001d:  ldc.i4.1
    IL_001e:  box        [mscorlib]System.Boolean
    IL_0023:  callvirt   instance void IWebPropertyAccessor::SetProperty(object,
                                                                             object)
              ldstr "!!! TEST PASSED !!!"
              call       void [System.Console]System.Console::WriteLine(string)
              ldc.i4 100
    IL_0028:  ret
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

  .property instance valuetype [mscorlib]System.Nullable`1<bool>
          MyProperty()
  {
    .get instance valuetype [mscorlib]System.Nullable`1<bool> My::get_MyProperty()
    .set instance void My::set_MyProperty(valuetype [mscorlib]System.Nullable`1<bool>)
  } // end of property My::MyProperty
} // end of class My



.class public auto ansi MyProperty
       extends [mscorlib]System.Object
       implements IWebPropertyAccessor
{
  .method public virtual instance object 
          GetProperty(object A_1) cil managed
  {
    .override IWebPropertyAccessor::GetProperty
    // Code size       17 (0x11)
    .maxstack  1
    IL_0000:  ldarg.1
    IL_0001:  castclass  My
    IL_0006:  callvirt   instance valuetype [mscorlib]System.Nullable`1<bool> [b176032]My::get_MyProperty()
    IL_000b:  box        valuetype [mscorlib]System.Nullable`1<bool>
    IL_0010:  ret
  } // end of method MyProperty::GetProperty

  .method public virtual instance void  SetProperty(object A_1,
                                                    object A_2) cil managed
  {
    .override IWebPropertyAccessor::SetProperty
    // Code size       23 (0x17)
    .maxstack  2
    IL_0000:  ldarg.1
    IL_0001:  castclass  My
    IL_0006:  ldarg.2
    IL_0007:  unbox      valuetype [mscorlib]System.Nullable`1<bool>
    IL_000c:  ldobj      valuetype [mscorlib]System.Nullable`1<bool>
    IL_0011:  callvirt   instance void My::set_MyProperty(valuetype [mscorlib]System.Nullable`1<bool>)
    IL_0016:  ret
  } // end of method MyProperty::SetProperty

  .method public specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  2
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method MyProperty::.ctor

} // end of class MyProperty

// =============================================================

// *********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file fastpropertyaccessor.res
