#! /bin/sh

SED="sed"
if [ `which gsed 2> /dev/null` ] ; then 
	SED="gsed"
fi

TEST_NAME=$1
TEST_VALIDITY=$2
TEST_SRC=$3
TEST_DEST=$4
TEST_INST_TYPE=$5

INST_TYPE="DefaultArgument";
if [ "$TEST_INST_TYPE" != "" ]; then
	INST_TYPE="$TEST_INST_TYPE";
fi



TEST_NAME=${TEST_VALIDITY}_${TEST_NAME}
TEST_FILE=${TEST_NAME}_generated.il
echo $TEST_FILE
$SED -e "s/INIT/${TEST_INIT}/g" -e "s/VALIDITY/${TEST_VALIDITY}/g" -e "s/TARGET_CONSTRAINT/${TEST_DEST}/g" -e "s/SOURCE_CONSTRAINT/${TEST_SRC}/g" > $TEST_FILE <<//EOF

// VALIDITY CIL which breaks the ECMA-335 rules. 
// this CIL should fail verification by a conforming CLI verifier.

.assembly '${TEST_NAME}_generated'
{
  .hash algorithm 0x00008004
  .ver  0:0:0:0
}

.assembly extern mscorlib
{
  .ver 2:0:0:0
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) // .z\V.4..
}

.module bne_with_generic_type_type.exe

.class interface public auto ansi abstract IfaceA
{
} 

.class interface public auto ansi abstract IfaceB
{
} 

.class public auto ansi Class extends [mscorlib]System.Object
{
	.method public hidebysig  specialname  rtspecialname instance default void '.ctor' ()  cil managed 
	{
	    .maxstack 8
		ldarg.0 
	    call instance void object::'.ctor'()
	    ret 
	}
} 

.class public auto ansi DefaultArgument	extends Class implements IfaceA, IfaceB
{
	.method public hidebysig  specialname  rtspecialname instance default void '.ctor' ()  cil managed 
	{
	    .maxstack 8
		ldarg.0 
	    call instance void Class::'.ctor'()
	    ret 
	}
} 



.class public auto ansi beforefieldinit Test
        extends [mscorlib]System.Object
{

	.method public hidebysig  specialname  rtspecialname instance default void '.ctor' ()  cil managed 
	{
	    .maxstack 8
		ldarg.0 
	    call instance void object::'.ctor'()
	    ret 
	}

	.method public static void Method< SOURCE_CONSTRAINT T> ()
	{
		.locals init ()
		ret
	}
}

.class public auto ansi beforefieldinit Test2< TARGET_CONSTRAINT T>
        extends [mscorlib]System.Object
{
	.method public static void Method ()
	{
		.locals init ()
		call void class Test::Method<!T>()
		ret
	}
}

.method public static int32 Main ()
{
	.entrypoint
	.maxstack 8

	call void class Test2< $INST_TYPE >::Method()

	ldc.i4.0
	ret 
}

//EOF
