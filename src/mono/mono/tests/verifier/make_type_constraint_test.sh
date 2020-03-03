#! /bin/sh

SED="sed"
if [ `which gsed 2> /dev/null` ] ; then 
	SED="gsed"
fi

TEST_NAME=$1
TEST_VALIDITY=$2
TEST_INSTANTIATION=$3
TEST_CONSTRAINTS=$4
TEST_EXTRA_CODE=$5

TEST_NAME=${TEST_VALIDITY}_${TEST_NAME}
TEST_FILE=${TEST_NAME}_generated.il
echo $TEST_FILE

$SED -e "s/VALIDITY/${TEST_VALIDITY}/g" -e "s/INSTANTIATION/${TEST_INSTANTIATION}/g" -e "s/CONSTRAINTS/${TEST_CONSTRAINTS}/g" -e "s/EXTRA_CODE/${TEST_EXTRA_CODE}/g" > $TEST_FILE <<//EOF

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

.class interface public auto ansi abstract IFace
{
	.method public virtual hidebysig newslot abstract instance default void Tst ()  cil managed 
	{
	}
}

.class public auto ansi beforefieldinit IFaceImpl extends [mscorlib]System.Object implements IFace
{
	.method public hidebysig specialname rtspecialname instance default void '.ctor' () cil managed 
	{
		.maxstack 8
		ldarg.0 
		call instance void object::'.ctor'()
		ret 
	}

	.method public final virtual hidebysig newslot instance default void Tst () cil managed 
	{
		.maxstack 8
		ret 
	}
}



.class ClassNoDefaultCtor extends [mscorlib]System.Object
{
	.method public hidebysig  specialname rtspecialname instance default void .ctor (int32 d) cil managed 
	{
		.maxstack 8
		ldarg.0 
		call instance void object::.ctor()
		ret 
	}
}

.class abstract AbstractClass extends [mscorlib]System.Object
{
	.method public hidebysig  specialname rtspecialname instance default void .ctor () cil managed 
	{
		.maxstack 8
		ldarg.0 
		call instance void object::.ctor()
		ret 
	}
}

.class ClassWithDefaultCtorNotVisible extends [mscorlib]System.Object
{
	.method private hidebysig  specialname rtspecialname instance default void .ctor () cil managed 
	{
		.maxstack 8
		ldarg.0 
		call instance void object::.ctor()
		ret 
	}
}

.class ClassWithDefaultCtor extends [mscorlib]System.Object
{
	.method public hidebysig  specialname rtspecialname instance default void .ctor () cil managed 
	{
		.maxstack 8
		ldarg.0 
		call instance void object::.ctor()
		ret 
	}
}


.class sealed MyValueType extends [mscorlib]System.ValueType
{
	.field public int32 v
}

.class public auto ansi sealed MyEnum
  	extends [mscorlib]System.Enum
{
    .field  public specialname  rtspecialname  int32 value__
    .field public static  literal  valuetype MyEnum B = int32(0x00000000)
    .field public static  literal  valuetype MyEnum C = int32(0x00000001)
}


.class TemplateTarget<CONSTRAINTS T> extends [mscorlib]System.Object
{
	.field !T t

	.method public hidebysig  specialname rtspecialname instance default void .ctor () cil managed 
	{
		.maxstack 8
		ldarg.0 
		call instance void object::.ctor()
		ret 
	}

	.method public void DoStuff() cil managed
	{
		.maxstack 8
		.locals init ()
		ldtoken !T
        call class [mscorlib]System.Type class [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
        call void class [mscorlib]System.Console::WriteLine(object)

		EXTRA_CODE
		ret
	}
}

.class Driver extends [mscorlib]System.Object
{
	.method public static void UseIFace (IFace arg0) {
		.maxstack 8
		ret
	}

	.method public static void MemberMain() cil managed
	{
		.maxstack 8
		.locals init ()
		newobj instance void class TemplateTarget<INSTANTIATION>::.ctor()
		call instance void class TemplateTarget<INSTANTIATION>::DoStuff()
		ret
	}

	.method public static void Main() cil managed
	{
		.entrypoint
		.maxstack 8
		.locals init ()

		call void Driver::MemberMain()
		leave END

END:
		ret
	}

}
//EOF
