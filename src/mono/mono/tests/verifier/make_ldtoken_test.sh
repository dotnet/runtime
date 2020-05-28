#! /bin/sh

SED="sed"
if [ `which gsed 2> /dev/null` ] ; then 
	SED="gsed"
fi

TEST_NAME=$1
TEST_VALIDITY=$2
TEST_OP=$3
TEST_CALL_OP=$4

TEST_FILE=`echo ${TEST_VALIDITY}_${TEST_NAME} | $SED -e 's/ /_/g' -e 's/\./_/g' -e 's/&/mp/g' -e 's/\[/_/g' -e 's/\]/_/g'`_generated.il
echo $TEST_FILE
TEST_TYPE1=`echo $TEST_TYPE1 | $SED -s 's/&/\\\&/'`
$SED -e "s/OPCODE/${TEST_OP}/g" -e "s/VALIDITY/${TEST_VALIDITY}/g" -e "s/CALL_OP/${TEST_CALL_OP}/g" > $TEST_FILE <<//EOF

.assembly extern mscorlib
{
  .ver 2:0:0:0
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) // .z\V.4..
}
.assembly 'bla'
{
  .hash algorithm 0x00008004
  .ver  0:0:0:0
}
.module ldtoken_type.exe

.class public auto ansi beforefieldinit Example
        extends [mscorlib]System.Object
{
	.field public int32 fld
	.method public static void Method ()
	{
		ret
	}


	.method public static int32 Main ()
	{
		.entrypoint
		.maxstack 8
		OPCODE // VALIDITY
		CALL_OP
		pop
		ldc.i4.0
		ret 
	}

}


 


//EOF
