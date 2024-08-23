.assembly extern mscorlib { }
.assembly extern xunit.core {}

.assembly extern System.Runtime {  }

.assembly genericcontext { }

.class interface private abstract auto ansi IFoo`1<T>
{
  .method public hidebysig newslot virtual instance bool IsInst(valuetype [mscorlib]System.RuntimeTypeHandle a) cil managed
  {
    ldarg.0
    ldarg.1
    call instance bool class IFoo`1<!T>::IsInstImpl(valuetype [mscorlib]System.RuntimeTypeHandle)

    ret
  }

  .method private hidebysig instance bool IsInstImpl(valuetype [mscorlib]System.RuntimeTypeHandle a) cil managed
  {
    ldarga 1
    ldtoken !T
    call instance bool valuetype [mscorlib]System.RuntimeTypeHandle::Equals(valuetype [mscorlib]System.RuntimeTypeHandle)
    ret
  }
}

.class private auto ansi beforefieldinit Gen`1<T>
       extends [mscorlib]System.Object
{ }

.class public auto ansi beforefieldinit Fooer`1<T>
       extends [mscorlib]System.Object
       implements class IFoo`1<class Gen`1<!T>>
{
  .method public hidebysig specialname rtspecialname
          instance void .ctor() cil managed
  {
    ldarg.0
    call instance void [mscorlib]System.Object::.ctor()
    ret
  }
}

.method public hidebysig static int32  RunTest() cil managed
{
  newobj instance void class Fooer`1<object>::.ctor()
  ldtoken class Gen`1<class [mscorlib]System.Object>
  callvirt instance bool class IFoo`1<class Gen`1<object>>::IsInst(valuetype [mscorlib]System.RuntimeTypeHandle)

  // If IFoo::IsInst returns true, return 100 else 0
  brtrue.s   PASS
  ldc.i4 0
  br.s       FAIL
  PASS:  ldc.i4 100
  FAIL:  nop
  ret
}

.class public auto ansi abstract sealed beforefieldinit genericcontext
    extends [System.Runtime]System.Object
{
  .method public hidebysig static int32 Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint

    ldstr "DefaultImplementationsOfInterfaces"
    call bool [System.Runtime]System.Runtime.CompilerServices.RuntimeFeature::IsSupported(string)

    // If default interfaces are not supported, consider the test successful.
    brtrue DoRunTest
    ldc.i4 100
    ret

  DoRunTest:
    call int32 RunTest()
    ret
  }
}
