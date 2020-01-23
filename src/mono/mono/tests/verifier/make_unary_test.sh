#! /bin/sh

SED="sed"
if [ `which gsed 2> /dev/null` ] ; then 
	SED="gsed"
fi

TEST_NAME=$1
TEST_VALIDITY=$2
TEST_OP=$3
TEST_TYPE1=$4

TEST_FILE=`echo ${TEST_VALIDITY}_${TEST_NAME} | $SED -e 's/ /_/g' -e 's/\./_/g' -e 's/&/mp/g' -e 's/\[/_/g' -e 's/\]/_/g'`_generated.il
echo $TEST_FILE
TEST_TYPE1=`echo $TEST_TYPE1 | $SED -s 's/&/\\\&/'`
$SED -e "s/OPCODE/${TEST_OP}/g" -e "s/VALIDITY/${TEST_VALIDITY}/g" -e "s/TYPE1/${TEST_TYPE1}/g" -e "s/LOAD_OPCODE/${TEST_LOAD_OP}/g" > $TEST_FILE <<//EOF

// VALIDITY CIL which breaks the ECMA-335 rules. 
// this CIL should fail verification by a conforming CLI verifier.

.assembly '${TEST_NAME}_generated'
{
  .hash algorithm 0x00008004
  .ver  0:0:0:0
}

.class Class extends [mscorlib]System.Object
{
    .field public int32 valid
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

.class public auto ansi sealed Int8Enum
	extends [mscorlib]System.Enum
{
	.field public specialname rtspecialname int8 value__
	.field public static literal valuetype Int8Enum A = int8(0x00000000)
}

.class explicit sealed Overlapped extends [mscorlib]System.ValueType
{
    .field [0] private int32 privateIntVal
    .field [0] public int32 publicIntVal
    .field [4] public int32 intVal
}

.method public static int32 Main() cil managed
{
	.entrypoint
	.maxstack 2
	.locals init (
                TYPE1 V_0)
	ldloc.0
	OPCODE // VALIDITY.

	leave branch_target
branch_target:
	ldc.i4.0
	ret
}
//EOF
