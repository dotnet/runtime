// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly interfacestatics { }

.assembly extern System.Runtime { }

.class interface IFoo<T>
{
  .field public static class [System.Runtime]System.Type O

  .method public static specialname rtspecialname void .cctor()
  {
    ldtoken !T[]
    call class [System.Runtime]System.Type class [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
    stsfld class [System.Runtime]System.Type class IFoo<!T>::O
    ret
  }

  .method public static class [System.Runtime]System.Type GimmeT()
  {
    ldtoken !T[0...]
    call class [System.Runtime]System.Type class [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
    ret
  }

  .method public static class [System.Runtime]System.Type GimmeU<U>()
  {
    ldtoken !!U
    call class [System.Runtime]System.Type class [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
    ret
  }

  .method public static class [System.Runtime]System.Type GimmeU_T<U>()
  {
    call class [System.Runtime]System.Type class IFoo<!!U>::GimmeT()
    ret
  }
}

.method static hidebysig int32 Main()
{
  .entrypoint

  ldsfld class [System.Runtime]System.Type class IFoo<object>::O
  ldtoken object[]
  call class [System.Runtime]System.Type class [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)    
  ceq
  brtrue IFoo_object_O_Okay
  ldc.i4.1
  ret

IFoo_object_O_Okay:
  ldsfld class [System.Runtime]System.Type class IFoo<string>::O
  ldtoken string[]
  call class [System.Runtime]System.Type class [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)    
  ceq
  brtrue IFoo_string_O_Okay
  ldc.i4.2
  ret

IFoo_string_O_Okay:
  call class [System.Runtime]System.Type class IFoo<object>::GimmeT()
  ldtoken object[0...]
  call class [System.Runtime]System.Type class [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)    
  ceq
  brtrue IFoo_object_Gimme_T_Okay
  ldc.i4.3
  ret

IFoo_object_Gimme_T_Okay:
  call class [System.Runtime]System.Type class IFoo<object>::GimmeU<string>()
  ldtoken string
  call class [System.Runtime]System.Type class [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)    
  ceq
  brtrue IFoo_object_Gimme_U_Okay
  ldc.i4.4
  ret

IFoo_object_Gimme_U_Okay:
  call class [System.Runtime]System.Type class IFoo<object>::GimmeU_T<string>()
  ldtoken string[0...]
  call class [System.Runtime]System.Type class [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)    
  ceq
  brtrue IFoo_object_Gimme_U_T_Okay
  ldc.i4.5
  ret

IFoo_object_Gimme_U_T_Okay:

  ldc.i4 100
  ret
}