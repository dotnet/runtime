// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0xa8b0959c {}
.class ILGEN_0xa8b0959c {
.field static	float32 field_0x2
.method static int32  Method_0xc0d2787() {
.maxstack	9
.locals (float64[] LOCAL_0x0)
 ldc.i4 255
 newarr [mscorlib]System.Double 
 stloc LOCAL_0x0
 ldc.r4 1.1 
 stsfld float32 ILGEN_0xa8b0959c::field_0x2

    ldsfld float32 ILGEN_0xa8b0959c::field_0x2
    ldsfld float32 ILGEN_0xa8b0959c::field_0x2
   rem 
    ldc.i4.s -15
   conv.r.un
  sub  
 conv.i4

// workaround for P4
// F->I, overflow to undefined
pop
ldc.i4 15

 ret
}
.method static int32 Main() {
.entrypoint
.maxstack	11
 call int32  ILGEN_0xa8b0959c::Method_0xc0d2787()
 conv.i4
 ldc.i4 85
 add
 ret
}
}
