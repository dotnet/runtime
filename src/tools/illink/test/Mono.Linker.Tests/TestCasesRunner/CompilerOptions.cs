using System;
using Mono.Linker.Tests.Extensions;

namespace Mono.Linker.Tests.TestCasesRunner {
	public class CompilerOptions {
		public NPath OutputPath;
		public NPath[] SourceFiles;
		public string[] Defines;
		public NPath[] References;
		public NPath[] Resources;
		public string[] AdditionalArguments;
		public string CompilerToUse;
	}
}
