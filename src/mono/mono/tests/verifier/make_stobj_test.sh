#! /bin/sh

SED="sed"
if [ `which gsed 2> /dev/null` ] ; then 
	SED="gsed"
fi

TEST_NAME=$1
TEST_VALIDITY=$2
TEST_TYPE1=$3
TEST_TYPE2=$4
TEST_TYPE3=$5
TEST_EXTRA_OPS=$6

ZZ=`echo $TEST_TYPE2 | grep "\&"`
T2_REF=$?

LOCAL_INIT="";
if [ $T2_REF -eq 0 ]; then
	T2_NO_REF=`echo $TEST_TYPE2 | cut -d '\' -f 1`
	INIT_LOCS=", $T2_NO_REF V_2"
	INIT_IL="ldloca.s 2\n\tstloc.1"
fi

TEST_FILE=`echo ${TEST_VALIDITY}_${TEST_NAME} | $SED -e 's/ /_/g' -e 's/\./_/g' -e 's/&/mp/g' -e 's/\[/_/g' -e 's/\]/_/g'`_generated.il
echo $TEST_FILE
$SED -e "s/EXTRA_OPS/${TEST_EXTRA_OPS}/g" -e "s/TYPE1/${TEST_TYPE1}/g" -e "s/VALIDITY/${TEST_VALIDITY}/g" -e "s/TYPE2/${TEST_TYPE2}/g"  -e "s/TYPE3/${TEST_TYPE3}/g"  -e "s/INIT_LOCS/${INIT_LOCS}/g"  -e "s/INIT_IL/${INIT_IL}/g"> $TEST_FILE <<//EOF

.assembly extern mscorlib
{
  .ver 2:0:0:0
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) // .z\V.4..
}

.assembly 'stobj_test'
{
  .hash algorithm 0x00008004
  .ver  0:0:0:0
}

.module ldobj.exe


.class Class extends [mscorlib]System.Object
{
    .field public int32 valid
}

.class public Template\`1<T>
  	extends [mscorlib]System.Object
{
}

.class sealed public StructTemplate\`1<T>
  	extends [mscorlib]System.ValueType
{
	.field public !0 t
}

.class sealed public StructTemplate2\`1<T>
  	extends [mscorlib]System.ValueType
{
	.field public !0 t
}


.class public auto ansi sealed MyStruct
  	extends [mscorlib]System.ValueType
{
	.field public int32 foo
}


.class public auto ansi sealed MyStruct2
  	extends [mscorlib]System.ValueType
{
	.field public int32 foo
}

.method public static int32 Main ()
{
	.entrypoint
	.maxstack 8
	.locals init (TYPE1 V_0, TYPE2 V_1 INIT_LOCS)

	INIT_IL

	ldloc.1
	ldloc.0

	EXTRA_OPS

	stobj TYPE3 // VALIDITY


	ldc.i4.0
	ret 
}

//EOF
