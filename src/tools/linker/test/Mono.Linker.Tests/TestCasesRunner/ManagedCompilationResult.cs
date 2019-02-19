using Mono.Linker.Tests.Extensions;

namespace Mono.Linker.Tests.TestCasesRunner {
	public class ManagedCompilationResult {
		public ManagedCompilationResult (NPath inputAssemblyPath, NPath expectationsAssemblyPath)
		{
			InputAssemblyPath = inputAssemblyPath;
			ExpectationsAssemblyPath = expectationsAssemblyPath;
		}

		public NPath InputAssemblyPath { get; }

		public NPath ExpectationsAssemblyPath { get; }
	}
}