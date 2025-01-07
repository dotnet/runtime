// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

.assembly extern mscorlib { }

.assembly 'library' { }

.class interface public auto ansi abstract IInterfaceWithoutNewSlot
{
	.method public hidebysig abstract virtual 
		instance void AbstractNoNewSlot () cil managed 
	{
	}

	.method public hidebysig abstract virtual 
		instance void AbstractNoNewSlotUnused () cil managed 
	{
	}

	.method public hidebysig newslot abstract virtual 
		instance void AbstractNewSlot () cil managed 
	{
	}

	.method public hidebysig newslot abstract virtual 
		instance void AbstractNewSlotUnused () cil managed 
	{
	}

	.method public hidebysig virtual 
		instance void ImplementedNoNewSlot () cil managed 
	{
		ret;
	}

	.method public hidebysig virtual 
		instance void ImplementedNoNewSlotUnused () cil managed 
	{
		ret;
	}

	.method public hidebysig newslot virtual 
		instance void ImplementedNewSlot () cil managed 
	{
		ret;
	}

	.method public hidebysig newslot virtual 
		instance void ImplementedNewSlotUnused () cil managed 
	{
		ret;
	}
}
