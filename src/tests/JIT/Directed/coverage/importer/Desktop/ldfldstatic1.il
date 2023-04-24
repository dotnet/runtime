// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern xunit.core {}

.assembly extern legacy library mscorlib {}
.assembly legacy library ldfldstatic1 { }
.class public auto ansi beforefieldinit ldfldstatic1
       extends [mscorlib]System.Object
{
  .field private static int32 fld
  .method private hidebysig static class ldfldstatic1 
          f() cil managed
  {
    .maxstack  1
    .locals init (class ldfldstatic1 V_0)
    IL_0000:  newobj     instance void ldfldstatic1::.ctor()
    IL_0005:  stloc.0
    IL_0006:  br.s       IL_0008

    IL_0008:  ldloc.0
    IL_0009:  ret
  } // end of method ldfldstatic1::f

  .method public hidebysig static int32  Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    .maxstack  1
    .locals init (class ldfldstatic1 V_0,
             int32 V_1)
    IL_0000:  call       class ldfldstatic1 ldfldstatic1::f()
    IL_0005:  //stloc.0
    IL_0006:  ldfld     int32 ldfldstatic1::fld
    IL_000b:  call       void [System.Console]System.Console::WriteLine(int32)
    IL_0010:  ldstr "PASSED"
	call       void [System.Console]System.Console::WriteLine(string)
	ldc.i4.s   100
    IL_0016:  ret
  } // end of method ldfldstatic1::Main

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method ldfldstatic1::.ctor

}
