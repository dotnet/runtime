using System.Reflection;
using System.Timers;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Attributes
{
	[Reference ("System.dll")]
	[SetupLinkerCoreAction ("link")]
	[KeptAttributeInAssembly (PlatformAssemblies.CoreLib, typeof (AssemblyDescriptionAttribute))]
	[KeptAttributeInAssembly (PlatformAssemblies.CoreLib, typeof (AssemblyCompanyAttribute))]
#if !NETCOREAPP
	[KeptAttributeInAssembly ("System.dll", typeof (AssemblyDescriptionAttribute))]
	[KeptAttributeInAssembly ("System.dll", typeof (AssemblyCompanyAttribute))]
#endif
	[SkipPeVerify]
	public class CoreLibraryAssemblyAttributesAreKept
	{
		public static void Main ()
		{
			// Use something from System so that the entire reference isn't linked away
			var system = new Timer ();
		}
	}
}