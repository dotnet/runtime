using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.LinkXml
{
	class CanPreserveMarkedChildrenOfUnrequiredType
	{
		public static void Main () {
		}

		[Kept]
		public int Field1;

		public int Field2;

		[Kept]
		public void Method1 () { }

		public void Method2 () { }

		[Kept]
		public int Property1 {
			[Kept]
			get {
				return Field1;
			}
			[Kept]
			set {
				Field1 = value;
			}
		}

		public int Property2 {
			get {
				return Field2;
			}
			set {
				Field2 = value;
			}
		}
	}
}
