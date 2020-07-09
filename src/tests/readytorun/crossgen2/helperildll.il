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
}
