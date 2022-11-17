// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// Invalid scenario verification: overriding a base type method returning an interface type
// with a MethodImpl that returns a struct implementing that interface should throw:
// struct is castable to the interface it implements, but not compatible with it.
//

.assembly extern System.Console { }
.assembly extern xunit.core {}
.assembly extern mscorlib { }
.assembly IncompatibleOverride { }


// ========================================================================================
// Types that will be used as return types on the various methods
// ========================================================================================

.class interface public auto ansi abstract IA { }
.class interface public auto ansi abstract IGenRetType<X,Y>  { }
.class interface public auto ansi abstract IDictionary<KEY,VAL>  { }

.class interface public auto ansi abstract IB implements IA { }
.class interface public auto ansi abstract IGenDerive<V1,V2> implements class IGenRetType<!V1,!V2>  { }

// Structs implementing the interfaces
.class public sealed auto ansi A extends [mscorlib]System.ValueType
    implements IA { }

.class public sealed auto ansi B extends [mscorlib]System.ValueType
    implements IB { }

.class public sealed auto ansi RetType extends [mscorlib]System.ValueType
    implements class IGenRetType<int32,string> { }

.class public sealed auto ansi DerivedRetType extends [mscorlib]System.ValueType
    implements class IGenDerive<int32,string> { }

.class public sealed auto ansi GenRetType<U,V> extends [mscorlib]System.ValueType
    implements class IGenRetType<!U, class IDictionary<string,!V>> { }

.class public sealed auto ansi DerivedGenRetType<U,V> extends [mscorlib]System.ValueType
    implements class IGenDerive<!U, class IDictionary<string,!V>> { }

// ========================================================================================
// Main base type with various virtual methods that will be overridden later
// ========================================================================================

.class public auto ansi beforefieldinit GenBaseType<A,B>
{
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed  { ret }

  .method public hidebysig newslot virtual instance class IA NonGen(string& res)
  {
    ldarg.1
    ldstr "IA GenBaseType.NonGen()"
    stind.ref
    ldnull
    ret
  }
  .method public hidebysig newslot virtual instance class IGenRetType<int32,string> GenFunc(string& res)
  {
    ldarg.1
    ldstr "IGenRetType<int32,string> GenBaseType.GenFunc()"
    stind.ref
    ldnull
    ret
  }
  .method public hidebysig newslot virtual instance class IGenRetType<!A, class IDictionary<string,!B>> GenFunc(string& res)
  {
    ldarg.1
    ldstr "IGenRetType<!A, class IDictionary<string,!B>> GenBaseType.GenFunc()"
    stind.ref
    ldnull
    ret
  }
}

// ========================================================================================
// INVALID TYPES: override base type methods and return structs implementing the return type
// interfaces of the overridden methods.
// ========================================================================================


.class public auto ansi beforefieldinit Invalid1<U,V> extends class GenBaseType<!U,!V>
{
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
  .method public hidebysig newslot virtual instance valuetype A NonGen(string& res)
  {
    .override method instance class IA class GenBaseType<!U,!V>::NonGen(string& res)
    .locals ( valuetype A )
    ldloc.0
    ret
  }
}

.class public auto ansi beforefieldinit Invalid2<U,V> extends class GenBaseType<!U,!V>
{
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
  .method public hidebysig newslot virtual instance valuetype B NonGen(string& res)
  {
    .override method instance class IA class GenBaseType<!U,!V>::NonGen(string& res)
    .locals ( valuetype B )
    ldloc.0
    ret
  }
}

.class public auto ansi beforefieldinit Invalid3<U,V> extends class GenBaseType<!U,!V>
{
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
  .method public hidebysig newslot virtual instance valuetype RetType GenFunc(string& res)
  {
    .override method instance class IGenRetType<int32,string> class GenBaseType<!U,!V>::GenFunc(string& res)
    .locals ( valuetype RetType )
    ldloc.0
    ret
  }
}

.class public auto ansi beforefieldinit Invalid4<U,V> extends class GenBaseType<!U,!V>
{
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
  .method public hidebysig newslot virtual instance valuetype DerivedRetType GenFunc(string& res)
  {
    .override method instance class IGenRetType<int32,string> class GenBaseType<!U,!V>::GenFunc(string& res)
    .locals ( valuetype DerivedRetType )
    ldloc.0
    ret
  }
}

.class public auto ansi beforefieldinit Invalid5<U,V> extends class GenBaseType<!U,!V>
{
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
  .method public hidebysig newslot virtual instance valuetype GenRetType<!U,!V> GenFunc(string& res)
  {
    .override method instance class IGenRetType<!U, class IDictionary<string,!V>> class GenBaseType<!U,!V>::GenFunc(string& res)
    .locals ( valuetype GenRetType<!U,!V> )
    ldloc.0
    ret
  }
}

.class public auto ansi beforefieldinit Invalid6<U,V> extends class GenBaseType<!U,!V>
{
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
  .method public hidebysig newslot virtual instance valuetype DerivedGenRetType<!U,!V> GenFunc(string& res)
  {
    .override method instance class IGenRetType<!U, class IDictionary<string,!V>> class GenBaseType<!U,!V>::GenFunc(string& res)
    .locals ( valuetype DerivedGenRetType<!U,!V> )
    ldloc.0
    ret
  }
}

// ========================================================================================

.class public auto ansi beforefieldinit CMain extends [mscorlib]System.Object
{
  .method public static void RunTest1() noinlining
  {
    newobj     instance void class Invalid1<int32,object>::.ctor()
    call       void [System.Console]System.Console::WriteLine(object)
    ret
  }

  .method public static void RunTest2() noinlining
  {
    newobj     instance void class Invalid2<int32,object>::.ctor()
    call       void [System.Console]System.Console::WriteLine(object)
    ret
  }

  .method public static void RunTest3() noinlining
  {
    newobj     instance void class Invalid3<int32,object>::.ctor()
    call       void [System.Console]System.Console::WriteLine(object)
    ret
  }

  .method public static void RunTest4() noinlining
  {
    newobj     instance void class Invalid4<int32,object>::.ctor()
    call       void [System.Console]System.Console::WriteLine(object)
    ret
  }

  .method public static void RunTest5() noinlining
  {
    newobj     instance void class Invalid5<int32,object>::.ctor()
    call       void [System.Console]System.Console::WriteLine(object)
    ret
  }

  .method public static void RunTest6() noinlining
  {
    newobj     instance void class Invalid6<int32,object>::.ctor()
    call       void [System.Console]System.Console::WriteLine(object)
    ret
  }

  .method public hidebysig static int32  Main( string[] args) cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    .maxstack  2
    .locals init (bool V_0)
    ldc.i4.1
    stloc.0

    T1:
    .try
    {
        call void CMain::RunTest1()
        ldc.i4.0
        stloc.0
        ldstr      "FAIL: did not catch expected TypeLoadException when loading Test1."
        call       void [System.Console]System.Console::WriteLine(string)
        leave.s T2
    }
    catch [mscorlib]System.TypeLoadException
    {
        ldstr "Caught expected TypeLoadException:"
        call       void [System.Console]System.Console::WriteLine(string)
        call       void [System.Console]System.Console::WriteLine(object)
        leave.s T2
    }

    T2:
    .try
    {
        call void CMain::RunTest2()
        ldc.i4.0
        stloc.0
        ldstr      "FAIL: did not catch expected TypeLoadException when loading Test2."
        call       void [System.Console]System.Console::WriteLine(string)
        leave.s T3
    }
    catch [mscorlib]System.TypeLoadException
    {
        ldstr "Caught expected TypeLoadException:"
        call       void [System.Console]System.Console::WriteLine(string)
        call       void [System.Console]System.Console::WriteLine(object)
        leave.s T3
    }

    T3:
    .try
    {
        call void CMain::RunTest3()
        ldc.i4.0
        stloc.0
        ldstr      "FAIL: did not catch expected TypeLoadException when loading Test3."
        call       void [System.Console]System.Console::WriteLine(string)
        leave.s T4
    }
    catch [mscorlib]System.TypeLoadException
    {
        ldstr "Caught expected TypeLoadException:"
        call       void [System.Console]System.Console::WriteLine(string)
        call       void [System.Console]System.Console::WriteLine(object)
        leave.s T4
    }

    T4:
    .try
    {
        call void CMain::RunTest4()
        ldc.i4.0
        stloc.0
        ldstr      "FAIL: did not catch expected TypeLoadException when loading Test4."
        call       void [System.Console]System.Console::WriteLine(string)
        leave.s T5
    }
    catch [mscorlib]System.TypeLoadException
    {
        ldstr "Caught expected TypeLoadException:"
        call       void [System.Console]System.Console::WriteLine(string)
        call       void [System.Console]System.Console::WriteLine(object)
        leave.s T5
    }

    T5:
    .try
    {
        call void CMain::RunTest5()
        ldc.i4.0
        stloc.0
        ldstr      "FAIL: did not catch expected TypeLoadException when loading Test5."
        call       void [System.Console]System.Console::WriteLine(string)
        leave.s T6
    }
    catch [mscorlib]System.TypeLoadException
    {
        ldstr "Caught expected TypeLoadException:"
        call       void [System.Console]System.Console::WriteLine(string)
        call       void [System.Console]System.Console::WriteLine(object)
        leave.s T6
    }

    T6:
    .try
    {
        call void CMain::RunTest6()
        ldc.i4.0
        stloc.0
        ldstr      "FAIL: did not catch expected TypeLoadException when loading Test5."
        call       void [System.Console]System.Console::WriteLine(string)
        leave.s DONE
    }
    catch [mscorlib]System.TypeLoadException
    {
        ldstr "Caught expected TypeLoadException:"
        call       void [System.Console]System.Console::WriteLine(string)
        call       void [System.Console]System.Console::WriteLine(object)
        leave.s DONE
    }

    DONE:

    ldloc.0
    brtrue.s   PASS

    ldc.i4.s   101
    ret

    PASS:
    ldstr      "Test PASSED"
    call       void [System.Console]System.Console::WriteLine(string)
    ldc.i4.s   100
    ret
  }

  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method CMain::.ctor

} // end of class CMain
