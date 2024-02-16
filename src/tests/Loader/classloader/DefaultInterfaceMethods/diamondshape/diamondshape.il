// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 4:0:0:0
}
.assembly extern xunit.core {}
// TODO: use the contract once this is exposed from contracts
.assembly extern System.Runtime { }

.assembly diamondshape
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
          instance int32  Foo(int32 a) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldarg.1
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret                 
  } // end of method IFoo::Foo

} // end of class IFoo

.class interface private abstract auto ansi IFoo2
       implements IFoo
{
  .method public hidebysig newslot virtual final 
          instance int32  Foo(int32 a) cil managed
  {
    .override IFoo::Foo
    // Code size       20 (0x14)
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "At IFoo2.Foo"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldarg.1
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
  .method public hidebysig newslot virtual final 
          instance int32  IFoo.Foo(int32 a) cil managed
  {
    .override IFoo::Foo
    // Code size       20 (0x14)
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "At IFooEx.Foo"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldarg.1
    IL_000d:  ldc.i4.2
    IL_000e:  add
    IL_000f:  stloc.0
    IL_0010:  br.s       IL_0012

    IL_0012:  ldloc.0
    IL_0013:  ret
  } // end of method IFooEx::IFoo.Foo
} // end of class IFooEx

.class private auto ansi beforefieldinit FooClass
       extends [mscorlib]System.Object
       implements IFoo2,
                  IFoo,
                  IFooEx
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  } // end of method FooClass::.ctor

} // end of class FooClass

.class interface private abstract auto ansi I1
{
  .method public hidebysig newslot abstract virtual 
          instance int32  Func(int32 a) cil managed
  {
  } // end of method I1::Func

} // end of class I1

.class interface private abstract auto ansi I2
       implements I1
{
  .method private hidebysig newslot virtual final 
          instance int32  I1.Func(int32 a) cil managed
  {
    .override I1::Func
    // Code size       20 (0x14)
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "At I2.Func"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldarg.1
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
  .method private hidebysig newslot virtual final 
          instance int32  I1.Func(int32 a) cil managed
  {
    .override I1::Func
    // Code size       20 (0x14)
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "At I3.Func"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldarg.1
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
  .method private hidebysig newslot virtual final 
          instance int32  I1.Func(int32 a) cil managed
  {
    .override I1::Func
    // Code size       20 (0x14)
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "At I4.Func"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldarg.1
    IL_000d:  ldc.i4.4
    IL_000e:  add
    IL_000f:  stloc.0
    IL_0010:  br.s       IL_0012

    IL_0012:  ldloc.0
    IL_0013:  ret
  } // end of method I4::I1.Func   
} // end of class I4

.class private auto ansi beforefieldinit I4Class
       extends [mscorlib]System.Object
       implements I4,
                  I2,
                  I1,
                  I3
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  } // end of method I4Class::.ctor

} // end of class I4Class

.class interface private abstract auto ansi I5
       implements I1
{
  .method private hidebysig newslot virtual final 
          instance int32  I1.Func(int32 a) cil managed
  {
    .override I1::Func
    // Code size       20 (0x14)
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "At I5.Func"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldarg.1
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
  .method private hidebysig newslot virtual final 
          instance int32  I1.Func(int32 a) cil managed
  {
    .override I1::Func
    // Code size       20 (0x14)
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "At I6.Func"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldarg.1
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
  .method private hidebysig newslot virtual final 
          instance int32  I1.Func(int32 a) cil managed
  {
    .override I1::Func
    // Code size       20 (0x14)
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "At I7.Func"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldarg.1
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
  .method private hidebysig newslot virtual final 
          instance int32  I1.Func(int32 a) cil managed
  {
    .override I1::Func
    // Code size       20 (0x14)
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "At I8.Func"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldarg.1
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
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  } // end of method I47Class::.ctor

} // end of class I47Class

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
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  } // end of method I8Class::.ctor

} // end of class I8Class  

.class interface private abstract auto ansi GI1`1<T>
{
  .method public hidebysig newslot abstract virtual 
          instance int32  Func<S>([out] class [mscorlib]System.Type[]& types) cil managed
  {
  } // end of method GI1`1::'GI1<T>.Func' 

} // end of class GI1`1

.class interface private abstract auto ansi GI2`1<T>
       implements class GI1`1<!T>
{
.method private hidebysig newslot virtual final 
          instance int32  'GI1<T>.Func'<S>([out] class [mscorlib]System.Type[]& types) cil managed
  {
    .override  method instance int32 class GI1`1<!T>::Func<[1]>(class [mscorlib]System.Type[]&)
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
    IL_003c:  ldarg.1
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
.method private hidebysig newslot virtual final 
          instance int32  'GI1<T>.Func'<S>([out] class [mscorlib]System.Type[]& types) cil managed
  {
    .override  method instance int32 class GI1`1<!T>::Func<[1]>(class [mscorlib]System.Type[]&)
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
    IL_003c:  ldarg.1
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
 .method private hidebysig newslot virtual final 
          instance int32  'GI1<T>.Func'<S>([out] class [mscorlib]System.Type[]& types) cil managed
  {
    .override  method instance int32 class GI1`1<!T>::Func<[1]>(class [mscorlib]System.Type[]&)
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
    IL_003c:  ldarg.1
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
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  } // end of method GI23Class`1::.ctor

} // end of class GI23Class`1

.class private auto ansi beforefieldinit GI4Class`1<T>
       extends [mscorlib]System.Object
       implements class GI4`1<!T>,
                  class GI2`1<!T>,
                  class GI1`1<!T>,
                  class GI3`1<!T>
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  } // end of method GI4Class`1::.ctor

} // end of class GI4Class`1

.class public auto ansi beforefieldinit Program
       extends [mscorlib]System.Object
{
  .method public hidebysig static void  Negative() cil managed
  {
    // Code size       225 (0xe1)
    .maxstack  2
    .locals init (class FooClass V_0,
             class IFoo V_1,
             class I47Class V_2,
             class I1 V_3,
             class GI23Class`1<object> V_4,
             class GI1`1<object> V_5,
             class [mscorlib]System.Exception V_6,
             class [mscorlib]System.Exception V_7,
             class [mscorlib]System.Type[] V_8,
             class [mscorlib]System.Exception V_9)
    IL_0000:  nop
    IL_0001:  newobj     instance void FooClass::.ctor()
    IL_0006:  stloc.0
    IL_0007:  ldloc.0
    IL_0008:  stloc.1
    IL_0009:  ldstr      "Calling IFoo.Foo on Foo - expecting exception."
    IL_000e:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_0013:  nop
    .try
    {
      IL_0014:  nop
      IL_0015:  ldloc.1
      IL_0016:  ldc.i4.s   10
      IL_0018:  callvirt   instance int32 IFoo::Foo(int32)
      IL_001d:  pop
      IL_001e:  ldc.i4.0
      IL_001f:  ldstr      "Expecting exception on Foo"
      IL_0024:  call       void Test::Assert(bool,
                                             string)
      IL_0029:  nop
      IL_002a:  nop
      IL_002b:  leave.s    IL_004a

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
      IL_0048:  leave.s    IL_004a

    }  // end handler
    IL_004a:  newobj     instance void I47Class::.ctor()
    IL_004f:  stloc.2
    IL_0050:  ldloc.2
    IL_0051:  stloc.3
    IL_0052:  ldstr      "Calling I1.Func on I47Class - expecting exception"
    IL_0057:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_005c:  nop
    .try
    {
      IL_005d:  nop
      IL_005e:  ldloc.3
      IL_005f:  ldc.i4.s   10
      IL_0061:  callvirt   instance int32 I1::Func(int32)
      IL_0066:  pop
      IL_0067:  ldc.i4.0
      IL_0068:  ldstr      "Expecting exception on I47Class"
      IL_006d:  call       void Test::Assert(bool,
                                             string)
      IL_0072:  nop
      IL_0073:  nop
      IL_0074:  leave.s    IL_0093

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
      IL_0091:  leave.s    IL_0093

    }  // end handler
    IL_0093:  newobj     instance void class GI23Class`1<object>::.ctor()
    IL_0098:  stloc.s    V_4
    IL_009a:  ldloc.s    V_4
    IL_009c:  stloc.s    V_5
    IL_009e:  ldstr      "Calling GI1<T>.Func on GI23Class<S> - expecting ex"
    + "ception"
    IL_00a3:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_00a8:  nop
    .try
    {
      IL_00a9:  nop
      IL_00aa:  ldloc.s    V_5
      IL_00ac:  ldloca.s   V_8
      IL_00ae:  callvirt   instance int32 class GI1`1<object>::Func<string>(class [mscorlib]System.Type[]&)
      IL_00b3:  pop
      IL_00b4:  ldc.i4.0
      IL_00b5:  ldstr      "Expecting exception on GI23Class"
      IL_00ba:  call       void Test::Assert(bool,
                                             string)
      IL_00bf:  nop
      IL_00c0:  nop
      IL_00c1:  leave.s    IL_00e0

    }  // end .try
    catch [System.Runtime]System.Runtime.AmbiguousImplementationException
    {
      IL_00c3:  stloc.s    V_9
      IL_00c5:  nop
      IL_00c6:  ldstr      "Exception caught: "
      IL_00cb:  ldloc.s    V_9
      IL_00cd:  callvirt   instance string [mscorlib]System.Object::ToString()
      IL_00d2:  call       string [mscorlib]System.String::Concat(string,
                                                                  string)
      IL_00d7:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_00dc:  nop
      IL_00dd:  nop
      IL_00de:  leave.s    IL_00e0

    }  // end handler
    IL_00e0:  ret 
  } // end of method Program::Negative

  .method public hidebysig static void  Positive() cil managed
  {
    // Code size       189 (0xbd)
    .maxstack  2
    .locals init (class I4Class V_0,
             class I1 V_1,
             class I8Class V_2,
             class GI4Class`1<object> V_3,
             class [mscorlib]System.Type[] V_4,
             class GI1`1<object> V_5)
    IL_0000:  nop
    IL_0001:  ldstr      "Calling I1.Func on I4Class - expecting I4.Func"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  newobj     instance void I4Class::.ctor()
    IL_0011:  stloc.0
    IL_0012:  ldloc.0
    IL_0013:  stloc.1
    IL_0014:  ldloc.1
    IL_0015:  ldc.i4.s   10
    IL_0017:  callvirt   instance int32 I1::Func(int32)
    IL_001c:  ldc.i4.s   14
    IL_001e:  ceq
    IL_0020:  ldstr      "Expecting I1.Func to land on I4.Func"
    IL_0025:  call       void Test::Assert(bool,
                                           string)
    IL_002a:  nop
    IL_002b:  ldstr      "Calling I1.Func on I8Class - expecting I8.Func"
    IL_0030:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_0035:  nop
    IL_0036:  newobj     instance void I8Class::.ctor()
    IL_003b:  stloc.2
    IL_003c:  ldloc.2
    IL_003d:  stloc.1
    IL_003e:  ldloc.1
    IL_003f:  ldc.i4.s   10
    IL_0041:  callvirt   instance int32 I1::Func(int32)
    IL_0046:  ldc.i4.s   18
    IL_0048:  ceq
    IL_004a:  ldstr      "Expecting I1.Func to land on I8.Func"
    IL_004f:  call       void Test::Assert(bool,
                                           string)
    IL_0054:  nop
    IL_0055:  ldstr      "Calling GI1.Func on GI4Class<object> - expecting G"
    + "I4.Func<S>"
    IL_005a:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_005f:  nop
    IL_0060:  newobj     instance void class GI4Class`1<object>::.ctor()
    IL_0065:  stloc.3
    IL_0066:  ldloc.3
    IL_0067:  stloc.s    V_5
    IL_0069:  ldloc.s    V_5
    IL_006b:  ldloca.s   V_4
    IL_006d:  callvirt   instance int32 class GI1`1<object>::Func<string>(class [mscorlib]System.Type[]&)
    IL_0072:  ldc.i4.4
    IL_0073:  ceq
    IL_0075:  ldstr      "Expecting GI1<T>.Func to land on GII4<T>.Func<S>"
    IL_007a:  call       void Test::Assert(bool,
                                           string)
    IL_007f:  nop
    IL_0080:  ldloc.s    V_4
    IL_0082:  ldc.i4.0
    IL_0083:  ldelem.ref
    IL_0084:  ldtoken    [mscorlib]System.Object
    IL_0089:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_008e:  call       bool [mscorlib]System.Type::op_Equality(class [mscorlib]System.Type,
                                                                 class [mscorlib]System.Type)
    IL_0093:  ldstr      "T must be object"
    IL_0098:  call       void Test::Assert(bool,
                                           string)
    IL_009d:  nop
    IL_009e:  ldloc.s    V_4
    IL_00a0:  ldc.i4.1
    IL_00a1:  ldelem.ref
    IL_00a2:  ldtoken    [mscorlib]System.String
    IL_00a7:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_00ac:  call       bool [mscorlib]System.Type::op_Equality(class [mscorlib]System.Type,
                                                                 class [mscorlib]System.Type)
    IL_00b1:  ldstr      "S must be string"
    IL_00b6:  call       void Test::Assert(bool,
                                           string)
    IL_00bb:  nop
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
