// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'Box_Unbox_unaligned_4'{}

//#######################################
//#        -- valClass --              #################################
//#######################################
.class public value sealed valClass {
//--------------------------#
// String used in testing  -#
//--------------------------#
//--------------------------#
// Fields                  -#
//--------------------------#
.field int32 Field1
.field float32 Field2
//---------------------------#
//- Initialization routine  -#
//---------------------------#
.method void _init() {
.maxstack	4
// -- store a value in int32 Field1
	ldarg 	0
	ldc.i4 		0xFF
	unaligned. 0x4
	stfld 		int32 valClass::Field1

// -- store a value in float32 Field2
	ldarg 	0
	ldc.r4 		float32(0x00ABCDEF)
	unaligned. 0x4
	stfld 		float32 valClass::Field2

// -- initialization complete... return
	ret
}

//----------------------------#
//- Return the string at RVA -#
//-  TestString              -#
//-  This overrides          -#
//-  Object.ToString         -#
//----------------------------#
.method public virtual class [mscorlib]System.String ToString() {
.maxstack	4
// -- load the string at RVA TestString _and return it
	ldstr "Some Test String"


	ret
}



//#######################
//#   -- BOX_UNBOX --  ###############################################
//#######################

}
.class public Box_Unbox {
//--------------------------#
// String used in testing  -#
// should be the same as   -#
// the string used in      -#
// valClass                -#
//--------------------------#
//--------------------------#
// ref to valClass         -#
//--------------------------#
.field static	value class valClass vc
//--------------------------#
// entry point             -#
//--------------------------#
.method public static int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	10
// -- initialize the val class
  	ldsflda 	value class valClass Box_Unbox::vc
  	call		instance void valClass::_init()
// -- see if Field1 of valClass has the correct value
	ldsflda		value class valClass Box_Unbox::vc
	unaligned. 0x4
	ldfld		int32 valClass::Field1
	ldc.i4		0xFF
	ceq
	brfalse		FAIL
// -- see if Field2 of valClass has the correct value
	ldsflda		value class valClass Box_Unbox::vc
	unaligned. 0x4
	ldfld		float32 valClass::Field2
	ldc.r4		float32(0x00ABCDEF)
	ceq
	brfalse		FAIL
// -- get the constant string from valClass _and
// -- compare it to the constant string in this
// -- if they are _not the same.  FAIL
	ldsfld 	value class valClass Box_Unbox::vc
	box			valClass
	callvirt    instance class [mscorlib]System.String [mscorlib]System.Object::ToString()
	ldstr		"Some Test String"


	callvirt	instance bool [mscorlib]System.String::Equals(class [mscorlib]System.String)
	brfalse		FAIL

// -- PASS --
PASS:
	ldc.i4	0x64
	ret

// -- FAIL --
FAIL:
	ldc.i4	0x0
	ret

}
}
// ---------------------------------------------------------
