#! /bin/sh

SED="sed"
if [ `which gsed 2> /dev/null` ] ; then 
	SED="gsed"
fi

TEST_NAME=$1
TEST_VALIDITY=$2
TEST_TYPE1=$3
TEST_TYPE2=$4
TEST_POST_OP=$5
TEST_INIT=$6

TEST_FILE=`echo ${TEST_VALIDITY}_${TEST_NAME} | $SED -e 's/ /_/g' -e 's/\./_/g' -e 's/&/mp/g' -e 's/\[/_/g' -e 's/\]/_/g'`_generated.il
echo $TEST_FILE
$SED -e "s/INIT_OP/${TEST_INIT}/g"  -e "s/TYPE1/${TEST_TYPE1}/g" -e "s/VALIDITY/${TEST_VALIDITY}/g" -e "s/TYPE2/${TEST_TYPE2}/g"  -e "s/POST_OP/${TEST_POST_OP}/g"> $TEST_FILE <<//EOF

.assembly extern mscorlib
{
  .ver 2:0:0:0
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) // .z\V.4..
}

.assembly 'ldobj_test'
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


.class public auto ansi sealed MyEnum
  	extends [mscorlib]System.Enum
{
    .field  public specialname  rtspecialname  int32 value__
    .field public static  literal  valuetype MyEnum B = int32(0x00000000)
    .field public static  literal  valuetype MyEnum C = int32(0x00000001)
}

.method public static int32 Main ()
{
	.entrypoint
	.maxstack 8
	.locals init (TYPE1 V_0, TYPE2 V_1)

	.try {
		INIT_OP
		stloc.0

		ldloc.0

		unbox.any TYPE2 // VALIDITY
		POST_OP
		leave END
	} catch [mscorlib]System.NullReferenceException {
		pop
		leave END
	}

END:
	ldc.i4.0
	ret 
}

//EOF
