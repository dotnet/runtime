using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Reflection
{
	[SetupCompileBefore ("reference.dll", new[] { "Dependencies/AssemblyDependency.cs" }, addAsReference: false)]
	[SetupCompileBefore ("library.dll", new[] { "Dependencies/AssemblyDependencyWithReference.cs" }, references: new[] { "reference.dll" }, addAsReference: false)]
	[KeptAssembly ("reference.dll")]
	[KeptAssembly ("library.dll")]
	[KeptTypeInAssembly ("library.dll", "Mono.Linker.Tests.Cases.Reflection.Dependencies.AssemblyDependencyWithReference")]
	[KeptTypeInAssembly ("reference.dll", "Mono.Linker.Tests.Cases.Reflection.Dependencies.AssemblyDependency")]
	public class AssemblyImportedViaReflectionWithReference
	{
		public static void Main ()
		{
			const string newAssemblyType = "Mono.Linker.Tests.Cases.Reflection.Dependencies.AssemblyDependencyWithReference, library, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null";
			var res = Type.GetType (newAssemblyType, true);
			return;
		}
	}
}