// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.





.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         
  .ver 2:0:0:0
}
.assembly b441487
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.assembly extern xunit.core {}
.module b441487.exe
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       
.corflags 0x00000001    



.class public auto ansi beforefieldinit Test_b441487.FooException`1<T>
       extends [mscorlib]System.Exception
{
  .method public hidebysig specialname virtual 
          instance string  get_Message() cil managed
  {
    .maxstack  2
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "hello from {0}"
ldnull
starg 0

    IL_0006:  ldtoken    !T
    IL_000b:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_0010:  call       string [mscorlib]System.String::Format(string,
                                                                object)
    IL_0015:  stloc.0
    IL_0016:  br.s       IL_0018

    IL_0018:  ldloc.0
    IL_0019:  ret
  } 

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Exception::.ctor()
    IL_0006:  ret
  } 

  .property instance string Message()
  {
    .get instance string Test_b441487.FooException`1::get_Message()
  } 
} 

.class public auto ansi beforefieldinit Test_b441487.Widget`1<T>
       extends [mscorlib]System.Object
{
  .method public hidebysig instance void 
          Foo() cil managed noinlining
  {
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "Foo"
    IL_0006:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000b:  nop
ldnull
starg 0

    IL_000c:  call       void class Test_b441487.Widget`1<!T>::Hello()
    IL_0011:  nop
    IL_0012:  ret
  } 

  .method public hidebysig static void  Hello() cil managed noinlining
  {
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "hello from {0}"
    IL_0006:  ldtoken    !T
    IL_000b:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_0010:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object)
    IL_0015:  nop
    IL_0016:  ret
  } 

  .method public hidebysig instance void 
          CatchMe() cil managed noinlining
  {
    .maxstack  1
    .locals init (class Test_b441487.FooException`1<!T> V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "CatchMe"
    IL_0006:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000b:  nop
ldnull
starg 0
    .try
    {
      IL_000c:  nop
      IL_000d:  newobj     instance void class Test_b441487.FooException`1<!T>::.ctor()
      IL_0012:  throw

    }  
    catch class Test_b441487.FooException`1<!T> 
    {
      IL_0013:  stloc.0
      IL_0014:  nop
      IL_0015:  ldloc.0
      IL_0016:  call       void [System.Console]System.Console::WriteLine(object)
      IL_001b:  nop
      IL_001c:  nop
      IL_001d:  leave.s    IL_001f

    }  
    IL_001f:  nop
    IL_0020:  ret
  } 

  .method public hidebysig instance void 
          SyncIt() cil managed synchronized noinlining
  {
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "SyncIt"
    IL_0006:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000b:  nop
ldnull
starg 0
    IL_000c:  call       void class Test_b441487.Widget`1<!T>::Hello()
    IL_0011:  nop
    IL_0012:  ret
  } 

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } 

} 

.class public auto ansi beforefieldinit Test_b441487.Turtle`1<.ctor T>
       extends [mscorlib]System.Object
{
  .method public hidebysig instance void 
          Foo() cil managed noinlining
  {
    .maxstack  2
    .locals init (!T V_0,
             !T V_1)
ldnull
starg 0
    IL_0000:  nop
    IL_0001:  ldloca.s   V_1
    IL_0003:  initobj    !T
    IL_0009:  ldloc.1
    IL_000a:  box        !T
    IL_000f:  brfalse.s  IL_001c

    IL_0011:  ldloca.s   V_1
    IL_0013:  initobj    !T
    IL_0019:  ldloc.1
    IL_001a:  br.s       IL_0021

    IL_001c:  call       !!0 [mscorlib]System.Activator::CreateInstance<!T>()
    IL_0021:  stloc.0
    IL_0022:  ret
  } 

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } 

} 

.class public auto ansi Test_b441487.Schyzofrenic`1<T>
       extends [mscorlib]System.Object
{
  .method private hidebysig specialname rtspecialname static
          void  .cctor() cil managed noinlining
  {
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldtoken    !T
    IL_0006:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_000b:  call       void [System.Console]System.Console::WriteLine(object)
    IL_0010:  nop
    IL_0011:  ret
  } 

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } 

} 

.class public abstract auto ansi sealed beforefieldinit Test_b441487.Program
       extends [mscorlib]System.Object
{
  .method public hidebysig static int32  Main(string[] args) cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    .maxstack  2
    .locals init (int32 V_0,
             int32 V_1,
             bool V_2)
    IL_0000:  nop
    IL_0001:  ldc.i4.0
    IL_0002:  stloc.0
    IL_0003:  ldloc.0
    IL_0004:  call       int32 Test_b441487.Program::RunTests<string>()
    IL_0009:  add
    IL_000a:  stloc.0
    IL_000b:  ldloc.0
    IL_000c:  call       int32 Test_b441487.Program::RunTests<int32>()
    IL_0011:  add
    IL_0012:  stloc.0
    IL_0013:  ldloc.0
    IL_0014:  call       int32 Test_b441487.Program::RunTests<object>()
    IL_0019:  add
    IL_001a:  stloc.0
    IL_001b:  ldloc.0
    IL_001c:  call       int32 Test_b441487.Program::RunTurtle<object>()
    IL_0021:  add
    IL_0022:  stloc.0
    IL_0023:  ldloc.0
    IL_0024:  call       int32 Test_b441487.Program::RunSchyzofrenic()
    IL_0029:  add
    IL_002a:  stloc.0
    IL_002b:  ldloc.0
    IL_002c:  ldc.i4.0
    IL_002d:  ceq
    IL_002f:  stloc.2
    IL_0030:  ldloc.2
    IL_0031:  brtrue.s   IL_0044

    IL_0033:  nop
    IL_0034:  ldstr      "Test FAILED"
    IL_0039:  call       void [System.Console]System.Console::WriteLine(string)
    IL_003e:  nop
    IL_003f:  ldc.i4.s   101
    IL_0041:  stloc.1
    IL_0042:  br.s       IL_0055

    IL_0044:  nop
    IL_0045:  ldstr      "Test SUCCESS"
    IL_004a:  call       void [System.Console]System.Console::WriteLine(string)
    IL_004f:  nop
    IL_0050:  ldc.i4.s   100
    IL_0052:  stloc.1
    IL_0053:  br.s       IL_0055

    IL_0055:  ldloc.1
    IL_0056:  ret
  } 

  .method private hidebysig static int32 
          RunSchyzofrenic() cil managed noinlining
  {
    .maxstack  1
    .locals init (class Test_b441487.Schyzofrenic`1<string> V_0,
             int32 V_1)
    IL_0000:  nop
    IL_0001:  ldstr      "Running Schyzofrenic<string>()"
    IL_0006:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  newobj     instance void class Test_b441487.Schyzofrenic`1<string>::.ctor()
    IL_0011:  stloc.0
    IL_0012:  ldc.i4.0
    IL_0013:  stloc.1
    IL_0014:  br.s       IL_0016

    IL_0016:  ldloc.1
    IL_0017:  ret
  } 

  .method private hidebysig static int32 
          RunTurtle<.ctor T>() cil managed noinlining
  {
    .maxstack  2
    .locals init (int32 V_0,
             class [mscorlib]System.NullReferenceException V_1,
             int32 V_2)
    IL_0000:  nop
    IL_0001:  ldstr      "Running turtle for {0}"
    IL_0006:  ldtoken    !!T
    IL_000b:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_0010:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object)
    IL_0015:  nop
    IL_0016:  ldc.i4.0
    IL_0017:  stloc.0
    .try
    {
      IL_0018:  nop
      IL_0019:  newobj     instance void class Test_b441487.Turtle`1<!!T>::.ctor()
      IL_001e:  call       instance void class Test_b441487.Turtle`1<!!T>::Foo()
      IL_0023:  nop
      IL_0024:  nop
      IL_0025:  leave.s    IL_004c

    }  
    catch [mscorlib]System.NullReferenceException 
    {
      IL_0027:  stloc.1
      IL_0028:  nop
      IL_0029:  ldstr      "Turtle<{0}>.Foo() failed"
      IL_002e:  ldtoken    !!T
      IL_0033:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
      IL_0038:  call       void [System.Console]System.Console::WriteLine(string,
                                                                    object)
      IL_003d:  nop
      IL_003e:  ldloc.1
      IL_003f:  call       void [System.Console]System.Console::WriteLine(object)
      IL_0044:  nop
      IL_0045:  ldloc.0
      IL_0046:  ldc.i4.1
      IL_0047:  add
      IL_0048:  stloc.0
      IL_0049:  nop
      IL_004a:  leave.s    IL_004c

    }  
    IL_004c:  nop
    IL_004d:  ldloc.0
    IL_004e:  stloc.2
    IL_004f:  br.s       IL_0051

    IL_0051:  ldloc.2
    IL_0052:  ret
  } 

  .method private hidebysig static int32 
          RunTests<T>() cil managed noinlining
  {
    .maxstack  2
    .locals init (int32 V_0,
             class [mscorlib]System.NullReferenceException V_1,
             int32 V_2)
    IL_0000:  nop
    IL_0001:  ldstr      "Running tests for {0}"
    IL_0006:  ldtoken    !!T
    IL_000b:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_0010:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object)
    IL_0015:  nop
    IL_0016:  ldc.i4.0
    IL_0017:  stloc.0
    .try
    {
      IL_0018:  nop
      IL_0019:  newobj     instance void class Test_b441487.Widget`1<!!T>::.ctor()
      IL_001e:  call       instance void class Test_b441487.Widget`1<!!T>::Foo()
      IL_0023:  nop
      IL_0024:  nop
      IL_0025:  leave.s    IL_004c

    }  
    catch [mscorlib]System.NullReferenceException 
    {
      IL_0027:  stloc.1
      IL_0028:  nop
      IL_0029:  ldstr      "Widget<{0}>.Foo() failed"
      IL_002e:  ldtoken    !!T
      IL_0033:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
      IL_0038:  call       void [System.Console]System.Console::WriteLine(string,
                                                                    object)
      IL_003d:  nop
      IL_003e:  ldloc.1
      IL_003f:  call       void [System.Console]System.Console::WriteLine(object)
      IL_0044:  nop
      IL_0045:  ldloc.0
      IL_0046:  ldc.i4.1
      IL_0047:  add
      IL_0048:  stloc.0
      IL_0049:  nop
      IL_004a:  leave.s    IL_004c

    }  
    IL_004c:  nop
    .try
    {
      IL_004d:  nop
      IL_004e:  newobj     instance void class Test_b441487.Widget`1<!!T>::.ctor()
      IL_0053:  call       instance void class Test_b441487.Widget`1<!!T>::CatchMe()
      IL_0058:  nop
      IL_0059:  nop
      IL_005a:  leave.s    IL_0081

    }  
    catch [mscorlib]System.NullReferenceException 
    {
      IL_005c:  stloc.1
      IL_005d:  nop
      IL_005e:  ldstr      "Widget<{0}>.CatchMe() failed"
      IL_0063:  ldtoken    !!T
      IL_0068:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
      IL_006d:  call       void [System.Console]System.Console::WriteLine(string,
                                                                    object)
      IL_0072:  nop
      IL_0073:  ldloc.1
      IL_0074:  call       void [System.Console]System.Console::WriteLine(object)
      IL_0079:  nop
      IL_007a:  ldloc.0
      IL_007b:  ldc.i4.1
      IL_007c:  add
      IL_007d:  stloc.0
      IL_007e:  nop
      IL_007f:  leave.s    IL_0081

    }  
    IL_0081:  nop
    .try
    {
      IL_0082:  nop
      IL_0083:  newobj     instance void class Test_b441487.Widget`1<!!T>::.ctor()
      IL_0088:  call       instance void class Test_b441487.Widget`1<!!T>::SyncIt()
      IL_008d:  nop
      IL_008e:  nop
      IL_008f:  leave.s    IL_00b6

    }  
    catch [mscorlib]System.NullReferenceException 
    {
      IL_0091:  stloc.1
      IL_0092:  nop
      IL_0093:  ldstr      "Widget<{0}>.SyncIt() failed"
      IL_0098:  ldtoken    !!T
      IL_009d:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
      IL_00a2:  call       void [System.Console]System.Console::WriteLine(string,
                                                                    object)
      IL_00a7:  nop
      IL_00a8:  ldloc.1
      IL_00a9:  call       void [System.Console]System.Console::WriteLine(object)
      IL_00ae:  nop
      IL_00af:  ldloc.0
      IL_00b0:  ldc.i4.1
      IL_00b1:  add
      IL_00b2:  stloc.0
      IL_00b3:  nop
      IL_00b4:  leave.s    IL_00b6

    }  
    IL_00b6:  nop
    IL_00b7:  ldloc.0
    IL_00b8:  stloc.2
    IL_00b9:  br.s       IL_00bb

    IL_00bb:  ldloc.2
    IL_00bc:  ret
  } 

} 
