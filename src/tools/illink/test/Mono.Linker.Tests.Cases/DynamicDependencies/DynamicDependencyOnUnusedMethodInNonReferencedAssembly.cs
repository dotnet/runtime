using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Mono.Linker.Tests.Cases.DynamicDependencies.Dependencies;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.DynamicDependencies
{
	[SetupCompileBefore ("base.dll", new[] { "Dependencies/DynamicDependencyMethodInNonReferencedAssemblyBase.cs" })]
	[SetupCompileBefore ("library.dll", new[] { "Dependencies/DynamicDependencyMethodInNonReferencedAssemblyLibrary.cs" }, references: new[] { "base.dll" }, addAsReference: false)]
	[KeptAssembly ("base.dll")]
	[RemovedAssembly ("library.dll")]
	[KeptMemberInAssembly ("base.dll", typeof (DynamicDependencyMethodInNonReferencedAssemblyBase), "Method()")]
	public class DynamicDependencyOnUnusedMethodInNonReferencedAssembly
	{
		public static void Main ()
		{
			var obj = new Foo ();
			var val = obj.Method ();
		}

		[DynamicDependency (".ctor()", "Mono.Linker.Tests.Cases.DynamicDependencies.Dependencies.DynamicDependencyMethodInNonReferencedAssemblyLibrary", "library")]
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