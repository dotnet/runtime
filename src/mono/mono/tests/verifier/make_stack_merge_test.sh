#! /bin/sh

TEST_NAME=$1
TEST_VALIDITY=$2
TEST_TYPE1=$3
TEST_TYPE2=$4

TEST_NAME=${TEST_VALIDITY}_${TEST_NAME}
TEST_FILE=${TEST_NAME}_generated.cil
echo $TEST_FILE
TEST_TYPE1=`echo $TEST_TYPE1 | sed -s 's/&/\\\&/'`
TEST_TYPE2=`echo $TEST_TYPE2 | sed -s 's/&/\\\&/'`
sed -e "s/VALIDITY/${TEST_VALIDITY}/g" -e "s/TYPE1/${TEST_TYPE1}/g" -e "s/TYPE2/${TEST_TYPE2}/g" > $TEST_FILE <<//EOF

// VALIDITY CIL which breaks the ECMA-335,III,1.8.1.3 rule. 
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

.class ValueType extends [mscorlib]System.ValueType
{
}

.class ValueTypeSubType extends ValueType
{
}

.class Class extends [mscorlib]System.Object
{
}

.method public static int32 Main() cil managed
{
	.entrypoint
	.maxstack 3
	.locals init (
		TYPE1 V_0,
		TYPE2 V_1)
	ldloc.0
	ldc.i4.0
	ldc.i4.0
	beq.s branch_target
	pop
	ldloc.1
	branch_target: // VALIDITY, stacks cannot be merged.
	pop
	ldc.i4.0
	ret
}
//EOF