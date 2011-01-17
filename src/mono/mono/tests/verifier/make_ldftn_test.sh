#! /bin/sh

SED="sed"
if [ `which gsed 2> /dev/null` ] ; then 
	SED="gsed"
fi

TEST_NAME=$1
TEST_VALIDITY=$2
TEST_LDFTN_OP=$3
TEST_OP=$4

TEST_FILE=`echo ${TEST_VALIDITY}_${TEST_NAME} | $SED -e 's/ /_/g' -e 's/\./_/g' -e 's/&/mp/g' -e 's/\[/_/g' -e 's/\]/_/g'`_generated.il
echo $TEST_FILE
TEST_TYPE1=`echo $TEST_TYPE1 | $SED -s 's/&/\\\&/'`
$SED -e "s/OPCODE/${TEST_OP}/g" -e "s/VALIDITY/${TEST_VALIDITY}/g" -e "s/LDFTN_OP/${TEST_LDFTN_OP}/g" > $TEST_FILE <<//EOF

.assembly extern mscorlib
{
  .ver 2:0:0:0
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) // .z\V.4..
}
.assembly 'ldftn_test'
{
  .hash algorithm 0x00008004
  .ver  0:0:0:0
}
.module ldftn_test.exe

.class public Test
  	extends [mscorlib]System.Object
{
	.method public hidebysig  specialname  rtspecialname instance default void .ctor ()  cil managed 
	{
		.maxstack 8
		ldarg.0
		call instance void object::.ctor()
		ret 
	}

	.method public virtual void VirtMethod ()
	{
		ret
	}

	.method public void Method ()
	{
		ret
	}

	.method public static void StaticMethod ()
	{
		ret
	}
}

.class public auto ansi beforefieldinit Driver
        extends [mscorlib]System.Object
{
	.method public static void Method ()
	{
		ret
	}

	.method public virtual void VirtMethod ()
	{
		ret
	}

	.method public static int32 Main ()
	{
		.entrypoint
		.maxstack 2
		.locals init (int32 bla)

		OPCODE // VALIDITY
		LDFTN_OP
		pop
		ldc.i4.0
		ret 
	}
}
//EOF
