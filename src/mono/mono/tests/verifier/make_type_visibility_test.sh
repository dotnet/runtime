#! /bin/sh

TEST_NAME=$1
TEST_VALIDITY=$2
TEST_OP=$3
TEST_INIT=$4
TEST_EXTRA_LOC=$5


if [ "$TEST_EXTRA_LOC" != "" ]; then
	EXTRA_LOC=", $TEST_EXTRA_LOC V_1"
fi

TEST_NAME=${TEST_VALIDITY}_${TEST_NAME}
TEST_FILE=${TEST_NAME}_generated.il
echo $TEST_FILE
TEST_TYPE=`echo $TEST_TYPE | sed -s 's/&/\\\&/'`
sed -e "s/VALIDITY/${TEST_VALIDITY}/g" -e "s/TYPE/${TEST_TYPE}/g" -e "s/OPCODE/${TEST_OP}/g"  -e "s/INIT/${TEST_INIT}/g" > $TEST_FILE <<//EOF

// VALIDITY CIL which breaks the ECMA-335 rules. 
// this CIL should fail verification by a conforming CLI verifier.

.assembly '${TEST_NAME}_generated'
{
  .hash algorithm 0x00008004
  .ver  0:0:0:0
}

.assembly extern 'test_lib'
{
  .ver 0:0:0:0
}


.class public auto ansi beforefieldinit SimpleClass extends [mscorlib]System.Object
{
	.method public hidebysig specialname rtspecialname instance void  .ctor() cil managed
	{
		.maxstack  8
		ldarg.0
		call instance void [mscorlib]System.Object::.ctor()
		ret
	}

	.method public hidebysig static void Generic<T>() cil managed
	{
		.maxstack  8
		ret
	}
}


.class public auto ansi beforefieldinit Foo<T> extends [mscorlib]System.Object
{
	.method public hidebysig specialname rtspecialname instance void  .ctor() cil managed
	{
		.maxstack  8
		ldarg.0
		call instance void [mscorlib]System.Object::.ctor()
		ret
	}
}

.class public auto ansi beforefieldinit Test<T> extends class Foo<!0>
{
	.method public hidebysig specialname rtspecialname instance void  .ctor() cil managed
	{
		.maxstack  8
		ldarg.0
		call instance void class Foo<!0>::.ctor()
		ret
	}
}


.method public static int32 Main() cil managed
{
	.entrypoint
	.maxstack 8
	.locals init (object V_0 $EXTRA_LOC )

	ldloc.0
	INIT
	OPCODE // VALIDITY.

	leave END
END:
	ldc.i4.0
	ret
}

//EOF
