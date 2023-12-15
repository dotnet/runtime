using System;
using Mono.Linker.Tests.Cases.Attributes;
using Mono.Linker.Tests.Cases.Attributes.Dependencies;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

// This attribute is defined in this assembly so it will be kept
[assembly: AssemblyAttributeKeptInComplexCase.Foo]

// This attribute is not going to get marked on the first pass because the reference
// it is defined in will not have been marked yet.
// The catch is, Foo's ctor() will mark a method in `library`, at which point we now expect this
// attribute to be kept
[assembly: AssemblyAttributeKeptInComplexCase_Lib.OtherAssembly]

[assembly: KeptAttributeAttribute (typeof (AssemblyAttributeKeptInComplexCase.FooAttribute))]
[assembly: KeptAttributeAttribute (typeof (AssemblyAttributeKeptInComplexCase_Lib.OtherAssemblyAttribute))]

namespace Mono.Linker.Tests.Cases.Attributes
{
	[SetupCompileBefore ("library2.dll", new[] { "Dependencies/AssemblyAttributeKeptInComplexCase_Lib.cs" })]
	[KeptAssembly ("library2.dll")]
	[KeptMemberInAssembly ("library2.dll", typeof (AssemblyAttributeKeptInComplexCase_Lib.OtherAssemblyAttribute), ".ctor()")]
	[KeptMemberInAssembly ("library2.dll", typeof (AssemblyAttributeKeptInComplexCase_Lib), "MethodThatWillBeUsed()")]
	public class AssemblyAttributeKeptInComplexCase
	{
		static void Main ()
		{
		}

		[Kept]
		[KeptBaseType (typeof (Attribute))]
		public class FooAttribute : Attribute
		{
			[Kept]
			public FooAttribute ()
			{
				// This ctor will be marked late after processing the queue
				// This method we call will be the first marked in the referenced library
				AssemblyAttributeKeptInComplexCase_Lib.MethodThatWillBeUsed ();
			}
		}
	}
}
