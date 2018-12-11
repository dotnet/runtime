using System.Runtime.CompilerServices;
using Mono.Linker.Tests.Cases.PreserveDependencies.Dependencies;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.PreserveDependencies {
	/// <summary>
	/// This is an acceptable bug with the currently implementation.  Embedded link xml files will not be processed
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
	[RemovedMemberInAssembly ("PreserveDependencyMethodInNonReferencedAssemblyLibrary.dll", "Mono.Linker.Tests.Cases.PreserveDependencies.Dependencies.PreserveDependencyMethodInNonReferencedAssemblyLibrary", "UnusedMethod()")]
	public class PreserveDependencyMethodInNonReferencedAssemblyWithEmbeddedXml {
		public static void Main ()
		{
			var obj = new Foo ();
			var val = obj.Method ();
			Dependency ();
		}

		[Kept]
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