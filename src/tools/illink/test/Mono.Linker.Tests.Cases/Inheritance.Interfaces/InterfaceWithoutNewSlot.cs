// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces
{
	// Tests the case where the interface method doesn't have 'newslot' flag set
	[TestCaseRequirements (TestRunCharacteristics.SupportsDefaultInterfaceMethods, "Requires support for default interface methods")]
	[Define ("IL_ASSEMBLY_AVAILABLE")]
	[SetupCompileBefore ("library.dll", new[] { "Dependencies/InterfaceWithoutNewSlot.il" })]
	class InterfaceWithoutNewSlot
	{
		public static void Main ()
		{
			ImplementationType.Test ();
		}

#if !IL_ASSEMBLY_AVAILABLE
		// Declaration for the build of the test suite
		// When compiled for actual run the IL version is used instead
		// This is because there's no way to express the newslot/no-newslot in C#
		interface IInterfaceWithoutNewSlot
		{
			void AbstractNoNewSlot ();
			void AbstractNoNewSlotUnused ();
			void AbstractNewSlot ();
			void AbstractNewSlotUnused ();
			void ImplementedNoNewSlot () { }
			void ImplementedNoNewSlotUnused () { }
			void ImplementedNewSlot () { }
			void ImplementedNewSlotUnused () { }
		}
#endif

		[Kept]
		[KeptInterface (typeof (IInterfaceWithoutNewSlot))]
		[KeptMember (".ctor()")]
		class ImplementationType : IInterfaceWithoutNewSlot
		{
			[Kept]
			public void AbstractNoNewSlot () { }

			public void AbstractNoNewSlotUnused () { }

			[Kept]
			public void AbstractNewSlot () { }

			public void AbstractNewSlotUnused () { }

			// This is not considered an implementation of the interface method
			// CoreCLR doesn't treat it that way either.
			public void ImplementedNoNewSlot () { }

			public void ImplementedNoNewSlotUnused () { }

			[Kept]
			public void ImplementedNewSlot () { }

			public void ImplementedNewSlotUnused () { }

			[Kept]
			public static void Test ()
			{
				IInterfaceWithoutNewSlot instance = new ImplementationType ();
				instance.AbstractNoNewSlot ();
				instance.AbstractNewSlot ();
				instance.ImplementedNoNewSlot ();
				instance.ImplementedNewSlot ();
			}
		}
	}
}
