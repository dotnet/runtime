using System.Runtime.InteropServices;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Attributes.StructLayout
{
	class UnusedTypeWithSequentialLayoutIsRemoved
	{
		static void Main ()
		{
		}

		[StructLayout (LayoutKind.Sequential)]
		class B
		{
			int a;
		}
	}
}