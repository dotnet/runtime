// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b374944' {}
.assembly extern xunit.core {}


.class public auto ansi beforefieldinit Helper
       extends [mscorlib]System.Object
{

  .method public hidebysig newslot virtual instance void 
          MyFunc<([mscorlib]System.Object) T>() cil managed
  {
    ret
  }


.method public hidebysig static int32 Main() cil managed
{
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    .locals init (native int v_0, int32 v_1)

    ldc.i4.m1
    stloc.1

    .try {
      ldnull
      ldvirtftn   instance void Helper::MyFunc<string>()
      leave.s EXIT
    }
    catch [mscorlib]System.NullReferenceException
    {
      ldc.i4 100
      stloc.1
      ldstr "pass"
      call void [System.Console]System.Console::WriteLine(string)
      leave.s EXIT
    }

EXIT:

    ldloc.1
    ret
}
}
