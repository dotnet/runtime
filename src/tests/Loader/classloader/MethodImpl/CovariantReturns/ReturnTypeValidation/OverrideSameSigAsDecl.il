// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console { }
.assembly extern xunit.core {}
.assembly extern System.Runtime { }
.assembly extern mscorlib { }
.assembly OverrideSameSigAsDecl { }

// ========================================================================================
// Types that will be used as return types on the various methods
// ========================================================================================

.class public auto ansi beforefieldinit A {
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
}
.class public auto ansi beforefieldinit B extends A {
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
}
.class public auto ansi beforefieldinit C extends B {
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
}

.class public auto ansi beforefieldinit GenRetType<X,Y> {
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
}
.class public auto ansi beforefieldinit Dictionary<KEY,VAL> {
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
}

.class public auto ansi beforefieldinit GenDerive1<V1,V2> extends class GenRetType<!V1,!V2> {
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
}
.class public auto ansi beforefieldinit GenDerive2<U1,U2,U3> extends class GenDerive1<!U1, class Dictionary<!U3,!U2>> {
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
}
.class public auto ansi beforefieldinit GenDerive3<T1,T2> extends class GenDerive2<!T1,!T2,string> {
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
}

.class public auto ansi beforefieldinit NonGenericDerived1<ARG1,ARG2> extends class GenRetType<!ARG1,!ARG2> {
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
}
.class public auto ansi beforefieldinit NonGenericDerived2<T> extends class NonGenericDerived1<!T,object> {
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
}
.class public auto ansi beforefieldinit NonGenericDerived3 extends class NonGenericDerived2<int32> {
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
}
.class public auto ansi beforefieldinit NonGenericDerived4 extends NonGenericDerived3 {
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
}

.class public auto ansi beforefieldinit GenToNonGen1<V1,V2> extends C {
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
}
.class public auto ansi beforefieldinit GenToNonGen2<U1,U2,U3> extends class GenToNonGen1<!U1, class Dictionary<!U3,!U2>> {
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
}
.class public auto ansi beforefieldinit GenToNonGen3<T1,T2> extends class GenToNonGen2<!T1,!T2,string> {
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
}


.class public auto ansi beforefieldinit NonGenThroughGen1<V1,V2> extends C {
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
}
.class public auto ansi beforefieldinit NonGenThroughGen2<U1,U2,U3> extends class NonGenThroughGen1<!U1, class Dictionary<!U3,!U2>> {
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
}
.class public auto ansi beforefieldinit NonGenThroughGen3 extends class NonGenThroughGen2<object,int32,string> {
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
}
.class public auto ansi beforefieldinit NonGenThroughGen4 extends NonGenThroughGen3 {
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
}

// ========================================================================================
// Main base type with various virtual methods that will be overridden later
// ========================================================================================

.class public auto ansi beforefieldinit GenBaseType<A,B>
{
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }

  .method public hidebysig newslot virtual instance object MyFunc(string& res)
  {
    ldarg.1
    ldstr "object GenBaseType.MyFunc()"
    stind.ref
    newobj instance void A::.ctor()
    ret
  }
  .method public hidebysig newslot virtual instance class B MyFunc(string& res)
  {
    ldarg.1
    ldstr "B GenBaseType.MyFunc()"
    stind.ref
    newobj instance void B::.ctor()
    ret
  }
  .method public hidebysig newslot virtual instance class B GenToNonGen(string& res)
  {
    ldarg.1
    ldstr "B GenBaseType.GenToNonGen()"
    stind.ref
    newobj instance void B::.ctor()
    ret
  }
  .method public hidebysig newslot virtual instance class B NonGenThroughGenFunc(string& res)
  {
    ldarg.1
    ldstr "B GenBaseType.NonGenThroughGenFunc()"
    stind.ref
    newobj instance void B::.ctor()
    ret
  }
  .method public hidebysig newslot virtual instance class GenRetType<int32,object> MyGenFunc(string& res)
  {
    ldarg.1
    ldstr "GenRetType<int32,object> GenBaseType.MyGenFunc()"
    stind.ref
    newobj instance void class GenRetType<int32,object>::.ctor()
    ret
  }
  .method public hidebysig newslot virtual instance class GenRetType<!A, class Dictionary<string,!B>> MyGenFunc(string& res)
  {
    ldarg.1
    ldstr "GenRetType<!A, class Dictionary<string,!B>> GenBaseType.MyGenFunc()"
    stind.ref
    newobj instance void class GenRetType<!A, class Dictionary<string,!B>>::.ctor()
    ret
  }
  .method public hidebysig newslot virtual instance class GenRetType<!A,!B> TestNonGenericDerived(string& res)
  {
    ldarg.1
    ldstr "GenRetType<!A,!B> GenBaseType.TestNonGenericDerived()"
    stind.ref
    newobj instance void class GenRetType<!A,!B>::.ctor()
    ret
  }
}


// ========================================================================================
// SECOND LAYER type: overrides all virtuals on GenBaseType using MethodImpls with
// covariant return types (more derived return types)
// ========================================================================================

.class public auto ansi beforefieldinit GenTestType<U,V> extends class GenBaseType<!U,!V>
{
  .method public hidebysig newslot virtual instance class NonGenThroughGen4 NonGenThroughGenOverride(string& res)
  {
    .override method instance class B class GenBaseType<!U,!V>::NonGenThroughGenFunc(string& res)
    ldarg.1
    ldstr "NonGenThroughGen4 TestType.NonGenThroughGenFunc()"
    stind.ref
    ldnull
    ret
  }

  .method public hidebysig newslot virtual instance class GenToNonGen3<!V,object> GenToNonGenOverride(string& res)
  {
    .override method instance class B class GenBaseType<!U,!V>::GenToNonGen(string& res)
    ldarg.1
    ldstr "GenToNonGen3<!V,object> TestType.GenToNonGenOverride()"
    stind.ref
    ldnull
    ret
  }

  .method public hidebysig newslot virtual instance class NonGenericDerived4 NewGenFunc1(string& res)
  {
    .override method instance class GenRetType<int32,object> class GenBaseType<!U,!V>::MyGenFunc(string& res)
    ldarg.1
    ldstr "NonGenericDerived4 TestType.NewGenFunc1()"
    stind.ref
    ldnull
    ret
  }

  .method public hidebysig newslot virtual instance class GenDerive3<!U,!V> NewGenFunc2(string& res)
  {
    .override method instance class GenRetType<!0, class Dictionary<string,!1>> class GenBaseType<!U,!V>::MyGenFunc(string& res)
    ldarg.1
    ldstr "GenDerive3<!U,!V> TestType.NewGenFunc2()"
    stind.ref
    ldnull
    ret
  }

  .method public hidebysig newslot virtual instance class GenRetType<!U,!V> NewFunc1(string& res)
  {
    .override method instance object class GenBaseType<!U,!V>::MyFunc(string& res)
    ldarg.1
    ldstr "GenRetType<!U,!V> TestType.NewFunc1()"
    stind.ref
    ldnull
    ret
  }

  .method public hidebysig newslot virtual instance class C NewFunc2(string& res)
  {
    .override method instance class B class GenBaseType<!U,!V>::MyFunc(string& res)
    ldarg.1
    ldstr "C TestType.NewFunc2()"
    stind.ref
    ldnull
    ret
  }

  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed
  {
    .maxstack  8
    ldarg.0
    call       instance void class GenBaseType<!U,!V>::.ctor()
    ret
  }
}

// ========================================================================================
// THIRD LAYER INVALID types: explicitly override the virtual methods on GenBaseType, using
// the same signature as GenBaseType, and are expected to throw TypeLoadExceptions on load
// because the return types here are not compatible with the return types used on GenTestType
// ========================================================================================

.class public auto ansi beforefieldinit Invalid1<U,V> extends class GenTestType<!U,!V>
{
  .method public hidebysig newslot virtual instance class B NonGenThroughGenOverride_Invalid(string& res)
  {
    .override method instance class B class GenBaseType<!U,!V>::NonGenThroughGenFunc(string& res)
    ldnull
    ret
  }

  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
}

.class public auto ansi beforefieldinit Invalid2<U,V> extends class GenTestType<!U,!V>
{
  .method public hidebysig newslot virtual instance class B GenToNonGenOverride_Invalid(string& res)
  {
    .override method instance class B class GenBaseType<!U,!V>::GenToNonGen(string& res)
    ldnull
    ret
  }

  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
}

.class public auto ansi beforefieldinit Invalid3<U,V> extends class GenTestType<!U,!V>
{
  .method public hidebysig newslot virtual instance class GenRetType<int32,object> NewGenFunc1_Invalid(string& res)
  {
    .override method instance class GenRetType<int32,object> class GenBaseType<!U,!V>::MyGenFunc(string& res)
    ldnull
    ret
  }

  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
}

.class public auto ansi beforefieldinit Invalid4<U,V> extends class GenTestType<!U,!V>
{
  .method public hidebysig newslot virtual instance class GenRetType<!U, class Dictionary<string,!V>> NewGenFunc2_Invalid(string& res)
  {
    .override method instance class GenRetType<!0, class Dictionary<string,!1>> class GenBaseType<!U,!V>::MyGenFunc(string& res)
    ldnull
    ret
  }

  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
}

.class public auto ansi beforefieldinit Invalid5<U,V> extends class GenTestType<!U,!V>
{
  .method public hidebysig newslot virtual instance object NewFunc1_Invalid(string& res)
  {
    .override method instance object class GenBaseType<!U,!V>::MyFunc(string& res)
    ldnull
    ret
  }

  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
}

.class public auto ansi beforefieldinit Invalid6<U,V> extends class GenTestType<!U,!V>
{
  .method public hidebysig newslot virtual instance class B NewFunc2_Invalid(string& res)
  {
    .override method instance class B class GenBaseType<!U,!V>::MyFunc(string& res)
    ldnull
    ret
  }

  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
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
        ldstr      "FAIL: did not catch expected TypeLoadException when loading Invalid1."
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
        ldstr      "FAIL: did not catch expected TypeLoadException when loading Invalid2."
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
        ldstr      "FAIL: did not catch expected TypeLoadException when loading Invalid3."
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
        ldstr      "FAIL: did not catch expected TypeLoadException when loading Invalid4."
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
        ldstr      "FAIL: did not catch expected TypeLoadException when loading Invalid5."
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
        ldstr      "FAIL: did not catch expected TypeLoadException when loading Invalid6."
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
    .maxstack  8
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }
}
