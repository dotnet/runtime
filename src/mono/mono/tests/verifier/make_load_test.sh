#! /bin/sh

TEST_NAME=$1
TEST_VALIDITY=$2
TEST_OP=$3
TEST_PARAMS=$4
TEST_LOCALS=$5

TEST_NAME=${TEST_VALIDITY}_${TEST_NAME}
TEST_FILE=${TEST_NAME}_generated.il
echo $TEST_FILE
TEST_PARAMS=`echo $TEST_PARAMS | sed -s 's/&/\\\&/'`
TEST_LOCALS=`echo $TEST_LOCALS | sed -s 's/&/\\\&/'`
sed -e "s/VALIDITY/${TEST_VALIDITY}/g" -e "s/PARAMS/${TEST_PARAMS}/g" -e "s/LOCALS/${TEST_LOCALS}/g" -e "s/OPCODE/${TEST_OP}/g" > $TEST_FILE <<//EOF

// VALIDITY CIL which breaks the ECMA-335 rules. 
// this CIL should fail verification by a conforming CLI verifier.

.assembly '${TEST_NAME}_generated'
{
  .hash algorithm 0x00008004
  .ver  0:0:0:0
}

.method public static int32 Main(PARAMS) cil managed
{
	.maxstack 2
	.entrypoint
	.locals init (
        LOCALS
    )

	OPCODE // VALIDITY.
	ret
}
//EOF