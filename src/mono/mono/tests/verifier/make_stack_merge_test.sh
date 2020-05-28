#! /bin/sh

SED="sed"
if [ `which gsed 2> /dev/null` ] ; then 
	SED="gsed"
fi

TEST_NAME=$1
TEST_VALIDITY=$2
TEST_TYPE1=$3
TEST_TYPE2=$4

TEST_NAME=${TEST_VALIDITY}_${TEST_NAME}
TEST_FILE=${TEST_NAME}_generated.il
echo $TEST_FILE
TEST_TYPE1=`echo $TEST_TYPE1 | $SED -s 's/&/\\\&/'`
TEST_TYPE2=`echo $TEST_TYPE2 | $SED -s 's/&/\\\&/'`
$SED -e "s/VALIDITY/${TEST_VALIDITY}/g" -e "s/TYPE1/${TEST_TYPE1}/g" -e "s/TYPE2/${TEST_TYPE2}/g" > $TEST_FILE <<//EOF

// VALIDITY CIL which breaks the ECMA-335,III,1.8.1.3 rule. 
// this CIL should fail verification by a conforming CLI verifier.

.assembly '${TEST_NAME}_generated'
{
  .hash algorithm 0x00008004
  .ver  0:0:0:0
}

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
	/*we need a 'random' source of values so the conditional cannot be DCE and the stack merger ignored*/
	newobj instance void object::.ctor()
	callvirt instance int32 object::GetHashCode()
	beq.s branch_target
	pop
	ldloc.1
	branch_target: // VALIDITY, stacks cannot be merged.
	pop
	ldc.i4.0
	ret
}
//EOF