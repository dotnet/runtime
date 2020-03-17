.assembly snippets {}
.assembly extern mscorlib {}

.class public auto ansi sealed beforefieldinit Snippets {

	.method static public int32 Main(string[] args) il managed {
		.entrypoint
		ldc.i4.s 0x1337
		ret
	}

	.method public hidebysig static int32 no_inline_int32 () cil managed noinlining {
		.maxstack 0
		ldc.i4 0x1337
		ret
	}

	.method public hidebysig static int32 test_cprop_add_consts () cil managed {
		ldc.i4 0x1122
		ldc.i4 0x3344
		add
		ret
	}

	.method public hidebysig static int32 test_cprop_ldloc_stloc () cil managed {
		.locals init (
			int32 i
		)
		call int32 class Snippets::no_inline_int32 ()
		stloc 0
		ldloc 0
		ldloc 0
		add
		ret
	}
}
