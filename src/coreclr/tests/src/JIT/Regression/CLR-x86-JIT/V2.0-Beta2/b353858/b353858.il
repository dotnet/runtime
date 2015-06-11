// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.




.assembly extern mscorlib {}
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
  call       void [mscorlib]System.Console::WriteLine(string)
  ldc.i4.0
  br.s       END

PASS:
  ldstr      "PASS"
  call       void [mscorlib]System.Console::WriteLine(string)
  ldc.i4     100
  br.s       END

END:

  ret
}
