#! /bin/sh

SED="sed"
if [ `which gsed 2> /dev/null` ] ; then 
	SED="gsed"
fi

TEST_NAME=$1
TEST_VALIDITY=$2
TEST_OP=$3
TEST_TYPE1=$4
TEST_TYPE2=$5

T1_VAL=`echo $TEST_TYPE1 | $SED -s 's/\\\&//g'`

TEST_FILE=`echo ${TEST_VALIDITY}_${TEST_NAME}_generated.il`
echo $TEST_FILE

$SED -e "s/TYPE1/${TEST_TYPE1}/g" -e "s/T1_VAL/${T1_VAL}/g" -e "s/TYPE2/${TEST_TYPE2}/g"  -e "s/OP/${TEST_OP}/g" > $TEST_FILE <<//EOF

.assembly extern mscorlib
{
  .ver 2:0:0:0
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) // .z\V.4..
}

.assembly 'load_indirect_test'
{
  .hash algorithm 0x00008004
  .ver  0:0:0:0
}

.module load_indirect_test.exe

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
	.field public int32 foo
}


.method public static int32 Main ()
{
	.entrypoint
	.maxstack 8
	.locals init (TYPE1 V_0, TYPE2 V_1, T1_VAL V_2)
	ldloca 2
	stloc.0
	ldloc.0
	ldloc.1
	OP
	ldc.i4.0
	ret 
}

//EOF
