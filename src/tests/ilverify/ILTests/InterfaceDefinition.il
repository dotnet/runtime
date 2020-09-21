// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly InterfaceDefinition
{
}

.assembly extern System.Runtime
{
}

.class interface public auto ansi abstract Interface
{	
	.method public hidebysig newslot abstract virtual 
		instance void M1 () cil managed 
	{
	}

	.method public hidebysig newslot abstract virtual 
		instance void M2 () cil managed 
	{
	}

	.method public hidebysig newslot abstract virtual 
		instance void M3 (
			int32 i
		) cil managed 
	{
	}

	.method public hidebysig newslot abstract virtual 
		instance int32 M4 (
			int32 i
		) cil managed 
	{
	}
}
