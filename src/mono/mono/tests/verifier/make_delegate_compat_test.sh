#! /bin/sh

SED="sed"
if [ `which gsed 2> /dev/null` ] ; then 
	SED="gsed"
fi

TEST_NAME=$1
TEST_VALIDITY=$2
TEST_RET_TYPE1=$3
TEST_RET_TYPE2=$4
TEST_PARAM_TYPE1=$5
TEST_PARAM_TYPE2=$6
TEST_CCONV1=$7
TEST_CCONV2=$8
TEST_USE_NATIVE=$9

TCONV_1="default"
TCONV_2=""

if [ "$TEST_CCONV1" != "" ]; then
	TCONV_1=$TEST_CCONV1
	TCONV_2=$TEST_CCONV2
fi

RET_2_LOCAL="$TEST_RET_TYPE2"
RET_2_OP="ldloc 0"

if [ "$TEST_RET_TYPE2" = "void" ]; then
	RET_2_LOCAL="int32"
	RET_2_OP="nop"
fi

LDFTN="ldftn $TCONV_2 ${TEST_RET_TYPE2} Driver::Method(${TEST_PARAM_TYPE2})"
CALLVIRT="callvirt instance $TCONV_1 ${TEST_RET_TYPE1} TargetDelegate::Invoke (${TEST_PARAM_TYPE1})"
LOCAL_PARAM_1="${TEST_PARAM_TYPE1}"


MANAGED_METHOD="
	.method public static $TCONV_2 RET_2 Method(PARAM_2 V_0) cil managed
	{
		.maxstack 2
		.locals init ($RET_2_LOCAL ARG)

		$RET_2_OP
		ret
	}
"

if [ "$TEST_USE_NATIVE" = "pinvoke" ]; then
	LDFTN="ldftn $TCONV_2 ${TEST_RET_TYPE2} Driver::NativeMethod(${TEST_PARAM_TYPE2})"
	CALLVIRT="nop"
	MANAGED_METHOD=""
	LOCAL_PARAM_1="native int"
fi



TEST_NAME=${TEST_VALIDITY}_${TEST_NAME}
TEST_FILE=${TEST_NAME}_generated.il
echo $TEST_FILE


$SED -e "s/VALIDITY/${TEST_VALIDITY}/g" -e "s/RET_1/${TEST_RET_TYPE1}/g" -e "s/RET_2/${TEST_RET_TYPE2}/g" -e "s/PARAM_1/${TEST_PARAM_TYPE1}/g" -e "s/PARAM_2/${TEST_PARAM_TYPE2}/g"> $TEST_FILE <<//EOF

// VALIDITY CIL which breaks the ECMA-335 rules. 
// this CIL should fail verification by a conforming CLI verifier.

.assembly '${TEST_NAME}_generated'
{
  .hash algorithm 0x00008004
  .ver  0:0:0:0
}

.class ClassA extends [mscorlib]System.Object
{
}

.class ClassB extends [mscorlib]System.Object
{
}

.class ClassSubA extends ClassA
{
}

.class interface abstract InterfaceA
{
}

.class interface abstract InterfaceB implements InterfaceA
{
}

.class ImplA extends [mscorlib]System.Object implements InterfaceA
{
}

.class sealed MyValueType extends [mscorlib]System.ValueType
{
	.field private int32 v
}

.class public auto ansi sealed Int32Enum
	extends [mscorlib]System.Enum
{
	.field  public specialname  rtspecialname  unsigned int32 value__

	.field public static literal valuetype Int32Enum V_0 = int32(0x00)
	.field public static literal valuetype Int32Enum V_1 = int32(0x01)
}


.class public auto ansi sealed TargetDelegate extends [mscorlib]System.MulticastDelegate
{
	.method public hidebysig  specialname  rtspecialname 
		instance default void .ctor (object 'object', native int 'method')  runtime managed 
	{
	}

	.method public virtual  hidebysig  newslot
		instance $TCONV_1 RET_1 Invoke (PARAM_1 V_0)  runtime managed 
	{
	}

 	.method public virtual hidebysig  newslot 
		instance default class [mscorlib]System.IAsyncResult BeginInvoke (class [mscorlib]System.AsyncCallback callback, object 'object')  runtime managed 
	{
	}

	.method public virtual  hidebysig  newslot 
		instance default RET_1 EndInvoke (class [mscorlib]System.IAsyncResult result)  runtime managed 
	{
	}
}


.class public auto ansi Driver
{
	$MANAGED_METHOD

	.method public static pinvokeimpl ("libtest" as "Bla" winapi) $TCONV_2 RET_2 NativeMethod (PARAM_2 V_0)  cil managed 
    {
    } 

	.method public static int32 Foo() cil managed
	{
		.entrypoint
		.maxstack 2
		.locals init ($LOCAL_PARAM_1 ARG)

		ldnull
		
		$LDFTN
		newobj instance void class TargetDelegate::.ctor(object, native int) // VALIDITY
		ldloc.0
		$CALLVIRT

		leave END
END:
		ldc.i4.0
		ret
	}
}
//EOF
