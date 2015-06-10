// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

.assembly extern legacy library mscorlib {}
.assembly '7341'
{}
.class ExceptionTest {

.method static int32 main(){
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
