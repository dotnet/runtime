#! /bin/sh

SED="sed"
if [ `which gsed 2> /dev/null` ] ; then 
	SED="gsed"
fi

TEST_NAME=$1
TEST_VALIDITY=$2
TEST_OP=$3
TEST_TYPE1=$4
TEST_TYPE2=$5
TEST_CREATE_FIELD=$6

TEST_NAME=${TEST_VALIDITY}_${TEST_NAME}
TEST_FILE=${TEST_NAME}_generated.il
echo $TEST_FILE
TEST_TYPE1=`echo $TEST_TYPE1 | $SED -s 's/&/\\\&/'`
TEST_TYPE2=`echo $TEST_TYPE2 | $SED -s 's/&/\\\&/'`

if [ "$TEST_CREATE_FIELD" = "no" ]; then
	CLASS_FIELDS="";
else
	CLASS_FIELDS=".field public ${TEST_TYPE1} fld\n	.field public static ${TEST_TYPE1} sfld";
fi

$SED -e "s/VALIDITY/${TEST_VALIDITY}/g" -e "s/CLASS_FIELDS/${CLASS_FIELDS}/g" -e "s/OPCODE/${TEST_OP}/g" -e "s/TYPE1/${TEST_TYPE1}/g" -e "s/TYPE2/${TEST_TYPE2}/g" > $TEST_FILE <<//EOF

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
	CLASS_FIELDS

	.method public hidebysig specialname rtspecialname instance default void .ctor () cil managed
	{
		.maxstack 8
		ldarg.0 
		call instance void object::.ctor()
		ret
	}

    .method public void Method(TYPE1) cil managed
    {
	ret
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
	newobj instance void class Class::.ctor()
	ldloc.1
	OPCODE // VALIDITY, TYPE2 cannot be stored in TYPE1.
	ldc.i4.0
	ret
}
//EOF
