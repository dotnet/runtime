// Test using calli with native methods and ensure that it respects calling convention

.assembly extern mscorlib{}
.assembly calli_native{}

.method public static pinvokeimpl ("libtest" winapi) int32 mono_test_has_thiscall_globals() cil managed preservesig {}
.method public static pinvokeimpl ("libtest" winapi) void * mono_test_get_native_thiscall2() cil managed preservesig {}

.method public static int32 test_0_calli_native () cil managed
{
	.entrypoint
	.locals init (void *V_0)

	call int32 mono_test_has_thiscall_globals()
	brfalse pass

	call void* mono_test_get_native_thiscall2()
	stloc.0
	ldc.i4 268894549
	ldc.i4 1212675791
	ldloc.0
	calli unmanaged thiscall int32 (int32, int32)
	ldc.i4 1481570339
	ceq
	brtrue pass

	ldstr "FAIL"
	call void [mscorlib]System.Console::WriteLine(string)
	ldc.i4.1
	ret

pass:
	ldstr "PASS"
	call void [mscorlib]System.Console::WriteLine(string)
	ldc.i4.0
	ret
}