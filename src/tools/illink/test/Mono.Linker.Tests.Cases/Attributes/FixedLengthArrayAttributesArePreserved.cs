using System.Runtime.CompilerServices;
using System.Security;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Attributes
{
	/// <summary>
	/// The purpose of this test is mainly to provide coverage on the `KeptAttributeOnFixedBufferType` attribute
	/// </summary>
	[SetupCompileArgument ("/unsafe")]
	// Can't verify because the test contains unsafe code
	[SkipPeVerify]
	public class FixedLengthArrayAttributesArePreserved
	{
		public static void Main ()
		{
			Helper ();
		}

		[Kept]
		static unsafe void Helper ()
		{
			var tmp = new WithFixedArrayField ();
			var v = tmp.Values;
			AMethodToUseTheReturnValue (v);
		}

		[Kept]
		static unsafe void AMethodToUseTheReturnValue (int* ptr)
		{
		}

		[Kept]
		public unsafe struct WithFixedArrayField
		{
			[Kept]
			[KeptFixedBuffer]
			[KeptAttributeOnFixedBufferType (typeof (UnsafeValueTypeAttribute))]
			[KeptAttributeAttribute (typeof (FixedBufferAttribute))]
			public fixed int Values[10];
		}
	}
}