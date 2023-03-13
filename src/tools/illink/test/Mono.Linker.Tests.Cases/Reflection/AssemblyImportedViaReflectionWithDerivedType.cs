using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.Reflection.Dependencies;

namespace Mono.Linker.Tests.Cases.Reflection
{
	[SetupCompileBefore ("base.dll", new[] { "Dependencies/AssemblyImportedViaReflectionWithDerivedType_Base.cs" })]
	[SetupCompileBefore ("reflection.dll", new[] { "Dependencies/AssemblyImportedViaReflectionWithDerivedType_Reflect.cs" }, references: new[] { "base.dll" }, addAsReference: false)]
	[KeptAssembly ("base.dll")]
	[KeptAssembly ("reflection.dll")]
	[KeptMemberInAssembly ("base.dll", typeof (AssemblyImportedViaReflectionWithDerivedType_Base), "Method()")]
	[KeptMemberInAssembly ("reflection.dll", "Mono.Linker.Tests.Cases.Reflection.Dependencies.AssemblyImportedViaReflectionWithDerivedType_Reflect", "Method()")]
	public class AssemblyImportedViaReflectionWithDerivedType
	{
		public static void Main ()
		{
			// Cause a the new assembly to be included via reflection usage
			const string newAssemblyType = "Mono.Linker.Tests.Cases.Reflection.Dependencies.AssemblyImportedViaReflectionWithDerivedType_Reflect, reflection, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null";
			var res = Type.GetType (newAssemblyType, true);

			// Foo and the reflection assembly both have a class the inherits from the base type.
			// by using `Method` here and marking the reflection type above, we've introduced a requirement that `Method` be marked on the type in the reflection assembly as well 
			var obj = new Foo ();
			var val = obj.Method ();
		}

		[Kept]
		[KeptMember (".ctor()")]
		[KeptBaseType (typeof (AssemblyImportedViaReflectionWithDerivedType_Base))]
		class Foo : AssemblyImportedViaReflectionWithDerivedType_Base
		{
			[Kept]
			public override string Method ()
			{
				return "Foo";
			}
		}
	}
}