#! /bin/sh

SED="sed"
if [ `which gsed 2> /dev/null` ] ; then 
	SED="gsed"
fi

TEST_OP=$1
TEST_TYPE=$2

TEST_OP_SHORT=`echo $TEST_OP | cut -d " " -f 1`
TEST_FILE=`echo invalid_stack_1_${TEST_OP_SHORT}_${TEST_TYPE} | $SED -e "s/ /_/g" -e "s/\./_/g" -e "s/&/mp/g"`_generated.il
echo $TEST_FILE
$SED -e "s/OPCODE/${TEST_OP}/g" -e "s/TYPE/${TEST_TYPE}/g" > $TEST_FILE <<//EOF
// invalid CIL which breaks the ECMA-335 rules.
// This CIL should fail verification by a conforming CLI verifier.

.assembly '${TEST_NAME}_generated'
{
  .hash algorithm 0x00008004
  .ver  0:0:0:0
}

.class Class extends [mscorlib]System.Object
{
    .field public int32 fld
}

.method public static int32 Main() cil managed
{
	.entrypoint
	.maxstack 1
	.locals init (
	    TYPE V_0
	)
	ldloc.0
	OPCODE // invalid, only 1 value on stack.
	branch_target:
	pop
	ldc.i4.0
	ret
}
//EOF