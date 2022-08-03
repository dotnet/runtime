// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.Inheritance.Interfaces.StaticInterfaceMethods.Dependencies;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.StaticInterfaceMethods
{
	[SetupCompileBefore ("library.dll", new[] { "Dependencies/Library.cs" })]
	[SetupLinkerAction ("skip", "library")]
	[SetupLinkerArgument ("-a", "test.exe")]
	public static class StaticInterfaceMethodsInPreservedScope
	{
		[Kept]
		public static void Main ()
		{
			var x = typeof (VirtualInterfaceMethods);
			x = typeof (AbstractInterfaceMethods);
			x = typeof (IStaticInterfaceWithDefaultImpls);
			x = typeof (IStaticAbstractMethods);
		}

		[Kept]
		[KeptInterface (typeof (IStaticInterfaceWithDefaultImpls))]
		public class VirtualInterfaceMethods : IStaticInterfaceWithDefaultImpls
		{
			[Kept]
			static int IStaticInterfaceWithDefaultImpls.Property {
				[Kept]
				[KeptOverride (typeof (IStaticInterfaceWithDefaultImpls))]
				get => 1;
				[Kept]
				[KeptOverride (typeof (IStaticInterfaceWithDefaultImpls))]
				set => _ = value;
			}

			[Kept]
			[KeptOverride (typeof (IStaticInterfaceWithDefaultImpls))]
			static int IStaticInterfaceWithDefaultImpls.Method () => 1;

			// There is a default implementation and the type isn't instantiated, so we don't need this
			int IStaticInterfaceWithDefaultImpls.InstanceMethod () => 0;
		}

		[Kept]
		[KeptInterface (typeof (IStaticAbstractMethods))]
		public class AbstractInterfaceMethods : IStaticAbstractMethods
		{
			[Kept]
			static int IStaticAbstractMethods.Property {
				[Kept]
				[KeptOverride (typeof (IStaticAbstractMethods))]
				get => 1; [Kept]
				[KeptOverride (typeof (IStaticAbstractMethods))]
				set => _ = value;
			}

			[Kept]
			[KeptOverride (typeof (IStaticAbstractMethods))]
			static int IStaticAbstractMethods.Method () => 1;

			[Kept]
			[KeptOverride (typeof (IStaticAbstractMethods))]
			int IStaticAbstractMethods.InstanceMethod () => 0;
		}
	}
}

