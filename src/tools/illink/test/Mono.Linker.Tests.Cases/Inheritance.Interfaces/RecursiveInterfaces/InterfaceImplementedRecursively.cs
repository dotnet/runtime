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
	[SetupLinkerArgument ("--skip-unresolved", "true")]
	[TestCaseRequirements (TestRunCharacteristics.SupportsDefaultInterfaceMethods, "Requires support for default interface methods")]
	[Define ("IL_ASSEMBLY_AVAILABLE")]
	[SetupCompileBefore ("library.dll", new[] { "Dependencies/InterfaceImplementedRecursively.il" })]
	[SkipILVerify]
#if IL_ASSEMBLY_AVAILABLE
	[KeptTypeInAssembly ("library.dll", typeof(Program.IBase))]
	[KeptTypeInAssembly ("library.dll", typeof(Program.IMiddle))]
	[KeptInterfaceOnTypeInAssembly ("library.dll", typeof (Program.IMiddle), "library.dll", typeof (Program.IBase))]
	[KeptTypeInAssembly ("library.dll", typeof(Program.IDerived))]
	[KeptInterfaceOnTypeInAssembly ("library.dll", typeof (Program.IDerived), "library.dll", typeof (Program.IMiddle))]
	[KeptTypeInAssembly ("library.dll", typeof(Program.C))]
	[KeptInterfaceOnTypeInAssembly ("library.dll", typeof (Program.C), "library.dll", typeof (Program.IDerived))]
#endif
	/// <summary>
	/// This test case is to verify that the linker will keep all the metadata necessary for C to implement IBase when an interfaceImpl isn't directly on C.
	/// </summary>
	class InterfaceImplementedRecursively
	{
		public static void Main()
		{

#if IL_ASSEMBLY_AVAILABLE
			Program.IBase b = null;
			object c = new Program.C();

#endif
		}
	}
	// interface IBase {}
	// interface IMiddle : IBase {}
	// interface IDerived : IMiddle {}
	// class C : IDerived
	// {
	// }
}
