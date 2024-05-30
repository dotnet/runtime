// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.StaticInterfaceMethods
{
	[SetupLinkerArgument ("--skip-unresolved", "true")]
	[Define ("IL_ASSEMBLY_AVAILABLE")]
	[SetupCompileBefore ("library.dll", new string[] { "Dependencies/InstanceMethods.il" })]
	[Kept]
#if IL_ASSEMBLY_AVAILABLE
	[KeptTypeInAssembly ("library.dll", typeof (InstanceMethods.TypeWithMethodAccessedViaInterface))]
	[KeptTypeInAssembly ("library.dll", typeof (InstanceMethods.TypeWithMethodAccessedDirectly))]
	[KeptTypeInAssembly ("library.dll", typeof (InstanceMethods.TypeWithMethodAccessedViaReflection))]
	[KeptTypeInAssembly ("library.dll", typeof (InstanceMethods.TypeWithMethodKeptByDynamicDependency))]
	[KeptMemberInAssembly ("library.dll", typeof (InstanceMethods.TypeWithMethodAccessedViaInterface), ["GetInt()"])]
	[KeptMemberInAssembly ("library.dll", typeof (InstanceMethods.TypeWithMethodAccessedDirectly), ["GetInt()"])]
	[KeptMemberInAssembly ("library.dll", typeof (InstanceMethods.TypeWithMethodAccessedViaReflection), ["GetInt()"])]
	[KeptMemberInAssembly ("library.dll", typeof (InstanceMethods.TypeWithMethodKeptByDynamicDependency), ["GetInt()"])]
	[KeptInterfaceOnTypeInAssembly ("library.dll", typeof (InstanceMethods.TypeWithMethodAccessedViaInterface), "library.dll", typeof (InstanceMethods.IInt))]
	[KeptInterfaceOnTypeInAssembly ("library.dll", typeof (InstanceMethods.TypeWithMethodAccessedDirectly), "library.dll", typeof (InstanceMethods.IInt))]
	[KeptInterfaceOnTypeInAssembly ("library.dll", typeof (InstanceMethods.TypeWithMethodAccessedViaReflection), "library.dll", typeof (InstanceMethods.IInt))]
	[KeptInterfaceOnTypeInAssembly ("library.dll", typeof (InstanceMethods.TypeWithMethodKeptByDynamicDependency), "library.dll", typeof (InstanceMethods.IInt))]
#endif
	public class InstanceMethodsWithOverridesSwept
	{
		[Kept]
		public static void Main ()
		{
#if IL_ASSEMBLY_AVAILABLE
			InstanceMethods.Test ();
			typeof (InstanceMethods.TypeWithMethodAccessedViaReflection).GetMethod ("GetInt").Invoke (null, null);
#endif
			KeepMethodOnType ();
		}

#if IL_ASSEMBLY_AVAILABLE
		[DynamicDependency ("GetInt()", typeof (InstanceMethods.TypeWithMethodKeptByDynamicDependency))]
#endif
		[Kept]
		public static void KeepMethodOnType ()
		{ }
	}
}
