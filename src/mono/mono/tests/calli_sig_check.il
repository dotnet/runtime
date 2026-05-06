// Test that calli does signature checking

.assembly extern mscorlib{}
.assembly calli_sig_check{}

.method public static int32 calli_sig_check (string s) cil managed
{
	.maxstack 16
	ldc.i4.0
	ret
}

.method public static int32 calli_sig_check_2 () cil managed
{
	.maxstack 16

	ldc.r4 1.13
	ldftn int32 calli_sig_check(string)
	calli int32(string)
	ret
}

.method public static int32 test_0_calli_sig_check () cil managed
{
	.entrypoint
	.maxstack 16

	.try {
		call int32 calli_sig_check_2()
		pop
		leave L0
	} catch [mscorlib]System.InvalidProgramException {
		pop
		leave L1
	}

	L0:
		ldc.i4.1
		ret
	L1:
		ldc.i4.0
		ret
}
