// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly constrained
{
}
.class public auto ansi beforefieldinit Test
       extends [mscorlib]System.Object
{
  .method public hidebysig static void  M<([mscorlib]System.Object) T>(!!0 t) cil managed
  {
    // Code size       21 (0x15)
    .maxstack  1
    .locals init (class [mscorlib]System.Type V_0)
    IL_0000:  ldarga.s   t
    IL_0002:  constrained. !!0
    IL_0008:  callvirt   instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
    IL_000d:  stloc.0
    IL_000e:  ldloc.0
    IL_000f:  call       void [System.Console]System.Console::WriteLine(object)
    IL_0014:  ret
  } // end of method Test::M

  .method public hidebysig static int32 Main() cil managed
  {
    .entrypoint
    // Code size       17 (0x11)
    .maxstack  8
    IL_0000:  ldstr      "Hello"
    IL_0005:  call       void Test::M<string>(!!0)
    IL_000a:  ldc.i4.3
    IL_000b:  call       void Test::M<int32>(!!0)
    IL_0010:  ldc.i4 100
    			ret
  } // end of method Test::Main

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Test::.ctor

}
