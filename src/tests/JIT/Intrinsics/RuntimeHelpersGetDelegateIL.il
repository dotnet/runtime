// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }
.assembly extern RuntimeHelpersGetDelegateCases { }

.assembly RuntimeHelpersGetDelegateIL { }

.class public abstract auto ansi sealed beforefieldinit NonGenericIl
       extends [System.Runtime]System.Object
{
  .custom instance void [System.Runtime]System.Runtime.CompilerServices.NullableContextAttribute::.ctor(uint8) = ( 01 00 01 00 00 )
  .custom instance void [System.Runtime]System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = ( 01 00 00 00 00 )
  .method public hidebysig static class [System.Runtime]System.Action
          GetInstanceBasicClosed() cil managed noinlining
  {
    .maxstack  2
    ldftn      instance void [RuntimeHelpersGetDelegateCases]NonGeneric::InstanceBasic()
    ldsflda    class [System.Runtime]System.Action [RuntimeHelpersGetDelegateCases]NonGeneric::InstanceBasicClosed
    call       !!0 [System.Runtime]System.Runtime.CompilerServices.RuntimeHelpers::GetDelegate<class [System.Runtime]System.Action>(native int, !!0&)
    ret
  } // end of method NonGenericIl::GetInstanceBasicClosed

  .method public hidebysig static class [System.Runtime]System.Action`1<class [RuntimeHelpersGetDelegateCases]NonGeneric>
          GetInstanceBasicOpen() cil managed noinlining
  {
    .maxstack  2
    ldftn      instance void [RuntimeHelpersGetDelegateCases]NonGeneric::InstanceBasic()
    ldsflda    class [System.Runtime]System.Action`1<class [RuntimeHelpersGetDelegateCases]NonGeneric> [RuntimeHelpersGetDelegateCases]NonGeneric::InstanceBasicOpen
    call       !!0 [System.Runtime]System.Runtime.CompilerServices.RuntimeHelpers::GetDelegate<class [System.Runtime]System.Action`1<class [RuntimeHelpersGetDelegateCases]NonGeneric>>(native int, !!0&)
    ret
  } // end of method NonGenericIl::GetInstanceBasicOpen

  .method public hidebysig static class [System.Runtime]System.Func`1<int32>
          GetInstanceReturnClosed() cil managed noinlining
  {
    .maxstack  2
    ldftn      instance int32 [RuntimeHelpersGetDelegateCases]NonGeneric::InstanceReturn()
    ldsflda    class [System.Runtime]System.Func`1<int32> [RuntimeHelpersGetDelegateCases]NonGeneric::InstanceReturnClosed
    call       !!0 [System.Runtime]System.Runtime.CompilerServices.RuntimeHelpers::GetDelegate<class [System.Runtime]System.Func`1<int32>>(native int, !!0&)
    ret
  } // end of method NonGenericIl::GetInstanceReturnClosed

  .method public hidebysig static class [System.Runtime]System.Func`2<class [RuntimeHelpersGetDelegateCases]NonGeneric,int32>
          GetInstanceReturnOpen() cil managed noinlining
  {
    .maxstack  2
    ldftn      instance int32 [RuntimeHelpersGetDelegateCases]NonGeneric::InstanceReturn()
    ldsflda    class [System.Runtime]System.Func`2<class [RuntimeHelpersGetDelegateCases]NonGeneric,int32> [RuntimeHelpersGetDelegateCases]NonGeneric::InstanceReturnOpen
    call       !!0 [System.Runtime]System.Runtime.CompilerServices.RuntimeHelpers::GetDelegate<class [System.Runtime]System.Func`2<class [RuntimeHelpersGetDelegateCases]NonGeneric,int32>>(native int, !!0&)
    ret
  } // end of method NonGenericIl::GetInstanceReturnOpen

  .method public hidebysig static class [System.Runtime]System.Func`2<object,object>
          GetInstanceParamClosed() cil managed noinlining
  {
    .maxstack  2
    ldftn      instance object [RuntimeHelpersGetDelegateCases]NonGeneric::InstanceParam(object)
    ldsflda    class [System.Runtime]System.Func`2<object,object> [RuntimeHelpersGetDelegateCases]NonGeneric::InstanceParamClosed
    call       !!0 [System.Runtime]System.Runtime.CompilerServices.RuntimeHelpers::GetDelegate<class [System.Runtime]System.Func`2<object,object>>(native int, !!0&)
    ret
  } // end of method NonGenericIl::GetInstanceParamClosed

  .method public hidebysig static class [System.Runtime]System.Func`3<class [RuntimeHelpersGetDelegateCases]NonGeneric,object,object>
          GetInstanceParamOpen() cil managed noinlining
  {
    .maxstack  2
    ldftn      instance object [RuntimeHelpersGetDelegateCases]NonGeneric::InstanceParam(object)
    ldsflda    class [System.Runtime]System.Func`3<class [RuntimeHelpersGetDelegateCases]NonGeneric,object,object> [RuntimeHelpersGetDelegateCases]NonGeneric::InstanceParamOpen
    call       !!0 [System.Runtime]System.Runtime.CompilerServices.RuntimeHelpers::GetDelegate<class [System.Runtime]System.Func`3<class [RuntimeHelpersGetDelegateCases]NonGeneric,object,object>>(native int, !!0&)
    ret
  } // end of method NonGenericIl::GetInstanceParamOpen

  .method public hidebysig static class [System.Runtime]System.Func`1<object>
          GetInstanceVirtualClosed() cil managed noinlining
  {
    .maxstack  2
    newobj     instance void [RuntimeHelpersGetDelegateCases]NonGeneric::.ctor()
    ldvirtftn  instance object [RuntimeHelpersGetDelegateCases]NonGeneric::Virtual()
    ldsflda    class [System.Runtime]System.Func`1<object> [RuntimeHelpersGetDelegateCases]NonGeneric::VirtualClosed
    call       !!0 [System.Runtime]System.Runtime.CompilerServices.RuntimeHelpers::GetDelegate<class [System.Runtime]System.Func`1<object>>(native int, !!0&)
    ret
  } // end of method NonGenericIl::GetInstanceVirtualClosed

  .method public hidebysig static class [System.Runtime]System.Func`2<class [RuntimeHelpersGetDelegateCases]NonGeneric,object>
          GetInstanceVirtualOpen() cil managed noinlining
  {
    .maxstack  2
    newobj     instance void [RuntimeHelpersGetDelegateCases]NonGeneric::.ctor()
    ldvirtftn  instance object [RuntimeHelpersGetDelegateCases]NonGeneric::Virtual()
    ldsflda    class [System.Runtime]System.Func`2<class [RuntimeHelpersGetDelegateCases]NonGeneric,object> [RuntimeHelpersGetDelegateCases]NonGeneric::VirtualOpen
    call       !!0 [System.Runtime]System.Runtime.CompilerServices.RuntimeHelpers::GetDelegate<class [System.Runtime]System.Func`2<class [RuntimeHelpersGetDelegateCases]NonGeneric,object>>(native int, !!0&)
    ret
  } // end of method NonGenericIl::GetInstanceVirtualOpen

  .method public hidebysig static class [System.Runtime]System.Func`1<class [System.Runtime]System.Type>
          GetInstanceGenericClosed<T>() cil managed noinlining
  {
    .param type T
      .custom instance void [System.Runtime]System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = ( 01 00 02 00 00 )
    .maxstack  2
    ldftn      instance class [System.Runtime]System.Type [RuntimeHelpersGetDelegateCases]NonGeneric::InstanceGeneric<!!0>()
    ldsflda    class [System.Runtime]System.Func`1<class [System.Runtime]System.Type> class [RuntimeHelpersGetDelegateCases]NonGeneric/Cache`1<!!T>::InstanceGenericClosed
    call       !!0 [System.Runtime]System.Runtime.CompilerServices.RuntimeHelpers::GetDelegate<class [System.Runtime]System.Func`1<class [System.Runtime]System.Type>>(native int, !!0&)
    ret
  } // end of method NonGenericIl::GetInstanceGenericClosed

  .method public hidebysig static class [System.Runtime]System.Func`2<class [RuntimeHelpersGetDelegateCases]NonGeneric,class [System.Runtime]System.Type>
          GetInstanceGenericOpen<T>() cil managed noinlining
  {
    .param type T
      .custom instance void [System.Runtime]System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = ( 01 00 02 00 00 )
    .maxstack  2
    ldftn      instance class [System.Runtime]System.Type [RuntimeHelpersGetDelegateCases]NonGeneric::InstanceGeneric<!!0>()
    ldsflda    class [System.Runtime]System.Func`2<class [RuntimeHelpersGetDelegateCases]NonGeneric,class [System.Runtime]System.Type> class [RuntimeHelpersGetDelegateCases]NonGeneric/Cache`1<!!T>::InstanceGenericOpen
    call       !!0 [System.Runtime]System.Runtime.CompilerServices.RuntimeHelpers::GetDelegate<class [System.Runtime]System.Func`2<class [RuntimeHelpersGetDelegateCases]NonGeneric,class [System.Runtime]System.Type>>(native int, !!0&)
    ret
  } // end of method NonGenericIl::GetInstanceGenericOpen

  .method public hidebysig static class [System.Runtime]System.Func`2<!!T,class [System.Runtime]System.Type>
          GetInstanceGenericParamClosed<T>() cil managed noinlining
  {
    .param type T
      .custom instance void [System.Runtime]System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = ( 01 00 02 00 00 )
    .maxstack  2
    ldftn      instance class [System.Runtime]System.Type [RuntimeHelpersGetDelegateCases]NonGeneric::InstanceGenericParam<!!0>(!!0)
    ldsflda    class [System.Runtime]System.Func`2<!0,class [System.Runtime]System.Type> class [RuntimeHelpersGetDelegateCases]NonGeneric/Cache`1<!!T>::InstanceGenericParamClosed
    call       !!0 [System.Runtime]System.Runtime.CompilerServices.RuntimeHelpers::GetDelegate<class [System.Runtime]System.Func`2<!!0,class [System.Runtime]System.Type>>(native int, !!0&)
    ret
  } // end of method NonGenericIl::GetInstanceGenericParamClosed

  .method public hidebysig static class [System.Runtime]System.Func`3<class [RuntimeHelpersGetDelegateCases]NonGeneric,!!T,class [System.Runtime]System.Type>
          GetInstanceGenericParamOpen<T>() cil managed noinlining
  {
    .param type T
      .custom instance void [System.Runtime]System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = ( 01 00 02 00 00 )
    .maxstack  2
    ldftn      instance class [System.Runtime]System.Type [RuntimeHelpersGetDelegateCases]NonGeneric::InstanceGenericParam<!!0>(!!0)
    ldsflda    class [System.Runtime]System.Func`3<class [RuntimeHelpersGetDelegateCases]NonGeneric,!0,class [System.Runtime]System.Type> class [RuntimeHelpersGetDelegateCases]NonGeneric/Cache`1<!!T>::InstanceGenericParamOpen
    call       !!0 [System.Runtime]System.Runtime.CompilerServices.RuntimeHelpers::GetDelegate<class [System.Runtime]System.Func`3<class [RuntimeHelpersGetDelegateCases]NonGeneric,!!0,class [System.Runtime]System.Type>>(native int, !!0&)
    ret
  } // end of method NonGenericIl::GetInstanceGenericParamOpen

} // end of class NonGenericIl

.class public abstract auto ansi sealed beforefieldinit GenericIl`1<T>
       extends [System.Runtime]System.Object
{
  .custom instance void [System.Runtime]System.Runtime.CompilerServices.NullableContextAttribute::.ctor(uint8) = ( 01 00 01 00 00 )
  .custom instance void [System.Runtime]System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = ( 01 00 00 00 00 )
  .param type T
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = ( 01 00 02 00 00 )
  .method public hidebysig static class [System.Runtime]System.Action
          GetInstanceBasicClosed() cil managed noinlining
  {
    .maxstack  2
    ldftn      instance void class [RuntimeHelpersGetDelegateCases]Generic`1<!T>::InstanceBasic()
    ldsflda    class [System.Runtime]System.Action class [RuntimeHelpersGetDelegateCases]Generic`1<!T>::InstanceBasicClosed
    call       !!0 [System.Runtime]System.Runtime.CompilerServices.RuntimeHelpers::GetDelegate<class [System.Runtime]System.Action>(native int, !!0&)
    ret
  } // end of method GenericIl`1::GetInstanceBasicClosed

  .method public hidebysig static class [System.Runtime]System.Action`1<class [RuntimeHelpersGetDelegateCases]Generic`1<!T>>
          GetInstanceBasicOpen() cil managed noinlining
  {
    .maxstack  2
    ldftn      instance void class [RuntimeHelpersGetDelegateCases]Generic`1<!T>::InstanceBasic()
    ldsflda    class [System.Runtime]System.Action`1<class [RuntimeHelpersGetDelegateCases]Generic`1<!0>> class [RuntimeHelpersGetDelegateCases]Generic`1<!T>::InstanceBasicOpen
    call       !!0 [System.Runtime]System.Runtime.CompilerServices.RuntimeHelpers::GetDelegate<class [System.Runtime]System.Action`1<class [RuntimeHelpersGetDelegateCases]Generic`1<!T>>>(native int, !!0&)
    ret
  } // end of method GenericIl`1::GetInstanceBasicOpen

  .method public hidebysig static class [System.Runtime]System.Func`1<int32>
          GetInstanceReturnClosed() cil managed noinlining
  {
    .maxstack  2
    ldftn      instance int32 class [RuntimeHelpersGetDelegateCases]Generic`1<!T>::InstanceReturn()
    ldsflda    class [System.Runtime]System.Func`1<int32> class [RuntimeHelpersGetDelegateCases]Generic`1<!T>::InstanceReturnClosed
    call       !!0 [System.Runtime]System.Runtime.CompilerServices.RuntimeHelpers::GetDelegate<class [System.Runtime]System.Func`1<int32>>(native int, !!0&)
    ret
  } // end of method GenericIl`1::GetInstanceReturnClosed

  .method public hidebysig static class [System.Runtime]System.Func`2<class [RuntimeHelpersGetDelegateCases]Generic`1<!T>,int32>
          GetInstanceReturnOpen() cil managed noinlining
  {
    .maxstack  2
    ldftn      instance int32 class [RuntimeHelpersGetDelegateCases]Generic`1<!T>::InstanceReturn()
    ldsflda    class [System.Runtime]System.Func`2<class [RuntimeHelpersGetDelegateCases]Generic`1<!0>,int32> class [RuntimeHelpersGetDelegateCases]Generic`1<!T>::InstanceReturnOpen
    call       !!0 [System.Runtime]System.Runtime.CompilerServices.RuntimeHelpers::GetDelegate<class [System.Runtime]System.Func`2<class [RuntimeHelpersGetDelegateCases]Generic`1<!T>,int32>>(native int, !!0&)
    ret
  } // end of method GenericIl`1::GetInstanceReturnOpen

  .method public hidebysig static class [System.Runtime]System.Func`2<object,object>
          GetInstanceParamClosed() cil managed noinlining
  {
    .maxstack  2
    ldftn      instance object class [RuntimeHelpersGetDelegateCases]Generic`1<!T>::InstanceParam(object)
    ldsflda    class [System.Runtime]System.Func`2<object,object> class [RuntimeHelpersGetDelegateCases]Generic`1<!T>::InstanceParamClosed
    call       !!0 [System.Runtime]System.Runtime.CompilerServices.RuntimeHelpers::GetDelegate<class [System.Runtime]System.Func`2<object,object>>(native int, !!0&)
    ret
  } // end of method GenericIl`1::GetInstanceParamClosed

  .method public hidebysig static class [System.Runtime]System.Func`3<class [RuntimeHelpersGetDelegateCases]Generic`1<!T>,object,object>
          GetInstanceParamOpen() cil managed noinlining
  {
    .maxstack  2
    ldftn      instance object class [RuntimeHelpersGetDelegateCases]Generic`1<!T>::InstanceParam(object)
    ldsflda    class [System.Runtime]System.Func`3<class [RuntimeHelpersGetDelegateCases]Generic`1<!0>,object,object> class [RuntimeHelpersGetDelegateCases]Generic`1<!T>::InstanceParamOpen
    call       !!0 [System.Runtime]System.Runtime.CompilerServices.RuntimeHelpers::GetDelegate<class [System.Runtime]System.Func`3<class [RuntimeHelpersGetDelegateCases]Generic`1<!T>,object,object>>(native int, !!0&)
    ret
  } // end of method GenericIl`1::GetInstanceParamOpen

  .method public hidebysig static class [System.Runtime]System.Func`1<object>
          GetInstanceVirtualClosed() cil managed noinlining
  {
    .maxstack  2
    newobj     instance void class [RuntimeHelpersGetDelegateCases]Generic`1<!T>::.ctor()
    ldvirtftn  instance object class [RuntimeHelpersGetDelegateCases]Generic`1<!T>::Virtual()
    ldsflda    class [System.Runtime]System.Func`1<object> class [RuntimeHelpersGetDelegateCases]Generic`1<!T>::VirtualClosed
    call       !!0 [System.Runtime]System.Runtime.CompilerServices.RuntimeHelpers::GetDelegate<class [System.Runtime]System.Func`1<object>>(native int, !!0&)
    ret
  } // end of method GenericIl`1::GetInstanceVirtualClosed

  .method public hidebysig static class [System.Runtime]System.Func`2<class [RuntimeHelpersGetDelegateCases]Generic`1<!T>,object>
          GetInstanceVirtualOpen() cil managed noinlining
  {
    .maxstack  2
    newobj     instance void class [RuntimeHelpersGetDelegateCases]Generic`1<!T>::.ctor()
    ldvirtftn  instance object class [RuntimeHelpersGetDelegateCases]Generic`1<!T>::Virtual()
    ldsflda    class [System.Runtime]System.Func`2<class [RuntimeHelpersGetDelegateCases]Generic`1<!0>,object> class [RuntimeHelpersGetDelegateCases]Generic`1<!T>::VirtualOpen
    call       !!0 [System.Runtime]System.Runtime.CompilerServices.RuntimeHelpers::GetDelegate<class [System.Runtime]System.Func`2<class [RuntimeHelpersGetDelegateCases]Generic`1<!T>,object>>(native int, !!0&)
    ret
  } // end of method GenericIl`1::GetInstanceVirtualOpen

  .method public hidebysig static class [System.Runtime]System.Func`1<class [System.Runtime]System.Type>
          GetInstanceGenericClosed() cil managed noinlining
  {
    .maxstack  2
    ldftn      instance class [System.Runtime]System.Type class [RuntimeHelpersGetDelegateCases]Generic`1<!T>::InstanceGeneric()
    ldsflda    class [System.Runtime]System.Func`1<class [System.Runtime]System.Type> class [RuntimeHelpersGetDelegateCases]Generic`1<!T>::InstanceGenericClosed
    call       !!0 [System.Runtime]System.Runtime.CompilerServices.RuntimeHelpers::GetDelegate<class [System.Runtime]System.Func`1<class [System.Runtime]System.Type>>(native int, !!0&)
    ret
  } // end of method GenericIl`1::GetInstanceGenericClosed

  .method public hidebysig static class [System.Runtime]System.Func`2<class [RuntimeHelpersGetDelegateCases]Generic`1<!T>,class [System.Runtime]System.Type>
          GetInstanceGenericOpen() cil managed noinlining
  {
    .maxstack  2
    ldftn      instance class [System.Runtime]System.Type class [RuntimeHelpersGetDelegateCases]Generic`1<!T>::InstanceGeneric()
    ldsflda    class [System.Runtime]System.Func`2<class [RuntimeHelpersGetDelegateCases]Generic`1<!0>,class [System.Runtime]System.Type> class [RuntimeHelpersGetDelegateCases]Generic`1<!T>::InstanceGenericOpen
    call       !!0 [System.Runtime]System.Runtime.CompilerServices.RuntimeHelpers::GetDelegate<class [System.Runtime]System.Func`2<class [RuntimeHelpersGetDelegateCases]Generic`1<!T>,class [System.Runtime]System.Type>>(native int, !!0&)
    ret
  } // end of method GenericIl`1::GetInstanceGenericOpen

  .method public hidebysig static class [System.Runtime]System.Func`2<!T,class [System.Runtime]System.Type>
          GetInstanceGenericParamClosed() cil managed noinlining
  {
    .maxstack  2
    ldftn      instance class [System.Runtime]System.Type class [RuntimeHelpersGetDelegateCases]Generic`1<!T>::InstanceGenericParam(!0)
    ldsflda    class [System.Runtime]System.Func`2<!0,class [System.Runtime]System.Type> class [RuntimeHelpersGetDelegateCases]Generic`1<!T>::InstanceGenericParamClosed
    call       !!0 [System.Runtime]System.Runtime.CompilerServices.RuntimeHelpers::GetDelegate<class [System.Runtime]System.Func`2<!T,class [System.Runtime]System.Type>>(native int, !!0&)
    ret
  } // end of method GenericIl`1::GetInstanceGenericParamClosed

  .method public hidebysig static class [System.Runtime]System.Func`3<class [RuntimeHelpersGetDelegateCases]Generic`1<!T>,!T,class [System.Runtime]System.Type>
          GetInstanceGenericParamOpen() cil managed noinlining
  {
    .maxstack  2
    ldftn      instance class [System.Runtime]System.Type class [RuntimeHelpersGetDelegateCases]Generic`1<!T>::InstanceGenericParam(!0)
    ldsflda    class [System.Runtime]System.Func`3<class [RuntimeHelpersGetDelegateCases]Generic`1<!0>,!0,class [System.Runtime]System.Type> class [RuntimeHelpersGetDelegateCases]Generic`1<!T>::InstanceGenericParamOpen
    call       !!0 [System.Runtime]System.Runtime.CompilerServices.RuntimeHelpers::GetDelegate<class [System.Runtime]System.Func`3<class [RuntimeHelpersGetDelegateCases]Generic`1<!T>,!T,class [System.Runtime]System.Type>>(native int, !!0&)
    ret
  } // end of method GenericIl`1::GetInstanceGenericParamOpen

  .method public hidebysig static class [System.Runtime]System.Func`1<valuetype [System.Runtime]System.ValueTuple`2<class [System.Runtime]System.Type,class [System.Runtime]System.Type>>
          GetInstanceDoubleGenericClosed<T2>() cil managed noinlining
  {
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.NullableContextAttribute::.ctor(uint8) = ( 01 00 02 00 00 )
    .param [0]
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8[]) = ( 01 00 04 00 00 00 01 00 01 01 00 00 )
    .maxstack  2
    ldftn      instance valuetype [System.Runtime]System.ValueTuple`2<class [System.Runtime]System.Type,class [System.Runtime]System.Type> class [RuntimeHelpersGetDelegateCases]Generic`1<!T>::InstanceDoubleGeneric<!!0>()
    ldsflda    class [System.Runtime]System.Func`1<valuetype [System.Runtime]System.ValueTuple`2<class [System.Runtime]System.Type,class [System.Runtime]System.Type>> class [RuntimeHelpersGetDelegateCases]Generic`1/Cache`1<!T,!!T2>::InstanceDoubleGenericClosed
    call       !!0 [System.Runtime]System.Runtime.CompilerServices.RuntimeHelpers::GetDelegate<class [System.Runtime]System.Func`1<valuetype [System.Runtime]System.ValueTuple`2<class [System.Runtime]System.Type,class [System.Runtime]System.Type>>>(native int, !!0&)
    ret
  } // end of method GenericIl`1::GetInstanceDoubleGenericClosed

  .method public hidebysig static class [System.Runtime]System.Func`2<class [RuntimeHelpersGetDelegateCases]Generic`1<!T>,valuetype [System.Runtime]System.ValueTuple`2<class [System.Runtime]System.Type,class [System.Runtime]System.Type>>
          GetInstanceDoubleGenericOpen<T2>() cil managed noinlining
  {
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.NullableContextAttribute::.ctor(uint8) = ( 01 00 02 00 00 )
    .param [0]
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8[]) = ( 01 00 06 00 00 00 01 01 01 00 01 01 00 00 )
    .maxstack  2
    ldftn      instance valuetype [System.Runtime]System.ValueTuple`2<class [System.Runtime]System.Type,class [System.Runtime]System.Type> class [RuntimeHelpersGetDelegateCases]Generic`1<!T>::InstanceDoubleGeneric<!!0>()
    ldsflda    class [System.Runtime]System.Func`2<class [RuntimeHelpersGetDelegateCases]Generic`1<!0>,valuetype [System.Runtime]System.ValueTuple`2<class [System.Runtime]System.Type,class [System.Runtime]System.Type>> class [RuntimeHelpersGetDelegateCases]Generic`1/Cache`1<!T,!!T2>::InstanceDoubleGenericOpen
    call       !!0 [System.Runtime]System.Runtime.CompilerServices.RuntimeHelpers::GetDelegate<class [System.Runtime]System.Func`2<class [RuntimeHelpersGetDelegateCases]Generic`1<!T>,valuetype [System.Runtime]System.ValueTuple`2<class [System.Runtime]System.Type,class [System.Runtime]System.Type>>>(native int, !!0&)
    ret
  } // end of method GenericIl`1::GetInstanceDoubleGenericOpen

} // end of class GenericIl`1

.class public abstract auto ansi sealed beforefieldinit NonGenericStructIl
       extends [System.Runtime]System.Object
{
  .custom instance void [System.Runtime]System.Runtime.CompilerServices.NullableContextAttribute::.ctor(uint8) = ( 01 00 01 00 00 )
  .custom instance void [System.Runtime]System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = ( 01 00 00 00 00 )
  .method public hidebysig static class [System.Runtime]System.Action
          GetInstanceBasicClosed() cil managed noinlining
  {
    .maxstack  2
    ldftn      instance void [RuntimeHelpersGetDelegateCases]NonGenericStruct::InstanceBasic()
    ldsflda    class [System.Runtime]System.Action [RuntimeHelpersGetDelegateCases]NonGenericStruct::InstanceBasicClosed
    call       !!0 [System.Runtime]System.Runtime.CompilerServices.RuntimeHelpers::GetDelegate<class [System.Runtime]System.Action>(native int, !!0&)
    ret
  } // end of method NonGenericStructIl::GetInstanceBasicClosed

  .method public hidebysig static class [System.Runtime]System.Action`1<valuetype [RuntimeHelpersGetDelegateCases]NonGenericStruct>
          GetInstanceBasicOpen() cil managed noinlining
  {
    .maxstack  2
    ldftn      instance void [RuntimeHelpersGetDelegateCases]NonGenericStruct::InstanceBasic()
    ldsflda    class [System.Runtime]System.Action`1<valuetype [RuntimeHelpersGetDelegateCases]NonGenericStruct> [RuntimeHelpersGetDelegateCases]NonGenericStruct::InstanceBasicOpen
    call       !!0 [System.Runtime]System.Runtime.CompilerServices.RuntimeHelpers::GetDelegate<class [System.Runtime]System.Action`1<valuetype [RuntimeHelpersGetDelegateCases]NonGenericStruct>>(native int, !!0&)
    ret
  } // end of method NonGenericStructIl::GetInstanceBasicOpen

  .method public hidebysig static class [System.Runtime]System.Func`1<int32>
          GetInstanceReturnClosed() cil managed noinlining
  {
    .maxstack  2
    ldftn      instance int32 [RuntimeHelpersGetDelegateCases]NonGenericStruct::InstanceReturn()
    ldsflda    class [System.Runtime]System.Func`1<int32> [RuntimeHelpersGetDelegateCases]NonGenericStruct::InstanceReturnClosed
    call       !!0 [System.Runtime]System.Runtime.CompilerServices.RuntimeHelpers::GetDelegate<class [System.Runtime]System.Func`1<int32>>(native int, !!0&)
    ret
  } // end of method NonGenericStructIl::GetInstanceReturnClosed

  .method public hidebysig static class [System.Runtime]System.Func`2<valuetype [RuntimeHelpersGetDelegateCases]NonGenericStruct,int32>
          GetInstanceReturnOpen() cil managed noinlining
  {
    .maxstack  2
    ldftn      instance int32 [RuntimeHelpersGetDelegateCases]NonGenericStruct::InstanceReturn()
    ldsflda    class [System.Runtime]System.Func`2<valuetype [RuntimeHelpersGetDelegateCases]NonGenericStruct,int32> [RuntimeHelpersGetDelegateCases]NonGenericStruct::InstanceReturnOpen
    call       !!0 [System.Runtime]System.Runtime.CompilerServices.RuntimeHelpers::GetDelegate<class [System.Runtime]System.Func`2<valuetype [RuntimeHelpersGetDelegateCases]NonGenericStruct,int32>>(native int, !!0&)
    ret
  } // end of method NonGenericStructIl::GetInstanceReturnOpen

  .method public hidebysig static class [System.Runtime]System.Func`2<object,object>
          GetInstanceParamClosed() cil managed noinlining
  {
    .maxstack  2
    ldftn      instance object [RuntimeHelpersGetDelegateCases]NonGenericStruct::InstanceParam(object)
    ldsflda    class [System.Runtime]System.Func`2<object,object> [RuntimeHelpersGetDelegateCases]NonGenericStruct::InstanceParamClosed
    call       !!0 [System.Runtime]System.Runtime.CompilerServices.RuntimeHelpers::GetDelegate<class [System.Runtime]System.Func`2<object,object>>(native int, !!0&)
    ret
  } // end of method NonGenericStructIl::GetInstanceParamClosed

  .method public hidebysig static class [System.Runtime]System.Func`3<valuetype [RuntimeHelpersGetDelegateCases]NonGenericStruct,object,object>
          GetInstanceParamOpen() cil managed noinlining
  {
    .maxstack  2
    ldftn      instance object [RuntimeHelpersGetDelegateCases]NonGenericStruct::InstanceParam(object)
    ldsflda    class [System.Runtime]System.Func`3<valuetype [RuntimeHelpersGetDelegateCases]NonGenericStruct,object,object> [RuntimeHelpersGetDelegateCases]NonGenericStruct::InstanceParamOpen
    call       !!0 [System.Runtime]System.Runtime.CompilerServices.RuntimeHelpers::GetDelegate<class [System.Runtime]System.Func`3<valuetype [RuntimeHelpersGetDelegateCases]NonGenericStruct,object,object>>(native int, !!0&)
    ret
  } // end of method NonGenericStructIl::GetInstanceParamOpen

  .method public hidebysig static class [System.Runtime]System.Func`1<class [System.Runtime]System.Type>
          GetInstanceGenericClosed<T>() cil managed noinlining
  {
    .param type T
      .custom instance void [System.Runtime]System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = ( 01 00 02 00 00 )
    .maxstack  2
    ldftn      instance class [System.Runtime]System.Type [RuntimeHelpersGetDelegateCases]NonGenericStruct::InstanceGeneric<!!0>()
    ldsflda    class [System.Runtime]System.Func`1<class [System.Runtime]System.Type> class [RuntimeHelpersGetDelegateCases]NonGenericStruct/Cache`1<!!T>::InstanceGenericClosed
    call       !!0 [System.Runtime]System.Runtime.CompilerServices.RuntimeHelpers::GetDelegate<class [System.Runtime]System.Func`1<class [System.Runtime]System.Type>>(native int, !!0&)
    ret
  } // end of method NonGenericStructIl::GetInstanceGenericClosed

  .method public hidebysig static class [System.Runtime]System.Func`2<valuetype [RuntimeHelpersGetDelegateCases]NonGenericStruct,class [System.Runtime]System.Type>
          GetInstanceGenericOpen<T>() cil managed noinlining
  {
    .param type T
      .custom instance void [System.Runtime]System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = ( 01 00 02 00 00 )
    .maxstack  2
    ldftn      instance class [System.Runtime]System.Type [RuntimeHelpersGetDelegateCases]NonGenericStruct::InstanceGeneric<!!0>()
    ldsflda    class [System.Runtime]System.Func`2<valuetype [RuntimeHelpersGetDelegateCases]NonGenericStruct,class [System.Runtime]System.Type> class [RuntimeHelpersGetDelegateCases]NonGenericStruct/Cache`1<!!T>::InstanceGenericOpen
    call       !!0 [System.Runtime]System.Runtime.CompilerServices.RuntimeHelpers::GetDelegate<class [System.Runtime]System.Func`2<valuetype [RuntimeHelpersGetDelegateCases]NonGenericStruct,class [System.Runtime]System.Type>>(native int, !!0&)
    ret
  } // end of method NonGenericStructIl::GetInstanceGenericOpen

  .method public hidebysig static class [System.Runtime]System.Func`2<!!T,class [System.Runtime]System.Type>
          GetInstanceGenericParamClosed<T>() cil managed noinlining
  {
    .param type T
      .custom instance void [System.Runtime]System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = ( 01 00 02 00 00 )
    .maxstack  2
    ldftn      instance class [System.Runtime]System.Type [RuntimeHelpersGetDelegateCases]NonGenericStruct::InstanceGenericParam<!!0>(!!0)
    ldsflda    class [System.Runtime]System.Func`2<!0,class [System.Runtime]System.Type> class [RuntimeHelpersGetDelegateCases]NonGenericStruct/Cache`1<!!T>::InstanceGenericParamClosed
    call       !!0 [System.Runtime]System.Runtime.CompilerServices.RuntimeHelpers::GetDelegate<class [System.Runtime]System.Func`2<!!0,class [System.Runtime]System.Type>>(native int, !!0&)
    ret
  } // end of method NonGenericStructIl::GetInstanceGenericParamClosed

  .method public hidebysig static class [System.Runtime]System.Func`3<valuetype [RuntimeHelpersGetDelegateCases]NonGenericStruct,!!T,class [System.Runtime]System.Type>
          GetInstanceGenericParamOpen<T>() cil managed noinlining
  {
    .param type T
      .custom instance void [System.Runtime]System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = ( 01 00 02 00 00 )
    .maxstack  2
    ldftn      instance class [System.Runtime]System.Type [RuntimeHelpersGetDelegateCases]NonGenericStruct::InstanceGenericParam<!!0>(!!0)
    ldsflda    class [System.Runtime]System.Func`3<valuetype [RuntimeHelpersGetDelegateCases]NonGenericStruct,!0,class [System.Runtime]System.Type> class [RuntimeHelpersGetDelegateCases]NonGenericStruct/Cache`1<!!T>::InstanceGenericParamOpen
    call       !!0 [System.Runtime]System.Runtime.CompilerServices.RuntimeHelpers::GetDelegate<class [System.Runtime]System.Func`3<valuetype [RuntimeHelpersGetDelegateCases]NonGenericStruct,!!0,class [System.Runtime]System.Type>>(native int, !!0&)
    ret
  } // end of method NonGenericStructIl::GetInstanceGenericParamOpen

} // end of class NonGenericStructIl

.class public abstract auto ansi sealed beforefieldinit GenericStructIl`1<T>
       extends [System.Runtime]System.Object
{
  .custom instance void [System.Runtime]System.Runtime.CompilerServices.NullableContextAttribute::.ctor(uint8) = ( 01 00 01 00 00 )
  .custom instance void [System.Runtime]System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = ( 01 00 00 00 00 )
  .param type T
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = ( 01 00 02 00 00 )
  .method public hidebysig static class [System.Runtime]System.Action
          GetInstanceBasicClosed() cil managed noinlining
  {
    .maxstack  2
    ldftn      instance void valuetype [RuntimeHelpersGetDelegateCases]GenericStruct`1<!T>::InstanceBasic()
    ldsflda    class [System.Runtime]System.Action valuetype [RuntimeHelpersGetDelegateCases]GenericStruct`1<!T>::InstanceBasicClosed
    call       !!0 [System.Runtime]System.Runtime.CompilerServices.RuntimeHelpers::GetDelegate<class [System.Runtime]System.Action>(native int, !!0&)
    ret
  } // end of method GenericStructIl`1::GetInstanceBasicClosed

  .method public hidebysig static class [System.Runtime]System.Action`1<valuetype [RuntimeHelpersGetDelegateCases]GenericStruct`1<!T>>
          GetInstanceBasicOpen() cil managed noinlining
  {
    .param [0]
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8[]) = ( 01 00 03 00 00 00 01 00 01 00 00 )
    .maxstack  2
    ldftn      instance void valuetype [RuntimeHelpersGetDelegateCases]GenericStruct`1<!T>::InstanceBasic()
    ldsflda    class [System.Runtime]System.Action`1<valuetype [RuntimeHelpersGetDelegateCases]GenericStruct`1<!0>> valuetype [RuntimeHelpersGetDelegateCases]GenericStruct`1<!T>::InstanceBasicOpen
    call       !!0 [System.Runtime]System.Runtime.CompilerServices.RuntimeHelpers::GetDelegate<class [System.Runtime]System.Action`1<valuetype [RuntimeHelpersGetDelegateCases]GenericStruct`1<!T>>>(native int, !!0&)
    ret
  } // end of method GenericStructIl`1::GetInstanceBasicOpen

  .method public hidebysig static class [System.Runtime]System.Func`1<int32>
          GetInstanceReturnClosed() cil managed noinlining
  {
    .maxstack  2
    ldftn      instance int32 valuetype [RuntimeHelpersGetDelegateCases]GenericStruct`1<!T>::InstanceReturn()
    ldsflda    class [System.Runtime]System.Func`1<int32> valuetype [RuntimeHelpersGetDelegateCases]GenericStruct`1<!T>::InstanceReturnClosed
    call       !!0 [System.Runtime]System.Runtime.CompilerServices.RuntimeHelpers::GetDelegate<class [System.Runtime]System.Func`1<int32>>(native int, !!0&)
    ret
  } // end of method GenericStructIl`1::GetInstanceReturnClosed

  .method public hidebysig static class [System.Runtime]System.Func`2<valuetype [RuntimeHelpersGetDelegateCases]GenericStruct`1<!T>,int32>
          GetInstanceReturnOpen() cil managed noinlining
  {
    .param [0]
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8[]) = ( 01 00 03 00 00 00 01 00 01 00 00 )
    .maxstack  2
    ldftn      instance int32 valuetype [RuntimeHelpersGetDelegateCases]GenericStruct`1<!T>::InstanceReturn()
    ldsflda    class [System.Runtime]System.Func`2<valuetype [RuntimeHelpersGetDelegateCases]GenericStruct`1<!0>,int32> valuetype [RuntimeHelpersGetDelegateCases]GenericStruct`1<!T>::InstanceReturnOpen
    call       !!0 [System.Runtime]System.Runtime.CompilerServices.RuntimeHelpers::GetDelegate<class [System.Runtime]System.Func`2<valuetype [RuntimeHelpersGetDelegateCases]GenericStruct`1<!T>,int32>>(native int, !!0&)
    ret
  } // end of method GenericStructIl`1::GetInstanceReturnOpen

  .method public hidebysig static class [System.Runtime]System.Func`2<object,object>
          GetInstanceParamClosed() cil managed noinlining
  {
    .maxstack  2
    ldftn      instance object valuetype [RuntimeHelpersGetDelegateCases]GenericStruct`1<!T>::InstanceParam(object)
    ldsflda    class [System.Runtime]System.Func`2<object,object> valuetype [RuntimeHelpersGetDelegateCases]GenericStruct`1<!T>::InstanceParamClosed
    call       !!0 [System.Runtime]System.Runtime.CompilerServices.RuntimeHelpers::GetDelegate<class [System.Runtime]System.Func`2<object,object>>(native int, !!0&)
    ret
  } // end of method GenericStructIl`1::GetInstanceParamClosed

  .method public hidebysig static class [System.Runtime]System.Func`3<valuetype [RuntimeHelpersGetDelegateCases]GenericStruct`1<!T>,object,object>
          GetInstanceParamOpen() cil managed noinlining
  {
    .param [0]
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8[]) = ( 01 00 05 00 00 00 01 00 01 01 01 00 00 )
    .maxstack  2
    ldftn      instance object valuetype [RuntimeHelpersGetDelegateCases]GenericStruct`1<!T>::InstanceParam(object)
    ldsflda    class [System.Runtime]System.Func`3<valuetype [RuntimeHelpersGetDelegateCases]GenericStruct`1<!0>,object,object> valuetype [RuntimeHelpersGetDelegateCases]GenericStruct`1<!T>::InstanceParamOpen
    call       !!0 [System.Runtime]System.Runtime.CompilerServices.RuntimeHelpers::GetDelegate<class [System.Runtime]System.Func`3<valuetype [RuntimeHelpersGetDelegateCases]GenericStruct`1<!T>,object,object>>(native int, !!0&)
    ret
  } // end of method GenericStructIl`1::GetInstanceParamOpen

  .method public hidebysig static class [System.Runtime]System.Func`1<class [System.Runtime]System.Type>
          GetInstanceGenericClosed() cil managed noinlining
  {
    .maxstack  2
    ldftn      instance class [System.Runtime]System.Type valuetype [RuntimeHelpersGetDelegateCases]GenericStruct`1<!T>::InstanceGeneric()
    ldsflda    class [System.Runtime]System.Func`1<class [System.Runtime]System.Type> valuetype [RuntimeHelpersGetDelegateCases]GenericStruct`1<!T>::InstanceGenericClosed
    call       !!0 [System.Runtime]System.Runtime.CompilerServices.RuntimeHelpers::GetDelegate<class [System.Runtime]System.Func`1<class [System.Runtime]System.Type>>(native int, !!0&)
    ret
  } // end of method GenericStructIl`1::GetInstanceGenericClosed

  .method public hidebysig static class [System.Runtime]System.Func`2<valuetype [RuntimeHelpersGetDelegateCases]GenericStruct`1<!T>,class [System.Runtime]System.Type>
          GetInstanceGenericOpen() cil managed noinlining
  {
    .param [0]
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8[]) = ( 01 00 04 00 00 00 01 00 01 01 00 00 )
    .maxstack  2
    ldftn      instance class [System.Runtime]System.Type valuetype [RuntimeHelpersGetDelegateCases]GenericStruct`1<!T>::InstanceGeneric()
    ldsflda    class [System.Runtime]System.Func`2<valuetype [RuntimeHelpersGetDelegateCases]GenericStruct`1<!0>,class [System.Runtime]System.Type> valuetype [RuntimeHelpersGetDelegateCases]GenericStruct`1<!T>::InstanceGenericOpen
    call       !!0 [System.Runtime]System.Runtime.CompilerServices.RuntimeHelpers::GetDelegate<class [System.Runtime]System.Func`2<valuetype [RuntimeHelpersGetDelegateCases]GenericStruct`1<!T>,class [System.Runtime]System.Type>>(native int, !!0&)
    ret
  } // end of method GenericStructIl`1::GetInstanceGenericOpen

  .method public hidebysig static class [System.Runtime]System.Func`2<!T,class [System.Runtime]System.Type>
          GetInstanceGenericParamClosed() cil managed noinlining
  {
    .maxstack  2
    ldftn      instance class [System.Runtime]System.Type valuetype [RuntimeHelpersGetDelegateCases]GenericStruct`1<!T>::InstanceGenericParam(!0)
    ldsflda    class [System.Runtime]System.Func`2<!0,class [System.Runtime]System.Type> valuetype [RuntimeHelpersGetDelegateCases]GenericStruct`1<!T>::InstanceGenericParamClosed
    call       !!0 [System.Runtime]System.Runtime.CompilerServices.RuntimeHelpers::GetDelegate<class [System.Runtime]System.Func`2<!T,class [System.Runtime]System.Type>>(native int, !!0&)
    ret
  } // end of method GenericStructIl`1::GetInstanceGenericParamClosed

  .method public hidebysig static class [System.Runtime]System.Func`3<valuetype [RuntimeHelpersGetDelegateCases]GenericStruct`1<!T>,!T,class [System.Runtime]System.Type>
          GetInstanceGenericParamOpen() cil managed noinlining
  {
    .param [0]
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8[]) = ( 01 00 05 00 00 00 01 00 01 01 01 00 00 )
    .maxstack  2
    ldftn      instance class [System.Runtime]System.Type valuetype [RuntimeHelpersGetDelegateCases]GenericStruct`1<!T>::InstanceGenericParam(!0)
    ldsflda    class [System.Runtime]System.Func`3<valuetype [RuntimeHelpersGetDelegateCases]GenericStruct`1<!0>,!0,class [System.Runtime]System.Type> valuetype [RuntimeHelpersGetDelegateCases]GenericStruct`1<!T>::InstanceGenericParamOpen
    call       !!0 [System.Runtime]System.Runtime.CompilerServices.RuntimeHelpers::GetDelegate<class [System.Runtime]System.Func`3<valuetype [RuntimeHelpersGetDelegateCases]GenericStruct`1<!T>,!T,class [System.Runtime]System.Type>>(native int, !!0&)
    ret
  } // end of method GenericStructIl`1::GetInstanceGenericParamOpen

  .method public hidebysig static class [System.Runtime]System.Func`1<valuetype [System.Runtime]System.ValueTuple`2<class [System.Runtime]System.Type,class [System.Runtime]System.Type>>
          GetInstanceDoubleGenericClosed<T2>() cil managed noinlining
  {
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.NullableContextAttribute::.ctor(uint8) = ( 01 00 02 00 00 )
    .param [0]
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8[]) = ( 01 00 04 00 00 00 01 00 01 01 00 00 )
    .maxstack  2
    ldftn      instance valuetype [System.Runtime]System.ValueTuple`2<class [System.Runtime]System.Type,class [System.Runtime]System.Type> valuetype [RuntimeHelpersGetDelegateCases]GenericStruct`1<!T>::InstanceDoubleGeneric<!!0>()
    ldsflda    class [System.Runtime]System.Func`1<valuetype [System.Runtime]System.ValueTuple`2<class [System.Runtime]System.Type,class [System.Runtime]System.Type>> class [RuntimeHelpersGetDelegateCases]GenericStruct`1/Cache`1<!T,!!T2>::InstanceDoubleGenericClosed
    call       !!0 [System.Runtime]System.Runtime.CompilerServices.RuntimeHelpers::GetDelegate<class [System.Runtime]System.Func`1<valuetype [System.Runtime]System.ValueTuple`2<class [System.Runtime]System.Type,class [System.Runtime]System.Type>>>(native int, !!0&)
    ret
  } // end of method GenericStructIl`1::GetInstanceDoubleGenericClosed

  .method public hidebysig static class [System.Runtime]System.Func`2<valuetype [RuntimeHelpersGetDelegateCases]GenericStruct`1<!T>,valuetype [System.Runtime]System.ValueTuple`2<class [System.Runtime]System.Type,class [System.Runtime]System.Type>>
          GetInstanceDoubleGenericOpen<T2>() cil managed noinlining
  {
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.NullableContextAttribute::.ctor(uint8) = ( 01 00 02 00 00 )
    .param [0]
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8[]) = ( 01 00 06 00 00 00 01 00 01 00 01 01 00 00 )
    .maxstack  2
    ldftn      instance valuetype [System.Runtime]System.ValueTuple`2<class [System.Runtime]System.Type,class [System.Runtime]System.Type> valuetype [RuntimeHelpersGetDelegateCases]GenericStruct`1<!T>::InstanceDoubleGeneric<!!0>()
    ldsflda    class [System.Runtime]System.Func`2<valuetype [RuntimeHelpersGetDelegateCases]GenericStruct`1<!0>,valuetype [System.Runtime]System.ValueTuple`2<class [System.Runtime]System.Type,class [System.Runtime]System.Type>> class [RuntimeHelpersGetDelegateCases]GenericStruct`1/Cache`1<!T,!!T2>::InstanceDoubleGenericOpen
    call       !!0 [System.Runtime]System.Runtime.CompilerServices.RuntimeHelpers::GetDelegate<class [System.Runtime]System.Func`2<valuetype [RuntimeHelpersGetDelegateCases]GenericStruct`1<!T>,valuetype [System.Runtime]System.ValueTuple`2<class [System.Runtime]System.Type,class [System.Runtime]System.Type>>>(native int, !!0&)
    ret
  } // end of method GenericStructIl`1::GetInstanceDoubleGenericOpen

} // end of class GenericStructIl`1

.class public abstract auto ansi sealed beforefieldinit VirtualResolver
       extends [System.Runtime]System.Object
{
  .custom instance void [System.Runtime]System.Runtime.CompilerServices.NullableContextAttribute::.ctor(uint8) = ( 01 00 01 00 00 )
  .custom instance void [System.Runtime]System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = ( 01 00 00 00 00 )
  .method public hidebysig static class [System.Runtime]System.Func`2<class [RuntimeHelpersGetDelegateCases]IInterface,object>
          GetInterfaceInstance() cil managed noinlining
  {
    .maxstack  2
    ldftn      instance object [RuntimeHelpersGetDelegateCases]IInterface::InterfaceInstance()
    ldsflda    class [System.Runtime]System.Func`2<class [RuntimeHelpersGetDelegateCases]IInterface,object> [RuntimeHelpersGetDelegateCases]VirtualCache::InterfaceInstance
    call       !!0 [System.Runtime]System.Runtime.CompilerServices.RuntimeHelpers::GetDelegate<class [System.Runtime]System.Func`2<class [RuntimeHelpersGetDelegateCases]IInterface,object>>(native int, !!0&)
    ret
  } // end of method VirtualResolver::GetInterfaceInstance

  .method public hidebysig static class [System.Runtime]System.Func`2<class [RuntimeHelpersGetDelegateCases]IInterface,object>
          GetInterfaceInstanceDim() cil managed noinlining
  {
    .maxstack  2
    ldftn      instance object [RuntimeHelpersGetDelegateCases]IInterface::InterfaceInstanceDim()
    ldsflda    class [System.Runtime]System.Func`2<class [RuntimeHelpersGetDelegateCases]IInterface,object> [RuntimeHelpersGetDelegateCases]VirtualCache::InterfaceInstanceDim
    call       !!0 [System.Runtime]System.Runtime.CompilerServices.RuntimeHelpers::GetDelegate<class [System.Runtime]System.Func`2<class [RuntimeHelpersGetDelegateCases]IInterface,object>>(native int, !!0&)
    ret
  } // end of method VirtualResolver::GetInterfaceInstanceDim

  .method public hidebysig static class [System.Runtime]System.Func`2<class [RuntimeHelpersGetDelegateCases]IInterface,object>
          GetInterfaceInstanceDimOverriden() cil managed noinlining
  {
    .maxstack  2
    ldftn      instance object [RuntimeHelpersGetDelegateCases]IInterface::InterfaceInstanceDimOverriden()
    ldsflda    class [System.Runtime]System.Func`2<class [RuntimeHelpersGetDelegateCases]IInterface,object> [RuntimeHelpersGetDelegateCases]VirtualCache::InterfaceInstanceDimOverriden
    call       !!0 [System.Runtime]System.Runtime.CompilerServices.RuntimeHelpers::GetDelegate<class [System.Runtime]System.Func`2<class [RuntimeHelpersGetDelegateCases]IInterface,object>>(native int, !!0&)
    ret
  } // end of method VirtualResolver::GetInterfaceInstanceDimOverriden

  .method public hidebysig static class [System.Runtime]System.Func`2<class [RuntimeHelpersGetDelegateCases]BaseClass,object>
          GetBaseAbstract() cil managed noinlining
  {
    .maxstack  2
    ldftn      instance object [RuntimeHelpersGetDelegateCases]BaseClass::BaseAbstract()
    ldsflda    class [System.Runtime]System.Func`2<class [RuntimeHelpersGetDelegateCases]BaseClass,object> [RuntimeHelpersGetDelegateCases]VirtualCache::BaseAbstract
    call       !!0 [System.Runtime]System.Runtime.CompilerServices.RuntimeHelpers::GetDelegate<class [System.Runtime]System.Func`2<class [RuntimeHelpersGetDelegateCases]BaseClass,object>>(native int, !!0&)
    ret
  } // end of method VirtualResolver::GetBaseAbstract

  .method public hidebysig static class [System.Runtime]System.Func`2<class [RuntimeHelpersGetDelegateCases]BaseClass,object>
          GetBaseVirtual() cil managed noinlining
  {
    .maxstack  2
    ldftn      instance object [RuntimeHelpersGetDelegateCases]BaseClass::BaseVirtual()
    ldsflda    class [System.Runtime]System.Func`2<class [RuntimeHelpersGetDelegateCases]BaseClass,object> [RuntimeHelpersGetDelegateCases]VirtualCache::BaseVirtual
    call       !!0 [System.Runtime]System.Runtime.CompilerServices.RuntimeHelpers::GetDelegate<class [System.Runtime]System.Func`2<class [RuntimeHelpersGetDelegateCases]BaseClass,object>>(native int, !!0&)
    ret
  } // end of method VirtualResolver::GetBaseVirtual

  .method public hidebysig static class [System.Runtime]System.Func`2<class [RuntimeHelpersGetDelegateCases]BaseClass,object>
          GetBaseVirtualOverriden() cil managed noinlining
  {
    .maxstack  2
    ldftn      instance object [RuntimeHelpersGetDelegateCases]BaseClass::BaseVirtualOverriden()
    ldsflda    class [System.Runtime]System.Func`2<class [RuntimeHelpersGetDelegateCases]BaseClass,object> [RuntimeHelpersGetDelegateCases]VirtualCache::BaseVirtualOverriden
    call       !!0 [System.Runtime]System.Runtime.CompilerServices.RuntimeHelpers::GetDelegate<class [System.Runtime]System.Func`2<class [RuntimeHelpersGetDelegateCases]BaseClass,object>>(native int, !!0&)
    ret
  } // end of method VirtualResolver::GetBaseVirtualOverriden

} // end of class VirtualResolver
