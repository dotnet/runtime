using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.LinkXml
{
	[KeptMember (".ctor()")]
	class CanPreserveTypesUsingRegex {
		public static void Main () {
		}

		[Kept]
		void UnusedHelper () {
		}

		[Kept]
		[KeptMember (".ctor()")]
		class Bar {
		}
	}
}
