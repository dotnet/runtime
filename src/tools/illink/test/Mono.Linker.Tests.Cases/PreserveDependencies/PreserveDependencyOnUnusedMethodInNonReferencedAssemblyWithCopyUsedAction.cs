﻿using System.Runtime.CompilerServices;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.PreserveDependencies
{
	[SetupLinkerDefaultAction ("copyused")]
	[SetupCompileBefore ("FakeSystemAssembly.dll", new[] { "Dependencies/PreserveDependencyAttribute.cs" })]
	[SetupCompileBefore ("library.dll", new[] { "Dependencies/PreserveDependencyOnUnusedMethodInNonReferencedAssemblyWithCopyUsedAction_Lib.cs" }, addAsReference: false)]
	[RemovedAssembly ("library.dll")]
	public class PreserveDependencyOnUnusedMethodInNonReferencedAssemblyWithCopyUsedAction
	{
		[Kept]
		private PreserveDependencyOnUnusedMethodInNonReferencedAssemblyWithCopyUsedAction ()
		{
		}

		public static void Main ()
		{
		}

		[PreserveDependency ("MethodPreservedViaDependencyAttribute()", "Mono.Linker.Tests.Cases.PreserveDependencies.Dependencies.PreserveDependencyOnUnusedMethodInNonReferencedAssemblyWithCopyUsedAction_Lib", "library")]
		[Kept]
		[KeptAttributeAttribute (typeof (PreserveDependencyAttribute))]
		static void Dependency ()
		{
		}
	}
}