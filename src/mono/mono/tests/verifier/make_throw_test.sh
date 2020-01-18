#! /bin/sh

SED="sed"
if [ `which gsed 2> /dev/null` ] ; then 
	SED="gsed"
fi

TEST_NAME=$1
TEST_VALIDITY=$2
TEST_LOCAL=$3
TEST_OP=$4


TEST_NAME=${TEST_VALIDITY}_${TEST_NAME}
TEST_FILE=${TEST_NAME}_generated.il
echo $TEST_FILE
$SED -e "s/VALIDITY/${TEST_VALIDITY}/g" -e "s/LOCAL/${TEST_LOCAL}/g"  -e "s/OP/${TEST_OP}/g" > $TEST_FILE <<//EOF

// VALIDITY CIL which breaks the ECMA-335 rules. 
// this CIL should fail verification by a conforming CLI verifier.

.assembly '${TEST_NAME}_generated'
{
  .hash algorithm 0x00008004
  .ver  0:0:0:0
}

.class ClassA extends [mscorlib]System.Object
{
    .method public hidebysig  specialname  rtspecialname 
           instance default void .ctor ()  cil managed 
    {
        .maxstack 8
        ldarg.0 
        call instance void object::.ctor()
        ret 
    }
}

.class ClassSubA extends ClassA
{
    .method public hidebysig  specialname  rtspecialname 
           instance default void .ctor ()  cil managed 
    {
        .maxstack 8
        ldarg.0 
        call instance void ClassA::.ctor()
        ret 
    }
}

.class public auto ansi sealed MyStruct
  	extends [mscorlib]System.ValueType
{
	.field public int32 foo
}

.method public static void foo() cil managed
{
	.maxstack 8
	.locals init (LOCAL l_0 )
	ldloc.0
	OP
	throw // VALIDITY.
	ret
}

.method public static int32 Main() cil managed
{
	.maxstack 8
	.entrypoint
	.try {
		call void foo ()
		leave END
	} catch [mscorlib]System.NullReferenceException {
		pop 
		leave END

        }
END:	ldc.i4.0
	ret
}
//EOF
