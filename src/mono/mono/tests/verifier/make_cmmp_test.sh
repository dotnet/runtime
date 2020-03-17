#! /bin/sh

SED="sed"
if [ `which gsed 2> /dev/null` ] ; then 
	SED="gsed"
fi

TEST_NAME=$1
TEST_VALIDITY=$2
TEST_OP=$3
TEST_LOAD_OP=$4
TEST_TYPE=$5
TEST_BEFORE_OP=$6

echo $TEST_OP | grep unbox > /dev/null;

if [ $? -eq 0 ]; then
	TEST_CODE="
	ldloc.0
	box $TEST_TYPE";
else
	TEST_CODE="
	ldc.i4.1
	newarr $TEST_TYPE
	ldc.i4.0";
fi


TEST_FILE=`echo ${TEST_VALIDITY}_${TEST_NAME} | $SED -e 's/ /_/g' -e 's/\./_/g' -e 's/&/mp/g' -e 's/\[/_/g' -e 's/\]/_/g'`_generated.il
echo $TEST_FILE
$SED  -e "s/TYPE/${TEST_TYPE}/g" -e "s/OPCODE/${TEST_OP}/g"  -e "s/BEFORE_OP/${TEST_BEFORE_OP}/g" -e "s/LOAD_OP/${TEST_LOAD_OP}/g"> $TEST_FILE <<//EOF

.assembly extern mscorlib
{
  .ver 2:0:0:0
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) // .z\V.4..
}

.assembly 'cmmp_test'
{
  .hash algorithm 0x00008004
  .ver  0:0:0:0
}

.module cmmp.exe


.class ClassA extends [mscorlib]System.Object
{
    .field public int32 valid

	.method public hidebysig  specialname  rtspecialname instance default void .ctor (int32&)  cil managed 
	{
		.maxstack 8
		ldarg.0 
		call instance void object::.ctor()
		ret 
	}

	.method public virtual void VirtTest (ClassA& arg)
	{
		ret
	}
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
	.field public native int ptr

	.method public static void Test (MyStruct& arg)
	{
		ret
	}

}


.class public auto ansi sealed MyEnum
  	extends [mscorlib]System.Enum
{
    .field public specialname  rtspecialname  int32 value__
    .field public static  literal  valuetype MyEnum B = int32(0x00000000)
    .field public static  literal  valuetype MyEnum C = int32(0x00000001)
}

.method public static int32 Main ()
{
	.entrypoint
	.maxstack 8
	.locals init (TYPE V_0)

	BEFORE_OP

	${TEST_CODE}

	OPCODE

	LOAD_OP

	leave END
END:
	ldc.i4.0
	ret 
}

//EOF
