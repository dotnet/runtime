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
	
	
	.method static public int32 test_1_ceq_to_i4 () il managed {
		.locals init (
			int32   foo
		)
		ldc.i4 500
		stloc foo
		ldloc foo
		ldc.i4 500
		ceq
		stloc foo
		ldloc foo
		ret
	}
	
	.method static public int32 test_3_shl_regvars () il managed {
		.locals init (
			int32	a,
			int32	b,
			int32	r1,
			int32	r2,
			int32	r3
		)
			
		ldc.i4.2
		stloc      a
		ldc.i4.1
		stloc      b
	
		ldloc      a
		ldloc      b
		shl 
		stloc      r1
		
		ldloc      a
		ldloc      b
		shl 
		stloc      r2
		
		ldloc      a
		ldloc      b
		shl 
		stloc      r3
		
		ldloc      r1
		ldc.i4.4
		ceq
		
		ldloc      r2
		ldc.i4.4
		ceq
		
		ldloc      r3
		ldc.i4.4
		ceq
		
		add
		add
		
		ret
	}
	
	.method static public int32 test_1_fceq_to_i4 () il managed {
	
		.locals init (
			float64	foo,
			int32   val
		)

		ldc.r8      2
		stloc       foo
		
		ldloc       foo
		ldc.r8      2
		ceq
		stloc       val
		
		ldloc       val
		ret
	}

	//
	// This should be manually checked. Basically under -O=linears,
	// you should not see tons of register spilling.
	//
	.method static public int32 test_1_bytedreg_free () il managed {
		.locals init (
			int32   foo
		)
		ldc.i4 500
		stloc foo
		ldloc foo
		ldc.i4 500
		ceq
		stloc foo
		
		ldloc foo
		ldc.i4 1
		ceq
		stloc foo

		ldloc foo
		ldc.i4 1
		ceq
		stloc foo

		ldloc foo
		ldc.i4 1
		ceq
		stloc foo
		
		ldloc foo
		ldc.i4 1
		ceq
		stloc foo
		
		ldloc foo
		ret
	}
	
	//
	// This should be manually checked. Basically under -O=linears,
	// you should not see tons of register spilling.
	//
	.method static public int32 test_0_bytesreg1_free () il managed {
		.locals init (
			unsigned int8	   dest,
			int32	           src,
			unsigned int8&	   pdest
		)

		ldloca     dest
		stloc      pdest
		
		ldloc      pdest
		ldloc      src
		stind.i1
		
		ldloc      pdest
		ldloc      src
		stind.i1
		
		ldloc      pdest
		ldloc      src
		stind.i1
		
		ldloc      pdest
		ldloc      src
		stind.i1
		
		ldloc      pdest
		ldloc      src
		stind.i1
		
		ldloc      pdest
		ldloc      src
		stind.i1
		
		ldloc      pdest
		ldind.i1
		ret
	}
	
	.method static public int32 test_1_shift_regvar () il managed {
	
		.locals init (
			int32   v7FFFFFFF,
			int32   v1
		)
		
		ldc.i4	0x7FFFFFFF
		stloc   v7FFFFFFF
	
		ldc.i4.1
		stloc v1
		
		ldloc   v7FFFFFFF
		ldloc   v1
		shl
		ldc.i4	0xFFFFFFFE
		ceq
		ret
	}
	
	// this only happens with the managed pointer, not an unmanaged one.
	.method static public int32 test_0_foo () il managed {
	
		.locals init (
			int32&	buf
		)
		
		ldc.i4.5
		localloc
		
		stloc buf
		ldloc buf
		
		ldind.i4
		
		ret
	}

	.method static public int32 test_0_localloc () cil managed {
		.locals init (native int, native int, native int, native int, int32)

		ldc.i4 6
		localloc
		conv.i
		stloc.0

		ldc.i4 6
		localloc
		conv.i
		stloc.1

		ldc.i4 6
		localloc
		conv.i
		stloc.2

		// Variable length
		ldc.i4 128
		stloc.s 4
		ldloc.s 4
		localloc
		conv.i
		stloc.3

		// Check zero initialized
		ldloc.0
		ldind.i4
		ldc.i4.0
		beq OK1
		ldc.i4.1
		br FAIL

OK1:
		ldloc.3
		ldind.i4
		ldc.i4.0
		beq OK2
		ldc.i4.2
		br FAIL

OK2:
		ldloc.3
		ldc.i4.s 124
		add
		ldind.i4
		ldc.i4.0
		beq OK3
		ldc.i4.3
		br FAIL

OK3:
		ldloc.1
		ldc.i4 999999
		stind.i4
		ldloc.1
		ldind.i4
		ldc.i4 999999
		beq OK4
		ldc.i4.4
		br FAIL

OK4:
		ldloc.0
		ldc.i4 999999
		stind.i4
		ldloc.0
		ldind.i4
		ldc.i4 999999
		beq PASS
		ldc.i4.5
		br FAIL

FAIL:
		ret

PASS:	ldc.i4.0
		ret
	}

    .method private static void do_localloc () cil managed {
        .maxstack 3
        .locals init (
                unsigned int8*  V_0)
        IL_0000:  ldc.i4.1
        IL_0001:  ldc.i4 131072
        IL_0006:  mul
        IL_0007:  localloc
        IL_0009:  stloc.0
        IL_000a:  ret
    }

	// Check that localloc can't be inlined
	.method static public int32 test_0_localloc_inline () cil managed {
		.maxstack 16
		.locals init (
			int32 i
		)

		ldc.i4.0
        stloc.0
        br COND

START:  call void class Tests::do_localloc()
        ldloc.0
        ldc.i4.1
        add
        stloc.0
COND:   ldloc.0
        ldc.i4 1000
        blt START

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

	.method public static int32 test_0_conv_ovf_i8_neg () il managed {
		.maxstack 16

		ldc.i4.m1
		conv.ovf.i8
		conv.i4
		ldc.i4.m1
		beq L_OK
		ldc.i4.1
		ret
	L_OK:
		ldc.i4.0
		ret
	}		

	.method public static int32 test_0_get_type_from_handle_on_bblock_boundary () cil managed 
	{
		.maxstack 16
	
		ldc.i4.1
		brfalse OBJECT
	
		ldtoken [mscorlib]System.String
		br AFTER
	OBJECT:
		ldtoken [mscorlib]System.Object
	AFTER:
		call class [mscorlib]'System.Type' class [mscorlib]'System.Type'::'GetTypeFromHandle'(valuetype [mscorlib]'System.RuntimeTypeHandle')
		callvirt instance string class [mscorlib]System.Type::get_FullName ()
		ldstr "System.String"
		callvirt instance bool class [mscorlib]System.Object::Equals(object)
		ldc.i4.0
		ceq
		ret 
	}
		
	.method public static int32 test_0_bug59580  ()
	{
	        ldc.r4          float32(0x7FC00000)
	        ldc.r4          float32(0x7FC00000)
        	bge.un          pass
	        br              fail
	pass:
        	ldc.i4.0
	        ret
	fail:
        	ldc.i4.1
	        ret
	}
	
	.method public static int32 test_1_bug60056  () {
		.locals init (int32 m5)
			
		ldc.i4.m1
		stloc.0
		
		ldc.i4.1
		conv.u8
		
		ldloc.0
		conv.i8
		mul
		
		ldc.i4.m1
		conv.i8
		ceq
		ret
	}
	
	.method public static int32 test_1_conv_u8_cfold  () {
		ldc.i4.m1
		conv.u8
		
		ldc.i8 0x00000000ffffffff
		
		ceq
		ret
	}
	
	.method public static int32 test_1_array_type_mismatch_ldelema  () {
		.locals init (int32 r)
		
			ldc.i4.1
			newarr string
			ldc.i4.0 
			ldelema string
			pop
		
		.try {
			ldc.i4.1
			newarr string
			ldc.i4.0 
			ldelema object
			pop
			
			leave end
		} catch [mscorlib]System.ArrayTypeMismatchException {
			pop
			ldc.i4.1
			stloc.0
			leave end
		}
	end:
		ldloc.0
		ret
	}
}
