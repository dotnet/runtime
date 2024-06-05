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
	/// <summary>
	/// This test exercises the case where a public instance method has a .override directive pointing to an interface method when the interface method is not directly used for the type
	/// Currently, the linker will always mark the .override method for instance methods, so there is not much testing required here.
	/// However, if that were to change, this test should be updated to verify that the .override is removed if the .interfaceImpl is kept.
	/// </summary>
	[SetupLinkerArgument ("--skip-unresolved", "true")]
	[Define ("IL_ASSEMBLY_AVAILABLE")]
	[SetupCompileBefore ("library.dll", new string[] { "Dependencies/InstanceMethods.il" })]
	[Kept]
#if IL_ASSEMBLY_AVAILABLE
	[KeptTypeInAssembly ("library.dll", typeof (InstanceMethods.TypeWithMethodAccessedViaInterface))]
	[KeptTypeInAssembly ("library.dll", typeof (InstanceMethods.TypeWithMethodAccessedDirectly))]
	[KeptTypeInAssembly ("library.dll", typeof (InstanceMethods.TypeWithMethodAccessedViaReflection))]
	[KeptTypeInAssembly ("library.dll", typeof (InstanceMethods.TypeWithMethodKeptByDynamicDependency))]
	[KeptTypeInAssembly ("library.dll", typeof (InstanceMethods.TypeWithMethodCalledDirectlyAndInterfaceUnreferenced))]
	[KeptTypeInAssembly ("library.dll", typeof (InstanceMethods.TypeWithMethodCalledDirectlyAndRecursiveInterfaceUnreferenced))]
	[KeptTypeInAssembly ("library.dll", typeof (InstanceMethods.IInt))]
	[KeptTypeInAssembly ("library.dll", typeof (InstanceMethods.IIntUnreferenced))] // Kept only because of the .override on the public implementation method
	[KeptTypeInAssembly ("library.dll", typeof (InstanceMethods.IIntBase))]
	[KeptTypeInAssembly ("library.dll", typeof (InstanceMethods.IIntDerived))]
	[KeptTypeInAssembly ("library.dll", typeof (InstanceMethods.IGeneric<>))]
	[KeptTypeInAssembly ("library.dll", typeof (InstanceMethods.TypeWithMethodCalledDirectlyAndTwoGenericInterfacesUnreferenced))]
	[KeptMemberInAssembly ("library.dll", typeof (InstanceMethods.TypeWithMethodAccessedViaInterface), ["GetInt()"])]
	[KeptMemberInAssembly ("library.dll", typeof (InstanceMethods.TypeWithMethodAccessedDirectly), ["GetInt()"])]
	[KeptMemberInAssembly ("library.dll", typeof (InstanceMethods.TypeWithMethodAccessedViaReflection), ["GetInt()"])]
	[KeptMemberInAssembly ("library.dll", typeof (InstanceMethods.TypeWithMethodKeptByDynamicDependency), ["GetInt()"])]
	[KeptMemberInAssembly ("library.dll", typeof (InstanceMethods.TypeWithMethodCalledDirectlyAndInterfaceUnreferenced), ["GetInt()"])]
	[KeptMemberInAssembly ("library.dll", typeof (InstanceMethods.TypeWithMethodCalledDirectlyAndRecursiveInterfaceUnreferenced), ["GetInt()"])]
	[KeptMemberInAssembly ("library.dll", typeof (InstanceMethods.IInt), ["GetInt()"])]
	[KeptMemberInAssembly ("library.dll", typeof (InstanceMethods.IIntUnreferenced), ["GetInt()"])]
	[KeptMemberInAssembly ("library.dll", typeof (InstanceMethods.IIntBase), ["GetInt()"])]
	[KeptMemberInAssembly ("library.dll", typeof (InstanceMethods.TypeWithMethodCalledDirectlyAndTwoGenericInterfacesUnreferenced), ["GetIntInt()"])]
	[KeptMemberInAssembly ("library.dll", typeof (InstanceMethods.TypeWithMethodCalledDirectlyAndTwoGenericInterfacesUnreferenced), ["GetIntFloat()"])] // Could be removed
	[KeptInterfaceOnTypeInAssembly ("library.dll", typeof (InstanceMethods.TypeWithMethodAccessedViaInterface), "library.dll", typeof (InstanceMethods.IInt))]
	[KeptInterfaceOnTypeInAssembly ("library.dll", typeof (InstanceMethods.TypeWithMethodAccessedDirectly), "library.dll", typeof (InstanceMethods.IInt))]
	[KeptInterfaceOnTypeInAssembly ("library.dll", typeof (InstanceMethods.TypeWithMethodAccessedViaReflection), "library.dll", typeof (InstanceMethods.IInt))]
	[KeptInterfaceOnTypeInAssembly ("library.dll", typeof (InstanceMethods.TypeWithMethodKeptByDynamicDependency), "library.dll", typeof (InstanceMethods.IInt))]
	[KeptInterfaceOnTypeInAssembly ("library.dll", typeof (InstanceMethods.TypeWithMethodCalledDirectlyAndInterfaceUnreferenced), "library.dll", typeof (InstanceMethods.IIntUnreferenced))]
	[KeptInterfaceOnTypeInAssembly ("library.dll", typeof (InstanceMethods.TypeWithMethodCalledDirectlyAndRecursiveInterfaceUnreferenced), "library.dll", typeof (InstanceMethods.IIntDerived))]
	[KeptInterfaceOnTypeInAssembly ("library.dll", typeof (InstanceMethods.IIntDerived), "library.dll", typeof (InstanceMethods.IIntBase))]
	[KeptInterfaceOnTypeInAssembly ("library.dll", "InstanceMethods/TypeWithMethodCalledDirectlyAndTwoGenericInterfacesUnreferenced", "library.dll", "InstanceMethods/IGeneric`1<System.Int32>")]
	[KeptInterfaceOnTypeInAssembly ("library.dll", "InstanceMethods/TypeWithMethodCalledDirectlyAndTwoGenericInterfacesUnreferenced", "library.dll", "InstanceMethods/IGeneric`1<System.Single>")]
#endif
	public class InstanceMethodsWithOverridesSwept
	{
		[Kept]
		public static void Main ()
		{
#if IL_ASSEMBLY_AVAILABLE
			InstanceMethods.Test ();
			typeof (InstanceMethods.TypeWithMethodAccessedViaReflection).GetMethod ("GetInt").Invoke (null, null);
			new InstanceMethods.TypeWithMethodCalledDirectlyAndInterfaceUnreferenced ().GetInt ();
			new InstanceMethods.TypeWithMethodCalledDirectlyAndRecursiveInterfaceUnreferenced ().GetInt ();
			new InstanceMethods.TypeWithMethodCalledDirectlyAndTwoGenericInterfacesUnreferenced ().GetIntInt ();
#endif
			KeepTypeThroughDynamicDependency ();
		}

#if IL_ASSEMBLY_AVAILABLE
		[DynamicDependency ("GetInt()", typeof (InstanceMethods.TypeWithMethodKeptByDynamicDependency))]
#endif
		[Kept]
		public static void KeepTypeThroughDynamicDependency ()
		{ }
	}
}
