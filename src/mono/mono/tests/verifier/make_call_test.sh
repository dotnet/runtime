#! /bin/sh

SED="sed"
if [ `which gsed 2> /dev/null` ] ; then 
	SED="gsed"
fi

TEST_NAME=$1
TEST_VALIDITY=$2
TEST_OP=$3
TEST_LOAD_ARGS=$4
TEST_INSTANCE_METHOD=$5
TEST_EXTRA_STUFF=$6

if [ "$TEST_INSTANCE_METHOD" = "instance" ]; then
	MEMBER_TEST_OP=$TEST_OP
	MEMBER_TEST_LOAD_ARGS=$TEST_LOAD_ARGS
	MEMBER_TEST_EXTRA_STUFF=$6

	TEST_LOAD_ARGS="newobj instance void Driver::.ctor()"
	TEST_OP="call instance void Driver::MemberMain()"
	TEST_EXTRA_STUFF=""
fi



TEST_NAME=${TEST_VALIDITY}_${TEST_NAME}
TEST_FILE=${TEST_NAME}_generated.il
echo $TEST_FILE
$SED -e "s/VALIDITY/${TEST_VALIDITY}/g" -e "s/OPCODE/${TEST_OP}/g" -e "s/LOAD_ARGS/${TEST_LOAD_ARGS}/g" -e "s/MEMBER_OP/${MEMBER_TEST_OP}/g" -e "s/MEMBER_LD_ARGS/${MEMBER_TEST_LOAD_ARGS}/g" -e "s/EXTRA_STUFF/${TEST_EXTRA_STUFF}/g" -e "s/EXTRA/${MEMBER_TEST_EXTRA_STUFF}/g" > $TEST_FILE <<//EOF

// VALIDITY CIL which breaks the ECMA-335 rules. 
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

.method public static void GlobalMethod1() cil managed
{
	ret
}

.method public static void GlobalMethod2(int32 a) cil managed
{
	ret
}

.class ClassA extends [mscorlib]System.Object
{
	.method public hidebysig  specialname rtspecialname instance default void .ctor () cil managed 
	{
		.maxstack 8
		ldarg.0 
		call instance void object::.ctor()
		ret 
	}

	.method public  void Method1() cil managed
	{
		ret
	}

	.method public  void Method2(int32 a) cil managed
	{
		ret
	}

	.method public virtual void VirtMethod() cil managed
	{
		ret
	}
}

.class ClassB extends [mscorlib]System.Object
{
	.method public hidebysig  specialname rtspecialname instance default void .ctor () cil managed 
	{
		.maxstack 8
		ldarg.0 
		call instance void object::.ctor()
		ret 
	}

}

.class ClassC extends ClassA
{
	.method public hidebysig  specialname rtspecialname instance default void .ctor () cil managed 
	{
		.maxstack 8
		ldarg.0 
		call instance void ClassA::.ctor()
		ret 
	}

	.method public virtual final void VirtMethod() cil managed
	{
		ret
	}
}

.class interface abstract InterfaceA
{
	.method public abstract virtual instance void AbsMethod () cil managed 
	{
	}
}

.class ImplIfaceA extends [mscorlib]System.Object implements InterfaceA
{
	.method public hidebysig  specialname rtspecialname instance default void .ctor () cil managed 
	{
		.maxstack 8
		ldarg.0 
		call instance void object::.ctor()
		ret 
	}

	.method public virtual instance void AbsMethod () cil managed 
	{
		ret
	}
}

.class sealed MyValueType extends [mscorlib]System.ValueType
{
	.field private int32 v

	.method public instance void Method ()
	{
		ret
	}

	.method public virtual instance int32 GetHashCode()
	{
		ldc.i4.0
		ret
	}
}


.class BaseClass extends [mscorlib]System.Object
{
	.method public hidebysig  specialname rtspecialname instance default void .ctor () cil managed 
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
}


.class Driver extends BaseClass
{

	.method public virtual void VirtMethod ()
	{
		ret
	}

	.method public hidebysig  specialname rtspecialname instance default void .ctor () cil managed 
	{
		.maxstack 8
		ldarg.0 
		call instance void BaseClass::.ctor()
		ret 
	}

	.method public void MemberMain() cil managed
	{
		.maxstack 8
		.locals init (MyValueType V_0)

		MEMBER_LD_ARGS
		MEMBER_OP
		EXTRA

		ret
	}

	.method public static void Main() cil managed
	{
		.entrypoint
		.maxstack 8
		.locals init (MyValueType V_0)

		.try {
			LOAD_ARGS
			OPCODE
			EXTRA_STUFF
			leave END
		} catch [mscorlib]System.NullReferenceException {
			leave END
		}

END:
		ret
	}

}
//EOF
