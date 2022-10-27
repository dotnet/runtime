// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono;
using Mono.Linker;
using Mono.Linker.Tests;
using Mono.Linker.Tests.Cases;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.Inheritance;
using Mono.Linker.Tests.Cases.Inheritance.Interfaces;
using Mono.Linker.Tests.Cases.Inheritance.Interfaces.StaticInterfaceMethods;
using Mono.Linker.Tests.Cases.Inheritance.Interfaces.StaticInterfaceMethods.Dependencies;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.StaticInterfaceMethods
{
	[SetupCompileBefore ("library.dll", new[] { "Dependencies/Library.cs" })]
	[SetupLinkerAction ("skip", "library")]
	class UnusedInterfacesInPreservedScope
	{
		[Kept]
		public static void Main ()
		{
			Test ();
		}

		[Kept]
		class MyType : IStaticInterfaceWithDefaultImpls
		{
			public static int Property { get => 0; set => _ = value; }
			public static int Method () => 0;
			public int InstanceMethod () => 0;
		}

		// Keep MyType without marking it relevant to variant casting
		[Kept]
		static void KeepMyType (MyType x)
		{ }

		[Kept]
		static void Test ()
		{
			KeepMyType (null);
		}
	}
}
