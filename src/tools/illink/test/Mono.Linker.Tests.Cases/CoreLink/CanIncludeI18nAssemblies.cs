using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.CoreLink {
	[SetupLinkerCoreAction ("link")]
	[Il8n ("all")]
	
	// i18n assemblies should only be included when processing mono class libs.  By forcing this test to use mcs,
	// we can ensure the test will be ignored when the test runs against .net fw assemblies.
	[SetupCSharpCompilerToUse ("mcs")]
	
	[KeptAssembly ("I18N.dll")]
	[KeptAssembly ("I18N.CJK.dll")]
	[KeptAssembly ("I18N.MidEast.dll")]
	[KeptAssembly ("I18N.Other.dll")]
	[KeptAssembly ("I18N.Rare.dll")]
	[KeptAssembly ("I18N.West.dll")]
	
	[SkipPeVerify (SkipPeVerifyForToolchian.Pedump)]
	public class CanIncludeI18nAssemblies {
		public static void Main ()
		{
		}
	}
}