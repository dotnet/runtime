#! /bin/sh

SED="sed"
if [ `which gsed 2> /dev/null` ] ; then 
	SED="gsed"
fi
TEST_NAME=$1
TEST_VALIDITY=$2
TEST_OP=$3
TEST_TYPE1=$4
TEST_TYPE2=$5
TEST_INIT_EXP=$6
TEST_INIT_VAL=$7

if [ "$TEST_INIT_VAL" = "yes" ]; then
	TEST_INIT="$TEST_INIT_EXP\n\t\stloc.1"
else
	TEST_INIT=""
fi


TEST_NAME=${TEST_VALIDITY}_${TEST_NAME}
TEST_FILE=${TEST_NAME}_generated.il
echo $TEST_FILE
TEST_TYPE1=`echo $TEST_TYPE1 | $SED -s 's/&/\\\&/'`
TEST_TYPE2=`echo $TEST_TYPE2 | $SED -s 's/&/\\\&/'`
$SED -e "s/INIT/${TEST_INIT}/g" -e "s/VALIDITY/${TEST_VALIDITY}/g" -e "s/TYPE1/${TEST_TYPE1}/g" -e "s/TYPE2/${TEST_TYPE2}/g" -e "s/OPCODE/${TEST_OP}/g" > $TEST_FILE <<//EOF

// VALIDITY CIL which breaks the ECMA-335 rules. 
// this CIL should fail verification by a conforming CLI verifier.

.assembly '${TEST_NAME}_generated'
{
  .hash algorithm 0x00008004
  .ver  0:0:0:0
}

.method public static int32 Main() cil managed
{
	.entrypoint
	.maxstack 2
	.locals init (
		TYPE1 V_0,
		TYPE2 V_1)
	INIT
	ldloc.0
	ldloc.1
	OPCODE // VALIDITY.
	pop
	ldc.i4.0
	ret
}
//EOF
