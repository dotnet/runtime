#! /bin/sh

SED="sed"
if [ `which gsed 2> /dev/null` ] ; then 
	SED="gsed"
fi

TEST_NAME=$1
TEST_VALIDITY=$2
TEST_TYPE1=$3
TEST_TYPE2=$4
TEST_EXTRA_OP=$5
TEST_POST_OP=$5

TEST_FILE=`echo ${TEST_VALIDITY}_${TEST_NAME} | $SED -e 's/ /_/g' -e 's/\./_/g' -e 's/&/mp/g' -e 's/\[/_/g' -e 's/\]/_/g'`_generated.il
echo $TEST_FILE

$SED -e "s/TYPE1/${TEST_TYPE1}/g" -e "s/VALIDITY/${TEST_VALIDITY}/g" -e "s/TYPE2/${TEST_TYPE2}/g" -e "s/EXTRA_OP/${TEST_EXTRA_OP}/g" -e "s/POST_OP/${TEST_POST_OP}/g" > $TEST_FILE <<//EOF

.assembly extern mscorlib
{
  .ver 2:0:0:0
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) // .z\V.4..
}

.assembly 'newarr_test'
{
  .hash algorithm 0x00008004
  .ver  0:0:0:0
}

.module newarr.exe


.class Class extends [mscorlib]System.Object
{
    .field public int32 valid
}


.method public static int32 Main ()
{
	.entrypoint
	.maxstack 8
	.locals init (TYPE1 V_0)
	ldloc.0
	EXTRA_OP
	newarr TYPE2
	POST_OP
	pop
	ldc.i4.0
	ret 
}

//EOF
