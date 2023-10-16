using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.Libraries.Dependencies;

namespace Mono.Linker.Tests.Cases.Libraries
{
	/// <summary>
	/// We have to check another assembly because the test exe is included with -a and that will cause it to be linked
	/// </summary>
	[SetupCompileBefore ("childlib.dll", new[] { "Dependencies/UserAssemblyActionWorks_ChildLib.cs" })]
	[SetupCompileBefore ("lib.dll", new[] { "Dependencies/UserAssemblyActionWorks_Lib.cs" }, new[] { "childlib.dll" })]
	[SetupLinkerAction ("link", "childlib")]
	[SetupLinkerAction ("copy", "lib")]
	[SetupLinkerAction ("link", "test")]

	[KeptAllTypesAndMembersInAssembly ("lib.dll")]
	[KeptTypeInAssembly("childlib", "Mono.Linker.Tests.Cases.Libraries.Dependencies.UserAssemblyActionWorks_ChildLib")]

	[KeptMemberInAssembly ("childlib", "Mono.Linker.Tests.Cases.Libraries.Dependencies.UserAssemblyActionWorks_ChildLib", "MustOverride()")]

	[RemovedMemberInAssembly ("childlib", "Mono.Linker.Tests.Cases.Libraries.Dependencies.UserAssemblyActionWorks_ChildLib", "ChildUnusedMethod(Mono.Linker.Tests.Cases.Libraries.Dependencies.InputType)")]
	[RemovedMemberInAssembly ("childlib", "Mono.Linker.Tests.Cases.Libraries.Dependencies.UserAssemblyActionWorks_ChildLib", "ChildUnusedPrivateMethod()")]
	[RemovedMemberInAssembly ("childlib", "Mono.Linker.Tests.Cases.Libraries.Dependencies.UserAssemblyActionWorks_ChildLib", "ChildUnusedInstanceMethod()")]
	[RemovedMemberInAssembly ("childlib", "Mono.Linker.Tests.Cases.Libraries.Dependencies.UserAssemblyActionWorks_ChildLib", "UnusedProperty")]
	[RemovedMemberInAssembly ("childlib", "Mono.Linker.Tests.Cases.Libraries.Dependencies.UserAssemblyActionWorks_ChildLib", "UnusedField")]
	[RemovedTypeInAssembly ("childlib", "Mono.Linker.Tests.Cases.Libraries.Dependencies.InputType")]
	public class UserAssemblyActionWorks
	{
		public static void Main ()
		{
			UserAssemblyActionWorks_Lib.Used ();
		}
	}
}
