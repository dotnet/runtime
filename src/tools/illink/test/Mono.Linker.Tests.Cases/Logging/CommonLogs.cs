using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Logging
{

#if !NETCOREAPP
	[IgnoreTestCase ("Can be enabled once MonoBuild produces a dll from which we can grab the types in the Mono.Linker namespace.")]
#else
	[SetupCompileBefore ("LogStep.dll", new[] { "Dependencies/LogStep.cs" }, new[] { "illink.dll", "Mono.Cecil.dll" })]
#endif
	[SetupLinkerArgument ("--custom-step", "Log.LogStep,LogStep.dll")]
	[SetupLinkerArgument ("--verbose")]
	[LogContains ("ILLink: error IL6001: Error")]
	[LogContains ("logtest(1,1): warning IL6002: Warning")]
	[LogContains ("ILLink: Info")]
	[LogContains ("ILLink: Diagnostics")]
	public class CommonLogs
	{
		public static void Main ()
		{
		}
	}
}
