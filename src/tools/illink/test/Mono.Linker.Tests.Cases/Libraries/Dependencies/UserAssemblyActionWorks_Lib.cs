namespace Mono.Linker.Tests.Cases.Libraries.Dependencies
{
	public class UserAssemblyActionWorks_Lib : UserAssemblyActionWorks_ChildLib
	{
		public override void MustOverride () { }

		public static void Used ()
		{
		}

		public static void Unused ()
		{
		}
	}
}
