#! /bin/sh

SED="sed"
if [ `which gsed 2> /dev/null` ] ; then 
	SED="gsed"
fi

TEST_NAME=$1
TEST_VALIDITY=$2
TEST_TYPE1=$3

TEST_FILE=`echo ${TEST_VALIDITY}_${TEST_NAME} | $SED -e 's/ /_/g' -e 's/\./_/g' -e 's/&/mp/g' -e 's/\[/_/g' -e 's/\]/_/g'`_generated.il
echo $TEST_FILE
TEST_TYPE1=`echo $TEST_TYPE1 | $SED -s 's/&/\\\&/'`

$SED -e "s/OPCODE/${TEST_OP}/g" -e "s/VALIDITY/${TEST_VALIDITY}/g" -e "s/TYPE1/${TEST_TYPE1}/g" > $TEST_FILE <<//EOF

// VALIDITY CIL which breaks the ECMA-335 rules. 
// this CIL should fail verification by a conforming CLI verifier.

.assembly '${TEST_NAME}_generated'
{
  .hash algorithm 0x00008004
  .ver  0:0:0:0
}

.class public Template\`1<T>
  	extends [mscorlib]System.Object
{
}

.class public auto ansi sealed MyStruct
  	extends [mscorlib]System.ValueType
{
    .field public int32 valid
}

.method public static int32 Main() cil managed
{
	.entrypoint
	.maxstack 2
	.locals init (int32 V_0)

	sizeof TYPE1
	stloc.0

	ldc.i4.0
	ret
}
//EOF
