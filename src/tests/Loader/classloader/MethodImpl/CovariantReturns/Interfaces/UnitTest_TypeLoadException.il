// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console { }
.assembly extern xunit.core {}
.assembly extern System.Runtime { }
.assembly extern mscorlib { }
.assembly UnitTest_TypeLoadException { }

// ========================================================================================
// Types that will be used as return types on the various methods
// ========================================================================================

.class interface public auto ansi abstract IUnused1 { }
.class interface public auto ansi abstract IUnused2 { }

.class interface public auto ansi abstract IA implements IUnused1, IUnused2 { }
.class interface public auto ansi abstract IB implements IUnused1, IUnused2, IA  { }
.class interface public auto ansi abstract IC implements IUnused1, IUnused2, IB  { }

.class interface public auto ansi abstract IGenRetType<X,Y>  { }
.class interface public auto ansi abstract IDictionary<KEY,VAL>  { }

.class public auto ansi abstract CA {}
.class public auto ansi abstract CB extends CA {}
.class public auto ansi abstract CC extends CB {}

.class interface public auto ansi abstract ICovariant<+ T>  { }
.class interface public auto ansi abstract IContravariant<- T>  { }

.class interface public auto ansi abstract IGenDerive1<V1,V2> implements IUnused1, IUnused2, class IGenRetType<!V1,!V2>  { }
.class interface public auto ansi abstract IGenDerive2<U1,U2,U3> implements IUnused1, IUnused2, class IGenDerive1<!U1, class IDictionary<!U3,!U2>>  { }
.class interface public auto ansi abstract IGenDerive3<T1,T2> implements IUnused1, IUnused2, class IGenDerive2<!T1,!T2,string>  { }

.class interface public auto ansi abstract INonGenericDerived1<ARG1,ARG2> implements IUnused1, IUnused2, class IGenRetType<!ARG1,!ARG2>  { }
.class interface public auto ansi abstract INonGenericDerived2<T> implements IUnused1, IUnused2, class INonGenericDerived1<!T,object>  { }
.class interface public auto ansi abstract INonGenericDerived3 implements IUnused1, IUnused2, class INonGenericDerived2<int32>  { }
.class interface public auto ansi abstract INonGenericDerived4 implements IUnused1, IUnused2, INonGenericDerived3  { }

.class interface public auto ansi abstract IGenToNonGen1<V1,V2> implements IUnused1, IUnused2, IC  { }
.class interface public auto ansi abstract IGenToNonGen2<U1,U2,U3> implements IUnused1, IUnused2, class IGenToNonGen1<!U1, class IDictionary<!U3,!U2>>  { }
.class interface public auto ansi abstract IGenToNonGen3<T1,T2> implements IUnused1, IUnused2, class IGenToNonGen2<!T1,!T2,string>  { }

.class interface public auto ansi abstract INonGenThroughGen1<V1,V2> implements IUnused1, IUnused2, IC  { }
.class interface public auto ansi abstract INonGenThroughGen2<U1,U2,U3> implements IUnused1, IUnused2, class INonGenThroughGen1<!U1, class IDictionary<!U3,!U2>>  { }
.class interface public auto ansi abstract INonGenThroughGen3 implements IUnused1, IUnused2, class INonGenThroughGen2<object,int32,string>  { }
.class interface public auto ansi abstract INonGenThroughGen4 implements IUnused1, IUnused2, INonGenThroughGen3  { }

// class implementing the interfaces
.class public auto ansi beforefieldinit NonGenThroughGen4   implements IUnused1, IUnused2, INonGenThroughGen4 { }
.class public auto ansi beforefieldinit GenToNonGen3<T1,T2> implements IUnused1, IUnused2, class IGenToNonGen3<!T1,!T2> { }
.class public auto ansi beforefieldinit NonGenericDerived4  implements IUnused1, IUnused2, INonGenericDerived4 { }
.class public auto ansi beforefieldinit GenDerive3<T1,T2>   implements IUnused1, IUnused2, class IGenDerive3<!T1,!T2> { }
.class public auto ansi beforefieldinit C                   implements IUnused1, IUnused2, IC { }
.class public auto ansi beforefieldinit GenRetType<X,Y>     implements IUnused1, IUnused2, class IGenRetType<!X,!Y> { }

.class public auto ansi beforefieldinit Base<T> {}
.class public auto ansi beforefieldinit Derived extends class Base<class Derived> {}
.class public auto ansi beforefieldinit Derived2 extends class Base<class Derived2> {}

.class interface public auto ansi abstract IVariant<+ V>
{
  .method public hidebysig newslot virtual instance void Test()
  {
    ret
  }
}

// ========================================================================================
// Main base type with various virtual methods that will be overridden later
// ========================================================================================

.class public auto ansi beforefieldinit GenBaseType<A,B>
{
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed  { ret }

  .method public hidebysig newslot virtual instance object MyFunc(string& res)
  {
    ldarg.1
    ldstr "object GenBaseType.MyFunc()"
    stind.ref
    ldnull
    ret
  }
  .method public hidebysig newslot virtual instance class IB MyFunc(string& res)
  {
    ldarg.1
    ldstr "IB GenBaseType.MyFunc()"
    stind.ref
    ldnull
    ret
  }
  .method public hidebysig newslot virtual instance class ICovariant<class CB> MyFuncCovariant(string& res)
  {
    ldarg.1
    ldstr "ICovariant<CB> GenBaseType.MyFuncCovariant()"
    stind.ref
    ldnull
    ret
  }
  .method public hidebysig newslot virtual instance class IContravariant<class CB> MyFuncContravariant(string& res)
  {
    ldarg.1
    ldstr "IContravariant<CB> GenBaseType.MyFuncContravariant()"
    stind.ref
    ldnull
    ret
  }
  .method public hidebysig newslot virtual instance class IB GenToNonGen(string& res)
  {
    ldarg.1
    ldstr "IB GenBaseType.GenToNonGen()"
    stind.ref
    ldnull
    ret
  }
  .method public hidebysig newslot virtual instance class IB NonGenThroughGenFunc(string& res)
  {
    ldarg.1
    ldstr "IB GenBaseType.NonGenThroughGenFunc()"
    stind.ref
    ldnull
    ret
  }
  .method public hidebysig newslot virtual instance class IGenRetType<int32,object> MyGenFunc(string& res)
  {
    ldarg.1
    ldstr "IGenRetType<int32,object> GenBaseType.MyGenFunc()"
    stind.ref
    ldnull
    ret
  }
  .method public hidebysig newslot virtual instance class IGenRetType<!A, class IDictionary<string,!B>> MyGenFunc(string& res)
  {
    ldarg.1
    ldstr "IGenRetType<!A, class IDictionary<string,!B>> GenBaseType.MyGenFunc()"
    stind.ref
    ldnull
    ret
  }

  .method public hidebysig newslot virtual instance class IVariant<class Base<class Derived>> MultiLevelGenericVariantFunc(string&)
  {
    ldarg.1
    ldstr "IVariant<class Base<class Derived>> GenBaseType.MultiLevelGenericVariantFunc()"
    stind.ref
    ldnull
    ret
  }
}

// ========================================================================================
// SECOND LAYER type: overrides *SOME* virtuals on GenBaseType using MethodImpls with
// covariant return types (more derived return types)
// ========================================================================================

.class public auto ansi beforefieldinit GenMiddleType<U,V> extends class GenBaseType<!V,!U>
{
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }

  .method public hidebysig newslot virtual instance class INonGenThroughGen2<object,int32,string> NonGenThroughGenFunc(string& res)
  {
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.PreserveBaseOverridesAttribute::.ctor() = (01 00 00 00)
    .override method instance class IB class GenBaseType<!V,!U>::NonGenThroughGenFunc(string& res)
    ldarg.1
    ldstr "INonGenThroughGen2<object,int32,string> GenMiddleType.NonGenThroughGenFunc()"
    stind.ref
    ldnull
    ret
  }

  .method public hidebysig newslot virtual instance class IGenToNonGen1<!V, class IDictionary<string,object>> GenToNonGen(string& res)
  {
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.PreserveBaseOverridesAttribute::.ctor() = (01 00 00 00)
    .override method instance class IB class GenBaseType<!V,!U>::GenToNonGen(string& res)
    ldarg.1
    ldstr "IGenToNonGen1<!V, class IDictionary<string,object>> GenMiddleType.GenToNonGen()"
    stind.ref
    ldnull
    ret
  }

  .method public hidebysig newslot virtual instance class INonGenericDerived1<int32,object> MyGenFunc(string& res)
  {
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.PreserveBaseOverridesAttribute::.ctor() = (01 00 00 00)
    .override method instance class IGenRetType<int32,object> class GenBaseType<!V,!U>::MyGenFunc(string& res)
    ldarg.1
    ldstr "INonGenericDerived1<int32,object> GenMiddleType.MyGenFunc()"
    stind.ref
    ldnull
    ret
  }

  .method public hidebysig newslot virtual instance class IGenDerive1<!V, class IDictionary<string,!U>> MyGenFunc(string& res)
  {
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.PreserveBaseOverridesAttribute::.ctor() = (01 00 00 00)
    .override method instance class IGenRetType<!0, class IDictionary<string,!1>> class GenBaseType<!V,!U>::MyGenFunc(string& res)
    ldarg.1
    ldstr "IGenDerive1<!U, class IDictionary<string,!V>> GenMiddleType.MyGenFunc()"
    stind.ref
    ldnull
    ret
  }
}


// ========================================================================================
// THIRD LAYER type: overrides all virtuals from GenBaseType using MethodImpls with
// covariant return types (more derived return types than the ones used in GenMiddleType)
// ========================================================================================

.class public auto ansi beforefieldinit GenTestType<UNUSED1,UNUSED2,U,V> extends class GenMiddleType<!V,!U>
{
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }

  .method public hidebysig newslot virtual instance class INonGenThroughGen4 NonGenThroughGenFunc(string& res)
  {
    .override method instance class IB class GenBaseType<!U,!V>::NonGenThroughGenFunc(string& res)
    ldarg.1
    ldstr "INonGenThroughGen4 TestType.NonGenThroughGenFunc()"
    stind.ref
    ldnull
    ret
  }

  .method public hidebysig newslot virtual instance class IGenToNonGen3<!U,object> GenToNonGen(string& res)
  {
    .override method instance class IB class GenBaseType<!U,!V>::GenToNonGen(string& res)
    ldarg.1
    ldstr "IGenToNonGen3<!U,object> TestType.GenToNonGen()"
    stind.ref
    ldnull
    ret
  }

  .method public hidebysig newslot virtual instance class INonGenericDerived4 MyGenFunc(string& res)
  {
    .override method instance class IGenRetType<int32,object> class GenBaseType<!U,!V>::MyGenFunc(string& res)
    ldarg.1
    ldstr "INonGenericDerived4 TestType.MyGenFunc()"
    stind.ref
    ldnull
    ret
  }

  .method public hidebysig newslot virtual instance class IGenDerive3<!U,!V> MyGenFunc(string& res)
  {
    .override method instance class IGenRetType<!0, class IDictionary<string,!1>> class GenBaseType<!U,!V>::MyGenFunc(string& res)
    ldarg.1
    ldstr "IGenDerive3<!U,!V> TestType.MyGenFunc()"
    stind.ref
    ldnull
    ret
  }

  .method public hidebysig newslot virtual instance class IGenRetType<!U,!V> MyFunc(string& res)
  {
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.PreserveBaseOverridesAttribute::.ctor() = (01 00 00 00)
    .override method instance object class GenBaseType<!U,!V>::MyFunc(string& res)
    ldarg.1
    ldstr "IGenRetType<!U,!V> TestType.MyFunc()"
    stind.ref
    ldnull
    ret
  }

  .method public hidebysig newslot virtual instance class IC MyFunc(string& res)
  {
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.PreserveBaseOverridesAttribute::.ctor() = (01 00 00 00)
    .override method instance class IB class GenBaseType<!U,!V>::MyFunc(string& res)
    ldarg.1
    ldstr "IC TestType.MyFunc()"
    stind.ref
    ldnull
    ret
  }

  .method public hidebysig newslot virtual instance class ICovariant<class CC> MyFuncCovariant(string& res)
  {
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.PreserveBaseOverridesAttribute::.ctor() = (01 00 00 00)
    .override method instance class ICovariant<class CB> class GenBaseType<!U,!V>::MyFuncCovariant(string& res)
    ldarg.1
    ldstr "ICovariant<CC> TestType.MyFuncCovariant()"
    stind.ref
    ldnull
    ret
  }

  .method public hidebysig newslot virtual instance class IContravariant<class CA> MyFuncContravariant(string& res)
  {
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.PreserveBaseOverridesAttribute::.ctor() = (01 00 00 00)
    .override method instance class IContravariant<class CB> class GenBaseType<!U,!V>::MyFuncContravariant(string& res)
    ldarg.1
    ldstr "IContravariant<CA> TestType.MyFuncContravariant()"
    stind.ref
    ldnull
    ret
  }

  // ========================================================================================
  // Set of implicit overrides that should be ignored given there are explicit overrides from the MethodImpls
  // ========================================================================================
  .method public hidebysig virtual instance class IB NonGenThroughGenFunc(string& res)
  {
    ldstr "Should never execute this method"
    newobj instance void [System.Runtime]System.Exception::.ctor(string)
    throw
  }

  .method public hidebysig virtual instance class IB GenToNonGen(string& res)
  {
    ldstr "Should never execute this method"
    newobj instance void [System.Runtime]System.Exception::.ctor(string)
    throw
  }

  .method public hidebysig virtual instance class IGenRetType<int32,object> MyGenFunc(string& res)
  {
    ldstr "Should never execute this method"
    newobj instance void [System.Runtime]System.Exception::.ctor(string)
    throw
  }

  .method public hidebysig virtual instance class IGenRetType<!2, class IDictionary<string,!3>> MyGenFunc(string& res)
  {
    ldstr "Should never execute this method"
    newobj instance void [System.Runtime]System.Exception::.ctor(string)
    throw
  }

  .method public hidebysig virtual instance object MyFunc(string& res)
  {
    ldstr "Should never execute this method"
    newobj instance void [System.Runtime]System.Exception::.ctor(string)
    throw
  }

  .method public hidebysig virtual instance class IB MyFunc(string& res)
  {
    ldstr "Should never execute this method"
    newobj instance void [System.Runtime]System.Exception::.ctor(string)
    throw
  }

  .method public hidebysig virtual instance class ICovariant<class CB> MyFuncCovariant(string& res)
  {
    ldstr "Should never execute this method"
    newobj instance void [System.Runtime]System.Exception::.ctor(string)
    throw
  }

  .method public hidebysig virtual instance class IContravariant<class CB> MyFuncContravariant(string& res)
  {
    ldstr "Should never execute this method"
    newobj instance void [System.Runtime]System.Exception::.ctor(string)
    throw
  }
}

// ========================================================================================
// FOURTH LAYER type: overrides all virtuals from GenBaseType using MethodImpls with
// covariant return types (classes that implement the interfaces used as return types)
// ========================================================================================

.class public auto ansi beforefieldinit GenMoreDerived<UNUSED1,UNUSED2,U,V> extends class GenTestType<!UNUSED2,!UNUSED1,!U,!V>
{
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }

  .method public hidebysig newslot virtual instance class NonGenThroughGen4 NonGenThroughGenFunc(string& res)
  {
    .override method instance class IB class GenBaseType<!U,!V>::NonGenThroughGenFunc(string& res)
    ldarg.1
    ldstr "class NonGenThroughGen4 GenMoreDerived.NonGenThroughGenFunc()"
    stind.ref
    ldnull
    ret
  }

  .method public hidebysig newslot virtual instance class GenToNonGen3<!U,object> GenToNonGen(string& res)
  {
    .override method instance class IB class GenBaseType<!U,!V>::GenToNonGen(string& res)
    ldarg.1
    ldstr "class GenToNonGen3<!U,object> GenMoreDerived.GenToNonGen()"
    stind.ref
    ldnull
    ret
  }

  .method public hidebysig newslot virtual instance class NonGenericDerived4 MyGenFunc(string& res)
  {
    .override method instance class IGenRetType<int32,object> class GenBaseType<!U,!V>::MyGenFunc(string& res)
    ldarg.1
    ldstr "class NonGenericDerived4 GenMoreDerived.MyGenFunc()"
    stind.ref
    ldnull
    ret
  }

  .method public hidebysig newslot virtual instance class GenDerive3<!U,!V> MyGenFunc(string& res)
  {
    .override method instance class IGenRetType<!0, class IDictionary<string,!1>> class GenBaseType<!U,!V>::MyGenFunc(string& res)
    ldarg.1
    ldstr "class GenDerive3<!U,!V> GenMoreDerived.MyGenFunc()"
    stind.ref
    ldnull
    ret
  }

  .method public hidebysig newslot virtual instance class GenRetType<!U,!V> MyFunc(string& res)
  {
    .override method instance object class GenBaseType<!U,!V>::MyFunc(string& res)
    ldarg.1
    ldstr "class GenRetType<!U,!V> GenMoreDerived.MyFunc()"
    stind.ref
    ldnull
    ret
  }

  .method public hidebysig newslot virtual instance class C MyFunc(string& res)
  {
    .override method instance class IB class GenBaseType<!U,!V>::MyFunc(string& res)
    ldarg.1
    ldstr "class C GenMoreDerived.MyFunc()"
    stind.ref
    ldnull
    ret
  }

  .method public hidebysig newslot virtual instance class IVariant<class Derived> MultiLevelGenericVariantFunc(string&)
  {
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.PreserveBaseOverridesAttribute::.ctor() = (01 00 00 00)
    .override method instance class IVariant<class Base<class Derived>> class GenBaseType<!U,!V>::MultiLevelGenericVariantFunc(string&)
    ldarg.1
    ldstr "class IVariant<class Derived> GenMoreDerived.MultiLevelGenericVariantFunc()"
    stind.ref
    ldnull
    ret
  }
}


// ========================================================================================
// FIFTH LAYER INVALID type: Used to verify we can't override the method using a compatible interface
// if it has been already overridden using a class that implements the interface (i.e. the new
// interface return type won't be compatible with the class return type on the previous override
// ========================================================================================

.class public auto ansi beforefieldinit Invalid1<Q,W> extends class GenMoreDerived<!Q,!W,!Q,!W>
{
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }

  .method public hidebysig newslot virtual instance class INonGenThroughGen4 NonGenThroughGenFunc(string& res)
  {
    .override method instance class IB class GenBaseType<!Q,!W>::NonGenThroughGenFunc(string& res)
    ldnull
    ret
  }
}

// ========================================================================================
// FIFTH LAYER INVALID type: Used to verify we can't override the method using a less derived interface
// than one that has already been used in a previous override
// ========================================================================================

.class public auto ansi beforefieldinit Invalid2<Q,W> extends class GenTestType<!Q,!W,!Q,!W>
{
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }

  .method public hidebysig newslot virtual instance class INonGenThroughGen2<object,int32,string> NonGenThroughGenFunc(string& res)
  {
    .override method instance class IB class GenBaseType<!Q,!W>::NonGenThroughGenFunc(string& res)
    ldnull
    ret
  }
}

.class public auto ansi beforefieldinit Invalid3<Q,W> extends class GenTestType<!Q,!W,!Q,!W>
{
  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed { ret }

  .method public hidebysig newslot virtual instance class IVariant<class Derived2> MultiLevelGenericVariantFunc(string&)
  {
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.PreserveBaseOverridesAttribute::.ctor() = (01 00 00 00)
    .override method instance class IVariant<class Base<class Derived>> class GenBaseType<!Q,!W>::MultiLevelGenericVariantFunc(string&)
    ldnull
    ret
  }
}

  // ===================================================================================== //

  .method public static void RunTest_Invalid1() noinlining
  {
    newobj     instance void class Invalid1<int32,object>::.ctor()
    call       void [System.Console]System.Console::WriteLine(object)
    ret
  }

  .method public static void RunTest_Invalid2() noinlining
  {
    newobj     instance void class Invalid2<int32,object>::.ctor()
    call       void [System.Console]System.Console::WriteLine(object)
    ret
  }

  .method public static void RunTest_Invalid3() noinlining
  {
    newobj     instance void class Invalid3<int32,object>::.ctor()
    callvirt   instance class IVariant<class Derived2> class Invalid3<int32,object>::MultiLevelGenericVariantFunc()
    pop
    ret
  }
  // ===================================================================================== //

  .method public hidebysig static int32  Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    .maxstack  2
    .locals init ( bool result )

    ldc.i4.1
    stloc.0

    INVALID1:
    .try
    {
        call void CMain::RunTest_Invalid1()
        ldc.i4.0
        stloc.0
        ldstr      "FAIL: did not catch expected TypeLoadException when loading Invalid1."
        call       void [System.Console]System.Console::WriteLine(string)
        leave.s INVALID2
    }
    catch [mscorlib]System.TypeLoadException
    {
        ldstr "Caught expected TypeLoadException:"
        call       void [System.Console]System.Console::WriteLine(string)
        call       void [System.Console]System.Console::WriteLine(object)
        leave.s INVALID2
    }

    INVALID2:
    .try
    {
        call void CMain::RunTest_Invalid2()
        ldc.i4.0
        stloc.0
        ldstr      "FAIL: did not catch expected TypeLoadException when loading Invalid2."
        call       void [System.Console]System.Console::WriteLine(string)
        leave.s INVALID3
    }
    catch [mscorlib]System.TypeLoadException
    {
        ldstr "Caught expected TypeLoadException:"
        call       void [System.Console]System.Console::WriteLine(string)
        call       void [System.Console]System.Console::WriteLine(object)
        leave.s INVALID3
    }

    INVALID3:
    .try
    {
        call void CMain::RunTest_Invalid3()
        ldc.i4.0
        stloc.0
        ldstr      "FAIL: did not catch expected TypeLoadException when loading DerivedClassFail1."
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

    // ===================================================================================== //

    DONE:
    ldloc.0
    brtrue.s   PASS

    ldstr      "Test FAILED"
    call       void [System.Console]System.Console::WriteLine(string)
    ldc.i4.s   101
    ret

    PASS:
    ldstr      "Test PASSED"
    call       void [System.Console]System.Console::WriteLine(string)
    ldc.i4.s   100
    ret

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
