// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime { }
.assembly extern xunit.core {}

.assembly extern provider { }

.assembly consumer { }

.class public auto ansi beforefieldinit Test1
       extends [System.Runtime]System.Object
       implements [provider]I1,
                  [provider]I2,
                  [provider]I4,
                  class [provider]IGen2`1<class [System.Runtime]System.ValueType>,
                  class [provider]IGen2`1<class [System.Runtime]System.Object>,
                  class [provider]IGen1`1<class [System.Runtime]System.Object>
{
  .method public hidebysig static int32 Main()
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    .locals init (class Test1)
    newobj instance void Test1::.ctor()
    stloc.0

    ldloc.0
    callvirt   instance int32 [provider]I2::M1()
    ldloc.0
    callvirt   instance int32 [provider]I4::M1()
    add

    ldloc.0
    callvirt   instance int32 class [provider]IGen2`1<class [System.Runtime]System.ValueType>::M1()
    ldloc.0
    callvirt   instance int32 class [provider]IGen2`1<class [System.Runtime]System.Object>::M1()
    add

    add

    ldc.i4 90
    add

    ret
  }

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call instance void [System.Runtime]System.Object::.ctor()
    ret
  }
}
