#! /bin/sh

SED="sed"
if [ `which gsed 2> /dev/null` ] ; then 
	SED="gsed"
fi

TEST_NAME=$1
TEST_VALIDITY=$2
TEST_BYTE_0=$3
TEST_BYTE_1=$4
TEST_BYTE_2=$5
TEST_BYTE_3=$6
TEST_BYTE_4=$7


if [ "$TEST_BYTE_1" != "" ] ; then
	EMIT_BYTE_1=".emitbyte $TEST_BYTE_1";
fi

if [ "$TEST_BYTE_2" != "" ] ; then
	EMIT_BYTE_2=".emitbyte $TEST_BYTE_2";
fi

if [ "$TEST_BYTE_3" != "" ] ; then
	EMIT_BYTE_3=".emitbyte $TEST_BYTE_3";
fi

if [ "$TEST_BYTE_4" != "" ] ; then
	EMIT_BYTE_4=".emitbyte $TEST_BYTE_4";
fi

if [ "$TEST_BYTE_5" != "" ] ; then
	EMIT_BYTE_5=".emitbyte $TEST_BYTE_5";
fi

TEST_FILE=`echo ${TEST_VALIDITY}_${TEST_NAME} | $SED -e 's/ /_/g' -e 's/\./_/g' -e 's/&/mp/g' -e 's/\[/_/g' -e 's/\]/_/g'`_generated.il
echo $TEST_FILE
$SED -e "s/VALIDITY/${TEST_VALIDITY}/g" -e "s/BYTE_0/${TEST_BYTE_0}/g" -e "s/BYTE_1/${TEST_BYTE_1}/g" > $TEST_FILE <<//EOF

// VALIDITY CIL which breaks the ECMA-335 rules. 
// this CIL should fail verification by a conforming CLI verifier.

.assembly '${TEST_NAME}_generated'
{
  .hash algorithm 0x00008004
  .ver  0:0:0:0
}

.method public static void Main() cil managed
{
	.entrypoint
	.maxstack 2
	.locals init ()

	nop
	nop
	.emitbyte BYTE_0
	${EMIT_BYTE_1}
	${EMIT_BYTE_2}
	${EMIT_BYTE_3}
	${EMIT_BYTE_4}
	${EMIT_BYTE_5}
}


//EOF
