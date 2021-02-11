// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x7a129ee8 {}
.class ILGEN_0x7a129ee8 {
.method static int32 Method_0x63d7449b() {
.maxstack	17
.locals (unsigned int64 local_0x0)
 ldc.i8 0x92c3df118380ec63 
 stloc local_0x0
Start_Orphan_0:
 ldc.i8 0x9d67f62eaa865926
 ldloc local_0x0
 rem
 ldc.i4 5
 conv.i8
 rem
 pop
End_Orphan_0:
 ldc.i4 55
 ret
}
.method static int32 Main() {
.entrypoint
.maxstack	20
call int32 ILGEN_0x7a129ee8::Method_0x63d7449b()
ldc.i4 45
add
 ret
}
}
