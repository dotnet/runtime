// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//  Microsoft (R) .NET Framework IL Disassembler.  Version 4.6.1055.0
//  Copyright (c) Microsoft Corporation.  All rights reserved.



// Metadata version: v4.0.30319
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 4:0:0:0
}
.assembly extern xunit.core {}
// TODO: use the contract once this is exposed from contracts
.assembly extern System.Runtime { }

.assembly svm_diamondshape
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::.ctor() = ( 01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
                                                                                                             63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01 )       // ceptionThrows.

  // --- The following custom attribute is added automatically, do not uncomment -------
  //  .custom instance void [mscorlib]System.Diagnostics.DebuggableAttribute::.ctor(valuetype [mscorlib]System.Diagnostics.DebuggableAttribute/DebuggingModes) = ( 01 00 07 01 00 00 00 00 ) 

  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
// MVID: {25B467F3-D284-4DB6-BAD4-C7EAC75CF064}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x010B0000


// =============== CLASS MEMBERS DECLARATION ===================

.class interface private abstract auto ansi IFoo
{
  .method public hidebysig newslot virtual
          static int32  Foo(int32 a) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret                 
  } // end of method IFoo::Foo

} // end of class IFoo

.class interface private abstract auto ansi IFoo2
       implements IFoo
{
  .method public hidebysig
          static int32  Foo(int32 a) cil managed
  {
    .override method int32 IFoo::Foo(int32)
    // Code size       20 (0x14)
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "At IFoo2.Foo"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldarg.0
    IL_000d:  ldc.i4.1
    IL_000e:  add
    IL_000f:  stloc.0
    IL_0010:  br.s       IL_0012

    IL_0012:  ldloc.0
    IL_0013:  ret
  } // end of method IFoo2::IFoo.Foo

} // end of class IFoo2

.class interface private abstract auto ansi IFooEx
       implements IFoo
{
  .method public hidebysig
          static int32  IFoo.Foo(int32 a) cil managed
  {
    .override method int32 IFoo::Foo(int32)
    // Code size       20 (0x14)
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "At IFooEx.Foo"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldarg.0
    IL_000d:  ldc.i4.2
    IL_000e:  add
    IL_000f:  stloc.0
    IL_0010:  br.s       IL_0012

    IL_0012:  ldloc.0
    IL_0013:  ret
  } // end of method IFooEx::IFoo.Foo
} // end of class IFooEx

.class interface private abstract auto ansi IFooExReabstract
       implements IFooEx
{
  .method public hidebysig abstract final
          static int32  IFoo.Foo(int32 a) cil managed
  {
  } // end of method IFooExReabstract::Func

}

.class private auto ansi beforefieldinit FooClass
       extends [mscorlib]System.Object
       implements IFoo2,
                  IFoo,
                  IFooEx
{
} // end of class FooClass

.class private auto ansi beforefieldinit FooClassReabstract
       extends [mscorlib]System.Object
       implements IFoo2,
                  IFoo,
                  IFooExReabstract
{
} // end of class FooClassReabstract

.class private auto ansi sealed beforefieldinit FooStruct
       extends [mscorlib]System.ValueType
       implements IFoo2,
                  IFoo,
                  IFooEx
{
} // end of class FooStruct

.class interface private abstract auto ansi I1
{
  .method public hidebysig newslot abstract virtual 
          static int32  Func(int32 a) cil managed
  {
  } // end of method I1::Func

} // end of class I1

.class interface private abstract auto ansi I2
       implements I1
{
  .method private hidebysig
          static int32  I1.Func(int32 a) cil managed
  {
    .override method int32 I1::Func(int32)
    // Code size       20 (0x14)
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "At I2.Func"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldarg.0
    IL_000d:  ldc.i4.2
    IL_000e:  add
    IL_000f:  stloc.0
    IL_0010:  br.s       IL_0012

    IL_0012:  ldloc.0
    IL_0013:  ret
  } // end of method I2::I1.Func
} // end of class I2

.class interface private abstract auto ansi I3
       implements I1
{
  .method private hidebysig
          static int32  I1.Func(int32 a) cil managed
  {
    .override method int32 I1::Func(int32)
    // Code size       20 (0x14)
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "At I3.Func"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldarg.0
    IL_000d:  ldc.i4.3
    IL_000e:  add
    IL_000f:  stloc.0
    IL_0010:  br.s       IL_0012

    IL_0012:  ldloc.0
    IL_0013:  ret
  } // end of method I3::I1.Func
} // end of class I3

.class interface private abstract auto ansi I4
       implements I2,
                  I1,
                  I3
{
  .method private hidebysig
          static int32  I1.Func(int32 a) cil managed
  {
    .override method int32 I1::Func(int32)
    // Code size       20 (0x14)
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "At I4.Func"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldarg.0
    IL_000d:  ldc.i4.4
    IL_000e:  add
    IL_000f:  stloc.0
    IL_0010:  br.s       IL_0012

    IL_0012:  ldloc.0
    IL_0013:  ret
  } // end of method I4::I1.Func   
} // end of class I4

.class interface private abstract auto ansi I4Reabstract
       implements I4
{
  .method private hidebysig virtual abstract
          static int32  Func(int32 a) cil managed
  {
    .override method int32 I1::Func(int32)
  } // end of I4Reabstract::Func
} // end of class I4Reabstract

.class interface private abstract auto ansi I4Reimplement
       implements I4Reabstract
{
  .method private hidebysig
          static int32  I1.Func(int32 a) cil managed
  {
    .override method int32 I1::Func(int32)
    // Code size       20 (0x14)
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "At I4Reimplement.Func"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldarg.0
    IL_000d:  ldc.i4.s   17
    IL_000e:  add
    IL_000f:  stloc.0
    IL_0010:  br.s       IL_0012

    IL_0012:  ldloc.0
    IL_0013:  ret
  } // end of method I4Reimplement::I1.Func
} // end of class I4Reimplement

.class private auto ansi beforefieldinit I4Class
       extends [mscorlib]System.Object
       implements I4,
                  I2,
                  I1,
                  I3
{
} // end of class I4Class

.class private auto ansi beforefieldinit I4ReimplementClass
       extends [mscorlib]System.Object
       implements I4Reimplement,
                  I2,
                  I1,
                  I3
{
} // end of class I4ReimplementClass

.class private auto ansi sealed beforefieldinit I4Struct
       extends [mscorlib]System.ValueType
       implements I4,
                  I2,
                  I1,
                  I3
{
} // end of class I4Struct

.class interface private abstract auto ansi I5
       implements I1
{
  .method private hidebysig
          static int32  I1.Func(int32 a) cil managed
  {
    .override method int32 I1::Func(int32)
    // Code size       20 (0x14)
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "At I5.Func"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldarg.0
    IL_000d:  ldc.i4.5
    IL_000e:  add
    IL_000f:  stloc.0
    IL_0010:  br.s       IL_0012

    IL_0012:  ldloc.0
    IL_0013:  ret
  } // end of method I5::I1.Func  
} // end of class I5

.class interface private abstract auto ansi I6
       implements I1
{
  .method private hidebysig
          static int32  I1.Func(int32 a) cil managed
  {
    .override method int32 I1::Func(int32)
    // Code size       20 (0x14)
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "At I6.Func"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldarg.0
    IL_000d:  ldc.i4.6
    IL_000e:  add
    IL_000f:  stloc.0
    IL_0010:  br.s       IL_0012

    IL_0012:  ldloc.0
    IL_0013:  ret
  } // end of method I6::I1.Func  
} // end of class I6

.class interface private abstract auto ansi I7
       implements I5,
                  I1,
                  I6
{
  .method private hidebysig
          static int32  I1.Func(int32 a) cil managed
  {
    .override method int32 I1::Func(int32)
    // Code size       20 (0x14)
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "At I7.Func"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldarg.0
    IL_000d:  ldc.i4.7
    IL_000e:  add
    IL_000f:  stloc.0
    IL_0010:  br.s       IL_0012

    IL_0012:  ldloc.0
    IL_0013:  ret
  } // end of method I7::I1.Func  
} // end of class I7

.class interface private abstract auto ansi I8
       implements I4,
                  I2,
                  I1,
                  I3,
                  I7,
                  I5,
                  I6
{
  .method private hidebysig
          static int32  I1.Func(int32 a) cil managed
  {
    .override method int32 I1::Func(int32)
    // Code size       20 (0x14)
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "At I8.Func"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldarg.0
    IL_000d:  ldc.i4.8
    IL_000e:  add
    IL_000f:  stloc.0
    IL_0010:  br.s       IL_0012

    IL_0012:  ldloc.0
    IL_0013:  ret
  } // end of method I8::I1.Func  
} // end of class I8

.class private auto ansi beforefieldinit I47Class
       extends [mscorlib]System.Object
       implements I4,
                  I2,
                  I1,
                  I3,
                  I7,
                  I5,
                  I6
{
} // end of class I47Class

.class private auto ansi sealed beforefieldinit I47Struct
       extends [mscorlib]System.ValueType
       implements I4,
                  I2,
                  I1,
                  I3,
                  I7,
                  I5,
                  I6
{
} // end of class I47Struct

.class private auto ansi beforefieldinit I8Class
       extends [mscorlib]System.Object
       implements I8,
                  I4,
                  I2,
                  I1,
                  I3,
                  I7,
                  I5,
                  I6 
{
} // end of class I8Class  

.class private auto ansi sealed beforefieldinit I8Struct
       extends [mscorlib]System.ValueType
       implements I8,
                  I4,
                  I2,
                  I1,
                  I3,
                  I7,
                  I5,
                  I6 
{
} // end of class I8Struct

.class interface private abstract auto ansi GI1`1<T>
{
  .method public hidebysig newslot abstract virtual 
          static int32  Func<S>([out] class [mscorlib]System.Type[]& types) cil managed
  {
  } // end of method GI1`1::'GI1<T>.Func' 

} // end of class GI1`1

.class interface private abstract auto ansi GI2`1<T>
       implements class GI1`1<!T>
{
.method private hidebysig
          static int32  'GI1<T>.Func'<S>([out] class [mscorlib]System.Type[]& types) cil managed
  {
    .override  method int32 class GI1`1<!T>::Func<[1]>(class [mscorlib]System.Type[]&)
    // Code size       100 (0x64)
    .maxstack  5
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldc.i4.4
    IL_0002:  newarr     [mscorlib]System.Object
    IL_0007:  dup
    IL_0008:  ldc.i4.0
    IL_0009:  ldtoken    !T
    IL_000e:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_0013:  stelem.ref
    IL_0014:  dup
    IL_0015:  ldc.i4.1
    IL_0016:  ldstr      ", "
    IL_001b:  stelem.ref
    IL_001c:  dup
    IL_001d:  ldc.i4.2
    IL_001e:  ldtoken    !!S
    IL_0023:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_0028:  stelem.ref
    IL_0029:  dup
    IL_002a:  ldc.i4.3
    IL_002b:  ldstr      ", GI2"
    IL_0030:  stelem.ref
    IL_0031:  call       string [mscorlib]System.String::Concat(object[])
    IL_0036:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_003b:  nop
    IL_003c:  ldarg.0
    IL_003d:  ldc.i4.2
    IL_003e:  newarr     [mscorlib]System.Type
    IL_0043:  dup
    IL_0044:  ldc.i4.0
    IL_0045:  ldtoken    !T
    IL_004a:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_004f:  stelem.ref
    IL_0050:  dup
    IL_0051:  ldc.i4.1
    IL_0052:  ldtoken    !!S
    IL_0057:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_005c:  stelem.ref
    IL_005d:  stind.ref
    IL_005e:  ldc.i4.2
    IL_005f:  stloc.0
    IL_0060:  br.s       IL_0062

    IL_0062:  ldloc.0
    IL_0063:  ret
  } // end of method G2`1::'GI1<T>.Func' 
} // end of class GI2`1

.class interface private abstract auto ansi GI3`1<T>
       implements class GI1`1<!T>
{
.method private hidebysig
          static int32  'GI1<T>.Func'<S>([out] class [mscorlib]System.Type[]& types) cil managed
  {
    .override  method int32 class GI1`1<!T>::Func<[1]>(class [mscorlib]System.Type[]&)
    // Code size       100 (0x64)
    .maxstack  5
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldc.i4.4
    IL_0002:  newarr     [mscorlib]System.Object
    IL_0007:  dup
    IL_0008:  ldc.i4.0
    IL_0009:  ldtoken    !T
    IL_000e:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_0013:  stelem.ref
    IL_0014:  dup
    IL_0015:  ldc.i4.1
    IL_0016:  ldstr      ", "
    IL_001b:  stelem.ref
    IL_001c:  dup
    IL_001d:  ldc.i4.2
    IL_001e:  ldtoken    !!S
    IL_0023:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_0028:  stelem.ref
    IL_0029:  dup
    IL_002a:  ldc.i4.3
    IL_002b:  ldstr      ", GI3"
    IL_0030:  stelem.ref
    IL_0031:  call       string [mscorlib]System.String::Concat(object[])
    IL_0036:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_003b:  nop
    IL_003c:  ldarg.0
    IL_003d:  ldc.i4.2
    IL_003e:  newarr     [mscorlib]System.Type
    IL_0043:  dup
    IL_0044:  ldc.i4.0
    IL_0045:  ldtoken    !T
    IL_004a:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_004f:  stelem.ref
    IL_0050:  dup
    IL_0051:  ldc.i4.1
    IL_0052:  ldtoken    !!S
    IL_0057:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_005c:  stelem.ref
    IL_005d:  stind.ref
    IL_005e:  ldc.i4.3
    IL_005f:  stloc.0
    IL_0060:  br.s       IL_0062

    IL_0062:  ldloc.0
    IL_0063:  ret
  } // end of method GI3`1::'GI1<T>.Func' 
} // end of class GI3`1

.class interface private abstract auto ansi GI4`1<T>
       implements class GI2`1<!T>,
                  class GI1`1<!T>,
                  class GI3`1<!T>
{
 .method private hidebysig
          static int32  'GI1<T>.Func'<S>([out] class [mscorlib]System.Type[]& types) cil managed
  {
    .override  method int32 class GI1`1<!T>::Func<[1]>(class [mscorlib]System.Type[]&)
    // Code size       100 (0x64)
    .maxstack  5
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldc.i4.4
    IL_0002:  newarr     [mscorlib]System.Object
    IL_0007:  dup
    IL_0008:  ldc.i4.0
    IL_0009:  ldtoken    !T
    IL_000e:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_0013:  stelem.ref
    IL_0014:  dup
    IL_0015:  ldc.i4.1
    IL_0016:  ldstr      ", "
    IL_001b:  stelem.ref
    IL_001c:  dup
    IL_001d:  ldc.i4.2
    IL_001e:  ldtoken    !!S
    IL_0023:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_0028:  stelem.ref
    IL_0029:  dup
    IL_002a:  ldc.i4.3
    IL_002b:  ldstr      ", GI4"
    IL_0030:  stelem.ref
    IL_0031:  call       string [mscorlib]System.String::Concat(object[])
    IL_0036:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_003b:  nop
    IL_003c:  ldarg.0
    IL_003d:  ldc.i4.2
    IL_003e:  newarr     [mscorlib]System.Type
    IL_0043:  dup
    IL_0044:  ldc.i4.0
    IL_0045:  ldtoken    !T
    IL_004a:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_004f:  stelem.ref
    IL_0050:  dup
    IL_0051:  ldc.i4.1
    IL_0052:  ldtoken    !!S
    IL_0057:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_005c:  stelem.ref
    IL_005d:  stind.ref
    IL_005e:  ldc.i4.4
    IL_005f:  stloc.0
    IL_0060:  br.s       IL_0062

    IL_0062:  ldloc.0
    IL_0063:  ret
  } // end of method GI4`1::'GI1<T>.Func' 
} // end of class GI4`1

.class private auto ansi beforefieldinit GI23Class`1<T>
       extends [mscorlib]System.Object
       implements class GI2`1<!T>,
                  class GI1`1<!T>,
                  class GI3`1<!T>
{
} // end of class GI23Class`1

.class private auto ansi sealed beforefieldinit GI23Struct`1<T>
       extends [mscorlib]System.ValueType
       implements class GI2`1<!T>,
                  class GI1`1<!T>,
                  class GI3`1<!T>
{
} // end of class GI23Struct`1

.class private auto ansi beforefieldinit GI4Class`1<T>
       extends [mscorlib]System.Object
       implements class GI4`1<!T>,
                  class GI2`1<!T>,
                  class GI1`1<!T>,
                  class GI3`1<!T>
{
} // end of class GI4Class`1

.class private auto ansi sealed beforefieldinit GI4Struct`1<T>
       extends [mscorlib]System.ValueType
       implements class GI4`1<!T>,
                  class GI2`1<!T>,
                  class GI1`1<!T>,
                  class GI3`1<!T>
{
} // end of class GI4Struct`1

.class interface private abstract auto ansi IResolutionAtRuntime`1<T>
{
  .method public hidebysig newslot abstract virtual static int32 Func(int32 arg) cil managed
  {
  } // end of method IResolutionAtRuntime`1::Func

} // end of class GI1`1

.class private auto ansi beforefieldinit ResolutionAtRuntimeBase
       implements class IResolutionAtRuntime`1<object>,
                  class IResolutionAtRuntime`1<string>
{
  .method private hidebysig static int32 FuncObject(int32 arg) cil managed
  {
    .override  method int32 class IResolutionAtRuntime`1<object>::Func(int32)
              ldstr      "At ResolutionAtRuntimeBase.FuncObject"
              call       void [mscorlib]System.Console::WriteLine(string)
              nop
              ldarg.0
              ldc.i4.s   19
              add
              ret
  }

  .method private hidebysig static int32 FuncString(int32 arg) cil managed
  {
    .override  method int32 class IResolutionAtRuntime`1<string>::Func(int32)
              ldstr      "At ResolutionAtRuntimeBase.FuncString"
              call       void [mscorlib]System.Console::WriteLine(string)
              nop
              ldarg.0
              ldc.i4.s   23
              add
              ret
  }
}

.class private auto ansi beforefieldinit ResolutionAtRuntimeThisObj`2<(class IResolutionAtRuntime`1<!V>) T, V>
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname rtspecialname 
            instance void .ctor() il managed
  {
              ldarg.0
              call       instance void [mscorlib]System.Object::.ctor()
              ret
  } // end of method 'Test::.ctor'
    

  .method public hidebysig instance int32 RuntimeResolvedFunc(int32 arg)
  {
              ldstr      "At ResolutionAtRuntimeThisObj::RuntimeResolvedFunc"
              call       void [mscorlib]System.Console::WriteLine(string)
              nop
              ldarg.1
              constrained. !T
              call       int32 class [svm_diamondshape]IResolutionAtRuntime`1<!V>::Func(int32)
              ret
  }

} // end of class ResolutionAtRuntimeThisObj


.class private auto ansi beforefieldinit ResolutionAtRuntimeClassParam`2<(class IResolutionAtRuntime`1<!V>) T, V>
{
  .method public hidebysig static int32 RuntimeResolvedFunc(int32 arg)
  {
              ldstr      "At ResolutionAtRuntimeClassParam::RuntimeResolvedFunc"
              call       void [mscorlib]System.Console::WriteLine(string)
              nop
              ldarg.0
              constrained. !T
              call       int32 class [svm_diamondshape]IResolutionAtRuntime`1<!V>::Func(int32)
              ret
  }

} // end of class ResolutionAtRuntimeClassParam

.class private auto ansi beforefieldinit ResolutionAtRuntimeMethodParam
{
  .method public hidebysig static int32 RuntimeResolvedFunc`2<(class IResolutionAtRuntime`1<!!V>) T, V>(int32 arg)
  {
              ldstr      "At ResolutionAtRuntimeMethodParam::RuntimeResolvedFunc"
              call       void [mscorlib]System.Console::WriteLine(string)
              nop
              ldarg.0
              constrained. !!T
              call       int32 class [svm_diamondshape]IResolutionAtRuntime`1<!!V>::Func(int32)
              ret
  }

} // end of class ResolutionAtRuntimeMethodParam

.class public auto ansi beforefieldinit Program
       extends [mscorlib]System.Object
{
  .method public hidebysig static void  Negative() cil managed
  {
    // Code size       225 (0xe1)
    .maxstack  10
    .locals init (
             class [mscorlib]System.Exception V_6,
             class [mscorlib]System.Exception V_7,
             class [mscorlib]System.Type[] V_8,
             class [mscorlib]System.Exception V_9)
    IL_0000:  nop
    IL_0009:  ldstr      "Calling IFoo.Foo on FooClass - expecting exception."
    IL_000e:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_0013:  nop
    .try
    {
      IL_0014:  nop
      IL_0016:  ldc.i4.s   10
                constrained. class [svm_diamondshape]FooClass
      IL_0018:  call         int32 IFoo::Foo(int32)
      IL_001d:  pop
      IL_001e:  ldc.i4.0
      IL_001f:  ldstr      "Expecting exception on FooClass"
      IL_0024:  call       void Test::Assert(bool,
                                             string)
      IL_0029:  nop
      IL_002a:  nop
      IL_002b:  leave.s    IL_004aa

    }  // end .try
    catch [System.Runtime]System.Runtime.AmbiguousImplementationException
    {
      IL_002d:  stloc.s    V_6
      IL_002f:  nop
      IL_0030:  ldstr      "Exception caught: "
      IL_0035:  ldloc.s    V_6
      IL_0037:  callvirt   instance string [mscorlib]System.Object::ToString()
      IL_003c:  call       string [mscorlib]System.String::Concat(string,
                                                                  string)
      IL_0041:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_0046:  nop
      IL_0047:  nop
      IL_0048:  leave.s    IL_004aa

    }  // end handler

    IL_004aa: ldstr      "Resolving delegate IFoo.Foo on FooClass - expecting exception."
              call       void [mscorlib]System.Console::WriteLine(string)
              nop
    .try
    {
                nop
                ldc.i4.s   10
                constrained. class [svm_diamondshape]FooClass
                ldftn        int32 IFoo::Foo(int32)
                pop
                ldc.i4.0
                ldstr      "Expecting exception on FooClass"
                call       void Test::Assert(bool, string)
                nop
                leave.s    IL_004ab

    }  // end .try
    catch [System.Runtime]System.Runtime.AmbiguousImplementationException
    {
                stloc.s    V_6
                nop
                ldstr      "Exception caught: "
                ldloc.s    V_6
                callvirt   instance string [mscorlib]System.Object::ToString()
                call       string [mscorlib]System.String::Concat(string, string)
                call       void [mscorlib]System.Console::WriteLine(string)
                nop
                leave.s    IL_004ab

    }  // end handler

    IL_004ab:   nop
                ldstr      "Calling IFoo.Foo on FooClassReabstract - expecting exception."
                call       void [mscorlib]System.Console::WriteLine(string)
                nop
    .try
    {
                nop
                ldc.i4.s   10
                constrained. class [svm_diamondshape]FooClassReabstract
                call         int32 IFoo::Foo(int32)
                pop
                ldc.i4.0
                ldstr      "Expecting exception on FooClassReabstract"
                call       void Test::Assert(bool,
                                             string)
                nop
                leave.s    IL_004ac

    }  // end .try
    catch [System.Runtime]System.Runtime.AmbiguousImplementationException
    {
                stloc.s    V_6
                nop
                ldstr      "Exception caught: "
                ldloc.s    V_6
                callvirt   instance string [mscorlib]System.Object::ToString()
                call       string [mscorlib]System.String::Concat(string,
                                                                  string)
                call       void [mscorlib]System.Console::WriteLine(string)
                nop
                leave.s    IL_004ac

    }  // end handler

    IL_004ac:   nop
                ldstr      "Calling IFoo.Foo on FooStruct - expecting exception."
                call       void [mscorlib]System.Console::WriteLine(string)
                nop
    .try
    {
                nop
                ldc.i4.s   10
                constrained. valuetype [svm_diamondshape]FooStruct
                call         int32 IFoo::Foo(int32)
                pop
                ldc.i4.0
                ldstr      "Expecting exception on FooStruct"
                call       void Test::Assert(bool,
                                             string)
                nop
                leave.s    IL_004ad

    }  // end .try
    catch [System.Runtime]System.Runtime.AmbiguousImplementationException
    {
                stloc.s    V_6
                nop
                ldstr      "Exception caught: "
                ldloc.s    V_6
                callvirt   instance string [mscorlib]System.Object::ToString()
                call       string [mscorlib]System.String::Concat(string,
                                                                  string)
                call       void [mscorlib]System.Console::WriteLine(string)
                nop
                leave.s    IL_004ad

    }  // end handler

    IL_004ad:   nop
                ldstr      "Resolving delegate IFoo.Foo on FooStruct - expecting exception."
                call       void [mscorlib]System.Console::WriteLine(string)
                nop
    .try
    {
                nop
                ldc.i4.s   10
                constrained. valuetype [svm_diamondshape]FooStruct
                ldftn        int32 IFoo::Foo(int32)
                pop
                ldc.i4.0
                ldstr      "Expecting exception on FooStruct"
                call       void Test::Assert(bool,
                                             string)
                nop
                leave.s    IL_004a

    }  // end .try
    catch [System.Runtime]System.Runtime.AmbiguousImplementationException
    {
                stloc.s    V_6
                nop
                ldstr      "Exception caught: "
                ldloc.s    V_6
                callvirt   instance string [mscorlib]System.Object::ToString()
                call       string [mscorlib]System.String::Concat(string,
                                                                  string)
                call       void [mscorlib]System.Console::WriteLine(string)
                nop
                leave.s    IL_004a

    }  // end handler

    IL_004a:  ldstr      "Calling I1.Func on I47Class - expecting exception"
    IL_0057:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_005c:  nop
    .try
    {
      IL_005d:  nop
      IL_005f:  ldc.i4.s   10
                constrained. class [svm_diamondshape]I47Class
      IL_0061:  call         int32 I1::Func(int32)
      IL_0066:  pop
      IL_0067:  ldc.i4.0
      IL_0068:  ldstr      "Expecting exception on I47Class"
      IL_006d:  call       void Test::Assert(bool,
                                             string)
      IL_0072:  nop
      IL_0073:  nop
      IL_0074:  leave.s    IL_0093a

    }  // end .try
    catch [System.Runtime]System.Runtime.AmbiguousImplementationException
    {
      IL_0076:  stloc.s    V_7
      IL_0078:  nop
      IL_0079:  ldstr      "Exception caught: "
      IL_007e:  ldloc.s    V_7
      IL_0080:  callvirt   instance string [mscorlib]System.Object::ToString()
      IL_0085:  call       string [mscorlib]System.String::Concat(string,
                                                                  string)
      IL_008a:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_008f:  nop
      IL_0090:  nop
      IL_0091:  leave.s    IL_0093a

    }  // end handler

    IL_0093a: ldstr      "Resolving delegate I1.Func on I47Class - expecting exception"
              call       void [mscorlib]System.Console::WriteLine(string)
              nop
    .try
    {
                nop
                ldc.i4.s   10
                constrained. class [svm_diamondshape]I47Class
                ldftn        int32 I1::Func(int32)
                pop
                ldc.i4.0
                ldstr      "Expecting exception on I47Class"
                call       void Test::Assert(bool, string)
                nop
                nop
                leave.s    IL_0093b

    }  // end .try
    catch [System.Runtime]System.Runtime.AmbiguousImplementationException
    {
                stloc.s    V_7
                nop
                ldstr      "Exception caught: "
                ldloc.s    V_7
                callvirt   instance string [mscorlib]System.Object::ToString()
                call       string [mscorlib]System.String::Concat(string, string)
                call       void [mscorlib]System.Console::WriteLine(string)
                nop
                nop
                leave.s    IL_0093b

    }  // end handler

    IL_0093b: ldstr      "Calling I1.Func on I47Struct - expecting exception"
              call       void [mscorlib]System.Console::WriteLine(string)
              nop
    .try
    {
                nop
                ldc.i4.s   10
                constrained. valuetype [svm_diamondshape]I47Struct
                call         int32 I1::Func(int32)
                pop
                ldc.i4.0
                ldstr      "Expecting exception on I47Struct"
                call       void Test::Assert(bool, string)
                nop
                nop
                leave.s    IL_0093c

    }  // end .try
    catch [System.Runtime]System.Runtime.AmbiguousImplementationException
    {
                stloc.s    V_7
                nop
                ldstr      "Exception caught: "
                ldloc.s    V_7
                callvirt   instance string [mscorlib]System.Object::ToString()
                call       string [mscorlib]System.String::Concat(string, string)
                call       void [mscorlib]System.Console::WriteLine(string)
                nop
                nop
                leave.s    IL_0093c

    }  // end handler

    IL_0093c: ldstr      "Resolving delegate I1.Func on I47Struct - expecting exception"
              call       void [mscorlib]System.Console::WriteLine(string)
              nop
    .try
    {
                nop
                ldc.i4.s   10
                constrained. valuetype [svm_diamondshape]I47Struct
                ldftn        int32 I1::Func(int32)
                pop
                ldc.i4.0
                ldstr      "Expecting exception on I47Struct"
                call       void Test::Assert(bool, string)
                nop
                nop
                leave.s    IL_0093

    }  // end .try
    catch [System.Runtime]System.Runtime.AmbiguousImplementationException
    {
                stloc.s    V_7
                nop
                ldstr      "Exception caught: "
                ldloc.s    V_7
                callvirt   instance string [mscorlib]System.Object::ToString()
                call       string [mscorlib]System.String::Concat(string, string)
                call       void [mscorlib]System.Console::WriteLine(string)
                nop
                nop
                leave.s    IL_0093

    }  // end handler

    IL_0093:  ldstr      "Calling GI1<T>.Func on GI23Class<S> - expecting exception"
    IL_00a3:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_00a8:  nop
    .try
    {
      IL_00a9:  nop
      IL_00ac:  ldloca.s   V_8
                constrained. class [svm_diamondshape]GI23Class`1<object>
      IL_00ae:  call         int32 class GI1`1<object>::Func<string>(class [mscorlib]System.Type[]&)
      IL_00b3:  pop
      IL_00b4:  ldc.i4.0
      IL_00b5:  ldstr      "Expecting exception on GI23Class"
      IL_00ba:  call       void Test::Assert(bool, string)
      IL_00bf:  nop
      IL_00c0:  nop
      IL_00c1:  leave.s    IL_00e0a

    }  // end .try
    catch [System.Runtime]System.Runtime.AmbiguousImplementationException
    {
      IL_00c3:  stloc.s    V_9
      IL_00c5:  nop
      IL_00c6:  ldstr      "Exception caught: "
      IL_00cb:  ldloc.s    V_9
      IL_00cd:  callvirt   instance string [mscorlib]System.Object::ToString()
      IL_00d2:  call       string [mscorlib]System.String::Concat(string, string)
      IL_00d7:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_00dc:  nop
      IL_00de:  leave.s    IL_00e0a

    }  // end handler

    IL_00e0a: ldstr      "Resolving delegate GI1<T>.Func on GI23Class<S> - expecting exception"
              call       void [mscorlib]System.Console::WriteLine(string)
              nop
    .try
    {
                nop
                ldloca.s   V_8
                constrained. class [svm_diamondshape]GI23Class`1<object>
                ldftn        int32 class GI1`1<object>::Func<string>(class [mscorlib]System.Type[]&)
                pop
                ldc.i4.0
                ldstr      "Expecting exception on GI23Class"
                call       void Test::Assert(bool,
                                             string)
                nop
                leave.s    IL_00e0b

    }  // end .try
    catch [System.Runtime]System.Runtime.AmbiguousImplementationException
    {
                stloc.s    V_9
                nop
                ldstr      "Exception caught: "
                ldloc.s    V_9
                callvirt   instance string [mscorlib]System.Object::ToString()
                call       string [mscorlib]System.String::Concat(string,
                                                                  string)
                call       void [mscorlib]System.Console::WriteLine(string)
                nop
                nop
                leave.s    IL_00e0b

    }  // end handler

    IL_00e0b: ldstr      "Calling GI1<T>.Func on GI23Struct<S> - expecting exception"
              call       void [mscorlib]System.Console::WriteLine(string)
              nop
    .try
    {
                nop
                ldloca.s   V_8
                constrained. valuetype [svm_diamondshape]GI23Struct`1<object>
                call         int32 class GI1`1<object>::Func<string>(class [mscorlib]System.Type[]&)
                pop
                ldc.i4.0
                ldstr      "Expecting exception on GI23Class"
                call       void Test::Assert(bool,
                                             string)
                nop
                leave.s    IL_00e0c

    }  // end .try
    catch [System.Runtime]System.Runtime.AmbiguousImplementationException
    {
                stloc.s    V_9
                nop
                ldstr      "Exception caught: "
                ldloc.s    V_9
                callvirt   instance string [mscorlib]System.Object::ToString()
                call       string [mscorlib]System.String::Concat(string,
                                                                  string)
                call       void [mscorlib]System.Console::WriteLine(string)
                nop
                nop
                leave.s    IL_00e0c

    }  // end handler

    IL_00e0c: ldstr      "Resolving delegate GI1<T>.Func on GI23Struct<S> - expecting exception"
              call       void [mscorlib]System.Console::WriteLine(string)
              nop
    .try
    {
                nop
                ldloca.s   V_8
                constrained. valuetype [svm_diamondshape]GI23Struct`1<object>
                ldftn        int32 class GI1`1<object>::Func<string>(class [mscorlib]System.Type[]&)
                pop
                ldc.i4.0
                ldstr      "Expecting exception on GI23Class"
                call       void Test::Assert(bool,
                                             string)
                nop
                leave.s    IL_00e0

    }  // end .try
    catch [System.Runtime]System.Runtime.AmbiguousImplementationException
    {
                stloc.s    V_9
                nop
                ldstr      "Exception caught: "
                ldloc.s    V_9
                callvirt   instance string [mscorlib]System.Object::ToString()
                call       string [mscorlib]System.String::Concat(string,
                                                                  string)
                call       void [mscorlib]System.Console::WriteLine(string)
                nop
                nop
                leave.s    IL_00e0

    }  // end handler

    IL_00e0:  ret 
  } // end of method Program::Negative

  .method public hidebysig static void  Positive() cil managed
  {
    // Code size       189 (0xbd)
    .maxstack  10
    .locals init (class [mscorlib]System.Type[] V_4)
    IL_0000:  nop
    IL_0001:  ldstr      "Calling I1.Func on I4Class - expecting I4.Func"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_0015:  ldc.i4.s   10
              constrained. class I4Class
    IL_0017:  call       int32 I1::Func(int32)
    IL_001c:  ldc.i4.s   14
    IL_001e:  ceq
    IL_0020:  ldstr      "Expecting I1.Func to land on I4.Func"
    IL_0025:  call       void Test::Assert(bool, string)
    IL_002a:  nop

              ldstr      "Calling I1.Func on I4ReimplementClass - expecting I4Reimplement.Func"
              call       void [mscorlib]System.Console::WriteLine(string)
              nop
              ldc.i4.s   10
              constrained. class I4ReimplementClass
              call       int32 I1::Func(int32)
              ldc.i4.s   27
              ceq
              ldstr      "Expecting I1.Func to land on I4Reimplement.Func"
              call       void Test::Assert(bool, string)
              nop

              ldstr      "Calling I1.Func on I4Class as a delegate - expecting I4.Func"
              call       void [mscorlib]System.Console::WriteLine(string)
              nop
              ldc.i4.s   10
              constrained. class I4Class
              ldftn      int32 I1::Func(int32)
              calli      int32(int32)
              ldc.i4.s   14
              ceq
              ldstr      "Expecting I1.Func to land on I4.Func"
              call       void Test::Assert(bool, string)
              nop

              ldstr      "Calling I1.Func on I4Struct - expecting I4.Func"
              call       void [mscorlib]System.Console::WriteLine(string)
              nop
              ldc.i4.s   10
              constrained. valuetype I4Struct
              call       int32 I1::Func(int32)
              ldc.i4.s   14
              ceq
              ldstr      "Expecting I1.Func to land on I4.Func"
              call       void Test::Assert(bool, string)
              nop

              ldstr      "Calling I1.Func on I4Struct as a delegate - expecting I4.Func"
              call       void [mscorlib]System.Console::WriteLine(string)
              nop
              ldc.i4.s   10
              constrained. valuetype I4Struct
              ldftn      int32 I1::Func(int32)
              calli      int32(int32)
              ldc.i4.s   14
              ceq
              ldstr      "Expecting I1.Func to land on I4.Func"
              call       void Test::Assert(bool, string)
              nop

    IL_002b:  ldstr      "Calling I1.Func on I8Class - expecting I8.Func"
    IL_0030:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_0035:  nop
    IL_003f:  ldc.i4.s   10
              constrained. class [svm_diamondshape]I8Class
    IL_0041:  call       int32 I1::Func(int32)
    IL_0046:  ldc.i4.s   18
    IL_0048:  ceq
    IL_004a:  ldstr      "Expecting I1.Func to land on I8.Func"
    IL_004f:  call       void Test::Assert(bool, string)
    IL_0054:  nop

              ldstr      "Calling I1.Func on I8Class as a delegate - expecting I8.Func"
              call       void [mscorlib]System.Console::WriteLine(string)
              nop
              ldc.i4.s   10
              constrained. class [svm_diamondshape]I8Class
              ldftn      int32 I1::Func(int32)
              calli      int32(int32)
              ldc.i4.s   18
              ceq
              ldstr      "Expecting I1.Func to land on I8.Func"
              call       void Test::Assert(bool, string)
              nop

              ldstr      "Calling I1.Func on I8Struct - expecting I8.Func"
              call       void [mscorlib]System.Console::WriteLine(string)
              nop
              ldc.i4.s   10
              constrained. valuetype [svm_diamondshape]I8Struct
              call       int32 I1::Func(int32)
              ldc.i4.s   18
              ceq
              ldstr      "Expecting I1.Func to land on I8.Func"
              call       void Test::Assert(bool, string)
              nop

              ldstr      "Calling I1.Func on I8Struct as a delegate - expecting I8.Func"
              call       void [mscorlib]System.Console::WriteLine(string)
              nop
              ldc.i4.s   10
              constrained. valuetype [svm_diamondshape]I8Struct
              ldftn      int32 I1::Func(int32)
              calli      int32(int32)
              ldc.i4.s   18
              ceq
              ldstr      "Expecting I1.Func to land on I8.Func"
              call       void Test::Assert(bool, string)
              nop

    IL_0055:  ldstr      "Calling GI1.Func on GI4Class<object> - expecting GI4.Func<S>"
    IL_005a:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_005f:  nop
    IL_006b:  ldloca.s   V_4
              constrained. class [svm_diamondshape]GI4Class`1<object>
    IL_006d:  call       int32 class GI1`1<object>::Func<string>(class [mscorlib]System.Type[]&)
    IL_0072:  ldc.i4.4
    IL_0073:  ceq
    IL_0075:  ldstr      "Expecting GI1<T>.Func to land on GII4<T>.Func<S>"
    IL_007a:  call       void Test::Assert(bool, string)
    IL_007f:  nop

              ldloc.s    V_4
              ldc.i4.0
              ldelem.ref
              ldtoken    [mscorlib]System.Object
              call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
              call       bool [mscorlib]System.Type::op_Equality(class [mscorlib]System.Type, class [mscorlib]System.Type)
              ldstr      "T must be object"
              call       void Test::Assert(bool, string)
              nop
              ldloc.s    V_4
              ldc.i4.1
              ldelem.ref
              ldtoken    [mscorlib]System.String
              call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
              call       bool [mscorlib]System.Type::op_Equality(class [mscorlib]System.Type, class [mscorlib]System.Type)
              ldstr      "S must be string"
              call       void Test::Assert(bool, string)
              nop

              ldstr      "Calling GI1.Func on GI4Class<object> as a delegate - expecting GI4.Func<S>"
              call       void [mscorlib]System.Console::WriteLine(string)
              nop
              ldloca.s   V_4
              constrained. class [svm_diamondshape]GI4Class`1<object>
              ldftn      int32 class GI1`1<object>::Func<string>(class [mscorlib]System.Type[]&)
              calli      int32(class [mscorlib]System.Type[]&)
              ldc.i4.4
              ceq
              ldstr      "Expecting GI1<T>.Func to land on GII4<T>.Func<S>"
              call       void Test::Assert(bool, string)
              nop

              ldloc.s    V_4
              ldc.i4.0
              ldelem.ref
              ldtoken    [mscorlib]System.Object
              call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
              call       bool [mscorlib]System.Type::op_Equality(class [mscorlib]System.Type, class [mscorlib]System.Type)
              ldstr      "T must be object"
              call       void Test::Assert(bool, string)
              nop
              ldloc.s    V_4
              ldc.i4.1
              ldelem.ref
              ldtoken    [mscorlib]System.String
              call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
              call       bool [mscorlib]System.Type::op_Equality(class [mscorlib]System.Type, class [mscorlib]System.Type)
              ldstr      "S must be string"
              call       void Test::Assert(bool, string)
              nop

              ldstr      "Calling GI1.Func on GI4Struct<object> - expecting GI4.Func<S>"
              call       void [mscorlib]System.Console::WriteLine(string)
              nop
              ldloca.s   V_4
              constrained. valuetype [svm_diamondshape]GI4Struct`1<object>
              call       int32 class GI1`1<object>::Func<string>(class [mscorlib]System.Type[]&)
              ldc.i4.4
              ceq
              ldstr      "Expecting GI1<T>.Func to land on GII4<T>.Func<S>"
              call       void Test::Assert(bool, string)
              nop

    IL_0080:  ldloc.s    V_4
    IL_0082:  ldc.i4.0
    IL_0083:  ldelem.ref
    IL_0084:  ldtoken    [mscorlib]System.Object
    IL_0089:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_008e:  call       bool [mscorlib]System.Type::op_Equality(class [mscorlib]System.Type, class [mscorlib]System.Type)
    IL_0093:  ldstr      "T must be object"
    IL_0098:  call       void Test::Assert(bool, string)
    IL_009d:  nop
    IL_009e:  ldloc.s    V_4
    IL_00a0:  ldc.i4.1
    IL_00a1:  ldelem.ref
    IL_00a2:  ldtoken    [mscorlib]System.String
    IL_00a7:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_00ac:  call       bool [mscorlib]System.Type::op_Equality(class [mscorlib]System.Type, class [mscorlib]System.Type)
    IL_00b1:  ldstr      "S must be string"
    IL_00b6:  call       void Test::Assert(bool, string)
    IL_00bb:  nop

              ldstr      "Calling GI1.Func on GI4Struct<object> as a delegate - expecting GI4.Func<S>"
              call       void [mscorlib]System.Console::WriteLine(string)
              nop
              ldloca.s   V_4
              constrained. valuetype [svm_diamondshape]GI4Struct`1<object>
              ldftn      int32 class GI1`1<object>::Func<string>(class [mscorlib]System.Type[]&)
              calli      int32(class [mscorlib]System.Type[]&)
              ldc.i4.4
              ceq
              ldstr      "Expecting GI1<T>.Func to land on GII4<T>.Func<S>"
              call       void Test::Assert(bool, string)
              nop

              ldloc.s    V_4
              ldc.i4.0
              ldelem.ref
              ldtoken    [mscorlib]System.Object
              call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
              call       bool [mscorlib]System.Type::op_Equality(class [mscorlib]System.Type, class [mscorlib]System.Type)
              ldstr      "T must be object"
              call       void Test::Assert(bool, string)
              nop
              ldloc.s    V_4
              ldc.i4.1
              ldelem.ref
              ldtoken    [mscorlib]System.String
              call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
              call       bool [mscorlib]System.Type::op_Equality(class [mscorlib]System.Type, class [mscorlib]System.Type)
              ldstr      "S must be string"
              call       void Test::Assert(bool, string)
              nop

              ldstr      "Calling ResolutionAtRuntimeThisObj<ResolutionAtRuntimeBase, object>::RuntimeResolvedFunc - expecting ResolutionAtRuntimeBase.FuncObject"
              call       void [mscorlib]System.Console::WriteLine(string)
              nop
              newobj     instance void class ResolutionAtRuntimeThisObj`2<class ResolutionAtRuntimeBase, object>::.ctor()
              ldc.i4.s   200
              call       instance int32 class ResolutionAtRuntimeThisObj`2<class ResolutionAtRuntimeBase, object>::RuntimeResolvedFunc(int32)
              ldc.i4.s   219
              ceq
              ldstr      "Expecting ResolutionAtRuntimeThisObj<ResolutionAtRuntimeBase, object>::RuntimeResolvedFunc to land on ResolutionAtRuntimeBase.FuncObject"
              call       void Test::Assert(bool, string)

              ldstr      "Calling ResolutionAtRuntimeThisObj<ResolutionAtRuntimeBase, string>::RuntimeResolvedFunc - expecting ResolutionAtRuntimeBase.FuncString"
              call       void [mscorlib]System.Console::WriteLine(string)
              nop
              newobj     instance void class ResolutionAtRuntimeThisObj`2<class ResolutionAtRuntimeBase, string>::.ctor()
              ldc.i4.s   200
              call       instance int32 class ResolutionAtRuntimeThisObj`2<class ResolutionAtRuntimeBase, string>::RuntimeResolvedFunc(int32)
              ldc.i4.s   223
              ceq
              ldstr      "Expecting ResolutionAtRuntimeThisObj<ResolutionAtRuntimeBase, string>::RuntimeResolvedFunc to land on ResolutionAtRuntimeBase.FuncString"
              call       void Test::Assert(bool, string)


              ldstr      "Calling ResolutionAtRuntimeClassParam<ResolutionAtRuntimeBase, object>::RuntimeResolvedFunc - expecting ResolutionAtRuntimeBase.FuncObject"
              call       void [mscorlib]System.Console::WriteLine(string)
              nop
              ldc.i4.s   200
              call       int32 class ResolutionAtRuntimeClassParam`2<class ResolutionAtRuntimeBase, object>::RuntimeResolvedFunc(int32)
              ldc.i4.s   219
              ceq
              ldstr      "Expecting ResolutionAtRuntimeClassParam<ResolutionAtRuntimeBase, object>::RuntimeResolvedFunc to land on ResolutionAtRuntimeBase.FuncObject"
              call       void Test::Assert(bool, string)

              ldstr      "Calling ResolutionAtRuntimeClassParam<ResolutionAtRuntimeBase, string>::RuntimeResolvedFunc - expecting ResolutionAtRuntimeBase.FuncString"
              call       void [mscorlib]System.Console::WriteLine(string)
              nop
              ldc.i4.s   200
              call       int32 class ResolutionAtRuntimeClassParam`2<class ResolutionAtRuntimeBase, string>::RuntimeResolvedFunc(int32)
              ldc.i4.s   223
              ceq
              ldstr      "Expecting ResolutionAtRuntimeClassParam<ResolutionAtRuntimeBase, string>::RuntimeResolvedFunc to land on ResolutionAtRuntimeBase.FuncString"
              call       void Test::Assert(bool, string)


              ldstr      "Calling ResolutionAtRuntimeMethodParam::RuntimeResolvedFunc<ResolutionAtRuntimeBase, object> - expecting ResolutionAtRuntimeBase.FuncObject"
              call       void [mscorlib]System.Console::WriteLine(string)
              nop
              ldc.i4.s   100
              call       int32 class ResolutionAtRuntimeMethodParam::RuntimeResolvedFunc`2<class ResolutionAtRuntimeBase, object>(int32)
              ldc.i4.s   119
              ceq
              ldstr      "Expecting ResolutionAtRuntimeMethodParam::RuntimeResolvedFunc<ResolutionAtRuntimeBase, object> to land on ResolutionAtRuntimeBase.FuncObject"
              call       void Test::Assert(bool, string)

              ldstr      "Calling ResolutionAtRuntimeMethodParam::RuntimeResolvedFunc<ResolutionAtRuntimeBase, string> - expecting ResolutionAtRuntimeBase.FuncString"
              call       void [mscorlib]System.Console::WriteLine(string)
              nop
              ldc.i4.s   100
              call       int32 class ResolutionAtRuntimeMethodParam::RuntimeResolvedFunc`2<class ResolutionAtRuntimeBase, string>(int32)
              ldc.i4.s   123
              ceq
              ldstr      "Expecting ResolutionAtRuntimeMethodParam::RuntimeResolvedFunc<ResolutionAtRuntimeBase, string> to land on ResolutionAtRuntimeBase.FuncString"
              call       void Test::Assert(bool, string)

    IL_00bc:  ret
  } // end of method Program::Positive

  .method public hidebysig static int32  Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       23 (0x17)
    .maxstack  1
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  call       void Program::Negative()
    IL_0006:  nop
    IL_0007:  call       void Program::Positive()
    IL_000c:  nop
    IL_000d:  call       int32 Test::Ret()
    IL_0012:  stloc.0
    IL_0013:  br.s       IL_0015

    IL_0015:  ldloc.0
    IL_0016:  ret
  } // end of method Program::Main

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  } // end of method Program::.ctor

} // end of class Program

.class private auto ansi beforefieldinit Test
       extends [mscorlib]System.Object
{
  .field private static bool Pass
  .method public hidebysig static int32  Ret() cil managed
  {
    // Code size       19 (0x13)
    .maxstack  1
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldsfld     bool Test::Pass
    IL_0006:  brtrue.s   IL_000c

    IL_0008:  ldc.i4.s   101
    IL_000a:  br.s       IL_000e

    IL_000c:  ldc.i4.s   100
    IL_000e:  stloc.0
    IL_000f:  br.s       IL_0011

    IL_0011:  ldloc.0
    IL_0012:  ret
  } // end of method Test::Ret

  .method public hidebysig static void  Assert(bool cond,
                                               string msg) cil managed
  {
    // Code size       47 (0x2f)
    .maxstack  2
    .locals init (bool V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  stloc.0
    IL_0003:  ldloc.0
    IL_0004:  brfalse.s  IL_0015

    IL_0006:  nop
    IL_0007:  ldstr      "PASS"
    IL_000c:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_0011:  nop
    IL_0012:  nop
    IL_0013:  br.s       IL_002e

    IL_0015:  nop
    IL_0016:  ldstr      "FAIL: "
    IL_001b:  ldarg.1
    IL_001c:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_0021:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_0026:  nop
    IL_0027:  ldc.i4.0
    IL_0028:  stsfld     bool Test::Pass
    IL_002d:  nop
    IL_002e:  ret
  } // end of method Test::Assert

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  } // end of method Test::.ctor

  .method private hidebysig specialname rtspecialname static 
          void  .cctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldc.i4.1
    IL_0001:  stsfld     bool Test::Pass
    IL_0006:  ret
  } // end of method Test::.cctor

} // end of class Test


// =============================================================

// *********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file diamondshape.res
