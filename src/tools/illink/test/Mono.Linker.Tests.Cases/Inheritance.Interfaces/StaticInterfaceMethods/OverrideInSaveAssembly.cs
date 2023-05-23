// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.Inheritance.Interfaces.StaticInterfaceMethods.Dependencies;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.StaticInterfaceMethods
{
	[KeptMemberInAssembly ("library", typeof (IStaticAbstractMethods), "Method()", "get_Property()", "Property", "set_Property(System.Int32)")]
    // Add a custom step which sets the assembly action of the test to "save"    
    [SetupCompileBefore ("CustomStepSaveAssembly.dll", new[] { "Dependencies/CustomStepSaveAssembly.cs" }, new[] { "illink.dll", "Mono.Cecil.dll", "netstandard.dll" })]
    [SetupLinkerArgument ("--custom-step", "-MarkStep:CustomStepSaveAssembly,CustomStepSaveAssembly.dll")]
	[SetupCompileBefore ("library.dll", new[] { "Dependencies/Library.cs" })]
	[SetupLinkerAction ("link", "library")]
	/// <summary>
	///	Regression test for issue: https://github.com/dotnet/runtime/issues/86242
	///	OverridesStaticInterfaceMethods.Method() (and Property.set/get) has an entry in .overrides pointing to IStaticAbstractMethods.Method.
	///	IStaticAbstractMethods.Method() isn't referenced anywhere else and isn't otherwise needed.
	///	Usually the interface method could be removed, and the pointer to it in the .overrides metadata would be removed
	///	However, since OverridesStaticInterfaceMethods is in a 'save' assembly, the .overrides metadata isn't swept. If we remove the method from the interface,
	///	we have a "dangling reference" which makes the metadata invalid.
	/// </summary>
	static class OverrideInSaveAssembly
	{
		[Kept]
		public static void Main ()
		{
			OverridesStaticInterfaceMethods.Property = OverridesStaticInterfaceMethods.Method ();
			var x = OverridesStaticInterfaceMethods.Property;
		}
		[Kept]
		[KeptMember (".ctor()")]
		[KeptInterface (typeof (IStaticAbstractMethods))]
		class OverridesStaticInterfaceMethods : IStaticAbstractMethods
		{
			[Kept]
			public static int Property {
				[Kept]
				[KeptOverride (typeof (IStaticAbstractMethods))]
				get => throw new NotImplementedException ();
				[Kept]
				[KeptOverride (typeof (IStaticAbstractMethods))]
				set => throw new NotImplementedException ();
			}
			[Kept]
			[KeptOverride (typeof (IStaticAbstractMethods))]

			public static int Method () => throw new NotImplementedException ();
			[Kept]
			public int InstanceMethod () => throw new NotImplementedException ();
		}
	}
}
