#! /bin/sh

SED="sed"
if [ `which gsed 2> /dev/null` ] ; then 
	SED="gsed"
fi

TEST_NAME=$1
TEST_VALIDITY=$2
TEST_OP=$3
TEST_BEFORE_OP=$4
TEST_CONSTRAINT_TYPE=$5

if [ "x$TEST_CONSTRAINT_TYPE" = "x" ]; then
	TEST_CONSTRAINT_TYPE="IFace";
fi


TEST_FILE=`echo ${TEST_VALIDITY}_${TEST_NAME} | $SED -e 's/ /_/g' -e 's/\./_/g' -e 's/&/mp/g' -e 's/\[/_/g' -e 's/\]/_/g'`_generated.il
echo $TEST_FILE
$SED  -e "s/TYPE/${TEST_TYPE}/g" -e "s/OPCODE/${TEST_OP}/g"  -e "s/BEFORE_OP/${TEST_BEFORE_OP}/g"> $TEST_FILE <<//EOF

.assembly extern mscorlib
{
  .ver 2:0:0:0
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) // .z\V.4..
}

.assembly 'boxed_generic_arg_test'
{
  .hash algorithm 0x00008004
  .ver  0:0:0:0
}

.module boxed_genarg.exe


.class interface public auto ansi abstract IFace
{
	.method public virtual hidebysig newslot abstract instance default void Tst ()  cil managed 
	{
	}
}

.class public auto ansi beforefieldinit BaseClass extends [mscorlib]System.Object
{
	.field public int32 fld

	.method public hidebysig specialname rtspecialname instance default void '.ctor' () cil managed 
	{
		.maxstack 8
		ldarg.0 
		call instance void object::'.ctor'()
		ret 
	}
}

.class public auto ansi beforefieldinit IFaceImpl extends BaseClass implements IFace
{
	.method public hidebysig specialname rtspecialname instance default void '.ctor' () cil managed 
	{
		.maxstack 8
		ldarg.0 
		call instance void BaseClass::'.ctor'()
		ret 
	}

	.method public final virtual hidebysig newslot instance default void Tst () cil managed 
	{
		.maxstack 8
		ret 
	}
}

.class public auto ansi sealed TstDelegate extends [mscorlib]System.MulticastDelegate
{
	.method public hidebysig  specialname  rtspecialname  instance default void '.ctor' (object 'object', native int 'method')  runtime managed 
	{
	}

	.method public virtual  hidebysig  newslot instance default void Invoke ()  runtime managed 
	{
	}

	.method public virtual  hidebysig  newslot instance default class [mscorlib]System.IAsyncResult BeginInvoke (class [mscorlib]System.AsyncCallback callback, object 'object')  runtime managed 
	{
	}

	.method public virtual  hidebysig  newslot 
	instance default void EndInvoke (class [mscorlib]System.IAsyncResult result)  runtime managed 
	{
	}
}

.class public DriverClass<(${TEST_CONSTRAINT_TYPE}) T>
{
	.field !T t
	.field ${TEST_CONSTRAINT_TYPE} ifField

	.method public hidebysig  specialname  rtspecialname instance default void .ctor (!T A_0)  cil managed 
	{
		.maxstack 8
		ldarg.0 
		call instance void object::.ctor()

		ldarg.0
		ldarg.1
		stfld !0 class DriverClass<!0>::t
		ret 
	}

	.method public void Driver ()
	{
		.maxstack 8
		.locals init (!T V_0, ${TEST_CONSTRAINT_TYPE} V_1, !T[] V_2, ${TEST_CONSTRAINT_TYPE}[] V_3)

		ldc.i4.1
		newarr !T
		stloc.2

		ldc.i4.1
		newarr ${TEST_CONSTRAINT_TYPE}
		stloc.3

		ldarg.0
		ldfld !0 class DriverClass<!0>::t
		stloc.0


		BEFORE_OP

		OPCODE

TARGET:
		leave END
END:
		ret 
	}

}


.method public static int32 Main ()
{
	.entrypoint
	.maxstack 8
	.locals init ()

	newobj instance void class IFaceImpl::.ctor()

	newobj instance void class DriverClass<IFaceImpl>::.ctor(!0)
	call instance void class DriverClass<IFaceImpl>::Driver()

	ldc.i4.0
	ret 
}

//EOF
