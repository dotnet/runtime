using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.DynamicDependencies
{
	[SetupLinkerDefaultAction ("copyused")]
	[SetupCompileBefore ("library.dll", new[] { "Dependencies/DynamicDependencyOnUnusedMethodInNonReferencedAssemblyWithCopyUsedAction_Lib.cs" }, addAsReference: false)]
	[RemovedAssembly ("library.dll")]
	public class DynamicDependencyOnUnusedMethodInNonReferencedAssemblyWithCopyUsedAction
	{
#if NETCOREAPP
		[Kept]
		public DynamicDependencyOnUnusedMethodInNonReferencedAssemblyWithCopyUsedAction ()
		{
		}
#endif

		public static void Main ()
		{
		}

		[DynamicDependency ("MethodPreservedViaDependencyAttribute()", "Mono.Linker.Tests.Cases.DynamicDependencies.Dependencies.DynamicDependencyOnUnusedMethodInNonReferencedAssemblyWithCopyUsedAction_Lib", "library")]
#if NETCOREAPP
		[Kept]
		[KeptAttributeAttribute (typeof (DynamicDependencyAttribute))]
#endif
		static void Dependency ()
		{
		}
	}
}