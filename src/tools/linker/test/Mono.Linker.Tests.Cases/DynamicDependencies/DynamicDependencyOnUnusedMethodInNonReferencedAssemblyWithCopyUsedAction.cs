using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.DynamicDependencies
{
	[SetupLinkerUserAction ("copyused")]
	[SetupCompileBefore ("library.dll", new[] { "Dependencies/DynamicDependencyOnUnusedMethodInNonReferencedAssemblyWithCopyUsedAction_Lib.cs" }, addAsReference: false)]
	[RemovedAssembly ("library.dll")]
	public class DynamicDependencyOnUnusedMethodInNonReferencedAssemblyWithCopyUsedAction
	{
		public static void Main ()
		{
		}

		[DynamicDependency ("MethodPreservedViaDependencyAttribute()", "Mono.Linker.Tests.Cases.DynamicDependencies.Dependencies.DynamicDependencyOnUnusedMethodInNonReferencedAssemblyWithCopyUsedAction_Lib", "library")]
		static void Dependency ()
		{
		}
	}
}