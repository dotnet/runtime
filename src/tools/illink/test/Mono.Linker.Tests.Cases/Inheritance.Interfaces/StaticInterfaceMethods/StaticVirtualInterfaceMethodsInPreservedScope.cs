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
	public static class StaticVirtualInterfaceMethodsInPreservedScope
	{
		[Kept]
		public static void Main ()
		{
			NotRelevantToVariantCasting.Keep ();
			var t = typeof (RelevantToVariantCasting);
			MarkInterfaceMethods<UsedAsTypeArgument> ();
			var x = new InstantiatedClass ();
		}

		[Kept]
		static void MarkInterfaceMethods<T> () where T : IStaticInterfaceWithDefaultImpls
		{
			T.Property = T.Property + 1;
			T.Method ();
			CallInstanceMethod (null);

			[Kept]
			void CallInstanceMethod (IStaticInterfaceWithDefaultImpls x)
			{
				x.InstanceMethod ();
			}
		}

		[Kept]
		[KeptInterface (typeof (IStaticInterfaceWithDefaultImpls))]
		public class RelevantToVariantCasting : IStaticInterfaceWithDefaultImpls
		{
			[Kept]
			static int IStaticInterfaceWithDefaultImpls.Property { [Kept][KeptOverride (typeof (IStaticInterfaceWithDefaultImpls))] get => 1; [Kept][KeptOverride (typeof (IStaticInterfaceWithDefaultImpls))] set => _ = value; }
			[Kept]
			[KeptOverride (typeof (IStaticInterfaceWithDefaultImpls))]
			static int IStaticInterfaceWithDefaultImpls.Method () => 1;
			int IStaticInterfaceWithDefaultImpls.InstanceMethod () => 0;
		}

		[Kept]
		[KeptInterface (typeof (IStaticInterfaceWithDefaultImpls))]
		public class UsedAsTypeArgument : IStaticInterfaceWithDefaultImpls
		{
			[Kept]
			static int IStaticInterfaceWithDefaultImpls.Property { [Kept][KeptOverride (typeof (IStaticInterfaceWithDefaultImpls))] get => 1; [Kept][KeptOverride (typeof (IStaticInterfaceWithDefaultImpls))] set => _ = value; }
			[Kept]
			[KeptOverride (typeof (IStaticInterfaceWithDefaultImpls))]
			static int IStaticInterfaceWithDefaultImpls.Method () => 1;
			int IStaticInterfaceWithDefaultImpls.InstanceMethod () => 0;
		}

		[Kept]
		public class NotRelevantToVariantCasting : IStaticInterfaceWithDefaultImpls
		{
			[Kept]
			public static void Keep () { }
			static int IStaticInterfaceWithDefaultImpls.Property { get => 1; set => _ = value; }
			static int IStaticInterfaceWithDefaultImpls.Method () => 1;
			int IStaticInterfaceWithDefaultImpls.InstanceMethod () => 0;
		}
		[Kept]
		[KeptMember (".ctor()")]
		[KeptInterface (typeof (IStaticInterfaceWithDefaultImpls))]
		public class InstantiatedClass : IStaticInterfaceWithDefaultImpls
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
			[Kept]
			int IStaticInterfaceWithDefaultImpls.InstanceMethod () => 0;
		}
	}
}

