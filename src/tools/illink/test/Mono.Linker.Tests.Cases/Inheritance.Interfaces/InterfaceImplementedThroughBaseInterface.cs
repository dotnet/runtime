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
	[SetupLinkerArgument ("-a", "test.exe", "library")]
	[SetupLinkerArgument ("-a", "library.dll", "library")]
	[TestCaseRequirements (TestRunCharacteristics.SupportsDefaultInterfaceMethods, "Requires support for default interface methods")]
	[Define ("IL_ASSEMBLY_AVAILABLE")]
	[SetupCompileBefore ("library.dll", new[] { "Dependencies/InterfaceImplementedThroughBaseInterface.il" })]
	[SkipILVerify]

#if IL_ASSEMBLY_AVAILABLE
	[KeptMemberInAssembly ("library.dll", typeof(C), "IBase.M()")]
#endif
	[KeptMember(".ctor()")]
	public class InterfaceImplementedThroughBaseInterface
	{
		public static void Main ()
		{
		}
	}
}


