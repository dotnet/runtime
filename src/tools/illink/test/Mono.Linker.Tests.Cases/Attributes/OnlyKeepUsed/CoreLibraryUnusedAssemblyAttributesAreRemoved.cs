using System.Reflection;
using System.Timers;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Attributes.OnlyKeepUsed
{
	[Reference ("System.dll")]
	[SetupLinkerTrimMode ("link")]
	[SetupLinkerArgument ("--used-attrs-only", "true")]
	[RemovedAttributeInAssembly (PlatformAssemblies.CoreLib, typeof (AssemblyDescriptionAttribute))]
#if !NET
	[RemovedAttributeInAssembly ("System.dll", typeof (AssemblyDescriptionAttribute))]
#endif
	public class CoreLibraryUnusedAssemblyAttributesAreRemoved
	{
		public static void Main ()
		{
			// Use something from System so that the entire reference isn't linked away
			var system = new Timer ();
		}
	}
}