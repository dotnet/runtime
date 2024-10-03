// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.RecursiveInterfaces
{
	/// <summary>
	/// This tests that when a type implements an interface recursively (via implementations on implemented interfaces),
	/// the interface implementations kept are in type declaration order according to ECMA-335 12.2
	/// </summary>
	[TestCaseRequirements (TestRunCharacteristics.SupportsDefaultInterfaceMethods, "Requires support for default interface methods")]
	[Define ("IL_ASSEMBLY_AVAILABLE")]
	[SetupCompileBefore ("library.dll", new[] { "Dependencies/RecursiveInterfaceTwoImplementationPaths.il" })]
	[SkipILVerify]
#if IL_ASSEMBLY_AVAILABLE
	[KeptTypeInAssembly ("library.dll", typeof(Library.MyClass))]
	[KeptInterfaceOnTypeInAssembly ("library.dll", typeof (Library.MyClass), "library.dll", typeof (Library.I0100))]
	[KeptInterfaceOnTypeInAssembly ("library.dll", typeof (Library.I0100), "library.dll", typeof (Library.I010))]
	[KeptInterfaceOnTypeInAssembly ("library.dll", typeof (Library.I010), "library.dll", typeof (Library.I01))]
	[KeptInterfaceOnTypeInAssembly ("library.dll", typeof (Library.I01), "library.dll", typeof (Library.I0))]
	[KeptTypeInAssembly("library.dll", typeof(Library.I00))]
	[KeptTypeInAssembly("library.dll", typeof(Library.I000))]
	[KeptInterfaceOnTypeInAssembly("library.dll", typeof (Library.MyClass), "library.dll", typeof (Library.I000))]
#endif
	public class RecursiveInterfaceKept
	{
		public static void Main ()
		{
#if IL_ASSEMBLY_AVAILABLE
			Library.I0 _ = new Library.MyClass();
#endif
		}
	}
}
