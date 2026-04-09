// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b07341'
{}
.class public ExceptionTest {

.method public static int32 main(){
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
L1:
  newobj instance void [mscorlib]System.Exception::.ctor() throw
L2:
ldc.i4 100
  ret

.try L1 to L2 filter {
  pop
  ldc.i4 1
  endfilter
}
{ //handler
  pop
  leave L2
  newobj instance void [mscorlib]System.Exception::.ctor() throw // this is just to stop verifier complaining, see BugID 7339
}

}
}
