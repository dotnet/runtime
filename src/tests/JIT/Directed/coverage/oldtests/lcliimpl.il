// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly lcliimpl {}
.class public auto ansi lcliimpl {
.method public static int32 main() cil managed
{
  .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
      01 00 00 00
  )
  .entrypoint
  call int32 f()
  ret
}
}

.method public static int32 f() cil managed
{
  // Code size       26 (0x1a)
  .maxstack  1
  .locals init (class itest& V_0,
           class ctest V_1)
  IL_0000:  ldnull
  IL_0001:  stloc.1
  IL_0002:  newobj     instance void ctest::.ctor()
  IL_0007:  stloc.1
  IL_0008:  ldloca.s   V_1
  IL_000a:  //castclass  itest
  IL_000f:  stloc.0
  IL_0010:  ldloc.0
  IL_0011:  ldind.ref
  IL_0012:  callvirt   instance int32 itest::f()
  IL_0017:  br.s       IL_0019

  IL_0019:  ret
}

.class interface private abstract auto ansi itest
{
  .method public newslot abstract virtual 
          instance int32  f() cil managed
  {
  } // end of method itest::f

} // end of class itest

.class private auto ansi ctest
       extends [mscorlib]System.Object
       implements itest
{
  .method public virtual instance int32  f() cil managed
  {
    // Code size       5 (0x5)
    .maxstack  1
    IL_0000:  ldc.i4.s   100
    IL_0002:  br.s       IL_0004

    IL_0004:  ret
  } // end of method ctest::f

  .method public specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method ctest::.ctor

}
