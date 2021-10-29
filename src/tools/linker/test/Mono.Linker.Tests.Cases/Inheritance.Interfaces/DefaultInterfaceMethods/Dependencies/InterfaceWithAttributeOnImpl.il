// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { }

.assembly 'library' { }

.class interface public auto ansi abstract IMyInterface
{
	.method public hidebysig newslot virtual 
		instance void Frob () cil managed 
	{
	    ret
	}
}

.class public auto ansi MyClass
	extends [mscorlib]System.Object
	implements IMyInterface
{
	.interfaceimpl type IMyInterface
	.custom instance void MyAttribute::.ctor() = ( 01 00 00 00 )

	.method public hidebysig specialname rtspecialname
	instance void .ctor () cil managed
	{
		ret
	}
}

.class private auto ansi sealed beforefieldinit MyAttribute
	extends [mscorlib]System.Attribute
{
	.method public hidebysig specialname rtspecialname
		instance void .ctor () cil managed
	{
		ret
	}
}
