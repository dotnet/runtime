// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly provider { }

.assembly extern mscorlib { }

.class interface public IFoo<T>
{
  .field public static class [mscorlib]System.Type O

  .method public static specialname rtspecialname void .cctor()
  {
    ldtoken !T[]
    call class [mscorlib]System.Type class [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    stsfld class [mscorlib]System.Type class IFoo<!T>::O
    ret
  }

  .method public static class [mscorlib]System.Type GimmeT()
  {
    ldtoken !T
    call class [mscorlib]System.Type class [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    ret
  }

  .method public static class [mscorlib]System.Type GimmeU<U>()
  {
    ldtoken !!U
    call class [mscorlib]System.Type class [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    ret
  }
}
