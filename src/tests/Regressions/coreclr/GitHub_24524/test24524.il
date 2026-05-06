// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


//
// This test checks that signatures for fields are encoded properly in cross-module scenario.
// It needs to be crossgen-ed with large version bubble enabled to test. It always succeeds
// in other cases.
//
.assembly extern System.Runtime { auto }
.assembly extern xunit.core {}
.assembly extern System.Console { auto }

.assembly test24524{}

.class public auto ansi test24524 {
.method public hidebysig static int32
        Main() cil managed
{
  .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
      01 00 00 00
  )
  .entrypoint
  .maxstack  2

  ldtoken field object [System.Runtime]System.Type::Missing
  call class[System.Runtime]System.Reflection.FieldInfo [System.Runtime]System.Reflection.FieldInfo::GetFieldFromHandle(valuetype [System.Runtime]System.RuntimeFieldHandle)
  call void [System.Console]System.Console::WriteLine(object)

  ldc.i4     100
  ret
}
}
