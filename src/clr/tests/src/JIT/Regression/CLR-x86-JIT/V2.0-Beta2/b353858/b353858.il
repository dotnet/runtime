// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.




.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly q {}

.method assembly static void CreateArrayOfVoid() noinlining
{
    .maxstack 1
    ldc.i4.s   10000
    newarr     [mscorlib]System.Void
    pop
    ret
}


.method assembly static int32  main() cil managed
{
  .entrypoint
  .maxstack  1

  .try
  {
    call void CreateArrayOfVoid()
    leave.s    FAIL
  }
  catch [mscorlib]System.InvalidProgramException
  {
    pop
    leave.s    PASS
  }

FAIL:
  ldstr      "FAIL"
  call       void [System.Console]System.Console::WriteLine(string)
  ldc.i4.0
  br.s       END

PASS:
  ldstr      "PASS"
  call       void [System.Console]System.Console::WriteLine(string)
  ldc.i4     100
  br.s       END

END:

  ret
}
