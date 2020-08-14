// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0xdc3a83a8 {}
.class ILGEN_0xdc3a83a8 {

.field static	int16[] field_0x0
.field static	int64[] field_0x2
.method static int32 Main() {
.entrypoint
.maxstack	20
.try {
 ldc.i4 255
 newarr [mscorlib]System.Int16 
 stsfld int16[] ILGEN_0xdc3a83a8::field_0x0
 ldc.i4 255
 newarr [mscorlib]System.Int64 
 stsfld int64[] ILGEN_0xdc3a83a8::field_0x2

 ldsfld int64[] ILGEN_0xdc3a83a8::field_0x2
 ldc.i4 0x4d225a69 
 ldelema [mscorlib]System.Int64
 ldind.i8
 conv.i4
 ldc.i4 0x4d225a69 
 conv.i8
 ldsfld int16[] ILGEN_0xdc3a83a8::field_0x0
 ldc.i8 0x9f4310c57657362 
 conv.r4
 conv.i4
 ldelem.i2
 conv.i8
 mul
 conv.i
 mul
pop
leave sss
} catch [mscorlib]System.IndexOutOfRangeException {
pop
leave sss
}
sss:
ldc.i4 100

ret
}

}
