// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


//  .NET IL Disassembler.  Version 8.0.0-dev



// Metadata version: v4.0.30319
.assembly extern System.Runtime
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )                         // .?_....:
  .ver 8:0:0:0
}
.assembly extern xunit.core {}
.assembly extern xunit.assert
{
  .publickeytoken = (8D 05 B1 BB 7A 6F DB 6C )                         // ....zo.l
  .ver 2:4:2:0
}
.assembly Reabstraction
{
  .custom instance void [System.Runtime]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
  .custom instance void [System.Runtime]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::.ctor() = ( 01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
                                                                                                                   63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01 )       // ceptionThrows.

  // --- The following custom attribute is added automatically, do not uncomment -------
  //  .custom instance void [System.Runtime]System.Diagnostics.DebuggableAttribute::.ctor(valuetype [System.Runtime]System.Diagnostics.DebuggableAttribute/DebuggingModes) = ( 01 00 07 01 00 00 00 00 ) 

  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.module Reabstraction.dll
// MVID: {b8407d43-f3f1-4afa-9216-253e1c35c868}
.custom instance void [System.Runtime]System.Runtime.CompilerServices.RefSafetyRulesAttribute::.ctor(int32) = ( 01 00 0B 00 00 00 00 00 ) 
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x0000022EC3B10000


// =============== CLASS MEMBERS DECLARATION ===================

.class public auto ansi beforefieldinit VirtualStaticMethodReabstraction
       extends [System.Runtime]System.Object
{
  .class interface abstract auto ansi nested private IFoo
  {
    .method public hidebysig static virtual 
            void  Frob() cil managed
    {
      // Code size       2 (0x2)
      .maxstack  8
      IL_0000:  ldnull
      IL_0001:  throw
    } // end of method IFoo::Frob

    .method public hidebysig static virtual 
            void  Frob<Z>() cil managed
    {
      // Code size       2 (0x2)
      .maxstack  8
      IL_0000:  ldnull
      IL_0001:  throw
    } // end of method IFoo::Frob

  } // end of class IFoo

  .class interface abstract auto ansi nested private IBar
         implements VirtualStaticMethodReabstraction/IFoo
  {
    .method private hidebysig static void 
            VirtualStaticMethodReabstraction.IFoo.Frob() cil managed
    {
      .override VirtualStaticMethodReabstraction/IFoo::Frob
      // Code size       2 (0x2)
      .maxstack  8
      IL_0000:  ldnull
      IL_0001:  throw
    } // end of method IBar::VirtualStaticMethodReabstraction.IFoo.Frob

    .method private hidebysig static void 
            VirtualStaticMethodReabstraction.IFoo.Frob<Z>() cil managed
    {
      .override VirtualStaticMethodReabstraction/IFoo::Frob
      // Code size       2 (0x2)
      .maxstack  8
      IL_0000:  ldnull
      IL_0001:  throw
    } // end of method IBar::VirtualStaticMethodReabstraction.IFoo.Frob

  } // end of class IBar

  .class interface abstract auto ansi nested private IBaz
         implements VirtualStaticMethodReabstraction/IFoo
  {
    .method private hidebysig static abstract virtual final 
            void  VirtualStaticMethodReabstraction.IFoo.Frob() cil managed
    {
      .override VirtualStaticMethodReabstraction/IFoo::Frob
    } // end of method IBaz::VirtualStaticMethodReabstraction.IFoo.Frob

    .method private hidebysig static abstract virtual final 
            void  VirtualStaticMethodReabstraction.IFoo.Frob<Z>() cil managed
    {
      .override VirtualStaticMethodReabstraction/IFoo::Frob
    } // end of method IBaz::VirtualStaticMethodReabstraction.IFoo.Frob

  } // end of class IBaz

  .class auto ansi nested private beforefieldinit BarClass
         extends [System.Runtime]System.Object
         implements VirtualStaticMethodReabstraction/IBaz,
                    VirtualStaticMethodReabstraction/IFoo
  {
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      // Code size       8 (0x8)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [System.Runtime]System.Object::.ctor()
      IL_0006:  nop
      IL_0007:  ret
    } // end of method BarClass::.ctor

  } // end of class BarClass

  .class sequential ansi sealed nested private beforefieldinit BarStruct
         extends [System.Runtime]System.ValueType
         implements VirtualStaticMethodReabstraction/IBaz,
                    VirtualStaticMethodReabstraction/IFoo
  {
    .pack 0
    .size 1
  } // end of class BarStruct

  .class auto ansi serializable sealed nested private beforefieldinit '<>c'
         extends [System.Runtime]System.Object
  {
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
    .field public static initonly class VirtualStaticMethodReabstraction/'<>c' '<>9'
    .field public static class [System.Runtime]System.Action '<>9__0_0'
    .field public static class [System.Runtime]System.Action '<>9__0_1'
    .field public static class [System.Runtime]System.Action '<>9__0_2'
    .field public static class [System.Runtime]System.Action '<>9__0_3'
    .field public static class [System.Runtime]System.Action '<>9__0_4'
    .field public static class [System.Runtime]System.Action '<>9__0_5'
    .field public static class [System.Runtime]System.Action '<>9__0_6'
    .field public static class [System.Runtime]System.Action '<>9__0_7'
    .field public static class [System.Runtime]System.Action '<>9__0_8'
    .field public static class [System.Runtime]System.Action '<>9__0_9'
    .field public static class [System.Runtime]System.Action '<>9__0_10'
    .field public static class [System.Runtime]System.Action '<>9__0_11'
    .method private hidebysig specialname rtspecialname static 
            void  .cctor() cil managed
    {
      // Code size       11 (0xb)
      .maxstack  8
      IL_0000:  newobj     instance void VirtualStaticMethodReabstraction/'<>c'::.ctor()
      IL_0005:  stsfld     class VirtualStaticMethodReabstraction/'<>c' VirtualStaticMethodReabstraction/'<>c'::'<>9'
      IL_000a:  ret
    } // end of method '<>c'::.cctor

    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      // Code size       8 (0x8)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [System.Runtime]System.Object::.ctor()
      IL_0006:  nop
      IL_0007:  ret
    } // end of method '<>c'::.ctor

    .method assembly hidebysig instance void 
            '<Main>b__0_0'() cil managed
    {
      // Code size       8 (0x8)
      .maxstack  8
      IL_0000:  nop
      IL_0001:  call       void VirtualStaticMethodReabstraction::Call<valuetype VirtualStaticMethodReabstraction/BarStruct>()
      IL_0006:  nop
      IL_0007:  ret
    } // end of method '<>c'::'<Main>b__0_0'

    .method assembly hidebysig instance void 
            '<Main>b__0_1'() cil managed
    {
      // Code size       8 (0x8)
      .maxstack  8
      IL_0000:  nop
      IL_0001:  call       void VirtualStaticMethodReabstraction::Call<class VirtualStaticMethodReabstraction/BarClass>()
      IL_0006:  nop
      IL_0007:  ret
    } // end of method '<>c'::'<Main>b__0_1'

    .method assembly hidebysig instance void 
            '<Main>b__0_2'() cil managed
    {
      // Code size       8 (0x8)
      .maxstack  8
      IL_0000:  nop
      IL_0001:  call       void VirtualStaticMethodReabstraction::Call<valuetype VirtualStaticMethodReabstraction/BarStruct,class VirtualStaticMethodReabstraction/BarClass>()
      IL_0006:  nop
      IL_0007:  ret
    } // end of method '<>c'::'<Main>b__0_2'

    .method assembly hidebysig instance void 
            '<Main>b__0_3'() cil managed
    {
      // Code size       8 (0x8)
      .maxstack  8
      IL_0000:  nop
      IL_0001:  call       void VirtualStaticMethodReabstraction::Call<valuetype VirtualStaticMethodReabstraction/BarStruct,valuetype VirtualStaticMethodReabstraction/BarStruct>()
      IL_0006:  nop
      IL_0007:  ret
    } // end of method '<>c'::'<Main>b__0_3'

    .method assembly hidebysig instance void 
            '<Main>b__0_4'() cil managed
    {
      // Code size       8 (0x8)
      .maxstack  8
      IL_0000:  nop
      IL_0001:  call       void VirtualStaticMethodReabstraction::Call<class VirtualStaticMethodReabstraction/BarClass,class VirtualStaticMethodReabstraction/BarClass>()
      IL_0006:  nop
      IL_0007:  ret
    } // end of method '<>c'::'<Main>b__0_4'

    .method assembly hidebysig instance void 
            '<Main>b__0_5'() cil managed
    {
      // Code size       8 (0x8)
      .maxstack  8
      IL_0000:  nop
      IL_0001:  call       void VirtualStaticMethodReabstraction::Call<class VirtualStaticMethodReabstraction/BarClass,valuetype VirtualStaticMethodReabstraction/BarStruct>()
      IL_0006:  nop
      IL_0007:  ret
    } // end of method '<>c'::'<Main>b__0_5'

    .method assembly hidebysig instance void 
            '<Main>b__0_6'() cil managed
    {
      // Code size       13 (0xd)
      .maxstack  8
      IL_0000:  nop
      IL_0001:  call       class [System.Runtime]System.Action VirtualStaticMethodReabstraction::GetAction<valuetype VirtualStaticMethodReabstraction/BarStruct>()
      IL_0006:  callvirt   instance void [System.Runtime]System.Action::Invoke()
      IL_000b:  nop
      IL_000c:  ret
    } // end of method '<>c'::'<Main>b__0_6'

    .method assembly hidebysig instance void 
            '<Main>b__0_7'() cil managed
    {
      // Code size       13 (0xd)
      .maxstack  8
      IL_0000:  nop
      IL_0001:  call       class [System.Runtime]System.Action VirtualStaticMethodReabstraction::GetAction<class VirtualStaticMethodReabstraction/BarClass>()
      IL_0006:  callvirt   instance void [System.Runtime]System.Action::Invoke()
      IL_000b:  nop
      IL_000c:  ret
    } // end of method '<>c'::'<Main>b__0_7'

    .method assembly hidebysig instance void 
            '<Main>b__0_8'() cil managed
    {
      // Code size       13 (0xd)
      .maxstack  8
      IL_0000:  nop
      IL_0001:  call       class [System.Runtime]System.Action VirtualStaticMethodReabstraction::GetAction<valuetype VirtualStaticMethodReabstraction/BarStruct,class VirtualStaticMethodReabstraction/BarClass>()
      IL_0006:  callvirt   instance void [System.Runtime]System.Action::Invoke()
      IL_000b:  nop
      IL_000c:  ret
    } // end of method '<>c'::'<Main>b__0_8'

    .method assembly hidebysig instance void 
            '<Main>b__0_9'() cil managed
    {
      // Code size       13 (0xd)
      .maxstack  8
      IL_0000:  nop
      IL_0001:  call       class [System.Runtime]System.Action VirtualStaticMethodReabstraction::GetAction<valuetype VirtualStaticMethodReabstraction/BarStruct,valuetype VirtualStaticMethodReabstraction/BarStruct>()
      IL_0006:  callvirt   instance void [System.Runtime]System.Action::Invoke()
      IL_000b:  nop
      IL_000c:  ret
    } // end of method '<>c'::'<Main>b__0_9'

    .method assembly hidebysig instance void 
            '<Main>b__0_10'() cil managed
    {
      // Code size       13 (0xd)
      .maxstack  8
      IL_0000:  nop
      IL_0001:  call       class [System.Runtime]System.Action VirtualStaticMethodReabstraction::GetAction<class VirtualStaticMethodReabstraction/BarClass,class VirtualStaticMethodReabstraction/BarClass>()
      IL_0006:  callvirt   instance void [System.Runtime]System.Action::Invoke()
      IL_000b:  nop
      IL_000c:  ret
    } // end of method '<>c'::'<Main>b__0_10'

    .method assembly hidebysig instance void 
            '<Main>b__0_11'() cil managed
    {
      // Code size       13 (0xd)
      .maxstack  8
      IL_0000:  nop
      IL_0001:  call       class [System.Runtime]System.Action VirtualStaticMethodReabstraction::GetAction<class VirtualStaticMethodReabstraction/BarClass,valuetype VirtualStaticMethodReabstraction/BarStruct>()
      IL_0006:  callvirt   instance void [System.Runtime]System.Action::Invoke()
      IL_000b:  nop
      IL_000c:  ret
    } // end of method '<>c'::'<Main>b__0_11'

  } // end of class '<>c'

  .class abstract auto ansi sealed nested private beforefieldinit '<GetAction>O__3_0`1'<(VirtualStaticMethodReabstraction/IFoo) T>
         extends [System.Runtime]System.Object
  {
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
    .field public static class [System.Runtime]System.Action '<0>__Frob'
  } // end of class '<GetAction>O__3_0`1'

  .class abstract auto ansi sealed nested private beforefieldinit '<GetAction>O__4_0`2'<(VirtualStaticMethodReabstraction/IFoo) T,U>
         extends [System.Runtime]System.Object
  {
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
    .field public static class [System.Runtime]System.Action '<0>__Frob'
  } // end of class '<GetAction>O__4_0`2'

  .method public hidebysig static int32 
          Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       452 (0x1c4)
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldsfld     class [System.Runtime]System.Action VirtualStaticMethodReabstraction/'<>c'::'<>9__0_0'
    IL_0006:  dup
    IL_0007:  brtrue.s   IL_0020

    IL_0009:  pop
    IL_000a:  ldsfld     class VirtualStaticMethodReabstraction/'<>c' VirtualStaticMethodReabstraction/'<>c'::'<>9'
    IL_000f:  ldftn      instance void VirtualStaticMethodReabstraction/'<>c'::'<Main>b__0_0'()
    IL_0015:  newobj     instance void [System.Runtime]System.Action::.ctor(object,
                                                                            native int)
    IL_001a:  dup
    IL_001b:  stsfld     class [System.Runtime]System.Action VirtualStaticMethodReabstraction/'<>c'::'<>9__0_0'
    IL_0020:  call       !!0 [xunit.assert]Xunit.Assert::Throws<class [System.Runtime]System.EntryPointNotFoundException>(class [System.Runtime]System.Action)
    IL_0025:  pop
    IL_0026:  ldsfld     class [System.Runtime]System.Action VirtualStaticMethodReabstraction/'<>c'::'<>9__0_1'
    IL_002b:  dup
    IL_002c:  brtrue.s   IL_0045

    IL_002e:  pop
    IL_002f:  ldsfld     class VirtualStaticMethodReabstraction/'<>c' VirtualStaticMethodReabstraction/'<>c'::'<>9'
    IL_0034:  ldftn      instance void VirtualStaticMethodReabstraction/'<>c'::'<Main>b__0_1'()
    IL_003a:  newobj     instance void [System.Runtime]System.Action::.ctor(object,
                                                                            native int)
    IL_003f:  dup
    IL_0040:  stsfld     class [System.Runtime]System.Action VirtualStaticMethodReabstraction/'<>c'::'<>9__0_1'
    IL_0045:  call       !!0 [xunit.assert]Xunit.Assert::Throws<class [System.Runtime]System.EntryPointNotFoundException>(class [System.Runtime]System.Action)
    IL_004a:  pop
    IL_004b:  ldsfld     class [System.Runtime]System.Action VirtualStaticMethodReabstraction/'<>c'::'<>9__0_2'
    IL_0050:  dup
    IL_0051:  brtrue.s   IL_006a

    IL_0053:  pop
    IL_0054:  ldsfld     class VirtualStaticMethodReabstraction/'<>c' VirtualStaticMethodReabstraction/'<>c'::'<>9'
    IL_0059:  ldftn      instance void VirtualStaticMethodReabstraction/'<>c'::'<Main>b__0_2'()
    IL_005f:  newobj     instance void [System.Runtime]System.Action::.ctor(object,
                                                                            native int)
    IL_0064:  dup
    IL_0065:  stsfld     class [System.Runtime]System.Action VirtualStaticMethodReabstraction/'<>c'::'<>9__0_2'
    IL_006a:  call       !!0 [xunit.assert]Xunit.Assert::Throws<class [System.Runtime]System.EntryPointNotFoundException>(class [System.Runtime]System.Action)
    IL_006f:  pop
    IL_0070:  ldsfld     class [System.Runtime]System.Action VirtualStaticMethodReabstraction/'<>c'::'<>9__0_3'
    IL_0075:  dup
    IL_0076:  brtrue.s   IL_008f

    IL_0078:  pop
    IL_0079:  ldsfld     class VirtualStaticMethodReabstraction/'<>c' VirtualStaticMethodReabstraction/'<>c'::'<>9'
    IL_007e:  ldftn      instance void VirtualStaticMethodReabstraction/'<>c'::'<Main>b__0_3'()
    IL_0084:  newobj     instance void [System.Runtime]System.Action::.ctor(object,
                                                                            native int)
    IL_0089:  dup
    IL_008a:  stsfld     class [System.Runtime]System.Action VirtualStaticMethodReabstraction/'<>c'::'<>9__0_3'
    IL_008f:  call       !!0 [xunit.assert]Xunit.Assert::Throws<class [System.Runtime]System.EntryPointNotFoundException>(class [System.Runtime]System.Action)
    IL_0094:  pop
    IL_0095:  ldsfld     class [System.Runtime]System.Action VirtualStaticMethodReabstraction/'<>c'::'<>9__0_4'
    IL_009a:  dup
    IL_009b:  brtrue.s   IL_00b4

    IL_009d:  pop
    IL_009e:  ldsfld     class VirtualStaticMethodReabstraction/'<>c' VirtualStaticMethodReabstraction/'<>c'::'<>9'
    IL_00a3:  ldftn      instance void VirtualStaticMethodReabstraction/'<>c'::'<Main>b__0_4'()
    IL_00a9:  newobj     instance void [System.Runtime]System.Action::.ctor(object,
                                                                            native int)
    IL_00ae:  dup
    IL_00af:  stsfld     class [System.Runtime]System.Action VirtualStaticMethodReabstraction/'<>c'::'<>9__0_4'
    IL_00b4:  call       !!0 [xunit.assert]Xunit.Assert::Throws<class [System.Runtime]System.EntryPointNotFoundException>(class [System.Runtime]System.Action)
    IL_00b9:  pop
    IL_00ba:  ldsfld     class [System.Runtime]System.Action VirtualStaticMethodReabstraction/'<>c'::'<>9__0_5'
    IL_00bf:  dup
    IL_00c0:  brtrue.s   IL_00d9

    IL_00c2:  pop
    IL_00c3:  ldsfld     class VirtualStaticMethodReabstraction/'<>c' VirtualStaticMethodReabstraction/'<>c'::'<>9'
    IL_00c8:  ldftn      instance void VirtualStaticMethodReabstraction/'<>c'::'<Main>b__0_5'()
    IL_00ce:  newobj     instance void [System.Runtime]System.Action::.ctor(object,
                                                                            native int)
    IL_00d3:  dup
    IL_00d4:  stsfld     class [System.Runtime]System.Action VirtualStaticMethodReabstraction/'<>c'::'<>9__0_5'
    IL_00d9:  call       !!0 [xunit.assert]Xunit.Assert::Throws<class [System.Runtime]System.EntryPointNotFoundException>(class [System.Runtime]System.Action)
    IL_00de:  pop
    IL_00df:  ldsfld     class [System.Runtime]System.Action VirtualStaticMethodReabstraction/'<>c'::'<>9__0_6'
    IL_00e4:  dup
    IL_00e5:  brtrue.s   IL_00fe

    IL_00e7:  pop
    IL_00e8:  ldsfld     class VirtualStaticMethodReabstraction/'<>c' VirtualStaticMethodReabstraction/'<>c'::'<>9'
    IL_00ed:  ldftn      instance void VirtualStaticMethodReabstraction/'<>c'::'<Main>b__0_6'()
    IL_00f3:  newobj     instance void [System.Runtime]System.Action::.ctor(object,
                                                                            native int)
    IL_00f8:  dup
    IL_00f9:  stsfld     class [System.Runtime]System.Action VirtualStaticMethodReabstraction/'<>c'::'<>9__0_6'
    IL_00fe:  call       !!0 [xunit.assert]Xunit.Assert::Throws<class [System.Runtime]System.EntryPointNotFoundException>(class [System.Runtime]System.Action)
    IL_0103:  pop
    IL_0104:  ldsfld     class [System.Runtime]System.Action VirtualStaticMethodReabstraction/'<>c'::'<>9__0_7'
    IL_0109:  dup
    IL_010a:  brtrue.s   IL_0123

    IL_010c:  pop
    IL_010d:  ldsfld     class VirtualStaticMethodReabstraction/'<>c' VirtualStaticMethodReabstraction/'<>c'::'<>9'
    IL_0112:  ldftn      instance void VirtualStaticMethodReabstraction/'<>c'::'<Main>b__0_7'()
    IL_0118:  newobj     instance void [System.Runtime]System.Action::.ctor(object,
                                                                            native int)
    IL_011d:  dup
    IL_011e:  stsfld     class [System.Runtime]System.Action VirtualStaticMethodReabstraction/'<>c'::'<>9__0_7'
    IL_0123:  call       !!0 [xunit.assert]Xunit.Assert::Throws<class [System.Runtime]System.EntryPointNotFoundException>(class [System.Runtime]System.Action)
    IL_0128:  pop
    IL_0129:  ldsfld     class [System.Runtime]System.Action VirtualStaticMethodReabstraction/'<>c'::'<>9__0_8'
    IL_012e:  dup
    IL_012f:  brtrue.s   IL_0148

    IL_0131:  pop
    IL_0132:  ldsfld     class VirtualStaticMethodReabstraction/'<>c' VirtualStaticMethodReabstraction/'<>c'::'<>9'
    IL_0137:  ldftn      instance void VirtualStaticMethodReabstraction/'<>c'::'<Main>b__0_8'()
    IL_013d:  newobj     instance void [System.Runtime]System.Action::.ctor(object,
                                                                            native int)
    IL_0142:  dup
    IL_0143:  stsfld     class [System.Runtime]System.Action VirtualStaticMethodReabstraction/'<>c'::'<>9__0_8'
    IL_0148:  call       !!0 [xunit.assert]Xunit.Assert::Throws<class [System.Runtime]System.EntryPointNotFoundException>(class [System.Runtime]System.Action)
    IL_014d:  pop
    IL_014e:  ldsfld     class [System.Runtime]System.Action VirtualStaticMethodReabstraction/'<>c'::'<>9__0_9'
    IL_0153:  dup
    IL_0154:  brtrue.s   IL_016d

    IL_0156:  pop
    IL_0157:  ldsfld     class VirtualStaticMethodReabstraction/'<>c' VirtualStaticMethodReabstraction/'<>c'::'<>9'
    IL_015c:  ldftn      instance void VirtualStaticMethodReabstraction/'<>c'::'<Main>b__0_9'()
    IL_0162:  newobj     instance void [System.Runtime]System.Action::.ctor(object,
                                                                            native int)
    IL_0167:  dup
    IL_0168:  stsfld     class [System.Runtime]System.Action VirtualStaticMethodReabstraction/'<>c'::'<>9__0_9'
    IL_016d:  call       !!0 [xunit.assert]Xunit.Assert::Throws<class [System.Runtime]System.EntryPointNotFoundException>(class [System.Runtime]System.Action)
    IL_0172:  pop
    IL_0173:  ldsfld     class [System.Runtime]System.Action VirtualStaticMethodReabstraction/'<>c'::'<>9__0_10'
    IL_0178:  dup
    IL_0179:  brtrue.s   IL_0192

    IL_017b:  pop
    IL_017c:  ldsfld     class VirtualStaticMethodReabstraction/'<>c' VirtualStaticMethodReabstraction/'<>c'::'<>9'
    IL_0181:  ldftn      instance void VirtualStaticMethodReabstraction/'<>c'::'<Main>b__0_10'()
    IL_0187:  newobj     instance void [System.Runtime]System.Action::.ctor(object,
                                                                            native int)
    IL_018c:  dup
    IL_018d:  stsfld     class [System.Runtime]System.Action VirtualStaticMethodReabstraction/'<>c'::'<>9__0_10'
    IL_0192:  call       !!0 [xunit.assert]Xunit.Assert::Throws<class [System.Runtime]System.EntryPointNotFoundException>(class [System.Runtime]System.Action)
    IL_0197:  pop
    IL_0198:  ldsfld     class [System.Runtime]System.Action VirtualStaticMethodReabstraction/'<>c'::'<>9__0_11'
    IL_019d:  dup
    IL_019e:  brtrue.s   IL_01b7

    IL_01a0:  pop
    IL_01a1:  ldsfld     class VirtualStaticMethodReabstraction/'<>c' VirtualStaticMethodReabstraction/'<>c'::'<>9'
    IL_01a6:  ldftn      instance void VirtualStaticMethodReabstraction/'<>c'::'<Main>b__0_11'()
    IL_01ac:  newobj     instance void [System.Runtime]System.Action::.ctor(object,
                                                                            native int)
    IL_01b1:  dup
    IL_01b2:  stsfld     class [System.Runtime]System.Action VirtualStaticMethodReabstraction/'<>c'::'<>9__0_11'
    IL_01b7:  call       !!0 [xunit.assert]Xunit.Assert::Throws<class [System.Runtime]System.EntryPointNotFoundException>(class [System.Runtime]System.Action)
    IL_01bc:  pop
    IL_01bd:  ldc.i4.s   100
    IL_01bf:  stloc.0
    IL_01c0:  br.s       IL_01c2

    IL_01c2:  ldloc.0
    IL_01c3:  ret
  } // end of method VirtualStaticMethodReabstraction::Main

  .method private hidebysig static void  Call<(VirtualStaticMethodReabstraction/IFoo) T>() cil managed
  {
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  constrained. !!T
    IL_0006:  call       void VirtualStaticMethodReabstraction/IFoo::Frob()
    IL_000b:  nop
    IL_000c:  ret
  } // end of method VirtualStaticMethodReabstraction::Call

  .method private hidebysig static void  Call<(VirtualStaticMethodReabstraction/IFoo) T,U>() cil managed
  {
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  constrained. !!T
    IL_0006:  call       void VirtualStaticMethodReabstraction/IFoo::Frob<!!1>()
    IL_000b:  nop
    IL_000c:  ret
  } // end of method VirtualStaticMethodReabstraction::Call

  .method private hidebysig static class [System.Runtime]System.Action 
          GetAction<(VirtualStaticMethodReabstraction/IFoo) T>() cil managed
  {
    // Code size       34 (0x22)
    .maxstack  8
    IL_0000:  ldsfld     class [System.Runtime]System.Action class VirtualStaticMethodReabstraction/'<GetAction>O__3_0`1'<!!T>::'<0>__Frob'
    IL_0005:  dup
    IL_0006:  brtrue.s   IL_0021

    IL_0008:  pop
    IL_0009:  ldnull
    IL_000a:  constrained. !!T
    IL_0010:  ldftn      void VirtualStaticMethodReabstraction/IFoo::Frob()
    IL_0016:  newobj     instance void [System.Runtime]System.Action::.ctor(object,
                                                                            native int)
    IL_001b:  dup
    IL_001c:  stsfld     class [System.Runtime]System.Action class VirtualStaticMethodReabstraction/'<GetAction>O__3_0`1'<!!T>::'<0>__Frob'
    IL_0021:  ret
  } // end of method VirtualStaticMethodReabstraction::GetAction

  .method private hidebysig static class [System.Runtime]System.Action 
          GetAction<(VirtualStaticMethodReabstraction/IFoo) T,U>() cil managed
  {
    // Code size       34 (0x22)
    .maxstack  8
    IL_0000:  ldsfld     class [System.Runtime]System.Action class VirtualStaticMethodReabstraction/'<GetAction>O__4_0`2'<!!T,!!U>::'<0>__Frob'
    IL_0005:  dup
    IL_0006:  brtrue.s   IL_0021

    IL_0008:  pop
    IL_0009:  ldnull
    IL_000a:  constrained. !!T
    IL_0010:  ldftn      void VirtualStaticMethodReabstraction/IFoo::Frob<!!1>()
    IL_0016:  newobj     instance void [System.Runtime]System.Action::.ctor(object,
                                                                            native int)
    IL_001b:  dup
    IL_001c:  stsfld     class [System.Runtime]System.Action class VirtualStaticMethodReabstraction/'<GetAction>O__4_0`2'<!!T,!!U>::'<0>__Frob'
    IL_0021:  ret
  } // end of method VirtualStaticMethodReabstraction::GetAction

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [System.Runtime]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  } // end of method VirtualStaticMethodReabstraction::.ctor

} // end of class VirtualStaticMethodReabstraction


// =============================================================

// *********** DISASSEMBLY COMPLETE ***********************
