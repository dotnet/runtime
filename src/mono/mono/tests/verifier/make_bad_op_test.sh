#! /bin/sh

TEST_NAME=$1
TEST_VALIDITY=$2
TEST_BYTE_0=$3
TEST_BYTE_1=$4


if [ "$TEST_BYTE_1" == "" ] ; then
	TEST_BYTE_1="0";
fi

TEST_FILE=`echo ${TEST_VALIDITY}_${TEST_NAME} | sed -e 's/ /_/g' -e 's/\./_/g' -e 's/&/mp/g' -e 's/\[/_/g' -e 's/\]/_/g'`_generated.il
echo $TEST_FILE
sed -e "s/VALIDITY/${TEST_VALIDITY}/g" -e "s/BYTE_0/${TEST_BYTE_0}/g" -e "s/BYTE_1/${TEST_BYTE_1}/g" > $TEST_FILE <<//EOF

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
	.locals init ()

	.emitbyte BYTE_0
	.emitbyte BYTE_1

	leave end
end:
	ldc.i4.0
	ret
}


//EOF
