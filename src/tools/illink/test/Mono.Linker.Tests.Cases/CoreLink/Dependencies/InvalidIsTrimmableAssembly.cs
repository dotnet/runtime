using System.Reflection;

[assembly: AssemblyMetadata ("IsTrimmable", "true")]
[assembly: AssemblyMetadata ("IsTrimmable", "False")]

namespace Mono.Linker.Tests.Cases.CoreLink.Dependencies
{
	public static class InvalidIsTrimmableAssembly
	{
		public static void Used ()
		{
		}

		public static void Unused ()
		{
		}
	}
}