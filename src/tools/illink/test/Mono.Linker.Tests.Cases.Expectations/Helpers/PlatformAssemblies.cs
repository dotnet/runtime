namespace Mono.Linker.Tests.Cases.Expectations.Helpers
{
	public static class PlatformAssemblies
	{
#if NETCOREAPP
		public const string CoreLib = "System.Private.CoreLib.dll";
#else
		public const string CoreLib = "mscorlib.dll";
#endif
	}
}