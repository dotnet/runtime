using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Substitutions
{
	[SetupLinkerSubstitutionFile ("ResourceSubstitutions.xml")]
	[SetupCompileResource ("Dependencies/ResourceFile.txt", "ResourceFile.txt")]
	[ExpectedWarning ("IL2038", FileName = "ResourceSubstitutions.xml")]
	[ExpectedWarning ("IL2039", "''", "ResourceFile.txt", FileName = "ResourceSubstitutions.xml")]
	[ExpectedWarning ("IL2040", "MissingResourceFile.txt", "test", FileName = "ResourceSubstitutions.xml")]
	[RemovedResourceInAssembly ("test.exe", "ResourceFile.txt")]
	public class ResourceSubstitutions
	{
		public static void Main ()
		{
		}
	}
}
