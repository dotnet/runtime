.assembly iltests {}
.assembly extern TestDriver {}
.assembly extern mscorlib {}

.class public auto ansi sealed beforefieldinit Tests {

	.method static public int32 Main() il managed {
		.entrypoint
		
		ldtoken Tests
		call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call       int32 [TestDriver]TestDriver::RunTests(class [mscorlib]System.Type)
		ret
	}

	// make sure the register allocator works when the return value of
	// 'div' is discarded
	.method static public int32 test_0_div_regalloc () il managed {
		.locals init (
			int32 i
		)

		ldloc 0
		ldc.i4.s 0xa
		div
		pop
		ldc.i4.0
		ret
	}

	.method static public int32 test_3_copy_used_bug () il managed {

		.locals init (
			int32 size,
			int32 res
		)

		ldc.i4 0
		stloc res

		ldc.i4 1
		stloc size

		ldloc size
		ldloc size
		ldloc size
		add
		stloc size
		ldloc size
		add
		stloc res

		ldloc res
		ret
	}

	// demonstrate that the copy_used_var is not a fix for the above bug
	.method static public int32 test_3_copy_used_indir_bug () il managed {

		.locals init (
			int32 size,
			int32 res
		)

		ldc.i4 0
		stloc res

		ldc.i4 1
		stloc size

		ldloc size
		ldloca size
		ldloc size
		ldloc size
		add
		stind.i4
		ldloc size
		add
		stloc res

		ldloc res
		ret
	}

	.method static public void do_nothing (int32 a) il managed {
		ret
	}
	
	// demonstrate the block_split failure: needs -O=inline
	// mini -O=inline --compile Tests:test_0_split_block_bug iltests.exe
	.method static public int32 test_0_split_block_bug () il managed {

		.locals init (
			int32 i1
		)

		ldc.i4 1
		stloc i1
 test_label:
		ldloc i1
		call void class Tests::do_nothing (int32)
		ldc.i4 0
		brtrue test_label
		
		ldc.i4 0
		ret
	}

	.method public void inline_do_nothing () il managed {
		ret
	}
	.method static public int32 test_1_checkthis_inlining () il managed {
		ldnull
		call instance void class Tests::inline_do_nothing ()
		ldc.i4 1
		ret
	}

	.class nested private auto ansi sealed beforefieldinit TailCallStruct 
		extends [mscorlib]System.ValueType {
		.field public int32 a
		.field public int32 b
	}

	.method static valuetype Tests/TailCallStruct tail1 (valuetype Tests/TailCallStruct arg) {
		ldarga 0
		ldarga 0
		ldfld int32 Tests/TailCallStruct::a
		ldc.i4.1
		add
		stfld int32 Tests/TailCallStruct::a
		ldarga 0
		ldarga 0
		ldfld int32 Tests/TailCallStruct::a
		ldc.i4.2
		add
		stfld int32 Tests/TailCallStruct::a
		ldarg.0
		ret
	}

	.method static valuetype Tests/TailCallStruct tail2 (valuetype Tests/TailCallStruct arg) {
		ldarg.0
		tail.
		call valuetype Tests/TailCallStruct Tests::tail1 (valuetype Tests/TailCallStruct)
		ret
	}

	.method static public int32 test_0_tail_calls () il managed {
		.maxstack 16
		.locals init (
			valuetype Tests/TailCallStruct arg
		)
		ldloca 0
		ldc.i4.2
		stfld int32 Tests/TailCallStruct::a
		ldloca 0
		ldc.i4.4
		stfld int32 Tests/TailCallStruct::b
		ldloc.0
		call valuetype Tests/TailCallStruct Tests::tail2 (valuetype Tests/TailCallStruct)
		stloc.0
		ldloca 0
		ldfld int32 Tests/TailCallStruct::a
		ldloca 0
		ldfld int32 Tests/TailCallStruct::b
		add
		ldc.i4 9
		sub
		ret
	}

	.method static public int32 test_11_switch_with_nonempty_stack () il managed {
		.maxstack 16

		ldc.i4.5
		ldc.i4.6
		ldc.i4.1
		switch (L0, L1)
	L0: 
		add
		ret
	L1:
		add
		ret
	}
 
    .method public static int32 test_5_endfinally_with_nonempty_stack () il managed {
		.maxstack 16

        .try {
          leave IL_0
        }
        finally  {
          ldc.i4.0
          endfinally
        }
        IL_0:  ldc.i4.5
		ret
    }
}
