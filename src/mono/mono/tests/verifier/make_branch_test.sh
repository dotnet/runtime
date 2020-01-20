#! /bin/sh

SED="sed"
if [ `which gsed 2> /dev/null` ] ; then 
	SED="gsed"
fi

TEST_NAME=$1
TEST_VALIDITY=$2
TEST_POS=$3
TEST_OP=$4
TEST_FIN=$5

if [ "x$TEST_FIN" = "x" ]; then
	TEST_FIN="finally";
fi

declare BRANCH_${TEST_POS}="$TEST_OP"

TEST_NAME=${TEST_VALIDITY}_${TEST_NAME}
TEST_FILE=${TEST_NAME}_generated.il
echo $TEST_FILE

$SED -e "s/VALIDITY/${TEST_VALIDITY}/g" -e "s/OPCODE/${TEST_OP}/g" -e "s/FINALLY/${TEST_FIN}/g" > $TEST_FILE <<//EOF

// VALIDITY CIL which breaks the ECMA-335 rules. 
// this CIL should fail verification by a conforming CLI verifier.

.assembly '${TEST_NAME}_generated'
{
  .hash algorithm 0x00008004
  .ver  0:0:0:0
}

.class ClassA
	extends [mscorlib]System.Object
{
}

.class sealed MyValueType
	extends [mscorlib]System.ValueType
{
	.field private int32 v
}

.class public Template\`1<T>
  	extends [mscorlib]System.Object
{
}

.class sealed public ValueTypeTemplate\`1<T>
  	extends [mscorlib]System.ValueType
{
	.field private int32 v
}

.method public static int32 Main() cil managed
{
        .entrypoint
        .maxstack 8
	.locals init ( int32 BLA)
 
	${BRANCH_1}
        ldc.i4 0x7FFFFFFF
	pop

	ldloca 0
	unaligned. 1
AFTER_FIRST_PREFIX:
	volatile.
AFTER_SECOND_PREFIX:
	ldind.i4
	pop


BLOCK_1:
	nop
	.try {
		nop
		${BRANCH_2}
		nop
IN_TRY:
BLOCK_2:
		leave END
	} catch [mscorlib]System.Exception {
		pop
		${BRANCH_3}
		nop
IN_CATCH:
BLOCK_3:
		nop
		leave END
	}

	.try {
		leave END
	} FINALLY {
		nop
		${BRANCH_4}
		nop
IN_FINALLY:
BLOCK_4:
		nop
		endfinally
	}

	.try {
		leave END
	} filter {
		pop
		nop
		${BRANCH_5}
		nop
IN_FILTER:
BLOCK_5:
		nop
		ldc.i4.0
		endfilter
	}
	{
		pop
		nop
		${BRANCH_6}
		nop
IN_HANDLER:
BLOCK_6:
		nop
		leave END
	}

END:
        ldc.i4.0
        ret
}
//EOF
