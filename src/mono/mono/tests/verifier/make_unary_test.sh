#! /bin/sh

TEST_NAME=$1
TEST_VALIDITY=$2
TEST_OP=$3
TEST_TYPE1=$4

TEST_FILE=`echo ${TEST_VALIDITY}_${TEST_NAME} | sed -e 's/ /_/g' -e 's/\./_/g' -e 's/&/mp/g' -e 's/\[/_/g' -e 's/\]/_/g'`_generated.il
echo $TEST_FILE
TEST_TYPE1=`echo $TEST_TYPE1 | sed -s 's/&/\\\&/'`
sed -e "s/OPCODE/${TEST_OP}/g" -e "s/VALIDITY/${TEST_VALIDITY}/g" -e "s/TYPE1/${TEST_TYPE1}/g" -e "s/LOAD_OPCODE/${TEST_LOAD_OP}/g" > $TEST_FILE <<//EOF

// VALIDITY CIL which breaks the ECMA-335 rules. 
// this CIL should fail verification by a conforming CLI verifier.

.class Class extends [mscorlib]System.Object
{
    .field public int32 valid
}

.class explicit Overlapped extends [mscorlib]System.ValueType
{
    .field [0] private int32 privateIntVal
    .field [0] public int32 publicIntVal
    .field [4] public int32 intVal
    .field [4] public object objVal
}

.method public static int32 Main() cil managed
{
	.entrypoint
	.maxstack 2
	.locals init (
                TYPE1 V_0)
	ldloc.0
	OPCODE // VALIDITY.
	branch_target:
	ldc.i4.0
	ret
}
//EOF