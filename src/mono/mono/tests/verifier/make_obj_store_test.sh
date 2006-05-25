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
sed -e "s/VALIDITY/${TEST_VALIDITY}/g"  -e "s/OPCODE/${TEST_OP}/g" -e "s/TYPE1/${TEST_TYPE1}/g" -e "s/TYPE2/${TEST_TYPE2}/g" > $TEST_FILE <<//EOF

.assembly '${TEST_NAME}_generated'
{
  .hash algorithm 0x00008004
  .ver  0:0:0:0
}

// VALIDITY CIL which breaks the ECMA-335 rules. 
// this CIL should fail verification by a conforming CLI verifier.

.assembly extern mscorlib
{
  .ver 1:0:5000:0
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) // .z\V.4..
}

.class ClassA extends [mscorlib]System.Object
{
}

.class ClassB extends [mscorlib]System.Object
{
}

.class interface abstract InterfaceA
{
}

.class interface abstract InterfaceB
{
}

.class sealed ValueType extends [mscorlib]System.ValueType
{
	.field private int32 v
}

.class Class extends [mscorlib]System.Object
{
    .field public TYPE1 fld

    .method public void Method(TYPE1) cil managed
    {
	nop
    }
}

.method public static int32 Main() cil managed
{
	.entrypoint
	.maxstack 2
	.locals init (
		class Class V_0,
		TYPE2 V_1
	)
	ldloc.0
	ldloc.1
	OPCODE // VALIDITY, TYPE2 cannot be stored in TYPE1.
	ldc.i4.0
	ret
}
//EOF