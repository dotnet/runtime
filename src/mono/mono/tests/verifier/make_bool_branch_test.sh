#! /bin/sh

SED="sed"
if [ `which gsed 2> /dev/null` ] ; then 
	SED="gsed"
fi

TEST_NAME=$1
TEST_VALIDITY=$2
TEST_OP=$3
TEST_TYPE1=$4
TEST_EXTRAS=$5

TEST_NAME=${TEST_VALIDITY}_${TEST_NAME}
TEST_FILE=${TEST_NAME}_generated.il
echo $TEST_FILE
TEST_TYPE1=`echo $TEST_TYPE1 | $SED -s 's/&/\\\&/'`
$SED -e "s/VALIDITY/${TEST_VALIDITY}/g" -e "s/TYPE1/${TEST_TYPE1}/g"  -e "s/OPCODE/${TEST_OP}/g"  -e "s/EXTRAS/${TEST_EXTRAS}/g" > $TEST_FILE <<//EOF

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
        .maxstack 1
        .locals init (
            TYPE1 V_0
        )
        ldloc.0

	EXTRAS

        OPCODE branch_target // VALIDITY.
        nop
        branch_target:
        ldc.i4.0
        ret
}
//EOF
