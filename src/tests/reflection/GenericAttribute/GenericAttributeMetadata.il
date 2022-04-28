// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { }
.assembly GenericAttributeMetadata
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::.ctor() = ( 01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
                                                                                                                           63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01 )       // ceptionThrows.

  // --- The following custom attribute is added automatically, do not uncomment -------
  //  .custom instance void [mscorlib]System.Diagnostics.DebuggableAttribute::.ctor(valuetype [mscorlib]System.Diagnostics.DebuggableAttribute/DebuggingModes) = ( 01 00 07 01 00 00 00 00 ) 
/*  Re-enable once the fix to https://github.com/dotnet/msbuild/issues/6734 propagates to this repo.
  .custom instance void class SingleAttribute`1<int32>::.ctor() = ( 01 00 00 00 ) 
  .custom instance void class SingleAttribute`1<bool>::.ctor() = ( 01 00 00 00 ) 
  .custom instance void class MultiAttribute`1<int32>::.ctor() = ( 01 00 00 00 ) 
  .custom instance void class MultiAttribute`1<int32>::.ctor(!0) = ( 01 00 01 00 00 00 00 00 ) 
  .custom instance void class MultiAttribute`1<int32>::.ctor() = ( 01 00 01 00 54 08 05 56 61 6C 75 65 02 00 00 00 ) // ....T..Value....
  .custom instance void class MultiAttribute`1<bool>::.ctor() = ( 01 00 00 00 ) 
  .custom instance void class MultiAttribute`1<bool>::.ctor(!0) = ( 01 00 01 00 00 ) */
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.module GenericAttributeMetadata.dll
// MVID: {97ADA31C-7529-4F22-9907-B16BE1E0AF0B}
.imagebase 0x10000000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x0000025583AE0000


// =============== CLASS MEMBERS DECLARATION ===================

.class public auto ansi beforefieldinit SingleAttribute`1<T>
       extends [mscorlib]System.Attribute
{
  .custom instance void [mscorlib]System.AttributeUsageAttribute::.ctor(valuetype [mscorlib]System.AttributeTargets) = ( 01 00 85 00 00 00 01 00 54 02 0D 41 6C 6C 6F 77   // ........T..Allow
                                                                                                                                                     4D 75 6C 74 69 70 6C 65 00 )                      // Multiple.
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Attribute::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  } // end of method SingleAttribute`1::.ctor

} // end of class SingleAttribute`1

.class public auto ansi beforefieldinit MultiAttribute`1<T>
       extends [mscorlib]System.Attribute
{
  .custom instance void [mscorlib]System.AttributeUsageAttribute::.ctor(valuetype [mscorlib]System.AttributeTargets) = ( 01 00 85 00 00 00 01 00 54 02 0D 41 6C 6C 6F 77   // ........T..Allow
                                                                                                                                                     4D 75 6C 74 69 70 6C 65 01 )                      // Multiple.
  .field private !T '<Value>k__BackingField'
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
  .custom instance void [mscorlib]System.Diagnostics.DebuggerBrowsableAttribute::.ctor(valuetype [mscorlib]System.Diagnostics.DebuggerBrowsableState) = ( 01 00 00 00 00 00 00 00 ) 
  .method public hidebysig specialname instance !T 
          get_Value() cil managed
  {
    .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldfld      !0 class MultiAttribute`1<!T>::'<Value>k__BackingField'
    IL_0006:  ret
  } // end of method MultiAttribute`1::get_Value

  .method public hidebysig specialname instance void 
          set_Value(!T 'value') cil managed
  {
    .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  stfld      !0 class MultiAttribute`1<!T>::'<Value>k__BackingField'
    IL_0007:  ret
  } // end of method MultiAttribute`1::set_Value

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       9 (0x9)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Attribute::.ctor()
    IL_0006:  nop
    IL_0007:  nop
    IL_0008:  ret
  } // end of method MultiAttribute`1::.ctor

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(!T 'value') cil managed
  {
    // Code size       17 (0x11)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Attribute::.ctor()
    IL_0006:  nop
    IL_0007:  nop
    IL_0008:  ldarg.0
    IL_0009:  ldarg.1
    IL_000a:  call       instance void class MultiAttribute`1<!T>::set_Value(!0)
    IL_000f:  nop
    IL_0010:  ret
  } // end of method MultiAttribute`1::.ctor

  .property instance !T Value()
  {
    .get instance !T MultiAttribute`1::get_Value()
    .set instance void MultiAttribute`1::set_Value(!T)
  } // end of property MultiAttribute`1::Value
} // end of class MultiAttribute`1

.class public auto ansi sealed MyEnum
       extends [mscorlib]System.Enum
{
  .field public specialname rtspecialname int32 value__
  .field public static literal valuetype MyEnum Ctor = int32(0x00000000)
  .field public static literal valuetype MyEnum Property = int32(0x00000001)
} // end of class MyEnum

.class public auto ansi beforefieldinit Class
       extends [mscorlib]System.Object
{
  .custom instance void class SingleAttribute`1<int32>::.ctor() = ( 01 00 00 00 ) 
  .custom instance void class SingleAttribute`1<bool>::.ctor() = ( 01 00 00 00 ) 
  .custom instance void class MultiAttribute`1<int32>::.ctor() = ( 01 00 00 00 ) 
  .custom instance void class MultiAttribute`1<int32>::.ctor(!0) = ( 01 00 01 00 00 00 00 00 ) 
  .custom instance void class MultiAttribute`1<int32>::.ctor() = ( 01 00 01 00 54 08 05 56 61 6C 75 65 02 00 00 00 ) // ....T..Value....
  .custom instance void class MultiAttribute`1<bool>::.ctor() = ( 01 00 00 00 ) 
  .custom instance void class MultiAttribute`1<bool>::.ctor(!0) = ( 01 00 01 00 00 ) 
  .custom instance void class MultiAttribute`1<bool>::.ctor() = ( 01 00 01 00 54 02 05 56 61 6C 75 65 01 )          // ....T..Value.
  .custom instance void class MultiAttribute`1<valuetype [mscorlib]System.Nullable`1<bool>>::.ctor() = ( 01 00 00 00 ) 
  .custom instance void class MultiAttribute`1<string>::.ctor(!0) = ( 01 00 04 43 74 6F 72 00 00 )                      // ...Ctor..
  .custom instance void class MultiAttribute`1<string>::.ctor() = ( 01 00 01 00 54 0E 05 56 61 6C 75 65 08 50 72 6F   // ....T..Value.Pro
                                                                    70 65 72 74 79 )                                  // perty
  .custom instance void class MultiAttribute`1<class [mscorlib]System.Type>::.ctor(!0) = ( 01 00 05 43 6C 61 73 73 00 00 )                   // ...Class..
  .custom instance void class MultiAttribute`1<class [mscorlib]System.Type>::.ctor() = ( 01 00 01 00 54 50 05 56 61 6C 75 65 0C 43 6C 61   // ....TP.Value.Cla
                                                                                                       73 73 2B 44 65 72 69 76 65 )                      // ss+Derive
  .custom instance void class MultiAttribute`1<valuetype MyEnum>::.ctor(!0) = ( 01 00 00 00 00 00 00 00 ) 
  .custom instance void class MultiAttribute`1<valuetype MyEnum>::.ctor() = ( 01 00 01 00 54 55 06 4D 79 45 6E 75 6D 05 56 61   // ....TU.MyEnum.Va
                                                                              6C 75 65 01 00 00 00 )                            // lue....
  .class auto ansi nested public beforefieldinit Derive
         extends Class
  {
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      // Code size       8 (0x8)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void Class::.ctor()
      IL_0006:  nop
      IL_0007:  ret
    } // end of method Derive::.ctor

  } // end of class Derive

  .field private int32 '<Property>k__BackingField'
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
  .custom instance void [mscorlib]System.Diagnostics.DebuggerBrowsableAttribute::.ctor(valuetype [mscorlib]System.Diagnostics.DebuggerBrowsableState) = ( 01 00 00 00 00 00 00 00 ) 
  .method public hidebysig specialname instance int32 
          get_Property() cil managed
  {
    .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldfld      int32 Class::'<Property>k__BackingField'
    IL_0006:  ret
  } // end of method Class::get_Property

  .method public hidebysig specialname instance void 
          set_Property(int32 'value') cil managed
  {
    .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  stfld      int32 Class::'<Property>k__BackingField'
    IL_0007:  ret
  } // end of method Class::set_Property

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  } // end of method Class::.ctor

  .property instance int32 Property()
  {
    .custom instance void class SingleAttribute`1<int32>::.ctor() = ( 01 00 00 00 ) 
    .custom instance void class SingleAttribute`1<bool>::.ctor() = ( 01 00 00 00 ) 
    .custom instance void class MultiAttribute`1<int32>::.ctor() = ( 01 00 00 00 ) 
    .custom instance void class MultiAttribute`1<int32>::.ctor(!0) = ( 01 00 01 00 00 00 00 00 ) 
    .custom instance void class MultiAttribute`1<int32>::.ctor() = ( 01 00 01 00 54 08 05 56 61 6C 75 65 02 00 00 00 ) // ....T..Value....
    .custom instance void class MultiAttribute`1<bool>::.ctor() = ( 01 00 00 00 ) 
    .custom instance void class MultiAttribute`1<bool>::.ctor(!0) = ( 01 00 01 00 00 ) 
    .get instance int32 Class::get_Property()
    .set instance void Class::set_Property(int32)
  } // end of property Class::Property
} // end of class Class


// =============================================================

// *********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file GenericAttributeMetadata.res
