// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { }
.assembly extern helperdll { }

.assembly helperildll {}
.module helperildll.dll

.class auto ansi public beforefieldinit HelperILCode
       extends [mscorlib]System.Object
{
  .method public hidebysig static native int GetFunctionPointerFromOtherModule() cil managed noinlining
  {
     ldftn      void [helperdll]HelperClass::FunctionPointerReferencedMethod()
     ret
  }
  .method public hidebysig static void CallFunctionPointer(native int) cil managed noinlining
  {
     ldarg.0
     calli void()
     ret
  }

  .method public hidebysig static valuetype[mscorlib]System.RuntimeMethodHandle GetNonGenericFunctionMethodHandle() cil managed noinlining
  {
     ldtoken method instance int32 valuetype [helperdll]GenericStructForLdtoken`1<string>::NonGenericFunction(!0, int32)
     ret
  }

  .method  public hidebysig static valuetype[mscorlib]System.RuntimeMethodHandle GetGenericFunctionMethodHandle() cil managed noinlining
  {
     ldtoken method instance int32 valuetype [helperdll]GenericStructForLdtoken`1<string>::GenericFunction<valuetype [helperdll]GenericStructForLdtoken`1<object>>(!0, !!0, string, int32)
     ret
  }

  .method  public hidebysig static valuetype[mscorlib]System.RuntimeMethodHandle GetGenericFunctionMethodHandle<V>() cil managed noinlining
  {
     ldtoken method instance int32 valuetype [helperdll]GenericStructForLdtoken`1<string>::GenericFunction<!!0>(!0, !!0, string, int32)
     ret
  }

  .method public hidebysig static  void LdTokenArrayMethodsInt(valuetype[mscorlib]System.RuntimeMethodHandle& ctor, valuetype[mscorlib]System.RuntimeMethodHandle& ctor2, valuetype[mscorlib]System.RuntimeMethodHandle& set, valuetype[mscorlib]System.RuntimeMethodHandle& get, valuetype[mscorlib]System.RuntimeMethodHandle& address)
  {
     ldarg 0
     ldtoken method instance void int32[,]::.ctor(int32, int32, int32, int32)
     stobj valuetype[mscorlib]System.RuntimeMethodHandle

     ldarg 1
     ldtoken method instance void int32[,]::.ctor(int32, int32)
     stobj valuetype[mscorlib]System.RuntimeMethodHandle

     ldarg 2
     ldtoken method instance void int32[,]::Set(int32, int32, int32)
     stobj valuetype[mscorlib]System.RuntimeMethodHandle

     ldarg 3
     ldtoken method instance int32 int32[,]::Get(int32, int32)
     stobj valuetype[mscorlib]System.RuntimeMethodHandle

     ldarg 4
     ldtoken method instance int32& int32[,]::Address(int32, int32)
     stobj valuetype[mscorlib]System.RuntimeMethodHandle
     ret
  }

  .method public hidebysig static  void LdTokenArrayMethodsString(valuetype[mscorlib]System.RuntimeMethodHandle& ctor, valuetype[mscorlib]System.RuntimeMethodHandle& ctor2, valuetype[mscorlib]System.RuntimeMethodHandle& set, valuetype[mscorlib]System.RuntimeMethodHandle& get, valuetype[mscorlib]System.RuntimeMethodHandle& address)
  {
     ldarg 0
     ldtoken method instance void string[,]::.ctor(int32, int32, int32, int32)
     stobj valuetype[mscorlib]System.RuntimeMethodHandle

     ldarg 1
     ldtoken method instance void string[,]::.ctor(int32, int32)
     stobj valuetype[mscorlib]System.RuntimeMethodHandle

     ldarg 2
     ldtoken method instance void string[,]::Set(int32, int32, string)
     stobj valuetype[mscorlib]System.RuntimeMethodHandle

     ldarg 3
     ldtoken method instance string string[,]::Get(int32, int32)
     stobj valuetype[mscorlib]System.RuntimeMethodHandle

     ldarg 4
     ldtoken method instance string& string[,]::Address(int32, int32)
     stobj valuetype[mscorlib]System.RuntimeMethodHandle
     ret
  }


  .method public hidebysig static valuetype[mscorlib]System.RuntimeMethodHandle ForceStuffToBeCompiled() cil managed noinlining
  {
     call valuetype[mscorlib]System.RuntimeMethodHandle class HelperGenericILCode`1<object>::GetGenericFunctionMethodHandle<valuetype [helperdll]GenericStructForLdtoken`1<string>>()
     ret
  }
  .method public hidebysig static valuetype[mscorlib]System.RuntimeMethodHandle ForceStuffToBeCompiled2() cil managed noinlining
  {
     call valuetype[mscorlib]System.RuntimeMethodHandle HelperILCode::GetGenericFunctionMethodHandle<string>()
     ret
  }
  .method public hidebysig static void ForceStuffToBeCompiled3(valuetype[mscorlib]System.RuntimeMethodHandle& ctor, valuetype[mscorlib]System.RuntimeMethodHandle& ctor2, valuetype[mscorlib]System.RuntimeMethodHandle& set, valuetype[mscorlib]System.RuntimeMethodHandle& get, valuetype[mscorlib]System.RuntimeMethodHandle& address) cil managed noinlining
  {
     ldarg 0
     ldarg 1
     ldarg 2
     ldarg 3
     ldarg 4
     call void class HelperGenericILCode`1<object>::LdTokenArrayMethods(valuetype[mscorlib]System.RuntimeMethodHandle&, valuetype[mscorlib]System.RuntimeMethodHandle&, valuetype[mscorlib]System.RuntimeMethodHandle&, valuetype[mscorlib]System.RuntimeMethodHandle&, valuetype[mscorlib]System.RuntimeMethodHandle&)
     ldarg 0
     ldarg 1
     ldarg 2
     ldarg 3
     ldarg 4
     call void class HelperGenericILCode`1<int32>::LdTokenArrayMethods(valuetype[mscorlib]System.RuntimeMethodHandle& ctor, valuetype[mscorlib]System.RuntimeMethodHandle& ctor2, valuetype[mscorlib]System.RuntimeMethodHandle& set, valuetype[mscorlib]System.RuntimeMethodHandle& get, valuetype[mscorlib]System.RuntimeMethodHandle& address)
     ret
  }
}

.class auto ansi public beforefieldinit HelperGenericILCode`1<T>
       extends [mscorlib]System.Object
{
  .method  public hidebysig static valuetype[mscorlib]System.RuntimeMethodHandle GetNonGenericFunctionMethodHandle() cil managed noinlining
  {
     ldtoken method instance int32 valuetype [helperdll]GenericStructForLdtoken`1<!0>::NonGenericFunction(!0, int32)
     ret
  }

  .method  public hidebysig static valuetype[mscorlib]System.RuntimeMethodHandle GetGenericFunctionMethodHandle<V>() cil managed noinlining
  {
     ldtoken method instance int32 valuetype [helperdll]GenericStructForLdtoken`1<!0>::GenericFunction<!!0>(!0, !!0, string, int32)
     ret
  }
  .method public hidebysig static  void LdTokenArrayMethods(valuetype[mscorlib]System.RuntimeMethodHandle& ctor, valuetype[mscorlib]System.RuntimeMethodHandle& ctor2, valuetype[mscorlib]System.RuntimeMethodHandle& set, valuetype[mscorlib]System.RuntimeMethodHandle& get, valuetype[mscorlib]System.RuntimeMethodHandle& address)
  {
     ldarg 0
     ldtoken method instance void !0[,]::.ctor(int32, int32, int32, int32)
     stobj valuetype[mscorlib]System.RuntimeMethodHandle

     ldarg 1
     ldtoken method instance void !0[,]::.ctor(int32, int32)
     stobj valuetype[mscorlib]System.RuntimeMethodHandle

     ldarg 2
     ldtoken method instance void !0[,]::Set(int32, int32, !0)
     stobj valuetype[mscorlib]System.RuntimeMethodHandle

     ldarg 3
     ldtoken method instance !0 !0[,]::Get(int32, int32)
     stobj valuetype[mscorlib]System.RuntimeMethodHandle

     ldarg 4
     ldtoken method instance !0& !0[,]::Address(int32, int32)
     stobj valuetype[mscorlib]System.RuntimeMethodHandle
     ret
  }
}

.class auto ansi public beforefieldinit BaseTypeNonGenericMethodActuallyOnBase_ModuleLocal extends [mscorlib]System.Object
{
  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
  { 
    ldarg.0
    call instance void [mscorlib]System.Object::.ctor()
    ret
  }

  .method public hidebysig instance string Method() cil managed noinlining
  {
    ldstr "BaseTypeNonGenericMethodActuallyOnBase::Method"
    ret
  }
}

.class auto ansi public beforefieldinit DerivedTypeGenericMethodActuallyOnBase_ModuleLocal`1<T0> extends BaseTypeNonGenericMethodActuallyOnBase_ModuleLocal
{
  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
  { 
    ldarg.0
    call instance void BaseTypeNonGenericMethodActuallyOnBase_ModuleLocal::.ctor()
    ret
  }
}

.class auto ansi public beforefieldinit DerivedTypeGenericMethodActuallyOnBase`1<T0> extends [helperdll]BaseTypeNonGenericMethodActuallyOnBase
{
  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
  { 
    ldarg.0
    call instance void [helperdll]BaseTypeNonGenericMethodActuallyOnBase::.ctor()
    ret
  }
}

.class auto ansi public beforefieldinit DerivedTypeNonGenericMethodActuallyOnBase`1<T0> extends class [helperdll]BaseTypeGenericMethodActuallyOnBase<string>
{
  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
  { 
    ldarg.0
    call instance void class [helperdll]BaseTypeGenericMethodActuallyOnBase<string>::.ctor()
    ret
  }
}

.class public auto ansi beforefieldinit HelperILDllTests {

    .method static class [mscorlib]System.Func`1<string> GetDelegateMethodFromGenericType<T0>() cil managed noinlining
    {
        newobj       instance void class DerivedTypeGenericMethodActuallyOnBase_ModuleLocal`1<!!0>::.ctor()
        ldftn        instance string class DerivedTypeGenericMethodActuallyOnBase_ModuleLocal`1<!!T0>::Method()
        newobj       instance void class [mscorlib]System.Func`1<string>::.ctor(object, native int)
        ret
    }

    .method static string CallMethodFromGenericType<T0>() cil managed noinlining
    {
        newobj       instance void class DerivedTypeGenericMethodActuallyOnBase`1<!!0>::.ctor()
        callvirt     instance string class DerivedTypeGenericMethodActuallyOnBase`1<!!T0>::Method()
        ret
    }

    .method static string CallGenMethodFromGenericType<T0>() cil managed noinlining
    {
        newobj       instance void class DerivedTypeGenericMethodActuallyOnBase`1<!!0>::.ctor()
        callvirt     instance string class DerivedTypeGenericMethodActuallyOnBase`1<!!T0>::GenMethod<!!0>()
        ret
    }

    .method public hidebysig static bool CallMethodUsingMemberRefToDerivedWhereMethodIsActuallyOnBase_GenericCreateDelegate() cil managed noinlining
    {
        .maxstack  10
        .locals init (object V_0)
        call         class [mscorlib]System.Func`1<string> HelperILDllTests::GetDelegateMethodFromGenericType<object>()
        callvirt     instance !0 class [mscorlib]System.Func`1<string>::Invoke()
        ldstr        "BaseTypeNonGenericMethodActuallyOnBase::Method"
        call         bool [mscorlib]System.String::op_Equality(string,string)
        ret
    }


    .method public hidebysig static bool CallMethodUsingMemberRefToDerivedWhereMethodIsActuallyOnBase_GenericCaller() cil managed noinlining
    {
        .maxstack  10
        .locals init (object V_0)
        call         string HelperILDllTests::CallMethodFromGenericType<object>()
        ldstr        "BaseTypeNonGenericMethodActuallyOnBase::Method"
        call         bool [mscorlib]System.String::op_Equality(string,string)
        ret
    }

    .method public hidebysig static bool CallGenMethodUsingMemberRefToDerivedWhereMethodIsActuallyOnBase_GenericCaller() cil managed noinlining
    {
        .maxstack  10
        .locals init (object V_0)
        call         string HelperILDllTests::CallGenMethodFromGenericType<object>()
        ldstr        "BaseTypeNonGenericMethodActuallyOnBase::GenMethod<T>"
        call         bool [mscorlib]System.String::op_Equality(string,string)
        ret
    }

    .method public hidebysig static bool CallMethodUsingMemberRefToDerivedWhereMethodIsActuallyOnBase_NonGenericCaller() cil managed noinlining
    {
        .maxstack  10
        .locals init (object V_0)
        newobj       instance void class DerivedTypeGenericMethodActuallyOnBase`1<object>::.ctor()
        callvirt     instance string class DerivedTypeGenericMethodActuallyOnBase`1<object>::Method()
        ldstr        "BaseTypeNonGenericMethodActuallyOnBase::Method"
        call         bool [mscorlib]System.String::op_Equality(string,string)
        ret
    }

    .method public hidebysig static bool CallGenMethodUsingMemberRefToDerivedWhereMethodIsActuallyOnBase_NonGenericCaller() cil managed noinlining
    {
        .maxstack  10
        .locals init (object V_0)
        newobj       instance void class DerivedTypeGenericMethodActuallyOnBase`1<object>::.ctor()
        callvirt     instance string class DerivedTypeGenericMethodActuallyOnBase`1<object>::GenMethod<string>()
        ldstr        "BaseTypeNonGenericMethodActuallyOnBase::GenMethod<T>"
        call         bool [mscorlib]System.String::op_Equality(string,string)
        ret
    }
}
