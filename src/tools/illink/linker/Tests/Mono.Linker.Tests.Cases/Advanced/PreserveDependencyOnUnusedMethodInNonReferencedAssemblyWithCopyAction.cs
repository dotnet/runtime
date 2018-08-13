using System.Runtime.CompilerServices;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Advanced {
	[SetupLinkerCoreAction ("copyused")]
	[RemovedAssembly ("System.Core.dll")]
	[SkipPeVerify]
	public class PreserveDependencyOnUnusedMethodInNonReferencedAssemblyWithCopyAction {
		public static void Main ()
		{
		}

		[PreserveDependency (".ctor()", "System.Security.Cryptography.AesCryptoServiceProvider", "System.Core")]
		static void Dependency ()
		{
		}
	}
}