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
TEST_RET_TYPE=$6

if [ "x$TEST_RET_TYPE" = "x" ]; then
	TEST_RET_TYPE="void"
else
	LD_RET_CODE="ldloc.0"
fi

TEST_FILE=`echo ${TEST_VALIDITY}_${TEST_NAME} | $SED -e 's/ /_/g' -e 's/\./_/g' -e 's/&/mp/g' -e 's/\[/_/g' -e 's/\]/_/g'`_generated.il
echo $TEST_FILE
$SED  -e "s/RET_TYPE/${TEST_RET_TYPE}/g" -e "s/TYPE/${TEST_TYPE}/g" -e "s/OPCODE/${TEST_OP}/g"  -e "s/BEFORE_OP/${TEST_BEFORE_OP}/g" -e "s/LOAD_OP/${TEST_LOAD_OP}/g"> $TEST_FILE <<//EOF

.assembly extern mscorlib
{
  .ver 2:0:0:0
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) // .z\V.4..
}

.assembly 'prefix_test'
{
  .hash algorithm 0x00008004
  .ver  0:0:0:0
}

.module cmmp.exe


.class ClassA extends [mscorlib]System.Object
{
    .field public int32 valid

	.method public hidebysig  specialname  rtspecialname instance default void .ctor ()  cil managed 
	{
		.maxstack 8
		ldarg.0 
		call instance void object::.ctor()
		ret 
	}

	.method public virtual void VirtTest ()
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
	.field public static int32 stFoo
	.field public native int ptr

	.method public static string StrTest ()
	{
		ldstr "oi"
		ret
	}

	.method public static void Test ()
	{
		ret
	}

	.method public static void Test (int32&)
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

.method public static RET_TYPE TestMethod ()
{
	.maxstack 8
	.locals init (TYPE V_0)

	LOAD_OP

	tail.
MIDDLE:
	OPCODE
AFTER:
	ret
	leave END
END:
	$LD_RET_CODE
	ret 
}


.method public static int32 Main ()
{
	.entrypoint
	.maxstack 8
	.locals init ()

	call RET_TYPE TestMethod()

	leave END
END:
	ldc.i4.0
	ret 
}

//EOF
