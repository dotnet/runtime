using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.LinkXml.Dependencies;

namespace Mono.Linker.Tests.Cases.LinkXml
{
	[SetupCompileBefore ("Library.dll", new [] { "Dependencies/CanPreserveMarkedChildrenOfUnrequiredType_Library.cs" })]
	[KeptMemberInAssembly ("Library.dll", typeof (CanPreserveMarkedChildrenOfUnrequiredType_Library), "Field1", "Method1()", "Property1")]
	[RemovedMemberInAssembly ("Library.dll", typeof (CanPreserveMarkedChildrenOfUnrequiredType_Library), "Field2", "Method2()", "Property2")]

	class CanPreserveMarkedChildrenOfUnrequiredType
	{
		public static void Main () {
			// Mark the type by calling its ctor.
			new CanPreserveMarkedChildrenOfUnrequiredType_Library ();
		}
	}
}
