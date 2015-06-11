// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

.assembly extern mscorlib {}
.assembly helper {}
.module helper.exe


.class private auto ansi beforefieldinit Helper
       extends [mscorlib]System.Object
{

  .method public hidebysig newslot virtual instance void 
          MyFunc<([mscorlib]System.Object) T>() cil managed
  {
    ret
  }

}


.method private hidebysig static int32 Main() cil managed
{
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
      call void [mscorlib]System.Console::WriteLine(string)
      leave.s EXIT
    }

EXIT:

    ldloc.1
    ret
}
