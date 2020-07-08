// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


.assembly extern legacy library mscorlib {}
.assembly box_unbox.exe{}

.class public value sealed valClass {
.field public int32 Field1
.field public float32 Field2
.method public void _init() {
.maxstack	4
	ldarg 	0
	ldc.i4 		0xFF
	stfld 		int32 valClass::Field1

	ldarg 	0
	ldc.r4 		float32(0x00ABCDEF)
	stfld 		float32 valClass::Field2

	ret
}

.method public virtual class [mscorlib]System.String ToString() {
.maxstack	4
	ldstr "Some Test String"


	ret
}




}
.class public Box_Unbox {
.field public static	value class valClass vc
.method public static int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack	10
  	ldsflda 	value class valClass Box_Unbox::vc
  	call		instance void valClass::_init()
	ldsflda		value class valClass Box_Unbox::vc
	ldfld		int32 valClass::Field1
	ldc.i4		0xFF
	ceq
	brfalse		FAIL
	ldsflda		value class valClass Box_Unbox::vc
	ldfld		float32 valClass::Field2
	ldc.r4		float32(0x00ABCDEF)
	ceq
	brfalse		FAIL
	ldsfld 	value class valClass Box_Unbox::vc
	box			valClass
	callvirt    instance	class [mscorlib]System.String [mscorlib]System.Object::ToString()
	ldstr		"Some Test String"


	callvirt	instance	bool [mscorlib]System.String::Equals(class [mscorlib]System.String)
	brfalse		FAIL

PASS:
	ldc.i4 100
	ret

FAIL:
	ldc.i4	0x0
	ret

}
}
