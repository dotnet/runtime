// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.LinkXml
{
	[SetupLinkerDescriptorFile ("CanPreserveNamespace.xml")]
	public class CanPreserveNamespace
	{
		public static void Main ()
		{
		}
	}
}

namespace Mono.Linker.Tests.Cases.LinkXml.PreserveNamespace2
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

namespace Mono.Linker.Tests.Cases.LinkXml.PreserveNamespace2.SubNamespace
{
	class Type1
	{
	}

	class Type2
	{
		public void Method ()
		{
		}
	}
}
