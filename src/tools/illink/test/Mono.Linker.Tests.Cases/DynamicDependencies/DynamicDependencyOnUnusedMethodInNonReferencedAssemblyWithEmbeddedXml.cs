using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Mono.Linker.Tests.Cases.DynamicDependencies.Dependencies;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.DynamicDependencies
{
	/// <summary>
	/// This test is here to ensure that link xml embedded in an assembly used by a [DynamicDependency] is not processed if the dependency is not used
	/// </summary>
	[IgnoreDescriptors (false)]
	[SetupCompileBefore ("base.dll", new[] { "Dependencies/DynamicDependencyMethodInNonReferencedAssemblyBase.cs" })]
	[SetupCompileBefore (
		"DynamicDependencyMethodInNonReferencedAssemblyLibrary.dll",
		new[] { "Dependencies/DynamicDependencyMethodInNonReferencedAssemblyLibrary.cs" },
		references: new[] { "base.dll" },
		resources: new object[] { "Dependencies/DynamicDependencyMethodInNonReferencedAssemblyLibrary.xml" },
		addAsReference: false)]
	[KeptAssembly ("base.dll")]
	[RemovedAssembly ("DynamicDependencyMethodInNonReferencedAssemblyLibrary.dll")]
	public class DynamicDependencyOnUnusedMethodInNonReferencedAssemblyWithEmbeddedXml
	{
		public static void Main ()
		{
			var obj = new Foo ();
			var val = obj.Method ();
		}

		[DynamicDependency ("#ctor()", "Mono.Linker.Tests.Cases.DynamicDependencies.Dependencies.DynamicDependencyMethodInNonReferencedAssemblyLibrary", "DynamicDependencyMethodInNonReferencedAssemblyLibrary")]
		static void Dependency ()
		{
		}

		[Kept]
		[KeptMember (".ctor()")]
		[KeptBaseType (typeof (DynamicDependencyMethodInNonReferencedAssemblyBase))]
		class Foo : DynamicDependencyMethodInNonReferencedAssemblyBase
		{
			[Kept]
			public override string Method ()
			{
				return "Foo";
			}
		}
	}
}