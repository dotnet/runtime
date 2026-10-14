// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

// The test reproduces an issue with tail call via slow helper to VSD stub over generic interface.

.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }
.assembly extern System.Diagnostics.Debug {}
.assembly extern System.Console {}
.assembly GitHub_26311 {}
.assembly extern xunit.core {}



.class interface private abstract auto ansi GitHub_26311.Interface`1<TKey>
{
  .method public hidebysig newslot abstract virtual
          instance int32  TailCallWithManyArgs(!TKey[] keys,
                                               int32 index,
                                               int32 length,
                                               !TKey 'value',
                                               object comparer) cil managed noinlining
  {
  } // end of method Interface`1::TailCallWithManyArgs

} // end of class GitHub_26311.Interface`1

.class private auto ansi beforefieldinit GitHub_26311.GenericClassOverInterface`1<T>
       extends [System.Runtime]System.Object
       implements class GitHub_26311.Interface`1<!T>
{
  .field private static initonly class GitHub_26311.Interface`1<!T> s_default
  .method public hidebysig newslot virtual final
          instance int32  TailCallWithManyArgs(!T[] 'array',
                                               int32 index,
                                               int32 length,
                                               !T 'value',
                                               object comparer) cil managed noinlining
  {
    // Code size       149 (0x95)
    .maxstack  4
    IL_0000:  ldarg.1
    IL_0001:  ldnull
    IL_0002:  cgt.un
    IL_0004:  ldstr      "Check the arguments in the caller to BinarySearch:"
    + " array!"
    IL_0009:  call       void [System.Diagnostics.Debug]System.Diagnostics.Debug::Assert(bool,
                                                                                         string)
    IL_000e:  ldarg.2
    IL_000f:  ldc.i4.0
    IL_0010:  clt
    IL_0012:  ldc.i4.0
    IL_0013:  ceq
    IL_0015:  ldstr      "Check the arguments in the caller BinarySearch: in"
    + "dex!"
    IL_001a:  call       void [System.Diagnostics.Debug]System.Diagnostics.Debug::Assert(bool,
                                                                                         string)
    IL_001f:  ldarg.3
    IL_0020:  ldc.i4.0
    IL_0021:  clt
    IL_0023:  ldc.i4.0
    IL_0024:  ceq
    IL_0026:  ldstr      "Check the arguments in the caller BinarySearch: le"
    + "ngth!"
    IL_002b:  call       void [System.Diagnostics.Debug]System.Diagnostics.Debug::Assert(bool,
                                                                                         string)
    IL_0030:  ldarg.1
    IL_0031:  ldlen
    IL_0032:  conv.i4
    IL_0033:  ldarg.2
    IL_0034:  sub
    IL_0035:  ldarg.3
    IL_0036:  bge.s      IL_007d

    IL_0038:  ldc.i4.6
    IL_0039:  newarr     [System.Runtime]System.Object
    IL_003e:  dup
    IL_003f:  ldc.i4.0
    IL_0040:  ldstr      "index: "
    IL_0045:  stelem.ref
    IL_0046:  dup
    IL_0047:  ldc.i4.1
    IL_0048:  ldarg.2
    IL_0049:  box        [System.Runtime]System.Int32
    IL_004e:  stelem.ref
    IL_004f:  dup
    IL_0050:  ldc.i4.2
    IL_0051:  ldstr      ", length: "
    IL_0056:  stelem.ref
    IL_0057:  dup
    IL_0058:  ldc.i4.3
    IL_0059:  ldarg.3
    IL_005a:  box        [System.Runtime]System.Int32
    IL_005f:  stelem.ref
    IL_0060:  dup
    IL_0061:  ldc.i4.4
    IL_0062:  ldstr      ", array length: "
    IL_0067:  stelem.ref
    IL_0068:  dup
    IL_0069:  ldc.i4.5
    IL_006a:  ldarg.1
    IL_006b:  ldlen
    IL_006c:  conv.i4
    IL_006d:  box        [System.Runtime]System.Int32
    IL_0072:  stelem.ref
    IL_0073:  call       string [System.Runtime]System.String::Concat(object[])
    IL_0078:  call       void [System.Console]System.Console::WriteLine(string)
    IL_007d:  ldarg.1
    IL_007e:  ldlen
    IL_007f:  conv.i4
    IL_0080:  ldarg.2
    IL_0081:  sub
    IL_0082:  ldarg.3
    IL_0083:  clt
    IL_0085:  ldc.i4.0
    IL_0086:  ceq
    IL_0088:  ldstr      "Check the arguments in the caller BinarySearch for"
    + " array.Length - index >= length!"
    IL_008d:  call       void [System.Diagnostics.Debug]System.Diagnostics.Debug::Assert(bool,
                                                                                         string)
    IL_0092:  ldc.i4.s   100
    IL_0094:  ret
  } // end of method GenericClassOverInterface`1::TailCallWithManyArgs

  .method public hidebysig specialname static
          class GitHub_26311.Interface`1<!T>
          get_Default() cil managed
  {
    // Code size       6 (0x6)
    .maxstack  8
    IL_0000:  ldsfld     class GitHub_26311.Interface`1<!0> class GitHub_26311.GenericClassOverInterface`1<!T>::s_default
    IL_0005:  ret
  } // end of method GenericClassOverInterface`1::get_Default

  .method private hidebysig static class GitHub_26311.Interface`1<!T>
          CreateGenericClassOverInterface() cil managed noinlining
  {
    // Code size       6 (0x6)
    .maxstack  8
    IL_0000:  newobj     instance void class GitHub_26311.GenericClassOverInterface`1<!T>::.ctor()
    IL_0005:  ret
  } // end of method GenericClassOverInterface`1::CreateGenericClassOverInterface

  .method public hidebysig specialname rtspecialname
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [System.Runtime]System.Object::.ctor()
    IL_0006:  ret
  } // end of method GenericClassOverInterface`1::.ctor

  .method private hidebysig specialname rtspecialname static
          void  .cctor() cil managed
  {
    // Code size       11 (0xb)
    .maxstack  8
    IL_0000:  call       class GitHub_26311.Interface`1<!0> class GitHub_26311.GenericClassOverInterface`1<!T>::CreateGenericClassOverInterface()
    IL_0005:  stsfld     class GitHub_26311.Interface`1<!0> class GitHub_26311.GenericClassOverInterface`1<!T>::s_default
    IL_000a:  ret
  } // end of method GenericClassOverInterface`1::.cctor

  .property class GitHub_26311.Interface`1<!T>
          Default()
  {
    .get class GitHub_26311.Interface`1<!T> GitHub_26311.GenericClassOverInterface`1::get_Default()
  } // end of property GenericClassOverInterface`1::Default
} // end of class GitHub_26311.GenericClassOverInterface`1

.class public auto ansi beforefieldinit GitHub_26311.Program
       extends [System.Runtime]System.Object
{
  .method public hidebysig static int32  Test() cil managed noinlining
  {
    // Code size       104 (0x68)
    .maxstack  6
    .locals init (int32[] V_0,
             int32 V_1,
             int32 V_2,
             int32 V_3)
    IL_0000:  ldc.i4.6
    IL_0001:  newarr     [System.Runtime]System.Int32
    IL_0006:  dup
    IL_0007:  ldtoken    field valuetype '<PrivateImplementationDetails>'/'__StaticArrayInitTypeSize=24' '<PrivateImplementationDetails>'::DB17E883A647963A26D973378923EF4649801319
    IL_000c:  call       void [System.Runtime]System.Runtime.CompilerServices.RuntimeHelpers::InitializeArray(class [System.Runtime]System.Array,
                                                                                                              valuetype [System.Runtime]System.RuntimeFieldHandle)
    IL_0011:  stloc.0
    IL_0012:  ldc.i4.s   9
    IL_0014:  stloc.1
    IL_0015:  ldc.i4.0
    IL_0016:  stloc.2
    IL_0017:  ldc.i4.5
    IL_0018:  stloc.3
    IL_0019:  ldloc.0
    IL_001a:  brtrue.s   IL_0027

    IL_001c:  ldstr      "array == null"
    IL_0021:  newobj     instance void [System.Runtime]System.Exception::.ctor(string)
    IL_0026:  throw

    IL_0027:  ldloc.2
    IL_0028:  ldc.i4.0
    IL_0029:  bge.s      IL_0036

    IL_002b:  ldstr      "index < 0"
    IL_0030:  newobj     instance void [System.Runtime]System.Exception::.ctor(string)
    IL_0035:  throw

    IL_0036:  ldloc.3
    IL_0037:  ldc.i4.0
    IL_0038:  bge.s      IL_0045

    IL_003a:  ldstr      "length < 0"
    IL_003f:  newobj     instance void [System.Runtime]System.Exception::.ctor(string)
    IL_0044:  throw

    IL_0045:  ldloc.0
    IL_0046:  ldlen
    IL_0047:  conv.i4
    IL_0048:  ldloc.2
    IL_0049:  sub
    IL_004a:  ldloc.3
    IL_004b:  bge.s      IL_0058

    IL_004d:  ldstr      "array.Length - index < length"
    IL_0052:  newobj     instance void [System.Runtime]System.Exception::.ctor(string)
    IL_0057:  throw

    IL_0058:  call       class GitHub_26311.Interface`1<!0> class GitHub_26311.GenericClassOverInterface`1<int32>::get_Default()
    IL_005d:  ldloc.0
    IL_005e:  ldloc.2
    IL_005f:  ldloc.3
    IL_0060:  ldloc.1
    IL_0061:  ldnull
    IL_0062:  tail. // tail call to interface method over generic with many arguments.
    IL_0063:  callvirt   instance int32 class GitHub_26311.Interface`1<int32>::TailCallWithManyArgs(!0[],
                                                                                                    int32,
                                                                                                    int32,
                                                                                                    !0,
                                                                                                    object)
    IL_0067:  ret
  } // end of method Program::Test

  .method public hidebysig static int32
          Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       26 (0x1a)
    .maxstack  8
    IL_0000:  call       int32 GitHub_26311.Program::Test()
    IL_0005:  dup
    IL_0006:  ldc.i4.s   100
    IL_0008:  ceq
    IL_000a:  call       void [System.Diagnostics.Debug]System.Diagnostics.Debug::Assert(bool)
    IL_000f:  ldstr      "Passed"
    IL_0014:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0019:  ret
  } // end of method Program::Main

  .method public hidebysig specialname rtspecialname
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [System.Runtime]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Program::.ctor

} // end of class GitHub_26311.Program

.class private auto ansi sealed '<PrivateImplementationDetails>'
       extends [System.Runtime]System.Object
{
  .custom instance void [System.Runtime]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
  .class explicit ansi sealed nested private '__StaticArrayInitTypeSize=24'
         extends [System.Runtime]System.ValueType
  {
    .pack 1
    .size 24
  } // end of class '__StaticArrayInitTypeSize=24'

  .field static assembly initonly valuetype '<PrivateImplementationDetails>'/'__StaticArrayInitTypeSize=24' DB17E883A647963A26D973378923EF4649801319 at I_00002B7C
} // end of class '<PrivateImplementationDetails>'


// =============================================================

.data cil I_00002B7C = bytearray (
                 00 00 00 00 01 00 00 00 02 00 00 00 03 00 00 00
                 04 00 00 00 05 00 00 00)
