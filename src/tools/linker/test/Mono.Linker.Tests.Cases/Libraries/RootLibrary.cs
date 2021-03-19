using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Libraries
{
	[SetupLinkerArgument ("-a", "test.exe", "library")]
	[SetupLinkerArgument ("--enable-opt", "ipconstprop")]
	public class RootLibrary
	{
		private int field;

		[Kept]
		public RootLibrary ()
		{
		}

		[Kept]
		public static void Main ()
		{
			var t = typeof (SerializationTestPrivate);
			t = typeof (SerializationTestNested.SerializationTestPrivate);
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

		protected private void UnusedProtectedPrivateMethod ()
		{
		}

		internal void UnusedInternalMethod ()
		{
		}

		private void UnusedPrivateMethod ()
		{
		}

		[Kept]
		[KeptAttributeAttribute (typeof (DynamicDependencyAttribute))]
		[DynamicDependency (nameof (MethodWithDynamicDependencyTarget))]
		public void MethodWithDynamicDependency ()
		{
		}

		[Kept]
		private void MethodWithDynamicDependencyTarget ()
		{
		}

		[Kept]
		public class SerializationTest
		{
			[Kept]
			private SerializationTest (SerializationInfo info, StreamingContext context)
			{
			}
		}

		[Kept]
		private class SerializationTestPrivate
		{
			[Kept]
			private SerializationTestPrivate (SerializationInfo info, StreamingContext context)
			{
			}

			public void NotUsed ()
			{
			}
		}

		[Kept]
		private class SerializationTestNested
		{
			internal class SerializationTestPrivate
			{
				[Kept]
				private SerializationTestPrivate (SerializationInfo info, StreamingContext context)
				{
				}

				public void NotUsed ()
				{
				}
			}

			public void NotUsed ()
			{
			}
		}

		[Kept]
		public class SubstitutionsTest
		{
			[Kept]
			private static bool FalseProp { [Kept] get { return false; } }

			[Kept]
			[ExpectBodyModified]
			public SubstitutionsTest ()
			{
				if (FalseProp)
					LocalMethod ();
			}

			private void LocalMethod ()
			{
			}
		}

		[Kept]
		[KeptInterface (typeof (I))]
		public class IfaceClass : I
		{
			[Kept]
			public IfaceClass ()
			{
			}

			[Kept]
			public override string ToString ()
			{
				return "test";
			}
		}

		[Kept]
		public interface I
		{
		}
	}

	internal class RootLibrary_Internal
	{
		protected RootLibrary_Internal (SerializationInfo info, StreamingContext context)
		{
		}

		internal void Unused ()
		{
		}
	}
}
