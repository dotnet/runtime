#! /bin/sh

SED="sed"
if [ `which gsed 2> /dev/null` ] ; then 
	SED="gsed"
fi

TEST_NAME=$1
TEST_VALIDITY=$2
TEST_ARR=$3
TEST_IDX=$4
TEST_LD=$5


TEST_NAME=${TEST_VALIDITY}_${TEST_NAME}
TEST_FILE=${TEST_NAME}_generated.il
echo $TEST_FILE
$SED -e "s/VALIDITY/${TEST_VALIDITY}/g" -e "s/ARR/${TEST_ARR}/g" -e "s/IDX/${TEST_IDX}/g" -e "s/LD/${TEST_LD}/g" > $TEST_FILE <<//EOF

// VALIDITY CIL which breaks the ECMA-335 rules. 
// this CIL should fail verification by a conforming CLI verifier.

.assembly '${TEST_NAME}_generated'
{
  .hash algorithm 0x00008004
  .ver  0:0:0:0
}

.class ClassA extends [mscorlib]System.Object
{
}

.class ClassSubA extends ClassA
{
}

.class public auto ansi sealed MyStruct
	extends [mscorlib]System.ValueType
{
	.field public int32 foo
}

.class public auto ansi sealed ByteEnum
	extends [mscorlib]System.Enum
{
	.field  public specialname  rtspecialname  unsigned int8 value__

	.field public static literal valuetype ByteEnum V_0 = int8(0x00)
	.field public static literal valuetype ByteEnum V_1 = int8(0x01)
}

.class public auto ansi sealed ShortEnum
	extends [mscorlib]System.Enum
{
	.field  public specialname  rtspecialname  unsigned int16 value__

	.field public static literal valuetype ShortEnum V_0 = int16(0x00)
	.field public static literal valuetype ShortEnum V_1 = int16(0x01)
}

.class public auto ansi sealed IntEnum
	extends [mscorlib]System.Enum
{
	.field  public specialname  rtspecialname  unsigned int32 value__

	.field public static literal valuetype IntEnum V_0 = int32(0x00)
	.field public static literal valuetype IntEnum V_1 = int32(0x01)
}

.class public auto ansi sealed LongEnum
	extends [mscorlib]System.Enum
{
	.field  public specialname  rtspecialname  unsigned int64 value__

	.field public static literal valuetype LongEnum V_0 = int64(0x00)
	.field public static literal valuetype LongEnum V_1 = int64(0x01)
}

.class public auto ansi sealed NativeIntEnum
	extends [mscorlib]System.Enum
{
	.field  public specialname  rtspecialname native int value__

	.field public static literal valuetype NativeIntEnum V_0 = int32(0x00)
	.field public static literal valuetype NativeIntEnum V_1 = int32(0x01)
}


.method public static void foo() cil managed
{
	.maxstack 2
	ldc.i4.1
	ARR
	IDX
	LD // VALIDITY.
	pop
	ret
}

.method public static int32 Main() cil managed
{
	.maxstack 8
	.entrypoint
	.try {
		call void foo ()
		leave END
	} catch [mscorlib]System.NullReferenceException {
		pop 
		leave END

        }

END:	ldc.i4.0
	ret
}
//EOF
