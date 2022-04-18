// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
//
.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'i_conv'
{ }
.assembly extern xunit.core {}
.class public auto ansi Test_i_conv extends [mscorlib]System.Object
{
.method private hidebysig static native int to_native(int32) il managed
{
	ldarg.0
	conv.ovf.i
	ret
}
.method public hidebysig static int32 Main() il managed
{
	.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
	    01 00 00 00
	)
	.entrypoint
	.maxstack  8
	//------------------------
	// I -> I4 conversion test
	//------------------------
	.try {
		ldc.i4 0x7fffffff
		call native int Test_i_conv::to_native(int32)
		conv.ovf.i4
		ldc.i4 0x7fffffff
		beq OK1_1
		leave FAIL
	OK1_1:
		leave OK
	} catch [mscorlib]System.Exception {
		pop
		leave FAIL
	}
	.try {
		ldc.i4.m1
		call native int Test_i_conv::to_native(int32)
		conv.ovf.i4
		ldc.i4.m1
		beq OK1_2
		leave FAIL
	OK1_2:
		leave OK
	} catch [mscorlib]System.Exception {
		pop
		leave FAIL
	}
	//------------------------
	// I -> I2 conversion test
	//------------------------
	.try {
		ldc.i4 0x7fffffff
		call native int Test_i_conv::to_native(int32)
		conv.ovf.i2
		pop
		leave FAIL
	} catch [mscorlib]System.Exception {
		pop
		leave OK
	}
	.try {
		ldc.i4 0x7fffffff
		call native int Test_i_conv::to_native(int32)
		conv.i2
		ldc.i4.m1
		beq OK2_1
		leave FAIL
	OK2_1:
		leave OK
	} catch [mscorlib]System.Exception {
		pop
		leave FAIL
	}
	.try {
		ldc.i4 0x7fff
		call native int Test_i_conv::to_native(int32)
		conv.ovf.i2
		ldc.i4 0x7fff
		beq OK2_2
		leave FAIL
	OK2_2:
		leave OK
	} catch [mscorlib]System.Exception {
		pop
		leave FAIL
	}
	.try {
		ldc.i4.m1
		call native int Test_i_conv::to_native(int32)
		conv.ovf.i2
		ldc.i4.m1
		beq OK2_3
		leave FAIL
	OK2_3:
		leave OK
	} catch [mscorlib]System.Exception {
		pop
		leave FAIL
	}
	.try {
		ldc.i4.m1
		call native int Test_i_conv::to_native(int32)
		conv.ovf.i2
		ldc.i4.m1
		beq OK2_4
		leave FAIL
	OK2_4:
		leave OK
	} catch [mscorlib]System.Exception {
		pop
		leave FAIL
	}
	//------------------------
	// I -> I1 conversion test
	//------------------------
	.try {
		ldc.i4 0x7fff
		call native int Test_i_conv::to_native(int32)
		conv.ovf.i1
		pop
		leave FAIL
	} catch [mscorlib]System.Exception {
		pop
		leave OK
	}
	.try {
		ldc.i4 0x7fff
		call native int Test_i_conv::to_native(int32)
		conv.i1
		ldc.i4.m1
		beq OK3_1
		leave FAIL
	OK3_1:
		leave OK
	} catch [mscorlib]System.Exception {
		pop
		leave FAIL
	}
	.try {
		ldc.i4 0x7f
		call native int Test_i_conv::to_native(int32)
		conv.ovf.i1
		ldc.i4 0x7f
		beq OK3_2
		leave FAIL
	OK3_2:
		leave OK
	} catch [mscorlib]System.Exception {
		pop
		leave FAIL
	}
	//------------------------
	// I -> U4 conversion test
	//------------------------
	.try {
		ldc.i4 0x7fffffff
		call native int Test_i_conv::to_native(int32)
		conv.ovf.u4.un
		ldc.i4 0x7fffffff
		beq OK4_1
		leave FAIL
	OK4_1:
		leave OK
	} catch [mscorlib]System.Exception {
		pop
		leave FAIL
	}
	.try {
		ldc.i4.m1
		call native int Test_i_conv::to_native(int32)
		conv.ovf.u4
		ldc.i4.m1
		beq OK4_2
		leave FAIL
	OK4_2:
		leave OK
	} catch [mscorlib]System.Exception {
		pop
		leave FAIL
	}
	//------------------------
	// I -> U1 conversion test
	//------------------------
	.try {
		ldc.i4 0x7fff
		call native int Test_i_conv::to_native(int32)
		conv.ovf.u1.un
		pop
		leave FAIL
	} catch [mscorlib]System.Exception {
		pop
		leave OK
	}
	.try {
		ldc.i4 0x7fff
		call native int Test_i_conv::to_native(int32)
		conv.u1
		ldc.i4.m1
		beq OK5_1
		leave FAIL
	OK5_1:
		leave OK
	} catch [mscorlib]System.Exception {
		pop
		leave FAIL
	}
	.try {
		ldc.i4 0x7f
		call native int Test_i_conv::to_native(int32)
		conv.ovf.u1
		ldc.i4 0x7f
		beq OK5_2
		leave FAIL
	OK5_2:
		leave OK
	} catch [mscorlib]System.Exception {
		pop
		leave FAIL
	}
	//------------------------
	// I -> R4 conversion test
	//------------------------
	ldc.i4 0x7fff
	dup
	call native int Test_i_conv::to_native(int32)
	conv.r4
	conv.i
	ceq
	brfalse FAIL
	ldc.i4 -1900
	dup
	call native int Test_i_conv::to_native(int32)
	conv.r4
	conv.i
	ceq
	brfalse FAIL
	//------------------------
	// I -> R8 conversion test
	//------------------------
	ldc.i4 0x7fff
	dup
	call native int Test_i_conv::to_native(int32)
	conv.r8
	conv.i
	ceq
	brfalse FAIL
	ldc.i4 -1900
	dup
	call native int Test_i_conv::to_native(int32)
	conv.r8
	conv.i
	ceq
	brfalse FAIL
	
OK:
	ldc.i4 0x64
	ldstr "passed"
	br PRINT
FAIL:
	ldc.i4.1
	ldstr "failed"
PRINT:
	call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
	ret
} // end of method 'Test::Main'

.method public hidebysig specialname rtspecialname 
        instance void .ctor() il managed
{
  // Code size       7 (0x7)
  .maxstack  8
  IL_0000:  ldarg.0
  IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
  IL_0006:  ret
} // end of method 'Test::.ctor'

} // end of class 'Test'
