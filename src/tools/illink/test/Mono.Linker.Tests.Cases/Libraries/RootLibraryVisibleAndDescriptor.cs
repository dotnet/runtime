using System.Runtime.CompilerServices;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Libraries
{
	[Kept]
	[KeptMember (".ctor()")]
	[SetupLinkerLinkPublicAndFamily]
	[SetupLinkerDescriptorFile ("RootLibraryVisibleAndDescriptor.xml")]
	public class RootLibraryVisibleAndDescriptor
	{
		[Kept]
		private int field;

		[Kept]
		public static void Main ()
		{
		}

		[Kept]
		public void UnusedPublicMethod ()
		{
		}

		[Kept]
		protected void UnusedProtectedMethod ()
		{
		}

		[Kept]
		protected internal void UnusedProtectedInternalMethod ()
		{
		}

		internal void UnusedInternalMethod ()
		{
		}

		private void UnusedPrivateMethod ()
		{
		}

		[Kept]
		internal void UnusedInternalMethod_Descriptor ()
		{
		}
	}
}
