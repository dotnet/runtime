using System.Runtime.CompilerServices;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Advanced {
	[IgnoreTestCase("Bug introduced with https://github.com/mono/linker/pull/348")]
	[SetupLinkerCoreAction ("copy")]
	[RemovedAssembly ("System.Core.dll")]
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