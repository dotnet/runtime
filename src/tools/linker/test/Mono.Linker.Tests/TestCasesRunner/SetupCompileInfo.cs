using Mono.Linker.Tests.Extensions;

namespace Mono.Linker.Tests.TestCasesRunner
{
	public class SetupCompileInfo
	{
		public string OutputName;
		public NPath[] SourceFiles;
		public string[] Defines;
		public string[] References;
		public SourceAndDestinationPair[] Resources;
		public string AdditionalArguments;
		public string CompilerToUse;
		public bool AddAsReference;
		public bool RemoveFromLinkerInput;
		public string OutputSubFolder;
	}
}
