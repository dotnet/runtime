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
	public static class StaticVirtualInterfaceMethodsLibrary
	{
		[Kept]
		public static void Main ()
		{
		}

		[Kept]
		public static class IfaceMethodInPreserveScope
		{
			[Kept]
			[KeptMember (".ctor()")]
			[KeptInterface (typeof (IStaticInterfaceWithDefaultImpls))]
			public class ExplcitImplementations : IStaticInterfaceWithDefaultImpls
			{
				[Kept]
				static int IStaticInterfaceWithDefaultImpls.Property { [Kept][KeptOverride (typeof (IStaticInterfaceWithDefaultImpls))] get => 1; [Kept][KeptOverride (typeof (IStaticInterfaceWithDefaultImpls))] set => _ = value; }
				[Kept]
				[KeptOverride (typeof (IStaticInterfaceWithDefaultImpls))]
				static int IStaticInterfaceWithDefaultImpls.Method () => 1;
				[Kept]
				[KeptOverride (typeof (IStaticInterfaceWithDefaultImpls))]
				int IStaticInterfaceWithDefaultImpls.InstanceMethod () => 0;
			}
		}

		[Kept]
		public static class AbstractStaticInPreserveScope
		{
			[Kept]
			[KeptMember (".ctor()")]
			[KeptInterface (typeof (IStaticAbstractMethods))]
			public class ExplcitImplementations : IStaticAbstractMethods
			{
				[Kept]
				static int IStaticAbstractMethods.Property { [Kept][KeptOverride (typeof (IStaticAbstractMethods))] get => 1; [Kept][KeptOverride (typeof (IStaticAbstractMethods))] set => _ = value; }
				[Kept]
				[KeptOverride (typeof (IStaticAbstractMethods))]
				static int IStaticAbstractMethods.Method () => 1;
				[Kept]
				[KeptOverride (typeof (IStaticAbstractMethods))]
				int IStaticAbstractMethods.InstanceMethod () => 0;
			}
		}
	}
}

