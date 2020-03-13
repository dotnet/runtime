#! /bin/sh

SED="sed"
if [ `which gsed 2> /dev/null` ] ; then 
	SED="gsed"
fi

TEST_NAME=$1
TEST_VALIDITY=$2
TEST_ARR=$3
TEST_IDX=$4
TEST_TOKEN=$5


TEST_NAME=${TEST_VALIDITY}_${TEST_NAME}
TEST_FILE=${TEST_NAME}_generated.il
echo $TEST_FILE
$SED -e "s/VALIDITY/${TEST_VALIDITY}/g" -e "s/ARR/${TEST_ARR}/g" -e "s/IDX/${TEST_IDX}/g" -e "s/TOKEN/${TEST_TOKEN}/g" > $TEST_FILE <<//EOF

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


.method public static void foo() cil managed
{
	.maxstack 2
	ldc.i4.1
	ARR
	IDX
	ldelema TOKEN // VALIDITY.
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
