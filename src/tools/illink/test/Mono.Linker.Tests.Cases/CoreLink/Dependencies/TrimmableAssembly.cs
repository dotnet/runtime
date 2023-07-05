using System.Reflection;

[assembly: AssemblyMetadata ("IsTrimmable", "True")]

namespace Mono.Linker.Tests.Cases.CoreLink.Dependencies
{
	public static class TrimmableAssembly
	{
		public static void Used ()
		{
		}

		public static void Unused ()
		{
		}
	}
}