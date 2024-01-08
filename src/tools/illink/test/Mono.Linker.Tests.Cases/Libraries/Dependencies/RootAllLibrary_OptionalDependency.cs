using System.Diagnostics.CodeAnalysis;

namespace Mono.Linker.Tests.Cases.Libraries.Dependencies
{
	public class RootAllLibrary_OptionalDependency
	{
		[RequiresUnreferencedCode (nameof (RootAllLibrary_OptionalDependency))]
		public static void Use ()
		{
		}
	}
}
