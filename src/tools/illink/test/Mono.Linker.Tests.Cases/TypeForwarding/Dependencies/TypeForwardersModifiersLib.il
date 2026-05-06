.assembly extern mscorlib
{
}

.assembly extern 'TypeForwarderModifiersLibFwd'
{
}

.assembly 'TypeForwardersModifiersLib'
{
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}

.module 'TypeForwardersModifiersLib.dll'

.class public auto ansi beforefieldinit TestClass
       extends [mscorlib]System.Object
{  
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  }

  .method public hidebysig static void  Test1<valuetype .ctor (class [mscorlib]System.ValueType modreq([TypeForwarderModifiersLibFwd]C)) T>() cil managed
  {
    IL_0000:  ret
  }

  .method public hidebysig static void  Test2<valuetype .ctor (class [mscorlib]System.ValueType modopt([TypeForwarderModifiersLibFwd]C)) T>() cil managed
  {
    IL_0000:  ret
  }

  .method public hidebysig static void  TestAll () cil managed
  {
    call       void TestClass::Test1<class [TypeForwarderModifiersLibFwd]C>()
    call       void TestClass::Test2<class [TypeForwarderModifiersLibFwd]C>()
    ret
  }
}