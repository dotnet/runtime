using System;
using System.Reflection;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Reflection
{
	[SetupCompileBefore ("library.dll", new[] { "Dependencies/AssemblyDependency.cs" }, addAsReference: false)]
	[KeptAssembly ("library.dll")]
	[KeptTypeInAssembly ("library.dll", "Mono.Linker.Tests.Cases.Reflection.Dependencies.AssemblyDependency")]
	public class AssemblyImportedViaReflection
	{
		public static void Main ()
		{
			const string newAssemblyType = "Mono.Linker.Tests.Cases.Reflection.Dependencies.AssemblyDependency, library, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null";
			var res = Type.GetType (newAssemblyType, true);
			return;
		}
	}
}