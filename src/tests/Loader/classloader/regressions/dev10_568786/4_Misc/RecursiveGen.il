// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console { }
.assembly extern xunit.core {}

.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 4:0:0:0
}
.assembly RecursiveGen
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::.ctor() = ( 01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
                                                                                                             63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01 )       // ceptionThrows.
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
// MVID: {CCE38E80-73AC-4FAC-A7CD-8B523BD6C64F}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x00870000


// =============== CLASS MEMBERS DECLARATION ===================

.class interface private abstract auto ansi I`1<S>
{
  .method public hidebysig newslot abstract virtual 
          instance string  Method() cil managed
  {
  } // end of method I`1::Method

} // end of class I`1

.class private sequential ansi sealed beforefieldinit MyStruct
       extends [mscorlib]System.ValueType
       implements class I`1<valuetype MyStruct>,
                  class I`1<object>
{
  .field public int32 n
  .method private hidebysig newslot virtual final 
          instance string  'I<MyStruct>.Method'() cil managed
  {
    .override  method instance string class I`1<valuetype MyStruct>::Method()
    // Code size       25 (0x19)
    .maxstack  3
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  dup
    IL_0003:  ldfld      int32 MyStruct::n
    IL_0008:  ldc.i4.1
    IL_0009:  add
    IL_000a:  stfld      int32 MyStruct::n
    IL_000f:  ldstr      "I<MyStruct>.Method()"
    IL_0014:  stloc.0
    IL_0015:  br.s       IL_0017

    IL_0017:  ldloc.0
    IL_0018:  ret
  } // end of method MyStruct::'I<MyStruct>.Method'

  .method private hidebysig newslot virtual final 
          instance string  'I<System.Object>.Method'() cil managed
  {
    .override  method instance string class I`1<object>::Method()
    // Code size       25 (0x19)
    .maxstack  3
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  dup
    IL_0003:  ldfld      int32 MyStruct::n
    IL_0008:  ldc.i4.1
    IL_0009:  add
    IL_000a:  stfld      int32 MyStruct::n
    IL_000f:  ldstr      "I<object>.Method()"
    IL_0014:  stloc.0
    IL_0015:  br.s       IL_0017

    IL_0017:  ldloc.0
    IL_0018:  ret
  } // end of method MyStruct::'I<System.Object>.Method'

} // end of class MyStruct

.class private sequential ansi sealed beforefieldinit GenStruct`2<T,U>
       extends [mscorlib]System.ValueType
       implements 	class I`1<valuetype GenStruct`2<!T,!U>>,
					class I`1<valuetype GenStruct`2<!U,!T>>
{
  .field public int32 n
  .method private hidebysig newslot virtual final instance string  'I<GenStruct<T,U>>.Method'() cil managed
  {
    .override  method instance string class I`1<valuetype GenStruct`2<!T,!U>>::Method()
    // Code size       25 (0x19)
    .maxstack  3
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  dup
    IL_0003:  ldfld      int32 valuetype GenStruct`2<!T,!U>::n
    IL_0008:  ldc.i4.1
    IL_0009:  add
    IL_000a:  stfld      int32 valuetype GenStruct`2<!T,!U>::n
    IL_000f:  ldstr      "I<GenStruct<T, U>>.Method()"
    IL_0014:  stloc.0
    IL_0015:  br.s       IL_0017

    IL_0017:  ldloc.0
    IL_0018:  ret
  } // end of method GenStruct`2::'I<GenStruct<T,U>>.Method'

  .method private hidebysig newslot virtual final instance string  'I<GenStruct<U,T>>.Method'() cil managed
  {
    .override  method instance string class I`1<valuetype GenStruct`2<!U,!T>>::Method()
    // Code size       25 (0x19)
    .maxstack  3
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  dup
    IL_0003:  ldfld      int32 valuetype GenStruct`2<!U,!T>::n
    IL_0008:  ldc.i4.1
    IL_0009:  add
    IL_000a:  stfld      int32 valuetype GenStruct`2<!U,!T>::n
    IL_000f:  ldstr      "I<GenStruct<U, T>>.Method()"
    IL_0014:  stloc.0
    IL_0015:  br.s       IL_0017

    IL_0017:  ldloc.0
    IL_0018:  ret
  } // end of method GenStruct`2::'I<GenStruct<T,U>>.Method'

  } // end of class GenStruct`2

.class private auto ansi beforefieldinit Conversion1`2<T,(class I`1<!T>) U>
       extends [mscorlib]System.Object
{
  .method public hidebysig instance string 
          Caller(!U& 'instance') cil managed
  {
    // Code size       18 (0x12)
    .maxstack  1
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldarg.1
    IL_0002:  constrained. !U
    IL_0008:  callvirt   instance string class I`1<!T>::Method()
    IL_000d:  stloc.0
    IL_000e:  br.s       IL_0010

    IL_0010:  ldloc.0
    IL_0011:  ret
  } // end of method Conversion1`2::Caller

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Conversion1`2::.ctor

} // end of class Conversion1`2

.class private auto ansi beforefieldinit Conversion2`3<T,U,(class I`1<valuetype GenStruct`2<!T,!U>>) V>
       extends [mscorlib]System.Object
{
  .method public hidebysig instance string 
          Caller(!V& 'instance') cil managed
  {
    // Code size       18 (0x12)
    .maxstack  1
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldarg.1
    IL_0002:  constrained. !V
    IL_0008:  callvirt   instance string class I`1<valuetype GenStruct`2<!T,!U>>::Method()
    IL_000d:  stloc.0
    IL_000e:  br.s       IL_0010

    IL_0010:  ldloc.0
    IL_0011:  ret
  } // end of method Conversion2`3::Caller

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Conversion2`3::.ctor

} // end of class Conversion2`3

.class public auto ansi beforefieldinit Test_RecursiveGen
       extends [mscorlib]System.Object
{
  .method public hidebysig static int32 
          Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       206 (0xce)
    .maxstack  2
    .locals init (bool V_0,
             valuetype MyStruct V_1,
             class Conversion1`2<valuetype MyStruct,valuetype MyStruct> V_2,
             string V_3,
             valuetype GenStruct`2<valuetype GenStruct`2<string,object>,valuetype GenStruct`2<object,string>> V_4,
             class Conversion2`3<valuetype GenStruct`2<string,object>,valuetype GenStruct`2<object,string>,valuetype GenStruct`2<valuetype GenStruct`2<string,object>,valuetype GenStruct`2<object,string>>> V_5,
             int32 V_6,
             bool V_7)
    IL_0000:  nop
    IL_0001:  ldc.i4.1
    IL_0002:  stloc.0
    IL_0003:  ldloca.s   V_1
    IL_0005:  initobj    MyStruct
    IL_000b:  newobj     instance void class Conversion1`2<valuetype MyStruct,valuetype MyStruct>::.ctor()
    IL_0010:  stloc.2
    IL_0011:  ldloc.2
    IL_0012:  ldloca.s   V_1
    IL_0014:  callvirt   instance string class Conversion1`2<valuetype MyStruct,valuetype MyStruct>::Caller(!1&)
    IL_0019:  stloc.3
    IL_001a:  ldloc.3
    IL_001b:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0020:  nop
    IL_0021:  ldloca.s   V_1
    IL_0023:  ldfld      int32 MyStruct::n
    IL_0028:  call       void [System.Console]System.Console::WriteLine(int32)
    IL_002d:  nop
    IL_002e:  ldloc.3
    IL_002f:  ldstr      "I<MyStruct>.Method()"
    IL_0034:  call       bool [mscorlib]System.String::op_Inequality(string,
                                                                     string)
    IL_0039:  brtrue.s   IL_0047

    IL_003b:  ldloca.s   V_1
    IL_003d:  ldfld      int32 MyStruct::n
    IL_0042:  ldc.i4.1
    IL_0043:  ceq
    IL_0045:  br.s       IL_0048

    IL_0047:  ldc.i4.0
    IL_0048:  stloc.s    V_7
    IL_004a:  ldloc.s    V_7
    IL_004c:  brtrue.s   IL_0050

    IL_004e:  ldc.i4.0
    IL_004f:  stloc.0
    IL_0050:  ldloca.s   V_4
    IL_0052:  initobj    valuetype GenStruct`2<valuetype GenStruct`2<string,object>,valuetype GenStruct`2<object,string>>
    IL_0058:  newobj     instance void class Conversion2`3<valuetype GenStruct`2<string,object>,valuetype GenStruct`2<object,string>,valuetype GenStruct`2<valuetype GenStruct`2<string,object>,valuetype GenStruct`2<object,string>>>::.ctor()
    IL_005d:  stloc.s    V_5
    IL_005f:  ldloc.s    V_5
    IL_0061:  ldloca.s   V_4
    IL_0063:  callvirt   instance string class Conversion2`3<valuetype GenStruct`2<string,object>,valuetype GenStruct`2<object,string>,valuetype GenStruct`2<valuetype GenStruct`2<string,object>,valuetype GenStruct`2<object,string>>>::Caller(!2&)
    IL_0068:  stloc.3
    IL_0069:  ldloc.3
    IL_006a:  call       void [System.Console]System.Console::WriteLine(string)
    IL_006f:  nop
    IL_0070:  ldloca.s   V_4
    IL_0072:  ldfld      int32 valuetype GenStruct`2<valuetype GenStruct`2<string,object>,valuetype GenStruct`2<object,string>>::n
    IL_0077:  call       void [System.Console]System.Console::WriteLine(int32)
    IL_007c:  nop
    IL_007d:  ldloc.3
    IL_007e:  ldstr      "I<GenStruct<T, U>>.Method()"
    IL_0083:  call       bool [mscorlib]System.String::op_Inequality(string,
                                                                     string)
    IL_0088:  brtrue.s   IL_0096

    IL_008a:  ldloca.s   V_4
    IL_008c:  ldfld      int32 valuetype GenStruct`2<valuetype GenStruct`2<string,object>,valuetype GenStruct`2<object,string>>::n
    IL_0091:  ldc.i4.1
    IL_0092:  ceq
    IL_0094:  br.s       IL_0097

    IL_0096:  ldc.i4.0
    IL_0097:  stloc.s    V_7
    IL_0099:  ldloc.s    V_7
    IL_009b:  brtrue.s   IL_009f

    IL_009d:  ldc.i4.0
    IL_009e:  stloc.0
    IL_009f:  ldloc.0
    IL_00a0:  ldc.i4.0
    IL_00a1:  ceq
    IL_00a3:  stloc.s    V_7
    IL_00a5:  ldloc.s    V_7
    IL_00a7:  brtrue.s   IL_00bb

    IL_00a9:  nop
    IL_00aa:  ldstr      "Pass"
    IL_00af:  call       void [System.Console]System.Console::WriteLine(string)
    IL_00b4:  nop
    IL_00b5:  ldc.i4.s   100
    IL_00b7:  stloc.s    V_6
    IL_00b9:  br.s       IL_00cb

    IL_00bb:  ldstr      "Fail"
    IL_00c0:  call       void [System.Console]System.Console::WriteLine(string)
    IL_00c5:  nop
    IL_00c6:  ldc.i4.m1
    IL_00c7:  stloc.s    V_6
    IL_00c9:  br.s       IL_00cb

    IL_00cb:  ldloc.s    V_6
    IL_00cd:  ret
  } // end of method Test::Main

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Test::.ctor

} // end of class Test


// =============================================================

// *********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file RecursiveGen.res
