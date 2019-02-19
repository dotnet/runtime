using System.Runtime.CompilerServices;
using Mono.Linker.Tests.Cases.PreserveDependencies.Dependencies;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.PreserveDependencies {
	/// <summary>
	/// This test is here to ensure that link xml embedded in an assembly used by a [PreserveDependency] is not processed if the dependency is not used
	/// </summary>
	[IncludeBlacklistStep (true)]
	[SetupCompileBefore ("base.dll", new [] { "Dependencies/PreserveDependencyMethodInNonReferencedAssemblyBase.cs" })]
	[SetupCompileBefore (
		"PreserveDependencyMethodInNonReferencedAssemblyLibrary.dll",
		new [] { "Dependencies/PreserveDependencyMethodInNonReferencedAssemblyLibrary.cs" },
		references: new [] { "base.dll" },
		resources: new [] {"Dependencies/PreserveDependencyMethodInNonReferencedAssemblyLibrary.xml"},
		addAsReference: false)]
	[KeptAssembly ("base.dll")]
	[RemovedAssembly ("PreserveDependencyMethodInNonReferencedAssemblyLibrary.dll")]
	public class PreserveDependencyOnUnusedMethodInNonReferencedAssemblyWithEmbeddedXml {
		public static void Main ()
		{
			var obj = new Foo ();
			var val = obj.Method ();
		}

		[PreserveDependency (".ctor()", "Mono.Linker.Tests.Cases.PreserveDependencies.Dependencies.PreserveDependencyMethodInNonReferencedAssemblyLibrary", "PreserveDependencyMethodInNonReferencedAssemblyLibrary")]
		static void Dependency ()
		{
		}

		[Kept]
		[KeptMember (".ctor()")]
		[KeptBaseType (typeof (PreserveDependencyMethodInNonReferencedAssemblyBase))]
		class Foo : PreserveDependencyMethodInNonReferencedAssemblyBase {
			[Kept]
			public override string Method ()
			{
				return "Foo";
			}
		}
	}
}