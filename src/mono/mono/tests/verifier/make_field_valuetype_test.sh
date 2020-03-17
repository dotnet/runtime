#! /bin/sh

SED="sed"
if [ `which gsed 2> /dev/null` ] ; then 
	SED="gsed"
fi

TEST_NAME=$1
TEST_VALIDITY=$2
TEST_OP=$3
TEST_LOAD=$4

TEST_NAME=${TEST_VALIDITY}_${TEST_NAME}
TEST_FILE=${TEST_NAME}_generated.il
echo $TEST_FILE
$SED -e "s/VALIDITY/${TEST_VALIDITY}/g"  -e "s/LOAD/${TEST_LOAD}/g"  -e "s/OPCODE/${TEST_OP}/g" > $TEST_FILE <<//EOF

.assembly '${TEST_NAME}_generated'
{
  .hash algorithm 0x00008004
  .ver  0:0:0:0
}

// VALIDITY CIL which breaks the ECMA-335 rules. 
// this CIL should fail verification by a conforming CLI verifier.

.assembly extern mscorlib
{
  .ver 1:0:5000:0
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) // .z\V.4..
}

.class sealed MyValueType extends [mscorlib]System.ValueType
{
	.field public int32 fld
}

.method public static int32 Main() cil managed
{
	.entrypoint
	.maxstack 2
	.locals init (
		MyValueType VAR1,
		MyValueType& VAR2
	)
	ldloca.s 0
    initobj MyValueType
	ldloca.s 0
	stloc.1
	LOAD
	OPCODE // VALIDITY.
	pop
	ldc.i4.0
	ret
}
//EOF