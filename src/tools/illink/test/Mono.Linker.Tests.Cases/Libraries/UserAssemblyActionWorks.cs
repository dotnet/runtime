using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.Libraries.Dependencies;

namespace Mono.Linker.Tests.Cases.Libraries
{
	/// <summary>
	/// We have to check another assembly because the test exe is included with -a and that will cause it to be linked
	/// </summary>
	[SetupLinkerDefaultAction ("copy")]
	[SetupCompileBefore ("lib.dll", new[] { "Dependencies/UserAssemblyActionWorks_Lib.cs" })]
	[KeptAllTypesAndMembersInAssembly ("lib.dll")]
	[SetupLinkerAction ("link", "test")]
	public class UserAssemblyActionWorks
	{
		public static void Main ()
		{
			UserAssemblyActionWorks_Lib.Used ();
		}
	}
}