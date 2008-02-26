.assembly il2tests {}
.assembly extern TestDriver {}
.assembly extern mscorlib {}

.class public auto ansi sealed beforefieldinit Tests {

	.method static public int32 Main(string[] args) cil managed {
		.entrypoint
		
		ldtoken Tests
		call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		ldarg.0
		call       int32 [TestDriver]TestDriver::RunTests(class [mscorlib]System.Type, string[])
		ret
	}

	.method private static hidebysig default int32 Foo<T> (!!T n)  cil managed {
		ldarg.0
		box !!0
		brtrue HAS_VALUE
		ldc.i4.0
		ret
HAS_VALUE:	ldc.i4.1
		ret
	}

	// bug 78019
	.method static public int32 test_0_nullable_box_brtrue () cil managed {

		.locals init (valuetype [mscorlib]System.Nullable`1<int32> V_0)

		ldloc.0
		call int32 class Tests::Foo<valuetype [mscorlib]System.Nullable`1<int32>> (!!0)
		ret
	}
}

