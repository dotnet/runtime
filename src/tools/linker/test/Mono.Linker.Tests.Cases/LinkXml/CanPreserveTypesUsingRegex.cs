using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.LinkXml
{
	[KeptMember (".ctor()")]
	[SetupLinkerDescriptorFile ("CanPreserveTypesUsingRegex.xml")]
	class CanPreserveTypesUsingRegex
	{
		public static void Main ()
		{
		}

		[Kept]
		void UnusedHelper ()
		{
		}

		[Kept]
		[KeptMember (".ctor()")]
		class Bar
		{
		}
	}
}

namespace Mono.Linker.Tests.Cases.LinkXml.PreserveNamespace
{
	[Kept]
	[KeptMember (".ctor()")]
	class Type1
	{
		[Kept]
		public int UnusedField;
	}

	[Kept]
	[KeptMember (".ctor()")]
	class Type2
	{
		[Kept]
		public void Method ()
		{
		}
	}
}

namespace Mono.Linker.Tests.Cases.LinkXml.PreserveNamespace.SubNamespace
{
	[Kept]
	[KeptMember (".ctor()")]
	class Type1
	{
		[Kept]
		public int UnusedField;
	}

	[Kept]
	[KeptMember (".ctor()")]
	class Type2
	{
		[Kept]
		public void Method ()
		{
		}
	}
}
