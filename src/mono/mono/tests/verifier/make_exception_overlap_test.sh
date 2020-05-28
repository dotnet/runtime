#! /bin/sh

SED="sed"
if [ `which gsed 2> /dev/null` ] ; then 
	SED="gsed"
fi

TEST_NAME=$1
TEST_VALIDITY=$2
TEST_BLOCK_1=$3
TEST_BLOCK_2=$4
TEST_WITH_FILTER_BLOCK=$5
TEST_WITH_FINALLY_BLOCK=$6

if [ "$TEST_WITH_FILTER_BLOCK" = "yes" ]; then
	FILTER_BLOCK="
FILTER_BLOCK_3:
	nop

	ldloca 1
	volatile.
AFTER_PREFIX_3:
	unaligned. 1
	ldind.i4
	pop

FILTER_BLOCK_3_A:
	stloc.0	
	nop
	nop
	ldc.i4.0
	endfilter
FILTER_BLOCK_3_END:"
else
	FILTER_BLOCK="";
fi

if [ "$TEST_WITH_FINALLY_BLOCK" = "yes" ]; then
	FINALLY_BLOCK="
FINALLY_BLOCK_1:
	nop

	ldloca 1
	volatile.
AFTER_PREFIX_6:
	unaligned. 1
	ldind.i4
	pop

FINALLY_BLOCK_1_A:
	stloc.0	
	nop
	nop
	ldc.i4.0
	endfinally
FINALLY_BLOCK_1_END:"
else
	FINALLY_BLOCK="";
fi


TEST_FILE=${TEST_VALIDITY}_${TEST_NAME}_generated.il
echo $TEST_FILE

$SED -e "s/EXCEPTION_BLOCK_1/${TEST_BLOCK_1}/g" -e "s/EXCEPTION_BLOCK_2/${TEST_BLOCK_2}/g" > $TEST_FILE <<//EOF
// VALIDITY CIL

.assembly '${TEST_VALIDITY}_${TEST_NAME}_generated'
{
  .hash algorithm 0x00008004
  .ver  0:0:0:0
}

.method public static int32 Main() cil managed
{
        .entrypoint
        .maxstack 8
	.locals init (object _X0, int32 V0)
 
	ldloca 1
	volatile.
AFTER_PREFIX_1:
	unaligned. 1
	ldind.i4
	pop

	
TRY_BLOCK_1:
	nop
TRY_BLOCK_1_A:
	nop
	newobj instance void class [mscorlib]System.Exception::.ctor()
	throw
	nop
	leave END
TRY_BLOCK_1_END:

	ldloca 1
	volatile.
AFTER_PREFIX_4:
	unaligned. 1
	ldind.i4
	pop
	leave END


${FILTER_BLOCK}
${FINALLY_BLOCK}


CATCH_BLOCK_1:
	nop
CATCH_BLOCK_1_A:
	stloc.0	
	nop

	ldloca 1
	volatile.
AFTER_PREFIX_2:
	unaligned. 1
	ldind.i4
	pop

	nop
	leave END
CATCH_BLOCK_1_END:

	ldloca 1
	volatile.
AFTER_PREFIX_5:
	unaligned. 1
	ldind.i4
	pop
	leave END

TRY_BLOCK_2:
	nop
TRY_BLOCK_2_A:
	nop
	newobj instance void class [mscorlib]System.Exception::.ctor()
	throw
	nop
	leave END
TRY_BLOCK_2_END:


CATCH_BLOCK_2:
	nop
CATCH_BLOCK_2_A:
	stloc.0	
	nop
	nop
	leave END
CATCH_BLOCK_2_END:


END:
        ldc.i4.0
        ret

	EXCEPTION_BLOCK_1
	EXCEPTION_BLOCK_2
}
//EOF
