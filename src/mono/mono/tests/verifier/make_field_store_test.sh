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
TEST_EMIT_CSTOR=$6
if [ "${TEST_EMIT_CSTOR}" = "yes" ]; then
	TEST_CSTOR="newobj instance void ${TEST_TYPE2}::.ctor()";
else
	TEST_CSTOR="ldloc.0";
fi

TEST_NAME=${TEST_VALIDITY}_${TEST_NAME}
TEST_FILE=${TEST_NAME}_generated.il
echo $TEST_FILE
#TEST_TYPE1=`echo $TEST_TYPE1 | $SED -s 's/&/\\\&/'`
#TEST_TYPE2=`echo $TEST_TYPE2 | $SED -s 's/&/\\\&/'`
$SED -e "s/VALIDITY/${TEST_VALIDITY}/g"  -e "s/CSTOR/${TEST_CSTOR}/g"  -e "s/OPCODE/${TEST_OP}/g" -e "s/TYPE1/${TEST_TYPE1}/g" -e "s/TYPE2/${TEST_TYPE2}/g" > $TEST_FILE <<//EOF

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

.class interface abstract InterfaceA
{
}

.class interface abstract InterfaceB
{
}

.class sealed MyValueType extends [mscorlib]System.ValueType
{
	.field public int32 fld
}

.class ClassB extends [mscorlib]System.Object
{
    .field public TYPE1 fld
    .field public static TYPE1 sfld

	.method public hidebysig specialname rtspecialname instance default void .ctor () cil managed
	{
		.maxstack 8
		ldarg.0 
		call instance void object::.ctor()
		ret
	}
}

.class ClassA extends [mscorlib]System.Object
{
    .field public TYPE1 fld
    .field public static TYPE1 sfld
    .field public initonly TYPE1 const_field
    .field public static initonly TYPE1 st_const_field

	.method public hidebysig specialname rtspecialname instance default void .ctor () cil managed
	{
		.maxstack 8
		ldarg.0 
		call instance void object::.ctor()
		ret
	}
}

.class public SubClass extends ClassA
{
    .field public TYPE1 subfld
    .field public static TYPE1 subsfld

	.method public hidebysig specialname rtspecialname instance default void .ctor () cil managed
	{
		.maxstack 8
		ldarg.0 
		call instance void ClassA::.ctor()
		ret
	}
}

.class public explicit Overlapped extends [mscorlib]System.Object
{
    .field[0] public TYPE1 field1
    //.field[0] public TYPE1 field2
    .field[8] public TYPE1 field3
    //.field[8] public TYPE1 field4
    .field[16] public TYPE1 field5
    .field[20] public TYPE1 field10
    .field[24] public TYPE2 field_ok

	.method public hidebysig specialname rtspecialname instance default void .ctor () cil managed
	{
		.maxstack 8
		ldarg.0 
		call instance void object::.ctor()
		ret
	}
}

.class public explicit SubOverlapped extends Overlapped
{
    .field[16] public TYPE1 field6

	.method public hidebysig specialname rtspecialname instance default void .ctor () cil managed
	{
		.maxstack 8
		ldarg.0 
		call instance void Overlapped::.ctor()
		ret
	}
}

.method public static int32 Main() cil managed
{
	.entrypoint
	.maxstack 2
	.locals init (
		TYPE2 V_1
	)
	CSTOR
	OPCODE // VALIDITY.
	pop
	ldc.i4.0
	ret
}
//EOF
