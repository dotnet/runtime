// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console { }
.assembly extern xunit.core {}
/*

using System;
using System.Runtime.CompilerServices;

interface IFoo {
    void foo();
}

struct MyStruct : IFoo {
    public void foo() {
    }
}

struct MyGStruct<T> : IFoo {
    public void foo() {
    }
}

class Program
{
    static bool NonGeneric()
    {
        RuntimeMethodHandle mh1 = typeof(MyStruct).GetMethod("foo").MethodHandle;
        RuntimeMethodHandle mh2 = new RuntimeMethodHandle(); // replace with "ldtoken method instance void MyStruct::foo()"
        return mh1 == mh2;
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    static bool Generic<T>() {
        RuntimeMethodHandle mh1 = typeof(MyGStruct<String>).GetMethod("foo").MethodHandle;
        RuntimeMethodHandle mh2 = new RuntimeMethodHandle();
        return mh1 == mh2;
    }

    public static int Main()
    {
        if (!NonGeneric()) {
            Console.WriteLine("NonGeneric FAILED");
            return 1;
        }

        if (!Generic<String>()) {
            Console.WriteLine("Generic case FAILED");
            return 1;
        }

        Console.WriteLine("PASSED");
        return 100;
    }
}

*/

.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 4:0:0:0
}
.assembly 'dev10_393447'
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::.ctor() = ( 01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
                                                                                                             63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01 )       // ceptionThrows.
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
// MVID: {1A8972AD-8254-4063-BA42-1661A4257C01}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x00310000


// =============== CLASS MEMBERS DECLARATION ===================

.class interface private abstract auto ansi IFoo
{
  .method public hidebysig newslot abstract virtual 
          instance void  foo() cil managed
  {
  } // end of method IFoo::foo

} // end of class IFoo

.class private sequential ansi sealed beforefieldinit MyStruct
       extends [mscorlib]System.ValueType
       implements IFoo
{
  .pack 0
  .size 1
  .method public hidebysig newslot virtual final 
          instance void  foo() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method MyStruct::foo

} // end of class MyStruct

.class private sequential ansi sealed beforefieldinit MyGStruct`1<T>
       extends [mscorlib]System.ValueType
       implements IFoo
{
  .pack 0
  .size 1
  .method public hidebysig newslot virtual final 
          instance void  foo() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method MyGStruct`1::foo

} // end of class MyGStruct`1

.class private auto ansi beforefieldinit Program
       extends [mscorlib]System.Object
{
  .method private hidebysig static bool  NonGeneric() cil managed
  {
    // Code size       42 (0x2a)
    .maxstack  2
    .locals init (valuetype [mscorlib]System.RuntimeMethodHandle V_0,
             valuetype [mscorlib]System.RuntimeMethodHandle V_1)
    IL_0000:  ldtoken    MyStruct
    IL_0005:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_000a:  ldstr      "foo"
    IL_000f:  call       instance class [mscorlib]System.Reflection.MethodInfo [mscorlib]System.Type::GetMethod(string)
    IL_0014:  callvirt   instance valuetype [mscorlib]System.RuntimeMethodHandle [mscorlib]System.Reflection.MethodBase::get_MethodHandle()
    IL_0019:  stloc.0

              ldtoken    method instance void MyStruct::foo()
              stloc.1

    IL_0022:  ldloc.0
    IL_0023:  ldloc.1
    IL_0024:  call       bool [mscorlib]System.RuntimeMethodHandle::op_Equality(valuetype [mscorlib]System.RuntimeMethodHandle,
                                                                                valuetype [mscorlib]System.RuntimeMethodHandle)
    IL_0029:  ret
  } // end of method Program::NonGeneric

  .method private hidebysig static bool  Generic<T>() cil managed noinlining
  {
    // Code size       42 (0x2a)
    .maxstack  2
    .locals init (valuetype [mscorlib]System.RuntimeMethodHandle V_0,
             valuetype [mscorlib]System.RuntimeMethodHandle V_1)
    IL_0000:  ldtoken    valuetype MyGStruct`1<string>
    IL_0005:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_000a:  ldstr      "foo"
    IL_000f:  call       instance class [mscorlib]System.Reflection.MethodInfo [mscorlib]System.Type::GetMethod(string)
    IL_0014:  callvirt   instance valuetype [mscorlib]System.RuntimeMethodHandle [mscorlib]System.Reflection.MethodBase::get_MethodHandle()
    IL_0019:  stloc.0

              ldtoken    method instance void valuetype MyGStruct`1<string>::foo()
              stloc.1

    IL_0022:  ldloc.0
    IL_0023:  ldloc.1
    IL_0024:  call       bool [mscorlib]System.RuntimeMethodHandle::op_Equality(valuetype [mscorlib]System.RuntimeMethodHandle,
                                                                                valuetype [mscorlib]System.RuntimeMethodHandle)
    IL_0029:  ret
  } // end of method Program::Generic

  .method public hidebysig static int32  Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       51 (0x33)
    .maxstack  8
    IL_0000:  call       bool Program::NonGeneric()
    IL_0005:  brtrue.s   IL_0013

    IL_0007:  ldstr      "NonGeneric FAILED"
    IL_000c:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0011:  ldc.i4.1
    IL_0012:  ret

    IL_0013:  call       bool Program::Generic<string>()
    IL_0018:  brtrue.s   IL_0026

    IL_001a:  ldstr      "Generic case FAILED"
    IL_001f:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0024:  ldc.i4.1
    IL_0025:  ret

    IL_0026:  ldstr      "PASSED"
    IL_002b:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0030:  ldc.i4.s   100
    IL_0032:  ret
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
// WARNING: Created Win32 resource file bug393447.res
