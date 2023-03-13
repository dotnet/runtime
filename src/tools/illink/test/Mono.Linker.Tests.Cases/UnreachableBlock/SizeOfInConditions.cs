using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.UnreachableBlock
{
#if NETCOREAPP
	[SetupLinkerSubstitutionFile ("SizeOfInConditions.netcore.xml")]
#else
	[SetupLinkerSubstitutionFile ("SizeOfInConditions.net_4_x.xml")]
#endif
	[SetupCompileArgument ("/unsafe")]
	[SetupLinkerArgument ("--enable-opt", "ipconstprop")]
	public unsafe class SizeOfInConditions
	{
		public static void Main ()
		{
			TestIntPtr ();
			TestUIntPtr ();
		}

		[Kept]
		[ExpectBodyModified]
		static void TestIntPtr ()
		{
			if (sizeof (IntPtr) != 4) {
			} else {
				Reached_1 ();
			}
		}

		[Kept]
		[ExpectBodyModified]
		static void TestUIntPtr ()
		{
			if (sizeof (UIntPtr) != 8) {
			} else {
				Reached_2 ();
			}
		}

		static void Reached_1 ()
		{
		}

		static void Reached_2 ()
		{
		}
	}
}