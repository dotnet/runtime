// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.RecursiveInterfaces
{
	/// <summary>
	/// This tests that when a type implements an interface recursively (via implementations on implemented interfaces),
	/// the shortest chain of interface implementations required to keep the implementation is marked
	/// MyFoo => I000 => I00 => I0 (3 interfaceImpl long chain)
	/// MyFoo => I0100 => I010 => I01 => I0 (4 interfaceImpl long chain)
	/// </summary>
	[TestCaseRequirements (TestRunCharacteristics.SupportsDefaultInterfaceMethods, "Requires support for default interface methods")]
	[Define ("IL_ASSEMBLY_AVAILABLE")]
	[SetupCompileBefore ("library.dll", new[] { "Dependencies/RecursiveInterfaceTwoImplementationPaths.il" })]
	[SkipILVerify]
#if IL_ASSEMBLY_AVAILABLE
	[KeptTypeInAssembly ("library.dll", typeof(Library.MyClass))]
	[KeptInterfaceOnTypeInAssembly ("library.dll", typeof (Library.MyClass), "library.dll", typeof (Library.I000))]
	[KeptInterfaceOnTypeInAssembly ("library.dll", typeof (Library.I000), "library.dll", typeof (Library.I00))]
	[KeptInterfaceOnTypeInAssembly ("library.dll", typeof (Library.I00), "library.dll", typeof (Library.I0))]
	[RemovedTypeInAssembly("library.dll", typeof(Library.I01))]
	[RemovedTypeInAssembly("library.dll", typeof(Library.I010))]
	[RemovedTypeInAssembly("library.dll", typeof(Library.I0100))]
	[RemovedInterfaceOnTypeInAssembly("library.dll", typeof (Library.MyClass), "library.dll", typeof (Library.I0100))]
#endif
	public class RecursiveInterfaceKept
	{
		public static void Main()
		{
#if IL_ASSEMBLY_AVAILABLE
			Library.I0 _ = new Library.MyClass();
#endif
		}
	}
}
