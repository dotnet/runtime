#! /bin/sh

SED="sed"
if [ `which gsed 2> /dev/null` ] ; then 
	SED="gsed"
fi

TEST_NAME=$1
TEST_VALIDITY=$2
TEST_CODE=$3
TEST_TARGET_TYPE=$4

TARGET_TYPE="Test"
TEST_OTHER_CODE="call instance void TestClass::'.ctor'()"

if [ "$TEST_TARGET_TYPE" = "other" ]; then
	TARGET_TYPE="TestSubClass"
	TEST_OTHER_CODE=$TEST_CODE
	TEST_CODE=""
fi

TEST_FILE=`echo ${TEST_VALIDITY}_${TEST_NAME} | $SED -e 's/ /_/g' -e 's/\./_/g' -e 's/&/mp/g' -e 's/\[/_/g' -e 's/\]/_/g'`_generated.il
echo $TEST_FILE
$SED -e "s/CODE/${TEST_CODE}/g" -e "s/OTHER/${TEST_OTHER_CODE}/g" -e "s/VALIDITY/${TEST_VALIDITY}/g" > $TEST_FILE <<//EOF

.assembly extern mscorlib
{
  .ver 2:0:0:0
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) // .z\V.4..
}
.assembly 'delegate_test'
{
  .hash algorithm 0x00008004
  .ver  0:0:0:0
}
.module delegate_test.exe
.class ansi beforefieldinit TestClass extends [mscorlib]System.Object
{
	.method public hidebysig specialname  rtspecialname instance default void .ctor ()  cil managed 
	{
		.maxstack 8
		ldarg.0
		call instance void object::'.ctor'()
		ret 
	}

	.method public hidebysig specialname  rtspecialname instance default void .ctor (object V_1)  cil managed 
	{
		.maxstack 8
		ldarg.0
		call instance void object::'.ctor'()
		ret 
	}
}

.module delegate_test.exe
.class ansi beforefieldinit TestSubClass extends TestClass
{
	.method public hidebysig specialname  rtspecialname instance default void .ctor ()  cil managed 
	{
		.maxstack 8
		ldarg.0
		OTHER
		
		leave END
END:
		ret 
	}
}




.class ansi beforefieldinit Test extends [mscorlib]System.Object
{
	.field int32 val
	.field object obj

	.method public hidebysig  specialname  rtspecialname instance default void .ctor ()  cil managed 
	{
		.maxstack 8
		.locals init (Test V_0)
		ldarg.0
		CODE
		
		leave END
END:
		ret 
	}

	.method public hidebysig static default void StaticMethod (object A_0)  cil managed 
	{
		.maxstack 8
		ret 
	}


	.method public hidebysig instance default void InstanceMethod ()  cil managed 
	{
		.maxstack 8
		ret 
	}

}


.class public auto ansi beforefieldinit Driver
        extends [mscorlib]System.Object
{
	.method public hidebysig  specialname  rtspecialname instance default void .ctor ()  cil managed 
	{
		.maxstack 8
		ldarg.0
		call instance void object::'.ctor'()

		ret 
	}

	.method public static int32 Main ()
	{
		.entrypoint
		.maxstack 2
		.locals init ()
		newobj instance void ${TARGET_TYPE}::.ctor()
		pop
		ldc.i4.0
		ret 

	}
}
//EOF
