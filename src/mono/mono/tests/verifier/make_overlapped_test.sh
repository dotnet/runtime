#! /bin/sh

SED="sed"
if [ `which gsed 2> /dev/null` ] ; then 
	SED="gsed"
fi

TEST_NAME=$1
TEST_VALIDITY=$2
TEST_POS_0=$3
TEST_POS_1=$4
TEST_POS_2=$5
TEST_TYPE_0=$6

if [ "x$TEST_TYPE_0" = "x" ] ; then
	TEST_TYPE_0="int32";
fi

TEST_FILE=`echo ${TEST_VALIDITY}_${TEST_NAME} | $SED -e 's/ /_/g' -e 's/\./_/g' -e 's/&/mp/g' -e 's/\[/_/g' -e 's/\]/_/g'`_generated.il
echo $TEST_FILE
TEST_TYPE1=`echo $TEST_TYPE1 | $SED -s 's/&/\\\&/'`
$SED -e "s/VALIDITY/${TEST_VALIDITY}/g" -e "s/TYPE_0/${TEST_TYPE_0}/g"> $TEST_FILE <<//EOF

// VALIDITY CIL which breaks the ECMA-335 rules. 
// this CIL should fail verification by a conforming CLI verifier.

.assembly '${TEST_NAME}_generated'
{
  .hash algorithm 0x00008004
  .ver  0:0:0:0
}

.class explicit sealed Overlapped extends [mscorlib]System.ValueType
{
    .field [${TEST_POS_0}] public TYPE_0 intVal
    .field [${TEST_POS_1}] public object objVal
	.field [${TEST_POS_2}] public int32 intVal2

	.method public hidebysig  specialname  rtspecialname instance default void '.ctor' ()  cil managed 
	{
		.maxstack 8
		ret 
    }
}

.method public static int32 Main() cil managed
{
	.entrypoint
	.maxstack 2
	.locals init (Overlapped val)
	ldloca 0
	initobj Overlapped

	leave branch_target
branch_target:
	ldc.i4.0
	ret
}
//EOF
