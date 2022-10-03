// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console { }
.assembly extern xunit.core {}

.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .hash = (73 26 79 1F 31 96 69 CE 57 B9 48 24 EE A8 34 F1   // s&y.1.i.W.H$..4.
           42 87 88 29 )                                     // B..)
  .ver 1:1:3300:0
}
.assembly arity01
{

  // --- The following custom attribute is added automatically, do not uncomment -------
  //  .custom instance void [mscorlib]System.Diagnostics.DebuggableAttribute::.ctor(bool,
  //                                                                                bool) = ( 01 00 00 01 00 00 ) 

  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
// MVID: {076E2FBD-6901-448E-AD54-697AB5AE59C9}
.imagebase 0x00400000
.subsystem 0x00000003
.file alignment 512
.corflags 0x00000001
// Image base: 0x034B0000

// =============== CLASS MEMBERS DECLARATION ===================

.class private auto ansi beforefieldinit Foo
       extends [mscorlib]System.Object
{
  .method public hidebysig instance string 
          Function() cil managed
  {
    // Code size       20 (0x14)
    .maxstack  1
    .locals init (string V_0)
    IL_0000:  ldstr      "0 TPs"
    IL_0005:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000a:  ldstr      ""
    IL_000f:  stloc.0
    IL_0010:  br.s       IL_0012

    IL_0012:  ldloc.0
    IL_0013:  ret
  } // end of method Foo::Function

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Foo::.ctor

  .method public hidebysig instance string 
          Function<([mscorlib]System.Object) T>(!!0 t) cil managed
  {
    // Code size       26 (0x1a)
    .maxstack  1
    .locals init (string V_0)
    IL_0000:  ldstr      "1 TPs"
    IL_0005:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000a:  ldarg.1
    IL_000b:  box        !!0
    IL_0010:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_0015:  stloc.0
    IL_0016:  br.s       IL_0018

    IL_0018:  ldloc.0
    IL_0019:  ret
  } // end of method Foo01::Function

  .method public hidebysig instance string 
          Function<([mscorlib]System.Object) T,([mscorlib]System.Object) U>(!!0 t,
                                                                            !!1 u) cil managed
  {
    // Code size       42 (0x2a)
    .maxstack  2
    .locals init (string V_0)
    IL_0000:  ldstr      "2 TPs"
    IL_0005:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000a:  ldarg.1
    IL_000b:  box        !!0
    IL_0010:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_0015:  ldarg.2
    IL_0016:  box        !!1
    IL_001b:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_0020:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_0025:  stloc.0
    IL_0026:  br.s       IL_0028

    IL_0028:  ldloc.0
    IL_0029:  ret
  } // end of method Foo02::Function

  .method public hidebysig instance string 
          Function<([mscorlib]System.Object) T,([mscorlib]System.Object) U,([mscorlib]System.Object) V>(!!0 t,
                                                                                                        !!1 u,
                                                                                                        !!2 v) cil managed
  {
    // Code size       53 (0x35)
    .maxstack  3
    .locals init (string V_0)
    IL_0000:  ldstr      "3 TPs"
    IL_0005:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000a:  ldarg.1
    IL_000b:  box        !!0
    IL_0010:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_0015:  ldarg.2
    IL_0016:  box        !!1
    IL_001b:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_0020:  ldarg.3
    IL_0021:  box        !!2
    IL_0026:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_002b:  call       string [mscorlib]System.String::Concat(string,
                                                                string,
                                                                string)
    IL_0030:  stloc.0
    IL_0031:  br.s       IL_0033

    IL_0033:  ldloc.0
    IL_0034:  ret
  } // end of method Foo03::Function

 
} // end of class Foo03

.class public auto ansi beforefieldinit Test_arity01
       extends [mscorlib]System.Object
{
  .field public static int32 counter
  .field public static bool result
  .method public hidebysig static void  Eval(bool exp) cil managed
  {
    // Code size       47 (0x2f)
    .maxstack  2
    IL_0000:  ldsfld     int32 Test_arity01::counter
    IL_0005:  ldc.i4.1
    IL_0006:  add
    IL_0007:  stsfld     int32 Test_arity01::counter
    IL_000c:  ldarg.0
    IL_000d:  brtrue.s   IL_002e

    IL_000f:  ldarg.0
    IL_0010:  stsfld     bool Test_arity01::result
    IL_0015:  ldstr      "Test Failed at location: "
    IL_001a:  ldsfld     int32 Test_arity01::counter
    IL_001f:  box        [mscorlib]System.Int32
    IL_0024:  call       string [mscorlib]System.String::Concat(object,
                                                                object)
    IL_0029:  call       void [System.Console]System.Console::WriteLine(string)
    IL_002e:  ret
  } // end of method Test::Eval

  .method public hidebysig static int32  Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       478 (0x1de)
    .maxstack  4
    .locals init (class Foo V_0,
             class Foo V_1,
             class Foo V_2,
             class Foo V_3,
             int32 V_4)
    IL_0000:  newobj     instance void Foo::.ctor()
    IL_0005:  stloc.0
    IL_0006:  newobj     instance void Foo::.ctor()
    IL_000b:  stloc.1
    IL_000c:  newobj     instance void Foo::.ctor()
    IL_0011:  stloc.2
    IL_0012:  newobj     instance void Foo::.ctor()
    IL_0017:  stloc.3
    IL_0018:  ldloc.0
    IL_0019:  callvirt   instance string Foo::Function()
    IL_001e:  ldstr      ""
    IL_0023:  callvirt   instance bool [mscorlib]System.String::Equals(string)
    IL_0028:  call       void Test_arity01::Eval(bool)
    IL_002d:  ldloc.0
    IL_002e:  ldstr      "1"
    IL_0033:  callvirt   instance string Foo::Function<string>(!!0)
    IL_0038:  ldstr      "1"
    IL_003d:  callvirt   instance bool [mscorlib]System.String::Equals(string)
    IL_0042:  call       void Test_arity01::Eval(bool)
    IL_0047:  ldloc.0
    IL_0048:  ldc.i4.1
    IL_0049:  callvirt   instance string Foo::Function<int32>(!!0)
    IL_004e:  ldstr      "1"
    IL_0053:  callvirt   instance bool [mscorlib]System.String::Equals(string)
    IL_0058:  call       void Test_arity01::Eval(bool)
    IL_005d:  ldloc.0
    IL_005e:  ldstr      "1"
    IL_0063:  ldstr      "2"
    IL_0068:  callvirt   instance string Foo::Function<string,string>(!!0,
                                                                        !!1)
    IL_006d:  ldstr      "12"
    IL_0072:  callvirt   instance bool [mscorlib]System.String::Equals(string)
    IL_0077:  call       void Test_arity01::Eval(bool)
    IL_007c:  ldloc.0
    IL_007d:  ldc.i4.1
    IL_007e:  ldc.i4.2
    IL_007f:  callvirt   instance string Foo::Function<int32,int32>(!!0,
                                                                        !!1)
    IL_0084:  ldstr      "12"
    IL_0089:  callvirt   instance bool [mscorlib]System.String::Equals(string)
    IL_008e:  call       void Test_arity01::Eval(bool)
    IL_0093:  ldloc.0
    IL_0094:  ldc.i4.1
    IL_0095:  ldstr      "2"
    IL_009a:  callvirt   instance string Foo::Function<int32,string>(!!0,
                                                                        !!1)
    IL_009f:  ldstr      "12"
    IL_00a4:  callvirt   instance bool [mscorlib]System.String::Equals(string)
    IL_00a9:  call       void Test_arity01::Eval(bool)
    IL_00ae:  ldloc.0
    IL_00af:  ldstr      "1"
    IL_00b4:  ldc.i4.2
    IL_00b5:  callvirt   instance string Foo::Function<string,int32>(!!0,
                                                                        !!1)
    IL_00ba:  ldstr      "12"
    IL_00bf:  callvirt   instance bool [mscorlib]System.String::Equals(string)
    IL_00c4:  call       void Test_arity01::Eval(bool)
    IL_00c9:  ldloc.0
    IL_00ca:  ldstr      "1"
    IL_00cf:  ldstr      "2"
    IL_00d4:  ldstr      "3"
    IL_00d9:  callvirt   instance string Foo::Function<string,string,string>(!!0,
                                                                               !!1,
                                                                               !!2)
    IL_00de:  ldstr      "123"
    IL_00e3:  callvirt   instance bool [mscorlib]System.String::Equals(string)
    IL_00e8:  call       void Test_arity01::Eval(bool)
    IL_00ed:  ldloc.0
    IL_00ee:  ldstr      "1"
    IL_00f3:  ldstr      "2"
    IL_00f8:  ldc.i4.3
    IL_00f9:  callvirt   instance string Foo::Function<string,string,int32>(!!0,
                                                                               !!1,
                                                                               !!2)
    IL_00fe:  ldstr      "123"
    IL_0103:  callvirt   instance bool [mscorlib]System.String::Equals(string)
    IL_0108:  call       void Test_arity01::Eval(bool)
    IL_010d:  ldloc.0
    IL_010e:  ldstr      "1"
    IL_0113:  ldc.i4.2
    IL_0114:  ldc.i4.3
    IL_0115:  callvirt   instance string Foo::Function<string,int32,int32>(!!0,
                                                                               !!1,
                                                                               !!2)
    IL_011a:  ldstr      "123"
    IL_011f:  callvirt   instance bool [mscorlib]System.String::Equals(string)
    IL_0124:  call       void Test_arity01::Eval(bool)
    IL_0129:  ldloc.0
    IL_012a:  ldstr      "1"
    IL_012f:  ldc.i4.2
    IL_0130:  ldstr      "3"
    IL_0135:  callvirt   instance string Foo::Function<string,int32,string>(!!0,
                                                                               !!1,
                                                                               !!2)
    IL_013a:  ldstr      "123"
    IL_013f:  callvirt   instance bool [mscorlib]System.String::Equals(string)
    IL_0144:  call       void Test_arity01::Eval(bool)
    IL_0149:  ldloc.0
    IL_014a:  ldc.i4.1
    IL_014b:  ldc.i4.2
    IL_014c:  ldc.i4.3
    IL_014d:  callvirt   instance string Foo::Function<int32,int32,int32>(!!0,
                                                                               !!1,
                                                                               !!2)
    IL_0152:  ldstr      "123"
    IL_0157:  callvirt   instance bool [mscorlib]System.String::Equals(string)
    IL_015c:  call       void Test_arity01::Eval(bool)
    IL_0161:  ldloc.0
    IL_0162:  ldc.i4.1
    IL_0163:  ldc.i4.2
    IL_0164:  ldstr      "3"
    IL_0169:  callvirt   instance string Foo::Function<int32,int32,string>(!!0,
                                                                               !!1,
                                                                               !!2)
    IL_016e:  ldstr      "123"
    IL_0173:  callvirt   instance bool [mscorlib]System.String::Equals(string)
    IL_0178:  call       void Test_arity01::Eval(bool)
    IL_017d:  ldloc.0
    IL_017e:  ldc.i4.1
    IL_017f:  ldstr      "2"
    IL_0184:  ldc.i4.3
    IL_0185:  callvirt   instance string Foo::Function<int32,string,int32>(!!0,
                                                                               !!1,
                                                                               !!2)
    IL_018a:  ldstr      "123"
    IL_018f:  callvirt   instance bool [mscorlib]System.String::Equals(string)
    IL_0194:  call       void Test_arity01::Eval(bool)
    IL_0199:  ldloc.0
    IL_019a:  ldstr      "1"
    IL_019f:  ldc.i4.2
    IL_01a0:  ldc.i4.3
    IL_01a1:  callvirt   instance string Foo::Function<string,int32,int32>(!!0,
                                                                               !!1,
                                                                               !!2)
    IL_01a6:  ldstr      "123"
    IL_01ab:  callvirt   instance bool [mscorlib]System.String::Equals(string)
    IL_01b0:  call       void Test_arity01::Eval(bool)
    IL_01b5:  ldsfld     bool Test_arity01::result
    IL_01ba:  brfalse.s  IL_01cc

    IL_01bc:  ldstr      "Test Passed"
    IL_01c1:  call       void [System.Console]System.Console::WriteLine(string)
    IL_01c6:  ldc.i4.s   100
    IL_01c8:  stloc.s    V_4
    IL_01ca:  br.s       IL_01db

    IL_01cc:  ldstr      "Test Failed"
    IL_01d1:  call       void [System.Console]System.Console::WriteLine(string)
    IL_01d6:  ldc.i4.1
    IL_01d7:  stloc.s    V_4
    IL_01d9:  br.s       IL_01db

    IL_01db:  ldloc.s    V_4
    IL_01dd:  ret
  } // end of method Test::Main

  .method private hidebysig specialname rtspecialname static 
          void  .cctor() cil managed
  {
    // Code size       13 (0xd)
    .maxstack  1
    IL_0000:  ldc.i4.0
    IL_0001:  stsfld     int32 Test_arity01::counter
    IL_0006:  ldc.i4.1
    IL_0007:  stsfld     bool Test_arity01::result
    IL_000c:  ret
  } // end of method Test::.cctor

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Test::.ctor

} // end of class Test


// =============================================================


//*********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file arity01.res
