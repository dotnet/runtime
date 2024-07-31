// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.RecursiveInterfaces
{
	[SetupLinkerArgument ("--skip-unresolved", "true")]
	[TestCaseRequirements (TestRunCharacteristics.SupportsDefaultInterfaceMethods, "Requires support for default interface methods")]
	[Define ("IL_ASSEMBLY_AVAILABLE")]
	[SetupCompileBefore ("library.dll", new[] { "Dependencies/GenericInterfaceImplementedRecursively.il" })]
	[SkipILVerify]
#if IL_ASSEMBLY_AVAILABLE
	[KeptTypeInAssembly ("library.dll", typeof(Program.IBase<>))]
	[KeptTypeInAssembly ("library.dll", typeof(Program.IMiddle<>))]
	[KeptInterfaceOnTypeInAssembly ("library.dll", "Program/IMiddle`1", "library.dll", "Program/IBase`1<T>")]
	[KeptTypeInAssembly ("library.dll", typeof(Program.IDerived<>))]
	[KeptInterfaceOnTypeInAssembly ("library.dll", "Program/IDerived`1", "library.dll", "Program/IMiddle`1<T>")]
	[KeptTypeInAssembly ("library.dll", typeof(Program.C))]
	[KeptInterfaceOnTypeInAssembly ("library.dll", "Program/C", "library.dll", "Program/IDerived`1<System.Int32>")]
#endif
	/// <summary>
	/// This test case is to verify that the linker will keep all the metadata necessary for C to implement IBase when an interfaceImpl isn't directly on C.
	/// </summary>
	class GenericInterfaceImplementedRecursively
	{
		public static void Main ()
		{

#if IL_ASSEMBLY_AVAILABLE
			Program.IBase<int> _ = null;
			_ = new Program.C();
#endif
		}
	}
}
