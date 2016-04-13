// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern System.Console { }
.assembly 'RMV-4-CA'{}
.assembly extern mscorlib {}

.class public MyCA extends [mscorlib]System.Attribute{
	.field public int32 PubInstField
	.field private int32 PrvInstField
	.field private int32 PubInstProperty
	.field private int32 PrvInstProperty
	.field public int32[] PubInstFieldArr

	.method public specialname void .ctor(){
		ldarg.0
		call	instance void [mscorlib]System.Attribute::.ctor()
		ret
	}

	.method public specialname void .ctor(int32 argument){
		ldarg.0
		call	instance void [mscorlib]System.Attribute::.ctor()
		ret
	}

	.method public specialname void .ctor(int32[] argumentarray){
		ldarg.0
		call	instance void [mscorlib]System.Attribute::.ctor()
		ret
	}

	.method public static void NotTheConstuctorStat(int32){
		ret
	}

	.method public void NotTheConstructorInst(int32){
		ret
	}

	.property int32 PubInstProperty(){
		.set instance void MyCA::set_PubInstProperty(int32)
		.get instance int32 MyCA::get_PubInstProperty()
	}

	.property int32 PrvInstProperty(){
		.set instance void MyCA::set_PrvInstProperty(int32)
		.get instance int32 MyCA::get_PrvInstProperty()
	}

	.method public hidebysig specialname instance int32 get_PubInstProperty(){
		ldarg.0
		ldfld	int32 MyCA::PubInstProperty
		ret
	}
	.method public hidebysig specialname instance void set_PubInstProperty(int32){
		ldarg.0
		ldarg.1
		stfld	int32 MyCA::PubInstProperty
		ret
	}
	.method private hidebysig specialname instance int32 get_PrvInstProperty(){
		ldarg.0
		ldfld	int32 MyCA::PrvInstProperty
		ret
	}
	.method private hidebysig specialname instance void set_PrvInstProperty(int32){
		ldarg.0
		ldarg.1
		stfld	int32 MyCA::PrvInstProperty
		ret
	}
	
}
