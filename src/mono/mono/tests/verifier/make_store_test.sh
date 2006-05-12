#! /bin/sh

TEST_NAME=$1
TEST_VALIDITY=$2
TEST_OP=$3
TEST_TYPE1=$4
TEST_TYPE2=$5

TEST_NAME=${TEST_VALIDITY}_${TEST_NAME}
TEST_FILE=${TEST_NAME}_generated.cil
echo $TEST_FILE
TEST_TYPE1=`echo $TEST_TYPE1 | sed -s 's/&/\\\&/'`
TEST_TYPE2=`echo $TEST_TYPE2 | sed -s 's/&/\\\&/'`
sed -e "s/VALIDITY/${TEST_VALIDITY}/g" -e "s/TYPE1/${TEST_TYPE1}/g" -e "s/TYPE2/${TEST_TYPE2}/g" -e "s/OPCODE/${TEST_OP}/g" > $TEST_FILE <<//EOF

// VALIDITY CIL which breaks the ECMA-335 rules. 
// this CIL should fail verification by a conforming CLI verifier.

.class ClassA extends [mscorlib]System.Object
{
}

.class ClassB extends [mscorlib]System.Object
{
}

.class interface InterfaceA
{
}

.class interface InterfaceB
{
}

.class nested ValueType extends [mscorlib]System.ValueType
{
}

.class nested ValueTypeSubType extends ValueType
{
}

.method public static int32 f(TYPE1 arg)
{
	.maxstack 2
	.locals init (
	    TYPE1 V_0,
	    TYPE2 V_1
	)
	ldloc.1
	OPCODE // VALIDITY, TYPE2& cannot be stored in TYPE1.
	ldc.i4.0
	ret
}

.method public static int32 Main() cil managed
{
	.entrypoint
	.maxstack 2
	.locals init (
	    TYPE1 V_0
	)
	ldloc.0
	call int32 f(TYPE1)
	ret
}
//EOF