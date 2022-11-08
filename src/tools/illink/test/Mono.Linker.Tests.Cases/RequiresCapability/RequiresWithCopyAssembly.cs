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
using Mono.Linker.Tests.Cases.RequiresCapability.Dependencies;

namespace Mono.Linker.Tests.Cases.RequiresCapability
{
	[SetupLinkerAction ("copy", "lib")]
	[SetupCompileBefore ("lib.dll", new[] { "Dependencies/RequiresInCopyAssembly.cs" })]
	[KeptAllTypesAndMembersInAssembly ("lib.dll")]
	[SkipKeptItemsValidation]
	// Annotated members on a copied assembly should not produce any warnings
	// unless directly called or referenced through reflection.
	[LogDoesNotContain ("--UncalledMethod--")]
	[LogDoesNotContain ("--getter UnusedProperty--")]
	[LogDoesNotContain ("--setter UnusedProperty--")]
	[LogDoesNotContain ("--UnusedBaseTypeCctor--")]
	[LogDoesNotContain ("--UnusedVirtualMethod1--")]
	[LogDoesNotContain ("--UnusedVirtualMethod2--")]
	[LogDoesNotContain ("--IUnusedInterface.UnusedMethod--")]
	[LogDoesNotContain ("--UnusedImplementationClass.UnusedMethod--")]
	// [LogDoesNotContain ("UnusedVirtualMethod2")] // https://github.com/dotnet/linker/issues/2106

	[ExpectedNoWarnings]
	class RequiresWithCopyAssembly
	{
		[ExpectedWarning ("IL2026", "--IDerivedInterface.MethodInDerivedInterface--")]
		[ExpectedWarning ("IL2026", "--IBaseInterface.MethodInBaseInterface--")]
		public static void Main ()
		{
			TestRequiresInMethodFromCopiedAssembly ();
			TestRequiresThroughReflectionInMethodFromCopiedAssembly ();
			TestRequiresInDynamicallyAccessedMethodFromCopiedAssembly (typeof (RequiresInCopyAssembly.IDerivedInterface));
		}

		[ExpectedWarning ("IL2026", "--Method--")]
		[ExpectedWarning ("IL3002", "--Method--", ProducedBy = ProducedBy.Analyzer | ProducedBy.NativeAot)]
		[ExpectedWarning ("IL3050", "--Method--", ProducedBy = ProducedBy.Analyzer | ProducedBy.NativeAot)]
		static void TestRequiresInMethodFromCopiedAssembly ()
		{
			var tmp = new RequiresInCopyAssembly ();
			tmp.Method ();
		}

		[ExpectedWarning ("IL2026", "--MethodCalledThroughReflection--")]
		static void TestRequiresThroughReflectionInMethodFromCopiedAssembly ()
		{
			typeof (RequiresInCopyAssembly)
				.GetMethod ("MethodCalledThroughReflection", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
				.Invoke (null, new object[0]);
		}

		static void TestRequiresInDynamicallyAccessedMethodFromCopiedAssembly (
		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)] Type type)
		{
		}
	}
}
