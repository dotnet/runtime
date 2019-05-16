// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


//
// This test checks that signatures for fields are encoded properly in cross-module scenario.
// It needs to be crossgen-ed with large version bubble enabled to test. It always succeeds
// in other cases.
//
.assembly extern System.Private.CoreLib { auto }
.assembly extern System.Console { auto }

.assembly test24524{}

.method private hidebysig static int32
        Main() cil managed
{
  .entrypoint
  .maxstack  2

  ldtoken field object [System.Private.CoreLib]System.Type::Missing
  call class[System.Private.CoreLib]System.Reflection.FieldInfo [System.Private.CoreLib]System.Reflection.FieldInfo::GetFieldFromHandle(valuetype [System.Private.CoreLib]System.RuntimeFieldHandle)
  call void [System.Console]System.Console::WriteLine(object)

  ldc.i4     100
  ret
}
