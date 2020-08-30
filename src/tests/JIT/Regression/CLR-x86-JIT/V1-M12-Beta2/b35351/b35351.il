// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0xa276db72 {}
.class ILGEN_0xa276db72 {
.method static int32 Main() {
.entrypoint
.maxstack	20
.try {
 ldc.i4 0xFFFFFFFF 
 conv.ovf.u4.un
 conv.ovf.u4
 pop
  leave stop
 } catch [mscorlib]System.OverflowException {
 pop
 leave stop
 }
 stop:
ldc.i4 100
 ret
}

}
