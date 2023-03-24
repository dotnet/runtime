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
	[SetupLinkerArgument ("-a", "test.exe", "library")]
	public static class StaticVirtualInterfaceMethodsInPreservedScopeLibrary
	{
		[Kept]
		public static void Main ()
		{
		}

		[Kept]
		[KeptMember (".ctor()")]
		[KeptInterface (typeof (IStaticInterfaceWithDefaultImpls))]
		public class ImplementVirtualIface : IStaticInterfaceWithDefaultImpls
		{
			[Kept]
			static int IStaticInterfaceWithDefaultImpls.Property {
				[Kept]
				[KeptOverride (typeof (IStaticInterfaceWithDefaultImpls))]
				get => 1; [Kept]
				[KeptOverride (typeof (IStaticInterfaceWithDefaultImpls))]
				set => _ = value;
			}
			[Kept]
			[KeptOverride (typeof (IStaticInterfaceWithDefaultImpls))]
			static int IStaticInterfaceWithDefaultImpls.Method () => 1;
			[Kept]
			[KeptOverride (typeof (IStaticInterfaceWithDefaultImpls))]
			int IStaticInterfaceWithDefaultImpls.InstanceMethod () => 0;
		}

		[Kept]
		[KeptInterface (typeof (IStaticInterfaceWithDefaultImpls))]
		public class ImplementVirtualIfaceProtectedCtor : IStaticInterfaceWithDefaultImpls
		{
			[Kept]
			protected ImplementVirtualIfaceProtectedCtor () { }
			[Kept]
			static int IStaticInterfaceWithDefaultImpls.Property {
				[Kept]
				[KeptOverride (typeof (IStaticInterfaceWithDefaultImpls))]
				get => 1; [Kept]
				[KeptOverride (typeof (IStaticInterfaceWithDefaultImpls))]
				set => _ = value;
			}
			[Kept]
			[KeptOverride (typeof (IStaticInterfaceWithDefaultImpls))]
			static int IStaticInterfaceWithDefaultImpls.Method () => 1;
			[Kept]
			[KeptOverride (typeof (IStaticInterfaceWithDefaultImpls))]
			int IStaticInterfaceWithDefaultImpls.InstanceMethod () => 0;
		}

		[Kept]
		[KeptInterface (typeof (IStaticInterfaceWithDefaultImpls))]
		public class ImplementVirtualIfaceUninstantiated : IStaticInterfaceWithDefaultImpls
		{
			private ImplementVirtualIfaceUninstantiated () { }

			[Kept]
			static int IStaticInterfaceWithDefaultImpls.Property {
				[Kept]
				[KeptOverride (typeof (IStaticInterfaceWithDefaultImpls))]
				get => 1; [Kept]
				[KeptOverride (typeof (IStaticInterfaceWithDefaultImpls))]
				set => _ = value;
			}

			[Kept]
			[KeptOverride (typeof (IStaticInterfaceWithDefaultImpls))]
			static int IStaticInterfaceWithDefaultImpls.Method () => 1;
			// Type has private ctor, so instance methods can be removed. Since there's a default impl, we can remove this interface method
			int IStaticInterfaceWithDefaultImpls.InstanceMethod () => 0;
		}

		[Kept]
		[KeptInterface (typeof (IStaticInterfaceWithDefaultImpls))]
		public class ImplicitImplementVirtualIfaceUninstantiated : IStaticInterfaceWithDefaultImpls
		{
			private ImplicitImplementVirtualIfaceUninstantiated () { }
			[Kept]
			public static int Property {
				[Kept]
				[KeptOverride (typeof (IStaticInterfaceWithDefaultImpls))]
				get => 1; [Kept]
				[KeptOverride (typeof (IStaticInterfaceWithDefaultImpls))]
				set => _ = value;
			}
			[Kept]
			[KeptOverride (typeof (IStaticInterfaceWithDefaultImpls))]
			public static int Method () => 1;
			[Kept]
			public int InstanceMethod () => 0;
		}
	}
}

