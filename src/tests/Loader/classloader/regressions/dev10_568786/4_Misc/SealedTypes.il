// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console { }
.assembly extern xunit.core {}

.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 4:0:0:0
}
.assembly SealedTypes
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::.ctor() = ( 01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
                                                                                                             63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01 )       // ceptionThrows.
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
// MVID: {926F9C9D-6690-45EB-B4DA-12725C405BC3}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x016D0000


// =============== CLASS MEMBERS DECLARATION ===================

.class interface public abstract auto ansi I`1<S>
{
  .method public hidebysig newslot abstract virtual 
          instance string  Print(!S 'value') cil managed
  {
  } // end of method I`1::Print

} // end of class I`1

.class public auto ansi sealed beforefieldinit MyStruct1
       extends [mscorlib]System.Object
       implements class I`1<string>,
                  class I`1<object>
{
  .field public int32 n
  .method public hidebysig newslot virtual final 
          instance string  Print(string 'value') cil managed
  {
    // Code size       25 (0x19)
    .maxstack  3
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  dup
    IL_0003:  ldfld      int32 MyStruct1::n
    IL_0008:  ldc.i4.1
    IL_0009:  add
    IL_000a:  stfld      int32 MyStruct1::n
    IL_000f:  ldstr      "Print(string)"
    IL_0014:  stloc.0
    IL_0015:  br.s       IL_0017

    IL_0017:  ldloc.0
    IL_0018:  ret
  } // end of method MyStruct1::Print

  .method public hidebysig newslot virtual final 
          instance string  Print(object 'value') cil managed
  {
    // Code size       25 (0x19)
    .maxstack  3
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  dup
    IL_0003:  ldfld      int32 MyStruct1::n
    IL_0008:  ldc.i4.1
    IL_0009:  add
    IL_000a:  stfld      int32 MyStruct1::n
    IL_000f:  ldstr      "Print(object)"
    IL_0014:  stloc.0
    IL_0015:  br.s       IL_0017

    IL_0017:  ldloc.0
    IL_0018:  ret
  } // end of method MyStruct1::Print

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method MyStruct1::.ctor

} // end of class MyStruct1

.class public auto ansi sealed beforefieldinit MyStruct2
       extends [mscorlib]System.Object
       implements class I`1<object>,
                  class I`1<string>
{
  .field public int32 n
  .method public hidebysig newslot virtual final 
          instance string  Print(string 'value') cil managed
  {
    // Code size       25 (0x19)
    .maxstack  3
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  dup
    IL_0003:  ldfld      int32 MyStruct2::n
    IL_0008:  ldc.i4.1
    IL_0009:  add
    IL_000a:  stfld      int32 MyStruct2::n
    IL_000f:  ldstr      "Print(string)"
    IL_0014:  stloc.0
    IL_0015:  br.s       IL_0017

    IL_0017:  ldloc.0
    IL_0018:  ret
  } // end of method MyStruct2::Print

  .method public hidebysig newslot virtual final 
          instance string  Print(object 'value') cil managed
  {
    // Code size       25 (0x19)
    .maxstack  3
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  dup
    IL_0003:  ldfld      int32 MyStruct2::n
    IL_0008:  ldc.i4.1
    IL_0009:  add
    IL_000a:  stfld      int32 MyStruct2::n
    IL_000f:  ldstr      "Print(object)"
    IL_0014:  stloc.0
    IL_0015:  br.s       IL_0017

    IL_0017:  ldloc.0
    IL_0018:  ret
  } // end of method MyStruct2::Print

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method MyStruct2::.ctor

} // end of class MyStruct2

.class public auto ansi sealed beforefieldinit MyStruct3`2<T,U>
       extends [mscorlib]System.Object
       implements class I`1<string>,
                  class I`1<object>
{
  .field public int32 n
  .method public hidebysig newslot virtual final 
          instance string  Print(string 'value') cil managed
  {
    // Code size       25 (0x19)
    .maxstack  3
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  dup
    IL_0003:  ldfld      int32 class MyStruct3`2<!T,!U>::n
    IL_0008:  ldc.i4.1
    IL_0009:  add
    IL_000a:  stfld      int32 class MyStruct3`2<!T,!U>::n
    IL_000f:  ldstr      "Print(string)"
    IL_0014:  stloc.0
    IL_0015:  br.s       IL_0017

    IL_0017:  ldloc.0
    IL_0018:  ret
  } // end of method MyStruct3`2::Print

  .method public hidebysig newslot virtual final 
          instance string  Print(object 'value') cil managed
  {
    // Code size       25 (0x19)
    .maxstack  3
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  dup
    IL_0003:  ldfld      int32 class MyStruct3`2<!T,!U>::n
    IL_0008:  ldc.i4.1
    IL_0009:  add
    IL_000a:  stfld      int32 class MyStruct3`2<!T,!U>::n
    IL_000f:  ldstr      "Print(object)"
    IL_0014:  stloc.0
    IL_0015:  br.s       IL_0017

    IL_0017:  ldloc.0
    IL_0018:  ret
  } // end of method MyStruct3`2::Print

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method MyStruct3`2::.ctor

} // end of class MyStruct3`2

.class public auto ansi sealed beforefieldinit MyStruct4`2<T,U>
       extends [mscorlib]System.Object
       implements class I`1<object>,
                  class I`1<string>
{
  .field public int32 n
  .method public hidebysig newslot virtual final 
          instance string  Print(string 'value') cil managed
  {
    // Code size       25 (0x19)
    .maxstack  3
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  dup
    IL_0003:  ldfld      int32 class MyStruct4`2<!T,!U>::n
    IL_0008:  ldc.i4.1
    IL_0009:  add
    IL_000a:  stfld      int32 class MyStruct4`2<!T,!U>::n
    IL_000f:  ldstr      "Print(string)"
    IL_0014:  stloc.0
    IL_0015:  br.s       IL_0017

    IL_0017:  ldloc.0
    IL_0018:  ret
  } // end of method MyStruct4`2::Print

  .method public hidebysig newslot virtual final 
          instance string  Print(object 'value') cil managed
  {
    // Code size       25 (0x19)
    .maxstack  3
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  dup
    IL_0003:  ldfld      int32 class MyStruct4`2<!T,!U>::n
    IL_0008:  ldc.i4.1
    IL_0009:  add
    IL_000a:  stfld      int32 class MyStruct4`2<!T,!U>::n
    IL_000f:  ldstr      "Print(object)"
    IL_0014:  stloc.0
    IL_0015:  br.s       IL_0017

    IL_0017:  ldloc.0
    IL_0018:  ret
  } // end of method MyStruct4`2::Print

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method MyStruct4`2::.ctor

} // end of class MyStruct4`2

.class public auto ansi sealed beforefieldinit MyStruct5`1<T>
       extends [mscorlib]System.Object
       implements class I`1<string>,
                  class I`1<!T>
{
  .field public int32 n
  .method public hidebysig newslot virtual final instance string  Print(string 'value') cil managed
  {
    // Code size       25 (0x19)
    .maxstack  3
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  dup
    IL_0003:  ldfld      int32 class MyStruct5`1<!T>::n
    IL_0008:  ldc.i4.1
    IL_0009:  add
    IL_000a:  stfld      int32 class MyStruct5`1<!T>::n
    IL_000f:  ldstr      "Print(string)"
    IL_0014:  stloc.0
    IL_0015:  br.s       IL_0017

    IL_0017:  ldloc.0
    IL_0018:  ret
  } // end of method MyStruct5`1::Print

  .method public hidebysig newslot virtual final instance string Print(!T 'value') cil managed
  {
    // Code size       45 (0x2d)
    .maxstack  3
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  dup
    IL_0003:  ldfld      int32 class MyStruct5`1<!T>::n
    IL_0008:  ldc.i4.1
    IL_0009:  add
    IL_000a:  stfld      int32 class MyStruct5`1<!T>::n
    IL_000f:  ldstr      "Print_T("
    IL_0014:  ldtoken    !T
    IL_0019:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_001e:  ldstr      ")"
    IL_0023:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0028:  stloc.0
    IL_0029:  br.s       IL_002b

    IL_002b:  ldloc.0
    IL_002c:  ret
  } // end of method MyStruct5`1::Print

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method MyStruct5`1::.ctor

} // end of class MyStruct5`1

.class public auto ansi sealed beforefieldinit MyStruct6`1<T>
       extends [mscorlib]System.Object
       implements class I`1<!T>,
                  class I`1<string>
{
  .field public int32 n
  .method public hidebysig newslot virtual final instance string  Print(string 'value') cil managed
  {
    // Code size       25 (0x19)
    .maxstack  3
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  dup
    IL_0003:  ldfld      int32 class MyStruct6`1<!T>::n
    IL_0008:  ldc.i4.1
    IL_0009:  add
    IL_000a:  stfld      int32 class MyStruct6`1<!T>::n
    IL_000f:  ldstr      "Print(string)"
    IL_0014:  stloc.0
    IL_0015:  br.s       IL_0017

    IL_0017:  ldloc.0
    IL_0018:  ret
  } // end of method MyStruct6`1::Print

  .method public hidebysig newslot virtual final instance string Print(!T 'value') cil managed
  {
    // Code size       45 (0x2d)
    .maxstack  3
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  dup
    IL_0003:  ldfld      int32 class MyStruct6`1<!T>::n
    IL_0008:  ldc.i4.1
    IL_0009:  add
    IL_000a:  stfld      int32 class MyStruct6`1<!T>::n
    IL_000f:  ldstr      "Print_T("
    IL_0014:  ldtoken    !T
    IL_0019:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_001e:  ldstr      ")"
    IL_0023:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0028:  stloc.0
    IL_0029:  br.s       IL_002b

    IL_002b:  ldloc.0
    IL_002c:  ret
  } // end of method MyStruct6`1::Print

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method MyStruct6`1::.ctor

} // end of class MyStruct6`1

.class public auto ansi sealed beforefieldinit MyStruct7`1<T>
       extends [mscorlib]System.Object
       implements class I`1<object>,
                  class I`1<!T>
{
  .field public int32 n
  .method public hidebysig newslot virtual final instance string  Print(object 'value') cil managed
  {
    // Code size       25 (0x19)
    .maxstack  3
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  dup
    IL_0003:  ldfld      int32 class MyStruct7`1<!T>::n
    IL_0008:  ldc.i4.1
    IL_0009:  add
    IL_000a:  stfld      int32 class MyStruct7`1<!T>::n
    IL_000f:  ldstr      "Print(object)"
    IL_0014:  stloc.0
    IL_0015:  br.s       IL_0017

    IL_0017:  ldloc.0
    IL_0018:  ret
  } // end of method MyStruct7`1::Print

  .method public hidebysig newslot virtual final instance string Print(!T 'value') cil managed
  {
    // Code size       45 (0x2d)
    .maxstack  3
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  dup
    IL_0003:  ldfld      int32 class MyStruct7`1<!T>::n
    IL_0008:  ldc.i4.1
    IL_0009:  add
    IL_000a:  stfld      int32 class MyStruct7`1<!T>::n
    IL_000f:  ldstr      "Print_T("
    IL_0014:  ldtoken    !T
    IL_0019:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_001e:  ldstr      ")"
    IL_0023:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0028:  stloc.0
    IL_0029:  br.s       IL_002b

    IL_002b:  ldloc.0
    IL_002c:  ret
  } // end of method MyStruct7`1::Print

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method MyStruct7`1::.ctor

} // end of class MyStruct7`1

.class public auto ansi sealed beforefieldinit MyStruct8`1<T>
       extends [mscorlib]System.Object
       implements class I`1<!T>,
                  class I`1<object>
{
  .field public int32 n
  .method public hidebysig newslot virtual final instance string  Print(object 'value') cil managed
  {
    // Code size       25 (0x19)
    .maxstack  3
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  dup
    IL_0003:  ldfld      int32 class MyStruct8`1<!T>::n
    IL_0008:  ldc.i4.1
    IL_0009:  add
    IL_000a:  stfld      int32 class MyStruct8`1<!T>::n
    IL_000f:  ldstr      "Print(object)"
    IL_0014:  stloc.0
    IL_0015:  br.s       IL_0017

    IL_0017:  ldloc.0
    IL_0018:  ret
  } // end of method MyStruct8`1::Print

  .method public hidebysig newslot virtual final instance string Print(!T 'value') cil managed
  {
    // Code size       45 (0x2d)
    .maxstack  3
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  dup
    IL_0003:  ldfld      int32 class MyStruct8`1<!T>::n
    IL_0008:  ldc.i4.1
    IL_0009:  add
    IL_000a:  stfld      int32 class MyStruct8`1<!T>::n
    IL_000f:  ldstr      "Print_T("
    IL_0014:  ldtoken    !T
    IL_0019:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_001e:  ldstr      ")"
    IL_0023:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0028:  stloc.0
    IL_0029:  br.s       IL_002b

    IL_002b:  ldloc.0
    IL_002c:  ret
  } // end of method MyStruct8`1::Print

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method MyStruct8`1::.ctor

} // end of class MyStruct8`1

.class public auto ansi sealed beforefieldinit MyStruct9`2<T,U>
       extends [mscorlib]System.Object
       implements class I`1<!T>,
                  class I`1<!U>
{
  .field public int32 n
  .method public hidebysig newslot virtual final instance string Print(!T 'value') cil managed
  {
    // Code size       45 (0x2d)
    .maxstack  3
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  dup
    IL_0003:  ldfld      int32 class MyStruct9`2<!T,!U>::n
    IL_0008:  ldc.i4.1
    IL_0009:  add
    IL_000a:  stfld      int32 class MyStruct9`2<!T,!U>::n
    IL_000f:  ldstr      "Print_T("
    IL_0014:  ldtoken    !T
    IL_0019:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_001e:  ldstr      ")"
    IL_0023:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0028:  stloc.0
    IL_0029:  br.s       IL_002b

    IL_002b:  ldloc.0
    IL_002c:  ret
  } // end of method MyStruct9`2::Print

  .method public hidebysig newslot virtual final instance string Print(!U 'value') cil managed
  {
    // Code size       45 (0x2d)
    .maxstack  3
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  dup
    IL_0003:  ldfld      int32 class MyStruct9`2<!T,!U>::n
    IL_0008:  ldc.i4.1
    IL_0009:  add
    IL_000a:  stfld      int32 class MyStruct9`2<!T,!U>::n
    IL_000f:  ldstr      "Print_U("
    IL_0014:  ldtoken    !U
    IL_0019:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_001e:  ldstr      ")"
    IL_0023:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0028:  stloc.0
    IL_0029:  br.s       IL_002b

    IL_002b:  ldloc.0
    IL_002c:  ret
  } // end of method MyStruct9`2::Print

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method MyStruct9`2::.ctor

} // end of class MyStruct9`2

.class public auto ansi sealed beforefieldinit MyStruct10`2<T,U>
       extends [mscorlib]System.Object
       implements class I`1<!U>,
                  class I`1<!T>
{
  .field public int32 n
  .method public hidebysig newslot virtual final instance string Print(!T 'value') cil managed
  {
    // Code size       45 (0x2d)
    .maxstack  3
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  dup
    IL_0003:  ldfld      int32 class MyStruct10`2<!T,!U>::n
    IL_0008:  ldc.i4.1
    IL_0009:  add
    IL_000a:  stfld      int32 class MyStruct10`2<!T,!U>::n
    IL_000f:  ldstr      "Print_T("
    IL_0014:  ldtoken    !T
    IL_0019:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_001e:  ldstr      ")"
    IL_0023:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0028:  stloc.0
    IL_0029:  br.s       IL_002b

    IL_002b:  ldloc.0
    IL_002c:  ret
  } // end of method MyStruct10`2::Print

  .method public hidebysig newslot virtual final instance string Print(!U 'value') cil managed
  {
    // Code size       45 (0x2d)
    .maxstack  3
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  dup
    IL_0003:  ldfld      int32 class MyStruct10`2<!T,!U>::n
    IL_0008:  ldc.i4.1
    IL_0009:  add
    IL_000a:  stfld      int32 class MyStruct10`2<!T,!U>::n
    IL_000f:  ldstr      "Print_U("
    IL_0014:  ldtoken    !U
    IL_0019:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_001e:  ldstr      ")"
    IL_0023:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0028:  stloc.0
    IL_0029:  br.s       IL_002b

    IL_002b:  ldloc.0
    IL_002c:  ret
  } // end of method MyStruct10`2::Print

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method MyStruct10`2::.ctor

} // end of class MyStruct10`2

.class private auto ansi beforefieldinit Conversion`2<.ctor (class I`1<!TString>) TMyStruct,TString>
       extends [mscorlib]System.Object
{
  .field public !TString 'value'
  .method public hidebysig instance string 
          Caller1() cil managed
  {
    // Code size       58 (0x3a)
    .maxstack  2
    .locals init (!TMyStruct V_0,
             string V_1,
             !TMyStruct V_2)
    IL_0000:  nop
    IL_0001:  ldloca.s   V_2
    IL_0003:  initobj    !TMyStruct
    IL_0009:  ldloc.2
    IL_000a:  box        !TMyStruct
    IL_000f:  brfalse.s  IL_001c

    IL_0011:  ldloca.s   V_2
    IL_0013:  initobj    !TMyStruct
    IL_0019:  ldloc.2
    IL_001a:  br.s       IL_0021

    IL_001c:  call       !!0 [mscorlib]System.Activator::CreateInstance<!TMyStruct>()
    IL_0021:  stloc.0
    IL_0022:  ldloca.s   V_0
    IL_0024:  ldarg.0
    IL_0025:  ldfld      !1 class Conversion`2<!TMyStruct,!TString>::'value'
    IL_002a:  constrained. !TMyStruct
    IL_0030:  callvirt   instance string class I`1<!TString>::Print(!0)
    IL_0035:  stloc.1
    IL_0036:  br.s       IL_0038

    IL_0038:  ldloc.1
    IL_0039:  ret
  } // end of method Conversion`2::Caller1

  .method public hidebysig instance string 
          Caller2(!TMyStruct& 'instance') cil managed
  {
    // Code size       24 (0x18)
    .maxstack  2
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldarg.1
    IL_0002:  ldarg.0
    IL_0003:  ldfld      !1 class Conversion`2<!TMyStruct,!TString>::'value'
    IL_0008:  constrained. !TMyStruct
    IL_000e:  callvirt   instance string class I`1<!TString>::Print(!0)
    IL_0013:  stloc.0
    IL_0014:  br.s       IL_0016

    IL_0016:  ldloc.0
    IL_0017:  ret
  } // end of method Conversion`2::Caller2

  .method public hidebysig instance string 
          Caller3(!TMyStruct& 'instance',
                  !TString param) cil managed
  {
    // Code size       19 (0x13)
    .maxstack  2
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldarg.1
    IL_0002:  ldarg.2
    IL_0003:  constrained. !TMyStruct
    IL_0009:  callvirt   instance string class I`1<!TString>::Print(!0)
    IL_000e:  stloc.0
    IL_000f:  br.s       IL_0011

    IL_0011:  ldloc.0
    IL_0012:  ret
  } // end of method Conversion`2::Caller3

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Conversion`2::.ctor

} // end of class Conversion`2

.class private auto ansi beforefieldinit Program
       extends [mscorlib]System.Object
{
  .method private hidebysig static int32 
          Verification(string res1,
                       string res2,
                       string res3,
                       string expected,
                       int32 n) cil managed
  {
    // Code size       150 (0x96)
    .maxstack  2
    .locals init (int32 V_0,
             bool V_1)
    IL_0000:  nop
    IL_0001:  ldstr      "   "
    IL_0006:  ldarg.0
    IL_0007:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_000c:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0011:  nop
    IL_0012:  ldstr      "   "
    IL_0017:  ldarg.1
    IL_0018:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_001d:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0022:  nop
    IL_0023:  ldstr      "   "
    IL_0028:  ldarg.2
    IL_0029:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_002e:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0033:  nop
    IL_0034:  ldstr      "   "
    IL_0039:  ldarg.s    n
    IL_003b:  box        [mscorlib]System.Int32
    IL_0040:  call       string [mscorlib]System.String::Concat(object,
                                                                object)
    IL_0045:  call       void [System.Console]System.Console::WriteLine(string)
    IL_004a:  nop
    IL_004b:  ldarg.0
    IL_004c:  ldarg.3
    IL_004d:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_0052:  brfalse.s  IL_0070

    IL_0054:  ldarg.1
    IL_0055:  ldarg.3
    IL_0056:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_005b:  brfalse.s  IL_0070

    IL_005d:  ldarg.2
    IL_005e:  ldarg.3
    IL_005f:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_0064:  brfalse.s  IL_0070

    IL_0066:  ldarg.s    n
    IL_0068:  ldc.i4.2
    IL_0069:  ceq
    IL_006b:  ldc.i4.0
    IL_006c:  ceq
    IL_006e:  br.s       IL_0071

    IL_0070:  ldc.i4.1
    IL_0071:  stloc.1
    IL_0072:  ldloc.1
    IL_0073:  brtrue.s   IL_0085

    IL_0075:  nop
    IL_0076:  ldstr      "PASS\n"
    IL_007b:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0080:  nop
    IL_0081:  ldc.i4.0
    IL_0082:  stloc.0
    IL_0083:  br.s       IL_0094

    IL_0085:  ldstr      "FAIL\n"
    IL_008a:  call       void [System.Console]System.Console::WriteLine(string)
    IL_008f:  nop
    IL_0090:  ldc.i4.1
    IL_0091:  stloc.0
    IL_0092:  br.s       IL_0094

    IL_0094:  ldloc.0
    IL_0095:  ret
  } // end of method Program::Verification

  .method private hidebysig static int32 
          Main(string[] args) cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       882 (0x372)
    .maxstack  6
    .locals init (int32 V_0,
             class MyStruct1 V_1,
             class MyStruct2 V_2,
             class MyStruct3`2<object,object> V_3,
             class MyStruct4`2<object,object> V_4,
             class MyStruct5`1<object> V_5,
             class MyStruct6`1<object> V_6,
             class MyStruct7`1<string> V_7,
             class MyStruct8`1<string> V_8,
             class MyStruct9`2<string,object> V_9,
             class MyStruct10`2<string,object> V_10,
             class Conversion`2<class MyStruct1,string> V_11,
             class Conversion`2<class MyStruct2,string> V_12,
             class Conversion`2<class MyStruct3`2<object,object>,string> V_13,
             class Conversion`2<class MyStruct4`2<object,object>,string> V_14,
             class Conversion`2<class MyStruct5`1<object>,string> V_15,
             class Conversion`2<class MyStruct6`1<object>,string> V_16,
             class Conversion`2<class MyStruct7`1<string>,string> V_17,
             class Conversion`2<class MyStruct8`1<string>,string> V_18,
             class Conversion`2<class MyStruct9`2<string,object>,string> V_19,
             class Conversion`2<class MyStruct10`2<string,object>,string> V_20,
             string V_21,
             string V_22,
             string V_23,
             int32 V_24)
    IL_0000:  nop
    IL_0001:  ldc.i4.0
    IL_0002:  stloc.0
    IL_0003:  newobj     instance void MyStruct1::.ctor()
    IL_0008:  stloc.1
    IL_0009:  newobj     instance void MyStruct2::.ctor()
    IL_000e:  stloc.2
    IL_000f:  newobj     instance void class MyStruct3`2<object,object>::.ctor()
    IL_0014:  stloc.3
    IL_0015:  newobj     instance void class MyStruct4`2<object,object>::.ctor()
    IL_001a:  stloc.s    V_4
    IL_001c:  newobj     instance void class MyStruct5`1<object>::.ctor()
    IL_0021:  stloc.s    V_5
    IL_0023:  newobj     instance void class MyStruct6`1<object>::.ctor()
    IL_0028:  stloc.s    V_6
    IL_002a:  newobj     instance void class MyStruct7`1<string>::.ctor()
    IL_002f:  stloc.s    V_7
    IL_0031:  newobj     instance void class MyStruct8`1<string>::.ctor()
    IL_0036:  stloc.s    V_8
    IL_0038:  newobj     instance void class MyStruct9`2<string,object>::.ctor()
    IL_003d:  stloc.s    V_9
    IL_003f:  newobj     instance void class MyStruct10`2<string,object>::.ctor()
    IL_0044:  stloc.s    V_10
    IL_0046:  newobj     instance void class Conversion`2<class MyStruct1,string>::.ctor()
    IL_004b:  stloc.s    V_11
    IL_004d:  newobj     instance void class Conversion`2<class MyStruct2,string>::.ctor()
    IL_0052:  stloc.s    V_12
    IL_0054:  newobj     instance void class Conversion`2<class MyStruct3`2<object,object>,string>::.ctor()
    IL_0059:  stloc.s    V_13
    IL_005b:  newobj     instance void class Conversion`2<class MyStruct4`2<object,object>,string>::.ctor()
    IL_0060:  stloc.s    V_14
    IL_0062:  newobj     instance void class Conversion`2<class MyStruct5`1<object>,string>::.ctor()
    IL_0067:  stloc.s    V_15
    IL_0069:  newobj     instance void class Conversion`2<class MyStruct6`1<object>,string>::.ctor()
    IL_006e:  stloc.s    V_16
    IL_0070:  newobj     instance void class Conversion`2<class MyStruct7`1<string>,string>::.ctor()
    IL_0075:  stloc.s    V_17
    IL_0077:  newobj     instance void class Conversion`2<class MyStruct8`1<string>,string>::.ctor()
    IL_007c:  stloc.s    V_18
    IL_007e:  newobj     instance void class Conversion`2<class MyStruct9`2<string,object>,string>::.ctor()
    IL_0083:  stloc.s    V_19
    IL_0085:  newobj     instance void class Conversion`2<class MyStruct10`2<string,object>,string>::.ctor()
    IL_008a:  stloc.s    V_20
    IL_008c:  ldstr      "TEST1:"
    IL_0091:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0096:  nop
    IL_0097:  ldloc.s    V_11
    IL_0099:  callvirt   instance string class Conversion`2<class MyStruct1,string>::Caller1()
    IL_009e:  stloc.s    V_21
    IL_00a0:  ldloc.s    V_11
    IL_00a2:  ldloca.s   V_1
    IL_00a4:  callvirt   instance string class Conversion`2<class MyStruct1,string>::Caller2(!0&)
    IL_00a9:  stloc.s    V_22
    IL_00ab:  ldloc.s    V_11
    IL_00ad:  ldloca.s   V_1
    IL_00af:  ldstr      "mystring"
    IL_00b4:  callvirt   instance string class Conversion`2<class MyStruct1,string>::Caller3(!0&,
                                                                                             !1)
    IL_00b9:  stloc.s    V_23
    IL_00bb:  ldloc.0
    IL_00bc:  ldloc.s    V_21
    IL_00be:  ldloc.s    V_22
    IL_00c0:  ldloc.s    V_23
    IL_00c2:  ldstr      "Print(string)"
    IL_00c7:  ldloc.1
    IL_00c8:  ldfld      int32 MyStruct1::n
    IL_00cd:  call       int32 Program::Verification(string,
                                                     string,
                                                     string,
                                                     string,
                                                     int32)
    IL_00d2:  add
    IL_00d3:  stloc.0
    IL_00d4:  ldstr      "TEST2:"
    IL_00d9:  call       void [System.Console]System.Console::WriteLine(string)
    IL_00de:  nop
    IL_00df:  ldloc.s    V_12
    IL_00e1:  callvirt   instance string class Conversion`2<class MyStruct2,string>::Caller1()
    IL_00e6:  stloc.s    V_21
    IL_00e8:  ldloc.s    V_12
    IL_00ea:  ldloca.s   V_2
    IL_00ec:  callvirt   instance string class Conversion`2<class MyStruct2,string>::Caller2(!0&)
    IL_00f1:  stloc.s    V_22
    IL_00f3:  ldloc.s    V_12
    IL_00f5:  ldloca.s   V_2
    IL_00f7:  ldstr      "mystring"
    IL_00fc:  callvirt   instance string class Conversion`2<class MyStruct2,string>::Caller3(!0&,
                                                                                             !1)
    IL_0101:  stloc.s    V_23
    IL_0103:  ldloc.0
    IL_0104:  ldloc.s    V_21
    IL_0106:  ldloc.s    V_22
    IL_0108:  ldloc.s    V_23
    IL_010a:  ldstr      "Print(string)"
    IL_010f:  ldloc.2
    IL_0110:  ldfld      int32 MyStruct2::n
    IL_0115:  call       int32 Program::Verification(string,
                                                     string,
                                                     string,
                                                     string,
                                                     int32)
    IL_011a:  add
    IL_011b:  stloc.0
    IL_011c:  ldstr      "TEST3:"
    IL_0121:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0126:  nop
    IL_0127:  ldloc.s    V_13
    IL_0129:  callvirt   instance string class Conversion`2<class MyStruct3`2<object,object>,string>::Caller1()
    IL_012e:  stloc.s    V_21
    IL_0130:  ldloc.s    V_13
    IL_0132:  ldloca.s   V_3
    IL_0134:  callvirt   instance string class Conversion`2<class MyStruct3`2<object,object>,string>::Caller2(!0&)
    IL_0139:  stloc.s    V_22
    IL_013b:  ldloc.s    V_13
    IL_013d:  ldloca.s   V_3
    IL_013f:  ldstr      "mystring"
    IL_0144:  callvirt   instance string class Conversion`2<class MyStruct3`2<object,object>,string>::Caller3(!0&,
                                                                                                              !1)
    IL_0149:  stloc.s    V_23
    IL_014b:  ldloc.0
    IL_014c:  ldloc.s    V_21
    IL_014e:  ldloc.s    V_22
    IL_0150:  ldloc.s    V_23
    IL_0152:  ldstr      "Print(string)"
    IL_0157:  ldloc.3
    IL_0158:  ldfld      int32 class MyStruct3`2<object,object>::n
    IL_015d:  call       int32 Program::Verification(string,
                                                     string,
                                                     string,
                                                     string,
                                                     int32)
    IL_0162:  add
    IL_0163:  stloc.0
    IL_0164:  ldstr      "TEST4:"
    IL_0169:  call       void [System.Console]System.Console::WriteLine(string)
    IL_016e:  nop
    IL_016f:  ldloc.s    V_14
    IL_0171:  callvirt   instance string class Conversion`2<class MyStruct4`2<object,object>,string>::Caller1()
    IL_0176:  stloc.s    V_21
    IL_0178:  ldloc.s    V_14
    IL_017a:  ldloca.s   V_4
    IL_017c:  callvirt   instance string class Conversion`2<class MyStruct4`2<object,object>,string>::Caller2(!0&)
    IL_0181:  stloc.s    V_22
    IL_0183:  ldloc.s    V_14
    IL_0185:  ldloca.s   V_4
    IL_0187:  ldstr      "mystring"
    IL_018c:  callvirt   instance string class Conversion`2<class MyStruct4`2<object,object>,string>::Caller3(!0&,
                                                                                                              !1)
    IL_0191:  stloc.s    V_23
    IL_0193:  ldloc.0
    IL_0194:  ldloc.s    V_21
    IL_0196:  ldloc.s    V_22
    IL_0198:  ldloc.s    V_23
    IL_019a:  ldstr      "Print(string)"
    IL_019f:  ldloc.s    V_4
    IL_01a1:  ldfld      int32 class MyStruct4`2<object,object>::n
    IL_01a6:  call       int32 Program::Verification(string,
                                                     string,
                                                     string,
                                                     string,
                                                     int32)
    IL_01ab:  add
    IL_01ac:  stloc.0
    IL_01ad:  ldstr      "TEST5:"
    IL_01b2:  call       void [System.Console]System.Console::WriteLine(string)
    IL_01b7:  nop
    IL_01b8:  ldloc.s    V_15
    IL_01ba:  callvirt   instance string class Conversion`2<class MyStruct5`1<object>,string>::Caller1()
    IL_01bf:  stloc.s    V_21
    IL_01c1:  ldloc.s    V_15
    IL_01c3:  ldloca.s   V_5
    IL_01c5:  callvirt   instance string class Conversion`2<class MyStruct5`1<object>,string>::Caller2(!0&)
    IL_01ca:  stloc.s    V_22
    IL_01cc:  ldloc.s    V_15
    IL_01ce:  ldloca.s   V_5
    IL_01d0:  ldstr      "mystring"
    IL_01d5:  callvirt   instance string class Conversion`2<class MyStruct5`1<object>,string>::Caller3(!0&,
                                                                                                       !1)
    IL_01da:  stloc.s    V_23
    IL_01dc:  ldloc.0
    IL_01dd:  ldloc.s    V_21
    IL_01df:  ldloc.s    V_22
    IL_01e1:  ldloc.s    V_23
    IL_01e3:  ldstr      "Print(string)"
    IL_01e8:  ldloc.s    V_5
    IL_01ea:  ldfld      int32 class MyStruct5`1<object>::n
    IL_01ef:  call       int32 Program::Verification(string,
                                                     string,
                                                     string,
                                                     string,
                                                     int32)
    IL_01f4:  add
    IL_01f5:  stloc.0
    IL_01f6:  ldstr      "TEST6:"
    IL_01fb:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0200:  nop
    IL_0201:  ldloc.s    V_16
    IL_0203:  callvirt   instance string class Conversion`2<class MyStruct6`1<object>,string>::Caller1()
    IL_0208:  stloc.s    V_21
    IL_020a:  ldloc.s    V_16
    IL_020c:  ldloca.s   V_6
    IL_020e:  callvirt   instance string class Conversion`2<class MyStruct6`1<object>,string>::Caller2(!0&)
    IL_0213:  stloc.s    V_22
    IL_0215:  ldloc.s    V_16
    IL_0217:  ldloca.s   V_6
    IL_0219:  ldstr      "mystring"
    IL_021e:  callvirt   instance string class Conversion`2<class MyStruct6`1<object>,string>::Caller3(!0&,
                                                                                                       !1)
    IL_0223:  stloc.s    V_23
    IL_0225:  ldloc.0
    IL_0226:  ldloc.s    V_21
    IL_0228:  ldloc.s    V_22
    IL_022a:  ldloc.s    V_23
    IL_022c:  ldstr      "Print(string)"
    IL_0231:  ldloc.s    V_6
    IL_0233:  ldfld      int32 class MyStruct6`1<object>::n
    IL_0238:  call       int32 Program::Verification(string,
                                                     string,
                                                     string,
                                                     string,
                                                     int32)
    IL_023d:  add
    IL_023e:  stloc.0
    IL_023f:  ldstr      "TEST7:"
    IL_0244:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0249:  nop
    IL_024a:  ldloc.s    V_17
    IL_024c:  callvirt   instance string class Conversion`2<class MyStruct7`1<string>,string>::Caller1()
    IL_0251:  stloc.s    V_21
    IL_0253:  ldloc.s    V_17
    IL_0255:  ldloca.s   V_7
    IL_0257:  callvirt   instance string class Conversion`2<class MyStruct7`1<string>,string>::Caller2(!0&)
    IL_025c:  stloc.s    V_22
    IL_025e:  ldloc.s    V_17
    IL_0260:  ldloca.s   V_7
    IL_0262:  ldstr      "mystring"
    IL_0267:  callvirt   instance string class Conversion`2<class MyStruct7`1<string>,string>::Caller3(!0&,
                                                                                                       !1)
    IL_026c:  stloc.s    V_23
    IL_026e:  ldloc.0
    IL_026f:  ldloc.s    V_21
    IL_0271:  ldloc.s    V_22
    IL_0273:  ldloc.s    V_23
    IL_0275:  ldstr      "Print_T(System.String)"
    IL_027a:  ldloc.s    V_7
    IL_027c:  ldfld      int32 class MyStruct7`1<string>::n
    IL_0281:  call       int32 Program::Verification(string,
                                                     string,
                                                     string,
                                                     string,
                                                     int32)
    IL_0286:  add
    IL_0287:  stloc.0
    IL_0288:  ldstr      "TEST8:"
    IL_028d:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0292:  nop
    IL_0293:  ldloc.s    V_18
    IL_0295:  callvirt   instance string class Conversion`2<class MyStruct8`1<string>,string>::Caller1()
    IL_029a:  stloc.s    V_21
    IL_029c:  ldloc.s    V_18
    IL_029e:  ldloca.s   V_8
    IL_02a0:  callvirt   instance string class Conversion`2<class MyStruct8`1<string>,string>::Caller2(!0&)
    IL_02a5:  stloc.s    V_22
    IL_02a7:  ldloc.s    V_18
    IL_02a9:  ldloca.s   V_8
    IL_02ab:  ldstr      "mystring"
    IL_02b0:  callvirt   instance string class Conversion`2<class MyStruct8`1<string>,string>::Caller3(!0&,
                                                                                                       !1)
    IL_02b5:  stloc.s    V_23
    IL_02b7:  ldloc.0
    IL_02b8:  ldloc.s    V_21
    IL_02ba:  ldloc.s    V_22
    IL_02bc:  ldloc.s    V_23
    IL_02be:  ldstr      "Print_T(System.String)"
    IL_02c3:  ldloc.s    V_8
    IL_02c5:  ldfld      int32 class MyStruct8`1<string>::n
    IL_02ca:  call       int32 Program::Verification(string,
                                                     string,
                                                     string,
                                                     string,
                                                     int32)
    IL_02cf:  add
    IL_02d0:  stloc.0
    IL_02d1:  ldstr      "TEST9:"
    IL_02d6:  call       void [System.Console]System.Console::WriteLine(string)
    IL_02db:  nop
    IL_02dc:  ldloc.s    V_19
    IL_02de:  callvirt   instance string class Conversion`2<class MyStruct9`2<string,object>,string>::Caller1()
    IL_02e3:  stloc.s    V_21
    IL_02e5:  ldloc.s    V_19
    IL_02e7:  ldloca.s   V_9
    IL_02e9:  callvirt   instance string class Conversion`2<class MyStruct9`2<string,object>,string>::Caller2(!0&)
    IL_02ee:  stloc.s    V_22
    IL_02f0:  ldloc.s    V_19
    IL_02f2:  ldloca.s   V_9
    IL_02f4:  ldstr      "mystring"
    IL_02f9:  callvirt   instance string class Conversion`2<class MyStruct9`2<string,object>,string>::Caller3(!0&,
                                                                                                              !1)
    IL_02fe:  stloc.s    V_23
    IL_0300:  ldloc.0
    IL_0301:  ldloc.s    V_21
    IL_0303:  ldloc.s    V_22
    IL_0305:  ldloc.s    V_23
    IL_0307:  ldstr      "Print_T(System.String)"
    IL_030c:  ldloc.s    V_9
    IL_030e:  ldfld      int32 class MyStruct9`2<string,object>::n
    IL_0313:  call       int32 Program::Verification(string,
                                                     string,
                                                     string,
                                                     string,
                                                     int32)
    IL_0318:  add
    IL_0319:  stloc.0
    IL_031a:  ldstr      "TEST10:"
    IL_031f:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0324:  nop
    IL_0325:  ldloc.s    V_20
    IL_0327:  callvirt   instance string class Conversion`2<class MyStruct10`2<string,object>,string>::Caller1()
    IL_032c:  stloc.s    V_21
    IL_032e:  ldloc.s    V_20
    IL_0330:  ldloca.s   V_10
    IL_0332:  callvirt   instance string class Conversion`2<class MyStruct10`2<string,object>,string>::Caller2(!0&)
    IL_0337:  stloc.s    V_22
    IL_0339:  ldloc.s    V_20
    IL_033b:  ldloca.s   V_10
    IL_033d:  ldstr      "mystring"
    IL_0342:  callvirt   instance string class Conversion`2<class MyStruct10`2<string,object>,string>::Caller3(!0&,
                                                                                                               !1)
    IL_0347:  stloc.s    V_23
    IL_0349:  ldloc.0
    IL_034a:  ldloc.s    V_21
    IL_034c:  ldloc.s    V_22
    IL_034e:  ldloc.s    V_23
    IL_0350:  ldstr      "Print_T(System.String)"
    IL_0355:  ldloc.s    V_10
    IL_0357:  ldfld      int32 class MyStruct10`2<string,object>::n
    IL_035c:  call       int32 Program::Verification(string,
                                                     string,
                                                     string,
                                                     string,
                                                     int32)
    IL_0361:  add
    IL_0362:  stloc.0
    IL_0363:  ldloc.0
    IL_0364:  brfalse.s  IL_0369

    IL_0366:  ldc.i4.m1
    IL_0367:  br.s       IL_036b

    IL_0369:  ldc.i4.s   100
    IL_036b:  stloc.s    V_24
    IL_036d:  br.s       IL_036f

    IL_036f:  ldloc.s    V_24
    IL_0371:  ret
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
// WARNING: Created Win32 resource file SealedTypes.res
