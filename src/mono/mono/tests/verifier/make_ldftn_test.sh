#! /bin/sh

TEST_NAME=$1
TEST_VALIDITY=$2
TEST_LDFTN_OP=$3
TEST_OP=$4

TEST_FILE=`echo ${TEST_VALIDITY}_${TEST_NAME} | sed -e 's/ /_/g' -e 's/\./_/g' -e 's/&/mp/g' -e 's/\[/_/g' -e 's/\]/_/g'`_generated.il
echo $TEST_FILE
TEST_TYPE1=`echo $TEST_TYPE1 | sed -s 's/&/\\\&/'`
sed -e "s/OPCODE/${TEST_OP}/g" -e "s/VALIDITY/${TEST_VALIDITY}/g" -e "s/LDFTN_OP/${TEST_LDFTN_OP}/g" > $TEST_FILE <<//EOF

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
	.method public void Test ()
	{
		ret
	}
}


.class public Template<T>
  	extends [mscorlib]System.Object
{
	.method public void Test ()
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
