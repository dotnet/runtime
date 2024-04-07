.assembly extern System.Runtime { }
.assembly extern xunit.core {}

.assembly sharedinterfacemethod { }

.class interface private abstract auto ansi IFoo`1<T>
{
  .method public hidebysig virtual newslot instance class [System.Runtime]System.Type Frob() cil managed
  {
    ldtoken !0
    call class [System.Runtime]System.Type class [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
    ret
  }
}

.class public auto ansi beforefieldinit Fooer
       extends [System.Runtime]System.Object
       implements class IFoo`1<class [System.Runtime]System.Object>,
                  class IFoo`1<valuetype [System.Runtime]System.Int32>
{
  .method public hidebysig specialname rtspecialname
          instance void .ctor() cil managed
  {
    ldarg.0
    call instance void [System.Runtime]System.Object::.ctor()
    ret
  }
}

.class public auto ansi abstract sealed beforefieldinit sharedinterfacemethod
    extends [System.Runtime]System.Object
{
  .method public hidebysig static int32 Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint

    // Callvirt to a shared interface method

    newobj instance void Fooer::.ctor()
    callvirt instance class [System.Runtime]System.Type class IFoo`1<valuetype [System.Runtime]System.Int32>::Frob()
    ldtoken valuetype [System.Runtime]System.Int32
    call class [System.Runtime]System.Type class [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
    ceq
    brtrue UnsharedCallvirtIsGood

    ldc.i4.1
    ret

  UnsharedCallvirtIsGood:

    // Call to a shared interface

    newobj instance void Fooer::.ctor()
    call instance class [System.Runtime]System.Type class IFoo`1<valuetype [System.Runtime]System.Int32>::Frob()
    ldtoken valuetype [System.Runtime]System.Int32
    call class [System.Runtime]System.Type class [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
    ceq
    brtrue UnsharedCallIsGood

    ldc.i4.2
    ret

  UnsharedCallIsGood:

    // Callvirt to an unshared interface method

    newobj instance void Fooer::.ctor()
    callvirt instance class [System.Runtime]System.Type class IFoo`1<class [System.Runtime]System.Object>::Frob()
    ldtoken class [System.Runtime]System.Object
    call class [System.Runtime]System.Type class [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
    ceq
    brtrue SharedCallvirtIsGood

    ldc.i4.3
    ret

  SharedCallvirtIsGood:

    // Call to an unshared interface method

    newobj instance void Fooer::.ctor()
    call instance class [System.Runtime]System.Type class IFoo`1<class [System.Runtime]System.Object>::Frob()
    ldtoken class [System.Runtime]System.Object
    call class [System.Runtime]System.Type class [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
    ceq
    brtrue SharedCallIsGood

    ldc.i4.4
    ret

  SharedCallIsGood:

    ldc.i4 100
    ret
  }
}
