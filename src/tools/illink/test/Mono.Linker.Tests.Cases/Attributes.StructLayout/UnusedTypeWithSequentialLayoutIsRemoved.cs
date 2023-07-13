using System.Runtime.InteropServices;

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