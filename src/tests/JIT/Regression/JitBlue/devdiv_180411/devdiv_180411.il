// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}

.assembly devdiv_180411
{
}
.class _simple {

.method static int32 main(class System.String[]) {
.entrypoint
.maxstack	100
call       int64 [devdiv_180411]_simple::test()
conv.i4
ldc.i4 100
add     
ret  
}

.method static int64 test() {
.maxstack	100
.locals init (int32 local_0x1)

ldloc.s      0
neg         
conv.ovf.u8 
conv.i4     
neg         
conv.u8
ret  
}

}
