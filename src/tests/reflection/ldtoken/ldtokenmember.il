// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib
{
}
.assembly extern xunit.core {}

.assembly ldtokenmember
{
}

.class public auto ansi ldtokenmember {
.method public hidebysig static int32 Main() cil managed
{
  .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
      01 00 00 00
  )
  .entrypoint
  .maxstack  8
  .locals init (class [mscorlib]System.Type nonGenericHolderType,
                class [mscorlib]System.Reflection.FieldInfo staticField,
                class [mscorlib]System.Reflection.MethodBase staticInt32Method,
                class [mscorlib]System.Reflection.MethodBase staticMethodT,
                class [mscorlib]System.Reflection.MethodBase staticMethodU,
                class [mscorlib]System.Reflection.FieldInfo staticFieldT,
                class [mscorlib]System.Reflection.MethodBase weirdoGenericMethod,
                class [mscorlib]System.Reflection.MethodBase otherWeirdoGenericMethod)

  //
  // Type nonGenericHolderType = typeof(NonGenericHolder)
  //
  ldtoken    class NonGenericHolder
  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(
                 valuetype [mscorlib]System.RuntimeTypeHandle)
  stloc      nonGenericHolderType

  //
  // FieldInfo staticField = infoof(NonGenericHolder.StaticField)
  //
  ldtoken    field object NonGenericHolder::StaticField
  call       class [mscorlib]System.Reflection.FieldInfo 
                 [mscorlib]System.Reflection.FieldInfo::GetFieldFromHandle(
                     valuetype [mscorlib]System.RuntimeFieldHandle)
  stloc      staticField

  //
  // if (staticField.Name != "StaticField" || staticField.DeclaringType != nonGenericHolderType)
  //     return 1;
  //
  ldloc      staticField
  callvirt   instance string [mscorlib]System.Reflection.FieldInfo::get_Name()
  ldstr      "StaticField"
  callvirt   instance bool [mscorlib]System.Object::Equals(object)
  ldloc      staticField
  callvirt   instance class [mscorlib]System.Type [mscorlib]System.Reflection.FieldInfo::get_DeclaringType()
  ldloc      nonGenericHolderType
  ceq
  and
  brtrue     NonGenericStaticFieldOK
  ldc.i4     1
  ret
NonGenericStaticFieldOK:

  //
  // MethodInfo staticInt32Method = infoof(NonGenericHolder.StaticMethod)
  //
  ldtoken    method int32 NonGenericHolder::StaticMethod()
  call       class [mscorlib]System.Reflection.MethodBase 
                 [mscorlib]System.Reflection.MethodBase::GetMethodFromHandle(
                     valuetype [mscorlib]System.RuntimeMethodHandle)
  stloc      staticInt32Method

  //
  // if (staticInt32Method.Name != "StaticField" || staticInt32Method.DeclaringType != nonGenericHolderType)
  //     return 2;
  //
  ldloc      staticInt32Method
  callvirt   instance string [mscorlib]System.Reflection.MethodBase::get_Name()
  ldstr      "StaticMethod"
  callvirt   instance bool [mscorlib]System.Object::Equals(object)
  ldloc      staticInt32Method
  callvirt   instance class [mscorlib]System.Type [mscorlib]System.Reflection.MethodBase::get_DeclaringType()
  ldloc      nonGenericHolderType
  ceq
  and
  brtrue     NonGenericStaticMethodOK
  ldc.i4     2
  ret
NonGenericStaticMethodOK:

  //
  // if (staticInt32Method.Equals(infoof(NonGenericHolder.StaticMethod))
  //     return 3;
  //
  ldtoken    method int16 NonGenericHolder::StaticMethod()
  call       class [mscorlib]System.Reflection.MethodBase 
                 [mscorlib]System.Reflection.MethodBase::GetMethodFromHandle(
                     valuetype [mscorlib]System.RuntimeMethodHandle)
  ldloc      staticInt32Method
  callvirt   instance bool [mscorlib]System.Object::Equals(object)
  brfalse    OverloadsOK
  ldc.i4     3
  ret
OverloadsOK:

  //
  // MethodBase staticMethodT = GenericHolder<string, string>.GetInfoOfMethodT();
  //
  call       class [mscorlib]System.Reflection.MethodBase class GenericHolder`2<string, string>::GetInfoOfMethodT()
  stloc      staticMethodT

  //
  // if (!staticMethodT.Equals(infoof(GenericHolder<string, string>.StaticMethod)))
  //     return 4
  //
  ldtoken    method void class GenericHolder`2<string, string>::StaticMethod(!0)
  ldtoken    class GenericHolder`2<string,string>
  call       class [mscorlib]System.Reflection.MethodBase 
               [mscorlib]System.Reflection.MethodBase::GetMethodFromHandle(
                   valuetype [mscorlib]System.RuntimeMethodHandle,
                   valuetype [mscorlib]System.RuntimeTypeHandle)
  ldloc      staticMethodT
  callvirt   instance bool [mscorlib]System.Object::Equals(object)
  brtrue     MethodFromSharedContextOK
  ldc.i4     4
  ret
MethodFromSharedContextOK:

  //
  // MethodBase staticMethodU = GenericHolder<string, string>.GetInfoOfMethodU();
  //
  call        class [mscorlib]System.Reflection.MethodBase class GenericHolder`2<string, string>::GetInfoOfMethodU()
  stloc       staticMethodU

  //
  // if (staticMethodT.Equals(staticMethodU))
  //     return 5;
  //
  ldloc      staticMethodU
  ldloc      staticMethodT
  callvirt   instance bool [mscorlib]System.Object::Equals(object)
  brfalse    MethodOverloadsFromSharedContextOK
  ldc.i4     5
  ret
MethodOverloadsFromSharedContextOK:

  //
  // if (staticMethodT.DeclaringType != typeof(GenericHolder<string, string>))
  //     return 6;
  //
  ldloc      staticMethodT
  callvirt   instance class [mscorlib]System.Type [mscorlib]System.Reflection.MethodBase::get_DeclaringType()
  ldtoken    class GenericHolder`2<string,string>
  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(
                 valuetype [mscorlib]System.RuntimeTypeHandle)
  ceq
  brtrue     MethodGenericOwningTypeOK
  ldc.i4     6
  ret
MethodGenericOwningTypeOK:

  //
  // FieldInfo staticFieldT = GenericHolder<string, string>.GetInfoOfFieldT();
  //
  call        class [mscorlib]System.Reflection.FieldInfo class GenericHolder`2<string, string>::GetInfoOfFieldT()
  stloc       staticFieldT

  //
  // if (staticFieldT.DeclaringType != typeof(GenericHolder<string, string>))
  //     return 7;
  //
  ldloc      staticFieldT
  callvirt   instance class [mscorlib]System.Type [mscorlib]System.Reflection.FieldInfo::get_DeclaringType()
  ldtoken    class GenericHolder`2<string,string>
  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(
                 valuetype [mscorlib]System.RuntimeTypeHandle)
  ceq
  brtrue     FieldGenericOwningTypeOK
  ldc.i4     7
  ret
FieldGenericOwningTypeOK:

  //
  // if (!infoof(NonGenericHolder.GenericMethod<>).IsGenericMethodDefinition)
  //     return 8;
  //
  ldtoken    method void NonGenericHolder::GenericMethod<[1]>()
  call       class [mscorlib]System.Reflection.MethodBase 
                 [mscorlib]System.Reflection.MethodBase::GetMethodFromHandle(
                     valuetype [mscorlib]System.RuntimeMethodHandle)
  callvirt   instance bool [mscorlib]System.Reflection.MethodBase::get_IsGenericMethodDefinition()
  brtrue     StaticGenericMethodIsDefinition
  ldc.i4     8
  ret
StaticGenericMethodIsDefinition:

  //
  // if (infoof(NonGenericHolder.GenericMethod<int>).IsGenericMethodDefinition)
  //     return 9;
  //
  ldtoken    method void NonGenericHolder::GenericMethod<int32>()
  call       class [mscorlib]System.Reflection.MethodBase 
                 [mscorlib]System.Reflection.MethodBase::GetMethodFromHandle(
                     valuetype [mscorlib]System.RuntimeMethodHandle)
  callvirt   instance bool [mscorlib]System.Reflection.MethodBase::get_IsGenericMethodDefinition()
  brfalse    StaticGenericMethodIsInstantiated
  ldc.i4     9
  ret
StaticGenericMethodIsInstantiated:

  //
  // if (!infoof(GenericHolder<,>.GenericMethod<>).IsGenericMethodDefinition)
  //     return 10;
  //
  ldtoken    method void GenericHolder`2::GenericMethod<[1]>(!1, !!0)
  ldtoken    GenericHolder`2
  call       class [mscorlib]System.Reflection.MethodBase 
               [mscorlib]System.Reflection.MethodBase::GetMethodFromHandle(
                   valuetype [mscorlib]System.RuntimeMethodHandle,
                   valuetype [mscorlib]System.RuntimeTypeHandle)
  callvirt   instance bool [mscorlib]System.Reflection.MethodBase::get_IsGenericMethodDefinition()
  brtrue     StaticGenericMethodOnGenericTypeIsDefinition
  ldc.i4     10
  ret
StaticGenericMethodOnGenericTypeIsDefinition:

  //
  // MethodBase weirdoGenericMethod = infoof(GenericHolder<,>.GenericMethod<int>);
  // if (weirdoGenericMethod.IsGenericMethodDefinition)
  //     return 11;
  //
  ldtoken    method void GenericHolder`2::GenericMethod<int32>(!1, !!0)
  ldtoken    GenericHolder`2
  call       class [mscorlib]System.Reflection.MethodBase 
               [mscorlib]System.Reflection.MethodBase::GetMethodFromHandle(
                   valuetype [mscorlib]System.RuntimeMethodHandle,
                   valuetype [mscorlib]System.RuntimeTypeHandle)
  stloc      weirdoGenericMethod
  ldloc      weirdoGenericMethod
  callvirt   instance bool [mscorlib]System.Reflection.MethodBase::get_IsGenericMethodDefinition()
  brfalse    WeirdoGenericMethodIsNotDefinition
  ldc.i4     11
  ret
WeirdoGenericMethodIsNotDefinition:

  //
  // if (weirdoGenericMethod.DeclaringType != typeof(GenericHolder<,>))
  //     return 12;
  //
  ldloc      weirdoGenericMethod
  callvirt   instance class [mscorlib]System.Type [mscorlib]System.Reflection.MethodBase::get_DeclaringType()
  ldtoken    GenericHolder`2
  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(
                 valuetype [mscorlib]System.RuntimeTypeHandle)
  ceq
  brtrue     WeirdoGenericMethodOwningTypeOK
  ldc.i4     12
  ret
WeirdoGenericMethodOwningTypeOK:

  //
  // MethodBase otherWeirdoGenericMethod = infoof(GenericHolder<int,int>.GenericMethod<>);
  // if (!otherWeirdoGenericMethod.IsGenericMethodDefinition)
  //     return 13;
  //
  ldtoken    method void class GenericHolder`2<int32,int32>::GenericMethod<[1]>(!1, !!0)
  ldtoken    class GenericHolder`2<int32,int32>
  call       class [mscorlib]System.Reflection.MethodBase 
               [mscorlib]System.Reflection.MethodBase::GetMethodFromHandle(
                   valuetype [mscorlib]System.RuntimeMethodHandle,
                   valuetype [mscorlib]System.RuntimeTypeHandle)
  stloc      otherWeirdoGenericMethod
  ldloc      otherWeirdoGenericMethod
  callvirt   instance bool [mscorlib]System.Reflection.MethodBase::get_IsGenericMethodDefinition()
  brtrue     OtherWeirdoGenericMethodIsDefinition
  ldc.i4     13
  ret
OtherWeirdoGenericMethodIsDefinition:

  //
  // if (otherWeirdoGenericMethod.DeclaringType != typeof(GenericHolder<int, int>))
  //     return 14;
  //
  ldloc      otherWeirdoGenericMethod
  callvirt   instance class [mscorlib]System.Type [mscorlib]System.Reflection.MethodBase::get_DeclaringType()
  ldtoken    class GenericHolder`2<int32,int32>
  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(
                 valuetype [mscorlib]System.RuntimeTypeHandle)
  ceq
  brtrue     OtherWeirdoGenericMethodOwningTypeOK
  ldc.i4     14
  ret
OtherWeirdoGenericMethodOwningTypeOK:

  ldc.i4     100
  ret
}
}

.class private auto ansi beforefieldinit NonGenericHolder
       extends [mscorlib]System.Object
{
  .field public static object StaticField

  .method public hidebysig static int32 StaticMethod()
  {
    ldc.i4.0
    ret
  }

  .method public hidebysig static int16 StaticMethod()
  {
    ldc.i4.0
    ret
  }

  .method public hidebysig static void GenericMethod<T>()
  {
    ret
  }
}

.class private auto ansi beforefieldinit GenericHolder`2<T,U>
       extends [mscorlib]System.Object
{
  .field public static !0 StaticFieldT;

  .method public hidebysig static void StaticMethod(!0 x)
  {
    ret
  }

  .method public hidebysig static void StaticMethod(!1 x)
  {
    ret
  }

  .method public hidebysig static void GenericMethod<V>(!1 x, !!0 y)
  {
    ret
  }

  .method public hidebysig static class [mscorlib]System.Reflection.FieldInfo GetInfoOfFieldT()
  {
    ldtoken    field !0 class GenericHolder`2<!T,!U>::StaticFieldT
    ldtoken    class GenericHolder`2<!T,!U>
    call       class [mscorlib]System.Reflection.FieldInfo
                 [mscorlib]System.Reflection.FieldInfo::GetFieldFromHandle(
                     valuetype [mscorlib]System.RuntimeFieldHandle,
                     valuetype [mscorlib]System.RuntimeTypeHandle)
    ret
  }

  .method public hidebysig static class [mscorlib]System.Reflection.MethodBase GetInfoOfMethodT()
  {
    ldtoken    method void class GenericHolder`2<!T,!U>::StaticMethod(!0)
    ldtoken    class GenericHolder`2<!T,!U>
    call       class [mscorlib]System.Reflection.MethodBase 
                 [mscorlib]System.Reflection.MethodBase::GetMethodFromHandle(
                     valuetype [mscorlib]System.RuntimeMethodHandle,
                     valuetype [mscorlib]System.RuntimeTypeHandle)
    ret
  }

  .method public hidebysig static class [mscorlib]System.Reflection.MethodBase GetInfoOfMethodU()
  {
    ldtoken    method void class GenericHolder`2<!T,!U>::StaticMethod(!1)
    ldtoken    class GenericHolder`2<!T,!U>
    call       class [mscorlib]System.Reflection.MethodBase 
                 [mscorlib]System.Reflection.MethodBase::GetMethodFromHandle(
                     valuetype [mscorlib]System.RuntimeMethodHandle,
                     valuetype [mscorlib]System.RuntimeTypeHandle)
    ret
  }
}
