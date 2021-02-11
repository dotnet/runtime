#! /bin/sh

SED="sed"
if [ `which gsed 2> /dev/null` ] ; then 
	SED="gsed"
fi

TEST_NAME=$1
TEST_VALIDITY=$2
TEST_OP=$3
TEST_PARAMS=$4
TEST_LOCALS=$5
TEST_ARGS=$6
TEST_SIG=$7

TEST_NAME=${TEST_VALIDITY}_${TEST_NAME}
TEST_FILE=${TEST_NAME}_generated.il
echo $TEST_FILE
TEST_PARAMS=`echo $TEST_PARAMS | $SED -s 's/&/\\\&/'`
TEST_LOCALS=`echo $TEST_LOCALS | $SED -s 's/&/\\\&/'`
$SED -e "s/VALIDITY/${TEST_VALIDITY}/g" -e "s/PARAMS/${TEST_PARAMS}/g" -e "s/LOCALS/${TEST_LOCALS}/g" -e "s/OPCODE/${TEST_OP}/g" -e "s/ARGS/${TEST_ARGS}/g" -e "s/SIG/${TEST_SIG}/g" > $TEST_FILE <<//EOF

// VALIDITY CIL which breaks the ECMA-335 rules. 
// this CIL should fail verification by a conforming CLI verifier.

.assembly '${TEST_NAME}_generated'
{
  .hash algorithm 0x00008004
  .ver  0:0:0:0
}

.method public static void foo(PARAMS) cil managed
{
	.maxstack 2
	.locals init (
        LOCALS
    )

	OPCODE // VALIDITY.
	pop
	ret

}

.method public static int32 Main() cil managed
{
	.maxstack 8
	.entrypoint
	ARGS
	call void foo(SIG)
	ldc.i4.0
	ret
}
//EOF