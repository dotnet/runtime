// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
