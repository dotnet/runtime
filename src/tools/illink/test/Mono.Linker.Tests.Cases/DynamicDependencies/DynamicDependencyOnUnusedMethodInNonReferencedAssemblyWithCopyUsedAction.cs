using System.Diagnostics.CodeAnalysis;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.DynamicDependencies
{
	[SetupLinkerDefaultAction ("copyused")]
	[SetupCompileBefore ("library.dll", new[] { "Dependencies/DynamicDependencyOnUnusedMethodInNonReferencedAssemblyWithCopyUsedAction_Lib.cs" }, addAsReference: false)]
	[RemovedAssembly ("library.dll")]
	public class DynamicDependencyOnUnusedMethodInNonReferencedAssemblyWithCopyUsedAction
	{
		[Kept (By = Tool.Trimmer)] // Native AOT doesn't really support copyused behavior
		public DynamicDependencyOnUnusedMethodInNonReferencedAssemblyWithCopyUsedAction ()
		{
		}

		public static void Main ()
		{
		}

		[DynamicDependency ("MethodPreservedViaDependencyAttribute()", "Mono.Linker.Tests.Cases.DynamicDependencies.Dependencies.DynamicDependencyOnUnusedMethodInNonReferencedAssemblyWithCopyUsedAction_Lib", "library")]
		[Kept (By = Tool.Trimmer)] // Native AOT doesn't really support copyused behavior
		[KeptAttributeAttribute (typeof (DynamicDependencyAttribute))]
		static void Dependency ()
		{
		}
	}
}
