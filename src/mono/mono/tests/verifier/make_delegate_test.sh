#! /bin/sh

SED="sed"
if [ `which gsed 2> /dev/null` ] ; then 
	SED="gsed"
fi

TEST_NAME=$1
TEST_VALIDITY=$2
TEST_LDFTN_OP=$3
TEST_DELEGATE_NAME=$4
TEST_OP=$5
TEST_IS_STATIC=$6

if [ "$TEST_IS_STATIC" ]; then
	METHOD_STATIC="static";
	EXTRA_ARG="$TEST_IS_STATIC par, ";
fi

TEST_FILE=`echo ${TEST_VALIDITY}_${TEST_NAME} | $SED -e 's/ /_/g' -e 's/\./_/g' -e 's/&/mp/g' -e 's/\[/_/g' -e 's/\]/_/g'`_generated.il
echo $TEST_FILE
TEST_TYPE1=`echo $TEST_TYPE1 | $SED -s 's/&/\\\&/'`
$SED -e "s/OPCODE/${TEST_OP}/g" -e "s/VALIDITY/${TEST_VALIDITY}/g" -e "s/DELEGATE_NAME/${TEST_DELEGATE_NAME}/g" -e "s/LDFTN_OP/${TEST_LDFTN_OP}/g" > $TEST_FILE <<//EOF

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

.class private sequential ansi sealed beforefieldinit MyValueType extends [mscorlib]System.ValueType
{
	.field  private  int32 dd

	.method public virtual hidebysig instance default string ToString ()  cil managed 
	{
		.maxstack 8
		ldstr "test"
		ret 
	}

	.method public hidebysig instance default void NonVirtMethod ()  cil managed 
	{
		.maxstack 8
		ret 
	}

}

.class public auto ansi sealed ToStringDelegate extends [mscorlib]System.MulticastDelegate
{
	.method public hidebysig  specialname  rtspecialname 
		instance default void .ctor (object 'object', native int 'method')  runtime managed 
	{
	}

	.method public virtual  hidebysig  newslot
		instance default string Invoke ()  runtime managed 
	{
	}

 	.method public virtual  hidebysig  newslot 
		instance default class [mscorlib]System.IAsyncResult BeginInvoke (class [mscorlib]System.AsyncCallback callback, object 'object')  runtime managed 
	{
	}

	.method public virtual  hidebysig  newslot 
		instance default string EndInvoke (class [mscorlib]System.IAsyncResult result)  runtime managed 
	{
	}
}

.class public auto ansi sealed DelegateNoArg extends [mscorlib]System.MulticastDelegate
{
	.method public hidebysig  specialname  rtspecialname 
		instance default void .ctor (object 'object', native int 'method')  runtime managed 
	{
	}

	.method public virtual  hidebysig  newslot
		instance default void Invoke ()  runtime managed 
	{
	}

 	.method public virtual  hidebysig  newslot 
		instance default class [mscorlib]System.IAsyncResult BeginInvoke (class [mscorlib]System.AsyncCallback callback, object 'object')  runtime managed 
	{
	}

	.method public virtual  hidebysig  newslot 
		instance default void EndInvoke (class [mscorlib]System.IAsyncResult result)  runtime managed 
	{
	}
} 

.class public auto ansi sealed DelegateIntArg extends [mscorlib]System.MulticastDelegate
{
	.method public hidebysig  specialname  rtspecialname 
		instance default void .ctor (object 'object', native int 'method')  runtime managed 
	{
	}

	.method public virtual  hidebysig  newslot
		instance default void Invoke (int32 d)  runtime managed 
	{
	}

 	.method public virtual  hidebysig  newslot 
		instance default class [mscorlib]System.IAsyncResult BeginInvoke (int32 d, class [mscorlib]System.AsyncCallback callback, object 'object')  runtime managed 
	{
	}

	.method public virtual  hidebysig  newslot 
		instance default void EndInvoke (class [mscorlib]System.IAsyncResult result)  runtime managed 
	{
	}
} 

.class public Parent
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

	.method public virtual void ParentVirtMethod ()
	{
		ret
	}

	.method public void ParentMethod ()
	{
		ret
	}

	.method public static void ParentStaticMethod ()
	{
		ret
	}

	.method public virtual void SealedVirtMethod ()
	{
		ret
	}

}

.class public auto ansi beforefieldinit Driver
        extends Parent
{
	.method public hidebysig  specialname  rtspecialname instance default void .ctor ()  cil managed 
	{
		.maxstack 8
		ldarg.0
		call instance void Parent::.ctor()
		ret 
	}

	.method public static void Method ()
	{
		ret
	}

	.method public static void Method2 (int32 a)
	{
		ret
	}

	.method public virtual void VirtMethod ()
	{
		ret
	}

	.method public virtual void VirtMethod2 (int32 d)
	{
		ret
	}

	.method public void NonVirtMethod ()
	{
		ret
	}

	.method public void NonVirtMethod2 (int32 d)
	{
		ret
	}

	.method public final virtual void SealedVirtMethod ()
	{
		ret
	}

	.method public ${METHOD_STATIC} void DriverExec (${EXTRA_ARG} int32 V_1)
	{
		.maxstack 8
		.locals init (MyValueType V_0)
	
		OPCODE
		LDFTN_OP
DELEGATE_OP:
		newobj instance void class DELEGATE_NAME::.ctor(object, native int) // VALIDITY
		pop

		ret
	}

	.method public static int32 Main ()
	{
		.entrypoint
		.maxstack 2
		.locals init ()
		.try {
			newobj instance void Driver::.ctor()
			ldc.i4.0
			call void Driver::DriverExec (${EXTRA_ARG} int32)
			leave END
		} catch [mscorlib]System.ArgumentException
		{
			pop
			leave END
	
		}
END:
		ldc.i4.0
		ret 

	}
}
//EOF
